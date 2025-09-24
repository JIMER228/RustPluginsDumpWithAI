using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SpeedGuess", "S1m0n", "1.1.1")]
    class SpeedGuess : RustPlugin
    {
        private ConfigData config;

        private readonly string Prefix = "[Игра на скорость]";
        private readonly string TextColour = "#ffffff";
        private readonly string HighlightColour = "#00ffff";
        private readonly string PrefixColour = "#0099ff";
        private readonly string PermissionMaster = "speedguess.*";
        private readonly string PermissionStart = "speedguess.start";
        private readonly string PermissionStop = "speedguess.stop";
        private readonly string Alphabet = "abcdefghijklmnopqrstuvwxyz";
        private readonly string Symbols = "!£$%^&*#@=+";

        bool EventActive = false;
        string EventString = "";
        System.Random random = new System.Random();

        void Init()
        {
            LoadDefaultConfig();
            LoadConfig();
            RegisterPermissions();
        }

        void GlobalMessage(string message, bool prefix = true)
        {
            string prefixFormat = $"<color={PrefixColour}>{Prefix}</color> ";
            string messageFormat = $"<color={TextColour}>{message}</color>";

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (prefix)
                    player.ChatMessage(prefixFormat + messageFormat); else
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
            permission.RegisterPermission(PermissionStart, this);
            permission.RegisterPermission(PermissionStop, this);
        }

        [ChatCommand("guess")]
        void OnCommandGuess(BasePlayer player, string command, string[] args)
        {
            if (!EventActive) { SendMessage(player, "В настоящее время нет догадки для игры."); return; }
            if (args.Length != 1) { SendMessage(player, "Неправильно. Попробуй <color=#ffd479>/guess {YourText}</color>"); return; }

            if (args[0].ToLower() == EventString.ToLower())
            {
                DeclareWinner(player);
                StopEvent();
                return;
            }

            SendMessage(player, "Неверно, попробуй ещё раз!");
            return;
        }

        [ChatCommand("sg")]
        void OnCommandMain(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 1) { SendMessage(player, "Incorrect arguments."); return; }
            string userCommand = args[0].ToLower();

            switch (userCommand)
            {
                case "start":
                    if (!HasPermission(player, PermissionStart)) { SendMessage(player, "У вас нет прав, чтобы использовать эту команду."); return; }
                    if (EventActive) { SendMessage(player, "Ивент уже начался."); return; }
                    StartEvent();
                    break;

                case "stop":
                    if (!HasPermission(player, PermissionStop)) { SendMessage(player, "У вас нет прав, чтобы использовать эту команду."); return; }
                    if (!EventActive) { SendMessage(player, "В настоящее время активных ивентов нет."); return; }
                    StopEvent();
                    break;

                case "reload":
                    if (!HasPermission(player, PermissionStop)) { SendMessage(player, "У вас нет прав, чтобы использовать эту команду."); return; }
                    LoadConfig();
                    SendMessage(player, "Конфиг успешно перезагружен.");
                    break;
            }
        }

        void StartEvent()
        {
            EventActive = true;
            EventString = GenerateString();
            GlobalMessage($"<size=24>Ивент начался!</size>\n<size=16><color={PrefixColour}>Игра на скорость</color>\n</size>\n" + $"Первый игрок который введет в чат:\n<color={HighlightColour}>/guess {EventString}</color>\nполучит приз!", false);
        }

        void StopEvent()
        {
            EventActive = false;
            EventString = "";
            GlobalMessage($"Игра на скороть завершена.");
        }

        void DeclareWinner(BasePlayer player)
        {
            var itemAmount = random.Next(config.MinimumRewardAmount, config.MaximumRewardAmount);
            var winList = new List<string>();
            var winListAmt = new List<int>();
            var message = "";

            for (int i = 0; i < itemAmount; i++)
            {
                var val = random.Next(0, config.PossibleRewards.Count);
                var key = config.PossibleRewards.ElementAt(val).Key;
                var value = config.PossibleRewards.ElementAt(val).Value;
                winList.Add(GiveItem(player, key, value).info.displayName.english);
                winListAmt.Add(value);
            }

            int a = 0;
            foreach (string s in winList)
            {
                message += "\n(";
                message += $"{winListAmt[a]}x ";
                message += s;
                message += ")";
                a++;
            }

            if (!config.BroadcastRewards)
            {
                GlobalMessage($"<color={HighlightColour}>{player.displayName}</color> правильно ввел первым <color={HighlightColour}>{EventString}</color> и выиграл приз! Отлично сработано :^)");
                SendMessage(player, $"Вы были выиграли: {message}");
            }
            else
            {
                GlobalMessage($"<color={HighlightColour}>{player.displayName}</color> правильно ввел первым <color={HighlightColour}>{EventString}</color> и выиграл приз: {message}");
            }
            return;
        }

        Item GiveItem(BasePlayer player, string item, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                TryGiveItem(player, item);
            }
            return ItemManager.CreateByPartialName(item, amount);
        }

        Item TryGiveItem(BasePlayer player, string shortName)
        {
            var belt = getInv(player, "belt");
            var main = getInv(player, "main");

            var item = ItemManager.CreateByPartialName(shortName, 1);

            foreach (Item i in main.itemList)
                if (
                    i.info.shortname == shortName &&
                    i.amount < i.MaxStackable()
                    )
                {
                    item.MoveToContainer(main);
                    return null;
                }

            if (main.itemList.Count < main.capacity)
            {
                item.MoveToContainer(main);
                return null;
            }

            if (belt.itemList.Count == 0)
            {
                item.MoveToContainer(belt);
                return null;
            }

            foreach (Item i in belt.itemList)
                if (
                    i.info.shortname == shortName &&
                    i.amount < i.MaxStackable()
                    )
                {
                    item.MoveToContainer(belt);
                    return null;
                }
                else
                {
                    if (belt.itemList.Count < belt.capacity)
                    {
                        item.MoveToContainer(belt);
                        return null;
                    }
                    else
                    {
                        return item;
                    }

                }
            return null;
        }

        ItemContainer getInv(BasePlayer player, string type)
        {
            var inv = player.inventory;
            switch (type)
            {
                case "main":
                    return inv.containerMain;
                case "belt":
                    return inv.containerBelt;
                case "wear":
                    return inv.containerWear;
            }
            return null;
        }

        string GenerateString()
        {
            int stringLength = random.Next(config.MinimumStringLength, config.MaximumStringLength);
            string _string = "";
            for (int i = 0; i < stringLength; i++)
            {
                if (random.Next(100) > 25)
                    _string += ChooseLetter(); else
                    _string += ChooseNumber();
            }
            return _string;
        }

        char ChooseLetter()
        {
            if (config.UseSymbols)
                if (random.Next(100) > 20)
                return Alphabet[random.Next(Alphabet.Length)]; else
                return Symbols[random.Next(Symbols.Length)];

            return Alphabet[random.Next(Alphabet.Length)];
        }
        int ChooseNumber() => random.Next(0, 9);

        #region Configuration & Lang

        public class ConfigData
        {
            [JsonProperty(PropertyName = "ConfigurationValues")]
            public Dictionary<string, int> PossibleRewards;
            public int MaximumRewardAmount = 3;
            public int MinimumRewardAmount = 1;
            public int MaximumStringLength = 15;
            public int MinimumStringLength = 10;
            public bool UseSymbols = true;
            public bool BroadcastRewards = true;

            public static ConfigData DefaultConfig()
            {
                return new ConfigData
                {
                    PossibleRewards = new Dictionary<string, int>
                    {
                        ["ammo.rifle"] = 125,
                        ["ammo.pistol.hv"] = 100,
                        ["apple"] = 1,
                        ["gears"] = 7,
                        ["surveycharge"] = 5,
                        ["tarp"] = 15,
                        ["rifle.ak"] = 1,
                        ["rifle.bolt"] = 1,
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<ConfigData>();
            SaveConfig();
        }
        protected override void LoadDefaultConfig() => config = ConfigData.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

    }

}