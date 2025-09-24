using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System.Linq;
using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Custom Status Framework", "mr01sam", "1.1.3")]
    [Description("Allows plugins to add custom status displays for the UI")]
    internal partial class CustomStatusFramework : CovalencePlugin
    {
        private static CustomStatusFramework PLUGIN;

        [PluginReference] 
        private readonly Plugin ImageLibrary;

        private readonly bool TestingPositions = false;

        private readonly List<string> Changelog = new List<string>
        {
            "1.1.0",
            "Added config option to change the position of the status stack. If set to anything other than 'vanilla' the statuses won't stack on the vanilla statuses, which will result in a MASSIVE performance increase.",
            "Added config options to adjust the offset for status placement.",
            "Reworked code to improve performance for larger servers.",
            "1.1.1",
            "Fixed error with NRE on metabolize hook",
            "1.1.2",
            "Fixed error upon reload",
            "1.1.3",
            "Fixed bad performing OnPlayerMetabolize hook when using the vanilla position",
            "Fast refresh is now ON by default, only recommend changing it if you are seeing performance issues",
            "Show console warnings is now off by default - felt like it was confusing some users",
            "Updated the demo file, no longer errors on ShowStatus and correctly demonstrates how to auto-reload",
        };

        private StatusPosition SelectedStatusPosition = StatusPosition.Vanilla;

        void Init()
        {
            Unsubscribe(nameof(Unload));
            Unsubscribe(nameof(OnPlayerMetabolize));
            Unsubscribe(nameof(OnItemPickup));
            Unsubscribe(nameof(OnStructureUpgrade));
            Unsubscribe(nameof(OnStructureRepair));
            Unsubscribe(nameof(OnDispenserGather));
            Unsubscribe(nameof(OnEntityMounted));
            Unsubscribe(nameof(OnEntityDismounted));
            Unsubscribe(nameof(CanPickupEntity));
            Unsubscribe(nameof(OnPlayerSleepEnded));
            Unsubscribe(nameof(OnPlayerDisconnected));
            StatusHud.GlobalStatuses = new Dictionary<string, CustomStatus>();
            StatusHud.PlayerStatusHuds = new Dictionary<string, StatusHud>();
            if (TestingPositions)
            {
                PrintWarning("Warning - Testing Positions");
                TestingScript();
            }
        }

        void Unload()
        {
            StatusHud.GlobalStatuses = null;
            StatusHud.PlayerStatusHuds = null;
            PlayerItemPickupExpirations = null;
            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(basePlayer, STATUS_HUD_ID);
            }
        }

        void OnServerInitialized()
        {
            PLUGIN = this;
            try
            {
                ImageLibrary.Call("isLoaded", null);
            }
            catch (Exception)
            {
                PrintWarning($"The required dependency ImageLibary is not installed, {Name} will not work properly without it.");
            }
            Subscribe(nameof(Unload));
            Subscribe(nameof(OnEntityMounted));
            Subscribe(nameof(OnEntityDismounted));
            Subscribe(nameof(OnPlayerSleepEnded));
            Subscribe(nameof(OnPlayerDisconnected));
            if (IsUsingVanillaPosition)
            {
                Subscribe(nameof(OnItemPickup));
                Subscribe(nameof(OnStructureUpgrade));
                Subscribe(nameof(OnStructureRepair));
                Subscribe(nameof(OnPlayerMetabolize));
                Subscribe(nameof(OnDispenserGather));
                Subscribe(nameof(CanPickupEntity));
            }
            UpdateEveryOneInterval();
        }

        void UpdateEveryOneInterval()
        {
            timer.Every(config.FastRefresh ? 0.1f : 1f, () =>
            {
                foreach(var basePlayer in (BasePlayer.activePlayerList))
                {
                    try
                    {
                        if (basePlayer == null || basePlayer.IsDead()) { continue; }
                        var sh = StatusHud.Instance(basePlayer);
                        if (sh == null) { continue; }
                        sh.RedrawGlobalStatuses();
                    }
                    catch (Exception) { continue; }
                }
            });
        }

        void TestingScript()
        {
            timer.In(1, () =>
            {
                SelectedStatusPosition = StatusPosition.TopLeft;
                Func<BasePlayer, bool> always = (basePlayer) =>
                {
                    return basePlayer != null;
                };
                CreateStatus("test1", RustColor.Gray, "Status 1", null, "One", null, "star", null, always);
                CreateStatus("test2", RustColor.Blue, "Status 2", null, "Two", null, "star", null, always);
                CreateStatus("test3", RustColor.Orange, "Status 3", null, "Three", null, "star", null, always);
                Func<BasePlayer, string> coords = (basePlayer) =>
                {
                    return $"{Math.Round(basePlayer.transform.position.x, 1)} {Math.Round(basePlayer.transform.position.y, 1)} {Math.Round(basePlayer.transform.position.z, 1)}";
                };
                CreateDynamicStatus("test4", RustColor.White, "Coords", RustColor.Blue, RustColor.Blue, "star", RustColor.Blue, always, coords);
            });
        }
    }
}

namespace Oxide.Plugins
{
    internal partial class CustomStatusFramework : CovalencePlugin
    {
        // Returns a list of all custom statuses for a player, excludes vanilla ones.
        private List<string> GetStatusList
        (
            BasePlayer basePlayer
        )
        {
            if (basePlayer == null || basePlayer.IsNpc)
            {
                return new List<string>();
            }
            var sh = StatusHud.Instance(basePlayer);
            if (sh == null) { return new List<string>(); }
            return sh.Keys.ToList();
        }

        // Returns true if a player has a status matching the given id.
        private bool HasStatus
        (
            BasePlayer basePlayer, 
            string id
        )
        {
            if (basePlayer == null || basePlayer.IsNpc)
            {
                return false;
            }
            var sh = StatusHud.Instance(basePlayer);
            return sh?.ContainsKey(id) ?? false;
        }

        // Method for showing a simple temporary status to a player.
        // This status will appear and then disappear after some time.
        private void ShowStatus
        (
            BasePlayer basePlayer, 
            string id, 
            string color = null, 
            string text = null, 
            string textColor = null, 
            string subText = null, 
            string subTextColor = null, 
            string imageLibraryIconId = null, 
            string iconColor = null, 
            float? seconds = 4f
        )
        {
            if (basePlayer == null || basePlayer.IsNpc)
            {
                return;
            }
            if (!seconds.HasValue)
            {
                seconds = 4f;
            }
            SetStatus(basePlayer, id, color, text, textColor, subText, subTextColor, imageLibraryIconId, iconColor);
            timer.In(seconds.Value, () =>
            {
                if (basePlayer != null)
                {
                    var sh = StatusHud.Instance(basePlayer);
                    if (sh == null) { return; }
                    sh.Remove(id);
                    sh.Redraw();
                }
            });
        }

        // Performs a ClearStatus and then a SetStatus.
        // Useful if you want to simply refresh a status value.
        private void UpdateStatus
        (
            BasePlayer basePlayer, 
            string id, string color, 
            string text, 
            string textColor, 
            string subText, 
            string subTextColor, 
            string imageLibraryIconId,
            string iconColor
        )
        {
            if (basePlayer == null || basePlayer.IsNpc)
            {
                return;
            }
            var sh = StatusHud.Instance(basePlayer);
            if (sh == null) { return; }
            sh.Remove(id);
            SetStatus(basePlayer, id, color, text, textColor, subText, subTextColor, imageLibraryIconId, iconColor);
        }

        // Creates a status for the given player.
        private void SetStatus
        (
            BasePlayer basePlayer, 
            string id, 
            string color, 
            string text, 
            string textColor, 
            string subText, 
            string subTextColor, 
            string imageLibraryIconId, 
            string iconColor
        )
        {
            if (basePlayer == null || basePlayer.IsNpc)
            {
                return;
            }
            var sh = StatusHud.Instance(basePlayer);
            if (sh == null) { return; }
            sh[id] = new CustomStatus
            {
                Id = id,
                LeftText = text,
                RightText = subText,
                TextColor = textColor,
                SubTextColor = subTextColor,
                Color = color,
                Icon = imageLibraryIconId,
                IconColor = iconColor
            };
            sh.Redraw();
        }

        // Removes the specified status from the given player.
        private void ClearStatus
        (
            BasePlayer basePlayer, 
            string id
        )
        {
            if (basePlayer == null || basePlayer.IsNpc)
            {
                return;
            }
            var sh = StatusHud.Instance(basePlayer);
            if (sh == null) { return; }
            sh.Remove(id);
            sh.Redraw();
        }

        // Deletes a global status by id.
        private void DeleteStatus
        (
            string id
        )
        {
            try
            {
                StatusHud.GlobalStatuses.Remove(id);
                foreach (var basePlayer in BasePlayer.activePlayerList)
                {
                    var sh = StatusHud.Instance(basePlayer);
                    if (sh == null) { continue; }
                    sh.Redraw();
                }
            } catch(Exception) { }
        }

        // Creates a global status with a static value.
        // The value of this status will not change, but will be dynamically applied to all
        // players who meet the specified condition function.
        private void CreateStatus
        (
            string id, 
            string color, 
            string text, 
            string textColor, 
            string subText, 
            string subTextColor, 
            string imageLibraryIconId, 
            string iconColor, 
            Func<BasePlayer, bool> condition
        )
        {
            NextFrame(() =>
            {
                StatusHud.GlobalStatuses[id] = new CustomStatus
                {
                    Id = id,
                    LeftText = text,
                    RightText = subText,
                    TextColor = textColor,
                    Color = color,
                    Icon = imageLibraryIconId,
                    SubTextColor = subTextColor,
                    IconColor = iconColor,
                    OnCondition = condition
                };
                foreach (var basePlayer in BasePlayer.activePlayerList)
                {
                    var sh = StatusHud.Instance(basePlayer);
                    if (sh == null) { continue; }
                    sh.RedrawGlobalStatuses();
                }
            });
        }

        // Creates a global status with a dynamic value.
        // The value of this status will change, and is dynamically set for each player
        // based on the return of the dynamicValue function.
        // This status will be dynamically applied to all players who meet the specified
        // condition function.
        private void CreateDynamicStatus
        (
            string id, 
            string color, 
            string text, 
            string textColor, 
            string subTextColor, 
            string imageLibraryIconId, 
            string iconColor, 
            Func<BasePlayer, bool> condition, 
            Func<BasePlayer, string> dynamicValue
        )
        {
            NextFrame(() =>
            {
                StatusHud.GlobalStatuses[id] = new CustomStatus
                {
                    Id = id,
                    LeftText = text,
                    TextColor = textColor,
                    Color = color,
                    Icon = imageLibraryIconId,
                    SubTextColor = subTextColor,
                    IconColor = iconColor,
                    OnCondition = condition,
                    DynamicText = dynamicValue
                };
                foreach (var basePlayer in BasePlayer.activePlayerList)
                {
                    var sh = StatusHud.Instance(basePlayer);
                    if (sh == null) { continue; }
                    sh.RedrawGlobalStatuses();
                }
            });
        }
    }
}

namespace Oxide.Plugins
{
    partial class CustomStatusFramework
    {
        public static class RustColor
        {
            public static string Blue = "0.08627 0.25490 0.38431 1";
            public static string LightBlue = "0.25490 0.61176 0.86275 1";
            public static string Red = "0.68627 0.21569 0.14118 1";
            public static string LightRed = "0.91373 0.77647 0.75686 1";
            //public static string Green = "0.25490 0.30980 0.14510 1";
            public static string Green = "0.35490 0.40980 0.24510 1";
            public static string LightGreen = "0.76078 0.94510 0.41176 1";
            public static string Gray = "0.45490 0.43529 0.40784 1";
            public static string LightGray = "0.69804 0.66667 0.63529 1";
            public static string Orange = "1.00000 0.53333 0.18039 1";
            public static string LightOrange = "1.00000 0.82353 0.56471 1";
            public static string White = "0.87451 0.83529 0.80000 1";
            public static string LightWhite = "0.97647 0.97647 0.97647 1";
            public static string Lime = "0.64706 1.00000 0.00000 1";
            public static string LightLime = "0.69804 0.83137 0.46667 1";
        }
    }
}

namespace Oxide.Plugins
{
    partial class CustomStatusFramework
    {
        private Configuration config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "Position")]
            public string Position = "vanilla"; // vanilla, top left, top, top right, right, bottom right, bottom

            [JsonProperty(PropertyName = "Offset X")]
            public int OffsetX = 0;

            [JsonProperty(PropertyName = "Offset Y")]
            public int OffsetY = 0;

            [JsonProperty(PropertyName = "Fast Refresh (negatively impacts performance)")]
            public bool FastRefresh = true;

            [JsonProperty(PropertyName = "Show Console Warnings")]
            public bool ShowConsoleWarnings = false;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new Exception();
                var parsed = StatusPosition.Parse(config.Position);
                if (parsed == null)
                {
                    PrintError("Invalid 'Position' in config. Valid values are: 'vanilla', 'top left', 'top', 'top right', 'right', 'bottom right', 'bottom'");
                    throw new Exception();
                }
                else
                {
                    SelectedStatusPosition = parsed;
                    if (IsUsingVanillaPosition && config.ShowConsoleWarnings)
                    {
                        PrintWarning("Warning - You have selected the 'vanilla' position. This position requires constant updates and may severly impact server load. To increase performance, change this option in the config to any other valid position.");
                    }
                    if (config.FastRefresh && config.ShowConsoleWarnings)
                    {
                        PrintWarning("Warning - You have 'Fast Refresh' enabled, the plugin will recalculate statuses more frequently but this may result in consumption of more server resources.");
                    }
                }
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        protected override void LoadDefaultConfig() => config = new Configuration();
    }
}

namespace Oxide.Plugins
{
    internal partial class CustomStatusFramework : CovalencePlugin
    {
        public class CustomStatus
        {
            public string Id { get; set; }
            public string Color { get; set; } = RustColor.Gray;
            public string Icon { get; set; } = string.Empty;
            public string IconColor { get; set; } = RustColor.White;
            public string LeftText { get; set; } = string.Empty;
            public string RightText { get; set; } = string.Empty;
            public string TextColor { get; set; } = RustColor.White;
            public string SubTextColor { get; set; } = RustColor.LightGray;
            public Func<BasePlayer, string> DynamicText { get; set; } = null;
            public bool IsDynamic
            {
                get
                {
                    return DynamicText != null;
                }
            }
            public Func<BasePlayer, bool> OnCondition { get; set; } = (x) => { return true; };
            public bool IsTriggered(BasePlayer player)
            {
                return OnCondition == null ? true : OnCondition.Invoke(player);
            }
            public override bool Equals(object obj)
            {
                if ((obj == null) || !this.GetType().Equals(obj.GetType()))
                {
                    return false;
                }
                else
                {
                    var p = obj as CustomStatus;
                    return (Id == p.Id);
                }
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode();
            }
        }
    }
}

namespace Oxide.Plugins
{
    internal partial class CustomStatusFramework : CovalencePlugin
    {
        void OnPlayerSleepEnded(BasePlayer basePlayer)
        {
            var sh = StatusHud.Instance(basePlayer);
            if (sh == null) { return; }
            sh.Redraw();
        }

        void OnPlayerDeath(BasePlayer basePlayer, HitInfo info)
        {
            NextFrame(() =>
            {
                CuiHelper.DestroyUi(basePlayer, STATUS_HUD_ID);
            });
        }

        void OnPlayerDisconnected(BasePlayer basePlayer, string reason)
        {
            StatusHud.PlayerStatusHuds.Remove(basePlayer.UserIDString);
        }

        void CanPickupEntity(BasePlayer basePlayer, BaseEntity entity)
        {
            var name = entity?.name;
            NextTick(() =>
            {
                if (entity == null && basePlayer != null)
                {
                    IncrementPlayerItemChangeNotificationCount(basePlayer, name);
                }
            });
        }

        void OnItemPickup(Item item, BasePlayer basePlayer)
        {
            IncrementPlayerItemChangeNotificationCount(basePlayer, item?.info?.shortname);
        }

        void OnStructureUpgrade(BaseCombatEntity entity, BasePlayer basePlayer, BuildingGrade.Enum grade)
        {
            IncrementPlayerItemChangeNotificationCount(basePlayer, $"removed {grade}");
        }

        void OnStructureRepair(BuildingBlock entity, BasePlayer basePlayer)
        {
            var curHp = entity.health;
            NextTick(() =>
            {
                if (entity != null)
                {
                    var newHp = entity.health;
                    if (newHp > curHp)
                    {
                        IncrementPlayerItemChangeNotificationCount(basePlayer, $"removed {entity.grade}");
                    }
                }
            });
        }

        void OnDispenserGather(ResourceDispenser dispenser, BasePlayer basePlayer, Item item)
        {
            IncrementPlayerItemChangeNotificationCount(basePlayer, item.info.shortname);
        }

        void OnPlayerMetabolize(PlayerMetabolism metabolism, BasePlayer basePlayer, float delta)
        {
            if (basePlayer == null || !basePlayer.isActiveAndEnabled || basePlayer.IsSleeping() || delta == 0) { return; }
            NextTick(() =>
            {
                if (basePlayer == null) { return; }
                StatusHud sh = StatusHud.Instance(basePlayer);
                if (sh == null) { return; }
                var count = GetStatusCount(basePlayer);
                if (sh.HasChange || count != sh.VanillaStatusCount)
                {
                    sh.VanillaStatusCount = count;
                    sh.Redraw();
                }
            });
        }

        void OnEntityMounted(ComputerStation entity, BasePlayer basePlayer)
        {
            if (entity == null || basePlayer == null || basePlayer.UserIDString == null) { return; }
            CuiHelper.DestroyUi(basePlayer, STATUS_HUD_ID);
        }

        void OnEntityDismounted(ComputerStation entity, BasePlayer basePlayer)
        {
            NextTick(() =>
            {
                if (entity == null || basePlayer == null) { return; }
                StatusHud sh = StatusHud.Instance(basePlayer);
                if (sh == null) { return; }
                sh.Redraw();
            });
        }
    }
}

namespace Oxide.Plugins
{
    internal partial class CustomStatusFramework : CovalencePlugin
    {
        public class StatusPosition
        {
            public static StatusPosition Vanilla = new StatusPosition
            {
                AnchorMin = "1 0",
                AnchorMax = "1 0",
                GrowUp = true
            };

            public static StatusPosition TopLeft = new StatusPosition
            {
                AnchorMin = "0 1",
                AnchorMax = "0 1",
                OffsetY = -20,
                GrowRight = true
            };

            public static StatusPosition Top = new StatusPosition
            {
                AnchorMin = "0.5 1",
                AnchorMax = "0.5 1",
                OffsetY = -20,
                Centered = true
            };

            public static StatusPosition TopRight = new StatusPosition
            {
                AnchorMin = "1 1",
                AnchorMax = "1 1",
                OffsetY = -20,
            };

            public static StatusPosition Right = new StatusPosition
            {
                AnchorMin = "1 0.2",
                AnchorMax = "1 0.2",
                GrowUp = true
            };

            public static StatusPosition BottomRight = new StatusPosition
            {
                AnchorMin = "1 0",
                AnchorMax = "1 0",
                OffsetX = -200,
                OffsetY = -84,
                GrowUp = true
            };

            public static StatusPosition Bottom = new StatusPosition
            {
                AnchorMin = "0.5 0",
                AnchorMax = "0.5 0",
                GrowUp = true,
                Centered = true
            };

            public static StatusPosition Left = new StatusPosition
            {
                AnchorMin = "0 0.2",
                AnchorMax = "0 0.2",
                OffsetY = -20,
                GrowRight = true,
                GrowUp = true
            };

            public static StatusPosition BottomLeft = new StatusPosition
            {
                AnchorMin = "0 0",
                AnchorMax = "0 0",
                OffsetY = -84,
                GrowRight = true,
                GrowUp = true
            };

            public string Id = "";
            public string AnchorMin = "";
            public string AnchorMax = "";
            public int OffsetX = 0;
            public int OffsetY = 0;
            public bool GrowUp = false;
            public bool GrowRight = false;
            public bool Centered = false;

            public static StatusPosition Parse(string str)
            {
                str = str.ToLower().Trim();
                switch(str)
                {
                    case "vanilla":
                        return StatusPosition.Vanilla;
                    case "top left":
                        return StatusPosition.TopLeft;
                    case "top":
                        return StatusPosition.Top;
                    case "top right":
                        return StatusPosition.TopRight;
                    case "right":
                        return StatusPosition.Right;
                    case "bottom right":
                        return StatusPosition.BottomRight;
                    case "bottom":
                        return StatusPosition.Bottom;
                    case "left":
                        return StatusPosition.Left;
                    case "bottom left":
                        return StatusPosition.BottomLeft;
                    default:
                        return null;
                }
            }
        }
    }
}

namespace Oxide.Plugins
{
    internal partial class CustomStatusFramework : CovalencePlugin
    {
        private NullSafeDictionary<string, Dictionary<string, DateTime>> PlayerItemPickupExpirations = new NullSafeDictionary<string, Dictionary<string, DateTime>>();

        public bool IsUsingVanillaPosition => SelectedStatusPosition == StatusPosition.Vanilla;

        int PlayerItemChangeNotificationCount(BasePlayer basePlayer)
        {
            if (basePlayer == null)
            {
                return 0;
            }
            var count = 0;
            var now = DateTime.UtcNow;
            foreach(var key in PlayerItemPickupExpirations[basePlayer.UserIDString].Keys.ToArray())
            {
                if (PlayerItemPickupExpirations[basePlayer.UserIDString][key] <= now)
                {
                    PlayerItemPickupExpirations[basePlayer.UserIDString].Remove(key);
                }
                else
                {
                    count++;
                }
            }
            return count;
        }

        void IncrementPlayerItemChangeNotificationCount(BasePlayer basePlayer, string key)
        {
            if (basePlayer == null || key == null)
            {
                return;
            }
            var nowPlus4Seconds = DateTime.UtcNow.AddSeconds(4);
            PlayerItemPickupExpirations[basePlayer.UserIDString][key] = nowPlus4Seconds;
        }

        private int GetStatusCount(BasePlayer basePlayer)
        {
            if (!IsUsingVanillaPosition) { return 0; }
            try
            {
                var count = 0;
                if (basePlayer.metabolism.bleeding.value >= 1)
                {
                    count++; // bleeding
                }
                if (basePlayer.metabolism.temperature.value < 5)
                {
                    count++; // toocold
                }
                if (basePlayer.metabolism.temperature.value > 40)
                {
                    count++; // toohot
                }
                if (basePlayer.currentComfort > 0)
                {
                    count++; // comfort
                }
                if (basePlayer.metabolism.calories.value < 40)
                {
                    count++; // starving
                }
                if (basePlayer.metabolism.hydration.value < 35)
                {
                    count++; // dehydrated
                }
                if (basePlayer.metabolism.radiation_poison.value > 0)
                {
                    count++; // radiation
                }
                if (basePlayer.metabolism.wetness.value >= 0.02)
                {
                    count++; // wet
                }
                if (basePlayer.metabolism.oxygen.value < 1f)
                {
                    count++; // drowning
                }
                if (basePlayer.currentCraftLevel > 0)
                {
                    count++; // workbench
                }
                if (basePlayer.inventory.crafting.queue.Count > 0)
                {
                    count++; // crafting
                }
                if (basePlayer.modifiers.ActiveModifierCoount > 0)
                {
                    count++; // modifiers
                }
                var priv = basePlayer.GetBuildingPrivilege();
                if (priv != null && priv.IsAuthed(basePlayer))
                {
                    count++; // buildpriv
                    count++; // upkeep
                }
                else if (priv != null && !priv.IsAuthed(basePlayer) && basePlayer.GetActiveItem()?.info.shortname == "hammer")
                {
                    count++; // buildpriv
                }
                for (int i = 0; i < PlayerItemChangeNotificationCount(basePlayer); i++)
                {
                    count++; // itemchange
                }
                if (basePlayer.HasPlayerFlag(BasePlayer.PlayerFlags.SafeZone))
                {
                    count++; // safezone
                }
                if (basePlayer.isMounted && (basePlayer.GetMountedVehicle() != null || basePlayer.GetMounted() is RidableHorse))
                {
                    count++; // mounted
                }
                return count;
            }
            catch (NullReferenceException) { }
            return 0;
        }
    }
}

namespace Oxide.Plugins
{
    internal partial class CustomStatusFramework : CovalencePlugin
    {
        public static readonly string STATUS_HUD_ID = "status";

        public class StatusHud : Dictionary<string, CustomStatus>
        {
            public static Dictionary<string, StatusHud> PlayerStatusHuds = new Dictionary<string, StatusHud>();

            public static StatusHud Instance(BasePlayer basePlayer)
            {
                try
                {
                    StatusHud sh;
                    if (!PlayerStatusHuds.TryGetValue(basePlayer.UserIDString, out sh))
                    {
                        sh = new StatusHud(basePlayer.UserIDString);
                        PlayerStatusHuds[basePlayer.UserIDString] = sh;
                    }
                    return sh;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            public static Dictionary<string, CustomStatus> GlobalStatuses = new Dictionary<string, CustomStatus>();

            public int VanillaStatusCount = 0;
            public bool HasChange = true;

            private BasePlayer _basePlayer;

            private bool DrawBlocked
            {
                get
                {
                    return BasePlayer == null || BasePlayer.IsSleeping() || BasePlayer.IsDead() || IsUsingComputerStation(BasePlayer);
                }
            }

            private bool IsUsingComputerStation(BasePlayer basePlayer)
            {
                if (basePlayer == null || !basePlayer.isMounted) { return false; }
                var mount = basePlayer.GetMounted();
                if (mount == null) { return false; }
                return mount is ComputerStation;
            }

            public StatusHud(string userIdString)
            {
                UserIdString = userIdString;
            }

            string UserIdString { get; set; }
            BasePlayer BasePlayer
            {
                get
                {
                    if (_basePlayer == null)
                    {
                        _basePlayer = BasePlayer.FindAwakeOrSleeping(UserIdString);
                    }
                    return _basePlayer;
                }
            }
            CuiElementContainer Container { get; set; }
            CuiElement BaseElement { get; set; }

            Dictionary<string, CuiElement> TextElementsByStatusId = new Dictionary<string, CuiElement>();

            new public CustomStatus this[string key]
            {
                get
                {
                    return base[key];
                }
                set
                {
                    base[key] = value;
                    HasChange = true;
                }
            }

            public new void Remove(string statusId)
            {
                if (ContainsKey(statusId))
                {
                    base.Remove(statusId);
                    TextElementsByStatusId.Remove(statusId);
                    HasChange = true;
                }
            }

            public void RedrawGlobalStatuses()
            {
                if (DrawBlocked) { return; }
                foreach (var status in GlobalStatuses.Values)
                {
                    var containsKey = ContainsKey(status.Id);
                    var isTriggered = status.IsTriggered(BasePlayer);
                    if (!containsKey && isTriggered)
                    {
                        this[status.Id] = status;
                    }
                    else if (containsKey && !isTriggered)
                    {
                        Remove(status.Id);
                    }
                    else if (containsKey && isTriggered && status.IsDynamic)
                    {
                        Redraw(status.Id);
                    }
                }
            }

            public void Redraw()
            {
                if (DrawBlocked) { return; }
                var position = PLUGIN.SelectedStatusPosition;
                var idx = VanillaStatusCount;
                var fontSize = 12;
                var left = 26;
                var padding = 8;
                var imgP = 5;
                var imgS = 16;
                var ey = 0;
                var eh = 26;
                var eg = 2;
                var startY = (eh + eg) * idx;
                var numEntrees = 12;
                var x = (position.GrowRight ? 16 : -16) + position.OffsetX + PLUGIN.config.OffsetX;
                var y = (position.GrowUp ? 100 : -20) + startY + position.OffsetY + PLUGIN.config.OffsetY;
                var w = 192;
                var h = (eh + eg) * numEntrees - startY;
                if (position.Centered)
                {
                    x = (w / 2);
                }
                Container = new CuiElementContainer();
                // Base
                BaseElement = new CuiElement()
                {
                    Name = STATUS_HUD_ID,
                    Parent = "Under",
                    Components =
                    {
                        new CuiImageComponent
                        {
                            Color = "0 0 0 0"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = position.AnchorMin,
                            AnchorMax = position.AnchorMax,
                            OffsetMin = $"{(position.GrowRight ? x : x - w)} {(position.GrowUp ? y : y - h)}",
                            OffsetMax = $"{(position.GrowRight ? x + w : x)} {(position.GrowUp ? y + h : y)}"
                        }
                    }
                };
                Container.Add(BaseElement);
                foreach (var custom in Values)
                {
                    var id = $"{STATUS_HUD_ID}.{custom.Id}";
                    var fontColor = string.IsNullOrEmpty(custom.TextColor) ? RustColor.White : custom.TextColor;
                    var subFontColor = string.IsNullOrEmpty(custom.SubTextColor) ? RustColor.LightGray : custom.SubTextColor;
                    var bgColor = string.IsNullOrEmpty(custom.Color) ? RustColor.Gray : custom.Color;
                    var iconColor = string.IsNullOrEmpty(custom.IconColor) ? RustColor.White : custom.IconColor;
                    var leftText = string.IsNullOrEmpty(custom.LeftText) ? string.Empty : custom.LeftText;
                    var rightText = string.IsNullOrEmpty(custom.RightText) ? string.Empty : custom.RightText;
                    Container.Add(new CuiElement
                    {
                        Name = id,
                        Parent = BaseElement.Name,
                        Components =
                        {
                            new CuiImageComponent
                            {
                                Color = bgColor,
                                Material = "assets/icons/greyout.mat"
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0 {(position.GrowUp ? 0 : 1)}",
                                AnchorMax = $"1 {(position.GrowUp ? 0 : 1)}",
                                OffsetMin = $"{0} {(position.GrowUp ? ey : ey-eh)}",
                                OffsetMax = $"{0} {(position.GrowUp ? ey+eh : ey)}"
                            }
                        }
                    });
                    var iconElement = new CuiElement
                    {
                        Parent = id,
                        Components =
                        {
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0.5",
                                AnchorMax = "0 0.5",
                                OffsetMin = $"{imgP} {-imgS/2}",
                                OffsetMax = $"{imgP+imgS} {imgS/2}"
                            }
                        }
                    };
                    if (!string.IsNullOrEmpty(custom.Icon))
                    {
                        iconElement.Components.Add(new CuiImageComponent
                        {
                            Color = iconColor,
                            Png = PLUGIN.GetIcon(custom.Icon)
                        });
                    }
                    Container.Add(iconElement);
                    Container.Add(new CuiElement
                    {
                        Parent = id,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = leftText.ToUpper(),
                                FontSize = fontSize,
                                Color = fontColor,
                                Align = TextAnchor.MiddleLeft
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "1 1",
                                OffsetMin = $"{left} {0}",
                                OffsetMax = $"{0} {0}"
                            }
                        }
                    });
                    var rightTextE = new CuiElement
                    {
                        Name = $"{id}.value",
                        Parent = id,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Text = !custom.IsDynamic ? rightText : custom.DynamicText(BasePlayer),
                                FontSize = 11,
                                Color = subFontColor,
                                Align = TextAnchor.MiddleRight
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0",
                                AnchorMax = "1 1",
                                OffsetMin = $"{0} {0}",
                                OffsetMax = $"{-padding} {0}"
                            }
                        }
                    };
                    Container.Add(rightTextE);
                    TextElementsByStatusId[custom.Id] = rightTextE;
                    ey += (eh + eg) * (position.GrowUp ? 1 : -1);
                }
                CuiHelper.DestroyUi(BasePlayer, BaseElement.Name);
                CuiHelper.AddUi(BasePlayer, Container);
                HasChange = false;
            }

            public void Redraw(string statusId)
            {
                if (DrawBlocked) { return; }
                var container = new CuiElementContainer();
                CuiElement element;
                if (!TextElementsByStatusId.TryGetValue(statusId, out element)) { return; }
                CustomStatus cs;
                if (!TryGetValue(statusId, out cs)) { return; }
                var textComponent = element.Components[0] as CuiTextComponent;
                textComponent.Text = cs.IsDynamic ? cs.DynamicText.Invoke(BasePlayer) : cs.RightText;
                CuiHelper.DestroyUi(BasePlayer, element.Name);
                container.Add(element);
                CuiHelper.AddUi(BasePlayer, container);
            }
        }
    }
}

namespace Oxide.Plugins
{
    internal partial class CustomStatusFramework : CovalencePlugin
    {
        public string GetIcon(string icon)
        {
            return ImageLibrary?.Call<string>("GetImage", $"{icon}");
        }

        public class NullSafeDictionary<K, V> : Dictionary<K, V> where V : new()
        {
            new public V this[K key]
            {
                get
                {
                    try
                    {
                        return base[key];
                    }
                    catch
                    {
                        base[key] = new V();
                        return base[key];
                    }
                }
                set
                {
                    base[key] = value;
                }
            }
        }
    }
}
