#region License (GPL v2)
/*
    DESCRIPTION
    Copyright (c) 2023 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License v2.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License Information (GPL v2)
using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using System.Reflection.Emit;
using HarmonyLib;
//Reference: 0Harmony

namespace Oxide.Plugins
{
    [Info("NREHook", "RFC1920", "1.0.4")]
    [Description("Insert hook to be called on NRE")]
    internal class NREHook : RustPlugin
    {
        //private bool logAllHooks = true;

        [AutoPatch]
        [HarmonyPatch(typeof(Plugin), "CallHook")]
        public static class CallHookPatch
        {
            //[HarmonyPrefix]
            //private static void Prefix(Plugin __instance, ref string hook, ref object[] args)
            //{
            //    // Debug use only
            //    if (hook == "OnFoundNRE")
            //    {
            //        string argString = "";
            //        foreach(var arg in args)
            //        {
            //            argString += arg.GetType().Name + "(" + arg.ToString() + "), ";
            //        }
            //        UnityEngine.Debug.LogWarning($"CallHook {__instance.Name}:{hook} {args.Length} args: {argString.TrimEnd(',', ' ')}");
            //    }
            //}

            [HarmonyTranspiler]
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr, ILGenerator il)
            {
                List<CodeInstruction> codes = new List<CodeInstruction>(instr);
                Label newLabel = il.DefineLabel();
                int startIndex = -1;
                int i;

                for (i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Stloc_2 && codes[i + 1].opcode == OpCodes.Call && codes[i + 2].opcode == OpCodes.Ldstr && startIndex == -1)
                    {
                        startIndex = i + 14;
                        codes[startIndex].labels.Add(newLabel);
                        break;
                    }
                }

                if (startIndex > -1)
                {
                    // CallHook(string hook, params object[] args)
                    // Currently getting args plugin name, plugin function called (Name, ldarg_1)
                    // This should be ok, since the called function in the case of a real hook would be, e.g., OnEntityTakeDamage
                    //System.Reflection.ConstructorInfo constr = typeof(OxideMod).GetConstructors().First();
                    List<CodeInstruction> toInsert = new List<CodeInstruction>()
                    {
                        //new CodeInstruction(OpCodes.Newobj, constr),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldstr, "OnFoundNRE"),
                        new CodeInstruction(OpCodes.Ldc_I4_2),
                        new CodeInstruction(OpCodes.Newarr, typeof(object)),
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Ldc_I4_0),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Plugin), "get_Name")),
                        new CodeInstruction(OpCodes.Stelem_Ref),
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Stelem_Ref),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Plugin), "CallHook")),
                        //new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Interface), "CallHook")),
                        new CodeInstruction(OpCodes.Pop)
                    };
                    codes.InsertRange(startIndex, toInsert);
                }
                return codes;
            }
        }
    }
}
