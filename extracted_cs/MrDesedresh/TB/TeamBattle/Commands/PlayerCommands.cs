// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Facepunch;
using Rust;
using Oxide.Plugins.TeamBattle.Data;
using Oxide.Plugins.TeamBattle.Systems;

namespace Oxide.Plugins.TeamBattle
{
    public static class PlayerCommands
    {
        private static TeamBattle Plugin;
        
        public static void Init(TeamBattle plugin)
        {
            Plugin = plugin;
            Plugin.AddCovalenceCommand("play", plugin, nameof(PlayCommand));
            Plugin.AddCovalenceCommand("leave", plugin, nameof(LeaveCommand));
        }
        
        private static void PlayCommand(IPlayer iplayer, string command, string[] args)
        {
            var player = iplayer.Object as BasePlayer;
            RoomSelectionUI.ShowRoomSelection(player);
        }
        
        private static void LeaveCommand(IPlayer iplayer, string command, string[] args)
        {
            var player = iplayer.Object as BasePlayer;
            ProcessPlayerLeave(player, false);
        }
        
        private static bool ProcessPlayerLeave(BasePlayer player, bool adminForced)
        {
            int roomId = RoomData.FindPlayerRoom(player);
            if (roomId == -1)
            {
                if (adminForced)
                    player.ChatMessage("<color=yellow>Игрок не в битве!</color>");
                return false;
            }

            if (PlayerData.RespawnTimers.TryGetValue(player, out Timer respawnTimer))
            {
                respawnTimer?.Destroy();
                PlayerData.RespawnTimers.Remove(player);
            }

            RoomData.RemovePlayerFromAllRooms(player);
            TeleportSystem.SafeTeleport(player, TeleportSystem.GetDefaultSpawnPoint());
            
            player.inventory.Strip();
            player.health = player.MaxHealth();
            player.metabolism.bleeding.value = 0;

            Effect.server.Run("assets/prefabs/misc/supply drop/effects/supply_drop_parachute.prefab", player.transform.position);
            CuiHelper.DestroyUi(player, "TeamSelectPanel");
            
            if (adminForced)
            {
                player.ChatMessage("<color=red>Администратор исключил вас из битвы!</color>");
            }
            else
            {
                player.ChatMessage("<color=green>Вы вышли из командной битвы!</color>");
            }

            if (RoomData.Rooms[roomId]["Team1"].Count + RoomData.Rooms[roomId]["Team2"].Count == 0)
            {
                RoomData.ResetRoom(roomId);
            }

            if (RoomData.RoomLocked[roomId] && PlayerData.PlayerStats.TryGetValue(player, out var stats))
            {
                stats.Deaths++;
            }

            return true;
        }
    }
}