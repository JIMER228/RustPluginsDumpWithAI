// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using ConVar;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static ConsoleSystem;

namespace Oxide.Plugins
{
    [Info("SvFastMenu", "BUDAPESHTER#9999", "1.7.0")]
    class SvFastMenu : RustPlugin
    {
        public string Layer = "SvFastMenu";
        public Dictionary<string, int> playerStat = new Dictionary<string, int>();
        #region DATA,HOOK
        private void Init()
        {   permission.RegisterPermission("SvFastMenu.use", this);
            config = Config.ReadObject<PluginConfig>();
            LoadData();
        }
        private void OnServerSave()
        {
            SaveCom();
        }
        private void SaveCom()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}/PlayerList", playerStat);
        }
        private void LoadData()
        {
            try
            {
                playerStat = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, int>>($"{Name}/PlayerList");
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }
            if (playerStat == null) playerStat = new Dictionary<string, int>();
        }
        void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!playerStat.ContainsKey($"{player.userID}") && permission.UserHasPermission(player.UserIDString, "SvFastMenu.use"))
                {
                    Draw(player);
                }
            }
        }
        void Unload()
        {
            SaveCom();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, Layer);
            }
        }
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            CuiHelper.DestroyUi(player, Layer);
        }
        private void OnPlayerConnected(BasePlayer player)
        {
            if (!playerStat.ContainsKey($"{player.userID}") && permission.UserHasPermission(player.UserIDString, "SvFastMenu.use"))
            {
                Draw(player);
            }
        }
        #endregion
        #region Config
        private PluginConfig config;
        protected override void LoadConfig() //load config
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }
        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }
        private class PluginConfig
        {
            public string Overlay;
            public string ChatTag;
            public string AnchorMin_Main;
            public string AnchorMax_Main;
            public int FontSize;
            public string FontName;
            public List<ButtonConfig> Buttons;
        }
        public class ButtonConfig
        {
            public string ButtonName;
            public string Command;
            public string ColorButton;
            public string ColorText;
        }
        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                Overlay = "Overlay",
                AnchorMin_Main = "0.3696925 0.001302288",
                AnchorMax_Main = "0.6125930 0.01822916",
                FontSize = 10,
                FontName = "robotocondensed-bold.ttf",
                ChatTag = "SVFAST" ,
                Buttons = new List<ButtonConfig>
                {
                    new ButtonConfig { ButtonName = "UP", Command = "/up 4", ColorButton = "0.24 0.29 0.15 0.9", ColorText = "1 1 1 0.89" },
                    new ButtonConfig { ButtonName = "TURRET", Command = "/turret", ColorButton = "0.24 0.29 0.15 0.9", ColorText = "1 1 1 0.89" },
                    new ButtonConfig { ButtonName = "LSAVE", Command = "/load save", ColorButton = "0.24 0.29 0.15 0.9", ColorText = "1 1 1 0.89" },
                    new ButtonConfig { ButtonName = "NOMINI", Command = "/nomini", ColorButton = "0.24 0.29 0.15 0.9" , ColorText = "1 1 1 0.89"},
                    new ButtonConfig { ButtonName = "MINI", Command = "/mymini", ColorButton = "0.24 0.29 0.15 0.9", ColorText = "1 1 1 0.89" },
                }
            };
        }
        #endregion
        #region ADMINUI
        [ChatCommand("3")]
        private void SaveConf(BasePlayer player)
        {

        }
        [ChatCommand("1")]
        private void FastAdd1(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Main_Fast_Menu");
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0.4352942 0.5372549 0.2588235 0.3215686", Material = "assets/content/ui/uibackgroundblur.mat" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-506.121 -291.611", OffsetMax = "493.031 307.488" }
            }, "Overlay", "Main_Fast_Menu");

            int row = 0;
            int col = 0;
            float rowHeight = 50f;
            float colWidth = 182.49f;
            float initialOffsetY = 231.286f;

            for (int i = 0; i < config.Buttons.Count; i++)
            {
                if (i % 5 == 0 && i != 0)
                {
                    row++;
                    col = 0;
                }
                float offsetX = -452.766f + (colWidth * col);
                float offsetY = initialOffsetY - (rowHeight * row);

                container.Add(new CuiElement
                {
                    Name = $"Label_{i}",
                    Parent = "Main_Fast_Menu",
                    Components = {
                new CuiTextComponent { 
                    Text = config.Buttons[i].ButtonName,
                    Font = "robotocondensed-regular.ttf", 
                    FontSize = 14,
                    Align = TextAnchor.MiddleCenter, 
                    Color = "1 1 1 1" 
                },
                new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                new CuiRectTransformComponent 
                { 
                    AnchorMin = "0.5 0.5", 
                    AnchorMax = "0.5 0.5",
                    OffsetMin = $"{offsetX} {offsetY - 23.628f}",
                    OffsetMax = $"{offsetX + colWidth} {offsetY}" 
                }
            }
                });

                container.Add(new CuiElement
                {
                    Name = $"InputField_{i}",
                    Parent = "Main_Fast_Menu",
                    Components = {
        new CuiInputFieldComponent {
            Color = "1 1 1 1",
            Text = config.Buttons[i].Command,
            Font = "robotocondensed-regular.ttf",
            FontSize = 14,
            Align = TextAnchor.MiddleCenter,
            CharsLimit = 0,
            IsPassword = false,
            Command = $"SaveCommandInput {i} {{value}}"
        },
        new CuiRectTransformComponent {
            AnchorMin = "0.5 0.5",
            AnchorMax = "0.5 0.5",
            OffsetMin = $"{offsetX} {offsetY - 60}",
            OffsetMax = $"{offsetX + colWidth} {offsetY - 60 + rowHeight}"
        }
    }
                });

                col++;
            }

            CuiHelper.AddUi(player, container);
        }
        [ConsoleCommand("SaveCommandInput")]
        private void SaveCommandInput(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            int buttonIndex = arg.GetInt(0); // Получаем индекс кнопки из аргументов
            string inputText = arg.GetString(1); // Получаем текст из поля ввода из аргументов

            // Сохранение команды
            config.Buttons[buttonIndex].Command = inputText;

            // Сохранение конфигурации
            SaveConfig();

            // Возможно, обновление пользовательского интерфейса для отображения новой команды
            // ...
        }
        [ChatCommand("2")]
        private void FastAdd2(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "Main_Fast_Menu");
        }
        #endregion
        #region UI COMMANDS
        [ChatCommand("fast")]
        private void FastAdd(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "SvFastMenu.use")) return;
            int value;
            string playerKey = player.userID.ToString();
            if (playerStat.TryGetValue(playerKey, out value))
            {
                playerStat.Remove(playerKey);
                Draw(player);
                SendChat(player, String.Format(lang.GetMessage("AddPanel", this), config.ChatTag));
            }
            else
            {
                playerStat[playerKey] = 1;
                FastNo(player);
                SendChat(player, String.Format(lang.GetMessage("RemPanel", this), config.ChatTag));
            }
        }
        private void FastNo(BasePlayer player)
        {
           CuiHelper.DestroyUi(player, Layer);
        }
        private void Draw(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
            Image = {Color = "0 0 0 0" },
            RectTransform =
                {
                   AnchorMin = config.AnchorMin_Main,AnchorMax = config.AnchorMax_Main},CursorEnabled = false,
                }, config.Overlay, Layer);
            // Store common properties in variables
            var fontSize = config.FontSize;
            var fontName = config.FontName;
            var buttonWidth = 0.1997638 - 0.001181223; // The total width of the button (excluding the 1-pixel gap between buttons)
            var gap = buttonWidth * 0.025; // The width of the 1-pixel gap between buttons
            float buttonCount = config.Buttons.Count;
            float totalWidth = (float)((buttonCount * buttonWidth) + ((buttonCount - 1) * gap));
            float panelStartPosition = 0.5f - (totalWidth / 2);
            // Use a loop to generate buttons
            for (int i = 0; i < config.Buttons.Count; i++)
            {
                var buttonConfig = config.Buttons[i];
                var buttonName = buttonConfig.ButtonName;
                var command = buttonConfig.Command;
                // Use StringBuilder to build the AnchorMin and AnchorMax properties
                var anchorMin = new StringBuilder();
                anchorMin.AppendFormat("{0} {1}", panelStartPosition + (i * (buttonWidth + gap)), 0);
                var anchorMax = new StringBuilder();
                anchorMax.AppendFormat("{0} {1}", panelStartPosition + (i * (buttonWidth + gap)) + buttonWidth, 0.9392304);
                container.Add(new CuiButton
                {
                RectTransform =
                {
                AnchorMin = anchorMin.ToString(),
                AnchorMax = anchorMax.ToString(),
                },
                Button =
                {
                Color = buttonConfig.ColorButton,
                Command = "commander " + command,
                },
                Text =
                {
                Text = buttonName,
                Color = buttonConfig.ColorText,
                FontSize = fontSize,
                Align = TextAnchor.MiddleCenter,
                Font = fontName,
                }
                }, Layer);
            }
            CuiHelper.AddUi(player, container);
        }
        #endregion
        #region ConCommand
        [ConsoleCommand("commander")]
        void cmdRUnPlayerCommand(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;
            if (args.FullString.Contains("/"))
            {
                player.Command("chat.say", args.FullString);
            }
            else
                player.Command(args.FullString);
        }
        // Создайте словарь для хранения аргументов
        private Dictionary<string, string> argumentStorage = new Dictionary<string, string>();

        [ConsoleCommand("com_ui")]
        void cmdRUnAdminUi(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;
            if (args.FullString.Contains("!"))
            {
                player.Command("chat.say", args.FullString);
            }
            else
                player.Command(args.FullString);
        }
        #endregion
        #region Help
        public void SendChat(BasePlayer player, string Message, Chat.ChatChannel channel = Chat.ChatChannel.Global) 
        {
            player.SendConsoleCommand("chat.add", channel, 0, Message);
        }
        #endregion
        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AddPanel"] = "<size=14><color=#ffd500>[{0}]</color>PANEL IS OPEN</size>",
                ["RemPanel"] = "<size=14><color=#ffd500>[{0}]</color>PANEL IS CLOSED</size>"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AddPanel"] = "<size=14><color=#ffd500>[{0}]</color>ПАНЕЛЬ ОТКРЫТА</size>",
                ["RemPanel"] = "<size=14><color=#ffd500>[{0}]</color>ПАНЕЛЬ ЗАКРЫТА</size>"
            }, this, "ru");
        }
        #endregion
    }
}
