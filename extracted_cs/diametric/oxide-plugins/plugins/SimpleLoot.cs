using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Simple Loot", "Jacob", "1.0.5")]
    class SimpleLoot : RustPlugin
    {
        #region Configuration 

        private class Configuration
        {
            // public int Multiplier = 1;

            public Dictionary<string, object> Multipliers = new Dictionary<string, object>
            {
                {"scrap", 1}
            };

            public bool ReplaceItems;

            public Configuration()
            {
                // GetConfig(ref Multiplier, "Settings", "Scrap multiplier");

                GetConfig(ref Multipliers, "Settings", "Multipliers");

                GetConfig(ref ReplaceItems, "Settings", "Replace items with blueprints");

                foreach (var itemDefinition in ItemManager.itemList.Where(x => x?.category == ItemCategory.Component))
                {
                    if (itemDefinition.shortname == "bleach" || itemDefinition.shortname == "ducttape" ||
                        itemDefinition.shortname == "glue" || itemDefinition.shortname == "sticks")
                        continue;

                    if (Multipliers.ContainsKey(itemDefinition.shortname))
                        continue;

                    Multipliers.Add(itemDefinition.shortname, 1);
                }

                instance.SaveConfig();
            }

            private void GetConfig<T>(ref T variable, params string[] path)
            {
                if (path.Length == 0) return;

                if (instance.Config.Get(path) == null)
                {
                    SetConfig(ref variable, path);
                    instance.PrintWarning($"Added field to config: {string.Join("/", path)}");
                }

                variable = (T)Convert.ChangeType(instance.Config.Get(path), typeof(T));
            }

            private void SetConfig<T>(ref T variable, params string[] path) => instance.Config.Set(path.Concat(new object[] { variable }).ToArray());
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");

        #endregion

        #region Fields

        private static SimpleLoot instance;
        private Configuration configuration;

        #endregion

        #region Oxide Hooks

        private void Init() => instance = this;

        private void OnServerInitialized()
        {
            configuration = new Configuration();
            var containers = UnityEngine.Object.FindObjectsOfType<LootContainer>();
            for (var i = 0; i < containers.Length; i++)
            {
                OnEntitySpawned(containers[i]);
                if (i == containers.Length - 1)
                    PrintWarning($"Repopulating {i} loot containers.");
            }
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            var lootContainer = entity as LootContainer;
            if (lootContainer?.inventory?.itemList == null)
                return;

            foreach (var item in lootContainer.inventory.itemList.ToList())
            {
                item.RemoveFromWorld();
                item.RemoveFromContainer();
            }

            lootContainer.PopulateLoot();
            foreach (var item in lootContainer.inventory.itemList.ToList())
            {
                var itemBlueprint = ItemManager.FindItemDefinition(item.info.shortname).Blueprint;
                if (configuration.ReplaceItems && itemBlueprint != null && itemBlueprint.isResearchable)
                {
                    var slot = item.position;
                    item.RemoveFromWorld();
                    item.RemoveFromContainer();
                    var blueprint = ItemManager.CreateByName("blueprintbase");
                    blueprint.blueprintTarget = item.info.itemid;
                    blueprint.MoveToContainer(lootContainer.inventory, slot);
                }
                else
                {
                    object multiplier;
                    if (configuration.Multipliers.TryGetValue(item.info.shortname, out multiplier))
                        item.amount *= Convert.ToInt32(multiplier);
                }
            }
        }

        #endregion
    }
}