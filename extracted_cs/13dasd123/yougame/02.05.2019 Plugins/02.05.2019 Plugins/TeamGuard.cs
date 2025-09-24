// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins 
{ 
    [Info("TeamGuard", "RustPlugin.ru -Vlad-00003", "1.3.2")]
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("Plugin allow admins to easier controll player groups size.")]
    	/*      
        * Author info:      
        * E-mail: Vlad-00003@mail.ru      
        * Vk: vk.com/vlad_00003      
        * v1.3.0      
        *   В исключения добавлен EventManager      
        *   Добавлена поддержка плагина ZoneManager. В файле конфигурации вы можете указать зоны, в которых не будет идти проверка.      *   Упавшие игроки не учитываются при проверке      *   Добавлена опция для отключения проверки по радиусу      *   Добавлена команда для очистки последнего объекта, в котором не удалось авторизоваться.      
        * v1.3.1      
        *   Исправлена NRE при использовании команды  
        * v1.3.2      
        *   Убраны приватные методы    
        */
    class TeamGuard: RustPlugin 
    {
        #region Vars
        private PluginConfig config;
		private static TeamGuard inst;
		private const string PanelName = "TeamGuardGUI";
		private static readonly Collider[] colBuffer = Vis.colBuffer;
		private const string EffectPrefab1 = "assets/prefabs/npc/autoturret/effects/targetacquired.prefab";
		private const string EffectPrefab2 = "assets/prefabs/misc/junkpile/effects/despawn.prefab";
		[PluginReference] private Plugin Duel, EventManager, ZoneManager;
 		private readonly Dictionary <BasePlayer, object> LastActivate = new Dictionary <BasePlayer, object>();
        #endregion

        #region Localization
        private string GetMsg(string langline, object id = null, params object[] args) 
        {
			var msg = lang.GetMessage(langline, this, id?.ToString());
			return args.Length > 0 ? string.Format(msg, args) : msg;
		}

		protected override void LoadDefaultMessages()
        {
			lang.RegisterMessages(new Dictionary <string, string>
            { 
                ["TextBeforeDmg"] = "You have more players around then allowed (Allowed - {0}).\nYou have {1} seconds before you will start to get damage.",
				["TextWhileDmg"] = "You have more players around then allowed (Allowed - {0}).\nAll players in the area would get {1} damage every {2} seconds until redundant player will leave the area.",
				["LogCode"] = "Player {0} attempted to auth in the code lock of player {1}. {2}",
				["ChatCode"] = "This codelock has it's auth limit. (Max - {0})\nUse [#59f442]{1}[/#] command to clear it's authorized list",
				["LogCup"] = "Player {0} attempted to auth in the tool cupboard of player {1}. {2}",
				["ChatCup"] = "This tool cupboard has it's auth limit. (Max - {0})\nUse [#59f442]{1}[/#] command to clear it's authorized list",
				["LogTurret"] = "Player {0} attempted to auth in the turret of player {1}. {2}",
				["ChatTurret"] = "This turret has it's auth limit. (Max - {0})\nUse [#59f442]{1}[/#] command to clear it's authorized list",
				["CantCommand"] = "You can not use this command right now!",
				["LogCupCleared"] = "Player \"{0}\" has cleared tool cupboard of the player \"{1}\"\nPlayer pos: {2} | Target pos: {3}",
				["CupCleared"] = "Tool cupboard authorized list has been cleared!",
				["LogTurretCleared"] = "Player \"{0}\" has cleared Autoturret of the player \"{1}\"\nPlayer pos: {2} | Target pos: {3}",
				["TurretCleared"] = "Autoturret authorized list has been cleared!",
				["LogLockCleared"] = "Player \"{0}\" has cleared code lock of the player \"{1}\"\nPlayer pos: {2} | Target pos: {3}",
				["LockCleared"] = "Code lock authorized list has been cleared!"
			}, this);
            lang.RegisterMessages(new Dictionary <string, string>
            {
                ["TextBeforeDmg"] = "В зоне вокруг вас находится больше игроков, чем разрешено (Разрешено {0}).\nЧерез {1}с. все игроки в зоне начнут получать урон.",
				["TextWhileDmg"] = "В зоне вокруг вас находится больше игроков, чем разрешено (Разрешено {0}).\nВсем игрокам в зоне будет наносится урон в размере {1} каждые {2}с. до тех пор, пока лишние игроки не покинут зону проверки.",
				["LogCode"] = "Игрок {0} попытался авторизоваться в замке игрока {1}. {2}",
				["ChatCode"] = "Достигнут лимит авторизации в замке. (Максимально - {0})\nИспользуйте  команду [#59f442]{1}[/#] для очистки списка авторизованных",
				["LogCup"] = "Игрок {0} попытался авторизоваться в шкаф игрока {1}. {2}",
				["ChatCup"] = "Достигнут лимит авторизованных в шкафу. (Максимально - {0})\nИспользуйте  команду [#59f442]{1}[/#] для очистки списка авторизованных",
				["LogTurret"] = "Игрок {0} попытался авторизоваться в турели игрока {1}. {2}",
				["ChatTurret"] = "Достигнут лимит авторизаций в турели. (Максимально - {0})\nИспользуйте  команду [#59f442]{1}[/#] для очистки списка авторизованных",
				["CantCommand"] = "Вы не можете использовать эту команду в данный момент!",
				["LogCupCleared"] = "Игрок \"{0}\" очистил шкаф игрока \"{1}\"\nПозиция игрока: {2} | Позиция цели: {3}",
				["CupCleared"] = "Список авторизованных в шкафу очищен!",
				["LogTurretCleared"] = "Игрок \"{0}\" очистил турель игрока \"{1}\"\nПозиция игрока: {2} | Позиция цели: {3}",
				["TurretCleared"] = "Список авторизованных в турели очищен!",
				["LogLockCleared"] = "Игрок \"{0}\" очистил замок игрока \"{1}\"\nПозиция игрока: {2} | Позиция цели: {3}",
				["LockCleared"] = "Список авторизованных в замке очищен!",
			}, this, "ru");
		}
        #endregion

        #region Config
        private class PluginConfig
        {
			public class GenericConfig
            {
                [JsonProperty("Использовать ли проверку по радиусу")]
                public bool UseZone = true;
				[JsonProperty("Команда очистки авторизованных для игроков")]
                public string Command = "/clear";
				[JsonProperty("Максимальный размер группы игроков")]
                public int MaxAllowedPlayers = 6;
				[JsonProperty("Радиус зоны проверки")]
                public float AroundRadius = 10f;
				[JsonProperty("Частота проверок")]
                public float CheckInterval = 5f;
				[JsonProperty("Разрешённое время нахождения рядом")]
                public float timeBeforeShock = 20f;
				[JsonProperty("Наносимый урон за раз")]
                public float DamagePerTime = 5f;
				[JsonProperty("Привилегия для просмотра сообщений в чате")]
                public string perm = "teamguard.log";
				[JsonProperty("Формат сообщений в чате")]
                public string ChatFormat = "[#f46600][TeamGuard][/#] {0}";
				[JsonProperty("Выводить ли сообщения о нанесении урона в чат?")]
                public bool UseChat = false;
			}
			public class GUIConfig
            { 
                [JsonProperty("Использовать ли графическую панель?")]
                public bool UseGui = true;
				[JsonProperty("Минимальный отступ")]
                public string Amin = "0 0.355";
				[JsonProperty("Максимальный отступ")]
                public string Amax = "1 0.655";
				[JsonProperty("Цвет фона")]
                public string BackGroundColor = "0.30 0.01 0.01 0.80";
				[JsonProperty("Размер шрифта")]
                public int FontSize = 16;
			}
			public class AdminsConfig
            { 
                [JsonProperty("Игнорировать администраторов при проверке?")]
                public bool IgnoreAdmins = false;
			    [JsonProperty("Необходимый уровень AuthLevel для игнорирования")]
                public int AuthLevel = 2;
			}
            [JsonProperty("Общие Настройки")]
            public GenericConfig Generic;
			[JsonProperty("Настройки GUI")]
            public GUIConfig GUI;
			[JsonProperty("Проверка администраторов")]
            public AdminsConfig Admins;
			[JsonProperty("Список зон, в которых не нужно вести проверку")]
            public List <string> ZonesList;

			public static PluginConfig DefaultConfig()
            {
				return new PluginConfig()
                {
					Generic = new GenericConfig(),
					GUI = new GUIConfig(),
					Admins = new AdminsConfig(),
					ZonesList = new List <string> () { "zone1", "warzone", "safehouse" }
				};
			}
        }
        #endregion

        #region Config initialization
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating config file...");
            config = PluginConfig.DefaultConfig();
		}
		protected override void LoadConfig()
        {
			base.LoadConfig();
			config = Config.ReadObject <PluginConfig>();
			permission.RegisterPermission(config.Generic.perm, this);
			cmd.AddChatCommand(config.Generic.Command.Replace("/", string.Empty), this, CmdClear);
		}
		protected override void SaveConfig()
        {
			Config.WriteObject(config);
		}
        #endregion

        #region Initialization and quiting
        private void Init()
        {
			inst = this;
		}
		void OnPlayerInit(BasePlayer player) => CheckComponent(player);
		void OnServerInitialized()
        {
			foreach(var player in BasePlayer.activePlayerList)
                CheckComponent(player);
		}
		void Unload()
        {
			foreach(TGChecker checker in Resources.FindObjectsOfTypeAll <TGChecker>())
            {
				DestroyUI(checker.player);
				UnityEngine.Object.Destroy(checker);
			}
		}
        #endregion

        #region Checker
        private void CheckComponent(BasePlayer player)
        {
            if (!config.Generic.UseZone)
                return;
			TGChecker checker = player.GetComponent <TGChecker>();
			if (!checker)
                player.gameObject.AddComponent <TGChecker>();
			else
                checker.enabled = true;
		}
		private class TGChecker: MonoBehaviour
        {
			float seconds;
			public BasePlayer player;
			void Awake()
            {
				player = GetComponent < BasePlayer > ();
				if (IsNpc(player))
                    Destroy(this);
				InvokeRepeating("CheckSequence", inst.config.Generic.CheckInterval, inst.config.Generic.CheckInterval);
			}
			void CheckSequence()
            {
				if (!player.IsConnected)
                {
					Destroy(this);
					return;
				}
				if (player.IsDead() || player.IsSleeping() || inst.HasAuth(player) || player.IsWounded() || inst.IsDuelPlayer(player) || inst.InEvent(player) || inst.InZone(player))
                {
					seconds = Math.Max(0, seconds - inst.config.Generic.CheckInterval);
					DestroyUI(player);
					return;
				}
				int players = 0;
				int entities = Physics.OverlapSphereNonAlloc(player.transform.position, inst.config.Generic.AroundRadius, colBuffer, Rust.Layers.Server.Players);
				for (var i = 0; i < entities; i++)
                {
					var player = colBuffer[i].GetComponentInParent <BasePlayer>();
					if (player == null)
                        continue;
					bool dontcount = this.player == player || inst.InZone(player) || inst.IsDuelPlayer(player) || player.IsDead() || player.IsSleeping() || !IsVisible(player, this.player.eyes.position, player.eyes.position) || inst.HasAuth(player) || IsNpc(player) || inst.InEvent(player) || player.IsWounded();
					if (!dontcount)
                        players++;
				}
				if (players >= inst.config.Generic.MaxAllowedPlayers)
                {
					seconds += inst.config.Generic.CheckInterval;
					string text;
					if (seconds >= inst.config.Generic.timeBeforeShock)
                    {
						text = inst.GetMsg("TextWhileDmg", player.UserIDString, inst.config.Generic.MaxAllowedPlayers, inst.config.Generic.DamagePerTime, inst.config.Generic.CheckInterval);
						DoShock(player);
					}
                    else
                    {
						text = inst.GetMsg("TextBeforeDmg", player.UserIDString, inst.config.Generic.MaxAllowedPlayers, inst.config.Generic.timeBeforeShock - seconds);
					}
					if (inst.config.Generic.UseChat)
                        inst.Reply(player, text);
					if (inst.config.GUI.UseGui)
                        ShowGUI(player, inst.config.GUI.Amin, inst.config.GUI.Amax, inst.config.GUI.BackGroundColor, inst.config.GUI.FontSize, text);
				}
                else
                {
					seconds = Math.Max(0, seconds - inst.config.Generic.CheckInterval);
					DestroyUI(player);
				}
			}
		}
        #endregion

        #region GUI
        private static void DestroyUI(BasePlayer player)
        {
			CuiHelper.DestroyUi(player, PanelName);
		}
		private static void ShowGUI(BasePlayer player, string amin, string amax, string backgroundColor, int FontSize, string text)
        {
			CuiHelper.DestroyUi(player, PanelName);
			var elements = new CuiElementContainer();
			var panel = elements.Add(new CuiPanel
            {
				Image = { Color = backgroundColor },
				RectTransform = { AnchorMin = amin, AnchorMax = amax },
				CursorEnabled = false
			}, "Overlay", PanelName);
			elements.Add(new CuiLabel
            {
				Text = {
					Text = text,
					FontSize = FontSize,
					Font = "robotocondensed-regular.ttf",
					Align = TextAnchor.MiddleCenter
				},
				RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
			}, panel);
			CuiHelper.AddUi(player, elements);
		}
        #endregion

        #region Oxide Hooks
        object OnCodeEntered(CodeLock Lock, BasePlayer player, string code)
        {
			bool canOpen = code == Lock.guestCode || code == Lock.code;
			if (!canOpen)
                return null;
			var parrent = Lock.GetParentEntity();
			string owner = GetName(parrent.OwnerID);
			var whitelistPlayers = Lock.whitelistPlayers;
			var guestPlayers = Lock.guestPlayers;
			var count = guestPlayers.Count + whitelistPlayers.Count;
			if (count < config.Generic.MaxAllowedPlayers)
                return null;
			Log("LogCode", player.displayName, owner, parrent.ServerPosition);
			Reply(player, "ChatCode", config.Generic.MaxAllowedPlayers, config.Generic.Command);
			LastActivate[player] = Lock;
			return false;
		}
		object OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
			string owner = GetName(privilege.OwnerID);
			var authlist = privilege.authorizedPlayers;
			if (authlist.Count < config.Generic.MaxAllowedPlayers) return null;
			Log("LogCup", player.displayName, owner, privilege.ServerPosition);
			Reply(player, "ChatCup", config.Generic.MaxAllowedPlayers, config.Generic.Command);
			LastActivate[player] = privilege;
			return false;
		}
		object OnTurretAuthorize(AutoTurret turret, BasePlayer player)
        {
			string owner = GetName(turret.OwnerID);
			var authlist = turret.authorizedPlayers;
			if (authlist.Count < config.Generic.MaxAllowedPlayers) return null;
			Log("LogTurret", player.displayName, owner, turret.ServerPosition);
			Reply(player, "ChatTurret", config.Generic.MaxAllowedPlayers, config.Generic.Command);
			LastActivate[player] = turret;
			return false;
		}
        #endregion

        #region Command
        private void CmdClear(BasePlayer player, string command, string[] args)
        {
			if (!LastActivate.ContainsKey(player))
            {
				Reply(player, "CantCommand");
				return;
			}
			var obj = LastActivate[player];
			if (obj == null)
            {
				PrintError($"Запись об игроке \"{player.displayName}\" пуста. Сообщите об этом разработчику!");
				LastActivate.Remove(player);
				return;
			}
			var privilage = obj as BuildingPrivlidge;
			if (privilage)
            {
				privilage.authorizedPlayers.Clear();
				privilage.SendNetworkUpdate();
				Reply(player, "CupCleared");
				Log("LogCupCleared", player.displayName, GetName(privilage.OwnerID), player.transform.position, privilage.transform.position);
			}
			var @lock = obj as CodeLock;
			if (@lock)
            {
                @lock.guestPlayers.Clear();
                @lock.whitelistPlayers.Clear();
                @lock.SendNetworkUpdate();
				Reply(player, "LockCleared");
				var ent = @lock.GetParentEntity();
				Log("LogLockCleared", player.displayName, GetName(ent.OwnerID), player.transform.position, ent.transform.position);
			}
			var turret = obj as AutoTurret;
			if (turret)
            {
				turret.authorizedPlayers.Clear();
				turret.SendNetworkUpdate();
				Reply(player, "TurretCleared");
				Log("LogTurretCleared", player.displayName, GetName(turret.OwnerID), player.transform.position, turret.transform.position);
			}
			LastActivate.Remove(player);
		}
        #endregion

        #region Helpers
        private static bool IsNpc(BasePlayer player)
        {
            //BotSpawn
            if (player is NPCPlayer)
                return true;
            //HumanNPC
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L))
                return true;
			return false;
		}
		private bool HasAuth(BasePlayer player) => config.Admins.IgnoreAdmins && player?.net?.connection?.authLevel >= config.Admins.AuthLevel;
		private bool IsDuelPlayer(BasePlayer player)
        {
			var dueler = Duel?.Call("IsPlayerOnActiveDuel", player);
			if (dueler is bool)
                return (bool) dueler;
			return false;
		}
		private bool InZone(BasePlayer player) => ZoneManager != null && config.ZonesList.Any(zoneId =>(bool) ZoneManager.Call("isPlayerInZone", zoneId, player));
		private bool InEvent(BasePlayer player)
        {
			var check = EventManager ? .Call("isPlaying", player);
			if (check is bool)
                return (bool) check;
			return false;
		}
		private static bool IsVisible(BasePlayer player, Vector3 source, Vector3 dest) => player.IsVisible(source, dest);
		private static void DoShock(BasePlayer player)
        {
			player.Hurt(inst.config.Generic.DamagePerTime, DamageType.ElectricShock, player, false);
			EffectNetwork.Send(new Effect(EffectPrefab1, player, 0u, Vector3.zero, Vector3.forward), player.net.connection);
			EffectNetwork.Send(new Effect(EffectPrefab2, player, 0u, Vector3.zero, Vector3.forward), player.net.connection);
		}
		private void Reply(BasePlayer player, string langline, params object[] args)
        {
			var reply = 976;
			if (reply == 0) {}
			var text = GetMsg(langline, player.UserIDString, args);
			player.ChatMessage(covalence.FormatText(string.Format(config.Generic.ChatFormat, text)));
		}
		private string GetName(ulong id)
        {
			IPlayer player = covalence.Players.FindPlayerById(id.ToString());
			return player == null ? id.ToString() : player.Name;
		}
		private void Log(string langline, params object[] args)
        {
			LogToFile("Log", $"({DateTime.Now.ToShortTimeString()}) {GetMsg(langline, null, args)}", this);
			foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
				if (permission.UserHasPermission(player.UserIDString, config.Generic.perm))
					Reply(player, langline, args);
			}
		}
        #endregion
    }
}
