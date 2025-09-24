// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Linq;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using UnityEngine; 
using System.Collections;
using Oxide.Game.Rust.Cui;
using Facepunch;


//  V1.2.4 update notes.
//  Added AddToBotlist null checking. Laodu
//  Fixed unwrappables getting loot despite loot table enabled : false
//  Added new Jungle npc type.

namespace Oxide.Plugins
{
    [Info("CustomLoot", "Steenamaroo", "1.2.4", ResourceId = 16)]
    [Description("Custom loot table plugin for NPC corpses, crates, and containers.")]   
     
    public class CustomLoot : RustPlugin
    { 
        #region Declarations   
        [PluginReference] Plugin BotReSpawn;   
         
        bool loaded;
        static CustomLoot cl;
        List<string> LootTableNames = new List<string>();
        public System.Random random = new System.Random();
        public ItemDefinition water = ItemManager.FindItemDefinition("water");
        Dictionary<string, LootTable> LootTables = new Dictionary<string, LootTable>();
        Dictionary<string, Dictionary<int, string>> BalancedCategories = new Dictionary<string, Dictionary<int, string>>();
        Dictionary<string, Dictionary<string, Dictionary<int, KeyValuePair<string, lootData>>>> BalancedItems = new Dictionary<string, Dictionary<string, Dictionary<int, KeyValuePair<string, lootData>>>>(); 

        Dictionary<string, List<ulong>> BotReSpawnBots = new Dictionary<string, List<ulong>>();
        public List<LootContainer> AllContainers = new List<LootContainer>();
        public Dictionary<string, Settings> ContainerTypesFromGame = new Dictionary<string, Settings>();
        public Dictionary<string, CategorySettings> GotCategories = new Dictionary<string, CategorySettings>();
        public Dictionary<string, Dictionary<string, lootData>> GotLootTypes = new Dictionary<string, Dictionary<string, lootData>>();  
        public Dictionary<string, List<ulong>> BotTypes = new Dictionary<string, List<ulong>>();

        BackupData backupData;
        #endregion
         
        [ConsoleCommand("Repop")] 
        private void Repop(ConsoleSystem.Arg arg)
        {
            var player = arg?.Connection?.player as BasePlayer;
            if (player == null || IsAuth(player))
                foreach (var group in SpawnHandler.Instance.SpawnGroups.ToList())
                    group.Fill();
        }

        #region Hooks
        void OnServerInitialized()
        {
            BalancedCategories = new Dictionary<string, Dictionary<int, string>>();
            BalancedItems = new Dictionary<string, Dictionary<string, Dictionary<int, KeyValuePair<string, lootData>>>>();
            GotCategories = new Dictionary<string, CategorySettings>();
            ContainerTypesFromGame = new Dictionary<string, Settings>();
            GotLootTypes = new Dictionary<string, Dictionary<string, lootData>>();
            BotTypes = new Dictionary<string, List<ulong>>(); 

            List<string> names = new List<string>();
            foreach (var entry in Resources.FindObjectsOfTypeAll<LootContainer>().Where(x => !x.isSpawned))
            { 
                if (entry.PrefabName.Contains("underwater_labs"))
                    names.Add("underwater_labs_" + entry.ShortPrefabName); 
                else
                    names.Add(entry.ShortPrefabName);
            }

            names = names.Distinct().ToList();

            foreach (var entry in names)
                if (!entry.Contains("test") && !entry.Contains("stocking"))
                    ContainerTypesFromGame.Add(entry, new Settings());

            var ItemMods = Resources.FindObjectsOfTypeAll<ItemMod>();

            foreach (var entry in ItemMods.OfType<ItemModUnwrap>().Where(x => x != null))
                ContainerTypesFromGame[entry.name.Replace(".item", "")] = new Settings();

            cl = this;
            if (BotReSpawn)
                RefreshBotReSpawn(); 

            var cats = Enum.GetNames(typeof(ItemCategory)).ToList();
            List<string> exclude = new List<string> { "Search", "Favourite", "All", "Common" };
            backupData = Interface.Oxide.DataFileSystem.ReadObject<BackupData>("CustomLoot/BotReSpawnBackups");
            LoadConfigVariables();
            Interface.Oxide.DataFileSystem.WriteObject("CustomLoot/BotReSpawnBackups", backupData);

            foreach (LootContainer container in BaseNetworkable.serverEntities.OfType<LootContainer>())
            {
                string name = container.PrefabName.Contains("underwater_labs") ? "underwater_labs_" + container.ShortPrefabName : container.ShortPrefabName;
                if (configData.ContainerTypes.ContainsKey(name) && configData.ContainerTypes[name].enabled)
                    ProcessContainer(container, true);
            }

            timer.Once(5f, () => ServerMgr.Instance.StartCoroutine(FillContainers(AllContainers)));
            loaded = true;

            foreach (BasePlayer player in BaseNetworkable.serverEntities.OfType<BasePlayer>())
                if (player is NPCPlayer || player is global::HumanNPC)
                    AddToBotList(player);

            foreach (var cat in cats.Where(cat => !exclude.Contains(cat)))
            {
                GotCategories.Add(cat, new CategorySettings());
                GotLootTypes.Add(cat, new Dictionary<string, lootData>());
            }

            foreach (var lT in LootTableNames.ToList())
                if (CreateLootTable(lT))
                {
                    BalanceCategories(lT);
                    BalanceItems(lT);
                }
        }
        #endregion

        #region ContainerTypes
        void ProcessContainer(LootContainer container, bool start) 
        {
            if (container.shouldRefreshContents)
                container.Invoke(new Action(container.SpawnLoot), UnityEngine.Random.Range(container.minSecondsBetweenRefresh, container.maxSecondsBetweenRefresh));

            if (container?.net?.ID == null)
                return;
            if (Interface.CallHook("OnCustomLootContainer", container.net.ID) != null)
                return;
            if (configData.GlobalSettings.IgnoreContainersWithOwnerID && container.OwnerID != 0 && !(container is SupplyDrop))
                return;
            if (start)
                AllContainers.Add(container);
            else
                PopulateContainer(container);
        }

        void PopulateContainer(LootContainer container) 
        {
            if (container == null)
                return; 
            string name = container.PrefabName.Contains("underwater_labs") ? "underwater_labs_" + container.ShortPrefabName : container.ShortPrefabName;
            Settings settings = configData.ContainerTypes[name];

            timer.Once(1f, () => 
            {
                if (container == null)
                    return;
                container.inventory.capacity = 48; 
                container.onlyAcceptCategory = ItemCategory.All;
                container.SendNetworkUpdateImmediate();
                
                var lootObj = MakeLoot(name, container); 
                if (lootObj == null)
                    return;

                if (settings.ClearContainerFirst) 
                    container.inventory.Clear();

                bool crate = container.ShortPrefabName.Contains("hackable"); 
                if (crate)
                    container.inventory.onItemAddedRemoved = null; 

                foreach (var item in lootObj) 
                    if (!item.MoveToContainer(container.inventory, -1, true))
                        item.Remove();
                    else
                    {
                        var vessel = item?.GetHeldEntity() as BaseLiquidVessel; 
                        if (vessel != null && vessel.hasLid)
                        {
                            int percentage = Mathf.Clamp(settings.WaterPreFillPercent, 0, 100);
                            var stack = item?.info?.GetComponent<ItemModContainer>()?.maxStackSize;
                            if (percentage == 0 || stack == null)
                                continue;

                            Item waterItem = ItemManager.Create(water, (int)(stack / 100f * percentage), 0);
                            if (!waterItem.MoveToContainer(item.contents, -1, true))
                                waterItem.Remove();
                        }
                    }
                if (crate)
                    timer.Once(1f, () => container.inventory.onItemAddedRemoved = new Action<Item, bool>(container.OnItemAddedOrRemoved));

                Pool.FreeUnmanaged(ref lootObj); 
            });
        }

        object OnLootSpawn(LootContainer container)
        {
            if (!loaded || container == null || configData == null) 
                return null;

            string name = container.PrefabName.Contains("underwater_labs") ? "underwater_labs_" + container.ShortPrefabName : container.ShortPrefabName;

            if (configData.ContainerTypes.ContainsKey(name) && configData.ContainerTypes[name].enabled)
            {
                timer.Once(0.3f, () =>
                {
                    if (container == null)
                        return;

                    ProcessContainer(container, false);
                });
                return true;
            }
            return null;
        }

        object OnItemUnwrap(Item item, BasePlayer player, ItemModUnwrap unwrap)
        {
            if (item == null || player == null || unwrap == null || configData == null)
                return null;

            string name = unwrap.name.Replace(".item", "");
            if (!configData.ContainerTypes.ContainsKey(name) || !configData.ContainerTypes[name].enabled)
                return null;

            var lootObj = MakeLoot(name, null);
            if (lootObj == null)
                return null;

            item.UseItem(1);
            int num = UnityEngine.Random.Range(unwrap.minTries, unwrap.maxTries + 1);

            foreach (var entry in lootObj)
                if (!entry.MoveToContainer(player.inventory.containerMain))
                    entry.Drop(player.GetDropPosition(), player.GetDropVelocity(), new Quaternion()); 

            if (unwrap.successEffect.isValid)
                Effect.server.Run(unwrap.successEffect.resourcePath, player.eyes.position, new Vector3(), null, false, null);

            return true;
        }

        public IEnumerator FillContainers(List<LootContainer> AllContainers)
        {
            foreach (var container in AllContainers.ToList())
            {
                cl.PopulateContainer(container);
                yield return new WaitForEndOfFrame();
            }
            cl.PrintWarning("Finished populating all containers.");
            yield return null;
        }

        #endregion ContainerTypes     

        #region Methods
        bool IsAuth(BasePlayer player) => player?.net?.connection != null && player.net.connection.authLevel == 2;

        Settings GetSettings(object record)
        {
            var outRecord = JsonConvert.SerializeObject(record);
            var response = JsonConvert.DeserializeObject<Settings>(outRecord);

            response.enabled = true;
            return response;
        }
        #endregion

        #region BotHandling
        void OnEntitySpawned(LootableCorpse corpse)
        {
            if (!loaded) return;

            if (corpse == null)
                return;

            ulong ID = corpse.playerSteamID;

            foreach (var entry in BotTypes.Where(entry => entry.Value.Contains(corpse.playerSteamID)))
            {
                timer.Once(0.3f, () =>
                {
                    if (corpse == null)
                        return;

                    var lootObj = MakeLoot(entry.Key, null);
                    if (lootObj == null)
                        return;

                    if (configData.CorpseTypes[entry.Key].ClearContainerFirst)
                        corpse.containers[0].Clear();

                    foreach (var item in lootObj)
                    {
                        if (!item.MoveToContainer(corpse.containers[0], -1, true))
                            item.Remove();
                        else
                        {
                            var vessel = item?.GetHeldEntity() as BaseLiquidVessel;
                            if (vessel != null && vessel.hasLid)
                            {
                                int percentage = Mathf.Clamp(configData.CorpseTypes[entry.Key].WaterPreFillPercent, 0, 100);
                                var stack = item?.info?.GetComponent<ItemModContainer>()?.maxStackSize;
                                if (percentage == 0 || stack == null)
                                    continue;

                                Item waterItem = ItemManager.Create(water, (int)(stack / 100f * percentage), 0);
                                if (!waterItem.MoveToContainer(item.contents, -1, true))
                                    waterItem.Remove();
                            }
                        }
                    }
                    Pool.FreeUnmanaged<Item>(ref lootObj);
                    timer.Once(0.2f, () => RemoveFromMemory(ID));
                });
            }
        }
         
        void OnEntitySpawned(NPCPlayer player)  
        {
            if (!loaded)
                return;

            if (player == null || player.IsDestroyed)
                return;
            if (BotReSpawn)
            {
                NextTick(() =>
                {
                    if (player != null)
                    {
                        RefreshBotReSpawn();
                        AddToBotList(player);
                    }
                });
            }
            else
                AddToBotList(player);
        }

        void RefreshBotReSpawn() => BotReSpawnBots = (Dictionary<string, List<ulong>>)BotReSpawn?.Call("BotReSpawnBots");

        void AddToBotList(BasePlayer player) 
        {
            if (player?.net?.ID == null || player.ShortPrefabName == null)
                return;

            if (Interface.CallHook("OnCustomLootNPC", player.net.ID) != null)
                return;

            if (player.Categorize() == "Zombie") 
            {
                ProcessNpc(player, "ZombieHorde");
                return;
            }

            if (BotReSpawn && BotReSpawnBots != null)
            { 
                foreach (var monument in BotReSpawnBots.Where(monument => monument.Value.Contains(player.userID)))
                {
                    AddBotReSpawn(player, monument.Key);
                    return;
                }
            }

            // Check for spawnpopulations
            var instance = player?.GetComponent<SpawnPointInstance>()?.parentSpawnPoint;
            if (instance != null)
            {
                var name = instance?.GetComponentInParent<PrefabParameters>()?.ToString();
                if (name != null)
                {
                    foreach (var n in names)
                        if (name.Contains(n.Key))
                        {
                            ProcessNpc(player, (n.Key == "nuclear_missile_silo" && player.ShortPrefabName.Contains("nvg") ? "NuclearMissileSiloUnderGround" : n.Value));
                            return;
                        }
                }
            }

            foreach (var entry in names)
                if (player.ShortPrefabName == entry.Key)
                {
                    ProcessNpc(player, entry.Value);
                    return;
                }

            foreach (var entry in names)
                if (player.ShortPrefabName.Contains(entry.Key))
                {
                    ProcessNpc(player, entry.Value); 
                    return;
                }

            //Puts($"Prefab name {player.PrefabName}");
            //foreach (var p in BasePlayer.activePlayerList)
            //    p.SendConsoleCommand("ddraw.line", 5f, Color.blue, player.transform.position, player.transform.position + new Vector3(0, 100, 0));
        }

        Dictionary<string, string> names = new Dictionary<string, string>() 
        {
            {"oilrig", "OilRig"},
            {"excavator", "Excavator"}, 
            {"peacekeeper", "CompoundScientist"},
            {"bandit_guard", "BanditTown"},
            {"_ch47_gunner", "MountedScientist"},
            {"junkpile", "JunkPileScientist"},
            {"scarecrow_dungeon", "DungeonScarecrow"},
            {"scarecrow", "ScareCrow"},
            {"military_tunnel", "MilitaryTunnelScientist"},
            {"scientist_full", "MilitaryTunnelScientist"},
            {"scientist_turret", "CargoShip"},
            {"scientistnpc_cargo", "CargoShip"},
            {"scientist_astar", "CargoShip"},
            {"scientistnpc_bradley", "APCScientist" },
            {"scientistnpc_bradley_heavy", "APCScientistHeavy" },
            {"scientistnpc_heavy", "HeavyScientist"},
            {"scientistnpc_outbreak", "JungleScientist" },
            {"tunneldweller", "TunnelDweller"},
            {"underwaterdweller" , "UnderwaterDweller"},
            {"trainyard" , "Trainyard"},
            {"airfield" , "Airfield"},
            {"scientistnpc_roamtethered", "DesertScientist"},
            {"arctic_research_base", "ArcticResearchBase"},
            {"launch_site", "LaunchSite"},
            {"nuclear_missile_silo", "NuclearMissileSiloAboveGround" },
            {"gingerbread", "Gingerbread"},
            {"ZombieHorde", "ZombieHorde"}
        };

        void ProcessNpc(BasePlayer player, string NPCType) 
        {
            var record = configData.CorpseTypes[NPCType]; 
            if (record == null)
                return;

            if (record.enabled)
                BotTypes[NPCType].Add(player.userID);
        }

        void AddBotReSpawn(BasePlayer npc, string monument)  
        {
            if (configData.GlobalSettings.corpseTypePerBotReSpawnProfile)
            {
                if (!BotTypes.ContainsKey("BotReSpawn-" + monument))
                    OnServerInitialized();

                if (configData.CorpseTypes["BotReSpawn-" + monument].enabled)
                    BotTypes["BotReSpawn-" + monument].Add(npc.userID);
            }
            else if (configData.CorpseTypes["BotReSpawn"].enabled)
                BotTypes["BotReSpawn"].Add(npc.userID);
        }

        void RemoveFromMemory(ulong userID)
        {
            foreach (var botList in BotTypes.Where(botList => botList.Value.Contains(userID)))
            {
                botList.Value.Remove(userID);
                break;
            }
        }
        #endregion

        #region LootHandling
        public void BalanceCategories(string lootTable)
        {
            int counter = 0;
            BalancedCategories.Add(lootTable, new Dictionary<int, string>());
            foreach (var category in LootTables[lootTable].Categories)
            {
                if (!HasValidItem(lootTable, category.Key)) continue;
                for (int i = 0; i < category.Value.probability; i++)
                {
                    BalancedCategories[lootTable].Add(counter, category.Key);
                    counter++;
                }
            }
        }

        public void BalanceItems(string lootTable)
        {
            BalancedItems.Add(lootTable, new Dictionary<string, Dictionary<int, KeyValuePair<string, lootData>>>());
            foreach (var category in LootTables[lootTable].LootTypes.Where(category => LootTables[lootTable].Categories[category.Key].probability > 0))
            {
                int counter = 0;
                if (!HasValidItem(lootTable, category.Key)) continue;
                BalancedItems[lootTable].Add(category.Key, new Dictionary<int, KeyValuePair<string, lootData>>());
                foreach (var entry in category.Value)
                {
                    entry.Value.minStack = Mathf.Max(entry.Value.minStack, 1);
                    entry.Value.maxStack = Mathf.Max(entry.Value.maxStack, 1);
                    for (int i = 0; i < entry.Value.probability; i++)
                    {
                        var blank = new KeyValuePair<string, lootData>(entry.Key, entry.Value);
                        BalancedItems[lootTable][category.Key].Add(counter, blank);
                        counter++;
                    }
                }
            }
        }

        public bool HasValidItem(string lootTable, string category)
        {
            if (LootTables[lootTable].LootTypes[category].Count == 0)
                return false;
            foreach (var item in LootTables[lootTable].LootTypes[category].Where(item => item.Value.probability > 0))
                return true;
            return false;
        }

        private List<Item> MakeLoot(string config, LootContainer container = null)
        { 
            Settings settings = null;
            if (configData.CorpseTypes.ContainsKey(config)) 
                settings = configData.CorpseTypes[config];
            else if (configData.ContainerTypes.ContainsKey(config))
                settings = configData.ContainerTypes[config];
            else if (configData.API.ContainsKey(config))
                settings = GetSettings(configData.API[config]);
            else
            {
                Puts("CustomLoot had a call for a profile that doesn't exist."); 
                Puts($"Adding profile {config} using loottable 'default'.");
                configData.API.Add(config, new BaseSettings());
                SaveConfig(configData);
                settings = GetSettings(configData.API[config]);
            }

            if (settings == null)
                return null;

            string lootTable = settings.lootTable;
            if (!LootTableNames.Contains(lootTable))
                return null;

            int safety = 0;
            int number = settings.maxItems;

            List<Item> newLoot = Pool.Get<List<Item>>();
            if (settings.maxItems > settings.minItems)
                number = random.Next(settings.minItems, settings.maxItems + 1);

            int BPs = 0;
            for (int i = 0; i < number; i++)
            {
                safety++;
                bool duplicate = false;
                if (safety > 50) { break; }
                string category = GetCategory(lootTable);
                if (category == "zero")
                    return null;
                Item item = null;
                bool forced = false;

                if (LootTables[lootTable].AlwaysSpawnList.Count >= i + 1)
                {
                    foreach (var cat in LootTables[lootTable].LootTypes)
                    {
                        if (cat.Value.ContainsKey(LootTables[lootTable].AlwaysSpawnList[i]))
                        {
                            forced = true;
                            item = GetItem(settings, cat.Key, BPs, LootTables[lootTable].AlwaysSpawnList[i], container, LootTables[lootTable].Spawn_Vanilla_Scrap, LootTables[lootTable].Vanilla_Scrap_Multiplier);
                            break;
                        }
                    }
                }

                if (!forced) 
                    item = GetItem(settings, category, BPs, string.Empty, container, LootTables[lootTable].Spawn_Vanilla_Scrap);
                if (item == null) { i--; continue; }


                foreach (var newItem in newLoot)
                {
                    if (!configData.GlobalSettings.allowDuplicates || newItem.info.shortname == "scrap")
                    {
                        if (newItem.blueprintTarget != 0 && item.blueprintTarget != 0)
                        {
                            if (newItem.blueprintTarget == item.blueprintTarget)
                                duplicate = true;
                            else
                                BPs++;
                        }
                        else if (newItem.info.shortname == item.info.shortname && newItem.name == item.name)
                            duplicate = true;
                    }
                }

                if (duplicate) 
                {
                    item.Remove();
                    i--;
                    continue;
                }
                else
                {
                    if (settings.noGuns)
                    {
                        if (item.GetHeldEntity() as BaseProjectile)
                        {
                            i--; 
                            item.Remove();
                            continue;
                        }
                    }

                    newLoot.Add(item);
                    if (category == "Weapon")
                    {
                        if (settings.gunsWithAmmo && item.blueprintTarget == 0)
                        {
                            string matchedAmmo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine?.ammoType?.shortname;
                            if (matchedAmmo == null || matchedAmmo == string.Empty || !LootTables[lootTable].LootTypes["Ammunition"].ContainsKey(matchedAmmo))
                                continue;

                            lootData ammopath;
                            if (item.info.shortname != "flamethrower")
                            {
                                if (item.info.shortname.Contains("hunting")) 
                                    continue;

                                ammopath = LootTables[lootTable].LootTypes["Ammunition"][matchedAmmo];
                            }
                            else //if (LootTables[lootTable].LootTypes["Resources"].ContainsKey("lowgradefuel"))
                            { 
                                ammopath = LootTables[lootTable].LootTypes["Resources"]["lowgradefuel"];
                                matchedAmmo = "lowgradefuel";
                            }
                            //else
                            //    continue;
                            int stack = random.Next(ammopath.minStack, ammopath.maxStack);

                            if (stack > 10 && configData.GlobalSettings.RoundResourcesStacksDownToNearestTen)
                                stack = (stack / 10) * 10;
                            if (ammopath.probability > 0)
                            {
                                Item ammoItem = ItemManager.CreateByName(matchedAmmo, stack);
                                newLoot.Add(ammoItem);
                                i++;
                            }
                        }
                    }
                }
            }

            if (newLoot.Count > number)
                foreach (var entry in newLoot.Where(entry => entry.info.category != ItemCategory.Weapon && entry.info.category != ItemCategory.Ammunition).ToList())
                {
                    entry.Remove();
                    newLoot.Remove(entry);
                    break;
                }

            return newLoot;
        }

        float GetCondition(int min, int max, float itemMax)
        {
            min = Mathf.Max(1, min);
            max = Mathf.Max(1, max);
            if (min >= max)
            {
                return itemMax / 100f * max;
            }
            return itemMax / 100f * random.Next(min, max);
        }

        public string GetCategory(string lootTable)
        {
            if (!BalancedCategories.ContainsKey(lootTable))
            {
                Puts($"Balanced categories did not contain {lootTable}"); 
                return "zero";
            }

            if (BalancedCategories[lootTable].Count == 0)
                return "zero";
            int catnumber = random.Next(BalancedCategories[lootTable].Count);
            return BalancedCategories[lootTable][catnumber];
        }

        Dictionary<string, Dictionary<string, List<ulong>>> skinref = new Dictionary<string, Dictionary<string, List<ulong>>>();

        [ChatCommand("CustomLoot")] 
        void CustomLootChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
                return;

            var held = player.GetActiveItem();
            if (held == null)
            {
                SendReply(player, "Make sure you're holding an item, with the desired skin, first.");
                SendReply(player, "For consumables, you can use scroll to set them as active without consuming.");
                return;
            }

            if (held.skin == 0)
            {
                SendReply(player, $"Currently held item skin is 0/default.");
                SendReply(player, $"Please hold an item with a custom skin and try again.");
                return;
            }

            if (args.Length != 3)
            {
                SendReply(player, "Please specify the existing loottable (or All), the category, and a custom name to use.");
                SendReply(player, "example - /CustomLoot NPCTable Resources Money"); 
                return;
            }

            if (!LootTableNames.Contains(args[0]) && args[0] != "All")
            {
                SendReply(player, $"No loot table {args[0]} exists.");
                return;
            }
            if (!GotCategories.ContainsKey(args[1]))
            {
                SendReply(player, $"No category {args[1]} exists.");
                return;
            }

            int added = 0;
            foreach (var lt in LootTables)
            {
                if (args[0] == "All" || args[0] == lt.Key)
                {
                    if (lt.Value.LootTypes[args[1]].ContainsKey(args[2]) && lt.Value.LootTypes[args[1]][args[2]].ItemShortname != "")
                        continue;

                    lt.Value.LootTypes[args[1]][args[2]] = new lootData() { item = held.info, ItemShortname = held.info.shortname, ItemText = args[2], skins = new List<ulong>() { held.skin } };
                    Interface.Oxide.DataFileSystem.WriteObject($"CustomLoot/{lt.Key}", lt.Value); 
                    if (args[0] != "All")
                        SendReply(player, $"Item {args[2]} successfully added to {args[1]} for {args[0]}.");
                    added++;
                }
            }

            if (args[0] == "All" && added > 0)
                SendReply(player, $"Item {args[2]} successfully added to {args[1]} for {added} loot tables."); 
        }

        ItemDefinition bpb = ItemManager.FindItemDefinition("blueprintbase");
        public Item GetItem(Settings settings, string category, int BPs, string perma, LootContainer container, bool vanillascrap, int scrapmulti = 1)
        {
            string lootTable = settings.lootTable;
            if (!skinref.ContainsKey(lootTable))
                skinref.Add(lootTable, new Dictionary<string, List<ulong>>());

            var skins = skinref[lootTable]; 

            Item item = new Item(); 
            int chosenItem;
            string name = string.Empty;
            lootData path = null; 

            if (perma != string.Empty)
            {
                name = perma;
                path = LootTables[lootTable].LootTypes[category][perma]; 
            }
            else
            { 
                chosenItem = random.Next(BalancedItems[lootTable][category].Count);
                name = BalancedItems[lootTable][category]?[chosenItem].Key;
                path = BalancedItems[lootTable]?[category]?[chosenItem].Value;
            }
            if (name == string.Empty || path == null)
                return null;
            int stack = path.maxStack;

            if (name == "scrap" && container?.scrapAmount > 0 && vanillascrap)
            {
                stack = container.scrapAmount;
                if (scrapmulti > 1)
                    stack *= scrapmulti;
            }
            else
            {
                if (path.minStack < path.maxStack)
                    stack = random.Next(path.minStack, path.maxStack);
            }
            if (category == "Resources" && stack > 10 && configData.GlobalSettings.RoundResourcesStacksDownToNearestTen) stack = (stack / 10) * 10;

            item = ItemManager.CreateByName(path.ItemShortname != "" ? path.ItemShortname : name, stack, 0);
             
            if (!skins.ContainsKey(name)) 
                skins.Add(name, path.skins);

            if (path.IncludeAllApprovedSkins)
                foreach (var entry in Rust.Workshop.Approved.All)
                {
                    if (entry.Value?.Skinnable == null)
                        continue;
                    if (entry.Value.Skinnable.ItemName == name && !skins[name].Contains(entry.Value.WorkshopdId))
                        skins[name].Add(entry.Value.WorkshopdId);
                }

            if (skins[name].Count > 0)
                item.skin = skins[name][random.Next(skins[name].Count)];

            if (item.info.condition.enabled)
                item.condition = GetCondition(path.MinConditionPercent, path.MaxConditionPercent, item.info.condition.max);

            CategorySettings confPath = LootTables[lootTable].Categories[category];
            ItemDefinition def = ItemManager.FindItemDefinition(item.info.itemid);

            if (confPath.allowBlueprints && random.Next(99) < path.blueprintChancePercent && item.info.Blueprint != null && IsBP(def))
            {
                if (BPs >= settings.MaxBps)
                    return null;

                item.Remove();
                item = ItemManager.Create(bpb, 1, 0uL);
                item.blueprintTarget = def.itemid;
            }

            if (path.ItemShortname != "")
            {
                item.name = name;
                if (path.ItemText != string.Empty)
                    item.text = path.ItemText;
                //item.info.displayName = name;
            }

            if (item == null)
                Puts($"Null-item spawn attempted. Please notify Author - '{name}'"); 
            else
            {
                item.MarkDirty();
                BaseEntity held = item.GetHeldEntity();
                if (held != null)
                {
                    held.skinID = item.skin;
                    held.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                }
            }
            return item;
        }

        bool IsBP(ItemDefinition def) => def?.Blueprint != null && def.Blueprint.isResearchable && !def.Blueprint.defaultBlueprint;
        #endregion

        #region Data
        public class BackupData 
        {
            public Dictionary<string, Settings> BotReSpawnBackups = new Dictionary<string, Settings>();
        }
        public class LootTable
        {
            public bool allowChristmas, allowHalloween, allowKeycards = false;
            public List<string> BlackList = new List<string>();
            public List<string> AlwaysSpawnList = new List<string>();
            public bool Spawn_Vanilla_Scrap = false;
            public int Vanilla_Scrap_Multiplier = 1;
            public Dictionary<string, CategorySettings> Categories = cl.GotCategories.ToDictionary(pair => pair.Key, pair => pair.Value);
            public Dictionary<string, Dictionary<string, lootData>> LootTypes = cl.GotLootTypes.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public class CategorySettings
        {
            public int probability;
            public bool allowBlueprints = true;  
        } 

        public class lootData
        {
            [JsonIgnore] public ItemDefinition item;
            [JsonProperty(Order = 1)] public string ItemShortname = "";
            [JsonProperty(Order = 2)] public string ItemText = "";
            [JsonProperty(Order = 3)] public int probability;
            [JsonProperty(Order = 4)] public int minStack = 1; 
            [JsonProperty(Order = 5)] public int maxStack = 1; 
            [JsonProperty(Order = 6)] public int blueprintChancePercent = 0;
            [JsonProperty(Order = 7)] public List<ulong> skins = new List<ulong>();
            [JsonProperty(Order = 8)] public bool IncludeAllApprovedSkins = false;
            [JsonProperty(Order = 9)] public int MinConditionPercent = 90;
            [JsonProperty(Order = 10)] public int MaxConditionPercent = 100;
             
            public bool ShouldSerializeItemShortname() => ItemShortname != "";
            public bool ShouldSerializeItemText() => ItemShortname != "" || ItemText != "";
            public bool ShouldSerializeblueprintChancePercent() => item != null && cl.IsBP(item);
            public bool ShouldSerializeskins() => ItemShortname != "" || (item != null && (cl.configData.GlobalSettings.AllowSkinsFor.Contains(item.shortname) || cl.configData.GlobalSettings.Show_Skins_For_All_Items || item.HasSkins));
            public bool ShouldSerializeIncludeAllApprovedSkins() => item != null && ( cl.configData.GlobalSettings.Show_Skins_For_All_Items || item.HasSkins);
            public bool ShouldSerializeMaxConditionPercent() => item != null && item.condition.enabled; 
            public bool ShouldSerializeMinConditionPercent() => item != null && item.condition.enabled;
        }
         
        public List<string> DefaultBlackList = new List<string> { "glue", "door.key", "note", "bleach", "ducttape", "blood", "tool.camera", "grenade.smoke", "ammo.rocket.smoke", "battery", "bone.fragments", "blueprintbase", "water.salt", "jackhammer" };

        public bool CreateLootTable(string lootTable) 
        {
            try
            { 
                LootTables[lootTable] = Interface.Oxide.DataFileSystem.ReadObject<LootTable>($"CustomLoot/{lootTable}");
            }
            catch
            {
                Puts($"There is an error in data file {lootTable}.");
                LootTableNames.Remove(lootTable);
                return false;
            } 
            try
            {
                var catPath = string.Empty;
                var path = LootTables[lootTable];
                Dictionary<string, List<string>> itemCheck = new Dictionary<string, List<string>>();
                Dictionary<string, List<string>> toRemove = new Dictionary<string, List<string>>();

                if (path.BlackList.Count == 0)
                    path.BlackList = DefaultBlackList;
                path.BlackList.Sort();

                foreach (ItemDefinition rustItem in ItemManager.itemList)
                {
                    catPath = rustItem.category.ToString();
                    if (!path.LootTypes.ContainsKey(catPath))
                    {
                        Puts($"New item category *{catPath}* added to game. Please notify author.");
                        continue;
                    }

                    if (IsJunk(lootTable, rustItem.shortname) || (!path.allowChristmas && rustItem.shortname.Contains("xmas")) || (!path.allowHalloween && rustItem.shortname.Contains("halloween")) || (!path.allowKeycards && rustItem.shortname.Contains("keycard_")))
                    {
                        path.LootTypes[catPath].Remove(rustItem.shortname);  
                        continue;
                    }

                    if (!itemCheck.ContainsKey(catPath)) 
                        itemCheck.Add(catPath, new List<string>());
                    itemCheck[catPath].Add(rustItem.shortname);

                    if (path.LootTypes[catPath].ContainsKey(rustItem.shortname)) 
                    {
                        path.LootTypes[catPath][rustItem.shortname].item = rustItem;
                        continue;
                    }
                    var stackPath = path.Categories[catPath];
                    path.LootTypes[catPath].Add(rustItem.shortname, new lootData { item = rustItem });
                }

                foreach (var cat in itemCheck)
                    foreach (var item in path.LootTypes[cat.Key].Where(x => x.Value.ItemShortname == "")) 
                        if (!itemCheck[cat.Key].Contains(item.Key))
                        {
                            if (!toRemove.ContainsKey(cat.Key)) 
                                toRemove.Add(cat.Key, new List<string>());
                            toRemove[cat.Key].Add(item.Key);
                        }

                foreach (var odd in toRemove)
                    foreach (var oddItem in odd.Value)
                        path.LootTypes[odd.Key].Remove(oddItem);

                LootTables[lootTable].Categories = LootTables[lootTable].Categories.OrderBy(x => x.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
                LootTables[lootTable].LootTypes = LootTables[lootTable].LootTypes.OrderBy(x => x.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
                foreach (var entry in LootTables[lootTable].LootTypes.ToDictionary(pair => pair.Key, pair => pair.Value))
                    LootTables[lootTable].LootTypes[entry.Key] = entry.Value.OrderBy(x => x.Key).ToDictionary(pair => pair.Key, pair => pair.Value);

                Interface.Oxide.DataFileSystem.WriteObject($"CustomLoot/{lootTable}", path);
                return true;
            }
            catch
            {
                Puts($"There was an error loading data file {lootTable}.");
                LootTableNames.Remove(lootTable);
                return false;
            }
        }

        public bool IsJunk(string lootTable, string rustItem)
        {
            List<string> junk = LootTables[lootTable].BlackList; 
            if (junk.Contains(rustItem))
                return true;
            return false;
        }

        public bool IsReal(string rustItem)
        {
            var createTest = ItemManager.CreateByName(rustItem, 1);
            if (createTest != null)
            {
                createTest.Remove(0f);
                return true;
            }
            return false;
        }
        #endregion















































        //#region CUI
        //Dictionary<ulong, int> UiUsers = new Dictionary<ulong, int>();

        //void Unload()
        //{
        //    foreach (BasePlayer player in BasePlayer.activePlayerList)
        //        DestroyMenu(player, true);
        //}

        //void OnPlayerConnected(BasePlayer player)
        //{
        //    if (player.IsReceivingSnapshot)
        //    {
        //        timer.Once(1f, () => { OnPlayerConnected(player); });
        //        return;
        //    }
        //}

        //void OnPlayerDisconnected(BasePlayer player)
        //{
        //    if (UiUsers.ContainsKey(player.userID))
        //        DestroyMenu(player, true);
        //}

        //void OnPlayerDeath(BasePlayer player, HitInfo info)
        //{
        //    if (UiUsers.ContainsKey(player.userID))
        //        DestroyMenu(player, false);
        //}

        //void DestroyMenu(BasePlayer player, bool all)
        //{
        //    if (all)
        //        CuiHelper.DestroyUi(player, "CLUIBG");

        //    CuiHelper.DestroyUi(player, "CLUI");
        //    UiUsers.Remove(player.userID);
        //}

        //[ConsoleCommand("CLRefreshUI")]
        //private void CLRefreshUI(ConsoleSystem.Arg arg)
        //{
        //    if (arg.Player() == null)
        //        return;
        //    CLUI(arg.Player(), Convert.ToInt32(arg.Args[0]), Convert.ToInt32(arg.Args[1]));
        //}

        //[ConsoleCommand("CLClose")]
        //private void CLClose(ConsoleSystem.Arg arg)
        //{
        //    if (arg.Player() == null)
        //        return;
        //    DestroyMenu(arg.Player(), true);
        //}




        //[ChatCommand("CustomLootUI")]
        //void template(BasePlayer player, string command, string[] args)
        //{
        //    CLUIBG(player);
        //    CLUI(player, 0, 0);
        //}

        //void CLUIBG(BasePlayer player)
        //{
        //    DestroyMenu(player, true);
        //    var elements = new CuiElementContainer();
        //    var mainName = elements.Add(new CuiPanel { Image = { Color = $"0.1 0.1 0.1 1" }, RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.98" }, CursorEnabled = true, FadeOut = 0.1f }, "Overlay", "CLUIBG");
        //    CuiHelper.AddUi(player, elements); 
        //}

        //void CLUI(BasePlayer player, int page, int subpage)
        //{
        //    DestroyMenu(player, false);
        //    UiUsers[player.userID] = page;
        //    var elements = new CuiElementContainer();
        //    var mainName = elements.Add(new CuiPanel { Image = { Color = $"0.1 0.1 0.1 0" }, RectTransform = { AnchorMin = "0.02 0.02", AnchorMax = "0.98 0.98" }, CursorEnabled = true, FadeOut = 0.1f }, "Overlay", "CLUI");

        //    elements.Add(new CuiLabel { FadeOut = 0.5f, Text = { FadeIn = 0.5f, Text = "Category Probabilities", Color = "1 1 1 0.5", FontSize = 20, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0 0.9", AnchorMax = $"1 1" } }, mainName);

        //    float height1 = 0.87f;
        //    float height2 = 0.9f;
        //    float largest = 0;

        //    foreach (var entry in LootTables["default"].Categories) 
        //        largest = Mathf.Max(largest, entry.Value.probability);

        //    foreach (var entry in LootTables["default"].LootTypes)
        //    {
        //        float p = LootTables["default"].Categories[entry.Key].probability;
        //        if (p > 0)
        //            p = p / largest * 0.86f;

        //        elements.Add(new CuiLabel { FadeOut = 0.5f, Text = { FadeIn = 0.5f, Text = entry.Key, Color = "1 1 1 0.5", FontSize = 12, Align = TextAnchor.MiddleLeft }, RectTransform = { AnchorMin = $"0.01 {height1}", AnchorMax = $"0.06 {height2}" } }, mainName);
        //        elements.Add(new CuiLabel { FadeOut = 0.5f, Text = { FadeIn = 0.5f, Text = LootTables["default"].Categories[entry.Key].probability.ToString(), Color = "1 1 1 0.5", FontSize = 13, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0.08 {height1}", AnchorMax = $"0.1 {height2}" } }, mainName);

                 
        //        elements.Add(new CuiButton { Button = { Command = $"cl.decrease default {entry.Key}", Color = "1 1 1 0.4" }, RectTransform = { AnchorMin = $"0.06 {height1}", AnchorMax = $"0.08 {height2}" }, Text = { Text = "-", Color = "1 1 1 1", FontSize = 18, Align = TextAnchor.MiddleCenter } }, mainName);
        //        elements.Add(new CuiButton { Button = { Command = $"cl.increase default {entry.Key}", Color = "1 1 1 0.4" }, RectTransform = { AnchorMin = $"0.1 {height1}", AnchorMax = $"0.12 {height2}" }, Text = { Text = "+", Color = "1 1 1 1", FontSize = 18, Align = TextAnchor.MiddleCenter } }, mainName);



        //        elements.Add(new CuiButton { Button = { Command = "", Color = "1 0.9 0.9 0.7" }, RectTransform = { AnchorMin = $"0.13 {height1}", AnchorMax = $"{0.13 + p} {height2}" }, Text = { Text = "", FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);

        //        height1 -= 0.04f;
        //        height2 -= 0.04f;
        //    }

        //    elements.Add(new CuiButton { Button = { Command = "CLClose", Color = "0.1 0.1 0.1 0" }, RectTransform = { AnchorMin = "0.97 0.97", AnchorMax = "0.995 0.995" }, Text = { Text = "X", FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
        //    CuiHelper.AddUi(player, elements);
        //}


        //[ConsoleCommand("cl.increase")]
        //private void clincrease(ConsoleSystem.Arg arg)
        //{
        //    if (arg.Player() == null)
        //        return;
        //    DestroyMenu(arg.Player(), false);

        //    LootTables[arg.Args[0]].Categories[arg.Args[1]].probability++;
        //    Interface.Oxide.DataFileSystem.WriteObject($"CustomLoot/{arg.Args[0]}", LootTables[arg.Args[0]]);
        //    CLUI(arg.Player(), 0, 0);
        //}

        //[ConsoleCommand("cl.decrease")]
        //private void cldecrease(ConsoleSystem.Arg arg)
        //{
        //    if (arg.Player() == null)
        //        return;
        //    DestroyMenu(arg.Player(), false);

        //    if (LootTables[arg.Args[0]].Categories[arg.Args[1]].probability > 0)
        //        LootTables[arg.Args[0]].Categories[arg.Args[1]].probability--;
        //    Interface.Oxide.DataFileSystem.WriteObject($"CustomLoot/{arg.Args[0]}", LootTables[arg.Args[0]]);
        //    CLUI(arg.Player(), 0, 0);
        //}
        //#endregion



































        #region Config 
        private ConfigData configData;

        class ConfigData
        {
            public GlobalSettings GlobalSettings = new GlobalSettings();

            public Dictionary<string, Settings> CorpseTypes = new Dictionary<string, Settings>
            {
                {"Excavator", new Settings()},
                {"OilRig", new Settings()},
                {"CompoundScientist", new Settings()},
                {"BanditTown", new Settings()},
                {"MountedScientist", new Settings()},
                {"JunkPileScientist", new Settings()},
                {"DungeonScarecrow", new Settings() },
                {"ScareCrow", new Settings()},
                {"MilitaryTunnelScientist", new Settings()},
                {"CargoShip", new Settings()},
                {"HeavyScientist", new Settings()},
                {"JungleScientist", new Settings() },
                {"APCScientist", new Settings()},
                {"APCScientistHeavy", new Settings()},
                {"TunnelDweller", new Settings()},
                {"UnderwaterDweller", new Settings()},
                {"Trainyard", new Settings()},
                {"Airfield", new Settings()},
                {"DesertScientist", new Settings() },
                {"ArcticResearchBase", new Settings() },
                {"NuclearMissileSiloUnderGround", new Settings()},
                {"NuclearMissileSiloAboveGround", new Settings()},
                {"LaunchSite", new Settings() },
                {"Gingerbread", new Settings()},
                {"ZombieHorde", new Settings()},
                {"BotReSpawn", new Settings()},
                {"Scientist", new Settings()}, 
            };

            public Dictionary<string, Settings> ContainerTypes = cl.ContainerTypesFromGame;
            public Dictionary<string, BaseSettings> API = new Dictionary<string, BaseSettings>();
        }

        public class GlobalSettings
        {
            public bool allowDuplicates = false;
            public bool RoundResourcesStacksDownToNearestTen = true;
            public bool IgnoreContainersWithOwnerID = true;
            public bool corpseTypePerBotReSpawnProfile = false;
            public bool Include_DM_Crates = false;
            public bool Show_Skins_For_All_Items = false;
            public string[] AllowSkinsFor = new string[] { "note" }; 
        }

        public class BaseSettings
        {
            [JsonProperty(Order = 1)]
            public string lootTable = "default";
            [JsonProperty(Order = 2)]
            public int maxItems = 6;
            [JsonProperty(Order = 3)]
            public int minItems = 6;
            [JsonProperty(Order = 4)] 
            public bool gunsWithAmmo;
            [JsonProperty(Order = 5)]
            public bool noGuns;
            [JsonProperty(Order = 6)]
            public int MaxBps = 3;  
            [JsonProperty(Order = 7)]
            public int WaterPreFillPercent = 20;
            [JsonProperty(Order = 8)]
            public bool ClearContainerFirst = true;
        }

        public class Settings : BaseSettings
        {
            [JsonProperty(Order = 1)]
            public bool enabled;
        }

        List<string> Remove = new List<string>() {"crate_fuel","crate_ammunition","crate_food_1","crate_food_2","crate_medical","tech_parts_1","tech_parts_2"}; 
        private void LoadConfigVariables()
        {
            BotTypes.Clear();
            LootTableNames = new List<string>() { "default" };
            configData = Config.ReadObject<ConfigData>();
            bool perBotReSpawn = false;
            foreach (var entry in configData.ContainerTypes.ToList())
                if (Remove.Contains(entry.Key))
                    configData.ContainerTypes.Remove(entry.Key);

            if (configData.GlobalSettings.corpseTypePerBotReSpawnProfile)
            {
                perBotReSpawn = true;
                if (configData.CorpseTypes.ContainsKey("BotReSpawn"))
                {
                    if (backupData.BotReSpawnBackups.ContainsKey("BotReSpawn"))
                        backupData.BotReSpawnBackups["BotReSpawn"] = configData.CorpseTypes["BotReSpawn"];
                    else if (JsonConvert.SerializeObject(configData.CorpseTypes["BotReSpawn"]) != JsonConvert.SerializeObject(new Settings()))
                        backupData.BotReSpawnBackups.Add("BotReSpawn", configData.CorpseTypes["BotReSpawn"]);

                    configData.CorpseTypes.Remove("BotReSpawn");
                }
                foreach (var entry in BotReSpawnBots.Where(entry => !configData.CorpseTypes.ContainsKey($"BotReSpawn-{entry.Key}")))
                {
                    if (backupData.BotReSpawnBackups.ContainsKey($"BotReSpawn-{entry.Key}"))
                        configData.CorpseTypes.Add($"BotReSpawn-{entry.Key}", backupData.BotReSpawnBackups[$"BotReSpawn-{entry.Key}"]);
                    else
                        configData.CorpseTypes.Add($"BotReSpawn-{entry.Key}", new Settings());
                }
            }
            else
            {
                if (JsonConvert.SerializeObject(configData.CorpseTypes["BotReSpawn"]) == JsonConvert.SerializeObject(new Settings()))
                {
                    if (backupData.BotReSpawnBackups.ContainsKey("BotReSpawn"))
                        configData.CorpseTypes["BotReSpawn"] = backupData.BotReSpawnBackups["BotReSpawn"];
                }


                if (BotReSpawn)
                {
                    foreach (var entry in configData.CorpseTypes.ToDictionary(pair => pair.Key, pair => pair.Value).Where(entry => (!perBotReSpawn && entry.Key.Contains("BotReSpawn-")) || (perBotReSpawn && entry.Key.Contains("BotReSpawn-") && !BotReSpawnBots.ContainsKey(entry.Key.Remove(0, 9)))))
                    {
                        if (backupData.BotReSpawnBackups.ContainsKey(entry.Key))
                            backupData.BotReSpawnBackups[entry.Key] = configData.CorpseTypes[entry.Key];
                        else
                            backupData.BotReSpawnBackups.Add(entry.Key, configData.CorpseTypes[entry.Key]);

                        configData.CorpseTypes.Remove(entry.Key);
                    }
                }
            }
            if (!configData.GlobalSettings.Include_DM_Crates)
                foreach (var entry in configData.ContainerTypes.ToDictionary(pair => pair.Key, pair => pair.Value).Where(x => x.Key.Contains("dm ")))
                    configData.ContainerTypes.Remove(entry.Key);

            foreach (var entry in configData.CorpseTypes.ToDictionary(pair => pair.Key, pair => pair.Value))
            {
                BotTypes.Add(entry.Key, new List<ulong>());
                if (!LootTableNames.Contains(entry.Value.lootTable))
                    LootTableNames.Add(entry.Value.lootTable);
            }
            foreach (var entry in configData.ContainerTypes)
                if (!LootTableNames.Contains(entry.Value.lootTable))
                    LootTableNames.Add(entry.Value.lootTable);
            foreach (var entry in configData.API)
                if (!LootTableNames.Contains(entry.Value.lootTable))
                    LootTableNames.Add(entry.Value.lootTable);

            SaveConfig(configData);
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
        }

        void SaveConfig(ConfigData config)
        {
            config.CorpseTypes = config.CorpseTypes.OrderBy(x => x.Key).OrderBy(x => x.Key.Contains("BotReSpawn")).ToDictionary(pair => pair.Key, pair => pair.Value);
            configData.ContainerTypes = configData.ContainerTypes.OrderBy(x => x.Key).OrderByDescending(x => x.Key.Contains("barrel")).ToDictionary(pair => pair.Key, pair => pair.Value);
            configData.API = configData.API.OrderBy(x => x.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
            Config.WriteObject(config, true);
        }
        #endregion     
    }
}
 