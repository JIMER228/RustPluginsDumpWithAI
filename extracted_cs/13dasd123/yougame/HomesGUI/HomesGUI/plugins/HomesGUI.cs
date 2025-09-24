// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;
using System.Linq;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HomesGUI", "PsychoTea", "1.1.9")]

    class HomesGUI : RustPlugin
    {
        #region Fields

        static HomesGUI Instance;
        const string permUse = "homesgui.use";
        const string permBack = "homesgui.back";

        GameObject CMObject;
        bool DebuggingMode = false;

        Dictionary<BasePlayer, bool> GUIOpen = new Dictionary<BasePlayer, bool>();
        Dictionary<BasePlayer, Vector3> HomeBack = new Dictionary<BasePlayer, Vector3>();

        [PluginReference] Plugin Economics;
        [PluginReference] Plugin ServerRewards;
        [PluginReference] Plugin ZoneManager;

        #endregion

        #region Classes

        class GamePos
        {
            public float x;
            public float y;
            public float z;

            public GamePos(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public static explicit operator GamePos(Vector3 v) => new GamePos(v.x, v.y, v.z);

            public Vector3 ToVector() => new Vector3(x, y, z);
        }

        class HomeTeleporter : MonoBehaviour
        {
            BasePlayer Player { get { return GetComponentInParent<BasePlayer>(); } }
            int TimeUntilTeleport;
            public Vector3 Pos;
            public string HomeName;

            public void Go()
            {
                Instance.SendReply(Player, Instance.GetMessage("TeleportingTo").Replace("{0}", HomeName).Replace("{1}", TimeUntilTeleport.ToString()));
                InvokeRepeating("TimerTick", 0, 1.0f);
            }

            void Awake()
            {
                name = "HomeTeleporter";

                TimeUntilTeleport = Instance.GetConfig<int>("DefaultTimeUntilTeleport");
                foreach (var kvp in Instance.GetConfig<Dictionary<string, object>>("TimeUntilTeleport"))
                    if (Instance.permission.UserHasPermission(Player.UserIDString, kvp.Key))
                        if (Int32.Parse(kvp.Value.ToString()) < TimeUntilTeleport)
                            TimeUntilTeleport = Int32.Parse(kvp.Value.ToString());
            }

            void TimerTick()
            {
                if (TimeUntilTeleport == 0)
                {
                    Teleport();
                    GameObject.Destroy(this);
                }
                TimeUntilTeleport--;
            }

            public void CancelTeleport()
            {
                if (Instance.EconomicsInstalled() && Instance.GetConfig<bool>("UseEconomicsPlugin"))
                    Instance.RefundPlayerEconomics(Player);
                if (Instance.ServerRewardsInstalled() && Instance.GetConfig<bool>("UseServerRewardsPlugin"))
                    Instance.RefundServerRewards(Player);
                if (Instance.storedData.Cooldowns.ContainsKey(Player.userID))
                    Instance.storedData.Cooldowns.Remove(Player.userID);
                GameObject.Destroy(this);
            }

            void Teleport()
            {
                Instance.RecordHomeBack(Player);
                Instance.Teleport(Player, Pos);
                Instance.SendReply(Player, Instance.GetMessage("TeleportedTo").Replace("{0}", HomeName));

                Instance.AssignCooldown(Player);
                Instance.RecordUse(Player);
            }
        }

        class CooldownManager : MonoBehaviour
        {
            void Awake()
            {
                name = "CooldownManager";
                InvokeRepeating("TimerTick", 0, 1.0f);
            }

            void TimerTick()
            {
                foreach (var kvp in new Dictionary<ulong, int>(Instance.storedData.Cooldowns))
                {
                    Instance.storedData.Cooldowns[kvp.Key]--;
                    if (kvp.Value == 0)
                        Instance.storedData.Cooldowns.Remove(kvp.Key);
                }
            }
        }

        class StoredData
        {
            public Dictionary<ulong, Dictionary<string, GamePos>> Homes = new Dictionary<ulong, Dictionary<string, GamePos>>();
            public Dictionary<ulong, int> Cooldowns = new Dictionary<ulong, int>();
            public Dictionary<ulong, int> Uses = new Dictionary<ulong, int>();
        }
        StoredData storedData;

        #endregion

        #region Oxide Hooks

        void Init()
        {
            DebuggingMode = (ConVar.Server.hostname == "PsychoTea's Testing Server");

            //Register permissions
            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(permBack, this);
            foreach (string perm in GetConfig<Dictionary<string, object>>("Cooldowns").Keys)
                if (!permission.PermissionExists(perm))
                    permission.RegisterPermission(perm, this);
            foreach (string perm in GetConfig<Dictionary<string, object>>("MaxHomes").Keys)
                if (!permission.PermissionExists(perm))
                    permission.RegisterPermission(perm, this);
            foreach (string perm in GetConfig<Dictionary<string, object>>("TimeUntilTeleport").Keys)
                if (!permission.PermissionExists(perm))
                    permission.RegisterPermission(perm, this);
            foreach (string perm in GetConfig<Dictionary<string, object>>("MaxUses").Keys)
                if (!permission.PermissionExists(perm))
                    permission.RegisterPermission(perm, this);

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                { "NoPermission", "You do not have permission to use this command." },
                { "HomesTitle", "Дом" },
                { "SetHome-Usage", "Incorrect usage! /sethome {name}" },
                { "HomeAlreadyExists", "You already have a home called {0}." },
                { "HomeCreated", "Home {0} set." },
                { "DelHome-Usage", "Incorrect usage! /delhome {name}" },
                { "HomeDoesntExist", "The home {0} doesn't exist." },
                { "HomeDeleted", "Home {0} deleted." },
                { "HomeInBuildBlock", "You may not set a home whilst build blocked." },
                { "NoHomesSet", "You have no homes set." },
                { "HomesList", "{0}" },
                { "TeleportingTo", "Teleporting to home {0} in {1} seconds..." },
                { "TeleportedTo", "You have teleported to home {0}." },
                { "HomeLimitedReached", "You have reached the home limit." },
                { "TeleportWhilstBuildBlock", "You may not use teleport whilst building blocked." },
                { "TeleportWhilstBleeding", "You may not teleport whilst bleeding." },
                { "TeleportWhilstCrafting", "You may not teleport whilst crafting." },
                { "OnCooldown", "Your home teleport is on cooldown for {0} seconds." },
                { "YouTookDamage", "You took damage; home teleport cancelled." },
                { "CantAffordEconomics", "You can't afford this! Price: ${0}" },
                { "EconomicsYouSpent", "You spent ${0} on this home teleport." },
                { "EconomicsRefunded", "You were refunded ${0}." },
                { "CantAffordServerRewards", "You can't afford this! Price: {0}RP" },
                { "ServerRewardsYouSpent", "You spent {0}RP on this home teleport." },
                { "ServerRewardsRefunded", "You were refunded {0}RP." },
                { "MustBeOnFoundation", "You must be on a foundation to set a home." },
                { "MustBeOnFoundationOrFloor", "You must be on a foundation or a floor to set a home." },
                { "HomeBuildBlockDestroyed", "The building your home was set on has been destroyed." },
                { "NoPreviousHomes", "You have no previous homes to return to." },
                { "TeleportedBack", "Teleported back to your previous location." },
                { "AlreadyTeleporting", "You are already teleporting somewhere." },
                { "NoTeleportsToCancel", "You don't have any teleports to cancel." },
                { "TeleportCancelled", "Teleport cancelled." },
                { "TeleportIntoBuildBlock", "You may not teleport into a building blocked zone." },
                { "NoSetHomeInZones", "You may not set home in this ZoneManager zone." },
                { "MaxUsesReached", "You have reached your max uses of {0} for today." }
            }, this, "en");

            //Register command alises
            foreach (string cmdAlias in GetConfig<List<object>>("HomeCommandAliases"))
                cmd.AddChatCommand(cmdAlias, this, "homeCommand");

            ReadData();

            if (DebuggingMode) BasePlayer.activePlayerList.ForEach(x => ShowUI(x));

            timer.Every(60f, () =>
            {
                if (DateTime.Now.Hour == 0 && DateTime.Now.Minute == 0)
                {
                    storedData.Uses.Clear();
                }
            });
        }

        void OnServerInitialized()
        {
            Instance = this;

            ReadData();

            CMObject = new GameObject();
            CMObject.AddComponent<CooldownManager>();

            if (Economics == null && GetConfig<bool>("UseEconomicsPlugin"))
            {
                Debug.LogError("[TeleportGUI] Error! Economics is enabled in the config but is not installed! Please install Economics or disable 'UseEconomicsPlugin' in the config!");
            }

            if (ServerRewards == null && GetConfig<bool>("UseServerRewardsPlugin"))
            {
                Debug.LogError("[TeleportGUI] Error! ServerRewards is enabled in the config but is not installed! Please install ServerRewards or disable 'UseServerRewardsPlugin' in the config!");
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (GetConfig<bool>("KeybindEnabled"))
                player.SendConsoleCommand($"bind {GetConfig<string>("KeybindKey")} homegui");
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!GetConfig<bool>("ShouldCancelOnDamage")) return;
            if (!(entity is BasePlayer)) return;
            BasePlayer player = entity as BasePlayer;

            if (HasComponent<HomeTeleporter>(player))
            {
                player.GetComponent<HomeTeleporter>().CancelTeleport();
                SendReply(player, GetMessage("YouTookDamage"));
            }
        }

        void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                HideUI(player);

            SaveData();

            GameObject.Destroy(CMObject);
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");

            Config["PrefixEnabled"] = true;
            Config["PrefixText"] = "<color=orange>Дом: </color>";
            Config["DefaultTimeUntilTeleport"] = 15;
            Config["TimeUntilTeleport"] = new Dictionary<string, int>()
            {
                { "homesgui.vip", 10 },
                { "homesgui.elite", 5 },
                { "homesgui.god", 3 },
                { "homesgui.none", 0 }
            };
            Config["HomeCommandAliases"] = new List<string>() { };
            Config["DefaultCooldown"] = 180;
            Config["Cooldowns"] = new Dictionary<string, int>()
            {
                { "homesgui.vip", 60 },
                { "homesgui.elite", 30 },
                { "homesgui.god", 15 },
                { "homesgui.none", 0 }
            };
            Config["DefaultMaxHomes"] = 3;
            Config["MaxHomes"] = new Dictionary<string, int>()
            {
                { "homesgui.vip", 4 },
                { "homesgui.elite", 6 },
                { "homesgui.god", 10 },
                { "homesgui.unlimited", 0 }
            };
            Config["DefaultMaxUses"] = 5;
            Config["MaxUses"] = new Dictionary<string, int>()
            {
                { "homesgui.vip", 10 },
                { "homesgui.elite", 20 },
                { "homesgui.god", 30 },
                { "homesgui.unlimited", 0 }
            };
            Config["KeybindEnabled"] = true;
            Config["KeybindKey"] = "h";
            Config["AllowTeleportWhilstBleeding"] = false;
            Config["AllowTeleportFromBuildBlock"] = false;
            Config["AllowTeleportToBuildBlock"] = false;
            Config["UseEconomicsPlugin"] = false;
            Config["EconomicsPrice"] = 100;
            Config["UseServerRewardsPlugin"] = false;
            Config["ServerRewardsPrice"] = 10;
            Config["BlockTPCrafting"] = true;
            Config["AdminInstaTP"] = false;
            Config["AllowSetHomeInBuildBlocked"] = false;
            Config["MustSetHomeOnBuilding"] = true;
            Config["CanSetHomeOnFloor"] = false;
            Config["CheckForBuildingOnHomeTP"] = true;
            Config["ShouldCancelOnDamage"] = true;
        }

        #endregion

        #region Commands

        [ChatCommand("home")]
        void HomeCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length > 0)
            {
                if (!storedData.Homes.ContainsKey(player.userID))
                    storedData.Homes.Add(player.userID, new Dictionary<string, GamePos>());

                if (!storedData.Homes[player.userID].ContainsKey(args[0]))
                {
                    SendReply(player, GetMessage("HomeDoesntExist").Replace("{0}", args[0]));
                    return;
                }

                Vector3 pos = storedData.Homes[player.userID][args[0]].ToVector();

                object canTP = AllowedToTeleport(player, pos);
                if (canTP is string)
                {
                    SendReply(player, canTP.ToString());
                    return;
                }

                HomeTeleporter ht = player.gameObject.AddComponent<HomeTeleporter>();
                ht.Pos = pos;
                ht.HomeName = args[0];//Do time until teleport
                ht.Go();
                return;
            }

            ShowHomesUI(player);
        }

        [ChatCommand("sethome")]
        void SetHomeCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length < 1)
            {
                SendReply(player, GetMessage("SetHome-Usage"));
                return;
            }

            if (!storedData.Homes.ContainsKey(player.userID))
                storedData.Homes.Add(player.userID, new Dictionary<string, GamePos>());

            if (storedData.Homes[player.userID].ContainsKey(args[0]))
            {
                SendReply(player, GetMessage("HomeAlreadyExists").Replace("{0}", args[0]));
                return;
            }

            if (!CanSetHome(player)) return;

            storedData.Homes[player.userID].Add(args[0], (GamePos)player.transform.position);
            SaveData();
            SendReply(player, GetMessage("HomeCreated").Replace("{0}", args[0]));
        }

        [ChatCommand("delhome")]
        void DelHomeCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length < 1)
            {
                SendReply(player, GetMessage("DelHome-Usage"));
                return;
            }

            if (!storedData.Homes.ContainsKey(player.userID))
                storedData.Homes.Add(player.userID, new Dictionary<string, GamePos>());

            if (!storedData.Homes[player.userID].ContainsKey(args[0]))
            {
                SendReply(player, GetMessage("HomeDoesntExist").Replace("{0}", args[0]));
                return;
            }

            storedData.Homes[player.userID].Remove(args[0]);
            SaveData();
            SendReply(player, GetMessage("HomeDeleted").Replace("{0}", args[0]));
        }

        [ChatCommand("listhomes")]
        void ListHomesCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (!storedData.Homes.ContainsKey(player.userID))
                storedData.Homes.Add(player.userID, new Dictionary<string, GamePos>());

            if (storedData.Homes[player.userID].Count() == 0)
            {
                SendReply(player, GetMessage("NoHomesSet"));
                return;
            }

            string homes = string.Join(", ", storedData.Homes[player.userID].Keys.ToArray());
            SendReply(player, GetMessage("HomesList").Replace("{0}", homes));
        }

        [ChatCommand("homec")]
        void HomeCancelCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            SendReply(player, TPCancel(player));
        }

        [ChatCommand("homeback")]
        void HomeBackCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPerm(player, permBack))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            TPBack(player);
        }

        [ConsoleCommand("homegui")]
        void HomeGUICommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            BasePlayer player = arg.Player();

            string[] args = arg.Args ?? new string[] { };

            if (!HasPerm(player))
            {
                SendReply(player, GetMessage("NoPermission"));
                return;
            }

            if (args.Length == 0 || args[0] == "True" || args[0] == "close")
            {
                ShowHomesUI(player);
                return;
            }

            if (args[0] == "to")
            {
                if (!storedData.Homes.ContainsKey(player.userID))
                    storedData.Homes.Add(player.userID, new Dictionary<string, GamePos>());

                if (!storedData.Homes[player.userID].ContainsKey(args[1]))
                {
                    SendReply(player, GetMessage("HomeDoesntExist").Replace("{0}", args[1]));
                    return;
                }

                Vector3 pos = storedData.Homes[player.userID][args[1]].ToVector();

                object canTP = AllowedToTeleport(player, pos);
                if (canTP is string)
                {
                    SendReply(player, canTP.ToString());
                    ShowHomesUI(player);
                    return;
                }

                if (GetConfig<bool>("AdminInstaTP") && player.IsAdmin)
                {
                    ShowHomesUI(player);
                    SendReply(player, GetMessage("TeleportedTo").Replace("{0}", args[1]));
                    Teleport(player, pos);
                    return;
                }
                else
                {
                    ShowHomesUI(player);
                    HomeTeleporter ht = player.gameObject.AddComponent<HomeTeleporter>();
                    ht.Pos = storedData.Homes[player.userID][args[1]].ToVector();
                    ht.HomeName = args[1];
                    ht.Go();
                }
                return;
            }

            if (args[0] == "back")
            {
                TPBack(player);
                ShowHomesUI(player);
                return;
            }
        }

        [ConsoleCommand("clearcooldowns")]
        void ClearCooldownsCommand(ConsoleSystem.Arg arg)
        {
            if (!DebuggingMode) return;

            storedData.Cooldowns.Clear();
            SaveData();
            Puts("Cleared all cooldowns.");
        }

        #endregion

        #region External Hooks

        string CancelAllTeleports(BasePlayer player)
        {
            if (HasComponent<HomeTeleporter>(player)) return TPCancel(player);
            return null;
        }

        #endregion

        #region GUIs

        void ShowHomesUI(BasePlayer player)
        {
            if (!GUIOpen.ContainsKey(player))
                GUIOpen.Add(player, false);

            if (!GUIOpen[player])
            {
                ShowUI(player);
                GUIOpen[player] = true;
                return;
            }

            if (GUIOpen[player])
            {
                HideUI(player);
                GUIOpen[player] = false;
                return;
            }
        }

        void ShowUI(BasePlayer player)
        {
            HideUI(player);

            var GUIElement = new CuiElementContainer();

            var wholePanel = GUIElement.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = "0.3 0.3",
                    AnchorMax = "0.7 0.75"
                },
                Image =
                {
                    Color = "0 0 0 0.75"
                },
                CursorEnabled = true
            }, "Hud", "homesGUI");

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
            }, wholePanel);

            #region Title

            GUIElement.Add(new CuiLabel
            {
                Text =
                {
                    Text = lang.GetMessage("HomesTitle", this),
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

            #region Back Button

            if (HasPerm(player, permBack))
            {
                string backCommand = HasLastHome(player) ? "homegui back" : "";
                string backColour = HasLastHome(player) ? "0.15 0.15 1 1" : "0.5 0.5 0.5 1";

                GUIElement.Add(new CuiButton
                {
                    RectTransform =
                {
                    AnchorMin = "0.834 0",
                    AnchorMax = "0.934 0.97"
                },
                    Text =
                {
                    Text = "Назад",
                    Color = "1 1 1 1",
                    Align = TextAnchor.MiddleCenter
                },
                    Button =
                {
                    Command = backCommand,
                    Color = backColour
                }
                }, titleBar);
            }

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
                    Command = "homegui close",
                    Color = "1 0 0 1"
                },
                Text =
                {
                    Text = "X",
                    FontSize = 16,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, titleBar);

            #endregion

            #endregion

            #region Homes List

            var homesList = GUIElement.Add(new CuiPanel
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
            }, wholePanel);

            const float columnWidth = 0.2f;
            const float rowWidth = 0.2f;

            int homeCount = 0;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (storedData.Homes.ContainsKey(player.userID) && storedData.Homes[player.userID].Count() <= homeCount) continue;

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
                    }, homesList);

                    string homeName = "";
                    if (storedData.Homes.ContainsKey(player.userID))
                    {
                        if (storedData.Homes[player.userID].Count() > homeCount)
                        {
                            var items = from pair in storedData.Homes[player.userID]
                                        orderby pair.Key ascending
                                        select pair;
                            homeName = items.ToArray()[homeCount].Key;
                        }
                    }
                    GUIElement.Add(new CuiButton
                    {
                        RectTransform =
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "1 1"
                                },
                        Text =
                                {
                                    Text = homeName,
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 18,
                                    Color = "1 1 1 1",
                                    Font = "robotocondensed-regular.ttf"
                                },
                        Button =
                                {
                                    Command = $"homegui to {homeName}",
                                    Color = "0 0 0 0"
                                }
                    }, panel);
                    homeCount++;
                }
            }

            #endregion

            #region Empty List

            int count = 0;
            if (storedData.Homes.ContainsKey(player.userID))
                count = storedData.Homes[player.userID].Count();
            if (count == 0)
            {
                GUIElement.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = lang.GetMessage("NoHomesSet", this),
                        FontSize = 20,
                        Align = TextAnchor.MiddleCenter
                    },
                    RectTransform =
                    {
                        AnchorMin = "0 0.1",
                        AnchorMax = "1 1"
                    }
                }, homesList);
            }

            #endregion

            CuiHelper.AddUi(player, GUIElement);
        }

        void HideUI(BasePlayer player) => CuiHelper.DestroyUi(player, "homesGUI");

        #endregion

        #region Custom Functions

        void TPBack(BasePlayer player)
        {
            if (!HomeBack.ContainsKey(player))
            {
                SendReply(player, GetMessage("NoPreviousHomes"));
                return;
            }

            Teleport(player, HomeBack[player]);
            SendReply(player, GetMessage("TeleportedBack"));
        }

        string TPCancel(BasePlayer player)
        {
            if (!HasComponent<HomeTeleporter>(player))
                return GetMessage("NoTeleportsToCancel");

            var ht = player.GetComponent<HomeTeleporter>();
            ht.CancelTeleport();
            return GetMessage("TeleportCancelled");
        }

        void RecordHomeBack(BasePlayer player)
        {
            if (HomeBack.ContainsKey(player))
                HomeBack.Remove(player);
            HomeBack.Add(player, player.transform.position);
        }

        bool CanSetHome(BasePlayer player)
        {
            if (!CheckMaxHomes(player))
            {
                SendReply(player, GetMessage("HomeLimitedReached"));
                return false;
            }

            if (!GetConfig<bool>("AllowSetHomeInBuildBlocked") && !player.CanBuild())
            {
                SendReply(player, GetMessage("HomeInBuildBlock"));
                return false;
            }

            if (GetConfig<bool>("MustSetHomeOnBuilding"))
            {
                if (!GetConfig<bool>("CanSetHomeOnFloor"))
                {
                    if (!CheckFoundation(player.transform.position))
                    {
                        SendReply(player, GetMessage("MustBeOnFoundation"));
                        return false;
                    }
                }
                else
                {
                    if (!CheckFoundation(player.transform.position) && !CheckFloor(player.transform.position))
                    {
                        SendReply(player, GetMessage("MustBeOnFoundationOrFloor"));
                        return false;
                    }
                }
            }

            var zmgrCall = ZoneManager?.CallHook("EntityHasFlag", player, "notp");
            if (zmgrCall != null && zmgrCall is bool && (bool)zmgrCall)
            {
                SendReply(player, GetMessage("NoSetHomeInZones"));
                return false;
            }

            return true;
        }

        bool FindBuildBlock(Vector3 pos, string BlockName)
        {
            pos += new Vector3(0, 1f, 0);
            RaycastHit[] hits = Physics.RaycastAll(new Ray(pos, Vector3.down), 2f);
            if (hits.Count() == 0) return false;
            foreach (var hit in hits)
            {
                var buildBlockName = hit.GetEntity()?.GetComponent<BuildingBlock>()?.ShortPrefabName;
                if (buildBlockName != null && buildBlockName == BlockName) return true;
            }
            return false;
        }

        bool CheckMaxHomes(BasePlayer player)
        {
            if (!storedData.Homes.ContainsKey(player.userID))
                storedData.Homes.Add(player.userID, new Dictionary<string, GamePos>());

            int maxHomes = GetConfig<int>("DefaultMaxHomes");
            foreach (var kvp in GetConfig<Dictionary<string, object>>("MaxHomes"))
            {
                if (permission.UserHasPermission(player.UserIDString, kvp.Key))
                {
                    int homes = Int32.Parse(kvp.Value.ToString());
                    if (homes == 0) return true;
                    if (homes > maxHomes) maxHomes = homes;
                }
            }

            int homesCount = storedData.Homes[player.userID].Count();
            return (homesCount < maxHomes);
        }

        bool CheckHighestUses(BasePlayer player, out string uses)
        {
            int highestUses = GetConfig<int>("DefaultMaxUses");
            uses = highestUses.ToString();
            if (highestUses == 0) return false;

            foreach (var kvp in GetConfig<Dictionary<string, object>>("MaxUses").Where(x => permission.UserHasPermission(player.UserIDString, x.Key)))
            {
                int permUses;
                if (Int32.TryParse(kvp.Value.ToString(), out permUses))
                {
                    if (permUses == 0)
                    {
                        highestUses = 0;
                        return false;
                    }

                    if (permUses > highestUses)
                    {
                        highestUses = permUses;
                    }
                }
            }

            uses = highestUses.ToString();
            return (highestUses > 0 && storedData.Uses.ContainsKey(player.userID) && storedData.Uses[player.userID] > highestUses);
        }

        object AllowedToTeleport(BasePlayer player, Vector3 homePos)

        {
            if (storedData.Cooldowns.ContainsKey(player.userID))
                return GetMessage("OnCooldown").Replace("{0}", storedData.Cooldowns[player.userID].ToString());

            if (!GetConfig<bool>("AllowTeleportFromBuildBlock"))
                if (!player.CanBuild())
                    return GetMessage("TeleportWhilstBuildBlock");

            if (!GetConfig<bool>("AllowTeleportToBuildBlock"))
                if (IsBuildingBlocked(player, homePos))
                    return GetMessage("TeleportIntoBuildBlock");

            if (!GetConfig<bool>("AllowTeleportWhilstBleeding"))
                if (player.metabolism.bleeding.value > 0f)
                    return GetMessage("TeleportWhilstBleeding");

            if (GetConfig<bool>("BlockTPCrafting"))
                if (IsCrafting(player))
                    return GetMessage("TeleportWhilstCrafting");

            if (GetConfig<bool>("UseEconomicsPlugin") && EconomicsInstalled())
            {
                if (!CanAffordEconomics(player))
                    return GetMessage("CantAffordEconomics").Replace("{0}", GetConfig<double>("EconomicsPrice").ToString());
                SendReply(player, GetMessage("EconomicsYouSpent").Replace("{0}", GetConfig<double>("EconomicsPrice").ToString()));
            }

            if (GetConfig<bool>("UseServerRewardsPlugin") && ServerRewardsInstalled())
            {
                if (!CanAffordServerRewards(player))
                    return GetMessage("CantAffordServerRewards").Replace("{0}", GetConfig<double>("ServerRewardsPrice").ToString());
                SendReply(player, GetMessage("ServerRewardsYouSpent").Replace("{0}", GetConfig<double>("ServerRewardsPrice").ToString()));
            }

            string uses;
            if (CheckHighestUses(player, out uses))
                return GetMessage("MaxUsesReached").Replace("{0}", uses);

            var call = Interface.Oxide.CallHook("CanTeleport", player);
            if (call != null) return call.ToString();

            if (GetConfig<bool>("MustSetHomeOnBuilding"))
            {
                if (GetConfig<bool>("CanSetHomeOnFloor"))
                {
                    if (!CheckFloor(homePos) && !CheckFoundation(homePos))
                        return GetMessage("HomeBuildBlockDestroyed");
                }
                else if (!CheckFoundation(homePos))
                    return GetMessage("HomeBuildBlockDestroyed");
            }

            if (HasComponent<HomeTeleporter>(player)) return GetMessage("AlreadyTeleporting");

            return true;
        }

        void AssignCooldown(BasePlayer player)
        {
            int cooldown = GetConfig<int>("DefaultCooldown");
            foreach (var kvp in GetConfig<Dictionary<string, object>>("Cooldowns"))
            {
                if (permission.UserHasPermission(player.UserIDString, kvp.Key))
                {
                    int cd = Int32.Parse(kvp.Value.ToString());
                    if (cd < cooldown) cooldown = cd;
                }
            }
            if (storedData.Cooldowns.ContainsKey(player.userID))
                storedData.Cooldowns[player.userID] = cooldown;
            else storedData.Cooldowns.Add(player.userID, cooldown);
            SaveData();
        }

        void RecordUse(BasePlayer player)
        {
            if (!storedData.Uses.ContainsKey(player.userID))
                storedData.Uses.Add(player.userID, 0);
            storedData.Uses[player.userID]++;
        }

        bool IsBuildingBlocked(BasePlayer player, Vector3 pos)
        {
            var colliders = Pool.GetList<Collider>();
            Vis.Colliders(pos, 0.1f, colliders, LayerMask.GetMask("Trigger"));
            var cupboard = colliders.Select(x => x.GetComponentInParent<BuildingPrivlidge>()).Where(x => x != null).FirstOrDefault();
            Pool.FreeList(ref colliders);
            if (cupboard == null) return false;
            return player.userID != cupboard.OwnerID && !cupboard.IsAuthed(player);
        }

        #region Server Rewards/Economics

        bool EconomicsInstalled() => Economics != null;

        bool ServerRewardsInstalled() => ServerRewards != null;

        bool CanAffordEconomics(BasePlayer player)
        {
            double price = GetConfig<double>("EconomicsPrice");
            double playerMoney = (double)Economics.Call("GetPlayerMoney", player.userID);

            if (playerMoney - price >= 0)
            {
                Economics?.Call("Set", player.userID, playerMoney - price);
                return true;
            }
            return false;
        }

        bool CanAffordServerRewards(BasePlayer player)
        {
            int price = GetConfig<int>("ServerRewardsPrice");
            int currentPoints;
            var call = ServerRewards?.Call("CheckPoints", player.userID);
            if (call == null) currentPoints = 0;
            else currentPoints = (int)call;

            if (currentPoints - price >= 0)
            {
                ServerRewards.Call("TakePoints", player.userID, price);
                return true;
            }
            return false;
        }

        void RefundPlayerEconomics(BasePlayer player)
        {
            double price = GetConfig<double>("EconomicsPrice");
            double playerMoney = (double)Economics.Call("GetPlayerMoney", player.userID);
            Economics?.Call("Set", player.userID, playerMoney + price);
            SendReply(player, GetMessage("EconomicsRefunded").Replace("{0}", price.ToString()));
        }

        void RefundServerRewards(BasePlayer player)
        {
            int price = GetConfig<int>("ServerRewardsPrice");
            ServerRewards.Call("AddPoints", player.userID, price);
            SendReply(player, GetMessage("ServerRewardsRefunded").Replace("{0}", price.ToString()));
        }

        #endregion

        void Teleport(BasePlayer player, Vector3 pos)
        {
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading");
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

        bool HasLastHome(BasePlayer player) => HomeBack.ContainsKey(player);

        bool CheckFoundation(Vector3 homePos) => FindBuildBlock(homePos, "foundation") || FindBuildBlock(homePos, "foundation.triangle");
        bool CheckFloor(Vector3 homePos) => FindBuildBlock(homePos, "floor") || FindBuildBlock(homePos, "floor.triangle");

        bool HasPerm(BasePlayer player) => (permission.UserHasPermission(player.UserIDString, permUse) || player.IsAdmin);
        bool HasPerm(BasePlayer player, string perm) => (permission.UserHasPermission(player.UserIDString, perm) || player.IsAdmin);

        bool IsCrafting(BasePlayer player) => player.inventory.crafting.queue.Count() > 0;

        T GetConfig<T>(string key)
        {
            if (Config[key] == null)
            {
                Debug.LogError("[HomesGUI] Tried to grab something from the config that doesn't exist - please delete your config and allow it to regenerate.");
                return default(T);
            }
            return (T)Convert.ChangeType(Config[key], typeof(T));
        }

        string GetMessage(string key)
        {
            string message = "";
            if (GetConfig<bool>("PrefixEnabled"))
                message += GetConfig<string>("PrefixText");
            message += lang.GetMessage(key, this);
            return message;
        }

        bool HasComponent<T>(BasePlayer player) => (player.GetComponent<T>() != null);

        void SaveData() { Interface.Oxide.DataFileSystem?.WriteObject<StoredData>(this.Title, storedData); }
        void ReadData() { storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(this.Title); }

        #endregion
    }
}