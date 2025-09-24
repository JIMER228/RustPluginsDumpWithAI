/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

namespace Oxide.Plugins
{
    [Info("No Stuck Arrows", "VisEntities", "1.0.1")]
    [Description("Fixes arrows and spears remaining stuck on players even after they respawn.")]
    public class NoStuckArrows : RustPlugin
    {
        #region Fields

        private static NoStuckArrows _plugin;

        #endregion Fields

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
        }

        private void Unload()
        {
            _plugin = null;
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo hitInfo)
        {
            if (player == null || hitInfo == null)
                return;
 
            RemoveStuckProjectiles(player);
        }

        #endregion Oxide Hooks

        #region Projectiles Removal

        public void RemoveStuckProjectiles(BasePlayer player)
        {
            if (player == null)
                return;

            foreach (WorldItem worldItem in player.GetComponentsInChildren<WorldItem>())
            {
                if (worldItem == null)
                    continue;

                Item item = worldItem.item;
                if (item != null && item.info.category == ItemCategory.Ammunition)
                {
                    worldItem.DestroyItem();
                    worldItem.Kill();
                }
            }
        }

        #endregion Projectiles Removal
    }
}