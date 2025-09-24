using Oxide.Core;
using WishInfrastructure;

namespace Oxide.Plugins
{
    [Info("WishStatistics", "Latvish&Zurius", "0.0.2")]
    [Description("WishStatistics")]

    public partial class WishStatistics : RustPlugin
    {
        private ConfigSetup _config;
        private GuiService _guiService;

        public static DatabaseClient Database { get; set; }
        public ClanGuiService _clanGuiService { get; set; }
        void Init()
        {
            Interface.Oxide.LogDebug("Starting statistics plugin");
            _config = new ConfigSetup(this);

            SubscribeToEvents();
            InitInfrastructure();
            _guiService = new GuiService(Database);
            //_clanGuiService = new ClanGuiService(_config, Database);
            //timer.Every(30, () =>
            //{
            //    Interface.Oxide.LogDebug("Updating clan scores");
            //    _clanGuiService.UpdateClanUI();
            //});
            
        }

        private void InitInfrastructure()
        {
            Subscribe("OnServerSave");
            Subscribe("OnUserConnected");
            Database = new DatabaseClient("WishStats", "WishStatsClans", this, _config.ConfigFile.DatabaseConfig);
            Database.SetupDatabase();
        }

        private void SubscribeToEvents()
        {
            Subscribe("OnBigWheelWin");
            Subscribe("OnBigWheelLoss");
            Subscribe("CanMoveItem");
            Subscribe("CanLootEntity");
            Subscribe("OnItemSplit");
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(ConfigSetup.GetDefaultConfig(), true);
        }



    }
}
