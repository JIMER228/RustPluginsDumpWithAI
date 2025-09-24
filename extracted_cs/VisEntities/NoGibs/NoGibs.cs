// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using HarmonyLib;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Oxide.Plugins
{
    [Info("No Gibs", "VisEntities", "1.2.0")]
    [Description("Prevents debris from spawning when entities decay, are killed by admins, demolished, or collapsed due to instability.")]
    public class NoGibs : RustPlugin
    {
        #region Fields

        private static NoGibs _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Disable Debris On Decay")]
            public bool DisableDebrisOnDecay { get; set; }

            [JsonProperty("Disable Debris On Admin Kill")]
            public bool DisableDebrisOnAdminKill { get; set; }

            [JsonProperty("Disable Debris On Demolish")]
            public bool DisableDebrisOnDemolish { get; set; }

            [JsonProperty("Disable Debris On Stability Collapse")]
            public bool DisableDebrisOnStabilityCollapse { get; set; }

            [JsonProperty("Disable Debris On Other")]
            public bool DisableDebrisOnOther { get; set; }

            [JsonProperty("Excluded Prefabs")]
            public List<string> ExcludedPrefabs { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            if (string.Compare(_config.Version, "1.1.0") < 0)
            {
                _config.DisableDebrisOnOther = defaultConfig.DisableDebrisOnOther;
            }

            if (string.Compare(_config.Version, "1.2.0") < 0)
            {
                _config = defaultConfig;
            }

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                DisableDebrisOnDecay = true,
                DisableDebrisOnAdminKill = true,
                DisableDebrisOnDemolish = true,
                DisableDebrisOnStabilityCollapse = true,
                DisableDebrisOnOther = true,
                ExcludedPrefabs = new List<string>()
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private object OnAdminKill(BaseNetworkable baseNetworkable)
        {
            if (baseNetworkable == null)
                return null;

            if (ShouldKeepDebris(baseNetworkable as BaseEntity))
                return null;

            if (!_config.DisableDebrisOnAdminKill)
                return null;
   
            baseNetworkable.Kill(BaseNetworkable.DestroyMode.None);
            return true;
        }

        private object OnStructureDemolish(StabilityEntity stabilityEntity, BasePlayer player)
        {
            if (stabilityEntity == null || player == null)
                return null;

           if (ShouldKeepDebris(stabilityEntity))
                return null;

            if (!_config.DisableDebrisOnDemolish)
                return null;
    
            stabilityEntity.Kill(BaseNetworkable.DestroyMode.None);
            return true;
        }

        private object OnDecayEntityDied(DecayEntity decayEntity)
        {
            if (decayEntity == null)
                return null;

            if (ShouldKeepDebris(decayEntity))
                return null;

            if (!_config.DisableDebrisOnDecay)
                return null;
 
            decayEntity.Kill(BaseNetworkable.DestroyMode.None);
            return true;
        }

        private object OnBaseCombatEntityDied(BaseCombatEntity baseCombatEntity, HitInfo info)
        {
            if (baseCombatEntity == null)
                return null;

            if (ShouldKeepDebris(baseCombatEntity))
                return null;

            if (!_config.DisableDebrisOnOther)
                return null;

            baseCombatEntity.Kill(BaseNetworkable.DestroyMode.None);
            return true;
        }

        #endregion Oxide Hooks

        #region Helper Functions

        private static bool ShouldKeepDebris(BaseEntity entity)
        {
            return entity != null
                   && _config.ExcludedPrefabs != null
                   && _config.ExcludedPrefabs.Contains(entity.ShortPrefabName);
        }

        #endregion Helper Functions

        #region Harmony Patches

        [AutoPatch]
        [HarmonyPatch(typeof(BaseCombatEntity), "OnDied")]
        public static class BaseCombatEntity_OnDied_Patch
        {
            public static bool Prefix(BaseCombatEntity __instance, HitInfo info)
            {
                if (Interface.CallHook("OnBaseCombatEntityDied", __instance, info) != null)
                {
                    return false;
                }

                return true;
            }
        }

        // This's necessary because 'DecayEntity' has extra debris logic not covered by the 'BaseCombatEntity.OnDied' method.
        [AutoPatch]
        [HarmonyPatch(typeof(DecayEntity), "OnDied")]
        public static class DecayEntity_OnDied_Patch
        {
            public static bool Prefix(DecayEntity __instance)
            {
                if (Interface.CallHook("OnDecayEntityDied", __instance) != null)
                {
                    return false;
                }

                return true;
            }
        }

        [AutoPatch]
        [HarmonyPatch(typeof(StabilityEntity), "StabilityCheck")]
        public static class StabilityEntity_StabilityCheck_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codeInstructions = new List<CodeInstruction>(instructions);
                var methodKill = AccessTools.Method(typeof(BaseNetworkable), "Kill");

                for (int instructionIndex = 0; instructionIndex < codeInstructions.Count; instructionIndex++)
                {
                    if (codeInstructions[instructionIndex].opcode == OpCodes.Call && codeInstructions[instructionIndex].operand.Equals(methodKill))
                    {
                        if (_config != null && _config.DisableDebrisOnStabilityCollapse)
                            codeInstructions[instructionIndex - 1] = new CodeInstruction(OpCodes.Ldc_I4_0);
                    }
                }

                return codeInstructions;
            }
        }

        [AutoPatch]
        [HarmonyPatch(typeof(BaseNetworkable), "AdminKill")]
        public static class BaseNetworkable_AdminKill_Patch
        {
            public static bool Prefix(BaseNetworkable __instance)
            {
                if (Interface.CallHook("OnAdminKill", __instance) != null)
                {
                    return false;
                }

                return true;
            }
        }

        #endregion Harmony Patches
    }
}