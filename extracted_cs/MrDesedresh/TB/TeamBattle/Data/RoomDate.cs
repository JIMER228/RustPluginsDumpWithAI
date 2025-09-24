using Oxide.Core;
using Facepunch;
using Rust;
using System.Collections.Generic;
using Oxide.Plugins.TeamBattle.Data;

namespace Oxide.Plugins.TeamBattle
{
    public static class RoomData
    {
        public static Dictionary<int, Dictionary<string, List<BasePlayer>>> Rooms { get; private set; }
        public static Dictionary<BasePlayer, int> PlayerRooms { get; private set; }
        public static Dictionary<int, bool> RoomLocked { get; private set; }
        public static Dictionary<int, int> RoomCountdowns { get; private set; }
        
        public static void Init(TeamBattle plugin)
        {
            Rooms = new Dictionary<int, Dictionary<string, List<BasePlayer>>>();
            PlayerRooms = new Dictionary<BasePlayer, int>();
            RoomLocked = new Dictionary<int, bool>();
            RoomCountdowns = new Dictionary<int, int>();
            
            for (int i = 1; i <= Configuration.MAX_ROOMS; i++)
            {
                Rooms[i] = new Dictionary<string, List<BasePlayer>>
                {
                    ["Team1"] = new List<BasePlayer>(),
                    ["Team2"] = new List<BasePlayer>()
                };
                RoomLocked[i] = false;
                RoomCountdowns[i] = Configuration.DEFAULT_COUNTDOWN;
            }
        }
        
        public static int FindPlayerRoom(BasePlayer player)
        {
            return PlayerRooms.TryGetValue(player, out int roomId) ? roomId : -1;
        }
        
        public static void AddPlayerToRoom(BasePlayer player, int roomId, string team)
        {
            RemovePlayerFromAllRooms(player);
            Rooms[roomId][team].Add(player);
            PlayerRooms[player] = roomId;
            
            if (!PlayerData.PlayerStats.ContainsKey(player))
            {
                PlayerData.PlayerStats[player] = new PlayerData.PlayerStatsData();
            }
        }
        
        public static void RemovePlayerFromAllRooms(BasePlayer player)
        {
            if (PlayerRooms.TryGetValue(player, out int roomId))
            {
                foreach (var team in Rooms[roomId].Values)
                {
                    team.Remove(player);
                }
                PlayerRooms.Remove(player);
            }
        }
        
        public static void OnPlayerDisconnected(BasePlayer player)
        {
            int roomId = FindPlayerRoom(player);
            if (roomId == -1) return;
            
            RemovePlayerFromAllRooms(player);
            if (Rooms[roomId]["Team1"].Count + Rooms[roomId]["Team2"].Count == 0)
            {
                ResetRoom(roomId);
            }
        }
        
        public static void ResetRoom(int roomId)
        {
            Rooms[roomId]["Team1"].Clear();
            Rooms[roomId]["Team2"].Clear();
            RoomLocked[roomId] = false;
            RoomCountdowns[roomId] = Configuration.DEFAULT_COUNTDOWN;
        }
    }
}