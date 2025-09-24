using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("InstantSmelt", "A0001", "1.0.0")]
    [Description("")]

    class RInstantSmelt : RustPlugin
    {
        void Init()
        {
            permission.RegisterPermission("rinstantsmelt.use", this);
        }

        Item NewItem;

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity?.ToPlayer();
            if (player == null) return;
            if (!permission.UserHasPermission(player.userID.ToString(), "rinstantsmelt.use")) return;

            switch (item.info.shortname)
            {
                case "sulfur.ore": // Sulfur ore
                    {
                        NewItem = ItemManager.CreateByName("sulfur", item.amount);
                        NextTick(() =>
                        {
                            List<Item> items = new List<Item>();
                            player.inventory.Take(items, item.info.itemid, item.amount);
                            if (!NewItem.MoveToContainer(player.inventory.containerMain))
                            {
                                NewItem.Drop(player.GetCenter(), Vector3.up);
                            }
                        });
                        break;
                    }
                case "metal.ore": // Metal Ore
                    {
                        NewItem = ItemManager.CreateByName("metal.fragments", item.amount);
                        NextTick(() =>
                        {
                            List<Item> items = new List<Item>();
                            player.inventory.Take(items, item.info.itemid, item.amount);
                            if (!NewItem.MoveToContainer(player.inventory.containerMain))
                            {
                                NewItem.Drop(player.GetCenter(), Vector3.up);
                            }
                        });
                        break;
                    }
                case "hq.metal.ore": // HQM Ore
                    {
                        NewItem = ItemManager.CreateByName("metal.refined", item.amount);
                        NextTick(() =>
                        {
                            List<Item> items = new List<Item>();
                            player.inventory.Take(items, item.info.itemid, item.amount);
                            if (!NewItem.MoveToContainer(player.inventory.containerMain))
                            {
                                NewItem.Drop(player.GetCenter(), Vector3.up);
                            }
                        });
                        break;
                    }
            }
        }

        private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (player == null) return;
            if (!permission.UserHasPermission(player.userID.ToString(), "instantsmelt.use")) return;

            switch (item.info.shortname)
            {
                case "sulfur.ore": // Sulfur ore
                    {
                        NewItem = ItemManager.CreateByName("sulfur", item.amount);
                        NextTick(() =>
                        {
                            List<Item> items = new List<Item>();
                            player.inventory.Take(items, item.info.itemid, item.amount);
                            if (!NewItem.MoveToContainer(player.inventory.containerMain))
                            {
                                NewItem.Drop(player.GetCenter(), Vector3.up);
                            }
                        });
                        break;
                    }
                case "metal.ore": // Metal Ore
                    {
                        NewItem = ItemManager.CreateByName("metal.fragments", item.amount);
                        NextTick(() =>
                        {
                            List<Item> items = new List<Item>();
                            player.inventory.Take(items, item.info.itemid, item.amount);
                            if (!NewItem.MoveToContainer(player.inventory.containerMain))
                            {
                                NewItem.Drop(player.GetCenter(), Vector3.up);
                            }
                        });
                        break;
                    }
                case "hq.metal.ore": // HQM Ore
                    {
                        NewItem = ItemManager.CreateByName("metal.refined", item.amount);
                        NextTick(() =>
                        {
                            List<Item> items = new List<Item>();
                            player.inventory.Take(items, item.info.itemid, item.amount);
                            if (!NewItem.MoveToContainer(player.inventory.containerMain))
                            {
                                NewItem.Drop(player.GetCenter(), Vector3.up);
                            }
                        });
                        break;
                    }
            }
        }

        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (player == null) return;
            if (!permission.UserHasPermission(player.userID.ToString(), "instantsmelt.use")) return;

            switch (item.info.shortname)
            {
                case "sulfur.ore": // Sulfur ore
                    {
                        NewItem = ItemManager.CreateByName("sulfur", item.amount);
                        NextTick(() =>
                        {
                            List<Item> items = new List<Item>();
                            player.inventory.Take(items, item.info.itemid, item.amount);
                            if (!NewItem.MoveToContainer(player.inventory.containerMain))
                            {
                                NewItem.Drop(player.GetCenter(), Vector3.up);
                            }
                        });
                        break;
                    }
                case "metal.ore": // Metal Ore
                    {
                        NewItem = ItemManager.CreateByName("metal.fragments", item.amount);
                        NextTick(() =>
                        {
                            List<Item> items = new List<Item>();
                            player.inventory.Take(items, item.info.itemid, item.amount);
                            if (!NewItem.MoveToContainer(player.inventory.containerMain))
                            {
                                NewItem.Drop(player.GetCenter(), Vector3.up);
                            }
                        });
                        break;
                    }
            }
        }
    }
}
