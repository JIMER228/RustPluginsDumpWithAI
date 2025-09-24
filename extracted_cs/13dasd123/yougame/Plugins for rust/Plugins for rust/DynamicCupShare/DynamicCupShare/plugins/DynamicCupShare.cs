using System; 
using System.Collections.Generic; 
using System.Linq; 
using System.Text; 
using System.Reflection; 
using Oxide.Core; 
using Oxide.Core.Libraries; 
using Oxide.Core.Plugins; 
using UnityEngine; 
using ProtoBuf; 
using Newtonsoft.Json.Linq;  

namespace Oxide.Plugins 
{
	[Info("DynamicCupShare", "S1m0n", "1.9.1", ResourceId = 20)] 
	[Description("Dynamic sharing of cupboards/doors/boxes/lockers/turrets")]
	class DynamicCupShare : RustPlugin 
	{ 
		[PluginReference] Plugin Clans;  
		[PluginReference] Plugin Friends;  
		
		bool configRemoval = false; 
		bool Changed = false; 
		bool Initialized = false;
		bool clansEnabled = false; 
		bool friendsEnabled = false; 
		bool friendsIOEnabled = false;
		bool friendsAPIEnabled = false; 
		bool rustIOInstalled = false; 
		bool pluginDisabled = false; 
		bool playerDataLoaded = false;  
		
		List<ulong> usedConsoleInput = new List<ulong>(); 
		Dictionary <string, bool> adminAccessEnabled = new Dictionary <string, bool>(); 
		StoredData playerPrefs = new StoredData(); 
		FieldInfo _buildingPrivilege = typeof(BasePlayer).GetField("buildingPrivilege", (BindingFlags.Instance | BindingFlags.NonPublic)); 
		FieldInfo _whitelistPlayers = typeof(CodeLock).GetField("whitelistPlayers", (BindingFlags.Instance | BindingFlags.NonPublic)); 
		FieldInfo _nextShotTime = typeof(AutoTurret).GetField("nextShotTime", (BindingFlags.Instance | BindingFlags.NonPublic)); 
		FieldInfo _nextTargetScanTime = typeof(AutoTurret).GetField("nextTargetScanTime", (BindingFlags.Instance | BindingFlags.NonPublic)); 
		FieldInfo _nextVisCheck = typeof(AutoTurret).GetField("nextVisCheck", (BindingFlags.Instance | BindingFlags.NonPublic)); 
		
		int collLayers = UnityEngine.LayerMask.GetMask(new string[] 
		{
			"Construction", 
			"Deployed", 
			"Tree", 
			"Terrain", 
			"Resource", 
			"World", 
			"Water", 
			"Default" 
		});  
		
		class StoredData 
		{
			public Dictionary<ulong, PlayerInfo> PlayerInfo = new Dictionary<ulong, PlayerInfo>(); 
			public StoredData(){} 
		}  
		class PlayerInfo 
		{
			public bool CS; 
			public bool DS;
			public bool BS; 
			public bool TS; 
			public bool LS; 
			public bool AA; 
			public bool CCS; 
			public bool CDS; 
			public bool CBS; 
			public bool CLS; 
			public bool CTS; 
			public PlayerInfo(){} 
		}  
		
		string shareCommand; 
		bool useFriendsApi; 
		bool useFriendsIO; 
		bool useClans;  
		bool blockCupAuthClanMembers; 
		bool blockCupAuthFriends; 
		bool blockCupClearClanMembers; 
		bool blockCupClearFriends; 
		bool blockBuildIntoBlocked;  
		string permGetClanShares; 
		bool usePermGetClanShares; 
		string permGetFriendShares; 
		bool usePermGetFriendShares; 
		string permAutoAuth; 
		bool usePermAutoAuth;  
		bool clanTurretShareOverride; 
		bool enableCupSharing; 
		bool enableDoorSharing; 
		bool enableBoxSharing; 
		bool enableLockerSharing; 
		bool enableTurretSharing; 
		bool enableAutoAuth; 
		bool notifyAuthCupboard; 
		bool notifyAuthTurret; 
		bool CupShare; 
		bool DoorShare; 
		bool TurretShare; 
		bool BoxShare; 
		bool LockerShare; 
		bool AutoAuth; 
		bool ClanCupShare; 
		bool ClanDoorShare; 
		bool ClanBoxShare; 
		bool ClanLockerShare; 
		bool ClanTurretShare; 
		bool toggleCupShare; 
		bool toggleDoorShare; 
		bool toggleTurretShare; 
		bool toggleBoxShare; 
		bool toggleLockerShare; 
		bool toggleAutoAuth; 
		bool toggleClanCupShare; 
		bool toggleClanDoorShare; 
		bool toggleClanBoxShare; 
		bool toggleClanLockerShare; 
		bool toggleClanTurretShare; 
		string pluginPrefix; 
		string prefixColor; 
		string prefixFormat; 
		string colorTextMsg; 
		string colorCmdUsage; 
		string colorON; 
		string colorOFF;  
		
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
			shareCommand = Convert.ToString(GetConfig("Command", "shareCommand", "share")); 
			useFriendsApi = Convert.ToBoolean(GetConfig("Options", "useFriendsApi", true)); 
			useFriendsIO = Convert.ToBoolean(GetConfig("Options", "useFriendsIO", false)); 
			useClans = Convert.ToBoolean(GetConfig("Options", "useClans", true));  
			permGetClanShares = Convert.ToString(GetConfig("Permission", "permGetClanShares", "dynamiccupshare.getclanshares")); 
			usePermGetClanShares = Convert.ToBoolean(GetConfig("Permission", "usePermGetClanShares", false)); 
			permGetFriendShares = Convert.ToString(GetConfig("Permission", "permGetFriendShares", "dynamiccupshare.getfriendshares")); 
			usePermGetFriendShares = Convert.ToBoolean(GetConfig("Permission", "usePermGetFriendShares", false)); 
			permAutoAuth = Convert.ToString(GetConfig("Permission", "permGetShares", "dynamiccupshare.autoauth")); 
			usePermAutoAuth = Convert.ToBoolean(GetConfig("Permission", "usePermAutoAuth", false));  
			
			clanTurretShareOverride = Convert.ToBoolean(GetConfig("Security", "clanTurretShareOverride", false)); 
			blockCupAuthClanMembers = Convert.ToBoolean(GetConfig("Security", "blockCupAuthClanMembers", true)); 
			blockCupAuthFriends = Convert.ToBoolean(GetConfig("Security", "blockCupAuthFriends", true)); 
			blockCupClearClanMembers = Convert.ToBoolean(GetConfig("Security", "blockCupClearClanMembers", true)); 
			blockCupClearFriends = Convert.ToBoolean(GetConfig("Security", "blockCupClearFriends", true)); 
			blockBuildIntoBlocked = Convert.ToBoolean(GetConfig("Security", "blockBuildIntoBlocked", true)); 
			 
			notifyAuthCupboard = Convert.ToBoolean(GetConfig("Notification", "notifyAuthCupboard", true)); 
			notifyAuthTurret = Convert.ToBoolean(GetConfig("Notification", "notifyAuthTurret", true));  
			
			CupShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "CupShare", false)); 
			DoorShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "DoorShare", false)); 
			TurretShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "TurretShare", false)); 
			BoxShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "BoxShare", false)); 
			LockerShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "LockerShare", false)); 
			AutoAuth = Convert.ToBoolean(GetConfig("PlayerDefaults", "AutoAuth", true)); 
			ClanCupShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "ClanCupShare", true)); 
			ClanDoorShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "ClanDoorShare", true)); 
			ClanBoxShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "ClanBoxShare", true)); 
			ClanLockerShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "ClanLockerShare", true)); 
			ClanTurretShare = Convert.ToBoolean(GetConfig("PlayerDefaults", "ClanTurretShare", true));  
			
			toggleCupShare = Convert.ToBoolean(GetConfig("PlayerToggles", "CupShare", true)); 
			toggleDoorShare = Convert.ToBoolean(GetConfig("PlayerToggles", "DoorShare", true)); 
			toggleTurretShare = Convert.ToBoolean(GetConfig("PlayerToggles", "TurretShare", true)); 
			toggleBoxShare = Convert.ToBoolean(GetConfig("PlayerToggles", "BoxShare", true));
			toggleLockerShare = Convert.ToBoolean(GetConfig("PlayerToggles", "LockerShare", true)); 
			toggleAutoAuth = Convert.ToBoolean(GetConfig("PlayerToggles", "AutoAuth", true)); 
			toggleClanCupShare = Convert.ToBoolean(GetConfig("PlayerToggles", "ClanCupShare", true)); 
			toggleClanDoorShare = Convert.ToBoolean(GetConfig("PlayerToggles", "ClanDoorShare", true)); 
			toggleClanBoxShare = Convert.ToBoolean(GetConfig("PlayerToggles", "ClanBoxShare", true)); 
			toggleClanLockerShare = Convert.ToBoolean(GetConfig("PlayerToggles", "ClanLockerShare", true)); 
			toggleClanTurretShare = Convert.ToBoolean(GetConfig("PlayerToggles", "ClanTurretShare", true)); 
			 
			enableCupSharing = Convert.ToBoolean(GetConfig("Functions", "enableCupSharing", true)); 
			enableDoorSharing = Convert.ToBoolean(GetConfig("Functions", "enableDoorSharing", true)); 
			enableBoxSharing = Convert.ToBoolean(GetConfig("Functions", "enableBoxSharing", true)); 
			enableLockerSharing = Convert.ToBoolean(GetConfig("Functions", "enableLockerSharing", true)); 
			enableTurretSharing = Convert.ToBoolean(GetConfig("Functions", "enableTurretSharing", true)); 
			enableAutoAuth = Convert.ToBoolean(GetConfig("Functions", "enableAutoAuth", true));  
			
			pluginPrefix = Convert.ToString(GetConfig("Formatting", "pluginPrefix", "DynamicShare")); 
			prefixColor = Convert.ToString(GetConfig("Formatting", "prefixColor", "orange")); 
			
			if (Config.Get("CupCheck") != null) 
			{
				configRemoval = true; 
				Config.Remove("CupCheck");
				(Config.Get("Command") as Dictionary<string,object>).Remove("checkCommand"); 
				(Config.Get("PlayerToggles") as Dictionary<string,object>).Remove("LockShare"); 
				(Config.Get("PlayerToggles") as Dictionary<string,object>).Remove("ClanLockShare"); 
				(Config.Get("PlayerDefaults") as Dictionary<string,object>).Remove("LockShare"); 
				(Config.Get("PlayerDefaults") as Dictionary<string,object>).Remove("ClanLockShare");
				(Config.Get("Functions") as Dictionary<string,object>).Remove("enableLockSharing"); 
			}  
			if (!Changed && !configRemoval) 
				return; SaveConfig(); 
				
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
				{"ShareEnabled", "Доступ к шкафу для друзей включен"}, 
				{"ShareDisabled", "Доступ к шкафу для друзей выключен"}, 
				{"CodesEnabled", "Доступ к дверям для друзей включен"}, 
				{"CodesDisabled", "Доступ к дверям для друзей выключен"}, 
				{"BoxesEnabled", "Доступ к ящикам для друзей включен"}, 
				{"BoxesDisabled", "Доступ к ящикам для друзей выключен"}, 
				{"LockersEnabled", "Доступ к замкам для друзей включен"}, 
				{"LockersDisabled", "Доступ к замкам для друзей выключен"}, 
				{"TurretEnabled", "Доступ к турелям для друзей включен"}, 
				{"TurretDisabled", "Доступ к турелям для друзей выключен"}, 
				{"ClanShareEnabled", "Доступ к шкафу для клана включен"}, 
				{"ClanShareDisabled", "Доступ к шкафу для клана выключен"}, 
				{"ClanCodesEnabled", "Доступ к дверям для клана включен"}, 
				{"ClanCodesDisabled", "Доступ к дверям для клана выключен"}, 
				{"ClanBoxesEnabled", "Доступ к ящикам для клана включен"}, 
				{"ClanBoxesDisabled", "Доступ к ящикам для клана выключен"}, 
				{"ClanLockersEnabled", "Доступ к замкам для клана включен"}, 
				{"ClanLockersDisabled", "Доступ к замкам для клана выключен"}, 
				{"ClanTurretEnabled", "Доступ к турелям для клана включен"}, 
				{"ClanTurretDisabled", "Доступ к турелям для клана выключен"}, 
				{"AdminAccessEnabled", "Доступ администратора включен"}, 
				{"AdminAccessDisabled", "Доступ администратора выключен"}, 
				{"AutoAuthEnabled", "Автоматическая авторизация в шкафу включена"}, 
				{"AutoAuthDisabled", "Автоматическая авторизация в шкафу выключена"}, 
				{"CupAuth", "Cupboard authorized"}, 
				{"TurretAuth", "Turret authorized"}, 
				{"NoAccess", "Вам не предоставлена эта функция"}, 
				{"NotEnabled", "На данный момент функция '<color=#ffd479>{0}</color>' не активна"}, 
				{"SwitchBlocked", "Администратор заблокировал переключатель '{0}'"}, 
				{"NotSupported", "Функция '<color=#ffd479>{0}</color>' недоступна"}, 
				{"NotFound", "Игрок '<color=#ffd479>{0}</color>' не найден."}, 
				{"NeedArgs", "Укажите никнейм игрока."}, 
				{"CupAuthBlocked", "Авторизация запрещена. Этот шкаф уже доступен вам"}, 
				{"CupAuthClearBlocked", "Очистка списка игроков с доступом запрещена"}, 
				{"BlockBuildIntoBlocked", "Вы не можете строить в запрещенной зоне!"}, 
				{"DoorClanNotShared", "Участник клана '<color=#ffd479>{0}</color>' запретил доступ к дверям"}, 
				{"BoxClanNotShared", "Участник клана '<color=#ffd479>{0}</color>' запретил доступ к ящикам"}, 
				{"LockerClanNotShared", "Участник клана '<color=#ffd479>{0}</color>' запретил доступ к замкам"}, 
				{"CupClanNotShared", "Авторизация запрещена. Участник клана '<color=#ffd479>{0}</color>' запретил доступ к шкафам"}, 
				{"AccessRights", "Вы можете получить доступ к:"}, 
				{"CommandPlgDisabled", "Плагин отключен! Свяжитесь с администратором"}, 
				{"CommandUsage", "Команда:"}, 
				{"CommandToggle", "Все переключатели переключают свои настройки (вкл<>выкл)"}, 
				{"CommandFriendCup", "Шкафы друзей:"}, 
				{"CommandFriendDoor", "Двери друзей:"}, 
				{"CommandFriendBox", "Ящики друзей:"}, 
				{"CommandFriendLocker", "Замки друзей:"}, 
				{"CommandFriendTurret", "Турели друзей:"}, 
				{"CommandAutoAuth", "Авторизация шкафа / турели:"}, 
				{"CommandClanCup", "Шкафы клана:"}, 
				{"CommandClanDoor", "Двери клана:"}, 
				{"CommandClanBox", "Ящики клана:"}, 
				{"CommandClanLocker", "Замки клана:"}, 
				{"CommandClanTurret", "Нацеливание турелей на участников клана:"}, 
				{"CommandClanTurretM", "Турели клана:"}, 
				{"CommandAdminAccess", "Доступ администратора"}, 
				{"HelpCups", "Описание для обмена шкафами"}, 
				{"HelpDoors", "Описание для обмена дверями"}, 
				{"HelpBoxes", "Описание для обмена ящиками"}, 
				{"HelpLockers", "Описание для обмена замкам"}, 
				{"HelpTurrets", "Описание для обмена турелями"}, 
				{"HelpAutoAuth", "Описание для автоматической авторизации"}, 
				{"HelpNotAvailable", "Этого раздела помощи не существуют."}, 
				{"DescriptionCups", "Предоставляет возможность обмена шкафами среди друзей / участников клана, эти игроки получают права на строительство в зоне действия ваших шкафов, которые вы ставили, и где вы сами авторизированы. Он не разделяет, если вы не авторизованы."}, 
				{"DescriptionDoors", "Предоставляет возможность обмена дверьми среди друзей / участников клана, эти игроки могут открыть все ваши запертые двери без доступа к самому кодовому замку."}, 
				{"DescriptionBoxes", "Предоставляет возможность обмена ящиками среди друзей / участников клана, эти игроки могут открыть все ваши заблокированные ящики, не имея доступа к самому кодовому замку."}, 
				{"DescriptionLockers", "Предоставляет возможность обмена замками среди друзей / участников клана, эти игроки могут открыть все ваши замки без доступа к самому кодовому замку."}, 
				{"DescriptionTurrets", "Предоставляет возможность обмена турелями среди друзей / участников клана, эти игроки не будут убиты вашими турелями, не имея доступа к турели. Эта функция может быть переопределена на стороне сервера для того, чтобы у кланов функция была включена."}, 
				{"DescriptionAutoAuth", "Включив автоматическую авторизацию для шкафов и турелей, вы можете пропустить последующие авторизации."}, 
			},this); 
		} 
		
		Library lib; 
		MethodInfo hasFriend;  
		
		void InitializeRustIO() 
		{
			lib = Interface.GetMod().GetLibrary<Library>("RustIO"); 
			
			if (lib == null || lib.GetFunction("IsInstalled") == null || (hasFriend = lib.GetFunction("HasFriend")) == null ) 
				lib = null; 
			else 
				rustIOInstalled = true; 
		}  
		
		bool HasFriend(ulong owner, ulong friend) 
		{
			if (friendsIOEnabled) 
				return (bool)hasFriend.Invoke(lib, new object[] 
				{
					owner.ToString(), 
					friend.ToString() 
				}); 
			if (friendsAPIEnabled) 
				return (bool)Friends.CallHook("HasFriend", owner, friend); 
			return false; 
		}  
		bool SameClan(ulong owner, ulong member) 
		{
			var o = Clans.CallHook("GetClanOf", owner); 
			var m = Clans.CallHook("GetClanOf", member); 
			
			if (o != null && m != null && (string)o == (string)m) 
				return true; 
			return false; 
		}  
		
		void Init() 
		{
			LoadVariables(); 
			LoadDefaultMessages(); 
			
			cmd.AddChatCommand(shareCommand, this, "ShareCommand"); 
			cmd.AddConsoleCommand(shareCommand, this, "cShareCommand"); 
			
			LoadPlayerData(); 
			
			if (!permission.PermissionExists(permGetClanShares)) 
				permission.RegisterPermission(permGetClanShares, this); 
			if (!permission.PermissionExists(permGetFriendShares)) 
				permission.RegisterPermission(permGetFriendShares, this); 
			if (!permission.PermissionExists(permAutoAuth)) 
				permission.RegisterPermission(permAutoAuth, this); 
				
			var filter = Oxide.Game.Rust.RustExtension.Filter.ToList(); 
				filter.Add("DynamicCupShare - False (Boolean), Vanish (False (Boolean)"); 
				filter.Add("Vanish - False (Boolean), DynamicCupShare (False (Boolean)"); 
				filter.Add("DynamicCupShare - False (Boolean), TurretConfig (False (Boolean)"); 
				filter.Add("TurretConfig (False (Boolean), DynamicCupShare - False (Boolean)"); 
			
			Oxide.Game.Rust.RustExtension.Filter = filter.ToArray(); 
		}  
		void LoadPlayerData() 
		{
			playerPrefs = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(this.Title); 
			
			if (configRemoval) 
			{
				foreach (var pref in  playerPrefs.PlayerInfo.ToList()) 
				{
					playerPrefs.PlayerInfo[pref.Key].DS = playerPrefs.PlayerInfo[pref.Key].LS; 
					playerPrefs.PlayerInfo[pref.Key].LS = LockerShare; playerPrefs.PlayerInfo[pref.Key].CDS = playerPrefs.PlayerInfo[pref.Key].CLS; 
					playerPrefs.PlayerInfo[pref.Key].CLS = ClanLockerShare; 
				} 
				
				SaveData(); 
			} 
			
			playerDataLoaded = true; 
		}  
		void OnServerInitialized() 
		{
			if (Initialized) 
				return; 
				
			InitializeRustIO(); 
			
			if (Clans && useClans) 
			{
				clansEnabled = true; 
				Puts("Plugin 'Clans' found - Clan support activated"); 
			} 
			if (!Clans && useClans) 
				PrintWarning("Plugin 'Clans' not found - Clan support not active"); 
			if (useFriendsApi || useFriendsIO) 
			{
				if (rustIOInstalled && useFriendsIO) 
				{
					friendsEnabled = true; 
					friendsIOEnabled = true; 
					Puts("RustIO Friends found - Friends support activated"); 
				} 
				else if (Friends && useFriendsApi &&!friendsEnabled) 
				{
					friendsEnabled = true; 
					friendsAPIEnabled = true; 
					Puts("Plugin Friends found - Friends support activated"); 
				}
			} 
			if ((useFriendsApi || useFriendsIO) && !friendsEnabled) 
				PrintWarning("No Friend Plugin found - Friend support not active"); 
			if (!clansEnabled && !friendsEnabled) 
			{
				PrintWarning("No supported plugin found - Sharing disabled"); 
				pluginDisabled = true; 
			} 
			if(!pluginDisabled) 
			{
				foreach(var player in BasePlayer.activePlayerList) SetPlayer(player); 
				foreach(var player in BasePlayer.sleepingPlayerList) SetPlayer(player); 
				
				Interface.Oxide.DataFileSystem.WriteObject(this.Title, playerPrefs); 
				Initialized = true; 
			} 
		}  
		void SetPlayer(BasePlayer player) 
		{ 
			if (player == null) 
				return; 
			if (player.IsAdmin) 
				adminAccessEnabled[player.UserIDString] = false; 
			
			PlayerInfo p = null; 
			
			if (!playerPrefs.PlayerInfo.TryGetValue(player.userID, out p)) 
			{
				var info = new PlayerInfo(); 
					info.CS = CupShare; 
					info.DS = DoorShare; 
					info.TS = TurretShare; 
					info.BS = BoxShare; 
					info.LS = LockerShare; 
					info.AA = AutoAuth; 
					info.CCS = ClanCupShare; 
					info.CDS = ClanDoorShare; 
					info.CBS = ClanBoxShare; 
					info.CLS = ClanLockerShare; 
					info.CTS = ClanTurretShare; 
					
				playerPrefs.PlayerInfo.Add(player.userID, info); 
				return; 
			} 
		}  
		void AddPlayerData(ulong userID) 
		{
			var info = new PlayerInfo(); 
				info.CS = CupShare; 
				info.DS = DoorShare; 
				info.TS = TurretShare; 
				info.BS = BoxShare; 
				info.LS = LockerShare; 
				info.AA = AutoAuth; 
				info.CCS = ClanCupShare; 
				info.CDS = ClanDoorShare; 
				info.CBS = ClanBoxShare; 
				info.CLS = ClanLockerShare; 
				info.CTS = ClanTurretShare; 
				
			playerPrefs.PlayerInfo.Add(userID, info); 
		}  
		void OnPlayerInit(BasePlayer player) 
		{
			if (pluginDisabled) 
				return; 
				
			SetPlayer(player); 
		}  
		void Unload() 
		{
			SaveData(); 
			
			var filter = Oxide.Game.Rust.RustExtension.Filter.ToList(); 
				filter.Remove("DynamicCupShare - False (Boolean), Vanish (False (Boolean)"); 
				filter.Remove("Vanish - False (Boolean), DynamicCupShare (False (Boolean)"); 
				filter.Add("DynamicCupShare - False (Boolean), TurretConfig (False (Boolean)"); 
				filter.Add("TurretConfig (False (Boolean), DynamicCupShare - False (Boolean)"); 
				
			Oxide.Game.Rust.RustExtension.Filter = filter.ToArray(); 
		}  
		void OnServerSave() 
		{
			SaveData(); 
		}  
		void OnServerShutdown() 
		{
			var turrets = UnityEngine.Object.FindObjectsOfType<AutoTurret>(); 
			
			foreach(var turret in turrets) 
			{
				_nextShotTime.SetValue(turret, UnityEngine.Time.realtimeSinceStartup +30f); 
				_nextTargetScanTime.SetValue(turret, UnityEngine.Time.realtimeSinceStartup +30f); 
				_nextVisCheck.SetValue(turret, UnityEngine.Time.realtimeSinceStartup +30f); 
			} 
		}  
		
		bool FreshStart() 
		{
			if (UnityEngine.Time.realtimeSinceStartup < 30f) 
				return true; 
			return false; 
		}  
		
		void SaveData() 
		{
			if (pluginDisabled) 
				return; 
				
			Interface.Oxide.DataFileSystem.WriteObject(this.Title, playerPrefs); 
		}  
		void OnPluginUnloaded(Plugin name) 
		{
			if (name.Name == this.Title) 
				return; 
			if (name.Name == "Clans" && useClans && clansEnabled) 
			{
				clansEnabled = false; 
				Puts("Clans support disabled"); 
			} 
			if (useFriendsIO && friendsIOEnabled) 
				return; 
			if (name.Name == "Friends" && useFriendsApi && friendsAPIEnabled) 
			{
				friendsAPIEnabled = false; 
				friendsEnabled = false; Puts("Friends support disabled"); 
			} 
			if (!clansEnabled && !friendsEnabled && !pluginDisabled) 
			{
				pluginDisabled = true; 
				PrintWarning("Sharing functions disabled"); 
			}
		}  
		void OnPluginLoaded(Plugin name) 
		{
			if (name.Name == this.Title) 
				return; 
			if (name.Name == "Clans" && useClans && !clansEnabled) 
			{
				clansEnabled = true; 
				Puts("Clans support enabled"); 
			} 
			if (useFriendsIO && friendsIOEnabled) 
				return; 
			if (name.Name == "Friends" && useFriendsApi && !friendsAPIEnabled) 
			{
				friendsAPIEnabled = true; 
				friendsEnabled = true; 
				Puts("Friends support enabled");
			} 
			if ((clansEnabled || friendsEnabled) && pluginDisabled) 
			{
				pluginDisabled = false; 
				Puts("Sharing functions re-enabled"); 
			} 
		}  
		
		object OnTurretTarget(AutoTurret turret, BaseCombatEntity entity) 
		{
			if (pluginDisabled || !enableTurretSharing || turret == null || entity == null || !(entity is BasePlayer)) 
				return null; 
			if (!Initialized) 
				return false; 
			if (!playerPrefs.PlayerInfo.ContainsKey((turret as BaseEntity).OwnerID)) 
				AddPlayerData((turret as BaseEntity).OwnerID); 
			if ((entity as BasePlayer).IsAdmin && (bool)adminAccessEnabled[(entity as BasePlayer).UserIDString]) 
				return true; 
			if(!turret.authorizedPlayers.Any((PlayerNameID x) => x.userid == turret.OwnerID)) 
				return null; 
			if (clansEnabled && (clanTurretShareOverride || playerPrefs.PlayerInfo[(turret as BaseEntity).OwnerID].CTS) && SameClan((turret as BaseEntity).OwnerID,(entity as BasePlayer).userID)) 
				if (usePermGetClanShares && permission.UserHasPermission((entity as BasePlayer).UserIDString, permGetClanShares) || !usePermGetClanShares || clanTurretShareOverride) 
					return true; 
			if (friendsEnabled && playerPrefs.PlayerInfo[(turret as BaseEntity).OwnerID].TS && HasFriend((turret as BaseEntity).OwnerID,(entity as BasePlayer).userID)) 
				if (usePermGetFriendShares && permission.UserHasPermission((entity as BasePlayer).UserIDString, permGetFriendShares) || !usePermGetFriendShares) 
					return true; 
			return null;
		}  
		object CanBeTargeted(object obj, object turret) 
		{
			if (turret == null || turret is HelicopterTurret || pluginDisabled || !enableTurretSharing || (turret as BaseEntity).OwnerID == 0uL) 
				return null; 
			if (!Initialized) 
				return false; 
			if (obj == null || !(obj is BasePlayer)) 
				return null; 
			
			var player = obj as BasePlayer; 
			
			if (!playerPrefs.PlayerInfo.ContainsKey((turret as BaseEntity).OwnerID)) 
				AddPlayerData((turret as BaseEntity).OwnerID); 
			if ((player.IsAdmin && (bool)adminAccessEnabled[player.UserIDString])) 
				return false; 
			if (turret is FlameTurret && player.userID == (turret as BaseEntity).OwnerID) 
				return false; 
			if(turret is AutoTurret && !(turret as AutoTurret).authorizedPlayers.Any((PlayerNameID x) => x.userid == (turret as BaseEntity).OwnerID)) 
				return null;
			if (clansEnabled && (clanTurretShareOverride || playerPrefs.PlayerInfo[(turret as BaseEntity).OwnerID].CTS) && SameClan((turret as BaseEntity).OwnerID, player.userID)) 
				if (usePermGetClanShares && permission.UserHasPermission(player.UserIDString, permGetClanShares) || !usePermGetClanShares || clanTurretShareOverride) 
					return false; 
			if (friendsEnabled && playerPrefs.PlayerInfo[(turret as BaseEntity).OwnerID].TS && HasFriend((turret as BaseEntity).OwnerID, player.userID)) 
				if (usePermGetFriendShares && permission.UserHasPermission(player.UserIDString, permGetFriendShares) || !usePermGetFriendShares) 
					return false; 
			return null;
		}  
		void OnEntityBuilt(Planner planner, GameObject obj) 
		{
			if (pluginDisabled || !enableAutoAuth || planner == null || planner.GetOwnerPlayer() == null || obj.GetComponent<BaseEntity>() == null || obj.GetComponent<BaseEntity>().OwnerID == 0) 
				return; 
				
			BaseEntity entity = obj.GetComponent<BaseEntity>(); 
			BasePlayer player = planner.GetOwnerPlayer(); 
			
			if (!playerPrefs.PlayerInfo[player.userID].AA) 
				return; 
			if (usePermAutoAuth && !permission.UserHasPermission(player.UserIDString, permAutoAuth)) 
				return; 
			if (entity is BuildingPrivlidge) 
			{
				(entity as BuildingPrivlidge).authorizedPlayers.Add(new PlayerNameID{ userid = player.userID, username = player.displayName }); 
				obj.AddComponent<CupboardAutoAuth>().justPlaced = true; 
				entity.SendNetworkUpdateImmediate(true); 
				
				if (notifyAuthCupboard) 
					PrintToChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + string.Format(lang.GetMessage("CupAuth", this, player.UserIDString))); 
			} 
			else if (entity is AutoTurret) 
			{
				(entity as AutoTurret).authorizedPlayers.Add(new PlayerNameID{ userid = player.userID,	username = player.displayName }); 
				entity.SendNetworkUpdateImmediate(true); 
				
				if (notifyAuthTurret) 
					PrintToChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + string.Format(lang.GetMessage("TurretAuth", this, player.UserIDString))); 
			} 
		}  
		
		object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player) 
		{
			if (pluginDisabled || !enableCupSharing || privilege == null || player == null || privilege.OwnerID == player.userID) 
				return null; 
			if (!playerPrefs.PlayerInfo.ContainsKey(privilege.OwnerID)) 
				AddPlayerData(privilege.OwnerID); 
			if (player.IsAdmin && (bool)adminAccessEnabled[player.UserIDString]) 
				return null; 
			if(!privilege.authorizedPlayers.Any((PlayerNameID x) => x.userid == privilege.OwnerID)) 
				return null; 
			if (clansEnabled && SameClan(privilege.OwnerID, player.userID)) 
			{
				if (blockCupAuthClanMembers) 
				{
					if (playerPrefs.PlayerInfo[privilege.OwnerID].CCS) 
					{
						player.SetPlayerFlag(BasePlayer.PlayerFlags.HasBuildingPrivilege, true); 
						PrintToChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + string.Format(lang.GetMessage("Авторизация запрещена. Этот шкаф уже доступен вам", this, player.UserIDString))); 
						return true; 
					} 
					else 
					{
						var member = rust.FindPlayerById(privilege.OwnerID); 
						
						if (member != null) 
							PrintToChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + string.Format(lang.GetMessage("Авторизация запрещена. Участник клана '<color=#ffd479>{0}</color>' запретил доступ к шкафам", this, player.UserIDString), member.displayName)); 
						return true; 
					} 
				} 
				else 
				{
					return null; 
				} 
			} 
			if (friendsEnabled && HasFriend(privilege.OwnerID, player.userID)) 
			{
				if (blockCupAuthFriends) 
				{
					if (playerPrefs.PlayerInfo[privilege.OwnerID].CS) 
						player.SetPlayerFlag(BasePlayer.PlayerFlags.HasBuildingPrivilege, true); 
						
					PrintToChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + string.Format(lang.GetMessage("Авторизация запрещена. Этот шкаф уже доступен вам", this, player.UserIDString))); 
					return true; 
				} 
			} 
			return null; 
		}  
		object OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player) 
		{
			if (pluginDisabled || !enableCupSharing || privilege == null || player == null || privilege.OwnerID == player.userID) 
				return null; 
			if (!playerPrefs.PlayerInfo.ContainsKey(privilege.OwnerID)) 
				AddPlayerData(privilege.OwnerID); 
			if (player.IsAdmin && (bool)adminAccessEnabled[player.UserIDString]) 
				return null; 
			if(!privilege.authorizedPlayers.Any((PlayerNameID x) => x.userid == privilege.OwnerID)) 
				return null; 
			if (clansEnabled && playerPrefs.PlayerInfo[privilege.OwnerID].CCS && SameClan(privilege.OwnerID, player.userID)) 
			{
				privilege.authorizedPlayers.RemoveAll((PlayerNameID x) => x.userid == player.userID); 
				privilege.SendNetworkUpdate(); 
				return true; 
			} 
			if (friendsEnabled && playerPrefs.PlayerInfo[privilege.OwnerID].CS && HasFriend(privilege.OwnerID, player.userID)) 
			{
				privilege.authorizedPlayers.RemoveAll((PlayerNameID x) => x.userid == player.userID); 
				privilege.SendNetworkUpdate(); 
				return true; 
			} 
			return null; 
		}  
		object OnCupboardClearList(BuildingPrivlidge privilege, BasePlayer player) 
		{
			if (pluginDisabled || !enableCupSharing || privilege == null || player == null || privilege.OwnerID == player.userID) 
				return null; 
			if (!playerPrefs.PlayerInfo.ContainsKey(privilege.OwnerID)) 
				AddPlayerData(privilege.OwnerID); 
			if (player.IsAdmin && (bool)adminAccessEnabled[player.UserIDString]) 
				return null;
			if(!privilege.authorizedPlayers.Any((PlayerNameID x) => x.userid == privilege.OwnerID)) 
				return null; 
			if (clansEnabled && blockCupClearClanMembers && SameClan(privilege.OwnerID, player.userID)) 
			{
				PrintToChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + string.Format(lang.GetMessage("Очистка списка игроков с доступом запрещена", this, player.UserIDString))); 
				return true; 
			} 
			if (friendsEnabled && blockCupClearFriends && HasFriend(privilege.OwnerID, player.userID))
			{
				PrintToChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + string.Format(lang.GetMessage("Очистка списка игроков с доступом запрещена", this, player.UserIDString))); 
				return true; 
			} 
			return null; 
		}  
		object CanBuild(Planner plan, Construction prefab) 
		{
			if (pluginDisabled || !enableCupSharing || plan == null || plan.GetOwnerPlayer() == null || prefab == null) 
				return null; 
				
			var player = plan.GetOwnerPlayer(); 
			
			Ray targetRay = player.eyes.BodyRay(); 
			RaycastHit raycastHit; 
			Vector3 targetPosition; 
			
			if (GamePhysics.Trace(targetRay, 0f, out raycastHit, prefab.maxplaceDistance, 27328768, QueryTriggerInteraction.Ignore)) 
				targetPosition = targetRay.origin + targetRay.direction * raycastHit.distance; 
			else 
				targetPosition = targetRay.origin + targetRay.direction * prefab.maxplaceDistance; BuildingPrivlidge cup = BuildingPrivlidge.Get(targetPosition, new Quaternion(), prefab.bounds); 
			if (cup == null) 
				return null;
			if (player.IsAdmin && (bool)adminAccessEnabled[player.UserIDString]) 
			{
				prefab.canBypassBuildingPermission = true; 
				return null; 
			} 
			if(!cup.AnyAuthed() || cup.authorizedPlayers.Any((PlayerNameID x) => x.userid == player.userID) || !cup.authorizedPlayers.Any((PlayerNameID x) => x.userid == cup.OwnerID) || !playerPrefs.PlayerInfo.ContainsKey(cup.OwnerID)) 
				return null; 
			if ((!usePermGetClanShares || usePermGetClanShares && permission.UserHasPermission(player.UserIDString, permGetClanShares))) 
			{
				if (clansEnabled && playerPrefs.PlayerInfo[cup.OwnerID].CCS && SameClan(cup.OwnerID, player.userID)) 
				{
					prefab.canBypassBuildingPermission = true; 
					return null; 
				} 
				if (friendsEnabled && playerPrefs.PlayerInfo[cup.OwnerID].CS && HasFriend(cup.OwnerID, plan.GetOwnerPlayer().userID)) 
				{
					prefab.canBypassBuildingPermission = true; 
					return null; 
				} 
			} 
			if (blockBuildIntoBlocked) 
			{
				PrintToChat(plan.GetOwnerPlayer(), string.Format(prefixFormat,prefixColor, pluginPrefix) + string.Format(lang.GetMessage("Вы не можете строить в запрещенной зоне!", this, plan.GetOwnerPlayer().UserIDString))); 
				return false; 
			} 
			return null; 
		}  
		
		uint FirstPrivID(BasePlayer player) 
		{
			var privs = (List<BuildingPrivlidge>)_buildingPrivilege.GetValue(player); 
			
			if (privs == null || privs.Count == 0) 
				return 0; 
			return privs.OrderBy(c => c.net.ID).ToList().First().net.ID; 
		}  
		
		BuildingPrivlidge FirstPriv(BasePlayer player) 
		{
			BuildingPrivlidge firstPriv = null; 
			List<BuildingPrivlidge> buildingPrivilege = (List<BuildingPrivlidge>)_buildingPrivilege.GetValue(player); 
			
			if (buildingPrivilege == null || buildingPrivilege.Count == 0) 
				return firstPriv; 
				
			buildingPrivilege.RemoveAll(p => p == null); 
				
			if (buildingPrivilege.Count == 0) 
				return firstPriv; 
			return buildingPrivilege.OrderBy(c => c.net.ID).ToList().First(); 
		}  
		
		private class CupboardAutoAuth : MonoBehaviour 
		{
			public bool justPlaced = false;
		}  
		
		void OnEntityEnter(TriggerBase trigger, BaseEntity entity) 
		{
			if (pluginDisabled || !enableCupSharing || entity == null || !(trigger is BuildPrivilegeTrigger) || !(entity is BasePlayer)) 
				return; 
				
			var enterCup = (trigger as BuildPrivilegeTrigger).privlidgeEntity; 
			
			if (!playerPrefs.PlayerInfo.ContainsKey(enterCup.OwnerID)) 
				AddPlayerData(enterCup.OwnerID); 
				
			var obj = enterCup.gameObject.GetComponent<CupboardAutoAuth>(); 
			var player = entity as BasePlayer; 
			
			if (obj != null && obj is CupboardAutoAuth) 
			{
				GameObject.Destroy(obj); 
				
				if (enterCup.OwnerID == player.userID) 
					return; 
				if (enterCup.OwnerID != player.userID && !player.CanBuild()) 
					return; 
			} 
			
			NextTick(() => CheckCupAccess(player)); 
		}  
		void OnEntityLeave(TriggerBase trigger, BaseEntity entity) 
		{
			if (pluginDisabled || !enableCupSharing || entity == null || !(trigger is BuildPrivilegeTrigger) || !(entity is BasePlayer)) 
				return; 
				
			NextTick(() => CheckCupAccess(entity as BasePlayer)); 
		}  
		void OnPlayerRespawned(BasePlayer player) 
		{
			if (pluginDisabled || !enableCupSharing || player == null || player.CanBuild()) 
				return; 
			if (player.HasPlayerFlag(BasePlayer.PlayerFlags.InBuildingPrivilege)) 
				CheckCupAccess(player); 
		}  
		void OnPlayerSleepEnded(BasePlayer player) 
		{
			if (player.IsAdmin && !adminAccessEnabled.ContainsKey(player.UserIDString)) 
				adminAccessEnabled[player.UserIDString] = false; 
			if (pluginDisabled || !enableCupSharing || player == null || player.CanBuild()) 
				return; 
			if (player.HasPlayerFlag(BasePlayer.PlayerFlags.InBuildingPrivilege)) 
				CheckCupAccess(player); 
		}  
		void CheckCupAccess(BasePlayer player) 
		{
			if (player.CanBuild()) 
				return; 
				
			var cup = FirstPriv(player); 
			
			if (cup == null || cup.OwnerID == 0) 
				return; 
			if (!playerPrefs.PlayerInfo.ContainsKey(cup.OwnerID)) 
				AddPlayerData(cup.OwnerID); 
			if (player.IsAdmin && !adminAccessEnabled.ContainsKey(player.UserIDString)) 
				adminAccessEnabled[player.UserIDString] = false; 
			if (player.IsAdmin && (bool)adminAccessEnabled[player.UserIDString]) 
			{
				player.SetPlayerFlag(BasePlayer.PlayerFlags.HasBuildingPrivilege, true); 
				return; 
			} 
			if(!cup.AnyAuthed() || cup.authorizedPlayers.Any((PlayerNameID x) => x.userid == player.userID) || !cup.authorizedPlayers.Any((PlayerNameID x) => x.userid == cup.OwnerID) ) 
				return; 
			if (clansEnabled && playerPrefs.PlayerInfo[cup.OwnerID].CCS && SameClan(cup.OwnerID, player.userID) && (usePermGetClanShares && permission.UserHasPermission(player.UserIDString, permGetClanShares) || !usePermGetClanShares)) 
			{
				player.SetPlayerFlag(BasePlayer.PlayerFlags.HasBuildingPrivilege, true); 
				return; 
			} 
			if (friendsEnabled && playerPrefs.PlayerInfo[cup.OwnerID].CS && HasFriend(cup.OwnerID, player.userID) && (usePermGetFriendShares && permission.UserHasPermission(player.UserIDString, permGetFriendShares) || !usePermGetFriendShares)) 
			{
				player.SetPlayerFlag(BasePlayer.PlayerFlags.HasBuildingPrivilege, true); 
				return; 
			} 
		}  
		object CanUseLockedEntity(BasePlayer player, BaseLock code) 
		{
			if (!code.IsLocked() || (code is CodeLock && (_whitelistPlayers.GetValue(code) as List<ulong>).Contains(player.userID)) || (code is KeyLock && code.HasLockPermission(player))) 
				return true; 
			if (pluginDisabled || player == null || code == null || code is KeyLock) 
				return null; 
			if (!playerPrefs.PlayerInfo.ContainsKey(code.GetParentEntity().OwnerID)) 
				AddPlayerData(code.GetParentEntity().OwnerID); 
				
			ulong owner = code.GetParentEntity().OwnerID; 
			
			if (player.IsAdmin && (bool)adminAccessEnabled[player.UserIDString]) 
				return true; 
				
			bool hasClanShare = usePermGetClanShares && permission.UserHasPermission(player.UserIDString, permGetClanShares) || !usePermGetClanShares; 
			bool hasFriendShare = usePermGetFriendShares && permission.UserHasPermission(player.UserIDString, permGetFriendShares) || !usePermGetFriendShares; 
			
			if (clansEnabled && code.GetParentEntity() is Door && enableDoorSharing && playerPrefs.PlayerInfo[owner].CDS && SameClan(owner, player.userID) && hasClanShare) 
				return true;  
			else if (clansEnabled && ((code.GetParentEntity() is BoxStorage && enableBoxSharing && playerPrefs.PlayerInfo[owner].CBS) || (code.GetParentEntity() is Locker && enableLockerSharing && playerPrefs.PlayerInfo[owner].CBS))  && SameClan(owner, player.userID) && hasClanShare) 
				return true;  
			else if (friendsEnabled && code.GetParentEntity() is Door && enableDoorSharing && playerPrefs.PlayerInfo[owner].DS && HasFriend(owner, player.userID) &&hasFriendShare) 
				return true;  
			else if (friendsEnabled && ((code.GetParentEntity() is BoxStorage && enableBoxSharing && playerPrefs.PlayerInfo[owner].BS) || (code.GetParentEntity() is Locker && enableLockerSharing && playerPrefs.PlayerInfo[owner].LS)) && HasFriend(owner, player.userID) && hasFriendShare) 
				return true;  
			else if (clansEnabled && code.GetParentEntity() is Door && enableDoorSharing && !playerPrefs.PlayerInfo[owner].CDS && SameClan(owner, player.userID) && hasClanShare) 
			{
				var member = rust.FindPlayerById(owner); 
				
				if (member != null) 
					PrintToChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + string.Format(lang.GetMessage("Участник клана '{0}' запретил доступ к дверям", this, player.UserIDString), member.displayName)); 
				return false; 
			} 
			else if (clansEnabled && code.GetParentEntity() is BoxStorage && enableBoxSharing && !playerPrefs.PlayerInfo[owner].CBS && SameClan(owner, player.userID) && hasClanShare) 
			{
				var member = rust.FindPlayerById(owner); 
				
				if (member != null) 
					PrintToChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + string.Format(lang.GetMessage("Участник клана '{0}' запретил доступ к ящикам", this, player.UserIDString), member.displayName)); 
				return false; 
			} 
			else if (clansEnabled && code.GetParentEntity() is Locker && enableLockerSharing && !playerPrefs.PlayerInfo[owner].CLS && SameClan(owner, player.userID) && hasClanShare) 
			{
				var member = rust.FindPlayerById(owner); 
				
				if (member != null) 
					PrintToChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + string.Format(lang.GetMessage("Участник клана '{0}' запретил доступ к замкам", this, player.UserIDString), member.displayName)); 
				return false; 
			} 
			return null; 
		}  
		[ConsoleCommand("dynashare.resetdata")] 
		void dataReset(ConsoleSystem.Arg arg) 
		{
			if(arg.Connection != null && arg.Connection.authLevel < 2) 
				return; 
				
			Puts($"Resetting {playerPrefs.PlayerInfo.Count} entries in userdata"); 
			
			foreach (var info in playerPrefs.PlayerInfo) 
			{
				info.Value.CS = CupShare; 
				info.Value.DS = DoorShare; 
				info.Value.TS = TurretShare; 
				info.Value.BS = BoxShare; 
				info.Value.LS = LockerShare; 
				info.Value.AA = AutoAuth; 
				info.Value.CCS = ClanCupShare; 
				info.Value.CDS = ClanDoorShare; 
				info.Value.CBS = ClanBoxShare; 
				info.Value.CLS = ClanLockerShare; 
				info.Value.CTS = ClanTurretShare; 
			} 
			
			Puts("Saving userdata"); SaveData(); 
		}  
		void cShareCommand(ConsoleSystem.Arg arg) 
		{
			if(arg != null && arg.Connection != null && arg.Connection.player != null) 
			{
				usedConsoleInput.Add(arg.Connection.userid); 
				
				if (arg.Args != null) 
					ShareCommand((BasePlayer)arg.Connection.player, shareCommand, arg.Args); 
				else 
					ShareCommand((BasePlayer)arg.Connection.player, shareCommand, new string[] {}); 
			} 
		}  
		void CheckSB(BasePlayer player, StringBuilder sb) 
		{ 
			if (sb.ToString().Length > 1000) 
			{
				PrintChat(player, sb.ToString().TrimEnd()); 
				sb.Clear(); 
			} 
		} 
		void ShareCommand(BasePlayer player, string command, string[] args) 
		{
			var userID = player.userID; 
			var UserIDString = player.UserIDString; 
			
			if (pluginDisabled) 
			{
				PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + "Плагин отключен! Свяжитесь с администратором!"); 
				return; 
			} 
			
			bool hasClan = false; 
			
			if (clansEnabled && Clans?.Call("GetClanOf", player) != null) 
				hasClan = true; 
			if(args.Length == 0) 
			{
				var sb = new StringBuilder(); 
					sb.AppendLine($"<size=16><color={prefixColor}>{pluginPrefix}</color></size>"); 
					sb.AppendLine(lang.GetMessage("Команда: ", this, player.UserIDString) + $"<color=orange>/{shareCommand} <опции></color> или <color=orange>/{shareCommand} help | h</color>"); 
					
				if(usePermGetClanShares && clansEnabled || usePermGetFriendShares && friendsEnabled) 
				{
					bool hasClanShare = usePermGetClanShares && permission.UserHasPermission(UserIDString, permGetClanShares) && clansEnabled; 
					bool hasFriendShare = usePermGetFriendShares && permission.UserHasPermission(UserIDString, permGetFriendShares) && friendsEnabled; 
					string hasAccessTo; 
					
					if (hasClanShare && hasFriendShare) 
						hasAccessTo = $" <color={colorON}>Clan</color> | <color={colorON}>Friends</color>"; 
					else if (hasClanShare && !hasFriendShare) 
						hasAccessTo = $" <color={colorON}>Clan</color>"; 
					else if (!hasClanShare && hasFriendShare) 
						hasAccessTo = $" <color={colorON}>Friends</color>"; 
					else 
						hasAccessTo = $" <color={colorOFF}>None</color>"; 
						
					sb.AppendLine(lang.GetMessage("Вы можете получить доступ к:", this, player.UserIDString) + hasAccessTo); 
				} 
				else 
					sb.AppendLine(lang.GetMessage("Все переключатели переключают свои настройки (вкл<>выкл)", this, player.UserIDString)); 
				if (friendsEnabled) 
				{
					if (enableCupSharing) 
					{
						sb.AppendLine($"<color=#ffd479>cup | c</color> - "+ lang.GetMessage("Шкафы друзей:", this, player.UserIDString) +" "+ (playerPrefs.PlayerInfo[userID].CS ? $"<color=green>ВКЛ.</color>" : $"<color=red>ВЫКЛ.</color>")); 
						CheckSB(player, sb); 
					} 
					if (enableDoorSharing) 
					{
						sb.AppendLine($"<color=#ffd479>door | d</color> - "+ lang.GetMessage("Двери друзей:", this, player.UserIDString) +" "+ (playerPrefs.PlayerInfo[userID].DS ? $"<color=green>ВКЛ.</color>" : $"<color=red>ВЫКЛ.</color>")); 
						CheckSB(player, sb); 
					} 
					if (enableBoxSharing) 
					{
						sb.AppendLine($"<color=#ffd479>box | b</color> - "+ lang.GetMessage("Ящики друзей:", this, player.UserIDString) +" "+ (playerPrefs.PlayerInfo[userID].BS ? $"<color=green>ВКЛ.</color>" : $"<color=red>ВЫКЛ.</color>")); 
						CheckSB(player, sb); 
					}
					if (enableLockerSharing) 
					{
						sb.AppendLine($"<color=#ffd479>locker | l</color> - "+ lang.GetMessage("Замки друзей:", this, player.UserIDString) +" "+ (playerPrefs.PlayerInfo[userID].LS ? $"<color=green>ВКЛ.</color>" : $"<color=red>ВЫКЛ.</color>")); 
						CheckSB(player, sb); 
					} 
					if (enableTurretSharing) 
					{
						sb.AppendLine($"<color=#ffd479>turret | t</color> - "+ lang.GetMessage("Турели друзей:", this, player.UserIDString) +" "+ (playerPrefs.PlayerInfo[userID].TS ? $"<color=green>ВКЛ.</color>" : $"<color=red>ВЫКЛ.</color>")); 
						CheckSB(player, sb); 
					} 
				}  
				if ((usePermAutoAuth && permission.UserHasPermission(UserIDString, permAutoAuth)) || !usePermAutoAuth) 
				{
					sb.AppendLine($"-------------------------------------\n<color=#ffd479>autoauth | a</color> - "+ lang.GetMessage("Авторизация шкафа / турели", this, player.UserIDString)+" "+ (playerPrefs.PlayerInfo[userID].AA ? $"<color=green>ВКЛ.</color>" : $"<color=red>ВЫКЛ.</color>")); 
					CheckSB(player, sb); 
				} 
				if (hasClan) 
				{
					if (enableCupSharing) 
					{ sb.AppendLine($"-------------------------------------\n<color=#ffd479>clancup | cc</color> - "+ lang.GetMessage("Шкафы клана", this, player.UserIDString)+" "+ (playerPrefs.PlayerInfo[userID].CCS ? $"<color=green>ВКЛ.</color>" : $"<color=red>ВЫКЛ.</color>"));  
					} 
					if (enableDoorSharing) 
					{
						sb.AppendLine($"<color=#ffd479>clandoor | cd</color> - "+ lang.GetMessage("Двери клана", this, player.UserIDString)+" "+ (playerPrefs.PlayerInfo[userID].CDS ? $"<color=green>ВКЛ.</color>" : $"<color=red>ВЫКЛ.</color>")); 
						CheckSB(player, sb); 
					} 
					if (enableBoxSharing) 
					{
						sb.AppendLine($"<color=#ffd479>clanbox | cb</color> - "+ lang.GetMessage("Ящики клана", this, player.UserIDString)+" "+ (playerPrefs.PlayerInfo[userID].CBS ? $"<color=green>ВКЛ.</color>" : $"<color=red>ВЫКЛ.</color>")); 
						CheckSB(player, sb); 
					} 
					if (enableLockerSharing) 
					{
						sb.AppendLine($"<color=#ffd479>clanlocker | cl</color> - "+ lang.GetMessage("Замки клана", this, player.UserIDString)+" "+ (playerPrefs.PlayerInfo[userID].CLS ? $"<color=green>ВКЛ.</color>" : $"<color=red>ВЫКЛ.</color>")); 
						CheckSB(player, sb); 
					} 
					if (clanTurretShareOverride) 
					{
						sb.AppendLine(lang.GetMessage("Нацеливание турелей на участников клана:", this, player.UserIDString)+" "+ (clanTurretShareOverride ? $"<color=green>ВКЛ.</color>" : $"<color=red>ВЫКЛ.</color>")); 
						CheckSB(player, sb); 
					} 
					else 
					{
						sb.AppendLine($"<color=#ffd479>clanturret | ct</color> - "+ lang.GetMessage("Захват турелями участников клана:", this, player.UserIDString)+" "+ (playerPrefs.PlayerInfo[userID].CTS ? $"<color=green>ВКЛ.</color>" : $"<color=red>ВЫКЛ.")); 
						CheckSB(player, sb); 
					} 
				}
				
				if (player.IsAdmin) 
				{
					sb.Append(lang.GetMessage("Доступ администратора:", this, player.UserIDString)+$" (<color=orange>admin | adm</color>): "); 
					sb.Append((bool)adminAccessEnabled[player.UserIDString] ? $"<color=green>ВКЛ.</color>" : $"<color=red>ВЫКЛ.</color>"); 
				} 
				
				PrintChat(player, sb.ToString().TrimEnd()); 
				return; 
			} 
			
			switch (args[0]) 
			{
				case "cup": 
				case "c": 
					if (!friendsEnabled || !enableCupSharing) 
						goto case "disabled"; 
					if (!toggleCupShare) 
						goto case "blocked"; 
						
					playerPrefs.PlayerInfo[userID].CS = !playerPrefs.PlayerInfo[userID].CS; 
					PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + (playerPrefs.PlayerInfo[userID].CS ? lang.GetMessage("Доступ к шкафу для друзей <color=green>включен</color>", this, player.UserIDString) : lang.GetMessage("Доступ к шкафу для друзей <color=red>выключен</color>", this, player.UserIDString))); 
				break; 
				case "door": 
				case "d": 
					if (!friendsEnabled) 
						goto case "disabled"; 
					if (!toggleDoorShare) 
						goto case "blocked"; 
						
					playerPrefs.PlayerInfo[userID].DS = !playerPrefs.PlayerInfo[userID].DS; 
					PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + (playerPrefs.PlayerInfo[userID].DS ? lang.GetMessage("Доступ к дверям для друзей <color=green>включен</color>", this, player.UserIDString) : lang.GetMessage("Доступ к дверям для друзей <color=red>выключен</color>", this, player.UserIDString))); 
				break; 
				case "box": 
				case "b": 
					if (!friendsEnabled) 
						goto case "disabled"; 
					if (!toggleBoxShare) 
						goto case "blocked"; 
						
					playerPrefs.PlayerInfo[userID].BS = !playerPrefs.PlayerInfo[userID].BS; 
					PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + (playerPrefs.PlayerInfo[userID].BS ? lang.GetMessage("Доступ к ящикам для друзей <color=green>включен</color>", this, player.UserIDString) : lang.GetMessage("Доступ к ящикам для друзей <color=red>выключен</color>", this, player.UserIDString))); 
				break; 
				case "locker": 
				case "l": 
					if (!friendsEnabled) 
						goto case "disabled"; 
					if (!toggleLockerShare) 
						goto case "blocked"; 
					
					playerPrefs.PlayerInfo[userID].LS = !playerPrefs.PlayerInfo[userID].LS;
					PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + (playerPrefs.PlayerInfo[userID].LS ? lang.GetMessage("Доступ к замкам для друзей <color=green>включен</color>", this, player.UserIDString) : lang.GetMessage("Доступ к замкам для друзей <color=red>выключен</color>", this, player.UserIDString))); 
				break; 
				case "turret": 
				case "t": 
					if (!friendsEnabled) 
						goto case "disabled"; 
					if (!toggleTurretShare) 
						goto case "blocked"; 
						
					playerPrefs.PlayerInfo[userID].TS = !playerPrefs.PlayerInfo[userID].TS; 
					PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + (playerPrefs.PlayerInfo[userID].TS ? lang.GetMessage("Доступ к турелям для друзей <color=green>включен</color>", this, player.UserIDString) : lang.GetMessage("Доступ к турелям для друзей <color=red>выключен</color>", this, player.UserIDString))); 
				break; 
				case "autoauth": 
				case "a": 
					if (usePermAutoAuth && !permission.UserHasPermission(UserIDString, permAutoAuth)) 
						goto case "disabled"; 
					if (!toggleAutoAuth) 
						goto case "blocked"; 
						
					playerPrefs.PlayerInfo[userID].AA = !playerPrefs.PlayerInfo[userID].AA; 
					PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + (playerPrefs.PlayerInfo[userID].AA ? lang.GetMessage("Автоматическая авторизация в шкафу <color=green>включена</color>", this, player.UserIDString) : lang.GetMessage("Автоматическая авторизация в шкафу <color=red>выключена</color>", this, player.UserIDString))); 
				break; 
				case "clancup": 
				case "cc": 
					if (!hasClan) 
						goto case "disabled"; 
					if (!toggleClanCupShare) 
						goto case "blocked"; 
						
					playerPrefs.PlayerInfo[userID].CCS = !playerPrefs.PlayerInfo[userID].CCS; 
					PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + (playerPrefs.PlayerInfo[userID].CCS ? lang.GetMessage("Доступ к шкафу для клана <color=green>включен</color>", this, player.UserIDString) : lang.GetMessage("Доступ к шкафу для клана <color=red>выключен</color>", this, player.UserIDString))); 
				break; 
				case "clandoor": 
				case "cd": 
					if (!hasClan) 
						goto case "disabled"; 
					if (!toggleClanDoorShare) 
						goto case "blocked"; 
					
					playerPrefs.PlayerInfo[userID].CDS = !playerPrefs.PlayerInfo[userID].CDS; 
					PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + (playerPrefs.PlayerInfo[userID].CDS ? lang.GetMessage("Доступ к дверям для клана <color=green>включен</color>", this, player.UserIDString) : lang.GetMessage("Доступ к дверям для клана <color=red>выключен</color>", this, player.UserIDString))); 
				break; 
				case "clanbox": 
				case "cb": 
					if (!hasClan) 
						goto case "disabled"; 
					if (!toggleClanBoxShare) 
						goto case "blocked"; 
					
					playerPrefs.PlayerInfo[userID].CBS = !playerPrefs.PlayerInfo[userID].CBS; 
					PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + (playerPrefs.PlayerInfo[userID].CBS ? lang.GetMessage("Доступ к ящикам для клана <color=green>включен</color>", this, player.UserIDString) : lang.GetMessage("Доступ к ящикам для клана <color=red>выключен</color>", this, player.UserIDString))); 
				break; 
				case "clanlocker": 
				case "cl": 
					if (!hasClan) 
						goto case "disabled"; 
					if (!toggleClanLockerShare) 
						goto case "blocked"; 
						
					playerPrefs.PlayerInfo[userID].CLS = !playerPrefs.PlayerInfo[userID].CLS; 
					PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + (playerPrefs.PlayerInfo[userID].CLS ? lang.GetMessage("Доступ к замкам для клана <color=green>включен</color>", this, player.UserIDString) : lang.GetMessage("Доступ к замкам для клана <color=red>выключен</color>", this, player.UserIDString))); 
				break; 
				case "clanturret": 
				case "ct": 
					if (!hasClan || clanTurretShareOverride) 
						goto case "disabled"; 
					if (!toggleClanTurretShare) 
						goto case "blocked"; 
						
					playerPrefs.PlayerInfo[userID].CTS = !playerPrefs.PlayerInfo[userID].CTS; 
					PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + (playerPrefs.PlayerInfo[userID].CTS ? lang.GetMessage("Доступ к турелям для клана <color=green>включен</color>", this, player.UserIDString) : lang.GetMessage("Доступ к турелям для клана <color=red>выключен</color>", this, player.UserIDString))); 
				break; 
				case "disabled": 
					PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + string.Format(lang.GetMessage("На данный момент функция '<color=#ffd479>{0}</color>' не активна", this, player.UserIDString), args[0])); 
				break; 
				case "blocked": 
					PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + string.Format(lang.GetMessage("Администратор заблокировал переключатель '<color=#ffd479>{0}</color>'", this, player.UserIDString), args[0])); 
				break; 
				case "admin": 
				case "adm": 
					if (!player.IsAdmin) 
						goto default; 
						
					adminAccessEnabled[player.UserIDString] = !adminAccessEnabled[player.UserIDString]; 
					
					if (adminAccessEnabled[player.UserIDString] && !player.CanBuild()) 
						player.SetPlayerFlag(BasePlayer.PlayerFlags.HasBuildingPrivilege, true); 
						
					PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + ((bool)adminAccessEnabled[player.UserIDString] ? lang.GetMessage("Доступ администратора <color=green>включен</color>", this, player.UserIDString) : lang.GetMessage("Доступ администратора <color=red>выключен</color>", this, player.UserIDString))); 
				break; 
				case "help": 
				case "h": 
					if (args.Length != 2) 
					{
						var sbhelp = new StringBuilder(); 
							sbhelp.AppendLine($"<size=16><color={prefixColor}>{pluginPrefix}</color></size>"); 
							sbhelp.AppendLine(lang.GetMessage("Команда: ", this, player.UserIDString) + $"<color=orange>/{shareCommand} help <опции></color>"); 
						
						if (enableCupSharing) 
							sbhelp.AppendLine($"<color=#ffd479>cups</color> - "+ lang.GetMessage("Описание для обмена шкафами", this, player.UserIDString)); 
							
							sbhelp.AppendLine($"<color=#ffd479>doors</color> - "+ lang.GetMessage("Описание для обмена дверями", this, player.UserIDString)); 
							sbhelp.AppendLine($"<color=#ffd479>boxes</color> - "+ lang.GetMessage("Описание для обмена ящиками", this, player.UserIDString)); 
							sbhelp.AppendLine($"<color=#ffd479>lockers</color> - "+ lang.GetMessage("Описание для обмена замками", this, player.UserIDString)); 
							sbhelp.AppendLine($"<color=#ffd479>turrets</color> - "+ lang.GetMessage("Описание для обмена турелями", this, player.UserIDString)); 
							sbhelp.AppendLine($"<color=#ffd479>autoauth</color> - "+ lang.GetMessage("Описание для автоматической авторизации", this, player.UserIDString)); 
						
						PrintChat(player, sbhelp.ToString().TrimEnd()); 
					} 
					else if (args.Length >= 2) 
					{
						switch (args[1]) 
						{
							case "cups": 
								PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + lang.GetMessage("Предоставляет возможность обмена шкафами среди друзей / участников клана, эти игроки получают права на строительство в зоне действия ваших шкафов, которые вы ставили, и где вы сами авторизированы. Он не разделяет, если вы не авторизованы", this, player.UserIDString)); 
							break; 
							case "doors": 
								PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + lang.GetMessage("Предоставляет возможность обмена дверьми среди друзей / участников клана, эти игроки могут открыть все ваши запертые двери без доступа к самому кодовому замку.", this, player.UserIDString)); 
							break; 
							case "boxes": 
								PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + lang.GetMessage("Предоставляет возможность обмена ящиками среди друзей / участников клана, эти игроки могут открыть все ваши заблокированные ящики, не имея доступа к самому кодовому замку.", this, player.UserIDString)); 
							break; 
							case "lockers": 
								PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + lang.GetMessage("Предоставляет возможность обмена замками среди друзей / участников клана, эти игроки могут открыть все ваши замки без доступа к самому кодовому замку.", this, player.UserIDString)); 
							break; 
							case "turrets": 
								PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + lang.GetMessage("Предоставляет возможность обмена турелями среди друзей / участников клана, эти игроки не будут убиты вашими турелями, не имея доступа к турели. Эта функция может быть переопределена на стороне сервера для того, чтобы у кланов функция была включена.", this, player.UserIDString)); 
							break; 
							case "autoauth": 
								PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + lang.GetMessage("Включает автоматическую авторизацию для шкафов и турелей, вы можете пропустить последующие авторизации.", this, player.UserIDString)); 
							break; 
							default: 
								PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + lang.GetMessage("Этого раздела помощи не существует.", this, player.UserIDString)); 
							break; 
						} 
					} 
				break; 
				default: 
					PrintChat(player, string.Format(prefixFormat,prefixColor, pluginPrefix) + string.Format(lang.GetMessage("Функция '<color=#ffd479>{0}</color>' недоступна", this, player.UserIDString), args[0])); 
				break; 
			} 
		}  
		void PrintChat(BasePlayer player, string message) 
		{
			if(usedConsoleInput.Contains(player.userID)) 
			{
				usedConsoleInput.Remove(player.userID); 
				player.ConsoleMessage(message); 
			} 
			else 
			{
				player.ChatMessage(message); 
			} 
		} 
	} 
}