using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Carbon.Core;
using Carbon.Extensions;
using Carbon.Plugins;
using Facepunch;

namespace Oxide.Plugins
{
    [Info("AdminRadar", "nivex", "4.3.0")]
    [Description("Enhanced admin radar with improved performance and error handling")]
    public class AdminRadar : RustPlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Admin Permission")]
            public string AdminPermission { get; set; } = "adminradar.allowed";

            [JsonProperty("Radar Settings")]
            public RadarSettings Radar { get; set; } = new RadarSettings();

            [JsonProperty("Performance Settings")]
            public PerformanceSettings Performance { get; set; } = new PerformanceSettings();

            [JsonProperty("Display Settings")]
            public DisplaySettings Display { get; set; } = new DisplaySettings();
        }

        public class RadarSettings
        {
            [JsonProperty("Default Distance")]
            public float DefaultDistance { get; set; } = 500f;

            [JsonProperty("Max Distance")]
            public float MaxDistance { get; set; } = 1000f;

            [JsonProperty("Update Rate")]
            public float UpdateRate { get; set; } = 1f;

            [JsonProperty("Show Players")]
            public bool ShowPlayers { get; set; } = true;

            [JsonProperty("Show Animals")]
            public bool ShowAnimals { get; set; } = true;

            [JsonProperty("Show Vehicles")]
            public bool ShowVehicles { get; set; } = true;

            [JsonProperty("Show Structures")]
            public bool ShowStructures { get; set; } = false;
        }

        public class PerformanceSettings
        {
            [JsonProperty("Max Entities Per Update")]
            public int MaxEntitiesPerUpdate { get; set; } = 100;

            [JsonProperty("Enable Caching")]
            public bool EnableCaching { get; set; } = true;

            [JsonProperty("Cache Duration")]
            public float CacheDuration { get; set; } = 5f;
        }

        public class DisplaySettings
        {
            [JsonProperty("Player Color")]
            public string PlayerColor { get; set; } = "1 0 0 1";

            [JsonProperty("Animal Color")]
            public string AnimalColor { get; set; } = "0 1 0 1";

            [JsonProperty("Vehicle Color")]
            public string VehicleColor { get; set; } = "0 0 1 1";

            [JsonProperty("Structure Color")]
            public string StructureColor { get; set; } = "1 1 0 1";

            [JsonProperty("Show Distance")]
            public bool ShowDistance { get; set; } = true;

            [JsonProperty("Show Health")]
            public bool ShowHealth { get; set; } = true;
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
                ["NoPermission"] = "You don't have permission to use admin radar!",
                ["RadarEnabled"] = "Admin radar enabled - Distance: {0}m",
                ["RadarDisabled"] = "Admin radar disabled",
                ["RadarToggled"] = "Admin radar toggled",
                ["DistanceSet"] = "Radar distance set to {0}m",
                ["InvalidDistance"] = "Invalid distance. Must be between 50 and {0}",
                ["EntityInfo"] = "{0} - Distance: {1:F0}m - Health: {2:F0}/{3:F0}",
                ["PlayersFound"] = "Players found: {0}",
                ["AnimalsFound"] = "Animals found: {0}",
                ["VehiclesFound"] = "Vehicles found: {0}",
                ["StructuresFound"] = "Structures found: {0}"
            }, this);
        }

        private string GetMessage(string key, string playerId = null, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, playerId), args);
        }

        #endregion

        #region Fields

        private readonly Dictionary<ulong, RadarData> activeRadars = new Dictionary<ulong, RadarData>();
        private readonly Dictionary<ulong, Timer> radarTimers = new Dictionary<ulong, Timer>();
        private readonly Dictionary<string, CachedEntityData> entityCache = new Dictionary<string, CachedEntityData>();

        private class RadarData
        {
            public float Distance { get; set; }
            public bool IsActive { get; set; }
            public DateTime LastUpdate { get; set; }
            public Vector3 LastPosition { get; set; }
        }

        private class CachedEntityData
        {
            public List<BaseEntity> Entities { get; set; }
            public DateTime CacheTime { get; set; }
            public Vector3 Position { get; set; }
            public float Distance { get; set; }
        }

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(config.AdminPermission, this);
            AddCovalenceCommand("radar", "RadarCommand");
            AddCovalenceCommand("adminradar", "RadarCommand");

            Puts("AdminRadar Enhanced v2.0.0 initialized successfully");
        }

        private void Unload()
        {
            foreach (var timer in radarTimers.Values)
            {
                timer?.Destroy();
            }
            radarTimers.Clear();
            activeRadars.Clear();
            entityCache.Clear();
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null) return;
            CleanupPlayerData(player.userID);
        }

        #endregion

        #region Commands

        [Command("radar", "adminradar")]
        private void RadarCommand(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, config.AdminPermission))
            {
                player.Reply(GetMessage("NoPermission", player.Id));
                return;
            }

            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            if (args.Length == 0)
            {
                ToggleRadar(basePlayer);
            }
            else
            {
                HandleRadarCommand(basePlayer, args);
            }
        }

        #endregion

        #region Core Methods

        private void ToggleRadar(BasePlayer player)
        {
            if (activeRadars.TryGetValue(player.userID, out var radarData) && radarData.IsActive)
            {
                DisableRadar(player);
            }
            else
            {
                EnableRadar(player, config.Radar.DefaultDistance);
            }
        }

        private void EnableRadar(BasePlayer player, float distance)
        {
            if (player == null) return;

            distance = Mathf.Clamp(distance, 50f, config.Radar.MaxDistance);

            var radarData = new RadarData
            {
                Distance = distance,
                IsActive = true,
                LastUpdate = DateTime.Now,
                LastPosition = player.transform.position
            };

            activeRadars[player.userID] = radarData;

            if (radarTimers.TryGetValue(player.userID, out var existingTimer))
            {
                existingTimer?.Destroy();
            }

            radarTimers[player.userID] = timer.Repeat(config.Radar.UpdateRate, 0, () =>
            {
                if (player != null && player.IsConnected && activeRadars.ContainsKey(player.userID))
                {
                    UpdateRadar(player);
                }
                else
                {
                    CleanupPlayerData(player?.userID ?? 0);
                }
            });

            player.ChatMessage(GetMessage("RadarEnabled", player.UserIDString, distance));
        }

        private void DisableRadar(BasePlayer player)
        {
            if (player == null) return;

            CleanupPlayerData(player.userID);
            player.ChatMessage(GetMessage("RadarDisabled", player.UserIDString));
        }

        private void CleanupPlayerData(ulong playerId)
        {
            activeRadars.Remove(playerId);
            
            if (radarTimers.TryGetValue(playerId, out var timer))
            {
                timer?.Destroy();
                radarTimers.Remove(playerId);
            }
        }

        private void HandleRadarCommand(BasePlayer player, string[] args)
        {
            var subCommand = args[0].ToLower();

            switch (subCommand)
            {
                case "on":
                case "enable":
                    var distance = args.Length > 1 && float.TryParse(args[1], out var d) ? d : config.Radar.DefaultDistance;
                    EnableRadar(player, distance);
                    break;

                case "off":
                case "disable":
                    DisableRadar(player);
                    break;

                case "distance":
                case "dist":
                    if (args.Length > 1 && float.TryParse(args[1], out var newDistance))
                    {
                        SetRadarDistance(player, newDistance);
                    }
                    else
                    {
                        player.ChatMessage($"Usage: /{args[0]} distance <value>");
                    }
                    break;

                case "toggle":
                    ToggleRadar(player);
                    break;

                default:
                    ShowRadarHelp(player);
                    break;
            }
        }

        private void SetRadarDistance(BasePlayer player, float distance)
        {
            if (distance < 50f || distance > config.Radar.MaxDistance)
            {
                player.ChatMessage(GetMessage("InvalidDistance", player.UserIDString, config.Radar.MaxDistance));
                return;
            }

            if (activeRadars.TryGetValue(player.userID, out var radarData))
            {
                radarData.Distance = distance;
                player.ChatMessage(GetMessage("DistanceSet", player.UserIDString, distance));
            }
            else
            {
                EnableRadar(player, distance);
            }
        }

        private void ShowRadarHelp(BasePlayer player)
        {
            var help = "Admin Radar Commands:\n" +
                      "• /radar - Toggle radar on/off\n" +
                      "• /radar on [distance] - Enable radar\n" +
                      "• /radar off - Disable radar\n" +
                      "• /radar distance <value> - Set radar distance\n" +
                      "• /radar toggle - Toggle radar state";

            player.ChatMessage(help);
        }

        private void UpdateRadar(BasePlayer player)
        {
            if (!activeRadars.TryGetValue(player.userID, out var radarData))
                return;

            try
            {
                var entities = GetNearbyEntities(player, radarData.Distance);
                var radarInfo = ProcessEntities(player, entities, radarData.Distance);
                
                if (!string.IsNullOrEmpty(radarInfo))
                {
                    SendRadarUpdate(player, radarInfo);
                }

                radarData.LastUpdate = DateTime.Now;
                radarData.LastPosition = player.transform.position;
            }
            catch (Exception ex)
            {
                LogError($"Error updating radar for {player.displayName}: {ex.Message}");
            }
        }

        private List<BaseEntity> GetNearbyEntities(BasePlayer player, float distance)
        {
            var cacheKey = $"{player.userID}_{distance}_{(int)(player.transform.position.x / 10)}_{(int)(player.transform.position.z / 10)}";
            
            if (config.Performance.EnableCaching && 
                entityCache.TryGetValue(cacheKey, out var cached) &&
                (DateTime.Now - cached.CacheTime).TotalSeconds < config.Performance.CacheDuration)
            {
                return cached.Entities;
            }

            var entities = new List<BaseEntity>();
            var playerPos = player.transform.position;
            var processed = 0;

            foreach (var entity in BaseNetworkable.serverEntities)
            {
                if (processed >= config.Performance.MaxEntitiesPerUpdate)
                    break;

                if (entity is BaseEntity baseEntity && 
                    baseEntity != player && 
                    Vector3.Distance(playerPos, baseEntity.transform.position) <= distance)
                {
                    if (ShouldShowEntity(baseEntity))
                    {
                        entities.Add(baseEntity);
                    }
                }
                processed++;
            }

            if (config.Performance.EnableCaching)
            {
                entityCache[cacheKey] = new CachedEntityData
                {
                    Entities = entities,
                    CacheTime = DateTime.Now,
                    Position = playerPos,
                    Distance = distance
                };
            }

            return entities;
        }

        private bool ShouldShowEntity(BaseEntity entity)
        {
            if (entity is BasePlayer && config.Radar.ShowPlayers)
                return true;
            
            if (entity is BaseAnimalNPC && config.Radar.ShowAnimals)
                return true;
            
            if (entity is BaseVehicle && config.Radar.ShowVehicles)
                return true;
            
            if (entity is BuildingBlock && config.Radar.ShowStructures)
                return true;

            return false;
        }

        private string ProcessEntities(BasePlayer player, List<BaseEntity> entities, float maxDistance)
        {
            var players = new List<BaseEntity>();
            var animals = new List<BaseEntity>();
            var vehicles = new List<BaseEntity>();
            var structures = new List<BaseEntity>();

            foreach (var entity in entities)
            {
                if (entity is BasePlayer)
                    players.Add(entity);
                else if (entity is BaseAnimalNPC)
                    animals.Add(entity);
                else if (entity is BaseVehicle)
                    vehicles.Add(entity);
                else if (entity is BuildingBlock)
                    structures.Add(entity);
            }

            var info = new StringBuilder();
            
            if (players.Count > 0)
                info.AppendLine(GetMessage("PlayersFound", player.UserIDString, players.Count));
            
            if (animals.Count > 0)
                info.AppendLine(GetMessage("AnimalsFound", player.UserIDString, animals.Count));
            
            if (vehicles.Count > 0)
                info.AppendLine(GetMessage("VehiclesFound", player.UserIDString, vehicles.Count));
            
            if (structures.Count > 0)
                info.AppendLine(GetMessage("StructuresFound", player.UserIDString, structures.Count));

            return info.ToString().TrimEnd();
        }

        private void SendRadarUpdate(BasePlayer player, string info)
        {
            // Send as a subtle UI notification instead of chat spam
            player.SendConsoleCommand("echo", $"[Radar] {info}");
        }

        #endregion

        #region API

        private void ToggleRadarForPlayer(BasePlayer player)
        {
            if (player != null && permission.UserHasPermission(player.UserIDString, config.AdminPermission))
            {
                ToggleRadar(player);
            }
        }

        private bool IsRadarActive(BasePlayer player)
        {
            return player != null && 
                   activeRadars.TryGetValue(player.userID, out var data) && 
                   data.IsActive;
        }

        private void SetRadarDistanceForPlayer(BasePlayer player, float distance)
        {
            if (player != null && permission.UserHasPermission(player.UserIDString, config.AdminPermission))
            {
                SetRadarDistance(player, distance);
            }
        }

        private List<BaseEntity> GetRadarEntities(BasePlayer player)
        {
            if (player == null || !activeRadars.TryGetValue(player.userID, out var radarData))
                return new List<BaseEntity>();

            return GetNearbyEntities(player, radarData.Distance);
        }

        #endregion

        #region Cleanup

        private void OnServerSave()
        {
            // Clean up old cache entries
            var cutoff = DateTime.Now.AddSeconds(-config.Performance.CacheDuration * 2);
            var keysToRemove = entityCache
                .Where(kvp => kvp.Value.CacheTime < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                entityCache.Remove(key);
            }
        }

        #endregion
    }
}
