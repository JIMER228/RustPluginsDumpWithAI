// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Minimalist Hud", "yun", "1.1.2")]
    [Description("A beautiful and minimalist hud")]

    class MinimalistHud : RustPlugin
    {
        #region Fields

        [PluginReference]
        private readonly Plugin ImageLibrary, Economics, IQEconomic;

        private Timer refreshTimeTimer;
        private ActiveEvents activeEvents = new ActiveEvents();

        private class ActiveEvents
        {
            public List<CargoPlane> cargoplanes = new List<CargoPlane>();
            public List<BradleyAPC> bradleyAPC = new List<BradleyAPC>();
            public List<PatrolHelicopter> helicopters = new List<PatrolHelicopter>();
            public List<CH47Helicopter> ch47 = new List<CH47Helicopter>();
            public List<CargoShip> cargoships = new List<CargoShip>();
            public List<HackableLockedCrate> largeOilrigCrates = new List<HackableLockedCrate>();
            public List<HackableLockedCrate> smallOilrigCrates = new List<HackableLockedCrate>();

            public bool excavator = false;
        }

        private enum EventType
        {
            CargoPlane,
            BradleyAPC,
            Helicopter,
            CH47,
            CargoShip,
            Excavator,
            LargeOilrig,
            SmallOilrig
        };

        private Vector3 largeOilrigPos;
        private Vector3 smallOilrigPos;

        #endregion Fields

        #region Initialization

        private const string permissionToggleUI = "minimalisthud.toggle";

        private void Init()
        {
            foreach (var command in config.toggleUICommands)
                cmd.AddChatCommand(command, this, nameof(ToggleUICommand));

            permission.RegisterPermission(permissionToggleUI, this);

            if (!config.events.largeOilrig.enabled && !config.events.smallOilrig.enabled)
            {
                Unsubscribe(nameof(OnCrateHack));
                Unsubscribe(nameof(OnCrateHackEnd));
            }

            if (!config.events.excavator.enabled)
                Unsubscribe(nameof(OnExcavatorMiningToggled));
        }

        #endregion Initialization

        #region uMod Hooks

        private void OnServerInitialized()
        {
            LoadData();

            if (!ImageLibrary)
            {
                Interface.Oxide.LogError("The 'ImageLibrary' plugin is necessary for the plugin to work; Download it and try reloading the plugin again.\nDownload it at: https://umod.org/plugins/image-library");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }

            LoadAllImages();

            foreach (MonumentInfo monument in TerrainMeta.Path.Monuments)
            {
                if (monument.name == "OilrigAI2")
                    largeOilrigPos = monument.transform.position;
                else if (monument.name == "OilrigAI")
                    smallOilrigPos = monument.transform.position;
            }

            activeEvents.cargoplanes = BaseNetworkable.serverEntities.OfType<CargoPlane>().ToList();
            activeEvents.bradleyAPC = BaseNetworkable.serverEntities.OfType<BradleyAPC>().ToList();
            activeEvents.helicopters = BaseNetworkable.serverEntities.OfType<PatrolHelicopter>().ToList();
            activeEvents.ch47 = BaseNetworkable.serverEntities.OfType<CH47Helicopter>().ToList();
            activeEvents.cargoships = BaseNetworkable.serverEntities.OfType<CargoShip>().ToList();
            activeEvents.excavator = BaseNetworkable.serverEntities.OfType<ExcavatorArm>().FirstOrDefault((x) => x.IsMining()) != null;

            foreach (HackableLockedCrate crate in BaseNetworkable.serverEntities.OfType<HackableLockedCrate>())
            {
                if (crate.IsBeingHacked())
                {
                    if (crate.Distance(largeOilrigPos) < 50f)
                    {
                        activeEvents.largeOilrigCrates.Add(crate);
                    }
                    else if (crate.Distance(smallOilrigPos) < 50f)
                    {
                        activeEvents.smallOilrigCrates.Add(crate);
                    }
                }
            }

            NextTick(() => InitializePlayersUI());
        }

        private void Unload()
        {
            timer.Destroy(ref refreshTimeTimer);

            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, WrapperPanelName);
                CuiHelper.DestroyUi(player, MinimizeAndMaximizeButtonName);
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            NextTick(() =>
            {
                if (config.info.users.enabled)
                {
                    foreach (var p in BasePlayer.activePlayerList)
                        UpdateConnectedPlayersPanel(p);
                }

                if (config.info.sleepers.enabled)
                {
                    foreach (var p in BasePlayer.activePlayerList)
                        UpdateSleepersPanel(p);
                }

                LoadPlayerUI(player);
            });
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            NextTick(() =>
            {
                if (config.info.users.enabled)
                {
                    foreach (var p in BasePlayer.activePlayerList)
                        UpdateConnectedPlayersPanel(p);
                }

                if (config.info.sleepers.enabled)
                {
                    foreach (var p in BasePlayer.activePlayerList)
                        UpdateSleepersPanel(p);
                }
            });
        }

        private void OnEntitySpawned(BaseEntity entity)
        {
            EventType eventType;

            if (entity is CargoPlane && config.events.cargoplane.enabled)
            {
                activeEvents.cargoplanes.Add(entity as CargoPlane);
                eventType = EventType.CargoPlane;
            }
            else if (entity is BradleyAPC && config.events.bradley.enabled)
            {
                activeEvents.bradleyAPC.Add(entity as BradleyAPC);
                eventType = EventType.BradleyAPC;
            }
            else if (entity is PatrolHelicopter && config.events.helicopter.enabled)
            {
                activeEvents.helicopters.Add(entity as PatrolHelicopter);
                eventType = EventType.Helicopter;
            }
            else if (entity is CH47Helicopter && config.events.ch47.enabled)
            {
                activeEvents.ch47.Add(entity as CH47Helicopter);
                eventType = EventType.CH47;
            }
            else if (entity is CargoShip && config.events.cargoship.enabled)
            {
                activeEvents.cargoships.Add(entity as CargoShip);
                eventType = EventType.CargoShip;
            }
            else return;

            UpdatePlayersEventPanel(eventType);
        }

        private void OnEntityKill(BaseEntity entity)
        {
            EventType eventType;

            if (entity is CargoPlane && config.events.cargoplane.enabled)
            {
                activeEvents.cargoplanes.Remove(entity as CargoPlane);
                eventType = EventType.CargoPlane;
            }
            else if (entity is BradleyAPC && config.events.bradley.enabled)
            {
                activeEvents.bradleyAPC.Remove(entity as BradleyAPC);
                eventType = EventType.BradleyAPC;
            }
            else if (entity is PatrolHelicopter && config.events.helicopter.enabled)
            {
                activeEvents.helicopters.Remove(entity as PatrolHelicopter);
                eventType = EventType.Helicopter;
            }
            else if (entity is CH47Helicopter && config.events.ch47.enabled)
            {
                activeEvents.ch47.Remove(entity as CH47Helicopter);
                eventType = EventType.CH47;
            }
            else if (entity is CargoShip && config.events.cargoship.enabled)
            {
                activeEvents.cargoships.Remove(entity as CargoShip);
                eventType = EventType.CargoShip;
            }
            else if (entity is ExcavatorArm && config.events.excavator.enabled)
            {
                activeEvents.excavator = false;
                eventType = EventType.Excavator;
            }
            else if (entity is HackableLockedCrate)
            {
                if (config.events.largeOilrig.enabled && activeEvents.largeOilrigCrates.Contains(entity as HackableLockedCrate))
                {
                    activeEvents.largeOilrigCrates.Remove(entity as HackableLockedCrate);
                    eventType = EventType.LargeOilrig;
                }
                else if (config.events.smallOilrig.enabled && activeEvents.smallOilrigCrates.Contains(entity as HackableLockedCrate))
                {
                    activeEvents.smallOilrigCrates.Remove(entity as HackableLockedCrate);
                    eventType = EventType.SmallOilrig;
                }
                else return;
            }
            else return;

            UpdatePlayersEventPanel(eventType);
        }

        private void OnExcavatorMiningToggled(ExcavatorArm arm)
        {
            activeEvents.excavator = arm.IsMining();
            UpdatePlayersEventPanel(EventType.Excavator);
        }

        private void OnCrateHack(HackableLockedCrate crate)
        {
            if (crate.Distance(largeOilrigPos) < 50f)
            {
                activeEvents.largeOilrigCrates.Add(crate);
                UpdatePlayersEventPanel(EventType.LargeOilrig);
            }
            else if (crate.Distance(smallOilrigPos) < 50f)
            {
                activeEvents.smallOilrigCrates.Add(crate);
                UpdatePlayersEventPanel(EventType.SmallOilrig);
            }
        }

        private void OnCrateHackEnd(HackableLockedCrate crate)
        {
            if (activeEvents.largeOilrigCrates.Contains(crate))
            {
                activeEvents.largeOilrigCrates.Remove(crate);
                UpdatePlayersEventPanel(EventType.LargeOilrig);
            }
            else if (activeEvents.smallOilrigCrates.Contains(crate))
            {
                activeEvents.smallOilrigCrates.Remove(crate);
                UpdatePlayersEventPanel(EventType.SmallOilrig);
            }
        }

        // Economics
        private void OnEconomicsBalanceUpdated(string playerId, double amount)
        {
            if (!config.misc.balance.enabled)
                return;

            var player = BasePlayer.FindByID(ulong.Parse(playerId));
            if (player != null)
            {
                if (IsHidden(player))
                    return;

                UpdateBalancePanel(player);
            }
        }

        #endregion uMod Hooks

        #region Commands

        private void ToggleUICommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, permissionToggleUI))
            {
                SendMessage(player, "NoPermission");
                return;
            }

            if (!playersData.ContainsKey(player.userID))
                playersData.Add(player.userID, new PlayerData());

            var playerData = playersData[player.userID];
            playerData.Hidden = !playerData.Hidden;
            SaveData();

            if (!playerData.Hidden)
            {
                LoadPlayerUI(player);
            }
            else
            {
                CuiHelper.DestroyUi(player, WrapperPanelName);
            }

            SendMessage(player, $"UI{(!playerData.Hidden ? "Enabled" : "Disabled")}");
        }

        [ConsoleCommand("minimalisthud.ui")]
        private void MinimalistHudUIConsoleCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg?.Player();
            if (player == null || !arg.HasArgs(1))
                return;

            string cmd = arg.Args[0];
            if (cmd == "resize")
            {
                if (!config.canMinimize) return;

                if (!playersData.ContainsKey(player.userID))
                    playersData.Add(player.userID, new PlayerData());

                playersData[player.userID].Minimized = !playersData[player.userID].Minimized;
                SaveData();

                if (playersData[player.userID].Minimized)
                {
                    CuiHelper.DestroyUi(player, WrapperPanelName);
                    UpdateMinimizeAndMaximizeButton(player);
                }
                else
                {
                    LoadPlayerUI(player);
                }
            }
        }

        #endregion Commands

        #region UI Methods

        private const string WrapperPanelName = "MH_WrapperPanel";
        private const string CargoPlanePanelName = "MH_CargoPlanePanel";
        private const string BradleyAPCPanelName = "MH_BradleyAPCPanel";
        private const string HelicopterPanelName = "MH_HelicopterPanel";
        private const string CH47PanelName = "MH_CH47Panel";
        private const string CargoShipPanelName = "MH_CargoShipPanel";
        private const string ExcavatorPanelName = "MH_ExcavatorPanel";
        private const string LargeOilrigPanelName = "MH_LargeOilrigPanel";
        private const string SmallOilrigPanelName = "MH_SmallOilrigPanel";
        private const string ConnectedPlayersPanelName = "MH_ConnectedPlayersPanel";
        private const string SleepersPanelName = "MH_SleepersPanel";
        private const string TimePanelName = "MH_TimePanel";
        private const string BalancePanelName = "MH_BalancePanel";
        private const string MinimizeAndMaximizeButtonName = "MH_MinimizeAndMaximizeButton";

        private void CreateWrapperPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();
            UIBuilder.CreatePanel(ref container, "Overlay", WrapperPanelName, "0 0 0 0", config.anchor, config.offset, WrapperPanelName);
            CuiHelper.AddUi(player, container);

            if (config.canMinimize)
            {
                NextTick(() =>
                {
                    UpdateMinimizeAndMaximizeButton(player);
                });
            }
        }

        private void UpdateMinimizeAndMaximizeButton(BasePlayer player)
        {
            if (IsHidden(player)) return;

            var isMinimized = IsMinimized(player);
            var settings = isMinimized ? config.minimizeAndMaximizeButtons.Maximize : config.minimizeAndMaximizeButtons.Minimize;

            var container = new CuiElementContainer();
            UIBuilder.CreatePanel(ref container, "Overlay", MinimizeAndMaximizeButtonName, settings.backgroundColor, settings.anchor, settings.offset, MinimizeAndMaximizeButtonName);
            UIBuilder.CreateImage(ref container, MinimizeAndMaximizeButtonName, null, GetImage(settings.iconUrl), settings.iconColor, "0.5 0.5 0.5 0.5", $"-{settings.iconSize / 2} -{settings.iconSize / 2} {settings.iconSize / 2} {settings.iconSize / 2}");

            container.Add(
                new CuiButton
                {
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Button = { Color = "0 0 0 0", Command = "minimalisthud.ui resize" }
                },
                MinimizeAndMaximizeButtonName
            );

            CuiHelper.AddUi(player, container);
        }

        private void UpdateCargoPlanePanel(BasePlayer player)
        {
            if (!CanUpdateUI(player)) return;
            var container = new CuiElementContainer();

            var isActive = activeEvents.cargoplanes.Count > 0;
            var settings = config.events.cargoplane;

            var backgroundColor = isActive
                ? settings.active?.backgroundColor ?? settings.backgroundColor
                : settings.backgroundColor;

            var iconColor = isActive
                ? settings.active?.iconColor ?? settings.iconColor
                : settings.iconColor;

            UIBuilder.CreatePanel(ref container, WrapperPanelName, CargoPlanePanelName, backgroundColor, settings.anchor, settings.offset, CargoPlanePanelName);
            UIBuilder.CreateImage(ref container, CargoPlanePanelName, null, GetImage(settings.iconUrl), iconColor, "0 0 1 1", "1 1 -1 -1");

            CuiHelper.AddUi(player, container);
        }

        private void UpdateBradleyAPCPanel(BasePlayer player)
        {
            if (!CanUpdateUI(player)) return;
            var container = new CuiElementContainer();

            var isActive = activeEvents.bradleyAPC.Count > 0;
            var settings = config.events.bradley;

            var backgroundColor = isActive
                ? settings.active?.backgroundColor ?? settings.backgroundColor
                : settings.backgroundColor;

            var iconColor = isActive
                ? settings.active?.iconColor ?? settings.iconColor
                : settings.iconColor;

            UIBuilder.CreatePanel(ref container, WrapperPanelName, BradleyAPCPanelName, backgroundColor, settings.anchor, settings.offset, BradleyAPCPanelName);
            UIBuilder.CreateImage(ref container, BradleyAPCPanelName, null, GetImage(settings.iconUrl), iconColor, "0 0 1 1", "1 1 -1 -1");

            CuiHelper.AddUi(player, container);
        }

        private void UpdateHelicopterPanel(BasePlayer player)
        {
            if (!CanUpdateUI(player)) return;
            var container = new CuiElementContainer();

            var isActive = activeEvents.helicopters.Count > 0;
            var settings = config.events.helicopter;

            var backgroundColor = isActive
                ? settings.active?.backgroundColor ?? settings.backgroundColor
                : settings.backgroundColor;

            var iconColor = isActive
                ? settings.active?.iconColor ?? settings.iconColor
                : settings.iconColor;

            UIBuilder.CreatePanel(ref container, WrapperPanelName, HelicopterPanelName, backgroundColor, settings.anchor, settings.offset, HelicopterPanelName);
            UIBuilder.CreateImage(ref container, HelicopterPanelName, null, GetImage(settings.iconUrl), iconColor, "0 0 1 1", "1 1 -1 -1");

            CuiHelper.AddUi(player, container);
        }

        private void UpdateCH47Panel(BasePlayer player)
        {
            if (!CanUpdateUI(player)) return;
            var container = new CuiElementContainer();

            var isActive = activeEvents.ch47.Count > 0;
            var settings = config.events.ch47;

            var backgroundColor = isActive
                ? settings.active?.backgroundColor ?? settings.backgroundColor
                : settings.backgroundColor;

            var iconColor = isActive
                ? settings.active?.iconColor ?? settings.iconColor
                : settings.iconColor;

            UIBuilder.CreatePanel(ref container, WrapperPanelName, CH47PanelName, backgroundColor, settings.anchor, settings.offset, CH47PanelName);
            UIBuilder.CreateImage(ref container, CH47PanelName, null, GetImage(settings.iconUrl), iconColor, "0 0 1 1", "1 1 -1 -1");

            CuiHelper.AddUi(player, container);
        }

        private void UpdateCargoShipPanel(BasePlayer player)
        {
            if (!CanUpdateUI(player)) return;
            var container = new CuiElementContainer();

            var isActive = activeEvents.cargoships.Count > 0;
            var settings = config.events.cargoship;

            var backgroundColor = isActive
                ? settings.active?.backgroundColor ?? settings.backgroundColor
                : settings.backgroundColor;

            var iconColor = isActive
                ? settings.active?.iconColor ?? settings.iconColor
                : settings.iconColor;

            UIBuilder.CreatePanel(ref container, WrapperPanelName, CargoShipPanelName, backgroundColor, settings.anchor, settings.offset, CargoShipPanelName);
            UIBuilder.CreateImage(ref container, CargoShipPanelName, null, GetImage(settings.iconUrl), iconColor, "0 0 1 1", "1 1 -1 -1");

            CuiHelper.AddUi(player, container);
        }

        private void UpdateExcavatorPanel(BasePlayer player)
        {
            if (!CanUpdateUI(player)) return;
            var container = new CuiElementContainer();

            var isActive = activeEvents.excavator;
            var settings = config.events.excavator;

            var backgroundColor = isActive
                ? settings.active?.backgroundColor ?? settings.backgroundColor
                : settings.backgroundColor;

            var iconColor = isActive
                ? settings.active?.iconColor ?? settings.iconColor
                : settings.iconColor;

            UIBuilder.CreatePanel(ref container, WrapperPanelName, ExcavatorPanelName, backgroundColor, settings.anchor, settings.offset, ExcavatorPanelName);
            UIBuilder.CreateImage(ref container, ExcavatorPanelName, null, GetImage(settings.iconUrl), iconColor, "0 0 1 1", "1 1 -1 -1");

            CuiHelper.AddUi(player, container);
        }

        private void UpdateLargeOilrigPanel(BasePlayer player)
        {
            if (!CanUpdateUI(player)) return;
            var container = new CuiElementContainer();

            var isActive = activeEvents.largeOilrigCrates.Count > 0;
            var settings = config.events.largeOilrig;

            var backgroundColor = isActive
                ? settings.active?.backgroundColor ?? settings.backgroundColor
                : settings.backgroundColor;

            var iconColor = isActive
                ? settings.active?.iconColor ?? settings.iconColor
                : settings.iconColor;

            UIBuilder.CreatePanel(ref container, WrapperPanelName, LargeOilrigPanelName, backgroundColor, settings.anchor, settings.offset, LargeOilrigPanelName);
            UIBuilder.CreateImage(ref container, LargeOilrigPanelName, null, GetImage(settings.iconUrl), iconColor, "0 0 1 1", "1 1 -1 -1");

            CuiHelper.AddUi(player, container);
        }

        private void UpdateSmallOilrigPanel(BasePlayer player)
        {
            if (!CanUpdateUI(player)) return;
            var container = new CuiElementContainer();

            var isActive = activeEvents.smallOilrigCrates.Count > 0;
            var settings = config.events.smallOilrig;

            var backgroundColor = isActive
                ? settings.active?.backgroundColor ?? settings.backgroundColor
                : settings.backgroundColor;

            var iconColor = isActive
                ? settings.active?.iconColor ?? settings.iconColor
                : settings.iconColor;

            UIBuilder.CreatePanel(ref container, WrapperPanelName, SmallOilrigPanelName, backgroundColor, settings.anchor, settings.offset, SmallOilrigPanelName);
            UIBuilder.CreateImage(ref container, SmallOilrigPanelName, null, GetImage(settings.iconUrl), iconColor, "0 0 1 1", "1 1 -1 -1");

            CuiHelper.AddUi(player, container);
        }

        private void UpdateConnectedPlayersPanel(BasePlayer player)
        {
            if (!CanUpdateUI(player)) return;
            var container = new CuiElementContainer();
            var settings = config.info.users;
            var connected = BasePlayer.activePlayerList.Count;

            UIBuilder.CreatePanel(ref container, WrapperPanelName, ConnectedPlayersPanelName, settings.backgroundColor, settings.anchor, settings.offset, ConnectedPlayersPanelName);
            UIBuilder.CreateImage(ref container, ConnectedPlayersPanelName, null, GetImage(settings.iconUrl), settings.iconColor, "0 0 0 1", "1 1 23 -1");
            UIBuilder.CreateLabel(ref container, ConnectedPlayersPanelName, null, $"{connected}", settings.textColor, settings.fontSize, "0 0 1 1", "23 0 -4 0", TextAnchor.MiddleCenter);

            CuiHelper.AddUi(player, container);
        }

        private void UpdateSleepersPanel(BasePlayer player)
        {
            if (!CanUpdateUI(player)) return;
            var container = new CuiElementContainer();
            var settings = config.info.sleepers;
            var sleeping = BasePlayer.sleepingPlayerList.Count;

            UIBuilder.CreatePanel(ref container, WrapperPanelName, SleepersPanelName, settings.backgroundColor, settings.anchor, settings.offset, SleepersPanelName);
            UIBuilder.CreateImage(ref container, SleepersPanelName, null, GetImage(settings.iconUrl), settings.iconColor, "0 0 0 1", "1 1 23 -1");
            UIBuilder.CreateLabel(ref container, SleepersPanelName, null, $"{sleeping}", settings.textColor, settings.fontSize, "0 0 1 1", "23 0 -4 0", TextAnchor.MiddleCenter);

            CuiHelper.AddUi(player, container);
        }

        private void UpdateTimePanel(BasePlayer player)
        {
            if (!CanUpdateUI(player)) return;
            var container = new CuiElementContainer();
            var settings = config.info.time;

            var date = TOD_Sky.Instance.Cycle.DateTime;
            var time = date.ToString("HH:mm tt");

            var theme = (date.Hour >= 6 && date.Hour < 19) ? settings.day : settings.night;
            var iconUrl = theme?.iconUrl ?? settings.iconUrl;
            var iconColor = theme?.iconColor ?? settings.iconColor;
            var backgroundColor = theme?.backgroundColor ?? settings.backgroundColor;
            var textColor = theme?.textColor ?? settings.textColor;

            UIBuilder.CreatePanel(ref container, WrapperPanelName, TimePanelName, backgroundColor, settings.anchor, settings.offset, TimePanelName);
            UIBuilder.CreateImage(ref container, TimePanelName, null, GetImage(iconUrl), iconColor, "0 0 0 1", "1 1 23 -1");
            UIBuilder.CreateLabel(ref container, TimePanelName, null, time, textColor, settings.fontSize, "0 0 1 1", "23 0 -4 0", TextAnchor.MiddleCenter);

            CuiHelper.AddUi(player, container);
        }

        private void UpdateBalancePanel(BasePlayer player)
        {
            if (!CanUpdateUI(player)) return;
            var container = new CuiElementContainer();
            var settings = config.misc.balance;

            double balance = GetBalance(player);

            UIBuilder.CreatePanel(ref container, WrapperPanelName, BalancePanelName, settings.backgroundColor, settings.anchor, settings.offset, BalancePanelName);
            UIBuilder.CreateImage(ref container, BalancePanelName, null, GetImage(settings.iconUrl), settings.iconColor, "0 0 0 1", "1 1 23 -1");
            UIBuilder.CreateLabel(ref container, BalancePanelName, null, string.Format("{0:C}", balance).Substring(1), settings.textColor, settings.fontSize, "0 0 1 1", "23 0 -4 0", TextAnchor.MiddleCenter);

            CuiHelper.AddUi(player, container);
        }

        private bool CanUpdateUI(BasePlayer player)
        {
            return !IsHidden(player) && !IsMinimized(player);
        }

        #endregion UI Methods

        #region UI Builder

        private class UIBuilder
        {
            public static void CreatePanel(ref CuiElementContainer container, string parent, string name, string color, string anchor, string offset, string destroyUi = null)
            {
                var dimensions = ParseDimensions(anchor, offset);

                container.Add(
                    new CuiPanel
                    {
                        RectTransform = {
                            AnchorMin = dimensions[0],
                            AnchorMax = dimensions[1],
                            OffsetMin = dimensions[2],
                            OffsetMax = dimensions[3],
                        },
                        Image = {
                            Color = color,
                            Material = "assets/icons/greyout.mat",
                        }
                    },
                    parent,
                    name,
                    destroyUi
                );
            }

            public static void CreateImage(ref CuiElementContainer container, string parent, string name, string image, string color, string anchor, string offset)
            {
                var dimensions = ParseDimensions(anchor, offset);

                uint _value;
                var isPng = uint.TryParse(image, out _value);

                container.Add(
                    new CuiElement
                    {
                        Parent = parent,
                        Name = name ?? CuiHelper.GetGuid(),
                        Components = {
                            new CuiRectTransformComponent {
                                AnchorMin = dimensions[0],
                                AnchorMax = dimensions[1],
                                OffsetMin = dimensions[2],
                                OffsetMax = dimensions[3],
                            },
                            new CuiRawImageComponent {
                                Png = isPng ? image : null,
                                Url = !isPng ? image : null,
                                Color = color
                            }
                        }
                    }
                );
            }

            public static void CreateLabel(ref CuiElementContainer container, string parent, string name, string text, string color, int fontSize, string anchor, string offset, TextAnchor align = TextAnchor.MiddleLeft)
            {
                var dimensions = ParseDimensions(anchor, offset);

                container.Add(
                    new CuiLabel
                    {
                        RectTransform = {
                            AnchorMin = dimensions[0],
                            AnchorMax = dimensions[1],
                            OffsetMin = dimensions[2],
                            OffsetMax = dimensions[3],
                        },
                        Text = {
                            Text = text,
                            Color = color,
                            Align = align,
                            FontSize = fontSize
                        }
                    },
                    parent,
                    name
                );
            }

            private static string[] ParseDimensions(string anchor, string offset)
            {
                var anchors = anchor.Split(' ');
                string anchorMin = string.Join(" ", anchors.Take(2)),
                    anchorMax = string.Join(" ", anchors.Skip(2));

                var offsets = offset.Split(' ');
                string offsetMin = string.Join(" ", offsets.Take(2)),
                    offsetMax = string.Join(" ", offsets.Skip(2));

                return new string[] {
                    anchorMin,
                    anchorMax,
                    offsetMin,
                    offsetMax
                };
            }
        }

        #endregion UI Builder

        #region Helpers

        private void InitializePlayersUI()
        {
            foreach (var player in BasePlayer.activePlayerList)
                LoadPlayerUI(player);

            if (config.info.time.enabled)
            {
                refreshTimeTimer = timer.Every(config.info.time.refreshInterval, () =>
                {
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        if (IsHidden(player))
                            continue;

                        UpdateTimePanel(player);
                    }
                });
            }
        }

        private void LoadPlayerUI(BasePlayer player)
        {
            if (IsHidden(player)) return;

            CreateWrapperPanel(player);

            if (config.events.cargoplane.enabled)
                UpdateCargoPlanePanel(player);
            if (config.events.bradley.enabled)
                UpdateBradleyAPCPanel(player);
            if (config.events.helicopter.enabled)
                UpdateHelicopterPanel(player);
            if (config.events.ch47.enabled)
                UpdateCH47Panel(player);
            if (config.events.cargoship.enabled)
                UpdateCargoShipPanel(player);
            if (config.events.excavator.enabled)
                UpdateExcavatorPanel(player);
            if (config.events.largeOilrig.enabled)
                UpdateLargeOilrigPanel(player);
            if (config.events.smallOilrig.enabled)
                UpdateSmallOilrigPanel(player);
            if (config.info.users.enabled)
                UpdateConnectedPlayersPanel(player);
            if (config.info.sleepers.enabled)
                UpdateSleepersPanel(player);
            if (config.info.time.enabled)
                UpdateTimePanel(player);
            if (config.misc.balance.enabled)
                UpdateBalancePanel(player);
        }

        private void UpdatePlayersEventPanel(EventType eventType)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                PlayerData playerData;
                if (playersData.TryGetValue(player.userID, out playerData) && playerData.Hidden)
                    continue;

                if (eventType == EventType.CargoPlane)
                    UpdateCargoPlanePanel(player);
                else if (eventType == EventType.BradleyAPC)
                    UpdateBradleyAPCPanel(player);
                else if (eventType == EventType.Helicopter)
                    UpdateHelicopterPanel(player);
                else if (eventType == EventType.CH47)
                    UpdateCH47Panel(player);
                else if (eventType == EventType.CargoShip)
                    UpdateCargoShipPanel(player);
                else if (eventType == EventType.Excavator)
                    UpdateExcavatorPanel(player);
                else if (eventType == EventType.LargeOilrig)
                    UpdateLargeOilrigPanel(player);
                else if (eventType == EventType.SmallOilrig)
                    UpdateSmallOilrigPanel(player);
            }
        }

        private double GetBalance(BasePlayer player)
        {
            if (config.economicsPlugin.ToLower() == "economics" && Economics)
                return Economics.Call<double>("Balance", player.userID);
            else if (config.economicsPlugin.ToLower() == "iqeconomic" && IQEconomic)
                return (double)IQEconomic.Call<int>("API_GET_BALANCE", player.userID);

            return 0;
        }

        private bool IsHidden(BasePlayer player)
        {
            PlayerData playerData;
            return playersData.TryGetValue(player.userID, out playerData) && playerData.Hidden;
        }

        public bool IsMinimized(BasePlayer player)
        {
            PlayerData playerData;
            return playersData.TryGetValue(player.userID, out playerData) && playerData.Minimized;
        }

        private string GetImage(string image)
        {
            if (!(ImageLibrary?.Call<bool>("IsReady") ?? false))
                return image;

            if (!ImageLibrary.Call<bool>("HasImage", image))
            {
                ImageLibrary.Call("AddImage", image, image, (ulong)0);
                return image;
            }

            string imageId = ImageLibrary.Call<string>("GetImage", image, (ulong)0, true);
            if (string.IsNullOrEmpty(imageId))
                return image;

            return imageId;
        }

        private void LoadAllImages()
        {
            var newLoadOrder = GetConfigIcons().Distinct().ToDictionary(x => x);
            if (newLoadOrder.Count > 0)
            {
                ImageLibrary.Call("ImportImageList", Title, newLoadOrder);
            }
        }

        private List<string> GetConfigIcons(Dictionary<string, object> dict = null)
        {
            var result = new List<string>();

            dict = dict ?? config.ToDictionary();
            foreach (var pair in dict)
            {
                if (pair.Key == "Icon url" && pair.Value is string && !string.IsNullOrEmpty(pair.Value.ToString()))
                {
                    result.Add(pair.Value.ToString());
                }
                else if (pair.Value.GetType().Equals(typeof(Newtonsoft.Json.Linq.JObject)))
                {
                    var json = JsonConvert.SerializeObject(pair.Value);
                    result = Enumerable.Concat(result, GetConfigIcons(JsonConvert.DeserializeObject<Dictionary<string, object>>(json))).ToList();
                }
            }

            return result;
        }

        private void SendMessage(BasePlayer player, string key, params object[] args)
            => rust.SendChatMessage(player, Lang("ChatPrefix", player.UserIDString), string.Format(Lang(key, player.UserIDString), args));

        #endregion

        #region Data

        private Dictionary<ulong, PlayerData> playersData;

        private class PlayerData
        {
            public bool Hidden = false;
            public bool Minimized = false;
        }

        private void SaveData() =>
            Interface.Oxide.DataFileSystem.WriteObject(Name, playersData);

        private void LoadData() =>
            playersData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerData>>(Name);

        #endregion Data

        #region Configuration

        private Configuration config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Panel anchor")]
            public string anchor { get; set; }

            [JsonProperty(PropertyName = "Panel offset")]
            public string offset { get; set; }

            [JsonProperty(PropertyName = "Economics plugin (Economics/IQEconomic)")]
            public string economicsPlugin { get; set; }

            [JsonProperty(PropertyName = "Toggle UI Commands")]
            public string[] toggleUICommands { get; set; }

            [JsonProperty(PropertyName = "Can minimize and maximize the panel?")]
            public bool canMinimize { get; set; }

            [JsonProperty(PropertyName = "Minimize and maximize buttons")]
            public MinimizeAndMaximizeButtons minimizeAndMaximizeButtons { get; set; }

            [JsonProperty(PropertyName = "Events panel")]
            public EventsPanel events { get; set; }

            [JsonProperty(PropertyName = "Server info panel")]
            public ServerInfoPanel info { get; set; }

            [JsonProperty(PropertyName = "Misc panel")]
            public MiscPanel misc { get; set; }

            public VersionNumber Version { get; set; }

            public class MinimizeAndMaximizeButtons
            {
                public Button Minimize { get; set; }
                public Button Maximize { get; set; }

                public class Button
                {
                    [JsonProperty("Anchor")]
                    public string anchor { get; set; }

                    [JsonProperty("Offset")]
                    public string offset { get; set; }

                    [JsonProperty("Icon url")]
                    public string iconUrl { get; set; }

                    [JsonProperty("Icon color")]
                    public string iconColor { get; set; }

                    [JsonProperty("Icon size")]
                    public float iconSize { get; set; }

                    [JsonProperty("Background color")]
                    public string backgroundColor { get; set; }
                }
            }

            public class EventsPanel
            {
                [JsonProperty(PropertyName = "Cargo plane")]
                public EventPanel cargoplane { get; set; }

                [JsonProperty(PropertyName = "Bradley APC")]
                public EventPanel bradley { get; set; }

                [JsonProperty(PropertyName = "Helicopter")]
                public EventPanel helicopter { get; set; }

                [JsonProperty(PropertyName = "CH47 Chinook")]
                public EventPanel ch47 { get; set; }

                [JsonProperty(PropertyName = "Cargoship")]
                public EventPanel cargoship { get; set; }

                [JsonProperty(PropertyName = "Escavator")]
                public EventPanel excavator { get; set; }

                [JsonProperty(PropertyName = "Large Oil Rig")]
                public EventPanel largeOilrig { get; set; }

                [JsonProperty(PropertyName = "Small Oil Rig")]
                public EventPanel smallOilrig { get; set; }

                public class EventPanel
                {
                    [JsonProperty(PropertyName = "Enabled")]
                    public bool enabled { get; set; }

                    [JsonProperty(PropertyName = "Anchor")]
                    public string anchor { get; set; }

                    [JsonProperty(PropertyName = "Offset")]
                    public string offset { get; set; }

                    [JsonProperty(PropertyName = "Icon url")]
                    public string iconUrl { get; set; }

                    [JsonProperty(PropertyName = "Icon color")]
                    public string iconColor { get; set; }

                    [JsonProperty(PropertyName = "Background color")]
                    public string backgroundColor { get; set; }

                    [JsonProperty(PropertyName = "Active overrides")]
                    public ActiveSettings active { get; set; }

                    public class ActiveSettings
                    {
                        [JsonProperty(PropertyName = "Background color")]
                        public string backgroundColor { get; set; }

                        [JsonProperty(PropertyName = "Icon color")]
                        public string iconColor { get; set; }
                    }
                }
            }

            public class ServerInfoPanel
            {
                [JsonProperty(PropertyName = "Users")]
                public InfoPanel users { get; set; }

                [JsonProperty(PropertyName = "Sleepers")]
                public InfoPanel sleepers { get; set; }

                [JsonProperty(PropertyName = "Time")]
                public TimePanel time { get; set; }

                public class InfoPanel
                {
                    [JsonProperty(PropertyName = "Enabled")]
                    public bool enabled { get; set; }

                    [JsonProperty(PropertyName = "Anchor")]
                    public string anchor { get; set; }

                    [JsonProperty(PropertyName = "Offset")]
                    public string offset { get; set; }

                    [JsonProperty(PropertyName = "Icon url")]
                    public string iconUrl { get; set; }

                    [JsonProperty(PropertyName = "Icon color")]
                    public string iconColor { get; set; }

                    [JsonProperty(PropertyName = "Background color")]
                    public string backgroundColor { get; set; }

                    [JsonProperty(PropertyName = "Text color")]
                    public string textColor { get; set; }

                    [JsonProperty(PropertyName = "Font size")]
                    public int fontSize { get; set; }
                }

                public class TimePanel : InfoPanel
                {
                    [JsonProperty(PropertyName = "Refresh interval (seconds)")]
                    public float refreshInterval { get; set; }

                    [JsonProperty(PropertyName = "Day overrides")]
                    public OverrideAttributes day { get; set; }

                    [JsonProperty(PropertyName = "Night overrides")]
                    public OverrideAttributes night { get; set; }

                    public class OverrideAttributes
                    {
                        [JsonProperty(PropertyName = "Icon url")]
                        public string iconUrl { get; set; }

                        [JsonProperty(PropertyName = "Icon color")]
                        public string iconColor { get; set; }

                        [JsonProperty(PropertyName = "Background color")]
                        public string backgroundColor { get; set; }

                        [JsonProperty(PropertyName = "Text color")]
                        public string textColor { get; set; }
                    }
                }
            }

            public class MiscPanel
            {
                [JsonProperty("Balance")]
                public ServerInfoPanel.InfoPanel balance { get; set; }
            }

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }

                if (config.Version < Version)
                {
                    UpdateConfigValues();
                    SaveConfig();
                }
            }
            catch
            {
                Interface.Oxide.LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        protected override void LoadDefaultConfig() => config = GetBaseConfig();

        private Configuration GetBaseConfig()
        {
            return new Configuration
            {
                anchor = "0 1 0 1",
                offset = "12 -92 232 -12",
                economicsPlugin = "Economics",
                toggleUICommands = new string[] { "mhtoggle", "minimalisthud" },
                canMinimize = true,
                minimizeAndMaximizeButtons = new()
                {
                    Minimize = new()
                    {
                        anchor = "0 1 0 1",
                        offset = "12 -116 32 -96",
                        iconUrl = "https://i.postimg.cc/7YyQn1Xw/minimize.png",
                        iconColor = "0 1 1 1",
                        iconSize = 12f,
                        backgroundColor = "0.07 0.15 0.23 1"
                    },
                    Maximize = new()
                    {
                        anchor = "0 1 0 1",
                        offset = "12 -36 36 -12",
                        iconUrl = "https://i.postimg.cc/mgLX0b0n/menu.png",
                        iconColor = "0 1 1 1",
                        iconSize = 22f,
                        backgroundColor = "0.07 0.15 0.23 1"
                    }
                },
                events = new()
                {
                    cargoplane = new()
                    {
                        enabled = true,
                        anchor = "0 1 0 1",
                        offset = "0 -24 24 0",
                        iconUrl = "https://i.postimg.cc/T3Q46G4p/airplane.png",
                        iconColor = "1 1 1 1",
                        backgroundColor = "0 0 0 0.75",
                        active = new()
                        {
                            backgroundColor = "0.55 0.78 0.24 1",
                            iconColor = "1 1 1 1"
                        }
                    },
                    bradley = new()
                    {
                        enabled = true,
                        anchor = "0 1 0 1",
                        offset = "28 -24 52 0",
                        iconUrl = "https://i.postimg.cc/N0Pz6WCz/bradley.png",
                        iconColor = "1 1 1 1",
                        backgroundColor = "0 0 0 0.75",
                        active = new()
                        {
                            backgroundColor = "0.8 0.28 0.2 1",
                            iconColor = "1 1 1 1"
                        }
                    },
                    helicopter = new()
                    {
                        enabled = true,
                        anchor = "0 1 0 1",
                        offset = "56 -24 80 0",
                        iconUrl = "https://i.postimg.cc/7YMtsCQ7/helicopter.png",
                        iconColor = "1 1 1 1",
                        backgroundColor = "0 0 0 0.75",
                        active = new()
                        {
                            backgroundColor = "0.8 0.28 0.2 1",
                            iconColor = "1 1 1 1"
                        },
                    },
                    ch47 = new()
                    {
                        enabled = true,
                        anchor = "0 1 0 1",
                        offset = "84 -24 108 0",
                        iconUrl = "https://i.postimg.cc/nrqStMFy/chinook.png",
                        iconColor = "1 1 1 1",
                        backgroundColor = "0 0 0 0.75",
                        active = new()
                        {
                            backgroundColor = "0.8 0.28 0.2 1",
                            iconColor = "1 1 1 1"
                        },
                    },
                    cargoship = new()
                    {
                        enabled = true,
                        anchor = "0 1 0 1",
                        offset = "112 -24 136 0",
                        iconUrl = "https://i.postimg.cc/ZRGsg6SP/cargoship.png",
                        iconColor = "1 1 1 1",
                        backgroundColor = "0 0 0 0.75",
                        active = new()
                        {
                            backgroundColor = "0.65 0.41 0.88 1",
                            iconColor = "1 1 1 1"
                        },
                    },
                    excavator = new()
                    {
                        enabled = true,
                        anchor = "0 1 0 1",
                        offset = "140 -24 164 0",
                        iconUrl = "https://i.postimg.cc/KcTHTpP0/excavator.png",
                        iconColor = "1 1 1 1",
                        backgroundColor = "0 0 0 0.75",
                        active = new()
                        {
                            backgroundColor = "1 0.74 0 1",
                            iconColor = "1 1 1 1"
                        },
                    },
                    largeOilrig = new()
                    {
                        enabled = true,
                        anchor = "0 1 0 1",
                        offset = "168 -24 192 0",
                        iconUrl = "https://i.postimg.cc/fy7CrF5Z/oilrig.png",
                        iconColor = "1 1 1 1",
                        backgroundColor = "0 0 0 0.75",
                        active = new()
                        {
                            backgroundColor = "0.55 0.78 0.24 1",
                            iconColor = "1 1 1 1"
                        },
                    },
                    smallOilrig = new()
                    {
                        enabled = true,
                        anchor = "0 1 0 1",
                        offset = "196 -24 220 0",
                        iconUrl = "https://i.postimg.cc/QCZSSsQ0/small-oilrig.png",
                        iconColor = "1 1 1 1",
                        backgroundColor = "0 0 0 0.75",
                        active = new()
                        {
                            backgroundColor = "0.45 0.64 0.82 1",
                            iconColor = "1 1 1 1"
                        },
                    }
                },
                info = new()
                {
                    users = new()
                    {
                        enabled = true,
                        anchor = "0 1 0 1",
                        offset = "0 -52 52 -28",
                        iconUrl = "https://i.postimg.cc/tgfBZFY0/users.png",
                        iconColor = "1 1 1 1",
                        backgroundColor = "0.23 0.48 0.29 1",
                        textColor = "1 1 1 1",
                        fontSize = 12
                    },
                    sleepers = new()
                    {
                        enabled = true,
                        anchor = "0 1 0 1",
                        offset = "56 -52 108 -28",
                        iconUrl = "https://i.postimg.cc/XJCQwXCr/sleepers.png",
                        iconColor = "1 1 1 1",
                        backgroundColor = "0.24 0.24 0.24 1",
                        textColor = "1 1 1 1",
                        fontSize = 12
                    },
                    time = new()
                    {
                        enabled = true,
                        refreshInterval = 3,
                        anchor = "0 1 0 1",
                        offset = "112 -52 192 -28",
                        iconUrl = "https://i.postimg.cc/YSjXCd2f/sun.png",
                        iconColor = "1 1 1 1",
                        backgroundColor = "1 0.73 0 1",
                        fontSize = 12,
                        textColor = "1 1 1 1",
                        day = new()
                        {
                            iconUrl = "https://i.postimg.cc/YSjXCd2f/sun.png",
                            iconColor = "1 1 1 1",
                            backgroundColor = "1 0.73 0 1",
                            textColor = "1 1 1 1"
                        },
                        night = new()
                        {
                            iconUrl = "https://i.postimg.cc/CxcP030x/moon.png",
                            iconColor = "1 1 1 1",
                            backgroundColor = "0.15 0.15 0.27 1",
                            textColor = "1 1 1 1"
                        }
                    }
                },
                misc = new()
                {
                    balance = new()
                    {
                        enabled = true,
                        anchor = "0 1 0 1",
                        offset = "0 -80 108 -56",
                        iconUrl = "https://i.postimg.cc/tTZm22LC/balance.png",
                        iconColor = "1 1 1 1",
                        backgroundColor = "0.23 0.46 0.31 1",
                        fontSize = 12,
                        textColor = "1 1 1 1"
                    }
                },
                Version = Version
            };
        }

        private void UpdateConfigValues()
        {
            Interface.Oxide.LogWarning("Config update detected! Updating config values...");

            Configuration baseConfig = GetBaseConfig();

            if (config.Version < new VersionNumber(1, 0, 1))
            {
                config.toggleUICommands = baseConfig.toggleUICommands;
            }

            if (config.Version < new VersionNumber(1, 0, 2))
            {
                config.events.smallOilrig = baseConfig.events.smallOilrig;
            }

            if (config.Version < new VersionNumber(1, 1, 0))
            {
                config.offset = baseConfig.offset;
                config.canMinimize = baseConfig.canMinimize;
                config.minimizeAndMaximizeButtons = baseConfig.minimizeAndMaximizeButtons;
            }

            config.Version = Version;
            Interface.Oxide.LogWarning("Config update completed!");
        }

        #endregion Configuration

        #region Localization

        private string Lang(string key, string playerId)
            => lang.GetMessage(key, this, playerId);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ChatPrefix"] = "<color=orange>MinimalistHud:</color>",
                ["NoPermission"] = "You don't have permission to use this command",
                ["UIEnabled"] = "<color=#b9f0bd>You have enabled the server UI</color>",
                ["UIDisabled"] = "<color=#db9696>You have disabled the server UI</color>"
            }, this, "en");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ChatPrefix"] = "<color=orange>MinimalistHud:</color>",
                ["NoPermission"] = "Você não tem permissão para utilizar este comando",
                ["UIEnabled"] = "<color=#b9f0bd>Você habilitou a hud do servidor</color>",
                ["UIDisabled"] = "<color=#db9696>Você desabilitou a hud do servidor</color>"
            }, this, "pt-BR");
        }

        #endregion
    }
}