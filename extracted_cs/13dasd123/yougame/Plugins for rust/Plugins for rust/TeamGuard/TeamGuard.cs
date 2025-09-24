// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Rust;
using UnityEngine;
using System.Globalization;
using Oxide.Core;
using System.IO;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("TeamGuard", "Enigma", "1.1.2")]
    [Description("Plugin allow admins to easyer controll player groups size.")]
    class TeamGuard : RustPlugin
    {
        #region Config Setup
        private static int MaxAllowedPlayers = 6;
        private static float AroundRadius = 10f;
        private static float CheckInterval = 5f;
        private static float timeBeforeShock = 20f;
        private static float DamagePerTime = 5f;
        private static string perm = "teamguard.log";
        private static string Prefix = "<color=#f46600>[TeamGuard]</color>";
        private static string Amin = "0 0.355";
        private static string Amax = "1 0.655";
        private static string BackGroundColor = "0.30 0.01 0.01 0.80";
        private static int FontSize = 16;
        private static bool UseGui = true;
        private static bool UseChat = false;
        private static bool IgnoreAdmins = false;
        private static int AuthLevel = 2;
        #endregion

        #region Vars
		
        private static readonly int playerLayer = LayerMask.GetMask("Player (Server)");
        private static readonly Collider[] colBuffer = (Collider[])typeof(Vis).GetField("colBuffer", (BindingFlags.Static | BindingFlags.NonPublic))?.GetValue(null);
        //"assets/prefabs/locks/keypad/effects/lock.code.shock.prefab"
        private static string EffectPrefab1 = "assets/prefabs/npc/autoturret/effects/targetacquired.prefab";
        private static string EffectPrefab2 = "assets/prefabs/misc/junkpile/effects/despawn.prefab";

        #endregion

        #region Localization
        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TextBeforeDmg"] = "You have more players around then allowed (Allowed - {0}).\nYou have {1} seconds before you will start to get damage.",
                ["TextWhileDmg"] = "You have more players around then allowed (Allowed - {0}).\nAll players in the area would get {1} damage every {2} seconds until redundant player will leave the area.",
                ["LogCode"] = "Player {0} attempted to auth in the code lock of player {1}. {2}",
                ["ChatCode"] = "{0} This codelock has it's auth limit. (Max - {1})",
                ["LogCup"] = "Player {0} attempted to auth in the tool cupboard of player {1}. {2}",
                ["ChatCup"] = "{0} This tool cupboard has it's auth limit. (Max - {1})",
                ["LogTurret"] = "Player {0} attempted to auth in the turret of player {1}. {2}",
                ["ChatTurret"] = "{0} This turret has it's auth limit. (Max - {1})"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["TextBeforeDmg"] = "В зоне вокруг вас находится больше игроков, чем разрешено (Разрешено {0}).\nЧерез {1}с. все игроки в зоне начнут получать урон.",
                ["TextWhileDmg"] = "В зоне вокруг вас находится больше игроков, чем разрешено (Разрешено {0}).\nВсем игрокам в зоне будет наносится урон в размере {1} каждые {2}с. до тех пор, пока лишние игроки не покинут зону проверки.",
                ["LogCode"] = "Игрок {0} попытался авторизоваться в замке игрока {1}. {2}",
                ["ChatCode"] = "{0} Достигнут лимит авторизации в замке. (Максимально - {1})",
                ["LogCup"] = "Игрок {0} попытался авторизоваться в шкаф игрока {1}. {2}",
                ["ChatCup"] = "{0} Достигнут лимит авторизованных в шкафу. (Максимально - {1})",
                ["LogTurret"] = "Игрок {0} попытался авторизоваться в турели игрока {1}. {2}",
                ["ChatTurret"] = "{0} Достигнут лимит авторизаций в турели. (Максимально - {1})"
            }, this, "ru");
        }
        #endregion

        #region Initialization
        private void LoadConfigValues()
        {
            GetConfig("Общие Настройки","Максимальный размер группы игроков", ref MaxAllowedPlayers);
            GetConfig("Общие Настройки", "Радиус зоны проверки", ref AroundRadius);
            GetConfig("Общие Настройки", "Частота проверок", ref CheckInterval);
            GetConfig("Общие Настройки", "Разрешённое время нахождения рядом", ref timeBeforeShock);
            GetConfig("Общие Настройки", "Наносимый урон за раз", ref DamagePerTime);
            GetConfig("Общие Настройки", "Привилегия для просмотра сообщений в чате", ref perm);
            GetConfig("Общие Настройки", "Префикс в чате", ref Prefix);
            GetConfig("Настройки GUI", "Минимальный отступ", ref Amin);
            GetConfig("Настройки GUI", "Максимальный отступ", ref Amax);
            GetConfig("Настройки GUI", "Цвет фона", ref BackGroundColor);
            GetConfig("Настройки GUI", "Размер шрифта", ref FontSize);
            GetConfig("Настройки GUI", "Использовать ли графическую панель?", ref UseGui);
            GetConfig("Настройки GUI", "Выводить ли сообщения о нанесении урона в чат?", ref UseChat);
            GetConfig("Проверка администраторов", "Игнорировать администраторов при проверке?", ref IgnoreAdmins);
            GetConfig("Проверка администраторов", "Необходимый уровень AuthLevel для игнорирования", ref AuthLevel);
            SaveConfig();
        }
        void Loaded()
        {
            LoadConfigValues();
            LoadMessages();
            timer.Once(2f, () =>
             {
                 foreach (BasePlayer player in BasePlayer.activePlayerList)
                 {
                     CheckComponent(player);
                 }
             }); 
            permission.RegisterPermission(perm, this);
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating config file...");
            LoadConfigValues();
        }
        void Unload()
        {
            foreach (AroundTimer arTimer in Resources.FindObjectsOfTypeAll<AroundTimer>())
            {
                CuiHelper.DestroyUi(arTimer.player, "TeamGuardGUI");
                UnityEngine.Object.Destroy(arTimer);
            }
        }
        void OnPlayerInit(BasePlayer player)
        {
            CheckComponent(player);
        }
        #endregion

        #region Main Funcs
        class AroundTimer : MonoBehaviour
        {
            float ElaspedSeconds;
            public BasePlayer player;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
                InvokeRepeating("CheckAround", CheckInterval, CheckInterval);
            }

            void CheckAround()
            {
                if (!player.IsConnected)
                {
                    Destroy(this);
                    return;
                }

                if (player.IsDead() || player.IsSleeping()) return;
                
                int entities = Physics.OverlapSphereNonAlloc(player.transform.position, AroundRadius, colBuffer, playerLayer);

                int playersAround = 0;
                
                for (var i = 0; i < entities; i++)
                {
                    var player = colBuffer[i].GetComponentInParent<BasePlayer>();
                    if (player == null) continue;
                    var check = Interface.Oxide.CallHook("TeamGuardCheck", (object)this.player, (object)player);
                    if (check == null)
                    {
                        playersAround++;
                    }
                }

                if (playersAround >= MaxAllowedPlayers)
                {
                    ElaspedSeconds += CheckInterval;
                    if (ElaspedSeconds >= timeBeforeShock)
                    {
                        Interface.Oxide.CallHook("OnTeamGuard", (object)player, (object)ElaspedSeconds, (object)true);
                    }else
                    {
                        Interface.Oxide.CallHook("OnTeamGuard", (object)player, (object)ElaspedSeconds, (object)false);
                    }
                }
                else
                {
                    ElaspedSeconds = Mathf.Max(0, ElaspedSeconds - CheckInterval);
                    CuiHelper.DestroyUi(player, "TeamGuardGUI");
                }
            }
        }
        
        void OnTeamGuard(BasePlayer player, float ElaspedSeconds, bool TakingDmg)
        {
            string text = string.Empty;
            if (TakingDmg)
            {
                text = string.Format(GetMsg("TextWhileDmg", player.UserIDString), MaxAllowedPlayers, DamagePerTime, CheckInterval);
                if(UseChat)
                    player.ChatMessage(Prefix + " " + text);
                DoShock(player);
            }else
            {
                string timeleft = (timeBeforeShock - ElaspedSeconds).ToString();
                text = string.Format(GetMsg("TextBeforeDmg", player.UserIDString), MaxAllowedPlayers, timeleft);
            }
            if (UseGui)
                ShowGUI(player, Amin, Amax, BackGroundColor, FontSize, text);
        }
        private static void ShowGUI(BasePlayer player, string amin, string amax, string backgroundColor, int FontSize, string text)
        {
            CuiHelper.DestroyUi(player, "TeamGuardGUI");
            var elements = new CuiElementContainer();
            var panel = elements.Add(new CuiPanel
            {
                Image = { Color = backgroundColor },
                RectTransform = { AnchorMin = amin, AnchorMax = amax },
                CursorEnabled = false
            }, "Overlay", "TeamGuardGUI");
            elements.Add(new CuiLabel
            {
                Text = { Text = text, FontSize = FontSize, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, panel);
            CuiHelper.AddUi(player, elements);
        }
        void CheckComponent(BasePlayer player)
        {
            AroundTimer arTimer = player.GetComponent<AroundTimer>();

            if (!arTimer)
            {
                player.gameObject.AddComponent<AroundTimer>();
            }
        }
        static void DoShock(BasePlayer player)
        {
            player.Hurt(DamagePerTime, DamageType.ElectricShock, player, false);
            Effect.server.Run(EffectPrefab1, player, 0, Vector3.zero, Vector3.forward, null, false);
            Effect.server.Run(EffectPrefab2, player, 0, Vector3.zero, Vector3.forward, null, false);
        }
        static bool IsVisible(BasePlayer player, Vector3 source, Vector3 dest) => player.IsVisible(source, dest);
        #endregion

        #region Oxide Hooks
        object OnCodeEntered(CodeLock Lock, BasePlayer player, string code)
        {
            bool CanOpen = code == Lock.guestCode || code == Lock.code;
            if (CanOpen)
            {
                var parrent = Lock.GetParentEntity();
                string owner = BasePlayer.FindByID(parrent.OwnerID)?.displayName;
                if (owner == null)
                {
                    owner = parrent.OwnerID.ToString();
                }
                var whitelistPlayers = Lock.whitelistPlayers;
                var guestPlayers = Lock.guestPlayers;

                var count = guestPlayers.Count + whitelistPlayers.Count;
                if (count >= MaxAllowedPlayers)
                {
                    Log("LogCode", player.displayName, owner, parrent.ServerPosition.ToString());
                    player.ChatMessage(string.Format(GetMsg("ChatCode", player.UserIDString), Prefix, MaxAllowedPlayers));
                    return false;
                }
            }
            return null;
        }
        object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            string owner = BasePlayer.FindByID(privilege.OwnerID)?.displayName;
            if(owner == null)
            {
                owner = privilege.OwnerID.ToString();
            }
            var authlist = privilege.authorizedPlayers;
            if(authlist.Count >= MaxAllowedPlayers)
            {
                Log("LogCup", player.displayName, owner, privilege.ServerPosition.ToString());
                player.ChatMessage(string.Format(GetMsg("ChatCup", player.UserIDString), Prefix, MaxAllowedPlayers));
                return false;
            }
            return null;
        }
        object OnTurretAuthorize(AutoTurret turret, BasePlayer player)
        {
            string owner = BasePlayer.FindByID(turret.OwnerID)?.displayName;
            if (owner == null)
            {
                owner = turret.OwnerID.ToString();
            }
            var authlist = turret.authorizedPlayers;
            if(authlist.Count >= MaxAllowedPlayers)
            {
                Log("LogTurret", player.displayName, owner, turret.ServerPosition.ToString());
                player.ChatMessage(string.Format(GetMsg("ChatTurret", player.UserIDString), Prefix, MaxAllowedPlayers));
                return false;
            }
            return null;
        }

        #endregion

        #region Helpers
        private void GetConfig<T>(string Menu, string Key, ref T var)
        {
            if (Config[Menu,Key] != null)
            {
                var = (T)Convert.ChangeType(Config[Menu,Key], typeof(T));
            }
            Config[Menu,Key] = var;
        }
        private void Log(string LangLine, params string[] repl)
        {
            string logtext = $"({DateTime.Now.ToShortTimeString()}) {GetMsg(LangLine)}";
            for (int i = 0; i < repl.Length; i++)
            {
                logtext = logtext.Replace("{" + i.ToString() + "}", repl[i]);
            }
            LogToFile("Log", logtext, this);

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (permission.UserHasPermission(player.UserIDString, perm))
                {
                    string text = GetMsg(LangLine, player.UserIDString);
                    for(int i = 0; i < repl.Length; i++)
                    {
                        text = text.Replace("{" + i.ToString() + "}", repl[i]);
                    }
                    player.ChatMessage(Prefix + " " + text);
                }
            }
        }
        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());
        #endregion
    }
}
