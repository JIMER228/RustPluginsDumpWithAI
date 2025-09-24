// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Facepunch;
using Rust;
using Oxide.Plugins.TeamBattle.Data;

namespace Oxide.Plugins.TeamBattle
{
    public static class RoomSelectionUI
    {
        private static TeamBattle Plugin;
        
        public static void Init(TeamBattle plugin)
        {
            Plugin = plugin;
        }
        
        public static void ShowRoomSelection(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;

            if (RoomData.FindPlayerRoom(player) != -1)
            {
                player.ChatMessage("<color=red>Вы уже участвуете в битве!</color>");
                return;
            }

            var elements = new CuiElementContainer();
            string panel = elements.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.1 0.1 0.9" },
                RectTransform = { AnchorMin = "0.3 0.2", AnchorMax = "0.7 0.8" },
                CursorEnabled = true
            }, "Overlay", "RoomSelectPanel");

            elements.Add(new CuiLabel
            {
                Text = { Text = "Выберите комнату", FontSize = 18, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0.1 0.85", AnchorMax = "0.9 0.95" }
            }, panel, "Title");

            for (int i = 1; i <= Configuration.MAX_ROOMS; i++)
            {
                int roomId = i;
                int playersCount = RoomData.Rooms[roomId]["Team1"].Count + RoomData.Rooms[roomId]["Team2"].Count;
                string status = RoomData.RoomLocked[roomId] ? "[ЗАКРЫТО]" : $"[{playersCount}/{Configuration.MAX_TEAM_PLAYERS * 2}]";

                elements.Add(new CuiButton
                {
                    Button = {
                        Command = $"teambattle.selectroom {roomId}",
                        Color = RoomData.RoomLocked[roomId] ? "0.3 0.3 0.3 0.5" : "0.3 0.3 0.3 1",
                        Close = panel
                    },
                    RectTransform = { AnchorMin = $"0.1 {0.75 - (i * 0.15)}", AnchorMax = $"0.9 {0.85 - (i * 0.15)}" },
                    Text = { Text = $"Комната {roomId} {status}", FontSize = 14, Align = TextAnchor.MiddleCenter }
                }, panel, $"RoomBtn_{i}");
            }

            CuiHelper.AddUi(player, elements);
        }
        
        public static void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "RoomSelectPanel");
            }
        }
    }
}