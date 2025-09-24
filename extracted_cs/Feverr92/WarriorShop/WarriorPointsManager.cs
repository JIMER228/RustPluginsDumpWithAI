using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("WarriorPointsManager", "YourName", "1.2.6")]
    [Description("Manages points and coins for players, giving new players 100 points on their first login and awarding points for destroying barrels, roadsigns, and looting specific items.")]
    public class WarriorPointsManager : RustPlugin
    {
        private Dictionary<string, PlayerInfo> playerPoints = new Dictionary<string, PlayerInfo>();
        private const string PointsDataFileName = "WarriorPointsData";
        private const int DefaultPoints = 100;
        private const int DefaultCoins = 0; // Default coins set to 0 for new players
        private const int BarrelDestroyPoints = 50;
        private const int LootablePoints = 50;

        private const string PermissionGiveCoins = "warriorpointsmanager.givecoins";

        private class PlayerInfo
        {
            public string Name { get; set; }
            public int Points { get; set; }
            public int Coins { get; set; } // New currency property
        }

        #region Oxide Hooks

        private void Init()
        {
            LoadPointsData();
            permission.RegisterPermission(PermissionGiveCoins, this);
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            Puts($"Player connected: {player.displayName} ({player.UserIDString})");
            AddOrUpdatePlayerPoints(player);
        }

        private void Unload()
        {
            SavePointsData();
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null || info.Initiator == null)
                return;

            var initiator = info.Initiator.ToPlayer();
            if (initiator == null)
                return;

            if (IsBarrelOrRoadsign(entity))
            {
                AwardPoints(initiator, BarrelDestroyPoints);
                Puts($"Awarded {BarrelDestroyPoints} points to {initiator.displayName} for destroying a barrel or roadsign.");
            }
        }

        private void OnLootEntityEnd(BasePlayer player, BaseEntity entity)
        {
            if (entity == null)
                return;

            if (IsLootable(entity))
            {
                var storageContainer = entity as StorageContainer;
                if (storageContainer == null)
                    return;

                if (IsContainerEmpty(storageContainer))
                {
                    AwardPoints(player, LootablePoints);
                    Puts($"Awarded {LootablePoints} points to {player.displayName} for fully looting a lootable.");
                }
            }
        }

        #endregion

        #region Points Management

        private void LoadPointsData()
        {
            try
            {
                var dataFile = Interface.Oxide.DataFileSystem.GetFile(PointsDataFileName);
                playerPoints = dataFile.ReadObject<Dictionary<string, PlayerInfo>>() ?? new Dictionary<string, PlayerInfo>();
                Puts("Points data loaded.");
            }
            catch (Exception ex)
            {
                Puts($"Error loading points data: {ex.Message}");
                playerPoints = new Dictionary<string, PlayerInfo>();
            }
        }

        private void SavePointsData()
        {
            try
            {
                var dataFile = Interface.Oxide.DataFileSystem.GetFile(PointsDataFileName);
                dataFile.WriteObject(playerPoints);
                Puts("Points data saved.");
            }
            catch (Exception ex)
            {
                Puts($"Error saving points data: {ex.Message}");
            }
        }

        private void AddOrUpdatePlayerPoints(BasePlayer player)
        {
            string userId = player.UserIDString;
            if (!playerPoints.ContainsKey(userId))
            {
                playerPoints[userId] = new PlayerInfo
                {
                    Name = player.displayName,
                    Points = DefaultPoints,
                    Coins = DefaultCoins // Initialize coins for new players to 0
                };
                player.ChatMessage($"Welcome! You have been given {DefaultPoints} points and {DefaultCoins} coins.");
                Puts($"Added {DefaultPoints} points and {DefaultCoins} coins for new player {player.displayName} ({userId}).");
            }
            else
            {
                var playerInfo = playerPoints[userId];
                if (playerInfo.Name != player.displayName)
                {
                    playerInfo.Name = player.displayName; // Update name in case it has changed
                    Puts($"Updated name for player {userId} to {player.displayName}.");
                }
                // Do not reset points or coins for existing players
            }
            // Comment out SavePointsData() to prevent automatic saving on connection
            // SavePointsData();
        }

        private void AwardPoints(BasePlayer player, int points)
        {
            string userId = player.UserIDString;
            if (playerPoints.ContainsKey(userId))
            {
                playerPoints[userId].Points += points;
                player.ChatMessage($"You have been awarded {points} points. Total points: {playerPoints[userId].Points}");
                SavePointsData();
            }
        }

        private void AwardCoins(BasePlayer player, int coins)
        {
            string userId = player.UserIDString;
            if (playerPoints.ContainsKey(userId))
            {
                playerPoints[userId].Coins += coins;
                player.ChatMessage($"You have been awarded {coins} coins. Total coins: {playerPoints[userId].Coins}");
                SavePointsData();
            }
        }

        // Public method to get player points and coins
        public int GetPlayerPoints(string userId)
        {
            if (playerPoints.ContainsKey(userId))
            {
                return playerPoints[userId].Points;
            }
            return 0;
        }

        public int GetPlayerCoins(string userId)
        {
            if (playerPoints.ContainsKey(userId))
            {
                return playerPoints[userId].Coins;
            }
            return 0;
        }

        // Public method to set player points and coins
        public void SetPlayerPoints(string userId, int points)
        {
            if (playerPoints.ContainsKey(userId))
            {
                playerPoints[userId].Points = points;
                SavePointsData();
            }
        }

        public void SetPlayerCoins(string userId, int coins)
        {
            if (playerPoints.ContainsKey(userId))
            {
                playerPoints[userId].Coins = coins;
                SavePointsData();
            }
        }

        private bool IsBarrelOrRoadsign(BaseCombatEntity entity)
        {
            string prefabName = entity.ShortPrefabName;
            return prefabName == "loot-barrel-1" || prefabName == "loot-barrel-2" ||
                   prefabName == "loot_barrel_1" || prefabName == "loot_barrel_2" ||
                   prefabName == "oil_barrel" ||
                   prefabName.StartsWith("roadsign") && prefabName.Length == 9 && char.IsDigit(prefabName[8]);
        }

        private bool IsLootable(BaseEntity entity)
        {
            string prefabName = entity.ShortPrefabName;
            return prefabName == "trash-pile-1" || prefabName == "crate_basic" || prefabName == "crate_elite" ||
                   prefabName == "crate_mine" || prefabName == "crate_normal" || prefabName == "crate_normal_2" ||
                   prefabName == "crate_normal_2_food" || prefabName == "crate_normal_2_medical" || prefabName == "crate_tools" ||
                   prefabName == "crate_underwater_advanced" || prefabName == "crate_underwater_basic" || prefabName == "foodbox" ||
                   prefabName == "loot_trash" || prefabName == "minecart" || prefabName == "crate_ammunition" || prefabName == "crate_food_1" ||
                   prefabName == "crate_food_2" || prefabName == "crate_fuel" || prefabName == "crate_medical" || prefabName == "tech_parts_1" ||
                   prefabName == "tech_parts_22" || prefabName == "vehicle_parts";
        }

        private bool IsContainerEmpty(StorageContainer container)
        {
            return container.inventory.itemList.Count == 0;
        }

        #endregion

        #region Chat Commands

        [ChatCommand("points")]
        private void PointsCommand(BasePlayer player, string command, string[] args)
        {
            string userId = player.UserIDString;
            if (playerPoints.TryGetValue(userId, out PlayerInfo playerInfo))
            {
                player.ChatMessage($"You have {playerInfo.Points} points and {playerInfo.Coins} coins.");
            }
            else
            {
                player.ChatMessage("You have no points or coins recorded.");
            }
        }

        [ChatCommand("givecoins")]
        private void GiveCoinsCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionGiveCoins))
            {
                player.ChatMessage("You do not have permission to use this command.");
                return;
            }

            if (args.Length != 2)
            {
                player.ChatMessage("Usage: /givecoins <playername> <amount>");
                return;
            }

            var targetPlayer = FindPlayerByName(args[0]);
            if (targetPlayer == null)
            {
                player.ChatMessage($"Player '{args[0]}' not found.");
                return;
            }

            if (!int.TryParse(args[1], out var amount) || amount <= 0)
            {
                player.ChatMessage("Invalid amount specified.");
                return;
            }

            AwardCoins(targetPlayer, amount);
            player.ChatMessage($"You have given {amount} coins to {targetPlayer.displayName}.");
        }

        #endregion

        #region Console Commands

        [ConsoleCommand("givecoins")]
        private void GiveCoinsConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (!arg.IsAdmin)
            {
                arg.ReplyWith("You do not have permission to use this command.");
                return;
            }

            if (arg.Args == null || arg.Args.Length != 2)
            {
                arg.ReplyWith("Usage: givecoins <playername> <amount>");
                return;
            }

            var targetPlayer = FindPlayerByName(arg.Args[0]);
            if (targetPlayer == null)
            {
                arg.ReplyWith($"Player '{arg.Args[0]}' not found.");
                return;
            }

            if (!int.TryParse(arg.Args[1], out var amount) || amount <= 0)
            {
                arg.ReplyWith("Invalid amount specified.");
                return;
            }

            AwardCoins(targetPlayer, amount);
            arg.ReplyWith($"You have given {amount} coins to {targetPlayer.displayName}.");
        }

        #endregion

        private BasePlayer FindPlayerByName(string name)
        {
            var players = BasePlayer.activePlayerList;
            foreach (var player in players)
            {
                if (player.displayName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return player;
                }
            }
            return null;
        }
    }
}
