using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("UILoot", "Hougan", "0.1.6")]
    public class UILoot : RustPlugin
    {
        #region Classes

        private class RateHandler
        {
            public class Rates
            {
                public class Items
                {
                    internal class Amount
                    {
                        [JsonProperty("Минимальное количество")]
                        public int Min;

                        [JsonProperty("Максимальное количество")]
                        public int Max;

                        public int GenerateAmount() => Core.Random.Range(Min, Max);

                        public Amount(int min, int max)
                        {
                            Min = min;
                            Max = max;
                        }
                    }

                    [JsonProperty("Короткое название предмета")]
                    public string ShortName;

                    [JsonProperty("SkinID предмета")] public ulong SkinID;

                    [JsonProperty("Настройки количества предмета")]
                    public Amount Amounts;

                    [JsonProperty("Шанс выпадения предмета")]
                    public int DropChance = 100;

                    [JsonProperty("Будет ли спавниться как чертеж")]
                    public bool IsBlueprint;

                    public Item ToItem()
                    {
                        Item item = null;
                        if (IsBlueprint)
                        {
                            item = ItemManager.CreateByItemID(-996920608);

                            var info = ItemManager.FindItemDefinition(ShortName);
                            item.blueprintTarget = info.itemid;
                        }
                        else
                        {
                            item = ItemManager.CreateByPartialName(ShortName, Amounts.GenerateAmount());
                            item.skin = SkinID;
                        }

                        return item;
                    }

                    public static Items FromItem(Item item) => new Items(
                        item.IsBlueprint() ? item.blueprintTargetDef.shortname : item.info.shortname, item.amount,
                        item.amount, item.skin, item.IsBlueprint());

                    public Items()
                    {
                    }

                    public Items(string shortName, int minAmount, int maxAmount = 0, ulong skinId = 0, bool bp = false)
                    {
                        ShortName = shortName;
                        Amounts = new Amount(minAmount, maxAmount);
                        SkinID = skinId;
                        IsBlueprint = bp;
                    }
                }

                public class Scrap
                {
                    [JsonProperty("Активировать принудительное выпадение скрапа")]
                    public bool EnableDrop = true;

                    [JsonProperty("Минимальное кол-во")] public int MinAmount = 2;
                    [JsonProperty("Максимальное кол-во")] public int MaxAmount = 5;

                    public Item CreateScrap()
                    {
                        return ItemManager.CreateByPartialName("scrap", Oxide.Core.Random.Range(MinAmount, MaxAmount));
                    }
                }

                [JsonProperty("Название префаба объекта")]
                public string PrefabName;

                [JsonProperty("Количество выпадаемых предметов")]
                public int DropAmount;

                [JsonProperty("Запрещать повторяющиеся предметы")]
                public bool BlockRepeat;

                [JsonProperty("Настройки выпадения скрапа")]
                public Scrap ScrapSettings;

                [JsonProperty("Список выпадающих предметов")]
                public List<Items> ItemsList = new List<Items>();

                public Rates()
                {
                }

                public Rates(BaseEntity entity)
                {
                    PrefabName = entity.PrefabName;
                    DropAmount = 1;
                    BlockRepeat = false;

                    ScrapSettings = new Scrap();
                    List<Items> itemList = new List<Items>();

                    var obj = entity.GetComponent<LootContainer>();
                    /*if (entity.GetComponent<SupplyDrop>())
                    {
                        foreach (var check in entity.GetComponent<SupplyDrop>().LootSpawnSlots.Select(p => p.definition))
                        {
                            foreach (var drop in check.subSpawn.Select(p => p.category))
                            {
                                foreach (var item in drop.subSpawn.SelectMany(p => p.category.items))
                                {
                                    if (!ItemsList.Any(p => p.ShortName == item.itemDef.shortname && p.SkinID == 0 && p.IsBlueprint == item.itemDef.spawnAsBlueprint))
                                        itemList.Add(new Items(item.itemDef.shortname, (int) item.startAmount, item.maxAmount == -1 || item.maxAmount == 0 ? (int) item.startAmount : (int) item.maxAmount, 0, item.itemDef.spawnAsBlueprint)); 
                                }
                            }
                        }
                    }*/
                    if (obj != null)
                    {
                        foreach (var check in entity.GetComponent<LootContainer>()?.LootSpawnSlots
                                     ?.Select(p => p.definition))
                        {
                            foreach (var drop in check?.subSpawn?.Select(p => p.category))
                            {
                                foreach (var item in drop?.subSpawn?.SelectMany(p => p.category.items))
                                {
                                    if (!ItemsList.Any(p =>
                                            p.ShortName == item.itemDef.shortname && p.SkinID == 0 &&
                                            p.IsBlueprint == item.itemDef.spawnAsBlueprint))
                                        itemList.Add(new Items(item.itemDef.shortname, (int)item.startAmount,
                                            item.maxAmount == -1 || item.maxAmount == 0
                                                ? (int)item.startAmount
                                                : (int)item.maxAmount, 0, item.itemDef.spawnAsBlueprint));
                                }
                            }
                        }

                        if (obj.lootDefinition?.subSpawn != null)
                        {
                            obj.lootDefinition.subSpawn.ToList().ForEach(p =>
                            {
                                if (p.category?.subSpawn != null)
                                {
                                    p.category.subSpawn.ToList().ForEach(t =>
                                    {
                                        if (t.category?.items != null)
                                        {
                                            t.category.items.ToList().ForEach(z =>
                                            {
                                                if (!ItemsList.Any(x =>
                                                        x.ShortName == z.itemDef.shortname && x.SkinID == 0 &&
                                                        x.IsBlueprint == z.itemDef.spawnAsBlueprint))
                                                    itemList.Add(new Items(z.itemDef.shortname, (int)z.startAmount,
                                                        z.maxAmount == -1 || z.maxAmount == 0
                                                            ? (int)z.startAmount
                                                            : (int)z.maxAmount, 0, z.itemDef.spawnAsBlueprint));
                                            });
                                        }
                                    });
                                }
                            });
                        }
                    }
                    else
                    {
                        ItemsList = new List<Items>
                        {
                            new Items("wood", 5, 1007, 0),
                            new Items("stones", 6, 151007, 0),
                            new Items("gears", 6, 110079, 0),
                            new Items("rifle.ak", 6, 5810071, 0),
                            new Items("rope", 6, 100788, 0),
                        };
                        return;
                    }

                    ItemsList = itemList;
                }
            }

            [JsonProperty("Список настроеных рейтов")]
            public List<Rates> Rateses = new List<Rates>();

            public Rates GetRates(BaseEntity entity)
            {
                if (!(entity is LootContainer)) return null;

                var obj = Rateses.FirstOrDefault(p => p.PrefabName == entity.PrefabName);
                return obj;
            }

            public Rates GetRates(string prefabName)
            {
                var obj = Rateses.FirstOrDefault(p => p.PrefabName == prefabName);
                return obj;
            }

            public Rates CreateRates(BaseEntity entity)
            {
                var rate = GetRates(entity.PrefabName);
                if (rate != null)
                {
                    Interface.Oxide.LogError($"Tried to create rates, to exists rates!");
                    return null;
                }

                rate = new Rates(entity);
                Rateses.Add(rate);

                return rate;
            }

            public void CopyTo(BasePlayer player, Rates rates)
            {
                if (!player.IsAdmin) return;

                rates.ItemsList.ForEach(p => p.ToItem().MoveToContainer(player.inventory.containerMain));
            }

            public void CopyFrom(BasePlayer player, Rates rates)
            {
                if (!player.IsAdmin) return;

                rates.ItemsList.Clear();
                player.inventory.AllItems().ToList().ForEach(p => rates.ItemsList.Add(Rates.Items.FromItem(p)));
            }
        }

        #endregion

        #region Variables

        [PluginReference] private Plugin ImageLibrary;

        private string AdminPermission = "UILoot.Admin";
        private static RateHandler Handler = new RateHandler();

        #endregion

        #region Initialization

        private void OnServerInitialized()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile(Name))
                Handler = Interface.Oxide.DataFileSystem.ReadObject<RateHandler>(Name);

            permission.RegisterPermission(AdminPermission, this);
            timer.Every(30, SaveData);
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, Handler);

        #endregion

        #region Hooks

        private void OnEntitySpawned(BaseEntity entity)
        {
            if (entity is LootContainer) PopulateLoot(entity);
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity.PrefabName.Contains("barrel"))
            {
                DropLoot(entity);
            }
        }

        private void OnHammerHit(BasePlayer player, HitInfo info)
        {
            if (!CanConfigure(player) || !(info?.HitEntity is LootContainer)) return;

            var rate = Handler.GetRates(info?.HitEntity);
            if (rate == null) rate = Handler.CreateRates(info?.HitEntity);

            UI_DrawRate(player, rate, 0);
        }

        private void Unload()
        {
            foreach (var obj in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(obj, MainLayer);
            }
        }

        #endregion

        #region Interface

        private const string MainLayer = "UI_LootUIMainLayer";

        private void UI_DrawLayer(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, MainLayer);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0.9" }
            }, "Overlay", MainLayer);

            /*container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9", AnchorMax = "1 1.03", OffsetMax = "0 0" },
                Text = { Text = "ГРАФИЧЕСКАЯ НАСТРОЙКА ВЫПАДАЮЩИХ ПРЕДМЕТОВ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 32, Color = "1 1 1 0.5" }
            }, MainLayer);*/

            CuiHelper.AddUi(player, container);
        }

        private const string ChooseLayer = "UI_LootUIChooseLayer";

        private void UI_DrawCreation(BasePlayer player, BaseEntity entity)
        {
            CuiElementContainer container = new CuiElementContainer();
            UI_DrawLayer(player);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "1 0.6", OffsetMax = "0 0" },
                Text =
                {
                    Text = "Для этого ящика отсутствуют особые настройки, создать их?\n" +
                           "<size=16>Вам придётся самостоятельно настроить кол-во выпадающих предметов</size>",
                    Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 26
                }
            }, MainLayer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.4 0.44", AnchorMax = "0.49 0.49", OffsetMax = "0 0" },
                Button = { Color = "0.8 0.5 0.5 0.6", Close = MainLayer },
                Text =
                {
                    Text = "НЕТ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 20
                }
            }, MainLayer);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.51 0.44", AnchorMax = "0.6 0.49", OffsetMax = "0 0" },
                Button =
                {
                    Color = "0.5 0.8 0.5 0.6", Close = MainLayer,
                    Command = $"UI_UILootHandler create {entity.PrefabName}"
                },
                Text =
                {
                    Text = "ДА", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 20
                }
            }, MainLayer);

            CuiHelper.AddUi(player, container);
        }

        private const string ShowRate = "UI_LootUIShowRatesLayer";

        private void UI_DrawRate(BasePlayer player, RateHandler.Rates rate, int page)
        {
            CuiElementContainer container = new CuiElementContainer();
            UI_DrawLayer(player);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Image = { Color = "0 0 0 0" }
            }, MainLayer, ShowRate);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button = { Color = "0 0 0 0", Close = MainLayer },
                Text = { Text = "" }
            }, ShowRate);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "25 -75", OffsetMax = "500 -10" },
                Button =
                {
                    Color = "0 0 0 0", Command = $"UI_UILootHandler populate {rate.PrefabName.Replace(" ", "+")}",
                    Close = MainLayer
                },
                Text =
                {
                    Text = "ПЕРЕНАПОЛНИТЬ", Align = TextAnchor.UpperLeft, Font = "robotocondensed-regular.ttf",
                    FontSize = 24
                }
            }, ShowRate);


            container.Add(new CuiButton
            {
                RectTransform =
                    { AnchorMin = "0.6 1", AnchorMax = "0.6 1", OffsetMin = "-175 -75", OffsetMax = "-150 -10" },
                Button = { Color = "0 0 0 0", Command = $"UI_UILootHandler" },
                Text =
                {
                    Text = rate.DropAmount.ToString(), Align = TextAnchor.UpperCenter,
                    Font = "robotocondensed-regular.ttf", FontSize = 24
                }
            }, ShowRate, ShowRate + ".Amount");

            container.Add(new CuiButton
            {
                RectTransform =
                    { AnchorMin = "0.6 1", AnchorMax = "0.6 1", OffsetMin = "-300 -75", OffsetMax = "-184 0" },
                Button =
                {
                    Color = "0 0 0 0", Material = "",
                    Command = $"UI_UILootHandler amount {rate.PrefabName.Replace(" ", "+")} {page - 1}"
                },
                Text =
                {
                    Text = "-", Color = rate.DropAmount > 0 ? "1 1 1 1" : "1 1 1 0.2", Align = TextAnchor.UpperRight,
                    Font = "robotocondensed-regular.ttf", FontSize = 40
                }
            }, ShowRate);

            container.Add(new CuiButton
            {
                RectTransform =
                    { AnchorMin = "0.6 1", AnchorMax = "0.6 1", OffsetMin = "-140 -75", OffsetMax = "-25 -10" },
                Button =
                {
                    Color = "0 0 0 0", Material = "",
                    Command = $"UI_UILootHandler amount {rate.PrefabName.Replace(" ", "+")} {page + 1}"
                },
                Text =
                {
                    Text = "+", Color = "1 1 1 1", Align = TextAnchor.UpperLeft, Font = "robotocondensed-regular.ttf",
                    FontSize = 23
                }
            }, ShowRate);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-350 -75", OffsetMax = "-25 -10" },
                Button =
                {
                    Color = "0 0 0 0", Material = "",
                    Command = $"UI_UILootHandler recover {rate.PrefabName.Replace(" ", "+")}"
                },
                Text =
                {
                    Text = "ВОССТАНОВИТЬ", Color = "1 1 1 1", Align = TextAnchor.UpperRight,
                    Font = "robotocondensed-regular.ttf", FontSize = 24
                }
            }, ShowRate);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "25 10", OffsetMax = "350 75" },
                Button = { Color = "0 0 0 0", Command = $"UI_UILootHandler to {rate.PrefabName.Replace(" ", "+")}" },
                Text =
                {
                    Text = "СКОПИРОВАТЬ <b>В</b> ИНВЕНТАРЬ", Align = TextAnchor.LowerLeft,
                    Font = "robotocondensed-regular.ttf", FontSize = 24
                }
            }, ShowRate);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-350 10", OffsetMax = "-25 75" },
                Button = { Color = "0 0 0 0", Command = $"UI_UILootHandler from {rate.PrefabName.Replace(" ", "+")}" },
                Text =
                {
                    Text = "СКОПИРОВАТЬ <b>ИЗ</b> ИНВЕНТАРЯ", Align = TextAnchor.LowerRight,
                    Font = "robotocondensed-regular.ttf", FontSize = 24
                }
            }, ShowRate);

            container.Add(new CuiButton
            {
                RectTransform =
                    { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-150 10", OffsetMax = "150 75" },
                Button =
                {
                    Color = "0 0 0 0", Command = $"UI_UILootHandler from {rate.PrefabName.Replace(" ", "+")} add"
                },
                Text =
                {
                    Text = "ДОБАВИТЬ <b>ИЗ</b> ИНВЕНТАРЯ", Align = TextAnchor.LowerCenter,
                    Font = "robotocondensed-regular.ttf", FontSize = 24
                }
            }, ShowRate);

            container.Add(new CuiButton
            {
                RectTransform =
                    { AnchorMin = "0 0.56", AnchorMax = "0 0.56", OffsetMin = "5 -125", OffsetMax = "125 0" },
                Button =
                {
                    Color = "0 0 0 0", Material = "",
                    Command = page > 0 ? $"UI_UILootHandler page {rate.PrefabName.Replace(" ", "+")} {page - 1}" : ""
                },
                Text =
                {
                    Text = "<", Color = page > 0 ? "1 1 1 1" : "1 1 1 0.2", Align = TextAnchor.MiddleLeft,
                    Font = "robotocondensed-regular.ttf", FontSize = 80
                }
            }, ShowRate);

            container.Add(new CuiButton
            {
                RectTransform =
                    { AnchorMin = "1 0.56", AnchorMax = "1 0.56", OffsetMin = "-125 -125", OffsetMax = "-5 0" },
                Button =
                {
                    Color = "0 0 0 0", Material = "",
                    Command = (page + 1) * 3 * SquareOnString < rate.ItemsList.Count
                        ? $"UI_UILootHandler page {rate.PrefabName.Replace(" ", "+")} {page + 1}"
                        : ""
                },
                Text =
                {
                    Text = ">",
                    Color = (page + 1) * 3 * SquareOnString < rate.ItemsList.Count ? "1 1 1 1" : "1 1 1 0.2",
                    Align = TextAnchor.MiddleRight, Font = "robotocondensed-regular.ttf", FontSize = 80
                }
            }, ShowRate);

            CuiHelper.AddUi(player, container);

            int stringNumber = 1;
            float stringAmount = Math.Min(Mathf.CeilToInt(rate.ItemsList.Count / (float)SquareOnString), 3);
            float leftPosition = stringNumber * SquareOnString / -2f * SquareSide - SquareOnString / 2f * SquareMargin;

            float topPosition = stringAmount / 2f * SquareSide + (stringAmount - 1) / 2f * 110;
            foreach (var check in rate.ItemsList.Skip((int)(page * 3 * SquareOnString)).Take((int)(3 * SquareOnString))
                         .Select((i, t) => new { A = i, B = t - page * 3 * SquareOnString }))
            {
                UI_DrawItem(player, rate, check.A, leftPosition, topPosition);

                leftPosition += SquareMargin + SquareSide;
                if ((check.B + 1) % SquareOnString == 0)
                {
                    stringNumber++;
                    leftPosition = SquareOnString / -2f * SquareSide - SquareOnString / 2f * SquareMargin;
                    topPosition -= SquareSide + 110;
                }
            }
        }

        float SquareSide = 100;
        float SquareMargin = 5;
        int SquareOnString = 12;

        private void UI_DrawItem(BasePlayer player, RateHandler.Rates rate, RateHandler.Rates.Items item, float leftPos,
            float topPos, bool firstPass = true)
        {
            CuiElementContainer container = new CuiElementContainer();

            if (firstPass)
            {
                container.Add(new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = $"0.5 0.5", AnchorMax = "0.5 0.5",
                        OffsetMin = $"{leftPos} {topPos - SquareSide}",
                        OffsetMax = $"{leftPos + SquareSide} {topPos}"
                    },
                    Image = { Color = "1 1 1 0.03" }
                }, ShowRate, ShowRate + $".{rate.ItemsList.IndexOf(item)}");
            }

            CuiHelper.DestroyUi(player, ShowRate + $".{rate.ItemsList.IndexOf(item)}.BG");
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = $"0 0", AnchorMax = "1 1", OffsetMin = $"0 0", OffsetMax = $"0 0" },
                Image = { Color = "1 1 1 0" }
            }, ShowRate + $".{rate.ItemsList.IndexOf(item)}", ShowRate + $".{rate.ItemsList.IndexOf(item)}.BG");

            if (item.IsBlueprint)
            {
                container.Add(new CuiElement
                {
                    Parent = ShowRate + $".{rate.ItemsList.IndexOf(item)}.BG",
                    Components =
                    {
                        new CuiRawImageComponent
                            { Png = (string)plugins.Find("ImageLibrary").Call("GetImage", "blueprintbase") },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                    }
                });
            }

            container.Add(new CuiElement
            {
                Parent = ShowRate + $".{rate.ItemsList.IndexOf(item)}.BG",
                Components =
                {
                    new CuiRawImageComponent { Png = (string)ImageLibrary.CallHook("GetImage", item.ShortName) },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" }
                }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0" },
                Button =
                {
                    Color = "0 0 0 0",
                    Command = $"UI_UILootHandler bp {rate.PrefabName.Replace(" ", "+")} {rate.ItemsList.IndexOf(item)}"
                },
                Text = { Text = "" }
            }, ShowRate + $".{rate.ItemsList.IndexOf(item)}.BG");

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "0 2", OffsetMax = "0 50" },
                Image = { Color = "1 1 1 0.01" },
            }, ShowRate + $".{rate.ItemsList.IndexOf(item)}.BG", ShowRate + $".{rate.ItemsList.IndexOf(item)}.Up");

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "0 -50", OffsetMax = "0 -2" },
                Image = { Color = "1 1 1 0.01" },
            }, ShowRate + $".{rate.ItemsList.IndexOf(item)}.BG", ShowRate + $".{rate.ItemsList.IndexOf(item)}.Down");

            container.Add(new CuiButton
            {
                RectTransform =
                    { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-35 -10", OffsetMax = "35 10" },
                Button =
                {
                    Color = "0 0 0 0.9", Material = "assets/content/ui/uibackgroundblur.mat",
                    Command =
                        $"UI_UILootHandler remove {rate.PrefabName.Replace(" ", "+")} {rate.ItemsList.IndexOf(item)}"
                },
                Text =
                {
                    Text = "УДАЛИТЬ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                    FontSize = 14
                }
            }, ShowRate + $".{rate.ItemsList.IndexOf(item)}.BG");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 25", OffsetMax = "0 0" },
                Text =
                {
                    Text = "МАКСИМУМ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                    Color = "1 1 1 0.4"
                }
            }, ShowRate + $".{rate.ItemsList.IndexOf(item)}.Up");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 -25" },
                Text =
                {
                    Text = "МИНИМУМ", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                    Color = "1 1 1 0.4"
                }
            }, ShowRate + $".{rate.ItemsList.IndexOf(item)}.Down");


            CuiHelper.DestroyUi(player, ShowRate + $".{rate.ItemsList.IndexOf(item)}.Down.Min");
            container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "0 1", OffsetMin = "0 -25", OffsetMax = "25 0" },
                    Button =
                    {
                        Color = $"0.801007 0.4 0.4 {(item.Amounts.Min - 1 >= 0 ? 0.6 : 0.1)}",
                        Command =
                            $"UI_UILootHandler min {rate.PrefabName.Replace(" ", "+")} -1 {rate.ItemsList.IndexOf(item)}"
                    },
                    Text =
                    {
                        Text = "-", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 20
                    }
                }, ShowRate + $".{rate.ItemsList.IndexOf(item)}.Down",
                ShowRate + $".{rate.ItemsList.IndexOf(item)}.Down.Min");

            CuiHelper.DestroyUi(player, ShowRate + $".{rate.ItemsList.IndexOf(item)}.Down.Max");
            container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = "-25 -25", OffsetMax = "0 0" },
                    Button =
                    {
                        Color = $"0.4 0.8 0.4 {(item.Amounts.Min + 1 <= item.Amounts.Max ? 0.6 : 0.1)}",
                        Command =
                            $"UI_UILootHandler min {rate.PrefabName.Replace(" ", "+")} 1 {rate.ItemsList.IndexOf(item)}"
                    },
                    Text =
                    {
                        Text = "+", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 20
                    }
                }, ShowRate + $".{rate.ItemsList.IndexOf(item)}.Down",
                ShowRate + $".{rate.ItemsList.IndexOf(item)}.Down.Max");

            CuiHelper.DestroyUi(player, ShowRate + $".{rate.ItemsList.IndexOf(item)}.Up.Min");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 0", OffsetMax = "25 25" },
                Button =
                {
                    Color =
                        $"0.8 0.4 0.4 {(item.Amounts.Max - 1 >= item.Amounts.Min && item.Amounts.Max - 1 >= 0 ? 0.6 : 0.1)}",
                    Command =
                        $"UI_UILootHandler max {rate.PrefabName.Replace(" ", "+")} -1 {rate.ItemsList.IndexOf(item)}"
                },
                Text = { Text = "-", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 20 }
            }, ShowRate + $".{rate.ItemsList.IndexOf(item)}.Up", ShowRate + $".{rate.ItemsList.IndexOf(item)}.Up.Min");

            CuiHelper.DestroyUi(player, ShowRate + $".{rate.ItemsList.IndexOf(item)}.Up.Max");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = "-25 0", OffsetMax = "0 25" },
                Button =
                {
                    Color = "0.4054100787 0.8010074584 0.401007879 0.6",
                    Command =
                        $"UI_UILootHandler max {rate.PrefabName.Replace(" ", "+")} 1 {rate.ItemsList.IndexOf(item)}"
                },
                Text =
                {
                    Text = "+", Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf", FontSize = 20
                }
            }, ShowRate + $".{rate.ItemsList.IndexOf(item)}.Up", ShowRate + $".{rate.ItemsList.IndexOf(item)}.Up.Max");

            CuiHelper.DestroyUi(player, ShowRate + $".{rate.ItemsList.IndexOf(item)}.Down.Minimum");
            container.Add(new CuiElement
            {
                Parent = ShowRate + $".{rate.ItemsList.IndexOf(item)}.Down",
                Name = ShowRate + $".{rate.ItemsList.IndexOf(item)}.Down.Minimum.Input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                        Command =
                            $"UI_UILootHandler minSet {rate.PrefabName.Replace(" ", "+")} {rate.ItemsList.IndexOf(item)} "
                    },
                    new CuiRectTransformComponent
                        { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "26 -25", OffsetMax = "-26 0" }
                }
            });
            container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = "26 -25", OffsetMax = "-26 0" },
                    Button =
                        { Color = "0 0 0 0.2", Close = ShowRate + $".{rate.ItemsList.IndexOf(item)}.Down.Minimum" },
                    Text =
                    {
                        Text = $"{item.Amounts.Min}", Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 14
                    }
                }, ShowRate + $".{rate.ItemsList.IndexOf(item)}.Down",
                ShowRate + $".{rate.ItemsList.IndexOf(item)}.Down.Minimum");

            CuiHelper.DestroyUi(player, ShowRate + $".{rate.ItemsList.IndexOf(item)}.Up.Maximum");
            container.Add(new CuiElement
            {
                Parent = ShowRate + $".{rate.ItemsList.IndexOf(item)}.Up",
                Name = ShowRate + $".{rate.ItemsList.IndexOf(item)}.Up.Maximum.Input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, Font = "robotocondensed-regular.ttf",
                        Command =
                            $"UI_UILootHandler maxSet {rate.PrefabName.Replace(" ", "+")} {rate.ItemsList.IndexOf(item)} "
                    },
                    new CuiRectTransformComponent
                        { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "26 0", OffsetMax = "-26 25" }
                }
            });
            container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "26 0", OffsetMax = "-26 25" },
                    Button = { Color = "0 0 0 0.2", Close = ShowRate + $".{rate.ItemsList.IndexOf(item)}.Up.Maximum" },
                    Text =
                    {
                        Text = $"{item.Amounts.Max}", Align = TextAnchor.MiddleCenter,
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 14
                    }
                }, ShowRate + $".{rate.ItemsList.IndexOf(item)}.Up",
                ShowRate + $".{rate.ItemsList.IndexOf(item)}.Up.Maximum");

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Commands

        [ConsoleCommand("UI_UILootHandler")]
        private void CmdConsoleHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (!args.HasArgs(1) || !CanConfigure(player)) return;

            switch (args.Args[0].ToLower())
            {
                case "to":
                {
                    string prefabName = args.Args[1].Replace("+", " ");
                    var rate = Handler.GetRates(prefabName);
                    if (rate == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    player.inventory.Strip();
                    NextTick(() =>
                    {
                        foreach (var check in rate.ItemsList)
                        {
                            Item create = check.ToItem();
                            if (!player.inventory.GiveItem(create))
                                create.Drop(player.transform.position, Vector3.down);
                        }

                        CuiHelper.DestroyUi(player, MainLayer);
                        player.ChatMessage($"Все предметы успешно скопированы вам в инвентарь!");
                    });
                    break;
                }
                case "populate":
                {
                    string prefabName = args.Args[1].Replace("+", " ");
                    var rate = Handler.GetRates(prefabName);
                    if (rate == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    BaseNetworkable.serverEntities.Where(p => p.PrefabName == prefabName).ToList()
                        .ForEach(p => PopulateLoot((BaseEntity)p));
                    SaveData();
                    break;
                }
                case "from":
                {
                    string prefabName = args.Args[1].Replace("+", " ");
                    var rate = Handler.GetRates(prefabName);
                    if (rate == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    rate.DropAmount = 1;
                    if (!args.HasArgs(3) || args.Args[2] != "add") rate.ItemsList.Clear();

                    foreach (var check in player.inventory.containerMain.itemList)
                    {
                        if (!rate.ItemsList.Any(p =>
                                p.ShortName == check.info.shortname && p.SkinID == check.skin &&
                                p.IsBlueprint == check.IsBlueprint()))
                            rate.ItemsList.Add(RateHandler.Rates.Items.FromItem(check));
                    }

                    UI_DrawRate(player, rate, 0);
                    break;
                }
                /*case "create":
                {
                    string prefabName = args.Args[1];
                    
                    var rate = Handler.CreateRates(prefabName);
                    if (rate == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    UI_DrawRate(player, rate);
                    break;
                }*/
                case "maxset":
                {
                    string prefabName = args.Args[1].Replace("+", " ");
                    var rate = Handler.GetRates(prefabName);
                    if (rate == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    var item = rate.ItemsList.ElementAt(int.Parse(args.Args[2]));
                    if (item == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    int amount = -1;
                    if (!int.TryParse(args.Args[3], out amount))
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    item.Amounts.Max = amount;
                    UI_DrawItem(player, rate, item, 0, 0, false);
                    break;
                }
                case "minset":
                {
                    string prefabName = args.Args[1].Replace("+", " ");
                    var rate = Handler.GetRates(prefabName);
                    if (rate == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    var item = rate.ItemsList.ElementAt(int.Parse(args.Args[2]));
                    if (item == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    int amount = -1;
                    if (!int.TryParse(args.Args[3], out amount))
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    item.Amounts.Min = amount;
                    UI_DrawItem(player, rate, item, 0, 0, false);
                    break;
                }
                case "min":
                {
                    string prefabName = args.Args[1].Replace("+", " ");
                    var rate = Handler.GetRates(prefabName);
                    if (rate == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    int amount = -1;
                    if (!int.TryParse(args.Args[2], out amount))
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    var item = rate.ItemsList.ElementAt(int.Parse(args.Args[3]));
                    if (item == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    if (item.Amounts.Min + amount > item.Amounts.Max) return;
                    if (item.Amounts.Min + amount < 0) return;

                    item.Amounts.Min += amount;

                    UI_DrawItem(player, rate, item, 0, 0, false);
                    break;
                }
                case "page":
                {
                    string prefabName = args.Args[1].Replace("+", " ");
                    var rate = Handler.GetRates(prefabName);
                    if (rate == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    UI_DrawRate(player, rate, int.Parse(args.Args[2]));
                    break;
                }
                case "recover":
                {
                    string prefabName = args.Args[1].Replace("+", " ");
                    var rate = Handler.GetRates(prefabName);
                    if (rate == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    Handler.Rateses.Remove(rate);
                    SaveData();

                    CuiHelper.DestroyUi(player, MainLayer);
                    break;
                }
                case "amount":
                {
                    string prefabName = args.Args[1].Replace("+", " ");
                    var rate = Handler.GetRates(prefabName);
                    if (rate == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    if (rate.DropAmount + int.Parse(args.Args[2]) > rate.ItemsList.Count) return;
                    if (rate.DropAmount + int.Parse(args.Args[2]) < 1) return;

                    rate.DropAmount += int.Parse(args.Args[2]);

                    CuiHelper.DestroyUi(player, ShowRate + ".Amount");
                    CuiElementContainer container = new CuiElementContainer();
                    container.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0.6 1", AnchorMax = "0.6 1", OffsetMin = "-175 -75", OffsetMax = "-150 -10"
                        },
                        Button = { Color = "0 0 0 0", Command = $"UI_UILootHandler" },
                        Text =
                        {
                            Text = (rate.DropAmount).ToString(), Align = TextAnchor.UpperCenter,
                            Font = "robotocondensed-regular.ttf", FontSize = 24
                        }
                    }, ShowRate, ShowRate + ".Amount");

                    CuiHelper.AddUi(player, container);

                    break;
                }
                case "bp":
                {
                    string prefabName = args.Args[1].Replace("+", " ");
                    var rate = Handler.GetRates(prefabName);
                    if (rate == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    var item = rate.ItemsList.ElementAt(int.Parse(args.Args[2]));
                    if (item == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    item.IsBlueprint = !item.IsBlueprint;
                    UI_DrawItem(player, rate, item, 0, 0, false);
                    break;
                }
                case "remove":
                {
                    string prefabName = args.Args[1].Replace("+", " ");
                    var rate = Handler.GetRates(prefabName);
                    if (rate == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    var item = rate.ItemsList.ElementAt(int.Parse(args.Args[2]));
                    if (item == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    rate.ItemsList.Remove(item);
                    UI_DrawRate(player, rate, 0);
                    break;
                }
                case "max":
                {
                    string prefabName = args.Args[1].Replace("+", " ");
                    var rate = Handler.GetRates(prefabName);
                    if (rate == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    int amount = -1;
                    if (!int.TryParse(args.Args[2], out amount))
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    var item = rate.ItemsList.ElementAt(int.Parse(args.Args[3]));
                    if (item == null)
                    {
                        CuiHelper.DestroyUi(player, MainLayer);
                        return;
                    }

                    if (item.Amounts.Max + amount < item.Amounts.Min) return;
                    if (item.Amounts.Max + amount < 0) return;

                    item.Amounts.Max += amount;
                    UI_DrawItem(player, rate, item, 0, 0, false);
                    break;
                }
            }
        }

        #endregion

        #region Functions

        private void PopulateLoot(BaseEntity entity)
        {
            var rate = Handler.GetRates(entity);
            if (rate == null) return;

            var obj = entity.GetComponent<LootContainer>();
            if (obj == null) return;

            obj.inventory.itemList.Clear();
            obj.inventory.Clear();
            if (entity.PrefabName.Contains("barrel")) return;

            obj.inventory.capacity = rate.DropAmount + 1;
            obj.inventory.MarkDirty();

            NextTick(() =>
            {
                var listItems = GetRandom(rate);

                foreach (var check in listItems)
                {
                    Item item;
                    var amount = check.Amounts.GenerateAmount();
                    if (amount == 0) continue;
                    if (check.IsBlueprint)
                    {
                        item = ItemManager.CreateByItemID(-996920608);

                        var info = ItemManager.FindItemDefinition(check.ShortName);
                        item.blueprintTarget = info.itemid;
                    }
                    else
                    {
                        item = ItemManager.CreateByPartialName(check.ShortName, amount);
                        item.skin = check.SkinID;
                    }

                    item.MoveToContainer(obj.inventory);
                }

                if (rate.ScrapSettings.EnableDrop)
                {
                    rate.ScrapSettings.CreateScrap().MoveToContainer(obj.inventory);
                }
            });
        }

        private List<RateHandler.Rates.Items> GetRandom(RateHandler.Rates rate)
        {
            var result = new List<RateHandler.Rates.Items>();

            for (int i = 0; i < rate.DropAmount; i++)
            {
                var totalChance = rate.ItemsList.Sum(p => p.DropChance);
                var dropChance = Core.Random.Range(0, totalChance);

                int curChance = 0;
                foreach (var check in rate.ItemsList)
                {
                    if (check.DropChance + curChance > dropChance)
                    {
                        result.Add(check);
                        break;
                    }

                    curChance += check.DropChance;
                }
            }

            return result;
        }


        // Reserved4 -> Typing
        private void CreateInput(BasePlayer player)
        {
            if (player.HasFlag(BaseEntity.Flags.Reserved4)) return;

            // TODO: Create Input
            ServerMgr.Instance.StartCoroutine(PreparePlayer(player));
        }

        private object OnPlayerSleepEnded(BasePlayer player)
        {
            if (player.HasFlag(BaseEntity.Flags.Reserved4)) return false;

            return null;
        }

        private IEnumerator PreparePlayer(BasePlayer player)
        {
            player.SetFlag(BaseEntity.Flags.Reserved4, true);
            player.StartSleeping();

            yield return new WaitWhile(() => player.HasFlag(BaseEntity.Flags.Reserved4) || !player.IsConnected);

            player.SetFlag(BaseEntity.Flags.Reserved4, false);
            player.EndSleeping();
        }

        private void DropLoot(BaseEntity entity)
        {
            var rate = Handler.GetRates(entity);
            if (rate == null) return;

            entity.GetComponent<LootContainer>().inventory.Clear();

            var listItems = GetRandom(rate);

            foreach (var check in listItems)
            {
                check.ToItem().Drop(entity.transform.position + new Vector3(0, 1f, 0), Vector3.down);
            }

            if (rate.ScrapSettings.EnableDrop)
            {
                rate.ScrapSettings.CreateScrap().Drop(entity.transform.position + new Vector3(0, 1f, 0), Vector3.down);
            }
        }

        private bool CanConfigure(BasePlayer player) =>
            player.IsAdmin || permission.UserHasPermission(player.UserIDString, AdminPermission);

        #endregion
    }
}