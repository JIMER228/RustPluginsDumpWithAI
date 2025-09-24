/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using HarmonyLib;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("No Respawn Cooldown", "VisEntities", "1.0.1")]
    [Description("Lets players use the respawn command without any cooldown.")]
    public class NoRespawnCooldown : RustPlugin
    {
        #region Fields

        private static NoRespawnCooldown _plugin;

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

        #endregion Oxide Hooks

        #region Harmony Patches

        [AutoPatch]
        [HarmonyPatch(typeof(BasePlayer), "MarkRespawn")]
        public static class BasePlayer_MarkRespawn_Patch
        {
            public static bool Prefix(BasePlayer __instance, ref float nextSpawnDelay)
            {
                nextSpawnDelay = 0f;
                return true;
            }
        }

        #endregion Harmony Patches
    }
}