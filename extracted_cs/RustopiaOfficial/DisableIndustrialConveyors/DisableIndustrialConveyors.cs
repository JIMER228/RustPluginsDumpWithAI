// Reference: 0Harmony
using Harmony;
using System;
using Network;

namespace Oxide.Plugins
{
    [Info("DisableIndustrialConveyors", "bmgjet", "1.0.0")]
    [Description("Blocks Run Job From Running On IndustrialConveyors If Save Nearly Full")]
    class DisableIndustrialConveyors : RustPlugin
    {
        private HarmonyInstance _harmony;
        private void Init()
        {
            //Create Harmony Patch
            _harmony = HarmonyInstance.Create(Name + "PATCH");
            Type t = AccessTools.Inner(typeof(DisableIndustrialConveyors), "IndustrialConveyore_RunJob");
            new PatchProcessor(_harmony, t, HarmonyMethod.Merge(t.GetHarmonyMethods())).Patch();
        }
        [ChatCommand("conveyorsoff")]
        private void cmdconoff(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) { return; }
            int consoff = 0;
            foreach (BaseNetworkable bn in BaseNetworkable.serverEntities.entityList.Values)
            {
                if (bn is IndustrialConveyor)
                {
                    IndustrialConveyor io = bn as IndustrialConveyor;
                    if (io != null)
                    {
                        if (io.HasFlag(global::BaseEntity.Flags.On)) { consoff++; }
                        io.SetSwitch(false);
                    }
                }
            }
            player.ChatMessage("Switched off " + consoff + " conveyers.");
        }
        private void Unload() { _harmony.UnpatchAll(Name + "PATCH"); }//Unload Harmony Patch

        [HarmonyPatch(typeof(IndustrialConveyor), "RunJob")]
        public static class IndustrialConveyore_RunJob { [HarmonyPrefix] private static bool a() { return (4080218899 >= Net.sv.lastValueGiven); } }
        //4080218899 = 95% of save file used up
    }
}
