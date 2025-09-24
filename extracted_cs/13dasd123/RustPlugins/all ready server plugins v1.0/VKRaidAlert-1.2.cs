using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Globalization;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("VKRaidAlert", "SkiTles", "1.2")]
    class VKRaidAlert : RustPlugin
    {

        #region Vars
        [PluginReference]
        Plugin VKBot, Friends;

        DateTime LNT;

        List<string> allowedentity = new List<string>()
        {
            "door",
            "wall.window.bars.metal",
            "wall.window.bars.toptier",
            "wall.external",
            "gates.external.high",
            "floor.ladder",
            "embrasure",
            "floor.grill",
            "wall.frame.fence",
            "wall.frame.cell",
            "foundation",
            "floor.frame",
            "floor.triangle",
            "floor",
            "foundation.steps",
            "foundation.triangle",
            "roof",
            "stairs.l",
            "stairs.u",
            "wall.doorway",
            "wall.frame",
            "wall.half",
            "wall.low",
            "wall.window",
            "wall",
            "wall.external.high.stone"
        };
        #endregion

        #region Data
        class DataStorage
        {
            public Dictionary<ulong, VKRADATA> VKRaidAlertData = new Dictionary<ulong, VKRADATA>();
            public DataStorage() { }
        }
        class VKRADATA
        {
            public string LastOnlNotice;
        }
        DataStorage data;
        private DynamicConfigFile VKRData;
        void LoadData()
        {
            try
            {
                data = Interface.GetMod().DataFileSystem.ReadObject<DataStorage>("VKRaidAlert");
            }

            catch
            {
                data = new DataStorage();
            }
        }
        #endregion

        #region Config
        private string permissionvk = "vkraidalert.allow";
        private string permissionol = "vkraidalert.online";
        private int timeallowed = 30;
        private int tallowedol = 10;
        private string raidnotice = "[Оповещения о рейдах] Кажется игрок {attacker} хочет вас ограбить!";
        private bool MsgAttB = false;
        private string MsgAtt = "photo-1_265827614";
        private string raidnoticeol = "Кажется игрок {attacker} хочет вас ограбить!";
        private bool friendsalert = true;
        
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Создан новый файл конфигурации. Поддержи разработчика! Вступи в группу vk.com/vkbotrust");
            Config.Clear();
        }
        private void LoadConfigValues()
        {
            GetConfig("A. Привилегия для отправки оповещений оффлайн игрокам в ВК", ref permissionvk);
            GetConfig("B. Интервал оповещений оффлайн игрокам в ВК (в минутах)", ref timeallowed);
            GetConfig("C. Текст оповещения оффлайн игрокам в ВК", ref raidnotice);
            GetConfig("D. Прикрепить изображение к сообщению?", ref MsgAttB);
            GetConfig("E. Ссылка на изображение, пример: photo-1_265827614", ref MsgAtt);

            GetConfig("F. Привилегия для отправки оповещений онлайн игрокам в чат", ref permissionol);
            GetConfig("G. Интервал оповещений онлайн игрокам в чат (в минутах)", ref tallowedol);
            GetConfig("H. Текст оповещения онлайн игрокам в чат", ref raidnoticeol);
            GetConfig("I. Отправка оповещений друзьям (при наличии привилегии)", ref friendsalert);
            SaveConfig();
        }
        void Loaded()
        {
            LoadConfigValues();
            if (!plugins.Exists("VKBot"))
            {
                PrintWarning("VKBot не установлен! Оффлайн оповещения не работают без VKBot!");
            }
        }
        private void GetConfig<T>(string Key, ref T var)
        {
            if (Config[Key] != null)
            {
                var = (T)Convert.ChangeType(Config[Key], typeof(T));
            }
            Config[Key] = var;
        }
        #endregion

        #region OxideHooks
        void OnServerInitialized()
        {
            VKRData = Interface.Oxide.DataFileSystem.GetFile("VKRaidAlert");
            LoadData();
            if (!permission.PermissionExists(permissionvk)) permission.RegisterPermission(permissionvk, this);
            if (!permission.PermissionExists(permissionol)) permission.RegisterPermission(permissionol, this);
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
                    {
                        return;
                    }
                }
                else
                {
                    bool ok = false;
                    foreach (var ent in allowedentity)
                    {
                        if (entity.LookupPrefab().name.Contains(ent))
                        {
                            ok = true;
                        }
                    }
                    if (!ok) return;
                }
                if (entity.OwnerID == 0) return;                
                if (!IsOnline(entity.OwnerID))
                {
                    if (plugins.Exists("VKBot"))
                    {
                        SendOfflineMessage(entity.OwnerID, hitInfo.Initiator?.ToPlayer().displayName);                        
                    }                    
                }
                else
                {
                    var victimid = OnlinePlayer(entity.OwnerID);
                    if (victimid != null)
                    {
                        SendOnlinenotice(victimid, hitInfo.Initiator?.ToPlayer().displayName);
                    }
                }
                if (friendsalert && plugins.Exists("Friends"))
                {
                    var friends = Friends?.Call<ulong[]>("GetFriends", entity.OwnerID);
                    foreach (var friend in friends)
                    {
                        if (!IsOnline(friend))
                        {
                            if (plugins.Exists("VKBot"))
                            {
                                SendOfflineMessage(friend, hitInfo.Initiator?.ToPlayer().displayName);
                            }
                        }
                        else
                        {
                            var victimid = OnlinePlayer(friend);
                            if (victimid != null)
                            {
                                SendOnlinenotice(victimid, hitInfo.Initiator?.ToPlayer().displayName);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Helpers
        bool IsOnline(ulong id)
        {
            foreach (BasePlayer active in BasePlayer.activePlayerList)
            {
                if (active.userID == id) return true;
            }
            return false;
        }
        BasePlayer OnlinePlayer(ulong id)
        {
            foreach (BasePlayer active in BasePlayer.activePlayerList)
            {
                if (active.userID == id) return active;
            }
            return null;
        }
        TimeSpan TimeLeft(DateTime time)
        {
            return DateTime.Now.Subtract(time);
        }
        private string TextReplace(string key, params KeyValuePair<string, string>[] replacements)
        {
            string message = key;
            foreach (var replacement in replacements)
                message = message.Replace($"{{{replacement.Key}}}", replacement.Value);
            return message;
        }
        void Log(string filename, string text)
        {
            LogToFile(filename, $"[{DateTime.Now}] {text}", this);
        }
        #endregion

        #region SendAlerts
        private void SendOnlinenotice(BasePlayer victim, string raidername)
        {
            if (!permission.UserHasPermission(victim.userID.ToString(), permissionol)) return;
			Puts("ASD");
            if (data.VKRaidAlertData.ContainsKey(victim.userID))
            {
                if (DateTime.TryParseExact(data.VKRaidAlertData[victim.userID].LastOnlNotice, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out LNT))
                {
                    if (TimeLeft(LNT).TotalMinutes >= tallowedol)
                    {
                        string text = TextReplace(raidnoticeol,
                                        new KeyValuePair<string, string>("attacker", raidername));
                        PrintToChat(victim, text);
                        data.VKRaidAlertData[victim.userID].LastOnlNotice = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                        VKRData.WriteObject(data);
                    }
                }
                else
                {
                    Log("Log", $"Ошибка обработки времени последнего оповещения игрока {victim.userID.ToString()}");
                    return;
                }
            }
            else
            {
                string text = TextReplace(raidnoticeol,
                                        new KeyValuePair<string, string>("attacker", raidername));
                PrintToChat(victim, text);
                data.VKRaidAlertData.Add(victim.userID, new VKRADATA()
                {
                    LastOnlNotice = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")
                });
                VKRData.WriteObject(data);
            }
        }
        void SendOfflineMessage(ulong id, string raidername)
        {
            if (!permission.UserHasPermission(id.ToString(), permissionvk)) return;
            var userVK = (string)VKBot?.Call("GetUserVKId", id);
            if (userVK == null) return;
            var LastNotice = (string)VKBot?.Call("GetUserLastNotice", id);
            if (LastNotice == null)
            {
                string text = TextReplace(raidnotice,
                                            new KeyValuePair<string, string>("attacker", raidername));
                VKBot?.Call("VKAPIMsg", text, MsgAtt, userVK, MsgAttB);
                string LastRaidNotice = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                VKBot?.Call("VKAPISaveLastNotice", id, LastRaidNotice);
            }
            else
            {
                if (DateTime.TryParseExact(LastNotice, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out LNT))
                {
                    if (TimeLeft(LNT).TotalMinutes >= timeallowed)
                    {
                        string text = TextReplace(raidnotice,
                            new KeyValuePair<string, string>("attacker", raidername));
                        VKBot?.Call("VKAPIMsg", text, MsgAtt, userVK, MsgAttB);
                        string LastRaidNotice = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                        VKBot?.Call("VKAPISaveLastNotice", id, LastRaidNotice);
                    }
                }
                else
                {
                    Log("Log", $"Ошибка обработки времени последнего оповещения игрока {id}");
                    return;
                }
            }
        }
        #endregion      
    }
}