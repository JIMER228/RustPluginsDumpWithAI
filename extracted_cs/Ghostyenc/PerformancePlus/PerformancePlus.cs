//            ________               __                      //
//           / ____/ /_  ____  _____/ /___  __              //
//          / / __/ __ \/ __ \/ ___/ __/ / / /             //
//         / /_/ / / / / /_/ (__  ) /_/ /_/ /             //
//         \____/_/ /_/\____/____/\__/\__, /             //
//                                   /____/             //
//                                                     //
////////////////////////////////////////////////////////
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Performance Plus", "Ghosty", "1.0.6")]
    [Description("Conveyors and Auto Turrets turn off when their owner/team logs out if non vip, Sets some basic optimizations on startup, Has simple UI.")]
    public class PerformancePlus : RustPlugin
    {
        #region Plugin Reference

        [PluginReference]
        Plugin ImageLibrary;

        #endregion Plugin Reference

        #region Fields

        private const float ScanFrequency = 5.0f;
        private int lastReportedProgress = 0;
        private List<IndustrialConveyor> conveyorCache = new List<IndustrialConveyor>();
        private List<AutoTurret> autoTurretCache = new List<AutoTurret>();
        private Dictionary<string, bool> playerDeviceStates = new Dictionary<string, bool>();

        #endregion Fields

        #region Loading Images

        void Loaded()
        {
            ImageLibrary?.Call(
                "AddImage",
                "https://rustlabs.com/img/items180/autoturret.png",
                "TurretImage"
            );
            ImageLibrary?.Call(
                "AddImage",
                "https://rustlabs.com/img/items180/industrial.conveyor.png",
                "ConveyorImage"
            );
        }

        #endregion Loading Images

        #region Hooks

        void OnServerInitialized()
        {
            SetOptimizations();
            UpdateDeviceCache();
            ServerMgr.Instance.StartCoroutine(CheckDevicesOnceRoutine());
            permission.RegisterPermission("performanceplus.vip", this);
            permission.RegisterPermission("performanceplus.toggleturrets", this);
            permission.RegisterPermission("performanceplus.toggleconveyors", this);
            var turrets = BaseNetworkable.serverEntities.OfType<AutoTurret>().ToList();
            foreach (var turret in turrets)
            {
                turret.CancelInvoke("TargetScan");
                turret.InvokeRepeating("TargetScan", ScanFrequency, ScanFrequency);
            }
        }

        void OnPlayerConnected(BasePlayer player)
        {
            ServerMgr.Instance.StartCoroutine(ToggleDevicesForPlayer(player.UserIDString, true));
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            ServerMgr.Instance.StartCoroutine(ToggleDevicesForPlayer(player.UserIDString, false));
        }

        void OnEntityBuilt(Planner plan, GameObject go)
        {
            AddDeviceToCache(go);
        }

        #endregion Hooks

        #region Config

        public class Configuration
        {
            [JsonProperty(PropertyName = "Turret Scan Frequency (Default > 1)")]
            public float ScanFrequency { get; set; }

            [JsonProperty(PropertyName = "Turn team members devices on when team member logs in")]
            public bool TurnTeamMembersDevicesOn { get; set; } = true;

            public static Configuration DefaultConfig()
            {
                return new Configuration { ScanFrequency = 5.0f, TurnTeamMembersDevicesOn = true };
            }
        }

        static Configuration config;

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                    LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PrintWarning("Creating new config file.");
                LoadDefaultConfig();
                SaveConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();

        protected override void SaveConfig() => Config.WriteObject(config, true);

        #endregion Config

        #region Setting Optimizations

        private void SetOptimizations()
        {
            ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "fps.limit 60");
            ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "contacts false");
            ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "auto_turret_budget_ms 2");
        }

        #endregion Setting Optimizations

        #region Devices

        private void UpdateDeviceCache()
        {
            conveyorCache.Clear();
            autoTurretCache.Clear();

            foreach (var conveyor in BaseNetworkable.serverEntities.OfType<IndustrialConveyor>())
            {
                conveyorCache.Add(conveyor);
            }

            foreach (var turret in BaseNetworkable.serverEntities.OfType<AutoTurret>())
            {
                autoTurretCache.Add(turret);
            }
        }

        private void AddDeviceToCache(GameObject go)
        {
            IndustrialConveyor conveyor = go.GetComponent<IndustrialConveyor>();
            AutoTurret turret = go.GetComponent<AutoTurret>();

            if (conveyor != null)
            {
                conveyorCache.Add(conveyor);
                ProcessDevice(conveyor.OwnerID, conveyor.SetSwitch);
            }
            else if (turret != null)
            {
                autoTurretCache.Add(turret);
                bool isTurretPowered = turret.IsPowered();
                var owner = covalence.Players.FindPlayerById(turret.OwnerID.ToString());
                bool isOwnerOnline = owner != null && owner.IsConnected;
                if (isTurretPowered && isOwnerOnline)
                {
                    ProcessDevice(turret.OwnerID, turret.SetIsOnline);
                }
            }
        }

        private IEnumerator CheckDevicesOnceRoutine()
        {
            int totalDevices = conveyorCache.Count + autoTurretCache.Count;
            int processedDevices = 0;
            foreach (var conveyor in conveyorCache)
            {
                ProcessDevice(
                    conveyor.OwnerID,
                    state => conveyor.SetSwitch(conveyor.IsPowered() && state)
                );
                processedDevices++;
                yield return ProcessedDeviceRoutine(processedDevices, totalDevices);
            }
            foreach (var turret in autoTurretCache)
            {
                ProcessDevice(
                    turret.OwnerID,
                    state => turret.SetIsOnline(turret.IsPowered() && state)
                );
                processedDevices++;
                yield return ProcessedDeviceRoutine(processedDevices, totalDevices);
            }
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                if (entity is AutoTurret)
                {
                    var turret = entity as AutoTurret;
                    if (
                        turret.ShortPrefabName == "sentry.bandit.static"
                        || turret.ShortPrefabName == "sentry.scientist.static"
                    )
                    {
                        turret.SetIsOnline(true);
                        processedDevices++;
                    }
                }
            }
            Puts($"Total devices processed: {processedDevices}");
            Puts($"Finished Loading!");
            yield return null;
        }

        private void ProcessDevice(ulong ownerId, Action<bool> setDeviceState)
        {
            string ownerIdString = ownerId.ToString();
            bool isVip = permission.UserHasPermission(ownerIdString, "performanceplus.vip");
            var owner = covalence.Players.FindPlayerById(ownerIdString);
            bool deviceState = isVip || (owner != null && owner.IsConnected);
            setDeviceState(deviceState);
            playerDeviceStates[ownerIdString] = deviceState;
        }

        private IEnumerator ProcessedDeviceRoutine(int processedDevices, int totalDevices)
        {
            float progress = (float)processedDevices / totalDevices * 100;

            if (
                (progress >= 25 && lastReportedProgress < 25)
                || (progress >= 50 && lastReportedProgress < 50)
                || (progress >= 75 && lastReportedProgress < 75)
                || (progress >= 100 && lastReportedProgress < 100)
            )
            {
                Puts($"Processing devices: {progress:F2}% completed.");
                lastReportedProgress = (int)(progress / 25) * 25;
            }

            yield return null;
        }

        private IEnumerator ToggleDevicesForPlayer(string playerId, bool turnOn)
        {
            bool isVip = permission.UserHasPermission(playerId, "performanceplus.vip");
            var player = BasePlayer.FindByID(ulong.Parse(playerId));
            if (player == null)
                yield break;
            bool shouldToggle = true;
            if (config.TurnTeamMembersDevicesOn)
            {
                var team = player.Team;
                shouldToggle =
                    turnOn
                    || (
                        team == null
                        || team.members.All(memberId =>
                            !BasePlayer.activePlayerList.Any(p =>
                                p.userID == memberId && p.IsConnected
                            )
                        )
                    );
            }
            if (!isVip && shouldToggle)
            {
                foreach (var conveyor in conveyorCache)
                {
                    if (conveyor.OwnerID.ToString() == playerId && conveyor.IsPowered())
                    {
                        conveyor.SetSwitch(turnOn);
                    }
                }

                foreach (var turret in autoTurretCache)
                {
                    if (turret.OwnerID.ToString() == playerId && turret.IsPowered())
                    {
                        turret.SetIsOnline(turnOn);
                    }
                }
            }

            playerDeviceStates[playerId] = turnOn;
            yield return null;
        }

        #endregion Devices

        #region Menu

        private void Menu(BasePlayer player)
        {
            var container = new CuiElementContainer();

            container.Add(
                new CuiPanel
                {
                    CursorEnabled = true,
                    Image =
                    {
                        Color = "0.1843137 0.1803922 0.145098 1",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-108.425 -64.519",
                        OffsetMax = "108.425 64.519"
                    }
                },
                "Overlay",
                "Menu"
            );

            container.Add(
                new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1372549 0.1333333 0.1098039 1" },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-108.425 42.889",
                        OffsetMax = "108.425 64.519"
                    }
                },
                "Menu",
                "PanelBG"
            );

            container.Add(
                new CuiElement
                {
                    Name = "Title",
                    Parent = "Menu",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = "Performance Plus",
                            Font = "robotocondensed-regular.ttf",
                            FontSize = 14,
                            Align = TextAnchor.UpperLeft,
                            Color = "1 1 1 1"
                        },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-48.469 45.345",
                            OffsetMax = "48.469 63.699"
                        }
                    }
                }
            );

            container.Add(
                new CuiButton
                {
                    Button =
                    {
                        Color = "0.1411765 0.1490196 0.1254902 1",
                        Command = "toggleturrets"
                    },
                    Text =
                    {
                        Text = "On/Off",
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 9,
                        Align = TextAnchor.MiddleCenter,
                        Color = "0.6980392 0.6705883 0.627451 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-85.844 -47.471",
                        OffsetMax = "-26.756 -33.129"
                    }
                },
                "Menu",
                "TurretOnOffButton"
            );

            container.Add(
                new CuiElement
                {
                    Name = "TurretImage",
                    Parent = "Menu",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Sprite = "assets/icons/autoturret (1).png",
                            Png = ImageLibrary?.Call<string>("GetImage", "TurretImage")
                        },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-83.028 -30.585",
                            OffsetMax = "-29.572 20.986"
                        }
                    }
                }
            );

            container.Add(
                new CuiElement
                {
                    Name = "ConveyorImage",
                    Parent = "Menu",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Sprite = "assets/icons/industrialconveyor.png",
                            Png = ImageLibrary?.Call<string>("GetImage", "ConveyorImage")
                        },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "33.811 -24.789",
                            OffsetMax = "82.989 24.389"
                        }
                    }
                }
            );

            container.Add(
                new CuiButton
                {
                    Button =
                    {
                        Color = "0.1411765 0.1490196 0.1254902 1",
                        Command = "toggleconveyors"
                    },
                    Text =
                    {
                        Text = "On/Off",
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 9,
                        Align = TextAnchor.MiddleCenter,
                        Color = "0.6980392 0.6705883 0.627451 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "29.256 -47.371",
                        OffsetMax = "88.344 -33.029"
                    }
                },
                "Menu",
                "ConveyorOnOffButton"
            );

            container.Add(
                new CuiButton
                {
                    Button = { Color = "0.6981132 0.03622286 0.03622286 1", Command = "closemenu" },
                    Text =
                    {
                        Text = "✘",
                        FontSize = 13,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },
                    RectTransform = { AnchorMin = "0.45 0.45", AnchorMax = "0.55 0.55" }
                },
                "Menu",
                "CloseButton"
            );

            CuiHelper.DestroyUi(player, "Menu");
            CuiHelper.AddUi(player, container);
        }

        #endregion Menu

        #region Commands

        [ChatCommand("toggle")]
        private void CmdTestUI(BasePlayer player)
        {
            Menu(player);
        }

        [ConsoleCommand("closemenu")]
        private void CloseMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            CuiHelper.DestroyUi(player, "Menu");
        }

        [ConsoleCommand("toggleturrets")]
        private void ConsoleToggleTurrets(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
            {
                return;
            }
            if (!permission.UserHasPermission(player.UserIDString, "performanceplus.toggleturrets"))
            {
                player.ChatMessage(
                    "<color=red>Warning</color>: You do not have permission to use this command."
                );
                return;
            }
            bool canToggle = false;
            foreach (var turret in autoTurretCache)
            {
                if (turret.OwnerID.ToString() == player.UserIDString && turret.IsPowered())
                {
                    canToggle = true;
                    break;
                }
            }

            if (!canToggle)
            {
                player.ChatMessage(
                    "<color=red>Warning</color>: You cannot toggle your turrets as one or more of your turrets are not receiving power."
                );
                return;
            }

            bool newState =
                !playerDeviceStates.TryGetValue(player.UserIDString, out bool currentState)
                || !currentState;
            ServerMgr.Instance.StartCoroutine(
                ToggleTurretsForPlayer(player.UserIDString, newState)
            );
            player.ChatMessage(
                $"Turrets toggled {(newState ? "<color=green>(ON)</color>" : "<color=red>(OFF)</color>")}."
            );
        }

        [ConsoleCommand("toggleconveyors")]
        private void ConsoleToggleConveyors(ConsoleSystem.Arg arg)
        {
            var player = arg.Connection.player as BasePlayer;
            if (player == null)
            {
                return;
            }

            if (
                !permission.UserHasPermission(
                    player.UserIDString,
                    "performanceplus.toggleconveyors"
                )
            )
            {
                player.ChatMessage(
                    "<color=red>Warning</color>: You do not have permission to use this command."
                );
                return;
            }
            bool canToggle = false;
            foreach (var conveyor in conveyorCache)
            {
                if (conveyor.OwnerID.ToString() == player.UserIDString && conveyor.IsPowered())
                {
                    canToggle = true;
                    break;
                }
            }

            if (!canToggle)
            {
                player.ChatMessage(
                    "<color=red>Warning</color>: You cannot toggle your conveyors as one or more of your conveyors are not receiving power."
                );
                return;
            }

            bool newState =
                !playerDeviceStates.TryGetValue(player.UserIDString, out bool currentState)
                || !currentState;
            ServerMgr.Instance.StartCoroutine(
                ToggleConveyorsForPlayer(player.UserIDString, newState)
            );
            player.ChatMessage(
                $"Conveyors toggled {(newState ? "<color=green>(ON)</color>" : "<color=red>(OFF)</color>")}."
            );
        }

        private IEnumerator ToggleTurretsForPlayer(string playerId, bool turnOn)
        {
            foreach (var turret in autoTurretCache)
            {
                if (turret.OwnerID.ToString() == playerId && turret.IsPowered())
                {
                    turret.SetIsOnline(turnOn);
                }
            }
            playerDeviceStates[playerId] = turnOn;
            yield return null;
        }

        private IEnumerator ToggleConveyorsForPlayer(string playerId, bool turnOn)
        {
            foreach (var conveyor in conveyorCache)
            {
                if (conveyor.OwnerID.ToString() == playerId && conveyor.IsPowered())
                {
                    conveyor.SetSwitch(turnOn);
                }
            }

            playerDeviceStates[playerId] = turnOn;
            yield return null;
        }

        #endregion Commands
    }
}
