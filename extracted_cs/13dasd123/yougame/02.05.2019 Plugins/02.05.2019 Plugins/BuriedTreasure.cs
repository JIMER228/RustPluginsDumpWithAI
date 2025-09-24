using System;
using Rust;
using Oxide.Core;
using Oxide.Core.Configuration;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("BuriedTreasure", "Colon Blow", "1.0.5")]
      //  Слив плагинов server-rust by Apolo YouGame
    class BuriedTreasure : RustPlugin
    {

        #region Load

        [PluginReference] Plugin ServerRewards;
        [PluginReference] Plugin Economics;
        [PluginReference] Plugin RustShop;

        void Loaded()
        {
            LoadVariables();
            permission.RegisterPermission("buriedtreasure.admin", this);
        }

        #endregion

        #region Configuration

        bool UseServerRewards = true;
        bool UseEconomics = true;
        bool UseRustShop = true;
        int ServerRewardsGoldExchange = 100;
        int RustShopGoldExchange = 100;
        double EconomicsGoldExchange = 100;

        bool EnableMapsInStandardLoot = false;
        bool EnableGoldInStandardLoot = false;
        bool EnableAutoGoldRewardOnLoot = false;
        bool EnableAutoReadMapOnLoot = false;
        int StandardLootAddMapChance = 1;
        int StandardLootAddGoldChance = 1;

        static float LocalTreasureMaxDistance = 300;
        static bool UseWholeMapSpawn = false;
        static float WholeMapOffset = 500f;
        static float DespawnTime = 3600f;
        static float TreasureDespawnTime = 3600f;
        static float LootDetectionRadius = 8f;

        static int AddMapChance = 5;
        static int AddGoldChance = 5;

        static int BasicMapChance = 50;
        static int UnCommonMapChance = 30;
        static int RareMapChance = 15;
        static int EliteMapChance = 5;

        static string MapMarkerPrefab = "assets/prefabs/tools/map/explosionmarker.prefab";

        static string BasicTreasurePrefab = "assets/bundled/prefabs/radtown/crate_basic.prefab";
        static string UnCommonTreasurePrefab = "assets/bundled/prefabs/radtown/crate_normal.prefab";
        static string RareTreasurePrefab = "assets/bundled/prefabs/radtown/crate_normal_2.prefab";
        static string EliteTreasurePrefab = "assets/bundled/prefabs/radtown/crate_elite.prefab";

        bool Changed;

        void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            LoadVariables();
        }

        private void LoadConfigVariables()
        {
            CheckCfg("Gold - Enable gold to be sold for Server Reward Points ? ", ref UseServerRewards);
            CheckCfg("Gold - Enable gold to be sold for Economics Bucks ? ", ref UseEconomics);
            CheckCfg("Gold - Enable gold to be sold for RustShop money ? ", ref UseRustShop);
            CheckCfg("Gold - Player will get this many Server Reward Points when selling 1 gold : ", ref ServerRewardsGoldExchange);
            CheckCfg("Gold - Player will get this many RustShop Money when selling 1 gold : ", ref RustShopGoldExchange);
            CheckCfg("Gold - Player will get this many Economics Bucks when selling 1 gold : ", ref EconomicsGoldExchange);

            CheckCfg("AutoLoot - Automatically turn in gold coins for rewards when looting ? ", ref EnableAutoGoldRewardOnLoot);
            CheckCfg("AutoLoot - Automatically mark treasure maps when they are looted ? ", ref EnableAutoReadMapOnLoot);

            CheckCfg("Standard Loot - Enable chance for random treasure map in standard loot crates ? ", ref EnableMapsInStandardLoot);
            CheckCfg("Standard Loot - Enable chance for gold to spawn in standard loot crates ? ", ref EnableGoldInStandardLoot);
            CheckCfg("Standard Loot - Random Treasure Map chance (if enabled) : ", ref StandardLootAddMapChance);
            CheckCfg("Standard Loot - Gold spawn chance (if enabled) : ", ref StandardLootAddGoldChance);

            CheckCfgFloat("Treasure - Spawn - Only spawn Treasure up to this far from players current postion : ", ref LocalTreasureMaxDistance);
            CheckCfg("Treasure - Spawn - Use whole map (instead of distance from player) to get random spawn point ? ", ref UseWholeMapSpawn);
            CheckCfgFloat("Treasure - Spawn - When whole map size is used, reduce spawn area by this much offset (closer to land) : ", ref WholeMapOffset);
            CheckCfgFloat("Treasure - Despawn - Approx Seconds the Treasure Marker and Location will despawn if not found : ", ref DespawnTime);
            CheckCfgFloat("Treasure - Despawn - Approx Seconds the Spawned Chest will despawn if not looted : ", ref TreasureDespawnTime);
            CheckCfgFloat("Treasure - Location - When player gets within this distance, treasure will spawn nearby : ", ref LootDetectionRadius);

            CheckCfg("Treasure - Chance - to add a Random Map to Treasure Chest : ", ref AddMapChance);
            CheckCfg("Treasure - Chance - to add a Gold to Treasure Chest : ", ref AddGoldChance);

            CheckCfg("Treasure - Chance - When a random map is added to chest or spawned, chance it will be a Basic Map: ", ref BasicMapChance);
            CheckCfg("Treasure - Chance - When a random map is added to chest or spawned, chance it will be a UnCommon Map: ", ref UnCommonMapChance);
            CheckCfg("Treasure - Chance - When a random map is added to chest or spawned, chance it will be a Rare Map: ", ref RareMapChance);
            CheckCfg("Treasure - Chance - When a random map is added to chest or spawned, chance it will be a Elite Map: ", ref EliteMapChance);

            CheckCfg("Map Marker - Prefab - Treasure Chest Map marker prefab (default explosion marker) : ", ref MapMarkerPrefab);

            CheckCfg("Treasure - Prefab - Basic Treasure Chest prefab : ", ref BasicTreasurePrefab);
            CheckCfg("Treasure - Prefab - UnCommon Treasure Chest prefab : ", ref UnCommonTreasurePrefab);
            CheckCfg("Treasure - Prefab - Rare Treasure Chest prefab : ", ref RareTreasurePrefab);
            CheckCfg("Treasure - Prefab - Elite Treasure Chest prefab : ", ref EliteTreasurePrefab);
        }

        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }

        private void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] is T)
                var = (T)Config[Key];
            else
                Config[Key] = var;
        }

        private void CheckCfgFloat(string Key, ref float var)
        {
            if (Config[Key] != null)
                var = Convert.ToSingle(Config[Key]);
            else
                Config[Key] = var;
        }

        object GetConfig(string menu, string datavalue, object defaultValue)
        {
            var data = Config[menu] as Dictionary<string, object>;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                Config[menu] = data;
                Changed = true;
            }

            object value;
            if (!data.TryGetValue(datavalue, out value))
            {
                value = defaultValue;
                data[datavalue] = value;
                Changed = true;
            }
            return value;
        }

        #endregion

        #region Commands

        [ConsoleCommand("buymap")]
        void cmdConsoleBuyMap(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, "buriedtreasure.admin")) return;
                GiveTreasureMap(player);
                return;
            }
            if (arg.Args.Length > 0)
            {
                ulong id = Convert.ToUInt64(arg.Args[0]);
                GiveTreasureMap(BasePlayer.FindByID(id));
            }
        }

        [ConsoleCommand("buyuncommonmap")]
        void cmdConsoleBuyUnCommonMap(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, "buriedtreasure.admin")) return;
                GiveUnCommonTreasureMap(player);
                return;
            }
            if (arg.Args.Length > 0)
            {
                ulong id = Convert.ToUInt64(arg.Args[0]);
                GiveUnCommonTreasureMap(BasePlayer.FindByID(id));
            }
        }

        [ConsoleCommand("buyraremap")]
        void cmdConsoleBuyRareMap(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, "buriedtreasure.admin")) return;
                GiveRareTreasureMap(player);
                return;
            }
            if (arg.Args.Length > 0)
            {
                ulong id = Convert.ToUInt64(arg.Args[0]);
                GiveRareTreasureMap(BasePlayer.FindByID(id));
            }
        }

        [ConsoleCommand("givegold")]
        void cmdConsoleGiveGold(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, "buriedtreasure.admin")) return;
                GiveGold(player);
                return;
            }
        }

        [ConsoleCommand("buyelitemap")]
        void cmdConsoleBuyEliteMap(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, "buriedtreasure.admin")) return;
                GiveEliteTreasureMap(player);
                return;
            }
            if (arg.Args.Length > 0)
            {
                ulong id = Convert.ToUInt64(arg.Args[0]);
                GiveEliteTreasureMap(BasePlayer.FindByID(id));
            }
        }

        [ConsoleCommand("buyrandommap")]
        void cmdConsoleBuyRandomMap(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player != null)
            {
                if (!permission.UserHasPermission(player.UserIDString, "buriedtreasure.admin")) return;
                GiveRandomTreasureMap(player);
                return;
            }
            if (arg.Args.Length > 0)
            {
                ulong id = Convert.ToUInt64(arg.Args[0]);
                GiveRandomTreasureMap(BasePlayer.FindByID(id));
            }
        }

        [ChatCommand("markmap")]
        void cmdMarkMap(BasePlayer player, string command, string[] args)
        {
            if (!HoldingMap(player, player.GetActiveItem()))
            {
                SendReply(player, "У вас нет карты в руках!");
            }
        }

        [ChatCommand("treasurehelp")]
        void cmdTreasureHelp(BasePlayer player, string command, string[] args)
        {
            string help1 = "/markmap - отобразить место сундука на карте G";
            string help2 = "/sellgold - while holding gold, will sell gold for RP or Economics Bucks.";
            string help3 = "Возьмите в руки карту";

            SendReply(player, " Treasure Map Commands : \n " + help1 + " \n " + help2 + " \n " + help3);
        }

        [ChatCommand("sellgold")]
        void cmdSellGold(BasePlayer player, string command, string[] args)
        {
            SellGold(player);
        }

        #endregion

        #region Hooks

        object CanStackItem(Item item, Item targetItem)
        {
            if (item == null || targetItem == null) return null;
            if (item.skin == 1376561963) return false;
            if (item.skin == 1389950043) return false;
            if (item.skin == 1390209788) return false;
            if (item.skin == 1390210901) return false;
            if (item.skin == 1390211736) return false;
            return null;
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || input == null) return;
            if (input.IsDown(BUTTON.FIRE_SECONDARY))
            {
                if (!HoldingMap(player))
                    SellGold(player);
            }
        }

        void CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot)
        {
            if (item == null || playerLoot == null || targetContainer == null || targetSlot == null) return;

            var thplayer = playerLoot.GetComponentInParent<BasePlayer>() as BasePlayer;
            if (thplayer == null) return;
            if (EnableAutoGoldRewardOnLoot && item.skin == 1376561963) { SellGold(thplayer, item); return; }
            if (EnableAutoReadMapOnLoot && HoldingMap(thplayer, item)) return;

            if (targetSlot != -1) return;

            var container = playerLoot.FindContainer(targetContainer) ?? null;
            if (container == null || container != playerLoot.containerMain) return;

            if (HoldingMap(thplayer, item)) return;
            SellGold(thplayer, item);
        }

        bool HoldingMap(BasePlayer player, Item item = null)
        {
            Item activeItem;
            if (item != null) activeItem = item;
            else activeItem = player.GetActiveItem();

            if (activeItem != null)
            {
                if (activeItem.skin == 1389950043)
                {
                    activeItem.Remove(0f);
                    BuryTheTreasure(player, 1);
                    return true;
                }
                if (activeItem.skin == 1390209788)
                {
                    activeItem.Remove(0f);
                    BuryTheTreasure(player, 2);
                    return true;
                }
                if (activeItem.skin == 1390210901)
                {
                    activeItem.Remove(0f);
                    BuryTheTreasure(player, 3);
                    return true;
                }
                if (activeItem.skin == 1390211736)
                {
                    activeItem.Remove(0f);
                    BuryTheTreasure(player, 4);
                    return true;
                }
            }
            return false;
        }

        void GiveTreasureMap(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(1414245162, 1, 1389950043);
            player.inventory.GiveItem(item);
        }

        void GiveUnCommonTreasureMap(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(1414245162, 1, 1390209788);
            player.inventory.GiveItem(item);
        }

        void GiveRareTreasureMap(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(1414245162, 1, 1390210901);
            player.inventory.GiveItem(item);
        }

        void GiveEliteTreasureMap(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(1414245162, 1, 1390211736);
            player.inventory.GiveItem(item);
        }

        void GiveRandomTreasureMap(BasePlayer player)
        {
            ulong skinid = 1389950043;
            var randomroll = UnityEngine.Random.Range(0, (BasicMapChance + UnCommonMapChance + RareMapChance + EliteMapChance));
            if (randomroll >= 0 && randomroll <= BasicMapChance) skinid = 1389950043;
            if (randomroll >= (BasicMapChance + 1) && randomroll <= (BasicMapChance + UnCommonMapChance)) skinid = 1390209788;
            if (randomroll >= (UnCommonMapChance + 1) && randomroll <= (UnCommonMapChance + RareMapChance)) skinid = 1390210901;
            if (randomroll >= (RareMapChance + 1) && randomroll <= (RareMapChance + EliteMapChance)) skinid = 1390211736;
            var item = ItemManager.CreateByItemID(1414245162, 1, skinid);
            player.inventory.GiveItem(item);
        }

        void GiveContainerRandomTreasureMap(LootContainer container)
        {
            ulong skinid = 1389950043;
            var randomroll = UnityEngine.Random.Range(0, (BasicMapChance + UnCommonMapChance + RareMapChance + EliteMapChance));
            if (randomroll >= 0 && randomroll <= BasicMapChance) skinid = 1389950043;
            if (randomroll >= (BasicMapChance + 1) && randomroll <= (BasicMapChance + UnCommonMapChance)) skinid = 1390209788;
            if (randomroll >= (UnCommonMapChance + 1) && randomroll <= (UnCommonMapChance + RareMapChance)) skinid = 1390210901;
            if (randomroll >= (RareMapChance + 1) && randomroll <= (RareMapChance + EliteMapChance)) skinid = 1390211736;

            ItemContainer component1 = container.GetComponent<StorageContainer>().inventory;
            Item item = ItemManager.CreateByItemID(1414245162, 1, skinid);
            component1.itemList.Add(item);
            item.parent = component1;
            item.MarkDirty();
        }

        void GiveGold(BasePlayer player)
        {
            var item = ItemManager.CreateByItemID(1414245162, 1, 1376561963);
            player.inventory.GiveItem(item);
        }

        void GiveContainerGold(LootContainer container)
        {
            ItemContainer component1 = container.GetComponent<StorageContainer>().inventory;
            Item item = ItemManager.CreateByItemID(1414245162, 1, 1376561963);
            component1.itemList.Add(item);
            item.parent = component1;
            item.MarkDirty();
        }

        void SellGold(BasePlayer player, Item item = null)
        {
            Item activeItem = new Item();
            if (item != null) activeItem = item;
            else activeItem = player.GetActiveItem();

            if (activeItem != null)
            {
                if (activeItem.skin == 1376561963)
                {
                    if (UseServerRewards && ServerRewards != null)
                    {
                        ServerRewards?.Call("AddPoints", new object[] { player.userID, ServerRewardsGoldExchange });
                        SendReply(player, "You Just sold your gold for " + ServerRewardsGoldExchange.ToString() + " Rewards Points !!!");
                    }
                    if (UseEconomics && Economics != null)
                    {
                        Economics?.Call("Deposit", new object[] { player.userID, EconomicsGoldExchange });
                        SendReply(player, "You Just sold your gold for " + EconomicsGoldExchange.ToString() + " Economic Bucks !!!");
                    }
                    if (UseRustShop && RustShop != null)
                    {
                        RustShop?.Call("AddBalance", new object[] { player.userID, RustShopGoldExchange });
                        SendReply(player, "You Just sold your gold for " + RustShopGoldExchange.ToString() + " RustShop Money !!!");
                    }
                    activeItem.Remove(0f);
                    return;
                }
            }
        }

        static float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos);

            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out hit, Mathf.Infinity, UnityEngine.LayerMask.GetMask("World", "Construction", "Default")))
                return Mathf.Max(hit.point.y, y);

            return y;
        }

        Vector3 GetSpawnLocation(BasePlayer player)
        {
            Vector3 targetPos = new Vector3();
            RaycastHit hitInfo;
      //  Слив плагинов server-rust by Apolo YouGame
            Vector3 randomizer = new Vector3(UnityEngine.Random.Range(-LocalTreasureMaxDistance, LocalTreasureMaxDistance), 0f, UnityEngine.Random.Range(-LocalTreasureMaxDistance, LocalTreasureMaxDistance));
            Vector3 newp = (player.transform.position + randomizer);
            var groundy = GetGroundPosition(newp);
            targetPos = new Vector3(newp.x, groundy, newp.z);
            return targetPos;
        }

        Vector3 FindGlobalSpawnPoint()
        {
            Vector3 spawnpoint = new Vector3();
            float mapoffset = WholeMapOffset;
            float mapsize = ((ConVar.Server.worldsize) / 2) - mapoffset;
            Vector3 randomizer = new Vector3(UnityEngine.Random.Range(-mapsize, mapsize), 0f, UnityEngine.Random.Range(-mapsize, mapsize));
            Vector3 newp = randomizer;
            var groundy = GetGroundPosition(newp);
            spawnpoint = new Vector3(randomizer.x, groundy, randomizer.z);
            return spawnpoint;
        }

        void BuryTheTreasure(BasePlayer player, int maprarity = 1)
        {
            string prefabstash = "assets/prefabs/deployable/small stash/small_stash_deployed.prefab";
            Vector3 position = GetSpawnLocation(player);
            if (UseWholeMapSpawn) position = FindGlobalSpawnPoint();
            var stash = GameManager.server.CreateEntity(prefabstash, position, Quaternion.identity, true);
            stash.OwnerID = player.userID;
            var stashcont = stash.GetComponent<StashContainer>();
            stashcont.uncoverRange = -1f;
            stashcont.burriedOffset = 1f;
            var stashstab = stash.GetComponent<StabilityEntity>();
            if (stashstab) stashstab.grounded = true;
            stash?.Spawn();
            var addmarker = stash.gameObject.AddComponent<TreasureMarker>();
            addmarker.rarity = maprarity;
            SendReply(player, "<color=lightblue>Сокровище появилось на карте</color> <color=red>G</color>: <color=yellow>клетка</color> " + GetGridLocation(position));
        }

        object CanNetworkTo(BaseEntity entity, BasePlayer target)
        {
            var mapobj = entity.GetComponentInParent<MapMarker>() ?? null;
            if (mapobj != null && mapobj.skinID == 1234)
            {
                if (target.userID == entity.OwnerID) return true;
                return false;
            }
            return null;
        }

        void OnLootSpawn(LootContainer container)
        {
            var getobj = container.GetComponentInParent<BaseEntity>() ?? null;
            if (getobj != null && getobj.skinID == 111) return;
            if (EnableMapsInStandardLoot)
            {
                int randomlootroll = UnityEngine.Random.Range(0, 100);
                if (randomlootroll <= StandardLootAddMapChance) GiveContainerRandomTreasureMap(container);
            }
            if (EnableGoldInStandardLoot)
            {
                int randomgoldlootroll = UnityEngine.Random.Range(0, 100);
                if (randomgoldlootroll <= StandardLootAddGoldChance) GiveContainerGold(container);
            }
        }

        string GetGridLocation(Vector3 location)
        {
            //Credits !!!! base code from carny666's GrTeleport plugin, the rest by Colon Blow !!!
            string gridLocation = "";
            int numx = Convert.ToInt32(location.x);
            int numz = Convert.ToInt32(location.z);

            float offset = (ConVar.Server.worldsize) / 2;
            float step = (ConVar.Server.worldsize) / (0.0066666666666667f * (ConVar.Server.worldsize));
            string start = "";

            int diff = Convert.ToInt32(step);
            int absoluteDifference = diff;

            char letter = 'A';
            int number = 0;
            for (float xx = -offset; xx < offset; xx += step)
            {
                for (float zz = offset; zz > -offset; zz -= step)
                {
                    if (Math.Abs(numx - xx) <= diff && Math.Abs(numz - zz) <= diff)
                    {
                        gridLocation = $"{start}{letter}{number}";
                        break;
                    }
                    number++;
                }
                number = 0;
                if (letter.ToString().ToUpper() == "Z")
                {
                    start = "A";
                    letter = 'A';
                }
                else
                {
                    letter = (char)(((int)letter) + 1);
                }
                if (Math.Abs(numx - xx) <= diff)
                {
                    break;
                }
            }
            return gridLocation;
        }

        #endregion

        #region TreasureMarker 

        class TreasureMarker : BaseEntity
        {
            BaseEntity lootbox;
            BaseEntity treasurechest;
            MapMarker mapmarker;
            SphereCollider sphereCollider;
            public ulong playerid;
            BuriedTreasure instance;
            public int rarity;
            string prefabtreasure;
            bool isvisible;
            bool didspawnchest;
            float despawncounter;
            float detectionradius;

            void Awake()
            {
                instance = new BuriedTreasure();
                lootbox = GetComponentInParent<BaseEntity>();
                playerid = lootbox.OwnerID;
                rarity = 1;
                despawncounter = 0f;
                isvisible = false;
                didspawnchest = false;
                detectionradius = LootDetectionRadius;
                string prefabmarker = MapMarkerPrefab;
                mapmarker = GameManager.server.CreateEntity(prefabmarker, lootbox.transform.position, Quaternion.identity, true) as MapMarker;
                mapmarker.OwnerID = playerid;
                mapmarker.skinID = 1234;
                mapmarker.Spawn();

                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.gameObject.layer = (int)Layer.Reserved1;
                sphereCollider.isTrigger = true;
                sphereCollider.radius = detectionradius;
            }

            private void OnTriggerEnter(Collider col)
            {
                if (didspawnchest) return;
                var target = col.GetComponentInParent<BasePlayer>();
                if (target != null)
                {
                    if (target.userID == lootbox.OwnerID)
                    {
                        SpawnTreasureChest();
                        didspawnchest = true;
                        instance.SendReply(target, "<color=lightblue>Сокровище уже близко!</color>");
                    }
                }
            }

            void SpawnTreasureChest()
            {
                if (rarity == 1) prefabtreasure = BasicTreasurePrefab;
                if (rarity == 2) prefabtreasure = UnCommonTreasurePrefab;
                if (rarity == 3) prefabtreasure = RareTreasurePrefab;
                if (rarity == 4) prefabtreasure = EliteTreasurePrefab;
                treasurechest = GameManager.server.CreateEntity(prefabtreasure, lootbox.transform.position, Quaternion.identity, true);
                treasurechest.skinID = 111;
                treasurechest.OwnerID = lootbox.OwnerID;
                treasurechest.Spawn();
                treasurechest.gameObject.AddComponent<TreasureDespawner>();
                lootbox.Invoke("KillMessage", 0.2f);
                CheckForExtras(treasurechest);
                CheckSpawnVisibility(treasurechest);
            }

            void CheckSpawnVisibility(BaseEntity entitybox)
            {
                if (isvisible) return;
                if (entitybox.IsOutside()) { isvisible = true; return; }
                entitybox.transform.position = entitybox.transform.position + new Vector3(0f, 0.2f, 0f);
                entitybox.transform.hasChanged = true;
                entitybox.SendNetworkUpdateImmediate();
                CheckSpawnVisibility(entitybox);
            }

            void CheckForExtras(BaseEntity entitybox)
            {
                int randommaproll = UnityEngine.Random.Range(0, 100);
                if (rarity == 2) randommaproll = randommaproll - 2;
                if (rarity == 3) randommaproll = randommaproll - 4;
                if (rarity == 4) randommaproll = randommaproll - 6;
                if (randommaproll > 100) randommaproll = 100;
                if (randommaproll < 0) randommaproll = 0;
                if (randommaproll <= AddMapChance) AddRandomMap(entitybox);

                AddRandomGold(entitybox);
            }

            void AddRandomGold(BaseEntity entitybox)
            {
                int randomgoldroll = UnityEngine.Random.Range(0, 100);
                if (rarity == 2) randomgoldroll = randomgoldroll - 2;
                if (rarity == 3) randomgoldroll = randomgoldroll - 4;
                if (rarity == 4) randomgoldroll = randomgoldroll - 6;
                if (randomgoldroll > 100) randomgoldroll = 100;
                if (randomgoldroll < 0) randomgoldroll = 0;
                if (randomgoldroll <= AddGoldChance)
                {
                    ItemContainer component1 = entitybox.GetComponent<StorageContainer>().inventory;
                    Item item = ItemManager.CreateByItemID(1414245162, 1, 1376561963);
                    component1.itemList.Add(item);
                    item.parent = component1;
                    item.MarkDirty();
                }
            }

            void AddRandomMap(BaseEntity entitybox)
            {
                ulong skinid = 1389950043;
                var randomroll = UnityEngine.Random.Range(0, (BasicMapChance + UnCommonMapChance + RareMapChance + EliteMapChance));
                if (randomroll >= 0 && randomroll <= BasicMapChance) skinid = 1389950043;
                if (randomroll >= (BasicMapChance + 1) && randomroll <= (BasicMapChance + UnCommonMapChance)) skinid = 1390209788;
                if (randomroll >= (UnCommonMapChance + 1) && randomroll <= (UnCommonMapChance + RareMapChance)) skinid = 1390210901;
                if (randomroll >= (RareMapChance + 1) && randomroll <= (RareMapChance + EliteMapChance)) skinid = 1390211736;
                ItemContainer component1 = entitybox.GetComponent<StorageContainer>().inventory;
                Item item = ItemManager.CreateByItemID(1414245162, 1, skinid);
                component1.itemList.Add(item);
                item.parent = component1;
                item.MarkDirty();
            }

            void FixedUpdate()
            {
                if (despawncounter >= (DespawnTime * 15) && lootbox != null) { lootbox.Invoke("KillMessage", 0.1f); return; }
                despawncounter = despawncounter + 1f;
            }

            void OnDestroy()
            {
                if (mapmarker != null) mapmarker.Invoke("KillMessage", 0.1f);
                if (lootbox != null) lootbox.Invoke("KillMessage", 0.1f);
            }
        }

        #endregion

        #region TreasureDespawner 

        class TreasureDespawner : BaseEntity
        {
            BaseEntity treasure;
            BuriedTreasure instance;
            float despawncounter;

            void Awake()
            {
                instance = new BuriedTreasure();
                treasure = GetComponentInParent<BaseEntity>();
                despawncounter = 0f;
            }

            void FixedUpdate()
            {
                if (despawncounter >= (TreasureDespawnTime * 15) && treasure != null) { treasure.Invoke("KillMessage", 0.1f); return; }
                despawncounter = despawncounter + 1f;
            }

            void OnDestroy()
            {
                if (treasure != null) treasure.Invoke("KillMessage", 0.1f);
            }
        }

        #endregion
    }
}
