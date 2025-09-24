using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ ПОФИКШЕН С ПОМОЩЬЮ ПРОГРАММЫ СКАЧАНОЙ С https://discord.gg/dNGbxafuJn */ [Info("Better No Workbench", "https://discord.gg/dNGbxafuJn", "1.2.1")]
    [Description("Adds workbench access to players with permission")]
    public class BetterNoWorkbench : RustPlugin
    {
        private TriggerWorkbench trigger;

        private void Init()
        {
            permission.RegisterPermission("BetterNoWorkbench.on", this);
        }

        #region Oxide Hooks 
        private void OnServerInitialized()
        {
            trigger = new GameObject().AddComponent<TriggerWorkbench>();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (permission.UserHasPermission(player.UserIDString, "BetterNoWorkbench.on"))
                {
                    AddWorkbench(player);
                }
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, "BetterNoWorkbench.on"))
            {
                AddWorkbench(player);
            }
        }

        private void AddWorkbench(BasePlayer player)
        {
            float level = 3f;

            player.nextCheckTime = float.MaxValue;
            player.cachedCraftLevel = level;
            player.EnterTrigger(trigger);
            player.SendNetworkUpdateImmediate();
        }

        private void RemoveWorkbench(BasePlayer player)
        {
            float level = 0f;

            player.LeaveTrigger(trigger);
            player.nextCheckTime = float.MaxValue;
            player.cachedCraftLevel = level;
            player.SendNetworkUpdateImmediate();
        }
        #endregion
    }
}/* Boosty - https://boosty.to/skulidropek 
Discord - https://discord.gg/k3hXsVua7Q 
Discord The Rust Bay - https://discord.gg/Zq3TVjxKWk  */