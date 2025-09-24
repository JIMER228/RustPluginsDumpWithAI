using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Rust;
using Rust.Modular;
using Facepunch;
using Facepunch.Extend;
using Facepunch.Math;
using Facepunch.Models;
using Facepunch.Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using UnityEngine.Assertions;
using ConVar;
using Network;
using ProtoBuf;



namespace Oxide.Plugins
{
    [Info("TcItemCounter", "jerkypaisen", "1.0.0")]
    [Description("You can count the specified item in TC.")]
    class TcItemCounter : RustPlugin
    {
        #region Console Commands
        [ConsoleCommand("tic")]
        private void CmdCount(ConsoleSystem.Arg arg)
        {
            int num = 5;
            if (arg != null && arg.Args != null && (!(int.TryParse(arg.Args[0], out num)))) num = 5;
            if (arg.Connection == null || (arg.Connection != null && arg.Connection.authLevel == 2))
            {
                TextTable textTable = new TextTable();
                textTable.AddColumns("OwnerName(SteamID)", "Total");
                Dictionary<string, int> scoreDic = new Dictionary<string, int>();
                foreach (BuildingPrivlidge item in BaseNetworkable.serverEntities.OfType<BuildingPrivlidge>())
                {
                    int total = 0;
                    foreach (var target in config.targets)
                    {
                        total += target.Value * ItemCount(item, target.Key);
                    }
                    if (total == 0) continue;
                    BaseEntity baseEntity = item.inventory.GetEntityOwner() as BaseEntity;
                    string scoreKey = baseEntity.OwnerID.ToString();
                    BasePlayer basePlayer = BasePlayer.FindAwakeOrSleeping(baseEntity.OwnerID.ToString());
                    if (basePlayer != null) scoreKey = string.Format("{0}({1})", basePlayer.displayName, basePlayer.userID.ToString());
                    if (scoreDic.ContainsKey(scoreKey))
                    {
                        scoreDic[scoreKey] += total;
                    }
                    else
                    {
                        scoreDic.Add(scoreKey, total);
                    }
                }
                var sortedDict = (from entry in scoreDic orderby entry.Value descending select entry).Take(num).ToDictionary(pair => pair.Key, pair => pair.Value);
                foreach (var score in sortedDict)
                {
                    textTable.AddRow(score.Key, score.Value.ToString());
                }
                arg.ReplyWith(arg.HasArg("--json") ? textTable.ToJson() : textTable.ToString());
            }
        }


        [ConsoleCommand("ttic")]
        private void CmdTeamCount(ConsoleSystem.Arg arg)
        {
            int num = 5;
            if (arg != null && arg.Args != null && (!(int.TryParse(arg.Args[0], out num)))) num = 5;
            if (arg.Connection == null || (arg.Connection != null && arg.Connection.authLevel == 2))
            {
                TextTable textTable = new TextTable();
                textTable.AddColumns("TeamLeaderName(SteamID)", "Total");
                Dictionary<string, int> scoreDic = new Dictionary<string, int>();
                foreach (BuildingPrivlidge item in BaseNetworkable.serverEntities.OfType<BuildingPrivlidge>())
                {
                    int total = 0;
                    foreach (var target in config.targets)
                    {
                        total += target.Value * ItemCount(item, target.Key);
                    }
                    if (total == 0) continue;
                    BaseEntity baseEntity = item.inventory.GetEntityOwner() as BaseEntity;
                    string scoreKey = baseEntity.OwnerID.ToString();
                    BasePlayer basePlayer = BasePlayer.FindAwakeOrSleeping(baseEntity.OwnerID.ToString());
                    BasePlayer baseLeaderPlayer = basePlayer;
                    if (basePlayer != null)
                    {
                        var team = RelationshipManager.ServerInstance.FindPlayersTeam(basePlayer.userID);
                        if (team != null) baseLeaderPlayer = BasePlayer.FindAwakeOrSleeping(team.teamLeader.ToString());
                    }
                    if (baseLeaderPlayer != null) scoreKey = string.Format("{0}({1})", baseLeaderPlayer.displayName, baseLeaderPlayer.userID.ToString());
                    if (scoreDic.ContainsKey(scoreKey))
                    {
                        scoreDic[scoreKey] += total;
                    }
                    else
                    {
                        scoreDic.Add(scoreKey, total);
                    }
                }
                var sortedDict = (from entry in scoreDic orderby entry.Value descending select entry).Take(num).ToDictionary(pair => pair.Key, pair => pair.Value);
                foreach (var score in sortedDict)
                {
                    textTable.AddRow(score.Key, score.Value.ToString());
                }
                arg.ReplyWith(arg.HasArg("--json") ? textTable.ToJson() : textTable.ToString());
            }
        }
        #endregion

        #region Helpers
        public int ItemCount(BuildingPrivlidge item, string name)
        {
            int itemid = FindItemIdByItemName(name); 
            if (itemid == 0) return 0;
            int subtotal = item.inventory.GetAmount(itemid, true);
            return subtotal;
        }
        public int FindItemIdByItemName(string name)
        {
            ItemDefinition itemDefinition = ItemManager.FindItemDefinition(name);
            if (itemDefinition == null) return 0;
            return itemDefinition.itemid;
        }
        #endregion

        #region Configuration
        private ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "count targets")]
            public Dictionary<string, int> targets;
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                targets = new Dictionary<string, int>
                {
                    ["dogtagneutral"] = 1,
                    ["bluedogtags"] = 10,
                    ["reddogtags"] = 100
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Configuration file is corrupt(or not exists), creating new one!");
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

    }
}
