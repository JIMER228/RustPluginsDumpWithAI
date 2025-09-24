using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Configuration;
using Carbon.Core;
using Carbon.Extensions;
using Carbon.Plugins;

namespace Oxide.Plugins
{
    [Info("Backpacks", "WhiteThunder", "3.0.0")]
    [Description("Allows players to have a backpack that provides extra storage")]
    public class Backpacks : RustPlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Default Backpack Size")]
            public int DefaultBackpackSize { get; set; } = 1;

            [JsonProperty("Backpack Sizes")]
            public Dictionary<string, BackpackSize> BackpackSizes { get; set; } = new Dictionary<string, BackpackSize>
            {
                ["backpack.small"] = new BackpackSize { Capacity = 6, Permission = "backpacks.small" },
                ["backpack.medium"] = new BackpackSize { Capacity = 12, Permission = "backpacks.medium" },
                ["backpack.large"] = new BackpackSize { Capacity = 18, Permission = "backpacks.large" }
            };

            [JsonProperty("Drop on Death")]
            public bool DropOnDeath { get; set; } = true;

            [JsonProperty("Erase on Death")]
            public bool EraseOnDeath { get; set; } = false;
        }

        public class BackpackSize
        {
            [JsonProperty("Capacity")]
            public int Capacity { get; set; }

            [JsonProperty("Permission")]
            public string Permission { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }
            }
            catch
            {
                LogWarning("Configuration file is corrupt, using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You don't have permission to use backpacks!",
                ["BackpackOpened"] = "Backpack opened!",
                ["BackpackClosed"] = "Backpack closed!",
                ["BackpackFull"] = "Your backpack is full!",
                ["InvalidBackpack"] = "Invalid backpack type!"
            }, this);
        }

        private string GetMessage(string key, string playerId = null) => lang.GetMessage(key, this, playerId);

        #endregion

        #region Fields

        private readonly Dictionary<ulong, ItemContainer> playerBackpacks = new Dictionary<ulong, ItemContainer>();
        private DynamicConfigFile backpackData;

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission("backpacks.use", this);
            permission.RegisterPermission("backpacks.small", this);
            permission.RegisterPermission("backpacks.medium", this);
            permission.RegisterPermission("backpacks.large", this);

            AddCovalenceCommand("backpack", "BackpackCommand");
            AddCovalenceCommand("viewbackpack", "ViewBackpackCommand");

            backpackData = Interface.Oxide.DataFileSystem.GetFile("backpacks_data");
        }

        private void OnServerInitialized()
        {
            LoadBackpackData();
        }

        private void OnServerSave()
        {
            SaveBackpackData();
        }

        private void Unload()
        {
            SaveBackpackData();
            
            foreach (var backpack in playerBackpacks.Values)
            {
                backpack?.Kill();
            }
            playerBackpacks.Clear();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null) return;
            LoadPlayerBackpack(player);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null) return;
            SavePlayerBackpack(player);
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null) return;

            if (config.DropOnDeath)
            {
                DropBackpackItems(player);
            }

            if (config.EraseOnDeath)
            {
                EraseBackpack(player.userID);
            }
        }

        #endregion

        #region Commands

        [Command("backpack")]
        private void BackpackCommand(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, "backpacks.use"))
            {
                player.Reply(GetMessage("NoPermission", player.Id));
                return;
            }

            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            OpenBackpack(basePlayer);
        }

        [Command("viewbackpack")]
        private void ViewBackpackCommand(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, "backpacks.use"))
            {
                player.Reply(GetMessage("NoPermission", player.Id));
                return;
            }

            if (args.Length == 0)
            {
                player.Reply("Usage: /viewbackpack <player>");
                return;
            }

            var targetPlayer = covalence.Players.FindPlayer(args[0]);
            if (targetPlayer == null)
            {
                player.Reply("Player not found!");
                return;
            }

            var basePlayer = player.Object as BasePlayer;
            var targetBasePlayer = targetPlayer.Object as BasePlayer;
            
            if (basePlayer == null || targetBasePlayer == null) return;

            ViewBackpack(basePlayer, targetBasePlayer);
        }

        #endregion

        #region Core Methods

        private void LoadBackpackData()
        {
            var data = backpackData.ReadObject<Dictionary<ulong, List<ItemData>>>();
            if (data == null) return;

            foreach (var kvp in data)
            {
                var playerId = kvp.Key;
                var items = kvp.Value;
                
                if (items == null || items.Count == 0) continue;

                var backpack = CreateBackpack(playerId);
                if (backpack == null) continue;

                foreach (var itemData in items)
                {
                    var item = ItemManager.CreateByItemID(itemData.ItemId, itemData.Amount);
                    if (item != null)
                    {
                        item.condition = itemData.Condition;
                        item.MoveToContainer(backpack);
                    }
                }
            }
        }

        private void SaveBackpackData()
        {
            var data = new Dictionary<ulong, List<ItemData>>();

            foreach (var kvp in playerBackpacks)
            {
                var playerId = kvp.Key;
                var backpack = kvp.Value;

                if (backpack?.itemList == null || backpack.itemList.Count == 0) continue;

                var items = new List<ItemData>();
                foreach (var item in backpack.itemList)
                {
                    items.Add(new ItemData
                    {
                        ItemId = item.info.itemid,
                        Amount = item.amount,
                        Condition = item.condition
                    });
                }

                data[playerId] = items;
            }

            backpackData.WriteObject(data);
        }

        private void LoadPlayerBackpack(BasePlayer player)
        {
            if (player == null) return;

            if (!playerBackpacks.ContainsKey(player.userID))
            {
                CreateBackpack(player.userID);
            }
        }

        private void SavePlayerBackpack(BasePlayer player)
        {
            if (player == null) return;
            // Data is saved in SaveBackpackData method
        }

        private ItemContainer CreateBackpack(ulong playerId)
        {
            var capacity = GetBackpackCapacity(playerId);
            var backpack = new ItemContainer();
            backpack.ServerInitialize(null, capacity);
            backpack.GiveUID();

            playerBackpacks[playerId] = backpack;
            return backpack;
        }

        private int GetBackpackCapacity(ulong playerId)
        {
            var playerIdString = playerId.ToString();
            
            foreach (var size in config.BackpackSizes.Values.OrderByDescending(s => s.Capacity))
            {
                if (permission.UserHasPermission(playerIdString, size.Permission))
                {
                    return size.Capacity;
                }
            }

            return config.DefaultBackpackSize;
        }

        private void OpenBackpack(BasePlayer player)
        {
            if (player == null) return;

            if (!playerBackpacks.TryGetValue(player.userID, out var backpack))
            {
                backpack = CreateBackpack(player.userID);
            }

            if (backpack == null) return;

            player.inventory.loot.Clear();
            player.inventory.loot.PositionChecks = false;
            player.inventory.loot.entitySource = player;
            player.inventory.loot.itemSource = null;
            player.inventory.loot.AddContainer(backpack);
            player.inventory.loot.SendImmediate();
            player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "generic");

            player.ChatMessage(GetMessage("BackpackOpened", player.UserIDString));
        }

        private void ViewBackpack(BasePlayer viewer, BasePlayer target)
        {
            if (viewer == null || target == null) return;

            if (!playerBackpacks.TryGetValue(target.userID, out var backpack))
            {
                backpack = CreateBackpack(target.userID);
            }

            if (backpack == null) return;

            viewer.inventory.loot.Clear();
            viewer.inventory.loot.PositionChecks = false;
            viewer.inventory.loot.entitySource = viewer;
            viewer.inventory.loot.itemSource = null;
            viewer.inventory.loot.AddContainer(backpack);
            viewer.inventory.loot.SendImmediate();
            viewer.ClientRPCPlayer(null, viewer, "RPC_OpenLootPanel", "generic");
        }

        private void DropBackpackItems(BasePlayer player)
        {
            if (player == null) return;

            if (!playerBackpacks.TryGetValue(player.userID, out var backpack) || backpack?.itemList == null)
                return;

            var items = backpack.itemList.ToList();
            foreach (var item in items)
            {
                item.Drop(player.transform.position + Vector3.up, Vector3.zero);
            }
        }

        private void EraseBackpack(ulong playerId)
        {
            if (playerBackpacks.TryGetValue(playerId, out var backpack))
            {
                backpack?.Kill();
                playerBackpacks.Remove(playerId);
            }
        }

        #endregion

        #region API

        private Dictionary<ulong, ItemContainer> GetExistingBackpacks()
        {
            return new Dictionary<ulong, ItemContainer>(playerBackpacks);
        }

        private bool TryDepositBackpackItem(ulong playerId, Item item)
        {
            if (item == null) return false;

            if (!playerBackpacks.TryGetValue(playerId, out var backpack))
            {
                backpack = CreateBackpack(playerId);
            }

            return item.MoveToContainer(backpack);
        }

        #endregion

        #region Data Classes

        public class ItemData
        {
            public int ItemId { get; set; }
            public int Amount { get; set; }
            public float Condition { get; set; }
        }

        #endregion
    }
}

