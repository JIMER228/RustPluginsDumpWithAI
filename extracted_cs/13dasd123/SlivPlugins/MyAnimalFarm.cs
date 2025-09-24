// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Oxide.Core;
using Network;
using Rust.Ai;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("MyAnimalFarm", "Qbis", "1.1.1")]
    public class MyAnimalFarm : RustPlugin
    {
        #region [Vars]
        [PluginReference] private Plugin ImageLibrary, PersonalAnimal;

        private List<ulong> trapsSkins = new List<ulong>();
        private Dictionary<ulong, string> animalsSkinsPrefabs = new Dictionary<ulong, string>();
        private Dictionary<HitchTrough, FarmManager> Farms = new Dictionary<HitchTrough, FarmManager>();
        private Dictionary<ulong, int> playersFarms = new Dictionary<ulong, int>();
        private List<BaseAnimalNPC> SavedAnimals = new List<BaseAnimalNPC>();
        private static MyAnimalFarm plugin;

        public class Data
        {
            public Vector3 pos;
            public string type;
        }

        public class AnimalData
        {
            public Vector3 pos;
            public string prefab;
            public float Health;
        }
        #endregion

        #region [Config]
        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < Version)
            {
                PrintWarning("Config checked completed!");
            }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private class PluginConfig
        {
            [JsonProperty("Основные настройки")]
            public GeneralSettings settings;

            [JsonProperty("Настройки ловушек")]
            public List<TrapSettings> traps;

            [JsonProperty("Настройки ферм")]
            public List<FarmSettings> farms;

            [JsonProperty("Config version")]
            public VersionNumber PluginVersion = new VersionNumber();

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    settings = new GeneralSettings()
                    {
                        maxFarmsPerPlayer = 4,
                        animalImages = new Dictionary<string, string>()
                        {
                            ["bear"] = "https://i.imgur.com/rwNKrbQ.png",
                            ["wolf"] = "https://i.imgur.com/AHpf3pf.png",
                            ["stag"] = "https://i.imgur.com/WmIKIFG.png",
                            ["boar"] = "https://i.imgur.com/GehMURO.png"
                        }
                    },
                    traps = new List<TrapSettings>()
                    {
                        new TrapSettings()
                        {
                            skinID = 2700797172,
                            name = "Ловушка для Кобана/Оленя",
                            bait = "pumpkin",
                            dist = 30f,
                            animalsSettings = new  List<AnimalSettings>()
                            {
                                new AnimalSettings()
                                {
                                    animalName = "boar",
                                    animalPrefab = "assets/rust.ai/agents/boar/boar.prefab",
                                    skinID = 2683341177
                                },
                                new AnimalSettings()
                                {
                                    animalName = "stag",
                                    animalPrefab = "assets/rust.ai/agents/stag/stag.prefab",
                                    skinID = 2700820020
                                }
                            },
                            canCraft = true,
                            imgUrl = "https://i.imgur.com/IL0QYe0.png",
                            needItemsToCraft = new List<CraftItem>()
                            {
                                new CraftItem(){ shortName = "pumpkin", amount = 1 },
                                new CraftItem() { shortName = "trap.bear", amount = 1 }
                            }
                        },
                        new TrapSettings()
                        {
                            skinID = 2700876388,
                            name = "Ловушка для Волка/Медведя",
                            bait = "meat.boar",
                            dist = 30f,
                            animalsSettings = new  List<AnimalSettings>()
                            {
                                new AnimalSettings()
                                {
                                    animalName = "bear",
                                    animalPrefab = "assets/rust.ai/agents/bear/bear.prefab",
                                    skinID = 2700880093
                                },
                                new AnimalSettings()
                                {
                                    animalName = "wolf",
                                    animalPrefab = "assets/rust.ai/agents/wolf/wolf.prefab",
                                    skinID = 2700884049
                                }
                            },
                            canCraft = true,
                            imgUrl = "https://i.imgur.com/sZyLCgP.png",
                            needItemsToCraft = new List<CraftItem>()
                            {
                                new CraftItem(){ shortName = "meat.boar", amount = 1 },
                                new CraftItem() { shortName = "trap.bear", amount = 1 }
                            }
                        }
                    },
                    farms = new List<FarmSettings>()
                    {
                        new FarmSettings()
                        {
                            perm = "myanimalfarm.bear",
                            animalName = "bear",
                            animalRemoveHp = 10,
                            maxAnimals = 4,
                            raidus = 15,
                            rewardTime = 5,
                            foodAmount = 1,
                            food = "meat.boar",
                            rewards = new List<Reward>()
                            {
                                 new Reward()
                                {
                                    shortName = "stones",
                                    amount = 100,
                                    skinID = 0,
                                    customName = "",
                                    chance = 100
                                },
                                new Reward()
                                {
                                    shortName = "rifle.ak",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    chance = 1
                                },
                                new Reward()
                                {
                                    shortName = "glue",
                                    amount = 10,
                                    skinID = 2409891781,
                                    customName = "$",
                                    chance = 25
                                }
                            }
                        },
                        new FarmSettings()
                        {
                            perm = "myanimalfarm.boar",
                            animalName = "boar",
                            animalRemoveHp = 10,
                            maxAnimals = 4,
                            raidus = 15,
                            rewardTime = 5,
                            foodAmount = 1,
                            food = "pumpkin",
                            rewards = new List<Reward>()
                            {
                                 new Reward()
                                {
                                    shortName = "stones",
                                    amount = 100,
                                    skinID = 0,
                                    customName = "",
                                    chance = 100
                                },
                                new Reward()
                                {
                                    shortName = "rifle.ak",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    chance = 1
                                },
                                new Reward()
                                {
                                    shortName = "glue",
                                    amount = 10,
                                    skinID = 2409891781,
                                    customName = "$",
                                    chance = 25
                                }
                            }
                        },
                        new FarmSettings()
                        {
                            perm = "myanimalfarm.stag",
                            animalName = "stag",
                            animalRemoveHp = 10,
                            maxAnimals = 4,
                            raidus = 15,
                            rewardTime = 5,
                            foodAmount = 1,
                            food = "pumpkin",
                            rewards = new List<Reward>()
                            {
                                 new Reward()
                                {
                                    shortName = "stones",
                                    amount = 100,
                                    skinID = 0,
                                    customName = "",
                                    chance = 100
                                },
                                new Reward()
                                {
                                    shortName = "rifle.ak",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    chance = 1
                                },
                                new Reward()
                                {
                                    shortName = "glue",
                                    amount = 10,
                                    skinID = 2409891781,
                                    customName = "$",
                                    chance = 25
                                }
                            }
                        },
                        new FarmSettings()
                        {
                            perm = "myanimalfarm.wolf",
                            animalName = "wolf",
                            animalRemoveHp = 10,
                            maxAnimals = 4,
                            raidus = 15,
                            rewardTime = 5,
                            foodAmount = 1,
                            food = "meat.boar",
                            rewards = new List<Reward>()
                            {
                                 new Reward()
                                {
                                    shortName = "stones",
                                    amount = 100,
                                    skinID = 0,
                                    customName = "",
                                    chance = 100
                                },
                                new Reward()
                                {
                                    shortName = "rifle.ak",
                                    amount = 1,
                                    skinID = 0,
                                    customName = "",
                                    chance = 1
                                },
                                new Reward()
                                {
                                    shortName = "glue",
                                    amount = 10,
                                    skinID = 2409891781,
                                    customName = "$",
                                    chance = 25
                                }
                            }
                        }
                    },
                    PluginVersion = new VersionNumber()
                };
            }
        }

        public class GeneralSettings
        {
            [JsonProperty("Максимум ферм на 1 игрока")]
            public int maxFarmsPerPlayer;

            [JsonProperty("Ссылка на картинки животных")]
            public Dictionary<string, string> animalImages;
        }

        public class TrapSettings
        {
            [JsonProperty("СкинАйди ловушки")]
            public ulong skinID;

            [JsonProperty("Название ловушки")]
            public string name;

            [JsonProperty("ShortName приманки")]
            public string bait;

            [JsonProperty("Дальность действия приманки")]
            public float dist;

            [JsonProperty("Кого можно поймать")]
            public List<AnimalSettings> animalsSettings;

            [JsonProperty("Можно ли крафтить")]
            public bool canCraft;

            [JsonProperty("Ссылка на картинку")]
            public string imgUrl;

            [JsonProperty("Список нужных предметов для крафта")]
            public List<CraftItem> needItemsToCraft;
        }

        public class AnimalSettings
        {
            [JsonProperty("СкинАйди предмета после поимки")]
            public ulong skinID;

            [JsonProperty("Имя животного")]
            public string animalName;

            [JsonProperty("Префаб животного")]
            public string animalPrefab;
        }

        public class FarmSettings
        {
            [JsonProperty("Привилегия для возможности создания фермы")]
            public string perm;

            [JsonProperty("Имя животного")]
            public string animalName;

            [JsonProperty("Каждые сколько секунд даются вещи")]
            public int rewardTime;

            [JsonProperty("Сколько отнимать у животных хп за 1 тик наград (0 - выкл)")]
            public int animalRemoveHp;

            [JsonProperty("Максимум животных на ферме")]
            public int maxAnimals;

            [JsonProperty("Радиус фермы")]
            public int raidus;

            [JsonProperty("Количество поедаемой еды на 1 животного за 1 тик (0 - выкл)")]
            public int foodAmount;

            [JsonProperty("ShortName еды для животного")]
            public string food;

            [JsonProperty("Награды")]
            public List<Reward> rewards;
        }

        public class Reward
        {
            [JsonProperty("Shortname предмета")]
            public string shortName;

            [JsonProperty("Количество")]
            public int amount;

            [JsonProperty("skinID предмета")]
            public ulong skinID;

            [JsonProperty("Имя предмета (если кастом)")]
            public string customName;

            [JsonProperty("Шансы выпадения (1 - 100)")]
            public int chance;
        }

        public class CraftItem
        {
            [JsonProperty("Shortname предмета")]
            public string shortName;

            [JsonProperty("Количество")]
            public int amount;
        }
        #endregion

        #region Localization⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["bear"] = "Bear",
                ["wolf"] = "Wolf",
                ["stag"] = "Stag",
                ["boar"] = "Boar",
                ["UI_MaxFarms"] = "You have created a maximum of farms",
                ["UI_ChooseType"] = "Select the type of farm",
                ["UI_NoPerm"] = "You don't have access to create a farm",
                ["UI_NeedFood"] = "need food",
                ["UI_BoxNo"] = "<color=red> not connected</color>",
                ["UI_BoxYes"] = "<color=green>connected</color>",
                ["UI_TimeReward"] = "reward every {0}m. {1}s.",
                ["UI_CraftTrap"] = "Craft trap",
                ["UI_CraftTrap2"] = "Craft"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["bear"] = "Медведь",
                ["wolf"] = "Волк",
                ["stag"] = "Олень",
                ["boar"] = "Кабан",
                ["UI_MaxFarms"] = "Вы создали максимум ферм",
                ["UI_ChooseType"] = "Выберите тип фермы",
                ["UI_NoPerm"] = "У вас нет привилегий для создания фермы",
                ["UI_NeedFood"] = "нужная еда",
                ["UI_BoxNo"] = "<color=red>не установлен</color>",
                ["UI_BoxYes"] = "<color=green>установлен</color>",
                ["UI_TimeReward"] = "награда каждые {0}м. {1}с.",
                ["UI_CraftTrap"] = "Крафт ловушки",
                ["UI_CraftTrap2"] = "Скрафтить"
            }, this, "ru");
        }
        string GetMsg(string key, BasePlayer player = null) => lang.GetMessage(key, this, player?.UserIDString);
        string GetMsg(string key) => lang.GetMessage(key, this);
        #endregion

        #region [Func]
        private void RemoveAi(BaseNpc npc)
        {
            if (npc == null)
            {
                return;
            }

            npc.CancelInvoke(npc.TickAi);
            var script1 = npc.GetComponent<AiManagedAgent>();
            UnityEngine.Object.Destroy(script1);
            var script2 = npc.GetComponent<AnimalBrain>();
            UnityEngine.Object.Destroy(script2);
            var script3 = npc.GetComponent<NPCNavigator>();
            UnityEngine.Object.Destroy(script3);

            var obj = npc as BaseAnimalNPC;
            if (obj != null)
            {
                AIThinkManager.RemoveAnimal(obj);
            }
        }

        private void AddFarm(HitchTrough farm, string type)
        {
            if (Farms.ContainsKey(farm))
                return;

            foreach(var setting in config.farms)
            {
                if (setting.animalName == type)
                {
                    var comp = farm.gameObject.AddComponent<FarmManager>();
                    comp.Init(setting);
                    Farms.Add(farm, comp);
                    if (!playersFarms.ContainsKey(farm.OwnerID))
                        playersFarms.Add(farm.OwnerID, 1);
                    else
                        playersFarms[farm.OwnerID]++;
                }
            }

            SaveFarms();
        }

        private int HasPlayerItems(BasePlayer player, string shortname)
        {
            var playerHas = 0;
            foreach (var item in player.inventory.containerMain.itemList)
            {
                if (item.info.shortname == shortname && item.skin == 0)
                    playerHas += item.amount;
            }

            foreach (var item in player.inventory.containerBelt.itemList)
            {
                if (item.info.shortname == shortname && item.skin == 0)
                    playerHas += item.amount;
            }

            return playerHas;
        }

        private void RemovePlayerItems(BasePlayer player, string shortname, int amount)
        {
            int count = amount;
            for (int i = 0; i < 24; i++)
            {
                var item = player.inventory.containerMain.GetSlot(i);
                if (item == null)
                    continue;

                if (item.info.shortname == shortname && item.skin == 0)
                {
                    if (item.amount > count)
                    {
                        item.UseItem(count);
                        count = 0;
                    }
                    else if (item.amount < count)
                    {
                        count -= item.amount;
                        item.UseItem(item.amount);
                    }
                    else
                    {
                        item.UseItem(item.amount);
                        count = 0;
                    }

                    if (count == 0)
                        return;
                }
            }

            for (int i = 0; i < 6; i++)
            {
                var item = player.inventory.containerBelt.GetSlot(i);
                if (item == null)
                    continue;

                if (item.info.shortname == shortname && item.skin == 0)
                {
                    if (item.amount > count)
                    {
                        item.UseItem(count);
                        count = 0;
                    }
                    else if (item.amount < count)
                    {
                        count -= item.amount;
                        item.UseItem(item.amount);
                    }
                    else
                    {
                        item.UseItem(item.amount);
                        count = 0;
                    }

                    if (count == 0)
                        return;
                }
            }
        }

        private Dictionary<ulong, string> GetPlayers(string nameOrId)
        {
            var pl = covalence.Players.FindPlayers(nameOrId).ToList();
            return pl.Select(p => new KeyValuePair<ulong, string>(ulong.Parse(p.Id), p.Name)).ToDictionary(x => x.Key, x => x.Value);
        }

        private BasePlayer FindBasePlayer(ulong userId)
        {
            BasePlayer player = BasePlayer.activePlayerList.FirstOrDefault(p => p.userID == userId);
            player = player ?? BasePlayer.sleepingPlayerList.FirstOrDefault(p => p.userID == userId);
            return player;
        }
        #endregion

        #region [Oxide]
        private void Init()
        {
            plugin = this;

            AddCovalenceCommand("ganimal", "GiveAnimal", "myanimalfarm.give.animal");
            AddCovalenceCommand("gtrap", "GiveTrap", "myanimalfarm.give.trap");
        }

        private void Unload()
        {
            SaveFarms(true);
            SaveAnimals();
            config = null;
        }

        private void OnServerInitialized()
        {
            foreach(var cfg in config.traps)
            {
                trapsSkins.Add(cfg.skinID);
                ImageLibrary?.Call("AddImage", cfg.imgUrl, "trap_" + cfg.skinID);
                foreach (var a in cfg.animalsSettings)
                {
                    animalsSkinsPrefabs.Add(a.skinID, a.animalPrefab);
                }
            }
            foreach (var img in config.settings.animalImages)
            {
                ImageLibrary?.Call("AddImage", img.Value, img.Key);
            }

            foreach(var farm in config.farms)
            {
                permission.RegisterPermission(farm.perm, this);
            }

            LoadFarms();
            LoadAnimals();
        }

        private object OnItemPickup(Item item, BasePlayer player)
        {
            if (item.skin == 2644444444)
                return false;

            return null;
        }

        private void OnLootEntity(BasePlayer player, HitchTrough entity)
        {
            CreateFarmUi(player, entity);
        }

        private void OnLootEntityEnd(BasePlayer player, HitchTrough entity)
        {
            CuiHelper.DestroyUi(player, "FarmMain");
        }

        private void OnEntityKill(HitchTrough entity)
        {
            if (entity == null)
                return;

            if(Farms.ContainsKey(entity))
            {
                if (playersFarms.ContainsKey(entity.OwnerID))
                    playersFarms[entity.OwnerID] -= 1;

                Farms.Remove(entity);
            }
        }

        private void OnEntityKill(BaseAnimalNPC entity)
        {
            if (entity == null)
                return;

            if (SavedAnimals.Contains(entity))
                SavedAnimals.Remove(entity);
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (go == null || plan == null)
                return;

            var player = plan?.GetOwnerPlayer();
            if (player == null)
                return;

            var ent = go.ToBaseEntity();
            if (ent == null)
                return;

            if (ent.ShortPrefabName.Contains("box.wooden.large"))
            {
                if (!animalsSkinsPrefabs.ContainsKey(ent.skinID))
                    return;

                
                var animal = GameManager.server.CreateEntity(animalsSkinsPrefabs[ent.skinID], ent.transform.position) as BaseAnimalNPC;
                animal.enableSaving = false;
                animal.Spawn();
                RemoveAi(animal);
                SavedAnimals.Add(animal);
                NextTick(() => ent.Kill());
                return;
            }

            if (!go.name.Contains("beartrap"))
                return;

            var activItem = player.GetActiveItem();

            if (trapsSkins.Contains(activItem.skin))
            {
                var comp = go.AddComponent<TrapManager>();
                comp.Init(activItem.skin);
                ent.enableSaving = false;
            }
        }

        private object CanCombineDroppedItem(DroppedItem item, DroppedItem targetItem)
        {
            if (item.item.skin != targetItem.item.skin)
                return false;

            return null;
        }
        #endregion

        #region [Comp]
        public class TrapManager : MonoBehaviour
        {
            BaseEntity ent;
            BaseAnimalNPC catchAnimal;
            BaseEntity bait;

            private float dist;
            Dictionary<string, Animals> animals = new Dictionary<string, Animals>(); 
            public class Animals
            {
                public ulong Skin;
            }

            public void Init(ulong trapID)
            {
                var trap = plugin.config.traps.FirstOrDefault(s => s.skinID == trapID);
                if (trap == null)
                    return;

                foreach(var animal in trap.animalsSettings)
                    animals.Add(animal.animalName, new Animals() { Skin = animal.skinID});

                ent = gameObject.GetComponent<BaseEntity>();
                InvokeRepeating("Timer", 1f, 1f);
                var Item = ItemManager.CreateByName(trap.bait, 1, 2644444444);;
                bait = Item.Drop(transform.position + new Vector3(0f, 0.2f, 0f), Vector3.zero);
                bait.GetComponent<Rigidbody>().isKinematic = true;
                bait.enableSaving = false;
                catchAnimal = null;
                dist = trap.dist;
            }

            private void Timer()
            {
                if (catchAnimal == null)
                {
                    var findAnimals = new List<BaseAnimalNPC>();
                    Vis.Entities(transform.position, dist, findAnimals);

                    foreach (var animal in findAnimals)
                    {
                        var brain = animal.GetComponent<AnimalBrain>();
                        if (brain == null)
                            continue;

                        if(plugin.PersonalAnimal)
                        {
                            if ((bool)plugin.PersonalAnimal?.Call("IsPersonalAnimal", animal as BaseEntity))
                                continue;
                        }

                        if (animals.ContainsKey(animal.ShortPrefabName))
                        {
                            catchAnimal = animal;
                            return;
                        }
                    }
                }
                else
                {

                    catchAnimal.brain.Navigator.SetDestination(transform.position, BaseNavigator.NavigationSpeed.Normal, 0f, 0f);
                    if (Vector3.Distance(catchAnimal.transform.position, transform.position) < 2f)
                    {
                        List<Connection> connections = new List<Connection>();
                        List<BasePlayer> players = new List<BasePlayer>();
                        Vis.Entities(transform.position, dist, players);

                        foreach (var p in players)
                        {
                            if (!p.IsConnected || !p.IsAlive())
                                continue;

                            connections.Add(p.Connection);
                        }

                        Effect effect1 = new Effect("assets/bundled/prefabs/fx/beartrap/fire.prefab", transform.position, Vector3.up);
                        SendEffect(effect1, connections);
                        
                        var Item = ItemManager.CreateByName("box.wooden.large", 1, animals[catchAnimal.ShortPrefabName].Skin);
                        var ownerPlayer = BasePlayer.FindByID(ent.OwnerID);
                        if (ownerPlayer == null)
                            Item.name = plugin.GetMsg(catchAnimal.ShortPrefabName);
                        else
                            Item.name = plugin.GetMsg(catchAnimal.ShortPrefabName, ownerPlayer);
                        Item.Drop(transform.position, Vector3.zero);
                        plugin.NextTick(() => { ent.Kill(); catchAnimal.Kill(); bait.Kill(); });
                    }
                }
            }

            private void OnDestroy()
            {
                bait.Kill();
                Destroy(this);
            }

            private void SendEffect(Effect effect, List<Connection> connections)
            {
                effect.pooledstringid = StringPool.Get(effect.pooledString);
                if (effect.pooledstringid == 0U)
                {
                    plugin.PrintError("EffectNetwork.Send - unpooled effect name: " + effect.pooledString);
                }
                else
                {
                    NetWrite netWrite = Net.sv.StartWrite();
                    netWrite.PacketID(Message.Type.Effect);
                    effect.WriteToStream(netWrite);
                    netWrite.Send(new SendInfo(connections));
                }
            }
        }

        public class FarmManager : MonoBehaviour
        {
            HitchTrough farmEnt;
            public BoxStorage storage;
            
            public int rewardTick;
            public int foodPerOneTick;
            public string foodName;
            public string foodShortName;
            public int radius;
            public int maxAnimals;
            public int animalRemoveHp;
            public string animalName;
            public List<Reward> rewards;

            private Item RewardItem;
            private int nextReward;

            public void Init(FarmSettings setting)
            {
                farmEnt = gameObject.GetComponent<HitchTrough>();
                farmEnt.inventory.allowedContents = ItemContainer.ContentsType.Generic;
                farmEnt.inventory.onlyAllowedItems = null;
                foodPerOneTick = setting.foodAmount;
                foodShortName = setting.food;
                radius = setting.raidus;
                maxAnimals = setting.maxAnimals;
                animalRemoveHp = setting.animalRemoveHp;
                animalName = setting.animalName;
                rewardTick = setting.rewardTime;
                rewards = setting.rewards;
                nextReward = 0;
                storage = null;


                var storages = new List<BoxStorage>();
                Vis.Entities(transform.position, 3f, storages);
                foreach(var str in storages)
                {
                    if(str != farmEnt && farmEnt.OwnerID == str.OwnerID)
                     storage = str;
                }

                InvokeRepeating("TimerReward", 1f, 1f);
            }

            private void TimerReward()
            {
                nextReward++;
                if (storage == null)
                {
                    var storages = new List<BoxStorage>();
                    Vis.Entities(transform.position, 3f, storages);
                    if (storages.Count <= 1)
                        return;

                    foreach (var str in storages)
                    {
                        if (str != farmEnt && farmEnt.OwnerID == str.OwnerID)
                            storage = str;
                    }
                }

                if (rewardTick > nextReward)
                    return;

                nextReward = 0;
                var findAnimals = new List<BaseAnimalNPC>();
                Vis.Entities(transform.position, radius, findAnimals);
                findAnimals = findAnimals.Where(e => e.ShortPrefabName == animalName && e.GetComponent<AnimalBrain>() == null).ToList();

                if (findAnimals.Count <= 0)
                    return;

                if (findAnimals.Count > maxAnimals)
                    return;


                if (foodPerOneTick > 0)
                {
                    if (!HasFood(findAnimals.Count))
                        return;
                    else
                        RemoveFood(findAnimals.Count);
                }

                foreach (var animal in findAnimals)
                {
                    if (animalRemoveHp > 0)
                    {
                        if (animal.health <= animalRemoveHp)
                            animal.Kill();
                        else
                            animal.Hurt(animalRemoveHp);
                    }

                    foreach(var reward in rewards)
                    {
                        if (UnityEngine.Random.Range(0, 100) > reward.chance)
                            continue;

                        RewardItem = ItemManager.CreateByName(reward.shortName, reward.amount, reward.skinID);
                        if (!String.IsNullOrEmpty(reward.customName)) RewardItem.name = reward.customName;
                        RewardItem.MoveToContainer(storage.inventory);
                    }
                }
            }

            private void OnDestroy()
            {
                Destroy(this);
            }

            private bool HasFood(int animals)
            {
                var amount = 0;
                foreach (var item in farmEnt.inventory.itemList)
                {
                    if (item.info.shortname == foodShortName)
                        amount += item.amount;

                    if (amount >= foodPerOneTick * animals)
                        return true;
                }
                return false;
            }

            private void RemoveFood(int animals)
            {
                int count = foodPerOneTick * animals;
                for (int i = 0; i < 36; i++)
                {
                    var item = farmEnt.inventory.GetSlot(i);
                    if (item == null)
                        continue;

                    if (item.info.shortname == foodShortName)
                    {
                        if (item.amount > count)
                        {
                            item.UseItem(count);
                            count = 0;
                        }
                        else if (item.amount < count)
                        {
                            count -= item.amount;
                            item.UseItem(item.amount);
                        }
                        else
                        {
                            item.UseItem(item.amount);
                            count = 0;
                        }

                        if (count == 0)
                            return;
                    }
                }
            }
        }
        #endregion

        #region [Command]
        private void GiveTrap(IPlayer player, string command, string[] args)
        {
            if (args.Length < 2)
            {
                player.Message($"Error command \n/gtrap \"name|steamid\" \"SkinId\" ");
                return;
            }
            var recivers = GetPlayers(args[0]);
            if (recivers == null || recivers.Count == 0)
            {
                player.Message($"Player not found");
                return;
            }
            if (recivers.Count > 1)
            {
                player.Message($"Plyers found {string.Join("\n", recivers.Select(p => $"{p.Value} ({p.Key})").ToArray())}");
                return;
            }
            var Ireciver = recivers.First();
            var reciver = FindBasePlayer(Ireciver.Key);
            if (reciver == null)
            {
                player.Message($"Player not online");
                return;
            }

            var skinid = Convert.ToUInt64(args[1]);

            var trap = config.traps.Where(t => t.skinID == skinid).FirstOrDefault();
            if (trap == null)
            {
                player.Message($"Trap not found");
                return;
            }

            var Item = ItemManager.CreateByName("trap.bear", 1, trap.skinID);
            Item.name = trap.name;
            reciver.GiveItem(Item);
        }

        private void GiveAnimal(IPlayer player, string command, string[] args)
        {
            if (args.Length < 2)
            {
                player.Message($"Error command \n/gtrap \"name|steamid\" \"SkinId\" ");
                return;
            }
            var recivers = GetPlayers(args[0]);
            if (recivers == null || recivers.Count == 0)
            {
                player.Message($"Player not found");
                return;
            }
            if (recivers.Count > 1)
            {
                player.Message($"Plyers found {string.Join("\n", recivers.Select(p => $"{p.Value} ({p.Key})").ToArray())}");
                return;
            }
            var Ireciver = recivers.First();
            var reciver = FindBasePlayer(Ireciver.Key);
            if (reciver == null)
            {
                player.Message($"Player not online");
                return;
            }

            var skinid = Convert.ToUInt64(args[1]);

            bool found = false;

            foreach(var trap in config.traps)
            {
                foreach(var animal in trap.animalsSettings)
                {
                    if(animal.skinID == skinid)
                    {
                        var Item = ItemManager.CreateByName("box.wooden.large", 1, animal.skinID);
                        Item.name = plugin.GetMsg(animal.animalName, reciver);
                        reciver.GiveItem(Item);
                        return;
                    }
                }
            }

            if (!found)
            {
                player.Message($"Animal not found");
                return;
            }
        }
        #endregion

        #region [Data]
        private void SaveAnimals()
        {
            List<AnimalData> data = new List<AnimalData>();

            foreach(var animal in SavedAnimals)
            {
                if (animal == null || !animal.IsAlive())
                    continue;

                data.Add(new AnimalData() { Health = animal.health, pos = animal.transform.position, prefab = animal.PrefabName });
                animal.Kill();
            }

            Interface.Oxide.DataFileSystem.WriteObject($"MyAnimalFarms/animals", data);
        }

        private void LoadAnimals()
        {
            List<AnimalData> data = new List<AnimalData>();
            data = Interface.Oxide.DataFileSystem.ReadObject<List<AnimalData>>($"MyAnimalFarms/animals");

            foreach(var animal in data)
            {
                var ent = GameManager.server.CreateEntity(animal.prefab, animal.pos) as BaseAnimalNPC;
                ent.enableSaving = false;
                ent.Spawn();
                ent.SetHealth(animal.Health);
                RemoveAi(ent);
                SavedAnimals.Add(ent);
            }
        }

        private void LoadFarms()
        {
            List<Data> data = new List<Data>();
            data = Interface.Oxide.DataFileSystem.ReadObject<List<Data>>($"MyAnimalFarms/data");


            foreach (var farm in data)
            {
                var ents = new List<HitchTrough>();
                Vis.Entities(farm.pos, 0.1f, ents);
                var ent = ents.FirstOrDefault();
                if (ent == null)
                    continue;
                AddFarm(ent, farm.type);
            }
        }

        private void SaveFarms(bool destoyComp = false)
        {
            List<Data> data = new List<Data>();
            foreach (var farm in Farms)
            {
                if (farm.Key == null)
                    continue;

                if (farm.Value == null)
                    continue;

                data.Add(new Data() { pos = farm.Key.transform.position, type = farm.Value.animalName });


                if (destoyComp)
                    UnityEngine.Object.Destroy(farm.Value);
            }

            Interface.Oxide.DataFileSystem.WriteObject($"MyAnimalFarms/data", data);
        }
        #endregion

        #region [UI]
        private void CreateFarmUi(BasePlayer player, HitchTrough hitch)
        {
            if (hitch == null)
                return;

            if (player.userID != hitch.OwnerID)
                return;

            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiElement { Parent = "Overlay", Name = "FarmMain", Components = { new CuiImageComponent { Color = "0 0 0 0" }, new CuiRectTransformComponent { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-200 370", OffsetMax = "180 650" }, new CuiNeedsCursorComponent() } });


            UI.CreatePanel(ref container, "FarmPanel", "FarmMain", "0 0 0 0", "0 0", "1 1");
            if (!Farms.ContainsKey(hitch))
            {
                if(playersFarms.ContainsKey(player.userID))
                {
                    if(playersFarms[player.userID] >= config.settings.maxFarmsPerPlayer)
                    {
                        UI.CreateTextOutLine(ref container, "FarmPanel", GetMsg("UI_MaxFarms", player), "1 1 1 0.6", $"0 0", $"1 1", TextAnchor.MiddleCenter, 24);


                        CuiHelper.DestroyUi(player, "FarmMain");
                        CuiHelper.AddUi(player, container);
                        return;
                    }
                }

                UI.CreateTextOutLine(ref container, "FarmPanel", GetMsg("UI_ChooseType", player), "1 1 1 0.6", $"0 0.8", $"1 1", TextAnchor.MiddleCenter, 18);

                int i = 0;
                int j = 0;

                foreach (var farm in config.farms)
                {
                    if (!permission.UserHasPermission(player.UserIDString, farm.perm) && !player.IsAdmin)
                        continue;

                    UI.CreateImage(ref container, "img_" + farm.animalName, "FarmPanel", "0 0 0 1", farm.animalName, $"{0.2 + i * 0.4} {0.56 - j * 0.3}", $"{0.4 + i * 0.42} {0.8 - j * 0.3}");
                    UI.CreateTextOutLine(ref container, "img_" + farm.animalName, GetMsg(farm.animalName, player), "1 1 1 0.4", $"0 0", $"1 1", TextAnchor.MiddleCenter, 14);
                    UI.CreateButton(ref container, "img_" + farm.animalName, "0 0 0 0", "", 18, "0 0", "1 1", $"UI_CREATE_FARM {farm.animalName}");

                    i++;
                    if (i == 2)
                    {

                        i = 0;
                        j++;
                    }
                }

                if(i == 0 && j == 0)
                    UI.CreateTextOutLine(ref container, "FarmPanel", GetMsg("UI_NoPerm", player), "1 1 1 0.6", $"0 0", $"1 1", TextAnchor.MiddleCenter, 24);

                CuiHelper.DestroyUi(player, "FarmMain");
                CuiHelper.AddUi(player, container);
                return;
            }

            var comp = Farms[hitch];

            var findAnimals = new List<BaseAnimalNPC>();
            Vis.Entities(hitch.transform.position, comp.radius, findAnimals);
            findAnimals = findAnimals.Where(e => e.ShortPrefabName == comp.animalName && e.GetComponent<AnimalBrain>() == null).ToList();

            UI.CreateImage(ref container, comp.animalName, "FarmPanel", "0 0 0 1", comp.animalName, "0.4 0.75", "0.62 0.98");
            UI.CreateTextOutLine(ref container, comp.animalName, comp.maxAnimals < findAnimals.Count ? GetMsg(comp.animalName, player) + $" <color=red>{findAnimals.Count} | {comp.maxAnimals}</color>" : GetMsg(comp.animalName, player) + $" <color=green>{findAnimals.Count} | {comp.maxAnimals}</color>", "1 1 1 0.4", $"0 0", $"1 1", TextAnchor.MiddleCenter, 14);
            UI.CreateButton(ref container, comp.animalName, "0 0 0 0", "", 18, "0 0", "1 1", $"UI_REMOVE_FARM");

            var time = TimeSpan.FromSeconds(comp.rewardTick);
            UI.CreateTextOutLine(ref container, "FarmPanel", String.Format(GetMsg("UI_TimeReward", player), time.Minutes, time.Seconds), "1 1 1 0.4", $"0 0.6", $"1 0.76", TextAnchor.MiddleCenter, 16);


            UI.CreateImage(ref container, "Box", "FarmPanel", "1 1 1 1", "box.wooden.large", "0.057 0.45", "0.179 0.61");
            UI.CreateTextOutLine(ref container, "FarmPanel", comp.storage == null ? GetMsg("UI_BoxNo", player) : GetMsg("UI_BoxYes", player), "1 1 1 0.4", $"0.2 0.45", $"0.7 0.61", TextAnchor.MiddleLeft, 16);

            UI.CreateImage(ref container, "Box", "FarmPanel", "1 1 1 1", "box.wooden.large", "0.057 0.45", "0.179 0.61");
            UI.CreateTextOutLine(ref container, "FarmPanel", comp.storage == null ? GetMsg("UI_BoxNo", player) : GetMsg("UI_BoxYes", player), "1 1 1 0.4", $"0.2 0.45", $"0.7 0.61", TextAnchor.MiddleLeft, 16);

            if (comp.foodPerOneTick > 0)
            {
                UI.CreateImage(ref container, comp.foodShortName, "FarmPanel", "1 1 1 1", comp.foodShortName, "0.057 0.31", "0.179 0.47");
                UI.CreateTextOutLine(ref container, "FarmPanel", GetMsg("UI_NeedFood", player), "1 1 1 0.4", $"0.2 0.31", $"0.7 0.47", TextAnchor.MiddleLeft, 16);
            }

            foreach(var trap in config.traps)
            {
                if (!trap.canCraft)
                    continue;

                foreach(var animal in trap.animalsSettings)
                {
                    if(animal.animalName == comp.animalName)
                    {
                        UI.CreateImage(ref container, "Trap", "FarmPanel", "1 1 1 1", "trap_" + trap.skinID, "0.4 0.01", "0.62 0.26");
                        UI.CreateTextOutLine(ref container, "Trap", GetMsg("UI_CraftTrap", player), "1 1 1 0.4", $"0 0", $"1 1", TextAnchor.MiddleCenter, 14);
                        UI.CreateButton(ref container, "Trap", "0 0 0 0", "", 18, "0 0", "1 1", $"UI_CREATE_TRAP show {trap.skinID}");
                    }
                }
            }

            CuiHelper.DestroyUi(player, "FarmMain");
            CuiHelper.AddUi(player, container);
        }

        private void CreateCraftTrap(BasePlayer player, ulong id)
        {
            var trap = config.traps.Where(t => t.skinID == id).FirstOrDefault();
            if (trap == null)
                return;

            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiElement { Parent = "Overlay", Name = "FarmMain", Components = { new CuiImageComponent { Color = "0 0 0 0" }, new CuiRectTransformComponent { AnchorMin = "0.5 0", AnchorMax = "0.5 0", OffsetMin = "-200 370", OffsetMax = "180 650" }, new CuiNeedsCursorComponent() } });
            UI.CreatePanel(ref container, "FarmPanel", "FarmMain", "0 0 0 0", "0 0", "1 1");


            int i = 0;
            int j = 0;
            foreach(var cItem in trap.needItemsToCraft)
            {
                UI.CreateImage(ref container, "img_" + cItem.shortName, "FarmPanel", "1 1 1 1", cItem.shortName, $"{0.2 + i * 0.2} {0.56 - j * 0.2}", $"{0.35 + i * 0.2} {0.74 - j * 0.2}");
                UI.CreateTextOutLine(ref container, "img_" + cItem.shortName, $"{HasPlayerItems(player, cItem.shortName)} | {cItem.amount}", "1 1 1 0.4", $"0 0", $"1 1", TextAnchor.LowerRight, 14);

                i++;
                if (i == 3)
                {

                    i = 0;
                    j++;
                }
            }

            UI.CreateImage(ref container, "Trap", "FarmPanel", "1 1 1 1", "hammer", "0.4 0.01", "0.62 0.26");
            UI.CreateTextOutLine(ref container, "Trap", GetMsg("UI_CraftTrap2", player), "1 1 1 0.4", $"0 0", $"1 1", TextAnchor.MiddleCenter, 14);
            UI.CreateButton(ref container, "Trap", "0 0 0 0", "", 18, "0 0", "1 1", $"UI_CREATE_TRAP craft {trap.skinID}");

            UI.CreateTextOutLine(ref container, "FarmPanel", "<", "1 1 1 0.4", "0.1 0.01", "0.2 0.15", TextAnchor.MiddleCenter, 24, "return");
            UI.CreateButton(ref container, "return", "0 0 0 0", "", 18, "0 0", "1 1", $"UI_RETURN");

            CuiHelper.DestroyUi(player, "FarmMain");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("UI_CREATE_FARM")]
        private void cmd_UI_CREATE_FARM(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            var ent = player.inventory.loot.entitySource as HitchTrough;
            if (ent == null)
                return;

            var type = arg.Args[0];

            AddFarm(ent, type);
            CreateFarmUi(player, ent);
        }

        [ConsoleCommand("UI_REMOVE_FARM")]
        private void cmd_UI_REMOVE_FARM(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            var ent = player.inventory.loot.entitySource as HitchTrough;
            if (ent == null)
                return;

            if (!Farms.ContainsKey(ent))
                return;

            var farm = Farms[ent];
            UnityEngine.Object.Destroy(farm);
            Farms.Remove(ent);
            if (playersFarms.ContainsKey(player.userID))
                playersFarms[player.userID] -= 1;
            CreateFarmUi(player, ent);
        }

        [ConsoleCommand("UI_RETURN")]
        private void cmd_UI_RETURN(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            var ent = player.inventory.loot.entitySource as HitchTrough;
            if (ent == null)
            {
                CuiHelper.DestroyUi(player, "FarmMain");
                return;
            }

            CreateFarmUi(player, ent);
        }

        [ConsoleCommand("UI_CREATE_TRAP")]
        private void cmd_UI_CREATE_TRAP(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            var act = arg.Args[0];
            var id = Convert.ToUInt64(arg.Args[1]);
            if (act == "show")
            {
                CreateCraftTrap(player, id);
                return;
            }

            if(act == "craft")
            {
                var trap = config.traps.Where(t => t.skinID == id).FirstOrDefault();
                if (trap == null)
                    return;

                foreach(var item in trap.needItemsToCraft)
                {
                    if (HasPlayerItems(player, item.shortName) < item.amount)
                        return;
                }

                foreach (var item in trap.needItemsToCraft)
                    RemovePlayerItems(player, item.shortName, item.amount);

                var Item = ItemManager.CreateByName("trap.bear", 1, trap.skinID);
                Item.name = trap.name;
                player.GiveItem(Item);

                Effect effect = new Effect("assets/bundled/prefabs/fx/notice/item.select.fx.prefab", player, 0, new Vector3(), new Vector3());
                EffectNetwork.Send(effect, player.Connection);
                CreateCraftTrap(player, id);
            }
        }
        #endregion

        #region [UI generator]
        public class UI
        {
            public static void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter, string name = "button", float FadeIn = 0f)
            {

                container.Add(new CuiButton
                {

                    Button = { Color = color, Command = command, FadeIn = FadeIn },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax},
                    Text = { Text = text, FontSize = size, Align = align }

                },
                panel, name);
            }

            public static void CreatePanel(ref CuiElementContainer container, string name, string parent, string color, string aMin, string aMax, float Fadeout = 0f, float Fadein = 0f)
            {

                container.Add(new CuiElement
                {
                    Name = name,
                    Parent = parent,
                    Components =
                {
                    new CuiImageComponent { Color = color, FadeIn = Fadein },
                    new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax}
                },
                    FadeOut = Fadeout
                });
            }

            public static void CreatePanelBlur(ref CuiElementContainer container, string name, string parent, string color, string aMin, string aMax, float Fadeout = 0f, float Fadein = 0f)
            {
                container.Add(new CuiPanel()
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Image = { Color = color, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", FadeIn = Fadein },
                    FadeOut = Fadeout
                }, parent, name);
            }
            public static void CreateText(ref CuiElementContainer container, string parent, string text, string color, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleLeft, int size = 14, string name = "name", float Fadein = 0f)
            {
                container.Add(new CuiElement
                {
                    Parent = parent,
                    Name = name,
                    Components =
                {
                    new CuiTextComponent(){ Color = color, Text = text, FontSize = size, Align = align, FadeIn = Fadein },
                    new CuiRectTransformComponent{ AnchorMin =  aMin ,AnchorMax = aMax }
                }
                });
            }

            public static void CreateTextOutLine(ref CuiElementContainer container, string parent, string text, string color, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleLeft, int size = 14, string name = "name", float Fadein = 0f)
            {
                container.Add(new CuiElement
                {
                    Parent = parent,
                    Name = name,
                    Components =
                {
                    new CuiTextComponent(){ Color = color, Text = text, FontSize = size, Align = align, FadeIn = Fadein },
                    new CuiRectTransformComponent{ AnchorMin =  aMin ,AnchorMax = aMax },
                    new CuiOutlineComponent{ Color = "0 0 0 1" }
                }
                });
            }


            public static void CreateImage(ref CuiElementContainer container, string name, string panel, string color, string image, string aMin, string aMax, float Fadeout = 0f, float Fadein = 0f, ulong skin = 0)
            {
                container.Add(new CuiElement
                {
                    Name = name,
                    Parent = panel,
                    Components =
                {
                    new CuiRawImageComponent { Color = color, Png = (string)plugin.ImageLibrary.Call("GetImage", image, skin), FadeIn = Fadein },
                    new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax },

                },
                    FadeOut = Fadeout
                });
            }
        }
        #endregion
    }
}
