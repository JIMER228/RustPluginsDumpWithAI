// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("AdvancedRates", "Drop Dead", "1.0.21")]
    public class AdvancedRates : RustPlugin
    {
        #region Var [Определения]

        public List<uint> LootedBoxes = new List<uint>();

        #endregion

        #region Data [Работа с датой]

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Title}/LootedBoxes", LootedBoxes);
        }

        private void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile($"{Title}/LootedBoxes"))
                LootedBoxes = Interface.Oxide.DataFileSystem.ReadObject<List<uint>>($"{Title}/LootedBoxes");
        }

        private void WipeData()
        {
            LootedBoxes.Clear();
        }

        #endregion

        #region Config [Конфигурация плагина]

        private PluginConfig cfg;

        public class PluginConfig
        {
            [JsonProperty("Настройки рейтов фарма")]
            public GatherSettings gather = new GatherSettings();
            [JsonProperty("Настройки рейтов на бонус-добычу")]
            public BonusSettings bonus = new BonusSettings();
            [JsonProperty("Настройки рейтов подбора")]
            public CollectibleSettings collectible = new CollectibleSettings();
            [JsonProperty("Настройки рейтов компонтентов (ящики)")]
            public ComponentsSettings components = new ComponentsSettings();
            [JsonProperty("Настройки рейтов компонтентов (бочки)")]
            public BarrelsSettings barrels = new BarrelsSettings();

            public class GatherSettings
            {
                [JsonProperty("Включить рейты на добычу (камни, деревья)?")]
                public bool enablegatherrate = true;
                [JsonProperty("Рейты на добычу")]
                public float gatherrate = 7f;
                [JsonProperty("Список запрещенных предметов (рейты не будут распространятся на них)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> blacklist = new List<string>()
                {
                    { "example" },
                    { "example" }
                };
                [JsonProperty("Пермишны для увеличения рейтов на добычу", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public Dictionary<string, float> perms = new Dictionary<string, float>
                {
                    ["advancedrates.vip"] = 7f,
                    ["advancedrates.prem"] = 9f
                };
            }

            public class BonusSettings
            {
                [JsonProperty("Включить рейты на бонус-добычу (бонус дается в конце фарма)?")]
                public bool enablebonusrate = true;
                [JsonProperty("Рейты на бонус-добычу")]
                public float bonusrate = 7f;
                [JsonProperty("Список запрещенных предметов (рейты не будут распространятся на них)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> blacklist = new List<string>()
                {
                    { "example" },
                    { "example" }
                };
                [JsonProperty("Пермишны для увеличения рейтов на бонус-добычу", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public Dictionary<string, float> perms = new Dictionary<string, float>
                {
                    ["advancedrates.vip"] = 7f,
                    ["advancedrates.prem"] = 9f
                };
            }

            public class CollectibleSettings
            {
                [JsonProperty("Включить рейты на подбор (collectible ресурсы)?")]
                public bool enablepickuprate = true;
                [JsonProperty("Рейты на подбор")]
                public float pickuprate = 7f;
                [JsonProperty("Список запрещенных предметов (рейты не будут распространятся на них)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> blacklist = new List<string>()
                {
                    { "example" },
                    { "example" }
                };
                [JsonProperty("Пермишны для увеличения рейтов на подбор", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public Dictionary<string, float> perms = new Dictionary<string, float>
                {
                    ["advancedrates.vip"] = 7f,
                    ["advancedrates.prem"] = 9f
                };
            }

            public class ComponentsSettings
            {
                [JsonProperty("Включить рейты на компоненты?")]
                public bool enablecomponentsrate = true;
                [JsonProperty("Рейты на компоненты отдельно для каждого ящика", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public Dictionary<string, float> rate = new Dictionary<string, float>
                {
                    ["bradley_crate"] = 5f,
                    ["heli_crate"] = 5f,
                    ["crate_basic"] = 7f,
                    ["crate_elite"] = 7f,
                    ["crate_mine"] = 5f,
                    ["crate_normal"] = 7f,
                    ["crate_normal_2"] = 7f,
                    ["crate_normal_2_food"] = 5f,
                    ["crate_normal_2_medical"] = 7f,
                    ["crate_tools"] = 5f,
                    ["crate_underwater_advanced"] = 7f,
                    ["crate_underwater_basic"] = 5f,
                    ["codelockedhackablecrate"] = 9f,
                    ["codelockedhackablecrate_oilrig"] = 7f,
                };
                [JsonProperty("Список запрещенных предметов (рейты не будут распространятся на них в любом ящике)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> blacklist = new List<string>()
                {
                    { "example" },
                    { "example" },
                };
                [JsonProperty("Пермишны для увеличения рейтов на компоненты", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public Dictionary<string, float> perms = new Dictionary<string, float>
                {
                    ["advancedrates.vip"] =7f,
                    ["advancedrates.prem"] = 9f
                };
            }

            public class BarrelsSettings
            {
                [JsonProperty("Включить рейты на компоненты с бочкек?")]
                public bool enablebarrelsrate = true;
                [JsonProperty("Рейты на компоненты отдельно для каждой бочки", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public Dictionary<string, float> rate = new Dictionary<string, float>
                {
                    ["loot-barrel-1"] = 7f,
                    ["loot-barrel-2"] = 7f,
                    ["loot_barrel_1"] = 7f,
                    ["loot_barrel_2"] = 7f,
                    ["oil_barrel"] = 7f
                };
                [JsonProperty("Список запрещенных предметов (рейты не будут распространятся на них в любой бочке)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public List<string> blacklist = new List<string>()
                {
                    { "example" },
                    { "example" }
                };
                [JsonProperty("Пермишны для увеличения рейтов на бочки", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public Dictionary<string, float> perms = new Dictionary<string, float>
                {
                    ["advancedrates.vip"] = 7f,
                    ["advancedrates.prem"] = 9f
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

        #region Methods [Методы]

        float GetPlayerRate(string playerid, string type, string crate)
        {
            float gatherrate = cfg.gather.gatherrate;
            float bonusrate = cfg.bonus.bonusrate;
            float collectiblerate = cfg.collectible.pickuprate;
            float cratesrate = 1f;
            float barrelsrate = 1f;

            if (type == "gather")
            {
                foreach (var num in cfg.gather.perms)
                {
                    if (permission.UserHasPermission(playerid, num.Key))
                    {
                        if (num.Value > gatherrate) gatherrate = num.Value;
                    }
                }
                return gatherrate;
            }
            if (type == "bonus")
            {
                foreach (var num in cfg.bonus.perms)
                {
                    if (permission.UserHasPermission(playerid, num.Key))
                    {
                        if (num.Value > bonusrate) bonusrate = num.Value;
                    }
                }
                return bonusrate;
            }
            if (type == "collectible")
            {
                foreach (var num in cfg.collectible.perms)
                {
                    if (permission.UserHasPermission(playerid, num.Key))
                    {
                        if (num.Value > collectiblerate) collectiblerate = num.Value;
                    }
                }
                return collectiblerate;
            }
            if (type == "components")
            {
                cratesrate = cfg.components.rate[crate];
                foreach (var num in cfg.components.perms)
                {
                    if (permission.UserHasPermission(playerid, num.Key))
                    {
                        if (num.Value > cratesrate) cratesrate = num.Value;
                    }
                }
                return cratesrate;
            }
            if (type == "barrel")
            {
                barrelsrate = cfg.barrels.rate[crate];
                foreach (var num in cfg.barrels.perms)
                {
                    if (permission.UserHasPermission(playerid, num.Key))
                    {
                        if (num.Value > cratesrate) barrelsrate = num.Value;
                    }
                }
                return barrelsrate;
            }

            return 5f;
        }

        #endregion

        #region Hooks [Хуки]

        void OnNewSave()
        {
            WipeData();
        }

        void OnServerSave()
        {
            SaveData();
        }

        void Unload()
        {
            SaveData();
        }

        void OnServerInitialized()
        {
            LoadData();
            foreach (var perm in cfg.gather.perms) if (!permission.PermissionExists(perm.Key)) permission.RegisterPermission(perm.Key, this);
            foreach (var perm in cfg.bonus.perms) if (!permission.PermissionExists(perm.Key)) permission.RegisterPermission(perm.Key, this);
            foreach (var perm in cfg.collectible.perms) if (!permission.PermissionExists(perm.Key)) permission.RegisterPermission(perm.Key, this);
            foreach (var perm in cfg.components.perms) if (!permission.PermissionExists(perm.Key)) permission.RegisterPermission(perm.Key, this);
            foreach (var perm in cfg.barrels.perms) if (!permission.PermissionExists(perm.Key)) permission.RegisterPermission(perm.Key, this);
        }

        void OnCollectiblePickup(CollectibleEntity collectible, BasePlayer player)
        {
            if (player == null || collectible == null || !cfg.collectible.enablepickuprate) return;
            foreach (var item in collectible.itemList)
            {
                if (cfg.collectible.blacklist.Contains(item.itemDef.shortname) || item.itemDef.shortname.Contains("clone") || item.itemDef.shortname.Contains("seed")) return;
                item.amount = (int)(Convert.ToDouble(item.amount) * GetPlayerRate(player.UserIDString, "collectible", ""));
            }
        }

        void OnGrowableGathered(GrowableEntity collectible, Item item, BasePlayer player)
        {
            if (player == null || item == null || collectible == null || !cfg.collectible.enablepickuprate) return;
            if (cfg.collectible.blacklist.Contains(item.info.shortname) || item.info.shortname.Contains("clone") || item.info.shortname.Contains("seed")) return;
            item.amount = (int)(Convert.ToDouble(item.amount) * GetPlayerRate(player.UserIDString, "collectible", ""));
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (item == null || player == null || dispenser == null || !cfg.bonus.enablebonusrate) return;
            if (cfg.bonus.blacklist.Contains(item.info.shortname)) return;
            item.amount = (int)(Convert.ToDouble(item.amount) * GetPlayerRate(player.UserIDString, "bonus", ""));
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (dispenser == null || entity == null || item == null || !cfg.gather.enablegatherrate) return;
            if (!entity.ToPlayer()) return;
            var player = entity.ToPlayer();
            if (player == null) return;
            if (cfg.gather.blacklist.Contains(item.info.shortname) || item.info.shortname == "hq.metal.ore") return;
            item.amount = (int)(Convert.ToDouble(item.amount) * GetPlayerRate(player.UserIDString, "gather", ""));
        }

        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (player == null || entity == null || entity.net == null || !cfg.components.enablecomponentsrate) return;
            if (entity is LootableCorpse) return;
            var container = entity as LootContainer;
            if (container == null) return;
            if (LootedBoxes.Contains(container.net.ID)) return;
            if (!cfg.components.rate.ContainsKey(container.ShortPrefabName)) return;
            if (container.inventory == null) return;
            if (container.inventory.itemList?.Count == 0) return;
            foreach (var item in container.inventory.itemList)
            {
                if (item == null) continue;
                if (cfg.components.blacklist.Contains(item.info.shortname) || item.info.stackable == 1 ||
                    item.info.shortname.Contains("electric") || item.info.shortname.Contains("electric")) continue;
                item.amount = (int)(Convert.ToDouble(item.amount) * GetPlayerRate(player.UserIDString, "components", container.ShortPrefabName));
            }
            if (!LootedBoxes.Contains(container.net.ID)) LootedBoxes.Add(container.net.ID);
        }

        void OnEntityDeath(BaseEntity entity, HitInfo info)
        {
            if (!entity.ShortPrefabName.Contains("barrel")) return;
            if (info == null || info.InitiatorPlayer == null) return;

            var lootContainer = entity.GetComponent<LootContainer>();
            if (lootContainer == null) return;
            if (lootContainer.inventory?.itemList == null) return;

            if (!cfg.barrels.enablebarrelsrate) return;
            if (lootContainer != null)
            {
                foreach (var barrel in cfg.barrels.rate)
                {
                    if (lootContainer.ShortPrefabName.Contains(barrel.Key))
                    {
                        foreach (var items in lootContainer.inventory.itemList)
                        {
                            if (cfg.barrels.blacklist.Contains(items.info.shortname) || items.info.stackable == 1) continue;
                            items.amount = (int)(Convert.ToDouble(items.amount) * GetPlayerRate(info.InitiatorPlayer.UserIDString, "barrel", entity.ShortPrefabName));
                        }
                    }
                }
            }
        }

        #endregion
    }
}