using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("NpcDropTagInBag", "jerky", "1.0.0")]
    [Description("NpcDropTag")]
    class NpcDropTagInBag : RustPlugin
    {
        #region [Oxide Hooks]
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
                SpawnTag(corpse.containers[0]);
            }
            return corpse;
        }
        #endregion

        #region [method]
        private void SpawnTag(ItemContainer container)
        {
            var idx = UnityEngine.Random.Range(0, container.itemList.Count);
            var item = container.itemList[idx];
            if (item == null) return;

            var shortName = "dogtagneutral";
            var tagItem = ItemManager.CreateByName(shortName, 1, 0);

            container.itemList[idx] = tagItem;
            container.MarkDirty();
        }
        #endregion
    }
}
