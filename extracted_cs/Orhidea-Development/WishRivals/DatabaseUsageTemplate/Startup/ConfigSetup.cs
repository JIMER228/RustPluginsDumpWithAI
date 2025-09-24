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
            };
        }

    }
}

