using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using System.Globalization;
using System;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("SimpleBroadcast", "S1m0n", "1.0.0")]
    [Description("A simple chat broadcast plugin.")]

    public class SimpleBroadcast : RustPlugin
    {
        string PrefixText;
        string PrefixColour;
        string TextColour;
        bool PrefixActive;

        [ChatCommand("bcast")] void onCommandBcast(BasePlayer player, string command, string[] args) => onCommandBroadcast(player, command, args);
        [ChatCommand("bc")] void onCommandBc(BasePlayer player, string command, string[] args) => onCommandBroadcast(player, command, args);
        [ChatCommand("broadcast")]
        void onCommandBroadcast(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) { PrintToChat(player, "[SimpleBroadcast] Not enough permissions."); return; }
            if (args.Length == 0) { showHelp(player); return; }

            string msg = "";
            foreach (string s in args)
                msg += $"{s} ";
            broadcastMessage(msg);
        }

        [ChatCommand("bcreload")]
        void onCommandReload(BasePlayer player, string command, string[] args)
        {
            LoadConfig();
            getConfigValues();
            PrintToChat(player, "<color={PrefixColour}>[SimpleBroadcast]</color> Конфиг был перезагружен.");
        }

        protected override void LoadDefaultConfig()
        {
            Puts("[SimpleBroadcast] Generating default configuration file...");
            Config.Clear();

            Config["Broadcast-Prefix"] = true;
            Config["Broadcast-Prefix-Format"] = "[ИНФО]";
            Config["Broadcast-Prefix-Colour"] = "#66ff66";
            Config["Broadcast-Text-Colour"] = "#ffffff";

            Puts("[SimpleBroadcast] Config file generated!");
            SaveConfig();
        }

        void Loaded()
        {
            getConfigValues();
        }

        void getConfigValues()
        {
            PrefixActive = (bool)Config["Broadcast-Prefix"];
            PrefixColour = Config["Broadcast-Prefix-Colour"].ToString();
            TextColour = Config["Broadcast-Text-Colour"].ToString();
            PrefixText = Config["Broadcast-Prefix-Format"].ToString();
        }

        void broadcastMessage(string message)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                sendMessage(player, message);
        }

        void sendBMessage(BasePlayer player, string message, bool prefix=true)
        {
            if (prefix)
                PrintToChat(player, $"<color={PrefixColour}>[SimpleBroadcast]</color> <color={TextColour}>{message}</color>");
            else
                PrintToChat(player, $"{message}");
        }

        void sendMessage(BasePlayer player, string message)
        {
            if (PrefixActive)
                PrintToChat(player, $"<color={PrefixColour}>{PrefixText}</color> <color={TextColour}>{message}</color>"); else
                PrintToChat(player, $"<color={TextColour}>{message}</color>");
        }

        string addColour(string colour, string message)
        {
            return $"<color={colour}>{message}</color>";
        }

        void showHelp(BasePlayer player)
        {
            sendBMessage(player, "<size=24>SimpleBroadcast</size>\n" + addColour("", $" <size=20><color=orange>Доступные команды:</color></size>\n") + addColour("#159e47", "<color=#ffd479>/broadcast <сообщение></color>") + "\nЭта команда передает сообщение всему серверу. Это можно настроить в конфигурационном файле <color=#ffd479>SimpleBroadcast.json</color>. \nВы также можете использовать <color=#ffd479>/bcast <сообщение></color> и <color=#ffd479>/bc <сообщение></color>. \nИспользуйте <color=#ffd479>/bcreload</color>, для загрузки конфига после его изменения.", false);
        }
    }

}