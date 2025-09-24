using Oxide.Core.Plugins;
using Oxide.Core;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System;

namespace Oxide.Plugins
{
    [Info("ChippyKing", "jerky", "1.0.0")]
    [Description("ChippyKing plugin for Rust")]
    public class ChippyKing : RustPlugin
    {
        private int highestScore = 0;
        private ulong highestScorer = 0;
        private const string DataFilePath = "ChippyKingData";

        void Init()
        {
            LoadData();
        }

        void OnArcadeScoreAdded(BaseArcadeMachine machine, BasePlayer player, int score)
        {
            if (machine == null || player == null) return;
            ulong playerId = player.userID;

            if (score > highestScore)
            {
                highestScore = score;
                highestScorer = playerId;
                SaveData();
                GiveScrap(player, score * 100); // Configure it later.
                PrintToChat($"{player.displayName} achieved the high score of Level.{highestScore} on this server and received {score * 100} scrap!!");
            }
        }

        private void GiveScrap(BasePlayer player, int amount)
        {
            Item scrap = ItemManager.CreateByName("scrap", amount);
            player.GiveItem(scrap);
        }

        private void SaveData()
        {
            var data = new Dictionary<string, object>
            {
                { "highestScore", highestScore },
                { "highestScorer", highestScorer }
            };
            Interface.Oxide.DataFileSystem.WriteObject(DataFilePath, data);
        }

        private void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile(DataFilePath))
            {
                var data = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, object>>(DataFilePath);
                highestScore = Convert.ToInt32(data["highestScore"]);
                highestScorer = Convert.ToUInt64(data["highestScorer"]);
                Puts("Data loaded successfully.");
            }
        }

        [ChatCommand("highscore")]
        private void ShowHighScore(BasePlayer player, string command, string[] args)
        {
            var highestScorerPlayer = BasePlayer.FindByID(highestScorer);
            string highestScorerName = highestScorerPlayer != null ? highestScorerPlayer.displayName : "Unknown";
            PrintToChat(player, $"{highestScorerName} is the King of Chippy. Level.{highestScore}");
        }
    }
}
