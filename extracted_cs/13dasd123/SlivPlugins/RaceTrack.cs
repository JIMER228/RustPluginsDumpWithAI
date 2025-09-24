using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using System.Globalization;
using System.Reflection;
using Rust;

namespace Oxide.Plugins
{
    using Extensions;

    [Info("RaceTrack", "k1lly0u", "0.2.24")]
    [Description("A standalone racing event for cars, boats, mincopters and horses")]
    class RaceTrack : RustPlugin
    {
        #region Fields        
        [PluginReference] Plugin CarCommander, BoatCommander, HeliCommander, Economics, ServerRewards;

        private Hash<string, TrackData> trackData;
        private Hash<string, ModularCarData> carData;
        private RestoreData restoreData;
        private List<ulong> spawnedEntities;
        private DynamicConfigFile entitydata, trackdata, cardata, restorationData;
        
        private Timer autoTimer;

        private int lastEventIndex = 0;

        private Hash<string, TrackData> raceTracks = new Hash<string, TrackData>();
        private Hash<ulong, TrackData> trackCreator = new Hash<ulong, TrackData>();

        private CuiElementContainer scoreContainer;

        private LightType lightType;

        private static RaceTrack Instance;
        private static RaceManager Current;

        private const string SEDAN_PREFAB = "assets/content/vehicles/sedan_a/sedantest.entity.prefab";
        private const string ROWBOAT_PREFAB = "assets/content/vehicles/boats/rowboat/rowboat.prefab";
        private const string RHIB_PREFAB = "assets/content/vehicles/boats/rhib/rhib.prefab";
        private const string MINICOPTER_PREFAB = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
        private const string HORSE_PREFAB = "assets/rust.ai/nextai/testridablehorse.prefab";
        private const string BOOGIEBOARD_PREFAB = "assets/prefabs/misc/summer_dlc/boogie_board/boogieboard.deployed.prefab";
        private const string INNER_TUBE_PREFAB = "assets/prefabs/misc/summer_dlc/inner_tube/innertube.deployed.prefab";
        private const string SUBMARINE_PREFAB = "assets/content/vehicles/submarine/submarinesolo.entity.prefab";
        private const string SNOWMOBILE_PREFAB = "assets/content/vehicles/snowmobiles/snowmobile.prefab";
        private const string KAYAK_PREFAB = "assets/content/vehicles/boats/kayak/kayak.prefab";
        private const string KAYAK_PADDLE_ITEM = "paddle";

        private const string LANTERN_PREFAB = "assets/prefabs/deployable/lantern/lantern.deployed.prefab";
        private const string FLASHER_PREFAB = "assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab";
        private const string SIREN_PREFAB = "assets/prefabs/deployable/playerioents/lights/sirenlight/electric.sirenlight.deployed.prefab";
        private const string WALL_PREFAB = "assets/prefabs/building core/wall.low/wall.low.prefab";

        private enum RaceMode { Laps, Sprint }

        private enum RaceType { Car, Boat, RHIB, Minicopter, Horse, Kayak, BoogieBoard, InnerTube, Submarine, Snowmobile, Foot }

        private enum EventStatus { Open, Loading, Prestart, Started, Finishing, Finished }

        private enum LightType { Lantern, Flasher, Siren }
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            permission.RegisterPermission("racetrack.admin", this);
            permission.RegisterPermission("racetrack.play", this);

            lang.RegisterMessages(Messages, this);

            entitydata = Interface.Oxide.DataFileSystem.GetFile("RaceTrack/entity_data");
            trackdata = Interface.Oxide.DataFileSystem.GetFile("RaceTrack/track_data");
            cardata = Interface.Oxide.DataFileSystem.GetFile("RaceTrack/car_data");
            restorationData = Interface.Oxide.DataFileSystem.GetFile("RaceTrack/restoration_data");

            trackdata.Settings.Converters = new JsonConverter[] { new UnityVector3Converter() };
            restorationData.Settings.Converters = new JsonConverter[] { new UnityVector3Converter() };

            LoadData();
        }

        private void OnServerInitialized()
        {
            Instance = this;

            if (Configuration.DisableSpectate)            
                Unsubscribe(nameof(CanSpectateTarget));            

            if (!Configuration.Racers.DisableMetabolism)
                Unsubscribe(nameof(OnRunPlayerMetabolism));
            
            Instance.Unsubscribe(nameof(OnEngineStart));

            lightType = ParseType<LightType>(Configuration.Checkpoints.LightType);
            
            CleanupEntities();
            StartEventTimer();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsSleeping() || player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(1, () => OnPlayerConnected(player));
                return;
            }

            if (restoreData.HasRestoreData(player.userID))
                restoreData.RestorePlayer(player);
        }

        private void OnPlayerRespawned(BasePlayer player) => OnPlayerConnected(player);

        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (Current)
                Current.PlayerDisconnected(player);
            else
            {
                RaceDriver raceDriver = player.GetComponent<RaceDriver>();
                if (raceDriver)
                {                   
                    raceDriver.DismountPlayer();
                    NextTick(() =>
                    {
                        if (raceDriver)
                            UnityEngine.Object.Destroy(raceDriver);
                    });                    
                }
            }
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {            
            RaceDriver raceDriver = player.GetComponent<RaceDriver>();
            if (raceDriver)
            {
                if (!player.isMounted)
                {
                    if (raceDriver.Vehicle && raceDriver.Vehicle.GetEntity())
                        raceDriver.MountPlayer(raceDriver.Vehicle);
                    
                    UpdatePosition(raceDriver);
                }
            }
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!entity || info == null)
                return;

            if (entity.GetComponent<RaceDriver>())
            {
                NullifyDamage(ref info);
                return;
            }

            if (entity.GetComponent<RaceVehicle>())
            {
                if (Current.AllowDamage && entity is ModularCar)
                {
                    HandleModularVehicleDamage(entity, info);
                    return;
                }

                NullifyDamage(ref info);
                return;
            }

            BaseVehicleModule module = entity as BaseVehicleModule;
            if (module && module.Vehicle?.GetComponent<RaceVehicle>())
            {
                if (Current.AllowDamage)
                {
                    HandleModularVehicleDamage(entity, info);
                    return;
                }

                NullifyDamage(ref info);
                return;
            }

            CheckPoint.Marker marker = entity.GetComponent<CheckPoint.Marker>();
            if (marker && Current && Current.Status != EventStatus.Finished)
                NullifyDamage(ref info);
        }

        private void HandleModularVehicleDamage(BaseCombatEntity entity, HitInfo info)
        {
            BaseModularVehicle baseModularVehicle = entity as BaseModularVehicle ?? (entity as BaseVehicleModule)?.Vehicle;
            if (baseModularVehicle)
            {
                RaceDriver raceDriver = entity.GetComponent<RaceVehicle>()?.Driver;

                if (raceDriver)
                {
                    if (info.damageTypes.GetMajorityDamageType() != DamageType.Collision)
                    {
                        NullifyDamage(ref info);
                        return;
                    }

                    info.damageTypes.Scale(DamageType.Collision, Current.TrackData.modularDamageMulti);

                    if (info.damageTypes.Total() >= baseModularVehicle.health)
                    {
                        Current.FailedRace(raceDriver);
                    }
                }
            }
        }

        private object OnRunPlayerMetabolism(PlayerMetabolism metabolism, BasePlayer owner, float delta) => owner.GetComponent<RaceDriver>() ? (object)true : null;
        

        private object CanMountEntity(BasePlayer player, BaseMountable mountable) => mountable.GetComponent<RaceModularCar>() ? (object)true : null;

        private object CanDismountEntity(BasePlayer player, BaseMountable mountable)
        {
            RaceDriver raceDriver = player.GetComponent<RaceDriver>();
            if (raceDriver && !raceDriver.HasFinished)            
                return false;            
            return null;
        }

        private object OnEngineStart(MiniCopter miniCopter, BasePlayer player)
        {
            if (!Current || Current.Status != EventStatus.Prestart)
                return null;
            
            RaceHelicopter raceHelicopter = miniCopter.GetComponent<RaceHelicopter>();
            if (raceHelicopter)
                return false;

            return null;
        }

        private object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (player && player.GetComponent<RaceDriver>())
            {
                if (Configuration.CommandBlacklist.Any(x => x.StartsWith("/") ? x.Substring(1).ToLower() == command : x.ToLower() == command))
                {
                    SendReply(player, msg("blacklistcmd", player.UserIDString));
                    return false;
                }
            }
            return null;
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            
            if (player && player.GetComponent<RaceDriver>())
            {
                if (Configuration.CommandBlacklist.Any(x => arg.cmd.FullName.Contains(x.ToLower())) || arg.cmd.FullName == "vehicle.swapseats")
                {
                    SendReply(player, msg("blacklistcmd", player.UserIDString));
                    return false;
                }
            }

            return null;
        }

        private object CanSpectateTarget(BasePlayer player, string name)
        {
            RaceDriver raceDriver = player.GetComponent<RaceDriver>();
            if (raceDriver && raceDriver.Player.IsSpectating())
            {
                raceDriver.UpdateSpectateTarget(1);
                return false;
            }

            return null;
        }

        private object OnSamSiteTarget(SamSite samSite, BaseCombatEntity baseCombatEntity) => baseCombatEntity.GetComponent<RaceVehicle>() ? (object)true : null;
                
        private void Unload()
        {
            SaveRestoreData();

            if (autoTimer != null)
                autoTimer.Destroy();

            RaceDriver[] raceDrivers = UnityEngine.Object.FindObjectsOfType<RaceDriver>();
            if (raceDrivers != null)
            {
                for (int i = 0; i < raceDrivers.Length; i++)
                {
                    UnityEngine.Object.Destroy(raceDrivers[i]);
                }
            }

            RaceVehicle[] raceVehicles = UnityEngine.Object.FindObjectsOfType<RaceVehicle>();
            if (raceVehicles != null)
            {
                for (int i = 0; i < raceVehicles.Length; i++)
                {
                    UnityEngine.Object.Destroy(raceVehicles[i]);
                }
            }

            CheckPoint[] checkPoints = UnityEngine.Object.FindObjectsOfType<CheckPoint>();
            if (checkPoints != null)
            {
                for (int i = 0; i < checkPoints.Length; i++)
                {
                    UnityEngine.Object.Destroy(checkPoints[i]);
                }
            }

            CheckPoint.Marker[] markers = UnityEngine.Object.FindObjectsOfType<CheckPoint.Marker>();
            if (markers != null)
            {
                for (int i = 0; i < markers.Length; i++)
                {
                    UnityEngine.Object.Destroy(markers[i]);
                }
            }

            UnityEngine.Object.Destroy(Current);

            Current = null;
            Instance = null;
            Configuration = null;
        }
        #endregion

        #region Functions        
        private void CleanupEntities()
        {            
            if (spawnedEntities.Count > 0)
            {
                PrintWarning("Finding and destroying left over entities");

                foreach (BaseNetworkable obj in BaseNetworkable.serverEntities)
                {
                    if (obj && !obj.IsDestroyed)
                    {
                        if (spawnedEntities.Contains(obj.net.ID.Value))
                            obj.Kill(BaseNetworkable.DestroyMode.None);
                    }
                }

                spawnedEntities.Clear();
                SaveEntityData();
                PrintWarning("Cleanup completed");
            }            
        }   
                
        private T ParseType<T>(string type)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), type, true);
            }
            catch
            {
                return default(T);
            }
        }

        private static string FormatTime(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds((float)time);

            int hours = dateDifference.Hours + (dateDifference.Days * 24);
           
            if (hours > 0)
                return string.Format("{0:00}:{1:00}:{2:00}", hours, dateDifference.Minutes, dateDifference.Seconds);
            else return string.Format("{0:00}:{1:00}", dateDifference.Minutes, dateDifference.Seconds);
        }

        private static bool IsWaterSurfaceBasedRace(RaceType raceType) => raceType == RaceType.Boat || raceType == RaceType.BoogieBoard || raceType == RaceType.InnerTube || raceType == RaceType.Kayak || raceType == RaceType.RHIB;
        
        private static string EntityFromRaceType(RaceType raceType)
        {
            switch (raceType)
            {
                case RaceType.Car:
                    return SEDAN_PREFAB;
                case RaceType.Boat:
                    return ROWBOAT_PREFAB;
                case RaceType.RHIB:
                    return RHIB_PREFAB;
                case RaceType.Minicopter:
                    return MINICOPTER_PREFAB;
                case RaceType.Horse:
                    return HORSE_PREFAB;
                case RaceType.Kayak:
                    return KAYAK_PREFAB;
                case RaceType.BoogieBoard:
                    return BOOGIEBOARD_PREFAB;
                case RaceType.InnerTube:
                    return INNER_TUBE_PREFAB;
                case RaceType.Submarine:
                    return SUBMARINE_PREFAB;
                case RaceType.Snowmobile:
                    return SNOWMOBILE_PREFAB;
                default:
                    return string.Empty;
            }
        }

        private static Type VehicleComponentFromRaceType(RaceType raceType)
        {
            switch (raceType)
            {
                case RaceType.Car:
                    return typeof(RaceModularCar);
                case RaceType.Boat:
                case RaceType.RHIB:
                    return typeof(RaceBoat);
                case RaceType.Kayak:
                    return typeof(RaceKayak);
                case RaceType.BoogieBoard:
                case RaceType.InnerTube:
                    return typeof(RaceInflatable);
                case RaceType.Minicopter:
                    return typeof(RaceHelicopter);
                case RaceType.Horse:
                    return typeof(RaceHorse);
                case RaceType.Submarine:
                    return typeof(RaceSubmarine);
                case RaceType.Snowmobile:
                    return typeof(RaceSnowmobile);
                default:
                    return null;
            }
        }

        private void NullifyDamage(ref HitInfo info)
        {
            info.damageTypes = new DamageTypeList();
            info.HitEntity = null;
            info.HitMaterial = 0;
            info.PointStart = Vector3.zero;
        }

        private void StartEventTimer()
        {
            if (Configuration.Automation.Enabled)
            {
                if (trackData.Count == 0)
                {
                    PrintError("You have no race tracks set up. Unable to automate events");
                    return;
                }

                autoTimer = timer.In(Configuration.Timers.Interval, () =>
                {
                    if (Configuration.Automation.MinimumPlayers > 0 && BasePlayer.activePlayerList.Count < Configuration.Automation.MinimumPlayers)
                    {
                        StartEventTimer();
                        return;
                    }

                    if (Configuration.Automation.Time.Enabled)
                    {
                        float currentTime = TOD_Sky.Instance.Cycle.Hour;

                        if (Configuration.Automation.Time.Start > Configuration.Automation.Time.End)
                        {
                            if (currentTime > Configuration.Automation.Time.Start || currentTime < Configuration.Automation.Time.End)
                            {
                                StartEventTimer();
                                return;
                            }
                        }
                        else 
                        {
                            if (currentTime > Configuration.Automation.Time.Start && currentTime < Configuration.Automation.Time.End)
                            {
                                StartEventTimer();
                                return;
                            }
                        }
                    }

                    if (!Current)
                        Current = new GameObject().AddComponent<RaceManager>();

                    if (Current.Status != EventStatus.Finished)
                        return;

                    if (Configuration.Automation.Random)
                    {
                        KeyValuePair<string, TrackData> data = trackData.ElementAt(UnityEngine.Random.Range(0, trackData.Count));
                        Current.SetTrackData(data.Key, data.Value);
                        Current.OpenEvent();
                    }
                    else
                    {
                        if (Configuration.Automation.Order.Count < 1)
                        {
                            PrintError("You have no race tracks set in your config. Unable to automate events by order");
                            return;
                        }
                        if (!trackData.ContainsKey(Configuration.Automation.Order[lastEventIndex]))
                        {
                            PrintError($"Unable to find a race track with the name : {Configuration.Automation.Order[lastEventIndex]}");
                            return;
                        }

                        TrackData data = trackData[Configuration.Automation.Order[lastEventIndex]];
                        Current.SetTrackData(Configuration.Automation.Order[lastEventIndex], data);

                        lastEventIndex++;

                        if (lastEventIndex >= Configuration.Automation.Order.Count)
                            lastEventIndex = 0;
                        Current.OpenEvent();
                    }
                });
            }
        }

        private void BroadcastEvent()
        {
            if (!Configuration.Entrance.Enabled)
                BroadcastToChat(string.Format(msg("eventopen1"), Current.RaceType));
            else
            {
                if (Configuration.Entrance.ServerRewards)
                    BroadcastToChat(string.Format(msg("eventopenfee1"), Configuration.Entrance.Amount, msg("serverrewards"), Current.RaceType));

                if (Configuration.Entrance.Economics)
                    BroadcastToChat(string.Format(msg("eventopenfee1"), Configuration.Entrance.Amount, msg("economics"), Current.RaceType));

                if (Configuration.Entrance.Scrap)
                    BroadcastToChat(string.Format(msg("eventopenfee1"), Configuration.Entrance.Amount, msg("scrap"), Current.RaceType));
            }

            if (Configuration.Rewards.Enabled)
            {
                string rewards = Configuration.Rewards.ServerRewards ? msg("serverrewards") : Configuration.Rewards.Scrap ? msg("scrap") : msg("economics");

                if (Configuration.Rewards.Podium)
                    BroadcastToChat(string.Format(msg("eventprizepodium"), Configuration.Rewards.Prize1, rewards, Configuration.Rewards.Prize2, Configuration.Rewards.Prize3));
                else BroadcastToChat(string.Format(msg("eventprize"), Configuration.Rewards.Prize1, rewards));
            }
        }

        private void OnVehicleUnderwater(BasicCar car)
        {
            RaceModularCar raceCar = car.GetComponent<RaceModularCar>();
            if (raceCar)
            {
                raceCar.ResetVehicle();
                Instance.CarCommander.Call("ToggleController", car, true);
            }
        }

        private static void DisableTerrainAntiHack()
        {
            int protection = ConVar.AntiHack.terrain_protection;
            ConVar.AntiHack.terrain_protection = 0;

            Instance.timer.In(3f, () => ConVar.AntiHack.terrain_protection = protection);
        }

        private static void BroadcastToChat(string message)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                player.SendConsoleCommand("chat.add", new object[] { 0, 0, message });
        }

        private static string ToOrdinal(int i) => (i + "th") .Replace("1th", "1st") .Replace("2th", "2nd") .Replace("3th", "3rd");

        private object IsEventPlayer(BasePlayer player) => player.GetComponent<RaceDriver>() ? (object)true : null;
        private object isEventPlayer(BasePlayer player) => player.GetComponent<RaceDriver>() ? (object)true : null;

        #endregion

        #region Component
        private class RaceManager : MonoBehaviour
        {
            private List<BasePlayer> joiners;

            private List<RaceDriver> raceDrivers;
            private List<RaceVehicle> raceVehicles;
            private List<CheckPoint> checkPoints;
            private List<RaceDriver> positions;
            internal List<RaceDriver> spectateTargets;

            private RaceScores[] winners;

            private int playersFinished;
            private int playersFailed;
            private int totalRacers;
            private int maxRacers;
            private int countdown = 10;
            private int timerTick;
            private double startTime;
            private double endTime;

            private bool hasStarted;
            private bool isEnding;
            private bool isStarting;
            private bool hasLaunchedFirework;

            #region Properties
            public EventStatus Status { get; private set; }

            public bool HasTrackSet => TrackData != null;
             
            public int RacerCount => raceDrivers.Count;
            
            public int CheckpointCount => TrackData.checkPoints.Count;
             
            public int JoinerCount => joiners.Count;

            public string TrackName { get; private set; }

            public RaceMode RaceMode => TrackData.raceMode;
            
            public RaceType RaceType => TrackData.raceType;

            public bool AllowDamage => TrackData.allowVehicleDamage && TrackData.UseModularCars;

            public TrackData TrackData { get; private set; }

            public List<CheckPoint> Checkpoints => checkPoints;

            public int Laps => TrackData.laps;
             
            public double StartTime => startTime;
            
            public double CurrentTime => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

            public bool CanReset { get; private set; }
            #endregion

            private void Awake()
            {
                joiners = Facepunch.Pool.GetList<BasePlayer>();
                raceDrivers = Facepunch.Pool.GetList<RaceDriver>();
                raceVehicles = Facepunch.Pool.GetList<RaceVehicle>();
                checkPoints = Facepunch.Pool.GetList<CheckPoint>();
                spectateTargets = Facepunch.Pool.GetList<RaceDriver>();
                positions = Facepunch.Pool.GetList<RaceDriver>();

                countdown = Configuration.Timers.Countdown;

                Status = EventStatus.Finished;
                enabled = false;
            }

            private void OnDestroy()
            {
                DestroyEvent();
                Instance?.StartEventTimer();
            }

            #region Event Management
            public void SetTrackData(string trackName, TrackData trackData)
            {
                this.TrackName = trackName;
                this.TrackData = trackData;

                maxRacers = trackData.gridPoints.Count;
            }

            public void OpenEvent()
            {
                if (Instance.autoTimer != null)
                    Instance.autoTimer.Destroy();

                isStarting = false;
                isEnding = false;
                hasStarted = false;
                CanReset = false;

                Status = EventStatus.Loading;

                Instance.scoreContainer = null;

                ServerMgr.Instance.StartCoroutine(CreateCheckPoints());
            }

            public void StartEvent()
            {
                if (TrackData.raceType == RaceType.Minicopter)
                    Instance.Subscribe("OnEngineStart");
                
                isStarting = false;
                hasStarted = true;

                Status = EventStatus.Prestart;
                
                InvokeHandler.CancelInvoke(this, StartEvent);
                InvokeHandler.CancelInvoke(this, EndEvent);

                for (int i = 0; i < joiners.Count; i++)
                {
                    BasePlayer joiner = joiners[i];
                    if (!joiner || joiner.IsDead())
                        continue;

                    SpawnPlayer(joiner);
                }
                
                joiners.Clear();

                DisplayInformation(raceDrivers, TrackName, TrackData);
                Countdown();

                winners = new RaceScores[raceDrivers.Count];
                playersFinished = 0;
                totalRacers = raceDrivers.Count;
            }

            public void EndEvent()
            {        
                Instance.Unsubscribe("OnEngineStart");
                
                InvokeHandler.CancelInvoke(this, Countdown);
                InvokeHandler.CancelInvoke(this, CalculateScores);

                raceDrivers.ForEach((RaceDriver driver) =>
                {
                    driver.UpdateTravelDistance();
                    CuiHelper.DestroyUi(driver.Player, UITime);
                });

                if (Status == EventStatus.Open)
                {
                    Status = EventStatus.Finished;
                    Instance.IssueRefunds(joiners);
                    joiners.Clear();
                    BroadcastToChat(msg("cancelled"));
                    Destroy(this);
                }
                else FinishRace();                
            }

            public void JoinEvent(BasePlayer player)
            {
                if (Status != EventStatus.Open)
                {
                    player.ChatMessage(Status == EventStatus.Finished ? msg("noevent", player.UserIDString) : msg("nojoin", player.UserIDString));
                    return;
                }

                if (joiners.Contains(player))
                {
                    player.ChatMessage(msg("inevent", player.UserIDString));
                    return;
                }

                if (joiners.Count >= maxRacers)
                {
                    player.ChatMessage(msg("eventfull", player.UserIDString));
                    return;
                }

                joiners.Add(player);
                player.ChatMessage(msg("joinevent", player.UserIDString));
                BroadcastToChat(string.Format(msg("joinevent.player"), player.displayName));

                if (joiners.Count >= TrackData.minPlayers && !isStarting)
                {
                    isStarting = true;
                    InvokeHandler.CancelInvoke(this, EndEvent);
                    BroadcastToChat(string.Format(msg("minplayers.reached"), Configuration.Timers.StartTime));
                    InvokeHandler.Invoke(this, StartEvent, Configuration.Timers.StartTime);
                }
            }

            public void LeaveEvent(BasePlayer player, bool hasFinished)
            {
                if (Status == EventStatus.Open)
                {
                    if (joiners.Contains(player))
                    {
                        Instance.IssueRefund(player);

                        joiners.Remove(player);
                        if (joiners.Count < TrackData.minPlayers)
                        {
                            isStarting = false;
                            InvokeHandler.CancelInvoke(this, EndEvent);
                            InvokeHandler.CancelInvoke(this, StartEvent);
                            InvokeHandler.Invoke(this, EndEvent, Configuration.Timers.CloseTime);

                            BroadcastToChat(string.Format(msg("leftevent.wait"), player.displayName));
                        }
                        else BroadcastToChat(string.Format(msg("leftevent"), player.displayName));
                    }
                }
                else if (Status == EventStatus.Prestart || Status == EventStatus.Started)
                {
                    RaceDriver raceDriver = player.GetComponent<RaceDriver>();
                    if (raceDriver)
                    {
                        raceDrivers.Remove(raceDriver);
                        spectateTargets.Remove(raceDriver);
                        positions.Remove(raceDriver);

                        raceDriver.UpdateTravelDistance();

                        if (!Configuration.DisableSpectate)
                        {
                            DisableTerrainAntiHack();

                            raceDrivers.ForEach((RaceDriver spectatorDriver) =>
                            {
                                if (spectatorDriver.Player.IsSpectating())
                                {
                                    if (raceDrivers.Count == 0)
                                        spectatorDriver.FinishSpectating();
                                    else
                                    {
                                        if (raceDriver.SpectateTarget == raceDriver)
                                            raceDriver.UpdateSpectateTarget();
                                    }
                                }
                            });
                        }

                        winners[raceDrivers.Count] = new RaceScores(raceDriver, CurrentTime - StartTime, raceDriver.LapNumber - 1, raceDriver.TotalDistanceTravelled);
                        winners[raceDrivers.Count].hasFinished = false;

                        Destroy(raceDriver);

                        if (!hasFinished)
                            BroadcastToChat(string.Format(msg("leftevent"), player.displayName));

                        totalRacers -= 1;

                        if (raceDrivers.Count == 0)
                            EndEvent();
                    }
                }
                else player.ChatMessage(msg("noevent", player.UserIDString));
            }       

            private void DestroyEvent()
            {
                isStarting = false;
                isEnding = false;
                CanReset = false;

                joiners.Clear();

                for (int i = raceDrivers.Count - 1; i >= 0; i--)
                    Destroy(raceDrivers[i]);                    
                
                raceDrivers.Clear();

                for (int i = raceVehicles.Count - 1; i >= 0; i--)                
                    Destroy(raceVehicles[i]);
                
                raceVehicles.Clear();

                for (int i = checkPoints.Count - 1; i >= 0; i--)                
                    Destroy(checkPoints[i]);
                
                checkPoints.Clear();

                Facepunch.Pool.FreeList(ref joiners);
                Facepunch.Pool.FreeList(ref raceDrivers);
                Facepunch.Pool.FreeList(ref spectateTargets);
                Facepunch.Pool.FreeList(ref raceVehicles);
                Facepunch.Pool.FreeList(ref checkPoints);
                Facepunch.Pool.FreeList(ref positions);
                
                InvokeHandler.CancelInvoke(this, OpenEvent);
                InvokeHandler.CancelInvoke(this, StartEvent);
                InvokeHandler.CancelInvoke(this, EndEvent);
                InvokeHandler.CancelInvoke(this, CalculateScores);
                InvokeHandler.CancelInvoke(this, Countdown);
                InvokeHandler.CancelInvoke(this, DestroyCountdown);
                InvokeHandler.CancelInvoke(this, FinishRace);                
            }
           
            private IEnumerator DestroyCheckpoints()
            {
                for (int i = checkPoints.Count - 1; i >= 0; i--)
                {
                    Destroy(checkPoints[i]);
                    yield return new WaitForSeconds(0.5f);
                }

                checkPoints.Clear();

                Destroy(this, 15f);
            }

            #endregion

            #region Player Management
            private void SpawnPlayer(BasePlayer player)
            {
                if (!player)
                    return;

                if (player.isMounted)
                {
                    player.GetMounted().DismountPlayer(player, true);
                    player.DismountObject();
                }

                if (player.IsSleeping())
                    player.EndSleeping();

                player.inventory.crafting.CancelAll(true);

                Instance.restoreData.AddData(player);

                player.inventory.Strip();

                Instance.NextTick(() =>
                {
                    player.inventory.containerWear.SetFlag(ItemContainer.Flag.IsLocked, true);

                    string[] clothing = Configuration.Racers.Clothing.GetRandom();
                    for (int i = 0; i < clothing.Length; i++)
                    {
                        Item item = ItemManager.CreateByName(clothing[i]);
                        if (item != null)
                        {
                            if (!item.MoveToContainer(player.inventory.containerWear))
                                item.Remove(0f);
                        }
                    }
                });

                player.metabolism.Reset();
                player.health = 100f;

                int gridId = raceDrivers.Count;

                RaceDriver raceDriver = player.gameObject.AddComponent<RaceDriver>(); 
                raceDrivers.Add(raceDriver);
                spectateTargets.Add(raceDriver);
                positions.Add(raceDriver);
                        
                TrackData.PointInfo gridPoint = TrackData.gridPoints[gridId];

                if (TrackData.raceType == RaceType.Foot)
                {
                    raceDriver.gridId = gridId;
                    raceDriver.Manager = this;
                    raceDriver.Position = gridId + 1;
                    
                    if (player.isMounted)
                    {
                        player.GetMounted()?.DismountPlayer(player);
                        Instance.NextTick(() =>
                        {
                            MovePosition(player, gridPoint.position, true);
                            player.OverrideViewAngles(Quaternion.Euler(0, gridPoint.rotation, 0) * Vector3.forward);
                        });
                        return;
                    }
                    
                    MovePosition(player, gridPoint.position, true);
                    player.OverrideViewAngles(Quaternion.Euler(0, gridPoint.rotation, 0) * Vector3.forward);

                    raceDriver.freezePosition = gridPoint.position;
                }
                else
                {
                    BaseEntity baseEntity = CreateEntity(RaceType, gridPoint.position, gridPoint.rotation, TrackData.useCommander, TrackData.UseModularCars);

                    RaceVehicle raceVehicle = RaceType == RaceType.Car && !TrackData.UseModularCars ? baseEntity.gameObject.AddComponent<RaceSedan>() : baseEntity.gameObject.AddComponent(VehicleComponentFromRaceType(RaceType)) as RaceVehicle;

                    Instance.spawnedEntities.Add(baseEntity.net.ID.Value);

                    raceVehicles.Add(raceVehicle);

                    raceVehicle.ToggleKinematic(true);

                    if (RaceType == RaceType.Horse)
                        (baseEntity as RidableHorse).SetBreed(TrackData.horseBreed);

                    raceVehicle.Driver = raceDriver;
                    raceVehicle.Manager = this;
                    
                    raceDriver.gridId = gridId;
                    raceDriver.Vehicle = raceVehicle;
                    raceDriver.Manager = this;
                    raceDriver.Position = gridId + 1;
                    raceDriver.enabled = false;

                    if (player.isMounted)
                    {
                        player.GetMounted()?.DismountPlayer(player);
                        Instance.NextTick(() => MovePosition(player, raceVehicle.transform.position + raceVehicle.transform.up, true));
                        return;
                    }
                    
                    MovePosition(player, raceVehicle.transform.position + raceVehicle.transform.up, true);
                }
            }
            
            public void PlayerDisconnected(BasePlayer player)
            {
                if (joiners.Contains(player))
                {
                    joiners.Remove(player);
                    if (joiners.Count < TrackData.minPlayers)
                    {
                        isStarting = false;
                        InvokeHandler.CancelInvoke(this, EndEvent);
                        InvokeHandler.CancelInvoke(this, StartEvent);
                        InvokeHandler.Invoke(this, EndEvent, Configuration.Timers.CloseTime);

                        BroadcastToChat(string.Format(msg("leftevent.wait"), player.displayName));
                    }
                    else BroadcastToChat(string.Format(msg("leftevent"), player.displayName));
                }

                RaceDriver raceDriver = player.GetComponent<RaceDriver>();
                if (raceDriver)
                {
                    raceDrivers.Remove(raceDriver);
                    spectateTargets.Remove(raceDriver);
                    positions.Remove(raceDriver);

                    if (!Configuration.DisableSpectate)
                    {
                        DisableTerrainAntiHack();

                        raceDrivers.ForEach((RaceDriver spectatorDriver) =>
                        {
                            if (spectatorDriver.Player.IsSpectating())
                            {
                                if (raceDrivers.Count == 0)
                                    spectatorDriver.FinishSpectating();
                                else
                                {
                                    if (raceDriver.SpectateTarget == raceDriver)
                                        raceDriver.UpdateSpectateTarget();
                                }
                            }
                        });
                    }

                    winners[raceDrivers.Count] = new RaceScores(raceDriver, CurrentTime - StartTime, raceDriver.LapNumber - 1, raceDriver.TotalDistanceTravelled);
                    winners[raceDrivers.Count].hasFinished = false;

                    raceDriver.DismountPlayer();
                    Destroy(raceDriver);

                    totalRacers -= 1;

                    if (raceDrivers.Count == 0)
                        EndEvent();
                }
            }

            private BaseEntity CreateEntity(RaceType raceType, Vector3 position, float rotation, bool useCommander, bool useModularCars)
            {
                if (IsWaterSurfaceBasedRace(raceType))                
                    position.y = TerrainMeta.WaterMap.GetHeight(position);
                
                if (useModularCars)                
                    return SpawnModularCar(position, rotation);

                if (raceType == RaceType.Car && Instance.CarCommander && useCommander)
                    return SpawnCarCommander(position, rotation);

                if (raceType == RaceType.Boat && Instance.BoatCommander && useCommander)
                    return SpawnBoatCommander(position, rotation);

                if (raceType == RaceType.Minicopter && Instance.HeliCommander && useCommander)
                    return SpawnHeliCommander(position, rotation);

                string prefab = EntityFromRaceType(raceType);
                
                BaseEntity baseEntity = GameManager.server.CreateEntity(prefab, position + (Vector3.up * 0.2f), Quaternion.Euler(0, rotation, 0));
                baseEntity.enableSaving = false;
                baseEntity.Spawn();

                baseEntity.enabled = false;

                return baseEntity;
            }

            private BaseEntity SpawnCarCommander(Vector3 position, float rotation)
            {
                BaseEntity baseEntity = Instance.CarCommander.Call("SpawnAtLocation", position + (Vector3.up * 0.2f), Quaternion.Euler(0, rotation, 0), false, true) as BasicCar;
                Instance.CarCommander.Call("ToggleController", baseEntity as BasicCar, false);
                return baseEntity;
            }

            private BaseEntity SpawnBoatCommander(Vector3 position, float rotation)
            {
                BaseEntity baseEntity = Instance.BoatCommander.Call("SpawnAtLocation", position + (Vector3.up * 0.2f), Quaternion.Euler(0, rotation, 0), false, true, true, false, false, false, RaceType == RaceType.RHIB) as MotorRowboat;
                Instance.BoatCommander.Call("ToggleController", baseEntity as MotorRowboat, false);
                return baseEntity;
            }

            private BaseEntity SpawnHeliCommander(Vector3 position, float rotation)
            {
                BaseEntity baseEntity = Instance.HeliCommander.Call("SpawnAtLocation", position + (Vector3.up * 0.2f), Quaternion.Euler(0, rotation, 0), "Mini", false, 0UL, true) as MiniCopter;
                Instance.HeliCommander.Call("ToggleController", baseEntity, false);
                return baseEntity;
            }

            private BaseEntity SpawnModularCar(Vector3 position, float rotation)
            {
                ModularCarData modularCarData = Instance.carData[TrackData.modularCarProfiles.GetRandom()];
                ModularCar modularCar = modularCarData.LoadVehicle(position + (Vector3.up * 0.2f), Quaternion.Euler(0, rotation, 0));
                modularCar.SetFlag(BaseEntity.Flags.Reserved1, true);
                modularCar.SetFlag(BaseEntity.Flags.On, false);
                
                modularCar.SetHealth(200);
                modularCar.SetMaxHealth(200);
                return modularCar;
            }              
            
            #endregion

            #region Checkpoints  
            private IEnumerator CreateCheckPoints()
            {
                print("[RaceTrack] Creating race checkpoints, please wait!");
                enabled = true;
                for (int i = 0; i < CheckpointCount; i++)
                {
                    print($"[RaceTrack] Building Checkpoint {i + 1}/{CheckpointCount}");
                    TrackData.PointInfo pointInfo = TrackData.checkPoints[i];

                    CheckPoint checkPoint = new GameObject().AddComponent<CheckPoint>();
                    checkPoint.transform.position = pointInfo.position;
                    checkPoint.transform.rotation = Quaternion.Euler(0, pointInfo.rotation, 0);

                    if (pointInfo.SegmentDistance < 0f)
                    {
                        if (i == CheckpointCount - 1)
                        {
                            if (TrackData.raceMode == RaceMode.Laps)
                                pointInfo.SegmentDistance = Vector3.Distance(pointInfo.position, TrackData.checkPoints[0].position);
                            else pointInfo.SegmentDistance = 0f;
                        }
                        else pointInfo.SegmentDistance = Vector3.Distance(pointInfo.position, TrackData.checkPoints[i + 1].position);
                    }

                    checkPoint.SetCheckpointData(i, pointInfo.size);
                    checkPoints.Add(checkPoint);

                    yield return new WaitWhile(()=> checkPoint.IsBuilding);
                    yield return new WaitForSeconds(0.5f);
                }

                print("[RaceTrack] All checkpoints have been loaded. Race is now open!");

                Instance.SaveEntityData();
                countdown = Mathf.Max(11, Configuration.Timers.Countdown);
                Status = EventStatus.Open;
                Instance.BroadcastEvent();
                InvokeHandler.Invoke(this, EndEvent, Configuration.Timers.CloseTime);
                enabled = false;
            }

            public TrackData.PointInfo GetCheckpoint(int index)
            {
                if (index < 0)
                    index += CheckpointCount;
                if (index >= CheckpointCount)
                    index -= CheckpointCount;

                return TrackData.checkPoints[Mathf.Clamp(index, 0, CheckpointCount - 1)];
            }
            #endregion

            #region Pre-race Setup
            private void Countdown()
            {
                --countdown;

                UpdateCountdown(raceDrivers, countdown > 0 ? string.Format(msg("countdown.count"), countdown) : msg("countdown.go"));

                if (countdown > 0)
                    InvokeHandler.Invoke(this, Countdown, 1f);
                else
                {
                    if (TrackData.raceType == RaceType.Minicopter)
                        Instance.Unsubscribe("OnEngineStart");
                    
                    Status = EventStatus.Started;

                    startTime = CurrentTime;

                    EnableAllVehicles();
                    InvokeHandler.Invoke(this, DestroyCountdown, 1f);

                    if (TrackData.maxTime > 0)
                    {
                        for (int i = 0; i < raceDrivers.Count; i++)
                        {
                            RaceDriver raceDriver = raceDrivers[i];
                            raceDriver.Player.ChatMessage(string.Format(msg("eventtimer", raceDriver.Player.UserIDString), FormatTime(TrackData.maxTime)));
                        }

                        endTime = UnityEngine.Time.realtimeSinceStartup + TrackData.maxTime;

                        InvokeHandler.Invoke(this, FinishRace, TrackData.maxTime);                        
                    }
                    InvokeHandler.InvokeRepeating(this, UpdateDriverPositions, 1f, 1f);
                }
            }

            private void EnableAllVehicles()
            {
                if (TrackData.raceType == RaceType.Foot)
                {
                    for (int i = 0; i < raceDrivers.Count; i++)
                    {
                        RaceDriver raceDriver = raceDrivers[i];
                        if (raceDriver)
                            raceDriver.enabled = false;
                    }
                    return;
                }
                
                for (int i = 0; i < raceDrivers.Count; i++)
                {
                    RaceDriver raceDriver = raceDrivers[i];
                    RaceVehicle raceVehicle = raceDriver.Vehicle;

                    BaseMountable vehicle = raceVehicle.GetEntity();

                    if (RaceType == RaceType.Car)
                    {
                        if (TrackData.UseModularCars)
                        {
                            vehicle.SetFlag(BaseEntity.Flags.Reserved1, false);
                            vehicle.SetFlag(BaseEntity.Flags.On, true);
                        }
                        else
                        {
                            if (Instance.CarCommander && TrackData.useCommander)
                                Instance.CarCommander.Call("ToggleController", vehicle as BasicCar, true);
                        }
                    }
                    else if (RaceType == RaceType.Boat || RaceType == RaceType.RHIB)
                    {
                        if (Instance.BoatCommander && TrackData.useCommander)
                            Instance.BoatCommander.Call("ToggleController", vehicle as MotorRowboat, true);

                        vehicle.GetComponent<MotorRowboat>().EngineToggle(true);
                    }  
                    else if (RaceType == RaceType.Minicopter)
                    {
                        MiniCopter miniCopter = vehicle as MiniCopter;

                        if (Instance.HeliCommander && TrackData.useCommander)
                            Instance.HeliCommander.Call("ToggleController", miniCopter, true);

                        (raceVehicle as RaceHelicopter).OnRaceStarted();

                        miniCopter.SetFlag(miniCopter.engineController.engineStartingFlag, true, false, true);
                        miniCopter.SetFlag(BaseEntity.Flags.On, false, false, true);
                        miniCopter.Invoke(miniCopter.engineController.FinishStartingEngine, miniCopter.engineController.engineStartupTime);
                    }

                    vehicle.enabled = true;

                    raceVehicle.ToggleKinematic(false);
                    raceVehicle.WakeUp();

                    raceDriver.StartRace();
                }
                CanReset = true;
            }

            private void DestroyCountdown()
            {
                for (int i = 0; i < raceDrivers.Count; i++)
                {
                    RaceDriver raceDriver = raceDrivers[i];

                    raceDriver.DestroyUI(UIInfo);
                    raceDriver.DestroyUI(UITime);

                    if (TrackData.raceMode == RaceMode.Laps)
                        RaceTrack.UpdateLapCounter(raceDriver, true, null);
                    else RaceTrack.UpdateLapCounter(raceDriver, false, null);
                }               
            }
            #endregion

            #region Finished Racers
            public void FailedRace(RaceDriver raceDriver)
            {
                raceDriver.OnDriverFailed();

                spectateTargets.Remove(raceDriver);

                playersFailed++;

                int position = winners.Length - playersFailed;

                winners[position] = new RaceScores(raceDriver, CurrentTime - StartTime, raceDriver.LapNumber - 1, raceDriver.TotalDistanceTravelled);

                if (!Configuration.DisableSpectate)
                {
                    DisableTerrainAntiHack();

                    raceDrivers.ForEach((RaceDriver spectatorDriver) =>
                    {
                        if (spectatorDriver.Player.IsSpectating())
                        {
                            if (playersFinished + playersFailed >= totalRacers)
                                spectatorDriver.FinishSpectating();
                            else
                            {
                                if (spectatorDriver.SpectateTarget == raceDriver)
                                    spectatorDriver.UpdateSpectateTarget();
                            }
                        }
                    });
                }
                
                raceDriver.DismountPlayer();

                string str = string.Format(msg("vehicle.destroyed", raceDriver.Player.UserIDString), ToOrdinal(position + 1));

                raceDriver.Player.ChatMessage(str);

                if (playersFinished + playersFailed >= totalRacers)
                {
                    InvokeHandler.CancelInvoke(this, FinishRace);
                    FinishRace();
                }
                else
                {
                    if (Configuration.DisableSpectate)
                        LeaveEvent(raceDriver.Player, true);
                    else Instance.NextTick(raceDriver.BeginSpectating);
                }

                Instance.NextTick(() => Destroy(raceDriver.Vehicle));
            }

            public void FinishedRace(RaceDriver raceDriver)
            {
                spectateTargets.Remove(raceDriver);
                
                if (!hasLaunchedFirework && Configuration.Checkpoints.LaunchFirework)
                {
                    MortarFirework firework = GameManager.server.CreateEntity(Configuration.Checkpoints.FireworkPrefab, raceDriver.transform.position + (Vector3.up * 3f)) as MortarFirework;
                    if (firework)
                    {
                        firework.enableSaving = false;
                        firework.Spawn();
                        firework.Ignite(firework.transform.position);
                    }
                    hasLaunchedFirework = true;
                }

                winners[playersFinished] = new RaceScores(raceDriver, TrackData);
                playersFinished++;

                Instance.IssueReward(raceDriver.Player.userID, playersFinished, true);

                if (!Configuration.DisableSpectate)
                {
                    DisableTerrainAntiHack();

                    raceDrivers.ForEach((RaceDriver spectatorDriver) =>
                    {
                        if (spectatorDriver.Player.IsSpectating())
                        {
                            if (playersFinished + playersFailed >= totalRacers)
                                spectatorDriver.FinishSpectating();
                            else
                            {
                                if (spectatorDriver.SpectateTarget == raceDriver)
                                    spectatorDriver.UpdateSpectateTarget();
                            }
                        }
                    });
                }
                
                raceDriver.DismountPlayer();                
                    
                if (!isEnding)
                {
                    isEnding = true;

                    int finishTime = Configuration.Timers.EndTime;
                    if (TrackData.maxTime > 0)
                    {
                        double forcedFinishTime = endTime - UnityEngine.Time.realtimeSinceStartup;
                        if (forcedFinishTime < finishTime)
                            finishTime = (int)forcedFinishTime;
                    }

                    for (int i = 0; i < raceDrivers.Count; i++)
                    {
                        RaceDriver driver = raceDrivers[i];
                        if (driver == raceDriver || driver.HasFinished)
                            continue;

                        driver.Player.ChatMessage(string.Format(msg("win.end", driver.Player.UserIDString), raceDriver.Player.displayName, finishTime));
                    }

                    timerTick = finishTime;
                    TimerTick();                    
                }

                string str = string.Format(msg("finish.player", raceDriver.Player.UserIDString), ToOrdinal(playersFinished));

                if ((playersFinished <= 3 && Configuration.Rewards.Podium) || playersFinished == 1)
                {
                    string rewards = Configuration.Rewards.ServerRewards ? msg("serverrewards", raceDriver.Player.UserIDString) : 
                                     Configuration.Rewards.Scrap ? msg("scrap", raceDriver.Player.UserIDString) : 
                                     msg("economics", raceDriver.Player.UserIDString);

                    int amount = playersFinished == 1 ? Configuration.Rewards.Prize1 : playersFinished == 2 ? Configuration.Rewards.Prize2 : Configuration.Rewards.Prize3;

                    str += string.Format(msg("finish.player.prize", raceDriver.Player.UserIDString), $"{amount}x {rewards}");
                }

                raceDriver.Player.ChatMessage(str);

                if (playersFinished + playersFailed >= totalRacers)
                {
                    InvokeHandler.CancelInvoke(this, FinishRace);
                    FinishRace();
                }
                else
                {                    
                    if (Configuration.DisableSpectate)
                        LeaveEvent(raceDriver.Player, true);                    
                    else Instance.NextTick(raceDriver.BeginSpectating);                    
                }

                Instance.NextTick(() => Destroy(raceDriver.Vehicle));
            }

            private void TimerTick()
            {
                timerTick -= 1;

                if (timerTick == 30 || timerTick == 10)
                {
                    int finishTime = timerTick;
                    if (TrackData.maxTime > 0)
                    {
                        double forcedFinishTime = endTime - UnityEngine.Time.realtimeSinceStartup;
                        if (forcedFinishTime < finishTime)
                            finishTime = (int)forcedFinishTime;
                    }

                    for (int i = 0; i < raceDrivers.Count; i++)
                    {
                        RaceDriver driver = raceDrivers[i];
                        driver.Player.ChatMessage(string.Format(msg("win.countdown", driver.Player.UserIDString), finishTime));
                    }
                }

                if (timerTick > 0)
                    InvokeHandler.Invoke(this, TimerTick, 1f);
                else FinishRace();                
            }   
            
            public void FinishRace()
            {
                InvokeHandler.CancelInvoke(this, UpdateDriverPositions);
                InvokeHandler.CancelInvoke(this, TimerTick);                

                Status = EventStatus.Finishing;

                Instance.NextTick(DismountDrivers);
            }

            private void DismountDrivers()
            {
                DisableTerrainAntiHack();

                for (int i = 0; i < raceDrivers.Count; i++)
                {
                    RaceDriver raceDriver = raceDrivers[i];

                    if (raceDriver.Player.IsSpectating())
                        raceDriver.FinishSpectating();

                    raceDriver.CancelInvokes();

                    raceDriver.DestroyAllUI();

                    raceDriver.DismountPlayer();
                }                

                Instance.NextTick(CalculateScores);
            }

            public void UpdateLapCounter(RaceDriver raceDriver)
            {
                RaceTrack.UpdateLapCounter(raceDriver, RaceMode == RaceMode.Laps, null);

                for (int i = 0; i < raceDrivers.Count; i++)                
                {
                    RaceDriver spectator = raceDrivers[i];
                    if (spectator.SpectateTarget == raceDriver)
                        RaceTrack.UpdateLapCounter(raceDriver, RaceMode == RaceMode.Laps, spectator);
                }
            }

            public void UpdateLapTime(RaceDriver raceDriver)
            {
                RaceTrack.UpdateLapTime(raceDriver, raceDriver.LastLapTime, null);

                for (int i = 0; i < raceDrivers.Count; i++)
                {
                    RaceDriver spectator = raceDrivers[i];
                    if (spectator.SpectateTarget == raceDriver)
                        RaceTrack.UpdateLapTime(raceDriver, raceDriver.LastLapTime, spectator);
                }
            }
            #endregion

            #region Scores
            public void UpdateDriverPositions()
            {
                positions.Sort(delegate (RaceDriver a, RaceDriver b) 
                {
                    return a.TotalDistanceTravelled.CompareTo(b.TotalDistanceTravelled) * -1;
                });
                
                for (int i = 0; i < positions.Count; i++)                
                    positions[i].Position = i + 1 + playersFinished;               
            }

            public void CalculateScores()
            {
                List<RaceDriver> list = Facepunch.Pool.GetList<RaceDriver>();

                list.AddRange(raceDrivers);
                list.Sort(delegate (RaceDriver a, RaceDriver b)
                {
                    return a.TotalDistanceTravelled.CompareTo(b.TotalDistanceTravelled) * -1;
                });

                for (int i = 0; i < list.Count; i++)
                {
                    RaceDriver raceDriver = list[i];
                    if (!raceDriver.HasFinished)
                    {
                        winners[playersFinished] = new RaceScores(raceDriver, CurrentTime - StartTime, raceDriver.LapNumber - 1, raceDriver.TotalDistanceTravelled);
                        playersFinished++;
                    }
                }

                Facepunch.Pool.FreeList(ref list);

                string podiumStr = string.Empty;
                for (int i = 0; i < 3; i++)
                {
                    if (winners.Length >= i + 1 && winners[i] != null)
                        podiumStr += string.Format(msg("win.podium"), "\n" + ToOrdinal(i + 1), Instance.StripTags(winners[i].displayName));
                }

                BroadcastToChat(string.Format(msg("win.winners"), podiumStr));
                BroadcastToChat(msg("win.viewscores"));

                Instance.CreateScoreboard(TrackName, winners);

                for (int i = 0; i < raceDrivers.Count; i++)                
                    raceDrivers[i].AddUI(Instance.scoreContainer, UIScoreboard);
                
                ServerMgr.Instance.StartCoroutine(DestroyCheckpoints());                
            }
           
            public class RaceScores
            {
                public ulong playerId;
                public string displayName;

                public double raceTime;
                public float laps;
                public float distance;
                public bool hasFinished;

                public RaceScores() { }
                public RaceScores(RaceDriver raceDriver, TrackData trackData)
                {
                    playerId = raceDriver.Player.userID;
                    displayName = raceDriver?.Player?.displayName ?? "Unknown";

                    if (raceDriver.HasFinished)
                    {
                        distance = trackData.raceMode == RaceMode.Laps ? trackData.totalDistance * trackData.laps : trackData.totalDistance;
                        raceTime = raceDriver.FinishTime;
                        laps = trackData.laps;
                        hasFinished = true;
                    }                    
                }

                public RaceScores(RaceDriver raceDriver, double raceTime, float laps, float distance)
                {
                    playerId = raceDriver.Player.userID;
                    displayName = raceDriver.Player?.displayName ?? "Unknown";

                    this.distance = distance;
                    this.laps = laps;
                    this.raceTime = raceTime;
                }                
            }
            #endregion
        }

        private class RaceDriver : MonoBehaviour
        {
            private int nextCheckpointIndex;

            private double finishTime;

            private double lastMissedMsg;

            private int position;

            private int spectateIndex = 0;
            
            protected int lastCheckpoint;

            protected bool hasFinished;
            
            public int gridId;


            public Vector3 freezePosition;
            
            private List<string> uiPanels = Facepunch.Pool.GetList<string>();

            private void Awake()
            {
                Player = GetComponent<BasePlayer>();
                
                LapNumber = 1;

                nextCheckpointIndex = 0;
            }

            private void Update()
            {
                if (Player)
                    Player.Teleport(freezePosition);
            }

            private void OnDestroy()
            {
                CancelInvokes();

                if (Player && Player.IsConnected)
                {
                    DestroyAllUI();

                    if (Player.isMounted)
                        DismountPlayer();

                    Player.SetParent(null, false, true);
                    Instance?.restoreData.RestorePlayer(Player);
                }
                
                Destroy(Vehicle);
                
                Facepunch.Pool.FreeList(ref uiPanels);
            }

            private void CheckWanderDistance()
            {
                if (!Configuration.Racers.RestrictTravelDistance)
                    return;

                float maxWanderDistance = Configuration.Racers.MaximumWanderDistance;
                
                if (Manager.TrackData.raceType == RaceType.Minicopter)
                    maxWanderDistance *= 2f;

                if (Vector3Ext.DistanceToLine(Manager.GetCheckpoint(LastCheckpointIndex).position, Manager.GetCheckpoint(NextCheckpointIndex).position, transform.position) > maxWanderDistance)
                {
                    if (Vehicle)
                        Vehicle.ResetVehicle();
                    else
                    {
                        TrackData.PointInfo pointInfo = NextCheckpointIndex == 0 ? Manager.TrackData.gridPoints[gridId] : Manager.GetCheckpoint(NextCheckpointIndex - 1);

                        if (IsWaterSurfaceBasedRace(Manager.RaceType))
                            pointInfo.position.y = TerrainMeta.WaterMap.GetHeight(pointInfo.position);
                        
                        MovePosition(Player, pointInfo.position, false);
                        Player.OverrideViewAngles(Quaternion.Euler(0, pointInfo.rotation, 0) * Vector3.forward);
                    }
                    Player.ChatMessage(msg("wanderedoff", Player.UserIDString));
                }
            }
            
            #region Checkpoints
            public void HitCheckPoint(int number)
            {
                if (HasFinished)
                    return;

                if (NextCheckpointIndex != number)
                {
                    if (Manager.RaceMode == RaceMode.Laps && LapNumber == 1 && number == Manager.CheckpointCount - 1)
                        return;

                    if (NextCheckpointIndex - 1 == number)
                        return;

                    if (lastCheckpoint == number)
                        return;

                    if (NextCheckpointIndex < number)
                        MissedCheckpoint();
                    return;
                }

                if (Manager.RaceMode == RaceMode.Laps)
                {
                    if (number == Manager.CheckpointCount - 1)
                    {
                        FinishedLap(false);
                        if (LapNumber == Manager.Laps && !hasFinished)
                        {
                            hasFinished = true;
                            DestroyAllUI();
                            FinishTime = Manager.CurrentTime - Manager.StartTime;
                            Manager.FinishedRace(this);
                        }
                        else
                        {
                            LapNumber += 1;
                            NextCheckpointIndex = 0;

                            Manager.UpdateLapCounter(this);
                            Manager.UpdateLapTime(this);
                        }
                    }
                    else NextCheckpointIndex += 1;
                }
                else
                {
                    FinishedLap(true);
                    if (number == Manager.CheckpointCount - 1 && !hasFinished)
                    {
                        hasFinished = true;
                        DestroyAllUI();
                        FinishTime = Manager.CurrentTime - Manager.StartTime;
                        Manager.FinishedRace(this);
                    }
                    else
                    {
                        NextCheckpointIndex += 1;
                        Manager.UpdateLapCounter(this);
                    }
                }

                lastCheckpoint = number;
            }
            #endregion

            #region Components
            public BasePlayer Player { get; private set; }

            public RaceDriver SpectateTarget { get; private set; } = null;

            public RaceVehicle Vehicle { get; set; }

            public RaceManager Manager { get; set; }
            #endregion

            #region Laps
            public int LastCheckpointIndex { get; private set; } = 0;

            public int NextCheckpointIndex
            {
                get
                {
                    return nextCheckpointIndex;
                }
                set
                {
                    CompletedSegmentDistanceTravelled += Manager.GetCheckpoint(LastCheckpointIndex).SegmentDistance;

                    LastCheckpointIndex = nextCheckpointIndex;

                    nextCheckpointIndex = value;
                }
            }

            public int LapNumber { get; set; }

            public double LapStart { get; private set; }

            public double FinishTime
            {
                get
                {
                    return finishTime;
                }
                set
                {
                    finishTime = value;
                    HasFinished = true;

                    InvokeHandler.CancelInvoke(this, CheckWanderDistance);
                    InvokeHandler.CancelInvoke(this, UpdateCurrentDistance);
                }
            }

            public double LastLapTime { get; private set; }

            public double TotalRaceTime { get; private set; }

            public bool HasFinished { get; private set; }

            public int Position
            {
                get
                {
                    return position;
                }
                set
                {
                    if (value == position)
                        return;

                    position = value;
                    positionDirty = true;
                }
            }

            private bool hasFailed = false;

            private bool positionDirty = true;

            public float CompletedSegmentDistanceTravelled { get; private set; }

            public float CurrentSegmentDistanceTravelled
            {
                get
                {
                    if (HasFinished)
                        return 0f;

                    TrackData.PointInfo lastCheckpoint = Manager.GetCheckpoint(LastCheckpointIndex);
                    TrackData.PointInfo nextCheckpoint = Manager.GetCheckpoint(NextCheckpointIndex);

                    float d = lastCheckpoint.SegmentDistance * Vector3Ext.InverseLerp(lastCheckpoint.position, nextCheckpoint.position, transform.position);
                    if (float.IsNaN(d) || float.IsInfinity(d))
                        d = 0;

                    return d;
                }
            }

            public float TotalDistanceTravelled { get; private set; } = 0f;

            public void StartRace()
            {
                LapStart = Manager.CurrentTime;

                InvokeHandler.InvokeRandomized(this, CheckWanderDistance, 1f, 2f, 0.25f);

                InvokeHandler.InvokeRandomized(this, UpdateCurrentDistance, 1f, 0.75f, 0.1f);
            }

            public void FinishedLap(bool checkPoint = false)
            {
                UpdateTravelDistance();

                double currentTime = Manager.CurrentTime;
                double time = currentTime - LapStart;
                LastLapTime = time;
                TotalRaceTime += time;
                LapStart = currentTime;
            }

            public void MissedCheckpoint()
            {
                if (lastMissedMsg < Manager.CurrentTime)
                {
                    Player.ChatMessage(msg("missedcp", Player.UserIDString));
                    lastMissedMsg = Manager.CurrentTime + 3;
                }
            }

            public void OnDriverFailed()
            {
                hasFailed = true;
                HasFinished = true;
                CancelInvokes();

                DestroyAllUI();
                FinishTime = Manager.CurrentTime - Manager.StartTime;
            }

            public void UpdateTravelDistance()
            {
                TotalDistanceTravelled = CompletedSegmentDistanceTravelled + CurrentSegmentDistanceTravelled;
            }
            
            public void UpdateCurrentDistance()
            {          
                if (positionDirty)
                {
                    UpdatePosition(this);
                    positionDirty = false;
                }

                TotalDistanceTravelled = CompletedSegmentDistanceTravelled + CurrentSegmentDistanceTravelled;                               
            }
            #endregion

            #region Mounting
            public void MountPlayer(RaceVehicle raceVehicle)
            {
                BaseMountable mountable = raceVehicle.GetEntity();

                mountable._name = Player.displayName;
                Player.EnsureDismounted();

                if (Manager.RaceType == RaceType.Car && Instance.CarCommander && Manager.TrackData.useCommander)
                {
                    Instance.CarCommander.Call("MountPlayerTo", Player, mountable as BasicCar);
                    return;
                }
                else if ((Manager.RaceType == RaceType.Boat || Manager.RaceType == RaceType.RHIB) && Instance.BoatCommander && Manager.TrackData.useCommander)
                {
                    Instance.BoatCommander.Call("MountPlayerTo", Player, mountable as MotorRowboat);
                    return;
                }               
                else if (Manager.RaceType == RaceType.Minicopter && Instance.HeliCommander && Manager.TrackData.useCommander)
                {
                    Instance.HeliCommander.Call("MountPlayerTo", Player, mountable as MiniCopter, false);
                    return;
                }
                else if (Manager.RaceType == RaceType.Kayak)
                {
                    ItemManager.CreateByName(KAYAK_PADDLE_ITEM).MoveToContainer(Player.inventory.containerBelt);
                }

                if (mountable is BaseVehicle)
                {
                    foreach (BaseVehicle.MountPointInfo allMountPoint in (mountable as BaseVehicle).allMountPoints)
                    {
                        if (allMountPoint == null || !allMountPoint.mountable || !allMountPoint.isDriver)                        
                            continue;

                        BasePlayer mounted = allMountPoint.mountable.GetMounted();
                        if (mounted)                        
                            continue;

                        allMountPoint.mountable.MountPlayer(Player);                        
                        return;
                    }
                }

                mountable.MountPlayer(Player);
            }

            public void DismountPlayer()
            {
                if (!Player || !Vehicle)
                    return;
                
                BaseMountable baseMountable = Vehicle.GetEntity();
                if (!baseMountable)
                    return;

                if (Vehicle is RaceModularCar)
                {
                    if (!Manager.TrackData.UseModularCars && Instance.CarCommander && Manager.TrackData.useCommander)
                        Instance.CarCommander.Call("EjectAllPlayers", baseMountable as BasicCar);
                    else baseMountable.DismountAllPlayers();
                }
                else if (Vehicle is RaceBoat)
                {
                    if (Instance.BoatCommander && Manager.TrackData.useCommander)
                        Instance.BoatCommander.Call("EjectAllPlayers", baseMountable as MotorRowboat);
                    else baseMountable.DismountAllPlayers();
                }
                else if (Vehicle is RaceHorse)
                    baseMountable.DismountAllPlayers();
                else
                {
                    if (Instance.HeliCommander && Manager.TrackData.useCommander)
                        Instance.HeliCommander.Call("EjectAllPlayers", baseMountable as MiniCopter);
                    else baseMountable.DismountAllPlayers();
                }
            }
            #endregion

            #region Spectating  
            public void BeginSpectating()
            {
                if (Player.IsSpectating())
                    return;

                DestroyAllUI();

                Player.StartSpectating();
                Player.ChatMessage(msg("spectatecycle", Player.UserIDString));        
                UpdateSpectateTarget();
            }

            public void FinishSpectating()
            {
                if (!Player.IsSpectating())
                    return;

                Player.SetParent(null, true, false);
                Player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
                Player.gameObject.SetLayerRecursive(17);
            }

            public void SetSpectateTarget(RaceDriver raceDriver)
            {                
                UpdateLapCounter(SpectateTarget, Manager.RaceMode == RaceMode.Laps, this);

                Player.ChatMessage($"Spectating: {raceDriver.Player.displayName}");

                Player.SendEntitySnapshot(raceDriver.Player);
                Player.gameObject.Identity();
                Player.SetParent(raceDriver.Player, false, false);                       
            }

            public void UpdateSpectateTarget(int index = 0)
            {                
                int newIndex = spectateIndex += index;

                if (newIndex > Manager.spectateTargets.Count - 1)
                    newIndex = 0;
                else if (newIndex < 0)
                    newIndex = Manager.spectateTargets.Count - 1;

                if (Manager.spectateTargets[newIndex] == SpectateTarget)
                    return;

                spectateIndex = newIndex;
                SpectateTarget = Manager.spectateTargets[spectateIndex];
                SetSpectateTarget(SpectateTarget);
            }
            #endregion

            #region UI
            public void AddUI(CuiElementContainer container, string panel, float destroyIn = 0)
            {
                DestroyUI(panel);

                uiPanels.Add(panel);
                CuiHelper.AddUi(Player, container);

                if (destroyIn > 0)
                    InvokeHandler.Invoke(this, ()=> DestroyUI(panel), destroyIn);
            }

            public void DestroyUI(string panel)
            {
                if (uiPanels.Contains(panel))
                    uiPanels.Remove(panel);
                CuiHelper.DestroyUi(Player, panel);
            }

            public void DestroyAllUI()
            {
                for (int i = 0; i < uiPanels.Count; i++)                
                    CuiHelper.DestroyUi(Player, uiPanels[i]);
                
                CuiHelper.DestroyUi(Player, UIInfo);
                CuiHelper.DestroyUi(Player, UITime);
                CuiHelper.DestroyUi(Player, UILaps);
                CuiHelper.DestroyUi(Player, UILapTime);
                CuiHelper.DestroyUi(Player, UIPosition);
                CuiHelper.DestroyUi(Player, UIScoreboard);              
            }
            #endregion

            public void CancelInvokes()
            {
                InvokeHandler.CancelInvoke(this, CheckWanderDistance);
                InvokeHandler.CancelInvoke(this, UpdateCurrentDistance);
            }
        }

        #region Cars
        private class RaceSnowmobile : RaceCar<Snowmobile> 
        {
            public override void Awake()
            {
                base.Awake();
                InitializeFuel(Component.GetFuelSystem().GetFuelContainer());
            }
        }

        private class RaceSedan : RaceCar<BasicCar> { }

        private class RaceModularCar : RaceCar<ModularCar>
        {
            public override void Awake()
            {
                base.Awake();
                InitializeFuel(Component.GetFuelSystem().GetFuelContainer());
            }

            protected override void CheckUpsideDown()
            {
                if (!Component.HasAnyWorkingEngines())
                {
                    Manager.FailedRace(Driver);
                    return;
                }

                base.CheckUpsideDown();
            }
        }

        private class RaceCar<T> : RaceVehicle<T> where T : BaseMountable
        {
            public override void Awake()
            {
                base.Awake();
                InvokeHandler.InvokeRepeating(this, CheckUpsideDown, 3, 3);
            }

            public override void OnDestroy()
            {
                InvokeHandler.CancelInvoke(this, CheckUpsideDown);
                base.OnDestroy();
            }

            protected virtual void CheckUpsideDown()
            {
                if (Manager.Status != EventStatus.Started)
                    return;

                if (Vector3.Dot(Transform.up, Vector3.down) > 0)
                {
                    Driver.Player.ChatMessage(msg("reset_vehicle", Driver.Player.UserIDString));
                }
            }
        }
        #endregion

        #region Helicopters
        private class RaceHelicopter : RaceVehicle<MiniCopter>
        {
            private StorageContainer container;

            public override void Awake()
            {
                base.Awake();

                enabled = true;
                InitializeFuel();
                InvokeHandler.InvokeRepeating(this, CheckUpsideDown, 3, 3);
            }

            private void Update()
            {
                Component.ApplyWheelForce(Component.frontWheel, 0, 1, 0f);
                Component.ApplyWheelForce(Component.leftWheel, 0, 1, 0f);
                Component.ApplyWheelForce(Component.rightWheel, 0, 1, 0f);
            }

            public override void OnDestroy()
            {
                InvokeHandler.CancelInvoke(this, CheckUpsideDown);
                base.OnDestroy();
            }

            public void OnRaceStarted() => enabled = false;

            private void InitializeFuel()
            {
                StorageContainer container = Component.GetFuelSystem().GetFuelContainer();
                Item item = ItemManager.CreateByItemID(-946369541, 1000);
                item.MoveToContainer(container.inventory, -1, true);
                container.SetFlag(BaseEntity.Flags.Locked, true);

                Component.fuelPerSec = 0f;
            }

            private void CheckUpsideDown()
            {
                if (Manager.Status != EventStatus.Started)
                    return;

                if (Component.engineController.IsWaterlogged())
                {
                    Driver.Player.ChatMessage(msg("reset_vehicle", Driver.Player.UserIDString));
                }
            }

            public override void ToggleKinematic(bool b)
            {
               
            }

            public override void WakeUp() 
            {
                Component.engineController.FinishStartingEngine();
                Rigidbody.WakeUp();
            }
        }
        #endregion

        #region Water Vehicles
        private class RaceSubmarine : RaceWaterVehicle<BaseSubmarine>
        {
            private static readonly FieldInfo curSubDepthY = typeof(BaseSubmarine).GetField("curSubDepthY", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            private static readonly FieldInfo buoyancy = typeof(BaseSubmarine).GetField("buoyancy", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            protected bool IsInWater => (float)curSubDepthY.GetValue(Component) > 0.8f;
             

            public override void Awake()
            {
                base.Awake();
                InitializeFuel(Component.GetFuelSystem().GetFuelContainer());
            }
             
            protected override void CheckStuck()
            {
                if (Manager.Status != EventStatus.Started)
                    return;

                if (IsInWater && !IsFlipped())
                    return;

                Driver.Player.ChatMessage(msg("reset_vehicle", Driver.Player.UserIDString));
            }

            public override void WakeUp()
            {
                base.WakeUp();

                Buoyancy b = buoyancy.GetValue(Component) as Buoyancy;
                if (b)                
                    b.Wake();                
            }
        }

        private class RaceBoat : RaceWaterVehicle<MotorRowboat>
        {
            public override void Awake()
            {                
                base.Awake();
                InitializeFuel(Component.fuelSystem.GetFuelContainer());
            }

            public override void WakeUp()
            {
                base.WakeUp();

                if (Component.buoyancy)
                    Component.buoyancy.Wake();
            }
        }

        private class RaceKayak : RaceWaterVehicle<Kayak> 
        { 
            public override void WakeUp()
            {
                base.WakeUp();

                if (Component.buoyancy)                
                    Component.buoyancy.Wake();                
            }
        }

        private class RaceInflatable : RaceWaterVehicle<WaterInflatable> 
        {
            public override void WakeUp()
            {
                base.WakeUp();

                if (Component.buoyancy)                
                    Component.buoyancy.Wake();                
            }
        }
       
        private class RaceWaterVehicle<T> : RaceVehicle<T> where T : BaseMountable
        {
            public override void Awake()
            {
                base.Awake();

                InvokeHandler.InvokeRepeating(this, CheckStuck, 3, 3);
            }

            public override void OnDestroy()
            {
                InvokeHandler.CancelInvoke(this, CheckStuck);
                base.OnDestroy();
            }

            protected virtual void CheckStuck()
            {
                if (Manager.Status != EventStatus.Started)
                    return;

                if ((TerrainMeta.WaterMap.GetHeight(Transform.position) - TerrainMeta.HeightMap.GetHeight(Transform.position) >= 0) && !IsFlipped())
                    return;

                Driver.Player.ChatMessage(msg("reset_vehicle", Driver.Player.UserIDString));
            }

            protected bool IsFlipped() => Vector3.Dot(Vector3.up, Transform.up) <= 0f;
        }
        #endregion

        #region Horses
        private class RaceHorse : RaceVehicle<BaseRidableAnimal>
        {
            private FieldInfo inQueue = typeof(BaseRidableAnimal).GetField("inQueue", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            public override void Awake()
            {
                base.Awake();
                inQueue.SetValue(Component, true);
                RefreshHorse();
            }

            public override void OnDestroy()
            {
                base.OnDestroy();
            }  
            
            private void RefreshHorse()
            {
                Component.ReplenishStaminaCore(1500, 1500);
                Component.ReplenishStamina(1500);
                Component.Heal(Component.MaxHealth());
                Component.DoNetworkUpdate();
            }

            public override void WakeUp()
            {
                inQueue.SetValue(Component, false);
            }
        }
        #endregion

        private class RaceVehicle<T> : RaceVehicle where T : BaseMountable
        {
            public T Component { get; protected set; }

            public Transform Transform { get; protected set; }
           
            public virtual void Awake()
            {
                Component = GetComponent<T>();
                Transform = Component.transform;

                Rigidbody = GetComponent<Rigidbody>();

                enabled = false;
            }

            public virtual void OnDestroy()
            {
                if (Component && !Component.IsDestroyed)
                {
                    if (Component is ModularCar)
                    {
                        ModularCar m = Component as ModularCar;
                        for (int i = 0; i < m.AttachedModuleEntities.Count; i++)
                        {
                            VehicleModuleStorage storage = m.AttachedModuleEntities[i] as VehicleModuleStorage;
                            if (storage)
                            {
                                ItemContainer container = storage.GetContainer()?.inventory;
                                if (container != null)
                                {
                                    for (int y = container.itemList.Count - 1; y >= 0; y--)
                                    {
                                        Item item = container.itemList[y];
                                        item.RemoveFromContainer();
                                        item.Remove();
                                    }
                                }
                            }
                        }                        
                    }

                    Component.Kill();
                }
            }

            public override BaseMountable GetEntity()
            {
                return Component;
            }

            public override void ResetVehicle()
            {
                Rigidbody rb = Component.GetComponent<Rigidbody>();
                if (rb)
                    rb.velocity = Vector3.zero;

                TrackData.PointInfo pointInfo = base.Driver.NextCheckpointIndex == 0 ? base.Manager.TrackData.gridPoints[Driver.gridId] : base.Manager.GetCheckpoint(base.Driver.NextCheckpointIndex - 1);

                if (IsWaterSurfaceBasedRace(base.Manager.RaceType))
                    pointInfo.position.y = TerrainMeta.WaterMap.GetHeight(pointInfo.position);

                Component.transform.position = pointInfo.position;
                Component.transform.rotation = Quaternion.Euler(0, pointInfo.rotation, 0);

                if (Component is MiniCopter)
                {
                    Component.SetFlag(BaseEntity.Flags.On, true, false, true);
                    Component.SetFlag(BaseEntity.Flags.Reserved4, false, false, true);
                }

                Component.SendNetworkUpdate();
            }

            protected void InitializeFuel(StorageContainer container)
            {
                Item item = ItemManager.CreateByItemID(-946369541, 1000);
                item.MoveToContainer(container.inventory, -1, true);
                container.SetFlag(BaseEntity.Flags.Locked, true);
            }
        }

        private class RaceVehicle : MonoBehaviour
        {
            internal RaceDriver Driver;

            internal RaceManager Manager;

            internal Rigidbody Rigidbody;

            public virtual BaseMountable GetEntity()
            {
                return null;
            }

            public virtual void ToggleKinematic(bool b)
            {
                if (!Rigidbody)                
                    return;

                Rigidbody.isKinematic = b;
            }
            
            public virtual void WakeUp()
            {
                if (Rigidbody)
                {
                    Rigidbody.WakeUp();
                    Rigidbody.AddForce(Vector3.up * 0.1f, ForceMode.Impulse);
                }
            }

            public virtual void ResetVehicle() { }
        }

        private class CheckPoint : MonoBehaviour
        {
            private List<Marker> markers;

            public int Index { get; private set; }

            public bool IsBuilding { get; private set; }

            private void Awake()
            {
                markers = new List<Marker>();
                enabled = false;
            }

            private void OnTriggerEnter(Collider col)
            {
                RaceVehicle raceVehicle = col.GetComponentInChildren<RaceVehicle>();
                if (raceVehicle && raceVehicle.Driver)
                {
                    raceVehicle.Driver.HitCheckPoint(Index);
                    return;
                }

                RaceDriver raceDriver = col.GetComponentInChildren<RaceDriver>();
                if (raceDriver)
                    raceDriver.HitCheckPoint(Index);
            }
            private void OnTriggerExit(Collider col)
            {
                RaceVehicle raceVehicle = col.GetComponentInChildren<RaceVehicle>();
                if (raceVehicle && raceVehicle.Driver)
                {
                    raceVehicle.Driver.HitCheckPoint(Index);
                    return;
                }

                RaceDriver raceDriver = col.GetComponentInChildren<RaceDriver>();
                if (raceDriver)
                    raceDriver.HitCheckPoint(Index);
            }

            private void OnDestroy()
            {
                for (int i = 0; i < markers.Count; i++)                
                    Destroy(markers[i]);                
            }

            public void SetCheckpointData(int index, float radius)
            {
                this.Index = index;

                BoxCollider collider = gameObject.AddComponent<BoxCollider>();                
                collider.gameObject.layer = (int)Layer.Reserved1;
                collider.size = new Vector3(radius * 2, radius * 2, 4f);
                collider.isTrigger = true;

                ServerMgr.Instance.StartCoroutine(SpawnMarkers(radius));
            }    
            
            private IEnumerator SpawnMarkers(float radius)
            {
                IsBuilding = true;

                int objectCount = (int)(2 * Math.PI * radius) / 3;

                Vector3 lastPos = Vector3.zero;
                int count = Current.RaceType == RaceType.Minicopter ? objectCount : (objectCount / 2) + 1;
                int i = 0;
                while (i <= count)
                {                    
                    float angle = i * Mathf.PI * 2 / objectCount;
                    Vector3 position = transform.position + (transform.rotation * (new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius));
                    
                    BuildingBlock block = GameManager.server.CreateEntity(WALL_PREFAB, position) as BuildingBlock;
                    block.enableSaving = false;
                    block.Spawn();

                    block.transform.LookAt(transform, transform.right);
                    block.transform.rotation = block.transform.rotation * (i == 0 ? Quaternion.Euler(270, 90, 0) : Quaternion.Euler(90, 90, 0));
                    block.grounded = true;

                    block.SetGrade(Instance.ParseType<BuildingGrade.Enum>(Configuration.Checkpoints.Grade));
                    block.health = block.MaxHealth();

                    markers.Add(block.gameObject.AddComponent<Marker>());
                    Instance.spawnedEntities.Add(block.net.ID.Value);

                    if (Configuration.Checkpoints.Lights)
                    {
                        if (i < count)
                        {
                            if (Instance.lightType == LightType.Lantern)
                            {
                                BaseOven oven = GameManager.server.CreateEntity(LANTERN_PREFAB, position, Quaternion.LookRotation(lastPos - position, -transform.up)) as BaseOven;
                                oven.enableSaving = false;
                                oven.Spawn();
                                
                                oven.GetComponent<DestroyOnGroundMissing>().enabled = false;
                                oven.GetComponent<GroundWatch>().enabled = false;

                                markers.Add(oven.gameObject.AddComponent<LightMarker>());
                                Instance.spawnedEntities.Add(oven.net.ID.Value);
                            }
                            else
                            {
                                IOEntity ioEntity = GameManager.server.CreateEntity(Instance.lightType == LightType.Flasher ? FLASHER_PREFAB : SIREN_PREFAB, position, Quaternion.LookRotation(lastPos - position, -transform.up)) as IOEntity;
                                ioEntity.enableSaving = false;
                                ioEntity.Spawn();

                                ioEntity.SetFlag(BaseEntity.Flags.Reserved8, true);
                                markers.Add(ioEntity.gameObject.AddComponent<Marker>());
                                Instance.spawnedEntities.Add(ioEntity.net.ID.Value);
                            }
                           
                        }
                    }
                    lastPos = block.transform.position;
                    i++;
                    yield return CoroutineEx.waitForEndOfFrame;
                    yield return CoroutineEx.waitForEndOfFrame;
                    yield return CoroutineEx.waitForEndOfFrame;
                    yield return CoroutineEx.waitForEndOfFrame;
                    yield return CoroutineEx.waitForEndOfFrame;
                }

                IsBuilding = false;
            }
            
            public class Marker : MonoBehaviour
            {
                internal BaseEntity entity;                

                public virtual void Awake()
                {
                    entity = GetComponent<BaseEntity>();
                    enabled = false;                    
                }

                public virtual void OnDestroy()
                {
                    if (entity && !entity.IsDestroyed)
                        entity.Kill();
                }                      
            }  
            
            public class LightMarker : Marker
            {
                private bool isOn = false;

                public override void Awake()
                {
                    base.Awake();
                    InvokeHandler.InvokeRepeating(this, ToggleLight, 0.75f, 0.75f);
                }
                public override void OnDestroy()
                {
                    InvokeHandler.CancelInvoke(this, ToggleLight);
                    base.OnDestroy();
                }

                private void ToggleLight()
                {
                    isOn = !isOn;
                    entity.SetFlag(BaseEntity.Flags.On, isOn);
                }
            } 
        }
        #endregion

        #region Rewards
        private void IssueReward(ulong playerId, int position, bool addToData)
        {
            if (!Configuration.Rewards.Enabled)
                return;

            if (!Configuration.Rewards.Podium && position > 1)
                return;

            int amount = position == 1 ? Configuration.Rewards.Prize1 : position == 2 ? Configuration.Rewards.Prize2 : Configuration.Rewards.Prize3;

            if (Configuration.Rewards.ServerRewards)
                ServerRewards?.Call("AddPoints", playerId, amount);

            if (Configuration.Rewards.Economics)
                Economics?.Call("Deposit", playerId.ToString(), (double)amount);

            if (Configuration.Rewards.Scrap)
            {
                if (!addToData)
                {
                    BasePlayer player = BasePlayer.FindByID(playerId);
                    if (player)
                        player.GiveItem(ItemManager.CreateByItemID(-932201673, amount), BaseEntity.GiveItemReason.PickedUp);
                }
                else restoreData.AddPrizeToData(playerId, -932201673, amount);
            }
        }

        private void IssueRefunds(List<BasePlayer> players)
        {
            for (int i = 0; i < players.Count; i++)            
                IssueRefund(players[i]);            
        }

        private void IssueRefund(BasePlayer player)
        {
            if (!Configuration.Entrance.Enabled)
                return;

            int amount = Configuration.Entrance.Amount;

            if (Configuration.Entrance.ServerRewards)
                ServerRewards?.Call("AddPoints", player.userID, amount);

            if (Configuration.Entrance.Economics)
                Economics?.Call("Deposit", player.UserIDString, (double)amount);

            if (Configuration.Entrance.Scrap)
                player.GiveItem(ItemManager.CreateByItemID(-932201673, amount), BaseEntity.GiveItemReason.PickedUp);
        }
        #endregion

        #region Teleportation
        private static void MovePosition(BasePlayer player, Vector3 destination, bool sleep)
        {
            if (sleep)
            {
                if (player.net?.connection != null)
                    player.ClientRPCPlayer(null, player, "StartLoading");
                StartSleeping(player);
                player.MovePosition(destination);
                if (player.net?.connection != null)
                    player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
                if (player.net?.connection != null)
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
                player.UpdateNetworkGroup();
                player.SendNetworkUpdateImmediate(false);
                if (player.net?.connection == null) return;
                try { player.ClearEntityQueue(null); } catch { }
                player.SendFullSnapshot();
            }
            else
            {
                player.MovePosition(destination);
                player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
                player.SendNetworkUpdateImmediate();
                try { player.ClearEntityQueue(null); } catch { }
            }
        }

        private static void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }
        #endregion

        #region UI 
        private const string UIInfo = "TrackUI_Info";
        private const string UITime = "TrackUI_Time";
        private const string UILaps = "TrackUI_Laps";
        private const string UILapTime = "TrackUI_LapTime";
        private const string UIPosition = "TrackUI_Position";
        private const string UIScoreboard = "TrackUI_Scoreboard";
            
        public static class UI
        {
            public static CuiElementContainer Container(string panelName, string color, UI4 dimensions, bool useCursor = false, string parent = "Overlay")
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax()},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panelName.ToString()
                    }
                };
                return container;
            }  
            
            public static void Panel(ref CuiElementContainer container, string panel, string color, UI4 dimensions, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    CursorEnabled = cursor
                },
                panel);
            }

            public static void Label(ref CuiElementContainer container, string panel, string text, int size, UI4 dimensions, TextAnchor align = TextAnchor.MiddleCenter, bool altFont = false)
            {
                container.Add(new CuiLabel
                {
                    Text = { FontSize = size, Align = align, Text = text, Font = altFont ? "droidsansmono.ttf" : "robotocondensed-regular.ttf" },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                },
                panel);

            }

            public static void Button(ref CuiElementContainer container, string panel, string color, string text, int size, UI4 dimensions, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0f },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }

            public static void OutlineLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string distance, UI4 dimensions, TextAnchor align = TextAnchor.MiddleCenter, string parent = "Overlay")
            {
                CuiElement textElement = new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    FadeOut = 0.2f,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = text,
                            FontSize = size,
                            Align = align,
                            FadeIn = 0.2f
                        },
                        new CuiOutlineComponent
                        {
                            Distance = distance,
                            Color = color
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = dimensions.GetMin(),
                            AnchorMax = dimensions.GetMax()
                        }
                    }
                };
                container.Add(textElement);
            }

            public static void Input(ref CuiElementContainer container, string panel, string text, int size, string command, UI4 dimensions)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Align = TextAnchor.MiddleLeft,
                            CharsLimit = 300,
                            Command = command + text,
                            FontSize = size,
                            IsPassword = false,
                            Text = text
                        },
                        new CuiRectTransformComponent {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                    }
                });
            }

            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.Substring(1);
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }

        public class UI4
        {
            public float xMin, yMin, xMax, yMax;

            public UI4(float xMin, float yMin, float xMax, float yMax)
            {
                this.xMin = xMin;
                this.yMin = yMin;
                this.xMax = xMax;
                this.yMax = yMax;
            }

            public string GetMin() => $"{xMin} {yMin}";
            public string GetMax() => $"{xMax} {yMax}";
        }
        #endregion

        #region UI Display
        private static void DisplayInformation(List<RaceDriver> drivers, string trackName, TrackData data)
        {
            CuiElementContainer container = UI.Container(UIInfo, "0 0 0 0", new UI4(0.3f, 0.7f, 0.7f, 1f), false);

            UI.OutlineLabel(ref container, UIInfo, "0 0 0 1", trackName, 40, "2 2", new UI4(0, 0.5f, 1, 1), TextAnchor.LowerCenter);
            UI.OutlineLabel(ref container, UIInfo, "0 0 0 1", data.raceMode == RaceMode.Laps ? string.Format(msg("ui.info.laps"), data.laps, data.checkPoints.Count) : string.Format(msg("ui.info.sprint"), data.checkPoints.Count), 20, "1 1", new UI4(0, 0.25f, 1, 0.5f), TextAnchor.MiddleLeft);
            UI.OutlineLabel(ref container, UIInfo, "0 0 0 1", string.Format(msg("ui.info.distance"), data.raceMode == RaceMode.Laps ? Math.Round(data.totalDistance * data.laps, 1) : Math.Round(data.totalDistance, 1)), 20, "1 1", new UI4(0, 0, 1, 0.5f), TextAnchor.MiddleRight);

            for (int i = 0; i < drivers.Count; i++)            
                drivers[i].AddUI(container, UIInfo);            
        }

        private static void UpdateCountdown(List<RaceDriver> drivers, string time)
        {
            CuiElementContainer container = UI.Container(UITime, "0 0 0 0", new UI4(0.3f, 0.3f, 0.7f, 0.7f), false);
            UI.OutlineLabel(ref container, UITime, "", time, 100, "2 2", new UI4(0, 0, 1, 1));

            for (int i = 0; i < drivers.Count; i++)
                drivers[i].AddUI(container, UITime);
        }

        private static void UpdateLapCounter(RaceDriver raceDriver, bool isLaps, RaceDriver spectator)
        {
            ConfigData.UIOptions.UICounter opt = Configuration.UI.Counter;
            if (opt.Enabled)
            {
                if (!spectator)
                    spectator = raceDriver;

                CuiElementContainer container = UI.Container(UILaps, opt.BackgroundColor, opt.Position, false);

                UI.Label(ref container, UILaps, isLaps ? string.Format(msg("ui.lap", spectator.Player.UserIDString), opt.Color2, raceDriver.LapNumber, Current.Laps) : string.Format(msg("ui.checkpoint", spectator.Player.UserIDString), opt.Color2, raceDriver.NextCheckpointIndex, Current.CheckpointCount), 12, new UI4(0.03f, 0, 1, 1), TextAnchor.MiddleLeft, true);

                spectator.AddUI(container, UILaps);
            }
        }

        private static void UpdateLapTime(RaceDriver raceDriver, double currentTime, RaceDriver spectator)
        {
            ConfigData.UIOptions.UICounter opt = Configuration.UI.Times;
            if (opt.Enabled)
            {
                if (!spectator)
                    spectator = raceDriver;

                CuiElementContainer container = UI.Container(UILapTime, opt.BackgroundColor, opt.Position, false);
                UI.Label(ref container, UILapTime, string.Format(msg("ui.laptime", spectator.Player.UserIDString), opt.Color2, FormatTime(currentTime)), 12, new UI4(0.03f, 0, 1, 1), TextAnchor.MiddleLeft, true);

                spectator.AddUI(container, UILapTime, 5f);
            }
        }

        private static void UpdatePosition(RaceDriver raceDriver)
        {
            ConfigData.UIOptions.UICounter opt = Configuration.UI.Position;
            if (opt.Enabled)
            {                
                CuiElementContainer container = UI.Container(UIPosition, opt.BackgroundColor, opt.Position, false);
                UI.Label(ref container, UIPosition, string.Format(msg("ui.position", raceDriver.Player.UserIDString), opt.Color2, raceDriver.Position), 12, new UI4(0.03f, 0, 1, 1), TextAnchor.MiddleLeft, true);

                raceDriver.AddUI(container, UIPosition);
            }
        }
        #endregion        

        #region UI Scoreboard
        private void CreateScoreboard(string trackName, RaceManager.RaceScores[] raceScores)
        {
            if (raceScores == null || raceScores.Length == 0)
                return;

            scoreContainer = UI.Container(UIScoreboard, UI.Color("#2b2b2b", 1f), new UI4(0, 0, 1, 1), true);

            UI.Label(ref scoreContainer, UIScoreboard, trackName, 30, new UI4(0.3f, 0.85f, 0.7f, 0.95f));
            UI.Button(ref scoreContainer, UIScoreboard, UI.Color("#393939", 1f), msg("close"), 15, new UI4(0.01f, 0.95f, 0.1f, 0.99f), "trackuileave");

            UI.Panel(ref scoreContainer, UIScoreboard, UI.Color("#d85540", 1f), new UI4(0.2f, 0.8f, 0.23f, 0.84f));

            UI.Panel(ref scoreContainer, UIScoreboard, UI.Color("#393939", 1f), new UI4(0.233f, 0.8f, 0.527f, 0.84f));
            UI.Label(ref scoreContainer, UIScoreboard, msg("player"), 15, new UI4(0.243f, 0.8f, 0.517f, 0.84f), TextAnchor.MiddleLeft);

            UI.Panel(ref scoreContainer, UIScoreboard, UI.Color("#393939", 1f), new UI4(0.53f, 0.8f, 0.66f, 0.84f));
            UI.Label(ref scoreContainer, UIScoreboard, msg("distance"), 15, new UI4(0.53f, 0.8f, 0.66f, 0.84f));

            UI.Panel(ref scoreContainer, UIScoreboard, UI.Color("#393939", 1f), new UI4(0.663f, 0.8f, 0.8f, 0.84f));
            UI.Label(ref scoreContainer, UIScoreboard, msg("time"), 15, new UI4(0.663f, 0.8f, 0.8f, 0.84f));

            UI.Panel(ref scoreContainer, UIScoreboard, UI.Color("#d85540", 1f), new UI4(0.2f, 0.7955f, 0.8f, 0.7995f));

            for (int i = 0; i < Mathf.Min(raceScores.Length, 16); i++)
            {                
                if (raceScores[i] == null)
                    continue;

                float yMin = 0.8f - ((i + 1) * 0.045f);
                float yMax = yMin + 0.04f;
                UI.Panel(ref scoreContainer, UIScoreboard, UI.Color("#545554", 1f), new UI4(0.2f, yMin, 0.23f, yMax));
                UI.Label(ref scoreContainer, UIScoreboard, (i + 1).ToString(), 15, new UI4(0.2f, yMin, 0.23f, yMax));

                UI.Panel(ref scoreContainer, UIScoreboard, UI.Color("#545554", 1f), new UI4(0.233f, yMin, 0.527f, yMax));
                UI.Label(ref scoreContainer, UIScoreboard, $"{StripTags(raceScores[i].displayName)}", 15, new UI4(0.243f, yMin, 0.517f, yMax), TextAnchor.MiddleLeft);

                UI.Panel(ref scoreContainer, UIScoreboard, UI.Color("#545554", 1f), new UI4(0.53f, yMin, 0.66f, yMax));
                UI.Label(ref scoreContainer, UIScoreboard, $"{Math.Round(raceScores[i].distance, 1)} M", 15, new UI4(0.54f, yMin, 0.65f, yMax), TextAnchor.MiddleRight);

                UI.Panel(ref scoreContainer, UIScoreboard, UI.Color("#545554", 1f), new UI4(0.663f, yMin, 0.8f, yMax));
                UI.Label(ref scoreContainer, UIScoreboard, !raceScores[i].hasFinished ? "DNF" : FormatTime(raceScores[i].raceTime), 15, new UI4(0.663f, yMin, 0.79f, yMax), TextAnchor.MiddleRight);
            }
        }

        private string StripTags(string str)
        {
            if (str.StartsWith("[") && str.Contains("]") && str.Length > str.IndexOf("]"))
                str = str.Substring(str.IndexOf("]") + 1).Trim();

            if (str.StartsWith("[") && str.Contains("]") && str.Length > str.IndexOf("]"))
                StripTags(str);

            return str;
        }

        [ConsoleCommand("trackuileave")]
        void ccmdLeaveEvent(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (!player)
                return;

            RaceDriver raceDriver = player.GetComponent<RaceDriver>();
            if (raceDriver)
            {
                if (Current && (Current.Status == EventStatus.Open || Current.Status == EventStatus.Prestart || Current.Status == EventStatus.Started))
                {
                    Current.LeaveEvent(player, false);
                    return;
                }
                raceDriver.DestroyAllUI();
                UnityEngine.Object.Destroy(raceDriver);
            }
            else CuiHelper.DestroyUi(player, UIScoreboard);
        }
        #endregion

        #region Commands  
        [ChatCommand("reset")]
        void cmdReset(BasePlayer player, string command, string[] args)
        {
            if (!Current)
                return;

            if (Current.Status != EventStatus.Started)
                return;

            RaceDriver raceDriver = player.GetComponent<RaceDriver>();
            if (raceDriver)
            {
                if (raceDriver.Vehicle)
                    raceDriver.Vehicle.ResetVehicle();
            }
        }

        [ChatCommand("race")]
        void cmdRace(BasePlayer player, string command, string[] args)
        {
            bool isAdmin = permission.UserHasPermission(player.UserIDString, "racetrack.admin");

            if (args.Length == 0)
            {                
                string message = $"<color=#ce422b>RaceTrack</color><color=#D3D3D3> v</color><color=#ce422b>{Version}</color>"
                        + "\n<color=#ce422b>/race join</color><color=#D3D3D3> - Join an open race event</color>"
                        + "\n<color=#ce422b>/race leave</color><color=#D3D3D3> - Leave the event</color>";
                
                if (isAdmin)
                {
                    message += "\n<color=#ce422b>/race open</color><color=#D3D3D3> - Open a race event</color>"
                        + "\n<color=#ce422b>/race openrandom</color><color=#D3D3D3> - Opens a randomly selected race event</color>"
                        + "\n<color=#ce422b>/race start</color><color=#D3D3D3> - Start a race event</color>"
                        + "\n<color=#ce422b>/race stop</color><color=#D3D3D3> - Stop the current event</color>"
                        + "\n<color=#ce422b>/race select <trackname></color><color=#D3D3D3> - Set the next race track</color>";                    
                }

                SendReply(player, message);
                return;
            }

            if (!Current)
                Current = new GameObject().AddComponent<RaceManager>();

            switch (args[0].ToLower())
            {
                case "join":
                    if (!permission.UserHasPermission(player.UserIDString, "racetrack.play"))
                    {
                        SendReply(player, msg("nopermission", player.UserIDString));
                        return;
                    }
                    if (Configuration.Entrance.Enabled)
                    {
                        if (Configuration.Entrance.Economics)
                        {
                            double amount = (double)Economics?.Call("Balance", player.UserIDString);
                            if (amount < Configuration.Entrance.Amount || !(bool)Economics?.Call("Withdraw", player.UserIDString, (double)Configuration.Entrance.Amount))
                            {
                                SendReply(player, string.Format(msg("eventfee", player.UserIDString), Configuration.Entrance.Amount, msg("economics", player.UserIDString)));
                                return;
                            }                            
                        }
                        if (Configuration.Entrance.ServerRewards)
                        {
                            int amount = (int)ServerRewards?.Call("CheckPoints", player.userID);
                            if (amount < Configuration.Entrance.Amount || !(bool)ServerRewards?.Call("TakePoints", player.userID, Configuration.Entrance.Amount))
                            {
                                SendReply(player, string.Format(msg("eventfee", player.UserIDString), Configuration.Entrance.Amount, msg("serverrewards", player.UserIDString)));
                                return;
                            }
                        }
                        if (Configuration.Entrance.Scrap)
                        {
                            int amount = player.inventory.GetAmount(-932201673);
                            if (amount <= Configuration.Entrance.Amount)
                            {
                                player.inventory.Take(null, -932201673, Configuration.Entrance.Amount);
                                SendReply(player, string.Format(msg("eventfee", player.UserIDString), Configuration.Entrance.Amount, msg("scrap", player.UserIDString)));
                                return;
                            }
                        }
                    }
                    Current.JoinEvent(player);
                    return;

                case "leave":
                    Current.LeaveEvent(player, false);
                    return;

                case "open":
                    if (!isAdmin)
                        return;

                    if (Current.Status != EventStatus.Finished)
                    {
                        SendReply(player, string.Format("<color=#D3D3D3>There is already an event {0}</color>", Current.Status));
                        return;
                    }
                   
                    if (!Current.HasTrackSet)
                    {
                        SendReply(player, "<color=#D3D3D3>You need to set a track before opening an event</color>");
                        return;
                    }

                    if (Current.TrackData.raceType == RaceType.Car)
                    {
                        if (Current.TrackData.modularCarProfiles != null && Current.TrackData.modularCarProfiles.Count > 0)
                        {
                            for (int i = 0; i < Current.TrackData.modularCarProfiles.Count; i++)
                            {
                                if (!carData.ContainsKey(Current.TrackData.modularCarProfiles[i]))
                                {
                                    SendReply(player, $"<color=#D3D3D3>The selected track is setup to use a modular vehicle profile that does not exist. You must either remove the profile from your track data, or create a new profile with the same name ({Current.TrackData.modularCarProfiles[i]}). Unable to open race!</color>");
                                    return;
                                }
                            }                            
                        }
                    }

                    Current.OpenEvent();
                    SendReply(player, "<color=#D3D3D3>Creating checkpoints, please wait!</color>");
                    return;

                case "openrandom":
                    if (!isAdmin)
                        return;

                    if (Current.Status != EventStatus.Finished)
                    {
                        SendReply(player, string.Format("<color=#D3D3D3>There is already an event {0}</color>", Current.Status));
                        return;
                    }

                    if (trackData.Count == 0)
                    {
                        SendReply(player, "<color=#D3D3D3>There are no tracks setup</color>");
                        return;
                    }

                    KeyValuePair<string, TrackData> data = trackData.ElementAt(UnityEngine.Random.Range(0, trackData.Count));
                    Current.SetTrackData(data.Key, data.Value);

                    if (Current.TrackData.raceType == RaceType.Car)
                    {
                        if (Current.TrackData.modularCarProfiles != null && Current.TrackData.modularCarProfiles.Count > 0)
                        {
                            for (int i = 0; i < Current.TrackData.modularCarProfiles.Count; i++)
                            {
                                if (!carData.ContainsKey(Current.TrackData.modularCarProfiles[i]))
                                {
                                    SendReply(player, $"<color=#D3D3D3>The selected track ({data.Key}) is setup to use a modular vehicle profile that does not exist. You must either remove the profile from your track data, or create a new profile with the same name ({Current.TrackData.modularCarProfiles[i]}). Unable to open race!</color>");                                   
                                    return;
                                }
                            }

                        }
                    }

                    SendReply(player, $"<color=#D3D3D3>Track {data.Key} was randomly selected. Creating checkpoints, please wait!</color>");
                    Current.OpenEvent();
                    return;

                case "start":
                    if (!isAdmin)
                        return;

                    if (Current.Status != EventStatus.Open)
                    {
                        SendReply(player, "<color=#D3D3D3>You must open the event before starting it</color>");
                        return;
                    }

                    if (Current.JoinerCount < 1)
                    {
                        SendReply(player, "<color=#D3D3D3>You can not start the event if there are no players</color>");
                        return;
                    }

                    Current.StartEvent();
                    return;

                case "stop":
                    if (!isAdmin)
                        return;
                    if (Current.Status == EventStatus.Loading)
                    {
                        SendReply(player, "<color=#D3D3D3>You must wait until the event has finished loading</color>");
                        return;
                    }
                    if (Current.Status == EventStatus.Finishing)
                    {
                        SendReply(player, "<color=#D3D3D3>The last event is finishing up</color>");
                        return;
                    }
                    if (Current.Status == EventStatus.Finished)
                    {
                        SendReply(player, "<color=#D3D3D3>There is no event in progress</color>");
                        return;
                    }
                    else Current.EndEvent();
                    return;

                case "select":
                    if (!isAdmin)
                        return;

                    if (args.Length != 2)
                    {
                        SendReply(player, "<color=#D3D3D3>You must enter a track name</color>");
                        return;
                    }

                    if (!raceTracks.ContainsKey(args[1]))
                    {
                        SendReply(player, string.Format("<color=#D3D3D3>Unable to find a track with the name {0}</color>", args[1]));
                        return;
                    }

                    if (Current.Status != EventStatus.Finished)
                    {
                        SendReply(player, Current.Status.ToString());
                        SendReply(player, "<color=#D3D3D3>You can not set the track when an event is open or in progress</color>");
                        return;
                    }

                    Current.SetTrackData(args[1], raceTracks[args[1]]);
                    SendReply(player, string.Format("<color=#D3D3D3>You have set the track to {0}</color>", args[1]));
                    return;

                default:
                    SendReply(player, "<color=#D3D3D3>Invalid Syntax. Type \"/race\" for help</color>");
                    return;
            }
        }

        [ChatCommand("tr")]
        private void cmdtetRace(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
                return;

            if (args.Length == 0)
            {
                SendReply(player, "Enter track name");
                return;
            }

            if (!Current)
                Current = new GameObject().AddComponent<RaceManager>();

            Current.SetTrackData(args[0], raceTracks[args[0]]);

            Current.OpenEvent();

            timer.In(1f, CreateNPCS);
        }

        private void CreateNPCS()
        {
            if (Current.Status != EventStatus.Open)
            {
                timer.In(1f, CreateNPCS);
                return;
            }

            bool createdNpc = false;

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                Current.JoinEvent(player);

                if (!createdNpc)
                {
                    BasePlayer npc = GameManager.server.CreateEntity(player.PrefabName, player.transform.position, player.transform.rotation) as BasePlayer;
                    npc.enableSaving = false;
                    npc.displayName = $"Bot {UnityEngine.Random.Range(1000, 9000)}";
                    npc.userID = player.userID + (ulong)UnityEngine.Random.Range(1000, 9000);
                    npc.UserIDString = npc.userID.ToString();
                    npc.Spawn();

                    Current.JoinEvent(npc);

                    timer.In(2f, npc.EndSleeping);
                    createdNpc = true;
                }
            }

            Current.StartEvent();
        }

        [ChatCommand("track")]
        void cmdTrack(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "racetrack.admin"))
                return;

            TrackData trackData = trackCreator.ContainsKey(player.userID) ? trackCreator[player.userID] : null;

            if (args.Length == 0)
            {
                string message = $"<color=#D3D3D3><color=#ce422b>RaceTrack</color> v<color=#ce422b>{Version}</color> - Track Creator"
                        + "<size=12>\n<color=#ce422b>/track new</color> - Begin creating a new track"
                        + "\n<color=#ce422b>/track cancel</color> - Cancel current track creation"                       
                        + "\n<color=#ce422b>/track save <name></color> - Save the current track"
                        + "\n<color=#ce422b>/track edit <name></color> - Edit an existing track"
                        + "\n<color=#ce422b>/track list</color> - List all race tracks"
                        + "\n<color=#ce422b>/track delete <name></color> - Delete the track with the specified name</color></size>";

                if (trackData != null)
                {
                    message += "\n<size=12><color=#D3D3D3><color=#ce422b>/track players <number></color> - Set the minimum players"
                            + "\n<color=#ce422b>/track mode <laps/sprint></color> - Set the race mode"
                            + "\n<color=#ce422b>/track type <car / boat / rhib / minicopter / horse / kayak / boogieboard / innertube / submarine / snowmobile / foot></color> - Set the race type"
                            + "\n<color=#ce422b>/track laps <number></color> - Set the amount of laps"
                            + "\n<color=#ce422b>/track time <amount></color> - Set a time limit to how long the race can last (in seconds)</color></size>";
                }
                SendReply(player, message);

                if (trackData != null)
                {
                    string message2 = "<size=12><color=#D3D3D3><color=#ce422b>/track gp add</color> - Add a track grid poin"
                            + "\n<color=#ce422b>/track gp remove <number></color> - Remove a track grid point"
                            + "\n<color=#ce422b>/track gp move <number></color> - Move a track grid point to your position"
                            + "\n<color=#ce422b>/track gp show</color> - Show all track grid points"
                            + "\n<color=#ce422b>/track cp add <radius></color> - Add a track checkpoint"
                            + "\n<color=#ce422b>/track cp remove <number></color> - Remove a track checkpoint"
                            + "\n<color=#ce422b>/track cp move <number></color> - Move a track check point to your position"
                            + "\n<color=#ce422b>/track cp size <number> <radius></color> - Adjust the size of the specified track checkpoint"
                            + "\n<color=#ce422b>/track cp show</color> - Show all track checkpoints</color></size>";

                    SendReply(player, message2);
                }

                if (trackData != null)
                {
                    string message3 = string.Empty;
                    if (trackData.raceType == RaceType.Car)
                    {
                        message3 += "<size=12><color=#D3D3D3><color=#ce422b>/track modularcar add <profile name></color> - Adds the specified vehicle profile to the track"
                           + "\n<color=#ce422b>/track modularcar remove <profile name></color> - Removes the specified vehicle profile from the track"
                           + "\n<color=#ce422b>/track modularcar list</color> - Lists vehicle profiles associated with the track"
                           + "\n<color=#ce422b>/track allowdamage <true/false></color> - Enable vehicle damage (Modular Cars only)"
                           + "\n<color=#ce422b>/track damagemultiplier <number></color> - Set a collision damage multiplier (Modular Cars only)</color></size>";
                    }
                    if (trackData.raceType == RaceType.Horse)
                        message3 += "<size=12><color=#ce422b>/track breed <0-9></color><color=#D3D3D3> - Set the horse breed, breed ID's are from 0 - 9</color></size>";

                    if ((int)trackData.raceType <= 3)
                        message3 += "\n<color=#ce422b><size=12>/track commander <true/false></color><color=#D3D3D3> - Use Boat/Car/HeliCommander for this race</color></size>";

                    SendReply(player, message3);
                }
                return;
            }

            switch (args[0].ToLower())
            {
                case "new":
                    trackData = trackCreator[player.userID] = new TrackData();
                    SendReply(player, "<color=#D3D3D3>You are now creating a new race track</color>");
                    return;
                case "list":
                    string tracks = "<color=#ce422b>Race Tracks:</color>";
                    foreach (KeyValuePair<string, TrackData> track in raceTracks)
                        tracks += $"\n<color=#D3D3D3>{track.Key}</color>";
                    SendReply(player, tracks);
                    return;
                case "edit":
                    if (args.Length < 2)
                    {
                        SendReply(player, "<color=#D3D3D3>You must specify a track name</color>");
                        return;
                    }

                    if (!raceTracks.ContainsKey(args[1]))
                    {
                        SendReply(player, "<color=#D3D3D3>Unable to find a track with that name</color>");
                        return;
                    }

                    trackData = trackCreator[player.userID] = raceTracks[args[1]];
                    SendReply(player, string.Format("<color=#D3D3D3>You are now editing the race track named {0}</color>", args[1]));
                    return;
                case "delete":
                    if (args.Length < 2)
                    {
                        SendReply(player, "<color=#D3D3D3>You must specify a track name</color>");
                        return;
                    }

                    if (!raceTracks.ContainsKey(args[1]))
                    {
                        SendReply(player, "<color=#D3D3D3>Unable to find a track with that name</color>");
                        return;
                    }

                    if (Current && Current.TrackName == args[1])
                    {
                        SendReply(player, "<color=#D3D3D3>You can't delete a track when it is in use</color>");
                        return;
                    }

                    raceTracks.Remove(args[1]);
                    SaveTrackData();
                    SendReply(player, string.Format("<color=#D3D3D3>You have deleted the track named {0}</color>", args[1]));
                    return;
                default:
                    break;
            }           

            if (!trackCreator.ContainsKey(player.userID))
            {
                SendReply(player, "<color=#D3D3D3>You must either create a new track, or edit an existing one</color>");
                return;
            }

            switch (args[0].ToLower())
            {               
                case "cancel":
                    if (trackData != null)
                    {
                        trackCreator.Remove(player.userID);
                        SendReply(player, "<color=#D3D3D3>You have cancelled track creation</color>");
                    }
                    else SendReply(player, "<color=#D3D3D3>You are not currently creating a race track</color>");
                    return;
                case "save":
                    if (trackData != null)
                    {
                        if (args.Length < 2)
                        {
                            SendReply(player, "<color=#D3D3D3>You must specify a track name</color>");
                            return;
                        }

                        if(trackData.gridPoints.Count == 0)
                        {
                            SendReply(player, "<color=#D3D3D3>You must create grid points for racers to start on</color>");
                            return;
                        }

                        if (trackData.checkPoints.Count < 2)
                        {
                            SendReply(player, "<color=#D3D3D3>You must create atleast 2 check points for racers to follow</color>");
                            return;
                        }

                        if (trackData.laps == 0 && trackData.raceMode == RaceMode.Laps)
                        {
                            SendReply(player, "<color=#D3D3D3>You must specify the amount of laps in this race</color>");
                            return;
                        }

                        if (trackData.minPlayers == 0)
                        {
                            SendReply(player, "<color=#D3D3D3>You must specify the minimum amount of players for this race</color>");
                            return;
                        }

                        float totalDistance = 0;
                        for (int i = 1; i < trackData.checkPoints.Count; i++)                        
                            totalDistance += Vector3.Distance(trackData.checkPoints[i - 1].position, trackData.checkPoints[i].position);                        

                        trackData.totalDistance = totalDistance;
                                                
                        raceTracks[string.Join(" ", args).Substring(5)] = trackData;
                        SaveTrackData();
                        trackCreator.Remove(player.userID);
                        SendReply(player, string.Format("<color=#D3D3D3>You have successfully saved a race track named {0}</color>", string.Join(" ", args).Substring(5)));
                    }
                    else SendReply(player, "<color=#D3D3D3>You are not currently creating a race track</color>");
                    return;                
                case "laps":
                    if (args.Length < 2)
                    {
                        SendReply(player, "<color=#D3D3D3>You must enter a number of laps</color>");
                        return;
                    }

                    int laps = 0;
                    if (!int.TryParse(args[1], out laps))
                    {
                        SendReply(player, "<color=#D3D3D3>You must enter a number of laps</color>");
                        return;
                    }

                    trackData.laps = laps;
                    SendReply(player, string.Format("<color=#D3D3D3>You have set the number of laps to {0}</color>", laps));
                    return;
                case "time":
                    if (args.Length < 2)
                    {
                        SendReply(player, "<color=#D3D3D3>You must enter a amount of time in seconds</color>");
                        return;
                    }

                    int time = 0;
                    if (!int.TryParse(args[1], out time))
                    {
                        SendReply(player, "<color=#D3D3D3>You must enter a amount of time in seconds</color>");
                        return;
                    }

                    trackData.maxTime = time;
                    SendReply(player, string.Format("<color=#D3D3D3>You have set the race time limit to {0} seconds</color>", time));
                    return;
                case "players":
                    if (args.Length < 2)
                    {
                        SendReply(player, "<color=#D3D3D3>You must enter a number</color>");
                        return;
                    }

                    int minPlayers = 0;
                    if (!int.TryParse(args[1], out minPlayers))
                    {
                        SendReply(player, "<color=#D3D3D3>You must enter a number</color>");
                        return;
                    }

                    trackData.minPlayers = minPlayers;
                    SendReply(player, string.Format("<color=#D3D3D3>You have set the minimum players to {0}</color>", minPlayers));
                    return;
                case "mode":
                    if (args.Length < 2)
                    {
                        SendReply(player, "<color=#D3D3D3>You must enter a race mode (laps or sprint)</color>");
                        return;
                    }

                    trackData.raceMode = ParseType<RaceMode>(args[1]);
                    SendReply(player, string.Format("<color=#D3D3D3>You have set the race mode to {0}</color>", trackData.raceMode));
                    return;
                case "type":
                    if (args.Length < 2)
                    {
                        SendReply(player, "<color=#D3D3D3>You must enter a type of race (car/boat/rhib/minicopter/horse/kayak/boogieboard/innertube)</color>");
                        return;
                    }

                    trackData.raceType = ParseType<RaceType>(args[1]);
                    SendReply(player, string.Format("<color=#D3D3D3>You have set the race type to {0}</color>", trackData.raceType));
                    return;
                case "allowdamage":                   
                    bool allowDamage;
                    if (args.Length >= 2 && bool.TryParse(args[1], out allowDamage))
                    {
                        trackData.allowVehicleDamage = allowDamage;
                        SendReply(player, string.Format("<color=#D3D3D3>You have set vehicle damage to {0}</color>", allowDamage));
                        return;
                    }

                    SendReply(player, "<color=#D3D3D3>You must enter true or false</color>");
                    return;
                case "damagemultiplier":
                    if (args.Length < 2)
                    {
                        SendReply(player, "<color=#D3D3D3>You must enter a number</color>");
                        return;
                    }
                    float damageMulti = 1f;
                    if (!float.TryParse(args[1], out damageMulti))
                    {
                        SendReply(player, "<color=#D3D3D3>You must enter a number</color>");
                        return;
                    }
                    trackData.modularDamageMulti = damageMulti;
                    SendReply(player, string.Format("<color=#D3D3D3>You have set the vehicle collision damage multiplier to {0}</color>", damageMulti));
                    return;

                case "modularcar":
                    if (args.Length < 2)
                    {
                        SendReply(player, "<color=#D3D3D3>Invalid syntax!</color>");
                        return;
                    }

                    switch (args[1].ToLower())
                    {
                        case "add":
                            {
                                if (args.Length < 3)
                                {
                                    SendReply(player, "<color=#D3D3D3>You must enter vehicle profile name</color>");
                                    return;
                                }

                                if (!carData.ContainsKey(args[2]))
                                {
                                    SendReply(player, "<color=#D3D3D3>The vehicle profile name you have entered does not exist</color>");
                                    return;
                                }

                                if (trackData.modularCarProfiles == null)
                                    trackData.modularCarProfiles = new List<string>();

                                trackData.modularCarProfiles.Add(args[2]);
                                SendReply(player, string.Format("<color=#D3D3D3>You have added the vehicle profile {0} to the track</color>", args[2]));
                            }
                            return;
                        case "remove":
                            {
                                if (args.Length < 3)
                                {
                                    SendReply(player, "<color=#D3D3D3>You must enter vehicle profile name</color>");
                                    return;
                                }

                                if (trackData.modularCarProfiles == null)
                                {
                                    SendReply(player, "<color=#D3D3D3>The track does not have any vehicle profiles assigned</color>");
                                    return;
                                }

                                if (!trackData.modularCarProfiles.Contains(args[2]))
                                {
                                    SendReply(player, "<color=#D3D3D3>The track does not contain the specified vehicle profile</color>");
                                    return;
                                }
                                                                
                                trackData.modularCarProfiles.Remove(args[2]);
                                if (trackData.modularCarProfiles.Count == 0)
                                    trackData.modularCarProfiles = null;

                                SendReply(player, string.Format("<color=#D3D3D3>You have removed the vehicle profile {0} from the track</color>", args[2]));
                            }
                            return;
                        case "list":
                            {
                                if (trackData.modularCarProfiles == null)
                                {
                                    SendReply(player, "<color=#D3D3D3>The track does not have any vehicle profiles assigned</color>");
                                    SendReply(player, $"<color=#D3D3D3>Vehicle profiles available;</color> <color=#ce422b>{carData.Keys.ToSentence()}</color>");
                                    return;
                                }

                                SendReply(player, $"<color=#D3D3D3>Vehicle profiles currently assigned;</color> <color=#ce422b>{trackData.modularCarProfiles.ToSentence()}</color>");
                                SendReply(player, $"<color=#D3D3D3>Vehicle profiles available;</color> <color=#ce422b>{carData.Keys.ToSentence()}</color>");
                            }
                            return;
                        default:
                            break;
                    }
                    
                    return;
                case "breed":
                    if (args.Length < 2)
                    {
                        SendReply(player, "<color=#D3D3D3>You must enter a breed ID (from 0 - 9)</color>");
                        return;
                    }

                    int breed = 0;
                    if (!int.TryParse(args[1], out breed))
                    {
                        SendReply(player, "<color=#D3D3D3>You must enter a breed ID (from 0 - 9)</color>");
                        return;
                    }

                    breed = Mathf.Clamp(breed, 0, 9);

                    trackData.horseBreed = breed;
                    SendReply(player, string.Format("<color=#D3D3D3>You have set the horse breed to {0}</color>", breed));
                    return;
                case "commander":                    
                    bool useCommander;
                    if (args.Length < 2 || !bool.TryParse(args[1], out useCommander))
                    {
                        SendReply(player, "<color=#D3D3D3>You must enter either true or false</color>");
                        return;
                    }
                    trackData.useCommander = useCommander;
                    SendReply(player, string.Format("<color=#D3D3D3>You have set this race to {0} Boat/Car/HeliCommander</color>", useCommander ? "use" : "not use"));
                    return;
                case "cp":
                    if (args.Length < 2)
                    {
                        SendReply(player, "<color=#D3D3D3>Invalid Syntax. Type \"/track\" for help</color>");
                        return;
                    }

                    switch (args[1].ToLower())
                    {
                        case "add":
                            if (args.Length < 3)
                            {
                                SendReply(player, "<color=#D3D3D3>You must enter a checkpoint diameter</color>");
                                return;
                            }

                            float size = 0f;
                            if (!float.TryParse(args[2], out size))
                            {
                                SendReply(player, "<color=#D3D3D3>You must enter a checkpoint diameter</color>");
                                return;
                            }

                            trackData.checkPoints.Add(new TrackData.PointInfo(player.transform.position, player?.eyes?.rotation.eulerAngles.y ?? 0, size));
                            ShowPoint(player, player.transform.position, $"Checkpoint {trackData.checkPoints.Count}", size);
                            SendReply(player, string.Format("<color=#D3D3D3>You have added checkpoint number {0}</color>", trackData.checkPoints.Count));
                            return;
                        case "remove":
                            if (args.Length < 3)
                            {
                                SendReply(player, "<color=#D3D3D3>You must enter a check point number</color>");
                                return;
                            }
                            int number = 0;
                            if (!int.TryParse(args[2], out number))
                            {
                                SendReply(player, "<color=#D3D3D3>You must enter a check point number</color>");
                                return;
                            }

                            if (number > trackData.checkPoints.Count)
                            {
                                SendReply(player, string.Format("<color=#D3D3D3>This track only has {0} check points</color>", trackData.checkPoints.Count));
                                return;
                            }

                            trackData.checkPoints.RemoveAt(number - 1);
                            SendReply(player, string.Format("<color=#D3D3D3>You have successfully removed check point #{0}</color>", number));
                            return;
                        case "move":
                            if (args.Length < 3)
                            {
                                SendReply(player, "<color=#D3D3D3>You must enter a check point number</color>");
                                return;
                            }
                            int moveNumber = 0;
                            if (!int.TryParse(args[2], out moveNumber))
                            {
                                SendReply(player, "<color=#D3D3D3>You must enter a check point number</color>");
                                return;
                            }

                            if (moveNumber > trackData.checkPoints.Count)
                            {
                                SendReply(player, string.Format("<color=#D3D3D3>This track only has {0} check points</color>", trackData.checkPoints.Count));
                                return;
                            }

                            trackData.checkPoints[moveNumber - 1] = new TrackData.PointInfo(player.transform.position, player?.eyes?.rotation.eulerAngles.y ?? 0, trackData.checkPoints[moveNumber - 1].size);
                            ShowPoint(player, player.transform.position, $"Checkpoint {moveNumber}", trackData.checkPoints[moveNumber - 1].size, player?.eyes?.rotation.eulerAngles.y ?? 0);
                            SendReply(player, string.Format("<color=#D3D3D3>You have successfully moved grid point #{0}</color>", moveNumber));
                            return;
                        case "size":
                            if (args.Length < 4)
                            {
                                SendReply(player, "<color=#D3D3D3>You must enter a check point number and new radius</color>");
                                return;
                            }

                            int sizeNumber = 0;
                            if (!int.TryParse(args[2], out sizeNumber))
                            {
                                SendReply(player, "<color=#D3D3D3>You must enter a check point number</color>");
                                return;
                            }

                            float newSize = 0;
                            if (!float.TryParse(args[3], out newSize))
                            {
                                SendReply(player, "<color=#D3D3D3>You must enter a checkpoint diameter</color>");
                                return;
                            }

                            if (sizeNumber > trackData.checkPoints.Count)
                            {
                                SendReply(player, string.Format("<color=#D3D3D3>This track only has {0} check points</color>", trackData.checkPoints.Count));
                                return;
                            }

                            trackData.checkPoints[sizeNumber - 1].size = newSize;

                            ShowPoint(player, trackData.checkPoints[sizeNumber - 1].position, $"Checkpoint {sizeNumber - 1}", newSize, trackData.checkPoints[sizeNumber - 1].rotation);
                            SendReply(player, string.Format("<color=#D3D3D3>You have successfully changed the size of grid point #{0}</color>", sizeNumber));
                            return;
                        case "show":
                            float displayTime = 10;
                            if (args.Length > 2)
                                float.TryParse(args[2], out displayTime);

                            int i = 1;
                            foreach (TrackData.PointInfo checkPoint in trackData.checkPoints)
                            {
                                ShowPoint(player, checkPoint.position + Vector3.up, $"Checkpoint {i}", checkPoint.size, checkPoint.rotation, displayTime);
                                i++;
                            }
                            return;
                    }
                    return;
                case "gp":
                    if (args.Length < 2)
                    {
                        SendReply(player, "<color=#D3D3D3>Invalid Syntax. Type \"/track\" for help</color>");
                        return;
                    }

                    switch (args[1].ToLower())
                    {
                        case "add":
                            trackData.gridPoints.Add(new TrackData.PointInfo(player.transform.position, player?.eyes?.rotation.eulerAngles.y ?? 0));
                            ShowPoint(player, player.transform.position, $"Gridpoint {trackData.gridPoints.Count}", 0, player?.eyes?.rotation.eulerAngles.y ?? 0);
                            SendReply(player, string.Format("<color=#D3D3D3>You have added gridpoint number {0}</color>", trackData.gridPoints.Count));
                            return;
                        case "remove":
                            if (args.Length < 3)
                            {
                                SendReply(player, "<color=#D3D3D3>You must enter a grid point number</color>");
                                return;
                            }
                            int number = 0;
                            if (!int.TryParse(args[2], out number))
                            {
                                SendReply(player, "<color=#D3D3D3>You must enter a grid point number</color>");
                                return;
                            }

                            if (number > trackData.gridPoints.Count)
                            {
                                SendReply(player, string.Format("<color=#D3D3D3>This track only has {0} grid points</color>", trackData.gridPoints.Count));
                                return;
                            }

                            trackData.gridPoints.RemoveAt(number - 1);
                            SendReply(player, string.Format("<color=#D3D3D3>You have successfully removed grid point #{0}</color>", number));
                            return;
                        case "move":
                            if (args.Length < 3)
                            {
                                SendReply(player, "<color=#D3D3D3>You must enter a grid point number</color>");
                                return;
                            }
                            int moveNumber = 0;
                            if (!int.TryParse(args[2], out moveNumber))
                            {
                                SendReply(player, "<color=#D3D3D3>You must enter a grid point number</color>");
                                return;
                            }

                            if (moveNumber > trackData.gridPoints.Count)
                            {
                                SendReply(player, string.Format("<color=#D3D3D3>This track only has {0} grid points</color>", trackData.gridPoints.Count));
                                return;
                            }

                            trackData.gridPoints[moveNumber - 1] = new TrackData.PointInfo(player.transform.position, player?.eyes?.rotation.eulerAngles.y ?? 0);
                            ShowPoint(player, player.transform.position, $"Gridpoint {moveNumber}", 0, player?.eyes?.rotation.eulerAngles.y ?? 0);
                            SendReply(player, string.Format("<color=#D3D3D3>You have successfully moved grid point #{0}</color>", moveNumber));
                            return;
                        case "show": 
                            float displayTime = 10;
                            if (args.Length > 2)
                                float.TryParse(args[2], out displayTime);

                            int i = 1;
                            foreach (TrackData.PointInfo gridPoint in trackData.gridPoints)
                            {
                                ShowPoint(player, gridPoint.position + Vector3.up, $"Gridpoint {i}", 0, gridPoint.rotation, displayTime);
                                i++;
                            }
                            return;
                    }
                    return;
                default:
                    SendReply(player, "<color=#D3D3D3>Invalid Syntax. Type \"/track\" for help</color>");
                    return;
            }
        }

        [ChatCommand("racescores")]
        void cmdRaceScores(BasePlayer player, string command, string[] args)
        {
            if (scoreContainer == null)
            {
                SendReply(player, msg("scoreboard.nonesaved", player.UserIDString));
                return;
            }

            CuiHelper.AddUi(player, scoreContainer);
        }

        [ChatCommand("racevehicle")]
        void cmdSaveCar(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "racetrack.admin"))
                return;

            if (args.Length == 0)
            {
                SendReply(player, "<color=#ce422b>/racevehicle list</color> - Lists all vehicle profiles currently available");
                SendReply(player, "<color=#ce422b>/racevehicle remove \"profile name\"</color> - Lists all vehicle profiles currently available");
                SendReply(player, "<color=#ce422b>/racevehicle save \"profile name\" <tier></color> - Saves the modular car you are looking at to use in races. Tier is the quality of components that will be placed in the vehicle");
                return;
            }

            switch (args[0].ToLower())
            {
                case "list":
                    if (carData.Count == 0)
                        SendReply(player, "<color=#D3D3D3>No vehicles currently saved!</color>");
                    else SendReply(player, $"<color=#D3D3D3>Vehicles:</color> {carData.Select(x => x.Key).ToSentence()}");
                    return;
                case "remove":
                    if (args.Length == 2)
                    {
                        if (carData.ContainsKey(args[1]))
                        {
                            carData.Remove(args[1]);
                            SaveCarData();
                            SendReply(player, $"<color=#D3D3D3>You have removed the vehicle profile with the name <color=#ce422b>{args[1]}</color></color>");
                        }
                        else SendReply(player, $"<color=#D3D3D3>There is no vehicle profile with the name <color=#ce422b>{args[1]}</color></color>");
                    }
                    else goto default;
                    return;
                case "save":
                    if (args.Length == 3)
                    {
                        ModularCar modularCar = FindVehicle(player);
                        if (!modularCar)
                        {
                            SendReply(player, "<color=#D3D3D3>No modular vehicle found!</color>");
                            return;
                        }

                        int tier;
                        if (!int.TryParse(args[2], out tier))
                        {
                            SendReply(player, "<color=#D3D3D3>Invalid tier entered! It must be a number between <color=#ce422b>1 and 3</color></color>");
                            return;
                        }

                        carData[args[1]] = new ModularCarData(modularCar, Mathf.Clamp(tier, 1, 3));
                        SaveCarData();
                        SendReply(player, $"<color=#D3D3D3>Vehicle successfully saved as <color=#ce422b>{args[1]}</color></color>");
                    }
                    else goto default;
                    return;
                default:
                    SendReply(player, "<color=#D3D3D3>Invalid syntax!</color>");
                    break;
            }
            
        }

        private ModularCar FindVehicle(BasePlayer player)
        {
            Ray ray = new Ray(player.eyes.position, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 10f))
                return null;

            return hit.collider.GetComponentInParent<ModularCar>();            
        }

        [ConsoleCommand("race")]
        void ccmdRace(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
                return;

            if (arg.Args.Length == 0)
            {
                string message = "\nrace open - Open a race event"
                        + "\nrace openrandom - Opens a random race event"
                        + "\nrace start - Start a race event"
                        + "\nrace stop - Stop the current event"
                        + "\nrace select <trackname> - Set the next race track";                                        

                SendReply(arg, message);
                return;
            }

            if (!Current)
                Current = new GameObject().AddComponent<RaceManager>();

            switch (arg.Args[0].ToLower())
            {                
                case "open":
                    if (Current.Status != EventStatus.Finished)
                    {
                        SendReply(arg, string.Format("There is already an event {0}", Current.Status));
                        return;
                    }

                    if (!Current.HasTrackSet)
                    {
                        SendReply(arg, "You need to set a track before opening an event");
                        return;
                    }

                    if (Current.TrackData.raceType == RaceType.Car)
                    {
                        if (Current.TrackData.modularCarProfiles != null && Current.TrackData.modularCarProfiles.Count > 0)
                        {
                            for (int i = 0; i < Current.TrackData.modularCarProfiles.Count; i++)
                            {
                                if (!carData.ContainsKey(Current.TrackData.modularCarProfiles[i]))
                                {
                                    SendReply(arg, $"The selected track is setup to use a modular vehicle profile that does not exist. You must either remove the profile from your track data, or create a new profile with the same name ({Current.TrackData.modularCarProfiles[i]}). Unable to open race!");
                                    return;
                                }
                            }
                           
                        }
                    }

                    SendReply(arg, "Creating checkpoints, please wait!");
                    Current.OpenEvent();
                    return;

                case "openrandom":
                    if (Current.Status != EventStatus.Finished)
                    {
                        SendReply(arg, string.Format("There is already an event {0}", Current.Status));
                        return;
                    }

                    if (trackData.Count == 0)
                    {
                        SendReply(arg, "There are no tracks setup");
                        return;
                    }

                    KeyValuePair<string, TrackData> data = trackData.ElementAt(UnityEngine.Random.Range(0, trackData.Count));
                    Current.SetTrackData(data.Key, data.Value);

                    if (Current.TrackData.raceType == RaceType.Car)
                    {
                        if (Current.TrackData.modularCarProfiles != null && Current.TrackData.modularCarProfiles.Count > 0)
                        {
                            for (int i = 0; i < Current.TrackData.modularCarProfiles.Count; i++)
                            {
                                if (!carData.ContainsKey(Current.TrackData.modularCarProfiles[i]))
                                {
                                    SendReply(arg, $"The selected track ({data.Key}) is setup to use a modular vehicle profile that does not exist. You must either remove the profile from your track data, or create a new profile with the same name ({Current.TrackData.modularCarProfiles[i]}). Unable to open race!");
                                    return;
                                }
                            }

                        }
                    }

                    SendReply(arg, $"Track {data.Key} was randomly selected. Creating checkpoints, please wait!");
                    Current.OpenEvent();
                    return;

                case "start":                   
                    if (Current.Status != EventStatus.Open)
                    {
                        SendReply(arg, "You must open the event before starting it");
                        return;
                    }

                    if (Current.JoinerCount < 1)
                    {
                        SendReply(arg, "You can not start the event if there are no players");
                        return;
                    }

                    Current.StartEvent();
                    return;

                case "stop":
                    if (Current.Status == EventStatus.Loading)
                    {
                        SendReply(arg, "You must wait until the event has finished loading");
                        return;
                    }
                    if (Current.Status == EventStatus.Finishing)
                    {
                        SendReply(arg, "The last event is finishing up");
                        return;
                    }
                    if (Current.Status == EventStatus.Finished)
                    {
                        SendReply(arg, "There is no event in progress");
                        return;
                    }
                    else Current.EndEvent();
                    return;

                case "select":
                    if (arg.Args.Length != 2)
                    {
                        SendReply(arg, "You must enter a track name");
                        return;
                    }

                    if (!raceTracks.ContainsKey(arg.Args[1]))
                    {
                        SendReply(arg, string.Format("Unable to find a track with the name {0}", arg.Args[1]));
                        return;
                    }

                    if (Current.Status != EventStatus.Finished)
                    {
                        SendReply(arg, Current.Status.ToString());
                        SendReply(arg, "You can not set the track when an event is open or in progress");
                        return;
                    }

                    Current.SetTrackData(arg.Args[1], raceTracks[arg.Args[1]]);
                    SendReply(arg, string.Format("You have set the track to {0}", arg.Args[1]));
                    return;

                default:
                    SendReply(arg, "Invalid Syntax. Type \"race\" for help");
                    return;
            }
        }

        private void ShowPoint(BasePlayer player, Vector3 point, string text, float radius = 0, float rotation = 0, float time = 10)
        {
            bool isAdmin = player.HasPlayerFlag(BasePlayer.PlayerFlags.IsAdmin);

            if (!isAdmin)
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, true);
                player.SendNetworkUpdateImmediate();
            }
          
            player.SendConsoleCommand("ddraw.text", time, Color.green, point + new Vector3(0, 1.5f, 0), $"<size=40>{text}</size>");
            player.SendConsoleCommand("ddraw.box", time, Color.green, point, 1f);

            if (radius > 0)
                player.SendConsoleCommand("ddraw.sphere", time, Color.blue, point, radius);

            if (rotation != 0)            
                player.SendConsoleCommand("ddraw.arrow", time, Color.blue, point, point + (Quaternion.Euler(0, rotation, 0) * (Vector3.forward * 3)), 1f);

            if (!isAdmin)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.IsAdmin, false);
        }
        #endregion

        #region Config   
        private static ConfigData Configuration;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Event Automation")]
            public EventAutomation Automation { get; set; }

            [JsonProperty(PropertyName = "Event Timers")]
            public EventTimers Timers { get; set; }

            [JsonProperty(PropertyName = "Checkpoint Options")]
            public CheckpointOptions Checkpoints { get; set; }

            [JsonProperty(PropertyName = "Reward Options")]
            public RewardOptions Rewards { get; set; }

            [JsonProperty(PropertyName = "Event Entry Options")]
            public EntryOptions Entrance { get; set; }

            [JsonProperty(PropertyName = "Racer Options")]
            public RacerOptions Racers { get; set; }

            [JsonProperty(PropertyName = "UI Options")]
            public UIOptions UI { get; set; }

            [JsonProperty(PropertyName = "Blacklisted commands for event players")]
            public string[] CommandBlacklist { get; set; }

            [JsonProperty(PropertyName = "Disable spectate mode for finished racers")]
            public bool DisableSpectate { get; set; }
           
            public class EventAutomation
            {
                [JsonProperty(PropertyName = "Enable automated events")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Randomise automated events")]
                public bool Random { get; set; }

                [JsonProperty(PropertyName = "Minimum required players to start a auto event")]
                public int MinimumPlayers { get; set; }

                [JsonProperty(PropertyName = "Restrict auto-events between the set hours (24hr time)")]
                public TimeFrame Time { get; set; }

                [JsonProperty(PropertyName = "Event order for non-randomised events")]
                public List<string> Order { get; set; }

                public class TimeFrame
                {
                    public bool Enabled { get; set; }

                    public float Start { get; set; }

                    public float End { get; set; }
                }
            }

            public class EventTimers
            {
                [JsonProperty(PropertyName = "Time to close the event if minimum players has not been reached (seconds)")]
                public int CloseTime { get; set; }

                [JsonProperty(PropertyName = "Time to start the event when minimum players reached (seconds)")]
                public int StartTime { get; set; }

                [JsonProperty(PropertyName = "Time to finish racing after the first player has won (seconds)")]
                public int EndTime { get; set; }

                [JsonProperty(PropertyName = "Interval between automated events (seconds)")]
                public int Interval { get; set; }

                [JsonProperty(PropertyName = "Time from when racers are teleported to their race car until the race starts (seconds)")]
                public int Countdown { get; set; }
            }

            public class CheckpointOptions
            {
                [JsonProperty(PropertyName = "Spawn lights around checkpoint markers")]
                public bool Lights { get; set; }

                [JsonProperty(PropertyName = "Light type (Lantern, Flasher, Siren)")]
                public string LightType { get; set; }

                [JsonProperty(PropertyName = "Checkpoint marker block grade (Twigs, Wood, Stone, Metal, TopTier)")]
                public string Grade { get; set; }

                [JsonProperty(PropertyName = "Launch a firework when player crosses the finish line")]
                public bool LaunchFirework { get; set; }

                [JsonProperty(PropertyName = "Firework prefab path")]
                public string FireworkPrefab { get; set; }
            }

            public class EntryOptions
            {
                [JsonProperty(PropertyName = "Enable entrance fee")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Collect entrance fee from ServerRewards money")]
                public bool ServerRewards { get; set; }

                [JsonProperty(PropertyName = "Collect entrance fee from Economics money")]
                public bool Economics { get; set; }

                [JsonProperty(PropertyName = "Collect entrance fee using Scrap")]
                public bool Scrap { get; set; }

                [JsonProperty(PropertyName = "Fee amount")]
                public int Amount { get; set; }
            }

            public class RewardOptions
            {
                [JsonProperty(PropertyName = "Enable reward system")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Give prizes to all podium winners")]
                public bool Podium { get; set; }

                [JsonProperty(PropertyName = "Use ServerRewards as prize money")]
                public bool ServerRewards { get; set; }

                [JsonProperty(PropertyName = "Use Economics as prize money")]
                public bool Economics { get; set; }

                [JsonProperty(PropertyName = "Use Scrap as prize money")]
                public bool Scrap { get; set; }

                [JsonProperty(PropertyName = "Prize money for first place")]
                public int Prize1 { get; set; }

                [JsonProperty(PropertyName = "Prize money for second place")]
                public int Prize2 { get; set; }

                [JsonProperty(PropertyName = "Prize money for third place")]
                public int Prize3 { get; set; }
            }

            public class RacerOptions
            {
                [JsonProperty(PropertyName = "Disable metabolism while racing")]
                public bool DisableMetabolism { get; set; }

                [JsonProperty(PropertyName = "Racers clothing (chosen at random)")]
                public List<string[]> Clothing { get; set; }

                [JsonProperty(PropertyName = "Prevent racers from straying too far away from the target checkpoint")]
                public bool RestrictTravelDistance { get; set; }

                [JsonProperty(PropertyName = "Maximum wander distance")]
                public float MaximumWanderDistance { get; set; }
            }

            public class UIOptions
            {
                [JsonProperty(PropertyName = "Lap/Checkpoint counter")]
                public UICounter Counter { get; set; }

                [JsonProperty(PropertyName = "Lap/Checkpoint times")]
                public UICounter Times { get; set; }

                [JsonProperty(PropertyName = "Driver position")]
                public UICounter Position { get; set; }

                public class UICounter
                {
                    [JsonProperty(PropertyName = "Display to player")]
                    public bool Enabled { get; set; }

                    [JsonProperty(PropertyName = "Position - X minimum")]
                    public float Xmin { get; set; }

                    [JsonProperty(PropertyName = "Position - X maximum")]
                    public float XMax { get; set; }

                    [JsonProperty(PropertyName = "Position - Y minimum")]
                    public float YMin { get; set; }

                    [JsonProperty(PropertyName = "Position - Y maximum")]
                    public float YMax { get; set; }

                    [JsonProperty(PropertyName = "Background color (hex)")]
                    public string Color1 { get; set; }

                    [JsonProperty(PropertyName = "Background alpha")]
                    public float Color1A { get; set; }

                    [JsonProperty(PropertyName = "Status color (hex)")]
                    public string Color2 { get; set; }

                    private UI4 _position;

                    private string _backgroundColor;

                    private string _statusColor;

                    [JsonIgnore]
                    public UI4 Position
                    {
                        get
                        {
                            if (_position == null)
                                _position = new UI4(Xmin, YMin, XMax, YMax);
                            return _position;
                        }
                    }

                    [JsonIgnore]
                    public string BackgroundColor
                    {
                        get
                        {
                            if (string.IsNullOrEmpty(_backgroundColor))
                                _backgroundColor = RaceTrack.UI.Color(Color1, Color1A);
                            return _backgroundColor;
                        }
                    }
                }
            }

            public Oxide.Core.VersionNumber Version;
        }

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Automation = new ConfigData.EventAutomation
                {
                    Enabled = true,
                    Random = true,
                    Order = new List<string>(),
                    MinimumPlayers = 0,
                    Time = new ConfigData.EventAutomation.TimeFrame
                    {
                        Enabled = false,
                        Start = 6.0f,
                        End = 16.0f
                    }
                },
                Checkpoints = new ConfigData.CheckpointOptions
                {
                    Grade = "Metal",
                    Lights = true,
                    LightType = "Siren",
                    LaunchFirework = false,
                    FireworkPrefab = "assets/prefabs/deployable/fireworks/mortarchampagne.prefab",
                },
                CommandBlacklist = new string[] { "s", "tp", "tpr", "tpa", "home" },
                DisableSpectate = false,
                Entrance = new ConfigData.EntryOptions
                {
                    Amount = 100,
                    Economics = false,
                    ServerRewards = true,
                    Enabled = false,
                    Scrap = false,
                },
                Timers = new ConfigData.EventTimers
                {
                    CloseTime = 240,
                    StartTime = 45,
                    EndTime = 60,
                    Interval = 1800,
                    Countdown = 10
                },
                Racers = new ConfigData.RacerOptions
                {
                    Clothing = new List<string[]>
                    {
                        new string[] { "hazmatsuit" },
                        new string[] { "shoes.boots", "tshirt", "pants", "deer.skull.mask" }
                    },
                    DisableMetabolism = false,
                    RestrictTravelDistance = true,
                    MaximumWanderDistance = 30f
                },
                Rewards = new ConfigData.RewardOptions
                {
                    Economics = false,
                    Enabled = true,
                    Podium = true,
                    Prize1 = 250,
                    Prize2 = 125,
                    Prize3 = 50,
                    Scrap = false,
                    ServerRewards = true
                },
                UI = new ConfigData.UIOptions
                {
                    Counter = new ConfigData.UIOptions.UICounter
                    {
                        Color1 = "#F2F2F2",
                        Color1A = 0.05f,
                        Color2 = "#ce422b",
                        Enabled = true,
                        Xmin = 0.69f,
                        XMax = 0.83f,
                        YMin = 0.14f,
                        YMax = 0.175f
                    },
                    Times = new ConfigData.UIOptions.UICounter
                    {
                        Color1 = "#F2F2F2",
                        Color1A = 0.05f,
                        Color2 = "#ce422b",
                        Enabled = true,
                        Xmin = 0.69f,
                        XMax = 0.83f,
                        YMin = 0.22f,
                        YMax = 0.255f
                    },
                    Position = new ConfigData.UIOptions.UICounter
                    {
                        Color1 = "#F2F2F2",
                        Color1A = 0.05f,
                        Color2 = "#ce422b",
                        Enabled = true,
                        Xmin = 0.69f,
                        XMax = 0.83f,
                        YMin = 0.18f,
                        YMax = 0.215f                        
                    }
                },
                Version = Version
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Configuration = Config.ReadObject<ConfigData>();

            if (Configuration.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(Configuration, true);
        }

        protected override void LoadDefaultConfig() => Configuration = GetBaseConfig();

        protected override void SaveConfig() => Config.WriteObject(Configuration, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (Configuration.Version < new VersionNumber(0, 1, 78))
            {
                Configuration.Timers.Countdown = baseConfig.Timers.Countdown;
            }

            if (Configuration.Version < new VersionNumber(0, 1, 80))
                Configuration.Checkpoints.LightType = baseConfig.Checkpoints.LightType;

            if (Configuration.Version < new VersionNumber(0, 1, 88))
            {
                Configuration.Checkpoints.FireworkPrefab = baseConfig.Checkpoints.FireworkPrefab;
                Configuration.Checkpoints.LaunchFirework = false;
            }

            if (Configuration.Version < new VersionNumber(0, 1, 92))
                Configuration.Racers = baseConfig.Racers;

            if (Configuration.Version < new VersionNumber(0, 1, 99))
                Configuration.UI = baseConfig.UI;

            if (Configuration.Version < new VersionNumber(0, 2, 2))
                Configuration.Racers.RestrictTravelDistance = true;

            if (Configuration.Version < new VersionNumber(0, 2, 8))
            {
                Configuration.Automation.MinimumPlayers = 0;
                Configuration.Automation.Time = baseConfig.Automation.Time;

            }

            if (Configuration.Version < new VersionNumber(0, 2, 13))
                Configuration.Racers.MaximumWanderDistance = 30f;

            Configuration.Version = Version;
            PrintWarning("Config update completed!");
        }
        #endregion

        #region Data Management
        private void SaveTrackData()
        {
            trackData = raceTracks;
            trackdata.WriteObject(trackData);
        }

        private void SaveCarData()
        {
            cardata.WriteObject(carData);
        }

        private void SaveRestoreData() => restorationData.WriteObject(restoreData);

        private void SaveEntityData() => entitydata.WriteObject(spawnedEntities);

        private void LoadData()
        {
            try
            {
                trackData = trackdata.ReadObject<Hash<string, TrackData>>();
                raceTracks = trackData;
            }
            catch
            {
                trackData = new Hash<string, TrackData>();
            }

            try
            {
                carData = cardata.ReadObject<Hash<string, ModularCarData>>();
            }
            catch
            {
                carData = new Hash<string, ModularCarData>();
            }

            try
            {
                restoreData = restorationData.ReadObject<RestoreData>();
                if (restoreData.restoreData == null)
                    restoreData.restoreData = new Hash<ulong, RestoreData.PlayerData>();

            }
            catch
            {
                restoreData = new RestoreData();
            }
           
            try
            {
                spawnedEntities = entitydata.ReadObject<List<ulong>>();
            }
            catch
            {
                spawnedEntities = new List<ulong>();
            }
        }

        private class ModularCarData
        {
            public string chassis;

            public int tier = 2;

            public List<ModuleItem> moduleContainer;

            public ModularCarData() { }

            public ModularCarData(ModularCar modularCar, int tier)
            {
                chassis = modularCar.PrefabName;
                this.tier = tier;

                moduleContainer = new List<ModuleItem>();
                for (int i = 0; i < modularCar.Inventory.ModuleContainer.itemList.Count; i++)
                {
                    moduleContainer.Add(new ModuleItem(modularCar.Inventory.ModuleContainer.itemList[i]));
                }
            }

            public ModularCar LoadVehicle(Vector3 position, Quaternion rotation)
            {
                ModularCar modularCar = GameManager.server.CreateEntity(chassis, position, rotation) as ModularCar;
                modularCar.enableSaving = false;

                modularCar.spawnSettings.useSpawnSettings = false;

                modularCar.Spawn();

                for (int i = 0; i < moduleContainer.Count; i++)
                {
                    moduleContainer[i].Create(modularCar.Inventory.ModuleContainer);
                }

                modularCar.Invoke(()=> modularCar.AdminFixUp(tier), 1f);
                return modularCar;
            }

            public class ModuleItem
            {
                public int itemID;

                public int slot;

                public ModuleItem() { }

                public ModuleItem(Item item)
                {
                    itemID = item.info.itemid;
                    slot = item.position;
                }

                public void Create(ItemContainer target)
                {
                    Item item = ItemManager.CreateByItemID(itemID);
                    item.MoveToContainer(target, slot);
                }
            }
        }

        private class TrackData
        {
            public int laps, minPlayers, maxTime, horseBreed;
            public float totalDistance, modularDamageMulti = 1f;
            public bool useCommander = false, allowVehicleDamage = false;
            public List<string> modularCarProfiles;

            public RaceMode raceMode;
            public RaceType raceType;

            public List<PointInfo> checkPoints;
            public List<PointInfo> gridPoints;

            public bool UseModularCars { get { return raceType == RaceType.Car && (modularCarProfiles?.Count ?? 0) > 0; } }

            public TrackData()
            {
                checkPoints = new List<PointInfo>();
                gridPoints = new List<PointInfo>();
            }
            
            public class PointInfo
            {
                public Vector3 position;
                public float rotation, size;

                [JsonIgnore]
                public float SegmentDistance { get; set; } = -1f;

                public PointInfo() { }
                public PointInfo(Vector3 position, float rotation)
                {
                    this.position = position;
                    this.rotation = rotation;
                }
                public PointInfo(Vector3 position, float rotation, float size)
                {
                    this.position = position;
                    this.rotation = rotation;
                    this.size = size;
                }
            }            
        }

        public class RestoreData
        {
            public Hash<ulong, PlayerData> restoreData = new Hash<ulong, PlayerData>();

            public void AddData(BasePlayer player)
            {
                restoreData[player.userID] = new PlayerData(player);
            }

            public void AddPrizeToData(ulong playerId, int itemId, int amount)
            {
                PlayerData playerData;
                if (restoreData.TryGetValue(playerId, out playerData))
                {
                    ItemData itemData = FindItem(playerData, itemId);
                    if (itemData != null)
                        itemData.amount += amount;
                    else
                    {
                        Array.Resize<ItemData>(ref playerData.containerMain, playerData.containerMain.Length + 1);
                        playerData.containerMain[playerData.containerMain.Length - 1] = new ItemData() { amount = amount, condition = 100, contents = new ItemData[0], itemid = itemId, position = -1, skin = 0UL };
                    }                    
                }
            }

            private ItemData FindItem(PlayerData playerData, int itemId)
            {
                for (int i = 0; i < playerData.containerMain.Length; i++)
                {
                    ItemData itemData = playerData.containerMain[i];
                    if (itemData.itemid.Equals(itemId))
                        return itemData;
                }

                for (int i = 0; i < playerData.containerBelt.Length; i++)
                {
                    ItemData itemData = playerData.containerBelt[i];
                    if (itemData.itemid.Equals(itemId))
                        return itemData;
                }

                return null;
            }

            public void RemoveData(ulong playerId)
            {
                if (HasRestoreData(playerId))
                    restoreData.Remove(playerId);
            }

            public bool HasRestoreData(ulong playerId) => restoreData.ContainsKey(playerId);

            public void RestorePlayer(BasePlayer player)
            {
                PlayerData playerData;
                if (restoreData.TryGetValue(player.userID, out playerData))
                {
                    player.inventory.containerWear.SetFlag(ItemContainer.Flag.IsLocked, false);

                    player.inventory.Strip();

                    player.metabolism.Reset();

                    if (player.IsSleeping() || player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
                    {
                        Instance.timer.Once(1, () => RestorePlayer(player));
                        return;
                    }

                    Instance.NextTick(() =>
                    {
                        playerData.SetStats(player);
                        MovePosition(player, playerData.position, true);
                        RestoreAllItems(player, playerData);

                        player.RemoveFromTriggers();
                    });
                }
            }

            private void RestoreAllItems(BasePlayer player, PlayerData playerData)
            {
                if (!player || !player.IsConnected)
                    return;

                if (RestoreItems(player, playerData.containerBelt, "belt") && RestoreItems(player, playerData.containerWear, "wear") && RestoreItems(player, playerData.containerMain, "main"))
                    RemoveData(player.userID);
            }

            private bool RestoreItems(BasePlayer player, ItemData[] itemData, string type)
            {
                ItemContainer container = type == "belt" ? player.inventory.containerBelt : type == "wear" ? player.inventory.containerWear : player.inventory.containerMain;

                for (int i = 0; i < itemData.Length; i++)
                {
                    ItemData data = itemData[i];
                    if (data.amount < 1)
                        continue;

                    Item item = CreateItem(data);
                    item.position = data.position;
                    item.SetParent(container);
                }
                return true;
            }

            private Item CreateItem(ItemData itemData)
            {
                Item item = ItemManager.CreateByItemID(itemData.itemid, itemData.amount, itemData.skin);
                item.condition = itemData.condition;
                item.maxCondition = itemData.maxCondition;                

                if (itemData.frequency > 0)
                {
                    ItemModRFListener rfListener = item.info.GetComponentInChildren<ItemModRFListener>();
                    if (rfListener)
                    {                       
                        PagerEntity pagerEntity = BaseNetworkable.serverEntities.Find(item.instanceData.subEntity) as PagerEntity;
                        if (pagerEntity)
                        {
                            pagerEntity.ChangeFrequency(itemData.frequency);
                            item.MarkDirty();
                        }             
                    }
                }

                if (itemData.instanceData?.IsValid() ?? false)
                    itemData.instanceData.Restore(item);

                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon)
                {
                    if (!string.IsNullOrEmpty(itemData.ammotype))
                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(itemData.ammotype);
                    weapon.primaryMagazine.contents = itemData.ammo;
                }

                FlameThrower flameThrower = item.GetHeldEntity() as FlameThrower;
                if (flameThrower)
                    flameThrower.ammo = itemData.ammo;

                Chainsaw chainsaw = item.GetHeldEntity() as Chainsaw;
                if (chainsaw)
                    chainsaw.ammo = itemData.ammo;

                if (itemData.contents != null)
                {
                    foreach (ItemData contentData in itemData.contents)
                    {
                        Item newContent = ItemManager.CreateByItemID(contentData.itemid, contentData.amount);
                        if (newContent != null)
                        {
                            newContent.condition = contentData.condition;
                            newContent.MoveToContainer(item.contents);
                        }
                    }
                }
                return item;
            }

            public class PlayerData
            {
                public float[] stats;
                public Vector3 position;

                public ItemData[] containerMain;
                public ItemData[] containerWear;
                public ItemData[] containerBelt;

                public PlayerData() { }

                public PlayerData(BasePlayer player)
                {
                    stats = GetStats(player);
                    position = player.transform.position;

                    containerBelt = GetItems(player.inventory.containerBelt).ToArray();
                    containerMain = GetItems(player.inventory.containerMain).ToArray();
                    containerWear = GetItems(player.inventory.containerWear).ToArray();
                }

                private IEnumerable<ItemData> GetItems(ItemContainer container)
                {
                    return container.itemList.Select(item => new ItemData
                    {
                        itemid = item.info.itemid,
                        amount = item.amount,
                        ammo = item.GetHeldEntity() is BaseProjectile ? (item.GetHeldEntity() as BaseProjectile).primaryMagazine.contents : 
                               item.GetHeldEntity() is FlameThrower ? (item.GetHeldEntity() as FlameThrower).ammo :
                               item.GetHeldEntity() is Chainsaw ? (item.GetHeldEntity() as Chainsaw).ammo : 0,
                        ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                        position = item.position,
                        skin = item.skin,
                        condition = item.condition,
                        maxCondition = item.maxCondition,
                        frequency = ItemModAssociatedEntity<PagerEntity>.GetAssociatedEntity(item)?.GetFrequency() ?? -1,
                        instanceData = new ItemData.InstanceData(item),
                        contents = item.contents?.itemList.Select(item1 => new ItemData
                        {
                            itemid = item1.info.itemid,
                            amount = item1.amount,
                            condition = item1.condition
                        }).ToArray()
                    });
                }

                private float[] GetStats(BasePlayer player) => new float[] { player.health, player.metabolism.hydration.value, player.metabolism.calories.value };

                public void SetStats(BasePlayer player)
                {
                    player.health = stats[0];
                    player.metabolism.hydration.value = stats[1];
                    player.metabolism.calories.value = stats[2];
                    player.metabolism.SendChangesToClient();
                }                
            }

            public class ItemData
            {
                public int itemid;
                public ulong skin;
                public int amount;
                public float condition;
                public float maxCondition;
                public int ammo;
                public string ammotype;
                public int position;
                public int frequency;
                public InstanceData instanceData;
                public ItemData[] contents;

                public class InstanceData
                {
                    public int dataInt;
                    public int blueprintTarget;
                    public int blueprintAmount;
                    public ulong subEntity;

                    public InstanceData() { }
                    public InstanceData(Item item)
                    {
                        if (item.instanceData == null)
                            return;

                        dataInt = item.instanceData.dataInt;
                        blueprintAmount = item.instanceData.blueprintAmount;
                        blueprintTarget = item.instanceData.blueprintTarget;
                    }

                    public void Restore(Item item)
                    {
                        if (item.instanceData == null)
                            item.instanceData = new ProtoBuf.Item.InstanceData();

                        item.instanceData.ShouldPool = false;

                        item.instanceData.blueprintAmount = blueprintAmount;
                        item.instanceData.blueprintTarget = blueprintTarget;
                        item.instanceData.dataInt = dataInt;

                        item.MarkDirty();
                    }

                    public bool IsValid()
                    {
                        return dataInt != 0 || blueprintAmount != 0 || blueprintTarget != 0;
                    }
                }
            }
        }

        private class UnityVector3Converter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                Vector3 vector = (Vector3)value;
                writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    string[] values = reader.Value.ToString().Trim().Split(' ');
                    return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
                }
                JObject o = JObject.Load(reader);
                return new Vector3(Convert.ToSingle(o["x"]), Convert.ToSingle(o["y"]), Convert.ToSingle(o["z"]));
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3);
            }
        }
        #endregion

        #region Localization
        private static string msg(string key, string playerId = null) => Instance.lang.GetMessage(key, Instance, playerId);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["ui.lap"] = "LAP : <color={0}>{1} / {2}</color>",
            ["ui.checkpoint"] = "CHECKPOINT : <color={0}>{1} / {2}</color>",
            ["ui.laptime"] = "LAP TIME : <color={0}>{1}</color>",
            ["ui.position"] = "POSITION : <color={0}>{1}</color>",
            ["leave"] = "Leave",
            ["close"] = "Close",
            ["player"] = "Player",
            ["distance"] = "Distance",
            ["time"] = "Time",
            ["ui.info.laps"] = "<color=#D3D3D3>Lap Race  |  </color><color=#ce422b>{0} laps</color><color=#D3D3D3>  |  </color><color=#ce422b>{1} checkpoints</color>",
            ["ui.info.sprint"] = "<color=#D3D3D3>Sprint Race  |  </color><color=#ce422b>{0} checkpoints</color>",
            ["ui.info.distance"] = "<color=#ce422b>{0} M</color><color=#D3D3D3> total distance</color>",
            ["missedcp"] = "<color=#D3D3D3>You have missed a checkpoint. </color><color=#ce422b>Turn around!</color>",
            ["win.end"] = "<color=#ce422b>{0}</color> <color=#D3D3D3>has won the race! You have</color><color=#ce422b> {1} seconds</color> <color=#D3D3D3>to make it to the finish line!</color>",
            ["win.winners"] = "<color=#ce422b>The race is over!</color> <color=#D3D3D3>The winners are:</color> {0}",
            ["win.podium"] = "\n<color=#ce422b>{0}</color> <color=#D3D3D3>{1}</color>",
            ["finish.player"] = "<color=#D3D3D3>You placed <color=#ce422b>{0}</color></color>",
            ["finish.player.prize"] = "<color=#D3D3D3> and a prize of <color=#ce422b>{0}</color> has been added to your inventory</color>",
            ["vehicle.destroyed"] = "<color=#D3D3D3>You vehicle is too damaged to continue. You placed <color=#ce422b>{0}</color></color>",
            ["win.viewscores"] = "<color=#D3D3D3>Type <color=#ce422b>/racescores</color> to view the scoreboard!</color>",
            ["win.countdown"] = "<color=#D3D3D3>You have</color><color=#ce422b> {0} seconds</color> <color=#D3D3D3>to make it to the finish line!</color>",
            ["scoreboard.nonesaved"] = "<color=#D3D3D3>There is no scoreboard currently available</color>",
            ["countdown.count"] = "<color=#B20000>{0}</color>",
            ["countdown.go"] = "<color=#00CD00>GO!</color>",
            ["noevent"] = "<color=#D3D3D3>There is no event in progress</color>",
            ["leftevent"] = "<color=#ce422b>{0} </color><color=#D3D3D3>has left the event</color>",
            ["leftevent.wait"] = "<color=#ce422b>{0} </color><color=#D3D3D3>has left the event. Waiting for more players to start</color>",
            ["minplayers.reached"] = "<color=#D3D3D3>Minimum players has been reached. The event will start in</color> <color=#ce422b>{0} seconds</color>",
            ["joinevent"] = "<color=#ce422b>You have joined the race</color>",
            ["joinevent.player"] = "<color=#ce422b>{0}</color> <color=#D3D3D3>has joined the race</color>",
            ["eventfull"] = "<color=#D3D3D3>This event already has the maximum amount of contestents</color>",
            ["inevent"] = "<color=#D3D3D3>You are already in this event</color>",
            ["noevent"] = "<color=#D3D3D3>There is no event in progress</color>",
            ["nojoin"] = "<color=#D3D3D3>You can't join an event that is in progress</color>",
            ["cancelled"] = "<color=#ce422b>The event has been cancelled</color>",
            ["eventopen1"] = "<color=#D3D3D3>A {0} race event has been opened! You can join by typing</color> <color=#ce422b>/race join</color>",
            ["eventopenfee1"] = "<color=#D3D3D3>A {2} race event has been opened! Entrance to this race will cost you </color> <color=#ce422b>{0} {1}</color> <color=#D3D3D3>.\nYou can join by typing</color> <color=#ce422b>/race join</color>",
            ["eventprize"] = "<color=#D3D3D3>The prize for winning this event is</color><color=#ce422b> {0} {1}</color>",
            ["eventprizepodium"] = "<color=#D3D3D3>The prizes for this event are:</color>\n<color=#ce422b>First Place: {0} {1}\nSecond Place: {2} {1}\nThird Place: {3} {1}</color>",
            ["reset_vehicle"] = "<color=#D3D3D3>Type <color=#ce422b>/reset</color> <color=#D3D3D3>to reset your vehicle back at the last checkpoint</color></color>",
            ["eventfee"] = "<color=#D3D3D3>The fee to enter this event is </color><color=#ce422b>{0} {1}</color><color=#D3D3D3>. You do not have enough to enter</color>",
            ["economics"] = "coins",
            ["serverrewards"] = "RP",
            ["scrap"] = "Scrap",
            ["blacklistcmd"] = "<color=#939393>You can not run that command while you are racing!</color>",
            ["nopermission"] = "<color=#939393>You do not have permission to enter the race</color>",
            ["eventtimer"] = "<color=#939393>You have <color=#ce422b>{0}</color> to finish the race!</color>",
            ["spectatecycle"] = "<color=#D3D3D3>Press <color=#ce422b>JUMP</color> to cycle spectate targets</color>",
            ["wanderedoff"] = "<color=#D3D3D3>You travelled too far away from the track and was sent back to the last checkpoint</color>"
        };       
        #endregion
    }

    namespace Extensions
    {
        public static class Vector3Ext
        {
            public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
            {
                Vector3 AB = b - a;
                Vector3 AV = value - a;
                return Vector3.Dot(AV, AB) / Vector3.Dot(AB, AB);
            }

            public static float DistanceToLine(Vector3 a, Vector3 b, Vector3 c)
            {
                Vector3 direction = (a - b).normalized;
                return Vector3.Cross(direction, c - a).magnitude;
            }
        }
    }
}
