using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("AnimalLoot", "Drop Dead", "1.0.3")]
    public class AnimalLoot : RustPlugin
    {
        private static AnimalLoot _ins;
        public Dictionary<ulong, bool> Taked = new Dictionary<ulong, bool>();

        #region Config [Конфигурация плагина]

        private PluginConfig cfg;

        public class random
        {
            [JsonProperty("Минимальное количество")]
            public int min;
            [JsonProperty("Максимальное количество")]
            public int max;
        }

        public class PluginConfig
        {
            [JsonProperty("Настройки желудка с кабана")]
            public BoarSettings boar = new BoarSettings();
            [JsonProperty("Настройки желудка с медведя")]
            public BearSettings bear = new BearSettings();
            [JsonProperty("Настройки желудка с оленя")]
            public DeerSettings deer = new DeerSettings();
            [JsonProperty("Настройки желудка с волка")]
            public WolfSettings wolf = new WolfSettings();

            public class BoarSettings
            {
                [JsonProperty("Включить выпадение желудка?")]
                public bool enable = true;
                [JsonProperty("Скин желудка")]
                public ulong skinid = 2775209553;
                [JsonProperty("Название выпадающего предмета (желудка)")]
                public string displayname = "Желудок кабана с камнями";
                [JsonProperty("Шанс выпадения желудка")]
                public int chance = 100;

                [JsonProperty("Предметы которые могут выпасть при переработке", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public Dictionary<string, random> itemsdrop = new Dictionary<string, random>
                {
                    ["sulfur"] = new random { min = 10, max = 50}, 
                    ["metal.fragments"] = new random { min = 50, max = 150},
                };
            }

            public class BearSettings
            {
                [JsonProperty("Включить выпадение желудка?")]
                public bool enable = true;
                [JsonProperty("Скин желудка")]
                public ulong skinid = 2775208793;
                [JsonProperty("Название выпадающего предмета (желудка)")]
                public string displayname = "Желудок медведя с камнями";
                [JsonProperty("Шанс выпадения желудка")]
                public int chance = 100;

                [JsonProperty("Предметы которые могут выпасть при переработке", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public Dictionary<string, random> itemsdrop = new Dictionary<string, random>
                {
                    ["sulfur"] = new random { min = 10, max = 50}, 
                    ["metal.fragments"] = new random { min = 50, max = 150},
                };
            }

            public class DeerSettings
            {
                [JsonProperty("Включить выпадение желудка?")]
                public bool enable = true;
                [JsonProperty("Скин желудка")]
                public ulong skinid = 2775208865;
                [JsonProperty("Название выпадающего предмета (желудка)")]
                public string displayname = "Желудок оленя с камнями";
                [JsonProperty("Шанс выпадения желудка")]
                public int chance = 100;

                [JsonProperty("Предметы которые могут выпасть при переработке", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public Dictionary<string, random> itemsdrop = new Dictionary<string, random>
                {
                    ["sulfur"] = new random { min = 10, max = 50}, 
                    ["metal.fragments"] = new random { min = 50, max = 150},
                };
            }

            public class WolfSettings
            {
                [JsonProperty("Включить выпадение желудка?")]
                public bool enable = true;
                [JsonProperty("Скин желудка")]
                public ulong skinid = 2775209677;
                [JsonProperty("Название выпадающего предмета (желудка)")]
                public string displayname = "Желудок волка с камнями";
                [JsonProperty("Шанс выпадения желудка")]
                public int chance = 100;

                [JsonProperty("Предметы которые могут выпасть при переработке", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public Dictionary<string, random> itemsdrop = new Dictionary<string, random>
                {
                    ["sulfur"] = new random { min = 10, max = 50}, 
                    ["metal.fragments"] = new random { min = 50, max = 150},
                };
            }
        }

        private void Init()
        {
            cfg = Config.ReadObject<PluginConfig>();
            Config.WriteObject(cfg);
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new PluginConfig(), true);
        }

        #endregion

        #region Hooks

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (entity == null || entity.ToPlayer() == null || dispenser == null || item == null) return;
            var player = entity.ToPlayer();
            if (player == null) return;
            if (Taked.ContainsKey(player.userID)) return;
            var activeitem = player.GetActiveItem();
            if (activeitem == null) return;

            string type = "";
            if (item.info.shortname == "meat.boar") type = "boar";
            if (item.info.shortname == "bearmeat") type = "bear";
            if (item.info.shortname == "deermeat.raw") type = "deer";
            if (item.info.shortname == "wolfmeat.raw") type = "wolf";
			if (string.IsNullOrEmpty(type)) return;
			
            if (type == "boar" && cfg.boar.enable)
            {
                if (UnityEngine.Random.Range(0, 100) < cfg.boar.chance)
                {
                    Item newItem = ItemManager.CreateByName("coal", 1, cfg.boar.skinid);
                    if (newItem == null) return;
                    newItem.name = cfg.boar.displayname;

                    player.GiveItem(newItem);
                    player.ChatMessage($"Вы нашли <color=orange>{cfg.boar.displayname.ToLower()}</color>");

                    Taked.Add(player.userID, true);
                    timer.Once(2f, () => { Taked.Remove(player.userID); });
                }
            }

            if (type == "bear" && cfg.bear.enable)
            {
                if (UnityEngine.Random.Range(0, 100) < cfg.bear.chance)
                {
                    Item newItem = ItemManager.CreateByName("coal", 1, cfg.bear.skinid);
                    if (newItem == null) return;
                    newItem.name = cfg.bear.displayname;

                    player.GiveItem(newItem);
                    player.ChatMessage($"Вы нашли <color=orange>{cfg.bear.displayname.ToLower()}</color>");

                    Taked.Add(player.userID, true);
                    timer.Once(2f, () => { Taked.Remove(player.userID); });
                }
            }

            if (type == "deer" && cfg.deer.enable )
            {
                if (UnityEngine.Random.Range(0, 100) < cfg.deer.chance)
                {
                    Item newItem = ItemManager.CreateByName("coal", 1, cfg.deer.skinid);
                    if (newItem == null) return;
                    newItem.name = cfg.deer.displayname;

                    player.GiveItem(newItem);
                    player.ChatMessage($"Вы нашли <color=orange>{cfg.deer.displayname.ToLower()}</color>");

                    Taked.Add(player.userID, true);
                    timer.Once(2f, () => { Taked.Remove(player.userID); });
                }
            }

            if (type == "wolf" && cfg.wolf.enable)
            {
                if (UnityEngine.Random.Range(0, 100) < cfg.wolf.chance)
                {
                    Item newItem = ItemManager.CreateByName("coal", 1, cfg.wolf.skinid);
                    if (newItem == null) return;
                    newItem.name = cfg.wolf.displayname;

                    player.GiveItem(newItem);
                    player.ChatMessage($"Вы нашли <color=orange>{cfg.wolf.displayname.ToLower()}</color>");

                    Taked.Add(player.userID, true);
                    timer.Once(2f, () => { Taked.Remove(player.userID); });
                }
            }
        }

        void CanRecycle(Recycler recycler, Item item)
        {
            if (item.info.shortname == "coal")
            {
                recycler.StartRecycling();
                return;
            }
        }
        object OnRecycleItem(Recycler recycler, Item item)
        {
            if (item.skin == cfg.boar.skinid)
            {
                foreach (var items in cfg.boar.itemsdrop)
                {
                    Item itemcreate = ItemManager.CreateByName(items.Key, UnityEngine.Random.Range(items.Value.min, items.Value.max));
                    recycler.MoveItemToOutput(itemcreate);
                }
                item.RemoveFromWorld(); 
                item.RemoveFromContainer();
                return false;
            }
            if (item.skin == cfg.bear.skinid)
            {
                foreach (var items in cfg.bear.itemsdrop)
                {
                    Item itemcreate = ItemManager.CreateByName(items.Key, UnityEngine.Random.Range(items.Value.min, items.Value.max));
                    recycler.MoveItemToOutput(itemcreate);
                }
                item.RemoveFromWorld(); 
                item.RemoveFromContainer();
                return false;
            }
            if (item.skin == cfg.deer.skinid)
            {
                foreach (var items in cfg.deer.itemsdrop)
                {
                    Item itemcreate = ItemManager.CreateByName(items.Key, UnityEngine.Random.Range(items.Value.min, items.Value.max));
                    recycler.MoveItemToOutput(itemcreate);
                }
                item.RemoveFromWorld(); 
                item.RemoveFromContainer();
                return false;
            }
            if (item.skin == cfg.wolf.skinid)
            {
                foreach (var items in cfg.wolf.itemsdrop)
                {
                    Item itemcreate = ItemManager.CreateByName(items.Key, UnityEngine.Random.Range(items.Value.min, items.Value.max));
                    recycler.MoveItemToOutput(itemcreate);
                }
                item.RemoveFromWorld(); 
                item.RemoveFromContainer();
                return false;
            }
            return null;
        }

        #endregion
    }
}