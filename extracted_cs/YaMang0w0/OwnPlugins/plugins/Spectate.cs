
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Spectate", "YaMang -w-", "1.0.0")]
    [Description("https://discord.gg/DTQuEE7neZ")]
    internal class Spectate : RustPlugin
    {
        [ChatCommand("spectate")]
        private void SpectateCMD(BasePlayer player, string command, string[] args)
        {
            if(!permission.UserHasPermission(player.UserIDString, "spectate.use"))
            {
                PrintToChat(player, $"<color=red>You don't have Permission</color>");
                return;
            }

            if(args.Length == 0)
            {
                PrintToChat(player, "Require Type Steamid Or Name\n/spectate yamang\n/spectate 765611*******");
                return;
            }

            if (args[0] == "stop")
            {
                player.Respawn();
                return;
            }

            var target = FindPlayers(args[0]);
            if(target == null)
            {
                PrintToChat(player, $"플레이어를 찾을 수 없습니다.");
                return;
            }

            if (!player.IsSpectating())
            {
                player.StartSpectating();
                player.UpdateSpectateTarget(target.UserIDString);
            }
            else
            {
                player.UpdateSpectateTarget(target.UserIDString);
            }
            
        }
        void OnServerInitialized(bool initial)
        {
            permission.RegisterPermission("spectate.use", this);
        }

        private static BasePlayer FindPlayers(string nameOrIdOrIp)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.userID.ToString() == nameOrIdOrIp)
                    return activePlayer;
                if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return activePlayer;
                if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress == nameOrIdOrIp)
                    return activePlayer;
            }
            return null;
        }

    }
}
