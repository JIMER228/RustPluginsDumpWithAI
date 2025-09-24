// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6


using Oxide.Core;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Oxide.Plugins.SimpleStatusExtensionMethods;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("SimpleStatus", "mr01sam", "1.0.7")]
    [Description("Allows plugins to add custom status displays for the UI.")]
    partial class SimpleStatus : CovalencePlugin
    {
        /* // Changelog
        ## v1.0.0
        - Initial release
        ## v1.0.1
        - Statuses will not be cleaned up when a plugin is unloaded
        ## v1.0.2
        - Fixed issue where statuses were not refreshed for players who were offline/reconnecting
        - Fixed statuses overlaying ontop of maps and other UI elements
        - Fixed problem where the cache was not being cleared when statuses were updated from the CreateStatus api
        - You can now specify asset sprite paths in addition to imageLibaryNames. Asset paths must start with "assets/"
        - Moved icons to the left by 1px to match vanilla statuses more
        - Updated demo code on plugin page
        ## v1.0.3
        - Updated the background material for statuses to more closely reflect vanilla UI on all colors
        - Statuses will not immediately update when a plugin is unloaded
        # v1.0.4
        - Fixed caching issue where static cache was not cleared when Simple Status is unloaded
        # v1.0.5
        - Fixed issue when picking up items would cause statuses to stack higher when they shouldn't
        - Statuses will no longer show when you are sleeping
        - Adjusted sensitivity for bleeding
        # v1.0.6
        - Fixed null reference error for CanPickupEntity hook
        # v1.0.7
        - Fixed GetName error
         */

        public static SimpleStatus PLUGIN;

        [PluginReference]
        private readonly Plugin ImageLibrary;

        [PluginReference]
        private readonly Plugin CustomStatusFramework;

        private readonly bool Debugging = false; // If you are a developer, you can enable this to get console logs.

        #region Oxide Hooks

        private void Init()
        {
            if (Data == null) { Data = new SavedData(); }
            if (CachedUI == null) { CachedUI = new Dictionary<string, string>(); }
            LoadData();
        }

        private void OnServerInitialized()
        {
            PLUGIN = this;
            if (!ImageLibrary?.IsLoaded ?? true)
            {
                PrintError("ImageLibary is REQUIRED for this plugin to work properly. Please load it onto your server and reload this plugin.");
                return;
            }
            if (CustomStatusFramework?.IsLoaded ?? false)
            {
                PrintError("You have both Simple Status and Custom Status Framework installed. These plugins do the same thing and will conflict with each other. Please unload Custom Status Framework and reload this plugin.");
                return;
            }
            AddCovalenceCommand(config.ToggleStatusCommand, nameof(CmdToggleStatus));
            foreach (var player in BasePlayer.activePlayerList)
            {
                OnPlayerConnected(player);
            }
        }

        private void Unload()
        {
            SaveData();
            foreach (var pair in Behaviours)
            {
                foreach(var status in Data.Statuses.Keys)
                {
                    pair.Value.RemoveStatus(status);
                }
                UnityEngine.Object.Destroy(pair.Value);
            }
            CachedUI = null;
            Data = null;
        }

        private void OnPluginUnloaded(Plugin plugin)
        {
            var name = plugin?.Name;
            if (name == Name) { return; }
            if (Data.Statuses.Values.Any(x => x.PluginName == name))
            {
                RemovePluginData(name);
            }
        }

        private void RemovePluginData(string pluginName)
        {
            Debug($"Removing {pluginName} because its no longer loaded");
            var statuses = Data.Statuses.Where(x => x.Value.PluginName == pluginName).Select(x => x.Key).ToArray();
            foreach(var status in statuses)
            {
                Data.Statuses.Remove(status);
            }
            var userIdsToUpdate = new HashSet<ulong>();
            foreach(var playerData in Data.Player.ToArray())
            {
                var userId = playerData.Key;
                foreach(var key in playerData.Value.Keys.ToArray())
                {
                    if (statuses.Contains(key)) { Data.Player[userId].Remove(key); }
                }
                userIdsToUpdate.Add(userId);
            }
            foreach(var userId in userIdsToUpdate)
            {
                var behavior = Behaviours.GetValueOrDefault(userId); if (behavior == null) { continue; }
                behavior.rowsNeedUpdate = true;
            }
        }

        private void OnPlayerConnected(BasePlayer basePlayer)
        {
            var obj = basePlayer.gameObject.AddComponent<StatusBehaviour>();
            Behaviours.Add(basePlayer.userID, obj);
            Debug($"Connecting {basePlayer.displayName} has {(Data.Player.ContainsKey(basePlayer.userID) ? Data.Player.GetValueOrDefault(basePlayer.userID)?.Count : 0)} statuses: {Data.Player.GetValueOrDefault(basePlayer.userID)?.Keys.ToSentence()}");
            NextFrame(() =>
            {
                Debug($"Resuming statuses for {basePlayer.displayName}..");
                if (Data.Player.ContainsKey(basePlayer.userID))
                {
                    foreach (var data in Data.Player[basePlayer.userID])
                    {
                        var statusName = data.Key;
                        Debug($"Resuming {data.Key} {data.Value.Duration} {data.Value.Title} {data.Value.Text} {data.Value.EndTime.HasValue} {data.Value.DurationUntilEndTime}");
                        obj.SetStatus(data.Key, data.Value.Duration, !data.Value.EndTime.HasValue, true);
                    }
                }
                if (config.WarnPlayersThatStatusIsHidden && Data.PlayersHiding.Contains(basePlayer.userID))
                {
                    Message(basePlayer, Lang(PLUGIN, "warning", basePlayer.userID, config.ToggleStatusCommand));
                }
            });
        }

        private void OnPlayerDisconnected(BasePlayer basePlayer)
        {
            var obj = Behaviours[basePlayer.userID];
            UnityEngine.Object.Destroy(obj);
            Behaviours.Remove(basePlayer.userID);
            Debug($"Disconnecting {basePlayer.displayName} has {(Data.Player.ContainsKey(basePlayer.userID) ? Data.Player[basePlayer.userID].Count : 0)} statuses: {Data.Player.GetValueOrDefault(basePlayer.userID)?.Keys.ToSentence()}");
        }

        private void CanPickupEntity(BasePlayer basePlayer, BaseEntity entity)
        {
            if (basePlayer == null || entity == null) { return; }
            var name = entity?.name;
            NextTick(() =>
            {
                if (basePlayer != null && name != null)
                {
                    Behaviours.GetValueOrDefault(basePlayer.userID)?.itemStatuses.Inc(name);
                }
            });
        }

        private void OnItemPickup(Item item, BasePlayer basePlayer)
        {
            if (basePlayer == null || item == null) { return; }
            Behaviours.GetValueOrDefault(basePlayer.userID)?.itemStatuses.Inc(item.info.shortname);
        }

        private void OnStructureUpgrade(BaseCombatEntity entity, BasePlayer basePlayer, BuildingGrade.Enum grade)
        {
            if (basePlayer == null) { return; }
            Behaviours.GetValueOrDefault(basePlayer.userID)?.itemStatuses.Inc($"removed {grade}");
        }

        private void OnStructureRepair(BuildingBlock entity, BasePlayer basePlayer)
        {
            NextTick(() =>
            {
                if (entity != null && basePlayer != null)
                {
                    Behaviours.GetValueOrDefault(basePlayer.userID)?.itemStatuses.Inc($"removed {entity.grade}");
                }
            });
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BasePlayer basePlayer, Item item)
        {
            if (basePlayer == null || item == null) { return; }
            Behaviours.GetValueOrDefault(basePlayer.userID)?.itemStatuses.Inc(item.info.shortname);
        }

        private void OnCollectiblePickup(CollectibleEntity collectible, BasePlayer basePlayer)
        {
            if (basePlayer == null) { return; }
            collectible.itemList.ForEach(item =>
            {
                Behaviours.GetValueOrDefault(basePlayer.userID)?.itemStatuses.Inc(item.itemDef.shortname);
            });
        }
        #endregion

        #region Status Info
        protected class SavedData
        {
            public Dictionary<string, StatusInfo> Statuses = new Dictionary<string, StatusInfo>();
            public HashSet<ulong> PlayersHiding = new HashSet<ulong>();
            public Dictionary<ulong, Dictionary<string, PlayerStatusInfo>> Player = new Dictionary<ulong, Dictionary<string, PlayerStatusInfo>>();
        }

        protected class PlayerStatusInfo
        {
            public int Duration;
            public string Title;
            public string Text;
            public DateTime? EndTime = null;
            [JsonIgnore]
            public bool IsPastEndTime => EndTime.HasValue && EndTime.Value < DateTime.Now;
            [JsonIgnore]
            public int DurationUntilEndTime => !EndTime.HasValue ? Duration : (int)Math.Floor((EndTime.Value.Subtract(DateTime.Now)).TotalSeconds);
        }

        protected static SavedData Data = new SavedData();

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/data", Data);
        }

        private void LoadData()
        {
            Debug("Load data called");
            var data = Interface.Oxide.DataFileSystem.ReadObject<SavedData>($"{Name}/data") ?? new SavedData();
            if (data.Statuses == null)
            {
                data.Statuses = new Dictionary<string, StatusInfo>();
            }
            if (data.Player == null)
            {
                data.Player = new Dictionary<ulong, Dictionary<string, PlayerStatusInfo>>();
            }
            Data.Player = data.Player;
            if (Data.Statuses == null)
            {
                Data.Statuses = new Dictionary<string, StatusInfo>();
            }
            foreach(var status in data.Statuses.ToArray())
            {
                if (!status.Value.Plugin?.IsLoaded ?? true)
                {
                    RemovePluginData(status.Value.PluginName);
                }
                else if (!Data.Statuses.ContainsKey(status.Key))
                {
                    Debug("Assigned new status from load");
                    Data.Statuses[status.Key] = status.Value;
                }
            }
        }


        private static void Debug(string message)
        {
            if (PLUGIN == null || !PLUGIN.Debugging) { return; }
            PLUGIN?.Puts($"DEBUG: {message}");
        }

        protected class StatusInfo
        {
            [JsonIgnore]
            public Plugin Plugin => Interface.uMod.RootPluginManager.GetPlugin(PluginName);
            [JsonIgnore]
            public bool PluginIsLoaded => Plugin?.IsLoaded ?? false;
            public string PluginName;
            public string Id;
            public string Color;
            public string Title;
            public string TitleColor;
            public string Text = null;
            public string TextColor;
            public string ImageLibraryNameOrAssetPath;
            [JsonProperty("ImageLibraryIconId")]
            private string ImageLibraryIconId // old version
            {
                set { ImageLibraryNameOrAssetPath = value; }
            }
            public string IconColor;
            [JsonIgnore]
            public bool IsAssetImage => !string.IsNullOrEmpty(ImageLibraryNameOrAssetPath) && ImageLibraryNameOrAssetPath.StartsWith("assets/");
        }
        #endregion

        #region Utility
        private static void Message(BasePlayer basePlayer, string message)
        {
            var icon = PLUGIN.config.ChatMessageSteamId;
            ConsoleNetwork.SendClientCommand(basePlayer.Connection, "chat.add", 2, icon, message);
        }

        #endregion
    }
}

namespace Oxide.Plugins
{
    partial class SimpleStatus : CovalencePlugin
    {
        [HookMethod(nameof(CreateStatus))]
        private void CreateStatus(Plugin plugin, string statusId, string backgroundColor = "1 1 1 1", string title = "Text", string titleColor = "1 1 1 1", string text = null, string textColor = "1 1 1 1", string imageLibraryNameOrAssetPath = null, string imageColor = "1 1 1 1")
        {
            Debug("CreateStatus called");
            Data.Statuses[statusId] = new StatusInfo
            {
                PluginName = plugin.Name,
                Id = statusId,
                Color = backgroundColor,
                Title = title,
                TitleColor = titleColor,
                Text = text,
                TextColor = textColor,
                ImageLibraryNameOrAssetPath = imageLibraryNameOrAssetPath,
                IconColor = imageColor
            };
            CachedUI.Clear();
        }

        [HookMethod(nameof(SetStatus))]
        private void SetStatus(ulong userId, string statusId, int duration = int.MaxValue, bool pauseOffline = true)
        {
            Debug($"SetStatus called {userId} {statusId} {duration} {pauseOffline}");
            if (IsStatusIdInvalid(statusId)) { return; }
            var b = Behaviours.GetValueOrDefault(userId);
            if (b == null)
            {
                // save status for later if player is offline
                SetStatusForOfflinePlayer(userId, statusId, duration, pauseOffline);
                return;
            }
            if (duration > 0)
            {
                b.SetStatus(statusId, duration, pauseOffline);
            }
            else
            {
                b.RemoveStatus(statusId);
            }
        }

        [HookMethod(nameof(SetStatusTitle))]
        private void SetStatusTitle(ulong userId, string statusId, string title = null)
        {
            Debug("SetStatusTitle called");
            if (IsStatusIdInvalid(statusId)) { return; }
            var b = Behaviours.GetValueOrDefault(userId); if (b == null) { return; }
            b.SetStatusTitle(statusId, title);
        }

        [HookMethod(nameof(SetStatusText))]
        private void SetStatusText(ulong userId, string statusId, string text = null)
        {
            Debug("SetStatusText called");
            if (IsStatusIdInvalid(statusId)) { return; }
            var b = Behaviours.GetValueOrDefault(userId); if (b == null) { return; }
            b.SetStatusText(statusId, text);
        }

        [HookMethod(nameof(GetDuration))]
        private int GetDuration(ulong userId, string statusId)
        {
            Debug("GetDuration called");
            if (IsStatusIdInvalid(statusId)) { return 0; }
            if (!Data.Player.ContainsKey(userId) || !Data.Player[userId].ContainsKey(statusId)) { return 0; }
            return Data.Player[userId][statusId].Duration;
        }

        private bool IsStatusIdInvalid(string statusId)
        {
            if (Data?.Statuses?.ContainsKey(statusId) ?? true) { return false; }
            if (Debugging) { PrintError($"There is no status with the id of '{statusId}'"); }
            return true;
        }

        /* Subscribable Hooks */

        /*
         * # Called when a status is initially set for a player.
         * void OnStatusSet(ulong userId, string statusId, int duration)
         * 
         * 
         * # Called when a status is removed for a player. (When the duration reaches 0).
         * void OnStatusEnd(ulong userId, string statusId, int duration)
         * 
         * # Called when a status property is updated.
         * # The 'property' parameter can be: 'title', 'text'
         * void OnStatusUpdate(ulong userId, string statusId, string property, string value);
         */
    }
}

namespace Oxide.Plugins
{
    partial class SimpleStatus : CovalencePlugin
    {
        private void CmdToggleStatus(IPlayer player, string command, string[] args)
        {
            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) { return; }
            if (!Data.PlayersHiding.Contains(basePlayer.userID))
            {
                Data.PlayersHiding.Add(basePlayer.userID);
                CuiHelper.DestroyUi(basePlayer, UI_Base_ID);
                Message(basePlayer, Lang(PLUGIN, "hiding", basePlayer.userID));
            }
            else
            {
                Data.PlayersHiding.Remove(basePlayer.userID);
                var b = Behaviours.GetValueOrDefault(basePlayer.userID); if (b == null) { return; }
                b.InitStatusUI();
                b.rowsNeedUpdate = true;
                Message(basePlayer, Lang(PLUGIN, "showing", basePlayer.userID));
            }
        }
    }
}

namespace Oxide.Plugins
{
    partial class SimpleStatus : CovalencePlugin
    {
        private Configuration config;

        private partial class Configuration
        {
            public ulong ChatMessageSteamId = 0;
            public string ToggleStatusCommand = "ts";
            public bool WarnPlayersThatStatusIsHidden = true;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new Exception();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        protected override void LoadDefaultConfig() => config = new Configuration();
    }
}

namespace Oxide.Plugins.SimpleStatusExtensionMethods
{
    public static class ExtensionMethods
    {
        public static void RemoveAll<TKey, TValue>(this Dictionary<TKey, TValue> dict, Func<KeyValuePair<TKey, TValue>, bool> condition)
        {
            foreach (var cur in dict.Where(condition).ToList())
            {
                dict.Remove(cur.Key);
            }
        }

        public static void Inc<TKey>(this Dictionary<TKey, float> dict, TKey key)
        {
            dict[key] = Time.realtimeSinceStartup + 3.6f;
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T element in source) action(element);
        }
    }
}

namespace Oxide.Plugins
{
    partial class SimpleStatus : CovalencePlugin
    {
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["showing"] = "Showing statuses.",
                ["hiding"] = "Hiding statuses.",
                ["warning"] = "You have statuses hidden. Use the /{0} command to show them again."
            }, this);
        }

        private string Lang(Plugin plugin, string key, ulong userId, params object[] args) => string.Format(lang.GetMessage(key, plugin, userId.ToString()), args);
    }
}

namespace Oxide.Plugins
{
    partial class SimpleStatus : CovalencePlugin
    {
        private void SetStatusForOfflinePlayer(ulong userId, string statusId, int duration, bool pauseOffline)
        {
            if (duration <= 0)
            {
                var data = Data.Player.GetValueOrDefault(userId);
                if (data == null) { return; }
                Debug($"Remove status {statusId} for offline player {userId}");
                data.Remove(statusId);
                if (data.Count <= 0)
                {
                    Data.Player.Remove(userId);
                }
                return;
            }
            if (!Data.Player.ContainsKey(userId))
            {
                Data.Player[userId] = new Dictionary<string, PlayerStatusInfo>();
            }
            Debug($"Set status {statusId} for offline player {userId} duration {duration}");
            Data.Player[userId][statusId] = new PlayerStatusInfo() { Duration = duration, EndTime = pauseOffline ? null : (DateTime?)DateTime.Now.AddSeconds(duration) };
        }

        private Dictionary<ulong, StatusBehaviour> Behaviours = new Dictionary<ulong, StatusBehaviour>();
        public int BehaviorCount = 0;
        public class StatusBehaviour : MonoBehaviour
        {
            private BasePlayer basePlayer;
            private int smallModifiersCount;
            private int bigModifiersCount;
            private int previousModifiersCount = 0;
            public bool rowsNeedUpdate = false;
            public bool privForceUpdate = false;
            public Dictionary<string, float> itemStatuses = new Dictionary<string, float>();
            private float nextBigStatusUpdate;
            public int ModifiersCount => smallModifiersCount + bigModifiersCount;
            public string[] ActiveStatusIds => !Data.Player.ContainsKey(UserId) ? new string[] { } : Data.Player[UserId].Keys.ToArray();
            public ulong UserId => basePlayer.userID;
            private bool inBuildingPriv = false;
            private ulong lastPrivId = 0;

            #region Private

            private void Awake()
            {
                basePlayer = GetComponent<BasePlayer>();
                PLUGIN.BehaviorCount++;
            }
            private void OnDestroy()
            {
                CuiHelper.DestroyUi(basePlayer, UI_Base_ID);
                PLUGIN.BehaviorCount--;
            }

            private void StartWorking()
            {
                InitStatusUI();
                RepeatCheckModifiers();
                RepeatUpdateUI();
                nextBigStatusUpdate = 0;

                InvokeRepeating(nameof(RepeatCheckDurations), 1f, 1f);
                InvokeRepeating(nameof(RepeatUpdateUI), 0.2f, 0.2f);
                InvokeRepeating(nameof(RepeatCheckModifiers), 0.2f, 0.2f);
            }

            private void StopWorking()
            {
                CancelInvoke(nameof(RepeatCheckDurations));
                CancelInvoke(nameof(RepeatUpdateUI));
                CancelInvoke(nameof(RepeatCheckModifiers));
                CuiHelper.DestroyUi(basePlayer, UI_Base_ID);
            }

            public void InitStatusUI()
            {
                if (Data.PlayersHiding.Contains(UserId)) { return; }
                CuiHelper.DestroyUi(basePlayer, UI_Base_ID);
                CuiHelper.AddUi(basePlayer, UI_Base());
            }

            private void RepeatCheckDurations()
            {
                if (basePlayer.IsSleeping())
                {
                    rowsNeedUpdate = true;
                    return;
                }
                foreach (var statusId in ActiveStatusIds)
                {
                    var status = Data.Statuses.GetValueOrDefault(statusId); if (status == null) { continue; }
                    var data = Data.Player[UserId].GetValueOrDefault(statusId); if (data == null) { continue; }
                    var duration = data.Duration;
                    if (duration >= int.MaxValue) { continue; } // No need to update duration
                    data.Duration -= 1;
                    if (data.Duration <= 0 || data.IsPastEndTime)
                    {
                        if (data.IsPastEndTime)
                        {
                            Debug($"Current: {DateTime.Now.ToLongTimeString()} EndTime: {data.EndTime.Value.ToLongTimeString()}");
                        }
                        RemoveStatus(statusId);
                    }
                    else if (Data.Statuses[statusId].Text == null && data.Text == null && !Data.PlayersHiding.Contains(basePlayer.userID))
                    {
                        // Update duration text
                        CuiHelper.DestroyUi(basePlayer, string.Format(UI_Text_ID, statusId));
                        CuiHelper.AddUi(basePlayer, UI_StatusText(Data.Statuses[statusId], FormatDuration(data.Duration)));
                    }
                }
            }

            private void RepeatUpdateUI()
            {
                if (!rowsNeedUpdate && !privForceUpdate) { return; }
                if (Data.PlayersHiding.Contains(basePlayer.userID))
                {
                    rowsNeedUpdate = false;
                    privForceUpdate = false;
                    return;
                }
                if (basePlayer.IsSleeping())
                {
                    ActiveStatusIds.ForEach(statusId => CuiHelper.DestroyUi(basePlayer, string.Format(UI_Status_ID, statusId)));
                    return;
                }
                Debug($"RepeatUpdateUI {ActiveStatusIds.Length}");
                var index = ModifiersCount;
                foreach (var statusId in ActiveStatusIds)
                {
                    var status = Data.Statuses.GetValueOrDefault(statusId); if (status == null) { continue; }
                    if (!status.PluginIsLoaded)
                    {
                        RemoveStatus(statusId);
                        continue;
                    }
                    var data = Data.Player[UserId].GetValueOrDefault(statusId); if (data == null) { continue; }
                    var titleLocalized = data.Title != null ? data.Title : PLUGIN.Lang(status.Plugin, status.Title, UserId);
                    var textOrDurationLocalized = data.Text != null ? data.Text : status.Text != null ? PLUGIN.Lang(status.Plugin, status.Text, UserId) : data.Duration < int.MaxValue ? FormatDuration(data.Duration) : string.Empty;
                    CuiHelper.DestroyUi(basePlayer, string.Format(UI_Status_ID, statusId));
                    CuiHelper.AddUi(basePlayer, UI_Status(status, index, titleLocalized, textOrDurationLocalized));
                    index++;
                }
                rowsNeedUpdate = false;
                privForceUpdate = false;
                if (ActiveStatusIds.Length <= 0)
                {
                    StopWorking();
                }
            }

            private void RepeatCheckModifiers()
            {
                smallModifiersCount = 0;

                if (basePlayer.metabolism.bleeding.value >= 0.00001)
                {
                    smallModifiersCount++; // bleeding
                }
                if (basePlayer.metabolism.temperature.value < 5)
                {
                    smallModifiersCount++; // toocold
                }
                if (basePlayer.metabolism.temperature.value > 40)
                {
                    smallModifiersCount++; // toohot
                }
                if (basePlayer.currentComfort > 0)
                {
                    smallModifiersCount++; // comfort
                }
                if (basePlayer.metabolism.calories.value < 40)
                {
                    smallModifiersCount++; // starving
                }
                if (basePlayer.metabolism.hydration.value < 35)
                {
                    smallModifiersCount++; // dehydrated
                }
                if (basePlayer.metabolism.radiation_poison.value > 0)
                {
                    smallModifiersCount++; // radiation
                }
                if (basePlayer.metabolism.wetness.value >= 0.02)
                {
                    smallModifiersCount++; // wet
                }
                if (basePlayer.metabolism.oxygen.value < 1f)
                {
                    smallModifiersCount++; // drowning
                }
                if (basePlayer.currentCraftLevel > 0)
                {
                    smallModifiersCount++; // workbench
                }
                if (basePlayer.inventory.crafting.queue.Count > 0)
                {
                    smallModifiersCount++; // crafting
                }
                if (basePlayer.modifiers.ActiveModifierCoount > 0)
                {
                    smallModifiersCount++; // modifiers
                }
                if (basePlayer.HasPlayerFlag(BasePlayer.PlayerFlags.SafeZone))
                {
                    smallModifiersCount++; // safezone
                }
                if (basePlayer.isMounted && (basePlayer.GetMountedVehicle() != null || basePlayer.GetMounted() is RidableHorse))
                {
                    smallModifiersCount++; // mounted
                }

                // Other stats
                if (nextBigStatusUpdate < Time.realtimeSinceStartup)
                {
                    var stillInBuildingPriv = false;

                    bigModifiersCount = 0;
                    var priv = basePlayer.GetBuildingPrivilege();
                    if (priv != null && priv.IsAuthed(basePlayer))
                    {
                        bigModifiersCount++; // buildpriv authed
                        bigModifiersCount++; // upkeep
                        inBuildingPriv = true;
                        stillInBuildingPriv = true;
                    }
                    else if (priv != null && !priv.IsAuthed(basePlayer) && basePlayer.GetActiveItem()?.info.shortname == "hammer")
                    {
                        bigModifiersCount++; // buildpriv not authed
                        inBuildingPriv = true;
                        stillInBuildingPriv = true;
                    }
                    if (inBuildingPriv && !stillInBuildingPriv) // Raid Protection relies on this
                    {
                        Interface.CallHook("OnStatusEnd", UserId, "simplestatus.buildingpriv", 0);
                        inBuildingPriv = false;
                    }
                    if (priv == null || lastPrivId != priv.net.ID.Value)
                    {
                        lastPrivId = priv == null ? 0 : priv.net.ID.Value;
                        privForceUpdate = true;
                    }
                    nextBigStatusUpdate = Time.realtimeSinceStartup + 2;
                }
                itemStatuses.RemoveAll(x => x.Value < Time.realtimeSinceStartup);
                smallModifiersCount += itemStatuses.Count;
                if (ModifiersCount != previousModifiersCount)
                {
                    previousModifiersCount = ModifiersCount;
                    rowsNeedUpdate = true;
                }
            }

            private string FormatDuration(int duration)
            {
                var ts = TimeSpan.FromSeconds(duration);
                return ts.TotalDays >= 1 ? $"{Math.Floor(ts.TotalDays):0}d {ts.Hours}h {ts.Minutes}m" :
                    ts.TotalHours >= 1 ? $"{Math.Floor(ts.TotalHours):0}h {ts.Minutes}m" :
                    ts.TotalMinutes >= 1 ? $"{Math.Floor(ts.TotalMinutes):0}m" :
                    $"{ts.TotalSeconds:0}";
            }

            #endregion

            #region Public

            public void SetStatus(string statusId, int duration, bool pauseOffline, bool resuming = false)
            {
                if (!Data.Player.ContainsKey(UserId))
                {
                    Data.Player[UserId] = new Dictionary<string, PlayerStatusInfo>();
                }
                var data = Data.Player[UserId].GetValueOrDefault(statusId);
                if (data == null)
                {
                    Debug($"Set status new PauseOffline={pauseOffline}");
                    Data.Player[UserId][statusId] = new PlayerStatusInfo() { Duration = duration, EndTime = pauseOffline ? null : (DateTime?) DateTime.Now.AddSeconds(duration) };
                    Debug($"End time is {Data.Player[UserId][statusId].EndTime?.ToShortTimeString()}");
                    Interface.CallHook("OnStatusSet", UserId, statusId, duration);
                    rowsNeedUpdate = true;
                    if (!IsInvoking(nameof(RepeatUpdateUI))) { StartWorking(); }
                }
                else if (data != null && !data.IsPastEndTime)
                {
                    Debug($"Set status existing end time is {data.EndTime?.ToShortTimeString()}");
                    if (resuming)
                    {
                        data.Duration = data.DurationUntilEndTime;
                    }
                    else
                    {
                        data.Duration = duration;
                        data.EndTime = pauseOffline ? null : (DateTime?)DateTime.Now.AddSeconds(duration);
                    }
                    Interface.CallHook("OnStatusSet", basePlayer, statusId, duration);
                    rowsNeedUpdate = true;
                    if (!IsInvoking(nameof(RepeatUpdateUI))) { StartWorking(); }
                }
                else if (data != null && data.IsPastEndTime)
                {
                    Debug($"Cancelling {statusId} past end time");
                    RemoveStatus(statusId);
                }
                Debug($"Player {basePlayer.displayName} has {(Data.Player.ContainsKey(basePlayer.userID) ? Data.Player[basePlayer.userID].Count : 0)} statuses");
            }

            public void RemoveStatus(string statusId)
            {
                Debug($"Remove status invoked");
                if (Data?.Player.ContainsKey(UserId) ?? false)
                {
                    Debug($"Removing {statusId}");
                    var status = Data.Player[UserId].GetValueOrDefault(statusId); if (status == null) { return; }
                    Data.Player[UserId].Remove(statusId);
                    CuiHelper.DestroyUi(basePlayer, string.Format(UI_Status_ID, statusId));
                    rowsNeedUpdate = true;
                    Interface.CallHook("OnStatusEnd", UserId, statusId, status.Duration);
                    if (Data.Player[UserId].Count <= 0)
                    {
                        Debug($"All statuses removed");
                        Data.Player.Remove(UserId);
                        StopWorking();
                    }
                }
            }

            public void SetStatusTitle(string statusId, string title)
            {
                if (Data.Player.ContainsKey(UserId) && Data.Player[UserId].ContainsKey(statusId))
                {
                    var status = Data.Player[UserId][statusId];
                    if (status.Title != title)
                    {
                        Data.Player[UserId][statusId].Title = title;
                        Interface.CallHook("OnStatusUpdate", UserId, statusId, "title", title);
                        // Update
                        if (Data.PlayersHiding.Contains(basePlayer.userID)) { return; }
                        CuiHelper.DestroyUi(basePlayer, string.Format(UI_Title_ID, statusId));
                        CuiHelper.AddUi(basePlayer, UI_StatusTitle(Data.Statuses[statusId], title ?? string.Empty));
                    }
                }
            }

            public void SetStatusText(string statusId, string text)
            {
                if (Data.Player.ContainsKey(UserId) && Data.Player[UserId].ContainsKey(statusId))
                {
                    var data = Data.Player[UserId][statusId];
                    if (data.Text != text)
                    {
                        data.Text = text;
                        Interface.CallHook("OnStatusUpdate", UserId, statusId, "text", text);
                        // Update
                        if (Data.PlayersHiding.Contains(basePlayer.userID)) { return; }
                        CuiHelper.DestroyUi(basePlayer, string.Format(UI_Text_ID, statusId));
                        CuiHelper.AddUi(basePlayer, UI_StatusText(Data.Statuses[statusId], text ?? string.Empty));
                    }
                }
            }

            #endregion
        }
    }
}

namespace Oxide.Plugins
{
    partial class SimpleStatus : CovalencePlugin
    {
        protected static Dictionary<string, string> CachedUI = new Dictionary<string, string>();

        protected static readonly string UI_Base_ID = "ss";
        protected static readonly string UI_Status_ID = "ss.{0}";
        protected static readonly string UI_Content_ID = "ss.{0}.content";
        protected static readonly string UI_Title_ID = "ss.{0}.title";
        protected static readonly string UI_Text_ID = "ss.{0}.text";


        protected static class UI
        {
            public static int EntryH = 26;
            public static int EntryGap = 2;
            public static int Padding = 8;
            public static int ImageSize = 16;
            public static int ImageMargin = 5;
        }

        protected static string UI_Base()
        {
            if (!CachedUI.ContainsKey(UI_Base_ID))
            {
                var container = new CuiElementContainer();
                var offX = -16;
                var offY = 100;
                var w = 192;
                var eh = 26;
                var eg = 2;
                var numEntries = 12;
                var h = (eh + eg) * numEntries - offY;
                container.Add(new CuiElement
                {
                    Name = UI_Base_ID,
                    Parent = "Under",
                    Components =
                    {
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "1 0",
                            AnchorMax = "1 0",
                            OffsetMin = $"{offX-w} {offY}",
                            OffsetMax = $"{offX} {offY+h}"
                        }
                    }
                });
                CachedUI[UI_Base_ID] = container.ToJson();
            }
            return CachedUI[UI_Base_ID];
        }

        protected static string UI_Status(StatusInfo status, int index, string titleLocalized, string textLocalized)
        {
            var uiStatusId = string.Format(UI_Status_ID, status.Id);
            var bottom = index * (UI.EntryGap + UI.EntryH);
            var top = bottom + UI.EntryH;
            if (!CachedUI.ContainsKey(uiStatusId))
            {
                var uiContentId = string.Format(UI_Content_ID, status.Id);
                var uiText1Id = string.Format(UI_Title_ID, status.Id);
                var uiText2Id = string.Format(UI_Text_ID, status.Id);
                var container = new CuiElementContainer();
                container.Add(new CuiElement
                {
                    Parent = UI_Base_ID,
                    Name = uiStatusId,
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = status.Color,
                            Sprite = "assets/content/ui/ui.background.tile.psd",
                            Material = "assets/content/ui/namefontmaterial.mat"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 0",
                            OffsetMin = "0 {bottom}",
                            OffsetMax = "0 {top}"
                        }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = uiStatusId,
                    Name = uiContentId,
                    Components =
                    {
                        new CuiRectTransformComponent
                        {
                            OffsetMin = $"{UI.Padding+UI.ImageSize+UI.ImageMargin-3} {0}",
                            OffsetMax = $"{-UI.Padding} {0}"
                        }
                    }
                });
                var imageComponent = new CuiImageComponent
                {
                    Color = status.IconColor
                };
                if (status.IsAssetImage)
                {
                    imageComponent.Sprite = status.ImageLibraryNameOrAssetPath;
                }
                else
                {
                    imageComponent.Png = PLUGIN.ImageLibrary?.Call<string>("GetImage", $"{status.ImageLibraryNameOrAssetPath}");
                }
                container.Add(new CuiElement
                {
                    Parent = uiStatusId,
                    Components =
                    {
                        imageComponent,
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0.5",
                            AnchorMax = "0 0.5",
                            OffsetMin = $"{UI.Padding-4} {-UI.ImageSize/2}",
                            OffsetMax = $"{UI.Padding+UI.ImageSize-4} {UI.ImageSize/2}"
                        }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = uiContentId,
                    Name = uiText1Id,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = "{title}",
                            FontSize = 12,
                            Color = status.TitleColor,
                            Align = TextAnchor.MiddleLeft
                        }
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = uiContentId,
                    Name = uiText2Id,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = "{text}",
                            FontSize = 12,
                            Color = status.TextColor,
                            Align = TextAnchor.MiddleRight
                        }
                    }
                });
                CachedUI[uiStatusId] = container.ToJson();
            }
            return CachedUI[uiStatusId]
                .Replace("{title}", titleLocalized)
                .Replace("{text}", textLocalized)
                .Replace("{bottom}", bottom.ToString())
                .Replace("{top}", top.ToString());
        }

        protected static string UI_StatusTitle(StatusInfo status, string title)
        {
            var parentId = string.Format(UI_Content_ID, status.Id);
            var uiTitleId = string.Format(UI_Title_ID, status.Id);
            if (!CachedUI.ContainsKey(uiTitleId))
            {
                var container = new CuiElementContainer();
                container.Add(new CuiElement
                {
                    Parent = parentId,
                    Name = uiTitleId,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Color = status.TextColor,
                            Text = "{title}",
                            FontSize = 12,
                            Align = TextAnchor.MiddleLeft
                        }
                    }
                });
                CachedUI[uiTitleId] = container.ToJson();
            }
            return CachedUI[uiTitleId].Replace("{title}", title);
        }

        protected static string UI_StatusText(StatusInfo status, string text)
        {
            var parentId = string.Format(UI_Content_ID, status.Id);
            var uiTextId = string.Format(UI_Text_ID, status.Id);
            if (!CachedUI.ContainsKey(uiTextId))
            {
                var container = new CuiElementContainer();
                container.Add(new CuiElement
                {
                    Parent = parentId,
                    Name = uiTextId,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Color = status.TextColor,
                            Text = "{text}",
                            FontSize = 12,
                            Align = TextAnchor.MiddleRight
                        }
                    }
                });
                CachedUI[uiTextId] = container.ToJson();
            }
            return CachedUI[uiTextId].Replace("{text}", text);
        }
    }
}
