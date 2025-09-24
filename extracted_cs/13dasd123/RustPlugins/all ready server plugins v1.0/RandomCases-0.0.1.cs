using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Diagnostics;
// Reference: System.Drawing

namespace Oxide.Plugins
{
    [Info("Random Cases", "server-rust.ru", "0.0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    public class RandomCases : RustPlugin
    {

        #region Classes
        [PluginReference]
        Plugin Duel;

        private bool IsDuelPlayer(BasePlayer player)
        {
            if (Duel == null)
                return false;
            var dueler = Duel.Call("IsPlayerOnActiveDuel", player);
            if (dueler is bool)
                return (bool)dueler;
            return false;
        }

        static RandomCases instance;

        public class CaseDefinition
        {
            public string Type;
            public string Name;
            public string Images;
            public string Description;
            public int CoolDown;
            public List<CaseItem> Items;

            public CaseItem Open() => Items.GetRandom();
        }

        public class CaseItem
        {
            public string Shortname;
            public int Min;
            public int Max;
            public int GetRandom() => UnityEngine.Random.Range(Min, Max + 1);
        }

        public class Case
        {
            public string Type;
            public int CoolDown;
            public int Amount;

            public CaseDefinition info()
            {
                if (!instance.cases.ContainsKey(Type))
                {
                    Interface.Oxide.LogWarning($"[{nameof(RandomCases)}] TYPE '{Type}' not contains in the Dictionary");
                    return null;

                }
                return instance.cases[Type];
            }
        }

        public class CasePlayer
        {
            public List<Case> CasesQueue = new List<Case>();
            public List<string> Inventory = new List<string>();
            public bool giftcases = false;
            public void OnTimer(ulong steamid, int delay)
            {
                List<Case> remove = new List<Case>();
                for (var i = CasesQueue.Count - 1; i >= 0; i--)
                {
                    var c = CasesQueue[i];
                    c.CoolDown -= delay;
                    if (c.Amount <= 0)
                    {
                        remove.Add(c);
                        continue;
                    }
                    if (c.CoolDown <= 0)
                    {
                        if (instance.GiveCase(steamid, c.Type, c))
                            remove.Add(c);
                    }
                }
                remove.ForEach(c => CasesQueue.Remove(c));
            }
        }

        public Dictionary<string, CaseDefinition> cases = new Dictionary<string, CaseDefinition>();
        public Dictionary<ulong, CasePlayer> players = new Dictionary<ulong, CasePlayer>();

        const int TIMEOUT = 60;
        bool init = false;

        #endregion

        #region Configuration
        string cmdChatCommand = "case";
        bool enabledGiftCase = false;
        string giftcase = "casename";
        int giftcaseAmount = 5;
        string imagesServerLogo = "https://i.imgur.com/FSGvggs.png";

        private void LoadConfigValues()
        {
            bool changed = false;
            if (GetConfig("Основные настройки", "Чатовая команда открытия меню кейсов", ref cmdChatCommand))
            {
                PrintWarning("Благодарим за скачивание плагина на сайте Server-rust.ru Если вы приобрели этот плагин на другом ресурсе знайте - вы лох который потратил деньги зря)");
                changed = true;
            }
            if (GetConfig("Основные настройки", "Ссылка на логотип сервера, для вывода в окно кейсов", ref imagesServerLogo))
            {
                changed = true;
            }
            if (GetConfig("Бонусный кейс", "Выдавать игроку при первом посещении бонусный кейс?", ref enabledGiftCase))
            {
                changed = true;
            }
            if (GetConfig("Бонусный кейс", "Название кейса какой выдавать игроку", ref giftcase))
            {
                changed = true;
            }
            if (GetConfig("Бонусный кейс", "Количество кейсов выдаваемых игроку (Игрок получает 1 кейс, а остаток выдаеться в N количество времени указаного в data кейса)", ref giftcaseAmount))
            {
                changed = true;
            }
            if (changed)
                SaveConfig();
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
        #endregion

        #region Oxide


        void OnServerInitialized()
        {
            instance = this;
            LoadData();
            LoadConfig();
            LoadConfigValues();
            cmd.AddChatCommand(cmdChatCommand, this, cmdChatCase);
            lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
            timer.Every(TIMEOUT, TimerHandle);
            InitFileManager();
            CommunityEntity.ServerInstance.StartCoroutine(m_FileManager.LoadFile("Serverlogo", imagesServerLogo));
            foreach (var check in cases)
            {
                CommunityEntity.ServerInstance.StartCoroutine(m_FileManager.LoadFile($"Icon{check.Value.Type}", check.Value.Images));
            }
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                OnPlayerInit(player);
            }
        }

        void SaveData()
        {
            players_File.WriteObject(players);
        }

        void Unload()
        {
            SaveData();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "randomcase_menu");
                CuiHelper.DestroyUi(player, "randomcases_give");
            }
        }

        void OnServerSave()
        {
            SaveData();
        }

        void OnPlayerInit(BasePlayer player)
        {
            CasePlayer casePlayer;
            if (!players.TryGetValue(player.userID, out casePlayer))
            {
                casePlayer = new CasePlayer();
                players.Add(player.userID, casePlayer);
            }
            if (enabledGiftCase)
            {
                if (!casePlayer.giftcases)
                {
                    GiveCaseGift(player.userID, giftcase, giftcaseAmount);
                }
            }
            PlayerCheck(player);
        }

        void PlayerCheck(BasePlayer player, int Current = 0, Case ccase2 = null)
        {
            CasePlayer casePlayer;
            if (!players.TryGetValue(player.userID, out casePlayer))
            {
                casePlayer = new CasePlayer();
                players.Add(player.userID, casePlayer);
            }
            var reply = 748;
            var x = new Stopwatch();
            x.Start();
            Dictionary<string, int> casesCollapsed = new Dictionary<string, int>();

            foreach (var ccase in casePlayer.Inventory)
            {
                if (!casesCollapsed.ContainsKey(ccase))
                    casesCollapsed[ccase] = 0;
                casesCollapsed[ccase]++;
            }
            Dictionary<string, int> casesQueue = new Dictionary<string, int>();
            foreach (var ccase1 in casePlayer.CasesQueue)
            {
                if (!casesQueue.ContainsKey(ccase1.Type))
                    casesQueue[ccase1.Type] = 0;
                casesQueue[ccase1.Type]++;
            }
            int Error = 0;
            List<string> QueInventory = new List<string>();
            foreach (var check in casesQueue)
            {
                if (!cases.ContainsKey(check.Key))
                {
                    QueInventory.Add(check.Key);
                    continue;
                }
            }
            foreach (var remove in QueInventory)
            {
                var caseQueue = ccase2 ?? casePlayer.CasesQueue.Find(c => c.Type == remove);
                casePlayer.CasesQueue.Remove(caseQueue);
                continue;
            }
            List<string> Inventory = new List<string>();
            foreach (var check in casesCollapsed)
            {
                foreach (var players1 in check.Key)
                {
                    if (!cases.ContainsKey(check.Key))
                    {
                        Inventory.Add(check.Key);
                        Error += check.Value;
                    }
                }
            }

            foreach (var remove in Inventory)
            {
                casePlayer.Inventory.Remove(remove);
                Error--;
                continue;
            }
            if (Error > 0)
            {
                PlayerCheck(player, Current);
            }
            x.Stop();
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            CasePlayer casePlayer;
            if (players.TryGetValue(player.userID, out casePlayer) && casePlayer.Inventory.Count > 0)
                SendReply(player, Messages["isHaveCases"]);
        }
        #endregion

        #region COMMANDS

        [ConsoleCommand("casegive")]
        void cmdRandomCaseGive(ConsoleSystem.Arg arg)
        {
            BasePlayer player;
            if (arg.Args == null || arg.Args.Length < 3)
            {
                Puts("Используйте: casegive STEAMID CASE AMOUNT");
                return;
            }
            if (arg.Connection != null)
            {
                player = arg.Connection.player as BasePlayer;
                if (player != null && player.net.connection.authLevel != 2) return;
            }
            var uid = ulong.Parse(arg.Args[0]);
            CasePlayer casePlayer;
            if (!players.TryGetValue(uid, out casePlayer))
            {
                casePlayer = new CasePlayer();
                players.Add(uid, casePlayer);
            }

            for (int i = 1; i < arg.Args.Length; i += 2)
            {
                var type = arg.Args[i];
                var amount = int.Parse(arg.Args[2]);
                Case cCase = casePlayer.CasesQueue.FirstOrDefault(c => c.Type == type);
                if (cCase == null)
                {
                    cCase = new Case() { Amount = amount, Type = type, CoolDown = 0 };
                    casePlayer.CasesQueue.Add(cCase);
                }
                else cCase.Amount += amount;
                GiveCase(uid, type, null, amount);
            }
        }

        [ConsoleCommand("casedrop")]
        void cmdDropUser(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            ulong userId = arg.GetUInt64(0);

            CasePlayer casePlayer;

            if (arg.Args == null || arg.Args.Length != 1)
            {
                Puts("Используйте: casedrop STEAMID");
                return;
            }
            if (!players.TryGetValue(userId, out casePlayer))
            {
                Puts("У игрока нет кейсов! Или игрок не найден");
                return;
            }

            var commandMsg = $"casegive {userId}";

            foreach (var ccase in casePlayer.Inventory)
                commandMsg += $" \"{ccase}\" 1";
            foreach (var ccase in casePlayer.CasesQueue)
                commandMsg += $" \"{ccase.Type}\" {ccase.Amount - 1}";
            players.Remove(userId);
            Puts($"Очищены кейсы {userId}\nДля переноса используйте следующую команду:\n{commandMsg}");
        }

        void cmdChatCase(BasePlayer player, string cmd, string[] args)
        {
            if (!init)
            {
                SendReply(player, Messages["plInit"]);
                return;
            }
            CasePlayer casePlayer;
            if (players.TryGetValue(player.userID, out casePlayer))
            {
                int i1 = 0;
                string getCases = "";
                foreach (var ccase in casePlayer.CasesQueue)
                {
                    string name = cases[ccase.Type].Name;
                    getCases = getCases + "Кейсов " + name + " - Осталось: " + ccase.Amount + $"\nСледующий через {FormatTime(TimeSpan.FromSeconds(ccase.CoolDown))}" + "\n";
                    i1++;
                    if (i1 > ccase.Amount) break;
                }
                Puts("1");
                SendReply(player, getCases);
            }
            DrawCases(player, 1);
        }

        [ConsoleCommand("nextpage.case2")]
        void cmdCloseMenu1(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null) return;
            var player = arg.Player();
            DrawCases(player, 2);
        }

        [ConsoleCommand("nextpage.case1")]
        void cmdCloseMenu0(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null) return;
            var player = arg.Player();
            DrawCases(player, 1);
        }

        [ConsoleCommand("randomcase.open")]
        void cmdOpenCase(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null) return;
            var player = arg.Player();
            var ccase = arg.Args[0];
            DrawCaseInfo(player, ccase);
      //  Слив плагинов server-rust by Apolo YouGame
        }

        [ConsoleCommand("openplayercase")]
        void cmdOpenCaseTest(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null) return;
            var player = arg.Player();
            var ccase = arg.Args[0];
            var amount = arg.Args[1];

            OpenCase(player, ccase);
            DrawCases(player, 1);
            if (amount != "1")
            {
                DrawCaseInfo(player, ccase);
      //  Слив плагинов server-rust by Apolo YouGame
            }
        }


        [ConsoleCommand("casesmenuclose")]
        void cmdCloseMenu(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null) return;
            var player = arg.Player();
            CuiHelper.DestroyUi(player, "randomcase_menu");
            CuiHelper.DestroyUi(player, "casescontainer");
        }

        #endregion

        #region CORE

        void SendReply(BasePlayer player, string msg)
        {
            base.SendReply(player, $"{msg}");
        }

        void TimerHandle()
        {
            foreach (var rCase in players)
                rCase.Value.OnTimer(rCase.Key, TIMEOUT);
            SaveData();
        }

        bool GiveCase(ulong userID, string ccase, Case ccase1 = null, int amount = 0)
        {
            CasePlayer casePlayer;
            if (players.TryGetValue(userID, out casePlayer))
            {
                var caseQueue = ccase1 ?? casePlayer.CasesQueue.Find(c => c.Type == ccase);
                if (caseQueue == null) return false;
                caseQueue.Amount--;
                if (caseQueue.info() == null)
                {
                    casePlayer.CasesQueue.Remove(caseQueue);
                    return false;
                }
                caseQueue.CoolDown = caseQueue.info().CoolDown;
                casePlayer.Inventory.Add(ccase);
                var player = BasePlayer.FindByID(userID);

                Puts($"{(player == null ? userID.ToString() : player.displayName)} получил кейс {ccase} в размере {amount} шт.");
                if (player != null)
                    SendReply(player, Messages["givePlayerCase"], ccase);

                if (caseQueue.Amount == 0)
                {
                    instance.Puts($"{(player == null ? userID.ToString() : player.displayName)} перестал получать кейс с {ccase}");
                    return true;
                }

            }
            return false;
        }

        void GiveCaseGift(ulong userID, string ccase, int amount)
        {
            CasePlayer casePlayer;
            if (!players.TryGetValue(userID, out casePlayer))
            {
                casePlayer = new CasePlayer();
                players.Add(userID, casePlayer);
            }

            Case cCase = casePlayer.CasesQueue.FirstOrDefault(c => c.Type == ccase);
            if (cCase == null)
            {
                cCase = new Case() { Amount = amount, Type = ccase, CoolDown = 0 };
                casePlayer.CasesQueue.Add(cCase);
            }

            else cCase.Amount += amount;
            casePlayer.giftcases = true;
            SaveData();
            GiveCase(userID, ccase);

        }

        bool OpenCase(BasePlayer player, string ccase)
        {
            CasePlayer casePlayer;
            if (players.TryGetValue(player.userID, out casePlayer)
                && casePlayer.Inventory.Contains(ccase))
            {
                if (!CanTake(player))
                {
                    SendReply(player, Messages["invFull"]);
                    return false;
                }
                if (IsDuelPlayer(player))
                {
                    return false;
                }
                casePlayer.Inventory.Remove(ccase);
                var item = cases[ccase].Items.GetRandom();
                var amount = item.GetRandom();
                player.inventory.GiveItem(ItemManager.CreateByName(item.Shortname, amount, 0));
                var x = ItemManager.CreateByPartialName(item.Shortname);
                SendReply(player, Messages["openCases"], cases[ccase].Name, x.info.displayName.english, amount);
                SaveData();
                return true;
            }
            return false;
        }

        bool CanTake(BasePlayer player) => !player.inventory.containerMain.IsFull() || !player.inventory.containerBelt.IsFull();

        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result += $"{Format(time.Days, "дней", "дня", "день")} ";

            if (time.Hours != 0)
                result += $"{Format(time.Hours, "часов", "часа", "час")} ";

            if (time.Minutes != 0)
                result += $"{Format(time.Minutes, "минут", "минуты", "минута")} ";

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
        #endregion

        #region UI

        string GUI = "[{\"name\":\"randomcase_menu\",\"parent\":\"Hud\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.1647059 0.1254902 0.3372549 0.8527624\"},{\"type\":\"NeedsCursor\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.1 0.2591146\",\"anchormax\":\"0.9 0.8541666\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"randomcase_list\",\"parent\":\"randomcase_menu\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0 0 0 0.301601\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.3934355\",\"anchormax\":\"0.998 0.8973742\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"randomcase.title_bp\",\"parent\":\"randomcase_menu\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0 0.3803922 0.4 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.917\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"randomcase.title.text\",\"parent\":\"randomcase.title_bp\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{title}\",\"fontSize\":30,\"align\":\"MiddleCenter\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"0 0 0 0.1019608\",\"distance\":\"0.5 -0.5\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"randomcase_info\",\"parent\":\"randomcase_menu\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 0.3785851\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"server.logo\",\"parent\":\"randomcase_info\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"1 1 1 0.5468885\",\"png\":\"{server_logo}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5979137 0.07877916\",\"anchormax\":\"0.9849928 0.9221781\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"GUIText.randomcase\",\"parent\":\"randomcase_info\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.005856483 0.02889946\",\"anchormax\":\"0.5896779 0.9768029\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"GUITEXT.customcase\",\"parent\":\"GUIText.randomcase\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{customtext}\",\"fontSize\":17,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}]},{\"name\":\"randomcase_menuClose\",\"parent\":\"randomcase_menu\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"casesmenuclose\",\"color\":\"0.1647059 0.1254902 0.3372549 0.8196079\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.95 0.9\",\"anchormax\":\"0.998 0.998\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"randomcase_X\",\"parent\":\"randomcase_menuClose\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"X\",\"fontSize\":22,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"CuiElement\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.07320644 0.1302083\",\"anchormax\":\"0.1464129 0.2604167\"}]},{\"name\":\"CuiElement\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.07320644 0.1302083\",\"anchormax\":\"0.1464129 0.2604167\"}]},{\"name\":\"CuiElement\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.07320644 0.1302083\",\"anchormax\":\"0.1464129 0.2604167\"}]},{\"name\":\"CuiElement\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"RectTransform\",\"anchormin\":\"0.07320644 0.1302083\",\"anchormax\":\"0.1464129 0.2604167\"}]}]";
        string GUIInfo = "[{\"name\":\"randomcase.info.name\",\"parent\":\"randomcase_info\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0 0 0 0.1137255\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.005 0.02317023\",\"anchormax\":\"0.58 0.9546165\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"randomcase.info.nametitle\",\"parent\":\"randomcase.info.name\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0 0 0 0.1137255\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.78\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"randomcase.info.texttitle\",\"parent\":\"randomcase.info.nametitle\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{1}\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2798101 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"randomcase.infotext\",\"parent\":\"randomcase.info.name\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{desc}\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.2942004 0.2636817\",\"anchormax\":\"1 0.8\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"randomcase.info.down\",\"parent\":\"randomcase.info.name\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"color\":\"0 0 0 0.1529412\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.28 0\",\"anchormax\":\"1 0.2384105\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"randomcase.info.col\",\"parent\":\"randomcase.info.down\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"color\":\"0.1647059 0.1254902 0.3372549 0.3882353\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.5 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"randomcase.info.colinfo\",\"parent\":\"randomcase.info.col\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Осталось кейсов: {amount} шт.\",\"fontSize\":18,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"randomcase.info.button\",\"parent\":\"randomcase.info.down\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"openplayercase {3} {amount}\",\"color\":\"0 0.9568627 1 0.8196079\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0\",\"anchormax\":\"0.997 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"randomcase.info.buttontext\",\"parent\":\"randomcase.info.button\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"Взять\",\"fontSize\":20,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]},{\"name\":\"randomcase.info.iconm\",\"parent\":\"randomcase.info.name\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"png\":\"{0}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.28 0.989071\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 1\"}]}]";
      //  Слив плагинов server-rust by Apolo YouGame
        string GUIBuyCases = "[{\"name\":\"randomcases_text\",\"parent\":\"randomcase_list\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"0.1647059 0.1254902 0.3372549 0.3\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}]},{\"name\":\"randomcases_text.text\",\"parent\":\"randomcases_text\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{noCases}\",\"fontSize\":23,\"align\":\"MiddleCenter\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}]}]}]";

        string HandleArgs(string json, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
                json = json.Replace("{" + i + "}", args[i].ToString());
            return json;
        }

        void DrawCaseInfo(BasePlayer player, string ccase1)
      //  Слив плагинов server-rust by Apolo YouGame
        {
            CasePlayer casePlayer;
            if (!players.TryGetValue(player.userID, out casePlayer))
            {
                SendReply(player, Messages["plNotCases"]);
                return;
            }
            Dictionary<string, int> casesCollapsed = new Dictionary<string, int>();

            foreach (var ccase in casePlayer.Inventory)
            {
                if (!casesCollapsed.ContainsKey(ccase))
                    casesCollapsed[ccase] = 0;
                casesCollapsed[ccase]++;
            }
            var amount = casesCollapsed[ccase1];
            if (casesCollapsed == null) return;
            if (amount <= 0) return;
            CuiHelper.DestroyUi(player, "GUITEXT.customcase");
            CuiHelper.DestroyUi(player, "randomcase.info.name");
            CuiHelper.AddUi(player,
                      HandleArgs(GUIInfo, m_FileManager.GetPng($"Icon{ccase1}"), cases[ccase1].Name, 1).Replace("{3}", ccase1).Replace("{desc}", cases[ccase1].Description)
      //  Слив плагинов server-rust by Apolo YouGame
                      .Replace("{desc}", cases[ccase1].Description)
                      .Replace("{amount}", casesCollapsed[ccase1].ToString()));
        }


        void DrawCases(BasePlayer player, int page)
        {
            CasePlayer casePlayer;
            CuiHelper.DestroyUi(player, "randomcase_menu");
            CuiHelper.DestroyUi(player, "casescontainer");
            CuiHelper.DestroyUi(player, "casescontainer1");
            CuiHelper.AddUi(player,
                GUI.Replace("{server_logo}", m_FileManager.GetPng("Serverlogo"))
                .Replace("{title}", Messages["GUITitle"])
                .Replace("{customtext}", Messages["customText"]));

            if (!players.TryGetValue(player.userID, out casePlayer))
            {
                SendReply(player, Messages["plNotCases"]);
                return;
            }
            var container = new CuiElementContainer();
            float gap = -0.005f;
            float width = 0.19f;
            float height = 0.95f;
            float startxBox = 0.035f;
            float startyBox = 1f - height;
            float xmin = startxBox;
            float ymin = startyBox;
            int i = 0;
            var mainPanel = container.Add(new CuiPanel() { Image = { Color = "0 0 0 0" } }, "randomcase_list", "casescontainer");
            var mainPanel1 = container.Add(new CuiPanel() { Image = { Color = "0 0 0 0" } }, "GUIText.randomcase", "casescontainer1");

            Dictionary<string, int> casesCollapsed = new Dictionary<string, int>();

            foreach (var ccase in casePlayer.Inventory)
            {
                if (!casesCollapsed.ContainsKey(ccase))
                    casesCollapsed[ccase] = 0;
                casesCollapsed[ccase]++;
            }


            if (casesCollapsed.Count == 0 && casePlayer.Inventory.Count == 0)
            {
                if (casePlayer.CasesQueue.Count == 0)
                {
                    CuiHelper.DestroyUi(player, "casescontainer1");
                    CuiHelper.AddUi(player, GUIBuyCases.Replace("{noCases}", Messages["nogiveCases"]));
                    return;
                }
                CuiHelper.DestroyUi(player, "casescontainer1");
                CuiHelper.AddUi(player, GUIBuyCases.Replace("{noCases}", Messages["endCases"]));
                return;

            }
            if (page == 1)
            {
                foreach (var ccase in casesCollapsed)
                {
                    container.Add(new CuiButton()
                    {
                        Button = { Command = $"randomcase.open {ccase.Key}", Color = "0.1647059 0.1254902 0.3372549 0.5" },
                        RectTransform = {
                        AnchorMin = xmin + " " + ymin,
                        AnchorMax = (xmin + width) + " " + (ymin + height *1),
                        OffsetMax = "-5 -7",
                        OffsetMin = "4 0"
                    },
                        Text = { Text = $"", Align = TextAnchor.LowerCenter, Color = "1 1 1 1", FontSize = 18 }
                    }, mainPanel, $"ui.case.{cases[ccase.Key].Type}");
                    xmin += width + gap;
                    if (xmin + width >= 1)
                    {
                        xmin = startxBox;
                        ymin -= height + gap;
                    }
                    InitilizeKitImageUI(ref container, cases[ccase.Key].Type);
                    i++;
                    if (!cases.ContainsKey(ccase.Key))
                    {
                        break;
                    }
                    if (i >= 5)
                    {
                        break;
                    }
                }
                if (casesCollapsed.Count > 5)
                {
                    container.Add(new CuiButton
                    {
                        Button = { Command = "nextpage.case2", Color = "0.1647059 0.1254902 0.3372549 0.6" },
                        RectTransform = { AnchorMin = "0.965 0.045", AnchorMax = "0.997 0.97" },
                        Text = { Text = ">", FontSize = 22, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
                    }, mainPanel);
                }

                CuiHelper.AddUi(player, container);
                return;
            }

            if (page == 2)
            {
                var casesCollapsed1 = casesCollapsed.Skip(5);
                foreach (var ccase in casesCollapsed1)
                {
                    container.Add(new CuiButton()
                    {
                        Button = { Command = $"randomcase.open {ccase.Key}", Color = "0.1647059 0.1254902 0.3372549 0.5" },
                        RectTransform = {
                        AnchorMin = xmin + " " + ymin,
                        AnchorMax = (xmin + width) + " " + (ymin + height *1),
                        OffsetMax = "-5 -7",
                        OffsetMin = "4 0"
                    },
                        Text = { Text = $"", Align = TextAnchor.LowerCenter, Color = "1 1 1 1", FontSize = 18 }
                    }, mainPanel, $"ui.case.{cases[ccase.Key].Type}");
                    xmin += width + gap;
                    if (xmin + width >= 1)
                    {
                        xmin = startxBox;
                        ymin -= height + gap;
                    }
                    InitilizeKitImageUI(ref container, cases[ccase.Key].Type);
                    i++;
                }
                if (casesCollapsed.Count > 5)
                {
                    container.Add(new CuiButton
                    {
                        Button = { Command = "nextpage.case1", Color = "0.1647059 0.1254902 0.3372549 0.6" },
                        RectTransform = { AnchorMin = "0.002 0.045", AnchorMax = "0.035 0.97" },
                        Text = { Text = "<", FontSize = 20, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
                    }, mainPanel);
                }
                CuiHelper.AddUi(player, container);
            }
        }

        private void InitilizeKitImageUI(ref CuiElementContainer container, string ccase)
        {
            string image = m_FileManager.GetPng($"Icon{ccase}");
            CuiRawImageComponent imageComp = new CuiRawImageComponent
            {
                Sprite = "assets/content/textures/generic/fulltransparent.tga",
                Color = "#FFFFFFFF"
            };
            if (image != string.Empty)
            {
                imageComp.Png = image;
            }
            container.Add(new CuiElement
            {
                Parent = $"ui.case.{ccase}",
                Components =
                {
                    imageComp,
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1" }
                }
            });

        }



        #endregion

        #region DATA

        DynamicConfigFile players_File = Interface.Oxide.DataFileSystem.GetFile("RandomCases/RandomCase_Players");

        void LoadData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("RandomCases/RandomCase_Cases"))
                cases = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, CaseDefinition>>("RandomCases/RandomCase_Cases");
            else
            {
                cases.Add("weapons", new CaseDefinition()
                {
                    Name = "Оружие",
                    Type = "weapons",
                    Images = "https://i.imgur.com/w9tvRY8.png",
                    Description = "Описание",
                    CoolDown = 43200,
                    Items = new List<CaseItem>
                    {
                        new CaseItem
                        {
                            Shortname = "rifle.ak",
                            Min = 1,
                            Max = 1
                        }
                    }
                });
                cases.Add("resources", new CaseDefinition()
                {
                    Name = "Ресурсы",
                    Type = "resources",
                    Images = "https://i.imgur.com/w9tvRY8.png",
                    Description = "Описание",
                    CoolDown = 43200,
                    Items = new List<CaseItem>
                    {
                        new CaseItem
                        {
                            Shortname = "rifle.ak",
                            Min = 1,
                            Max = 1
                        }
                    }
                });
                Interface.Oxide.DataFileSystem.WriteObject("RandomCases/RandomCase_Cases", cases);
            }

            try
            {
                players = players_File.ReadObject<Dictionary<ulong, CasePlayer>>();
            }
            catch
            {
                players = new Dictionary<ulong, CasePlayer>();
            }
        }



        #endregion

        #region File Manager

        #region LoadIcon
        private GameObject FileManagerObject;
        private FileManager m_FileManager;

        /// <summary>
        /// Инициализация скрипта взаимодействующего с файлами сервера
        /// </summary>
        void InitFileManager()
        {
            FileManagerObject = new GameObject("MAP_FileManagerObject");
            m_FileManager = FileManagerObject.AddComponent<FileManager>();

        }

        class FileManager : MonoBehaviour
        {
            int loaded = 0;
            int needed = 0;

            public bool IsFinished => needed == loaded;
            const ulong MaxActiveLoads = 10;
            Dictionary<string, FileInfo> files = new Dictionary<string, FileInfo>();
      //  Слив плагинов server-rust by Apolo YouGame

            private class FileInfo
      //  Слив плагинов server-rust by Apolo YouGame
            {
                public string Url;
                public string Png;
            }


            public string GetPng(string name) => files[name].Png;


            public IEnumerator LoadFile(string name, string url, int size = -1)
            {
                if (files.ContainsKey(name) && files[name].Url == url && !string.IsNullOrEmpty(files[name].Png)) yield break;
                files[name] = new FileInfo() { Url = url };
      //  Слив плагинов server-rust by Apolo YouGame
                needed++;
                yield return StartCoroutine(LoadImageCoroutine(name, url, size));

            }

            IEnumerator LoadImageCoroutine(string name, string url, int size = -1)
            {
                using (WWW www = new WWW(url))
                {
                    yield return www;
                    if (string.IsNullOrEmpty(www.error))
                    {
                        var bytes = size == -1 ? www.bytes : Resize(www.bytes, size);


                        var entityId = CommunityEntity.ServerInstance.net.ID;
                        var crc32 = FileStorage.server.Store(bytes, FileStorage.Type.png, entityId).ToString();
                        files[name].Png = crc32;
                    }

                }
                loaded++;
                instance.init = true;

            }

            static byte[] Resize(byte[] bytes, int size)
            {
                Image img = (Bitmap)(new ImageConverter().ConvertFrom(bytes));
                Bitmap cutPiece = new Bitmap(size, size);
                System.Drawing.Graphics graphic = System.Drawing.Graphics.FromImage(cutPiece);
                graphic.DrawImage(img, new Rectangle(0, 0, size, size), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel);
                graphic.Dispose();
                MemoryStream ms = new MemoryStream();
                cutPiece.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();

            }
        }
        #endregion

        #endregion

        #region LOCALIZATION

        Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"GUITitle", "ВАШИ СОБРАННЫЕ КЕЙСЫ" },
            {"givePlayerCase", "Вы получили кейс {0}" },
            {"isHaveCases", "У вас есть не открытые кейсы!\nЧтобы их открыть наберите команду <color=#fee3b4>/case</color>" },
             {"plInit", "В данный момент кейсы не активны" },
             {"invFull", "У вас переполнен инвентарь!" },
             {"openCases", "Вы открыли кейс {0}, и получили: {1} - {2} шт." },
             {"plNotCases", "У вас нет кейсов" },
             {"endCases", "У Вас закончились кейсы =(" },
             {"nogiveCases", "У Вас нету кейсов =(\nКупить вы их сможете в магазине сервера <color=#fee3b4>rust.facepunch.com</color>" },
             {"customText", "Здесь текст!\nНу или информация о том как получить кейсы\nТекст вы сможете изменить в lang" },
        };

        #endregion

    }
}
                   
