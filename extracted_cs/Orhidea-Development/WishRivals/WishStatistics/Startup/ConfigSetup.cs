using WishInfrastructure.Models;

namespace Oxide.Plugins
{
    public class ConfigSetup
    {


        private readonly RustPlugin _plugin;
        public ConfigFile ConfigFile { get; set; }


        internal ConfigSetup(RustPlugin plugin)
        {
            _plugin = plugin;
            Init();
        }

        private void Init()
        {
            ConfigFile = _plugin.Config.ReadObject<ConfigFile>();

        }

        internal static object GetDefaultConfig()
        {
            return new ConfigFile()
            {
                DatabaseConfig = DatabaseConfig.GetDefaultDatabaseConfig(),
                lb_clans = new LbClan[] { new LbClan() { Id = "1", Name = "Clan1" }, new LbClan() { Id = "2", Name = "Clan2" }, new LbClan() { Id = "3", Name = "Clan3" }, new LbClan() { Id = "4", Name = "Clan4" } }
            };
        }
    }
}

