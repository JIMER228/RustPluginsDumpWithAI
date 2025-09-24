using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
using Harmony;

namespace Oxide.Plugins
{
    [Info("TeamSwitchLimit", "Kira", "1.0.0")]
    [Description("Лимит смены тимы")]
    public class TeamSwitchLimit : RustPlugin
    {
        #region [Vars]

        private StoredData _dataBase = new StoredData();

        #endregion

        #region [Lang]

        protected override void LoadDefaultMessages()
        {
            var ru = new Dictionary<string, string>
            {
                ["LIMIT"] = "Ваш лимит смены команды был исчерпан",
                ["LEFT"] = "У вас осталось {0} смены команд"
            };

            var en = new Dictionary<string, string>
            {
                ["LIMIT"] = "Your team change limit has been reached",
                ["LEFT"] = "You have {0} team shift left"
            };
            lang.RegisterMessages(ru, this, "ru");
            lang.RegisterMessages(en, this);
        }

        #endregion

        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "TeamSwitchLimit - Настройка")]
            public TeamSwitchLimitCFG TeamSwitchConfig = new TeamSwitchLimitCFG();

            public class TeamSwitchLimitCFG
            {
                [JsonProperty(PropertyName = "Лимит смены команды")]
                public int Limit;
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                TeamSwitchConfig = new ConfigData.TeamSwitchLimitCFG
                {
                    Limit = 2
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region [Hooks]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            LoadData();
        }

        // ReSharper disable once UnusedMember.Local
        private object OnTeamAcceptInvite(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            CheckData(player.userID);
            var leader = team.GetLeader();
            var db1 = _dataBase.LimitData[player.userID];
            var db2 = _dataBase.LimitData[leader.userID];

            if (db1 > 0 & db2 > 0)
            {
                _dataBase.LimitData[player.userID]--;
                _dataBase.LimitData[leader.userID]--;
                SendTip(player,
                    lang.GetMessage("LEFT", this, player.UserIDString)
                        .Replace("{0}", _dataBase.LimitData[player.userID].ToString()));
                SendTip(leader,
                    lang.GetMessage("LEFT", this, leader.UserIDString)
                        .Replace("{0}", _dataBase.LimitData[leader.userID].ToString()));
                return null;
            }

            if (_dataBase.LimitData[player.userID] <= 0 & _dataBase.LimitData[leader.userID] <= 0)
            {
                SendTip(player, lang.GetMessage("LIMIT", this, player.UserIDString));
                SendTip(leader, lang.GetMessage("LIMIT", this, leader.UserIDString));
                return false;
            }

            if (_dataBase.LimitData[player.userID] <= 0)
            {
                SendTip(player, lang.GetMessage("LIMIT", this, player.UserIDString));
                return false;
            }

            if (_dataBase.LimitData[leader.userID] <= 0)
            {
                SendTip(leader, lang.GetMessage("LIMIT", this, leader.UserIDString));
                return false;
            }

            return null;
        }

        // ReSharper disable once UnusedMember.Local
        private object OnTeamInvite(BasePlayer inviter, BasePlayer target)
        {
            CheckData(inviter.userID);
            CheckData(target.userID);
            var db1 = _dataBase.LimitData[inviter.userID];
            var db2 = _dataBase.LimitData[target.userID];
            if (db1 <= 0)
            {
                SendTip(inviter, lang.GetMessage("LIMIT", this, inviter.UserIDString));
                return false;
            }

            if (db2 <= 0)
            {
                SendTip(target, lang.GetMessage("LIMIT", this, target.UserIDString));
                return false;
            }

            return null;
        }

        // ReSharper disable once UnusedMember.Local
        private object OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            CheckData(player.userID);
            if (_dataBase.LimitData[player.userID] > 0) return null;
            SendTip(player, lang.GetMessage("LIMIT", this, player.UserIDString));
            return false;
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnNewSave(string filename)
        {
            LoadData();
            _dataBase.LimitData.Clear();
            SaveData();
            PrintWarning("База данных на лимит смены команд сброшена");
        }

        // ReSharper disable once UnusedMember.Local
        private void OnServerSave()
        {
            timer.Once(5f, SaveData);
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
        }

        #endregion

        #region [Commands]

        [ConsoleCommand("teamlimit.clear")]
        private void ClearDB(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            if (!args.Player().IsAdmin) return;
            _dataBase.LimitData.Clear();
            SaveData();
        }

        #endregion

        #region [DataBase]

        private class StoredData
        {
            public Dictionary<ulong, int> LimitData = new Dictionary<ulong, int>();
        }

        #endregion

        #region [Helpers]

        private static void SendTip(BasePlayer player, string text)
        {
            player.SendConsoleCommand("showtoast", 0, text);
        }

        private void CheckData(ulong userid)
        {
            if (!_dataBase.LimitData.ContainsKey(userid))
                _dataBase.LimitData.Add(userid, _config.TeamSwitchConfig.Limit);
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _dataBase);

        private void LoadData()
        {
            try
            {
                _dataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception)
            {
                _dataBase = new StoredData();
            }
        }

        #endregion
    }
}