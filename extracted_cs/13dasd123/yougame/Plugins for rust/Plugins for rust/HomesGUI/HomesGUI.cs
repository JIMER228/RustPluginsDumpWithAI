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
    [Info("HomesGUI", "PsychoTea", "1.1.4")]

    class HomesGUI : RustPlugin
    {
        static HomesGUI Instance;
        const string permUse = "homesgui.use";
        const string permBack = "homesgui.back";
        Dictionary<BasePlayer, bool> guiOpen = new Dictionary<BasePlayer, bool>();
        GameObject cmObject;
        bool debuggingMode = false;
        Dictionary<BasePlayer, Vector3> homeBack = new Dictionary<BasePlayer, Vector3>();

        [PluginReference]
        Plugin Economics;
        [PluginReference]
        Plugin ServerRewards;

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

                TimeUntilTeleport = Instance.GetConfig<int>("Стандартная длительность задержки перед телепортацией (в секундах)");
                foreach (var kvp in Instance.GetConfig<Dictionary<string, object>>("ДлительностьЗадержкиПередТелепортациейВСекундах"))
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
                if (Instance.EconomicsInstalled() && Instance.GetConfig<bool>("Использовать плагин Economics"))
                    Instance.RefundPlayerEconomics(Player);
                if (Instance.ServerRewardsInstalled() && Instance.GetConfig<bool>("Использовать плагин ServerRewards"))
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
        }
        StoredData storedData;

        #endregion

        #region Oxide Hooks

        void Init()
        {
            debuggingMode = (ConVar.Server.hostname == "PsychoTea's Testing Server");

            //Register permissions
            permission.RegisterPermission(permUse, this);
            permission.RegisterPermission(permBack, this);
            foreach (string perm in GetConfig<Dictionary<string, object>>("ДлительностьПерезарядкиТелепортаВСекундах").Keys)
                if (!permission.PermissionExists(perm))
                    permission.RegisterPermission(perm, this);
            foreach (string perm in GetConfig<Dictionary<string, object>>("ОграничениеНаКоличествоСохранённыхМестоположенийДомов").Keys)
                if (!permission.PermissionExists(perm))
                    permission.RegisterPermission(perm, this);
            foreach (string perm in GetConfig<Dictionary<string, object>>("ДлительностьЗадержкиПередТелепортациейВСекундах").Keys)
                if (!permission.PermissionExists(perm))
                    permission.RegisterPermission(perm, this);

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                { "NoPermission", "<color=#F79F81>У вас нет доступа к этой команде</color>" },
                { "HomesTitle", "Дом" },
                { "SetHome-Usage", "Используйте <color=#BEF781>/sethome <название дома></color> чтобы сохранить местоположение дома" },
                { "HomeAlreadyExists", "Вы не можете сохранить местоположение дома\n<color=#F5DA81>ПРИЧИНА:</color> местоположение дома с названием <color=#BEF781>{0}</color> уже существует" },
                { "HomeCreated", "Местоположение дома с названием <color=#BEF781>{0}</color> сохранено" },
                { "DelHome-Usage", "Используйте <color=#BEF781>/removehome <название дома></color> чтобы удалить местоположение дома" },
                { "HomeDoesntExist", "Местоположение дома с названием <color=#BEF781>{0}</color> не найдено" },
                { "HomeDeleted", "Местоположение дома с названием <color=#BEF781>{0}</color> удалёно" },
                { "HomeInBuildBlock", "Вы не можете сохранить местоположение дома\n<color=#F5DA81>ПРИЧИНА:</color> строительство запрещено" },
                { "NoHomesSet", "Список ваших домов пуст" },
                { "HomesList", "<color=#F5DA81>СПИСОК ДОМОВ:</color>{0}" },
                { "TeleportingTo", "Вы телепортируетесь в дом с названием <color=#BEF781>{0}</color> через <color=#BEF781>{1} сек.</color>" },
                { "TeleportedTo", "Вы телепортировались в дом с названием <color=#BEF781>{0}</color>" },
                { "HomeLimitedReached", "Вы не можете сохранить местоположение дома\n<color=#F5DA81>ПРИЧИНА:</color> вы не можете сохранить больше <color=#BEF781>{maxHomes}</color> домов" },
                { "TeleportWhilstBuildBlock", "Вы не можете использовать телепорт в зоне действия чужого шкафа." },
                { "TeleportWhilstBleeding", "Телепортация домой прервана\n<color=#F5DA81>ПРИЧИНА:</color> вы получили урон" },
                { "TeleportWhilstCrafting", "Телепортация домой прервана\n<color=#F5DA81>ПРИЧИНА:</color> вы не закончили крафт предметов" },
                { "OnCooldown", "Вы не можете отправить запрос на телепортацию\n<color=#F5DA81>ПРИЧИНА:</color> телепорт перезаряжается, подождите <color=#BEF781>{0}</color>" },
                { "YouTookDamage", "Телепортация домой прервана\n<color=#F5DA81>ПРИЧИНА:</color> вы получили урон" },
                { "CantAffordEconomics", "Вы не можете себе это позволить! Цена: <color=#BEF781>{0}₽</color>" },
                { "EconomicsYouSpent", "Вы потратили <color=#BEF781>{0}₽</color> на телепорт домой." },
                { "EconomicsRefunded", "Вам было возвращено <color=#BEF781>{0}₽</color>" },
                { "CantAffordServerRewards", "Вы не можете себе это позволить! Цена: <color=#BEF781>{0}RP</color>" },
                { "ServerRewardsYouSpent", "Вы потратили <color=#BEF781>{0}RP</color> на телепорт домой." },
                { "ServerRewardsRefunded", "Вам было возвращено <color=#BEF781>{0}RP</color>" },
                { "MustBeOnFoundation", "Вы не можете сохранить местоположение дома\n<color=#F5DA81>ПРИЧИНА:</color> сохранить местоположение можно только на фундаменте" },
                { "MustBeOnFoundationOrFloor", "Вы не можете сохранить местоположение дома\n<color=#F5DA81>ПРИЧИНА:</color> сохранить местоположение можно только на фундаменте или на полу" },
                { "HomeBuildBlockDestroyed", "Фундамент или пол, где установлено местоположение дома, разрушен." },
                { "NoPreviousHomes", "У вас нет предыдущих телепортаций, чтобы вернуться обратно." },
                { "TeleportedBack", "Вы телепортированы на предыдущее местоположение." },
                { "AlreadyTeleporting", "Вы уже телепортируетесь." },
                { "NoTeleportsToCancel", "Вы не можете отменить запрос на телепортацию\n<color=#F5DA81>ПРИЧИНА:</color> нет активных запросов" },
                { "TeleportCancelled", "Вы отклонили запрос на телепортацию." },
                { "TeleportIntoBuildBlock", "Вы не можете отправить запрос на телепортацию\n<color=#F5DA81>ПРИЧИНА:</color> телепортация в зону действия чужого шкафа запрещена" }
            }, this, "en");

            //Register command alises
            foreach (string cmdAlias in GetConfig<List<object>>("Команды альянса"))
                cmd.AddChatCommand(cmdAlias, this, "homeCommand");

            ReadData();

            if (debuggingMode) BasePlayer.activePlayerList.ForEach(x => ShowUI(x));
        }

        void OnServerInitialized()
        {
            Instance = this;

            ReadData();

            cmObject = new GameObject();
            cmObject.AddComponent<CooldownManager>();

            if (Economics == null && GetConfig<bool>("Использовать плагин Economics"))
            {
                Debug.LogError("[TeleportGUI] Error! Economics is enabled in the config but is not installed! Please install Economics or disable 'Использовать плагин Economics' in the config!");
            }

            if (ServerRewards == null && GetConfig<bool>("Использовать плагин Economics"))
            {
                Debug.LogError("[TeleportGUI] Error! ServerRewards is enabled in the config but is not installed! Please install ServerRewards or disable 'Использовать плагин Economics' in the config!");
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (GetConfig<bool>("Использовать бинд"))
                player.SendConsoleCommand($"bind {GetConfig<string>("Кнопка бинда")} homegui");
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!GetConfig<bool>("Отменять телепортацию если игрок получил урон")) return;
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

            GameObject.Destroy(cmObject);
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");

            Config["Включить префикс"] = true;
            Config["Префикс"] = "<color=orange>ДОМ: </color>";
            Config["Стандартная длительность задержки перед телепортацией (в секундах)"] = 15;
            Config["ДлительностьЗадержкиПередТелепортациейВСекундах"] = new Dictionary<string, int>()
            {
                { "homesgui.vip", 10 },
                { "homesgui.elite", 5 },
                { "homesgui.god", 3 },
                { "homesgui.none", 0 }
            };
            Config["Команды альянса"] = new List<string>() { };
            Config["Стандартная длительность перезарядки телепорта (в секундах)"] = 180;
            Config["ДлительностьПерезарядкиТелепортаВСекундах"] = new Dictionary<string, int>()
            {
                { "homesgui.vip", 60 },
                { "homesgui.elite", 30 },
                { "homesgui.god", 15 },
                { "homesgui.none", 0 }
            };
            Config["Стандартные ограничения на количество сохранённых местоположений"] = 3;
            Config["ОграничениеНаКоличествоСохранённыхМестоположенийДомов"] = new Dictionary<string, int>()
            {
                { "homesgui.vip", 4 },
                { "homesgui.elite", 6 },
                { "homesgui.god", 10 },
                { "homesgui.unlimited", 0 }
            };
            Config["Использовать бинд"] = true;
            Config["Кнопка бинда"] = "h";
            Config["Разрешить телепортацию во время кровотечения"] = false;
            Config["Разрешить отправлять запрос на телепортацию из зоны действия чужого шкафа"] = false;
            Config["Разрешить отправлять запрос на телепортацию в зону действия чужого шкафа"] = false;
            Config["Использовать плагин Economics"] = false;
            Config["Цена для Economics"] = 100;
            Config["Использовать плагин ServerRewards"] = false;
            Config["Цена для ServerRewards"] = 10;
            Config["Запретить телепорт во время крафта"] = true;
            Config["Мгновненный телепорт для админа"] = false;
            Config["Разрешить устанавливать точку дома в зоне действия чужого шкафа"] = false;
            Config["Точка дома должна быть на постройки"] = true;
            Config["Установка точек дома на этажах"] = false;
            Config["Проверка построек с установленными точками дома"] = true;
            Config["Отменять телепортацию если игрок получил урон"] = true;
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

            if (!CheckMaxHomes(player))
            {
                SendReply(player, GetMessage("HomeLimitedReached"));
                return;
            }

            if (!GetConfig<bool>("Разрешить устанавливать точку дома в зоне действия чужого шкафа") && !player.CanBuild())
            {
                SendReply(player, GetMessage("HomeInBuildBlock"));
                return;
            }

            if (GetConfig<bool>("Точка дома должна быть на постройки"))
            {
                if (!GetConfig<bool>("Установка точек дома на этажах"))
                {
                    if (!CheckFoundation(player.transform.position))
                    {
                        SendReply(player, GetMessage("MustBeOnFoundation"));
                        return;
                    }
                }
                else
                {
                    if (!CheckFoundation(player.transform.position) && !CheckFloor(player.transform.position))
                    {
                        SendReply(player, GetMessage("MustBeOnFoundationOrFloor"));
                        return;
                    }
                }
            }

            storedData.Homes[player.userID].Add(args[0], (GamePos)player.transform.position);
            SaveData();
            SendReply(player, GetMessage("HomeCreated").Replace("{0}", args[0]));
        }

        [ChatCommand("removehome")]
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

        [ChatCommand("homelist")]
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

            if (args.Length == 0) return;

            if (args[0] == "True")
            {
                ShowHomesUI(player);
                return;
            }

            if (args[0] == "close")
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

                if (GetConfig<bool>("Мгновненный телепорт для админа") && player.IsAdmin)
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
            if (!debuggingMode) return;

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
            if (!guiOpen.ContainsKey(player))
                guiOpen.Add(player, false);

            if (!guiOpen[player])
            {
                ShowUI(player);
                guiOpen[player] = true;
                return;
            }

            if (guiOpen[player])
            {
                HideUI(player);
                guiOpen[player] = false;
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
                    FontSize = 20,
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
            if (!homeBack.ContainsKey(player))
            {
                SendReply(player, GetMessage("NoPreviousHomes"));
                return;
            }

            Teleport(player, homeBack[player]);
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
            if (homeBack.ContainsKey(player))
                homeBack.Remove(player);
            homeBack.Add(player, player.transform.position);
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

            int maxHomes = GetConfig<int>("Стандартные ограничения на количество сохранённых местоположений");
            foreach (var kvp in GetConfig<Dictionary<string, object>>("ОграничениеНаКоличествоСохранённыхМестоположенийДомов"))
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

        object AllowedToTeleport(BasePlayer player, Vector3 homePos)
        {
            if (storedData.Cooldowns.ContainsKey(player.userID))
                return GetMessage("OnCooldown").Replace("{0}", storedData.Cooldowns[player.userID].ToString());

            if (!GetConfig<bool>("Разрешить отправлять запрос на телепортацию из зоны действия чужого шкафа"))
                if (!player.CanBuild())
                    return GetMessage("TeleportWhilstBuildBlock");

            if (!GetConfig<bool>("Разрешить отправлять запрос на телепортацию в зону действия чужого шкафа"))
                if (IsBuildingBlocked(player, homePos))
                    return GetMessage("TeleportIntoBuildBlock");

            if (!GetConfig<bool>("Разрешить телепортацию во время кровотечения"))
                if (player.metabolism.bleeding.value > 0f)
                    return GetMessage("TeleportWhilstBleeding");

            if (GetConfig<bool>("Запретить телепорт во время крафта"))
                if (IsCrafting(player))
                    return GetMessage("TeleportWhilstCrafting");

            if (GetConfig<bool>("Использовать плагин Economics") && EconomicsInstalled())
            {
                if (!CanAffordEconomics(player))
                    return GetMessage("CantAffordEconomics").Replace("{0}", GetConfig<double>("Цена для Economics").ToString());
                SendReply(player, GetMessage("EconomicsYouSpent").Replace("{0}", GetConfig<double>("Цена для Economics").ToString()));
            }

            if (GetConfig<bool>("Использовать плагин ServerRewards") && ServerRewardsInstalled())
            {
                if (!CanAffordServerRewards(player))
                    return GetMessage("CantAffordServerRewards").Replace("{0}", GetConfig<double>("Цена для ServerRewards").ToString());
                SendReply(player, GetMessage("ServerRewardsYouSpent").Replace("{0}", GetConfig<double>("Цена для ServerRewards").ToString()));
            }

            var call = Interface.Oxide.CallHook("CanTeleport", player);
            if (call != null) return call.ToString();

            if (GetConfig<bool>("Точка дома должна быть на постройки"))
            {
                if (GetConfig<bool>("Установка точек дома на этажах"))
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
            int cooldown = GetConfig<int>("Стандартная длительность перезарядки телепорта (в секундах)");
            foreach (var kvp in GetConfig<Dictionary<string, object>>("ДлительностьПерезарядкиТелепортаВСекундах"))
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
            double price = GetConfig<double>("Цена для Economics");
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
            int price = GetConfig<int>("Цена для ServerRewards");
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
            double price = GetConfig<double>("Цена для Economics");
            double playerMoney = (double)Economics.Call("GetPlayerMoney", player.userID);
            Economics?.Call("Set", player.userID, playerMoney + price);
            SendReply(player, GetMessage("EconomicsRefunded").Replace("{0}", price.ToString()));
        }

        void RefundServerRewards(BasePlayer player)
        {
            int price = GetConfig<int>("Цена для ServerRewards");
            ServerRewards.Call("AddPoints", player.userID, price);
            SendReply(player, GetMessage("ServerRewardsRefunded").Replace("{0}", price.ToString()));
        }

        #endregion

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

        bool HasLastHome(BasePlayer player) => homeBack.ContainsKey(player);

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
            if (GetConfig<bool>("Включить префикс"))
                message += GetConfig<string>("Префикс");
            message += lang.GetMessage(key, this);
            return message;
        }

        bool HasComponent<T>(BasePlayer player) => (player.GetComponent<T>() != null);

        void SaveData() { Interface.Oxide.DataFileSystem?.WriteObject<StoredData>(this.Title, storedData); }
        void ReadData() { storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(this.Title); }

        #endregion
    }
}