using System.Collections.Generic;
using WishInfrastructure;

namespace Oxide.Plugins
{
    public class Leaderboard
    {
        private readonly DatabaseClient _databaseClient;
        public readonly string LeaderboardName;
        private  List<KeyValuePair<string, int>> _playersWithValues;
        
        public Leaderboard(DatabaseClient databaseClient, string leaderboard)
        {
            _databaseClient = databaseClient;
            LeaderboardName = leaderboard;
            Update();
        }
        public List<KeyValuePair<string, int>> GetLeaderboard()
        {
            return _playersWithValues;
        }
        public void Update()
        {
            _playersWithValues = _databaseClient.GetLeaderboard<int>(LeaderboardName);
        }
    }
}
