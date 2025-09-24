// Requires: ZoneManager
namespace Oxide.Plugins
{
    [Info("Factions", "Rust Factions", "1.0.0")]
    [Description("Pending Description")]
    public partial class Factions : RustPlugin
    {
        #region Plugin Declaration and Global Variables
        private IZoneManagerRepository _zoneManagerRepository;
        private FactionsMapMarkerManager _factionsMapMarkerManager;
        #endregion
    }
}
﻿namespace Oxide.Plugins
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using Oxide.Core.Plugins;
    using System;
    using UnityEngine;
    using UnityEngine.UIElements;

    partial class Factions
    {
        private class ZoneManagerRepository : IZoneManagerRepository
        {
            private readonly ZoneManager _zoneManager;

            private static class Constants
            {
                public const string ZoneManagerPluginName = "ZoneManager";

                public const string CreateOrUpdateZone = "CreateOrUpdateZone";
                public const string CreateOrUpdateZoneParameterName = "name";
                public const string CreateOrUpdateZoneParameterRadius = "radius";
                public const string CreateOrUpdateZoneParameterSize = "size";

                public const string CreateOrUpdateZoneParameterSizeValueFormat = "{0} {1} {2}";


                public const char GridPrefix = '_';

                public const int GridHeight = 2000;
            }

            public static IZoneManagerRepository CreateInstance(PluginManager manager)
            {
                var zoneManager = manager.GetPlugin(Constants.ZoneManagerPluginName) as ZoneManager;
                return zoneManager == null ? null : new ZoneManagerRepository(zoneManager);
            }

            private ZoneManagerRepository(ZoneManager zoneManager)
            {
                _zoneManager = zoneManager;
            }

            bool IZoneManagerRepository.CreateZoneForGrid(FactionsGrid factionsGrid, UnityEngine.Vector2 center, float gridSize)
            {
                var adjGridSize = (int)Mathf.Floor(gridSize);
                return (this as IZoneManagerRepository).CreateOrUpdateZoneRectangular(
                    $"{Constants.GridPrefix}{factionsGrid}",
                    factionsGrid.ToString(),
                    center,
                    adjGridSize,
                    Constants.GridHeight,
                    adjGridSize
                    );
            }


            bool IZoneManagerRepository.CreateOrUpdateZoneRectangular(string zoneId, string name, UnityEngine.Vector3 location, int width, int height, int length)
            {
                /*
                var argsMap = new Dictionary<string, string>
                {
                    [Constants.CreateOrUpdateZoneParameterName] = name,
                    [Constants.CreateOrUpdateZoneParameterSize] = string.Format(Constants.CreateOrUpdateZoneParameterSizeValueFormat, width, height, length)
                };
                var response = _zoneManager.Call<bool?>(Constants.CreateOrUpdateZone, zoneId, argsMap.ToArray(), location);
                return response ?? false;
                */
                return true;
            }

            bool IZoneManagerRepository.CreateOrUpdateZoneCircular(string zoneId, string name, UnityEngine.Vector3 location, int radius)
            {
                var argsMap = new Dictionary<string, string>
                {
                    [Constants.CreateOrUpdateZoneParameterName] = name,
                    [Constants.CreateOrUpdateZoneParameterRadius] = radius.ToString()
                };
                var response = _zoneManager.Call<bool?>(Constants.CreateOrUpdateZone, zoneId, argsMap.ToArray(), location);
                return response ?? false;
            }

            bool IZoneManagerRepository.EraseZone(string zoneId)
            {
                throw new System.NotImplementedException();
            }

            string[] IZoneManagerRepository.GetPlayerZoneIds(BasePlayer player)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
﻿namespace Oxide.Plugins
{
    using System.Text;
    public class FactionsGrid
    {
        private readonly byte _columnByte;
        private readonly string _columnString;
        private readonly byte _row;

        private static class Constants
        {
            public const char ColumnFirstChar = 'A';
            public const char ColumnLastBeforeRepeat = 'Z';
            public const char RowColumnDelimiter = ':';
        }

        public FactionsGrid(byte row, byte columnByte)
        {
            _row = row;
            _columnByte = columnByte;
            const int columnRange = (Constants.ColumnLastBeforeRepeat - Constants.ColumnFirstChar) + 1;
            var columnStringBuilder = new StringBuilder();
            for (var excessRanges = 0; excessRanges < (_columnByte / columnRange); excessRanges++)
            {
                columnStringBuilder.Append(Constants.ColumnFirstChar);
            }
            columnStringBuilder.Append((char)(Constants.ColumnFirstChar + (_columnByte % columnRange)));
            _columnString = columnStringBuilder.ToString();
        }

        public byte GetRow()
        {
            return _row;
        }

        public byte GetColumnNumeric()
        {
            return _columnByte;
        }

        public string GetColumnString()
        {
            return _columnString;
        }

        public override string ToString()
        {
            return $"{_columnString}{Constants.RowColumnDelimiter}{_row}";
        }
    }
}
﻿namespace Oxide.Plugins
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    public sealed class FactionsMap : IEnumerable<FactionsGrid>
    {
        private readonly int _columns;
        private readonly int _rows;
        private readonly float _gridOffset;
        private List<FactionsGrid> _grids;

        public FactionsMap(int worldSize)
        {
            // The in-game map is always square
            _columns = (int)Mathf.Floor(worldSize / Constants.GridCellSize);
            _rows = _columns;
            // Sometimes (0,0,0) in-game is not the center of the center factionsGrid - the offset is how much it is off by
            var sizeUsedByGrids = _columns * Constants.GridCellSize;
            var sizeUsedByGridsHalved = sizeUsedByGrids / 2f;
            _gridOffset = (worldSize - sizeUsedByGridsHalved) / 2f;
        }

        private static class Constants
        {
            public const float GridCellSize = 146.3f;
        }

        public Vector2 GetGridTopLeft(FactionsGrid factionsGrid)
        {
            return new Vector2(
                (factionsGrid.GetColumnNumeric() * Constants.GridCellSize) - _gridOffset,
                (factionsGrid.GetRow() * Constants.GridCellSize * -1) + _gridOffset
            );
        }

        public Vector2 GetGridCenter(FactionsGrid factionsGrid)
        {
            var sizeOfGridHalved = Constants.GridCellSize / 2f;
            return new Vector2(
                (factionsGrid.GetColumnNumeric() * Constants.GridCellSize) - _gridOffset + sizeOfGridHalved,
                (factionsGrid.GetRow() * Constants.GridCellSize * -1) + _gridOffset - sizeOfGridHalved
            );
        }

        public static float GetGridSize()
        {
            return Constants.GridCellSize;
        }

        public IEnumerator<FactionsGrid> GetEnumerator()
        {
            if (_grids != null)
            {
                foreach (var grid in _grids) yield return grid;
            }
            else
            {
                _grids = new List<FactionsGrid>();



                for (byte row = 0; row < _rows; row++)
                {
                    for (byte column = 0; column < _columns; column++)
                    {
                        _grids.Add(new FactionsGrid(row, column));
                        yield return _grids.Last();
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

    }
}

﻿namespace Oxide.Plugins
{
    using UnityEngine;
    using System.Collections.Generic;
    using Network;

    public interface IFactionsMapMarkerSpecification
    {
        Vector2 GetLocation();
    }

    public sealed class ClanClaim : IFactionsMapMarkerSpecification
    {
        private readonly Vector2 _location;
        public readonly Color Color;
        public readonly bool IsCapital;

        public ClanClaim(float colorRed, float colorGreen, float colorBlue, bool isCapital, Vector2 location)
        {
            Color = new Color(colorRed, colorGreen, colorBlue);
            _location = location;
            IsCapital = isCapital;
        }

        Vector2 IFactionsMapMarkerSpecification.GetLocation()
        {
            return _location;
        }
    }

    public sealed class Badlands : IFactionsMapMarkerSpecification
    {
        private readonly Vector2 _location;

        public Badlands()
        {
            // TODO
        }

        Vector2 IFactionsMapMarkerSpecification.GetLocation()
        {
            return _location;
        }
    }

    public sealed class FactionsMapMarker
    {
        private MapMarkerGenericRadius _marker;
        private Timer _pendingTimer = null;

        private static class Constants
        {
            public const string EntityPrefab = "assets/prefabs/tools/map/genericradiusmarker.prefab";
            public const string MarkerUpdateRpcFunction = "MarkerUpdate";
            public const float ClanClaimAlpha = 0.5f;
            public const float SubscriptionAddDelaySeconds = 10f;
        }

        public FactionsMapMarker(IFactionsMapMarkerSpecification specification)
        {
            Vector2 position = specification.GetLocation();
            _marker = GameManager.server.CreateEntity(Constants.EntityPrefab, position)
                .GetComponent<MapMarkerGenericRadius>();

            var claim = (ClanClaim)specification;
            if (claim != null)
            {
                _marker.color1 = claim.Color;
                _marker.radius = 2f;
                _marker.alpha = Constants.ClanClaimAlpha;
            }

            _marker.Spawn();
        }

        public void Respawn()
        {
            var prevColor = _marker.color1;
            var prevRadius = _marker.radius;
            var prevAlpha = _marker.alpha;
            _marker.Kill();
            _marker = GameManager.server.CreateEntity(Constants.EntityPrefab, _marker.ServerPosition)
                .GetComponent<MapMarkerGenericRadius>();
            _marker.color1 = prevColor;
            _marker.radius = prevRadius;
            _marker.alpha = prevAlpha;
            _marker.Spawn();
        }

        public void PlayerSubscriptionAdd(BasePlayer player, PluginTimers timer)
        {
            _marker.OnNetworkSubscribersEnter(new List<Connection>() { player.Connection });
            // Adding network subscribers is an async queue, so sending the update must be on a slight delay
            _pendingTimer?.Destroy();
            _pendingTimer = timer.Once(Constants.SubscriptionAddDelaySeconds, () =>
            {
                _marker.SendUpdate();
            });
        }

        public void PlayerSubscriptionRemove(BasePlayer player)
        {
            _marker.OnNetworkSubscribersLeave(new List<Connection>() { player.Connection });
        }

        public void Kill()
        {
            _marker.Kill();
        }
    }
}
﻿namespace Oxide.Plugins
{
    using System.Collections.Generic;
    using System.Linq;
    public class FactionsMapMarkerManager
    {
        private readonly Dictionary<string, FactionsMapMarker> _landClaimMarkers =
            new Dictionary<string, FactionsMapMarker>();

        private readonly HashSet<ulong> _playersHidingLandClaimMarkers = new HashSet<ulong>();

        /** Add a new Land Claim type FactionsMapMarker to the Manager **/
        public void AddLandClaimMarker(string gridString, FactionsMapMarker marker, PluginTimers timer)
        {
            // Kill and remove any previous marker, if it exists
            FactionsMapMarker prevMarker;
            _landClaimMarkers.TryGetValue(gridString, out prevMarker);
            prevMarker?.Kill();

            // Update tracked marker reference
            _landClaimMarkers[gridString] = marker;

            // Subscribe all players who are not hiding land claim markers to the new marker
            foreach (var player in BasePlayer.activePlayerList.Where(player => !_playersHidingLandClaimMarkers.Contains(player.userID)))
            {
                marker.PlayerSubscriptionAdd(player, timer);
            }
        }

        /** Invoked when a player toggles their land claim marker visibility **/
        public void PlayerToggleLandClaimMarkerVisibility(BasePlayer player, PluginTimers timer)
        {
            if (_playersHidingLandClaimMarkers.Contains(player.userID))
            {
                _playersHidingLandClaimMarkers.Remove(player.userID);

                foreach (var landClaimMarker in _landClaimMarkers.Values)
                {
                    landClaimMarker.PlayerSubscriptionAdd(player, timer);
                }
            }
            else
            {
                _playersHidingLandClaimMarkers.Add(player.userID);
                foreach (var landClaimMarker in _landClaimMarkers.Values)
                {
                    landClaimMarker.PlayerSubscriptionRemove(player);
                }
            }
        }

        // Destroys all managed markers
        public void DestroyMarkers()
        {
            foreach (var landClaimMarker in _landClaimMarkers.Values)
            {
                landClaimMarker.Kill();
            }
        }
    }
}
﻿namespace Oxide.Plugins
{
    using UnityEngine;
    partial class Factions
    {
        /// <summary> Refer to Developer API https://umod.org/plugins/zone-manager </summary>
        private interface IZoneManagerRepository
        {
            /// <summary>
            /// Creates or updates a ZoneManager zone.
            /// </summary>
            /// <param name="zoneId"></param>
            /// <param name="name"></param>
            /// <param name="location"></param>
            /// <param name="radius"></param>
            /// <returns>Returns true if the zone is valid, else returns false if it was saved but not created (only reason would be that no position for the zone was set)</returns>
            bool CreateOrUpdateZoneCircular(string zoneId, string name, Vector3 location, int radius);

            bool CreateOrUpdateZoneRectangular(string zoneId, string name, Vector3 location, int width, int height, int length);

            /// <summary>
            /// Erase a zone by ZoneID or name.
            /// </summary>
            /// <param name="zoneId"></param>
            /// <returns>Returns true if the zone was deleted or false if the zone doesn't exist</returns>
            bool EraseZone(string zoneId);

            /// <summary>
            /// 
            /// </summary>
            /// <param name="player"></param>
            /// <returns>Returns a string[] of IDs for zones the specified player is currently in, or null if none found</returns>
            string[] GetPlayerZoneIds(BasePlayer player);

            bool CreateZoneForGrid(FactionsGrid factionsGrid, Vector2 center, float gridSize);
        }
    }
}
﻿namespace Oxide.Plugins
{
    partial class Factions
    {
        public void InitializeMapForNewWipe()
        {
            // For each factionsGrid on the map create a ZoneManager zone for them
            var map = new FactionsMap(ConVar.Server.worldsize);
            foreach (var grid in map)
            {
                _zoneManagerRepository.CreateZoneForGrid(
                    factionsGrid: grid,
                    center: map.GetGridCenter(grid),
                    gridSize: FactionsMap.GetGridSize()
                );
            }
        }
    }
}
﻿namespace Oxide.Plugins
{
    using Oxide.Core.Plugins;
    using System.Collections.Generic;
    using System.Text;
    using WebSocketSharp;

    partial class Factions
    {
        private void Loaded()
        {
            var manager = Manager;

            _zoneManagerRepository = ZoneManagerRepository.CreateInstance(manager);
            _factionsMapMarkerManager = new FactionsMapMarkerManager();

            var missingPluginsConsoleMessage = new StringBuilder();

            if (_zoneManagerRepository == null)
            {
                missingPluginsConsoleMessage.AppendLine(
                    "ZoneManager is not loaded! Get it here https://umod.org/plugins/zone-manager.");
            }

            var message = missingPluginsConsoleMessage.ToString();
            if (!message.IsNullOrEmpty())
            {
                Puts(message);
            }

            InitializeMapForNewWipe();
        }

        private void Unload()
        {
            _factionsMapMarkerManager.DestroyMarkers();
        }
    }
}
﻿namespace Oxide.Plugins
{
    using Oxide.Core.Plugins;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
    partial class Factions
    {

        [ChatCommand("test")]
        private void OnCommand(BasePlayer player, string command, string[] args)
        {
            var myMapMarker = new FactionsMapMarker(new ClanClaim(
                colorRed: 1.0f,
                colorGreen: 1.0f,
                colorBlue: 1.0f,
                isCapital: false,
                location: Vector2.zero
            ));

            _factionsMapMarkerManager.AddLandClaimMarker("A:6", myMapMarker, timer);
        }

        [ChatCommand("test2")]
        private void OnCommandTwo(BasePlayer player, string command, string[] args)
        {
            _factionsMapMarkerManager.PlayerToggleLandClaimMarkerVisibility(player, timer);
        }
    }
}
