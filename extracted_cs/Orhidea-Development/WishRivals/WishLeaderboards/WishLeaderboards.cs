using Oxide.Core;
using System.Diagnostics;
using WishInfrastructure;


namespace Oxide.Plugins
{
    [Info("WishLeaderboards", "Latvish&Zurius", "0.0.2")]
    [Description("WishLeaderboards")]
    public partial class WishLeaderboards : RustPlugin
    {
        private ConfigSetup _config;

        public static DatabaseClient Database { get; set; }
        public static LeaderboardService LbService { get; set; }

        private GuiService _guiService;

        void Init()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Interface.Oxide.LogDebug($"START Init WishLeaderboards");
            _config = new ConfigSetup(this);

            SubscribeToEvents();

            InitInfrastructure();

            LbService = new LeaderboardService(Database);
            _guiService = new GuiService(LbService);
            stopwatch.Stop();
            Interface.Oxide.LogDebug($"END Init WishLeaderboards {stopwatch.ElapsedMilliseconds}ms");

        }

        private void InitInfrastructure()
        {
            Database = new DatabaseClient("WishStats","WishStatsClans", this, _config.ConfigFile.DatabaseConfig);
            Database.SetupDatabase();
        }

        private void SubscribeToEvents()
        {
            Subscribe("OnServerSave");
        }
        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(ConfigSetup.GetDefaultConfig(), true);
        }
    }
}
