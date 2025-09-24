// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Newtonsoft.Json; 
using Newtonsoft.Json.Linq; 
using Oxide.Core; 
using Oxide.Core.Libraries; 
using Oxide.Core.Libraries.Covalence; 
using Oxide.Core.Plugins; 
using System; 
using System.Collections.Generic; 
using System.Reflection; 
using System.Text; 
using System.Linq; 
using System.Text.RegularExpressions; 
using UnityEngine; 
using Rust;  

namespace Oxide.Plugins 
{ 
    [Info("Clans", "S1m0n", "2.7.7")] 
	public class Clans : RustPlugin 
	{ 
	
		bool Changed; 
		bool Initialized; 
		internal static Clans cc = null; 
		bool newSaveDetected = false; 
		
		List<ulong> manuallyEnabledBy = new List<ulong>(); 
		HashSet<ulong> bypass = new HashSet<ulong>(); 
		Dictionary<string, DateTime> notificationTimes = new Dictionary<string, DateTime>();  
		
		static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc); 
		static readonly double MaxUnixSeconds = (DateTime.MaxValue - UnixEpoch).TotalSeconds;  
		
		FieldInfo displayName = typeof(BasePlayer).GetField("_displayName", (BindingFlags.Instance | BindingFlags.NonPublic)); 
		FieldInfo name = typeof(BasePlayer).GetField("_name", (BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));  
		Library lib; 
		MethodInfo isInstalled; 
		MethodInfo addFriend;  
		
		public Dictionary<string, Clan> clans = new Dictionary<string, Clan>(); 
		
		List<string> purgedClans = new List<string>(); 
		Dictionary<string, string> originalNames = new Dictionary<string, string>(); 
		Dictionary<string, List<string>> pendingPlayerInvites = new Dictionary<string, List<string>>(); 
		
		Regex tagReExt; 
		
		Dictionary<string, Clan> clanCache = new Dictionary<string, Clan>();  
		List<object> filterDefaults() 
		{ 
			var dp = new List<object>(); 
				dp.Add("admin"); 
				dp.Add("mod"); 
				dp.Add("owner"); 
			return dp; 
		}  
		
		public int limitMembers; 
		int limitModerators; 
		public int limitAlliances; 
		int tagLengthMin; 
		int tagLengthMax; 
		int inviteValidDays; 
		int friendlyFireNotifyTimeout; 		
		string allowedSpecialChars;  		
		public bool enableFFOPtion; 
		bool enableAllyFFOPtion; 
		bool enableWordFilter; 
		bool enableClanTagging; 
		public bool enableClanAllies; 
		bool forceAllyFFNoDeactivate;  		
		int authLevelRename;
		int authLevelDelete; 
		int authLevelInvite;
		int authLevelKick; 
		int authLevelPromoteDemote; 
		int authLevelClanInfo;  		
		bool purgeOldClans; 
		int notUpdatedSinceDays; 
		bool listPurgedClans; 
		bool wipeClansOnNewSave; 		
		string consoleName; 
		string broadcastPrefix; 
		string broadcastPrefixAlly; 
		string broadcastPrefixColor; 
		string broadcastPrefixFormat; 
		string broadcastMessageColor; 
		string colorCmdUsage; 
		string colorTextMsg; 
		string colorClanNamesOverview; 
		string colorClanFFOff; 
		string colorClanFFOn; 
		string pluginPrefix; 
		string pluginPrefixColor; 
		string pluginPrefixREBORNColor; 
		string pluginPrefixFormat;  
		string clanServerColor; 
		string clanOwnerColor; 
		string clanCouncilColor; 
		string clanModeratorColor; 
		string clanMemberColor;  
		bool setHomeOwner; 
		bool setHomeModerator; 
		bool setHomeMember;  
		string chatCommandClan; 
		string chatCommandFF; 
		string chatCommandAllyChat;
		string chatCommandClanChat; 
		string chatCommandClanInfo;  
		bool usePermGroups; 
		string permGroupPrefix; 
		bool usePermToCreateClan; 
		string permissionToCreateClan;  
		bool addClanMembersAsIOFriends;
		bool enableRustIOSupport;  
		string clanTagColorBetterChat; 
		int clanTagSizeBetterChat; 
		string clanTagOpening; string clanTagClosing;  
		
		List<object> wordFilter = new List<object>();  
		
		object GetConfig(string menu, string datavalue, object defaultValue) 
		{ 
			var data = Config[menu] as Dictionary<string, object>; 
			
			if (data == null) 
			{ 
				data = new Dictionary<string, object>(); 
				Config[menu] = data; 
				Changed = true; 				
			} 
			
			object value; 
			
			if (!data.TryGetValue(datavalue, out value)) 
			{ 
				value = defaultValue; 
				data[datavalue] = value; 
				Changed = true; 				
			} 
			return value; 		
		}  
		
		void LoadVariables() 
		{ 
			wordFilter = (List<object>)GetConfig("WordFilter", "Words", filterDefaults());  
			
			limitMembers = Convert.ToInt32(GetConfig("Limits", "limitMembers", 8)); 
			limitModerators = Convert.ToInt32(GetConfig("Limits", "limitModerators", 2)); 
			limitAlliances = Convert.ToInt32(GetConfig("Limits", "limitAlliances", 2)); 
			tagLengthMin = Convert.ToInt32(GetConfig("Limits", "tagLengthMin", 2)); 
			tagLengthMax = Convert.ToInt32(GetConfig("Limits", "tagLengthMax", 6)); 
			inviteValidDays = Convert.ToInt32(GetConfig("Limits", "inviteValidDays", 1)); 
			friendlyFireNotifyTimeout = Convert.ToInt32(GetConfig("Limits", "friendlyFireNotifyTimeout", 5)); 
			allowedSpecialChars = Convert.ToString(GetConfig("Limits", "allowedSpecialChars", "!²³")); 
			
			enableFFOPtion = Convert.ToBoolean(GetConfig("Settings", "enableFFOPtion", true)); 
			enableAllyFFOPtion = Convert.ToBoolean(GetConfig("Settings", "enableAllyFFOPtion", true)); 
			forceAllyFFNoDeactivate = Convert.ToBoolean(GetConfig("Settings", "forceAllyFFNoDeactivate", true)); 
			enableWordFilter = Convert.ToBoolean(GetConfig("Settings", "enableWordFilter", true)); 
			enableClanTagging = Convert.ToBoolean(GetConfig("Settings", "enableClanTagging", true));
			enableClanAllies = Convert.ToBoolean(GetConfig("Settings", "enableClanAllies", false));  
			
			setHomeOwner = Convert.ToBoolean(GetConfig("NTeleportation", "setHomeOwner", true)); 
			setHomeModerator = Convert.ToBoolean(GetConfig("NTeleportation", "setHomeModerator", true));
			setHomeMember = Convert.ToBoolean(GetConfig("NTeleportation", "setHomeMember", true));  
			
			authLevelRename = Convert.ToInt32(GetConfig("Permission", "authLevelRename", 1)); 
			authLevelDelete = Convert.ToInt32(GetConfig("Permission", "authLevelDelete", 2)); 
			authLevelInvite = Convert.ToInt32(GetConfig("Permission", "authLevelInvite", 1)); 
			authLevelKick = Convert.ToInt32(GetConfig("Permission", "authLevelKick", 2)); 			
			authLevelPromoteDemote = Convert.ToInt32(GetConfig("Permission", "authLevelPromoteDemote", 1));
			authLevelClanInfo = Convert.ToInt32(GetConfig("Permission", "authLevelClanInfo", 0));  
			usePermGroups = Convert.ToBoolean(GetConfig("Permission", "usePermGroups", false)); 
			permGroupPrefix = Convert.ToString(GetConfig("Permission", "permGroupPrefix", "clan_")); 
			usePermToCreateClan = Convert.ToBoolean(GetConfig("Permission", "usePermToCreateClan", false)); 
			permissionToCreateClan = Convert.ToString(GetConfig("Permission", "permissionToCreateClan", "clans.cancreate"));  
			
			purgeOldClans = Convert.ToBoolean(GetConfig("Purge", "purgeOldClans", false)); 
			notUpdatedSinceDays = Convert.ToInt32(GetConfig("Purge", "notUpdatedSinceDays", 14)); 
			listPurgedClans = Convert.ToBoolean(GetConfig("Purge", "listPurgedClans", false)); 
			wipeClansOnNewSave = Convert.ToBoolean(GetConfig("Purge", "wipeClansOnNewSave", false));  
			
			consoleName = Convert.ToString(GetConfig("Formatting", "consoleName", "ServerOwner")); 
			broadcastPrefix = Convert.ToString(GetConfig("Formatting", "broadcastPrefix", "[КЛАН]")); 
			broadcastPrefixAlly = Convert.ToString(GetConfig("Formatting", "broadcastPrefixAlly", "[СОЮЗ]")); 
			broadcastPrefixColor = Convert.ToString(GetConfig("Formatting", "broadcastPrefixColor", "#ff9900")); 
			broadcastPrefixFormat = Convert.ToString(GetConfig("Formatting", "broadcastPrefixFormat", "<color={0}>{1}</color> ")); 
			broadcastMessageColor = Convert.ToString(GetConfig("Formatting", "broadcastMessageColor", "#ffffff")); 
			colorCmdUsage = Convert.ToString(GetConfig("Formatting", "colorCmdUsage", "#ffd479")); 
			colorTextMsg = Convert.ToString(GetConfig("Formatting", "colorTextMsg", "#ffffff")); 
			colorClanNamesOverview = Convert.ToString(GetConfig("Formatting", "colorClanNamesOverview", "orange"));
			colorClanFFOff = Convert.ToString(GetConfig("Formatting", "colorClanFFOff", "lime")); 
			colorClanFFOn = Convert.ToString(GetConfig("Formatting", "colorClanFFOn", "#47d147")); 
			pluginPrefix = Convert.ToString(GetConfig("Formatting", "pluginPrefix", "КЛАНЫ")); 
			pluginPrefixColor = Convert.ToString(GetConfig("Formatting", "pluginPrefixColor", "orange")); 
			pluginPrefixREBORNColor = Convert.ToString(GetConfig("Formatting", "pluginPrefixREBORNColor", "#ce422b")); 
			pluginPrefixFormat = Convert.ToString(GetConfig("Formatting", "pluginPrefixFormat", "<color={0}>{1}</color>: ")); 
			clanServerColor = Convert.ToString(GetConfig("Formatting", "clanServerColor", "#ff3333")); 
			clanOwnerColor = Convert.ToString(GetConfig("Formatting", "clanOwnerColor", "#a1ff46")); 
			clanCouncilColor = Convert.ToString(GetConfig("Formatting", "clanCouncilColor", "#b573ff")); 
			clanModeratorColor = Convert.ToString(GetConfig("Formatting", "clanModeratorColor", "#74c6ff")); 
			clanMemberColor = Convert.ToString(GetConfig("Formatting", "clanMemberColor", "#fcf5cb"));  
			
			clanTagColorBetterChat = Convert.ToString(GetConfig("BetterChat", "clanTagColorBetterChat", "#66cc00"));
			clanTagSizeBetterChat = Convert.ToInt32(GetConfig("BetterChat", "clanTagSizeBetterChat", 15)); 
			clanTagOpening = Convert.ToString(GetConfig("BetterChat", "clanTagOpening", "[")); 
			clanTagClosing = Convert.ToString(GetConfig("BetterChat", "clanTagClosing", "]"));  
			
			chatCommandClan = Convert.ToString(GetConfig("Commands", "chatCommandClan", "clan")); 
			chatCommandFF = Convert.ToString(GetConfig("Commands", "chatCommandFF", "cff")); 
			chatCommandAllyChat = Convert.ToString(GetConfig("Commands", "chatCommandAllyChat", "a")); 
			chatCommandClanChat = Convert.ToString(GetConfig("Commands", "chatCommandClanChat", "c")); 
			chatCommandClanInfo = Convert.ToString(GetConfig("Commands", "chatCommandClanInfo", "cinfo"));  
			
			addClanMembersAsIOFriends = Convert.ToBoolean(GetConfig("RustIO", "addClanMembersAsIOFriends", true)); 
			enableRustIOSupport = Convert.ToBoolean(GetConfig("RustIO", "enableRustIOSupport", false));  
		
		
		if (!Changed) 
			return; 
		
		SaveConfig(); 
		Changed = false; 
	}  
	
	protected override void LoadDefaultConfig() 
	{ 
		Config.Clear(); 
		LoadVariables(); 
	}  
	void LoadDefaultMessages() 
	{ 
		lang.RegisterMessages(new Dictionary<string, string> 
		{ 
			{"nopermtocreate", "У вас нет прав на создание клана."}, 
			{"claninvite", "<color=white>Вас пригласили присоединиться к клану:</color> [<color=#ffd479>{0}</color>] '<color=#ffd479>{1}</color>'\n<color=white>Чтобы присоединиться, введите:</color> <color=#ffd479>/clan join {0}</color>"}, 
			{"comeonline", "<color=#ffd479>{0}</color> <color=white>зашёл на сервер!</color>"},
			{"goneoffline", "<color=#ffd479>{0}</color> <color=white>покинул сервер!</color>"}, 
			{"friendlyfire", "<color=#ffd479>{0}</color> <color=white>является участником клана.\nЧтобы переключить дружественный огонь для клана, введите</color> <color=#ffd479>/clan ff</color>"}, 
			{"allyfriendlyfire", "<color=#ffd479>{0}</color> <color=white>является союзником клана.</color>"}, 
			{"notmember", "<color=white>На данный момент вы не состоите в клане.</color>"}, 
			{"youareownerof", "<color=white>Вы являетесь владельцем: </color>"},
			{"youaremodof", "<color=white>Вы являетесь модератором: </color>"}, 
			{"youarecouncilof", "<color=white>Вы являетесь советом: </color>"}, 
			{"youarememberof", "<color=white>Вы являетесь участником: </color>"}, 
			{"claninfo", " [{0}] {1}"}, 
			{"memberon", "<color=white>В сети: </color>"}, 
			{"overviewnamecolor", "<color={0}>{1}</color>"}, 
			{"memberoff", "<color=white>Оффлайн: </color>"}, 
			{"notmoderator", "<color=white>Это команда доступна модератору и владельцу клана.</color>"},
			{"pendinvites", "<color=white>Приглашения в ожидании: </color>"}, 
			{"bannedwords", "<color=white>Тег клана содержит запрещенные слова.</color>"}, 
			{"viewthehelp", "<color=white>Чтобы узнать больше о кланах, введите: </color><color=#ffd479>/{1} help</color>"}, 
			{"usagecreate", "<color=white>Используйте - </color><color=#ffd479>/clan create <ТЕГ> <описание></color>"}, 
			{"hintlength", "Тег клана должен содержать от <color=#ffd479>{0}</color> до <color=#ffd479>{1}</color> символов"}, 
			{"hintchars", "<color=white>Тег клана должен содержать только такие символы как: '</color><color=#ffd479>a-z</color><color=white>' '</color><color=#ffd479>A-Z</color><color=white>' '</color><color=#ffd479>0-9</color><color=white>' '</color><color=#ffd479>а-я</color><color=white>' '</color><color=#ffd479>А-Я</color><color=white>' '</color><color=#ffd479>{0}</color><color=white>'</color>"}, 
			{"providedesc", "<color=white>Напишите краткое описание вашего клана.</color>"}, 
			{"tagblocked", "<color=white>Какой-то клан уже использует этот тег.</color>"}, 
			{"nownewowner", "<color=white>Теперь вы владелец клана</color> <color=#ffd479>{0}</color>\n<color=white>Описание клана:</color> <color=#ffd479>{1}</color>"}, 
			{"inviteplayers", "<color=white>Чтобы пригласить игрока, введите:</color> <color=#ffd479>/clan invite <игрок></color>"}, 
			{"usageinvite", "<color=white>Используйте - </color><color=#ffd479>/clan invite <игрок></color>"}, 
			{"nosuchplayer", "<color=white>Игрока с никнеймом</color> <color=#ffd479>{0}</color> <color=white>нет, либо вы ввели неправильно никнейм.</color>"}, 
			{"alreadymember", "<color=white>Игрок</color> <color=#ffd479>{0}</color> <color=white>уже состоит в вашем клане.</color>"}, 
			{"alreadyinvited", "<color=white>Игрок</color> <color=#ffd479>{0}</color> <color=white>уже приглашен в ваш клан.</color>"}, 
			{"alreadyinclan", "<color=white>Игрок</color> <color=#ffd479>{0}</color> <color=white>уже состоит в клане.</color>"}, 
			{"invitebroadcast", "Игрок <color=#ffd479>{0}</color> пригласил <color=#ffd479>{1}</color> в клан."}, 
			{"usagewithdraw", "<color=white>Используйте - </color><color=#ffd479>/clan withdraw <игрок></color>"}, 
			{"notinvited", "<color=white>Игрок</color> <color=#ffd479>{0}</color> <color=white>не приглашен в ваш клан.</color>"}, 
			{"canceledinvite", "<color=#ffd479>{0}</color> отменил приглашение <color=#ffd479>{1}</color>"}, 
			{"usagejoin", "<color=white>Используйте - </color><color=#ffd479>/clan join <ТЕГ></color>"}, 
			{"youalreadymember", "<color=white>Вы уже состоите в клане.</color>"}, 
			{"younotinvited", "<color=white>Вас не приглашали в этот клан.</color>"}, 
			{"reachedmaximum", "<color=white>В этом клане достигнуто максимальное количество участников.</color>"}, 
			{"broadcastformat", "<color={0}>{1}</color>: {2}"}, 
			{"allybroadcastformat", "[{0}] <color={1}>{2}</color>: {3}"}, 
			{"clanrenamed", "<color=#ffd479>{0}</color> переименовал клан на: [<color=#ffd479>{1}</color>]"}, 
			{"yourenamed", "Вы переименовали клан [<color=#ffd479>{0}</color>] на [<color=#ffd479>{1}</color>]"}, 
			{"clandeleted", "<color=#ffd479>{0}</color> удалил ваш клан."}, 
			{"youdeleted", "Вы удалили клан [<color=#ffd479>{0}</color>]"}, 
			{"noclanfound", "Клана с тегом [<color=#ffd479>{0}</color>] не существует"}, 
			{"renamerightsowner", "Вам нужно быть владельцем сервера, чтобы переименовать клан."}, 
			{"usagerename", "<color=white>Используйте - </color><color=#ffd479>/clan rename <СТАРЫЙ ТЕГ> <НОВЫЙ ТЕГ></color>"}, 
			{"deleterightsowner", "Вы должны быть владельцем сервера, чтобы удалить клан."}, 
			{"usagedelete", "<color=white>Используйте - </color><color=#ffd479>/clan delete <ТЕГ></color>"}, 
			{"clandisbanded", "<color=white>Вы распустили свой клан.</color>"},
			{"needclanowner", "Вы должны быть владельцем сервера, чтобы использовать эту команду."}, 
			{"needclanownercouncil", "Вы должны быть владельцем или советом, чтобы использовать эту команду."}, 
			{"usagedisband", "<color=white>Используйте - </color><color=#ffd479>/clan disband forever</color>"}, 
			{"usagepromote", "<color=white>Используйте - </color><color=#ffd479>/clan promote <игрок></color>"}, 
			{"playerjoined", "<color=#ffd479>{0}</color> зашёл в клан!"}, 
			{"waskicked", "<color=#ffd479>{0}</color> выгнал <color=#ffd479>{1}</color> из клана."}, 
			{"modownercannotkicked", "Игрок <color=#ffd479>{0}</color> является модератором или владельцем клана и его нельзя выгнать."}, 
			{"notmembercannotkicked", "Игрок <color=#ffd479>{0}</color> не участник вашего клана."}, 
			{"usageff", "<color=white>Используйте - </color><color=#ffd479>/clan ff</color> Изменить статус дружественного огня."}, 
			{"usagekick", "<color=white>Используйте - </color><color=#ffd479>/clan kick <игрок></color>"}, 
			{"playerleft", "<color=orange>{0}</color> <color=white>покинул клан.</color>"},
			{"youleft", "<color=white>Вы покинули свой клан.</color>"}, 
			{"usageleave", "<color=white>Используйте - </color><color=#ffd479>/clan leave</color>"}, 
			{"notaclanmember", "Игрок <color=#ffd479>{0}</color> не участник вашего клана."}, 
			{"alreadyamod", "Игрок <color=#ffd479>{0}</color> уже является модератором вашего клана."}, 
			{"alreadyacouncil", "Игрок <color=#ffd479>{0}</color> уже является советом вашего клана."}, 
			{"alreadyacouncilset", "Должность совета уже присуждена."}, 
			{"maximummods", "В этом клане уже достигнуто максимальное количество модераторов."}, 
			{"playerpromoted", "Игрок <color=#ffd479>{0}</color> повысил <color=#ffd479>{1}</color> до модератора."}, 
			{"playerpromotedcouncil", "Игрок <color=#ffd479>{0}</color> повысил <color=#ffd479>{1}</color> до совета."}, 
			{"usagedemote", "<color=white>Используйте - </color><color=#ffd479>/clan demote <игрок></color>"}, 
			{"notamoderator", "Игрок <color=#ffd479>{0}</color> не модератор вашего клана."}, 
			{"notpromoted", "Игрок <color=#ffd479>{0}</color> не модератор/совет вашего клана."}, 
			{"playerdemoted", "Игрок <color=#ffd479>{0}</color> понизил <color=#ffd479>{1}</color> до участника."}, 
			{"councildemoted", "Игрок <color=#ffd479>{0}</color> понизил <color=#ffd479>{1}</color> до модератора."}, 
			{"noactiveally", "У вашего клана нет союзов."}, 
			{"yourffstatus", "Дружественный огонь:"}, 
			{"yourclanallies", "Ваши союзные кланы:"},
			{"allyinvites", "Приглашения в союз:"}, 
			{"allypending", "Запросы союзника:"}, 
			{"allyReqHelp", "Предложить союз другому клану"}, 
			{"allyAccHelp", "Принять союз с другим кланом"}, 
			{"allyDecHelp", "Отклонить союз от другого клана"}, 
			{"allyCanHelp", "Отменить союз с другим кланом"}, 
			{"reqAlliance", "[<color=#ffd479>{0}</color>] отправил вам запрос на союз"}, 
			{"invitePending", "У вас уже есть приглашение союза с [<color=#ffd479>{0}</color>]"}, 
			{"clanNoExist", "Клана [<color=#ffd479>{0}</color>] не существует"}, 
			{"alreadyAllies", "Вы уже союзники с"}, 
			{"allyProvideName", "Необходимо указать название клана"}, 
			{"allyLimit", "Вы достигли максимального количества союзников клана"}, 
			{"allyAccLimit", "Вы не можете принять союз с <color=#ffd479>{0}</color>. Вы достигли предела"}, 
			{"allyCancel", "Вы отменили свой союз с [<color=#ffd479>{0}</color>]"}, 
			{"allyCancelSucc", "[<color=#ffd479>{0}</color>] отменил ваш клановый союз"}, 
			{"noAlly", "У вас в клане нету союзников"}, 
			{"noAllyInv", "У вас нет союз приглашения от [<color=#ffd479>{0}</color>]"}, 
			{"allyInvWithdraw", "Вы отменили свой запрос на союз с [<color=#ffd479>{0}</color>]"}, 
			{"allyDeclined", "Вы отказались от кланового союза с [<color=#ffd479>{0}</color>]"}, 
			{"allyDeclinedSucc", "[<color=#ffd479>{0}</color>] отклонил ваш запрос на союз"}, 
			{"allyReq", "Вы отправили запрос на клановый союз с [<color=#ffd479>{0}</color>]"}, 
			{"allyAcc", "Вы приняли клановый союз с [<color=#ffd479>{0}</color>]"}, 
			{"allyAccSucc", "[<color=#ffd479>{0}</color>] принял ваш запрос на союз"}, 
			{"allyPendingInfo", "У вашго клана есть запрос на союз."}, 
			{"clanffdisabled", "Вы <color={0}>выключили</color> дружественный огонь для вашего клана."},
			{"clanffenabled", "Вы <color={0}>включили</color> дружественный огонь для вашего клана."}, 
			{"yourname", "ВЫ"}, 
			{ "helpavailablecmds", "Доступные команды:" }, 
			{ "helpinformation", "Показать информацию о вашем клане" }, 
			{ "helpmessagemembers", "Отправить сообщение всем участникам клана" }, 
			{ "helpmessageally", "Отправить сообщение всем участникам клана" }, 
			{ "helpcreate", "Создать новый клан" }, 
			{ "helpjoin", "Вступить в клан по приглашению" }, 
			{ "helpleave", "Покинуть клан" }, 
			{ "helptoggleff", "Изменить статус дружественного огня" }, 
			{ "helpinvite", "Пригласить игрока в клан" }, 
			{ "helpwithdraw", "Отменить приглашение в клан" }, 
			{ "helpkick", "Выгнать игрока из клана" }, 
			{ "helpallyoptions", "Список опций союза" }, 
			{ "helppromote", "Повысить участника клана до модератора" }, 
			{ "helpdemote", "Снять привилегию модератора с участника клана" }, 
			{ "helpdisband", "Распустить ваш клан" }, 
			{ "helpconsole", "<color=white>Откройте консоль с помощью</color> <color=#ffd479>F1</color> <color=white>и введите:</color>"}, 
		}, this); 		
	}  
	
	void Init() 
	{ 
		cc = this; 
		LoadVariables(); 
		LoadDefaultMessages(); 
		Initialized = false; 
		
		if (!permission.PermissionExists(permissionToCreateClan)) 
			permission.RegisterPermission(permissionToCreateClan, this); 
		
		cmd.AddChatCommand(chatCommandFF, this, "cmdChatClanFF"); 
		cmd.AddChatCommand(chatCommandClan, this, "cmdChatClan"); 
		cmd.AddChatCommand(chatCommandClanChat, this, "cmdChatClanchat"); 
		cmd.AddChatCommand(chatCommandAllyChat, this, "cmdChatAllychat"); 
		cmd.AddChatCommand(chatCommandClanInfo, this, "cmdChatClanInfo"); 
		cmd.AddChatCommand(chatCommandClan+"help", this, "cmdChatClanHelp"); 
		cmd.AddChatCommand(chatCommandClan+"ally", this, "cmdChatClanAlly"); 
	
		if (enableClanTagging) 
			Interface.CallHook("API_RegisterThirdPartyTitle", this, new Func<IPlayer, string>(getFormattedClanTag));
	}  
	void OnPluginLoaded(Plugin plugin) 
	{ 
		if (plugin.Title != "Better Chat") 
			return; 
		if (enableClanTagging) 
			Interface.CallHook("API_RegisterThirdPartyTitle", this, new Func<IPlayer, string>(getFormattedClanTag));
	}  
	
	string getFormattedClanTag(IPlayer player) 
	{ 
		var clan = findClanByUser(player.Id); 
		
		if (clan != null && !string.IsNullOrEmpty(clan.tag)) 
			return $"[#{clanTagColorBetterChat.Replace("#","")}][+{clanTagSizeBetterChat}]{clanTagOpening}{clan.tag}{clanTagClosing}[/+][/#]"; 
		return string.Empty; 
	}  
	
	void OnServerInitialized() 
	{ 
		if (enableRustIOSupport) 
			ioInitialize(); 
			
		LoadData(); 
		
		if (purgeOldClans) 
			Puts($"Valid clans loaded: '{clans.Count}'"); 
		if (purgeOldClans && purgedClans.Count() > 0) 
		{ 
			Puts($"Old Clans purged: '{purgedClans.Count}'"); 
			
			if (listPurgedClans) 
			{ 
				foreach (var purged in purgedClans) Puts($"Purged > {purged}"); 
			} 
		} 
		AllyRemovalCheck(); 
		tagReExt = new Regex("[^a-zA-Z0-9" + allowedSpecialChars + "]"); 
	
		foreach (var player in BasePlayer.activePlayerList) setupPlayer(player); 
		foreach (var player in BasePlayer.sleepingPlayerList) setupPlayer(player); 
		
		Initialized = true; 		
	}  
	void OnServerSave() 
	{ 
		SaveData(); 		
	}  
	void OnNewSave() 
	{ 
		if (wipeClansOnNewSave) 
			newSaveDetected = true; 		
	}  
	void Unload() 
	{ 
		if (!Initialized) 
			return; 
			
		SaveData(); 
		
		foreach (var pair in originalNames) 
		{ 
			var player = rust.FindPlayerByIdString(pair.Key); 
			
			if (player != null && player.displayName != pair.Value) 
			{ 
				displayName.SetValue(player, pair.Value); 
				
				if (player.net != null) 
					name.SetValue(player, string.Format("{1}[{0}/{2}]", player.net.ID, pair.Value, player.userID)); 
					
				player.SendNetworkUpdate(); 				
			} 			
		}						
	}  
	void ioInitialize() 
	{ 
		lib = Interface.GetMod().GetLibrary<Library>("RustIO"); 
		
		if (lib == null || (isInstalled = lib.GetFunction("IsInstalled")) == null || (addFriend = lib.GetFunction("AddFriend")) == null) 
		{ 
			lib = null; 
			Puts("{0}: {1}", Title, "Rust:IO is not present. You need to install Rust:IO first in order to use this plugin!"); 			
		} 		
	}  
	
	bool ioIsInstalled() 
	{ 
		if (lib == null) 
			return false; 
		
		return (bool)isInstalled.Invoke(lib, new object[] { }); 	
	}  
	bool ioAddFriend(string playerId, string friendId) 
	{ 
		if (lib == null) 
			return false; 
			
		return (bool)addFriend.Invoke(lib, new object[] { playerId, friendId }); 		
	}  
	
	void LoadData() 
	{ 
		if (wipeClansOnNewSave && newSaveDetected) 
		{ 
			var newdata = Interface.GetMod().DataFileSystem.GetDatafile("rustio_clans"); 
			
			newdata.Save(Interface.GetMod().DataFileSystem.Directory+ "/rustio_clans.old"); 
			clans.Clear(); 
			newdata["clans"] = clans; 
			Interface.GetMod().DataFileSystem.SaveDatafile("rustio_clans"); 
			Puts("New save detected > Created backup of clans and wiped datafile."); 
			return; 		
		} 
		Puts("Loading clans data."); 
		clans.Clear(); 
		
		var data = Interface.GetMod().DataFileSystem.GetDatafile("rustio_clans"); 
		
		if (data["clans"] != null) 
		{ 
			Dictionary<string, object> clansData = new Dictionary<string, object>(); 
			
			try 
			{ 
				clansData = (Dictionary<string, object>)Convert.ChangeType(data["clans"], typeof(Dictionary<string, object>)); 				
			} 
			catch { } 
			
			foreach (var iclan in clansData) 
			{ 
				string tag = iclan.Key; 
				string permGroup = permGroupPrefix + tag; 
				
				if (usePermGroups && !permission.GroupExists(permGroup)) 
					permission.CreateGroup(permGroup, "Clan " + tag, 0); 
					
				var clanData = iclan.Value as Dictionary<string, object>; 
				
				string description = (string)clanData["description"]; 
				string owner = (string)clanData["owner"];  
				string council = null; 
				
				if (clanData.ContainsKey("council")) 
					council = (string)clanData["council"]; 
				if (!enableClanAllies) 
					council = null; List<string> moderators = new List<string>(); 
					
				foreach (var imoderator in clanData["moderators"] as List<object>) 
				{ 
					moderators.Add((string)imoderator); 
				} 
				
				List<string> members = new List<string>(); 
				
				foreach (var imember in clanData["members"] as List<object>) 
				{ 
					members.Add((string)imember); 
					
					if (usePermGroups && !permission.UserHasGroup((string)imember, permGroup)) 
						permission.AddUserGroup((string)imember, permGroup); 						
				} 
				
				List<string> clanAlliances = new List<string>(); 
				
				if (enableClanAllies && clanData.ContainsKey("clanAlliances")) 
					foreach (var iAlly in clanData["clanAlliances"] as List<object>) 
					{ 
						clanAlliances.Add((string)iAlly); 
					} 
					
				List<string> invitedAllies = new List<string>(); 
				
				if (enableClanAllies && clanData.ContainsKey("invitedAllies")) 
					foreach (var iInvited in clanData["invitedAllies"] as List<object>) 
					{ 
						invitedAllies.Add((string)iInvited); 					
					}  
					
				List<string> pendingInvites = new List<string>(); 
				
				if (enableClanAllies && clanData.ContainsKey("pendingInvites")) 
					foreach (var iPending in clanData["pendingInvites"] as List<object>) 
					{ 
						pendingInvites.Add((string)iPending); 
					}  
					
				Dictionary<string, int> invites = new Dictionary<string, int>(); 
				
				try 
				{ 
					foreach (var iinvited in clanData["invites"] as Dictionary<string, object>) 
					{ 
						if ((UnixTimeStampUTC() - (int)iinvited.Value) < (inviteValidDays * 86400)) 
							invites.Add((string)iinvited.Key, (int)iinvited.Value); 							
					} 					
				} 
				catch {} 
				
				object time; 
				
				int created = 0; 
				int updated = 0; 
				
				if (clanData.TryGetValue("created", out time)) 
					created = (int)time; 
				if (created == 0) 
					created = UnixTimeStampUTC(); 
					
				time = null; 
				
				if (clanData.TryGetValue("updated", out time)) 
					updated = (int)time; 
				if (updated == 0) 
					updated = UnixTimeStampUTC();  
					
				Clan clan; 
				
				if (purgeOldClans && (UnixTimeStampUTC() - updated) > (notUpdatedSinceDays * 86400)) 
				{ 
					purgedClans.Add($"[{tag}] | {description} | Owner: {owner} | LastUpd: {UnixTimeStampToDateTime(updated)}"); 
					
					if (permission.GroupExists(permGroup)) 
					{ 
					foreach (var member in members) if (permission.UserHasGroup(member, permGroup)) permission.RemoveUserGroup(member, permGroup); 
					
					permission.RemoveGroup(permGroup); 					
					} 
					continue; 					
				} 
				
				clans.Add(tag, clan = new Clan() 
				{ 
					tag = tag, 
					description = description, 					
					owner = owner, 
					council = council, 
					moderators = moderators, 
					members = members, 
					invites = invites, 
					clanAlliances = clanAlliances, 
					invitedAllies = invitedAllies, 
					pendingInvites = pendingInvites, 
					created = created, 
					updated = updated, 
					total = members.Count(), 
					mods = moderators.Count() 															
				}); 
				clanCache[owner] = clan; 
				
				foreach (var member in members) clanCache[member] = clan; 
				foreach (var invite in invites) 
				{ 
					if (!pendingPlayerInvites.ContainsKey(invite.Key)) 
						pendingPlayerInvites.Add(invite.Key, new List<string>()); 
						
					pendingPlayerInvites[invite.Key].Add(tag); 					
				} 					
			} 					
		} 
		
		Puts($"Loaded data with '{clans.Count}' valid Clans and overall '{clanCache.Count}' Members."); 		
	}  
	
	void SaveData() 
	{ 
		if (!Initialized) 
			return; 
			
		var data = Interface.GetMod().DataFileSystem.GetDatafile("rustio_clans"); 
			data.Clear(); 
			data["clans"] = clans; 
			
		Interface.GetMod().DataFileSystem.SaveDatafile("rustio_clans"); 		
	}  
	
	public Clan findClan(string tag) 
	{ 
		Clan clan; 
		
		if (clans.TryGetValue(tag, out clan)) 
			return clan; 
		
		return null; 		
	}  
	public Clan findClanByUser(string userId) 
	{ 
		Clan clan; 
		
		if (clanCache.TryGetValue(userId, out clan)) 
			return clan; 
			
		return null; 	
	}  
	
	void setupPlayer(BasePlayer player, string cName = "", string cId = "") 
	{ 
		if (player == null || player.UserIDString == "" || player.displayName == "") 
			return; 
		if (cName == "" || cId == "") 
		{ 
		var current = this.covalence.Players.FindPlayer(player.UserIDString); 
			
		if (current == null) 
			return; 
				
		cName = current.Name; 
		cId = current.Id; 			
		} 
		if (!originalNames.ContainsKey(cId)) 
			originalNames.Add(cId, cName); 
		else 
			originalNames[cId] = cName; 
			
		var prevName = player.displayName; 
		var clan = findClanByUser(cId); 
		
		if (clan == null) 
		{ 
			if (enableClanTagging) 
			{
				displayName.SetValue(player, cName); 
				
				if (player.IsConnected) 
					name.SetValue(player, string.Format("{1}[{0}/{2}]", player.net.ID, cName, player.userID)); 				
			} 			
		} 
		else 
		{ 
			if (enableClanTagging) 
				displayName.SetValue(player, $"[{clan.tag}] {cName}"); 
			if (player.IsConnected) 
			{ 
				if (enableClanTagging) 
					name.SetValue(player, string.Format("{1}[{0}/{2}]", player.net.ID, $"[{clan.tag}] {cName}", player.userID)); 
				
				clan.online++; 		
			} 		
		} 
		if (enableClanTagging && player.displayName != prevName) 
			player.SendNetworkUpdate(); 				
	}  
	void setupPlayers(List<string> playerIds) 
	{ 
		if (enableClanTagging) foreach (var playerId in playerIds) 
		{ 
			var player = rust.FindPlayerByIdString(playerId); 
			
			if (player != null) 
				setupPlayer(player); 		
		} 	
	}  
	void OnPlayerInit(BasePlayer player) 
	{
		if (player == null || player.net == null || player.net.connection == null) 
			return; 
			
		displayName.SetValue(player, player.net.connection.username); 
		setupPlayer(player, player.net.connection.username, player.UserIDString); 
		
		var clan = findClanByUser(player.UserIDString); 
		
		if (clan != null) 
		{
			clan.BroadcastLoc("comeonline", clan.ColNam(player.UserIDString, player.net.connection.username), "", "", "", player.UserIDString); 
			
			var sb = new StringBuilder(); 
				sb.Append($"<color={colorTextMsg}>"); 
				sb.Append(string.Format(msg("<color=white>В сети: </color>", player.UserIDString))); 
				
			int n = 0; 
			
			foreach (var memberId in clan.members) 
			{ 
				var op = this.covalence.Players.FindPlayer(memberId); 
				
				if (op != null && op.IsConnected) 
				{
					var memberName = op.Name; 
					
					if (op.Name == player.net.connection.username) 
						memberName = msg("ВЫ", player.UserIDString); 
					if (n > 0) 
						sb.Append(", "); 
					if (clan.IsOwner(memberId)) 
					{
						sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanOwnerColor, memberName)); 					
					} 
					else if (clan.IsCouncil(memberId)) 
					{
						sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanCouncilColor, memberName)); 				
					} 
					else if (clan.IsModerator(memberId)) 
					{
						sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanModeratorColor, memberName)); 
					} 
					else 
					{
						sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanMemberColor, memberName)); 
					} 
					++n; 				
				} 		
			} 
			sb.Append($"</color>"); 
			PrintChat(player, sb.ToString().TrimEnd()); 
			clan.updated = UnixTimeStampUTC(); 
			manuallyEnabledBy.Remove(player.userID); 
			
			if (enableClanAllies && (clan.IsOwner(player.UserIDString) || clan.IsCouncil(player.UserIDString)) && clan.pendingInvites.Count > 0) 
			{
				if (player != null) 
					PrintChat(player, string.Format(msg("У вашго клана есть запрос на союз.", player.UserIDString))); 
			} 
			return; 		
		} 
		if (pendingPlayerInvites.ContainsKey(player.UserIDString)) 
		{
			foreach (var invitation in pendingPlayerInvites[player.UserIDString] as List<string>) 
			{
				Clan newclan = findClan(invitation); 
				
				if (newclan != null) 
					timer.Once(3f, () => 
					{ 
						if (player != null) 
							PrintChat(player, string.Format(msg("<color=white>Вас пригласили присоединиться к клану:</color> [<color=#ffd479>{0}</color>] '<color=#ffd479>{1}</color>'\n<color=white>Чтобы присоединиться, введите:</color> <color=#ffd479>/clan join {0}</color>", player.UserIDString), newclan.tag, newclan.description, colorCmdUsage)); 
					}); 
			}
		} 	
	}  
	void OnPlayerDisconnected(BasePlayer player) 
	{
		var clan = findClanByUser(player.UserIDString); 
		
		if (clan != null) 
		{
			clan.BroadcastLoc("goneoffline", clan.ColNam(player.UserIDString, player.net.connection.username), "", "", "", player.UserIDString); 
			clan.online--; 
			manuallyEnabledBy.Remove(player.userID); 
		} 
	}  
	void OnPlayerAttack(BasePlayer attacker, HitInfo hit) 
	{
		if (!enableFFOPtion || attacker == null || hit == null || !(hit.HitEntity is BasePlayer)) 
			return; 
			
		OnAttackShared(attacker, hit.HitEntity as BasePlayer, hit); 
	}  
	void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hit) 
	{
		if (!enableFFOPtion || entity == null || hit == null || !(entity is BasePlayer) || !(hit.Initiator is BasePlayer)) 
			return; 
			
		OnAttackShared(hit.Initiator as BasePlayer, entity as BasePlayer, hit); 
	}  
	
	object OnAttackShared(BasePlayer attacker, BasePlayer victim, HitInfo hit) 
	{
		if (bypass.Contains(victim.userID) || attacker == victim) 
			return null; 
			
		var victimClan = findClanByUser(victim.UserIDString); 
		var attackerClan = findClanByUser(attacker.UserIDString); 
		
		if (victimClan == null || attackerClan == null) 
			return null; 
		if (victimClan.tag == attackerClan.tag) 
		{
			if (manuallyEnabledBy.Contains(attacker.userID)) 
				return null; 
				
			DateTime now = DateTime.UtcNow; 
			DateTime time; 
			
			var key = attacker.UserIDString + "-" + victim.UserIDString; 
			
			if (!notificationTimes.TryGetValue(key, out time) || time < now.AddSeconds(-friendlyFireNotifyTimeout)) 
			{
				PrintChat(attacker, string.Format(msg("<color=#ffd479>{0}</color> <color=white>является участником клана.\nЧтобы переключить дружественный огонь для клана, введите</color> <color=#ffd479>/clan ff</color>", attacker.UserIDString), victim.displayName, colorCmdUsage)); 
				notificationTimes[key] = now; 
			}
			hit.damageTypes = new DamageTypeList(); 
			hit.DidHit = false; 
			hit.HitEntity = null; 
			hit.Initiator = null; 
			hit.DoHitEffects = false; 
			return false; 
		} 
		if (victimClan.tag != attackerClan.tag && enableClanAllies && enableAllyFFOPtion) 
		{
			if (!victimClan.clanAlliances.Contains(attackerClan.tag)) 
				return null; 
			if (manuallyEnabledBy.Contains(attacker.userID) && !forceAllyFFNoDeactivate) 
				return null; 
				
			DateTime now = DateTime.UtcNow; 
			DateTime time; 
			
			var key = attacker.UserIDString + "-" + victim.UserIDString; 
			
			if (!notificationTimes.TryGetValue(key, out time) || time < now.AddSeconds(-friendlyFireNotifyTimeout)) 
			{
				PrintChat(attacker, string.Format(msg("<color=#ffd479>{0}</color> <color=white>является союзником клана.</color>", attacker.UserIDString), victim.displayName)); 
				notificationTimes[key] = now; 
			} 
			hit.damageTypes = new DamageTypeList(); 
			hit.DidHit = false; 
			hit.HitEntity = null; 
			hit.Initiator = null; 
			hit.DoHitEffects = false; 
			return false; 
		} 
		return null; 
	}  
	void AllyRemovalCheck() 
	{
		foreach (var ally in clans) 
		{ 
			try 
			{ 
				Clan allyClan = clans[ally.Key]; 
				
				foreach (var clanAlliance in allyClan.clanAlliances.ToList()) 
				{
					if (!clans.ContainsKey(clanAlliance)) 
						allyClan.clanAlliances.Remove(clanAlliance); 
				} 
				foreach (var invitedAlly in allyClan.invitedAllies.ToList()) 
				{
					if (!clans.ContainsKey(invitedAlly)) 
						allyClan.clanAlliances.Remove(invitedAlly); 
				} 
				foreach (var pendingInvite in allyClan.pendingInvites.ToList()) 
				{
					if (!clans.ContainsKey(pendingInvite)) 
						allyClan.clanAlliances.Remove(pendingInvite); 
				} 
			} 
			catch 
			{ 
				PrintWarning("Ally removal check failed. Please contact the developer."); 
			} 
		} 
	}  
	void cmdChatClan(BasePlayer player, string command, string[] args) 
	{
		if (player == null) 
			return; 
		if (args.Length == 0) 
		{ 
			cmdClanOverview(player); 
				return; 
		} 
			
		switch (args[0]) 
		{
			case "create": 
				cmdClanCreate(player, args); 
			return; 
			case "invite": 
				cmdClanInvite(player, args); 
			return; 
			case "withdraw": 
				cmdClanWithdraw(player, args); 
			return; 
			case "join": 
				cmdClanJoin(player, args); 
			return; 
			case "promote": 
				cmdClanPromote(player, args); 
			return; 
			case "demote": 
				cmdClanDemote(player, args); 
			return; 
			case "leave": 
				cmdClanLeave(player, args); 
			return; 
			case "ff": 
				if (!enableFFOPtion) 
					goto default; 
				cmdChatClanFF(player, command, args); 
			return; 
			case "ally": 
				if (!enableClanAllies) 
					goto default; 
					
				for (var i = 0; i < args.Length-1; ++i) 
				{
					if (i < args.Length) 
						args[i] = args[i+1]; 
				} 
				
				Array.Resize(ref args, args.Length - 1); 
				
				cmdChatClanAlly(player, command, args); 
			return; 
			case "kick": 
				cmdClanKick(player, args); 
			return; 
			case "disband": 
				cmdClanDisband(player, args); 
			return; 
			default: 
				cmdChatClanHelp(player, command, args); 
			return; 
		} 
	}  
	void cmdClanOverview(BasePlayer player) 
	{
		var current = this.covalence.Players.FindPlayer(player.UserIDString); 
		var myClan = findClanByUser(current.Id); 
		var sb1 = new StringBuilder(); 
		var sb2 = new StringBuilder(); 
		var sb3 = new StringBuilder(); 
		var sb4 = new StringBuilder(); 
			sb1.AppendLine("<size=20><color=orange>Clans</color></size><size=16><color=#ce422b>REBORN</color></size>");
		
		if (myClan == null) 
		{
			sb1.AppendLine(string.Format(msg("<color=white>На данный момент вы не состоите в клане.</color>", current.Id))); 
			sb2.AppendLine(string.Format(msg("<color=white>Чтобы узнать больше о кланах, введите: </color><color=#ffd479>/{1} help</color>", current.Id), colorCmdUsage, $"/{chatCommandClan}")); 
			
			SendReply(player, sb1.ToString() + sb2.ToString()); 
			return; 		
		} 
		if (myClan.IsOwner(current.Id)) 
			sb1.Append(string.Format(msg("<color=white>Вы являетесь владельцем клана:</color>", current.Id))); 
		else if (myClan.IsCouncil(current.Id)) 
			sb1.Append(string.Format(msg("<color=white>Вы являетесь советом: </color>", current.Id))); 
		else if (myClan.IsModerator(current.Id)) 
			sb1.Append(string.Format(msg("<color=white>Вы являетесь модератором: </color>", current.Id))); 
		else 
			sb1.Append(string.Format(msg("<color=white>Вы являетесь участником: </color>", current.Id))); 
		
		sb1.Append($" <color=#ffd479>{myClan.tag}</color> (В сети: <color=#ffd479>{myClan.online}/{myClan.total}</color>)"); 
		sb2.Append($"<color=white>" + string.Format(msg("<color=white>В сети: </color>", current.Id))); 
		
		int n = 0; 
		
		foreach (var memberId in myClan.members) 
		{
			var op = this.covalence.Players.FindPlayer(memberId); 
			
			if (op != null && op.IsConnected) 
			{
				var memberName = op.Name; 
				
				if (op.Name == current.Name) 
					memberName = msg("ВЫ", current.Id); 
				if (n > 0) 
					sb2.Append(", "); 
					
				var memberOn = string.Empty; 
				
				if (myClan.IsOwner(memberId)) 
				{ 
					memberOn = string.Format(msg("overviewnamecolor", current.Id), clanOwnerColor, memberName); 
				} 
				else if (myClan.IsCouncil(memberId)) 
				{ 
					memberOn = string.Format(msg("overviewnamecolor", current.Id), clanCouncilColor, memberName); 		
				} 
				else if (myClan.IsModerator(memberId)) 
				{
					memberOn = string.Format(msg("overviewnamecolor", current.Id), clanModeratorColor, memberName); 
				} 
				else 
				{
					memberOn = string.Format(msg("overviewnamecolor", current.Id), clanMemberColor, memberName); 
				} 
				
				++n; 
				
				if ((sb2.ToString().Length + memberOn.Length) < 1080) 
					sb2.Append(memberOn); 
				else 
					break; 
			} 
		} 
		sb2.Append("</color>"); 
		
		bool offline = false; 
		
		foreach (var memberId in myClan.members) 
		{
			var op = this.covalence.Players.FindPlayer(memberId); 
			
			if (op != null && !op.IsConnected) 
			{
				offline = true; 
				break; 
			} 
		} 
		
		if (offline) 
		{ 
			sb3.Append($"<color={colorTextMsg}>" + string.Format(msg("<color=white>Оффлайн: </color>", current.Id))); 
			n = 0; 
			
			foreach (var memberId in myClan.members) 
			{
				var p = this.covalence.Players.FindPlayer(memberId); 
				var memberOff = string.Empty; 
				
				if (p != null && !p.IsConnected) 
				{ 
				if (n > 0) 
					sb3.Append(", "); 
					
					if (myClan.IsOwner(memberId)) 
					{
						memberOff = string.Format(msg("overviewnamecolor", current.Id), clanOwnerColor, p.Name); 
					} 
					else if (myClan.IsCouncil(memberId)) 
					{
						memberOff = string.Format(msg("overviewnamecolor", current.Id), clanCouncilColor, p.Name); 
					} 
					else if (myClan.IsModerator(memberId)) 
					{
						memberOff = string.Format(msg("overviewnamecolor", current.Id), clanModeratorColor, p.Name); 
					} 
					else 
					{ 
						memberOff = string.Format(msg("overviewnamecolor", current.Id), clanMemberColor, p.Name); 
					} 
					++n; 
					
					if ((sb3.ToString().Length + memberOff.Length) < 1080) 
						sb3.Append(memberOff); 
					else 
						break; 
				} 	
			} 
			sb3.Append("</color>"); 
		} 
		sb4.Append($"<color={colorTextMsg}>"); 
		
		if ((myClan.IsOwner(current.Id) || myClan.IsCouncil(current.Id) || myClan.IsModerator(current.Id)) && myClan.invites.Count() > 0) 
		{
			sb4.Append(string.Format(msg("<color=white>Приглашения в ожидании: </color>", current.Id))); 
			
			int m = 0; 
			
			foreach (var inviteId in myClan.invites) 
			{
				var p = this.covalence.Players.FindPlayer(inviteId.Key); 
				
				if (p != null) 
				{
					if (m > 0) 
						sb4.Append(", "); 
						
					sb2.Append(string.Format(msg("overviewnamecolor", current.Id), clanMemberColor, p.Name)); 
					++m; 
				} 
			} 
			sb4.Append("\n"); 
		} 
		
		if (enableClanAllies && myClan.clanAlliances.Count() > 0) 
		{
			sb4.AppendLine(string.Format(msg("Ваши союзные кланы:", current.Id)) + $" <color={colorClanNamesOverview}>" + string.Join(", ", myClan.clanAlliances.ToArray()) + "</color>"); 
		} 
		if (enableClanAllies && (myClan.invitedAllies.Count() > 0 || myClan.pendingInvites.Count() > 0) && (myClan.IsOwner(current.Id) || myClan.IsCouncil(current.Id))) 
		{
			if (myClan.invitedAllies.Count() > 0) 
				sb4.Append(string.Format(msg("Приглашения в союз", current.Id)) + $" <color={colorClanNamesOverview}>" + string.Join(", ", myClan.invitedAllies.ToArray()) + "</color> "); 
			if (myClan.pendingInvites.Count() > 0) 
				sb4.Append(string.Format(msg("Запросы союзника:", current.Id)) + $" <color={colorClanNamesOverview}>" + string.Join(", ", myClan.pendingInvites.ToArray()) + "</color> ");  
				
			sb4.AppendLine(); 
		} 
		if (enableFFOPtion) 
		{
			sb4.AppendLine(string.Format(msg("<color=white>Дружественный огонь:</color>", current.Id)) + " " + (manuallyEnabledBy.Contains(player.userID) ? $"<color={colorClanFFOn}>ВКЛ</color>" : $"<color={colorClanFFOff}>ВЫКЛ</color>") + $" ( <color={colorCmdUsage}>/{chatCommandFF}</color> )"); 
		} 
		
		sb4.Append(string.Format(msg("<color=white>Чтобы узнать больше о кланах, введите: </color><color=#ffd479>{1} help</color>", current.Id), colorCmdUsage, $"/{chatCommandClan}")); 
		sb4.Append("</color>"); 
		
		var sb0 = new StringBuilder(); 
			sb0.Append(sb1.ToString()); 
			
		if ((sb0.ToString().Length + sb2.ToString().Length) > 1080) 
		{
			SendReply(player, sb0.ToString()); 
			sb0.Clear(); 
		} 
		else 
			sb0.Append("\n" + sb2.ToString());  
		if (offline) 
		{
			if ((sb0.ToString().Length + sb3.ToString().Length) > 1080) 
			{
				SendReply(player, sb0.ToString()); 
				sb0.Clear(); 		
			} 
			else 
				sb0.Append("\n" + sb3.ToString()); 
		} 
		if ((sb0.ToString().Length + sb4.ToString().Length) > 1080) 
		{
			SendReply(player, sb0.ToString()); 
			sb0.Clear(); 
		} 
		else 
		{
			sb0.Append("\n" + sb4.ToString()); 
			SendReply(player, sb0.ToString()); 
			sb0.Clear(); 
		} 
	}  
	
	void cmdClanCreate(BasePlayer player, string [] args) 
	{
		var current = this.covalence.Players.FindPlayer(player.UserIDString); 
		var myClan = findClanByUser(current.Id); 
		
		if (myClan != null) 
		{
			PrintChat(player, string.Format(msg("<color=white>Вы уже состоите в клане.</color>", current.Id))); 
			return; 
		} 
		if (usePermToCreateClan && !permission.UserHasPermission(current.Id, permissionToCreateClan)) 
		{
			PrintChat(player, msg("nopermtocreate", current.Id)); 
			return; 
		} 
		if (args.Length < 3) 
		{ 
			PrintChat(player, string.Format(msg("<color=white>Используйте - </color><color=#ffd479>/clan create <ТЕГ> <описание></color>", current.Id), colorCmdUsage)); 
			return; 
		} 
		if (tagReExt.IsMatch(args[1])) 
		{
			PrintChat(player, string.Format(msg("<color=white>Тег клана должен содержать только такие символы как: '</color><color=#ffd479>a-z</color><color=white>' '</color><color=#ffd479>A-Z</color><color=white>' '</color><color=#ffd479>0-9</color><color=white>' '</color><color=#ffd479>а-я</color><color=white>' '</color><color=#ffd479>А-Я</color><color=white>' '</color><color=#ffd479>{0}</color><color=white>'</color>", current.Id), allowedSpecialChars)); 
			return; 
		} 
		if (args[1].Length < tagLengthMin || args[1].Length > tagLengthMax) 
		{
			PrintChat(player, string.Format(msg("Тег клана должен содержать от <color=#ffd479>{0}</color> до <color=#ffd479>{1}</color> символов", current.Id), tagLengthMin, tagLengthMax)); 
			return; 
		} 
		
		args[2] = args[2].Trim(); 
		
		if (args[2].Length < 2 || args[2].Length > 30) 
		{ 
			PrintChat(player, string.Format(msg("<color=white>Напишите краткое описание вашего клана.</color>", current.Id))); 
			return; 
		} 
		if (enableWordFilter && FilterText(args[1])) 
		{
			PrintChat(player, string.Format(msg("<color=white>Тег клана содержит запрещенные слова.</color>", current.Id))); 
			return; 
		} 
		
		string[] clanKeys = clans.Keys.ToArray(); 
		
		clanKeys = clanKeys.Select(c => c.ToLower()).ToArray(); 
		
		if (clanKeys.Contains(args[1].ToLower())) 
		{
			PrintChat(player, string.Format(msg("<color=white>Какой-то клан уже использует этот тег.</color>", current.Id))); 
			return; 		
		} 
		
		myClan = Clan.Create(args[1], args[2], current.Id); 
		clans.Add(myClan.tag, myClan); 
		clanCache[current.Id] = myClan; 
		setupPlayer(player, current.Name, current.Id); 
		
		if (usePermGroups && !permission.GroupExists(permGroupPrefix + myClan.tag)) 
			permission.CreateGroup(permGroupPrefix + myClan.tag, "Clan " + myClan.tag, 0); 
		if (usePermGroups && !permission.UserHasGroup(current.Id, permGroupPrefix + myClan.tag)) 
			permission.AddUserGroup(current.Id, permGroupPrefix + myClan.tag); myClan.onCreate(); 
		
		myClan.total++; 
		PrintChat(player, string.Format(msg("<color=white>Теперь вы владелец клана</color> <color=#ffd479>{0}</color>\n<color=white>Описание клана:</color> <color=#ffd479>{1}</color>", current.Id), myClan.tag, myClan.description) +"\n" + string.Format(msg("<color=white>Чтобы пригласить игрока, введите:</color> <color=#ffd479>/clan invite <игрок></color>", current.Id), colorCmdUsage)); 
		return; 
		}  
		
		public void InvitePlayer(BasePlayer player, string targetId) => cmdClanInvite(player, new string[] { "", targetId });  
		void cmdClanInvite(BasePlayer player, string [] args) 
		{
			var current = this.covalence.Players.FindPlayer(player.UserIDString); 
			var myClan = findClanByUser(current.Id);
			
			if (myClan == null) 
			{
				PrintChat(player, string.Format(msg("<color=white>На данный момент вы не состоите в клане.</color>", current.Id))); 
				return; 
			} 
			if (args.Length < 2) 
			{
				PrintChat(player, string.Format(msg("<color=white>Используйте - </color><color=#ffd479>/clan invite <игрок></color>", current.Id), colorCmdUsage)); 
				return; 
			} 
			if (!myClan.IsOwner(current.Id) && !myClan.IsCouncil(current.Id) && !myClan.IsModerator(current.Id)) 
			{
				PrintChat(player, string.Format(msg("<color=white>Это команда доступна модератору и владельцу клана.</color>", current.Id))); 
				return; 
			} 
			
			var invPlayer = myClan.GetIPlayer(args[1]); 
			
			if (invPlayer == null) 
			{
				PrintChat(player, string.Format(msg("<color=white>Игрока с никнеймом</color> <color=#ffd479>{0}</color> <color=white>нет, либо вы ввели неправильно никнейм.</color>", current.Id), args[1])); 
				return; 
			} 
			if (myClan.members.Contains(invPlayer.Id)) 
			{ 
				PrintChat(player, string.Format(msg("<color=white>Игрок</color> <color=#ffd479>{0}</color> <color=white>уже состоит в вашем клане.</color>", current.Id), invPlayer.Name)); 
				return; 
			} 
			if (myClan.invites.ContainsKey(invPlayer.Id)) 
			{
				PrintChat(player, string.Format(msg("<color=white>Игрок</color> <color=#ffd479>{0}</color> <color=white>уже приглашен в ваш клан.</color>", current.Id), invPlayer.Name)); 
				return; 
			} 
			if (findClanByUser(invPlayer.Id) != null) 
			{
				PrintChat(player, string.Format(msg("<color=white>Игрок</color> <color=#ffd479>{0}</color> <color=white>уже состоит в клане.</color>", current.Id), invPlayer.Name)); 
				return; 
			} 
			
			myClan.invites.Add(invPlayer.Id, UnixTimeStampUTC()); 
			
			if (!pendingPlayerInvites.ContainsKey(invPlayer.Id)) 
				pendingPlayerInvites.Add(invPlayer.Id, new List<string>()); 
		
			pendingPlayerInvites[invPlayer.Id].Add(myClan.tag);
			myClan.BroadcastLoc("Игрок <color=#ffd479>{0}</color> пригласил <color=#ffd479>{1}</color> в клан.", 
			myClan.ColNam(current.Id, current.Name), 
			myClan.ColNam(invPlayer.Id, invPlayer.Name)); 
		
		
			if (invPlayer.IsConnected) 
			{
				var invited = rust.FindPlayerByIdString(invPlayer.Id); 
				
				if (invited != null) PrintChat(invited, string.Format(msg("<color=white>Вас пригласили присоединиться к клану:</color> [<color=#ffd479>{0}</color>] '<color=#ffd479>{1}</color>'\n<color=white>Чтобы присоединиться, введите:</color> <color=#ffd479>/clan join {0}</color>", invPlayer.Id), myClan.tag, myClan.description, colorCmdUsage)); 
			} 			
			myClan.updated = UnixTimeStampUTC(); 
		}  
		public void WithdrawPlayer(BasePlayer player, string targetId) => cmdClanWithdraw(player, new string[] { "", targetId });  
		void cmdClanWithdraw(BasePlayer player, string [] args) 
		{
			var current = this.covalence.Players.FindPlayer(player.UserIDString); 
			var myClan = findClanByUser(current.Id); 
			
			if (myClan == null) 
			{
				PrintChat(player, string.Format(msg("<color=white>На данный момент вы не состоите в клане.</color>", current.Id))); 
				return; 
			} 
			if (args.Length < 2) 
			{
				PrintChat(player, string.Format(msg("<color=white>Используйте - </color><color=#ffd479>/clan withdraw <игрок></color>", current.Id), colorCmdUsage)); 
				return; 
			} 
			if (!myClan.IsOwner(current.Id) && !myClan.IsCouncil(current.Id) && !myClan.IsModerator(current.Id)) 
			{
				PrintChat(player, string.Format(msg("<color=white>Это команда доступна модератору и владельцу клана.</color>", current.Id))); 
				return; 
			} 
			
			var disinvPlayer = myClan.GetIPlayer(args[1]); 
			
			if (disinvPlayer == null) 
			{
				PrintChat(player, string.Format(msg("<color=white>Игрока с никнеймом</color> <color=#ffd479>{0}</color> <color=white>нет, либо вы ввели неправильно никнейм.</color>", current.Id), args[1])); 
				return; 
			} 
			if (myClan.members.Contains(disinvPlayer.Id)) 
			{
				PrintChat(player, string.Format(msg("<color=white>Игрок</color> <color=#ffd479>{0}</color> <color=white>уже состоит в вашем клане.</color>", current.Id), disinvPlayer.Name)); 
				return; 
			} 
			if (!myClan.invites.ContainsKey(disinvPlayer.Id)) 
			{
				PrintChat(player, string.Format(msg("<color=white>Игрок</color> <color=#ffd479>{0}</color> <color=white>не приглашен в ваш клан.</color>", current.Id), disinvPlayer.Name)); 
				return; 
			} 
			
			myClan.invites.Remove(disinvPlayer.Id); 
			
			if (pendingPlayerInvites.ContainsKey(disinvPlayer.Id)) 
				pendingPlayerInvites[disinvPlayer.Id].Remove(myClan.tag); 
			
			myClan.BroadcastLoc("<color=#ffd479>{0}</color> отменил приглашение <color=#ffd479>{1}</color>", 			
			myClan.ColNam(current.Id, current.Name), myClan.ColNam(disinvPlayer.Id, disinvPlayer.Name)); 
			myClan.updated = UnixTimeStampUTC(); 
		}  
		void cmdClanJoin(BasePlayer player, string [] args) 
		{
			var current = this.covalence.Players.FindPlayer(player.UserIDString); 
			var myClan = findClanByUser(current.Id); 
			
			if (myClan != null) 
			{
				PrintChat(player, string.Format(msg("<color=white>Вы уже состоите в клане.</color>", current.Id))); 
				return; 
			} 
			if (args.Length != 2) 
			{
				PrintChat(player, string.Format(msg("<color=white>Используйте - </color><color=#ffd479>/clan join <ТЕГ></color>", current.Id), colorCmdUsage)); 
				return; 
			} 
			
			myClan = findClan(args[1]); 
		
			if (myClan == null || !myClan.IsInvited(current.Id)) 
			{
				PrintChat(player, string.Format(msg("<color=white>Вас не приглашали в этот клан.</color>", current.Id))); 
				return;
			} 
			if (limitMembers >= 0 && myClan.members.Count() >= limitMembers) 
			{
				PrintChat(player, string.Format(msg("<color=white>В этом клане достигнуто максимальное количество участников.</color>", current.Id))); 
				return; 
			} 
			
			myClan.invites.Remove(current.Id); 
			pendingPlayerInvites.Remove(current.Id); 
			myClan.members.Add(current.Id); 
			clanCache[current.Id] = myClan; 
			setupPlayer(player, current.Name, current.Id); 
			
			if (usePermGroups && !permission.UserHasGroup(current.Id, permGroupPrefix + myClan.tag)) 
				permission.AddUserGroup(current.Id, permGroupPrefix + myClan.tag); 
				
			myClan.BroadcastLoc("<color=#ffd479>{0}</color> зашёл в клан!", myClan.ColNam(current.Id, current.Name)); 
			myClan.updated = UnixTimeStampUTC(); 
			myClan.total++; 
			
			if (enableRustIOSupport && ioIsInstalled() && addClanMembersAsIOFriends) foreach (var memberId in myClan.members) 
			{
				if (memberId != current.Id) 
				{ 
					ioAddFriend(memberId, current.Id); 
					ioAddFriend(current.Id, memberId); 
				}
			} 
			
			myClan.onUpdate();
		}  
		public void PromotePlayer(BasePlayer player, string targetId) => cmdClanPromote(player, new string[] { "", targetId });  
		void cmdClanPromote(BasePlayer player, string [] args) 
		{
			var current = this.covalence.Players.FindPlayer(player.UserIDString); 
			var myClan = findClanByUser(current.Id); 
			
			if (myClan == null) 
			{
				PrintChat(player, string.Format(msg("<color=white>На данный момент вы не состоите в клане.</color>", current.Id))); 
				return; 
			} 
			if (!myClan.IsOwner(current.Id)) 
			{
				PrintChat(player, string.Format(msg("Вы должны быть владельцем сервера, чтобы использовать эту команду.", current.Id))); 
				return; 
			} 
			if (args.Length != 2) 
			{ 
				PrintChat(player, string.Format(msg("<color=white>Используйте - </color><color=#ffd479>/clan promote <игрок></color>", current.Id), colorCmdUsage)); 
				return; 			
			} 
			
			var promotePlayer = myClan.GetIPlayer(args[1]); 
			
			if (promotePlayer == null) 
			{
				PrintChat(player, string.Format(msg("<color=white>Игрока с никнеймом</color> <color=#ffd479>{0}</color> <color=white>нет, либо вы ввели неправильно никнейм.</color>", current.Id), args[1])); 
				return; 
			} 
			if (!myClan.IsMember(promotePlayer.Id)) 
			{
				PrintChat(player, string.Format(msg("Игрок <color=#ffd479>{0}</color> не участник вашего клана.", current.Id), promotePlayer.Name)); 
				return; 
			} 
			if (enableClanAllies && myClan.IsCouncil(promotePlayer.Id)) 
			{
				PrintChat(player, string.Format(msg("Игрок <color=#ffd479>{0}</color> уже является советом вашего клана.", current.Id), promotePlayer.Name)); 
				return; 
			} 
			if (enableClanAllies && myClan.council != null && myClan.IsModerator(promotePlayer.Id)) 
			{
				PrintChat(player, string.Format(msg("Должность совета уже присуждена.", current.Id), promotePlayer.Name)); 
				return; 
			} 
			if (!enableClanAllies && myClan.IsModerator(promotePlayer.Id)) 
			{
				PrintChat(player, string.Format(msg("Игрок <color=#ffd479>{0}</color> уже является модератором вашего клана.", current.Id), promotePlayer.Name)); 
				return; 
			}  
			if (!myClan.IsModerator(promotePlayer.Id) && limitModerators >= 0 && myClan.moderators.Count() >= limitModerators) 
			{
				PrintChat(player, string.Format(msg("В этом клане уже достигнуто максимальное количество модераторов.", current.Id))); 
				return; 
			} 
			if (enableClanAllies && myClan.IsModerator(promotePlayer.Id)) 
			{
				myClan.council = promotePlayer.Id; 
				myClan.moderators.Remove(promotePlayer.Id); 
				myClan.BroadcastLoc("playerpromotedcouncil", myClan.ColNam(current.Id, current.Name), myClan.ColNam(promotePlayer.Id, promotePlayer.Name)); 
			} 
			else
			{
				myClan.moderators.Add(promotePlayer.Id); 
				myClan.BroadcastLoc("playerpromoted", myClan.ColNam(current.Id, current.Name), myClan.ColNam(promotePlayer.Id, promotePlayer.Name)); 
			} 
			
			myClan.updated = UnixTimeStampUTC(); myClan.onUpdate(); 
		}  
		public void DemotePlayer(BasePlayer player, string targetId) => cmdClanDemote(player, new string[] { "", targetId });  
		void cmdClanDemote(BasePlayer player, string [] args) 
		{
			var current = this.covalence.Players.FindPlayer(player.UserIDString); 
			var myClan = findClanByUser(current.Id); 
			
			if (myClan == null) 
			{ 
				PrintChat(player, string.Format(msg("<color=white>На данный момент вы не состоите в клане.</color>", current.Id))); 
				return; 
			} 
			if (!myClan.IsOwner(current.Id)) 
			{
				PrintChat(player, string.Format(msg("Вы должны быть владельцем сервера, чтобы использовать эту команду.", current.Id))); 
				return; 
			} 
			if (args.Length < 2) 
			{
				PrintChat(player, string.Format(msg("<color=white>Используйте - </color><color=#ffd479>/clan demote <игрок></color>", current.Id), colorCmdUsage)); 
				return; 
			} var demotePlayer = myClan.GetIPlayer(args[1]); 
			if (demotePlayer == null) 
			{
				PrintChat(player, string.Format(msg("<color=white>Игрока с никнеймом</color> <color=#ffd479>{0}</color> <color=white>нет, либо вы ввели неправильно никнейм.</color>", current.Id), args[1])); 
				return; 
			} 
			if (!myClan.IsMember(demotePlayer.Id)) 
			{
				PrintChat(player, string.Format(msg("Игрок <color=#ffd479>{0}</color> не участник вашего клана.", current.Id), demotePlayer.Name)); 
				return; 
			} 
			if (!myClan.IsModerator(demotePlayer.Id) && !myClan.IsCouncil(demotePlayer.Id)) 
			{
				PrintChat(player, string.Format(msg("Игрок <color=#ffd479>{0}</color> не модератор/совет вашего клана.", current.Id), demotePlayer.Name)); 
				return; 
			} 
			if (enableClanAllies && myClan.IsCouncil(demotePlayer.Id)) 
			{
				myClan.council = null; 
			
				if (limitModerators >= 0 && myClan.moderators.Count() >= limitModerators) 
					myClan.BroadcastLoc("Игрок <color=#ffd479>{0}</color> понизил <color=#ffd479>{1}</color> до участника.", myClan.ColNam(current.Id, current.Name), myClan.ColNam(demotePlayer.Id, demotePlayer.Name)); 
				else 
				{
					myClan.moderators.Add(demotePlayer.Id); myClan.BroadcastLoc("Игрок <color=#ffd479>{0}</color> повысил <color=#ffd479>{1}</color> до модератора.", myClan.ColNam(current.Id, current.Name), myClan.ColNam(demotePlayer.Id, demotePlayer.Name)); 
				} 
			} 
			else 
			{
				myClan.moderators.Remove(demotePlayer.Id); myClan.BroadcastLoc("Игрок <color=#ffd479>{0}</color> понизил <color=#ffd479>{1}</color> до участника.", myClan.ColNam(current.Id, current.Name), myClan.ColNam(demotePlayer.Id, demotePlayer.Name)); 
			}  
			
			myClan.updated = UnixTimeStampUTC(); myClan.onUpdate(); 
		}  
		public void LeaveClan(BasePlayer player) => cmdClanLeave(player, new string[] { "leave" });  
		void cmdClanLeave(BasePlayer player, string [] args) 
		{
			var current = this.covalence.Players.FindPlayer(player.UserIDString); 
			var myClan = findClanByUser(current.Id); 
			
			if (myClan == null) 
			{
				PrintChat(player, string.Format(msg("<color=white>На данный момент вы не состоите в клане.</color>", current.Id))); 
				return; 
			} 
			if (args.Length != 1)
			{
				PrintChat(player, string.Format(msg("<color=white>Используйте - </color><color=#ffd479>/clan leave</color>", current.Id), colorCmdUsage)); 
				return; 
			} 
			if (myClan.members.Count() == 1) 
			{
				clans.Remove(myClan.tag); 
			} 
			else 
			{ 
				if (myClan.IsCouncil(current.Id)) 
					myClan.council = null; 
				
					myClan.moderators.Remove(current.Id); 
					myClan.members.Remove(current.Id); 
					myClan.invites.Remove(current.Id); 
				
				if (myClan.IsOwner(current.Id) && myClan.members.Count() > 0) 
				{ 
					myClan.owner = myClan.members[0]; 
				} 
			}
			
			clanCache.Remove(current.Id); 
			setupPlayer(player, current.Name, current.Id); 
			
			if (usePermGroups && permission.UserHasGroup(current.Id, permGroupPrefix + myClan.tag)) 
				permission.RemoveUserGroup(current.Id, permGroupPrefix + myClan.tag); 
				
			PrintChat(player, string.Format(msg("<color=white>Вы покинули свой клан.</color>", current.Id)));
			
			myClan.BroadcastLoc("{0} <color=white>покинул клан.</color>", myClan.ColNam(current.Id, current.Name)); 
			myClan.updated = UnixTimeStampUTC(); 
			myClan.total--; 
			myClan.onUpdate(); 
		}  
		public void KickPlayer(BasePlayer player, string targetId) => cmdClanKick(player, new string[] { "", targetId });  
		void cmdClanKick(BasePlayer player, string [] args) 
		{
			var current = this.covalence.Players.FindPlayer(player.UserIDString); 
			var myClan = findClanByUser(current.Id); 
			
			if (myClan == null) 
			{
				PrintChat(player, string.Format(msg("<color=white>На данный момент вы не состоите в клане.</color>", current.Id))); 
				return;
			} 
			if (!myClan.IsOwner(current.Id) && !myClan.IsCouncil(current.Id) && !myClan.IsModerator(current.Id)) 
			{
				PrintChat(player, string.Format(msg("<color=white>Это команда доступна модератору и владельцу клана.</color>", current.Id))); 
				return; 
			} 
			if (args.Length != 2) 
			{ 
				PrintChat(player, string.Format(msg("<color=white>Используйте - </color><color=#ffd479>/clan kick <игрок></color>", current.Id), colorCmdUsage)); 
				return; 
			} 
			
			var kickPlayer = myClan.GetIPlayer(args[1]); 
			
			if (kickPlayer == null) 
			{
				PrintChat(player, string.Format(msg("<color=white>Игрока с никнеймом</color> <color=#ffd479>{0}</color> <color=white>нет, либо вы ввели неправильно никнейм.</color>", current.Id), args[1])); 
				return; 
			} 
			if (!myClan.IsMember(kickPlayer.Id) && !myClan.IsInvited(kickPlayer.Id)) 
			{
				PrintChat(player, string.Format(msg("Игрок <color=#ffd479>{0}</color> не участник вашего клана.", current.Id), kickPlayer.Name)); 
				return; 
			} 
			if (myClan.IsOwner(kickPlayer.Id) || myClan.IsCouncil(kickPlayer.Id) || myClan.IsModerator(kickPlayer.Id)) 
			{
				PrintChat(player, string.Format(msg("Игрок <color=#ffd479>{0}</color> является модератором или владельцем клана и его нельзя выгнать.", current.Id), kickPlayer.Name)); 
				return; 
			} 
			if (myClan.members.Contains(kickPlayer.Id)) 
				myClan.total--; 
				
				myClan.members.Remove(kickPlayer.Id); 
				myClan.invites.Remove(kickPlayer.Id); 
				
			if (pendingPlayerInvites.ContainsKey(kickPlayer.Id))
				pendingPlayerInvites[kickPlayer.Id].Remove(myClan.tag); 
				
			clanCache.Remove(kickPlayer.Id); 
			
			var kickBasePlayer = rust.FindPlayerByIdString(kickPlayer.Id); 
			
			if (kickBasePlayer != null) 
			{
				setupPlayer(kickBasePlayer, kickPlayer.Name, kickPlayer.Id); 
			} 
			if (usePermGroups && permission.UserHasGroup(kickPlayer.Id, permGroupPrefix + myClan.tag)) 
				permission.RemoveUserGroup(kickPlayer.Id, permGroupPrefix + myClan.tag); 
				
			myClan.BroadcastLoc("<color=#ffd479>{0}</color> выгнал <color=#ffd479>{1}</color> из клана.", myClan.ColNam(current.Id, current.Name), myClan.ColNam(kickPlayer.Id, kickPlayer.Name)); 
			myClan.updated = UnixTimeStampUTC(); 
			myClan.onUpdate(); 
		}  
		public void DisbandClan(BasePlayer player) => cmdClanDisband(player, new string[] 
		{ 
			"disband", 
			"forever" 
		});  
		void cmdClanDisband(BasePlayer player, string [] args) 
		{
			var current = this.covalence.Players.FindPlayer(player.UserIDString); 
			var myClan = findClanByUser(current.Id); 
			
			if (myClan == null) 
			{ 
				PrintChat(player, string.Format(msg("<color=white>На данный момент вы не состоите в клане.</color>", current.Id))); 
				return; 
			} 
			if (!myClan.IsOwner(current.Id)) 
			{
				PrintChat(player, string.Format(msg("Вы должны быть владельцем сервера, чтобы использовать эту команду.", current.Id))); 
				return; 
			} 
			if (args.Length != 2) 
			{
				PrintChat(player, string.Format(msg("<color=white>Используйте - </color><color=#ffd479>/clan disband forever</color>", current.Id), colorCmdUsage)); 
				return; 
			} 
			
			clans.Remove(myClan.tag); 
			
			foreach (var member in myClan.members) 
			{
				clanCache.Remove(member); 
				
				if (usePermGroups && permission.UserHasGroup((string)member, permGroupPrefix + myClan.tag)) 
					permission.RemoveUserGroup((string)member, permGroupPrefix + myClan.tag); 
			} 
			
			myClan.BroadcastLoc("<color=white>Вы распустили свой клан.</color>"); 
			setupPlayers(myClan.members); 
			
			foreach (var ally in clans) 
			{
				Clan allyClan = clans[ally.Key]; 
					allyClan.clanAlliances.Remove(myClan.tag); 
					allyClan.invitedAllies.Remove(myClan.tag); 
					allyClan.pendingInvites.Remove(myClan.tag); 
			} 
			
			if (usePermGroups && permission.GroupExists(permGroupPrefix + myClan.tag)) 
				permission.RemoveGroup(permGroupPrefix + myClan.tag); myClan.onDestroy(); 
				
			AllyRemovalCheck(); 
		}  
		public void Alliance(BasePlayer player, string targetClan, string type) => cmdChatClanAlly(player, "ally", new string[] { type, targetClan });  
		void cmdChatClanAlly(BasePlayer player, string command, string[] args) 
		{
			if (!enableClanAllies || player == null) 
				return; 
				
			var current = this.covalence.Players.FindPlayer(player.UserIDString); 
			var myClan = findClanByUser(current.Id); 
			
			if (myClan == null) 
			{
				PrintChat(player, string.Format(msg("<color=white>На данный момент вы не состоите в клане.</color>", current.Id))); 
				return; 
			} 
			if (!myClan.IsOwner(current.Id) && !myClan.IsCouncil(current.Id)) 
			{
				PrintChat(player, string.Format(msg("Вы должны быть владельцем или советом, чтобы использовать эту команду", current.Id))); 
					return; 
			} 
			if (args == null || args.Length == 0) 
			{
				var sbally = new StringBuilder(); 
					sbally.Append($"<size=18><color=orange>{this.Title}</color></size><size=14><color=#ce422b>REBORN</color></size>\n"); 
					sbally.Append($"<color={colorTextMsg}>"); 
			
				if (myClan.IsOwner(current.Id)) 
					sbally.Append(string.Format(msg("<color=white>Вы являетесь владельцем: </color>", current.Id))); 
				else if (myClan.IsCouncil(current.Id)) 
					sbally.Append(string.Format(msg("<color=white>Вы являетесь советом: </color>", current.Id))); 
				else if (myClan.IsModerator(current.Id)) 
					sbally.Append(string.Format(msg("<color=white>Вы являетесь модератором: </color>", current.Id))); 
				else 
					sbally.Append(string.Format(msg("<color=white>Вы являетесь участником: </color>", current.Id))); 
			
					sbally.AppendLine($" <color={colorClanNamesOverview}>{myClan.tag}</color> ( {myClan.online}/{myClan.total} )"); 
		
				if (myClan.clanAlliances.Count() > 0) 
					sbally.AppendLine(string.Format(msg("Ваши союзные кланы:", current.Id)) + $" <color={colorClanNamesOverview}>" + string.Join(", ", myClan.clanAlliances.ToArray()) + "</color>"); 
				if ((myClan.invitedAllies.Count() > 0 || myClan.pendingInvites.Count() > 0) && (myClan.IsOwner(current.Id) || myClan.IsCouncil(current.Id))) 
				{
					if (myClan.invitedAllies.Count() > 0) 
						sbally.Append(string.Format(msg("Приглашения в союз", current.Id)) + $" <color={colorClanNamesOverview}>" + string.Join(", ", myClan.invitedAllies.ToArray()) + "</color> "); 
					if (myClan.pendingInvites.Count() > 0) 
						sbally.Append(string.Format(msg("Запросы союзника:", current.Id)) + $" <color={colorClanNamesOverview}>" + string.Join(", ", myClan.pendingInvites.ToArray()) + "</color> "); 
				
					sbally.AppendLine(); 
				} 
		
				string commandtext = string.Empty; 
		
				if (command.Contains("ally")) 
					commandtext = command; 
				else 
					commandtext = chatCommandClan + " ally"; 
			
				sbally.AppendLine($"<color={colorCmdUsage}>/{commandtext} <request | req> <clantag></color>"); 
				sbally.AppendLine(" "+ msg("Предложить союз другому клану", current.Id)); 
				sbally.AppendLine($"<color={colorCmdUsage}>/{commandtext} <accept | acc> <clantag></color>"); 
				sbally.AppendLine(" "+ msg("Принять союз с другим кланом", current.Id)); 
				sbally.AppendLine($"<color={colorCmdUsage}>/{commandtext} <decline | dec> <clantag></color>"); 
				sbally.AppendLine(" " +msg("Отклонить союз от другого клана", current.Id)); 
				sbally.AppendLine($"<color={colorCmdUsage}>/{commandtext} <cancel | can> <clantag></color>"); 
				sbally.AppendLine(" " +msg("Отменить союз с другим кланом", current.Id)); 
				sbally.Append("</color>"); 
				SendReply(player, sbally.ToString().TrimEnd()); 
				return; 
			} 
			else if (args != null && args.Length >= 1 && args.Length < 2) 
			{
				PrintChat(player, string.Format(msg("Необходимо указать название клана", current.Id))); 
				return; 
			} 
			else if (args.Length >= 1) 
			{
				Clan targetClan = null; 
				
				switch (args[0]) 
				{
					case "request": 
					case "req": 
						if (limitAlliances != 0 && myClan.clanAlliances.Count >= limitAlliances) 
						{
							PrintChat(player, string.Format(msg("Вы достигли максимального количества союзников клана", current.Id))); 
							return; 
						} 
						if (myClan.invitedAllies.Contains(args[1])) 
						{
							PrintChat(player, string.Format(msg("У вас уже есть приглашение союза с [<color=#ffd479>{0}</color>]", current.Id), args[1])); 
							return; 
						} 
						if (myClan.clanAlliances.Contains(args[1])) 
						{
							PrintChat(player, string.Format(msg("Вы уже союзники с", current.Id))); 
							return; 
						} 
						
						targetClan = findClan(args[1]); 
						
						if (targetClan == null) 
						{
							PrintChat(player, string.Format(msg("Клана [<color=#ffd479>{0}</color>] не существует", current.Id), args[1])); 
							return; 
						} 
						
						targetClan.pendingInvites.Add(myClan.tag); 
						myClan.invitedAllies.Add(targetClan.tag); 
						PrintChat(player, string.Format(msg("Вы отправили запрос на клановый союз с [<color=#ffd479>{0}</color>]", current.Id), args[1])); 
						targetClan.AllyBroadcastLoc("[<color=#ffd479>{0}</color>] отправил вам запрос на союз", myClan.tag); 
						myClan.onUpdate(); 
						targetClan.onUpdate(); 
					return; 
					case "accept": 
					case "acc": 
						if (!myClan.pendingInvites.Contains(args[1])) 
						{ 
							PrintChat(player, string.Format(msg("У вас нет союз приглашения от [<color=#ffd479>{0}</color>]", current.Id), args[1])); 
							return; 
						} 
						
						targetClan = findClan(args[1]); 
						
						if (targetClan == null) 
						{
							PrintChat(player, string.Format(msg("Клана [<color=#ffd479>{0}</color>] не существует", current.Id), args[1])); 
							return; 
						} 
						if (limitAlliances != 0 && myClan.clanAlliances.Count >= limitAlliances) 
						{
							PrintChat(player, string.Format(msg("Вы не можете принять союз с <color=#ffd479>{0}</color>. Вы достигли предела", current.Id), targetClan.tag)); 
							
							targetClan.invitedAllies.Remove(myClan.tag); 
							myClan.pendingInvites.Remove(targetClan.tag); 
							return; 
						} 
						
						targetClan.invitedAllies.Remove(myClan.tag); 
						targetClan.clanAlliances.Add(myClan.tag); 
						myClan.pendingInvites.Remove(targetClan.tag); 
						myClan.clanAlliances.Add(targetClan.tag); 
						myClan.onUpdate(); targetClan.onUpdate(); 
						
						PrintChat(player, string.Format(msg("Вы приняли клановый союз с [<color=#ffd479>{0}</color>]", current.Id), targetClan.tag)); 
						
						targetClan.AllyBroadcastLoc("[<color=#ffd479>{0}</color>] принял ваш запрос на союз", myClan.tag); 
					return;
					case "decline": 
					case "dec": 
						if (!myClan.pendingInvites.Contains(args[1])) 
						{
							PrintChat(player, string.Format(msg("У вас нет союз приглашения от [<color=#ffd479>{0}</color>]", current.Id), args[1])); 
							return; 
						} 
						
						targetClan = findClan(args[1]); 
						
						if (targetClan == null) 
						{
							PrintChat(player, string.Format(msg("Клана [<color=#ffd479>{0}</color>] не существует", current.Id), args[1])); 
							return; 
						} 
						
						targetClan.invitedAllies.Remove(myClan.tag); 
						myClan.pendingInvites.Remove(targetClan.tag); 
						AllyRemovalCheck(); 
						
						PrintChat(player, string.Format(msg("Вы отказались от кланового союза с [<color=#ffd479>{0}</color>]", current.Id), args[1])); 
						
						myClan.onUpdate(); 
						targetClan.onUpdate(); 
						targetClan.AllyBroadcastLoc("[<color=#ffd479>{0}</color>] отклонил ваш запрос на союз", myClan.tag); 
					return; 
					case "cancel": 
					case "can": 
						if (!myClan.clanAlliances.Contains(args[1])) 
						{
							if (myClan.invitedAllies.Contains(args[1])) 
							{
								myClan.invitedAllies.Remove(args[1]); 
								targetClan = findClan(args[1]); 
								
								if (targetClan != null) 
									targetClan.pendingInvites.Remove(myClan.tag); 
									
								PrintChat(player, string.Format(msg("Вы отменили свой запрос на союз с [<color=#ffd479>{0}</color>]", current.Id), args[1])); 
								
								myClan.onUpdate(); 
								targetClan.onUpdate(); 
								return; 
							} 
							
							PrintChat(player, string.Format(msg("У вас в клане нету союзников", current.Id))); 
							return; 
						} 
						
						targetClan = findClan(args[1]); 
						
						if (targetClan == null) 
						{
							PrintChat(player, string.Format(msg("Клана [<color=#ffd479>{0}</color>] не существует", current.Id), args[1])); 
							return; 
						} 
						
						targetClan.clanAlliances.Remove(myClan.tag); 
						myClan.clanAlliances.Remove(targetClan.tag); 
						AllyRemovalCheck(); 
						
						PrintChat(player, string.Format(msg("Вы отменили свой союз с [<color=#ffd479>{0}</color>]", current.Id), args[1])); 
						
						myClan.onUpdate(); 
						targetClan.onUpdate(); 
						targetClan.AllyBroadcastLoc("[<color=#ffd479>{0}</color>] отменил ваш клановый союз", myClan.tag); 
					return; 
					default: 
						cmdChatClanAlly(player, command, new string[] {}); 
					return; 
				} 
			} 
		}  
		void cmdChatClanHelp(BasePlayer player, string command, string[] args) 
		{
			if (player == null) 
				return; 
				
			var current = this.covalence.Players.FindPlayer(player.UserIDString); 
			var myClan = findClanByUser(current.Id); 
			
			if (myClan == null) 
			{
				var sb = new StringBuilder(); 
					sb.Append($"<color={colorTextMsg}>"); 
					sb.AppendLine(msg("<color=white><size=18>Доступные команды:</size></color>", current.Id)); 
					sb.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} create <ТЕГ> <описание></color> - {msg("<color=white>Создать новый клан</color>", current.Id)}"); 
					sb.Append($"<color={colorCmdUsage}>/{chatCommandClan} join <ТЕГ></color> - {msg("<color=white>Вступить в клан по приглашению</color>", current.Id)}"); 
					sb.Append("</color>"); 
					
				SendReply(player, sb.ToString().TrimEnd()); 
				return; 
			} 
			var sb3 = new StringBuilder(); 
			var sb4 = new StringBuilder(); 
				sb3.Append($"<color={colorTextMsg}>"); 
				sb3.AppendLine(msg("<color=white><size=17>Доступные команды:</size></color>", current.Id)); 
				sb3.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan}</color> <color=white>- {msg("Показать информацию о вашем клане</color>", current.Id)}"); 
				sb3.AppendLine($"<color={colorCmdUsage}>/{chatCommandClanChat} <сообщение></color> <color=white>- {msg("Отправить сообщение всем участникам клана</color>", current.Id)}"); 
				
			if (enableClanAllies) 
				sb3.AppendLine($"<color={colorCmdUsage}>/{chatCommandAllyChat} <msg></color> - {msg("helpmessageally", current.Id)}"); 
				sb3.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} leave</color> <color=white>- {msg("Покинуть клан</color>", current.Id)}"); 
			if (enableFFOPtion) 
				sb3.AppendLine($"<color={colorCmdUsage}>/{chatCommandFF}</color> <color=white>- {msg("Изменить статус дружественного огня</color>", current.Id)}"); 
			if ((myClan.IsOwner(current.Id) || myClan.IsCouncil(current.Id) || myClan.IsModerator(current.Id))) 
			{
				sb3.AppendLine($"\n<color={clanModeratorColor}><size=17>Команды модератора:</size></color>"); 
				sb3.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} invite <никнейм | SteamID></color> <color=white>- {msg("Пригласить игрока в клан</color>", current.Id)}"); 
				sb3.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} withdraw <никнейм | SteamID></color> <color=white>- {msg("Отменить приглашение в клан</color>", current.Id)}"); 
				sb3.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} kick <никнейм | SteamID></color> <color=white>- {msg("Выгнать игрока из клана</color>", current.Id)}"); 
			} 
			
			sb3.Append("</color>"); 
			sb4.Append($"<color={colorTextMsg}>"); 
			
			if ((myClan.IsOwner(current.Id) || (enableClanAllies && myClan.IsCouncil(current.Id)))) 
			{
					sb4.AppendLine($"<color={clanOwnerColor}><size=17>Команды владельца:</size></color>"); 
				
				if (enableClanAllies) 
					sb4.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} ally | {chatCommandClan+"ally"}</color> - {msg("Список опций союза", current.Id)}"); 
				if (myClan.IsOwner(current.Id)) 
					sb4.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} promote <никнейм | SteamID></color> <color=white>- {msg("Повысить участника клана до модератора</color>", current.Id)}"); 
				if (myClan.IsOwner(current.Id)) 
					sb4.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} demote <никнейм | SteamID></color> <color=white>- {msg("Снять привилегию модератора с участника клана</color>", current.Id)}"); 
				if (myClan.IsOwner(current.Id)) 
					sb4.AppendLine($"<color={colorCmdUsage}>/{chatCommandClan} disband forever</color> <color=white>- {msg("Распустить ваш клан</color>", current.Id)}"); 
			} 
			if (player.net.connection.authLevel >= authLevelDelete || player.net.connection.authLevel >= authLevelRename || player.net.connection.authLevel >= authLevelInvite || player.net.connection.authLevel >= authLevelKick || player.net.connection.authLevel >= authLevelPromoteDemote) 
				sb4.AppendLine($"<color={clanServerColor}>Управление сервером</color>: {msg("<color=white>Откройте консоль с помощью</color> <color=#ffd479>F1</color> <color=white>и введите:</color> <color=#ffd479>clans</color>", current.Id)}"); 
				
				sb4.Append("</color>"); 
				
			if ((sb3.ToString().TrimEnd() + sb4.ToString().TrimEnd()).Length > 1090) 
			{
				SendReply(player, sb3.ToString().TrimEnd()); 
				SendReply(player, sb4.ToString().TrimEnd()); 
			} 
			else 
				SendReply(player, sb3.ToString().TrimEnd() +sb4.ToString().TrimEnd()); 
		}  
		void cmdChatClanInfo(BasePlayer player, string command, string[] args) 
		{
			if (player == null) 
				return; 
			if (player.net.connection.authLevel < authLevelClanInfo) 
			{
				PrintChat(player, "Нет доступа к этой команде."); 
				return; 
			} 
			if (args == null || args.Length == 0) 
			{
				PrintChat(player, "Укажите тег клана."); 
				return; 
			} 
			
			var Clan = findClan(args[0]);  
			
			if (Clan == null) 
			{
				PrintChat(player, string.Format(msg("Клана [<color=#ffd479>{0}</color>] не существует", player.UserIDString), args[0])); 
				return; 
			} 
			
			var sb = new StringBuilder(); 
				sb.Append($"<size=18><color=orange>{this.Title}</color></size><size=14><color=#ce422b>REBORN</color></size>\n"); 
				sb.AppendLine($"Подробная информация о клане:"); 
				sb.AppendLine($"Тег клана:  <color=orange>{Clan.tag}</color> (В сети: <color=green>{Clan.online}</color>/<color=#ff3333>{Clan.total}</color> )"); 
				sb.AppendLine($"Описание: <color=#ffd479>{Clan.description}</color>"); 
				sb.Append(string.Format(msg("<color=white>Онлайн: </color>", player.UserIDString))); 
				
			int n = 0; 
			
			foreach (var memberId in Clan.members) 
			{
				var op = this.covalence.Players.FindPlayer(memberId); 
				
				if (op != null && op.IsConnected) 
				{
					if (n > 0) 
						sb.Append(", "); 
					if (Clan.IsOwner(memberId)) 
					{
						sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanOwnerColor, op.Name)); 
					} 
					else if (Clan.IsCouncil(memberId)) 
					{
						sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanCouncilColor, op.Name)); 
					} 
					else if (Clan.IsModerator(memberId)) 
					{
						sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanModeratorColor, op.Name)); 
					} 
					else 
					{
						sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanMemberColor, op.Name)); 
					} 
					++n; 
				} 
			} 
			if (Clan.online == 0) 
				sb.Append("<color=#ff3333>никого</color>"); 
				 
				
			bool offline = false; 
			
			foreach (var memberId in Clan.members) 
			{
				var op = this.covalence.Players.FindPlayer(memberId); 
				
				if (op != null && !op.IsConnected) 
				{
					offline = true; 
					break; 
				} 
			} 
			if (offline) 
			{	
				sb.Append(string.Format(msg($"\n<color=white>Оффлайн: </color>", player.UserIDString))); 
				n = 0; 
				
				foreach (var memberId in Clan.members) 
				{
					var p = this.covalence.Players.FindPlayer(memberId); 
					
					if (p != null && !p.IsConnected) 
					{
						if (n > 0) 
							sb.Append(", "); 
						if (Clan.IsOwner(memberId)) 
						{
							sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanOwnerColor, p.Name)); 
						} 
						else if (Clan.IsCouncil(memberId)) 
						{
							sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanCouncilColor, p.Name)); 
						}
						else if (Clan.IsModerator(memberId)) 
						{
							sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanModeratorColor, p.Name)); 
						} 
						else 
						{
							sb.Append(string.Format(msg("overviewnamecolor", player.UserIDString), clanMemberColor, p.Name)); 
						} 
						++n; 
					} 					
				} 
				
				sb.Append("\n"); 
			} 
			
			sb.AppendLine($"Время создания: <color=#ffd479>{UnixTimeStampToDateTime(Clan.created)}</color>"); 
			sb.AppendLine($"Последнии обновления: <color=#ffd479>{UnixTimeStampToDateTime(Clan.updated)}</color>"); 
			
			SendReply(player, sb.ToString().TrimEnd()); 
		} 
		void cmdChatClanchat(BasePlayer player, string command, string[] args) 
		{ 
			if (player == null || args.Length == 0) 
				return; 
				
			var myClan = findClanByUser(player.UserIDString); 
			
			if (myClan == null) 
			{ 
				SendReply(player, string.Format(msg("<color=white>На данный момент вы не состоите в клане.</color>", player.UserIDString))); 
				return; 
			} 
			
			var message = string.Join(" ", args); 
			
			if (string.IsNullOrEmpty(message)) 
				return; 
				
			myClan.BroadcastChat(string.Format(msg("broadcastformat"), myClan.PlayerColor(player.UserIDString), player.net.connection.username, message)); 
				
			if (ConVar.Chat.serverlog) 
			{
				Debug.Log(string.Format("[CHAT] CLAN [{0}] - {1}: {2}", myClan.tag, player.net.connection.username, message)); 
				ConVar.Server.Log("Log.Chat.txt", string.Format("[CHAT] CLAN [{0}] - {1}: {2}\n", myClan.tag, player.net.connection.username, message)); 
			} 
		}  
		void cmdChatAllychat(BasePlayer player, string command, string[] args) 
		{
			if (player == null || args.Length == 0) 
				return; 
				
			var myClan = findClanByUser(player.UserIDString); 
			
			if (myClan == null) 
			{
				PrintChat(player, string.Format(msg("<color=white>На данный момент вы не состоите в клане.</color>", player.UserIDString))); 
				return; 
			} 
			if (myClan.clanAlliances.Count == 0) 
			{
				PrintChat(player, string.Format(msg("У вашего клана нет союзов.", player.UserIDString))); 
				return; 
			} 
			
			var message = string.Join(" ", args); 
			
			if (string.IsNullOrEmpty(message)) 
				return; 
				
			foreach (var clanAllyName in myClan.clanAlliances) 
			{
				var clanAlly = findClan(clanAllyName); 
				
				if (clanAlly == null) 
					continue; 
					
				clanAlly.AllyBroadcastChat(string.Format(msg("allybroadcastformat"), myClan.tag, myClan.PlayerColor(player.UserIDString), player.net.connection.username, message)); 
			} 
			
			myClan.AllyBroadcastChat(string.Format(msg("broadcastformat"), myClan.PlayerColor(player.UserIDString), player.net.connection.username, message)); 
			
			if (ConVar.Chat.serverlog) 
			{
				Debug.Log(string.Format("[CHAT] ALLY [{0}] - {1}: {2}", myClan.tag, player.net.connection.username, message)); 
				ConVar.Server.Log("Log.Chat.txt", string.Format("[CHAT] ALLY [{0}] - {1}: {2}\n", myClan.tag, player.net.connection.username, message)); 
			} 
		}  
		void cmdChatClanFF(BasePlayer player, string command, string[] args) 
		{
			if (!enableFFOPtion || player == null) 
				return; 
				
				var myClan = findClanByUser(player.UserIDString); 
				
				if (myClan == null) 
				{
					PrintChat(player, string.Format(msg("<color=white>На данный момент вы не состоите в клане.</color>", player.UserIDString))); 
					return; 
				} 
				if (manuallyEnabledBy.Contains(player.userID)) 
				{
					manuallyEnabledBy.Remove(player.userID); 
					
					PrintChat(player, string.Format(msg("<color=white>Вы</color> <color={0}>выключили</color> <color=white>дружественный огонь для вашего клана.</color>", player.UserIDString), colorClanFFOff)); 
					return; 
				} 
				else 
				{
					manuallyEnabledBy.Add(player.userID); PrintChat(player, string.Format(msg("<color=white>Вы</color> <color={0}>включили</color> <color=white>дружественный огонь для вашего клана.</color>", player.UserIDString), colorClanFFOn)); 
					return; 
				} 
		}  
		public bool HasFFEnabled(ulong playerId) => !enableFFOPtion ? false : !manuallyEnabledBy.Contains(playerId) ? false : true;  
		
		public void ToggleFF(ulong playerId) 
		{
			if (manuallyEnabledBy.Contains(playerId)) 
				manuallyEnabledBy.Remove(playerId); 
			else 
				manuallyEnabledBy.Add(playerId); 
		}  
		
		public class Clan 
		{ 
			public string tag; 
			public string description; 
			public string owner; 
			public string council; 
			public int created; 
			public int updated; [JsonIgnore] 
			public int online; [JsonIgnore] 
			public int total; [JsonIgnore] 
			public int mods; 
			
			public List<string> moderators = new List<string>(); 
			public List<string> members = new List<string>(); 
			public Dictionary<string, int> invites = new Dictionary<string, int>();  
			public List<string> clanAlliances = new List<string>(); 
			public List<string> invitedAllies = new List<string>(); 
			public List<string> pendingInvites = new List<string>();  
			
			public static Clan Create(string tag, string description, string ownerId) 
			{
				var clan = new Clan() 
				{
					tag = tag, 
					description = description, 
					owner = ownerId, 
					created = cc.UnixTimeStampUTC(), 
					updated = cc.UnixTimeStampUTC() 
				}; 
				
				clan.members.Add(ownerId); 
				return clan; 
			}  
			public bool IsOwner(string userId) 
			{ 
				return userId == owner; 
			}  
			public bool IsCouncil(string userId) 
			{
				return userId == council; 
			}  
			public bool IsModerator(string userId) 
			{
				return moderators.Contains(userId); 
			}  
			public bool IsMember(string userId) 
			{
				return members.Contains(userId); 
			}  
			public bool IsInvited(string userId) 
			{
				return invites.ContainsKey(userId); 
			}  
			public void BroadcastChat(string message) 
			{
				foreach (var memberId in members) 
				{
					var player = BasePlayer.Find(memberId); 
					
					if (player == null) 
						continue; 
					
					player.ChatMessage(string.Format(cc.broadcastPrefixFormat, cc.broadcastPrefixColor, cc.broadcastPrefix) + $"<color={cc.broadcastMessageColor}>{message}</color>"); 
				} 
			}  
			public void BroadcastLoc(string messagetype, string arg1 = "", string arg2 = "", string arg3 = "", string arg4 = "", string current = "") 
			{
				string message = string.Empty; 
				
				foreach (var memberId in members) 
				{
					var player = BasePlayer.Find(memberId); 
					
					if (player == null || player.UserIDString == current) 
						continue; 
					
					message = string.Format(cc.msg(messagetype, memberId), arg1, arg2, arg3, arg4); 
					player.ChatMessage(string.Format(cc.broadcastPrefixFormat, cc.broadcastPrefixColor, cc.broadcastPrefix) + $"<color={cc.broadcastMessageColor}>{message}</color>"); 
				} 
			}  
			public void AllyBroadcastChat(string message) 
			{
				foreach (var memberId in members) 
				{
					var player = BasePlayer.Find(memberId); 
					
					if (player == null) 
						continue; 
						
					player.ChatMessage(string.Format(cc.broadcastPrefixFormat, cc.broadcastPrefixColor, cc.broadcastPrefixAlly) + $"<color={cc.broadcastMessageColor}>{message}</color>"); 
				} 
			}  
			public void AllyBroadcastLoc(string messagetype, string arg1 = "", string arg2 = "", string arg3 = "", string arg4 = "") 
			{
				string message = string.Empty; 
				
				foreach (var memberId in members) 
				{
					var player = BasePlayer.Find(memberId); 
					
					if (player == null) 
						continue; 
						
					message = string.Format(cc.msg(messagetype, memberId), arg1, arg2, arg3, arg4); 
					player.ChatMessage(string.Format(cc.broadcastPrefixFormat, cc.broadcastPrefixColor, cc.broadcastPrefixAlly) + $"<color={cc.broadcastMessageColor}>{message}</color>"); 			
				}
			} 
			public string ColNam(string Id, string Name) 
			{
				if (IsOwner(Id)) 
					return $"<color={cc.clanOwnerColor}>{Name}</color>"; 
				else if (IsCouncil(Id) && !IsOwner(Id)) 
					return $"<color={cc.clanCouncilColor}>{Name}</color>"; 
				else if (IsModerator(Id) && !IsOwner(Id)) 
					return $"<color={cc.clanModeratorColor}>{Name}</color>"; 
				else 
					return $"<color={cc.clanMemberColor}>{Name}</color>"; 
			}  
			public string PlayerLevel(string userID) 
			{
				if (IsOwner(userID)) 
					return "Owner"; 
				if (IsCouncil(userID)) 
					return "Council"; 
				if (IsModerator(userID)) 
					return "Moderator"; 
				return "Member"; 
			}  
			public string PlayerColor(string userID) 
			{
				if (IsOwner(userID)) 
					return cc.clanOwnerColor; 
				if (IsCouncil(userID)) 
					return cc.clanCouncilColor; 
				if (IsModerator(userID)) 
					return cc.clanModeratorColor; 
				return cc.clanMemberColor; 
			}  
			
			public IPlayer GetIPlayer(string partialName) 
			{
				var player = cc.rust.FindPlayerByName(partialName); 
				
				if (player != null) 
					return cc.covalence.Players.FindPlayer(player.UserIDString); 
					
				try 
				{
					var iplayer = cc.covalence.Players.FindPlayer(partialName); 
					
					if (iplayer is IPlayer) 
						return iplayer;
				} 
				catch { } 
				
				var idplayer = cc.covalence.Players.FindPlayer(partialName); 
				
				if (idplayer != null) 
					return idplayer; 
				return null; 
			} 
			
			internal JObject ToJObject() 
			{
				var obj = new JObject(); 
				
				obj["tag"] = tag;
				obj["description"] = description; 
				obj["owner"] = owner; 
				obj["council"] = council; 
				
				var jmoderators = new JArray(); 
				
				foreach (var moderator in moderators) jmoderators.Add(moderator); 
				
				obj["moderators"] = jmoderators; 
				
				var jmembers = new JArray(); 
				
				foreach (var member in members) jmembers.Add(member); 
				
				obj["members"] = jmembers; 
				
				var jallies = new JArray(); 
				
				foreach (var ally in clanAlliances) jallies.Add(ally	); 
				
				obj["allies"] = jallies; 
				
				var jinvallies = new JArray(); 
				
				foreach (var ally in invitedAllies) jinvallies.Add(ally); 
				
				obj["invitedallies"] = jinvallies; 
				
				return obj; 
			}  
			internal void onCreate() => Interface.CallHook("OnClanCreate", tag);  
			internal void onUpdate() => Interface.CallHook("OnClanUpdate", tag);  
			internal void onDestroy() => Interface.CallHook("OnClanDestroy", tag); 
		}  
		[HookMethod("GetClan")] 
		private JObject GetClan(string tag) 
		{
			if (tag == null || tag == "") 
				return null; 
				
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
		[HookMethod("GetClanMembers")] 
		private List<ulong> GetClanMembers(ulong PlayerID) 
		{
			List<ulong> Players = new List<ulong>(); 
			
			var myClan = findClanByUser(PlayerID.ToString()); 
			
			if (myClan == null) 
				return null; 
			
			foreach (var it in myClan.members) Players.Add(Convert.ToUInt64(it)); 
			return Players; 
		}  
		[HookMethod("HasFriend")] 
		private object HasFriend(ulong entOwnerID, ulong PlayerUserID) 
		{
			var clanOwner = findClanByUser(entOwnerID.ToString()); 
			
			if (clanOwner == null) 
				return null; 
			
			var clanFriend = findClanByUser(PlayerUserID.ToString()); 
			
			if (clanFriend == null) 
				return null; 
			if (clanOwner.tag == clanFriend.tag) 
				return true; 
			return false; 
		}  
		[HookMethod("IsModerator")] 
		private object IsModerator(ulong PlayerUserID) 
		{
			var clan = findClanByUser(PlayerUserID.ToString()); 
			
			if (clan == null) 
				return null; 
			if ((setHomeOwner && clan.IsOwner(PlayerUserID.ToString())) || (setHomeModerator && (clan.IsModerator(PlayerUserID.ToString()) || clan.IsCouncil(PlayerUserID.ToString()))) || setHomeMember) 
				return true; 
			return false; 
		}  
		private Int32 UnixTimeStampUTC() 
		{
			Int32 unixTimeStamp; 
			DateTime currentTime = DateTime.Now; 
			DateTime zuluTime = currentTime.ToUniversalTime(); 
			DateTime unixEpoch = new DateTime(1970, 1, 1); 
			unixTimeStamp = (Int32)(zuluTime.Subtract(unixEpoch)).TotalSeconds; 
			return unixTimeStamp; 
		}  
		private static DateTime UnixTimeStampToDateTime(double unixTimeStamp) 
		{
			return unixTimeStamp > MaxUnixSeconds ? UnixEpoch.AddMilliseconds(unixTimeStamp) : UnixEpoch.AddSeconds(unixTimeStamp); 
		}  
		string msg(string key, string id = null) => lang.GetMessage(key, this, id);  
		
		void PrintChat(BasePlayer player, string message) 
		{
			SendReply(player, string.Format(pluginPrefixFormat, pluginPrefixColor, pluginPrefix) + $"<color={colorTextMsg}>" + message + "</color>"); 
		}  
		[ConsoleCommand("clans")] 
		void cclans(ConsoleSystem.Arg arg) 
		{
			if (arg != null && arg.Connection != null && arg.Connection.player != null && arg.Connection.authLevel >= 1) 
			{
				var sb = new StringBuilder(); 
					sb.AppendLine("<color=orange>clans.list</color> <color=white>(Список всех кланов, их владельцев и их участников)</color>"); 
					sb.AppendLine("<color=orange>clans.listex</color> <color=white>(Список всех кланов, их владельцев / участников и их онлайн статус)</color>"); 
					sb.AppendLine("<color=orange>clans.show <ТЕГ></color> <color=white>(Список участников указанного клана и их онлайн статус)</color>"); 
					sb.AppendLine("<color=orange>clans.msg <ТЕГ> <сообщение></color> <color=white>(Отправить сообщение клану)</color>"); 
					
				if (arg.Connection.authLevel >= authLevelRename) 
					sb.AppendLine("<color=orange>clans.rename <СТАРЫЙ ТЕГ> <НОВЫЙ ТЕГ></color> <color=white>(Переименовать клан)</color>"); 
				if (arg.Connection.authLevel >= authLevelDelete) 
					sb.AppendLine("<color=orange>clans.delete <ТЕГ></color> <color=white>(Удалить клан)</color>"); 
				if (arg.Connection.authLevel >= authLevelInvite) 
					sb.AppendLine("<color=orange>clans.playerinvite <ТЕГ> <игрок></color> <color=white>(Отправить приглашение игроку в клан)</color>"); 
				if (arg.Connection.authLevel >= authLevelKick) 
					sb.AppendLine("<color=orange>clans.playerkick <ТЕГ> <игрок></color> <color=white>(Выгнать игрока с клана)</color>"); 
				if (arg.Connection.authLevel >= authLevelPromoteDemote) 
				{
					sb.AppendLine("<color=orange>clans.playerpromote <ТЕГ> <игрок></color> <color=white>(Повысить участника клана до модератора)</color>"); 
					sb.AppendLine("<color=orange>clans.playerdemote <ТЕГ> <игрок></color> <color=white>(Снять привилегию модератора с участника клана)</color>"); 
				} 
				
				SendReply(arg, sb.ToString()); 
			} 
		}  
		[ConsoleCommand("clans.cmds")] 
		void cclansCommands(ConsoleSystem.Arg arg) 
		{
			if (arg.Connection != null && arg.Connection.authLevel < 2) 
				return; 
				
			var sb = new StringBuilder(); 
				sb.AppendLine("\n>> Команды кланов <<\n"); 
				sb.AppendLine("<color=orange>clans.list</color>".PadRight(20) + " | <color=white>Список всех кланов, их владельцев и их участников</color>"); 
				sb.AppendLine("<color=orange>clans.listex</color>".PadRight(20) + " | <color=white>Список всех кланов, их владельцев / участников и их онлайн статус</color>"); 
				sb.AppendLine("<color=orange>clans.show</color>".PadRight(20) + " | <color=white>Список участников указанного клана и их онлайн статус</color>"); 
				sb.AppendLine("<color=orange>clans.msg</color>".PadRight(20) + " | <color=white>Отправить сообщение клану</color>"); 
				sb.AppendLine("<color=orange>clans.rename</color>".PadRight(20) + " | <color=white>Переименовать клан</color>"); 
				sb.AppendLine("<color=orange>clans.delete</color>".PadRight(20) + " | <color=white>Удалить клан</color>");
				sb.AppendLine("<color=orange>clans.playerinvite</color>".PadRight(20) + " | <color=white>Отправить приглашение игроку в клан</color>"); 
				sb.AppendLine("<color=orange>clans.playerkick</color>".PadRight(20) + " | <color=white>Выгнать игрока с клана</color>"); 
				sb.AppendLine("<color=orange>clans.playerpromote</color>".PadRight(20) + " | <color=white>Повысить участника клана до модератораr</color>"); 
				sb.AppendLine("<color=orange>clans.playerdemote</color>".PadRight(20) + " | <color=white>Снять привилегию модератора с участника клана</color>"); 
			
			SendReply(arg, sb.ToString()); 
		}  
		[ConsoleCommand("clans.list")] 
		void cclansList(ConsoleSystem.Arg arg) 
		{
			if (arg.Connection != null && arg.Connection.authLevel < 1) 
				return; 
				
			TextTable textTable = new TextTable(); 
				textTable.AddColumn("Тег"); 
				textTable.AddColumn("Владелец"); 
				textTable.AddColumn("SteamID"); 
				textTable.AddColumn("Кол-во"); 
				textTable.AddColumn("Вкл."); 
				
			foreach (var iclan in clans) 
			{
				Clan clan = clans[iclan.Key]; 
				
				var owner = this.covalence.Players.FindPlayer(clan.owner); 
				
				if (owner == null) 
					continue; 
					
				textTable.AddRow(new string[] 
				{
					clan.tag, 
					owner.Name, 
					clan.owner, 
					clan.total.ToString(), 
					clan.online.ToString() 
				}); 
			} 
			
			SendReply(arg, "\n>> Текущии кланы <<\n" + textTable.ToString()); 
		}  
		[ConsoleCommand("clans.listex")] 
		void cclansListEx(ConsoleSystem.Arg arg) 
		{
			if (arg.Connection != null && arg.Connection.authLevel < 1) 
				return; 
				
			TextTable textTable = new TextTable(); 
				textTable.AddColumn("ТЕГ"); 
				textTable.AddColumn("Уровень"); 
				textTable.AddColumn("Имя"); 
				textTable.AddColumn("SteamID"); 
				textTable.AddColumn("Статус"); 
				
			foreach (var iclan in clans) 
			{
				Clan clan = clans[iclan.Key]; 
				
				foreach (var memberid in clan.members) 
				{
					var member = this.covalence.Players.FindPlayer(memberid); 
					
					if (member == null) 
						continue; 
					
					textTable.AddRow(new string[] 
					{
						clan.tag, 
						clan.PlayerLevel(member.Id), 
						member.Name, 
						member.Id.ToString(), 
						(member.IsConnected ? "<color=green>Онлайн</color>" : "<color=red>Оффлайн</color>").ToString() 
					}); 
				} 
				
				textTable.AddRow(new string[] {}); 
			} 
			
			SendReply(arg, "\n>> Текущии кланы с участниками <<\n" + textTable.ToString()); 
		}  
		[ConsoleCommand("clans.show")] 
		void cclansShow(ConsoleSystem.Arg arg) 
		{
			if (arg.Connection != null && arg.Connection.authLevel < 1) 
				return; 
			if (arg.Args == null || arg.Args.Length < 1) 
			{
				SendReply(arg, "Используйте: <color=white>clans.show <ТЕГ></color>"); 
				return; 
			} 
			
			Clan clan; 
			
			if (!clans.TryGetValue(arg.Args[0], out clan)) 
				if (!clans.TryGetValue(arg.Args[0].ToUpper(), out clan)) 
				{
					SendReply(arg, string.Format(msg("Клана с тегом [<color=#ffd479>{0}</color>] не существует"), arg.Args[0])); 
					return; 
				} 
			
			var sb = new StringBuilder(); 
				sb.AppendLine($"\n>> Клан [<color=white>{clan.tag}</color>] <<"); 
				sb.AppendLine($"Описание: <color=white>{clan.description}</color>"); 
				sb.AppendLine($"Время создания: <color=white>{UnixTimeStampToDateTime(clan.created)}</color>"); 
				sb.AppendLine($"Последнии обновления: <color=white>{UnixTimeStampToDateTime(clan.updated)}</color>"); 
				sb.AppendLine($"Кол-во участников: <color=white>{clan.total}</color>"); 
				
			TextTable textTable = new TextTable(); 
				textTable.AddColumn("Уровень"); 
				textTable.AddColumn("Имя"); 
				textTable.AddColumn("SteamID"); 
				textTable.AddColumn("Статус"); 
				
				sb.AppendLine(); 
				
				foreach (var memberid in clan.members) 
				{
					var member = this.covalence.Players.FindPlayer(memberid); 
					
					if (member == null) 
						continue; 
					
					textTable.AddRow(new string[] 
					{
						clan.PlayerLevel(member.Id), 
						member.Name, 
						member.Id.ToString(), 
						(member.IsConnected ? "<color=green>Онлайн</color>" : "<color=red>Оффлайн</color>").ToString() 
					}); 
				} 
				
				sb.AppendLine(textTable.ToString()); 
				SendReply(arg, sb.ToString()); 
		}  
		[ConsoleCommand("clans.msg")] 
		void cclansBroadcast(ConsoleSystem.Arg arg) 
		{
			if (arg.Connection != null && arg.Connection.authLevel < 1) 
				return; 
			if (arg.Args == null || arg.Args.Length < 2) 
			{ 
				SendReply(arg, "Используйте: <color=white>clans.msg <ТЕГ> <сообщение></color>"); 
				return; 
			} 
			
			Clan clan; 
			
			if (!clans.TryGetValue(arg.Args[0], out clan)) 
				if (!clans.TryGetValue(arg.Args[0].ToUpper(), out clan)) 
				{
					SendReply(arg, string.Format(msg("Клана с тегом [<color=#ffd479>{0}</color>] не существует"), arg.Args[0])); 
					return; 
				} 
				
			string BroadcastBy = consoleName; 
			
			if (arg.Connection != null) 
			{
				if (arg.Connection.authLevel == 2) 
					BroadcastBy = "[АДМИН] " + arg.Connection.username; 
				else 
					BroadcastBy = "[Модер] " + arg.Connection.username; 
			} 
			
			string Msg = ""; 
			
			for (int i = 1; i < arg.Args.Length; i++) Msg = Msg + " " + arg.Args[i]; 
			
			clan.BroadcastChat($"<color={clanServerColor}>{BroadcastBy}</color>: {Msg}"); 
			SendReply(arg, $"Broadcast to [{clan.tag}]: {Msg}"); 
		}  
		[ConsoleCommand("clans.rename")] 
		void cclansRename(ConsoleSystem.Arg arg) 
		{
			if (arg.Connection != null && arg.Connection.authLevel < authLevelRename) 
				return; 
			if (arg.Args == null || arg.Args.Length < 2) 
			{
				SendReply(arg, "Используйте: <color=white>clans.rename <СТАРЫЙ ТЕГ> <НОВЫЙ ТЕГ></color>"); 
				return; 
			} 
			
			Clan clan; 
			
			if (!clans.TryGetValue(arg.Args[0], out clan)) 
				if (!clans.TryGetValue(arg.Args[0].ToUpper(), out clan)) 
				{
					SendReply(arg, string.Format(msg("Клана с тегом [<color=#ffd479>{0}</color>] не существует"), arg.Args[0])); 
					return; 
				} 
			if (tagReExt.IsMatch(arg.Args[1])) 
			{
				SendReply(arg, string.Format(msg("<color=white>Тег клана должен содержать только такие символы как: '</color><color=#ffd479>a-z</color><color=white>' '</color><color=#ffd479>A-Z</color><color=white>' '</color><color=#ffd479>0-9</color><color=white>' '</color><color=#ffd479>а-я</color><color=white>' '</color><color=#ffd479>А-Я</color><color=white>' '</color><color=#ffd479>{0}</color><color=white>'</color>"), allowedSpecialChars)); 
				return; 
			} 
			if (arg.Args[1].Length < tagLengthMin || arg.Args[1].Length > tagLengthMax) 
			{
				SendReply(arg, string.Format(msg("Тег клана должен содержать от <color=#ffd479>{0}</color> до <color=#ffd479>{1}</color> символов"), tagLengthMin, tagLengthMax)); 
				return; 
			} 
			if (clans.ContainsKey(arg.Args[1])) 
			{
				SendReply(arg, string.Format(msg("<color=white>Какой-то клан уже использует этот тег.</color>"))); 
				return; 
			} 
			
			string oldtag = clan.tag; 
				clan.tag = arg.Args[1]; 
				clan.online = 0;
				clans.Add(clan.tag, clan); 
				clans.Remove(oldtag); 
				
			setupPlayers(clan.members);  
			
			string oldGroup = permGroupPrefix + oldtag; 
			string newGroup = permGroupPrefix + clan.tag; 
			
			if (permission.GroupExists(oldGroup)) 
			{
				foreach (var member in clan.members) 
					if (permission.UserHasGroup(member, oldGroup)) 
						permission.RemoveUserGroup(member, oldGroup); 
						
						permission.RemoveGroup(oldGroup); 
			} 
			if (usePermGroups && !permission.GroupExists(newGroup)) 
				permission.CreateGroup(newGroup, "Clan " + clan.tag, 0); 
				
			foreach (var member in clan.members) 
				if (usePermGroups && !permission.UserHasGroup(member, newGroup)) 
					permission.AddUserGroup(member, newGroup); 
			
			string RenamedBy = consoleName; 
			
			if (arg.Connection != null) 
				RenamedBy = arg.Connection.username; 
				
			foreach (var ally in clans) 
			{
				Clan allyClan = clans[ally.Key]; 
				
				if (allyClan.clanAlliances.Contains(oldtag)) 
				{
					allyClan.clanAlliances.Remove(oldtag); 
					allyClan.clanAlliances.Add(clan.tag); 
				} 
				if (allyClan.invitedAllies.Contains(oldtag)) 
				{
					allyClan.invitedAllies.Remove(oldtag); 
					allyClan.invitedAllies.Add(clan.tag); 
				} 
				if (allyClan.pendingInvites.Contains(oldtag)) 
				{
					allyClan.pendingInvites.Remove(oldtag); 
					allyClan.pendingInvites.Add(clan.tag); 
				} 
			} 
			
			clan.BroadcastLoc("<color=#ffd479>{0}</color> переименовал клан на: [<color=#ffd479>{1}</color>]", clan.tag); 
			SendReply(arg, string.Format(msg("Вы переименовали клан [<color=#ffd479>{0}</color>] на [<color=#ffd479>{1}</color>]"), oldtag, clan.tag)); 
			clan.onUpdate(); 
		}  
		[ConsoleCommand("clans.playerinvite")] 
		void cclansPlayerInvite(ConsoleSystem.Arg arg) 
		{
			if (arg.Connection != null && arg.Connection.authLevel < authLevelInvite) 
				return; 
			if (arg.Args == null || arg.Args.Length < 2) 
			{
				SendReply(arg, "Используйте: <color=white>clans.playerinvite <ТЕГ> <игрок></color>"); 
				return; 
			} 
			
			Clan myClan; 
			Clan check; 
			
			if (!clans.TryGetValue(arg.Args[0], out check)) 
			{
				SendReply(arg, string.Format(msg("Клана с тегом [<color=#ffd479>{0}</color>] не существует"), arg.Args[0])); 
				return; 
			} 
			else 
				myClan = (Clan)check;  
				
			var invPlayer = myClan.GetIPlayer(arg.Args[1]); 
			
			if (invPlayer == null) 
			{
				SendReply(arg, string.Format(msg("<color=white>Игрока с никнеймом</color> <color=#ffd479>{0}</color> <color=white>нет, либо вы ввели неправильно никнейм.</color>"), arg.Args[1])); 
				return; 
			} 
			if (myClan.members.Contains(invPlayer.Id)) 
			{
				SendReply(arg, string.Format(msg("<color=white>Игрок</color> <color=#ffd479>{0}</color> <color=white>уже состоит в вашем клане.</color>"), invPlayer.Name)); 
				return; 
			} 
			if (myClan.invites.ContainsKey(invPlayer.Id)) 
			{
				SendReply(arg, string.Format(msg("<color=white>Игрок</color> <color=#ffd479>{0}</color> <color=white>уже приглашен в ваш клан.</color>"), invPlayer.Name)); 
				return; 
			} 
			if (findClanByUser(invPlayer.Id) != null) 
			{
				SendReply(arg, string.Format(msg("<color=white>Игрок</color> <color=#ffd479>{0}</color> <color=white>уже состоит в клане.</color>"), invPlayer.Name)); 
				return; 
			} 
			
			myClan.invites.Add(invPlayer.Id, UnixTimeStampUTC()); 
			
			if (!pendingPlayerInvites.ContainsKey(invPlayer.Id)) 
				pendingPlayerInvites.Add(invPlayer.Id, new List<string>()); 
				
				pendingPlayerInvites[invPlayer.Id].Add(myClan.tag); 
				
			if (invPlayer.IsConnected) 
			{
				var invited = rust.FindPlayerByIdString(invPlayer.Id); 
				
				if (invited != null) 
					PrintChat(invited, string.Format(msg("<color=white>Вас пригласили присоединиться к клану:</color> [<color=#ffd479>{0}</color>] '<color=#ffd479>{1}</color>'\n<color=white>Чтобы присоединиться, введите:</color> <color=#ffd479>/clan join {0}</color>", invPlayer.Id), myClan.tag, myClan.description, colorCmdUsage)); 
			} 
			
			myClan.updated = UnixTimeStampUTC(); 
			SendReply(arg, $"Invitation for clan '{myClan.tag}' sent to '{invPlayer.Name}'"); 	
		}  
		[ConsoleCommand("clans.playerkick")] 
		void cclansPlayerKick(ConsoleSystem.Arg arg) 
		{
			if (arg.Connection != null && arg.Connection.authLevel < authLevelKick) 
				return; 
			if (arg.Args == null || arg.Args.Length < 2) 
			{
				SendReply(arg, "Используйте: <color=white>clans.playerkick <ТЕГ> <игрок></color>"); 
				return; 
			} 
			
			Clan myClan; 
			Clan check; 
			
			if (!clans.TryGetValue(arg.Args[0], out check)) 
			{
				SendReply(arg, string.Format(msg("Клана с тегом [<color=#ffd479>{0}</color>] не существует"), arg.Args[0])); 
				return; 
			} 
			else 
				myClan = (Clan)check; 
				
				var kickPlayer = myClan.GetIPlayer(arg.Args[1]); 
				
			if (kickPlayer == null) 
			{
				SendReply(arg, string.Format(msg("<color=white>Игрока с никнеймом</color> <color=#ffd479>{0}</color> <color=white>нет, либо вы ввели неправильно никнейм.</color>"), arg.Args[1])); 
				return; 
			} 
			if (!myClan.IsMember(kickPlayer.Id) && !myClan.IsInvited(kickPlayer.Id)) 
			{
				SendReply(arg, string.Format(msg("Игрок <color=#ffd479>{0}</color> не участник вашего клана."), kickPlayer.Name)); 
				return; 
			} 
			if (myClan.members.Count() == 1) 
			{
				SendReply(arg, "В клане только один участник. Вам нужно удалить клан"); 
				return; 
			} 
			if (myClan.members.Contains(kickPlayer.Id)) 
				myClan.total--; 
				
				myClan.members.Remove(kickPlayer.Id); 
				myClan.invites.Remove(kickPlayer.Id); 
				
			if (myClan.IsCouncil(kickPlayer.Id)) 
				myClan.council = null; 
				
				myClan.moderators.Remove(kickPlayer.Id); 
				myClan.members.Remove(kickPlayer.Id); 
				myClan.invites.Remove(kickPlayer.Id); 
				
			bool ownerChanged = false; 
			
			if (myClan.IsOwner(kickPlayer.Id) && myClan.members.Count() > 0) 
			{
				myClan.owner = myClan.members[0]; 
				ownerChanged = true; 
			} 
			if (pendingPlayerInvites.ContainsKey(kickPlayer.Id)) 
				pendingPlayerInvites[kickPlayer.Id].Remove(myClan.tag); 
				
			clanCache.Remove(kickPlayer.Id); 
			
			var kickBasePlayer = rust.FindPlayerByIdString(kickPlayer.Id); 
			
			if (kickBasePlayer != null) 
			{
				setupPlayer(kickBasePlayer, kickPlayer.Name, kickPlayer.Id); 		
			} 
			if (usePermGroups && permission.UserHasGroup(kickPlayer.Id, permGroupPrefix + myClan.tag)) 
				permission.RemoveUserGroup(kickPlayer.Id, permGroupPrefix + myClan.tag); 
				
				myClan.updated = UnixTimeStampUTC(); 
				myClan.onUpdate(); 
				
			SendReply(arg, $"Игрок '<color=white>{kickPlayer.Name}</color>' был выгнан с клана '<color=white>{myClan.tag}</color>'"); 
			
			if (ownerChanged) 
			{
				var newOwner = myClan.GetIPlayer(myClan.owner); 
				
				if (newOwner != null) 
					SendReply(arg, $"<color=white>{newOwner.Name}</color> является новым владельцем клана '<color=white>{myClan.tag}</color>'"); 
			} 		
		}  
		[ConsoleCommand("clans.playerpromote")] 
		void cclansPlayerPromote(ConsoleSystem.Arg arg) 
		{
			if (arg.Connection != null && arg.Connection.authLevel < authLevelPromoteDemote) 
				return; 
			if (arg.Args == null || arg.Args.Length < 2) 
			{
				SendReply(arg, "Используйте: <color=white>clans.playerpromote <ТЕГ> <игрок></color>"); 
				return; 
			} 
			
			Clan myClan; 
			Clan check; 
			
			if (!clans.TryGetValue(arg.Args[0], out check)) 
			{
				SendReply(arg, string.Format(msg("Клана с тегом [<color=#ffd479>{0}</color>] не существует"), arg.Args[0])); 
				return; 
			} 
			else 
				myClan = (Clan)check; 
				
			var promotePlayer = myClan.GetIPlayer(arg.Args[1]); 
			
			if (promotePlayer == null)
			{
				SendReply(arg, string.Format(msg("<color=white>Игрока с никнеймом</color> <color=#ffd479>{0}</color> <color=white>нет, либо вы ввели неправильно никнейм.</color>"), arg.Args[1])); 
				return; 
			} 
			if (!myClan.IsMember(promotePlayer.Id)) 
			{
				SendReply(arg, string.Format(msg("Игрок <color=#ffd479>{0}</color> не участник вашего клана."), promotePlayer.Name)); 
				return; 
			} 
			if (enableClanAllies && myClan.IsCouncil(promotePlayer.Id)) 
			{
				SendReply(arg, string.Format(msg("Игрок <color=#ffd479>{0}</color> уже является советом вашего клана."), promotePlayer.Name)); 
				return; 
			} 
			if (enableClanAllies && myClan.council != null && myClan.IsModerator(promotePlayer.Id)) 
			{
				SendReply(arg, string.Format(msg("Должность совета уже присуждена."), promotePlayer.Name)); 
				return; 
			} 
			if (!enableClanAllies && myClan.IsModerator(promotePlayer.Id)) 
			{
				SendReply(arg, string.Format(msg("Игрок <color=#ffd479>{0}</color> уже является модератором вашего клана."), promotePlayer.Name)); 
				return;
			} 
			if (!myClan.IsModerator(promotePlayer.Id) && limitModerators >= 0 && myClan.moderators.Count() >= limitModerators) 
			{
				SendReply(arg, string.Format(msg("В этом клане уже достигнуто максимальное количество модераторов."))); 
				return; 
			} 
			
			string PromotedBy = consoleName; 
			
			if (arg.Connection != null) 
				PromotedBy = arg.Connection.username; 
			if (enableClanAllies && myClan.IsModerator(promotePlayer.Id)) 
			{
				myClan.council = promotePlayer.Id; 
				myClan.moderators.Remove(promotePlayer.Id); 
				myClan.BroadcastLoc("playerpromotedcouncil", $"<color={clanServerColor}>{PromotedBy}</color>", myClan.ColNam(promotePlayer.Id, promotePlayer.Name)); 
			} 
			else 
			{
				myClan.moderators.Add(promotePlayer.Id); 
				myClan.BroadcastLoc("playerpromoted", $"<color={clanServerColor}>{PromotedBy}</color>", myClan.ColNam(promotePlayer.Id, promotePlayer.Name)); 
			} 
			
			myClan.updated = UnixTimeStampUTC(); 
			myClan.onUpdate(); 
			
			SendReply(arg, $"You promoted '{promotePlayer.Name}' to a {myClan.PlayerLevel(promotePlayer.Id.ToString())}"); 
		}  
		[ConsoleCommand("clans.playerdemote")] 
		void cclansPlayerDemote(ConsoleSystem.Arg arg) 
		{
			if (arg.Connection != null && arg.Connection.authLevel < authLevelPromoteDemote) 
				return; 
			if (arg.Args == null || arg.Args.Length < 2) 
			{
				SendReply(arg, "Используйте: <color=white>clans.playerdemote <ТЕГ> <игрок></color>"); 
				return; 
			} 
			
			Clan myClan; 
			Clan check;
			
			if (!clans.TryGetValue(arg.Args[0], out check)) 
			{
				SendReply(arg, string.Format(msg("Клана с тегом [<color=#ffd479>{0}</color>] не существует"), arg.Args[0])); 
				return; 
			} 
			else 
				myClan = (Clan)check; 
				
			var demotePlayer = myClan.GetIPlayer(arg.Args[1]); 
			
			if (demotePlayer == null) 
			{
				SendReply(arg, string.Format(msg("<color=white>Игрока с никнеймом</color> <color=#ffd479>{0}</color> <color=white>нет, либо вы ввели неправильно никнейм.</color>"), arg.Args[1])); 
				return; 
			} 
			if (!myClan.IsMember(demotePlayer.Id)) 
			{
				SendReply(arg, string.Format(msg("Игрок <color=#ffd479>{0}</color> не участник вашего клана."), demotePlayer.Name)); 
				return; 
			} 
			if (!myClan.IsModerator(demotePlayer.Id) && !myClan.IsCouncil(demotePlayer.Id)) 
			{
				SendReply(arg, string.Format(msg("Игрок <color=#ffd479>{0}</color> не модератор/совет вашего клана."), demotePlayer.Name)); 
				return; 
			} 
			
			string DemotedBy = consoleName; 
			
			if (arg.Connection != null) 
				DemotedBy = arg.Connection.username; 
			if (enableClanAllies && myClan.IsCouncil(demotePlayer.Id)) 
			{
				myClan.council = null; 
				if (limitModerators >= 0 && myClan.moderators.Count() >= limitModerators) 
					myClan.BroadcastLoc("Игрок <color=#ffd479>{0}</color> понизил <color=#ffd479>{1}</color> до участника.", $"<color={clanServerColor}>{DemotedBy}</color>", 
					myClan.ColNam(demotePlayer.Id, demotePlayer.Name)); 
				else 
				{ 
					myClan.moderators.Add(demotePlayer.Id); 
					myClan.BroadcastLoc("Игрок <color=#ffd479>{0}</color> повысил <color=#ffd479>{1}</color> до модератора.", $"<color={clanServerColor}>{DemotedBy}</color>", myClan.ColNam(demotePlayer.Id, demotePlayer.Name)); 
				} 
			} 
			else 
			{
				myClan.moderators.Remove(demotePlayer.Id); 
				myClan.BroadcastLoc("Игрок <color=#ffd479>{0}</color> понизил <color=#ffd479>{1}</color> до участника.", $"<color={clanServerColor}>{DemotedBy}</color>", myClan.ColNam(demotePlayer.Id, demotePlayer.Name)); 
			} 
			
			myClan.updated = UnixTimeStampUTC(); 
			myClan.onUpdate(); 
			
			SendReply(arg, $"You demoted '{demotePlayer.Name}' to a {myClan.PlayerLevel(demotePlayer.Id.ToString())}"); 
		}  
		[ConsoleCommand("clans.delete")] 
		void cclansDelete(ConsoleSystem.Arg arg) 
		{
			if (arg.Connection != null && arg.Connection.authLevel < authLevelDelete) 
				return; 
			if (arg.Args == null || arg.Args.Length != 1) 
			{
				SendReply(arg, "Используйте: <color=white>clans.delete <ТЕГ></color>"); 
				return; 
			} 
			
			Clan clan; 
			
			if (!clans.TryGetValue(arg.Args[0], out clan)) 
			{
				SendReply(arg, string.Format(msg("Клана с тегом [<color=#ffd479>{0}</color>] не существует"), arg.Args[0])); 
				return; 
			} 
			
			string DeletedBy = consoleName; 
			
			if (arg.Connection != null) 
				DeletedBy = arg.Connection.username; 
				
			clan.BroadcastLoc("<color=#ffd479>{0}</color> удалил ваш клан.", $"<color={clanServerColor}>{DeletedBy}</color>"); 
			clans.Remove(arg.Args[0]); 
			
			foreach (var member in clan.members) clanCache.Remove(member); 
			
			setupPlayers(clan.members); 
			
			string permGroup = permGroupPrefix + clan.tag; 
			
			if (permission.GroupExists(permGroup)) 
			{
				foreach (var member in clan.members) 
					if (permission.UserHasGroup(member, permGroup)) 
						permission.RemoveUserGroup(member, permGroup); 
						
				permission.RemoveGroup(permGroup); 
			} 
			
			foreach (var ally in clans) 
			{
				Clan allyClan = clans[ally.Key]; 
					allyClan.clanAlliances.Remove(arg.Args[0]); 
					allyClan.invitedAllies.Remove(arg.Args[0]); 
					allyClan.pendingInvites.Remove(arg.Args[0]); 
			} 
			
			SendReply(arg, string.Format(msg("Вы удалили клан [<color=#ffd479>{0}</color>]"), clan.tag)); 
			
			clan.onDestroy(); 
			AllyRemovalCheck(); 
		}  
		
		bool FilterText(string tag) 
		{
			foreach (string bannedword in wordFilter) 
				if (TranslateLeet(tag).ToLower().Contains(bannedword.ToLower())) 
					return true; 
			return false; 
		}  
		string TranslateLeet(string original) 
		{
			string translated = original; 
			
			Dictionary<string, string> leetTable = new Dictionary<string, string> 
			{ 
				{ "}{", "h" }, 
				{ "|-|", "h" }, 
				{ "]-[", "h" }, 
				{ "/-/", "h" }, 
				{ "|{", "k" }, 
				{ "/\\/\\", "m" }, 
				{ "|\\|", "n" }, 
				{ "/\\/", "n" }, 
				{ "()", "o" }, 
				{ "[]", "o" }, 
				{ "vv", "w" }, 
				{ "\\/\\/", "w" }, 
				{ "><", "x" }, 
				{ "2", "z" }, 
				{ "4", "a" }, 
				{ "@", "a" }, 
				{ "8", "b" }, 
				{ "ß", "b" }, 
				{ "(", "c" }, 
				{ "<", "c" }, 
				{ "{", "c" }, 
				{ "3", "e" }, 
				{ "€", "e" }, 
				{ "6", "g" }, 
				{ "9", "g" }, 
				{ "&", "g" }, 
				{ "#", "h" }, 
				{ "$", "s" }, 
				{ "7", "t" }, 
				{ "|", "l" }, 
				{ "1", "i" }, 
				{ "!", "i" },
				{ "0", "o" }, 
			}; 
			
			foreach (var leet in leetTable) translated = translated.Replace(leet.Key, leet.Value); 
			return translated; 
		}  
		[HookMethod("EnableBypass")] 
		void EnableBypass(object userId) 
		{
			if (!enableFFOPtion || userId == null) 
				return; 
			if (userId is string) 
				userId = Convert.ToUInt64((string)userId); 
			
			bypass.Add((ulong)userId); 
		}  
		[HookMethod("DisableBypass")] 
		void DisableBypass(object userId) 
		{
			if (!enableFFOPtion || userId == null) 
				return; 
			if (userId is string) 
				userId = Convert.ToUInt64((string)userId); 
				
			bypass.Remove((ulong)userId); 
		} 
	} 
}