namespace Oxide.Plugins
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
