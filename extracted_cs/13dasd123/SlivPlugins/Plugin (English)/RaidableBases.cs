// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿/*
*  < ----- End-User License Agreement ----->
*  
*  You may not copy, modify, merge, publish, distribute, sublicense, or sell copies of This Software without the Developer’s consent
*
*  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, 
*  THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS 
*  BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
*  GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT 
*  LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*
*  Developer: nivex (mswenson82@yahoo.com)
*
*  Copyright © 2020-2023 nivex
*/

#pragma warning disable
using Facepunch;
using Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Oxide.Game.Rust.Cui;
using Oxide.Plugins.RaidableBasesExtensionMethods;
using Rust;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Oxide.Plugins
{
    [Info("Raidable Bases", "nivex", "2.8.5")]
    [Description("Create fully automated raidable bases with npcs.")]
    internal class RaidableBases : RustPlugin
    {
        [PluginReference]
        Plugin
        DangerousTreasures, ZoneManager, BankSystem, IQEconomic, Economics, ServerRewards, GUIAnnouncements, AdvancedAlerts, Archery, Space, Sputnik, PocketDimensions, FauxAdmin,
        Friends, Clans, Kits, TruePVE, NightLantern, Wizardry, NextGenPVE, Imperium, Backpacks, BaseRepair, Notify, SkillTree, ShoppyStock, BuyableBases, XPerience;

        private new const string Name = "RaidableBases";
        private const int targetMask = 8454145;
        private const int visibleMask = 10551553;
        private const int targetMask2 = 10551313;
        private const int manualMask = 1084293393;
        private const int blockLayers = 2228480;
        private const int queueLayers = 2294528; //2228992
        private const int gridLayers = 327936;

        internal ProtectionProperties _elevatorProtection;
        internal ProtectionProperties _turretProtection;
        public AutomatedController Automated;
        public QueueController Queues;
        private Coroutine setupCopyPasteObstructionRadius;
        private float OceanLevel;
        public List<RaidableBase> Raids = new();
        public Dictionary<ulong, DelaySettings> PvpDelay = new();
        public Dictionary<ulong, HumanoidBrain> HumanoidBrains = new();
        private Dictionary<string, SkinInfo> Skins = new();
        private Dictionary<NetworkableId, BMGELEVATOR> _elevators = new();
        public StoredData data = new();
        public TemporaryData temporaryData = new();
        private StringBuilder _sb = new();
        private StringBuilder _sb2 = new();
        private bool wiped;
        private bool buyableEnabled;
        private bool IsUnloading;
        private bool IsShuttingDown;
        private bool bypassRestarting;
        public BuildingTables Buildings = new();
        private bool DebugMode;
        private int despawnLimit = 10;
        private Dictionary<string, ItemDefinition> DeployableItems = new();
        private Dictionary<ItemDefinition, string> ItemDefinitions = new();
        private Dictionary<string, ItemDefinition> _itemModEntity = new();
        private List<string> ExcludedMounts = new() { "beachchair", "boogieboard", "cardtable", "chair", "chippyarcademachine", "computerstation", "drumkit", "microphonestand", "piano", "secretlabchair", "slotmachine", "sofa", "xylophone" };
        private List<string> Blocks = new() { "wall.doorway", "wall", "wall.frame", "wall.half", "wall.low", "wall.window", "foundation.triangle", "foundation", "wall.external.high.wood", "wall.external.high.stone", "wall.external.high.ice", "floor.triangle.frame", "floor.triangle", "floor.frame" };
        private List<string> TrueDamage = new() { "spikes.floor", "barricade.metal", "barricade.woodwire", "barricade.wood", "wall.external.high.wood", "wall.external.high.stone", "wall.external.high.ice" };
        private List<string> arguments = new() { "add", "remove", "list", "clean", "easy", "med", "medium", "hard", "expert", "nightmare", "0", "1", "2", "3", "4", "toggle", "stability", "maintained", "scheduled" };
        private SkinSettingsImportedWorkshop ImportedWorkshopSkins = new();
        private const float M_RADIUS = 25f;
        private const float CELL_SIZE = 12.5f;
        private List<Coroutine> loadCoroutines = new();
        private Dictionary<string, PasteData> _pasteData = new();
        private readonly IPlayer _consolePlayer = new Game.Rust.Libraries.Covalence.RustConsolePlayer();
        private List<BaseEntity.Slot> _checkSlots = new() { BaseEntity.Slot.Lock, BaseEntity.Slot.UpperModifier, BaseEntity.Slot.MiddleModifier, BaseEntity.Slot.LowerModifier };

        public class PasteData
        {
            public bool valid;
            public float radius;
            public List<Vector3> foundations;
            public PasteData() { }
        }

        private List<RankedRecord> RankedRecords = new()
        {
            new("raidablebases.ladder.easy", "raideasy", RaidableMode.Easy),
            new("raidablebases.ladder.medium", "raidmedium", RaidableMode.Medium),
            new("raidablebases.ladder.hard", "raidhard", RaidableMode.Hard),
            new("raidablebases.ladder.expert", "raidexpert", RaidableMode.Expert),
            new("raidablebases.ladder.nightmare", "raidnightmare", RaidableMode.Nightmare),
            new("raidablebases.th", "raidhunter", RaidableMode.Points),
        };

        public enum SphereColor { None, Blue, Cyan, Green, Magenta, Purple, Red, Yellow }

        public enum RaidableType { None, Manual, Scheduled, Purchased, Maintained, Grid }

        public enum RaidableMode { Disabled = -1, Easy = 0, Medium = 1, Hard = 2, Expert = 3, Nightmare = 4, Points = 8888, Random = 9999 }

        public enum AlliedType { All, Clan, Friend, Team }

        public enum CacheType { Close, Delete, Generic, Temporary, Privilege, Seabed, Submerged }

        public enum ConstructionType { Barricade, Ladder, Any }

        public class TemporaryData
        {
            public HashSet<ulong> NetworkIdentifiers = new();
            internal bool IsLoaded;
            public TemporaryData() { }
        }

        public class StoredData
        {
            public RotationCycle Cycle = new();
            public Dictionary<string, Lockout> Lockouts = new();
            public Dictionary<string, PlayerInfo> Players = new();
            public Dictionary<ulong, BuyableInfo> BuyableCooldowns = new();
            public DateTime RaidTime = DateTime.MinValue;
            public int TotalEvents;
            public StoredData() { }
        }

        public class RandomBase
        {
            public string username;
            public ulong userid;
            public IPlayer user;
            public BasePlayer owner;
            public PasteData pasteData;
            public string BaseName;
            public Vector3 Position;
            public BaseProfile Profile;
            public RaidableType type;
            public RaidableSpawns spawns;
            public Payments payments = new();
            public float heightAdj;
            public float typeDistance;
            public float protectionRadius;
            public float safeRadius;
            public float buildRadius;
            public float baseHeight;
            public int attempts;
            public bool autoHeight;
            public bool stability;
            public bool checkTerrain;
            public bool Sorted;
            public RaidableBases Instance;
            public RaidableBase raid;
            public HashSet<ulong> members = new();

            public bool IsTeleportPending(BasePlayer player, Vector3 v)
            {
                return type == RaidableType.Purchased && options.CustomSpawns.BuyableTeleportPositions.Count > 0 && player.HasPermission("raidablebases.buyraid.prefabteleport") && options.CustomSpawns.HasTeleportPositionAt(v);
            }

            public RandomBase(RaidableBases Instance, string BaseName, BaseProfile Profile, Vector3 Position, RaidableType Type, RaidableSpawns spawns, Payments payments)
            {
                this.Instance = Instance;
                this.BaseName = BaseName;
                this.Profile = Profile;
                this.Position = Position;
                this.type = Type;
                this.spawns = spawns ??= new(Instance);
                this.payments = payments ??= new();
                pasteData = Instance.GetPasteData(BaseName);
            }

            public BuildingOptions options => Profile.Options;
            public bool isBuyableEvent => payments.position != Vector3.zero;
            public bool hasSpawns => spawns.Spawns.Count > 0 || spawns.Seabed.Count > 0 && options.Water.SpawnOnSeabed;
            public void TrySortByDistance()
            {
                if (!Sorted && isBuyableEvent && Instance.config.Settings.Buyable.Closest)
                {
                    Instance.Message(owner, "BuyBaseLocate");
                    var positions = spawns.GetLocations(options.Water.CacheType).ToList();
                    positions.Sort((x, y) => (x.Location - payments.position).sqrMagnitude.CompareTo((y.Location - payments.position).sqrMagnitude));
                    if (options.Water.CacheType == CacheType.Seabed)
                    {
                        spawns.Seabed = new(positions);
                    }
                    else spawns.Spawns = new(positions);
                    Sorted = true;
                }
                else Sorted = false;
            }
        }

        public class BackpackData
        {
            public DroppedItemContainer backpack;
            public BasePlayer _player;
            public ulong userID;
            public BasePlayer player => _player = ((bool)_player ? _player : RustCore.FindPlayerById(userID));
        }

        public class BuyableInfo
        {
            public string Easy, Medium, Hard, Expert, Nightmare;
            public string Get(RaidableMode mode) => mode switch { RaidableMode.Easy => Easy, RaidableMode.Medium => Medium, RaidableMode.Hard => Hard, RaidableMode.Expert => Expert, _ => Nightmare };
            private static double GetTimeRemaining(RaidableBases m, ulong userid, RaidableMode mode)
            {
                if (m.config.Settings.Buyable.Cooldowns.Get(mode).Cooldown <= 0 || !m.data.BuyableCooldowns.TryGetValue(userid, out var bi) || !DateTime.TryParse(bi.Get(mode), out var date))
                    return 0;

                return Math.Max(0, date.Subtract(DateTime.Now).TotalSeconds);
            }
            public static double GetTimeRemaining(RaidableBases m, BasePlayer buyer, RaidableMode mode, bool message)
            {
                double time = GetTimeRemaining(m, buyer.userID, mode);
                if (time > 0 && message)
                {
                    m.Message(buyer, "BuyCooldown", Math.Ceiling(time));
                }
                return time;
            }
            public static bool HasTimeRemaining(RaidableBases m, ulong userid)
            {
                return GetRaidableModes().Exists(mode => GetTimeRemaining(m, userid, mode) > 0);
            }
        }

        public class TimeSettings
        {
            public RaidableMode mode;
            public Timer Timer;
            public float time;
            public void Destroy()
            {
                if (Timer != null && !Timer.Destroyed)
                {
                    Timer.Callback();
                    Timer.Destroy();
                }
            }
        }

        public class DelaySettings : TimeSettings
        {
            public RaidableBase raid;
            public bool AllowPVP;
        }

        public class MountInfo
        {
            public Vector3 position;
            public float radius;
            public BaseMountable m;
            public bool IsClaimed => !m || m.IsDestroyed || m.transform == null || m.GetMounted() != null || !InRange2D(m.transform.position, position, radius);
        }

        public class RaidEntity
        {
            public RaidableBase raid;
            public BaseEntity entity;
            public RaidEntity(RaidableBase raid, BaseEntity entity)
            {
                this.raid = raid;
                this.entity = entity;
            }
        }

        public class SkinInfo
        {
            public List<ulong> skins = new(), workshopSkins = new(), importedWorkshopSkins = new(), allSkins = new();
        }

        public class Lockout
        {
            public Dictionary<RaidableMode, DateTime> Levels = new();

            public bool Any() => Levels.Exists(level => level.Value > DateTime.Now);

            public double Get(RaidableMode mode)
            {
                if (Levels.TryGetValue(mode, out var level) && level > DateTime.Now)
                {
                    return level.Subtract(DateTime.Now).TotalSeconds;
                }
                Levels.Remove(mode);
                return 0;
            }

            public void Set(RaidableMode mode, double time)
            {
                if (!Levels.ContainsKey(mode))
                {
                    Levels[mode] = DateTime.Now.AddSeconds(time);
                }
            }
        }

        public class RankedRecord
        {
            public string Permission;
            public string Group;
            public RaidableMode Mode;
            public RankedRecord(string permission, string group, RaidableMode mode)
            {
                (Permission, Group, Mode) = (permission, group, mode);
            }
        }

        public class RaidableSpawnLocation
        {
            public List<Vector3> Surroundings = new();
            public Vector3 Location;
            public float WaterHeight;
            public float TerrainHeight;
            public float SpawnHeight;
            public float Radius;
            public bool AutoHeight;
            public RaidableSpawnLocation(Vector3 location)
            {
                Location = location;
            }
        }

        public class ZoneInfo
        {
            internal string ZoneId;
            internal Vector3 Position;
            internal Vector3 Size;
            internal Vector3 extents;
            internal Matrix4x4 m;
            internal float Distance;
            internal bool IsBlocked;

            public ZoneInfo(string zoneID, Vector3 position, object radius, object size, bool isBlocked, float dist)
            {
                (IsBlocked, ZoneId, Position) = (isBlocked, zoneID, position);

                dist = Mathf.Max(dist, 100f);

                if (radius is float r)
                {
                    Distance = r + M_RADIUS + dist;
                }

                if (size is Vector3 v && v != Vector3.zero)
                {
                    Size = v + new Vector3(dist, Position.y + 100f, dist);
                    extents = Size * 0.5f;
                    m = Matrix4x4.TRS(Position, default, Vector3.one);
                }
            }

            public bool IsPositionInZone(Vector3 a)
            {
                if (Size != Vector3.zero)
                {
                    Vector3 v = m.inverse.MultiplyPoint3x4(a);

                    return v.x <= extents.x && v.x > -extents.x && v.y <= extents.y && v.y > -extents.y && v.z <= extents.z && v.z > -extents.z;
                }
                return InRange2D(Position, a, Distance);
            }
        }

        public class BaseProfile
        {
            public List<LootItem> BaseLootList = new();
            public BuildingOptions Options = new();
            public RaidableSpawns Spawns;
            public string ProfileName;
            public RaidableBases Instance;

            public BaseProfile(RaidableBases instance)
            {
                Instance = instance;
                Spawns = new(instance);
                Options.AdditionalBases = new();
                Options.NPC.SetAccuracy(Options.Mode);
            }

            public BaseProfile(RaidableBases instance, BuildingOptions options, string name)
            {
                Spawns = new(instance);
                Instance = instance;
                Options = options;
                ProfileName = name;
            }

            public static BaseProfile Clone(BaseProfile profile)
            {
                return new BaseProfile(profile.Instance)
                {
                    BaseLootList = profile.BaseLootList,
                    Options = profile.Options.Clone(),
                    ProfileName = profile.ProfileName,
                    Spawns = profile.Spawns
                };
            }
        }

        internal class BuildingTables
        {
            public Dictionary<RaidableMode, List<LootItem>> DifficultyLootLists = new();
            public Dictionary<DayOfWeek, List<LootItem>> WeekdayLootLists = new();
            public Dictionary<string, BaseProfile> Profiles = new();
            public List<string> Removed = new();

            public bool IsConfigured(string baseName) => Profiles.Exists(m => m.Key == baseName || m.Value.Options.AdditionalBases.ContainsKey(baseName));

            public bool TryGetValue(string baseName, out BaseProfile profile)
            {
                profile = Profiles.FirstOrDefault(m => m.Key == baseName || m.Value.Options.AdditionalBases.ContainsKey(baseName)).Value;
                return profile != null;
            }

            public void Remove(string baseName)
            {
                if (Profiles.Remove(baseName) || Profiles.Values.Exists(m => m.Options.AdditionalBases.Remove(baseName)))
                {
                    Removed.Add(baseName);
                }
            }
        }

        public GridControllerManager GridController = new();

        public class GridControllerManager
        {
            internal RaidableBases Instance;
            internal Dictionary<RaidableType, RaidableSpawns> Spawns = new();
            internal Coroutine gridCoroutine;
            internal Coroutine fileCoroutine;
            internal float gridTime;

            public SpawnsControllerManager SpawnsController => Instance.SpawnsController;
            public StoredData data => Instance.data;
            public Configuration config => Instance.config;
            public double GetRaidTime() => data.RaidTime.Subtract(DateTime.Now).TotalSeconds;

            public void StartAutomation()
            {
                if (Instance.Automated.IsScheduledEnabled)
                {
                    if (data.RaidTime != DateTime.MinValue && GetRaidTime() > config.Settings.Schedule.IntervalMax)
                    {
                        data.RaidTime = DateTime.MinValue;
                    }

                    Instance.Automated.StartCoroutine(RaidableType.Scheduled);
                }

                if (Instance.Automated.IsMaintainedEnabled)
                {
                    Instance.Automated.StartCoroutine(RaidableType.Maintained);
                }
            }

            private IEnumerator LoadFiles()
            {
                Instance.Buildings = new();
                yield return Instance.LoadTables();
                yield return Instance.LoadProfiles();
                if (Instance.Buildings.Profiles.Count == 0)
                {
                    CriticalError();
                    yield break;
                }
                Dictionary<string, int> custom = new();
                foreach (var prefab in World.Serialization.world.prefabs)
                {
                    if (StringPool.toString.TryGetValue(prefab.id, out var fullname))
                    {
                        TryAddCustomSpawn(prefab, fullname, new(prefab.position.x, prefab.position.y, prefab.position.z), custom);
                    }
                }
                foreach (var (type, amount) in custom) Puts($"Loaded {amount} custom spawns from {type}");
                yield return CoroutineEx.waitForSeconds(5f);
                Instance.IsSpawnerBusy = false;
                StartAutomation();
            }

            public void Setup()
            {
                if (Spawns.Count >= 5)
                {
                    fileCoroutine = ServerMgr.Instance.StartCoroutine(LoadFiles());
                    return;
                }

                StopCoroutine();
                gridCoroutine = ServerMgr.Instance.StartCoroutine(GenerateGrid());
            }

            public void StopCoroutine()
            {
                if (gridCoroutine != null)
                {
                    ServerMgr.Instance.StopCoroutine(gridCoroutine);
                    gridCoroutine = null;
                }
                if (fileCoroutine != null)
                {
                    ServerMgr.Instance.StopCoroutine(fileCoroutine);
                    fileCoroutine = null;
                }
            }

            private void CriticalError(string text = "No valid profiles exist!")
            {
                if (Instance.profileErrors.Count > 0)
                {
                    Puts("Json errors found in:");
                    Instance.profileErrors.ForEach(str => Puts(str));
                }
                Puts("ERROR: Grid has failed initialization. {0}", text);
                Interface.Oxide.NextTick(() => gridCoroutine = null);
            }

            public bool BadFrameRate;

            private IEnumerator GenerateGrid() // Credits to Jake_Rich for helping me with this!
            {
                yield return CoroutineEx.waitForSeconds(0.1f);

                while (Performance.report.frameRate < 15 && ConVar.FPS.limit > 15)
                {
                    BadFrameRate = true;

                    yield return CoroutineEx.waitForSeconds(1f);
                }

                BadFrameRate = false;

                Stopwatch gridStopwatch = Stopwatch.StartNew();
                RaidableSpawns spawns = Spawns[RaidableType.Grid] = new(Instance);

                gridTime = Time.realtimeSinceStartup;
                Instance.Buildings = new();

                yield return Instance.LoadTables();
                yield return Instance.LoadProfiles();
                yield return SpawnsController.SetupMonuments();

                if (Instance.Buildings.Profiles.Count == 0)
                {
                    gridStopwatch.Stop();
                    CriticalError();
                    yield break;
                }

                var spawnOnSeabed = false;
                var minPos = (int)(World.Size / -2f);
                var maxPos = (int)(World.Size / 2f);
                var maxProtectionRadius = -10000f;
                var minProtectionRadius = 10000f;
                var maxWaterDepthSeabed = 0f;
                var minWaterDepthSeabed = 0f;
                var maxWaterDepth = 0f;
                var landLevel = 0.5f;
                var checks = 0;

                foreach (var profile in Instance.Buildings.Profiles.Values)
                {
                    if (profile.Options.Water.Seabed > 0f) spawnOnSeabed = true;

                    maxProtectionRadius = Mathf.Max(profile.Options.ProtectionRadii.Max(), maxProtectionRadius);

                    minProtectionRadius = Mathf.Min(profile.Options.ProtectionRadii.Min(), minProtectionRadius);

                    maxWaterDepthSeabed = Mathf.Min(maxWaterDepthSeabed, profile.Options.Water.MaximumSeabedWaterDepth);

                    minWaterDepthSeabed = Mathf.Min(minWaterDepthSeabed, profile.Options.Water.MinimumSeabedWaterDepth);

                    maxWaterDepth = Mathf.Max(maxWaterDepth, profile.Options.Water.WaterDepth);

                    landLevel = Mathf.Max(Mathf.Clamp(profile.Options.LandLevel, 0.5f, 3f), landLevel);
                }

                if (!config.Settings.Management.AllowOnBeach && !config.Settings.Management.AllowInland && !spawnOnSeabed)
                {
                    gridStopwatch.Stop();
                    CriticalError("Spawn options for beach, inland and seabed are disabled!");
                    yield break;
                }

                var blockedPositions = config.Settings.Management.BlockedPositions.ToList();
                var zero = blockedPositions.Find(x => x.position == Vector3.zero);

                if (zero == null)
                {
                    blockedPositions.Add(zero = new(Vector3.zero, 200f));
                }

                if (zero.radius < 200f)
                {
                    zero.radius = 200f;
                }

                var wtObj = Interface.Oxide.CallHook("GetGridWaitTime");
                var waitTime = CoroutineEx.waitForSeconds(wtObj is float w ? w : 0.0035f);
                var threshold = Interface.Oxide.CallHook("GetGridWaitThreshold") is int th ? th : 25;
                var prefabs = config.Settings.Management.BlockedPrefabs.ToDictionary(pair => pair.Key, pair => pair.Value);
                var blockedMapPrefabs = Pool.Get<Dictionary<Vector3, float>>();
                var custom = new Dictionary<string, int>();

                prefabs.Remove("test_prefab");
                prefabs.Remove("test_prefab_2");

                foreach (var prefab in World.Serialization.world.prefabs)
                {
                    if (!StringPool.toString.TryGetValue(prefab.id, out var fullname))
                    {
                        continue;
                    }
                    Vector3 v = new(prefab.position.x, prefab.position.y, prefab.position.z);
                    if (prefabs.Count > 0 && prefabs.TryGetValue(GetFileNameWithoutExtension(fullname), out var dist))
                    {
                        blockedMapPrefabs[v] = dist;
                    }
                    TryAddCustomSpawn(prefab, fullname, v, custom);
                }

                bool hasBlockedMapPrefabs = blockedMapPrefabs.Count > 0;
                bool hasBlockedPositions = blockedPositions.Count > 0;
                double totalPoints = Math.Pow((maxPos - minPos) / CELL_SIZE, 2);
                double step = totalPoints / 4.0;
                int stepCounter = 0;
                int progress = 0;

                for (float x = minPos; x < maxPos; x += CELL_SIZE)
                {
                    for (float z = minPos; z < maxPos; z += CELL_SIZE)
                    {
                        progress++;
                        if (++stepCounter >= step)
                        {
                            Puts($"{Math.Round((progress / totalPoints) * 100.0)}% loaded ({spawns.Spawns.Count + spawns.Seabed.Count} potential points)");
                            stepCounter = 0;
                        }

                        var position = new Vector3(x, 0f, z);

                        if (hasBlockedPositions && blockedPositions.Exists(a => InRange2D(position, a.position, a.radius)))
                        {
                            continue;
                        }

                        position.y = SpawnsController.GetSpawnHeight(position);

                        if (hasBlockedMapPrefabs && SpawnsController.IsBlockedByMapPrefab(blockedMapPrefabs, position))
                        {
                            continue;
                        }

                        SpawnsController.ExtractLocation(spawns, position, landLevel, minProtectionRadius, maxProtectionRadius, minWaterDepthSeabed, maxWaterDepthSeabed, maxWaterDepth, spawnOnSeabed);

                        if (++checks >= threshold)
                        {
                            checks = 0;
                            yield return waitTime;
                        }
                    }
                }

                blockedMapPrefabs.ResetToPool();
                Instance.IsSpawnerBusy = false;
                Instance.GridController.StartAutomation();
                Instance.Queues.Messages.Clear();
                gridStopwatch.Stop();

                Puts(Instance.mx("Initialized Grid", null, Math.Floor(gridStopwatch.Elapsed.TotalSeconds), gridStopwatch.Elapsed.Milliseconds, World.Size, spawns.Spawns.Count));
                if (spawns.Seabed.Count > 0) Puts(Instance.mx("Initialized Grid Sea", null, spawns.Seabed.Count));
                foreach (var (type, amount) in custom) Puts($"Loaded {amount} custom spawns from {type}");
                gridCoroutine = null;
            }

            public void TryAddCustomSpawn(ProtoBuf.PrefabData prefab, string fullname, Vector3 v, Dictionary<string, int> custom)
            {
                foreach (var (name, profile) in Instance.Buildings.Profiles)
                {
                    if (profile.Options.CustomSpawns.ShouldAdd(profile, prefab, fullname, v))
                    {
                        if (custom.ContainsKey(name))
                        {
                            custom[name]++;
                        }
                        else custom[name] = 1;
                    }
                }
            }

            public void LoadSpawns()
            {
                Spawns = new();
                Spawns.Add(RaidableType.Grid, new(Instance));

                LoadSpawnsForType(RaidableType.Manual, config.Settings.Manual.SpawnsFile, "LoadedManual");
                LoadSpawnsForType(RaidableType.Scheduled, config.Settings.Schedule.SpawnsFile, "LoadedScheduled");
                LoadSpawnsForType(RaidableType.Maintained, config.Settings.Maintained.SpawnsFile, "LoadedMaintained");
                LoadSpawnsForType(RaidableType.Purchased, config.Settings.Buyable.SpawnsFile, "LoadedBuyable");
            }

            private void LoadSpawnsForType(RaidableType type, string spawnsFile, string key)
            {
                if (SpawnsFileValid(spawnsFile))
                {
                    var spawns = GetSpawnsLocations(spawnsFile);

                    if (spawns.Count > 0)
                    {
                        Puts(Instance.mx(key, null, spawns.Count));
                        Spawns[type] = new(Instance, spawns);
                    }
                }
            }

            public bool SpawnsFileValid(string spawnsFile)
            {
                if (string.IsNullOrEmpty(spawnsFile) || spawnsFile.Equals("none", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return Instance.FileExists($"SpawnsDatabase{Path.DirectorySeparatorChar}{spawnsFile}");
            }

            public HashSet<RaidableSpawnLocation> GetSpawnsLocations(string spawnsFile)
            {
                try
                {
                    return new(Interface.Oxide.DataFileSystem.ReadObject<Spawnfile>($"SpawnsDatabase{Path.DirectorySeparatorChar}{spawnsFile}").spawnPoints.Values.Select(value => new RaidableSpawnLocation(value.ToString().ToVector3())));
                }
                catch
                {
                    Puts("Invalid spawns file: {0}", spawnsFile);

                    return new();
                }
            }
        }

        private class Spawnfile
        {
            public Dictionary<string, object> spawnPoints = new();
        }

        public class QueueController
        {
            internal YieldInstruction instruction0;
            internal YieldInstruction instruction1;
            internal Queue<RandomBase> queue = new();
            internal DebugMessages Messages = new();
            internal Coroutine _coroutine;
            internal int spawnChecks;
            internal bool Paused;
            internal RaidableBases Instance;
            internal Configuration config => Instance.config;
            internal SpawnsControllerManager SpawnsController => Instance.SpawnsController;
            internal bool Any => queue.Count > 0;

            private void Message(BasePlayer player, string key, params object[] args) => Instance.Message(player, key, args);

            private string mx(string key, string id = null, params object[] args) => Instance.mx(key, id, args);

            public class DebugMessages
            {
                internal Dictionary<string, Info> _elements = new();
                internal RaidableBases _instance;
                internal bool _logToFile;
                internal IPlayer _user;

                public class Info
                {
                    public int Amount = 1;
                    public List<string> Values = new();
                    public override string ToString() => Values.Count > 0 ? $": {string.Join(", ", Values)}" : string.Empty;
                }

                public string Add(string element, object obj = null)
                {
                    if (string.IsNullOrEmpty(element))
                    {
                        return null;
                    }
                    if (!_elements.TryGetValue(element, out var info))
                    {
                        if (_elements.Count >= 20)
                        {
                            _elements.Remove(_elements.ElementAt(0).Key);
                        }
                        _elements[element] = info = new();
                    }
                    else info.Amount++;
                    if (obj == null)
                    {
                        return element;
                    }
                    string value = obj.ToString().Replace("(", "").Replace(")", "").Replace(",", "");
                    if (!info.Values.Contains(value))
                    {
                        if (info.Values.Count >= 5)
                        {
                            info.Values.RemoveAt(0);
                        }
                        info.Values.Add(value);
                    }
                    return $"{element}: {value}";
                }
                public void Clear()
                {
                    _elements.Clear();
                }
                public bool Any()
                {
                    return _elements.Count > 0;
                }
                public void PrintAll(IPlayer user = null)
                {
                    if (_elements.Count > 0 && _instance.DebugMode)
                    {
                        foreach (var (key, info) in _elements)
                        {
                            PrintInternal(user, $"{info.Amount}x - {key}{info.ToString()}");
                        }
                        Clear();
                    }
                }
                private bool PrintInternal(IPlayer user, string message)
                {
                    if (!string.IsNullOrEmpty(message) && _instance.DebugMode)
                    {
                        if (_logToFile)
                        {
                            _instance.LogToFile("debug", message, _instance, true);
                        }
                        if (user == null)
                        {
                            Puts("DEBUG: {0}", message);
                        }
                        else user.Reply($"DEBUG: {message}");
                        return true;
                    }
                    return false;
                }
                public void Log(string baseName, string message)
                {
                    _instance?.Buildings?.Remove(baseName);
                    _instance.IsSpawnerBusy = false;
                    Print(message);
                    Puts(message);
                }
                public bool Print(string message)
                {
                    Print(_user, message, null);
                    return false;
                }
                public void Print(string message, object obj)
                {
                    Print(_user, message, obj);
                }
                public void Print(IPlayer user, string message, object obj)
                {
                    if (!PrintInternal(user, obj == null ? message : $"{message}: {obj}"))
                    {
                        Add(message, obj);
                    }
                }
                public void PrintLast(string id = null)
                {
                    if (_elements.Count > 0 && _instance.DebugMode)
                    {
                        PrintInternal(_user, GetLast(id));
                    }
                }
                public string GetLast(string id = null)
                {
                    if (_elements.Count == 0)
                    {
                        return _instance.m("CannotFindPosition", id);
                    }
                    var (key, info) = _elements.ElementAt(_elements.Count - 1);
                    _elements.Remove(key);
                    return $"{info.Amount}x - {key}{info.ToString()}";
                }
            }

            public QueueController(RaidableBases instance)
            {
                Messages._instance = Instance = instance;
                Messages._logToFile = instance.config.LogToFile;
                spawnChecks = Mathf.Clamp(instance.config.Settings.Management.SpawnChecks, 1, 500);
                instruction0 = CoroutineEx.waitForSeconds(0.025f);
                instruction1 = CoroutineEx.waitForSeconds(1f);
            }

            public void StartCoroutine()
            {
                StopCoroutine();
                _coroutine = ServerMgr.Instance.StartCoroutine(FindEventPosition());
            }

            public void StopCoroutine()
            {
                if (_coroutine != null)
                {
                    ServerMgr.Instance.StopCoroutine(_coroutine);
                    _coroutine = null;
                }

                queue.ToList().ForEach(rb => rb.payments.Refund());
                queue.Clear();
            }

            public void Add(RandomBase sp)
            {
                if (!queue.Contains(sp))
                {
                    queue.Enqueue(sp);
                }
            }

            private void Spawn(RandomBase rb, Vector3 position)
            {
                if (!Instance.IsUnloading)
                {
                    rb.Position = position;

                    if (Instance.PasteBuilding(rb))
                    {
                        Instance.IsSpawnerBusy = true;

                        CompletePurchase(rb);
                        Teleport(rb);
                    }
                }
            }

            private void CompletePurchase(RandomBase rb)
            {
                if (rb.type == RaidableType.Purchased)
                {
                    if (!rb.owner.IsKilled())
                    {
                        var grid = Instance.FormatGridReference(rb.owner, rb.Position);

                        if (rb.owner.HasPermission("raidablebases.despawn.buyraid"))
                        {
                            if (config.Settings.Buyable.Refunds.Enabled)
                            {
                                Message(rb.owner, "BuyRefundableBaseSpawnedAt", rb.Position, grid, config.Settings.EventCommand, config.Settings.Buyable.Refunds.Percentage);
                            }
                            else Message(rb.owner, "BuyCancellationsBaseSpawnedAt", rb.Position, grid, config.Settings.EventCommand);
                        }
                        else Message(rb.owner, "BuyBaseSpawnedAt", rb.Position, grid);

                        if (config.EventMessages.AnnounceBuy)
                        {
                            foreach (var target in BasePlayer.activePlayerList)
                            {
                                Message(target, "BuyBaseAnnouncement", rb.owner.displayName, rb.Position, Instance.FormatGridReference(target, rb.Position));
                            }
                        }
                    }

                    Puts(mx("BuyBaseAnnouncementConsole", null, rb.username, rb.options.Mode, rb.BaseName, rb.Position, Instance.PositionToGrid(rb.Position)));

                    config.Settings.Buyable.Cooldowns.Set(Instance, rb.members, rb.userid, rb.options.Mode, true);
                }
            }

            private void Teleport(RandomBase rb)
            {
                if (rb.user != null && rb.user.IsAdmin && rb.user.IsConnected && !rb.IsTeleportPending(rb.user.Player(), rb.Position))
                {
                    rb.user.Teleport(rb.Position.x, rb.Position.y, rb.Position.z);
                }
            }

            private bool CanBypassPause(RandomBase rb)
            {
                if (rb.type == RaidableType.Purchased)
                {
                    return rb.userid.HasPermission("raidablebases.canbypass");
                }
                if (rb.type == RaidableType.Manual)
                {
                    return rb.user != null;
                }
                return false;
            }

            private IEnumerator FindEventPosition()
            {
                int checks = 0;

                while (!Instance.IsUnloading)
                {
                    if (++checks >= spawnChecks)
                    {
                        yield return instruction0;
                        checks = 0;
                    }

                    if (!queue.TryPeek(out var spq))
                    {
                        yield return instruction1;
                        continue;
                    }

                    if (Instance.Buildings.Removed.Contains(spq.BaseName))
                    {
                        if (spq.type == RaidableType.Scheduled)
                        {
                            Instance.data.RaidTime = DateTime.Now.AddSeconds(1f);
                        }
                        queue.Dequeue();
                        continue;
                    }

                    if (spq.Position != Vector3.zero)
                    {
                        Spawn(spq, spq.Position);
                        queue.Dequeue();
                        yield return instruction1;
                        continue;
                    }

                    if (!spq.isBuyableEvent && queue.Exists(sp => sp.isBuyableEvent))
                    {
                        queue.Enqueue(queue.Dequeue());
                        continue;
                    }

                    spq.TrySortByDistance();
                    spq.spawns.Check();

                    while (spq.hasSpawns)
                    {
                        if (++checks >= spawnChecks)
                        {
                            checks = 0;
                            yield return instruction0;
                        }

                        if (Instance.IsSpawnerBusy || Paused && !CanBypassPause(spq))
                        {
                            yield return instruction1;
                            continue;
                        }

                        spq.attempts++;

                        var rsl = spq.spawns.GetRandom(spq.options.Water, spq.isBuyableEvent);
                        var v = rsl.Location;

                        if (!TopologyChecks(spq, v))
                        {
                            continue;
                        }

                        v.y = GetAdjustedHeight(spq, v);

                        if (IsTooClose(spq, v))
                        {
                            continue;
                        }

                        if (IsAreaManuallyBlocked(spq, v))
                        {
                            continue;
                        }

                        if (CanSpawnCustom(spq, spq.type, v, spq.options.CustomSpawns.Ignore, spq.options.CustomSpawns.SafeRadius))
                        {
                            yield return instruction1;
                            break;
                        }

                        if (CanSpawnCustom(spq, RaidableType.Maintained, v, config.Settings.Maintained.Ignore, config.Settings.Maintained.SafeRadius))
                        {
                            yield return instruction1;
                            break;
                        }

                        if (CanSpawnCustom(spq, RaidableType.Scheduled, v, config.Settings.Schedule.Ignore, config.Settings.Schedule.SafeRadius))
                        {
                            yield return instruction1;
                            break;
                        }

                        if (CanSpawnCustom(spq, RaidableType.Purchased, v, config.Settings.Buyable.Ignore, config.Settings.Buyable.SafeRadius))
                        {
                            yield return instruction1;
                            break;
                        }

                        if (IsSubmerged(spq, rsl, v))
                        {
                            continue;
                        }

                        if (!IsAreaSafe(spq, rsl, v))
                        {
                            continue;
                        }

                        if (!spq.pasteData.valid)
                        {
                            yield return SetupCopyPasteRadius(spq);
                        }

                        if (spq.pasteData.foundations.IsNullOrEmpty())
                        {
                            Instance.Buildings.Remove(spq.BaseName);
                            break;
                        }

                        if (IsObstructed(spq, v))
                        {
                            continue;
                        }

                        Spawn(spq, v);
                        yield return instruction1;
                        break;
                    }

                    if (Instance.IsGridLoading() && !Instance.IsSpawnerBusy)
                    {
                        yield return instruction1;
                        continue;
                    }

                    queue.Dequeue();
                    CheckSpawner(spq);
                }

                _coroutine = null;
            }

            private void CheckSpawner(RandomBase spq)
            {
                if (!Instance.IsSpawnerBusy)
                {
                    if (spq.type == RaidableType.Manual)
                    {
                        if (spq.user == null || spq.user.IsServer)
                        {
                            Puts(mx("CannotFindPosition"));
                        }
                        else
                        {
                            Message(spq.user.Player(), Instance.Queues.Messages.GetLast(spq.user.Id));
                        }
                    }
                    else if (spq.isBuyableEvent)
                    {
                        spq.payments.Refund();
                        Message(spq.owner, Instance.mx("CannotFindPosition", spq.userid.ToString()));
                    }

                    spq.spawns.TryAddRange();
                    Messages.PrintAll();
                    Instance.data.Cycle.Add(spq.type, spq.options.Mode, spq.BaseName, spq.owner);
                }
            }

            private bool IsObstructed(RandomBase spq, Vector3 v)
            {
                if (!spq.spawns.IsCustomSpawn && SpawnsController.IsObstructed(v, spq.pasteData.radius, Mathf.Clamp(spq.options.LandLevel, -3f, 3f), spq.options.Setup.ForcedHeight, spq.options.Water.SpawnOnSeabed))
                {
                    Messages.Add("Area is obstructed", v);
                    spq.spawns.RemoveNear(v, spq.protectionRadius / 2f, spq.options.Water.SpawnOnSeabed ? CacheType.Seabed : CacheType.Temporary, spq.type);
                    return true;
                }
                return false;
            }

            private IEnumerator SetupCopyPasteRadius(RandomBase spq)
            {
                yield return Instance.SetupCopyPasteObstructionRadius(spq.BaseName, spq.options.ProtectionRadii.Obstruction == -1 ? 0f : GetObstructionRadius(spq.options.ProtectionRadii, RaidableType.None));
            }

            private bool IsAreaSafe(RandomBase spq, RaidableSpawnLocation rsl, Vector3 v)
            {
                if (!SpawnsController.IsAreaSafe(v, spq.safeRadius, spq.buildRadius, spq.pasteData.radius, queueLayers, spq.spawns.IsCustomSpawn, out var cacheType, spq.type, spq.options.CustomSpawns))
                {
                    if (spq.options.Water.SpawnOnSeabed)
                    {
                        cacheType = CacheType.Seabed;
                    }
                    if (cacheType == CacheType.Delete)
                    {
                        spq.spawns.Remove(rsl, cacheType);
                    }
                    else if (cacheType == CacheType.Privilege)
                    {
                        spq.spawns.RemoveNear(v, spq.buildRadius, cacheType, spq.type);
                    }
                    else spq.spawns.RemoveNear(v, spq.safeRadius / 2f, cacheType, spq.type);
                    return false;
                }
                return true;
            }

            private bool IsSubmerged(RandomBase spq, RaidableSpawnLocation rsl, Vector3 v)
            {
                if (!spq.spawns.IsCustomSpawn && spq.options.Setup.ForcedHeight == -1 && spq.options.Water.Seabed <= 0f && SpawnsController.IsSubmerged(spq.options.Water, rsl))
                {
                    Messages.Add("Area is submerged", v);
                    return true;
                }
                return false;
            }

            private bool CanSpawnCustom(RandomBase spq, RaidableType type, Vector3 v, bool ignore, float safeRadius)
            {
                if (spq.type == type && spq.spawns.IsCustomSpawn && (ignore || safeRadius > 0f))
                {
                    if (safeRadius <= 0f)
                    {
                        Messages.Add($"Ignored checks for {spq.type} event", v);
                        Spawn(spq, v);
                        return true;
                    }
                    else spq.safeRadius = safeRadius;
                }
                return false;
            }

            private bool IsTooClose(RandomBase spq, Vector3 v)
            {
                if (spq.typeDistance > 0 && Instance.IsTooClose(v, spq.typeDistance))
                {
                    spq.spawns.RemoveNear(v, spq.options.ProtectionRadius(spq.type), CacheType.Close, spq.type);
                    Messages.Add("Too close (Spawn Bases X Distance Apart)", v);
                    return true;
                }
                return false;
            }

            internal bool IsAreaManuallyBlocked(RandomBase spq, Vector3 v)
            {
                if (!spq.spawns.IsCustomSpawn && config.Settings.Management.BlockedPositions.Exists(x => InRange2D(v, x.position, x.radius)))
                {
                    spq.spawns.RemoveNear(v, spq.options.ProtectionRadius(spq.type), CacheType.Close, spq.type);
                    Messages.Add("Block Spawns At Positions", v);
                    return true;
                }
                return false;
            }

            private float GetAdjustedHeight(RandomBase spq, Vector3 v)
            {
                if (spq.options.Setup.ForcedHeight != -1)
                {
                    return spq.options.Setup.PasteHeightAdjustment + spq.options.Setup.ForcedHeight;
                }
                return v.y + spq.options.Setup.PasteHeightAdjustment;
            }

            private bool TopologyChecks(RandomBase spq, Vector3 v)
            {
                if (!spq.spawns.IsCustomSpawn && !SpawnsController.TopologyChecks(v, spq.protectionRadius, spq.options.Water.SpawnOnSeabed, out var topology))
                {
                    spq.spawns.RemoveNear(v, M_RADIUS, CacheType.Delete, spq.type);
                    Messages.Add($"Blocked on {topology} topology", v);
                    return false;
                }
                return true;
            }
        }

        public class DespawnableEntity
        {
            public BaseNetworkable entity;
            public NetworkableId uid;
            public DespawnableEntity(BaseEntity entity, NetworkableId uid)
            {
                this.entity = entity;
                this.uid = uid;
            }
            public DespawnableEntity(ulong value)
            {
                uid = new(value);
            }
        }

        public class AutomatedController
        {
            internal List<DespawnableEntity> batch = new();
            internal RaidableBases Instance;
            internal YieldInstruction instruction0;
            internal YieldInstruction instruction1;
            internal YieldInstruction instruction5;
            internal YieldInstruction instruction15;
            internal Coroutine _maintainedCoroutine;
            internal Coroutine _scheduledCoroutine;
            internal Coroutine _despawnCoroutine;
            internal bool IsMaintainedEnabled;
            internal bool IsScheduledEnabled;
            internal int _maxOnce;

            internal StoredData data => Instance.data;
            internal Configuration config => Instance.config;
            internal UndoLoopComparer uc => Instance.UndoComparer;

            public AutomatedController(RaidableBases instance, bool a, bool b)
            {
                instruction0 = CoroutineEx.waitForSeconds(0.0025f);
                instruction1 = CoroutineEx.waitForSeconds(1f);
                instruction5 = CoroutineEx.waitForSeconds(5f);
                instruction15 = CoroutineEx.waitForSeconds(15f);
                Instance = instance;
                IsMaintainedEnabled = a;
                IsScheduledEnabled = b;
            }

            public bool Contains(NetworkableId uid) => batch.Count > 0 && batch.Exists(x => x.uid == uid);

            public void Enbatch(DespawnableEntity despawnable)
            {
                if (!Contains(despawnable.uid))
                {
                    batch.Add(despawnable);
                }
            }

            public IEnumerator DespawnTemporaryEntitiesCo()
            {
                yield return CoroutineEx.waitForSeconds(1f);
                int checks = 0;
                batch.Sort(uc.Compare);
                while (batch.Count > 0)
                {
                    DespawnableEntity despawnable = batch[0];
                    batch.Remove(despawnable);
                    Instance.temporaryData.NetworkIdentifiers.Remove(despawnable.uid.Value);
                    despawnable.entity ??= BaseNetworkable.serverEntities.Find(despawnable.uid);
                    Instance.KillEntity(despawnable.entity, despawnable.uid, Instance.UndoSettings, true);
                    if (++checks >= 25)
                    {
                        yield return CoroutineEx.waitForSeconds(0.0025f);
                        checks = 0;
                    }
                }
            }

            public void StartDespawnTemporaryEntities()
            {
                _despawnCoroutine = ServerMgr.Instance.StartCoroutine(DespawnTemporaryEntitiesCo());
            }

            public void DestroyMe()
            {
                if (_despawnCoroutine != null)
                {
                    ServerMgr.Instance.StopCoroutine(_despawnCoroutine);
                }
                StopCoroutine(RaidableType.Scheduled);
                StopCoroutine(RaidableType.Maintained);
            }

            public void StopCoroutine(RaidableType type, IPlayer user = null)
            {
                if (type == RaidableType.Scheduled && _scheduledCoroutine != null)
                {
                    ServerMgr.Instance.StopCoroutine(_scheduledCoroutine);
                    Instance.Message(user, "ReloadScheduleCo");
                    _scheduledCoroutine = null;
                }
                else if (type == RaidableType.Maintained && _maintainedCoroutine != null)
                {
                    ServerMgr.Instance.StopCoroutine(_maintainedCoroutine);
                    Instance.Message(user, "ReloadMaintainCo");
                    _maintainedCoroutine = null;
                }
            }

            public void StartCoroutine(RaidableType type, IPlayer user = null)
            {
                StopCoroutine(type, user);

                if (type == RaidableType.Scheduled ? !IsScheduledEnabled || config.Settings.Schedule.Max <= 0 : !IsMaintainedEnabled || config.Settings.Maintained.Max <= 0)
                {
                    return;
                }

                if (Instance.IsGridLoading() || !Instance.CanContinueAutomation())
                {
                    Instance.timer.Once(1f, () => StartCoroutine(type));
                    return;
                }

                if (type == RaidableType.Scheduled && data.RaidTime == DateTime.MinValue)
                {
                    ScheduleNextAutomatedEvent();
                }

                if (type == RaidableType.Scheduled)
                {
                    Instance.timer.Once(0.2f, () => _scheduledCoroutine = ServerMgr.Instance.StartCoroutine(ScheduleCoroutine()));
                }
                else Instance.timer.Once(0.2f, () => _maintainedCoroutine = ServerMgr.Instance.StartCoroutine(MaintainCoroutine()));
            }

            private IEnumerator MaintainCoroutine()
            {
                float timeBetweenSpawns = Mathf.Max(1f, config.Settings.Maintained.Time);

                while (!Instance.IsUnloading)
                {
                    if (!CanSpawn(RaidableType.Maintained, BasePlayer.activePlayerList.Count, config.Settings.Maintained.PlayerLimit, config.Settings.Maintained.PlayerLimitMax, config.Settings.Maintained.Max, false))
                    {
                        yield return instruction5;
                    }
                    else if (!Instance.Queues.Any)
                    {
                        yield return ProcessEvent(RaidableType.Maintained, timeBetweenSpawns);
                    }
                    else if (Instance.Queues.Messages.Any())
                    {
                        Instance.Queues.Messages.PrintLast();
                    }

                    yield return instruction5;
                }

                _maintainedCoroutine = null;
            }

            private IEnumerator ScheduleCoroutine()
            {
                float timeBetweenSpawns = Mathf.Max(1f, config.Settings.Schedule.Time);

                while (!Instance.IsUnloading)
                {
                    if (CanSpawn(RaidableType.Scheduled, BasePlayer.activePlayerList.Count, config.Settings.Schedule.PlayerLimit, config.Settings.Schedule.PlayerLimitMax, config.Settings.Schedule.Max, true))
                    {
                        while (Instance.Get(RaidableType.Scheduled) < config.Settings.Schedule.Max && MaxOnce())
                        {
                            if (SaveRestore.IsSaving)
                            {
                                Instance.Queues.Messages.Print("Scheduled: Server saving");
                                yield return instruction15;
                            }
                            else if (!Instance.Queues.Any)
                            {
                                yield return ProcessEvent(RaidableType.Scheduled, timeBetweenSpawns);
                            }
                            else if (Instance.Queues.Messages.Any())
                            {
                                Instance.Queues.Messages.PrintLast();
                            }

                            yield return instruction1;
                        }

                        yield return CoroutineEx.waitForSeconds(ScheduleNextAutomatedEvent());
                    }

                    yield return instruction5;
                }

                _scheduledCoroutine = null;
            }

            private IEnumerator ProcessEvent(RaidableType type, float timeBetweenSpawns)
            {
                RaidableMode mode;
                if (!IsModeValid(mode = Instance.GetRandomDifficulty(type)))
                {
                    Instance.Queues.Messages.PrintLast();
                    yield return instruction1;
                    yield break;
                }

                Instance.SpawnRandomBase(type, mode);
                yield return instruction1;
                //yield return new WaitWhile(() => Instance.Queues.Any);

                if (!Instance.IsSpawnerBusy)
                {
                    yield break;
                }

                if (type == RaidableType.Scheduled)
                {
                    _maxOnce++;
                }

                Instance.Queues.Messages.Print($"{type}: Waiting for base to be setup", Instance.IsBusy(out var pastedLocation) ? pastedLocation : (object)null);
                yield return new WaitWhile(() => Instance.IsSpawnerBusy);

                Instance.Queues.Messages.Print($"{type}: Waiting {timeBetweenSpawns} seconds");
                yield return CoroutineEx.waitForSeconds(timeBetweenSpawns);
            }

            private float ScheduleNextAutomatedEvent()
            {
                var raidInterval = Core.Random.Range(config.Settings.Schedule.IntervalMin, config.Settings.Schedule.IntervalMax + 1);

                _maxOnce = 0;
                data.RaidTime = DateTime.Now.AddSeconds(raidInterval);
                Puts(Instance.mx("Next Automated Raid", null, Instance.FormatTime(raidInterval, null), data.RaidTime));
                Instance.Queues.Messages.Print("Scheduled next automated event");

                return (float)raidInterval;
            }

            private bool MaxOnce()
            {
                return config.Settings.Schedule.MaxOnce <= 0 || _maxOnce < config.Settings.Schedule.MaxOnce;
            }

            private bool CanSpawn(RaidableType type, int onlinePlayers, int playerLimit, int playerLimitMax, int maxEvents, bool checkRaidTime)
            {
                if (onlinePlayers < playerLimit)
                {
                    return Instance.Queues.Messages.Print($"{type}: Insufficient amount of players online {onlinePlayers}/{playerLimit}");
                }
                else if (onlinePlayers > playerLimitMax)
                {
                    return Instance.Queues.Messages.Print($"{type}: Too many players online {onlinePlayers}/{playerLimitMax}");
                }
                else if (Instance.IsSpawnerBusy || Instance.IsLoaderBusy)
                {
                    return Instance.Queues.Messages.Print($"{type}: Waiting for a base to finish its task");
                }
                else if (maxEvents > 0 && Instance.Get(type) >= maxEvents)
                {
                    return Instance.Queues.Messages.Print($"{type}: The max amount of events are spawned");
                }
                else if (checkRaidTime && Instance.GridController.GetRaidTime() > 0)
                {
                    return Instance.Queues.Messages.Print($"{type}: Waiting on timer for next event");
                }
                else if (SaveRestore.IsSaving)
                {
                    return Instance.Queues.Messages.Print($"{type}: Server saving");
                }
                else if (!Instance.IsCopyPasteLoaded(out var error))
                {
                    return Instance.Queues.Messages.Print(error);
                }
                else if (Instance.Queues.queue.Exists(sp => sp.type == RaidableType.Purchased))
                {
                    return Instance.Queues.Messages.Print($"{type}: Waiting on purchase in queue first");
                }

                return true;
            }
        }

        public class BMGELEVATOR : FacepunchBehaviour // credits: bmgjet
        {
            internal const string ElevatorPanelName = "RB_UI_Elevator";
            internal Elevator _elevator;
            internal RaycastHit hit;
            internal BaseEntity hitEntity;
            internal RaidableBase raid;
            internal BuildingOptionsElevators options;
            internal Dictionary<ulong, BasePlayer> _UI = new();
            internal bool HasButton;
            internal NetworkableId uid;
            internal int CurrentFloor;
            internal int returnDelay = 60;
            internal float Floors;
            internal float _LiftSpeedPerMetre = 3f;
            internal RaidableBases MyInstance;

            private void Awake()
            {
                _elevator = GetComponent<Elevator>();
                _elevator.LiftSpeedPerMetre = _LiftSpeedPerMetre;
            }

            private void OnDestroy()
            {
                _elevator.SafelyKill();
                _UI.Values.ToList().ForEach(DestroyUi);
                MyInstance?._elevators.Remove(uid);
                try { CancelInvoke(); } catch { }
            }

            public bool ValidPosition => !_elevator.IsKilled() && !_elevator.liftEntity.IsKilled();

            public Vector3 ServerPosition => _elevator.liftEntity.transform.position;

            private Vector3 GetWorldSpaceFloorPosition(int targetFloor)
            {
                int num = _elevator.Floor - targetFloor;
                Vector3 b = Vector3.up * ((float)num * _elevator.FloorHeight);
                b.y -= 1f;
                return base.transform.position - b;
            }

            public void GoToFloor(Elevator.Direction Direction = Elevator.Direction.Down, bool FullTravel = false, int forcedFloor = -1)
            {
                if (_elevator.HasFlag(BaseEntity.Flags.Busy))
                {
                    return;
                }
                int maxFloors = (int)(Floors / 3f);
                if (forcedFloor != -1)
                {
                    int targetFloor = Mathf.RoundToInt((forcedFloor - ServerPosition.y) / 3);
                    if (targetFloor == 0 && CurrentFloor == 0) targetFloor = maxFloors;
                    else if (targetFloor == 0 && CurrentFloor == maxFloors) targetFloor = -maxFloors;
                    CurrentFloor += targetFloor;
                    if (CurrentFloor > maxFloors) CurrentFloor = maxFloors;
                    if (CurrentFloor < 0) CurrentFloor = 0;
                }
                else
                {
                    if (Direction == Elevator.Direction.Up)
                    {
                        CurrentFloor++;
                        if (FullTravel) CurrentFloor = (int)(Floors / _elevator.FloorHeight);
                        if ((CurrentFloor * 3) > Floors) CurrentFloor = (int)(Floors / _elevator.FloorHeight);
                    }
                    else
                    {
                        if (GamePhysics.CheckSphere(ServerPosition - new Vector3(0, 1f, 0), 0.5f, Layers.Mask.Construction | Layers.Server.Deployed, QueryTriggerInteraction.Ignore))
                        {
                            _elevator.Invoke(Retry, returnDelay);
                            return;
                        }

                        CurrentFloor--;
                        if (CurrentFloor < 0 || FullTravel) CurrentFloor = 0;
                    }
                }
                Vector3 worldSpaceFloorPosition = GetWorldSpaceFloorPosition(CurrentFloor);
                if (!GamePhysics.LineOfSight(ServerPosition, worldSpaceFloorPosition, 2097152))
                {
                    if (Direction == Elevator.Direction.Up)
                    {
                        if (!Physics.Raycast(ServerPosition, Vector3.up, out hit, 21f) || (hitEntity = hit.GetEntity()).IsNull())
                        {
                            return;
                        }
                        CurrentFloor = (int)(hitEntity.transform.position.Distance(_elevator.transform.position) / 3);
                        worldSpaceFloorPosition = GetWorldSpaceFloorPosition(CurrentFloor);
                    }
                    else
                    {
                        if (!Physics.Raycast(ServerPosition - new Vector3(0, 2.9f, 0), Vector3.down, out hit, 21f) || (hitEntity = hit.GetEntity()).IsNull() || hitEntity.ShortPrefabName == "foundation" || hitEntity.ShortPrefabName == "elevator.static")
                        {
                            _elevator.Invoke(Retry, returnDelay);
                            return;
                        }
                        CurrentFloor = (int)(hitEntity.transform.position.Distance(_elevator.transform.position) / 3) + 1;
                        worldSpaceFloorPosition = GetWorldSpaceFloorPosition(CurrentFloor);
                    }
                }
                Vector3 v = transform.InverseTransformPoint(worldSpaceFloorPosition);
                float timeToTravel = _elevator.TimeToTravelDistance(Mathf.Abs(_elevator.liftEntity.transform.localPosition.y - v.y));
                LeanTween.moveLocalY(_elevator.liftEntity.gameObject, v.y, timeToTravel);
                _elevator.SetFlag(BaseEntity.Flags.Busy, true, false, true);
                _elevator.liftEntity.ToggleHurtTrigger(true);
                _elevator.Invoke(_elevator.ClearBusy, timeToTravel);
                _elevator.CancelInvoke(ElevatorToGround);
                _elevator.Invoke(ElevatorToGround, timeToTravel + returnDelay);
            }

            private void Retry()
            {
                GoToFloor(Elevator.Direction.Down, true);
            }

            private void ElevatorToGround()
            {
                if (CurrentFloor != 0)
                {
                    if (_elevator.HasFlag(BaseEntity.Flags.Busy))
                    {
                        _elevator.Invoke(ElevatorToGround, 5f);
                        return;
                    }
                    GoToFloor(Elevator.Direction.Down, true);
                }
            }

            public void Init(RaidableBase raid)
            {
                this.raid = raid;
                this.MyInstance = raid.Instance;
                options = raid.Options.Elevators;
                _elevator._maxHealth = options.ElevatorHealth;
                _elevator.InitializeHealth(options.ElevatorHealth, options.ElevatorHealth);

                if (options.Enabled)
                {
                    InvokeRepeating(ShowHealthUI, 10, 1);
                }

                if (HasButton)
                {
                    MyInstance.Subscribe(nameof(OnButtonPress));
                }
            }

            private void ShowHealthUI()
            {
                var players = raid.GetIntruders().Where(player => player.Distance(ServerPosition) <= 3f);
                foreach (var player in _UI.Values.ToList())
                {
                    if (!players.Contains(player)) // || !GamePhysics.LineOfSight(ServerPosition, player.transform.position, 2097152))
                    {
                        DestroyUi(player);
                        _UI.Remove(player.userID);
                    }
                }
                foreach (var player in players)
                {
                    if (!player.IsSleeping()) // && GamePhysics.LineOfSight(ServerPosition, player.transform.position, 2097152))
                    {
                        var translated = MyInstance.mx("Elevator Health", player.UserIDString);
                        var color = MyInstance.UI._(options.PanelColor, options.PanelAlpha.Value);
                        var container = new CuiElementContainer();
                        UiHandler.AddCuiPanel(container, false, color, options.AnchorMin, options.AnchorMax, null, null, MyInstance.UI.ELEVATOR_PARENT, ElevatorPanelName);
                        UiHandler.AddCuiElement(container, $"{translated} {_elevator._health:#.##}/{_elevator._maxHealth}", 16, TextAnchor.MiddleCenter, "1 1 1 1", "0 0", "1 1", null, null, ElevatorPanelName, $"{ElevatorPanelName}_LABEL");
                        DestroyUi(player);
                        CuiHelper.AddUi(player, container);
                        _UI[player.userID] = player;
                    }
                }
            }

            public static void DestroyUi(BasePlayer player) => CuiHelper.DestroyUi(player, ElevatorPanelName);

            private static void CleanElevatorKill(BaseEntity entity)
            {
                if (!entity.IsKilled())
                {
                    entity.transform.position = new(0, -100f, 0);
                    Interface.Oxide.NextTick(entity.SafelyKill);
                }
            }

            public static List<List<BaseEntity>> SplitElevators(List<BaseEntity> source)
            {
                Dictionary<int, List<BaseEntity>> groupedEntities = new();
                foreach (var entity in source)
                {
                    int distance = (int)(entity.transform.position.x * 2f);
                    if (!groupedEntities.TryGetValue(distance, out var group))
                    {
                        groupedEntities[distance] = group = new();
                    }
                    group.Add(entity);
                }
                return groupedEntities.Values.ToList();
            }

            public static Dictionary<Elevator, BMGELEVATOR> FixElevators(RaidableBase raid)
            {
                var bmgs = new Dictionary<Elevator, BMGELEVATOR>();
                var elevators = new List<BaseEntity>();
                bool hasButton = false;

                foreach (BaseEntity entity in raid.Entities.ToList())
                {
                    switch (entity)
                    {
                        case Elevator or ElevatorLift:
                            raid.Entities.Remove(entity);
                            elevators.Add(entity);
                            break;
                        case PressButton _:
                            hasButton = true;
                            break;
                    }
                }

                foreach (var split in SplitElevators(elevators))
                {
                    if (FixElevators(raid.Instance, split, out var bmgELEVATOR) is Elevator elevator)
                    {
                        bmgELEVATOR.HasButton = hasButton;
                        bmgs[elevator] = bmgELEVATOR;
                        raid.AddEntity(elevator);
                    }
                }

                return bmgs;
            }

            public static Elevator FixElevators(RaidableBases instance, List<BaseEntity> elevators, out BMGELEVATOR bmgELEVATOR)
            {
                if (FixElevator(instance, elevators, out bmgELEVATOR) is Elevator elevator)
                {
                    elevators.Add(elevator);
                    return elevator;
                }
                return null;
            }

            public static Elevator FixElevator(RaidableBases instance, List<BaseEntity> elevators, out BMGELEVATOR bmgELEVATOR)
            {
                bmgELEVATOR = null;
                if (elevators.IsNullOrEmpty())
                {
                    return null;
                }
                if (elevators.Count == 1)
                {
                    CleanElevatorKill(elevators[0]);
                    return null;
                }
                Vector3 bottom = new(999f, 999f, 999f);
                Vector3 top = new(-999f, -999f, -999f);
                Quaternion rot = elevators[0].transform.rotation;
                foreach (BaseEntity entity in elevators)
                {
                    if (entity.transform.position.y < bottom.y) bottom = entity.transform.position;
                    if (entity.transform.position.y > top.y) top = entity.transform.position;
                    CleanElevatorKill(entity);
                }
                Elevator elevator = GameManager.server.CreateEntity("assets/prefabs/deployable/elevator/static/elevator.static.prefab", bottom, rot, true) as Elevator;
                if (rot != Quaternion.identity) elevator.transform.rotation = rot;
                elevator.transform.position = bottom;
                elevator.transform.localPosition += new Vector3(0f, 0.25f, 0f);
                bmgELEVATOR = elevator.gameObject.AddComponent<BMGELEVATOR>();
                bmgELEVATOR.MyInstance = instance;
                elevator.Spawn();
                bmgELEVATOR.Floors = top.y - bottom.y;
                Interface.Oxide.NextTick(() =>
                {
                    if (elevator.IsKilled()) return;
                    elevator.baseProtection = instance.GetElevatorProtection();
                    elevator.liftEntity.baseProtection = instance.GetElevatorProtection();
                    RemoveImmortality(elevator.baseProtection, 0.9f, 0f, 0f, 0f, 0f, 0.95f, 0f, 0f, 0f, 0.99f, 0.99f, 0.99f, 0f, 1f, 1f, 0.99f, 0.5f, 0f, 0f, 0f, 0f, 1f, 1f, 1f, 0);
                });
                elevator.SetFlag(BaseEntity.Flags.Reserved1, true, false, true);
                elevator.SetFlag(Elevator.Flag_HasPower, true);
                elevator.SendNetworkUpdateImmediate();
                bmgELEVATOR.uid = elevator.net.ID;
                instance._elevators[elevator.net.ID] = bmgELEVATOR;
                instance.Subscribe(nameof(OnElevatorButtonPress));
                return elevator;
            }

            internal static void RemoveImmortality(ProtectionProperties baseProtection, params float[] obj)
            {
                DamageType[] damageTypes = (DamageType[])Enum.GetValues(typeof(DamageType));

                for (int i = 0; i < damageTypes.Length && i < obj.Length; i++)
                {
                    baseProtection.amounts[(int)damageTypes[i]] = obj[i];
                }
            }

            private HashSet<ulong> _granted = new();

            public bool HasCardPermission(BasePlayer player)
            {
                if (options.RequiredAccessLevel == 0 || _granted.Contains(player.userID) || player.HasPermission("raidablebases.elevators.bypass.card"))
                {
                    return true;
                }

                string shortname = options.RequiredAccessLevel == 1 ? "keycard_green" : options.RequiredAccessLevel == 2 ? "keycard_blue" : "keycard_red";

                if (!(player.inventory.FindItemByItemName(shortname) is Item item) || item.skin != options.SkinID)
                {
                    raid.Message(player, options.RequiredAccessLevel == 1 ? "Elevator Green Card" : options.RequiredAccessLevel == 2 ? "Elevator Blue Card" : options.RequiredAccessLevel == 3 ? "Elevator Red Card" : "Elevator Special Card");
                    return false;
                }

                if (item.GetHeldEntity() is Keycard keycard && keycard.accessLevel == options.RequiredAccessLevel)
                {
                    if (options.RequiredAccessLevelOnce)
                    {
                        _granted.Add(player.userID);
                    }

                    return true;
                }

                raid.Message(player, options.RequiredAccessLevel == 1 ? "Elevator Green Card" : options.RequiredAccessLevel == 2 ? "Elevator Blue Card" : options.RequiredAccessLevel == 3 ? "Elevator Red Card" : "Elevator Special Card");
                return false;
            }

            public bool HasBuildingPermission(BasePlayer player)
            {
                if (!options.RequiresBuildingPermission || player.HasPermission("raidablebases.elevators.bypass.building") || raid.priv.IsKilled() || raid.priv.IsAuthed(player))
                {
                    return true;
                }

                raid.Message(player, "Elevator Privileges");

                return false;
            }
        }

        public class RaidableSpawns
        {
            public HashSet<RaidableSpawnLocation> Seabed = new();
            public HashSet<RaidableSpawnLocation> Spawns = new();
            public HashSet<RaidableSpawnLocation> Garbage = new();
            public Dictionary<CacheType, HashSet<RaidableSpawnLocation>> Cached = new();
            private float lastTryTime;
            public bool IsCustomSpawn;
            public RaidableBases Instance;
            internal Configuration config => Instance.config;
            public SpawnsControllerManager SpawnsController => Instance.SpawnsController;
            public HashSet<RaidableSpawnLocation> Inactive(CacheType cacheType) => GetCache(cacheType);

            public RaidableSpawns(RaidableBases instance, HashSet<RaidableSpawnLocation> spawns)
            {
                Spawns = spawns;
                Instance = instance;
                IsCustomSpawn = true;
            }

            public RaidableSpawns(RaidableBases instance)
            {
                Instance = instance;
            }

            public bool CanBuild(BasePlayer player, Vector3 buildPos, float radius)
            {
                if (IsCustomSpawn)
                {
                    foreach (var rsl in Spawns)
                    {
                        if (InRange(rsl.Location, buildPos, radius))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            public bool Add(RaidableSpawnLocation rsl, CacheType cacheType, HashSet<RaidableSpawnLocation> cache, bool forced)
            {
                if (!forced)
                {
                    switch (cacheType)
                    {
                        case CacheType.Close when Instance.IsTooClose(rsl.Location, Instance.GetDistance(RaidableType.None)):
                        case CacheType.Generic when Instance.EventTerritory(rsl.Location):
                        case CacheType.Submerged when !SetOceanLevel(rsl):
                            return false;
                    }
                }

                return GetLocations(cacheType).Add(rsl);
            }

            public bool SetOceanLevel(RaidableSpawnLocation rsl)
            {
                rsl.WaterHeight = WaterSystem.OceanLevel;
                rsl.Surroundings.Clear();
                return true;
            }

            public void Check()
            {
                if (Time.time > lastTryTime)
                {
                    TryAddRange(CacheType.Temporary, true);
                    TryAddRange(CacheType.Privilege, true);
                    TryAddRange(CacheType.Seabed, false);
                    lastTryTime = Time.time + 300f;
                }
                
                if (Spawns.Count == 0)
                {
                    TryAddRange();
                }

                if (Seabed.Count == 0)
                {
                    TryAddRange(CacheType.Seabed);
                }
            }

            public void TryAddRange(CacheType cacheType = CacheType.Generic, bool forced = false)
            {
                HashSet<RaidableSpawnLocation> cache = GetCache(cacheType);

                foreach (var rsl in cache)
                {
                    if (Add(rsl, cacheType, cache, forced))
                    {
                        Garbage.Add(rsl);
                    }
                }

                cache.RemoveWhere(Garbage.Contains);

                Garbage.Clear();
            }

            private RaidableSpawnLocation GetSeabed(BuildingWaterOptions options, bool isNearest)
            {
                for (int i = 0; i < Seabed.Count; i++)
                {
                    RaidableSpawnLocation rsl = isNearest ? Seabed.ElementAt(i) : Seabed.GetRandom();

                    if (SpawnsController.InDeepWater(rsl.Location, true, options.MinimumSeabedWaterDepth, options.MaximumSeabedWaterDepth))
                    {
                        return rsl;
                    }
                }
                return null;
            }

            public RaidableSpawnLocation GetRandom(BuildingWaterOptions options, bool buyableEvent)
            {
                RaidableSpawnLocation rsl;

                if (Seabed.Count > 0 && options.Random && (rsl = GetSeabed(options, config.Settings.Buyable.Closest)) != null)
                {
                    options.SpawnOnSeabed = true;
                }
                else
                {
                    rsl = config.Settings.Buyable.Closest && buyableEvent ? Spawns.ElementAt(0) : Spawns.GetRandom();
                    options.SpawnOnSeabed = false;
                }

                Remove(rsl, options.CacheType);

                return rsl;
            }

            public HashSet<RaidableSpawnLocation> GetLocations(CacheType cacheType)
            {
                return cacheType == CacheType.Seabed ? Seabed : Spawns;
            }

            public HashSet<RaidableSpawnLocation> GetCache(CacheType cacheType)
            {
                if (!Cached.TryGetValue(cacheType, out var cache))
                {
                    Cached[cacheType] = cache = new();
                }
                return cache;
            }

            public void AddNear(Vector3 target, float radius, CacheType cacheType, float delayTime)
            {
                if (delayTime > 0)
                {
                    Instance.timer.Once(delayTime, () => AddNear(target, radius, cacheType, 0f));
                    return;
                }

                HashSet<RaidableSpawnLocation> cache = GetCache(cacheType);

                foreach (var rsl in cache)
                {
                    if (InRange2D(target, rsl.Location, radius) && GetLocations(cacheType).Add(rsl))
                    {
                        Garbage.Add(rsl);
                    }
                }

                cache.RemoveWhere(Garbage.Contains);

                Garbage.Clear();
            }

            public void Remove(RaidableSpawnLocation a, CacheType cacheType)
            {
                GetCache(cacheType).Add(a);
                GetLocations(cacheType).Remove(a);
            }

            public float RemoveNear(Vector3 target, float radius, CacheType cacheType, RaidableType type)
            {
                if (cacheType == CacheType.Generic)
                {
                    radius = Mathf.Max(Instance.GetDistance(type), radius);
                }

                HashSet<RaidableSpawnLocation> cache = GetCache(cacheType);
                HashSet<RaidableSpawnLocation> locations = GetLocations(cacheType);

                foreach (var rsl in locations)
                {
                    if (InRange2D(target, rsl.Location, radius) && (cacheType == CacheType.Delete || cache.Add(rsl)))
                    {
                        Garbage.Add(rsl);
                    }
                }

                locations.RemoveWhere(Garbage.Contains);

                Garbage.Clear();

                return radius;
            }
        }

        public class PlayerInfo
        {
            public int Raids, Points, Easy, Medium, Hard, Expert, Nightmare;
            public int EasyPoints, MediumPoints, HardPoints, ExpertPoints, NightmarePoints;
            public int TotalRaids, TotalPoints, TotalEasy, TotalMedium, TotalHard, TotalExpert, TotalNightmare;
            public int TotalEasyPoints, TotalMediumPoints, TotalHardPoints, TotalExpertPoints, TotalNightmarePoints;
            public DateTime ExpiredDate = DateTime.MinValue;
            public bool IsExpired()
            {
                if (ExpiredDate == DateTime.MinValue)
                {
                    ResetExpiredDate();
                }

                return ExpiredDate < DateTime.Now;
            }
            public void ResetExpiredDate() => ExpiredDate = DateTime.Now.AddDays(60);
            public static PlayerInfo Get(StoredData data, string userid)
            {
                if (!data.Players.TryGetValue(userid, out var playerInfo))
                {
                    data.Players[userid] = playerInfo = new();
                }
                return playerInfo;
            }
            public void Reset()
            {
                Raids = Points = Easy = Medium = Hard = Expert = Nightmare = EasyPoints = MediumPoints = HardPoints = ExpertPoints = NightmarePoints = 0;
            }
            [JsonIgnore]
            public bool Any => Easy > 0 || Medium > 0 || Hard > 0 || Expert > 0 || Nightmare > 0;
        }

        public class RotationCycle
        {
            [JsonProperty(PropertyName = "Buildings")]
            public Dictionary<RaidableMode, List<string>> _buildings = new();

            [JsonProperty(PropertyName = "Player Buildings")]
            public Dictionary<ulong, Dictionary<RaidableMode, List<string>>> _playerBuildings = new();

            [JsonIgnore]
            public RaidableBases Instance;

            [JsonIgnore]
            public Configuration config => Instance.config;

            public void Add(RaidableType type, RaidableMode mode, string key, BasePlayer player)
            {
                if (type == RaidableType.Grid || type == RaidableType.Manual)
                {
                    return;
                }

                var buildings = GetBuildingsDictionary(type, player);

                if (buildings == null)
                {
                    return;
                }

                if (!buildings.TryGetValue(mode, out var keyList))
                {
                    buildings[mode] = keyList = new();
                }

                if (!keyList.Contains(key))
                {
                    keyList.Add(key);
                }
            }

            private Dictionary<RaidableMode, List<string>> GetBuildingsDictionary(RaidableType type, BasePlayer player)
            {
                if (config.Settings.Management.RequireAllSpawnedBuyableEvents && type == RaidableType.Purchased && !player.IsNull())
                {
                    if (!_playerBuildings.TryGetValue(player.userID, out var playerBuildings))
                    {
                        _playerBuildings[player.userID] = playerBuildings = new();
                    }

                    return playerBuildings;
                }

                return config.Settings.Management.RequireAllSpawned ? _buildings : null;
            }

            public bool CanSpawn(RaidableType type, RaidableMode mode, string key, BasePlayer player)
            {
                if (mode == RaidableMode.Disabled)
                {
                    return false;
                }

                if (mode == RaidableMode.Random || type == RaidableType.Grid || type == RaidableType.Manual)
                {
                    return true;
                }

                var buildings = GetBuildingsDictionary(type, player);

                if (buildings == null)
                {
                    return !config.Settings.Management.RequireAllSpawned;
                }

                return !buildings.TryGetValue(mode, out var keyList) || !keyList.Contains(key) || TryClear(type, mode, keyList);
            }

            private bool TryClear(RaidableType type, RaidableMode mode, List<string> keyList)
            {
                bool Required(string file) => !keyList.Contains(file) && Instance.FileExists(file);

                foreach (var (key, profile) in Instance.Buildings.Profiles)
                {
                    if (!profile.Options.Enabled || profile.Options.Mode != mode || !Instance.CanSpawnDifficultyToday(mode) || Instance.MustExclude(type, profile.Options.AllowPVP))
                    {
                        continue;
                    }

                    if (Required(key) || profile.Options.AdditionalBases.Keys.Exists(Required))
                    {
                        return false;
                    }
                }

                keyList.Clear();
                return true;
            }
        }

        public class PlayerInputEx : FacepunchBehaviour
        {
            private BasePlayer player;
            private Action queuedAction;
            private RaidableBases Instance;
            private RaidableBase raid;
            private Raider raider;
            private Transform t;
            private Vector3 lastPosition;
            private ulong userid;
            private RaycastHit hit;
            private float fixedTimeTaken;
            public bool isDestroyed;
            public Configuration config => Instance.config;
            public bool IsInvalid => !t || !player || !player.IsConnected || !player.IsAlive();

            private void OnDestroy()
            {
                DestroyMe();
            }

            private void DestroyMe()
            {
                if (!isDestroyed)
                {
                    Instance.ResetPlayer(userid);
                    isDestroyed = true;
                    Destroy(this);
                }
            }

            public void Setup(RaidableBase raid, Raider ri)
            {
                player = GetComponent<BasePlayer>();
                userid = player.userID;
                t = player.transform;
                raider = ri;
                raider.Input = this;
                Instance = raid.Instance;
                this.raid = raid;
            }

            public void Restart()
            {
                fixedTimeTaken = 0f;
            }

            private void FixedUpdate()
            {
                fixedTimeTaken += Time.fixedDeltaTime;

                if (fixedTimeTaken >= 0.1f && !isDestroyed && !IsInvalid)
                {
                    fixedTimeTaken = float.MinValue;

                    if (t.position != lastPosition)
                    {
                        lastPosition = t.position;
                        raider.lastActiveTime = Time.time;
                    }

                    if (config.Settings.Management.AllowLadders)
                    {
                        if (queuedAction != null)
                        {
                            queuedAction();
                            queuedAction = null;
                        }

                        TryPlace(ConstructionType.Any);
                    }

                    fixedTimeTaken = 0f;
                }
            }

            public bool TryPlace(ConstructionType constructionType)
            {
                if (isDestroyed || !player.svActiveItemID.IsValid || !player.serverInput.IsDown(BUTTON.FIRE_PRIMARY) && !player.serverInput.WasDown(BUTTON.FIRE_PRIMARY))
                {
                    return false;
                }

                if (!(player.GetActiveItem() is Item item) || !IsConstructionType(item.info.shortname, ref constructionType))
                {
                    return false;
                }

                int amount = item.amount;

                queuedAction = () =>
                {
                    if (raid == null || item == null || item.amount != amount || IsConstructionNear(constructionType, hit.point) || !Instance.ItemDefinitions.TryGetValue(item.info, out var prefab))
                    {
                        return;
                    }

                    Quaternion rot = Quaternion.LookRotation(constructionType == ConstructionType.Barricade ? (t.position.WithY(0f) - hit.point.WithY(0f)).normalized : hit.normal);

                    if (GameManager.server.CreateEntity(prefab, hit.point, rot, true) is BaseEntity e)
                    {
                        e.gameObject.SendMessage("SetDeployedBy", player, SendMessageOptions.DontRequireReceiver);
                        e.OwnerID = 0;
                        e.Spawn();
                        item.UseItem(1);

                        if (constructionType == ConstructionType.Ladder && hit.GetEntity() is BaseEntity hitEntity)
                        {
                            e.SetParent(hitEntity, true, false);
                        }

                        Instance.RaidEntities[e.net.ID] = new(raid, e);
                        raid.BuiltList[e] = hit.point;
                        raid.AddEntity(e);
                    }
                };

                return true;
            }

            private bool IsConstructionType(string shortname, ref ConstructionType constructionType)
            {
                hit = default;

                if ((constructionType == ConstructionType.Any || constructionType == ConstructionType.Ladder) && shortname == "ladder.wooden.wall")
                {
                    constructionType = ConstructionType.Ladder;

                    if (raid.Options.RequiresCupboardAccessLadders && !raid.CanBuild(player))
                    {
                        raid.Message(player, "Ladders Require Building Privilege!");
                        return false;
                    }

                    return Physics.Raycast(player.eyes.HeadRay(), out hit, 4f, Layers.Mask.Construction, QueryTriggerInteraction.Ignore) && hit.GetEntity() is BaseEntity entity && entity.OwnerID == 0 && Instance.Blocks.Contains(entity.ShortPrefabName);
                }

                if ((constructionType == ConstructionType.Any || constructionType == ConstructionType.Barricade) && shortname.StartsWith("barricade."))
                {
                    constructionType = ConstructionType.Barricade;

                    return Physics.Raycast(player.eyes.HeadRay(), out hit, 5f, targetMask, QueryTriggerInteraction.Ignore) && hit.GetEntity().IsNull();
                }

                return false;
            }

            private bool IsConstructionNear(ConstructionType constructionType, Vector3 target)
            {
                float radius = constructionType == ConstructionType.Barricade ? 1f : 0.3f;
                int layerMask = constructionType == ConstructionType.Barricade ? -1 : Layers.Mask.Deployed;
                var entities = FindEntitiesOfType<BaseEntity>(target, radius, layerMask);
                if (constructionType == ConstructionType.Barricade)
                {
                    return entities.Count > 0;
                }
                return entities.Exists(e => e is BaseLadder);
            }
        }

        public class HumanoidNPC : ScientistNPC
        {
            public new HumanoidBrain Brain;

            public new Translate.Phrase LootPanelTitle => displayName;

            public override string Categorize() => "Humanoid";

            public override bool ShouldDropActiveItem() => false;

            protected override string OverrideCorpseName() => displayName;

            public override void AttackerInfo(ProtoBuf.PlayerLifeStory.DeathInfo info)
            {
                info.attackerName = displayName;
                info.attackerSteamID = userID;
                info.inflictorName = inventory.containerBelt.GetSlot(0).info.shortname;
                info.attackerDistance = Vector3.Distance(Brain.ServerPosition, Brain.AttackPosition);
            }

            public override void OnKilled(HitInfo info)
            {
                Brain.DisableShouldThink();
                base.OnKilled(info);
            }

            private void Respawn()
            {
                if (Brain == null || Brain.raid == null || Brain.raid.npcs == null || Brain.raid.IsDespawning)
                {
                    return;
                }
                Brain.raid.npcs.RemoveAll(npc => npc.IsKilled() || npc.userID == userID);
                Brain.Instance.HumanoidBrains.Remove(userID);
                if (Brain.raid.Options.RespawnRateMax > 0f)
                {
                    Brain.raid.TryRespawnNpc(Brain.isMurderer);
                }
            }

            public override BaseCorpse CreateCorpse(PlayerFlags flagsOnDeath, Vector3 posOnDeath, Quaternion rotOnDeath, List<TriggerBase> triggersOnDeath)
            {
                if (inventory == null || Brain == null || !Brain.HasCorpseLoot())
                {
                    inventory.SafelyStrip();
                    Respawn();
                    return null;
                }
                PlayerCorpse corpse = DropCorpse("assets/prefabs/player/player_corpse.prefab") as PlayerCorpse;
                if (!corpse)
                {
                    Respawn();
                    return null;
                }
                if (Brain.keepInventory)
                {
                    inventory.containerWear.SafelyRemove("gloweyes");
                }
                else
                {
                    inventory.SafelyStrip();
                }
                corpse.transform.position = corpse.transform.position + Vector3.down * NavAgent.baseOffset;
                corpse.TakeFrom(this, inventory.containerMain, inventory.containerWear, inventory.containerBelt);
                corpse.playerName = OverrideCorpseName();
                corpse.playerSteamID = userID;
                corpse.Spawn();
                corpse.TakeChildren(this);
                if (LootSpawnSlots.Length != 0)
                {
                    foreach (var lootSpawnSlot in LootSpawnSlots)
                    {
                        for (int k = 0; k < lootSpawnSlot.numberToSpawn; k++)
                        {
                            if (UnityEngine.Random.Range(0f, 1f) <= lootSpawnSlot.probability)
                            {
                                lootSpawnSlot.definition.SpawnIntoContainer(corpse.containers[0]);
                            }
                        }
                    }
                }
                return corpse;
            }
        }

        public class HumanoidBrain : ScientistBrain
        {
            public void DisableShouldThink()
            {
                if (isKilled)
                {
                    return;
                }
                if (!Rust.Application.isQuitting)
                {
                    BaseEntity.Query.Server.RemoveBrain(GetBaseEntity());
                    LeaveGroup();
                }
                if (thinker != null)
                {
                    AIThinkManager._processQueue.Remove(thinker);
                }
                lastWarpTime = float.MaxValue;
                sleeping = true;
                isKilled = true;
                SetEnabled(false);
            }

            internal enum AttackType { BaseProjectile, Explosive, FlameThrower, Melee, Water, None }
            internal string displayName;
            internal Transform NpcTransform;
            internal IThinker thinker;
            internal HumanoidNPC npc;
            internal AttackEntity _attackEntity;
            internal FlameThrower flameThrower;
            internal BaseProjectile launcher;
            internal LiquidWeapon liquidWeapon;
            internal BaseMelee baseMelee;
            internal BaseProjectile baseProjectile;
            internal BasePlayer AttackTarget;
            internal Transform AttackTransform;
            internal RaidableBases Instance;
            internal RaidableBase raid;
            internal NpcSettings Settings;
            internal List<Vector3> positions;
            internal Vector3 DestinationOverride;
            internal bool keepInventory;
            internal bool isKilled;
            internal bool isMurderer;
            internal bool isStationary;
            internal bool isSleeper;
            internal bool isAsleep;
            internal bool unwakeable;
            internal float lastWarpTime;
            internal float softLimitSenseRange;
            internal float nextAttackTime;
            internal float attackRange;
            internal float attackCooldown;
            internal AttackType attackType = AttackType.None;
            internal BaseNavigator.NavigationSpeed CurrentSpeed = BaseNavigator.NavigationSpeed.Normal;
            internal Configuration config => Instance.config;
            internal Vector3 AttackPosition => AttackTransform == null ? default : AttackTransform.position;
            internal Vector3 ServerPosition => NpcTransform == null ? default : NpcTransform.position;

            internal List<AttackEntity> AttackWeapons = new();
            internal List<Item> MedicalTools = new();
            internal float equipWeaponTime;
            internal float equipToolTime;
            internal float updateDeltaTime;

            internal AttackEntity AttackEntity
            {
                get
                {
                    if (_attackEntity.IsNull())
                    {
                        IdentifyWeapon();
                    }

                    return _attackEntity;
                }
            }

            private void Update()
            {
                if (isKilled)
                {
                    return;
                }
                updateDeltaTime = Time.deltaTime;
                equipToolTime += updateDeltaTime;
                if (equipToolTime >= 5f)
                {
                    equipToolTime = float.MinValue;
                    EquipMedicalTool();
                    equipToolTime = 0f;
                }
                equipWeaponTime += updateDeltaTime;
                if (equipWeaponTime >= 5f)
                {
                    equipWeaponTime = float.MinValue;
                    EquipWeapon();
                    equipWeaponTime = 0f;
                }
            }

            private void EquipWeapon()
            {
                AttackWeapons.RemoveAll(IsKilled);
                if (AttackWeapons.Count <= 1)
                {
                    return;
                }
                Shuffle(AttackWeapons);
                foreach (var weapon in AttackWeapons)
                {
                    if (AttackTransform != null)
                    {
                        if (weapon is BaseMelee && !IsInAttackRange(5f))
                        {
                            continue;
                        }
                        if (weapon is ThrownWeapon && (!AttackTarget.IsOnGround() || !IsInAttackRange(15f)))
                        {
                            continue;
                        }
                    }
                    UpdateWeapon(weapon, weapon.ownerItemUID);
                    _attackEntity = null;
                    IdentifyWeapon();
                    break;
                }
            }

            public bool HasCorpseLoot()
            {
                if (isMurderer ? Settings.MurdererDrops.Count > 0 : Settings.ScientistDrops.Count > 0)
                {
                    return true;
                }
                return keepInventory || npc != null && npc.LootSpawnSlots.Length != 0;
            }

            public void EnableMedicalTools()
            {
                equipToolTime = MedicalTools.Count == 0 ? float.MinValue : 4f;
            }

            private void EquipMedicalTool()
            {
                if (npc.health > npc.startHealth * HealBelowHealthFraction)
                {
                    return;
                }
                if (AttackTransform != null)
                {
                    if (!isMurderer && Senses.Memory.IsLOS(AttackTarget)) return;
                    if (isMurderer && IsInReachableRange()) return;
                }
                MedicalTools.RemoveAll(IsKilled);
                if (MedicalTools.Count == 0)
                {
                    return;
                }
                Item tool = MedicalTools[0];
                if (tool == null)
                {
                    return;
                }
                equipWeaponTime = 0f;
                npc.UseHealingItem(tool);
            }

            public void UpdateWeapon(AttackEntity attackEntity, ItemId uid)
            {
                npc.UpdateActiveItem(uid);

                if (attackEntity is Chainsaw cs)
                {
                    cs.ServerNPCStart();
                }

                npc.damageScale = 1f;

                attackEntity.TopUpAmmo();
                attackEntity.SetHeld(true);
            }

            internal void IdentifyWeapon()
            {
                _attackEntity = GetEntity().GetAttackEntity();

                attackRange = 0f;
                attackCooldown = 99999f;
                attackType = AttackType.None;
                baseMelee = null;
                flameThrower = null;
                launcher = null;
                liquidWeapon = null;

                if (_attackEntity.IsNull())
                {
                    return;
                }

                (_attackEntity.ShortPrefabName switch
                {
                    "double_shotgun.entity" or "shotgun_pump.entity" or "shotgun_waterpipe.entity" or "spas12.entity" => (Action)(() =>
                    {
                        SetAttackRestrictions(AttackType.BaseProjectile, 30f, 0f, 30f);
                    }),
                    "ak47u.entity" or "ak47u_ice.entity" or "bolt_rifle.entity" or "glock.entity" or "hmlmg.entity" or "l96.entity" or "lr300.entity" or "m249.entity" or "m39.entity" or "m92.entity" or "mp5.entity" or "nailgun.entity" or "pistol_eoka.entity" or "pistol_revolver.entity" or "pistol_semiauto.entity" or "python.entity" or "semi_auto_rifle.entity" or "thompson.entity" or "smg.entity" => (Action)(() =>
                    {
                        SetAttackRestrictions(AttackType.BaseProjectile, 300f, 0f, 150f);
                    }),
                    "snowballgun.entity" => (Action)(() =>
                    {
                        SetAttackRestrictions(AttackType.BaseProjectile, 15f, 0.1f, 15f);
                    }),
                    "chainsaw.entity" or "jackhammer.entity" => (Action)(() =>
                    {
                        baseMelee = _attackEntity as BaseMelee;
                        SetAttackRestrictions(AttackType.Melee, 2.5f, (_attackEntity.animationDelay + _attackEntity.deployDelay) * 2f);
                    }),
                    "axe_salvaged.entity" or "bone_club.entity" or "butcherknife.entity" or "candy_cane.entity" or "hammer_salvaged.entity" or "hatchet.entity" or "icepick_salvaged.entity" or "knife.combat.entity" or "knife_bone.entity" or "longsword.entity" or "mace.baseballbat" or "mace.entity" or "machete.weapon" or "pickaxe.entity" or "pitchfork.entity" or "salvaged_cleaver.entity" or "salvaged_sword.entity" or "sickle.entity" or "spear_stone.entity" or "spear_wooden.entity" or "stone_pickaxe.entity" or "stonehatchet.entity" => (Action)(() =>
                    {
                        baseMelee = _attackEntity as BaseMelee;
                        SetAttackRestrictions(AttackType.Melee, 2.5f, _attackEntity.animationDelay + _attackEntity.deployDelay);
                    }),
                    "explosive.satchel.entity" or "explosive.timed.entity" => (Action)(() =>
                    {
                        SetAttackRestrictions(AttackType.Explosive, 17.5f, 10f);
                    }),
                    "grenade.beancan.entity" or "grenade.f1.entity" or "grenade.molotov.entity" or "grenade.flashbang.entity" => (Action)(() =>
                    {
                        SetAttackRestrictions(AttackType.Explosive, 17.5f, 5f);
                    }),
                    "mgl.entity" => (Action)(() =>
                    {
                        launcher = _attackEntity as BaseProjectile;
                        SetAttackRestrictions(AttackType.Explosive, 150f, 4f, 150f);
                    }),
                    "rocket_launcher.entity" => (Action)(() =>
                    {
                        launcher = _attackEntity as BaseProjectile;
                        SetAttackRestrictions(AttackType.Explosive, 300f, 6f, 150f);
                    }),
                    "flamethrower.entity" => (Action)(() =>
                    {
                        flameThrower = _attackEntity as FlameThrower;
                        SetAttackRestrictions(AttackType.FlameThrower, 10f, (_attackEntity.animationDelay + _attackEntity.deployDelay) * 2f);
                    }),
                    "compound_bow.entity" or "crossbow.entity" or "speargun.entity" or "bow_hunting.entity" => (Action)(() =>
                    {
                        SetAttackRestrictions(AttackType.BaseProjectile, 200f, (_attackEntity.animationDelay + _attackEntity.deployDelay) * 1.25f, 150f);
                    }),
                    "watergun.entity" or "waterpistol.entity" => (Action)(() =>
                    {
                        if ((liquidWeapon = _attackEntity as LiquidWeapon) != null)
                        {
                            liquidWeapon.AutoPump = true;
                            SetAttackRestrictions(AttackType.Water, 10f, 2f);
                        }
                    }),
                    _ => (Action)(() => _attackEntity = null)
                })();
            }

            private void SetAttackRestrictions(AttackType attackType, float attackRange, float attackCooldown, float effectiveRange = 0f)
            {
                if (attackType == AttackType.BaseProjectile && _attackEntity is BaseProjectile projectile)
                {
                    baseProjectile = projectile;

                    if (!baseProjectile.MuzzlePoint)
                    {
                        baseProjectile.MuzzlePoint = baseProjectile.transform;
                    }
                }

                if (effectiveRange != 0f)
                {
                    _attackEntity.effectiveRange = effectiveRange;
                }

                (this.attackType, this.attackRange, this.attackCooldown) = (attackType, attackRange, attackCooldown);
            }

            public bool ValidTarget => AttackTransform && !AttackTarget.IsKilled() && !ShouldForgetTarget(AttackTarget);

            public override void OnDestroy()
            {
                if (!Rust.Application.isQuitting && !isKilled)
                {
                    BaseEntity.Query.Server.RemoveBrain(GetEntity());
                    LeaveGroup();
                }
            }

            public override void InitializeAI()
            {
                base.InitializeAI();
                base.ForceSetAge(0f);

                Pet = false;
                sleeping = false;
                UseAIDesign = true;
                AllowedToSleep = false;
                HostileTargetsOnly = false;
                AttackRangeMultiplier = 2f;
                MaxGroupSize = 0;

                Senses.Init(
                    owner: GetEntity(),
                    brain: this,
                    memoryDuration: 5f,
                    range: 50f,
                    targetLostRange: 75f,
                    visionCone: -1f,
                    checkVision: false,
                    checkLOS: true,
                    ignoreNonVisionSneakers: true,
                    listenRange: 15f,
                    hostileTargetsOnly: false,
                    senseFriendlies: false,
                    ignoreSafeZonePlayers: false,
                    senseTypes: config.Settings.Management.TargetNpcs ? EntityType.Player | EntityType.BasePlayerNPC : EntityType.Player,
                    refreshKnownLOS: true
                );

                CanUseHealingItems = true;
            }

            public void TryStartSleeping()
            {
                if (Settings.Inside.Sleepers.Enabled && isStationary)
                {
                    SetSleeping(true);
                    isSleeper = true;

                    if (Settings.Inside.Sleepers.Unwakeable)
                    {
                        DisableShouldThink();
                        unwakeable = true;
                    }
                }
            }

            public void SetSleeping(bool state)
            {
                SetEnabled(!state);
                sleeping = state;
                isAsleep = state;
                AllowedToSleep = state;
                npc.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, state);
            }

            public override void AddStates()
            {
                base.AddStates();

                states[AIState.Attack] = new AttackState(this);
            }

            public class AttackState : BaseAttackState
            {
                private new HumanoidBrain brain;
                private global::HumanNPC npc;
                private Transform NpcTransform;

                private IAIAttack attack => brain.Senses.ownerAttack;

                public AttackState(HumanoidBrain humanoidBrain)
                {
                    base.brain = brain = humanoidBrain;
                    base.AgrresiveState = true;
                    npc = brain.GetBrainBaseEntity() as global::HumanNPC;
                    NpcTransform = npc.transform;
                }

                public override void StateEnter(BaseAIBrain _brain, BaseEntity _entity)
                {
                    if (_brain != null && NpcTransform != null && brain.ValidTarget)
                    {
                        if (InAttackRange())
                        {
                            StartAttacking();
                        }
                        if (brain.CanUseNavMesh())
                        {
                            brain.Navigator.SetDestination(brain.DestinationOverride, BaseNavigator.NavigationSpeed.Fast, 0f, 0f);
                        }
                    }
                }

                public override void StateLeave(BaseAIBrain _brain, BaseEntity _entity)
                {

                }

                private void StopAttacking()
                {
                    if (attack != null)
                    {
                        attack.StopAttacking();
                        brain.AttackTarget = null;
                        brain.AttackTransform = null;
                        brain.Navigator.ClearFacingDirectionOverride();
                    }
                }

                public override StateStatus StateThink(float delta, BaseAIBrain _brain, BaseEntity _entity)
                {
                    if (_brain == null || NpcTransform == null || attack == null)
                    {
                        return StateStatus.Error;
                    }
                    if (!brain.ValidTarget || brain.isKilled)
                    {
                        StopAttacking();

                        return StateStatus.Finished;
                    }
                    if (brain.Senses.ignoreSafeZonePlayers && brain.AttackTarget.InSafeZone())
                    {
                        return StateStatus.Error;
                    }
                    if (brain.CanUseNavMesh() && !brain.Navigator.SetDestination(brain.DestinationOverride, BaseNavigator.NavigationSpeed.Fast, 0f, 0f))
                    {
                        return StateStatus.Error;
                    }
                    if (!brain.CanShoot() || !InAttackRange())
                    {
                        brain.Forget();

                        StopAttacking();

                        return StateStatus.Finished;
                    }
                    else
                    {
                        StartAttacking();

                        return StateStatus.Running;
                    }
                }

                private bool InAttackRange()
                {
                    return !npc.IsWounded() && brain.AttackTransform != null && attack.CanAttack(brain.AttackTarget) && brain.IsInAttackRange() && brain.CanSeeTarget(brain.AttackTarget);
                }

                private void StartAttacking()
                {
                    if (brain.AttackTransform == null)
                    {
                        return;
                    }

                    brain.SetAimDirection();

                    if (!brain.CanShoot() || brain.IsAttackOnCooldown() || brain.TryThrowWeapon())
                    {
                        return;
                    }
                    if (brain.attackType == AttackType.Explosive && !brain.launcher.IsNull())
                    {
                        brain.EmulatedFire();
                    }
                    else if (brain.attackType == AttackType.BaseProjectile)
                    {
                        RealisticShotTest();
                    }
                    else if (brain.attackType == AttackType.FlameThrower)
                    {
                        brain.UseFlameThrower();
                    }
                    else if (brain.attackType == AttackType.Water)
                    {
                        brain.UseWaterGun();
                    }
                    else brain.MeleeAttack();
                }

                private void PseudoShotTest()
                {
                    if (brain.AttackTarget.IsNpc)
                    {
                        float damage = brain.AttackTarget._maxHealth * UnityEngine.Random.Range(0.1f, 0.25f);
                        brain.AttackTarget.Hurt(new HitInfo(npc, brain.AttackTarget, DamageType.Bullet, damage));
                    }
                    npc.ShotTest(brain.AttackPosition.Distance(brain.ServerPosition));
                }

                private void RealisticShotTest()
                {
                    if (brain.AttackTarget.IsNpc)
                    {
                        var faction = brain.AttackTarget.faction;
                        brain.AttackTarget.faction = BaseCombatEntity.Faction.Horror;
                        npc.ShotTest(brain.AttackPosition.Distance(brain.ServerPosition));
                        if (brain.AttackTarget != null) brain.AttackTarget.faction = faction;
                    }
                    else npc.ShotTest(brain.AttackPosition.Distance(brain.ServerPosition));
                }
            }

            private bool init;

            public void Init()
            {
                if (init) return;
                init = true;
                lastWarpTime = Time.time;
                npc.spawnPos = raid.Location;
                npc.AdditionalLosBlockingLayer = visibleMask;
                SetupNavigator(GetEntity(), GetComponent<BaseNavigator>(), raid.ProtectionRadius, isStationary);
            }

            private void Converge()
            {
                foreach (var brain in Instance.HumanoidBrains.Values)
                {
                    if (brain != null && brain.NpcTransform != null && brain != this && brain.attackType == attackType && brain.CanConverge(npc))
                    {
                        brain.SetTarget(AttackTarget, false);
                    }
                }
            }

            public void Forget()
            {
                Senses.Players.Clear();
                Senses.Memory.All.Clear();
                Senses.Memory.Threats.Clear();
                Senses.Memory.Targets.Clear();
                Senses.Memory.Players.Clear();
                Navigator.ClearFacingDirectionOverride();

                if (!isStationary)
                    DestinationOverride = GetRandomRoamPosition();

                SenseRange = ListenRange = Settings.AggressionRange;
                Senses.targetLostRange = TargetLostRange = SenseRange * 1.25f;
                AttackTarget = null;
                AttackTransform = null;

                TryReturnHome();
            }

            private void RandomMove(float radius)
            {
                var to = AttackPosition + UnityEngine.Random.onUnitSphere * radius;

                to.y = TerrainMeta.HeightMap.GetHeight(to);

                SetDestination(to);
            }

            public void SetupNavigator(BaseCombatEntity owner, BaseNavigator navigator, float distance, bool isStationary)
            {
                navigator.CanUseNavMesh = !isStationary && !Rust.Ai.AiManager.nav_disable;

                if (isStationary)
                {
                    navigator.MaxRoamDistanceFromHome = navigator.BestMovementPointMaxDistance = navigator.BestRoamPointMaxDistance = 0f;
                    navigator.DefaultArea = "Not Walkable";
                }
                else
                {
                    navigator.MaxRoamDistanceFromHome = navigator.BestMovementPointMaxDistance = navigator.BestRoamPointMaxDistance = distance * 0.85f;
                    navigator.DefaultArea = "Walkable";
                    navigator.topologyPreference = ((TerrainTopology.Enum)TerrainTopology.EVERYTHING);
                }

                navigator.Agent.agentTypeID = NavMesh.GetSettingsByIndex(1).agentTypeID; // 0:0, 1: -1372625422, 2: 1479372276, 3: -1923039037
                navigator.MaxWaterDepth = config.Settings.Management.WaterDepth;

                if (navigator.CanUseNavMesh)
                {
                    navigator.Init(owner, navigator.Agent);
                }
            }

            public Vector3 GetAimDirection()
            {
                if (Navigator.IsOverridingFacingDirection)
                {
                    return Navigator.FacingDirectionOverride;
                }
                if (InRange2D(AttackPosition, ServerPosition, 1f))
                {
                    return npc.eyes.BodyForward();
                }
                return (AttackPosition - ServerPosition).normalized;
            }

            private void SetAimDirection()
            {
                Navigator.SetFacingDirectionEntity(AttackTarget);
                npc.SetAimDirection(GetAimDirection());
            }

            private void SetDestination()
            {
                SetDestination(GetRandomRoamPosition());
            }

            private void SetDestination(Vector3 destination)
            {
                if (!CanLeave(destination))
                {
                    destination = GetRandomRoamPosition();

                    CurrentSpeed = BaseNavigator.NavigationSpeed.Normal;
                }

                if (destination != DestinationOverride)
                {
                    destination.y = TerrainMeta.HeightMap.GetHeight(destination);

                    if (destination.y < WaterSystem.OceanLevel - 1f)
                    {
                        destination = GetRandomRoamPosition();
                    }

                    DestinationOverride = destination;
                }

                Navigator.SetCurrentSpeed(CurrentSpeed);

                if (Navigator.CurrentNavigationType == BaseNavigator.NavigationType.None && !Rust.Ai.AiManager.ai_dormant && !Rust.Ai.AiManager.nav_disable)
                {
                    Navigator.SetCurrentNavigationType(BaseNavigator.NavigationType.NavMesh);
                }

                if (CanUseNavMesh() && !Navigator.SetDestination(destination, CurrentSpeed))
                {
                    Navigator.Destination = destination;
                    npc.finalDestination = destination;
                }
            }

            public bool CanUseNavMesh() => !isStationary && Navigator.CanUseNavMesh && !Navigator.StuckOffNavmesh;

            public bool SetTarget(BasePlayer player, bool converge = true)
            {
                if (unwakeable)
                {
                    return false;
                }

                if (NpcTransform == null)
                {
                    DisableShouldThink();
                    Destroy(this);
                    return false;
                }

                if (player.IsKilled() || player.limitNetworking)
                {
                    return false;
                }

                if (AttackTarget == player)
                {
                    return true;
                }

                if (npc.lastGunShotTime < Time.time + 2f)
                {
                    npc.nextTriggerTime = Time.time + 0.2f;
                    nextAttackTime = Time.realtimeSinceStartup + 0.2f;
                }

                TrySetKnown(player);
                npc.lastAttacker = player;
                AttackTarget = player;
                AttackTransform = player.transform;

                if (!IsInSenseRange(AttackPosition))
                {
                    SenseRange = ListenRange = Settings.AggressionRange + AttackPosition.Distance(ServerPosition);
                    TargetLostRange = SenseRange + (SenseRange * 0.25f);
                }
                else
                {
                    SenseRange = ListenRange = softLimitSenseRange;
                    TargetLostRange = softLimitSenseRange * 1.25f;
                }

                if (converge)
                {
                    Converge();
                }

                return true;
            }

            private void TryReturnHome()
            {
                if (!isStationary && Settings.CanLeave && !IsInHomeRange())
                {
                    CurrentSpeed = BaseNavigator.NavigationSpeed.Normal;

                    Warp();
                }
            }

            private void TryToAttack() => TryToAttack(null);

            private void TryToAttack(BasePlayer attacker)
            {
                if (unwakeable)
                {
                    return;
                }

                if ((attacker ??= GetBestTarget()).IsNull())
                {
                    return;
                }

                if (ShouldForgetTarget(attacker))
                {
                    Forget();

                    return;
                }

                if (!SetTarget(attacker) || AttackTransform == null || !CanSeeTarget(attacker))
                {
                    return;
                }

                if (isStationary)
                {
                    SetAimDirection();
                }
                else if (attackType == AttackType.BaseProjectile)
                {
                    TryScientistActions();
                }
                else
                {
                    TryMurdererActions();
                }

                SwitchToState(AIState.Attack, -1);
            }

            private void TryMurdererActions()
            {
                CurrentSpeed = BaseNavigator.NavigationSpeed.Fast;

                if (attackType == AttackType.Explosive)
                {
                    if (IsInAttackRange(20f))
                    {
                        RandomMove(15f);
                    }
                    else
                    {
                        SetDestination(AttackPosition);
                    }
                }
                else if (!IsInReachableRange())
                {
                    RandomMove(15f);
                }
                else if (!IsInAttackRange())
                {
                    if (attackType == AttackType.FlameThrower)
                    {
                        RandomMove(attackRange);
                    }
                    else
                    {
                        SetDestination(AttackPosition);
                    }
                }
            }

            private void TryScientistActions()
            {
                CurrentSpeed = BaseNavigator.NavigationSpeed.Fast;

                SetDestination();
            }

            public void SetupMovement(List<Vector3> positions)
            {
                if (positions.IsNullOrEmpty())
                {
                    isStationary = true;
                }

                if (!isStationary)
                {
                    InvokeRepeating(TryToRoam, 0f, 7.5f);
                }

                InvokeRepeating(TryToAttack, 1f, 1f);
            }

            private void TryToRoam()
            {
                if (Settings.KillUnderwater && npc.IsSwimming())
                {
                    DisableShouldThink();
                    npc.Kill();
                    Destroy(this);
                    return;
                }

                if (ValidTarget)
                {
                    return;
                }

                if (IsStuck())
                {
                    Warp();

                    Navigator.stuckTimer = 0f;
                }

                CurrentSpeed = BaseNavigator.NavigationSpeed.Normal;

                SetDestination();
            }

            private bool IsStuck() => false; //InRange(npc.transform.position, Navigator.stuckCheckPosition, Navigator.StuckDistance);

            public void Warp()
            {
                if (Time.time < lastWarpTime)
                {
                    return;
                }

                lastWarpTime = Time.time + 1f;

                DestinationOverride = GetRandomRoamPosition();

                Navigator.Warp(DestinationOverride);
            }

            private void UseFlameThrower()
            {
                if (flameThrower.ammo < flameThrower.maxAmmo * 0.25)
                {
                    flameThrower.SetFlameState(false);
                    flameThrower.ServerReload();
                    return;
                }
                npc.triggerEndTime = Time.time + attackCooldown;
                flameThrower.SetFlameState(true);
                flameThrower.Invoke(() => flameThrower.SetFlameState(false), 2f);
            }

            private void UseWaterGun()
            {
                if (Physics.Raycast(npc.eyes.BodyRay(), out var hit, 10f, 1218652417))
                {
                    WaterBall.DoSplash(hit.point, 2f, ItemManager.FindItemDefinition("water"), 10);
                    DamageUtil.RadiusDamage(npc, liquidWeapon.LookupPrefab(), hit.point, 0.15f, 0.15f, new(), 131072, true);
                }
            }

            private void UseChainsaw()
            {
                AttackEntity.TopUpAmmo();
                AttackEntity.ServerUse();
                AttackTarget.Hurt(10f * AttackEntity.npcDamageScale, DamageType.Bleeding, npc);
            }

            private void EmulatedFire()
            {
                if (launcher.HasAttackCooldown()) return;
                float dist;
                string prefab;
                switch (launcher.ShortPrefabName)
                {
                    case "rocket_launcher.entity":
                        prefab = "assets/prefabs/ammo/rocket/rocket_basic.prefab";
                        dist = ServerPosition.Distance(AttackPosition);
                        launcher.repeatDelay = 6f;
                        break;
                    case "mgl.entity":
                        prefab = "assets/prefabs/ammo/40mmgrenade/40mm_grenade_he.prefab";
                        launcher.repeatDelay = 4f;
                        dist = ServerPosition.Distance(AttackPosition) + 5f;
                        break;
                    default: return;
                }
                Vector3 euler = launcher.MuzzlePoint.transform.forward + Vector3.up;
                Vector3 position = launcher.MuzzlePoint.transform.position + (Vector3.up * 1.6f);
                if (!(GameManager.server.CreateEntity(prefab, position, GetEntity().eyes.GetLookRotation()) is BaseEntity entity)) return;
                entity.creatorEntity = GetEntity();
                if (entity.GetComponent<ServerProjectile>() is ServerProjectile serverProjectile)
                {
                    serverProjectile.InitializeVelocity(Quaternion.Euler(euler) * entity.transform.forward * dist);
                }
                if (entity is TimedExplosive explosive)
                {
                    explosive.timerAmountMin = 1;
                    explosive.timerAmountMax = 15;
                }
                entity.Spawn();
                launcher.StartAttackCooldown(launcher.repeatDelay);
            }

            private void MeleeAttack()
            {
                if (baseMelee.IsNull())
                {
                    return;
                }

                if (AttackEntity is Chainsaw)
                {
                    UseChainsaw();
                    return;
                }

                Vector3 position = AttackPosition;
                AttackEntity.StartAttackCooldown(AttackEntity.repeatDelay * 2f);
                npc.SignalBroadcast(BaseEntity.Signal.Attack, string.Empty, null);
                if (baseMelee.swingEffect.isValid)
                {
                    Effect.server.Run(baseMelee.swingEffect.resourcePath, position, Vector3.forward, npc.Connection, false);
                }
                HitInfo hitInfo = new()
                {
                    damageTypes = new(),
                    DidHit = true,
                    Initiator = npc,
                    HitEntity = AttackTarget,
                    HitPositionWorld = position,
                    HitPositionLocal = AttackTransform.InverseTransformPoint(position),
                    HitNormalWorld = npc.eyes.BodyForward(),
                    HitMaterial = StringPool.Get("Flesh"),
                    PointStart = ServerPosition,
                    PointEnd = position,
                    Weapon = AttackEntity,
                    WeaponPrefab = AttackEntity
                };

                hitInfo.damageTypes.Set(DamageType.Slash, baseMelee.TotalDamage() * AttackEntity.npcDamageScale);
                Effect.server.ImpactEffect(hitInfo);
                AttackTarget.OnAttacked(hitInfo);
            }

            public bool TryThrowWeapon()
            {
                if (!IsInThrowRange() || !(AttackEntity is ThrownWeapon thrownWeapon))
                {
                    return false;
                }

                npc.SetAiming(true);
                SetAimDirection();

                npc.Invoke(() =>
                {
                    if (!ValidTarget)
                    {
                        CurrentSpeed = BaseNavigator.NavigationSpeed.Normal;

                        Forget();
                        SetDestination();
                        npc.SetAiming(false);

                        return;
                    }

                    if (IsInThrowRange())
                    {
                        Item item = thrownWeapon.GetItem();
                        if (item != null) item.amount++;
                        thrownWeapon.ServerThrow(AttackPosition);
                    }
                    else nextAttackTime = Time.realtimeSinceStartup + 1f;

                    npc.SetAiming(false);
                    RandomMove(15f);
                }, 1f);

                return true;
            }

            private bool CanConverge(global::HumanNPC other)
            {
                if (ValidTarget || other.IsKilled() || other.IsDead()) return false;
                return IsInTargetRange(other.transform.position);
            }

            private bool CanLeave(Vector3 destination)
            {
                return Settings.CanLeave || isStationary || IsInLeaveRange(destination);
            }

            private bool CanSeeTarget(BasePlayer target)
            {
                if (isStationary)
                {
                    return Senses.Memory.IsLOS(target);
                }

                if (attackType == AttackType.Explosive && raid.Options.NPC.CounterRaid && raid.IsInForwardOperatingBase(target.transform.position))
                {
                    return true;
                }

                if (Navigator.CurrentNavigationType == BaseNavigator.NavigationType.None && (attackType == AttackType.FlameThrower || attackType == AttackType.Melee))
                {
                    return true;
                }

                if (Senses.Memory.IsLOS(target))
                {
                    return true;
                }

                nextAttackTime = Time.realtimeSinceStartup + 1f;

                return false;
            }

            public bool CanRoam(Vector3 destination) => destination == DestinationOverride && IsInSenseRange(destination);

            private bool CanShoot()
            {
                if (attackType == AttackType.None)
                {
                    return false;
                }

                return Settings.CanShoot || attackType != AttackType.BaseProjectile && attackType != AttackType.Explosive || IsInLeaveRange(AttackPosition);
            }

            private void TrySetKnown(BasePlayer player)
            {
                if (Senses.ownerAttack != null && !Senses.Memory.IsPlayerKnown(player) && !Senses.Memory.Targets.Contains(player))
                {
                    Senses.Memory.SetKnown(player, npc, Senses);
                }
            }

            public BasePlayer GetBestTarget()
            {
                if (npc.IsWounded())
                {
                    return null;
                }
                float sqrSenseRange = SenseRange * SenseRange;
                float delta = -1f;
                BasePlayer target = null;
                foreach (var entity in Senses.Memory.Players)
                {
                    if (!(entity is BasePlayer player) || ShouldForgetTarget(player)) continue;
                    if (!config.Settings.Management.TargetNpcs && !player.IsHuman()) continue;
                    float sqrDist = (player.transform.position - npc.transform.position).sqrMagnitude;
                    float rangeDelta = 1f - Mathf.InverseLerp(1f, sqrSenseRange, sqrDist);
                    rangeDelta += (CanSeeTarget(player) ? 2f : 0f);
                    if (rangeDelta <= delta) continue;
                    target = player;
                    delta = rangeDelta;
                }
                return target;
            }

            private Vector3 GetRandomRoamPosition() => positions.GetRandom();

            private bool IsAttackOnCooldown()
            {
                if (isStationary && attackType == AttackType.BaseProjectile)
                {
                    return false;
                }

                if (attackType == AttackType.None || Time.realtimeSinceStartup < nextAttackTime)
                {
                    return true;
                }

                if (attackCooldown > 0f)
                {
                    nextAttackTime = Time.realtimeSinceStartup + attackCooldown;
                }

                return false;
            }

            private bool IsInAttackRange(float range = 0f) => InRange(ServerPosition, AttackPosition, range == 0f ? attackRange : range);

            private bool IsInHomeRange() => InRange(ServerPosition, raid.Location, Mathf.Max(raid.ProtectionRadius, TargetLostRange));

            private bool IsInLeaveRange(Vector3 destination) => InRange(raid.Location, destination, raid.ProtectionRadius);

            private bool IsInReachableRange() => AttackPosition.y - ServerPosition.y <= attackRange && (attackType != AttackType.Melee || InRange(AttackPosition, ServerPosition, 15f));

            private bool IsInSenseRange(Vector3 destination) => InRange2D(raid.Location, destination, SenseRange);

            private bool IsInTargetRange(Vector3 destination) => InRange2D(raid.Location, destination, TargetLostRange);

            private bool IsInThrowRange() => InRange(ServerPosition, AttackPosition, attackRange);

            private bool ShouldForgetTarget(BasePlayer target) => target.IsKilled() || target.health <= 0f || target.limitNetworking || target.IsDead() || target.skinID == 14922524 || !IsInTargetRange(target.transform.position);
        }

        public class Raider
        {
            public ulong userid;
            public string id;
            public string displayName;
            public bool reward = true;
            public bool IsRaider;
            public bool IsAlly;
            public bool IsAllowed;
            public bool PreEnter = true;
            public bool LockedOnEnter;
            public PlayerInputEx Input;
            public float lastActiveTime;
            private BasePlayer _player;
            public BasePlayer player => _player = ((bool)_player ? _player : RustCore.FindPlayerById(userid));

            public bool IsParticipant
            {
                get
                {
                    return IsRaider || LockedOnEnter;
                }
                set
                {
                    IsRaider = value;
                    LockedOnEnter = value;
                }
            }
            public Raider(ulong userid, string username)
            {
                this.userid = userid;
                id = userid.ToString();
                displayName = username;
            }
            public Raider(BasePlayer target)
            {
                _player = target;
                userid = target.userID;
                id = target.UserIDString;
                displayName = target.displayName;
            }
            public void DestroyInput()
            {
                if (Input != null)
                {
                    Input.isDestroyed = true;
                    UnityEngine.Object.DestroyImmediate(Input);
                }
            }
            public void CheckInput(BasePlayer player, RaidableBase raid)
            {
                if (Input == null && player.IsOnline())
                {
                    _player = player;

                    Input = VLB.Utils.GetOrAddComponent<PlayerInputEx>(player.gameObject);

                    Input.Setup(raid, this);

                    raid.UpdateUi(player, UiType.Status);
                }
            }
        }

        public class RaidableBase : FacepunchBehaviour
        {
            public enum SkinType { Box, Deployable, Loot, Npc }
            private const float Radius = M_RADIUS;
            public HashSet<ulong> alliance = Pool.Get<HashSet<ulong>>();
            public HashSet<ulong> cooldowns = Pool.Get<HashSet<ulong>>();
            public HashSet<ulong> intruders = Pool.Get<HashSet<ulong>>();
            public Dictionary<ulong, Raider> raiders = Pool.Get<Dictionary<ulong, Raider>>();
            public Dictionary<ItemId, float> conditions = Pool.Get<Dictionary<ItemId, float>>();
            internal List<StorageContainer> fridges = new();
            internal HashSet<StorageContainer> _containers = new();
            internal HashSet<StorageContainer> _allcontainers = new();
            public List<HumanoidNPC> npcs = Pool.GetList<HumanoidNPC>();
            public List<HotAirBalloon> habs = Pool.GetList<HotAirBalloon>();
            public List<BaseMountable> mountables = Pool.GetList<BaseMountable>();
            public Dictionary<NetworkableId, BackpackData> backpacks = Pool.Get<Dictionary<NetworkableId, BackpackData>>();
            public List<Vector3> foundations = new();
            public List<Vector3> floors = new();
            public List<BaseEntity> locks = Pool.GetList<BaseEntity>();
            private List<BuildingBlock> blocks = Pool.GetList<BuildingBlock>();
            private List<Vector3> _inside = Pool.GetList<Vector3>();
            private List<SphereEntity> spheres = Pool.GetList<SphereEntity>();
            private List<IOEntity> lights = Pool.GetList<IOEntity>();
            private List<BaseOven> ovens = Pool.GetList<BaseOven>();
            public List<AutoTurret> turrets = Pool.GetList<AutoTurret>();
            private List<Door> doors = Pool.GetList<Door>();
            public List<string> ids = Pool.GetList<string>();
            private List<CustomDoorManipulator> doorControllers = Pool.GetList<CustomDoorManipulator>();
            private List<Locker> lockers = Pool.GetList<Locker>();
            private List<BaseEntity> _decorDeployables = Pool.GetList<BaseEntity>();
            private Dictionary<string, Dictionary<SkinType, ulong>> _shortnameToSkin = Pool.Get<Dictionary<string, Dictionary<SkinType, ulong>>>();
            private Dictionary<uint, ulong> _prefabToSkin = Pool.Get<Dictionary<uint, ulong>>();
            internal Dictionary<TriggerBase, BaseEntity> triggers = Pool.Get<Dictionary<TriggerBase, BaseEntity>>();
            private List<SleepingBag> _beds = Pool.GetList<SleepingBag>();
            private Dictionary<SleepingBag, ulong> _bags = Pool.Get<Dictionary<SleepingBag, ulong>>();
            private List<BaseCombatEntity> _rugs = Pool.GetList<BaseCombatEntity>();
            public List<SamSite> samsites = Pool.GetList<SamSite>();
            public List<VendingMachine> vms = Pool.GetList<VendingMachine>();
            public Dictionary<DamageType, float> PlayerDamageMultiplier = new();
            public BuildingPrivlidge priv;
            public List<ulong> TeleportExceptions = new();
            private List<string> murdererKits = new();
            private List<string> scientistKits = new();
            private MapMarkerExplosion explosionMarker;
            private MapMarkerGenericRadius genericMarker;
            private VendingMachineMapMarker vendingMarker;
            public Coroutine setupRoutine = null;
            public Coroutine turretsCoroutine = null;
            private GameObject go;
            private bool IsPrivDestroyed;
            public bool IsDespawning;
            public Vector3 Location;
            public string ProfileName;
            public float BaseHeight;
            public string BaseName;
            public Color NoneColor;
            public bool ownerFlag;
            public string ID = "0";
            public ulong ownerId;
            public string ownerName;
            public float loadTime;
            public DateTime spawnDateTime;
            public DateTime despawnDateTime = DateTime.MaxValue;
            public float AddNearTime;
            public bool AllowPVP;
            public BuildingOptions Options;
            public bool IsAuthed;
            public bool IsOpened = true;
            public bool IsResetting;
            public bool IsPayLocked;
            public int npcMaxAmountMurderers;
            public int npcMaxAmountScientists;
            public int npcMaxAmountInside = -1;
            public int npcAmountInside;
            public int npcAmountThrown;
            private bool isInvokingRespawnNpc;
            public RaidableType Type;
            public bool IsLoading;
            public bool InitiateTurretOnSpawn;
            private bool markerCreated;
            private bool lightsOn;
            private int itemAmountSpawned;
            public bool privSpawned;
            public bool privHadLoot;
            public string markerName;
            public string NoMode;
            public bool isAuthorized;
            public bool IsEngaged;
            public int _undoLimit;
            private Dictionary<Elevator, BMGELEVATOR> Elevators = new();
            public HashSet<BaseEntity> Entities = new();
            public HashSet<BaseEntity> DespawnExceptions = new();
            public Dictionary<BaseEntity, Vector3> BuiltList = new();
            public RaidableSpawns spawns;
            public RandomBase rb;
            public float RemoveNearDistance;
            public bool IsAnyLooted;
            public bool IsDamaged;
            public bool IsEligible = true;
            public bool IsCompleted;
            public Payments payments = new();
            public float ProtectionRadius => Options.ProtectionRadius(Type);
            public RaidableBases Instance;
            public bool stability;
            private int numLootRequired;

            public List<string> BlacklistedCommands => AllowPVP ? config.Settings.BlacklistedPVPCommands : config.Settings.BlacklistedPVECommands;
            public SpawnsControllerManager SpawnsController => Instance.SpawnsController;
            public StoredData data => Instance.data;
            public Configuration config => Instance.config;
            public bool IsUnloading => Instance.IsUnloading;
            public bool IsShuttingDown => Instance.IsShuttingDown;

            private object[] hookObjects => new object[16] { Location, (int)Options.Mode, AllowPVP, ID, 0f, 0f, loadTime, ownerId, GetOwner(), GetRaiders(), GetIntruders(), Entities.ToList(), BaseName, spawnDateTime, despawnDateTime, ProtectionRadius };

            public int DespawnMinutes => Options.DespawnOptions.OverrideConfig ? Options.DespawnOptions.DespawnMinutes : config.Settings.Management.DespawnMinutes;

            public bool DespawnMinutesReset => Options.DespawnOptions.OverrideConfig ? Options.DespawnOptions.DespawnMinutesReset : config.Settings.Management.DespawnMinutesReset;

            public int DespawnMinutesInactive => Options.DespawnOptions.OverrideConfig ? Options.DespawnOptions.DespawnMinutesInactive : config.Settings.Management.DespawnMinutesInactive;

            public bool DespawnMinutesInactiveReset => Options.DespawnOptions.OverrideConfig ? Options.DespawnOptions.DespawnMinutesInactiveReset : config.Settings.Management.DespawnMinutesInactiveReset;

            public string GetPercentCompleteMessage() => IsDespawning ? "DESPAWNING" : IsLoading ? "LOADING" : string.Join(", ", GetRaiders().Select(x => x.displayName)) is string str && !string.IsNullOrEmpty(str) ? str : "INACTIVE";

            public double GetPercentComplete() => IsDespawning ? 100.0 : IsLoading ? 0.0 : Math.Max(0.0, Math.Round((((double)numLootRequired - (double)GetLootAmountRemaining()) / (double)numLootRequired) * 100.0, 2));

            public int GetLootAmountRemaining()
            {
                int num = _containers.Sum(x => IsKilled(x) ? 0 : x.inventory.itemList.Count);

                if (num > numLootRequired)
                {
                    numLootRequired = num;
                }

                return num;
            }

            public bool IsBox(BaseEntity entity, bool inherit) => Instance.IsBox(entity, inherit);

            public string FormatGridReference(BasePlayer player, Vector3 v) => Instance.FormatGridReference(player, v);

            public void SetupCollider()
            {
                go = gameObject;
                transform.position = Location;
            }

            private void OnDestroy()
            {
                Despawn();
            }

            public bool CanDropBackpack(ulong userid)
            {
                if (AllowPVP && config.Settings.Management.BackpacksPVP || !AllowPVP && config.Settings.Management.BackpacksPVE)
                {
                    return raiders.TryGetValue(userid, out var ri) && ri.IsRaider && !userid.HasPermission("raidablebases.keepbackpackplugin");
                }
                return false;
            }

            public void UpdateUi(BasePlayer player, UiType type) => Instance.UI.UpdateUi(player, type);

            public void DestroyUi(BasePlayer player, UiType type) => Instance.UI.DestroyUi(player, type);

            public Raider GetRaider(BasePlayer player)
            {
                if (!raiders.TryGetValue(player.userID, out var ri))
                {
                    raiders[player.userID] = ri = new(player);
                }
                return ri;
            }

            public bool CanHurtBox(BaseEntity entity)
            {
                if (Options.InvulnerableUntilCupboardIsDestroyed && IsBox(entity, false) && !priv.IsKilled()) return false;
                if (Options.Invulnerable && IsBox(entity, false)) return false;
                return true;
            }

            public void DestroyGroundCheck(BaseEntity entity)
            {
                if (entity.GetParentEntity() is Tugboat) return;
                if (entity.TryGetComponent<GroundWatch>(out var obj1)) Destroy(obj1);
                if (entity.TryGetComponent<DestroyOnGroundMissing>(out var obj2)) Destroy(obj2);
            }

            public void SetupEntity(BaseEntity entity, bool skipCheck = true)
            {
                if (!entity.IsValid()) return;
                if (skipCheck) AddEntity(entity);
                else AddTemporary(entity);
                Instance.RaidEntities[entity.net.ID] = new(this, entity);
            }

            public void AddEntity(BaseEntity entity)
            {
                if (entity.IsValid())
                {
                    AddTemporary(entity);
                    Entities.Add(entity);
                }
            }

            private void AddTemporary(BaseEntity entity)
            {
                if (entity.HasParent() && entity.GetParentEntity() is Tugboat) return;
                if (Instance.temporaryData.NetworkIdentifiers.Contains(entity.net.ID.Value)) return;
                Instance.temporaryData.NetworkIdentifiers.Add(entity.net.ID.Value);
            }

            public void ResetToPool()
            {
                Interface.CallHook("OnRaidableBaseEnded", hookObjects);
                ids.ResetToPool();
                vms.ResetToPool();
                habs.ResetToPool();
                npcs.ResetToPool();
                _rugs.ResetToPool();
                _bags.ResetToPool();
                _beds.ResetToPool();
                doors.ResetToPool();
                locks.ResetToPool();
                ovens.ResetToPool();
                blocks.ResetToPool();
                lights.ResetToPool();
                lockers.ResetToPool();
                raiders.ResetToPool();
                spheres.ResetToPool();
                turrets.ResetToPool();
                _inside.ResetToPool();
                alliance.ResetToPool();
                samsites.ResetToPool();
                triggers.ResetToPool();
                backpacks.ResetToPool();
                intruders.ResetToPool();
                cooldowns.ResetToPool();
                conditions.ResetToPool();
                mountables.ResetToPool();
                _prefabToSkin.ResetToPool();
                doorControllers.ResetToPool();
                _shortnameToSkin.ResetToPool();
                _decorDeployables.ResetToPool();
            }

            public void Message(BasePlayer player, string key, params object[] args)
            {
                Instance.Message(player, key, args);
            }

            public void TryMessage(BasePlayer player, string key, params object[] args)
            {
                Instance.TryMessage(player, key, args);
            }

            public void QueueNotification(BasePlayer player, string key, params object[] args)
            {
                if (!Options.Smart)
                {
                    Instance.Message(player, key, args);
                }
            }

            public string mx(string key, string id = null, params object[] args) => Instance.mx(key, id, args);

            internal HashSet<BasePlayer> playersInSphere = new();
            internal HashSet<HotAirBalloon> habsInSphere = new();
            internal HashSet<BaseMountable> mountablesInSphere = new();
            internal List<BaseCombatEntity> checkEntities = new();
            internal float lastMountCheckTime;

            internal void CheckEntitiesInSphere()
            {
                if (IsUnloading || IsDespawning)
                {
                    return;
                }
                FindEntities(Location, ProtectionRadius, 134381576);
                CheckMountablesExiting();
                CheckMountablesEntering();
                CheckIntrudersExiting();
                CheckIntrudersEntering();
                habsInSphere.Clear();
                playersInSphere.Clear();
                mountablesInSphere.Clear();
            }

            public void FindEntities(Vector3 a, float n, int m)
            {
                checkEntities = FindEntitiesOfType<BaseCombatEntity>(a, n, m);
                for (int i = 0; i < checkEntities.Count; i++)
                {
                    var entity = checkEntities[i];
                    if (entity.IsKilled() || entity.IsNpc)
                    {
                        continue;
                    }
                    if (entity is BasePlayer player)
                    {
                        if (player.GetMounted() is ZiplineMountable)
                        {
                            continue;
                        }
                        playersInSphere.Add(player);
                    }
                    else if (entity is BaseMountable mount)
                    {
                        if (mount is ZiplineMountable)
                        {
                            continue;
                        }
                        mountablesInSphere.Add(mount);
                    }
                    else if (entity is HotAirBalloon hab)
                    {
                        habsInSphere.Add(hab);
                    }
                }
            }

            private void CheckMountablesExiting()
            {
                if (habs.Count > 0 && Time.time > lastMountCheckTime)
                {
                    lastMountCheckTime = Time.time + 1f;
                    habs.RemoveAll(m => m.IsKilled() || m.health <= 0 || m.IsDead() || !habsInSphere.Contains(m));
                }
                if (mountables.Count > 0 && Time.time > lastMountCheckTime)
                {
                    lastMountCheckTime = Time.time + 1f;
                    mountables.RemoveAll(m => m.IsKilled() || m.health <= 0 || m.IsDead() || !mountablesInSphere.Contains(m));
                }
            }

            private void CheckMountablesEntering()
            {
                if (habsInSphere.Count > 0)
                {
                    habsInSphere.ForEach(m =>
                    {
                        if (!habs.Contains(m))
                        {
                            var players = GetMountedPlayers(m);

                            if (TryRemoveMountable(m, players))
                            {
                                playersInSphere.RemoveWhere(players.Contains);
                            }
                            else habs.Add(m);
                        }
                    });
                }
                if (mountablesInSphere.Count > 0)
                {
                    mountablesInSphere.ForEach(m =>
                    {
                        if (!mountables.Contains(m))
                        {
                            var players = GetMountedPlayers(m);
                            if (TryRemoveMountable(m, players))
                            {
                                playersInSphere.RemoveWhere(players.Contains);
                            }
                            else
                            {
                                players.ForEach(OnPreEnterRaid);
                                mountables.Add(m);
                            }
                        }
                    });
                }
            }

            private void CheckIntrudersExiting()
            {
                if (intruders.Count > 0)
                {
                    raiders.Values.ForEach(ri =>
                    {
                        if (!playersInSphere.Exists(x => x?.userID == ri.userid) && intruders.Contains(ri.userid))
                        {
                            if (!ri.player.IsNull())
                            {
                                OnPlayerExit(ri.player, ri.player.IsDead());
                            }
                            else intruders.Remove(ri.userid);
                        }
                    });
                }
            }

            private void CheckIntrudersEntering()
            {
                if (playersInSphere.Count > 0)
                {
                    playersInSphere.ForEach(OnPreEnterRaid);
                }
            }

            public bool IsUnderground(Vector3 a) => a.y < Location.y && GamePhysics.CheckSphere<TerrainCollisionTrigger>(a, 5f, 262144, QueryTriggerInteraction.Collide);

            private void OnPreEnterRaid(BasePlayer target)
            {
                if (target.IsNull() || !target.IsHuman() || IsUnderground(target.transform.position))
                {
                    return;
                }

                if (target.IsDead())
                {
                    intruders.Remove(target.userID);
                    return;
                }

                if (intruders.Contains(target.userID) && raiders.ContainsKey(target.userID))
                {
                    GetRaider(target).CheckInput(target, this);
                    return;
                }

                if (!Options.Permission.Has(target, Type))
                {
                    Message(target, "No Permission To Enter");
                    RemovePlayer(target, Location, ProtectionRadius, Type);
                    return;
                }

                if (IsLoading && Type != RaidableType.None && !CanBypass(target))
                {
                    RemovePlayer(target, Location, ProtectionRadius, Type);
                    return;
                }

                if (RemoveFauxAdmin(target) || IsScavenging(target))
                {
                    return;
                }

                if (config.Settings.Management.AllowRespawn && target.IsSleeping() && target.lifeStory != null && target.lifeStory.secondsAlive <= 1.5f)
                {
                    TeleportExceptions.Add(target.userID);
                }

                OnEnterRaid(target, false);
            }

            public void OnEnterRaid(BasePlayer target, bool checkUnderground = true)
            {
                if (checkUnderground && IsUnderground(target.transform.position) || target.IsDead())
                {
                    intruders.Remove(target.userID);
                    return;
                }

                if (Type != RaidableType.None && CannotEnter(target, true))
                {
                    return;
                }

                Raider ri = GetRaider(target);

                ri.CheckInput(target, this);

                if (!intruders.Add(target.userID) && raiders.ContainsKey(target.userID))
                {
                    return;
                }

                Protector();

                if (!intruders.Contains(target.userID))
                {
                    return;
                }

                UpdateUi(target, UiType.Status);

                StopUsingWeapon(target);

                if (config.EventMessages.AnnounceEnterExit)
                {
                    QueueNotification(target, AllowPVP ? (Options.Eco.Enabled ? "OnPlayerEnteredEco" : "OnPlayerEntered") : (Options.Eco.Enabled ? "OnPlayerEnteredPVEEco" : "OnPlayerEnteredPVE"));
                }

                Interface.CallHook("OnPlayerEnteredRaidableBase", new object[] { target, Location, AllowPVP, (int)Options.Mode, ID, 0f, 0f, loadTime, ownerId, BaseName, spawnDateTime, despawnDateTime });

                if (config.Settings.Management.PVPDelay > 0)
                {
                    Interface.CallHook("OnPlayerPvpDelayEntry", new object[] { target, (int)Options.Mode, Location, AllowPVP, ID, 0f, 0f, loadTime, ownerId, BaseName, spawnDateTime, despawnDateTime });
                }

                foreach (var brain in Instance.HumanoidBrains.Values)
                {
                    if (InRange2D(brain.DestinationOverride, Location, brain.SenseRange))
                    {
                        brain.SwitchToState(AIState.Attack, -1);
                    }
                }

                UpdateTime(target, true);
                ri.PreEnter = false;
            }

            public void OnPlayerExit(BasePlayer target, bool skipDelay = true)
            {
                if (target == null || !target.IsHuman() || IsUnloading)
                {
                    return;
                }

                Raider ri = GetRaider(target);

                ri.DestroyInput();
                UpdateTime(target, false);
                DestroyUi(target, UiType.Status);

                if (!intruders.Remove(target.userID) || ri.PreEnter)
                {
                    return;
                }

                Interface.CallHook("OnPlayerExitedRaidableBase", new object[] { target, Location, AllowPVP, (int)Options.Mode, ID, 0f, 0f, loadTime, ownerId, BaseName, spawnDateTime, despawnDateTime });

                TrySetPVPDelay(target, skipDelay);

            enterExit:
                if (config.EventMessages.AnnounceEnterExit)
                {
                    QueueNotification(target, AllowPVP ? "OnPlayerExit" : "OnPlayerExitPVE");
                }
            }

            public bool CanSetPVPDelay(BasePlayer target)
            {
                return AllowPVP && config.Settings.Management.PVPDelayTrigger && target.userID.IsSteamId() && !InRange(Location, target.transform.position, ProtectionRadius);
            }

            public void TrySetPVPDelay(BasePlayer target, bool skipDelay = true, string key = "DoomAndGloom")
            {
                if (config.Settings.Management.PVPDelay <= 0f || skipDelay || !Instance.IsPVE() || !AllowPVP || target.IsFlying || target.limitNetworking)
                {
                    return;
                }

                if (config.EventMessages.AnnounceEnterExit)
                {
                    string arg = mx(GetAllowKey(), target.UserIDString).Replace("[", string.Empty).Replace("] ", string.Empty);
                    QueueNotification(target, key, arg, config.Settings.Management.PVPDelay);
                }

                if (!Instance.Get(target.userID, out DelaySettings ds))
                {
                    ulong userid = target.userID;
                    Instance.PvpDelay[userid] = ds = new()
                    {
                        Timer = Instance.timer.Once(config.Settings.Management.PVPDelay, () =>
                        {
                            Interface.CallHook("OnPlayerPvpDelayExpired", new object[] { target, (int)Options.Mode, Location, AllowPVP, ID, 0f, 0f, loadTime, ownerId, BaseName, spawnDateTime, despawnDateTime });
                            if (!config.UI.Delay.Enabled) Instance.PvpDelay.Remove(userid);
                        }),
                        AllowPVP = AllowPVP,
                        raid = this,
                        time = Time.time + config.Settings.Management.PVPDelay
                    };

                    UpdateUi(target, UiType.Delay);
                }
                else
                {
                    ds.Timer.Reset();
                    ds.time = Time.time + config.Settings.Management.PVPDelay;
                }
            }

            public string GetAllowKey()
            {
                if (Options.Eco.Enabled)
                {
                    return AllowPVP ? "PVPFlagEco" : "PVEFlagEco";
                }
                return AllowPVP ? "PVPFlag" : "PVEFlag";
            }

            private bool IsScavenging(BasePlayer player)
            {
                if (IsOpened || !config.Settings.Management.EjectScavengers || !ownerId.IsSteamId() || CanBypass(player))
                {
                    return false;
                }

                return !Any(player.userID) && !IsAlly(player) && RemovePlayer(player, Location, ProtectionRadius, Type);
            }

            private bool RemoveFauxAdmin(BasePlayer player)
            {
                if (Instance.FauxAdmin != null && player.IsReallyValid() && player.IsDeveloper && player.HasPermission("fauxadmin.allowed") && player.HasPermission("raidablebases.block.fauxadmin") && player.IsCheating())
                {
                    RemovePlayer(player, Location, ProtectionRadius, Type);
                    Message(player, "NoFauxAdmin");
                    return true;
                }

                return false;
            }

            private bool IsBanned(BasePlayer player)
            {
                if (player.HasPermission("raidablebases.banned") || IsPayLocked && player.HasPermission("raidablebases.buyraid.banned"))
                {
                    Message(player, player.IsAdmin ? "BannedAdmin" : "Banned");
                    return true;
                }

                return false;
            }

            private bool Teleported(BasePlayer player)
            {
                if (!config.Settings.Management.AllowTeleport && !TeleportExceptions.Contains(player.userID) && player.IsConnected && !CanBypass(player) && NearFoundation(player.transform.position) && Interface.CallHook("OnBlockRaidableBasesTeleport", player, Location) == null)
                {
                    Message(player, "CannotTeleport");
                    return true;
                }

                return false;
            }

            private bool IsHogging(BasePlayer player)
            {
                if (!config.Settings.Management.PreventHogging && !IsPayLocked || IsPayLocked && !config.Settings.Buyable.PreventHogging || !player.IsReallyValid() || CanBypass(player) || player.HasPermission("raidablebases.hoggingbypass"))
                {
                    return false;
                }

                foreach (var raid in Instance.Raids)
                {
                    if (raid.AllowPVP && config.Settings.Management.BypassUseOwnersForPVP)
                    {
                        continue;
                    }

                    if (!raid.AllowPVP && config.Settings.Management.BypassUseOwnersForPVE)
                    {
                        continue;
                    }

                    if (raid.IsPayLocked && !config.Settings.Buyable.PreventHogging)
                    {
                        continue;
                    }

                    if (raid.IsOpened && raid.Location != Location && raid.Any(player.userID, false))
                    {
                        TryMessage(player, "HoggingFinishYourRaid", FormatGridReference(player, raid.Location));
                        return true;
                    }
                }

                if (!config.Settings.Management.Lockout.IsBlocking() || player.HasPermission("raidablebases.blockbypass"))
                {
                    return false;
                }

                return IsAllyHogging(player);
            }

            public bool IsAllyHogging(BasePlayer player)
            {
                foreach (var raid in Instance.Raids)
                {
                    if (raid.Location != Location && !raid.IsPayLocked && raid.IsOpened && raid.Type != RaidableType.None && IsAllyHogging(player, raid))
                    {
                        TryMessage(player, "HoggingFinishYourRaid", FormatGridReference(player, raid.Location));

                        return true;
                    }
                }

                return false;
            }

            private bool IsAllyHogging(BasePlayer player, RaidableBase raid)
            {
                if (CanBypass(player))
                {
                    return false;
                }

                if (raid.AllowPVP && config.Settings.Management.BypassUseOwnersForPVP)
                {
                    return false;
                }

                if (!raid.AllowPVP && config.Settings.Management.BypassUseOwnersForPVE)
                {
                    return false;
                }

                foreach (var target in raid.GetIntruders().Where(x => x != player && !CanBypass(x)))
                {
                    if (config.Settings.Management.Lockout.BlockTeams && raid.IsAlly(player.userID, target.userID, AlliedType.Team))
                    {
                        TryMessage(player, "HoggingFinishYourRaidTeam", target.displayName, FormatGridReference(player, raid.Location));
                        return true;
                    }

                    if (config.Settings.Management.Lockout.BlockFriends && raid.IsAlly(player.userID, target.userID, AlliedType.Friend))
                    {
                        TryMessage(player, "HoggingFinishYourRaidFriend", target.displayName, FormatGridReference(player, raid.Location));
                        return true;
                    }

                    if (config.Settings.Management.Lockout.BlockClans && raid.IsAlly(player.userID, target.userID, AlliedType.Clan))
                    {
                        TryMessage(player, "HoggingFinishYourRaidClan", target.displayName, FormatGridReference(player, raid.Location));
                        return true;
                    }
                }

                return false;
            }

            private void CheckBackpacks(bool bypass = false)
            {
                foreach (var (uid, backpack) in backpacks.ToList())
                {
                    if (EjectBackpack(uid, backpack, bypass))
                    {
                        backpacks.Remove(uid);
                    }
                }
            }

            private float RadiationProtection(BasePlayer player)
            {
                float protection = Mathf.Ceil(player.RadiationProtection());

                if (player.modifiers == null)
                {
                    return protection;
                }

                return protection + (protection * Mathf.Clamp01(player.modifiers.GetValue(Modifier.ModifierType.Radiation_Exposure_Resistance)));
            }

            private void CheckRads(BasePlayer player)
            {
                if (!Options.Radiation.Enabled)
                {
                    return;
                }
                if (RadiationProtection(player) < Options.Radiation.Protection)
                {
                    if (Options.Radiation.Rads > 0f)
                    {
                        player.metabolism.radiation_poison.Add(Options.Radiation.Rads);
                        player.metabolism.radiation_level.value = Mathf.Max(1f, player.metabolism.radiation_poison.value / 5f);
                    }
                    if (Options.Radiation.Damage > 0)
                    {
                        player.Hurt(Options.Radiation.Damage, DamageType.Radiation);
                    }
                }
                else player.metabolism.radiation_level.value = 0f;
            }

            private void Protector()
            {
                if (IsDespawning)
                {
                    return;
                }

                if (DateTime.Now >= despawnDateTime)
                {
                    Despawn();
                    return;
                }

                if (backpacks.Count > 0)
                {
                    CheckBackpacks(!AllowPVP && Options.EjectBackpacksPVE);
                }

                if (Type == RaidableType.None || intruders.Count == 0)
                {
                    return;
                }

                foreach (var ri in raiders.Values.ToList())
                {
                    if (ri.player.IsNull() || !intruders.Contains(ri.userid) || RemoveFauxAdmin(ri.player))
                    {
                        continue;
                    }

                    if (IsBanned(ri.player))
                    {
                        ri.DestroyInput();
                        raiders.Remove(ri.userid);
                        intruders.Remove(ri.userid);
                        DestroyUi(ri.player, UiType.Status);
                        RemovePlayer(ri.player, Location, ProtectionRadius, Type);
                        continue;
                    }

                    if (config.Settings.Management.Mounts.Jetpacks && IsWearingJetpack(ri.player))
                    {
                        RemovePlayer(ri.player, Location, ProtectionRadius, Type, true);
                        continue;
                    }

                    CheckRads(ri.player);

                    if (ri.player.userID == ownerId || ri.IsAllowed || CanBypass(ri.player))
                    {
                        ri.IsAllowed = true;
                        continue;
                    }

                    if (CanEject(ri.player))
                    {
                        ri.DestroyInput();
                        raiders.Remove(ri.userid);
                        intruders.Remove(ri.userid);
                        DestroyUi(ri.player, UiType.Status);
                        RemovePlayer(ri.player, Location, ProtectionRadius, Type);
                        continue;
                    }

                    if (config.Settings.Management.LockToRaidOnEnter && !ri.LockedOnEnter)
                    {
                        QueueNotification(ri.player, "OnLockedToRaid");

                        ri.LockedOnEnter = true;
                    }

                    ri.IsAllowed = true;
                }
            }

            public void AddMember(ulong userid)
            {
                if (IsPayLocked && config.Settings.Buyable.Cooldowns.Has(data, userid, Options.Mode))
                {
                    cooldowns.Add(userid);
                }
                alliance.Add(userid);
            }

            public void FinalizeUi()
            {
                if (!raiders.IsNullOrEmpty())
                {
                    raiders.Values.ToList().ForEach(ri =>
                    {
                        if (ri.player.IsOnline())
                        {
                            if (intruders.Contains(ri.userid))
                            {
                                DestroyUi(ri.player, UiType.Status);
                            }
                            if (data.BuyableCooldowns.ContainsKey(ri.userid))
                            {
                                UpdateUi(ri.player, UiType.Cooldown);
                            }
                            else if (Instance.UI.Teleport.ContainsKey(ri.userid))
                            {
                                DestroyUi(ri.player, UiType.Teleport);
                            }
                        }
                        TrySetLockout(ri);
                    });
                }
            }

            public void StopSetupCoroutine()
            {
                if (setupRoutine != null)
                {
                    StopCoroutine(setupRoutine);
                    setupRoutine = null;
                }
                if (turretsCoroutine != null)
                {
                    StopCoroutine(turretsCoroutine);
                    turretsCoroutine = null;
                }
            }

            public void Despawn()
            {
                if (!IsDespawning)
                {
                    IsDespawning = true;
                    IsOpened = false;
                    TryInvokeMethod(SetNoDrops);
                    TryInvokeMethod(RemoveAllFromEvent);
                    TryInvokeMethod(StopSetupCoroutine);
                    TryInvokeMethod(StartPurchaseCooldown);
                    TryInvokeMethod(FinalizeUi);
                    TryInvokeMethod(DestroyLocks);
                    TryInvokeMethod(DestroyNpcs);
                    TryInvokeMethod(DestroyInputs);
                    TryInvokeMethod(DestroySpheres);
                    TryInvokeMethod(DestroyMapMarkers);
                    TryInvokeMethod(ResetSleepingBags);
                    TryInvokeMethod(DestroyEntities);
                    TryInvokeMethod(DestroyElevators);
                    TryInvokeMethod(CheckSubscribe);
                    TryInvokeMethod(ResetToPool);
                    Destroy(go);
                    Destroy(this);
                    LogEvent();
                }
            }

            public void LogEvent()
            {
                TryInvokeMethod(() => Instance.LogToFile("despawn", $"{BaseName} {ownerName ?? "N/A"} ({ownerId}) @ approx. {Instance.PositionToGrid(Location)} {Location} {Type}", Instance, true, true));
            }

            public static void TryInvokeMethod(Action action)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Puts("{0} ERROR: {1}", action.Method.Name, ex);
                }
            }

            public void RemoveAllFromEvent()
            {
                Interface.CallHook("OnRaidableBaseDespawn", hookObjects);

                GetIntruders().ToList().ForEach(player => OnPlayerExit(player));
            }

            public void CheckSubscribe()
            {
                Instance.Raids.Remove(this);

                if (Instance.Raids.Count == 0)
                {
                    if (IsUnloading)
                    {
                        Instance.UnsetStatics();
                    }
                    else
                    {
                        Instance.UnsubscribeHooks();
                    }
                }

                if (!IsUnloading)
                {
                    CheckPurchasedAmount();
                    spawns?.AddNear(Location, RemoveNearDistance, Options.Water.CacheType, AddNearTime);
                }
            }

            private void CheckPurchasedAmount()
            {
                if (Options.Silent || Type != RaidableType.Purchased || !config.EventMessages.PurchaseAvailable)
                {
                    return;
                }
                if (Instance.Get(Type) != config.Settings.Buyable.Max - 1)
                {
                    return;
                }
                foreach (var target in BasePlayer.activePlayerList)
                {
                    QueueNotification(target, "Purchase Available");
                }
            }

            public void DestroyElevators()
            {
                if (Elevators?.Count > 0)
                {
                    TryInvokeMethod(RemoveParentFromEntitiesOnElevators);
                    Elevators.Keys.ToList().ForEach(SafelyKill);
                }
            }

            public void DestroyEntities()
            {
                if (!IsShuttingDown)
                {
                    if (Entities.Count > 0)
                    {
                        Entities.RemoveWhere(DespawnExceptions.Contains);

                        Instance.UndoLoop(Entities.ToList(), _undoLimit, hookObjects);
                    }

                    SetPreventLooting();
                }
            }

            public void SetPreventLooting()
            {
                if (Options.PreventLooting <= 0f) return;
                ulong userid = ownerId;
                if (userid == 0uL)
                {
                    var owner = GetOwner();
                    if (owner == null) return;
                    userid = owner.userID;
                }
                foreach (var e in DespawnExceptions)
                {
                    if (e.IsKilled() || e.OwnerID != 0uL) continue;
                    if (e.ShortPrefabName != "item_drop") continue;
                    e.Invoke(() =>
                    {
                        if (e.IsKilled()) return;
                        e.OwnerID = 0uL;
                        e.skinID = 0uL;
                    }, Options.PreventLooting);
                    e.skinID = 14922524;
                    e.OwnerID = userid;
                }
            }

            public void OnBuildingPrivilegeDestroyed()
            {
                Interface.CallHook("OnRaidableBasePrivilegeDestroyed", hookObjects);
                IsPrivDestroyed = true;
                CreateSpheres();
                TryToEnd();
            }

            public void UpdateTime(BasePlayer player, bool state)
            {
                if (!player.IsConnected || !player.HasPermission("raidablebases.time"))
                {
                    return;
                }

                int time = state ? Options.ForcedTime : -1;

                if (player.IsAdmin)
                {
                    player.SendConsoleCommand("admintime", time);
                }
                else if (!player.IsFlying)
                {
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                    player.SendNetworkUpdateImmediate();
                    player.SendConsoleCommand("admintime", time);
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                }

                player.SendNetworkUpdateImmediate();
            }

            public bool IsOwnerConnected() => ownerId.IsSteamId() && RustCore.FindPlayerById(ownerId).IsOnline();

            public BasePlayer GetOwner()
            {
                BasePlayer owner = null;
                foreach (var x in raiders.Values)
                {
                    if (!x.player.IsReallyValid()) continue;
                    if (x.player.userID == ownerId) return x.player;
                    if (x.IsParticipant) owner = x.player;
                }
                return owner;
            }

            public List<BasePlayer> GetIntruders()
            {
                return raiders.Values.Where(x => intruders.Contains(x.userid) && x.player.IsReallyValid()).Select(x => x.player).ToList();
            }

            public List<BasePlayer> GetRaiders(bool participantOnly = true)
            {
                return raiders.Values.Where(x => x.player.IsReallyValid() && (!participantOnly || x.IsParticipant)).Select(x => x.player).ToList();
            }

            public bool AddLooter(BasePlayer looter, HitInfo hitInfo = null)
            {
                if (!looter.IsHuman())
                {
                    return false;
                }

                if (looter.IsFlying || looter.limitNetworking)
                {
                    return false;
                }

                if (!IsAlly(looter))
                {
                    if (hitInfo != null)
                    {
                        if (CanEject())
                        {
                            NullifyDamage(hitInfo);
                        }
                        if (!hitInfo.damageTypes.Has(DamageType.Heat))
                        {
                            TryMessage(looter, "NoDamageToEnemyBase");
                        }
                    }
                    else
                    {
                        Message(looter, "OwnerLocked");
                    }
                    return false;
                }

                if (IsHogging(looter))
                {
                    NullifyDamage(hitInfo);
                    return false;
                }

                GetRaider(looter).IsRaider = true;

                return true;
            }

            public bool IsDamageBlocked(BaseEntity entity)
            {
                return Options.BlockedEntityDamage.Exists(value => !string.IsNullOrEmpty(value) && (entity.ShortPrefabName.StartsWith(value, StringComparison.OrdinalIgnoreCase) || entity.GetType().Name == value));
            }

            public bool IsPickupBlacklisted(string name)
            {
                return Options.BlacklistedPickupItems.Exists(value => !string.IsNullOrEmpty(value) && name.Contains(value, CompareOptions.OrdinalIgnoreCase));
            }

            private void FillAmmoTurret(AutoTurret turret)
            {
                if (isAuthorized || IsUnloading || Type == RaidableType.None || turret.IsKilled())
                {
                    return;
                }

                if (turret.HasFlag(BaseEntity.Flags.OnFire))
                {
                    turret.SetFlag(BaseEntity.Flags.OnFire, false);
                }

                foreach (var id in turret.authorizedPlayers)
                {
                    if (id.userid.IsSteamId() && !CanBypassAuthorized(id.userid))
                    {
                        isAuthorized = true;
                        return;
                    }
                }

                if (!(turret.GetAttachedWeapon() is BaseProjectile attachedWeapon))
                {
                    turret.Invoke(() => FillAmmoTurret(turret), 0.2f);
                    return;
                }

                int p = Math.Max(config.Weapons.Ammo.AutoTurret, attachedWeapon.primaryMagazine.capacity);
                Item ammo = ItemManager.Create(attachedWeapon.primaryMagazine.ammoType, p, 0uL);
                if (!ammo.MoveToContainer(turret.inventory, -1, true, true, null, true)) ammo.Remove();
                attachedWeapon.primaryMagazine.contents = attachedWeapon.primaryMagazine.capacity;
                attachedWeapon.SendNetworkUpdateImmediate();
                turret.Invoke(turret.UpdateTotalAmmo, 0.25f);
            }

            private void FillAmmoGunTrap(GunTrap gt)
            {
                if (IsUnloading || isAuthorized || gt.IsKilled())
                {
                    return;
                }

                gt.ammoType ??= ItemManager.FindItemDefinition("ammo.handmade.shell");

                var ammo = gt.inventory.GetSlot(0);

                if (ammo == null)
                {
                    gt.inventory.AddItem(gt.ammoType, config.Weapons.Ammo.GunTrap);
                }
                else ammo.amount = config.Weapons.Ammo.GunTrap;
            }

            private ItemDefinition lowgradefuel;

            private void FillAmmoFogMachine(FogMachine fm)
            {
                if (IsUnloading || isAuthorized || fm.IsKilled())
                {
                    return;
                }

                lowgradefuel ??= ItemManager.FindItemDefinition("lowgradefuel");
                fm.inventory.AddItem(lowgradefuel, config.Weapons.Ammo.FogMachine);
            }

            private void FillAmmoFlameTurret(FlameTurret ft)
            {
                if (IsUnloading || isAuthorized || ft.IsKilled())
                {
                    return;
                }

                lowgradefuel ??= ItemManager.FindItemDefinition("lowgradefuel");
                ft.inventory.AddItem(lowgradefuel, config.Weapons.Ammo.FlameTurret);
            }

            private void FillAmmoSamSite(SamSite ss)
            {
                if (IsUnloading || isAuthorized || ss.IsKilled())
                {
                    return;
                }

                if (ss.ammoItem == null || !ss.HasAmmo())
                {
                    Item item = ItemManager.Create(ss.ammoType, config.Weapons.Ammo.SamSite);

                    if (!item.MoveToContainer(ss.inventory))
                    {
                        item.Remove();
                    }
                    else ss.ammoItem = item;
                }
                else if (ss.ammoItem.amount < config.Weapons.Ammo.SamSite)
                {
                    ss.ammoItem.amount = config.Weapons.Ammo.SamSite;
                }
            }

            private void OnWeaponItemPreRemove(Item item)
            {
                if (isAuthorized || IsUnloading || IsDespawning)
                {
                    return;
                }
                else if (!priv.IsKilled() && priv.authorizedPlayers.Exists(id => id.userid.IsSteamId() && !CanBypassAuthorized(id.userid)))
                {
                    isAuthorized = true;
                    return;
                }
                else if (privSpawned && priv.IsKilled())
                {
                    isAuthorized = true;
                    return;
                }

                var weapon = item.parent?.entityOwner;

                if (weapon is AutoTurret turret)
                {
                    weapon.Invoke(() => FillAmmoTurret(turret), 0.1f);
                }
                else if (weapon is GunTrap gt)
                {
                    weapon.Invoke(() => FillAmmoGunTrap(gt), 0.1f);
                }
                else if (weapon is SamSite ss)
                {
                    weapon.Invoke(() => FillAmmoSamSite(ss), 0.1f);
                }
            }

            public void TryToEnd()
            {
                if (IsOpened && !IsLoading && !IsCompleted && CanUndo())
                {
                    if (Options.DropPrivilegeLoot && privHadLoot && !priv.IsKilled())
                    {
                        Instance.DropOrRemoveItems(priv, this, true, true);
                    }
                    UnlockEverything();
                    AwardRaiders();
                    Undo();
                }
            }

            private void UnlockEverything()
            {
                if (Options.UnlockEverything)
                {
                    DestroyLocks();
                }
            }

            public bool GetInitiatorPlayer(HitInfo hitInfo, BaseCombatEntity entity, out BasePlayer target)
            {
                var weapon = hitInfo.Initiator ?? hitInfo.Weapon ?? hitInfo.WeaponPrefab;

                target = weapon switch
                {
                    BasePlayer player => player,
                    { creatorEntity: BasePlayer player } => player,
                    { parentEntity: EntityRef parentEntity } when parentEntity.Get(true) is BasePlayer player => player,
                    _ => hitInfo.IsMajorityDamage(DamageType.Heat) ? entity.lastAttacker as BasePlayer ?? GetArsonist() : null
                };

                return target != null;
            }

            private List<string> fireAmmoTypes = new() { "arrow.fire", "ammo.pistol.fire", "ammo.rifle.explosive", "ammo.rifle.incendiary", "ammo.shotgun.fire" };

            public BasePlayer GetArsonist()
            {
                return GetRaiders().FirstOrDefault(player =>
                {
                    if (player.svActiveItemID.IsValid && player.GetActiveItem() is Item item && item.GetHeldEntity() is BaseEntity e)
                    {
                        return e is FlameThrower || (e is BaseProjectile projectile && projectile.primaryMagazine.ammoType != null && fireAmmoTypes.Contains(projectile.primaryMagazine.ammoType.shortname));
                    }
                    return false;
                });
            }

            public void SetAllowPVP(RandomBase rb)
            {
                Type = rb.type;

                AllowPVP = Type switch
                {
                    RaidableType.Purchased when rb.payments.type != 0 => rb.payments.type == 2,
                    RaidableType.Maintained when config.Settings.Maintained.Chance > 0 => Convert.ToDecimal(UnityEngine.Random.Range(0f, 100f)) <= config.Settings.Maintained.Chance,
                    RaidableType.Scheduled when config.Settings.Schedule.Chance > 0 => Convert.ToDecimal(UnityEngine.Random.Range(0f, 100f)) <= config.Settings.Schedule.Chance,
                    RaidableType.Purchased when config.Settings.Buyable.ConvertPVP => false,
                    RaidableType.Purchased when config.Settings.Buyable.ConvertPVE => true,
                    RaidableType.Maintained when config.Settings.Maintained.ConvertPVP => false,
                    RaidableType.Maintained when config.Settings.Maintained.ConvertPVE => true,
                    RaidableType.Scheduled when config.Settings.Schedule.ConvertPVP => false,
                    RaidableType.Scheduled when config.Settings.Schedule.ConvertPVE => true,
                    RaidableType.Manual when config.Settings.Manual.ConvertPVP => false,
                    RaidableType.Manual when config.Settings.Manual.ConvertPVE => true,
                    _ => rb.options.AllowPVP
                };
            }

            private bool CancelOnServerRestart()
            {
                return config.Settings.Management.Restart && IsShuttingDown;
            }

            public void AwardRaiders()
            {
                //StartPurchaseCooldown();

                var sb = new StringBuilder();

                foreach (var ri in raiders.Values)
                {
                    if (ri.player.IsNull() || CancelOnServerRestart() || !IsEligible)
                    {
                        ri.reward = false;
                        continue;
                    }

                    if (ri.player.IsFlying)
                    {
                        if (config.EventMessages.Rewards.Flying) Message(ri.player, "No Reward: Flying");
                        ri.reward = false;
                        continue;
                    }

                    if (ri.player._limitedNetworking)
                    {
                        if (config.EventMessages.Rewards.Vanished) Message(ri.player, "No Reward: Vanished");
                        ri.reward = false;
                        continue;
                    }

                    if (!IsPlayerActive(ri.userid))
                    {
                        if (config.EventMessages.Rewards.Inactive) Message(ri.player, "No Reward: Inactive");
                        ri.reward = false;
                        continue;
                    }

                    if (config.Settings.Management.OnlyAwardOwner && ri.player.userID != ownerId && ownerId.IsSteamId())
                    {
                        if (config.EventMessages.Rewards.NotOwner) Message(ri.player, "No Reward: Not Owner");
                        ri.reward = false;
                    }

                    if (!ri.IsParticipant)
                    {
                        if (config.EventMessages.Rewards.NotParticipant) Message(ri.player, "No Reward: Not A Participant");
                        ri.reward = false;
                        continue;
                    }

                    if (config.Settings.Management.OnlyAwardAllies && ri.player.userID != ownerId && !IsAlly(ri.userid, ownerId))
                    {
                        if (config.EventMessages.Rewards.NotAlly) Message(ri.player, "No Reward: Not Ally");
                        ri.reward = false;
                    }

                    if (config.Settings.RemoveAdminRaiders && ri.player.IsAdmin && Type != RaidableType.None)
                    {
                        if (config.EventMessages.Rewards.RemoveAdmin) Message(ri.player, "No Reward: Admin");
                        ri.reward = false;
                        continue;
                    }

                    sb.Append(ri.displayName).Append(", ");
                }

                if (IsEligible)
                {
                    if (!CancelOnServerRestart())
                    {
                        Interface.CallHook("OnRaidableBaseCompleted", hookObjects);
                    }

                    if (!IsUnloading && Options.Levels.Level2 && npcMaxAmountMurderers + npcMaxAmountScientists > 0)
                    {
                        SpawnNpcs();
                    }

                    if (IsCompleted)
                    {
                        HandleAwards();
                    }
                }

                if (sb.Length == 0)
                {
                    return;
                }

                sb.Length -= 2;
                string thieves = sb.ToString();
                string con = mx(IsEligible ? "Thieves" : "ThievesDespawn", null, $"{mx($"Mode{Options.Mode}")} ({BaseName})", Instance.PositionToGrid(Location), thieves);

                Puts(con);

                if (config.EventMessages.AnnounceThief && IsEligible)
                {
                    foreach (var target in BasePlayer.activePlayerList)
                    {
                        QueueNotification(target, "Thieves", mx($"Mode{Options.Mode}", target.UserIDString), FormatGridReference(target, Location), thieves);
                    }
                }

                if (config.EventMessages.LogThieves)
                {
                    Instance.LogToFile("treasurehunters", $"{DateTime.Now} : {con}", Instance, false);
                }
            }

            private void RunCommands(BuildingOptionsCommands boc, ulong userid)
            {
                if (!boc.Enabled)
                {
                    return;
                }
                foreach (var command in boc.Commands)
                {
                    if (string.IsNullOrEmpty(command)) continue;
                    if (!CanAssignTo(userid, ownerId > 0 ? ownerId : userid, boc.Owner)) continue;
                    ConsoleSystem.Run(ConsoleSystem.Option.Server, command.Replace("{userid}", userid.ToString()));
                }
            }

            private int GetRankedLadderPointsForDifficulty(ulong id) => !CanAssignTo(id, ownerId, config.RankedLadder.Points.Owner) ? 0 : Options.Mode switch
            {
                RaidableMode.Easy => config.RankedLadder.Points.Easy,
                RaidableMode.Medium => config.RankedLadder.Points.Medium,
                RaidableMode.Hard => config.RankedLadder.Points.Hard,
                RaidableMode.Expert => config.RankedLadder.Points.Expert,
                _ => config.RankedLadder.Points.Nightmare,
            };

            private void HandleAwards()
            {
                foreach (var ri in raiders.Values)
                {
                    TrySetLockout(ri);

                    if (!ri.IsParticipant || !ri.reward)
                    {
                        continue;
                    }

                    if (config.RankedLadder.Enabled)
                    {
                        PlayerInfo playerInfo = PlayerInfo.Get(Instance.data, ri.id);

                        playerInfo.ResetExpiredDate();

                        int points = GetRankedLadderPointsForDifficulty(ri.userid);

                        playerInfo.TotalRaids++;
                        playerInfo.Raids++;
                        playerInfo.TotalPoints += points;
                        playerInfo.Points += points;

                        switch (Options.Mode)
                        {
                            case RaidableMode.Easy:
                                playerInfo.Easy++;
                                playerInfo.TotalEasy++;
                                playerInfo.EasyPoints += points;
                                playerInfo.TotalEasyPoints += points;

                                if (config.RankedLadder.Assign.Easy > 0 && playerInfo.Easy >= config.RankedLadder.Assign.Easy && CanAssignTo(ri.userid, ownerId, config.RankedLadder.Assign.Owner))
                                {
                                    AddGroupedPermission(ri.id, "raideasy", "raidablebases.ladder.easy");
                                    RunCommands(Options.EventRankedAwards, ri.userid);
                                }
                                break;
                            case RaidableMode.Medium:
                                playerInfo.Medium++;
                                playerInfo.TotalMedium++;
                                playerInfo.MediumPoints += points;
                                playerInfo.TotalMediumPoints += points;

                                if (config.RankedLadder.Assign.Medium > 0 && playerInfo.Medium >= config.RankedLadder.Assign.Medium && CanAssignTo(ri.userid, ownerId, config.RankedLadder.Assign.Owner))
                                {
                                    AddGroupedPermission(ri.id, "raidmedium", "raidablebases.ladder.medium");
                                    RunCommands(Options.EventRankedAwards, ri.userid);
                                }
                                break;
                            case RaidableMode.Hard:
                                playerInfo.Hard++;
                                playerInfo.TotalHard++;
                                playerInfo.HardPoints += points;
                                playerInfo.TotalHardPoints += points;

                                if (config.RankedLadder.Assign.Hard > 0 && playerInfo.Hard >= config.RankedLadder.Assign.Hard && CanAssignTo(ri.userid, ownerId, config.RankedLadder.Assign.Owner))
                                {
                                    AddGroupedPermission(ri.id, "raidhard", "raidablebases.ladder.hard");
                                    RunCommands(Options.EventRankedAwards, ri.userid);
                                }
                                break;
                            case RaidableMode.Expert:
                                playerInfo.Expert++;
                                playerInfo.TotalExpert++;
                                playerInfo.ExpertPoints += points;
                                playerInfo.TotalExpertPoints += points;

                                if (config.RankedLadder.Assign.Expert > 0 && playerInfo.Expert >= config.RankedLadder.Assign.Expert && CanAssignTo(ri.userid, ownerId, config.RankedLadder.Assign.Owner))
                                {
                                    AddGroupedPermission(ri.id, "raidexpert", "raidablebases.ladder.expert");
                                    RunCommands(Options.EventRankedAwards, ri.userid);
                                }
                                break;
                            case RaidableMode.Nightmare:
                                playerInfo.Nightmare++;
                                playerInfo.TotalNightmare++;
                                playerInfo.NightmarePoints += points;
                                playerInfo.TotalNightmarePoints += points;

                                if (config.RankedLadder.Assign.Nightmare > 0 && playerInfo.Nightmare >= config.RankedLadder.Assign.Nightmare && CanAssignTo(ri.userid, ownerId, config.RankedLadder.Assign.Owner))
                                {
                                    AddGroupedPermission(ri.id, "raidnightmare", "raidablebases.ladder.nightmare");
                                    RunCommands(Options.EventRankedAwards, ri.userid);
                                }
                                break;
                        }

                        RunCommands(Options.EventCompletion, ri.userid);

                        Interface.CallHook("OnRaidableAwardGiven", ri.displayName, ri.id, JsonConvert.SerializeObject(playerInfo));
                    }

                    if (Options.Rewards.NoBuyableRewards && IsPayLocked && payments.valid)
                    {
                        continue;
                    }

                    int total = raiders.Values.Count(x => x.IsParticipant && x.reward);

                    if (Options.Rewards.Custom.isValid && !ri.player.IsNull())
                    {
                        int amount = config.Settings.Management.DivideRewards ? Options.Rewards.Custom.Amount / total : Options.Rewards.Custom.Amount;
                        Item item = ItemManager.Create(Options.Rewards.Custom.Definition, amount, Options.Rewards.Custom.Skin);
                        if (item.skin != 0 && item.GetHeldEntity()) item.GetHeldEntity().skinID = item.skin;
                        if (!string.IsNullOrEmpty(Options.Rewards.Custom.Name)) item.name = Options.Rewards.Custom.Name;
                        if (!ri.player.inventory.GiveItem(item)) item.DropAndTossUpwards(ri.player.eyes.position);
                        QueueNotification(ri.player, "CustomDeposit", string.Format("{0} {1}", item.info.displayName.english, amount));
                    }

                    if (Options.Rewards.Money > 0 && Instance.Economics.CanCall())
                    {
                        double money = config.Settings.Management.DivideRewards ? Options.Rewards.Money / (double)total : Options.Rewards.Money;
                        Instance.Economics?.Call("Deposit", ri.id, money);
                        QueueNotification(ri.player, "EconomicsDeposit", money);
                    }

                    if (Options.Rewards.Money > 0 && Instance.BankSystem.CanCall())
                    {
                        int money = Convert.ToInt32(config.Settings.Management.DivideRewards ? Options.Rewards.Money / total : Options.Rewards.Money);
                        Instance.BankSystem?.Call("Deposit", ri.id, money);
                        QueueNotification(ri.player, "EconomicsDeposit", money);
                    }

                    if (Options.Rewards.Money > 0 && Instance.IQEconomic.CanCall())
                    {
                        int money = Convert.ToInt32(config.Settings.Management.DivideRewards ? Options.Rewards.Money / total : Options.Rewards.Money);
                        Instance.IQEconomic?.Call("API_SET_BALANCE", ri.userid, money);
                        QueueNotification(ri.player, "EconomicsDeposit", money);
                    }

                    if (Options.Rewards.Points > 0 && Instance.ServerRewards.CanCall())
                    {
                        int points = config.Settings.Management.DivideRewards ? Options.Rewards.Points / total : Options.Rewards.Points;
                        Instance.ServerRewards?.Call("AddPoints", ri.userid, points);
                        QueueNotification(ri.player, "ServerRewardPoints", points);
                    }

                    if (Options.Rewards.XP > 0 && Instance.SkillTree.CanCall())
                    {
                        double xp = config.Settings.Management.DivideRewards ? Options.Rewards.XP / (double)total : Options.Rewards.XP;
                        QueueNotification(ri.player, "SkillTreeXP", xp);
                        Instance.SkillTree?.Call("AwardXP", ri.player, xp);
                    }

                    if (Options.Rewards.XP > 0 && Instance.XPerience.CanCall())
                    {
                        double xp = config.Settings.Management.DivideRewards ? Options.Rewards.XP / (double)total : Options.Rewards.XP;
                        QueueNotification(ri.player, "SkillTreeXP", xp);
                        Instance.XPerience?.Call("GiveXPID", ri.userid, xp);
                    }
                }
            }

            private void AddGroupedPermission(string userid, string group, string perm)
            {
                if (userid.HasPermission("raidablebases.notitle"))
                {
                    return;
                }

                if (!userid.HasPermission(perm))
                {
                    Instance.permission.GrantUserPermission(userid, perm, Instance);
                }

                if (!Instance.permission.UserHasGroup(userid, group))
                {
                    Instance.permission.AddUserGroup(userid, group);
                }
            }

            private bool CanAssignTo(ulong userid, ulong owner, bool only)
            {
                return only == false || owner == 0uL || userid == owner;
            }

            public bool CanBypass(BasePlayer player)
            {
                return !player.IsHuman() || player.IsFlying || player.limitNetworking || player.HasPermission("raidablebases.canbypass");
            }

            private bool Exceeds(BasePlayer player)
            {
                if (IsPayLocked && player.userID == ownerId || CanBypass(player) || config.Settings.Management.Players.BypassPVP && AllowPVP)
                {
                    return false;
                }

                int amount = config.Settings.Management.Players.Get(Options.Mode, Type);

                if (amount == -1 || amount > 0 && GetParticipantsAmount() > amount)
                {
                    Message(player, "Event is full");
                    return true;
                }

                return false;
            }

            public int GetParticipantsAmount()
            {
                return raiders.Values.Count(x => x.player != null && !CanBypass(x.player));
            }

            public bool HasLockout(BasePlayer player, bool canMessage = true)
            {
                if (!config.Settings.Management.Lockout.Any() || player.IsNull() || CanBypass(player) || player.HasPermission("raidablebases.lockoutbypass") || Type == RaidableType.None)
                {
                    return false;
                }

                if (!IsOpened && Any(player.userID))
                {
                    return false;
                }

                if (player.userID == ownerId)
                {
                    return false;
                }

                if (config.Settings.Buyable.AllowAlly && IsAlly(ownerId, player.userID))
                {
                    return false;
                }

                if (data.Lockouts.TryGetValue(player.UserIDString, out var lo))
                {
                    double time = 0;

                    if (config.Settings.Management.Lockout.Global)
                    {
                        DateTime now = DateTime.Now;

                        foreach (var level in lo.Levels.Values)
                        {
                            time = Math.Max(time, (level > now) ? level.Subtract(now).TotalSeconds : 0);
                        }
                    }
                    else
                    {
                        time = lo.Get(Options.Mode);
                    }

                    if (time > 0f)
                    {
                        if (canMessage)
                        {
                            Message(player, "LockedOut", mx($"Mode{Options.Mode}", player.UserIDString), Instance.FormatTime(time, player.UserIDString));
                        }
                        return true;
                    }

                    if (!lo.Any())
                    {
                        data.Lockouts.Remove(player.UserIDString);
                    }
                }

                DestroyUi(player, UiType.Lockout);

                return false;
            }

            private void TrySetGlobalLockout(string playerId, BasePlayer player)
            {
                if (!data.Lockouts.TryGetValue(playerId, out var lo))
                {
                    data.Lockouts[playerId] = lo = new();
                }
                foreach (var mode in GetRaidableModes())
                {
                    lo.Set(mode, GetLockoutTime(mode));
                }
                if (lo.Any())
                {
                    UpdateUi(player, UiType.Lockout);
                }
                else data.Lockouts.Remove(playerId);
            }

            private List<ulong> lockFlags = new();

            private void TrySetLockout(Raider ri)
            {
                if (IsUnloading || IsPayLocked || IsResetting || ri == null || !ri.IsParticipant || Type == RaidableType.None || ri.id.HasPermission("raidablebases.canbypass") || ri.id.HasPermission("raidablebases.lockoutbypass"))
                {
                    return;
                }

                if (lockFlags.Contains(ri.userid) || AllowPVP && !config.Settings.Management.Lockout.PVP || !AllowPVP && !config.Settings.Management.Lockout.PVE)
                {
                    return;
                }

                if (ri.player.IsReallyValid() && (ri.player.IsFlying || ri.player._limitedNetworking))
                {
                    return;
                }

                lockFlags.Add(ri.userid);

                if (config.Settings.Management.Lockout.Global)
                {
                    TrySetGlobalLockout(ri.id, ri.player);
                    return;
                }

                double time = GetLockoutTime(Options.Mode);

                if (time <= 0)
                {
                    return;
                }

                if (!data.Lockouts.TryGetValue(ri.id, out var lo))
                {
                    data.Lockouts[ri.id] = lo = new();
                }

                lo.Set(Options.Mode, time);

                if (lo.Any())
                {
                    UpdateUi(ri.player, UiType.Lockout);
                }
                else data.Lockouts.Remove(ri.id);
            }

            private double GetLockoutTime(RaidableMode mode) => mode switch
            {
                RaidableMode.Easy => config.Settings.Management.Lockout.Easy * 60,
                RaidableMode.Medium => config.Settings.Management.Lockout.Medium * 60,
                RaidableMode.Hard => config.Settings.Management.Lockout.Hard * 60,
                RaidableMode.Expert => config.Settings.Management.Lockout.Expert * 60,
                _ => config.Settings.Management.Lockout.Nightmare * 60
            };

            public string Mode(string userid = null, bool forceShowName = false)
            {
                string text = rf(mx($"Mode{Options.Mode}", userid));
                if (Instance.GetRaidableMode(text) != RaidableMode.Random)
                {
                    text = text.SentenceCase();
                }
                if (ownerId.IsSteamId())
                {
                    return Instance.mx("Map Marker", null, config.Settings.Markers.ShowPurchased && IsPayLocked ? mx("Purchased") : string.Empty, config.Settings.Markers.ShowOwnersName || forceShowName ? ownerName : mx("Claimed"), text).Trim();
                }
                return text;
            }

            public void InvokeResetPayLock()
            {
                if (config.Settings.Buyable.ResetDuration > 0)
                {
                    if (IsInvoking(ResetPayLock))
                    {
                        CancelInvoke(ResetPayLock);
                    }
                    Invoke(ResetPayLock, config.Settings.Buyable.ResetDuration * 60f);
                }
            }

            private void TrySetPayLock()
            {
                if (config.Settings.Buyable.UsePayLock && rb.type == RaidableType.Purchased && rb.payments.valid)
                {
                    cooldowns.UnionWith(rb.members);
                    TrySetPayLock(rb.payments);
                }
            }

            public bool TrySetPayLock(Payments payments, bool forced = false)
            {
                InvokeResetPayLock();
                IsPayLocked = true;
                SetOwnerInternal(payments);
                ClearEnemies();
                Interface.CallHook("OnRaidableBasePurchased", new object[] { payments.userid.ToString(), Location, Instance.PositionToGrid(Location), (int)Options.Mode, AllowPVP, 0f, loadTime, BaseName, spawnDateTime, despawnDateTime });

                return true;
            }

            private void SetOwnerInternal(Payments payments)
            {
                if (config.Settings.Management.LockTime > 0f)
                {
                    if (IsInvoking(ResetPublicOwner))
                    {
                        CancelInvoke(ResetPublicOwner);
                    }
                    Invoke(ResetPublicOwner, config.Settings.Management.LockTime * 60f);
                }
                this.payments = payments;
                if (!raiders.TryGetValue(payments.userid, out var ri))
                {
                    raiders[payments.userid] = ri = new(payments.userid, payments.username);
                }
                ownerId = payments.userid;
                ownerName = payments.username;
                UpdateMarker();
                CreateSpheres();
            }

            private void SetOwner(BasePlayer owner)
            {
                SetOwnerInternal(new(owner) { Economics = new(Instance, owner) });
                ResetRaiderRelations();
                Protector();
            }

            public void StartPurchaseCooldown()
            {
                if (!IsResetting && !IsUnloading && IsPayLocked && ownerId.IsSteamId())
                {
                    config.Settings.Buyable.Cooldowns.Set(Instance, alliance, ownerId, Options.Mode, false);
                }
            }

            public bool HasBuyableCooldown(BasePlayer buyer, RaidableMode mode)
            {
                if (!IsDespawning && mode == Options.Mode && cooldowns.Contains(buyer.userID))
                {
                    Message(buyer, "BuyableAlreadyOwner");
                    return true;
                }
                return false;
            }

            public void Refund(BasePlayer player)
            {
                if (config.Settings.Buyable.Refunds.Percentage == 0 || !config.Settings.Buyable.Refunds.Enabled || !payments.valid)
                {
                    return;
                }

                if (IsDamaged && config.Settings.Buyable.Refunds.Damaged || IsAnyLooted && config.Settings.Buyable.Refunds.Damaged)
                {
                    return;
                }

                IsResetting = config.Settings.Buyable.Refunds.Reset;
                Reset(player);

                if (payments.Custom?.Options?.Count > 0)
                {
                    foreach (var option in payments.Custom.Options)
                    {
                        var def = ItemManager.FindItemDefinition(option.Shortname);

                        if (def == null)
                        {
                            continue;
                        }

                        int amount = Math.Max(1, (int)Math.Ceiling(option.Amount * config.Settings.Buyable.Refunds.Percentage / 100.0));

                        Item item = ItemManager.Create(def, amount, option.Skin);

                        if (!string.IsNullOrEmpty(option.Name))
                        {
                            item.name = option.Name;
                        }

                        player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);

                        QueueNotification(player, "Refunded Item", amount, string.IsNullOrEmpty(option.Name) ? item.info.displayName.english : option.Name);
                    }
                }

                if (payments.ServerRewards?.RP > 0)
                {
                    int points = (int)(payments.ServerRewards.RP * config.Settings.Buyable.Refunds.Percentage / 100.0);
                    Instance.ServerRewards?.Call("AddPoints", player.userID, points);
                    QueueNotification(player, "Refunded RP", points);
                }

                if (payments.Economics?.money > 0)
                {
                    double money = payments.Economics.money * config.Settings.Buyable.Refunds.Percentage / 100.0;
                    Instance.BankSystem?.Call("Deposit", player.userID, (int)money);
                    Instance.Economics?.Call("Deposit", player.userID, money);
                    Instance.IQEconomic?.Call("API_SET_BALANCE", player.userID, (int)money);
                    QueueNotification(player, "Refunded Money", money);
                }
            }

            private void Reset(BasePlayer player)
            {
                if (config.Settings.Buyable.Refunds.Reset)
                {
                    data.BuyableCooldowns.Remove(player.userID);
                }
            }

            private float PlayerActivityTimeLeft(ulong userid)
            {
                if (config.Settings.Management.LockTime <= 0f)
                {
                    return float.PositiveInfinity;
                }

                if (!raiders.TryGetValue(userid, out var raider))
                {
                    return float.PositiveInfinity;
                }

                return (config.Settings.Management.LockTime * 60f) - (Time.time - raider.lastActiveTime);
            }

            public bool IsPlayerActive(ulong userid)
            {
                return PlayerActivityTimeLeft(userid) > 0f;
            }

            public void TrySetOwner(BasePlayer attacker, BaseEntity entity, HitInfo hitInfo)
            {
                if (!config.Settings.Management.UseOwners)
                {
                    CreateSpheres();
                    return;
                }

                if (!IsOpened || ownerId.IsSteamId() || config.Settings.Management.PreventHogging && Instance.IsEventOwner(attacker, false))
                {
                    return;
                }

                if (config.Settings.Management.BypassUseOwnersForPVP && AllowPVP || config.Settings.Management.BypassUseOwnersForPVE && !AllowPVP)
                {
                    return;
                }

                if (HasLockout(attacker, !hitInfo.IsMajorityDamage(DamageType.Heat)) || IsHogging(attacker))
                {
                    NullifyDamage(hitInfo);
                    return;
                }

                if (entity is HumanoidNPC)
                {
                    SetOwner(attacker);
                    return;
                }

                if (!(entity is BuildingBlock) && !(entity is Door) && !(entity is SimpleBuildingBlock))
                {
                    return;
                }

                if (InRange2D(attacker.transform.position, Location, ProtectionRadius) || IsLootingWeapon(hitInfo))
                {
                    SetOwner(attacker);
                }
            }

            public void ResetRaiderRelations()
            {
                foreach (var ri in raiders.Values.ToList())
                {
                    if (ri.userid == ownerId)
                    {
                        continue;
                    }

                    ri.IsAllowed = false;
                    ri.IsAlly = false;
                }
            }

            public void ClearEnemies()
            {
                raiders.RemoveAll((uid, ri) => !IsAlly(ownerId, ri.userid));
            }

            public void CheckDespawn()
            {
                if (!IsOpened)
                {
                    if (DespawnMinutesReset)
                    {
                        UpdateDespawnDateTime(DespawnMinutes);
                    }
                    return;
                }

                if (IsDespawning || DespawnMinutesInactive <= 0f || config.Settings.Management.Engaged && !IsEngaged)
                {
                    return;
                }

                if (DespawnMinutesInactiveReset || despawnDateTime == DateTime.MaxValue)
                {
                    UpdateDespawnDateTime(DespawnMinutesInactive);
                }
            }

            public void UpdateDespawnDateTime(float time)
            {
                if (time > 0f)
                {
                    despawnDateTime = DateTime.Now.AddSeconds(time * 60f);
                }
                else
                {
                    despawnDateTime = DateTime.Now;
                }
            }

            public bool EndWhenCupboardIsDestroyed()
            {
                if (config.Settings.Management.EndWhenCupboardIsDestroyed && privSpawned)
                {
                    return IsCompleted = IsPrivDestroyed || priv.IsKilled() || privHadLoot && priv.inventory.IsEmpty();
                }

                return false;
            }

            public bool CanUndo()
            {
                if (EndWhenCupboardIsDestroyed())
                {
                    return IsCompleted = true;
                }

                if (config.Settings.Management.RequireCupboardLooted && privHadLoot && !IsPrivDestroyed)
                {
                    if (!priv.IsKilled() && !priv.inventory.IsEmpty())
                    {
                        return false;
                    }
                }

                foreach (var container in _containers)
                {
                    if (!container.IsKilled() && !container.inventory.IsEmpty() && IsBox(container, true))
                    {
                        return false;
                    }
                }

                foreach (string value in config.Settings.Management.Inherit)
                {
                    foreach (var container in _allcontainers)
                    {
                        if (container.IsKilled() || !container.ShortPrefabName.Contains(value, CompareOptions.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (!container.inventory.IsEmpty())
                        {
                            return false;
                        }
                    }
                }

                return IsCompleted = true;
            }

            private bool CanPlayerBeLooted(ulong looter, ulong target)
            {
                if (!config.Settings.Management.PlayersLootableInPVE && !AllowPVP || !config.Settings.Management.PlayersLootableInPVP && AllowPVP)
                {
                    return IsAlly(looter, target);
                }

                return true;
            }

            private bool CanBeLooted(BasePlayer player, BaseEntity e)
            {
                if (IsLoading)
                {
                    return CanBypassAuthorized(player.userID);
                }

                if (IsProtectedWeapon(e, true))
                {
                    if (config.Settings.Management.LootableTraps)
                    {
                        if (!CanBypassAuthorized(player.userID)) isAuthorized = true;

                        return true;
                    }

                    return false;
                }

                if (e is NPCPlayerCorpse)
                {
                    return true;
                }

                if (e is LootableCorpse corpse)
                {
                    if (CanBypass(player) || !corpse.playerSteamID.IsSteamId() || corpse.playerSteamID == player.userID || corpse.playerName == player.displayName)
                    {
                        return true;
                    }

                    return CanPlayerBeLooted(player.userID, corpse.playerSteamID);
                }
                else if (e is DroppedItemContainer container)
                {
                    if (CanBypass(player) || !container.playerSteamID.IsSteamId() || container.playerSteamID == player.userID || container.playerName == player.displayName)
                    {
                        return true;
                    }

                    return CanPlayerBeLooted(player.userID, container.playerSteamID);
                }

                return true;
            }

            public bool IsProtectedWeapon(BaseEntity e, bool checkBuiltList = false)
            {
                if (!e.IsReallyValid() || checkBuiltList && BuiltList.ContainsKey(e))
                {
                    return false;
                }

                return e is GunTrap || e is FlameTurret || e is FogMachine || e is SamSite || e is AutoTurret;
            }

            public object CanLootEntityInternal(BasePlayer player, BaseEntity entity)
            {
                if (!player || entity.OwnerID == player.userID || !entity.OwnerID.IsSteamId() && !Instance.Has(entity))
                {
                    return null;
                }

                if (!player.limitNetworking && IsPickupBlacklisted(entity.ShortPrefabName))
                {
                    return true;
                }

                if (entity.ShortPrefabName == "coffinstorage" && Mathf.Approximately(entity.transform.position.Distance(new(0f, -50f, 0f)), 0f))
                {
                    return null;
                }

                if (entity is BaseMountable || entity.HasParent() && entity.GetParentEntity() is BaseMountable)
                {
                    return null;
                }

                if (!player.limitNetworking && !CanBeLooted(player, entity))
                {
                    return true;
                }

                if (entity is LootableCorpse || entity is DroppedItemContainer)
                {
                    return null;
                }

                if (player.GetMounted())
                {
                    Message(player, "CannotBeMounted");
                    return true;
                }

                if (Options.RequiresCupboardAccess && !CanBuild(player))
                {
                    Message(player, "MustBeAuthorized");
                    return true;
                }

                if (Type != RaidableType.None && raiders.Values.Exists(ri => ri.IsParticipant))
                {
                    CheckDespawn();
                }

                if (player.IsFlying || player.limitNetworking)
                {
                    return null;
                }

                if (!AddLooter(player))
                {
                    return true;
                }

                AddMember(player.userID);

                return null;
            }

            public bool CanBuild(BasePlayer player)
            {
                if (privSpawned)
                {
                    return priv.IsKilled() || priv.IsAuthed(player);
                }
                return true;
            }

            public static void ClearInventory(ItemContainer container)
            {
                if (container == null || container.itemList == null)
                {
                    return;
                }
                Item[] array = container.itemList.ToArray();
                for (int i = 0; i < array.Length; i++)
                {
                    array[i].GetHeldEntity().SafelyKill();
                    array[i].RemoveFromContainer();
                    array[i].Remove(0f);
                }
            }

            public void SetNoDrops()
            {
                foreach (var container in _allcontainers)
                {
                    if (container.IsKilled())
                    {
                        continue;
                    }
                    if (!IsShuttingDown && IsCompleted && Options != null && Options.DropPrivilegeLoot && container is BuildingPrivlidge)
                    {
                        Instance.DropOrRemoveItems(container, this, true, true);
                    }
                    else
                    {
                        container.dropsLoot = false;
                        ClearInventory(container.inventory);
                    }
                }

                if (Type != RaidableType.None)
                {
                    foreach (var turret in turrets)
                    {
                        if (!turret.IsKilled())
                        {
                            if (!IsShuttingDown)
                            {
                                Interface.Oxide.NextTick(turret.SafelyKill);
                            }
                            ClearInventory(turret.inventory);
                        }
                    }
                }

                ItemManager.DoRemoves();
            }

            public void DestroyInputs()
            {
                raiders.Values.ToList().ForEach(ri => ri.DestroyInput());
            }

            public void Init(RandomBase rb, List<BaseEntity> entities = null)
            {
                RemoveNearDistance = rb.spawns == null ? rb.options.ProtectionRadius(rb.type) : rb.spawns.RemoveNear(rb.Position, rb.options.ProtectionRadius(rb.type), rb.options.Water.CacheType, rb.type);

                Instance.data.Cycle.Add(rb.type, rb.options.Mode, rb.BaseName, rb.owner);

                alliance.UnionWith(rb.members);

                if (!Options.Setup.BlockedPrefabs.IsNullOrEmpty())
                {
                    setupBlockedPrefabs.AddRange(Options.Setup.BlockedPrefabs);
                }

                rb.raid = this;
                this.rb = rb;
                spawns = rb.spawns;
                Elevators = BMGELEVATOR.FixElevators(this);

                TryInvokeMethod(() => AddEntities(entities));

                Interface.Oxide.NextTick(() =>
                {
                    TryInvokeMethod(SetCenterFromMultiplePoints);
                    TryInvokeMethod(TrySetPayLock);
                    TryInvokeMethod(SetupElevators);

                    setupRoutine = ServerMgr.Instance.StartCoroutine(EntitySetup());
                });
            }

            private void Teleport()
            {
                if (rb.IsTeleportPending(rb.owner, Location))
                {
                    if (rb.options.CustomSpawns.BuyableUiDuration > 0f)
                    {
                        Instance.UI.ShowBuyableTeleportUi(rb.owner, false, rb.options.CustomSpawns.BuyableUiDuration, rb.options.Mode);
                    }
                    else
                    {
                        Instance.BuyableTeleport(rb.owner);
                    }
                    InitiateTurretOnSpawn = true;
                }
            }

            private void SetupElevators()
            {
                if (Elevators == null || Elevators.Count == 0)
                {
                    return;
                }

                Elevators.ToList().ForEach(element => element.Value.Init(this));
            }

            private List<string> setupBlockedPrefabs = new();

            private void AddEntities(List<BaseEntity> entities)
            {
                if (entities.IsNullOrEmpty())
                {
                    return;
                }
                foreach (var e in entities.ToList())
                {
                    if (e.IsKilled() || setupBlockedPrefabs.Exists(e.ShortPrefabName.Contains))
                    {
                        if (e.IsValid()) Instance.temporaryData.NetworkIdentifiers.Remove(e.net.ID.Value);
                        Interface.Oxide.NextTick(e.SafelyKill);
                        entities.Remove(e);
                        continue;
                    }
                    if (e.ShortPrefabName == "foundation.triangle" || e.ShortPrefabName == "foundation" || e.skinID == 1337424001 && e is CollectibleEntity)
                    {
                        foundations.Add(e.transform.position);
                    }
                    if (e.ShortPrefabName.StartsWith("floor"))
                    {
                        floors.Add(e.transform.position);
                    }
                    e.OwnerID = 0;
                    AddEntity(e);
                }
                Instance.SaveTemporaryData();
            }

            private bool centerSetFromMultiplePoints;

            public void SetCenterFromMultiplePoints()
            {
                if (foundations.Count == 0)
                {
                    foundations.AddRange(floors);
                    floors = null;
                }

                Vector3 vector = Location;

                if (foundations.Count > 1)
                {
                    float x = 0f;
                    float z = 0f;

                    foreach (var position in foundations)
                    {
                        x += position.x;
                        z += position.z;
                    }

                    vector = new Vector3(x / foundations.Count, 0f, z / foundations.Count);
                }

                if (Options.Water.SpawnOnSeabed)
                {
                    if (!spawns.IsCustomSpawn) vector.y = TerrainMeta.HeightMap.GetHeight(vector);
                }
                else if (Options.Setup.ForcedHeight == -1)
                {
                    vector.y = SpawnsController.GetSpawnHeight(vector) + Options.Setup.PasteHeightAdjustment;
                }
                else vector.y = Options.Setup.ForcedHeight + Options.Setup.PasteHeightAdjustment;

                vector.y += BaseHeight;

                Location = vector;

                centerSetFromMultiplePoints = true;
            }

            private SphereColor GetSphereColor(SphereColorSettings sc)
            {
                if (sc.Unlocked != SphereColor.None && !ownerId.IsSteamId()) return sc.Unlocked;
                if (sc.Locked != SphereColor.None && ownerId.IsSteamId()) return sc.Locked;
                if (sc.PVPState != SphereColor.None && AllowPVP) return sc.PVPState;
                if (sc.PVEState != SphereColor.None && !AllowPVP) return sc.PVEState;
                return GetRaiders().Count > 0 ? sc.Active : sc.Inactive;
            }

            public void CreateSpheres()
            {
                if (Options.Silent)
                {
                    return;
                }

                if (!centerSetFromMultiplePoints)
                {
                    if (!IsInvoking(CreateSpheres))
                    {
                        Invoke(CreateSpheres, 0.1f);
                    }
                    return;
                }

                SphereColor sphereColor = GetSphereColor(Options.SphereColor);

                if (_currentSphereColor != SphereColor.None && _currentSphereColor == sphereColor)
                {
                    return;
                }

                if (_currentSphereColor == SphereColor.None && sphereColor == SphereColor.None && spheres.Count > 0)
                {
                    return;
                }

                DestroySpheres();

                void SpawnSphere(string prefab)
                {
                    if (StringPool.toNumber.ContainsKey(prefab) && GameManager.server.CreateEntity(prefab, Location) is SphereEntity sphere)
                    {
                        sphere.currentRadius = 1f;
                        sphere.Spawn();
                        sphere?.LerpRadiusTo(ProtectionRadius * 2f, ProtectionRadius * 0.75f);
                        spheres.Add(sphere);
                    }
                }

                List<string> prefabs = sphereColor switch
                {
                    SphereColor.Blue => new() { "assets/bundled/prefabs/modding/events/twitch/br_sphere.prefab" },
                    SphereColor.Cyan => new() { "assets/bundled/prefabs/modding/events/twitch/br_sphere.prefab", "assets/bundled/prefabs/modding/events/twitch/br_sphere_green.prefab" },
                    SphereColor.Green => new() { "assets/bundled/prefabs/modding/events/twitch/br_sphere_green.prefab" },
                    SphereColor.Magenta => new() { "assets/bundled/prefabs/modding/events/twitch/br_sphere_purple.prefab", "assets/bundled/prefabs/modding/events/twitch/br_sphere_red.prefab" },
                    SphereColor.Purple => new() { "assets/bundled/prefabs/modding/events/twitch/br_sphere_purple.prefab" },
                    SphereColor.Red => new() { "assets/bundled/prefabs/modding/events/twitch/br_sphere_red.prefab" },
                    SphereColor.Yellow => new() { "assets/bundled/prefabs/modding/events/twitch/br_sphere_red.prefab", "assets/bundled/prefabs/modding/events/twitch/br_sphere_green.prefab" },
                    _ => new(),
                };

                if (prefabs.Count > 0)
                {
                    prefabs.ForEach(SpawnSphere);
                }

                if (Options.SphereAmount > 0)
                {
                    for (int i = 0; i < Options.SphereAmount; i++)
                    {
                        SpawnSphere("assets/prefabs/visualization/sphere.prefab");
                    }
                }

                _currentSphereColor = sphereColor;
            }

            internal SphereColor _currentSphereColor;

            private void CreateZoneWalls()
            {
                if (!Options.ArenaWalls.Enabled)
                {
                    return;
                }

                var minHeight = 999f;
                var maxHeight = -999f;
                var maxDistance = 48f;
                var stacks = Options.ArenaWalls.Stacks;
                var center = new Vector3(Location.x, Location.y, Location.z);
                var gap = Options.ArenaWalls.Stone || Options.ArenaWalls.Ice ? 0.3f : 0.5f;
                var prefab = Options.ArenaWalls.Ice ? StringPool.Get(921229511) : Options.ArenaWalls.Stone ? StringPool.Get(1585379529) : StringPool.Get(1745077396);
                var next1 = Mathf.CeilToInt(360 / Options.ArenaWalls.Radius * 0.1375f);
                var next2 = 360 / Options.ArenaWalls.Radius - gap;
                var adjusted = false;

                if (Options.ArenaWalls.IgnoreForcedHeight && Options.Setup.ForcedHeight >= 0 && center.y >= Options.Setup.ForcedHeight)
                {
                    center.y = TerrainMeta.HeightMap.GetHeight(center);
                    adjusted = true;
                }

                foreach (var position in SpawnsController.GetCircumferencePositions(center, Options.ArenaWalls.Radius, next1, false, false, 1f))
                {
                    float y = SpawnsController.GetSpawnHeight(position, false, false, targetMask | Layers.Mask.Construction);
                    maxHeight = Mathf.Max(y, maxHeight, TerrainMeta.WaterMap.GetHeight(position));
                    minHeight = Mathf.Min(y, minHeight);
                    center.y = minHeight;
                }

                if (Options.Setup.ForcedHeight >= 0)
                {
                    maxDistance += Options.Setup.ForcedHeight + Options.Setup.PasteHeightAdjustment;

                    if (Options.ArenaWalls.LeastAmount && adjusted)
                    {
                        stacks += Mathf.CeilToInt((maxHeight - minHeight) / 6f);
                    }
                    else stacks = Mathf.CeilToInt((Options.Setup.ForcedHeight + Options.Setup.PasteHeightAdjustment) / 6f);
                }
                else if (Options.ArenaWalls.IgnoreWhenClippingTerrain)
                {
                    stacks += Mathf.CeilToInt((maxHeight - minHeight) / 6f);
                }

                for (int i = 0; i < stacks; i++)
                {
                    if (Options.ArenaWalls.LeastAmount && !Options.ArenaWalls.IgnoreForcedHeight && Options.Setup.ForcedHeight != -1 && i + 1 < stacks * 0.75)
                    {
                        center.y += 6f;
                        continue;
                    }

                    foreach (var position in SpawnsController.GetCircumferencePositions(center, Options.ArenaWalls.Radius, next2, false, false, center.y))
                    {
                        if (position.y - Location.y > maxDistance)
                        {
                            continue;
                        }

                        float spawnHeight = TerrainMeta.HeightMap.GetHeight(new(position.x, position.y + 6f, position.z));

                        if (i == 0 && position.y + 8.018669f < spawnHeight)
                        {
                            continue;
                        }

                        if (spawnHeight > position.y + 6.5f)
                        {
                            continue;
                        }

                        if (Options.ArenaWalls.LeastAmount)
                        {
                            float h = SpawnsController.GetSpawnHeight(position, !Options.Water.SpawnOnSeabed, false, targetMask | Layers.Mask.Construction);
                            float j = stacks * 6f + 6f;

                            if (position.y - spawnHeight > j && position.y < h)
                            {
                                continue;
                            }
                        }

                        var e = GameManager.server.CreateEntity(prefab, position, Quaternion.identity, true);

                        if (e.IsNull())
                        {
                            return;
                        }

                        e.transform.LookAt(center, Vector3.up);

                        if (Options.ArenaWalls.UseUFOWalls)
                        {
                            e.transform.Rotate(-67.5f, 0f, 0f);
                        }

                        e.Spawn();
                        SetupEntity(e);

                        if (Options.ArenaWalls.IgnoreWhenClippingTerrain && stacks == i - 1)
                        {
                            if (Physics.Raycast(new(position.x, position.y + 6.5f, position.z), Vector3.down, out var hit, 13f, targetMask))
                            {
                                if (hit.collider.ObjectName().Contains("rock") || hit.collider.ObjectName().Contains("formation", CompareOptions.OrdinalIgnoreCase))
                                {
                                    stacks++;
                                }
                            }
                        }
                    }

                    center.y += 6f;
                }
            }

            private void RemoveClutter()
            {
                foreach (var e in FindEntitiesOfType<BaseEntity>(Location, ProtectionRadius))
                {
                    if (e is TreeEntity)
                    {
                        if (!Entities.Contains(e))
                        {
                            if (Options.TreeRadius > 0f) Eject(e, Location, Options.TreeRadius, true);
                            else e.SafelyKill();
                        }
                    }
                    else if ((e is ResourceEntity || e is CollectibleEntity) && NearFoundation(e.transform.position))
                    {
                        Eject(e, Location, ProtectionRadius, true);
                    }
                    else if (e is HotAirBalloon && NearFoundation(e.transform.position, 10f) && !Entities.Contains(e))
                    {
                        TryEjectMountable(e);
                    }
                    else if (e is BaseMountable m && CanEjectMountable(m))
                    {
                        TryEjectMountable(e);
                    }
                    else if (e.GetParentEntity() is Tugboat)
                    {
                        continue;
                    }
                    else if (e is ScientistNPC && NearFoundation(e.transform.position, 15f))
                    {
                        e.SafelyKill();
                    }
                    else if (Instance.DeployableItems.ContainsKey(e.PrefabName) && !Entities.Contains(e))
                    {
                        DeployableItemHandler(e);
                    }
                    else if (e is DroppedItemContainer container && e.ShortPrefabName == "item_drop_backpack" && e.IsValid())
                    {
                        EjectionNotice(BasePlayer.FindByID(container.playerSteamID), container.transform.position);

                        Eject(e, Location, ProtectionRadius, true);
                    }
                    else if (e is LootableCorpse corpse)
                    {
                        EjectionNotice(BasePlayer.FindByID(corpse.playerSteamID), corpse.transform.position);

                        Eject(e, Location, ProtectionRadius, true);
                    }
                }
            }

            private bool CanEjectMountable(BaseMountable m)
            {
                if (Entities.Contains(m) || m.GetParentEntity() is TrainCar) return false;
                if (NearFoundation(m.transform.position, 10f)) return true;
                return config.Settings.Management.EjectMountables || TryRemoveMountable(m, new());
            }

            private void DeployableItemHandler(BaseEntity e)
            {
                if (e is SleepingBag bag)
                {
                    if (spawns.IsCustomSpawn && Options.CustomSpawns.KillSleepingBags)
                    {
                        bag.SafelyKill();
                        return;
                    }
                    _bags[bag] = bag.deployerUserID;
                    bag.deployerUserID = 0uL;
                    bag.unlockTime = UnityEngine.Time.realtimeSinceStartup + 99999f;
                }
                if (config.Settings.Management.KillDeployables && e.OwnerID.IsSteamId())
                {
                    Interface.Oxide.NextTick(e.SafelyKill);
                }
                else if (config.Settings.Management.EjectDeployables && e.OwnerID.IsSteamId())
                {
                    Eject(e, Location, ProtectionRadius + 10f, true);
                }
            }

            public void ResetSleepingBags()
            {
                foreach (var (bag, userid) in _bags)
                {
                    if (bag.IsKilled()) continue;
                    bag.deployerUserID = userid;
                    bag.unlockTime = UnityEngine.Time.realtimeSinceStartup;
                }
            }

            private IEnumerator EntitySetup()
            {
                if (Type != RaidableType.None)
                {
                    TryInvokeMethod(RemoveClutter);
                }

                int checks = 0;
                float invokeTime = 0f;
                int limit = Mathf.Clamp(Options.Setup.SpawnLimit, 1, 500);

                foreach (var e in Entities.ToList())
                {
                    TryInvokeMethod(() => TrySetupEntity(e, ref invokeTime));

                    if (++checks >= limit)
                    {
                        checks = 0;
                        yield return CoroutineEx.waitForSeconds(0.025f);
                    }
                }

                yield return CoroutineEx.waitForSeconds(2f);

                if (SetupLoot())
                {
                    TryInvokeMethod(Subscribe);
                    TryInvokeMethod(SetupTurrets);
                    TryInvokeMethod(CreateGenericMarker);
                    TryInvokeMethod(UpdateMarker);
                    TryInvokeMethod(EjectSleepers);
                    TryInvokeMethod(CreateZoneWalls);
                    TryInvokeMethod(CreateSpheres);
                    TryInvokeMethod(SetupLights);
                    TryInvokeMethod(SetupDoorControllers);
                    TryInvokeMethod(SetupDoors);
                    TryInvokeMethod(CheckDespawn);
                    TryInvokeMethod(SetupContainers);
                    TryInvokeMethod(MakeAnnouncements);
                    TryInvokeMethod(SetupRugs);
                    InvokeRepeating(Protector, 1f, 1f);
                    Interface.CallHook("OnRaidableBaseStarted", hookObjects);
                    Interface.CallHook("OnRaidableBaseStarted", rb);
                }
                else
                {
                    IsResetting = true;
                    payments.Refund();
                    Despawn();
                }

                TryInvokeMethod(Teleport);

                loadTime = Time.time - loadTime;
                IsLoading = false;
                Instance.IsSpawnerBusy = false;
                setupRoutine = null;
            }

            private void TrySetupEntity(BaseEntity e, ref float invokeTime)
            {
                if (!CanSetupEntity(e))
                {
                    return;
                }

                SetupEntity(e);

                e.OwnerID = 0;

                if (e.skinID == 1337424001 && e is CollectibleEntity ce)
                {
                    ce.itemList = null; // WaterBases compatibility
                }

                if (!Options.AllowPickup && e is BaseCombatEntity bce)
                {
                    SetupPickup(bce);
                }

                if (e is DecorDeployable && !_decorDeployables.Contains(e))
                {
                    _decorDeployables.Add(e);
                }

                if (e is IOEntity io)
                {
                    if (e is ContainerIOEntity cio)
                    {
                        SetupIO(cio);
                    }
                    else if (e is ElectricBattery eb)
                    {
                        SetupBattery(eb);
                    }
                    if (e is AutoTurret turret)
                    {
                        SetupTurret(turret);
                    }
                    else if (e is Igniter igniter)
                    {
                        SetupIgniter(igniter);
                    }
                    else if (e is SamSite ss)
                    {
                        SetupSamSite(ss);
                    }
                    else if (e is TeslaCoil tc)
                    {
                        SetupTeslaCoil(tc);
                    }
                    else if (e.PrefabName.Contains("light"))
                    {
                        SetupLight(io);
                    }
                    else if (e is CustomDoorManipulator cdm)
                    {
                        doorControllers.Add(cdm);
                    }
                    else if (e is HBHFSensor sensor)
                    {
                        SetupHBHFSensor(sensor);
                    }
                    else if (e is ElectricGenerator generator)
                    {
                        SetupGenerator(generator);
                    }
                    else if (e is PressButton button)
                    {
                        SetupButton(button);
                    }
                    else if (e is FogMachine fm)
                    {
                        SetupFogMachine(fm);
                    }
                }
                else if (e is StorageContainer container)
                {
                    SetupContainer(container);

                    if (e is BaseOven oven)
                    {
                        ovens.Add(oven);
                    }
                    else if (e is FlameTurret ft)
                    {
                        SetupFlameTurret(ft);
                    }
                    else if (e is VendingMachine vm)
                    {
                        SetupVendingMachine(vm);
                    }
                    else if (e is BuildingPrivlidge priv)
                    {
                        SetupBuildingPriviledge(priv);
                    }
                    else if (e is Locker locker)
                    {
                        SetupLocker(locker);
                    }
                    else if (e is GunTrap gt)
                    {
                        SetupGunTrap(gt);
                    }
                    else if (e is LootContainer lc)
                    {
                        //SetupLootContainer(lc);
                    }
                }
                else if (e is BuildingBlock block)
                {
                    SetupBuildingBlock(block);
                }
                else if (e is BaseLock)
                {
                    SetupLock(e);
                }
                else if (e is SleepingBag bag)
                {
                    SetupSleepingBag(bag);
                }
                else if (e is BaseMountable m)
                {
                    SetupMountable(m);
                }
                else if (e is CollectibleEntity ce2)
                {
                    SetupCollectible(ce2);
                }
                else if (e is SpookySpeaker speaker)
                {
                    SetupSpookySpeaker(speaker);
                }

                if (e is DecayEntity de)
                {
                    SetupDecayEntity(de);
                }

                if (e is Door door)
                {
                    SetupDoor(door);
                }
                else SetupSkin(e);
            }

            private void SetupLights()
            {
                if (!config.Settings.Management.NightLantern && Instance.NightLantern.CanCall())
                {
                    return;
                }

                if (config.Settings.Management.Lights || config.Settings.Management.AlwaysLights)
                {
                    ToggleLights();
                }
            }

            public bool IsPasted;

            public void CheckPaste()
            {
                if (IsPasted || !IsLoading)
                {
                    return;
                }
                if (Time.time - loadTime > 900)
                {
                    Puts("{0} @ {1} timed out after 15 minutes of no response from CopyPaste; despawning...", BaseName, Instance.PositionToGrid(Location));
                    IsLoading = false;
                    Despawn();
                    return;
                }
                Invoke(CheckPaste, 1f);
            }

            private void SetupContainers()
            {
                foreach (var container in _containers)
                {
                    if (!container.IsKilled())
                    {
                        container.SendNetworkUpdate();
                    }
                }
            }

            private void SetupPickup(BaseCombatEntity e)
            {
                e.pickup.enabled = false;
            }

            private void AddContainer(StorageContainer container)
            {
                if (IsBox(container, true) || container is BuildingPrivlidge)
                {
                    _containers.Add(container);
                }

                if (container.ShortPrefabName == "fridge.deployed")
                {
                    fridges.Add(container);
                }

                _allcontainers.Add(container);

                AddEntity(container);
            }

            private void RemoveContainer(StorageContainer container)
            {
                if (!container.IsKilled())
                {
                    _allcontainers.Remove(container);
                    _containers.Remove(container);
                    Entities.Remove(container);
                    container.dropsLoot = false;
                    Interface.Oxide.NextTick(container.SafelyKill);
                }
            }

            public void TryEmptyContainer(StorageContainer container)
            {
                if (Options.EmptyAll && Type != RaidableType.None && !Options.EmptyExemptions.Exists(container.ShortPrefabName.Contains))
                {
                    ClearInventory(container.inventory);
                    ItemManager.DoRemoves();
                }
                container.dropsLoot = false;
                container.dropFloats = false;
            }

            private void SetupContainer(StorageContainer container)
            {
                AddContainer(container);

                if (container.inventory == null)
                {
                    container.inventory = new();
                    container.inventory.ServerInitialize(null, 48);
                    container.inventory.GiveUID();
                    container.inventory.entityOwner = container;
                }
                else TryEmptyContainer(container);

                SetupBoxSkin(container);

                if (Type == RaidableType.None && container.inventory.itemList.Count > 0)
                {
                    return;
                }

                container.dropsLoot = false;
                container.dropFloats = false;

                if (container is BuildingPrivlidge)
                {
                    container.dropsLoot = config.Settings.Management.AllowCupboardLoot;
                }
                else if (!IsProtectedWeapon(container) && !(container is VendingMachine))
                {
                    container.dropsLoot = true;
                }

                if (IsBox(container, false) || container is BuildingPrivlidge)
                {
                    container.inventory.SetFlag(ItemContainer.Flag.NoItemInput, Options.NoItemInput);
                }

                if (IsBox(container, false))
                {
                    CreateLock(container, Options.KeyLockBoxes, Options.CodeLockBoxes);
                }

                if (container is Locker)
                {
                    CreateLock(container, Options.KeyLockLockers, Options.CodeLockLockers);
                }
            }

            private void SetupIO(ContainerIOEntity io)
            {
                io.dropFloats = false;
                io.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);
                io.dropsLoot = IsProtectedWeapon(io) ? config.Settings.Management.DropLoot.Get(io) : true;
            }

            private void SetupIO(IOEntity io)
            {
                io.SetFlag(BaseEntity.Flags.Reserved8, true, false, true);
            }

            private void SetupLock(BaseEntity e, bool justCreated = false)
            {
                AddEntity(e);
                locks.Add(e);

                if (e.IsValid())
                {
                    Instance.RaidEntities[e.net.ID] = new(this, e);
                }

                if (Type == RaidableType.None)
                {
                    return;
                }

                if (e is CodeLock codeLock)
                {
                    if (config.Settings.Management.RandomCodes || justCreated)
                    {
                        codeLock.code = UnityEngine.Random.Range(1000, 9999).ToString();
                        codeLock.hasCode = true;
                    }

                    codeLock.OwnerID = 0;
                    codeLock.guestCode = string.Empty;
                    codeLock.hasGuestCode = false;
                    codeLock.guestPlayers.Clear();
                    codeLock.whitelistPlayers.Clear();
                    codeLock.SetFlag(BaseEntity.Flags.Locked, true);
                }
                else if (e is KeyLock keyLock)
                {
                    if (config.Settings.Management.RandomCodes)
                    {
                        keyLock.keyCode = UnityEngine.Random.Range(1, 100000);
                    }

                    keyLock.OwnerID = 0;
                    keyLock.firstKeyCreated = true;
                    keyLock.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }

            private void SetupVendingMachine(VendingMachine vm)
            {
                vms.Add(vm);

                vm.SetFlag(BaseEntity.Flags.Reserved4, config.Settings.Management.AllowBroadcasting, false, true);
                vm.FullUpdate();
            }

            private void SetupLight(IOEntity light)
            {
                if (light == null || !config.Settings.Management.Lights && !config.Settings.Management.AlwaysLights || config.Settings.Management.IgnoredLights.Exists(light.ShortPrefabName.Contains))
                {
                    return;
                }

                lights.Add(light);
            }

            private void SetupHBHFSensor(HBHFSensor sensor)
            {
                triggers[sensor.myTrigger] = sensor;
                SetupIO(sensor);
                sensor.SetFlag(HBHFSensor.Flag_IncludeAuthed, true, false, true);
                sensor.SetFlag(HBHFSensor.Flag_IncludeOthers, true, false, true);
            }

            private void SetupBattery(ElectricBattery eb)
            {
                eb.rustWattSeconds = eb.maxCapactiySeconds - 1f;
            }

            private void SetupGenerator(ElectricGenerator generator)
            {
                generator.electricAmount = config.Weapons.TestGeneratorPower;
            }

            private void SetupLootContainer(LootContainer lc)
            {
                if (!NearFoundation(lc.transform.position)) return;
                if (lc.TryGetComponent<GroundWatch>(out var gw)) Destroy(gw);
                if (lc.TryGetComponent<DestroyOnGroundMissing>(out var gm)) Destroy(gm);
            }

            private void SetupButton(PressButton button)
            {
                button._maxHealth = Options.Elevators.ButtonHealth;
                button.InitializeHealth(Options.Elevators.ButtonHealth, Options.Elevators.ButtonHealth);
            }

            private void SetupBuildingBlock(BuildingBlock block)
            {
                if (block.IsKilled())
                {
                    return;
                }

                if (blockPrefabs.Contains(block.ShortPrefabName))
                {
                    blocks.Add(block);
                }

                if (!IsUnloading)
                {
                    ChangeTier(block);
                    block.StopBeingDemolishable();
                    block.StopBeingRotatable();
                }
            }

            private List<string> blockPrefabs = new() { "foundation.triangle", "foundation", "floor.triangle", "floor", "roof", "roof.triangle" };

            private void ChangeTier(BuildingBlock block)
            {
                if (Options.Blocks.Exclusions.Contains(BaseName))
                {
                    if (!Options.Blocks.HasSkin(block, block.grade, block.skinID))
                    {
                        block.skinID = 0uL;
                        block.UpdateSkin();
                    }
                    return;
                }
                BuildingGrade.Enum grade = Options.Blocks switch
                {
                    { HQM: true } => BuildingGrade.Enum.TopTier,
                    { Metal: true } => BuildingGrade.Enum.Metal,
                    { Stone: true } => BuildingGrade.Enum.Stone,
                    { Wooden: true } => BuildingGrade.Enum.Wood,
                    _ => block.grade
                };
                ulong skinID = Options.Blocks.GetSkin(block, grade, block.skinID);
                if (Options.Blocks.RandomWhole)
                {
                    if (!skinWhole.TryGetValue(grade, out var skin))
                    {
                        skinWhole[grade] = skin = skinID;
                    }
                    skinID = skin;
                }
                if (!Options.Blocks.HasSkin(block, grade, skinID))
                {
                    skinID = 0uL;
                }
                block.skinID = skinID;
                block.SetGrade(grade);
                block.SetHealthToMax();
                block.UpdateSkin();
                block.SendNetworkUpdate();
                if (grade != BuildingGrade.Enum.Metal)
                {
                    return;
                }
                if (Options.Blocks.IdenticalColour)
                {
                    if (!skinColors.TryGetValue(block.grade, out var color))
                    {
                        skinColors[block.grade] = color = block.currentSkin.GetStartingDetailColour(0u);
                    }
                    block.SetCustomColour(color);
                }
                else if (Options.Blocks.RandomColour)
                {
                    block.SetCustomColour(block.currentSkin.GetStartingDetailColour(0u));
                }
            }

            private Dictionary<BuildingGrade.Enum, ulong> skinWhole = new();
            private Dictionary<BuildingGrade.Enum, uint> skinColors = new();

            private void SetupTeslaCoil(TeslaCoil tc)
            {
                if (!Options.TeslaCoil.RequiresPower)
                {
                    tc.UpdateFromInput(25, 0);
                    tc.SetFlag(IOEntity.Flag_HasPower, true, false, true);
                }

                tc.InitializeHealth(Options.TeslaCoil.Health, Options.TeslaCoil.Health);
                tc.maxDischargeSelfDamageSeconds = Mathf.Clamp(Options.TeslaCoil.MaxDischargeSelfDamageSeconds, 0f, 9999f);
                tc.maxDamageOutput = Mathf.Clamp(Options.TeslaCoil.MaxDamageOutput, 0f, 9999f);
            }

            private void SetupIgniter(Igniter igniter)
            {
                igniter.SelfDamagePerIgnite = 0f;
            }

            public void PreSetupTurret(AutoTurret turret)
            {
                turret.skinID = 14922524;
                turret.dropsLoot = false;
                if (turret.targetTrigger != null)
                {
                    triggers[turret.targetTrigger] = turret;
                }
            }

            private void SetupTurret(AutoTurret turret)
            {
                triggers[turret.targetTrigger] = turret;

                if (IsUnloading || Type == RaidableType.None)
                {
                    return;
                }

                if (config.Settings.Management.ClippedTurrets && turret.RCEyes != null)
                {
                    var position = turret.RCEyes.position;

                    if (TestClippedInside(position, 0.5f, Layers.Mask.Terrain) || IsRockFaceUpwardsSecondary(position))
                    {
                        Entities.Remove(turret);
                        turrets.Remove(turret);
                        turret.dropsLoot = false;
                        Interface.Oxide.NextTick(turret.SafelyKill);
                        return;
                    }
                }

                if (turret is NPCAutoTurret)
                {
                    turret.baseProtection = Instance.GetTurretProtection();
                    BMGELEVATOR.RemoveImmortality(turret.baseProtection, 1f, 1f, 1f, 1f, 1f, 0.8f, 1f, 1f, 1f, 0.9f, 0.5f, 0.5f, 1f, 1f, 0f, 0.5f, 0f, 1f, 1f, 0f, 1f, 0.9f, 0f, 1f, 0f);
                }

                SetupIO(turret as IOEntity);

                if (Type != RaidableType.None)
                {
                    turret.authorizedPlayers.Clear();
                }

                Options.AutoTurret.Shortnames.Remove("fun.trumpet");

                turret.InitializeHealth(Options.AutoTurret.Health, Options.AutoTurret.Health);
                //turret.targetTrigger.GetComponent<SphereCollider>().radius = Options.AutoTurret.SightRange;
                turret.sightRange = Options.AutoTurret.SightRange;
                turret.aimCone = Options.AutoTurret.AimCone;
                turrets.Add(turret);

                if (Options.AutoTurret.RemoveWeapon)
                {
                    turret.AttachedWeapon = null;
                    Item slot = turret.inventory.GetSlot(0);

                    if (slot != null && (slot.info.category == ItemCategory.Weapon || slot.info.category == ItemCategory.Fun))
                    {
                        slot.RemoveFromContainer();
                        slot.Remove();
                    }
                }

                if (Options.AutoTurret.Hostile)
                {
                    turret.SetPeacekeepermode(false);
                }

                if (config.Weapons.InfiniteAmmo.AutoTurret)
                {
                    turret.inventory.onPreItemRemove += new Action<Item>(OnWeaponItemPreRemove);
                }
            }

            private void SetupTurrets()
            {
                if (Type != RaidableType.None && turrets.Count > 0)
                {
                    turretsCoroutine = ServerMgr.Instance.StartCoroutine(TurretsCoroutine());
                }
                else SetupNpcKits();
            }

            private IEnumerator TurretsCoroutine()
            {
                if (InitiateTurretOnSpawn)
                {
                    while (IsLoading)
                    {
                        yield return CoroutineEx.waitForSeconds(0.1f);
                    }
                }

                foreach (var turret in turrets.ToList())
                {
                    yield return CoroutineEx.waitForSeconds(0.025f);

                    EquipTurretWeapon(turret);

                    yield return CoroutineEx.waitForSeconds(0.025f);

                    UpdateAttachedWeapon(turret);

                    yield return CoroutineEx.waitForSeconds(0.025f);

                    InitiateStartup(turret);

                    yield return CoroutineEx.waitForSeconds(0.025f);

                    FillAmmoTurret(turret);

                    if (turret != null && turret.HasFlag(BaseEntity.Flags.OnFire))
                    {
                        turret.SetFlag(BaseEntity.Flags.OnFire, false);
                    }
                }

                SetupNpcKits();

                turretsCoroutine = null;
            }

            private void EquipTurretWeapon(AutoTurret turret)
            {
                if (Options.AutoTurret.Shortnames.Count > 0 && !turret.IsKilled() && turret.AttachedWeapon.IsNull())
                {
                    var shortname = Options.AutoTurret.Shortnames.GetRandom();
                    var itemToCreate = ItemManager.FindItemDefinition(shortname);

                    if (itemToCreate != null)
                    {
                        Item item = ItemManager.Create(itemToCreate, 1, itemToCreate.skins2.IsNullOrEmpty() ? 0 : itemToCreate.skins2.GetRandom().WorkshopId);

                        if (item.MoveToContainer(turret.inventory, 0, false))
                        {
                            item.SwitchOnOff(true);
                        }
                        else
                        {
                            item.Remove();
                        }
                    }
                }
            }

            private void UpdateAttachedWeapon(AutoTurret turret)
            {
                if (!turret.IsKilled())
                {
                    turret.UpdateAttachedWeapon();
                }
            }

            private void InitiateStartup(AutoTurret turret)
            {
                if (!Options.AutoTurret.RequiresPower && !turret.IsKilled())
                {
                    turret.InitiateStartup();
                }
            }

            private void Authorize(BasePlayer player)
            {
                foreach (var turret in turrets)
                {
                    if (!turret.IsKilled())
                    {
                        turret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID
                        {
                            ShouldPool = false,
                            userid = player.userID,
                            username = player.displayName
                        });
                    }
                }
                if (privSpawned && !priv.IsKilled())
                {
                    priv.authorizedPlayers.Add(new()
                    {
                        ShouldPool = false,
                        userid = player.userID,
                        username = player.displayName,
                    });
                }
            }

            private bool CanBypassAuthorized(ulong userid) => userid.BelongsToGroup("admin") || userid.HasPermission("raidablebases.canbypass");

            private void SetupGunTrap(GunTrap gt)
            {
                if (config.Weapons.Ammo.GunTrap > 0)
                {
                    FillAmmoGunTrap(gt);
                }

                if (config.Weapons.InfiniteAmmo.GunTrap)
                {
                    gt.inventory.onPreItemRemove += new Action<Item>(OnWeaponItemPreRemove);
                }

                triggers[gt.trigger] = gt;
            }

            private void SetupFogMachine(FogMachine fm)
            {
                if (config.Weapons.Ammo.FogMachine > 0)
                {
                    FillAmmoFogMachine(fm);
                }

                if (config.Weapons.InfiniteAmmo.FogMachine)
                {
                    fm.fuelPerSec = 0f;
                }

                if (config.Weapons.FogMotion)
                {
                    fm.SetFlag(BaseEntity.Flags.Reserved9, true, false, true);
                }

                if (!config.Weapons.FogRequiresPower)
                {
                    fm.CancelInvoke(fm.CheckTrigger);
                    fm.SetFlag(BaseEntity.Flags.Reserved5, b: true);
                    fm.SetFlag(BaseEntity.Flags.Reserved6, b: true);
                    fm.SetFlag(BaseEntity.Flags.Reserved10, b: true);
                    fm.SetFlag(BaseEntity.Flags.On, true, false, true);
                }
            }

            private void SetupFlameTurret(FlameTurret ft)
            {
                triggers[ft.trigger] = ft;
                ft.InitializeHealth(Options.FlameTurretHealth, Options.FlameTurretHealth);

                if (config.Weapons.Ammo.FlameTurret > 0)
                {
                    FillAmmoFlameTurret(ft);
                }

                if (config.Weapons.InfiniteAmmo.FlameTurret)
                {
                    ft.fuelPerSec = 0f;
                }
            }

            private void SetupSamSite(SamSite ss)
            {
                samsites.Add(ss);

                ss.vehicleScanRadius = ss.missileScanRadius = Options.SamSite.Range;

                if (Options.SamSite.Repair > 0f)
                {
                    ss.staticRespawn = true;
                    ss.InvokeRepeating(ss.SelfHeal, Options.SamSite.Repair * 60f, Options.SamSite.Repair * 60f);
                }
                else
                {
                    ss.SetFlag(BaseEntity.Flags.Reserved1, false);
                    ss.CancelInvoke(ss.SelfHeal);
                    ss.staticRespawn = false;
                }

                if (!Options.SamSite.RequiresPower)
                {
                    SetupIO(ss as IOEntity);
                }

                if (config.Weapons.Ammo.SamSite > 0)
                {
                    FillAmmoSamSite(ss);
                }

                if (config.Weapons.InfiniteAmmo.SamSite)
                {
                    ss.inventory.onPreItemRemove += new Action<Item>(OnWeaponItemPreRemove);
                }

                ss.startHealth = UnityEngine.Random.Range(Options.SamSite.Min, Options.SamSite.Max);
                ss.InitializeHealth(ss.startHealth, ss.startHealth);
            }

            private bool ChangeTier(Door door)
            {
                uint prefabID = door.ShortPrefabName switch
                {
                    "door.hinged.toptier" => Options.Doors.Metal ? 202293038u : Options.Doors.Wooden ? 1343928398u : 0u,
                    "door.hinged.metal" or "door.hinged.industrial.a" or "door.hinged.industrial.d" => Options.Doors.HQM ? 170207918u : Options.Doors.Wooden ? 1343928398u : 0u,
                    "door.hinged.wood" => Options.Doors.HQM ? 170207918u : Options.Doors.Metal ? 202293038u : 0u,
                    "door.double.hinged.toptier" => Options.Doors.Metal ? 1418678061u : Options.Doors.Wooden ? 43442943u : 0u,
                    "wall.frame.garagedoor" when !Options.Doors.GarageDoor => 0u,
                    "wall.frame.garagedoor" => Options.Doors.HQM ? 201071098u : Options.Doors.Wooden ? 43442943u : 0u,
                    "door.double.hinged.metal" => Options.Doors.HQM ? 201071098u : Options.Doors.Wooden ? 43442943u : 0u,
                    "door.double.hinged.wood" => Options.Doors.HQM ? 201071098u : Options.Doors.Metal ? 1418678061u : 0u,
                    _ => 0u,
                };

                return prefabID != 0u && StringPool.toString.TryGetValue(prefabID, out var prefab) && SetDoorType(door, prefab);
            }

            private bool SetDoorType(Door door, string prefab)
            {
                var other = GameManager.server.CreateEntity(prefab, door.transform.position, door.transform.rotation) as Door;

                door.SafelyKill();
                other.Spawn();

                if (CanSetupEntity(other))
                {
                    SetupEntity(other);
                    SetupDoor(other, true);
                }

                return true;
            }

            private void SetupDoor(Door door)
            {
                if (door.canTakeLock && !door.isSecurityDoor)
                {
                    doors.Add(door);
                }
            }

            private void SetupDoor(Door door, bool changed)
            {
                CreateLock(door, Options.KeyLockDoors, Options.CodeLockDoors);

                if (!changed && Options.Doors.Any() && ChangeTier(door))
                {
                    return;
                }

                SetupSkin(door);

                if (Options.CloseOpenDoors)
                {
                    door.SetOpen(false, true);
                }
            }

            private void SetupDoors()
            {
                doors.RemoveAll(IsKilled);

                foreach (var door in doors)
                {
                    SetupDoor(door, false);
                }
            }

            private void SetupDoorControllers()
            {
                doorControllers.RemoveAll(IsKilled);

                foreach (var cdm in doorControllers)
                {
                    SetupIO(cdm);

                    if (cdm.IsPaired())
                    {
                        doors.Remove(cdm.targetDoor);
                        continue;
                    }

                    Door door;

                    try { door = cdm.FindDoor(true); } catch { continue; }

                    if (door.IsReallyValid())
                    {
                        cdm.SetTargetDoor(door);
                        doors.Remove(door);

                        if (door.isSecurityDoor || !door.canTakeLock)
                        {
                            continue;
                        }

                        CreateLock(door, Options.KeyLockDoors, Options.CodeLockDoors);
                    }
                }

                doorControllers.Clear();
            }

            private void CreateLock(BaseEntity entity, bool createKeyLock, bool createCodeLock)
            {
                if (Type == RaidableType.None || !createKeyLock && !createCodeLock || entity.IsKilled())
                {
                    return;
                }

                var slot = entity.GetSlot(BaseEntity.Slot.Lock);

                if (slot.IsNull())
                {
                    if (createKeyLock)
                    {
                        CreateKeyLock(entity);
                    }
                    else if (createCodeLock)
                    {
                        CreateCodeLock(entity);
                    }
                    return;
                }

                if (createKeyLock)
                {
                    if (slot.TryGetComponent<CodeLock>(out var codeLock))
                    {
                        codeLock.SetParent(null);
                        codeLock.SafelyKill();
                    }

                    if (!slot.TryGetComponent<KeyLock>(out var keyLock))
                    {
                        CreateKeyLock(entity);
                    }
                    else SetupLock(keyLock);
                }
                else if (createCodeLock)
                {
                    if (slot.TryGetComponent<KeyLock>(out var keyLock))
                    {
                        keyLock.SetParent(null);
                        keyLock.SafelyKill();
                    }

                    if (!slot.TryGetComponent<CodeLock>(out var codeLock))
                    {
                        CreateCodeLock(entity);
                    }
                    else SetupLock(codeLock, true);
                }
            }

            private void CreateKeyLock(BaseEntity entity)
            {
                if (GameManager.server.CreateEntity(StringPool.Get(2106860026)) is KeyLock keyLock)
                {
                    keyLock.gameObject.Identity();
                    keyLock.SetParent(entity, entity.GetSlotAnchorName(BaseEntity.Slot.Lock));
                    keyLock.Spawn();
                    entity.SetSlot(BaseEntity.Slot.Lock, keyLock);
                    SetupLock(keyLock, true);
                }
            }

            private void CreateCodeLock(BaseEntity entity)
            {
                if (GameManager.server.CreateEntity(StringPool.Get(3518824735)) is CodeLock codeLock)
                {
                    codeLock.gameObject.Identity();
                    codeLock.SetParent(entity, entity.GetSlotAnchorName(BaseEntity.Slot.Lock));
                    codeLock.Spawn();
                    entity.SetSlot(BaseEntity.Slot.Lock, codeLock);
                    SetupLock(codeLock, true);
                }
            }

            private void SetupBuildingPriviledge(BuildingPrivlidge priv)
            {
                if (Type != RaidableType.None)
                {
                    priv.authorizedPlayers.Clear();
                    priv.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                }

                CreateLock(priv, Options.KeyLockPrivilege, Options.CodeLockPrivilege);

                if (this.priv.IsKilled())
                {
                    this.priv = priv;
                    privSpawned = true;
                }
                else if (priv.Distance(Location) < this.priv.Distance(Location))
                {
                    this.priv = priv;
                    privSpawned = true;
                }

                if (privSpawned && !privHadLoot)
                {
                    privHadLoot = priv != null && priv.inventory != null && !priv.inventory.IsEmpty();
                }
            }

            private void SetupLocker(Locker locker)
            {
                if (config.Settings.Management.Lockers)
                {
                    lockers.Add(locker);
                }
            }

            private void SetupRugs()
            {
                _rugs.RemoveAll(IsKilled);
                _decorDeployables.RemoveAll(IsKilled);

                foreach (var deployable in _decorDeployables)
                {
                    _rugs.RemoveAll(rug => rug != deployable && deployable.transform.position.y >= rug.transform.position.y && InRange(rug.transform.position, deployable.transform.position, 1f));
                }
            }

            private void SetupSleepingBag(SleepingBag bag)
            {
                if (Options.NPC.Inside.SpawnOnBeds)
                {
                    _beds.Add(bag);
                }

                if (Options.NPC.Inside.BedHealthMultiplier != 1f)
                {
                    bag.health *= Options.NPC.Inside.BedHealthMultiplier;
                }

                if (Type != RaidableType.None)
                {
                    bag.deployerUserID = 0uL;
                }
            }

            private void SetupMountable(BaseMountable mountable)
            {
                if (mountable.IsValid())
                {
                    Instance.MountEntities[mountable.net.ID] = new()
                    {
                        position = Location,
                        radius = ProtectionRadius,
                        m = mountable
                    };
                }
            }

            private void SetupCollectible(CollectibleEntity ce)
            {
                if (IsPickupBlacklisted(ce.ShortPrefabName))
                {
                    ce.itemList = null;
                }
            }

            private void SetupSpookySpeaker(SpookySpeaker ss)
            {
                if (!config.Weapons.SpookySpeakersRequiresPower)
                {
                    ss.SetFlag(BaseEntity.Flags.On, b: true);
                    ss.InvokeRandomized(ss.SendPlaySound, ss.soundSpacing, ss.soundSpacing, ss.soundSpacingRand);
                }
            }

            private void SetupDecayEntity(DecayEntity e)
            {
                e.decay = null;
                e.upkeepTimer = float.MinValue;

                if (Options.NPC.Inside.SpawnOnRugs && e.ShortPrefabName.StartsWith("rug.") && Mathf.Approximately(e.transform.up.y, 1f))
                {
                    _rugs.RemoveAll(IsKilled);
                    _rugs.Add(e);

                    if (Options.NPC.Inside.SpawnOnRugsSkin != 1 && Options.NPC.Inside.SpawnOnRugsSkin >= 0)
                    {
                        _rugs.RemoveAll(rug => rug.skinID != Options.NPC.Inside.SpawnOnRugsSkin);
                    }

                    if (Options.NPC.Inside.RugHealthMultiplier != 1f && _rugs.Contains(e))
                    {
                        e.health *= Options.NPC.Inside.RugHealthMultiplier;
                    }
                }

                if (e is BaseTrap && !NearFoundation(e.transform.position, 1.75f))
                {
                    e.transform.position = e.transform.position.WithY(SpawnsController.GetSpawnHeight(e.transform.position, false) + 0.02f);
                }

                if (e is Barricade && !Physics.Raycast(e.transform.position + new Vector3(0f, 0.15f, 0f), Vector3.down, 50f, Layers.Mask.Construction))
                {
                    e.transform.position = e.transform.position.WithY(SpawnsController.GetSpawnHeight(e.transform.position, false));
                }
            }

            private void SetupBoxSkin(StorageContainer container)
            {
                if (!IsBox(container, false) || config.Skins.Boxes.IgnoreSkinned && container.skinID != 0uL)
                {
                    return;
                }

                if (!Instance.DeployableItems.TryGetValue(container.gameObject.name, out var def))
                {
                    return;
                }

                if (config.Skins.Boxes.Unique && _prefabToSkin.TryGetValue(container.prefabID, out var skin))
                {
                    container.skinID = skin;
                    return;
                }

                var si = GetItemSkins(def, config.Skins.Boxes.ApprovedOnly);

                if (config.Skins.Boxes.Skins.Count > 0 && SetItemSkin(config.Skins.Boxes.Skins.ToList(), si, container, config.Skins.Boxes.Unique))
                {
                    return;
                }

                var skins = GetItemSkins(si, config.Skins.Boxes.Random, config.Skins.Boxes.Workshop, config.Skins.Boxes.ImportedWorkshop);

                if (!_prefabToSkin.ContainsKey(container.prefabID))
                {
                    _prefabToSkin[container.prefabID] = skins.Count == 0 ? container.skinID : skins.GetRandom();
                }

                if (config.Skins.Boxes.Unique)
                {
                    container.skinID = _prefabToSkin[container.prefabID];
                }
                else if (skins.Count > 0)
                {
                    container.skinID = skins.GetRandom();
                }
            }

            private void SetupSkin(BaseEntity entity)
            {
                if (IsUnloading || IsBox(entity, false) || config.Skins.Deployables.IgnoreSkinned && entity.skinID != 0uL)
                {
                    return;
                }

                if (config.Skins.Deployables.Unique && _prefabToSkin.TryGetValue(entity.prefabID, out var skin))
                {
                    entity.skinID = skin;
                    return;
                }

                if (!Instance.DeployableItems.TryGetValue(entity.gameObject.name, out var def) || def == null)
                {
                    return;
                }

                var si = GetItemSkins(def, config.Skins.Deployables.ApprovedOnly);

                if (config.Skins.Deployables.Doors.Count > 0 && entity is Door && SetItemSkin(config.Skins.Deployables.Doors.ToList(), si, entity, config.Skins.Deployables.Unique))
                {
                    return;
                }

                if (!config.Skins.Deployables.SkinEverything && !config.Skins.Deployables.PartialNames.Exists(entity.ShortPrefabName.Contains))
                {
                    return;
                }

                var skins = GetItemSkins(si, config.Skins.Deployables.Random, config.Skins.Deployables.Workshop, config.Skins.Deployables.ImportedWorkshop);

                if (!_prefabToSkin.ContainsKey(entity.prefabID))
                {
                    _prefabToSkin[entity.prefabID] = skins.Count == 0 ? entity.skinID : skins.GetRandom();
                }

                if (config.Skins.Deployables.Unique && entity is Door)
                {
                    entity.skinID = _prefabToSkin[entity.prefabID];
                    entity.SendNetworkUpdate();
                }
                else if (skins.Count > 0)
                {
                    entity.skinID = skins.GetRandom();
                    entity.SendNetworkUpdate();
                }
            }

            private void Subscribe()
            {
                if (IsUnloading)
                {
                    return;
                }

                if (Instance.BaseRepair.CanCall())
                {
                    Subscribe(nameof(OnBaseRepair));
                }

                if (config.Settings.Management.Lockout.AllyExploit)
                {
                    if (config.Settings.Management.Lockout.BlockClans) Subscribe(nameof(OnClanMemberJoined));
                    if (config.Settings.Management.Lockout.BlockTeams) Subscribe(nameof(OnTeamAcceptInvite));
                }

                if (Options.EnforceDurability)
                {
                    Subscribe(nameof(OnLoseCondition));
                    Subscribe(nameof(OnNeverWear));
                }

                if ((Options.NPC.SpawnAmountMurderers > 0 || Options.NPC.SpawnAmountScientists > 0) && Options.NPC.Enabled)
                {
                    npcMaxAmountMurderers = Options.NPC.SpawnRandomAmountMurderers && Options.NPC.SpawnAmountMurderers > 1 ? UnityEngine.Random.Range(Options.NPC.SpawnMinAmountMurderers, Options.NPC.SpawnAmountMurderers + 1) : Options.NPC.SpawnAmountMurderers;
                    npcMaxAmountScientists = Options.NPC.SpawnRandomAmountScientists && Options.NPC.SpawnAmountScientists > 1 ? UnityEngine.Random.Range(Options.NPC.SpawnMinAmountScientists, Options.NPC.SpawnAmountScientists + 1) : Options.NPC.SpawnAmountScientists;

                    if (Options.NPC.Inside.Max > 0)
                    {
                        npcMaxAmountInside = UnityEngine.Random.Range(Options.NPC.Inside.Min, Options.NPC.Inside.Max + 1);
                        npcMaxAmountInside = Mathf.Clamp(npcMaxAmountInside, -1, npcMaxAmountScientists);
                    }

                    if (npcMaxAmountMurderers > 0 || npcMaxAmountScientists > 0)
                    {
                        if (config.Settings.Management.BlockCustomLootNPC)
                        {
                            Subscribe(nameof(OnCustomLootNPC));
                        }

                        if (config.Settings.Management.BlockNpcKits)
                        {
                            Subscribe(nameof(OnNpcKits));
                        }

                        if (Options.NPC.PlayCatch)
                        {
                            Subscribe(nameof(OnExplosiveFuseSet));
                        }

                        Subscribe(nameof(OnNpcDuck));
                        Subscribe(nameof(OnNpcDestinationSet));
                    }
                }

                if (config.Settings.Management.PreventFallDamage)
                {
                    Subscribe(nameof(OnPlayerLand));
                }

                if (!config.Settings.Management.AllowTeleport)
                {
                    Subscribe(nameof(CanTeleport));
                    Subscribe(nameof(canTeleport));
                }

                if (config.Settings.Management.BlockRestorePVP && AllowPVP || config.Settings.Management.BlockRestorePVE && !AllowPVP)
                {
                    Subscribe(nameof(OnRestoreUponDeath));
                }

                if (config.Settings.Management.NoLifeSupport)
                {
                    Subscribe(nameof(OnLifeSupportSavingLife));
                }

                if (config.Settings.Management.NoDoubleJump)
                {
                    Subscribe(nameof(CanDoubleJump));
                }

                if (Options.DropTimeAfterLooting > 0 || config.UI.Status.ShowLootLeft)
                {
                    Subscribe(nameof(OnLootEntityEnd));
                }

                if (!config.Settings.Management.BackpacksOpenPVP || !config.Settings.Management.BackpacksOpenPVE)
                {
                    Subscribe(nameof(CanOpenBackpack));
                }

                if (config.Settings.Management.PreventFireFromSpreading)
                {
                    Subscribe(nameof(OnFireBallSpread));
                }

                if (privSpawned)
                {
                    Subscribe(nameof(OnCupboardProtectionCalculated));
                }

                if (Options.BuildingRestrictions.Any() || !config.Settings.Management.AllowUpgrade)
                {
                    Subscribe(nameof(OnStructureUpgrade));
                }

                if (BlacklistedCommands.Exists(x => x.Equals("remove", StringComparison.OrdinalIgnoreCase)))
                {
                    Subscribe(nameof(canRemove));
                }

                if (Options.Invulnerable || Options.InvulnerableUntilCupboardIsDestroyed)
                {
                    Subscribe(nameof(OnEntityGroundMissing));
                }

                if (Options.RequiresCupboardAccess)
                {
                    Subscribe(nameof(OnCupboardAuthorize));
                }

                Subscribe(nameof(OnFireBallDamage));
                Subscribe(nameof(CanPickupEntity));
                Subscribe(nameof(OnPlayerDropActiveItem));
                Subscribe(nameof(OnPlayerDeath));
                Subscribe(nameof(OnEntityDeath));
                Subscribe(nameof(OnEntityKill));
                Subscribe(nameof(CanBGrade));
                Subscribe(nameof(CanBePenalized));
                Subscribe(nameof(CanLootEntity));
                Subscribe(nameof(OnEntityBuilt));
                Subscribe(nameof(STCanGainXP));
            }

            private void Subscribe(string hook) => Instance.Subscribe(hook);

            private void MakeAnnouncements()
            {
                if (Type == RaidableType.None)
                {
                    _allcontainers.RemoveWhere(x => x.IsKilled());

                    itemAmountSpawned = _allcontainers.ToList().Sum(x => x.inventory.itemList.Count);
                }

                Puts("{0} @ {1} : {2} items", BaseName, Instance.PositionToGrid(Location), itemAmountSpawned);

                if (Options.Silent || Options.Smart)
                {
                    return;
                }

                foreach (var target in BasePlayer.activePlayerList)
                {
                    float distance = Mathf.Floor(target.transform.position.Distance(Location));
                    string mode = mx($"Mode{Options.Mode}", target.UserIDString);
                    string flag = mx(GetAllowKey(), target.UserIDString).Replace("[", string.Empty).Replace("] ", string.Empty);
                    string posStr = FormatGridReference(target, Location);
                    string text = posStr != Location.ToString() ? mx("RaidOpenMessage", target.UserIDString, mode, posStr, distance, flag) : mx("RaidOpenNoMapMessage", target.UserIDString, mode, distance, flag);
                    if (Type == RaidableType.None) text = text.Replace(mode, NoMode);
                    string message = ownerId.IsSteamId() ? $"{text}[{mx("Owner", target.UserIDString)} {ownerName}]" : text;

                    if ((!IsPayLocked && config.EventMessages.OpenedPVE && !AllowPVP) || (!IsPayLocked && config.EventMessages.OpenedPVP && AllowPVP) || (IsPayLocked && config.EventMessages.OpenedAndPaid))
                    {
                        QueueNotification(target, message);
                    }
                    else if (distance <= config.GUIAnnouncement.Distance && (config.EventMessages.OpenedPVE && !AllowPVP || config.EventMessages.OpenedPVP && AllowPVP))
                    {
                        QueueNotification(target, message);
                    }
                }
            }

            public void ResetPublicOwner()
            {
                float remainingTime = ownerId.IsSteamId() ? PlayerActivityTimeLeft(ownerId) : 0f;
                if (!IsOpened || IsPayLocked || remainingTime > 0f)
                {
                    Invoke(ResetPublicOwner, (remainingTime > 0f && !float.IsPositiveInfinity(remainingTime)) ? remainingTime : config.Settings.Management.LockTime * 60f);
                    return;
                }

                if (config.Settings.Management.SetLockout)
                {
                    if (raiders.TryGetValue(ownerId, out var ri))
                    {
                        TrySetLockout(ri);
                    }
                }

                ResetPublicLock();
                CheckBackpacks(true);
            }

            private void ResetPayLock()
            {
                if (IsPlayerActive(ownerId) || IsOwnerConnected())
                {
                    InvokeResetPayLock();
                    return;
                }

                StartPurchaseCooldown();
                ResetPublicLock();
                CheckBackpacks(true);
                raiders.Values.ToList().ForEach(ri => ri.IsParticipant = false);
            }

            public void ResetPublicLock()
            {
                raiders.Remove(ownerId);
                IsEngaged = true;
                IsPayLocked = false;
                ownerId = 0uL;
                ownerName = string.Empty;
                UpdateMarker();
                CreateSpheres();
            }

            public void SpawnDrops(ItemContainer[] containers, List<LootItem> lootList)
            {
                Invoke(() =>
                {
                    if (containers == null || containers.Length == 0)
                    {
                        return;
                    }

                    lootList.ForEach(ti =>
                    {
                        if (!string.IsNullOrEmpty(ti.shortname) && ti.HasProbability())
                        {
                            if (ti.definition == null)
                            {
                                Puts("Invalid shortname in profile for npc: {0}", ti.shortname);
                                return;
                            }

                            Item item = CreateItem(ti, ti.amountMin < ti.amount ? Core.Random.Range(ti.amountMin, ti.amount + 1) : ti.amount);

                            if (item == null || Array.Exists(containers, container => item.MoveToContainer(container)))
                            {
                                return;
                            }

                            item.Remove();
                        }
                    });
                }, 2f);
            }

            private bool SetupLoot()
            {
                _containers.RemoveWhere(IsKilled);

                int amount = Options.GetLootAmount(Type);

                if (Options.SkipTreasureLoot || amount <= 0)
                {
                    return true;
                }

                var containers = new List<StorageContainer>();

                if (!SetupLootContainers(containers))
                {
                    return false;
                }

                LootProfile loot = new()
                {
                    Unique = Instance.config.Loot,
                    BaseName = BaseName,
                    Amount = amount,
                    Instance = Instance,
                    Options = Options,
                    UserID = ownerId,
                    AllowPVP = AllowPVP
                };

                TakeLootFromLootTables(loot);

                if (loot.Tables.Count == 0)
                {
                    Puts(mx("NoConfiguredLoot"));
                    return true;
                }

                DivideLoot(loot.Tables, loot.Amount, containers);

                SetupSellOrders();

                numLootRequired = GetLootAmountRemaining();

                return true;
            }

            private bool SetupLootContainers(List<StorageContainer> containers)
            {
                if (_containers.Count == 0)
                {
                    Puts(mx(Entities.Exists() ? "NoContainersFound" : "NoEntitiesFound", null, BaseName, Instance.PositionToGrid(Location)));
                    return false;
                }

                TryInvokeMethod(CheckExpansionSettings);

                foreach (var container in _containers.ToList())
                {
                    if (!IsBox(container, true) || Options.IgnoreContainedLoot && !container.inventory.IsEmpty())
                    {
                        continue;
                    }

                    if (config.Settings.Management.ClippedBoxes && IsRockFaceUpwardsSecondary(container.transform.position + new Vector3(0f, container.bounds.extents.y, 0f)))
                    {
                        RemoveContainer(container);
                        continue;
                    }

                    if (Options.DivideLoot)
                    {
                        containers.Add(container);
                        continue;
                    }
                    else if (container.inventory.IsEmpty())
                    {
                        containers.Add(container);
                        break;
                    }
                }

                if (Options.IgnoreContainedLoot)
                {
                    lockers.RemoveAll(x => x.IsKilled() || x.inventory == null || !x.inventory.IsEmpty());
                }

                if (containers.Count == 0)
                {
                    Puts(mx("NoBoxesFound", null, BaseName, Instance.PositionToGrid(Location)));
                    return false;
                }

                return true;
            }

            public class LootProfile
            {
                public List<LootItem> Base = new();
                public List<LootItem> Difficulty = new();
                public List<LootItem> Default = new();
                public List<LootItem> Tables = new();
                public TreasureSettings Unique;
                public BuildingOptions Options;
                public RaidableBases Instance;
                public string BaseName;
                public bool AllowPVP;
                public ulong UserID;
                public int Amount;
                public int Count => Base.Count + Difficulty.Count + Default.Count;
            }

            private bool IsItemBlockedInto(LootItem lootItem, StorageContainer container)
            {
                return container.IsKilled() || container is BaseOven && !IsCookable(lootItem.definition) || container is Locker && !IsLockerItem(lootItem.definition);
            }

            private LootItem GetLootItem(List<LootItem> lootList)
            {
                Shuffle(lootList);

                foreach (LootItem lootItem in lootList)
                {
                    if (lootItem.hasPriority)
                    {
                        lootItem.hasPriority = false;

                        return lootItem;
                    }
                }

                LootItem randomLootItem = lootList.GetRandom();

                //if (randomLootItem.isSplit)
                //{
                //    lootList.ForEach(lootItem3 =>
                //    {
                //        if (lootItem3.shortname == randomLootItem.shortname)
                //        {
                //            lootItem3.hasPriority = true;
                //        }
                //    });
                //}

                return randomLootItem;
            }

            private void DivideLoot(List<LootItem> lootList, int amount, List<StorageContainer> containers)
            {
                while (lootList.Count > 0 && containers.Count > 0 && itemAmountSpawned < amount)
                {
                    LootItem lootItem = GetLootItem(lootList);

                    lootList.Remove(lootItem);

                    Item item = CreateItem(lootItem, lootItem.amount);

                    if (item == null)
                    {
                        continue;
                    }

                    if (MoveToCupboard(item) || MoveToBBQ(item) || MoveToOven(item) || MoveFood(item) || MoveToLocker(item))
                    {
                        itemAmountSpawned++;
                        continue;
                    }

                    bool itemMovedToContainer = false;

                    foreach (var container in containers)
                    {
                        if (IsItemBlockedInto(lootItem, container))
                        {
                            continue;
                        }
                        if (item.MoveToContainer(container.inventory, -1, false))
                        {
                            containers.Remove(container);

                            if (!container.inventory.IsFull())
                            {
                                containers.Add(container);
                            }

                            itemMovedToContainer = true;
                            itemAmountSpawned++;
                            break;
                        }
                    }

                    if (!itemMovedToContainer)
                    {
                        item.Remove();
                    }
                }

                if (itemAmountSpawned == 0)
                {
                    Puts(mx("NoLootSpawned"));
                }
            }

            private static void TakeLootFromBaseLoot(LootProfile loot)
            {
                foreach (var (key, profile) in loot.Instance.Buildings.Profiles)
                {
                    if (key == loot.BaseName || profile.Options.AdditionalBases.ContainsKey(loot.BaseName))
                    {
                        TakeLootFrom(profile.BaseLootList, loot.Base, loot.Options, loot.UserID, loot.AllowPVP);
                        break;
                    }
                }

                if (loot.Options.AlwaysSpawnBaseLoot)
                {
                    foreach (var ti in loot.Base.ToList())
                    {
                        if (ti.HasProbability())
                        {
                            if (!loot.Options.AllowDuplicates)
                            {
                                loot.Base.Remove(ti);
                            }

                            ti.hasPriority = true;

                            AddToLoot(loot, ti);
                        }

                        if (loot.Options.EnforceProbability && ti.probability < 1f)
                        {
                            loot.Base.Remove(ti);
                        }
                    }

                    if (loot.Unique.Base)
                    {
                        loot.Base.Clear();
                    }
                }
            }

            private static void TakeLootFromDifficultyLoot(LootProfile loot)
            {
                if (loot.Instance.Buildings.DifficultyLootLists.TryGetValue(loot.Options.Mode, out var lootList))
                {
                    TakeLootFrom(lootList, loot.Difficulty, loot.Options, loot.UserID, loot.AllowPVP);
                }
            }

            private static void TakeLootFromWeekdayLoot(LootProfile loot)
            {
                if (loot.Instance.WeekdayLoot.Count > 0)
                {
                    TakeLootFrom(loot.Instance.WeekdayLoot, loot.Default, loot.Options, loot.UserID, loot.AllowPVP);
                }
            }

            private static void TakeLootFromDefaultLoot(LootProfile loot)
            {
                if (loot.Count < loot.Amount)
                {
                    TakeLootFrom(loot.Instance.TreasureLoot, loot.Default, loot.Options, loot.UserID, loot.AllowPVP);
                }
            }

            private static void TakeLootFrom(List<LootItem> lootList, List<LootItem> to, BuildingOptions Options, ulong UserID, bool AllowPVP)
            {
                if (lootList.Count == 0)
                {
                    return;
                }

                foreach (var ti in lootList.Where(ti => ti != null && ti.amount > 0 && ti.probability > 0f))
                {
                    to.Add(ti.Clone());
                }

                if (Options.Multiplier != 1f || Options.MultiplierPVE != 1f || Options.MultiplierPVP != 1f)
                {
                    float m = !AllowPVP && UserID.HasPermission("raidablebases.buyable.vip.pve") ? Options.MultiplierPVE :
                              AllowPVP && UserID.HasPermission("raidablebases.buyable.vip.pvp") ? Options.MultiplierPVP :
                              Options.Multiplier;

                    foreach (var ti in to)
                    {
                        if (ti.amount > 1)
                        {
                            ti.amount = Mathf.CeilToInt(ti.amount * m);
                            ti.amountMin = Mathf.CeilToInt(ti.amountMin * m);
                        }
                    }
                }
            }

            private static void TakeLootFromLootTables(LootProfile loot)
            {
                TakeLootFromBaseLoot(loot);
                TakeLootFromDifficultyLoot(loot);
                TakeLootFromWeekdayLoot(loot);
                TakeLootFromDefaultLoot(loot);

                int iterations = 0;

                List<LootItem> source = new();

                Action<LootItem> remove = (LootItem ti) =>
                {
                    loot.Base.Remove(ti);
                    loot.Difficulty.Remove(ti);
                    loot.Default.Remove(ti);
                };

                Action refill = () =>
                {
                    source.AddRange(loot.Base);
                    source.AddRange(loot.Difficulty);
                    source.AddRange(loot.Default);
                };

                refill();

                if (loot.Unique.Base)
                {
                    loot.Base.Clear();
                }

                if (loot.Unique.Difficulty)
                {
                    loot.Difficulty.Clear();
                }

                if (loot.Unique.Default)
                {
                    loot.Default.Clear();
                }

                while (loot.Tables.Count < loot.Amount && source.Count > 0)
                {
                    LootItem ti = source.GetRandom();

                    source.Remove(ti);

                    if (ti.HasProbability())
                    {
                        if (!loot.Options.AllowDuplicates)
                        {
                            remove(ti);
                        }

                        AddToLoot(loot, ti);
                    }

                    if (loot.Options.EnforceProbability && ti.probability < 1f)
                    {
                        remove(ti);
                    }

                    if (source.Count == 0 && ++iterations < loot.Tables.Count)
                    {
                        refill();
                    }
                }
            }

            private static bool AddToLoot(LootProfile loot, LootItem lootItem)
            {
                if (lootItem.definition == null)
                {
                    Puts("Invalid shortname in loot table: {0} for {1}", lootItem.shortname, loot.BaseName);
                    return false;
                }

                LootItem ti = lootItem.Clone();

                int amount = ti.amountMin < ti.amount ? Core.Random.Range(ti.amountMin, ti.amount + 1) : ti.amount;

                if (amount <= 0)
                {
                    return false;
                }

                int[] stacks = loot.Unique.Stacks || ti.stacksize != -1 ? GetStacks(amount, ti.stacksize <= 0 ? ti.definition.stackable : ti.stacksize) : new int[1] { amount };

                if (loot.Options.Dynamic && stacks.Length > 1)
                {
                    loot.Amount += stacks.Length - 1;
                }

                foreach (int stack in stacks)
                {
                    loot.Tables.Add(new(ti.shortname, stack, stack, ti.skin, ti.isBlueprint, ti.probability, ti.stacksize, ti.name, ti.text, ti.hasPriority) { isSplit = stacks.Length > 1 });
                }

                return true;
            }

            private static int[] GetStacks(int amount, int maxStack)
            {
                int size = (amount + maxStack - 1) / maxStack;
                int[] stacks = new int[size];
                for (int i = 0; i < size; i++)
                {
                    stacks[i] = Math.Min(amount, maxStack);
                    amount -= stacks[i];
                }
                return stacks;
            }

            public static void GenerateLoot(RaidableBases Instance, IPlayer user, RaidableMode mode, string[] args)
            {
                BaseProfile profile = args.Select(arg => Instance.Get(arg, out var val) ? val.Item2 : null).FirstOrDefault(x => x != null) ?? Instance.Buildings.Profiles.FirstOrDefault(v => v.Value.Options.Mode == mode).Value;

                if (profile == null)
                {
                    Instance.Message(user, "Difficulty Not Available", mode);
                    return;
                }

                int amount = args.Where(arg => !Instance.IsRaidableMode(arg) && arg.IsNumeric()).Select(int.Parse).FirstOrDefault();

                LootProfile loot = new()
                {
                    Amount = amount != 0 ? amount : profile.Options.GetLootAmount(RaidableType.None),
                    BaseName = profile.Options.AdditionalBases.ToList().GetRandom().Key,
                    AllowPVP = profile.Options.AllowPVP,
                    Unique = Instance.config.Loot,
                    Options = profile.Options,
                    Instance = Instance,
                };

                TakeLootFromLootTables(loot);

                string text = string.Format("{0} ({1} selected, {2} expected): {3}", mode, loot.Tables.Count, loot.Amount, string.Join(", ", loot.Tables.Select(ti => $"{ti.shortname} ({ti.amount})")));

                Instance.LogToFile("items", text, Instance, false, true);

                Puts(text);
            }

            private Item CreateItem(LootItem ti, int amount)
            {
                if (amount <= 0 || ti.definition == null)
                {
                    return null;
                }

                Item item;
                if (ti.isBlueprint && ti.definition.Blueprint != null)
                {
                    item = ItemManager.Create(Workbench.GetBlueprintTemplate());
                    item.blueprintTarget = ti.definition.itemid;
                    item.amount = amount;
                }
                else
                {
                    item = ItemManager.Create(ti.definition, amount, 0uL);
                    item.skin = GetItemSkin(ti.definition, SkinType.Loot, ti.skin, config.Skins.Loot.Stackable, config.Skins.Loot.NonStackable, config.Skins.Loot.Random, config.Skins.Loot.Workshop, config.Skins.Loot.ImportedWorkshop, config.Skins.Loot.ApprovedOnly, item.MaxStackable());
                }

                if (!string.IsNullOrEmpty(ti.name))
                {
                    item.name = ti.name;
                }

                if (!string.IsNullOrEmpty(ti.text) && item.info.category != ItemCategory.Resources)
                {
                    item.text = ti.text;
                }

                var e = item.GetHeldEntity();

                if (e.IsReallyValid())
                {
                    e.skinID = item.skin;
                    e.SendNetworkUpdate();
                }

                return item;
            }

            private void SetupSellOrders()
            {
                if (!config.Settings.Management.Inherit.Exists("vendingmachine".Contains))
                {
                    return;
                }

                vms.RemoveAll(IsKilled);

                foreach (var vm in vms)
                {
                    vm.InstallDefaultSellOrders();
                    vm.SetFlag(BaseEntity.Flags.Reserved4, config.Settings.Management.AllowBroadcasting, false, true);
                    foreach (Item item in vm.inventory.itemList)
                    {
                        if (vm.sellOrders.sellOrders.Count < 8)
                        {
                            ItemDefinition itemToSellDef = ItemManager.FindItemDefinition(item.info.itemid);
                            ItemDefinition currencyDef = ItemManager.FindItemDefinition(-932201673);

                            if (!(itemToSellDef == null) && !(currencyDef == null))
                            {
                                int itemToSellAmount = Mathf.Clamp(item.amount, 1, itemToSellDef.stackable);

                                vm.sellOrders.sellOrders.Add(new()
                                {
                                    ShouldPool = false,
                                    itemToSellID = item.info.itemid,
                                    itemToSellAmount = itemToSellAmount,
                                    currencyID = -932201673,
                                    currencyAmountPerItem = 999999,
                                    currencyIsBP = true,
                                    itemToSellIsBP = item.IsBlueprint()
                                });

                                vm.RefreshSellOrderStockLevel(itemToSellDef);
                            }
                        }
                    }

                    vm.FullUpdate();
                }
            }

            private bool MoveFood(Item item)
            {
                if (!config.Settings.Management.Food || fridges.Count == 0 || item.info.category != ItemCategory.Food || config.Settings.Management.Foods.Exists(item.info.shortname.Contains))
                {
                    return false;
                }

                fridges.RemoveAll(IsKilled);

                if (fridges.Count > 1)
                {
                    Shuffle(fridges);
                }

                return fridges.Exists(x => item.MoveToContainer(x.inventory, -1, true));
            }

            private bool MoveToBBQ(Item item)
            {
                if (!config.Settings.Management.Food || ovens.Count == 0 || item.info.category != ItemCategory.Food || !IsCookable(item.info) || config.Settings.Management.Foods.Exists(item.info.shortname.Contains))
                {
                    return false;
                }

                ovens.RemoveAll(IsKilled);

                if (ovens.Count > 1)
                {
                    Shuffle(ovens);
                }

                return ovens.Exists(oven => !oven.IsKilled() && oven.ShortPrefabName.Contains("bbq") && item.MoveToContainer(oven.inventory, -1, true));
            }

            private bool MoveToCupboard(Item item)
            {
                if (!config.Settings.Management.Cupboard || !privSpawned || item.info.category != ItemCategory.Resources || config.Loot.ExcludeFromCupboard.Contains(item.info.shortname))
                {
                    return false;
                }

                if (config.Settings.Management.Cook && ovens.Count > 0 && item.info.shortname.Equals("crude.oil") && SplitIntoFurnaces(ovens, item))
                {
                    return true;
                }

                if (config.Settings.Management.Cook && item.info.shortname.EndsWith(".ore") && MoveToOven(item))
                {
                    return true;
                }

                if (!priv.IsKilled() && item.MoveToContainer(priv.inventory, -1, true))
                {
                    privHadLoot = true;
                    return true;
                }

                return false;
            }

            private bool IsCookable(ItemDefinition def)
            {
                if (def.shortname.EndsWith(".cooked") || def.shortname.EndsWith(".burned") || def.shortname.EndsWith(".spoiled") || def.shortname == "lowgradefuel")
                {
                    return false;
                }

                return def.GetComponent<ItemModCookable>() || def.shortname == "wood" || def.shortname == "crude.oil";
            }

            private bool MoveToOven(Item item)
            {
                if (!config.Settings.Management.Cook || ovens.Count == 0 || !IsCookable(item.info))
                {
                    return false;
                }

                ovens.RemoveAll(IsKilled);

                if (ovens.Count > 1)
                {
                    Shuffle(ovens);
                }

                if ((item.info.shortname.EndsWith(".ore") || item.info.shortname.Equals("crude.oil")) && item.skin == 0 && SplitIntoFurnaces(ovens, item))
                {
                    return true;
                }

                foreach (var oven in ovens)
                {
                    if (oven.ShortPrefabName.Contains("bbq") ||
                        (item.info.shortname == "crude.oil" && !oven.IsMaterialInput(item)) ||
                        (item.info.shortname.EndsWith(".ore") && !oven.IsMaterialInput(item)) ||
                        (item.info.shortname == "lowgradefuel" && !oven.IsBurnableItem(item))) continue;

                    if (item.MoveToContainer(oven.inventory, -1, true))
                    {
                        if (!oven.IsOn() && oven.FindBurnable() != null)
                        {
                            oven.SetFlag(BaseEntity.Flags.On, true, false, true);
                        }

                        if (oven.IsOn() && !item.HasFlag(global::Item.Flag.OnFire))
                        {
                            item.SetFlag(global::Item.Flag.OnFire, true);
                            item.MarkDirty();
                        }

                        return true;
                    }
                }

                return false;
            }

            private bool SplitIntoFurnaces(List<BaseOven> ovens, Item item)
            {
                List<(BaseOven, int)> furnaces = new();
                foreach (var oven in ovens)
                {
                    int position = -1;

                    try { position = oven.GetIdealSlot(null, null, item); } catch { }

                    if (position != -1)
                    {
                        furnaces.Add(new(oven, position));
                    }
                }
                if (item.amount <= 0 || furnaces.Count == 0)
                {
                    return false;
                }
                int size = item.amount / furnaces.Count;
                foreach (var (furnace, position) in furnaces)
                {
                    if (size > 0 && size < item.amount && item.SplitItem(size) is Item split)
                    {
                        if (!split.MoveToContainer(furnace.inventory, position, true, true))
                        {
                            item.amount += split.amount;
                            item.MarkDirty();
                            split.Remove();
                            return false;
                        }
                    }
                    else if (!item.MoveToContainer(furnace.inventory, position, true, true))
                    {
                        return false;
                    }
                    if (furnace is ElectricOven eo && eo.spawnedIo.Get(true) is IOEntity io && !io.IsPowered())
                    {
                        io.Invoke(() =>
                        {
                            io.UpdateHasPower(25, 0);
                            eo.StartCooking();
                        }, 0.1f);
                    }
                    if (config.Weapons.Furnace > 0 && !(furnace is ElectricOven) && furnace.inventory.GetSlot(0) == null)
                    {
                        ItemManager.Create(furnace.fuelType, config.Weapons.Furnace).MoveToContainer(furnace.inventory, 0);

                        if (!furnace.IsInvoking(furnace.Cook))
                        {
                            furnace.Invoke(furnace.StartCooking, 0.2f);
                        }
                    }
                }
                return true;
            }

            private bool IsLockerItem(ItemDefinition def)
            {
                if (def.shortname.Contains("explosive") || def.shortname.Contains("rocket"))
                {
                    return false;
                }
                if (config.Settings.Management.Food && def.category == ItemCategory.Food && !config.Settings.Management.Foods.Exists(def.shortname.Contains))
                {
                    return fridges.Count == 0;
                }
                return def.category == ItemCategory.Attire || def.category == ItemCategory.Ammunition || def.category == ItemCategory.Medical || def.category == ItemCategory.Weapon;
            }

            private bool MoveToLocker(Item item)
            {
                if (!config.Settings.Management.Lockers || lockers.Count == 0 || !IsLockerItem(item.info))
                {
                    return false;
                }

                lockers.RemoveAll(IsKilled);

                if (config.Settings.Management.DivideLockerLoot)
                {
                    if (itemAmountSpawned % _containers.Count != 0)
                    {
                        return false;
                    }

                    lockers.Sort((a, b) => a.inventory.itemList.Count.CompareTo(b.inventory.itemList.Count));
                }

                return lockers.Exists(locker => MoveToLocker(item, locker));
            }

            private bool MoveToLocker(Item item, Locker locker)
            {
                try
                {
                    int position = locker.GetIdealSlot(null, null, item);

                    if (position != int.MinValue)
                    {
                        return item.MoveToContainer(locker.inventory, position, true);
                    }
                }
                catch { }

                return false;
            }

            private void CheckExpansionSettings()
            {
                if (!config.Settings.ExpansionMode || !Instance.DangerousTreasures.CanCall())
                {
                    return;
                }
                var containers = _containers.Where(x => x.ShortPrefabName == "box.wooden.large");
                if (containers.Count > 0)
                {
                    Instance.DangerousTreasures?.Call("API_SetContainer", containers.GetRandom(), Radius, !Options.NPC.Enabled || Options.NPC.UseExpansionNpcs);
                }
            }

            private bool ToggleNpcMinerHat(HumanoidNPC npc, bool state)
            {
                if (npc.IsKilled() || npc.inventory == null || npc.IsDead())
                {
                    return false;
                }

                var slot = npc.inventory.FindItemByItemName("hat.miner");

                if (slot == null)
                {
                    return false;
                }

                if (state && slot.contents != null)
                {
                    slot.contents.AddItem(ItemManager.FindItemDefinition("lowgradefuel"), 50);
                }

                slot.SwitchOnOff(state);
                npc.inventory.ServerUpdate(0f);
                return true;
            }

            public void ToggleLights()
            {
                bool state = config.Settings.Management.AlwaysLights || TOD_Sky.Instance?.IsDay == false;

                if (lights?.Count > 0)
                {
                    foreach (var io in lights.Where(io => !io.IsKilled()))
                    {
                        io.UpdateHasPower(state ? 25 : 0, 1);
                        io.SetFlag(BaseEntity.Flags.On, state);
                        io.SendNetworkUpdateImmediate();
                    }
                }

                if (ovens?.Count > 0)
                {
                    foreach (var oven in ovens.Where(oven => !oven.IsKilled()))
                    {
                        if (state && (oven.ShortPrefabName.Contains("furnace") && oven.inventory.IsEmpty())) continue;
                        oven.SetFlag(BaseEntity.Flags.On, state);
                    }
                }

                if (npcs?.Count > 0)
                {
                    foreach (var npc in npcs.Where(npc => !npc.IsKilled()))
                    {
                        ToggleNpcMinerHat(npc, state);
                    }
                }

                lightsOn = state;
            }

            public void Undo()
            {
                if (IsOpened)
                {
                    IsOpened = false;

                    if (DespawnMinutes > 0f)
                    {
                        UpdateDespawnDateTime(DespawnMinutes);

                        if (config.EventMessages.ShowWarning)
                        {
                            foreach (var target in BasePlayer.activePlayerList)
                            {
                                QueueNotification(target, "DestroyingBaseAt", FormatGridReference(target, Location), DespawnMinutes);
                            }
                        }
                    }
                    else
                    {
                        Despawn();
                    }
                }
            }

            public bool Any(ulong userid, bool checkAllies = true)
            {
                if (ownerId == userid) return true;
                if (!raiders.TryGetValue(userid, out var ri)) return false;
                return ri.IsParticipant || checkAllies && ri.IsAlly;
            }

            private static bool IsBlacklistedSkin(ItemDefinition def, int num)
            {
                var skinId = ItemDefinition.FindSkin(def.isRedirectOf?.itemid ?? def.itemid, num);
                var dirSkin = def.isRedirectOf == null ? def.skins.FirstOrDefault(x => (ulong)x.id == skinId) : def.isRedirectOf.skins.FirstOrDefault(x => (ulong)x.id == skinId);
                var itemSkin = (dirSkin.id == 0) ? null : (dirSkin.invItem as ItemSkin);

                return itemSkin?.Redirect != null || def.isRedirectOf != null;
            }

            public ulong GetItemSkin(ItemDefinition def, SkinType type, ulong defaultSkin, bool stackable, bool nonstackable, bool random, bool workshop, bool importedworkshop, bool approved, int stacksize)
            {
                ulong skin = defaultSkin;

                if (def.shortname != "explosive.satchel" && def.shortname != "grenade.f1" && skin == 0uL)
                {
                    if (stackable && stacksize > 1 && _shortnameToSkin.TryGetValue(def.shortname, out var dict) && dict.TryGetValue(type, out var skin2))
                    {
                        return skin2;
                    }

                    if (nonstackable && stacksize == 1 && _shortnameToSkin.TryGetValue(def.shortname, out var dict2) && dict2.TryGetValue(type, out var skin3))
                    {
                        return skin3;
                    }

                    var si = GetItemSkins(def, approved);
                    var skins = GetItemSkins(si, random, workshop, importedworkshop);

                    if (skins.Count != 0)
                    {
                        if (!_shortnameToSkin.TryGetValue(def.shortname, out dict))
                        {
                            _shortnameToSkin[def.shortname] = dict = new();
                        }
                        dict[type] = skin = skins.GetRandom();
                    }
                }

                return skin;
            }

            public SkinInfo GetItemSkins(ItemDefinition def, bool approvedOnly)
            {
                if (!Instance.Skins.TryGetValue(def.shortname, out var si))
                {
                    Instance.Skins[def.shortname] = si = new();

                    if (!def.skins.IsNullOrEmpty())
                    {
                        foreach (var skin in def.skins)
                        {
                            if (IsBlacklistedSkin(def, skin.id))
                            {
                                continue;
                            }
                            var id = Convert.ToUInt64(skin.id);
                            si.skins.Add(id);
                            si.allSkins.Add(id);
                        }
                    }

                    if (Instance.ImportedWorkshopSkins.SkinList.TryGetValue(def.shortname, out var value) && !value.IsNullOrEmpty())
                    {
                        foreach (var skin in value)
                        {
                            if (IsBlacklistedSkin(def, (int)skin))
                            {
                                continue;
                            }
                            if (approvedOnly && !IsApproved(def, skin))
                            {
                                continue;
                            }
                            si.importedWorkshopSkins.Add(skin);
                            si.allSkins.Add(skin);
                        }
                    }

                    if (!def.skins2.IsNullOrEmpty())
                    {
                        foreach (var skin in def.skins2)
                        {
                            if (skin == null || IsBlacklistedSkin(def, (int)skin.WorkshopId))
                            {
                                continue;
                            }
                            if (!si.workshopSkins.Contains(skin.WorkshopId))
                            {
                                si.workshopSkins.Add(skin.WorkshopId);
                                si.allSkins.Add(skin.WorkshopId);
                            }
                        }
                    }
                }

                return si;
            }

            private bool IsApproved(ItemDefinition def, ulong skin)
            {
                if (def.skins != null && def.skins.Exists(x => (ulong)x.id == skin)) return true;
                if (def.skins2 != null && def.skins2.Exists(x => x.WorkshopId == skin)) return true;
                return false;
            }

            private List<ulong> GetItemSkins(SkinInfo si, bool random, bool workshop, bool importedworkshop)
            {
                List<ulong> skins = new();

                if (random && si.skins.Count > 0)
                {
                    skins.Add(si.skins.GetRandom());
                }

                if (workshop && si.workshopSkins.Count > 0)
                {
                    skins.Add(si.workshopSkins.GetRandom());
                }

                if (importedworkshop && si.importedWorkshopSkins.Count > 0)
                {
                    skins.Add(si.importedWorkshopSkins.GetRandom());
                }

                return skins;
            }

            private bool SetItemSkin(List<ulong> skins, SkinInfo si, BaseEntity entity, bool unique)
            {
                Shuffle(skins);
                foreach (ulong skin in skins)
                {
                    if (!si.allSkins.Contains(skin))
                    {
                        continue;
                    }
                    if (unique)
                    {
                        _prefabToSkin[entity.prefabID] = skin;
                    }
                    entity.skinID = skin;
                    entity.SendNetworkUpdate();
                    return true;
                }
                return false;
            }

            public bool IsAlly(ulong playerId, ulong targetId, AlliedType type = AlliedType.All) => type switch
            {
                AlliedType.All or AlliedType.Team when RelationshipManager.ServerInstance.playerToTeam.TryGetValue(playerId, out var team) && team.members.Contains(targetId) => true,
                AlliedType.All or AlliedType.Clan when Convert.ToBoolean(Instance.Clans?.Call("IsMemberOrAlly", playerId.ToString(), targetId.ToString())) => true,
                AlliedType.All or AlliedType.Friend when Convert.ToBoolean(Instance.Friends?.Call("AreFriends", playerId.ToString(), targetId.ToString())) => true,
                _ => false
            };

            public bool IsAlly(BasePlayer player)
            {
                if (ownerId.IsSteamId() && player.userID != ownerId && !CanBypass(player))
                {
                    Raider ri = GetRaider(player);

                    return ri.IsAlly || (ri.IsAlly = IsAlly(player.userID, ownerId));
                }

                return true;
            }

            private BasePlayer GetAliveOrDeadOwnerPlayer(FlameThrower ft)
            {
                return ft.GetParentEntity() as BasePlayer;
            }

            public bool IsEcoTool(BasePlayer attacker, HitInfo hitInfo)
            {
                if (hitInfo.WeaponPrefab is TimedExplosive)
                {
                    return false;
                }
                HeldEntity heldEntity = attacker.GetHeldEntity();
                if (heldEntity is BaseProjectile)
                {
                    if (hitInfo.WeaponPrefab is BowWeapon || heldEntity is BowWeapon)
                    {
                        return Options.Eco.Bows;
                    }
                    return false;
                }
                if (hitInfo.WeaponPrefab is FlameThrower || hitInfo.damageTypes.Has(DamageType.Heat))
                {
                    return Options.Eco.FlameThrowers;
                }
                return hitInfo.damageTypes.IsMeleeType() || hitInfo.WeaponPrefab is BaseMelee;
            }

            public void StopUsingWeapon(BasePlayer player)
            {
                if (!player.svActiveItemID.IsValid)
                {
                    return;
                }

                if ((config.Settings.NoWizardryPVP && AllowPVP || config.Settings.NoWizardryPVE && !AllowPVP) && Instance.Wizardry.CanCall())
                {
                    StopUsingWeapon(player, "knife.bone");
                }

                if ((config.Settings.NoArcheryPVP && AllowPVP || config.Settings.NoArcheryPVE && !AllowPVP) && Instance.Archery.CanCall())
                {
                    StopUsingWeapon(player, "bow.compound", "bow.hunting", "crossbow");
                }
            }

            public void StopUsingWeapon(BasePlayer player, params string[] weapons)
            {
                Item item = player.GetActiveItem();

                if (item == null || !weapons.Contains(item.info.shortname))
                {
                    return;
                }

                if (!item.MoveToContainer(player.inventory.containerMain))
                {
                    item.DropAndTossUpwards(player.GetDropPosition() + player.transform.forward, 2f);
                    Message(player, "TooPowerfulDrop");
                }
                else Message(player, "TooPowerful");
            }

            public BackpackData AddBackpack(DroppedItemContainer backpack, BasePlayer player)
            {
                if (!backpacks.TryGetValue(backpack.net.ID, out var data))
                {
                    backpacks[backpack.net.ID] = data = new()
                    {
                        backpack = backpack,
                        _player = player,
                        userID = backpack.playerSteamID
                    };
                }

                return data;
            }

            private void RemoveParentFromEntitiesOnElevators()
            {
                foreach (var e in FindEntitiesOfType<BaseEntity>(Location, ProtectionRadius))
                {
                    if ((e is PlayerCorpse || e is DroppedItemContainer) && e.HasParent())
                    {
                        e.SetParent(null, false, true);
                    }
                }
            }

            public bool EjectBackpack(NetworkableId key, BackpackData data, bool bypass)
            {
                if (data.backpack.IsKilled())
                {
                    return true;
                }

                if (!bypass && (!ownerId.IsSteamId() || Any(data.userID) || data.player.IsReallyValid() && IsAlly(data.player)))
                {
                    return false;
                }

                var position = GetEjectLocation(data.backpack.transform.position, 5f, Location, ProtectionRadius);

                position.y = Mathf.Max(position.y, TerrainMeta.WaterMap.GetHeight(position));
                data.backpack.transform.position = position;
                data.backpack.TransformChanged();

                EjectionNotice(data.player, position);

                Interface.CallHook("OnRaidableBaseBackpackEjected", new object[] { data.player, data.userID, data.backpack, Location, AllowPVP, (int)Options.Mode, GetOwner(), GetRaiders(), BaseName });

                return true;
            }

            private void EjectionNotice(BasePlayer player, Vector3 position)
            {
                if (!player.IsOnline())
                {
                    return;
                }
                if (player.IsDead() || player.IsSleeping())
                {
                    player.Invoke(() => EjectionNotice(player, position), 1f);
                    return;
                }
                QueueNotification(player, "EjectedYourCorpse");
                if (config.Settings.Management.DrawTime > 0)
                {
                    AdminCommand(player, () => DrawText(player, config.Settings.Management.DrawTime, Color.red, position, mx("YourCorpse", player.UserIDString)));
                }
            }

            private void EjectSleepers()
            {
                if (!config.Settings.Management.EjectSleepers || Type == RaidableType.None)
                {
                    return;
                }

                foreach (var player in FindEntitiesOfType<BasePlayer>(Location, ProtectionRadius, Layers.Mask.Player_Server))
                {
                    if (player.IsSleeping() && !player.IsBuildingAuthed())
                    {
                        RemovePlayer(player, Location, ProtectionRadius, Type);
                    }
                }
            }

            public Vector3 GetEjectLocation(Vector3 a, float distance, Vector3 target, float radius)
            {
                var position = ((a.XZ3D() - target.XZ3D()).normalized * (radius + distance)) + target; // credits ZoneManager
                float y = TerrainMeta.HighestPoint.y + 250f;

                if (Physics.Raycast(position + new Vector3(0f, y, 0f), Vector3.down, out var hit, Mathf.Infinity, targetMask2, QueryTriggerInteraction.Ignore))
                {
                    position.y = hit.point.y + 0.75f;
                }
                else position.y = Mathf.Max(TerrainMeta.HeightMap.GetHeight(position), TerrainMeta.WaterMap.GetHeight(position)) + 0.75f;

                return position;
            }

            public bool RemovePlayer(BasePlayer player, Vector3 a, float radius, RaidableType type, bool isJetpack = false)
            {
                if (player.IsNull() || !player.IsHuman() || type == RaidableType.None && !player.IsSleeping())
                {
                    return false;
                }

                if (isJetpack || IsWearingJetpack(player))
                {
                    player.DismountObject();
                }
                else if (player.GetMounted() is BaseMountable m)
                {
                    return EjectMountable(m, GetMountedPlayers(m), a, radius);
                }

                var position = GetEjectLocation(player.transform.position, 10f, a, radius);

                if (player.IsFlying)
                {
                    position.y = player.transform.position.y;
                }

                player.Teleport(position);
                player.SendNetworkUpdateImmediate();

                return true;
            }

            public void Teleport(BasePlayer player)
            {
                if (!Options.CustomSpawns.GetBuyableTeleportPosition(Location, out var position))
                {
                    position = GetEjectLocation(player.transform.position, 10f, Location, ProtectionRadius);
                }
                TeleportExceptions.Add(player.userID);
                player.Teleport(position);
                player.SendNetworkUpdateImmediate();
            }

            public void DismountAllPlayers(BaseMountable m)
            {
                foreach (var target in GetMountedPlayers(m))
                {
                    if (target.IsNull()) continue;

                    m.DismountPlayer(target, true);

                    target.EnsureDismounted();
                }
            }

            public static List<BasePlayer> GetMountedPlayers(HotAirBalloon m)
            {
                return FindEntitiesOfType<BasePlayer>(m.CenterPoint(), 1.75f, Layers.Mask.Player_Server).Where(player => player.IsHuman() && player.GetParentEntity() == m);
            }

            public static List<BasePlayer> GetMountedPlayers(BaseMountable m)
            {
                BaseVehicle vehicle = m.HasParent() ? m.VehicleParent() : m as BaseVehicle;
                List<BasePlayer> players = new();

                if (!vehicle.IsRealNull())
                {
                    vehicle.GetMountedPlayers(players);
                }
                else if (m.GetMounted() is BasePlayer player)
                {
                    players.Add(player);
                }

                return players.Where(x => x.IsHuman());
            }

            private bool CanEject(List<BasePlayer> players)
            {
                return players.Exists(player => !intruders.Contains(player.userID) && CanEject(player));
            }

            private bool CanEject(BasePlayer target)
            {
                if (target.IsNull() || target.userID == ownerId)
                {
                    return false;
                }

                if (CannotEnter(target, false))
                {
                    return true;
                }

                if (CanEject() && !IsAlly(target))
                {
                    Message(target, "OnPlayerEntryRejected");
                    return true;
                }

                return false;
            }

            public bool CanEject()
            {
                if (IsPayLocked) return AllowPVP ? Options.EjectPurchasedPVP : Options.EjectPurchasedPVE;
                if (ownerId.IsSteamId()) return AllowPVP ? Options.EjectLockedPVP : Options.EjectLockedPVE;
                return false;
            }

            private bool CannotEnter(BasePlayer target, bool justEntered)
            {
                if (GetRaider(target).IsAllowed)
                {
                    if (IsBanned(target))
                    {
                        return RemovePlayer(target, Location, ProtectionRadius, Type);
                    }
                }
                else if (Exceeds(target) || HasLockout(target) || IsBanned(target) || IsHogging(target) || justEntered && Teleported(target))
                {
                    return RemovePlayer(target, Location, ProtectionRadius, Type);
                }

                return false;
            }

            public bool IsControlledMount(BaseEntity m)
            {
                if (config.Settings.Management.Mounts.ControlledMounts)
                {
                    return false;
                }

                if (m is BaseChair chair)
                {
                    DismountAllPlayers(chair);

                    return true;
                }

                if (!(m.GetParentEntity() is BaseEntity parentEntity) || parentEntity is RidableHorse)
                {
                    return false;
                }

                if (parentEntity.GetType().Name.Contains("Controller"))
                {
                    DismountAllPlayers(m as BaseMountable);

                    return true;
                }

                return false;
            }

            private bool IsBlockingCampers(ModularCar car)
            {
                if (!config.Settings.Management.Mounts.Campers || car.AttachedModuleEntities == null)
                {
                    return false;
                }

                return car.AttachedModuleEntities.Exists(module => module.ShortPrefabName.Contains("module_camper"));
            }

            private bool TryRemoveMountable(BaseEntity m, List<BasePlayer> players)
            {
                if (m.IsNull() || Type == RaidableType.None || m.GetParentEntity() is TrainCar || IsControlledMount(m) || Entities.Contains(m))
                {
                    return false;
                }

                if (m is HotAirBalloon && (config.Settings.Management.Mounts.HotAirBalloon || CanEject(players)))
                {
                    return Eject(m, Location, ProtectionRadius, false);
                }

                if (m is BaseMountable m2 && (ShouldEject(m) || CanEject(players) || config.Settings.Management.Mounts.FlyingCarpet && m.ObjectName() == "FlyingCarpet"))
                {
                    return EjectMountable(m2, players, Location, ProtectionRadius);
                }

                return false;
            }

            private bool ShouldEject(BaseEntity entity) => entity switch
            {
                Parachute _ => config.Settings.Management.Mounts.Parachutes,
                Tugboat _ => config.Settings.Management.Mounts.Tugboats,
                BaseBoat _ => config.Settings.Management.Mounts.Boats,
                BasicCar _ => config.Settings.Management.Mounts.BasicCars,
                ModularCar car => config.Settings.Management.Mounts.ModularCars || IsBlockingCampers(car),
                CH47Helicopter _ => config.Settings.Management.Mounts.CH47,
                RidableHorse _ => config.Settings.Management.Mounts.Horses,
                ScrapTransportHelicopter _ => config.Settings.Management.Mounts.Scrap,
                AttackHelicopter _ => config.Settings.Management.Mounts.AttackHelicopters,
                Minicopter _ => config.Settings.Management.Mounts.MiniCopters,
                Snowmobile _ => config.Settings.Management.Mounts.Snowmobile,
                StaticInstrument _ => config.Settings.Management.Mounts.Pianos,
                _ => config.Settings.Management.Mounts.Other
            };

            public static bool IsWearingJetpack(BasePlayer player)
            {
                return !player.IsKilled() && player.GetMounted() is BaseMountable m && (m.ShortPrefabName == "testseat" || m.ShortPrefabName == "standingdriver") && m.GetParentEntity() is DroppedItemContainer;
            }

            private static void TryPushMountable(BaseVehicle vehicle, Vector3 target)
            {
                Rigidbody body = vehicle.rigidBody ?? vehicle.GetComponent<Rigidbody>();

                if (vehicle is BaseHelicopter || vehicle is Parachute || vehicle is Tugboat || vehicle.PrefabName.Contains("modularcar"))
                {
                    Vector3 normalized = Vector3.ProjectOnPlane(vehicle.transform.position - target, vehicle.transform.up).normalized;
                    float d2 = body.mass * (vehicle is Tugboat ? 10f : 4f);
                    body.AddForce(-normalized * d2 + Vector3.up * (vehicle is Parachute ? 100f : 15f), ForceMode.Impulse);
                }
                else
                {
                    var e = vehicle.transform.eulerAngles; // credits k1lly0u
                    vehicle.transform.rotation = Quaternion.Euler(e.x, e.y - 180f, e.z);
                    if (body != null) body.velocity *= -1f;
                }
            }

            private static bool IsFlying(BasePlayer player)
            {
                return !player.IsKilled() && !player.modelState.onground && TerrainMeta.HeightMap.GetHeight(player.transform.position) < player.transform.position.y - 1f;
            }

            private void TryEjectMountable(BaseEntity e)
            {
                if (e is BaseMountable m)
                {
                    if (GetMountedPlayers(m).Count == 0)
                    {
                        EjectMountable(m, new(), Location, ProtectionRadius);
                    }
                }
                else if (e is HotAirBalloon hab)
                {
                    if (GetMountedPlayers(hab).Count == 0)
                    {
                        Eject(hab, Location, ProtectionRadius, false);
                    }
                }
            }

            public bool Eject(BaseEntity m, Vector3 position, float radius, bool groundLevel)
            {
                var j = TerrainMeta.HeightMap.GetHeight(m.transform.position) - m.transform.position.y;
                var target = ((m.transform.position.XZ3D() - position.XZ3D()).normalized * (radius + 10f)) + position;

                if (groundLevel)
                {
                    target.y = SpawnsController.GetSpawnHeight(target);
                }
                else
                {
                    target.y = Mathf.Max(m.transform.position.y, SpawnsController.GetSpawnHeight(target)) + 5f;
                }

                m.transform.position = target;
                m.TransformChanged();
                m.SendNetworkUpdate();

                return true;
            }

            public bool EjectMountable(BaseMountable m, List<BasePlayer> players, Vector3 position, float radius)
            {
                m = GetParentMountableEntity(m);

                var j = TerrainMeta.HeightMap.GetHeight(m.transform.position) - m.transform.position.y;
                float distance = m is RidableHorse ? 2f : 10f;

                if (j > 5f)
                {
                    distance += j;
                }

                var target = ((m.transform.position.XZ3D() - position.XZ3D()).normalized * (radius + distance)) + position;

                if (m is BaseHelicopter || players.Exists(player => IsFlying(player)))
                {
                    target.y = Mathf.Max(m.transform.position.y, SpawnsController.GetSpawnHeight(target)) + 5f;
                }
                else target.y = SpawnsController.GetSpawnHeight(target) + 0.1f;

                BaseVehicle vehicle = m.HasParent() ? m.VehicleParent() : m as BaseVehicle;

                if (vehicle.IsNull() || m is RidableHorse || m is not BaseHelicopter && m is not Parachute && InRange(m.transform.position, position, radius + 1f))
                {
                    if (m.GetParentEntity() is BaseEntity parentEntity && parentEntity != m)
                    {
                        parentEntity.transform.position = target;
                    }
                    m.transform.position = target;
                }
                else
                {
                    TryPushMountable(vehicle, target);
                }

                return true;
            }

            private static BaseMountable GetParentMountableEntity(BaseMountable m)
            {
                while (m != null && m.HasParent())
                {
                    if (!(m.GetParentEntity() is BaseMountable parent)) break;
                    m = parent;
                }

                return m;
            }

            public bool CanSetupEntity(BaseEntity e)
            {
                if (e.IsKilled() || setupBlockedPrefabs.Exists(e.ShortPrefabName.Contains))
                {
                    if (e.IsValid()) Instance.temporaryData.NetworkIdentifiers.Remove(e.net.ID.Value);
                    Interface.Oxide.NextTick(e.SafelyKill);
                    Entities.Remove(e);
                    return false;
                }

                e.net ??= Net.sv.CreateNetworkable();

                return true;
            }

            public void TryRespawnNpc(bool isMurderer)
            {
                if (!IsOpened && !Options.Levels.Level2 || isInvokingRespawnNpc)
                {
                    return;
                }

                Invoke(() => RespawnNpcNow(isMurderer), Options.RespawnRateMin > 0 ? UnityEngine.Random.Range(Options.RespawnRateMin, Options.RespawnRateMax) : Options.RespawnRateMax);

                isInvokingRespawnNpc = true;
            }

            private void RespawnNpcNow(bool isMurderer)
            {
                isInvokingRespawnNpc = false;

                if (npcs.Count < npcMaxAmountMurderers + npcMaxAmountScientists)
                {
                    SpawnNpc(isMurderer);
                }

                if (npcs.Count < npcMaxAmountMurderers + npcMaxAmountScientists)
                {
                    TryRespawnNpc(isMurderer);
                }
            }

            public void SpawnNpcs()
            {
                if (!Options.NPC.Enabled || (Options.NPC.UseExpansionNpcs && config.Settings.ExpansionMode && Instance.DangerousTreasures.CanCall()))
                {
                    return;
                }

                if (npcMaxAmountMurderers > 0)
                {
                    for (int i = 0; i < npcMaxAmountMurderers; i++)
                    {
                        SpawnNpc(true);
                    }
                }

                if (npcMaxAmountScientists > 0)
                {
                    for (int i = 0; i < npcMaxAmountScientists; i++)
                    {
                        SpawnNpc(false);
                    }
                }
            }

            public bool IsInForwardOperatingBase(Vector3 from)
            {
                return BuiltList.Count > 0 && BuiltList.Values.Exists(to => InRange2D(from, to, 3f));
            }

            public bool NearFoundation(Vector3 from, float range = 5f)
            {
                return foundations.Exists(to => InRange2D(from, to, range));
            }

            private NavMeshHit _navHit;

            public bool FindPointOnNavmesh(Vector3 a, float radius, out Vector3 v)
            {
                for (int tries = 25; tries > 0; --tries)
                {
                    if (NavMesh.SamplePosition(a, out _navHit, radius, 25) && !NearFoundation(_navHit.position) && !IsNpcNearSpot(_navHit.position) && IsAcceptableWaterDepth(_navHit.position) && !TestInsideObject(_navHit.position))
                    {
                        v = _navHit.position;
                        return true;
                    }
                }

                v = default;
                return false;
            }

            private bool IsAcceptableWaterDepth(Vector3 position)
            {
                return WaterLevel.GetOverallWaterDepth(position, true, true, null, false) <= config.Settings.Management.WaterDepth;
            }

            private bool TestInsideObject(Vector3 position)
            {
                return GamePhysics.CheckSphere(position, 0.5f, Layers.Mask.Player_Server | Layers.Server.Deployed, QueryTriggerInteraction.Ignore) || TestInsideRock(position);
            }

            private bool TestClippedInside(Vector3 position, float radius, int mask)
            {
                var colliders = Pool.GetList<Collider>();
                Vis.Colliders<Collider>(position, radius, colliders, mask, QueryTriggerInteraction.Ignore);
                bool isClipped = colliders.Count > 0;
                Pool.FreeList(ref colliders);
                return isClipped;
            }

            private bool TestInsideRock(Vector3 a)
            {
                bool isInside = false;
                bool faces = Physics.queriesHitBackfaces;
                Physics.queriesHitBackfaces = true;
                try { isInside = IsRockFaceUpwards(a); } catch { }
                Physics.queriesHitBackfaces = faces;
                if (isInside) return true;
                try { isInside = IsRockFaceDownwards(a); } catch { }
                return isInside;
            }

            private RaycastHit _hit;

            private bool IsRockFaceDownwards(Vector3 a)
            {
                Vector3 b = a + new Vector3(0f, 30f, 0f);
                Vector3 d = a - b;
                return Array.Exists(Physics.RaycastAll(b, d, d.magnitude, Layers.World), hit => IsRock(hit.collider.ObjectName()));
            }

            private bool IsRockFaceUpwards(Vector3 point)
            {
                return Physics.Raycast(point, Vector3.up, out _hit, 30f, Layers.Mask.World | Layers.Mask.Terrain) && ((_hit.collider.IsOnLayer(Layer.Terrain) || IsRock(_hit.collider.ObjectName())));
            }

            private bool IsRockFaceUpwardsSecondary(Vector3 point)
            {
                bool isRockFaceUpwards = false;
                bool faces = Physics.queriesHitBackfaces;
                Physics.queriesHitBackfaces = true;
                try { isRockFaceUpwards = IsRockFaceUpwards(point); } catch { }
                Physics.queriesHitBackfaces = faces;
                return isRockFaceUpwards;
            }

            private bool IsRock(string name) => _prefabs.Exists(value => name.Contains(value, CompareOptions.OrdinalIgnoreCase));

            private List<string> _prefabs = new() { "rock", "formation", "cliff" };

            private bool InstantiateEntity(List<Vector3> wander, Vector3 position, bool isStationary, out HumanoidBrain brain, out HumanoidNPC npc)
            {
                static void CopySerializableFields<T>(T src, T dst)
                {
                    var srcFields = typeof(T).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    foreach (var field in srcFields)
                    {
                        object value = field.GetValue(src);
                        field.SetValue(dst, value);
                    }
                }

                //"assets/prefabs/player/player.prefab"
                var prefabName = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab";
                var prefab = GameManager.server.FindPrefab(prefabName);
                var go = Facepunch.Instantiate.GameObject(prefab, position, Quaternion.identity);

                go.SetActive(false);

                go.name = prefabName;

                ScientistBrain scientistBrain = go.GetComponent<ScientistBrain>();
                ScientistNPC scientistNpc = go.GetComponent<ScientistNPC>();

                npc = go.AddComponent<HumanoidNPC>();
                npc.enableSaving = false;

                brain = go.AddComponent<HumanoidBrain>();
                brain.Instance = Instance;
                brain.positions = wander;
                brain.DestinationOverride = position;
                brain.CheckLOS = brain.RefreshKnownLOS = true;
                brain.SenseRange = Options.NPC.AggressionRange;
                brain.softLimitSenseRange = brain.SenseRange + (brain.SenseRange * 0.25f);
                brain.TargetLostRange = brain.SenseRange * 1.25f;
                brain.Settings = Options.NPC;
                brain.isStationary = isStationary;
                brain.UseAIDesign = false;
                brain._baseEntity = npc;
                brain.raid = this;
                brain.npc = npc;
                brain.thinker = npc;
                brain.NpcTransform = npc.transform;
                npc.Brain = brain;

                CopySerializableFields(scientistNpc, npc);
                DestroyImmediate(scientistBrain, true);
                DestroyImmediate(scientistNpc, true);

                SceneManager.MoveGameObjectToScene(go, Rust.Server.EntityScene);

                go.SetActive(true);

                return npc != null;
            }

            private List<Vector3> GetWanderPositions(float radius)
            {
                List<Vector3> m = new();

                for (int i = 0; i < 11; i++)
                {
                    var target = Location + UnityEngine.Random.onUnitSphere * radius;

                    target.y = TerrainMeta.HeightMap.GetHeight(target);

                    if (FindPointOnNavmesh(target, radius, out var v))
                    {
                        m.Add(v);
                    }
                }

                return m;
            }

            private float GetSpawnRadius() => Mathf.Clamp(Options.ArenaWalls.Radius, CELL_SIZE, ProtectionRadius * 0.9f);

            private HumanoidNPC SpawnNpc(bool isMurderer)
            {
                if (isMurderer && !Options.NPC.Inside.SpawnMurderersOutside)
                    return null;

                bool isStationary = SpawnInsideBase(isMurderer, out var position);

                if (!isMurderer && !Options.NPC.Inside.SpawnScientistsOutside && position == default)
                    return null;

                var positions = GetWanderPositions(GetSpawnRadius());

                if (positions.Count == 0 && !isStationary)
                    return null;

                if (position == default)
                    position = positions.GetRandom();

                //bool unwakeable = Options.NPC.Inside.Sleepers.Enabled && Options.NPC.Inside.Sleepers.Unwakeable && isStationary;

                if (position == Vector3.zero || !InstantiateEntity(positions, position, isStationary, out var brain, out var npc))
                    return null;

                if (isStationary)
                {
                    npcAmountInside++;
                    isMurderer = false;
                }

                npc.skinID = 14922524;
                npc.userID = (ulong)UnityEngine.Random.Range(0, 10000000);
                npc.UserIDString = npc.userID.ToString();
                List<string> RandomNames = isMurderer ? Options.NPC.RandomMurdererNames : Options.NPC.RandomScientistNames;
                npc.displayName = RandomNames.Count > 0 ? RandomNames.GetRandom() : RandomUsernames.Get(npc.userID);
                brain.displayName = npc.displayName;
                brain.isMurderer = isMurderer;
                Instance.HumanoidBrains[npc.userID] = brain;

                BasePlayer.bots.Add(npc);

                Authorize(npc);

                npcs.Add(npc);

                npc.loadouts = Array.Empty<PlayerInventoryProperties>();

                npc.Spawn();

                npc.CancelInvoke(npc.EquipTest);

                brain.TryStartSleeping();

                SetupNpc(npc, brain, positions);

                return npc;
            }

            public class Loadout
            {
                public List<PlayerInventoryProperties.ItemAmountSkinned> belt = new();
                public List<PlayerInventoryProperties.ItemAmountSkinned> main = new();
                public List<PlayerInventoryProperties.ItemAmountSkinned> wear = new();
            }

            private PlayerInventoryProperties GetLoadout(HumanoidNPC npc, HumanoidBrain brain)
            {
                var loadout = CreateLoadout(npc, brain);
                var pip = ScriptableObject.CreateInstance<PlayerInventoryProperties>();

                pip.belt = loadout.belt;
                pip.main = loadout.main;
                pip.wear = loadout.wear;

                return pip;
            }

            private Loadout CreateLoadout(HumanoidNPC npc, HumanoidBrain brain)
            {
                var loadout = new Loadout();
                var items = brain.isMurderer ? Options.NPC.MurdererLoadout : Options.NPC.ScientistLoadout;

                AddItemAmountSkinned(loadout.wear, items.Boots);
                AddItemAmountSkinned(loadout.wear, items.Gloves);
                AddItemAmountSkinned(loadout.wear, items.Helm);
                AddItemAmountSkinned(loadout.wear, items.Pants);
                AddItemAmountSkinned(loadout.wear, items.Shirt);
                AddItemAmountSkinned(loadout.wear, items.Torso);
                if (!items.Torso.Exists(v => v.Contains("suit")))
                {
                    AddItemAmountSkinned(loadout.wear, items.Kilts);
                }
                AddItemAmountSkinned(loadout.belt, items.Weapon);

                return loadout;
            }

            private void AddItemAmountSkinned(List<PlayerInventoryProperties.ItemAmountSkinned> source, List<string> shortnames)
            {
                if (shortnames.Count == 0)
                {
                    return;
                }

                string shortname = shortnames.GetRandom();

                if (string.IsNullOrEmpty(shortname))
                {
                    return;
                }

                ItemDefinition def = ItemManager.FindItemDefinition(shortname);

                if (def == null)
                {
                    Puts("Invalid shortname {0} in profile {1}", shortname, ProfileName);
                    return;
                }

                bool isThrownWeapon = def.GetComponent<ItemModEntity>()?.entityPrefab.Get().GetComponent<ThrownWeapon>().IsNull() == false;

                if (isThrownWeapon)
                {
                    if (npcAmountThrown >= Options.NPC.Thrown)
                    {
                        shortnames.Remove(shortname);
                        AddItemAmountSkinned(source, shortnames);
                        return;
                    }
                    else npcAmountThrown++;
                }

                ulong skin = GetItemSkin(def, SkinType.Npc, 0uL, config.Skins.Npc.Unique, config.Skins.Npc.Unique, config.Skins.Npc.Random, config.Skins.Npc.Workshop, config.Skins.Npc.ImportedWorkshop, config.Skins.Npc.ApprovedOnly, def.stackable);

                source.Add(new()
                {
                    amount = 1,
                    itemDef = def,
                    skinOverride = skin,
                    startAmount = 1
                });
            }

            private void SetupNpc(HumanoidNPC npc, HumanoidBrain brain, List<Vector3> positions)
            {
                if (!Options.NPC.AlternateScientistLoot.None)
                {
                    var loot = Options.NPC.AlternateScientistLoot;

                    if (loot.Enabled && loot.IDs.Count > 0 && StringPool.toString.TryGetValue(loot.GetRandom(), out var prefab))
                    {
                        if (GameManager.server.FindPrefab(prefab) is GameObject obj && obj.GetComponent<ScientistNPC>() is ScientistNPC obj2)
                        {
                            npc.LootSpawnSlots = obj2.LootSpawnSlots;
                        }
                    }
                }
                else npc.LootSpawnSlots = Array.Empty<LootContainer.LootSpawnSlot>();

                npc.CancelInvoke(npc.PlayRadioChatter);
                npc.DeathEffects = Array.Empty<GameObjectRef>();
                npc.RadioChatterEffects = Array.Empty<GameObjectRef>();
                npc.radioChatterType = ScientistNPC.RadioChatterType.NONE;
                npc.startHealth = brain.isMurderer ? Options.NPC.MurdererHealth : Options.NPC.ScientistHealth;
                npc.InitializeHealth(npc.startHealth, npc.startHealth);
                npc.Invoke(() => UpdateItems(npc, brain, brain.isMurderer), 0.2f);
                npc.Invoke(() => brain.SetupMovement(positions), 0.3f);

                GiveKit(npc, brain, brain.isMurderer);
            }

            private void CopyLoadout(HumanoidNPC npc, HumanoidBrain brain)
            {
                if (brain.isSleeper && Options.NPC.Inside.Sleepers.CopyLoadout || !brain.isSleeper && Options.NPC.CopyLoadout)
                {
                    brain.keepInventory = true;
                }
            }

            private void CopyKit(HumanoidNPC npc, HumanoidBrain brain)
            {
                if (brain.isSleeper && Options.NPC.Inside.Sleepers.CopyKit || !brain.isSleeper && Options.NPC.CopyKit)
                {
                    brain.keepInventory = true;
                }
            }

            private void GiveKit(HumanoidNPC npc, HumanoidBrain brain, bool isMurderer)
            {
                List<string> kits = isMurderer ? murdererKits : scientistKits;

                if (kits.Count > 0)
                {
                    string kit = kits.GetRandom();

                    if (Options.NPC.UniqueKits && (isMurderer && Options.NPC.MurdererKits.Count >= npcMaxAmountMurderers || !isMurderer && Options.NPC.ScientistKits.Count >= npcMaxAmountScientists))
                    {
                        kits.Remove(kit);
                    }

                    if (Instance.Kits?.Call("GiveKit", npc, kit) is string val)
                    {
                        if (val.Contains("Couldn't find the player"))
                        {
                            val = "Npcs cannot use the CopyPasteFile field in Kits";
                        }
                        Puts("Invalid kit '{0}' ({1})", kit, val);
                    }
                    else CopyKit(npc, brain);
                }

                bool isInventoryEmpty = npc.inventory.AllItems().Length == 0;

                if (isInventoryEmpty)
                {
                    var loadout = GetLoadout(npc, brain);

                    if (loadout.belt.Count > 0 || loadout.main.Count > 0 || loadout.wear.Count > 0)
                    {
                        npc.loadouts = new PlayerInventoryProperties[1];
                        npc.loadouts[0] = loadout;
                        npc.EquipLoadout(npc.loadouts);
                        CopyLoadout(npc, brain);
                        isInventoryEmpty = false;
                    }
                }

                if (isInventoryEmpty)
                {
                    npc.inventory.GiveItem(ItemManager.CreateByName(isMurderer ? "halloween.surgeonsuit" : "hazmatsuit.spacesuit", 1, 0uL), npc.inventory.containerWear);
                    npc.inventory.GiveItem(ItemManager.CreateByName(isMurderer ? "knife.combat" : "pistol.python", 1, isMurderer ? 1703079727uL : 2241854552uL), npc.inventory.containerBelt);
                }
            }

            private void UpdateItems(HumanoidNPC npc, HumanoidBrain brain, bool isMurderer)
            {
                brain.Init();

                EquipWeapon(npc, brain);

                if (!ToggleNpcMinerHat(npc, TOD_Sky.Instance?.IsNight == true))
                {
                    npc.inventory.ServerUpdate(0f);
                }
            }

            public void EquipWeapon(HumanoidNPC npc, HumanoidBrain brain)
            {
                bool isHoldingProjectileWeapon = false;

                foreach (Item item in npc.inventory.AllItems())
                {
                    if (item.GetHeldEntity() is HeldEntity e && e.IsValid())
                    {
                        AddTemporary(e);

                        if (item.skin != 0)
                        {
                            e.skinID = item.skin;
                            e.SendNetworkUpdate();
                        }

                        if (!(e is AttackEntity attackEntity))
                        {
                            continue;
                        }

                        if (!isHoldingProjectileWeapon && attackEntity.hostileScore >= 2f && item.GetRootContainer() == npc.inventory.containerBelt && brain._attackEntity.IsNull())
                        {
                            isHoldingProjectileWeapon = e is BaseProjectile;

                            brain.UpdateWeapon(attackEntity, item.uid);
                        }

                        if (attackEntity is MedicalTool tool)
                        {
                            brain.MedicalTools.Add(tool.GetItem());
                        }
                        else if (attackEntity.hostileScore >= 2f)
                        {
                            brain.AttackWeapons.Add(attackEntity);
                        }
                    }

                    item.MarkDirty();
                }

                brain.EnableMedicalTools();

                brain.IdentifyWeapon();
            }

            private void SortRandomNpcSpots()
            {
                List<string> platforms = new() { "floor", "floor.triangle", "roof", "roof.triangle" };
                for (int i = 0; i < blocks.Count; i++)
                {
                    var block = blocks[i];
                    if (block.IsKilled() || !Options.NPC.Roofcampers && platforms.Contains(block.ShortPrefabName) && IsOutside(block))
                    {
                        continue;
                    }
                    var center = block.CenterPoint();
                    if (Physics.Raycast(center + new Vector3(0f, block.bounds.extents.y + 0.45f), Vector3.up, 1.75f, Layers.Mask.Construction | Layers.Mask.Deployed))
                    {
                        continue;
                    }
                    var entities = FindEntitiesOfType<BaseEntity>(block.transform.position + Vector3.up * 1.25f, 1.5f, blockLayers);
                    if (entities.Exists(e => Instance.DeployableItems.ContainsKey(e.PrefabName) && e.bounds.extents.y > 0.2f))
                    {
                        continue;
                    }
                    var walls = entities.Count(e => e.ShortPrefabName == "wall");
                    if (walls < 3 && block.ShortPrefabName == "foundation.triangle" || walls < 4 && block.ShortPrefabName == "foundation")
                    {
                        _inside.Add(center + new Vector3(0f, block.bounds.extents.y + 0.1125f));
                    }
                    else if (walls < 3 && block.ShortPrefabName == "floor.triangle" || walls < 4 && block.ShortPrefabName == "floor")
                    {
                        _inside.Add(center + new Vector3(0f, 0.155f));
                    }
                }
            }

            private bool IsOutside(BaseEntity entity) => entity.IsOutside(entity.WorldSpaceBounds().position.WithY(entity.transform.position.y));

            public bool SpawnInsideBase(bool f, out Vector3 v)
            {
                if (f)
                {
                    v = default;
                    return false;
                }

                if (npcMaxAmountInside == -1)
                {
                    npcMaxAmountInside = npcMaxAmountScientists;
                }

                if (npcAmountInside >= npcMaxAmountInside)
                {
                    v = default;
                    return false;
                }

                return FindRandomRug(out v) || FindRandomBed(out v) || FindRandomFloor(out v);
            }

            private bool FindRandomRug(out Vector3 v)
            {
                if (Options.NPC.Inside.SpawnOnRugs)
                {
                    var rug = _rugs.FirstOrDefault(x => !x.IsKilled() && !IsNpcNearSpot(x.transform.position));
                    v = rug ? rug.transform.position : default;
                    return v != default;
                }

                v = default;
                return false;
            }

            private bool FindRandomBed(out Vector3 v)
            {
                if (Options.NPC.Inside.SpawnOnBeds)
                {
                    var bed = _beds.FirstOrDefault(x => !x.IsKilled() && !IsNpcNearSpot(x.transform.position));
                    v = bed ? bed.transform.position : default;
                    return v != default;
                }

                v = default;
                return false;
            }

            private bool FindRandomFloor(out Vector3 v)
            {
                if (Options.NPC.Inside.SpawnOnFloors)
                {
                    if (_inside.Count == 0)
                    {
                        SortRandomNpcSpots();
                    }

                    Shuffle(_inside);
                    _beds.RemoveAll(IsKilled);
                    _decorDeployables.RemoveAll(IsKilled);

                    foreach (var position in _inside)
                    {
                        if (Options.NPC.Inside.SpawnOnRugs && _decorDeployables.Exists(x => x.ShortPrefabName.StartsWith("rug") && InRange(x.transform.position, position, 1f)))
                        {
                            continue;
                        }

                        if (Options.NPC.Inside.SpawnOnBeds && _beds.Exists(x => InRange(x.transform.position, position, 1f)) || IsNpcNearSpot(position))
                        {
                            continue;
                        }

                        v = position;
                        return true;
                    }
                }

                v = default;
                return false;
            }

            private bool IsNpcNearSpot(Vector3 position)
            {
                return npcs.Exists(npc => !npc.IsKilled() && InRange(npc.transform.position, position, 0.5f));
            }

            private void SetupNpcKits()
            {
                if (npcMaxAmountScientists > 0 || npcMaxAmountMurderers > 0)
                {
                    scientistKits.AddRange(Options.NPC.ScientistKits.Where(kit => Convert.ToBoolean(Instance.Kits?.Call("isKit", kit))));
                    murdererKits.AddRange(Options.NPC.MurdererKits.Where(kit => Convert.ToBoolean(Instance.Kits?.Call("isKit", kit))));
                    SpawnNpcs();
                }
            }

            public string DespawnString => despawnDateTime == DateTime.MaxValue ? string.Empty : $"[{DespawnTime}m]";

            public double DespawnTime => despawnDateTime != DateTime.MaxValue && DespawnMinutesInactive > 0 && despawnDateTime.Subtract(DateTime.Now).TotalSeconds > 0 ? Math.Ceiling(despawnDateTime.Subtract(DateTime.Now).TotalMinutes) : 0;

            public void UpdateMarker()
            {
                if (IsLoading)
                {
                    Invoke(UpdateMarker, 1f);
                    return;
                }

                if (!genericMarker.IsKilled())
                {
                    genericMarker.SendUpdate();
                }

                if (!explosionMarker.IsKilled())
                {
                    explosionMarker.transform.position = Location;
                    explosionMarker.SendNetworkUpdate();
                }

                if (!vendingMarker.IsKilled())
                {
                    string despawnText = DespawnTime > 0 ? string.Format(" [{0}]", mx("UIFormatLockoutMinutes", null, DespawnTime)) : null;
                    vendingMarker.transform.position = Location;
                    vendingMarker.markerShopName = (markerName == config.Settings.Markers.MarkerName ? mx("MapMarkerOrderWithMode", null, mx(GetAllowKey()), Mode(), markerName, despawnText) : string.Format("{0} {1}", mx(GetAllowKey()), markerName)).Trim();
                    vendingMarker.SendNetworkUpdate();
                }

                if (markerCreated || !IsMarkerAllowed())
                {
                    return;
                }

                if (config.Settings.Markers.UseExplosionMarker)
                {
                    explosionMarker = GameManager.server.CreateEntity(StringPool.Get(4060989661), Location) as MapMarkerExplosion;

                    if (!explosionMarker.IsNull())
                    {
                        explosionMarker.Spawn();
                        explosionMarker.Invoke(() => explosionMarker.CancelInvoke(explosionMarker.DelayedDestroy), 1f);
                    }
                }
                else if (config.Settings.Markers.UseVendingMarker)
                {
                    vendingMarker = GameManager.server.CreateEntity(StringPool.Get(3459945130), Location) as VendingMachineMapMarker;

                    if (!vendingMarker.IsNull())
                    {
                        string flag = mx(GetAllowKey());
                        string despawnText = DespawnMinutesInactive > 0 ? string.Format(" [{0}m]", DespawnMinutesInactive.ToString()) : null;

                        if (markerName == config.Settings.Markers.MarkerName)
                        {
                            vendingMarker.markerShopName = mx("MapMarkerOrderWithMode", null, flag, Mode(), markerName, despawnText);
                        }
                        else vendingMarker.markerShopName = mx("MapMarkerOrderWithoutMode", null, flag, markerName, despawnText);

                        vendingMarker.enabled = false;
                        vendingMarker.Spawn();
                    }
                }

                markerCreated = true;
                UpdateMarker();
            }

            private void CreateGenericMarker()
            {
                if (IsMarkerAllowed() && (config.Settings.Markers.UseExplosionMarker || config.Settings.Markers.UseVendingMarker))
                {
                    genericMarker = GameManager.server.CreateEntity(StringPool.Get(2849728229), Location) as MapMarkerGenericRadius;

                    if (!genericMarker.IsNull())
                    {
                        genericMarker.alpha = 0.75f;
                        genericMarker.color1 = GetMarkerColor1();
                        genericMarker.color2 = GetMarkerColor2();
                        genericMarker.radius = Mathf.Min(2.5f, World.Size <= 3600 ? config.Settings.Markers.SubRadius : config.Settings.Markers.Radius);
                        genericMarker.Spawn();
                        genericMarker.SendUpdate();
                    }
                }
            }

            private bool TryParseHtmlString(string value, out Color color) => ColorUtility.TryParseHtmlString(value.StartsWith("#") ? value : $"#{value}", out color);

            private Color GetMarkerColor1() => Type == RaidableType.None ? Color.clear : Options.Mode switch
            {
                RaidableMode.Easy => TryParseHtmlString(config.Settings.Management.Colors1.Easy, out var color) ? color : Color.green,
                RaidableMode.Medium => TryParseHtmlString(config.Settings.Management.Colors1.Medium, out var color) ? color : Color.yellow,
                RaidableMode.Hard => TryParseHtmlString(config.Settings.Management.Colors1.Hard, out var color) ? color : Color.red,
                RaidableMode.Expert => TryParseHtmlString(config.Settings.Management.Colors1.Expert, out var color) ? color : Color.blue,
                _ => TryParseHtmlString(config.Settings.Management.Colors1.Nightmare, out var colorDefault) ? colorDefault : Color.black
            };

            private Color GetMarkerColor2() => Type == RaidableType.None ? NoneColor : Options.Mode switch
            {
                RaidableMode.Easy => TryParseHtmlString(config.Settings.Management.Colors2.Easy, out var color) ? color : Color.green,
                RaidableMode.Medium => TryParseHtmlString(config.Settings.Management.Colors2.Medium, out var color) ? color : Color.yellow,
                RaidableMode.Hard => TryParseHtmlString(config.Settings.Management.Colors2.Hard, out var color) ? color : Color.red,
                RaidableMode.Expert => TryParseHtmlString(config.Settings.Management.Colors2.Expert, out var color) ? color : Color.blue,
                _ => TryParseHtmlString(config.Settings.Management.Colors2.Nightmare, out var colorDefault) ? colorDefault : Color.black
            };

            private bool IsMarkerAllowed() => Options.Silent ? false : Type switch
            {
                RaidableType.Maintained => config.Settings.Markers.Maintained,
                RaidableType.Purchased => config.Settings.Markers.Buyables,
                RaidableType.Scheduled => config.Settings.Markers.Scheduled,
                _ => config.Settings.Markers.Manual
            };

            public void DestroyLocks()
            {
                locks.ForEach(SafelyKill);
            }

            public void DestroyNpcs()
            {
                npcs.ForEach(SafelyKill);
            }

            public void DestroySpheres()
            {
                spheres.ForEach(SafelyKill);
            }

            private void SafelyKill(HumanoidNPC npc)
            {
                if (!npc.IsRealNull() && Instance.HumanoidBrains.TryGetValue(npc.userID, out var brain))
                {
                    brain.DisableShouldThink();
                }
                if (!npc.IsKilled())
                {
                    //npc.MovePosition(default);
                    //npc.TransformChanged();
                    npc.SafelyKill();
                }
            }

            private static void SafelyKill(BaseEntity entity) => entity.SafelyKill();

            public void DestroyMapMarkers()
            {
                if (!explosionMarker.IsKilled())
                {
                    explosionMarker.CancelInvoke(explosionMarker.DelayedDestroy);
                    explosionMarker.Kill();
                }

                genericMarker.SafelyKill();
                vendingMarker.SafelyKill();
            }
        }

        public SpawnsControllerManager SpawnsController = new();

        public class SpawnsControllerManager
        {
            internal YieldInstruction instruction0;
            internal List<ZoneInfo> managedZones;
            internal List<string> assets;
            internal List<string> AdditionalBlockedColliders;
            internal List<string> _materialNames;
            internal List<MonumentInfoEx> Monuments = new();
            public RaidableBases Instance;
            internal Configuration config => Instance.config;

            public class MonumentInfoEx
            {
                public float radius;
                public string text;
                public Vector3 position;
                public MonumentInfoEx(string text, Vector3 position, float radius)
                {
                    this.text = text;
                    this.position = position;
                    this.radius = radius;
                }
            }

            public void Initialize()
            {
                managedZones = new();
                assets = new() { "perimeter_wall", "/props/", "/structures/", "/building/", "train_", "powerline_", "dune", "candy-cane", "assets/content/nature/", "assets/content/vehicles/", "walkway", "invisible_collider", "module_", "junkpile", "low_arc" };
                _materialNames = new() { "Generic (Instance)", "Concrete (Instance)", "Rock (Instance)", "Metal (Instance)", "Snow (Instance)" };
                AdditionalBlockedColliders = new() { "powerline", "invisible", "TopCol", "swamp_", "floating_", "sentry", "walkway", "junkpile" };
                AdditionalBlockedColliders.AddRange(config.Settings.Management.AdditionalBlockedColliders);
            }

            public IEnumerator SetupMonuments()
            {
                int attempts = 0;
                while (TerrainMeta.Path == null || TerrainMeta.Path.Monuments == null || TerrainMeta.Path.Monuments.Count == 0)
                {
                    if (++attempts >= 30)
                    {
                        break;
                    }
                    yield return CoroutineEx.waitForSeconds(1f);
                }
                Monuments = new();
                foreach (var prefab in World.Serialization.world.prefabs)
                {
                    if (prefab.id == 1724395471 && prefab.category != "IGNORE_MONUMENT")
                    {
                        yield return CalculateMonumentSize(new(prefab.position.x, prefab.position.y, prefab.position.z), prefab.category);
                    }
                }
                if (TerrainMeta.Path == null || TerrainMeta.Path.Monuments == null || TerrainMeta.Path.Monuments.Count == 0)
                {
                    yield break;
                }
                foreach (var monument in TerrainMeta.Path.Monuments)
                {
                    if (monument.name.Contains("monument_marker"))
                    {
                        continue;
                    }
                    if (monument.Bounds.size.Max() > 0f)
                    {
                        Monuments.Add(new(monument.displayPhrase.english.Trim(), monument.transform.position, monument.Bounds.size.Max()));
                        continue;
                    }
                    yield return CalculateMonumentSize(monument.transform.position, string.IsNullOrEmpty(monument.displayPhrase.english.Trim()) ? monument.name.Contains("cave") ? "Cave" : monument.name : monument.displayPhrase.english.Trim());
                }
            }

            public IEnumerator CalculateMonumentSize(Vector3 from, string text)
            {
                int checks = 0;
                float radius = 15f;
                while (radius < World.Size / 2f)
                {
                    int pointsOfTopology = 0;
                    foreach (var to in GetCircumferencePositions(from, radius, 30f, false, false, 0f))
                    {
                        if (ContainsTopology(TerrainTopology.Enum.Building | TerrainTopology.Enum.Monument, to, 5f))
                        {
                            pointsOfTopology++;
                        }
                        if (++checks >= 25)
                        {
                            yield return instruction0;
                            checks = 0;
                        }
                    }
                    if (pointsOfTopology < 4)
                    {
                        break;
                    }
                    radius += 15f;
                }
                if (radius <= 15f)
                {
                    radius = 100f;
                }
                Monuments.Add(new(text, from, radius));
            }

            public List<Vector3> GetCircumferencePositions(Vector3 center, float radius, float next, bool spawnHeight = true, bool shouldSkipSmallRock = false, float y = 0f)
            {
                float degree = 0f;
                float angleInRadians = 2f * Mathf.PI;
                List<Vector3> positions = new();

                while (degree < 360)
                {
                    float radian = (angleInRadians / 360) * degree;
                    float x = center.x + radius * Mathf.Cos(radian);
                    float z = center.z + radius * Mathf.Sin(radian);
                    Vector3 a = new(x, y, z);

                    positions.Add(y == 0f ? a.WithY(spawnHeight ? GetSpawnHeight(a, true, shouldSkipSmallRock) : TerrainMeta.HeightMap.GetHeight(a)) : a);

                    degree += next;
                }

                return positions;
            }

            private bool IsValidMaterial(string materialName) => materialName.Contains("rock_") || _materialNames.Contains(materialName);

            private bool ShouldSkipSmallRock(RaycastHit hit, string colName)
            {
                return colName.Contains("rock_") || colName.Contains("formation_", CompareOptions.OrdinalIgnoreCase) ? hit.collider.bounds.size.y <= 2f : false;
            }

            public float GetSpawnHeight(Vector3 target, bool max = true, bool skip = false, int mask = targetMask)
            {
                float y = TerrainMeta.HeightMap.GetHeight(target);
                float w = TerrainMeta.WaterMap.GetHeight(target);
                float p = TerrainMeta.HighestPoint.y + 250f;
                RaycastHit[] hits = Physics.RaycastAll(target.WithY(p), Vector3.down, ++p, mask, QueryTriggerInteraction.Ignore);
                GamePhysics.Sort(hits);
                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];
                    string colName = hit.collider.ObjectName();
                    if (skip && i != hits.Length - 1 && ShouldSkipSmallRock(hit, colName))
                    {
                        continue;
                    }
                    if (AdditionalBlockedColliders.Exists(colName.Contains))
                    {
                        continue;
                    }
                    if (!IsValidMaterial(hit.collider.MaterialName()))
                    {
                        continue;
                    }
                    y = Mathf.Max(y, hit.point.y);
                    break;
                }
                y = max ? Mathf.Max(y, w) : y;
                return y;
            }

            public MinMax GetLandLevel(Vector3 from, float radius, bool spawnOnSeabed, BasePlayer player = null)
            {
                float x = 1000;
                float y = -1000;

                foreach (var to in GetCircumferencePositions(from, radius, 30f, !spawnOnSeabed, true, 0f))
                {
                    if (player != null && player.IsAdmin)
                    {
                        DrawText(player, 30f, Color.blue, to, $"{Mathf.Abs(from.y - to.y):N1}");
                        DrawLine(player, 30f, Color.blue, from, to);
                    }
                    if (to.y < x) x = to.y;
                    if (to.y > y) y = to.y;
                }

                return new(x, y);
            }

            public bool ContainsTopology(TerrainTopology.Enum mask, Vector3 position, float radius)
            {
                return (TerrainMeta.TopologyMap.GetTopology(position, radius) & (int)mask) != 0;
            }

            public bool IsLocationBlocked(Vector3 v)
            {
                string grid = PhoneController.PositionToGridCoord(v);
                return config.Settings.Management.BlockedGrids.Exists(blockedGrid => grid.Equals(blockedGrid, StringComparison.OrdinalIgnoreCase)) || IsZoneBlocked(v);
            }

            public bool IsZoneBlocked(Vector3 v)
            {
                foreach (var zone in managedZones)
                {
                    if (zone.IsPositionInZone(v))
                    {
                        return zone.IsBlocked;
                    }
                }
                return config.Settings.UseZoneManagerOnly && managedZones.Count > 0;
            }

            private bool IsValidLocation(Vector3 v, float safeRadius, float minProtectionRadius, bool spawnOnSeabed)
            {
                if (IsLocationBlocked(v))
                {
                    return false;
                }

                if (!IsAreaSafe(v, safeRadius, safeRadius, safeRadius, gridLayers, false, out var cacheType))
                {
                    return false;
                }

                if (!spawnOnSeabed && InDeepWater(v, false, 5f, 5f))
                {
                    return false;
                }

                if (IsMonumentPosition(v, config.Settings.Management.MonumentDistance > 0 ? config.Settings.Management.MonumentDistance : minProtectionRadius))
                {
                    return false;
                }

                return TopologyChecks(v, minProtectionRadius, spawnOnSeabed, out var topology);
            }

            internal bool TopologyChecks(Vector3 v, float radius, bool spawnOnSeabed, out string topology)
            {
                if (!config.Settings.Management.AllowOnBeach && ContainsTopology(TerrainTopology.Enum.Beach | TerrainTopology.Enum.Beachside, v, radius))
                {
                    topology = "Beach or Beachside";
                    return false;
                }

                if (!config.Settings.Management.AllowInland && !ContainsTopology(TerrainTopology.Enum.Beach | TerrainTopology.Enum.Beachside, v, radius))
                {
                    topology = "Inland";
                    return false;
                }

                if (!config.Settings.Management.AllowOnRailroads && ContainsTopology(TerrainTopology.Enum.Rail | TerrainTopology.Enum.Railside, v, radius) || HasPointOnPathList(TerrainMeta.Path?.Rails, v, Mathf.Max(M_RADIUS * 2f, radius)))
                {
                    topology = "Rail or Railside";
                    return false;
                }

                if (!config.Settings.Management.AllowOnBuildingTopology && ContainsTopology(TerrainTopology.Enum.Building, v, radius))
                {
                    topology = "Building";
                    return false;
                }

                if (!config.Settings.Management.AllowOnMonumentTopology && ContainsTopology(TerrainTopology.Enum.Monument, v, radius))
                {
                    topology = "Monument";
                    return false;
                }

                if (!config.Settings.Management.AllowOnRivers && ContainsTopology(TerrainTopology.Enum.River | TerrainTopology.Enum.Riverside, v, radius))
                {
                    topology = "River or Riverside";
                    return false;
                }

                if (!config.Settings.Management.AllowOnRoads && ContainsTopology(TerrainTopology.Enum.Road | TerrainTopology.Enum.Roadside, v, radius)) // || HasPointOnPathList(TerrainMeta.Path?.Roads, v, Mathf.Max(M_RADIUS * 2f, radius)))
                {
                    topology = "Road or Roadside";
                    return false;
                }

                topology = "";
                return true;
            }

            private bool HasPointOnPathList(List<PathList> paths, Vector3 point, float radius)
            {
                return !paths.IsNullOrEmpty() && paths.Exists(path => path?.Path?.Points?.Exists(p => InRange(point, p, radius)) ?? false);
            }

            public bool IsBlockedByMapPrefab(Dictionary<Vector3, float> prefabs, Vector3 position)
            {
                return prefabs.Exists(prefab => InRange(prefab.Key, position, prefab.Value));
            }

            public void ExtractLocation(RaidableSpawns spawns, Vector3 position, float maxLandLevel, float minProtectionRadius, float maxProtectionRadius, float minWaterDepthSeabed, float maxWaterDepthSeabed, float maxWaterDepth, bool spawnOnSeabed)
            {
                bool canSpawnOnSeabed = spawnOnSeabed && InDeepWater(position, true, minWaterDepthSeabed, maxWaterDepthSeabed);

                if (canSpawnOnSeabed)
                {
                    position.y = GetSpawnHeight(position, false);
                }

                if (IsValidLocation(position, CELL_SIZE, minProtectionRadius, spawnOnSeabed))
                {
                    var landLevel = GetLandLevel(position, 15f, canSpawnOnSeabed);

                    if (IsFlatTerrain(position, landLevel, maxLandLevel) && config.Settings.Management.Biomes.IsBiomeEnabled(position))
                    {
                        var rsl = new RaidableSpawnLocation(position)
                        {
                            WaterHeight = TerrainMeta.WaterMap.GetHeight(position),
                            TerrainHeight = TerrainMeta.HeightMap.GetHeight(position),
                            SpawnHeight = canSpawnOnSeabed ? position.y : GetSpawnHeight(position, false),
                            Radius = maxProtectionRadius,
                            AutoHeight = true
                        };

                        if (canSpawnOnSeabed)
                        {
                            spawns.Seabed.Add(rsl);
                        }
                        else if (rsl.WaterHeight - rsl.SpawnHeight <= maxWaterDepth)
                        {
                            spawns.Spawns.Add(rsl);
                        }
                    }
                }
            }

            public bool IsSubmerged(BuildingWaterOptions options, RaidableSpawnLocation rsl)
            {
                if (rsl.WaterHeight - rsl.TerrainHeight > options.WaterDepth)
                {
                    if (!options.AllowSubmerged)
                    {
                        return true;
                    }

                    rsl.Location.y = rsl.WaterHeight;
                }

                return !options.AllowSubmerged && options.SubmergedAreaCheck && IsSubmerged(options, rsl, rsl.Radius);
            }

            private bool IsSubmerged(BuildingWaterOptions options, RaidableSpawnLocation rsl, float radius)
            {
                if (rsl.Surroundings.Count == 0)
                {
                    rsl.Surroundings = GetCircumferencePositions(rsl.Location, radius, 90f, false, false, 1f);
                }

                foreach (var vector in rsl.Surroundings)
                {
                    float w = TerrainMeta.WaterMap.GetHeight(vector);
                    float h = GetSpawnHeight(vector, false); // TerrainMeta.HeightMap.GetHeight(vector);

                    if (w - h > options.WaterDepth)
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool IsMonumentPosition(Vector3 a, float extra)
            {
                return Monuments.Exists(mi =>
                {
                    var dist = a.Distance2D(mi.position);
                    var dir = (mi.position - a).normalized;

                    return dist <= mi.radius + a.Distance2D(mi.position + dir * extra) - dist;
                });
            }

            private bool IsSafeZone(Vector3 a, float extra)
            {
                return TriggerSafeZone.allSafeZones.Exists(triggerSafeZone =>
                {
                    try
                    {
                        return InRange2D(triggerSafeZone.transform.position, a, triggerSafeZone.triggerCollider.bounds.size.Max() + extra);
                    }
                    catch
                    {
                        return InRange2D(triggerSafeZone.transform.position, a, 200f + extra);
                    }
                });
            }

            public bool IsDangerousEvent(BaseEntity entity) => entity is StorageContainer && !entity.enableSaving && entity.OwnerID == 0;

            public bool IsSputnik(BaseEntity entity) => entity != null && entity.ShortPrefabName == "large.rechargable.battery.deployed" && entity.OwnerID == 0 && !entity.enableSaving;

            public bool IsAssetBlocked(BaseEntity entity, string colName, string entityName) => assets.Exists(colName.Contains) && (entity.IsNull() || entityName.Contains("/treessource/"));

            public bool IsAreaSafe(Vector3 area, float protectionRadius, float cupboardRadius, float worldRadius, int layers, bool isCustomSpawn, out CacheType cacheType, RaidableType type = RaidableType.None, BuildingOptionsDifficultySpawns spawns = null)
            {
                if (IsSafeZone(area, config.Settings.Management.MonumentDistance))
                {
                    Instance.Queues.Messages.Add("Safe Zone", area);
                    cacheType = CacheType.Delete;
                    return false;
                }

                CacheType worldType = layers == gridLayers ? CacheType.Delete : CacheType.Temporary;

                cacheType = CacheType.Generic;

                int hits = Physics.OverlapSphereNonAlloc(area, Mathf.Max(protectionRadius, cupboardRadius), Vis.colBuffer, layers, QueryTriggerInteraction.Collide);

                for (int i = 0; i < hits; i++)
                {
                    if (cacheType != CacheType.Generic)
                    {
                        goto next;
                    }

                    var collider = Vis.colBuffer[i];
                    var colName = collider.ObjectName();
                    var position = collider.GetPosition();

                    if (position == Vector3.zero || colName == "ZoneManager" || colName.Contains("xmas"))
                    {
                        goto next;
                    }

                    var e = collider.ToBaseEntity();
                    float dist = position.Distance(area);

                    if (e is BuildingPrivlidge)
                    {
                        if (e.OwnerID.IsSteamId() && dist <= cupboardRadius || !e.OwnerID.IsSteamId() && dist <= protectionRadius)
                        {
                            Instance.Queues.Messages.Add($"Blocked by a building privilege", position);
                            cacheType = CacheType.Privilege;
                        }
                        goto next;
                    }

                    string entityName = e.ObjectName();

                    if (!isCustomSpawn && IsAssetBlocked(e, colName, entityName))
                    {
                        if (layers == gridLayers || !collider.IsOnLayer(Layer.World))
                        {
                            Instance.Queues.Messages.Add("Blocked by a map prefab", $"{position} {colName}");
                            cacheType = CacheType.Delete;
                        }
                        goto next;
                    }

                    if (IsSputnik(e) || IsDangerousEvent(e))
                    {
                        if (!isCustomSpawn)
                        {
                            Instance.Queues.Messages.Add("Blocked by another plugin event", $"{position} {colName}");
                            cacheType = CacheType.Temporary;
                        }
                        goto next;
                    }

                    if (dist > protectionRadius)
                    {
                        goto next;
                    }

                    if (e.IsReallyValid())
                    {
                        if (e is Tugboat)
                        {
                            if (!isCustomSpawn)
                            {
                                Instance.Queues.Messages.Add("Tugboat is too close", $"{e.transform.position}");
                                cacheType = CacheType.Temporary;
                            }
                            goto next;
                        }

                        if (e.PrefabName.Contains("xmas") || entityName.StartsWith("assets/prefabs/plants") || entityName.Contains("tunnel") || e is BaseMountable)
                        {
                            goto next;
                        }

                        bool isSteamId = e.OwnerID.IsSteamId();

                        if (e is BasePlayer player)
                        {
                            if (!(!player.IsHuman() || player.IsFlying || config.Settings.Management.EjectSleepers && player.IsSleeping()))
                            {
                                Instance.Queues.Messages.Add("Player is too close", $"{player.displayName} ({player.userID}) {e.transform.position}");
                                cacheType = CacheType.Temporary;
                                goto next;
                            }
                        }
                        else if (isSteamId && e is SleepingBag)
                        {
                            goto next;
                        }
                        else if (isSteamId && spawns?.Skip == true)
                        {
                            goto next;
                        }
                        else if (isSteamId && config.Settings.Schedule.Skip && type == RaidableType.Scheduled)
                        {
                            goto next;
                        }
                        else if (isSteamId && config.Settings.Maintained.Skip && type == RaidableType.Maintained)
                        {
                            goto next;
                        }
                        else if (isSteamId && config.Settings.Buyable.Skip && type == RaidableType.Purchased)
                        {
                            goto next;
                        }
                        else if (Instance.Has(e))
                        {
                            Instance.Queues.Messages.Add("Already occupied by a raidable base", e.transform.position);
                            cacheType = CacheType.Temporary;
                            goto next;
                        }
                        else if (e.IsNpc || e is SleepingBag)
                        {
                            goto next;
                        }
                        else if (e is BaseOven)
                        {
                            if (!isCustomSpawn && e.bounds.size.Max() > 1.6f && !CanIgnoreDeployable())
                            {
                                Instance.Queues.Messages.Add("An oven is too close", e.transform.position);
                                cacheType = CacheType.Temporary;
                                goto next;
                            }
                        }
                        else if (e is PlayerCorpse corpse)
                        {
                            if (corpse.playerSteamID == 0 || corpse.playerSteamID.IsSteamId())
                            {
                                Instance.Queues.Messages.Add("A player corpse is too close", e.transform.position);
                                cacheType = CacheType.Temporary;
                                goto next;
                            }
                        }
                        else if (e is DroppedItemContainer backpack && e.ShortPrefabName != "item_drop")
                        {
                            if (backpack.playerSteamID == 0 || backpack.playerSteamID.IsSteamId())
                            {
                                Instance.Queues.Messages.Add("A player's backpack is too close", e.transform.position);
                                cacheType = CacheType.Temporary;
                                goto next;
                            }
                        }
                        else if (e.OwnerID == 0)
                        {
                            if (e is BuildingBlock)
                            {
                                Instance.Queues.Messages.Add("A building block is too close", $"{e.ShortPrefabName} {e.transform.position}");
                                cacheType = CacheType.Temporary;
                                goto next;
                            }
                            else if (e is MiningQuarry)
                            {
                                Instance.Queues.Messages.Add("A mining quarry is too close", $"{e.ShortPrefabName} {e.transform.position}");
                                cacheType = CacheType.Delete;
                                goto next;
                            }
                        }
                        else
                        {
                            if (!CanIgnoreDeployable() || !Instance.DeployableItems.ContainsKey(e.PrefabName))
                            {
                                Instance.Queues.Messages.Add("Blocked by other object", $"{e.ShortPrefabName} {e.transform.position}");
                                cacheType = CacheType.Temporary;
                            }
                            goto next;
                        }
                    }
                    else if (collider.gameObject.layer == (int)Layer.World && dist <= worldRadius && !isCustomSpawn)
                    {
                        if (colName.Contains("rock_") || colName.Contains("formation_", CompareOptions.OrdinalIgnoreCase))
                        {
                            if (collider.bounds.size.y > 2f)
                            {
                                Instance.Queues.Messages.Add("Rock is too large", position);
                                cacheType = worldType;
                                goto next;
                            }
                        }
                        else if (!config.Settings.Management.AllowOnRoads && colName.StartsWith("road_"))
                        {
                            Instance.Queues.Messages.Add("Not allowed on roads", position);
                            cacheType = CacheType.Delete;
                            goto next;
                        }
                        else if (!config.Settings.Management.AllowOnIceSheets && colName.StartsWith("ice_sheet"))
                        {
                            Instance.Queues.Messages.Add("Not allowed on ice sheets", position);
                            cacheType = CacheType.Delete;
                            goto next;
                        }
                    }
                    else if (collider.gameObject.layer == (int)Layer.Water && !isCustomSpawn)
                    {
                        if (!config.Settings.Management.AllowOnRivers && colName.StartsWith("River Mesh"))
                        {
                            Instance.Queues.Messages.Add("Not allowed on rivers", position);
                            cacheType = CacheType.Delete;
                            goto next;
                        }
                    }

                next:
                    Vis.colBuffer[i] = null;
                }

                return cacheType == CacheType.Generic;
            }

            public bool CanIgnoreDeployable() => config.Settings.Management.EjectDeployables || config.Settings.Management.KillDeployables;

            public bool IsFlatTerrain(Vector3 center, MinMax landLevel, float maxLandLevel)
            {
                return landLevel.y - landLevel.x <= maxLandLevel && landLevel.y - center.y <= maxLandLevel;
            }

            public bool InDeepWater(Vector3 v, bool seabed, float minDepth, float maxDepth)
            {
                v.y = TerrainMeta.HeightMap.GetHeight(v);

                float waterDepth = WaterLevel.GetWaterDepth(v, true, true, null);

                if (seabed)
                {
                    return waterDepth >= 0 - minDepth && waterDepth <= 0 - maxDepth;
                }

                return waterDepth > maxDepth;
            }

            public void SetupZones(Plugin ZoneManager)
            {
                if (!ZoneManager.CanCall())
                {
                    return;
                }

                var zoneIds = ZoneManager?.Call("GetZoneIDs") as string[];

                if (zoneIds == null)
                {
                    return;
                }

                managedZones.Clear();
                config.Settings.Inclusions.RemoveAll(x => string.IsNullOrEmpty(x));

                int allowed = 0, blocked = 0;

                foreach (string zoneId in zoneIds)
                {
                    var isBlocked = AddZone(ZoneManager, zoneId);

                    if (isBlocked) { blocked++; } else { allowed++; }
                }

                if (allowed > 0 || blocked > 0)
                {
                    Puts(Instance.mx("AllowedZones", null, allowed));
                    Puts(Instance.mx("BlockedZones", null, blocked));
                }
            }

            public bool AddZone(Plugin ZoneManager, string zoneId)
            {
                if (ZoneManager.Call("GetZoneLocation", zoneId) is not Vector3 zoneLoc || zoneLoc == Vector3.zero)
                {
                    return false;
                }

                var zoneName = Convert.ToString(ZoneManager.Call("GetZoneName", zoneId));
                var exists = Instance.config.Settings.Inclusions.Exists(zone => zone == "*" || zone == zoneId || !string.IsNullOrEmpty(zoneName) && zoneName.Equals(zone, StringComparison.OrdinalIgnoreCase));
                var radius = ZoneManager.Call("GetZoneRadius", zoneId);
                var size = ZoneManager.Call("GetZoneSize", zoneId);
                var isBlocked = Instance.config.Settings.UseZoneManagerOnly ? !exists : true;

                managedZones.Add(new(zoneId, zoneLoc, radius, size, isBlocked, Instance.config.Settings.ZoneDistance));

                return isBlocked;
            }

            public bool IsObstructed(Vector3 from, float radius, float landLevel, float forcedHeight, bool spawnOnSeabed, BasePlayer player = null)
            {
                int n = 5;
                float f = radius * 0.2f;
                bool flag = false;
                bool valid = player != null;
                if (forcedHeight != -1)
                {
                    landLevel += forcedHeight;
                }
                while (n-- > 0)
                {
                    float step = f * n;
                    float next = 360f / step;
                    foreach (var to in GetCircumferencePositions(from, step, next, !spawnOnSeabed, true, 0f))
                    {
                        var distance = Mathf.Abs((from - to).y);
                        if (distance > landLevel)
                        {
                            if (!valid) return true;
                            DrawText(player, 30f, Color.red, to, $"{distance:N1}");
                            flag = true;
                        }
                        else if (valid) DrawText(player, 30f, Color.green, to, $"{distance:N1}");
                    }
                }
                return flag;
            }
        }

        #region Hooks

        private void UnsubscribeHooks()
        {
            if (IsUnloading)
            {
                return;
            }

            Unsubscribe(nameof(OnCustomLootNPC));
            Unsubscribe(nameof(CanBGrade));
            Unsubscribe(nameof(CanDoubleJump));
            Unsubscribe(nameof(OnLifeSupportSavingLife));
            Unsubscribe(nameof(OnRestoreUponDeath));
            Unsubscribe(nameof(OnNpcKits));
            Unsubscribe(nameof(CanTeleport));
            Unsubscribe(nameof(canTeleport));
            Unsubscribe(nameof(canRemove));
            Unsubscribe(nameof(CanEntityBeTargeted));
            Unsubscribe(nameof(CanEntityTrapTrigger));
            Unsubscribe(nameof(CanOpenBackpack));
            Unsubscribe(nameof(CanBePenalized));
            Unsubscribe(nameof(OnBaseRepair));
            Unsubscribe(nameof(OnClanMemberJoined));
            Unsubscribe(nameof(STCanGainXP));
            Unsubscribe(nameof(OnNeverWear));

            Unsubscribe(nameof(OnInterferenceOthersUpdate));
            Unsubscribe(nameof(OnInterferenceUpdate));
            Unsubscribe(nameof(OnMlrsFire));
            Unsubscribe(nameof(OnTeamAcceptInvite));
            Unsubscribe(nameof(OnButtonPress));
            Unsubscribe(nameof(OnElevatorButtonPress));
            Unsubscribe(nameof(OnSamSiteTarget));
            Unsubscribe(nameof(OnPlayerCommand));
            Unsubscribe(nameof(OnServerCommand));
            Unsubscribe(nameof(OnTrapTrigger));
            Unsubscribe(nameof(OnEntityBuilt));
            Unsubscribe(nameof(OnStructureUpgrade));
            Unsubscribe(nameof(OnEntityGroundMissing));
            Unsubscribe(nameof(OnEntityKill));
            Unsubscribe(nameof(OnLootEntityEnd));
            Unsubscribe(nameof(OnExplosiveFuseSet));
            Unsubscribe(nameof(OnEntityDeath));
            Unsubscribe(nameof(CanPickupEntity));
            Unsubscribe(nameof(OnPlayerLand));
            Unsubscribe(nameof(OnPlayerDeath));
            Unsubscribe(nameof(OnPlayerDropActiveItem));
            Unsubscribe(nameof(OnEntityEnter));
            Unsubscribe(nameof(OnNpcDuck));
            Unsubscribe(nameof(OnNpcDestinationSet));
            Unsubscribe(nameof(OnCupboardAuthorize));
            Unsubscribe(nameof(OnActiveItemChanged));
            Unsubscribe(nameof(OnFireBallSpread));
            Unsubscribe(nameof(OnFireBallDamage));
            Unsubscribe(nameof(OnCupboardProtectionCalculated));
        }

        private void OnMapMarkerAdded(BasePlayer player, ProtoBuf.MapNote note)
        {
            if (player.IsAlive() && player.HasPermission("raidablebases.mapteleport"))
            {
                float y = GetSpawnHeight(note.worldPosition);
                if (player.IsFlying) y = Mathf.Max(y, player.transform.position.y);
                player.Teleport(note.worldPosition.WithY(y));
                if (config.Settings.DestroyMarker)
                {
                    player.State.pointsOfInterest?.Remove(note);
                    note.Dispose();
                    player.DirtyPlayerState();
                    player.SendMarkersToClient();
                }
            }
        }

        private void OnNewSave(string filename)
        {
            wiped = true;
            temporaryData.NetworkIdentifiers.Clear();
        }

        private void Loaded()
        {
            if (InstallationError)
            {
                return;
            }
            LoadTemporaryData();
        }

        private void Init()
        {
            if (InstallationError)
            {
                return;
            }
            Automated = new(this, config.Settings.Maintained.Enabled, config.Settings.Schedule.Enabled);
            UndoComparer = new() { DeployableItems = DeployableItems };
            SpawnsController.Instance = this;
            UI = new() { Instance = this };
            UI.LoadOffsetData();
            IsUnloading = false;
            Buildings = new();
            GridController.Instance = this;
            IsSpawnerBusy = true;
            RegisterPermissions();
            buyableEnabled = config.Settings.Buyable.Max > 0;
            Unsubscribe(nameof(OnMapMarkerAdded));
            Unsubscribe(nameof(OnPlayerSleepEnded));
            Unsubscribe(nameof(CanBuild));
            Unsubscribe(nameof(CanLootEntity));
            Unsubscribe(nameof(OnEntitySpawned));
            UnsubscribeHooks();
            SpawnsController.Initialize();
            Queues = new(this);
        }

        private void OnServerShutdown()
        {
            IsShuttingDown = true;
            IsUnloading = true;
        }

        private void Unload()
        {
            if (InstallationError)
            {
                return;
            }
            IsUnloading = true;
            IsSpawnerBusy = true;
            SaveData();
            TryInvokeMethod(StopLoadCoroutines);
            TryInvokeMethod(() => SetOnSun(false));
            TryInvokeMethod(StartEntityCleanup);
            DestroyProtection();
        }

        private void OnServerInitialized(bool isStartup)
        {
            if (InstallationError)
            {
                return;
            }
            if (!temporaryData.IsLoaded)
            {
                LoadTemporaryData();
            }
            timer.Once(120f, Automated.StartDespawnTemporaryEntities);
            SpawnsController.instruction0 = CoroutineEx.waitForSeconds(0.0025f);
            AddCovalenceCommand(config.Settings.BuyCommand, nameof(CommandBuyRaid));
            AddCovalenceCommand(config.Settings.EventCommand, nameof(CommandRaidBase));
            AddCovalenceCommand(config.Settings.HunterCommand, nameof(CommandRaidHunter));
            AddCovalenceCommand(config.Settings.ConsoleCommand, nameof(CommandRaidBase));
            AddCovalenceCommand("rb.reloadconfig", nameof(CommandReloadConfig));
            AddCovalenceCommand("rb.reloadprofiles", nameof(CommandReloadConfig));
            AddCovalenceCommand("rb.reloadtables", nameof(CommandReloadConfig));
            AddCovalenceCommand("rb.config", nameof(CommandConfig), "raidablebases.config");
            AddCovalenceCommand("rb.populate", nameof(CommandPopulate), "raidablebases.config");
            AddCovalenceCommand("rb.toggle", nameof(CommandToggle), "raidablebases.config");
            LoadPlayerData();
            CheckForWipe();
            InitializeSkins();
            Initialize();
            OceanLevel = WaterSystem.OceanLevel;
            Queues.StartCoroutine();
            timer.Repeat(Mathf.Clamp(config.EventMessages.Interval, 1f, 60f), 0, CheckNotifications);
            timer.Repeat(30f, 0, UpdateAllMarkers);
            timer.Repeat(60f, 0, CheckOceanLevel);
            timer.Repeat(300f, 0, SaveData);
            setupCopyPasteObstructionRadius = ServerMgr.Instance.StartCoroutine(SetupCopyPasteObstructionRadius());
        }

        private void OnSunrise()
        {
            Raids.ForEach(raid => raid.ToggleLights());
        }

        private void OnSunset()
        {
            Raids.ForEach(raid => raid.ToggleLights());
        }

        private object OnLifeSupportSavingLife(BasePlayer player)
        {
            return EventTerritory(player.transform.position) || HasPVPDelay(player.userID) ? true : (object)null;
        }

        private object CanDoubleJump(BasePlayer player)
        {
            return EventTerritory(player.transform.position) || HasPVPDelay(player.userID) ? true : (object)null;
        }

        private object OnRestoreUponDeath(BasePlayer player)
        {
            return Get(player.transform.position, out var raid) && ((config.Settings.Management.BlockRestorePVE && !raid.AllowPVP) || (config.Settings.Management.BlockRestorePVP && raid.AllowPVP)) ? true : (object)null;
        }

        private object OnCustomLootNPC(NetworkableId networkableId)
        {
            return Has(networkableId) ? true : (object)null;
        }

        private object OnNpcKits(ulong targetId)
        {
            return Has(targetId) ? true : (object)null;
        }

        private object OnReflectDamage(BasePlayer victim, BasePlayer attacker)
        {
            return PlayerInEvent(victim) || PlayerInEvent(attacker) ? true : (object)null;
        }

        private object CanBGrade(BasePlayer player, int playerGrade, BuildingBlock block, Planner planner)
        {
            return PlayerInEvent(player) ? 0 : (object)null;
        }

        private object canRemove(BasePlayer player)
        {
            return !player.IsFlying && EventTerritory(player.transform.position) ? mx("CannotRemove", player.UserIDString) : null;
        }

        private object canTeleport(BasePlayer player)
        {
            return !player.IsFlying && (EventTerritory(player.transform.position) || PvpDelay.ContainsKey(player.userID)) ? m("CannotTeleport", player.UserIDString) : null;
        }

        private object CanTeleport(BasePlayer player, Vector3 to)
        {
            return !player.IsFlying && (EventTerritory(to) || EventTerritory(player.transform.position) || PvpDelay.ContainsKey(player.userID)) ? m("CannotTeleport", player.UserIDString) : null;
        }

        private object OnBaseRepair(BuildingManager.Building building, BasePlayer player)
        {
            return EventTerritory(player.transform.position) ? false : (object)null;
        }

        private object STCanGainXP(BasePlayer player, double amount, string pluginName)
        {
            if (pluginName == Name)
            {
                foreach (var raid in Raids)
                {
                    if (raid.GetRaiders().Contains(player))
                    {
                        return true;
                    }
                }
            }
            return null;
        }

        private object OnRaidingUltimateTargetAcquire(BasePlayer player, Vector3 targetPoint)
        {
            return !Get(targetPoint, out var raid) || raid.Options.MLRS ? (object)null : true;
        }

        private void OnClanMemberJoined(ulong userid, string tag)
        {
            var player = BasePlayer.FindByID(userid);
            if (!player) return;
            var raid = Raids.FirstOrDefault(other => other.ownerId == player.userID && other.IsAllyHogging(player));
            if (raid == null) return;
            Clans?.Call("cmdChatClan", player, "clan", new string[1] { "leave" });
        }

        private object OnTeamAcceptInvite(RelationshipManager.PlayerTeam playerTeam, BasePlayer player)
        {
            if (!player) return null;
            var raid = Raids.FirstOrDefault(other => other.ownerId == player.userID && other.IsAllyHogging(player));
            if (raid == null) return null;
            playerTeam.RejectInvite(player);
            return true;
        }

        private object OnNeverWear(Item item, float amount)
        {
            var player = item.parentItem?.GetOwnerPlayer() ?? item.GetOwnerPlayer();

            if (!player || !player.IsHuman() || player.HasPermission("raidablebases.durabilitybypass"))
            {
                return null;
            }

            if (!Get(player.transform.position, out var raid) || !raid.Options.EnforceDurability)
            {
                return null;
            }

            return amount;
        }

        private void OnDeleteDynamicPVP(string zoneId, string eventName)
        {
            SpawnsController.managedZones.RemoveAll(zone => zone.ZoneId == zoneId);
        }

        private void OnLoseCondition(Item item, ref float amount)
        {
            if (item == null)
            {
                return;
            }

            var player = item.parentItem?.GetOwnerPlayer() ?? item.GetOwnerPlayer();

            if (!player || !player.userID.IsSteamId() || player.HasPermission("raidablebases.durabilitybypass"))
            {
                return;
            }

            if (!Get(player.transform.position, out var raid) || !raid.Options.EnforceDurability)
            {
                return;
            }

            var uid = item.uid;

            if (!raid.conditions.TryGetValue(uid, out var condition))
            {
                raid.conditions[uid] = condition = item.condition;
            }

            float _amount = amount;

            NextTick(() =>
            {
                if (raid == null)
                {
                    return;
                }

                if (IsKilled(item))
                {
                    raid.conditions.Remove(uid);
                    return;
                }

                item.condition = condition - _amount;

                if (item.condition <= 0f && item.condition < condition)
                {
                    item.OnBroken();
                    raid.conditions.Remove(uid);
                }
                else raid.conditions[uid] = item.condition;
            });
        }

        private object OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade, ulong skin)
        {
            if (!Get(block.transform.position, out var raid))
            {
                return null;
            }

            if (block.OwnerID == 0uL && Has(block))
            {
                return config.Settings.Management.AllowUpgrade ? (object)null : true;
            }

            return grade switch
            {
                BuildingGrade.Enum.Wood when raid.Options.BuildingRestrictions.Wooden => true,
                BuildingGrade.Enum.Stone when raid.Options.BuildingRestrictions.Stone => true,
                BuildingGrade.Enum.Metal when raid.Options.BuildingRestrictions.Metal => true,
                BuildingGrade.Enum.TopTier when raid.Options.BuildingRestrictions.HQM => true,
                _ => null
            };
        }

        private void OnEntityBuilt(Planner planner, GameObject go)
        {
            if (go == null)
            {
                return;
            }

            var e = go.ToBaseEntity();

            if (!e || !Get(e.transform.position, out var raid))
            {
                return;
            }

            var player = planner.GetOwnerPlayer();

            if (!player || IsPocketDimensions(player, e))
            {
                return;
            }

            if (raid.Options.BuildingRestrictions.Any() && e is BuildingBlock block)
            {
                var grade = block.grade;

                block.Invoke(() =>
                {
                    if (raid == null || block.IsDestroyed)
                    {
                        return;
                    }

                    if (block.grade == grade || OnStructureUpgrade(block, player, block.grade, block.skinID) == null)
                    {
                        AddPlayerEntity(e, raid);
                        return;
                    }

                    foreach (var ia in block.BuildCost())
                    {
                        player.GiveItem(ItemManager.Create(ia.itemDef, (int)ia.amount));
                    }

                    block.SafelyKill();
                }, 0.1f);
            }
            else if (!InRange(raid.Location, player.transform.position, raid.ProtectionRadius))
            {
                e.Invoke(e.SafelyKill, 0.1f);
            }
            else if ((e.ShortPrefabName == "foundation.triangle" || e.ShortPrefabName == "foundation") && raid.NearFoundation(e.transform.position))
            {
                Message(player, "TooCloseToABuilding");
                e.Invoke(e.SafelyKill, 0.1f);
            }
            else AddPlayerEntity(e, raid);
        }

        private void AddPlayerEntity(BaseEntity e, RaidableBase raid)
        {
            raid.BuiltList[e] = e.transform.position;
            raid.SetupEntity(e, false);

            if (e.PrefabName.Contains("assets/prefabs/deployable/"))
            {
                if (config.Settings.Management.KeepDeployables)
                {
                    raid.DestroyGroundCheck(e);
                }
                else
                {
                    raid.AddEntity(e);
                }
            }
            else if (!config.Settings.Management.KeepStructures)
            {
                raid.AddEntity(e);
            }
        }

        private void OnElevatorButtonPress(ElevatorLift e, BasePlayer player, Elevator.Direction Direction, bool FullTravel)
        {
            if (_elevators.TryGetValue(e.GetParentEntity().net.ID, out var bmgELEVATOR) && bmgELEVATOR.HasCardPermission(player) && bmgELEVATOR.HasBuildingPermission(player))
            {
                bmgELEVATOR.GoToFloor(Direction, FullTravel);
            }
        }

        private void OnButtonPress(PressButton button, BasePlayer player)
        {
            if (button.OwnerID == 0 && Has(button))
            {
                foreach (var e in _elevators)
                {
                    if (e.Value.ValidPosition && Vector3Ex.Distance2D(button.transform.position, e.Value.ServerPosition) <= 3f)
                    {
                        e.Value.GoToFloor(Elevator.Direction.Up, false, Mathf.CeilToInt(button.transform.position.y));
                    }
                }
            }
        }

        private bool IsProtectedScientist(BasePlayer player, TriggerBase trigger)
        {
            if (!player.GetType().Name.Contains("CustomScientist", CompareOptions.OrdinalIgnoreCase))
            {
                return false;
            }
            if (!(player is NPCPlayer npc))
            {
                return false;
            }
            var raid = Raids.FirstOrDefault(y => InRange(y.Location, npc.transform.position, y.ProtectionRadius));
            if (raid == null || !raid.Options.NPC.IgnorePlayerTrapsTurrets || !InRange(raid.Location, npc.spawnPos, raid.ProtectionRadius))
            {
                return false;
            }
            var entity = trigger.GetComponentInParent<BaseEntity>();
            if (entity is AutoTurret turret && turret.OwnerID == 0)
            {
                turret.authorizedPlayers.Add(new()
                {
                    ShouldPool = false,
                    userid = player.userID,
                    username = player.userID.ToString(),
                });
            }
            if (entity is StorageContainer && !raid.priv.IsKilled())
            {
                raid.priv.authorizedPlayers.Add(new()
                {
                    ShouldPool = false,
                    userid = player.userID,
                    username = player.userID.ToString(),
                });
            }
            return true;
        }

        private object OnNpcDuck(HumanoidNPC npc) => npc && Has(npc.userID) ? true : (object)null;

        private object OnNpcDestinationSet(HumanoidNPC npc, Vector3 newDestination)
        {
            if (!npc || !npc.NavAgent || !npc.NavAgent.enabled || !npc.NavAgent.isOnNavMesh)
            {
                return null;
            }

            if (!HumanoidBrains.TryGetValue(npc.userID, out var brain) || brain.CanRoam(newDestination))
            {
                return null;
            }

            return true;
        }

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (player.IsHuman() && Get(player.transform.position, out var raid))
            {
                raid.StopUsingWeapon(player);
            }
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            player.Invoke(() =>
            {
                if (player.IsDestroyed || !player.IsHuman())
                {
                    return;
                }

                if (PvpDelay.Remove(player.userID, out var ds))
                {
                    ds.Destroy();
                }

                if (config.UI.Lockout.Enabled)
                {
                    UI.UpdateUi(player, UiType.Lockout);
                }

                if (config.UI.Status.Enabled)
                {
                    UI.UpdateUi(player, UiType.Status);
                }

                if (!Get(player.transform.position, out var raid, 5f, false))
                {
                    return;
                }

                raid.intruders.Remove(player.userID);

                if (!config.Settings.Management.AllowTeleport && !raid.TeleportExceptions.Remove(player.userID) && !raid.CanBypass(player) && raid.Type != RaidableType.None)
                {
                    raid.RemovePlayer(player, raid.Location, raid.ProtectionRadius, raid.Type);
                }
            }, 0.015f);
        }

        private object OnPlayerLand(BasePlayer player, float amount)
        {
            return !Get(player.transform.position, out var raid) || !raid.IsDespawning ? (object)null : true;
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo hitInfo)
        {
            if (!Get(player, hitInfo, out var raid))
            {
                return;
            }

            if (!player.IsHuman())
            {
                if (!HumanoidBrains.TryGetValue(player.userID, out var brain))
                {
                    return;
                }

                brain.DisableShouldThink();

                if (config.Settings.Management.UseOwners && hitInfo != null && hitInfo.Initiator is BasePlayer attacker && raid.AddLooter(attacker))
                {
                    raid.TrySetOwner(attacker, player, hitInfo);
                }

                if (!brain.keepInventory)
                {
                    player.inventory.SafelyStrip();
                }

                raid.CheckDespawn();
            }
            else
            {
                if (CanDropPlayerBackpack(player, raid))
                {
                    Backpacks?.Call("API_DropBackpack", player);
                }

                raid.OnPlayerExit(player);
            }
        }

        private object OnPlayerDropActiveItem(BasePlayer player, Item item)
        {
            return EventTerritory(player.transform.position) ? true : (object)null;
        }

        private object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsKilled() && !player.HasPermission("raidablebases.allow.commands") && Get(player.transform.position, out var raid))
            {
                foreach (var value in raid.BlacklistedCommands)
                {
                    if (command.EndsWith(value, StringComparison.OrdinalIgnoreCase))
                    {
                        Message(player, "CommandNotAllowed");
                        return true;
                    }
                }
            }
            return null;
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            return OnPlayerCommand(arg.Player(), arg.cmd.FullName, arg.Args);
        }

        private object OnExplosiveFuseSet(TimedExplosive explosive, float fuseLength)
        {
            if (!(explosive.creatorEntity is HumanoidNPC npc) || !HumanoidBrains.TryGetValue(npc.userID, out var brain) || !brain.Settings.PlayCatch || !brain.ValidTarget)
            {
                return null;
            }

            return brain.ServerPosition.Distance(brain.AttackPosition) * 0.1275f;
        }

        private void OnEntityDeath(BuildingPrivlidge priv, HitInfo hitInfo)
        {
            if (!Get(priv, out var raid) || raid.priv != priv)
            {
                return;
            }

            if (!raid.IsDespawning && config.Settings.Management.AllowCupboardLoot)
            {
                DropOrRemoveItems(priv, raid, true, false);
            }

            if (raid.Options.RequiresCupboardAccess)
            {
                OnCupboardAuthorize(priv, null);
            }

            raid.OnBuildingPrivilegeDestroyed();
        }

        private void OnEntityKill(StorageContainer container)
        {
            if (container is BuildingPrivlidge priv)
            {
                OnEntityDeath(priv, null);
            }
            if (container != null)
            {
                EntityHandler(container, null);
            }
        }

        private void OnEntityDeath(StorageContainer container, HitInfo hitInfo) => EntityHandler(container, hitInfo);

        //private void OnEntityKill(BuildingBlock block) => OnEntityDeath(block, null); // ent kill testing

        private void OnEntityDeath(StabilityEntity entity, HitInfo hitInfo)
        {
            if (!Get(entity.transform.position, out var raid) || raid.IsDespawning || hitInfo == null || !(hitInfo.Initiator is BasePlayer player))
            {
                return;
            }

            if (raid.AddLooter(player))
            {
                raid.AddMember(player.userID);

                raid.TrySetOwner(player, entity, hitInfo);
            }

            if (raid.CanSetPVPDelay(player))
            {
                raid.TrySetPVPDelay(player, false, "AttackableFromOutside");
            }

            raid.CheckDespawn();

            if (raid.IsDamaged)
            {
                return;
            }

            if (entity is BuildingBlock || entity is Door)
            {
                raid.IsDamaged = true;
            }
        }

        private object OnEntityGroundMissing(StorageContainer container)
        {
            return Get(container, out var raid) && !raid.CanHurtBox(container) ? true : (object)null;
        }

        //private void OnEntityKill(IOEntity io) => OnEntityDeath(io, null);

        private void OnEntityDeath(IOEntity io, HitInfo hitInfo)
        {
            if (config.Settings.Management.DropLoot.AutoTurret && !io.IsKilled() && io is AutoTurret turret && Get(io, out var raid1) && raid1.turrets.Remove(turret) && !raid1.IsDespawning && !raid1.IsLoading)
            {
                DropLoot(io, turret.inventory, raid1.Options.BuoyantBox);
            }
            else if (config.Settings.Management.DropLoot.SamSite && !io.IsKilled() && io is SamSite samsite && Get(io, out var raid2) && raid2.samsites.Remove(samsite) && !raid2.IsDespawning && !raid2.IsLoading)
            {
                DropLoot(io, samsite.inventory, raid2.Options.BuoyantBox);
            }
        }

        private void EntityHandler(StorageContainer container, HitInfo hitInfo)
        {
            if (!Get(container, out var raid) || raid.IsDespawning || raid.IsLoading)
            {
                return;
            }

            DropOrRemoveItems(container, raid, false, false);

            raid._containers.Remove(container);

            if (!raid.IsAnyLooted && hitInfo != null)
            {
                raid.IsAnyLooted = hitInfo.Initiator is BasePlayer || hitInfo.damageTypes.Has(DamageType.Heat);
            }

            if (IsLootingWeapon(hitInfo) && raid.GetInitiatorPlayer(hitInfo, container, out var attacker))
            {
                raid.AddLooter(attacker);
            }

            if (raid.IsOpened && (IsBox(container, true) || container is BuildingPrivlidge))
            {
                raid.TryToEnd();
            }

            if (!Raids.Exists(x => x._containers.Count > 0))
            {
                Unsubscribe(nameof(OnEntityKill));
                Unsubscribe(nameof(OnEntityGroundMissing));
            }
        }

        private static bool IsLootingWeapon(HitInfo hitInfo)
        {
            if (hitInfo == null || hitInfo.damageTypes == null)
            {
                return false;
            }

            return hitInfo.damageTypes.Has(DamageType.Explosion) || hitInfo.damageTypes.Has(DamageType.Heat) || hitInfo.damageTypes.IsMeleeType();
        }

        private void OnCupboardAuthorize(BuildingPrivlidge priv, BasePlayer player)
        {
            bool isHookNeeded = false;

            foreach (var raid in Raids)
            {
                if (!raid.IsAuthed && raid.Options.RequiresCupboardAccess && raid.priv == priv)
                {
                    raid.IsAuthed = true;

                    if (config.EventMessages.AnnounceRaidUnlock)
                    {
                        foreach (var target in BasePlayer.activePlayerList)
                        {
                            raid.QueueNotification(target, "OnRaidFinished", FormatGridReference(target, raid.Location));
                        }
                    }
                }

                if (!raid.IsAuthed)
                {
                    isHookNeeded = true;
                }
            }

            if (!isHookNeeded)
            {
                Unsubscribe(nameof(OnCupboardAuthorize));
            }
        }

        private object CanPickupEntity(BasePlayer player, BaseEntity entity)
        {
            if (!Get(entity, out var raid))
            {
                return null;
            }

            if (player.IsReallyValid())
            {
                if (entity is BaseLadder || player.userID == entity.OwnerID)
                {
                    return true;
                }
                if (!raid.AddLooter(player))
                {
                    return raid.CanBypass(player);
                }
            }

            if (raid.IsPickupBlacklisted(entity.ShortPrefabName))
            {
                return false;
            }

            if (!raid.Options.AllowPickup && entity.OwnerID == 0)
            {
                return false;
            }

            if (entity.OwnerID == 0uL && TryRemoveItems(entity))
            {
                ItemManager.DoRemoves();
            }

            return null;
        }

        private void OnFireBallSpread(FireBall fire, BaseEntity spread)
        {
            if (!spread.IsKilled() && Get(spread.transform.position, out var raid) && !raid.Options.Eco.CanSpread(spread))
            {
                NextTick(spread.SafelyKill);
            }
        }

        private void OnFireBallDamage(FireBall fire, BaseCombatEntity target, HitInfo hitInfo)
        {
            if (hitInfo != null && !fire.IsKilled() && EventTerritory(fire.transform.position))
            {
                hitInfo.Initiator ??= fire.creatorEntity;
            }
        }

        private object CanMlrsTargetLocation(MLRS mlrs, BasePlayer player)
        {
            return Get(mlrs.TrueHitPos, out var raid, 25f) ? raid.Options.MLRS : (object)null;
        }

        private object OnMlrsFire(MLRS mlrs, BasePlayer player)
        {
            if (!Get(mlrs.TrueHitPos, out var raid, 25f) || raid.Options.MLRS) return null;
            Message(player, "MLRS Target Denied");
            return true;
        }

        private object OnInterferenceOthersUpdate(AutoTurret turret)
        {
            return OnInterferenceUpdate(turret);
        }

        private object OnInterferenceUpdate(AutoTurret turret)
        {
            return Has(turret) ? true : (object)null;
        }

        private void OnSaveLoad(Dictionary<BaseEntity, ProtoBuf.Entity> dictionary)
        {
            foreach (var (entity, proto) in dictionary)
            {
                try
                {
                    if (temporaryData.NetworkIdentifiers.Contains(proto.baseNetworkable.uid.Value) && proto.ownerInfo.steamid == 0uL)
                    {
                        Automated.Enbatch(new(entity, proto.baseNetworkable.uid));
                    }
                }
                catch { }
            }
        }

        private void OnEntitySpawned(MLRSRocket rocket)
        {
            if (rocket.IsKilled()) return;
            var systems = FindEntitiesOfType<MLRS>(rocket.transform.position, 15f);
            if (systems.Count == 0) return;
            if (!Get(systems[0].TrueHitPos, out var raid)) return;
            if (!(systems[0].rocketOwnerRef.Get(true) is BasePlayer owner)) return;
            rocket.creatorEntity = raid.Options.MLRS ? owner : null;
            rocket.OwnerID = raid.Options.MLRS ? owner.userID : 0uL;
        }

        private void OnEntitySpawned(FireBall fire)
        {
            if (fire.IsKilled() || !Get(fire.transform.position, out var raid))
            {
                return;
            }
            if (raid.Options.Eco.Enabled && !raid.Options.Eco.CanSpread(fire))
            {
                NextTick(fire.SafelyKill);
            }
            else if (config.Settings.Management.PreventFireFromSpreading && fire.ShortPrefabName == "flamethrower_fireball" && fire.creatorEntity is BasePlayer player && !player.userID.IsSteamId())
            {
                NextTick(fire.SafelyKill);
            }
        }

        private void OnEntitySpawned(DroppedItemContainer backpack)
        {
            if (backpack.IsKilled() || !Get(backpack.transform.position, out var raid))
            {
                return;
            }
            if (backpack.ShortPrefabName == "item_drop" || backpack.ShortPrefabName == "item_drop_buoyant")
            {
                raid.AddNearTime = 1800f;
                return;
            }
            NextTick(() =>
            {
                if (IsUnloading || backpack.IsKilled())
                {
                    return;
                }
                if (raid != null && raid.CanDropBackpack(backpack.playerSteamID))
                {
                    backpack.playerSteamID = 0;
                }
                else if (Get(backpack.playerSteamID, out DelaySettings ds) && ds.raid.CanDropBackpack(backpack.playerSteamID))
                {
                    backpack.playerSteamID = 0;
                }
            });
        }

        private void OnEntitySpawned(BaseLock entity)
        {
            if (entity.IsKilled() || !Get(entity.transform.position, out var raid) || raid.IsLoading)
            {
                return;
            }
            if (entity.GetParentEntity() is StorageContainer parent && raid._containers.Contains(parent))
            {
                NextTick(entity.SafelyKill);
            }
        }

        private void OnEntitySpawned(PlayerCorpse corpse)
        {
            if (corpse.IsKilled() || !Get(corpse, out var raid))
            {
                return;
            }

            if (corpse.playerSteamID.IsSteamId())
            {
                var playerSteamID = corpse.playerSteamID;

                if (raid.Options.EjectBackpacks && !playerSteamID.HasPermission("reviveplayer.use"))
                {
                    if (Interface.CallHook("OnRaidablePlayerCorpseCreate", new object[] { corpse, raid.Location, raid.AllowPVP, (int)raid.Options.Mode, raid.GetOwner(), raid.GetRaiders(), raid.BaseName }) != null)
                    {
                        return;
                    }

                    if (corpse.containers.IsNullOrEmpty())
                    {
                        goto done;
                    }

                    var container = GameManager.server.CreateEntity(StringPool.Get(1519640547), corpse.transform.position) as DroppedItemContainer;
                    container.maxItemCount = 48;
                    container.lootPanelName = "generic_resizable";
                    container.playerName = corpse.playerName;
                    container.playerSteamID = corpse.playerSteamID;
                    container.Spawn();

                    if (container.IsKilled())
                    {
                        goto done;
                    }

                    container.TakeFrom(corpse.containers);
                    corpse.SafelyKill();

                    var player = RustCore.FindPlayerById(playerSteamID);
                    var data = raid.AddBackpack(container, player);
                    bool canEjectBackpack = Interface.CallHook("OnRaidableBaseBackpackEject", new object[] { container, playerSteamID, raid.Location, raid.AllowPVP, (int)raid.Options.Mode, raid.GetOwner(), raid.GetRaiders(), raid.BaseName }) == null;

                    if (canEjectBackpack && raid.EjectBackpack(container.net.ID, data, false))
                    {
                        raid.backpacks.Remove(container.net.ID);
                    }

                    if (config.Settings.Management.PlayersLootableInPVE && !raid.AllowPVP || config.Settings.Management.PlayersLootableInPVP && raid.AllowPVP)
                    {
                        container.playerSteamID = 0;
                    }

                    return;
                }

            done:

                if (config.Settings.Management.PlayersLootableInPVE && !raid.AllowPVP || config.Settings.Management.PlayersLootableInPVP && raid.AllowPVP)
                {
                    corpse.playerSteamID = 0;
                }
            }
            else
            {
                raid.npcs.RemoveAll(npc => npc.IsKilled() || npc.userID == corpse.playerSteamID);

                if (HumanoidBrains.TryGetValue(corpse.playerSteamID, out var brain))
                {
                    raid.SpawnDrops(corpse.containers, brain.isMurderer ? raid.Options.NPC.MurdererDrops : raid.Options.NPC.ScientistDrops);

                    if (!brain.keepInventory)
                    {
                        corpse.Invoke(corpse.SafelyKill, 30f);
                    }

                    if (raid.Options.RespawnRateMax > 0f)
                    {
                        raid.TryRespawnNpc(brain.isMurderer);
                    }
                    else if (!AnyNpcs())
                    {
                        Unsubscribe(nameof(OnNpcDestinationSet));
                    }

                    corpse.playerName = brain.displayName;
                    brain.DisableShouldThink();
                    UnityEngine.Object.Destroy(brain);
                }
            }
        }

        private object CanBuild(BasePlayer player, Vector3 buildPos)
        {
            foreach (var profile in Buildings.Profiles.Values)
            {
                if (profile.Options.CustomSpawns.PreventBuilding && !profile.Spawns.CanBuild(player, buildPos, profile.Options.ProtectionRadius(RaidableType.None)))
                {
                    Message(player, "Building is blocked for spawns!");
                    return false;
                }
            }
            return null;
        }

        private object CanBuild(Planner planner, Construction construction, Construction.Target target)
        {
            var buildPos = target.entity && target.entity.transform && target.socket ? target.GetWorldPosition() : target.position;

            if (!Get(buildPos, out var raid))
            {
                return CanBuild(target.player, buildPos);
            }

            if (!raid.Options.AllowBuildingPriviledges && construction.prefabID == 2476970476)
            {
                Message(target.player, "Cupboards are blocked!");
                return false;
            }
            else if (config.Settings.Management.AllowLadders && construction.prefabID == 2150203378)
            {
                if (!raid.Options.RequiresCupboardAccessLadders || raid.CanBuild(target.player))
                {
                    if (raid.raiders.TryGetValue(target.player.userID, out var ri) && ri.Input != null)
                    {
                        ri.Input.Restart();
                        ri.Input.TryPlace(ConstructionType.Ladder);
                    }
                }
                else
                {
                    Message(target.player, "Ladders are blocked!");
                    return false;
                }
            }
            else if (construction.fullName.Contains("/barricades/barricade."))
            {
                if (raid.Options.Barricades)
                {
                    if (raid.raiders.TryGetValue(target.player.userID, out var ri) && ri.Input != null)
                    {
                        ri.Input.Restart();
                        ri.Input.TryPlace(ConstructionType.Barricade);
                    }
                }
                else
                {
                    Message(target.player, "Barricades are blocked!");
                    return false;
                }
            }
            else if (!config.Settings.Management.AllowBuilding && !config.Settings.Management.AllowedBuildingBlocks.Contains(GetFileNameWithoutExtension(construction.fullName)))
            {
                Message(target.player, "Building is blocked!");
                return false;
            }

            return null;
        }

        [HookMethod("AddLootToDifficultyProfile")]
        public bool AddLootToDifficultyProfile(int mode, List<object[]> lootObjects)
        {
            if (lootObjects == null || lootObjects.Count < 1)
            {
                return false;
            }

            if (!Buildings.DifficultyLootLists.TryGetValue((RaidableMode)mode, out var lootList))
            {
                return false;
            }

            bool success = false;
            foreach (var obj in lootObjects)
            {
                if (!(obj[0] is string)) continue;
                string shortname = obj[0] as string;
                int amountMin = obj.Length > 1 && (obj[1] is int) ? (int)obj[1] : 1;
                int amountMax = obj.Length > 2 && (obj[2] is int) ? (int)obj[2] : 1;
                ulong skin = obj.Length > 3 && (obj[3] is ulong) ? (ulong)obj[3] : 0;
                float probability = obj.Length > 4 && (obj[4] is float) ? (float)obj[4] : 1.0f;
                string displayName = obj.Length > 5 && (obj[5] is string) ? obj[5] as string : null;
                int stackSize = obj.Length > 6 && (obj[6] is int) ? (int)obj[6] : -1;
                string text = obj.Length > 7 && (obj[7] is string) ? obj[7] as string : null;

                lootList.Add(new(shortname, amountMin, amountMax, skin, false, probability, stackSize, displayName, text));
                success = true;
            }

            return success;
        }

        private void OnLootEntityEnd(BasePlayer player, StorageContainer container)
        {
            if (!player || player.limitNetworking || container?.inventory == null || container.OwnerID.IsSteamId() || !Get(container, out var raid))
            {
                return;
            }

            if (player.userID.IsSteamId())
            {
                raid.IsAnyLooted = true;
            }

            if (raid.Options.DropTimeAfterLooting <= 0 || (raid.Options.DropOnlyBoxesAndPrivileges && !IsBox(container, true) && !(container is BuildingPrivlidge)))
            {
                raid.TryToEnd();
                return;
            }

            if (container.inventory.IsEmpty() && IsBox(container, false))
            {
                container.Invoke(container.SafelyKill, 0.1f);
            }
            else container.Invoke(() => DropOrRemoveItems(container, raid, false, true), raid.Options.DropTimeAfterLooting);

            raid.TryToEnd();
        }

        private object CanLootDroppedItemContainer(BasePlayer player, BaseEntity entity) => entity switch
        {
            _ when entity.skinID != 14922524 || !entity.OwnerID.IsSteamId() || entity.OwnerID == player.userID => null,
            _ when RelationshipManager.ServerInstance.playerToTeam.TryGetValue(entity.OwnerID, out var team) && team.members.Contains(player.userID) => null,
            _ when Convert.ToBoolean(Clans?.Call("IsMemberOrAlly", entity.OwnerID.ToString(), player.UserIDString)) => null,
            _ when Convert.ToBoolean(Friends?.Call("AreFriends", entity.OwnerID.ToString(), player.UserIDString)) => null,
            _ => ((Func<object>)(() => { Message(player, "You do not own this loot!"); return true; }))(),
        };

        private object CanLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity.IsKilled()) return null;
            if (CanLootDroppedItemContainer(player, entity) != null) return true;
            return Get(entity.transform.position, out var raid) ? raid.CanLootEntityInternal(player, entity) : (object)null;
        }

        private object CanBePenalized(BasePlayer player)
        {
            return Get(player, null, out var raid) && (raid.AllowPVP && !raid.Options.PenalizePVP || !raid.AllowPVP && !raid.Options.PenalizePVE) ? false : (object)null;
        }

        private void CanOpenBackpack(BasePlayer looter, ulong backpackOwnerID)
        {
            if (!Get(looter.transform.position, out var raid))
            {
                return;
            }

            if (!raid.AllowPVP && !config.Settings.Management.BackpacksOpenPVE || raid.AllowPVP && !config.Settings.Management.BackpacksOpenPVP)
            {
                looter.Invoke(looter.EndLooting, 0.01f);
                Message(looter, "NotAllowed");
            }
        }

        private bool CanDropPlayerBackpack(BasePlayer player, RaidableBase raid)
        {
            if (Get(player.userID, out DelaySettings ds) && ds.raid.CanDropBackpack(player.userID))
            {
                return true;
            }

            return InRange(raid.Location, player.transform.position, raid.ProtectionRadius) && raid.CanDropBackpack(player.userID);
        }

        private object OnEntityEnter(TriggerBase trigger, Drone drone) => Has(trigger) ? true : (object)null;

        private object OnEntityEnter(TriggerBase trigger, BasePlayer player) => player switch
        {
            _ when player.IsKilled() || trigger == null => null,
            _ when Has(player.userID) && (Has(trigger) || (Get(player.userID, out HumanoidBrain brain) && brain.raid.Options.NPC.IgnorePlayerTrapsTurrets)) => true,
            _ when IsProtectedScientist(player, trigger) || ShouldIgnoreFlyingPlayer(player) => true,
            _ => IsPVE() || CanEntityBeTargeted(player, trigger.gameObject.ToBaseEntity()) is true or null ? (object)null : true,
        };

        private bool ShouldIgnoreFlyingPlayer(BasePlayer player) => config.Settings.Management.IgnoreFlying && player.IsFlying && EventTerritory(player.transform.position);

        private bool IsArmoredTrain(BaseEntity entity) => entity.OwnerID == 0uL && entity is AutoTurret && entity.GetParentEntity() is TrainCar;

        private bool IsSentryTargetingNpc(BasePlayer player, BaseEntity entity) => entity is NPCAutoTurret && !player.userID.IsSteamId();

        private bool IgnorePlayer(BasePlayer player, BaseEntity entity) => player.limitNetworking || IsPositionInSpace(entity.transform.position) || IsSentryTargetingNpc(player, entity) || IsArmoredTrain(entity);

        private bool IsPositionInSpace(Vector3 a) => Space != null && Convert.ToBoolean(Space?.Call("IsPositionInSpace", a));

        private object CanEntityBeTargeted(BasePlayer player, BaseEntity entity)
        {
            if (player.IsKilled() || entity.IsKilled() || IgnorePlayer(player, entity))
            {
                return null;
            }

            if (!Get(player.transform.position, out var raid) && !Get(entity.transform.position, out raid))
            {
                return null;
            }

            if (PvpDelay.ContainsKey(player.userID))
            {
                return true;
            }

            if (Has(player.userID))
            {
                if (entity.OwnerID == 0 && !entity.enableSaving && Sputnik != null)
                {
                    return null;
                }
                return entity.OwnerID.IsSteamId() ? !raid.Options.NPC.IgnorePlayerTrapsTurrets : !Has(entity);
            }

            if (player.IsHuman())
            {
                if (raid.AllowPVP)
                {
                    return raid.Entities.Contains(entity) || raid.BuiltList.ContainsKey(entity);
                }

                return raid.Entities.Contains(entity) && !raid.BuiltList.ContainsKey(entity);
            }

            return entity.OwnerID.IsSteamId() ? !raid.Options.NPC.IgnorePlayerTrapsTurrets : !raid.Options.NPC.IgnoreTrapsTurrets;
        }

        private object CanEntityBeTargeted(BaseEntity entity, SamSite ss)
        {
            if (entity.IsKilled() || ss.IsKilled() || !entity.IsValid())
            {
                return null;
            }
            if (!IsPositionInSpace(entity.transform.position) && Get(ss.transform.position, out var raid))
            {
                if (raid.IsLoading || MountEntities.ContainsKey(entity.net.ID))
                {
                    return false;
                }
                return (entity.transform.position - ss.transform.position).sqrMagnitude <= raid.Options.SamSite.Range * raid.Options.SamSite.Range;
            }

            return null;
        }

        private object OnSamSiteTarget(SamSite ss, BaseEntity entity)
        {
            if (entity.IsValid() && !IsPositionInSpace(entity.transform.position) && Get(ss.transform.position, out var raid))
            {
                if (raid.IsLoading || MountEntities.ContainsKey(entity.net.ID))
                {
                    return true;
                }
                return (entity.transform.position - ss.transform.position).sqrMagnitude <= raid.Options.SamSite.Range * raid.Options.SamSite.Range ? (object)null : true;
            }

            return null;
        }

        private object OnTrapTrigger(BaseTrap trap, GameObject go)
        {
            var player = go.GetComponent<BasePlayer>();
            var success = CanEntityTrapTrigger(trap, player);

            return success is bool val && !val ? true : (object)null;
        }

        private object CanEntityTrapTrigger(BaseTrap trap, BasePlayer player)
        {
            if (!player || player.limitNetworking)
            {
                return null;
            }

            if (Has(player.userID))
            {
                return false;
            }

            if (!Get(trap, out var raid))
            {
                return null;
            }

            if (raid.Options.RearmBearTraps && trap is BearTrap)
            {
                trap.Invoke(trap.Arm, 0.1f);
            }

            return true;
        }

        private void OnCupboardProtectionCalculated(BuildingPrivlidge priv, float cachedProtectedMinutes)
        {
            if (priv.OwnerID == 0 && Has(priv))
            {
                priv.cachedProtectedMinutes = 1500;
            }
        }

        private object CanEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (ShouldDespawnEntity(entity))
            {
                return true;
            }

            if (Raids.Count == 0 || hitInfo == null || hitInfo.damageTypes == null || entity.IsKilled() || entity.OwnerID == 1337422)
            {
                return null;
            }

            object success = entity is BasePlayer player ? HandlePlayerDamage(player, hitInfo) : HandleEntityDamage(entity, hitInfo);

            if (success is bool val && !val)
            {
                NullifyDamage(hitInfo);
                return false;
            }

            return success;
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo) => CanEntityTakeDamage(entity, hitInfo);

        private object HandlePlayerDamage(BasePlayer victim, HitInfo hitInfo)
        {
            if (!Get(victim, hitInfo, out var raid) || raid.IsDespawning || IsSpecialEvent(hitInfo))
            {
                return null;
            }

            if (hitInfo.WeaponPrefab is MLRSRocket)
            {
                return raid.Options.MLRS;
            }

            if (IsHelicopter(hitInfo, out var eventHeli))
            {
                return eventHeli ? (object)null : true;
            }

            var weapon = hitInfo.Initiator;

            if (victim.skinID == 14922524 && weapon != null && weapon.OwnerID == 0 && Has(weapon))
            {
                return false;
            }

            if (IsTrueDamage(weapon, raid.IsProtectedWeapon(weapon)))
            {
                return HandleTrueDamage(raid, hitInfo, weapon, victim);
            }

            if (raid.GetInitiatorPlayer(hitInfo, victim, out var attacker))
            {
                return HandleAttacker(attacker, victim, hitInfo, raid);
            }

            return victim.skinID == 14922524 ? false : (object)null;
        }

        private object HandleTrueDamage(RaidableBase raid, HitInfo hitInfo, BaseEntity weapon, BasePlayer victim)
        {
            if (victim is ScientistNPC && !Has(victim.userID))
            {
                return null;
            }

            if (weapon is AutoTurret)
            {
                (float min, float max) = victim.userID.IsSteamId() ? (raid.Options.AutoTurret.Min, raid.Options.AutoTurret.Max) : (raid.Options.AutoTurret.NpcMin, raid.Options.AutoTurret.NpcMax);

                hitInfo.damageTypes.Scale(DamageType.Bullet, UnityEngine.Random.Range(min, max));

                if (Has(victim.userID) && (raid.Options.NPC.IgnorePlayerTrapsTurrets || Has(weapon) && !raid.BuiltList.ContainsKey(weapon)))
                {
                    return false;
                }

                if (weapon.OwnerID.IsSteamId())
                {
                    if (InRange2D(weapon.transform.position, raid.Location, raid.ProtectionRadius))
                    {
                        return victim.IsHuman() ? raid.AllowPVP : true;
                    }

                    return raid.AllowPVP || !victim.IsHuman();
                }
            }

            return true;
        }

        private object HandleAttacker(BasePlayer attacker, BasePlayer victim, HitInfo hitInfo, RaidableBase raid)
        {
            if (Has(attacker.userID) && Has(victim.userID))
            {
                return false;
            }

            if (attacker.userID == victim.userID)
            {
                return raid.Options.AllowSelfDamage;
            }

            if (PvpDelay.ContainsKey(victim.userID))
            {
                if (EventTerritory(attacker.transform.position))
                {
                    return true;
                }

                if (config.Settings.Management.PVPDelayAnywhere && PvpDelay.ContainsKey(attacker.userID))
                {
                    return true;
                }
            }

            if (config.Settings.Management.PVPDelayDamageInside && PvpDelay.ContainsKey(attacker.userID) && InRange2D(raid.Location, victim.transform.position, raid.ProtectionRadius))
            {
                return true;
            }

            if (!victim.IsHuman() && attacker.IsHuman())
            {
                return HandleNpcVictim(raid, victim, attacker, hitInfo);
            }
            else if (victim.IsHuman() && attacker.IsHuman())
            {
                return HandlePVPDamage(raid, victim, attacker, hitInfo);
            }
            else if (Has(attacker.userID))
            {
                return HandleNpcAttacker(raid, victim, attacker, hitInfo);
            }

            return null;
        }

        private object HandleNpcVictim(RaidableBase raid, BasePlayer victim, BasePlayer attacker, HitInfo hitInfo)
        {
            if (!HumanoidBrains.TryGetValue(victim.userID, out var brain))
            {
                return true;
            }

            if (config.Settings.Management.BlockMounts && attacker.GetMounted())
            {
                return false;
            }

            if (CanBlockOutsideDamage(raid, attacker, raid.Options.NPC.BlockOutsideDamageToNpcsInside))
            {
                return false;
            }

            var e = attacker.HasParent() ? attacker.GetParentEntity() : null;

            if (e is BaseHelicopter || e is HotAirBalloon)
            {
                return false;
            }

            if (!raid.Options.NPC.CanLeave && raid.Options.NPC.BlockOutsideDamageOnLeave && !InRange(attacker.transform.position, raid.Location, raid.ProtectionRadius))
            {
                brain.Forget();

                return false;
            }

            if (raid.Options.NPC.PlayerMaxEffectiveRange > 0f)
            {
                float distance = (attacker.transform.position - brain.ServerPosition).magnitude;

                if (distance > raid.ProtectionRadius)
                {
                    float multiplier = distance > raid.Options.NPC.PlayerMaxEffectiveRange ? 0f : 1f - (distance / raid.Options.NPC.PlayerMaxEffectiveRange);

                    hitInfo.damageTypes.ScaleAll(multiplier);
                }
            }

            if (brain.isAsleep)
            {
                if (brain.unwakeable)
                {
                    return true;
                }

                brain.SetSleeping(false);
            }

            brain.SetTarget(attacker);

            return true;
        }

        private object HandlePVPDamage(RaidableBase raid, BasePlayer victim, BasePlayer attacker, HitInfo hitInfo)
        {
            if (raid.HasLockout(attacker, !hitInfo.IsMajorityDamage(DamageType.Heat)))
            {
                return false;
            }

            if (CanBlockOutsideDamage(raid, attacker, raid.Options.BlockOutsideDamageToPlayersInside) && !(hitInfo.WeaponPrefab is MLRSRocket))
            {
                if (config.EventMessages.NoDamageFromOutsideToPlayersInside && !hitInfo.IsMajorityDamage(DamageType.Heat)) TryMessage(attacker, "NoDamageFromOutsideToPlayersInside");
                return false;
            }

            if (IsPVE() && (!InRange(attacker.transform.position, raid.Location, raid.ProtectionRadius) || !InRange(victim.transform.position, raid.Location, raid.ProtectionRadius)))
            {
                return false;
            }

            if (raid.IsAlly(attacker.userID, victim.userID))
            {
                return raid.Options.AllowFriendlyFire;
            }

            return raid.AllowPVP;
        }

        private object HandleNpcAttacker(RaidableBase raid, BasePlayer victim, BasePlayer attacker, HitInfo hitInfo)
        {
            if (!HumanoidBrains.TryGetValue(attacker.userID, out var brain))
            {
                return true;
            }

            if (Has(victim.userID) || (Has(attacker.userID) && CanBlockOutsideDamage(raid, victim, raid.Options.BlockNpcDamageToPlayersOutside)))
            {
                return false;
            }

            if (brain.SenseRange <= brain.softLimitSenseRange && hitInfo.IsProjectile() && UnityEngine.Random.Range(0f, 100f) > raid.Options.NPC.Accuracy.Get(brain))
            {
                return false;
            }

            if (raid.Options.NPC.NpcMaxEffectiveRange > 0f)
            {
                float distance = (victim.transform.position - brain.ServerPosition).magnitude;

                if (distance > raid.ProtectionRadius)
                {
                    float multiplier = distance > raid.Options.NPC.NpcMaxEffectiveRange ? 0f : 1f - (distance / raid.Options.NPC.NpcMaxEffectiveRange);

                    hitInfo.damageTypes.ScaleAll(multiplier);
                }
            }

            if (hitInfo.IsMajorityDamage(DamageType.Explosion))
            {
                hitInfo.UseProtection = false;
            }

            if (brain.attackType == HumanoidBrain.AttackType.BaseProjectile)
            {
                hitInfo.damageTypes.ScaleAll(raid.Options.NPC.Multipliers.ProjectileDamageMultiplier);
            }
            else if (brain.attackType == HumanoidBrain.AttackType.Explosive)
            {
                hitInfo.damageTypes.ScaleAll(raid.Options.NPC.Multipliers.ExplosiveDamageMultiplier);
            }
            else if (brain.attackType == HumanoidBrain.AttackType.Melee)
            {
                hitInfo.damageTypes.ScaleAll(raid.Options.NPC.Multipliers.MeleeDamageMultiplier);
            }

            return true;
        }

        private bool IsSpecialEvent(HitInfo hitInfo)
        {
            return hitInfo.Initiator != null && hitInfo.Initiator.OwnerID == 8002738255;
        }

        private object HandleEntityDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (hitInfo.Initiator is SamSite ss)
            {
                return Has(ss) ? true : (object)null;
            }

            if (IsSpecialEvent(hitInfo) || !Get(entity.transform.position, out var raid))
            {
                return null;
            }

            if (IsHelicopter(hitInfo, out var eventHeli))
            {
                if (config.Settings.Management.BlockHelicopterDamage && !entity.OwnerID.IsSteamId())
                {
                    hitInfo.damageTypes.ScaleAll(0f);
                }
                return eventHeli ? (object)null : true;
            }

            bool isAttacker = raid.GetInitiatorPlayer(hitInfo, entity, out var attacker);

            if (raid.IsDespawning)
            {
                return !isAttacker ? true : (object)null;
            }

            if (entity.OwnerID == 0uL && raid.Type != RaidableType.None && isAttacker)
            {
                raid.IsEngaged = true;
                raid.CheckDespawn();
            }

            if (raid.PlayerDamageMultiplier.Count > 0 && isAttacker && attacker.IsHuman())
            {
                foreach (var (index, amount) in raid.PlayerDamageMultiplier)
                {
                    hitInfo.damageTypes.Scale(index, amount);
                }
            }

            if (entity is BearTrap trap)
            {
                if (raid.Options.BearTrapsImmuneToExplosives && hitInfo.WeaponPrefab is TimedExplosive) hitInfo.damageTypes.ScaleAll(0f);
                if (raid.Options.RearmBearTraps) trap.Invoke(trap.Arm, 0.1f);
            }

            if (raid.IsDamageBlocked(entity) || !raid.Options.MLRS && hitInfo.WeaponPrefab is MLRSRocket)
            {
                return false;
            }

            if (raid.IsLoading || entity is DroppedItemContainer || hitInfo.IsMajorityDamage(DamageType.Decay))
            {
                return false;
            }

            if (entity.IsNpc || entity is PlayerCorpse)
            {
                return true;
            }

            if (entity is BuildingBlock block)
            {
                if (raid.Options.Setup.FoundationsImmune && raid.Options.Setup.ForcedHeight != -1)
                {
                    if (raid.foundations.Count > 0 && entity.ShortPrefabName.StartsWith("foundation"))
                    {
                        return false;
                    }

                    if (raid.floors == null && entity.ShortPrefabName.StartsWith("floor") && entity.transform.position.y - raid.Location.y <= 3f)
                    {
                        return false;
                    }
                }
                if (entity.OwnerID == 0)
                {
                    if (raid.Options.TwigImmune && block.grade == BuildingGrade.Enum.Twigs)
                    {
                        return false;
                    }
                    if (raid.Options.BlocksImmune)
                    {
                        return block.grade == BuildingGrade.Enum.Twigs;
                    }
                }
                else if (block.grade == BuildingGrade.Enum.Twigs)
                {
                    return true;
                }
            }
            else if (entity is BaseMountable mountable)
            {
                if (config.Settings.Management.MiniCollision && entity is Minicopter && entity == hitInfo.Initiator)
                {
                    return false;
                }
                if (isAttacker && attacker.IsHuman() && !ExcludedMounts.Exists(entity.ShortPrefabName.StartsWith))
                {
                    BaseVehicle vehicle = mountable.HasParent() ? mountable.VehicleParent() : mountable as BaseVehicle;

                    if (vehicle != null && vehicle.GetDriver() == attacker)
                    {
                        return config.Settings.Management.MountDamageFromPlayers;
                    }
                    if (!config.Settings.Management.MountDamageFromPlayers)
                    {
                        TryMessage(attacker, "NoMountedDamageTo");
                        return false;
                    }
                    if (config.Settings.Management.BlockMounts && (attacker.GetMounted() || attacker.GetParentEntity() is BaseMountable))
                    {
                        TryMessage(attacker, "NoMountedDamageFrom");
                        return false;
                    }
                    if (CanBlockOutsideDamage(raid, attacker, raid.Options.BlockOutsideDamageToBaseInside) && !(hitInfo.WeaponPrefab is MLRSRocket))
                    {
                        TryMessage(attacker, "NoDamageFromOutsideToBaseInside");
                        return false;
                    }
                }
                if (hitInfo.Initiator == mountable)
                {
                    return config.Settings.Management.MountDamageFromPlayers;
                }
            }

            if (!Has(entity))
            {
                return null;
            }

            if (hitInfo.Initiator == entity && entity is AutoTurret)
            {
                return false;
            }

            if (hitInfo.WeaponPrefab?.ShortPrefabName == "torpedostraight")
            {
                hitInfo.damageTypes.ScaleAll(UnityEngine.Random.Range(raid.Options.Water.TorpedoMin, raid.Options.Water.TorpedoMax));
            }

            if (!attacker.IsReallyValid())
            {
                return hitInfo.Initiator.IsNull() || Has(hitInfo.Initiator) ? true : (object)null;
            }

            if (!attacker.IsHuman())
            {
                if (entity.OwnerID == 0uL && !raid.Options.RaidingNpcs && !Has(attacker.userID))
                {
                    return false;
                }

                if (hitInfo.damageTypes.Has(DamageType.Explosion) || hitInfo.WeaponPrefab is TimedExplosive)
                {
                    if (entity.OwnerID == 0uL && !(entity is BasePlayer) && Has(attacker.userID))
                    {
                        return false;
                    }

                    return entity.OwnerID == 0uL || raid.BuiltList.ContainsKey(entity);
                }

                return true;
            }

            entity.lastAttacker = attacker;
            attacker.lastDealtDamageTime = Time.time;

            if (raid.Options.Eco.Enabled && !raid.IsEcoTool(attacker, hitInfo))
            {
                TryMessage(attacker, "EcoOnly");
                return false;
            }

            if (config.Settings.Management.BlockMounts && (attacker.GetMounted() || attacker.GetParentEntity() is BaseMountable))
            {
                TryMessage(attacker, "NoMountedDamageFrom");
                return false;
            }

            if (CanBlockOutsideDamage(raid, attacker, raid.Options.BlockOutsideDamageToBaseInside) && !(hitInfo.WeaponPrefab is MLRSRocket))
            {
                TryMessage(attacker, "NoDamageFromOutsideToBaseInside");
                return false;
            }

            if (raid.ID.IsSteamId() && IsBox(entity, false) && (attacker.UserIDString == raid.ID || raid.IsAlly(attacker.userID, Convert.ToUInt64(raid.ID))))
            {
                return false;
            }

            if (raid.ownerId.IsSteamId() && raid.CanEject() && !raid.IsAlly(attacker))
            {
                TryMessage(attacker, "NoDamageToEnemyBase");
                return false;
            }

            if (raid.HasLockout(attacker, !hitInfo.IsMajorityDamage(DamageType.Heat)))
            {
                return false;
            }

            if (raid.Options.AutoTurret.AutoAdjust && raid.turrets.Count > 0 && entity is AutoTurret turret)
            {
                if (raid.turrets.Contains(turret) && turret.sightRange <= raid.Options.AutoTurret.SightRange)
                {
                    turret.sightRange = raid.Options.AutoTurret.SightRange * 2f;
                }
            }

            if (!raid.Options.ExplosionModifier.Equals(100) && hitInfo.damageTypes.Has(DamageType.Explosion))
            {
                float m = Mathf.Clamp(raid.Options.ExplosionModifier, 0f, 999f);

                hitInfo.damageTypes.Scale(DamageType.Explosion, m.Equals(0f) ? 0f : m / 100f);
            }

            if (raid.BuiltList.ContainsKey(entity))
            {
                return true;
            }

            if (raid.IsOpened && IsLootingWeapon(hitInfo) && raid.AddLooter(attacker, hitInfo))
            {
                raid.TrySetOwner(attacker, entity, hitInfo);
            }

            if (!raid.CanHurtBox(entity))
            {
                if (!hitInfo.damageTypes.Has(DamageType.Heat))
                {
                    TryMessage(attacker, "NoDamageToBoxes");
                }
                return false;
            }

            if (raid.Options.MLRS && hitInfo.WeaponPrefab is MLRSRocket)
            {
                raid.GetRaider(attacker).lastActiveTime = Time.time;
            }

            return true;
        }

        #endregion Hooks

        #region Spawn

        private static void Shuffle<T>(IList<T> list) // Fisher-Yates shuffle
        {
            int count = list.Count;
            int n = count;
            while (n-- > 0)
            {
                int k = UnityEngine.Random.Range(0, count);
                int j = UnityEngine.Random.Range(0, count);
                T value = list[k];
                list[k] = list[j];
                list[j] = value;
            }
        }

        public RaidableBase OpenEvent(RandomBase rb)
        {
            var raid = new GameObject().AddComponent<RaidableBase>();

            raid.Instance = this;
            raid.markerName = config.Settings.Markers.MarkerName;
            raid.spawnDateTime = DateTime.Now;
            raid.stability = rb.stability;
            raid.name = Name;
            raid.SetAllowPVP(rb);
            raid.Location = rb.Position;
            raid.Options = rb.options;
            raid.BaseName = rb.BaseName;
            raid.BaseHeight = rb.baseHeight;
            raid.ProfileName = rb.Profile.ProfileName;
            raid.IsLoading = true;
            raid.loadTime = Time.time;
            raid.InitiateTurretOnSpawn = rb.options.AutoTurret.InitiateOnSpawn;

            if (rb.type == RaidableType.Purchased)
            {
                raid.ownerId = rb.payments.userid;
                raid.ownerName = rb.payments.username;
            }

            foreach (var multiplier in raid.Options.PlayerDamageMultiplier)
            {
                float amount = multiplier.amount;
                if (amount == 1f) continue;
                DamageType index = multiplier.index;
                if (index == DamageType.Generic) continue;
                raid.PlayerDamageMultiplier[index] = amount;
            }

            if (!raid.Options.MLRS)
            {
                Subscribe(nameof(OnMlrsFire));
            }

            if ((config.Settings.NoWizardryPVP && raid.AllowPVP || config.Settings.NoWizardryPVE && !raid.AllowPVP) && Wizardry.CanCall())
            {
                Subscribe(nameof(OnActiveItemChanged));
            }
            else if ((config.Settings.NoArcheryPVP && raid.AllowPVP || config.Settings.NoArcheryPVE && !raid.AllowPVP) && Archery.CanCall())
            {
                Subscribe(nameof(OnActiveItemChanged));
            }

            if (raid.BlacklistedCommands.Count > 0)
            {
                Subscribe(nameof(OnPlayerCommand));
                Subscribe(nameof(OnServerCommand));
            }

            if (IsPVE())
            {
                Subscribe(nameof(CanEntityTrapTrigger));
                Subscribe(nameof(CanEntityBeTargeted));
            }
            else
            {
                Subscribe(nameof(OnSamSiteTarget));
                Subscribe(nameof(OnTrapTrigger));
            }

            Subscribe(nameof(OnInterferenceOthersUpdate));
            Subscribe(nameof(OnInterferenceUpdate));
            Subscribe(nameof(OnStructureUpgrade));
            Subscribe(nameof(OnEntityEnter));
            Subscribe(nameof(CanBuild));
            Subscribe(nameof(OnEntitySpawned));

            data.TotalEvents++;
            raid._undoLimit = Mathf.Clamp(raid.Options.Setup.DespawnLimit, 1, 500);

            Raids.Add(raid);

            raid.CheckPaste();
            raid.SetupCollider();
            raid.InvokeRepeating(raid.CheckEntitiesInSphere, 0.1f, 0.1f);

            return raid;
        }

        #endregion

        #region Paste

        private float isSpawnerBusyTime;
        private bool isSpawnerBusy;

        private bool IsLoaderBusy => Raids.Exists(raid => raid.IsDespawning || raid.IsLoading);

        private bool IsSpawnerBusy
        {
            get
            {
                if (Time.time > isSpawnerBusyTime)
                {
                    isSpawnerBusy = false;
                }

                return IsUnloading || isSpawnerBusy;
            }
            set
            {
                isSpawnerBusyTime = Time.time + 180f;
                isSpawnerBusy = value;
            }
        }

        private bool IsGridLoading() => GridController.gridCoroutine != null;

        private bool IsPasteAvailable() => !Raids.Exists(raid => raid.IsLoading);

        private bool IsBusy() => IsSpawnerBusy || IsLoaderBusy || IsGridLoading();

        private Payment TryBuyRaidServerRewards(int cost, BasePlayer buyer, BasePlayer player)
        {
            int points = Convert.ToInt32(ServerRewards?.Call("CheckPoints", buyer.userID));

            if (points > 0 && points - cost >= 0)
            {
                return new(this, buyer, player, null, cost);
            }

            Message(buyer, "ServerRewardPointsFailed", cost);
            return null;
        }

        private Payment TryBuyRaidEconomics(double cost, BasePlayer buyer, BasePlayer player)
        {
            object obj;

            if ((obj = Economics?.Call("Balance", buyer.UserIDString)) != null || (obj = IQEconomic?.Call("API_GET_BALANCE", buyer.userID)) != null || (obj = BankSystem?.Call("Balance", buyer.userID)) != null)
            {
                var balance = Convert.ToDouble(obj);

                if (balance > 0 && balance - cost >= 0)
                {
                    return new(this, buyer, player, null, 0, cost);
                }
            }

            Message(buyer, "EconomicsWithdrawFailed", cost);
            return null;
        }

        private Payment TryBuyRaidCustom(List<CustomCostOptions> options, BasePlayer buyer, BasePlayer player)
        {
            foreach (var option in options)
            {
                var slots = buyer.inventory.FindItemsByItemID(option.Definition.itemid);
                int amount = 0;

                foreach (var slot in slots)
                {
                    if (option.Skin != 0 && slot.skin != option.Skin)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(option.Name) && slot.name != option.Name && option.Skin == 0)
                    {
                        continue;
                    }

                    amount += slot.amount;

                    if (amount >= option.Amount)
                    {
                        break;
                    }
                }

                if (amount < option.Amount && config.Settings.ShoppyStock != null)
                {
                    amount += Convert.ToInt32(ShoppyStock?.Call("GetCurrencyAmount", config.Settings.ShoppyStock.ShopName, player.userID));
                }

                if (amount < option.Amount)
                {
                    Message(buyer, "CustomWithdrawFailed", $"{(string.IsNullOrEmpty(option.Name) ? option.Shortname : option.Name)} ({option.Amount})");
                    return null;
                }
            }

            return new(this, buyer, player, options);
        }

        public class Payments
        {
            public Payment Custom;
            public Payment Economics;
            public Payment ServerRewards;
            public BasePlayer owner;
            public ulong userid;
            public string username;
            public Vector3 position;
            public int type;
            public bool valid => Payment.IsValid(Custom) || Payment.IsValid(Economics) || Payment.IsValid(ServerRewards);
            public Payments() { }
            public Payments(BasePlayer owner)
            {
                if (owner.IsKilled()) return;
                this.owner = owner;
                userid = owner.userID;
                username = owner.displayName;
                position = owner.transform.position;
            }
            public void Refund()
            {
                Custom?.RefundItems();
                Economics?.RefundMoney();
                ServerRewards?.RefundPoints();
            }
            public void Take(bool reset)
            {
                Custom?.TakeItems(reset);
                Economics?.TakeMoney(reset);
                ServerRewards?.TakePoints(reset);
            }
        }

        public class Payment
        {
            public Payment(RaidableBases instance, BasePlayer buyer, BasePlayer owner = null, List<CustomCostOptions> options = null, int RP = 0, double money = 0)
            {
                this.userId = owner?.userID ?? buyer?.userID ?? 0;
                this.buyerName = buyer?.displayName ?? owner?.displayName;
                this.buyerId = buyer?.userID ?? userId;
                this.money = money;
                this.RP = RP;

                Options = options;
                Instance = instance;

                free = money == 0.0 && RP == 0 && options.IsNullOrEmpty();
                paid = free;
            }

            public RaidableBases Instance;
            public bool paid;
            public bool free;
            public int RP;
            public double money;
            public string buyerName;
            public ulong buyerId;
            public ulong userId;
            public BasePlayer _buyer;
            public BasePlayer _owner;
            public BasePlayer buyer => _buyer = ((bool)_buyer ? _buyer : RustCore.FindPlayerById(buyerId));
            public BasePlayer owner => _owner = ((bool)_owner ? _owner : RustCore.FindPlayerById(userId));
            public List<CustomCostOptions> Options;
            public bool self => buyerId == userId;
            public StringBuilder _sb => Instance._sb;
            public Configuration config => Instance.config;
            public static bool IsValid(Payment payment) => payment != null && payment.owner != null && payment.buyer != null;
            private void QueueNotification(BasePlayer player, string key, params object[] args) => Instance.Message(player, key, args);

            private void Message(BasePlayer player, string key, params object[] args) => Instance.Message(player, key, args);

            private string mx(string key, string id = null, params object[] args) => Instance.mx(key, id, args);

            public void RefundItems()
            {
                var target = buyer ?? owner;

                if (target == null || !paid)
                {
                    return;
                }

                _sb.Clear();

                foreach (var option in Options)
                {
                    Item item = ItemManager.CreateByItemID(option.Definition.itemid, option.Amount, option.Skin);

                    _sb.Append(mx("Items", target.UserIDString, option.Amount, string.IsNullOrEmpty(option.Name) ? item.info.displayName.english : item.name = option.Name)).Append(", ");

                    target.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
                }

                if (_sb.Length > 2)
                {
                    _sb.Length -= 2;

                    Message(target, _sb.ToString());
                }

                paid = false;
            }

            public void TakeItems(bool reset)
            {
                if (buyer == null)
                {
                    return;
                }

                var sb = new StringBuilder();

                foreach (var option in Options)
                {
                    var slots = buyer.inventory.FindItemsByItemID(option.Definition.itemid);
                    var amountLeft = option.Amount;

                    foreach (var slot in slots)
                    {
                        if (slot == null || option.Skin != 0 && slot.skin != option.Skin)
                        {
                            continue;
                        }

                        var taken = slot.amount > amountLeft ? slot.SplitItem(amountLeft) : slot;

                        taken.Drop(Vector3.zero, Vector3.zero);

                        amountLeft -= taken.amount;

                        if (amountLeft <= 0)
                        {
                            string name = string.IsNullOrEmpty(option.Name) ? slot.info.displayName.english : option.Name;
                            sb.Append(string.Format("{0} {1}", option.Amount, name)).Append(", ");
                            paid = true;
                            break;
                        }
                    }

                    if (amountLeft > 0 && Instance.ShoppyStock != null && config.Settings.ShoppyStock != null && config.Settings.ShoppyStock.IsItem(option))
                    {
                        Instance.ShoppyStock?.Call("TakeCurrency", config.Settings.ShoppyStock.ShopName, buyer.userID, amountLeft);
                        CuiHelper.DestroyUi(buyer, $"PopUpAPI_{config.Settings.ShoppyStock.PanelName}_Parent");
                        paid = true;
                    }
                }

                if (sb.Length > 2)
                {
                    sb.Length -= 2;

                    if (!self)
                    {
                        Message(owner, "CustomWithdrawGift", buyerName, sb.ToString());
                    }

                    Message(buyer, reset ? "CustomWithdrawReset" : "CustomWithdraw", sb.ToString());
                }
            }

            public void TakeMoney(bool reset)
            {
                if (money > 0)
                {
                    if (Convert.ToBoolean(Instance.Economics?.Call("Withdraw", userId.ToString(), money)))
                    {
                        paid = true;
                    }

                    if (Convert.ToBoolean(Instance.BankSystem?.Call("Withdraw", userId, (int)money)))
                    {
                        paid = true;
                    }

                    if (Instance.IQEconomic != null)
                    {
                        Instance.IQEconomic?.Call("API_REMOVE_BALANCE", userId, (int)money);
                        paid = true;
                    }

                    if (!self)
                    {
                        Message(owner, "EconomicsWithdrawGift", buyerName, money);
                    }

                    Message(buyer, reset ? "EconomicsWithdrawReset" : "EconomicsWithdraw", money);
                }
            }

            public void RefundMoney()
            {
                if (paid && money > 0)
                {
                    Instance.BankSystem?.Call("Deposit", userId, (int)money);
                    Instance.Economics?.Call("Deposit", userId.ToString(), money);
                    Instance.IQEconomic?.Call("API_SET_BALANCE", userId, (int)money);
                    QueueNotification(buyer, "Refunded Money", money);
                    money = 0;
                }
            }

            public void TakePoints(bool reset)
            {
                if (RP > 0)
                {
                    if (Convert.ToBoolean(Instance.ServerRewards?.Call("TakePoints", userId, RP)))
                    {
                        paid = true;
                    }

                    if (!self)
                    {
                        Message(owner, "ServerRewardPointsGift", buyerName, RP);
                    }

                    Message(buyer, reset ? "ServerRewardPointsTakenReset" : "ServerRewardPointsTaken", RP);
                }
            }

            public void RefundPoints()
            {
                if (paid && RP > 0)
                {
                    Instance.ServerRewards?.Call("AddPoints", userId, RP);
                    QueueNotification(buyer, "Refunded RP", RP);
                    RP = 0;
                }
            }
        }

        private bool BuyRaid(RaidableMode mode, Payments payments, BasePlayer owner, string baseName, bool free)
        {
            if (SpawnRandomBase(RaidableType.Purchased, mode, baseName, false, payments, owner, null, free))
            {
                Message(owner, "BaseQueued", Queues.queue.Count);
                return true;
            }
            return false;
        }

        private bool IsDifficultyAvailable(RaidableMode mode, bool checkAllowPVP)
        {
            return CanSpawnDifficultyToday(mode) && Buildings.Profiles.Values.Exists(profile => profile.Options.Mode == mode && (!checkAllowPVP || config.Settings.Buyable.BuyPVP || !profile.Options.AllowPVP || config.Settings.Buyable.ConvertPVP));
        }

        private bool PasteBuilding(RandomBase rb)
        {
            Queues.Messages.Print($"{rb.BaseName} trying to paste at {rb.Position}");

            if (!IsCopyPasteLoaded(out var error))
            {
                Puts(error);

                return false;
            }

            loadCoroutines.Add(ServerMgr.Instance.StartCoroutine(LoadCopyPasteFile(rb)));

            return true;
        }

        private void StopLoadCoroutines()
        {
            if (setupCopyPasteObstructionRadius != null)
            {
                ServerMgr.Instance.StopCoroutine(setupCopyPasteObstructionRadius);
                setupCopyPasteObstructionRadius = null;
            }
            foreach (var co in loadCoroutines.ToList())
            {
                if (co != null)
                {
                    ServerMgr.Instance.StopCoroutine(co);
                }
            }
            foreach (var raid in Raids)
            {
                raid.StopSetupCoroutine();
            }
            Queues?.StopCoroutine();
            Automated?.DestroyMe();
            GridController.StopCoroutine();
        }

        private bool IsPrefabFoundation(Dictionary<string, object> entity)
        {
            var prefabname = entity["prefabname"].ToString();

            return prefabname.Contains("/foundation.") || prefabname.EndsWith("diesel_collectable.prefab") && entity.ContainsKey("skinid") && entity["skinid"] == "1337424001";
        }

        private bool IsPrefabExternalWall(Dictionary<string, object> entity)
        {
            return entity["prefabname"].ToString().Contains("/wall.external.high.");
        }

        private bool IsPrefabFloor(Dictionary<string, object> entity)
        {
            return entity.TryGetValue("prefabname", out var obj) && obj != null && obj.ToString().Contains("/floor");
        }

        private IEnumerator SetupCopyPasteObstructionRadius()
        {
            foreach (var profile in Buildings.Profiles)
            {
                var radius = profile.Value.Options.ProtectionRadii.Obstruction == -1 ? 0f : GetObstructionRadius(profile.Value.Options.ProtectionRadii, RaidableType.None);
                foreach (var extra in profile.Value.Options.AdditionalBases)
                {
                    yield return SetupCopyPasteObstructionRadius(extra.Key, radius);
                }
                yield return SetupCopyPasteObstructionRadius(profile.Key, radius);
            }
            setupCopyPasteObstructionRadius = null;
        }

        private IEnumerator SetupCopyPasteObstructionRadius(string baseName, float radius)
        {
            var filename = $"copypaste/{baseName}";

            if (!Interface.Oxide.DataFileSystem.ExistsDatafile(filename))
            {
                yield break;
            }

            DynamicConfigFile data;

            try
            {
                data = Interface.Oxide.DataFileSystem.GetDatafile(filename);
            }
            catch (Exception ex)
            {
                Queues.Messages.Log(baseName, $"{baseName} could not be read from the disk #1: {ex}");
                yield break;
            }

            if (data["entities"] == null)
            {
                Queues.Messages.Log(baseName, $"{baseName} is missing entity data");
                yield break;
            }

            var entities = data["entities"] as List<object>;
            var foundations = new List<Vector3>();
            var floors = new List<Vector3>();
            int checks = 0;
            float x = 0f;
            float z = 0f;

            foreach (Dictionary<string, object> entity in entities)
            {
                if (++checks >= 1000)
                {
                    checks = 0;
                    yield return Automated.instruction0;
                }
                if (!entity.ContainsKey("prefabname") || !entity.ContainsKey("pos"))
                {
                    continue;
                }
                try
                {
                    var axes = entity["pos"] as Dictionary<string, object>;
                    var position = new Vector3(Convert.ToSingle(axes?["x"]), Convert.ToSingle(axes?["y"]), Convert.ToSingle(axes?["z"]));
                    if (IsPrefabFoundation(entity) || IsPrefabExternalWall(entity))
                    {
                        foundations.Add(position);
                        x += position.x;
                        z += position.z;
                    }
                    if (IsPrefabFloor(entity))
                    {
                        floors.Add(position);
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogException(ex);
                    Puts("Invalid entity found in copypaste file: {0} ({1})", baseName, entity["prefabname"]);
                }
            }

            if (foundations.Count == 0)
            {
                foreach (var position in floors)
                {
                    foundations.Add(position);
                    x += position.x;
                    z += position.z;
                }
            }

            if (foundations.Count == 0)
            {
                Queues.Messages.Log(baseName, $"{baseName} is missing foundation/floor data #1");
                yield break;
            }

            var center = new Vector3(x / foundations.Count, 0f, z / foundations.Count);

            center.y = GetSpawnHeight(center);

            if (radius == 0f)
            {
                foundations.Sort((a, b) => (a - center).sqrMagnitude.CompareTo((b - center).sqrMagnitude));

                radius = Vector3.Distance(foundations[0], foundations[foundations.Count - 1]) + 3f;
            }

            var pasteData = GetPasteData(baseName);

            pasteData.radius = Mathf.Ceil(Mathf.Max(CELL_SIZE, radius));
            pasteData.foundations = foundations;
            pasteData.valid = true;
        }

        private IEnumerator LoadCopyPasteFile(RandomBase rb)
        {
            Core.Configuration.DynamicConfigFile data;

            try
            {
                data = Interface.Oxide.DataFileSystem.GetDatafile($"copypaste/{rb.BaseName}");
            }
            catch (Exception ex)
            {
                Queues.Messages.Log(rb.BaseName, $"{rb.BaseName} could not be read from the disk #2: {ex}");
                yield break;
            }

            yield return ApplyStartPositionAdjustment(rb, data);

            if (rb.pasteData.foundations.IsNullOrEmpty())
            {
                Queues.Messages.Log(rb.BaseName, $"{rb.BaseName} is missing foundation/floor data #2");
                Buildings.Remove(rb.BaseName);
                IsSpawnerBusy = false;
                yield break;
            }

            var entities = data["entities"] as List<object>;

            if (entities == null)
            {
                Queues.Messages.Log(rb.BaseName, $"{rb.BaseName} is missing entity data");
                yield break;
            }

            var preloadData = CopyPaste.Call("PreLoadData", entities, rb.Position, 0f, true, true, false, true) as HashSet<Dictionary<string, object>>;

            yield return TryApplyAutoHeight(rb, preloadData);

            if (!IsUnloading)
            {
                TryInvokeMethod(() => RFManager.GetListenList(1).RemoveAll(obj => !BaseEntityEx.IsValidEntityReference(obj)));

                var raid = OpenEvent(rb);
                var protocol = data["protocol"] == null ? new Dictionary<string, object>() : data["protocol"] as Dictionary<string, object>;
                var result = CopyPaste.Call("Paste", new object[] { preloadData, protocol, false, rb.Position, _consolePlayer, rb.stability, 0f, rb.heightAdj, false, CreatePastedCallback(raid, rb), CreateSpawnCallback(raid), rb.BaseName, false });
                if (result == null)
                {
                    Queues.Messages.Print($"CopyPaste {CopyPaste.Version} did not respond for {rb.BaseName}!");
                    rb.payments.Refund();
                    raid.Despawn();
                }
                else
                {
                    Queues.Messages.Print($"{rb.BaseName} is pasting at {rb.Position}");
                }
            }
        }

        private Action CreatePastedCallback(RaidableBase raid, RandomBase rb)
        {
            return new(() =>
            {
                SaveTemporaryData();

                raid.IsPasted = true;

                if (raid.IsUnloading)
                {
                    rb.payments.Refund();
                    raid.Despawn();
                }
                else
                {
                    raid.Init(rb);
                }
            });
        }

        private Action<BaseEntity> CreateSpawnCallback(RaidableBase raid)
        {
            return new(e =>
            {
                if (e.IsKilled() || raid.IsUnloading)
                {
                    NextTick(e.SafelyKill);
                    return;
                }
                if (e is AutoTurret turret)
                {
                    raid.PreSetupTurret(turret);
                }
                else if (e.ShortPrefabName == "foundation.triangle" || e.ShortPrefabName == "foundation" || e.skinID == 1337424001 && e is CollectibleEntity)
                {
                    raid.foundations.Add(e.transform.position);
                }
                else if (e.ShortPrefabName.Contains("floor"))
                {
                    raid.floors.Add(e.transform.position);
                }
                if (!raid.stability && e is BuildingBlock block)
                {
                    block.grounded = true;
                }
                else if (raid.Options.EmptyAll && e is StorageContainer container)
                {
                    raid.TryEmptyContainer(container);
                }
                foreach (var slot in _checkSlots)
                {
                    if (e.GetSlot(slot) is BaseEntity ent)
                    {
                        raid.AddEntity(ent);
                    }
                }
                e.OwnerID = 0;
                raid.AddEntity(e);
            });
        }

        private IEnumerator ApplyStartPositionAdjustment(RandomBase rb, DynamicConfigFile data)
        {
            ParseListedOptions(rb);

            var foundations = new List<Vector3>();
            float x = 0f, z = 0f;

            if (!rb.pasteData.valid)
            {
                yield return SetupCopyPasteObstructionRadius(rb.BaseName, rb.options.ProtectionRadii.Obstruction == -1 ? 0f : GetObstructionRadius(rb.options.ProtectionRadii, RaidableType.None));
            }

            if (rb.pasteData.foundations.IsNullOrEmpty())
            {
                Queues.Messages.Log(rb.BaseName, $"{rb.BaseName} is missing foundation/floor data #3");
                yield break;
            }

            foreach (var foundation in rb.pasteData.foundations)
            {
                var a = foundation + rb.Position;
                a.y = GetSpawnHeight(a);
                foundations.Add(a);
                x += a.x;
                z += a.z;
            }

            var center = new Vector3(x / foundations.Count, 0f, z / foundations.Count);

            center.y = GetSpawnHeight(center, !rb.options.Water.SpawnOnSeabed);

            rb.Position = rb.Position + (rb.Position - center);

            if (rb.options.Setup.ForcedHeight == -1)
            {
                rb.Position.y = GetSpawnHeight(rb.Position, !rb.options.Water.SpawnOnSeabed);

                TryApplyCustomAutoHeight(rb);
                TryApplyMultiFoundationSupport(rb);

                rb.Position.y += rb.baseHeight + rb.options.Setup.PasteHeightAdjustment;
            }
            else rb.Position.y = rb.baseHeight + rb.options.Setup.PasteHeightAdjustment + rb.options.Setup.ForcedHeight;

            yield return CoroutineEx.waitForFixedUpdate;
        }

        private IEnumerator TryApplyAutoHeight(RandomBase rb, HashSet<Dictionary<string, object>> preloadData)
        {
            if (rb.autoHeight && !config.Settings.Experimental.Contains(ExperimentalSettings.Type.AutoHeight, rb))
            {
                var bestHeight = Convert.ToSingle(CopyPaste.Call("FindBestHeight", preloadData, rb.Position));
                int checks = 0;

                rb.heightAdj = bestHeight - rb.Position.y;

                foreach (var entity in preloadData)
                {
                    if (++checks >= 1000)
                    {
                        checks = 0;
                        yield return Automated.instruction0;
                    }

                    if (entity.TryGetValue("position", out var obj) && obj is Vector3 pos)
                    {
                        pos.y += rb.heightAdj;

                        entity["position"] = pos;
                    }
                }
            }
        }

        private void TryApplyCustomAutoHeight(RandomBase rb)
        {
            if (config.Settings.Experimental.Contains(ExperimentalSettings.Type.AutoHeight, rb))
            {
                foreach (var foundation in rb.pasteData.foundations)
                {
                    var a = foundation + rb.Position;

                    if (a.y < rb.Position.y)
                    {
                        rb.Position.y += rb.Position.y - a.y;
                        return;
                    }
                    else
                    {
                        rb.Position.y -= a.y - rb.Position.y;
                        return;
                    }
                }
            }
        }

        private void TryApplyMultiFoundationSupport(RandomBase rb)
        {
            float j = 0f, k = 0f, y = 0f;
            for (int i = 0; i < rb.pasteData.foundations.Count; i++)
            {
                y = (float)Math.Round(rb.pasteData.foundations[i].y, 1);
                j = Mathf.Max(y, j);
                k = Mathf.Min(y, k);
            }
            if (j != 0f && config.Settings.Experimental.Contains(ExperimentalSettings.Type.MultiFoundation, rb))
            {
                rb.Position.y += j + 1f;
            }
            else if (k != 0f && config.Settings.Experimental.Contains(ExperimentalSettings.Type.Bunker, rb))
            {
                y = rb.Position.y + Mathf.Abs(k);
                if (y < rb.Position.y)
                {
                    rb.Position.y = y + 1.5f;
                }
            }
        }

        [HookMethod("GetSpawnHeight")]
        public float GetSpawnHeight(Vector3 a, bool flag = true, bool shouldSkipSmallRock = false) => SpawnsController.GetSpawnHeight(a, flag, shouldSkipSmallRock);

        private void ParseListedOptions(RandomBase rb)
        {
            rb.autoHeight = false;

            List<PasteOption> options = rb.options.PasteOptions;

            foreach (var (key, value) in rb.options.AdditionalBases)
            {
                if (key.Equals(rb.BaseName, StringComparison.OrdinalIgnoreCase))
                {
                    options = value;
                    break;
                }
            }

            foreach (var option in options)
            {
                switch (option.Key.ToLower())
                {
                    case "stability": rb.stability = option.Value.ToLower() == "true"; break;
                    case "autoheight": rb.autoHeight = option.Value.ToLower() == "true"; break;
                    case "height" when float.TryParse(option.Value, out var y): rb.baseHeight = y; break;
                }
            }
        }

        private object SpawnRandomBase(string b = null, Vector3 a = default, int m = -1, int t = 1, bool free = true)
        {
            var type = (RaidableType)t;
            var mode = GetRaidableMode(m.ToString());
            var (key, profile) = GetBuilding(type, mode, b, null);

            if (!IsProfileValid(key, profile, free, RaidableType.Manual))
            {
                return "API_INVALID_PROFILE";
            }

            var spawns = GetSpawns(type, profile, out var checkTerrain);

            if (a == Vector3.zero)
            {
                return SpawnRandomBase(type, mode, key);
            }

            return AddSpawnToQueue(key, profile, checkTerrain, type, spawns, null, null, null, a);
        }

        private bool SpawnRandomBase(RaidableType type, RaidableMode mode, string baseName = null, bool isAdmin = false, Payments payments = null, BasePlayer owner = null, IPlayer user = null, bool free = false)
        {
            var (key, profile) = GetBuilding(type, mode, baseName, owner);
            bool validProfile = IsProfileValid(key, profile, free, type);
            var spawns = GetSpawns(type, profile, out var checkTerrain);

            if (validProfile && spawns != null)
            {
                return AddSpawnToQueue(key, profile, checkTerrain, type, spawns, payments, owner, user, Vector3.zero);
            }
            else if (type == RaidableType.Maintained || type == RaidableType.Scheduled)
            {
                Queues.Messages.PrintAll();
            }
            else Queues.Messages.Add(GetDebugMessage(mode, validProfile, isAdmin, owner?.UserIDString, baseName, profile?.Options), null);

            if (!validProfile)
            {
                if (payments != null)
                {
                    if (!string.IsNullOrEmpty(baseName) && profile != null && !profile.Options.Enabled)
                    {
                        Message(owner, "Profile Not Enabled", baseName);
                    }
                    else Message(owner, "Difficulty Not Buyable", mode);
                    payments.Refund();
                }
                else if (user != null)
                {
                    user.Message(Queues.Messages.GetLast());
                }
            }

            return false;
        }

        private bool AddSpawnToQueue(string key, BaseProfile profile, bool checkTerrain, RaidableType type, RaidableSpawns spawns, Payments payments = null, BasePlayer owner = null, IPlayer user = null, Vector3 point = default)
        {
            var rb = new RandomBase(this, key, profile, point, type, spawns, payments);

            rb.checkTerrain = checkTerrain;
            rb.owner = owner;
            rb.user = user;
            rb.userid = owner?.userID ?? 0;
            rb.username = owner?.displayName ?? "";
            rb.typeDistance = GetDistance(rb.type);
            rb.protectionRadius = rb.options.ProtectionRadius(rb.type);
            rb.safeRadius = Mathf.Max(rb.options.ArenaWalls.Radius, rb.protectionRadius);
            rb.buildRadius = Mathf.Max(config.Settings.Management.CupboardDetectionRadius, rb.options.ArenaWalls.Radius, rb.protectionRadius) + 5f;

            if (rb.buildRadius < 105f && !rb.spawns.IsCustomSpawn)
            {
                rb.buildRadius = 105f;
            }

            Queues.Add(rb);

            return true;
        }

        private string GetDebugMessage(RaidableMode mode, bool validProfile, bool isAdmin, string id, string baseName, BuildingOptions options)
        {
            if (options != null)
            {
                if (!options.Enabled)
                {
                    return mx("Profile Not Enabled", id, baseName);
                }
                else if (options.Mode == RaidableMode.Disabled)
                {
                    return mx("Difficulty Disabled", id, baseName);
                }
            }

            if (!validProfile)
            {
                return Queues.Messages.GetLast(id);
            }

            if (!string.IsNullOrEmpty(baseName))
            {
                if (!FileExists(baseName))
                {
                    return mx("FileDoesNotExist", id);
                }
                else if (!Buildings.IsConfigured(baseName))
                {
                    return mx("BuildingNotConfigured", id);
                }
            }

            if (!IsDifficultyAvailable(mode, options?.AllowPVP ?? false) && mode != RaidableMode.Random)
            {
                return mx(isAdmin ? "Difficulty Not Available Admin" : "Difficulty Not Available", id, (int)mode);
            }
            else if (Buildings.Profiles.Count == 0)
            {
                return mx("NoBuildingsConfigured", id);
            }

            return Queues.Messages.GetLast(id);
        }

        public RaidableSpawns GetSpawns(RaidableType type, BaseProfile profile, out bool checkTerrain)
        {
            checkTerrain = false;
            RaidableSpawns spawns;
            return profile != null && profile.Spawns.IsCustomSpawn ? profile.Spawns : type switch
            {
                RaidableType.Maintained when GridController.Spawns.TryGetValue(RaidableType.Maintained, out spawns) => spawns,
                RaidableType.Manual when GridController.Spawns.TryGetValue(RaidableType.Manual, out spawns) => spawns,
                RaidableType.Purchased when GridController.Spawns.TryGetValue(RaidableType.Purchased, out spawns) => spawns,
                RaidableType.Scheduled when GridController.Spawns.TryGetValue(RaidableType.Scheduled, out spawns) => spawns,
                _ => GridController.Spawns.TryGetValue(RaidableType.Grid, out spawns) && (checkTerrain = true) ? spawns : null
            };
        }

        public (string, BaseProfile) GetBuilding(RaidableType type, RaidableMode mode, string baseName, BasePlayer player = null)
        {
            if (!string.IsNullOrEmpty(baseName) && Buildings.Removed.Contains(baseName))
            {
                return default;
            }

            bool isBaseNull = string.IsNullOrEmpty(baseName) || baseName.Length == 1 && baseName[0] >= '0' && baseName[0] <= '4';
            List<(string, BaseProfile)> profiles = new();

            foreach (var (key, profile) in Buildings.Profiles)
            {
                if (MustExclude(type, profile.Options.AllowPVP))
                {
                    Queues.Messages.Add($"{type} is not configured to include {(profile.Options.AllowPVP ? "PVP" : "PVE")} bases.");
                }

                if (!IsBuildingAllowed(type, mode, profile.Options.Mode, profile.Options.AllowPVP))
                {
                    continue;
                }

                if (!profile.Options.Permission.Has(player, type))
                {
                    continue;
                }

                if (FileExists(key) && (key == baseName || data.Cycle.CanSpawn(type, mode, key, player)))
                {
                    if (!profile.Options.Enabled && key != baseName)
                    {
                        continue;
                    }

                    if (isBaseNull)
                    {
                        profiles.Add((key, profile));
                    }
                    else if (key.Equals(baseName, StringComparison.OrdinalIgnoreCase))
                    {
                        return (key, profile);
                    }
                }

                foreach (var (extra, options) in profile.Options.AdditionalBases)
                {
                    if (FileExists(extra) && (extra == baseName || data.Cycle.CanSpawn(type, mode, extra, player)))
                    {
                        if (!profile.Options.Enabled && extra != baseName)
                        {
                            continue;
                        }

                        var clone = BaseProfile.Clone(profile);

                        clone.Options.PasteOptions = options.ToList();
                        clone.ProfileName = extra;

                        if (isBaseNull)
                        {
                            profiles.Add((extra, clone));
                        }
                        else if (extra.Equals(baseName, StringComparison.OrdinalIgnoreCase))
                        {
                            return (extra, clone);
                        }
                    }
                }
            }

            if (profiles.Count > 0)
            {
                return profiles.GetRandom();
            }

            if (type == RaidableType.Purchased && !config.Settings.Buyable.BuyPVP && Buildings.Profiles.All(x => x.Value.Options.Mode == mode && x.Value.Options.AllowPVP))
            {
                Queues.Messages.Print($"Your config has Allow Players To Buy PVP Raids enabled, and your {mode} profile has Allow PVP enabled which is blocking all purchases of this difficulty.");
            }
            else if (!AnyCopyPasteFileExists)
            {
                Queues.Messages.Print("No copypaste file in any profile exists");
            }
            else Queues.Messages.Print($"Building is unavailable", $"{mode} {type}");

            return default;
        }

        private bool IsProfileValid(string key, BaseProfile profile, bool free, RaidableType type)
        {
            if (string.IsNullOrEmpty(key) || profile == null || profile.Options == null)
            {
                return false;
            }

            return free || profile.Options.Mode != RaidableMode.Disabled && profile.Options.Enabled || !profile.Options.Enabled && type == RaidableType.Manual;
        }

        private RaidableMode GetRandomDifficulty(RaidableType type)
        {
            var modes = new List<RaidableMode>();

            foreach (RaidableMode mode in GetRaidableModes())
            {
                if (!CanSpawnDifficultyToday(mode))
                {
                    Queues.Messages.Add("Cannot spawn difficulty today", mode);
                    continue;
                }

                int max = config.Settings.Management.Amounts.Get(this, mode);

                if (max < 0 || max > 0 && Get(mode, false) >= max)
                {
                    Queues.Messages.Add("Max amount of events reached for difficulty", mode);
                    continue;
                }

                foreach (var profile in Buildings.Profiles.Values)
                {
                    if (profile.Options.Mode == mode && !MustExclude(type, profile.Options.AllowPVP))
                    {
                        modes.Add(mode);
                        break;
                    }
                }
            }

            if (modes.Count > 0)
            {
                if (config.Settings.Management.Chances.Cumulative)
                {
                    return GetRandomDifficulty(modes);
                }

                decimal chance = Convert.ToDecimal(Core.Random.Range(0.0, 100.0));

                foreach (var mode in GetRaidableModes())
                {
                    if (chance <= config.Settings.Management.Chances.Get(mode) && modes.Contains(mode))
                    {
                        return mode;
                    }
                }

                return modes.GetRandom();
            }
            else
            {
                Queues.Messages.Add("All modes excluded. Nothing left to spawn.");
            }

            return RaidableMode.Random;
        }

        private RaidableMode GetRandomDifficulty(List<RaidableMode> modes)
        {
            var elements = modes.ToDictionary(x => x, config.Settings.Management.Chances.Get);
            decimal chance = Convert.ToDecimal(Core.Random.Range(0.0, 100.0));
            decimal cumulative = 0.0m;

            foreach (var (mode, amount) in elements)
            {
                cumulative += amount;

                if (chance < cumulative)
                {
                    return mode;
                }
            }

            return modes.GetRandom();
        }

        private bool FileExists(string file)
        {
            if (!file.Contains(Path.DirectorySeparatorChar))
            {
                return Interface.Oxide.DataFileSystem.ExistsDatafile($"copypaste{Path.DirectorySeparatorChar}{file}");
            }

            return Interface.Oxide.DataFileSystem.ExistsDatafile(file);
        }

        private bool IsBuildingAllowed(RaidableType type, RaidableMode search, RaidableMode found, bool allowPVP) => (search != RaidableMode.Random && search != found) ? false : type switch
        {
            RaidableType.Purchased when !CanSpawnDifficultyToday(found) => (Queues.Messages.Add("Cannot spawn difficulty today", found), false).Item2,
            RaidableType.Purchased when !config.Settings.Buyable.BuyPVP && allowPVP => (Queues.Messages.Add("Buyable Events is configured to block PVP purchases.", found), false).Item2,
            RaidableType.Maintained or RaidableType.Scheduled when !CanSpawnDifficultyToday(found) => (Queues.Messages.Add("Cannot spawn difficulty today", found), false).Item2,
            _ => true
        };

        private bool CanSpawnDifficultyToday(RaidableMode mode) => mode switch { RaidableMode.Easy => GetDifficultyDay(config.Settings.Management.Easy), RaidableMode.Medium => GetDifficultyDay(config.Settings.Management.Medium), RaidableMode.Hard => GetDifficultyDay(config.Settings.Management.Hard), RaidableMode.Expert => GetDifficultyDay(config.Settings.Management.Expert), RaidableMode.Nightmare => GetDifficultyDay(config.Settings.Management.Nightmare), _ => false };

        private bool GetDifficultyDay(DayLimitSettings ds) => DateTime.Now.DayOfWeek switch { DayOfWeek.Monday => ds.Monday, DayOfWeek.Tuesday => ds.Tuesday, DayOfWeek.Wednesday => ds.Wednesday, DayOfWeek.Thursday => ds.Thursday, DayOfWeek.Friday => ds.Friday, DayOfWeek.Saturday => ds.Saturday, _ => ds.Sunday };

        #endregion

        #region Commands

        [ConsoleCommand("ui_buyraid")]
        private void ccmdBuyRaid(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs())
            {
                return;
            }

            var player = arg.Player();

            if (player.IsNull() || player.IPlayer == null)
            {
                return;
            }

            if (arg.Args[0] == "closeui")
            {
                UI.DestroyTimer(player, UiType.Buyable);
                CuiHelper.DestroyUi(player, "RB_UI_Buyable");
                return;
            }

            if (arg.Args[0] == "accept_teleport")
            {
                BuyableTeleport(player);
                UI.DestroyUi(player, UiType.Teleport);
                CuiHelper.DestroyUi(player, "RB_UI_Teleport");
                return;
            }

            if (arg.Args[0] == "decline_teleport")
            {
                UI.DestroyUi(player, UiType.Teleport);
                CuiHelper.DestroyUi(player, "RB_UI_Teleport");
                return;
            }

            CommandBuyRaid(player.IPlayer, config.Settings.BuyCommand, arg.Args);
        }

        [ConsoleCommand("rb_ui_move")]
        private void ccmdMovePosition(ConsoleSystem.Arg arg)
        {
            if (!arg.HasArgs() || !(arg.Player() is BasePlayer player))
            {
                return;
            }
            if (!Enum.TryParse<UiType>(arg.Args[0], true, out UiType type))
            {
                return;
            }
            if (arg.Args.Length == 1)
            {
                UI.ShowMoveUi(player, type, true);
                return;
            }
            if (!UI.Offsets.TryGetValue(player.userID, out var ui) || !ui.TryGetValue(type, out var offsets))
            {
                return;
            }
            string[] offsetMin = offsets.Min.Split(' ');
            string[] offsetMax = offsets.Max.Split(' ');
            int n = player.serverInput.IsDown(BUTTON.DUCK) ? 1 : player.serverInput.IsDown(BUTTON.SPRINT) ? 15 : 5;
            switch (arg.Args[1])
            {
                case "left":
                    offsets.Min = $"{Convert.ToSingle(offsetMin[0]) - n} {offsetMin[1]}";
                    offsets.Max = $"{Convert.ToSingle(offsetMax[0]) - n} {offsetMax[1]}";
                    break;
                case "right":
                    offsets.Min = $"{Convert.ToSingle(offsetMin[0]) + n} {offsetMin[1]}";
                    offsets.Max = $"{Convert.ToSingle(offsetMax[0]) + n} {offsetMax[1]}";
                    break;
                case "up":
                    offsets.Min = $"{offsetMin[0]} {Convert.ToSingle(offsetMin[1]) + n}";
                    offsets.Max = $"{offsetMax[0]} {Convert.ToSingle(offsetMax[1]) + n}";
                    break;
                case "down":
                    offsets.Min = $"{offsetMin[0]} {Convert.ToSingle(offsetMin[1]) - n}";
                    offsets.Max = $"{offsetMax[0]} {Convert.ToSingle(offsetMax[1]) - n}";
                    break;
            }
            switch (type)
            {
                case UiType.Buyable: UI.ShowBuyableUi(player, true); break;
                case UiType.Cooldown: UI.ShowBuyableCooldownsUi(player, true); break;
                case UiType.Delay: UI.ShowDelayUi(player, true); break;
                case UiType.Lockout: UI.ShowLockoutsUi(player, true); break;
                case UiType.Status: UI.ShowStatusUi(player, true); break;
            }
        }

        private void CommandReloadConfig(IPlayer user, string command, string[] args)
        {
            if (user.IsServer || user.Player().IsAdmin)
            {
                if (IsGridLoading() || !IsPasteAvailable())
                {
                    Message(user, IsGridLoading() ? "GridIsLoading" : "PasteOnCooldown");
                    return;
                }
                Message(user, "ReloadInit");
                if (command == "rb.reloadconfig")
                {
                    SetOnSun(false);
                    UI.DestroyAll();
                    Message(user, "ReloadConfig");
                    LoadConfig();
                    Automated.IsMaintainedEnabled = config.Settings.Maintained.Enabled;
                    Automated.StartCoroutine(RaidableType.Maintained, user);
                    Automated.IsScheduledEnabled = config.Settings.Schedule.Enabled;
                    Automated.StartCoroutine(RaidableType.Scheduled, user);
                    buyableEnabled = config.Settings.Buyable.Max > 0;
                    Initialize();
                }
                if (command == "rb.reloadprofiles")
                {
                    ServerMgr.Instance.StartCoroutine(LoadProfiles(user));
                }
                if (command == "rb.reloadtables")
                {
                    ServerMgr.Instance.StartCoroutine(ReloadTables(user));
                }
            }
        }

        private void Initialize()
        {
            if (config.Settings.Buyable.Cooldowns == null)
            {
                config.Settings.Buyable.Cooldowns = new();
                data.BuyableCooldowns.Clear();
                SaveConfig();
            }
            if (config.Settings.TeleportMarker)
            {
                Subscribe(nameof(OnMapMarkerAdded));
            }
            else Unsubscribe(nameof(OnMapMarkerAdded));
            Subscribe(nameof(OnPlayerSleepEnded));
            GridController.LoadSpawns();
            SpawnsController.SetupZones(ZoneManager);
            Skins.Clear();
            UpdateUI();
            CreateDefaultFiles();
            SetOnSun(true);
            GridController.Setup();
        }

        private void CommandBuyRaid(IPlayer user, string command, string[] args)
        {
            if (config.Settings.Buyable.UsePermission && !user.HasPermission("raidablebases.buyraid"))
            {
                Message(user, "No Permission");
                return;
            }

            var player = user.Player();

            if (user.IsServer && args.Length >= 1 && args[0].IsSteamId())
            {
                player = BasePlayer.FindByID(ulong.Parse(args[0]));
                args = Array.Empty<string>();
            }
            else if (args.Length > 1 && args[1].IsSteamId())
            {
                player = BasePlayer.FindByID(ulong.Parse(args[1]));
            }

            if (!player.IsReallyValid())
            {
                Message(user, args.Length > 1 ? m("TargetNotFoundId", user.Id, args[1]) : "TargetNotFoundNoId");
                return;
            }

            var buyer = user.Player() ?? player;

            if (player.HasPermission("raidablebases.banned") || player.HasPermission("raidablebases.buyraid.banned"))
            {
                Message(player, player.IsAdmin ? "BannedAdmin" : "Banned", player.UserIDString);
                return;
            }

            if (args.Length == 0)
            {
                if (Interface.CallHook("OnPurchaseBase", buyer, player) != null)
                {
                    return;
                }

                if (config.UI.Buyable.Enabled)
                {
                    UI.ShowBuyableUi(player, false);
                }
                else
                {
                    Message(buyer, "BuySyntax", config.Settings.BuyCommand, user.IsServer ? "ID" : user.Id);
                }
                return;
            }

            if (args[0].ToLower() == "reset" && config.Settings.Buyable.Cooldowns.Costs.Any)
            {
                CommandBuyRaidTakePayments(user, buyer, player, Array.Empty<string>());
                return;
            }

            string value = args[0];
            RaidableMode mode = GetRaidableMode(value, buyer);

            if (!buyableEnabled && !buyer.HasPermission("raidablebases.canbypass"))
            {
                Message(buyer, "BuyRaidsDisabled");
                return;
            }

            if (!IsCopyPasteLoaded(out var error))
            {
                Message(buyer, error);
                return;
            }

            if (!CanSpawnDifficultyToday(mode))
            {
                if (!CanFileMode(buyer)) Message(buyer, "No Permission To Buy File", value);
                else if (!FileExists(value)) Message(buyer, "FileDoesNotExist2", value);
                else Message(buyer, "BuyDifficultyNotAvailableToday", mode);
                return;
            }

            if (IsGridLoading())
            {
                Message(buyer, "GridIsLoading");
                return;
            }

            if (mode == RaidableMode.Random || !IsDifficultyAvailable(mode, false))
            {
                Message(buyer, "BuyAnotherDifficulty", value);
                return;
            }

            if (!IsDifficultyAvailable(mode, true))
            {
                Message(buyer, "BuyRaidNotConfiguredProperly");
                return;
            }

            if (Get(RaidableType.Purchased) >= config.Settings.Buyable.Max)
            {
                Message(buyer, "Max Events", command, config.Settings.Buyable.Max);
                return;
            }

            if (config.Settings.Buyable.Limits.Any)
            {
                int max = config.Settings.Buyable.Limits.Get(mode);

                if (max < 0 || max > 0 && Get(mode, true) >= max)
                {
                    Message(buyer, "Max Events", mode, max);
                    return;
                }
            }

            if (!bypassRestarting && ServerMgr.Instance.Restarting)
            {
                Message(buyer, buyer.IsAdmin ? "BuyableServerRestartingAdmin" : "BuyableServerRestarting");
                return;
            }

            if (SaveRestore.IsSaving)
            {
                Message(buyer, "BuyableServerSaving");
                return;
            }

            if (IsEventOwner(player, true))
            {
                Message(buyer, "BuyableAlreadyOwner");
                return;
            }

            if (!buyer.HasPermission("raidablebases.buyable.bypass.cooldown"))
            {
                if (BuyableInfo.GetTimeRemaining(this, buyer, mode, true) > 0) return;
                else if (Raids.Exists(raid => raid.HasBuyableCooldown(buyer, mode))) return;
            }

            if (IsQueued(player, GetMembers(buyer.userID)))
            {
                return;
            }

            if (!Buildings.Profiles.Exists(pair => pair.Value.Options.Mode == mode && pair.Value.Options.Permission.Has(player, RaidableType.Purchased)))
            {
                Message(player, "No Permission To Buy");
                return;
            }

            CuiHelper.DestroyUi(player, "RB_UI_Buyable");

            if (Interface.CallHook("OnPurchaseTakePayments", buyer, player, value) is object obj && obj != null)
            {
                Message(player, obj is string str ? str : "No Permission");
                return;
            }

            CommandBuyRaidTakePayments(user, buyer, player, args, false, mode, value);
        }

        public void CommandBuyRaidTakePayments(IPlayer user, BasePlayer buyer, BasePlayer player, string[] args, bool reset = true, RaidableMode mode = RaidableMode.Disabled, string value = null)
        {
            var payments = new Payments(buyer);
            var money = reset ? config.Settings.Buyable.Cooldowns.Costs.Money : config.Settings.Include.Economics ? config.Settings.Economics.Get(mode) : 0.0;
            var points = reset ? config.Settings.Buyable.Cooldowns.Costs.Points : config.Settings.Include.ServerRewards ? config.Settings.ServerRewards.Get(mode) : 0;
            var options = reset ? new() { config.Settings.Buyable.Cooldowns.Costs.Custom } : config.Settings.Include.Custom && config.Settings.Custom.TryGetValue(GetModeText(mode), out var val) ? val : new();
            var free = args.Contains("free") && user != null && user.IsAdmin;
            if (free)
            {
                InitializeFreePayments(buyer, player, payments);
            }
            if (InvalidCustomPayment(buyer, player, payments, options, free))
            {
                return;
            }
            if (InvalidEconomicsPayment(buyer, player, payments, money, free))
            {
                return;
            }
            if (InvalidServerRewardsPayment(buyer, player, payments, points, free))
            {
                return;
            }
            if (payments.valid)
            {
                ProcessValidPayments(buyer, player, payments, mode, reset, free, value, args);
            }
            else
            {
                ProcessInvalidPayments(buyer, options, money, points);
            }
        }

        private void InitializeFreePayments(BasePlayer buyer, BasePlayer player, Payments payments)
        {
            payments.Custom = new(this, buyer, player, new());
            payments.Economics = new(this, buyer, player);
            payments.ServerRewards = new(this, buyer, player);
        }

        private bool InvalidCustomPayment(BasePlayer buyer, BasePlayer player, Payments payments, List<CustomCostOptions> options, bool free)
        {
            return !free && options.Count > 0 && options.All(o => o.isValid) && (payments.Custom = TryBuyRaidCustom(options, buyer, player)) == null;
        }

        private bool InvalidEconomicsPayment(BasePlayer buyer, BasePlayer player, Payments payments, double money, bool free)
        {
            return !free && money > 0 && (Economics.CanCall() || IQEconomic.CanCall()) && (payments.Economics = TryBuyRaidEconomics(money, buyer, player)) == null;
        }

        private bool InvalidServerRewardsPayment(BasePlayer buyer, BasePlayer player, Payments payments, int points, bool free)
        {
            return !free && points > 0 && ServerRewards.CanCall() && (payments.ServerRewards = TryBuyRaidServerRewards(points, buyer, player)) == null;
        }

        private void ProcessValidPayments(BasePlayer buyer, BasePlayer player, Payments payments, RaidableMode mode, bool reset, bool free, string value, string[] args)
        {
            if (!reset)
            {
                if (value != null && GetFileMode(value, buyer) == RaidableMode.Random) value = null;

                payments.type = args.Contains("pve") ? 1 : args.Contains("pvp") ? 2 : 0;

                payments.Take(false);

                BuyRaid(mode, payments, player, value, free);
            }
            else if (data.BuyableCooldowns.Remove(player.userID))
            {
                payments.Take(true);
                UI.UpdateUi(player, UiType.Cooldown);
                Message(buyer, "RemovedCooldownFor", player.displayName, player.UserIDString);
            }
        }

        private void ProcessInvalidPayments(BasePlayer buyer, List<CustomCostOptions> options, double money, int points)
        {
            if (options.Count > 0 && (!config.Settings.Include.Custom && options.Exists(o => o.Enabled) || !options.Exists(o => o.Enabled)))
            {
                Message(buyer, "CustomWithdrawDisabled");
            }
            else if (money > 0 && (!Economics.CanCall() && !IQEconomic.CanCall() && !BankSystem.CanCall()))
            {
                Message(buyer, "EconomicsWithdrawDisabled");
            }
            else if (points > 0 && !ServerRewards.CanCall())
            {
                Message(buyer, "ServerRewardPointsDisabled");
            }
            else if (money == 0 && config.Settings.Include.Economics && (Economics.CanCall() || IQEconomic.CanCall() || BankSystem.CanCall()))
            {
                Message(buyer, "NoBuyableEventsCostConfigured");
            }
            else if (points == 0 && config.Settings.Include.ServerRewards && ServerRewards.CanCall())
            {
                Message(buyer, "NoBuyableEventsCostConfigured");
            }
            else Message(buyer, "NoBuyableEventsCostConfigured");
        }

        public bool IsQueued(BasePlayer player, HashSet<ulong> members)
        {
            foreach (ulong member in members)
            {
                foreach (var sp in Queues.queue)
                {
                    if (sp.type == RaidableType.Purchased && sp.userid == member)
                    {
                        Message(player, player.userID == sp.userid ? "BuyableAlreadyQueued" : "BuyableAlreadyQueuedAllied");

                        return true;
                    }
                }
            }
            return false;
        }

        private void CommandBlockRaids(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 1) { Player.Message(player, "You must specify a radius"); return; }
            if (!float.TryParse(args[1], out var radius) || radius <= 5f) { Player.Message(player, $"Invalid radius {args[1]} specified"); return; }
            if (config.Settings.Management.BlockedPositions.RemoveAll(x => InRange(player.transform.position, x.position, radius)) == 0)
            {
                config.Settings.Management.BlockedPositions.Add(new(player.transform.position, radius));
                Player.Message(player, $"Block added; raids will no longer spawn within {radius}m of this position");
                SaveConfig();
            }
            else Player.Message(player, "Block removed; raids are now allowed to spawn at this position");
        }

        private void CommandRaidHunter(IPlayer user, string command, string[] args)
        {
            var player = user.Player();
            bool isAdmin = user.IsServer || player.IsAdmin;
            string arg = args.Length >= 1 ? args[0].ToLower() : string.Empty;

            switch (arg)
            {
                case "pvp":
                    {
                        var nearest = GetNearestBase(player.transform.position);
                        if (nearest == null || !nearest.ownerId.IsSteamId() || !nearest.IsAlly(player))
                        {
                            Message(player, "CommandNotAllowed");
                            return;
                        }
                        nearest._currentSphereColor = SphereColor.None;
                        nearest.AllowPVP = true;
                        nearest.UpdateMarker();
                        nearest.CreateSpheres();
                        return;
                    }
                case "blockraids":
                    {
                        if (isAdmin)
                        {
                            CommandBlockRaids(player, command, args);
                        }
                        return;
                    }
                case "version":
                    {
                        Message(user, $"Version: {Version}");
                        return;
                    }
                case "invite":
                    {
                        CommandInvite(user, player, args);
                        return;
                    }
                case "resettime":
                    {
                        if (isAdmin)
                        {
                            data.RaidTime = DateTime.MinValue;
                        }

                        return;
                    }
                case "wipe":
                    {
                        if (isAdmin)
                        {
                            wiped = true;
                            CheckForWipe();
                        }

                        return;
                    }
                case "ignore_restart":
                    {
                        if (isAdmin)
                        {
                            bypassRestarting = !bypassRestarting;
                            Message(user, $"Bypassing restart check: {bypassRestarting}");
                        }

                        return;
                    }
                case "savefix":
                    {
                        if (user.IsAdmin || user.HasPermission("raidablebases.allow"))
                        {
                            int removed = BaseEntity.saveList.RemoveWhere(e => e.IsKilled());

                            Message(user, $"Removed {removed} invalid entities from the save list.");

                            if (SaveRestore.IsSaving)
                            {
                                SaveRestore.IsSaving = false;
                                Message(user, "Server save has been canceled. You must type server.save again, and then restart your server.");
                            }
                            else Message(user, "Server save is operating normally.");
                        }

                        return;
                    }
                case "tp":
                    {
                        if (player.IsReallyValid() && (isAdmin || user.HasPermission("raidablebases.allow")))
                        {
                            RaidableBase raid = null;
                            float num = 9999f;

                            foreach (var other in Raids)
                            {
                                float num2 = player.Distance(other.Location);

                                if (num2 > other.ProtectionRadius * 2f && num2 < num)
                                {
                                    num = num2;
                                    raid = other;
                                }
                            }

                            if (raid != null)
                            {
                                raid.Teleport(player);
                            }
                        }
                        else CommandRaidHunter(user, command, new string[1] { "teleport" });

                        return;
                    }
                case "isblocked":
                    {
                        if (isAdmin)
                        {
                            Message(user, "IsLocationBlocked: " + SpawnsController.IsLocationBlocked(new(user.Position().X, user.Position().Y, user.Position().Z)).ToString());
                        }
                        return;
                    }
                case "grid":
                    {
                        if (player.IsReallyValid() && (isAdmin || user.HasPermission("raidablebases.ddraw")))
                        {
                            ShowGrid(player, args.Length == 2 && args[1] == "all");
                        }
                        return;
                    }
                case "ladder":
                case "lifetime":
                    {
                        ShowLadder(user, args);
                        return;
                    }
                case "queue_clear":
                    {
                        if (isAdmin)
                        {
                            int num = Queues.queue.Count;
                            Queues.StartCoroutine();
                            Message(user, $"Cleared and refunded {num} in the queue.");
                        }
                        return;
                    }
                case "resetui":
                    {
                        UiHandler.DestroyUi(player);
                        if (UI.Offsets.TryGetValue(player.userID, out var ui))
                        {
                            if (args.Length == 1) UI.Offsets.Remove(player.userID);
                            if (args.Contains("buyable")) ui.Remove(UiType.Buyable);
                            if (args.Contains("cooldown")) ui.Remove(UiType.Cooldown);
                            if (args.Contains("delay")) ui.Remove(UiType.Delay);
                            if (args.Contains("lockout")) ui.Remove(UiType.Lockout);
                            if (args.Contains("status")) ui.Remove(UiType.Status);
                            Message(player, "ResetUI");
                        }
                        UI.UpdateUi(player, UiType.Cooldown);
                        UI.UpdateUi(player, UiType.Delay);
                        UI.UpdateUi(player, UiType.Lockout);
                        UI.UpdateUi(player, UiType.Status);
                        Message(player, "Your UI settings have been reset to defaults.");
                        return;
                    }
            }

            if (config.RankedLadder.Enabled)
            {
                PlayerInfo playerInfo = PlayerInfo.Get(data, user.Id);

                user.Reply(m("RankedWins1", user.Id, playerInfo.Raids, playerInfo.Points, playerInfo.Easy, playerInfo.Medium, playerInfo.Hard, playerInfo.Expert, playerInfo.Nightmare, playerInfo.TotalRaids, playerInfo.TotalPoints, playerInfo.TotalEasy, playerInfo.TotalMedium, playerInfo.TotalHard, playerInfo.TotalExpert, playerInfo.TotalNightmare, config.Settings.HunterCommand));
                user.Reply(m("RankedWins2", user.Id, config.Settings.HunterCommand));
            }

            if (Automated.IsScheduledEnabled && (Raids.Count == 0 || !Automated.IsMaintainedEnabled) && GridController.GetRaidTime() > 0)
            {
                ShowNextScheduledEvent(user);
            }

            if (player.IsReallyValid())
            {
                DrawRaidLocations(player, isAdmin || player.HasPermission("raidablebases.ddraw"));
            }
        }

        private void CommandInvite(IPlayer user, BasePlayer player, string[] args)
        {
            if (args.Length < 2) { Message(user, "Invite Usage", config.Settings.HunterCommand); return; }
            if (!(RustCore.FindPlayer(args[1]) is BasePlayer target)) { Message(user, "TargetNotFoundId", args[1]); return; }
            var isAllowed = user.IsServer || player.IsAdmin || player.HasPermission("fauxadmin.allowed");
            var raid = isAllowed ? GetNearestBase(target.transform.position) : Raids.FirstOrDefault(x => x.ownerId.IsSteamId() && x.IsAlly(player.userID, x.ownerId));
            if (raid == null) { Message(user, "Invite Ownership Error"); return; }
            if (!isAllowed && !raid.IsAlly(player.userID, target.userID)) { Message(user, "Invite Not Ally"); return; }
            if (!isAllowed && !raid.IsPayLocked && raid.HasLockout(target)) { Message(user, "Invite Lockout Error"); Message(target, "Invite Failed"); return; }
            if (!raid.raiders.TryGetValue(target.userID, out var raider)) raid.raiders[target.userID] = raider = new(target);
            if (InRange(raid.Location, target.transform.position, raid.ProtectionRadius * 1.5f)) raider.lastActiveTime = Time.time;
            if (user.IsServer || player.IsAdmin || user.HasPermission("raidablebases.allow")) Message(user, $"You can use this command to set them as the owner of this raid: {config.Settings.EventCommand} setowner {target.userID}");
            raider.IsAlly = true;
            raider.IsAllowed = true;
            raider.IsRaider = true;
            Message(target, "Invite Allowed", user.Name);
            Message(user, "Invite Success", target.displayName);
        }

        protected void DrawRaidLocations(BasePlayer player, bool hasPerm)
        {
            if (!player.HasPermission("raidablebases.block.filenames") && !player.IsAdmin && !player.IsDeveloper)
            {
                foreach (var raid in Raids)
                {
                    if (InRange2D(raid.Location, player.transform.position, 100f))
                    {
                        Player.Message(player, $"{raid.BaseName} @ {raid.Location} ({PhoneController.PositionToGridCoord(raid.Location)})");
                    }
                }
            }

            if (hasPerm)
            {
                AdminCommand(player, () =>
                {
                    foreach (var raid in Raids)
                    {
                        int num = BasePlayer.activePlayerList.Count(x => x.IsReallyValid() && x.Distance(raid.Location) <= raid.ProtectionRadius * 3f);
                        int distance = Mathf.CeilToInt(player.transform.position.Distance(raid.Location));
                        string message = mx("RaidMessage", player.UserIDString, distance, num);
                        string flag = mx(raid.GetAllowKey(), player.UserIDString);

                        DrawText(player, 15f, Color.yellow, raid.Location, string.Format("<size=24>{0}{1} {2} [{3} {4}] {5}</size>", raid.BaseName, flag, raid.Mode(player.UserIDString, true), message, FormatGridReference(player, raid.Location), raid.Location));

                        foreach (var ri in raid.raiders.Values.Where(x => x.IsAlly && x.player.IsReallyValid()))
                        {
                            DrawText(player, 15f, Color.yellow, ri.player.transform.position, $"<size=24>{mx("Ally", player.UserIDString).Replace(":", string.Empty)}</size>");
                        }

                        if (raid.ownerId.IsSteamId() && raid.GetOwner() is BasePlayer owner && !owner.IsKilled())
                        {
                            DrawText(player, 15f, Color.yellow, owner.transform.position, $"<size=24>{mx("Owner", player.UserIDString).Replace(":", string.Empty)}</size>");
                        }
                    }
                });
            }
        }

        protected void ShowNextScheduledEvent(IPlayer user)
        {
            string message;
            double time = GridController.GetRaidTime();

            if (BasePlayer.activePlayerList.Count < config.Settings.Schedule.PlayerLimit)
            {
                message = mx("Not Enough Online", user.Id, config.Settings.Schedule.PlayerLimit);
            }
            else if (BasePlayer.activePlayerList.Count > config.Settings.Schedule.PlayerLimitMax)
            {
                message = mx("Too Many Online", user.Id, config.Settings.Schedule.PlayerLimitMax);
            }
            else message = FormatTime(time, user.Id);

            user.Reply(m("Next", user.Id, message));
        }

        protected void ShowLadder(IPlayer user, string[] args)
        {
            if (!config.RankedLadder.Enabled || config.RankedLadder.Top < 1)
            {
                return;
            }

            if (args.Length == 2 && args[1].ToLower() == "resetme")
            {
                data.Players[user.Id] = new();
                user.Reply("Your ranked stats have been reset.");
                return;
            }

            string key = args[0].ToLower();
            var mode = args.Length == 2 ? GetRaidableMode(args[1]) : RaidableMode.Points;

            if (data.Players.Count == 0)
            {
                user.Reply(m("Ladder Insufficient Players", user.Id));
                return;
            }

            int rank = 0;
            bool isByWipe = key == "ladder";
            List<(string, int)> ladder = GetLadder(key, mode);

            ladder.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            user.Reply(m(isByWipe ? "RankedLadder" : "RankedTotal", user.Id, config.RankedLadder.Top, mode));

            foreach (var (userid, _) in ladder.Take(config.RankedLadder.Top))
            {
                NotifyPlayer(user, ++rank, userid, isByWipe, mode);
            }
        }

        private bool IsRaidableMode(string value) => GetRaidableMode(value) != RaidableMode.Random;

        private RaidableMode GetRaidableMode(string value, BasePlayer buyer = null) => value switch
        {
            null or "" => RaidableMode.Random,
            _ when IsEasy(value.ToLower()) => RaidableMode.Easy,
            _ when IsMedium(value.ToLower()) => RaidableMode.Medium,
            _ when IsHard(value.ToLower()) => RaidableMode.Hard,
            _ when IsExpert(value.ToLower()) => RaidableMode.Expert,
            _ when IsNightmare(value.ToLower()) => RaidableMode.Nightmare,
            _ => GetFileMode(value, buyer)
        };

        private bool CanFileMode(BasePlayer buyer) => config.Settings.Buyable.FileMode || buyer.HasPermission("raidablebasesbuyableui.spawn.filenames") || buyer.HasPermission("raidablebases.buyable.spawn.filenames");

        private RaidableMode GetFileMode(string value, BasePlayer buyer)
        {
            return CanFileMode(buyer) && Get(value, out (string key, BaseProfile profile) val) ? val.profile.Options.Mode : RaidableMode.Random;
        }

        private bool Get(string baseName, out (string, BaseProfile) val)
        {
            foreach (var (key, profile) in Buildings.Profiles)
            {
                if (key.Equals(baseName, StringComparison.OrdinalIgnoreCase) || profile.Options.AdditionalBases.Exists(extra => extra.Key.Equals(baseName, StringComparison.OrdinalIgnoreCase)))
                {
                    val = (key, profile);
                    return true;
                }
            }
            val = default;
            return false;
        }

        protected void ShowGrid(BasePlayer player, bool showAll)
        {
            AdminCommand(player, () =>
            {
                if (!GridController.Spawns.TryGetValue(RaidableType.Grid, out var spawns))
                {
                    return;
                }

                foreach (var rsl in spawns.Spawns.Union(spawns.Seabed))
                {
                    if (showAll || InRange2D(rsl.Location, player.transform.position, 1000f))
                    {
                        DrawText(player, 30f, Color.green, rsl.Location, "X");
                    }
                }

                foreach (CacheType cacheType in Enum.GetValues(typeof(CacheType)))
                {
                    (Color color, string text) = cacheType switch
                    {
                        CacheType.Generic => (Color.red, "X"),
                        CacheType.Temporary => (Color.cyan, "C"),
                        CacheType.Privilege => (Color.yellow, "TC"),
                        CacheType.Seabed or CacheType.Submerged => (Color.blue, "W"),
                        _ => (Color.red, "X")
                    };

                    foreach (var rsl in spawns.Inactive(cacheType))
                    {
                        if (showAll || InRange2D(rsl.Location, player.transform.position, 1000f))
                        {
                            DrawText(player, 30f, color, rsl.Location, text);
                        }
                    }
                }

                foreach (var cmi in SpawnsController.Monuments)
                {
                    DrawSphere(player, 30f, Color.blue, cmi.position, cmi.radius);
                    DrawText(player, 30f, Color.cyan, cmi.position, $"<size=16>{cmi.text} ({cmi.radius})</size>");
                }
            });
        }

        protected List<(string, int)> GetLadder(string arg, RaidableMode mode)
        {
            var ladder = new List<(string, int)>();
            bool isLadder = arg.ToLower() == "ladder";

            foreach (var (userid, info) in data.Players)
            {
                int value = mode switch
                {
                    RaidableMode.Points => isLadder ? info.Points : info.TotalPoints,
                    RaidableMode.Easy => isLadder ? info.Easy : info.TotalEasy,
                    RaidableMode.Medium => isLadder ? info.Medium : info.TotalMedium,
                    RaidableMode.Hard => isLadder ? info.Hard : info.TotalHard,
                    RaidableMode.Expert => isLadder ? info.Expert : info.TotalExpert,
                    RaidableMode.Nightmare => isLadder ? info.Nightmare : info.TotalNightmare,
                    _ => 0
                };

                if (value > 0)
                {
                    ladder.Add(new(userid, value));
                }
            }

            return ladder;
        }

        protected void NotifyPlayer(IPlayer user, int rank, string key, bool isByWipe, RaidableMode mode)
        {
            PlayerInfo info = PlayerInfo.Get(data, key);

            info.ResetExpiredDate();

            (int wipe, int lifetime) = (mode, isByWipe) switch
            {
                (RaidableMode.Easy, _) => (isByWipe ? info.Easy : info.TotalEasy, isByWipe ? info.EasyPoints : info.TotalEasyPoints),
                (RaidableMode.Medium, _) => (isByWipe ? info.Medium : info.TotalMedium, isByWipe ? info.MediumPoints : info.TotalMediumPoints),
                (RaidableMode.Hard, _) => (isByWipe ? info.Hard : info.TotalHard, isByWipe ? info.HardPoints : info.TotalHardPoints),
                (RaidableMode.Expert, _) => (isByWipe ? info.Expert : info.TotalExpert, isByWipe ? info.ExpertPoints : info.TotalExpertPoints),
                (RaidableMode.Nightmare, _) => (isByWipe ? info.Nightmare : info.TotalNightmare, isByWipe ? info.NightmarePoints : info.TotalNightmarePoints),
                (RaidableMode.Points or _, _) => (isByWipe ? info.Raids : info.TotalRaids, isByWipe ? info.Points : info.TotalPoints),
            };

            string name = covalence.Players.FindPlayerById(key)?.Name ?? key;
            string message = lang.GetMessage("NotifyPlayerFormat", this, user.Id);

            message = message.Replace("{rank}", rank.ToString());
            message = message.Replace("{name}", name);
            message = message.Replace("{value}", wipe.ToString());
            message = message.Replace("{points}", lifetime.ToString());

            user.Reply(message);
        }

        private void CommandRaidBase(IPlayer user, string command, string[] args)
        {
            var player = user.Player();
            bool isAllowed = user.IsServer || player.IsAdmin || user.HasPermission("raidablebases.allow");
            if (!CanCommandContinue(player, user, isAllowed, args))
            {
                return;
            }
            if (command == config.Settings.EventCommand) // rbe
            {
                ProcessEventCommand(user, player, isAllowed, args);
            }
            else if (command == config.Settings.ConsoleCommand) // rbevent
            {
                ProcessConsoleCommand(user, player, isAllowed, args);
            }
        }

        protected void ProcessEventCommand(IPlayer user, BasePlayer player, bool isAllowed, string[] args) // rbe
        {
            if (!isAllowed || !player.IsReallyValid())
            {
                return;
            }

            var baseName = Array.Find(args, FileExists);
            var mode = GetRaidableMode(Array.Find(args, IsRaidableMode));
            var (key, profile) = GetBuilding(RaidableType.Manual, mode, baseName, null);

            if (!IsProfileValid(key, profile, true, RaidableType.Manual))
            {
                user.Reply(profile == null ? m("BuildingNotConfigured", user.Id) : GetDebugMessage(mode, false, true, user.Id, key, profile.Options));
                return;
            }

            if (!Physics.Raycast(player.eyes.HeadRay(), out var hit, isAllowed ? Mathf.Infinity : 100f, targetMask2, QueryTriggerInteraction.Ignore))
            {
                user.Reply(m("LookElsewhere", user.Id));
                return;
            }

            var safeRadius = Mathf.Max(M_RADIUS * 2f, profile.Options.ArenaWalls.Radius);
            var safe = player.IsAdmin || SpawnsController.IsAreaSafe(hit.point, safeRadius, safeRadius, safeRadius, manualMask, false, out _, RaidableType.Manual, profile.Options.CustomSpawns);

            if (!safe && !player.IsFlying && InRange(player.transform.position, hit.point, 50f))
            {
                user.Reply(m("PasteIsBlockedStandAway", user.Id));
                return;
            }

            bool pasted = false;

            if (safe && (isAllowed || !SpawnsController.IsMonumentPosition(hit.point, profile.Options.ProtectionRadius(RaidableType.Manual))))
            {
                var spawns = GridController.Spawns.Values.FirstOrDefault(s => s.GetLocations(CacheType.Generic).Exists(t => InRange2D(t.Location, hit.point, M_RADIUS)) || s.GetLocations(CacheType.Seabed).Exists(t => InRange2D(t.Location, hit.point, M_RADIUS)));
                var point = hit.point + new Vector3(0f, profile.Options.Setup.PasteHeightAdjustment);
                var rb = new RandomBase(this, key, profile, point, RaidableType.Manual, spawns, null);
                ParseListedOptions(rb);
                if (profile.Options.Setup.ForcedHeight != -1)
                {
                    point.y = profile.Options.Setup.ForcedHeight;
                }
                point.y += rb.baseHeight;
                if (PasteBuilding(rb))
                {
                    DrawText(player, 10f, Color.red, point, rb.BaseName);
                    if (ConVar.Server.hostname.Contains("Test Server"))
                    {
                        DrawSphere(player, 30f, Color.blue, point, rb.pasteData.radius);
                    }
                    pasted = true;
                }
            }
            else user.Reply(m("PasteIsBlocked", user.Id));

            if (!pasted && Queues.Messages.Any())
            {
                user.Reply(IsGridLoading() ? m("GridIsLoading") : Queues.Messages.GetLast(user.Id));
            }
        }

        protected void ProcessConsoleCommand(IPlayer user, BasePlayer player, bool isAllowed, string[] args) // rbevent
        {
            if (IsGridLoading() && !user.IsAdmin)
            {
                int count = GridController.Spawns.ContainsKey(RaidableType.Grid) ? GridController.Spawns[RaidableType.Grid].Spawns.Count : 0;
                user.Reply(m("GridIsLoadingFormatted", user.Id, (Time.realtimeSinceStartup - GridController.gridTime).ToString("N02"), count));
                return;
            }

            Message(player, "BaseQueued", Queues.queue.Count);

            SpawnRandomBase(RaidableType.Manual, GetRaidableMode(Array.Find(args, IsRaidableMode)), Array.Find(args, FileExists), isAllowed, null, null, isAllowed && user.IsConnected ? user : null);
        }

        private bool CanCommandContinue(BasePlayer player, IPlayer user, bool isAllowed, string[] args)
        {
            if (HandledCommandArguments(player, user, isAllowed, args))
            {
                return false;
            }

            if (!IsCopyPasteLoaded(out var error))
            {
                Message(user, error);
                return false;
            }

            if (!isAllowed && Get(RaidableType.Manual) >= config.Settings.Manual.Max)
            {
                user.Reply(m("Max Events", user.Id, RaidableType.Manual, config.Settings.Manual.Max));
                return false;
            }

            return true;
        }

        private bool HandledCommandArguments(BasePlayer player, IPlayer user, bool isAllowed, string[] args)
        {
            if (args.Length == 0)
            {
                return false;
            }

            switch (args[0].ToLower())
            {
                case "despawn":
                    if (player.IsReallyValid() && (isAllowed || player.HasPermission("raidablebases.despawn.buyraid")))
                    {
                        DespawnBase(player, isAllowed);
                    }
                    return true;
                case "draw":
                    if (player.IsReallyValid())
                    {
                        DrawSpheres(player, isAllowed);
                    }
                    return true;
                case "checkflat":
                    {
                        if (!isAllowed) return false;
                        if (args.Length != 2 || !float.TryParse(args[1], out var radius)) radius = 50f;
                        Message(user, SpawnsController.IsObstructed(player.transform.position, radius, 2.5f, -1f, player.IsHeadUnderwater(), player) ? "Obstruction test failed" : "Obstruction test passed");
                        var landLevel = SpawnsController.GetLandLevel(player.transform.position, radius, player.IsHeadUnderwater(), player);
                        DrawText(player, 30f, Color.red, player.transform.position, $"{landLevel.y - landLevel.x:N01}");
                        return true;
                    }
                case "debug":
                    {
                        if (!isAllowed) return false;
                        DebugMode = !DebugMode;
                        Queues.Messages._user = DebugMode ? user : null;
                        Message(user, $"Debug mode (v{Version}): {DebugMode}");
                        ConfigCheckFrames(user);
                        if (DebugMode)
                        {
                            Message(user, $"Scheduled Events Running: {Automated._scheduledCoroutine != null}");
                            Message(user, $"Maintained Events Running: {Automated._maintainedCoroutine != null}");
                            Message(user, $"Queues Pending: {Queues.queue.Count}");
                            if (!AnyCopyPasteFileExists && !GridController.BadFrameRate)
                            {
                                Message(user, "No copypaste file in any profile exists!");
                            }
                            if (Queues.Messages.Any())
                            {
                                Message(user, $"DEBUG: Last messages:");
                                Queues.Messages.PrintAll(user);
                            }
                            else Message(user, "No debug messages.");
                            if (exConf is JsonException)
                            {
                                Message(user, $"{exConf.Message}\n\n\nYour config contains a json error!");
                            }
                            foreach (var error in profileErrors)
                            {
                                Message(user, $"Json error found in {error}");
                            }
                            foreach (var (type, spawns) in GridController.Spawns)
                            {
                                if (spawns.Spawns.Count > 0)
                                {
                                    Message(user, $"Potential points on {type}: {spawns.Spawns.Count}");
                                }
                            }
                        }
                        return true;
                    }
                case "kill_cleanup":
                    {
                        if (!isAllowed || !player) return false;
                        var num = 0;
                        foreach (var entity in FindEntitiesOfType<BaseEntity>(player.transform.position, 100f))
                        {
                            if (entity.OwnerID == 0 && IsKillableEntity(entity))
                            {
                                entity.SafelyKill();
                                num++;
                            }
                        };
                        if (num == 0) Message(user, "You must use the command near the base that you want to despawn. It cannot be owned by a player.");
                        else Message(user, $"Kill sent for {num} entities.");
                        return true;
                    }
                case "despawnall":
                case "despawn_inactive":
                    {
                        if (isAllowed && Raids.Count > 0)
                        {
                            DespawnAll(args[0].ToLower() == "despawn_inactive");
                            Puts(mx("DespawnedAll", null, user.Name));
                        }

                        return true;
                    }
                case "generateloot":
                    {
                        if (isAllowed)
                        {
                            RaidableMode mode = args.Length > 1 ? GetRaidableMode(args[1]) : RaidableMode.Random;
                            if (mode == RaidableMode.Random) mode = GetRaidableModes().GetRandom();
                            RaidableBase.GenerateLoot(this, user, mode, args);
                        }
                        return true;
                    }
                case "active":
                    {
                        if (!isAllowed) return false;

                        var sb = new StringBuilder();

                        sb.AppendLine($"Queue: {Queues.queue.Count}, Raids: {Raids.Count}");

                        foreach (var spq in Queues.queue)
                        {
                            sb.AppendLine($"{spq.type} ({spq.options.Mode}) with {spq.attempts} attempts");
                        }

                        foreach (var raid in Raids)
                        {
                            sb.AppendLine($"{raid.Type}: {raid.Options.Mode} is {raid.GetPercentComplete()}% done at {raid.Location} in {PositionToGrid(raid.Location)} ({raid.GetPercentCompleteMessage()}) {raid.DespawnString}");
                        }

                        foreach (var (type, spawns) in GridController.Spawns)
                        {
                            sb.AppendLine($"{type} with {spawns.Spawns.Count} spawns and {spawns.Cached.Sum(x => x.Value.Count)} cached");
                        }

                        if (config.Settings.Management.RequireAllSpawned)
                        {
                            if (data.Cycle._buildings.Count > 0)
                            {
                                sb.AppendLine("Bases that cannot respawn yet:");
                                foreach (var (mode, buildings) in data.Cycle._buildings)
                                {
                                    sb.AppendLine($"{mode}: {string.Join(", ", buildings)}");
                                }
                            }

                            sb.AppendLine().Append("Bases that can spawn in the current rotation:");

                            RaidableMode current = RaidableMode.Random;

                            foreach (var (key, profile) in Buildings.Profiles)
                            {
                                foreach (var extra in profile.Options.AdditionalBases.Keys)
                                {
                                    if (FileExists(extra) && data.Cycle.CanSpawn(RaidableType.Maintained, profile.Options.Mode, extra, player))
                                    {
                                        if (current != profile.Options.Mode)
                                        {
                                            current = profile.Options.Mode;
                                            sb.AppendLine();
                                        }
                                        sb.Append(extra).Append(" ");
                                    }
                                }
                            }
                        }

                        Message(user, sb.ToString());

                        return true;
                    }
                case "expire":
                case "resetcooldown":
                    {
                        if (!isAllowed) return false;
                        if (args.Length >= 2)
                        {
                            var target = RustCore.FindPlayer(args[1]);

                            if (!target.IsNull())
                            {
                                if (args.Length == 2 || args[2] == "buyable")
                                {
                                    foreach (var raid in Raids)
                                    {
                                        raid.cooldowns.Remove(target.userID);
                                    }
                                    data.BuyableCooldowns.Remove(target.userID);
                                    UI.UpdateUi(target, UiType.Cooldown);
                                    Message(user, "RemovedCooldownFor", target.displayName, target.UserIDString);
                                }
                                if (args.Length == 2 || args[2] == "lockout")
                                {
                                    data.Lockouts.Remove(target.UserIDString);
                                    UI.UpdateUi(target, UiType.Lockout);
                                    user.Reply(m("RemovedLockFor", user.Id, target.displayName, target.UserIDString));
                                }
                            }
                            return true;
                        }
                        Message(user, "Target not found");
                        return true;
                    }
                case "expireall":
                case "resetall":
                    {
                        if (isAllowed)
                        {
                            data.BuyableCooldowns.Clear();
                            data.Lockouts.Clear();
                            foreach (var target in BasePlayer.activePlayerList)
                            {
                                UI.UpdateUi(target, UiType.Cooldown);
                                UI.UpdateUi(target, UiType.Lockout);
                            }
                            Puts($"All cooldowns and lockouts have been reset by {user.Name} ({user.Id})");
                        }
                        return true;
                    }
                case "setowner":
                case "lockraid":
                    {
                        if (args.Length >= 2 && (isAllowed || user.HasPermission("raidablebases.setowner")))
                        {
                            if (RustCore.FindPlayer(args[1]) is BasePlayer target)
                            {
                                if (!(GetNearestBase(target.transform.position) is RaidableBase raid))
                                {
                                    user.Reply(m("TargetTooFar", user.Id));
                                }
                                else if (raid.TrySetPayLock(new(target) { Economics = new(this, target) }))
                                {
                                    user.Reply(m("RaidLockedTo", user.Id, target.displayName));
                                }
                                else user.Reply("You must use clearowner first.");
                            }
                            else user.Reply(m("TargetNotFoundId", user.Id, args[1]));
                        }

                        return true;
                    }
                case "clearowner":
                    {
                        if (player.IsReallyValid() && (isAllowed || user.HasPermission("raidablebases.setowner")))
                        {
                            if (!(GetNearestBase(player.transform.position) is RaidableBase raid))
                            {
                                user.Reply(m("TooFar", user.Id));
                            }
                            else
                            {
                                raid.ResetPublicLock();
                                raid.raiders.Clear();
                                user.Reply(m("RaidOwnerCleared", user.Id));
                            }
                        }

                        return true;
                    }
            }

            return false;
        }

        private void DrawSpheres(BasePlayer player, bool isAllowed)
        {
            if (isAllowed || player.HasPermission("raidablebases.ddraw"))
            {
                AdminCommand(player, () =>
                {
                    foreach (var raid in Raids)
                    {
                        DrawSphere(player, 30f, Color.blue, raid.Location, raid.ProtectionRadius);
                    }
                });
            }
        }

        private void CommandToggle(IPlayer user, string command, string[] args)
        {
            if (config.Settings.Maintained.Enabled || args.Contains("maintained"))
            {
                Automated.IsMaintainedEnabled = !Automated.IsMaintainedEnabled;
                Automated.StartCoroutine(RaidableType.Maintained);
                Message(user, $"Toggled maintained events {(Automated.IsMaintainedEnabled ? "on" : "off")}");
                if (args.Contains("maintained"))
                {
                    config.Settings.Maintained.Enabled = Automated.IsMaintainedEnabled;
                    SaveConfig();
                    return;
                }
            }

            if (config.Settings.Schedule.Enabled || args.Contains("scheduled"))
            {
                Automated.IsScheduledEnabled = !Automated.IsScheduledEnabled;
                Automated.StartCoroutine(RaidableType.Scheduled);
                Message(user, $"Toggled scheduled events {(Automated.IsScheduledEnabled ? "on" : "off")}");
                if (args.Contains("scheduled"))
                {
                    config.Settings.Schedule.Enabled = Automated.IsScheduledEnabled;
                    SaveConfig();
                    return;
                }
            }

            if (config.Settings.Buyable.Max > 0)
            {
                Message(user, $"Toggled buyable events {((buyableEnabled = !buyableEnabled) ? "on" : "off")}");
            }

            Queues.Paused = !buyableEnabled && !Automated.IsScheduledEnabled && !Automated.IsMaintainedEnabled;
            Message(user, $"Toggled queue/spawn manager {(Queues.Paused ? "off" : "on")}");
        }

        private void CommandPopulate(IPlayer user, string command, string[] args)
        {
            if (args.Length == 0)
            {
                Message(user, "Valid arguments: 0 1 2 3 4 all");
                return;
            }

            List<LootItem> lootList = new(ItemManager.GetItemDefinitions().Where(def => !BlacklistedItems.Contains(def.shortname)).Select(def => new LootItem(def.shortname)));

            lootList.Sort((x, y) => x.shortname.CompareTo(y.shortname));

            foreach (var arg in args)
            {
                if (IsEasy(arg) || arg == "all") AddAndReply(RaidableMode.Easy, "Editable_Lists/Easy.json");
                if (IsMedium(arg) || arg == "all") AddAndReply(RaidableMode.Medium, "Editable_Lists/Medium.json");
                if (IsHard(arg) || arg == "all") AddAndReply(RaidableMode.Hard, "Editable_Lists/Hard.json");
                if (IsExpert(arg) || arg == "all") AddAndReply(RaidableMode.Expert, "Editable_Lists/Expert.json");
                if (IsNightmare(arg) || arg == "all") AddAndReply(RaidableMode.Nightmare, "Editable_Lists/Nightmare.json");
            }

            void AddAndReply(RaidableMode mode, string fileName)
            {
                AddToList(mode, lootList);
                Message(user, $"Created {fileName}");
            }

            SaveConfig();
        }

        private void CommandToggleProfile(IPlayer user, string command, string[] args)
        {
            if (args.Length == 2 && Get(args[1], out (string key, BaseProfile profile) val))
            {
                val.profile.Options.Enabled = !val.profile.Options.Enabled;
                SaveProfile(val.key, val.profile.Options);
                user.Reply(mx(val.profile.Options.Enabled ? "ToggleProfileEnabled" : "ToggleProfileDisabled", user.Id, val.key));
            }
        }

        private void CommandStability(IPlayer user, string command, string[] args)
        {
            if (args.Length < 2 || args[1] != "true" && args[1] != "false")
            {
                return;
            }
            var changes = 0;
            var value = args[1];
            var sb = new StringBuilder();
            var name = args.Length == 3 ? args[2] : null;
            foreach (var (key, profile) in Buildings.Profiles.ToList())
            {
                if (!string.IsNullOrEmpty(name) && key != name)
                {
                    continue;
                }
                foreach (var (extra, options) in profile.Options.AdditionalBases.ToList())
                {
                    var stability = options.Find(o => o.Key == "stability");
                    if (stability == null)
                    {
                        changes++;
                        options.Add(new() { Key = "stability", Value = value });
                        sb.AppendFormat("{0} added missing stability option for {1} to {2}", user.Name, key, value).AppendLine();
                    }
                    else if (stability.Value != value)
                    {
                        changes++;
                        stability.Value = value;
                        sb.AppendFormat("{0} changed stability option for {1} to {2}", user.Name, key, value).AppendLine();
                    }
                }
            }
            if (changes > 0)
            {
                foreach (var (key, profile) in Buildings.Profiles)
                {
                    SaveProfile(key, profile.Options);
                }
                Puts("\n{0}\nChanged stability for {1} bases to {2}", sb, changes, value);
            }
            else Puts("No changes required.");
        }

        private void CommandConfig(IPlayer user, string command, string[] args)
        {
            if (!user.HasPermission("raidablebases.config") || args.Length == 0 || !arguments.Exists(str => args[0].Equals(str, StringComparison.OrdinalIgnoreCase)))
            {
                Message(user, "ConfigUseFormat", string.Join("|", arguments));
                return;
            }

            string arg = args[0].ToLower();

            switch (arg)
            {
                case "add": ConfigAddBase(user, args); return;
                case "remove": case "clean": ConfigRemoveBase(user, args); return;
                case "list": ConfigListBases(user); return;
                case "toggle": CommandToggleProfile(user, command, args); return;
                case "stability": CommandStability(user, command, args); return;
                case "maintained": CommandToggle(user, command, args); return;
                case "scheduled": CommandToggle(user, command, args); return;
            }

            if (args.Length == 3)
            {
                RaidableMode mode = GetRaidableMode(arg);
                if (IsModeValid(mode))
                {
                    ConfigSetEnabledWeekday(user, mode, args[1].ToLower(), args[2].ToLower());
                }
            }
        }

        #endregion Commands

        #region Garbage

        internal Dictionary<NetworkableId, MountInfo> MountEntities = new();
        internal Dictionary<NetworkableId, RaidEntity> RaidEntities = new();

        public void RemoveHeldEntities()
        {
            foreach (var re in RaidEntities.Values.ToList())
            {
                if (re.entity is IItemContainerEntity ice && ice != null)
                {
                    RaidableBase.ClearInventory(ice.inventory);
                }
            }
            ItemManager.DoRemoves();
        }

        public void DespawnAll(bool inactiveOnly)
        {
            var entities = new List<BaseEntity>();
            int undoLimit = 1;

            foreach (RaidableBase raid in Raids.ToList())
            {
                if (raid == null || !raid.IsPasted || inactiveOnly && (raid.intruders.Count > 0 || raid.ownerId.IsSteamId()))
                {
                    continue;
                }

                foreach (var entity in raid.Entities)
                {
                    if (!entity.IsKilled() && !raid.DespawnExceptions.Contains(entity))
                    {
                        entities.Add(entity);
                    }
                }

                raid.Entities.Clear();

                if (raid.Options.Setup.DespawnLimit > undoLimit)
                {
                    undoLimit = raid.Options.Setup.DespawnLimit;
                }

                raid.Despawn();
            }

            if (entities.Count > 0)
            {
                UndoLoop(entities, undoLimit);
            }
        }

        private void KillEntity(BaseNetworkable entity, NetworkableId uid, UndoLoopSettings us, bool noLoot)
        {
            if (entity.IsKilled() || entity.ShortPrefabName == "item_drop_backpack" || !us.DespawnMounts && MountEntities.Remove(uid, out var mi) && mi.IsClaimed)
            {
                return;
            }

            if (entity is BaseEntity ent)
            {
                if (ent.OwnerID.IsSteamId() && (entity.PrefabName.Contains("building") ? us.KeepStructures : us.KeepDeployables))
                {
                    return;
                }

                if (entity is IItemContainerEntity ice && ice?.inventory?.itemList?.Count > 0)
                {
                    if (ent.OwnerID.IsSteamId())
                    {
                        DropLoot(ent, ice.inventory, BuoyantBox);
                    }
                    else if (ice.DropsLoot)
                    {
                        ice.inventory.Clear();
                        ItemManager.DoRemoves();
                    }
                }
            }

            if (entity is IOEntity io)
            {
                if (entity is SamSite ss)
                {
                    ss.staticRespawn = false;
                }
                if (noLoot && io is ContainerIOEntity cie)
                {
                    cie.dropsLoot = false;
                    RaidableBase.ClearInventory(cie.inventory);
                }
                try { io.ClearConnections(); } catch { }
            }
            else if (noLoot && entity is StorageContainer container)
            {
                container.dropsLoot = false;
                RaidableBase.ClearInventory(container.inventory);
            }

            entity.SafelyKill();
        }

        private DroppedItemContainer DropLoot(BaseEntity ent, ItemContainer container, bool buoyant)
        {
            try
            {
                string prefab = buoyant ? "assets/prefabs/misc/item drop/item_drop_buoyant.prefab" : "assets/prefabs/misc/item drop/item_drop.prefab";
                Vector3 v = new(0f, ent is AutoTurret ? 0.1f : ent is GunTrap ? 0f : ent is FlameTurret ? ent.bounds.extents.y : ent.bounds.size.y);
                return container.Drop(prefab, ent.transform.position + v, ent.transform.rotation, 0f);
            }
            catch
            {
                return null;
            }
        }

        private UndoLoopSettings UndoSettings = new();

        private UndoLoopComparer UndoComparer = new();

        private List<NetworkableId> UndoNetworkables = new();

        public class UndoLoopSettings
        {
            public bool LogToFile, DespawnMounts, KeepStructures, KeepDeployables;
            public UndoLoopSettings() { }
            public UndoLoopSettings(Configuration config) => (LogToFile, DespawnMounts, KeepStructures, KeepDeployables) = (config.LogToFile, config.Settings.Management.DespawnMounts, config.Settings.Management.KeepStructures, config.Settings.Management.KeepDeployables);
        }

        public class UndoLoopComparer : IComparer<BaseNetworkable>
        {
            public Dictionary<string, ItemDefinition> DeployableItems = new();

            private int Evaluate(BaseNetworkable entity) => entity switch
            {
                null or BuildingBlock => 0,
                IOEntity or StorageContainer => 4,
                IceFence or SimpleBuildingBlock => 3,
                _ when DeployableItems.ContainsKey(entity.PrefabName) => 2,
                _ => 1
            };

            public int Compare(DespawnableEntity x, DespawnableEntity y)
            {
                return Evaluate(x.entity).CompareTo(Evaluate(y.entity));
            }

            public int Compare(BaseNetworkable x, BaseNetworkable y)
            {
                return Evaluate(x).CompareTo(Evaluate(y));
            }
        }

        private bool ShouldDespawnEntity(BaseNetworkable entity)
        {
            return entity.IsValid() ? Automated.Contains(entity.net.ID) || UndoNetworkables.Contains(entity.net.ID) : false;
        }

        public void UndoLoop(List<BaseEntity> entities, int limit, object[] hookObjects = null, int count = 0)
        {
            if (count == 0)
            {
                entities.RemoveAll(entity =>
                {
                    if (entity.IsKilled() || entity.ShortPrefabName == "item_drop_backpack" || (entity.HasParent() && entity.GetParentEntity() is Tugboat))
                    {
                        return true;
                    }
                    if (entity.IsValid())
                    {
                        UndoNetworkables.Add(entity.net.ID);
                    }
                    return false;
                });
                entities.Sort(UndoComparer);
            }
            else entities.RemoveAll(IsKilled);

            entities.Take(limit).ForEach(entity =>
            {
                var uid = entity.IsValid() ? entity.net.ID : default;
                entities.Remove(entity);
                KillEntity(entity, uid, UndoSettings, false);
                temporaryData?.NetworkIdentifiers?.Remove(uid.Value);
                UndoNetworkables?.Remove(uid);
                MountEntities?.Remove(uid);
                RaidEntities?.Remove(uid);
            });

            if (count != 0 && entities.Count != 0 && entities.Count == count)
            {
                Puts("Undo loop canceled because of infinite loop");
                entities.ForEach(e => e.SafelyKill());
                goto done;
            }

            count = entities.Count;

            if (count > 0)
            {
                NextTick(() => UndoLoop(entities, limit, hookObjects, count));
                return;
            }

        done:

            foreach (var (uid, re) in RaidEntities.ToList())
            {
                if (re == null || re.entity.IsKilled())
                {
                    temporaryData?.NetworkIdentifiers?.Remove(uid.Value);
                    RaidEntities.Remove(uid);
                }
            }

            if (RaidEntities.Count == 0)
            {
                MountEntities.Clear();
            }

            if (hookObjects != null)
            {
                if (UndoSettings.LogToFile && hookObjects.Length > 0)
                {
                    LogToFile("despawn", $"{DateTime.Now} Depawn completed {hookObjects[0]}", this, true);
                }
                Interface.CallHook("OnRaidableBaseDespawned", hookObjects);
            }
        }

        #endregion Garbage

        #region Helpers

        private string GetModeText(RaidableMode mode) => en ? mode.ToString() : mode switch
        {
            RaidableMode.Easy => "Легкий",
            RaidableMode.Medium => "Средний",
            RaidableMode.Hard => "Тяжело",
            RaidableMode.Expert => "Эксперт",
            RaidableMode.Nightmare => "Кошмарный",
        };

        private void RegisterPermissions()
        {
            if (config.RankedLadder.Amount > 0)
            {
                foreach (var record in RankedRecords)
                {
                    permission.RegisterPermission(record.Permission, this);
                    permission.CreateGroup(record.Group, record.Group, 0);
                    permission.GrantGroupPermission(record.Group, record.Permission, this);
                }
            }

            permission.RegisterPermission("raidablebases.allow", this);
            permission.RegisterPermission("raidablebases.allow.commands", this);
            permission.RegisterPermission("raidablebases.setowner", this);
            permission.RegisterPermission("raidablebases.ladder.exclude", this);
            permission.RegisterPermission("raidablebases.durabilitybypass", this);
            permission.RegisterPermission("raidablebases.ddraw", this);
            permission.RegisterPermission("raidablebases.mapteleport", this);
            permission.RegisterPermission("raidablebases.canbypass", this);
            permission.RegisterPermission("raidablebases.lockoutbypass", this);
            permission.RegisterPermission("raidablebases.blockbypass", this);
            permission.RegisterPermission("raidablebases.banned", this);
            permission.RegisterPermission("raidablebases.vipcooldown", this);
            permission.RegisterPermission("raidablebases.despawn.buyraid", this);
            permission.RegisterPermission("raidablebases.notitle", this);
            permission.RegisterPermission("raidablebases.block.fauxadmin", this);
            permission.RegisterPermission("raidablebases.elevators.bypass.building", this);
            permission.RegisterPermission("raidablebases.elevators.bypass.card", this);
            permission.RegisterPermission("raidablebases.time", this);
            permission.RegisterPermission("raidablebases.buyraid", this);
            permission.RegisterPermission("raidablebases.buyraid.banned", this);
            permission.RegisterPermission("raidablebases.buyraid.prefabteleport", this);
            permission.RegisterPermission("raidablebases.buyable.bypass.cooldown", this);
            permission.RegisterPermission("raidablebases.buyable.spawn.filenames", this);
            permission.RegisterPermission("raidablebases.buyable.vip.pve", this);
            permission.RegisterPermission("raidablebases.buyable.vip.pvp", this);
            permission.RegisterPermission("raidablebases.hoggingbypass", this);
            permission.RegisterPermission("raidablebases.block.filenames", this);
            permission.RegisterPermission("raidablebases.keepbackpackplugin", this);
        }

        public void LoadTemporaryData()
        {
            try { temporaryData = Interface.Oxide.DataFileSystem.ReadObject<TemporaryData>($"{Name}_UID"); } catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            temporaryData ??= new();
            temporaryData.NetworkIdentifiers ??= new();
            temporaryData.NetworkIdentifiers.ForEach(uid => Automated.Enbatch(new(uid)));
            temporaryData.IsLoaded = true;
        }

        public void LoadPlayerData()
        {
            try { data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name); } catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            data ??= new();
            data.Players ??= new();
            data.BuyableCooldowns ??= new();
            data.Cycle ??= new();
            data.Cycle.Instance = this;
            if (!config.Settings.Management.RequireAllSpawnedPersists)
            {
                data.Cycle._buildings.Clear();
            }
        }

        private void SaveData()
        {
            SavePlayerData();
            SaveTemporaryData();
            UI.SaveOffsetData();
        }

        public void SavePlayerData()
        {
            if (data != null)
            {
                data.Lockouts.RemoveAll((userid, lo) => !lo.Any());
                data.BuyableCooldowns.RemoveAll((userid, bi) => !BuyableInfo.HasTimeRemaining(this, userid));
                data.Players.RemoveAll((useridstring, playerInfo) =>
                {
                    if (playerInfo.IsExpired())
                    {
                        if (ulong.TryParse(useridstring, out var userid))
                        {
                            UI?.Offsets?.Remove(userid);
                        }
                        return true;
                    }
                    return playerInfo.TotalRaids == 0;
                });
                Interface.Oxide.DataFileSystem.WriteObject(Name, data);
            }
        }

        private string GetPlayerData() => JsonConvert.SerializeObject(data.Players);

        private void SaveTemporaryData()
        {
            if (temporaryData != null)
            {
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}_UID", temporaryData);
            }
        }

        internal void StartEntityCleanup()
        {
            IsSpawnerBusy = true;
            var entities = new List<BaseEntity>();
            foreach (var raid in Raids.ToList())
            {
                if (!IsShuttingDown)
                {
                    Puts(mx("Destroyed Raid"), $"{PositionToGrid(raid.Location)} {raid.Location}");
                    if (raid.IsOpened) TryInvokeMethod(raid.AwardRaiders);
                    entities.AddRange(raid.Entities);
                }

                raid.Despawn();
            }
            if (entities.Count == 0)
            {
                TryInvokeMethod(RemoveHeldEntities);
                TryInvokeMethod(UnsetStatics);
            }
            else UndoLoop(entities, despawnLimit);
        }

        private void UnsetStatics()
        {
            UI.DestroyAll();
            RaidableBasesExtensionMethods.ExtensionMethods.p = null;
        }

        private bool IsEmptyMap()
        {
            return BasePlayer.activePlayerList.Count == 0 && BuildingManager.server.buildingDictionary.Values.All(b => !b.HasDecayEntities() || b.decayEntities.All(de => !de || !de.OwnerID.IsSteamId()));
        }

        private void CheckForWipe()
        {
            if (wiped || IsEmptyMap())
            {
                var raids = new List<int>();

                if (data.Players.Count > 0)
                {
                    if (AssignTreasureHunters())
                    {
                        foreach (var info in data.Players.Values.ToList())
                        {
                            if (info.Raids > 0)
                            {
                                raids.Add(info.Raids);
                            }

                            info.Reset();
                        }
                    }

                    if (raids.Count > 0)
                    {
                        var average = raids.Average();

                        data.Players.RemoveAll((userid, playerInfo) => playerInfo.TotalRaids < average);
                    }
                }

                wiped = false;
                data.Lockouts.Clear();
                NextTick(SaveData);
            }
        }

        private bool IsPocketDimensions(BasePlayer player, BaseEntity e)
        {
            if (e.skinID != 0 && e.ShortPrefabName == "woodbox_deployed" && PocketDimensions != null && player.GetActiveItem() is Item activeItem)
            {
                if (Convert.ToBoolean(PocketDimensions?.Call("CheckIsDimensionalItem", activeItem, true))) return true;
                if (Convert.ToBoolean(PocketDimensions?.Call("CheckIsDimensionalItem", activeItem, false))) return true;
            }
            return false;
        }

        public void BuyableTeleport(BasePlayer player)
        {
            if (player.IsOnline() && !player.IsDestroyed && player.HasPermission("raidablebases.buyraid.prefabteleport"))
            {
                foreach (var raid in Raids)
                {
                    if (raid.Type != RaidableType.Purchased) continue;
                    if (raid.ownerId != player.userID) continue;
                    if (!raid.IsOpened) continue;
                    raid.Teleport(player);
                    break;
                }
            }
        }

        private static float GetObstructionRadius(BuildingOptionsProtectionRadius radii, RaidableType type)
        {
            if (radii.Obstruction > 0)
            {
                return Mathf.Clamp(radii.Obstruction, CELL_SIZE, radii.Get(type));
            }
            return radii.Get(type);
        }

        public void ResetPlayer(ulong userid)
        {
            foreach (var raid in Raids)
            {
                raid.intruders.Remove(userid);
            }
        }

        public PasteData GetPasteData(string baseName)
        {
            if (!_pasteData.TryGetValue(baseName, out var pasteData))
            {
                _pasteData[baseName] = pasteData = new();
            }
            return pasteData;
        }

        private bool IsEventOwner(BasePlayer player, bool isLoading)
        {
            return Raids.Exists(raid => raid.ownerId == player.userID && (config.Settings.Buyable.PreventNew || raid.IsOpened || raid.IsDespawning || isLoading && raid.IsLoading));
        }

        private bool Has(NetworkableId networkableId)
        {
            return HumanoidBrains.Values.Exists(brain => brain?.npc?.net?.ID == networkableId);
        }

        private bool Has(ulong userID)
        {
            return HumanoidBrains.ContainsKey(userID);
        }

        private bool Has(TriggerBase trigger)
        {
            return trigger != null && Raids.Exists(raid => raid.triggers.ContainsKey(trigger));
        }

        private bool Has(BaseEntity entity)
        {
            return entity.IsValid() && RaidEntities.ContainsKey(entity.net.ID);
        }

        public int Get(RaidableType type)
        {
            return Queues.queue.Count(sp => sp.type == type) + Raids.Count(raid => raid.Type == type && !raid.IsDespawning);
        }

        private bool HasLimit(RaidableType type)
        {
            return type == RaidableType.Maintained || type == RaidableType.Scheduled || type == RaidableType.Purchased;
        }

        public int Get(RaidableMode mode, bool isPurchased)
        {
            return Queues.queue.Count(sp => (isPurchased == (sp.type == RaidableType.Purchased)) && sp.options.Mode == mode && HasLimit(sp.type)) + Raids.Count(raid => (isPurchased == (raid.Type == RaidableType.Purchased)) && raid.Options.Mode == mode && !raid.IsDespawning && HasLimit(raid.Type));
        }

        public bool Get(ulong userID, out HumanoidBrain brain)
        {
            return HumanoidBrains.TryGetValue(userID, out brain) && brain.raid != null ? brain.raid : null;
        }

        public bool Get(ulong userID, out DelaySettings ds)
        {
            return PvpDelay.TryGetValue(userID, out ds) && ds.raid != null ? ds.raid : null;
        }

        public bool Get(Vector3 target, out RaidableBase raid, float f = 0f, bool b = true)
        {
            if (b) raid = Raids.FirstOrDefault(x => InRange2D(x.Location, target, x.ProtectionRadius + f));
            else raid = Raids.FirstOrDefault(x => InRange(x.Location, target, x.ProtectionRadius + f));
            return raid != null;
        }

        public bool Get(BasePlayer victim, HitInfo hitInfo, out RaidableBase raid)
        {
            if (Get(victim.userID, out HumanoidBrain brain))
            {
                raid = brain.raid;
                return true;
            }
            if (Get(victim.userID, out DelaySettings ds))
            {
                raid = ds.raid;
                return true;
            }
            if (Get(victim.transform.position, out raid, 0f, false))
            {
                return true;
            }
            if (hitInfo != null && !hitInfo.Initiator.IsKilled() && Get(hitInfo.Initiator.transform.position, out raid, 0f, false))
            {
                return true;
            }
            if (hitInfo != null && !hitInfo.Weapon.IsKilled() && Get(hitInfo.Weapon.transform.position, out raid, 0f, false))
            {
                return true;
            }
            raid = null;
            return false;
        }

        public bool Get(PlayerCorpse corpse, out RaidableBase raid)
        {
            if (!corpse.playerSteamID.IsSteamId() && Get(corpse.playerSteamID, out HumanoidBrain brain))
            {
                raid = brain.raid;
                return true;
            }
            if (corpse.playerSteamID.IsSteamId() && Get(corpse.playerSteamID, out DelaySettings ds))
            {
                raid = ds.raid;
                return true;
            }
            if (Get(corpse.transform.position, out raid))
            {
                return true;
            }
            raid = null;
            return false;
        }

        public bool Get(BaseEntity entity, out RaidableBase raid)
        {
            raid = entity.IsValid() && RaidEntities.TryGetValue(entity.net.ID, out var re) ? re.raid : null;
            return raid != null;
        }

        public bool IsTooClose(Vector3 target, float radius)
        {
            return Raids.Exists(x => InRange2D(x.Location, target, radius));
        }

        private static void DrawText(BasePlayer player, float duration, Color color, Vector3 from, object text) => player?.SendConsoleCommand("ddraw.text", duration, color, from, $"<size=16>{text}</size>");
        private static void DrawLine(BasePlayer player, float duration, Color color, Vector3 from, Vector3 to) => player?.SendConsoleCommand("ddraw.line", duration, color, from, to);
        private static void DrawSphere(BasePlayer player, float duration, Color color, Vector3 from, float radius) => player?.SendConsoleCommand("ddraw.sphere", duration, color, from, radius);
        private static bool IsKilled(StorageContainer container) => container.IsKilled() || container.inventory == null;
        private static bool IsKilled(Item item) => item == null || item.isBroken || !item.IsValid();
        private static bool IsKilled(BaseEntity entity) => entity.IsKilled();

        internal void DestroyProtection()
        {
            if (_elevatorProtection != null)
            {
                UnityEngine.Object.DestroyImmediate(_elevatorProtection);
            }
            if (_turretProtection != null)
            {
                UnityEngine.Object.DestroyImmediate(_turretProtection);
            }
        }

        internal ProtectionProperties GetElevatorProtection()
        {
            if (_elevatorProtection == null)
            {
                _elevatorProtection = ScriptableObject.CreateInstance<ProtectionProperties>();
                _elevatorProtection.name = "EventElevatorProtection";
            }
            return _elevatorProtection;
        }

        internal ProtectionProperties GetTurretProtection()
        {
            if (_turretProtection == null)
            {
                _turretProtection = ScriptableObject.CreateInstance<ProtectionProperties>();
                _turretProtection.name = "EventTurretProtection";
            }
            return _turretProtection;
        }

        public void UpdateAllMarkers()
        {
            foreach (var raid in Raids)
            {
                raid.UpdateMarker();
            }
        }

        private bool IsBusy(out Vector3 pastedLocation)
        {
            foreach (RaidableBase raid in Raids)
            {
                if (raid.IsDespawning || raid.IsLoading)
                {
                    pastedLocation = raid.Location;
                    return true;
                }
            }
            pastedLocation = Vector3.zero;
            return false;
        }

        public static void TryInvokeMethod(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                Puts("{0} ERROR: {1}", action.Method.Name, ex);
            }
        }

        public static List<RaidableMode> GetRaidableModes()
        {
            return new() { RaidableMode.Easy, RaidableMode.Medium, RaidableMode.Hard, RaidableMode.Expert, RaidableMode.Nightmare };
        }

        private bool IsKillableEntity(BaseEntity entity)
        {
            return entity.PrefabName.Contains("building") || DeployableItems.ContainsKey(entity.PrefabName) || entity is VendingMachineMapMarker || entity is MapMarkerGenericRadius || entity is SphereEntity || entity is HumanoidNPC;
        }

        private static List<T> FindEntitiesOfType<T>(Vector3 a, float n, int m = -1) where T : BaseEntity
        {
            int hits = Physics.OverlapSphereNonAlloc(a, n, Vis.colBuffer, m, QueryTriggerInteraction.Collide);
            List<T> entities = new();
            for (int i = 0; i < hits; i++)
            {
                if (Vis.colBuffer[i] is Collider col && col.ToBaseEntity() is T entity && !entity.IsDestroyed && !entities.Contains(entity))
                {
                    entities.Add(entity);
                }
                Vis.colBuffer[i] = null;
            }
            return entities;
        }

        private void CheckOceanLevel()
        {
            if (OceanLevel != WaterSystem.OceanLevel)
            {
                OceanLevel = WaterSystem.OceanLevel;

                if (GridController.Spawns.TryGetValue(RaidableType.Grid, out var spawns))
                {
                    spawns.TryAddRange(CacheType.Submerged);
                }
            }
        }

        private void SetOnSun(bool state, int retries = 0)
        {
            if (retries >= 3 || !config.Settings.Management.Lights)
            {
                return;
            }

            try
            {
                if (state)
                {
                    TOD_Sky.Instance.Components.Time.OnSunrise += OnSunrise;
                    TOD_Sky.Instance.Components.Time.OnSunset += OnSunset;
                }
                else
                {
                    TOD_Sky.Instance.Components.Time.OnSunrise -= OnSunrise;
                    TOD_Sky.Instance.Components.Time.OnSunset -= OnSunset;
                }
            }
            catch
            {
                timer.Once(10f, () => SetOnSun(state, ++retries));
            }
        }

        public void InitializeSkins()
        {
            foreach (var def in ItemManager.GetItemDefinitions())
            {
                if (def.TryGetComponent<ItemModDeployable>(out var imd))
                {
                    DeployableItems[imd.entityPrefab.resourcePath] = def;
                    ItemDefinitions[def] = imd.entityPrefab.resourcePath;
                }
                if (def.TryGetComponent<ItemModEntity>(out var ime))
                {
                    _itemModEntity[ime.entityPrefab.resourcePath] = def;
                }
            }
        }

        public static void AdminCommand(BasePlayer player, Action action)
        {
            if (!player.IsAdmin && !player.IsDeveloper && player.IsFlying)
            {
                return; // BasePlayer => FinalizeTick => NoteAdminHack => Ban => Cheat Detected!
            }

            bool isAdmin = player.IsAdmin;

            if (!isAdmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                player.SendNetworkUpdateImmediate();
            }
            try
            {
                action();
            }
            finally
            {
                if (!isAdmin)
                {
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
                    player.SendNetworkUpdateImmediate();
                }
            }
        }

        private HashSet<ulong> GetMembers(ulong userid)
        {
            HashSet<ulong> members = new() { userid };

            if (RelationshipManager.ServerInstance.playerToTeam.TryGetValue(userid, out var team))
            {
                members.UnionWith(team.members);
            }

            if (Clans?.Call("GetClanMembers", userid) is List<string> clan && !clan.IsNullOrEmpty())
            {
                clan.ForEach(member => members.Add(ulong.Parse(member)));
            }

            return members;
        }

        private bool IsHelicopter(HitInfo hitInfo, out bool eventHeli)
        {
            eventHeli = false;
            if (hitInfo.Initiator is PatrolHelicopter heli)
            {
                eventHeli = heli._name != null && !heli._name.Contains("patrolhelicopter");
                return true;
            }
            if (hitInfo.Initiator?.ShortPrefabName == "oilfireballsmall" || hitInfo.Initiator?.ShortPrefabName == "napalm")
            {
                return true;
            }
            return hitInfo.WeaponPrefab?.ShortPrefabName == "rocket_heli" || hitInfo.WeaponPrefab?.ShortPrefabName == "rocket_heli_napalm";
        }

        private Plugin CopyPaste => plugins.Find("CopyPaste");

        public bool IsCopyPasteLoaded(out string error)
        {
            error = "You must install or reload CopyPaste: https://umod.org/plugins/copy-paste";
            try { return CopyPaste.Version >= new VersionNumber(4, 1, 32); } catch { return false; }
        }

        private bool PlayerInEvent(BasePlayer player)
        {
            return player.IsKilled() ? false : HasPVPDelay(player.userID) || EventTerritory(player.transform.position);
        }

        private bool HasPVPDelay(ulong userid)
        {
            return PvpDelay.ContainsKey(userid);
        }

        private bool IsBox(BaseEntity entity, bool inherit)
        {
            if (entity.ShortPrefabName == "box.wooden.large" || entity.ShortPrefabName == "woodbox_deployed" || entity.ShortPrefabName == "coffinstorage" || entity.ShortPrefabName.Contains("storage_barrel"))
            {
                return true;
            }

            return inherit && config.Settings.Management.Inherit.Exists(entity.ShortPrefabName.Contains);
        }

        public float GetDistance(RaidableType type)
        {
            return type switch
            {
                RaidableType.Maintained => Mathf.Clamp(config.Settings.Maintained.Distance, CELL_SIZE, 9000f),
                RaidableType.Purchased => Mathf.Clamp(config.Settings.Buyable.Distance, CELL_SIZE, 9000f),
                RaidableType.Scheduled => Mathf.Clamp(config.Settings.Schedule.Distance, CELL_SIZE, 9000f),
                RaidableType.None => Mathf.Max(config.Settings.Maintained.Distance, config.Settings.Buyable.Distance, config.Settings.Schedule.Distance),
                _ => 100f
            };
        }

        private void AddToList(RaidableMode mode, List<LootItem> source)
        {
            if (!Buildings.DifficultyLootLists.TryGetValue(mode, out var lootList))
            {
                Buildings.DifficultyLootLists[mode] = lootList = new();
            }

            foreach (var ti in source)
            {
                if (!lootList.Exists(x => x.shortname == ti.shortname))
                {
                    lootList.Add(ti);
                }
            }

            Interface.Oxide.DataFileSystem.WriteObject($"{Name}{Path.DirectorySeparatorChar}Editable_Lists{Path.DirectorySeparatorChar}{mode}", lootList);
        }

        private bool IsPVE() => TruePVE != null || NextGenPVE != null || Imperium != null;

        private static bool IsEasy(string value) => value == "0" || value.Equals("easy", StringComparison.OrdinalIgnoreCase) || value.Equals("easy bases", StringComparison.OrdinalIgnoreCase);

        private static bool IsMedium(string value) => value == "1" || value.Equals("med", StringComparison.OrdinalIgnoreCase) || value.Equals("medium", StringComparison.OrdinalIgnoreCase) || value.Equals("medium bases", StringComparison.OrdinalIgnoreCase);

        private static bool IsHard(string value) => value == "2" || value.Equals("hard", StringComparison.OrdinalIgnoreCase) || value.Equals("hard bases", StringComparison.OrdinalIgnoreCase);

        private static bool IsExpert(string value) => value == "3" || value.Equals("expert", StringComparison.OrdinalIgnoreCase) || value.Equals("expert bases", StringComparison.OrdinalIgnoreCase);

        private static bool IsNightmare(string value) => value == "4" || value.Equals("nightmare", StringComparison.OrdinalIgnoreCase) || value.Equals("nightmare bases", StringComparison.OrdinalIgnoreCase);

        [HookMethod("IsPremium")]
        public bool IsPremium() => true;

        private void UpdateUI()
        {
            if (config.UI.Lockout.Enabled || config.UI.BuyableCooldowns.Enabled)
            {
                BasePlayer.activePlayerList.ForEach(player =>
                {
                    UI.UpdateUi(player, UiType.Lockout);
                    UI.UpdateUi(player, UiType.Cooldown);
                });
            }
        }

        private static void NullifyDamage(HitInfo hitInfo)
        {
            if (hitInfo != null)
            {
                hitInfo.damageTypes = new();
                hitInfo.DidHit = false;
                hitInfo.DoHitEffects = false;
            }
        }

        public bool MustExclude(RaidableType type, bool allowPVP)
        {
            if (!config.Settings.Maintained.IncludePVE && type == RaidableType.Maintained && !allowPVP)
            {
                return true;
            }

            if (!config.Settings.Maintained.IncludePVP && type == RaidableType.Maintained && allowPVP)
            {
                return true;
            }

            if (!config.Settings.Schedule.IncludePVE && type == RaidableType.Scheduled && !allowPVP)
            {
                return true;
            }

            if (!config.Settings.Schedule.IncludePVP && type == RaidableType.Scheduled && allowPVP)
            {
                return true;
            }

            return false;
        }

        private bool AnyNpcs()
        {
            return Raids.Exists(x => x.npcs.Exists(npc => !npc.IsKilled()));
        }

        private string[] GetProfileFiles()
        {
            try
            {
                return Interface.Oxide.DataFileSystem.GetFiles($"{Name}{Path.DirectorySeparatorChar}Profiles");
            }
            catch (UnauthorizedAccessException ex)
            {
                UnityEngine.Debug.LogException(ex);
                profileErrors.Add("Unauthorized");
            }

            return Array.Empty<string>();
        }

        private string[] GetCopyPasteFiles()
        {
            try
            {
                return Interface.Oxide.DataFileSystem.GetFiles("copypaste");
            }
            catch (UnauthorizedAccessException ex)
            {
                UnityEngine.Debug.LogException(ex);
                profileErrors.Add("Unauthorized");
            }

            return Array.Empty<string>();
        }

        private bool CheckAutoCorrect(IPlayer user, string file, ref string value)
        {
            string other = GetFileNameWithoutExtension(file);
            if (other == value) return true;
            if (!other.Equals(value, StringComparison.OrdinalIgnoreCase)) return false;
            Message(user, $"Auto-corrected spelling of '{value}' to '{other}'");
            value = other;
            return true;
        }

        private void ConfigAddBase(IPlayer user, string[] args)
        {
            if (args.Length < 2)
            {
                Message(user, "ConfigAddBaseSyntax");
                return;
            }

            _sb.Clear();
            List<string> values = new(args);
            values.RemoveAt(0);
            string profileName = values[0];

            foreach (var file in GetProfileFiles())
            {
                if (file.Contains("_empty")) continue;
                if (CheckAutoCorrect(user, file, ref profileName)) break;
            }

            RaidableMode mode = RaidableMode.Random;

            foreach (string value in values)
            {
                var m = GetRaidableMode(value);

                if (m != RaidableMode.Random)
                {
                    values.Remove(value);
                    mode = m;
                    break;
                }
            }

            values.RemoveAll(v => v.Length == 1);
            if (!FileExists(profileName)) values.Remove(profileName);
            Message(user, "Adding", string.Join(" ", values));

            if (!Buildings.Profiles.TryGetValue(profileName, out var profile))
            {
                Buildings.Profiles[profileName] = profile = new(this);
                profile.ProfileName = profileName;
                _sb.AppendLine(mx("AddedPrimaryBase", user.Id, profileName));
            }

            if (IsModeValid(mode))
            {
                _sb.AppendLine(mx("DifficultySetTo", user.Id, profile.Options.Mode = mode));
            }

            var copypasteFiles = GetCopyPasteFiles();

            if (args.Contains("*"))
            {
                foreach (var path in copypasteFiles)
                {
                    string value = GetFileNameWithoutExtension(path);
                    if (values.Contains(value) || profile.Options.AdditionalBases.ContainsKey(value))
                    {
                        continue;
                    }
                    value = profile.Options.Mode switch
                    {
                        RaidableMode.Easy when value.Contains("easy", StringComparison.OrdinalIgnoreCase) => value,
                        RaidableMode.Medium when value.Contains("med", StringComparison.OrdinalIgnoreCase) => value,
                        RaidableMode.Hard when value.Contains("hard", StringComparison.OrdinalIgnoreCase) => value,
                        RaidableMode.Expert when value.Contains("expert", StringComparison.OrdinalIgnoreCase) => value,
                        RaidableMode.Nightmare when value.Contains("nightmare", StringComparison.OrdinalIgnoreCase) => value,
                        _ => null
                    };
                    if (!string.IsNullOrEmpty(value))
                    {
                        values.Add(value);
                    }
                }
            }

            for (int i = 0; i < values.Count; i++)
            {
                string value = values[i];
                foreach (var cpf in copypasteFiles)
                {
                    if (CheckAutoCorrect(user, cpf, ref value)) break;
                }
                if (!profile.Options.AdditionalBases.ContainsKey(value))
                {
                    profile.Options.AdditionalBases.Add(value, DefaultPasteOptions());
                    _sb.AppendLine(mx("AddedAdditionalBase", user.Id, value));
                }
            }

            if (_sb.Length > 0)
            {
                Message(user, _sb.ToString());
                profile.Options.Enabled = true;
                SaveProfile(profileName, profile.Options);
                Buildings.Profiles[profileName] = profile;
                if (mode == RaidableMode.Disabled)
                {
                    Message(user, "DifficultyNotSet");
                }

                _sb.Clear();
            }
            else Message(user, "EntryAlreadyExists");
        }

        private static string GetFileNameWithoutExtension(string file) => Oxide.Core.Utility.GetFileNameWithoutExtension(file);

        private void ConfigRemoveBase(IPlayer user, string[] args)
        {
            if (args.Length < 2)
            {
                Message(user, "RemoveSyntax");
                return;
            }

            int num = 0;
            var profiles = new Dictionary<string, BaseProfile>(Buildings.Profiles);
            var files = (string.Join(" ", args[0].ToLower() == "remove" ? args.Skip(1) : args)).Replace(", ", " ");
            var split = files.Split(' ');

            _sb.Clear();
            _sb.AppendLine(mx("RemovingAllBasesFor", user.Id, string.Join(" ", files)));

            foreach (var (key, profile) in profiles)
            {
                foreach (var (extra, _) in profile.Options.AdditionalBases.ToList())
                {
                    if (args.Contains("*") && key == args[1] || split.Contains(extra))
                    {
                        _sb.AppendLine(mx("RemovedAdditionalBase", user.Id, extra, key));
                        if (profile.Options.AdditionalBases.Remove(extra)) num++;
                        SaveProfile(key, profile.Options);
                    }
                }

                if (split.Contains(key))
                {
                    _sb.AppendLine(mx("RemovedPrimaryBase", user.Id, key));
                    if (Buildings.Profiles.Remove(key)) num++;
                    profile.Options.Enabled = false;
                    SaveProfile(key, profile.Options);
                }
            }

            _sb.AppendLine(mx("RemovedEntries", user.Id, num));
            Message(user, _sb.ToString());
            _sb.Clear();
        }

        private void ConfigSetEnabledWeekday(IPlayer user, RaidableMode mode, string day, string flag)
        {
            if (!Enum.TryParse(day.SentenceCase(), out DayOfWeek dayOfWeek))
            {
                Message(user, $"Invalid weekday: {day}");
                return;
            }

            if (!bool.TryParse(flag, out var value))
            {
                Message(user, $"Invalid flag (true/false): {flag}");
                return;
            }

            Message(user, $"{mode} is now {(value ? "enabled" : "disabled")} on {dayOfWeek}");

            var ds = mode switch
            {
                RaidableMode.Easy => config.Settings.Management.Easy,
                RaidableMode.Medium => config.Settings.Management.Medium,
                RaidableMode.Hard => config.Settings.Management.Hard,
                RaidableMode.Expert => config.Settings.Management.Expert,
                RaidableMode.Nightmare => config.Settings.Management.Nightmare,
                _ => null
            };

            if (ds != null)
            {
                switch (dayOfWeek)
                {
                    case DayOfWeek.Monday: ds.Monday = value; break;
                    case DayOfWeek.Tuesday: ds.Tuesday = value; break;
                    case DayOfWeek.Wednesday: ds.Wednesday = value; break;
                    case DayOfWeek.Thursday: ds.Thursday = value; break;
                    case DayOfWeek.Friday: ds.Friday = value; break;
                    case DayOfWeek.Saturday: ds.Saturday = value; break;
                    case DayOfWeek.Sunday: ds.Sunday = value; break;
                }
            }

            _saveConfigTimer?.Destroy();
            _saveConfigTimer = timer.Once(1f, SaveConfig);
        }

        private Timer _saveConfigTimer;

        private void ConfigCheckFrames(IPlayer user)
        {
            if (GridController.BadFrameRate)
            {
                Message(user, "Server FPS must be above 15 for the plugin to function properly...");
            }
        }

        private void ConfigListBases(IPlayer user)
        {
            ConfigCheckFrames(user);
            _sb.Clear();
            _sb.AppendLine();

            bool buyable = false;
            bool validBase = false;
            var _sb2 = new StringBuilder();

            if (Buildings.Profiles.Count == 0)
            {
                if (IsGridLoading()) Message(user, "GridIsLoading");
                Message(user, "No profiles are loaded!");
            }

            foreach (var (key, profile) in Buildings.Profiles)
            {
                if (!profile.Options.AllowPVP)
                {
                    buyable = true;
                }

                if (FileExists(key))
                {
                    _sb.Append(key);
                    validBase = true;
                }
                else _sb.Append(key).Append(mx("IsProfile", user.Id));

                if (profile.Options.AdditionalBases.Count > 0)
                {
                    foreach (var extra in profile.Options.AdditionalBases.Keys)
                    {
                        if (FileExists(extra))
                        {
                            _sb.Append(extra).Append(", ");
                            validBase = true;
                        }
                        else _sb2.Append(extra).Append(mx("FileDoesNotExist", user.Id));
                    }

                    if (validBase)
                    {
                        _sb.Length -= 2;
                    }

                    _sb.AppendLine();
                    _sb.Append(_sb2);
                    _sb2.Clear();
                }

                _sb.AppendLine();
            }

            if (!buyable && !config.Settings.Buyable.BuyPVP)
            {
                _sb.AppendLine(mx("NoBuyableEventsPVP", user.Id));
            }

            if (!validBase)
            {
                _sb.AppendLine(mx("NoBuildingsConfigured", user.Id));
            }

            Message(user, _sb.ToString());
        }

        private bool TryRemoveItems(BaseEntity entity)
        {
            if (entity is IItemContainerEntity ice && ice != null && ice.inventory != null)
            {
                bool clearInventory = entity.OwnerID == 0 && entity switch
                {
                    FlameTurret when !config.Settings.Management.DropLoot.FlameTurret => true,
                    FogMachine when !config.Settings.Management.DropLoot.FogMachine => true,
                    GunTrap when !config.Settings.Management.DropLoot.GunTrap => true,
                    BuildingPrivlidge when !config.Settings.Management.AllowCupboardLoot => true,
                    _ => false
                };
                if (clearInventory)
                {
                    RaidableBase.ClearInventory(ice.inventory);
                    return true;
                }
            }
            return false;
        }

        private void DropOrRemoveItems(StorageContainer container, RaidableBase raid, bool forced, bool kill)
        {
            if (!container.inventory.IsEmpty() && (forced || !TryRemoveItems(container)))
            {
                var drop = DropLoot(container, container.inventory, container is BuildingPrivlidge ? raid.Options.BuoyantPrivilege : raid.Options.BuoyantBox);
                if (container.OwnerID == 0uL)
                {
                    if (raid.Options.DespawnGreyBags) raid.AddEntity(drop);
                    else raid.DespawnExceptions.Add(drop);
                }
            }

            ItemManager.DoRemoves();

            if (kill && (container is BuildingPrivlidge || IsBox(container, false)))
            {
                container.Invoke(container.SafelyKill, 0.1f);
            }
        }

        protected bool DespawnBase(BasePlayer player, bool isAllowed)
        {
            var raid = isAllowed ? GetNearestBase(player.transform.position) : GetPurchasedBase(player);

            if (raid == null || raid.IsLoading)
            {
                Message(player, isAllowed ? "DespawnBaseNoneAvailable" : "DespawnBaseNoneOwned");
                return false;
            }

            if (!raid.CanBypass(player) && raid.IsDamaged && config.Settings.Buyable.Refunds.Despawn)
            {
                Message(player, "DespawnBaseDamaged");
                return false;
            }

            if (!raid.CanBypass(player) && raid.IsAnyLooted && config.Settings.Buyable.Refunds.AnyLooted)
            {
                Message(player, "DespawnBaseLooted");
                return false;
            }

            if (raid.IsPayLocked)
            {
                if (raid.GetOwner() is BasePlayer owner) raid.Refund(owner);
                raid.IsEligible = !config.Settings.Buyable.Refunds.Ineligible;
            }

            if (raid.AddNearTime <= 0f)
            {
                raid.AddNearTime = 15f;
            }

            raid.Despawn();

            Message(player, "DespawnBaseSuccess");

            Puts(mx("DespawnedAt", null, player.displayName, PositionToGrid(player.transform.position)));

            return true;
        }

        private RaidableBase GetPurchasedBase(BasePlayer player)
        {
            return Raids.FirstOrDefault(raid => raid.IsPayLocked && raid.ownerId == player.userID);
        }

        private RaidableBase GetNearestBase(Vector3 target, float radius = 100f)
        {
            return Raids.Where(x => InRange2D(x.Location, target, radius)).OrderBy(x => (x.Location - target).sqrMagnitude).FirstOrDefault();
        }

        private bool IsTrueDamage(BaseEntity entity, bool isProtectedWeapon)
        {
            if (entity.IsNull())
            {
                return false;
            }

            return isProtectedWeapon || entity.skinID == 1587601905 || TrueDamage.Contains(entity.ShortPrefabName) || entity is TeslaCoil || entity is BaseTrap;
        }

        private Vector3 GetCenterLocation(Vector3 position)
        {
            for (int i = 0; i < Raids.Count; i++)
            {
                if (InRange2D(Raids[i].Location, position, Raids[i].ProtectionRadius))
                {
                    return Raids[i].Location;
                }
            }

            return Vector3.zero;
        }

        private bool HasEventEntity(BaseEntity entity)
        {
            return entity is HumanoidNPC player ? Has(player.userID) : Has(entity);
        }

        [HookMethod("EventTerritory")]
        public bool EventTerritory(Vector3 position)
        {
            for (int i = 0; i < Raids.Count; i++)
            {
                if (InRange2D(Raids[i].Location, position, Raids[i].ProtectionRadius))
                {
                    return true;
                }
            }
            return false;
        }

        private string SetUiParent(string value, int type)
        {
            return type switch
            {
                0 => UI.BUYABLE_PARENT = value,
                1 => UI.COOLDOWN_PARENT = value,
                2 => UI.DELAY_PARENT = value,
                3 => UI.LOCKOUT_PARENT = value,
                4 => UI.STATUS_PARENT = value,
                5 => UI.ELEVATOR_PARENT = value,
                _ => UI.TELEPORT_PARENT = value
            };
        }

        private int[] GetPlayerAmount(string userid, int mode) => !data.Players.TryGetValue(userid, out var user) ? Array.Empty<int>() : mode switch
        {
            0 => new int[] { user.Easy, user.TotalEasy },
            1 => new int[] { user.Medium, user.TotalMedium },
            2 => new int[] { user.Hard, user.TotalHard },
            3 => new int[] { user.Expert, user.TotalExpert },
            _ => new int[] { user.Nightmare, user.TotalNightmare },
        };

        public bool CanBlockOutsideDamage(RaidableBase raid, BasePlayer attacker, bool isEnabled)
        {
            return isEnabled && !InRange(attacker.transform.position, raid.Location, Mathf.Max(raid.ProtectionRadius, raid.Options.ArenaWalls.Radius));
        }

        public static bool InRange2D(Vector3 a, Vector3 b, float distance)
        {
            return (new Vector3(a.x, 0f, a.z) - new Vector3(b.x, 0f, b.z)).sqrMagnitude <= distance * distance;
        }

        public static bool InRange(Vector3 a, Vector3 b, float distance)
        {
            return (a - b).sqrMagnitude <= distance * distance;
        }

        private bool AssignTreasureHunters()
        {
            var players = data.Players.ToList().Where(x => IsNormalUser(x.Key));

            if (!players.Exists(entry => entry.Value.Any))
            {
                return false;
            }

            foreach (var target in covalence.Players.All)
            {
                foreach (var record in RankedRecords)
                {
                    if (target.HasPermission(record.Permission))
                    {
                        permission.RevokeUserPermission(target.Id, record.Permission);
                    }

                    if (permission.UserHasGroup(target.Id, record.Group))
                    {
                        permission.RemoveUserGroup(target.Id, record.Group);
                    }
                }
            }

            if (config.RankedLadder.Enabled && config.RankedLadder.Amount > 0 && players.Count > 0)
            {
                RankedRecords.ForEach(record => AssignTreasureHunters(record, players));

                Puts(mx("Log Saved", null, "treasurehunters"));
            }

            return true;
        }

        private bool IsNormalUser(string userid)
        {
            return userid.IsSteamId() && !userid.HasPermission("raidablebases.notitle") && covalence.Players.FindPlayerById(userid) is IPlayer user && !user.IsBanned;
        }

        private void AssignTreasureHunters(RankedRecord record, List<KeyValuePair<string, PlayerInfo>> players)
        {
            List<(string, int)> ladder = new();

            foreach (var (userid, info) in players)
            {
                (string, int) score = record.Mode switch
                {
                    RaidableMode.Points when info.Points > 0 => new(userid, info.Points),
                    RaidableMode.Easy when info.Easy > 0 && config.RankedLadder.Assign.Easy == 0 => new(userid, info.Easy),
                    RaidableMode.Medium when info.Medium > 0 && config.RankedLadder.Assign.Medium == 0 => new(userid, info.Medium),
                    RaidableMode.Hard when info.Hard > 0 && config.RankedLadder.Assign.Hard == 0 => new(userid, info.Hard),
                    RaidableMode.Expert when info.Expert > 0 && config.RankedLadder.Assign.Expert == 0 => new(userid, info.Expert),
                    RaidableMode.Nightmare when info.Nightmare > 0 && config.RankedLadder.Assign.Nightmare == 0 => new(userid, info.Nightmare),
                    _ => default
                };

                if (score.Item2 != 0)
                {
                    ladder.Add(score);
                }
            }

            if (ladder.Count == 0)
            {
                return;
            }

            ladder.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            foreach (var (userid, score) in ladder.Take(config.RankedLadder.Amount))
            {
                var user = covalence.Players.FindPlayerById(userid);

                if (user == null || user.HasPermission("raidablebases.ladder.exclude"))
                {
                    continue;
                }

                permission.GrantUserPermission(user.Id, record.Permission, this);
                permission.AddUserGroup(user.Id, record.Group);

                LogToFile("treasurehunters", $"{DateTime.Now} : {mx("Log Stolen", null, user.Name, user.Id, score)}", this, true);
                Puts(mx("Log Granted", null, user.Name, user.Id, record.Permission, record.Group));
            }
        }

        private bool CanContinueAutomation() => GetRaidableModes().Exists(CanSpawnDifficultyToday);

        private static bool IsModeValid(RaidableMode mode) => mode != RaidableMode.Disabled && mode != RaidableMode.Random && mode != RaidableMode.Points;

        public string PositionToGrid(Vector3 v) => config.Settings.ShowXZ ? $"{PhoneController.PositionToGridCoord(v)} ({v.x:N2} {v.z:N2})" : PhoneController.PositionToGridCoord(v);

        public string FormatGridReference(BasePlayer player, Vector3 v)
        {
            BaseGameMode svActiveGameMode = BaseGameMode.GetActiveGameMode(true);
            List<string> format = new();

            if (svActiveGameMode == null || svActiveGameMode.ingameMap)
            {
                format.Add(PhoneController.PositionToGridCoord(v));
            }

            if (config.Settings.ShowDir && !player.IsKilled())
            {
                format.Add(format.Count > 0 ? $"({GetDirection(player, v)})" : $"{GetDirection(player, v)} ({Mathf.CeilToInt(player.Distance(v))}m)");
            }

            if (config.Settings.ShowXZ)
            {
                format.Add(format.Count > 0 ? $"({v.x:N2} {v.z:N2})" : $"{v.x:N2} {v.z:N2}");
            }

            return format.Count > 0 ? string.Join(" ", format) : $"{v}";
        }

        private string GetDirection(BasePlayer player, Vector3 target)
        {
            Vector3 targetDir = (target - player.eyes.position).normalized;
            float yaw = Quaternion.LookRotation(targetDir).eulerAngles.y;

            return yaw switch
            {
                _ when yaw > 337.5 || yaw < 22.5 => "North",
                _ when yaw > 22.5 && yaw < 67.5 => "North East",
                _ when yaw > 292.5 && yaw < 337.5 => "North West",
                _ when yaw > 67.5 && yaw < 112.5 => "East",
                _ when yaw > 157.5 && yaw < 202.5 => "South",
                _ when yaw > 112.5 && yaw < 157.5 => "South East",
                _ when yaw > 202.5 && yaw < 247.5 => "South West",
                _ when yaw > 247.5 && yaw < 292.5 => "West",
                _ => "Unknown"
            };
        }

        private string FormatTime(double seconds, string id = null)
        {
            if (seconds < 0)
            {
                return "0s";
            }

            var ts = TimeSpan.FromSeconds(seconds);

            return mx("TimeFormat", id, ts.Hours, ts.Minutes, ts.Seconds);
        }

        #endregion

        #region Data files

        private bool ProfilesExists()
        {
            try
            {
                Interface.Oxide.DataFileSystem.GetFiles($"{Name}{Path.DirectorySeparatorChar}Profiles");

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void CreateDefaultFiles()
        {
            if (ProfilesExists())
            {
                return;
            }

            Interface.Oxide.DataFileSystem.GetDatafile($"{Name}{Path.DirectorySeparatorChar}Profiles{Path.DirectorySeparatorChar}_emptyfile");

            foreach (var (key, options) in DefaultBuildingOptions())
            {
                string filename = $"{Name}{Path.DirectorySeparatorChar}Profiles{Path.DirectorySeparatorChar}{key}";

                if (!Interface.Oxide.DataFileSystem.ExistsDatafile(filename))
                {
                    SaveProfile(key, options);
                }
            }

            string lootFile = $"{Name}{Path.DirectorySeparatorChar}Default_Loot";

            if (!Interface.Oxide.DataFileSystem.ExistsDatafile(lootFile))
            {
                Interface.Oxide.DataFileSystem.WriteObject(lootFile, DefaultLoot());
            }
        }

        protected void VerifyProfiles()
        {
            bool allowPVP = Buildings.Profiles.Values.Exists(profile => profile.Options.AllowPVP);
            bool allowPVE = Buildings.Profiles.Values.Exists(profile => !profile.Options.AllowPVP);

            if (config.Settings.Maintained.Enabled)
            {
                if (allowPVP && !config.Settings.Maintained.IncludePVP && !allowPVE)
                {
                    Puts("Invalid configuration: Maintained Events -> Include PVP Bases is set false when all profiles have Allow PVP set to true. You can set Include PVP Bases to true, and Convert PVP To PVE to true.");
                }

                if (allowPVE && !config.Settings.Maintained.IncludePVE && !allowPVP)
                {
                    Puts("Invalid configuration: Maintained Events -> Include PVE Bases is set false when all profiles have Allow PVP set to false. You can set Include PVE Bases to true, and Convert PVE To PVP to true.");
                }
            }

            if (config.Settings.Schedule.Enabled)
            {
                if (allowPVP && !config.Settings.Schedule.IncludePVP && !allowPVE)
                {
                    Puts("Invalid configuration: Scheduled Events -> Include PVP Bases is set false when all profiles have Allow PVP set to true. You can set Include PVP Bases to true, and Convert PVP To PVE to true.");
                }

                if (allowPVE && !config.Settings.Schedule.IncludePVE && !allowPVP)
                {
                    Puts("Invalid configuration: Scheduled Events -> Include PVE Bases is set false when all profiles have Allow PVP set to false. You can set Include PVE Bases to true, and Convert PVE To PVP to true.");
                }
            }
        }

        private List<string> profileErrors = new();
        private bool AnyCopyPasteFileExists;

        protected IEnumerator LoadProfiles(IPlayer user = null)
        {
            string folder = $"{Name}{Path.DirectorySeparatorChar}Profiles";
            string[] files = GetProfileFiles();

            if (files.Length == 0)
            {
                yield break;
            }

            Buildings.Profiles.Clear();

            foreach (string file in files)
            {
                yield return CoroutineEx.waitForFixedUpdate;

                string profileName = GetFileNameWithoutExtension(file);

                try
                {
                    if (file.Contains("_empty"))
                    {
                        continue;
                    }

                    var options = Interface.Oxide.DataFileSystem.ReadObject<BuildingOptions>($"{folder}{Path.DirectorySeparatorChar}{profileName}");

                    if (options == null)
                    {
                        continue;
                    }

                    options.AdditionalBases ??= new();

                    options.NPC.SetAccuracy(options.Mode);

                    if (options.NPC.SpawnAmountMurderers == -9)
                    {
                        options.NPC.SpawnAmountMurderers = options.Mode == RaidableMode.Easy ? 2 : options.Mode == RaidableMode.Medium ? 3 : 0;
                        options.NPC.SpawnMinAmountMurderers = options.NPC.SpawnAmountMurderers;
                        options.NPC.SpawnAmountScientists = options.Mode == RaidableMode.Easy ? 1 : options.Mode == RaidableMode.Medium ? 2 : options.Mode == RaidableMode.Hard ? 6 : options.Mode == RaidableMode.Expert ? 8 : 10;
                        options.NPC.SpawnMinAmountScientists = options.NPC.SpawnAmountScientists;
                    }

                    if (options.Setup.DespawnLimit > despawnLimit)
                    {
                        despawnLimit = options.Setup.DespawnLimit;
                    }

                    if (options.NPC._MurdererItems != null)
                    {
                        options.NPC.MurdererLoadout = options.NPC._MurdererItems;
                        options.NPC._MurdererItems = null;
                    }

                    if (options.NPC._ScientistItems != null)
                    {
                        options.NPC.ScientistLoadout = options.NPC._ScientistItems;
                        options.NPC._ScientistItems = null;
                    }

                    if (options.NPC._DespawnInventory != null)
                    {
                        options.NPC.AlternateScientistLoot.None = options.NPC._DespawnInventory.Value;
                        options.NPC._DespawnInventory = null;
                    }

                    if (options.NPC._RandomNames != null)
                    {
                        options.NPC.RandomScientistNames = options.NPC._RandomNames.ToList();
                        options.NPC.RandomMurdererNames = options.NPC._RandomNames.ToList();
                        options.NPC._RandomNames = null;
                    }

                    if (options.BuoyantBox)
                    {
                        BuoyantBox = true;
                    }

                    if (options.Blocks.Skins.Remove(10226))
                    {
                        options.Blocks.Skins.Add(2);
                    }

                    options.Permission.Register(this, permission);
                    options.CustomSpawns.BuyableTeleportPrefabs.Remove("");
                    //options.CustomSpawns.SpawnPointPrefabs.Remove("");

                    Buildings.Profiles[profileName] = new(this, options, profileName);
                }
                catch (Exception ex)
                {
                    Puts("{0}\n{1}", file, ex);
                    profileErrors.Add(file);
                    continue;
                }
            }

            foreach (var (key, profile) in Buildings.Profiles)
            {
                if (!AnyCopyPasteFileExists && (FileExists(key) || profile.Options.AdditionalBases.Keys.Exists(FileExists)))
                {
                    AnyCopyPasteFileExists = true;
                }
                SaveProfile(key, profile.Options);
                yield return CoroutineEx.waitForFixedUpdate;
            }

            yield return LoadBaseTables();

            VerifyProfiles();
            LoadImportedSkins();

            foreach (var profile in Buildings.Profiles.ToList())
            {
                if (GridController.SpawnsFileValid(profile.Value.Options.CustomSpawns.SpawnsFile))
                {
                    var spawns = GridController.GetSpawnsLocations(profile.Value.Options.CustomSpawns.SpawnsFile);

                    if (spawns.Count > 0)
                    {
                        Puts(mx("LoadedDifficulty", null, spawns.Count, profile.Value.Options.Mode));
                        profile.Value.Spawns = new(this, spawns);

                        if (profile.Value.Options.CustomSpawns.PreventBuilding)
                        {
                            Subscribe(nameof(CanBuild));
                        }

                        profile.Value.Options.CustomSpawns.BuyableTeleportRadius = profile.Value.Options.ProtectionRadius(RaidableType.Purchased);
                    }

                    yield return CoroutineEx.waitForFixedUpdate;
                }
            }

            Message(user, "Initialized base loot tables and profiles.");
        }

        private IEnumerator ReloadTables(IPlayer user)
        {
            yield return LoadTables();
            yield return LoadBaseTables(user);
        }

        private void LoadImportedSkins()
        {
            string skinsFilename = $"{Name}{Path.DirectorySeparatorChar}ImportedWorkshopSkins";
            try
            {
                ImportedWorkshopSkins = Interface.Oxide.DataFileSystem.ReadObject<SkinSettingsImportedWorkshop>(skinsFilename);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
            ImportedWorkshopSkins ??= new();
            ImportedWorkshopSkins.SkinList ??= new();
        }

        protected void SaveProfile(string key, BuildingOptions options)
        {
            Interface.Oxide.DataFileSystem.WriteObject($"{Name}{Path.DirectorySeparatorChar}Profiles{Path.DirectorySeparatorChar}{key}", options);
        }

        protected IEnumerator LoadTables()
        {
            _sb.Length = 0;
            _sb.AppendLine("-");

            var modes = GetRaidableModes();
            modes.Add(RaidableMode.Random);

            foreach (RaidableMode mode in modes)
            {
                string file = mode == RaidableMode.Random ? $"{Name}{Path.DirectorySeparatorChar}Default_Loot" : $"{Name}{Path.DirectorySeparatorChar}Difficulty_Loot{Path.DirectorySeparatorChar}{mode}";
                if (GetTable(file, out var lootList))
                {
                    LoadTable(file, Buildings.DifficultyLootLists[mode] = lootList);
                }
                yield return CoroutineEx.waitForFixedUpdate;
            }

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                string file = $"{Name}{Path.DirectorySeparatorChar}Weekday_Loot{Path.DirectorySeparatorChar}{day}";
                if (GetTable(file, out var lootList))
                {
                    LoadTable(file, Buildings.WeekdayLootLists[day] = lootList);
                }
                yield return CoroutineEx.waitForFixedUpdate;
            }
        }

        protected IEnumerator LoadBaseTables(IPlayer user = null)
        {
            foreach (var (key, profile) in Buildings.Profiles)
            {
                string file = $"{Name}{Path.DirectorySeparatorChar}Base_Loot{Path.DirectorySeparatorChar}{key}";
                if (GetTable(file, out var lootList))
                {
                    LoadTable(file, profile.BaseLootList = lootList);
                }
                yield return CoroutineEx.waitForFixedUpdate;
            }

            Interface.Oxide.LogInfo("{0}", _sb.ToString());
            _sb.Clear();

            Message(user, "Initialized tables.");
        }

        private void LoadTable(string file, List<LootItem> lootList)
        {
            if (lootList.Count == 0)
            {
                return;
            }

            if (lootList.All(ti => ti.probability == 0f))
            {
                lootList.ForEach(ti => ti.probability = 1f);
            }

            if (lootList.All(ti => ti.stacksize == 0))
            {
                lootList.ForEach(ti => ti.stacksize = -1);
            }

            Interface.Oxide.DataFileSystem.WriteObject(file, lootList);

            //var probs = new Dictionary<float, int>();

            lootList.ToList().ForEach(ti =>
            {
                if (ti.amount == 0 || string.IsNullOrEmpty(ti.shortname) || BlacklistedItems.Contains(ti.shortname))
                {
                    lootList.Remove(ti);
                    return;
                }
                //if (!probs.ContainsKey(ti.probability))
                //{
                //    probs[ti.probability] = 0;
                //}
                //probs[ti.probability]++;
                if (ti.amount < ti.amountMin)
                {
                    ti.amount = ti.amountMin;
                }
                if (ti.shortname == "chocholate")
                {
                    ti.shortname = "chocolate";
                }
                if (ti.shortname.EndsWith(".bp"))
                {
                    ti.shortname = ti.shortname.Replace(".bp", "");
                    ti.isBlueprint = true;
                }
            });

            if (lootList.Count == 0)
            {
                return;
            }

            //if (probs.Count > 0)
            //{
            //    _sb.Append(file);
            //    _sb.Append(string.Join("\n", probs.OrderBy(x => x.Key).Select(x => $"probability {x.Key} ({x.Value}x)")));
            //    _sb.AppendLine();
            //}

            _sb.AppendLine($"Loaded {lootList.Count} items from {file}");
        }

        private List<string> BlacklistedItems = new()
        {
            "ammo.snowballgun", "habrepair", "minihelicopter.repair", "scraptransport.repair", "vehicle.chassis", "vehicle.chassis.4mod", "vehicle.chassis.2mod", "vehicle.module", "car.key", "mlrs", "attackhelicopter",
            "scraptransportheli.repair", "snowmobile", "snowmobiletomaha", "submarineduo", "submarinesolo", "locomotive", "wagon", "workcart", "rhib", "rowboat", "tugboat", "door.key", "blueprintbase", "photo"
        };

        private bool GetTable(string file, out List<LootItem> lootList)
        {
            try
            {
                lootList = Interface.Oxide.DataFileSystem.ReadObject<List<LootItem>>(file);
            }
            catch (JsonReaderException ex)
            {
                Puts("Json error in loot table file: {0}\nUse a json validator: www.jsonlint.com\n\n{1}", file, ex);
                lootList = null;
                return false;
            }

            lootList ??= new();
            lootList.RemoveAll(ti => ti == null);

            return lootList.Count > 0;
        }

        #endregion

        #region Configuration

        private const string en_ru_compressed_64 = "H4sIAAAAAAAAA+19aXMc13Xo9/yKhuKUqCoK4iI7Dq0wAUlQYhUXhIQk+0mql8ZMA+hoZhqZniEIi6ziEppySImOK6nncrYnuyrvfUnVCCREEJv+QuMf5Wx37ds9PQPKyockZXHQ3fecu5x77tnuOZ/9URS9lvReOxN9Br/g99UsWkj63TTP0wwfv/azbBi1s6iXDaLV+GYSrem30SCLhnkSDVbTPGpl3W7ca8++djwAJ1rMovneIOmPB5jgZwwyuQl/VAM8N9wYD25puMHA2unyctoadgYbtRCji2knGQ92bdhvrcY09iR6/bMTd16PluBPDfrcMO20095KBJiXOlnr06QdLWf9KF+L13v5DCJ4p5V1sv6f//HFiyfg/86GWgx7g7QTxVE/TtvxUichHArGO28RgLM1KJvi6SVx30VSBn85breTfh7F/aQOfOCzKO2NA34u7vfTVtxOxsIPf9kAxfnh2lIW99tjMSxmWSdqhb5ugEUN/3ryt8MUWurZXuinN4GyVpIQSqS1XgIIltTna+pzIrZO3EqiDoMuoVzoZ8vwaXQVKHW+h31rI4pFJPo1eQc/kZATfn0m8vAD/SqomoIv6P0SXUhzDdZ6TJuK3xBt41ZQCCdDgF2HvYegPBzVcNSY4ptx2sGWVYDn1AfTg45g08e9jShbjjayYV8vUz4WZzTX7qa97wJztMhcd9hBumzDnhggN4oH/OH5bG1jIc7hGa4HkzFCRvaFDfOkfxP4bNqjZctupe3krXY8iKEjaxtr3DDrAMHpIV6Jb0XzyJFzHA78lXaH3aiTdtMBdg9GwQw7j475AzypB/gGMFPgO0kCOymJW6u4BTX83jDuMIroIoyfCY7/Xqa/cTPkg7g/mIneJ1LGB9nSIIZhxNHNuJO2o7UsTwfApWejhU5CHLq/EcUr8IkeyXtJZw1Bv4V9fmewdjZ6k8ECkC73gkZyHKYecCadZC2Dl4CLSFwQROlytLgAE7sy7GIXcVLXkla6nEJHsSGsA017DJ1DKtD4rwMPubaW9K4keR6vJDZDOH8C///sHE2ny/apSzR9GbRFHAOLknCq7YmeiX4m6M03cz/Eb07d6Wpyi9fjjdnoo89O3/nEZyuqk1ezK/Ha0Xpa3ZeTgb6cKvflanJrEMAN57dQHMJGXLMRfiodAKLw58fstPJwe8DhP0x7+UmXQVNHcTKiG4N4kJ9RDT/ueb2h/bieriW08CQz4LRY7Dbrx72VxNnvdIoQ94yBvgZAebCVvM/NogLtpTDcmY9783G+URrdKfPh7ehK0k5hf/rfnLa/eQ9OuNIXb9tfzN8CuadMZz+0v7marqwOuu7y8mc/umNm6+Pe5XQ5GaRdOTqrJudPDej6aflx02n5swbTcvLE2Hk5ebLBxJw81WxmTpp1KFPgqQCtf5Am68Tnb6ZtZFBGnIWJ7H2KR0K0tAHiOEoPHvUSn2PxITpx++TtU7dP335bdxMmOfi9Wq1SixCnsBjEdcULziEvAFBd2NQn76AUswHyyyfueFlgcrccTdFHIJCvEXPBxsdod30Iu+uNSLOHMy6oRSSU8ZAUGYYAcW+iS718iNObIhtZ4H4HAF/N1KCYAfFZKhO9kQxKohoxp7nhIOvCYd2OcKYQLD2O9WPcGMi8sMfA46HTRmFBES8brqxG13qdtEfzfZUEO3qoepPRy+gYQoCDBw/qNxQMEG+jKyhSGAj4qIuPgu35oH/DXm1eWhALB3GvFToVXBrwuf4Jw/W1/JTkg362YU2KeRJHnWQZZAyUV2hu6JDBmTmhZ+ZSr43fD1uDVCQ9X6oHcSMfwiqBzJHLiZE6jcqyfLYCPD/rkHb8Gs7FMViLN6iHeQTc1v7wXdiFA5ZY5GdkWli6IzQjvrbSz4ZAlKcdIDfg0GirPRS9N0RVOOejhESmTrayQlLQGXvsC/1kOb2FzT7SU/32iR+dO/1Ddx1yveU/ifRqJiT4XEgGSUu6L4+itjzzz1QhquEgMeLvfCvrZd20lV9ISDxSCjSfgkkrSWFgPhP8gX0QknI8SECEA/6F22ggCzZTwvFhOlht9+N1B8kaztg4BJ4mXQ0a5iAZBHa8A3EdBBxCi6BJ6G6B6tjO1lG2hfbV4N9NlweKplYVlBKqk27nCUPD/hvx2TdjvLIpspXCRdfAgkahPEpUi2h9NW2tgnzce30AJAy6uxH6b5Aacj1ZhxN3gU7xRqSDE3d9YQLiKeNZjD9NemNJKICoaoYqUFSRkge5MTGV0TShJjz5vHHU0FMZR2OCOtKEjaWp6wu1xPRp2ukAo09+utCYin46CRWdH+ZwSE/C4yZjcQy/MX9runddsDUEOTk1uqAbUmJTMnSBT0KCU0xLHe3hWHpZ1KIGMBW5NqXNGAGEzA+XBkkXIcifUQp/R/lq1h/04m5Cx/Zs9D6IL2/hjgRJkT54R39xNnon7mZw8J+NPspBqtfS8hwIlRo6/UGwz7CUSE3OoKh4PMJmZ2zxhMe5IGaLG7z4/FBOdbIow25qxWTYgAX2ZQwXxvWkm4mkUgenz59pKeEaWQOMDYn/RjQ0G6zLm697LHmT3wCatbIKyynRQMJfobEFDh6UYDfMsi98sMBL74t4Ho5GCCYHu/DB/NF6P98UzcQDGA+5v3E9+RstHAaAt+Ie25bRb4MDIFIgS2Q7S9ieuZR0MmR6GW73ALbLZF9fzJTwH0DDBsz1SEzxZIxDFwZ6XcoAL6b9fFCztiVGAaCQJS9jO9sNlXi2rVqjm8F/HrUMpu8JEecJO5U6GdqBa018ZbQ4fxdB4ctXFSPj/ocAWX3QNtlhT/lF1L6nxT2XXEEGY7ivrDn10T7J8HSGmery1x6URTGkKiDKJB13Otm6LKmytS73gakEnIAMiflPDRzmPLiIwK/QwFYB7gpwr3MJaOTAf9Ofm/F14bnoXUGPTNxqJXnOEEun+LX1XtJnitaHiZ4g3gk5rXTWTTJQs5OOfSjRAGEN24rZEpclMz8Ochne0BbutWFIyhatm5PJH8hPTmtsy16AFLV6fYrr76FzMCOgthHrDpAqUhAeMeOJ31BRm6GhP6vOSFmiX+nMK+oF2xcn7MK54caNjd4gvhXoBL/gExdWc+N2l2yJt1fRWhjgAotxfyUZXIXVhO3QvhRiBPwJQaTFxQ+Po0WOXTVohSkxNxfs1SwI+GpGomYXHSL97CbORAkQjHUO8KwmfeOb8jxVr1czjddLvqrj0ZrlcWHIIbc74DUoYBzaWbaYteMj9QB2FUCYoB/IL6EH57PecroyxDO0n60l/c5GYEoXxOVvfGkt3QwnmdrN+s69OWRKyoaoYgxQcCArkn0KKF8qe52AmSlnau5LuTbgD4z+IiLpbKQ5GD/x6R9GexNt19gL6BAIAQoEkV4LWNWAZQPdgZTcj1Hay9NcznZlK5SjYEmGhbsuD5EajdcWsssUS20RIGzgaAktmnQCMpdNu+WtAGDRsHUZBc4ktL36GymLHcQ22YNKLMQXVUGyCEE/j/bNToe+yRHVDWZscyEF6mcavOZBig+ysZKsZyDozpAGUOZKinHqFYWOt6gHs5HTE02B/WQZeEAcsl7ilOu332PXiZpFPYZ5/+z0nT+Rblct56vpaAXwOaDWIfQLXbYVVplVW/mNjbX5JAM/dacJbNhkeeZbosMoUIBC4IToGMB/g9CdZnRvB9DBh381TIbBfWRmaD0FHsJ0vr6aoIfcuK5zPmtmo0vkuG4N+33oNGgduLPVVzKd2L+/RXSlBRMbPewxGn7IR2s+cW33dRJx2W9rne9iei6HwaCYcyk/19GClyWoKu++6YLSUhJLcLucZZ/OgzC2jk8RggkyELFLTc1PUPT9lCQ3+rgU9+UcKn5naEmEsIiXyjlmzhMrRE1BzF14wC7ZwqAjQiw/QQCQSHi0v4YkvYYXCz9S9ikMC4Pls2atQoG7oAS+OW3+MTKgDJQDK5jCLJeWadrpBNoCBbO3WDXXknvWTi7DIlKjDv2w3qAfGF+gmGY/Z+8vvmHZzX6HTl98g+KcA4s8vQSNf1nvtIMXX/f0H+U5vwqCvhOI5M28PrKXcN/CideLTp440UW3d+zOv0xOlA5C29HGh3pIuwpXvMFWBgVOVkmHLqgAxzKXtrBciLvxSiUOIXcPAxG8URUYQh0S2JWDCXFkwIQxbArnccUg6xCkEq53+2n7Un45i3EbKa15pY/Wu5xMzPD4J0qmXI/TgcRlpjyOZdG3Z4PwLmb9bjzQnkEHKMZwUcgcKZMIa4AeA+K7ObpPQBrKgffEfaIJOiSUxJKj8RB2iDidsbu6A4tZtgCacH952LkAcmnIG0rBgRRWCD9WhkDyrCohi+OWwKbSPhAZSZNtAMPi0zoe50Cdy8AbygqcQfwKkMKkxjDq8WhRULMU3oDxiLYWLZ09t4Hw5TJry7LuXK/9LtBOt866Ri7qmMD/HJV67K9IsrD8sQ4AVvoIrqX0o4Rzjhogp7jYz7rXhoMc9Lc65IgIbWM+Mmuw5fMShPULSYcVLv0H+/xvcJs3zClkDh/cjhSX1+9n/TMRnENkBSJxQkkLfP7MOM0xSA/0BVJa3fZD0gmwkXwjwNQI/hInywI2L1adAChj8CEIIhL6AM5lt4KtpSNL+HpMH3AWRE4NTAa6T+QIs4QL9FVxkCM24b/Zm84xhxzXPes2sMJirRZL/DTc5ArMIs1ku4RHvQg3vIHxmMNOqV2unoebudYDqx3RuNviUg/WB+SWn8MnyA0NV1QvWB+D7QgMd5DoQBi1Z3FfIdwuCLepeohBshRZkQNgGOaa8EsQodeA5fcQsgSJVfYEiD5WAojfyA7vyZN4CZo4DBekvXR5g5V8Zvj2bp27cOHH8z86+xnGaZkw6Mg7zT5D948Rc4+J+cqXhEHoG1qfmfhB/0PuuR8jp7c0b2hQ5kx/4Y8zUX9plndv9A7MxNnoI8TwSfRRPlzKk8EnbvO5dpsESm0x80DAERX1QO+7dZL/eZv/+SH/8yMFDK9cXMiSHKZx/laaU2eisxzgTNHk2o+Q4OuPe1UNT2l7a6CZ48+6lEvYvOCSvwzoq9kHKF6j9I3C9YdxvyfSQUDyRvKwjEFEfCoemXpCQg/1Ip+NLqQcG6xbWF/moi6jhMDRxp3hStr7C8sNKN3gX86gyC240E9BBiV91LgK5SGFAZWbICjcdXHHbWWelxs6FjzxKV7wPaYDXx0RfdfcTJiJnHsKpLJyO8uqh43woGaxi5prmS6k6uhuQL8WM69nAecmebnmOv0kbm8QHeVMRzF5tdCIyO/UCprgKfQyVFK/OCF4AzhtYOFA3aGYLNh+2PTjnnph6TzLeKZY/RS3q7fC8hQdFLTGS/5SyQflVVYtY7POSxKzyR4TG6SttElDnLeU7fUKFNnG5bEWCldWOolsMOuCCjFa+37KurZgBpvatsNAW2U+tQ4nlINAerL9ROI9zIbiYlLB68xDrSGCRHSxE6+I3/YjT/KCx7bp1cTTSbNQm4rv50NoTpz4sUEzX9ks1Kbi+4Vo/vy16P1LJK3AL3hiv+QXzsP5Uot5+6VqoR9qn/Gt1LOsBSXlCbzoAHCMb7sMs8Kx7VqJbgyA286tx/oeYRNzEfCdVjwUXRlpapBlUauTsXpOIgLKPLPRfEqSPjEB4WC9rNVJ1ywGgnyez1LeRaIZClubnfU+VaLdeSKXG4NsbQ2oWcl10KoPlI1OI7eZEuzcZkqsq25GImzfa8UPKxuhXMURMCJfoaJrxoHmyfNZfy1X7lu8qkR/KmbMwQaB71r0p2H9EpbAG1mHEdlC2Rx7hIO6KDD3uKWMobb7OJUbUqjGlWwFAvF/wTvie/K32PREWhSRFQGYw0JITreUvxu0fP8Sy2jI0GDKr7ABVNhg1wwXvulE8lHOkor7yKjLtEmUzszdKT2Ur98DHox2DLJy4DrUxGiQms32EF6QTqzMh6KKLSXAZ5Pob2C4dNaxNlya5iDS8524Qr8H2oB3EYbPR38NaP7a6QlgSP2unLzTDOdiEleo/SDHwLvvAudFOD17FVPcB2rHt9OjXEy7lsz/2YkzF07dQdMS/gvn4Sn81ycVkY3wUmsC/LONMJxQiZBpBQUn0FNpmZ3rdwJUhX8SsxWh1uLB6FAUDizeC7my2Jf4dBQbYSaQvYyBrO9gTgk+knbREt6MVGEfG2skf7/VX4rSlR5Q9f+WpsHe3Ihvjhkj6wmJvpmZRzm18Ucni0FRJgF4XZByU9CdxRMbbmy8R+4SsoRNW3etn9xMs2HOrh+YFlr7iE1lrJvP1AEHzpiOQRHjJ/nrE6Hi6IfFLLsY9/XJMOAwCuVGobNYrsRgsI57LjhtjS+/vhHuSxWkRic1rpllWJaYtJDcjc1EyldScoc5srYu0S0Rr5lixF5THYcbbouXNunM7qDvxtwlyejMNloTv7bn5DxOwGI2p/xNpALZko2yXCq111p8DmawjxEJ8lRSl3ImkMCq/9LWhCvxWnQl7n/KBK1MR3YIK1tn3SO9IvbLN+lGffTLkIff9qpRP7mb57N8UHavefG/scn5AGpRK6ny1nlwS2A9KNTPsnoczFbhANfBMm5XMcSNtSjXGDvss2GNImSqYFraGcyzAZtK0Iu+7h8CXQUUlYoSZXjjHmRmFUkhULOUczRML3PjUUgRJ/dJnwUKLxpFT7J2AqlUGhYY6gMFu3DM1RIZB/DAiHRwDfl+406Ol8WQBLildSd+jen2Wr+d9DGUHL2CQsRCw9Ydr/LnKM85LSyiPwdjQbXdOrxI2bGDUslQMLtEX1rXzGZmZlwo9n6RryUmMlHaTW4toIR7sn+POZ7tbaPHHOWZz1S0QteF3y5TTZ0wURXLa0EyIMT7sZih0eJST3lCbH9pSk/RVYozI5qaRoXPnLD/IHQJ0JoGQSzEV0KwmM33kq6201SwKwVFuCvDel0CVQMwyXmh7ZxL2S0k47TbHfYSA86+mHet19lwRoQMEqPMkYY4PCrr0J1Pmy+ou6U9rZhBewFj7GYwPDIP0A+jIm0IB9HPlNMI/9WXbbUEQz/cx0ZCvfbh1fnr+pY42qTtaBCtI6WXeqjO3UxQNr2c6HsoWj/C2/vD/AwN4dr716Mbi3OL79+oAmyfPZ0YQNIOOia/9XmLFzKUJnbLNh/Zd5vy6wvqWDvG97B0+yswvxv65Q9cMQBjpPBCrHuZSXJn8EsORr2+UGqkborUNLvlWvSkoe5RVcsfWBLOYn9jDkV7IkeVZgMVj5jOjgRjirTH2tBkJ7kZD4DhvpfEncEqEZD76Ezp03f72IvzEqKhH0scdp+z7eAhvUIfyvMWfD9TgnWuA0JmA1BL+F0tpOswYQ0A0a2LOjg3MG8IaOoNYOXyaS08nWwor4VWjm3P7ftMKWi276tb/W9xMBg9fIduKnlf0rbNV9O1iJyhDschOsr0ByaoBRiWwJRg+FkPqtguXJhynEhLdicA6KynPQSkCUvT5NYajtaHbK6T4cjW49xnzAIerXxs10vV9XPL/kcjixUuH4cVaaW3E4NlSznARUtIwHglADxp1+xHq58EQckENMsUVIE3zTxolJmoY04D6zRKE/v4jZhxnYkudjbKWiuoDj2iavxGjvJl+nA2AOKD2FxHGQPkZuxF0lhgFHtvACaVT0NgtDRFP0CI7OPOyB1wDCwPNccZdBRvJSJQEiXSsURKUOS9hPc70pXUTlDlAbSXJACPLlVR5NwUoKMFNIG00rW4N/BxgOBiXjYDfjEe3nIEUowjw4nD/UBJjjC/QsyWTNqYQGOxEZcvX78RyZ2HC0kvrdXjRKknKOoM0TRqXQFFZVg+ygYzCmD4pT7tQNBnBwZ5eXUSp/yMCq24rRTt222Mi7ktO/x2TmKE5rg3JfPT8pDDF90HnvrrpiK71tNRdqQYJaoperPKWc3U7anor9A4IveCFq3cVComhOD8RamZxPRgxP8ccKU19/6yZ7uL6QuN2zSQX8rTmrRUDhH1E17cwbev9VFarMopWfw+KjaL0eG9qNgvtg7vR8W3h3eLUbEZFc+L3cOn8PbwcfFtcVDs4gfwv214ePjF4X149DIqduCfPfh+v3h++LhByskSPsL2Av67dfi5hg//jBDzQ4D+HH4IwujwHjz4+vAx/LFdbI1NSDkJMhzizuED+Bf+awYIGHfhxzfw0cHhPcTbLGnl9KiLr+nDx5zM8gh5LIuvDu8DvgPAdB8w7h4+oQFswjwWLwDH1zSyHXiNH23SIh7gsh9Qlw6fwrNd01XVLaELHEnxEv4YTZv/csr+HRDN7eJP6lKDDjXMmFn8MywOdgFH/AscakU38NWmoIc1fthkThrn1Sz+AUDeBVDbgHSE2+pV9qNx8k1cHZxnwIFEvA3Y7uF6AZ3uIVHA2xH82n6VnZsuZ2fxjwz3Aa0dUQn0YAe7tmsvKG3IYg82HYzhGyambRwMDOuJ5nv4FHcljzZEnU3zfhb/iTOoWOrB4d8Btl3EBAwhAlQ7xMU27Tk7Sj7Q4iuXUyEmwA4TsXv45eEj2sgjzdXtLj09WpLQAOKavJp0ijyXL5HnOb0ynNBitHVpRF8F8n0kFfoFpw2Rzx5Sxgvm4J/DAj1EKn9OtLVdvJwg1+j30r2IKS8C2kbUdPo8IGYC/Jsa3WcyHBGFALYH9F+kbjhWIyCLEexQ7Pimlb5UrRSi4V7A4bgFdLRJ/92i7f4tvMbV2wpmMq1IYQo7WA7X4hnuTtwze7RD7tEOHeG+QY6CEscuPdrGb3ASUF5yxIKXtVlPxyU5Lf4NZxS41g4OFlnbt8hY6E8bzeHTmaj4V1guYAjw2Z5aZKbhXWjzCPuI8wwLykCoe3vw60v+6gVyJfjfl7Bi/04AcKAPipfMyI7zV7RTAS0+v4+zvHX4S1wkkidqM6ma7lOPsTOuAIUi1l3q6j73ajtiZgf/QwngLrUT7oTtAZIILYqgmBoCYzqOHUXhadv5NsLDrXhmThHM3IoEvonnBRx3uLbEr5n/joiH3p0gYytsOXeI1okD67rFtMTSKx9OW0REzCxhzI9582zW5nQtfo393ceDjbYlnRLQ5yduQ5XoFejZ7HI6E3Crbx0t4+urGemEYzlZN5bmGWPtvjNNbTn9AmUiIuYJDBGpC2keqd6lXzzWaMwvXlmCWZJ7RtQt3rC089W8wk6ozjsL/GCkNIc9ljZewqbYYuKmrhOrJA5DG0PAwgd1qWltGWrbYTAEBdkuTgrOUEhhqcliC588InF6c+bjHoq/wH93iIfWZrSFObpLJ9V+8NvT7reqP8S+a3PcFv9JLP9bmqT79blui99C5z/Hw+Hw7ph0t8X/YW3yHrE4mpQDlK/pT+BFd0lUnXyJ/vQ7W6IfT7NEfzbBErlZdseskZtvd8wiuXl3a1fJSr07WQre4t9Jer2Hxy6dEizC81rRpt2HWdrWYiXyleeKHwLzeIjKAh9r+1UrYo7040Q0sqZPQIZ6gDAdw8dkeX6FKF5prt/i/wY4/0jr7bABiPPvcQZgJntRpTebJwIufocCicngC39vocAOhI1UVvyaWd40mYFLoGnT/po37W9olz44eqJgYtGuWmhEKmUBcyanaepg77Ci6QB1kg8DOVAekUjEu1czFusMoyXaHJtnmOROURNEQHqk1Fmr5yQQoISO5HyMl59lKxRCi726RMQ0mm3YuTt8lu2RHPGsGQ4tuDtYGqUrHk/G5RTGljxizKF+EuPi97Taj2g9vlHKsEwhLNPniIXE26AM5XQBl2iChMfAih8Qm3hARonDe3zO7AgH2gZqEUvKPlECmQmBjD6nKXzcOBsyLhA1/xoZTE1aZGKfFgUpOx8eg8oKMrKSJtNckRAr/JQ2IzDEb6FFVeJkUKQeEuPdJxmK7UT25CrbET16pgV+34LUNMFyiHLgNNvShstmOZeLf4Pv98kC94DpJOKjDjeo0QYnFE7N5kPltjZZMwvjvgBRlyyYadIsvthsVYcsOqvP4MyYaZAAgzhWM9yhHcMaIU/9gwbpnWHeGSsbqg4aYL0Hc01igLNCsHBPoStAcc2yPvsDrs3+LNsfzS7oJJh+3JbBwXcU1A7cN6EG1TzgGZWIHYuhMlBqaQiZwJOgNORJQGxU+oIOAbIF4SY/fHCc5CKRr0dar8MpfmZ2VDEak3W60R7w8htPsQtqUlE33AuhPjSninF5qsftiSD2xruiPn31uG3hpbGedmPU5bcetzP84U+6N+qzXh91c1xfsDcDmy5Dm6EqZ3bjPfDTI+2BUiLtpniPjnSKs+cIR09F6u0Ge2zaDVadkbvB5jryzqpK2d1gV02/paoTeRe/NpYxW/kO7ifRw1AMU144UDMjttyQ+V8+rUr6LZZ5XB3sqUWKI5LV9uUxSdjiDdsi2zNqMiPJDl78xtvXbIA/UsJwdHXQtInM7eHmMFLxcIvCyF5HySku+uP++Lzixb9I4wMJ31B6j2zNb6kT+yW/hu9I3ZWNgZ82y0R+BMxEZIKxMls5qBa2pd71/pxRWcwnS1/ODGiTrFVisavca3gbQe23SLzkT3DN9Ib5QsUooMocsCGMz3Y+bX+mzoA+GcL5VzsB4Uvl03Zp6iTqyhGyZblOyXyP3I5Z06an5TkS74EYkXhTo6kFHVpIzt+ww5ICECbPuk5mNja4PhFPWU3Qxb7xQhjHz6vIzC7nHQaEvcRxi5+M1gfWonZ9NLnU+tVM55qnbQ/2CjgJ+rKfc7+ey4o9Goe/QSJ3ZWfgea7rF2AE5GzQCa1VXWr3Ckpk2iMgI+PPdSxJxy1PHlm0kGKIdPDRXk0meLLe7imv8V02iopd0T4IjTu45C2OOH5mk8hgzwQKPvPY9OHTyjzy1b1QpwOMioNbANrfk/35vvCJyTCHUs5LLJEThAHi144WXpDheXNORCWRRYAcUYkfFc13tkltXGCRc/HG5K2XcI7SWlfzmR3u6pu8Q5+TtvGMn43Jb4+SkzmHD5h+ED7zP5TWSC1AO7/IUHvVp31dNnyy7bP1cUfmxvifOdBA+Mk3HKY1Tt4em0EfI+uMK6SOD8jm5TgHI5KU9/tUOfSn6UdJwZqoL3XJ9IuvyC55X5ntD++90sT6xX8wkfNg9/mEHNEZIUZ4cZHuSwiDeBOmy7ZP5KtFas++Hd3ARBGXLjTOvx+I3qrNgh+K36qK4pFNSYejrwMdPpkgRf+kncQJJmbwHFb96XfU50nS+VPsk47689U+EWm2NBvdVgpoKcM/HM0mxFqCr4y3ihi0CTDkAwxFZyWzWC6CUuAkOYEphkDRlwRP0lP96Qj+e79Bt6wMVLMSfvOceOc34vXQELe9mEGesd+S2ZWPdS9Ei7WBTTtbldpidIBQoKB8OP+mOxQrZoulTFkK6P82HXEiD9MxRschKKTsUvsaPzeTvW0JL88YYGDq3zRTP02hAqB89Pc/0tYMUdSflpHVI6otXUDk+S1JD+qcUspAzcmngjO3LDOJ1o4sFfjItQ5QXkLvpeWe1GcXi557WoZSrktxMkczVSaO8WUF3IXH7bLHQydOgJEASGqbLKiizEMcmSUdZTJUjVCA2rf4NrnYdGgHCMpHqqjw33GC/CAhekYFGSSeVCaOxUVr6qap1HCkCZi+eAPtCmPitBVACkHZfDUFHKrQUBAJlXBATKcZ4aQlHIJThxKhudFjTx4bA4DhPXe/dCQeCRF+rlU0dh7XMRPcTzryml0Je+R04whaOgzCErS+XyRxuUB3sroUNLIlglJZ2mpQUCIQUrHNgck6VtKxEodEXUtfronltAVi7UVvUHeiyp6zT/Z+92SQ7pa8+PryGYfB8BoZxaZUqcJWoLasZVBnxqRr/xO+jGUY4rbYpBy9zura+AoYVcYFQ88mYFztpwrPrierwXlmIpcq6mWwiO43PJB7DuqKwzM2Jm5JMPym0ZbNa33nwVLqG1XXAMo1St02x4jgtYJvaJn40K4k4yY1N7y4eq1EBhhhGYopv1ELRmRR6ZU2atjVOFzXpf2JKstBc4ABpQf2W1OcQ50+uNr2F6pEh60FOPB1qQ40BZtwUfsbp2QHUZcEigbWclzVjoAM7XLDJ8qioJXDLTo1HmJ1D4yLZF9v1aI7p7lZB94iWwHzasMKINxvZxe6wF3remAzsnWVpHUWeEmSn65WSBVj8MZbyRK0sQK52KbYbkTPn6qwyGQdUpxyFz8R4x91clTsyI0puqkgaynWMe70gWJ/m0IcqGCOrU+CGojSC2y/vmb9zL0rbvY8pzdy30Ke7bCtR85zCRfQIel42Bs8L3DFGxQ7GdtLvH9EeuAviPff48/2D59iGCHfr6IwX4xi5AvCZCh6yBN+F72SLNORod0YXEhY2avVlADHnrrfJqafHesMG19Axdx1Ewy4kx/SYuIA9BGMx+cvuUMRqRhkDhftWxjlvlGU0avxJV9n2+Vr2ZvKcotsZWvCiiuvqJfK9m/cARP3s75ES4CP2pTKIpNPEp4RoMpX8AUamNzA+bKrbkyVFxOdssNyINsWEA+qkXRobnvSHslyyCZGbOQsLF8ZB34j7doDmroODOkL98TswfcYHaTcj03ym4hlaVsuZdXid0rEYGQ8KRZ3YYy4qfHtMR9GbckY+5bjmUhLZLaOon1mcn2FIv3IUPSMdsuI1Yn60jJj8GjysuRhEscY2X06jCko2hhPGtWhGY+XLr3+0g06GTWoUTPliOTHjrmU0qCWjYfrH/WKqLNBq/HasFfYwZV+oRsiHDsO84ApT8mYeDGmRhUx955LBXEqILt2ZYa/497yrlV+fIxuPZ0qpMyw1AYRwyW64iYbnlODpwqXDuSis8zkqZh4Mj3HQwW6GnihQCo7CiZU76f4TbEvdgFWK17wtmYbJZ/F264IIkYi5b1yJYNtlgL26BjdpZPAeksNd8h156mbd0mFRSMaChKneQ1p+++rzkk4/x5fUeY7cXSHYGwZIb6VqUULPmPvUhyVVjMrzCeKnEJdwUtk3BEY4ndaf4j4Cx3w4ZgDrxCRmxVlXDEic72wriCRawXVVA6cLVCnSNnKPoGfvBf59tA3JphtXPWiZviOVtTIBPhKugNmqnIwSFc5mHdMnaMJINWWPhIhXPKENKmAhOdPOVJSsx8/88BjL1ME30Vyeym31/bsLBBKAHXwiPOrhMNKIEHioHt/Rmku28ophLzzGcpygcpKfrAkE8CYOkulRnzPq+BoRvcqV5MKTCF4zzUH3ncMU5WwQ0WaQglbwkY1E1anY1rcBC4zUX2yGcsUbR/JFH8qd7oCYjvrZ55iWobodbeu+lNgyMFo01E52jRcGoqGrRJ10B0yhIisJrj9KgtGNWM4k9WRKlsZyWSHe9Bm0Q0KSzmQmpJzZbEpH1oTYhbNCjttI3fM+7VlqRykjvDEUsJLo0eHi1S5zJGFj/tOWKTtsB9fuGosPC8EIFzNytxbqAjGNMK+bVsUbeN/ilxZGF5tkasKQ4Fo48gNJBNPs+DpysJXE+CpCVCuKYbVyIfluayUzWHb8r29IHYN/PG4EmklmvGRhPFy/gvn1vfXohSQ7XnHils4fEApi/hYtxxGKm2MnO7+vSj2nY8pvaXhOoZO1h2CJogxtblAiw4eMDtiPed0ESOOJ6XwRq01uq6lUAWvpsCpt/eE21UAtmt8NYZrQkno6nhFCbBqRa+yIJi4vrdUjK927wTLgpW+NnZ1wvaMlBjJUfDCCY7RxNqsXhiIYyoB2ZbnIVTmci94edOKhidDYcBd41cTc2K2lCV7y6ie28EUmZsKAfqV7LTPfskx0u9D+S2Phqa2PlmxZ+a3VKAMr72TaUnfmXf7Br8sA3K5aJkVwyfGlsp4XQWnaTmzgEna84kwB9SWcHEBUXzjl/hIM2MJbjmunIDknKLAA4oNJOf6PZJin3OiDOM43LHd7S+PVCaN94qEc7PtSIqI6VGSYH6kUR6xrJrq432xW+PM/PL762VlITbpp1qbP0gPQ3XbDh9J4TayY9GTe96GqSrdNqXLJUzChY7atR0O4hgWb3UofWBNJbgqAcQBak2a9phbsVCHVqLIYD4Nyxc5pksmteYfuF9R2Vcvc+2lWKTDhq/h0FXhESYxmbg43fTDU9q6MzjkNq7D0Q4o2/Jn3S9t16g3+8WWmFV1wIJ0UHdvbBW8JrvBtkj4m/uedZ0XBvbc5KZSkgHd3Pbj3xoW0Pueeser9yV8xMl08Ealey3ClNLTIYvozCfV1pfqlR6+w17o+yF9IVCjj13QR4Hll+4rfiXfjiw9RceQVKUAH9XYTawKf8VXSOhsHSpLMyNbM29Q9M+GFpJqauAFCgHCGu2yDxcnDzNThzQ2vj9IlLDN0uy4KoF+1i5Pf3Mvgtnxc19a5G9KCAaFOvcynVZgnbqCxW+pkRh7jlhcUC4EspUoGOw/UcTBBKUHi3+1cwhKYGI4WWCF3dbPWEB5xgvrtkvzgoXhsMkGKDzj8ZiSBuHKhnTTwrozVB0PoO5xFV7acSu4c1R3d6i6DiKGQFci9Yx1nNDzXjC9xJjqiA2IPnRFifevGwmzr+UsUSRHoeUBvHy5qMpn5N43Qp154YN5Thlt3UqtunzDUSj+fRja9Y94Ue0Dy7Pt24EFvmeBDFrW3b2QIZ8Ck2gKOM3+XaueIxAR13NUQZl6hN9LUUcrL8qWck2USnbAMEP1HoNFHlUsUPgc29VBb18QUT30aOzwYX0RyBqzIJO6kkYlso3yMqLuQEu0N1MBXNWKnBR86NY57XuDdcvCObaqZON4Xr4LgPqpN4H+VqyaFsmPMGFRyj9c//S2tq5uV9S1rLnAXwnf6dSXtojgJfscBbDrCpjGAy5xTqwMPADp6AVZ5vb0KaQiLY2p0qqMGZjWmtQDRs9w8o3YeVa3gpVLjGMmaH00WVZNwLrU3MQOGo+kFN0UVxJX8yAJ3TIrqvPTkt+tuyMD42HmyK5yTU5fWKwqzwnf/XPxK4D1T/DvPxX/MX2lTmMptEp1AvhfFf8f017+Dn78rvh98dWEJTvR6GmZMg++h9qdaodIaij21owt3VndakzlzoqGVYU75aZpTbmHJtd+AkU9ifw47flT5YtGReHvkPjHlfhUxTlQeiAtQlpS6h9ShL5WcsoLplHlGJZYL9nSJp/GmEKgzREiOys4g/cEyOxaoRMMbkd8OBMPzq8pOtH4UNm04s4mxe3WH51gtLoKXMOUJcEqpZKe5BndtFFHzzucdK1B1VKVjMWGYtVOk/i9N43/0j05dNz3pmGg0PNfkKlUe+PNOVRV6LRKFgoNzs4684g9YXSjZEdf+jBiNlcioDhwnPGAkaK+UqrXpXCHlEy7KUZv62bb8UgZ5/iGjOSxsSXgJp2yrtmJM9ztBom6hhpqHCzKKbhnJTq0FPZyJVZlGvDl9IOxuAz446EkOpwlcI9vFNQVbzW04Zno7MD4QBHXSpoqvOJB++wEx77x+jhJjiRuh5IWjiv5OjVKLwkPku0jMn/tC3G73uyKUrHToQ9ms/I6hAeAqaYWrNNYKjoLIkxFpSGnl5b2LGmPyn18HELklKct/p8R9KsLzpUGfnSOVlHe9g/dH20m2NLpuBxj9l7jormvvOeyqFIMQuymwZl0Cu5WEXOVslLowlZaLxGCf+zGJxwQxx15FByu1luv7rH5nwNZDOczuQGtywTNavmaCNNd3hUm51lFHrJQlV/Sdfx44cKrCXb4+KjVfwuvNBaHGpoitjqaJVwRuHASLrrZkQ60LbgUd1VbxTBYNjgURGWdUr5yHoDfsL5wwLxVf41PVpST7QXqD5PSqz/QMb+mDjFnaSX5QQ5fKkn8R3f+CzaObhnpygAA";

        private Dictionary<string, Dictionary<string, string>> DecompressedLanguageMessages() => JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(Encoding.UTF8.GetString(Facepunch.Utility.Compression.Uncompress(Convert.FromBase64String(en_ru_compressed_64))));

        protected override void LoadDefaultMessages()
        {
            foreach (var language in DecompressedLanguageMessages()) 
            {
                lang.RegisterMessages(language.Value, this, language.Key);
            }
        }

        public void TryMessage(BasePlayer player, string key, params object[] args)
        {
            if (player.IsValid() && !waiting.Contains(player.userID))
            {
                ulong userid = player.userID;

                waiting.Add(userid);
                QueueNotification(player, key, args);
                timer.Once(10f, () => waiting.Remove(userid));
            }
        }

        public void Message(BasePlayer player, string key, params object[] args)
        {
            if (player.IsReallyValid())
            {
                QueueNotification(player, key, args);
            }
        }

        public void Message(IPlayer user, string key, params object[] args)
        {
            if (user != null)
            {
                user.Reply(mx(key, null, args));
            }
        }

        private void CheckNotifications()
        {
            if (_notifications.Count > 0)
            {
                foreach (var (userid, notifications) in _notifications.ToList())
                {
                    var notification = notifications.ElementAt(0);

                    SendNotification(notification);

                    notifications.Remove(notification);

                    if (notifications.Count == 0)
                    {
                        _notifications.Remove(userid);
                    }
                }
            }
        }

        private void QueueNotification(BasePlayer player, string key, params object[] args)
        {
            if (!player.IsOnline())
            {
                return;
            }

            string message = m(key, player.UserIDString, args);

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (config.EventMessages.Message)
            {
                Player.Message(player, message, config.Settings.ChatID);
            }

            if (config.GUIAnnouncement.Enabled || config.UI.AA.Enabled || config.EventMessages.NotifyType != -1)
            {
                if (!_notifications.TryGetValue(player.userID, out var notifications))
                {
                    _notifications[player.userID] = notifications = new();
                }

                notifications.Add(new()
                {
                    player = player,
                    messageEx = mx(key, player.UserIDString, args)
                });
            }
        }

        private void SendNotification(Notification notification)
        {
            if (!notification.player.IsOnline())
            {
                return;
            }

            if (config.GUIAnnouncement.Enabled && GUIAnnouncements.CanCall())
            {
                GUIAnnouncements?.Call("CreateAnnouncement", notification.messageEx, config.GUIAnnouncement.TintColor, config.GUIAnnouncement.TextColor, notification.player);
            }

            if (config.UI.AA.Enabled && AdvancedAlerts.CanCall())
            {
                AdvancedAlerts?.Call("SpawnAlert", notification.player, "hook", notification.messageEx, config.UI.AA.AnchorMin, config.UI.AA.AnchorMax, config.UI.AA.Time);
            }

            if (config.EventMessages.NotifyType != -1 && Notify.CanCall())
            {
                Notify?.Call("SendNotify", notification.player, config.EventMessages.NotifyType, notification.messageEx);
            }
        }

        public string m(string key, string id = null, params object[] args)
        {
            if (id == null || id == "server_console")
            {
                return mx(key, id, args);
            }

            _sb2.Length = 0;

            if (config.EventMessages.Prefix)
            {
                _sb2.Append(lang.GetMessage("Prefix", this, id));
            }

            string message = lang.GetMessage(key, this, id);

            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            _sb2.Append(message);

            return args.Length > 0 ? string.Format(_sb2.ToString(), args) : _sb2.ToString();
        }

        public string mx(string key, string id = null, params object[] args)
        {
            _sb2.Length = 0;

            string message = lang.GetMessage(key, this, id);

            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            _sb2.Append(id == null || id == "server_console" ? rf(message) : message);

            return args.Length > 0 ? string.Format(_sb2.ToString(), args) : _sb2.ToString();
        }

        public static string rf(string source) => source.Contains(">") ? Regex.Replace(source, "<.*?>", string.Empty) : source;

        public class Notification { public BasePlayer player; public string messageEx; }


        private Dictionary<ulong, List<Notification>> _notifications = new();

        private List<ulong> waiting = new();

        protected new static void Puts(string format, params object[] args)
        {
            if (!string.IsNullOrEmpty(format))
            {
                Interface.Oxide.LogInfo("[{0}] {1}", Name, (args.Length != 0) ? string.Format(format, args) : format);
            }
        }

        private Configuration config;

        private static Dictionary<string, List<ulong>> DefaultImportedSkins() => new()
        {
            ["jacket.snow"] = new() { 785868744, 939797621 },
            ["knife.bone"] = new() { 1228176194, 2038837066 }
        };

        private static Dictionary<string, List<CustomCostOptions>> DefaultCustomCosts() => new()
        {
            ["Easy"] = new() { new(50) },
            ["Medium"] = new() { new(100) },
            ["Hard"] = new() { new(150) },
            ["Expert"] = new() { new(200) },
            ["Nightmare"] = new() { new(250) }
        };

        private static List<PasteOption> DefaultPasteOptions() => new()
        {
            new() { Key = "stability", Value = "false" },
            new() { Key = "autoheight", Value = "false" },
            new() { Key = "height", Value = "1.0" },
        };

        private static Dictionary<string, BuildingOptions> DefaultBuildingOptions() => new()
        {
            ["Easy Bases"] = new(RaidableMode.Easy, "EasyBase1", "EasyBase2", "EasyBase3", "EasyBase4", "EasyBase5") { NPC = new(15.0) },
            ["Medium Bases"] = new(RaidableMode.Medium, "MediumBase1", "MediumBase2", "MediumBase3", "MediumBase4", "MediumBase5") { NPC = new(15.0) },
            ["Hard Bases"] = new(RaidableMode.Hard, "HardBase1", "HardBase2", "HardBase3", "HardBase4", "HardBase5") { NPC = new(20.0) },
            ["Expert Bases"] = new(RaidableMode.Expert, "ExpertBase1", "ExpertBase2", "ExpertBase3", "ExpertBase4", "ExpertBase5") { NPC = new(25.0) },
            ["Nightmare Bases"] = new(RaidableMode.Nightmare, "NightmareBase1", "NightmareBase2", "NightmareBase3", "NightmareBase4", "NightmareBase5") { NPC = new(30.0) }
        };

        private static List<LootItem> DefaultLoot() => new()
        {
            new("ammo.pistol", 40, 40),
            new("ammo.pistol.fire", 40, 40),
            new("ammo.pistol.hv", 40, 40),
            new("ammo.rifle", 60, 60),
            new("ammo.rifle.explosive", 60, 60),
            new("ammo.rifle.hv", 60, 60),
            new("ammo.rifle.incendiary", 60, 60),
            new("ammo.shotgun", 24, 24),
            new("ammo.shotgun.slug", 40, 40),
            new("surveycharge", 20, 20),
            new("bucket.helmet", 1, 1),
            new("cctv.camera", 1, 1),
            new("coffeecan.helmet", 1, 1),
            new("explosive.timed", 1, 1),
            new("metal.facemask", 1, 1),
            new("metal.plate.torso", 1, 1),
            new("mining.quarry", 1, 1),
            new("pistol.m92", 1, 1),
            new("rifle.ak", 1, 1),
            new("rifle.bolt", 1, 1),
            new("rifle.lr300", 1, 1),
            new("shotgun.pump", 1, 1),
            new("shotgun.spas12", 1, 1),
            new("smg.2", 1, 1),
            new("smg.mp5", 1, 1),
            new("smg.thompson", 1, 1),
            new("supply.signal", 1, 1),
            new("targeting.computer", 1, 1),
            new("metal.refined", 150, 150),
            new("stones", 7500, 15000),
            new("sulfur", 2500, 7500),
            new("metal.fragments", 2500, 7500),
            new("charcoal", 1000, 5000),
            new("gunpowder", 1000, 3500),
            new("scrap", 100, 150)
        };

        public class DifficultyModesInt
        {
            [JsonProperty(PropertyName = en ? "Easy" : "Легкий")]
            public int Easy;

            [JsonProperty(PropertyName = en ? "Medium" : "Средний")]
            public int Medium;

            [JsonProperty(PropertyName = en ? "Hard" : "Тяжело")]
            public int Hard;

            [JsonProperty(PropertyName = en ? "Expert" : "Эксперт")]
            public int Expert;

            [JsonProperty(PropertyName = en ? "Nightmare" : "Кошмарный")]
            public int Nightmare;
        }

        public class DayLimitSettings
        {
            [JsonProperty(PropertyName = en ? "Monday" : "Понедельник")]
            public bool Monday = true;

            [JsonProperty(PropertyName = en ? "Tuesday" : "Вторник")]
            public bool Tuesday = true;

            [JsonProperty(PropertyName = en ? "Wednesday" : "Среда")]
            public bool Wednesday = true;

            [JsonProperty(PropertyName = en ? "Thursday" : "Четверг")]
            public bool Thursday = true;

            [JsonProperty(PropertyName = en ? "Friday" : "Пятница")]
            public bool Friday = true;

            [JsonProperty(PropertyName = en ? "Saturday" : "Суббота")]
            public bool Saturday = true;

            [JsonProperty(PropertyName = en ? "Sunday" : "Воскресенье")]
            public bool Sunday = true;
        }

        public class BaseLockoutSettings
        {
            [JsonProperty(PropertyName = en ? "Apply Lockouts To PVE" : "Применять блокировки к PVE")]
            public bool PVE = true;

            [JsonProperty(PropertyName = en ? "Apply Lockouts To PVP" : "Применять блокировки к PVP")]
            public bool PVP = true;

            [JsonProperty(PropertyName = en ? "Apply All Lockouts Everytime" : "Применять Все Блокировки при рейде Базы любого уровня")]
            public bool Global;

            [JsonProperty(PropertyName = en ? "Time Between Raids In Minutes (Easy)" : "Время между рейдами в минутах (Легкий)")]
            public double Easy;

            [JsonProperty(PropertyName = en ? "Time Between Raids In Minutes (Medium)" : "Время между рейдами в минутах (Средний)")]
            public double Medium;

            [JsonProperty(PropertyName = en ? "Time Between Raids In Minutes (Hard)" : "Время между рейдами в минутах (Тяжело)")]
            public double Hard;

            [JsonProperty(PropertyName = en ? "Time Between Raids In Minutes (Expert)" : "Время между рейдами в минутах (Эксперт)")]
            public double Expert;

            [JsonProperty(PropertyName = en ? "Time Between Raids In Minutes (Nightmare)" : "Время между рейдами в минутах (Кошмарный)")]
            public double Nightmare;

            [JsonProperty(PropertyName = en ? "Block Clans From Owning More Than One Raid" : "Запретить кланам владеть более чем одним рейдом")]
            public bool BlockClans;

            [JsonProperty(PropertyName = en ? "Block Friends From Owning More Than One Raid" : "Запретить друзьям владеть более чем одним рейдом")]
            public bool BlockFriends;

            [JsonProperty(PropertyName = en ? "Block Teams From Owning More Than One Raid" : "Запретить командам владеть более чем одним рейдом")]
            public bool BlockTeams;

            [JsonProperty(PropertyName = en ? "Block Players From Joining A Clan/Team To Exploit Restrictions" : "Запретить игрокам вступать в Клан/Команду для обхода ограничений")]
            public bool AllyExploit;

            public bool Any() => Easy > 0 || Medium > 0 || Hard > 0 || Expert > 0 || Nightmare > 0;

            public bool IsBlocking() => BlockClans || BlockFriends || BlockTeams;
        }

        public class BaseAmountSettings : DifficultyModesInt
        {
            [JsonProperty(PropertyName = en ? "Allow Max Amount Increase From Difficulties Disabled On A Specific Day Of The Week" : "Увеличивать Максимальное Количество Баз в определённый день недели на количество Баз Отключенных Сложностей")]
            public bool Merge;

            public bool Any() => Easy > -1 || Medium > -1 || Hard > -1 || Expert > -1 || Nightmare > -1;

            public int Get(RaidableBases instance, RaidableMode a)
            {
                int j = GetInternal(a);

                if (j > 0 && Merge && !instance.CanSpawnDifficultyToday(a))
                {
                    foreach (RaidableMode b in GetRaidableModes())
                    {
                        if (a != b && !instance.CanSpawnDifficultyToday(b))
                        {
                            j += Math.Max(0, GetInternal(b));
                        }
                    }
                }

                return j;
            }

            private int GetInternal(RaidableMode mode) => mode switch
            {
                RaidableMode.Easy => Easy,
                RaidableMode.Medium => Medium,
                RaidableMode.Hard => Hard,
                RaidableMode.Expert => Expert,
                _ => Nightmare
            };
        }

        public class BaseChanceSettings
        {
            [JsonProperty(PropertyName = en ? "Use Cumulative Probability" : "Использовать кумулятивную систему вероятности (рандома)")]
            public bool Cumulative = true;

            [JsonProperty(PropertyName = en ? "Easy" : "Легкий")]
            public decimal Easy = -1m;

            [JsonProperty(PropertyName = en ? "Medium" : "Средний")]
            public decimal Medium = -1m;

            [JsonProperty(PropertyName = en ? "Hard" : "Тяжело")]
            public decimal Hard = -1m;

            [JsonProperty(PropertyName = en ? "Expert" : "Эксперт")]
            public decimal Expert = -1m;

            [JsonProperty(PropertyName = en ? "Nightmare" : "Кошмарный")]
            public decimal Nightmare = -1m;

            public decimal Get(RaidableMode mode) => mode switch
            {
                RaidableMode.Easy => Easy,
                RaidableMode.Medium => Medium,
                RaidableMode.Hard => Hard,
                RaidableMode.Expert => Expert,
                _ => Nightmare
            };
        }

        public class Color1Settings
        {
            [JsonProperty(PropertyName = en ? "Easy" : "Легкий")]
            public string Easy = "000000";

            [JsonProperty(PropertyName = en ? "Medium" : "Средний")]
            public string Medium = "000000";

            [JsonProperty(PropertyName = en ? "Hard" : "Тяжело")]
            public string Hard = "000000";

            [JsonProperty(PropertyName = en ? "Expert" : "Эксперт")]
            public string Expert = "000000";

            [JsonProperty(PropertyName = en ? "Nightmare" : "Кошмарный")]
            public string Nightmare = "000000";

            public string Get(RaidableMode mode)
            {
                string hex = mode switch
                {
                    RaidableMode.Easy => Easy,
                    RaidableMode.Medium => Medium,
                    RaidableMode.Hard => Hard,
                    RaidableMode.Expert => Expert,
                    _ => Nightmare
                };

                return hex.StartsWith("#") ? hex : $"#{hex}";
            }
        }

        public class Color2Settings
        {
            [JsonProperty(PropertyName = en ? "Easy" : "Легкий")]
            public string Easy = "00FF00"; // Green

            [JsonProperty(PropertyName = en ? "Medium" : "Средний")]
            public string Medium = "FFEB04"; // Yellow

            [JsonProperty(PropertyName = en ? "Hard" : "Тяжело")]
            public string Hard = "FF0000"; // Red

            [JsonProperty(PropertyName = en ? "Expert" : "Эксперт")]
            public string Expert = "0000FF"; // Blue

            [JsonProperty(PropertyName = en ? "Nightmare" : "Кошмарный")]
            public string Nightmare = "000000"; // Black

            public string Get(RaidableMode mode)
            {
                string hex = mode switch
                {
                    RaidableMode.Easy => Easy,
                    RaidableMode.Medium => Medium,
                    RaidableMode.Hard => Hard,
                    RaidableMode.Expert => Expert,
                    _ => Nightmare
                };

                return hex.StartsWith("#") ? hex : $"#{hex}";
            }
        }

        public class ManagementMountableSettings
        {
            [JsonProperty(PropertyName = en ? "All Controlled Mounts" : "Весь управляемый транспорт")]
            public bool ControlledMounts;

            [JsonProperty(PropertyName = en ? "All Other Mounts" : "Весь остальной транспорт")]
            public bool Other;

            [JsonProperty(PropertyName = en ? "Attack Helicopters" : "Боевые вертолеты")]
            public bool AttackHelicopters;

            [JsonProperty(PropertyName = en ? "Boats" : "Лодки")]
            public bool Boats;

            [JsonProperty(PropertyName = en ? "Campers" : "Кемперский модуль (на машине)")]
            public bool Campers = true;

            [JsonProperty(PropertyName = en ? "Cars (Basic)" : "Машины (Базовые)")]
            public bool BasicCars;

            [JsonProperty(PropertyName = en ? "Cars (Modular)" : "Машины (Модульные)")]
            public bool ModularCars;

            [JsonProperty(PropertyName = en ? "Chinook" : "Чинук")]
            public bool CH47;

            [JsonProperty(PropertyName = en ? "Flying Carpet" : "Flying Carpet (Plugin)")]
            public bool FlyingCarpet;

            [JsonProperty(PropertyName = en ? "Horses" : "Лошади")]
            public bool Horses;

            [JsonProperty(PropertyName = en ? "HotAirBalloon" : "Воздушные шары")]
            public bool HotAirBalloon = true;

            [JsonProperty(PropertyName = en ? "Jetpacks" : "Jetpacks (Plugin)")]
            public bool Jetpacks = true;

            [JsonProperty(PropertyName = en ? "MiniCopters" : "Миникоптер")]
            public bool MiniCopters;

            [JsonProperty(PropertyName = en ? "Parachutes" : "Парашюты")]
            public bool Parachutes;

            [JsonProperty(PropertyName = en ? "Pianos" : "Пианино")]
            public bool Pianos = true;

            [JsonProperty(PropertyName = en ? "Scrap Transport Helicopters" : "Грузовой вертолет (Корова)")]
            public bool Scrap;

            [JsonProperty(PropertyName = en ? "Snowmobiles" : "Снегоходы")]
            public bool Snowmobile;

            [JsonProperty(PropertyName = en ? "Tugboats" : "Буксиры")]
            public bool Tugboats;
        }

        public class BuildingOptionsSetupSettings
        {
            [JsonProperty(PropertyName = en ? "Amount Of Entities To Spawn Per Batch" : "Количество объектов для спавна")]
            public int SpawnLimit = 1;

            [JsonProperty(PropertyName = en ? "Amount Of Entities To Despawn Per Batch" : "Количество объектов для удаления")]
            public int DespawnLimit = 10;

            [JsonProperty(PropertyName = en ? "Height Adjustment Applied To This Paste" : "Регулировка высота для данной базы")]
            public float PasteHeightAdjustment;

            [JsonProperty(PropertyName = en ? "Force All Bases To Spawn At Height Level (0 = Water)" : "Статичная высота для всех баз (0 = Уровень Воды)")]
            public float ForcedHeight = -1f;

            [JsonProperty(PropertyName = en ? "Foundations Immune To Damage When Forced Height Is Applied" : "Запретить наносить урон фундаменту (при статичной высоте)")]
            public bool FoundationsImmune;

            [JsonProperty(PropertyName = en ? "Kill These Prefabs After Paste" : "Список префабов для удаления после спавна базы", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> BlockedPrefabs = new();
        }

        public class PlayerAmountsEventTypeSettings
        {
            [JsonProperty(PropertyName = en ? "Buyable Events" : "Покупаемые События")]
            public int Buyable;

            [JsonProperty(PropertyName = en ? "Maintained Events" : "Поддерживаемых Событий")]
            public int Maintained;

            [JsonProperty(PropertyName = en ? "Manual Events" : "Ручные События")]
            public int Manual;

            [JsonProperty(PropertyName = en ? "Scheduled Events" : "Запланированные События")]
            public int Scheduled;
        }

        public class ManagementPlayerAmountsSettings
        {
            [JsonProperty(PropertyName = en ? "Easy" : "Легкий")]
            public PlayerAmountsEventTypeSettings Easy = new();

            [JsonProperty(PropertyName = en ? "Medium" : "Средний")]
            public PlayerAmountsEventTypeSettings Medium = new();

            [JsonProperty(PropertyName = en ? "Hard" : "Тяжело")]
            public PlayerAmountsEventTypeSettings Hard = new();

            [JsonProperty(PropertyName = en ? "Expert" : "Эксперт")]
            public PlayerAmountsEventTypeSettings Expert = new();

            [JsonProperty(PropertyName = en ? "Nightmare" : "Кошмарный")]
            public PlayerAmountsEventTypeSettings Nightmare = new();

            [JsonProperty(PropertyName = en ? "Bypass For PVP Bases" : "Обход (Bypass) для PVP баз")]
            public bool BypassPVP;

            internal Dictionary<RaidableMode, (int Maintained, int Scheduled, int Purchased, int Manual)> Values;

            public int Get(RaidableMode mode, RaidableType type)
            {
                Values ??= new Dictionary<RaidableMode, (int Maintained, int Scheduled, int Purchased, int Manual)>
                {
                    { RaidableMode.Easy, ( Easy.Maintained, Easy.Scheduled, Easy.Buyable, Easy.Manual) },
                    { RaidableMode.Medium, (Medium.Maintained, Medium.Scheduled, Medium.Buyable, Medium.Manual) },
                    { RaidableMode.Hard, ( Hard.Maintained, Hard.Scheduled, Hard.Buyable, Hard.Manual) },
                    { RaidableMode.Expert, ( Expert.Maintained, Expert.Scheduled, Expert.Buyable, Expert.Manual) },
                    { RaidableMode.Nightmare, ( Nightmare.Maintained, Nightmare.Scheduled, Nightmare.Buyable, Nightmare.Manual) }
                };

                return Values.TryGetValue(mode, out var levels) ? type switch
                {
                    RaidableType.Maintained => levels.Maintained,
                    RaidableType.Scheduled => levels.Scheduled,
                    RaidableType.Purchased => levels.Purchased,
                    _ => levels.Manual
                } : 0;
            }
        }

        public class ManagementDropSettings
        {
            [JsonProperty(PropertyName = en ? "Auto Turrets" : "Автоматические турели")]
            public bool AutoTurret;

            [JsonProperty(PropertyName = en ? "Flame Turret" : "Пламенная турель")]
            public bool FlameTurret;

            [JsonProperty(PropertyName = en ? "Fog Machine" : "Туманная машина")]
            public bool FogMachine;

            [JsonProperty(PropertyName = en ? "Gun Trap" : "Ловушка с дробовиком (гантрап)")]
            public bool GunTrap;

            [JsonProperty(PropertyName = en ? "SAM Site" : "Зенитная установка САМ")]
            public bool SamSite;

            public bool Get(BaseEntity entity) => entity switch
            {
                AutoTurret _ => AutoTurret,
                FlameTurret _ => FlameTurret,
                FogMachine _ => FogMachine,
                GunTrap _ => GunTrap,
                SamSite _ => SamSite,
                _ => false
            };
        }

        public class ManagementSettingsLocations
        {
            [JsonProperty(PropertyName = "position")]
            public string _position;
            public float radius;
            public ManagementSettingsLocations() { }
            public ManagementSettingsLocations(Vector3 position, float radius)
            {
                (_position, this.radius) = (position.ToString(), radius);
            }
            internal Vector3 position { get { try { return _position.ToVector3(); } catch { Puts("{0} is invalid in config file.", _position); return default; } } }
        }

        public class ManagementBiomeSettings
        {
            [JsonProperty(PropertyName = en ? "Arctic" : "Arctic")]
            public bool Arctic = true;

            [JsonProperty(PropertyName = en ? "Arid" : "Arid")]
            public bool Arid = true;

            [JsonProperty(PropertyName = en ? "Temperate" : "Temperate")]
            public bool Temperate = true;

            [JsonProperty(PropertyName = en ? "Tundra" : "Tundra")]
            public bool Tundra = true;

            public bool IsBiomeEnabled(Vector3 a) => (TerrainBiome.Enum)(TerrainMeta.BiomeMap?.GetBiomeMaxType(a) ?? -1) switch
            {
                TerrainBiome.Enum.Arctic => Arctic,
                TerrainBiome.Enum.Arid => Arid,
                TerrainBiome.Enum.Temperate => Temperate,
                TerrainBiome.Enum.Tundra => Tundra,
                _ => true
            };
        }

        public class ManagementSettings : DespawnOptionsBase
        {
            [JsonProperty(PropertyName = en ? "Grids To Block Spawns At" : "Сетки для Блокировки Спауна", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> BlockedGrids = new();

            [JsonProperty(PropertyName = en ? "Block Spawns At Positions" : "Блокировать спаун в позиции", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ManagementSettingsLocations> BlockedPositions = new() { new(Vector3.zero, 200f) };

            [JsonProperty(PropertyName = en ? "Additional Map Prefabs To Block Spawns At" : "Дополнительные префабы для блокировки спауна", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, float> BlockedPrefabs = new() { ["test_prefab"] = 150f, ["test_prefab_2"] = 125.25f };

            [JsonProperty(PropertyName = en ? "Eject Mounts" : "Не допускать транспортные средства")]
            public ManagementMountableSettings Mounts = new();

            [JsonProperty(PropertyName = en ? "Max Amount Of Players Allowed To Enter Each Difficulty (0 = infinite, -1 = none)" : "Максимальное количество участников для каждой сложности (0 = бесконечно, -1 = никого)")]
            public ManagementPlayerAmountsSettings Players = new();

            [JsonProperty(PropertyName = en ? "Max Amount Allowed To Automatically Spawn Per Difficulty (0 = infinite, -1 = disabled)" : "Максимальное количество Баз для автоматического появления для каждой сложности (0 = бесконечно, -1 = отключено)")]
            public BaseAmountSettings Amounts = new();

            [JsonProperty(PropertyName = en ? "Chance To Automatically Spawn Each Difficulty (-1 = ignore)" : "Вероятность автоматического спауна в каждой сложности (-1 = игнорировать)")]
            public BaseChanceSettings Chances = new();

            [JsonProperty(PropertyName = en ? "Player Lockouts (0 = ignore)" : "Блокировки Игроков (0 = игнорировать)")]
            public BaseLockoutSettings Lockout = new();

            [JsonProperty(PropertyName = en ? "Easy Raids Can Spawn On" : "Дни спавна легких рейд-баз")]
            public DayLimitSettings Easy = new();

            [JsonProperty(PropertyName = en ? "Medium Raids Can Spawn On" : "Дни спавна cредний рейд-баз")]
            public DayLimitSettings Medium = new();

            [JsonProperty(PropertyName = en ? "Hard Raids Can Spawn On" : "Дни спавна сложных рейд-баз")]
            public DayLimitSettings Hard = new();

            [JsonProperty(PropertyName = en ? "Expert Raids Can Spawn On" : "Дни спавна Эксперт рейд-баз")]
            public DayLimitSettings Expert = new();

            [JsonProperty(PropertyName = en ? "Nightmare Raids Can Spawn On" : "Дни спавна Кошмарный рейд-баз")]
            public DayLimitSettings Nightmare = new();

            [JsonProperty(PropertyName = en ? "Additional Containers To Include As Boxes" : "Дополнительные контейнеры для распределения лута, как в ящиках", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Inherit = new();

            [JsonProperty(PropertyName = en ? "Difficulty Colors (Border)" : "Цвета сложности (Обводка)")]
            public Color1Settings Colors1 = new();

            [JsonProperty(PropertyName = en ? "Difficulty Colors (Inner)" : "Цвета сложности (Заполнение)")]
            public Color2Settings Colors2 = new();

            [JsonProperty(PropertyName = en ? "Entities Allowed To Drop Loot" : "Объекты с которых выпадает лут")]
            public ManagementDropSettings DropLoot = new();

            [JsonProperty(PropertyName = en ? "Additional Blocked Colliders" : "Коллайдеры для блокировок", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> AdditionalBlockedColliders = new() { "cubes" };

            [JsonProperty(PropertyName = en ? "Allow Teleport" : "Разрешить телепортацию")]
            public bool AllowTeleport;

            [JsonProperty(PropertyName = en ? "Allow Teleport Ignores Respawning" : "Разрешить телепорт игнорировать возрождение")]
            public bool AllowRespawn;

            [JsonProperty(PropertyName = en ? "Allow Cupboard Loot To Drop" : "Разрешить выпадение лута из шкафов")]
            public bool AllowCupboardLoot = true;

            [JsonProperty(PropertyName = en ? "Allow Players To Build" : "Разрешить строить игрокам")]
            public bool AllowBuilding = true;

            [JsonProperty(PropertyName = en ? "Allow Players To Build (Exclusions)" : "Разрешить строить игрокам (Исключительные объекты, даже если строить - false)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> AllowedBuildingBlocks = new();

            [JsonProperty(PropertyName = en ? "Allow Players To Use Ladders" : "Разрешить использовать лестницы")]
            public bool AllowLadders = true;

            [JsonProperty(PropertyName = en ? "Allow Players To Upgrade Event Buildings" : "Разрешить улучшение строений")]
            public bool AllowUpgrade;

            [JsonProperty(PropertyName = en ? "Allow Player Bags To Be Lootable At PVP Bases" : "Разрешить лутать чужие рюкзаки на PVP Базах")]
            public bool PlayersLootableInPVP = true;

            [JsonProperty(PropertyName = en ? "Allow Player Bags To Be Lootable At PVE Bases" : "Разрешить лутать чужие рюкзаки на PVE Базах")]
            public bool PlayersLootableInPVE = true;

            [JsonProperty(PropertyName = en ? "Allow Players To Loot Traps" : "Разрешить лутать ловушки (турелли и т.д)")]
            public bool LootableTraps;

            [JsonProperty(PropertyName = en ? "Allow Npcs To Target Other Npcs" : "Разрешить НПС Нападать на Других НПС")]
            public bool TargetNpcs;

            [JsonProperty(PropertyName = en ? "Allow Raid Bases Inland" : "Спавнить рейд-базы на островах")]
            public bool AllowInland = true;

            [JsonProperty(PropertyName = en ? "Allow Raid Bases On Beaches" : "Спавнить рейд-базы на пляжах")]
            public bool AllowOnBeach = true;

            [JsonProperty(PropertyName = en ? "Allow Raid Bases On Ice Sheets" : "Спавнить рейд-базы ледяных глыбах")]
            public bool AllowOnIceSheets;

            [JsonProperty(PropertyName = en ? "Allow Raid Bases On Roads" : "Спавнить рейд-базы возле дорог")]
            public bool AllowOnRoads = true;

            [JsonProperty(PropertyName = en ? "Allow Raid Bases On Rivers" : "Спавнить рейд-базы возле рек")]
            public bool AllowOnRivers = true;

            [JsonProperty(PropertyName = en ? "Allow Raid Bases On Railroads" : "Спавнить рейд-базы возле ЖД-путей")]
            public bool AllowOnRailroads;

            [JsonProperty(PropertyName = en ? "Allow Raid Bases On Building Topology" : "Разрешить Рейдовые Базы на Застроенной территории")]
            public bool AllowOnBuildingTopology = true;

            [JsonProperty(PropertyName = en ? "Allow Raid Bases On Monument Topology" : "Спавнить рейд-базы на территории монументов (рт)")]
            public bool AllowOnMonumentTopology;

            [JsonProperty(PropertyName = en ? "Allow Raid Bases In Biomes" : "Спавнить рейд-базы по разным биомам")]
            public ManagementBiomeSettings Biomes = new();

            [JsonProperty(PropertyName = en ? "Amount Of Spawn Position Checks Per Frame (ADVANCED USERS ONLY)" : "Количество Проверок Позиций Спауна за Кадр (ТОЛЬКО ДЛЯ ОПЫТНЫХ ПОЛЬЗОВАТЕЛЕЙ)")]
            public int SpawnChecks = 25;

            [JsonProperty(PropertyName = en ? "Allow Vending Machines To Broadcast" : "Отображать торговые аппараты в базах на карте")]
            public bool AllowBroadcasting;

            [JsonProperty(PropertyName = en ? "Backpacks Can Be Opened At PVE Bases" : "Можно ли открыть рюкзак на PVE Базах")]
            public bool BackpacksOpenPVE = true;

            [JsonProperty(PropertyName = en ? "Backpacks Can Be Opened At PVP Bases" : "Можно ли открыть рюкзак на на PVP Базах")]
            public bool BackpacksOpenPVP = true;

            [JsonProperty(PropertyName = en ? "Backpacks Drop At PVE Bases" : "Рюкзаки выпадают на PVE базах")]
            public bool BackpacksPVE;

            [JsonProperty(PropertyName = en ? "Backpacks Drop At PVP Bases" : "Рюкзаки выпадают на PVP базах")]
            public bool BackpacksPVP;

            [JsonProperty(PropertyName = en ? "Block Custom Loot Plugin" : "Блокировать Custom Loot Plugin")]
            public bool BlockCustomLootNPC;

            [JsonProperty(PropertyName = en ? "Block Npc Kits Plugin" : "Блокировать Npc Kits Plugin")]
            public bool BlockNpcKits;

            [JsonProperty(PropertyName = en ? "Block Helicopter Damage To Bases" : "Блокировать урон от вертолета по базам")]
            public bool BlockHelicopterDamage;

            [JsonProperty(PropertyName = en ? "Block Mounted Damage To Bases And Players" : "Блокировать урон от транспортных средств по Базам и Игрокам")]
            public bool BlockMounts;

            [JsonProperty(PropertyName = en ? "Block Mini Collision Damage" : "Блокировать урон от столкновений по миникоптеру")]
            public bool MiniCollision;

            [JsonProperty(PropertyName = en ? "Block DoubleJump Plugin" : "Отключить DoubleJump Plugin")]
            public bool NoDoubleJump = true;

            [JsonProperty(PropertyName = en ? "Block RestoreUponDeath Plugin For PVP Bases" : "Отключить RestoreUponDeath Plugin для PVP баз")]
            public bool BlockRestorePVP;

            [JsonProperty(PropertyName = en ? "Block RestoreUponDeath Plugin For PVE Bases" : "Отключить RestoreUponDeath Plugin для PVE баз")]
            public bool BlockRestorePVE;

            [JsonProperty(PropertyName = en ? "Block LifeSupport Plugin" : "Отключить LifeSupport Plugin")]
            public bool NoLifeSupport = true;

            [JsonProperty(PropertyName = en ? "Block Rewards During Server Restart" : "Блокировать Награды Во Время Перезагрузки Сервера")]
            public bool Restart;

            [JsonProperty(PropertyName = en ? "Bypass Lock Treasure To First Attacker For PVE Bases" : "Обход Блокировки Сокровища для Первого Атакующего на PVE Базах")]
            public bool BypassUseOwnersForPVE;

            [JsonProperty(PropertyName = en ? "Bypass Lock Treasure To First Attacker For PVP Bases" : "Обход Блокировки Сокровища для Первого Атакующего на PVP Базах")]
            public bool BypassUseOwnersForPVP;

            [JsonProperty(PropertyName = en ? "Despawn Spawned Mounts" : "Удалять Транспортные Средства при Спавне")]
            public bool DespawnMounts = true;

            [JsonProperty(PropertyName = en ? "Do Not Destroy Player Built Deployables" : "Не Уничтожать Игровые Постройки Игроков")]
            public bool KeepDeployables = true;

            [JsonProperty(PropertyName = en ? "Do Not Destroy Player Built Structures" : "Не Уничтожать Структуры, Построенные Игроками")]
            public bool KeepStructures = true;

            [JsonProperty(PropertyName = en ? "Divide Rewards Among All Raiders" : "Делить Награды Между Всеми Рейдерами")]
            public bool DivideRewards = true;

            [JsonProperty(PropertyName = en ? "Draw Corpse Time (Seconds)" : "Время жизни трупа (Секунды)")]
            public float DrawTime = 300f;

            [JsonProperty(PropertyName = en ? "Destroy Boxes Clipped Too Far Into Terrain" : "Уничтожать ящики заспавненные под землей")]
            public bool ClippedBoxes = true;

            [JsonProperty(PropertyName = en ? "Destroy Turrets Clipped Too Far Into Terrain" : "Уничтожать турели заспавненные под землей")]
            public bool ClippedTurrets = true;

            [JsonProperty(PropertyName = en ? "Eject Sleepers Before Spawning Base" : "Изгнание Спящих Перед Появлением Базы")]
            public bool EjectSleepers = true;

            [JsonProperty(PropertyName = en ? "Eject Scavengers When Raid Is Completed" : "Выкидывать за купол Мародеров После Завершения Рейда")]
            public bool EjectScavengers = true;

            [JsonProperty(PropertyName = en ? "Eject Mountables Before Spawning A Base" : "Выкидывать за купол Транспортные Средства Перед Появлением Базы")]
            public bool EjectMountables;

            [JsonProperty(PropertyName = en ? "Kill Deployables Before Spawning A Base" : "Уничтожение Построек Перед Появлением Базы")]
            public bool KillDeployables;

            [JsonProperty(PropertyName = en ? "Eject Deployables Before Spawning A Base" : "Выкидывать за купол Размещённые Предметы Перед Появлением Базы")]
            public bool EjectDeployables;

            [JsonProperty(PropertyName = en ? "Extra Distance To Spawn From Monuments" : "Расстояние для Спауна от Монументов")]
            public float MonumentDistance = 25f;

            [JsonProperty(PropertyName = en ? "Move Cookables Into Ovens" : "Перемещать и плавить руду и нефть в печки и НПЗ")]
            public bool Cook = true;

            [JsonProperty(PropertyName = en ? "Move Food Into BBQ Or Fridge" : "Перемещать Еду в Мангалы или Холодильники")]
            public bool Food = true;

            [JsonProperty(PropertyName = en ? "Blacklist For BBQ And Fridge" : "Черный Список для Мангалов и Холодильников")]
            public HashSet<string> Foods = new() { "syrup", "pancakes" };

            [JsonProperty(PropertyName = en ? "Move Resources Into Tool Cupboard" : "Перемещать Ресурсы в Шкаф для Инструментов")]
            public bool Cupboard = true;

            [JsonProperty(PropertyName = en ? "Move Items Into Lockers" : "Перемещать Предметы в шкафы для переодевания")]
            public bool Lockers;

            [JsonProperty(PropertyName = en ? "Divide Locker Loot When Enabled" : "Разделить лут между шкафами для переодевания, если включено")]
            public bool DivideLockerLoot = true;

            [JsonProperty(PropertyName = en ? "Lock Treasure To First Attacker" : "Заблокировать Базу для Первого Атакующего")]
            public bool UseOwners = true;

            [JsonProperty(PropertyName = en ? "Lock Treasure Max Inactive Time (Minutes)" : "Время Неактивности до снятия блокировки с Базы (Минуты)")]
            public float LockTime = 20f;

            [JsonProperty(PropertyName = en ? "Assign Lockout When Lock Treasure Max Inactive Time Expires" : "Назначить Блокировку при Истечении Максимального Времени Неактивности Сокровища")]
            public bool SetLockout;

            [JsonProperty(PropertyName = en ? "Lock Players To Raid Base After Entering Zone" : "Заблокировать Базу на Игроков После Входа в Зону")]
            public bool LockToRaidOnEnter;

            [JsonProperty(PropertyName = en ? "Only Award First Attacker and Allies" : "Награждать Только Первого Атакующего и Его Союзников")]
            public bool OnlyAwardAllies;

            [JsonProperty(PropertyName = en ? "Only Award Owner Of Raid" : "Награждать Только Владельца Рейда")]
            public bool OnlyAwardOwner;

            [JsonProperty(PropertyName = en ? "Mounts Can Take Damage From Players" : "Транспортные Средства Могут Получать Урон от Игроков")]
            public bool MountDamageFromPlayers;

            [JsonProperty(PropertyName = en ? "Player Cupboard Detection Radius" : "Радиус Обнаружения Шкафов Игроков")]
            public float CupboardDetectionRadius = 125f;

            [JsonProperty(PropertyName = en ? "Players With PVP Delay Can Damage Anything Inside Zone" : "Игроки с Задержкой PVP Могут Наносить Урон Любому Объекту в Зоне")]
            public bool PVPDelayDamageInside;

            [JsonProperty(PropertyName = en ? "Players With PVP Delay Can Damage Other Players With PVP Delay Anywhere" : "Игроки с Задержкой PVP Могут Везде Наносить Урон Другим Игрокам с Задержкой PVP")]
            public bool PVPDelayAnywhere;

            [JsonProperty(PropertyName = en ? "PVP Delay Between Zone Hopping" : "Задержка PVP Между Перемещениями по Зонам")]
            public float PVPDelay = 10f;

            [JsonProperty(PropertyName = en ? "PVP Delay Triggers When Entity Destroyed From Outside Zone" : "Задержка PVP Активируется При Уничтожении Объекта Извне Зоны")]
            public bool PVPDelayTrigger;

            [JsonProperty(PropertyName = en ? "Prevent Fire From Spreading" : "Предотвратить Распространение Огня")]
            public bool PreventFireFromSpreading = true;

            [JsonProperty(PropertyName = en ? "Prevent Players From Hogging Raids" : "Предотвратить Захват Рейдов Игроками")]
            public bool PreventHogging = true;

            [JsonProperty(PropertyName = en ? "Prevent Fall Damage When Base Despawns" : "Предотвратить Урон от Падения при Исчезновении Базы")]
            public bool PreventFallDamage;

            [JsonProperty(PropertyName = en ? "Require Cupboard To Be Looted Before Despawning" : "Требовать Ограбления Шкафа перед Исчезновением")]
            public bool RequireCupboardLooted;

            [JsonProperty(PropertyName = en ? "Destroying The Cupboard Completes The Raid" : "Уничтожение Шкафа Завершает Рейд")]
            public bool EndWhenCupboardIsDestroyed;

            [JsonProperty(PropertyName = en ? "Require All Bases To Spawn Before Respawning An Existing Base" : "Требовать Появления Всех Баз Перед повторным созданием такой же Базы")]
            public bool RequireAllSpawned;

            [JsonProperty(PropertyName = en ? "Require All Bases To Spawn For Individual Players" : "Требовать, чтобы все базы спавнились для индивидуальных игроков")]
            public bool RequireAllSpawnedBuyableEvents;

            [JsonProperty(PropertyName = en ? "Require All Bases To Spawn Persists On Restart" : "Требование о Появлении Всех Баз Сохраняется После Перезапуска")]
            public bool RequireAllSpawnedPersists;

            [JsonProperty(PropertyName = en ? "Turn Lights On At Night" : "Включать Освещение Ночью")]
            public bool Lights = true;

            [JsonProperty(PropertyName = en ? "Turn Lights On Indefinitely" : "Включать Освещение на Всегда")]
            public bool AlwaysLights;

            [JsonProperty(PropertyName = en ? "Turn Lights On Bypasses NightLantern" : "Включение Освещения переназначает настройку NightLantern")]
            public bool NightLantern;

            [JsonProperty(PropertyName = en ? "Ignore List For Turn Lights On" : "Список исключений для включения света", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> IgnoredLights = new() { "laserlight", "weaponrack", "lightswitch", "soundlight", "xmas" };

            [JsonProperty(PropertyName = en ? "Traps And Turrets Ignore Users Using NOCLIP" : "Ловушки и Турели Игнорируют Пользователей в Режиме NOCLIP")]
            public bool IgnoreFlying;

            [JsonProperty(PropertyName = en ? "Use Random Codes On Code Locks" : "Использовать Случайные Коды на Кодовых Замках")]
            public bool RandomCodes = true;

            [JsonProperty(PropertyName = en ? "Wait To Start Despawn Timer When Base Takes Damage From Player" : "Ожидание Начала Таймера Исчезновения После Урона Базе от Игрока")]
            public bool Engaged;

            [JsonProperty(PropertyName = en ? "Maximum Water Depth For All Npcs" : "Максимальная Глубина Воды для Всех НПС")]
            public float WaterDepth = 3f;
        }

        public class MapMarkerSettings
        {
            [JsonProperty(PropertyName = en ? "Marker Name" : "Название Маркера")]
            public string MarkerName = "Raidable Base Event";

            [JsonProperty(PropertyName = en ? "Radius" : "Радиус")]
            public float Radius = 0.25f;

            [JsonProperty(PropertyName = en ? "Radius (Map Size 3600 Or Less)" : "Радиус (Размер Карты 3600 или Меньше)")]
            public float SubRadius = 0.5f;

            [JsonProperty(PropertyName = en ? "Use Vending Map Marker" : "Использовать Маркер Торгового Автомата на Карте")]
            public bool UseVendingMarker = true;

            [JsonProperty(PropertyName = en ? "Show Owners Name on Map Marker" : "Показывать Имя Владельца на Маркере Карты")]
            public bool ShowOwnersName = true;

            [JsonProperty(PropertyName = en ? "Show If Purchased On Map Marker" : "Показывать Покупная ли База на Маркере Карты")]
            public bool ShowPurchased;

            [JsonProperty(PropertyName = en ? "Use Explosion Map Marker" : "Использовать Маркер Взрыва на Карте")]
            public bool UseExplosionMarker;

            [JsonProperty(PropertyName = en ? "Create Markers For Buyable Events" : "Создавать Маркеры для Покупаемых Событий")]
            public bool Buyables = true;

            [JsonProperty(PropertyName = en ? "Create Markers For Maintained Events" : "Создавать Маркеры для Поддерживаемых Событий")]
            public bool Maintained = true;

            [JsonProperty(PropertyName = en ? "Create Markers For Scheduled Events" : "Создавать Маркеры для Запланированных Событий")]
            public bool Scheduled = true;

            [JsonProperty(PropertyName = en ? "Create Markers For Manual Events" : "Создавать Маркеры для Ручных Событий")]
            public bool Manual = true;
        }

        public class ExperimentalSettings
        {
            [JsonProperty(PropertyName = en ? "Apply Custom Auto Height To" : "Применить Пользовательскую Автоматическую Высоту к", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> AutoHeight = new();

            [JsonProperty(PropertyName = en ? "Bunker Bases Or Profiles" : "Бункерные Базы или Профили", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Bunker = new();

            [JsonProperty(PropertyName = en ? "Multi Foundation Bases Or Profiles" : "Базы или Профили с Многоуровневым Фундаментом", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> MultiFoundation = new();

            public enum Type { AutoHeight, Bunker, MultiFoundation };

            public bool Contains(Type type, RandomBase rb) => type switch
            {
                Type.AutoHeight => Contains(AutoHeight, rb),
                Type.Bunker => Contains(Bunker, rb),
                _ => Contains(MultiFoundation, rb),
            };

            public bool Contains(List<string> m, RandomBase rb) => m.Contains("*") || m.Contains(rb.BaseName) || m.Contains(rb.Profile.ProfileName);
        }

        public class PluginSettings
        {
            [JsonProperty(PropertyName = en ? "Experimental [* = everything]" : "Экспериментальные Настройки [* = все]")]
            public ExperimentalSettings Experimental = new();

            [JsonProperty(PropertyName = en ? "Raid Management" : "Управление Рейдами")]
            public ManagementSettings Management = new();

            [JsonProperty(PropertyName = en ? "Map Markers" : "Маркеры на Карте")]
            public MapMarkerSettings Markers = new();

            [JsonProperty(PropertyName = en ? "Buyable Events" : "Покупаемые События")]
            public BuyableSettings Buyable = new();

            [JsonProperty(PropertyName = en ? "Maintained Events" : "Поддерживаемых Событий")]
            public MaintainedSettings Maintained = new();

            [JsonProperty(PropertyName = en ? "Manual Events" : "Ручные События")]
            public ManualSettings Manual = new();

            [JsonProperty(PropertyName = en ? "Scheduled Events" : "Запланированные События")]
            public ScheduledSettings Schedule = new();

            [JsonProperty(PropertyName = en ? "Allowed Zone Manager Zones" : "Разрешенные Зоны Управления Зонами", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Inclusions = new() { "pvp", "99999999" };

            [JsonProperty(PropertyName = en ? "Buyable Event Costs" : "Стоимость Покупаемых Событий")]
            public RaidableBaseCostOptions Include = new();

            [JsonProperty(PropertyName = en ? "Economics Buy Raid Costs (0 = disabled)" : "Стоимость Покупки Рейда в Экономике (0 = отключено)")]
            public EconomicsOptions Economics = new();

            [JsonProperty(PropertyName = en ? "ServerRewards Buy Raid Costs (0 = disabled)" : "Стоимость Покупки Рейда в ServerRewards (0 = отключено)")]
            public DifficultyModeOptions ServerRewards = new();

            [JsonProperty(PropertyName = en ? "Custom Buy Raid Cost" : "Пользовательская Стоимость Покупки Рейда", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, List<CustomCostOptions>> Custom = DefaultCustomCosts();

            [JsonProperty(PropertyName = en ? "ShoppyStock Buy Raid Cost" : "Стоимость Покупки Рейда в ShoppyStock", NullValueHandling = NullValueHandling.Ignore)]
            public CustomCostShoppyStock ShoppyStock = null;

            [JsonProperty(PropertyName = en ? "Use Grid Locations In Allowed Zone Manager Zones Only" : "Использовать Сеточные Расположения Только в Разрешенных Зонах Управления Зонами")]
            public bool UseZoneManagerOnly;

            [JsonProperty(PropertyName = en ? "Extended Distance To Spawn Away From Zone Manager Zones" : "Расширенное Расстояние для Спауна Вне Зон Управления Зонами")]
            public float ZoneDistance = 25f;

            [JsonProperty(PropertyName = en ? "Blacklisted Commands (PVE)" : "Черный Список Команд (PVE)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> BlacklistedPVECommands = new();

            [JsonProperty(PropertyName = en ? "Blacklisted Commands (PVP)" : "Черный Список Команд (PVP)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> BlacklistedPVPCommands = new();

            [JsonProperty(PropertyName = en ? "Automatically Teleport Admins To Their Map Marker Positions" : "Автоматически Телепортировать Администраторов к Их Маркерам на Карте")]
            public bool TeleportMarker = true;

            [JsonProperty(PropertyName = en ? "Automatically Destroy Markers That Admins Teleport To" : "Автоматически Уничтожать Маркеры, к Которым Телепортируются Администраторы")]
            public bool DestroyMarker;

            [JsonProperty(PropertyName = en ? "Block Archery Plugin At PVE Events" : "Блокировать Archery Plugin на PVE-мероприятиях")]
            public bool NoArcheryPVE;

            [JsonProperty(PropertyName = en ? "Block Archery Plugin At PVP Events" : "Блокировать Archery Plugin на PVP-мероприятиях")]
            public bool NoArcheryPVP;

            [JsonProperty(PropertyName = en ? "Block Wizardry Plugin At PVE Events" : "Блокировать Wizardry Plugin на PVE-мероприятиях")]
            public bool NoWizardryPVE;

            [JsonProperty(PropertyName = en ? "Block Wizardry Plugin At PVP Events" : "Блокировать Wizardry Plugin на PVP-мероприятиях")]
            public bool NoWizardryPVP;

            [JsonProperty(PropertyName = en ? "Chat Steam64ID" : "Steam64ID Чата")]
            public ulong ChatID = 76561199564392767;

            [JsonProperty(PropertyName = en ? "Expansion Mode (Dangerous Treasures)" : "Режим Расширения (Dangerous Treasures)")]
            public bool ExpansionMode;

            [JsonProperty(PropertyName = en ? "Remove Admins From Raiders List" : "Удалить Администраторов из Списка Рейдеров")]
            public bool RemoveAdminRaiders;

            [JsonProperty(PropertyName = en ? "Show Direction To Coordinates" : "Показать Направление к Координатам")]
            public bool ShowDir;

            [JsonProperty(PropertyName = en ? "Show X Z Coordinates" : "Показать Координаты X Z")]
            public bool ShowXZ;

            [JsonProperty(PropertyName = en ? "Buy Raid Command" : "Команда Покупки Рейда")]
            public string BuyCommand = "buyraid";

            [JsonProperty(PropertyName = en ? "Event Command" : "Команда События")]
            public string EventCommand = "rbe";

            [JsonProperty(PropertyName = en ? "Hunter Command" : "Команда Охотника")]
            public string HunterCommand = "rb";

            [JsonProperty(PropertyName = en ? "Server Console Command" : "Команда Консоли Сервера")]
            public string ConsoleCommand = "rbevent";
        }

        public class EventMessageRewardSettings
        {
            [JsonProperty(PropertyName = en ? "Flying" : "Летающий")]
            public bool Flying;

            [JsonProperty(PropertyName = en ? "Vanished" : "Vanish")]
            public bool Vanished;

            [JsonProperty(PropertyName = en ? "Inactive" : "Неактивный")]
            public bool Inactive = true;

            [JsonProperty(PropertyName = en ? "Not An Ally" : "Не Союзник")]
            public bool NotAlly = true;

            [JsonProperty(PropertyName = en ? "Not The Owner" : "Не Владелец")]
            public bool NotOwner = true;

            [JsonProperty(PropertyName = en ? "Not A Participant" : "Не Участник")]
            public bool NotParticipant = true;

            [JsonProperty(PropertyName = en ? "Remove Admins From Raiders List" : "Удалить Администраторов из Списка Рейдеров")]
            public bool RemoveAdmin;
        }

        public class EventMessageSettings
        {
            [JsonProperty(PropertyName = en ? "Ineligible For Rewards" : "Не Имеет Права на Награды")]
            public EventMessageRewardSettings Rewards = new();

            [JsonProperty(PropertyName = en ? "Announce Raid Unlocked" : "Объявить о снятии блокировки с Рейда")]
            public bool AnnounceRaidUnlock;

            [JsonProperty(PropertyName = en ? "Announce Buy Base Messages" : "Объявить Сообщения о Покупке Базы")]
            public bool AnnounceBuy;

            [JsonProperty(PropertyName = en ? "Announce Thief Message" : "Объявить Сообщение когда забраны все предметы")]
            public bool AnnounceThief = true;

            [JsonProperty(PropertyName = en ? "Announce PVE/PVP Enter/Exit Messages" : "Объявить Сообщения о Входе/Выходе PVE/PVP")]
            public bool AnnounceEnterExit = true;

            [JsonProperty(PropertyName = en ? "Show Destroy Warning" : "Показать Предупреждение об Уничтожении")]
            public bool ShowWarning = true;

            [JsonProperty(PropertyName = en ? "Show Opened Message For PVE Bases" : "Показать Сообщение об Открытии для PVE Баз")]
            public bool OpenedPVE = true;

            [JsonProperty(PropertyName = en ? "Show Opened Message For PVP Bases" : "Показать Сообщение об Открытии для PVP Баз")]
            public bool OpenedPVP = true;

            [JsonProperty(PropertyName = en ? "Show Opened Message For Paid Bases" : "Показать Сообщение об Открытии для Оплаченных Баз")]
            public bool OpenedAndPaid = true;

            [JsonProperty(PropertyName = en ? "Show Message For Block Damage Outside Of The Dome To Players Inside" : "Показать Сообщение о Блокировке Урона Снаружи Купола Игрокам Внутри")]
            public bool NoDamageFromOutsideToPlayersInside;

            [JsonProperty(PropertyName = en ? "Show Message When Purchase Becomes Available" : "Показать Сообщение, Когда Покупка Станет Доступной")]
            public bool PurchaseAvailable = true;

            [JsonProperty(PropertyName = en ? "Show Prefix" : "Показать Префикс")]
            public bool Prefix = true;

            [JsonProperty(PropertyName = en ? "Notify Plugin - Type (-1 = disabled)" : "Notify Plugin - Тип (-1 = отключено)")]
            public int NotifyType = -1;

            [JsonProperty(PropertyName = en ? "Notification Interval" : "Интервал Уведомлений")]
            public float Interval = 1f;

            [JsonProperty(PropertyName = en ? "Send Messages To Player" : "Отправлять Сообщения Игроку")]
            public bool Message = true;

            [JsonProperty(PropertyName = en ? "Save Thieves To Log File" : "Сохранить Воров в Лог-файл")]
            public bool LogThieves;
        }

        public class GUIAnnouncementSettings
        {
            [JsonProperty(PropertyName = en ? "Enabled" : "Включено")]
            public bool Enabled;

            [JsonProperty(PropertyName = en ? "Banner Tint Color" : "Цвет Тонирования Баннера")]
            public string TintColor = "Grey";

            [JsonProperty(PropertyName = en ? "Maximum Distance" : "Максимальное Расстояние")]
            public float Distance = 300f;

            [JsonProperty(PropertyName = en ? "Text Color" : "Цвет текста")]
            public string TextColor = "White";
        }

        public class NpcSettingsInsideBaseSleepers
        {
            [JsonProperty(PropertyName = en ? "Enabled" : "Включено")]
            public bool Enabled;

            [JsonProperty(PropertyName = en ? "Unwakeable" : "Не Просыпающийся")]
            public bool Unwakeable = true;

            [JsonProperty(PropertyName = en ? "Spawn Kit In Corpses Inventory" : "Создавать Комплект в Инвентаре Трупов")]
            public bool CopyKit;

            [JsonProperty(PropertyName = en ? "Spawn Loadout In Corpses Inventory" : "Создавать Снаряжение в Инвентаре Трупов")]
            public bool CopyLoadout;
        }

        public class NpcSettingsInsideBase
        {
            [JsonProperty(PropertyName = en ? "Sleepers" : "Спящие")]
            public NpcSettingsInsideBaseSleepers Sleepers = new();

            [JsonProperty(PropertyName = en ? "Spawn On Floors" : "Создавать на полу")]
            public bool SpawnOnFloors;

            [JsonProperty(PropertyName = en ? "Spawn On Beds" : "Создавать на Кроватях")]
            public bool SpawnOnBeds;

            [JsonProperty(PropertyName = en ? "Spawn On Rugs" : "Создавать на Коврах")]
            public bool SpawnOnRugs;

            [JsonProperty(PropertyName = en ? "Spawn On Rugs With Skin Only" : "Создавать на Коврах Только с Скином")]
            public ulong SpawnOnRugsSkin = 1;

            [JsonProperty(PropertyName = en ? "Bed Health Multiplier" : "Множитель Здоровья Кровати")]
            public float BedHealthMultiplier = 1f;

            [JsonProperty(PropertyName = en ? "Rug Health Multiplier" : "Множитель Здоровья Ковра")]
            public float RugHealthMultiplier = 1f;

            [JsonProperty(PropertyName = en ? "Spawn Murderers Outside" : "Создавать Убийц Снаружи")]
            public bool SpawnMurderersOutside = true;

            [JsonProperty(PropertyName = en ? "Spawn Scientists Outside" : "Создавать Ученых Снаружи")]
            public bool SpawnScientistsOutside = true;

            [JsonProperty(PropertyName = en ? "Minimum Inside (-1 = ignore)" : "Минимум Внутри (-1 = игнорировать)")]
            public int Min = -1;

            [JsonProperty(PropertyName = en ? "Maximum Inside (-1 = ignore)" : "Максимум Внутри (-1 = игнорировать)")]
            public int Max = -1;
        }

        public class NpcKitSettings
        {
            [JsonProperty(PropertyName = en ? "Helm" : "Шлем", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Helm = new();

            [JsonProperty(PropertyName = en ? "Torso" : "Торс", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Torso = new();

            [JsonProperty(PropertyName = en ? "Pants" : "Штаны", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Pants = new();

            [JsonProperty(PropertyName = en ? "Gloves" : "Перчатки", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Gloves = new();

            [JsonProperty(PropertyName = en ? "Boots" : "Ботинки", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Boots = new();

            [JsonProperty(PropertyName = en ? "Shirt" : "Рубашка", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Shirt = new();

            [JsonProperty(PropertyName = en ? "Kilts" : "Килты", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Kilts = new();

            [JsonProperty(PropertyName = en ? "Weapon" : "Оружие", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Weapon = new();
        }

        public class ScientistLootSettings
        {
            [JsonProperty(PropertyName = en ? "Prefab ID List" : "Список префабов", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> IDs = new() { "cargo", "turret_any", "ch47_gunner", "excavator", "full_any", "heavy", "junkpile_pistol", "oilrig", "patrol", "peacekeeper", "roam", "roamtethered" };

            [JsonProperty(PropertyName = en ? "Enabled" : "Включено", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public bool Enabled;

            [JsonProperty(PropertyName = en ? "Disable All Prefab Loot Spawns" : "Отключить все выпадения добычи из префабов", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public bool None;

            public uint GetRandom() => IDs.GetRandom() switch
            {
                "cargo" => 3623670799u,
                "turret_any" => 1639447304u,
                "ch47_gunner" => 1017671955u,
                "excavator" => 4293908444u,
                "full_any" => 1539172658u,
                "heavy" => 1536035819u,
                "junkpile_pistol" => 2066159302u,
                "cargo_turret" => 881071619u,
                "oilrig" => 548379897u,
                "patrol" => 4272904018u,
                "peacekeeper" => 2390854225u,
                "roam" => 4199494415u,
                "roamtethered" => 529928930u,
                _ => 1536035819u
            };
        }

        public class NpcMultiplierSettings
        {
            [JsonProperty(PropertyName = en ? "Explosive Damage Multiplier" : "Множитель урона от взрывов")]
            public float ExplosiveDamageMultiplier = 1f;

            [JsonProperty(PropertyName = en ? "Gun Damage Multiplier" : "Множитель урона от огнестрельного оружия")]
            public float ProjectileDamageMultiplier = 1f;

            [JsonProperty(PropertyName = en ? "Melee Damage Multiplier" : "Множитель урона от ближнего боя")]
            public float MeleeDamageMultiplier = 1f;
        }

        public class NpcSettingsAccuracyDifficulty
        {
            [JsonProperty(PropertyName = "AK47")]
            public double AK47;

            [JsonProperty(PropertyName = "AK47 ICE")]
            public double AK47ICE;

            [JsonProperty(PropertyName = "Bolt Rifle")]
            public double BOLT_RIFLE;

            [JsonProperty(PropertyName = "Compound Bow")]
            public double COMPOUND_BOW;

            [JsonProperty(PropertyName = "Crossbow")]
            public double CROSSBOW;

            [JsonProperty(PropertyName = "Double Barrel Shotgun")]
            public double DOUBLE_SHOTGUN;

            [JsonProperty(PropertyName = "Eoka")]
            public double EOKA;

            [JsonProperty(PropertyName = "Glock")]
            public double GLOCK;

            [JsonProperty(PropertyName = "HMLMG")]
            public double HMLMG;

            [JsonProperty(PropertyName = "L96")]
            public double L96;

            [JsonProperty(PropertyName = "LR300")]
            public double LR300;

            [JsonProperty(PropertyName = "M249")]
            public double M249;

            [JsonProperty(PropertyName = "M39")]
            public double M39;

            [JsonProperty(PropertyName = "M92")]
            public double M92;

            [JsonProperty(PropertyName = "MP5")]
            public double MP5;

            [JsonProperty(PropertyName = "Nailgun")]
            public double NAILGUN;

            [JsonProperty(PropertyName = "Pump Shotgun")]
            public double PUMP_SHOTGUN;

            [JsonProperty(PropertyName = "Python")]
            public double PYTHON;

            [JsonProperty(PropertyName = "Revolver")]
            public double REVOLVER;

            [JsonProperty(PropertyName = "Semi Auto Pistol")]
            public double SEMI_AUTO_PISTOL;

            [JsonProperty(PropertyName = "Semi Auto Rifle")]
            public double SEMI_AUTO_RIFLE;

            [JsonProperty(PropertyName = "Spas12")]
            public double SPAS12;

            [JsonProperty(PropertyName = "Speargun")]
            public double SPEARGUN;

            [JsonProperty(PropertyName = "SMG")]
            public double SMG;

            [JsonProperty(PropertyName = "Snowball Gun")]
            public double SNOWBALL_GUN;

            [JsonProperty(PropertyName = "Thompson")]
            public double THOMPSON;

            [JsonProperty(PropertyName = "Waterpipe Shotgun")]
            public double WATERPIPE_SHOTGUN;

            public NpcSettingsAccuracyDifficulty(double accuracy)
            {
                AK47 = AK47ICE = BOLT_RIFLE = COMPOUND_BOW = CROSSBOW = DOUBLE_SHOTGUN = EOKA = GLOCK = HMLMG = L96 = LR300 = M249 = M39 = M92 = MP5 = NAILGUN = PUMP_SHOTGUN = PYTHON = REVOLVER = SEMI_AUTO_PISTOL = SEMI_AUTO_RIFLE = SPAS12 = SPEARGUN = SMG = SNOWBALL_GUN = THOMPSON = WATERPIPE_SHOTGUN = accuracy;
            }

            public double Get(HumanoidBrain brain) => brain.AttackEntity?.ShortPrefabName switch
            {
                "ak47u.entity" => AK47,
                "ak47u_ice.entity" => AK47ICE,
                "bolt_rifle.entity" => BOLT_RIFLE,
                "compound_bow.entity" => COMPOUND_BOW,
                "crossbow.entity" or "bow_hunting.entity" => CROSSBOW,
                "double_shotgun.entity" => DOUBLE_SHOTGUN,
                "glock.entity" => GLOCK,
                "hmlmg.entity" => HMLMG,
                "l96.entity" => L96,
                "lr300.entity" => LR300,
                "m249.entity" => M249,
                "m39.entity" => M39,
                "m92.entity" => M92,
                "mp5.entity" => MP5,
                "nailgun.entity" => NAILGUN,
                "pistol_eoka.entity" => EOKA,
                "pistol_revolver.entity" => REVOLVER,
                "pistol_semiauto.entity" => SEMI_AUTO_PISTOL,
                "python.entity" => PYTHON,
                "semi_auto_rifle.entity" => SEMI_AUTO_RIFLE,
                "shotgun_pump.entity" => PUMP_SHOTGUN,
                "shotgun_waterpipe.entity" => WATERPIPE_SHOTGUN,
                "spas12.entity" => SPAS12,
                "speargun.entity" => SPEARGUN,
                "smg.entity" => SMG,
                "snowballgun.entity" => SNOWBALL_GUN,
                "thompson.entity" or _ => THOMPSON,
            };
        }

        public class NpcSettings
        {
            public NpcSettings() { }

            public NpcSettings(double accuracy)
            {
                Accuracy = new(accuracy);
            }

            public void SetAccuracy(RaidableMode mode)
            {
                Accuracy ??= new(mode == RaidableMode.Easy || mode == RaidableMode.Medium ? 15.0 : mode == RaidableMode.Hard ? 20.0 : mode == RaidableMode.Expert ? 25.0 : 30.0);
            }

            [JsonProperty(PropertyName = en ? "Weapon Accuracy (0 - 100)" : "Точность оружия (0 - 100)")]
            public NpcSettingsAccuracyDifficulty Accuracy;

            [JsonProperty(PropertyName = en ? "Damage Multipliers" : "Множители урона")]
            public NpcMultiplierSettings Multipliers = new();

            [JsonProperty(PropertyName = en ? "Spawn Inside Bases" : "Заселение внутри базы")]
            public NpcSettingsInsideBase Inside = new();

            [JsonProperty(PropertyName = en ? "Murderer (Items)" : "Убийца (Предметы)", NullValueHandling = NullValueHandling.Ignore)]
            public NpcKitSettings _MurdererItems = null;

            [JsonProperty(PropertyName = en ? "Murderer Loadout" : "Набор убийцы")]
            public NpcKitSettings MurdererLoadout = new()
            {
                Helm = { "metal.facemask" },
                Torso = { "metal.plate.torso" },
                Pants = { "pants" },
                Gloves = { "tactical.gloves" },
                Boots = { "boots.frog" },
                Shirt = { "tshirt" },
                Weapon = { "machete" }
            };

            [JsonProperty(PropertyName = "Scientist (Items)", NullValueHandling = NullValueHandling.Ignore)]
            public NpcKitSettings _ScientistItems = null;

            [JsonProperty(PropertyName = en ? "Scientist Loadout" : "Снаряжение ученых")]
            public NpcKitSettings ScientistLoadout = new()
            {
                Torso = { "hazmatsuit_scientist_peacekeeper" },
                Weapon = { "rifle.ak" }
            };

            [JsonProperty(PropertyName = en ? "Murderer Kits" : "Комплекты Убийц", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> MurdererKits = new() { "murderer_kit_1", "murderer_kit_2" };

            [JsonProperty(PropertyName = en ? "Scientist Kits" : "Комплекты Ученых", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> ScientistKits = new() { "scientist_kit_1", "scientist_kit_2" };

            [JsonProperty(PropertyName = en ? "Murderer Items Dropped On Death" : "Предметы Убийц, Выпавшие При Смерти", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<LootItem> MurdererDrops = new() { new("ammo.pistol", 1, 30) };

            [JsonProperty(PropertyName = en ? "Scientist Items Dropped On Death" : "Предметы Ученых, Выпавшие При Смерти", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<LootItem> ScientistDrops = new() { new("ammo.rifle", 1, 30) };

            [JsonProperty(PropertyName = en ? "Spawn Alternate Default Scientist Loot" : "Спавн Альтернативных Предметов Ученых", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public ScientistLootSettings AlternateScientistLoot = new();

            [JsonProperty(PropertyName = en ? "Random Names" : "Случайные Имена", NullValueHandling = NullValueHandling.Ignore)]
            public List<string> _RandomNames = null;

            [JsonProperty(PropertyName = en ? "Random Murderer Names" : "Случайные Murderer Имена", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> RandomMurdererNames = new();

            [JsonProperty(PropertyName = en ? "Random Scientist Names" : "Случайные Scientist Имена", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> RandomScientistNames = new();

            [JsonProperty(PropertyName = en ? "Enabled" : "Включено")]
            public bool Enabled = true;

            [JsonProperty(PropertyName = en ? "Amount That Can Throw Weapons" : "Количество, Которое Может Метать Оружие")]
            public int Thrown = 2;

            [JsonProperty(PropertyName = en ? "Amount Of Murderers To Spawn" : "Количество Спавна Убийц")]
            public int SpawnAmountMurderers = -9;

            [JsonProperty(PropertyName = en ? "Minimum Amount Of Murderers To Spawn" : "Минимальное Количество Спавна Убийц")]
            public int SpawnMinAmountMurderers = -9;

            [JsonProperty(PropertyName = en ? "Spawn Random Amount Of Murderers" : "Случайное Количество Спавна Убийц")]
            public bool SpawnRandomAmountMurderers;

            [JsonProperty(PropertyName = en ? "Amount Of Scientists To Spawn" : "Количество Спавна Ученых")]
            public int SpawnAmountScientists = -9;

            [JsonProperty(PropertyName = en ? "Minimum Amount Of Scientists To Spawn" : "Минимальное Количество Спавна Ученых")]
            public int SpawnMinAmountScientists = -9;

            [JsonProperty(PropertyName = en ? "Spawn Random Amount Of Scientists" : "Случайное Количество Спавна Ученых")]
            public bool SpawnRandomAmountScientists;

            [JsonProperty(PropertyName = en ? "Allow Npcs To Roofcamp" : "Разрешить НПС стрелять с крыши")]
            public bool Roofcampers;

            [JsonProperty(PropertyName = en ? "Allow Npcs To Counter Raid" : "Разрешить НПС контрнаступление")]
            public bool CounterRaid = true;

            [JsonProperty(PropertyName = en ? "Allow Npcs To Leave Dome When Attacking" : "Разрешить НПС покидать купол при атаке")]
            public bool CanLeave = true;

            [JsonProperty(PropertyName = en ? "Allow Npcs To Shoot Players Outside Of The Dome" : "Разрешить НПС стрелять в игроков снаружи купола")]
            public bool CanShoot = true;

            [JsonProperty(PropertyName = en ? "Allow Npcs To Play Catch When Equipped With Explosives" : "Разрешить НПС играть в подкидывание, когда у них есть взрывчатка")]
            public bool PlayCatch;

            [JsonProperty(PropertyName = en ? "Aggression Range" : "радиус агрессии")]
            public float AggressionRange = 70f;

            [JsonProperty(PropertyName = en ? "Decrease Damage Linearly From Npcs With A Maximum Effective Range Of" : "Постепенное уменьшение урона от НПС с максимальной эффективной дальностью")]
            public float NpcMaxEffectiveRange;

            [JsonProperty(PropertyName = en ? "Decrease Damage Linearly From Players With A Maximum Effective Range Of" : "Постепенное уменьшение урона от игроков с максимальной эффективной дальностью")]
            public float PlayerMaxEffectiveRange;

            [JsonProperty(PropertyName = en ? "Block Damage Outside To Npcs When Not Allowed To Leave Dome" : "Блокировать урон снаружи для НПС, когда им не разрешено покидать купол")]
            public bool BlockOutsideDamageOnLeave = true;

            [JsonProperty(PropertyName = en ? "Block Damage Outside Of The Dome To Npcs Inside" : "Блокировать урон снаружи купола по НПС внутри")]
            public bool BlockOutsideDamageToNpcsInside;

            [JsonProperty(PropertyName = en ? "Spawn Kit In Corpses Inventory" : "Создавать Комплект в Инвентаре Трупов")]
            public bool CopyKit;

            [JsonProperty(PropertyName = en ? "Spawn Loadout In Corpses Inventory" : "Создавать Снаряжение в Инвентаре Трупов")]
            public bool CopyLoadout;

            [JsonProperty(PropertyName = en ? "Despawn Inventory On Death" : "Убирать инвентарь при смерти", NullValueHandling = NullValueHandling.Ignore)]
            public bool? _DespawnInventory { get; set; } = null;

            [JsonProperty(PropertyName = en ? "Health For Murderers" : "Здоровье для убийц")]
            public float MurdererHealth = 150f;

            [JsonProperty(PropertyName = en ? "Health For Scientists" : "Здоровье для ученых")]
            public float ScientistHealth = 150f;

            [JsonProperty(PropertyName = en ? "Kill Underwater Npcs" : "Убивать подводных НПС")]
            public bool KillUnderwater = true;

            [JsonProperty(PropertyName = en ? "Kits Are Unique When Applicable" : "Комплекты уникальны, когда это применимо")]
            public bool UniqueKits;

            [JsonProperty(PropertyName = en ? "Player Traps And Turrets Ignore Npcs" : "Ловушки и турели игроков игнорируют НПС")]
            public bool IgnorePlayerTrapsTurrets;

            [JsonProperty(PropertyName = en ? "Event Traps And Turrets Ignore Npcs" : "Ловушки и турели событий игнорируют НПС")]
            public bool IgnoreTrapsTurrets = true;

            [JsonProperty(PropertyName = en ? "Use Dangerous Treasures NPCs" : "Использовать НПС опасных сокровищ (Dangerous Treasures)")]
            public bool UseExpansionNpcs;
        }

        public class ProfileDespawnOptions : DespawnOptionsBase
        {
            [JsonProperty(PropertyName = en ? "Override Global Config With These Options For This Profile" : "Переопределить глобальные настройки этими параметрами для этого профиля")]
            public bool OverrideConfig = false;
        }

        public class DespawnOptionsBase
        {
            [JsonProperty(PropertyName = en ? "Minutes Until Despawn After Looting (min: 1)" : "Минуты до исчезновения после разграбления (минимум: 1)")]
            public int DespawnMinutes = 15;

            [JsonProperty(PropertyName = en ? "Minutes Until Despawn After Looting Resets When Damaged" : "Минуты до исчезновения после разграбления сбрасываются при повреждении")]
            public bool DespawnMinutesReset;

            [JsonProperty(PropertyName = en ? "Minutes Until Despawn After Inactive (0 = disabled)" : "Минуты до исчезновения после бездействия (0 = отключено)")]
            public int DespawnMinutesInactive = 45;

            [JsonProperty(PropertyName = en ? "Minutes Until Despawn After Inactive Resets When Damaged" : "Минуты до исчезновения после бездействия сбрасываются при повреждении")]
            public bool DespawnMinutesInactiveReset = true;
        }

        public class PasteOption
        {
            [JsonProperty(PropertyName = en ? "Option" : "Опция")]
            public string Key;

            [JsonProperty(PropertyName = en ? "Value" : "Значение")]
            public string Value;
        }

        public class BuildingLevels
        {
            [JsonProperty(PropertyName = en ? "Level 2 - Final Death" : "Уровень 2 - Окончательная смерть")]
            public bool Level2;
        }

        public class DoorTypes
        {
            [JsonProperty(PropertyName = en ? "Wooden" : "Деревянные")]
            public bool Wooden;

            [JsonProperty(PropertyName = en ? "Metal" : "Металлические")]
            public bool Metal;

            [JsonProperty(PropertyName = en ? "HQM" : "МВК")]
            public bool HQM;

            [JsonProperty(PropertyName = en ? "Include Garage Doors" : "Включая гаражные двери")]
            public bool GarageDoor;

            public bool Any() => Wooden || Metal || HQM;
        }

        public class BuildingGradeLevels
        {
            [JsonProperty(PropertyName = en ? "Wooden" : "Деревянные")]
            public bool Wooden;

            [JsonProperty(PropertyName = en ? "Stone" : "Каменные")]
            public bool Stone;

            [JsonProperty(PropertyName = en ? "Metal" : "Металлические")]
            public bool Metal;

            [JsonProperty(PropertyName = en ? "HQM" : "МВК")]
            public bool HQM;

            public bool Any() => Wooden || Stone || Metal || HQM;
        }

        public class Mapping
        {
            [JsonProperty(PropertyName = en ? "Name" : "Имя", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public string Name = "Example";
            
            [JsonProperty(PropertyName = en ? "Enabled" : "Включено", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public bool Enabled;
            
            [JsonProperty(PropertyName = en ? "Skin" : "Скин", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public ulong Skin;
            
            [JsonProperty(PropertyName = en ? "Grade" : "Класс", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public int Grade;

            public Mapping() { }
            public Mapping(string name, bool enabled, ulong skin, int grade)
            {
                Name = name;
                Enabled = enabled;
                Skin = skin;
                Grade = grade;
            }
        }

        public class BuildingGradeLevelsSkins : BuildingGradeLevels
        {
            [JsonProperty(PropertyName = en ? "Exclusions" : "Исключения", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Exclusions = new() { "raideasy100", "raideasy101" };

            [JsonProperty(PropertyName = en ? "Additional Rust Shop Building Skins" : "Дополнительные скины Rust Shop для зданий", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<Mapping> AdditionalMappings = new()
            {
                new("Example", false, 0uL, 0)
            };

            [JsonProperty(PropertyName = en ? "Only Apply Skin When Material Is Enabled" : "Применять скин только при включенном материале")]
            public bool RequireMaterial;

            [JsonProperty(PropertyName = en ? "Use Adobe Skin" : "Использовать скин Adobe")]
            public bool Adobe;

            [JsonProperty(PropertyName = en ? "Use Shipping Container Skin" : "Используйте скин Shipping Container")]
            public bool Shipping;

            [JsonProperty(PropertyName = en ? "Use Brick Skin" : "Использовать скин кирпича")]
            public bool Brick;

            [JsonProperty(PropertyName = en ? "Use Frontier Skin" : "Использовать скин фронтира")]
            public bool Frontier;

            [JsonProperty(PropertyName = en ? "Use Gingerbread Skin" : "Использовать скин пряничного домика")]
            public bool Gingerbread;

            [JsonProperty(PropertyName = en ? "Use Brutalist Skin" : "Использовать скин брутализма")]
            public bool Brutalist;

            [JsonProperty(PropertyName = en ? "Use Random Colors" : "Использовать случайные цвета")]
            public bool RandomColour;

            [JsonProperty(PropertyName = en ? "Use Identical Colors" : "Использовать идентичные цвета")]
            public bool IdenticalColour;

            [JsonProperty(PropertyName = en ? "Use Random Skin For Whole Base" : "Использовать случайный скин для всей базы")]
            public bool RandomWhole;

            [JsonProperty(PropertyName = en ? "Use Random Skin On Every Block" : "Использовать случайный скин на каждом блоке")]
            public bool RandomEvery;

            [JsonProperty(PropertyName = en ? "Random Building Skin List" : "Список случайных скинов для здания", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> Skins = new() { 0, 2, 10220, 10221, 10223, 10225, 10232 };

            public ulong GetSkin(BuildingBlock block, BuildingGrade.Enum grade, ulong skinID)
            {
                List<ulong> skins = new();
                if (RandomEvery || RandomWhole)
                {
                    skins.AddRange(Skins.Where(skin => HasSkin(block, grade, skin)));
                    if (skins.Count > 0) return skins.GetRandom();
                }
                foreach (var map in GetMappings())
                {
                    if (map.Grade < 0 || map.Grade >= (int)BuildingGrade.Enum.Count)
                    {
                        continue;
                    }
                    bool material = map.Grade switch
                    {
                        (int)BuildingGrade.Enum.Wood => Wooden,
                        (int)BuildingGrade.Enum.Stone => Stone,
                        (int)BuildingGrade.Enum.Metal => Metal,
                        (int)BuildingGrade.Enum.TopTier => HQM,
                        _ => false
                    };
                    if (map.Enabled && (material || !RequireMaterial) && HasSkin(block, grade, map.Skin))
                    {
                        skins.Add(map.Skin);
                    }
                }
                return skins.Count > 0 ? skins.GetRandom() : skinID;
            }

            internal Construction construction = null;

            public List<Mapping> GetMappings()
            {
                List<Mapping> maps = new()
                {
                    new("Adobe", Adobe, 10220, (int)BuildingGrade.Enum.Stone),
                    new("Shipping Container", Shipping, 10221, (int)BuildingGrade.Enum.Metal),
                    new("Brick", Brick, 10223, (int)BuildingGrade.Enum.Stone),
                    new("Brutalist", Brutalist, 10225, (int)BuildingGrade.Enum.Stone),
                    new("Legacy Wood", Frontier, 10232, (int)BuildingGrade.Enum.Wood),
                    new("Gingerbread", Gingerbread, 2, (int)BuildingGrade.Enum.Wood),
                };
                construction ??= PrefabAttribute.server.Find<Construction>(870964632u);
                foreach (var map in AdditionalMappings.ToList())
                {
                    if (map.Skin == 0uL && !string.IsNullOrEmpty(map.Name))
                    {
                        foreach (ConstructionGrade grade in construction.grades)
                        {
                            if (grade.gradeBase.upgradeMenu.name.english.EndsWith(map.Name))
                            {
                                map.Skin = grade.gradeBase.skin;
                                break;
                            }
                        }
                    }
                    maps.Add(map);
                }
                return maps;
            }

            public bool HasSkin(BuildingBlock block, BuildingGrade.Enum grade, ulong skin)
            {
                ConstructionGrade constructionGrade = block.blockDefinition.GetGrade(grade, skin);
                if (constructionGrade == null || !constructionGrade.skinObject.isValid) return false;
                if (constructionGrade.gradeBase.type != grade || constructionGrade.gradeBase.skin != skin) return false;
                return GameManager.server.FindPrefab(constructionGrade.skinObject.resourcePath).GetComponent<ConstructionSkin>();
            }
        }

        public class BuildingOptionsAutoTurrets
        {
            [JsonProperty(PropertyName = en ? "Aim Cone" : "Угол прицеливания")]
            public float AimCone = 5f;

            [JsonProperty(PropertyName = en ? "Wait To Power On Until Event Starts" : "Ожидание включения до начала события")]
            public bool InitiateOnSpawn;

            [JsonProperty(PropertyName = en ? "Minimum Damage Modifier" : "Минимальный модификатор урона")]
            public float Min = 1f;

            [JsonProperty(PropertyName = en ? "Maximum Damage Modifier" : "Максимальный модификатор урона")]
            public float Max = 1f;

            [JsonProperty(PropertyName = en ? "Minimum Damage Modifier (NPC)" : "Минимальный модификатор урона (NPC)")]
            public float NpcMin = 1f;

            [JsonProperty(PropertyName = en ? "Maximum Damage Modifier (NPC)" : "Максимальный модификатор урона (NPC)")]
            public float NpcMax = 1f;

            [JsonProperty(PropertyName = en ? "Start Health" : "Начальное здоровье")]
            public float Health = 1000f;

            [JsonProperty(PropertyName = en ? "Sight Range" : "Дальность видимости")]
            public float SightRange = 30f;

            [JsonProperty(PropertyName = en ? "Double Sight Range When Shot" : "Двойная дальность видимости после выстрела")]
            public bool AutoAdjust;

            [JsonProperty(PropertyName = en ? "Set Hostile (False = Do Not Set Any Mode)" : "Установить враждебный режим (False = Не устанавливать никакой режим)")]
            public bool Hostile = true;

            [JsonProperty(PropertyName = en ? "Requires Power Source" : "Требуется источник питания")]
            public bool RequiresPower;

            [JsonProperty(PropertyName = en ? "Remove Equipped Weapon" : "Удалить экипированное оружие")]
            public bool RemoveWeapon;

            [JsonProperty(PropertyName = en ? "Random Weapons To Equip When Unequipped" : "Случайное оружие для экипировки при снятии", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Shortnames = new() { "rifle.ak" };
        }

        public class BuildingOptionsPermissions
        {
            [JsonProperty(PropertyName = en ? "Buyable Events" : "Покупаемые События")]
            public string Buyable = "";

            [JsonProperty(PropertyName = en ? "Maintained Events" : "Поддерживаемых Событий")]
            public string Maintained = "";

            [JsonProperty(PropertyName = en ? "Scheduled Events" : "Запланированные События")]
            public string Scheduled = "";

            public void Register(RaidableBases instance, Core.Libraries.Permission permission)
            {
                var buyable = Get(RaidableType.Purchased);

                if (!string.IsNullOrEmpty(buyable) && !permission.PermissionExists(buyable))
                {
                    permission.RegisterPermission(buyable, instance);
                }

                var maintained = Get(RaidableType.Maintained);

                if (!string.IsNullOrEmpty(maintained) && !permission.PermissionExists(maintained))
                {
                    permission.RegisterPermission(maintained, instance);
                }

                var scheduled = Get(RaidableType.Scheduled);

                if (!string.IsNullOrEmpty(scheduled) && !permission.PermissionExists(scheduled))
                {
                    permission.RegisterPermission(scheduled, instance);
                }
            }

            public string Get(RaidableType type)
            {
                string permission = type switch
                {
                    RaidableType.Purchased => Buyable,
                    RaidableType.Maintained => Maintained,
                    RaidableType.Scheduled => Scheduled,
                    _ => null
                };

                return !string.IsNullOrEmpty(permission) ? permission.Contains(".") ? permission : $"raidablebases.{permission}" : null;
            }

            public bool Has(BasePlayer player, RaidableType type)
            {
                if (!player)
                {
                    return true;
                }

                string permission = Get(type);

                if (string.IsNullOrEmpty(permission))
                {
                    return true;
                }

                return player.HasPermission(permission.Contains(".") ? permission : $"raidablebases.{permission}");
            }
        }

        public class BuildingOptionsProtectionRadius
        {
            [JsonProperty(PropertyName = en ? "Buyable Events" : "Покупаемые События")]
            public float Buyable = 50f;

            [JsonProperty(PropertyName = en ? "Maintained Events" : "Поддерживаемых Событий")]
            public float Maintained = 50f;

            [JsonProperty(PropertyName = en ? "Manual Events" : "Ручные События")]
            public float Manual = 50f;

            [JsonProperty(PropertyName = en ? "Scheduled Events" : "Запланированные События")]
            public float Scheduled = 50f;

            [JsonProperty(PropertyName = en ? "Obstruction Distance Check" : "Проверка на препятствия")]
            public float Obstruction = -1f;

            public void Set(float value)
            {
                Buyable = value;
                Maintained = value;
                Manual = value;
                Scheduled = value;
            }

            public float Get(RaidableType type) => type switch
            {
                RaidableType.Purchased => Buyable,
                RaidableType.Maintained => Maintained,
                RaidableType.Scheduled => Scheduled,
                RaidableType.Manual => Manual,
                _ => Max()
            };

            public float Max() => Mathf.Max(Buyable, Maintained, Manual, Scheduled);

            public float Min() => Mathf.Min(Buyable, Maintained, Manual, Scheduled);
        }

        public class BuildingOptionsBradleySettings
        {
            [JsonProperty(PropertyName = en ? "Spawn Bradley When Base Spawns" : "Спаун Брэдли при создании базы")]
            public bool SpawnImmediately;

            [JsonProperty(PropertyName = en ? "Spawn Bradley When Base Is Completed" : "Спаун Брэдли, когда база завершена")]
            public bool SpawnCompleted;

            [JsonProperty(PropertyName = en ? "Chance To Spawn (Min)" : "Шанс спауна (мин)")]
            public float Min = 0.05f;

            [JsonProperty(PropertyName = en ? "Chance To Spawn (Max)" : "Шанс спауна (макс)")]
            public float Max = 0.1f;

            [JsonProperty(PropertyName = en ? "Health" : "Здоровье")]
            public float Health = 1000f;

            [JsonProperty(PropertyName = en ? "Bullet Damage" : "Урон от пули")]
            public float BulletDamage = 15f;

            [JsonProperty(PropertyName = en ? "Crates" : "Ящики")]
            public int Crates = 3;

            [JsonProperty(PropertyName = en ? "Sight Range" : "Дальность видимости")]
            public float SightRange = 100f;

            [JsonProperty(PropertyName = en ? "Double Sight Range When Shot" : "Двойная дальность видимости после выстрела")]
            public bool Vision = true;

            [JsonProperty(PropertyName = en ? "Splash Radius" : "Радиус взрыва")]
            public float Splash = 15f;
        }

        public class BuildingWaterOptions
        {
            [JsonProperty(PropertyName = en ? "Allow Bases To Float Above Water" : "Разрешить базам плавать над водой")]
            public bool AllowSubmerged;

            [JsonProperty(PropertyName = en ? "Chance For Underwater Bases To Spawn (0-100) (BETA - WORK IN PROGRESS)" : "Шанс появления подводных баз (0-100) (БЕТА - В РАЗРАБОТКЕ)")]
            public float Seabed;

            [JsonProperty(PropertyName = en ? "Prevent Bases From Floating Above Water By Also Checking Surrounding Area" : "Предотвращать плавание баз над водой, также проверяя окружающую область")]
            public bool SubmergedAreaCheck;

            [JsonProperty(PropertyName = en ? "Maximum Water Depth Level Used For Float Above Water Option" : "Максимальный уровень глубины воды, используемый для опции плавания над водой")]
            public float WaterDepth = 1f;

            [JsonProperty(PropertyName = en ? "Minimum Water Depth Level Used For Seabed Option" : "Минимальный уровень глубины воды, используемый для опции морского дна")]
            public float MinimumSeabedWaterDepth = -20f;

            [JsonProperty(PropertyName = en ? "Maximum Water Depth Level Used For Seabed Option" : "Максимальный уровень глубины воды, используемый для опции морского дна")]
            public float MaximumSeabedWaterDepth = -35f;

            [JsonProperty(PropertyName = en ? "Torpedo Damage Multiplier (Min)" : "Множитель урона торпеды (мин)")]
            public float TorpedoMin = 5f;

            [JsonProperty(PropertyName = en ? "Torpedo Damage Multiplier (Max)" : "Множитель урона торпеды (макс)")]
            public float TorpedoMax = 10f;

            [JsonIgnore]
            public bool SpawnOnSeabed;

            [JsonIgnore]
            public CacheType CacheType => SpawnOnSeabed ? CacheType.Seabed : CacheType.Generic;

            [JsonIgnore]
            public bool Random => Seabed > 0f && UnityEngine.Random.Range(0f, 100f) <= Seabed;
        }

        public class BuildingOptionsDifficultySpawns
        {
            [JsonProperty(PropertyName = en ? "Spawns Database File (Optional)" : "Файл базы данных спавнов (опционально)")]
            public string SpawnsFile = "none";

            [JsonProperty(PropertyName = en ? "Prevent Building Until Base Spawns" : "Запретить строительство до появления базы")]
            public bool PreventBuilding;

            [JsonProperty(PropertyName = en ? "Ignore Safe Checks" : "Игнорировать проверки безопасности")]
            public bool Ignore;

            [JsonProperty(PropertyName = en ? "Ignore Safe Checks In X Radius Only" : "Игнорировать проверки безопасности только в радиусе X")]
            public float SafeRadius;

            [JsonProperty(PropertyName = en ? "Ignore Player Entities At Custom Spawn Locations" : "Игнорировать игровые объекты игроков в пользовательских точках спавна")]
            public bool Skip;

            [JsonProperty(PropertyName = en ? "Kill Sleeping Bags" : "Уничтожать спальные мешки")]
            public bool KillSleepingBags = true;

            //[JsonProperty(PropertyName = en ? "Map Prefabs For Spawn Points" : "Префабы карты для точек спавна", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            //public List<string> SpawnPointPrefabs = new();

            [JsonProperty(PropertyName = en ? "Map Prefabs For Buyable Teleport" : "Префабы карты для покупного телепорта", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> BuyableTeleportPrefabs = new();

            [JsonProperty(PropertyName = en ? "Time To Accept Buyable Teleport" : "Время для принятия покупного телепорта", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public float BuyableUiDuration = 60f;

            internal float BuyableTeleportRadius;

            internal List<Vector3> BuyableTeleportPositions = new();

            //internal List<Vector3> SpawnPointPositions = new();

            public bool HasTeleportPositionAt(Vector3 from)
            {
                return BuyableTeleportRadius > 0f && BuyableTeleportPositions.Exists(v => InRange(from, v, BuyableTeleportRadius));
            }

            public bool GetBuyableTeleportPosition(Vector3 from, out Vector3 to)
            {
                return (to = BuyableTeleportPositions.FirstOrDefault(v => InRange(from, v, BuyableTeleportRadius))) != default;
            }

            public bool ShouldAdd(BaseProfile profile, ProtoBuf.PrefabData prefab, string fullname, Vector3 v)
            {
                if (BuyableTeleportPrefabs.Count > 0 && BuyableTeleportPrefabs.Exists(fullname.Contains))
                {
                    BuyableTeleportPositions.Add(v);
                    return true;
                }
                //if (SpawnPointPrefabs.Exists(fullname.Contains) || SpawnPointPrefabs.Exists(prefab.category.Contains))
                //{
                //    SpawnPointPositions.Add(v);
                //    profile.Spawns ??= new(profile.Instance, new());
                //    (v.y < WaterSystem.OceanLevel - 15f ? profile.Spawns.Seabed : profile.Spawns.Spawns).Add(new(v));
                //    return true;
                //}
                return false;
            }
        }

        public class BuildingOptionsRadiation
        {
            [JsonProperty(PropertyName = en ? "Enabled" : "Включено")]
            public bool Enabled;

            [JsonProperty(PropertyName = en ? "Damage" : "Урон")]
            public float Damage = 1f;

            [JsonProperty(PropertyName = en ? "Rads" : "Радиация")]
            public float Rads = 2f;

            [JsonProperty(PropertyName = en ? "Protection Required" : "Требуется защита")]
            public float Protection = 6f;
        }

        public class BuildingOptionsEco
        {
            [JsonProperty(PropertyName = en ? "Allow Eco Raiding Only" : "Разрешить только эко-нападение")]
            public bool Enabled;

            [JsonProperty(PropertyName = en ? "Allow Flame Throwers" : "Разрешить огнеметы")]
            public bool FlameThrowers;

            [JsonProperty(PropertyName = en ? "Allow Bows" : "Разрешить луки")]
            public bool Bows = true;

            [JsonProperty(PropertyName = en ? "Allow Molotov Cocktails" : "Разрешить коктейли Молотова")]
            public bool Molotov = true;

            internal bool CanSpread(BaseEntity fireball) => fireball.ShortPrefabName switch
            {
                "flamethrower_fireball" when FlameThrowers => Enabled,
                "fireball_small_molotov" when Molotov => Enabled,
                "fireball_small_arrow" when Bows => Enabled,
                _ => false
            };
        }

        public class BuildingOptionsCommands
        {
            [JsonProperty(PropertyName = en ? "Commands" : "Команды", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Commands = new();

            [JsonProperty(PropertyName = en ? "Assign To Owner Of Raid Only" : "Начислять только владельцу рейда")]
            public bool Owner;

            [JsonProperty(PropertyName = en ? "Enabled" : "Включено")]
            public bool Enabled;

            public BuildingOptionsCommands()
            {
                Commands.Add("inventory.giveto {userid} apple 1");
                Commands.Add("o.usergroup add {userid} specialgroup");
            }
        }

        public class PlayerDamageMultiplier
        {
            [JsonProperty(PropertyName = en ? "Type" : "Тип")]
            public string Type;

            [JsonProperty(PropertyName = en ? "Min" : "Мин")]
            public float Min = 1f;

            [JsonProperty(PropertyName = en ? "Max" : "Макс")]
            public float Max = 1f;

            internal float amount => UnityEngine.Random.Range(Min, Max);

            internal DamageType[] _damageTypes;

            internal DamageType index => Array.Find(_damageTypes ??= (DamageType[])Enum.GetValues(typeof(DamageType)), type => type.ToString().Equals(Type, StringComparison.OrdinalIgnoreCase));

            public PlayerDamageMultiplier() { }

            public PlayerDamageMultiplier(string type, float min, float max)
            {
                (Type, Min, Max) = (type, min, max);
            }
        }

        public class BuildingOptions
        {
            public BuildingOptions() { }

            public BuildingOptions(RaidableMode mode, params string[] bases)
            {
                (Mode, PasteOptions, AdditionalBases) = (mode, DefaultPasteOptions(), bases.ToDictionary(value => value, _ => DefaultPasteOptions()));
            }

            [JsonProperty(PropertyName = en ? "Difficulty (0 = easy, 1 = medium, 2 = hard, 3 = expert, 4 = nightmare)" : "Сложность (0 = легкий, 1 = cредний, 2 = сложно, 3 = эксперт, 4 = кошмарный)")]
            public RaidableMode Mode = RaidableMode.Easy;

            [JsonProperty(PropertyName = en ? "Enable Profile For Buyable Bases Plugin" : "Включить профиль для плагина Buyable Bases")]
            public bool BuyableBase;

            [JsonProperty(PropertyName = en ? "Commands To Run With Assign Rank After X Completions" : "Команды для выполнения с присвоением ранга после X завершений")]
            public BuildingOptionsCommands EventRankedAwards = new();

            [JsonProperty(PropertyName = en ? "Commands To Run On Event Completion" : "Команды для выполнения при завершении события")]
            public BuildingOptionsCommands EventCompletion = new();

            [JsonProperty(PropertyName = en ? "Permission Required To Enter" : "Требуется разрешение для входа")]
            public BuildingOptionsPermissions Permission = new();

            [JsonProperty(PropertyName = en ? "Advanced Protection Radius" : "Расширенный радиус защиты")]
            public BuildingOptionsProtectionRadius ProtectionRadii = new();

            [JsonProperty(PropertyName = en ? "Advanced Setup Settings" : "Расширенные настройки установки")]
            public BuildingOptionsSetupSettings Setup = new();

            [JsonProperty(PropertyName = en ? "Despawn Options Override" : "Переопределение параметров исчезновения")]
            public ProfileDespawnOptions DespawnOptions = new();

            [JsonProperty(PropertyName = en ? "Elevators" : "Лифты")]
            public BuildingOptionsElevators Elevators = new();

            [JsonProperty(PropertyName = en ? "Entities Not Allowed To Be Damaged" : "Сущности, не подлежащие повреждению", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> BlockedEntityDamage = new();

            [JsonProperty(PropertyName = en ? "Entities Not Allowed To Be Picked Up" : "Сущности, не подлежащие поднятию", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> BlacklistedPickupItems = new() { "generator.small", "generator.static", "autoturret_deployed" };

            [JsonProperty(PropertyName = en ? "Additional Bases For This Difficulty" : "Дополнительные базы для данной сложности", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, List<PasteOption>> AdditionalBases = new();

            [JsonProperty(PropertyName = en ? "Paste Options" : "Параметры вставки", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<PasteOption> PasteOptions = new();

            [JsonProperty(PropertyName = en ? "Arena Walls" : "Стены арены")]
            public RaidableBaseWallOptions ArenaWalls = new();

            [JsonProperty(PropertyName = en ? "Eco Raiding" : "Эко-рейды")]
            public BuildingOptionsEco Eco = new();

            [JsonProperty(PropertyName = en ? "NPC Levels" : "Уровни NPC")]
            public BuildingLevels Levels = new();

            [JsonProperty(PropertyName = en ? "NPCs" : "NPC")]
            public NpcSettings NPC = new();

            [JsonProperty(PropertyName = en ? "Rewards" : "Награды")]
            public RewardSettings Rewards = new();

            [JsonProperty(PropertyName = en ? "Change Building Material Tier To" : "Изменить уровень материала здания на")]
            public BuildingGradeLevelsSkins Blocks = new();

            [JsonProperty(PropertyName = en ? "Change Door Type To" : "Изменить тип двери на")]
            public DoorTypes Doors = new();

            [JsonProperty(PropertyName = en ? "Player Damage To Base Multipliers" : "Множители урона игроков по базе", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<PlayerDamageMultiplier> PlayerDamageMultiplier = new()
            {
                new("Arrow", 1f, 1f),
                new("Blunt", 1f, 1f),
                new("Bullet", 1f, 1f),
                new("Heat", 1f, 1f),
                new("Explosion", 1f, 1f),
                new("Slash", 1f, 1f),
                new("Stab", 1f, 1f),
            };

            [JsonProperty(PropertyName = en ? "Auto Turrets" : "Автоматические турели")]
            public BuildingOptionsAutoTurrets AutoTurret = new();

            [JsonProperty(PropertyName = en ? "Player Building Restrictions" : "Ограничения на строительство игроков")]
            public BuildingGradeLevels BuildingRestrictions = new();

            [JsonProperty(PropertyName = en ? "Water Settings" : "Настройки воды")]
            public BuildingWaterOptions Water = new();

            [JsonProperty(PropertyName = en ? "Spawns Database" : "База данных спавнов")]
            public BuildingOptionsDifficultySpawns CustomSpawns = new();

            [JsonProperty(PropertyName = en ? "Radiation" : "Радиация")]
            public BuildingOptionsRadiation Radiation = new();

            [JsonProperty(PropertyName = en ? "Sam Site" : "Зенитная установка САМ")]
            public WeaponSettingsSamSite SamSite = new();

            [JsonProperty(PropertyName = en ? "Sphere Colors (0 None, 1 Blue, 2 Cyan, 3 Green, 4 Magenta, 5 Purple, 6 Red, 7 Yellow)" : "Цвета сфер (0 Нет, 1 Синий, 2 Голубой, 3 Зеленый, 4 Пурпурный, 5 Фиолетовый, 6 Красный, 7 Желтый)")]
            public SphereColorSettings SphereColor = new();

            [JsonProperty(PropertyName = en ? "Tesla Coil" : "Тесла-катушка")]
            public WeaponSettingsTeslaCoil TeslaCoil = new();

            [JsonProperty(PropertyName = en ? "Profile Enabled" : "Профиль включен")]
            public bool Enabled = true;

            [JsonProperty(PropertyName = en ? "Maximum Land Level" : "Максимальный уровень земли")]
            public float LandLevel = 2.5f;

            [JsonProperty(PropertyName = en ? "Allow Players To Use MLRS" : "Разрешить игрокам использовать МЛРС")]
            public bool MLRS = true;

            [JsonProperty(PropertyName = en ? "Allow Third-Party Npc Explosive Damage To Bases" : "Разрешить NPC сторонний взрывной урон по базам")]
            public bool RaidingNpcs;

            [JsonProperty(PropertyName = en ? "Add Code Lock To Unlocked Or KeyLocked Doors" : "Добавить кодовый замок к открытым или замкнутым дверям с ключом")]
            public bool CodeLockDoors = true;

            [JsonProperty(PropertyName = en ? "Add Key Lock To Unlocked Or CodeLocked Doors" : "Добавить ключевой замок к открытым или дверям с кодовым замком")]
            public bool KeyLockDoors;

            [JsonProperty(PropertyName = en ? "Add Code Lock To Tool Cupboards" : "Добавить кодовый замок к сундукам с инструментами")]
            public bool CodeLockPrivilege;

            [JsonProperty(PropertyName = en ? "Add Key Lock To Tool Cupboards" : "Добавить ключевой замок к сундукам с инструментами")]
            public bool KeyLockPrivilege;

            [JsonProperty(PropertyName = en ? "Add Code Lock To Boxes" : "Добавить кодовый замок к ящикам")]
            public bool CodeLockBoxes;

            [JsonProperty(PropertyName = en ? "Add Key Lock To Boxes" : "Добавить ключевой замок к ящикам")]
            public bool KeyLockBoxes;

            [JsonProperty(PropertyName = en ? "Add Code Lock To Lockers" : "Добавить кодовый замок к шкафам")]
            public bool CodeLockLockers = true;

            [JsonProperty(PropertyName = en ? "Add Key Lock To Lockers" : "Добавить ключевой замок к шкафам")]
            public bool KeyLockLockers;

            [JsonProperty(PropertyName = en ? "Close Open Doors With No Door Controller Installed" : "Закрыть открытые двери без установленного контроллера дверей")]
            public bool CloseOpenDoors = true;

            [JsonProperty(PropertyName = en ? "Allow Duplicate Items" : "Разрешить дублирование предметов")]
            public bool AllowDuplicates;

            [JsonProperty(PropertyName = en ? "Allow Players To Pickup Deployables" : "Разрешить игрокам поднимать размещаемые предметы")]
            public bool AllowPickup;

            [JsonProperty(PropertyName = en ? "Allow Players To Deploy A Cupboard" : "Разрешить игрокам размещать шкафы")]
            public bool AllowBuildingPriviledges = true;

            [JsonProperty(PropertyName = en ? "Allow Players To Deploy Barricades" : "Разрешить игрокам размещать баррикады")]
            public bool Barricades = true;

            [JsonProperty(PropertyName = en ? "Allow PVP" : "Разрешить PVP")]
            public bool AllowPVP = true;

            [JsonProperty(PropertyName = en ? "Allow Self Damage" : "Разрешить наносить урон себе")]
            public bool AllowSelfDamage = true;

            [JsonProperty(PropertyName = en ? "Allow Friendly Fire (Teams)" : "Разрешить дружественный огонь (команды)")]
            public bool AllowFriendlyFire = true;

            [JsonProperty(PropertyName = en ? "Check Lower Probability Once Per Loot Item" : "Проверять более низкую вероятность один раз для каждого дропа")]
            public bool EnforceProbability;

            [JsonProperty(PropertyName = en ? "Amount Of Items To Spawn For Buyable Events (0 = Use Default Value)" : "Количество предметов для спавна для купленных событий (0 = использовать значение по умолчанию)")]
            public int MaxBuyableTreasure = 0;

            [JsonProperty(PropertyName = en ? "Minimum Amount Of Items To Spawn (0 = Use Max Value)" : "Минимальное количество предметов для спавна (0 = использовать максимальное значение)")]
            public int MinTreasure;

            [JsonProperty(PropertyName = en ? "Amount Of Items To Spawn" : "Количество предметов для спавна")]
            public int MaxTreasure = 30;

            [JsonProperty(PropertyName = en ? "Amount Of Items To Spawn Increased By Item Splits" : "Увеличивать количество предметов для спавна из-за разделения предметов")]
            public bool Dynamic;

            [JsonProperty(PropertyName = en ? "Flame Turret Health" : "Здоровье огненной турели")]
            public float FlameTurretHealth = 300f;

            [JsonProperty(PropertyName = en ? "Block Plugins Which Prevent Item Durability Loss" : "Блокировать плагины, которые предотвращают потерю прочности предметов")]
            public bool EnforceDurability;

            [JsonProperty(PropertyName = en ? "Block Damage Outside Of The Dome To Players Inside" : "Блокировать урон снаружи купола по игрокам внутри")]
            public bool BlockOutsideDamageToPlayersInside;

            [JsonProperty(PropertyName = en ? "Block Damage Outside Of The Dome To Bases Inside" : "Блокировать урон снаружи купола по базам внутри")]
            public bool BlockOutsideDamageToBaseInside;

            [JsonProperty(PropertyName = en ? "Block Damage Inside From Npcs To Players Outside" : "Блокировать урон снаружи купола от NPC по игрокам внутри")]
            public bool BlockNpcDamageToPlayersOutside;

            [JsonProperty(PropertyName = en ? "Building Blocks Are Immune To Damage" : "Строительные блоки устойчивы к урону")]
            public bool BlocksImmune;

            [JsonProperty(PropertyName = en ? "Building Blocks Are Immune To Damage (Twig Only)" : "Строительные блоки устойчивы к урону (только опора)")]
            public bool TwigImmune;

            [JsonProperty(PropertyName = en ? "Boxes Are Invulnerable" : "Ящики неуязвимы")]
            public bool Invulnerable;

            [JsonProperty(PropertyName = en ? "Boxes Are Invulnerable Until Cupboard Is Destroyed" : "Ящики неуязвимы, пока не уничтожен шкаф")]
            public bool InvulnerableUntilCupboardIsDestroyed;

            [JsonProperty(PropertyName = en ? "Spawn Silently (No Notifcation, No Dome, No Map Marker)" : "Бесшумный спавн (нет уведомлений, нет купола, нет маркера на карте)")]
            public bool Silent;

            [JsonProperty(PropertyName = en ? "Use Simple Messaging" : "Использовать простые сообщения")]
            public bool Smart;

            [JsonProperty(PropertyName = en ? "Despawn Dropped Loot Bags From Raid Boxes When Base Despawns" : "Убирать сумки с добычей из ящиков при исчезновении базы")]
            public bool DespawnGreyBags;

            [JsonProperty(PropertyName = en ? "Protect Loot Bags From Raid Boxes For X Seconds After Base Despawns" : "Защищать сумки с добычей от ящиков при рейдах в течение X секунд после исчезновения базы")]
            public float PreventLooting;

            [JsonProperty(PropertyName = en ? "Divide Loot Into All Containers" : "Распределять добычу по всем контейнерам")]
            public bool DivideLoot = true;

            [JsonProperty(PropertyName = en ? "Drop Tool Cupboard Loot After Raid Is Completed" : "Выбрасывать добычу из шкафа инструментов после завершения рейда")]
            public bool DropPrivilegeLoot;

            [JsonProperty(PropertyName = en ? "Drop Container Loot X Seconds After It Is Looted" : "Выбрасывать добычу из контейнера через X секунд после его обчистки")]
            public float DropTimeAfterLooting;

            [JsonProperty(PropertyName = en ? "Drop Container Loot Applies Only To Boxes And Cupboards" : "Выбрасывать добычу из контейнеров только из ящиков и шкафов")]
            public bool DropOnlyBoxesAndPrivileges = true;

            [JsonProperty(PropertyName = en ? "Create Dome Around Event Using Spheres (0 = disabled, recommended = 5)" : "Создавать купол вокруг события с использованием сфер (0 = отключено, рекомендуется = 5)")]
            public int SphereAmount = 5;

            [JsonProperty(PropertyName = en ? "Empty All Containers Before Spawning Loot" : "Очищать все контейнеры перед появлением добычи")]
            public bool EmptyAll = true;

            [JsonProperty(PropertyName = en ? "Empty All Containers (Exclusions)" : "Очищать все контейнеры (исключения)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> EmptyExemptions = new() { "xmas_tree.deployed", "xmas_tree_a.deployed" };

            [JsonProperty(PropertyName = en ? "Eject Corpses From Enemy Raids (Advanced Users Only)" : "Изгонять трупы из рейдов врагов (только для опытных пользователей)")]
            public bool EjectBackpacks = true;

            [JsonProperty(PropertyName = en ? "Eject Corpses From PVE Instantly (Advanced Users Only)" : "Мгновенно изгонять трупы из PvE (только для опытных пользователей)")]
            public bool EjectBackpacksPVE;

            [JsonProperty(PropertyName = en ? "Eject Enemies From Purchased PVE Raids" : "Изгонять врагов из купленных PvE рейдов")]
            public bool EjectPurchasedPVE = true;

            [JsonProperty(PropertyName = en ? "Eject Enemies From Purchased PVP Raids" : "Изгонять врагов из купленных PvP рейдов")]
            public bool EjectPurchasedPVP;

            [JsonProperty(PropertyName = en ? "Eject Enemies From Locked PVE Raids" : "Изгонять врагов из закрытых PvE рейдов")]
            public bool EjectLockedPVE = true;

            [JsonProperty(PropertyName = en ? "Eject Enemies From Locked PVP Raids" : "Изгонять врагов из закрытых PvP рейдов")]
            public bool EjectLockedPVP;

            [JsonProperty(PropertyName = en ? "Eject Tree Radius When Spawning Base" : "Радиус изгнания деревьев при появлении базы")]
            public float TreeRadius;

            [JsonProperty(PropertyName = en ? "Explosion Damage Modifier (0-999)" : "Модификатор урона от взрыва (0-999)")]
            public float ExplosionModifier = 100f;

            [JsonProperty(PropertyName = en ? "Ignore Containers That Spawn With Loot Already" : "Игнорировать контейнеры, которые появляются уже с добычей")]
            public bool IgnoreContainedLoot;

            [JsonProperty(PropertyName = en ? "Loot Amount Multiplier" : "Множитель количества добычи")]
            public float Multiplier = 1f;

            [JsonProperty(PropertyName = en ? "Loot Amount Multiplier (raidablebases.buyable.vip.pve)" : "Множитель количества добычи (raidablebases.buyable.vip.pve)")]
            public float MultiplierPVE = 1f;

            [JsonProperty(PropertyName = en ? "Loot Amount Multiplier (raidablebases.buyable.vip.pvp)" : "Множитель количества добычи (raidablebases.buyable.vip.pvp)")]
            public float MultiplierPVP = 1f;

            [JsonProperty(PropertyName = en ? "Maximum Respawn Npc X Seconds After Death" : "Максимальное время возрождения NPC после смерти (в секундах)")]
            public float RespawnRateMax;

            [JsonProperty(PropertyName = en ? "Minimum Respawn Npc X Seconds After Death" : "Минимальное время возрождения NPC после смерти (в секундах)")]
            public float RespawnRateMin;

            [JsonProperty(PropertyName = en ? "No Item Input For Boxes And TC" : "Запрет складирования предметов в ящики")]
            public bool NoItemInput = true;

            [JsonProperty(PropertyName = en ? "Penalize Players On Death In PVE (ZLevels)" : "Наказывать игроков при смерти в PvE (ZLevels)")]
            public bool PenalizePVE = true;

            [JsonProperty(PropertyName = en ? "Penalize Players On Death In PVP (ZLevels)" : "Наказывать игроков при смерти в PvP (ZLevels)")]
            public bool PenalizePVP = true;

            [JsonProperty(PropertyName = en ? "Require Cupboard Access To Loot" : "Требовать доступ к шкафу для добычи")]
            public bool RequiresCupboardAccess;

            [JsonProperty(PropertyName = en ? "Require Cupboard Access To Place Ladders" : "Требовать доступ к шкафу для установки лестниц")]
            public bool RequiresCupboardAccessLadders;

            [JsonProperty(PropertyName = en ? "Skip Treasure Loot And Use Loot In Base Only" : "Пропускать добычу из сокровищ и использовать только добычу в базе")]
            public bool SkipTreasureLoot;

            [JsonProperty(PropertyName = en ? "Use Buoyant Boxes For Dropped Privilege Loot" : "Использовать плавучие ящики для выбрасываемой добычи из привилегированных шкафов")]
            public bool BuoyantPrivilege;

            [JsonProperty(PropertyName = en ? "Use Buoyant Boxes For Dropped Box Loot" : "Использовать плавучие ящики для выбрасываемой добычи из ящиков")]
            public bool BuoyantBox;

            [JsonProperty(PropertyName = en ? "Rearm Bear Traps When Damaged" : "Повторно вооружать капканы при повреждении")]
            public bool RearmBearTraps;

            [JsonProperty(PropertyName = en ? "Bear Traps Are Immune To Timed Explosives" : "Капканы устойчивы к взрывчатым устройствам с таймером")]
            public bool BearTrapsImmuneToExplosives;

            [JsonProperty(PropertyName = en ? "Force Time In Dome To (requires raidablebases.time)" : "Принудительно установить время в куполе на (требуется raidablebases.time)")]
            public int ForcedTime = -1;

            [JsonProperty(PropertyName = en ? "Remove Locks When Event Is Completed" : "Удалять замки после завершения события")]
            public bool UnlockEverything;

            [JsonProperty(PropertyName = en ? "Always Spawn Base Loot Table" : "Всегда генерировать базовую таблицу добычи")]
            public bool AlwaysSpawnBaseLoot;

            public BuildingOptions Clone() => MemberwiseClone() as BuildingOptions;

            public float ProtectionRadius(RaidableType type) => Mathf.Max(CELL_SIZE, ProtectionRadii.Get(type));

            public int GetLootAmount(RaidableType type)
            {
                int maxTreasure = type == RaidableType.Purchased && MaxBuyableTreasure > 0 ? MaxBuyableTreasure : MaxTreasure;
                return MinTreasure > 0 ? UnityEngine.Random.Range(MinTreasure, maxTreasure + 1) : maxTreasure;
            }
        }

        public class RaidableBaseSettingsEventTypeBase
        {
            [JsonProperty(PropertyName = en ? "Convert PVE To PVP" : "Преобразовать PVE в PVP")]
            public bool ConvertPVE;

            [JsonProperty(PropertyName = en ? "Convert PVP To PVE" : "Преобразовать PVP в PVE")]
            public bool ConvertPVP = true;

            [JsonProperty(PropertyName = en ? "Ignore Safe Checks" : "Игнорировать проверки безопасности")]
            public bool Ignore;

            [JsonProperty(PropertyName = en ? "Ignore Safe Checks In X Radius Only" : "Игнорировать проверки безопасности только в радиусе X")]
            public float SafeRadius;

            [JsonProperty(PropertyName = en ? "Ignore Player Entities At Custom Spawn Locations" : "Игнорировать игровые объекты игроков в пользовательских точках спавна")]
            public bool Skip;

            [JsonProperty(PropertyName = en ? "Spawn Bases X Distance Apart" : "Расстояние между спавнами баз (X)")]
            public float Distance = 100f;

            [JsonProperty(PropertyName = en ? "Spawns Database File (Optional)" : "Файл базы данных спавнов (опционально)")]
            public string SpawnsFile = "none";
        }

        public class EventTypeBaseExtendedSettings : RaidableBaseSettingsEventTypeBase
        {
            [JsonProperty(PropertyName = en ? "Chance To Randomly Spawn PVP Bases (0 = Ignore Setting)" : "Шанс случайного спавна PvP баз (0 = Игнорировать настройку)")]
            public decimal Chance;

            [JsonProperty(PropertyName = en ? "Include PVE Bases" : "Включать PvE базы")]
            public bool IncludePVE = true;

            [JsonProperty(PropertyName = en ? "Include PVP Bases" : "Включать PvP базы")]
            public bool IncludePVP = true;

            [JsonProperty(PropertyName = en ? "Minimum Required Players Online" : "Минимальное количество игроков онлайн")]
            public int PlayerLimit = 1;

            [JsonProperty(PropertyName = en ? "Maximum Limit Of Players Online" : "Максимальное количество игроков онлайн")]
            public int PlayerLimitMax = 300;

            [JsonProperty(PropertyName = en ? "Time To Wait Between Spawns" : "Время ожидания между спавнами")]
            public float Time = 15f;
        }

        public class ScheduledSettings : EventTypeBaseExtendedSettings
        {
            [JsonProperty(PropertyName = en ? "Enabled" : "Включено")]
            public bool Enabled;

            [JsonProperty(PropertyName = en ? "Every Min Seconds" : "Каждые минимальные секунды")]
            public double IntervalMin = 3600f;

            [JsonProperty(PropertyName = en ? "Every Max Seconds" : "Каждые максимальные секунды")]
            public double IntervalMax = 7200f;

            [JsonProperty(PropertyName = en ? "Max Scheduled Events" : "Максимальное количество запланированных событий")]
            public int Max = 1;

            [JsonProperty(PropertyName = en ? "Max To Spawn At Once (0 = Use Max Scheduled Events Amount)" : "Максимум для одновременного спавна (0 = Использовать максимальное количество запланированных событий)")]
            public int MaxOnce;
        }

        public class MaintainedSettings : EventTypeBaseExtendedSettings
        {
            [JsonProperty(PropertyName = en ? "Always Maintain Max Events" : "Всегда поддерживать максимальное количество событий")]
            public bool Enabled;

            [JsonProperty(PropertyName = en ? "Max Maintained Events" : "Максимальное количество поддерживаемых событий")]
            public int Max = 1;
        }

        public class BuyableCooldownsDifficultySettings
        {
            [JsonProperty(PropertyName = en ? "VIP Permission: raidablebases.vipcooldown" : "VIP Разрешение: raidablebases.vipcooldown")]
            public double VIP = 1800f;

            [JsonProperty(PropertyName = en ? "Admin Permission: raidablebases.allow" : "Админ Разрешение: raidablebases.allow")]
            public double Allow;

            [JsonProperty(PropertyName = en ? "Server Admins" : "Серверные администраторы")]
            public double Admin;

            [JsonProperty(PropertyName = en ? "Normal Users" : "Обычные пользователи")]
            public double Cooldown = 1800f;
        }

        public class PluginResetCosts
        {
            [JsonProperty(PropertyName = en ? "Custom Currency" : "Пользовательская валюта")]
            public CustomCostOptions Custom = new(0);

            [JsonProperty(PropertyName = en ? "Economics Money" : "Деньги Economics")]
            public double Money;

            [JsonProperty(PropertyName = en ? "ServerRewards Points" : "Очки ServerRewards")]
            public int Points;

            [JsonIgnore]
            public bool Any => Money > 0 || Points > 0 || Custom.isValid;
        }

        public class BuyableCooldownsSettings
        {
            [JsonProperty(PropertyName = en ? "Reset Cooldown Costs" : "Стоимость сброса времени ожидания")]
            public PluginResetCosts Costs = new();

            [JsonProperty(PropertyName = en ? "Easy" : "Легкий")]
            public BuyableCooldownsDifficultySettings Easy = new();

            [JsonProperty(PropertyName = en ? "Medium" : "Средний")]
            public BuyableCooldownsDifficultySettings Medium = new();

            [JsonProperty(PropertyName = en ? "Hard" : "Тяжело")]
            public BuyableCooldownsDifficultySettings Hard = new();

            [JsonProperty(PropertyName = en ? "Expert" : "Эксперт")]
            public BuyableCooldownsDifficultySettings Expert = new();

            [JsonProperty(PropertyName = en ? "Nightmare" : "Кошмарный")]
            public BuyableCooldownsDifficultySettings Nightmare = new();

            [JsonProperty(PropertyName = en ? "Apply Cooldown To Entire Clan And Team" : "Применить ограничение времени ожидания ко всему клану и команде")]
            public bool ApplyAlly;

            [JsonProperty(PropertyName = en ? "Apply All Cooldowns" : "Применить все ограничения времени ожидания")]
            public bool ApplyAll;

            public void Set(RaidableBases instance, HashSet<ulong> alliance, ulong userid, RaidableMode mode, bool read)
            {
                HashSet<ulong> members = new() { userid };
                if (ApplyAlly)
                {
                    members.UnionWith(alliance);
                    members.UnionWith(instance.GetMembers(userid));
                }
                foreach (var member in members)
                {
                    if (Set(instance.data, member, mode, read))
                    {
                        members.Add(member);
                        alliance.Add(member);
                    }
                }
                instance.UpdateUI();
            }

            public bool Set(StoredData data, ulong userid, RaidableMode mode, bool read)
            {
                bool set = false;
                if (ApplyAll)
                {
                    foreach (var mode2 in GetRaidableModes())
                    {
                        set |= SetInternal(data, userid, mode2, read);
                    }
                }
                else
                {
                    set = SetInternal(data, userid, mode, read);
                }
                return set;
            }

            public BuyableCooldownsDifficultySettings Get(RaidableMode mode) => mode switch { RaidableMode.Easy => Easy, RaidableMode.Medium => Medium, RaidableMode.Hard => Hard, RaidableMode.Expert => Expert, _ => Nightmare };

            public bool Has(StoredData data, ulong userid, RaidableMode mode) => SetInternal(data, userid, mode, true);

            private bool SetInternal(StoredData data, ulong userid, RaidableMode mode, bool read)
            {
                if (userid.HasPermission("raidablebases.buyable.bypass.cooldown"))
                {
                    return false;
                }

                var diff = Get(mode);

                if (diff.Cooldown <= 0)
                {
                    return false;
                }

                var cooldowns = new List<double>() { diff.Cooldown };

                if (BasePlayer.FindByID(userid) is BasePlayer player)
                {
                    if (player.IsFlying || player.limitNetworking)
                    {
                        return false;
                    }

                    if (player.IsAdmin || player.IsDeveloper)
                    {
                        cooldowns.Add(diff.Admin);
                    }
                }

                if (userid.HasPermission("raidablebases.vipcooldown"))
                {
                    cooldowns.Add(diff.VIP);
                }

                if (userid.HasPermission("raidablebases.allow"))
                {
                    cooldowns.Add(diff.Allow);
                }

                double cooldown = double.MaxValue;

                foreach (double value in cooldowns)
                {
                    if (value < cooldown)
                    {
                        cooldown = value;
                    }
                }

                if (cooldown <= 0)
                {
                    return false;
                }

                if (!read)
                {
                    if (!data.BuyableCooldowns.TryGetValue(userid, out var bi))
                    {
                        data.BuyableCooldowns[userid] = bi = new();
                    }

                    string expiredDate = DateTime.Now.AddSeconds(cooldown).ToString();

                    switch (mode)
                    {
                        case RaidableMode.Easy: bi.Easy = expiredDate; break;
                        case RaidableMode.Medium: bi.Medium = expiredDate; break;
                        case RaidableMode.Hard: bi.Hard = expiredDate; break;
                        case RaidableMode.Expert: bi.Expert = expiredDate; break;
                        case RaidableMode.Nightmare: bi.Nightmare = expiredDate; break;
                    }
                }

                return true;
            }
        }

        public class BuyableRefundsSettings
        {
            [JsonProperty(PropertyName = en ? "Refund Despawned Bases" : "Возврат средств при деспавне базы")]
            public bool Enabled;

            [JsonProperty(PropertyName = en ? "Base Becomes Ineligible For Rewards On Despawn" : "База становится неподходящей для вознаграждения при уничтожении")]
            public bool Ineligible = true;

            [JsonProperty(PropertyName = en ? "Block Refund If Base Is Damaged" : "Блокировать возврат, если база повреждена")]
            public bool Damaged = true;

            [JsonProperty(PropertyName = en ? "Block Despawn If Base Is Damaged" : "Блокировать деспавн, если база повреждена")]
            public bool Despawn = true;

            [JsonProperty(PropertyName = en ? "Block Despawn If Anything Is Looted" : "Блокировать деспавн, если что-либо было залутано")]
            public bool AnyLooted = true;

            [JsonProperty(PropertyName = en ? "Refund Percentage" : "Процент возврата")]
            public double Percentage = 100.0;

            [JsonProperty(PropertyName = en ? "Refund Resets Cooldown Timer" : "Возврат сбрасывает таймер времени ожидания")]
            public bool Reset;
        }

        public class BuyableSettings : RaidableBaseSettingsEventTypeBase
        {
            [JsonProperty(PropertyName = en ? "Max Amount Purchasable Per Difficulty (0 = infinite, -1 = disabled)" : "Максимальное количество, доступное к покупке на сложность (0 = бесконечно, -1 = отключено)")]
            public DifficultyModeOptions Limits = new();

            [JsonProperty(PropertyName = en ? "Cooldowns (0 = No Cooldown)" : "Перезарядки (0 = без перезарядки)")]
            public BuyableCooldownsSettings Cooldowns;

            [JsonProperty(PropertyName = en ? "Refunds" : "Возвраты")]
            public BuyableRefundsSettings Refunds = new();

            [JsonProperty(PropertyName = en ? "Allow Players To Spawn Specified Base Files" : "Разрешить игрокам создавать базы из указанных файлов")]
            public bool FileMode;

            [JsonProperty(PropertyName = en ? "Allow Players To Buy PVP Raids" : "Разрешить игрокам покупать PVP рейды")]
            public bool BuyPVP = true;

            [JsonProperty(PropertyName = en ? "Allow Ally With Lockouts To Enter" : "Разрешить союзникам с блокировками входить")]
            public bool AllowAlly = true;

            [JsonProperty(PropertyName = en ? "Lock Raid To Buyer And Friends" : "Заблокировать рейд для покупателя и его друзей")]
            public bool UsePayLock = true;

            [JsonProperty(PropertyName = en ? "Max Buyable Events" : "Максимальное количество покупаемых событий")]
            public int Max = 15;

            [JsonProperty(PropertyName = en ? "Prevent Players From Buying Until Previous Raid Despawns" : "Запретить игрокам покупать, пока предыдущий рейд не будет деспавнен")]
            public bool PreventNew;

            [JsonProperty(PropertyName = en ? "Prevent Players From Hogging Purchased Raids" : "Предотвращение захвата купленных рейдов игроками")]
            public bool PreventHogging;

            [JsonProperty(PropertyName = en ? "Reset Purchased Owner After X Minutes Offline" : "Сброс владельца купленного после X минут неактивности")]
            public float ResetDuration = 10f;

            [JsonProperty(PropertyName = en ? "Use Permission (raidablebases.buyraid)" : "Использовать разрешение (raidablebases.buyraid)")]
            public bool UsePermission;

            [JsonProperty(PropertyName = en ? "Spawn At Closest Position From Player" : "Спавн в ближайшей позиции от игрока")]
            public bool Closest = true;
        }

        public class ManualSettings
        {
            [JsonProperty(PropertyName = en ? "Convert PVE To PVP" : "Преобразовать PVE в PVP")]
            public bool ConvertPVE;

            [JsonProperty(PropertyName = en ? "Convert PVP To PVE" : "Преобразовать PVP в PVE")]
            public bool ConvertPVP;

            [JsonProperty(PropertyName = en ? "Max Manual Events" : "Максимальное количество ручных событий")]
            public int Max = 1;

            [JsonProperty(PropertyName = en ? "Spawns Database File (Optional)" : "Файл базы данных спавнов (опционально)")]
            public string SpawnsFile = "none";
        }

        public class RaidableBaseWallOptions
        {
            [JsonProperty(PropertyName = en ? "Enabled" : "Включено")]
            public bool Enabled = true;

            [JsonProperty(PropertyName = en ? "Stacks" : "Слои")]
            public int Stacks = 1;

            [JsonProperty(PropertyName = en ? "Ignore Stack Limit When Clipping Terrain" : "Игнорировать лимит слоев при обрезке террейна")]
            public bool IgnoreWhenClippingTerrain = true;

            [JsonProperty(PropertyName = en ? "Ignore Forced Height Option" : "Игнорировать насильно заданную высоту (опция)")]
            public bool IgnoreForcedHeight = true;

            [JsonProperty(PropertyName = en ? "Use Stone Walls" : "Использовать каменные стены")]
            public bool Stone = true;

            [JsonProperty(PropertyName = en ? "Use Iced Walls" : "Использовать ледяные стены")]
            public bool Ice;

            [JsonProperty(PropertyName = en ? "Use Least Amount Of Walls" : "Использовать наименьшее количество стен")]
            public bool LeastAmount = true;

            [JsonProperty(PropertyName = en ? "Use UFO Walls" : "Использовать стены UFO")]
            public bool UseUFOWalls;

            [JsonProperty(PropertyName = en ? "Radius" : "Радиус")]
            public float Radius = 25f;
        }

        public class RaidableBaseCostOptions
        {
            [JsonProperty(PropertyName = en ? "Require Custom Costs" : "Требовать индивидуальные затраты")]
            public bool Custom = true;

            [JsonProperty(PropertyName = en ? "Require Economics Costs" : "Требовать затраты в экономике")]
            public bool Economics = true;

            [JsonProperty(PropertyName = en ? "Require Server Rewards Costs" : "Требовать затраты в Server Rewards")]
            public bool ServerRewards = true;
        }

        public class EconomicsOptions
        {
            [JsonProperty(PropertyName = en ? "Easy" : "Легкий")]
            public double Easy;

            [JsonProperty(PropertyName = en ? "Medium" : "Средний")]
            public double Medium;

            [JsonProperty(PropertyName = en ? "Hard" : "Тяжело")]
            public double Hard;

            [JsonProperty(PropertyName = en ? "Expert" : "Эксперт")]
            public double Expert;

            [JsonProperty(PropertyName = en ? "Nightmare" : "Кошмарный")]
            public double Nightmare;

            [JsonIgnore]
            public bool Any => Easy > 0 || Medium > 0 || Hard > 0 || Expert > 0 || Nightmare > 0;

            public double Get(RaidableMode mode) => mode switch
            {
                RaidableMode.Easy => Easy,
                RaidableMode.Medium => Medium,
                RaidableMode.Hard => Hard,
                RaidableMode.Expert => Expert,
                _ => Nightmare
            };
        }

        public class DifficultyModeOptions : DifficultyModesInt
        {
            internal bool Any => Easy > 0 || Medium > 0 || Hard > 0 || Expert > 0 || Nightmare > 0;

            public DifficultyModeOptions() { }

            public int Get(RaidableMode mode) => mode switch
            {
                RaidableMode.Easy => Easy,
                RaidableMode.Medium => Medium,
                RaidableMode.Hard => Hard,
                RaidableMode.Expert => Expert,
                _ => Nightmare
            };
        }

        public class CustomCostShoppyStock
        {
            [JsonProperty(PropertyName = en ? "Item Shortname" : "Сокращенное название предмета")]
            public string ItemName = "";

            [JsonProperty(PropertyName = en ? "Item Skin" : "Скин предмета")]
            public ulong ItemSkin;

            [JsonProperty(PropertyName = en ? "Shop Name" : "Название магазина")]
            public string ShopName = "";

            [JsonProperty(PropertyName = en ? "Panel Family Name" : "Название семейства панели")]
            public string PanelName = "Legacy";

            public bool IsItem(CustomCostOptions option) => option.Shortname == ItemName && option.Skin == ItemSkin;
        }

        public class CustomCostOptions
        {
            [JsonProperty(PropertyName = en ? "Enabled" : "Включено")]
            public bool Enabled;

            [JsonProperty(PropertyName = en ? "Item Shortname" : "Сокращенное название предмета")]
            public string Shortname = "scrap";

            [JsonProperty(PropertyName = en ? "Item Name" : "Название предмета")]
            public string Name = null;

            [JsonProperty(PropertyName = en ? "Amount" : "Количество")]
            public int Amount;

            [JsonProperty(PropertyName = en ? "Skin" : "Скин")]
            public ulong Skin;

            internal ItemDefinition _definition;

            internal ItemDefinition Definition => _definition ??= ItemManager.FindItemDefinition(Shortname);

            internal bool isValid => Enabled && !string.IsNullOrEmpty(Shortname) && Amount > 0 && Definition != null;

            public CustomCostOptions(int amount)
            {
                Amount = amount;
            }
        }

        public class RankedLadderSettings
        {
            [JsonProperty(PropertyName = en ? "Award Top X Players On Wipe" : "Наградить топ X игроков при вайпе")]
            public int Amount = 3;

            [JsonProperty(PropertyName = en ? "Enabled" : "Включено")]
            public bool Enabled = true;

            [JsonProperty(PropertyName = en ? "Show Top X Ladder" : "Показывать топ X лестницы")]
            public int Top = 10;

            [JsonProperty(PropertyName = en ? "Assign Rank After X Completions" : "Назначить ранг после X завершений")]
            public RaidableBaseSettingsRankedLadderPointOptions Assign = new(0, 0, 0, 0, 0, false);

            [JsonProperty(PropertyName = en ? "Difficulty Points" : "Очки сложности")]
            public RaidableBaseSettingsRankedLadderPointOptions Points = new(1, 2, 3, 4, 5, false);
        }

        public class RaidableBaseSettingsRankedLadderPointOptions : DifficultyModesInt
        {
            [JsonProperty(PropertyName = en ? "Assign To Owner Of Raid Only" : "Начислять только владельцу рейда")]
            public bool Owner;

            public RaidableBaseSettingsRankedLadderPointOptions(int a, int b, int c, int d, int e, bool f)
            {
                (Easy, Medium, Hard, Expert, Nightmare, Owner) = (a, b, c, d, e, f);
            }
        }

        public class RewardSettings
        {
            [JsonProperty(PropertyName = en ? "Custom Currency" : "Пользовательская валюта")]
            public CustomCostOptions Custom = new(0);

            [JsonProperty(PropertyName = en ? "Economics Money" : "Деньги Economics")]
            public double Money;

            [JsonProperty(PropertyName = en ? "ServerRewards Points" : "Очки ServerRewards")]
            public int Points;

            [JsonProperty(PropertyName = en ? "SkillTree XP" : "Опыт SkillTree")]
            public double XP;

            [JsonProperty(PropertyName = en ? "Do Not Reward Buyable Events" : "Не награждать события, доступные для покупки")]
            public bool NoBuyableRewards;
        }

        public class SkinSettingsBoxes : SkinSettingsDefault
        {
            [JsonProperty(PropertyName = en ? "Preset Skins" : "Предустановленные скины", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> Skins = new();

            [JsonProperty(PropertyName = en ? "Ignore If Skinned Already" : "Игнорировать, если уже есть скин")]
            public bool IgnoreSkinned;

            [JsonProperty(PropertyName = en ? "Use Identical Skins" : "Использовать идентичные скины")]
            public bool Unique;
        }

        public class SkinSettingsLoot : SkinSettingsDefault
        {
            [JsonProperty(PropertyName = en ? "Use Identical Skins For Stackable Items" : "Использовать идентичные скины для стопок предметов")]
            public bool Stackable = true;

            [JsonProperty(PropertyName = en ? "Use Identical Skins For Non-Stackable Items" : "Использовать идентичные скины для нестопок предметов")]
            public bool NonStackable;
        }

        public class SkinSettingsNpcs : SkinSettingsDefault
        {
            [JsonProperty(PropertyName = en ? "Use Identical Skins" : "Использовать идентичные скины")]
            public bool Unique = true;
        }

        public class SkinSettingsDeployables : SkinSettingsDefault
        {
            [JsonProperty(PropertyName = en ? "Partial Names" : "Частичные названия", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> PartialNames = new()
            {
                "door", "barricade", "chair", "fridge", "furnace", "locker", "reactivetarget", "rug", "sleepingbag", "table", "vendingmachine", "waterpurifier", "skullspikes", "skulltrophy", "summer_dlc", "sled"
            };

            [JsonProperty(PropertyName = en ? "Preset Door Skins" : "Предустановленные скины для дверей", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> Doors = new();

            [JsonProperty(PropertyName = en ? "Skin Everything" : "наносить на всё скины")]
            public bool SkinEverything = true;

            [JsonProperty(PropertyName = en ? "Ignore If Skinned Already" : "Игнорировать, если уже есть скин")]
            public bool IgnoreSkinned;

            [JsonProperty(PropertyName = en ? "Use Identical Skins" : "Использовать идентичные скины")]
            public bool Unique;
        }

        public class SkinSettingsDefault
        {
            [JsonProperty(PropertyName = en ? "Use Random Skin" : "Использовать случайный скин")]
            public bool Random = true;

            [JsonProperty(PropertyName = en ? "Use Workshop Skins" : "Использовать скины из мастерской")]
            public bool Workshop = true;

            [JsonProperty(PropertyName = en ? "Use Imported Workshop Skins File" : "Использовать импортированные скины из мастерской")]
            public bool ImportedWorkshop = true;

            [JsonProperty(PropertyName = en ? "Use Approved Workshop Skins Only" : "Использовать только одобренные скины из мастерской")]
            public bool ApprovedOnly;
        }

        public class SkinSettings
        {
            [JsonProperty(PropertyName = en ? "Boxes" : "Ящики")]
            public SkinSettingsBoxes Boxes = new();

            [JsonProperty(PropertyName = en ? "Npcs" : "NPC")]
            public SkinSettingsNpcs Npc = new();

            [JsonProperty(PropertyName = en ? "Loot Items" : "предметы лута")]
            public SkinSettingsLoot Loot = new();

            [JsonProperty(PropertyName = en ? "Deployables" : "Размещаемые предметы")]
            public SkinSettingsDeployables Deployables = new();
        }

        public class SkinSettingsImportedWorkshop
        {
            [JsonProperty(PropertyName = en ? "Imported Workshop Skins" : "Импортированные скины из мастерской", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, List<ulong>> SkinList = DefaultImportedSkins();
        }

        public class LootItem : IEquatable<LootItem>
        {
            public LootItem() { }

            public LootItem(string shortname, int amountMin = 1, int amount = 1, ulong skin = 0, bool isBlueprint = false, float probability = 1.0f, int stacksize = -1, string name = null, string text = null, bool hasPriority = false)
            {
                (this.shortname, this.amountMin, this.amount, this.skin, this.isBlueprint, this.probability, this.stacksize, this.name, this.text, this.hasPriority) =
                    (shortname, amountMin, amount, skin, isBlueprint, probability, stacksize, name, text, hasPriority);
            }

            [JsonProperty(PropertyName = en ? "text" : "текст", NullValueHandling = NullValueHandling.Ignore)]
            public string text = null;

            [JsonProperty(PropertyName = en ? "shortname" : "краткое_название")]
            public string shortname;

            [JsonProperty(PropertyName = en ? "name" : "имя")]
            public string name = null;

            [JsonProperty(PropertyName = en ? "blueprint" : "чертёж")]
            public bool isBlueprint;

            [JsonProperty(PropertyName = en ? "skin" : "скин")]
            public ulong skin;

            [JsonProperty(PropertyName = en ? "amount" : "количество")]
            public int amount;

            [JsonProperty(PropertyName = en ? "amountMin" : "мин_количество")]
            public int amountMin;

            [JsonProperty(PropertyName = en ? "probability" : "вероятность")]
            public float probability = 1f;

            [JsonProperty(PropertyName = en ? "stacksize" : "размер_стека")]
            public int stacksize = -1;

            [JsonIgnore] public ItemDefinition definition => _def ??= ItemManager.FindItemDefinition(shortname);
            [JsonIgnore] public ItemDefinition _def;
            [JsonIgnore] public bool hasPriority;
            [JsonIgnore] public bool isSplit;

            public bool HasProbability() => UnityEngine.Random.value <= probability;

            public LootItem Clone() => new(shortname, amountMin, amount, skin, isBlueprint, probability, stacksize, name, text, hasPriority);

            public bool Equals(LootItem other) => shortname == other.shortname && amount == other.amount && skin == other.skin && amountMin == other.amountMin;

            public override bool Equals(object obj) => obj is LootItem ti && Equals(ti);

            public override int GetHashCode() => base.GetHashCode();
        }

        public class TreasureSettings
        {
            [JsonProperty(PropertyName = en ? "Resources Not Moved To Cupboards" : "Ресурсы, не перемещаемые в шкафы", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> ExcludeFromCupboard = new()
            {
                "skull.human", "battery.small", "bone.fragments", "can.beans.empty", "can.tuna.empty", "water.salt", "water", "skull.wolf"
            };

            [JsonProperty(PropertyName = en ? "Use Day Of Week Loot" : "Использовать лут по дням недели")]
            public bool Daily;

            [JsonProperty(PropertyName = en ? "Do Not Duplicate Base Loot" : "Не дублировать базовый лут")]
            public bool Base;

            [JsonProperty(PropertyName = en ? "Do Not Duplicate Difficulty Loot" : "Не дублировать лут сложности")]
            public bool Difficulty;

            [JsonProperty(PropertyName = en ? "Do Not Duplicate Default Loot" : "Не дублировать лут по умолчанию")]
            public bool Default;

            [JsonProperty(PropertyName = en ? "Use Stack Size Limit For Spawning Items" : "Использовать ограничение размера стека для появления предметов")]
            public bool Stacks;
        }

        public class UIBaseSettings
        {
            [JsonProperty(PropertyName = en ? "Enabled" : "Включено", Order = 1)]
            public bool Enabled = true;

            [JsonProperty(PropertyName = "Offset Min", Order = 2, NullValueHandling = NullValueHandling.Ignore)]
            public string OffsetMin;

            [JsonProperty(PropertyName = "Offset Max", Order = 3, NullValueHandling = NullValueHandling.Ignore)]
            public string OffsetMax;

            [JsonProperty(PropertyName = en ? "Panel Alpha" : "Прозрачность панели", NullValueHandling = NullValueHandling.Ignore, Order = 4)]
            public float? PanelAlpha = 0.98f;

            [JsonProperty(PropertyName = en ? "Background Color" : "Цвет фона", NullValueHandling = NullValueHandling.Ignore, Order = 5)]
            public string PanelColor = "#252121";

            [JsonProperty(PropertyName = en ? "Title Background Color" : "Цвет фона заголовка", NullValueHandling = NullValueHandling.Ignore, Order = 6)]
            public string TitlePanelColor = "#000000";
        }

        public class BuildingOptionsElevators : UIBaseSettings
        {
            public BuildingOptionsElevators()
            {
                (AnchorMin, AnchorMax, PanelAlpha) = ("0.406 0.915", "0.59 0.949", 0.98f);
            }

            [JsonProperty(PropertyName = "Anchor Min", Order = 2)]
            public string AnchorMin;

            [JsonProperty(PropertyName = "Anchor Max", Order = 3)]
            public string AnchorMax;

            [JsonProperty(PropertyName = en ? "Required Access Level" : "Требуемый уровень доступа", Order = 5)]
            public int RequiredAccessLevel;

            [JsonProperty(PropertyName = en ? "Required Access Level Grants Permanent Use" : "Уровень доступа предоставляет постоянное использование", Order = 6)]
            public bool RequiredAccessLevelOnce;

            [JsonProperty(PropertyName = en ? "Required Keycard Skin ID" : "ID Скина ключа доступа", Order = 7)]
            public ulong SkinID = 2690554489;

            [JsonProperty(PropertyName = en ? "Requires Building Permission" : "Требуется разрешение на строительство", Order = 8)]
            public bool RequiresBuildingPermission;

            [JsonProperty(PropertyName = en ? "Button Health" : "Прочность кнопки", Order = 9)]
            public float ButtonHealth = 1000f;

            [JsonProperty(PropertyName = en ? "Elevator Health" : "Прочность лифта", Order = 10)]
            public float ElevatorHealth = 600f;
        }

        public class UIDelaySettings : UIBaseSettings
        {
            public UIDelaySettings()
            {
                (OffsetMin, OffsetMax, PanelAlpha) = ("-34.488 87.056", "179.631 124.804", 0.98f);
            }

            [JsonProperty(PropertyName = en ? "Font Size" : "Размер шрифта", Order = 4)]
            public int FontSize = 14;

            [JsonProperty(PropertyName = en ? "Text Color" : "Цвет текста", Order = 5)]
            public string TextColor = "#FF0000";
        }

        public class UILockoutSettings
        {
            [JsonProperty(PropertyName = en ? "Enabled" : "Включено")]
            public bool Enabled = true;

            [JsonProperty(PropertyName = "Offset Min")]
            public string OffsetMin = "-117.966 -149.658";

            [JsonProperty(PropertyName = "Offset Max")]
            public string OffsetMax = "-17.834 -106.342";

            [JsonProperty(PropertyName = en ? "Panel Alpha" : "Прозрачность панели")]
            public float Alpha = 0.98f;

            [JsonProperty(PropertyName = en ? "Background Color" : "Цвет фона")]
            public string BackgroundColor = "#242020";

            [JsonProperty(PropertyName = en ? "Title Text Color" : "Цвет текста заголовка")]
            public string TitleColor = "#FFFFFF";

            [JsonProperty(PropertyName = en ? "Title Embed Color" : "Цвет внедренного заголовка")]
            public string TitleEmbedColor = "#242020";

            [JsonProperty(PropertyName = en ? "Title Panel Color" : "Цвет панели заголовка")]
            public string TitlePanelColor = "#000000";
        }

        public class UICooldownSettings
        {
            [JsonProperty(PropertyName = en ? "Enabled" : "Включено")]
            public bool Enabled = true;

            [JsonProperty(PropertyName = "Offset Min")]
            public string OffsetMin = "-117.966 -77.055";

            [JsonProperty(PropertyName = "Offset Max")]
            public string OffsetMax = "-17.834 -33.74";

            [JsonProperty(PropertyName = en ? "Panel Alpha" : "Прозрачность панели")]
            public float Alpha = 0.98f;

            [JsonProperty(PropertyName = en ? "Background Color" : "Цвет фона")]
            public string BackgroundColor = "#242020";

            [JsonProperty(PropertyName = en ? "Title Text Color" : "Цвет текста заголовка")]
            public string TitleColor = "#FFFFFF";

            [JsonProperty(PropertyName = en ? "Title Embed Color" : "Цвет внедренного заголовка")]
            public string TitleEmbedColor = "#242020";

            [JsonProperty(PropertyName = en ? "Title Panel Color" : "Цвет панели заголовка")]
            public string TitlePanelColor = "#000000";
        }

        public class UIBuyableSettings : UIBaseSettings
        {
            public UIBuyableSettings()
            {
                (OffsetMin, OffsetMax, PanelAlpha) = ("-34.159 86.718", "179.959 254.682", 0.98f);
            }

            [JsonProperty(PropertyName = en ? "Cursor Enabled" : "Включение курсора", Order = 5)]
            public bool CursorEnabled;

            [JsonProperty(PropertyName = en ? "Button Alpha" : "Прозрачность кнопки", Order = 6)]
            public float ButtonAlpha = 1f;

            [JsonProperty(PropertyName = en ? "X Text Color" : "Цвет текста 'X'", Order = 7)]
            public string XTextColor = "#FFFFFF";

            [JsonProperty(PropertyName = en ? "Font Size" : "Размер шрифта", Order = 8)]
            public int FontSize = 14;

            [JsonProperty(PropertyName = en ? "Use Contrast Colors For Text Color" : "Использовать контрастные цвета для цвета текста", Order = 9)]
            public bool Contrast = true;

            [JsonProperty(PropertyName = en ? "Use Difficulty Colors For Buttons" : "Использовать цвета сложности для кнопок", Order = 10)]
            public bool Difficulty = true;

            [JsonProperty(PropertyName = en ? "X Button Color" : "Цвет кнопки 'X'", Order = 11)]
            public string CloseColor = "#497CAF";

            [JsonProperty(PropertyName = en ? "Easy Button Color" : "Цвет кнопки 'Легкий'", Order = 12)]
            public string EasyColor = "#497CAF";

            [JsonProperty(PropertyName = en ? "Easy Text Color" : "Цвет текста 'Легкий'", Order = 13)]
            public string EasyTextColor = "#FFFFFF";

            [JsonProperty(PropertyName = en ? "Medium Button Color" : "Цвет кнопки 'Средний'", Order = 14)]
            public string MediumColor = "#497CAF";

            [JsonProperty(PropertyName = en ? "Medium Text Color" : "Цвет текста 'Средний'", Order = 15)]
            public string MediumTextColor = "#FFFFFF";

            [JsonProperty(PropertyName = en ? "Hard Button Color" : "Цвет кнопки 'Тяжело'", Order = 16)]
            public string HardColor = "#497CAF";

            [JsonProperty(PropertyName = en ? "Hard Text Color" : "Цвет текста 'Тяжело'", Order = 17)]
            public string HardTextColor = "#FFFFFF";

            [JsonProperty(PropertyName = en ? "Expert Button Color" : "Цвет кнопки 'Эксперт'", Order = 18)]
            public string ExpertColor = "#497CAF";

            [JsonProperty(PropertyName = en ? "Expert Text Color" : "Цвет текста 'Эксперт'", Order = 19)]
            public string ExpertTextColor = "#FFFFFF";

            [JsonProperty(PropertyName = en ? "Nightmare Button Color" : "Цвет кнопки 'Кошмарный'", Order = 20)]
            public string NightmareColor = "#497CAF";

            [JsonProperty(PropertyName = en ? "Nightmare Text Color" : "Цвет текста 'Кошмарный'", Order = 21)]
            public string NightmareTextColor = "#FFFFFF";

            public string GetButton(RaidableMode mode) => mode switch
            {
                RaidableMode.Easy => EasyColor,
                RaidableMode.Medium => MediumColor,
                RaidableMode.Hard => HardColor,
                RaidableMode.Expert => ExpertColor,
                _ => NightmareColor
            };

            public string GetText(RaidableMode mode) => mode switch
            {
                RaidableMode.Easy => EasyTextColor,
                RaidableMode.Medium => MediumTextColor,
                RaidableMode.Hard => HardTextColor,
                RaidableMode.Expert => ExpertTextColor,
                _ => NightmareTextColor
            };
        }

        public class UIAdvancedAlertSettings : UIBaseSettings
        {
            [JsonProperty(PropertyName = en ? "Time Shown" : "Время отображения", Order = 5)]
            public float Time = 5f;

            [JsonProperty(PropertyName = "Anchor Min", Order = 2)]
            public string AnchorMin;

            [JsonProperty(PropertyName = "Anchor Max", Order = 3)]
            public string AnchorMax;

            public UIAdvancedAlertSettings()
            {
                (AnchorMin, AnchorMax, OffsetMin, OffsetMax, PanelAlpha, PanelColor) = ("0.35 0.85", "0.65 0.95", null, null, null, null);
            }
        }

        public class UIStatusSettings : UIBaseSettings
        {
            public UIStatusSettings()
            {
                (OffsetMin, OffsetMax, PanelAlpha) = ("191.957 17.056", "327.626 79.024", 0.98f);
            }

            [JsonProperty(PropertyName = en ? "Font Size" : "Размер шрифта")]
            public int FontSize = 12;

            [JsonProperty(PropertyName = en ? "Panel Color" : "Цвет панели")]
            public string PanelColor = "#252121";

            [JsonProperty(PropertyName = en ? "PVP Color" : "Цвет PVP")]
            public string ColorPVP = "#FF0000";

            [JsonProperty(PropertyName = en ? "PVE Color" : "Цвет PVE")]
            public string ColorPVE = "#008000";

            [JsonProperty(PropertyName = en ? "No Owner Color" : "Цвет без владельца", Order = 7)]
            public string NoneColor = "#FFFFFF";

            [JsonProperty(PropertyName = en ? "Negative Color" : "Отрицательный цвет", Order = 7)]
            public string NegativeColor = "#FF0000";

            [JsonProperty(PropertyName = en ? "Positive Color" : "Положительный цвет", Order = 8)]
            public string PositiveColor = "#008000";

            [JsonProperty(PropertyName = en ? "Show Loot Left" : "Показывать оставшийся лут")]
            public bool ShowLootLeft = true;
        }

        public class UISettings
        {
            [JsonProperty(PropertyName = en ? "Advanced Alerts UI" : "Расширенные оповещения UI")]
            public UIAdvancedAlertSettings AA = new();

            [JsonProperty(PropertyName = en ? "Buyable Events UI" : "События для покупки UI")]
            public UIBuyableSettings Buyable = new();

            [JsonProperty(PropertyName = en ? "Buyable Cooldowns UI" : "Перезарядки для покупки UI")]
            public UICooldownSettings BuyableCooldowns = new();

            [JsonProperty(PropertyName = en ? "Delay UI" : "Задержка UI")]
            public UIDelaySettings Delay = new();

            [JsonProperty(PropertyName = en ? "Lockouts UI" : "Блокировки UI")]
            public UILockoutSettings Lockout = new();

            [JsonProperty(PropertyName = en ? "Status UI" : "Статус UI")]
            public UIStatusSettings Status = new();
        }

        public class WeaponTypeStateSettings
        {
            [JsonProperty(PropertyName = en ? "AutoTurret" : "Автоматические турели")]
            public bool AutoTurret = true;

            [JsonProperty(PropertyName = en ? "FlameTurret" : "Пламенная турель")]
            public bool FlameTurret = true;

            [JsonProperty(PropertyName = en ? "FogMachine" : "Туманная машина")]
            public bool FogMachine = true;

            [JsonProperty(PropertyName = en ? "GunTrap" : "Ловушка с дробовиком (гантрап)")]
            public bool GunTrap = true;

            [JsonProperty(PropertyName = en ? "SamSite" : "Зенитная установка САМ")]
            public bool SamSite = true;
        }

        public class WeaponTypeAmountSettings
        {
            [JsonProperty(PropertyName = en ? "AutoTurret" : "Автоматические турели")]
            public int AutoTurret = 256;

            [JsonProperty(PropertyName = en ? "FlameTurret" : "Пламенная турель")]
            public int FlameTurret = 256;

            [JsonProperty(PropertyName = en ? "FogMachine" : "Туманная машина")]
            public int FogMachine = 5;

            [JsonProperty(PropertyName = en ? "GunTrap" : "Ловушка с дробовиком (гантрап)")]
            public int GunTrap = 128;

            [JsonProperty(PropertyName = en ? "SamSite" : "Зенитная установка САМ")]
            public int SamSite = 24;
        }

        public class WeaponSettingsSamSite
        {
            [JsonProperty(PropertyName = en ? "Repairs Every X Minutes (0.0 = disabled)" : "Восстановление каждые X минут (0.0 = отключено)")]
            public float Repair = 5f;

            [JsonProperty(PropertyName = en ? "Range (350.0 = Rust default)" : "Дальность (350.0 = значение по умолчанию в Rust)")]
            public float Range = 75f;

            [JsonProperty(PropertyName = en ? "Requires Power Source" : "Требуется источник питания")]
            public bool RequiresPower;

            [JsonProperty(PropertyName = en ? "Minimum Health" : "Минимальное здоровье")]
            public float Min = 1000f;

            [JsonProperty(PropertyName = en ? "Maximum Health" : "Максимальное здоровье")]
            public float Max = 1000f;
        }

        public class WeaponSettingsTeslaCoil
        {
            [JsonProperty(PropertyName = en ? "Requires A Power Source" : "Требуется источник питания")]
            public bool RequiresPower;

            [JsonProperty(PropertyName = en ? "Max Discharge Self Damage Seconds (0 = None, 120 = Rust default)" : "Максимальное время самоповреждения разряда (0 = Нет, 120 = значение по умолчанию в Rust)")]
            public float MaxDischargeSelfDamageSeconds;

            [JsonProperty(PropertyName = en ? "Max Damage Output" : "Максимальный урон")]
            public float MaxDamageOutput = 35f;

            [JsonProperty(PropertyName = en ? "Health" : "Здоровье")]
            public float Health = 250f;
        }

        public class WeaponSettings
        {
            [JsonProperty(PropertyName = en ? "Infinite Ammo" : "Бесконечные патроны")]
            public WeaponTypeStateSettings InfiniteAmmo = new();

            [JsonProperty(PropertyName = en ? "Ammo" : "Патроны")]
            public WeaponTypeAmountSettings Ammo = new();

            [JsonProperty(PropertyName = en ? "Fog Machine Allows Motion Toggle" : "Туманная машина разрешает переключение движения")]
            public bool FogMotion = true;

            [JsonProperty(PropertyName = en ? "Fog Machine Requires A Power Source" : "Туманная машина требует источник питания")]
            public bool FogRequiresPower = true;

            [JsonProperty(PropertyName = en ? "Spooky Speakers Requires Power Source" : "Страшные динамики требуют источник питания")]
            public bool SpookySpeakersRequiresPower;

            [JsonProperty(PropertyName = en ? "Test Generator Power" : "Мощность тестового генератора")]
            public float TestGeneratorPower = 100f;

            [JsonProperty(PropertyName = en ? "Furnace Starting Fuel" : "Начальное топливо печи")]
            public int Furnace = 1000;
        }

        public class SphereColorSettings
        {
            [JsonProperty(PropertyName = en ? "When Locked" : "Когда заблокировано")]
            public SphereColor Locked;

            [JsonProperty(PropertyName = en ? "When Unlocked" : "Когда разблокировано")]
            public SphereColor Unlocked;

            [JsonProperty(PropertyName = en ? "When PVP" : "Когда PVP")]
            public SphereColor PVPState;

            [JsonProperty(PropertyName = en ? "When PVE" : "Когда PVE")]
            public SphereColor PVEState;

            [JsonProperty(PropertyName = en ? "When Active" : "Когда активно")]
            public SphereColor Active;

            [JsonProperty(PropertyName = en ? "When Inactive" : "Когда неактивно")]
            public SphereColor Inactive;
        }

        public class Configuration
        {
            [JsonProperty(PropertyName = en ? "Settings" : "Настройки")]
            public PluginSettings Settings = new();

            [JsonProperty(PropertyName = en ? "Event Messages" : "Сообщения о событиях")]
            public EventMessageSettings EventMessages = new();

            [JsonProperty(PropertyName = en ? "GUIAnnouncements" : "Объявления GUI")]
            public GUIAnnouncementSettings GUIAnnouncement = new();

            [JsonProperty(PropertyName = en ? "Ranked Ladder" : "Ранговая лестница")]
            public RankedLadderSettings RankedLadder = new();

            [JsonProperty(PropertyName = en ? "Skins" : "Скины")]
            public SkinSettings Skins = new();

            [JsonProperty(PropertyName = en ? "Treasure" : "Сокровища")]
            public TreasureSettings Loot = new();

            [JsonProperty(PropertyName = en ? "UI" : "Интерфейс пользователя")]
            public UISettings UI = new();

            [JsonProperty(PropertyName = en ? "Weapons" : "Оружие")]
            public WeaponSettings Weapons = new();

            [JsonProperty(PropertyName = en ? "Log Debug To File" : "Запись отладочных сообщений в файл")]
            public bool LogToFile;
        }

        private bool BuoyantBox;
        private bool isInitialized;
        private Exception exConf;
        private const bool en = true;
        private bool InstallationError;

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                if (en && Config.Get("Настройки") != null || !en && Config.Get("Settings") != null)
                {
                    InstallationError = true;
                    if (en) Puts("ERROR! You cannot install the English version over this Russian installation!");
                    else Puts("ERROR! You cannot install the Russian version without converting your installation first!");
                    NextTick(() => Interface.Oxide.UnloadPlugin(Name));
                    return;
                }
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new NullReferenceException("config");
                isInitialized = true;
            }
            catch (Exception ex)
            {
                exConf = ex;
                LoadDefaultConfig();
                UnityEngine.Debug.LogException(ex);
            }
            if (config.UI.Status.OffsetMin == "43.957 87.056")
            {
                config.UI.Status.OffsetMin = "191.957 17.056";
                config.UI.Status.OffsetMax = "327.626 79.024";
            }
            if (isInitialized)
            {
                SaveConfig();
            }
            UndoSettings = new(config);
        }

        public List<LootItem> TreasureLoot
        {
            get
            {
                if (!Buildings.DifficultyLootLists.TryGetValue(RaidableMode.Random, out var lootList))
                {
                    Buildings.DifficultyLootLists[RaidableMode.Random] = lootList = new();
                }

                return lootList.ToList();
            }
        }

        public List<LootItem> WeekdayLoot
        {
            get
            {
                if (!config.Loot.Daily || !Buildings.WeekdayLootLists.TryGetValue(DateTime.Now.DayOfWeek, out var lootList))
                {
                    Buildings.WeekdayLootLists[DateTime.Now.DayOfWeek] = lootList = new();
                }

                return lootList.ToList();
            }
        }

        protected override void SaveConfig()
        {
            if (isInitialized)
            {
                Config.WriteObject(config);
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new();
            Puts("Loaded default configuration file");
        }

        #endregion

        #region UI

        public enum UiType { Buyable, Cooldown, Delay, Lockout, Status, Teleport }

        public UiHandler UI;

        public class UiOffsets
        {
            public string Min;
            public string Max;
            public UiOffsets() { }
            public UiOffsets(string min, string max)
            {
                (Min, Max) = (min, max);
            }
            public UiOffsets Clone()
            {
                return new(Min, Max);
            }
            public bool Equals(UiOffsets other)
            {
                if (other != null && other.Min == Min)
                {
                    return other.Max == Max;
                }
                return false;
            }
        }

        public class UiHandler
        {
            public string BUYABLE_PARENT = "Hud";
            public string COOLDOWN_PARENT = "Overlay";
            public string DELAY_PARENT = "Overlay";
            public string LOCKOUT_PARENT = "Overlay";
            public string STATUS_PARENT = "Overlay";
            public string ELEVATOR_PARENT = "Hud";
            public string TELEPORT_PARENT = "Hud";
            public RaidableBases Instance;
            public StoredData data => Instance.data;
            public Configuration config => Instance.config;

            public static void AddCuiPanel(CuiElementContainer container, bool cursor, string color, string amin, string amax, string omin, string omax, string parent, string name)
            {
                container.Add(new CuiPanel
                {
                    CursorEnabled = cursor,
                    Image = { Color = color },
                    RectTransform = { AnchorMin = amin, AnchorMax = amax, OffsetMin = omin, OffsetMax = omax }
                }, parent, name, name);
            }

            public static void AddCuiButton(CuiElementContainer container, string buttonColor, string command, string text, string textColor, int fontSize, TextAnchor align, string amin, string amax, string omin, string omax, string parent, string name, string font = "robotocondensed-regular.ttf")
            {
                container.Add(new CuiButton
                {
                    Button = { Color = buttonColor, Command = command },
                    Text = { Text = text, Font = font, FontSize = fontSize, Align = align, Color = textColor },
                    RectTransform = { AnchorMin = amin, AnchorMax = amax, OffsetMin = omin, OffsetMax = omax }
                }, parent, name, name);
            }

            public static void AddCuiElement(CuiElementContainer container, string text, int fontSize, TextAnchor align, string textColor, string amin, string amax, string omin, string omax, string parent, string name, string font = "robotocondensed-bold.ttf", string distance = "1 -1")
            {
                container.Add(new CuiElement
                {
                    DestroyUi = name,
                    Name = name,
                    Parent = parent,
                    Components = {
                    new CuiTextComponent { Text = text, Font = font, FontSize = fontSize, Align = align, Color = textColor },
                    new CuiOutlineComponent { Color = "0 0 0 0", Distance = distance },
                    new CuiRectTransformComponent { AnchorMin = amin, AnchorMax = amax, OffsetMin = omin, OffsetMax = omax }
                }
                });
            }

            private double p(string hex, int j, int k) => int.TryParse(hex.TrimStart('#').Substring(j, k), NumberStyles.AllowHexSpecifier, NumberFormatInfo.CurrentInfo, out var num) ? num : 1;

            private string c(string hex) => ((p(hex, 0, 2) * 299) + (p(hex, 2, 2) * 587) + (p(hex, 4, 2) * 114)) / 1000 >= 128 ? "0 0 0 1" : "1 1 1 1";

            public string _(string hex, float a) => $"{p(hex, 0, 2) / 255} {p(hex, 2, 2) / 255} {p(hex, 4, 2) / 255} {Mathf.Clamp(a, 0f, 1f)}";

            public static void DestroyUi(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, "RB_UI_Buyable");
                CuiHelper.DestroyUi(player, "RB_UI_Cooldown");
                CuiHelper.DestroyUi(player, "RB_UI_Delay");
                CuiHelper.DestroyUi(player, "RB_UI_Lockout");
                CuiHelper.DestroyUi(player, "RB_UI_Status");
                CuiHelper.DestroyUi(player, "RB_UI_Teleport");
            }

            public void DestroyUi(BasePlayer player, UiType type)
            {
                if (config == null || !player.IsOnline() || !users.TryGetValue(player.userID, out var ui))
                {
                    return;
                }

                switch (type)
                {
                    case UiType.Cooldown: CuiHelper.DestroyUi(player, "RB_UI_Cooldown"); ui.Cooldown?.Destroy(); break;
                    case UiType.Delay: CuiHelper.DestroyUi(player, "RB_UI_Delay"); ui.Delay?.Destroy(); break;
                    case UiType.Lockout: CuiHelper.DestroyUi(player, "RB_UI_Lockout"); ui.Lockout?.Destroy(); break;
                    case UiType.Status: CuiHelper.DestroyUi(player, "RB_UI_Status"); ui.Status?.Destroy(); break;
                    case UiType.Teleport: CuiHelper.DestroyUi(player, "RB_UI_Teleport"); ui.Teleport?.Destroy(); Teleport.Remove(player.userID); break;
                }

                if (ui.IsDestroyed)
                {
                    users.Remove(player.userID);
                }
            }

            public void UpdateUi(BasePlayer player, UiType type)
            {
                if (config == null || !player.IsOnline())
                {
                    return;
                }

                var ui = TryAddUser(player);
                var isMovingUi = Movers.ContainsKey(player.userID) && Movers[player.userID].ContainsKey(type);

                switch (type)
                {
                    case UiType.Buyable:
                        {
                            if (config.UI.Buyable.Enabled)
                            {
                                DestroyUi(player, UiType.Buyable);
                                ShowBuyableUi(player, isMovingUi);
                            }
                            break;
                        }
                    case UiType.Cooldown:
                        {
                            if (config.UI.BuyableCooldowns.Enabled)
                            {
                                DestroyUi(player, UiType.Cooldown);
                                ShowBuyableCooldownsUi(player, isMovingUi);
                                if (ui.Cooldown == null || ui.Cooldown.Destroyed)
                                {
                                    ui.Cooldown = Instance.timer.Once(60f, () => UpdateUi(player, UiType.Cooldown));
                                }
                                else ui.Cooldown.Reset();
                            }
                            break;
                        }
                    case UiType.Delay:
                        {
                            if (config.UI.Delay.Enabled)
                            {
                                ShowDelayUi(player, isMovingUi);
                                if (!isMovingUi)
                                {
                                    ui.Delay?.Destroy();
                                }
                                if (ui.Delay == null || ui.Delay.Destroyed)
                                {
                                    ui.Delay = Instance.timer.Once(isMovingUi ? 10f : 1f, () => UpdateUi(player, UiType.Delay));
                                }
                                else ui.Delay.Reset();
                            }
                            break;
                        }
                    case UiType.Lockout:
                        {
                            if (config.UI.Lockout.Enabled)
                            {
                                DestroyUi(player, UiType.Lockout);
                                ShowLockoutsUi(player, isMovingUi);
                                if (ui.Lockout == null || ui.Lockout.Destroyed)
                                {
                                    ui.Lockout = Instance.timer.Once(60f, () => UpdateUi(player, UiType.Lockout));
                                }
                                else ui.Lockout.Reset();
                            }
                            break;
                        }
                    case UiType.Status:
                        {
                            if (config.UI.Status.Enabled)
                            {
                                ShowStatusUi(player, isMovingUi);
                                if (!Instance.Get(player.transform.position, out var raid, 5f) || raid.IsDespawning)
                                {
                                    return;
                                }
                                if (!isMovingUi)
                                {
                                    ui.Status?.Destroy();
                                }
                                if (ui.Status == null || ui.Status.Destroyed)
                                {
                                    ui.Status = Instance.timer.Once(isMovingUi ? 10f : 1f, () => UpdateUi(player, UiType.Status));
                                }
                                else ui.Status.Reset();
                            }
                            break;
                        }
                    case UiType.Teleport:
                        {
                            ShowBuyableTeleportUi(player, isMovingUi);
                            if (!isMovingUi)
                            {
                                ui.Teleport?.Destroy();
                            }
                            if (ui.Teleport == null || ui.Teleport.Destroyed)
                            {
                                ui.Teleport = Instance.timer.Once(isMovingUi ? 10f : 1f, () => UpdateUi(player, UiType.Teleport));
                            }
                            else ui.Teleport.Reset();
                            break;
                        }
                }
            }

            public void DestroyAll()
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    DestroyUi(player);
                }
            }

            private string GetPurchasePrice(RaidableMode mode, string userid, bool special = false)
            {
                if (Instance.CanSpawnDifficultyToday(mode))
                {
                    string text = rf(mx(special ? "Specials" : $"Mode{mode}", userid));
                    if (Instance.GetRaidableMode(text) != RaidableMode.Random)
                    {
                        text = text.SentenceCase();
                    }
                    if (config.Settings.Custom.TryGetValue(Instance.GetModeText(mode), out var o) && o.Count > 0 && o.All(x => x.isValid))
                    {
                        return o.Count == 1 ? string.Format("{0} ({1} {2})", text, o[0].Amount, string.IsNullOrEmpty(o[0].Name) ? o[0].Shortname : o[0].Name) : text;
                    }
                    if (config.Settings.ServerRewards.Get(mode) > 0)
                    {
                        return mx("ServerRewardsRP", userid, text, config.Settings.ServerRewards.Get(mode));
                    }
                    if (config.Settings.Economics.Get(mode) > 0)
                    {
                        return mx("Money", userid, text, config.Settings.Economics.Get(mode));
                    }
                }
                return null;
            }

            public void ShowBuyableUi(BasePlayer player, bool moveUI)
            {
                var ui = config.UI.Buyable;

                if (!ui.Enabled)
                {
                    return;
                }

                var num = 0;
                var container = new CuiElementContainer();
                var panelColor = _(ui.PanelColor, ui.PanelAlpha.Value);
                var tpanelColor = _(ui.TitlePanelColor, ui.PanelAlpha.Value);
                var buyRaids = mx("Buy Raids", player.UserIDString);
                var offsets = GetOffsets(player.userID, UiType.Buyable);
                var dir = moveUI ? "↑" : "→";

                AddCuiPanel(container, true, panelColor, "0.5 0", "0.5 0", offsets.Min, offsets.Max, "Hud", "RB_UI_Buyable");
                AddCuiPanel(container, false, tpanelColor, "0.5 0.5", "0.5 0.5", "-98.647 71.598", "45.647 97.223", "RB_UI_Buyable", "BR_TITLE_PANEL");
                AddCuiPanel(container, false, panelColor, "0.5 0.5", "0.5 0.5", "-68.493 -10.485", "68.193 10.091", "BR_TITLE_PANEL", "BR_EMBED_PANEL");
                AddCuiButton(container, panelColor, $"rb_ui_move {UiType.Buyable}", buyRaids + " " + dir, "1 1 1 1", ui.FontSize, TextAnchor.MiddleCenter, "0.5 0.5", "0.5 0.5", "-68.339 -10.288", "68.341 10.288", "BR_EMBED_PANEL", "BR_MOVE_BUTTON");
                AddCuiPanel(container, false, tpanelColor, "0.5 0.5", "0.5 0.5", "54.61 71.598", "98.48 97.223", "RB_UI_Buyable", "BR_CLOSE_PANEL");
                AddCuiButton(container, panelColor, "ui_buyraid closeui", "ⓧ", _(ui.CloseColor, ui.PanelAlpha.Value), ui.FontSize, TextAnchor.MiddleCenter, "0.5 0.5", "0.5 0.5", "-19.368 -10.288", "19.372 10.486", "BR_CLOSE_PANEL", "BR_CLOSE_BUTTON");

                foreach (RaidableMode mode in GetRaidableModes())
                {
                    var priceText = GetPurchasePrice(mode, player.UserIDString);

                    if (!string.IsNullOrEmpty(priceText))
                    {
                        AddCuiButton(container, _(ui.Difficulty ? config.Settings.Management.Colors2.Get(mode) : ui.GetButton(mode), ui.ButtonAlpha), $"ui_buyraid {(int)mode}", priceText, ui.Contrast ? c(ui.Difficulty ? config.Settings.Management.Colors2.Get(mode) : ui.GetButton(mode)) : _(ui.GetText(mode), 1f), ui.FontSize, TextAnchor.MiddleCenter, "0.5 0.5", "0.5 0.5", $"-98.649 {41.713 - (SPACING_Y * num)}", $"98.481 {62.487 - (SPACING_Y * num)}", "RB_UI_Buyable", $"BR_{mode}_BUTTON"); num++;
                    }
                }

                if (num == 0)
                {
                    SendError(player);
                    return;
                }

                CuiHelper.AddUi(player, container);

                if (moveUI)
                {
                    ShowMoveUi(player, UiType.Buyable, false);
                }
            }

            private void SendError(BasePlayer player)
            {
                if (!config.Settings.Custom.Exists(kvp => kvp.Value.All(o => o.isValid)) && !config.Settings.ServerRewards.Any && !config.Settings.Economics.Any)
                {
                    Message(player, "NoBuyableEventsCostsConfigured");
                }

                if (!config.Settings.Buyable.BuyPVP && Instance.Buildings.Profiles.All(profile => profile.Value.Options.AllowPVP))
                {
                    Message(player, "NoBuyableEventsPVP");
                }

                if (!config.Settings.Management.Amounts.Any())
                {
                    Message(player, "NoBuyableEventsEnabled");
                }

                if (!GetRaidableModes().Exists(Instance.CanSpawnDifficultyToday))
                {
                    Message(player, "NoBuyableEventsToday");
                }
            }

            public void ShowBuyableTeleportUi(BasePlayer player, bool moveUI, float seconds = 0f, RaidableMode mode = RaidableMode.Random)
            {
                if (!Teleport.TryGetValue(player.userID, out var ts))
                {
                    ulong userid = player.userID;
                    Teleport[userid] = ts = new()
                    {
                        Timer = Instance.timer.Once(seconds, () =>
                        {
                            DestroyUi(player, UiType.Teleport);
                            Teleport.Remove(userid);
                        }),
                        time = Time.time + seconds,
                        mode = mode
                    };

                    UpdateUi(player, UiType.Teleport);
                    return;
                }

                if (ts.time < Time.time)
                {
                    DestroyUi(player, UiType.Teleport);
                    ts.Destroy();
                    return;
                }

                var container = new CuiElementContainer();
                var ui = config.UI.Buyable;
                var panelColor = _(ui.PanelColor, ui.PanelAlpha.Value);
                var closeColor = _(ui.CloseColor, ui.PanelAlpha.Value);
                var acceptColor = _(ui.Difficulty ? config.Settings.Management.Colors2.Get(ts.mode) : ui.GetButton(ts.mode), ui.ButtonAlpha);
                var time = Mathf.CeilToInt(ts.time - Time.time);
                var lblTeleport = mx("Teleport Question", player.UserIDString);
                var lblTime = mx("Teleport Seconds To Accept", player.UserIDString, time);

                AddCuiPanel(container, false, panelColor, "0.5 0", "0.5 0", "-116.034 88.736", "116.045 205.264", TELEPORT_PARENT, "RB_UI_Teleport");
                AddCuiButton(container, panelColor, $"ui_buyraid accept_teleport", mx("Accept", player.UserIDString), acceptColor, ui.FontSize, TextAnchor.MiddleCenter, "0.5 0.5", "0.5 0.5", "-93.95 -46.28", "-48.45 -29.12", "RB_UI_Teleport", "BT_ACCEPT_BUTTON");
                AddCuiButton(container, panelColor, $"ui_buyraid decline_teleport", mx("Decline", player.UserIDString), closeColor, ui.FontSize, TextAnchor.MiddleCenter, "0.5 0.5", "0.5 0.5", "48.55 -46.28", "94.05 -29.12", "RB_UI_Teleport", "BR_DECLINE_BUTTON");
                AddCuiElement(container, lblTeleport, ui.FontSize, TextAnchor.MiddleCenter, acceptColor, "0.5 0.5", "0.5 0.5", "-116.04 21.341", "116.04 55.021", "RB_UI_Teleport", "ST_TELEPORT_LABEL");
                AddCuiElement(container, lblTime, ui.FontSize, TextAnchor.MiddleCenter, closeColor, "0.5 0.5", "0.5 0.5", "-116.039 -12.34", "116.041 21.34", "RB_UI_Teleport", "ST_TIME_LABEL");

                CuiHelper.AddUi(player, container);

                if (moveUI)
                {
                    ShowMoveUi(player, UiType.Teleport, false);
                }
            }

            public void ShowDelayUi(BasePlayer player, bool moveUI)
            {
                if (player.IsKilled())
                {
                    return;
                }

                if (!Instance.Get(player.userID, out DelaySettings ds))
                {
                    DestroyUi(player, UiType.Delay);
                    return;
                }

                if (ds.time < Time.time)
                {
                    DestroyUi(player, UiType.Delay);
                    if (Instance.PvpDelay.Remove(player.userID))
                    {
                        ds.Destroy();
                    }
                    return;
                }

                if (Instance.EventTerritory(player.transform.position))
                {
                    DestroyUi(player, UiType.Delay);
                    return;
                }

                var ui = config.UI.Delay;
                var container = new CuiElementContainer();
                var panelColor = _(ui.PanelColor, ui.PanelAlpha.Value);
                var tpColor = _(ui.TitlePanelColor, ui.PanelAlpha.Value);
                var textcolor = _(ui.TextColor, ui.PanelAlpha.Value);
                var time = mx("PVP Delay", player.UserIDString, Mathf.CeilToInt(ds.time - Time.time));
                var offsets = GetOffsets(player.userID, UiType.Delay);
                var dir = moveUI ? "↑" : "→";

                AddCuiPanel(container, false, panelColor, "0.5 0", "0.5 0", offsets.Min, offsets.Max, DELAY_PARENT, "RB_UI_Delay");
                AddCuiPanel(container, false, tpColor, "0.5 0.5", "0.5 0.5", "-99.218 -12.813", "99.218 12.812", "RB_UI_Delay", "PD_TITLE_PANEL");
                AddCuiPanel(container, false, panelColor, "0.5 0.5", "0.5 0.5", "-91.795 -10.485", "91.495 10.091", "PD_TITLE_PANEL", "PD_EMBED_PANEL");
                AddCuiButton(container, panelColor, $"rb_ui_move {UiType.Delay}", time + " " + dir, textcolor, ui.FontSize, TextAnchor.MiddleCenter, "0.5 0.5", "0.5 0.5", "-91.647 -10.288", "91.645 10.288", "PD_EMBED_PANEL", "PD_EMBED_LABEL");

                CuiHelper.AddUi(player, container);

                if (moveUI)
                {
                    ShowMoveUi(player, UiType.Delay, false);
                }
            }

            public void ShowStatusUi(BasePlayer player, bool moveUI)
            {
                if (!Instance.Get(player.transform.position, out var raid, 5f))
                {
                    DestroyUi(player, UiType.Status);
                    return;
                }

                string ownerColor, ownerName;
                var ui = config.UI.Status;
                var container = new CuiElementContainer();
                var colorPanel = _(ui.PanelColor, ui.PanelAlpha.Value);
                var colorTitle = _(ui.TitlePanelColor, ui.PanelAlpha.Value);
                var colorAllow = _(raid.AllowPVP ? ui.ColorPVP : ui.ColorPVE, 1f);
                var textAllow = raid.AllowPVP ? mx(raid.Options.Eco.Enabled ? "PVP ECO UI" : "PVP UI", player.UserIDString) : mx(raid.Options.Eco.Enabled ? "PVE ECO UI" : "PVE UI", player.UserIDString);
                var offsets = GetOffsets(player.userID, UiType.Status);
                var dir = moveUI ? "↑" : "→";

                SetOwner(raid, ui, player, out ownerName, out ownerColor);
                AddCuiPanel(container, false, colorPanel, "0.5 0", "0.5 0", offsets.Min, offsets.Max, STATUS_PARENT, "RB_UI_Status");
                AddCuiPanel(container, false, colorTitle, "0.5 0.5", "0.5 0.5", "-58.355 18.811", "58.265 44.436", "RB_UI_Status", "ST_TITLE_PANEL");
                AddCuiPanel(container, false, colorPanel, "0.5 0.5", "0.5 0.5", "-53.432 -10.288", "53.432 10.288", "ST_TITLE_PANEL", "ST_PVP_PANEL");
                if (raid.DespawnTime > 0)
                {
                    AddCuiButton(container, colorPanel, $"rb_ui_move {UiType.Status}", mx("UIFormatLockoutMinutes", player.UserIDString, raid.DespawnTime), "1 1 1 1", ui.FontSize, TextAnchor.MiddleCenter, "0 0", "1 1", "55.871 0.697", "0.333 0", "ST_PVP_PANEL", "ST_DESPAWN_LABEL");
                }
                AddCuiButton(container, colorPanel, $"rb_ui_move {UiType.Status}", textAllow + " " + dir, colorAllow, ui.FontSize, TextAnchor.MiddleCenter, "0.5 0.5", "0.5 0.5", "-53.429 -10.288", "2.439 10.288", "ST_PVP_PANEL", "ST_PVP_LABEL");
                AddCuiElement(container, mx("Owner", player.UserIDString), ui.FontSize, TextAnchor.MiddleLeft, "1 0.87 0.05 1", "0 0", "1 1", "9.853 2.775", "-85.197 -38.414", "RB_UI_Status", "ST_OWNER_LABEL");
                AddCuiElement(container, ownerName, ui.FontSize, TextAnchor.MiddleRight, ownerColor, "0 0", "1 1", "50.473 2.774", "-9.194 -38.415", "RB_UI_Status", "ST_NAME_LABEL");
                if (ui.ShowLootLeft)
                {
                    AddCuiElement(container, mx("Loot", player.UserIDString), ui.FontSize, TextAnchor.MiddleLeft, "1 0.87 0.05 1", "0 0", "1 1", "9.851 23.555", "-69.078 -17.644", "RB_UI_Status", "ST_LOOT_LABEL");
                    AddCuiElement(container, raid.GetLootAmountRemaining().ToString(), ui.FontSize, TextAnchor.MiddleRight, "1 1 1 1", "0 0", "1 1", "66.595 23.555", "-9.195 -17.644", "RB_UI_Status", "ST_LOOTLEFT_LABEL");
                }

                CuiHelper.AddUi(player, container);

                if (moveUI)
                {
                    ShowMoveUi(player, UiType.Status, false);
                }
            }

            private void SetOwner(RaidableBase raid, UIStatusSettings ui, BasePlayer player, out string ownerName, out string ownerColor)
            {
                ownerColor = ui.NoneColor;
                ownerName = mx("None", player.UserIDString);

                if (raid.ownerId.IsSteamId())
                {
                    if (raid.ownerId == player.userID)
                    {
                        ownerColor = ui.PositiveColor;
                        ownerName = mx("You", player.UserIDString);
                    }
                    else if (raid.IsAlly(raid.ownerId, player.userID))
                    {
                        ownerColor = ui.PositiveColor;
                        ownerName = mx("Ally", player.UserIDString);
                    }
                    else
                    {
                        ownerColor = ui.NegativeColor;
                        ownerName = mx("Enemy", player.UserIDString);
                    }
                }

                if (config.Settings.Management.LockTime > 0f)
                {
                    float time = raid.GetRaider(player).lastActiveTime;
                    float secondsLeft = Mathf.Max(0f, (config.Settings.Management.LockTime * 60f) - (Time.time - time));
                    ownerName = $"{ownerName} ({mx("UiInactiveTimeLeft", player.UserIDString, GetMinutes(secondsLeft).ToString())})";
                }

                ownerColor = _(ownerColor, 1f);
            }

            private void CreateUi(BasePlayer player, bool moveUI, UiType type, string name, float alpha, string title, string titleColor, string backgroundColor, string titlePanelColor, string titleEmbedColor, string offsetMin, string offsetMax, string easy, string med, string hard, string expert, string nm)
            {
                var container = new CuiElementContainer();
                var dir = moveUI ? "↑" : "→";

                AddCuiPanel(container, false, backgroundColor, "1 0.5", "1 0.5", offsetMin, offsetMax, type == UiType.Lockout ? LOCKOUT_PARENT : COOLDOWN_PARENT, name);
                AddCuiPanel(container, false, titlePanelColor, "0.5 0.5", "0.5 0.5", "-50.065 24.488", "50.065 45.711", name, $"{name}_PANEL");
                AddCuiButton(container, titleEmbedColor, $"rb_ui_move {type}", title + " " + dir, titleColor, 8, TextAnchor.MiddleCenter, "0.5 0.5", "0.5 0.5", "-46.192 -8.12", "46.194 8.12", $"{name}_PANEL", $"{name}_DESC_BUTTON");

                AddCuiPanel(container, false, GetBackgroundColor(RaidableMode.Easy, alpha), "0.5 0.5", "0.5 0.5", "-47.717 1.546", "-17.283 19.454", name, $"{name}_EASY_PANEL");
                AddCuiElement(container, easy, 8, TextAnchor.MiddleCenter, GetTextColor(RaidableMode.Easy), "0.5 0.5", "0.5 0.5", "-16.31 -8.955", "15.217 8.954", $"{name}_EASY_PANEL", $"{name}_EASY_LABEL");

                AddCuiPanel(container, false, GetBackgroundColor(RaidableMode.Medium, alpha), "0.5 0.5", "0.5 0.5", "-15.317 1.546", "15.117 19.454", name, $"{name}_MED_PANEL");
                AddCuiElement(container, med, 8, TextAnchor.MiddleCenter, GetTextColor(RaidableMode.Medium), "0.5 0.5", "0.5 0.5", "-23.48 -12.812", "23.3 12.813", $"{name}_MED_PANEL", $"{name}_MED_LABEL");

                AddCuiPanel(container, false, GetBackgroundColor(RaidableMode.Hard, alpha), "0.5 0.5", "0.5 0.5", "17.683 1.546", "48.117 19.454", name, $"{name}_HARD_PANEL");
                AddCuiElement(container, hard, 8, TextAnchor.MiddleCenter, GetTextColor(RaidableMode.Hard), "0.5 0.5", "0.5 0.5", "-23.48 -12.812", "23.3 12.813", $"{name}_HARD_PANEL", $"{name}_HARD_LABEL");

                AddCuiPanel(container, false, GetBackgroundColor(RaidableMode.Expert, alpha), "0.5 0.5", "0.5 0.5", "-47.717 -17.909", "-17.283 0", name, $"{name}_EXPERT_PANEL");
                AddCuiElement(container, expert, 8, TextAnchor.MiddleCenter, GetTextColor(RaidableMode.Expert), "0.5 0.5", "0.5 0.5", "-23.48 -12.812", "23.3 12.813", $"{name}_EXPERT_PANEL", $"{name}_EXPERT_LABEL");

                AddCuiPanel(container, false, GetBackgroundColor(RaidableMode.Nightmare, alpha), "0.5 0.5", "0.5 0.5", "-15.317 -17.909", "15.117 0", name, $"{name}_NM_PANEL");
                AddCuiElement(container, nm, 8, TextAnchor.MiddleCenter, GetTextColor(RaidableMode.Nightmare), "0.5 0.5", "0.5 0.5", "-23.48 -12.812", "23.3 12.813", $"{name}_NM_PANEL", $"{name}_NM_LABEL");

                CuiHelper.AddUi(player, container);

                if (moveUI)
                {
                    ShowMoveUi(player, type, false);
                }
            }

            public void ShowLockoutsUi(BasePlayer player, bool moveUI)
            {
                if (!data.Lockouts.TryGetValue(player.UserIDString, out var lo))
                {
                    return;
                }

                if (!lo.Any())
                {
                    CuiHelper.DestroyUi(player, "RB_UI_Lockout");
                    return;
                }

                var ui = Instance.config.UI.Lockout;
                var minsEasy = mx("UIFormatLockoutMinutes", player.UserIDString, GetMinutes(RaidableMode.Easy, lo));
                var minsMed = mx("UIFormatLockoutMinutes", player.UserIDString, GetMinutes(RaidableMode.Medium, lo));
                var minsHard = mx("UIFormatLockoutMinutes", player.UserIDString, GetMinutes(RaidableMode.Hard, lo));
                var minsExpert = mx("UIFormatLockoutMinutes", player.UserIDString, GetMinutes(RaidableMode.Expert, lo));
                var minsNightmare = mx("UIFormatLockoutMinutes", player.UserIDString, GetMinutes(RaidableMode.Nightmare, lo));
                var title = mx("Normal Lockouts", player.UserIDString);
                var offsets = GetOffsets(player.userID, UiType.Lockout);

                CuiHelper.DestroyUi(player, "RB_UI_Lockout");
                CreateUi(player, moveUI, UiType.Lockout, "RB_UI_Lockout", ui.Alpha, title, _(ui.TitleColor, ui.Alpha), _(ui.BackgroundColor, ui.Alpha), _(ui.TitlePanelColor, ui.Alpha), _(ui.TitleEmbedColor, ui.Alpha), offsets.Min, offsets.Max, minsEasy, minsMed, minsHard, minsExpert, minsNightmare);
            }

            public void ShowBuyableCooldownsUi(BasePlayer player, bool moveUI)
            {
                data.BuyableCooldowns.RemoveAll((userid, bin) => userid.HasPermission("raidablebases.buyable.bypass.cooldown") || !BuyableInfo.HasTimeRemaining(Instance, userid));

                if (!data.BuyableCooldowns.ContainsKey(player.userID))
                {
                    return;
                }

                var ui = Instance.config.UI.BuyableCooldowns;
                var minsEasy = mx("UIFormatLockoutMinutes", player.UserIDString, GetMinutes(player, RaidableMode.Easy));
                var minsMed = mx("UIFormatLockoutMinutes", player.UserIDString, GetMinutes(player, RaidableMode.Medium));
                var minsHard = mx("UIFormatLockoutMinutes", player.UserIDString, GetMinutes(player, RaidableMode.Hard));
                var minsExpert = mx("UIFormatLockoutMinutes", player.UserIDString, GetMinutes(player, RaidableMode.Expert));
                var minsNightmare = mx("UIFormatLockoutMinutes", player.UserIDString, GetMinutes(player, RaidableMode.Nightmare));
                var title = mx("Buyable Cooldowns", player.UserIDString);
                var offsets = GetOffsets(player.userID, UiType.Cooldown);

                CreateUi(player, moveUI, UiType.Cooldown, "RB_UI_Cooldown", ui.Alpha, title, _(ui.TitleColor, ui.Alpha), _(ui.BackgroundColor, ui.Alpha), _(ui.TitlePanelColor, ui.Alpha), _(ui.TitleEmbedColor, ui.Alpha), offsets.Min, offsets.Max, minsEasy, minsMed, minsHard, minsExpert, minsNightmare);
            }

            public void ShowMoveUi(BasePlayer player, UiType type, bool destroyUi)
            {
                string parent = type switch
                {
                    UiType.Buyable => "RB_UI_Buyable",
                    UiType.Cooldown => "RB_UI_Cooldown",
                    UiType.Delay => "RB_UI_Delay",
                    UiType.Lockout => "RB_UI_Lockout",
                    _ => "RB_UI_Status"
                };

                string name = $"RB_UI_MOVE_{player.userID}";
                ulong userid = player.userID;

                if (!Movers.TryGetValue(player.userID, out var types))
                {
                    Movers[player.userID] = types = new();
                }

                if (destroyUi && TryDestroyMoveUi(player, userid, types, type, name))
                {
                    return;
                }

                var container = new CuiElementContainer();
                var yOffset = type == UiType.Cooldown || type == UiType.Lockout ? 11.0 : 0.0;

                AddCuiPanel(container, true, "0 0 0 1", "0.5 1", "0.5 1", $"-38.204 {14.798 + yOffset}", $"41.204 {35.602 + yOffset}", parent, name);
                AddCuiPanel(container, false, "1 1 1 1", "0.5 0.5", "0.5 0.5", "-35.722 -7.843", "35.247 7.253", name, $"{name}_E");
                AddCuiButton(container, "0.14 0.12 0.12 1", $"rb_ui_move {type} left", "←", "0.68 0.49 0.14 1", 10, TextAnchor.MiddleCenter, "0.5 0.5", "0.5 0.5", "-35.484 -7.548", "-17.742 7.548", $"{name}_E", $"{name}_L");
                AddCuiButton(container, "0.14 0.12 0.12 1", $"rb_ui_move {type} up", "↑", "0.68 0.49 0.14 1", 10, TextAnchor.MiddleCenter, "0.5 0.5", "0.5 0.5", "-17.743 -7.548", "0 7.548", $"{name}_E", $"{name}_T");
                AddCuiButton(container, "0.14 0.12 0.12 1", $"rb_ui_move {type} down", "↓", "0.68 0.49 0.14 1", 10, TextAnchor.MiddleCenter, "0.5 0.5", "0.5 0.5", "-0.001 -7.548", "17.742 7.548", $"{name}_E", $"{name}_B");
                AddCuiButton(container, "0.14 0.12 0.12 1", $"rb_ui_move {type} right", "→", "0.68 0.49 0.14 1", 10, TextAnchor.MiddleCenter, "0.5 0.5", "0.5 0.5", "17.742 -7.548", "35.485 7.548", $"{name}_E", $"{name}_R");

                CuiHelper.AddUi(player, container);

                if (types.TryGetValue(type, out var closer))
                {
                    closer.Destroy();
                }

                types[type] = closer = Instance.timer.Once(5f, () =>
                {
                    TryDestroyMoveUi(player, userid, types, type, name);
                });
            }

            public void DestroyTimer(BasePlayer player, UiType type)
            {
                if (Movers.TryGetValue(player.userID, out var types) && types.TryGetValue(type, out var closer))
                {
                    if (types.Remove(type) && types.Count == 0)
                    {
                        Movers.Remove(player.userID);
                    }
                    closer?.Destroy();
                }
            }

            private bool TryDestroyMoveUi(BasePlayer player, ulong userid, Dictionary<UiType, Timer> types, UiType type, string name)
            {
                if (types.Remove(type))
                {
                    if (types.Count == 0)
                    {
                        Movers.Remove(userid);
                    }
                    UpdateUi(player, type);
                    CuiHelper.DestroyUi(player, name);
                    SaveOffsetData();
                    return true;
                }
                return false;
            }

            private double GetMinutes(double value) => Math.Ceiling(TimeSpan.FromSeconds(value).TotalMinutes);

            private double GetMinutes(BasePlayer buyer, RaidableMode mode) => GetMinutes(Math.Max(0, BuyableInfo.GetTimeRemaining(Instance, buyer, mode, false)));

            private string GetMinutes(RaidableMode mode, Lockout lo) => GetMinutes(Math.Max(0, lo.Get(mode))).ToString();

            private string GetBackgroundColor(RaidableMode mode, float alpha) => _(config.UI.Buyable.Difficulty ? config.Settings.Management.Colors2.Get(mode) : config.UI.Buyable.GetButton(mode), alpha);

            private string GetTextColor(RaidableMode mode) => config.UI.Buyable.Contrast ? c(config.UI.Buyable.Difficulty ? config.Settings.Management.Colors2.Get(mode) : config.UI.Buyable.GetButton(mode)) : _(config.UI.Buyable.GetText(mode), 1f);

            public PlayerUi TryAddUser(BasePlayer player)
            {
                if (!Offsets.TryGetValue(player.userID, out var ui) || ui == null)
                {
                    Offsets[player.userID] = ui = new();
                }
                TrySetDefaultTypes(ui);
                if (!users.TryGetValue(player.userID, out var pi) || pi == null)
                {
                    users[player.userID] = pi = new();
                }
                return pi;
            }

            public UiOffsets GetOffsets(ulong userid, UiType type)
            {
                if (!Offsets.TryGetValue(userid, out var ui) || ui == null)
                {
                    Offsets[userid] = ui = new();
                }
                return TrySetDefaultTypes(ui)[type];
            }

            private Dictionary<UiType, UiOffsets> TrySetDefaultTypes(Dictionary<UiType, UiOffsets> ui)
            {
                if (!ui.TryGetValue(UiType.Buyable, out var offsets) || !IsValidOffset(offsets))
                {
                    ui[UiType.Buyable] = DefaultBuyableOffsets.Clone();
                }
                if (!ui.TryGetValue(UiType.Cooldown, out offsets) || !IsValidOffset(offsets))
                {
                    ui[UiType.Cooldown] = DefaulCooldownOffsets.Clone();
                }
                if (!ui.TryGetValue(UiType.Delay, out offsets) || !IsValidOffset(offsets))
                {
                    ui[UiType.Delay] = DefaultDelayOffsets.Clone();
                }
                if (!ui.TryGetValue(UiType.Lockout, out offsets) || !IsValidOffset(offsets))
                {
                    ui[UiType.Lockout] = DefaultLockoutOffsets.Clone();
                }
                if (!ui.TryGetValue(UiType.Status, out offsets) || !IsValidOffset(offsets))
                {
                    ui[UiType.Status] = DefaultStatusOffsets.Clone();
                }
                if (ui[UiType.Status].Min == "43.957 87.056")
                {
                    ui[UiType.Status].Min = "191.957 17.056";
                }
                if (ui[UiType.Status].Max == "179.626 149.024")
                {
                    ui[UiType.Status].Max = "327.626 79.024";
                }
                return ui;
            }

            public void SaveOffsetData()
            {
                if (Offsets == null)
                {
                    return;
                }
                var _offsets = new Dictionary<ulong, Dictionary<UiType, UiOffsets>>();
                foreach (var (userid, offsets) in Offsets)
                {
                    var ui = new Dictionary<UiType, UiOffsets>();
                    foreach (var (type, offset) in offsets)
                    {
                        if (!IsDefault(type, offset))
                        {
                            ui[type] = offset;
                        }
                    }
                    if (ui.Count > 0)
                    {
                        _offsets[userid] = ui;
                    }
                }
                Interface.Oxide.DataFileSystem.WriteObject(Name + "UI", _offsets);
            }

            private bool IsDefault(UiType type, UiOffsets offsets) => type switch
            {
                UiType.Buyable => offsets.Equals(DefaultBuyableOffsets),
                UiType.Cooldown => offsets.Equals(DefaulCooldownOffsets),
                UiType.Delay => offsets.Equals(DefaultDelayOffsets),
                UiType.Lockout => offsets.Equals(DefaultLockoutOffsets),
                UiType.Status => offsets.Equals(DefaultStatusOffsets),
                _ => false,
            };

            public void LoadOffsetData()
            {
                try { Offsets = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<UiType, UiOffsets>>>(Name + "UI"); } catch { }
                Offsets ??= new();
                DefaultBuyableOffsets = new(config.UI.Buyable.OffsetMin, config.UI.Buyable.OffsetMax);
                if (!IsValidOffset(DefaultBuyableOffsets)) { Puts("Invalid Buyable UI Offsets"); DefaultBuyableOffsets = new("-34.159 86.718", "179.959 254.682"); }
                DefaulCooldownOffsets = new(config.UI.BuyableCooldowns.OffsetMin, config.UI.BuyableCooldowns.OffsetMax);
                if (!IsValidOffset(DefaulCooldownOffsets)) { Puts("Invalid Buyable Cooldown UI Offsets"); DefaulCooldownOffsets = new("-117.966 -77.055", "-17.834 -33.74"); }
                DefaultDelayOffsets = new(config.UI.Delay.OffsetMin, config.UI.Delay.OffsetMax);
                if (!IsValidOffset(DefaultDelayOffsets)) { Puts("Invalid Delay UI Offsets"); DefaultDelayOffsets = new("-34.488 87.056", "179.631 124.804"); }
                DefaultLockoutOffsets = new(config.UI.Lockout.OffsetMin, config.UI.Lockout.OffsetMax);
                if (!IsValidOffset(DefaultLockoutOffsets)) { Puts("Invalid Lockout UI Offsets"); DefaultLockoutOffsets = new("-117.966 -149.658", "-17.834 -106.342"); }
                DefaultStatusOffsets = new(config.UI.Status.OffsetMin, config.UI.Status.OffsetMax);
                if (!IsValidOffset(DefaultStatusOffsets)) { Puts("Invalid Status UI Offsets"); DefaultStatusOffsets = new("191.957 17.056", "327.626 79.024"); }
            }

            private bool IsValidOffset(UiOffsets offsets)
            {
                if (offsets == null) return false;
                if (string.IsNullOrEmpty(offsets.Min)) return false;
                if (string.IsNullOrEmpty(offsets.Max)) return false;
                if (!offsets.Min.Contains(" ") || !offsets.Max.Contains(" ")) return false;
                if (offsets.Min.Contains(",") || offsets.Max.Contains(",")) return false;
                if (!offsets.Min.Replace(" ", "").Replace(".", "").Replace("-", "").IsNumeric()) return false;
                if (!offsets.Max.Replace(" ", "").Replace(".", "").Replace("-", "").IsNumeric()) return false;
                return true;
            }

            private void Message(BasePlayer player, string key, params object[] args) => Instance.Message(player, key, args);

            private string mx(string key, string id = null, params object[] args) => Instance.mx(key, id, args);

            public UiOffsets DefaultBuyableOffsets, DefaulCooldownOffsets, DefaultDelayOffsets, DefaultLockoutOffsets, DefaultStatusOffsets;

            public Dictionary<ulong, Dictionary<UiType, UiOffsets>> Offsets;
            public Dictionary<ulong, Dictionary<UiType, Timer>> Movers = new();
            public Dictionary<ulong, TimeSettings> Teleport = new();

            public const double SPACING_Y = 28.5;

            public Dictionary<ulong, PlayerUi> users = new();

            public class PlayerUi
            {
                public BasePlayer player;
                public Timer Cooldown, Delay, Lockout, Status, Teleport;
                public bool IsDestroyed => Cooldown == null & Delay == null & Lockout == null && Status == null && Teleport == null;
            }
        }

        #endregion UI
    }
}

namespace Oxide.Plugins.RaidableBasesExtensionMethods
{
    public static class ExtensionMethods
    {
        internal static Core.Libraries.Permission p;
        public static bool All<T>(this IEnumerable<T> a, Func<T, bool> b) { foreach (T c in a) { if (!b(c)) { return false; } } return true; }
        public static int Average(this IList<int> a) { if (a.Count == 0) { return 0; } int b = 0; for (int i = 0; i < a.Count; i++) { b += a[i]; } return b != 0 ? b / a.Count : 0; }
        public static T ElementAt<T>(this IEnumerable<T> a, int b) { using (var c = a.GetEnumerator()) { while (c.MoveNext()) { if (b == 0) { return c.Current; } b--; } } return default(T); }
        public static bool Exists<T>(this HashSet<T> a) where T : BaseEntity { foreach (var b in a) { if (!b.IsKilled()) { return true; } } return false; }
        public static bool Exists<T>(this IEnumerable<T> a, Func<T, bool> b = null) { using (var c = a.GetEnumerator()) { while (c.MoveNext()) { if (b == null || b(c.Current)) { return true; } } } return false; }
        public static T FirstOrDefault<T>(this IEnumerable<T> a, Func<T, bool> b = null) { using (var c = a.GetEnumerator()) { while (c.MoveNext()) { if (b == null || b(c.Current)) { return c.Current; } } } return default(T); }
        public static IEnumerable<T> DefaultIfEmpty<T>(this IEnumerable<T> a, T v) { using (var c = a.GetEnumerator()) { if (c.MoveNext()) { do { yield return c.Current; } while (c.MoveNext()); } else { yield return v; } } }
        public static void ForEach<T>(this IEnumerable<T> a, Action<T> action) { foreach (T n in a) { action(n); } }
        public static int RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> c, Func<TKey, TValue, bool> d) { int a = 0; foreach (var b in c.ToList()) { if (d(b.Key, b.Value)) { c.Remove(b.Key); a++; } } return a; }
        public static IEnumerable<V> Select<T, V>(this IEnumerable<T> a, Func<T, V> b) { var c = new List<V>(); using (var d = a.GetEnumerator()) { while (d.MoveNext()) { c.Add(b(d.Current)); } } return c; }
        public static string[] Skip(this string[] a, int b) { if (a.Length == 0 || b >= a.Length) { return Array.Empty<string>(); } int n = a.Length - b; string[] c = new string[n]; Array.Copy(a, b, c, 0, n); return c; }
        public static List<T> Take<T>(this IList<T> a, int b) { var c = new List<T>(); for (int i = 0; i < a.Count; i++) { if (c.Count == b) { break; } c.Add(a[i]); } return c; }
        public static Dictionary<T, V> ToDictionary<S, T, V>(this IEnumerable<S> a, Func<S, T> b, Func<S, V> c) { var d = new Dictionary<T, V>(); using (var e = a.GetEnumerator()) { while (e.MoveNext()) { d[b(e.Current)] = c(e.Current); } } return d; }
        public static List<T> ToList<T>(this IEnumerable<T> a) => new(a);
        public static List<T> Where<T>(this IEnumerable<T> a, Func<T, bool> b) { List<T> c = new(a is ICollection<T> n ? n.Count : 4); foreach (var d in a) { if (b(d)) { c.Add(d); } } return c; }
        public static List<T> OrderBy<T, TKey>(this IEnumerable<T> a, Func<T, TKey> s) { List<T> m = new(a); m.Sort((x, y) => Comparer<TKey>.Default.Compare(s(x), s(y))); return m; }
        public static int Sum<T>(this IEnumerable<T> a, Func<T, int> b) { int c = 0; foreach (T d in a) { c += b(d); } return c; }
        public static int Count<T>(this IEnumerable<T> a, Func<T, bool> b) { int c = 0; foreach (T d in a) { if (b(d)) { c++; } } return c; }
        public static IEnumerable<T> Union<T>(this IEnumerable<T> a, IEnumerable<T> b, IEqualityComparer<T> c = null) { HashSet<T> d = new(c); foreach (T e in a) { if (d.Add(e)) { yield return e; } } foreach (T f in b) { if (d.Add(f)) { yield return f; } } }
        public static bool HasPermission(this string a, string b) { if (p == null) { p = Interface.Oxide.GetLibrary<Core.Libraries.Permission>(null); } return !string.IsNullOrEmpty(a) && p.UserHasPermission(a, b); }
        public static bool HasPermission(this BasePlayer a, string b) => a != null && a.UserIDString.HasPermission(b);
        public static bool HasPermission(this ulong a, string b) => a.IsSteamId() && a.ToString().HasPermission(b);
        public static bool BelongsToGroup(this string a, string b) { if (p == null) { p = Interface.Oxide.GetLibrary<Core.Libraries.Permission>(null); } return !string.IsNullOrEmpty(a) && p.UserHasGroup(a, b); }
        public static bool BelongsToGroup(this ulong a, string b) => a.ToString().BelongsToGroup(b);
        public static bool IsOnline(this BasePlayer a) => a.IsReallyValid() && a.net.connection != null;
        public static bool IsKilled(this BaseNetworkable a) { try { return (object)a == null || a.IsDestroyed || a.transform == null;  } catch { return true; } }
        public static bool IsNull(this BaseNetworkable a) => (object)a == null || a.IsDestroyed;
        public static bool IsReallyValid(this BaseNetworkable a) => !((object)a == null || a.IsDestroyed || (object)a.net == null);
        public static void SafelyKill(this BaseNetworkable a) { try { if (!a.IsKilled()) a.Kill(BaseNetworkable.DestroyMode.None); } catch { } }
        public static bool CanCall(this Plugin o) => (object)o != null && o.IsLoaded;
        public static bool IsHuman(this BasePlayer a) => a.userID.IsSteamId();
        public static bool IsCheating(this BasePlayer a) => a._limitedNetworking || a.IsFlying || a.UsedAdminCheat(30f) || a.IsGod() || a.metabolism?.calories?.min == 500;
        public static void SetAiming(this BasePlayer a, bool f) { a.modelState.aiming = f; a.SendNetworkUpdate(); }
        public static void SafelyStrip(this PlayerInventory inv) { if (inv == null) return; inv.containerMain?.Clear(); inv.containerWear?.Clear(); inv.containerBelt?.Clear(); ItemManager.DoRemoves(); }
        public static void SafelyRemove(this ItemContainer inv, string shortname) { if (inv == null) return; Item item = inv.FindItemByItemName(shortname); if (item == null) return; item.RemoveFromContainer(); item.Remove(); }
        public static BasePlayer Player(this IPlayer user) => user?.Object as BasePlayer;
        public static string MaterialName(this Collider collider) { try { return collider.sharedMaterial.name; } catch { return string.Empty; } }
        public static string ObjectName(this Collider collider) { try { return collider.gameObject?.name ?? string.Empty; } catch { return string.Empty; } }
        public static Vector3 GetPosition(this Collider collider) { try { return collider.transform?.position ?? Vector3.zero; } catch { return Vector3.zero; } }
        public static string ObjectName(this BaseEntity entity) { try { return entity.name ?? string.Empty; } catch { return string.Empty; } }
        public static T GetRandom<T>(this HashSet<T> h) { if (h == null || h.Count == 0) { return default(T); } return h.ElementAt(UnityEngine.Random.Range(0, h.Count)); }
        public static float Distance(this Vector3 a, Vector3 b) => (a - b).magnitude;
        public static float Distance2D(this Vector3 a, Vector3 b) => (a.WithY(0f) - b.WithY(0f)).magnitude;
        public static bool IsMajorityDamage(this HitInfo hitInfo, DamageType damageType) => hitInfo?.damageTypes?.GetMajorityDamageType() == damageType;
        public static void ResetToPool<K, V>(this Dictionary<K, V> collection) { if (collection == null) return; collection.Clear(); Pool.Free(ref collection); }
        public static void ResetToPool<T>(this HashSet<T> collection) { if (collection == null) return; collection.Clear(); Pool.Free(ref collection); }
        public static void ResetToPool<T>(this List<T> collection) { if (collection == null) return; collection.Clear(); Pool.Free(ref collection); }
    }
}
