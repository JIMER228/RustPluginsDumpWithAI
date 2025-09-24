using System;
using System.Collections.Generic;
using System.Collections;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Explorer Map", "st-little", "0.1.1")]
    [Description("View explored terrain on a map.")]
    public class ExplorerMap : RustPlugin
    {
        #region Fields

        private const string GenericradiusmarkerPrefab = "assets/prefabs/tools/map/genericradiusmarker.prefab";
        private const string DataFileName = "ExplorerMap";
        private const int GridSize = 150;
        private const string MapMarkerName = "explorermap.marker";
        private const string PermissionUse = "explorermap.use";

        private readonly Dictionary<string, MapMarkerGenericRadius> SpawnedMarkers = new Dictionary<string, MapMarkerGenericRadius>();

        #endregion

        #region Configuration

        private Configuration _configuration;

        private class Configuration
        {
            public float MapUpdateInterval;
            public string MarkerColor;
            public float MarkerAlpha;
            public float MarkerRadius;
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                MapUpdateInterval = 1f,
                MarkerColor = "#14001a",
                MarkerAlpha = 1f,
                MarkerRadius = 1f
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _configuration = Config.ReadObject<Configuration>();

                if (_configuration == null)
                    LoadDefaultConfig();
            }
            catch
            {
                PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => _configuration = GetDefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(_configuration);

        #endregion

        #region Stored Data

        private class StoredData
        {
            public List<string> Explored = new List<string>();

            public StoredData()
            {
            }
        }

        private StoredData storedData;

        #endregion

        #region Oxide hooks

        void Init()
        {
            permission.RegisterPermission(PermissionUse, this);
            storedData = Core.Interface.Oxide.DataFileSystem.ReadObject<StoredData>(DataFileName);
        }

        void OnServerInitialized(bool initial)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                SpawnMarkersOnThePlayerMap(player.UserIDString);
            }

            ServerMgr.Instance.StartCoroutine(WatchPlayersMovement());
        }

        void OnUserConnected(IPlayer player)
        {
            SpawnMarkersOnThePlayerMap(player.Id);
        }

        object CanNetworkTo(MapMarkerGenericRadius radius, BasePlayer target)
        {
            if (radius.name != MapMarkerName) return null;

            if (target.userID == radius.OwnerID) return null;

            return false;
        }

        void Unload()
        {
            ServerMgr.Instance.StopCoroutine(WatchPlayersMovement());
        }

        #endregion

        #region Map Helper

        private IEnumerator WatchPlayersMovement()
        {
            foreach (BasePlayer activePlayer in BasePlayer.activePlayerList)
            {
                if (!permission.UserHasPermission(activePlayer.UserIDString, PermissionUse)) continue;

                string gridCoord = MapHelper.PositionToString(activePlayer.transform.position);
                if (SpawnedMarkers.TryGetValue($"{activePlayer.UserIDString}_{gridCoord}", out var _spawnedMarkers))
                {
                    _spawnedMarkers.Kill();
                    _spawnedMarkers.SendUpdate();
                    SpawnedMarkers.Remove($"{activePlayer.UserIDString}_{gridCoord}");

                    storedData.Explored.Add($"{activePlayer.UserIDString}_{gridCoord}");
                    Core.Interface.Oxide.DataFileSystem.WriteObject(DataFileName, storedData);
                }
            }
            yield return new WaitForSeconds(_configuration.MapUpdateInterval);

            yield return WatchPlayersMovement();
        }

        private void SpawnMarkersOnThePlayerMap(string playerId)
        {
            if (!permission.UserHasPermission(playerId, PermissionUse)) return;

            int wrldSize = ConVar.Server.worldsize;
            int gridLength = wrldSize / GridSize;

            for (int x = 0; x < gridLength; x++)
            {
                for (int y = 0; y < gridLength; y++)
                {
                    var gridCoord = $"{ToAlphabet(x)}{y}";

                    var isExplored = storedData.Explored.Exists(m => m == $"{playerId}_{gridCoord}");
                    if (isExplored) continue;

                    var position = MapHelper.StringToPosition(gridCoord);
                    if (position == null) continue;

                    var spawnedMarker = SpawnMarker(Convert.ToUInt64(playerId), (Vector3)position);
                    if (spawnedMarker == null) continue;

                    SpawnedMarkers[$"{playerId}_{gridCoord}"] = spawnedMarker;
                }
            }
        }

        private MapMarkerGenericRadius? SpawnMarker(ulong ownerID, Vector3 position)
        {
            MapMarkerGenericRadius? mapMarker = GameManager.server.CreateEntity(GenericradiusmarkerPrefab, position) as MapMarkerGenericRadius;
            if (mapMarker == null) return null;

            _ = ColorUtility.TryParseHtmlString(_configuration.MarkerColor, out var color);

            mapMarker.OwnerID = ownerID;
            mapMarker.name = MapMarkerName;
            mapMarker.alpha = _configuration.MarkerAlpha;
            mapMarker.radius = _configuration.MarkerRadius;
            mapMarker.color1 = color;
            mapMarker.color2 = color;

            mapMarker.Spawn();
            mapMarker.SendUpdate();

            return mapMarker;
        }

        private static string ToAlphabet(int num)
        {
            string result = "";
            while (num >= 0)
            {
                result = (char)(num % 26 + 'A') + result;
                num = num / 26 - 1;
            }
            return result;
        }

        #endregion
    }
}

