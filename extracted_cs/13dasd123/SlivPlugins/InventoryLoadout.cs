// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using System.Collections.Generic;
using System.Text;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Inventory Loadout", "Amino", "0.1.1")]
    [Description("Creates and saves loadouts for Scrim and Recoil Training Ground minigames.")]
    class InventoryLoadout : RustPlugin
    {
        private DynamicConfigFile _dataManager;
        private GameData _gameData;

        //loadout.list
        [ChatCommand("loadout.list")]
        void ShowInventory(BasePlayer player, string command, string[] args)
        {
            if (player == null || !player.IsAdmin)
            {
                return;
            }
            var loadoutNames = new StringBuilder("Loadout Names:");
            loadoutNames.AppendLine();
            foreach (var loadoutKey in _gameData.Loadout.Keys)
            {
                loadoutNames.AppendLine(loadoutKey);
            }
            player.ChatMessage(loadoutNames.ToString());
            PrintToConsole(loadoutNames.ToString());
            Puts(loadoutNames.ToString());
        }        
        
        //loadout.save <name>
        [ChatCommand("loadout.save")]
        void SaveInventory(BasePlayer player, string command, string[] args)
        {
            if (player == null || !player.IsAdmin || args.Length <= 0)
            {
                return;
            }

            var loadoutName = args[0];
            var inventoryItems = new List<LoadoutItem>();
            foreach (var item in player.inventory.containerMain.itemList)
            {
                if (item.info != null)
                {
                    inventoryItems.Add(CreateLoadout(item, "main"));
                }
                else
                {
                    PrintError("Item Info Not found");
                }
            }
            foreach (var item in player.inventory.containerBelt.itemList)
            {
                if (item.info != null)
                {
                    inventoryItems.Add(CreateLoadout(item, "belt"));
                }
                else
                {
                    PrintError("Item Info Not found");
                }
            }
            foreach (var item in player.inventory.containerWear.itemList)
            {
                if (item.info != null)
                {
                    inventoryItems.Add(CreateLoadout(item, "wear"));
                }
                else
                {
                    PrintError("Item Info Not found");
                }
            }

            _gameData.Loadout[loadoutName] = inventoryItems;
            SaveData();
            player.ChatMessage("Loadout Saved");
        }

        //loadout.take <name>
        [ChatCommand("loadout.take")]
        void LoadInventory(BasePlayer player, string command, string[] args)
        {
            if (player == null || !player.IsAdmin || args.Length <= 0)
            {
                return;
            }

            var loadoutName = args[0];
            GiveLoadout(player, loadoutName);
        }
        private LoadoutItem CreateLoadout(Item item, string container)
        {
            var loadout = new LoadoutItem
            {
                ShortName = item.info.shortname,
                Amount = item.amount,
                Position = item.position,
                SkinId = item.skin,
                Container = container
            };
            var weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                if (weapon.primaryMagazine != null)
                {
                    if (weapon.primaryMagazine.ammoType != null)
                        loadout.AmmoType = weapon.primaryMagazine.ammoType.shortname;
                    loadout.AmmoAmount = weapon.primaryMagazine.contents;
                }
            }

            var flameThrower = item.GetHeldEntity() as FlameThrower;
            if (flameThrower != null)
                loadout.AmmoAmount = flameThrower.ammo;

            if (item.contents != null)
            {
                foreach (var subItem in item.contents.itemList)
                {
                    if (subItem.info != null)
                    {
                        var subLoadout = new LoadoutSubItem
                        {
                            ItemShortName = subItem.info.shortname,
                            Amount = subItem.amount
                        };
                        loadout.Contents.Add(subLoadout);
                    }
                    else
                    {
                        PrintError($"Item content for {item.info.shortname} doesn't have ItemInfo");
                    }
                }
            }

            return loadout;
        }
        private Item CreateItem(LoadoutItem itemData)
        {
            var item = ItemManager.CreateByName(itemData.ShortName, itemData.Amount, itemData.SkinId);

            var weapon = item.GetHeldEntity() as BaseProjectile;
            if (weapon != null)
            {
                if (!string.IsNullOrEmpty(itemData.AmmoType))
                    weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(itemData.AmmoType);
                weapon.primaryMagazine.contents = itemData.AmmoAmount;
            }

            var flameThrower = item.GetHeldEntity() as FlameThrower;
            if (flameThrower != null)
                flameThrower.ammo = itemData.AmmoAmount;


            if (itemData.Contents != null)
            {
                foreach (var contentData in itemData.Contents)
                {
                    var newContent = ItemManager.CreateByName(contentData.ItemShortName, contentData.Amount);
                    newContent?.MoveToContainer(item.contents);
                }
            }
            return item;
        }

        [HookMethod("GiveLoadout")]
        private bool GiveLoadout(BasePlayer player, string loadoutName, bool keepCurrentInventory = false)
        {
            if (_gameData?.Loadout == null || (player.userID.IsSteamId() && !player.IsConnected))
            {
                return false;
            }
            List<LoadoutItem> items;
            if (_gameData.Loadout.TryGetValue(loadoutName, out items) && items != null && items.Count > 0)
            {
                try
                {
                    if (!keepCurrentInventory)
                    {
                        player.inventory.containerMain?.Clear();
                        player.inventory.containerWear?.Clear();
                        player.inventory.containerBelt?.Clear();
                        ItemManager.DoRemoves();
                    }
                    foreach (var loadoutItem in items)
                    {
                        var item = CreateItem(loadoutItem);
                        item.position = loadoutItem.Position;
                        ItemContainer container = null;
                        switch (loadoutItem.Container.ToLower())
                        {
                            case "belt":
                                {
                                    container = player.inventory.containerBelt;
                                    break;
                                }
                            case "wear":
                                {
                                    container = player.inventory.containerWear;
                                    break;
                                }
                            case "main":
                                {
                                    container = player.inventory.containerMain;
                                    break;
                                }
                        }
                        if (container != null)
                            item.SetParent(container);
                    }
                    return true;
                }
                catch (Exception exception)
                {
                    PrintError($"Error Message {exception.Message} Inner Message: {exception.InnerException?.Message}");
                    return false;
                }
            }
            return false;
        }

        [HookMethod("IsValidLoadout")]
        private bool IsValidLoadout(string loadoutName)
        {
            if (string.IsNullOrWhiteSpace(loadoutName))
            {
                return false;
            }

            if (_gameData?.Loadout == null)
            {
                return false;
            }
            return _gameData.Loadout.ContainsKey(loadoutName);
        }
        private void SaveData()
        {
            _dataManager.WriteObject(_gameData);
        }
        private void LoadData()
        {
            try
            {
                _gameData = _dataManager.ReadObject<GameData>();
            }
            catch
            {
                _gameData = new GameData();
            }
        }
        private void Loaded()
        {
            _dataManager = Interface.Oxide.DataFileSystem.GetFile(nameof(InventoryLoadout));
        }
        private void OnServerInitialized()
        {
            LoadData();
        }
        public class GameData
        {
            public Dictionary<string, List<LoadoutItem>> Loadout { get; set; } = new Dictionary<string, List<LoadoutItem>>(StringComparer.OrdinalIgnoreCase);
        }
        public class LoadoutItem
        {
            [JsonProperty(PropertyName = "Item Short Name")]
            public string ShortName { get; set; }

            [JsonProperty(PropertyName = "Skin Id")]
            public ulong SkinId { get; set; }

            [JsonProperty(PropertyName = "Container Type")]
            public string Container { get; set; }

            [JsonProperty(PropertyName = "Position")]
            public int Position { get; set; }

            [JsonProperty(PropertyName = "Item Amount")]
            public int Amount { get; set; }

            [JsonProperty(PropertyName = "Ammo Amount")]
            public int AmmoAmount { get; set; }

            [JsonProperty(PropertyName = "Ammo Type")]
            public string AmmoType { get; set; }

            [JsonProperty(PropertyName = "Mods")]
            public List<LoadoutSubItem> Contents { get; set; } = new List<LoadoutSubItem>();
        }
        public class LoadoutSubItem
        {
            [JsonProperty(PropertyName = "Item Short Name")]
            public string ItemShortName { get; set; }

            [JsonProperty(PropertyName = "Item Amount")]
            public int Amount { get; set; }
        }
    }
}
