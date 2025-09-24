// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Globalization;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("PeacePanel", "RustMan", "0.1.0")]
    class PeacePanel : RustPlugin
    {
		
		#region Hooks [Хуки]

        private string TimeFormat = "HH:mm"; // формат времени

        private void OnServerInitialized()
        {
            timer.Once(1, () =>
            {
                BasePlayer.activePlayerList.ForEach(OnPlayerInit);
            });
        }
		
        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "Layer");  
            }   
        }
		
        private void OnPlayerInit(BasePlayer player)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerInit(player));
                return;
            }
			
            foreach (var players in BasePlayer.activePlayerList)
            {
			    timer.Once(1, () =>
			    {
                    DrawInterface(players);
			    });
            }
        }
		
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            foreach (var players in BasePlayer.activePlayerList)
            {
			    timer.Once(1, () =>
			    {
				    DrawInterface(players);
			    });
            }
        }
        
		#endregion
            
		#region UI [Визуальная часть]
		
		private void DrawInterface(BasePlayer player)
        {
        
        Timer tim = timer.Repeat(1, 0, () =>
            {  

                var QueueCount = ServerMgr.Instance.connectionQueue.Queued.ToString();      // {QueueCount} очередь
                var JoiningCount = ServerMgr.Instance.connectionQueue.Joining.ToString();   // {JoiningCount} подключается
                var Players = BasePlayer.activePlayerList.Count;                            // {Players} игрки онлайн
                var SleepersCount = BasePlayer.sleepingPlayerList.Count.ToString();         // {SleepersCount} спят
                var time = TOD_Sky.Instance.Cycle.DateTime.ToString(TimeFormat);            // {time} время

                string Layer = "Layer";
                CuiHelper.DestroyUi(player, Layer);
                var container = new CuiElementContainer();
                container.Add(new CuiPanel
                {
                    Image = { Color = HexToRustFormat("#FFFFFF00") },
                    RectTransform = { AnchorMin = "0.345 1.024455E-08", AnchorMax = "0.641875 0.02499998" },
                    CursorEnabled = false,
                }, "Hud", Layer);

                container.Add(new CuiElement
                {
                    Parent = Layer,
                    Components =
                    {
                        new CuiTextComponent { Text = $"Время: {time} | Онлайн: {Players} | Спят: {SleepersCount} | Заходят: {JoiningCount} | Очередь: {QueueCount}", Align = TextAnchor.MiddleCenter, FontSize = 12, Font = "RobotoCondensed-regular.ttf"},
                        new CuiOutlineComponent { Color = "0 0 0 1", Distance = "0.5 0.5" },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                    }
                });

                CuiHelper.AddUi(player, container);
            }); 
        }
		
		#endregion
		
		#region Helpers [Доп. методы]
		
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