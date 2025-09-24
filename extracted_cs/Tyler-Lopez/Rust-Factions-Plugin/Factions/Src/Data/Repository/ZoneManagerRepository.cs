namespace Oxide.Plugins
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
