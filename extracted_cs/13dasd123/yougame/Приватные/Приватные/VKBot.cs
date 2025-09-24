using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("VKBot", "SkiTles", "1.4")]
    class VKBot : RustPlugin
    {
        private System.Random random = new System.Random();
        private string opc;
        private string stw;
        private string sts;
        private string str;
        private string stb;
        private string ste;
        private string msg;
        private string mapfile;
        private bool NewWipe = false;
        private string VkID = "Заполните эти поля, и выполните команду o.reload VKBot";
        private string VKToken = "Заполните эти поля, и выполните команду o.reload VKBot";
        private string VKTokenApp = "Заполните эти поля, и выполните команду o.reload VKBot";
        private string GroupID = "Заполните эти поля, и выполните команду o.reload VKBot";
        private bool SendReports = false;
        private bool UpdateStatus = true;
        private string EmojiCounterList = "onlinecounter, rocketscounter, blueprintsconter, explosivecounter";
        private string StatusText = "{usertext}. Онлайн игроков: {onlinecounter}. Добыто дерева: {woodcounter}. Добыто серы: {sulfurecounter}. Выпущено ракет: {rocketscounter}. Использовано взрывчатки: {explosivecounter}. Создано чертежей: {blueprintsconter}. {connect}";
        private string connecturl = "Заполните эти поля, и выполните команду o.reload VKBot";
        private string StatusUT = "Сервер 1 Max 3";
        private int UpdateTimer = 30;
        private bool WPostB = true;
        private string WPostMsg = "Заполните эти поля, и выполните команду o.reload VKBot";
        private bool WPostAttB = true;
        private string WPostAtt = "Заполните эти поля, и выполните команду o.reload VKBot, не нужно заполнять если нет оповещения";
        private bool WPostMsgAdmin = true;
        private bool UserBannedMsg = false;
        private bool VKGroupGifts = true;
        private bool VKGGNotify = true;
        private int VKGGTimer = 30;
        public Dictionary<string, object> VKGroupGiftList = new Dictionary<string, object>
            {
                {"supply.signal", 1},
                {"pookie.bear", 2}
            };
        private string VKGroupUrl = "vk.com/1234";
        private bool GiftsBool = true;

        private void Init()
        {
            cmd.AddChatCommand("report", this, "SendReport");
            cmd.AddChatCommand("vk", this, "VKcommand");
            cmd.AddConsoleCommand("updatestatus", this, "UStatus");
            cmd.AddConsoleCommand("sendmsgadmin", this, "MsgAdmin");
        }
        class DataStorageStats
        {
            public int WoodGath;
            public int SulfureGath;
            public int Rockets;
            public int Blueprints;
            public int Explosive;
            public DataStorageStats() { }
        }
        class DataStorageUsers
        {
            public Dictionary<ulong, VKUDATA> VKUsersData = new Dictionary<ulong, VKUDATA>();
            public DataStorageUsers() { }
        }
        class VKUDATA
        {
            public ulong UserID;
            public string Name;
            public string VkID;
            public int ConfirmCode;
            public bool Confirmed;
            public bool GiftRecived;
            public string LastRaidNotice;
        }
        DataStorageStats statdata;
        DataStorageUsers usersdata;
        private DynamicConfigFile VKBData;
        private DynamicConfigFile StatData;
        void OnServerInitialized()
        {
            VKBData = Interface.Oxide.DataFileSystem.GetFile("VKBotUsers");
            StatData = Interface.Oxide.DataFileSystem.GetFile("VKBot");
            LoadData();
            if (NewWipe)
            {
                WipeAlerts(mapfile);
            }
        }
        protected override void LoadDefaultConfig()
        {
            Puts("Creating a new config file");
            Config.Clear();
            PrintWarning("");
        }
        private void LoadConfigValues()
        {
            GetConfig("VkID администратора", ref VkID);
            GetConfig("VK Token группы (для сообщений)", ref VKToken);
            GetConfig("VK Token приложения (для записей на стене и статуса)", ref VKTokenApp);
            GetConfig("VKID группы", ref GroupID);
            GetConfig("Включить отправку сообщений администратору командой /report ?", ref SendReports);
            GetConfig("Обновлять статус в группе? Если стоит /false/ статистика собираться не будет", ref UpdateStatus);
            GetConfig("Список счетчиков, которые будут отображаться в виде emoji", ref EmojiCounterList);
            GetConfig("Формат статуса", ref StatusText);
            GetConfig("Ссылка на коннект сервера вида /connect 111.111.111.11:11111/", ref connecturl);
            GetConfig("Текст для статуса", ref StatusUT);
            GetConfig("Таймер обновления статуса (минуты)", ref UpdateTimer);
            GetConfig("Отправлять пост в группу после вайпа?", ref WPostB);
            GetConfig("Текст поста о вайпе", ref WPostMsg);
            GetConfig("Добавить изображение к посту о вайпе?", ref WPostAttB);
            GetConfig("Ссылка на изображение к посту о вайпе вида 'photo-1_265827614'", ref WPostAtt);
            GetConfig("Отправлять сообщение администратору о вайпе?", ref WPostMsgAdmin);
            GetConfig("Отправлять сообщение администратору о бане игрока?", ref UserBannedMsg);
            GetConfig("Выдавать подарок игроку за вступление в группу ВК?", ref VKGroupGifts);
            GetConfig("Подарки за вступление в группу (shortname предмета, количество)", ref VKGroupGiftList);
            GetConfig("Ссылка на группу ВК", ref VKGroupUrl);
            GetConfig("Оповещения в общий чат о получении награды", ref GiftsBool);
            GetConfig("Включить оповещения для игроков не получивших награду за вступление в группу?", ref VKGGNotify);
            GetConfig("Интервал оповещений для игроков не получивших награду за вступление в группу (в минутах)", ref VKGGTimer);
            SaveConfig();
        }
        void Loaded()
        {
            LoadConfigValues();
            if (UpdateStatus)
            {
                timer.Repeat(UpdateTimer * 60, 0, Update);
            }
            if (VKGGNotify)
            {
                timer.Repeat(VKGGTimer * 60, 0, GiftNotifier);
            }
        }
        void Unload()
        {
            if (UpdateStatus)
            {
                StatData.WriteObject(statdata);
            }
        }
        void OnNewSave(string filename)
        {
            NewWipe = true;
            mapfile = filename;
        }
        private void WipeAlerts(string filename)
        {
            if (UpdateStatus)
            {
                statdata.Blueprints = 0;
                statdata.Rockets = 0;
                statdata.SulfureGath = 0;
                statdata.WoodGath = 0;
                statdata.Explosive = 0;
                StatData.WriteObject(statdata);
                NewWipe = false;
                Update();
            }
            if (WPostMsgAdmin)
            {
                string s = filename;
                string[] array = s.Split('/');
                int t = array.Length - 1;
                string savename = array[t];
                string[] mapname = savename.Split('.');
                string msg = $"[VKBot] Сервер вайпнут. Установлена карта: {mapname[0]}. Размер: {mapname[1]}. Сид: {mapname[2]}";
                SendVkMessage(VkID, msg);
            }
            if (WPostB)
            {
                if (WPostAttB)
                {
                    SendVkWall($"{WPostMsg}&attachments={WPostAtt}");
                }
                else
                {
                    SendVkWall($"{WPostMsg}");
                }
            }
        }
        #region VKAPI
        private void MsgAdmin(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) { return; }
            if (arg.Args == null)
            {
                PrintWarning($"Текст сообщения отсутсвует, правильная команда |sendmsgadmin сообщение|. Message missing, use command |sendmsgadmin message|");
                return;
            }
            string[] args = arg.Args;
            if (args.Length > 0)
            {
                string text = string.Join(" ", args.Skip(0).ToArray());
                SendVkMessage(VkID, text);
                Log("Log", $"Отправлено новое сообщение пользователю администратору: ({text})");
            }
        }
        string GetUserVKId(ulong userid)
        {
            if (usersdata.VKUsersData.ContainsKey(userid) && usersdata.VKUsersData[userid].Confirmed)
            {
                return usersdata.VKUsersData[userid].VkID;
            }
            else
            {
                return null;
            }
        }
        string GetUserLastNotice(ulong userid)
        {
            if (usersdata.VKUsersData.ContainsKey(userid) && usersdata.VKUsersData[userid].Confirmed)
            {
                return usersdata.VKUsersData[userid].LastRaidNotice;
            }
            else
            {
                return null;
            }
        }
        string AdminVkID()
        {
            return VkID;
        }
        private void VKAPISaveLastNotice(ulong userid, string lasttime)
        {
            if (usersdata.VKUsersData.ContainsKey(userid))
            {
                usersdata.VKUsersData[userid].LastRaidNotice = lasttime;
                VKBData.WriteObject(usersdata);
            }
            else
            {
                return;
            }
        }
        private void VKAPIWall(string text, string attachments, bool atimg)
        {
            if (atimg)
            {
                SendVkWall($"{text}&attachments={attachments}");
                Log("Log", $"Отправлен новый пост на стену: ({text}&attachments={attachments})");
            }
            else
            {
                SendVkWall($"{text}");
                Log("Log", $"Отправлен новый пост на стену: ({text})");
            }
        }
        private void VKAPIMsg(string text, string attachments, string reciverID, bool atimg)
        {
            if (atimg)
            {
                SendVkMessage(reciverID, $"{text}&attachment={attachments}");
                Log("Log", $"Отправлено новое сообщение пользователю {reciverID}: ({text}&attachments={attachments})");
            }
            else
            {
                SendVkMessage(reciverID, $"{text}");
                Log("Log", $"Отправлено новое сообщение пользователю {reciverID}: ({text})");
            }
        }
        private void VKAPIStatus(string msg)
        {
            SendVkStatus(msg);
            Log("Log", $"Отправлен новый статус: {msg}");
        }
        #endregion

        #region VKfunction
        private string EmojiCounters(string counter)
        {
            var chars = counter.ToCharArray();
            string emoji = "";
            for (int ctr = 0; ctr < chars.Length; ctr++)
            {
                string replace = chars[ctr] + "⃣";
                emoji = emoji + replace;
            }
            return emoji;
        }
        private void UStatus(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) { return; }
            if (UpdateStatus)
            {
                Update();
            }
            else
            {
                PrintWarning($"Функция обновления статуса отключена. Status update disabled");
            }
        }
        void OnPlayerBanned(string name, ulong id, string address, string reason)
        {
            if (UserBannedMsg)
            {
                string msg = $"Игрок {name} ({id}) был забанен на сервере. Причина: {reason}. Ссылка на профиль стим: steamcommunity.com/profiles/{id}/";
                if (usersdata.VKUsersData.ContainsKey(id) && usersdata.VKUsersData[id].Confirmed)
                {
                    msg = msg + $" . Ссылка на профиль ВК: vk.com/id{usersdata.VKUsersData[id].VkID}";
                }
                SendVkMessage(VkID, msg);
            }
        }
        private void SendReport(BasePlayer player, string cmd, string[] args)
        {
            if (SendReports)
            {
                if (args.Length > 0)
                {
                    string text = string.Join(" ", args.Skip(0).ToArray());
                    msg = "[VKBot] " + player.displayName + "(" + player.UserIDString + ") написал сообщение: " + text;
                    string reciverID = VkID;
                    SendVkMessage(reciverID, msg);
                    Log("Log", $"{player.displayName} ({player.userID}): написал администратору: {msg}");
                    PrintToChat(player, String.Format($"<size=17>Ваше сообщение было отправлено администратору.</size>"));
                    PrintToChat(player, String.Format($"<size=17><color=#990404>ВНИМАНИЕ!</color> Наличие в тексте нецензурных выражений, оскорблений администрации или игроков сервера, а так же большое количество безсмысленных сообщений <color=#990404>приведет к бану!</color></size>"));
                }
                else
                {
                    PrintToChat(player, String.Format($"<size=17>Введите команду <color=#990404>/report сообщение</color> . Текст сообщения будет отправлен администратору.</size>"));
                    PrintToChat(player, String.Format($"<size=17><color=#990404>ВНИМАНИЕ!</color> Наличие в тексте нецензурных выражений, оскорблений администрации или игроков сервера, а так же большое количество безсмысленных сообщений <color=#990404>приведет к бану!</color></size>"));
                    return;
                }
            }
            else
            {
                PrintToChat(player, String.Format("Данная функция отключена администратором."));
            }
        }
        private void CheckVkUser(BasePlayer player, string url)
        {
            string Userid = null;
            string[] arr1 = url.Split('/');
            int num = arr1.Length - 1;
            string vkname = arr1[num];
            string url2 = "https://api.vk.com/method/users.get?user_ids=" + vkname + "&v=5.68&access_token=" + VKToken;
            webrequest.Enqueue(url2, null, (code, response) => {
                Log("responce", response);
                var json = JObject.Parse(response);
                Userid = (string)json["response"][0]["id"];
                if (Userid != null)
                {
                    AddVKUser(player, Userid);
                }
                else
                {
                    PrintToChat(player, "Ошибка обработки вашей ссылки ВК, обратитесь к администратору.");
                }
                }, this);
        }
        private void AddVKUser(BasePlayer player, string Userid)
        {
            if (!usersdata.VKUsersData.ContainsKey(player.userID))
            {
                usersdata.VKUsersData.Add(player.userID, new VKUDATA()
                {
                    UserID = player.userID,
                    Name = player.displayName,
                    VkID = Userid,
                    ConfirmCode = random.Next(1, 9999999),
                    Confirmed = false,
                    GiftRecived = false
                });
                VKBData.WriteObject(usersdata);
                SendVkMessage(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /vk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}");
                PrintToChat(player, String.Format($"<size=17>Вам был отправлен код подтверждения. Если сообщение не пришло, зайдите в группу <color=#990404>{VKGroupUrl}</color> и напишите любое сообщение. После этого введите команду <color=#990404>/vk confirm</color></size>"));
            }
            else
            {
                if (Userid == usersdata.VKUsersData[player.userID].VkID) { PrintToChat(player, String.Format($"<size=17>Вы уже подтвердили свой профиль.</size>")); return; }
                usersdata.VKUsersData[player.userID].Name = player.displayName;
                usersdata.VKUsersData[player.userID].VkID = Userid;
                usersdata.VKUsersData[player.userID].Confirmed = false;
                usersdata.VKUsersData[player.userID].ConfirmCode = random.Next(1, 9999999);
                VKBData.WriteObject(usersdata);
                SendVkMessage(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /vk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}");
                PrintToChat(player, String.Format($"<size=17>Вам был отправлен код подтверждения. Если сообщение не пришло, зайдите в группу <color=#990404>{VKGroupUrl}</color> и напишите любое сообщение. После этого введите команду <color=#990404>/vk confirm</color></size>"));
            }
        }
        private void VKcommand(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "add")
                {
                    if (args.Length == 1) { PrintToChat(player, String.Format($"<size=17>Список доступных команд: \n /vk add ссылка на вашу страницу - добавление вашего профиля ВК в базу</size>")); return; }
                    CheckVkUser(player, args[1]);
                }
                if (args[0] == "confirm")
                {
                    if (args.Length >= 2)
                    {
                        if (usersdata.VKUsersData.ContainsKey(player.userID))
                        {
                            if (usersdata.VKUsersData[player.userID].Confirmed) { PrintToChat(player, String.Format($"<size=17>Вы уже подтвердили свой профиль.</size>")); return; }
                            if (args[1] == usersdata.VKUsersData[player.userID].ConfirmCode.ToString())
                            {
                                usersdata.VKUsersData[player.userID].Confirmed = true;
                                VKBData.WriteObject(usersdata);
                                PrintToChat(player, String.Format($"<size=17>Вы подтвердили свой профиль! Спасибо!</size>"));
                                if (VKGroupGifts) { PrintToChat(player, String.Format($"<size=17>Вы можете получить награду, если вступили в нашу группу <color=#990404>{VKGroupUrl}</color> введя команду <color=#990404>/vk gift</color></size>")); }
                            }
                            else
                            {
                                PrintToChat(player, String.Format($"<size=17>Неверный код подтверждения.</size>"));
                            }
                        }
                        else
                        {
                            PrintToChat(player, String.Format($"<size=17>Сначала добавьте свой VK ID командой <color=#990404>/vk add ссылка на вашу страницу</color></size>"));
                        }
                    }
                    else
                    {
                        if (usersdata.VKUsersData[player.userID].Confirmed) { PrintToChat(player, String.Format($"<size=17>Вы уже подтвердили свой профиль.</size>")); return; }
                        if (usersdata.VKUsersData.ContainsKey(player.userID))
                        {
                            SendVkMessage(usersdata.VKUsersData[player.userID].VkID, $"Для подтверждения вашего ВК профиля введите в игровой чат команду /vk confirm {usersdata.VKUsersData[player.userID].ConfirmCode}");
                            PrintToChat(player, String.Format($"<size=17>Вам был отправлен код подтверждения. Если сообщение не пришло, зайдите в группу <color=#990404>{VKGroupUrl}</color> и напишите любое сообщение. После этого введите команду <color=#990404>/vk confirm</color></size>"));
                        }
                        else
                        {
                            PrintToChat(player, String.Format($"<size=17>Сначала добавьте свой VK ID командой <color=#990404>/vk add ссылка на вашу страницу</color></size>"));
                        }
                    }
                }
                if (args[0] == "gift")
                {
                    if (VKGroupGifts)
                    {
                        if (!usersdata.VKUsersData.ContainsKey(player.userID)) { PrintToChat(player, String.Format($"<size=17>Сначала добавьте свой VK ID командой <color=#990404>/vk add ссылка на вашу страницу</color></size>")); return; }
                        if (!usersdata.VKUsersData[player.userID].Confirmed) { PrintToChat(player, String.Format($"<size=17>Сначала подтвердите свой профиль ВК командой <color=#990404>/vk confirm</color></size>")); return; }
                        if (usersdata.VKUsersData[player.userID].GiftRecived) { PrintToChat(player, String.Format($"<size=17>Вы уже получили свою награду.</size>")); return; }
                        string url = $"https://api.vk.com/method/groups.isMember?group_id={GroupID}&user_id={usersdata.VKUsersData[player.userID].VkID}&v=5.68&access_token={VKToken}";
                        webrequest.Enqueue(url, null, (code, response) => {
                            var json = JObject.Parse(response);
                            string Result = (string)json["response"];
                            GetGift(code, Result, player);
                        }, this);
                    }
                    else
                    {
                        PrintToChat(player, String.Format("Данная функция отключена администратором."));
                    }
                }
                if (args[0] != "add" && args[0] != "gift" && args[0] != "confirm")
                {
                    PrintToChat(player, String.Format($"<size=17>Список доступных команд: \n /vk add ссылка на вашу страницу - добавление вашего профиля ВК в базу. \n /vk confirm - подтверждение вашего профиля ВК</size>"));
                    if (VKGroupGifts)
                    {
                        PrintToChat(player, String.Format($"<size=17>/vk gift - получение награды за вступление в группу <color=#990404>{VKGroupUrl}</color></size>"));
                    }
                }
            }
            else
            {
                PrintToChat(player, String.Format($"<size=17>Список доступных команд: \n /vk add ссылка на вашу страницу - добавление вашего профиля ВК в базу. \n /vk confirm - подтверждение вашего профиля ВК</size>"));
                if (VKGroupGifts)
                {
                    PrintToChat(player, String.Format($"<size=17>/vk gift - получение награды за вступление в группу <color=#990404>{VKGroupUrl}</color></size>"));
                }
            }
        }
        private void GetGift(int code, string Result, BasePlayer player)
        {
            if (Result == "1")
            {
                int FreeSlots = 24 - player.inventory.containerMain.itemList.Count;
                if (FreeSlots >= VKGroupGiftList.Count)
                {
                    for (int i = 0; i < VKGroupGiftList.Count; i++)
                    {
                        Item gift = ItemManager.CreateByName(VKGroupGiftList.ElementAt(i).Key, Convert.ToInt32(VKGroupGiftList.ElementAt(i).Value));
                        gift.MoveToContainer(player.inventory.containerMain, -1, false);
                    }
                    usersdata.VKUsersData[player.userID].GiftRecived = true;
                    VKBData.WriteObject(usersdata);
                    PrintToChat(player, String.Format($"<size=17>Вы получили свою награду! Проверьте инвентарь!</size>"));
                    if (GiftsBool)
                    {
                        Server.Broadcast(String.Format($"<size=17>Игрок <color=#990404>{player.displayName}</color> получил награду за вступление в группу <color=#990404>{VKGroupUrl}.</color> \n Хочешь тоже получить награду? Введи в чат команду <color=#990404>/vk gift</color>.</size>"));
                    }
                }
                else
                {
                    PrintToChat(player, String.Format($"<size=17>Недостаточно места для получения награды.</size>"));
                }
            }
            else
            {
                PrintToChat(player, String.Format($"<size=17>Вы не являетесь участником группы <color=#990404>{VKGroupUrl}</color></size>"));
            }
        }
        private void GiftNotifier()
        {
            if (VKGroupGifts)
            {
                foreach (var pl in BasePlayer.activePlayerList)
                {
                    if (!usersdata.VKUsersData.ContainsKey(pl.userID))
                    {
                        PrintToChat(pl, String.Format($"<size=17>Вступите в нашу группу <color=#990404>{VKGroupUrl}</color> и получите награду! \n Введите команду <color=#990404>/vk gift</color></size>"));
                    }
                    else
                    {
                        if (!usersdata.VKUsersData[pl.userID].GiftRecived)
                        {
                            PrintToChat(pl, String.Format($"<size=17>Вступите в нашу группу <color=#990404>{VKGroupUrl}</color> и получите награду! \n Введите команду <color=#990404>/vk gift</color></size>"));
                        }
                    }
                }
            }
        }
        private void OnItemResearchStart(ResearchTable table)
        {
            statdata.Blueprints++;
        }
        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (UpdateStatus)
            {
                if (item.info.shortname == "wood")
                {
                    statdata.WoodGath = statdata.WoodGath + item.amount;
                }
                if (item.info.shortname == "sulfur.ore")
                {
                    statdata.SulfureGath = statdata.SulfureGath + item.amount;
                }
            }
        }
        private void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (UpdateStatus && item.info.shortname == "sulfur.ore")
            {
                statdata.SulfureGath = statdata.SulfureGath + item.amount;
            }
        }
        private void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (UpdateStatus)
            {
                if (item.info.shortname == "wood")
                {
                    statdata.WoodGath = statdata.WoodGath + item.amount;
                }
                if (item.info.shortname == "sulfur.ore")
                {
                    statdata.SulfureGath = statdata.SulfureGath + item.amount;
                }
            }
        }
        private void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            if (UpdateStatus)
            {
                statdata.Rockets++;
            }
        }
        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if (UpdateStatus)
            {
                List<object> include = new List<object>()
                {
                "explosive.satchel.deployed",
                "grenade.f1.deployed",
                "grenade.beancan.deployed",
                "explosive.timed.deployed"
                };
                if (include.Contains(entity.ShortPrefabName))
                {
                    statdata.Explosive++;
                }
            }
        }
        void Update()
        {
            StatData.WriteObject(statdata);
            List<ulong> OnlinePlayers = new List<ulong>();
            foreach (var pl in BasePlayer.activePlayerList)
            {
                OnlinePlayers.Add(pl.userID);
            }
            if (EmojiCounterList.Contains("onlinecounter"))
            {
                opc = EmojiCounters(OnlinePlayers.Count.ToString());
            }
            else
            {
                opc = OnlinePlayers.Count.ToString();
            }
            if (EmojiCounterList.Contains("woodcounter"))
            {
                stw = EmojiCounters(statdata.WoodGath.ToString());
            }
            else
            {
                stw = statdata.WoodGath.ToString();
            }
            if (EmojiCounterList.Contains("sulfurecounter"))
            {
                sts = EmojiCounters(statdata.SulfureGath.ToString());
            }
            else
            {
                sts = statdata.SulfureGath.ToString();
            }
            if (EmojiCounterList.Contains("rocketscounter"))
            {
                str = EmojiCounters(statdata.Rockets.ToString());
            }
            else
            {
                str = statdata.Rockets.ToString();
            }
            if (EmojiCounterList.Contains("blueprintsconter"))
            {
                stb = EmojiCounters(statdata.Blueprints.ToString());
            }
            else
            {
                stb = statdata.Blueprints.ToString();
            }
            if (EmojiCounterList.Contains("explosivecounter"))
            {
                ste = EmojiCounters(statdata.Explosive.ToString());
            }
            else
            {
                ste = statdata.Explosive.ToString();
            }
            string cu = connecturl;
            string su = StatusUT;
            SendStatus(StatusText,
                new KeyValuePair<string, string>("onlinecounter", opc),
                new KeyValuePair<string, string>("woodcounter", stw),
                new KeyValuePair<string, string>("sulfurecounter", sts),
                new KeyValuePair<string, string>("rocketscounter", str),
                new KeyValuePair<string, string>("blueprintsconter", stb),
                new KeyValuePair<string, string>("explosivecounter", ste),
                new KeyValuePair<string, string>("connect", cu),
                new KeyValuePair<string, string>("usertext", su));
        }
        private void SendStatus(string key, params KeyValuePair<string, string>[] replacements)
        {
            string message = key;
            foreach (var replacement in replacements)
                message = message.Replace($"{{{replacement.Key}}}", replacement.Value);
            SendVkStatus(message);
        }
        #endregion
        void LoadData()
        {
            try
            {
                statdata = Interface.GetMod().DataFileSystem.ReadObject<DataStorageStats>("VKBot");
                usersdata = Interface.GetMod().DataFileSystem.ReadObject<DataStorageUsers>("VKBotUsers");
            }

            catch
            {
                statdata = new DataStorageStats();
                usersdata = new DataStorageUsers();
            }
        }
        void Log(string filename, string text)
        {
            LogToFile(filename, $"[{DateTime.Now}] {text}", this);
        }
        private void GetConfig<T>(string Key, ref T var)
        {
            if (Config[Key] != null)
            {
                var = (T)Convert.ChangeType(Config[Key], typeof(T));
            }
            Config[Key] = var;
        }
        private void SendVkMessage(string reciverID, string msg)
        {
            string type = "message";
            string url = "https://api.vk.com/method/messages.send?user_ids=" + reciverID + "&message=" + msg + "&v=5.68&access_token=" + VKToken;
            webrequest.Enqueue(url, null, (code, response) => GetCallback(code, response, type), this);
        }
        private void SendVkWall(string msg)
        {
            string type = "post";
            string url = "https://api.vk.com/method/wall.post?owner_id=-" + GroupID + "&message=" + msg + "&from_group=1&v=5.68&access_token=" + VKTokenApp;
            webrequest.Enqueue(url, null, (code, response) => GetCallback(code, response, type), this);
        }
        private void SendVkStatus(string msg)
        {
            string type = "status";
            string url = "https://api.vk.com/method/status.set?group_id=" + GroupID + "&text=" + msg + "&v=5.68&access_token=" + VKTokenApp;
            webrequest.Enqueue(url, null, (code, response) => GetCallback(code, response, type), this);
        }
        void GetCallback(int code, string response, string type)
        {
            if (!response.Contains("error"))
            {
                Puts($"New {type} sended: {response}");
            }
            else
            {
                PrintWarning($"{type} not sended. Error logs: /oxide/logs/VKBot/");
                Log("Errors", $"{type} not sended. Error: {response}");

            }
        }
    }
}