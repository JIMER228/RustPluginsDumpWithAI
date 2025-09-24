// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Oxide.Core;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using Oxide.Core.Configuration;
using System.Linq;
using System;
using Newtonsoft.Json;
using Facepunch.Steamworks;
using Oxide.Core.Plugins;
using System.Globalization;

namespace Oxide.Plugins
{
    [Info("Cases", "SkiTles", "1.1.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("Выдача игрокам кейсов с рандомным выпадением ценных предметов")]
    class Cases : RustPlugin
    {
        //Данный плагин принадлежит группе vk.com/vkbotrust
        //Данный плагин предоставляется в существующей форме,
        //"как есть", без каких бы то ни было явных или
        //подразумеваемых гарантий, разработчик не несет
        //ответственность в случае его неправильного использования.

        #region Variables
        private bool NewWipe = false;
        private static ConfigFile config;

        [PluginReference]
        Plugin Duel;
        #endregion

        #region Config
        private class ConfigFile
        {
            [JsonProperty(PropertyName = "Содержимое кейсов | Список команд")]
            public Dictionary<string, string> PresentCommands;

            [JsonProperty(PropertyName = "Содержимое кейсов | Список предметов")]
            public Dictionary<string, string> PresentItems;

            [JsonProperty(PropertyName = "Рандомный скин для предметов (true/false)")]
            public bool RandomSkin { get; set; }

            [JsonProperty(PropertyName = "Оповещения в общий чат о получении кейса (true/false)")]
            public bool CaseGiveNotify { get; set; }

            [JsonProperty(PropertyName = "Оповещения в общий чат о открытии кейса (true/false)")]
            public bool CaseOpenNotify { get; set; }

            [JsonProperty(PropertyName = "Интервал выдачи (минуты)")]
            public int TimeChecks { get; set; }

            [JsonProperty(PropertyName = "Минимальный онлайн для выдачи кейсов")]
            public int MinimalOnline { get; set; }

            [JsonProperty(PropertyName = "Минимальный онлайн игрока (минуты)")]
            public int MinOnlineWinnerMinutes { get; set; }

            [JsonProperty(PropertyName = "Максимальное количество кейсов на игрока")]
            public int MaxCaseToPlayer { get; set; }

            [JsonProperty(PropertyName = "Дата (для подсчета максимального числа кейсов на игрока, меняется автоматически)")]
            public string Today { get; set; }

            [JsonProperty(PropertyName = "SkinID кейса")]
            public ulong CaseSkin { get; set; }

            public static ConfigFile DefaultConfig()
            {
                return new ConfigFile
                {
                    PresentCommands = new Dictionary<string, string>()
                    {
                        ["homerecycler.add {steamid}"] = "домашний переработчик",
                        ["grantperm {steamid} gathercontrol.vip 3d"] = "рейты x3 (3 дня)",
                        ["addgroup {steamid} premplayers 4d"] = "выживший (4 дня)",
                        ["mtools.give mpick {steamid}"] = "магическую кирку",
                        ["grantperm {steamid} vkraidalert.allow 3d"] = "оповещение о рейде на 3 дня",
                        ["mtools.give mhatchet {steamid}"] = "магический топор"
                    },
                    PresentItems = new Dictionary<string, string>()
                    {
                        ["ammo.rocket.basic"] = "ракету",
                        ["ammo.rocket.fire"] = "зажигательную ракету",
                        ["attire.reindeer.headband"] = "шапочку оленя",
                        ["autoturret"] = "турель",
                        ["door.double.hinged.toptier"] = "двойную армированную дверь",
                        ["explosive.satchel"] = "бич пакет",
                        ["explosive.timed"] = "С4",
                        ["fridge"] = "холодильник",
                        ["rifle.ak"] = "калаш",
                        ["rifle.lr300"] = "Lr300",
                        ["rocket.launcher"] = "ракетницу",
                        ["scarecrow"] = "пугало",
                        ["shotgun.pump"] = "помповый дробовик",
                        ["smg.2"] = "СМГ",
                        ["supply.signal"] = "сигнальную гранату",
                        ["workbench3"] = "верстак 3-го уровня",
                        ["xmas.lightstring"] = "гирлянду",
                        ["ammo.rocket.smoke"] = "дымовую ракету",
                        ["fun.guitar"] = "гитару",
                        ["lmg.m249"] = "пулемет М249",
                        ["metal.facemask"] = "металлическую маску",
                        ["rifle.bolt"] = "болтовку",
                        ["shotgun.double"] = "двуствольный дробовик",
                        ["smg.thompson"] = "томпсон",
                        ["pistol.semiauto"] = "пистолет",
                        ["hoodie"] = "толстовку",
                        ["shoes.boots"] = "ботинки",
                        ["box.wooden.large"] = "большой ящик",
                        ["door.hinged.toptier"] = "армированную дверь",
                        ["pistol.revolver"] = "револьвер",
                        ["target.reactive"] = "мишень",
                        ["rifle.semiauto"] = "берданку",
                        ["vending.machine"] = "торговый автомат",
                        ["locker"] = "шкафчик",
                        ["wall.frame.garagedoor"] = "гаражную дверь",
                        ["furnace"] = "печку"
                    },
                    RandomSkin = true,
                    CaseGiveNotify = true,
                    CaseOpenNotify = true,
                    TimeChecks = 40,
                    MinimalOnline = 5,
                    MinOnlineWinnerMinutes = 30,
                    MaxCaseToPlayer = 1,
                    Today = DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
      //  Слив плагинов server-rust by Apolo YouGame
                    CaseSkin = 1297033185
                };
            }
        }
        protected override void LoadDefaultConfig()
        {
            config = ConfigFile.DefaultConfig();
            PrintWarning("Создан новый файл конфигурации. Поддержи разработчика! Вступи в группу vk.com/vkbotrust");
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigFile>();
                if (config == null)
                    Regenerate();
            }
            catch { Regenerate(); }
        }
        protected override void SaveConfig() => Config.WriteObject(config);
        private void Regenerate()
        {
            PrintWarning($"Конфигурационный файл 'oxide/config/{Name}.json' поврежден, создается новый...");
            LoadDefaultConfig();
        }
        #endregion

        #region OXIDE HOOKS
        void OnServerInitialized()
        {
            ItSkinsData = Interface.Oxide.DataFileSystem.GetFile("Cases/CasesSkinsList");
            LoadData();
            if (NewWipe || ItemSkins.ItemSkinsData.Count == 0) webrequest.Enqueue("http://s3.amazonaws.com/s3.playrust.com/icons/inventory/rust/schema.json", null, ReadScheme, this);
            if (NewWipe) { caseslist.Clear(); csList.WriteObject(caseslist); }
            timer.Repeat(config.TimeChecks * 60, 0, () =>
            {
                GetWinner();
            });
            if (DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) != config.Today)
      //  Слив плагинов server-rust by Apolo YouGame
            {
                config.Today = DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
      //  Слив плагинов server-rust by Apolo YouGame
                Config.WriteObject(config, true);
                reciverslist.Clear();
                reciversList.WriteObject(reciverslist);
            }
        }
        void OnNewSave(string filename)
        {
            NewWipe = true;
        }
        void Loaded()
        {
            LoadMessages();
        }
        #endregion

        #region Main
        [ConsoleCommand("givecase")]
        private void GiveCaseCMD(ConsoleSystem.Arg arg)
        {
            //Синтаксис команды givecase steamid
            if (arg.IsAdmin != true) { return; }
            if (arg.Args == null) return;
            string[] args = arg.Args;
            if (args.Length == 1)
            {
                if (args.ElementAtOrDefault(0) == "random")
                {
                    BasePlayer randomman = BasePlayer.activePlayerList.GetRandom();
                    CaseToPlayer(randomman);
                }
                ulong playerid = 0;
                ulong.TryParse(args.ElementAtOrDefault(0), out playerid);
                if (playerid == 0) return;
                BasePlayer reciver = BasePlayer.FindByID(playerid);
                if (!reciver.IsConnected) return;
                CaseToPlayer(reciver);
            }
        }
        [ConsoleCommand("givecaseall")]
        private void GiveCaseAllCMD(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) { return; }
            foreach (var player in BasePlayer.activePlayerList)
            {
                CaseToPlayer(player);
            }
        }
        private void GetWinner()
        {
            if (BasePlayer.activePlayerList.Count >= config.MinimalOnline)
            {
                List<BasePlayer> challengers = new List<BasePlayer>();
                foreach (var player in BasePlayer.activePlayerList)
                {
                    bool Duelist = false;
                    if (plugins.Exists("Duel"))
                    {
                        Duelist = (bool)Duel?.Call("IsDuelPlayer", player);
                    }
                    if (Duelist)
                    {
                        continue;
                    }
                    if (player.net.connection.GetSecondsConnected() >= config.MinOnlineWinnerMinutes * 60)
                    {
                        challengers.Add(player);
                    }
                }
                if (challengers.Count > 0)
                {
                    var Winner = challengers.GetRandom();
                    if (reciverslist.ContainsKey(Winner.userID))
                    {
                        if (reciverslist[Winner.userID] < config.MaxCaseToPlayer)
                        {
                            reciverslist[Winner.userID]++;
                            reciversList.WriteObject(reciverslist);
                            CaseToPlayer(Winner);
                            if (config.CaseGiveNotify) Server.Broadcast(string.Format(GetMsg("ПолучилРандомныйКейс", Winner), Winner.displayName));
                        }
                    }
                    else
                    {
                        reciverslist.Add(Winner.userID, 1);
                        reciversList.WriteObject(reciverslist);
                        CaseToPlayer(Winner);
                        if (config.CaseGiveNotify) Server.Broadcast(string.Format(GetMsg("ПолучилРандомныйКейс", Winner), Winner.displayName));
                    }
                }
            }
        }
        private void CaseToPlayer(BasePlayer player)
        {
            Item newcase = ItemManager.CreateByItemID(-1622660759, 1, config.CaseSkin);
            caseslist.Add(newcase.uid);
            csList.WriteObject(caseslist);
            player.GiveItem(newcase);
            SendReply(player, string.Format(GetMsg("ИгрокПолучилКейс", player)));
            Puts($"Игрок {player.displayName} ({player.userID}) получил кейс.");
            Log("give", $"Игрок {player.displayName} ({player.userID}) получил кейс.");
        }
        private object OnItemAction(Item item, string action)
        {
            if (item == null || action == null || action == "")
                return null;
            if (item.info.itemid != -1622660759)
                return null;
            if (item.skin != config.CaseSkin)
                return null;
            if (!caseslist.Contains(item.uid))
                return null;
            if (action != "unwrap")
                return null;
            BasePlayer player = item.GetRootContainer().GetOwnerPlayer();
            if (player == null)
                return null;
            item.RemoveFromContainer();
            item.Remove();
            caseslist.Remove(item.uid);
            csList.WriteObject(caseslist);
            GivePresent(player);
            Effect.server.Run("assets/prefabs/misc/xmas/presents/effects/unwrap.prefab", player.transform.position);
            return false;
        }
        private void GivePresent(BasePlayer player)
        {
            var choice1 = Random.Range(1, 100);
            if (choice1 >= 85)
            {
                var choice2 = Random.Range(1, config.PresentCommands.Count) - 1;
                rust.RunServerCommand(config.PresentCommands.ElementAt(choice2).Key.ToString().Replace("{steamid}", player.UserIDString));
                Log("opens", $"Игрок {player.displayName} ({player.userID}) открыл кейс и получил {config.PresentCommands.ElementAt(choice2).Value}");
                Puts($"Игрок {player.displayName} ({player.userID}) открыл кейс и получил {config.PresentCommands.ElementAt(choice2).Value}");
                if (config.CaseOpenNotify) Server.Broadcast(string.Format(GetMsg("ОткрылКейс", player), player.displayName, config.PresentCommands.ElementAt(choice2).Value));
            }
            if (choice1 < 85)
            {
                ulong skinid = 0;
                var choice2 = Random.Range(1, config.PresentItems.Count) - 1;
                string shortname = config.PresentItems.ElementAt(choice2).Key;
                if (config.RandomSkin && ItemSkins.ItemSkinsData.ContainsKey(shortname))
                {
                    ulong.TryParse(ItemSkins.ItemSkinsData[shortname].skins.GetRandom(), out skinid);
                    if (skinid == 0) Puts("Не удалось получить ид скина из файла");
                }
                Item gift = ItemManager.CreateByName(shortname, 1, skinid);
                //player.GiveItem(gift);
                gift.MoveToContainer(player.inventory.containerMain);
                if (skinid != 0)
                {
                    Puts($"Игрок {player.displayName} ({player.userID}) открыл кейс и получил {config.PresentItems.ElementAt(choice2).Value}. Скин - steamcommunity.com/sharedfiles/filedetails/?id={gift.skin}");
                    Log("opens", $"Игрок {player.displayName} ({player.userID}) открыл кейс и получил {config.PresentItems.ElementAt(choice2).Value}. Скин - steamcommunity.com/sharedfiles/filedetails/?id={gift.skin}");
                }
                else
                {
                    Puts($"Игрок {player.displayName} ({player.userID}) открыл кейс и получил {config.PresentItems.ElementAt(choice2).Value}");
                    Log("opens", $"Игрок {player.displayName} ({player.userID}) открыл кейс и получил {config.PresentItems.ElementAt(choice2).Value}");
                }
                if (config.CaseOpenNotify) Server.Broadcast(string.Format(GetMsg("ОткрылКейс", player), player.displayName, config.PresentItems.ElementAt(choice2).Value));
            }
        }
        [ConsoleCommand("loadskinslist")]
        private void LoadWorkshopSkins(ConsoleSystem.Arg arg)
        {
            if (arg.IsAdmin != true) { return; }
            webrequest.Enqueue("http://s3.amazonaws.com/s3.playrust.com/icons/inventory/rust/schema.json", null, ReadScheme, this);
        }
        #endregion

        #region GetSkinsList
        private void ReadScheme(int code, string response)
        {
            if (response != null && code == 200)
            {
                var schema = JsonConvert.DeserializeObject<Rust.Workshop.ItemSchema>(response);
                var defs = new List<Inventory.Definition>();
                int newskins = 0;
                foreach (var item in schema.items)
                {
                    if (string.IsNullOrEmpty(item.itemshortname)) continue;
                    if (string.IsNullOrEmpty(item.workshopdownload)) continue;
                    if (string.IsNullOrEmpty(item.workshopid)) continue;
                    if (ItemSkins.ItemSkinsData.ContainsKey(item.itemshortname))
                    {
                        if (!ItemSkins.ItemSkinsData[item.itemshortname].skins.Contains(item.workshopid))
                        {
                            ItemSkins.ItemSkinsData[item.itemshortname].skins.Add(item.workshopid);
                            newskins++;
                        }
                    }
                    else
                    {
                        ItemSkins.ItemSkinsData.Add(item.itemshortname, new ISkins()
                        {
                            skins = new List<string>
                            {
                                item.workshopid
                            }
                        });
                        newskins++;
                    }
                }
                if (newskins > 0) Puts($"Добавлено новых скинов - {newskins}");
                ItSkinsData.WriteObject(ItemSkins);
            }
            else
            {
                PrintWarning($"Failed to load approved workshop skins... Error {code}");
            }
        }
        #endregion

        #region Promocodes
        [ConsoleCommand("addpromo")]
        private void PromoADD(ConsoleSystem.Arg arg)
        {
            //Синтаксис команды addpromo code
            if (arg.IsAdmin != true) { return; }
            if (arg.Args == null) return;
            string[] args = arg.Args;
            if (args.Length == 1)
            {
                var pcode = args.ElementAtOrDefault(0);
                promolist.Add(pcode);
                promoList.WriteObject(promolist);
                PrintWarning($"Промокод {pcode} добавлен.");
            }
        }

        [ChatCommand("promo")]
        private void UsePromo(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 1)
            {
                SendReply(player, string.Format(GetMsg("Промокод", player)));
                return;
            }
            var pcode = args.ElementAt(0);
            if (!promolist.Contains(pcode))
            {
                SendReply(player, string.Format(GetMsg("НеверныйПромокод", player)));
                return;
            }
            promolist.Remove(pcode);
            promoList.WriteObject(promolist);
            PrintWarning($"Игрок {player.displayName} активировал промокод {pcode}");
            CaseToPlayer(player);
        }
        #endregion

        #region Data
        DynamicConfigFile csList = Interface.Oxide.DataFileSystem.GetFile("Cases/CasesList");
        List<uint> caseslist;
        DynamicConfigFile promoList = Interface.Oxide.DataFileSystem.GetFile("Cases/CasesPromoList");
        List<string> promolist;
        DynamicConfigFile reciversList = Interface.Oxide.DataFileSystem.GetFile("Cases/CasesReciversList");
        Dictionary<ulong, int> reciverslist;
        void LoadData()
        {
            caseslist = csList.ReadObject<List<uint>>() ?? new List<uint>();
            promolist = promoList.ReadObject<List<string>>() ?? new List<string>();
            reciverslist = reciversList.ReadObject<Dictionary<ulong, int>>() ?? new Dictionary<ulong, int>();
            try
            {
                ItemSkins = Interface.GetMod().DataFileSystem.ReadObject<DataStorage3>("Cases/CasesSkinsList");
            }

            catch
            {
                ItemSkins = new DataStorage3();
            }
        }
        class DataStorage3
        {
            public Dictionary<string, ISkins> ItemSkinsData = new Dictionary<string, ISkins>();
            public DataStorage3() { }
        }
        class ISkins
        {
            public List<string> skins;
        }
        DataStorage3 ItemSkins;
        private DynamicConfigFile ItSkinsData;
        #endregion

        #region Helpers
        void Log(string filename, string text)
        {
            LogToFile(filename, $"[{DateTime.Now}] {text}", this);
        }
        #endregion

        #region Lang
        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"ПолучилРандомныйКейс", "<size=17>Игрок <color=#049906>{0}</color> получил кейс\nКейс выдается рандомно одному из игроков. Возможно следующий счастливчик именно ты!</size>"},
                {"ИгрокПолучилКейс", "<size=17>Поздравляем! Вы получили кейс, из которого можно получить ценные предметы! Не потеряйте его!</size>"},
                {"ОткрылКейс", "<size=17>Игрок <color=#049906>{0}</color> открыл кейс и получил <color=#049906>{1}</color>.</size>"},
                {"Промокод", "<size=17>Введите промокод. Пример: 'promo 123'. Промокоды можно найти в нашей группе ВК</size>"},
                {"НеверныйПромокод", "<size=17>Такого промокода не существует, или он уже был использован. Промокоды можно найти в нашей группе ВК</size>"}
            }, this);
        }
        string GetMsg(string key, BasePlayer player = null) => GetMsg(key, player.UserIDString);
        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());
        #endregion
    }
}
