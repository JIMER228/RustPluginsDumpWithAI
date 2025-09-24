using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("CLogo", "rust-plug.ru", "1.0.0")]
    public class CLogo : RustPlugin
    {
        private const string UIMain = "UI.TopPanel";
        private Configuration config;

        class Configuration
        {
            [JsonProperty("Цвет основной панели")]
            public string PanelColor = "0 0 0 0";

            [JsonProperty("Цвет текста")]
            public string TextColor = "1 1 1 1";

            [JsonProperty("Название сервера")]
            public string ServerName = "Название сервера";

            [JsonProperty("Максимальное количество игроков")]
            public string MaxPlayers = "50";
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintError("Ошибка чтения конфигурации! Создаю новую...");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private void CreateTopPanel(BasePlayer player)
        {
            var elements = new CuiElementContainer();

            elements.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.01 0.01", AnchorMax = "0.35 0.035" },
                CursorEnabled = false
            }, "Overlay", UIMain);

            elements.Add(new CuiPanel
            {
                Image = { Color = "0.5 0.5 0.5 0.3" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, UIMain, "Background");

            elements.Add(new CuiButton
            {
                Button = { Command = "chat.say /menu", Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0.02 0", AnchorMax = "0.12 1" },
                Text = { Text = "/MENU", Color = config.TextColor, FontSize = 10, Align = TextAnchor.MiddleLeft }
            }, "Background");

            elements.Add(new CuiLabel
            {
                Text = { Text = config.ServerName, Color = config.TextColor, FontSize = 10, Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = "0.14 0", AnchorMax = "0.32 1" }
            }, "Background");

            elements.Add(new CuiLabel
            {
                Text = { Text = $"Онлайн: {BasePlayer.activePlayerList.Count}", Color = config.TextColor, FontSize = 10, Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = "0.34 0", AnchorMax = "0.52 1" }
            }, "Background");

            elements.Add(new CuiLabel
            {
                Text = { Text = $"Спящих: {BasePlayer.sleepingPlayerList.Count}", Color = config.TextColor, FontSize = 10, Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = "0.54 0", AnchorMax = "0.72 1" }
            }, "Background");

            int connectingPlayers = 0;
            elements.Add(new CuiLabel
            {
                Text = { Text = $"Подключаются: {connectingPlayers}", Color = config.TextColor, FontSize = 10, Align = TextAnchor.MiddleLeft },
                RectTransform = { AnchorMin = "0.74 0", AnchorMax = "0.98 1" }
            }, "Background");

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, elements);
        }

        private int GetConnectingPlayers()
        {
            return 0;
        }

        void OnPlayerConnected(BasePlayer player)
        {
            CreateTopPanel(player);
            foreach (var p in BasePlayer.activePlayerList)
            {
                if (p != player)
                    CreateTopPanel(p);
            }
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, UIMain);
            foreach (var p in BasePlayer.activePlayerList)
                CreateTopPanel(p);
        }

        private void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CreateTopPanel(player);
            }

            timer.Every(10f, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    CreateTopPanel(player);
                }
            });
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, UIMain);
            }
        }

        [ChatCommand("menu")]
        private void CmdMenu(BasePlayer player)
        {
            PrintToChat(player, "Открытие меню панели");
        }
    }
}