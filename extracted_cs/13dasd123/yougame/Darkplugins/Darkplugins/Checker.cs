using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Plugins;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ /*ПЛАГИН БЫЛ СКАЧАН С ДИСКОРД СООБЩЕСТВА https://discord.gg/dNGbxafuJn */ [Info("Checker", "https://discord.gg/dNGbxafuJn", "1.0.2")]
    public class Checker : RustPlugin
    {
        private List<ulong> PlayerID = new List<ulong>();
        #region hooks

        void OnServerInitialized()
        {
            Broadcast();
            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
            string clockformat = "HH:mm";
            PermissionService.RegisterPermissions(this, permissions);
            LoadConfig();

            #region Broadcast

            timer.Every(_config.timerbroadcast, () =>
            {
                var clock = TOD_Sky.Instance.Cycle.DateTime.ToString(clockformat);
                var joining = SingletonComponent<ServerMgr>.Instance.connectionQueue.Joining;
                var Queque = SingletonComponent<ServerMgr>.Instance.connectionQueue.Queued;
                var TotalOnline = BasePlayer.activePlayerList.Count;
                foreach (BasePlayer p in BasePlayer.activePlayerList)
                {
                    SendReply(p, Messages["Broadcast"], TotalOnline, joining, Queque, clock);
                }

            });

            #endregion

        }

        void OnPlayerInit(BasePlayer player)
        {
            Playerid(player);
        }

        void Playerid(BasePlayer player)
        {
            if (!PlayerID.Contains(player.userID))
            {
                SendReply(player, _config.OnePlayer.Replace("{0}", ConVar.Server.hostname));
                foreach (var VARIABLE in BasePlayer.activePlayerList)
                {
                    SendReply(VARIABLE, _config.BroadcastNEW.Replace("{0}", player.displayName));
                    PlayerID.Add(player.userID);
                }
                Interface.Oxide.DataFileSystem.WriteObject("Checker/Players", PlayerID);
            }
        }
        
        private int lastIndex;
        void Broadcast() {
            foreach (var player in BasePlayer.activePlayerList)
            {
                SendReply(player, _config.BroadMessage[lastIndex]);
                player.ConsoleMessage(_config.BroadMessage[lastIndex]);
                Puts(_config.BroadMessage[lastIndex]);
            }
            lastIndex = (lastIndex + 1) % _config.BroadMessage.Count;
            timer.Once(_config.autoTimemsg, () => Broadcast());
        }
        
        

        private void PrintMessage(BasePlayer player, string msgId, params object[] args) // Chat 
        {
            PrintToChat(player, lang.GetMessage(msgId, this, player.UserIDString), args);
        }

        #endregion

        #region commands

        [ChatCommand("broadcast")]
        void broadcastcmd(BasePlayer player, string command)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, "Checker.admin.use"))
            {
                PrintMessage(player, Messages["NoPermission"]);
                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player, 0 , Vector3.zero, Vector3.forward);
                return;
            }

            ;
            string clockformat = "HH:mm";
            var clock = TOD_Sky.Instance.Cycle.DateTime.ToString(clockformat);
            var joining = SingletonComponent<ServerMgr>.Instance.connectionQueue.Joining;
            var Queque = SingletonComponent<ServerMgr>.Instance.connectionQueue.Queued;
            var id = 9;
            var TotalOnline = BasePlayer.activePlayerList.Count;
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                SendReply(p, Messages["Broadcast"], TotalOnline, joining, Queque, clock);
            }
        }

        [ChatCommand("time")]
        void timecmd(BasePlayer player)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, "Checker.check.Total"))
            {
                PrintMessage(player, Messages["NoPermission"]);
                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player, 0 , Vector3.zero, Vector3.forward);
                return;
            }

            string clockformat = "HH:mm";
            var clock = TOD_Sky.Instance.Cycle.DateTime.ToString(clockformat);
            PrintMessage(player, Messages["time"], clock);
        }

        [ChatCommand("Online")]
        void onlinechecker(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, "Checker.check.Total"))
            {
                PrintMessage(player, Messages["NoPermission"]);
                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player, 0 , Vector3.zero, Vector3.forward);
                return;
            }

            string playerslist = "Игроки в сети:";

            var TotalOnline = BasePlayer.activePlayerList.Count;
            var joining = SingletonComponent<ServerMgr>.Instance.connectionQueue.Joining;
            var Queque = SingletonComponent<ServerMgr>.Instance.connectionQueue.Queued;
            var SleepingCount = BasePlayer.sleepingPlayerList.Count;
            PrintMessage(player, Messages["Online"], TotalOnline, SleepingCount, joining, Queque);
        }

        [ChatCommand("admins")]
        void admincmd(BasePlayer player, string command)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, "Checker.check.admin"))
            {
                PrintMessage(player, Messages["NoPermission"]);
                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player, 0 , Vector3.zero, Vector3.forward);
                return;
            }

            string adminlist = "";
            var admincount = BasePlayer.activePlayerList.Count(p => p.IsAdmin);
            if (admincount == 0)
            {
                adminlist += "Администрации нет в сети";
            }

            if (admincount > 10)
            {
                PrintMessage(player, "Список Администрации слишком большой\nИнформация в консоле F1");
                PrintToConsole(player, Messages["admins"], admincount, adminlist);
            }
            else
            {
                foreach (BasePlayer p in BasePlayer.activePlayerList)
                {
                    if (_config.checkadmin)
                    {
                        if (permission.UserHasPermission(player.UserIDString, "Checker.admin"))
                        {
                            adminlist += $"{p.displayName} ({p.UserIDString})";
                        }
                    }
                    else
                    {
                        
                        if (p.IsAdmin)
                        {
                            adminlist += $"{p.displayName} ({p.UserIDString})";
                        }
                    }
                }

                player.ChatMessage(string.Format(Messages["admins"], admincount, adminlist));

            }
        }

        [ChatCommand("moders")]
        void moderscmd(BasePlayer player, string command)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, "Checker.check.moders"))
            {
                PrintMessage(player, Messages["NoPermission"]);
                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player, 0 , Vector3.zero, Vector3.forward);
                return;
            }

            string moderslist = "";
            var modercount =
                BasePlayer.activePlayerList.Count(p => permission.UserHasGroup(p.UserIDString, "Checker.moders"));
            if (modercount == 0)
            {
                moderslist += "Модерации нет в сети";
            }

            if (modercount > 10)
            {
                PrintMessage(player, "Список модерации слишком большой\nИнформация в консоле F1");
                PrintToConsole(player, Messages["moders"], modercount, moderslist);
            }
            else
            {
                foreach (BasePlayer p in BasePlayer.activePlayerList)
                {
                    if (permission.UserHasPermission(player.UserIDString, "Onlineadmin.check"))
                    {
                        moderslist += $"{p.displayName} ({p.UserIDString})";
                    }

                }

                player.ChatMessage(string.Format(Messages["moders"], modercount, moderslist));
            }
        }

        [ChatCommand("players")]
        void playerscmd(BasePlayer player, string command)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, "Checker.check.Total"))
            {
                PrintMessage(player, Messages["NoPermission"]);
                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", player, 0 , Vector3.zero, Vector3.forward);
                return;
            }

            string PlayersList = "";
            var playercount = BasePlayer.activePlayerList.Count;
            if (playercount > 10)
            {
                PrintMessage(player, "Список игроков слишком большой\nИнформация в консоле F1");
                PrintToConsole(player, Messages["players"], playercount, PlayersList);
            }
            else
            {
                foreach (BasePlayer p in BasePlayer.activePlayerList)
                {
                    PlayersList += $"{p.displayName} ({p.UserIDString})";
                }

                player.ChatMessage(string.Format(Messages["players"], playercount, PlayersList));
            }

        }

        #endregion

        #region config

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Время отправки сообщений")]
            public float timerbroadcast = 1800;

            [JsonProperty(PropertyName = "Отображать админов по AUTH LEVEL?")]
            public bool checkadmin = true;

            [JsonProperty(PropertyName = "Приветствие для новых игроков")]
            public string OnePlayer = "Добро пожаловать на сервер: {0}";

            [JsonProperty(PropertyName = "Автосообщение о новом игроке")]
            public string BroadcastNEW = "{0} Новый игрок на сервере!!!!";
            
            [JsonProperty(PropertyName = "Список сообщений", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> BroadMessage = new List<string>
            {
                {"Привет от SHILIZOR"},
                {"Данный плагин скачен с whiteplugins.ru"},
                {"Всего наилучшего ваш SH1L1Z0R"}
            };

            [JsonProperty(PropertyName = "Время автосообщений")]
            public int autoTimemsg = 300;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
            }
            catch (Exception e)
            {
                Puts(e.ToString());
                LoadDefaultConfig();
            }

            SaveConfig();
            var id = 9;
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
            PrintWarning("Создание нового файла конфигурации...");
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion

        #region perms

        List<string> permissions = new List<string>
        {
            "Checker.check.admin",
			"Checker.check.Total",
            "Checker.admin.use",
            "Checker.check.moders",
            "Checker.admin.use",
            "Checker.admin",
            "Checker.moders",
        };

        public static class PermissionService
        {
            public static Permission permission = Interface.GetMod().GetLibrary<Permission>();

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                if (player == null || string.IsNullOrEmpty(permissionName))
                    return false;
                var id = 9;

                var uid = player.UserIDString;
                if (permission.UserHasPermission(uid, permissionName))
                    return true;

                return false;
            }

            public static void RegisterPermissions(Plugin owner, List<string> permissions)
            {
                if (owner == null) throw new ArgumentNullException("owner");
                if (permissions == null) throw new ArgumentNullException("commands");

                foreach (var permissionName in permissions.Where(permissionName =>
                    !permission.PermissionExists(permissionName)))
                {
                    permission.RegisterPermission(permissionName, owner);
                }
            }
        }

        #endregion

        #region Localization

        private string GetLangValue(string key, string userId) => lang.GetMessage(key, this, userId);

        private Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"NoPermission", "У вас нехватает прав использовать данную команду"},
            {"admins", "Список Администрации ({0}):\n{1}"},
            {"moders", "Список Модерации ({0}):\n{1}"},
            {"players", "Список Игроков ({0}):\n{1}"},
            {"time", "Текущее время: <color=orange>{0}</color>"},
            {"Online", "<size=18>Информация об игроках:</size>\nОбщий онлайн: <color=orange>{0}</color>\nСпящих игроков: <color=orange>{1}</color>\nИгроков входит: <color=orange>{2}</color>\nИгроков в очереди: <color=orange>{3}</color>"},
            {"Broadcast", "<size=18>Информация об игроках:</size>\nОбщий онлайн: <color=orange>{0}</color>\nИгроков входит: <color=orange>{1}</color>\nИгроков в очереди: <color=orange>{2}</color>\nТекущее время: <color=orange>{3}</color>"}
        };

        #endregion
    }
