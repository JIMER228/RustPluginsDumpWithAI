// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Oxide.Plugins
{
    [Info("Static3D", "Hougan", "0.0.1")]
    public class Static3D : RustPlugin
    {
        #region Classes

        private class StoredMarker
        {
            public string Position;
            public string Text;

            public float Distance;
            public float UpdateInterval;
        }
        
        #endregion 
 
        #region Initialization

        private void OnServerInitialized() => Interface.Oxide.CallHook("AskForMarkers");

        #endregion

        #region API

        private void AddInstance(Vector3 position, string text, float distance, float interval)
        {
            var players = new List<BasePlayer>();
            Vis.Entities(position, distance, players);

            foreach (var check in players.Where(p => p.IsConnected))
                DrawMarker(check, new StoredMarker
                {
                    Position       = position.ToString(),
                    Text           = text,
                    Distance       = distance,
                    UpdateInterval = interval
                }); 
        }

        #endregion

        #region Methods
        

        private static void DrawMarker(BasePlayer player, StoredMarker marker)
        {
            player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
            player.SendEntityUpdate(); 
            player.SendConsoleCommand("ddraw.text", marker.UpdateInterval, Color.white, marker.Position, marker.Text);
            player.SendConsoleCommand("camspeed 0");
                    
            if (player.Connection.authLevel < 2)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                    
            player.SendEntityUpdate();
        }

        #endregion
    }
}