using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Linq;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Database;
using Oxide.Core.MySql;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Oxide.Core.Configuration;
using Oxide.Game.Rust;
using Rust;
using UnityEngine;
using ConVar;

namespace Oxide.Plugins
{
    [Info("ZLevelsRemasteredMySQL", "Limmek", "1.0.5")]
    [Description("MySQL database add-on for ZLevels Remastered")]
    class ZLevelsRemasteredMySQL : RustPlugin
    {
        [PluginReference]
        Plugin ZLevelRemastered;

        #region configuration
        public class ConfigData
        {
            public Options Options = new Options();
            public MySQL MySQL = new MySQL();
        }
        public class Options
        {
            public int SaveTimer = 600;
            public bool UpdateAtStart;
            public bool TurncateDataOnMonthlyWipe;
            public bool TurncateDataOnMapWipe;
        }

        public class MySQL
        {
            public string host = "hostname";
            public int port = 3306;
            public string username = "username";
            public string password = "password";
            public string db = "database";
            public string table = "table";
        }       

		class PlayerData
		{
			public Dictionary<ulong, PlayerInfo> PlayerInfo = new Dictionary<ulong, PlayerInfo>();
			public PlayerData(){}
		}

		class PlayerInfo
		{
			public long WCL;
			public long WCP;
			public long ML;
			public long MP;
			public long SL;
			public long SP;
			public long AL;
			public long AP;
			public long CL;
			public long CP;
			public long LD;
			public long LLD;
			public long XPM;
			public bool CUI;
			public bool ONOFF;
		}
        #endregion
        
        private ConfigData conf;
        private PlayerData storedData;
    
        private int RustNetwork = 0;
        private int RustSave = 0;
        private int RustWorldSize = 0;
        private int RustSeed = 0;

        Core.MySql.Libraries.MySql sqlLibrary = Interface.Oxide.GetLibrary<Core.MySql.Libraries.MySql>();
        Connection sqlConnection;
        
        public void executeQuery(string query, params object[] data) {
            var sql = Sql.Builder.Append(query, data);
            sqlLibrary.Insert(sql, sqlConnection);
        }                
        
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating new config file.");
            var config = new ConfigData();
            SaveConfig();
        }
        
        private void LoadConfigVariables()
        {
            conf = Config.ReadObject<ConfigData>();
            SaveConfig(Config.ReadObject<ConfigData>());
        }
        
        void SaveConfig(ConfigData config)=>Config.WriteObject(config, true);
        
        void Unloaded()
        {
            sqlLibrary.CloseDb(sqlConnection);
        }

        private void Init()
        {
            #if !RUST
                throw new NotSupportedException("This plugin does not support this game");
            #endif

            LoadConfigVariables();

            sqlConnection = sqlLibrary.OpenDb(conf.MySQL.host, conf.MySQL.port, conf.MySQL.db, conf.MySQL.username, conf.MySQL.password, this);
            
            executeQuery($@"CREATE TABLE IF NOT EXISTS {conf.MySQL.table} (player_id BIGINT(20) NOT NULL, 
                WCL INT(11) NULL, 
                WCP INT(11) NULL, 
                ML INT(11) NULL, 
                MP INT(11) NULL, 
                SL INT(11) NULL, 
                SP INT(11) NULL, 
                AL INT(11) NULL, 
                AP INT(11) NULL, 
                CL INT(11) NULL, 
                CP INT(11) NULL, 
                LD INT(11) NULL, 
                LLD INT(11) NULL, 
                XPM INT(11) NULL, 
                CUI BOOLEAN NULL, 
                ONOFF BOOLEAN NULL,
                PRIMARY KEY (`player_id`),
                UNIQUE (`player_id`) ) ENGINE=InnoDB;"
            );
        }

        private void OnServerInitialized()
        {   
            RustNetwork   = Convert.ToInt32(Protocol.network);
            RustSave      = Convert.ToInt32(Protocol.save);
            RustWorldSize = ConVar.Server.worldsize;
            RustSeed      = ConVar.Server.seed;
            Puts($"Game Version: {RustNetwork}.{RustSave}, size: {RustWorldSize}, seed: {RustSeed}");

            DynamicConfigFile settingsData = Interface.Oxide.DataFileSystem.GetDatafile(nameof(ZLevelsRemasteredMySQL));
            if (settingsData["RustNetwork"] == null)
            {
                settingsData["RustNetwork"] = RustNetwork;
                settingsData["RustSave"] = RustSave;
                settingsData["RustWorldSize"] = RustWorldSize;
                settingsData["RustSeed"] = RustSeed;
                settingsData.Save();
            }

            if(conf.Options.TurncateDataOnMonthlyWipe == true)
            {
                if (Convert.ToInt32(settingsData["RustNetwork"]) != Convert.ToInt32(Protocol.network))
                {
                    Puts("Detected monthly rust update. Turncating data.");
                    settingsData["RustNetwork"] = RustNetwork;
                    settingsData["RustSave"] = RustSave;
                    settingsData["RustWorldSize"] = RustWorldSize;
                    settingsData["RustSeed"] = RustSeed;
                    settingsData.Save();

                    TurncateData();
                }
            }

            if(conf.Options.TurncateDataOnMapWipe == true)
            {
                if (Convert.ToInt32(settingsData["RustSeed"]) != Convert.ToInt32(ConVar.Server.seed))
                {
                    Puts("Detected map change. Turncating data.");
                    settingsData["RustNetwork"] = RustNetwork;
                    settingsData["RustSave"] = RustSave;
                    settingsData["RustWorldSize"] = RustWorldSize;
                    settingsData["RustSeed"] = RustSeed;
                    settingsData.Save();

                    TurncateData();
                }
            }

            if (conf.Options.UpdateAtStart) UpdateStatsData();
            
            timer.Repeat((float)conf.Options.SaveTimer, 0, () =>
            {                
                UpdateStatsData();
            });
        }

        private void TurncateData()
        {
            executeQuery("TRUNCATE TABLE " + conf.MySQL.table);
        }

        private void UpdateStatsData()
        {
            Puts("Please do not reload, or unload the plugin until save is completed.");
            storedData = Interface.Oxide.DataFileSystem.ReadObject<PlayerData>("ZLevelsRemastered");
            foreach (var item in storedData.PlayerInfo)
            {
                executeQuery($@"INSERT INTO {conf.MySQL.table} (player_id, WCL, WCP, ML, MP, SL, SP, AL, AP, CL, CP, LD, LLD, XPM, CUI, ONOFF) VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13, @14, @15) ON DUPLICATE KEY UPDATE player_id=@0, WCL=@1, WCP=@2, ML=@3, MP=@4, SL=@5, SP=@6, AL=@7, AP=@8, CL=@9, CP=@10, LD=@11, LLD=@12, XPM=@13, CUI=@14, ONOFF=@15",
                    item.Key,
                    item.Value.WCL,
                    item.Value.WCP,
                    item.Value.ML,
                    item.Value.MP,
                    item.Value.SL,
                    item.Value.SP,
                    item.Value.AL,
                    item.Value.AP,
                    item.Value.CL,
                    item.Value.CP,
                    item.Value.LD,
                    item.Value.LLD,
                    item.Value.XPM,
                    item.Value.CUI,
                    item.Value.ONOFF
                );         
            }
            Puts("SQL saving is complete.");
        }

    }
}