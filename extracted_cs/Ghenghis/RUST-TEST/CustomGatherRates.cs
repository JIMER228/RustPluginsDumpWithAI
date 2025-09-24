/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Custom Gather Rates", "VisEntities", "1.0.0")]
    [Description("Change how much players get when gathering resources.")]
    public class CustomGatherRates : RustPlugin
    {
        #region Fields

        private static CustomGatherRates _plugin;
        private static Configuration _config;
        private readonly Dictionary<MiningQuarry, BasePlayer> _quarryStarters = new Dictionary<MiningQuarry, BasePlayer>();
        private readonly Dictionary<ExcavatorArm, BasePlayer> _excavatorStarters = new Dictionary<ExcavatorArm, BasePlayer>();

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Show Rates Chat Command")]
            public string ShowRatesChatCommand { get; set; }

            [JsonProperty("Global Rates")]
            public Dictionary<string, Dictionary<string, float>> GlobalRates { get; set; }

            [JsonProperty("Permission Profiles")]
            public List<PermissionRateConfig> PermissionProfiles { get; set; }
        }

        private class PermissionRateConfig
        {
            [JsonProperty("Permission Suffix")]
            public string PermissionSuffix { get; set; }

            [JsonIgnore]
            public string FullPermission { get; set; }

            [JsonProperty("Rates")]
            public Dictionary<string, Dictionary<string, float>> Rates { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                ShowRatesChatCommand = "gather",
                GlobalRates = CreateEmptyRateDictionary(),
                PermissionProfiles = new List<PermissionRateConfig>
                {
                    new PermissionRateConfig
                    {
                        PermissionSuffix = "vip",
                        Rates = new Dictionary<string, Dictionary<string, float>>
                        {
                            ["Dispenser"] = new Dictionary<string, float>
                            {
                                ["*"] = 1.5f,
                                ["wood"] = 2.0f
                            },
                            ["Pickup"] = new Dictionary<string, float>
                            {
                                ["*"] = 1.75f
                            },
                            ["Growable"] = new Dictionary<string, float>
                            {
                                ["*"] = 1.25f
                            },
                            ["Quarry"]    = new Dictionary<string, float> { ["*"] = 1.0f },
                            ["Excavator"] = new Dictionary<string, float> { ["*"] = 1.0f }
                        }
                    }
                }
            };
        }

        private Dictionary<string, Dictionary<string, float>> CreateEmptyRateDictionary()
        {
            string[] buckets = { "Dispenser", "Growable", "Pickup", "Quarry", "Excavator" };
            var dict = new Dictionary<string, Dictionary<string, float>>(buckets.Length);
            foreach (string key in buckets)
                dict[key] = new Dictionary<string, float> { { "*", 1f } };
            return dict;
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions(_config.PermissionProfiles);
            cmd.AddChatCommand(_config.ShowRatesChatCommand, this, nameof(cmdShowRates));
        }

        private void Unload()
        {
            _quarryStarters.Clear();
            _excavatorStarters.Clear();

            _config = null;
            _plugin = null;
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (dispenser == null || entity == null || item == null)
                return;

            BasePlayer player = entity as BasePlayer;
            if (player == null)
                return;

            float multiplier = GetGatherMultiplier(player, "Dispenser", item.info.shortname);
            ApplyMultiplier(item, multiplier);
        }

        private void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (dispenser == null || entity == null || item == null)
                return;

            OnDispenserGather(dispenser, entity, item);
        }

        private void OnGrowableGathered(GrowableEntity growable, Item item, BasePlayer player)
        {
            if (growable == null || item == null || player == null)
                return;

            float multiplier = GetGatherMultiplier(player, "Growable", item.info.shortname);
            ApplyMultiplier(item, multiplier);
        }

        private object CanTakeCutting(BasePlayer player, GrowableEntity plant)
        {
            if (player == null || plant == null || !plant.CanClone())
                return null;

            int amount = plant.Properties.BaseCloneCount + plant.Genes.GetGeneTypeCount(GrowableGenetics.GeneType.Yield) / 2;
            if (amount <= 0)
                return null;

            float multiplier = GetGatherMultiplier(player, "Growable", plant.Properties.CloneItem.shortname);
            amount = Mathf.Max(1, Mathf.CeilToInt(amount * multiplier));

            Item clone = ItemManager.Create(plant.Properties.CloneItem, amount, 0UL, true);
            clone.SetItemOwnership(player, ItemOwnershipPhrases.Cloned);
            GrowableGeneEncoding.EncodeGenesToItem(plant, clone);

            player.GiveItem(clone, BaseEntity.GiveItemReason.ResourceHarvested);

            if (plant.Properties.pickEffect.isValid)
                Effect.server.Run(plant.Properties.pickEffect.resourcePath, plant.transform.position, Vector3.up);

            plant.TellPlanter();
            plant.Die(null);

            return false;
        }

        private void OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
        {
            if (collectible.itemList == null || player == null)
                return;

            for (int i = 0; i < collectible.itemList.Length; i++)
            {
                ItemAmount amt = collectible.itemList[i];
                if (amt == null || amt.itemDef == null)
                    continue;

                float multiplier = GetGatherMultiplier(player, "Pickup", amt.itemDef.shortname);
                ApplyMultiplier(amt, multiplier);
            }
        }

        private void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            if (quarry == null || item == null)
                return;

            _quarryStarters.TryGetValue(quarry, out var starter);

            float multiplier = GetGatherMultiplier(starter, "Quarry", item.info.shortname);
            ApplyMultiplier(item, multiplier);
        }

        private void OnExcavatorGather(ExcavatorArm excavator, Item item)
        {
            if (excavator == null || item == null)
                return;

            _excavatorStarters.TryGetValue(excavator, out var starter);

            float multiplier = GetGatherMultiplier(starter, "Excavator", item.info.shortname);
            ApplyMultiplier(item, multiplier);
        }

        private void OnQuarryToggled(MiningQuarry quarry, BasePlayer player)
        {
            if (quarry == null || player == null)
                return;

            if (quarry.IsEngineOn())
                _quarryStarters[quarry] = player;
            else
                _quarryStarters.Remove(quarry);
        }

        private void OnExcavatorResourceSet(ExcavatorArm arm, string resourceCode, BasePlayer player)
        {
            if (arm != null && player != null)
                _excavatorStarters[arm] = player;
        }

        private void OnExcavatorMiningToggled(ExcavatorArm arm)
        {
            if (arm != null && !arm.IsMining())
                _excavatorStarters.Remove(arm);
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            if (entity is MiningQuarry quarry)
            {
                _quarryStarters.Remove(quarry);
            }
            else if (entity is ExcavatorArm arm)
            {
                _excavatorStarters.Remove(arm);
            }
        }

        #endregion Oxide Hooks

        #region Multiplier Helpers

        private void ApplyMultiplier(Item item, float multiplier)
        {
            if (multiplier <= 0f || Math.Abs(multiplier - 1f) < float.Epsilon)
                return;

            item.amount = Mathf.Max(1, (int)Mathf.Ceil(item.amount * multiplier));
        }

        private void ApplyMultiplier(ItemAmount amount, float multiplier)
        {
            if (multiplier <= 0f || Math.Abs(multiplier - 1f) < float.Epsilon)
                return;

            amount.amount = Mathf.Max(1f, Mathf.Ceil(amount.amount * multiplier));
        }

        private void AdjustDispenserRemainder(ResourceDispenser dispenser, Item item, int originalAmount, float multiplier)
        {
            if (multiplier <= 1f || dispenser == null || dispenser.containedItems == null)
                return;

            int delta = item.amount - originalAmount;
            if (delta <= 0) return;

            foreach (ItemAmount entry in dispenser.containedItems)
            {
                if (entry == null) continue;
                if (entry.itemDef == null) continue;
                if (entry.itemDef.shortname != item.info.shortname)
                    continue;

                entry.amount += delta;
                break;
            }
        }

        private float GetGatherMultiplier(BasePlayer gatherer, string gatherCategory, string itemShortName)
        {
            foreach (PermissionRateConfig permProfile in _config.PermissionProfiles)
            {
                if (!PermissionUtil.HasPermission(gatherer, permProfile.FullPermission))
                    continue;

                if (!permProfile.Rates.TryGetValue(gatherCategory, out var ratesByItem))
                    continue;

                if (ratesByItem.TryGetValue(itemShortName, out float multiplier))
                    return multiplier;

                if (ratesByItem.TryGetValue("*", out multiplier))
                    return multiplier;
            }

            if (_config.GlobalRates.TryGetValue(gatherCategory, out var globalRatesByItem))
            {
                if (globalRatesByItem.TryGetValue(itemShortName, out float multiplier))
                    return multiplier;

                if (globalRatesByItem.TryGetValue("*", out multiplier))
                    return multiplier;
            }

            return 1f;
        }

        #endregion Multiplier Helpers

        #region Permissions

        private static class PermissionUtil
        {
            public const string ADMIN = "customgatherrates.admin";
            private static readonly List<string> _permissions = new List<string>
            {
                ADMIN,
            };

            public static string ConstructPermission(string suffix, bool addToList = true)
            {
                string perm = string.Join(".", nameof(CustomGatherRates), suffix).ToLower();

                if (addToList && !_permissions.Contains(perm))
                    _permissions.Add(perm);

                return perm;
            }

            public static void AddPermission(string permission)
            {
                if (!_permissions.Contains(permission))
                    _permissions.Add(permission);
            }

            public static void RegisterPermissions(IEnumerable<PermissionRateConfig> dynamicPermissions = null)
            {
                foreach (string perm in _permissions)
                    _plugin.permission.RegisterPermission(perm, _plugin);

                if (dynamicPermissions == null)
                    return;

                foreach (var dynamicPerm in dynamicPermissions)
                {
                    if (dynamicPerm == null || string.IsNullOrEmpty(dynamicPerm.PermissionSuffix))
                        continue;

                    if (string.IsNullOrEmpty(dynamicPerm.FullPermission))
                        dynamicPerm.FullPermission = ConstructPermission(dynamicPerm.PermissionSuffix);

                    AddPermission(dynamicPerm.FullPermission);
                    _plugin.permission.RegisterPermission(dynamicPerm.FullPermission, _plugin);
                }
            }

            public static bool HasPermission(BasePlayer player, string permission)
            {
                return player != null && _plugin.permission.UserHasPermission(player.UserIDString, permission);
            }
        }

        #endregion Permissions

        #region Commands
        
        private void cmdShowRates(BasePlayer player, string command, string[] args)
        {
            List<string> lines = new List<string>
            {
                GetMessage(player, Lang.Info_Header_YourRates)
            };

            foreach (var categoryPair in _config.GlobalRates)
            {
                float multiplier = GetGatherMultiplier(player, categoryPair.Key, "*");
                if (Math.Abs(multiplier - 1f) > float.Epsilon)
                    lines.Add(string.Format(GetMessage(player, Lang.Info_Line_Rate), categoryPair.Key, multiplier));
            }

            if (lines.Count == 1)
                lines.Add(GetMessage(player, Lang.Info_NoRateOverrides));

            foreach (string line in lines)
                MessagePlayer(player, line);
        }

        [ConsoleCommand("gather.rate")]
        private void cmdSetRate(ConsoleSystem.Arg conArgs)
        {
            BasePlayer caller = conArgs.Player();
            bool fromServerConsole = caller == null;
            string[] arguments = conArgs.Args;

            if (!fromServerConsole && !PermissionUtil.HasPermission(caller, PermissionUtil.ADMIN))
            {
                MessagePlayer(caller, Lang.Error_NoPermission);
                return;
            }

            if (!conArgs.HasArgs(4))
            {
                string msg = GetMessage(null, Lang.Error_InvalidArguments);
                if (fromServerConsole)
                    Puts(msg);
                else
                    MessagePlayer(caller, msg);
                return;
            }

            string modeToken = arguments[0].ToLowerInvariant();
            bool globalMode = modeToken == "global";
            bool permMode = modeToken == "perm";

            if (!globalMode && !permMode)
            {
                string msg = GetMessage(null, Lang.Error_InvalidFirstArgument);
                if (fromServerConsole)
                    Puts(msg);
                else
                    MessagePlayer(caller, msg);
                return;
            }

            int argIndex = 1;
            PermissionRateConfig permProfile = null;

            if (permMode)
            {
                string suffix = arguments[argIndex++];
                permProfile = _config.PermissionProfiles.Find(p =>
                    p.PermissionSuffix.Equals(suffix, StringComparison.OrdinalIgnoreCase));

                if (permProfile == null)
                {
                    permProfile = new PermissionRateConfig
                    {
                        PermissionSuffix = suffix,
                        FullPermission = PermissionUtil.ConstructPermission(suffix),
                        Rates = CreateEmptyRateDictionary()
                    };
                    _config.PermissionProfiles.Add(permProfile);
                    PermissionUtil.AddPermission(permProfile.FullPermission);
                    PermissionUtil.RegisterPermissions(_config.PermissionProfiles);
                }
            }

            string gatherCategory = arguments[argIndex++];
            if (!_config.GlobalRates.ContainsKey(gatherCategory))
            {
                string msg = GetMessage(null, Lang.Error_InvalidGatherCategory);
                if (fromServerConsole)
                    Puts(msg);
                else
                    MessagePlayer(caller, msg);
                return;
            }

            string resourceKey = arguments[argIndex++];
            string multiplierToken = arguments[argIndex];

            if (!float.TryParse(multiplierToken, out float multiplier) || multiplier <= 0f)
            {
                string msg = GetMessage(null, Lang.Error_InvalidMultiplier);
                if (fromServerConsole)
                    Puts(msg);
                else
                    MessagePlayer(caller, msg);
                return;
            }

            Dictionary<string, Dictionary<string, float>> rateTable;
            if (globalMode)
                rateTable = _config.GlobalRates;
            else
                rateTable = permProfile.Rates;

            rateTable[gatherCategory][resourceKey] = multiplier;
            SaveConfig();

            string success = GetMessage(null, Lang.Info_RateSet,
                                        gatherCategory, resourceKey, multiplier);
            if (fromServerConsole)
                Puts(success);
            else
                MessagePlayer(caller, success);
        }
        
        #endregion Commands

        #region Localization

        private class Lang
        {
            public const string Error_NoPermission = "Error.NoPermission";
            public const string Error_InvalidArguments = "Error.InvalidArguments";
            public const string Error_InvalidFirstArgument = "Error.InvalidFirstArgument";
            public const string Error_InvalidPermissionName = "Error.InvalidPermissionName";
            public const string Error_InvalidGatherCategory = "Error.InvalidGatherCategory";
            public const string Error_InvalidMultiplier = "Error.InvalidMultiplier";
            public const string Error_EntryDoesNotExist = "Error.EntryDoesNotExist";
            public const string Info_Header_YourRates = "Info.Header.YourRates";
            public const string Info_Line_Rate = "Info.Line.Rate";
            public const string Info_NoRateOverrides = "Info.NoRateOverrides";
            public const string Info_RateSet = "Info.RateSet";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.Error_NoPermission] = "You do not have permission to use this command.",
                [Lang.Error_InvalidArguments] = "Invalid arguments supplied.",
                [Lang.Error_InvalidFirstArgument] = "First argument must be 'global' or 'perm'.",
                [Lang.Error_InvalidPermissionName] = "Permission name is not valid.",
                [Lang.Error_InvalidGatherCategory] = "Gather category is not recognised.",
                [Lang.Error_InvalidMultiplier] = "Multiplier must be a number greater than 0.",
                [Lang.Error_EntryDoesNotExist] = "There is no existing rate entry to remove.",

                [Lang.Info_Header_YourRates] = "Your gather multipliers:",
                [Lang.Info_Line_Rate] = "{0}: x{1}",
                [Lang.Info_NoRateOverrides] = "All rates are default (x1).",
                [Lang.Info_RateSet] = "Set {0} / {1} to x{2}.",
            }, this, "en");
        }

        private static string GetMessage(BasePlayer player, string messageKey, params object[] args)
        {
            string userId;
            if (player != null)
                userId = player.UserIDString;
            else
                userId = null;

            string message = _plugin.lang.GetMessage(messageKey, _plugin, userId);

            if (args.Length > 0)
                message = string.Format(message, args);

            return message;
        }

        public static void MessagePlayer(BasePlayer player, string messageKey, params object[] args)
        {
            string message = GetMessage(player, messageKey, args);

            if (!string.IsNullOrWhiteSpace(message))
                _plugin.SendReply(player, message);
        }

        #endregion Localization
    }
}