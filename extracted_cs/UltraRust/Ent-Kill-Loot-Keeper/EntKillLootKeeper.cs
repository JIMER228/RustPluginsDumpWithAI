using System;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("EntKillLootKeeper", "Ultra", "1.0.1")]
    [Description("Keeps loot from destroyed container by command ent kill")]

    class EntKillLootKeeper : CovalencePlugin
    {
        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Args != null && arg.Args.Length == 2 && arg.Args[0] == "kill")
            {
                uint itemId = 0;

                if (uint.TryParse(arg.Args[1], out itemId))
                {
                    BaseNetworkable baseNetworkable = BaseNetworkable.serverEntities.Find(itemId);
                    if (baseNetworkable != null)
                    {
                        var container = baseNetworkable as LootContainer;
                        if (container != null)
                        {
                            Vector3 dropPosition = container.GetDropPosition();
                            Vector3 dropVelocity = container.GetDropVelocity();
                            float move = 0;
                            foreach (Item item in container.inventory.itemList)
                            {
                                move = move + 0.1F;
                                dropPosition = new Vector3(dropPosition.x + move, dropPosition.y + 1, dropPosition.z + move);

                                Item itemToDrop = null;
                                if (item.info.shortname == "blueprintbase")
                                {
                                    itemToDrop = ItemManager.Create(ItemManager.FindItemDefinition("blueprintbase"), 1);
                                    itemToDrop.blueprintTarget = item.blueprintTarget;
                                }
                                else
                                {
                                    itemToDrop = ItemManager.CreateByItemID(item.info.itemid, item.amount);
                                }

                                itemToDrop.Drop(dropPosition, dropVelocity, Quaternion.identity);
                            }
                        }
                    }
                }
            }

            return null;
        }
    }
}
