// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("OreBonus", "r3dapple", "1.0.0")]
    class OreBonus : RustPlugin
    {
        private static float chance = 99f; //Шанс выпадения бонусной руды (в процентах, от 1 до 99)
        private static int radiation = 100; //Сколько выдавать радиации после переработки предмета
       
        ////////////////////////////////////
       
        private static int itemid = 204391461;
       
        object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (player == null) return null;
            if (dispenser.gatherType != ResourceDispenser.GatherType.Ore) return null;
            if (UnityEngine.Random.Range(0f, 100f) < chance)
            {
                switch (item.info.shortname)
                {
                    case "stones":
                        GiveOre(player, 1);
                        break;
                    case "metal.ore":
                        GiveOre(player, 2);
                        break;
                    case "hq.metal.ore":
                        GiveOre(player, 3);
                        break;
                    case "sulfur.ore":
                        GiveOre(player, 4);
                        break;
                }
            }
            return null;
        }
       
        private void GiveOre(BasePlayer player, int type)
        {
            ulong skinid = 0U;
            string newname = String.Empty;
           
            switch (type)
            {
                case 1:
                    skinid = 1499303078;
                    newname = "Радиоактивный камень";
                    break;
                case 2:
                    skinid = 1499311722;
                    newname = "Радиоактивный металл";
                    break;
                case 3:
                    skinid = 1499301592;
                    newname = "Радиоактивный МВК";
                    break;
                case 4:
                    skinid = 1499310834;
                    newname = "Радиоактивная сера";
                    break;
            }
           
            Item ore = ItemManager.CreateByItemID(itemid, 1, skinid);
            ore.name = newname;
           
            if (24 - player.inventory.containerMain.itemList.Count > 0)
            {
                ore.MoveToContainer(player.inventory.containerMain);
            }
            else if (6 - player.inventory.containerBelt.itemList.Count > 0)
            {
                ore.MoveToContainer(player.inventory.containerBelt);
            }
            else
            {
                ore.Drop(player.transform.position, Vector3.up);
                PrintToChat(player, "Радиоактивный предмет брошен Вам под ноги!");
            }
            PrintToChat(player, $"> Вы нашли предмет <color=#32CD32>{newname}</color>!");
            return;
        }
       
        void OnRecyclerToggle(Recycler recycler, BasePlayer player)
        {
            if (recycler.IsOn()) return;
            for (int i = 0; i < 6; i++)
            {
                Item slot = recycler.inventory.GetSlot(i);
                if (slot == null) continue;
                if (slot.info.itemid == itemid) player.metabolism.radiation_poison.value = radiation;
            }
           
        }
       
        private object OnRecycleItem(Recycler recycler, Item item)
        {
            if (item.info.itemid == itemid)
            {
                item.UseItem(1);
                switch (item.skin)
                {
                    case 1499303078:
                        recycler.MoveItemToOutput(ItemManager.CreateByName("stones", 10000 + Random.Range(1000, 10000)));
                        break;
                    case 1499311722:
                        recycler.MoveItemToOutput(ItemManager.CreateByName("metal.fragments", 10000 + Random.Range(1000, 10000)));
                        break;
                    case 1499301592:
                        recycler.MoveItemToOutput(ItemManager.CreateByName("metal.refined", 750 + Random.Range(100, 500)));
                        break;
                    case 1499310834:
                        recycler.MoveItemToOutput(ItemManager.CreateByName("sulfur", 5000 + Random.Range(1000, 5000)));
                        break;
                    default:
                        return null;
                }
                return true;
            }
            return null;
        }
       
        private object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (item.item.info.itemid == itemid) return false;
           
            return null;
        }
       
        private object CanStackItem(Item item, Item targetItem)
        {
            if (item.info.itemid == itemid) return false;
           
            return null;
        }
       
        [ChatCommand("getore")]
        private void cmdgetore(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                PrintToChat(player, "Нет прав!");
                return;
            }
            if (args.Length != 1)
            {
                PrintToChat(player, $"Используйте: /getore [номер руды]\n1 - камень\n2 - метал\n3 - МВК\n4 - сера");
                return;
            }
            int ruda = Int32.Parse(args[0]);
            GiveOre(player, ruda);
            return;
        }
       
        [ChatCommand("orec")]
        private void cmdoretest(BasePlayer player)
        {
            if (!player.IsAdmin) return;
            int count = 0;
            for (int i = 0; i < 50; i++)
            {
                if (UnityEngine.Random.Range(0f, 100f) < chance) count++;
            }
            PrintToChat(player, $"Из 50 руд выпадет примерно {count.ToString()} особых");
        }
    }
}