using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui; // Add this namespace for CUI

namespace Oxide.Plugins
{
    [Info("EnderChest", "herbs.acab", "1.2.0")]
    [Description("Provides a personal storage space for players accessible from anywhere.")]

    public class EnderChest : RustPlugin
    {
        public class StoredData
        {
            public Dictionary<ulong, List<Item>> PlayerInventories = new Dictionary<ulong, List<Item>>();
        }

        private StoredData storedData;

        public class EnderChestConfig
        {
            [JsonProperty("Number of Slots")]
            public int NumberOfSlots { get; set; } = 36; // Default to 36 slots

            [JsonProperty("UI Title")]
            public string UITitle { get; set; } = "Ender Chest";
        }

        private EnderChestConfig config;
        private const string permissionUse = "enderchest.use";

        protected override void LoadDefaultConfig()
        {
            config = new EnderChestConfig();
            SaveConfig();
        }

        void LoadConfigValues()
        {
            config = Config.ReadObject<EnderChestConfig>();
            SaveConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        void Init()
        {
            LoadConfigValues();
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("EnderChest");

            permission.RegisterPermission(permissionUse, this);
        }

        void OnServerSave()
        {
            SaveData();
        }

        void Unload()
        {
            SaveData();
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("EnderChest", storedData);
        }

        [ChatCommand("enderchest")]
        void OpenEnderChest(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, permissionUse))
            {
                player.ChatMessage("You do not have permission to use the Ender Chest.");
                return;
            }

            if (!storedData.PlayerInventories.ContainsKey(player.userID))
            {
                storedData.PlayerInventories[player.userID] = new List<Item>();
            }

            var container = CreateContainer(player);
            player.inventory.loot.StartLootingEntity(container.entityOwner as BaseEntity);
            player.inventory.loot.AddContainer(container);
            player.inventory.loot.SendImmediate();

            CreateUI(player);
        }

        private ItemContainer CreateContainer(BasePlayer player)
        {
            var container = new ItemContainer();
            container.ServerInitialize(null, config.NumberOfSlots); // Use configured number of slots
            container.GiveUID();
            container.entityOwner = player;

            if (storedData.PlayerInventories.TryGetValue(player.userID, out var items))
            {
                foreach (var item in items)
                {
                    item.MoveToContainer(container);
                }
            }

            return container;
        }

        void OnLootEntityEnd(BasePlayer player, BaseEntity entity)
        {
            var container = player.inventory.loot.containers.Find(c => c.entityOwner == player);
            if (container != null)
            {
                SavePlayerInventory(player, container);
            }
        }

        private void SavePlayerInventory(BasePlayer player, ItemContainer container)
        {
            var items = new List<Item>();

            foreach (var item in container.itemList)
            {
                items.Add(item);
                item.RemoveFromContainer();
            }

            storedData.PlayerInventories[player.userID] = items;
            SaveData();
        }

        void CreateUI(BasePlayer player)
        {
            var elements = new CuiElementContainer();
            var panel = new CuiPanel
            {
                RectTransform = { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.7" },
                Image = { Color = "0.1 0.1 0.1 0.8" }
            };
            elements.Add(panel, "Overlay", "EnderChest");

            var label = new CuiLabel
            {
                RectTransform = { AnchorMin = "0.05 0.9", AnchorMax = "0.95 0.98" },
                Text = { Text = config.UITitle, FontSize = 22, Align = TextAnchor.MiddleCenter }
            };
            elements.Add(label, "EnderChest");

            CuiHelper.AddUi(player, elements);
        }
    }
}
