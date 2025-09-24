using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Text;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using Network;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("RustMap", "Fartus", "1.5.7")]
    [Description("Интерактивная карта для вашего сервера")]
	//Данный плагин - переработка плагина MapManager от автора bazuka5801
    class RustMap : RustPlugin
    {
        #region Classes

		[PluginReference] Plugin Clans;
        [PluginReference] Plugin Friends;
		[PluginReference] Plugin NoEscape;

        class MapMarker
        {
            public Transform transform;
            public int Rotation = -1;
            public Vector2 anchorPosition { get { return m_Instance.ToScreenCoords(position); } }
            public Vector2 position = Vector2.zero;
            public string png;
            public bool rotSupport = false;
            public string name;
            public string text;
            public float size;
            public float alpha;
            public int fontsize;
            public ulong counter = 0;
            public bool inMap = true;

            protected MapMarker(Transform transform)
            {
                this.transform = transform;
            }

            public virtual bool NeedRedraw()
            {
                if (transform == null) return false;
                var lastRot = Rotation;
                if (rotSupport) Rotation = GetRotation(transform.eulerAngles.y);
                var lastPos = position;
                position = transform.position;
                position.y = 0;
                if (rotSupport && Rotation != lastRot)
                {
                    position = lastPos;
                    return true;
                }
                if (Vector3.Distance(position, lastPos) > 0.5) return true;
                position = lastPos;
                return false;
            }

            public static MapMarker Create(Transform transform)
            {
                return new MapMarker(transform);
            }
        }

        class MapPlayer : MapMarker
        {
            public BasePlayer player;
            public List<MapPlayer> clanTeam = new List<MapPlayer>();

            protected MapPlayer(BasePlayer player) : base(player.transform)
            {
                this.player = player;
            }

            public override bool NeedRedraw()
            {
                var lastRot = Rotation;
                if (player == null || transform == null) return false;
                Rotation = GetRotation(player.eyes.rotation.eulerAngles.y);
                var lastPos = position;
                position = transform.position;
                position.y = 0;
                if (Rotation != lastRot)
                {
                    position = lastPos;
                    return true;
                }
                if (Vector3.Distance(position, lastPos) > 0.5)
                {
                    return true;                    
                }
                position = lastPos;
                return false;
            }

            public void OnCloseMap()
            {
                Rotation = -1;
                position = Vector2.zero;
                clanTeam?.ForEach(p => p.OnCloseMap());
            }

            public static MapPlayer Create(BasePlayer player)
            {
                return new MapPlayer(player) { alpha = m_Instance.playerIconAlpha, size = m_Instance.playerIconSize, fontsize = m_Instance.playerIconFontSize };
            }
        }

        #endregion

        #region Configuration

        float mapAlpha;
        float mapSize;
        bool clanSupport;
        bool friendsSupport;
		bool raidhomes;
		float raidhomeIconSize;
		bool deathIcon;
		float deathIconSize;

        float playerIconSize;
        float playerIconAlpha;
        int playerIconFontSize;
        bool playerCoordinates;

        bool monuments;
        bool caves;
        bool powersub;
        bool water;
        float monumentIconSize;
        float monumentIconAlpha;
        bool monumentIconNames;
        int monumentsFontSize;

        bool plane;
        float planeIconSize;
        float planeIconAlpha;
        bool planeDrop;
        float planeDropIconSize;
        float planeDropIconAlpha;

        bool heli;
        float heliIconSize;
        float heliIconAlpha;
        bool heliDrop;
        float heliDropIconSize;
        float heliDropIconAlpha;

		bool NewHeli;
		float NewheliIconSize;
		float NewheliIconAlpha;
		bool NewHeliCrate;
		float NewHeliCrateIconSize;
		float NewHeliCrateIconAlpha;

		bool ship;
		float shipIconSize;
		float shipIconAlpha;

		bool bradley;
		float bradleyIconSize;
		float bradleyIconAlpha;

        string textColor;
		string textColor1;
        float bannedSize;
        string mapUrl;
		private string version;

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Создание нового файла конфигурации...");
            Config.Clear();
        }

        private void LoadConfigValues()
        {
			Config["Версия конфигурации карты"] = version = GetConfig("Версия конфигурации карты ", Version.ToString());
			Config["Изображение карты (http:// или с папки data/RustMap)"] = mapUrl = GetConfig("Изображение карты (http:// или с папки data/RustMap)", "map.png");
			Config["Прозрачность карты"] = mapAlpha = GetConfig("Прозрачность карты", 0.95f);
			Config["Размер карты"] = mapSize = GetConfig("Размер карты", 0.45f);
			Config["Цвет текста (RED, GREEN, BLUE, ALPHA)"] = textColor = GetConfig("Цвет текста (RED, GREEN, BLUE, ALPHA)", "0 0.7 0 0.7");
			Config["Цвет текста кастомных иконок"] = textColor1 = GetConfig("Цвет текста кастомных иконок", "1 1 1 1");

			Config["Размер иконки игрока"] = playerIconSize = GetConfig("Размер иконки игрока", 0.035f);
			Config["Прозрачность иконки игрока"] = playerIconAlpha = GetConfig("Прозрачность иконки игрока", 0.95f);
			Config["Размер шрифта иконки игрока"] = playerIconFontSize = GetConfig("Размер шрифта иконки игрока", 13);
			Config["Показывать текущие координаты игрока"] = playerCoordinates = GetConfig("Показывать текущие координаты игрока", true);
			Config["Отображать местоположение соклановцев"] = clanSupport = GetConfig("Отображать местоположение соклановцев", false);
            Config["Отображать местоположение друзей"] = friendsSupport = GetConfig("Отображать местоположение друзей", false);
            Config["Размер иконки забаненого игрока"] = bannedSize = GetConfig("Размер иконки забаненого игрока", 0.035f);
			Config["Отображать местоположение смерти игрока"] = deathIcon = GetConfig("Отображать местоположение смерти игрока", true);
			Config["Размер иконки смерти игрока"] = deathIconSize = GetConfig("Размер иконки смерти игрока", 0.035f);

			Config["Отображать местоположение монументов"] = monuments = GetConfig("Отображать местоположение монументов", true);
            Config["Показывать пещеры"] = caves = GetConfig("Показывать пещеры", true);
            Config["Показывать подстанции"] = powersub = GetConfig("Показывать подстанции", true);
            Config["Показывать водонапорные башни"] = water = GetConfig("Показывать водонапорные башни", true);
            Config["Размер иконок монументов"] = monumentIconSize = GetConfig("Размер иконок монументов", 0.045f);
            Config["Прозрачность иконок монументов"] = monumentIconAlpha = GetConfig("Прозрачность иконок монументов", 0.95f);
            Config["Показывать название монументов"] = monumentIconNames = GetConfig("Показывать название монументов", true);
            Config["Размер шрифта монументов"] = monumentsFontSize = GetConfig("Размер шрифта монументов", 13);
			Config["Отображать дома, которые рейдят"] = raidhomes = GetConfig("Отображать дома, которые рейдят", true);
			Config["Размер иконок домов, которые рейдят"] = raidhomeIconSize = GetConfig("Размер иконок домов, которые рейдят", 0.035f);

			Config["Отображать местоположение самолёта"] = plane = GetConfig("Отображать местоположение самолёта", true);
			Config["Размер иконок самолёта"] = planeIconSize = GetConfig("Размер иконок самолёта", 0.035f);
			Config["Прозрачность иконок самолёта"] = planeIconAlpha = GetConfig("Прозрачность иконок самолёта", 0.95f);
			Config["Отображать местоположение cброшенного груза"] = planeDrop = GetConfig("Отображать местоположение cброшенного груза", true);
			Config["Размер иконок cброшенного груза"] = planeDropIconSize = GetConfig("Размер иконок cброшенного груза", 0.035f);
			Config["Прозрачность иконок cброшенного груза"] = planeDropIconAlpha = GetConfig("Прозрачность иконок cброшенного груза", 0.95f);

            Config["Отображать местоположение вертолёта"] = heli = GetConfig("Отображать местоположение вертолёта", true);
            Config["Размер иконок вертолёта"] = heliIconSize = GetConfig("Размер иконок вертолёта", 0.045f);
            Config["Прозрачность иконок вертолёта"] = heliIconAlpha = GetConfig("Прозрачность иконок вертолёта", 0.95f);
			Config["Отображать местоположение ящиков с вертолёта"] = heliDrop = GetConfig("Отображать местоположение ящиков с вертолёта", true);
            Config["Размер иконок ящиков с вертолёта"] = heliDropIconSize = GetConfig("Размер иконок ящиков с вертолёта", 0.035f);
            Config["Прозрачность иконок ящиков с вертолёта"] = heliDropIconAlpha = GetConfig("Прозрачность иконок ящиков с вертолёта", 0.95f);

			Config["Отображать местоположение грузового вертолёта"] = NewHeli = GetConfig("Отображать местоположение грузового вертолёта", true);
			Config["Размер иконки грузового вертолёта"] = NewheliIconSize = GetConfig("Размер иконки грузового вертолёта", 0.035f);
			Config["Прозрачность иконки грузового вертолёта"] = NewheliIconAlpha = GetConfig("Прозрачность иконки грузового вертолёта", 0.95f);
			Config["Отображать местоположение cброшенного груза с грузового вертолёта"] = NewHeliCrate = GetConfig("Отображать местоположение cброшенного груза с грузового вертолёта", false);
			Config["Размер иконки груза с грузового вертолёта"] = NewHeliCrateIconSize = GetConfig("Размер иконки груза с грузового вертолёта", 0.035f);
			Config["Прозрачность иконки груза с грузового вертолёта"] = NewHeliCrateIconAlpha = GetConfig("Прозрачность иконки груза с грузового вертолёта", 0.95f);

			Config["Отображать местоположение корабля"] = ship = GetConfig("Отображать местоположение корабля", true);
			Config["Размер иконки корабля"] = shipIconSize = GetConfig("Размер иконки корабля", 0.035f);
			Config["Прозрачность иконки корабля"] = shipIconAlpha = GetConfig("Прозрачность иконки корабля", 0.95f);

			Config["Отображать местоположение танка"] = bradley = GetConfig("Отображать местоположение танка", false);
			Config["Размер иконки танка"] = bradleyIconSize = GetConfig("Размер иконки танка", 0.045f);
			Config["Прозрачность иконки танка"] = bradleyIconAlpha = GetConfig("Прозрачность иконки танка", 0.95f);

			SaveConfig();
			if (!string.IsNullOrEmpty(mapUrl))
			{
				if (!mapUrl.ToLower().Contains("http"))
				{
					mapUrl = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + "RustMap" + Path.DirectorySeparatorChar + mapUrl;
				}
			}
        }

        T GetConfig <T> (string name, T defaultValue) 
		    => Config[name] == null ? defaultValue : (T) Convert.ChangeType(Config[name], typeof(T));

        #endregion

        #region Fields

        static RustMap m_Instance;
		private string mapIconJson = @"[{""name"":""{0}"",""parent"":""map_mainImage"",""components"":[{""type"":""UnityEngine.UI.RawImage"",""sprite"":""assets/content/textures/generic/fulltransparent.tga"",""png"":""{3}"",""color"":""1 1 1 {4}""},{""type"":""RectTransform"",""anchormin"":""{1}"",""anchormax"":""{2}""}]}]";
		private string mapIconTextJson = @"[{""name"":""{0}"",""parent"":""map_mainImage"",""components"":[{""type"":""UnityEngine.UI.Text"",""text"":""{3}"",""align"":""MiddleCenter"",""fontSize"":12,""color"":""{color}""},{""type"": ""UnityEngine.UI.Outline"",""color"": ""0 0 0 1""},{""type"":""RectTransform"",""anchormin"":""{1}"",""anchormax"":""{2}""}]}]";
        private string mapIconTextJsonIcon = @"[{""name"":""{0}"",""parent"":""map_mainImage"",""components"":[{""type"":""UnityEngine.UI.Text"",""text"":""{3}"",""align"":""MiddleCenter"",""fontSize"":9,""color"":""{color1}""},{""type"": ""UnityEngine.UI.Outline"",""color"": ""0 0 0 1""},{""type"":""RectTransform"",""anchormin"":""{1}"",""anchormax"":""{2}""}]}]";
		private string mapJson = "[{\"name\":\"map_mainImage\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"Assets/Content/UI/UI.Background.Tile.psd\",\"color\":\"1 1 1 {1}\",\"png\":\"{0}\"},{\"type\":\"RectTransform\",\"anchormin\":\"{2}\",\"anchormax\":\"{3}\"}]}]";
        private string mapCoordsTextJson = @"[{""name"":""map_coordinates"",""parent"":""map_mainImage"",""components"":[{""type"":""UnityEngine.UI.Text"",""text"":""{0}"",""align"":""MiddleCenter"",""fontSize"":18},{""type"": ""UnityEngine.UI.Outline"",""color"": ""0 0 0 1"",""distance"": ""0.5 -0.5""},{""type"":""RectTransform"",""anchormin"":""0 0.95"",""anchormax"":""1 1""}]}]";

        Dictionary<BasePlayer, MapPlayer> mapPlayers = new Dictionary<BasePlayer, MapPlayer>();
        Dictionary<BasePlayer, MapPlayer> subscribers = new Dictionary<BasePlayer, MapPlayer>();
        List<MapMarker> temporaryMarkers = new List<MapMarker>();
		private List<BasePlayer> AllPlayerUsers = new List<BasePlayer>();

        const string MAP_ADMIN_PERM = "rustmap.admin";
		const string MAP_BANNED_PERM = "rustmap.banned";
		const string MAP_DEATH_PERM = "rustmap.death";

        string monumentsJson;
        bool init = false;

        #endregion

        #region Command

        [ChatCommand("map")]
        void cmdMapControl(BasePlayer player, string command, string[] args)
        {
            if (!init || player == null)
			{
				player.ChatMessage("Извините! В данный момент карта не активирована.");
				return;
			}

            if (args.Count() >= 1 && args[0] == "all")
			{
				if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, MAP_ADMIN_PERM))
				{
					player.ChatMessage("У вас нет доступа к этой команде!");
					return;
				}
                if (AllPlayerUsers.Contains(player))
                    AllPlayerUsers.Remove(player);
                else
                    AllPlayerUsers.Add(player);
				CloseMap(player);
				OpenMap(player);
				player.ChatMessage("Режим администратора карты включен!");
				return;
            }

            if (subscribers.Keys.Contains(player))
            {
                CloseMap(player);
            }
            else
            {
                OpenMap(player);
            }
        }

		[ConsoleCommand("map.open")]
		void ConsoleMap(ConsoleSystem.Arg arg) 
		{
			var player = arg.Player();
			if (!init || player == null)
			{
				SendReply(player, "Извините! В данный момент карта не активирована.");
				return;
			}
			if(AllPlayerUsers.Contains(player))
				AllPlayerUsers.Remove(player);
			else
				AllPlayerUsers.Add(player);
			if (subscribers.Keys.Contains(player))
			{
				CloseMap(player);
			}
			else 
			{
				OpenMap(player);
			}
			return;
		}

		[ConsoleCommand("map.wipe")]
		private void CmdTest(ConsoleSystem.Arg arg) 
		{
			if (arg.Connection != null) return;
			m_FileManager.WipeData();
			Interface.Oxide.ReloadPlugin(Title);
		}

		[ConsoleCommand("map.all")]
        void ConsoleMapAll( ConsoleSystem.Arg arg)
        {
			var player = arg.Player();
			if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, MAP_ADMIN_PERM))
		    {
				SendReply(player, "У вас нет доступа к этой команде!");
				return;
			}
			if(AllPlayerUsers.Contains(player))
				AllPlayerUsers.Remove(player);
			else
				AllPlayerUsers.Add(player);
			CloseMap(player);
			OpenMap(player);
			SendReply(player, "Режим администратора карты включен!");
			return;	
		}

		#endregion

		#region Oxide Hooks

        void OnServerInitialized()
        {
            m_Instance = this;
            LoadConfigValues();
			permission.RegisterPermission(MAP_ADMIN_PERM, this);
            permission.RegisterPermission(MAP_BANNED_PERM, this);
			permission.RegisterPermission(MAP_DEATH_PERM, this);

            var anchorMin = new Vector2(0.5f - mapSize * 0.5f, 0.5f - mapSize * 0.800f);
			var anchorMax = new Vector2(0.5f + mapSize * 0.5f, 0.5f + mapSize * 0.930f);
			mapJson = mapJson.Replace("{2}", $"{anchorMin.x} {anchorMin.y}").Replace("{3}", $"{anchorMax.x} {anchorMax.y}");
            mapIconTextJson = mapIconTextJson.Replace("{color}", textColor);
			mapIconTextJsonIcon = mapIconTextJsonIcon.Replace("{color1}", textColor1);

            InitFileManager();
            m_FileManager.StartCoroutine(DownloadMapImage());

            foreach (var player in BasePlayer.activePlayerList)
                OnPlayerConnected(player);

            timer.Every(0.1f, () =>
            {
                foreach (var mm in temporaryMarkers)
                    if (mm.NeedRedraw())
                    {
                        ++mm.counter;
                        foreach (var sub in subscribers)
                            DrawMapMarker(sub.Key, mm);
                    }
                foreach (var sub in subscribers)
                {
                    RedrawPlayers(sub.Value);
                }
            });
            BansUpdate();
            timer.Every(20f, BansUpdate);

            foreach (var entity in BaseNetworkable.serverEntities.Select(p => p as BaseEntity).Where(p => p != null))
                OnEntitySpawned(entity);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (!mapPlayers.ContainsKey(player)) mapPlayers[player] = MapPlayer.Create(player);
        }

        void OnNewSave()
        {
            PrintWarning("Обнаружен вайп. Обновляем карту!");
            Interface.Oxide.DataFileSystem.WriteObject("RustMap/Images", new Dictionary<string, FileInfo>());
            Interface.Oxide.ReloadPlugin(Title);
        }

        void Unload()
        {
            if (m_FileManager != null)
            {
                m_FileManager.SaveData();
            }

            if (!init) return;
            foreach (var sub in subscribers.Keys)
            {
                CuiHelper.DestroyUi(sub, "map_mainImage");
            }
            if (FileManagerObject != null)
                UnityEngine.Object.Destroy(FileManagerObject);
        }

        void OnEntitySpawned(BaseEntity entity)
        {
            if (entity == null) return;
            if (plane && entity is CargoPlane)
                AddTemporaryMarker("plane", true, planeIconSize, planeIconAlpha, entity.transform);
            if (planeDrop && entity is SupplyDrop)
            {
                AddTemporaryMarker("mapsupply", false, planeDropIconSize, planeDropIconAlpha, entity.transform);
            }
            if (heli && entity is BaseHelicopter)
            {
                AddTemporaryMarker("heli", true, heliIconSize, heliIconAlpha, entity.transform);
            }
            if (heliDrop && entity is HelicopterDebris)
            {
                AddTemporaryMarker("helidebris", false, heliDropIconSize, heliDropIconAlpha, entity.transform);
            }
			if (NewHeli && entity is CH47Helicopter) 
			{
				AddTemporaryMarker("newheli", true, NewheliIconSize, NewheliIconAlpha, entity.transform);
			}
			if (NewHeliCrate && entity is HackableLockedCrate)
            {
                if (!(entity.GetParentEntity() is CargoShip))
                AddTemporaryMarker("newhelicreate", false, NewHeliCrateIconSize, NewHeliCrateIconAlpha, entity.transform);
            }
			if (ship && entity is CargoShip) 
			{
				AddTemporaryMarker("ship", true, shipIconSize, shipIconAlpha, entity.transform);
			}
			if (bradley && entity is BradleyAPC) 
			{
				AddTemporaryMarker("bradley", true, bradleyIconSize, bradleyIconAlpha, entity.transform);
			}
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity?.net?.ID == null) return;
            if (entity is CargoPlane || entity is SupplyDrop || entity is BaseHelicopter || entity is HelicopterDebris)
            {
                var transform = entity.transform;
                var mm = temporaryMarkers.Find(p => p.transform == transform);
                if (mm != null)
                    RemoveTemporaryMarker(mm);
            }
        }

        #endregion

        #region Core

        void OpenMap(BasePlayer player)
        {
            CuiHelper.AddUi(player, mapJson);
            CuiHelper.AddUi(player, monumentsJson);

            foreach (var mm in temporaryMarkers)
                DrawMapMarker(player, mm);

            List<ulong> members = null;

            if (player.IsAdmin && permission.UserHasPermission(player.UserIDString, MAP_BANNED_PERM))
            {
                foreach (var mm in bannedMarkers.Values)
                    DrawMapMarker(player, mm);
            }

			if (player.IsAdmin && permission.UserHasPermission(player.UserIDString, MAP_ADMIN_PERM) && AllPlayerUsers.Contains(player))
            {
                mapPlayers[player].clanTeam = BasePlayer.activePlayerList.Where(p => p != player).Select(MapPlayer.Create).ToList();
            }
            else if (clanSupport)
            {
                members = Interface.Oxide.RootPluginManager.GetPlugin("Clans").Call("GetClanMembers", player.userID) as List<ulong>;                
            }
			else if (friendsSupport)
            {
                members = (Interface.Oxide.RootPluginManager.GetPlugin("Friends").Call("GetFriends", player.userID) as ulong[])?.ToList();
            }

            if (members != null)
            {
                var onlineMembers = BasePlayer.activePlayerList.Where(p => members.Contains(p.userID) && p != player).ToList();
                mapPlayers[player].clanTeam = onlineMembers.Select(MapPlayer.Create).ToList();
            }
			if (raidhomes)
			{
				var zones = GetRaidZones(player);
				if (zones != null)
				{
					foreach (var zone in zones)
                    {
                        var anchors = ToAnchors(zone, raidhomeIconSize);
                        DrawIconNull(player, "raidhome" + zone, "raidhome" + zone, anchors, images["raidhome"], 0.95f, null, 12, false);
                    }
				}
			}
			if (playerDic.ContainsKey(player.userID))
			{
				if (deathIcon)
				{
					var anchors = ToAnchors(playerDic[player.userID].ToVector3(), deathIconSize);
				    DrawIconNull(player, "death" + player.userID, "death" + player.userID, anchors, images["death"], 0.95f, $"Ты умер здесь", 10, false);
				}
			}

            subscribers[player] = mapPlayers[player];
            RedrawPlayers(mapPlayers[player]);
        }

        List<Vector3> GetRaidZones(BasePlayer player)
        {
            return (List<Vector3>)NoEscape?.Call("ApiGetOwnerRaidZones", player.userID);
        }

		private Dictionary<ulong, string> playerDic = new Dictionary<ulong, string>();

		private object OnPlayerDeath(BasePlayer player, HitInfo info)
		{
			if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, MAP_DEATH_PERM))
				return null;
			if (playerDic.ContainsKey(player.userID))
			{
				playerDic.Remove(player.userID);
			}
			playerDic.Add(player.userID, player.transform.position.ToString());
			return null;
		}

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            subscribers.Remove(player);
            mapPlayers.Remove(player);
            foreach (var sub in mapPlayers)
            {
                var toRemove = sub.Value.clanTeam.Where(p => p.player == player).ToList();
                foreach (var teammate in toRemove)
                {
                    CuiHelper.DestroyUi(sub.Key, teammate.name+teammate.counter);
                    CuiHelper.DestroyUi(sub.Key, teammate.name + teammate.counter + "text");
                }
                toRemove.ForEach(p => sub.Value.clanTeam.Remove(p));
            }
        }

        void RedrawPlayers(MapPlayer mapPlayer)
        {
            var player = mapPlayer.player;
            if (mapPlayer.clanTeam != null)
            {
                foreach (var tmMapPlayer in mapPlayer.clanTeam)
                {
                    DrawMapPlayer(player, tmMapPlayer, true);
                }
            }
            DrawMapPlayer(player, mapPlayer);
        }

        void CloseMap(BasePlayer player)
        {
            subscribers.Remove(player);
            mapPlayers[player].OnCloseMap();
            CuiHelper.DestroyUi(player, "map_mainImage");
        }

        void DrawMapPlayer(BasePlayer player, MapPlayer mp, bool friend = false)
        {
            if (mp.NeedRedraw())
            {
                if (!friend && playerCoordinates)
                {
                    CuiHelper.DestroyUi(player, "map_coordinates");
					var curX = ((float)Math.Round(mp.transform.position.x, 1)).ToString();
					var curZ = ((float)Math.Round(mp.transform.position.z, 1)).ToString();
					CuiHelper.AddUi(player, Format(mapCoordsTextJson, "<size=16>X: "+curX+" <color=#EF015A>/</color> Z: "+curZ+"</size>"));
                }
                var pos = mp.player.transform.position;
                var anchors = ToAnchors(pos, playerIconSize);
                var png = !friend ? PlayerPng(mp.Rotation) : FriendPng(mp.Rotation);
                if (png == null)
                {
                    PrintError($"{friend}", mp.Rotation.ToString());
                    png = FriendPng(mp.Rotation - 2);
                }
                if (!InMap(pos))
                {
                    CuiHelper.DestroyUi(player, "mapPlayer" + mp.player.userID + (mp.counter));
                    CuiHelper.DestroyUi(player, "mapPlayer" + mp.player.userID + (mp.counter) + "text");
                    return;
                }
                DrawIcon(player, "mapPlayer" + mp.player.userID + (mp.counter), "mapPlayer" + mp.player.userID+(++mp.counter), anchors, png, mp.alpha, mp.player.displayName);
            }
        }

        void AddTemporaryMarker(string png, bool rotSupport, float size, float alpha, Transform transform, string name = "")
        {
            var mm = MapMarker.Create(transform);
            mm.name = string.IsNullOrEmpty(name) ? transform.GetInstanceID().ToString() : name;
            mm.png = png;
            mm.rotSupport = rotSupport;
            mm.size = size;
            mm.alpha = alpha;
            mm.fontsize = 12;
            mm.position = transform.position;
            temporaryMarkers.Add(mm);
            foreach (var sub in subscribers)
                DrawMapMarker(sub.Key, mm);
        }

        void RemoveTemporaryMarkerByName( string name )
        {
            var mm = temporaryMarkers.FirstOrDefault(p => p.name == name);
            if (mm != null)
                RemoveTemporaryMarker(mm);
        }

        void RemoveTemporaryMarker(MapMarker mm)
        {
            temporaryMarkers.Remove(mm);
            foreach (var sub in subscribers)
                CuiHelper.DestroyUi(sub.Key, mm.transform.GetInstanceID().ToString()+mm.counter);
        }

        void DrawIcon(BasePlayer player, string lastName, string name, string[] anchors, string png, float alpha, string text = null, int fontsize = 12, bool destroy = true)
        {
            if (destroy)
            {
                timer.Once(0.05f, () => {
                    CuiHelper.DestroyUi(player, lastName);
                });
                CuiHelper.DestroyUi(player, lastName + "text");
            }
            CuiHelper.AddUi(player, Format(mapIconJson, name, anchors[0], anchors[1], png,alpha));
            if (!string.IsNullOrEmpty(text))
                CuiHelper.AddUi(player, Format(mapIconTextJson, name + "text", anchors[2], anchors[3], text.Replace("\"", ""), fontsize));
        }

		void DrawIconNull(BasePlayer player, string lastName, string name, string[] anchors, string png, float alpha, string text = null, int fontsize = 12, bool destroy = true)
		{
			if (destroy)
			{
				timer.Once(0.05f, () =>
				{
					CuiHelper.DestroyUi(player, lastName);
				});
				CuiHelper.DestroyUi(player, lastName + "text");
			}
			CuiHelper.AddUi(player, Format(mapIconJson, name, anchors[0], anchors[1], png, alpha));
            if (!string.IsNullOrEmpty(text))
                CuiHelper.AddUi(player, Format(mapIconTextJsonIcon, name + "text", anchors[2], anchors[3], text.Replace("\"", ""), fontsize));
		}

		void DrawMapMarker(BasePlayer player, MapMarker mm)
        {
            if (mm.transform == null) return;
            var pos = mm.transform.position;
            var anchors = ToAnchors(pos, mm.size);
            var png = mm.png;
            if (mm.rotSupport)
                png += GetRotation(mm.transform.rotation.eulerAngles.y);

            if (images[png] == null)
            {
                PrintError("PNG = NULL: " + png);
            }
            if (mm.inMap && !InMap(pos))
            {
                CuiHelper.DestroyUi(player, mm.name + (mm.counter - 1));
                CuiHelper.DestroyUi(player, mm.name + (mm.counter - 1) + "text");
                mm.inMap = false;
                return;
            }
            mm.inMap = InMap(pos);
            if (!mm.inMap)
            {
                return;
            }
            DrawIcon(player, mm.name + (mm.counter-1), mm.name+(mm.counter), anchors, images[png], mm.alpha, mm.text);
        }

        bool InMap(Vector3 pos)
        {
            float halfSize = (int)TerrainMeta.Size.x * 0.5f;
            return pos.x < halfSize && pos.x > -halfSize && pos.z < halfSize && pos.z > -halfSize;
        }

        #endregion

        #region Banned player

        List<ulong> bannedCache = new List<ulong>();
        Dictionary<BasePlayer, MapMarker> bannedMarkers = new Dictionary<BasePlayer, MapMarker>();

        void AddBannedMarker(BasePlayer player)
        {
            var transform = player.transform;
            var mm = MapMarker.Create(transform);
            mm.png = "banned";
            mm.rotSupport = false;
            mm.name = transform.GetInstanceID().ToString();
            mm.alpha = 1f;
            mm.size = bannedSize;
            mm.position = transform.position;
            bannedMarkers[player] = mm;

            foreach (var sub in subscribers)
                if (player.IsAdmin && permission.UserHasPermission(sub.Key.UserIDString, MAP_BANNED_PERM))
                    DrawMapMarker(sub.Key, mm);
        }

        void RemoveBannedMarker(BasePlayer player)
        {
            var mm = bannedMarkers[player];
            bannedMarkers.Remove(player);
            foreach (var sub in subscribers)
                CuiHelper.DestroyUi(sub.Key, mm.transform.GetInstanceID().ToString()+mm.counter);
        }

        void BansUpdate()
        {
            var unlisted = ServerUsers.GetAll(ServerUsers.UserGroup.Banned).Select(p => p.steamid).Except(bannedCache).ToList();
            bannedCache.AddRange(unlisted);
            if (unlisted.Count == 0) return;
            foreach (var player in BasePlayer.sleepingPlayerList)
            {
                if (unlisted.Contains(player.userID))
                {
                    AddBannedMarker(player);
                }
            }
        }

        void OnPlayerBanned(Connection connection, string reason)
        {
            var userId = connection.userid;
            if (bannedCache.Contains(userId))return;

            var user = BasePlayer.activePlayerList.ToList().Find(p => p.userID == userId);
            if (user == null)
            {
                user = BasePlayer.sleepingPlayerList.ToList().Find(p => p.userID == userId);
                if (user == null) return;
                AddBannedMarker(user);
                return;
            }
            AddBannedMarker(user);
        }

        void OnEntityDeath(BaseCombatEntity entity)
        {
            var player = entity as BasePlayer;
            if (player == null || !player.IsSleeping()) return;
            if (bannedMarkers.ContainsKey(player))
            {
                RemoveBannedMarker(player);
            }
        }

        #endregion

        #region Markers

        void FindStaticMarkers()
        {
            if (!this.monuments)
            {
                monumentsJson = "";
                return;
            }
            var monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
            var container = new CuiElementContainer();
            foreach (var monument in monuments)
            {
                var anchors = ToAnchors(monument.transform.position, monumentIconSize);
                string png;
                string text = null;
                if (monument.Type == MonumentType.Cave && caves)
                    png = "cave";
                else if (monument.name.Contains("lighthouse"))
                {
                    png = "lighthouse";
                    text = "Маяк";
                }
                else if (monument.name.Contains("powerplant_1"))
                {
                    png = "powerplant";
                    text = "Атомная станция";
                }
                else if (monument.name.Contains("military_tunnel_1"))
                {
                    png = "militarytunnel";
                    text = "Тоннель";
                }
                else if (monument.name.Contains("airfield_1"))
                {
                    png = "airfield";
                    text = "Аэропорт";
                }
                else if (monument.name.Contains("trainyard_1"))
                {
                    png = "trainyard";
                    text = "Депо";
                }
                else if (monument.name.Contains("water_treatment_plant_1"))
                {
                    png = "watertreatment";
                    text = "Водоочистная";
                }
                else if (monument.name.Contains("warehouse"))
				{
                    png = "warehouse";
					text = "Склад";
				}
                else if (monument.name.Contains("satellite_dish"))
                {
                    png = "satellitedish";
                    text = "Антены";
                }
                else if (monument.name.Contains("sphere_tank"))
                {
                    png = "spheretank";
                    text = "Сфера";
                }
                else if (monument.name.Contains("harbor_1"))
                {
                    png = "harbor";
                    text = "Порт";
                }
                else if (monument.name.Contains("harbor_2"))
                {
                    png = "harbor";
                    text = "Порт";
                }
                else if (monument.name.Contains("radtown_small_3"))
                {
                    png = "radtown";
                    text = "Радтаун";
                }
                else if (monument.name.Contains("power_sub") && powersub)
                    png = "powersub";
                else if (monument.name.Contains("launch_site_1"))
                {
                    png = "launchsite";
                    text = "Космодром";
                }
                else if (monument.name.Contains("water_well") && water)
                    png = "water";
				else if (monument.name.Contains("gas_station"))
                {
                    png = "gasstation";
                    text = "Заправка";
                }
				else if (monument.name.Contains("supermarket"))
                {
                    png = "supermarket";
                    text = "Супермаркет";
                }
                else if (monument.name.Contains("mining_quarry_a"))
                {
                    png = "suquarry";
                    text = "Cерный карьер";
                }
                else if (monument.name.Contains("mining_quarry_b"))
                {
                    png = "stquarry";
                    text = "Каменный карьер";
                }
                else if (monument.name.Contains("mining_quarry_c"))
                {
                    png = "mvkquarry";
                    text = "МВК карьер";
                }
                else if (monument.name.Contains("junkyard_1"))
                {
                    png = "dump";
                    text = "Свалка";
                }
                else if (monument.name.Contains("compound"))
                {
                    png = "outpost";
                    text = "Аванпост";
                }
                else if (monument.name.Contains("bandit_town"))
                {
                    png = "bandit";
                    text = "База бандитов";
                }
                else if (monument.name.Contains("swamp"))
                {
                    png = "swamp";
                    text = "Болото";
                }
                else if (monument.name.Contains("OilrigAI"))
                {
                    png = "oilrig";
                    text = "Нефтяная вышка";
                }
                else if (monument.name.Contains("excavator_1"))
                {
                    png = "excavator";
                    text = "Экскаватор";
                }
                else if (monument.name.Contains("fishing_village"))
                {
                    png = "fishing";
                    text = "Рыбацкая база";
                }
                else if (monument.name.Contains("stables"))
                {
                    png = "ranch";
                    text = "Конюшня";
                }
                else if (monument.name.Contains("entrance_bunker"))
                {
                    png = "bunker";
                    text = "Метро";
                }
                else if (monument.name.Contains("underwater_lab"))
                {
                    png = "laboratory";
                    text = "Лаборатория";
                }
                else if (monument.name.Contains("ice_lake_4"))
                {
                    png = "icelake";
                    text = "Озеро";
                }

                else
                {
                    Puts("MAP IGNORE: " + monument.name);
                    continue;
                }

                container.Add(new CuiElement()
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = "map_mainImage",
                    Components =
                    {
                        new CuiRawImageComponent()
                        {
                            Sprite = "assets/content/textures/generic/fulltransparent.tga",
                            Png = images[png],
                            Color = $"1 1 1 {monumentIconAlpha}"
                        },
                        new CuiRectTransformComponent() {AnchorMin = anchors[0], AnchorMax = anchors[1]}
                    }
                });
                if (!string.IsNullOrEmpty(text) && monumentIconNames)
                {
                    container.Add(new CuiElement()
                    {
                        Name = CuiHelper.GetGuid(),
                        Parent = "map_mainImage",
                        Components =
						{
							new CuiTextComponent()
							{
								Text = text, FontSize = monumentsFontSize, Align = TextAnchor.MiddleCenter
							},
							new CuiRectTransformComponent() {AnchorMin = anchors[2], AnchorMax = anchors[3]}
                        }
                    });
                }
            }
            this.monumentsJson = container.ToJson();
        }

        string[] ToAnchors(Vector3 position, float size)
        {
            Vector2 center = ToScreenCoords(position);
            size *= 0.5f;
            return new[]
            {
                $"{center.x - size} {center.y - size}",
                $"{center.x + size} {center.y + size}",
                $"{center.x - 0.1} {center.y - size-0.04f}",
                $"{center.x + 0.1} {center.y - size+0.02}"
            };
        }

        Vector2 ToScreenCoords(Vector3 vec)
        {
            return new Vector2((vec.x + (int)World.Size * 0.5f) / (int)World.Size, (vec.z + (int)World.Size * 0.5f) / (int)World.Size);
        }

        static int GetRotation(float angle)
        {
            if (angle > 348.75f && angle < 11.25f)
                return 16;
            if (angle > 11.25f && angle < 33.75f)
                return 1;
            if (angle > 33.75f && angle < 56.25f)
                return 2;
            if (angle > 56.25f && angle < 78.75f)
                return 3;
            if (angle > 78.75f && angle < 101.25f)
                return 4;
            if (angle > 101.25f && angle < 123.75f)
                return 5;
            if (angle > 123.75f && angle < 146.25F)
                return 6;
            if (angle > 146.25F && angle < 168.75D)
                return 7;
            if (angle > 168.75F && angle < 191.25D)
                return 8;
            if (angle > 191.25F && angle < 213.4D)
                return 9;
            if (angle > 213.75F && angle < 236.25D)
                return 10;
            if (angle > 236.25F && angle < 258.75D)
                return 11;
            if (angle > 258.75D && angle < 281.25D)
                return 12;
            if (angle > 281.25D && angle < 303.75D)
                return 13;
            if (angle > 303.75D && angle < 326.25D)
                return 14;
            if (angle > 326.25D && angle < 348.75D)
                return 15;
            return 16;
        }

        #endregion

        #region API

        /*private void EnableMaps( BasePlayer player )
        {
            if (!subscribers.Keys.Contains( player ))
            {
                OpenMap( player );
            }
        }*/
        private void DisableMaps( BasePlayer player )
        {
            if (subscribers.Keys.Contains( player ))
            {
                CloseMap( player );
            }
        }

        #endregion

        #region Helpers

        bool mapLoaded = false;

        IEnumerator DownloadMapImage()
        {
            if (!string.IsNullOrEmpty(mapUrl))
            {
                images[MapFilename] = mapUrl;
            }
            if (images.ContainsKey(MapFilename))
            {
                yield return CommunityEntity.ServerInstance.StartCoroutine(LoadImages());
                mapLoaded = true;
                yield break;
            }
		}

        string Format(string value, params object[] args)
        {
            var result = new StringBuilder(value);
            for (int i = 0; i < args.Length; i++)
                if (args[i] == null)
                {
                    throw new NullReferenceException();
                }
                else
                {
                    result.Replace("{" + i + "}", args[i].ToString());
                }
            return result.ToString();
        }

        #endregion

        #region Images

        IEnumerator LoadImages()
        {
            foreach(var name in imagesKeys)
            {
                yield return m_FileManager.StartCoroutine(m_FileManager.LoadFile(name, images[name], name == MapFilename ? 1440 : -1));
                images[name] = m_FileManager.GetPng(name);
            }
            mapJson = Format(mapJson, MapPng(), mapAlpha);
            FindStaticMarkers();
            init = true;
            mapLoaded = true;
            PrintWarning("Изображения карты успешно загружены!");
            Interface.Call("OnMapInitialized");
            m_FileManager.SaveData();
        }

        string MapFilename => $"{ConVar.Server.level}_{World.Seed}_{TerrainMeta.Size.x}";
        List<string> imagesKeys => images.Keys.ToList();
        string PlayerPng(int rot) => images[imagesKeys[rot - 1]];
        string FriendPng(int rot) => images[imagesKeys[15 + rot]];
        string PlanePng(int rot) => images[imagesKeys[31 + rot]];
        string MapPng() => images[MapFilename];
        [HookMethod("RaidHomePng")]
        string RaidHomePng() => images["raidhome"];

        Dictionary<string, string> images = new Dictionary<string, string>()
        {
            {"player1", "https://i.imgur.com/0GU6snz.png"},
            {"player2", "https://i.imgur.com/iidstON.png"},
            {"player3", "https://i.imgur.com/c9TKY0x.png"},
            {"player4", "https://i.imgur.com/hfqvhTQ.png"},
            {"player5", "https://i.imgur.com/gAsYv7I.png"},
            {"player6", "https://i.imgur.com/JbU9y2z.png"},
            {"player7", "https://i.imgur.com/GEl49th.png"},
            {"player8", "https://i.imgur.com/UuEehmp.png"},
            {"player9", "https://i.imgur.com/IT5ZQXY.png"},
            {"player10", "https://i.imgur.com/vVy14zK.png"},
            {"player11", "https://i.imgur.com/FTBjpKQ.png"},
            {"player12", "https://i.imgur.com/SOl8xzo.png"},
            {"player13", "https://i.imgur.com/Ei6qnnz.png"},
            {"player14", "https://i.imgur.com/TTfAxvA.png"},
            {"player15", "https://i.imgur.com/nLGS8qn.png"},
            {"player16", "https://i.imgur.com/u4nmLfE.png"},

            {"friend1", "https://i.imgur.com/UGqUoZB.png"},
            {"friend2", "https://i.imgur.com/bimNGy0.png"},
            {"friend3", "https://i.imgur.com/kaL5XfV.png"},
            {"friend4", "https://i.imgur.com/jUqvtYs.png"},
            {"friend5", "https://i.imgur.com/7WlRhzf.png"},
            {"friend6", "https://i.imgur.com/aPVWE4Q.png"},
            {"friend7", "https://i.imgur.com/3srp4LL.png"},
            {"friend8", "https://i.imgur.com/IbpIE09.png"},
            {"friend9", "https://i.imgur.com/fOA0fZ7.png"},
            {"friend10", "https://i.imgur.com/GKZPmpW.png"},
            {"friend11", "https://i.imgur.com/NF5chyh.png"},
            {"friend12", "https://i.imgur.com/ofbLK9f.png"},
            {"friend13", "https://i.imgur.com/pk5X4wU.png"},
            {"friend14", "https://i.imgur.com/Sa7e3PA.png"},
            {"friend15", "https://i.imgur.com/E5tp5tR.png"},
            {"friend16", "https://i.imgur.com/ZqPxhPj.png"},

            {"plane1",  "https://i.imgur.com/aDyeXvj.png"},
            {"plane2",  "https://i.imgur.com/pWlyizJ.png"},
            {"plane3",  "https://i.imgur.com/6E7K0Gq.png"},
            {"plane4",  "https://i.imgur.com/NRCe09X.png"},
            {"plane5",  "https://i.imgur.com/UhknpcE.png"},
            {"plane6",  "https://i.imgur.com/eWVd09P.png"},
            {"plane7",  "https://i.imgur.com/OcL9fzK.png"},
            {"plane8",  "https://i.imgur.com/mGqyuLa.png"},
            {"plane9",  "https://i.imgur.com/fQlgXTg.png"},
            {"plane10", "https://i.imgur.com/ECmakbY.png"},
            {"plane11", "https://i.imgur.com/6fphkjm.png"},
            {"plane12", "https://i.imgur.com/Og5uqOo.png"},
            {"plane13", "https://i.imgur.com/KcvyhO6.png"},
            {"plane14", "https://i.imgur.com/zVtwcMD.png"},
            {"plane15", "https://i.imgur.com/fuMekcw.png"},
            {"plane16", "https://i.imgur.com/Me67yNG.png"},

            {"heli1",  "https://i.imgur.com/f3MWIJH.png"},
            {"heli2",  "https://i.imgur.com/q3i1L0N.png"},
            {"heli3",  "https://i.imgur.com/6hYqU96.png"},
            {"heli4",  "https://i.imgur.com/djkJuvq.png"},
            {"heli5",  "https://i.imgur.com/KDJdjZV.png"},
            {"heli6",  "https://i.imgur.com/zSMmf2H.png"},
            {"heli7",  "https://i.imgur.com/keTPKCZ.png"},
            {"heli8",  "https://i.imgur.com/bGTKQD0.png"},
            {"heli9",  "https://i.imgur.com/3k5bmVd.png"},
            {"heli10", "https://i.imgur.com/ZfHGe1m.png"},
            {"heli11", "https://i.imgur.com/DMY06qG.png"},
            {"heli12", "https://i.imgur.com/3Bjd6yD.png"},
            {"heli13", "https://i.imgur.com/8j13f9V.png"},
            {"heli14", "https://i.imgur.com/Aunu44u.png"},
            {"heli15", "https://i.imgur.com/9L0ft5W.png"},
            {"heli16", "https://i.imgur.com/Wsusbf7.png"},

            {"airfield", "https://i.imgur.com/v6Wk7ry.png"},
			{"lighthouse", "https://i.imgur.com/CYVacFH.png" },
            {"militarytunnel", "https://i.imgur.com/oi51exc.png"},
            {"trainyard", "https://i.imgur.com/XSl0gvT.png"},
            {"watertreatment", "https://i.imgur.com/EzhMX74.png"},
            {"warehouse", "https://i.imgur.com/ruBMLVC.png"},
            {"satellitedish", "https://i.imgur.com/LaRpc9y.png"},
            {"spheretank", "https://i.imgur.com/CJA4NbH.png"},
			{"radtown", "https://i.imgur.com/5wznmpU.png"},
            {"powerplant", "https://i.imgur.com/LBXK74b.png"},
            {"harbor", "https://i.imgur.com/zoa651g.png"},
            {"launchsite", "https://i.imgur.com/2EG5COo.png"},
			{"gasstation", "https://i.imgur.com/3WvnrvJ.png"},
			{"supermarket", "https://i.imgur.com/IRaQqVd.png"},
			{"dump", "https://i.imgur.com/2f7nW65.png"},
			{"outpost", "https://i.imgur.com/L9uLMkB.png"},
			{"bandit", "https://i.imgur.com/L7KHJpw.png"},
			{"swamp", "https://i.imgur.com/CfjyeMY.png"},
			{"oilrig", "https://i.imgur.com/QeWm786.png"},
			{"excavator", "https://i.imgur.com/2RgJbGM.png"},
			{"fishing", "https://i.imgur.com/ckyxI7L.png"},
			{"ranch", "https://i.imgur.com/FKkShq7.png"},
			{"bunker", "https://i.imgur.com/aMYtR4E.png"},

			{"laboratory", "https://i.imgur.com/hFb4J6j.png"},
			{"icelake", "https://i.imgur.com/nISek8q.png"},
			{"stquarry", "https://i.imgur.com/1DvWMaj.png"},
			{"suquarry", "https://i.imgur.com/8lFiqTz.png"},
			{"mvkquarry", "https://i.imgur.com/r0z2HkC.png"},
            {"cave", "https://i.imgur.com/psmzgNn.png"},
			{"powersub", "https://i.imgur.com/BtGsv79.png"},
            {"water", "https://i.imgur.com/0pbkTNa.png"},
            {"special", "https://i.imgur.com/zipB6dO.png"},
            {"mapsupply","https://i.imgur.com/K0KpYhd.png"},
            {"helidebris", "https://i.imgur.com/02o5WI4.png"},
            {"banned", "https://i.imgur.com/cZRBUo7.png"},
			{"raidhome", "https://i.imgur.com/5oAHC26.png"},
			{"rad", "https://i.imgur.com/91cwQFw.png"},
			{"death", "https://i.imgur.com/KDOcO2F.png"},

			{"newheli1", "https://i.imgur.com/c0rpuX1.png"},
			{"newheli2", "https://i.imgur.com/eP2qTBd.png"},
			{"newheli3", "https://i.imgur.com/EZqFoSR.png"},
			{"newheli4", "https://i.imgur.com/ohQ2scH.png"},
			{"newheli5", "https://i.imgur.com/NQvK0CI.png"},
			{"newheli6", "https://i.imgur.com/l9yanK4.png"},
			{"newheli7", "https://i.imgur.com/0UlqmQa.png"},
			{"newheli8", "https://i.imgur.com/J0TVvAG.png"},
			{"newheli9", "https://i.imgur.com/m4SxmQc.png"},
			{"newheli10", "https://i.imgur.com/KdetLZ0.png"},
			{"newheli11", "https://i.imgur.com/Vc42jX9.png"},
			{"newheli12", "https://i.imgur.com/gYTH8Wg.png"},
			{"newheli13", "https://i.imgur.com/o1NJi1p.png"},
			{"newheli14", "https://i.imgur.com/K9zWCPZ.png"},
			{"newheli15", "https://i.imgur.com/YxhbznD.png"},
			{"newheli16", "https://i.imgur.com/zXChO43.png"},
			{"newhelicreate", "https://i.imgur.com/CcbkIRR.png"},

			{"ship1", "https://i.imgur.com/4hSRUdg.png"},
			{"ship2", "https://i.imgur.com/xUTEd3U.png"},
			{"ship3", "https://i.imgur.com/lZv1U5z.png"},
			{"ship4", "https://i.imgur.com/BgHZ6wc.png"},
			{"ship5", "https://i.imgur.com/Uv0P9CM.png"},
			{"ship6", "https://i.imgur.com/BncezCI.png"},
			{"ship7", "https://i.imgur.com/Ep0IUPx.png"},
			{"ship8", "https://i.imgur.com/NMLhajD.png"},
			{"ship9", "https://i.imgur.com/f3BoNai.png"},
			{"ship10", "https://i.imgur.com/Rlp7hXG.png"},
			{"ship11", "https://i.imgur.com/i90yF43.png"},
			{"ship12", "https://i.imgur.com/rf6zxil.png"},
			{"ship13", "https://i.imgur.com/VzT3dWF.png"},
			{"ship14", "https://i.imgur.com/fe09Ym5.png"},
			{"ship15", "https://i.imgur.com/fkUVPNZ.png"},
			{"ship16", "https://i.imgur.com/T3vsHVT.png"},

			{"bradley1", "https://i.imgur.com/Cx4YnU0.png"},
			{"bradley2", "https://i.imgur.com/YjPKrNs.png"},
			{"bradley3", "https://i.imgur.com/49zRAlK.png"},
			{"bradley4", "https://i.imgur.com/tUCdA20.png"},
			{"bradley5", "https://i.imgur.com/buXFPMd.png"},
			{"bradley6", "https://i.imgur.com/tLVzXOx.png"},
			{"bradley7", "https://i.imgur.com/1LER8ej.png"},
			{"bradley8", "https://i.imgur.com/KzD1onh.png"},
			{"bradley9", "https://i.imgur.com/UfvKBzR.png"},
			{"bradley10", "https://i.imgur.com/UokyBCR.png"},
			{"bradley11", "https://i.imgur.com/P42fccT.png"},
			{"bradley12", "https://i.imgur.com/2ZcOPEI.png"},
			{"bradley13", "https://i.imgur.com/m352IqW.png"},
			{"bradley14", "https://i.imgur.com/XdTrKOl.png"},
			{"bradley15", "https://i.imgur.com/n1wlE1q.png"},
			{"bradley16", "https://i.imgur.com/TNXVSNW.png"}
        };

        #endregion

        #region File Manager

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

            DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetFile("RustMap/Images");

            private class FileInfo 
			{
                public string Url;
                public string Png;
            }

            public void SaveData()
            {
                dataFile.WriteObject(files);
            }

            public void WipeData()
            {
                files.Clear();
                SaveData();
            }

            public string GetPng(string name) => files[name].Png;

            private void Awake()
            {
                files = dataFile.ReadObject<Dictionary<string, FileInfo>>() ?? new Dictionary<string, FileInfo>(); 
            }

            public IEnumerator LoadFile(string name, string url, int size = -1)
            {
                if (files.ContainsKey(name) && files[name].Url == url && !string.IsNullOrEmpty(files[name].Png)) yield break;
                files[name] = new FileInfo() { Url = url };
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
    }
}
