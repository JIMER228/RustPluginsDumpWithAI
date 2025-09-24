using System;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RaidMe", "Captain Blake", "1.0.1")]
    [Description("Allows players to create a temporary PvP zone around their Tool Cupboard (TC) using the /raidme command. " +
                 "Admins have additional commands to list, remove, or wipe all PvP zones. " +
                 "Features configurable zone radius, customizable messages, and automatic cleanup of invalid zones.")]
    public class RaidMe : RustPlugin
    {
        #region Variables and Constants

        [PluginReference] Plugin ZoneManager, TruePVE;
        
        private const string PermissionUse = "raidme.use";
        
        private const string PermissionAdmin = "raidme.admin";
        
        private const string DataFileName = "RaidMe";

        private const float TcSearchRadius = 5f;
        
        private Configuration _config;
        
        private StoredData _storedData;
        
        private Dictionary <ulong, NetworkableId> _raidMeZones = new Dictionary<ulong, NetworkableId>();
        
        private Dictionary<NetworkableId, NetworkableId> _mapMarkers = new Dictionary<NetworkableId, NetworkableId>();
        
        private Dictionary<ulong, Timer> _zoneTimers = new Dictionary<ulong, Timer>();
        
        private static RaidMe Instance { get; set; }
        
        #endregion

        #region Init
        
        void Init()
        {
            Instance = this;
            
            permission.RegisterPermission(PermissionUse, this);
            permission.RegisterPermission(PermissionAdmin,this);
            
            cmd.AddChatCommand("raidme", this, nameof(RaidMeCommand));
            cmd.AddChatCommand("raidmeadmin", this, nameof(AdminCommand));
            
            LoadConfig();
            LoadData();
        }
        
        void OnServerInitialized()
        {
            RemoveInvalidZones();
            PlaceMapMarkers();
            Puts("Initialization complete.");
        }
        
        #endregion
        
        #region Command
        
        private void RaidMeCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionUse))
            {
                SendReply(player, "You do not have permission to use this command.");
                return;
            }

            if (args.Length != 1)
            {
                SendReply(player, "Unknown command. Use /raidme help.");
                return;
            }

            switch (args[0].ToLower())
            {
                case "start":
                    if (TryGetValidTc(player, out var playerTc))
                        CreateRaidMeZone(player, playerTc);
                    break;

                case "remove":
                    RemoveRaidMeZone(player);
                    break;
                
                case "cancel":
                    CancelRemoveTimer(player);
                    break;
                
                case "help":
                    SendReply(player, "/raidme start: Creates a PvP zone around a designated TC.");
                    SendReply(player, "/raidme remove: Starts 180s timer to remove PvP zone.");
                    SendReply(player, "/raidme cancel: Cancels the removal timer.");
                    SendReply(player, "PvP zone gets removed when the TC gets destroyed.");
                    SendReply(player, "You can only have 1 PvP zone active at a time.");
                    SendReply(player, "Leaving the zone, taking or dealing damage cancels removal.");
                    SendReply(player, "Buildings in the PvP zone can be destroyed.");
                    break;

                default:
                    SendReply(player, "Unknown command. Use /raidme help.");
                    break;
            }
        }
        
        private void AdminCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin))
            {
                SendReply(player, "You do not have permission to use this command.");
                return;
            }

            if (args.Length != 1)
            {
                SendReply(player, "Unknown command. Use /raidmeadmin help.");
                return;
            }

            switch (args[0].ToLower())
            {
                case "list":
                    SendReply(player, $"List of PvP zones:\n {string.Join(", ", _raidMeZones.Keys)}");
                    break;

                case "remove":
                    FindAndRemoveZone(player);
                    break;

                case "wipe":
                    WipeAllZones(player);
                    break;

                case "help":
                    SendReply(player, "List of available commands:\n" +
                                      "/raidmeadmin list - List all PvP zones.\n" +
                                      "/raidmeadmin remove - Remove the PvP zone in which you are standing.\n" +
                                      "/raidmeadmin wipe - Remove all PvP zones.\n" +
                                      "/raidmeadmin help - Display this help message.");
                    break;

                default:
                    SendReply(player, "Unknown command. Use /raidmeadmin help.");
                    break;
            }
        }
        
        #endregion
        
        #region Functions
        
        private void CreateRaidMeZone(BasePlayer player, BuildingPrivlidge tc)
        {
            var id = $"RaidMe_{player.userID}";
            
            var radius = _config.RaidZoneBaseRadius * _config.RaidZoneRadiusMultiplier;
            
            if (radius > _config.RaidZoneMaxRadius)
                radius = _config.RaidZoneMaxRadius;
            if (radius < _config.RaidZoneMinRadius)
                radius = _config.RaidZoneMinRadius;
            
            var messages = new string[8];
            messages[0] = "name";
            messages[1] = $"RaidMeZone_{player.userID}";
            messages[2] = "enter_message";
            messages[3] = "You have entered a PvP zone.";
            messages[4] = "leave_message";
            messages[5] = "You have left the PvP zone.";
            messages[6] = "radius";
            messages[7] = Convert.ToInt32(radius).ToString();
            
            ZoneManager?.CallHook("CreateOrUpdateZone", id, messages, tc.transform.position);
            TruePVE?.CallHook("AddOrUpdateMapping", id, "exclude");
            
            _raidMeZones[player.userID] = tc.net.ID;

            CreateMapMarker(tc);
            
            SaveData();
            
            SendReply(player, "PvP zone created around your TC.");
        }

        private void RemoveRaidMeZone(BasePlayer player)
        {
            if (_raidMeZones.ContainsKey(player.userID))
            {
                // Check if the player is in the zone
                var isInZone = (bool)(ZoneManager?.CallHook("IsPlayerInZone", $"RaidMe_{player.userID}", player) ?? false);
                if (!isInZone)
                {
                    SendReply(player, "You are not in the PvP zone. You cannot remove it.");
                    return;
                }

                // Cancel the existing timer for this zone if there is one
                if (_zoneTimers.TryGetValue(player.userID, out var existingTimer))
                {
                    existingTimer.Destroy();
                    _zoneTimers.Remove(player.userID);
                    
                }

                // Start a new timer for this zone
                var newTimer = timer.Once(_config.ZoneDeactivationDelay, () => 
                {
                    RemoveZone(player.userID);
                    SendReply(player, "PvP zone removed.");
                });
                _zoneTimers[player.userID] = newTimer;

                SendReply(player, $"PvP zone will be removed from your TC after {_config.ZoneDeactivationDelay} seconds.");
            }
            else
            {
                SendReply(player, "You do not have a PvP zone around a TC.");
            }
        }
        
        private void FindAndRemoveZone(BasePlayer admin)
        {
            foreach (var (zoneOwner, _) in _raidMeZones)
            {
                // get the player id
                var isInZone = (bool)(ZoneManager?.CallHook("IsPlayerInZone", $"RaidMe_{zoneOwner}", admin) ?? false);
                Puts($"Player is in zone: {isInZone}");
                if (!isInZone) continue;
                // Remove the zone
                Puts($"Removing zone with key {zoneOwner}");
                RemoveZone(zoneOwner);
                //remove timer if it exists
                if (_zoneTimers.TryGetValue(zoneOwner, out var existingTimer))
                {
                    existingTimer.Destroy();
                    _zoneTimers.Remove(zoneOwner);
                }
                SendReply(admin, "PvP zone removed.");
                return;
            }
            Puts("No matching zone found for player.");
        }
        
        private void CancelRemoveTimer(BasePlayer player)
        {
            if (_zoneTimers.TryGetValue(player.userID, out var existingTimer))
            {
                existingTimer.Destroy();
                _zoneTimers.Remove(player.userID);
                SendReply(player, "Removal timer cancelled.");
            }
            else
            {
                SendReply(player, "You do not have a removal timer active.");
            }
        }
        
        private void WipeAllZones(BasePlayer player)
        {
            var zonesToRemove = _raidMeZones.Keys.ToList();
            foreach (var zoneOwner in zonesToRemove)
            {
                RemoveZone(zoneOwner);
            }
            SendReply(player, "All PvP zones removed.");
        }
        
        #endregion

        #region Helpers
        
        private bool TryGetValidTc(BasePlayer player, out BuildingPrivlidge playerTc)
        {
            playerTc = FindValidTc(player);
            if (playerTc != null) return true;
            Puts($"Player {player.displayName} ({player.UserIDString}) tried to create a zone but no valid TC found.");
            return false;
        }
        
        private BuildingPrivlidge FindValidTc(BasePlayer player)
        {
            var playerID = player.userID;
            var layerMask = LayerMask.GetMask("Deployed");
            // Check if the player is near a TC (within 5 meters)
            var buildingPrivileges = new List<BuildingPrivlidge>();
            Vis.Entities(player.transform.position, TcSearchRadius, buildingPrivileges, layerMask);
            var foundTc = buildingPrivileges.FirstOrDefault(tc => tc.IsAuthed(playerID));
            if (foundTc == null || foundTc.OwnerID != playerID)
            {
                SendReply(player, "You do not own a TC in this area.");
                return null;
            }
            // Check if there are other TCs within the exclusion radius
            var exclusionZoneTcs = new List<BuildingPrivlidge>();

            var exclusionZoneRadius = _config.RaidZoneExclusionBaseRadius * _config.RaidZoneExclusionRadiusMultiplier;
            // Check if the radius is within the limits
            if (exclusionZoneRadius > _config.RaidZoneExclusionMaxRadius)
                exclusionZoneRadius = _config.RaidZoneExclusionMaxRadius;
            if (exclusionZoneRadius < _config.RaidZoneExclusionMinRadius)
                exclusionZoneRadius = _config.RaidZoneExclusionMinRadius;
            
            Vis.Entities(foundTc.transform.position, exclusionZoneRadius, exclusionZoneTcs, layerMask);
            if (exclusionZoneTcs.Any(tc => !tc.IsAuthed(playerID)))
            {
                SendReply(player, "There are other TCs too close to your base that you are not authorized on. You cannot create a PvP zone here.");
                return null;
            }
            // Check if the TC is already registered
            if (_raidMeZones.ContainsKey(playerID))
            {
                SendReply(player, "You already have a PvP zone around a TC. Remove it first with /raidme clear.");
                return null;
            }

            return foundTc;
        }
        
        private void RemoveZone(ulong tcOwner)
        {
            // Remove the zone
            ZoneManager?.CallHook("EraseZone", $"RaidMe_{tcOwner}");
            TruePVE?.CallHook("RemoveMapping", $"RaidMe_{tcOwner}");
            
            //get the tc by network id
            var tcNetId = BaseNetworkable.serverEntities.Find(_raidMeZones[tcOwner]).net.ID;
            
            _raidMeZones.Remove(tcOwner);
            
            // Remove the marker
            if (_mapMarkers.TryGetValue(tcNetId, out var marker))
            {
                BaseNetworkable.serverEntities.Find(marker)?.Kill();
                _mapMarkers.Remove(tcNetId);
            }
            SaveData();
        }
        
        private void CreateMapMarker(BuildingPrivlidge tc)
        {
            var tcPosition = tc.transform.position;
           
            var markerColor = ColorUtility.TryParseHtmlString(_config.MarkerColor, out var color) ? color : Color.red;
            var markerOutlineColor = Color.black; 
            var marker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", tcPosition).GetComponent<MapMarkerGenericRadius>();
            marker.alpha = _config.MarkerAplha;
            marker.color1 = new Color(markerColor.r, markerColor.g, markerColor.b, 1.0f);
            marker.color2 = markerOutlineColor;
            marker.radius = _config.MarkerSize;
            marker.enabled = true;
            marker.Spawn();
            marker.SendUpdate();
            
            _mapMarkers[tc.net.ID] = marker.net.ID;
        }
        
        private void RemoveInvalidZones()
        {
            if (_raidMeZones == null) return;
            var invalidZones = _raidMeZones.Where(kvp => BaseNetworkable.serverEntities.Find(kvp.Value) == null).ToList();
            foreach (var (playerId, _) in invalidZones)
            {
                RemoveZone(playerId);
            }
            
            var zoneManagerDataBase = ZoneManager?.CallHook("GetZoneIDs");
            if (!(zoneManagerDataBase is string[] zoneIds)) return;

            var invalidRaidMeZones = zoneIds
                .Where(zoneId => zoneId.StartsWith("RaidMe_") && (!ulong.TryParse(zoneId.Replace("RaidMe_", ""), out var playerId) || !_raidMeZones.ContainsKey(playerId)))
                .ToList();
            
            foreach (var zoneId in invalidRaidMeZones.Where(zoneId => !string.IsNullOrEmpty(zoneId)))
            {
                ZoneManager?.CallHook("EraseZone", zoneId);
                TruePVE?.CallHook("RemoveMapping", zoneId);
            }
        }
        
        private void PlaceMapMarkers()
        {
            var tcIds = _mapMarkers.Select(pair => pair.Key).ToList();
            foreach (var tcId in tcIds)
            {
                var tc = BaseNetworkable.serverEntities.Find(tcId) as BuildingPrivlidge;
                if (!tc)
                {
                    _mapMarkers.Remove(tcId);
                    continue;
                }
                CreateMapMarker(tc);
            }
        }
        
        private void RemoveGlobalMarkers()
        {
            var globalMarkers = new List<MapMarkerGenericRadius>();
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                if (entity is MapMarkerGenericRadius marker)
                {
                    globalMarkers.Add(marker);
                }
            }
            if (globalMarkers.Count == 0) return;
            foreach (var marker in globalMarkers.Where(marker => marker != null)) 
            { 
                marker.Kill();
            }
        }
        
        #endregion
        
        #region Hooks
        
        void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            if (_zoneTimers.TryGetValue(attacker.userID, out var attackerTimer))
            {
                attackerTimer.Destroy();
                _zoneTimers.Remove(attacker.userID);
                SendReply(attacker, "You attacked something. The removal timer has been cancelled.");
                return;
            }
            var target = info.Initiator as BasePlayer;
            if (target != null && _zoneTimers.TryGetValue(target.userID, out var targetTimer))
            {
                targetTimer.Destroy();
                _zoneTimers.Remove(target.userID);
                SendReply(target, "You were attacked by a player. The removal timer has been cancelled.");
                return;
            }
        }
        
        void OnExitZone(string zoneID, BasePlayer player) 
        {
            if (!_zoneTimers.TryGetValue(player.userID, out var existingTimer)) return;
            existingTimer.Destroy();
            _zoneTimers.Remove(player.userID);
            SendReply(player, "You left your PvP zone. The removal timer has been cancelled.");
        }
        
        void OnEntityKill(BaseNetworkable entity)
        {
            if (!(entity is BuildingPrivlidge)) return;
            if (!_raidMeZones.Values.Contains(entity.net.ID)) return;
            var tcOwner = _raidMeZones.First(kvp => kvp.Value == entity.net.ID).Key;
            RemoveZone(tcOwner);
        }
        
        void OnServerSave()
        {
            SaveData();
        }
        
        void Unload()
        {
            foreach (var localTimer in _zoneTimers.Values)
            {
                localTimer.Destroy();
            }
            RemoveGlobalMarkers();
            SaveData();
            Puts("Plugin unloaded.");
        }
        
        #endregion
        
        #region Config
        
        private class Configuration
        {
            // The base radius for the PvP zone.
            public float RaidZoneBaseRadius { get; set; }

            // The minimum radius for the PvP zone.
            public float RaidZoneMinRadius { get; set; }

            // The maximum radius for the PvP zone.
            public float RaidZoneMaxRadius { get; set; }

            // The multiplier for the PvP zone radius.
            public float RaidZoneRadiusMultiplier { get; set; }
            
            // The radius for the exclusion zone.
            public float RaidZoneExclusionBaseRadius { get; set; }
            
            // The minimum radius for the exclusion zone.
            public float RaidZoneExclusionMinRadius { get; set; }
            
            // The maximum radius for the exclusion zone.
            public float RaidZoneExclusionMaxRadius { get; set; }

            // The multiplier for the exclusion zone radius.
            public float RaidZoneExclusionRadiusMultiplier { get; set; }

            // The delay before the PvP zone is deactivated.
            public float ZoneDeactivationDelay { get; set; }
            
            // The color of the marker.
            public string MarkerColor { get; set; }
            
            // The alpha of the marker.
            public float MarkerAplha { get; set; }
            
            // The size of the marker.
            public float MarkerSize { get; set; }
        }
        
        protected override void LoadDefaultConfig()
        {
            _config = new Configuration
            {
                RaidZoneBaseRadius = 40.0f,
                RaidZoneMinRadius = 30.0f,
                RaidZoneMaxRadius = 80.0f,
                RaidZoneRadiusMultiplier = 1.0f,
                RaidZoneExclusionBaseRadius = 100.0f,
                RaidZoneExclusionMinRadius = 50.0f,
                RaidZoneExclusionMaxRadius = 300.0f,
                RaidZoneExclusionRadiusMultiplier = 1.0f,
                ZoneDeactivationDelay = 180.0f,
                MarkerColor = "#FF0000",
                MarkerAplha = 0.4f,
                MarkerSize = 0.75f
            };
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }
        
        #endregion
        
        #region data management
        
        private void SaveData()
        {
            _storedData.RaidZoneList = _raidMeZones.Select(kvp => new RaidZones { PlayerId = kvp.Key, TcId = kvp.Value }).ToList();
            _storedData.TcMarkerMap = _mapMarkers.Select(kvp => new TcMapMarker { TcId = kvp.Key, MarkerId = kvp.Value }).ToList();
            Interface.Oxide.DataFileSystem.WriteObject(DataFileName, _storedData);
        }
        
        private void LoadData()
        {
            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(DataFileName);
            if (_storedData == null) return;
            _raidMeZones = _storedData.RaidZoneList.ToDictionary(rz => rz.PlayerId, rz => rz.TcId);
            _mapMarkers = _storedData.TcMarkerMap.ToDictionary(tm => tm.TcId, tm => tm.MarkerId);
        }
        
        private class StoredData
        {
            public List<RaidZones> RaidZoneList { get; set; } = new List<RaidZones>();
            public List<TcMapMarker> TcMarkerMap { get; set; } = new List<TcMapMarker>();
        }
        
        private class RaidZones
        {
            public ulong PlayerId { get; set; }
            public NetworkableId TcId { get; set; }
        }
        
        private class TcMapMarker
        {
            public NetworkableId TcId { get; set; }
            public NetworkableId MarkerId { get; set; }
        }
        
        #endregion
    }
}
