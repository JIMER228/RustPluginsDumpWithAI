using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Plugins.CustomMagazineExtensionMethods;

namespace Oxide.Plugins
{
    [Info("CustomMagazine", "KpucTaJl", "1.0.3")]
    internal class CustomMagazine : RustPlugin
    {
        #region Config
        private const bool En = false;

        private PluginConfig _config;

        protected override void LoadDefaultConfig()
        {
            Puts("Creating a default config...");
            _config = PluginConfig.DefaultConfig();
            _config.PluginVersion = Version;
            SaveConfig();
            Puts("Creation of the default config completed!");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
            if (_config.PluginVersion < Version) UpdateConfigValues();
        }

        private void UpdateConfigValues()
        {
            Puts("Config update detected! Updating config values...");
            _config.PluginVersion = Version;
            Puts("Config update completed!");
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        public class CrateConfig
        {
            [JsonProperty("Prefab")] public string Prefab { get; set; }
            [JsonProperty(En ? "Chance probability [0.0-100.0]" : "Шанс выпадения предмета [0.0-100.0]")] public float Chance { get; set; }
        }

        public class MagazineConfig
        {
            [JsonProperty(En ? "Name" : "Название")] public string Name { get; set; }
            [JsonProperty("SkinID")] public ulong SkinID { get; set; }
            [JsonProperty(En ? "Ammo Multiplier" : "Множитель патронов")] public float Scale { get; set; }
            [JsonProperty(En ? "Settings spawn in crates" : "Настройка появления в ящиках")] public List<CrateConfig> Crates { get; set; }
        }

        private class PluginConfig
        {
            [JsonProperty(En ? "List of custom magazines" : "Список магазинов")] public List<MagazineConfig> Magazines { get; set; }
            [JsonProperty(En ? "Configuration version" : "Версия конфигурации")] public VersionNumber PluginVersion { get; set; }

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    Magazines = new List<MagazineConfig>
                    {
                        new MagazineConfig
                        {
                            Name = "Extended Magazine +50%",
                            SkinID = 2817854052,
                            Scale = 1.5f,
                            Crates = new List<CrateConfig>
                            {
                                new CrateConfig { Prefab = "assets/bundled/prefabs/radtown/crate_normal.prefab", Chance = 25f }
                            }
                        },
                        new MagazineConfig
                        {
                            Name = "Extended Magazine +75%",
                            SkinID = 2817854377,
                            Scale = 1.75f,
                            Crates = new List<CrateConfig>
                            {
                                new CrateConfig { Prefab = "assets/bundled/prefabs/radtown/crate_elite.prefab", Chance = 25f }
                            }
                        },
                        new MagazineConfig
                        {
                            Name = "Extended Magazine +100%",
                            SkinID = 2817854677,
                            Scale = 2f,
                            Crates = new List<CrateConfig>
                            {
                                new CrateConfig { Prefab = "assets/prefabs/misc/supply drop/supply_drop.prefab", Chance = 25f },
                                new CrateConfig { Prefab = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate_oilrig.prefab", Chance = 25f },
                                new CrateConfig { Prefab = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab", Chance = 25f }
                            }
                        }
                    },
                    PluginVersion = new VersionNumber()
                };
            }
        }
        #endregion Config

        #region Oxide Hooks
        private void OnServerInitialized() { if (_config.Magazines.Sum(x => x.Crates.Count) == 0) Unsubscribe("OnLootSpawn"); }

        private void OnLootSpawn(LootContainer container)
        {
            if (container == null || container.inventory == null) return;
            MagazineConfig magazine = _config.Magazines.FirstOrDefault(x => x.Crates.Any(y => y.Prefab == container.PrefabName));
            if (magazine == null) return;
            CrateConfig crate = magazine.Crates.FirstOrDefault(x => x.Prefab == container.PrefabName);
            if (crate == null) return;
            if (UnityEngine.Random.Range(0f, 100f) <= crate.Chance)
            {
                if (container.inventory.itemList.Count == container.inventory.capacity) container.inventory.capacity++;
                Item item = GetMagazine(magazine);
                if (!item.MoveToContainer(container.inventory)) item.Remove();
            }
        }

        private object OnWeaponReload(BaseProjectile weapon, BasePlayer player)
        {
            foreach (BaseEntity entity in weapon.children)
            {
                if (entity.ShortPrefabName != "extendedmags.entity") continue;
                ProjectileWeaponMod magazine = entity as ProjectileWeaponMod;
                if (magazine.skinID == 0) return null;
                MagazineConfig config = _config.Magazines.FirstOrDefault(x => x.SkinID == magazine.skinID);
                if (config == null) return null;
                magazine.magazineCapacity.scalar = config.Scale;
                weapon.primaryMagazine.capacity = (int)(weapon.primaryMagazine.definition.builtInSize * config.Scale);
            }
            return null;
        }

        private object CanStackItem(Item item, Item targetItem)
        {
            if (item == null || targetItem == null) return null;
            if (item.info.itemid == targetItem.info.itemid && item.skin != targetItem.skin) return false;
            return null;
        }

        private object CanCombineDroppedItem(DroppedItem drItem, DroppedItem anotherDrItem)
        {
            if (drItem == null || anotherDrItem == null) return null;
            if (drItem.item.info.itemid == anotherDrItem.item.info.itemid && drItem.item.skin != anotherDrItem.item.skin) return false;
            return null;
        }

        private Item OnItemSplit(Item item, int amount)
        {
            if (_config.Magazines.Any(x => x.SkinID == item.skin))
            {
                item.amount -= amount;
                Item newItem = ItemManager.CreateByItemID(item.info.itemid, amount, item.skin);
                newItem.name = item.name;
                item.MarkDirty();
                return newItem;
            }
            return null;
        }
        #endregion Oxide Hooks

        #region Helpers
        private Item GetMagazine(MagazineConfig config)
        {
            Item item = ItemManager.CreateByName("weapon.mod.extendedmags", 1, config.SkinID);
            if (!string.IsNullOrEmpty(config.Name)) item.name = config.Name;
            ProjectileWeaponMod magazine = item.GetHeldEntity() as ProjectileWeaponMod;
            magazine.magazineCapacity.scalar = config.Scale;
            return item;
        }
        #endregion Helpers

        #region Commands
        [ConsoleCommand("givemagazine")]
        private void ConsoleCommandGiveMagazine(ConsoleSystem.Arg arg)
        {
            if (arg.Args == null || arg.Args.Length < 2 || arg.Player() != null) return;
            ulong skinid = Convert.ToUInt64(arg.Args[0]);
            MagazineConfig config = _config.Magazines.FirstOrDefault(x => x.SkinID == skinid);
            if (config == null)
            {
                Puts($"Custom Magazine with SkinID {skinid} not found in plugin configuration!");
                return;
            }
            ulong steamid = Convert.ToUInt64(arg.Args[1]);
            BasePlayer target = BasePlayer.FindByID(steamid);
            if (target == null)
            {
                Puts($"Player with SteamID {steamid} not found!");
                return;
            }
            Item item = GetMagazine(config);
            int slots = target.inventory.containerMain.capacity + target.inventory.containerBelt.capacity;
            int taken = target.inventory.containerMain.itemList.Count + target.inventory.containerBelt.itemList.Count;
            if (slots - taken > 0) target.inventory.GiveItem(item);
            else item.Drop(target.transform.position, Vector3.up);   
            Puts($"Player {target.displayName} has successfully received a custom magazine with SkinID = {config.SkinID}");
        }
        #endregion Commands
    }
}

namespace Oxide.Plugins.CustomMagazineExtensionMethods
{
    public static class ExtensionMethods
    {
        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return true;
            return false;
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return enumerator.Current;
            return default(TSource);
        }

        public static int Sum<TSource>(this IList<TSource> source, Func<TSource, int> predicate)
        {
            int result = 0;
            for (int i = 0; i < source.Count; i++) result += predicate(source[i]);
            return result;
        }
    }
}