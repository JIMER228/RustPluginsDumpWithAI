//plugin.merge -c -m -p ./merge.json

using System.Collections.Generic;

namespace Oxide.Plugins
{
    //Define:FileOrder=1
    [Info("Extra Loot", "Shady14u", "1.0.6")]
    [Description("Add extra items (including custom) to any loot container in the game")]
    public partial class ExtraLoot : RustPlugin
    {
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
    }
}
