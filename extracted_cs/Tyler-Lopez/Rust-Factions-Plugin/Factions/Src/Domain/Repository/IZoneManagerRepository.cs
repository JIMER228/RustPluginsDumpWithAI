namespace Oxide.Plugins
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
