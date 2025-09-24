// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AdminChat", "S1m0n", "1.1.0")]
    class AdminChat : RustPlugin
    {
        private StoredData storedData;

        private readonly string Prefix = "[АДМИН]";
        private readonly string TextColour = "#ffffff";
        private readonly string AdminTextColour = "#00ffff";
        private readonly string PrefixColour = "#0099ff";
        private readonly string PermissionMaster = "adminchat.*";
        private readonly string PermissionViewChat = "adminchat.view";
        private readonly string PermissionUseChat = "adminchat.use";
        private readonly string DataFile = "adminchat_logfile";

        public List<BasePlayer> ActiveAdminChat = new List<BasePlayer>();

        void GlobalMessage(string message, bool prefix = true)
        {
            string prefixFormat = $"<color={PrefixColour}>{Prefix}</color> ";
            string messageFormat = $"<color={TextColour}>{message}</color>";

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (prefix)
                    player.ChatMessage(prefixFormat + messageFormat);
                else
                    player.ChatMessage(messageFormat);
            }

            return;
        }
        void SendMessage(BasePlayer player, string message, bool prefix = true)
        {
            string prefixFormat = $"<color={PrefixColour}>{Prefix}</color> ";
            string messageFormat = $"<color={TextColour}>{message}</color>";

            if (prefix)
                player.ChatMessage(prefixFormat + messageFormat);
            else
                player.ChatMessage(messageFormat);

            return;
        }
        void SendToAdminChat(BasePlayer sender, string message)
        {
            string prefixFormat = $"<color={PrefixColour}>[АДМИН]</color> {sender.displayName}: ";
            string messageFormat = $"<color={AdminTextColour}>{message}</color>";

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (HasPermission(player, PermissionViewChat))
                    player.ChatMessage(prefixFormat + messageFormat);

            storedData.ChatLog.Add($"[АДМИН] ({DateTime.Now.ToShortDateString()}) ({DateTime.Now.ToShortTimeString()}) {sender.displayName}: {message}");
        }

        BasePlayer getPlayer(string partialName)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (player.displayName.ToLower().Contains(partialName.ToLower()))
                    return player;
            return null;
        }
        BasePlayer getPlayer(ulong playerID)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (player.userID == playerID)
                    return player;
            return null;
        }

        bool HasPermission(BasePlayer player, string permissionName)
        {
            if (permission.UserHasPermission(player.UserIDString, PermissionMaster)) return true;
            if (player.IsAdmin) return true;
            if (!permission.UserHasPermission(player.UserIDString, permissionName)) return false;

            return true;
        }
        void RegisterPermissions()
        {
            permission.RegisterPermission(PermissionMaster, this);
        }

        void Init()
        {
            openDataFile();
        }

        void OnServerSave()
        {
            writeFile();
        }

        #region Configuration & Lang

        [ChatCommand("a")]
        void OnCommandAdminChat(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, PermissionUseChat)) { SendMessage(player, "У вас нет прав, чтобы использовать чат админа."); return; }

            if (args.Length == 0)
            {
                if (!ActiveAdminChat.Contains(player))
                {
                    ActiveAdminChat.Add(player);
                    SendMessage(player, "Вы включили автоматический чат админа.");
                    return;
                } else
                {
                    ActiveAdminChat.Remove(player);
                    SendMessage(player, "Вы выключили автоматический чат админа.");
                    return;
                }
            }

            if (args.Length > 0)
            {
                string message = "";

                foreach (string s in args)
                    message += $"{s} ";

                SendToAdminChat(player, message);
                return;
            }
        }

        object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            if (!ActiveAdminChat.Contains(arg.Player()))
                return null;

            if (ActiveAdminChat.Contains(arg.Player()))
            {
                string message = arg.Args[0];
                SendToAdminChat(arg.Player(), message);
                return true;
            }

            return null;
        }

        #endregion

        #region Data Storage

        void writeFile() { Interface.Oxide.DataFileSystem.WriteObject(DataFile, storedData); }
        void openDataFile() { storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(DataFile); }

        class StoredData
        {
            public List<string> ChatLog = new List<string>();
        }

        #endregion

    }

}