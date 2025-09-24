// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Network;
using Network.Visibility;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Plugins.SleighEventExtensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/**
 * 
 * VERSION HISTORY
 * 
 * V 1.0.1
 * - event no longer starts with 0 players
 * - players are no longer able to dismount their sleigh during the event (WAS NOT A BUG)
 * - added scheduled events
 * 
 */

namespace Oxide.Plugins
{
    [Info(nameof(SleighEvent), "The_Kiiiing", "1.0.1")]
    public class SleighEvent : RustPlugin
    {

        #region Fields

        private const string SLEIGH_PREFAB = "assets/prefabs/misc/xmas/sleigh/santasleigh.prefab";
        private const string CHAIR_PREFAB = "assets/bundled/prefabs/static/chair.invisible.static.prefab";

        private const string PERM_SLEIGH_BLOCKED = "sleighevent.nosleigh";
        private const string PERM_ADMIN = "sleighevent.admin";

        private const string CMD_START = "sleighstart";
        private const string CMD_PLAYER = "sleigh";

        private static SleighEvent _instance;

        private Timer autoEventTimer;

        [PluginReference]
        private Plugin EntityScaleManager;

        #endregion

        #region Configuration

        private static Configuration _config;

        private class Configuration
        {
            [JsonProperty("Event duration (seconds)")]
            public int eventDuration = 240;

            [JsonProperty("Time before the event starts (seconds)")]
            public int eventDelay = 30;

            [JsonProperty("Event height")]
            public int eventHeight = 350;

            [JsonProperty("Start amount of collectables spawned per player (increases over time)")]
            public int collectablesPerPlayer = 6;

            [JsonProperty("Sleigh speed")]
            public float sleighSpeed = 3;

            [JsonProperty("Speed Perk configuration", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public PerkConfig speedPerk = new PerkConfig
            {
                duration = 20f,
                multiplier = 1.5f,
                spawnChance = 0.05f
            };

            [JsonProperty("Collect Range Perk configuration", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public PerkConfig collectPerk = new PerkConfig
            {
                duration = 20f,
                multiplier = 2f,
                spawnChance = 0.05f
            };

            [JsonProperty("Reward for 1st place (null = no reward)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<CustomItem> reward1 = new List<CustomItem>
            {
                new CustomItem
                {
                    shortName = "xmas.present.large",
                    skinId = 0,
                    displayName = null,
                    amount = 1
                }
            };

            [JsonProperty("Reward for 2nd place (null = no reward)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<CustomItem> reward2 = new List<CustomItem>
            {
                new CustomItem
                {
                    shortName = "xmas.present.medium",
                    skinId = 0,
                    displayName = null,
                    amount = 2
                }
            };

            [JsonProperty("Reward for 3rd place (null = no reward)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<CustomItem> reward3 = null;

            [JsonProperty("Automatically start event every x hours (in realtime, set to -1 to disable automatic events)")]
            public float autoEventTime = 1.5f;
        }

        private class PerkConfig
        {
            [JsonProperty("Duration (seconds)")]
            public float duration;

            [JsonProperty("Multiplier")]
            public float multiplier;

            [JsonProperty("Spawn chance (1 = 100%)")]
            public float spawnChance;
        }

        private class CustomItem
        {
            [JsonProperty("Item short name")]
            public string shortName;

            [JsonProperty("Item skin id")]
            public ulong skinId;

            [JsonProperty("Custom item name (null = default name)")]
            public string displayName;

            [JsonProperty("Item amount")]
            public int amount;

            public Item CreateItem()
            {
                var itm = ItemManager.CreateByName(shortName, amount, skinId);
                if (itm != null && displayName != null && displayName.Length > 0)
                {
                    itm.name = displayName;
                }

                return itm;
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();

        #endregion

        #region Lang

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["EventStartIn"] = "The sleigh event will start in {0} seconds, type '/"+CMD_PLAYER+" join' to join",
                ["EventStart"] = "The sleigh event has started. Collect as much presents as you can",
                ["EventStartNotEnoughPlayers"] = "The sleigh event has been canceled (not enough players joined)",
                ["EventEnd"] = "{0} won the sleigh event with a score of {1}",
                ["EventEndNoWinner"] = "The event ended and no one won :/",
                ["PlayerCannotEnter"] = "You cannot join the event at the moment",
                ["PlayerNotAllowedToEnter"] = "You are not allowed to join the event",
                ["PlayerAlreadyEntered"] = "You already joined the event",
                ["GlobalPlayerEnter"] = "{0} joined the event",
                ["GlobalPlayerLeave"] = "{0} fell out of their sleigh",
                ["PlayerReward"] = "You are placed {0} and received your reward"
            }, this);
        }

        private static string GetMessage(string key, BasePlayer player, params object[] args)
        {
            string msg = GetMessage(key, player);

            return String.Format(msg, args);
        }

        private static string GetMessage(string key, BasePlayer player = null)
        {
            return _instance.lang.GetMessage(key, _instance, player?.UserIDString);
        }

        #endregion

        #region Hooks

        void Init()
        {
            _instance = this;

            permission.RegisterPermission(PERM_SLEIGH_BLOCKED, this);
            permission.RegisterPermission(PERM_ADMIN, this);

            AddUniversalCommand(CMD_START, nameof(CmdStart), PERM_ADMIN);
        }

        void Unload()
        {
            autoEventTimer?.Destroy();

            if (!EventController.sleighEvent?.Finished ?? false)
            {
                EventController.sleighEvent.EndEvent();
            }

            _instance = null;
        }

        void OnServerInitialized()
        {
            if (_config.autoEventTime > 0)
            {
                Puts($"Automatic sleigh events will happen every {_config.autoEventTime:N1} hours");
                autoEventTimer = timer.Every(60f * 60f * _config.autoEventTime, AutoEventCallback);
            }
        }

        object CanDismountEntity(BasePlayer player, BaseMountable entity)
        {
            if (EventController.sleighEvent?.IsRunning ?? false)
            {
                return !EventController.sleighEvent.IsMounted(player);
            }

            return null;
        }

        #endregion

        #region Commands

        private void CmdStart(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, PERM_ADMIN) && !player.IsServer)
            {
                player.Reply("No permission");
            }

            if (!EventController.Start())
            {
                player.Reply("Cannot start sleigh event now");
            }
        }

        [ChatCommand(CMD_PLAYER)]
        private void CmdSleighEnter(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 1 && args[0] == "join")
            {
                EventController.sleighEvent?.Enter(player);
            }
        }

        private void AutoEventCallback()
        {
            if (EventController.Start())
            {
                Puts("Automatic sleigh event started");
            }
            else
            {
                PrintWarning($"Failed to start automatic sleigh event, next event will be in {_config.autoEventTime:N1} hours");
            }
        }

        #endregion

        #region Event Controller

        private class EventController
        {
            #region Fields & Ctor

            private const string SCOREBOARD_PANEL_NAME = "sleighevent.scoreboard";

            private const int UI_FONT_SIZE = 14;
            private const string UI_FONT = "permanentmarker.ttf";

            public bool Finished { get; private set; }
            public bool IsRunning { get; private set; }

            private HashSet<ulong> mountedPlayers;

            private List<BasePlayer> enteredPlayers;
            private bool canEnter;

            private List<SleighBehavior> sleighs;
            private int SleighCount => sleighs?.Count ?? 0;

            private List<BaseCollectable> collectables;

            private Timer endTimer;
            private Timer scoreboardTimer;

            private int eventTime;
            private float eventStartTime;

            private PlayerScoreboard scoreboard;

            private float TimeRemaining => (eventStartTime + eventTime) - Time.time;

            private EventController(float enterTime, int eventTime)
            {
                Finished = false;
                IsRunning = false;

                mountedPlayers = new HashSet<ulong>();

                canEnter = true;
                enteredPlayers = new List<BasePlayer>();

                sleighs = new List<SleighBehavior>();

                collectables = new List<BaseCollectable>();

                scoreboard = new PlayerScoreboard();

                this.eventTime = eventTime;

                Broadcast(GetMessage("EventStartIn", null, enterTime));

                _instance.timer.In(enterTime, StartEvent);
            }

            #endregion

            #region Functions

            public void Enter(BasePlayer player)
            {
                if (!canEnter)
                {
                    player.ChatMessage(GetMessage("PlayerCannotEnter", player));
                    return;
                }
                else if (_instance.permission.UserHasPermission(player.UserIDString, PERM_SLEIGH_BLOCKED))
                {
                    player.ChatMessage(GetMessage("PlayerNotAllowedToEnter", player));
                    return;
                }
                else if (enteredPlayers.Contains(player))
                {
                    player.ChatMessage(GetMessage("PlayerAlreadyEntered", player));
                    return;
                }

                enteredPlayers.Add(player);
                Broadcast(GetMessage("GlobalPlayerEnter", player, player.displayName));
            }

            private void StartEvent()
            {
                Broadcast(GetMessage("EventStart"));

                canEnter = false;
                eventStartTime = Time.time;

                if (enteredPlayers.Count < 1)
                {
                    Broadcast(GetMessage("EventStartNotEnoughPlayers"));
                    Finished = true;
                    return;
                }

                endTimer = _instance.timer.In(eventTime, EndEvent);
                scoreboardTimer = _instance.timer.Repeat(1f, eventTime + 1, UpdateScoreboards);

                InvokeHandler.Instance.StartCoroutine(StartEventCoro());
                InvokeHandler.Instance.StartCoroutine(SpawnCollectablesCoro());

                IsRunning = true;
            }

            private IEnumerator StartEventCoro()
            {
                foreach (var player in enteredPlayers.ToArray())
                {
                    if (player.IsSleeping())
                    {
                        enteredPlayers.Remove(player);
                        continue;
                    }

                    player.EnsureDismounted();

                    // TODO increase delay
                    yield return CoroutineEx.waitForEndOfFrame;

                    MountSleigh(player);
                }

                yield break;
            }

            public void EndEvent()
            {
                endTimer?.Destroy();
                scoreboardTimer?.Destroy();

                IsRunning = false;

                foreach(var pl in BasePlayer.activePlayerList)
                {
                    DestroyUi(pl);
                }

                foreach (var sleigh in sleighs.ToArray())
                {
                    DismountSleigh(sleigh, true);
                }

                foreach (var coll in collectables)
                {
                    coll.Destroy();
                }

                var winners = scoreboard.GetTopPlayers(3);
                if (winners.Count > 0)
                {
                    var winner = winners.First();
                    Broadcast(GetMessage("EventEnd", null, winner.DisplayName, winner.Score));

                    DistributeRewards(winners);
                }
                else
                {
                    Broadcast(GetMessage("EventEndNoWinner"));
                }

                Finished = true;

                collectables.Clear();
                enteredPlayers.Clear();
                sleighs.Clear();
            }

            private void UpdateScoreboards()
            {
                foreach (var pl in BasePlayer.activePlayerList)
                {
                    DestroyUi(pl);
                    DrawUi(pl);
                }
            }

            private void DistributeRewards(List<PlayerScoreboard.PlayerScore> winners)
            {
                var list = new List<CustomItem>[] { _config.reward1, _config.reward2, _config.reward3 };
                BasePlayer player;
                List<CustomItem> rewardList;
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        rewardList = list[i];
                        player = winners[i].GetPlayer();
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        break;
                    }

                    if (rewardList == null || rewardList.Count < 1)
                    {
                        continue;
                    }

                    foreach (var reward in rewardList)
                    {
                        player.GiveItem(reward.CreateItem());
                    }
                    player.ChatMessage(GetMessage("PlayerReward", player, i + 1));
                }
            }

            public bool IsMounted(BasePlayer player)
            {
                return mountedPlayers.Contains(player.userID);
            }

            #endregion

            #region Scoreboard UI

            private void DrawUi(BasePlayer player)
            {
                CuiElementContainer result = new CuiElementContainer();

                string rootPanel = result.Add(new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMax = "0.99 0.98",
                        AnchorMin = "0.99 0.98",
                        OffsetMin = "-200 -200",
                        OffsetMax = "0 0"
                    },
                    Image = new CuiImageComponent
                    {
                        Color = "1 1 1 0.5",
                    }
                }, "Hud", SCOREBOARD_PANEL_NAME);

                int i = 0;
                int i_max = 6;
                foreach (var ps in scoreboard.GetTopPlayers(10))
                {
                    result.Add(new CuiElement
                    {
                        Parent = rootPanel,
                        Components =
                        {
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"0.06 {0.85f-((float)i*0.12f)}",
                                AnchorMax = $"0.95 {0.96f-((float)i*0.12f)}"
                            },
                            new CuiTextComponent
                            {
                                Text = $"{ps.Score}  {ps.DisplayName}",
                                Color = ps.UserId == player.userID ? $"1 1 0 1" : "1 1 1 1",
                                Align = TextAnchor.MiddleLeft,
                                FontSize = UI_FONT_SIZE,
                                Font = UI_FONT,
                            },
                            new CuiOutlineComponent
                            {
                                Color = "0 0 0 1",
                            }
                        }
                    });

                    i++;
                    if (i > i_max) break;
                }

                int minutes = Mathf.FloorToInt(TimeRemaining / 60f);
                int seconds = Mathf.FloorToInt(TimeRemaining % 60f);
                result.Add(new CuiElement
                {
                    Parent = rootPanel,
                    Components =
                    {
                        new CuiRectTransformComponent
                        {
                            AnchorMin = $"0.05 0",
                            AnchorMax = $"0.95 0.15"
                        },
                        new CuiTextComponent
                        {
                            Text = $"{minutes}:{seconds:D2}",
                            Align = TextAnchor.MiddleCenter,
                            FontSize = UI_FONT_SIZE,
                            Font = UI_FONT
                        },
                        new CuiOutlineComponent
                        {
                            Color = "0 0 0 1",
                        }
                    }
                });

                CuiHelper.AddUi(player, result);
            }

            private void DestroyUi(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, SCOREBOARD_PANEL_NAME);
            }

            #endregion

            #region Sleigh Functions

            private void MountSleigh(BasePlayer player)
            {
                Vector3 pos = player.transform.position;

                BaseEntity sleigh = GameManager.server.CreateEntity(SLEIGH_PREFAB, pos);
                sleigh.enableSaving = false;
                sleigh.Spawn();

                var controller = sleigh.gameObject.AddComponent<SleighBehavior>();
                controller.Mount(player);

                sleighs.Add(controller);
                mountedPlayers.Add(player.userID);
            }

            private void DismountSleigh(SleighBehavior sleigh, bool end = false)
            {
                if (!end)
                {
                    BroadcastToParticipants(GetMessage("GlobalPlayerLeave", null, sleigh.player.displayName));
                }

                sleighs.Remove(sleigh);
                enteredPlayers.Remove(sleigh.player);
                mountedPlayers.Remove(sleigh.player.userID);

                UnityEngine.Object.Destroy(sleigh);

                if (!end && SleighCount < 1)
                {
                    EndEvent();
                }
            }

            #endregion

            #region Collectables

            private void TakeCollectable(SleighBehavior sleigh, BaseCollectable coll)
            {
                scoreboard.IncrementScore(sleigh.player, coll.GetPoints());

                collectables.Remove(coll);
                coll.Destroy();

                if (coll is CollectableSpeedPerk)
                {
                    sleigh.ActivateSpeedPerk(_config.speedPerk.duration, _config.speedPerk.multiplier);
                }
                else if (coll is CollectableRangePerk)
                {
                    sleigh.ActivatePickupPerk(_config.collectPerk.duration, _config.collectPerk.multiplier*2);
                }
            }

            private IEnumerator SpawnCollectablesCoro()
            {
                int multi = _config.collectablesPerPlayer;

                while (!Finished)
                {
                    if (multi == 4 && Time.time - ((float)eventTime)*0.5f > eventStartTime)
                    {
                        multi *= 2;
                    }

                    if (collectables.Count < SleighCount * multi)
                    {
                        SleighBehavior sleigh;
                        for(int i = 0; i < SleighCount; i++)
                        {
                            try
                            {
                                sleigh = sleighs[i];
                            }
                            catch (IndexOutOfRangeException)
                            {
                                break;
                            }

                            collectables.Add(BaseCollectable.SpawnNearSleigh(sleigh));
                            yield return CoroutineEx.waitForEndOfFrame;
                        }
                    }

                    yield return CoroutineEx.waitForSeconds(1f);
                }

                foreach (var coll in collectables)
                {
                    coll.Destroy();
                    yield return CoroutineEx.waitForEndOfFrame;
                }

                yield break;
            }

            #region Classes

            private class CollectableDrop : BaseCollectable
            {
                public CollectableDrop(Vector3 position) : base(position) { }

                public override void Create()
                {
                    var drop = GameManager.server.CreateEntity("assets/prefabs/misc/xmas/sleigh/presentdrop.prefab", position, Quaternion.Euler(0, Random.Range(0f, 89f), 0), false) as SupplyDrop;
                    drop.enableSaving = false;
                    drop.Spawn();

                    var rb = drop.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.detectCollisions = false;
                        rb.constraints = RigidbodyConstraints.FreezePosition;
                    }

                    entity = drop;
                }

                public override int GetPoints() => 1;
            }

            private class CollectableRangePerk : BaseCollectable
            {
                public CollectableRangePerk(Vector3 position) : base(position) { }

                public override void Create()
                {
                    entity = GameManager.server.CreateEntity("assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_point_red.prefab", position);
                    entity.enableSaving = false;
                    entity.Spawn();
                    ScaleEntity(entity, 5f);
                }
            }

            private class CollectableSpeedPerk : BaseCollectable
            {
                public CollectableSpeedPerk(Vector3 position) : base(position) { }

                public override void Create()
                {
                    entity = GameManager.server.CreateEntity("assets/bundled/prefabs/modding/cinematic/cinelights/cinelight_point_green.prefab", position);
                    entity.enableSaving = false;
                    entity.Spawn();
                    ScaleEntity(entity, 5f);
                }
            }

            private class BaseCollectable
            {
                public Vector3 position;
                public Vector2 Pos2D { get; }

                public BaseEntity entity;

                protected BaseCollectable(Vector3 position)
                {
                    this.position = position;
                    Pos2D = new Vector2(position.x, position.z);

                    Create();
                }

                public virtual void Create()
                {
                    _instance.PrintError("Creating collectable from base class");
                }

                public virtual int GetPoints() => 0;

                public void Destroy()
                {
                    entity?.Kill();
                }

                public static BaseCollectable SpawnNearSleigh(SleighBehavior sleigh)
                {
                    var pos = sleigh.Position.RandomPositionAround(100f, 20f);
                    pos.y = _config.eventHeight;

                    float r = Random.Range(0f, 1f);
                    if (r < _config.collectPerk.spawnChance)
                    {
                        return new CollectableRangePerk(pos);
                    }
                    else if (r < _config.speedPerk.spawnChance + _config.collectPerk.spawnChance)
                    {
                        return new CollectableSpeedPerk(pos);
                    }
                    else
                    {
                        return new CollectableDrop(pos);
                    }
                }
            }

            #endregion

            #endregion

            #region Sleigh MonoBehavior

            private class SleighBehavior : FacepunchBehaviour
            {
                private const string PICKUP_EFFECT = "assets/prefabs/misc/easter/painted eggs/effects/eggpickup.prefab";

                public BasePlayer player;
                private BaseMountable seat;
                private SantaSleigh sleigh;

                public Vector3 Position => sleigh.transform.position;

                private float currentSpeed;
                private float currentRotation;

                private readonly float acceleration = 0.1f;

                private static float maxSpeed = _config.sleighSpeed;
                private const float maxRotationSpeed = 3f;

                private float tilt;

                // Collectable vars
                private const int baseCollectDist = 2;
                private float sqrCollectDist = baseCollectDist * baseCollectDist;

                private float collectDistFraction = 1f;
                private float collectDistFractionResetTime = 0;

                private float maxSpeedFraction = 1f;
                private float maxSpeedFractionResetTime = 0;

                #region MonoBehavior

                private void Awake()
                {
                    sleigh = GetComponent<SantaSleigh>();
                    sleigh.CancelInvoke(sleigh.SendHoHoHo);

                    Enable(false);

                    seat = GameManager.server.CreateEntity(CHAIR_PREFAB, sleigh.transform.position) as BaseMountable;
                    seat.enableSaving = false;

                    seat.Spawn();
                    seat.maxMountDistance = 2f;
                    seat.isMobile = true;
                    seat.canWieldItems = false;
                    RemoveGroundChecks(seat);

                    seat.SetParent(sleigh);
                    seat.transform.localPosition = new Vector3(0f, 0.33f, -3.3f);
                }

                private void Update()
                {
                    if (player == null)
                    {
                        return;
                    }

                    if (collectDistFractionResetTime > 0 && Time.time > collectDistFractionResetTime)
                    {
                        collectDistFractionResetTime = 0;
                        collectDistFraction = 1f;

                        UpdateSqrCollectDist();
                    }
                    if (maxSpeedFractionResetTime > 0 && Time.time > maxSpeedFractionResetTime)
                    {
                        maxSpeedFractionResetTime = 0;
                        maxSpeedFraction = 1f;
                    }

                    for (int i = 0; i < sleighEvent.collectables.Count; i++)
                    {
                        Vector2 pos2D = new Vector2(sleigh.transform.position.x, sleigh.transform.position.z);
                        BaseCollectable coll;

                        try
                        {
                            coll = sleighEvent.collectables[i];
                        }
                        catch (IndexOutOfRangeException)
                        {
                            break;
                        }

                        Vector2 delta = coll.Pos2D - pos2D;
                        if (delta.sqrMagnitude <= sqrCollectDist)
                        {
                            sleighEvent.TakeCollectable(this, coll);
                            SendPickupEffect();
                        }
                    }
                }

                private void FixedUpdate()
                {
                    if (player == null)
                    {
                        return;
                    }

                    if (currentSpeed != 0)
                    {
                        if (currentRotation != 0)
                        {
                            float speedFraction = (currentSpeed / maxSpeed * maxSpeedFraction);
                            float rotAngle = currentRotation * speedFraction;
                            tilt = Mathf.Lerp(tilt, currentRotation * 5, Time.fixedDeltaTime);
                            sleigh.transform.rotation = sleigh.transform.rotation * Quaternion.Euler(0, rotAngle, 0);
                            sleigh.transform.localRotation = Quaternion.Euler(0, sleigh.transform.localEulerAngles.y, tilt * -1f);
                        }

                        Vector3 target = sleigh.transform.position + (sleigh.transform.forward * currentSpeed);
                        target.y = _config.eventHeight;

                        sleigh.gameObject.transform.position = target;
                        sleigh.transform.hasChanged = true;
                    }
                }

                private void LateUpdate()
                {
                    if (player == null)
                    {
                        return;
                    }

                    float turn = 0;
                    float forward = -0.5f;

                    if (player.serverInput.IsDown(BUTTON.LEFT))
                    {
                        turn = -1;
                    }
                    else if (player.serverInput.IsDown(BUTTON.RIGHT))
                    {
                        turn = 1;
                    }

                    if (player.serverInput.IsDown(BUTTON.FORWARD))
                    {
                        forward = 1;
                    }

                    currentSpeed = Mathf.Clamp(currentSpeed += (Time.fixedDeltaTime * acceleration) * forward, 0, maxSpeed * maxSpeedFraction);

                    if (turn != 0)
                    {
                        currentRotation = Mathf.Clamp(currentRotation += (Time.fixedDeltaTime * maxRotationSpeed) * turn, -maxRotationSpeed, maxRotationSpeed);
                    }
                    else
                    {
                        currentRotation = Mathf.Clamp(currentRotation > 0 ? currentRotation -= Time.fixedDeltaTime : currentRotation += Time.fixedDeltaTime, -maxRotationSpeed, maxRotationSpeed);
                    }
                }

                private void OnDestroy()
                {
                    if (player != null)
                    {
                        player.DismountObject();
                    } 

                    if (seat != null && !seat.IsDestroyed)
                    {
                        seat.Kill();
                    }

                    if (sleigh != null && !sleigh.IsDestroyed)
                    {
                        sleigh.Kill();
                    }
                }

                #endregion

                #region Perks

                public void ActivateSpeedPerk(float duration, float fraction)
                {
                    maxSpeedFractionResetTime = Time.time + duration;
                    maxSpeedFraction = fraction;
                }

                public void ActivatePickupPerk(float duration, float fraction)
                {
                    collectDistFractionResetTime = Time.time + duration;
                    collectDistFraction = fraction;

                    UpdateSqrCollectDist();
                }

                private void UpdateSqrCollectDist()
                {
                    sqrCollectDist = (int)Mathf.Pow(baseCollectDist * collectDistFraction, 2);
                }

                #endregion

                #region Mount

                public void Mount(BasePlayer player)
                {
                    this.player = player;

                    var pos = player.transform.position;
                    pos.y = _config.eventHeight;
                    sleigh.transform.position = pos;

                    player.MountObject(seat);

                    Enable(true);

                    InvokeRepeating(UpdateNetworkGroup, 0, 2f);
                }

                #endregion

                #region Other

                private void UpdateNetworkGroup()
                {
                    Group group = Net.sv.visibility.GetGroup(sleigh.transform.position);
                    if (seat?.net?.group != group)
                        seat.net.SwitchGroup(group);

                    if (player?.net?.group != group)
                        player.net.SwitchGroup(group);
                }

                private void Enable(bool enabled)
                {
                    this.enabled = enabled;
                    sleigh.enabled = false;
                }

                private void SendPickupEffect()
                {
                    RunEffect(PICKUP_EFFECT, player);
                }

                #endregion
            }

            #endregion

            #region Helpers

            private void BroadcastToParticipants(string msg)
            {
                foreach (var pl in enteredPlayers)
                    pl.ChatMessage(msg);
            }

            #endregion

            #region Static

            public static EventController sleighEvent;

            public static bool Start()
            {
                if (!(sleighEvent?.Finished ?? true)) return false;

                sleighEvent = new EventController(_config.eventDelay, _config.eventDuration);

                return true;
            }

            #endregion

            #region Scoreboard Class

            private class PlayerScoreboard
            {
                private List<PlayerScore> scores;
                private HashSet<ulong> trackedPlayers;

                public PlayerScoreboard()
                {
                    scores = new List<PlayerScore>();
                    trackedPlayers = new HashSet<ulong>();
                }

                public void IncrementScore(BasePlayer player, int step = 1)
                {
                    if (!trackedPlayers.Contains(player.userID)) TrackPlayer(player);

                    scores.Find(x => x.UserId == player.userID).Score += step;
                }

                public int GetScore(BasePlayer player)
                {
                    if (!trackedPlayers.Contains(player.userID)) TrackPlayer(player);

                    return scores.Find(x => x.UserId == player.userID).Score;
                }

                public List<PlayerScore> GetTopPlayers(int count)
                {
                    if (scores.Count < 1) return scores;

                    var l = scores.OrderByDescending(x => x.Score).ToList();
                    return l.GetRange(0, count > scores.Count ? scores.Count : count);
                }

                private void TrackPlayer(BasePlayer player)
                {
                    if (trackedPlayers.Contains(player.userID))
                        return;

                    trackedPlayers.Add(player.userID);
                    var sc = new PlayerScore(player);
                    scores.Add(sc);

                }

                public class PlayerScore
                {
                    public string DisplayName { get; }
                    public ulong UserId { get; }
                    public int Score { get; set; }

                    public PlayerScore(BasePlayer player)
                    {
                        DisplayName = player.displayName;
                        UserId = player.userID;
                        Score = 0;
                    }

                    public BasePlayer GetPlayer()
                    {
                        return BasePlayer.FindByID(UserId);
                    }
                }
            }

            #endregion
        }

        #endregion

        #region Helpers

        private static void ClearContainer(ItemContainer container)
        {
            List<Item> allItems = container.itemList;
            for (int i = allItems.Count - 1; i >= 0; i--)
            {
                Item item = allItems[i];
                item.RemoveFromContainer();
                item.Remove();
            }
        }

        private static void RemoveGroundChecks(BaseEntity ent)
        {
            UnityEngine.Object.DestroyImmediate(ent.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(ent.GetComponent<GroundWatch>());
        }

        private static void Broadcast(string msg)
        {
            _instance.Server.Broadcast(msg);
        }

        private static void RunEffect(string effect, BasePlayer player)
        {
            if (player == null) return;

            Effect.server.Run(effect, player, 0, Vector3.zero, Vector3.zero, null, true);
        }

        private static void ScaleEntity(BaseEntity ent, float scale)
        {
            _instance.EntityScaleManager?.Call("API_ScaleEntity", ent, 5f);
        }

        #endregion

    }
}

namespace Oxide.Plugins.SleighEventExtensions
{
    public static class SleighEventEx
    {
        public static Vector3 RandomPositionAround(this Vector3 pos, float radius, float minDistance = 0f)
        {
            float distance = Random.Range(minDistance, radius);
            float angle = Random.Range(0f, 359f);

            Vector3 delta = Quaternion.Euler(0, angle, 0) * (Vector3.forward * distance);
            return pos + delta;
        }
    }
}
