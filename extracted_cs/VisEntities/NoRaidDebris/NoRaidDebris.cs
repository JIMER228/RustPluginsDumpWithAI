/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

namespace Oxide.Plugins
{
    [Info("No Raid Debris", "VisEntities", "1.0.0")]
    [Description("Disables debris that block building during raids")]
    public class NoRaidDebris : RustPlugin
    {
        #region Fields

        private static NoRaidDebris _plugin;

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

        private void OnEntitySpawned(DebrisEntity debrisEntity)
        {
            if (debrisEntity != null)
                debrisEntity.RemoveCorpse();
        }

        #endregion Oxide Hooks
    }
}