using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Development", "Jacob", "1.0.0")]
    internal class Development : RustPlugin 
    {
        #region Oxide Hooks

        private object OnRunPlayerMetabolism() => false;

        private void OnPlayerInit(BasePlayer player)
        { 
            SetMetabolism(player);
            SetupResources(player);
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            SetMetabolism(player);
            SetupResources(player);
        }

        private void OnServerInitialized()
        {
            TOD_Sky.Instance.Cycle.Hour = 12;
            TOD_Sky.Instance.Components.Time.ProgressTime = false;

            foreach (var item in ItemManager.itemList)
            {
                if (item.Blueprint?.time != null) item.Blueprint.time = 0;
                if (item.Blueprint?.ingredients == null) continue;
                foreach (var itemAmount in item.Blueprint.ingredients) itemAmount.amount = 0;
                if (item.category == ItemCategory.Weapon) continue;
                item.stackable = int.MaxValue;
            }
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity.GetType() == typeof(BaseHelicopter))
                entity.Kill();
        }

        private void OnAirdrop(CargoPlane plane, Vector3 dropPosition) => plane.Kill();

        private void OnItemCraftCancelled(ItemCraftTask task)
        {
            if (task?.takenItems == null || task.takenItems.Count <= 0) return;
            foreach (var current in task.takenItems.ToList())
                if (current != null && current.amount > 0 && current.info.category.ToString() == "Component")
                    current.Remove();
        }

        private void OnPlayerDisconnected(BasePlayer player) => Clear(player);

        private void OnPlayerWound(BasePlayer player) => Clear(player);

        private void OnPlayerDie(BasePlayer player) => Clear(player);

        #endregion

        #region Helpers

        private void SetMetabolism(BasePlayer player)
        {
            player.metabolism.Reset();
            player.health = 100;
            player.metabolism.calories.value = player.metabolism.calories.max;
            player.metabolism.hydration.value = player.metabolism.hydration.max;
        }

        private void SetupResources(BasePlayer player)
        {
            Clear(player);
            var count = 24;
            foreach (var itemDefinition in ItemManager.itemList.Where(x => x.category == ItemCategory.Component || x.category == ItemCategory.Resources))
            {
                var item = ItemManager.CreateByName(itemDefinition.shortname, 10000);
                player.inventory.containerMain.capacity++;
                item.MoveToContainer(player.inventory.containerMain, count);
                player.inventory.SendUpdatedInventory(PlayerInventory.Type.Main, player.inventory.containerMain);
                count++;
            }
        }

        private void Clear(BasePlayer player)
        {
            foreach (var item in player.inventory.AllItems())
            {
                item.RemoveFromWorld();
                item.RemoveFromContainer();
            }

            player.inventory.containerMain.capacity = 23;
        }

        #endregion
    }
}