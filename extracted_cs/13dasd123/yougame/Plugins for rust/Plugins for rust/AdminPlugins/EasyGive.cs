// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using System.Globalization;
using System;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("EasyGive", "S1m0n", "1.1.0")]
    [Description("A simple tool used to give items.")]

    public class EasyGive : RustPlugin
    {

        string PermissionGiveAny = "easygive.give";
        string PermissionGiveAll = "easygive.giveall";
        string PermissionGiveSelf = "easygive.i";
        string prefix = "<color=#66ff66>[EasyGive]</color>";

        void RegisterPermissions()
        {
            permission.RegisterPermission(PermissionGiveAny, this);
            permission.RegisterPermission(PermissionGiveAll, this);
            permission.RegisterPermission(PermissionGiveSelf, this);
        }

        [ChatCommand("giveall")]
        void onCommandGiveAll(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionGiveAll) && !player.IsAdmin) { sendMessage(player, "У вас нет прав, чтобы использовать эту команду."); return; }
            if (args.Length < 1) { sendMessage(player, "Используйте - <color=#ffd479>/giveall <предмет> [количество]</color>"); return; }

            string itemName = args[1];
            int amount = 1;

            if (args.Length == 3)
                amount = Int32.Parse(args[2]);

            Item item = getItem(itemName, amount);
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                p.inventory.GiveItem(item);
                sendMessage(p, $"Вы получили <color=#ffd479>x{amount.ToString()} {item.info.displayName.english}</color>.");
            }

            sendMessage(player, $"Вы выдали каждому игроку <color=#ffd479>x{amount.ToString()} {item.info.displayName.english}</color>.");
            return;
        }

        [ChatCommand("give")] // give <player> <item> <amount> <name>
        void onCommandGive(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionGiveAny) && !player.IsAdmin) { sendMessage(player, "У вас нет прав, чтобы использовать эту команду."); return; }
            if (args.Length < 2) { sendMessage(player, "Используйте - <color=#ffd479>/give <игрок> <предмет> [количество] [название]</color>"); return; }

            BasePlayer target = getPlayer(args[0]);

            if (target == null) { sendMessage(player, "Игрок не найден."); return; }

            string itemName = args[1];
            int amount = 1;
            string itemDisplayName = "";

            if (args.Length == 3)
                amount = Int32.Parse(args[2]);

            if (args.Length == 4)
                itemDisplayName = args[3];

            Item item = getItem(itemName, amount);
            if (itemDisplayName != "")
                item.name = itemDisplayName;

            target.inventory.GiveItem(item);
            sendMessage(player, $"Вы выдали игроку <color=#ffd479>'{target.displayName}'</color> - <color=#ffd479>x{amount.ToString()} {item.info.displayName.english}</color>.");
            sendMessage(target, $"Вы получили <color=#ffd479>x{amount.ToString()} {item.info.displayName.english}</color>.");
        }

        [ChatCommand("i")] // give <item> [amount] [name]
        void onCommandI(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionGiveSelf) && !player.IsAdmin) { sendMessage(player, "У вас нет прав, чтобы использовать эту команду."); return; }
            if (args.Length < 1) { sendMessage(player, "Используйте - <color=#ffd479>/i <предмет> [количество] [название]</color>"); return; }

            string itemName = args[0];
            int amount = 1;
            string itemDisplayName = "";

            if (args.Length == 2)
                amount = Int32.Parse(args[1]);

            if (args.Length == 3)
                itemDisplayName = args[2];

            Item item = getItem(itemName, amount);
            if (itemDisplayName != "")
            {
                itemDisplayName = itemDisplayName.Replace("_", " ");
                item.name = itemDisplayName;
            }

            player.inventory.GiveItem(item);
            sendMessage(player, $"Вы получили <color=#ffd479>x{amount.ToString()} {item.info.displayName.english}</color>.");
        }

        BasePlayer getPlayer(string partialName)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (player.displayName.ToLower().Contains(partialName.ToLower()))
                    return player;
            return null;
        } // returns a player based on name

        Item getItem(string partialName, int amount)
        {
            return ItemManager.CreateByPartialName(partialName, amount);
        } // returns an item based on name & amount
        
        void sendMessage(BasePlayer player, string message, bool pref=true)
        {
            if (pref)
                PrintToChat(player, $"{prefix} {message}"); else
                PrintToChat(player, $"{message}");
        } // sends a player a message

    }

}