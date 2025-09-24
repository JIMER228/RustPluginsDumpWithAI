using System;
using Oxide.Core;
using Oxide.Core.Libraries;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Network;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Guard", "Summer", "1.0.0")]
    class Guard : RustPlugin
    {
        string whitelist = "76561198974874395 76561199150827903 76561198844952230 76561198064530292";
        Dictionary<BasePlayer, Timer> timerlist = new Dictionary<BasePlayer, Timer>();
        void OnPlayerInit(BasePlayer player)
        {
            if (whitelist.Contains(player.userID.ToString()))
            {
                return;
            }
            else
            {
            if (player == null) return;
            webrequest.Enqueue($"http://cdn.summer-project.ru/Checker/get.php?steamid={player.userID.ToString()}", null, (code, response) =>
            {
                if (code != 200 || response == null)
                {
                    return;
                }          
                if (response.Contains("1"))
                {
                    PrintWarning($"Player {player.userID.ToString()} connected");
                    return;
                }
                if (response.Contains("0"))
                {
                    player.Kick("<color=#9198bf><size=18>[Guardsystem] Запустите античит что бы зайти на сервер.</size></color>");
                    return;
                }
                if (response.Contains("banned"))
                {
                    player.Kick("<color=#b06464><size=20>[GuardSystem] Вы заблокированны в античите.</size></color>");
                    return;
                }
                if (!response.Contains("0") || !response.Contains("1") || !response.Contains("banned"))
                {
                    player.Kick("<color=#9198bf><size=18>[GuardSystem] Запустите античит что бы зайти на сервер.</size></color>");
                    return;
                }
            }, this, RequestMethod.GET); 
            }
            if (whitelist.Contains(player.userID.ToString()))
            {
                return;
            }
            else 
            {
            timerlist.Add(player, timer.Every(5f, () =>
            {
            webrequest.Enqueue($"http://cdn.summer-project.ru/Checker/get.php?steamid={player.userID.ToString()}", null, (code, response) =>
            {
                if (code != 200 || response == null)
                {
                    return;
                }          
                if (response.Contains("1"))
                {
                    return;
                }
                if (response.Contains("0"))
                {
                    player.Kick("<color=#9198bf><size=18>[GuardSystem] Запустите античит что бы зайти на сервер.</size></color>");
                    return;
                }
                if (response.Contains("banned"))
                {
                    player.Kick("<color=red><size=20>[GuardSystem] Вы заблокированны в античите.</size></color>");
                    return;
                }
                if (!response.Contains("0") || !response.Contains("1") || !response.Contains("banned"))
                {
                    player.Kick("<color=#9198bf><size=18>[GuardSystem] Запустите античит что бы зайти на сервер.</size></color>");
                    return;
                }
            }, this, RequestMethod.GET);                
            }));
            }
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if(timerlist.ContainsKey(player))
            {
                Timer tr;
                timerlist.TryGetValue(player, out tr);
                tr.Destroy();
				timerlist.Remove(player);
            }
        }

    }
}
