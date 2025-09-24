using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Carbon.Extensions;
using Carbon.Plugins;
using Facepunch;
using Network;

namespace Oxide.Plugins
{
    [Info("TurretLootSiphon", "WhiteThunder", "1.0.0")]
    [Description("Enhanced turret loot siphon with improved performance and error handling")]
    public class TurretLootSiphon : RustPlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Plugin Settings")]
            public PluginSettings Plugin { get; set; } = new PluginSettings();

            [JsonProperty("Siphon Settings")]
            public SiphonSettings Siphon { get; set; } = new SiphonSettings();

            [JsonProperty("Performance Settings")]
            public PerformanceSettings Performance { get; set; } = new PerformanceSettings();

            [JsonProperty("Debug Settings")]
            public DebugSettings Debug { get; set; } = new DebugSettings();
        }

        public class PluginSettings
        {
            [JsonProperty("Enabled")]
            public bool Enabled { get; set; } = true;

            [JsonProperty("Admin Permission")]
            public string AdminPermission { get; set; } = "turretlootsiphon.admin";

            [JsonProperty("Use Permission")]
            public string UsePermission { get; set; } = "turretlootsiphon.use";

            [JsonProperty("Require Turret Or Trap Killer")]
            public bool RequireTurretOrTrapKiller { get; set; } = true;
        }

        public class SiphonSettings
        {
            [JsonProperty("Kill Proximity From Turret")]
            public float KillProximityFromTurret { get; set; } = 12f;

            [JsonProperty("Box Search Radius")]
            public float BoxSearchRadius { get; set; } = 4f;

            [JsonProperty("Max Boxes Per Turret")]
            public int MaxBoxesPerTurret { get; set; } = 4;

            [JsonProperty("Allowed Box Short Prefabs")]
            public List<string> AllowedBoxShortPrefabs { get; set; } = new List<string>
            {
                "box.wooden.large",
                "box.wooden",
                "coffinstorage"
            };

            [JsonProperty("Skip Authorized Players")]
            public bool SkipAuthorizedPlayers { get; set; } = true;

            [JsonProperty("Siphon Timeout")]
            public float SiphonTimeout { get; set; } = 30f;
        }

        public class PerformanceSettings
        {
            [JsonProperty("Max Pending Siphons")]
            public int MaxPendingSiphons { get; set; } = 100;

            [JsonProperty("Cleanup Interval")]
            public float CleanupInterval { get; set; } = 60f;

            [JsonProperty("Enable Caching")]
            public bool EnableCaching { get; set; } = true;

            [JsonProperty("Cache Duration")]
            public float CacheDuration { get; set; } = 10f;
        }

        public class DebugSettings
        {
            [JsonProperty("Debug Log")]
            public bool DebugLog { get; set; } = false;

            [JsonProperty("Log Siphon Details")]
            public bool LogSiphonDetails { get; set; } = true;

            [JsonProperty("Log Performance")]
            public bool LogPerformance { get; set; } = false;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }
            }
            catch
            {
                LogWarning("Configuration file is corrupt, using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You don't have permission to use Turret Loot Siphon!",
                ["PluginEnabled"] = "Turret Loot Siphon enabled",
                ["PluginDisabled"] = "Turret Loot Siphon disabled",
                ["NoTurretFound"] = "No turret found within {0}m",
                ["NoBoxesFound"] = "Turret {0}: no boxes within {1}m",
                ["TurretBoxes"] = "Turret {0} boxes:\n - {1}",
                ["SiphonStarted"] = "Pending siphon for {0} near turret {1}",
                ["SiphonCompleted"] = "Siphoned {0}/{1} items from corpse {2} into {3} boxes near turret {4}",
                ["TurretDisappeared"] = "Nearest turret disappeared before siphon",
                ["CorpseTooFar"] = "Corpse too far from turret, aborting siphon",
                ["InvalidTarget"] = "Please look at a turret or storage container",
                ["ScanResults"] = "Scan Results:",
                ["ItemsMoved"] = "Items moved: {0}",
                ["TotalItems"] = "Total items: {0}",
                ["BoxesUsed"] = "Boxes used: {0}",
                ["Distance"] = "Distance: {0:F1}m",
                ["Status"] = "Status: {0}",
                ["Help"] = "Turret Loot Siphon Commands:\n/tls scan - Scan nearby turrets and boxes\n/tls toggle - Toggle plugin on/off (admin)\n/tls help - Show this help"
            }, this);
        }

        private string GetMessage(string key, string playerId = null, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, playerId), args);
        }

        #endregion

        #region Fields

        private readonly Dictionary<ulong, PendingSiphon> pendingSiphons = new Dictionary<ulong, PendingSiphon>();
        private readonly Dictionary<string, CachedTurretData> turretCache = new Dictionary<string, CachedTurretData>();
        private Timer cleanupTimer;

        private class PendingSiphon
        {
            public ulong UserId { get; set; }
            public Vector3 DeathPosition { get; set; }
            public NetworkableId TurretNetId { get; set; }
            public DateTime CreatedTime { get; set; }
            public string PlayerName { get; set; }
        }

        private class CachedTurretData
        {
            public AutoTurret Turret { get; set; }
            public List<StorageContainer> NearbyBoxes { get; set; }
            public DateTime CacheTime { get; set; }
        }

        private class SiphonResult
        {
            public int ItemsMoved { get; set; }
            public int TotalItems { get; set; }
            public int BoxesUsed { get; set; }
            public bool Success { get; set; }
        }

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(config.Plugin.AdminPermission, this);
            permission.RegisterPermission(config.Plugin.UsePermission, this);

            AddCovalenceCommand("tls", "TurretLootSiphonCommand");
            AddCovalenceCommand("turretlootsiphon", "TurretLootSiphonCommand");

            Puts("TurretLootSiphon Enhanced v2.0.0 initialized successfully");
        }

        private void OnServerInitialized()
        {
            if (config.Performance.CleanupInterval > 0)
            {
                cleanupTimer = timer.Every(config.Performance.CleanupInterval, CleanupExpiredData);
            }
        }

        private void Unload()
        {
            cleanupTimer?.Destroy();
            pendingSiphons.Clear();
            turretCache.Clear();
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null) return;
            pendingSiphons.Remove(player.userID);
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!config.Plugin.Enabled) return;

            var player = entity as BasePlayer;
            if (player == null || player.IsNpc) return;

            // Check if we've hit the pending siphon limit
            if (pendingSiphons.Count >= config.Performance.MaxPendingSiphons)
            {
                if (config.Debug.DebugLog)
                    LogWarning("Max pending siphons reached, skipping new siphon");
                return;
            }

            // Determine if death should trigger siphon
            bool killerIsTrap = info != null && IsValidKiller(info.Initiator);

            if (config.Plugin.RequireTurretOrTrapKiller && !killerIsTrap)
                return;

            // Find the nearest AutoTurret to victim
            var nearestTurret = FindNearestAutoTurret(player.transform.position, config.Siphon.KillProximityFromTurret);
            if (nearestTurret == null) return;

            // If victim is authorized on that turret, skip (don't steal your own/ally's items)
            if (config.Siphon.SkipAuthorizedPlayers && IsAuthorizedOnTurret(nearestTurret, player.userID))
                return;

            if (config.Debug.DebugLog)
                Puts(GetMessage("SiphonStarted", null, player.displayName, nearestTurret.net.ID.Value));

            pendingSiphons[player.userID] = new PendingSiphon
            {
                UserId = player.userID,
                DeathPosition = player.transform.position,
                TurretNetId = nearestTurret.net.ID,
                CreatedTime = DateTime.Now,
                PlayerName = player.displayName
            };
        }

        private void OnEntitySpawned(PlayerCorpse corpse)
        {
            if (!config.Plugin.Enabled || corpse == null) return;

            if (!pendingSiphons.TryGetValue(corpse.playerSteamID, out var pendingSiphon))
                return;

            NextTick(() => ProcessCorpseSiphon(corpse, pendingSiphon));
        }

        #endregion

        #region Commands

        [Command("tls", "turretlootsiphon")]
        private void TurretLootSiphonCommand(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, config.Plugin.UsePermission))
            {
                player.Reply(GetMessage("NoPermission", player.Id));
                return;
            }

            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            if (args.Length > 0)
            {
                HandleTurretLootSiphonCommand(basePlayer, args);
            }
            else
            {
                ShowHelp(basePlayer);
            }
        }

        #endregion

        #region Core Methods

        private void HandleTurretLootSiphonCommand(BasePlayer player, string[] args)
        {
            var subCommand = args[0].ToLower();

            switch (subCommand)
            {
                case "scan":
                    ScanNearbyTurrets(player);
                    break;
                case "toggle":
                    if (permission.UserHasPermission(player.UserIDString, config.Plugin.AdminPermission))
                        TogglePlugin(player);
                    else
                        player.ChatMessage(GetMessage("NoPermission", player.UserIDString));
                    break;
                case "help":
                    ShowHelp(player);
                    break;
                default:
                    ShowHelp(player);
                    break;
            }
        }

        private void ScanNearbyTurrets(BasePlayer player)
        {
            var turret = FindNearestAutoTurret(player.transform.position, 30f);
            if (turret == null)
            {
                player.ChatMessage(GetMessage("NoTurretFound", player.UserIDString, "30"));
                return;
            }

            var boxes = FindNearbyBoxes(turret.transform.position, config.Siphon.BoxSearchRadius, config.Siphon.MaxBoxesPerTurret);
            if (boxes.Count == 0)
            {
                player.ChatMessage(GetMessage("NoBoxesFound", player.UserIDString, turret.net.ID.Value, config.Siphon.BoxSearchRadius));
                return;
            }

            var boxInfo = boxes.Select(b => $"{b.ShortPrefabName} @ {Vector3.Distance(turret.transform.position, b.transform.position):F1}m");
            player.ChatMessage(GetMessage("TurretBoxes", player.UserIDString, turret.net.ID.Value, string.Join("\n - ", boxInfo)));
        }

        private void TogglePlugin(BasePlayer player)
        {
            config.Plugin.Enabled = !config.Plugin.Enabled;
            SaveConfig();

            var message = config.Plugin.Enabled ? "PluginEnabled" : "PluginDisabled";
            player.ChatMessage(GetMessage(message, player.UserIDString));
        }

        private void ShowHelp(BasePlayer player)
        {
            player.ChatMessage(GetMessage("Help", player.UserIDString));
        }

        private void ProcessCorpseSiphon(PlayerCorpse corpse, PendingSiphon pendingSiphon)
        {
            try
            {
                // Sanity check distance (corpse can ragdoll slightly)
                var turret = BaseNetworkable.serverEntities.Find(pendingSiphon.TurretNetId) as AutoTurret;
                if (turret == null)
                {
                    if (config.Debug.DebugLog)
                        PrintWarning(GetMessage("TurretDisappeared"));
                    pendingSiphons.Remove(corpse.playerSteamID);
                    return;
                }

                if (Vector3.Distance(turret.transform.position, corpse.transform.position) > config.Siphon.KillProximityFromTurret + 3f)
                {
                    if (config.Debug.DebugLog)
                        PrintWarning(GetMessage("CorpseTooFar"));
                    pendingSiphons.Remove(corpse.playerSteamID);
                    return;
                }

                // Get destination boxes
                var destBoxes = GetCachedNearbyBoxes(turret);
                if (destBoxes.Count == 0)
                {
                    if (config.Debug.DebugLog)
                        PrintWarning(GetMessage("NoBoxesFound", null, turret.net.ID.Value, config.Siphon.BoxSearchRadius));
                    pendingSiphons.Remove(corpse.playerSteamID);
                    return;
                }

                // Perform the siphon
                var result = PerformSiphon(corpse, destBoxes);

                if (config.Debug.LogSiphonDetails)
                {
                    Puts(GetMessage("SiphonCompleted", null, result.ItemsMoved, result.TotalItems, 
                    pendingSiphon.PlayerName, result.BoxesUsed, turret.net.ID.Value));
                }

                pendingSiphons.Remove(corpse.playerSteamID);
            }
            catch (Exception ex)
            {
                PrintError($"Error processing corpse siphon: {ex.Message}");
                pendingSiphons.Remove(corpse.playerSteamID);
            }
        }

        private SiphonResult PerformSiphon(PlayerCorpse corpse, List<StorageContainer> destBoxes)
        {
            var result = new SiphonResult();
            var destContainers = destBoxes.Select(b => b.inventory).Where(inv => inv != null).ToList();
            var usedBoxes = new HashSet<BaseEntity>();

            // Process all containers (main, wear, belt)
            foreach (var container in corpse.containers)
            {
                if (container?.itemList == null) continue;

                // Create a copy to avoid modification during iteration
                var items = container.itemList.ToList();
                foreach (var item in items)
                {
                    if (item == null) continue;

                    result.TotalItems += item.amount;

                    if (TryMoveItemToAny(item, destContainers, usedBoxes))
                    {
                        result.ItemsMoved += item.amount;
                    }
                }
            }

            result.BoxesUsed = usedBoxes.Count;
            result.Success = result.ItemsMoved > 0;

            return result;
        }

        private bool TryMoveItemToAny(Item item, List<ItemContainer> destinations, HashSet<BaseEntity> usedBoxes)
        {
            foreach (var dest in destinations)
            {
                if (dest?.parent == null || dest.IsFull()) continue;

                var storageContainer = dest.entityOwner as BaseEntity;
                if (storageContainer == null) continue;

                // Try to move the item
                if (item.MoveToContainer(dest, -1, true))
                {
                    usedBoxes.Add(storageContainer);
                    return true;
                }
            }
            return false;
        }

        private List<StorageContainer> GetCachedNearbyBoxes(AutoTurret turret)
        {
            var cacheKey = $"{turret.net.ID.Value}_{(int)turret.transform.position.x}_{(int)turret.transform.position.z}";

            if (config.Performance.EnableCaching && 
                turretCache.TryGetValue(cacheKey, out var cached) &&
                (DateTime.Now - cached.CacheTime).TotalSeconds < config.Performance.CacheDuration)
            {
                return cached.NearbyBoxes ?? new List<StorageContainer>();
            }

            var boxes = FindNearbyBoxes(turret.transform.position, config.Siphon.BoxSearchRadius, config.Siphon.MaxBoxesPerTurret);

            if (config.Performance.EnableCaching)
            {
                turretCache[cacheKey] = new CachedTurretData
                {
                    Turret = turret,
                    NearbyBoxes = boxes,
                    CacheTime = DateTime.Now
                };
            }

            return boxes;
        }

        private void CleanupExpiredData()
        {
            var now = DateTime.Now;
            var expiredSiphons = pendingSiphons.Where(kvp => 
                (now - kvp.Value.CreatedTime).TotalSeconds > config.Siphon.SiphonTimeout).ToList();

            foreach (var expired in expiredSiphons)
            {
                pendingSiphons.Remove(expired.Key);
            }

            if (config.Performance.EnableCaching)
            {
                var expiredCache = turretCache.Where(kvp => 
                    (now - kvp.Value.CacheTime).TotalSeconds > config.Performance.CacheDuration).ToList();

                foreach (var expired in expiredCache)
                {
                    turretCache.Remove(expired.Key);
                }
            }

            if (config.Debug.LogPerformance && (expiredSiphons.Count > 0 || turretCache.Count > 0))
            {
                Puts($"Cleanup: Removed {expiredSiphons.Count} expired siphons, cache size: {turretCache.Count}");
            }
        }

        #endregion

        #region Utility Methods

        private bool IsValidKiller(BaseEntity killer)
        {
            return killer is AutoTurret || killer is GunTrap || killer is FlameTurret;
        }

        private AutoTurret FindNearestAutoTurret(Vector3 position, float maxDistance)
        {
            var turrets = new List<AutoTurret>();
            Vis.Entities(position, maxDistance, turrets);

            AutoTurret nearest = null;
            var bestDistance = float.MaxValue;

            foreach (var turret in turrets)
            {
                if (turret?.IsDestroyed != false) continue;

                var distance = Vector3.Distance(position, turret.transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = turret;
                }
            }

            return nearest;
        }

        private bool IsAuthorizedOnTurret(AutoTurret turret, ulong userId)
        {
            if (turret?.authorizedPlayers == null) return false;

            foreach (var authorizedPlayer in turret.authorizedPlayers)
            {
                if (authorizedPlayer?.userid == userId) return true;
            }

            return false;
        }

        private List<StorageContainer> FindNearbyBoxes(Vector3 center, float radius, int maxCount)
        {
            var allContainers = new List<StorageContainer>();
            Vis.Entities(center, radius, allContainers);

            var validBoxes = new List<StorageContainer>();

            foreach (var container in allContainers)
            {
                if (container?.IsDestroyed != false) continue;

                var shortName = container.ShortPrefabName;
                if (config.Siphon.AllowedBoxShortPrefabs?.Count > 0)
                {
                    if (!config.Siphon.AllowedBoxShortPrefabs.Contains(shortName))
                        continue;
                }

                validBoxes.Add(container);
                if (validBoxes.Count >= maxCount) break;
            }

            // Sort by distance and return closest boxes
            return validBoxes
                .OrderBy(box => Vector3.Distance(center, box.transform.position))
                .Take(maxCount)
                .ToList();
        }

        #endregion

        #region API

        private bool IsSiphonEnabled()
        {
            return config.Plugin.Enabled;
        }

        private int GetPendingSiphonCount()
        {
            return pendingSiphons.Count;
        }

        private List<StorageContainer> GetNearbyBoxesForTurret(AutoTurret turret)
        {
            if (turret == null) return new List<StorageContainer>();
            return GetCachedNearbyBoxes(turret);
        }

        #endregion
    }
}
