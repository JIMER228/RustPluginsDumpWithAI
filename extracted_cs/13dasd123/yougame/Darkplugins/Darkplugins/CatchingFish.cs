using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("CatchingFish", "https://discord.gg/dNGbxafuJn", "1.0.2")]
    class CatchingFish : RustPlugin
    {
        #region Reference
        Plugin ImageLibrary => Interface.Oxide.RootPluginManager.GetPlugin("ImageLibrary");
        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        #endregion

        #region Vars

        List<string> EffectDropList = new List<string> { "assets/prefabs/weapons/arms/effects/uppercut.prefab", "assets/prefabs/weapons/arms/effects/shove.prefab", "assets/prefabs/weapons/arms/effects/jab-3.prefab", "assets/prefabs/weapons/arms/effects/hook-1.prefab", "assets/prefabs/weapons/arms/effects/hook-2.prefab" };
        string ShortnameBait = "sticks";
        string ShortnameRod = "fishingrod.handmade";
        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        public List<ulong> PlayerCooldownFish = new List<ulong>();
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

        #region FishType

        public enum BaitType
        {
            Rod,
            Normal,
            Rare,
            Epic,
            Legendary
        }

        #endregion

        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Настройка | Settings")]
            public Dictionary<string, FishSetting> FSetting = new Dictionary<string, FishSetting>();
            [JsonProperty("Настройка рыбы | Settings fish")]
            public List<FishCompleted> FishAction = new List<FishCompleted>();
            [JsonProperty("Настройка наживки [Наживка : Время клева] | Setting the bait [Bait : bite times]")]
            public Dictionary<BaitType, int> TimeDependingBait = new Dictionary<BaitType, int>();
            [JsonProperty("Ящики в которых будут спавнится наживки и их шанс (Оставьте пустым,если не нужен спавн) | Boxes in which will spawn bait & rare (Leave blank if you do not need spawn)")]
            public Dictionary<string, int> SpawnCratesList = new Dictionary<string, int>();
            [JsonProperty("Шанс успешного улова | The chance of a successful catch")]
            public int PercentBaitFish;

            internal class FishSetting
            {
                public string DisplayName;
                public string Description;
                public ulong SkinID;
                public string ImageURL;
                public BaitType BaitTypes;
                public Dictionary<string, int> CraftingItem = new Dictionary<string, int>();
            }

            internal class FishCompleted
            {
                public string Shortname;
                public string DisplayName;
                public ulong SkinID;

                internal class ItemClass
                {
                    public string DisplayName;
                    public string Shortname;
                    public ulong SkinID;
                    public int Amount;
                }

                public List<ItemClass> ItemDrop = new List<ItemClass>();
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    PercentBaitFish = 100,
                    FSetting = new Dictionary<string, FishSetting>
                    {
                        #region FSettings
                        ["fishRod"] = new FishSetting
                        {
                            DisplayName = "Удочка",
                            Description = "С помощью удочки ты можешь ловить рыбу! Не забудь про наживку",
                            SkinID = 1790334074,
                            ImageURL = "https://i.imgur.com/PTfT8sR.png",
                            BaitTypes = BaitType.Rod,
                            CraftingItem = new Dictionary<string, int>
                            {
                                ["wood"] = 2500,
                                ["rope"] = 10,
                            }
                        },
                        ["defaultRare"] = new FishSetting
                        {
                            DisplayName = "Обычная наживка",
                            Description = "Чем лучше наживка,тем лучше поклев!",
                            SkinID = 1790369244,
                            ImageURL = "https://i.imgur.com/mtA0RXO.png",
                            BaitTypes = BaitType.Normal,
                            CraftingItem = new Dictionary<string, int>
                            {
                                ["metal.fragments"] = 1000,
                                ["metalspring"] = 5,
                            }
                        },
                        ["normalRare"] = new FishSetting
                        {
                            DisplayName = "Необычная наживка",
                            Description = "Чем лучше наживка,тем лучше поклев!",
                            SkinID = 1790369647,
                            ImageURL = "https://i.imgur.com/wVi7NyH.png",
                            BaitTypes = BaitType.Rare,
                            CraftingItem = new Dictionary<string, int>
                            {
                                ["can.tuna.empty"] = 3,
                            }
                        },
                        ["epicRare"] = new FishSetting
                        {
                            DisplayName = "Эпическая наживка",
                            Description = "Чем лучше наживка,тем лучше поклев!",
                            SkinID = 1790369932,
                            ImageURL = "https://i.imgur.com/GK38o50.png",
                            BaitTypes = BaitType.Epic,
                            CraftingItem = new Dictionary<string, int>
                            {
                                ["fish.raw"] = 25,
                            }
                        },
                        ["legendaryRare"] = new FishSetting
                        {
                            DisplayName = "Легендарная наживка",
                            Description = "Чем лучше наживка,тем лучше поклев!",
                            SkinID = 1790370244,
                            ImageURL = "https://i.imgur.com/O74gtdb.png",
                            BaitTypes = BaitType.Legendary,
                            CraftingItem = new Dictionary<string, int>
                            {
                                ["metal.fragments"] = 3000,
                                ["rope"] = 10,
                                ["sewingkit"] = 15,
                            }
                        },
                        #endregion
                    },
                    FishAction = new List<FishCompleted>
                    {
                        #region FishType
                        new FishCompleted
                        {
                            Shortname = "fish.troutsmall",
                            DisplayName = "Карась",
                            SkinID = 1790549747,
                            ItemDrop = new List<FishCompleted.ItemClass>
                            {
                                new FishCompleted.ItemClass
                                {
                                    DisplayName = "",
                                    Shortname = "wood",
                                    Amount = 5000,
                                    SkinID = 0,
                                },
                                new FishCompleted.ItemClass
                                {
                                    DisplayName = "",
                                    Shortname = "stones",
                                    Amount = 5000,
                                    SkinID = 0,
                                },
                            }
                        },
                        new FishCompleted
                        {
                            Shortname = "fish.troutsmall",
                            DisplayName = "Окунь",
                            SkinID = 1790550997,
                            ItemDrop = new List<FishCompleted.ItemClass>
                            {
                                new FishCompleted.ItemClass
                                {
                                    DisplayName = "Карась",
                                    Shortname = "fish.troutsmall",
                                    Amount = 1,
                                    SkinID = 1790549747,
                                },
                                new FishCompleted.ItemClass
                                {
                                    DisplayName = "",
                                    Shortname = "stones",
                                    Amount = 5000,
                                    SkinID = 0,
                                },
                            }
                        },
                        new FishCompleted
                        {
                            Shortname = "fish.troutsmall",
                            DisplayName = "Щука",
                            SkinID = 1790551620,
                            ItemDrop = new List<FishCompleted.ItemClass>
                            {
                                new FishCompleted.ItemClass
                                {
                                    DisplayName = "Карась",
                                    Shortname = "fish.troutsmall",
                                    Amount = 1,
                                    SkinID = 1790549747,
                                },
                                new FishCompleted.ItemClass
                                {
                                    DisplayName = "",
                                    Shortname = "stones",
                                    Amount = 5000,
                                    SkinID = 0,
                                },
                            }
                        },
                        new FishCompleted
                        {
                            Shortname = "fish.troutsmall",
                            DisplayName = "Акула",
                            SkinID = 1790553037,
                            ItemDrop = new List<FishCompleted.ItemClass>
                            {
                                new FishCompleted.ItemClass
                                {
                                    DisplayName = "Карась",
                                    Shortname = "fish.troutsmall",
                                    Amount = 1,
                                    SkinID = 1790549747,
                                },
                                new FishCompleted.ItemClass
                                {
                                    DisplayName = "",
                                    Shortname = "stones",
                                    Amount = 5000,
                                    SkinID = 0,
                                },
                            }
                        },
                        #endregion
                    },
                    TimeDependingBait = new Dictionary<BaitType, int>
                    {
                        #region TimeBait
                        [BaitType.Normal] = 20,
                        [BaitType.Rare] = 15,
                        [BaitType.Epic] = 10,
                        [BaitType.Legendary] = 5,
                        #endregion
                    },
                    SpawnCratesList = new Dictionary<string, int>
                    {
                        #region CrateList
                        ["crate_basic"] = 10,
                        ["crate_normal"] = 15,
                        ["crate_mine"] = 30,
                        ["crate_tools"] = 60,
                        ["crate_underwater_basic"] = 70,
                        ["crate_elite"] = 80,
                        ["crate_underwater_advanced"] = 100,
                        #endregion
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning($"Ошибка чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Metods

        #region Init
        void RegisteredPlugin()
        {
            for (int i = 0; i < config.FSetting.Count; i++)
                AddImage(config.FSetting.ElementAt(i).Value.ImageURL, config.FSetting.ElementAt(i).Key);
        }
        #endregion

        #region ItemCraft

        private void TakeItemForCraft(BasePlayer player, string index)
        {
            var ICFG = config.FSetting[index].CraftingItem;

            for (int i = 0; i < ICFG.Count; i++)
                player.inventory.Take(null, ItemManager.FindItemDefinition(ICFG.ElementAt(i).Key).itemid, ICFG.ElementAt(i).Value);
        }


        #endregion

        #region FishMetods

        void FishRodDrop(BasePlayer player) // Тута пиздец,ухади если зашел.Даже не смотри,фуфуфу
        {
            if (PlayerCooldownFish.Contains(player.userID))
            {
                MessageRust(player, lang.GetMessage("YOU_USE_ROD", this));
                return;
            }

            var item = player.inventory.containerMain.FindItemsByItemName(ShortnameBait);
            if (item != null && item.skin == config.FSetting["defaultRare"].SkinID)
            {
                FishDropMetods(player, item, "defaultRare");
                return;
            }
            if (item != null && item.skin == config.FSetting["normalRare"].SkinID)
            {
                FishDropMetods(player, item, "normalRare");
                return;
            }
            if (item != null && item.skin == config.FSetting["epicRare"].SkinID)
            {
                FishDropMetods(player, item, "epicRare");
                return;
            }
            if (item != null && item.skin == config.FSetting["legendaryRare"].SkinID)
            {
                FishDropMetods(player, item, "legendaryRare");
                return;
            }
            MessageRust(player, lang.GetMessage("NO_BAIT_FISH_ROD", this));
        }

        void FishDropMetods(BasePlayer player, Item item, string Index)
        {
            var Time = config.TimeDependingBait[config.FSetting[Index].BaitTypes];
            var DisplayName = config.FSetting[Index].DisplayName;
            player.inventory.Take(null, item.info.itemid, 1);
            Effect.server.Run(EffectDropList[UnityEngine.Random.Range(0, EffectDropList.Count)], player.transform.localPosition);

            MessageRust(player, String.Format(lang.GetMessage("RUN_BAIT_ROD_DROP", this), Time, DisplayName));
            PlayerCooldownFish.Add(player.userID);

            double TimerInfo = Time + CurrentTime();
            TimeFish(player, Convert.ToInt32(TimerInfo));
            timer.Once(Time, () =>
            {
                if (PlayerCooldownFish.Contains(player.userID))
                    PlayerCooldownFish.Remove(player.userID);
                FishBiteCompleted(player);

            });
        }

        void TimeFish(BasePlayer player, int Time)
        {
            int TimerInfo = (int)(Time - CurrentTime());
            if (TimerInfo > 1)
            {
                timer.Once(1f, () =>
                {
                    rust.RunClientCommand(player, $"gametip.showgametip", String.Format(lang.GetMessage("TIMER_TICK", this), TimerInfo));
                    TimeFish(player, Time);
                });
            }
        }

        void FishBiteCompleted(BasePlayer player)
        {
            MessageRust(player, lang.GetMessage("FINISH_ROD_FISH", this));
            if (Oxide.Core.Random.Range(0, 100) >= (100 - config.PercentBaitFish))
            {
                int RandomIndexFish = Oxide.Core.Random.Range(0, config.FishAction.Count);
                var Fish = config.FishAction[RandomIndexFish];
                var item = ItemManager.CreateByName(Fish.Shortname, 1, Fish.SkinID);
                if (item == null) return;
                item.name = Fish.DisplayName;

                player.GiveItem(item);
                MessageRust(player, String.Format(lang.GetMessage("FISH_COMPLETE_FISHDROP", this), Fish.DisplayName));
            }
            Effect.server.Run(EffectDropList[UnityEngine.Random.Range(0, EffectDropList.Count)], player.transform.localPosition);
        }

        private static void ItemRemovalThink(Item item, BasePlayer player, int itemsToTake)
        {
            if (item.amount == itemsToTake)
            {
                item.RemoveFromContainer();
                item.Remove();
            }
            else
            {
                item.amount -= itemsToTake;
                item.GetRootContainer().MarkDirty();
            }
        }

        bool UseFishRod(BasePlayer player)
        {
            RaycastHit hit;
            Ray ray = new Ray(player.eyes.position, player.eyes.HeadForward());
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, 8f, LayerMask.GetMask("Water"))) return true;
            else
            {
                MessageRust(player, lang.GetMessage("NO_WATER_DROP_ROD", this));
                return false;
            }
        }

        #endregion

        #endregion

        #region Hooks
        void OnServerInitialized() => RegisteredPlugin();

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (item == null || action == null || action == "")
                return null;
            if (player == null || item.info.shortname != "fish.troutsmall" || action != "Gut" || config.FishAction.Find(p => p.SkinID == item.skin) == null)
                return null;
            var CFG = config.FishAction.Where(x => x.SkinID == item.skin).FirstOrDefault().ItemDrop;
            int RandomItem = UnityEngine.Random.Range(0, CFG.Count);
            if (CFG[RandomItem] == null) return null;
            Item itemS = ItemManager.CreateByName(CFG[RandomItem].Shortname, CFG[RandomItem].Amount, CFG[RandomItem].SkinID);
            if (itemS == null)
            {
                PrintError($"ITEM IS NULL {CFG[RandomItem].Shortname}");
                return null;
            }
            if (!string.IsNullOrEmpty(CFG[RandomItem].DisplayName))
                itemS.name = CFG[RandomItem].DisplayName;

            player.GiveItem(itemS, BaseEntity.GiveItemReason.Generic);

            ItemRemovalThink(item, player, 1);
            return false;

        }
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (PlayerCooldownFish.Contains(player.userID))
                PlayerCooldownFish.Remove(player.userID);
        }


        #region SpawnBaitCrates
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (config.SpawnCratesList != null && config.SpawnCratesList.Count >= 1)
            {
                if (entity.GetComponent<LootContainer>() == null) return;
                var item = (Item)CreateItem(entity.ShortPrefabName);
                item?.MoveToContainer(entity.GetComponent<LootContainer>().inventory);
            }
        }

        private Item CreateItem(string Index)
        {
            if (config.SpawnCratesList.ContainsKey(Index))
            {
                bool goodChance = UnityEngine.Random.Range(0, 100) >= (100 - config.SpawnCratesList[Index]);
                if (goodChance)
                {
                    int RandomItem = UnityEngine.Random.Range(0, config.FSetting.Count);
                    if (config.FSetting.ElementAt(RandomItem).Value.BaitTypes != BaitType.Rod)
                    {
                        Item itemS = ItemManager.CreateByName(ShortnameBait, 1, config.FSetting.ElementAt(RandomItem).Value.SkinID);
                        itemS.name = config.FSetting.ElementAt(RandomItem).Value.DisplayName;
                        return itemS;
                    }
                }
            }
            return null;
        }

        #endregion

        #region FishRodDropHooks

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.WasJustPressed(BUTTON.FIRE_PRIMARY) && player.GetActiveItem() != null && player.GetActiveItem().info.shortname == "fishingrod.handmade" && player.GetActiveItem().skin != 0 && UseFishRod(player))
                FishRodDrop(player);
        }

        #endregion

        #region ItemHooks
        private Item OnItemSplit(Item item, int amount)
        {
            for (int i = 0; i < config.FSetting.Count; i++)
            {
                var cfg = config.FSetting.ElementAt(i).Value;
                if (item.skin == cfg.SkinID)
                {
                    Item x = ItemManager.CreateByPartialName(config.FSetting.ElementAt(i).Key, amount);
                    x.name = cfg.DisplayName;
                    x.skin = cfg.SkinID;
                    x.amount = amount;
                    item.amount -= amount;
                    return x;
                }
            }
            return null;
        }

        object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (item.GetItem().skin != targetItem.GetItem().skin) return false;

            return null;
        }

        object CanStackItem(Item item, Item targetItem)
        {
            if (item.skin != targetItem.skin) return false;

            return null;
        }
        #endregion

        #endregion

        #region Command
        [ChatCommand("cf")]
        void CF_ChatCommand(BasePlayer player)
        {
            CraftFishingRod(player);
        }

        [ConsoleCommand("cf")]
        void CF_Command(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            switch (arg.Args[0])
            {
                case "open_craft":
                    {
                        string index = arg.Args[1];
                        CraftOpenItemsPanel(player, index);
                        break;
                    }
                case "craft_item":
                    {
                        string index = arg.Args[1];
                        var cfg = config.FSetting[index];

                        for (int i = 0; i < cfg.CraftingItem.Count; i++)
                            if (CheckResourceCraft(player, cfg.CraftingItem.ElementAt(i).Key, cfg.CraftingItem.ElementAt(i).Value))
                            {
                                MessageRust(player, lang.GetMessage("UI_NO_RESOURCE", this));
                                Effect.server.Run("assets/prefabs/npc/autoturret/effects/targetlost.prefab", player.transform.localPosition);
                                CuiHelper.DestroyUi(player, PARENT_CraftFishing);
                                return;
                            }

                        TakeItemForCraft(player, index);
                        CuiHelper.DestroyUi(player, PARENT_CraftFishing);
                        Effect.server.Run("assets/prefabs/deployable/tier 1 workbench/effects/experiment-start.prefab", player.transform.localPosition);
                        string Shortname = cfg.BaitTypes == BaitType.Rod ? ShortnameRod : ShortnameBait;
                        var item = ItemManager.CreateByName(Shortname, 1, cfg.SkinID);
                        if (item == null) return;
                        item.name = cfg.DisplayName;
                        player.GiveItem(item);
                        break;
                    }
                case "testcmd":
                    {
                        if (player == null)
                        {
                            PrintWarning("Используйте команду в игре!");
                            return;
                        }
                        if (!player.IsAdmin) return;
                        for (int i = 0; i < config.FSetting.Count; i++)
                        {
                            var cfg = config.FSetting.ElementAt(i).Value;
                            string Shortname = cfg.BaitTypes == BaitType.Rod ? ShortnameRod : ShortnameBait;
                            var item = ItemManager.CreateByName(Shortname, 1, cfg.SkinID);
                            if (item == null) return;
                            item.name = cfg.DisplayName;
                            player.GiveItem(item);
                        }

                        break;
                    }
            }
        }

        #endregion

        #region UI

        #region Parent
        static string PARENT_CraftFishing = "PARENT_CF";
        static string PARENT_CraftFishingInfo = "PARENT_CF_INFO";
        #endregion

        #region CraftMenu

        public void CraftFishingRod(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, PARENT_CraftFishing);
            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = HexToRustFormat("#0000002D"), Material = "assets/content/ui/uibackgroundblur.mat" }
            }, "Overlay", PARENT_CraftFishing);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
                Button = { Close = PARENT_CraftFishing, Color = "0 0 0 0" },
                Text = { FadeIn = 0.8f, Text = "" }
            }, PARENT_CraftFishing);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9185187", AnchorMax = "1 0.9675928" },
                Text = { Text = lang.GetMessage("UI_TITLE_MENU", this), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFF89") }
            }, PARENT_CraftFishing);

            #region LoadItemsUI

            #region SettingsCenter
            int ItemCount = 0;
            float itemMinPosition = 219f;
            float itemWidth = 0.7574074f - 0.5574074f;
            float itemMargin = 0.419895f - 0.393646f;
            int itemCount = config.FSetting.Count;
            float itemMinHeight = 0.755741f;
            float itemHeight = 0.5963541f - 0.4637621f;

            if (itemCount > 4)
            {
                itemMinPosition = 0.5f - 4 / 2f * itemWidth - (4 - 1) / 2f * itemMargin;
                itemCount -= 4;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;

            #endregion

            for (int i = 0; i < config.FSetting.Count; i++)
            {
                var cfg = config.FSetting.ElementAt(i).Value;

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{itemMinPosition} {itemMinHeight}", AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}" },
                    Image = { Color = HexToRustFormat("#00000051"), Material = "assets/content/ui/uibackgroundblur.mat" }
                }, PARENT_CraftFishing, $"ITEM_{i}");

                #region PanelItem

                #region InfoLabel

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.3563831 0.2816907", AnchorMax = $"0.9813833 0.9366211" },
                    Image = { Color = HexToRustFormat("#8282827D"), Material = "assets/content/ui/uibackgroundblur.mat" }
                }, $"ITEM_{i}", $"ITEM_TITLE_{i}");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.6300013", AnchorMax = "1 1" },
                    Text = { Text = cfg.DisplayName, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFFF") }
                }, $"ITEM_TITLE_{i}");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0.01999947", AnchorMax = "1 0.7183107" },
                    Text = { Text = cfg.Description, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFFF") }
                }, $"ITEM_TITLE_{i}");

                #endregion

                #region Images

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.01861692 0.06338032", AnchorMax = $"0.3404256 0.936621" },
                    Image = { Color = HexToRustFormat("#8C8C8C8D"), Material = "assets/content/ui/uibackgroundblur.mat" }
                }, $"ITEM_{i}", $"IMAGE_ITEM_{i}");

                container.Add(new CuiElement
                {
                    Parent = $"IMAGE_ITEM_{i}",
                    Components =
                        {
                        new CuiRawImageComponent { Png = GetImage(config.FSetting.ElementAt(i).Key),  Color = HexToRustFormat("#FFFFFFFF") },
                        new CuiRectTransformComponent{  AnchorMin = $"0.09090894 0.09600019", AnchorMax = $"0.9173549 0.8959998" },
                        }
                });

                #endregion

                #endregion

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.3563831 0.06338029", AnchorMax = "0.9813833 0.2676061" },
                    Button = { Command = $"cf open_craft {config.FSetting.ElementAt(i).Key}", Color = HexToRustFormat("#0F93AB9E"), Material = "assets/content/ui/uibackgroundblur.mat" },
                    Text = { Text = lang.GetMessage("UI_INTERFACE_BTN", this), Align = TextAnchor.MiddleCenter }
                }, $"ITEM_{i}");

                #region SettingsCenter

                ItemCount++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % 4 == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 2f));

                    if (itemCount > 4)
                    {
                        itemMinPosition = 0.5f - 4 / 2f * itemWidth - (4 - 1) / 2f * itemMargin;
                        itemCount -= 4;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }

                #endregion
            }
            #endregion

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region ItemCraft
        public void CraftOpenItemsPanel(BasePlayer player, string index)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, PARENT_CraftFishingInfo);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.1953125 0.01944444", AnchorMax = "0.8348958 0.4185185" },
                Image = { Color = "0 0 0 0" }
            }, PARENT_CraftFishing, PARENT_CraftFishingInfo);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.8352668", AnchorMax = "1 1" },
                Text = { Text = String.Format(lang.GetMessage("UI_TITLE_ITEMS", this)), Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFF89") }
            }, PARENT_CraftFishingInfo);

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.232899 0.2227378", AnchorMax = "0.769544 0.8120649" },
                Image = { Color = "0 0 0 0" }
            }, PARENT_CraftFishingInfo, "ITEM_PANEL");

            #region SettingsCenter

            int ItemCount = 0;
            float itemMinPosition = 219f;
            float itemWidth = 0.473646f - 0.301563f;
            float itemMargin = 0.419895f - 0.403646f;

            int itemCount = config.FSetting[index].CraftingItem.Count;
            float itemMinHeight = 0.665741f;
            float itemHeight = 0.908333f - 0.505741f;

            if (itemCount > 6)
            {
                itemMinPosition = 0.5f - 6 / 2f * itemWidth - (6 - 1) / 2f * itemMargin;
                itemCount -= 6;
            }
            else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;

            #endregion

            for (int i = 0; i < config.FSetting[index].CraftingItem.Count; i++)
            {
                var info = config.FSetting[index].CraftingItem.ElementAt(i);
                var Formul = player.inventory.GetAmount(ItemManager.FindItemDefinition(info.Key).itemid);
                string Color = CheckResourceCraft(player, info.Key, info.Value) ? "#AB0F0F9E" : "#25AB0F9E";
                string Status = CheckResourceCraft(player, info.Key, info.Value) ? $"{Convert.ToInt32(info.Value - Formul).ToString()}" : lang.GetMessage("UI_COMPLETED_TITLE", this);

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"{itemMinPosition} {itemMinHeight}", AnchorMax = $"{itemMinPosition + itemWidth} {itemMinHeight + itemHeight}" },
                    Image = { Color = HexToRustFormat(Color), Material = "assets/content/ui/uibackgroundblur.mat" }
                }, "ITEM_PANEL", $"ITEM_{i}");

                container.Add(new CuiElement
                {
                    Parent = $"ITEM_{i}",
                    Components =
                        {
                        new CuiRawImageComponent { Png = GetImage(info.Key),  Color = HexToRustFormat("#FFFFFFFF") },
                        new CuiRectTransformComponent{  AnchorMin = $"0 0", AnchorMax = $"1 1" },
                        }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Text = { Text = Status, Font = "robotocondensed-regular.ttf", Align = TextAnchor.LowerCenter, Color = HexToRustFormat("#FFFFFF89") }
                }, $"ITEM_{i}");

                #region SettingsCenter

                ItemCount++;
                itemMinPosition += (itemWidth + itemMargin);
                if (ItemCount % 6 == 0)
                {
                    itemMinHeight -= (itemHeight + (itemMargin * 2f));

                    if (itemCount > 6)
                    {
                        itemMinPosition = 0.5f - 6 / 2f * itemWidth - (6 - 1) / 2f * itemMargin;
                        itemCount -= 6;
                    }
                    else itemMinPosition = 0.5f - itemCount / 2f * itemWidth - (itemCount - 1) / 2f * itemMargin;
                }

                #endregion
            }

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.3566774 0.07031658", AnchorMax = "0.6457654 0.1708162" },
                Button = { Command = $"cf craft_item {index}", Color = HexToRustFormat("#0F4FAB9E"), Material = "assets/content/ui/uibackgroundblur.mat" },
                Text = { Text = lang.GetMessage("UI_INTERFACE_BTN", this), Align = TextAnchor.MiddleCenter }
            }, PARENT_CraftFishingInfo);

            CuiHelper.AddUi(player, container);
        }
        #endregion

        #endregion

        #region Lang
        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_TITLE_MENU"] = "<size=30>Fisherman's menu</size>",
                ["UI_INTERFACE_BTN"] = "<size=16>Create</size>",
                ["UI_COMPLETED_TITLE"] = "<size=15>Collected</size>",
                ["UI_NO_RESOURCE"] = "<size=16>Insufficient resources</size>",
                ["UI_TITLE_ITEMS"] = "<size=16>List of items</size>",

                ["NO_BAIT_FISH_ROD"] = "<size=16>You have no bait!</size>",
                ["NO_WATER_DROP_ROD"] = "<size=14>You can only cast your fishing rod into the water!</size>",
                ["RUN_BAIT_ROD_DROP"] = "<size=12>You have successfully cast a fishing rod!\nExpect {0}s to bite!\nYou use {1}</size>",
                ["YOU_USE_ROD"] = "<size=16>You're fishing! Wait for the fish to bite</size>",
                ["TIMER_TICK"] = "<size=16>Fishing process : {0}s</size>",
                ["FINISH_ROD_FISH"] = "<size=16>You have successfully finished fishing!</size>",
                ["FISH_COMPLETE_FISHDROP"] = "<size=12>Your catch : {0}</size>",

            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["UI_TITLE_MENU"] = "<size=30>Меню рыбака</size>",
                ["UI_INTERFACE_BTN"] = "<size=16>Создать</size>",
                ["UI_COMPLETED_TITLE"] = "<size=15>Собрано</size>",
                ["UI_NO_RESOURCE"] = "<size=16>Недостаточно ресурсов</size>",
                ["UI_TITLE_ITEMS"] = "<size=16>Список предметов</size>",

                ["NO_BAIT_FISH_ROD"] = "<size=16>У вас нет наживки!</size>",
                ["NO_WATER_DROP_ROD"] = "<size=14>Вы можете забросить удочку только в воду!</size>",
                ["RUN_BAIT_ROD_DROP"] = "<size=12>Вы успешно забросили удочку!\nОжидайте {0}с до поклева!\nВы используете {1}</size>",
                ["YOU_USE_ROD"] = "<size=16>Вы уже рыбачите! Ожидайте поклев</size>",
                ["TIMER_TICK"] = "<size=16>Процесс рыбалки : {0}с</size>",
                ["FINISH_ROD_FISH"] = "<size=16>Вы успешно закончили рыбалку!</size>",
                ["FISH_COMPLETE_FISHDROP"] = "<size=12>Ваш улов : {0}</size>",
            }, this, "ru");
            PrintWarning("Lang loaded");
        }
        #endregion

        #region Helps
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                hex = "#FFFFFFFF";

            var str = hex.Trim('#');
            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            UnityEngine.Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        void MessageRust(BasePlayer player, string Messages)
        {
            rust.RunClientCommand(player, "gametip.hidegametip");
            rust.RunClientCommand(player, $"gametip.showgametip", Messages);
            timer.Once(4f, () => { rust.RunClientCommand(player, "gametip.hidegametip"); });
        }

        private bool CheckResourceCraft(BasePlayer player, string Key, int Value)
        {
            var more = new Dictionary<string, int>();
            var has = player.inventory.GetAmount(ItemManager.FindItemDefinition(Key).itemid);
            if (has < Value)
            {
                if (!more.ContainsKey(Key))
                    more.Add(Key, 0);

                more[Key] += Value - has;
            }

            if (more.ContainsKey(Key))
                return true;
            else
                return false;
        }


        #endregion
    }
}