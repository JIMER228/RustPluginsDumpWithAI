#define DEBUG
using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using UnityEngine;


//ExtraLoot created with PluginMerge v(1.0.4.0) by MJSU @ https://github.com/dassjosh/Plugin.Merge
namespace Oxide.Plugins
{
    [Info("Extra Loot", "Shady14u", "1.0.6")]
    [Description("Add extra items (including custom) to any loot container in the game")]
    public partial class ExtraLoot : RustPlugin
    {
        #region 0.ExtraLoot.cs
        private void SpawnLoot(LootContainer container)
        {
            List<BaseItem> items;
            if (!_config.Containers.TryGetValue(container.ShortPrefabName, out items))
            {
                return;
            }
            
            ItemContainer component1 = container.GetComponent<StorageContainer>().inventory;
            foreach (var value in items)
            {
                if (!(value.Chance >= UnityEngine.Random.Range(0f, 100f))) continue;
                
                var amount = Core.Random.Range(value.AmountMin, value.AmountMax + 1);
                var shortName = value.IsBlueprint ? "blueprintbase" : value.ShortName;
                var item = ItemManager.CreateByName(shortName, amount, value.SkinId);
                
                if (component1 == null || item == null) continue;
                
                item.name = value.DisplayName;
                item.blueprintTarget = value.IsBlueprint ? ItemManager.FindItemDefinition(value.ShortName).itemid : 0;
                
                component1.itemList.Add(item);
                component1.capacity++;
                item.parent = component1;
                item.MarkDirty();
            }
        }
        #endregion

        #region 1.ExtraLoot.Config.cs
        private static Configuration _config;
        
        public class Configuration
        {
            [JsonProperty(PropertyName = "ShortName -> Items")]
            public Dictionary<string, List<BaseItem>> Containers;
            
            
            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    Containers = new Dictionary<string, List<BaseItem>>
                    {
                        ["crate_normal"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "pistol.revolver",
                                Chance = 50,
                                AmountMin = 1,
                                AmountMax = 1,
                                SkinId = 0,
                                DisplayName = "Recipe",
                                IsBlueprint = true
                            },
                            new BaseItem
                            {
                                ShortName = "box.repair.bench",
                                Chance = 30,
                                AmountMin = 1,
                                AmountMax = 2,
                                SkinId = 1594245394,
                                DisplayName = "Recycler",
                                IsBlueprint = false
                            },
                            new BaseItem
                            {
                                ShortName = "autoturret",
                                Chance = 30,
                                AmountMin = 1,
                                AmountMax = 2,
                                SkinId = 1587601905,
                                DisplayName = "Sentry Turret",
                                IsBlueprint = false
                            },
                            new BaseItem
                            {
                                ShortName = "paper",
                                Chance = 30,
                                AmountMin = 1,
                                AmountMax = 1,
                                SkinId = 1602864474,
                                DisplayName = "Recipe",
                                IsBlueprint = false
                            },
                            new BaseItem
                            {
                                ShortName = "paper",
                                Chance = 30,
                                AmountMin = 1,
                                AmountMax = 1,
                                SkinId = 1602955228,
                                DisplayName = "Recipe",
                                IsBlueprint = false
                            },
                        },
                        ["crate_elite"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "paper",
                                Chance = 15,
                                AmountMin = 1,
                                AmountMax = 1,
                                SkinId = 1602864474,
                                DisplayName = "Recipe"
                            },
                            new BaseItem
                            {
                                ShortName = "paper",
                                Chance = 15,
                                AmountMin = 1,
                                AmountMax = 1,
                                SkinId = 1602955228,
                                DisplayName = "Recipe"
                            },
                        },
                    }
                };
            }
        }
        
        
        #region BoilerPlate
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PrintWarning("Creating new config file.");
                LoadDefaultConfig();
            }
        }
        
        protected override void LoadDefaultConfig() => _config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(_config);
        
        #endregion
        #endregion

        #region 5.ExtraLoot.Hooks.cs
        private void OnLootSpawn(LootContainer container)
        {
            NextTick(() => { SpawnLoot(container); });
        }
        #endregion

        #region 7.ExtraLoot.Classes.cs
        public class BaseItem
        {
            [JsonProperty(PropertyName = "1. ShortName")]
            public string ShortName;
            
            [JsonProperty(PropertyName = "2. Chance")]
            public float Chance;
            
            [JsonProperty(PropertyName = "3. Minimal amount")]
            public int AmountMin;
            
            [JsonProperty(PropertyName = "4. Maximal Amount")]
            public int AmountMax;
            
            [JsonProperty(PropertyName = "5. Skin Id")]
            public ulong SkinId;
            
            [JsonProperty(PropertyName = "6. Display Name")]
            public string DisplayName;
            
            [JsonProperty(PropertyName = "7. Blueprint")]
            public bool IsBlueprint;
        }
        #endregion

    }

}
