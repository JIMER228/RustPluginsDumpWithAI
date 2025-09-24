// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System.Collections.Generic;
using Oxide.Core.Configuration;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("AlertsPlus", "S1m0n", "1.1.0")]

    class AlertsPlus : RustPlugin
    {

        #region Global Variables

        string usagePerm = "alertsplus.hide";
        string joinIcon = "";
        string leaveIcon = "";
        string firstMessageFormat;
        string joinMessageFormat;
        string leaveMessageFormat;
        string motdMessage;
        string motdColour;
        bool motdEnabled;
        bool joinLeaveIcons;
        string joinColour;
        string firstJoinColour;
        string leaveColour;
        string nameColour;
        string broadcastPrefix;
        bool broadcastPrefixEnabled;
        string broadcastPrefixColour;
        bool broadcastRandomMessages;
        int broadcastInterval;
        Timer autoBroadcastTimer;
        List<object> broadcastMessages = new List<object>();

        StoredData storedData;
        string DataFile = "Join_Data";
        string errorColour = "#c12c34";
        string mainColour = "#f4b342";
        string mainPrefix = "<color=#930702>[AlertsPlus]</color>";

        #endregion


        void Init()
        {
            openDataFile();
            LoadConfig();
            LoadDefaultConfig();
            LoadDefaultMessages();
            RegisterPermissions();
            initGlobals();

            foreach (BasePlayer p in BasePlayer.activePlayerList)
                if (getPlayerInfo(p) != null)
                    addNewPlayer(p, false);
        }

        void Loaded()
        {
            // foreach (BasePlayer player in BasePlayer.activePlayerList)
            //     updatePlayerInfo(player);

            if (broadcastRandomMessages)
                startTimer(broadcastInterval);
        }


        #region Player Commands

        [ChatCommand("who")]
        void OnCommandWho(BasePlayer sender, string command, string[] args) => OnCommandPlayers(sender, command, args);

        [ChatCommand("online")]
        void OnCommandOnline(BasePlayer sender, string command, string[] args) => OnCommandPlayers(sender, command, args);

        [ChatCommand("players")]
        void OnCommandPlayers(BasePlayer sender, string command, string[] args)
        {
            string playerList = color(mainColour, "Игроки в сети:");

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                playerList += $"\n- {player.displayName}";

            if (BasePlayer.activePlayerList.Count < 20)
                PrintToChat(sender, playerList);
            else
                PrintToChat(sender, "Список игроков слишком длинный для чата. \nНажмите F1, введите <color=#ffd479>/players</color> снова в чате, затем F1, чтобы увидеть полный список игроков.");
            sender.Command("echo", $"{playerList}");
        }

        #endregion


        #region Admin Commands

        [ChatCommand("whois")]
        void OnCommandWhois(BasePlayer sender, string command, string[] args)
        {
            if (!sender.IsAdmin) { sendMessage(sender, "NoPermission"); return; }

            BasePlayer target = getPlayer(args[0]);
            showPlayerInfo(target, sender);
        }

        [ChatCommand("forcebroadcast")]
        void OnCommandForce(BasePlayer sender, string command, string[] args)
        {
            if (!sender.IsAdmin) { sendMessage(sender, "NoPermission"); return; }
            broadcastRandomMessage();
        }

        [ChatCommand("ap")]
        void OnCommandAp(BasePlayer sender, string command, string[] args)
        {
            if (!sender.IsAdmin) { sendMessage(sender, "NoPermission"); return; }

            if (args.Length < 1)
            {
                sendError(sender, "Arguments", "/ap reload");
                return;
            }

            if (args[0] == "reload")
            {
                sendMessage(sender, "Reloaded");
                LoadConfig();
                autoBroadcastTimer.Destroy();
                initGlobals();
                startTimer(broadcastInterval);
                SaveConfig();
                return;
            }
        }

        #endregion


        #region Auto Broadcast

        void startTimer(int interval)
        {
            autoBroadcastTimer = timer.Every(interval, broadcastRandomMessage);
        }

        void broadcastRandomMessage()
        {
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                PrintToChat(p, getMessageFormat(p, "AutoBroadcast"));
        }

        string getRandomBroadcast()
        {
            int amount = broadcastMessages.Count;
            int ranNum = Oxide.Core.Random.Range(0, amount);

            return (string)broadcastMessages[ranNum];
        }

        #endregion


        #region Join / Leave Messages

        void OnPlayerInit(BasePlayer player)
        {
            addNewPlayer(player);
            sendJoinMessage(player);
            if (motdEnabled)
                sendMOTD(player);
            updatePlayerInfo(player);
        }

        void updatePlayerInfo(BasePlayer player)
        {
            PlayerInfo playerInfo = getPlayerInfo(player);
            playerInfo.displayName = player.displayName;
            playerInfo.addAlias(player.displayName);
            playerInfo.ipAddress = player.net.connection.ipaddress;
            writeFile();
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            sendLeaveMessage(player);
        }

        void sendJoinMessage(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, usagePerm))
                return;

            string text = getMessageFormat(player, "Join");
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                PrintToChat(p, text);
        }

        void sendLeaveMessage(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, usagePerm))
                return;

            string text = getMessageFormat(player, "Leave");
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                PrintToChat(p, text);
        }

        void sendMOTD(BasePlayer player)
        {
            string text = getMessageFormat(player, "Motd");
            PrintToChat(player, text);
        }

        #endregion


        #region Player Information

        void showPlayerInfo(BasePlayer player, BasePlayer sender)
        {
            PlayerInfo playerInfo = getPlayerInfo(player);
            string names = "\n";
            foreach (string name in playerInfo.Aliases)
                names += $"- {name}\n";

            string message = $"Информация о игроке: {color("#66ff66", player.displayName)}." +
                $"\nSteamID: {color("#66ff66", playerInfo.userID.ToString())}\nОС: {color("#66ff66", player.net.connection.os)}\nIP: {color("#66ff66", player.net.connection.ipaddress)}\nСоюз:{color("#66ff66", names)}";
            PrintToChat(sender, message);
        }

        void addNewPlayer(BasePlayer player, bool firstTime = true)
        {
            if (getPlayerInfo(player) != null)
                return;
            var playerInfo = new PlayerInfo(player.displayName, player.userID);
            playerInfo.newPlayer = firstTime;
            storedData.Players.Add(playerInfo);
            Puts("[AP] New Player Added.");
            writeFile();
        }

        #endregion


        #region Config, Saved Data & Helpers

        void RegisterPermissions()
        {
            permission.RegisterPermission(usagePerm, this);
        }

        void initGlobals()
        {
            LoadConfig();
            firstMessageFormat = (string)Config["Формат сообщения о первом присоеденение"];
            joinMessageFormat = (string)Config["Формат сообщения при подключение"];
            leaveMessageFormat = (string)Config["Формат сообщения при отключение"];
            joinLeaveIcons = (bool)Config["Использование иконок"];
            joinColour = (string)Config["Цвет сообщения подключения"];
            leaveColour = (string)Config["Цвет сообщения об отключение"];
            firstJoinColour = (string)Config["Цвет сообщения для первого присоединения"];
            nameColour = (string)Config["Цвет имени игрока"];
            motdEnabled = (bool)Config["Включение MOTD"];
            motdMessage = (string)Config["MOTD"];
            motdColour = (string)Config["Цвет MOTD"];
            broadcastMessages = (List<object>)Config["Автоматические сообщения"];
            broadcastPrefix = (string)Config["Префикс"];
            broadcastPrefixEnabled = (bool)Config["Включить префикс новостей"];
            broadcastPrefixColour = (string)Config["Цвет префикса сообщений"];
            broadcastRandomMessages = (bool)Config["Включить случайные сообщения"];
            broadcastInterval = (int)Config["Интервал сообщений"];
        }

        protected override void LoadDefaultConfig()
        {
            Config["Использование иконок"] = true;
            Config["Цвет сообщения подключения"] = "#66ff66";
            Config["Формат сообщения при подключение"] = "{PLAYER} зашёл на сервер.";
            Config["Цвет имени игрока"] = "#ffffff";
            Config["Цвет сообщения об отключение"] = "#c12c34";
            Config["Цвет сообщения для первого присоединения"] = "#66ff66";
            Config["Формат сообщения при отключение"] = "{PLAYER} покинул сервер.";
            Config["Формат сообщения о первом присоеденение"] = "{PLAYER} зашёл на сервер!";
            Config["Включение MOTD"] = true;
            Config["Цвет MOTD"] = "#357dce";
            Config["MOTD"] = "Эй, {PLAYER}!{LINE}Не забудьте проголосовать за потрясающие награды!{LINE}Вы можете проголосовать командой /vote";
            Config["Автоматические сообщения"] = GenerateDefaultBroadcastMessages();
            Config["Включить префикс новостей"] = true;
            Config["Префикс"] = "[ИНФО]";
            Config["Цвет префикса сообщений"] = "#66ff66";
            Config["Включить случайные сообщения"] = true;
            Config["Интервал сообщений"] = 30;
            SaveConfig();
        }

        List<string> GenerateDefaultBroadcastMessages()
        {
            List<string> defaultMessages = new List<string>
            {
                "Добро пожаловать на сервер!",
                "Разработчик плагина @ S1m0n(vk.com/rust_and_hw)",
                "Удачи вам в игре!"
            };
            return defaultMessages;
        }

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "У вас нет прав, чтобы использовать эту команду.",
                ["Arguments"] = "Неверный синтаксис, поробуй {0}",
                ["Reloaded"] = "Конфиг успешно загружен."
            }, this);
        }

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        void writeFile() { Interface.Oxide.DataFileSystem.WriteObject(DataFile, storedData); }

        void openDataFile() { storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(DataFile); }

        #endregion


        #region Formatting

        string color(string color, string text)
        {
            return $"<color={color}>{text}</color>";
        }

        string getMessageFormat(BasePlayer player, string type)
        {
            PlayerInfo playerInfo = getPlayerInfo(player);
            if (type == "Join")
            {
                if (playerInfo.newPlayer)
                {
                    string message = firstMessageFormat.Replace("{PLAYER}", $"{color(nameColour, player.displayName)}");
                    getPlayerInfo(player).newPlayer = false;
                    writeFile();
                    if (joinLeaveIcons)
                        return $"{joinIcon} {color(firstJoinColour, message)}";
                    return $"{message}";
                }
                else
                if (!playerInfo.newPlayer)
                {
                    string message = joinMessageFormat.Replace("{PLAYER}", $"{color(nameColour, player.displayName)}");
                    if (joinLeaveIcons)
                        return $"{joinIcon} {color(joinColour, message)}";
                    return $"{message}";
                }
            }
            else
            if (type == "Leave")
            {
                string message = leaveMessageFormat.Replace("{PLAYER}", $"{color(nameColour, player.displayName)}");
                if (joinLeaveIcons)
                    return $"{leaveIcon} {color(leaveColour, message)}";
                return $"{message}";
            }
            else
            if (type == "Motd")
            {
                string message = motdMessage.Replace("{PLAYER}", $"{color(nameColour, player.displayName)}");
                message = message.Replace("{LINE}", "\n");
                return $"{color(motdColour, message)}";
            }
            else
            if (type == "AutoBroadcast")
            {
                string message = getRandomBroadcast().Replace("{LINE}", "\n");
                if (broadcastPrefixEnabled)
                    return $"{color(broadcastPrefixColour, broadcastPrefix)} {message}";
                return message;
            }

            return "<Error>";
        }

        #endregion


        void sendError(BasePlayer player, string type, string extra = "")
        {
            PrintToChat(player, mainPrefix + " " + color(errorColour, Lang(type, player.UserIDString, extra)));
        }

        void sendMessage(BasePlayer player, string type, string extra = "")
        {
            PrintToChat(player, mainPrefix + " " + color(mainColour, Lang(type, player.UserIDString, extra)));
        }

        PlayerInfo getPlayerInfo(BasePlayer player)
        {
            foreach (PlayerInfo p in storedData.Players)
                if (p.userID == player.userID)
                    return p;
            return null;
        }

        BasePlayer getPlayer(PlayerInfo player)
        {
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                if (p.userID == player.userID)
                    return p;
            return null;
        }

        BasePlayer getPlayer(string partialName)
        {
            foreach (BasePlayer p in BasePlayer.activePlayerList)
                if (p.displayName.ToLower() == partialName.ToLower())
                    return p;
            return null;
        }

        class PlayerInfo
        {
            public string displayName;
            public ulong userID;
            public bool newPlayer = true;
            public string ipAddress;
            public List<string> Aliases = new List<string>();

            public PlayerInfo(string name, ulong id)
            {
                this.displayName = name;
                this.userID = id;
                this.Aliases.Add(name);
            }
            public void addAlias(string name)
            {
                foreach (string s in Aliases)
                    if (s == name)
                        return;
                Aliases.Add(name);
            }
        }

        class StoredData
        {
            public List<PlayerInfo> Players = new List<PlayerInfo>();
        }

    }

}