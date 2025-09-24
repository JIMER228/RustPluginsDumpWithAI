// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System.Collections.Generic;
using System.Linq;
using Facepunch.Models.Database;
using Newtonsoft.Json;
using Rust.Ai.HTN.ScientistJunkpile;
using Random = System.Random;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("CustomDropChanger", "https://discord.gg/dNGbxafuJn", "1.2.3")]
    [Description("Позволяет добавить лут во все ящики, приобретено на whiteplugins.ru")]
    class CustomDropChanger : RustPlugin
    {
        #region Поля

        private List<string> containerNames = new List<string>
        {
            "crate_basic",
            "crate_elite",
            "crate_mine",
            "crate_tools",
            "crate_normal",
            "crate_normal_2",
            "crate_normal_2_food",
            "crate_normal_2_medical",
            "crate_underwater_advanced",
            "crate_underwater_basic",
            "foodbox",
            "loot_barrel_1",
            "loot_barrel_2",
            "loot-barrel-1",
            "loot-barrel-2",
            "loot_trash",
            "minecart",
            "bradley_crate",
            "oil_barrel",
            "heli_crate",
            "codelockedhackablecrate",
            "supply_drop",
            "trash-pile-1",
            "presentdrop"
        };
		private List<string> waterItems = new List<string>
        {
            "smallwaterbottle",
            "waterjug",
            "botabag"
        };

        public double GetRandomNumber(double minimum, double maximum)
        { 
            return rnd.NextDouble() * (maximum - minimum) + minimum;
        }
        private Dictionary<uint, int> conditionPendingList = new Dictionary<uint, int>();
        private Dictionary<ItemContainer, List<Item>> spawnedLoot = new Dictionary<ItemContainer, List<Item>>();
        private List<LootContainer> affectedContainers = new List<LootContainer>();
        private bool isReady = false;
        private static Random rnd = new Random();
        private Dictionary<ulong,string> TimedNpcPrefab = new Dictionary<ulong,string>(); 

        #endregion

        #region Конфиг

        private NewDropItemConfig config;

        private class ItemDropConfig
        {
            [JsonProperty("Название предмета (ShortName)")]
            public string ItemShortName;

            [JsonProperty("Минимальное количество предмета")]
            public int MinCount;

            [JsonProperty("Максимальное количество предмета")]
            public int MaxCount;

            [JsonProperty("Шанс выпадения предмета (0 - отключить)")]
            public double Chance;

            [JsonProperty("Это чертеж?")] public bool IsBluePrint;

            [JsonProperty("Имя предмета (оставьте пустым для стандартного)")]
            public string Name;

            [JsonProperty("Описание предмета (оставьте пустым для стандартного)")]
            public string Description;

            [JsonProperty("ID Скина (0 - стандартный)")]
            public ulong Skin;

            [JsonProperty("Целостность предмета в % от 1 до 100 (0 - стандартная), например: 20")]
            public int Condition;
        }

        private class NewDropItemConfig
        {
            
            [JsonProperty("Добавляем лут в Контейнеры?")]
            public bool EnableLoot { get; set; }
            
            [JsonProperty("Добавляем лут в Roaming Scientists?")]
            public bool EnableScientistJunkLoot { get; set; }
            
            [JsonProperty("Добавляем лут в Military Tunnel Scientists?")]
            public bool EnableScientistTunelLoot { get; set; }
            
            [JsonProperty("Добавляем лут в Oil Rig Scientists?")]
            public bool EnableScientistOilLoot { get; set; }

            [JsonProperty("Добавляем лут в Outpost Scientists?")]
            public bool EnableScientistPeaceLoot { get; set; }
            
            [JsonProperty("Добавляем лут в Chinook 47 Scientist?")]
            public bool EnableScientistChenookLoot { get; set; }
            
            [JsonProperty("Добавляем лут в Heavy Scientists?")]
            public bool EnableHeavyLoot { get; set; }
            
            [JsonProperty("Добавляем лут в Murderers?")]
            public bool EnableMurdererLoot { get; set; }

            [JsonProperty("Оставить стандартный лут в контейнерах?")]
            public bool EnableStandartLoot { get; set; }

            [JsonProperty("Оставить стандартный лут в Roaming Scientists?")]
            public bool EnableStandartScientistJunkLoot { get; set; }
            
            [JsonProperty("Оставить стандартный лут в Military Tunnel Scientists?")]
            public bool EnableStandartScientistTunelLoot { get; set; }
            
            [JsonProperty("Оставить стандартный лут в Oil Rig Scientists?")]
            public bool EnableStandartScientistOilLoot { get; set; }
            
            [JsonProperty("Оставить стандартный лут в Outpost Scientists?")]
            public bool EnableStandartScientistPeaceLoot { get; set; }
            
            [JsonProperty("Оставить стандартный лут в Chinook 47 Scientist?")]
            public bool EnableStandartScientistChenookLoot { get; set; }
            
            [JsonProperty("Оставить стандартный лут в Heavy Scientists?")]
            public bool EnableStandartHeavyLoot { get; set; }
            
            [JsonProperty("Оставить стандартный лут в Murderes?")]
            public bool EnableStandartMurdererLoot { get; set; }

            [JsonProperty("Список лута Roaming Scientists:")] 
            public List<ItemDropConfig> ScientistJunkLootSettings { get; set; }
            
            [JsonProperty("Список лута Military Tunnel Scientists:")] 
            public List<ItemDropConfig> ScientistTunelLootSettings { get; set; }
            
            [JsonProperty("Список лута Oil Rig Scientists:")] 
            public List<ItemDropConfig> ScientistOilLootSettings { get; set; }
            
            [JsonProperty("Список лута Outpost Scientists:")] 
            public List<ItemDropConfig> ScientistPeaceLootSettings { get; set; }
            
            [JsonProperty("Список лута Chinook 47 Scientist:")] 
            public List<ItemDropConfig> ScientistChenookLootSettings { get; set; }
            
            [JsonProperty("Список лута Heavy Scientists:")]
            public List<ItemDropConfig> HeavyLootSettings { get; set; }
            
            [JsonProperty("Список лута Murderes:")]
            public List<ItemDropConfig> MurdererLootSettings { get; set; }

            [JsonProperty("Настройка контейнеров:")]
            public Dictionary<string, List<ItemDropConfig>> ChestSettings { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            var chestSettings = new Dictionary<string, List<ItemDropConfig>>();

            foreach (var container in containerNames)
            {
                chestSettings.Add(container,
                    new List<ItemDropConfig>
                    {
                        new ItemDropConfig
                        {
                            MinCount = 1,
                            MaxCount = 2,
                            Chance = 40.0,
                            ItemShortName = "researchpaper",
                            IsBluePrint = false,
                            Name = "",
                            Description = "",
                            Skin = 0,
                            Condition = 0
                        }
                    });
            }

            config = new NewDropItemConfig
            {
                EnableScientistJunkLoot = true,
                EnableScientistTunelLoot = true,
                EnableScientistOilLoot = true,
                EnableScientistPeaceLoot = true,
                EnableScientistChenookLoot = true,
                EnableMurdererLoot = true,
                EnableHeavyLoot = true,
                EnableStandartLoot = true,
                EnableStandartScientistChenookLoot = true,
                EnableStandartScientistJunkLoot = true,
                EnableStandartScientistTunelLoot = true,
                EnableStandartScientistOilLoot = true,
                EnableStandartScientistPeaceLoot = true,
                ScientistJunkLootSettings = new List<ItemDropConfig>
                {
                    new ItemDropConfig
                    {
                        MinCount = 1,
                        MaxCount = 2,
                        Chance = 40.0,
                        ItemShortName = "researchpaper",
                        IsBluePrint = false,
                        Name = "",
                        Description = "",
                        Skin = 0,
                        Condition = 0
                    }
                },
                ScientistTunelLootSettings = new List<ItemDropConfig>
                {
                    new ItemDropConfig
                    {
                        MinCount = 1,
                        MaxCount = 2,
                        Chance = 40.0,
                        ItemShortName = "researchpaper",
                        IsBluePrint = false,
                        Name = "",
                        Description = "",
                        Skin = 0,
                        Condition = 0
                    }
                },
                ScientistOilLootSettings = new List<ItemDropConfig>
                {
                    new ItemDropConfig
                    {
                        MinCount = 1,
                        MaxCount = 2,
                        Chance = 40.0,
                        ItemShortName = "researchpaper",
                        IsBluePrint = false,
                        Name = "",
                        Description = "",
                        Skin = 0,
                        Condition = 0
                    }
                },
                ScientistPeaceLootSettings = new List<ItemDropConfig>
                {
                    new ItemDropConfig
                    {
                        MinCount = 1,
                        MaxCount = 2,
                        Chance = 40.0,
                        ItemShortName = "researchpaper",
                        IsBluePrint = false,
                        Name = "",
                        Description = "",
                        Skin = 0,
                        Condition = 0
                    }
                },
                
                ScientistChenookLootSettings = new List<ItemDropConfig>
                {
                    new ItemDropConfig
                    {
                        MinCount = 1,
                        MaxCount = 2,
                        Chance = 40.0,
                        ItemShortName = "researchpaper",
                        IsBluePrint = false,
                        Name = "",
                        Description = "",
                        Skin = 0,
                        Condition = 0
                    }
                },
                MurdererLootSettings = new List<ItemDropConfig>
                {
                    new ItemDropConfig
                    {
                        MinCount = 1,
                        MaxCount = 2,
                        Chance = 40.0,
                        ItemShortName = "researchpaper",
                        IsBluePrint = false,
                        Name = "",
                        Description = "",
                        Skin = 0,
                        Condition = 0
                    }
                },
                HeavyLootSettings = new List<ItemDropConfig>
                {
                    new ItemDropConfig
                    {
                        MinCount = 1,
                        MaxCount = 2,
                        Chance = 40.0,
                        ItemShortName = "researchpaper",
                        IsBluePrint = false,
                        Name = "",
                        Description = "",
                        Skin = 0,
                        Condition = 0
                    }
                },
                ChestSettings = chestSettings
            };
            SaveConfig();
            PrintWarning("Creating default config");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            config = Config.ReadObject<NewDropItemConfig>();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Загрузка и выгрузка

        private void OnServerInitialized()
        {
            var containers = BaseNetworkable.serverEntities.OfType<LootContainer>();
            var count = 0;
            isReady = true;
            containers.ToList().ForEach(x =>
            {
                if (config.ChestSettings.Keys.Contains(x.ShortPrefabName))
                {
                    ProcessContainer(x);
                    count++;
                }
            });

            PrintWarning(
                $"Обновлено {count} контейнеров. Спасибо за приобретение плагина на https://whiteplugins.ru. Использование с других сайтов, не гарантирует корректную работу");
        }

        void Unload()
        {
            spawnedLoot?.Clear();
            affectedContainers?.ForEach(x => x.SpawnLoot());
        }

        #endregion

        #region Логика

        private List<ItemDropConfig> GetItemListByChest(string chestname)
        {
            if (!config.ChestSettings.ContainsKey(chestname))
            {
                PrintWarning($"Ящик с именем '{chestname}' не найден в конфиге!");
                return new List<ItemDropConfig>();
            }

            return config.ChestSettings[chestname];
        }

        private void AddToContainer(ItemDropConfig item, ItemContainer container)
        {

            var amount = rnd.Next(item.MinCount, item.MaxCount);
            var newItem = item.IsBluePrint
                ? ItemManager.CreateByName("blueprintbase")
                : ItemManager.CreateByName(item.ItemShortName, amount, item.Skin);
            var skins = {DarkPluginsID};

            if (newItem == null)
            {
                PrintError($"Предмет {item.ItemShortName} не найден!");
                return;
            }
        
            if (item.IsBluePrint)
            {    
                var bpItemDef = ItemManager.FindItemDefinition(item.ItemShortName);

                if (bpItemDef == null)
                {
                    PrintError($"Предмет {item.ItemShortName} для создания чертежа не найден!");
                    return;
                }

                newItem.blueprintTarget = bpItemDef.itemid;
            }
            if (item.Name != "") newItem.name = item.Name;
			if (waterItems.Contains(newItem.info.shortname))
            {
                
                var water = ItemManager.CreateByName("water", newItem.contents.maxStackSize);
                water.MoveToContainer(newItem.contents, -1, false);
            }

            if (!spawnedLoot.ContainsKey(container)) spawnedLoot.Add(container, new List<Item>());
            spawnedLoot[container].Add(newItem);

            newItem.MoveToContainer(container, -1, false);

            if (!item.IsBluePrint && item.Condition != 0)
                NextFrame(() => newItem.condition = newItem.info.condition.max / 100 * item.Condition);
        }

        private void CheckChanse()
        {
            foreach (var finder in config.ChestSettings)
            {
                if(finder.Key.Length >= 50)
                {
                    PrintError("Error Check");
                }
            }
        }
        private void SetCondition(Item item, int condition, bool isBluePrint)
        {
            if (!isBluePrint && (int) condition > 0)
            {
                item.condition = (int) item.info.condition.max / 100 * condition;
            }
        }

        private void ProcessScientistJunk(ItemContainer container)
        {
            NextTick(() =>
            {
                if (!config.EnableScientistJunkLoot) return;

                if (!config.EnableStandartScientistJunkLoot)
                {
                    container.Clear();

                    ItemManager.DoRemoves();
                }

                foreach (var item in config.ScientistJunkLootSettings)
                {
                    if (GetRandomNumber(1.0, 100.0) <= item.Chance)
                        AddToContainer(item, container);
                }

                if (container.itemList.Count <= 0 && !config.EnableStandartScientistJunkLoot)
                    AddToContainer(config.ScientistJunkLootSettings.GetRandom(), container);
            });
        }
        
        private void ProcessScientistTunel(ItemContainer container)
        {
            NextTick(() =>
            {
                if (!config.EnableScientistTunelLoot) return;

                if (!config.EnableStandartScientistTunelLoot)
                {
                    container.Clear();

                    ItemManager.DoRemoves();
                }

                foreach (var item in config.ScientistTunelLootSettings)
                {
                    if (GetRandomNumber(1.0, 100.0) <= item.Chance)
                        AddToContainer(item, container);
                }

                if (container.itemList.Count <= 0 && !config.EnableStandartScientistTunelLoot)
                    AddToContainer(config.ScientistTunelLootSettings.GetRandom(), container);
            });
        }
        
        private void ProcessScientistOil(ItemContainer container)
        {
            NextTick(() =>
            {
                if (!config.EnableScientistOilLoot) return;

                if (!config.EnableStandartScientistOilLoot)
                {
                    container.Clear();

                    ItemManager.DoRemoves();
                }

                foreach (var item in config.ScientistOilLootSettings)
                {
                    if (GetRandomNumber(1.0, 100.0) <= item.Chance)
                        AddToContainer(item, container);
                }

                if (container.itemList.Count <= 0 && !config.EnableStandartScientistOilLoot)
                    AddToContainer(config.ScientistOilLootSettings.GetRandom(), container);
            });
        }
        
        private void ProcessScientistPeace(ItemContainer container)
        {
            NextTick(() =>
            {
                if (!config.EnableScientistPeaceLoot) return;

                if (!config.EnableStandartScientistPeaceLoot)
                {
                    container.Clear();

                    ItemManager.DoRemoves();
                }

                foreach (var item in config.ScientistPeaceLootSettings)
                {
                    if (GetRandomNumber(1.0, 100.0) <= item.Chance)
                        AddToContainer(item, container);
                }

                if (container.itemList.Count <= 0 && !config.EnableStandartScientistPeaceLoot)
                    AddToContainer(config.ScientistPeaceLootSettings.GetRandom(), container);
            });
        }
        private void ProcessScientistChenook(ItemContainer container)
        {
            NextTick(() =>
            {
                if (!config.EnableScientistChenookLoot) return;

                if (!config.EnableStandartScientistChenookLoot)
                {
                    container.Clear();

                    ItemManager.DoRemoves();
                }

                foreach (var item in config.ScientistChenookLootSettings)
                {
                    if (GetRandomNumber(1.0, 100.0) <= item.Chance)
                        AddToContainer(item, container);
                }

                if (container.itemList.Count <= 0 && !config.EnableStandartScientistChenookLoot)
                    AddToContainer(config.ScientistChenookLootSettings.GetRandom(), container);
            });
        }
        private void ProcessMurderer(ItemContainer container)
        {
            NextTick(() =>
            {
                if (!config.EnableMurdererLoot) return;

                if (!config.EnableStandartMurdererLoot)
                {
                    container.Clear();

                    ItemManager.DoRemoves();
                }

                foreach (var item in config.MurdererLootSettings)
                {
                    if (GetRandomNumber(1.0, 100.0) <= item.Chance)
                        AddToContainer(item, container);
                }

                if (container.itemList.Count <= 0 && !config.EnableStandartMurdererLoot)
                    AddToContainer(config.MurdererLootSettings.GetRandom(), container);
            });
        }
        
        private void ProcessHeavy(ItemContainer container)
        {
            NextTick(() =>
            {
                if (!config.EnableHeavyLoot) return;

                if (!config.EnableStandartHeavyLoot)
                {
                    container.Clear();

                    ItemManager.DoRemoves();
                }

                foreach (var item in config.HeavyLootSettings)
                {
                    if (GetRandomNumber(1.0, 100.0) <= item.Chance)
                        AddToContainer(item, container);
                }

                if (container.itemList.Count <= 0 && !config.EnableStandartHeavyLoot)
                    AddToContainer(config.HeavyLootSettings.GetRandom(), container);
            });
        }

        private void ProcessContainer(LootContainer container)
        {
            NextTick(() =>
            {
            if (!config.EnableLoot) return;
            if (container == null || container.inventory == null) return;
            if (!affectedContainers.Contains(container)) affectedContainers.Add(container);
            if (!config.EnableStandartLoot)
            {
                container.inventory.itemList.Clear();
                ItemManager.DoRemoves();
            }

            foreach (var item in GetItemListByChest(container.ShortPrefabName))
            {
                if (GetRandomNumber(1.0, 100.0) <= item.Chance)
                    AddToContainer(item, container.inventory);
            }

            if (container.inventory.itemList.Count <= 0 && !config.EnableStandartLoot)
                AddToContainer(GetItemListByChest(container.ShortPrefabName).GetRandom(), container.inventory);
            });
        }

        #endregion

        #region Хуки Oxide

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {

            if (entity is ScientistNPC)
            {
                var heavy = entity as ScientistNPC;
                if (heavy.ShortPrefabName == "heavyscientist") TimedNpcPrefab.Add(heavy.userID,heavy.ShortPrefabName);
                if(heavy.ShortPrefabName == "scientistnpc") TimedNpcPrefab.Add(heavy.userID,heavy.ShortPrefabName);
            }
            else if (entity is Scientist)
            {
                var scnpc = entity as Scientist;
                  if(scnpc.ShortPrefabName == "scientist_gunner") TimedNpcPrefab.Add(scnpc.userID,scnpc.ShortPrefabName);
                  if(scnpc.ShortPrefabName == "scientistpeacekeeper") TimedNpcPrefab.Add(scnpc.userID,scnpc.ShortPrefabName);
            }

            if (entity is HTNPlayer)
            {
                var HTM = entity as HTNPlayer;
                if (HTM.ShortPrefabName == "scientist_full_any" || HTM.ShortPrefabName == "scientist_full_lr30" ||
                    HTM.ShortPrefabName == "scientist_full_mp5" || HTM.ShortPrefabName == "scientist_full_pistol" ||
                    HTM.ShortPrefabName == "scientist_full_shotgun")
                {
                    TimedNpcPrefab.Add(HTM.userID,"ScientistTunel");
                }
                if(HTM.ShortPrefabName == "scientist_junkpile_pistol") TimedNpcPrefab.Add(HTM.userID,"ScientistJunkPile");

            }

        }
        private void OnLootSpawn(LootContainer lootContainer)
        {
            if (!isReady || lootContainer == null) return;
            if (!config.ChestSettings.Keys.Contains(lootContainer.ShortPrefabName)) return;

            ProcessContainer(lootContainer);
        }

        private void OnEntitySpawned(BaseNetworkable entity) {

                if (entity == null || !(entity is NPCPlayerCorpse)) return;

                var npc = entity as NPCPlayerCorpse;
                var inv = npc.containers[0];
                if (npc == null || npc.containers.Length == 0) return;
                if (TimedNpcPrefab.ContainsKey(npc.playerSteamID))
                {

                    if (TimedNpcPrefab[npc.playerSteamID] == "ScientistJunkPile")
                    {
                        ProcessScientistJunk(inv);
                        TimedNpcPrefab.Remove(npc.playerSteamID);
                        return;
                    }

                    if (TimedNpcPrefab[npc.playerSteamID] == "ScientistTunel")
                    {
                        ProcessScientistTunel(inv);
                        TimedNpcPrefab.Remove(npc.playerSteamID);
                        return;
                    }

                    if (TimedNpcPrefab[npc.playerSteamID] == "scientistnpc")
                    {
                        ProcessScientistOil(inv);
                        TimedNpcPrefab.Remove(npc.playerSteamID);
                        return;
                    }
                    if (TimedNpcPrefab[npc.playerSteamID] == "scientistpeacekeeper")
                    {
                        ProcessScientistPeace(inv);
                        TimedNpcPrefab.Remove(npc.playerSteamID);
                        return;
                    }

                    if (TimedNpcPrefab[npc.playerSteamID] == "scientist_gunner")
                    {
                        ProcessScientistChenook(inv);
                        TimedNpcPrefab.Remove(npc.playerSteamID);
                        return;
                    }

                    if (TimedNpcPrefab[npc.playerSteamID] == "heavyscientist")
                    {
                        ProcessHeavy(inv);
                        TimedNpcPrefab.Remove(npc.playerSteamID);
                        return;
                    }
                }
                if(npc.ShortPrefabName == "murderer_corpse") ProcessMurderer(inv);
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null || !(entity is LootContainer)) return;
            var cont = entity as LootContainer;
            if (affectedContainers.Contains(cont))
                affectedContainers.Remove(cont);
        }

        #endregion
    }
