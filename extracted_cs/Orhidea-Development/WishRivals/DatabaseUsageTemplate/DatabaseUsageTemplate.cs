using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using WishInfrastructure;

namespace Oxide.Plugins
{
    [Info("DatabaseUsageTemplate", "Latvish", "0.0.1")]
    [Description("DatabaseUsageTemplate")]

    public partial class DatabaseUsageTemplate : RustPlugin
    {
        private ConfigSetup _config;

        public static DatabaseClient Database { get; set; }

        void Init()
        {
            _config = new ConfigSetup(this);

            Database = new DatabaseClient("WishStats", "WishStatsClans", this, _config.ConfigFile.DatabaseConfig);

            Database.SetupDatabase();

            Subscribe("OnServerSave");
            Subscribe("OnUserConnected");

        }

        [ChatCommand("savetest")]
        private void SaveTest(BasePlayer player, string wipeday, string[] args)
        {
            Interface.Oxide.LogDebug($"Adding test values {player.UserIDString}");

            Database.SetPlayerData(player.UserIDString, "TestDataString", "worked");

            Database.SetPlayerData(player.UserIDString, "TestDataInt", Database.GetPlayerDataRaw<int>(player.UserIDString, "TestDataInt") + 1);

            Interface.Oxide.LogDebug($"Performing database save");

            Database.SaveDatabase();
        }

        [ChatCommand("leaderboardtest")]
        private void LeaderboardTest(BasePlayer player, string leaderboard, string[] args)
        {
            Interface.Oxide.LogDebug($"Finding leaderboard: {args[0]}");
            var top = Database.GetLeaderboard<int>(args[0], 5);
            Interface.Oxide.LogDebug($"Found leaderboard: {top.Count}");

            Server.Broadcast($"Leaderboard: {top.Count}");

            Server.Broadcast($"Leaderboard: {args[0]}");

            for (int i = 0; i < top.Count; i++)
            {
                Server.Broadcast($"{i + 1}. {top[i].Key}: {top[i].Value}");
            }


        }


        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(ConfigSetup.GetDefaultConfig(), true);
        }

    }
}
