using System.Collections.Generic;
using WishInfrastructure;

namespace Oxide.Plugins
{
    public class LeaderboardService
    {
        private List<Leaderboard> _leaderboards;
        private readonly DatabaseClient _databaseClient;

        public LeaderboardService(DatabaseClient databaseClient)
        {

            _databaseClient = databaseClient;
            RegisterLeaderboards();
        }
        public void UpdateLeaderboards()
        {
            _leaderboards.ForEach(lb => lb.Update());
        }
        public List<Leaderboard> GetLeaderboards()
        {
            return _leaderboards;
        }
        private void RegisterLeaderboards()
        {
            _leaderboards = new List<Leaderboard>()
            {
                InitLeaderboard("Kills"),
                InitLeaderboard("Deaths"),
                InitLeaderboard("Damage"),
                InitLeaderboard("ScrapWon"),
                InitLeaderboard("OresFarmed"),
                InitLeaderboard("ItemsCrafted"),
            };
        }
        private Leaderboard InitLeaderboard(string name)
        {
            return new Leaderboard(_databaseClient, name);
        }
    }
}
