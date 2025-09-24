//Reference: WishInfrastructure
#define DEBUG
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WishInfrastructure;
using WishInfrastructure.Models;


//DatabaseUsageTemplate created with PluginMerge v(1.0.4.0) by MJSU @ https://github.com/dassjosh/Plugin.Merge
namespace Oxide.Plugins
{
    [Info("DatabaseUsageTemplate", "Latvish", "0.0.1")]
    [Description("DatabaseUsageTemplate")]
    public partial class DatabaseUsageTemplate : RustPlugin
    {
        #region DatabaseUsageTemplate.cs
        private ConfigSetup _config;
        
        public static DatabaseClient Database { get; set; }
        
        void Init()
        {
            _config = new ConfigSetup(this);
            
            Database = new DatabaseClient("WishStats", this, _config.ConfigFile.DatabaseConfig);
            
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
        #endregion

        #region EventListeners\DatabaseSaveEvents.cs
        void OnServerSave()
        {
            Interface.Oxide.LogDebug($"Performing database save");
            Database.SaveDatabase();
        }
        
        void OnUserConnected(IPlayer player)
        {
            if (!Database.IsKnownPlayer(player.Id))
            {
                Database.LoadPlayer(player.Id);
            }
            
            Database.SetPlayerData(player.Id, "name", player.Name);
        }
        #endregion

        #region Models\ConfigFile.cs
        public class ConfigFile
        {
            public DatabaseConfig DatabaseConfig { get; set; }
            
        }
        #endregion

        #region Startup\ConfigSetup.cs
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
        #endregion

    }

}
