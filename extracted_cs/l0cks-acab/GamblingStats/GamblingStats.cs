using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

namespace Oxide.Plugins
{
    [Info("GamblingStats", "herbs.acab", "1.3.0")]
    [Description("Records and displays gambling statistics for players.")]

    class GamblingStats : CovalencePlugin
    {
        class PlayerStats
        {
            public int ScrapSpent { get; set; }
            public int ScrapLost { get; set; }
            public int ScrapEarned { get; set; }
        }

        private Dictionary<string, PlayerStats> playerStats = new Dictionary<string, PlayerStats>();
        private const int BackupCount = 5;
        private const float SaveInterval = 600f; // Save every 10 minutes

        protected override void LoadDefaultConfig()
        {
            // No configuration needed for local storage
        }

        void Init()
        {
            LoadData();
            timer.Every(SaveInterval, SaveData);
            Puts("GamblingStats plugin loaded.");
            permission.RegisterPermission("gamblingstats.admin", this);
        }

        void Unload()
        {
            SaveData();
            Puts("GamblingStats plugin unloaded.");
        }

        void OnServerSave()
        {
            SaveData();
        }

        void OnServerShutdown()
        {
            SaveData();
        }

        void OnPlayerInit(BasePlayer player)
        {
            var stats = GetPlayerStats(player.UserIDString);
            int profitLoss = stats.ScrapEarned - (stats.ScrapSpent - stats.ScrapLost);
            player.ChatMessage($"Welcome! Your current gambling profit/loss is: {profitLoss} scrap.");
        }

        // Commands
        [Command("help")]
        void HelpCommand(IPlayer player, string command, string[] args)
        {
            player.Message("<color=red>/mystats</color> - View your gambling statistics.");
            player.Message("<color=red>/topstats</color> - View the top 10 gamblers.");
            player.Message("<color=red>/searchstats <PlayerName or Steam64ID></color> - View gambling statistics for a specific player.");
        }

        [Command("mystats")]
        void MyStatsCommand(IPlayer player, string command, string[] args)
        {
            var stats = GetPlayerStats(player.Id);
            player.Message($"You have spent {stats.ScrapSpent} scrap, lost {stats.ScrapLost} scrap, and earned {stats.ScrapEarned} scrap through gambling.");
        }

        [Command("topstats")]
        void TopStatsCommand(IPlayer player, string command, string[] args)
        {
            var topStats = GetTopStats();
            foreach (var stat in topStats)
            {
                var targetPlayer = covalence.Players.FindPlayerById(stat.Key);
                if (targetPlayer != null)
                {
                    player.Message($"{targetPlayer.Name}: Spent {stat.Value.ScrapSpent}, Lost {stat.Value.ScrapLost}, Earned {stat.Value.ScrapEarned}");
                }
            }
        }

        [Command("searchstats")]
        void SearchStatsCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                player.Message("Usage: /searchstats <PlayerName or Steam64ID>");
                return;
            }

            var targetPlayer = FindPlayer(args[0]);
            if (targetPlayer == null)
            {
                player.Message("Player not found.");
                return;
            }

            var stats = GetPlayerStats(targetPlayer.Id);
            player.Message($"{targetPlayer.Name} has spent {stats.ScrapSpent} scrap, lost {stats.ScrapLost} scrap, and earned {stats.ScrapEarned} scrap through gambling.");
        }

        [Command("deldata"), Permission("gamblingstats.admin")]
        void DelDataCommand(IPlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                player.Message("Usage: /deldata <Steam64ID>");
                return;
            }

            string playerId = args[0];
            if (playerStats.Remove(playerId))
            {
                player.Message($"Player data for {playerId} has been removed.");
            }
            else
            {
                player.Message($"Player data for {playerId} not found.");
            }

            SaveData();
        }

        // Hook into specific gambling events
        void OnScrapSpent(BasePlayer player, int amount)
        {
            var stats = GetPlayerStats(player.UserIDString);
            stats.ScrapSpent += amount;
            SaveData();
        }

        void OnScrapLost(BasePlayer player, int amount)
        {
            var stats = GetPlayerStats(player.UserIDString);
            stats.ScrapLost += amount;
            SaveData();
        }

        void OnScrapEarned(BasePlayer player, int amount)
        {
            var stats = GetPlayerStats(player.UserIDString);
            stats.ScrapEarned += amount;
            SaveData();
        }

        // Helper methods to load, save, and get player stats
        void LoadData()
        {
            playerStats = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, PlayerStats>>(Name);
        }

        void SaveData()
        {
            CreateBackup();
            Interface.Oxide.DataFileSystem.WriteObject(Name, playerStats);
        }

        void CreateBackup()
        {
            string backupDir = $"{Interface.Oxide.DataDirectory}/{Name}/backups";
            string backupFile = $"{backupDir}/{Name}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json";

            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            File.Copy($"{Interface.Oxide.DataDirectory}/{Name}.json", backupFile, true);

            var backupFiles = new DirectoryInfo(backupDir).GetFiles().OrderByDescending(f => f.CreationTime).ToList();

            if (backupFiles.Count > BackupCount)
            {
                for (int i = BackupCount; i < backupFiles.Count; i++)
                {
                    backupFiles[i].Delete();
                }
            }
        }

        PlayerStats GetPlayerStats(string playerId)
        {
            if (!playerStats.TryGetValue(playerId, out var stats))
            {
                stats = new PlayerStats();
                playerStats[playerId] = stats;
            }
            return stats;
        }

        Dictionary<string, PlayerStats> GetTopStats()
        {
            return playerStats.OrderByDescending(s => s.Value.ScrapEarned).Take(10).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        IPlayer FindPlayer(string identifier)
        {
            return covalence.Players.FindPlayer(identifier);
        }
    }
}
