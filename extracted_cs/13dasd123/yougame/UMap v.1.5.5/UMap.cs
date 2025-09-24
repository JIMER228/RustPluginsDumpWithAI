// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿// Requires: ImageLibrary
// Reference: System.Drawing
using System;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Drawing;

namespace Oxide.Plugins
{
    [Info("UMap", "ULTRARUST.RU", "1.5.5")]
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	//
	//	https://vk.com/lavrentyi13
	//
	//	to do:
	//
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    class UMap : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin Friends;
        [PluginReference] ImageLibrary ImageLibrary;

        static UMap instance;
        static float mapSize;

        private bool activated;
        private bool isNewSave;
		
        //  Частота обновления
		//	1f = 1 сек
        static float mapTime = 1.0f;        				//  карта && игрок
        static float entityTime = mapTime * delayUpdate;	//	объекты
		
		//	Задержка перед обновлением маркеров объектов
		//	Повышение этого значения уменьшает просадки фпс у игрока при открытой карте с большим количеством объектов, но уменьшает плавность их движения.
		static int delayUpdate = 2;

        private Dictionary<string, MapUser> mapUsers;

        private HashSet<MapMarker> staticMarkers;
		private HashSet<MapMarker> powerlineMarkers;
        private Dictionary<uint, ActiveEntity> entityMarkers;

        static string dataDirectory = $"file://{Interface.Oxide.DataDirectory}{Path.DirectorySeparatorChar}UMap{Path.DirectorySeparatorChar}";
        #endregion
        
        #region User Class  
        class MapUser : MonoBehaviour
        {
            private Dictionary<string, List<string>> friends;
            public List<string> friendList;

            public BasePlayer player;
            public MapMode mode;

            private MapMarker marker;

            private int mapX;
            private int mapZ;

            private bool mapOpen;

            private int changeCount;
            private double lastChange;
            private bool isBlocked;

			private int delayCount;
			
			//public bool isUpdatingEntity = false;
			//public bool isUpdatingPlayers = false;

            private SpamOptions spam;

            void Awake()
            {                
                player = GetComponent<BasePlayer>();
                friends = new Dictionary<string, List<string>>
                {
                    {"FriendsAPI", new List<string>() }
                };
                friendList = new List<string>();
                spam = instance.configData.SpamOptions;
                mapOpen = false;
                enabled = false;
                mode = MapMode.None;
                InvokeHandler.InvokeRepeating(this, UpdateMarker, 0.1f, mapTime);
            }
            void OnDestroy()
            {
				InvokeHandler.CancelInvoke(this, UpdateMarker);
                InvokeHandler.CancelInvoke(this, UpdateMap);
                DestroyUI();
            }
            public void InitializeComponent()
            {
                if (!instance.configData.FriendOptions.UseFriends) return;
				
                FillFriendList();
            }

            #region Friends
            private void FillFriendList()
            {
                friends["FriendsAPI"] = instance.GetRustFriends(player.userID);
				
                UpdateMembers();
            }
            private void UpdateMembers()
            {
                friendList = new List<string>();
                foreach (var list in friends)
                {
                    foreach (var member in list.Value)
                        friendList.Add(member);
                }
            }
            #endregion

            #region Maps
            public float Rotation() => GetDirection(player?.transform?.rotation.eulerAngles.y ?? 0);
            public int Position(bool x) => x ? mapX : mapZ;            
            public void Position(bool x, int pos)
            {
                if (x) mapX = pos;
                else mapZ = pos;
            } 

            public void ToggleMapType(MapMode mapMode)
            {
                if (isBlocked || IsSpam()) return;

                DestroyUI();

                if (mapMode == MapMode.None)
                {
                    InvokeHandler.CancelInvoke(this, UpdateMap);           
                    mode = MapMode.None;
                    mapOpen = false;
					DestroyUI();
                }
                else
                {
					mapOpen = true;
                    mode = MapMode.Main;
                    instance.OpenMainMap(player);
                    
                    if (!IsInvoking("UpdateMap"))
					{
						//	Обновляем маркеры объектов сразу при открытии карты
						delayCount = delayUpdate - 1;
						
						InvokeHandler.InvokeRepeating(this, UpdateMap, 0.1f, mapTime);
					}
                }   
            }
            public void UpdateMap()
            {
				// Счетчик задержки +1
				delayCount++;
					
				//	При достижении лимита обновляем overlay объектов и сбрасываем счетчик
				if (delayCount == delayUpdate)
				{
					delayCount = 0;
					//	Обновляем overlay объектов
					instance.UpdateEntity(player, LustyUI.EntityOverlay, LustyUI.MainMin, LustyUI.MainMax, 0.01f);
				}
				
				//	Обновляем overlay игрока и его друзей
				instance.UpdatePlayers(player, LustyUI.PlayersOverlay, LustyUI.MainMin, LustyUI.MainMax, 0.01f);
            }
            #endregion

            #region Spam Checking
            private bool IsSpam()
            {
                if (!spam.Enabled) return false;

                changeCount++;
                var current = GrabCurrentTime();
                if (current - lastChange < spam.TimeBetweenAttempts)
                {
                    lastChange = current;
                    if (changeCount > spam.WarningAttempts && changeCount < spam.DisableAttempts)
                    {
                        instance.SendReply(player, instance.msg("spamWarning", player.UserIDString));
                        return false;
                    }
                    if (changeCount >= spam.DisableAttempts)
                    {
                        instance.SendReply(player, string.Format(instance.msg("spamDisable", player.UserIDString), spam.DisableSeconds));
                        Block();
                        Invoke("Unblock", spam.DisableSeconds);
                        return true;
                    }
                }
                else
                {
                    lastChange = current;
                    changeCount = 0;
                }
                return false;
            }
            private void Block()
            {                
                isBlocked = true;
                OnDestroy();
            }
            private void Unblock()
            {
                isBlocked = false;
                instance.SendReply(player, instance.msg("spamEnable", player.UserIDString));
            }
            #endregion

            #region Other
            public void DestroyUI() => LustyUI.DestroyUI(player);
            private void UpdateMarker()
            {
                marker = new MapMarker { name = player.displayName, r = GetDirection(player?.eyes?.rotation.eulerAngles.y ?? 0), x = GetPosition(transform.position.x), z = GetPosition(transform.position.z) };
            }
            public MapMarker GetMarker() => marker;
            
            public void ToggleMain()
            {
                if (mapOpen && mode == MapMode.Main)
                {
                    ToggleMapType(MapMode.None);
                }
                else
                {
                    ToggleMapType(MapMode.Main);
                }
            }
            #region Friends
            public bool HasFriendList(string name) => friends.ContainsKey(name);
            public void AddFriendList(string name, List<string> friendlist) { friends.Add(name, friendlist); UpdateMembers(); }
            public void RemoveFriendList(string name) { friends.Remove(name); UpdateMembers(); }
            public void UpdateFriendList(string name, List<string> friendlist) { friends[name] = friendlist; UpdateMembers(); }

            public bool HasFriend(string name, string friendId) => friends[name].Contains(friendId);
            public void AddFriend(string name, string friendId) { friends[name].Add(friendId); UpdateMembers(); }
            public void RemoveFriend(string name, string friendId) { friends[name].Remove(friendId); UpdateMembers(); }
            #endregion
            #endregion
        }

        MapUser GetUser(BasePlayer player) => player.GetComponent<MapUser>() ?? null;
        MapUser GetUserByID(string playerId) => mapUsers.ContainsKey(playerId) ? mapUsers[playerId] : null;
        #endregion

        #region Markers
        class ActiveEntity : MonoBehaviour
        {
            public BaseEntity entity;
            private MapMarker marker;
            public AEType type;
            private string icon;

            void Awake()
            {
                entity = GetComponent<BaseEntity>();
                marker = new MapMarker();
                enabled = false;
            }
            void OnDestroy()
            {
                InvokeHandler.CancelInvoke(this, UpdatePosition);
            }
            public void SetType(AEType type)
            {
                this.type = type;
                switch (type)
                {
                    case AEType.None:
                        break;
                    case AEType.Plane:
                        icon = "plane";
                        break;
                    case AEType.SupplyDrop:
                        icon = "supply";
                        break;
                    case AEType.Helicopter:
                        icon = "heli";
                        break;
					//	Bradley
                    case AEType.Bradley:
                        icon = "bradley";
                        break;
                }
                //  Включаем постоянное обновление маркера для брэдли, самолета или вертолета
                if (type == AEType.Bradley || type == AEType.Plane || type == AEType.Helicopter)
                    InvokeHandler.InvokeRepeating(this, UpdatePosition, 0.1f, entityTime);
                //  Аирдроп не требует постоянного обновления маркера
                else if (type == AEType.SupplyDrop)
                    InvokeHandler.Invoke(this, UpdatePositionWithoutRotation, 0.1f);
            }
            public MapMarker GetMarker() => marker;
			//	Разделяем, меньше if'ов для обработки...
            //  Апдейт маркера без учета поворота
            void UpdatePositionWithoutRotation()
            {
                marker.icon = $"{icon}";
                marker.x = GetPosition(entity.transform.position.x);
                marker.z = GetPosition(entity.transform.position.z);    
            }
            //  Апдейт маркера с учетом поворота
            void UpdatePosition()
            {              
                marker.r = GetDirection(entity?.transform?.rotation.eulerAngles.y ?? 0);
                marker.icon = $"{icon}{marker.r}";
                marker.x = GetPosition(entity.transform.position.x);
                marker.z = GetPosition(entity.transform.position.z);    
            }
        }
        class MapMarker
        {
            public string name { get; set; }
            public float x { get; set; }
            public float z { get; set; }
            public float r { get; set; }
            public string icon { get; set; }
        }
        public enum MapMode
        {
            None,
            Main
        }
        enum AEType
        {
            None,
            Plane,
            SupplyDrop,
            Helicopter,
			Bradley			
        }
        #endregion

        #region UI
        class LMUI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, string parent = "Hud")
            {
				var NewElement = new CuiElementContainer()
				{
					{
						new CuiPanel
						{
							Image = {Color = color},
							RectTransform = {AnchorMin = aMin, AnchorMax = aMax}
						},
						new CuiElement().Parent = parent,
						panelName
					}
				};
                return NewElement;
            }
            static public void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel, CuiHelper.GetGuid());
            }
			//	Очистка от лишних параметров -> быстрее
            //  Из CuiLabel в CuiElement для обводки
            static public void CreateLabel(ref CuiElementContainer container, string panel, string text, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
					Components =
					{
						new CuiTextComponent { Color = "1.0 1.0 1.0 1.0", FontSize = 13, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter, Text = text },
						new CuiRectTransformComponent {	AnchorMin = aMin, AnchorMax = aMax },
						new CuiOutlineComponent	{ Distance = "0.693 0.793", Color = "0 0 0 1" }
					}
                });
            }
            //  Добавлен компонент обводки
            static public void LoadImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax, string odistance, string outline)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent { Png = png, Sprite = "assets/content/textures/generic/fulltransparent.tga" },
                        new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax },
                        new CuiOutlineComponent	{ Distance = odistance, Color = outline }
                    }
                });
            }
        }   
        #endregion

        #region Oxide Hooks
        void OnNewSave(string filename)
        {
            isNewSave = true;
        }
        void Loaded()
        {
            mapUsers = new Dictionary<string, MapUser>();
            staticMarkers = new HashSet<MapMarker>();
			powerlineMarkers = new HashSet<MapMarker>();
            entityMarkers = new Dictionary<uint, ActiveEntity>();

            lang.RegisterMessages(Messages, this);
        }
        void OnServerInitialized()
        {
            instance = this;

            mapSize = TerrainMeta.Size.x;

            LoadVariables();

            FindStaticMarkers();
            ValidateImages();
        }
		void FindTemporaryEntityMarkers()
		{
			//	Добавляем активные Брэдли в temporaryEntityMarkers
            BradleyAPC[] bradleys = UnityEngine.Object.FindObjectsOfType<BradleyAPC>();
            if (bradleys.Length > 0)
            {
                foreach (BradleyAPC entity in bradleys)
                {
					AddTemporaryEntityMarker(entity);
                }
            }
			//	Добавляем активные самолеты в temporaryEntityMarkers
            CargoPlane[] planes = UnityEngine.Object.FindObjectsOfType<CargoPlane>();
            if (planes.Length > 0)
            {
                foreach (CargoPlane entity in planes)
                {
					AddTemporaryEntityMarker(entity);
                }
            }
			//	Добавляем активные вертолеты в temporaryEntityMarkers
            BaseHelicopter[] heli = UnityEngine.Object.FindObjectsOfType<BaseHelicopter>();
            if (heli.Length > 0)
            {
                foreach (BaseHelicopter entity in heli)
                {
					AddTemporaryEntityMarker(entity);
                }
            }
			//	Добавляем активные аирдропы в temporaryEntityMarkers
            SupplyDrop[] supply = UnityEngine.Object.FindObjectsOfType<SupplyDrop>();
            if (supply.Length > 0)
            {
                foreach (SupplyDrop entity in supply)
                {
					AddTemporaryEntityMarker(entity);
                }
            }
		}
        void OnPlayerInit(BasePlayer player)
        {
            if (player == null) return;
            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot) || player.IsSleeping())
            {
                timer.In(3, () => OnPlayerInit(player));
                return;
            }
            if (activated)
            {
                var user = GetUser(player);
                if (user != null)
                {
                    UnityEngine.Object.DestroyImmediate(user);
                    if (mapUsers.ContainsKey(player.UserIDString))
                        mapUsers.Remove(player.UserIDString);
                }

                var mapUser = player.gameObject.AddComponent<MapUser>();
                if (!mapUsers.ContainsKey(player.UserIDString))
                    mapUsers.Add(player.UserIDString, mapUser);
                mapUser.InitializeComponent();
            }
        }
        void OnPlayerDisconnected(BasePlayer player)
        {
            if (player == null) return;            
            if (mapUsers.ContainsKey(player.UserIDString))
            {
                UnityEngine.Object.Destroy(mapUsers[player.UserIDString]);
                mapUsers.Remove(player.UserIDString);
            }

            LustyUI.DestroyUI(player);
        }
        void OnEntitySpawned(BaseEntity entity)
        {
            if (!activated) return;
            if (entity == null) return;
            if (entity is CargoPlane || entity is SupplyDrop || entity is BaseHelicopter || entity is BradleyAPC)
                AddTemporaryEntityMarker(entity);
        }
        void OnEntityKill(BaseNetworkable entity)
        {
            var activeEntity = entity?.GetComponent<ActiveEntity>();
            if (activeEntity == null || entity?.net?.ID == null) return;
            if (entityMarkers.ContainsKey(entity.net.ID))
                entityMarkers.Remove(entity.net.ID);
            UnityEngine.Object.Destroy(activeEntity);
        }
        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)                  
                OnPlayerDisconnected(player);

            var mapUsers = UnityEngine.Object.FindObjectsOfType<MapUser>();
            if (mapUsers != null)
                foreach (var user in mapUsers)
                    UnityEngine.Object.DestroyImmediate(user);

            var tempMarkers = UnityEngine.Object.FindObjectsOfType<ActiveEntity>();
            if (tempMarkers != null)
                foreach (var marker in tempMarkers)
                    UnityEngine.Object.DestroyImmediate(marker);
        }
        #endregion

        #region Static UI Generation
        static class LustyUI
        {
            public static string Main = "LMUI_MapMain";
            public static string EntityOverlay = "LMUI_EntityOverlay";
			public static string PlayersOverlay = "LMUI_PlayersOverlay";

            public static string MainMin;
            public static string MainMax;

            public static CuiElementContainer StaticMain;

            private static Dictionary<ulong, List<string>> OpenUI = new Dictionary<ulong, List<string>>();

            public static void RenameComponents()
            {
                if (StaticMain != null)
                {
                    foreach (var element in StaticMain)
                    {
                        if (element.Name == "AddUI CreatedPanel")
                            element.Name = CuiHelper.GetGuid();
                    }
                }
                instance.activated = true;
                instance.ActivateMaps();
            }
            public static void AddBaseUI(BasePlayer player, MapMode type)
            {
                try
				{
					var user = instance.GetUser(player);
					if (user == null) return;

					DestroyUI(player);
					CuiElementContainer element = null;
					element = StaticMain;
					CuiHelper.AddUi(player, StaticMain);
					AddElementIds(player, ref element);
                }
				catch
				{
				}
			}
            private static void AddElementIds(BasePlayer player, ref CuiElementContainer container)
            {
                if (!OpenUI.ContainsKey(player.userID))
                    OpenUI.Add(player.userID, new List<string>());
                foreach (var piece in container)
                    OpenUI[player.userID].Add(piece.Name);               
            }
            public static void DestroyUI(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, Main);
                CuiHelper.DestroyUi(player, EntityOverlay);
				CuiHelper.DestroyUi(player, PlayersOverlay);
                if (!OpenUI.ContainsKey(player.userID)) return;
                foreach (var piece in OpenUI[player.userID])
                    CuiHelper.DestroyUi(player, piece);
            }         
            public static string Color(string hexColor, float alpha)
            {
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }

        void GenerateMaps(bool main)
        {
            if (!ImageLibrary.IsReady())
            {
                timer.In(30, () => GenerateMaps(main));
                Puts("Ожидаем ImageLibrary...");
                return;
            }
            if (main) CreateStaticMain();
        }
        void CreateStaticMain()
        {
            Puts("Подготавливаем карту...");
            string mapimage = string.Empty;
            
            mapimage = GetImage("mapimage");
            
            if (string.IsNullOrEmpty(mapimage))
            {
                activated = false;
				timer.In(5f, () =>
				{
					Puts("Обновляем изображения в storage...");
					LoadImages();
                    LoadMapImage();
				});
                return;
            }
            float iconsize = 0.0145f;
            LustyUI.MainMin = "0.265 0.133";
            LustyUI.MainMax = "0.734 0.931";

            var mapContainer = LMUI.CreateElementContainer(LustyUI.Main, "0 0 0 1", LustyUI.MainMin, LustyUI.MainMax);
            LMUI.LoadImage(ref mapContainer, LustyUI.Main, mapimage, "0 0", "1 1", "1 1", "0 0 0 1");      

            foreach(var marker in staticMarkers)
            {
                var image = GetImage(marker.icon);
                if (string.IsNullOrEmpty(image)) continue;
                LMUI.LoadImage(ref mapContainer, LustyUI.Main, image, $"{marker.x - iconsize} {marker.z - iconsize}", $"{marker.x + iconsize} {marker.z + iconsize}", "0.03 0.07", "0 0 0 1");
            }
			
			//	Добавляем ЛЭП на карту
			if (configData.MapOptions.EnablePowerlines)
			{
				foreach(var marker in powerlineMarkers)
				{
					var image = GetImage(marker.icon);
					if (string.IsNullOrEmpty(image)) continue;
					LMUI.LoadImage(ref mapContainer, LustyUI.Main, image, $"{marker.x - iconsize} {marker.z - iconsize}", $"{marker.x + iconsize} {marker.z + iconsize}", "0.03 0.07", "0 0 0 1");
				}
			}
			 
            LustyUI.StaticMain = mapContainer;
			//	Ищем уже активные объекты и добавляем на карту
			Puts("Добавляем активные объекты на карту...");
			FindTemporaryEntityMarkers();
            Puts("Готово!");      
            
            LustyUI.RenameComponents();
        }
        #endregion

        #region Maps
        void ActivateMaps()
        {
            foreach (var player in BasePlayer.activePlayerList)            
                OnPlayerInit(player);            
        }

        #region Map/Overlays
        void OpenMainMap(BasePlayer player) => LustyUI.AddBaseUI(player, MapMode.Main);
		//	Overlay для объектов
        void UpdateEntity(BasePlayer player, string panel, string posMin, string posMax, float iconsize)
        {
			//var user = GetUser(player);
            //if (user == null || user.isUpdatingEntity) return;

			//	Обозначаем, что Overlay объектов уже обновляется
			//user.isUpdatingEntity = true;

			var mapContainer = LMUI.CreateElementContainer(panel, "0 0 0 0", posMin, posMax);

			//	Объекты
            foreach (var entity in entityMarkers)
            {
                var marker = entity.Value.GetMarker();
                if (marker == null) continue;
                var image = GetImage(marker.icon);
                if (string.IsNullOrEmpty(image)) continue;   
                AddIconToMap(ref mapContainer, panel, image, entity.Value.type == AEType.Bradley ? iconsize * 3.5f : iconsize * 3.3f, marker.x, marker.z);
            }

			//	Обозначаем, что Overlay объектов завершил обновление
			//user.isUpdatingEntity = false;
			
			//	Перекидываем на следующий Tick сервера
			NextTick(() =>
            {
				CuiHelper.DestroyUi(player, panel);
				CuiHelper.AddUi(player, mapContainer);
			});
        }
		//	Overlay для маркеров игрока и его друзей
        void UpdatePlayers(BasePlayer player, string panel, string posMin, string posMax, float iconsize)
        {
			var user = GetUser(player);
            if (user == null /*|| user.isUpdatingPlayers*/) return;

			//	Обозначаем, что Overlay игрока и его друзей уже обновляется
			//user.isUpdatingPlayers = true;

			var mapContainer = LMUI.CreateElementContainer(panel, "0 0 0 0", posMin, posMax);

			//	Друзья
			if (configData.FriendOptions.UseFriends)
			{	
				foreach (var friendId in user.friendList)
				{
					if (friendId == player.UserIDString) continue;

					if (mapUsers.ContainsKey(friendId))
					{
						var friend = mapUsers[friendId];
						var marker = friend.GetMarker();
						if (marker == null) continue;
						var image = GetImage($"friend{marker.r}");
						if (string.IsNullOrEmpty(image)) continue;
						AddNamedIconToMap(ref mapContainer, panel, image, marker.name, iconsize * 1.75f, marker.x, marker.z);
					}
				}
			}
			//	Игрок
            var selfMarker = user.GetMarker();
            if (selfMarker != null)
            {
                var selfImage = GetImage($"self{selfMarker.r}");
				//AddNamedIconToMap(ref mapContainer, panel, selfImage, "SergoMashina", iconsize * 1.75f, selfMarker.x, selfMarker.z);
                AddIconToMap(ref mapContainer, panel, selfImage, iconsize * 1.75f, selfMarker.x, selfMarker.z);
            }

            //	Обозначаем, что Overlay игрока и его друзей завершил обновление
			//user.isUpdatingPlayers = false;
			
			//	Перекидываем на следующий Tick сервера
			NextTick(() => 
            {
				CuiHelper.DestroyUi(player, panel);
				CuiHelper.AddUi(player, mapContainer);
			});
        }
		//	Разделяем маркеры дабы экономить ресурсы (даже невидимая приписка/имя жрет ресурсы и фпс у игрока)
		//	Маркер без имени
		void AddIconToMap(ref CuiElementContainer mapContainer, string panel, string image, float iconsize, float posX, float posZ)
        {
            if (posX < iconsize || posX > 1 - iconsize || posZ < iconsize || posZ > 1 - iconsize) return;
            LMUI.LoadImage(ref mapContainer, panel, image, $"{posX - iconsize} {posZ - iconsize}", $"{posX + iconsize} {posZ + iconsize}", "0.8 0.8", "0 0 0 1");
        }
		//	Маркер с именем
        void AddNamedIconToMap(ref CuiElementContainer mapContainer, string panel, string image, string name, float iconsize, float posX, float posZ)
        {
            if (posX < iconsize || posX > 1 - iconsize || posZ < iconsize || posZ > 1 - iconsize) return;
            LMUI.LoadImage(ref mapContainer, panel, image, $"{posX - iconsize} {posZ - iconsize}", $"{posX + iconsize} {posZ + iconsize}", "0.8 0.8", "0 0 0 1");
            LMUI.CreateLabel(ref mapContainer, panel, name, $"{posX - 0.1} {posZ - iconsize - 0.025}", $"{posX + 0.1} {posZ - iconsize}");
        }        
        #endregion
        #endregion

        #region Commands
		//	Alias для карты LustyMap v2
        [ConsoleCommand("LMUI_Control")]
        private void cmdLustyControl(ConsoleSystem.Arg arg)
        {
            if (!activated) return;
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;
            var user = GetUser(player);
            if (user == null) return;

            user.ToggleMain();
        }
		//	Alias для карты LustyMap v1
		[ConsoleCommand("LustyMap")]
        void cmdLustyConsole(ConsoleSystem.Arg arg)
		{
            if (!activated) return;
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;
            var user = GetUser(player);
            if (user == null) return;

            user.ToggleMain();
        }
		//	Alias для карты Moscow.Ovh
        [ConsoleCommand("map.open")]
        private void cmdMoscowOvh(ConsoleSystem.Arg arg)
        {
            if (!activated) return;
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;
            var user = GetUser(player);
            if (user == null) return;

            user.ToggleMain();
        }
        [ConsoleCommand("resetmap")]
        void ccmdResetmap(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            SendReply(arg, "Map reset Confirmed! Creating a new image load order with ImageLibrary");
            LoadImages();
            LoadMapImage();                 
        }
        [ChatCommand("map")]
        void cmdOpenMap(BasePlayer player, string command, string[] args)
        {
            if (!activated)
            {
                SendReply(player, "LustyMap is not activated");
                return;
            }
			player.ChatMessage($"<size=16><color=#00FF00>》</color> Используйте бинд на карту через консоль <color=#00FF00>F1</color> !</size>");
			player.ChatMessage($"<size=16><color=#00FF00>》</color> <color=#00FF00>bind M map.open</color></size>");
        }
        #endregion

        #region Functions
        private void AddTemporaryEntityMarker(BaseEntity entity)
        {
            if (entity == null || entity?.net?.ID == null) return;
            AEType type = AEType.None;
            if (entity is CargoPlane)
            {
                type = AEType.Plane;
            }
			else if (entity is SupplyDrop)
            {
                type = AEType.SupplyDrop;
            }
            else if (entity is BaseHelicopter)
            {
                type = AEType.Helicopter;
            }
			else if (entity is BradleyAPC)
            {
                type = AEType.Bradley;
            }
            var actEnt = entity.gameObject.AddComponent<ActiveEntity>();
            actEnt.SetType(type);

            entityMarkers.Add(entity.net.ID, actEnt);
        }
        private void FindStaticMarkers()
        {
            //  Ищем РТ
			//	Все РТ используют свою личную иконку, как на PlayRust.io
            var monuments = UnityEngine.Object.FindObjectsOfType<MonumentInfo>();
            foreach (var monument in monuments)
            {                    
                MapMarker mon = new MapMarker
                {
                    x = GetPosition(monument.transform.position.x),
                    z = GetPosition(monument.transform.position.z)
                };
                if (monument.name.Contains("lighthouse"))
                {
                    mon.icon = "lighthouse";
                    staticMarkers.Add(mon);
                    continue;
                }
                if (monument.Type == MonumentType.Cave && configData.MapOptions.EnableCaves)
                {
                    mon.icon = "cave";
                    staticMarkers.Add(mon);
                    continue;
                }
                if (monument.name.Contains("powerplant_1"))
                {
                    mon.icon = "powerplant";
                    staticMarkers.Add(mon);
                    continue;
                }
                if(monument.name.Contains("harbor_1"))
                {
                    mon.icon = "harbor";
                    staticMarkers.Add(mon);
                    continue;
                }
                if (monument.name.Contains("harbor_2"))
                {
                    mon.icon = "harbor";
                    staticMarkers.Add(mon);
                    continue;
                }
                if (monument.name.Contains("military_tunnel_1"))
                {
                    mon.icon = "tunnel";
                    staticMarkers.Add(mon);
                    continue;
                }
                if (monument.name.Contains("airfield_1"))
                {
                    mon.icon = "airfield";
                    staticMarkers.Add(mon);
                    continue;
                }
                if (monument.name.Contains("trainyard_1"))
                {
                    mon.icon = "trainyard";
                    staticMarkers.Add(mon);
                    continue;
                }
                if (monument.name.Contains("water_treatment_plant_1"))
                {
                    mon.icon = "treatment";
                    staticMarkers.Add(mon);
                    continue;
                }
                if (monument.name.Contains("warehouse"))
                {
                    mon.icon = "warehouse";
                    staticMarkers.Add(mon);
                    continue;
                }
                if (monument.name.Contains("satellite_dish"))
                {
                    mon.icon = "dish";
                    staticMarkers.Add(mon);
                    continue;
                }
                if (monument.name.Contains("sphere_tank"))
                {
                    mon.icon = "spheretank";
                    staticMarkers.Add(mon);
                    continue;
                }
                if (monument.name.Contains("radtown_small_3"))
                {
                    mon.icon = "radtown";
                    staticMarkers.Add(mon);
                    continue;
                }
                if (monument.name.Contains("launch_site_1"))
                {
                    mon.icon = "launchsite";
                    staticMarkers.Add(mon);
                    continue;
                }
                if (monument.name.Contains("gas_station_1"))
                {
                    mon.icon = "gas";
                    staticMarkers.Add(mon);
                    continue;
                }
                if (monument.name.Contains("supermarket_1"))
                {
                    mon.icon = "market";
                    staticMarkers.Add(mon);
                    continue;
                }
            }
			//	Проверяем включены ли ЛЭП в конфиге
			if (configData.MapOptions.EnablePowerlines)
			{
			
				//  Ищем ЛЭП
				var powerlines = UnityEngine.Object.FindObjectsOfType<PowerlineNode>();
				foreach (var powerline in powerlines)
				{
					MapMarker mon = new MapMarker
					{
						x = GetPosition(powerline.transform.position.x),
						z = GetPosition(powerline.transform.position.z)
					};
					if (powerline.name.Contains("powerline_b"))
					{
						mon.icon = "powerline_b";
						powerlineMarkers.Add(mon);
						continue;
					}
					if (powerline.name.Contains("powerline_c"))
					{
						mon.icon = "powerline_c";
						powerlineMarkers.Add(mon);
						continue;
					}
					if (powerline.name.Contains("powerline_d"))
					{
						mon.icon = "powerline_d";
						powerlineMarkers.Add(mon);
						continue;
					}
					if (powerline.name.Contains("powerline_"))
					{
						mon.icon = "powerline";
						powerlineMarkers.Add(mon);
						continue;
					}
				}
            }	
        }
        static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        #endregion

        #region Helpers
        static float GetPosition(float pos) => (pos + mapSize / 2f) / mapSize;  
        static int GetDirection(float rotation) => (int)((rotation - 5) / 10 + 0.5) * 10;
        #endregion

        #region API
        #region Friends
        bool AddFriendList(string playerId, string name, List<string> list, bool bypass = false)
        {
            if (!bypass && !configData.FriendOptions.AllowCustomLists) return false;
            var user = GetUserByID(playerId);
            if (user == null) return false;
            if (user.HasFriendList(name))
                return false;

            user.AddFriendList(name, list);
            return true;
        }
        bool RemoveFriendList(string playerId, string name, bool bypass = false)
        {
            if (!bypass && !configData.FriendOptions.AllowCustomLists) return false;
            var user = GetUserByID(playerId);
            if (user == null) return false;
            if (!user.HasFriendList(name))
                return false;

            user.RemoveFriendList(name);
            return true;
        }
        bool UpdateFriendList(string playerId, string name, List<string> list, bool bypass = false)
        {
            if (!bypass && !configData.FriendOptions.AllowCustomLists) return false;
            var user = GetUserByID(playerId);
            if (user == null) return false;
            if (!user.HasFriendList(name))
                return false;

            user.UpdateFriendList(name, list);
            return true;
        }
        bool AddFriend(string playerId, string name, string friendId, bool bypass = false)
        {
            if (!bypass && !configData.FriendOptions.AllowCustomLists) return false;
            var user = GetUserByID(playerId);
            if (user == null) return false;
            if (!user.HasFriendList(name))
                user.AddFriendList(name, new List<string>());
            if (user.HasFriend(name, friendId))
                return true;
            user.AddFriend(name, friendId);
            return true;
        }
        bool RemoveFriend(string playerId, string name, string friendId, bool bypass = false)
        {
            if (!bypass && !configData.FriendOptions.AllowCustomLists) return false;
            var user = GetUserByID(playerId);
            if (user == null) return false;
            if (!user.HasFriendList(name))
                return false;
            if (!user.HasFriend(name, friendId))
                return true;
            user.RemoveFriend(name, friendId);
            return true;
        }
        #endregion
        #endregion

        #region External API  
        #region Friends
		//	фикс листа FriendsAPI
        List<string> GetRustFriends(ulong playerId)
        {
            var success = Friends?.Call("IsFriendOfS", playerId.ToString());
            if (success is string[])
            {
                return (success as string[]).ToList();
            }
			//	return new list
            return new List<string>();
        }
        void OnFriendAdded(object playerId, object friendId)
        {
            AddFriend(friendId.ToString(), "FriendsAPI", playerId.ToString(), true);
        }
        void OnFriendRemoved(object playerId, object friendId)
        {
            RemoveFriend(friendId.ToString(), "FriendsAPI", playerId.ToString(), true);
        }
        #endregion
        #endregion

        #region Config        
        private ConfigData configData;
        class FriendLists
        {
            public bool AllowCustomLists { get; set; }
            public bool UseFriends { get; set; }            
        }
        class MapOptions
        {
			public bool EnableCaves { get; set; }
			public bool EnablePowerlines { get; set; }
        }
        class SpamOptions
        {
            public int TimeBetweenAttempts { get; set; }
            public int WarningAttempts { get; set; }
            public int DisableAttempts { get; set; }
            public int DisableSeconds { get; set; }
            public bool Enabled { get; set; }
        }
        class ConfigData
        {
            public FriendLists FriendOptions { get; set; }
            public MapOptions MapOptions { get; set; }
            public SpamOptions SpamOptions { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                FriendOptions = new FriendLists
                {
                    AllowCustomLists = true,
                    UseFriends = true
                },
                MapOptions = new MapOptions
                {
					EnableCaves = false,
					EnablePowerlines = false
                },
                SpamOptions = new SpamOptions
                {
                    DisableAttempts = 10,
                    DisableSeconds = 120,
                    Enabled = true,
                    TimeBetweenAttempts = 3,
                    WarningAttempts = 5
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Image Storage
        private string GetImage(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return ImageLibrary.GetImage(name, 0);     
        }        
        void ValidateImages()
        {
            Puts("Проверяем изображения...");
            if (isNewSave || !ImageLibrary.HasImage("mapimage", 0))
            {
                LoadImages();
                LoadMapImage();
            }
            else GenerateMaps(true);
        }                
        
        private void LoadImages()
        {
            Puts("Загружаем иконки...");
                    
            string[] files = new string[] { "self", "friend", "bradley", "heli", "plane" };
            string path = $"{dataDirectory}icons{Path.DirectorySeparatorChar}";

            Dictionary<string, string> newLoadOrder = new Dictionary<string, string>();
            foreach (string file in files)
            {                
                for (int i = 0; i <= 360; i = i + 10)
                    newLoadOrder.Add($"{file}{i}", $"{path}{file}{i}.png");                
            }
            //  Иконки для ЛЭП
            newLoadOrder.Add("powerline", $"{path}powerline.png");
            newLoadOrder.Add("powerline_b", $"{path}powerline_b.png");
            newLoadOrder.Add("powerline_c", $"{path}powerline_c.png");
            newLoadOrder.Add("powerline_d", $"{path}powerline_d.png");
            //  Раздельные иконки для всех РТ
            newLoadOrder.Add("lighthouse", $"{path}lighthouse.png");
			newLoadOrder.Add("launchsite", $"{path}launchsite.png");
            newLoadOrder.Add("radtown", $"{path}radtown.png");
            newLoadOrder.Add("cave", $"{path}cave.png");
            newLoadOrder.Add("warehouse", $"{path}warehouse.png");
            newLoadOrder.Add("dish", $"{path}dish.png");
            newLoadOrder.Add("spheretank", $"{path}spheretank.png");
            newLoadOrder.Add("harbor", $"{path}harbor.png");
            newLoadOrder.Add("airfield", $"{path}airfield.png");
            newLoadOrder.Add("tunnel", $"{path}tunnel.png");
            newLoadOrder.Add("treatment", $"{path}treatment.png");
            newLoadOrder.Add("trainyard", $"{path}trainyard.png");
            newLoadOrder.Add("powerplant", $"{path}powerplant.png");
            newLoadOrder.Add("supply", $"{path}supply.png");
			//	Gas
			newLoadOrder.Add("gas", $"{path}gas.png");
			//	Market
			newLoadOrder.Add("market", $"{path}market.png");

            ImageLibrary.ImportImageList(Title, newLoadOrder, 0, true);
        }
        private void LoadMapImage()
        {
            Puts("Загружаем изображение карты...");
            
            //  Загружаем изображение карты из папки data/lustymap/
			//	Имя файла map.jpg
            var mapurl = dataDirectory + "map.jpg";
            
            ImageLibrary.AddImage(mapurl, "mapimage", 0);
            
            GenerateMaps(true);
        }
        #endregion

        #region Localization
        string msg(string key, string playerid = null) => lang.GetMessage(key, this, playerid);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"spamWarning", "Прекратите спам карты или она будет отключена для вас!" },
            {"spamDisable", "Вам была отключена карта на {0} сек!" },
            {"spamEnable", "Карта снова доступна!" }
        };
        #endregion
    }
}