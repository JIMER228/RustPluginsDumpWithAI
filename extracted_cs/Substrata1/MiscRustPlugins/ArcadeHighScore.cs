using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core;
using Oxide.Core.Plugins;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("ArcadeHighScore", "Substrata", "1.0.0")]
    [Description("Get the high score for arcade machines & add user to group on wipe")]

    class ArcadeHighScore : RustPlugin
    {
        [PluginReference]
        Plugin ArcadeMachineConnector;

        bool serverWiped = false;
        string highScoreGroup => configData.highScoreGroup;

        void OnNewSave(string filename) => serverWiped = true;

        void Init()
        {
            LoadData();
            Unsubscribe(nameof(OnPlayerSleepEnded));
        }

        void OnServerInitialized(bool initial)
        {
            if (!ArcadeMachineConnector)
            {
                PrintWarning("ArcadeMachineConnector not found!");
                return;
            }

            if (string.IsNullOrEmpty(highScoreGroup))
                PrintWarning($"Group not set. Check config.");
            else if (!GroupExists(highScoreGroup))
                PrintWarning($"Group '{highScoreGroup}' not found!");

            if (serverWiped)
                ArcadeWipe();
            else
                UpdateHighScore();

            if (configData.alertTopPlayer && storedData.LastWipe.PlayerID.IsSteamId() && !storedData.LastWipe.Alerted)
                Subscribe(nameof(OnPlayerSleepEnded));
        }

        private void UpdateHighScore()
        {
            var leaderboard = GetLeaderboard();
            if (leaderboard == null || leaderboard.Count() <= 0) return;

            var topEntry = leaderboard.First();

            int highScore = topEntry.Value;
            if (highScore <= 0) return;

            ulong playerID = topEntry.Key;
            if (!playerID.IsSteamId()) return;

            if (highScore > storedData.CurrentWipe.HighScore)
            {
                storedData.CurrentWipe.HighScore = highScore;
                storedData.CurrentWipe.PlayerID = playerID;
            }
        }

        private void OnNewHighestScore(BasePlayer player, int score) => UpdateHighScore();

        private void ArcadeWipe()
        {
            PrintWarning("Server wipe detected. Updating high score info...");

            int highScore = storedData.CurrentWipe.HighScore;
            ulong playerID = storedData.CurrentWipe.PlayerID;

            // Update scores for new wipe
            storedData.LastWipe.HighScore = highScore;
            storedData.LastWipe.PlayerID = playerID;
            storedData.LastWipe.Alerted = false;
            storedData.CurrentWipe.HighScore = 0;
            storedData.CurrentWipe.PlayerID = 0;

            // Log file
            var log = new StringBuilder();
            log.AppendLine($"[New wipe - {DateTime.Now}]");

            // New high score
            bool highScoreValid = highScore > 0 && playerID.IsSteamId();
            string highScoreName = string.Empty;

            string msg1;
            if (highScoreValid)
            {
                var highScoreP = covalence.Players.FindPlayerById(playerID.ToString());
                highScoreName = highScoreP != null ? highScoreP.Name : "Unknown Player";
                msg1 = $"The highest score last wipe was {highScore}, set by {highScoreName}";
            }
            else
                msg1 = $"No valid high score found for last wipe!";
            PrintWarning(msg1);
            log.AppendLine(msg1);

            // Clear & add to group
            if (GroupExists(highScoreGroup))
            {
                // Remove users from group
                foreach (var user in permission.GetUsersInGroup(highScoreGroup))
                {
                    string id = user.Substring(0, user.LastIndexOf('(')).TrimEnd();
                    string name = user.Substring(user.LastIndexOf('(') + 1, (user.Substring(user.LastIndexOf('(')).Length - 2));
                    permission.RemoveUserGroup(id, highScoreGroup);
                    string msg2 = $"{name} ({id}) removed from group '{highScoreGroup}'";
                    PrintWarning(msg2);
                    log.AppendLine(msg2);
                }

                // Add top player to group
                if (highScoreValid)
                {
                    permission.AddUserGroup(playerID.ToString(), highScoreGroup);
                    string msg3 = $"{highScoreName} ({playerID}) added to group '{highScoreGroup}'";
                    PrintWarning(msg3);
                    log.AppendLine(msg3);
                }
            }
            else
                log.AppendLine($"Group '{highScoreGroup}' not found!");

            LogToFile("wipe", log.ToString(), this);

            SaveData();
            PrintWarning("High score info updated!");
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (player == null || !player.userID.IsSteamId() || storedData.LastWipe.Alerted) return;
            if (player.userID == storedData.LastWipe.PlayerID)
            {
                SendReply(player, Lang("HighScore_WipeAlert", null, player.displayName, player.userID, storedData.LastWipe.HighScore));
                storedData.LastWipe.Alerted = true;
            }
        }

        [ChatCommand("arcadeinfo")]
		void cmdArcadeScore(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;

            string groupStatus;
            if (string.IsNullOrEmpty(highScoreGroup))
                groupStatus = "Group not set!";
            else
            {
                if (GroupExists(highScoreGroup))
                    groupStatus = $"Group <color=#C6CFD3>{highScoreGroup}</color>:  <color=#C5E1A5>Found!</color>";
                else
                    groupStatus = $"Group <color=#C6CFD3>{highScoreGroup}</color>:  <color=#EF9A9A>Not found!</color>";
            }

            string users;
            var usersInGroup = permission.GetUsersInGroup(highScoreGroup);
            if (usersInGroup.Count() <= 0)
                users = "• Users:  None";
            else
            {
                var userList = new StringBuilder();
                foreach (var user in usersInGroup)
                {
                    userList.AppendLine($"{user}");
                }
                userList.Length -= 1;
                users = $"• Users:\n<size=90%>{userList}</size>";
            }

            string perms;
            var groupPerms = permission.GetGroupPermissions(highScoreGroup);
            if (groupPerms.Count() <= 0)
                perms = "• Permissions:  None";
            else
            {
                var permList = new StringBuilder();
                string delimiter = string.Empty;
                foreach (var perm in groupPerms)
                {
                    permList.Append(delimiter);
                    permList.Append($"{perm}");
                    delimiter = ", ";
                }
                perms = $"• Permissions:\n<size=90%>{permList}</size>";
            }

            SendReply(player, $"<line-height=150%>{groupStatus}\n</line-height><line-height=110%>{users}</line-height><line-height=150%>\n</line-height><line-height=110%>{perms}</line-height>");

            string lastHighScore = $"Last Wipe:  None";
            ulong hsLastID = storedData.LastWipe.PlayerID;
            int hsLastScore = storedData.LastWipe.HighScore;
            if (hsLastScore > 0 && hsLastID.IsSteamId())
            {
                var p = covalence.Players.FindPlayerById(hsLastID.ToString());
                var pName = p != null ? p.Name : "Unknown Player";
                lastHighScore = $"Last Wipe:  {pName} ({storedData.LastWipe.PlayerID})  <color=#FFF59D>[ {hsLastScore} ]</color>";
            }

            string currentHighScore = $"Current:  None yet";
            ulong hsCurID = storedData.CurrentWipe.PlayerID;
            int hsCurScore = storedData.CurrentWipe.HighScore;
            if (hsCurScore > 0 && hsCurID.IsSteamId())
            {
                var p = covalence.Players.FindPlayerById(hsCurID.ToString());
                var pName = p != null ? p.Name : "Unknown Player";
                currentHighScore = $"Current:  {pName} ({storedData.CurrentWipe.PlayerID})  <color=#FFF59D>[ {hsCurScore} ]</color>";
            }

            SendReply(player, $"<line-height=140%>High Scores:\n</line-height><line-height=130%><size=90%>{lastHighScore}</size>\n<size=90%>{currentHighScore}</size></line-height>");
        }

        #region Helpers
        private Dictionary<ulong, int> GetLeaderboard() => ArcadeMachineConnector.Call<Dictionary<ulong, int>>("GetLeaderboard", Name);
        private bool GroupExists(string group) => permission.GroupExists(group);
        #endregion

        #region Config
        private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Group Name")]
            public string highScoreGroup = string.Empty;
            [JsonProperty(PropertyName = "Alert Top Player On Wipe")]
            public bool alertTopPlayer = true;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                configData = Config.ReadObject<ConfigData>();
                if (configData == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => configData = new ConfigData();
        protected override void SaveConfig() => Config.WriteObject(configData);
        // s ubstrata
        #endregion

        #region DataFile
        private StoredData storedData;

        private class StoredData
        {
            public LastWipe LastWipe = new LastWipe();
            public CurrentWipe CurrentWipe = new CurrentWipe();
        }

        public class LastWipe
        {
            public ulong PlayerID;
            public int HighScore;
            public bool Alerted;
        }

        public class CurrentWipe
        {
            public ulong PlayerID;
            public int HighScore;
        }

        private void LoadData()
        {
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch
            {
                ClearData();
            }
            if (storedData == null) ClearData();
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);

        void Unload() => SaveData();
        void OnServerSave() => SaveData();

        private void ClearData()
        {
            storedData = new StoredData();
            SaveData();
        }
        #endregion

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["HighScore_WipeAlert"] = "Congratulations {0}! You got the highest score of the wipe with {2}!"
            }, this);
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        #endregion
    }
}