// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using HarmonyLib;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("No Distance Based Temperature", "VisEntities", "1.0.0")]
    [Description("Removes temperature falloff, making zones equally hot everywhere.")]
    public class NoDistanceBasedTemperature : RustPlugin
    {
        #region Fields

        private static NoDistanceBasedTemperature _plugin;

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
        [HarmonyPatch(typeof(TriggerTemperature), "WorkoutTemperature")]
        public static class TriggerTemperature_WorkoutTemperature_Patch
        {
            public static bool Prefix(TriggerTemperature __instance, ref float __result)
            {
                __result = __instance.Temperature;
                return false;
            }
        }

        #endregion Harmony Patches
    }
}