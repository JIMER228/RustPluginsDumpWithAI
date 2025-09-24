namespace Oxide.Plugins
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
