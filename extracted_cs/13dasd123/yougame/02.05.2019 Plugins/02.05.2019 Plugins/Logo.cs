// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using System;
using System.Globalization;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Logo", "Ryamkk", "1.0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    class Logo : RustPlugin
    {
		string LogoName1 = "ONION - ADVANCE CLASSIC";
		string LogoName2 = "НАЖМИТЕ ЧТОБЫ ОТКРЫТЬ МЕНЮ";
		string LogoCommand = "chat.say /menu";
		
		private void LoadDefaultConfig()
        {
            GetConfig("Настройки лога", "Названия сервера", ref LogoName1);
            GetConfig("Настройки лога", "Названия кнопки", ref LogoName2);
            GetConfig("Настройки лога", "Чат команда 'Пример: chat.say /help'", ref LogoCommand);
            SaveConfig();
        }
		
        void OnServerInitialized()
        {
			LoadDefaultConfig();
            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerInit(player);
        }
        
        void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() =>
                {
                    OnPlayerInit(player);
                    return;
                });
            }
            
            DrawGUI(player);
        }
        
        void DrawGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "LogoPanel");
            
            var container = new CuiElementContainer();
            var Panel = container.Add(new CuiPanel
            {
                Image = { Color = $"0 0 0 0" },
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1",  OffsetMin = "10 -35", OffsetMax = "180 -15" },
                CursorEnabled = false,
            }, "Overlay", "LogoPanel");
            
            container.Add(new CuiElement
            {
                Parent = "LogoPanel",
                Components = {
                    new CuiTextComponent() { Color = HexToCuiColor("#FFFFFF5A"), FadeIn = 1f, Text = LogoName1, FontSize = 20, Align = TextAnchor.UpperLeft, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "3 1.5" },
                }
            });
            
            container.Add(new CuiElement
            {
                Parent = "LogoPanel",
                Components = {
                    new CuiTextComponent() { Color = HexToCuiColor("#FFFFFF5A"), FadeIn = 1f, Text = LogoName2, FontSize = 12, Align = TextAnchor.LowerLeft, Font = "robotocondensed-bold.ttf" },
                    new CuiRectTransformComponent { AnchorMin = "0 -0.1", AnchorMax = "2 1" },
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 2" },
                Button = { Command = LogoCommand, Color = "0 0 0 0"},
                Text = { Text = "" }
            }, "LogoPanel");
            
            CuiHelper.AddUi(player, container);
        }
		
        private static string HexToCuiColor(string hex)
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
 
            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
		
		private void GetConfig<T>(string menu, string Key, ref T var)
        {
            if (Config[menu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[menu, Key], typeof(T));
            }

            Config[menu, Key] = var;
        }
    }
}
