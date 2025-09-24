using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("RaidNotice", "RustPlugin.ru", "1.1.2")]
    class RaidNotice : RustPlugin
    {
        Dictionary<BasePlayer, float> lastTimeSended = new Dictionary<BasePlayer, float>();
        private string VkToken = "";
        private string VkMessages = "Похоже Вас пытается зарейдить игрок {0} на сервере ServerName. \nНачало рейда в {1} МСК.";

        private string SMSMessages = "Похоже Вас пытается зарейдить игрок {0} на сервере ServerName.";

        private string SmsToken = "";
        private int timeMin = 15;
        string permissionvk = "raidnotice.allowed.vk";
        string permissionphone = "raidnotice.allowed.phone";
        string raidnotice = "Игрок {0} напал на Ваш дом! Скорее дайте отпор";
        int serverID = 0;
        private void LoadConfigValues()
        {
            bool changed = false;
            if (GetConfig("Основные настройки", "Токен группы Вконтакте (Разрешение сообществу на сообщения)", ref VkToken))
            {
                changed = true;
            }
            if (GetConfig("Основные настройки", "Api_ID для отправки смс (api_id с сайта sms.ru с Вашего аккаунта )", ref SmsToken))
            {
                changed = true;
            }
            if (GetConfig("Привилегии", "Привилегия использования оповещения Вконтакте", ref permissionvk))
            {
                changed = true;
            }
            if (GetConfig("Привилегии", "Привилегия использования оповещения СМС", ref permissionphone))
            {
                changed = true;
            }
            if (GetConfig("Основные настройки", "Сообщение о рейде игроку Вконтакте", ref VkMessages))
            {
                changed = true;
            }

            if (GetConfig("Основные настройки", "Сообщение о рейде игроку в СМС", ref SMSMessages))
            {
                changed = true;
            }
            if (GetConfig("Основные настройки", "Переодичность отправки сообщений Вконтакте", ref timeMin)) changed = true;
            if (GetConfig("Основные настройки", "Сообщение в чат о начале рейда", ref raidnotice)) changed = true;
            if (changed) SaveConfig();
        }
        private bool GetConfig<T>(string MainMenu, string Key, ref T var)
        {
            if (Config[MainMenu, Key] != null)
            {
                var = (T)Convert.ChangeType(Config[MainMenu, Key], typeof(T));
                return false;
            }
            Config[MainMenu, Key] = var;
            return true;
        }
        Dictionary<ulong, PlayerData> PlayersDate = new Dictionary<ulong, PlayerData>();
        class PlayerData
        {
            public DateTime LastDateOfRaid;
            public string UserVKId;
            public bool EnabledVK;
            public string UserPhone;
            public bool EnabledPhone;
            public string CodeVk;
            public string CodePhone;
            public DateTime TimeToAddVK;
            public DateTime TimeToAddPhone;
        }
        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("RaidNotice", PlayersDate);
        }
        void LoadData()
        {
            try
            {
                PlayersDate = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, PlayerData>>("RaidNotice");
            }
            catch
            {
                PlayersDate = new Dictionary<ulong, PlayerData>();
            }
        }
        void Unloaded()
        {
            SaveData();
        }
        void OnServerSave()
        {
            SaveData();
        }

        BasePlayer OnlinePlayer(ulong id) => BasePlayer.activePlayerList.ToList().Find(p => p.userID == id);
        void OnPlayerConnected(BasePlayer player)
        {
            if (!PlayersDate.ContainsKey(player.userID))
            {
                PlayersDate.Add(player.userID, new PlayerData()
                {
                    CodeVk = UnityEngine.Random.Range(1000, 9999).ToString(),
                    CodePhone = UnityEngine.Random.Range(1000, 9999).ToString(),
                    LastDateOfRaid = DateTime.UtcNow,
                    UserPhone = "",
                    UserVKId = "",
                    EnabledPhone = false,
                    EnabledVK = false,
                }
                );
            }
        }

        [ConsoleCommand("rn")]
        void cmdrn(ConsoleSystem.Arg arg)
        {
            rn(arg.Player(), arg.cmd.FullName, arg.Args);
            rust.RunClientCommand(arg.Player(), $"custommenu false Main {VKID}");
        }

        [ChatCommand("rn")]
        void rn(BasePlayer player, string command, string[] arg)
        {
            if (!permission.UserHasPermission(player.userID.ToString(), permissionvk) && !permission.UserHasPermission(player.userID.ToString(), permissionphone))
            {
                PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Купить эту услугу вы можете в магазине сервера");
                return;
            }
            if (!PlayersDate.ContainsKey(player.userID))
            {
                OnPlayerConnected(player);
            }
            var reply = 288;
            if (reply == 0) { }
            string vkID = PlayersDate[player.userID].UserVKId;
            string phoneNumber = PlayersDate[player.userID].UserPhone;
            serverID = reply;
            if (arg.Length == 0)
            {
                if (vkID == "" && phoneNumber == "")
                {
                    PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Напишите /rn add vk.com/id123456789\nЕсли Вам нужна помощь, напишите /rn help");
                    return;
                }
                if (vkID != "")
                {
                    PrintToChat(player, $"<color=red>[Оповещение-Рейда]</color>: Ваш вк указан как: <color=#a2d953>{vkID}</color>\nЕсли Вам нужна помощь, напишите /rn help");
                    return;
                }
                if (phoneNumber != "")
                {
                    PrintToChat(player, $"<color=red>[Оповещение-Рейда]</color>: Ваш номер телефона указан как: <color=#a2d953>{phoneNumber}</color>\nЕсли Вам нужна помощь, напишите /rn help");
                    return;
                }
            }
            if (arg.Length > 0)
            {
                if (arg[0].ToLower() == "help")
                {
                    var number = phoneNumber != "" ? phoneNumber : "Номер не указан";
                    var vk = vkID != "" ? vkID : "Страница ВК не указана";
                    PrintToChat(player, $"<color=red>[Оповещение-Рейда]</color>: Информация:\nНомер телефона: <color=#a2d953>{number}</color>\nВК: <color=#a2d953>{vk}</color>\nЧтобы удалить номер телефона или ссылку на страницу:\n<color=#a2d953>/rn delete vk</color> - Удалить страницу ВК\n<color=#a2d953>/rn delete phone</color> - Удалить номер телефона");
                    return;
                }
                if (arg[0].ToLower() == "delete")
                {
                    if (arg.Length == 2)
                    {
                        if (arg[1].ToLower() == "phone")
                        {
                            string vid = PlayersDate[player.userID].UserPhone;
                            if (vid == null)
                            {
                                PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: У вас нет привязанного телефона");
                                return;
                            }
                            DateTime cd = PlayersDate[player.userID].TimeToAddPhone;
                            var howmuch = cd - DateTime.UtcNow;
                            if (howmuch.Minutes > -5)
                            {
                                PrintToChat(player, $"<color=red>[Оповещение-Рейда]</color>: Вы сможете удалить свой номер телефона через <color=#CD2626>{howmuch.Minutes + 5} мин.</color>");
                                return;
                            }
                            PlayersDate[player.userID].UserPhone = "";
                            PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Ваш номер телефона был удален. Укажите новый командой /rn add");
                            return;
                        }
                        if (arg[1].ToLower() == "vk")
                        {
                            string tempvid = PlayersDate[player.userID].UserVKId;
                            if (tempvid == "")
                            {
                                PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: У вас нет привязанного VK");
                                return;
                            }
                            DateTime cd = PlayersDate[player.userID].TimeToAddVK;
                            var howmuch = cd - DateTime.UtcNow;
                            if (howmuch.Minutes > -5)
                            {
                                PrintToChat(player, $"<color=red>[Оповещение-Рейда]</color>: Вы сможете удалить свой VK через <color=#CD2626>{howmuch.Minutes + 5} мин.</color>");
                                return;
                            }
                            PlayersDate[player.userID].UserVKId = "";
                            PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Ваш VK был отвязан. Укажите новый командой /rn add");
                            return;
                        }
                    }
                    else
                    {
                        PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Напишите:\n/rn delete vk - отвязать вк\n/rn delete phone - отвязать номер");
                        return;
                    }
                }
            }
            if (arg.Length > 0)
            {
                if (arg[0].ToLower() == "accept")
                {
                    if (arg.Length == 2)
                    {
                        if (string.IsNullOrEmpty(arg[1]))
                        {
                            PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Вы не указали проверочный код");
                            Notifications?.Call("API_AddUINote", player.userID, "Вы не указали проверочный код");
                            return;
                        }
                        string codeVk = PlayersDate[player.userID].CodeVk;
                        string codePhone = PlayersDate[player.userID].CodePhone;
                        if (codeVk != "")
                        {
                            if (arg[1] == codeVk)
                            {
                                PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Отлично! Ваш VK подтвержден!");
                                Notifications?.Call("API_AddUINote", player.userID, "Отлично! Ваш VK подтвержден!");
                                PlayersDate[player.userID].EnabledVK = true;
                                return;
                            }
                            else if (arg[1] != codePhone)
                            {
                                PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Неправильный код подтверждения");
                                Notifications?.Call("API_AddUINote", player.userID, "Неправильный код подтверждения");
                                return;
                            }
                        }
                        if (arg[1] == codePhone)
                        {
                            PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Отлично! Ваш номер телефона подтвержден!");
                            Notifications?.Call("API_AddUINote", player.userID, "Отлично! Ваш номер телефона подтвержден!");
                            PlayersDate[player.userID].EnabledPhone = true;
                            return;
                        }
                        else
                        {
                            PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Неправильный код подтверждения");
                            Notifications?.Call("API_AddUINote", player.userID, "Неправильный код подтверждения");
                            return;
                        }
                    }
                    else
                    {
                        PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Вы не указали проверочный код");
                        Notifications?.Call("API_AddUINote", player.userID, "Вы не указали проверочный код");
                        return;
                    }
                }
                if (arg[0].ToLower() == "add")
                {
                    if (arg.Length == 2)
                    {
                        string id = arg[1];
                        if (string.IsNullOrEmpty(id))
                        {
                            PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Ошибка при вводе");
                            Notifications?.Call("API_AddUINote", player.userID, "Ошибка при вводе");
                            return;
                        }
                        if (id.ToLower().Contains("vk.com/"))
                        {
                            if (!permission.UserHasPermission(player.userID.ToString(), permissionvk))
                            {
                                PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: У вас нет привелегий для добавления ВК");
                                Notifications?.Call("API_AddUINote", player.userID, "У вас нет привелегий для добавления ВК");
                                return;
                            }
                            string valueid = PlayersDate[player.userID].UserVKId;
                            if (valueid != "")
                            {
                                PrintToChat(player, $"<color=red>[Оповещение-Рейда]</color>: У вас уже привязан VK: <color=#a2d953>{valueid}</color>");
                                Notifications?.Call("API_AddUINote", player.userID, $"У вас уже привязан VK: {valueid}");
                                return;
                            }
                            DateTime cd = PlayersDate[player.userID].TimeToAddVK;
                            var howmuch = cd - DateTime.UtcNow;
                            if (howmuch.Minutes > 0)
                            {
                                PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Вам уже отправлено сообщение на ваш адрес VK.\nПодождите <color=#CD2626>5 минут</color> прежде чем вк можно будет указать заного");
                                Notifications?.Call("API_AddUINote", player.userID, "Вам уже отправлено сообщение на ваш адрес VK. Подождите 5 минут прежде чем вк можно будет указать заного");
                                return;
                            }
                            PlayersDate[player.userID].TimeToAddVK = DateTime.UtcNow;
                            PlayersDate[player.userID].UserVKId = id;
                            GetRequest(player.userID, id, "0", $"Введите на сервер: /rn accept " + PlayersDate[player.userID].CodeVk, true);
                            return;
                        }
                        String cont = id.Substring(0, 1);
                        if (id.ToLower().Contains("+") && id.Length > 9)
                        {
                            if (!permission.UserHasPermission(player.userID.ToString(), permissionphone))
                            {
                                PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: У вас нет привелегий для добавления телефона");
                                Notifications?.Call("API_AddUINote", player.userID, "У вас нет привелегий для добавления телефона");
                                return;
                            }
                            string valueid = PlayersDate[player.userID].UserPhone;
                            if (valueid != "")
                            {
                                PrintToChat(player, $"<color=red>[Оповещение-Рейда]</color>: У вас уже привязан номер: <color=#a2d953>{valueid}</color>");
                                Notifications?.Call("API_AddUINote", player.userID, $"У вас уже привязан номер: {valueid}");
                                return;
                            }
                            DateTime cd = PlayersDate[player.userID].TimeToAddPhone;
                            var howmuch = cd - DateTime.UtcNow;
                            if (howmuch.Minutes > -5)
                            {
                                PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Вам уже отправлено сообщение на ваш номер.\nПодождите <color=#CD2626>5 минут</color> прежде чем номер можно будет указать заного");
                                Notifications?.Call("API_AddUINote", player.userID, "Вам уже отправлено сообщение на ваш номер. Подождите 5 минут прежде чем номер можно будет указать заного");
                                return;
                            }
                            PlayersDate[player.userID].TimeToAddPhone = DateTime.UtcNow;
                            if (id.Contains("+"))
                            {
                                id = id.Trim(new Char[] {
                                    '+'
                                }
                                );
                            }
                            if (!id.All(char.IsDigit))
                            {
                                PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Неверный номер телефона!");
                                Notifications?.Call("API_AddUINote", player.userID, "Неверный номер телефона!");
                                return;
                            }
                            PlayersDate[player.userID].UserPhone = id;
                            GetRequest(player.userID, id, "1", $"Введите на сервер: /rn accept " + PlayersDate[player.userID].CodePhone, true);
                        }
                        else
                        {
                            PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Вы неверно ввели номер телефона. Перед номером указывайте +\nНапример +1234567890");
                            Notifications?.Call("API_AddUINote", player.userID, "Вы неверно ввели номер телефона. Перед номером указывайте .Например +1234567890");
                            return;
                        }
                    }
                    else
                    {
                        PrintToChat(player, "<color=red>[Оповещение-Рейда]</color>: Вы не указали номер телефона или VK ID");
                        Notifications?.Call("API_AddUINote", player.userID, "Вы не указали номер телефона или VK ID");
                        return;
                    }
                }
            }
        }

        private string SendMessage(ulong playerid, string device = "type", string message = "Message")
        {
            PlayerData playerData;
            if (PlayersDate.TryGetValue(playerid, out playerData))
            {
                if (device.Equals("0") && playerData.EnabledVK)
                {
                    GetRequest(playerid, device == "0" ? playerData.UserVKId : playerData.UserPhone, device, message, false);
                    return "Success";
                }

                if (device.Equals("1") && playerData.EnabledPhone)
                {
                    GetRequest(playerid, device == "0" ? playerData.UserVKId : playerData.UserPhone, device, message, false);
                    return "Success";
                }

                return "Error";
            }
            else
            {
                return "Error";
            }
        }

        private void GetRequest(ulong playerid, string id = "id", string device = "type", string message = "", bool isCode = false)
        {
            string url = string.Empty;

            if (device == "0")
            {
                if (id.Contains("vk.com/id"))
                    id = id.Substring(id.IndexOf("id") + 2);
                else if (id.Contains("vk.com/"))
                    id = id.Substring(id.IndexOf("vk.com/") + 7);

                webrequest.EnqueueGet($"https://api.vk.com/method/users.get?user_ids={id}&v=5.73&access_token={VkToken}", (code2, response2) =>
                {
                    var ID = JsonConvert.DeserializeObject<Response>(response2);
                    id = ID.response[0].id.ToString();
                    url = $"https://api.vk.com/method/messages.send?user_ids={id}&message={URLEncode(message)}&v=5.73&access_token={VkToken}";
                    webrequest.EnqueueGet(url, (code, response) => GetCallback(code, response, playerid, id, device, isCode), this);
                }, this);
                return;
            }

            if (device == "1")
            {
                url = $"https://sms.ru/sms/send?api_id={SmsToken}&to={id}&msg={URLEncode(message)}&json=1";
            }

            webrequest.EnqueueGet(url, (code, response) => GetCallback(code, response, playerid, id, device, isCode), this);
        }

        public class Response
        {
            public List<User> response { get; set; }
        }
        public class User
        {
            public int id { get; set; }
        }

        [PluginReference] Plugin Notifications;
        private void GetCallback(int code, string response, ulong userID, string id, string device = "", bool isCode = false)
        {
            BasePlayer player = BasePlayer.FindByID(userID);
            if (player != null)
            {
                if (response == null || code != 200)
                {
                    PrintError($"Ошибка для {player.displayName}");
                    return;
                }

                if (device == "0")
                {
                    if (response.Contains("Can't send messages for users without permission"))
                    {
                        Notifications?.Call("API_AddUINote", userID, $"<color=red>[Оповещение-Рейда]</color>: Невозможно отправить сообщение.\nБот не может написать вам, разрешите группе присылать Вам сообщения");
                        PrintToChat(player, $"<color=red>[Оповещение-Рейда]</color>: Невозможно отправить сообщение.\nБот не может написать вам, разрешите группе присылать Вам сообщения");
                        PlayersDate[userID].UserVKId = "";
                        return;
                    }

                    if (response.Contains("One of the parameters specified was missing or invalid"))
                    {
                        Notifications?.Call("API_AddUINote", userID, $"<color=red>[Оповещение-Рейда]</color>: Невозможно отправить сообщение.\nПроверьте правильность ссылки (<color=#a2d953>{id}</color>) или повторите позже");
                        PrintToChat(player, $"<color=red>[Оповещение-Рейда]</color>: Невозможно отправить сообщение.\nПроверьте правильность ссылки (<color=#a2d953>{id}</color>) или повторите позже");
                        PlayersDate[userID].UserVKId = "";
                        return;
                    }

                    if (isCode)
                    {
                        Notifications?.Call("API_AddUINote", userID, $"<color=red>[Оповещение-Рейда]</color>: Вы указали VK: <color=#a2d953>{id}</color>\nВам в VK отправлено сообщение.\nПрочитайте и следуйте инструкциям для подтверждения");
                        PrintToChat(player, $"<color=red>[Оповещение-Рейда]</color>: Вы указали VK: <color=#a2d953>{id}</color>\nВам в VK отправлено сообщение.\nПрочитайте и следуйте инструкциям для подтверждения");
                    }
                }

                if (device == "1")
                {
                    if (response.Contains("Неправильно указан номер телефона получателя, либо на него нет маршрута"))
                    {
                        Notifications?.Call("API_AddUINote", userID, $"<color=red>[Оповещение-Рейда]</color>: Вы указали неправильный номер телефона. Повторите попытку");
                        PrintToChat(player, $"<color=red>[Оповещение-Рейда]</color>: Вы указали неправильный номер телефона. Повторите попытку");
                        PlayersDate[userID].UserPhone = "";
                        return;
                    }

                    if (isCode)
                    {
                        Notifications?.Call("API_AddUINote", userID, $"<color=red>[Оповещение-Рейда]</color>: Вы указали телефон: <color=#a2d953>{id}</color>\nВам на телефон отправлено сообщение.\nПрочитайте и следуйте инструкциям для подтверждения");
                        PrintToChat(player, $"<color=red>[Оповещение-Рейда]</color>: Вы указали телефон: <color=#a2d953>{id}</color>\nВам на телефон отправлено сообщение.\nПрочитайте и следуйте инструкциям для подтверждения");
                    }
                }
            }
        }

        /*
        void GetRequest(ulong playerid, string raiderName, string id = "id", string key = "key", string device = "type⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠")
        {
            var messagesSend = string.Format(device == "0" ? VkMessages : SMSMessages, raiderName, DateTime.Now.ToShortTimeString());
            string url = $"https://rustplugin.ru/plugins/sendrsrn.php?id={id}&key={key}&seckey=OIbhJHOIH&device={device}&accesstoken={VkToken}&smstoken={SmsToken}&serverID={serverID}&sendmessages={URLEncode(messagesSend)}";
            webrequest.EnqueueGet(url, (code, response) => GetCallback(code, response, playerid, id, device), this);
        }
        

        void GetCallback(int code, string response, ulong player, string id, string device)
        {
            var BsPlayer = BasePlayer.FindByID(player);
            if (BsPlayer != null)
            {
                if (response == null || code != 200)
                {
                    PrintError($"Ошибка для {BsPlayer.displayName}");
                    return;
                }
                if (response.Contains("Good"))
                {
                    if (device == "0")
                    {
                        PrintToChat(BsPlayer, $"<color=red>[Оповещение-Рейда]</color>: Вы указали VK: <color=#a2d953>{id}</color>\nВам в VK отправлено сообщение.\nПрочитайте и следуйте инструкциям для подтверждения");
                    }
                    if (device == "1")
                    {
                        PrintToChat(BsPlayer, $"<color=red>[Оповещение-Рейда]</color>: Вы указали телефон: <color=#a2d953>{id}</color>\nВам на телефон отправлено сообщение.\nПрочитайте и следуйте инструкциям для подтверждения");
                    }
                    return;
                }
                if (response.Contains("PrivateMessage"))
                {
                    PrintToChat(BsPlayer, $"<color=red>[Оповещение-Рейда]</color>: Ваши настройки приватности не позволяют отправить вам сообщение (<color=#a2d953>{id}</color>)");
                    return;
                }
                if (response.Contains("ErrorSend"))
                {
                    PrintToChat(BsPlayer, $"<color=red>[Оповещение-Рейда]</color>: Невозможно отправить сообщение.\nПроверьте правильность ссылки (<color=#a2d953>{id}</color>) или повторите позже");
                    return;
                }
                if (response.Contains("BlackList"))
                {
                    PrintToChat(BsPlayer, $"<color=red>[Оповещение-Рейда]</color>: Невозможно отправить сообщение.\nБот не может написать вам, разрешите группе присылать Вам сообщения");
                    return;
                }
                if (response.Contains("BadPhone"))
                {
                    PrintToChat(BsPlayer, $"<color=red>[Оповещение-Рейда]</color>: Вы указали неправильный номер телефона. Повторите попытку");
                    return;
                }
                if (response.Contains("BadBalance"))
                {
                    PrintToChat(BsPlayer, $"<color=red>[Оповещение-Рейда]</color>: Произошла ошибка! Обратитесь к администратору");
                    PrintError("Баланс для смсок иссяк");
                    return;
                }
                if (device == "0")
                {
                    PrintToChat(BsPlayer, $"<color=red>[Оповещение-Рейда]</color>: Вы указали неверный VK ID (<color=#a2d953>{id}</color>)");
                }
                if (device == "1")
                {
                    PrintToChat(BsPlayer, $"<color=red>[Оповещение-Рейда]</color>: Вы указали неверный номер (<color=#a2d953>{id}</color>)");
                }
            }
        }*/

        void SendMsg(BasePlayer player, BasePlayer iniciator)
        {
            float value = 0;
            if (lastTimeSended.TryGetValue(player, out value))
            {
                if (lastTimeSended[player] + 1 > Time.realtimeSinceStartup) return;
                Msg(player, iniciator);
                lastTimeSended[player] = Time.realtimeSinceStartup;
            }
            else
            {
                Msg(player, iniciator);
                lastTimeSended[player] = Time.realtimeSinceStartup;
            }
        }

        private double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalMinutes;

        private double TimeTotalSeconds(DateTime type) => type.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalMinutes;

        void SendOfflineMessage(string message, ulong playerid = 294912)
        {
            if (!PlayersDate.ContainsKey(playerid)) return;
            string value = PlayersDate[playerid].UserVKId;
            if (permission.UserHasPermission(playerid.ToString(), permissionvk))
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var time = GrabCurrentTime() - TimeTotalSeconds(PlayersDate[playerid].LastDateOfRaid);
                    if (time > timeMin)
                    {
                        SendMessage(playerid, "0", message);
                        PlayersDate[playerid].LastDateOfRaid = DateTime.UtcNow;
                    }
                }
            }
            string valuephone = PlayersDate[playerid].UserPhone;
            if (permission.UserHasPermission(playerid.ToString(), permissionphone))
            {
                if (!string.IsNullOrEmpty(valuephone))
                {
                    var time = GrabCurrentTime() - TimeTotalSeconds(PlayersDate[playerid].LastDateOfRaid);
                    if (time > timeMin)
                    {
                        PlayersDate[playerid].LastDateOfRaid = DateTime.UtcNow;
                        timer.Once(5f, () => SendMessage(playerid, "1", message));
                    }
                }
            }
        }

        void Msg(BasePlayer player, BasePlayer iniciator)
        {
            if (permission.UserHasPermission(player.userID.ToString(), permissionvk))
            {
                var time = GrabCurrentTime() - TimeTotalSeconds(PlayersDate[player.userID].LastDateOfRaid);
                if (time > timeMin)
                {
                    PrintToChat(player, raidnotice, iniciator.displayName);
                }
                return;
            }
            if (permission.UserHasPermission(player.userID.ToString(), permissionphone))
            {
                var time = GrabCurrentTime() - TimeTotalSeconds(PlayersDate[player.userID].LastDateOfRaid);
                if (time > timeMin)
                {
                    PrintToChat(player, raidnotice, iniciator.displayName);
                }
            }
        }

        private string URLEncode(string input)
        {
            if (input.Contains("#")) input = input.Replace("#", "%23");
            if (input.Contains("$")) input = input.Replace("$", "%24");
            if (input.Contains("+")) input = input.Replace("+", "%2B");
            if (input.Contains("/")) input = input.Replace("/", "%2F");
            if (input.Contains(":")) input = input.Replace(":", "%3A");
            if (input.Contains(";")) input = input.Replace(";", "%3B");
            if (input.Contains("?")) input = input.Replace("?", "%3F");
            if (input.Contains("@")) input = input.Replace("@", "%40");
            return input;
        }

        [PluginReference] Plugin XMenu;
        Timer TimerInitialize;

        int VKID;
        void OnServerInitialized()
        {
            LoadData();
            LoadConfig();
            LoadConfigValues();
            if (string.IsNullOrEmpty(VkToken))
            {
                PrintError("Vk Token is null! Sending messages is not possible!");
                return;
            }
            if (!permission.PermissionExists(permissionvk)) permission.RegisterPermission(permissionvk, this);
            if (!permission.PermissionExists(permissionphone)) permission.RegisterPermission(permissionphone, this);

            TimerInitialize = timer.Every(5f, () =>
            {
                if (XMenu.IsLoaded)
                {
                    XMenu.Call("API_RegisterSubMenu", this.Name, "Main", "ВКонтакте", "RenderVK", null);

                    VKID = (int)XMenu.Call("API_GetSubMenuID", "Main", "ВКонтакте");
                    cmd.AddChatCommand("vk", this, (p, cmd, args) => rust.RunClientCommand(p, $"custommenu true Main {VKID}"));

                    TimerInitialize.Destroy();
                }
            });
        }

        public const string MenuLayer = "XMenu";
        public const string MenuItemsLayer = "XMenu.MenuItems";
        public const string MenuSubItemsLayer = "XMenu.MenuSubItems";
        public const string MenuContent = "XMenu.Content";

        private void RenderVK(ulong userID, object[] objects)
        {
            CuiElementContainer Container = (CuiElementContainer)objects[0];
            bool FullRender = (bool)objects[1];
            string Name = (string)objects[2];
            int ID = (int)objects[3];
            int Page = (int)objects[4];

            Container.Add(new CuiElement
            {
                Name = MenuContent,
                Parent = MenuLayer,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-215 -230",
                            OffsetMax = "500 270"
                        },
                    }
            });
            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info",
                Parent = MenuContent,
                Components =
                    {
                        new CuiImageComponent
                        {
                            Color = HexToRustFormat("#0000007f"),
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "80 -460",
                            OffsetMax = "630 -10"
                        }
                    }
            });

            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info.Img",
                Parent = MenuContent + ".Info",
                Components =
                    {
                        new CuiRawImageComponent
                        {
                            Url = "https://i.imgur.com/C8qhaVA.png",
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = "25 -225",
                            OffsetMax = "525 -25"
                        }
                    }
            });

            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info.MainTitle",
                Parent = MenuContent + ".Info",
                Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = "Рейд уведомления от сервера <b>TRASH <color=#D04425>RUST</color> CLANS</b> [RPG]\nПрисоеденяйтесь к группе ВК: <color=#FFAA00AA>https://vk.com/trashrust</color>",
                                    Align = TextAnchor.MiddleCenter,
                                    FontSize = 20,
                                    Font = "robotocondensed-regular.ttf",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"25 -305",
                                    OffsetMax = $"525 -225"
                                }
                            }
            });

            string vkTitle = "Введите VK ID страницы например: <color=#FFAA00AA>vk.com/id01</color>\nБесплатное уведомление о рейде с информацией о нике \nрейдера и квадрате местоположения вашей постройки.";
            string phoneTitle = "Введите номер телефона в формате: <color=#FFAA00AA>+7 999-999-99-99</color>\nДанное уведомление предостовляется отдельно.\nПодробности уточняйте в ЛС группы сервера!";

            TextAnchor vkTextAnchor = TextAnchor.MiddleLeft;
            int vkFontSize = 12;

            string vkInputText = string.Empty;
            string vkInputCommand = $"rn add {vkInputText}";

            string phoneInputText = string.Empty;
            string phoneInputCommand = $"rn add {phoneInputText}";

            TextAnchor phoneTextAnchor = TextAnchor.MiddleLeft;
            int phoneFontSize = 12;

            PlayerData playerData;
            if(PlayersDate.TryGetValue(userID, out playerData))
            {
                if (!string.IsNullOrEmpty(playerData.UserVKId))
                {
                    vkTitle = $"Введите код подтверждения:";
                    vkInputCommand = $"rn accept {vkInputText}";
                }

                if (playerData.EnabledVK)
                {
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".Info.BackgroundVK",
                        Parent = MenuContent + ".Info",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#0000007f"),
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = $"25 -355",
                                OffsetMax = $"525 -305"
                            }
                        }
                    });

                    vkTitle = $"[ <color=#7FAA00>Профиль привязан! ВК уведомления включены</color> ]";
                    vkTextAnchor = TextAnchor.MiddleCenter;
                    vkFontSize = 22;
                }

                if (!string.IsNullOrEmpty(playerData.UserPhone))
                {
                    phoneTitle = $"Введите код подтверждения:";
                    phoneInputCommand = $"rn accept {phoneInputText}";
                }

                if (playerData.EnabledPhone)
                {
                    Container.Add(new CuiElement
                    {
                        Name = MenuContent + ".Info.BackgroundPhone",
                        Parent = MenuContent + ".Info",
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#0000007f"),
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = $"25 -430",
                                OffsetMax = $"525 -380"
                            }
                        }
                    });

                    phoneTitle = $"Номер телефона привязан";
                    phoneTextAnchor = TextAnchor.MiddleCenter;
                    phoneFontSize = 24;
                }
            }    

            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info" + $".TitleVK",
                Parent = MenuContent + ".Info",
                Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = vkTitle,
                                    Align = vkTextAnchor,
                                    FontSize = vkFontSize,
                                    Font = "robotocondensed-regular.ttf",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"25 -355",
                                    OffsetMax = $"525 -305"
                                }
                            }
            });

            Container.Add(new CuiElement
            {
                Name = MenuContent + ".Info" + $".TitlePhone",
                Parent = MenuContent + ".Info",
                Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = phoneTitle,
                                    Align = phoneTextAnchor,
                                    FontSize = phoneFontSize,
                                    Font = "robotocondensed-regular.ttf",
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1",
                                    AnchorMax = "0 1",
                                    OffsetMin = $"25 -430",
                                    OffsetMax = $"525 -380"
                                }
                            }
            });

            if (playerData == null || !playerData.EnabledVK)
            {
                Container.Add(new CuiElement
                {
                    Name = MenuContent + ".Info.BackgroundVK",
                    Parent = MenuContent + ".Info",
                    Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#0000007f"),
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = $"325 -355",
                                OffsetMax = $"525 -305"
                            }
                        }
                });
                Container.Add(new CuiElement
                {
                    Name = MenuContent + ".Info.InputVK",
                    Parent = MenuContent + ".Info",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            CharsLimit = 64,
                            Text = vkInputText,
                            Command = vkInputCommand,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = $"325 -355",
                            OffsetMax = $"525 -305"
                        }
                    }
                });
            }

            if (playerData == null || !playerData.EnabledPhone)
            {
                Container.Add(new CuiElement
                {
                    Name = MenuContent + ".Info.BackgroundPhone",
                    Parent = MenuContent + ".Info",
                    Components =
                        {
                            new CuiImageComponent
                            {
                                Color = HexToRustFormat("#0000007f"),
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = $"325 -430",
                                OffsetMax = $"525 -380"
                            }
                        }
                });
                Container.Add(new CuiElement
                {
                    Name = MenuContent + ".Info.InputPhone",
                    Parent = MenuContent + ".Info",
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = TextAnchor.MiddleCenter,
                            CharsLimit = 64,
                            Text = phoneInputText,
                            Command = phoneInputCommand,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = $"325 -430",
                            OffsetMax = $"525 -380"
                        }
                    }
                });
            }
        }

        #region Utils
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return $"{color.r:F2} {color.g:F2} {color.b:F2} {color.a:F2}";
        }
        #endregion
    }
}