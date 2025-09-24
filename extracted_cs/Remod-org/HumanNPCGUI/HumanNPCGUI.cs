//#define DEBUG
#region License (GPL v3)
/*
    DESCRIPTION
    Copyright (c) 2021 RFC1920 <desolationoutpostpve@gmail.com>

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion License Information (GPL v3)
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("HumanNPC Editor GUI", "RFC1920", "1.0.10")]
    [Description("Oxide Plugin")]
    class HumanNPCGUI : RustPlugin
    {
        #region vars
        [PluginReference]
        Plugin HumanNPC, Kits;

        private const string permNPCGuiUse = "humannpcgui.use";
        const string NPCGUI = "npcgui.editor";
        const string NPCGUK = "npcgui.kitselect";
        const string NPCGUN = "npcgui.kitsetnum";
        const string NPCGUS = "npcgui.select";
        const string NPCGUV = "npcgui.setval";

        private List<ulong> isopen = new List<ulong>();
        #endregion

        #region Message
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private void Message(IPlayer player, string key, params object[] args) => player.Reply(Lang(key, player.Id, args));
        #endregion

        #region init
        void Init()
        {
            AddCovalenceCommand("npcgui", "NpcEdit");

            permission.RegisterPermission(permNPCGuiUse, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["npcgui"] = "HumanNPC GUI",
                ["npcguisel"] = "HumanNPCGUI NPC Select ",
                ["npcguikit"] = "HumanNPCGUI Kit Select",
                ["close"] = "Close",
                ["none"] = "None",
                ["needselect"] = "Select NPC",
                ["select"] = "Select",
                ["editing"] = "Editing",
                ["mustselect"] = "Please press 'Select' to choose an NPC.",
                ["guihelp1"] = "For blue buttons, click to toggle true/false.",
                ["guihelp2"] = "For all values above in gray, you may type a new value and press enter.",
                ["guihelp3"] = "For kit, press the button to select a kit.",
                ["add"] = "Add",
                ["new"] = "Create New",
                ["remove"] = "Remove",
                ["spawnhere"] = "Spawn Here",
                ["tpto"] = "Teleport to NPC",
                ["name"] = "Name",
                ["online"] = "Online",
                ["offline"] = "Offline",
                ["deauthall"] = "DeAuthAll",
                ["remove"] = "Remove"
            }, this);
        }

        void Loaded()
        {
        }

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, NPCGUI);
                CuiHelper.DestroyUi(player, NPCGUK);
                CuiHelper.DestroyUi(player, NPCGUN);
                CuiHelper.DestroyUi(player, NPCGUS);
                CuiHelper.DestroyUi(player, NPCGUV);
            }
        }

        private object OnUserCommand(BasePlayer player, string command, string[] args)
        {
            if (command != "npcgui" && isopen.Contains(player.userID))
            {
#if DEBUG
                Puts($"OnPlayerCommand: {command} BLOCKED");
#endif
                return true;
            }
            return null;
        }

        private object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (command != "npcgui" && isopen.Contains(player.userID))
            {
#if DEBUG
                Puts($"OnPlayerCommand: {command} BLOCKED");
#endif
                return true;
            }
            return null;
        }

        protected override void LoadDefaultConfig()
        {
        }
        #endregion

        #region Main
        [Command("npcgui")]
        void NpcEdit(IPlayer iplayer, string command, string[] args)
        {
            if (!iplayer.HasPermission(permNPCGuiUse)) return;
            var player = iplayer.Object as BasePlayer;
            ulong npc = 0;
#if DEBUG
            string debug = string.Join(",", args); Puts($"{debug}");
#endif
            // Sure, this looks a little crazy.  But, all the user needs to type is /npcgui.  The rest is driven from the GUI.
            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "npcselkit":
                        if (args.Length > 2)
                        {
                            NPCKitGUI(player, ulong.Parse(args[1]), args[2]);
                        }
                        break;
                    case "kitsel":
                        if (args.Length > 2)
                        {
                            CuiHelper.DestroyUi(player, NPCGUK);
                            npc = ulong.Parse(args[1]);
                            Interface.CallHook("SetHumanNPCInfo", npc, "spawnkit", args[2]);
                            NpcEditGUI(player, npc);
                        }
                        break;
                    case "npctoggle":
                        if (args.Length > 3)
                        {
                            npc = ulong.Parse(args[1]);
                            string toset = args[2];
                            string newval = args[3] == "True" ? "false" : "true";
                            Interface.CallHook("SetHumanNPCInfo", npc, toset, newval);
                        }
                        NpcEditGUI(player, npc);
                        break;
                    case "spawn":
                        if (args.Length > 3)
                        {
                            npc = ulong.Parse(args[1]);
                            string pos = args[3];
                            pos = pos.Replace("(","").Replace(")","");
                            Interface.CallHook("SetHumanNPCInfo", npc, "spawn", pos);
                            NpcEditGUI(player, npc);
                        }
                        break;
                    case "spawnhere":
                        if (args.Length > 1)
                        {
                            npc = ulong.Parse(args[1]);
                            string newSpawn = player.transform.position.x.ToString() + "," + player.transform.position.y + "," + player.transform.position.z.ToString();
                            Quaternion newRot;
                            TryGetPlayerView(player, out newRot);
                            Interface.CallHook("SetHumanNPCInfo", npc, "spawn", newSpawn, newRot.ToString());
                            NpcEditGUI(player, npc);
                        }
                        break;
                    case "tpto":
                        if (args.Length > 1)
                        {
                            npc = ulong.Parse(args[1]);
                            CuiHelper.DestroyUi(player, NPCGUI);
                            string spawnpos = (string)HumanNPC?.Call("GetHumanNPCInfo", npc, "spawnInfo");
                            IsOpen(player.userID, false);
                            Teleport(player, StringToVector3(spawnpos));
                        }
                        break;
                    case "new":
                        CuiHelper.DestroyUi(player, NPCGUS);
                        Quaternion currentRot;
                        TryGetPlayerView(player, out currentRot);
                        npc = (ulong)Interface.CallHook("SpawnHumanNPC", player.transform.position, currentRot, "HumanNPCGUI");
                        NpcEditGUI(player, npc);
                        break;
                    case "remove":
                        if (args.Length == 2)
                        {
                            npc = ulong.Parse(args[1]);
                            CuiHelper.DestroyUi(player, NPCGUI);
                            Interface.CallHook("RemoveHumanNPC", npc);
                            NpcEditGUI(player);
                        }
                        break;
                    case "npcset":
                        if (args.Length > 1)
                        {
                            if (args.Length > 4)
                            {
                                npc = ulong.Parse(args[1]);
#if DEBUG
                                Puts($"Calling SetHumanNPCInfo {args[2]} {args[4]}");
#endif
                                Interface.CallHook("SetHumanNPCInfo", npc, args[2], args[4]);
                                NpcEditGUI(player, npc);
                            }
                        }
                        break;
                    case "close":
                        IsOpen(player.userID, false);
                        CuiHelper.DestroyUi(player, NPCGUS);
                        CuiHelper.DestroyUi(player, NPCGUI);
                        CuiHelper.DestroyUi(player, NPCGUK);
                        break;
                    case "select":
                        NPCSelectGUI(player);
                        break;
                    case "selclose":
                        CuiHelper.DestroyUi(player, NPCGUS);
                        break;
                    case "selkitclose":
                        CuiHelper.DestroyUi(player, NPCGUK);
                        break;
                    case "npc":
                    default:
                        if (args.Length > 1)
                        {
                            CuiHelper.DestroyUi(player, NPCGUS);
                            npc = ulong.Parse(args[1]);
                            NpcEditGUI(player, npc);
                        }
                        break;
                }
            }
            else
            {
                if (isopen.Contains(player.userID)) return;
                NpcEditGUI(player);
            }
        }

        private void IsOpen(ulong uid, bool set=false)
        {
            if (set)
            {
#if DEBUG
                Puts($"Setting isopen for {uid}");
#endif
                if (!isopen.Contains(uid)) isopen.Add(uid);
                return;
            }
#if DEBUG
            Puts($"Clearing isopen for {uid}");
#endif
            isopen.Remove(uid);
        }

        void NpcEditGUI(BasePlayer player, ulong npc = 0)
        {
            if (player == null) return;
            IsOpen(player.userID, true);
            CuiHelper.DestroyUi(player, NPCGUI);

            string npcname = Lang("needselect");
            if (npc > 0)
            {
                npcname = Lang("editing") + " " + (string)HumanNPC?.Call("HumanNPCname", npc);
            }

            CuiElementContainer container = UI.Container(NPCGUI, UI.Color("2b2b2b", 1f), "0.05 0.05", "0.95 0.95", true, "Overlay");
            if (npc == 0)
            {
                UI.Button(ref container, NPCGUI, UI.Color("#cc3333", 1f), Lang("new"), 12, "0.79 0.95", "0.85 0.98", $"npcgui new");
            }
            else
            {
                UI.Button(ref container, NPCGUI, UI.Color("#cc3333", 1f), Lang("remove"), 12, "0.79 0.95", "0.85 0.98", $"npcgui remove {npc.ToString()}");
            }
            UI.Button(ref container, NPCGUI, UI.Color("#d85540", 1f), Lang("select"), 12, "0.86 0.95", "0.92 0.98", $"npcgui select");
            UI.Button(ref container, NPCGUI, UI.Color("#d85540", 1f), Lang("close"), 12, "0.93 0.95", "0.99 0.98", $"npcgui close");
            UI.Label(ref container, NPCGUI, UI.Color("#ffffff", 1f), Lang("npcgui") + ": " + npcname, 24, "0.2 0.92", "0.7 1");

            int col = 0;
            int row = 0;

            if (npc == 0)
            {
                UI.Label(ref container, NPCGUI, UI.Color("#ffffff", 1f), Lang("mustselect"), 24, "0.2 0.47", "0.7 0.53");
            }
            else
            {
                Dictionary<string, string> npcinfo = new Dictionary<string, string>();
                object x = HumanNPC?.Call("GetHumanNPCInfos", npc);
                if (x != null)
                {
                    var json = JsonConvert.SerializeObject(x);
                    npcinfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                }
                npcinfo["spawnInfo"] = (string)HumanNPC?.Call("GetHumanNPCInfo", npc, "spawnInfo");
                npcinfo["hello"] = (string)HumanNPC?.Call("GetHumanNPCInfo", npc, "hello");
                npcinfo["bye"] = (string)HumanNPC?.Call("GetHumanNPCInfo", npc, "bye");
                npcinfo["hurt"] = (string)HumanNPC?.Call("GetHumanNPCInfo", npc, "hurt");
                npcinfo["use"] = (string)HumanNPC?.Call("GetHumanNPCInfo", npc, "use");
                npcinfo["kill"] = (string)HumanNPC?.Call("GetHumanNPCInfo", npc, "kill");
                npcinfo["kit"] = (string)HumanNPC?.Call("GetHumanNPCInfo", npc, "spawnkit");

                var validAttrs = new string[] {
                    "displayName", "kit", "invulnerable", "lootable", "hostile",
                    "ahostile", "defend", "evade", "follow", "followtime", "allowsit",
                    "allowride", "needsAmmo", "dropWeapon", "health", "attackDistance",
                    "damageAmount", "damageInterval", "maxDistance", "damageDistance",
                    "collisionRadius", "respawnSeconds", "speed", "stopandtalk",
                    "stopandtalkSeconds", "hitchance", "reloadDuration", "band",
                    "hostileTowardsArmed", "hostileTowardsArmedHard", "raiseAlarm",
                    "spawnInfo", "hello", "bye", "hurt", "use", "kill", "kit"
                };

                Dictionary<string, bool> isBool = new Dictionary<string, bool>
                {
                    { "enable", true },
                    { "invulnerable", true },
                    { "lootable", true },
                    { "hostile", true },
                    { "ahostile", true },
                    { "defend", true },
                    { "evade", true },
                    { "follow", true },
                    { "allowsit", true },
                    { "allowride", true },
                    { "needsAmmo", true },
                    { "dropWeapon", true },
                    { "stopandtalk", true },
                    { "hostileTowardsArmed", true },
                    { "hostileTowardsArmedHard", true },
                    { "raiseAlarm", true }
                };
                Dictionary<string, bool> isLarge = new Dictionary<string, bool>
                {
                    { "hello", true },
                    { "bye", true },
                    { "hurt", true },
                    { "use", true },
                    { "kill", true }
                };

                foreach (KeyValuePair<string, string> info in npcinfo)
                {
                    if (!validAttrs.Contains(info.Key)) continue;
                    if (row > 11)
                    {
                        row = 0;
                        col++;
                        col++;
                    }
                    float[] posl = GetButtonPositionP(row, col);
                    float[] posb = GetButtonPositionP(row, col + 1);

                    if (info.Key == "kit")
                    {
                        posl = GetButtonPositionP(11, 4);
                        posb = GetButtonPositionP(11, 5);
                        if (plugins.Exists("Kits"))
                        {
                            string kitname = info.Value ?? Lang("none");
                            UI.Button(ref container, NPCGUI, UI.Color("#d85540", 1f), kitname, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui npcselkit {npc.ToString()} {kitname}");
                        }
                        else
                        {
                            UI.Label(ref container, NPCGUI, UI.Color("#ffffff", 1f), Lang("none"), 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}");
                        }
                    }
                    else if (info.Key == "spawnInfo")
                    {
                        UI.Label(ref container, NPCGUI, UI.Color("#535353", 1f), info.Value, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}");
                        UI.Input(ref container, NPCGUI, UI.Color("#ffffff", 1f), info.Value, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui spawn {npc.ToString()} {info.Key} ");
                        posb = GetButtonPositionP(0, 6);
                        UI.Button(ref container, NPCGUI, UI.Color("#d85540", 1f), Lang("spawnhere"), 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui spawnhere {npc.ToString()} ");
                        if (StringToVector3(info.Value) != Vector3.zero)
                        {
                            row++;
                            posb = GetButtonPositionP(1, 6);
                            UI.Button(ref container, NPCGUI, UI.Color("#d85540", 1f), Lang("tpto"), 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui tpto {npc.ToString()} ");
                        }
                    }
                    else if (isLarge.ContainsKey(info.Key))
                    {
                        //string oldval = info.Value != null ? info.Value : Lang("unset");
                        //UI.Label(ref container, NPCGUI, UI.Color("#535353", 1f), oldval, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]))} {posb[3]}");
                        //UI.Input(ref container, NPCGUI, UI.Color("#ffffff", 1f), oldval, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]))} {posb[3]}", $"npcgui npcset {npc.ToString()} {info.Key} {oldval} ");
                    }
                    else if (isBool.ContainsKey(info.Key))
                    {
                        UI.Button(ref container, NPCGUI, UI.Color("#222255", 1f), info.Value, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui npctoggle {npc.ToString()} {info.Key} {info.Value}");
                    }
                    else
                    {
                        string oldval = info.Value ?? Lang("unset");
                        UI.Label(ref container, NPCGUI, UI.Color("#535353", 1f), oldval, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}");
                        UI.Input(ref container, NPCGUI, UI.Color("#ffffff", 1f), oldval, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui npcset {npc.ToString()} {info.Key} ");
                    }
                    if (!isLarge.ContainsKey(info.Key))
                    {
                        // Label for each attribute
                        UI.Label(ref container, NPCGUI, UI.Color("#ffffff", 1f), info.Key, 12, $"{posl[0]} {posl[1]}", $"{posl[0] + ((posl[2] - posl[0]) / 2)} {posl[3]}");
                    }

                    row++;
                }
                UI.Label(ref container, NPCGUI, UI.Color("#ffffff", 1f), Lang("guihelp1"), 12, "0.02 0.08", "0.9 0.11");
                UI.Label(ref container, NPCGUI, UI.Color("#ffffff", 1f), Lang("guihelp2"), 12, "0.02 0.04", "0.9 0.07");
                UI.Label(ref container, NPCGUI, UI.Color("#ffffff", 1f), Lang("guihelp3"), 12, "0.02 0", "0.9 0.03");
            }

            CuiHelper.AddUi(player, container);
        }

        void NPCMessageGUI(BasePlayer player, ulong npc, string field, string message)
        {
        }

        void NPCSelectGUI(BasePlayer player)
        {
            IsOpen(player.userID, true);
            CuiHelper.DestroyUi(player, NPCGUS);

            string description = Lang("npcguisel");
            CuiElementContainer container = UI.Container(NPCGUS, UI.Color("242424", 1f), "0.1 0.1", "0.9 0.9", true, "Overlay");
            UI.Label(ref container, NPCGUS, UI.Color("#ffffff", 1f), description, 18, "0.23 0.92", "0.7 1");
            UI.Label(ref container, NPCGUS, UI.Color("#22cc44", 1f), Lang("musician"), 12, "0.72 0.92", "0.77 1");
            UI.Label(ref container, NPCGUS, UI.Color("#2244cc", 1f), Lang("standard"), 12, "0.79 0.92", "0.86 1");
            UI.Button(ref container, NPCGUS, UI.Color("#d85540", 1f), Lang("close"), 12, "0.92 0.93", "0.985 0.98", $"npcgui selclose");
            int col = 0;
            int row = 0;

            List<ulong> npcs = (List<ulong>)HumanNPC?.Call("HumanNPCs");
            foreach (ulong npc in npcs)
            {
                if (row > 10)
                {
                    row = 0;
                    col++;
                }
                var hBand = (string)HumanNPC?.Call("GetHumanNPCInfo", npc, "band");
                if (hBand == "99") continue;
                string color = "#2244cc";
                if (hBand != "0") color = "#22cc44";

                var hName = (string)HumanNPC?.Call("HumanNPCname", npc);
                float[] posb = GetButtonPositionP(row, col);
                UI.Button(ref container, NPCGUS, UI.Color(color, 1f), hName, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui npc {npc.ToString()}");
                row++;
            }
            float[] posn = GetButtonPositionP(row, col);
            UI.Button(ref container, NPCGUS, UI.Color("#cc3333", 1f), Lang("new"), 12, $"{posn[0]} {posn[1]}", $"{posn[0] + ((posn[2] - posn[0]) / 2)} {posn[3]}", $"npcgui new");

            CuiHelper.AddUi(player, container);
        }

        void NPCKitGUI(BasePlayer player, ulong npc, string kit)
        {
            IsOpen(player.userID, true);
            CuiHelper.DestroyUi(player, NPCGUK);

            string description = Lang("npcguikit");
            CuiElementContainer container = UI.Container(NPCGUK, UI.Color("242424", 1f), "0.1 0.1", "0.9 0.9", true, "Overlay");
            UI.Label(ref container, NPCGUK, UI.Color("#ffffff", 1f), description, 18, "0.23 0.92", "0.7 1");
            UI.Button(ref container, NPCGUK, UI.Color("#d85540", 1f), Lang("close"), 12, "0.92 0.93", "0.985 0.98", $"npcgui selkitclose");

            int col = 0;
            int row = 0;
            List<string> kits = new List<string>();
            Kits?.CallHook("GetKitNames", kits);
            if (kits == null) return;
            foreach (var kitinfo in kits)
            {
                if (row > 10)
                {
                    row = 0;
                    col++;
                }
                float[] posb = GetButtonPositionP(row, col);

                if (kit == null) kit = Lang("none");
                if (kitinfo == kit)
                {
                    UI.Button(ref container, NPCGUK, UI.Color("#d85540", 1f), kitinfo, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui kitsel {npc.ToString()} {kitinfo}");
                }
                else
                {
                    UI.Button(ref container, NPCGUK, UI.Color("#424242", 1f), kitinfo, 12, $"{posb[0]} {posb[1]}", $"{posb[0] + ((posb[2] - posb[0]) / 2)} {posb[3]}", $"npcgui kitsel {npc.ToString()} {kitinfo}");
                }
                row++;
            }

            CuiHelper.AddUi(player, container);
        }

        private int RowNumber(int max, int count) => Mathf.FloorToInt(count / max);
        private float[] GetButtonPosition(int rowNumber, int columnNumber)
        {
            float offsetX = 0.05f + (0.096f * columnNumber);
            float offsetY = (0.80f - (rowNumber * 0.064f));

            return new float[] { offsetX, offsetY, offsetX + 0.196f, offsetY + 0.03f };
        }
        private float[] GetButtonPositionP(int rowNumber, int columnNumber)
        {
            float offsetX = 0.05f + (0.126f * columnNumber);
            float offsetY = (0.87f - (rowNumber * 0.064f));

            return new float[] { offsetX, offsetY, offsetX + 0.226f, offsetY + 0.03f };
        }

        private bool TryGetPlayerView(BasePlayer player, out Quaternion viewAngle)
        {
            viewAngle = new Quaternion(0f, 0f, 0f, 0f);
            if (player.serverInput?.current == null) return false;
            viewAngle = Quaternion.Euler(player.serverInput.current.aimAngles);
            return true;
        }

        public void Teleport(BasePlayer player, Vector3 position)
        {
            if (player.net?.connection != null) player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);

            player.SetParent(null, true, true);
            player.EnsureDismounted();
            player.Teleport(position);
            player.UpdateNetworkGroup();
            player.StartSleeping();
            player.SendNetworkUpdateImmediate(false);

            if (player.net?.connection != null) player.ClientRPCPlayer(null, player, "StartLoading");
        }

        private void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping()) return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player)) BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }

        public static Vector3 StringToVector3(string sVector)
        {
            // Remove the parentheses
            if (sVector.StartsWith("(") && sVector.EndsWith(")"))
            {
                sVector = sVector.Substring(1, sVector.Length - 2);
            }

            // split the items
            string[] sArray = sVector.Split(',');

            // store as a Vector3
            Vector3 result = new Vector3(
                float.Parse(sArray[0]),
                float.Parse(sArray[1]),
                float.Parse(sArray[2]));

            return result;
        }
        #endregion

        #region classes
        public static class UI
        {
            public static CuiElementContainer Container(string panel, string color, string min, string max, bool useCursor = false, string parent = "Overlay")
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = { Color = color },
                            RectTransform = {AnchorMin = min, AnchorMax = max},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panel
                    }
                };
                return container;
            }
            public static void Panel(ref CuiElementContainer container, string panel, string color, string min, string max, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = min, AnchorMax = max },
                    CursorEnabled = cursor
                },
                panel);
            }
            public static void Label(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = min, AnchorMax = max }
                },
                panel);

            }
            public static void Button(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0f },
                    RectTransform = { AnchorMin = min, AnchorMax = max },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }
            public static void Input(ref CuiElementContainer container, string panel, string color, string text, int size, string min, string max, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = align,
                            CharsLimit = 30,
                            Color = color,
                            Command = command + text,
                            FontSize = size,
                            IsPassword = false,
                            Text = text
                        },
                        new CuiRectTransformComponent { AnchorMin = min, AnchorMax = max },
                        new CuiNeedsCursorComponent()
                    }
                });
            }
            public static void Icon(ref CuiElementContainer container, string panel, string color, string imageurl, string min, string max)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Url = imageurl,
                            Sprite = "assets/content/textures/generic/fulltransparent.tga",
                            Color = color
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = min,
                            AnchorMax = max
                        }
                    }
                });
            }
            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                {
                    hexColor = hexColor.Substring(1);
                }
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        #endregion
    }
}
