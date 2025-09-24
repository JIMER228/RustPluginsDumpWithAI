// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;
using System.Globalization;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.IO;

namespace Oxide.Plugins
{
    [Info("TreasureChest", "S1m0n", "0.1.8")]
    class TreasureChest : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin LustyMap;
        [PluginReference] Plugin Spawns;
        [PluginReference] Plugin RandomSpawns;

        private Timer nextEvent;
        private Timer timeToUnlock;
        private Timer timeToDestroy;
        private Timer uiTimer;

        private BaseEntity container;
        private BaseEntity cupboard;

        private double nextTrigger;

        private bool isUnlocked;
        private bool isLooted;
        private bool isBlocked;

        private bool isLoaded;
        private string reason = "Loading Plugin!";

        static string treasureIcon;

        const string smokeSignal = "assets/bundled/prefabs/fx/smoke_signal_full.prefab";
        const string containerEnt = "assets/prefabs/deployable/large wood storage/box.wooden.large.prefab";
        const string cupboardEnt = "assets/prefabs/deployable/tool cupboard/cupboard.tool.deployed.prefab";
        #endregion

        #region Oxide Hooks 

        void OnServerInitialized()
        {
            lang.RegisterMessages(Messages, this);
            SubscribeToMethods(false);
            LoadVariables();
            VerifySettings();
        }
        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!isLoaded) return;
            if (entity != null && entity == container)
            {
                info.damageTypes.ScaleAll(0);
            }
        }
        void OnItemAddedToContainer(ItemContainer itemContainer, Item item)
        {
            if (!isLoaded) return;
            if (itemContainer.entityOwner == null) return;
            if (container == itemContainer.entityOwner)
            {
                if (isBlocked)
                    item.Drop(itemContainer.entityOwner.transform.position, Vector3.up);
            }
        }
        private void OnPlayerLootEnd(PlayerLoot loot)
        {
            if (!isLoaded) return;
            if (loot.entitySource != null && loot.entitySource == container)
            {
                ItemContainer itemContainer = container.GetComponent<StorageContainer>()?.inventory;
                if (itemContainer != null)
                {
                    if (!isLooted)
                    {
                        isLooted = true;
                        string playerName = loot.GetComponentInParent<BasePlayer>()?.displayName;

                        if (!string.IsNullOrEmpty(playerName))
                            PrintToChat(string.Format(msg("<color=orange>[Сундук с сокровищами] :</color> <color=#ffd479>{0}</color> забрал награду с сундука сокровищ!"), playerName));
                    }
                    if (itemContainer.itemList.Count == 0)
                    {
                        DestroyContainer();
                    }
                }
            }
        }
        object CanNetworkTo(BaseEntity entity, BasePlayer target)
        {
            if (cupboard != null && entity == cupboard)
                return false;
            return null;
        }
        void Unload()
        {
            if (nextEvent != null)
                nextEvent.Destroy();
            if (timeToUnlock != null)
                timeToUnlock.Destroy();
            if (timeToDestroy != null)
                timeToDestroy.Destroy();
            if (uiTimer != null)
                uiTimer.Destroy();

            foreach (var player in BasePlayer.activePlayerList)
                CuiHelper.DestroyUi(player, Main);

            if (container != null)
            {
                var loot = container.GetComponent<StorageContainer>()?.inventory;
                if (loot != null)
                    ClearContainer(loot);
                (container as BaseCombatEntity).DieInstantly();
            }
            if (cupboard != null)
            {
                (cupboard as BaseCombatEntity).DieInstantly();
            }
            if (configData.LustyMap.ShowOnLustyMap)
                RemoveMapMarker();
        }
        #endregion

        #region Functions
        void SubscribeToMethods(bool isSubscribing)
        {
            if (isSubscribing)
            {
                Subscribe(nameof(OnPlayerLootEnd));
                Subscribe(nameof(OnItemAddedToContainer));
                Subscribe(nameof(OnEntityTakeDamage));
                Subscribe(nameof(CanNetworkTo));
            }
            else
            {
                Unsubscribe(nameof(OnPlayerLootEnd));
                Unsubscribe(nameof(OnItemAddedToContainer));
                Unsubscribe(nameof(OnEntityTakeDamage));
                Unsubscribe(nameof(CanNetworkTo));
            }
        }
        void VerifySettings()
        {
            if (!configData.Options.UseSpawnsFromRandomSpawns)
            {
                if (!Spawns)
                {
                    reason = "Spawns Database not found!";
                    PrintError("Spawns Database not found! Can not continue");
                    return;
                }
                if (string.IsNullOrEmpty(configData.Options.LootSpawnfile))
                {
                    reason = "No spawnfile has been set in the config!";
                    PrintError("No spawnfile has been set in the config! Can not continue");
                    return;
                }
                object success = Spawns.Call("GetSpawnsCount", configData.Options.LootSpawnfile);
                if (success is string)
                {
                    reason = (string)success;
                    PrintError((string)success);
                    return;
                }
            }
            else
            {
                if (!RandomSpawns)
                {
                    reason = "RandomSpawns not found!";
                    PrintError("RandomSpawns can not be found! Can not continue");
                    return;
                }
            }
            if (configData.Options.UISettings.UseUIDisplay && !string.IsNullOrEmpty(configData.Options.UISettings.IconUrl))
                Add(configData.Options.UISettings.IconUrl);
            isLoaded = true;
            StartTimers();
        }
        void StartTimers()
        {
            var time = UnityEngine.Random.Range(configData.Timers.MinimumTimeBetweenEvents, configData.Timers.MaximumTimeBetweenEvents);
            nextTrigger = GrabCurrentTime() + time;
            nextEvent = timer.In((float)time, () => SpawnContainer());
        }
        void SpawnContainer(object spawnLoc = null)
        {
            if (BasePlayer.activePlayerList.Count >= configData.Options.MinimumPlayersRequired)
            {
                Vector3 location = Vector3.zero;

                if (spawnLoc == null)
                {
                    if (configData.Options.UseSpawnsFromRandomSpawns)
                    {
                        object success = RandomSpawns.Call("GetSpawnPoint");
                        if (success != null)
                            location = (Vector3)success;
                    }
                    else
                    {
                        object success = Spawns.Call("GetRandomSpawn", configData.Options.LootSpawnfile);
                        if (success is string)
                        {
                            PrintError((string)success);
                            return;
                        }
                        location = (Vector3)success;
                    }
                }
                else location = (Vector3)spawnLoc;

                if (location == Vector3.zero)
                {
                    PrintError("There was a error retrieving a spawn location for the treasure box!");
                    return;
                }
                SubscribeToMethods(true);

                container = GameManager.server.CreateEntity(containerEnt, location, new Quaternion(), true);
                container.skinID = configData.LootTable.ContainerSkin;
                container.Spawn();
                isBlocked = false;

                cupboard = GameManager.server.CreateEntity(cupboardEnt, location + Vector3.down, new Quaternion(), true);
                cupboard.Spawn();

                timer.In(1, () =>
                {
                    FillLootContainer(container);
                    isUnlocked = false;
                    isLooted = false;
                    NotifyEventStarted();
                });
            }
            else StartTimers();
        }
        void ClearContainer(ItemContainer itemContainer)
        {
            if (itemContainer == null || itemContainer.itemList == null) return;
            while (itemContainer.itemList.Count > 0)
            {
                var item = itemContainer.itemList[0];
                item.RemoveFromContainer();
                item.Remove(0f);
            }
        }
        private void FillLootContainer(BaseEntity entity)
        {
            if (entity == null) return;
            ItemContainer itemContainer = entity.GetComponent<StorageContainer>()?.inventory;
            LootTables lootTable = configData.LootTable;
            if (itemContainer == null || lootTable == null) return;

            int count = UnityEngine.Random.Range(lootTable.MinimumItems, lootTable.MaximumItems);

            if (itemContainer.capacity < count)
                itemContainer.capacity = count;

            List<LootItem> lootItems = new List<LootItem>(configData.LootTable.LootItems);
            for (int i = 0; i < count; i++)
            {
                var lootItem = lootItems.GetRandom();
                if (lootItem == null) continue;

                Item item = ItemManager.CreateByName(lootItem.Shortname);
                if (item != null)
                {
                    item.amount = UnityEngine.Random.Range(lootItem.MinimumAmount, lootItem.MaximumAmount);
                    item.MoveToContainer(itemContainer, -1, false);
                }
                lootItems.Remove(lootItem);
            }
            isBlocked = true;
            container.SetFlag(BaseEntity.Flags.Locked, true);
            if (configData.Options.ShowSmokeOnLocation)
                Effect.server.Run(smokeSignal, entity, 0, new Vector3(), new Vector3(), null, true);
        }

        private void NotifyEventStarted()
        {
            nextTrigger = GrabCurrentTime() + configData.Timers.TimeToUnlock;
            timeToUnlock = timer.In(configData.Timers.TimeToUnlock, UnlockContainer);
            if (configData.Options.UISettings.UseUIDisplay) RefreshAllUI();
            PrintToChat(string.Format(msg("<color=orange>[Сундук с сокровищами] :</color> Появился сундук с сокровищами. Он находится на координатах <color=#ffd479>{0}</color> и его можно открыть через <color=#ffd479>{1}</color>!"), $"X: {Math.Round(container.transform.position.x, 1)}, Z: {Math.Round(container.transform.position.z, 1)}", FormatTime(configData.Timers.TimeToUnlock)));

            if (configData.LustyMap.ShowOnLustyMap && !string.IsNullOrEmpty(configData.LustyMap.MarkerFilename))
                AddMapMarker();
        }
        private void UnlockContainer()
        {
            if (container != null)
            {
                container.SetFlag(BaseEntity.Flags.Locked, false);
                isUnlocked = true;
                nextTrigger = GrabCurrentTime() + configData.Timers.TimeToLoot;
                timeToDestroy = timer.In(configData.Timers.TimeToLoot, DestroyContainer);
                if (configData.Options.UISettings.UseUIDisplay) RefreshAllUI();
                PrintToChat(string.Format(msg("<color=orange>[Сундук с сокровищами] :</color>Сундук сокровищ был разблокирован! У вас есть <color=#ffd479>{1}</color>, чтобы забрать награду!"), FormatTime(configData.Timers.TimeToLoot)));
            }
            else StartTimers();
        }
        private void DestroyContainer()
        {
            if (container != null)
            {
                Unload();
                SubscribeToMethods(false);
                if (!isLooted)
                    PrintToChat(msg("<color=orange>[Сундук с сокровищами] :</color>Сундук сокровищ не найден во время, поэтому сундук исчез."));
                if (configData.LustyMap.ShowOnLustyMap)
                    RemoveMapMarker();
            }
            StartTimers();
        }
        private string FormatTime(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds((float)time);
            var days = dateDifference.Days;
            var hours = dateDifference.Hours;
            hours += (days * 24);
            var mins = dateDifference.Minutes;
            var secs = dateDifference.Seconds;
            if (hours > 0)
                return string.Format("{0:00}:{1:00}:{2:00}", hours, mins, secs);
            else return string.Format("{0:00}:{1:00}", mins, secs);
        }
        double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        #endregion

        #region UI
        public class UI
        {
            static public CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool useCursor = false)
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = "Hud",
                        panelName
                    }
                };
                return NewElement;
            }
            static public void AddImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax, float fadeOut = 0f)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png, Sprite = "assets/content/textures/generic/fulltransparent.tga" },
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }
            static public void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);

            }
            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.TrimStart('#');
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        #endregion

        #region UI Creation
        private const string Main = "TreasureUIMain";
        private void CreateTreasureUI(BasePlayer player)
        {
            var MainCont = UI.CreateElementContainer(Main, UI.Color(configData.Options.UISettings.UIBackgroundColor, configData.Options.UISettings.UIOpacity), $"{configData.Options.UISettings.XPosition} {configData.Options.UISettings.YPosition}", $"{configData.Options.UISettings.XPosition + configData.Options.UISettings.XDimension} {configData.Options.UISettings.YPosition + configData.Options.UISettings.YDimension}");

            if (!string.IsNullOrEmpty(treasureIcon))
                UI.AddImage(ref MainCont, Main, treasureIcon, "0.01 0.05", "0.12 0.95");
            UI.CreateLabel(ref MainCont, Main, "", string.Format(isUnlocked ? msg("<color=orange>{0}</color>  Сундук сокровищ исчезнет через <color=orange>{1}</color>", player.UserIDString) : msg("<color=orange>{0}</color>  Сундук сокровищ разблокируется через <color=orange>{1}</color>", player.UserIDString), $"X: {Math.Round(container.transform.position.x, 1)}, Z: {Math.Round(container.transform.position.z, 1)}", GetFormatTime()), 15, "0.14 0", "1 1", TextAnchor.MiddleLeft);

            CuiHelper.DestroyUi(player, Main);
            CuiHelper.AddUi(player, MainCont);
        }
        private string GetFormatTime()
        {
            var time = nextTrigger - GrabCurrentTime();
            double minutes = Math.Floor((double)(time / 60));
            time -= (int)(minutes * 60);
            return string.Format("{0:00}:{1:00}", minutes, time);
        }
        private void RefreshAllUI()
        {
            uiTimer = timer.Repeat(1, (int)(nextTrigger - GrabCurrentTime()) - 1, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (container == null) return;
                    if (!isLooted)
                        CreateTreasureUI(player);
                    else CuiHelper.DestroyUi(player, Main);
                }
            });
        }
        #endregion

        #region External Hooks
        private void AddMapMarker() => LustyMap?.Call("AddMarker", container.transform.position.x, container.transform.position.z, msg("сундук с сокровищами"), configData.LustyMap.MarkerFilename);

        private void RemoveMapMarker() => LustyMap?.Call("RemoveMarker", msg("сундук с сокровищами"));

        #endregion

        #region Commands
        [ChatCommand("th")]
        void cmdTH(BasePlayer player, string command, string[] args)
        {
            if (!isLoaded)
            {
                SendReply(player, "Плагин не загружен, потому что : " + reason);
                return;
            }
            if (args.Length == 0)
            {
                SendReply(player, $"<size=18><color=orange>{Title}</color></size>\n<color=#ffd479>/th info</color><color=white> - Показывает время, оставшееся до следующего появления сундука сокровищ, и его место положения (если включена функция)</color>");
                if (player.IsAdmin)
                {
                    SendReply(player, "<color=#ffd479>/th start</color><color=white> - Начать ивент</color>");
                    SendReply(player, "<color=#ffd479>/th starthere</color><color=white> - Начать ивент на вашей позиции</color>");
                    SendReply(player, "<color=#ffd479>/th cancel</color><color=white> - Отменить текущий ивент</color>");
                }
            }
            else
            {
                switch (args[0].ToLower())
                {
                    case "info":
                        string time = FormatTime(nextTrigger - GrabCurrentTime());
                        if (container != null)
                        {
                            if (isUnlocked)
                                SendReply(player, msg("Сундук сокровищ разблокирован!", player.UserIDString));
                            else SendReply(player, string.Format(msg("Сундук разблокируется через : <color=#ffd479>{0}</color>", player.UserIDString), time));
                            SendReply(player, string.Format(msg("Сундук можно найти на координатах : <color=#ffd479>{0}</color>", player.UserIDString), container.transform.position));
                        }
                        else SendReply(player, string.Format(msg("<color=orange>[Сундук с сокровищами] :</color> Следующий сундук появится через : <color=#ffd479>{0}</color>", player.UserIDString), time));
                        return;
                    case "start":
                        if (!player.IsAdmin) return;
                        if (container != null)
                        {
                            SendReply(player, "Ивент уже начат!");
                            return;
                        }
                        else
                        {
                            nextEvent.Destroy();
                            SpawnContainer();
                            SendReply(player, "Вы начали ивент!");
                        }
                        return;
                    case "starthere":
                        if (!player.IsAdmin) return;
                        if (container != null)
                        {
                            SendReply(player, "Ивент уже начат!");
                            return;
                        }
                        else
                        {
                            nextEvent.Destroy();
                            SpawnContainer(player.transform.position);
                            SendReply(player, "Вы начали ивент на вашем местоположении!");
                        }
                        return;
                    case "cancel":
                        if (!player.IsAdmin) return;
                        if (container == null)
                        {
                            SendReply(player, "В настоящее время ивент не начат!");
                            return;
                        }
                        else
                        {
                            Unload();
                            StartTimers();
                            SendReply(player, "Вы отменили ивент");
                        }
                        return;
                    default:
                        SendReply(player, "Неверная команда!");
                        break;
                }
            }
        }
        [ConsoleCommand("th")]
        void ccmdTH(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            if (!isLoaded)
            {
                SendReply(arg, "Плагин не загружен, потому что : " + reason);
                return;
            }
            if (arg.Args.Length == 0)
            {
                SendReply(arg, $"- {Title}  v.{Version}");
                SendReply(arg, "th start - Начать ивент");
                SendReply(arg, "th cancel - Отменить текущий ивент");
                SendReply(arg, "th clearicon - Очищает любые значки связанные с сундуком сокровищ");
            }
            else
            {
                switch (arg.Args[0].ToLower())
                {
                    case "start":
                        if (container != null)
                        {
                            SendReply(arg, "Ивент уже начат!");
                            return;
                        }
                        else
                        {
                            nextEvent.Destroy();
                            SpawnContainer();
                            SendReply(arg, "Вы начали ивент!");
                        }
                        return;
                    case "cancel":
                        if (container == null)
                        {
                            SendReply(arg, "В настоящее время ивент не начат!");
                            return;
                        }
                        else
                        {
                            Unload();
                            StartTimers();
                            SendReply(arg, "Вы отменили ивент!");
                        }
                        return;
                    case "clearicon":
                        RemoveMapMarker();
                        return;
                    default:
                        SendReply(arg, "Неверная команда");
                        break;
                }
            }
        }
        #endregion

        #region Imagery
        private MemoryStream stream = new MemoryStream();
        private WWW info;
        public void Add(string url)
        {
            info = new WWW(url);
            TryDownloadImage();
        }
        void TryDownloadImage()
        {
            if (!info.isDone)
            {
                timer.In(1, TryDownloadImage);
                return;
            }
            if (!string.IsNullOrEmpty(info.error))
            {
                PrintError(string.Format("Failed to load the Treasure Icon! Error: {0}", info.error));
                return;
            }
            else
            {
                stream.Position = 0;
                stream.SetLength(0);
                stream.Write(info.bytes, 0, info.bytes.Length);
                treasureIcon = FileStorage.server.Store(info.bytes, FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString();
                stream = null;
            }
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class LootTables
        {
            public ulong ContainerSkin { get; set; }
            public int MinimumItems { get; set; }
            public int MaximumItems { get; set; }
            public List<LootItem> LootItems { get; set; }
        }
        class LootItem
        {
            public string Shortname { get; set; }
            public int MinimumAmount { get; set; }
            public int MaximumAmount { get; set; }
        }
        class EventTimers
        {
            public int MinimumTimeBetweenEvents { get; set; }
            public int MaximumTimeBetweenEvents { get; set; }
            public int TimeToUnlock { get; set; }
            public int TimeToLoot { get; set; }
        }
        class LMIntegration
        {
            public bool ShowOnLustyMap { get; set; }
            public string MarkerFilename { get; set; }
        }
        class Options
        {
            public float BuildBlockedRadius { get; set; }
            public bool UseSpawnsFromRandomSpawns { get; set; }
            public string LootSpawnfile { get; set; }
            public int MinimumPlayersRequired { get; set; }
            public bool ShowSmokeOnLocation { get; set; }
            public UIOptions UISettings { get; set; }
        }
        class UIOptions
        {
            public bool UseUIDisplay { get; set; }
            public string IconUrl { get; set; }
            public float XPosition { get; set; }
            public float YPosition { get; set; }
            public float XDimension { get; set; }
            public float YDimension { get; set; }
            public string UIBackgroundColor { get; set; }
            public float UIOpacity { get; set; }
        }
        class ConfigData
        {
            public EventTimers Timers { get; set; }
            public Options Options { get; set; }
            public LootTables LootTable { get; set; }
            public LMIntegration LustyMap { get; set; }
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
                LustyMap = new LMIntegration
                {
                    MarkerFilename = "http://www.chaoscode.io/oxide/Images/treasureicon.png",
                    ShowOnLustyMap = true
                },
                LootTable = new LootTables
                {
                    ContainerSkin = 0,
                    MaximumItems = 4,
                    MinimumItems = 1,
                    LootItems = new List<LootItem>
                        {
                            new LootItem {Shortname = "metal.refined", MaximumAmount = 100, MinimumAmount = 10 },
                            new LootItem {Shortname = "explosive.timed", MaximumAmount = 2, MinimumAmount = 1 },
                            new LootItem {Shortname = "grenade.f1", MaximumAmount = 3, MinimumAmount = 1 },
                            new LootItem {Shortname = "supply.signal", MaximumAmount = 1, MinimumAmount = 1 },
                            new LootItem {Shortname = "cctv.camera", MaximumAmount = 1, MinimumAmount = 1 },
                            new LootItem {Shortname = "targeting.computer", MaximumAmount = 1, MinimumAmount = 1 },
                            new LootItem {Shortname = "ammo.rifle", MaximumAmount = 60, MinimumAmount = 20 },
                            new LootItem {Shortname = "ammo.pistol", MaximumAmount = 60, MinimumAmount = 20 }
                        }
                },
                Options = new Options
                {
                    BuildBlockedRadius = 10,
                    LootSpawnfile = "",
                    MinimumPlayersRequired = 1,
                    ShowSmokeOnLocation = true,
                    UISettings = new UIOptions
                    {
                        IconUrl = "http://www.chaoscode.io/oxide/Images/treasureicon.png",
                        UIBackgroundColor = "#4C4C4C",
                        UseUIDisplay = true,
                        XDimension = 0.3f,
                        XPosition = 0.66f,
                        YDimension = 0.05f,
                        YPosition = 0.93f,
                        UIOpacity = 0.7f
                    },
                    UseSpawnsFromRandomSpawns = true
                },
                Timers = new EventTimers
                {
                    MaximumTimeBetweenEvents = 1200,
                    MinimumTimeBetweenEvents = 600,
                    TimeToLoot = 300,
                    TimeToUnlock = 179
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Localization
        string msg(string key, string playerId = "") => lang.GetMessage(key, this, playerId);
        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            {"eventStart", "<color=orange>[Сундук с сокровищами] :</color> Появился сундук с сокровищами. Он находится на координатах <color=#ffd479>{0}</color> и его можно открыть через <color=#ffd479>{1}</color>!" },
            {"nextDrop", "<color=orange>[Сундук с сокровищами] :</color> Следующий сундук появится через : <color=#ffd479>{0}</color>" },
            {"nextUnlock", "Сундук разблокируется через : <color=#ffd479>{0}</color>" },
            {"isUnlocked", "Сундук сокровищ разблокирован!" },
            {"currentPos", "Сундук можно найти на координатах : <color=#ffd479>{0}</color>" },
            {"eventWin", "<color=orange>[Сундук с сокровищами] : </color><color=#ffd479>{0}</color> забрал награду с сундука сокровищ!" },
            {"eventLose", "<color=orange>[Сундук с сокровищами] :</color>Сундук сокровищ не найден во время, поэтому сундук исчез." },
            {"containerUnlock", "<color=orange>[Сундук с сокровищами] :</color>Сундук сокровищ был разблокирован! У вас есть <color=#ffd479>{1}</color>, чтобы забрать награду!" },
            {"unlocksIn", "<color=#ffd479>{0}</color> - Сундук сокровищ разблокируется через : <color=#ffd479>{1}</color>" },
            {"despawnsIn", "<color=#ffd479>{0}</color> - Сундук сокровищ исчезнет через : <color=#ffd479>{1}</color>" },
            {"iconName", "сундук с сокровищами" }
        };
        #endregion

    }
}
