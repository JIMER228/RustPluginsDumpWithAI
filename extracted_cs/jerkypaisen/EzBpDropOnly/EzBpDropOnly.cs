using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core;
using System.ComponentModel;

namespace Oxide.Plugins
{
    [Info("EzBpDropOnly", "jerkypaisen", "1.0.1")]
    [Description("BP is dropped. Research and tree are disabled.")]
    public class EzBpDropOnly : RustPlugin
    {
        #region Oxide Hooks
        private object CanResearchItem(BasePlayer player, Item item)
        {
            if (item == null || player == null) return null;
            SendReply(player, lang.GetMessage("ResearchItem", this, player.UserIDString));
            return false;
        }

        private object CanUnlockTechTreeNode(BasePlayer player, TechTreeData.NodeInstance node, TechTreeData techTree)
        {
            if (techTree == null || node == null || player == null) return null;
            SendReply(player, lang.GetMessage("UnlockTechTree", this, player.UserIDString));
            return false;
        }

        private void OnItemAction(Item item, string action, BasePlayer player)
        {
            if (player != null && action == "study" && item.IsBlueprint())
            {
                string itemName = item.blueprintTargetDef.displayName.translated;
                string msg = string.Format(lang.GetMessage("YouGetBlueprint", this, player.UserIDString), player.displayName, itemName);
                Server.Broadcast(msg, null);
            }
        }

        private BaseCorpse OnCorpsePopulate(HumanNPC npc, NPCPlayerCorpse corpse)
        {
            if (npc == null || corpse == null) return null;
            if (npc.LootSpawnSlots.Length != 0)
            {
                LootContainer.LootSpawnSlot[] lootSpawnSlots = npc.LootSpawnSlots;
                for (int j = 0; j < lootSpawnSlots.Length; j++)
                {
                    LootContainer.LootSpawnSlot lootSpawnSlot = lootSpawnSlots[j];
                    for (int k = 0; k < lootSpawnSlot.numberToSpawn; k++)
                    {
                        if ((string.IsNullOrEmpty(lootSpawnSlot.onlyWithLoadoutNamed) || lootSpawnSlot.onlyWithLoadoutNamed == npc.GetLoadoutName()) && UnityEngine.Random.Range(0f, 1f) <= lootSpawnSlot.probability)
                        {
                            lootSpawnSlot.definition.SpawnIntoContainer(corpse.containers[0]);
                        }
                    }
                }
                SpawnRandomBP(corpse.containers[0], _config.NpcDropRate);
            }
            return corpse;
        }

        private void SpawnLoot(LootContainer container)
        {
            if (container == null) return;
            SpawnRandomBP(container.inventory, _config.CrateDropRate);
        }

        private void SpawnRandomBP(ItemContainer container, float dropRate)
        {
            var idx = UnityEngine.Random.Range(0, container.itemList.Count);
            var item = container.itemList[idx];
            if (item == null) return;
            if (item.info.Blueprint == null) return;

            // 除外リスト判定
            if (_config.ExcludeItems != null && _config.ExcludeItems.Contains(item.info.shortname))
                return;

            var rate = UnityEngine.Random.Range(0f, 100f);
            if (item.info.Blueprint.isResearchable && dropRate >= rate)
            {
                var shortName = "blueprintbase";
                var bpitem = ItemManager.CreateByName(shortName, 1, 0);
                bpitem.name = "Recipe";
                bpitem.blueprintTarget = item.info.itemid;
                container.itemList.RemoveAt(idx);

                bpitem.MoveToContainer(container);
                container.MarkDirty();
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
            [JsonProperty(PropertyName = "Crate drop rate of the blueprint as a float.")]
            public float CrateDropRate;

            [JsonProperty(PropertyName = "Npc drop rate of the blueprint as a float.")]
            public float NpcDropRate;

            [JsonProperty(PropertyName = "List of item shortnames to exclude from blueprint drops.")]
            public List<string> ExcludeItems = new List<string>();

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    CrateDropRate = 5.0f,
                    NpcDropRate = 0.1f,
                    ExcludeItems = new List<string>() // 例: { "rifle.ak", "explosives" }
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
        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }
        #endregion
        #region Localization
        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ResearchItem"] = "You can not research!",
                ["UnlockTechTree"] = "You can not unlock!",
                ["YouGetBlueprint"] = "{0} learned the {1}!",
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ResearchItem"] = "このアイテムのリサーチはできません。",
                ["UnlockTechTree"] = "テックツリーをアンロックすることはできません。",
                ["YouGetBlueprint"] = "{0} さんが {1} をおぼえました。",
            }, this, "ja");
        }
        #endregion
    }
}