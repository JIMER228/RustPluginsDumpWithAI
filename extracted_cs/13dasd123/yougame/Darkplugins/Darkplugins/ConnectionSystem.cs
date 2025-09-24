// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("Connection System", "https://discord.gg/dNGbxafuJn", "1.0.0")]
    public class ConnectionSystem : RustPlugin
    {
        #region Varibles

        private class Response
        {
            [JsonProperty("country")] public string Country { get; set; }
        }

        #endregion
        
        #region Configuration

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Включить отображение отключаемых игроков?")]
            public bool showdisconnect = true;

            [JsonProperty(PropertyName = "Отображать логи в консоли?")]
            public bool showlogconsole = true;

            [JsonProperty(PropertyName = "Цвет никнейма игрока")]
            public string NameColor = "#fff";

            [JsonProperty(PropertyName = "Включить GUI")]
            public bool ShowGUI = true;

            [JsonProperty(PropertyName = "Цвет фона UI")]
            public string ColorPanelUI = "#2F2F2F77";

        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
            }
            catch (Exception e)
            {
                Puts(e.ToString());
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
            PrintWarning("Создание нового файла конфигурации...");
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion

        #region Helpers

        #region Broadcast

        private void Broadcast(BasePlayer player, string msgId, params string[] args)
        {
            foreach (var check in BasePlayer.activePlayerList)
                PrintToChat(check, lang.GetMessage(msgId, this, player.UserIDString), args);
        }

        #endregion

        #region ConnectMessage

        void SendJoinMessage(BasePlayer player)
        {
            if (_config.ShowGUI)
            {
                foreach (var VARIABLE in BasePlayer.activePlayerList)
                {
                    DrawUI(VARIABLE);
                }
            }
            else
            {
                string apiUrl = "http://ip-api.com/json/";

                webrequest.Enqueue(apiUrl, null, (code, response) =>
                {
                    if (code != 200 || response == null)
                    {
                        Puts($"WebRequest to {apiUrl} failed, sending connect message without the country.");
                        Broadcast(player, string.Format("<color=#55aaff>{0}</color> Присоедился к серверу", player.displayName));
                        if (_config.showlogconsole)
                        {
                            PrintToConsole(player, string.Format("<color=#55aaff>{0}</color> Присоедился к серверу", player.displayName));
                            Puts(string.Format("<color=#55aaff>{0}</color> Присоедился к серверу", player.displayName));
                        }
                        return;
                    }

                    string country = JsonConvert.DeserializeObject<Response>(response).Country;
                    Broadcast(player, string.Format("<color=#55aaff>{0} ({1})</color>  Присоедился к серверу ", player.displayName, country));
                    if (_config.showlogconsole)
                    {
                        PrintToConsole(player, string.Format("<color=#55aaff>{0} ({1})</color>  Присоедился к серверу ", player.displayName, country));
                        Puts(string.Format("<color=#55aaff>{0} ({1})</color>  Присоедился к серверу ", player.displayName, country));
                    }
                }, this);
            }
        }
        
        void OnPlayerInit(BasePlayer player)
        {
            SendJoinMessage(player);                
        }

        #endregion

        #region DisconnectMessage

        void SendDisconnectMessage(BasePlayer player, string reason)
        {
            Broadcast(player,
                string.Format("<color={2}>{0}</color> отключился от сервера ({1})", player.displayName, reason, _config.NameColor));
            if (_config.showlogconsole)
            {
                PrintToConsole(player,
                    string.Format("<color={2}>{0} ({1})</color>  отключился от сервера", player.displayName,
                        reason, _config.NameColor));
                Puts(string.Format("<color={2}>{0} ({1})</color>  отключился от сервера", player.displayName,
                    reason, _config.NameColor));
            }
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (_config.showdisconnect == true)
            {
                SendDisconnectMessage(player, reason);
            }
        }

        #endregion

        #endregion

        #region UI

        private void DrawUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "ConnectUI");
            var ConnectGUI = new CuiElementContainer();
            var ConnectUI = ConnectGUI.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image =
                {
                    Color = HexToRustFormat("#2F2F2F77"),
                },
                RectTransform =
                {
                    AnchorMin = "0.6567708 0.03703703",
                    AnchorMax = "0.8359375 0.1296296"
                }
            }, "Hud", "ConnectUI");
            ConnectGUI.Add(new CuiElement
            {
                Parent = "ConnectUI",
                Components =
                {
                    new CuiTextComponent
                    {
                        Text = $"{player.displayName} Присоединился",
                        Color = _config.NameColor,
                        Font = "robotocondensed-bold.ttf",
                        FontSize = 18,
                        Align = TextAnchor.MiddleCenter
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMax = "1 1",
                        AnchorMin = "0 0"
                    }
                }

            });
            CuiHelper.AddUi(player, ConnectGUI);
            timer.Once(5, () => { CuiHelper.DestroyUi(player, "ConnectUI"); });
        }

        #endregion
        
        #region Hooks

        void OnServerInitialized()
        {
            LoadConfig();
        }

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        #endregion
    }
}