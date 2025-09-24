// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Oxide.Core;
using Facepunch;
using Rust;

namespace Oxide.Plugins.TeamBattle
{
    public static class PlayerData
    {
        public class PlayerStatsData
        {
            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int Wins { get; set; }
        }
        
        public static Dictionary<BasePlayer, PlayerStatsData> PlayerStats { get; private set; }
        public static Dictionary<BasePlayer, Timer> RespawnTimers { get; private set; }
        
        public static void Init(TeamBattle plugin)
        {
            PlayerStats = new Dictionary<BasePlayer, PlayerStatsData>();
            RespawnTimers = new Dictionary<BasePlayer, Timer>();
        }
        
        public static void OnPlayerDisconnected(BasePlayer player)
        {
            if (PlayerStats.ContainsKey(player))
                PlayerStats.Remove(player);
                
            if (RespawnTimers.TryGetValue(player, out var timer))
            {
                timer?.Destroy();
                RespawnTimers.Remove(player);
            }
        }
    }
}