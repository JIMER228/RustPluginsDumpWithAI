/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Farming Restriction", "VisEntities", "1.0.0")]
    [Description("Blocks all farming actions while building blocked, including trees, ore, and planters.")]
    public class FarmingRestriction : RustPlugin
    {
        #region Fields

        private static FarmingRestriction _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Block Tree Farming When Building Blocked")]
            public bool BlockTreeFarmingWhenBuildingBlocked { get; set; }

            [JsonProperty("Block Ore Node Farming When Building Blocked")]
            public bool BlockOreNodesWhenBuildingBlocked { get; set; }

            [JsonProperty("Block Collectible Pickup When Building Blocked")]
            public bool BlockCollectiblesWhenBuildingBlocked { get; set; }

            [JsonProperty("Block Planter Harvest When Building Blocked")]
            public bool BlockPlantersWhenBuildingBlocked { get; set; }
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
                BlockTreeFarmingWhenBuildingBlocked = true,
                BlockOreNodesWhenBuildingBlocked = true,
                BlockCollectiblesWhenBuildingBlocked = true,
                BlockPlantersWhenBuildingBlocked = true
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private object OnMeleeAttack(BasePlayer player, HitInfo hitInfo)
        {
            if (player == null || hitInfo == null)
                return null;

            if (!ShouldBlock(player))
                return null;

            BaseEntity targetEntity = hitInfo.HitEntity;
            if (targetEntity == null)
                return null;

            if (_config.BlockTreeFarmingWhenBuildingBlocked && targetEntity is TreeEntity)
            {
                ShowToast(player, Lang.Blocked_FarmingDenied, GameTip.Styles.Red_Normal);
                return true;
            }

            if (_config.BlockOreNodesWhenBuildingBlocked && targetEntity is ResourceEntity)
            {
                ShowToast(player, Lang.Blocked_FarmingDenied, GameTip.Styles.Red_Normal);
                return true;
            }

            return null;
        }

        private object OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
        {
            if (player == null)
                return null;

            if (!_config.BlockCollectiblesWhenBuildingBlocked)
                return null;

            if (!ShouldBlock(player))
                return null;

            ShowToast(player, Lang.Blocked_FarmingDenied, GameTip.Styles.Red_Normal);
            return true;
        }

        private object OnGrowableGather(GrowableEntity growable, BasePlayer player)
        {
            if (player == null)
                return null;

            if (!_config.BlockPlantersWhenBuildingBlocked)
                return null;

            if (!ShouldBlock(player))
                return null;

            ShowToast(player, Lang.Blocked_FarmingDenied, GameTip.Styles.Red_Normal);
            return true;
        }

        #endregion Oxide Hooks

        #region Helper Functions

        private bool ShouldBlock(BasePlayer player)
        {
            if (player == null)
                return false;

            if (PermissionUtil.HasPermission(player, PermissionUtil.BYPASS))
                return false;

            return player.IsBuildingBlocked();
        }

        #endregion Helper Functions

        #region Permissions

        private static class PermissionUtil
        {
            public const string BYPASS = "farmingrestriction.bypass";

            private static readonly List<string> _permissions = new List<string>
            {
                BYPASS,
            };

            public static void RegisterPermissions()
            {
                foreach (string perm in _permissions)
                    _plugin.permission.RegisterPermission(perm, _plugin);
            }

            public static bool HasPermission(BasePlayer player, string permission)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permission);
            }
        }

        #endregion Permissions

        #region Localization

        private class Lang
        {
            public const string Blocked_FarmingDenied = "Blocked.FarmingDenied";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.Blocked_FarmingDenied] = "You cannot farm here (building blocked)."

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

        public static void ReplyToPlayer(BasePlayer player, string messageKey, params object[] args)
        {
            string message = GetMessage(player, messageKey, args);

            if (!string.IsNullOrWhiteSpace(message))
                _plugin.SendReply(player, message);
        }

        public static void ShowToast(BasePlayer player, string messageKey, GameTip.Styles style = GameTip.Styles.Blue_Normal, params object[] args)
        {
            string message = GetMessage(player, messageKey, args);

            if (!string.IsNullOrWhiteSpace(message))
                player.SendConsoleCommand("gametip.showtoast", (int)style, message);
        }

        #endregion Localization
    }
}