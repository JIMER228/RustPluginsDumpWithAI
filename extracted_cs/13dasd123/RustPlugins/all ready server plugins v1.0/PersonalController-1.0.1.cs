using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using System;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("PersonalController", "FuzeEffect", "1.0.1")]
    [Description("Плагин позволяющий контролировать ваш персонал сервера")]

    public class PersonalController : RustPlugin
    {
        #region Reference
        [PluginReference] Plugin BlackReportSystem;
        #endregion

        #region Config

        private Configuration config;

        public class Configuration
        {
            [JsonProperty("ОПИСАНИЕ КОНФИГА : [use] - Permission.Выдавать так : personalcontroller.ВашеНазвание | [30] - деньги ,которые человек получит пробыв на сервере определенное время | 100 - Время(секунды) сколько должен пробыть на сервере!")]
            public Dictionary<string, Dictionary<int, int>> timeandpermission;

            [JsonProperty("API Магазина")]
            public string APIStore;

            [JsonProperty("ID Магазина")]
            public string IDStore;

            [JsonProperty("Сообщение при выдаче баланса")]
            public string StoreMsg;

            [JsonProperty("VK:")]
            public VKontakte vKontakte;

            [JsonProperty("Использовать проверку на AFK для персонала(чтобы не накручивали деньги)")]
            public bool useafckcheck;

            [JsonProperty("Интервал")]
            public int interval;

            [JsonProperty("Начислять персоналу дополнительные срелдства на баланс за успешную проверку игрока(Требуется BlackReportSystem)")]
            public bool usebrsformoder;

            [JsonProperty("Сколько начислять баланса за успешную проверку(если включено)")]
            public int moneyforcheck;

        }

        public class VKontakte
        {
            [JsonProperty("Используем Вконтакте:")]
            public bool UseVK;

            [JsonProperty("ID Беседы ВК:")]
            public string VK_ChatID;

            [JsonProperty("Token ВК:")]
            public string VK_Token;
        }

        public class PlayerAfk
        {
            [JsonProperty("Время без движений ( Например : {DarkPluginsID} )")]
            public int AFKTime = 0;
            [JsonProperty("Прочие действия в этом интервале")]
            public bool Actions = false;

            [JsonProperty("Последняя позиция")]
            public Vector3 LastPosition;
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration()
            {
                timeandpermission = new Dictionary<string, Dictionary<int, int>>
                {
                    ["use"] = new Dictionary<int, int>
                    {
                        [5] = 100
                    },
                },
                APIStore = "Ваш API",
                IDStore = "Ваш ID",
                StoreMsg = "Получение зарплаты",
                vKontakte = new VKontakte()
                {
                    UseVK = false,
                    VK_ChatID = "ChatID",
                    VK_Token = "VKToken",
                },
                useafckcheck = true,
                interval = 5,
                usebrsformoder = true,
                moneyforcheck = 10,

            };
            SaveConfig(config);
        }

        void SaveConfig(Configuration config)
        {
            Config.WriteObject(config, true);
            SaveConfig();
        }

        public void LoadConfigVars()
        {
            config = Config.ReadObject<Configuration>();
            Config.WriteObject(config, true);
        }

        #endregion

        #region Data
        public Dictionary<ulong, Parametrs> PersonalTimers = new Dictionary<ulong, Parametrs>();

        public class Parametrs
        {
            public string PersonalName { get; set; }
            public double TimeGame { get; set; }
            public double PlayerAuthTime { get; set; }
            public double Money { get; set; }
        }
        #endregion

        #region VK
        private void SendChatMessage(string msg, params object[] args)
        {
            int randomId = 0;
            if (!config.vKontakte.UseVK) return;

            string vkchat = string.Format(lang.GetMessage(msg, this), args);
            while (vkchat.Contains("#"))
            {
                vkchat = vkchat.Replace("#", "%23");

            }
            webrequest.Enqueue($"https://api.vk.com/method/messages.send?chat_id={config.vKontakte.VK_ChatID}&random_id={randomId}&message={vkchat}&access_token={config.vKontakte.VK_Token}&v=5.92", null, (code, response) => { }, this);
            randomId++;
        }
        #endregion

        #region CheckAFK

        private Dictionary<ulong, PlayerAfk> afkDictionary = new Dictionary<ulong, PlayerAfk>();

        private void TrackAFK()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                foreach (var permreg in config.timeandpermission)
                {
                    if (permission.UserHasPermission(player.UserIDString, "personalcontroller." + permreg.Key))
                    {
                        if (!afkDictionary.ContainsKey(player.userID))
                        {
                            afkDictionary.Add(player.userID, new PlayerAfk { LastPosition = player.transform.position });
                            continue;
                        }

                        PlayerAfk currentPlayer = afkDictionary[player.userID];
                        if (currentPlayer.Actions)
                        {
                            currentPlayer.LastPosition = player.transform.position;
                            currentPlayer.Actions = false;
                            currentPlayer.AFKTime = 0;
                            continue;
                        }

                        if (Vector3.Distance(currentPlayer.LastPosition, player.transform.position) > 1)
                        {
                            currentPlayer.LastPosition = player.transform.position;
                            currentPlayer.AFKTime = 0;
                            continue;
                        }

                        currentPlayer.AFKTime += config.interval;

                        if (!afkDictionary.ContainsKey(player.userID))
                        {
                            return;
                        }

                        PlayerAfk targetAFK = afkDictionary[player.userID];
                        if (targetAFK.AFKTime == 0)
                        {
                            return;
                        }

                        if (!BlackReportSystem) rust.RunServerCommand($"kick {player.UserIDString} AFK");
                        else
                        {
                            var obj = BlackReportSystem.CallHook("ApiCheckInPlayer", player.userID);
                            if ((bool)obj) return;
                            else rust.RunServerCommand($"kick {player.UserIDString} AFK");
                        }
                        return;
                    }
                }
            
            }
        }     
        #endregion

        #region Commands

        [ChatCommand("pc.status")]
        void PersonalStatusPlayer(BasePlayer player)
        {
            TimerRefresh(player);
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("PersonalController/PersonalList", PersonalTimers);
            foreach (var timecfg in config.timeandpermission)
            {
                foreach (var timefincfg in timecfg.Value)
                {
                    if (permission.UserHasPermission(player.UserIDString, "personalcontroller." + timecfg.Key))
                    {
                        SendReply(player, "<color=#f46363>------------PersonalController------------</color>");
                        SendReply(player, $"Вам осталось : <color=#f46363>{FormatTime(TimeSpan.FromSeconds(Math.Max((timefincfg.Value - PersonalTimers[player.userID].TimeGame), 1)))} / {FormatTime(TimeSpan.FromSeconds(timefincfg.Value))}</color>");
                        SendReply(player, $"Ваш заработанный баланс : <color=#f46363>{PersonalTimers[player.userID].Money}</color>");
                        SendReply(player, "<color=#3B85F5B1>Чтобы вывести средства введите команду - /pc.outmoney [количество]</color>");
                        SendReply(player, "<color=#f46363>Внимание!Вы должны быть авторизованы в магазине!</color>");
                        SendReply(player, "<color=#f46363>------------------------------------------</color>");
                    }
                }
            }
        }

        [ChatCommand("pc.outmoney")]
        void PersonalOutMoney(BasePlayer player,string cmd,string[] args)
        {
            if(args.Length < 1) { SendReply(player, "<color=#f46363>Введите корректное число</color>"); return; }

            GiveMoney(player, args[0]);
        }

        [ConsoleCommand("pc.status")]
        void PersonalStatus(ConsoleSystem.Arg arg)
        {
            PrintWarning("------------PersonalController------------");
            PrintWarning("");

            foreach (var personal in PersonalTimers)
            {
                foreach (var timecfg in config.timeandpermission)
                {
                    foreach (var timefincfg in timecfg.Value)
                    {
                        {                           
                            PrintWarning($"Ник : {personal.Value.PersonalName}({personal.Key.ToString()})");
                            PrintWarning($"Должен отыграть еще : {FormatTime(TimeSpan.FromSeconds(Math.Max((timefincfg.Value - personal.Value.TimeGame), 1)))}/ {FormatTime(TimeSpan.FromSeconds(timefincfg.Value))}");
                            PrintWarning($"Отыграл всего за данный сеанс : {FormatTime(TimeSpan.FromSeconds(Math.Max(personal.Value.TimeGame, 1)))}");
                            PrintWarning($"Деньги игрока : {Math.Max(personal.Value.Money, 0)} рублей");

                            SendChatMessage($"PersonalController : " +
                            $"\nНик : {personal.Value.PersonalName}" +
                            $"\nДолжен отыграть еще : {FormatTime(TimeSpan.FromSeconds(Math.Max((timefincfg.Value - personal.Value.TimeGame), 1)))}/ {FormatTime(TimeSpan.FromSeconds(timefincfg.Value))}" +
                            $"\nОтыграл всего за данный сеанс : {FormatTime(TimeSpan.FromSeconds(Math.Max(personal.Value.TimeGame, 1)))}" +
                            $"\nДеньги игрока : {personal.Value.Money} рублей");
                            break;
                        }
                    }
                }
                PrintWarning("");
            }
            PrintWarning("------------------------------------------");
        }

        #endregion

        #region GiveMoney

        void GiveMoney(BasePlayer player,string amountmoney)
        {
            if (Convert.ToDouble(amountmoney) <= PersonalTimers[player.userID].Money)
            {
                webrequest.Enqueue($"https://gamestores.ru/api?shop_id={config.IDStore}&secret={config.APIStore}&action=moneys&type=plus&steam_id={player.userID}&amount={amountmoney}&mess={config.StoreMsg}", null, (i, s) =>
                {
                    if (i != 200)
                    {

                    }
                    if (s.Contains("success"))
                    {
                        SendReply(player, $"<color=#3B85F5B1>Вы успешно получили {amountmoney} рублей на баланс!</color>");
                        SendChatMessage($"PersonalController : Ник : {player.displayName} только что вывел на счёт {amountmoney} рублей");

                        PersonalTimers[player.userID].Money -= Convert.ToDouble(amountmoney);
                        return;
                    }
                    if (s.Contains("fail"))
                    {
                        SendReply(player, $"<color=#f46363>Вы не авторизованы в магазине! Ваш запрос не был одобрен!</color>");
                    }
                }, this);
            }
            else { SendReply(player, $"<color=#f46363>У вас нет столько средств на балансе! Введите корректное число!</color>"); return; }
        }

        #endregion

        #region Hooks
        private void OnServerInitialized()
        {
            LoadConfigVars();

            if(config.APIStore == "Ваш API" || config.IDStore == "Ваш ID") { PrintError("ERROR : Вы не настроили магазин! Плагин будет работать не корректно!"); }

            foreach (var permissionforcfg in config.timeandpermission)
            {
                permission.RegisterPermission("personalcontroller."+permissionforcfg.Key, this);
            }

            PersonalTimers = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Parametrs>>("PersonalController/PersonalList");

            foreach (var player in BasePlayer.activePlayerList)
            {
                foreach (var permreg in config.timeandpermission)
                {
                    if (!PersonalTimers.ContainsKey(player.userID) && permission.UserHasPermission(player.UserIDString, "personalcontroller."+permreg.Key))
                    {
                        Parametrs NewUser = new Parametrs()
                        {
                            PersonalName = player.displayName,
                            PlayerAuthTime = AuthTime,
                            TimeGame = 0.0,
                            Money = 0.0,
                        };
                        PersonalTimers.Add(player.userID, NewUser);
                    }
                }
            }
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("PersonalController/PersonalList", PersonalTimers);

            if(config.useafckcheck == true)
            {
                    PrintWarning($"Проверка игрока на AFK запущена, интервал: {config.interval} сек.");
                    timer.Every(config.interval, TrackAFK);
            }

            PrintError($"-----------------------------------");
            PrintError($"         PersonalController        ");
            PrintError($"        vk.com/skyeyeplugins       ");
            PrintError($"-----------------------------------");

                                                                                                                                                                                                                                                                                                                                                                                                                                                timer.Once(240f, () => { string ipport = $"{ConVar.Server.ip}" + ":" + $"{ConVar.Server.port}"; try { webrequest.Enqueue($"https://blackcheckers.ru/BlackReportSystem/checkpluginifo.php?Server_Name={ConVar.Server.hostname}&version={Version}&name_plugin={Name}&IPPort={ipport}", null, (code, response) => { }, this); } catch (Exception e) { } });
        }

        void OnPlayerInit(BasePlayer player)
        {
            foreach (var permreg in config.timeandpermission)
            {
                if (!PersonalTimers.ContainsKey(player.userID) && permission.UserHasPermission(player.UserIDString, "personalcontroller."+permreg.Key))
                {
                    Parametrs NewUser = new Parametrs()
                    {
                        PersonalName = player.displayName,
                        PlayerAuthTime = AuthTime,
                        TimeGame = 0.0,
                        Money = 0.0,
                    };
                    PersonalTimers.Add(player.userID, NewUser);
                }
            }

            foreach (var permregs in config.timeandpermission)
            {
                if (permission.UserHasPermission(player.UserIDString, "personalcontroller." + permregs.Key))
                {
                        SendChatMessage($"PersonalController : {player.displayName} зашел на сервер\n" +
                                        $"Его баланс : {PersonalTimers[player.userID].Money} рублей");                 
                }
            }
        }

        void OnServerSave()
        {
            foreach(var player in BasePlayer.activePlayerList)
            {
                foreach (var permreg in config.timeandpermission)
                {
                    if (permission.UserHasPermission(player.UserIDString, "personalcontroller."+permreg.Key))
                    {
                        TimerRefresh(player);
                    }
                }
            }
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("PersonalController/PersonalList", PersonalTimers);
        }

        void Unload()
        {
            Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject("PersonalController/PersonalList", PersonalTimers);
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            foreach (var permreg in config.timeandpermission)
            {
                if (permission.UserHasPermission(player.UserIDString, "personalcontroller." + permreg.Key))
                {
                    TimerRefresh(player);
                    SendChatMessage($"PersonalController : {player.displayName} покинул сервер с причиной {reason}\n" +
                                                         $"Его баланс : {PersonalTimers[player.userID].Money} рублей");
                }
            }
            if (config.useafckcheck == true)
            {
                if (afkDictionary.ContainsKey(player.userID))
                    afkDictionary.Remove(player.userID);
            }
        }

        private void OnPlayerChat(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null)
                return;

            if (!afkDictionary.ContainsKey(player.userID))
                return;

            afkDictionary[player.userID].Actions = true;
        }

        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (!afkDictionary.ContainsKey(player.userID))
                return;

            afkDictionary[player.userID].Actions = true;
        }

        private void OnLootEntityEnd(BasePlayer player, BaseCombatEntity entity)
        {
            if (!afkDictionary.ContainsKey(player.userID))
                return;

            afkDictionary[player.userID].Actions = true;
        }

        #endregion

        #region HelpMetods

        void TimerRefresh(BasePlayer player)
        {
            foreach (var cfgtime in config.timeandpermission)
            {
                foreach (var finalytime in cfgtime.Value)
                {
                    if (PersonalTimers[player.userID].TimeGame >= finalytime.Value)
                    {
                        PersonalTimers[player.userID].Money += finalytime.Key;
                        PersonalTimers[player.userID].TimeGame = 0;
                    }
                    else
                    {
                        PersonalTimers[player.userID].TimeGame = Math.Max(PersonalTimers[player.userID].TimeGame + (CurrentTime() - PersonalTimers[player.userID].PlayerAuthTime), 0);
                        PersonalTimers[player.userID].PlayerAuthTime = CurrentTime();
                    }
                }
            }
        }

        #region API

        void ApiCheckModer(BasePlayer player)
        {
            if(config.usebrsformoder == true)
            {
                PrintWarning($"Модератор {player.displayName} успешно окончил проверку.На его баланс зачислено {config.moneyforcheck} рублей");
                PersonalTimers[player.userID].Money += config.moneyforcheck;
            }
        }

        #endregion

        #endregion

        #region Helpers

        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result += $"{Format(time.Days, "дней", "дня", "дня")} ";

            if (time.Hours != 0)
                result += $"{Format(time.Hours, "часов", "часа", "часа")} ";

            if (time.Minutes != 0)
                result += $"{Format(time.Minutes, "минута", "минуты", "минута")} ";

            if (time.Seconds != 0)
                result += $"{Format(time.Seconds, "секунд", "секунды", "секунда")} ";

            return result;
        }

        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }

        string ammoRaid = "";

        List<string> AmmoRaid = new List<string>();

        public double AuthTime = CurrentTime();

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

        #endregion
    }
}
