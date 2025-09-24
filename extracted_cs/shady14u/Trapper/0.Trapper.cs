//plugin.merge -c -m -p ./merge.json

using System;
using System.Collections.Generic;
using System.Globalization;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    //Define:FileOrder=1
    [Info("Trapper", "Shady14u", "1.1.0")]
    [Description("Control who sets off traps")]
    public partial class Trapper : RustPlugin
    {
    
        [PluginReference] 
        private Plugin Friends, RustIO, Clans; 
        
        private static BasePlayer FindPlayer(string nameOrId)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString == nameOrId)
                    return activePlayer;
                if (activePlayer.displayName.Contains(nameOrId, CompareOptions.OrdinalIgnoreCase))
                    return activePlayer;
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.UserIDString == nameOrId)
                    return sleepingPlayer;
                if (sleepingPlayer.displayName.Contains(nameOrId, CompareOptions.OrdinalIgnoreCase))
                    return sleepingPlayer;
            }
            return null;
        }
    }
}
