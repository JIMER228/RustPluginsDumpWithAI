using System;
using System.Collections.Generic;
using Oxide.Core;
using System.Globalization;
using Oxide.Core.Configuration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("RaidAlert", "Partinzan", "1.0.1")]
    class RaidAlert : RustPlugin
    {
        #region Variables

        private string tokenGroup = "8f09b3811b58974b2a3a48b9a184d5389dca316b2724ea0a4d3af185b9095415cc33033d2e39c80e07b6d"; // Токен группы
        private int timeAllowed = 30; // Интервал оповещений оффлайн игрокам в ВК (в минутах)
        private bool messagePhoto = false; // Прикрепить изображение к сообщению?
        private string messageAttrib = "photo-167913710_456239050"; // Ссылка на изображение, пример: photo-1_265827614
        private string messageOffline = "Кажется игрок {0} хочет вас ограбить! Срочно заходите в игру!"; // Текст оповещения оффлайн игрокам в ВК
        private string messageOnline = "Кажется игрок {0} хочет вас ограбить! Срочно возвращайтесь домой!"; // Текст оповещения онлайн игрокам в чат

        private List<string> allowedEntity = new List<string>
        {
			"foundation",
			"foundation.triangle",
			"floor",
			"floor.triangle",
			"floor.grill",
            "wall",
            "wall.doorway",
            "wall.window",
            "wall.external",
            "gates.external.high",
            "wall.frame.fence",
            "wall.frame.cell",
        };

        #endregion

        #region DataStorage

        DateTime LNT;
        class DataStorage
        {
            public Dictionary<ulong, RAIDDATA> RaidAlertData = new Dictionary<ulong, RAIDDATA>();
            public DataStorage() { }
        }

        class RAIDDATA
        {
            public ulong UserID;
            public string Name;
            public string VkID;
            public int ConfirmCode;
            public bool Confirmed;
            public string LastRaidNotice;
        }

        DataStorage data;
        private DynamicConfigFile RaidData;

        #endregion

        #region Oxide Hooks

        void LoadData()
        {
            try
            {
                data = Interface.GetMod().DataFileSystem.ReadObject<DataStorage>("RaidAlert");
            }

            catch
            {
                data = new DataStorage();
            }
        }

        void OnServerInitialized()
        {
            RaidData = Interface.Oxide.DataFileSystem.GetDatafile("RaidAlert");
            LoadData();
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (data.RaidAlertData.ContainsKey(player.userID))
            {
                if (data.RaidAlertData[player.userID].Name != player.displayName)
                {
                    data.RaidAlertData[player.userID].Name = player.displayName;
                    RaidData.WriteObject(data);
                }
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (hitInfo == null) return;
            if (hitInfo.Initiator?.ToPlayer() == null) return;
            if (hitInfo.Initiator?.ToPlayer().userID == entity.OwnerID) return;
            if (hitInfo.damageTypes.GetMajorityDamageType() != Rust.DamageType.Explosion && hitInfo.damageTypes.GetMajorityDamageType() != Rust.DamageType.Heat && hitInfo.damageTypes.GetMajorityDamageType() != Rust.DamageType.Bullet) return;
            if (entity is BaseEntity)
            {
                BuildingBlock block = entity.GetComponent<BuildingBlock>();
                if (block != null)
                {
                    if (block.currentGrade.gradeBase.type.ToString() == "Twigs" || block.currentGrade.gradeBase.type.ToString() == "Wood")
                        return;
                }
                else
                {
                    bool ok = false;
                    foreach (var ent in allowedEntity)
                    {
                        if (entity.LookupPrefab().name.Contains(ent))
                            ok = true;
                    }
                    if (!ok) return;
                }
                if (entity.OwnerID == 0) return;

                if (!IsOnline(entity.OwnerID))
                {
                    SendOfflineMessage(entity.OwnerID, hitInfo.Initiator?.ToPlayer().displayName);
                }
                else
                {
                    BasePlayer player = FindOnlinePlayer(entity.OwnerID.ToString());
                    PrintToChat(player, string.Format(messageOnline, hitInfo.Initiator?.ToPlayer().displayName));
                    return;
                }
            }
        }

        #endregion

        #region Helpers

        private void CheckVkUser(BasePlayer player, string urls)
        {
            string userId = null;
            int confirmCode = UnityEngine.Random.Range(1000, 9999);
            string[] array = urls.Split('/');
            string url = $"https://api.vk.com/method/users.get?user_ids={array[array.Length - 1]}&v=5.71&fields=bdate&access_token={tokenGroup}";
            webrequest.EnqueueGet(url, (code, response) => 
            {
                if (!response.Contains("error"))
                {
                    var json = JObject.Parse(response);
                    userId = (string)json["response"][0]["id"];
                    string bdate = "noinfo";
                    bdate = (string)json["response"][0]["bdate"];
                    if (userId != null)
                        AddVKUser(player, userId, confirmCode, bdate);
                    else PrintToChat(player, "Ошибка обработки вашей ссылки ВК, обратитесь к администратору.");
                }
            }, this);
        }

        private void AddVKUser(BasePlayer player, string Userid, int Code, string bdate)
        {
            if (!data.RaidAlertData.ContainsKey(player.userID))
            {
                data.RaidAlertData.Add(player.userID, new RAIDDATA()
                {
                    UserID = player.userID,
                    Name = player.displayName,
                    VkID = Userid,
                    ConfirmCode = Code,
                    Confirmed = false
                });
                RaidData.WriteObject(data);
                SendConfCode(data.RaidAlertData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /vk confirm {data.RaidAlertData[player.userID].ConfirmCode}", player);
            }
            else
            {
                if (Userid == data.RaidAlertData[player.userID].VkID && data.RaidAlertData[player.userID].Confirmed) { PrintToChat(player, "Вы уже добавили и подтвердили свой профиль."); return; }
                if (Userid == data.RaidAlertData[player.userID].VkID && !data.RaidAlertData[player.userID].Confirmed) { PrintToChat(player, "Вы уже добавили свой профиль. Если вам не пришел код подтверждения, введите команду <color=#049906>/vk confirm</color>"); return; }

                data.RaidAlertData[player.userID].Name = player.displayName;
                data.RaidAlertData[player.userID].VkID = Userid;
                data.RaidAlertData[player.userID].ConfirmCode = Code;
                data.RaidAlertData[player.userID].Confirmed = false;
                RaidData.WriteObject(data);
                SendConfCode(data.RaidAlertData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /vk confirm {data.RaidAlertData[player.userID].ConfirmCode}", player);
            }
        }

        private void SendConfCode(string reciverID, string msg, BasePlayer player)
        {
            string url = $"https://api.vk.com/method/messages.send?user_ids={reciverID}&message={msg}&v=5.71&access_token={tokenGroup}";
            webrequest.EnqueueGet(url, (code, response) => GetCallbackConfCode(code, response, "Сообщение", player), this);
        }

        void GetCallback(int code, string response, string type)
        {
            if (!response.Contains("error"))
                Puts($"{type} отправлен(о): {response}");
        }

        void GetCallbackConfCode(int code, string response, string type, BasePlayer player)
        {
            if (!response.Contains("error"))
                Puts($"{type} отправлен(о): {response}");
        }

        bool IsOnline(ulong id)
        {
            foreach (BasePlayer active in BasePlayer.activePlayerList)
            {
                if (active.userID == id) return true;
            }
            return false;
        }

        TimeSpan TimeLeft(DateTime time)
        {
            return DateTime.Now.Subtract(time);
        }

        public static BasePlayer FindOnlinePlayer(string nameOrIdOrIp)
        {
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.userID.ToString() == nameOrIdOrIp)
                    return activePlayer;
                if (activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.OrdinalIgnoreCase))
                    return activePlayer;
                if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress == nameOrIdOrIp)
                    return activePlayer;
            }
            return null;
        }

        void SendOfflineMessage(ulong id, string raidername)
        {
            string raidDate = DateTime.Now.ToString("HH:mm:ss");
            var userVK = GetUserVKId(id);
            if (userVK == null) return;
            var LastNotice = GetUserLastNotice(id);
            if (LastNotice == null)
            {
                VKAPIMsg(string.Format(messageOffline, raidername, raidDate), messageAttrib, userVK, messagePhoto);
                string LastRaidNotice = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                VKAPISaveLastNotice(id, LastRaidNotice);
            }
            else
            {
                if (DateTime.TryParseExact(LastNotice, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out LNT))
                {
                    if (TimeLeft(LNT).TotalMinutes >= timeAllowed)
                    {
                        VKAPIMsg(string.Format(messageOffline, raidername, raidDate), messageAttrib, userVK, messagePhoto);
                        string LastRaidNotice = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                        VKAPISaveLastNotice(id, LastRaidNotice);
                    }
                }
            }
        }

        #endregion

        #region ChatCommand

        [ChatCommand("vk")]
        private void VKcommand(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length == 0)
            {
                PrintToChat(player, "Список доступных команд:\n /vk add ссылка на вашу страницу - добавление вашего профиля ВК в базу.\n /vk confirm - подтверждение вашего профиля ВК");
                return;
            }
            if (args.Length > 0)
            {
                if (args[0] == "add")
                {
                    if (args.Length == 1)
                    {
                        PrintToChat(player, "Список доступных команд:\n /vk add ссылка на вашу страницу - добавление вашего профиля ВК в базу.\n /vk confirm - подтверждение вашего профиля ВК");
                        return;
                    }
                    if (!args[1].Contains("vk.com/"))
                    {
                        PrintToChat(player, "Ссылка на страницу должна быть вида |vk.com/testpage| или |vk.com/id0000|");
                        return;
                    }
                    PrintToChat(player, "Запрос отравлен!");
                    CheckVkUser(player, args[1]);
                }
                if (args[0] == "confirm")
                {
                    if (args.Length >= 2)
                    {
                        if (data.RaidAlertData.ContainsKey(player.userID))
                        {
                            if (data.RaidAlertData[player.userID].Confirmed)
                            {
                                PrintToChat(player, "Вы уже добавили и подтвердили свой профиль.");
                                return;
                            }
                            if (args[1] == data.RaidAlertData[player.userID].ConfirmCode.ToString())
                            {
                                data.RaidAlertData[player.userID].Confirmed = true;
                                RaidData.WriteObject(data);
                                PrintToChat(player, "Вы подтвердили свой профиль! Спасибо!");
                            }
                            else PrintToChat(player, "Неверный код подтверждения.");
                        }
                        else PrintToChat(player, "Сначала добавьте и подтвердите свой профиль командой <color=#049906>/vk add ссылка на вашу страницу.</color> Ссылка должна быть вида |vk.com/testpage| или |vk.com/id0000|");
                    }
                    else
                    {
                        if (!data.RaidAlertData.ContainsKey(player.userID)) { PrintToChat(player, "Сначала добавьте и подтвердите свой профиль командой <color=#049906>/vk add ссылка на вашу страницу.</color> Ссылка на должна быть вида |vk.com/testpage| или |vk.com/id0000|"); return; }
                        if (data.RaidAlertData[player.userID].Confirmed) { PrintToChat(player, "Вы уже добавили и подтвердили свой профиль."); return; }
                        if (data.RaidAlertData.ContainsKey(player.userID))
                        {
                            SendVkMessage(data.RaidAlertData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /vk confirm {data.RaidAlertData[player.userID].ConfirmCode}");
                            PrintToChat(player, "Вам был отправлен код подтверждения. Если сообщение не пришло, зайдите в группу и напишите любое сообщение. После этого введите команду <color=#049906>/vk confirm</color>");
                        }
                    }
                }
            }
        }

        #endregion

        #region VKAPI

        private void SendVkMessage(string reciverID, string msg)
        {
            if (msg.Contains("#"))
                msg = msg.Replace("#", "%23");
            
            string url = $"https://api.vk.com/method/messages.send?user_ids={reciverID}&message={msg}&v=5.71&access_token={tokenGroup}";
            webrequest.EnqueueGet(url, (code, response) => GetCallback(code, response, "Сообщение"), this);
        }

        string GetUserVKId(ulong userid)
        {
            if (data.RaidAlertData.ContainsKey(userid) && data.RaidAlertData[userid].Confirmed)
            {
                return !ServerUsers.BanListString().Contains(userid.ToString()) ? data.RaidAlertData[userid].VkID : null;
            }
            
            return null;
        }

        string GetUserLastNotice(ulong userid)
        {
            if (data.RaidAlertData.ContainsKey(userid) && data.RaidAlertData[userid].Confirmed)
                return data.RaidAlertData[userid].LastRaidNotice;

            return null;
        }

        private void VKAPISaveLastNotice(ulong userid, string lasttime)
        {
            if (data.RaidAlertData.ContainsKey(userid))
            {
                data.RaidAlertData[userid].LastRaidNotice = lasttime;
                RaidData.WriteObject(data);
            }
            else return;
        }

        private void VKAPIMsg(string text, string attachments, string reciverID, bool atimg)
        {
            if (atimg)
            {
                SendVkMessage(reciverID, $"{text}&attachment={attachments}");
                Puts($"Отправлено новое сообщение пользователю {reciverID}: ({text}&attachments={attachments})");
            }
            else
            {
                SendVkMessage(reciverID, $"{text}");
                Puts($"Отправлено новое сообщение пользователю {reciverID}: ({text})");
            }
        }

        #endregion
    }
}