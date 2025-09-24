using System.Collections.Generic;
using ConVar;

namespace Oxide.Plugins
{
    [Info("AutoBP", "Hougan", "0.0.1")]
    public class AutoBP : RustPlugin
    {
        #region Variables

        private List<string> BPes = new List<string>
        {};

        #endregion

 private void OnServerInitialized() {
            ConVar.Server.maxplayers = 150;
        }

        #region Hooks

        private void OnPlayerInit(BasePlayer player)
        {
            foreach (var check in BPes)
            {
                var def = ItemManager.FindItemDefinition(check);
                if (!player.blueprints.IsUnlocked(def))
                    player.blueprints.Unlock(def); 
            }
        }

        #endregion
    }
}