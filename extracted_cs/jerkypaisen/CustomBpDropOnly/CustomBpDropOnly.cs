using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    // 'Extra Loot' modified it. https://umod.org/plugins/extra-loot
    [Info("CustomBpDropOnly", "jerkypaisen", "1.0.0")]
    [Description("BP is dropped. Research and tree are disabled.")]
    public class CustomBpDropOnly : RustPlugin
    {

        #region Oxide Hooks
        private object CanResearchItem(BasePlayer player, Item item)
        {
            return false;
        }

        private object CanUnlockTechTreeNode(BasePlayer player, TechTreeData.NodeInstance node, TechTreeData techTree)
        {
            return false;
        }

        private void OnItemAction(Item item, string action, BasePlayer player)
        {
            if (player != null && action == "study"  && item.IsBlueprint())
            {
                string itemName = item.blueprintTargetDef.displayName.translated;
                string msg = player.displayName + " さんが " + itemName + " をおぼえました";
                Server.Broadcast(msg, null);
            }
        }

        private void SpawnLoot(LootContainer container)
        {
            if (container == null) return;

            List<BaseItem> items;
            if (!_config.Containers.TryGetValue(container.ShortPrefabName, out items))
            {
                return;
            }

            var maxItems = _config.MaxItemsPerContainer;
            var cnt = 0;
            foreach (var value in items)
            {
                if (!(value.Chance >= UnityEngine.Random.Range(0f, 100f))) continue;

                var shortName =  "blueprintbase";
                var item = ItemManager.CreateByName(shortName, 1, 0);

                if (item == null) continue;

                item.name = "Recipe";
                item.blueprintTarget = ItemManager.FindItemDefinition(value.ShortName).itemid;

                item.MoveToContainer(container.inventory);
                container.inventory.MarkDirty();
                cnt++;
                if (maxItems > 0 && cnt >= maxItems) return;
            }
        }

        private void OnLootSpawn(LootContainer container)
        {
            NextTick(() => { SpawnLoot(container); });
        }
        #endregion


        #region Config
        private static Configuration _config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "ShortName -> Items")]
            public Dictionary<string, List<BaseItem>> Containers;

            [JsonProperty(PropertyName = "Max Extra Items Per Container")]
            public int MaxItemsPerContainer;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {

                    Containers = new Dictionary<string, List<BaseItem>>
                    {
                        ["loot_barrel_1"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "pistol.revolver",
                                Chance = 50
                            },
                        },
                        ["loot_barrel_2"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "pistol.revolver",
                                Chance = 50
                            },
                        },
                        ["oil_barrel"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "pistol.revolver",
                                Chance = 50
                            },
                        },
                        ["loot_trash"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "pistol.revolver",
                                Chance = 50
                            },
                        },
                        ["foodbox"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "pistol.revolver",
                                Chance = 50
                            },
                        },
                        ["crate_tools"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "pistol.revolver",
                                Chance = 50
                            },
                        },
                        ["heli_crate"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "pistol.revolver",
                                Chance = 50
                            },
                        },
                        ["supply_drop"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "pistol.revolver",
                                Chance = 50
                            },
                        },
                        ["crate_normal"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "pistol.revolver",
                                Chance = 50
                            },
                        },
                        ["crate_normal_2"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "pistol.revolver",
                                Chance = 50
                            },
                        },
                        ["crate_normal_2_medical"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "pistol.revolver",
                                Chance = 50
                            },
                        },
                        ["crate_elite"] = new List<BaseItem>
                        {
                            new BaseItem
                            {
                                ShortName = "pistol.revolver",
                                Chance = 50
                            },
                        },
                    }

                };
            }
        }

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


        #region Class
        public class BaseItem
        {
            [JsonProperty(PropertyName = "1. ShortName")]
            public string ShortName;

            [JsonProperty(PropertyName = "2. Chance")]
            public float Chance;
        }
        #endregion


    }
}