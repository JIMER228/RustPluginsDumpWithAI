/* 
           ________               __
          / ____/ /_  ____  _____/ /___  __
         / / __/ __ \/ __ \/ ___/ __/ / / /
        / /_/ / / / / /_/ (__  ) /_/ /_/ /
        \____/_/ /_/\____/____/\__/\__, /
                                  /____/

This plugin is exclusively licensed to Enchanted.gg and may not be edited or sold without explicit permission.

© 2025 Ghosty & Enchanted.gg
*/

using HarmonyLib;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Furnace Plug", "Ghosty", "1.0.1")]
    [Description("Modifies fuel, charcoal and smelting rates")]

    public class FurnacePlug : RustPlugin
    {
        #region Config

        private static FurnaceConfig _cfg;

        private class FurnaceConfig
        {
            [JsonProperty("FuelRateMultiplier")]      public int   FuelRate      = 1;
            [JsonProperty("CharcoalRateMultiplier")]  public int   CharcoalRate  = 2;
            [JsonProperty("SmeltingSpeedMultiplier")] public float SmeltSpeed   = 2f;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try       { _cfg = Config.ReadObject<FurnaceConfig>(); }
            catch     { PrintWarning("Bad config, loading defaults"); LoadDefaultConfig(); }
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => _cfg = new FurnaceConfig();

        protected override void SaveConfig() => Config.WriteObject(_cfg);

        #endregion

        #region Harmony

        private const string HarmonyId = "com.enchanted.furnaceplug";
        private static Harmony _harmony;

        private void Init()
        {
            _harmony = new Harmony(HarmonyId);
            _harmony.PatchAll();
        }

        private void Unload() => _harmony?.UnpatchAll(HarmonyId);

        [HarmonyPatch(typeof(BaseOven), nameof(BaseOven.GetFuelRate))]
        private static class Patch_Fuel
        {
            private static void Postfix(ref int __result) => __result *= _cfg.FuelRate;
        }

        [HarmonyPatch(typeof(BaseOven), nameof(BaseOven.GetCharcoalRate))]
        private static class Patch_Charcoal
        {
            private static void Postfix(ref int __result) => __result *= _cfg.CharcoalRate;
        }

        [HarmonyPatch(typeof(BaseOven), nameof(BaseOven.GetSmeltingSpeed))]
        private static class Patch_Speed
        {
            private static void Postfix(ref float __result) => __result *= _cfg.SmeltSpeed;
        }
        #endregion
    }
}
