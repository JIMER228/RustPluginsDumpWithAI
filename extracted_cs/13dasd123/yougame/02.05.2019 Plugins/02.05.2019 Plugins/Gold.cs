// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Gold", "Hougan", "0.0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    public class Gold : RustPlugin
    {
        #region eNums

        private enum Type
        {
            None,
            Gather,
            Oven,
            Recycler
        }

        #endregion
        
        #region Variables

        [JsonProperty("Шанс выпадения золотой руды из серной руды")]
        private int DropChance = 100;
        [JsonProperty("Кол-во выпадаемой золотой руды (не меньше 2, не больше 10)")]
        private int DropAmount = 3;
        [JsonProperty("Сколько нужно необработанной руды для получения одной обработанной")]
        private int RecycleToOne = 10;
        [JsonProperty("1 руда переплавляется в ?? необработанной")]
        private int OvenTo = 5;
        [JsonProperty("Цена одного слитка")]
        private int PriceForOne = 1;
        [JsonProperty("Тут трогать только DisplayName")]
        private Dictionary<Type, CustomItem> Items = new Dictionary<Type,CustomItem>
        {
            [Type.Gather] = new CustomItem
            {
                DisplayName = "Золотая руда",
                ShortName = "glue",
                SkinID = 1654255287
            },
            [Type.Oven] = new CustomItem
            {
                DisplayName = "Необработанное золото",
                ShortName = "battery.small",
                SkinID = 1654304290
            },
            [Type.Recycler] = new CustomItem
            {
                DisplayName = "Обработанные слитки",
                ShortName = "sticks",
                SkinID = 1654304094
            },
        };
        
        #endregion

        #region Class

        private class CustomItem
        {
            public string DisplayName;
            public string ShortName;
            public ulong SkinID;

            public Item CreateItem(int amount)
            {
                Item item = ItemManager.CreateByPartialName(ShortName, amount);
                item.name = DisplayName;
                item.skin = SkinID;
                
                if (item.info.GetComponent<ItemModCookable>() != null)
                    item.info.GetComponent<ItemModCookable>().OnItemCreated(item);
                
                return item;
            }
        }

        #endregion
        
        #region Initialization
        
        private void CanAcceptItem(ItemContainer container, Item item, int targetPos)
        {
            if (item.info.shortname == Items[Type.Oven].ShortName && targetPos == -1)
            {
                item.name = Items[Type.Oven].DisplayName;
                item.skin = Items[Type.Oven].SkinID;
            }
            else if (item.info.shortname == Items[Type.Recycler].ShortName)
            {
                item.name = Items[Type.Recycler].DisplayName;
                item.skin = Items[Type.Recycler].SkinID;
            }
        }

        private void OnServerInitialized()
        {
            Server.Broadcast("!");
            var itemInfo = ItemManager.FindItemDefinition(Items[Type.Gather].ShortName);
      //  Слив плагинов server-rust by Apolo YouGame
            if (itemInfo.GetComponent<ItemModCookable>() == null) itemInfo.gameObject.AddComponent<ItemModCookable>();
      //  Слив плагинов server-rust by Apolo YouGame
            itemInfo.stackable = 1000;
      //  Слив плагинов server-rust by Apolo YouGame
            
            var burnMod = itemInfo.gameObject.GetComponent<ItemModCookable>();
      //  Слив плагинов server-rust by Apolo YouGame
            burnMod.becomeOnCooked = ItemManager.FindItemDefinition(Items[Type.Oven].ShortName);
            burnMod.amountOfBecome = OvenTo;
            burnMod.highTemp = 1200;
            burnMod.lowTemp = 800;
            burnMod.cookTime = 5;
            
            itemInfo = ItemManager.FindItemDefinition(Items[Type.Oven].ShortName);
      //  Слив плагинов server-rust by Apolo YouGame
            if (itemInfo.GetComponent<ItemModRecycleInto>() == null) itemInfo.gameObject.AddComponent<ItemModRecycleInto>();
      //  Слив плагинов server-rust by Apolo YouGame
            itemInfo.stackable = 100;
      //  Слив плагинов server-rust by Apolo YouGame
            
            var recycleMod = itemInfo.GetComponent<ItemModRecycleInto>();
      //  Слив плагинов server-rust by Apolo YouGame
            recycleMod.recycleIntoItem = ItemManager.FindItemDefinition(Items[Type.Recycler].ShortName);
            recycleMod.numRecycledItemMin = 1;
            recycleMod.numRecycledItemMax = 1;
            if (itemInfo.Blueprint == null) itemInfo.gameObject.AddComponent<ItemBlueprint>(); 
      //  Слив плагинов server-rust by Apolo YouGame
            itemInfo.Blueprint.ingredients = new List<ItemAmount> { new ItemAmount { itemDef = ItemManager.FindItemDefinition(Items[Type.Recycler].ShortName), amount = 2 } };
      //  Слив плагинов server-rust by Apolo YouGame
            itemInfo.Blueprint.amountToCreate = RecycleToOne / 2;
      //  Слив плагинов server-rust by Apolo YouGame
            
            itemInfo = ItemManager.FindItemDefinition(Items[Type.Recycler].ShortName);
      //  Слив плагинов server-rust by Apolo YouGame
            itemInfo.stackable = 100;
      //  Слив плагинов server-rust by Apolo YouGame
        }

        #endregion

        #region ChatCommands

        [ChatCommand("g.spawn")]
        private void CmdChatDebugGoldSpawn(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;

            foreach (var check in Items)
            {
                var item = check.Value.CreateItem(100);
                item.MoveToContainer(player.inventory.containerMain);
            }
        }

        [ChatCommand("exchange")]
        private void CmdChatExchange(BasePlayer player, string command, string[] args)
        {
            UI_DrawInterface(player);
        }

        [ConsoleCommand("UI_Gold")]
        private void CmdConsoleHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            switch (args.Args[0])
            {
                case "exchange":
                {
                    int amount = player.inventory.GetAmount(ItemManager.FindItemDefinition(Items[Type.Recycler].ShortName).itemid);
                    plugins.Find("RustStore").CallHook("APIChangeUserBalance", player.userID, amount * PriceForOne, new Action<string>((result) =>
                    {
                        if (result == "SUCCESS")
                        {
                            player.inventory.Take(null, ItemManager.FindItemDefinition(Items[Type.Recycler].ShortName).itemid, amount);
                            UI_DrawInterface(player);
                            return;
                        }

                        CuiHelper.DestroyUi(player, Layer);
                        player.ChatMessage($"Произошла ошибка, скорее всего вы не авторизованы в магазине!");
                        Interface.Oxide.LogDebug($"Баланс не был изменен, ошибка: {result}");
                    }));
                    break;
                }
            }
        }

        #endregion

        #region Interface

        private const string Layer = "UI_GoldExchange";
        private void UI_DrawInterface(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9", Material = "" }
            }, "Overlay", Layer);
            
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.55", AnchorMax = "1 1", OffsetMax = "0 0" },
                Text = { Text = $"ОБМЕН <b>СЛИТКОВ ЗОЛОТА</b> НА <b>БАЛАНС</b> В МАГАЗИНЕ", Align = TextAnchor.LowerCenter, Font = "robotocondensed-regular.ttf", FontSize = 28 }
            }, Layer, Layer + ".Balance");
            
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.55", OffsetMax = "0 0" },
                Text = { Text = $"Вы можете обменять 'слитки золота' на баланс в нашем магазине\n" +
                                $"<size=20>Курс обмена: 1 слиток -> {PriceForOne} рубль (-я/-ей)</size>", Align = TextAnchor.UpperCenter, Font = "robotocondensed-regular.ttf", FontSize = 24 }
            }, Layer, Layer + ".Balance");
            
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5 0.9", AnchorMax = "0.935 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = Layer },
                Text = { Text = "ВЫХОД", Align = TextAnchor.MiddleRight, Font = "robotocondensed-regular.ttf", FontSize = 28 }
            }, Layer, Layer + ".Exit");
            
            int amount = player.inventory.GetAmount(ItemManager.FindItemDefinition(Items[Type.Recycler].ShortName).itemid);
            if (amount == 0)
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.33 0.4", AnchorMax = "0.67 0.45", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0.03" },
                    Text = { Text = $"У ВАС ОТСУТСТВУЮТ СЛИТКИ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 24 }
                }, Layer);
            }
            else
            {
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.33 0.4", AnchorMax = "0.67 0.45", OffsetMax = "0 0" },
                    Button = { Color = "1 1 1 0.03", Command = "UI_Gold exchange" },
                    Text = { Text = $"ОБМЕНЯТЬ {amount} СЛИТКОВ НА {amount * PriceForOne} РУБЛЕЙ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 24 }
                }, Layer);
            }
            

            CuiHelper.AddUi(player, container);
        }

        #endregion
        
        #region Hooks
        
        private Item OnItemSplit(Item item, int amount)
        {
            var customItem = Items.FirstOrDefault(p => p.Value.ShortName == item.info.shortname && item.skin == p.Value.SkinID);
            if (customItem.Value != null)
            {
                Item x = ItemManager.CreateByPartialName(customItem.Value.ShortName, amount);
                x.name = customItem.Value.DisplayName;
                x.skin = customItem.Value.SkinID;
                x.amount = amount;
            
                item.amount -= amount;
                return x;
            }

            return null;
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!(entity is BasePlayer)) return;
            if (dispenser.gatherType != ResourceDispenser.GatherType.Ore || !dispenser.GetComponent<BaseEntity>().PrefabName.Contains("sulfur")) return;
            if (Oxide.Core.Random.Range(0, 100) > DropChance) return;

            Item dropItem = Items[Type.Gather].CreateItem(Oxide.Core.Random.Range(DropAmount - 2, DropAmount + 2));
            dropItem.MoveToContainer((entity as BasePlayer).inventory.containerMain);
            (entity as BasePlayer).SendConsoleCommand($"note.inv {item.info.itemid} {dropItem.amount} \"Золотая руда\"");
        }

        #endregion
    }
}
