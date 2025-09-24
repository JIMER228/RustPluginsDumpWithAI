/*
 ▄▄▄▄    ███▄ ▄███▓  ▄████  ▄▄▄██▀▀▀▓█████▄▄▄█████▓
▓█████▄ ▓██▒▀█▀ ██▒ ██▒ ▀█▒   ▒██   ▓█   ▀▓  ██▒ ▓▒
▒██▒ ▄██▓██    ▓██░▒██░▄▄▄░   ░██   ▒███  ▒ ▓██░ ▒░
▒██░█▀  ▒██    ▒██ ░▓█  ██▓▓██▄██▓  ▒▓█  ▄░ ▓██▓ ░ 
░▓█  ▀█▓▒██▒   ░██▒░▒▓███▀▒ ▓███▒   ░▒████▒ ▒██▒ ░ 
░▒▓███▀▒░ ▒░   ░  ░ ░▒   ▒  ▒▓▒▒░   ░░ ▒░ ░ ▒ ░░   
▒░▒   ░ ░  ░      ░  ░   ░  ▒ ░▒░    ░ ░  ░   ░    
 ░    ░ ░      ░   ░ ░   ░  ░ ░ ░      ░    ░      
 ░             ░         ░  ░   ░      ░  ░
*/
using HarmonyLib;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection.Emit;

namespace Oxide.Plugins;

[Info("RustDebugger", "bmgjet", "1.0.0")]
public class RustDebugger : CovalencePlugin
{
    private Harmony harmony;
    private const string DOMAIN = "com.bmgjet.rust.rustdebugger";

    #region Oxide Hooks
    private void Init()
    {
        harmony = new Harmony(DOMAIN);

        try
        {
            harmony.CreateClassProcessor(typeof(DeveloperList_Contains)).Patch();
            harmony.CreateClassProcessor(typeof(NOP_The_Kick)).Patch();
            harmony.CreateClassProcessor(typeof(NOP_The_EAC_Kick)).Patch();
            Puts("Successfully patched!");
        }
        catch (Exception ex)
        {
            PrintError($"Failed to apply Harmony patches: {ex.Message}");
        }
    }

    private void Unload()
    {
        harmony.UnpatchAll(DOMAIN);
        Puts("Successfully unpatched!");
    }
    #endregion

    #region Harmony Patches
    // Patch for the DeveloperList.Contains method
    [HarmonyPatch(typeof(DeveloperList), "Contains", typeof(string))]
    internal class DeveloperList_Contains
    {
        [HarmonyPrefix]
        static bool Prefix(ref bool __result)
        {
            // Force the method to always return true
            __result = true;
            return false; // Skip the original method
        }
    }

    // Patch for ServerMgr.OnNetworkMessage to prevent specific kicks
    [HarmonyPatch(typeof(ServerMgr), nameof(ServerMgr.OnNetworkMessage))]
    public static class NOP_The_Kick
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionList = instructions.ToList();

            // Define the packet strings and their respective NOP ranges
            var packetRules = new Dictionary<string, (int before, int after)>
            {
                // NOP 3 before and 2 after
                { "Invalid Packet: Player Tick", (3, 2) },
                { "Invalid Packet: Client Ready", (3, 2) },
                { "Invalid Packet: World", (3, 2) },
                { "Invalid Packet: EAC", (3, 2) },
                { "Invalid Packet: User Information", (3, 2) },
                { "Invalid Packet: Player Voice", (3, 2) },
                { "Invalid Packet: RPC Message", (3, 2) },
                { "Invalid Packet: Client Command", (3, 2) },
                { "Invalid Packet: Disconnect Reason", (3, 2) },
                { "Packet Flooding: RPC Message", (3, 2) },

                // NOP 3 before and 4 after
                { "Packet Flooding: Client Commandn", (3, 4) },
                { "Packet Flooding: Client Ready", (3, 4) },
                { "Packet Flooding: World", (3, 4) },
                { "Packet Flooding: Disconnect Reason", (3, 4) },
                { "Packet Flooding: Player Tick", (3, 4) },
                { "Packet Flooding: User Information", (3, 4) }
            };

            for (int i = 0; i < instructionList.Count; i++)
            {
                if (instructionList[i].opcode == OpCodes.Ldstr &&
                    packetRules.TryGetValue(instructionList[i].operand as string, out var range))
                {
                    // NOP the surrounding instructions based on the range
                    NopSurroundingInstructions(instructionList, i, range.before, range.after);
                }
            }

            return instructionList;
        }

        private static void NopSurroundingInstructions(List<CodeInstruction> instructions, int index, int before, int after)
        {
            for (int offset = -before; offset <= after; offset++)
            {
                int targetIndex = index + offset;
                if (targetIndex >= 0 && targetIndex < instructions.Count)
                {
                    instructions[targetIndex].opcode = OpCodes.Nop;
                }
            }
        }
    }

    // Patch for ServerMgr.DoTick to prevent EAC kicks
    [HarmonyPatch(typeof(ServerMgr), "DoTick")]
    public static class NOP_The_EAC_Kick
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var instructionList = instructions.ToList();

            for (int i = 0; i < instructionList.Count; i++)
            {
                if (instructionList[i].opcode == OpCodes.Ldstr &&
                    instructionList[i].operand as string == "Authentication Timed Out")
                {
                    // NOP 2 before and 2 after
                    NopSurroundingInstructions(instructionList, i, 2, 2);
                    break;
                }
            }

            return instructionList;
        }

        private static void NopSurroundingInstructions(List<CodeInstruction> instructions, int index, int before, int after)
        {
            for (int offset = -before; offset <= after; offset++)
            {
                int targetIndex = index + offset;
                if (targetIndex >= 0 && targetIndex < instructions.Count)
                {
                    instructions[targetIndex].opcode = OpCodes.Nop;
                }
            }
        }
    }
    #endregion
}