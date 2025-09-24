using Oxide.Core.Plugins;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("DeathCleaner", "YourName", "1.4.0")]
    [Description("Удаляет стартовые предметы при смерти")]
    public class DeathCleaner : RustPlugin
    {
        private readonly HashSet<string> _itemsToRemove = new HashSet<string>
        {
            "burlap.headwrap",
            "burlap.shirt",
            "burlap.gloves.new",
            "burlap.trousers",
            "burlap.shoes",
            "concretehatchet",
            "concretepickaxe"
        };

        private const float CleanRadius = 3f;

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            var player = entity as BasePlayer;
            if (player == null) return;

            CleanPlayerInventory(player.inventory);
            CleanNearbyItems(player.transform.position);
        }

        private void CleanPlayerInventory(PlayerInventory inventory)
        {
            CleanContainer(inventory.containerWear);
            CleanContainer(inventory.containerBelt);
            CleanContainer(inventory.containerMain);
        }

        private void CleanContainer(ItemContainer container)
        {
            var items = container.itemList.ToArray();
            foreach (var item in items)
            {
                if (_itemsToRemove.Contains(item.info.shortname))
                {
                    item.RemoveFromContainer();
                    item.Remove();
                }
            }
        }

        private void CleanNearbyItems(Vector3 position)
        {
            var entities = new List<BaseEntity>();
            Vis.Entities(position, CleanRadius, entities);

            foreach (var entity in entities)
            {
                if (entity is DroppedItem droppedItem && _itemsToRemove.Contains(droppedItem.item.info.shortname))
                {
                    droppedItem.Kill();
                }
            }
        }
    }
}