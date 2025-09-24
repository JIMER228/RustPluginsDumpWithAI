#define DEBUG
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VLB;


//WishInfrastructure created with PluginMerge v(1.0.4.0) by MJSU @ https://github.com/dassjosh/Plugin.Merge
namespace Oxide.Plugins
{
    [Info("Wish Infrastructure", "Latvish", "0.1")]
    [Description("Nice plugin to save stuff for players")]
    public partial class WishInfrastructure : RustPlugin
    {
        #region WishInfrastructure.cs
        private ConfigSetup _config;
        void Init()
        {
            Puts("starting to save config");
            _config = new ConfigSetup(this);
            //SetupDatabase();
        }
        
        [HookMethod("CreateTable")]
        public string CreateTable(string table)
        {
            return "worked";
        }
        
        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(ConfigSetup.GetDefaultConfig(), true);
        }
        #endregion

        #region DatabaseClient.cs
        public class DatabaseClient
        {
            private string _tableName;
            private readonly WishInfrastructure _plugin;
            
            public DatabaseClient(string tableName, WishInfrastructure plugin)
            {
                _tableName = tableName;
                _plugin = plugin;
            }
            
            public string TestMethod()
            {
                return _tableName;
            }
        }
        #endregion

        #region Models\PluginConfig.cs
        public class PluginConfig
        {
            public string sql_host { get; set; }
            public int sql_port { get; set; }
            public string sql_db { get; set; }
            public string sql_user { get; set; }
            public string sql_pass { get; set; }
        }
        #endregion

        #region Startup\ConfigSetup.cs
        public class ConfigSetup
        {
            
            
            private readonly WishInfrastructure _plugin;
            public PluginConfig Config { get; set; }
            
            
            internal ConfigSetup(WishInfrastructure plugin)
            {
                plugin.PrintToConsole("Init");
                _plugin = plugin;
                Init();
            }
            
            private void Init()
            {
                _plugin.PrintToConsole("Config init");
                
                Config = _plugin.Config.ReadObject<PluginConfig>();
                
                _plugin.PrintToConsole("Config init 1");
                
                
            }
            public static PluginConfig GetDefaultConfig()
            {
                return new PluginConfig
                {
                    sql_host = "localhost",
                    sql_port = 1234,
                    sql_db = "rust",
                    sql_user = "admin",
                    sql_pass = "password"
                };
            }
        }
        #endregion

    }

}
