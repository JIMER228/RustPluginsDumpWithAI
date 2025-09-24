// Requires: GameModeManager
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Recoil Training Ground", "Amino", "0.4.2")]
    [Description("Practice and improve your aim on stationary and moving targets provided with a recoil pattern display of your spray.")]
    public class RecoilTrainingGround : RustPlugin, GameModeManager.IAdvancedLobby
    {
        private static GameModeManager _arena;
        public static RecoilTrainingGround _instance;
        private const string onShotRecordHook = "OnRecoilRecorded";
        private bool _isInitialized;
        private Configuration _config;
        private DynamicConfigFile _dataManager;
        private PluginData _pluginData;
        private AdvancedTrainingArenaData _arenaToEdit;
        private Dictionary<ulong, AdvancedTrainingPlayer> _players = new Dictionary<ulong, AdvancedTrainingPlayer>();
        private Dictionary<int, AdvancedTrainingArena> _arenas { get; set; } = new Dictionary<int, AdvancedTrainingArena>();
        public string _patternUiContainer = "recoiltrainingground.container";
        public const string _patternUi = "recoiltrainingground.pattern";
        private const float _ShotingCooldown = 1f;
        public static string _mainPanelJson;
        public const int _patternStartY = -20;
        public const int _patternYOffset = -6;
        public const int _patternPanelSizeY = 220;
        public const int _patternPanelSizeX = 150;
        private const int _border = 135;
        float _velocity;
        bool _clearedShots = false;
        //List<Vector3> angleVectors = new List<Vector3>();
        int shotIndex = 0;

        #region Classes                 
        public class AdvancedTrainingPlayerStats : GameModeManager.PlayerStatistics
        {
            public AdvancedTrainingPlayerStats(BasePlayer player) : base(player)
            {
            }
            public int Shots { get; set; }
            public int Hits { get; set; }
            public int Headshots { get; set; }
            public int BulletsFired { get; set; }
            public override void Reset()
            {
                base.Reset();
                Hits = 0;
                Headshots = 0;
                BulletsFired = 0;
                Shots = 0;
            }
        }

        #region Config

        public class Configuration
        {
            [JsonProperty("Symbol - Original Weapon Pattern")]
            public string PatternSymbol { get; set; } = "⦿";

            [JsonProperty("Symbol - Hit")]
            public string HitSymbol { get; set; } = "⦿";

            [JsonProperty("Symbol - Miss")]
            public string MissSymbol { get; set; } = "⦿";

            [JsonProperty("Color - Original Weapon Pattern")]
            public string PatternColor { get; set; } = "#ffffff";

            [JsonProperty("Color - Hit")]
            public string HitColor { get; set; } = "#00ff00";

            [JsonProperty("Color - Miss")]
            public string MissColor { get; set; } = "#ff0000";

            [JsonProperty("Font - Original Weapon Pattern")]
            public int PatternFont { get; set; } = 10;

            [JsonProperty("Font - Hit")]
            public int HitFont { get; set; } = 10;

            [JsonProperty("Font - Miss")]
            public int MissFont { get; set; } = 10;

            [JsonProperty("Hit - Max Tolerance")]
            public int HitMaxTolerance { get; set; } = 10;

            [JsonProperty(PropertyName = "Bots List")]
            public List<Bot> Bots { get; set; } = new List<Bot>();
        }

        public class Bot
        {
            [JsonProperty(PropertyName = "Name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "Loadout")]
            public string Loadout { get; set; }

            [JsonProperty(PropertyName = "Minimum Movement Speed")]
            public float MinimumMovementSpeed { get; set; }

            [JsonProperty(PropertyName = "Maximum Movement Speed")]
            public float MaximumMovementSpeed { get; set; }

            [JsonProperty(PropertyName = "Restrict To Named Spawn Point")]
            public bool RestrictToNamedSpawnPoint { get; set; }

            [JsonProperty(PropertyName = "Infinitive Health")]
            public bool InfinitiveHealth { get; set; }
        }
        #endregion Config

        #region Data
        public class AdvancedTrainingArenaData : GameModeManager.ArenaData
        {
            [JsonProperty(PropertyName = "Entrance Point - Display Text", Order = 16)]
            public string EntranceDisplayText { get; set; } = "$.Name";

            [JsonProperty(PropertyName = "Player Loadout", Order = 19)]
            public string PlayerLoadout { get; set; } = "";

            [JsonProperty(PropertyName = "Player Spawn Location", Order = 20)]
            public Vector3 PlayerSpawnLocation { get; set; } = Vector3.zero;

            [JsonProperty(PropertyName = "Performance Mode - Enabled", Order = 21)]
            public bool PerformanceMode { get; set; }//TODO: Set by commands

            [JsonProperty(PropertyName = "Bots", Order = 22)]
            public List<BotData> Bots { get; set; } = new List<BotData>();

            [JsonProperty(PropertyName = "Spawn Points", Order = 23)]
            public HashSet<ArenaSpawnData> SpawnLocations { get; set; } = new HashSet<ArenaSpawnData>();
        }
        public class BotData
        {
            [JsonProperty(PropertyName = "Bot Name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "Bot Count")]
            public int Count { get; set; }
        }
        public class ArenaSpawnData : GameModeManager.BaseSpawnData
        {
            [JsonProperty(PropertyName = "Spawn - Bot Name", Order = 1)]
            public string BotName { get; set; }
            public override bool Equals(object obj)
            {
                var spawnData = obj as ArenaSpawnData;
                if (spawnData == null)
                {
                    return false;
                }

                return spawnData.Name.Equals(Name, StringComparison.OrdinalIgnoreCase);
            }
            public override int GetHashCode()
            {
                return Name.ToLower().GetHashCode();
            }
        }
        public class AttachmentData
        {
            [JsonProperty(PropertyName = "Item Short Name")]
            public string ItemShortName { get; set; } = "";
        }
        public class RecoilData
        {
            [JsonProperty(PropertyName = "Weapon Short Name")]
            public string WeaponName { get; set; } = "";

            [JsonProperty(PropertyName = "Attachments")]
            public List<AttachmentData> Attachments { get; set; } = new List<AttachmentData>();

            [JsonProperty(PropertyName = "Recoil Points")]
            public List<int> RecoilPoints { get; set; } = new List<int>();

            public override bool Equals(object obj)
            {
                var targetData = obj as RecoilData;
                if (targetData == null || string.IsNullOrEmpty(targetData.WeaponName))
                    return false;
                //if (Attachements.Any() && targetData.Attachements.Any())
                //{
                var currentAttachments = Attachments.Any() ? Attachments
              .Select(a => a.ItemShortName)
              .Aggregate((a1, a2) => $"{a1}-{a2}") : "";
                currentAttachments = $"{currentAttachments}-{WeaponName}";

                var targetAttachments = targetData.Attachments.Any() ? targetData.Attachments
             .Select(a => a.ItemShortName)
             .Aggregate((a1, a2) => $"{a1}-{a2}") : "";
                targetAttachments = $"{targetAttachments}-{targetData.WeaponName}";

                return currentAttachments.ToLower() == targetAttachments.ToLower();
                //}
                //if (!string.IsNullOrEmpty(WeaponName) && !string.IsNullOrEmpty(targetData.WeaponName))
                //    return WeaponName.ToLower() == targetData.WeaponName.ToLower();
                //return false;

            }
            public override int GetHashCode()
            {
                if (!Attachments.Any())
                    return WeaponName.GetHashCode();
                var currentAttachments = Attachments
                   .Select(a => a.ItemShortName)
                   .Aggregate((a1, a2) => $"{a1}-{a2}");
                currentAttachments = $"{currentAttachments}-{WeaponName}";
                return currentAttachments.GetHashCode();
            }
        }
        public class PluginData
        {
            [JsonProperty(PropertyName = "Recoils")]
            public List<RecoilData> Recoils { get; set; } = new List<RecoilData>();

            [JsonProperty(PropertyName = "Arenas")]
            public List<AdvancedTrainingArenaData> Arenas { get; set; } = new List<AdvancedTrainingArenaData>();
        }
        #endregion Data

        #region Behaviours
        private class ShotRecorderBehaviour : MonoBehaviour
        {
            private List<int> _shots = new List<int>();
            private BaseProjectile _lastWeapon;
            private BasePlayer _player;
            private AdvancedTrainingPlayer matchPlayer;
            private float _firstShot = 0;
            private float _lastShotTime;

            public RecoilData RecoilPattern = new RecoilData();
            private int[] Shots => _shots.ToArray();

            private void Awake()
            {
                _player = GetComponent<BasePlayer>();
                _instance._players.TryGetValue(_player.userID, out matchPlayer);
            }

            private void Start()
            {
                InvokeRepeating(nameof(CheckPlayer), 60, 60);
                Enable();
            }

            public void Enable()
            {
                CuiHelper.DestroyUi(_player, _instance._patternUiContainer);
                CuiHelper.AddUi(_player, _mainPanelJson);
                Awake();
            }

            public void Disable()
            {
                CuiHelper.DestroyUi(_player, _instance._patternUiContainer);
            }

            private void OnDestroy()
            {
                Disable();
            }

            public void OnFired(BaseProjectile weapon)
            {
                CheckWeapon(weapon);
                RecordShot();
                _instance._clearedShots = false;
                var didHit = false;
                var index = _shots.Count - 1;
                var shotX = _shots[index];

                if (RecoilPattern == null || RecoilPattern.RecoilPoints.Count < _shots.Count)
                {
                    return;
                }

                var patternX = -RecoilPattern.RecoilPoints[index];

                if (Math.Abs(shotX) < _instance._config.HitMaxTolerance)
                {
                    shotX = patternX;
                    didHit = true;
                }

                _lastShotTime = Time.realtimeSinceStartup;
                ShowShot(_player, shotX, index, didHit);
                ShowScores(matchPlayer);
            }

            public void OnReloaded()
            {
                Interface.Call(onShotRecordHook, _player, Shots, RecoilPattern);
                _shots.Clear();
                ShowPattern(_player, RecoilPattern);
            }

            private void RecordShot()
            {
                var current = _player.eyes.rotation.eulerAngles.y;
                var value = 0f;
                if (_shots.Count == 0)
                {
                    _firstShot = current;
                    _shots.Add(0);
                    return;
                }

                if (_firstShot > _border)
                {
                    if (current < _border)
                    {
                        value = 360 - _firstShot + current;
                    }
                    else
                    {
                        value = current - _firstShot;
                    }
                }
                else
                {
                    if (current > _border)
                    {
                        value = -(360 - current);
                    }
                    else
                    {
                        value = current - _firstShot;
                    }
                }
                value *= 10;
                _shots.Add(Convert.ToInt32(value));
            }

            private void CheckWeapon(BaseProjectile weapon)
            {
                if (_lastWeapon != weapon)
                {
                    _lastWeapon = weapon;

                    RecoilPattern = _instance._pluginData.Recoils.FirstOrDefault(r => r.Equals(_instance.GetPlayerRecoilData(weapon.GetOwnerPlayer())));
                    if (RecoilPattern == null)
                    {
                        RecoilPattern = new RecoilData();
                    }

                    ClearShots();
                    return;
                }

                if (weapon != null && _shots.Count >= weapon.primaryMagazine.capacity)
                {
                    ClearShots();
                    return;
                }


            }
            void Update()
            {
                if (Time.realtimeSinceStartup > _lastShotTime + _ShotingCooldown && !_instance._clearedShots)
                {
                    _instance._clearedShots = true;
                    ClearShots();
                }
            }
            private void ClearShots()
            {
                _shots.Clear();
                ShowPattern(_player, RecoilPattern);
            }

            private void CheckPlayer()
            {
                if (_player.IsConnected == false)
                {
                    Destroy(this);
                }
            }
        }

        public class BotPlayer : MonoBehaviour
        {
            private Vector3 _moveTo;
            private Vector3 _moveFrom;
            private float _timeToTravel;
            private float _deltaTime;
            private float _movementSpeed;
            public BasePlayer Player { get; set; }
            //public AdvancedTrainingMatch AdvancedTrainingMatch => Match;
            public Bot Config { get; set; }
            public AdvancedTrainingMatch Match { get; set; }
            public AdvancedTrainingArena Arena { get; set; }
            public bool IsMoving { get; set; }
            public void SetRandomSpeed()
            {
                _movementSpeed = Core.Random.Range(Config.MinimumMovementSpeed, Config.MaximumMovementSpeed);
            }
            public static BotPlayer CreateInstance(AdvancedTrainingArena arena, AdvancedTrainingMatch match, BaseEntity botEntity)
            {
                // var gameObject = new GameObject();
                var botPlayer = botEntity.gameObject.AddComponent<BotPlayer>();
                var player = botEntity.gameObject.GetComponent<BasePlayer>();
                botPlayer.Player = player;
                botPlayer.Arena = arena;
                botPlayer.Match = match;

                return botPlayer;
            }
            public void Initial(Bot config)
            {
                Player = GetComponent<BasePlayer>();
                Config = config;
                _movementSpeed = Core.Random.Range(Config.MinimumMovementSpeed, Config.MaximumMovementSpeed);
                Player._lastSetName = string.Empty;
                Player.displayName = Config.Name;
                if (!string.IsNullOrWhiteSpace(Config.Loadout))
                {
                    _arena.GiveLoadout(Player, Config.Loadout);
                }
                Go();
            }
            public void Go()
            {
                //if (!Match.Arena.SpawnZones.Any())
                //{
                //    IsMoving = false;
                //    return;
                //}

                if (Player.IsSleeping())
                {
                    Player.EndSleeping();
                }
                var spawnZone = Arena.GetRandomSpawnLocationForBot(Config.Name, false);
                if (!spawnZone.HasValue)
                {
                    return;
                }
                _moveTo = spawnZone.Value;
                _moveFrom = Player.transform.position;
                if (_moveTo != transform.position)
                {
                    SetBotDirection(Quaternion.LookRotation(_moveTo - _moveFrom));
                }
                var distanceToTravel = Vector3.Distance(_moveFrom, _moveTo);
                _timeToTravel = distanceToTravel / _movementSpeed;
                _deltaTime = 0f;
                IsMoving = true;
                Arena.IsAnyBotMoving = true;
            }
            private void SetBotDirection(Quaternion angel)
            {
                if (angel.eulerAngles == default(Vector3))
                {
                    return;
                }
                Player.OverrideViewAngles(angel.eulerAngles);
                Player.SendNetworkUpdateImmediate();
            }
            private float GetLocationRealYInMap(Vector3 currentLocation)
            {
                RaycastHit raycastHit;
                var layerMask = LayerMask.GetMask("Construction", "Clutter", "World");
                var isRaycastHit = Physics.Raycast(currentLocation + Vector3.up, Vector3.down, out raycastHit, 30f, layerMask);
                var heightInMap = TerrainMeta.HeightMap.GetHeight(currentLocation);
                if (isRaycastHit)
                {
                    float locationY = Math.Max(raycastHit.point.y, heightInMap);
                    return locationY;
                }
                var location = new Vector3(currentLocation.x, heightInMap, currentLocation.z);
                return location.y;
            }
            private void FixedUpdate()
            {
                _deltaTime += Time.deltaTime;
                if (IsMoving && !Player.IsWounded() && _deltaTime >= 0.2f)
                {
                    if (_movementSpeed <= 0.5f)
                        return;
                    var movementPercentage = _deltaTime / _timeToTravel;
                    if (movementPercentage >= 1.0f)
                    {
                        IsMoving = false;
                        return;
                    }
                    var newLocation = Vector3.Lerp(_moveFrom, _moveTo, movementPercentage);
                    newLocation.y = GetLocationRealYInMap(newLocation);
                    Player.MovePosition(newLocation);
                    //Player.EnablePlayerCollider();
                }
                else if (!IsMoving)
                {
                    Go();
                }
            }
            public override bool Equals(object obj)
            {
                var botPlayer = obj as BotPlayer;
                if (botPlayer == null || botPlayer.Player == null || Player == null)
                {
                    return false;
                }

                return botPlayer.Player.userID == Player.userID;
            }
            public override int GetHashCode()
            {
                if (Player == null)
                {
                    return 0;
                }
                return Player.userID.GetHashCode();
            }
            public void DoDestroy()
            {
                if (!Player.IsDestroyed)
                    Player.Kill();
                if (Arena.Data.PerformanceMode)
                    Arena.Bots.Remove(this);
                else
                    Match.Bots.Remove(this);
                Destroy(this);
            }
        }

        #endregion Behaviours
        public static class ConsoleCommands
        {
            public const string ArenaHelp = "rtg.help";
            public const string ArenaEdit = "rtg.edit";
            public const string ArenaDone = "rtg.done";
            public const string ArenaDelete = "rtg.delete";
            public const string ArenaCreate = "rtg.create";
            public const string ArenaSet = "rtg.set";
            public const string ArenaSetHelp = "rtg.set.help";
            public const string ArenaSetSpawn = "rtg.set.spawn";
            public const string ArenaDeleteSpawn = "rtg.delete.spawn";
            public const string ArenaAllow = "rtg.allow";
            public const string ArenaBan = "rtg.ban";
            public const string ArenaBotAdd = "rtg.bot.add";
            public const string ArenaBotRemove = "rtg.bot.remove";


            public const string ArenaJoin = "rtg.join";
            public const string ArenaLeave = "rtg.leave";

            public const string ResetMatchStats = "rtg.match.reset.stats";
            public const string ResetMatch = "rtg.match.reset";
            public const string ResetLoadout = "rtg.match.reset.loadout";
            public const string UnlimitedAmmoToggle = "rtg.ammo.limit.toggle";

        }
        private static class Messages
        {
            public const string NoPermission = "No Permission";
            public const string WrongCommand = "Wrong Command";
            public const string ArenaCommandsList = "Arena Commands List";
            public const string AdvancedTrainingCommandsList = "Advanced Training Commands Help";
            public const string InvalidPlayerId = "Invalid Player Id";
            public const string PlayerNotFound = "Player Not Found";
            public const string LobbyNotFound = "Lobby Not Found";
            public const string ArenaNotFound = "Arena Not Found";
            public const string ChangesSaved = "Changes Saved";
            public const string NoEditingLobby = "No Editing Lobby";
            public const string NoEditingArena = "No Editing Arena";
            public const string ArenaParameterChanged = "Arena Parameter Changed";
            public const string ArenaSpawnPointAdded = "Arena Spawn Point Added";
            public const string ArenaInvalidPosition = "Arena Invalid Position";
            public const string ArenaInvalidEntrancePosition = "Arena Invalid Entrance Position";
            public const string ArenaDeleted = "Arena Deleted";
            public const string ArenaEditingStarted = "Arena Editing Started";
            public const string ArenaEditingDone = "Arena Editing Done";
            public const string ArenaNoAccess = "Arena No Access";
            public const string ArenaZoneNotFound = "Arena Zone Not Found";
            public const string LobbyZoneNotFound = "Lobby Zone Not Found";
            public const string ArenaCreated = "Arena Created";
            public const string SpawnPointNotFound = "Spawn Point Not Found";
            public const string ArenaSetValueHelp = "Arena Set Value Help";
            public const string UnlimitedAmmoEnabled = "Unlimited Ammo Enabled";
            public const string UnlimitedAmmoDisabled = "Unlimited Ammo Disabled";
        }
        public class AdvancedTrainingArena : GameModeManager.Arena
        {
            public AdvancedTrainingArena(GameModeManager.Lobby lobby, AdvancedTrainingArenaData arenaData) : base(lobby, arenaData)
            {
                Data = arenaData;
                Match = new Dictionary<Guid, AdvancedTrainingMatch>();
                if (arenaData.PerformanceMode)
                    BotManagementTimer = _instance.timer.Every(2, CheckBotAmounts);
            }
            public Timer BotManagementTimer { get; set; }
            public bool IsAnyBotMoving { get; set; }
            public List<BotPlayer> Bots { get; set; } = new List<BotPlayer>();
            public new Dictionary<Guid, AdvancedTrainingMatch> Match { get; set; }
            public new AdvancedTrainingArenaData Data { get; set; }
            public override string GetEntranceText()
            {
                var formattedText = Data.EntranceDisplayText?
                    .Replace("$.Name", Data.Name)
                    .Replace("$.ZoneName", Zone?.Name)
                    .Replace("$.TotalCount", Match.Count.ToString())
                    .Replace("$.MatchType", Data.MatchType.ToString());
                return formattedText ?? "";
            }
            public Vector3? GetRandomSpawnLocationForBot(string botName, bool restrictToName)
            {
                Vector2? randomPos = null;
                ArenaSpawnData spawnZone;
                var tryCount = 0;
                var randomChance = UnityEngine.Random.Range(0.1f, 100);
                List<ArenaSpawnData> validSpawnZones;
                if (restrictToName)
                {
                    validSpawnZones = Data.SpawnLocations.Where(x => x.IsEnabled && x.BotName.Equals(botName, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                else
                {
                    validSpawnZones = Data.SpawnLocations.Where(x => x.IsEnabled && (string.IsNullOrWhiteSpace(x.BotName) ||
                                                                     x.BotName.Equals(botName, StringComparison.OrdinalIgnoreCase))).ToList();
                }

                if (!validSpawnZones.Any())
                {
                    if (restrictToName)
                    {
                        return null;
                    }

                    spawnZone = new ArenaSpawnData { Radius = Zone.Radius, SpawnPoint = Zone.Location };
                }
                else
                {
                    spawnZone = validSpawnZones.Where(x => x.SpawnChance >= randomChance)
                        .ToList().GetRandom();
                }

                if (spawnZone.Radius > 0)
                {
                    while (tryCount < 20)
                    {
                        randomPos = UnityEngine.Random.insideUnitCircle * spawnZone.Radius;
                        var entities = Facepunch.Pool.GetList<BaseEntity>();
                        Vis.Entities(randomPos.Value, 10, entities, LayerMask.GetMask("Construction", "Deployable"));
                        var count = entities.Count;
                        Facepunch.Pool.FreeList(ref entities);
                        if (count == 0)
                        {
                            break;
                        }
                        randomPos = null;
                        tryCount++;
                    }
                }
                else
                {
                    randomPos = Vector2.zero;
                }

                return randomPos.HasValue
                    ? new Vector3(spawnZone.SpawnPoint.x + randomPos.Value.x, spawnZone.SpawnPoint.y, spawnZone.SpawnPoint.z + randomPos.Value.y)
                    : Vector3.zero;
            }
            public void CheckBotAmounts()
            {
                _instance.CheckBotAmount(this);
            }
        }
        public class AdvancedTrainingMatch : GameModeManager.Match
        {
            public AdvancedTrainingMatch(AdvancedTrainingArena arena) : base(arena)
            {
                arena.Match.Add(Id, this);
                Arena = arena;
            }
            public new AdvancedTrainingArena Arena { get; set; }
            public new HashSet<AdvancedTrainingPlayer> Players { get; set; } = new HashSet<AdvancedTrainingPlayer>();
            public List<BotPlayer> Bots { get; set; } = new List<BotPlayer>();
            public void SetMatchInitialData()
            {
                IsStarted = false;
                ResetStats();
            }
            public void ResetStats()
            {
                foreach (var matchPlayer in Players)
                {
                    matchPlayer.ResetStats();
                }
            }
            public void ResetLoadout()
            {
                foreach (var matchPlayer in Players)
                {
                    ResetLoadout(matchPlayer);
                }
            }
            public void ResetLoadout(AdvancedTrainingPlayer matchPlayer)
            {
                matchPlayer.ResetLoadout(Arena.Data.PlayerLoadout);
            }

        }
        public class AdvancedTrainingPlayer : GameModeManager.MatchPlayer
        {
            public AdvancedTrainingPlayer(BasePlayer player, AdvancedTrainingMatch match) : base(player, match)
            {
                Match = match;
                Statistics = new AdvancedTrainingPlayerStats(player);
                GiveLoadout(Match.Arena.Data.PlayerLoadout);
                if (!Match.Arena.Data.PerformanceMode)
                    BotManagementTimer = _instance.timer.Every(2, CheckBotAmounts);
            }
            //public AdvancedTrainingMatch AdvancedTrainingMatch => Match;

            public Timer BotManagementTimer { get; set; }
            public new AdvancedTrainingMatch Match { get; set; }
            public new AdvancedTrainingPlayerStats Statistics { get; set; }
            public bool UnlimitedAmmo { get; set; }
            public string WeaponName { get; set; }
            public void CheckBotAmounts()
            {
                _instance.CheckBotAmount(Match, PlayerId);
            }
            public void ResetLoadout(string loadoutName)
            {
                ClearInventory();
                GiveLoadout(loadoutName);
            }
            public void GiveLoadout(string loadoutName)
            {
                if (!string.IsNullOrWhiteSpace(loadoutName))
                {
                    _arena.GiveLoadout(Player, loadoutName);
                }
            }
            public override bool Equals(object obj)
            {
                var matchPlayer = obj as AdvancedTrainingPlayer;
                if (matchPlayer == null)
                {
                    return false;
                }

                return matchPlayer.PlayerId == PlayerId;
            }
            public override int GetHashCode()
            {
                return PlayerId.GetHashCode();
            }
        }

        #endregion Classes

        #region Commands

        #region ArenaDataCommands
        //rtg.help
        [ConsoleCommand(ConsoleCommands.ArenaHelp)]
        void ArenaHelp(ConsoleSystem.Arg conArgs)
        {
            var player = conArgs?.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, GameModeManager.PermissionAdmin))
            {
                _arena.ShowMessage(player, Messages.NoPermission, this);
                return;
            }
            _arena.ShowMessage(player, Messages.ArenaCommandsList, this, GetArenaCommands());
        }

        //rtg.edit <id>
        [ConsoleCommand(ConsoleCommands.ArenaEdit)]
        void EditArena(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                _arena.ShowMessage(null, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var player = conArgs.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, GameModeManager.PermissionAdmin))
            {
                _arena.ShowMessage(player, Messages.NoPermission, this);
                return;
            }

            //if (_lobbyToEdit == null)
            //{
            //    _arena.ShowMessage(player, Messages.NoEditingLobby, this, ConsoleCommands.LobbyEdit);
            //    return;
            //}

            var id = conArgs.GetInt(0, -1);
            if (id >= 0)
            {
                _arenaToEdit = _pluginData.Arenas.FirstOrDefault(x => x.Id == id);
                if (_arenaToEdit == null)
                {
                    _arena.ShowMessage(player, Messages.ArenaNotFound, this, GetArenaCommands());
                    return;
                }
                _arena.UnLockEntities(_arenaToEdit.ZoneId, player.userID);
                _arena.ShowMessage(player, Messages.ArenaEditingStarted, this, _arenaToEdit.Name, _arenaToEdit.Id);
            }
            else
            {
                _arena.ShowMessage(player, Messages.WrongCommand, this, GetArenaCommands());
            }
        }

        //rtg.done
        [ConsoleCommand(ConsoleCommands.ArenaDone)]
        void DoneArena(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                _arena.ShowMessage(null, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var player = conArgs.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, GameModeManager.PermissionAdmin))
            {
                _arena.ShowMessage(player, Messages.NoPermission, this);
                return;
            }

            if (_arenaToEdit != null)
            {
                _arena.LockEntities(_arenaToEdit.ZoneId);
                var name = _arenaToEdit.Name;
                var id = _arenaToEdit.Id;
                _arenaToEdit = null;
                _arena.ShowMessage(player, Messages.ArenaEditingDone, this, name, id);
            }
        }

        //rtg.delete <id>
        [ConsoleCommand(ConsoleCommands.ArenaDelete)]
        void DeleteArena(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                _arena.ShowMessage(null, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var player = conArgs.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, GameModeManager.PermissionAdmin))
            {
                _arena.ShowMessage(player, Messages.NoPermission, this);
                return;
            }

            //if (_lobbyToEdit == null)
            //{
            //    _arena.ShowMessage(player, Messages.NoEditingLobby, this, ConsoleCommands.LobbyEdit);
            //    return;
            //}

            var id = conArgs.GetInt(0, -1);
            if (id >= 0)
            {
                var arenaToRemove = _pluginData.Arenas.FirstOrDefault(x => x.Id == id);
                if (arenaToRemove != null)
                {
                    _pluginData.Arenas.Remove(arenaToRemove);
                    SaveData();
                    AdvancedTrainingArena teamDeathMatchArena;
                    _arenas.TryGetValue(arenaToRemove.Id, out teamDeathMatchArena);
                    RemoveArena(teamDeathMatchArena);
                    _arena.ShowMessage(player, Messages.ArenaDeleted, this);
                }
                else
                {
                    _arena.ShowMessage(player, Messages.ArenaNotFound, this);
                }
            }
            else
            {
                _arena.ShowMessage(player, Messages.WrongCommand, this, GetArenaCommands());
            }
        }

        //rtg.create <Name> <ZoneId> [<EntranceTriggerRadius>]
        [ConsoleCommand(ConsoleCommands.ArenaCreate)]
        void CreateArena(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                _arena.ShowMessage(null, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var player = conArgs.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, GameModeManager.PermissionAdmin))
            {
                _arena.ShowMessage(player, Messages.NoPermission, this);
                return;
            }

            if (conArgs.Args.Length < 2)
            {
                _arena.ShowMessage(player, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            if (_arena.LobbyToEdit == null)
            {
                _arena.ShowMessage(player, Messages.NoEditingLobby, this, GameModeManager.ConsoleCommands.LobbyEdit);
                return;
            }

            GameModeManager.Lobby lobby;
            if (!_arena.Lobbies.TryGetValue(_arena.LobbyToEdit.Id, out lobby) || lobby == null)
            {
                _arena.ShowMessage(player, Messages.LobbyNotFound, this);
                return;
            }
            var name = conArgs.GetString(0);
            var zoneId = conArgs.GetString(1);
            var radius = conArgs.GetFloat(2, 2);
            var entrancePosition = Vector3.zero;

            if (player != null)
            {
                entrancePosition = player.ServerPosition;
            }

            var zone = _arena.GetZone(zoneId);
            if (zone == null)
            {
                _arena.ShowMessage(player, Messages.ArenaZoneNotFound, this);
                return;
            }

            var lobbyZone = _arena.GetZone(_arena.LobbyToEdit.ZoneId);
            if (lobbyZone == null)
            {
                _arena.ShowMessage(player, Messages.LobbyZoneNotFound, this);
                return;
            }

            if (Vector3.Distance(entrancePosition, lobbyZone.Location) >= lobbyZone.Radius)
            {
                entrancePosition = Vector3.zero;
            }
            var id = (_arena.LobbyToEdit.Id * 100) + 1;
            var arenaIds = _arena.GetArenaIdsInLobby(_arena.LobbyToEdit.Id);

            if (arenaIds.Any())
            {
                id = arenaIds.Max() + 1;
            }

            var newArena = new AdvancedTrainingArenaData
            {
                Id = id,
                LobbyId = lobby.Data.Id,
                Name = name,
                ZoneId = zoneId,
                Capacity = 1,
                IsEnabled = true,
                MatchType = GameModeManager.MatchType.TrainingGround,
                EntranceLocation = entrancePosition,
                IsEntranceTriggerActive = entrancePosition != Vector3.zero,
                EntranceTriggerRadius = radius,
                AccessThroughLobbyOnly = false,
                RestrictAccess = false,
                SpawnLocations = new HashSet<ArenaSpawnData>()
            };


            _pluginData.Arenas.Add(newArena);
            _arenaToEdit = newArena;
            SaveData();
            var arena = new AdvancedTrainingArena(lobby, _arenaToEdit);
            if (arena.Match == null)
            {
                player.ChatMessage("Arena data is created but Match type is missing and arena is not activated!");//TODO: Lang
                return;
            }
            arena.Zone = zone;
            arena.Behaviour = GameModeManager.ArenaBehaviour.CreateEntrance($"Arena_EP_{newArena.Id}", _arenaToEdit.EntranceLocation,
                _arenaToEdit.EntranceTriggerRadius,
                _arenaToEdit.IsEntranceTriggerActive);
            arena.Behaviour.Initialize(arena);
            _arenas.Add(_arenaToEdit.Id, arena);
            _arena.ShowMessage(player, Messages.ArenaCreated, this);
        }

        //rtg.set <key> <value> [<key> <value>] [<key> <value>] ...
        [ConsoleCommand(ConsoleCommands.ArenaSet)]
        void SetArenaValue(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                _arena.ShowMessage(null, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var player = conArgs.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, GameModeManager.PermissionAdmin))
            {
                _arena.ShowMessage(player, Messages.NoPermission, this);
                return;
            }

            if (conArgs.Args.Length <= 1)
            {
                _arena.ShowMessage(player, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            if (_arenaToEdit == null)
            {
                _arena.ShowMessage(player, Messages.NoEditingArena, this, ConsoleCommands.ArenaEdit);
                return;
            }

            GameModeManager.Zone zone = null;
            var updateEntrance = false;
            var length = conArgs.Args.Length / 2;
            var index = 0;
            for (var i = 0; i < length; i++)
            {
                var key = conArgs.GetString(index).ToLower();
                index++;
                switch (key)
                {
                    case "name":
                        {
                            _arenaToEdit.Name = conArgs.GetString(index);
                            _arena.ShowMessage(player, Messages.ArenaParameterChanged, this, nameof(AdvancedTrainingArenaData.Name));
                            break;
                        }
                    case "zone":
                    case "zoneid":
                        {
                            var zoneId = conArgs.GetString(index);
                            zone = _arena.GetZone(zoneId);
                            if (zone == null)
                            {
                                _arena.ShowMessage(player, Messages.ArenaZoneNotFound, this);
                            }
                            else
                            {
                                _arenaToEdit.ZoneId = zoneId;
                                _arena.ShowMessage(player, Messages.ArenaParameterChanged, this, nameof(AdvancedTrainingArenaData.ZoneId));
                            }
                            break;
                        }
                    case "lobby":
                    case "lobbyid":
                        {
                            var lobbyId = conArgs.GetInt(index);

                            if (_arena.Lobbies.ContainsKey(lobbyId))
                            {
                                _arena.ShowMessage(player, Messages.LobbyNotFound, this);
                            }
                            else
                            {
                                _arenaToEdit.LobbyId = lobbyId;
                                _arena.ShowMessage(player, Messages.ArenaParameterChanged, this, nameof(AdvancedTrainingArenaData.LobbyId));
                            }
                            break;
                        }
                    case "enabled":
                    case "isenabled":
                        {
                            _arenaToEdit.IsEnabled = conArgs.GetBool(index);
                            updateEntrance = true;
                            _arena.ShowMessage(player, Messages.ArenaParameterChanged, this, nameof(AdvancedTrainingArenaData.IsEnabled));
                            break;
                        }
                    case "restrict":
                    case "restrictaccess":
                        {
                            _arenaToEdit.RestrictAccess = conArgs.GetBool(index);
                            _arena.ShowMessage(player, Messages.ArenaParameterChanged, this, nameof(AdvancedTrainingArenaData.RestrictAccess));
                            break;
                        }
                    case "entrance.active":
                        {
                            _arenaToEdit.IsEntranceTriggerActive = conArgs.GetBool(index);
                            updateEntrance = true;
                            _arena.ShowMessage(player, Messages.ArenaParameterChanged, this, nameof(AdvancedTrainingArenaData.IsEntranceTriggerActive));
                            break;
                        }
                    case "entrance.vr":
                    case "entrance.visibilityrange":
                    case "entrance.textvisibilityrange":
                        {
                            _arenaToEdit.EntrancePointTextVisibilityRange = conArgs.GetFloat(index);
                            updateEntrance = true;
                            _arena.ShowMessage(player, Messages.ArenaParameterChanged, this, nameof(AdvancedTrainingArenaData.EntrancePointTextVisibilityRange));
                            break;
                        }
                    case "entrance.pos":
                        {
                            var pos = conArgs.GetVector3(index, Vector3.zero);
                            if (pos.Equals(Vector3.zero))
                            {
                                if (conArgs.GetString(index).ToLower().Equals("here") && player != null)
                                {
                                    pos = player.ServerPosition;
                                }
                                else
                                {
                                    _arena.ShowMessage(player, Messages.WrongCommand, this, GetArenaCommands());
                                    break;
                                }
                            }
                            GameModeManager.Lobby lobby;
                            if (!_arena.Lobbies.TryGetValue(_arenaToEdit.LobbyId, out lobby) || lobby == null)
                            {
                                _arena.ShowMessage(player, Messages.LobbyNotFound, this);
                                break;
                            }
                            if (Vector3.Distance(player.ServerPosition, lobby.Zone.Location) >= lobby.Zone.Radius)
                            {
                                _arena.ShowMessage(player, Messages.ArenaInvalidPosition, this, pos);
                                break;
                            }
                            _arenaToEdit.EntranceLocation = pos;
                            updateEntrance = true;
                            _arena.ShowMessage(player, Messages.ArenaParameterChanged, this, nameof(AdvancedTrainingArenaData.EntranceLocation));
                            break;
                        }
                    case "entrance.r":
                    case "entrance.radius":
                        {
                            _arenaToEdit.EntranceTriggerRadius = conArgs.GetFloat(index);
                            updateEntrance = true;
                            _arena.ShowMessage(player, Messages.ArenaParameterChanged, this, nameof(AdvancedTrainingArenaData.EntranceTriggerRadius));
                            break;
                        }
                    case "access.lobbyonly":
                        {
                            _arenaToEdit.AccessThroughLobbyOnly = conArgs.GetBool(index);
                            _arena.ShowMessage(player, Messages.ArenaParameterChanged, this, nameof(AdvancedTrainingArenaData.AccessThroughLobbyOnly));
                            break;
                        }
                    case "kit.p":
                    case "kit.player":
                    case "loadout.p":
                    case "loadout.player":
                        {
                            _arenaToEdit.PlayerLoadout = conArgs.GetString(index);
                            _arena.ShowMessage(player, Messages.ArenaParameterChanged, this, nameof(AdvancedTrainingArenaData.PlayerLoadout));
                            break;
                        }
                    case "player.pos":
                    case "player.sp":
                    case "player.spawnposition":
                        {
                            var pos = conArgs.GetVector3(index, Vector3.zero);
                            if (pos.Equals(Vector3.zero))
                            {
                                if (conArgs.GetString(index).ToLower().Equals("here") && player != null)
                                {
                                    pos = player.ServerPosition;
                                }
                                else
                                {
                                    _arena.ShowMessage(player, Messages.WrongCommand, this, GetArenaCommands());
                                    break;
                                }
                            }
                            var arenaZone = _arena.GetZone(_arenaToEdit.ZoneId);
                            if (arenaZone == null)
                            {
                                _arena.ShowMessage(player, Messages.ArenaZoneNotFound, this);
                                return;
                            }
                            if (Vector3.Distance(player.ServerPosition, arenaZone.Location) >= arenaZone.Radius)
                            {
                                _arena.ShowMessage(player, Messages.ArenaInvalidPosition, this, pos);
                                break;
                            }
                            _arenaToEdit.PlayerSpawnLocation = pos;
                            _arena.ShowMessage(player, Messages.ArenaParameterChanged, this, nameof(AdvancedTrainingArenaData.PlayerSpawnLocation));
                            break;
                        }
                    case "text":
                    case "entrance.text":
                    case "entrance.displaytext":
                        {
                            _arenaToEdit.EntranceDisplayText = conArgs.GetString(index);
                            _arena.ShowMessage(player, Messages.ArenaParameterChanged, this, nameof(AdvancedTrainingArenaData.EntranceDisplayText));
                            break;
                        }
                    default:
                        break;
                }

                index++;
            }
            SaveData();

            AdvancedTrainingArena targetArena;
            if (_arenas.TryGetValue(_arenaToEdit.Id, out targetArena) && targetArena != null)
            {
                targetArena.Data = _arenaToEdit;
                if (zone != null)
                    targetArena.Zone = zone;
                if (updateEntrance)
                {
                    targetArena.Behaviour.DoDestroy();
                    targetArena.Behaviour = GameModeManager.ArenaBehaviour.CreateEntrance($"Arena_EP_{_arenaToEdit.Id}", _arenaToEdit.EntranceLocation,
                        _arenaToEdit.EntranceTriggerRadius,
                        _arenaToEdit.IsEntranceTriggerActive);
                    targetArena.Behaviour.Initialize(targetArena);
                }
            }
            _arena.ShowMessage(player, Messages.ChangesSaved, this);
        }

        //rtg.set.help
        [ConsoleCommand(ConsoleCommands.ArenaSetHelp)]
        void SetArenaValueHelp(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                _arena.ShowMessage(null, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var player = conArgs.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, GameModeManager.PermissionAdmin))
            {
                _arena.ShowMessage(player, Messages.NoPermission, this);
                return;
            }
            _arena.ShowMessage(player, Messages.ArenaSetValueHelp, this, GetArenaEditingKeys());
        }

        //rtg.bot.add <Name> <Count>
        [ConsoleCommand(ConsoleCommands.ArenaBotAdd)]
        void AddArenaBot(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                _arena.ShowMessage(null, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var player = conArgs.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, GameModeManager.PermissionAdmin))
            {
                _arena.ShowMessage(player, Messages.NoPermission, this);
                return;
            }

            if (_arenaToEdit == null)
            {
                _arena.ShowMessage(player, Messages.NoEditingArena, this, ConsoleCommands.ArenaEdit);
                return;
            }

            if (conArgs.Args.Length < 2)
            {
                _arena.ShowMessage(player, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var name = conArgs.GetString(0).ToLower();
            var count = conArgs.GetInt(1, 1);

            var botData = _arenaToEdit.Bots.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (botData == null)
            {
                botData = new BotData
                {
                    Name = name,
                    Count = count
                };
                _arenaToEdit.Bots.Add(botData);
            }
            else
            {
                botData.Count = count;
            }

            SaveData();
            AdvancedTrainingArena targetArena;
            if (_arenas.TryGetValue(_arenaToEdit.Id, out targetArena) && targetArena != null)
            {
                targetArena.Data = _arenaToEdit;
            }
            _arena.ShowMessage(player, Messages.ChangesSaved, this);
        }

        //rtg.bot.remove <Name>
        [ConsoleCommand(ConsoleCommands.ArenaBotRemove)]
        void RemoveArenaBot(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                _arena.ShowMessage(null, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var player = conArgs.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, GameModeManager.PermissionAdmin))
            {
                _arena.ShowMessage(player, Messages.NoPermission, this);
                return;
            }

            if (_arenaToEdit == null)
            {
                _arena.ShowMessage(player, Messages.NoEditingArena, this, ConsoleCommands.ArenaEdit);
                return;
            }

            if (conArgs.Args.Length < 1)
            {
                _arena.ShowMessage(player, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var name = conArgs.GetString(0).ToLower();

            var botData = _arenaToEdit.Bots.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (botData != null)
            {
                _arenaToEdit.Bots.Remove(botData);
            }

            SaveData();
            AdvancedTrainingArena targetArena;
            if (_arenas.TryGetValue(_arenaToEdit.Id, out targetArena) && targetArena != null)
            {
                targetArena.Data = _arenaToEdit;
            }
            _arena.ShowMessage(player, Messages.ChangesSaved, this);
        }

        //rtg.set.spawn <position> [<BotName>] [<radius>] [<chance>]
        [ConsoleCommand(ConsoleCommands.ArenaSetSpawn)]
        void SetArenaSpawn(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                _arena.ShowMessage(null, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var player = conArgs.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, GameModeManager.PermissionAdmin))
            {
                _arena.ShowMessage(player, Messages.NoPermission, this);
                return;
            }

            if (conArgs.Args.Length <= 0)
            {
                _arena.ShowMessage(player, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            if (_arenaToEdit == null)
            {
                _arena.ShowMessage(player, Messages.NoEditingArena, this, ConsoleCommands.ArenaEdit);
                return;
            }
            var number = 1;
            var lastItem = _arenaToEdit.SpawnLocations.OrderBy(x => x.Name).LastOrDefault();
            if (lastItem != null)
            {
                var numberString = lastItem.Name.Split('_').LastOrDefault();
                if (!string.IsNullOrWhiteSpace(numberString))
                {
                    if (int.TryParse(numberString, out number))
                    {
                        number++;
                    }
                    else
                    {
                        number = 1;
                    }
                }
            }
            var spawnPoint = new ArenaSpawnData
            {
                IsEnabled = true,
                Name = $"SP_{number:000}",
                Radius = 0,
                SpawnChance = 100,
            };
            var pos = conArgs.GetVector3(0, Vector3.zero);
            var name = conArgs.GetString(1).ToLower();
            if (!string.IsNullOrWhiteSpace(name))
            {
                spawnPoint.BotName = name;
            }
            if (pos.Equals(Vector3.zero))
            {
                if (conArgs.GetString(0).ToLower().Equals("here") && player != null)
                {
                    var zone = _arena.GetZone(_arenaToEdit.ZoneId);
                    if (zone == null)
                    {
                        _arena.ShowMessage(player, Messages.ArenaZoneNotFound, this);
                        return;
                    }
                    if (Vector3.Distance(player.ServerPosition, zone.Location) >= zone.Radius)
                    {
                        _arena.ShowMessage(player, Messages.ArenaInvalidPosition, this, pos);
                        return;
                    }
                    spawnPoint.SpawnPoint = player.ServerPosition;
                }
                else
                {
                    _arena.ShowMessage(player, Messages.WrongCommand, this, GetArenaCommands());
                }
            }
            else
            {
                var zone = _arena.GetZone(_arenaToEdit.ZoneId);
                if (zone == null)
                {
                    _arena.ShowMessage(player, Messages.ArenaZoneNotFound, this);
                    return;
                }
                if (Vector3.Distance(pos, zone.Location) >= zone.Radius)
                {
                    _arena.ShowMessage(player, Messages.ArenaInvalidPosition, this, pos);
                    return;
                }
                spawnPoint.SpawnPoint = pos;
            }

            spawnPoint.Radius = conArgs.GetFloat(2, 0);
            spawnPoint.SpawnChance = conArgs.GetInt(3, 100);
            _arenaToEdit.SpawnLocations.Add(spawnPoint);

            _arena.ShowMessage(player, Messages.ArenaSpawnPointAdded, this, spawnPoint.SpawnPoint);
            SaveData();
            AdvancedTrainingArena arena;
            if (_arenas.TryGetValue(_arenaToEdit.Id, out arena) && arena != null)
            {
                arena.Data = _arenaToEdit;
            }

            _arena.ShowMessage(player, Messages.ChangesSaved, this);
        }

        //rtg.delete.spawn <name>
        [ConsoleCommand(ConsoleCommands.ArenaDeleteSpawn)]
        void DeleteArenaSpawnPoint(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                _arena.ShowMessage(null, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var player = conArgs.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, GameModeManager.PermissionAdmin))
            {
                _arena.ShowMessage(player, Messages.NoPermission, this);
                return;
            }

            if (conArgs.Args.Length <= 0)
            {
                _arena.ShowMessage(player, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            if (_arenaToEdit == null)
            {
                _arena.ShowMessage(player, Messages.NoEditingArena, this, ConsoleCommands.ArenaEdit);
                return;
            }

            AdvancedTrainingArena arena;
            if (!_arenas.TryGetValue(_arenaToEdit.Id, out arena) || arena == null)
            {
                _arena.ShowMessage(player, Messages.ArenaNotFound, this);
                return;
            }

            var spawnPointName = conArgs.GetString(0).ToLower();
            var deleted = _arenaToEdit.SpawnLocations.RemoveWhere(x => x.Name.ToLower().Equals(spawnPointName));
            if (deleted == 0)
            {
                _arena.ShowMessage(player, Messages.SpawnPointNotFound, this);
                return;
            }
            arena.Data = _arenaToEdit;
            SaveData();
            _arena.ShowMessage(player, Messages.ChangesSaved, this);
        }

        //rtg.ban <add|remove> <arenaId> <playerId>
        [ConsoleCommand(ConsoleCommands.ArenaBan)]
        void ArenaBan(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                _arena.ShowMessage(null, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var player = conArgs.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, GameModeManager.PermissionAdmin))
            {
                _arena.ShowMessage(player, Messages.NoPermission, this);
                return;
            }

            if (conArgs.Args.Length <= 2)
            {
                _arena.ShowMessage(player, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var state = conArgs.GetString(0).ToLower();
            var arenaId = conArgs.GetInt(1);
            var playerId = conArgs.GetULong(2);
            AdvancedTrainingArena arena;
            if (!_arenas.TryGetValue(arenaId, out arena) || arena == null)
            {
                _arena.ShowMessage(player, Messages.ArenaNotFound, this);
                return;
            }

            var arenaToEdit = _pluginData.Arenas.FirstOrDefault(x => x.Id == arenaId);
            if (arenaToEdit == null)
            {
                _arena.ShowMessage(player, Messages.ArenaNotFound, this);
                return;
            }

            if (!playerId.IsSteamId())
            {
                _arena.ShowMessage(player, Messages.InvalidPlayerId, this);
                return;
            }

            var targetPlayer = BasePlayer.FindAwakeOrSleeping(playerId.ToString());
            if (targetPlayer == null)
            {
                _arena.ShowMessage(player, Messages.PlayerNotFound, this);
                return;
            }

            if (state.Equals("add"))
            {
                arenaToEdit.BannedPlayers.Add(playerId);
            }
            else if (state.Equals("remove"))
            {
                arenaToEdit.BannedPlayers.Remove(playerId);
            }
            SaveData();
            AdvancedTrainingArena targetArena;
            if (_arenas.TryGetValue(arenaToEdit.Id, out targetArena) && targetArena != null)
            {
                targetArena.Data = arenaToEdit;
            }
            _arena.ShowMessage(player, Messages.ChangesSaved, this);
        }

        //rtg.allow <add|remove> <arenaId> <playerId>
        [ConsoleCommand(ConsoleCommands.ArenaAllow)]
        void ArenaAccess(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                _arena.ShowMessage(null, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var player = conArgs.Player();
            if (player != null && !permission.UserHasPermission(player.UserIDString, GameModeManager.PermissionAdmin))
            {
                _arena.ShowMessage(player, Messages.NoPermission, this);
                return;
            }

            if (conArgs.Args.Length <= 2)
            {
                _arena.ShowMessage(player, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var state = conArgs.GetString(0).ToLower();
            var arenaId = conArgs.GetInt(1);
            var playerId = conArgs.GetULong(2);
            AdvancedTrainingArena arena;
            if (!_arenas.TryGetValue(arenaId, out arena) || arena == null)
            {
                _arena.ShowMessage(player, Messages.ArenaNotFound, this);
                return;
            }

            //var lobbyToEdit = _pluginData.Lobbies.FirstOrDefault(x => x.Id == arena.Lobby.Data.Id);
            //if (lobbyToEdit == null)
            //{
            //    _arena.ShowMessage(player, Messages.LobbyNotFound, this);
            //    return;
            //}
            var arenaToEdit = _pluginData.Arenas.FirstOrDefault(x => x.Id == arenaId);
            if (arenaToEdit == null)
            {
                _arena.ShowMessage(player, Messages.ArenaNotFound, this);
                return;
            }

            if (!playerId.IsSteamId())
            {
                _arena.ShowMessage(player, Messages.InvalidPlayerId, this);
                return;
            }

            var targetPlayer = BasePlayer.FindAwakeOrSleeping(playerId.ToString());
            if (targetPlayer == null)
            {
                _arena.ShowMessage(player, Messages.PlayerNotFound, this);
                return;
            }

            if (state.Equals("add"))
            {
                arenaToEdit.AllowedPlayers.Add(playerId);
            }
            else if (state.Equals("remove"))
            {
                arenaToEdit.AllowedPlayers.Remove(playerId);
            }
            SaveData();
            AdvancedTrainingArena targetArena;
            if (_arenas.TryGetValue(arenaToEdit.Id, out targetArena) && targetArena != null)
            {
                targetArena.Data = arenaToEdit;
            }
            _arena.ShowMessage(player, Messages.ChangesSaved, this);
        }

        #endregion ArenaDataCommands

        #region GUICommands

        //rtg.join <arenaId> 
        [ConsoleCommand(ConsoleCommands.ArenaJoin)]
        void ArenaJoin(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                _arena.ShowMessage(null, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var player = conArgs.Player();
            if (player == null)
            {
                _arena.ShowMessage(player, Messages.PlayerNotFound, this);
                return;
            }

            var arenaId = conArgs.GetInt(0);
            AdvancedTrainingArena arenaToJoin;
            if (!_arenas.TryGetValue(arenaId, out arenaToJoin) || arenaToJoin == null)
            {
                _arena.ShowMessage(player, Messages.ArenaNotFound, this);
                return;
            }
            GameModeManager.Ui.ClearAllMenus(player);
            TeleportToArena(player, arenaToJoin);
        }

        //rtg.leave
        [ConsoleCommand(ConsoleCommands.ArenaLeave)]
        void ArenaLeave(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                _arena.ShowMessage(null, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var player = conArgs.Player();
            if (player == null)
            {
                _arena.ShowMessage(player, Messages.PlayerNotFound, this);
                return;
            }

            LeaveAdvanceTraining(player);
        }

        //rtg.reset.stats
        [ConsoleCommand(ConsoleCommands.ResetMatchStats)]
        void ResetMatchStats(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                return;
            }

            var player = conArgs.Player();
            if (player == null)
            {
                return;
            }

            AdvancedTrainingPlayer matchPlayer;
            if (_players.TryGetValue(player.userID, out matchPlayer) && matchPlayer != null)
            {
                matchPlayer.Statistics.Reset();
                matchPlayer.Match.ResetStats();
                ShowScores(matchPlayer);
            }
        }

        //match.reset.loadout
        [ConsoleCommand(ConsoleCommands.ResetLoadout)]
        void ResetLoadout(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                return;
            }

            var player = conArgs.Player();
            if (player == null)
            {
                return;
            }

            AdvancedTrainingPlayer matchPlayer;
            if (_players.TryGetValue(player.userID, out matchPlayer) && matchPlayer != null)
                matchPlayer.Match.ResetLoadout();
        }

        //match.reset
        [ConsoleCommand(ConsoleCommands.ResetMatch)]
        void ResetMatch(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                return;
            }

            var player = conArgs.Player();
            if (player == null)
            {
                return;
            }

            AdvancedTrainingPlayer matchPlayer;
            if (_players.TryGetValue(player.userID, out matchPlayer) && matchPlayer != null)
            {
                if (!matchPlayer.Match.Arena.Data.PerformanceMode)
                    KillAllBots(matchPlayer.Match);
                matchPlayer.Statistics.Reset();
                matchPlayer.Reset();
            }
        }

        //rtg.ammo.limit.toggle
        [ConsoleCommand(ConsoleCommands.UnlimitedAmmoToggle)]
        void ToggleUnlimitedAmmo(ConsoleSystem.Arg conArgs)
        {
            if (conArgs == null)
            {
                _arena.ShowMessage(null, Messages.WrongCommand, this, GetArenaCommands());
                return;
            }

            var player = conArgs.Player();
            if (player == null)
            {
                return;
            }
            AdvancedTrainingPlayer matchPlayer;
            if (!_players.TryGetValue(player.userID, out matchPlayer) || matchPlayer == null)
            {
                _arena.ShowMessage(player, Messages.PlayerNotFound, this);
                return;
            }
            matchPlayer.UnlimitedAmmo = !matchPlayer.UnlimitedAmmo;
            _arena.ShowMessage(player, matchPlayer.UnlimitedAmmo ? Messages.UnlimitedAmmoEnabled : Messages.UnlimitedAmmoDisabled, this);
        }
        #endregion GUICommands
        #endregion Commands

        #region Methods
        public void CheckBotAmount(AdvancedTrainingMatch match, ulong playerId)
        {
            foreach (var botData in match.Arena.Data.Bots)
            {
                var currentBotCount = match.Bots.Count(x =>
                        x.Config.Name.Equals(botData.Name, StringComparison.InvariantCultureIgnoreCase));

                if (currentBotCount >= botData.Count)
                {
                    continue;
                }

                var neededBots = botData.Count - currentBotCount;
                for (var i = 0; i < neededBots; i++)
                {
                    var botConfig = _instance._config.Bots.FirstOrDefault(x => x.Name.Equals(botData.Name, StringComparison.OrdinalIgnoreCase));
                    if (botConfig == null)
                    {
                        continue;
                    }
                    var spawnPoint = match.Arena.GetRandomSpawnLocationForBot(botConfig.Name, botConfig.RestrictToNamedSpawnPoint);
                    if (!spawnPoint.HasValue)
                    {
                        continue;
                    }
                    var newBot = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", spawnPoint.Value, Quaternion.identity);
                    newBot.enableSaving = false;
                    newBot.OwnerID = playerId;
                    newBot.Spawn();

                    var botPlayer = BotPlayer.CreateInstance(match.Arena, match, newBot);
                    match.Bots.Add(botPlayer);
                    botPlayer.Initial(botConfig);
                    newBot.SetFlag(BaseEntity.Flags.Reserved2, true);
                }
            }
        }
        public void CheckBotAmount(AdvancedTrainingArena arena)
        {
            foreach (var botData in arena.Data.Bots)
            {
                var currentBotCount = arena.Bots.Count(x =>
                        x.Config.Name.Equals(botData.Name, StringComparison.InvariantCultureIgnoreCase));

                if (currentBotCount >= botData.Count)
                {
                    continue;
                }

                var botConfig = _instance._config.Bots.FirstOrDefault(x => x.Name.Equals(botData.Name, StringComparison.OrdinalIgnoreCase));
                if (botConfig == null)
                {
                    continue;
                }
                var spawnPoint = arena.GetRandomSpawnLocationForBot(botConfig.Name, botConfig.RestrictToNamedSpawnPoint);
                if (!spawnPoint.HasValue)
                {
                    continue;
                }
                var newBot = GameManager.server.CreateEntity("assets/prefabs/player/player.prefab", spawnPoint.Value, Quaternion.identity);
                newBot.enableSaving = false;
                newBot.Spawn();

                var botPlayer = BotPlayer.CreateInstance(arena, null, newBot);
                arena.Bots.Add(botPlayer);

                botPlayer.Initial(botConfig);
                newBot.SetFlag(BaseEntity.Flags.Reserved2, true);
            }
        }
        private void LeaveAdvanceTraining(BasePlayer player)
        {
            GameModeManager.Ui.ClearAllMenus(player);
            if (!_players.ContainsKey(player.userID))
                return;
            var playerArena = _players[player.userID].Match.Arena;
            if (playerArena == null)
                return;
            _arena.InventoryStrip(player);
            if (!playerArena.Data.PerformanceMode)
                KillAllBots(_players[player.userID].Match);

            RemoveShotRecoilRecorderBehaviour(player);
            player.limitNetworking = false;
            player.SendNetworkUpdateImmediate();
            playerArena.Match.Remove(_players[player.userID].Match.Id);
            _players[player.userID].BotManagementTimer?.Destroy();
            _players[player.userID].Match.Players.Remove(_players[player.userID]);
            _players.Remove(player.userID);
            GameModeManager.Ui.ClearAllMenus(player);
            _arena.TeleportToLobby(player, playerArena.Data.LobbyId, playerArena.Lobby.GetRandomSpawnLocation());
            TriggerNetworking(playerArena);

        }
        private void KillAllBots(AdvancedTrainingMatch match = null)
        {
            if (match == null)
            {
                foreach (var arena in _arenas.Values)
                {
                    if (arena.Data.PerformanceMode)
                    {
                        for (var i = arena.Bots.Count - 1; i >= 0; i--)
                        {
                            var bot = arena.Bots[i];
                            bot.DoDestroy();
                        }
                    }
                    else
                    {
                        foreach (var arenaMatch in arena.Match.Values)
                        {
                            for (var i = arenaMatch.Bots.Count - 1; i >= 0; i--)
                            {
                                var bot = arenaMatch.Bots[i];
                                bot.DoDestroy();
                            }
                        }
                    }
                }
            }
            else
            {
                for (var i = match.Bots.Count - 1; i >= 0; i--)
                {
                    var bot = match.Bots[i];
                    bot.DoDestroy();
                }
            }
        }
        private void EnterAdvancedTraining(BasePlayer player, int arenaId)
        {
            if (!_arenas.ContainsKey(arenaId))
                return;
            var arena = _arenas[arenaId];
            var newMatch = new AdvancedTrainingMatch(arena);
            var matchPlayer = new AdvancedTrainingPlayer(player, newMatch);
            arena.Match[newMatch.Id].Players.RemoveWhere(x => x.PlayerId == matchPlayer.PlayerId);
            arena.Match[newMatch.Id].Players.Add(matchPlayer);
            
            timer.In(2.5f, () =>
            {
                ShowAdvancedTrainingQuickMenu(player);
                ShowScores(matchPlayer);
                AddShotRecoilRecorderBehaviour(player);
            });
            
            _players[player.userID] = matchPlayer;
            
            
            TriggerNetworking(arena);
        }
        public void TriggerNetworking(AdvancedTrainingArena arena)
        {
            foreach (var trainingPlayer in _players.Values)
            {
                TriggerNetworking(trainingPlayer.Player);
                foreach (var bot in trainingPlayer.Match.Bots)
                {
                    TriggerNetworking(bot.Player);
                }
            }

            if (arena == null)
            {
                foreach (var arenaValue in _arenas.Values)
                {
                    if (arenaValue.Data.PerformanceMode)
                    {
                        foreach (var bot in arenaValue.Bots)
                        {
                            TriggerNetworking(bot.Player);
                        }
                    }
                }
            }
            else if (arena.Data.PerformanceMode)
            {
                foreach (var bot in arena.Bots)
                {
                    TriggerNetworking(bot.Player);
                }
            }
        }
        public void TriggerNetworking(BasePlayer player)
        {
            player.limitNetworking = true;
            player.limitNetworking = false;
            player.SendNetworkUpdate();
        }
        private void DestroyAllBots()
        {
            KillAllBots();
            foreach (var botPlayer in UnityEngine.Object.FindObjectsOfType<BotPlayer>())
            {
                if (botPlayer != null)
                {
                    botPlayer.DoDestroy();
                }
            }
        }
        void ArenaGenerate()
        {
            if (_arena == null)
            {
                return;
            }
            foreach (var arenaData in _pluginData.Arenas)
            {
                GameModeManager.Lobby lobby;
                if (!_arena.Lobbies.TryGetValue(arenaData.LobbyId, out lobby) || lobby == null)
                {
                    continue;
                }
                var arena = new AdvancedTrainingArena(lobby, arenaData)
                {
                    Data = arenaData,
                    Zone = _arena.GetZone(arenaData.ZoneId),
                    Behaviour = GameModeManager.ArenaBehaviour.CreateEntrance($"Arena_EP_{arenaData.Id}", arenaData.EntranceLocation,
                        arenaData.EntranceTriggerRadius,
                        arenaData.IsEntranceTriggerActive)
                };
                arena.Behaviour.Initialize(arena);
                _arenas.Add(arenaData.Id, arena);
                var players = _arena.GetZonePlayers(arena.Data.ZoneId);
                foreach (var basePlayer in players)
                {
                    _arena.TeleportToLobby(basePlayer.Player, arena.Lobby.Data.Id, arena.Lobby.GetRandomSpawnLocation());
                }
                _arena.TransportedPlayers.AddRange(players.Select(x => x.Player));
            }
        }
        public RecoilData GetPlayerRecoilData(BasePlayer player)
        {
            var heldEntity = player.GetHeldEntity();
            if (heldEntity != null)
            {
                var weaponName = heldEntity.ShortPrefabName;
                var heldItem = heldEntity.GetItem();
                if (heldItem != null)
                {
                    var items = heldItem.contents?.itemList;
                    if (items != null)
                    {
                        var tempRecoilData = new RecoilData();
                        foreach (var subItem in items)
                        {
                            if (subItem.info != null)
                            {
                                tempRecoilData.Attachments.Add(new AttachmentData
                                {
                                    ItemShortName = subItem.info.shortname
                                });
                            }
                        }
                        tempRecoilData.WeaponName = weaponName;
                        var exisitingData = _pluginData.Recoils.FirstOrDefault(a => a.Equals(tempRecoilData));
                        return exisitingData;
                    }
                }
                else
                {
                    var exisitingData = _pluginData.Recoils.FirstOrDefault(a => a.Equals(new RecoilData
                    {
                        WeaponName = weaponName
                    }));
                    return exisitingData;
                }

            }
            return null;
        }
        void RemoveArena(AdvancedTrainingArena arena)
        {
            arena.Behaviour.DoDestroy();

            foreach (var playerSet in arena.Match.Values.Select(m => m.Players))
            {
                foreach (var trainingPlayer in playerSet)
                {
                    var spawnLocation = arena.Lobby.GetRandomSpawnLocation();
                    var player = BasePlayer.FindByID(trainingPlayer.Player.userID);
                    _arena.TeleportToLobby(player, arena.Lobby.Data.Id, spawnLocation);
                }
            }

            _arenas.Remove(arena.Data.Id);
        }
        private string GetArenaCommands()
        {
            var commands = new StringBuilder(System.Environment.NewLine);
            commands.AppendLine("<color=#eb9534>Arena Console Commands:</color>");
            commands.AppendLine($"<color=#5af>{ConsoleCommands.ArenaHelp}</color> Get Arena management commands list");
            commands.AppendLine($"<color=#5af>{ConsoleCommands.ArenaEdit} <arenaId></color> Start editing an Arena");
            commands.AppendLine($"<color=#5af>{ConsoleCommands.ArenaDone}</color> Stop editing an Arena");
            commands.AppendLine($"<color=#5af>{ConsoleCommands.ArenaDelete} <arenaId></color> Delete an Arena");
            commands.AppendLine($"<color=#5af>{ConsoleCommands.ArenaCreate} <Name> <ZoneId> [<EntranceTriggerRadius>]</color> Create an Arena");
            commands.AppendLine($"<color=#5af>{ConsoleCommands.ArenaSet} <key> <value> [<key> <value>] [<key> <value>] ...</color> Set values for an editing Arena");
            commands.AppendLine($"<color=#5af>{ConsoleCommands.ArenaSetHelp}</color> Get list of available keys for editing the Arena");
            commands.AppendLine($"<color=#5af>{ConsoleCommands.ArenaSetSpawn} <position> [<BotName>] [<radius>] [<chance>]</color> Add bots spawns points for an editing Arena");
            commands.AppendLine($"<color=#5af>{ConsoleCommands.ArenaDeleteSpawn} <name></color> Delete a spawn point in Arena");
            commands.AppendLine($"<color=#5af>{ConsoleCommands.ArenaBotAdd} <name> <count>></color> Add a bot to Arena");
            commands.AppendLine($"<color=#5af>{ConsoleCommands.ArenaBotRemove} <name></color> Delete a bot in Arena");
            commands.AppendLine($"<color=#5af>{ConsoleCommands.ArenaAllow} <add|remove> <arenaId> <playerId></color> Add/Remove player to the arena whitelist");
            commands.AppendLine($"<color=#5af>{ConsoleCommands.ArenaBan} <add|remove> <arenaId> <playerId></color> Ban/Unban player from the arena");
            commands.AppendLine("<color=#5582ff>For passing the position you can use 'here' to send your current position or use X,Y,Z coordinates</color>");
            return commands.ToString();
        }
        private string GetArenaEditingKeys()
        {
            var keys = new StringBuilder(Environment.NewLine);
            keys.AppendLine("<color=#5af>name</color>");
            keys.AppendLine("<color=#5af>zone</color> or <color=#5af>zoneId</color>");
            keys.AppendLine("<color=#5af>lobby</color> or <color=#5af>lobbyId</color>");
            keys.AppendLine("<color=#5af>enabled</color> or <color=#5af>isEnabled</color>");
            keys.AppendLine("<color=#5af>entrance.pos</color>");
            keys.AppendLine("<color=#5af>entrance.active</color>");
            keys.AppendLine("<color=#5af>entrance.r</color> or <color=#5af>entrance.radius</color>");
            keys.AppendLine("<color=#5af>access.lobbyOnly</color>");
            keys.AppendLine("<color=#5af>entrance.vr</color> or <color=#5af>entrance.visibilityRange</color> or <color=#5af>entrance.textVisibilityRange</color>");
            keys.AppendLine("<color=#5af>restrict</color> or <color=#5af>restrictAccess</color>");
            keys.AppendLine("<color=#5af>kit.p</color> or <color=#5af>kit.player</color> or <color=#5af>loadout.p</color> or <color=#5af>loadout.player</color>");
            keys.AppendLine("<color=#5af>player.pos</color> or <color=#5af>player.sp</color> or <color=#5af>player.spawnPosition</color>");
            keys.AppendLine("<color=#5af>text</color> or <color=#5af>entrance.text</color> or <color=#5af>entrance.displayText</color>");
            keys.AppendLine($"{Environment.NewLine}Available placeholders for arena entrance text are:{GetArenaPlaceholders()}");
            return keys.ToString();
        }
        private string GetArenaPlaceholders()
        {
            var placeholders = new StringBuilder(Environment.NewLine);
            placeholders.AppendLine("<color=#5af>$.Name</color> for arena name");
            placeholders.AppendLine("<color=#5af>$.ZoneName</color> for arena zone name");
            placeholders.AppendLine("<color=#5af>$.TotalCount</color> for arena active players count");
            placeholders.AppendLine("<color=#5af>$.MatchType</color> for arena match type");
            return placeholders.ToString();
        }
        private void RemoveFromArenas(BasePlayer player)
        {
            var activeArena = _arenas.Values.FirstOrDefault(x => x.Match.Any()
                && x.Match.Any(m => m.Value.Players.Any(p => p.PlayerId == player.userID)));

            if (activeArena != null)
            {
                LeaveAdvanceTraining(player);
                return;
            }
        }

        public void TeleportToArena(BasePlayer player, AdvancedTrainingArena arena)
        {
            if (player == null || player.IsNpc || arena == null)
                return;
            var isVip = (!arena.Data.RestrictAccess || arena.Data.AllowedPlayers.Contains(player.userID)) && !arena.Data.BannedPlayers.Contains(player.userID);
            if (isVip)
            {
                var spawnLocation = arena.Data.PlayerSpawnLocation;
                if (spawnLocation.Equals(Vector3.zero))
                {
                    spawnLocation = arena.Zone.Location;
                }

                RemoveFromArenas(player);
                    EnterAdvancedTraining(player, arena.Data.Id);
                
               
                _arena.ResetPlayer(player);
                arena.Lobby.ActivePlayers.RemoveWhere(x => x.Player.userID == player.userID);
                _arena.Teleport(player, spawnLocation);
            }
            else
            {
                _arena.ShowMessage(player, Messages.ArenaNoAccess, this);
            }
        }

        #region GUI
        private static void ShowScores(AdvancedTrainingPlayer matchPlayer)
        {
            if (matchPlayer == null)
                return;
            var mainContainer = GameModeManager.Ui.Container(GameModeManager.Ui.Panels.MatchPlayers,
                GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.Black, 0f),
                GameModeManager.Ui.GetMin(0, 0), GameModeManager.Ui.GetMax(1, 1),
                false,
                false,
                GameModeManager.Ui.Panels.Under);
            #region stats -> kills


            GameModeManager.Ui.Panel(ref mainContainer,
              GameModeManager.Ui.Panels.MatchPlayers,
              GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.ButtonBlack, 0.9f),
               $"{matchPlayer.Statistics.Kills} BOTS",
              GameModeManager.Ui.GetMin(1750, 459),
              GameModeManager.Ui.GetMax(36, 581),
              20);
            #endregion stats -> kills
            #region stats -> accuracy

            GameModeManager.Ui.Panel(ref mainContainer,
              GameModeManager.Ui.Panels.MatchPlayers,
              GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.ButtonBlack, 0.9f),
               $"{(matchPlayer.Statistics.Shots > 0 ? matchPlayer.Statistics.Hits * 100 / matchPlayer.Statistics.Shots : 0)} %",
              GameModeManager.Ui.GetMin(1750, 503),
              GameModeManager.Ui.GetMax(36, 537),
              20);
            #endregion stats -> accuracy
            #region stats -> body shots


            GameModeManager.Ui.Panel(ref mainContainer,
              GameModeManager.Ui.Panels.MatchPlayers,
              GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.ButtonBlack, 0.9f),
              (matchPlayer.Statistics.Hits - matchPlayer.Statistics.Headshots).ToString(),
              GameModeManager.Ui.GetMin(1750, 547),
              GameModeManager.Ui.GetMax(105, 493),
              20);
            GameModeManager.Ui.Panel(ref mainContainer,
              GameModeManager.Ui.Panels.MatchPlayers,
              GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.ButtonBlack, 0.9f),
              matchPlayer.Statistics.Shots.ToString(),
              GameModeManager.Ui.GetMin(1819, 547),
              GameModeManager.Ui.GetMax(36, 493),
              20);
            #endregion  stats -> body shots
            #region stats -> headshots

            GameModeManager.Ui.Panel(ref mainContainer,
             GameModeManager.Ui.Panels.MatchPlayers,
             GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.ButtonBlack, 0.9f),
             matchPlayer.Statistics.Headshots.ToString(),
             GameModeManager.Ui.GetMin(1750, 591),
             GameModeManager.Ui.GetMax(105, 449),
             20);
            GameModeManager.Ui.Panel(ref mainContainer,
              GameModeManager.Ui.Panels.MatchPlayers,
              GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.ButtonBlack, 0.9f),
              matchPlayer.Statistics.Shots.ToString(),
              GameModeManager.Ui.GetMin(1819, 591),
              GameModeManager.Ui.GetMax(36, 449),
              20);
            #endregion stats -> headshots
            #region stats -> hits

            GameModeManager.Ui.Panel(ref mainContainer,
             GameModeManager.Ui.Panels.MatchPlayers,
             GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.ButtonBlack, 0.9f),
             matchPlayer.Statistics.Hits.ToString(),
             GameModeManager.Ui.GetMin(1750, 635),
             GameModeManager.Ui.GetMax(105, 405),
             20);
            GameModeManager.Ui.Panel(ref mainContainer,
              GameModeManager.Ui.Panels.MatchPlayers,
              GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.ButtonBlack, 0.9f),
              matchPlayer.Statistics.Shots.ToString(),
              GameModeManager.Ui.GetMin(1819, 635),
              GameModeManager.Ui.GetMax(36, 405),
              20);
            #endregion stats -> hits
            CuiHelper.DestroyUi(matchPlayer.Player, GameModeManager.Ui.Panels.MatchPlayers.ToString());
            CuiHelper.AddUi(matchPlayer.Player, mainContainer);
        }
        private static void ShowPattern(BasePlayer player, RecoilData pattern)
        {
            var container = new CuiElementContainer
            {
                new CuiElement
                {
                    Parent = _instance._patternUiContainer,
                    Name = _patternUi,
                    Components =
                    {
                        new CuiImageComponent {Color = "0 0 0 0",},
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                    }
                }
            };

            if (pattern != null)
            {
                for (var i = pattern.RecoilPoints.Count; i > 0; i--)
                {
                    var value = -pattern.RecoilPoints[i - 1];
                    var x = _patternPanelSizeX / 2 + value;
                    var y = i * _patternYOffset + _patternStartY;
                    if (x < 0 || x > _patternPanelSizeX)
                    {
                        continue;
                    }

                    if (Math.Abs(y) > _patternPanelSizeY + _patternYOffset)
                    {
                        continue;
                    }
                    container.Add(new CuiElement
                    {
                        Parent = _patternUi,
                        Components =
                        {
                            new CuiTextComponent
                            {
                                Color = GameModeManager.Ui.Color(_instance._config.PatternColor),
                                Text = _instance._config.PatternSymbol,
                                Align = TextAnchor.MiddleCenter,
                                FontSize = _instance._config.PatternFont,
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 1",
                                AnchorMax = "0 1",
                                OffsetMin = $"{x - 15} {y - 15}",
                                OffsetMax = $"{x + 15} {y + 15}"
                            }
                        }
                    });
                }
            }

            CuiHelper.DestroyUi(player, _patternUi);
            CuiHelper.AddUi(player, container);
        }
        private static void ShowShot(BasePlayer player, int shotValue, int shotCount, bool hit)
        {
            var x = _patternPanelSizeX / 2 + shotValue;
            var y = shotCount * _patternYOffset + _patternStartY;

            if (x < 0 || x > _patternPanelSizeX)
            {
                return;
            }

            if (Math.Abs(y) > _patternPanelSizeY + _patternYOffset)
            {
                return;
            }
            //_instance.PrintError($"X: {x} Y: {y}");
            var container = new CuiElementContainer
            {
                new CuiElement
                {
                    Parent = _patternUi,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Color = hit ? GameModeManager.Ui.Color(_instance._config.HitColor) : GameModeManager.Ui.Color(_instance._config.MissColor),
                            Text = hit ?_instance._config.HitSymbol:_instance._config.MissSymbol,
                            Align = TextAnchor.MiddleCenter,
                            FontSize = hit? _instance._config.HitFont: _instance._config.MissFont,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1",
                            AnchorMax = "0 1",
                            OffsetMin = $"{x - 15} {y - 15}",
                            OffsetMax = $"{x + 15} {y + 15}"
                        }
                    }
                }
            };

            CuiHelper.AddUi(player, container);
        }
        private void ShowAdvancedTrainingQuickMenu(BasePlayer player)
        {
            var mainContainer = GameModeManager.Ui.Container(GameModeManager.Ui.Panels.QuickMenu,
                GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.Black, 0f),
                GameModeManager.Ui.GetMin(0, 0), GameModeManager.Ui.GetMax(1, 1),
                false, false, GameModeManager.Ui.Panels.Hud);
            #region container panel
            GameModeManager.Ui.Panel(ref mainContainer,
                GameModeManager.Ui.Panels.QuickMenu,
                GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.ShadowBlack, 0.3f),
                GameModeManager.Ui.GetMin(1595, 285),
                GameModeManager.Ui.GetMax(22, 391));

            GameModeManager.Ui.Panel(ref mainContainer,
                GameModeManager.Ui.Panels.QuickMenu,
                GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.ShadowBlack, 0.3f),
                GameModeManager.Ui.GetMin(1615, 699), GameModeManager.Ui.GetMax(42, 22));

            GameModeManager.Ui.Panel(ref mainContainer,
                GameModeManager.Ui.Panels.QuickMenu,
                GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.ButtonBlack, 0.8f),
                GameModeManager.Ui.GetMin(1629, 713), GameModeManager.Ui.GetMax(56, 36));
            #endregion container panel
            #region buttons
            GameModeManager.Ui.Button(ref mainContainer,
                GameModeManager.Ui.Panels.QuickMenu,
                GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.Red, 0.9f),
                "LEAVE",
                19,
                GameModeManager.Ui.GetMin(1609, 299),
                GameModeManager.Ui.GetMax(36, 741),
                $"{ConsoleCommands.ArenaLeave}",
                false);

            GameModeManager.Ui.Button(ref mainContainer,
                GameModeManager.Ui.Panels.QuickMenu,
                GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.Blue, 0.9f),
                "INFINITE AMMO",
                19,
                GameModeManager.Ui.GetMin(1609, 344),
                GameModeManager.Ui.GetMax(36, 696),
                $"{ConsoleCommands.UnlimitedAmmoToggle}",
                false);

            GameModeManager.Ui.Button(ref mainContainer,
                GameModeManager.Ui.Panels.QuickMenu,
                GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.Blue, 0.9f),
                "RESET",
                19,
                GameModeManager.Ui.GetMin(1609, 389),
                GameModeManager.Ui.GetMax(36, 651),
                $"{ConsoleCommands.ResetMatchStats}",
                false);
            #endregion buttons
            GameModeManager.Ui.Panel(ref mainContainer,
               GameModeManager.Ui.Panels.QuickMenu,
               GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.Gray, 1f),
               "HITS",
               GameModeManager.Ui.GetMin(1609, 635),
               GameModeManager.Ui.GetMax(174, 405),
               20);
            GameModeManager.Ui.Panel(ref mainContainer,
              GameModeManager.Ui.Panels.QuickMenu,
              GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.Gray, 1f),
              "HEADSHOTS",
              GameModeManager.Ui.GetMin(1609, 591),
              GameModeManager.Ui.GetMax(174, 449),
              20);
            GameModeManager.Ui.Panel(ref mainContainer,
              GameModeManager.Ui.Panels.QuickMenu,
              GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.Gray, 1f),
              "BODYSHOTS",
              GameModeManager.Ui.GetMin(1609, 547),
              GameModeManager.Ui.GetMax(174, 493),
              20);
            GameModeManager.Ui.Panel(ref mainContainer,
              GameModeManager.Ui.Panels.QuickMenu,
              GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.Gray, 1f),
              "KILLS",
              GameModeManager.Ui.GetMin(1609, 459),
              GameModeManager.Ui.GetMax(174, 581),
              20);
            GameModeManager.Ui.Panel(ref mainContainer,
              GameModeManager.Ui.Panels.QuickMenu,
              GameModeManager.Ui.Color(GameModeManager.Ui.ColorCode.Gray, 1f),
              "ACCURACY",
              GameModeManager.Ui.GetMin(1609, 503),
              GameModeManager.Ui.GetMax(174, 537),
              20);
            GameModeManager.Ui.ClearAllMenus(player);
            CuiHelper.AddUi(player, mainContainer);
        }
        #endregion GUI

        #region RustHooks
        private void Init()
        {
            _instance = this;
            DestroyAllBots();
            LoadConfig();

            var container = new CuiElementContainer
            {
                new CuiElement
                {
                    Name = _patternUiContainer,
                    Components =
                    {
                        new CuiImageComponent {Color = "1.0 1.0 1.0 0.0"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.97 0.97",
                            AnchorMax = "0.97 0.97",
                            OffsetMin = $"-{_patternPanelSizeX} -{_patternPanelSizeY}",
                            OffsetMax = "0 0"
                        }
                    }
                }
            };

            _mainPanelJson = container.ToString();
        }
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            var player = entity as BasePlayer;
            if (player != null && player.HasFlag(BaseEntity.Flags.Reserved2))
            {
                var botPlayer = player.gameObject.GetComponent<BotPlayer>();
                if (botPlayer != null && botPlayer.Config.InfinitiveHealth)
                {
                    info?.damageTypes.Clear();
                    return false;
                }
            }

            if (player != null && _players.ContainsKey(player.userID))
            {
                info?.damageTypes.Clear();
                return false;
            }
            return null;
        }
        private object OnPlayerDeath(BasePlayer player, HitInfo hitInfo)
        {
            //if (_players.ContainsKey(player.userID))
            //    return null;
            //if the dead player is a bot
            if (player.HasFlag(BaseEntity.Flags.Reserved2))
            {
                var botPlayer = player.gameObject.GetComponent<BotPlayer>();
                if (botPlayer != null)
                {
                    AdvancedTrainingPlayer attacker = null;
                    var attackerPlayer = hitInfo?.InitiatorPlayer;
                    if (attackerPlayer != null)
                    {
                        _players.TryGetValue(attackerPlayer.userID, out attacker);
                    }
                    if (attacker != null)
                        attacker.Statistics.Kills++;

                    var spawnPosition = botPlayer.Arena.GetRandomSpawnLocationForBot(botPlayer.Config.Name, botPlayer.Config.RestrictToNamedSpawnPoint);
                    if (spawnPosition.HasValue)
                    {
                        player.Teleport(spawnPosition.Value);
                    }
                    foreach (var item in player.inventory.containerWear.itemList)
                    {
                        item.condition = item.maxCondition;
                    }
                    player.health = player.MaxHealth();
                    botPlayer.SetRandomSpeed();
                    botPlayer.Go();
                    return false;
                }
            }
            return null;
        }
        private void Unload()
        {
            foreach (var arena in _arenas.Values)
            {
                arena.BotManagementTimer?.Destroy();
            }
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (_players.ContainsKey(player.userID))
                {
                    LeaveAdvanceTraining(player);
                }
            }
            foreach (var scriptRecorder in UnityEngine.Object.FindObjectsOfType<ShotRecorderBehaviour>())
            {
                UnityEngine.Object.Destroy(scriptRecorder);
            }
            DestroyAllBots();
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Messages.NoPermission] = "You don't have permission to use this command",
                [Messages.WrongCommand] = "You entered the command in a wrong way, available commands are: {0}",
                [Messages.ArenaCommandsList] = "Available arena commands are: {0}",
                [Messages.AdvancedTrainingCommandsList] = "<color=#eb9534>Advanced Training</color> registered use <color=#5af>{0}</color> to get commands",
                [Messages.InvalidPlayerId] = "Invalid player steamID",
                [Messages.PlayerNotFound] = "No player found",
                [Messages.LobbyNotFound] = "No lobby found",
                [Messages.ArenaNotFound] = "No arena found",
                [Messages.ChangesSaved] = "Changes has been saved",
                [Messages.NoEditingLobby] = "There is no lobby to edit, use the {0} to start editing a lobby",
                [Messages.NoEditingArena] = "There is no arena to edit, use the {0} to start editing an arena",
                [Messages.ArenaParameterChanged] = "The arena {0} has been changed",
                [Messages.ArenaSpawnPointAdded] = "New spawn point {0} has been added to the arena",
                [Messages.ArenaInvalidPosition] = "The target position is an invalid position, it should be inside the arena",
                [Messages.ArenaInvalidEntrancePosition] = "The target entrance position is an invalid position, it should be inside the lobby",
                [Messages.ArenaDeleted] = "The arena has been deleted",
                [Messages.ArenaCreated] = "The arena has been created",
                [Messages.ArenaEditingStarted] = "Arena {0} ({1}) editing has been started",
                [Messages.ArenaEditingDone] = "Arena {0} ({1}) editing has been stopped",
                [Messages.ArenaNoAccess] = "You don't have access to this arena",
                [Messages.LobbyZoneNotFound] = "Lobby Zone not found",
                [Messages.ArenaZoneNotFound] = "Arena Zone not found",
                [Messages.SpawnPointNotFound] = "Spawn Point not found",
                [Messages.ArenaSetValueHelp] = "Available keys for editing the arena: {0}",
                [Messages.UnlimitedAmmoEnabled] = "Unlimited Ammo Enabled",
                [Messages.UnlimitedAmmoDisabled] = "Unlimited Ammo Disabled"
            }, this);
        }
        protected override void LoadDefaultConfig()
        {
            Config.Settings.Converters = new JsonConverter[] { new StringEnumConverter() };
            _config = GetDefaultConfig();
        }
        private void Loaded()
        {
            _dataManager = Interface.Oxide.DataFileSystem.GetFile($"{GameModeManager.MainFolderName}/{nameof(RecoilTrainingGround)}");
            _dataManager.Settings.Converters = new JsonConverter[] { new StringEnumConverter() };
        }
        private void OnServerInitialized()
        {
            _instance = this;
            LoadData();

            var advancedArena = plugins.PluginManager.GetPlugin(nameof(GameModeManager));
            if (advancedArena != null)
            {
                advancedArena.Call("RegisterArena", nameof(RecoilTrainingGround));
                TriggerNetworking(null as AdvancedTrainingArena);
            }

        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
            Config.Settings.Converters = new JsonConverter[] { new StringEnumConverter() };
            SaveConfig();
        }
        protected override void SaveConfig() => Config.WriteObject(_config, true);
        private void AddShotRecoilRecorderBehaviour(BasePlayer player)
        {
            var behaviour = player.GetComponent<ShotRecorderBehaviour>();
            if (behaviour == null)
            {
                behaviour = player.gameObject.AddComponent<ShotRecorderBehaviour>();
            }
            behaviour.Enable();
        }
        private void RemoveShotRecoilRecorderBehaviour(BasePlayer player)
        {
            var behaviour = player.GetComponent<ShotRecorderBehaviour>();
            if (behaviour != null)
            {
                behaviour.Disable();
            }
        }
        private void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
        {
            if (_players.ContainsKey(attacker.userID) && hitInfo.HitEntity is BasePlayer)
            {
                _players[attacker.userID].Statistics.Hits++;
                if (hitInfo.isHeadshot)
                {
                    _players[attacker.userID].Statistics.Headshots++;
                }
            }
        }
        private void OnWeaponFired(BaseProjectile projectile, BasePlayer player)
        {
            if (!_players.ContainsKey(player.userID))
                return;
            _players[player.userID].Statistics.Shots++;

            if (_players.ContainsKey(player.userID) /*&& _config.EnableUI*/)//TODO: check for UI
            {
                _players[player.userID].Statistics.BulletsFired++;
            }
            if (_players[player.userID].UnlimitedAmmo)
            {
                projectile.GetItem().condition = projectile.GetItem().info.condition.max; //weapon won't jam or break
                projectile.primaryMagazine.contents = projectile.primaryMagazine.capacity; //ammo will be always full 
                projectile.SendNetworkUpdateImmediate();
            }
            _players[player.userID].WeaponName = projectile.ShortPrefabName;

            var shotRecoilRecorder = player.GetComponent<ShotRecorderBehaviour>();
            if (shotRecoilRecorder != null)
            {
                shotRecoilRecorder.OnFired(projectile);
            }
        }
        void OnLoseCondition(Item item, ref float amount)
        {
            var player = item.GetOwnerPlayer();
            if (player == null || !_players.ContainsKey(player.userID))
                return;
            amount = 0;
        }
        private void OnReloadWeapon(BasePlayer player, BaseProjectile projectile)
        {
            //angleVectors.Clear();
            var shotRecoilRecorder = player.GetComponent<ShotRecorderBehaviour>();
            if (shotRecoilRecorder != null)
            {
                shotRecoilRecorder.OnReloaded();
            }
        }

        private void OnItemDropped(Item item, BaseEntity entity)
        {
            var player = item?.GetOwnerPlayer();
            if (player == null)
            {
                return;
            }
            if (_players.ContainsKey(player.userID))
            {
                item.Remove();
            }
        }
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (_players.ContainsKey(player.userID))
            {
                LeaveAdvanceTraining(player);
            }
        }
        private object CanBeWounded(BasePlayer player, HitInfo info)
        {
            if (player.HasFlag(BaseEntity.Flags.Reserved2) && player.GetComponent<BotPlayer>() != null)
            {
                return false;
            }

            return null;
        }
        object CanNetworkTo(BasePlayer player, BasePlayer target)
        {
            if (player.userID == target.userID)
            {
                return null;
            }
            if (permission.UserHasPermission(target.UserIDString, GameModeManager.PermissionVanishBypass))
            {
                return null;
            }
            AdvancedTrainingPlayer matchPlayer, matchTargetPlayer;
            _players.TryGetValue(target.userID, out matchTargetPlayer);
            if (matchTargetPlayer == null)
                return null;
            if (player.userID.IsSteamId() && target.userID.IsSteamId())
            {
                _players.TryGetValue(player.userID, out matchPlayer);
                if (matchPlayer?.Match == null && matchTargetPlayer?.Match == null)
                {
                    return null;
                }
                return matchPlayer?.Match?.Id == matchTargetPlayer?.Match?.Id;
            }

            if (matchTargetPlayer.Match.Arena.Data.PerformanceMode)
                return null;
            return player.OwnerID == target.userID || target.OwnerID == player.userID;
        }
        #endregion RustHooks

        #region CustomHooks

        [HookMethod(nameof(GameModeManager.IAdvancedLobby.InitializeArena))]
        public void InitializeArena(GameModeManager arena)
        {
            _arena = arena;
            ArenaGenerate();
        }

        [HookMethod(nameof(GameModeManager.IAdvancedLobby.GetArenaIdsInLobby))]
        public List<int> GetArenaIdsInLobby(int lobbyId)
        {
            return _arenas.Values.Where(a => a.Data.LobbyId == lobbyId).Select(a => a.Data.Id).ToList();
        }

        [HookMethod(nameof(GameModeManager.IAdvancedLobby.GetArenasDetailsInLobby))]
        public List<GameModeManager.ArenaDetail> GetArenasDetailsInLobby(int lobbyId)
        {
            return _arenas.Values.Where(a => a.Data.LobbyId == lobbyId).Select(a => new GameModeManager.ArenaDetail
            {
                ArenaId = a.Data.Id,
                Name = a.Data.Name
            }).ToList();
        }

        [HookMethod(nameof(GameModeManager.IAdvancedLobby.GetPlayerMatchPlayers))]
        public List<BasePlayer> GetPlayerMatchPlayers(ulong playerId)
        {
            AdvancedTrainingPlayer advancedTrainingPlayer;
            if (_players.TryGetValue(playerId, out advancedTrainingPlayer) && advancedTrainingPlayer != null)
            {
                return advancedTrainingPlayer.Match.Players.Select(x => x.Player).ToList();
            }
            return null;
        }

        [HookMethod(nameof(GameModeManager.IAdvancedLobby.HandleShowArena))]
        public void HandleShowArena(BasePlayer player, int lobbyId, int visibilityTime = 60)
        {
            if (player == null)
                return;

            foreach (var arena in _arenas.Values.Where(a => a.Data.LobbyId == lobbyId).ToList())
            {
                player.SendConsoleCommand("ddraw.text", visibilityTime, arena.Data.IsEnabled ? _arena.GetColor("#9B51E0") : _arena.GetColor("#333333"), arena.Data.EntranceLocation + new Vector3(0, 1.5f, 0), $"<size=15>{arena.Data.Name}</size>");
                player.SendConsoleCommand("ddraw.sphere", visibilityTime, arena.Data.IsEnabled ? _arena.GetColor("#9B51E0") : _arena.GetColor("#333333"), arena.Data.EntranceLocation + new Vector3(0, 1.5f, 0), arena.Data.EntranceTriggerRadius);
                for (float i = 0f; i < 1; i += 0.1f)
                {
                    var start = Vector3.Lerp(arena.Data.EntranceLocation, arena.Zone.Location, i);
                    var end = Vector3.Lerp(arena.Data.EntranceLocation, arena.Zone.Location, i + 0.1f);
                    player.SendConsoleCommand("ddraw.arrow", visibilityTime, _arena.GetColor("#FFFFFF"), start, end, 0.5f);
                }
                player.SendConsoleCommand("ddraw.text", visibilityTime, arena.Data.IsEnabled ? _arena.GetColor("#F2C94C") : _arena.GetColor("#333333"),
                    arena.Zone.Location + new Vector3(0, 1.5f, 0), $"<size=20>{arena.Data.Name} ({arena.Data.Id})</size>");
                _arena.ShowZone(player, arena.Zone, arena.Data.IsEnabled, false, visibilityTime);

                var color = _arena.GetColor("#BDBDBD");
                player.SendConsoleCommand("ddraw.text", visibilityTime, _arena.GetColor("#F2994A"),
                    arena.Data.PlayerSpawnLocation + new Vector3(0, 1.5f, 0), "<size=20>Player Spawn Location</size>");
                player.SendConsoleCommand("ddraw.sphere", visibilityTime, color, arena.Data.PlayerSpawnLocation, 1);

                foreach (var spawnLocation in arena.Data.SpawnLocations)
                {
                    player.SendConsoleCommand("ddraw.text", visibilityTime, _arena.GetColor("#F2994A"),
                        spawnLocation.SpawnPoint + new Vector3(0, 1.5f, 0), $"<size=20>{spawnLocation.Name}{(string.IsNullOrWhiteSpace(spawnLocation.BotName) ? string.Empty : $"\nFor Bot: {spawnLocation.BotName}")}</size>");
                    player.SendConsoleCommand("ddraw.sphere", visibilityTime, color, spawnLocation.SpawnPoint, spawnLocation.Radius);
                    player.SendConsoleCommand("ddraw.sphere", visibilityTime, color, spawnLocation.SpawnPoint, 1);
                }
            }
        }

        [HookMethod(nameof(GameModeManager.IAdvancedLobby.HandleRemoveArena))]
        public void HandleRemoveArena(int lobbyId)
        {
            var arenas = _arenas.Values.Where(x => x.Data.LobbyId == lobbyId);
            foreach (var arena in arenas)
            {
                RemoveArena(arena);
            }
        }

        [HookMethod(nameof(GameModeManager.IAdvancedLobby.HandleArenaLoadoutValidation))]
        public void HandleArenaLoadoutValidation(int lobbyId)
        {
            var arenas = _arenas.Values.Where(x => x.Data.LobbyId == lobbyId);
            foreach (var arena in arenas)
            {
                if (!_arena.IsLoadoutValid(arena.Data.PlayerLoadout))
                {
                    PrintWarning($"Spectator loadout name of the {arena.Data.Name} arena is invalid.");
                }
                foreach (var botData in arena.Data.Bots)
                {
                    var botConfig = _config.Bots.FirstOrDefault(x =>
                        x.Name.Equals(botData.Name, StringComparison.OrdinalIgnoreCase));

                    if (botConfig != null)
                    {
                        if (!_arena.IsLoadoutValid(botConfig.Loadout))
                        {
                            PrintWarning($"The {botConfig.Loadout} loadout name of the {botConfig.Name} bot in the {arena.Data.Name} arena is invalid.");
                        }
                    }
                }
            }
        }

        [HookMethod(nameof(GameModeManager.IAdvancedLobby.HandleJoinArena))]
        public object HandleJoinArena(BasePlayer player, int arenaId)
        {
            AdvancedTrainingArena advancedTrainingArena;
            if (!_arenas.TryGetValue(arenaId, out advancedTrainingArena) || advancedTrainingArena == null || advancedTrainingArena.Data == null)
            {
                return null;
            }
            if (advancedTrainingArena.Data.MatchType == GameModeManager.MatchType.TrainingGround)
            {
                TeleportToArena(player, _arenas[advancedTrainingArena.Data.Id]);
                return true;
            }
            return null;
        }

        [HookMethod(nameof(GameModeManager.IAdvancedLobby.HandleLeaveArena))]
        public void HandleLeaveArena(BasePlayer player)
        {
            LeaveAdvanceTraining(player);
        }

        [HookMethod(nameof(GameModeManager.IAdvancedLobby.HandleOnPlayerRespawn))]
        public object HandleOnPlayerRespawn(BasePlayer player)
        {
            AdvancedTrainingPlayer trainingPlayer;
            if (_players.TryGetValue(player.userID, out trainingPlayer) && trainingPlayer != null)
            {
                ShowAdvancedTrainingQuickMenu(player);
                ShowScores(trainingPlayer);
                var spawnLocation = trainingPlayer.Match.Arena.Data.PlayerSpawnLocation;
                if (spawnLocation.Equals(Vector3.zero))
                {
                    spawnLocation = trainingPlayer.Match.Arena.Zone.Location;
                }

                if (spawnLocation.Equals(Vector3.zero))
                {
                    return null;
                }
                return spawnLocation;
            }

            return null;
        }

        [HookMethod(nameof(GameModeManager.IAdvancedLobby.HandleShowPauseMenu))]
        public object HandleShowPauseMenu(BasePlayer player)
        {
            if (_players.ContainsKey(player.userID))
            {
                return true;
            }
            return null;
        }

        [HookMethod(nameof(GameModeManager.IAdvancedLobby.HandleArenaHelp))]
        public void HandleArenaHelp(BasePlayer player)
        {
            _arena.ShowMessage(player, Messages.AdvancedTrainingCommandsList, this, ConsoleCommands.ArenaHelp);
        }

        #endregion CustomHooks
        private void SaveData()
        {
            _dataManager.WriteObject(_pluginData);
        }
        private void LoadData()
        {
            try
            {
                _pluginData = _dataManager.ReadObject<PluginData>();
            }
            catch (Exception exception)
            {
                PrintError("Data file is corrupt, error message:");
                PrintError(exception.Message);
                if (exception.InnerException != null)
                {
                    PrintError($"Inner exception message: {exception.InnerException.Message}");
                }
                _pluginData = new PluginData();
            }
        }
        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Bots = new List<Bot>
                {
                    new Bot
                    {
                        Name = "Bot 1",
                        Loadout = "AK",
                        MinimumMovementSpeed = 3,
                        MaximumMovementSpeed = 5,
                        RestrictToNamedSpawnPoint = false
                    },
                    new Bot
                    {
                        Name = "Bot 2",
                        Loadout = "MP5",
                        MinimumMovementSpeed = 3,
                        MaximumMovementSpeed = 5,
                        RestrictToNamedSpawnPoint = false
                    },
                    new Bot
                    {
                        Name = "Bot 3",
                        Loadout = "Lobby",
                        MinimumMovementSpeed = 0,
                        MaximumMovementSpeed = 0,
                        RestrictToNamedSpawnPoint = false
                    },
                }
            };
        }
        #endregion Methods
    }
}

