using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Oxide.Plugins
{
	[Info("FishingTreasure", "Sempai#3239", "1.0.4")]
	[Description("Provides a chance to obtain a casket while fishing.")]
    class FishingTreasure : RustPlugin
	{
        #region Config       

        private Configuration config;
        public class Configuration
        {
            [JsonProperty("Rare roll table chance [out of 1000]")]
            public int rare_roll_table = 100;

            [JsonProperty("Elite roll table chance [out of 1000]")]
            public int elite_roll_table = 25;

            [JsonProperty("Chance to obtain a casket [out of 1000]")]
            public float casket_chance = 20;

            [JsonProperty("Fishing chest skin")]
            public ulong chest_skin = 2560835553;

            [JsonProperty("Opening effect")]
            public string open_effect = "assets/prefabs/misc/easter/painted eggs/effects/gold_open.prefab";

            [JsonProperty("Casket is rolled for whenever one of the following fish are caught")]
            public List<string> catchable;

            [JsonProperty("Send notifications when fishing")]
            public bool send_fishing_notifications = true;

            [JsonProperty("Default hotspot distance")]
            public float hot_spot_distance = 30f;

            [JsonProperty("Hotspot modifier")]
            public float hot_spot_modifier = 3.0f;

            [JsonProperty("Automatically enable new items that are added to the config?")]
            public bool auto_enable_new_items = true;

            [JsonProperty("Wipe hotspots on new save")]
            public bool auto_wipe_hotspots = true;

            [JsonProperty("Item List - Rarity key: [0 = common, 1 = rare, 2 = elite]")]
            public Dictionary<string, ItemList> Items = new Dictionary<string, ItemList>();            

            public class ItemList
            {
                public string shortname;
                public int rarity;
                public ulong skin;
                public bool enabled;
                public int maxQuantity = 1;
                public string img_url = "";
            }

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
            config.catchable = new List<string>() { "fish.herring", "fish.minnows", "fish.orangeroughy", "fish.salmon", "fish.sardine", "fish.smallshark", "fish.troutsmall", "fish.yellowperch", "fish.anchovy" };
            foreach (var item in ItemManager.itemList)
            {
                if (item == null) continue;
                var name = item.displayName.english;
                if (config.Items.ContainsKey(item.displayName.english))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        name = $"{item.displayName.english} {i}";
                        if (config.Items.ContainsKey(name)) continue;
                        else break;
                    }
                }
                    
                config.Items.Add(name, new Configuration.ItemList()
                {
                    shortname = item.shortname,
                    skin = 0,
                    rarity = 0,
                    enabled = true
                });
            }
            PrintToConsole("Setup items.");
            SaveConfig();
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

                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    PrintToConsole("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                PrintToConsole($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void SaveConfig()
        {
            PrintToConsole($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion

        #region Data

        DataInfo data;

        private DynamicConfigFile HSDATA;

        void Init()
        {
            HSDATA = Interface.Oxide.DataFileSystem.GetFile(this.Name);                     
        }

        void SaveData()
        {
            HSDATA.WriteObject(data);
        }

        void LoadData()
        {
            try
            {
                data = Interface.GetMod().DataFileSystem.ReadObject<DataInfo>(this.Name);
            }
            catch
            {
                Puts("Couldn't load data, creating new DataFile");
                data = new DataInfo();
            }
        }

        public Dictionary<string, Configuration.ItemList> common_items = new Dictionary<string, Configuration.ItemList>();
        public Dictionary<string, Configuration.ItemList> rare_items = new Dictionary<string, Configuration.ItemList>();
        public Dictionary<string, Configuration.ItemList> elite_items = new Dictionary<string, Configuration.ItemList>();

        class DataInfo
        {
            public List<HotSpotInfo> hot_spots = new List<HotSpotInfo>();
        }

        class HotSpotInfo
        {
            public Vector3 location;
            public float distance;
        }      

        void LoadItems()
        {
            foreach (KeyValuePair<string, Configuration.ItemList> kvp in config.Items)
            {
                if (kvp.Value.rarity == 2 && kvp.Value.enabled) elite_items[kvp.Key] = kvp.Value;
                else if (kvp.Value.rarity == 1 && kvp.Value.enabled) rare_items[kvp.Key] = kvp.Value;
                else if (kvp.Value.enabled) common_items[kvp.Key] = kvp.Value;
            }
        }

        #endregion;

        #region Hooks

        void OnNewSave(string filename)
        {
            UpdateItems();
            if (config.auto_wipe_hotspots) data.hot_spots.Clear();
            SaveData();
        }

        void Loaded()
        {
            LoadData();
            LoadConfig();
            LoadItems();
            permission.RegisterPermission("fishingtreasure.admin", this);
        }

        void Unload()
        {            
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "TreasureChest");
            }
            foreach (var hotspot in temp_hotspots)
            {
                data.hot_spots.Remove(hotspot.Value);
            }
            SaveData();
        }

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (item.skin == config.chest_skin)
            {
                if (action.Equals("unwrap"))
                {
                    if (Interface.CallHook("OnCasketOpen", player) != null) return false;
                    SendEffect(player);
                    item.Remove();
                    NextTick(() =>
                    {
                        GenerateLoot(player);
                    });
                    return false;
                }
                else if (action.Equals("upgrade")) return false;
            }               
            return null;
        }

        private Effect reusableSoundEffectInstance = new Effect();
        private bool soundEffectIsValid;

        private void ValidateEffects()
        {
            if (string.IsNullOrEmpty(config.open_effect))
            {
                return;
            }

            if (Prefab.DefaultManager.FindPrefab(config.open_effect) == null)
            {
                Puts("Invalid Sound Prefab: open_effect = {0}", config.open_effect);
            }
            else soundEffectIsValid = true;
        }

        private void SendEffect(BasePlayer player)
        {
            if (!soundEffectIsValid)
            {
                return;
            }

            reusableSoundEffectInstance.Init(Effect.Type.Generic, player, 0, Vector3.zero, Vector3.forward);
            reusableSoundEffectInstance.pooledString = config.open_effect;

            EffectNetwork.Send(reusableSoundEffectInstance, player.Connection);
        }

        #endregion

        #region ChatCommands

        [ChatCommand("addhotspot")]
        void AddHotSpot(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "fishingtreasure.admin")) return;
            var dist = config.hot_spot_distance;

            if (args.Length > 0 && args[0].IsNumeric())
            {
                dist = Convert.ToSingle(args[0]);
            }
            data.hot_spots.Add(new HotSpotInfo()
            {
                location = player.transform.position,
                distance = dist
            });
            SaveData();
            PrintToChat(player, $"Added new hotspot. Location: {player.transform.position} - Distance: {dist}");
            CreateSphere(player.transform.position, dist, 8);
        }

        [ChatCommand("clearhotspots")]
        void ClearHotSpots(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "fishingtreasure.admin")) return;
            data.hot_spots.Clear();
            SaveData();
            PrintToChat(player, "Removed all hotspots.");
        }        

        [ChatCommand("casket")]
        void GiveCastket(BasePlayer player)
        { 
            if (!permission.UserHasPermission(player.UserIDString, "fishingtreasure.admin")) return;
            CreateChest(player);
        }

        [ChatCommand("updatecasketitems")]
        void UpdateItems(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, "fishingtreasure.admin")) return;
            UpdateItems();
        }

        [ChatCommand("tbadditem")]
        void AddItem(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "fishingtreasure.admin")) return;
            // displayname, shortname, rarity, skin
            if (args.Length < 2 && args.Length > 4)
            {
                PrintToChat(player, "Usage: /tbadditem <display name> <short name> <skin> <max quantity>");
                return;
            }
            var key = args[0];
            if (config.Items.ContainsKey(key))
            {
                PrintToChat(player, $"{key} already exists in the config. Set a unique name.");
                return;
            }
            var shortname = args[1].ToLower();
            var def = ItemManager.FindItemDefinition(shortname);
            if (def == null)
            {
                PrintToChat(player, $"{shortname} is not a valid shortname.");
                return;
            }

            var skin = 0ul;
            var max_quantity = 1;
            if (args.Length > 2) if (args[2].IsNumeric()) skin = Convert.ToUInt64(args[2]);
            if (args.Length > 3) if (args[3].IsNumeric()) max_quantity = Convert.ToInt32(args[3]);

            config.Items.Add(key, new Configuration.ItemList()
            {
                shortname = shortname,
                skin = skin,
                maxQuantity = max_quantity
            });
            SaveConfig();
            PrintToChat(player, $"Saved new item: {key} [{shortname}, {skin} {max_quantity}]");
        }

        [ChatCommand("tbremoveitem")]
        void RemoveItem(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "fishingtreasure.admin")) return;
            if (args.Length != 1)
            {
                PrintToChat(player, "Usage: /tbremoveitem <display name>");
                return;
            }
            if (config.Items.ContainsKey(args[0]))
            {
                config.Items.Remove(args[0]);                
                SaveConfig();
                PrintToChat(player, $"Removed {args[0]} from the config.");
            }
            else
            {
                PrintToChat(player, $"Could not find {args[0]} in the config. Check the case sensitivity.");
            }
        }

        #endregion

        #region Helper

        void UpdateItems()
        {
            var itemcount = config.Items.Count;
            foreach (var item in ItemManager.GetItemDefinitions())
            {
                var name = item.displayName.english;
                if (config.Items.ContainsKey(name))
                {
                    if (item.shortname == config.Items[name].shortname) continue;
                    for (int i = 0; i < 10; i++)
                    {
                        name = $"{item.displayName.english} {i}";
                        if (config.Items.ContainsKey(name))
                        {
                            if (item.shortname == config.Items[name].shortname)
                            {
                                name = null;
                                break;
                            }
                            continue;
                        }                            
                        else break;
                    }
                }
                if (name == null || config.Items.ContainsKey(name)) continue;
                config.Items.Add(name, new Configuration.ItemList()
                {
                    shortname = item.shortname,
                    skin = 0,
                    rarity = 0,
                    enabled = config.auto_enable_new_items
                });
                Puts($"Added {name} to the list.");
            }
            if (config.Items.Count > itemcount) Puts($"Added {config.Items.Count - itemcount} new items to the config.");
            SaveConfig();
        }

        string GetRarity(int rarity)
        {
            if (rarity == 2) return elite_col;
            else if (rarity == 1) return rare_col;
            else return common_col;
        }
        
        void CreateChest(BasePlayer player)
        {
            var item = ItemManager.CreateByName("halloween.lootbag.medium", 1, config.chest_skin);
            item.name = "Casket";
            if (!player.inventory.containerBelt.IsFull()) item.MoveToContainer(player.inventory.containerBelt);
            else if (!player.inventory.containerMain.IsFull()) item.MoveToContainer(player.inventory.containerMain);
            else
            {
                PrintToChat(player, "You found a Casket but have no room in your inventory, so it was dropped on the ground.");
                item.DropAndTossUpwards(player.transform.position);
            }
        }

        private void CreateSphere(Vector3 position, float radius, int darkness)
        {
            List<SphereEntity> spheres = new List<SphereEntity>();
            for (int i = 0; i < darkness; i++)
            {
                SphereEntity sphere = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", position, new Quaternion(), true) as SphereEntity;
                sphere.currentRadius = radius * 2;
                sphere.lerpSpeed = 0f;
                sphere.Spawn();
                spheres.Add(sphere);
            }
            timer.Once(10f, () =>
            {
                foreach (var sphere in spheres)
                {
                    sphere.KillMessage();
                }
            });
        }

        #endregion

        #region Menu

        const string common_col = "0.2971698 0.3561946 1 0.7058824";
        const string rare_col = "0.8018868 0 0.493538 1";
        const string elite_col = "0.8679245 0.2812081 0 1";

        void TreasureChestMenu(BasePlayer player, Dictionary<int, MenuInfo> items)
        {
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = { Color = "0 0 0 0.8823529" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "-0.351 -0.332", OffsetMax = "0.349 0.338" }
            }, "Overlay", "TreasureChest");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 0.7528849 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-247.5 -133", OffsetMax = "247.5 -130" }
            }, "TreasureChest", "TreasureChest_boarderBottom");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 0.7529412 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-247.5 130", OffsetMax = "247.5 133" }
            }, "TreasureChest", "TreasureChest_boarderTop");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 0.7529412 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-247.5 -130", OffsetMax = "-244.5 130" }
            }, "TreasureChest", "TreasureChest_boarderLeft");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 0.7529412 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "244.5 -130", OffsetMax = "247.5 130" }
            }, "TreasureChest", "TreasureChest_boarderRight");

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "0 0 0 0.9215686" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-240 -130", OffsetMax = "240 130" }
            }, "TreasureChest", "TreasureChest_backpanel");

            container.Add(new CuiElement
            {
                Name = "TreasureChestTitle",
                Parent = "TreasureChest",
                Components = {
                    new CuiTextComponent { Text = "You open the chest and find...", Font = "robotocondensed-regular.ttf", FontSize = 24, Align = TextAnchor.LowerCenter, Color = "1 0.7529412 0 1" },
                    new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-240 31.001", OffsetMax = "240 130" }
                }
            });
            if (items.Count > 0)
            {
                var item_col_1 = GetRarity(items[1].rarity);

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = item_col_1 },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-176 -84", OffsetMax = "-80 12" }
                }, "TreasureChest", "TreasureChest_item_backpanel_1");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-45 -45", OffsetMax = "45 45" }
                }, "TreasureChest_item_backpanel_1", "TreasureChest_item_frontpanel_1");

                container.Add(new CuiElement
                {
                    Name = "TreasureChest_item_img_1",
                    Parent = "TreasureChest_item_frontpanel_1",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Url = GetIMGUrl(items.ElementAt(0).Value.key) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-45 -45", OffsetMax = "45 45" }
                }
                });
            }
            
            if (items.Count > 1)
            {
                var item_col_2 = GetRarity(items[2].rarity);

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = item_col_2 },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-48 -84", OffsetMax = "48 12" }
                }, "TreasureChest", "TreasureChest_item_backpanel_2");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-45 -45", OffsetMax = "45 45" }
                }, "TreasureChest_item_backpanel_2", "TreasureChest_item_frontpanel_2");

                container.Add(new CuiElement
                {
                    Name = "TreasureChest_item_img_2",
                    Parent = "TreasureChest_item_frontpanel_2",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Url = GetIMGUrl(items.ElementAt(1).Value.key) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-45 -45", OffsetMax = "45 45" }
                }
                });
            }

            if (items.Count > 2)
            {
                var item_col_3 = GetRarity(items[3].rarity);

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = item_col_3 },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "80 -84", OffsetMax = "176 12" }
                }, "TreasureChest", "TreasureChest_item_backpanel_3");

                container.Add(new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0 0 0 1" },
                    RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-45 -45", OffsetMax = "45 45" }
                }, "TreasureChest_item_backpanel_3", "TreasureChest_item_frontpanel_3");

                container.Add(new CuiElement
                {
                    Name = "TreasureChest_item_img_3",
                    Parent = "TreasureChest_item_frontpanel_3",
                    Components = {
                    new CuiRawImageComponent { Color = "1 1 1 1", Url = GetIMGUrl(items.ElementAt(2).Value.key) },
                    new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-45 -45", OffsetMax = "45 45" }
                }
                });
            }                

            container.Add(new CuiPanel
            {
                CursorEnabled = false,
                Image = { Color = "1 0.7529413 0 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-30 -173.5", OffsetMax = "30 -151.5" }
            }, "TreasureChest", "TreasureChest_Button_panel");

            container.Add(new CuiButton
            {
                Button = { Color = "0 0 0 1", Command = "closetreasurechest" },
                Text = { Text = "CLOSE", Font = "robotocondensed-regular.ttf", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = "-28 -9", OffsetMax = "28 9" }
            }, "TreasureChest_Button_panel", "TreasureChest_button");

            CuiHelper.DestroyUi(player, "TreasureChest");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("closetreasurechest")]
        void CloseMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, "TreasureChest");
        }

        #endregion

        #region GenerateLoot

        public class ItemInfo
        {
            public string shortname;
            public int rarity;
            public ulong skin;
            public int maxQuantity;
        }

        public class MenuInfo
        {
            public string key;
            public string shortname;
            public int rarity;
        }

        void GenerateLoot(BasePlayer player)
        {
            //Dictionary<string, ItemInfo> items = new Dictionary<string, ItemInfo>();
            Dictionary<int, MenuInfo> menu_info = new Dictionary<int, MenuInfo>();
            var count = 0;
            for (int i = 0; i < 3; i++)
            {
                var item = new Item();
                var roll = UnityEngine.Random.Range(0, 1000);
                if (roll <= config.elite_roll_table && elite_items.Count > 0)
                {
                    var itemDef = elite_items.ElementAt(UnityEngine.Random.Range(0, elite_items.Count));                    
                    var itemInfo = new ItemInfo()
                    {
                        shortname = itemDef.Value.shortname,
                        rarity = itemDef.Value.rarity,
                        skin = itemDef.Value.skin,
                        maxQuantity = itemDef.Value.maxQuantity
                    };
                    CreateItems(player, itemDef.Key, itemInfo);
                    count++;
                    menu_info.Add(count, new MenuInfo()
                    {
                        key = itemDef.Key,
                        shortname = itemDef.Value.shortname,
                        rarity = itemDef.Value.rarity
                    });
                }
                else if (roll > config.elite_roll_table && roll <= config.rare_roll_table && rare_items.Count > 0)
                {
                    var itemDef = rare_items.ElementAt(UnityEngine.Random.Range(0, rare_items.Count));
                    var itemInfo = new ItemInfo()
                    {
                        shortname = itemDef.Value.shortname,
                        rarity = itemDef.Value.rarity,
                        skin = itemDef.Value.skin,
                        maxQuantity = itemDef.Value.maxQuantity
                    };
                    CreateItems(player, itemDef.Key, itemInfo);
                    count++;
                    menu_info.Add(count, new MenuInfo()
                    {
                        key = itemDef.Key,
                        shortname = itemDef.Value.shortname,
                        rarity = itemDef.Value.rarity
                    });
                }
                else
                {
                    var itemDef = common_items.ElementAt(UnityEngine.Random.Range(0, common_items.Count));
                    var itemInfo = new ItemInfo()
                    {
                        shortname = itemDef.Value.shortname,
                        rarity = itemDef.Value.rarity,
                        skin = itemDef.Value.skin,
                        maxQuantity = itemDef.Value.maxQuantity
                    };
                    CreateItems(player, itemDef.Key, itemInfo);
                    count++;
                    menu_info.Add(count, new MenuInfo()
                    {
                        key = itemDef.Key,
                        shortname = itemDef.Value.shortname,
                        rarity = itemDef.Value.rarity
                    });
                }
                if (count == 3) break;
            }
            if (count != 3)
            {
                PrintToConsole("Error getting 3 items");
            }
            
            TreasureChestMenu(player, menu_info);            
        }

        void CreateItems(BasePlayer player, string key, ItemInfo info)
        {
            var qty = 1;
            if (info.maxQuantity > 1) qty = UnityEngine.Random.Range(1, info.maxQuantity + 1);
            var item = ItemManager.CreateByName(info.shortname, qty, info.skin);
            item.name = key;
            if (!player.inventory.containerBelt.IsFull() || !player.inventory.containerMain.IsFull()) player.GiveItem(item);
            else
            {
                PrintToChat(player, "Items were dropped to the floor.");
                item.DropAndTossUpwards(player.transform.position);
            }
                
        }

        #endregion

        #region Fishing

        public Dictionary<string, string> imgURLs = new Dictionary<string, string>();

        void OnServerInitialized(bool initial)
        {
			PrintWarning("\n-----------------------------\n" +
			"     Author - Sempai#3239\n" +
			"     VK - https://vk.com/rustnastroika\n" +
			"     Discord - https://discord.gg/5DPTsRmd3G\n" +
			"-----------------------------");
            if (config.catchable == null)
            {
                config.catchable = new List<string>() { "fish.herring", "fish.minnows", "fish.orangeroughy", "fish.salmon", "fish.sardine", "fish.smallshark", "fish.troutsmall", "fish.yellowperch", "fish.anchovy" };
                SaveConfig();
            }
            ValidateEffects();
            foreach (var item in config.Items)
            {
                string itemData;
                if (!imgURLs.TryGetValue(item.Key, out itemData)) imgURLs.Add(item.Key, itemData = null);
                if (item.Value.skin > 0 && item.Value.img_url != null) itemData = item.Value.img_url;
                else itemData = $"https://rustlabs.com/img/items180/{item.Value.shortname}.png";

                var url = $"https://rustlabs.com/img/items180/{item.Value.shortname}.png";
                if (item.Value.skin > 0 && item.Value.img_url != null) url = item.Value.img_url;

                if (imgURLs.ContainsKey(item.Key)) imgURLs[item.Key] = url;
                else imgURLs.Add(item.Key, url);
            }
        }

        string GetIMGUrl(string key)
        {
            string itemData;
            if (!imgURLs.TryGetValue(key, out itemData)) return null;
            Puts($"Returning: {itemData}");
            return itemData;
        }

        object CanStackItem(Item item, Item targetItem)
        {
            if (item.skin == config.chest_skin || targetItem.skin == config.chest_skin) return false;
            return null;
        }

        object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (item.item.skin == config.chest_skin || targetItem.item.skin == config.chest_skin) return false;
            return null;
        }

        void CanCatchFish(BasePlayer player, BaseFishingRod fishingRod, Item fish)
        {
            if (!config.catchable.Contains(fish.info.shortname)) return;
            var s = $"You caught a {fish.info.displayName.english}";
            var chance = 0f;
            object hookResult = Interface.CallHook("ModifyCasketChance", player);
            if (hookResult is float && hookResult != null) chance = config.casket_chance * (float)hookResult;
            else chance = config.casket_chance;
            foreach (var hotspot in data.hot_spots)
            {
                if (Vector3.Distance(hotspot.location, player.transform.position) <= hotspot.distance)
                {
                    chance = chance * config.hot_spot_modifier;
                    //chance = 1000f - (config.casket_chance * config.hot_spot_modifier) + config.casket_chance;
                    s = $"{s} while fishing in a hotspot.";
                }
            }
            if (config.send_fishing_notifications) PrintToChat(player, s);

            var roll = UnityEngine.Random.Range(0, 1000f);
            if (roll >= (1000 - chance))
            {
                Interface.CallHook("OnCasketCaught", player);
                PrintToChat(player, "<color=#E400FF>You found a Casket.</color>");
                CreateChest(player);
            }
        }

        #endregion

        #region API

        [HookMethod("GetCasketSkin")]
        ulong GetCasketSkin()
        {
            return config.chest_skin;
        }

        Dictionary<int, HotSpotInfo> temp_hotspots = new Dictionary<int, HotSpotInfo>();

        [HookMethod("AddTempHotspot")]
        int AddTempHotspot(Vector3 pos, float size)
        {
            for (int i = 0; i < 1000; i++)
            {
                if (!temp_hotspots.ContainsKey(i))
                {
                    temp_hotspots.Add(i, new HotSpotInfo()
                    {
                        distance = size,
                        location = pos
                    });
                    Puts($"Adding temporary hotspot [{i}] at {pos}.");
                    return i;
                }
            }
            return -1;
        }

        [HookMethod("RemoveTempHotspot")]
        void RemoveTempHotspot(int index)
        {
            Puts($"Removing temporary hotspot [{index}].");
            if (temp_hotspots.ContainsKey(index))
            {
                data.hot_spots.Remove(temp_hotspots[index]);
                temp_hotspots.Remove(index);
            }
        }

        #endregion
    }
}
