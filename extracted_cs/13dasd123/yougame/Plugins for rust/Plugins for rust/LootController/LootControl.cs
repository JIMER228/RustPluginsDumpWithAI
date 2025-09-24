using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine;
using System;

using Random = System.Random;

namespace Oxide.Plugins
{
    [Info("LootControl", "CodeHarbour", "1.0.0")]
    class LootControl : RustPlugin
    {
        private LootControlConfig config;

        void Init()
        {
            RegisterPermissions();
            LoadDefaultMessages();

            cmd.AddChatCommand(config.refreshContainersChatCommand, this, "cmdChatRefreshContainers");
            cmd.AddConsoleCommand(config.refreshContainersConsoleCommand, this, "cmdConsoleRefreshContainers");
        }

        void RegisterPermissions()
        {
            permission.RegisterPermission(config.refreshContainersPermission, this);
        }

        void cmdChatRefreshContainers(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString, config.refreshContainersPermission))
            {
                player.ChatMessage(Lang("NoPermission", player.UserIDString));
                return;
            }

            int refreshedContainers = RefreshContainers();

            player.ChatMessage(Lang("RefreshContainers", player.UserIDString, refreshedContainers));
        }

        void cmdConsoleRefreshContainers(ConsoleSystem.Arg arg)
        {
            if (arg?.Connection != null && !permission.UserHasPermission(arg?.Player()?.userID.ToString(), config.refreshContainersPermission)) return;

            int refreshedContainers = RefreshContainers();

            Puts(Lang("RefreshContainers", null, refreshedContainers));
        }

        int RefreshContainers()
        {
            int n = 0;
            foreach (var entity in BaseNetworkable.serverEntities.Where(x => config.containers.ContainsKey(x.name)).ToList())
            {
                if (entity == null) continue;

                var container = entity?.GetComponent<LootContainer>();
                if (!container) continue;

                var inventory = container?.inventory;
                if (inventory == null) continue;

                SuppressRefresh(container);
                ClearInventory(inventory);
                Puts(entity.name);
                AddItems(inventory, entity.name);

                n++;
            }

            return n;
        }

        Item NewItem(string type, int itemIDinList)
        {
            int multiplier = config.itemMultiplier;
			
			var tempItem = ItemManager.CreateByPartialName(config.containers[type][itemIDinList].ItemShortname, 1);

            if (config.itemMultiplierBlackList.Contains(config.containers[type][itemIDinList].ItemShortname) || (tempItem.info.stackable == 1 && !config.ignoreItemMaxStackSize))
            {
                multiplier = 1;
            }

            var item = ItemManager.CreateByPartialName(config.containers[type][itemIDinList].ItemShortname, UnityEngine.Random.Range(config.containers[type][itemIDinList].Min * multiplier, config.containers[type][itemIDinList].Max * multiplier));

            if (item == null)
            {
                PrintWarning($"INVALID ITEM: '{config.containers[type][itemIDinList].ItemShortname}' - Skipping item!");
                return null;
            }

            if (item.info.shortname == "hat.miner") ItemManager.CreateByPartialName("lowgradefuel", 5).MoveToContainer(item.contents);
            else if (item.info.shortname == "smallwaterbottle") ItemManager.CreateByPartialName("water", 120).MoveToContainer(item.contents);

            #region RandomSkin
            if (config.spawnRandomSkins)
            {
                if (new System.Random().Next(100) <= config.spawnRandomSkinsChancePercentage)
                {
                    var skins = GetSkins(ItemManager.FindItemDefinition(item.info.itemid));

                    if (skins.Count > 0)
                        item.skin = Convert.ToUInt32(skins.GetRandom());
                }
            }
            #endregion
            return item;
        }

        void AddItems(ItemContainer inventory, string type)
        {
            if (!config.containers.ContainsKey(type))
            {
                return;
            }

            Random random = new Random();

            var items = new List<string>();

            int loops = config.itemsPerContainer[type];
            if (config.itemsPerContainer.ContainsKey(type))
            {
                if (config.itemsPerContainer[type] > config.containers[type].Count) loops = 1;
                config.containers[type] = config.containers[type].OrderBy(a => Guid.NewGuid()).ToList();
            }

            for (int a = 0; a < loops; a++)
            {
                Start:
                float totalChance = 0;

                foreach (ItemData i in config.containers[type])
                    totalChance += i.ChancePercentage;

                float ran = UnityEngine.Random.Range(0f, totalChance);
                float upper = 0;

                for (int j = 0; j < config.containers[type].Count; j++)
                {
                    Top:
                    if (config.containers[type][j].ChancePercentage == 100)
                    {
                        Item _item100 = NewItem(type, j);
                        if (_item100 != null) _item100.MoveToContainer(inventory);
                        j++;

                        if (j >= config.containers[type].Count)
                            return;

                        goto Top;
                    }

                    upper += config.containers[type][j].ChancePercentage;
                    if (ran < upper)
                    {
                        if (config.preventDuplicateItems)
                        {
                            if (items.Contains(config.containers[type][j].ItemShortname)) goto Start;

                            items.Add(config.containers[type][j].ItemShortname);
                        }

                        Item _item = NewItem(type, j);
                        if (_item != null) _item.MoveToContainer(inventory);

                        break;
                    }
                }
            }
        }

        void OnEntitySpawned(BaseEntity entity)
        {
            var container = entity?.GetComponent<LootContainer>();
            if (!container) return;

            var inventory = container?.inventory;
            if (inventory == null) return;

            SuppressRefresh(container);

            string name = entity.name;

            if (config.containers.Keys.Contains(name))
            {
                ClearInventory(inventory);

                AddItems(inventory, name);
            }
        }

        bool IsEmpty(ItemContainer inv)
        {
            return inv?.itemList?.Count <= 0;
        }

        void SuppressRefresh(LootContainer container)
        {
            container.minSecondsBetweenRefresh = -1;
            container.maxSecondsBetweenRefresh = 0;
            container.CancelInvoke("SpawnLoot");
        }

        void ClearInventory(ItemContainer inventory)
        {
            inventory?.itemList.Clear();
        }

        Dictionary<string, List<int>> skinList = new Dictionary<string, List<int>>();
        List<int> GetSkins(ItemDefinition def)
        {
            List<int> skins;
            if (skinList.TryGetValue(def.shortname, out skins)) return skins;
            skins = new List<int>();
            if (def.skins != null) skins.AddRange(def.skins.Select(skin => skin.id));
            if (def.skins2 != null) skins.AddRange(def.skins2.Select(skin => skin.Id));
            skinList.Add(def.shortname, skins);
            return skins;
        }

        void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this command.",
                ["RefreshContainers"] = "Refreshed {0} Containers."
            }, this);
        }

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        public class ItemData
        {
            public string ItemShortname;
            public int Min, Max;
            public float ChancePercentage;

            public ItemData(string name, int min, int max, float chance)
            {
                ItemShortname = name;
                Min = min;
                Max = max;
                ChancePercentage = chance;
            }
        }

        public class LootControlConfig
        {
            public string refreshContainersChatCommand = "refreshcontainers";
            public string refreshContainersConsoleCommand = "lootcontrol.refresh";
            public string refreshContainersPermission = "lootcontrol.refresh";

            public int itemMultiplier = 1;
            public float spawnRandomSkinsChancePercentage = 25f;

            public bool spawnRandomSkins = false;
            public bool preventDuplicateItems = true;
            public bool ignoreItemMaxStackSize = false;

            [JsonProperty(PropertyName = "ItemMultiplierBlackList")]
            public List<string> itemMultiplierBlackList;

            [JsonProperty(PropertyName = "ItemsPerContainer")]
            public Dictionary<string, int> itemsPerContainer;

            [JsonProperty(PropertyName = "Containers")]
            public Dictionary<string, List<ItemData>> containers;

            public static LootControlConfig DefaultConfig()
            {
                return new LootControlConfig
                {
                    itemMultiplierBlackList = new List<string>()
                    {
                        "explosive.timed",
                        "supply.signal"
                    },
                    itemsPerContainer = new Dictionary<string, int>()
                    {
                        { "assets/bundled/prefabs/radtown/minecart.prefab", 1 },
                        { "assets/bundled/prefabs/radtown/crate_mine.prefab", 1 },
                        { "assets/bundled/prefabs/radtown/crate_normal.prefab", 1 },
                        { "assets/bundled/prefabs/radtown/crate_normal_2.prefab", 1 },
                        { "assets/bundled/prefabs/radtown/crate_normal_2_food.prefab", 2 },
                        { "assets/bundled/prefabs/radtown/crate_normal_2_medical.prefab", 2 },
                        { "assets/bundled/prefabs/radtown/dmloot/dm medical.prefab", 2 },
                        { "assets/bundled/prefabs/radtown/dmloot/dm food.prefab", 2 },
                        { "assets/bundled/prefabs/radtown/crate_tools.prefab", 2 },
                        { "assets/bundled/prefabs/radtown/foodbox.prefab", 2 },
                        { "assets/bundled/prefabs/radtown/loot_barrel_1.prefab", 1 },
                        { "assets/bundled/prefabs/radtown/loot_barrel_2.prefab", 1 },
                        { "assets/bundled/prefabs/radtown/loot_trash.prefab", 1 },
                        { "assets/bundled/prefabs/radtown/oil_barrel.prefab", 1 },
                        { "assets/prefabs/npc/patrol helicopter/heli_crate.prefab", 3 },
                        { "assets/prefabs/misc/supply drop/supply_drop.prefab", 4 }
                    },
                    containers = new Dictionary<string, List<ItemData>>()
                    {
                        {
                            "assets/bundled/prefabs/radtown/oil_barrel.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "lowgradefuel", 5, 9, 100f ),
                                new ItemData( "crude.oil", 15, 19, 100f ),
                            }
                        },
                        {
                            "assets/bundled/prefabs/radtown/loot_barrel_1.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "metal.fragments", 10, 14, 5f ),
                                new ItemData( "wood", 100, 149, 20f ),
                                new ItemData( "metalblade", 1, 1, 15f ),
                                new ItemData( "tarp", 1, 1, 10f ),
                                new ItemData( "rope", 1, 2, 20f ),
                                new ItemData( "metalpipe", 1, 4, 1.50f ),
                                new ItemData( "sewingkit", 3, 4, 10f ),
                                new ItemData( "gears", 2, 2, 2f ),
                                new ItemData( "propanetank", 1, 1, 10f ),
                                new ItemData( "semibody", 1, 1, 1.5f ),
                                new ItemData( "roadsigns", 2, 3, 2f ),
                                new ItemData( "metalspring", 1, 1, 1.5f ),
                                new ItemData( "sheetmetal", 1, 1, 2f )
                            }
                        },
                        {
                            "assets/bundled/prefabs/radtown/loot_barrel_2.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "metal.fragments", 10, 14, 5f ),
                                new ItemData( "wood", 100, 149, 20f ),
                                new ItemData( "metalblade", 1, 1, 15f ),
                                new ItemData( "tarp", 1, 1, 10f ),
                                new ItemData( "rope", 1, 2, 20f ),
                                new ItemData( "metalpipe", 1, 4, 1.50f ),
                                new ItemData( "sewingkit", 3, 4, 10f ),
                                new ItemData( "gears", 2, 2, 2f ),
                                new ItemData( "propanetank", 1, 1, 10f ),
                                new ItemData( "semibody", 1, 1, 1.5f ),
                                new ItemData( "roadsigns", 2, 3, 2f ),
                                new ItemData( "metalspring", 1, 1, 1.5f ),
                                new ItemData( "sheetmetal", 1, 1, 2f )
                            }
                        },
                        {
                            "assets/bundled/prefabs/radtown/loot_trash.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "wood", 100, 149, 75f ),
                                new ItemData( "metal.fragments", 10, 14, 25f )
                            }
                        },
                        {
                            "assets/bundled/prefabs/radtown/minecart.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "lowgradefuel", 10, 10, 60f ),
                                new ItemData( "metal.refined", 5, 5, 5f ),
                                new ItemData( "metal.fragments", 30, 49, 10f ),
                                new ItemData( "gunpowder", 15, 24, 10f ),
                                new ItemData( "hat.candle", 1, 1, 30f ),
                                new ItemData( "hat.miner", 1, 1, 30f ),
                                new ItemData( "hatchet", 1, 1, 3f ),
                                new ItemData( "pickaxe", 1, 1, 2.5f ),
                                new ItemData( "axe.salvaged", 1, 1, 1f ),
                                new ItemData( "icepick.salvaged", 1, 1, 1f ),
                                new ItemData( "surveycharge", 1, 2, 5f ),
                                new ItemData( "binoculars", 1, 1, 3f )
                            }
                        },
                        {
                            "assets/bundled/prefabs/radtown/crate_mine.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "lowgradefuel", 10, 10, 60f ),
                                new ItemData( "metal.refined", 5, 5, 5f ),
                                new ItemData( "metal.fragments", 30, 49, 10f ),
                                new ItemData( "gunpowder", 15, 24, 10f ),
                                new ItemData( "hat.candle", 1, 1, 30f ),
                                new ItemData( "hat.miner", 1, 1, 30f ),
                                new ItemData( "hatchet", 1, 1, 3f ),
                                new ItemData( "pickaxe", 1, 1, 3f ),
                                new ItemData( "axe.salvaged", 1, 1, 1f ),
                                new ItemData( "icepick.salvaged", 1, 1, 1f ),
                                new ItemData( "surveycharge", 1, 2, 5f ),
                                new ItemData( "binoculars", 1, 1, 3f )
                            }
                        },
                        {
                            "assets/bundled/prefabs/radtown/crate_normal.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "smgbody", 1, 1, 15f ),
                                new ItemData( "metalpipe", 3, 5, 15f ),
                                new ItemData( "techparts", 2, 3, 15f ),
                                new ItemData( "riflebody", 1, 1, 15f ),
                                new ItemData( "metal.refined", 15, 24, 15f ),
                                new ItemData( "targeting.computer", 1, 1, 8f ),
                                new ItemData( "cctv.camera", 1, 1, 8f ),
                                new ItemData( "supply.signal", 1, 1, 2f )
                            }
                        },
                        {
                            "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "metalspring", 1, 1, 15f ),
                                new ItemData( "roadsigns", 2, 3, 15f ),
                                new ItemData( "metalpipe", 1, 4, 15f ),
                                new ItemData( "sheetmetal", 1, 1, 15f ),
                                new ItemData( "gears", 2, 2, 15f ),
                                new ItemData( "cctv.camera", 1, 1, 2.5f ),
                                new ItemData( "targeting.computer", 1, 1, 2.5f ),
                                new ItemData( "hazmatsuit", 1, 1, 5f )
                            }
                        },
                        {
                            "assets/bundled/prefabs/radtown/crate_normal_2_food.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "smallwaterbottle", 1, 1, 30f ),
                                new ItemData( "pumpkin", 1, 1, 10f ),
                                new ItemData( "apple", 1, 1, 10f ),
                                new ItemData( "black.raspberries", 1, 1, 10f ),
                                new ItemData( "granolabar", 1, 1, 10f ),
                                new ItemData( "chocholate", 1, 1, 10f ),
                                new ItemData( "can.tuna", 1, 1, 10f ),
                                new ItemData( "can.beans", 1, 1, 10f ),
                                new ItemData( "blueberries", 1, 1, 10f )
                            }
                        },
                        {
                            "assets/bundled/prefabs/radtown/foodbox.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "smallwaterbottle", 1, 1, 30f ),
                                new ItemData( "antiradpills", 1, 2, 30f ),
                                new ItemData( "chocholate", 1, 2, 25f ),
                                new ItemData( "can.tuna", 1, 2, 25f ),
                                new ItemData( "granolabar", 1, 2, 25f ),
                                new ItemData( "apple", 1, 2, 25f ),
                                new ItemData( "can.beans", 1, 2, 25f )
                            }
                        },
                        {
                            "assets/bundled/prefabs/radtown/dmloot/dm medical.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "largemedkit", 1, 1, 99.5f ),
                                new ItemData( "bandage", 1, 2, 60f ),
                                new ItemData( "syringe.medical", 1, 2, 20f )
                            }
                        },
                        {
                            "assets/bundled/prefabs/radtown/crate_normal_2_medical.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "largemedkit", 1, 1, 99.5f ),
                                new ItemData( "bandage", 1, 1, 40f ),
                                new ItemData( "syringe.medical", 1, 1, 10f )
                            }
                        },
                        {
                            "assets/bundled/prefabs/radtown/dmloot/dm food.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "smallwaterbottle", 1, 1, 70f ),
                                new ItemData( "can.tuna", 1, 2, 15f ),
                                new ItemData( "can.beans", 1, 2, 15f ),
                                new ItemData( "pumpkin", 1, 1, 15f ),
                                new ItemData( "apple", 1, 2, 15f ),
                                new ItemData( "granolabar", 1, 2, 15f ),
                                new ItemData( "chocholate", 1, 2, 15f ),
                                new ItemData( "blueberries", 1, 2, 15f ),
                                new ItemData( "black.raspberries", 1, 2, 15f )
                            }
                        },
                        {
                            "assets/bundled/prefabs/radtown/crate_tools.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "wood.armor.jacket", 1, 1, 8f ),
                                new ItemData( "wood.armor.pants", 1, 1, 8f ),
                                new ItemData( "salvaged.cleaver", 1, 1, 8f ),
                                new ItemData( "salvaged.sword", 1, 1, 8f ),
                                new ItemData( "icepick.salvaged", 1, 1, 3f ),
                                new ItemData( "axe.salvaged", 1, 1, 3f ),
                                new ItemData( "arrow.wooden", 5, 5, 10f ),
                                new ItemData( "bow.hunting", 1, 1, 8f ),
                                new ItemData( "crossbow", 1, 1, 3.5f ),
                                new ItemData( "hoodie", 1, 1, 5f ),
                                new ItemData( "mace", 1, 1, 8f ),
                                new ItemData( "pickaxe", 1, 1, 8f ),
                                new ItemData( "pants", 1, 1, 8f ),
                                new ItemData( "binoculars", 1, 1, 10f ),
                                new ItemData( "hatchet", 1, 1, 8f ),
                                new ItemData( "tshirt.long", 1, 1, 10f ),
                                new ItemData( "targeting.computer", 1, 1, 3.5f ),
                                new ItemData( "cctv.camera", 1, 1, 3.5f )
                            }
                        },
                        {
                            "assets/prefabs/npc/patrol helicopter/heli_crate.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "cctv.camera", 1, 1, 8f ),
                                new ItemData( "targeting.computer", 1, 1, 8f ),
                                new ItemData( "explosive.timed", 2, 2, 15f ),
                                new ItemData( "smg.2", 1, 1, 2f ),
                                new ItemData( "smg.mp5", 1, 1, 1f ),
                                new ItemData( "smg.thompson", 1, 1, 2.5f ),
                                new ItemData( "lmg.m249", 1, 1, 5f ),
                                new ItemData( "pistol.m92", 1, 1, 2f ),
                                new ItemData( "rifle.ak", 1, 1, 1f ),
                                new ItemData( "rifle.bolt", 1, 1, 1f ),
                                new ItemData( "rifle.lr300", 1, 1, 0.5f ),
                                new ItemData( "ammo.pistol", 15, 200, 15f ),
                                new ItemData( "ammo.pistol.fire", 80, 80, 10f ),
                                new ItemData( "ammo.pistol.hv", 60, 60, 10f ),
                                new ItemData( "ammo.rifle", 8, 240, 20f ),
                                new ItemData( "ammo.rifle.incendiary", 120, 120, 10f ),
                                new ItemData( "ammo.rifle.explosive", 60, 60, 10f ),
                                new ItemData( "ammo.rifle.hv", 80, 80, 10f ),
                                new ItemData( "ammo.rocket.basic", 3, 3, 15f ),
                                new ItemData( "ammo.rocket.fire", 6, 6, 10f ),
                                new ItemData( "ammo.rocket.hv", 4, 4, 10f ),
                                new ItemData( "weapon.mod.holosight", 1, 1, 15f ),
                                new ItemData( "weapon.mod.silencer", 1, 1, 15f ),
                                new ItemData( "weapon.mod.flashlight", 1, 1, 15f ),
                                new ItemData( "weapon.mod.lasersight", 1, 1, 15f ),
                                new ItemData( "weapon.mod.small.scope", 1, 1, 15f )
                            }
                        },
                        {
                            "assets/prefabs/misc/supply drop/supply_drop.prefab",
                            new List<ItemData>()
                            {
                                new ItemData( "cctv.camera", 1, 1, 8f ),
                                new ItemData( "targeting.computer", 1, 1, 8f ),
                                new ItemData( "explosive.timed", 2, 2, 15f ),
                                new ItemData( "smg.2", 1, 1, 2f ),
                                new ItemData( "smg.mp5", 1, 1, 1f ),
                                new ItemData( "smg.thompson", 1, 1, 2.5f ),
                                new ItemData( "lmg.m249", 1, 1, 5f ),
                                new ItemData( "pistol.m92", 1, 1, 2f ),
                                new ItemData( "rifle.ak", 1, 1, 1f ),
                                new ItemData( "rifle.bolt", 1, 1, 1f ),
                                new ItemData( "rifle.lr300", 1, 1, 0.5f ),
                                new ItemData( "ammo.pistol", 15, 200, 15f ),
                                new ItemData( "ammo.pistol.fire", 80, 80, 10f ),
                                new ItemData( "ammo.pistol.hv", 60, 60, 10f ),
                                new ItemData( "ammo.rifle", 8, 240, 20f ),
                                new ItemData( "ammo.rifle.incendiary", 120, 120, 10f ),
                                new ItemData( "ammo.rifle.explosive", 60, 60, 10f ),
                                new ItemData( "ammo.rifle.hv", 80, 80, 10f ),
                                new ItemData( "ammo.rocket.basic", 3, 3, 15f ),
                                new ItemData( "ammo.rocket.fire", 6, 6, 10f ),
                                new ItemData( "ammo.rocket.hv", 4, 4, 10f ),
                                new ItemData( "weapon.mod.holosight", 1, 1, 15f ),
                                new ItemData( "weapon.mod.silencer", 1, 1, 15f ),
                                new ItemData( "weapon.mod.flashlight", 1, 1, 15f ),
                                new ItemData( "weapon.mod.lasersight", 1, 1, 15f ),
                                new ItemData( "weapon.mod.small.scope", 1, 1, 15f )
                            }
                        }
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<LootControlConfig>();

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = LootControlConfig.DefaultConfig();

        protected override void SaveConfig() => Config.WriteObject(config);

    }
}