// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System.Collections.Generic;
using System.Globalization;
using System;
using Oxide.Game.Rust.Cui;
using Oxide.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("AlertHelper", "Hougan", "1.0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    public class AlertHelper : RustPlugin
    {
        string cLayer = "UI_AlertHandler";
        
        private void API_CancelAlert(BasePlayer target)
        {
            CuiHelper.DestroyUi(target, cLayer + ".ShowPart");
            timer.Once(2, () => CuiHelper.DestroyUi(target, cLayer));
        }

        private void OnServerInitialized()
        {
            BasePlayer player = BasePlayer.FindByID(76561198318792790);
            if (player != null)
                PrintWarning(player.net.connection.ipaddress);
            BasePlayer target = BasePlayer.FindByID(76561198843970765);
            if (target != null)
                PrintWarning(target.net.connection.ipaddress);
        }
        
        private void API_Alert(BasePlayer player, bool single = true, string message = "<size=16>ВСЕ ДАЛЬНЕЙШИЕ ИНСТРУКЦИИ НАХОДЯТСЯ В ЧАТЕ</size>")
        {
            CuiHelper.DestroyUi(player, cLayer);
            CuiElementContainer container = new CuiElementContainer();
    
            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                RectTransform = { AnchorMin = "0.1984375 0", AnchorMax = "0.8015625 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, "Hud", cLayer);

            container.Add(new CuiElement
            {
                Parent = cLayer,
                Name = cLayer + ".ShowPart",
                FadeOut = 1f,
                Components =
                {
                    new CuiTextComponent { FadeIn = 1f, Text = message, Font = "robotocondensed-bold.ttf", FontSize = 8, Align = TextAnchor.MiddleCenter },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" },
                    new CuiOutlineComponent { Distance = "0.155 0.155", Color = HexToCuiColor("#000000FF") }
                }
            });

            CuiHelper.AddUi(player, container);

            if (single)
            {
                timer.Once(2, () => CuiHelper.DestroyUi(player, cLayer + ".ShowPart"));
            }
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
    }
}
