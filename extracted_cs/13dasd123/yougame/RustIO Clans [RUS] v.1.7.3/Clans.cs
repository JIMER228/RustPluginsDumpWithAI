using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Oxide.Plugins {
    [Info("Rust:IO Clans", "playrust.io / dcode", "1.7.3", ResourceId = 842)]
    [Description("Allows your players to form and manage clans with Rust:IO")]
    public class Clans : RustPlugin
    {
        #region Rust:IO Bindings

        private Library lib;
        private MethodInfo isInstalled;
        private MethodInfo hasFriend;
        private MethodInfo addFriend;
        private MethodInfo deleteFriend;

        private void ioInitialize()
        {
            lib = Interface.Oxide.GetLibrary<Library>("RustIO");
            if (lib == null || (isInstalled = lib.GetFunction("IsInstalled")) == null || (hasFriend = lib.GetFunction("HasFriend")) == null || (addFriend = lib.GetFunction("AddFriend")) == null || (deleteFriend = lib.GetFunction("DeleteFriend")) == null)
            {
                lib = null;
                Puts("Rust:IO is not present. You need to install Rust:IO first in order to use this plugin!");
            }
        }

        private bool ioIsInstalled()
        {
            if (lib == null) return false;
            return (bool)isInstalled.Invoke(lib, new object[] { });
        }

        private bool ioHasFriend(string playerId, string friendId)
        {
            if (lib == null) return false;
            return (bool)hasFriend.Invoke(lib, new object[] { playerId, friendId });
        }

        private bool ioAddFriend(string playerId, string friendId)
        {
            if (lib == null) return false;
            return (bool)addFriend.Invoke(lib, new object[] { playerId, friendId });
        }

        private bool ioDeleteFriend(string playerId, string friendId)
        {
            if (lib == null) return false;
            return (bool)deleteFriend.Invoke(lib, new object[] { playerId, friendId });
        }

        #endregion

        private Dictionary<string, Clan> clans = new Dictionary<string, Clan>();
        private Dictionary<string, string> originalNames = new Dictionary<string, string>();
        private Regex tagRe = new Regex("^[a-zA-Z0-9]{2,6}$");
        private Dictionary<string, string> messages = new Dictionary<string, string>();
        private Dictionary<string, Clan> lookup = new Dictionary<string, Clan>();
        private bool addClanMatesAsFriends = true;
        private int limitMembers = -1;
        private int limitModerators = -1;

        private void loadData()
        {
            clans.Clear();
            var data = Interface.Oxide.DataFileSystem.GetDatafile("rustio_clans");
            if (data["clans"] != null) {
                var clansData = (Dictionary<string, object>)Convert.ChangeType(data["clans"], typeof(Dictionary<string, object>));
                foreach (var iclan in clansData) {
                    string tag = iclan.Key;
                    var clanData = iclan.Value as Dictionary<string, object>;
                    string description = (string)clanData["description"];
                    string owner = (string)clanData["owner"];
                    List<string> moderators = new List<string>();
                    foreach (var imoderator in clanData["moderators"] as List<object>) {
                        moderators.Add((string)imoderator);
                    }
                    List<string> members = new List<string>();
                    foreach (var imember in clanData["members"] as List<object>) {
                        members.Add((string)imember);
                    }
                    List<string> invited = new List<string>();
                    foreach (var iinvited in clanData["invited"] as List<object>) {
                        invited.Add((string)iinvited);
                    }
                    Clan clan;
                    clans.Add(tag, clan = new Clan() {
                        tag = tag,
                        description = description,
                        owner = owner,
                        moderators = moderators,
                        members = members,
                        invited = invited
                    });
                    lookup[owner] = clan;
                    foreach (var member in members)
                        lookup[member] = clan;
                }
            }
        }

        private void saveData()
        {
            var data = Interface.Oxide.DataFileSystem.GetDatafile("rustio_clans");
            var clansData = new Dictionary<string, object>();
            foreach (var clan in clans) {
                var clanData = new Dictionary<string, object>();
                clanData.Add("tag", clan.Value.tag);
                clanData.Add("description", clan.Value.description);
                clanData.Add("owner", clan.Value.owner);
                var moderators = new List<object>();
                foreach (var imoderator in clan.Value.moderators)
                    moderators.Add(imoderator);
                var members = new List<object>();
                foreach (var imember in clan.Value.members)
                    members.Add(imember);
                var invited = new List<object>();
                foreach (var iinvited in clan.Value.invited)
                    invited.Add(iinvited);
                clanData.Add("moderators", moderators);
                clanData.Add("members", members);
                clanData.Add("invited", invited);
                clansData.Add(clan.Value.tag, clanData);
            }
            data["clans"] = clansData;
            Interface.Oxide.DataFileSystem.SaveDatafile("rustio_clans");
        }

        private List<string> texts = new List<string>() {
            "%NAME% в сети!",
            "%NAME% нет в сети.",
			
            "В настоящее время вы не являетесь членом клана.",
            "Вы владельц:",
            "Вы модератор:",
            "Вы являетесь членом:",
            "Пользователи онлайн:",
            "Ожидает приглашения:",
            "Чтобы узнать больше о кланах, введите: <color=\"#ffd479\">/clan help</color>",

            "Usage: <color=\"#ffd479\">/clan create \"TAG\" \"Description\"</color>",
            "Вы уже являетесь членом клана.",
            "Clan tags должны иметь длину от 2 до 6 символов и могут содержать только стандартные буквы и цифры",
            "Просьба представить краткое описание вашего клана.",
            "У этого клана уже есть название.",
            "Теперь вы являетесь владельцем своего нового клана:",
            "Чтобы пригласить новых членов, введите: <color=\"#ffd479\">/clan invite \"Имя игрока\"</color>",

            "Usage: <color=\"#ffd479\">/clan invite \"Имя игрока\"</color>",
            "Вы должны быть модератором своего клана, чтобы использовать эту команду.",
            "Нет такого имени игрока или игрок не в сети:",
            "Этот игрок уже является членом вашего клана:",
            "Этот игрок не является членом вашего клана:",
            "Этот игрок уже был приглашен в ваш клан:",
            "Этот игрок уже является модератором вашего клана:",
            "Этот игрок не модератор вашего клана:",
            "%MEMBER% пригласил %PLAYER% в клан.",
            "Usage: <color=\"#ffd479\">/clan join \"TAG\"</color>",
            "У вас нет приглашения в клан.",
            "%NAME% присоединился к клану!",
            "Вас пригласили присоединиться к клану:",
            "Чтобы присоединиться, введите: <color=#ffd479>/clan join \"%TAG%\"</color>",
            "Этот клан уже достиг максимального числа членов.",
            "Этот клан уже достиг максимального числа модераторов.",

            "Usage: <color=\"#ffd479\">/clan promote \"Имя игрока\"</color>",
            "Вы должны быть владельцем клана, чтобы использовать эту команду.",
            "%OWNER% повысил %MEMBER% до модератора.",

            "Usage: <color=\"#ffd479\">/clan demote \"Имя игрока\"</color>",

            "Usage: <color=\"#ffd479\">/clan leave</color>",
            "Вы оставили текущий клан.",
            "%NAME% покинул клан.",

            "Usage: <color=\"#ffd479\">/clan kick \"Имя игрока\"</color>",
            "Этот игрок является владельцем или модератором:",
            "%NAME% ударил %MEMBER% из клана.",

            "Usage: <color=\"#ffd479\">/clan disband forever</color>",
            "Ваш клан был распущен навсегда.",

            "Usage: <color=\"#ffd479\">/clan delete \"TAG\"</color>",
            "Вы должны быть владельцем сервера для удаления кланов.",
            "Нет клана с таким названием:",
            "Ваш клан был удален владельцем сервера.",
            "Вы удалили клан:",

            "Доступные команды:",
            "<color=#ffd479>/clan</color> - отображает информацию о вашем клане",
            "<color=#ffd479>/c Сообщение...</color> - отправляет сообщение всем членам клана",
            "<color=#ffd479>/clan create \"TAG\" \"Description\"</color> - Создает новый клан",
            "<color=#ffd479>/clan join \"TAG\"</color> - присоединяется к клану",
            "<color=#ffd479>/clan leave</color> - оставить текущий клан",
            "<color=#74c6ff>Команды модератора клана</color>:",
            "<color=#ffd479>/clan invite \"Имя игрока\"</color> - Приглашает игрока в ваш клан",
            "<color=#ffd479>/clan kick \"Player name\"</color> - Удалить игрока из вашего клана",
            "<color=#a1ff46>Команды владельца клана</color>:",
            "<color=#ffd479>/clan promote \"Имя\"</color> - Назначить игрока модератором",
            "<color=#ffd479>/clan demote \"Имя\"</color> - Разжаловать модератора до игрока",
            "<color=#ffd479>/clan disband forever</color> - расформировать клан навсегда(без отмены)",
            "<color=#cd422b>Команды владельца сервера</color>:",
            "<color=#ffd479>/clan delete \"TAG\"</color> - Удалить клан навсегда(без отмены)",

            "<color=\"#ffd479\">/clan</color> - отображает текущий статус клана",
            "<color=\"#ffd479\">/clan help</color> - Помощь по созданию клана"
        };

        protected override void LoadDefaultConfig() {
            var messages = new Dictionary<string, object>();
            foreach (var text in texts) {
                if (messages.ContainsKey(text))
                    Puts("Duplicate translation string: " + text);
                else
                    messages.Add(text, text);
            }
            Config["messages"] = messages;
            Config.Set("addClanMatesAsFriends", true);
            Config.Set("limit", "members", -1);
            Config.Set("limit", "moderators", -1);
        }

        private string _(string text, Dictionary<string, string> replacements = null)
        {
            if (messages.ContainsKey(text) && messages[text] != null)
                text = messages[text];
            if (replacements != null)
                foreach (var replacement in replacements)
                    text = text.Replace("%" + replacement.Key + "%", replacement.Value);
            return text;
        }

        private Clan findClan(string tag)
        {
            Clan clan;
            if (clans.TryGetValue(tag, out clan))
                return clan;
            return null;
        }

        private Clan findClanByUser(string userId)
        {
            Clan clan;
            if (lookup.TryGetValue(userId, out clan))
                return clan;
            return null;
        }

        private BasePlayer findPlayerByPartialName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            BasePlayer player = null;
            name = name.ToLower();
            var allPlayers = BasePlayer.activePlayerList.ToArray();

            foreach (var p in allPlayers) {
                if (p.displayName == name) {
                    if (player != null)
                        return null; // Not unique
                    player = p;
                }
            }
            if (player != null)
                return player;

            foreach (var p in allPlayers) {
                if (p.displayName.ToLower().IndexOf(name) >= 0) {
                    if (player != null)
                        return null; // Not unique
                    player = p;
                }
            }
            return player;
        }

        private string stripTag(string name, Clan clan)
        {
            if (clan == null)
                return name;
            var re = new Regex(@"^\[" + clan.tag + @"\]\s");
            while (re.IsMatch(name))
                name = name.Substring(clan.tag.Length + 3);
            return name;
        }

        private void setupPlayer(BasePlayer player)
        {
            var prevName = player.displayName;
            var playerId = player.UserIDString;
            var clan = findClanByUser(playerId);
            player.displayName = stripTag(player.displayName, clan);
            string originalName = null;
            if (!originalNames.ContainsKey(playerId)) {
                originalNames.Add(playerId, originalName = player.displayName);
            } else {
                originalName = originalNames[playerId];
            }
            if (clan == null) {
                player.displayName = originalName;
            } else {
                var tag = "[" + clan.tag + "]" + " ";
                if (!player.displayName.StartsWith(tag))
                    player.displayName = tag + originalName;
            }
            if (player.displayName != prevName)
                player.SendNetworkUpdate();
        }

        private void setupPlayers(List<string> playerIds)
        {
            foreach (var playerId in playerIds) {
                var uid = Convert.ToUInt64(playerId);
                var player = BasePlayer.FindByID(uid);
                if (player != null)
                    setupPlayer(player);
                else {
                    player = BasePlayer.FindSleeping(uid);
                    if (player != null)
                        setupPlayer(player);
                }
            }
        }

        private void OnServerInitialized()
        {
            try {
                ioInitialize();
                LoadConfig();
                try {
                    var customMessages = Config.Get<Dictionary<string, object>>("messages");
                    if (customMessages != null)
                        foreach (var pair in customMessages)
                            messages[pair.Key] = (string)pair.Value;
                    loadData();
                } catch (Exception ex2) {
                    PrintWarning("oxide/config/Clans.json seems to contain an invalid 'messages' structure. Please delete the config file once and reload the plugin.");
                }
                foreach (var player in BasePlayer.activePlayerList)
                    setupPlayer(player);
                foreach (var player in BasePlayer.sleepingPlayerList)
                    setupPlayer(player);
                try { addClanMatesAsFriends = Config.Get<bool>("addClanMatesAsFriends"); } catch { }
                try { limitMembers = Config.Get<int>("limit", "members"); } catch { }
                try { limitModerators = Config.Get<int>("limit", "moderators"); } catch { }
            } catch (Exception ex) {
                PrintError("OnServerInitialized failed", ex);
            }
        }

        private void OnUserApprove(Network.Connection connection)
        {
            originalNames[connection.userid.ToString()] = connection.username;
        }

        private void OnPlayerInit(BasePlayer player)
        {
            string originalName;
            if (originalNames.TryGetValue(player.UserIDString, out originalName))
                player.displayName = originalName;
            try {
                setupPlayer(player);
                var clan = findClanByUser(player.UserIDString);
                if (clan != null)
                    clan.Broadcast(_("%NAME% has come online!", new Dictionary<string, string>() { { "NAME", stripTag(player.displayName, clan) } }));
            } catch (Exception ex) {
                PrintError("OnPlayerInit failed", ex);
            }
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            try {
                var clan = findClanByUser(player.UserIDString);
                if (clan != null)
                    clan.Broadcast(_("%NAME% has gone offline.", new Dictionary<string, string>() { { "NAME", stripTag(player.displayName, clan) } }));
            } catch (Exception ex) {
                PrintError("OnPlayerDisconnected failed", ex);
            }
        }

        private void Unload()
        {
            try {
                foreach (var pair in originalNames) {
                    var playerId = Convert.ToUInt64(pair.Key);
                    var player = BasePlayer.FindByID(playerId);
                    if (player != null)
                        player.displayName = pair.Value;
                    else {
                        player = BasePlayer.FindSleeping(playerId);
                        if (player != null)
                            player.displayName = pair.Value;
                    }
                }
            } catch (Exception ex) {
                PrintError("Unload failed", ex);
            }
        }

        private void SendHelpText(BasePlayer player)
        {
            var sb = new StringBuilder()
               .Append("<size=18>Clans</size> by <color=#ce422b>http://playrust.io</color>\n")
               .Append("  ").Append(_("<color=\"#ffd479\">/clan</color> - отображает текущий статус клана")).Append("\n")
               .Append("  ").Append(_("<color=\"#ffd479\">/clan help</color> - Помощь по созданию клана"));
            player.ChatMessage(sb.ToString());
        }

        [ChatCommand("clan")]
        private void ClanCommand(BasePlayer player, string command, string[] args)
        {
            var userId = player.UserIDString;
            var myClan = findClanByUser(userId);
            var sb = new StringBuilder();

            if (args.Length == 0) {
                sb.Append("<size=22>Clans</size> " + Version + " by <color=#ce422b>http://playrust.io</color>\n");
                if (myClan == null) {
                    sb.Append(_("Вы не являетесь членом клана.")).Append("\n");
                } else {
                    if (myClan.IsOwner(userId)) {
                        sb.Append(_("Вы являетесь владельцем:"));
                    } else if (myClan.IsModerator(userId))
                        sb.Append(_("Вы являетесь модератором:"));
                    else
                        sb.Append(_("Вы являетесь членом:"));
                    sb.Append(" [").Append(myClan.tag).Append("] ").Append(myClan.description).Append("\n");
                    sb.Append(_("Пользователи онлайн:")).Append(" ");
                    List<string> onlineMembers = new List<string>();
                    int n = 0;
                    foreach (var memberId in myClan.members) {
                        var p = BasePlayer.FindByID(Convert.ToUInt64(memberId));
                        if (p != null) {
                            if (n > 0) sb.Append(", ");
                            if (myClan.IsOwner(memberId)) {
                                sb.Append("<color=#a1ff46>").Append(stripTag(p.displayName, myClan)).Append("</color>");
                            } else if (myClan.IsModerator(memberId)) {
                                sb.Append("<color=#74c6ff>").Append(stripTag(p.displayName, myClan)).Append("</color>");
                            } else {
                                sb.Append(p.displayName);
                            }
                            ++n;
                        }
                    }
                    sb.Append("\n");
                    if ((myClan.IsOwner(userId) || myClan.IsModerator(userId)) && myClan.invited.Count > 0) {
                        sb.Append(_("Ожидает приглашения:")).Append(" ");
                        int m = 0;
                        foreach (var inviteId in myClan.invited) {
                            var p = BasePlayer.FindByID(Convert.ToUInt64(inviteId));
                            if (p != null) {
                                if (m > 0) sb.Append(", ");
                                sb.Append(p.displayName);
                                ++m;
                            }
                        }
                        sb.Append("\n");
                    }
                }
                sb.Append(_("Чтобы узнать больше о кланах, введите: <color=\"#ffd479\">/clan help</color>"));
                SendReply(player, "{0}", sb.ToString());
                return;
            }
            switch (args[0]) {
                case "create":
                    if (args.Length != 3) {
                        sb.Append(_("Используйте: <color=\"#ffd479\">/clan create \"TAG\" \"Description\"</color>"));
                        break;
                    }
                    if (myClan != null) {
                        sb.Append(_("Вы уже являетесь членом клана."));
                        break;
                    }
                    if (!tagRe.IsMatch(args[1])) {
                        sb.Append(_("Clan tags должны иметь длину от 2 до 6 символов и могут содержать только стандартные буквы и цифры"));
                        break;
                    }
                    args[2] = args[2].Trim();
                    if (args[2].Length < 2 || args[2].Length > 30) {
                        sb.Append(_("Просьба представить краткое описание вашего клана."));
                        break;
                    }
                    if (clans.ContainsKey(args[1])) {
                        sb.Append(_("Уже есть клан с этим названием."));
                        break;
                    }
                    myClan = Clan.Create(args[1], args[2], userId);
                    clans.Add(myClan.tag, myClan);
                    saveData();
                    lookup[userId] = myClan;
                    setupPlayer(player); // Add clan tag
                    sb.Append(_("Теперь вы являетесь владельцем клана:")).Append(" ");
                    sb.Append("[").Append(myClan.tag).Append("] ").Append(myClan.description).Append("\n");
                    sb.Append(_("Чтобы пригласить новых участников, введите: <color=\"#ffd479\">/clan invite \"Имя игрока\"</color>"));
                    myClan.onCreate();
                    break;
                case "invite":
                    if (args.Length != 2) {
                        sb.Append(_("Используйте: <color=\"#ffd479\">/clan invite \"Имя игрока\"</color>"));
                        break;
                    }
                    if (myClan == null) {
                        sb.Append(_("Вы не являетесь членом клана."));
                        break;
                    }
                    if (!myClan.IsOwner(userId) && !myClan.IsModerator(userId)) {
                        sb.Append(_("Вы должны быть модератором клана, чтобы использовать эту команду."));
                        break;
                    }
                    var invPlayer = findPlayerByPartialName(args[1]);
                    if (invPlayer == null) {
                        sb.Append(_("Нет такого имени игрока или игрок не в сети:")).Append(" ").Append(args[1]);
                        break;
                    }
                    var invUserId = invPlayer.UserIDString;
                    if (myClan.members.Contains(invUserId)) {
                        sb.Append(_("Этот игрок уже является членом вашего клана:")).Append(" ").Append(invPlayer.displayName);
                        break;
                    }
                    if (myClan.invited.Contains(invUserId)) {
                        sb.Append(_("Этот игрок уже был приглашен в ваш клан:")).Append(" ").Append(invPlayer.displayName);
                        break;
                    }
                    myClan.invited.Add(invUserId);
                    saveData();
                    myClan.Broadcast(_("%MEMBER% пригласил %PLAYER% в клан.", new Dictionary<string, string>() { { "MEMBER", stripTag(player.displayName, myClan) }, { "PLAYER", invPlayer.displayName } }));
                    invPlayer.SendConsoleCommand("chat.add", "",
                        _("Вас пригласили в клан:") + " [" + myClan.tag + "] " + myClan.description + "\n" +
                        _("Чтобы присоединиться, введите: <color=#ffd479>/clan join \"%TAG%\"</color>", new Dictionary<string, string>() { { "TAG", myClan.tag } }));
                    myClan.onUpdate();
                    break;
                case "join":
                    if (args.Length != 2) {
                        sb.Append(_("Используйте: <color=\"#ffd479\">/clan join \"TAG\"</color>"));
                        break;
                    }
                    if (myClan != null) {
                        sb.Append(_("Вы уже являетесь членом клана."));
                        break;
                    }
                    myClan = findClan(args[1]);
                    if (myClan == null || !myClan.IsInvited(userId)) {
                        sb.Append(_("У вас нет приглашения в клан."));
                        break;
                    }
                    if (limitMembers >= 0 && myClan.members.Count >= limitMembers) {
                        sb.Append(_("Этот клан уже достиг максимального количества членов."));
                        break;
                    }
                    myClan.invited.Remove(userId);
                    myClan.members.Add(userId);
                    saveData();
                    lookup[userId] = myClan;
                    setupPlayer(player);
                    myClan.Broadcast(_("%NAME% присоединился к клану!", new Dictionary<string, string>() { { "NAME", stripTag(player.displayName, myClan) } }));
                    foreach (var memberId in myClan.members) {
                        if (memberId != userId && ioIsInstalled() && addClanMatesAsFriends) {
                            ioAddFriend(memberId, userId);
                            ioAddFriend(userId, memberId);
                        }
                    }
                    myClan.onUpdate();
                    break;
                case "promote":
                    if (args.Length != 2) {
                        sb.Append(_("Используйте: <color=\"#ffd479\">/clan promote \"Имя игрока\"</color>"));
                        break;
                    }
                    if (myClan == null) {
                        sb.Append(_("Вы не являетесь членом клана."));
                        break;
                    }
                    if (!myClan.IsOwner(userId)) {
                        sb.Append(_("Вы должны быть владельцем клана, чтобы использовать эту команду."));
                        break;
                    }
                    var promotePlayer = findPlayerByPartialName(args[1]);
                    if (promotePlayer == null) {
                        sb.Append(_("Нет такого имени игрока или игрок не в сети:") + " " + args[1]);
                        break;
                    }
                    var promotePlayerUserId = promotePlayer.UserIDString;
                    if (!myClan.IsMember(promotePlayerUserId)) {
                        sb.Append(_("Этот игрок не является членом вашего клана:") + " " + promotePlayer.displayName);
                        break;
                    }
                    if (myClan.IsModerator(promotePlayerUserId)) {
                        sb.Append(_("Этот игрок уже модератор вашего клана:") + " " + promotePlayer.displayName);
                        break;
                    }
                    if (limitModerators >= 0 && myClan.moderators.Count >= limitModerators) {
                        sb.Append(_("Этот клан уже достиг максимального количества модераторов."));
                        break;
                    }
                    myClan.moderators.Add(promotePlayerUserId);
                    saveData();
                    myClan.Broadcast(_("%OWNER% повысить %MEMBER% до модератора.", new Dictionary<string, string>() { { "OWNER", stripTag(player.displayName, myClan) }, { "MEMBER", stripTag(promotePlayer.displayName, myClan) } }));
                    myClan.onUpdate();
                    break;
                case "demote":
                    if (args.Length != 2) {
                        sb.Append(_("Используйте: <color=\"#ffd479\">/clan demote \"Имя игрока\"</color>"));
                        break;
                    }
                    if (myClan == null) {
                        sb.Append(_("Вы не являетесь членом клана."));
                        break;
                    }
                    if (!myClan.IsOwner(userId)) {
                        sb.Append(_("Вы должны быть владельцем клана, чтобы использовать эту команду."));
                        break;
                    }
                    var demotePlayer = findPlayerByPartialName(args[1]);
                    if (demotePlayer == null) {
                        sb.Append(_("Нет такого имени игрока или игрок не в сети:") + " " + args[1]);
                        break;
                    }
                    var demotePlayerUserId = demotePlayer.UserIDString;
                    if (!myClan.IsMember(demotePlayerUserId)) {
                        sb.Append(_("Этот игрок не является членом вашего клана:") + " " + demotePlayer.displayName);
                        break;
                    }
                    if (!myClan.IsModerator(demotePlayerUserId)) {
                        sb.Append(_("Этот игрок не модератор вашего клана:") + " " + demotePlayer.displayName);
                        break;
                    }
                    myClan.moderators.Remove(demotePlayerUserId);
                    saveData();
                    myClan.Broadcast(player.displayName + " разжалован " + demotePlayer.displayName + " до простого игрока");
                    myClan.onUpdate();
                    break;
                case "leave":
                    if (args.Length != 1) {
                        sb.Append(_("Используйте: <color=\"#ffd479\">/clan leave</color>"));
                        break;
                    }
                    if (myClan == null) {
                        sb.Append(_("Вы не являетесь членом клана."));
                        break;
                    }
                    if (myClan.members.Count == 1) { // Remove the clan once the last member leaves
                        clans.Remove(myClan.tag);
                    } else {
                        myClan.moderators.Remove(userId);
                        myClan.members.Remove(userId);
                        myClan.invited.Remove(userId);
                        if (myClan.IsOwner(userId) && myClan.members.Count > 0) { // Make the first member the new owner
                            myClan.owner = myClan.members[0];
                        }
                    }
                    saveData();
                    lookup.Remove(userId);
                    setupPlayer(player); // Remove clan tag
                    sb.Append(_("Вы вышли из клана."));
                    myClan.Broadcast(_("%NAME% покинул клан.", new Dictionary<string, string>() { { "Name", player.displayName } }));
                    myClan.onUpdate();
                    break;
                case "kick":
                    if (args.Length != 2) {
                        sb.Append(_("Используйте: <color=\"#ffd479\">/clan kick \"Имя игрока\"</color>"));
                        break;
                    }
                    if (myClan == null) {
                        sb.Append(_("Вы не являетесь членом клана."));
                        break;
                    }
                    if (!myClan.IsOwner(userId) && !myClan.IsModerator(userId)) {
                        sb.Append(_("Вы должны быть модератором клана, чтобы использовать эту команду."));
                        break;
                    }
                    var kickPlayer = findPlayerByPartialName(args[1]);
                    if (kickPlayer == null) {
                        sb.Append(_("Нет такого имени игрока или игрок не в сети:") + " " + args[1]);
                        break;
                    }
                    var kickPlayerUserId = kickPlayer.UserIDString;
                    if (!myClan.IsMember(kickPlayerUserId) && !myClan.IsInvited(kickPlayerUserId)) {
                        sb.Append(_("Этот игрок не является членом вашего клана:") + " " + kickPlayer.displayName);
                        break;
                    }
                    if (myClan.IsOwner(kickPlayerUserId) || myClan.IsModerator(kickPlayerUserId)) {
                        sb.Append(_("Этот игрок является владельцем или модератором:") + " " + kickPlayer.displayName);
                        break;
                    }
                    myClan.members.Remove(kickPlayerUserId);
                    myClan.invited.Remove(kickPlayerUserId);
                    saveData();
                    lookup.Remove(kickPlayerUserId);
                    setupPlayer(kickPlayer); // Remove clan tag
                    myClan.Broadcast(_("%NAME% исключён %MEMBER% из клана.", new Dictionary<string, string>() { { "Name", stripTag(player.displayName, myClan) }, { "MEMBER", kickPlayer.displayName } }));
                    myClan.onUpdate();
                    break;
                case "disband":
                    if (args.Length != 2) {
                        sb.Append(_("Используйте: <color=\"#ffd479\">/clan disband forever</color>"));
                        break;
                    }
                    if (myClan == null) {
                        sb.Append(_("Вы не являетесь членом клана."));
                        break;
                    }
                    if (!myClan.IsOwner(userId)) {
                        sb.Append(_("Вы должны быть владельцем клана, чтобы использовать эту команду."));
                        break;
                    }
                    clans.Remove(myClan.tag);
                    saveData();
                    foreach (var member in myClan.members)
                        lookup.Remove(member);
                    myClan.Broadcast(_("Текущий клан распущен навсегда."));
                    setupPlayers(myClan.members); // Remove clan tags
                    myClan.onDestroy();
                    break;
                case "delete":
                    if (args.Length != 2) {
                        sb.Append(_("Используйте: <color=\"#ffd479\">/clan delete \"TAG\"</color>"));
                        break;
                    }
                    if (player.net.connection.authLevel < 2) {
                        sb.Append(_("Вы должны быть владельцем сервера для удаления кланов."));
                        break;
                    }
                    Clan clan;
                    if (!clans.TryGetValue(args[1], out clan)) {
                        sb.Append(_("Нет клана с этим названием:")).Append(" ").Append(args[1]);
                        break;
                    }
                    clan.Broadcast(_("Ваш клан был удален владельцем сервера."));
                    clans.Remove(args[1]);
                    saveData();
                    foreach (var member in clan.members)
                        lookup.Remove(member);
                    setupPlayers(clan.members);
                    sb.Append(_("Вы удалили клан:")).Append(" [").Append(clan.tag).Append("] ").Append(clan.description);
                    myClan.onDestroy();
                    break;
                default:
                    sb.Append(_("Доступные команды:")).Append("\n");
                    sb.Append("  ").Append(_("<color=#ffd479>/clan</color> - Отображает информацию о текущем клане")).Append("\n");
                    sb.Append("  ").Append(_("<color=#ffd479>/c Сообщение...</color> - Отправляет сообщение всем участникам клана")).Append("\n");
                    sb.Append("  ").Append(_("<color=#ffd479>/clan create \"TAG\" \"Description\"</color> - Создает новый клан")).Append("\n");
                    sb.Append("  ").Append(_("<color=#ffd479>/clan join \"TAG\"</color> - Присоединяется к клану")).Append("\n");
                    sb.Append("  ").Append(_("<color=#ffd479>/clan leave</color> - Покинуть клан")).Append("\n");
                    sb.Append(_("<color=#74c6ff>Команды модератора</color>:")).Append("\n");
                    sb.Append("  ").Append(_("<color=#ffd479>/clan invite \"Имя игрока\"</color> - Пригласить игрока в клан")).Append("\n");
                    sb.Append("  ").Append(_("<color=#ffd479>/clan kick \"Имя игрока\"</color> - Исключить игрока из клана")).Append("\n");
                    sb.Append(_("<color=#a1ff46>Команды владельца</color>:")).Append("\n");
                    sb.Append("  ").Append(_("<color=#ffd479>/clan promote \"Имя\"</color> - Назначить игрока модератором")).Append("\n");
                    sb.Append("  ").Append(_("<color=#ffd479>/clan demote \"Имя\"</color> - Разжаловать модератора до игрока")).Append("\n");
                    sb.Append("  ").Append(_("<color=#ffd479>/clan disband forever</color> - расформировать клан навсегда(без отмены)")).Append("\n");
                    if (player.net.connection.authLevel >= 2) {
                        sb.Append(_("<color=#cd422b>Команды владельца сервера</color>:")).Append("\n");
                        sb.Append("  ").Append(_("<color=#ffd479>/clan delete \"TAG\"</color> - Удалить клан навсегда(без отмены)")).Append("\n");
                    }
                    break;
            }
            SendReply(player, "{0}", sb.ToString().TrimEnd());
        }

        [ChatCommand("c")]
        private void ClanChatCommand(BasePlayer player, string command, string[] args)
        {
            var playerId = player.UserIDString;
            var myClan = findClanByUser(playerId);
            if (myClan == null) {
                SendReply(player, "{0}", _("Вы не являетесь членом клана."));
                return;
            }
            var message = string.Join(" ", args);
            if (string.IsNullOrEmpty(message))
                return;
            myClan.Broadcast(stripTag(player.displayName, myClan) + ": " + message);
            var playerName = originalNames.ContainsKey(playerId) ? originalNames[playerId] : player.displayName;
            Puts("[CLANCHAT] {0} - {1}: {2}", myClan.tag, playerName, message);
        }

        public class Clan
        {
            public string tag;
            public string description;
            public string owner;
            public List<string> moderators = new List<string>();
            public List<string> members = new List<string>();
            public List<string> invited = new List<string>();

            public static Clan Create(string tag, string description, string ownerId) {
                var clan = new Clan() { tag = tag, description = description, owner = ownerId };
                clan.members.Add(ownerId);
                return clan;
            }

            public bool IsOwner(string userId) {
                return userId == owner;
            }

            public bool IsModerator(string userId) {
                return moderators.Contains(userId);
            }

            public bool IsMember(string userId) {
                return members.Contains(userId);
            }

            public bool IsInvited(string userId) {
                return invited.Contains(userId);
            }

            public void Broadcast(string message) {
                foreach (var memberId in members) {
                    var player = BasePlayer.FindByID(Convert.ToUInt64(memberId));
                    if (player == null)
                        continue;
                    player.SendConsoleCommand("chat.add", "", "<color=#a1ff46>(CLAN)</color> " + message);
                }
            }

            internal JObject ToJObject() {
                var obj = new JObject();
                obj["tag"] = tag;
                obj["description"] = description;
                obj["owner"] = owner;
                var jmoderators = new JArray();
                foreach (var moderator in moderators)
                    jmoderators.Add(moderator);
                obj["moderators"] = jmoderators;
                var jmembers = new JArray();
                foreach (var member in members)
                    jmembers.Add(member);
                obj["members"] = jmembers;
                var jinvited = new JArray();
                foreach (var invite in invited)
                    jinvited.Add(invite);
                obj["invited"] = jinvited;
                return obj;
            }

            internal void onCreate() => Interface.Call("OnClanCreate", tag);
            internal void onUpdate() => Interface.Call("OnClanUpdate", tag);
            internal void onDestroy() => Interface.Call("OnClanDestroy", tag);
        }

        #region Plugin API

        [HookMethod("GetClan")]
        private JObject GetClan(string tag)
        {
            var clan = findClan(tag);
            if (clan == null)
                return null;
            return clan.ToJObject();
        }

        [HookMethod("GetAllClans")]
        private JArray GetAllClans()
        {
            return new JArray(clans.Keys);
        }

        [HookMethod("GetClanOf")]
        private string GetClanOf(object player)
        {
            if (player == null)
                throw new ArgumentException("player");
            if (player is ulong)
                player = ((ulong)player).ToString();
            else if (player is BasePlayer)
                player = (player as BasePlayer).userID.ToString();
            if (!(player is string))
                throw new ArgumentException("player");
            var clan = findClanByUser((string)player);
            if (clan == null)
                return null;
            return clan.tag;
        }

        #endregion
    }
}
