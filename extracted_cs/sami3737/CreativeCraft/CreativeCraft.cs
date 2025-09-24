using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("CreativeCraft", "Sami37", "1.1.8")]
    [Description("Craft anything anywhere.")]

    class CreativeCraft : RustPlugin
    {
        private List<object> items;
        private int Capacity, Amount, DefaultStackSize;
        private bool Changed;

        static List<int> DefaultItems()
        {
            var d = new List<int>
            {
                -151838493
            };
            return d;
        }

        protected override void LoadDefaultConfig()
        {
            Config.Clear();
            Config["Items"] = DefaultItems();
            Config["Inventory Capacity"] = 60;
            Config["Item Amount"] = 500000;
            SaveConfig();
        }

        void Loaded()
        {
            items = (List<object>)Config["Items"];
            Capacity = (int)Config["Inventory Capacity"];
            Amount = (int)Config["Item Amount"];
        }

        [HookMethod("GetInventoryCapacity")]
        private int GetInventoryCapacity() => Capacity;

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (items == null) return;
            if (container?.playerOwner == null) return;
            if (container.playerOwner.IsAdmin) return;
            if (container.playerOwner.net?.connection != null && container.playerOwner.net.connection.authLevel > 0) 
                if (container.playerOwner != null && item.position > 23)
                {
                    foreach (var itemToRemove in items)
                        if (Convert.ToInt32(itemToRemove) == item.info.itemid)
                            item.Remove();
                    SendReply(container.playerOwner, "You haven't enough space.");
                }
        }
        void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            Unsubscribe(nameof(OnItemAddedToContainer));
            NextTick(() =>
            {
                Subscribe(nameof(OnItemAddedToContainer));
            });
        }

        void OnReloadMagazine(BasePlayer player, BaseProjectile projectile)
        {
            Unsubscribe(nameof(OnItemAddedToContainer));
            NextTick(() =>
            {
                Subscribe(nameof(OnItemAddedToContainer));
            });
        }

        void OnPlayerInit(BasePlayer player)
        {
            Unsubscribe(nameof(OnItemAddedToContainer));
            NextTick(() =>
            {
                DefaultStackSize = player.inventory.containerMain.maxStackSize;
                player.inventory.containerMain.capacity = Capacity; //add more slot to inventory
                player.inventory.containerMain.maxStackSize = Amount; //set maxstacksize
                int i = player.inventory.containerMain.capacity - 1;
                foreach (var item in items)
                {
                    GiveItem(player.inventory, BuildItem(Convert.ToInt32(item), Amount, 0),
                        player.inventory.containerMain, i);
                    i--;
                }
                player.inventory.containerMain.maxStackSize = DefaultStackSize; //set maxstacksize
                player.inventory.SendUpdatedInventory(0, player.inventory.containerMain, true);
                Subscribe(nameof(OnItemAddedToContainer));
            });
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var container = player.inventory.containerMain;
            List<Item> itemList = new List<Item>();
            foreach (var it in container.itemList)
            {
                Item item = it;
                if (item != null && item.position > 23)
                {
                    itemList.Add(item);
                }
            }
            foreach (var item in itemList)
            {
                container.Take(null, item.info.itemid, item.amount);
            }
        }

        void OnPlayerDie(BasePlayer player, HitInfo info)
        {
            var container = player.inventory.containerMain;
            List<Item> itemList = new List<Item>();
            foreach (var it in container.itemList)
            {
                Item item = it;
                if (item != null && item.position > 23)
                {
                    itemList.Add(item);
                }
            }
            foreach (var item in itemList)
            {
                container.Take(null, item.info.itemid, item.amount);
            }
        }

        void OnPlayerRespawned(BasePlayer player)
        {
            player.inventory.containerMain.capacity = Capacity; //add more slot to inventory
            player.inventory.containerMain.maxStackSize = Amount; //set maxstacksize
            player.inventory.SendUpdatedInventory(0, player.inventory.containerMain, true);
            Unsubscribe(nameof(OnItemAddedToContainer));
            NextTick(() =>
            {
                int i = Capacity - 1;
                foreach (var item in items)
                {
                    Item itemss = BuildItem(Convert.ToInt32(item), Amount, 0);
                    GiveItem(player.inventory, itemss,
                        player.inventory.containerMain, i);
                    i--;
                }
                player.inventory.containerMain.maxStackSize = DefaultStackSize; //set maxstacksize
                player.inventory.SendUpdatedInventory(0, player.inventory.containerMain, true);
                Subscribe(nameof(OnItemAddedToContainer));
            });
        }

        bool GiveItem(PlayerInventory inv, Item item, ItemContainer container = null, int position = -1)
        {
            if (item == null)
            {
                return false;
            }
            return container != null && item.MoveToContainer(container, position, true) || item.MoveToContainer(inv.containerMain, position, true) || item.MoveToContainer(inv.containerBelt, position, true);
        }

        private Item BuildItem(int itemid, int amount, ulong skin)
        {
            if (amount < 1) amount = 1;
            Item item = ItemManager.CreateByItemID(itemid, amount, skin);
            if(item == null)
                PrintWarning("ItemID: " + itemid + " is wrong, please change it.");
            return item;
        }
    }
}