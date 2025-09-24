using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide;
using Oxide.Plugins;
using UnityEngine;
using Oxide.Core;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("WarpsGUI", "PsychoTea", "1.0.1")]

    class WarpsGUI : RustPlugin
    {
        //Todo:
        //Show page numbers for many warps.

        static WarpsGUI Instance;
        const string permUse = "warpsgui.use";
        const string permAdmin = "warpsgui.admin";
        Dictionary<BasePlayer, bool> guiOpen = new Dictionary<BasePlayer, bool>();
        List<GameObject> gameObjects = new List<GameObject>();

        #region Classes

        class GamePos
        {
            public float x;
            public float y;
            public float z;

            GamePos()
            {
                x = x;
                y = y;
                z = z;
            }

            public GamePos(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public GamePos(Vector3 vec)
            {
                this.x = vec.x;
                this.y = vec.y;
                this.z = vec.z;
            }

            public Vector3 ToVector() => new Vector3(x, y, z);

            public override string ToString() => this.ToVector().ToString();

            public bool IsZero() => (this.ToVector() == Vector3.zero);

            public static explicit operator GamePos(Vector3 v) => new GamePos(v);
        }

        class CooldownManager : MonoBehaviour
        {
            GameObject GameObject;

            public static void Create()
            {
                CooldownManager cm = new CooldownManager();
                cm.GameObject = new GameObject();
                cm.GameObject.AddComponent<CooldownManager>();
                Instance.gameObjects.Add(cm.GameObject);
            }

            void Start()
            {
                InvokeRepeating("TimerTick", 0, 1.0f);
            }

            void TimerTick()
            {
                if (Instance?.storedData?.cooldowns == null) return;
                Dictionary<ulong, int> dict = new Dictionary<ulong, int>(Instance.storedData.cooldowns);
                foreach (KeyValuePair<ulong, int> kvp in dict)
                {
                    Instance.storedData.cooldowns[kvp.Key]--;
                    if (kvp.Value == 0)
                        Instance.storedData.cooldowns.Remove(kvp.Key);
                }
            }

            void OnDestroy()
            {
                Instance?.SaveData();
                CancelInvoke();
                Instance.gameObjects.Remove(GameObject);
            }
        }

        class Warper : MonoBehaviour
        {
            public int timerTime = -1;
            public string warpName;
            public Vector3 toPos;

            void Awake()
            {
                InvokeRepeating("TimerTick", 0f, 1.0f);
            }

            void TimerTick()
            {
                if (timerTime == -1) return;
                if (timerTime == 0)
                {
                    Teleport();
                    GameObject.Destroy(this);
                }
                timerTime--;
            }

            public void Cancel()
            {
                GameObject.Destroy(this);
            }

            void Teleport()
            {
                BasePlayer player = GetComponentInParent<BasePlayer>();
                Instance.Teleport(player, toPos);
                Instance.SendReply(player, Instance.GetMessage("YouHaveWarped").Replace("{0}", warpName));

                int cooldown = Instance.GetConfig<int>("DefaultCooldown");
                foreach (KeyValuePair<string, object> kvp in Instance.GetConfig<Dictionary<string, object>>("Cooldowns"))
                    if (Instance.permission.UserHasPermission(player.UserIDString, kvp.Key.ToString()))
                        if (Int32.Parse(kvp.Value.ToString()) < cooldown)
                            cooldown = Int32.Parse(kvp.Value.ToString());
                Instance.storedData.cooldowns.Add(player.userID, cooldown);
            }

            void OnDestroy()
            {
                Debug.Log("OnDestroy");
            }
        }

        class WarpPoint
        {
            public GamePos pos;
            public string permission;

            public WarpPoint(GamePos pos, string permission)
            {
                this.pos = pos;
                this.permission = permission;
            }
        }

        class StoredData
        {
            public Dictionary<string, WarpPoint> warps = new Dictionary<string, WarpPoint>();
            public Dictionary<ulong, int> cooldowns = new Dictionary<ulong, int>();
        }
        StoredData storedData = new StoredData();

        #endregion

        #region Oxide Hooks

        void Init()
        {
            ReadData();

            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(permAdmin, this);

            if (storedData?.warps?.Values != null)
                foreach (WarpPoint point in storedData.warps.Values)
                    RegisterPermission(point.permission);

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                { "NoPermission", "You don't have permission to use this command." },
                { "IncorrectUsage-Warp", "Incorrect usage! /warp {to/list} [name]" },
                { "IncorrectUsage-WarpAdd", "Incorrect usage! /warpadd {name} {permission}\nUse 'none' for no permission" },
                { "IncorrectUsage-WarpRemove", "Incorrect usage! /warpremove {name}" },
                { "WarpExists", "The warp {0} already exists." },
                { "WarpDoesntExist", "The warp {0} doesn't exist." },
                { "WarpCreatedNoPerm", "Warp {0} created with no permission." },
                { "WarpCreatedPerm", "Warp {0} created with permission {1}." },
                { "WarpRemoved", "Warp {0} removed." },
                { "WarpTitle", "Warps" },
                { "BuildBlocked", "You may not teleport whilst building blocked." },
                { "IsBleeding", "You may not teleporting whilst bleeding." },
                { "YouHaveWarped", "Warped to {0}." },
                { "WarpingIn", "Warping to {0} in {1} seconds..." },
                { "TeleportInterrupted", "Teleport interrupted!" },
                { "OnCooldown", "Your warps are on cooldown for {0} seconds." },
                { "NoPermissionWarp", "You don't have permission to use this warp." }
            }, this);
        }

        void OnServerInitialized()
        {
            Instance = this;

            CooldownManager.Create();
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (GetConfig<bool>("KeybindEnabled"))
                player.SendConsoleCommand($"bind {GetConfig<string>("KeybindKey")} warpgui");
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!(entity as BasePlayer)) return;
            BasePlayer player = entity as BasePlayer;

            if (GetConfig<bool>("DamageInterruptsWarp") && HasComponent<Warper>(player))
            {
                player.GetComponent<Warper>().Cancel();
                SendReply(player, GetMessage("TeleportInterrupted"));
            }
        }

        void Unload()
        {
            SaveData();

            foreach (GameObject go in gameObjects)
                GameObject.Destroy(go);
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");

            Config["PrefixEnabled"] = true;
            Config["PrefixText"] = "<color=orange>Warps: </color>";
            Config["TimeUntilTeleport"] = 3;
            Config["DefaultCooldown"] = 180;
            Config["Cooldowns"] = new Dictionary<string, int>()
            {
                { "warpsgui.vip", 60 },
                { "warpsgui.elite", 30 },
                { "warpsgui.god", 15 },
                { "warpsgui.none", 0 }
            };
            Config["KeybindEnabled"] = true;
            Config["KeybindKey"] = "n";
            Config["AllowTeleportWhilstBleeding"] = false;
            Config["AllowTeleportFromBuildBlock"] = false;
            Config["AdminInstaTP"] = true;
            Config["DamageInterruptsWarp"] = true;
        }

        #endregion

        #region Commands

        [ChatCommand("warp")]
        void warpCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length == 0)
            {
                ShowWarpGUI(player);
                return;
            }

            if (args[0] == "to")
            {
                string warpName = "";
                if (args.Length > 1)
                {
                    for (int i = 1; i < args.Length; i++)
                        warpName += " " + args[i];
                    warpName = warpName.Remove(0, 1);
                }
                WarpTo(player, warpName);
                return;
            }

            if (args[0] == "list")
            {
                string message = "";
                if (GetConfig<bool>("PrefixEnabled"))
                    message = GetConfig<string>("PrefixText") + "All Warps\n";
                else message = "All Warps\n";

                List<string> allowedWarps = new List<string>();
                foreach (KeyValuePair<string, WarpPoint> kvp in storedData.warps)
                    if (CheckPerm(player, kvp.Value.permission))
                        allowedWarps.Add(kvp.Key);
                allowedWarps.Sort();

                message += string.Join(", ", allowedWarps.ToArray());
                SendReply(player, message);
                return;
            }

            SendReply(player, GetMessage("IncorrectUsage-Warp"));
        }

        [ChatCommand("warpadd")]
        void warpAddCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasAdminPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length < 2)
            {
                SendReply(player, GetMessage("IncorrectUsage-WarpAdd"));
                return;
            }

            if (storedData.warps.ContainsKey(args[0]))
            {
                SendReply(player, GetMessage("WarpExists").Replace("{0}", args[0]));
                return;
            }

            if (!args[1].StartsWith("warpsgui.") && args[1] != "none")
                args[1] = "warpsgui." + args[1];

            GamePos pos = (GamePos)player.transform.position;
            storedData.warps.Add(args[0], new WarpPoint(pos, args[1]));
            SaveData();
            RegisterPermission(args[1]);

            if (args[1] == "none") SendReply(player, GetMessage("WarpCreatedNoPerm").Replace("{0}", args[0]));
            else SendReply(player, GetMessage("WarpCreatedPerm").Replace("{0}", args[0]).Replace("{1}", args[1]));
        }

        [ChatCommand("warpremove")]
        void warpRemoveCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasAdminPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length < 1)
            {
                SendReply(player, GetMessage("IncorrectUsage-WarpRemove"));
                return;
            }

            if (!storedData.warps.ContainsKey(args[0]))
            {
                SendReply(player, GetMessage("WarpDoesntExist").Replace("{0}", args[0]));
                return;
            }

            storedData.warps.Remove(args[0]);
            SaveData();
            SendReply(player, GetMessage("WarpRemoved").Replace("{0}", args[0]));
        }

        [ConsoleCommand("warpgui")]
        void warpGuiCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            BasePlayer player = arg.Player();

            string[] args = (arg.Args == null) ? new string[] { } : arg.Args;

            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length == 0) return;

            if (args[0] == "True")
            {
                ShowWarpGUI(player);
                return;
            }

            if (args[0] == "page")
            {
                if (args.Length < 2) return;
                int page;
                if (!Int32.TryParse(args[1], out page)) return;
                WarpUI(player, page);
            }

            if (args[0] == "close")
            {
                HideGUI(player);
                return;
            }

            if (args[0] == "to")
            {
                string warpName = "";
                if (args.Length > 1)
                {
                    for (int i = 1; i < args.Length; i++)
                        warpName += " " + args[i];
                    warpName = warpName.Remove(0, 1);
                }

                WarpTo(player, warpName);
                return;
            }
        }

        #endregion

        #region GUIs

        void ShowWarpGUI(BasePlayer player)
        {
            if (!guiOpen.ContainsKey(player))
                guiOpen.Add(player, false);
            if (guiOpen[player])
            {
                HideGUI(player);
                return;
            }
            guiOpen[player] = true;
            WarpUI(player);
        }

        void WarpUI(BasePlayer player, int page = 1)
        {
            CuiHelper.DestroyUi(player, "warpGUI");

            var GUIElement = new CuiElementContainer();

            #region Whole Panel

            var warpGUI = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.75"
                },
                RectTransform =
                {
                    AnchorMin = "0.3 0.3", //Left Bottom
                    AnchorMax = "0.7 0.75" // Right Top
                },
                CursorEnabled = true
            }, "Hud", "warpGUI");

            #endregion

            #region Title Bar

            var titleBar = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0.75"
                },
                RectTransform =
                {
                    AnchorMin = "0 0.9", //Left Bottom
                    AnchorMax = "0.998 0.999" // Right Top
                }
            }, warpGUI);

            #region Title

            GUIElement.Add(new CuiLabel
            {
                Text =
                {
                    Text = lang.GetMessage("WarpTitle", this),
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, titleBar);


            #endregion

            #region Close Button

            GUIElement.Add(new CuiButton
            {
                RectTransform =
                {
                    AnchorMin = "0.935 0",
                    AnchorMax = "0.998 0.97"
                },
                Button =
                {
                    Command = "warpgui close",
                    Color = "1 0 0 1"
                },
                Text =
                {
                    Text = "X",
                    FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, titleBar);

            #endregion

            #endregion

            #region Warp List

            var warpList = GUIElement.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 0.9"
                }
            }, warpGUI);

            const float columnWidth = 0.2f;
            const float rowWidth = 0.2f;

            List<string> allowedWarps = new List<string>();
            foreach (KeyValuePair<string, WarpPoint> kvp in storedData.warps)
                if (CheckPerm(player, kvp.Value.permission))
                    allowedWarps.Add(kvp.Key);
            allowedWarps.Sort();
            int maxPages = CalculatePages(allowedWarps.Count());

            int warpCount = (page * 25) - 25;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    var panel = GUIElement.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = (columnWidth * j).ToString() + " " + (1f - (rowWidth * i) - rowWidth).ToString(),
                            AnchorMax = ((columnWidth * j) + columnWidth).ToString() + " " + (1f - (rowWidth * i)).ToString()
                        },
                        Image =
                        {
                            Color = "0 0 0 0"
                        }
                    }, warpList);

                    if (allowedWarps.Count() <= warpCount) continue;
                    string warpName = allowedWarps.ToArray()[warpCount];
                    GUIElement.Add(new CuiButton
                    {
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        Text =
                        {
                            Text = warpName,
                            Align = TextAnchor.MiddleCenter,
                            FontSize = 18,
                            Color = "1 1 1 1",
                            Font = "robotocondensed-regular.ttf"
                        },
                        Button =
                        {
                            Command = $"warpgui to {warpName}",
                            Color = "0 0 0 0"
                        }
                    }, panel);

                    warpCount++;
                }
            }

            #endregion

            #region Page Buttons

            if (page < maxPages)
            {
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "1.025 0.575", //Left Top
                        AnchorMax = "1.125 0.725"    //Right Bottom
                    },
                    Text =
                    {
                        Text = "Up",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 16,
                        Color = "1 1 1 1"
                    },
                    Button =
                    {
                        Command = $"warpgui page {(page + 1).ToString()}",
                        Color = "0 0 0 0.75"
                    }
                }, warpGUI);
            }

            if (page > 1)
            {
                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "1.025 0.4",
                        AnchorMax = "1.125 0.55"
                    },
                    Text =
                    {
                        Text = "Down",
                        Align = TextAnchor.MiddleCenter,
                        FontSize = 16,
                        Color = "1 1 1 1"
                    },
                    Button =
                    {
                        Command = $"warpgui page {(page - 1).ToString()}",
                        Color = "0 0 0 0.75"
                    }
                }, warpGUI);
            }

            #endregion

            #region Empty List

            if (storedData.warps.Count() == 0)
            {
                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = "There are no warps set.",
                        FontSize = 20,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0.1",
                        AnchorMax = "1 1"
                    }
                }, warpList);
            }

            #endregion

            CuiHelper.AddUi(player, GUIElement);
        }

        void HideGUI(BasePlayer player)
        {
            if (!guiOpen.ContainsKey(player))
                guiOpen.Add(player, false);
            guiOpen[player] = false;
            CuiHelper.DestroyUi(player, "warpGUI");
        }

        #endregion

        #region Custom Functions

        void WarpTo(BasePlayer player, string warpName)
        {
            if (!storedData.warps.ContainsKey(warpName))
            {
                SendReply(player, GetMessage("WarpDoesntExist").Replace("{0}", warpName));
                return;
            }

            if (!CheckPerm(player, storedData.warps[warpName].permission))
            {
                SendReply(player, GetMessage("NoPermissionWarp"));
                return;
            }

            if (GetConfig<bool>("AdminInstaTP") && player.IsAdmin)
            {
                //Teleport instantly
                Teleport(player, storedData.warps[warpName].pos.ToVector());
                return;
            }

            if (!GetConfig<bool>("AllowTeleportFromBuildBlock") && !player.CanBuild())
            {
                SendReply(player, GetMessage("BuildBlocked"));
                return;
            }

            if (!GetConfig<bool>("AllowTeleportWhilstBleeding") && player.metabolism.bleeding.value > 0f)
            {
                SendReply(player, GetMessage("IsBleeding"));
                return;
            }

            if (storedData.cooldowns.ContainsKey(player.userID))
            {
                SendReply(player, GetMessage("OnCooldown").Replace("{0}", storedData.cooldowns[player.userID].ToString()));
                return;
            }

            int timeUntilTP = GetConfig<int>("TimeUntilTeleport");
            SendReply(player, GetMessage("WarpingIn").Replace("{0}", warpName).Replace("{1}", timeUntilTP.ToString()));
            Vector3 pos = storedData.warps[warpName].pos.ToVector();
            Warper warper = player.gameObject.AddComponent<Warper>();
            warper.warpName = warpName;
            warper.toPos = pos;
            warper.timerTime = timeUntilTP;
        }

        void RegisterPermission(string perm)
        {
            if (perm == "none" || perm == "warpsgui.none") return;
            permission.RegisterPermission(perm, this);
        }

        void Teleport(BasePlayer player, Vector3 pos)
        {
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            StartSleeping(player);
            player.MovePosition(pos);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", pos);
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
        }

        void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }

        #endregion

        #region Helpers

        bool HasPerm(BasePlayer player) => (permission.UserHasPermission(player.UserIDString, permUse) || player.IsAdmin);
        bool HasAdminPerm(BasePlayer player) => (permission.UserHasPermission(player.UserIDString, permAdmin)) || player.IsAdmin;

        bool HasComponent<T>(BasePlayer player) => (player.GetComponent<T>() != null);

        bool CheckPerm(BasePlayer player, string perm)
        {
            if (perm == "none")
                return true;
            if (permission.UserHasPermission(player.UserIDString, perm))
                return true;
            if (player.IsAdmin)
                return true;
            return false;
        }

        string GetMessage(string key)
        {
            string message = "";
            if (GetConfig<bool>("PrefixEnabled"))
                message += GetConfig<string>("PrefixText");
            message += lang.GetMessage(key, this);
            return message;
        }

        int CalculatePages(int value) => (int)Math.Ceiling(value / 25d);

        T GetConfig<T>(string name) => (T)Convert.ChangeType(Config[name], typeof(T));

        void SaveData() => Interface.Oxide.DataFileSystem.WriteObject<StoredData>(this.Title, storedData);
        void ReadData() => storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(this.Title);

        #endregion
    }
}