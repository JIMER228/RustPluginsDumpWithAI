using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Plugins.ShipwreckExtensionMethods;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CompanionServer.Handlers;
using UnityEngine;
using Rust;
using Time = UnityEngine.Time;

namespace Oxide.Plugins
{
    [Info("Shipwreck", "Adem", "1.1.2")]
    class Shipwreck : RustPlugin
    {
        #region Variables
        const bool en = true;
        static Shipwreck ins;
        [PluginReference] Plugin NpcSpawn, GUIAnnouncements, DiscordMessages, PveMode, Economics, ServerRewards, IQEconomic, DynamicPVP;
        HashSet<string> subscribeMetods = new HashSet<string>
        {
            "OnEntitySpawned",
            "OnCorpsePopulate",
            "OnEntityTakeDamage",
            "CanMountEntity",
            "OnEntityMounted",
            "CanExplosiveStick",
            "CanBuild",
            "CanAffordUpgrade",
            "OnStructureRotate",
            "OnDoorOpened",
            "OnCardSwipe",
            "OnEntityDeath",
            "CanHackCrate",
            "OnPlayerSleep",

            "CanEntityTakeDamage",
            "CanEntityTakeDamage",
            "OnRestoreUponDeath",
            "OnPlayerSleep"
        };
        EventController eventController;
        #endregion Variables

        #region Hooks
        void Init()
        {
            Unsubscribes();
        }

        void OnServerInitialized()
        {
            ins = this;
            UpdateConfig();
            PostLoadCheck();
            LoadDefaultMessages();
            EventLauncher.AutoStartEvent();
        }

        void Unload()
        {
            EventLauncher.StopEvent(isPluginUnloading: true);
        }

        void OnEntitySpawned(SimpleShark simpleShark)
        {
            if (!simpleShark.IsExists()) return;

            if (Vector3.Distance(eventController.transform.position, simpleShark.transform.position) < eventController.eventConfig.radius)
            {
                timer.In(0.5f, () =>
                {
                    if (simpleShark.IsExists())
                        simpleShark.Kill();
                });
            }
        }

        void OnEntitySpawned(DiveSite diveSite)
        {
            if (!diveSite.IsExists()) return;

            if (Vector3.Distance(eventController.transform.position, diveSite.transform.position) < eventController.eventConfig.radius)
                diveSite.Kill();
        }

        void OnEntitySpawned(DroppedItemContainer droppedItemContainer)
        {
            if (ins._config.mainConfig.disableFloatingCorpse && droppedItemContainer.IsExists())
                DisableCorpseFloating(droppedItemContainer);
        }

        void OnEntitySpawned(PlayerCorpse playerCorpse)
        {
            if (ins._config.mainConfig.disableFloatingCorpse && playerCorpse.IsExists())
                DisableCorpseFloating(playerCorpse);
        }

        void OnCorpsePopulate(BasePlayer entity, NPCPlayerCorpse corpse)
        {
            if (entity == null || corpse == null) return;
            if (entity is ScientistNPC)
            {
                NpcConfig npcConfig = _config.npcConfigs.FirstOrDefault(x => x.name == entity.displayName);
                if (npcConfig == null) return;
                NextTick(() =>
                {
                    if (corpse == null) return;
                    ItemContainer container = corpse.containers[0];
                    LootManager.UpdateLootContainerLootTable(container, npcConfig.lootTable, npcConfig.typeLootTable);
                    if (npcConfig.deleteCorpse && !corpse.IsDestroyed) corpse.Kill();
                });
            }
        }

        object OnEntityTakeDamage(BaseSubmarine baseSubmarine, HitInfo info)
        {
            if (!baseSubmarine.IsExists() || info == null || baseSubmarine.net == null) return null;

            PatrolSubmarine patrolSubmarine = PatrolSubmarine.GetPatrolSubmarineByNetID(baseSubmarine.net.ID.Value);
            if (patrolSubmarine != null)
            {
                if (info.InitiatorPlayer.IsRealPlayer())
                    patrolSubmarine.OnAttackedByPlayer(info.InitiatorPlayer);
                else if (info.Initiator == null)
                    return true;
            }

            if (info.Initiator == null && info.WeaponPrefab != null && info.WeaponPrefab.name == "TorpedoStraight")
                info.damageTypes.ScaleAll(_config.mainConfig.submarineDamageScale);

            return null;
        }

        object OnEntityTakeDamage(SimpleShark shark, HitInfo info)
        {
            if (!shark.IsExists() || info == null || shark.net == null) return null;

            PatrolShark patrolShark = PatrolShark.GetPatrolSharkByNetId(shark.net.ID.Value);
            if (patrolShark != null)
            {
                if (info.Initiator == null)
                    return true;
            }

            return null;
        }

        object OnEntityTakeDamage(ScientistNPC scientistNPC, HitInfo info)
        {
            if (!scientistNPC.IsExists() || info == null) return null;

            if (NpcSpawnManager.IsEventNpc(scientistNPC))
            {
                if (info.Initiator == null)
                    return true;
            }

            return null;
        }

        void OnEntityTakeDamage(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null) return;

            if (info.Initiator == null && info.WeaponPrefab != null && info.WeaponPrefab.name == "TorpedoStraight")
            {
                info.damageTypes.ScaleAll(_config.mainConfig.submarineDamageScale);
                return;
            }

            else if (info.Initiator != null)
            {
                if (info.Initiator is SimpleShark)
                {
                    PatrolShark patrolShark = PatrolShark.GetPatrolSharkByNetId(info.Initiator.net.ID.Value);
                    if (patrolShark != null)
                        info.damageTypes.ScaleAll(patrolShark.sharkConfig.damageScale);
                    return;
                }
            }
        }

        object CanMountEntity(BasePlayer player, BaseMountable seat)
        {
            if (!player.IsRealPlayer() || seat == null)
                return null;
            BaseVehicle vehicle = seat.VehicleParent();
            if (vehicle == null || vehicle.net == null)
                return null;
            PatrolSubmarine patrolSubmarine = PatrolSubmarine.GetPatrolSubmarineByNetID(vehicle.net.ID.Value);
            if (patrolSubmarine != null)
                return true;
            return null;
        }

        void OnEntityMounted(BaseMountable entity, BasePlayer player)
        {
            if (entity == null || player == null) return;

            BaseVehicle vehicle = entity.VehicleParent();
            if (vehicle == null) return;

            BaseSubmarine baseSubmarine = vehicle as BaseSubmarine;
            if (baseSubmarine != null && baseSubmarine.buoyancy.rigidBody == null)
            {
                baseSubmarine.buoyancy.rigidBody = baseSubmarine.rigidBody;
            }
        }

        object CanExplosiveStick(TimedExplosive timedExplosive, BaseSubmarine baseSubmarine)
        {
            if (!_config.mainConfig.allowStickC4 || baseSubmarine == null || baseSubmarine.net == null) return null;

            PatrolSubmarine patrolSubmarine = PatrolSubmarine.GetPatrolSubmarineByNetID(baseSubmarine.net.ID.Value);
            if (patrolSubmarine != null)
                return true;

            return null;
        }

        object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            BasePlayer player = planner.GetOwnerPlayer();

            if (player != null && ZoneController.IsPlayerNearTheEventByDistance(player.transform.position))
                return false;

            return null;
        }

        object CanAffordUpgrade(BasePlayer player, BuildingBlock block, BuildingGrade.Enum grade)
        {
            if (player == null) return null;

            if (ZoneController.IsPlayerNearTheEventByDistance(player.transform.position))
                return false;

            return null;
        }

        object OnStructureRotate(BuildingBlock block, BasePlayer player)
        {
            if (player == null) return null;

            if (ZoneController.IsPlayerNearTheEventByDistance(player.transform.position))
                return false;

            return null;
        }

        void OnDoorOpened(Door door, BasePlayer player)
        {
            if (door == null || player == null || door.OwnerID == 0) return;

            ShipContainer shipContainer = ShipContainer.GetShipContainerByDoor(door);
            if (shipContainer != null)
            {
                shipContainer.OnPlayerOpenContainerDoor(player, door);
            }
        }

        object OnCardSwipe(CardReader cardReader, Keycard card, BasePlayer player)
        {
            if (cardReader == null || card == null || player == null)
                return null;

            CardDoor cardDoor = CardDoor.GetCardDoorByCardReaderNetId(cardReader.net.ID.Value);
            if (cardDoor != null)
            {
                cardDoor.OnCardSwipe(card, player, cardReader);
                return true;
            }
            return null;
        }

        void CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (player == null || crate == null || crate.net == null) return;

            if (LootManager.IsEventCrate(crate.net.ID.Value))
            {
                EconomyManager.ActionEconomy(player.userID, "LockedCrate");
            }
        }

        void OnEntityDeath(ScientistNPC scientistNPC, HitInfo info)
        {
            if (scientistNPC == null || info == null) return;

            if (info.InitiatorPlayer.IsRealPlayer() && NpcSpawnManager.IsEventNpc(scientistNPC))
                EconomyManager.ActionEconomy(info.InitiatorPlayer.userID, "Npc");
        }

        void OnEntityDeath(SimpleShark shark, HitInfo info)
        {
            if (shark == null || info == null || shark.net == null) return;

            PatrolShark patrolShark = PatrolShark.GetPatrolSharkByNetId(shark.net.ID.Value);
            if (info.InitiatorPlayer.IsRealPlayer() && patrolShark != null)
                EconomyManager.ActionEconomy(info.InitiatorPlayer.userID, "Shark");
        }

        void OnEntityDeath(BaseSubmarine baseSubmarine, HitInfo info)
        {
            if (baseSubmarine == null || info == null || baseSubmarine.net == null) return;

            PatrolSubmarine patrolSubmarine = PatrolSubmarine.GetPatrolSubmarineByNetID(baseSubmarine.net.ID.Value);
            if (info.InitiatorPlayer.IsRealPlayer() && patrolSubmarine != null)
                EconomyManager.ActionEconomy(info.InitiatorPlayer.userID, "Submarine");
        }

        void OnEntityDeath(Door door, HitInfo info)
        {
            if (door == null || info == null || door.net == null) return;

            CardDoor cardDoor = CardDoor.GetCardDoorByDoorNetId(door.net.ID.Value);
            if (info.InitiatorPlayer.IsRealPlayer() && cardDoor != null)
                EconomyManager.ActionEconomy(info.InitiatorPlayer.userID, "Door");
        }

        void OnPlayerDeath(BasePlayer player)
        {
            if (player == null) return;
            ZoneController.OnPlayerLeaveZone(player);
        }

        void OnPlayerSleep(BasePlayer player)
        {
            if (player == null) return;
            ZoneController.OnPlayerLeaveZone(player);
        }

        #region SupportedPLugins
        object CanEntityTakeDamage(BasePlayer victim, HitInfo info)
        {
            if (eventController == null) return null;
            if (info == null || !victim.IsRealPlayer()) return null;

            if (info.InitiatorPlayer.IsRealPlayer())
            {
                if (_config.eventZone.isCreateZonePVP && ZoneController.IsPlayerInZone(victim.net.ID.Value) && ZoneController.IsPlayerInZone(info.InitiatorPlayer.net.ID.Value)) return true;
            }
            return null;
        }

        object CanEntityTakeDamage(BaseSubmarine baseSubmarine, HitInfo info)
        {
            if (eventController == null) return null;
            if (!baseSubmarine.IsExists() || info == null) return null;

            PatrolSubmarine patrolSubmarine = PatrolSubmarine.GetPatrolSubmarineByNetID(baseSubmarine.net.ID.Value);
            if (patrolSubmarine != null)
            {
                if (info.InitiatorPlayer.IsRealPlayer())
                {
                    patrolSubmarine.OnAttackedByPlayer(info.InitiatorPlayer);
                    return true;
                }
                else if (info.Initiator == null)
                    return false;
            }
            else if (info.Initiator == null && ZoneController.IsPlayerNearTheEventByDistance(baseSubmarine.transform.position))
                return true;
            return null;
        }

        object CanEntityTakeDamage(SimpleShark shark, HitInfo info)
        {
            if (!shark.IsExists() || info == null || shark.net == null) return null;

            PatrolShark patrolShark = PatrolShark.GetPatrolSharkByNetId(shark.net.ID.Value);
            if (patrolShark != null)
            {
                if (info.Initiator == null)
                    return false;
                else if (info.InitiatorPlayer.IsRealPlayer())
                    return true;
            }

            return null;
        }

        object OnRestoreUponDeath(BasePlayer player)
        {
            if (player == null)
                return null;

            if (_config.eventZone.blockRestoreUponDeath && ZoneController.IsPlayerInZone(player.userID))
                return false;

            return null;
        }
        #endregion SupportedPLugins
        #endregion Hooks

        #region Commands
        [ChatCommand("shipwreckstart")]
        void ChatStartCommand(BasePlayer player, string command, string[] arg)
        {
            if (!player.IsAdmin) return;
            if (arg != null && arg.Length >= 1) EventLauncher.StartEvent(player, arg[0]);
            else EventLauncher.StartEvent(player);
        }

        [ChatCommand("shipwreckstartmyloc")]
        void ChatStartMyPosCommand(BasePlayer player, string command, string[] arg)
        {
            if (!player.IsAdmin) return;
            if (arg != null && arg.Length >= 1) EventLauncher.StartEvent(player, arg[0], true);
            else EventLauncher.StartEvent(player, "", true);
        }

        [ChatCommand("shipwreckstop")]
        void ChatStopCommand(BasePlayer player, string command, string[] arg)
        {
            if (!player.IsAdmin) return;
            EventLauncher.StopEvent();
        }

        [ConsoleCommand("shipwreckstart")]
        void ConsoleStartCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null) return;
            if (arg.Args != null && arg.Args.Length > 0) EventLauncher.StartEvent(null, arg.Args[0]);
            else EventLauncher.StartEvent();
        }

        [ConsoleCommand("shipwreckstop")]
        void ConsoleStopCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null)
                EventLauncher.StopEvent();
        }

        [ChatCommand("screatepath")]
        void ChatCreatePathCommand(BasePlayer player, string command, string[] arg)
        {
            float step = 5;

            if (arg.Length > 0)
            {
                step = Convert.ToInt32(arg[0]);
                if (step <= 0) step = 5;
            }

            if (eventController == null)
            {
                NotifyManager.PrintError(player, "EventNoActive_Exeption", _config.prefix);
                return;
            }

            PathRecorder.CreatePathRecorder(player, step);
            NotifyManager.SendMessageToPlayer(player, "PathRecordStart", _config.prefix);
        }

        [ChatCommand("ssavepath")]
        void ChatSavePathCommand(BasePlayer player, string command, string[] arg)
        {
            if (eventController == null)
            {
                NotifyManager.PrintError(player, "EventNoActive_Exeption", _config.prefix);
                return;
            }
            PathRecorder pathRecorder = PathRecorder.GetPathRecorderByPlayerUID(player.userID);
            if (pathRecorder == null)
            {
                NotifyManager.PrintError(player, "PathSaveFailed", _config.prefix);
                return;
            }

            pathRecorder.SaveRout(arg[0]);
            NotifyManager.SendMessageToPlayer(player, "PathSaved", _config.prefix);
        }

        [ChatCommand("scancelpath")]
        void ChatCancelPathCommand(BasePlayer player, string command, string[] arg)
        {
            if (eventController == null)
            {
                NotifyManager.PrintError(player, "EventNoActive_Exeption", _config.prefix);
                return;
            }
            PathRecorder pathRecorder = PathRecorder.GetPathRecorderByPlayerUID(player.userID);
            if (pathRecorder == null)
                return;

            GameObject.Destroy(pathRecorder.gameObject);
            NotifyManager.SendMessageToPlayer(player, "PathCancelled", _config.prefix);
        }
        #endregion Commands

        #region Methods
        void UpdateConfig()
        {
            if (_config.version == null || _config.version == new VersionNumber(0, 0, 0))
            {
                ins.PrintError("The configuration file is corrupted!");
                return;
            }

            if (_config.version != Version)
            {
                if (_config.version.Minor == 0)
                {
                    if (_config.version.Patch <= 3)
                    {
                        _config.mainConfig.disableRadiationFromRadioactivrBarrels = true;
                        _config.animationConfig = new AnimationConfig
                        {
                            timeBeforeF15TakeOff = 60,
                            enableExplosionsEffects = true
                        };
                        _config.supportedPluginsConfig.pveMode.showEventOwnerNameOnMap = true;
                        _config.supportedPluginsConfig.pveMode.damage = 25;
                    }

                    if (_config.version.Patch <= 5)
                    {
                        _config.animationConfig.decorEntities = new HashSet<DataSkinnedEntityConfig>
                        {
                            new DataSkinnedEntityConfig
                            {
                                prefab = "assets/prefabs/building/wall.frame.garagedoor/wall.frame.garagedoor.prefab",
                                skin = 1788350229,
                                locations = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-7.512, 6.5, 57.093)",
                                        rotation = "(0, 90, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(7.550, 6.5, 57.093)",
                                        rotation = "(0, 90, 0)"
                                    },

                                    new LocationConfig
                                    {
                                        position = "(7.488, 6.430, -33.062)",
                                        rotation = "(0, 270, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-7.488, 6.430, -33.062)",
                                        rotation = "(0, 270, 0)"
                                    }
                                }
                            }
                        };
                    }

                    if (_config.version.Patch <= 9)
                    {
                        _config.supportedPluginsConfig.superCardConfig = new SuperCardConfig
                        {
                            enable = false,
                            skin = 1988408422
                        };
                    }
                }
                _config.version = Version;
                SaveConfig();
            }
        }

        void Unsubscribes()
        {
            foreach (string hook in subscribeMetods)
                Unsubscribe(hook);
        }

        void Subscribes()
        {
            foreach (string hook in subscribeMetods)
                Subscribe(hook);
        }

        void PostLoadCheck()
        {
            if (!NpcSpawnManager.CheckNPCSpawn())
                NextTick(() => Server.Command($"o.unload {Name}"));
            foreach (EventConfig eventConfig in _config.eventConfigs)
            {
                if (EventController.GetMonumentConfig(eventConfig) == null || EventController.LoadLocationDataFile(eventConfig) == null)
                {
                    NextTick(() => Server.Command($"o.unload {Name}"));
                    return;
                }
            }
            GuiManager.LoadImages();
        }

        void DisableCorpseFloating(BaseEntity entity)
        {
            if (entity.transform.position.y < 0)
            {
                if (Vector3.Distance(entity.transform.position, eventController.GetEventPosition()) < eventController.eventConfig.radius + 10)
                {
                    Rigidbody rigidbody = entity.GetComponent<Rigidbody>();
                    if (rigidbody != null)
                        rigidbody.mass = 100;
                }
            }
        }

        Vector3 GetPatrolInitiatePosition(PatrolCustomConfig patrolCustomConfig)
        {
            Vector3 patrolInitiatePosition = Vector3.zero;

            if (patrolCustomConfig != null)
                patrolInitiatePosition = PositionDefiner.GetGlobalPosition(ins.eventController.transform, patrolCustomConfig.patrolPathList[0].ToVector3());

            return patrolInitiatePosition;

        }
        #endregion Methods

        #region Classes
        static class EventLauncher
        {
            static Coroutine autoEventCoroutine;

            internal static void AutoStartEvent()
            {
                if (!ins._config.mainConfig.isAutoEvent) return;
                if (autoEventCoroutine != null)
                    ServerMgr.Instance.StopCoroutine(autoEventCoroutine);

                autoEventCoroutine = ServerMgr.Instance.StartCoroutine(AutoEventCorountine());
            }

            static IEnumerator AutoEventCorountine()
            {
                yield return CoroutineEx.waitForSeconds(UnityEngine.Random.Range(ins._config.mainConfig.minTimeBetweenEvents, ins._config.mainConfig.maxTimeBetweenEvents));
                StartEvent();
            }

            internal static void StartEvent(BasePlayer initiator = null, string presetName = "", bool initiatorPosition = false)
            {
                if (ins.eventController != null)
                {
                    NotifyManager.PrintError(initiator, "EventActive_Exeption");
                    return;
                }

                EventConfig eventConfig = DefineEventConfig(presetName);
                if (eventConfig == null)
                {
                    NotifyManager.PrintError(initiator, "ConfigurationNotFound_Exeption");
                    StopEvent();
                    return;
                }

                Vector3 eventPosition = Vector3.zero;
                Vector3 eventRotation = Vector3.zero;
                if (initiatorPosition)
                {
                    eventPosition = PositionDefiner.GetGroundPositionInPoint(initiator.transform.position);
                    eventRotation = new Vector3(0, initiator.viewAngles.y, 0);
                }
                else
                    eventPosition = PositionDefiner.DefineEventPosition();

                if (eventPosition == Vector3.zero)
                {
                    NotifyManager.PrintError(initiator, "LocationNotFound_Exeption");
                    StopEvent();
                    return;
                }

                GameObject gameObject = new GameObject();
                gameObject.transform.position = eventPosition;

                if (eventRotation != Vector3.zero)
                {
                    gameObject.transform.rotation = Quaternion.Euler(eventRotation);
                }

                ins.eventController = gameObject.AddComponent<EventController>();
                ins.eventController.Init(eventConfig);
            }

            static EventConfig DefineEventConfig(string eventPresetName)
            {
                if (eventPresetName != "")
                {
                    return ins._config.eventConfigs.FirstOrDefault(x => x.presetName == eventPresetName);
                }

                else
                {
                    if (!ins._config.eventConfigs.Any(x => x.automatickStart && x.chance != 0)) return null;

                    float sumChance = 0;
                    foreach (EventConfig eventConfig in ins._config.eventConfigs)
                    {
                        if (eventConfig.automatickStart)
                            sumChance += eventConfig.chance;
                    }
                    float random = UnityEngine.Random.Range(0, sumChance);
                    foreach (EventConfig eventConfig in ins._config.eventConfigs)
                    {
                        if (eventConfig.automatickStart)
                        {
                            random -= eventConfig.chance;
                            if (random <= 0) return eventConfig;
                        }
                    }

                    return null;
                }
            }

            internal static void StopEvent(bool isPluginUnloading = false)
            {
                if (ins.eventController != null)
                {
                    ins.Unsubscribes();
                    ZoneController.DeleteZone();
                    GameObject.Destroy(ins.eventController.gameObject);
                    NotifyManager.SendMessageToAll("Finish", ins._config.prefix);
                    Interface.CallHook("OnShipwreckStop");
                    if (ins._config.mainConfig.enableStartStopLogs) NotifyManager.PrintLogMessage("EventStop_Log");
                }

                if (!isPluginUnloading)
                    AutoStartEvent();
                else if (autoEventCoroutine != null)
                    ServerMgr.Instance.StopCoroutine(autoEventCoroutine);
            }
        }

        sealed class EventController : FacepunchBehaviour
        {
            internal EventConfig eventConfig { get; private set; }
            MapMarker mapMarker;
            Coroutine eventCoroutine;
            CargoSinkEffect cargoSinkEffect;

            HashSet<PatrolNpc> patrolNpcs = new HashSet<PatrolNpc>();
            HashSet<BaseEntity> decorEntities = new HashSet<BaseEntity>();
            int eventTime;

            internal int GetEventTime()
            {
                return eventTime;
            }

            internal Vector3 GetEventPosition()
            {
                return gameObject.transform.position;
            }

            internal void Init(EventConfig eventConfig)
            {
                this.eventConfig = eventConfig;
                eventTime = eventConfig.eventTime;
                eventCoroutine = ServerMgr.Instance.StartCoroutine(EventCoroutine());
            }

            IEnumerator EventCoroutine()
            {
                if (ins._config.mainConfig.preStartTime > 0) NotifyManager.SendMessageToAll("PreStart", ins._config.prefix, ins._config.mainConfig.preStartTime);
                yield return CoroutineEx.waitForSeconds(ins._config.mainConfig.preStartTime);

                if (ins._config.marker.useRingMarker || ins._config.marker.useShopMarker)
                    mapMarker = MapMarker.CreateMarker(gameObject.transform.position);

                if (eventConfig.enableCargoSinkEffect)
                {
                    if (ins._config.animationConfig.timeBeforeF15TakeOff > 0) NotifyManager.SendMessageToAll("Animation_Stage_1", ins._config.prefix, ins._config.animationConfig.timeBeforeF15TakeOff);

                    yield return CargoSinkCorountine();
                }

                yield return BuilderCorountine();

                ins.Subscribes();
                Interface.CallHook("OnShipwreckStart");
                NotifyManager.SendMessageToAll("EventStart", ins._config.prefix, eventConfig.displayName, PhoneController.PositionToGridCoord(transform.position));
                if (ins._config.mainConfig.enableStartStopLogs) NotifyManager.PrintLogMessage("EventStart_Log", eventConfig.presetName);

                yield return TimeCounter();

                EventLauncher.StopEvent();
            }

            IEnumerator CargoSinkCorountine()
            {
                Vector3 cargoPosition = new Vector3(transform.position.x, 0, transform.position.z);
                cargoSinkEffect = CargoSinkEffect.CreateCargoSinkControoler(cargoPosition, transform.rotation);
                yield return CoroutineEx.waitForSeconds(ins._config.animationConfig.timeBeforeF15TakeOff);
                if (cargoSinkEffect != null)
                {
                    cargoSinkEffect.StartAnimation();
                    NotifyManager.SendMessageToAll("Animation_Stage_2", ins._config.prefix);
                }

                while (cargoSinkEffect != null)
                {
                    yield return CoroutineEx.waitForSeconds(1f);
                }
                yield return null;
            }

            IEnumerator BuilderCorountine()
            {
                DeleteEntitiesInEventArea();

                HashSet<DataFileEntity> entitiesDataFile = LoadLocationDataFile(eventConfig);
                if (entitiesDataFile != null)
                {
                    foreach (DataFileEntity dataFileEntity in entitiesDataFile)
                    {
                        BaseEntity decorEntity = BuildManager.CreateDataFileEntity(dataFileEntity, gameObject.transform);
                        if (decorEntity != null) decorEntities.Add(decorEntity);
                        yield return null;
                    }
                }
                yield return null;

                MonumentConfig monumentConfig = GetMonumentConfig(eventConfig);
                if (monumentConfig != null && monumentConfig.shipContainerConfigs != null)
                {
                    foreach (LocationConfig locationConfig in monumentConfig.shipContainerConfigs.locations)
                    {
                        Vector3 globalPosition = PositionDefiner.GetGlobalPosition(transform, locationConfig.position.ToVector3());
                        Quaternion globalRotation = PositionDefiner.GetGlobalRotation(transform, locationConfig.rotation.ToVector3());

                        string cratePrefab = DefineRandomPrefab(monumentConfig.shipContainerConfigs.probabilities);
                        if (cratePrefab != null)
                            ShipContainer.BuildShipContainer(cratePrefab, globalPosition, globalRotation);
                        yield return null;
                    }
                }

                if (monumentConfig != null && monumentConfig.crates != null)
                {
                    foreach (DataEntityConfig crateData in monumentConfig.crates)
                    {
                        foreach (LocationConfig locationConfig in crateData.locations)
                        {
                            Vector3 globalPosition = PositionDefiner.GetGlobalPosition(transform, locationConfig.position.ToVector3());
                            Quaternion globalRotation = PositionDefiner.GetGlobalRotation(transform, locationConfig.rotation.ToVector3());

                            LootManager.CreateCrate(crateData.prefab, globalPosition, globalRotation);
                        }
                    }
                }

                if (monumentConfig != null && monumentConfig.raidDoorsConfigs != null)
                {
                    foreach (DataSkinnedEntityConfig doorData in monumentConfig.raidDoorsConfigs)
                    {
                        foreach (LocationConfig locationConfig in doorData.locations)
                        {
                            Vector3 globalPosition = PositionDefiner.GetGlobalPosition(transform, locationConfig.position.ToVector3());
                            Quaternion globalRotation = PositionDefiner.GetGlobalRotation(transform, locationConfig.rotation.ToVector3());

                            Door door = BuildManager.CreateStaticEntity(doorData.prefab, globalPosition, globalRotation, doorData.skin) as Door;
                            if (door != null)
                            {
                                if (eventConfig.doorType != 2)
                                    door.canHandOpen = false;

                                decorEntities.Add(door);
                            }
                            yield return null;
                        }
                    }
                }

                if (monumentConfig != null && monumentConfig.cardReaderConfigs != null)
                {
                    foreach (DataCardDoorConfig dataCardDoorConfig in monumentConfig.cardReaderConfigs)
                    {
                        CardDoor cardDoor = new CardDoor(dataCardDoorConfig, eventConfig.doorType);
                        yield return null;
                    }
                }

                if (monumentConfig != null && monumentConfig.resoursesConfigs != null)
                {
                    foreach (LocationConfig locationConfig in monumentConfig.resoursesConfigs.locations)
                    {
                        string resoursePrefab = DefineRandomPrefab(monumentConfig.resoursesConfigs.probabilities);
                        BaseEntity baseEntity = BuildManager.CreateStaticEntityInLocalCoordinates(resoursePrefab, transform, locationConfig.position.ToVector3(), locationConfig.rotation.ToVector3());
                        decorEntities.Add(baseEntity);
                        yield return null;
                    }
                }

                CreatePatrolEntities();

                ZoneController.CreateZone(gameObject.transform.position);

                yield return null;
            }

            string DefineRandomPrefab(HashSet<ProbabilityConfig> probabilityConfigs)
            {
                int counter = 0;

                while (counter < 20)
                {
                    counter++;

                    foreach (ProbabilityConfig probabilityConfig in probabilityConfigs)
                    {
                        if (UnityEngine.Random.Range(0.0f, 100.0f) <= probabilityConfig.probaility)
                        {
                            return probabilityConfig.prefab;
                        }
                    }
                }

                return null;
            }

            IEnumerator TimeCounter()
            {
                while (eventTime > 0 || (ins._config.mainConfig.dontStopEventIfPlayerInZone && ZoneController.AnyPlayerInZone()))
                {
                    if (eventTime > 0) eventTime--;
                    SendRemainTimeMessage();
                    GuiManager.UpdateCountdownGui(eventTime);
                    yield return CoroutineEx.waitForSeconds(1f);
                }
            }

            internal static HashSet<DataFileEntity> LoadLocationDataFile(EventConfig eventConfig)
            {
                string filePath = $"{ins.Name}/Prefabs/";
                HashSet<DataFileEntity> locationsDataFile = Interface.Oxide.DataFileSystem.ReadObject<HashSet<DataFileEntity>>(filePath + eventConfig.dataFileName);
                if (locationsDataFile == null || locationsDataFile.Count == 0)
                {
                    NotifyManager.PrintError(null, "FileNotFound_Exeption", eventConfig.presetName, filePath);
                    return null;
                }
                return locationsDataFile;
            }

            internal static MonumentConfig GetMonumentConfig(EventConfig eventConfig)
            {
                string filePath = $"{ins.Name}/Configs/";
                MonumentConfig monumentConfig = Interface.Oxide.DataFileSystem.ReadObject<MonumentConfig>(filePath + eventConfig.dataFileName);
                if (monumentConfig == null || monumentConfig.crates == null)
                {
                    NotifyManager.PrintError(null, "FileNotFound_Exeption", eventConfig.presetName, filePath);
                    return null;
                }
                return monumentConfig;
            }

            void DeleteEntitiesInEventArea()
            {
                foreach (Collider collider in UnityEngine.Physics.OverlapSphere(gameObject.transform.position, eventConfig.radius))
                {
                    BaseEntity entity = collider.ToBaseEntity();
                    if (entity.IsExists() && (entity is SimpleShark || entity is DiveSite || entity is FreeableLootContainer || entity is SimpleShark))
                        entity.Kill();
                }
            }

            void CreatePatrolEntities()
            {
                foreach (PatrolCustomConfig patrolConfig in eventConfig.customPatrolRoutes)
                    CreatePatrolEntity(patrolConfig);
            }

            void CreatePatrolEntity(PatrolCustomConfig patrolConfig)
            {
                SharkConfig sharkConfig = ins._config.sharkConfigs.FirstOrDefault(x => x.presetName == patrolConfig.presetName);
                if (sharkConfig != null)
                {
                    PatrolShark eventShark = PatrolShark.CreateShark(sharkConfig, patrolConfig);
                    return;
                }
                SubmarineConfig submarineConfig = ins._config.submarineConfigs.FirstOrDefault(x => x.presetName == patrolConfig.presetName);
                if (submarineConfig != null)
                {
                    PatrolSubmarine patrolSubmarine = PatrolSubmarine.CreatePatrolCubmarine(submarineConfig, patrolConfig);
                    return;
                }
                NpcConfig npcConfig = ins._config.npcConfigs.FirstOrDefault(x => x.presetName == patrolConfig.presetName);
                if (npcConfig != null)
                {
                    PatrolNpc patrolNpc = PatrolNpc.CreatePatrolNpc(npcConfig, patrolConfig);
                    patrolNpcs.Add(patrolNpc);
                    return;
                }
            }

            void SendRemainTimeMessage()
            {
                if (ins._config.mainConfig.timeNotifications.Contains(eventTime))
                {
                    NotifyManager.SendMessageToAll("RemainTime", ins._config.prefix, eventConfig.displayName, eventTime);
                }
            }

            void OnDestroy()
            {
                if (eventCoroutine != null)
                    ServerMgr.Instance.StopCoroutine(eventCoroutine);
                if (mapMarker != null)
                    mapMarker.DeleteMarker();
                foreach (BaseEntity entity in decorEntities)
                {
                    if (!entity.IsExists()) continue;
                    BaseSubmarine baseSubmarine = entity as BaseSubmarine;
                    if (baseSubmarine != null && baseSubmarine.buoyancy != null && baseSubmarine.buoyancy.rigidBody != null)
                        continue;
                    entity.Kill();
                }

                PatrolNpc.KillAllPatrolNpcs();
                PatrolShark.KillAllSharks();
                PatrolSubmarine.KillAllSubmarines();
                ShipContainer.KillAllShipContainers();
                PathRecorder.KillAllPathRecorders();
                LootManager.DestroyEventCrates();
                CardDoor.KillAllCardDoors();

                if (cargoSinkEffect != null)
                    cargoSinkEffect.Destroy();

                EconomyManager.OnEventEnd();
                GuiManager.DestroyGuiForAllPLayers();
            }
        }

        sealed class CargoSinkEffect : FacepunchBehaviour
        {
            BaseEntity cargoShip;
            HashSet<BaseEntity> decorEntities = new HashSet<BaseEntity>();
            PlanesController planesController;
            Coroutine cargoSinkCorountine;
            GameObject positionGameObject;

            static List<string> effects = new List<string>
            {
                "assets/bundled/prefabs/fx/explosions/explosion_01.prefab",
                "assets/bundled/prefabs/fx/explosions/explosion_02.prefab",
                "assets/bundled/prefabs/fx/explosions/explosion_03.prefab",
                "assets/bundled/prefabs/fx/gas_explosion_small.prefab",
                "assets/bundled/prefabs/fx/survey_explosion.prefab",
                "assets/bundled/prefabs/fx/weapons/landmine/landmine_explosion.prefab"
            };
            static List<AnimationPosition> animationData = new List<AnimationPosition>
            {
                new AnimationPosition
                {
                    moveTime = 10,
                    position = new Vector3(0, 0, 0),
                    rotation = new Vector3(0, 0, 30),
                },
                new AnimationPosition
                {
                    moveTime = 10,
                    position = new Vector3(0, -5, 0),
                    rotation = new Vector3(350, 0, 30),
                },
                new AnimationPosition
                {
                    moveTime = 40,
                    position = new Vector3(0, -35, 0),
                    rotation = new Vector3(0, 10, 30),
                    speed = 0.22f
                },
                new AnimationPosition
                {
                    moveTime = 50,
                    position = new Vector3(0, -60, 0),
                    rotation = new Vector3(0, 10, 30),
                    speed = 0.25f
                }
            };

            internal static CargoSinkEffect CreateCargoSinkControoler(Vector3 position, Quaternion rotation)
            {
                BaseEntity cargoShip = BuildManager.CreateDecorEntity("assets/content/vehicles/boats/cargoship/cargoshiptest.prefab", position, rotation);
                CargoSinkEffect cargoSinkEffect = cargoShip.gameObject.AddComponent<CargoSinkEffect>();
                cargoSinkEffect.Init(cargoShip);
                return cargoSinkEffect;
            }

            void Init(BaseEntity cargoShip)
            {
                this.cargoShip = cargoShip;
                positionGameObject = new GameObject();
                positionGameObject.transform.position = cargoShip.transform.position;
                positionGameObject.transform.rotation = cargoShip.transform.rotation;

                SpawnDecorEntities();
            }

            void SpawnDecorEntities()
            {
                foreach (DataSkinnedEntityConfig dataSkinnedEntityConfig in ins._config.animationConfig.decorEntities)
                {
                    foreach (LocationConfig locationConfig in dataSkinnedEntityConfig.locations)
                    {
                        BaseEntity entity = BuildManager.CreateChildEntity(cargoShip, dataSkinnedEntityConfig.prefab, locationConfig.position.ToVector3(), locationConfig.rotation.ToVector3(), dataSkinnedEntityConfig.skin);
                        decorEntities.Add(entity);
                    }
                }
            }

            internal void StartAnimation()
            {
                planesController = PlanesController.CreatePlaneController(this);
            }

            internal void StartcargoSinkAnimation()
            {
                cargoSinkCorountine = ServerMgr.Instance.StartCoroutine(CargoSinkCorountine());
            }

            IEnumerator CargoSinkCorountine()
            {
                float animationStartTime = 0;
                int currentPointIndex = 0;
                float lastExplodeTime = 0;
                AnimationPosition animationPosition = null;

                while (cargoShip.IsExists())
                {
                    if (currentPointIndex >= animationData.Count)
                        break;

                    AnimationPosition newAnimation = animationData[currentPointIndex];

                    if (newAnimation != animationPosition)
                    {
                        animationPosition = newAnimation;
                        animationStartTime = Time.realtimeSinceStartup;
                    }
                    if (Time.realtimeSinceStartup - animationStartTime >= animationPosition.moveTime)
                        currentPointIndex++;
                    if (animationPosition == null)
                        break;

                    if (ins._config.animationConfig.enableExplosionsEffects && Time.realtimeSinceStartup - lastExplodeTime >= 0.15f)
                    {
                        ExplodeEffect();
                        lastExplodeTime = Time.realtimeSinceStartup;
                    }

                    MoveCargo(animationPosition);

                    yield return null;
                }

                Destroy();
            }

            void ExplodeEffect()
            {
                Vector3 randomLocalPosition = new Vector3(UnityEngine.Random.Range(-12, 12), UnityEngine.Random.Range(7, 11), UnityEngine.Random.Range(-60, 80));
                Vector3 randomGlobalPosition = PositionDefiner.GetGlobalPosition(cargoShip.transform, randomLocalPosition);
                if (randomGlobalPosition.y < 0) return;

                string randomEffect = effects.GetRandom();
                Effect.server.Run(randomEffect, randomGlobalPosition);
            }

            void MoveCargo(AnimationPosition animationPosition)
            {
                Vector3 globalPosition = PositionDefiner.GetGlobalPosition(positionGameObject.transform, animationPosition.position);
                Quaternion globalRotation = PositionDefiner.GetGlobalRotation(positionGameObject.transform, animationPosition.rotation);

                cargoShip.transform.position = Vector3.Lerp(cargoShip.transform.position, globalPosition, 0.001f * animationPosition.speed);
                cargoShip.transform.rotation = Quaternion.Lerp(cargoShip.transform.rotation, globalRotation, 0.001f * animationPosition.speed);
            }

            internal void Destroy()
            {
                if (cargoShip.IsExists())
                    cargoShip.Kill();
            }

            void OnDestroy()
            {
                if (cargoSinkCorountine != null)
                    ServerMgr.Instance.StopCoroutine(cargoSinkCorountine);

                if (planesController != null)
                {
                    planesController.StopPlaneAnimation();
                    planesController = null;
                }
            }

            class AnimationPosition
            {
                internal float moveTime;
                internal float speed = 1;
                internal Vector3 position;
                internal Vector3 rotation;
            }

            class PlanesController : FacepunchBehaviour
            {
                CargoSinkEffect cargoSinkEffect;
                StorageContainer ammoContainer;
                BaseMountable baseMountableForFiring;

                Vector3 direction;
                List<F15> planes = new List<F15>();
                Coroutine planeAnimationCorountine;
                int animationStage;

                static internal PlanesController CreatePlaneController(CargoSinkEffect cargoSinkEffect)
                {
                    PlanesController planesController = cargoSinkEffect.gameObject.AddComponent<PlanesController>();
                    planesController.Init(cargoSinkEffect);
                    return planesController;
                }

                void Init(CargoSinkEffect cargoSinkEffect)
                {
                    this.cargoSinkEffect = cargoSinkEffect;
                    Vector3 centerPlaneSpawnPosition = -cargoSinkEffect.transform.position;
                    direction = Vector3Ex.Direction(cargoSinkEffect.transform.position, centerPlaneSpawnPosition);

                    F15 centralPlain = CreatePlane(centerPlaneSpawnPosition, direction);
                    if (centralPlain == null)
                        return;

                    Vector3 rightPlanePosition = centerPlaneSpawnPosition - direction * 25 + centralPlain.transform.right * 25;
                    CreatePlane(rightPlanePosition, direction);

                    Vector3 leftPlanePosition = centerPlaneSpawnPosition - direction * 25 - centralPlain.transform.right * 25;
                    CreatePlane(leftPlanePosition, direction);

                    CreateBaseMountable(transform.position);
                    CreateAndFillAmmoStorageContainer(transform.position);

                    planeAnimationCorountine = ServerMgr.Instance.StartCoroutine(PlaneAnimationCorountine());
                }

                F15 CreatePlane(Vector3 position, Vector3 direction)
                {
                    F15 plane = BuildManager.CreateRegularEntity("assets/scripts/entity/misc/f15/f15e.prefab", position, Quaternion.LookRotation(direction)) as F15;
                    plane.enabled = false;
                    planes.Add(plane);
                    return plane;
                }

                void CreateBaseMountable(Vector3 shipPosition)
                {
                    Vector3 spawnPosition = shipPosition - new Vector3(0, 200, 0);
                    baseMountableForFiring = BuildManager.CreateStaticEntity("assets/prefabs/vehicle/seats/testseat.prefab", spawnPosition, Quaternion.identity) as BaseMountable;
                }

                void CreateAndFillAmmoStorageContainer(Vector3 shipPosition)
                {
                    Vector3 spawnPosition = shipPosition - new Vector3(0, 200, 0);
                    ammoContainer = BuildManager.CreateStaticEntity("assets/prefabs/deployable/woodenbox/woodbox_deployed.prefab", spawnPosition, Quaternion.identity) as StorageContainer;
                    Item item = ItemManager.CreateByName("ammo.rocket.mlrs", 10);
                    item.MoveToContainer(ammoContainer.inventory);
                    ammoContainer.limitNetworking = true;
                }

                internal bool CheckPlanes()
                {
                    return !planes.Any(x => !x.IsExists()) && ammoContainer.IsExists() && baseMountableForFiring.IsExists();
                }

                IEnumerator PlaneAnimationCorountine()
                {
                    while (CheckPlanes() && animationStage != 4)
                    {
                        UpdatePlaneAnimation();
                        yield return CoroutineEx.waitForFixedUpdate;
                    }
                    StopPlaneAnimation();
                }

                internal void UpdatePlaneAnimation()
                {
                    F15 centerPlane = planes[0];

                    float distance = Vector3.Distance(cargoSinkEffect.transform.position, centerPlane.transform.position);

                    if (animationStage == 0)
                    {
                        if (distance < 750)
                            animationStage = 1;
                        foreach (F15 f15 in planes)
                        {
                            f15.transform.position += direction * 8;
                            f15.transform.rotation = Quaternion.LookRotation(direction);
                        }

                    }
                    else if (animationStage == 1)
                    {
                        Vector3 secondStageDirection = Vector3Ex.Direction(cargoSinkEffect.transform.position + Vector3.up * 10, centerPlane.transform.position);
                        if (distance < 300)
                        {
                            FirePlane(centerPlane, secondStageDirection);
                            Invoke(() => FirePlane(planes[1], secondStageDirection), 0.25f);
                            Invoke(() => FirePlane(planes[2], secondStageDirection), 0.5f);

                            animationStage = 2;
                        }

                        foreach (F15 f15 in planes)
                        {
                            f15.transform.position += secondStageDirection * 8;
                            f15.transform.rotation = Quaternion.LookRotation(secondStageDirection);
                        }
                    }
                    else if (animationStage == 2)
                    {
                        Vector3 mainPLaneDirection = Vector3Ex.Direction(transform.position, centerPlane.transform.position);

                        if (Vector3.Angle(direction, mainPLaneDirection) > 90)
                        {
                            animationStage = 3;
                            planes[1].SetFlag(F15.Flags.Reserved1, true);
                            planes[2].SetFlag(F15.Flags.Reserved2, true);
                            cargoSinkEffect.StartcargoSinkAnimation();
                        }

                        foreach (F15 f15 in planes)
                        {
                            f15.transform.position += f15.transform.forward * 8;
                            f15.transform.rotation = Quaternion.Lerp(f15.transform.rotation, Quaternion.LookRotation(direction), 0.2f);
                        }
                    }
                    else if (animationStage == 3)
                    {
                        Vector3 mainPLaneDirection = Vector3Ex.Direction(cargoSinkEffect.transform.position, planes[0].transform.position);

                        foreach (F15 f15 in planes)
                        {
                            f15.transform.position += f15.transform.forward * 8;
                        }

                        if (Vector3.Distance(centerPlane.transform.position, cargoSinkEffect.transform.position) > 2000)
                        {
                            animationStage = 4;
                        }

                        planes[1].transform.rotation = Quaternion.Lerp(planes[1].transform.rotation, Quaternion.LookRotation(centerPlane.transform.right), 0.02f);
                        planes[2].transform.rotation = Quaternion.Lerp(planes[2].transform.rotation, Quaternion.LookRotation(-centerPlane.transform.right), 0.02f);
                    }
                }
                void FirePlane(BaseEntity plane, Vector3 direction)
                {
                    ServerProjectile serverProjectile;
                    if (baseMountableForFiring.TryFireProjectile(ammoContainer, AmmoTypes.MLRS_ROCKET, plane.transform.position, direction, null, 0f, 0f, out serverProjectile))
                    {
                        serverProjectile.gravityModifier = 0;
                    }
                }

                internal void StopPlaneAnimation()
                {
                    if (planeAnimationCorountine != null)
                        ServerMgr.Instance.StopCoroutine(planeAnimationCorountine);

                    foreach (F15 f15 in planes)
                        if (f15.IsExists())
                            f15.Kill();

                    if (baseMountableForFiring.IsExists())
                        baseMountableForFiring.Kill();
                    if (ammoContainer.IsExists())
                        ammoContainer.Kill();
                }
            }
        }

        sealed class PatrolSubmarine : FacepunchBehaviour
        {
            BaseSubmarine baseSubmarine;
            SubmarineBrain submarineBrain;
            SirenLight sirenLight;
            static HashSet<PatrolSubmarine> patrolSubmarines = new HashSet<PatrolSubmarine>();

            internal static PatrolSubmarine CreatePatrolCubmarine(SubmarineConfig submarineConfig, PatrolCustomConfig patrolConfig)
            {
                Vector3 spawnPosition = ins.GetPatrolInitiatePosition(patrolConfig);
                if (spawnPosition == Vector3.zero) return null;

                string submarinePrefab = submarineConfig.submarineType == 0 ? "assets/content/vehicles/submarine/submarinesolo.entity.prefab" : "assets/content/vehicles/submarine/submarineduo.entity.prefab";
                BaseSubmarine baseSubmarine = BuildManager.CreateRegularEntity(submarinePrefab, spawnPosition, Quaternion.identity) as BaseSubmarine;

                baseSubmarine.buoyancy.buoyancyScale = 0;

                PatrolSubmarine patrolSubmarine = baseSubmarine.gameObject.AddComponent<PatrolSubmarine>();
                patrolSubmarine.Init(baseSubmarine, submarineConfig, patrolConfig);
                patrolSubmarines.Add(patrolSubmarine);

                return patrolSubmarine;
            }

            internal static PatrolSubmarine GetPatrolSubmarineByNetId(ulong netID)
            {
                return patrolSubmarines.FirstOrDefault(x => x != null && x.baseSubmarine.IsExists() && x.baseSubmarine.net.ID.Value == netID);
            }

            internal static void KillAllSubmarines()
            {
                foreach (PatrolSubmarine patrolSubmarine in patrolSubmarines)
                {
                    if (patrolSubmarine != null)
                        patrolSubmarine.KillSubmarine();
                }

                patrolSubmarines.Clear();
            }

            internal static PatrolSubmarine GetPatrolSubmarineByNetID(ulong netID)
            {
                return patrolSubmarines.FirstOrDefault(x => x != null && x.baseSubmarine.net.ID.Value == netID);
            }

            void Init(BaseSubmarine baseSubmarine, SubmarineConfig submarineConfig, PatrolCustomConfig patrolConfig)
            {
                this.baseSubmarine = baseSubmarine;
                UpdateBaseSubmarine(submarineConfig);

                submarineBrain = baseSubmarine.gameObject.AddComponent<SubmarineBrain>();
                submarineBrain.Init(this, submarineConfig, patrolConfig);

                CreateSirenLight();
                baseSubmarine.enabled = false;
            }

            void UpdateBaseSubmarine(SubmarineConfig submarineConfig)
            {
                baseSubmarine.InitializeHealth(submarineConfig.health, submarineConfig.health);

                baseSubmarine.rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                baseSubmarine.rigidBody.useGravity = false;
                baseSubmarine.rigidBody.isKinematic = true;

                baseSubmarine.buoyancy = new Buoyancy();

                baseSubmarine.CancelInvoke(baseSubmarine.UpdateClients);

                baseSubmarine.SetFlag(BaseSubmarine.Flags.Reserved5, true);
                baseSubmarine.SetFlag(BaseSubmarine.Flags.On, true);

                BaseMountable.FixedUpdateMountables.Remove(baseSubmarine);
            }

            void CreateSirenLight()
            {
                Vector3 localPosition = baseSubmarine.PrefabName.Contains("solo") ? new Vector3(0, 1.828f, 0) : new Vector3(0, 1.828f, -0.271f);
                sirenLight = BuildManager.CreateChildEntity(baseSubmarine, "assets/prefabs/deployable/playerioents/lights/sirenlight/electric.sirenlight.deployed.prefab", localPosition, Vector3.zero) as SirenLight;
                sirenLight.UpdateFromInput(10, 0);
            }

            internal void OnAttackedByPlayer(BasePlayer player)
            {
                submarineBrain.OnAttackedByPlayer(player);
            }

            internal void KillSubmarine()
            {
                if (baseSubmarine.IsExists())
                    baseSubmarine.Kill(BaseNetworkable.DestroyMode.Gib);
            }

            void OnDestroy()
            {
                for (int i = 0; i < baseSubmarine.mountPoints.Count; i++)
                {
                    BaseVehicle.MountPointInfo mountPointInfo = baseSubmarine.mountPoints[i];
                    if (mountPointInfo.mountable._mounted.IsExists())
                        mountPointInfo.mountable._mounted.Kill();
                }
            }

            class SubmarineBrain : FacepunchBehaviour
            {
                PatrolSubmarine patrolSubmarine;
                SubmarineConfig submarineConfig;
                PathController pathController;

                BaseState[] states;
                BaseState currentState;

                BasePlayer target;
                float lastTargetVisible;
                bool isTargetVisible = false;

                const float targetUpdateTick = 2f;
                const int barrierLayers = 1 << 16 | 1 << 21 | 1 << 27;

                internal void Init(PatrolSubmarine patrolSubmarine, SubmarineConfig submarineConfig, PatrolCustomConfig patrolConfig)
                {
                    this.patrolSubmarine = patrolSubmarine;
                    this.submarineConfig = submarineConfig;

                    states = new BaseState[2];
                    states[0] = new IdleState(this);
                    states[1] = new AttackState(this);

                    pathController = new PathController(patrolConfig, patrolSubmarine.transform.position, Vector3.zero);

                    InvokeRepeating(() => UpdateTarget(), targetUpdateTick, targetUpdateTick);
                }

                void UpdateTarget()
                {
                    if (target == null)
                        FindNewTarget();
                    else
                        CheckCurrentTarget();
                }

                void FindNewTarget()
                {
                    HashSet<BasePlayer> playersInZone = ZoneController.GetPlayersInZone();
                    foreach (BasePlayer player in playersInZone)
                    {
                        if (!player.IsRealPlayer() || player.limitNetworking)
                            continue;
                        if (CheckDistance(player, submarineConfig.targetDetectionRange) && CheckVisionCone(player, submarineConfig.visionCone) && IsVisible(player))
                        {
                            target = player;
                            lastTargetVisible = Time.realtimeSinceStartup;
                            return;
                        }
                    }
                }

                void CheckCurrentTarget()
                {
                    if (IsVisible(target) && target.transform.position.y < 0)
                    {
                        lastTargetVisible = Time.realtimeSinceStartup;
                        isTargetVisible = true;
                    }
                    else
                        isTargetVisible = false;

                    if (!CheckDistance(target, submarineConfig.targetLossRange))
                        target = null;

                    else if (Time.realtimeSinceStartup - lastTargetVisible > submarineConfig.memoryDuration)
                        target = null;
                }

                internal void OnAttackedByPlayer(BasePlayer player)
                {
                    if (target == null || !IsVisible(target))
                        target = player;
                }

                bool IsVisible(BasePlayer player)
                {
                    if (player.limitNetworking)
                        return false;

                    Vector3 direction = Vector3Ex.Direction(player.eyes.position, transform.position);
                    float distance = Vector3.Distance(transform.position, player.eyes.position);

                    RaycastHit hitInfo;
                    bool isVisible = !Physics.Raycast(transform.position, direction, out hitInfo, distance, barrierLayers);

                    return isVisible;
                }

                bool CheckDistance(BasePlayer target, float distance)
                {
                    return Vector3.Distance(target.transform.position, gameObject.transform.position) <= distance;
                }

                bool CheckVisionCone(BasePlayer target, float angle)
                {
                    Vector3 targetDirection = Vector3Ex.Direction(target.transform.position, patrolSubmarine.baseSubmarine.transform.position);

                    return Vector3.Angle(patrolSubmarine.baseSubmarine.transform.forward, targetDirection) <= angle;
                }

                void FixedUpdate()
                {
                    DefineCurrentState();
                    currentState.StateThink();
                }

                void DefineCurrentState()
                {
                    BaseState idealState = states.Max(x => x.GetStateWeight());
                    if (currentState != idealState)
                    {
                        if (currentState != null) currentState.StateLeave();
                        currentState = idealState;
                        currentState.StateEnter();
                    }
                }

                class IdleState : BaseState
                {

                    Vector3 destination;
                    Vector3 lastSpeed;

                    internal IdleState(SubmarineBrain submarineBrain) : base(submarineBrain)
                    {

                    }

                    internal override float GetStateWeight()
                    {
                        return 50;
                    }

                    internal override void StateEnter()
                    {
                        submarineBrain.pathController.FindBestPatrolPoint(baseSubmarine.transform.position);
                    }

                    internal override void StateThink()
                    {
                        UpdatePosition();
                        UpdateRotation();
                    }

                    void UpdatePosition()
                    {
                        destination = submarineBrain.pathController.GetNextPathPoint(baseSubmarine.transform.position);

                        Vector3 direction = Vector3Ex.Direction(destination, baseSubmarine.transform.position);
                        float destinationAngle2D = Vector2.Angle(new Vector2(baseSubmarine.transform.forward.x, baseSubmarine.transform.forward.z), new Vector2(direction.x, direction.z));

                        if (destinationAngle2D < 20)
                        {
                            Vector3 speed = direction * submarineBrain.submarineConfig.patrolSpeed;
                            float distance = Vector3.Distance(baseSubmarine.transform.position, destination);
                            if (distance < 5 && submarineBrain.pathController.isRandomPath)
                            {
                                float distanceSpeedScale = distance / 5;
                                speed *= distanceSpeedScale;
                            }
                            if (lastSpeed.magnitude < speed.magnitude)
                            {
                                speed = Vector3.Lerp(lastSpeed, speed, 0.1f);
                            }
                            lastSpeed = speed;
                            baseSubmarine.transform.position += speed / 5;
                        }
                        else if (submarineBrain.pathController.isRandomPath)
                        {
                            lastSpeed = Vector3.zero;
                        }
                    }

                    internal void UpdateRotation()
                    {
                        Vector3 direction = Vector3Ex.Direction(destination, baseSubmarine.transform.position);
                        if (submarineBrain.pathController.isRandomPath) direction.y = 0;

                        float rotationSpeedScale = submarineBrain.pathController.isRandomPath ? 0.04f : 0.1f;
                        baseSubmarine.transform.rotation = Quaternion.Lerp(baseSubmarine.transform.rotation, Quaternion.LookRotation(direction), rotationSpeedScale * submarineBrain.submarineConfig.turnSpeed);
                    }
                }

                class AttackState : BaseState
                {
                    Vector3 lastSpeed;
                    float lastShotTime;
                    float idealTargetDistance = 15;
                    StorageContainer torpedoContainer;

                    Vector3 lastTargetPosition;
                    bool haveBarrier;
                    float lastBarrierCheck;

                    internal AttackState(SubmarineBrain submarineBrain) : base(submarineBrain)
                    {
                        if (submarineBrain.submarineConfig.torpedoCount > 0)
                            AddTorpedoes(submarineBrain.submarineConfig.torpedoCount);

                        torpedoContainer = baseSubmarine.GetTorpedoContainer();
                        torpedoContainer.dropsLoot = false;
                    }

                    internal override void StateEnter()
                    {
                        lastTargetPosition = Vector3.zero;
                    }

                    void AddTorpedoes(int count)
                    {
                        Item torpedoItem = ItemManager.CreateByName("submarine.torpedo.straight", count, 0);
                        if (!torpedoItem.MoveToContainer(torpedoContainer.inventory))
                            torpedoItem.Remove();
                    }

                    internal override float GetStateWeight()
                    {
                        return submarineBrain.target == null ? 0 : 100;
                    }

                    internal override void StateThink()
                    {
                        if (submarineBrain.target == null) return;
                        UpdateTorpedo();
                        CheckBarriers();
                        FollowTarget();

                        lastTargetPosition = submarineBrain.target.transform.position;
                    }

                    void UpdateTorpedo()
                    {
                        Vector3 startTorpedoPosition = baseSubmarine.torpedoFiringPoint.position + baseSubmarine.transform.forward;

                        float timeScinceLastShot = Time.realtimeSinceStartup - lastShotTime;
                        if (timeScinceLastShot < submarineBrain.submarineConfig.timeBetweenShots)
                            return;

                        if (!submarineBrain.isTargetVisible || !submarineBrain.CheckDistance(submarineBrain.target, submarineBrain.submarineConfig.attackRange) || !submarineBrain.CheckVisionCone(submarineBrain.target, 10))
                            return;

                        if (torpedoContainer.inventory.IsEmpty())
                        {
                            if (submarineBrain.submarineConfig.torpedoCount == 0)
                                AddTorpedoes(1);
                            else
                                return;
                        }
                        Vector3 nextTargetPosition = submarineBrain.target.eyes.transform.position;
                        float distance = Vector3.Distance(submarineBrain.target.eyes.transform.position, startTorpedoPosition);
                        if (lastTargetPosition != Vector3.zero)
                        {
                            nextTargetPosition += Vector3Ex.Direction(nextTargetPosition, lastTargetPosition) * distance / 5;
                        }

                        Vector3 direction = Vector3Ex.Direction(nextTargetPosition, startTorpedoPosition);

                        ServerProjectile serverProjectile;
                        if (baseSubmarine.TryFireProjectile(torpedoContainer, AmmoTypes.TORPEDO, startTorpedoPosition, direction, null, 1f, 0, out serverProjectile))
                        {
                            serverProjectile.radius = 0.5f;
                            serverProjectile.swimRandom = 0;
                            lastShotTime = Time.realtimeSinceStartup;
                            baseSubmarine.ClientRPC(null, "TorpedoFired");
                        }
                    }

                    void CheckBarriers()
                    {
                        if (Time.realtimeSinceStartup - lastBarrierCheck < 1) return;
                        lastBarrierCheck = Time.realtimeSinceStartup;

                        RaycastHit raycastHit;
                        haveBarrier = Physics.SphereCast(baseSubmarine.transform.position, 1.5f, baseSubmarine.transform.forward, out raycastHit, 5, barrierLayers);
                    }

                    void FollowTarget()
                    {
                        UpdateRotation();
                        if (!haveBarrier && !submarineBrain.CheckDistance(submarineBrain.target, idealTargetDistance))
                        {
                            MoveToTarget();
                        }
                    }

                    void MoveToTarget()
                    {
                        Vector3 destination = submarineBrain.target.eyes.position;
                        Vector3 direction = Vector3Ex.Direction(destination, baseSubmarine.transform.position);

                        float destinationAngle2D = Vector2.Angle(new Vector2(baseSubmarine.transform.forward.x, baseSubmarine.transform.forward.z), new Vector2(direction.x, direction.z));

                        if (destinationAngle2D < 20)
                        {
                            Vector3 speed = direction * submarineBrain.submarineConfig.patrolSpeed;
                            float distance = Vector3.Distance(baseSubmarine.transform.position, destination);
                            if (distance < 5)
                            {
                                float distanceSpeedScale = distance / 5;
                                speed *= distanceSpeedScale;
                            }
                            if (lastSpeed.magnitude < speed.magnitude)
                            {
                                speed = Vector3.Lerp(lastSpeed, speed, 0.1f);
                            }
                            lastSpeed = speed;
                            baseSubmarine.transform.position += speed / 5;

                            if (baseSubmarine.transform.position.y > -1f)
                                baseSubmarine.transform.position = new Vector3(baseSubmarine.transform.position.x, -1f, baseSubmarine.transform.position.z);
                        }
                    }

                    internal void UpdateRotation()
                    {
                        Vector3 direction = Vector3Ex.Direction(submarineBrain.target.eyes.transform.position, baseSubmarine.transform.position);
                        baseSubmarine.transform.rotation = Quaternion.Lerp(baseSubmarine.transform.rotation, Quaternion.LookRotation(direction), 0.08f * submarineBrain.submarineConfig.turnSpeed);
                    }
                }

                abstract class BaseState
                {
                    protected SubmarineBrain submarineBrain;
                    protected BaseSubmarine baseSubmarine;
                    protected float submarineSize = 5;

                    internal BaseState(SubmarineBrain submarineBrain)
                    {
                        this.submarineBrain = submarineBrain;
                        baseSubmarine = submarineBrain.patrolSubmarine.baseSubmarine;
                    }

                    internal abstract float GetStateWeight();

                    internal virtual void StateEnter() { }

                    internal virtual void StateLeave() { }

                    internal virtual void StateThink() { }

                    internal void GoToPosition(Vector3 position)
                    {

                    }

                    internal void LookAtPosition(Vector3 position)
                    {
                        Vector3 direction = Vector3Ex.Direction(position, baseSubmarine.transform.position);
                        direction.y = 0;

                        baseSubmarine.transform.rotation = Quaternion.Lerp(baseSubmarine.transform.rotation, Quaternion.LookRotation(direction), 0.04f * submarineBrain.submarineConfig.turnSpeed);
                    }
                }
            }
        }

        sealed class PatrolShark : SimpleShark
        {
            static HashSet<PatrolShark> patrolSharks = new HashSet<PatrolShark>();

            internal SharkConfig sharkConfig;

            CustomSimpleState[] states;
            CustomSimpleState currentState;

            float currentSpeed;

            BasePlayer target;
            float lastSeenTargetTime;
            float lastTargetSearchTime;
            bool isTargetVisible;

            float obstacleRotateScale;
            int barrierLayers = 1 << 0 | 1 << 16 | 1 << 21 | 1 << 27;
            float detectionDistance = 7f;

            internal static PatrolShark CreateShark(SharkConfig sharkConfig, PatrolCustomConfig patrolConfig)
            {
                Vector3 spawnPosition = ins.GetPatrolInitiatePosition(patrolConfig);
                if (spawnPosition == Vector3.zero) return null;

                SimpleShark simpleShark = BuildManager.CreateRegularEntity("assets/rust.ai/agents/fish/simpleshark.prefab", spawnPosition, Quaternion.identity) as SimpleShark;
                PatrolShark patrolShark = simpleShark.gameObject.AddComponent<PatrolShark>();
                BuildManager.CopySerializableFields(simpleShark, patrolShark);
                UnityEngine.Object.DestroyImmediate(simpleShark, true);
                patrolShark.Spawn();
                patrolShark.transform.position = spawnPosition;
                patrolShark.Init(sharkConfig, patrolConfig);
                patrolSharks.Add(patrolShark);

                return patrolShark;
            }

            internal static PatrolShark GetPatrolSharkByNetId(ulong netID)
            {
                return patrolSharks.FirstOrDefault(x => x.IsExists() && x.net.ID.Value == netID);
            }

            internal static void KillAllSharks()
            {
                foreach (PatrolShark shark in patrolSharks)
                {
                    if (shark.IsExists())
                        shark.Kill();
                }

                patrolSharks.Clear();
            }

            void Init(SharkConfig sharkConfig, PatrolCustomConfig patrolConfig)
            {
                this.sharkConfig = sharkConfig;

                InitializeHealth(sharkConfig.health, sharkConfig.health);
                minSpeed = UnityEngine.Random.Range(sharkConfig.minSpeed, sharkConfig.minSpeed + 1);
                maxSpeed = sharkConfig.maxSpeed;
                minTurnSpeed = sharkConfig.minTurnSpeed;
                maxTurnSpeed = sharkConfig.maxTurnSpeed;
                aggroRange = sharkConfig.aggroRange;
                attackCooldown = sharkConfig.attackCooldown;

                states = new CustomSimpleState[2];
                states[0] = new CustomIdleState(this, patrolConfig);
                states[1] = new CustomAttackState(this);
            }

            void FixedUpdate()
            {
                UpdateTarget();
                DefineCurrentState();
                currentState.StateThink();

                UpdateObstacleAvoidance();
                UpdateSpeed();
                UpdatePosition();
                UpdateDirection();
                CheckHealthRecovery();
            }

            void UpdateTarget()
            {
                if (target != null)
                    CheckCurrentTarget();
                else
                    FindNewTarget();
            }

            void CheckCurrentTarget()
            {
                if (IsVisible(target))
                {
                    lastSeenTargetTime = Time.realtimeSinceStartup;
                    isTargetVisible = true;
                }

                if (!CheckDistance(target, sharkConfig.aggroRange * 1.5f) || Time.realtimeSinceStartup - lastSeenTargetTime > 2 || target.isMounted)
                    target = null;
            }

            void FindNewTarget()
            {
                if (Time.realtimeSinceStartup - lastTargetSearchTime < 1)
                    return;

                lastTargetSearchTime = Time.realtimeSinceStartup;

                if (!BaseNetworkable.HasCloseConnections(transform.position, sharkConfig.aggroRange))
                    return;

                BasePlayer[] playerQueryResults = new BasePlayer[64];
                int playersInSphere = Query.Server.GetPlayersInSphere(base.transform.position, sharkConfig.aggroRange, playerQueryResults);
                for (int i = 0; i < playersInSphere; i++)
                {
                    BasePlayer player = playerQueryResults[i];
                    if (!player.IsRealPlayer() || player.isMounted) continue;
                    if (IsVisible(player))
                    {
                        target = player;
                        lastSeenTargetTime = Time.realtimeSinceStartup;
                        isTargetVisible = true;
                        break;
                    }
                }
            }

            bool IsVisible(BasePlayer player)
            {
                if (player.limitNetworking)
                    return false;

                Vector3 direction = Vector3Ex.Direction(player.eyes.position, transform.position);
                float distance = Vector3.Distance(transform.position, player.eyes.position);

                RaycastHit hitInfo;
                bool isVisible = !Physics.Raycast(transform.position, direction, out hitInfo, distance, barrierLayers);

                return isVisible;
            }

            bool CheckDistance(BasePlayer target, float distance)
            {
                return Vector3.Distance(target.transform.position, gameObject.transform.position) <= distance;
            }

            void DefineCurrentState()
            {
                CustomSimpleState idealState = states.Max(x => x.GetStateWeight());
                if (currentState != idealState)
                {
                    if (currentState != null)
                        currentState.StateLeave();

                    currentState = idealState;
                    currentState.StateEnter();
                }
            }

            void UpdateDirection()
            {
                Vector3 direction = Vector3Ex.Direction(WaterClamp(destination), base.transform.position);
                if (obstacleRotateScale != 0)
                {
                    direction = -transform.forward;
                }
                if (direction == Vector3.zero) return;

                Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);

                RaycastHit hitInfo;
                if (obstacleRotateScale == 1 && !Physics.SphereCast(transform.position, obstacleDetectionRadius, direction, out hitInfo, detectionDistance, barrierLayers))
                {
                    transform.rotation = lookRotation;
                    return;
                }
                base.transform.rotation = Quaternion.Lerp(base.transform.rotation, lookRotation, GetTurnSpeed() * 0.05f);
            }

            private void UpdatePosition()
            {
                Vector3 point = transform.position + transform.forward * currentSpeed * 0.05f;
                point = WaterClamp(point);

                base.transform.position = point;
            }

            void UpdateObstacleAvoidance()
            {
                Vector3 forward = base.transform.forward;
                Vector3 position = base.transform.position;

                RaycastHit hitInfo;
                if (Physics.SphereCast(position, obstacleDetectionRadius, forward, out hitInfo, detectionDistance, barrierLayers))
                {

                    RaycastHit hitInfo1;
                    if (!Physics.SphereCast(position, obstacleDetectionRadius / 2, Vector3.Lerp(transform.forward, transform.right, 0.5f), out hitInfo1, detectionDistance, barrierLayers))
                    {
                        obstacleRotateScale = 0.5f;
                    }
                    else if (!Physics.SphereCast(position, obstacleDetectionRadius / 2, Vector3.Lerp(transform.forward, -transform.right, 0.5f), out hitInfo1, detectionDistance, barrierLayers))
                    {
                        obstacleRotateScale = -0.5f;
                    }
                    else obstacleRotateScale = 1f;
                }
                else
                {
                    obstacleRotateScale = 0;
                }
            }

            void UpdateSpeed()
            {
                if (IsStartled() && obstacleRotateScale == 0)
                    currentSpeed = Mathf.Lerp(currentSpeed, sharkConfig.maxSpeed, 0.1f);
                else
                    currentSpeed = Mathf.Lerp(currentSpeed, sharkConfig.minSpeed, 0.1f);
            }

            void CheckHealthRecovery()
            {
                if (sharkConfig.healtRecoverTime > 0 && healthFraction < 1f && TimeSinceAttacked() >= sharkConfig.healtRecoverTime)
                    health = MaxHealth();
            }

            public class CustomAttackState : CustomSimpleState
            {
                float lastAttackTime;

                public CustomAttackState(PatrolShark patrolShark) : base(patrolShark)
                {
                }

                public override void StateThink()
                {
                    patrolShark.Startle();
                    patrolShark.SetFlag(Flags.Open, true);

                    if (patrolShark.CheckDistance(patrolShark.target, 2))
                        DoAttack();
                    else
                        patrolShark.destination = patrolShark.target.transform.position;

                    base.StateThink();
                }

                public override void StateLeave()
                {
                    patrolShark.SetFlag(Flags.Open, false);
                    base.StateLeave();
                }

                void DoAttack()
                {
                    patrolShark.target.Hurt(UnityEngine.Random.Range(30f, 70f), DamageType.Bite, patrolShark);
                    Vector3 posWorld = patrolShark.WaterClamp(patrolShark.target.CenterPoint());
                    Effect.server.Run(patrolShark.bloodCloud.resourcePath, posWorld, Vector3.forward);
                    lastAttackTime = Time.realtimeSinceStartup;
                }

                public override float GetStateWeight()
                {
                    if (patrolShark.target != null && Time.realtimeSinceStartup - lastAttackTime >= patrolShark.sharkConfig.attackCooldown)
                    {
                        return 10f;
                    }
                    return 0f;
                }
            }

            public class CustomIdleState : CustomSimpleState
            {
                PathController pathController;

                public CustomIdleState(PatrolShark owner, PatrolCustomConfig patrolConfig) : base(owner)
                {
                    pathController = new PathController(patrolConfig, owner.transform.position, Vector3.zero, 2, true);
                }

                public override void StateEnter()
                {
                    pathController.FindBestPatrolPoint(patrolShark.transform.position);
                    patrolShark.destination = pathController.GetNextPathPoint(patrolShark.transform.position);

                    base.StateEnter();
                }

                public override void StateLeave()
                {
                    base.StateLeave();
                }

                public override void StateThink()
                {
                    patrolShark.destination = pathController.GetNextPathPoint(patrolShark.transform.position);
                }

                public override float GetStateWeight()
                {
                    return 1f;
                }
            }

            public abstract class CustomSimpleState
            {
                protected PatrolShark patrolShark;

                public CustomSimpleState(PatrolShark patrolShark)
                {
                    this.patrolShark = patrolShark;
                }

                public virtual void StateThink()
                {

                }

                public virtual void StateEnter()
                {

                }

                public virtual void StateLeave()
                {

                }

                public virtual float GetStateWeight()
                {
                    return 1f;
                }
            }

            new void Update()
            {

            }
        }

        sealed class PatrolNpc : FacepunchBehaviour
        {
            internal static HashSet<PatrolNpc> patrolNpcs = new HashSet<PatrolNpc>();

            MovableBaseMountable baseMountable;
            NpcConfig npcConfig;
            MovableDroppedItem movableDroppedItem;
            ScientistNPC scientistNPC;
            Vector3 destination;
            PathController pathController;
            BasePlayer target;
            float currentSpeed = 0;

            internal static PatrolNpc CreatePatrolNpc(NpcConfig npcConfig, PatrolCustomConfig patrolConfig)
            {
                Vector3 spawnPosition = ins.GetPatrolInitiatePosition(patrolConfig);
                ScientistNPC scientistNPC = NpcSpawnManager.CreateScientistNpc(npcConfig, spawnPosition);

                PatrolNpc patrolNpc = scientistNPC.gameObject.AddComponent<PatrolNpc>();
                patrolNpc.Init(npcConfig, scientistNPC, patrolConfig);
                patrolNpcs.Add(patrolNpc);
                return patrolNpc;
            }

            internal static void KillAllPatrolNpcs()
            {
                foreach (PatrolNpc patrolNpc in patrolNpcs)
                {
                    if (patrolNpc != null)
                        patrolNpc.KillNpc();
                }
                patrolNpcs.Clear();
            }

            void Init(NpcConfig npcConfig, ScientistNPC scientistNPC, PatrolCustomConfig patrolConfig)
            {
                this.npcConfig = npcConfig;
                this.scientistNPC = scientistNPC;

                movableDroppedItem = MovableDroppedItem.CreateMovableDroppedItem(scientistNPC.transform.position + new Vector3(0, 1.3f, 0), Quaternion.identity);
                Rigidbody rigidbody = movableDroppedItem.GetComponentInChildren<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    rigidbody.isKinematic = true;
                }

                baseMountable = MovableBaseMountable.CreateMovableBaseMountable(movableDroppedItem);
                baseMountable.MountPlayer(scientistNPC);

                pathController = new PathController(patrolConfig, scientistNPC.transform.position, new Vector3(0, 0.5f, 0), 2, false, 1.25f);
                InvokeRepeating(UpdateTarget, 1f, 1f);
            }

            internal ulong GetNpcNetId()
            {
                if (scientistNPC == null || scientistNPC.net == null)
                    return 0;

                return scientistNPC.net.ID.Value;
            }

            void UpdateTarget()
            {
                target = (BasePlayer)ins.NpcSpawn.Call("GetCurrentTarget", scientistNPC);
            }

            void FixedUpdate()
            {
                UpdateSpeed();
                UpdateMoving();
                baseMountable.SendNetworkUpdate();
            }

            void UpdateSpeed()
            {
                currentSpeed = Mathf.Lerp(currentSpeed, npcConfig.speed / 10, 0.1f);
            }

            void UpdateMoving()
            {
                if (pathController == null || movableDroppedItem == null)
                    return;

                UpdateDestination();
                UpdatePosition();
                UpdateRotation();
            }

            void UpdateDestination()
            {
                if (target != null)
                {
                    destination = pathController.GetBestPositionForHunting(movableDroppedItem.transform.position, target.transform.position) + new Vector3(0, 0.5f, 0);
                    if (Vector3.Distance(target.transform.position, movableDroppedItem.transform.position) < Vector3.Distance(destination, movableDroppedItem.transform.position))
                        destination = Vector3.zero;
                }
                else
                {
                    destination = pathController.GetNextPathPoint(movableDroppedItem.transform.position) + new Vector3(0, 0.5f, 0);
                }
            }

            void UpdatePosition()
            {
                if (destination == Vector3.zero)
                    return;

                if (target != null && Vector3.Distance(destination, movableDroppedItem.transform.position) < 1)
                    return;

                Vector3 direction = Vector3Ex.Direction(destination, scientistNPC.transform.position);
                movableDroppedItem.transform.position += direction * currentSpeed * 0.6f;

                movableDroppedItem.SendNetworkUpdate();
            }

            void UpdateRotation()
            {
                Vector3 targetPoint = target != null ? target.transform.position : destination;
                targetPoint.y = movableDroppedItem.transform.position.y;
                Vector3 direction = Vector3Ex.Direction(targetPoint, movableDroppedItem.transform.position);

                if (target == null)
                    scientistNPC.SetAimDirection(direction);

                direction.y -= 1.2f;

                if (direction != Vector3.zero)
                    movableDroppedItem.transform.rotation = Quaternion.Lerp(movableDroppedItem.transform.rotation, Quaternion.LookRotation(direction), 0.1f);
            }

            void KillNpc()
            {
                if (scientistNPC.IsExists())
                    scientistNPC.Kill();
            }

            void OnDestroy()
            {
                if (baseMountable.IsExists())
                    baseMountable.Kill();
            }

            sealed class MovableBaseMountable : BaseMountable
            {
                internal static MovableBaseMountable CreateMovableBaseMountable(BaseEntity parentEntity, string seatPrefab = "assets/prefabs/vehicle/seats/testseat.prefab")
                {
                    BaseMountable baseMountable = GameManager.server.CreateEntity(seatPrefab, parentEntity.transform.position) as BaseMountable;
                    baseMountable.enableSaving = false;
                    baseMountable.skinID = 45124514;
                    MovableBaseMountable movableBaseMountable = baseMountable.gameObject.AddComponent<MovableBaseMountable>();
                    BuildManager.CopySerializableFields(baseMountable, movableBaseMountable);

                    baseMountable.StopAllCoroutines();
                    UnityEngine.GameObject.DestroyImmediate(baseMountable, true);
                    BuildManager.SetParent(parentEntity, movableBaseMountable, new Vector3(0, -1f, 0), Vector3.zero);
                    movableBaseMountable.Spawn();
                    return movableBaseMountable;
                }

                public override void DismountAllPlayers()
                {

                }

                public override bool GetDismountPosition(BasePlayer player, out Vector3 res)
                {
                    res = player.transform.position;
                    return true;
                }
            }
        }

        sealed class PathController
        {
            List<Vector3> patrolPath = new List<Vector3>();
            Vector3 destination;

            Vector3 entityOffset;
            bool ringRoute;
            float entitySize;
            float pointDelta;
            int currentPositionIndex = 0;

            const int randomPathLenght = 10;
            internal bool isRandomPath;

            internal PathController(PatrolCustomConfig patrolBaseConfig, Vector3 initiatePosition, Vector3 entityOffset, float entitySize = 5f, bool shark = false, float pointDelta = 2.5f)
            {
                this.entitySize = entitySize;
                this.entityOffset = entityOffset;
                this.pointDelta = pointDelta;

                GeneratePatrolPath(patrolBaseConfig, initiatePosition, shark);
            }

            void GeneratePatrolPath(PatrolCustomConfig patrolCustomConfig, Vector3 initiatePosition, bool shark)
            {
                patrolPath.Clear();

                foreach (string stringVector in patrolCustomConfig.patrolPathList)
                {
                    Vector3 localPosition = stringVector.ToVector3();
                    Vector3 globalPosition = PositionDefiner.GetGlobalPosition(ins.eventController.transform, localPosition);
                    patrolPath.Add(globalPosition);
                }
                if (patrolPath.Count > 0)
                    destination = patrolPath[0];
                ringRoute = patrolCustomConfig.isRingRoute;
            }

            bool CheckPatrolPoint(Vector3 position, Vector3 previusPoint, Vector3 nextPoint)
            {
                if (HasObstacleOnWay(previusPoint, position)) return false;
                if (nextPoint != Vector3.zero && HasObstacleOnWay(position, nextPoint)) return false;
                return true;
            }

            bool HasObstacleOnWay(Vector3 startPosition, Vector3 endPosition)
            {
                Vector3 direction = Vector3Ex.Direction(endPosition, startPosition);
                float maxDistance = Vector3.Distance(startPosition, endPosition);
                RaycastHit hitInfo;
                return Physics.SphereCast(startPosition, entitySize, direction, out hitInfo, maxDistance, 10551553);
            }

            internal Vector3 GetNextPathPoint(Vector3 currentPosition)
            {
                if (Vector3.Distance(destination, currentPosition - entityOffset) < pointDelta)
                {
                    int nextPatrolIndex = 0;
                    if (currentPositionIndex >= patrolPath.Count - 1)
                    {
                        nextPatrolIndex = 0;
                        if (!ringRoute) patrolPath.Reverse();
                    }
                    else
                        nextPatrolIndex = currentPositionIndex + 1;
                    destination = patrolPath[nextPatrolIndex];
                    currentPositionIndex = patrolPath.IndexOf(destination);
                }
                return destination;
            }

            internal void FindBestPatrolPoint(Vector3 currentPosition)
            {
                Vector3 position = patrolPath.FirstOrDefault(x => !HasObstacleOnWay(currentPosition, x));
                if (position != null && position != Vector3.zero)
                    destination = position;
                else
                    destination = patrolPath.Min(x => Vector3.Distance(currentPosition, x));
            }

            internal Vector3 GetBestPositionForHunting(Vector3 currentPosition, Vector3 targetPosition)
            {
                int nextPointIndex = currentPositionIndex + 1;
                int previousPointIndex = currentPositionIndex - 1;

                if (nextPointIndex < patrolPath.Count)
                {
                    Vector3 nextPathPoint = patrolPath[nextPointIndex];
                    if (Vector3.Distance(targetPosition, nextPathPoint) < Vector3.Distance(destination, targetPosition))
                    {
                        destination = nextPathPoint;
                        currentPositionIndex = nextPointIndex;
                    }
                }
                if (previousPointIndex >= 0)
                {
                    Vector3 previousPathPoint = patrolPath[previousPointIndex];
                    if (Vector3.Distance(targetPosition, previousPathPoint) < Vector3.Distance(destination, targetPosition))
                    {
                        destination = previousPathPoint;
                        currentPositionIndex = previousPointIndex;
                    }
                }

                return destination;
            }
        }

        sealed class PathRecorder : FacepunchBehaviour
        {
            internal static HashSet<PathRecorder> pathRecorders = new HashSet<PathRecorder>();
            BasePlayer player;
            float step;
            List<Vector3> positions = new List<Vector3>();

            internal static void KillAllPathRecorders()
            {
                foreach (PathRecorder pathRecorder in pathRecorders)
                    if (pathRecorder != null)
                        DestroyImmediate(pathRecorder.gameObject);
                pathRecorders.Clear();
            }

            internal static PathRecorder GetPathRecorderByPlayerUID(ulong userId)
            {
                return pathRecorders.FirstOrDefault(x => x != null && x.player != null && x.player != null && x.player.userID == userId);
            }

            internal static PathRecorder CreatePathRecorder(BasePlayer player, float step)
            {
                GameObject gameObject = new GameObject();
                gameObject.transform.position = player.transform.position;
                PathRecorder pathRecorder = gameObject.AddComponent<PathRecorder>();
                pathRecorder.Init(player, step);
                pathRecorders.Add(pathRecorder);
                return pathRecorder;
            }

            void Init(BasePlayer player, float step)
            {
                this.player = player;
                this.step = step;
                positions.Add(player.transform.position);
            }

            void FixedUpdate()
            {
                float distance = Vector3.Distance(player.transform.position, positions.Last());
                if (distance > step)
                {
                    positions.Add(player.transform.position);
                }

                foreach (Vector3 vector3 in positions)
                {
                    player.SendConsoleCommand("ddraw.sphere", 1, Color.green, vector3, 0.2f);
                }
            }

            internal void SaveRout(string presetName)
            {
                List<string> stringPositions = new List<string>();
                bool ringRoute = Vector3.Distance(positions.First(), positions.Last()) < step * 2;

                foreach (Vector3 position in positions)
                {
                    Vector3 localPosition = PositionDefiner.GetLocalPosition(ins.eventController.transform, position);
                    stringPositions.Add(localPosition.ToString());
                }

                PatrolCustomConfig patrolCustomConfig = new PatrolCustomConfig
                {
                    presetName = presetName,
                    isRingRoute = ringRoute,
                    patrolPathList = stringPositions,
                };

                string eventPresetName = ins.eventController.eventConfig.presetName;
                EventConfig eventConfig = ins._config.eventConfigs.FirstOrDefault(x => x.presetName == eventPresetName);
                if (eventConfig != null)
                {
                    eventConfig.customPatrolRoutes.Add(patrolCustomConfig);
                }
                ins.SaveConfig();
                Destroy(this);
            }
        }

        sealed class ShipContainer : FacepunchBehaviour
        {
            static HashSet<ShipContainer> shipContainers = new HashSet<ShipContainer>();
            static HashSet<DoorLocationData> doorLocationDatas = new HashSet<DoorLocationData>
            {
                new DoorLocationData
                {
                    doorType= 0,
                    position = new Vector3(1.452f, -0.03f, 2.696f),
                    rotation = new Vector3(0, 0, 0)
                },
                new DoorLocationData
                {
                    doorType= 1,
                    position = new Vector3(1.453f, -0.03f, 0),
                    rotation = new Vector3(0, 0, 0)
                },
                new DoorLocationData
                {
                    doorType= 0,
                    position = new Vector3(1.452f, -0.03f, -2.696f),
                    rotation = new Vector3(0, 0, 0)
                },


                new DoorLocationData
                {
                    doorType= 0,
                    position = new Vector3(-1.452f, -0.03f, 2.696f),
                    rotation = new Vector3(0, 180, 0)
                },
                new DoorLocationData
                {
                    doorType= 1,
                    position = new Vector3(-1.453f, -0.03f, 0),
                    rotation = new Vector3(0, 180, 0)
                },
                new DoorLocationData
                {
                    doorType= 0,
                    position = new Vector3(-1.452f, -0.03f, -2.696f),
                    rotation = new Vector3(0, 180, 0)
                },


                new DoorLocationData
                {
                    doorType= 0,
                    position = new Vector3(0, 0.09f, 1.151f),
                    rotation = new Vector3(0, 90, 90)
                },
                new DoorLocationData
                {
                    doorType= 0,
                    position = new Vector3(0, 0.08f, -1.467f),
                    rotation = new Vector3(0, 90, 90)
                },
                new DoorLocationData
                {
                    doorType= 0,
                    position = new Vector3(0, 0.09f, -4.056f),
                    rotation = new Vector3(0, 90, 90)
                },

                new DoorLocationData
                {
                    doorType= 0,
                    position = new Vector3(0, 2.95f, 1.151f),
                    rotation = new Vector3(0, 90, 90)
                },
                new DoorLocationData
                {
                    doorType= 0,
                    position = new Vector3(0, 2.96f, -1.467f),
                    rotation = new Vector3(0, 90, 90)
                },
                new DoorLocationData
                {
                    doorType= 0,
                    position = new Vector3(0, 2.95f, -4.056f),
                    rotation = new Vector3(0, 90, 90)
                },

                new DoorLocationData
                {
                    doorType= 2,
                    position = new Vector3(0, -0.03f, 3.935f),
                    rotation = new Vector3(0, 90, 0)
                },
                new DoorLocationData
                {
                    doorType= 2,
                    position = new Vector3(0, -0.03f, -3.935f),
                    rotation = new Vector3(0, 270, 0)
                },

                new DoorLocationData
                {
                    doorType= 0,
                    position = new Vector3(0, -0.03f, 3.934f),
                    rotation = new Vector3(0, 90, 0)
                },
                new DoorLocationData
                {
                    doorType= 0,
                    position = new Vector3(0, -0.03f, -3.934f),
                    rotation = new Vector3(0, 270, 0)
                },
            };
            static List<DoorSkinData> doorSkinDatas = new List<DoorSkinData>
            {
                new DoorSkinData
                {
                    name = "Blue_Adem",
                    regularSkin = 2968288788,
                    logoSkin = 2968289354,
                    doorSkin = 2968290436
                },
                new DoorSkinData
                {
                    name = "Blue_MM",
                    regularSkin = 2968288788,
                    logoSkin = 2968294066,
                    doorSkin = 2968290436
                },
                new DoorSkinData
                {
                    name = "Blue_DC",
                    regularSkin = 2968288788,
                    logoSkin = 2968289786,
                    doorSkin = 2968290436
                },
                new DoorSkinData
                {
                    name = "Blue_Jtedal",
                    regularSkin = 2968288788,
                    logoSkin = 2968288066,
                    doorSkin = 2968290436
                },

                new DoorSkinData
                {
                    name = "Green_Adem",
                    regularSkin = 2977467253,
                    logoSkin = 2977467820,
                    doorSkin = 2977810173
                },
                new DoorSkinData
                {
                    name = "Green_MM",
                    regularSkin = 2977467253,
                    logoSkin = 2977472718,
                    doorSkin = 2977810173
                },
                new DoorSkinData
                {
                    name = "Green_DC",
                    regularSkin = 2977467253,
                    logoSkin = 2977474233,
                    doorSkin = 2977810173
                },
                new DoorSkinData
                {
                    name = "Green_Jtedal",
                    regularSkin = 2977467253,
                    logoSkin = 2977468697,
                    doorSkin = 2977810173
                },

                new DoorSkinData
                {
                    name = "Violet_Adem",
                    regularSkin = 2977816716,
                    logoSkin = 2977817429,
                    doorSkin = 2977830110
                },
                new DoorSkinData
                {
                    name = "Violet_MM",
                    regularSkin = 2977816716,
                    logoSkin = 2977821512,
                    doorSkin = 2977830110
                },
                new DoorSkinData
                {
                    name = "Violet_DC",
                    regularSkin = 2977816716,
                    logoSkin = 2977818991,
                    doorSkin = 2977830110
                },
                new DoorSkinData
                {
                    name = "Violet_Jtedal",
                    regularSkin = 2977816716,
                    logoSkin = 2977820252,
                    doorSkin = 2977830110
                },

                new DoorSkinData
                {
                    name = "Orange_Adem",
                    regularSkin = 2977843537,
                    logoSkin = 2977804190,
                    doorSkin = 2977811995
                },
                new DoorSkinData
                {
                    name = "Orange_MM",
                    regularSkin = 2977843537,
                    logoSkin = 2977806973,
                    doorSkin = 2977811995
                },
                new DoorSkinData
                {
                    name = "Orange_DC",
                    regularSkin = 2977843537,
                    logoSkin = 2977804666,
                    doorSkin = 2977811995
                },
                new DoorSkinData
                {
                    name = "Orange_Jtedal",
                    regularSkin = 2977843537,
                    logoSkin = 2977805295,
                    doorSkin = 2977811995
                },
            };

            LootContainer lootContainer;
            HashSet<Door> doors = new HashSet<Door>();

            internal static ShipContainer GetShipContainerByDoor(Door door)
            {
                if (door.OwnerID != 84726572)
                    return null;

                ShipContainer shipContainer = shipContainers.FirstOrDefault(x => x != null && x.doors.Any(y => y != null && y.net.ID == door.net.ID));
                return shipContainer;
            }

            internal static ShipContainer BuildShipContainer(string cratePresetName, Vector3 position, Quaternion rotation)
            {
                LootContainer lootContainer = LootManager.CreateCrate(cratePresetName, position, rotation);
                lootContainer.transform.Rotate(Vector3.up, 90);
                ShipContainer shipContainer = lootContainer.gameObject.AddComponent<ShipContainer>();
                shipContainer.Init(lootContainer);
                shipContainers.Add(shipContainer);
                return shipContainer;
            }

            internal static void KillAllShipContainers()
            {
                foreach (ShipContainer shipContainer in shipContainers)
                {
                    if (shipContainer != null && shipContainer.lootContainer.IsExists())
                        shipContainer.lootContainer.Kill();
                }

                shipContainers.Clear();
            }

            void Init(LootContainer lootContainer)
            {
                this.lootContainer = lootContainer;
                BuildDoors();
            }

            void BuildDoors()
            {
                int randomIndex = UnityEngine.Random.Range(0, doorSkinDatas.Count);
                DoorSkinData doorSkinData = doorSkinDatas[randomIndex];

                foreach (DoorLocationData doorLocationData in doorLocationDatas)
                {
                    ulong skin = doorLocationData.doorType == 0 ? doorSkinData.regularSkin : doorLocationData.doorType == 1 ? doorSkinData.logoSkin : doorSkinData.doorSkin;
                    BuildSkinDoor(doorLocationData.position, doorLocationData.rotation, skin);
                }
            }

            void BuildSkinDoor(Vector3 localPosition, Vector3 localRotation, ulong skin)
            {
                Door door = BuildManager.CreateStaticEntityInLocalCoordinates("assets/prefabs/building/wall.frame.garagedoor/wall.frame.garagedoor.prefab", lootContainer.transform, localPosition, localRotation, skin) as Door;
                door.OwnerID = 84726572;
                doors.Add(door);
            }

            internal void OnPlayerOpenContainerDoor(BasePlayer player, Door door)
            {
                door.CloseRequest();
                if (ins.plugins.Exists("PveMode") && ins._config.supportedPluginsConfig.pveMode.pve && ins.PveMode.Call("CanActionEvent", ins.Name, player) != null) return;
                StartLooting(player);
            }

            void StartLooting(BasePlayer player)
            {
                lootContainer.SetFlag(BaseEntity.Flags.Open, true, false);
                player.inventory.loot.StartLootingEntity(lootContainer, false);
                player.inventory.loot.AddContainer(lootContainer.inventory);
                player.inventory.loot.SendImmediate();
                player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", lootContainer.panelName);
                lootContainer.DecayTouch();
                lootContainer.SendNetworkUpdate();
            }

            void OnDestroy()
            {
                foreach (Door door in doors)
                {
                    if (door.IsExists())
                        door.Kill();
                }
            }

            class DoorLocationData
            {
                internal Vector3 position;
                internal Vector3 rotation;

                internal int doorType;
            }

            class DoorSkinData
            {
                internal string name;
                internal ulong regularSkin;
                internal ulong logoSkin;
                internal ulong doorSkin;
            }
        }

        sealed class CardDoor
        {
            static HashSet<CardDoor> cardDoors = new HashSet<CardDoor>();

            HashSet<Door> doors = new HashSet<Door>();
            HashSet<CardReader> cardReaders = new HashSet<CardReader>();

            internal CardDoor(DataCardDoorConfig dataCardDoorConfig, int doorType)
            {
                foreach (LocationConfig locationConfig in dataCardDoorConfig.doorLocations)
                    CreateDoor(dataCardDoorConfig.doorPrefab, locationConfig, dataCardDoorConfig.doorSkin, doorType);

                if (doorType == 0)
                    foreach (LocationConfig locationConfig in dataCardDoorConfig.cardReaderLocations)
                        CreateCardReader(locationConfig, dataCardDoorConfig.cardType);

                cardDoors.Add(this);
            }

            internal static CardDoor GetCardDoorByCardReaderNetId(ulong netID)
            {
                return cardDoors.FirstOrDefault(x => x != null && x.cardReaders.Any(y => y != null && y.net.ID.Value == netID));
            }

            internal static CardDoor GetCardDoorByDoorNetId(ulong netID)
            {
                return cardDoors.FirstOrDefault(x => x != null && x.doors.Any(y => y != null && y.net.ID.Value == netID));
            }

            internal static void KillAllCardDoors()
            {
                foreach (CardDoor cardDoor in cardDoors)
                {
                    if (cardDoor != null)
                        cardDoor.KillCardDoor();
                }
            }

            void CreateDoor(string prefab, LocationConfig locationConfig, ulong skin, int doorType)
            {
                Vector3 globalPosition = PositionDefiner.GetGlobalPosition(ins.eventController.transform, locationConfig.position.ToVector3());
                Quaternion globalRotation = PositionDefiner.GetGlobalRotation(ins.eventController.transform, locationConfig.rotation.ToVector3());

                Door door = BuildManager.CreateStaticEntity(prefab, globalPosition, globalRotation, skin) as Door;

                if (doorType != 2)
                    door.canHandOpen = false;

                doors.Add(door);
            }

            void CreateCardReader(LocationConfig locationConfig, int accesLevel)
            {
                if (locationConfig.position.Contains("s"))
                    locationConfig.position = locationConfig.position.Replace("s", "");

                Vector3 globalPosition = PositionDefiner.GetGlobalPosition(ins.eventController.transform, locationConfig.position.ToVector3());
                Quaternion globalRotation = PositionDefiner.GetGlobalRotation(ins.eventController.transform, locationConfig.rotation.ToVector3());

                CardReader cardReader = BuildManager.CreateStaticEntity("assets/prefabs/io/electric/switches/cardreader.prefab", globalPosition, globalRotation) as CardReader;
                cardReader.SetFlag(cardReader.AccessLevel1, accesLevel == 0 ? true : false);
                cardReader.SetFlag(cardReader.AccessLevel2, accesLevel == 1 ? true : false);
                cardReader.SetFlag(cardReader.AccessLevel3, accesLevel == 2 ? true : false);


                cardReader.accessLevel = accesLevel + 1;
                cardReader.SendNetworkUpdate();

                ins.NextTick(() =>
                {
                    cardReader.UpdateFromInput(100, 0);
                    cardReader.SendNetworkUpdate();
                });

                cardReaders.Add(cardReader);
            }

            internal void OnCardSwipe(Keycard card, BasePlayer player, CardReader cardReader)
            {
                Effect.server.Run(cardReader.swipeEffect.resourcePath, cardReader.audioPosition.position, Vector3.up, player.net.connection, false);

                if (ins.plugins.Exists("PveMode") && ins.PveMode.Call("CanActionEvent", ins.Name, player) != null)
                {
                    DeniedAcces(cardReader);
                    return;
                }
                else if (card.skinID == 0 && cardReader.accessLevel != card.accessLevel)
                {
                    DeniedAcces(cardReader);
                    return;
                }
                else if (card.skinID != 0 && (!ins._config.supportedPluginsConfig.superCardConfig.enable || ins._config.supportedPluginsConfig.superCardConfig.skin != card.skinID))
                {
                    DeniedAcces(cardReader);
                    return;
                }

                EconomyManager.ActionEconomy(player.userID, cardReader.accessLevel == 1 ? "GreenCard" : cardReader.accessLevel == 2 ? "BlueCard" : "RedCard");

                Item cardItem = card.GetItem();

                if (card.skinID == 0)
                    cardItem.LoseCondition(1);

                OpenDoors();
                Effect.server.Run(cardReader.accessGrantedEffect.resourcePath, cardReader.audioPosition.position, Vector3.up);
            }

            void DeniedAcces(CardReader cardReader)
            {
                Effect.server.Run(cardReader.accessDeniedEffect.resourcePath, cardReader.audioPosition.position, Vector3.up);
                cardReader.CancelInvoke(cardReader.GrantCard);
            }

            void OpenDoors()
            {
                foreach (Door door in doors)
                    if (door != null)
                        door.SetOpen(true);
            }

            internal void KillCardDoor()
            {
                foreach (CardReader cardReader in cardReaders)
                    if (cardReader.IsExists())
                        cardReader.Kill();

                foreach (Door door in doors)
                    if (door.IsExists())
                        door.Kill();
            }
        }

        sealed class MovableDroppedItem : DroppedItem
        {
            internal static MovableDroppedItem CreateMovableDroppedItem(Vector3 position, Quaternion rotation, bool hide = true)
            {
                DroppedItem droppedItem = GameManager.server.CreateEntity("assets/prefabs/misc/burlap sack/generic_world.prefab", position, rotation) as DroppedItem;
                droppedItem.enableSaving = false;
                droppedItem.allowPickup = false;
                droppedItem.item = ItemManager.CreateByName("weapon.mod.muzzleboost");

                if (hide) droppedItem.SetFlag(BaseEntity.Flags.Disabled, true);

                MovableDroppedItem movableDroppedItem = droppedItem.gameObject.AddComponent<MovableDroppedItem>();
                BuildManager.CopySerializableFields(droppedItem, movableDroppedItem);
                droppedItem.StopAllCoroutines();
                UnityEngine.GameObject.DestroyImmediate(droppedItem, true);
                movableDroppedItem.Spawn();
                movableDroppedItem.CancelInvoke(movableDroppedItem.IdleDestroy);
                return movableDroppedItem;
            }

            public override float MaxVelocity()
            {
                return 100;
            }

            public override float GetDespawnDuration()
            {
                return float.MaxValue;
            }
        }

        sealed class MapMarker : FacepunchBehaviour
        {
            MapMarkerGenericRadius mapmarker;
            VendingMachineMapMarker vendingMarker;
            Coroutine updateCounter;

            float lastEventOvnerNameCheckTime = 0;
            string eventOwnerName = "";

            internal static MapMarker CreateMarker(Vector3 position)
            {
                GameObject gameObject = new GameObject();
                gameObject.layer = (int)Rust.Layer.Reserved1;

                MapMarker mapMarker = gameObject.AddComponent<MapMarker>();
                mapMarker.Init(position);
                return mapMarker;
            }

            void Init(Vector3 position)
            {
                CreateRadiusMarker(position);
                CreateVendingMarker(position);
                updateCounter = ServerMgr.Instance.StartCoroutine(MarkerUpdateCounter());
            }

            void CreateRadiusMarker(Vector3 position)
            {
                if (!ins._config.marker.useRingMarker)
                    return;

                mapmarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", position) as MapMarkerGenericRadius;
                mapmarker.enableSaving = false;
                mapmarker.Spawn();
                mapmarker.radius = ins._config.marker.radius;
                mapmarker.alpha = ins._config.marker.alpha;
                mapmarker.color1 = new Color(ins._config.marker.color1.r, ins._config.marker.color1.g, ins._config.marker.color1.b);
                mapmarker.color2 = new Color(ins._config.marker.color2.r, ins._config.marker.color2.g, ins._config.marker.color2.b);
            }

            void CreateVendingMarker(Vector3 position)
            {
                if (!ins._config.marker.useShopMarker)
                    return;

                vendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", position) as VendingMachineMapMarker;
                vendingMarker.Spawn();
                vendingMarker.markerShopName = $"{ins.eventController.eventConfig.displayName} ({NotifyManager.GetTimeMessage(null, ins.eventController.GetEventTime())})";
            }

            IEnumerator MarkerUpdateCounter()
            {
                while (true)
                {
                    if (ins._config.supportedPluginsConfig.pveMode.pve && ins._config.supportedPluginsConfig.pveMode.showEventOwnerNameOnMap && ins.plugins.Exists("PveMode"))
                    {
                        if (Time.realtimeSinceStartup - lastEventOvnerNameCheckTime >= 5)
                        {
                            eventOwnerName = ZoneController.GetEventOwnerPlayerName();
                            lastEventOvnerNameCheckTime = Time.realtimeSinceStartup;
                        }
                    }

                    string displayEventOwnerName = eventOwnerName != "" ? GetMessage("Marker_EventOwner", null, eventOwnerName) : "";


                    if (mapmarker.IsExists())
                    {
                        mapmarker.SendUpdate();
                        mapmarker.SendNetworkUpdate();
                    }

                    if (vendingMarker.IsExists())
                    {
                        vendingMarker.markerShopName = $"{ins.eventController.eventConfig.displayName} ({NotifyManager.GetTimeMessage(null, ins.eventController.GetEventTime())}) {displayEventOwnerName}";
                        vendingMarker.SendNetworkUpdate();
                        if (ins._config.supportedPluginsConfig.pveMode.pve) vendingMarker.SetFlag(BaseEntity.Flags.Busy, eventOwnerName == "");
                    }

                    yield return CoroutineEx.waitForSeconds(1f);
                }
            }

            internal void DeleteMarker()
            {
                Destroy(this.gameObject);
            }

            void OnDestroy()
            {
                if (updateCounter != null) ServerMgr.Instance.StopCoroutine(updateCounter);
                if (mapmarker.IsExists()) mapmarker.Kill();
                if (vendingMarker.IsExists()) vendingMarker.Kill();
            }
        }

        sealed class ZoneController : FacepunchBehaviour
        {
            static ZoneController zoneController;

            SphereCollider sphereCollider;
            HashSet<BaseEntity> spheres = new HashSet<BaseEntity>();

            HashSet<BasePlayer> playersInZone = new HashSet<BasePlayer>();

            internal static bool AnyPlayerInZone()
            {
                return zoneController != null && zoneController.playersInZone.Any(x => x.IsRealPlayer() && x.IsConnected && !x.IsSleeping());
            }

            internal static void CreateZone(Vector3 position)
            {
                if (zoneController != null)
                    UnityEngine.GameObject.Destroy(zoneController.gameObject);

                GameObject gameObject = new GameObject();
                gameObject.transform.position = position;
                gameObject.layer = (int)Rust.Layer.Reserved1;

                zoneController = gameObject.AddComponent<ZoneController>();
                zoneController.Init();
            }

            internal static void DeleteZone()
            {
                if (ins._config.supportedPluginsConfig.pveMode.pve && ins.plugins.Exists("PveMode"))
                {
                    ins.PveMode.Call("EventRemovePveMode", ins.Name, true);
                }
                if (zoneController != null)
                    Destroy(zoneController.gameObject);
            }

            internal static bool IsPlayerInZone(ulong userID)
            {
                return zoneController != null && zoneController.playersInZone.Any(x => x != null && x.userID == userID);
            }

            internal static string GetEventOwnerPlayerName()
            {
                ulong ownerId = (ulong)ins.PveMode.Call("GetEventOwner", ins.Name);
                if (ownerId != 0)
                {
                    BasePlayer player = BasePlayer.FindByID(ownerId);
                    if (player != null)
                        return player.displayName;
                }
                return "";
            }

            internal static bool IsPlayerNearTheEventByDistance(Vector3 playerPosition)
            {
                return zoneController != null && Vector3.Distance(playerPosition, zoneController.transform.position) < ins.eventController.eventConfig.radius;
            }

            internal static HashSet<BasePlayer> GetPlayersInZone()
            {
                return zoneController.playersInZone;
            }

            internal static void OnPlayerLeaveZone(BasePlayer player)
            {
                if (zoneController == null) return;
                zoneController.playersInZone.Remove(player);
                if (ins._config.notificationConfig.guiConfig.isCountdownGUI) GuiManager.DestroyAllGuiForPlayer(player);
            }

            void Init()
            {
                CreateTriggerSphere();

                if (ins._config.supportedPluginsConfig.pveMode.pve && ins.plugins.Exists("PveMode"))
                    CreatePveModeZone();
                else if (ins._config.eventZone.isDome)
                    CreateSphere();
            }

            void CreateTriggerSphere()
            {
                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = ins.eventController.eventConfig.radius;
            }

            void CreatePveModeZone()
            {
                JObject config = new JObject
                {
                    ["Damage"] = ins._config.supportedPluginsConfig.pveMode.damage,
                    ["ScaleDamage"] = new JArray { ins._config.supportedPluginsConfig.pveMode.scaleDamage.Select(x => new JObject { ["Type"] = x.Type, ["Scale"] = x.Scale }) },
                    ["LootCrate"] = ins._config.supportedPluginsConfig.pveMode.lootCrate,
                    ["HackCrate"] = ins._config.supportedPluginsConfig.pveMode.hackCrate,
                    ["LootNpc"] = ins._config.supportedPluginsConfig.pveMode.lootNpc,
                    ["DamageNpc"] = ins._config.supportedPluginsConfig.pveMode.damageNpc,
                    ["DamageTank"] = false,
                    ["DamageHelicopter"] = false,
                    ["TargetNpc"] = ins._config.supportedPluginsConfig.pveMode.targetNpc,
                    ["TargetTank"] = false,
                    ["TargetHelicopter"] = false,
                    ["CanEnter"] = ins._config.supportedPluginsConfig.pveMode.canEnter,
                    ["CanEnterCooldownPlayer"] = ins._config.supportedPluginsConfig.pveMode.canEnterCooldownPlayer,
                    ["TimeExitOwner"] = ins._config.supportedPluginsConfig.pveMode.timeExitOwner,
                    ["AlertTime"] = ins._config.supportedPluginsConfig.pveMode.alertTime,
                    ["RestoreUponDeath"] = false,
                    ["CooldownOwner"] = ins._config.supportedPluginsConfig.pveMode.cooldownOwner,
                    ["Darkening"] = ins._config.supportedPluginsConfig.pveMode.darkening
                };

                HashSet<ulong> npcs = new HashSet<ulong>();
                HashSet<ulong> bradleys = new HashSet<ulong>();
                HashSet<ulong> helicopters = new HashSet<ulong>();
                HashSet<ulong> crates = new HashSet<ulong>();

                foreach (PatrolNpc patrolNpc in PatrolNpc.patrolNpcs)
                {
                    if (patrolNpc == null) continue;

                    ulong netId = patrolNpc.GetNpcNetId();
                    if (netId != 0)
                        npcs.Add(netId);
                }

                foreach (LootContainer lootContainer in LootManager.crates)
                    if (lootContainer != null && lootContainer.net != null)
                        crates.Add(lootContainer.net.ID.Value);

                ins.PveMode.Call("EventAddPveMode", ins.Name, config, gameObject.transform.position, ins.eventController.eventConfig.radius, crates, npcs, bradleys, helicopters, new HashSet<ulong>(), null);
            }

            void CreateSphere()
            {
                for (int i = 0; i < ins._config.eventZone.darkening; i++)
                {
                    BaseEntity sphere = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", gameObject.transform.position);
                    SphereEntity entity = sphere.GetComponent<SphereEntity>();
                    entity.currentRadius = ins.eventController.eventConfig.radius * 2;
                    entity.lerpSpeed = 0f;
                    sphere.enableSaving = false;
                    sphere.Spawn();
                    spheres.Add(sphere);
                }
            }

            void OnTriggerEnter(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsRealPlayer())
                {
                    playersInZone.Add(player);
                    if (ins._config.eventZone.isCreateZonePVP) NotifyManager.SendMessageToPlayer(player, "EnterPVP", ins._config.prefix);
                }
            }

            void OnTriggerExit(Collider other)
            {
                BasePlayer player = other.GetComponentInParent<BasePlayer>();
                if (player.IsRealPlayer())
                {
                    OnPlayerLeaveZone(player);
                    if (ins._config.eventZone.isCreateZonePVP)
                    {
                        if (ins.plugins.Exists("DynamicPVP") && (bool)ins.DynamicPVP.Call("IsPlayerInPVPDelay", player.userID)) return;
                        NotifyManager.SendMessageToPlayer(player, "ExitPVP", ins._config.prefix);
                    }
                }
            }

            void MessageGUI(BasePlayer player, string text)
            {
                CuiHelper.DestroyUi(player, "TextMain");

                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = ins._config.notificationConfig.guiConfig.anchorMin, AnchorMax = ins._config.notificationConfig.guiConfig.anchorMax },
                    CursorEnabled = false,
                }, "Hud", "TextMain");

                container.Add(new CuiElement
                {
                    Parent = "TextMain",
                    Components =
                {
                    new CuiTextComponent() { Color = "1 1 1 1", FadeIn = 0f, Text = text, FontSize = 24, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                    new CuiOutlineComponent { Distance = "1 1", Color = "0 0 0 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
                });

                CuiHelper.AddUi(player, container);
            }

            void OnDestroy()
            {
                foreach (BaseEntity sphere in spheres)
                    if (sphere != null && !sphere.IsDestroyed)
                        sphere.Kill();
            }
        }

        static class LootManager
        {
            internal static HashSet<LootContainer> crates = new HashSet<LootContainer>();

            internal static bool IsEventCrate(ulong netID)
            {
                return crates.Any(x => x.IsExists() && x.net.ID.Value == netID);
            }

            internal static OwnLootTableConfig GetUpdatedLootTable(OwnLootTableConfig ownLootTable)
            {
                ownLootTable.items = ownLootTable.items.OrderBy(x => x.chance);
                if (ownLootTable.maxItemsAmount > ownLootTable.items.Where(x => x.chance > 1).Count) ownLootTable.maxItemsAmount = ownLootTable.items.Count;
                if (ownLootTable.minItemsAmount > ownLootTable.maxItemsAmount) ownLootTable.minItemsAmount = ownLootTable.maxItemsAmount;
                return ownLootTable;
            }

            internal static LootContainer CreateCrate(string prefab, Vector3 position, Quaternion rotation)
            {
                BaseEntity entity = prefab.Contains("codelockedhackablecrate") ? BuildManager.CreateStaticEntity(prefab, position, rotation) : BuildManager.CreateRegularEntity(prefab, position, rotation);
                LootContainer lootContainer = entity as LootContainer;

                if (lootContainer != null)
                {
                    CrateConfig crateConfig = ins._config.crates.FirstOrDefault(x => x.prefab == lootContainer.PrefabName);
                    if (crateConfig == null)
                    {
                        NotifyManager.PrintError(null, "PresetNotFound_Exeption", prefab);
                        return lootContainer;
                    }

                    HackableLockedCrate hackableLockedCrate = lootContainer as HackableLockedCrate;
                    if (hackableLockedCrate != null)
                    {
                        hackableLockedCrate.DestroyShared();
                        hackableLockedCrate.shouldDecay = false;
                        hackableLockedCrate.decayTimer = float.MaxValue;

                        if (crateConfig != null)
                        {
                            hackableLockedCrate.hackSeconds = HackableLockedCrate.requiredHackSeconds - crateConfig.crateUnlockTime;
                        }
                        hackableLockedCrate.SendNetworkUpdate();
                    }

                    ins.NextTick(() => UpdateLootContainer(crateConfig, lootContainer));
                    crates.Add(lootContainer);

                    return lootContainer;
                }

                return lootContainer;
            }

            internal static void TrySpawnItemInDefaultCrate(LootContainer lootContatiner, LootItemConfig itemConfig, float chance, int removeItemIndex = 0)
            {
                ins.NextTick(() =>
                {
                    if (UnityEngine.Random.Range(0f, 100f) <= chance)
                    {
                        Item item = LootManager.CreateItem(itemConfig, 1);
                        if (lootContatiner.inventory.itemList.Count > removeItemIndex)
                        {
                            Item removeItem = lootContatiner.inventory.itemList[removeItemIndex];
                            if (removeItem != null) lootContatiner.inventory.Remove(removeItem);
                        }
                        if (!item.MoveToContainer(lootContatiner.inventory)) item.Remove();
                    }
                });
            }

            static void UpdateLootContainer(CrateConfig crateConfig, LootContainer lootContainer)
            {
                if (lootContainer == null) return;
                UpdateLootContainerLootTable(lootContainer.inventory, crateConfig.lootTable, crateConfig.typeLootTable);
            }

            internal static void UpdateLootContainerLootTable(ItemContainer itemContainer, OwnLootTableConfig lootTableConfig, int typeOfLootTable)
            {
                if (typeOfLootTable != 1 && typeOfLootTable != 4) return;

                if (typeOfLootTable == 1)
                    itemContainer.ClearItemsContainer();

                int countLootInContainer = 0;
                int countLoot = UnityEngine.Random.Range(lootTableConfig.minItemsAmount, lootTableConfig.maxItemsAmount);
                if (countLoot > lootTableConfig.items.Where(x => x.chance > 0).Count)
                    countLoot = lootTableConfig.items.Count;

                if (typeOfLootTable == 4)
                    itemContainer.capacity += countLoot;
                else
                    itemContainer.capacity = countLoot;

                while (countLootInContainer < countLoot)
                {
                    HashSet<LootItemConfig> suitableItems = lootTableConfig.items.Where(y => !itemContainer.itemList.Any(x => x.info.shortname == y.shortname));
                    if (suitableItems == null || suitableItems.Count == 0) return;

                    foreach (LootItemConfig item in suitableItems)
                    {
                        if (UnityEngine.Random.Range(0.0f, 100.0f) <= item.chance)
                        {
                            int amount = UnityEngine.Random.Range(item.minAmount, item.maxAmount);
                            Item newItem = item.isBlueprint ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(item.shortname, amount, item.skin);
                            if (item.isBlueprint) newItem.blueprintTarget = ItemManager.FindItemDefinition(item.shortname).itemid;
                            if (item.name != "") newItem.name = item.name;
                            if (!newItem.MoveToContainer(itemContainer))
                            {
                                newItem.Remove();
                                return;
                            }
                            else
                            {
                                if (countLootInContainer >= countLoot) return;
                                countLootInContainer++;
                            }
                        }
                    }
                }
                itemContainer.capacity = itemContainer.itemList.Count;
            }

            internal static void DestroyEventCrates()
            {
                foreach (LootContainer lootContainer in crates)
                    if (lootContainer.IsExists())
                        lootContainer.Kill();
                crates.Clear();
            }

            internal static Item CreateItem(LootItemConfig itemConfig, int amount)
            {
                Item item = ItemManager.CreateByName(itemConfig.shortname, amount, itemConfig.skin);
                if (itemConfig.name != "") item.name = itemConfig.name;
                return item;
            }

            internal static void GiveItemToPLayer(BasePlayer player, Item item)
            {
                int spaceCountItem = PLayerInventory.GetSpaceCountItem(player, item.info.shortname, item.MaxStackable(), item.skin);
                int inventoryItemCount;
                if (spaceCountItem > item.amount) inventoryItemCount = item.amount;
                else inventoryItemCount = spaceCountItem;

                if (inventoryItemCount > 0)
                {
                    Item itemInventory = ItemManager.CreateByName(item.info.shortname, inventoryItemCount, item.skin);
                    if (item.skin != 0) itemInventory.name = item.name;

                    item.amount -= inventoryItemCount;
                    PLayerInventory.MoveInventoryItem(player, itemInventory);
                }

                if (item.amount > 0) PLayerInventory.DropExtraItem(player, item);
            }

            static class PLayerInventory
            {
                internal static int GetSpaceCountItem(BasePlayer player, string shortname, int stack, ulong skinID)
                {
                    int slots = player.inventory.containerMain.capacity + player.inventory.containerBelt.capacity;
                    int taken = player.inventory.containerMain.itemList.Count + player.inventory.containerBelt.itemList.Count;
                    int result = (slots - taken) * stack;
                    foreach (Item item in player.inventory.AllItems()) if (item.info.shortname == shortname && item.skin == skinID && item.amount < stack) result += stack - item.amount;
                    return result;
                }

                internal static void MoveInventoryItem(BasePlayer player, Item item)
                {
                    if (item.amount <= item.MaxStackable())
                    {
                        foreach (Item itemInv in player.inventory.AllItems())
                        {
                            if (itemInv.info.shortname == item.info.shortname && itemInv.skin == item.skin && itemInv.amount < itemInv.MaxStackable())
                            {
                                if (itemInv.amount + item.amount <= itemInv.MaxStackable())
                                {
                                    itemInv.amount += item.amount;
                                    itemInv.MarkDirty();
                                    return;
                                }
                                else
                                {
                                    item.amount -= itemInv.MaxStackable() - itemInv.amount;
                                    itemInv.amount = itemInv.MaxStackable();
                                }
                            }
                        }
                        if (item.amount > 0) player.inventory.GiveItem(item);
                    }
                    else
                    {
                        while (item.amount > item.MaxStackable())
                        {
                            Item thisItem = ItemManager.CreateByName(item.info.shortname, item.MaxStackable(), item.skin);
                            if (item.skin != 0) thisItem.name = item.name;
                            player.inventory.GiveItem(thisItem);
                            item.amount -= item.MaxStackable();
                        }
                        if (item.amount > 0) player.inventory.GiveItem(item);
                    }
                }

                internal static void DropExtraItem(BasePlayer player, Item item)
                {
                    if (item.amount <= item.MaxStackable()) item.Drop(player.transform.position, Vector3.up);
                    else
                    {
                        while (item.amount > item.MaxStackable())
                        {
                            Item thisItem = ItemManager.CreateByName(item.info.shortname, item.MaxStackable(), item.skin);
                            if (item.skin != 0) thisItem.name = item.name;
                            thisItem.Drop(player.transform.position, Vector3.up);
                            item.amount -= item.MaxStackable();
                        }
                        if (item.amount > 0) item.Drop(player.transform.position, Vector3.up);
                    }
                }
            }
        }

        static class PositionDefiner
        {
            internal static Vector3 GetLocalPosition(Transform parentTransform, Vector3 globalPosition)
            {
                return parentTransform.InverseTransformPoint(globalPosition);
            }

            internal static Vector3 GetGlobalPosition(Transform parentTransform, Vector3 position)
            {
                return parentTransform.transform.TransformPoint(position);
            }

            internal static Quaternion GetGlobalRotation(Transform parentTransform, Vector3 rotation)
            {
                return parentTransform.rotation * Quaternion.Euler(rotation);
            }

            internal static Vector3 DefineEventPosition()
            {
                Vector3 position = GetRandomMapPoint();
                return position;
            }

            static Vector3 GetRandomMapPoint()
            {
                int counter = 30;

                while (counter > 0)
                {
                    --counter;

                    float mapSize = TerrainMeta.Size.x / 2;

                    float random1 = UnityEngine.Random.Range(-mapSize - 300, mapSize + 300);
                    float random2 = Math.Abs(random1) > mapSize ? UnityEngine.Random.Range(-mapSize - 300, mapSize + 300) : UnityEngine.Random.Range(0, 2) == 1 ? UnityEngine.Random.Range(mapSize, mapSize + 300) : UnityEngine.Random.Range(-mapSize - 300, -mapSize);

                    Vector3 randomPosition = new Vector3(random1, 0, random2);
                    if (UnityEngine.Random.Range(0, 2) == 1)
                        randomPosition = new Vector3(random2, 0, random1);

                    randomPosition = GetGroundPositionInPoint(randomPosition);
                    if (CheckMapPoint(randomPosition))
                        return randomPosition;
                }
                return Vector3.zero;
            }

            internal static Vector3 GetGroundPositionInPoint(Vector3 position)
            {
                position.y = 100;
                RaycastHit raycastHit;
                Physics.Raycast(position, Vector3.down, out raycastHit, 500, 1 << 16 | 1 << 23);
                position.y = raycastHit.point.y;
                if (position.y == 0) return Vector3.zero;
                return position;
            }

            static bool CheckMapPoint(Vector3 position)
            {

                return true;
            }
        }

        static class BuildManager
        {
            internal static BaseEntity CreateRegularEntity(string prefabName, Vector3 position, Quaternion rotation, bool enableSaving = false)
            {
                BaseEntity entity = CreateEntity(prefabName, position, rotation, enableSaving);
                entity.Spawn();
                return entity;
            }

            internal static BaseEntity CreateStaticEntity(string prefabName, Vector3 position, Quaternion rotation, ulong skinID = 0, bool enableSaving = false)
            {
                BaseEntity entity = CreateEntity(prefabName, position, rotation, enableSaving);
                if (entity == null)
                    return null;

                DestroyUnnessesaryComponents(entity);
                entity.skinID = skinID;
                entity.Spawn();
                StabilityEntity stabilityEntity = entity as StabilityEntity;
                if (stabilityEntity != null) stabilityEntity.grounded = true;
                BaseCombatEntity baseCombatEntity = entity as BaseCombatEntity;
                if (baseCombatEntity != null) baseCombatEntity.pickup.enabled = false;
                return entity;
            }

            internal static BaseEntity CreateChildEntity(BaseEntity parrentEntity, string prefabName, Vector3 localPosition, Vector3 localRotation, ulong skinID = 0)
            {
                BaseEntity entity = CreateEntity(prefabName, parrentEntity.transform.position, Quaternion.identity);
                if (entity == null)
                    return null;

                DestroyUnnessesaryComponents(entity);
                entity.skinID = skinID;
                SetParent(parrentEntity, entity, localPosition, localRotation);
                entity.Spawn();
                PostSpawnEntityUpdate(entity);
                return entity;
            }

            internal static BaseEntity CreateRegularEntityInLocalCoordinates(DataFileEntity dataFileEntity, Transform parentTransform, bool enableSaving = false)
            {
                return CreateRegularEntityInLocalCoordinates(dataFileEntity.prefab, parentTransform, dataFileEntity.position.ToVector3(), dataFileEntity.rotation.ToVector3(), enableSaving);
            }

            internal static BaseEntity CreateRegularEntityInLocalCoordinates(string prefabName, Transform parentTransform, Vector3 localPosition, Vector3 localRotation, bool enableSaving = false)
            {
                Vector3 globalPosition = PositionDefiner.GetGlobalPosition(parentTransform, localPosition);
                Quaternion globalRotation = PositionDefiner.GetGlobalRotation(parentTransform, localRotation);

                BaseEntity entity = CreateEntity(prefabName, globalPosition, globalRotation, enableSaving);
                entity.Spawn();

                return entity;
            }

            internal static BaseEntity CreateStaticEntityInLocalCoordinates(DataFileEntity dataFileEntity, Transform parentTransform)
            {
                return CreateStaticEntityInLocalCoordinates(dataFileEntity.prefab, parentTransform, dataFileEntity.position.ToVector3(), dataFileEntity.rotation.ToVector3(), dataFileEntity.skin);
            }

            internal static BaseEntity CreateStaticEntityInLocalCoordinates(string prefabName, Transform parentTransform, Vector3 localPosition, Vector3 localRotation, ulong skinID = 0)
            {
                Vector3 globalPosition = PositionDefiner.GetGlobalPosition(parentTransform, localPosition);
                Quaternion globalRotation = PositionDefiner.GetGlobalRotation(parentTransform, localRotation);

                BaseEntity entity = CreateEntity(prefabName, globalPosition, globalRotation);
                if (entity == null) return null;

                entity.skinID = skinID;

                DestroyUnnessesaryComponents(entity);
                entity.Spawn();

                StabilityEntity stabilityEntity = entity as StabilityEntity;
                if (stabilityEntity != null) stabilityEntity.grounded = true;
                BaseCombatEntity baseCombatEntity = entity as BaseCombatEntity;
                if (baseCombatEntity != null) baseCombatEntity.pickup.enabled = false;
                return entity;
            }

            internal static BaseEntity CreateDecorEntityInLocalCoordinates(DataFileEntity dataFileEntity, Transform parentTransform)
            {
                return CreateDecorEntityInLocalCoordinates(dataFileEntity.prefab, parentTransform, dataFileEntity.position.ToVector3(), dataFileEntity.rotation.ToVector3(), dataFileEntity.skin);
            }

            internal static BaseEntity CreateDecorEntityInLocalCoordinates(string prefabName, Transform parentTransform, Vector3 localPosition, Vector3 localRotation, ulong skinID = 0)
            {
                Vector3 globalPosition = PositionDefiner.GetGlobalPosition(parentTransform, localPosition);
                Quaternion globalRotation = PositionDefiner.GetGlobalRotation(parentTransform, localRotation);

                return CreateDecorEntity(prefabName, globalPosition, globalRotation);
            }

            internal static BaseEntity CreateDecorEntity(string prefabName, Vector3 globalPosition, Quaternion globalRotation, ulong skinID = 0)
            {
                BaseEntity entity = CreateEntity(prefabName, globalPosition, globalRotation);

                entity.skinID = skinID;
                entity.StopAllCoroutines();
                BaseEntity newEntity = entity.gameObject.AddComponent<BaseEntity>();
                CopySerializableFields(entity, newEntity);
                UnityEngine.GameObject.DestroyImmediate(entity, true);
                entity = newEntity;

                DestroyUnnessesaryComponents(entity);
                DestroyEntityConponents<GenericSpawnPoint>(entity);
                DestroyEntityConponents<SpawnGroup>(entity);
                DestroyEntityConponents<MissionPoint>(entity);

                if (ins._config.mainConfig.disableRadiationFromRadioactivrBarrels && entity.name.Contains("underwaterlab_dwelling"))
                {
                    DestroyEntityConponents<SphereCollider>(entity);
                }

                entity.Spawn();
                return entity;
            }

            internal static BaseEntity CreateDataFileEntity(DataFileEntity dataFileEntity, Transform parentTransform)
            {
                BaseEntity entity;

                if (dataFileEntity.prefab.Contains("cargoship"))
                {
                    entity = CreateDecorCargo(dataFileEntity, parentTransform);
                }
                else if (dataFileEntity.prefab.Contains("submarine"))
                {
                    entity = CreateRegularEntityInLocalCoordinates(dataFileEntity, parentTransform, true);
                }
                else if (dataFileEntity.prefab.Contains("vendingmachine"))
                {
                    entity = CreateDecorEntityInLocalCoordinates(dataFileEntity, parentTransform);
                }
                else if (dataFileEntity.prefab.Contains("divesite"))
                {
                    entity = CreateDecorEntityInLocalCoordinates(dataFileEntity, parentTransform);
                    DestroyEntityConponents<NPCSpawner>(entity);
                    entity.gameObject.layer = 27;
                }
                else if (dataFileEntity.prefab.Contains("dwelling"))
                {
                    entity = CreateDecorEntityInLocalCoordinates(dataFileEntity, parentTransform);
                }
                else if (dataFileEntity.prefab.Contains("junkpile_water"))
                {
                    entity = CreateDecorEntityInLocalCoordinates(dataFileEntity, parentTransform);
                    entity.enabled = false;
                }
                else if (dataFileEntity.prefab.Contains("lamp.red"))
                {
                    entity = CreateRegularEntityInLocalCoordinates(dataFileEntity, parentTransform);
                    entity.enabled = false;
                }
                else
                    entity = CreateStaticEntityInLocalCoordinates(dataFileEntity, parentTransform);

                if (entity == null) return null;
                PostSpawnEntityUpdate(entity);
                return entity;
            }

            static void PostSpawnEntityUpdate(BaseEntity entity)
            {
                BuildingBlock buildingBlock = entity as BuildingBlock;
                if (buildingBlock != null)
                {
                    buildingBlock.playerCustomColourToApply = 12;
                    buildingBlock.ChangeGradeAndSkin(BuildingGrade.Enum.Metal, 10221);
                    UpdateDecorEntity(buildingBlock);
                    return;
                }

                if (entity is Recycler || entity is ResearchTable || entity is RepairBench || entity is MixingTable || entity is Workbench || entity is Barricade)
                    return;

                SimpleLight simpleLight = entity as SimpleLight;
                if (simpleLight != null)
                {
                    simpleLight.UpdateFromInput(100, 0);
                    simpleLight.SendNetworkUpdate();
                    return;
                }

                BaseSubmarine baseSubmarine = entity as BaseSubmarine;
                if (baseSubmarine != null)
                {
                    baseSubmarine.buoyancy.rigidBody = null;
                    baseSubmarine.GetFuelSystem().AddStartingFuel(100);
                }

                else
                {
                    entity.SetFlag(BaseEntity.Flags.Busy, true);
                    entity.SetFlag(BaseEntity.Flags.Locked, true);
                }
            }

            static BaseEntity CreateDecorCargo(DataFileEntity dataFileEntity, Transform parentTransform)
            {
                Vector3 globalPosition = PositionDefiner.GetGlobalPosition(parentTransform, dataFileEntity.position.ToVector3());
                Quaternion globalRotation = PositionDefiner.GetGlobalRotation(parentTransform, dataFileEntity.rotation.ToVector3());

                return CreateDecorCargo(globalPosition, globalRotation);
            }

            static BaseEntity CreateDecorCargo(Vector3 globalPosition, Quaternion globalRotation)
            {
                CargoShip cargoShip = CreateEntity("assets/content/vehicles/boats/cargoship/cargoshiptest.prefab", globalPosition, globalRotation) as CargoShip;
                cargoShip.layouts[0].SetActive(true);
                cargoShip.scientistSpawnPoints = new Transform[0];
                BaseEntity customCargoShip = cargoShip.gameObject.AddComponent<BaseEntity>();
                CopySerializableFields(cargoShip, customCargoShip);
                UnityEngine.GameObject.DestroyImmediate(cargoShip, true);
                customCargoShip.Spawn();

                DestroyEntityConponents<TriggerBase>(customCargoShip);
                DestroyEntityConponents<NPCSpawner>(customCargoShip);
                DestroyEntityConponents<TriggerParent>(customCargoShip);

                DestroyEntityConponents<EnvironmentVolume>(customCargoShip);
                DestroyEntityConponents<AtmosphereVolume>(customCargoShip);
                DestroyEntityConponents<AIInformationZone>(customCargoShip);

                return customCargoShip;
            }

            static void UpdateDecorEntity(BaseCombatEntity baseCombatEntity)
            {
                UpdateInactiveEntity(baseCombatEntity);
                baseCombatEntity.lifestate = BaseCombatEntity.LifeState.Dead;
            }

            static void UpdateInactiveEntity(BaseEntity entity)
            {
                entity.SetFlag(BaseEntity.Flags.Busy, true);
                entity.SetFlag(BaseEntity.Flags.Locked, true);
            }

            internal static BaseEntity CreateEntity(string prefabName, Vector3 position, Quaternion rotation, bool enableSaving = false)
            {
                BaseEntity entity = GameManager.server.CreateEntity(prefabName, position, rotation);
                entity.enableSaving = enableSaving;
                return entity;
            }

            internal static void SetParent(BaseEntity parrentEntity, BaseEntity childEntity, Vector3 localPosition, Vector3 localRotation)
            {
                childEntity.SetParent(parrentEntity, true, false);
                childEntity.transform.localPosition = localPosition;
                childEntity.transform.localEulerAngles = localRotation;
            }

            internal static void DestroyEntityConponent<T>(BaseEntity entity)
            {
                T component = entity.GetComponent<T>();
                if (component != null) UnityEngine.GameObject.DestroyImmediate(component as UnityEngine.Object);
            }

            internal static void DestroyEntityConponents<T>(BaseEntity entity)
            {
                T[] components = entity.gameObject.GetComponentsInChildren<T>();
                for (int i = 0; i < components.Length; i++)
                {
                    T component = components[i];
                    if (component != null) UnityEngine.GameObject.DestroyImmediate(component as UnityEngine.Object);
                }
            }

            static void DestroyUnnessesaryComponents(BaseEntity entity)
            {
                DestroyEntityConponent<Rigidbody>(entity);
                DestroyEntityConponent<GroundWatch>(entity);
                DestroyEntityConponent<DestroyOnGroundMissing>(entity);
            }

            internal static void CopySerializableFields<T>(T src, T dst)
            {
                FieldInfo[] srcFields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo field in srcFields)
                {
                    object value = field.GetValue(src);
                    field.SetValue(dst, value);
                }
            }
        }

        static class NpcSpawnManager
        {
            internal static bool CheckNPCSpawn()
            {
                if (!ins.plugins.Exists("NpcSpawn"))
                {
                    ins.PrintError("NpcSpawn plugin doesn`t exist! Please read the file ReadMe.txt. NPCs will not spawn!");
                    return false;
                }
                else return true;
            }

            internal static bool IsEventNpc(ScientistNPC scientistNPC)
            {
                return scientistNPC != null && scientistNPC.transform.position.y < 0 && ins._config.npcConfigs.Any(x => x.name == scientistNPC.displayName);
            }

            internal static ScientistNPC CreateScientistNpc(NpcConfig npcConfig, Vector3 position)
            {
                JObject baseNpcConfigObj = GetBaseNpcConfig(npcConfig);
                ScientistNPC scientistNPC = (ScientistNPC)ins.NpcSpawn.Call("SpawnNpc", position, baseNpcConfigObj);
                return scientistNPC;
            }

            internal static ScientistNPC CreateDriverNpc(SubmarineConfig submarineConfig, Vector3 position)
            {
                JObject baseNpcConfigObj = GetDriverNpcConfig(submarineConfig);
                ScientistNPC scientistNPC = (ScientistNPC)ins.NpcSpawn.Call("SpawnNpc", position, baseNpcConfigObj);
                return scientistNPC;
            }

            static JObject GetBaseNpcConfig(NpcConfig config)
            {
                return new JObject
                {
                    ["Name"] = config.name,
                    ["WearItems"] = new JArray
                    {
                        config.wearItems.Select(x => new JObject
                        {
                            ["ShortName"] = x.shortName,
                            ["SkinID"] = x.skinID
                        })
                    },
                    ["BeltItems"] = new JArray { config.beltItems.Select(x => new JObject { ["ShortName"] = x.shortName, ["Amount"] = x.amount, ["SkinID"] = x.skinID, ["Mods"] = new JArray { x.Mods.ToHashSet() }, ["Ammo"] = x.ammo }) },
                    ["Kit"] = config.kit,
                    ["Health"] = config.health,
                    ["RoamRange"] = 0,
                    ["ChaseRange"] = 0,
                    ["SenseRange"] = config.senseRange,
                    ["ListenRange"] = config.senseRange / 3,
                    ["AttackRangeMultiplier"] = config.attackRangeMultiplier,
                    ["VisionCone"] = config.visionCone,
                    ["DamageScale"] = config.damageScale,
                    ["TurretDamageScale"] = 1f,
                    ["AimConeScale"] = 1,
                    ["DisableRadio"] = config.disableRadio,
                    ["CanRunAwayWater"] = false,
                    ["CanSleep"] = false,
                    ["Speed"] = 0,
                    ["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = string.Empty,
                    ["MemoryDuration"] = config.memoryDuration,
                    ["States"] = new JArray { "IdleState", "CombatStationaryState" }
                };
            }

            static JObject GetDriverNpcConfig(SubmarineConfig submarineConfig)
            {
                return new JObject
                {
                    ["Name"] = "Diver",
                    ["WearItems"] = new JArray(),
                    ["BeltItems"] = new JArray(),
                    ["Kit"] = "",
                    ["Health"] = 100,
                    ["RoamRange"] = 0,
                    ["ChaseRange"] = 0,
                    ["SenseRange"] = submarineConfig.targetDetectionRange * 3,
                    ["ListenRange"] = submarineConfig.targetDetectionRange * 3,
                    ["AttackRangeMultiplier"] = 1,
                    ["VisionCone"] = submarineConfig.visionCone,
                    ["DamageScale"] = 1f,
                    ["TurretDamageScale"] = 1f,
                    ["AimConeScale"] = 1f,
                    ["DisableRadio"] = true,
                    ["CanRunAwayWater"] = false,
                    ["CanSleep"] = false,
                    ["Speed"] = 0,
                    ["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = string.Empty,
                    ["MemoryDuration"] = submarineConfig.memoryDuration,
                    ["States"] = new JArray { "IdleState", "CombatStationaryState" }
                };
            }
        }

        public class DataFileEntity
        {
            public string prefab;
            public string position;
            public string rotation;
            public ulong skin;

            public DataFileEntity(string prefab, string position, string rotation, ulong skin = 0)
            {
                this.prefab = prefab;
                this.position = position;
                this.rotation = rotation;
                this.skin = skin;
            }
        }

        static class GuiManager
        {
            public class ImageName { public string name; public string path; }

            static readonly HashSet<ImageName> images = new HashSet<ImageName>
            {
                new ImageName { name = "Gui_1", path = "Shipwreck/Images/Gui_1.png" }
            };

            static readonly HashSet<string> _failedImages = new HashSet<string>();

            static readonly Dictionary<string, string> _images = new Dictionary<string, string>();

            internal static void LoadImages()
            {
                ImageName image = images.FirstOrDefault(x => !_images.ContainsKey(x.name) && !_failedImages.Contains(x.name));
                if (image != null)
                {
                    ServerMgr.Instance.StartCoroutine(ProcessLoadImage(image));
                }
                else if (_failedImages.Count > 0) Interface.Oxide.UnloadPlugin(ins.Name);
            }

            static IEnumerator ProcessLoadImage(ImageName image)
            {
                string url = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + image.path;
                using (WWW www = new WWW(url))
                {
                    yield return www;
                    if (www.error != null)
                    {
                        _failedImages.Add(image.name);
                        ins.PrintError($"Image {image.name} was not found!");
                    }
                    else
                    {
                        Texture2D tex = www.texture;
                        _images.Add(image.name, FileStorage.server.Store(tex.EncodeToPNG(), FileStorage.Type.png, CommunityEntity.ServerInstance.net.ID).ToString());
                        UnityEngine.Object.DestroyImmediate(tex);
                    }
                    LoadImages();
                }
            }

            internal static void UpdateCountdownGui(int time)
            {
                if (time < 0) time = 0;
                if (!ins._config.notificationConfig.guiConfig.isCountdownGUI) return;
                HashSet<BasePlayer> playersInZone = ZoneController.GetPlayersInZone();

                foreach (BasePlayer player in playersInZone)
                {
                    if (player == null || player.transform.position.y > 0)
                    {
                        DestroyAllGuiForPlayer(player);
                        continue;
                    }
                    CountdownGUI(player, time);
                }
            }

            static void CountdownGUI(BasePlayer player, int time)
            {
                if (!ins._config.notificationConfig.guiConfig.isCountdownGUI) return;
                CuiHelper.DestroyUi(player, "SpaceCountdownGUI");

                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = ins._config.notificationConfig.guiConfig.anchorMin, AnchorMax = ins._config.notificationConfig.guiConfig.anchorMax },
                    CursorEnabled = false,
                }, "Hud", "SpaceCountdownGUI");

                container.Add(new CuiElement
                {
                    Name = "Image_Back",
                    Parent = "SpaceCountdownGUI",
                    Components =
                    {
                        new CuiRawImageComponent { Png = _images["Gui_1"] },
                        new CuiRectTransformComponent { AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5", OffsetMin = $"-154 -63", OffsetMax = $"154 63" }
                    }
                });

                container.Add(new CuiElement
                {
                    Parent = "Image_Back",
                    Components =
                    {
                        new CuiTextComponent() { Color = "1 1 1 1", FadeIn = 0f, Text = NotifyManager.GetTimeMessage(player.UserIDString, time), FontSize = 14, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                        new CuiOutlineComponent { Distance = "1 1", Color = "0 0 0 1" },
                        new CuiRectTransformComponent { AnchorMin = "0 0.275", AnchorMax = "1 1" }
                    }
                });

                CuiHelper.AddUi(player, container);
            }

            internal static void MessageGUI(BasePlayer player, string text)
            {
                CuiHelper.DestroyUi(player, "SpaceMessageGui");

                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = ins._config.notificationConfig.guiConfig.anchorMin, AnchorMax = ins._config.notificationConfig.guiConfig.anchorMax },
                    CursorEnabled = false,
                }, "Hud", "SpaceMessageGui");
                container.Add(new CuiElement
                {
                    Parent = "SpaceMessageGui",
                    Components =
                {
                    new CuiTextComponent() { Color = "1 1 1 1", FadeIn = 0f, Text = text, FontSize = 24, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                    new CuiOutlineComponent { Distance = "1 1", Color = "0 0 0 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
                });

                CuiHelper.AddUi(player, container);
            }

            internal static void DestroyGuiForAllPLayers()
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    DestroyAllGuiForPlayer(player);
                }
            }

            internal static void DestroyAllGuiForPlayer(BasePlayer player)
            {
                DestroyMessageGUIForPlayer(player);
                DestroyCountdownGui(player);
            }

            internal static void DestroyMessageGUIForPlayer(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, "SpaceMessageGui");
            }

            internal static void DestroyCountdownGui(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, "SpaceCountdownGUI");
            }
        }

        static class NotifyManager
        {
            internal static void PrintError(BasePlayer player, string langKey, params object[] args)
            {
                if (player == null)
                    ins.PrintError(ClearColorAndSize(GetMessage(langKey, null, args)));
                else
                    ins.PrintToChat(player, GetMessage(langKey, player.UserIDString, args));
            }

            internal static void PrintLogMessage(string langKey, params object[] args)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] is int) args[i] = GetTimeMessage(null, (int)args[i]);
                }

                ins.Puts(ClearColorAndSize(GetMessage(langKey, null, args)));
            }

            internal static string ClearColorAndSize(string message)
            {
                message = message.Replace("</color>", string.Empty);
                message = message.Replace("</size>", string.Empty);
                while (message.Contains("<color="))
                {
                    int index = message.IndexOf("<color=");
                    message = message.Remove(index, message.IndexOf(">", index) - index + 1);
                }
                while (message.Contains("<size="))
                {
                    int index = message.IndexOf("<size=");
                    message = message.Remove(index, message.IndexOf(">", index) - index + 1);
                }
                return message;
            }

            internal static void SendMessageToAll(string langKey, params object[] args)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList) if (player != null) SendMessageToPlayer(player, langKey, args);
                SendDiscordMessage(langKey, args);
            }

            internal static void SendMessageToPlayer(BasePlayer player, string langKey, params object[] args)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] is int) args[i] = GetTimeMessage(player.UserIDString, (int)args[i]);
                }

                if (ins._config.notificationConfig.chat) ins.PrintToChat(player, GetMessage(langKey, player.UserIDString, args));
                if (ins._config.supportedPluginsConfig.GUIAnnouncements.enable) ins.GUIAnnouncements?.Call("CreateAnnouncement", ClearColorAndSize(GetMessage(langKey, player.UserIDString, args)), ins._config.supportedPluginsConfig.GUIAnnouncements.bannerColor, ins._config.supportedPluginsConfig.GUIAnnouncements.textColor, player, ins._config.supportedPluginsConfig.GUIAnnouncements.apiAdjustVPosition);
                if (ins._config.supportedPluginsConfig.notify.enable) player.SendConsoleCommand($"notify.show {ins._config.supportedPluginsConfig.notify.type} {ClearColorAndSize(GetMessage(langKey, player.UserIDString, args))}");
            }

            static void SendDiscordMessage(string langKey, params object[] args)
            {
                if (CanSendDiscordMessage() && ins._config.supportedPluginsConfig.discord.keys.Contains(langKey))
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i] is int) args[i] = GetTimeMessage(null, (int)args[i]);
                    }

                    object fields = new[] { new { name = ins.Title, value = ClearColorAndSize(GetMessage(langKey, null, args)), inline = false } };
                    ins.DiscordMessages?.Call("API_SendFancyMessage", ins._config.supportedPluginsConfig.discord.webhookUrl, "", ins._config.supportedPluginsConfig.discord.embedColor, JsonConvert.SerializeObject(fields), null, ins);
                }
            }

            static bool CanSendDiscordMessage() => ins._config.supportedPluginsConfig.discord.enable && !string.IsNullOrEmpty(ins._config.supportedPluginsConfig.discord.webhookUrl) && ins._config.supportedPluginsConfig.discord.webhookUrl != "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks";

            internal static string GetTimeMessage(string userIDString, int seconds)
            {
                string message = "";

                TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                if (timeSpan.Hours > 0) message += $" {timeSpan.Hours} {GetMessage("Hours", userIDString)}";
                if (timeSpan.Minutes > 0) message += $" {timeSpan.Minutes} {GetMessage("Minutes", userIDString)}";
                if (message == "") message += $" {timeSpan.Seconds} {GetMessage("Seconds", userIDString)}";

                return message;
            }
        }

        static class EconomyManager
        {
            static readonly Dictionary<ulong, double> playersBalance = new Dictionary<ulong, double>();

            internal static void ActionEconomy(ulong playerId, string type, string arg = "")
            {
                switch (type)
                {
                    case "Crates":
                        double economyCrateData;
                        if (ins._config.supportedPluginsConfig.economic.crates.TryGetValue(arg, out economyCrateData))
                            AddBalance(playerId, economyCrateData);
                        break;
                    case "Npc":
                        AddBalance(playerId, ins._config.supportedPluginsConfig.economic.npcPoint);
                        break;
                    case "Shark":
                        AddBalance(playerId, ins._config.supportedPluginsConfig.economic.sharkPoint);
                        break;
                    case "Submarine":
                        AddBalance(playerId, ins._config.supportedPluginsConfig.economic.submarinePoint);
                        break;
                    case "LockedCrate":
                        AddBalance(playerId, ins._config.supportedPluginsConfig.economic.lockedCratePoint);
                        break;
                    case "Door":
                        AddBalance(playerId, ins._config.supportedPluginsConfig.economic.doorPoint);
                        break;
                    case "RedCard":
                        AddBalance(playerId, ins._config.supportedPluginsConfig.economic.redCardPoint);
                        break;
                    case "BlueCard":
                        AddBalance(playerId, ins._config.supportedPluginsConfig.economic.blueCardPoint);
                        break;
                    case "GreenCard":
                        AddBalance(playerId, ins._config.supportedPluginsConfig.economic.greenCardPoint);
                        break;
                }
            }

            static void AddBalance(ulong playerId, double balance)
            {
                if (balance == 0) return;
                if (playersBalance.ContainsKey(playerId)) playersBalance[playerId] += balance;
                else playersBalance.Add(playerId, balance);
            }

            internal static void OnEventEnd()
            {
                if (!ins._config.supportedPluginsConfig.economic.enable || playersBalance.Count == 0)
                {
                    playersBalance.Clear();
                    return;
                }
                SendBalanceToPlayers();
                DefineEventWinner();

                playersBalance.Clear();
            }

            static void SendBalanceToPlayers()
            {
                foreach (KeyValuePair<ulong, double> pair in playersBalance)
                    SendBalanceToPlayer(pair.Key, pair.Value);
            }

            static void SendBalanceToPlayer(ulong userID, double amount)
            {
                if (amount < ins._config.supportedPluginsConfig.economic.minEconomyPiont) return;
                int intAmount = Convert.ToInt32(amount);
                if (intAmount <= 0) return;

                if (ins._config.supportedPluginsConfig.economic.plugins.Contains("Economics") && ins.plugins.Exists("Economics"))
                    ins.Economics.Call("Deposit", userID.ToString(), amount);
                if (ins._config.supportedPluginsConfig.economic.plugins.Contains("Server Rewards") && ins.plugins.Exists("ServerRewards"))
                    ins.ServerRewards.Call("AddPoints", userID, intAmount);
                if (ins._config.supportedPluginsConfig.economic.plugins.Contains("IQEconomic") && ins.plugins.Exists("IQEconomic"))
                    ins.IQEconomic.Call("API_SET_BALANCE", userID, intAmount);

                BasePlayer player = BasePlayer.FindByID(userID);
                if (player != null) NotifyManager.SendMessageToPlayer(player, "SendEconomy", ins._config.prefix, amount);
            }

            static void DefineEventWinner()
            {
                var winnerPair = playersBalance.Max(x => (float)x.Value);

                if (winnerPair.Value > 0)
                    Interface.CallHook("OnShipwreckEventWin", winnerPair.Key);

                if (winnerPair.Value >= ins._config.supportedPluginsConfig.economic.minCommandPoint)
                    foreach (string command in ins._config.supportedPluginsConfig.economic.commands)
                        ins.Server.Command(command.Replace("{steamid}", $"{winnerPair.Key}"));
            }
        }

        #endregion Classes 

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["EventActive_Exeption"] = "Ивент в данный момент активен, сначала завершите текущий ивент (<color=#ce3f27>/shipwreckstop</color>)!",
                ["EventNoActive_Exeption"] = "{0} Ивент в данный момент <color=#ce3f27>не активен</color>!",
                ["ConfigurationNotFound_Exeption"] = "<color=#ce3f27>Не удалось</color> найти конфигурацию ивента!",
                ["LocationNotFound_Exeption"] = "<color=#ce3f27>Не удалось</color> найти место проведения ивента!",
                ["PresetNotFound_Exeption"] = "Пресет {0} не найден в конфиге!",
                ["FileNotFound_Exeption"] = "Файл {0} не найден в папке {1}!",

                ["Animation_Stage_1"] = "{0} <color=#ce3f27>Грузовой корабль</color> обнаружен! Самолеты кобальта прибудут через <color=#ce3f27>{1}</color>",
                ["Animation_Stage_2"] = "{0} <color=#ce3f27>F15</color> отправились для уничтожения корабля! Будьте осторожны, рядом могут находиться солдаты <color=#ce3f27>Cobalt</color>",

                ["PreStart"] = "{0} Осталось <color=#43598d>{1}</color> до начала ивента!",
                ["EventStart"] = "{0} <color=#738d43>{1}</color> был обнаружен в открытом океане",
                ["RemainTime"] = "{0} <color=#738d43>{1}</color> будет уничтожен через <color=#ce3f27>{2}</color>!",
                ["Finish"] = "{0} Ивент <color=#ce3f27>окончен</color>!",

                ["EnterPVP"] = "{0} Вы <color=#ce3f27>вошли</color> в PVP зону, теперь другие игроки <color=#ce3f27>могут</color> наносить вам урон!",
                ["ExitPVP"] = "{0} Вы <color=#738d43>вышли</color> из PVP зоны, теперь другие игроки <color=#738d43>не могут</color> наносить вам урон!",

                ["PathRecordStart"] = "{0} Двигайтесь по маршруту, а затем введите команду <color=#738d43>/ssavepath <PresetName></color>",
                ["PathSaved"] = "{0} Маршрут <color=#738d43>сохранен</color>!",
                ["PathSaveFailed"] = "{0} Маршрут <color=#ce3f27>не сохранен</color>!",
                ["PathCancelled"] = "{0} Route <color=#ce3f27>cancelled</color>!",

                ["Hours"] = "ч.",
                ["Minutes"] = "м.",
                ["Seconds"] = "с.",

                ["SendEconomy"] = "{0} Вы <color=#738d43>получили</color> <color=#55aaff>{1}</color> баллов в экономику за прохождение ивента",

                ["EventStart_Log"] = "The event has begun! (Preset name - {0})",
                ["EventStop_Log"] = "The event is over!",
            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["EventActive_Exeption"] = "This event is active now. Finish the current event! (<color=#ce3f27>/shipwreckstop</color>)!",
                ["EventNoActive_Exeption"] = "{0} The event is <color=#ce3f27>offline</color>!",
                ["ConfigurationNotFound_Exeption"] = "The event configuration <color=#ce3f27>could not</color> be found!",
                ["LocationNotFound_Exeption"] = "<color=#ce3f27>Couldn't</color> find a place for the event!",
                ["PresetNotFound_Exeption"] = "{0} preset was not found in the config!",
                ["FileNotFound_Exeption"] = "File {0} not found in {1} folder",

                ["Animation_Stage_1"] = "{0} The <color=#ce3f27>Cargo Ship</color> has been pinged by enemy RADAR! The <color=#ce3f27>Cobalt Group</color> will launch fighter jets in <color=#ce3f27>{1}</color>!",
                ["Animation_Stage_2"] = "{0} The <color=#ce3f27>Cobalt Group</color> has launched fighter jets to attack the <color=#ce3f27>Cargo Ship</color>, and likely have their с soldiers nearby to raid the ship!",

                ["PreStart"] = "{0} The event will start in <color=#738d43>{1}</color>",
                ["EventStart"] = "{0} <color=#738d43>{1}</color> discovered at sea!",
                ["RemainTime"] = "{0} <color=#738d43>{1}</color> will be destroyed in <color=#ce3f27>{2}</color>!",
                ["Finish"] = "{0} The event is <color=#ce3f27>over</color>!",

                ["Marker_EventOwner"] = "Event Owner: {0}",

                ["EnterPVP"] = "{0} You <color=#ce3f27>have entered</color> the PVP zone, now other players <color=#ce3f27>can damage</color> you!",
                ["ExitPVP"] = "{0} You <color=#738d43>have gone out</color> the PVP zone, now other players <color=#738d43>can’t damage</color> you!",

                ["PathRecordStart"] = "{0} Follow the route and then enter <color=#738d43>/ssavepath</color> <PresetName>",
                ["PathSaved"] = "{0} The route is <color=#738d43>saved</color>!",
                ["PathSaveFailed"] = "{0} <color=#ce3f27>Failed</color> to save route!",
                ["PathCancelled"] = "{0} Route <color=#ce3f27>cancelled</color>!",

                ["SendEconomy"] = "{0} You <color=#738d43>have earned</color> <color=#55aaff>{1}</color> points in economics for participating in the event",

                ["Hours"] = "h.",
                ["Minutes"] = "m.",
                ["Seconds"] = "s.",


                ["EventStart_Log"] = "The event has begun! (Preset name - {0})",
                ["EventStop_Log"] = "The event is over!",
            }, this);
        }

        internal static string GetMessage(string langKey, string userID) => ins.lang.GetMessage(langKey, ins, userID);

        internal static string GetMessage(string langKey, string userID, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userID) : string.Format(GetMessage(langKey, userID), args);
        #endregion Lang

        #region Config
        private PluginConfig _config;

        protected override void LoadDefaultConfig()
        {
            _config = PluginConfig.DefaultConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
            Config.WriteObject(_config, true);
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #region StationConfig
        public class MonumentConfig
        {
            [JsonProperty(en ? "Setting up ship containers" : "Настройка корабельных контейнеров")] public ShipContainerConfigs shipContainerConfigs { get; set; }
            [JsonProperty(en ? "Setting up resources" : "Настройка ресурсов")] public ResourseConfigs resoursesConfigs { get; set; }
            [JsonProperty(en ? "Crate locations" : "Расположение ящиков")] public HashSet<DataEntityConfig> crates { get; set; }
            [JsonProperty(en ? "Settings for Card Doors" : "Настройка дверей с карточками")] public HashSet<DataCardDoorConfig> cardReaderConfigs { get; set; }
            [JsonProperty(en ? "Setting up doors for a raid" : "Настройка дверей для рейда")] public HashSet<DataSkinnedEntityConfig> raidDoorsConfigs { get; set; }

            public static MonumentConfig GetDefaultConfig()
            {
                return new MonumentConfig
                {
                    shipContainerConfigs = new ShipContainerConfigs
                    {
                        probabilities = new HashSet<ProbabilityConfig>
                        {
                            new ProbabilityConfig
                            {
                                prefab = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab",
                                probaility = 10
                            },
                            new ProbabilityConfig
                            {
                                prefab = "assets/bundled/prefabs/radtown/crate_normal.prefab",
                                probaility = 25
                            },
                            new ProbabilityConfig
                            {
                                prefab = "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
                                probaility = 25
                            },
                            new ProbabilityConfig
                            {
                                prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_ammunition.prefab",
                                probaility = 25
                            }
                        },
                        locations = new HashSet<LocationConfig>
                        {

                        }
                    },
                    resoursesConfigs = new ResourseConfigs
                    {
                        probabilities = new HashSet<ProbabilityConfig>
                        {
                            new ProbabilityConfig
                            {
                                prefab = "assets/bundled/prefabs/autospawn/resource/ores/sulfur-ore.prefab",
                                probaility = 25
                            },
                            new ProbabilityConfig
                            {
                                prefab = "assets/bundled/prefabs/autospawn/resource/ores/metal-ore.prefab",
                                probaility = 40
                            },
                            new ProbabilityConfig
                            {
                                prefab = "assets/bundled/prefabs/autospawn/resource/ores/stone-ore.prefab",
                                probaility = 35
                            }
                        },
                        locations = new HashSet<LocationConfig>
                        {

                        }
                    },
                    crates = new HashSet<DataEntityConfig>(),
                    cardReaderConfigs = new HashSet<DataCardDoorConfig>(),
                    raidDoorsConfigs = new HashSet<DataSkinnedEntityConfig>(),
                };
            }
        }

        public class ResourseConfigs
        {
            [JsonProperty(en ? "Setting the probability of spawning resources" : "Вероятность спавна камней")] public HashSet<ProbabilityConfig> probabilities { get; set; }
            [JsonProperty(en ? "Positions for spawn resources" : "Позиции для спавна ресурсов")] public HashSet<LocationConfig> locations { get; set; }
        }

        public class ShipContainerConfigs
        {
            [JsonProperty(en ? "Setting the probability of loot" : "Вероятность лута")] public HashSet<ProbabilityConfig> probabilities { get; set; }
            [JsonProperty(en ? "Positions for spawn containers" : "Позиции для спавна контейнеров")] public HashSet<LocationConfig> locations { get; set; }
        }

        public class ProbabilityConfig
        {
            [JsonProperty(en ? "Prefab" : "Префаб")] public string prefab { get; set; }
            [JsonProperty(en ? "Probability [0 - 100]" : "Вероятность [0 - 100]")] public float probaility { get; set; }
        }

        public class DataEntityConfig
        {
            [JsonProperty(en ? "Prefab" : "Префаб", Order = 0)] public string prefab { get; set; }
            [JsonProperty(en ? "Locations" : "Расположения", Order = 100)] public HashSet<LocationConfig> locations { get; set; }
        }

        public class DataSkinnedEntityConfig : DataEntityConfig
        {
            [JsonProperty("Skin", Order = 1)] public ulong skin { get; set; }
        }

        public class DataCardDoorConfig
        {
            [JsonProperty(en ? "Door prefab" : "Префаб двери")] public string doorPrefab { get; set; }
            [JsonProperty(en ? "Door skin" : "Скин двери")] public ulong doorSkin { get; set; }
            [JsonProperty(en ? "Card type (0 - green, 1 - blue, 2 - red)" : "Тип карточки (0 - зеленая, 1 - синяя, 2 - красная)")] public int cardType { get; set; }
            [JsonProperty(en ? "Door locations" : "Расположения дверей")] public HashSet<LocationConfig> doorLocations { get; set; }
            [JsonProperty(en ? "Card reader locations" : "Расположения считывателей карт")] public HashSet<LocationConfig> cardReaderLocations { get; set; }
        }
        #endregion StationConfig

        public class MainConfig
        {
            [JsonProperty(en ? "Enable automatic event holding [true/false]" : "Включить автоматическое проведение ивента [true/false]")] public bool isAutoEvent { get; set; }
            [JsonProperty(en ? "Minimum time between events [sec.]" : "Минимальное время между ивентами [sec.]")] public int minTimeBetweenEvents { get; set; }
            [JsonProperty(en ? "Maximum time between events [sec.]" : "Максимальное время между ивентами [sec.]")] public int maxTimeBetweenEvents { get; set; }
            [JsonProperty(en ? "The time between receiving the notification and the start of the event [sec.]" : "Время до начала ивента после сообщения в чате [sec.]")] public int preStartTime { get; set; }
            [JsonProperty(en ? "The time until the end of the event, when a message is displayed about the time until the end of the event [sec]" : "Время до конца ивента, когда выводится сообщение о сокром окончании ивента [sec]")] public HashSet<int> timeNotifications { get; set; }
            [JsonProperty(en ? "The event will not end if there are players nearby [true/false]" : "Ивент не будет заканчиваться, если рядом есть игроки [true/false]")] public bool dontStopEventIfPlayerInZone { get; set; }
            [JsonProperty(en ? "Disable the surfacing of corpses near the event? [true/false]" : "Отключить всплытие трупов вблизи ивента? [true/false]")] public bool disableFloatingCorpse { get; set; }
            [JsonProperty(en ? "C4 will be sticked to submarines guarding the event [true/false]" : "C4 будут прикрепляться на подводные лодки, охраняющие ивент? [true/false]")] public bool allowStickC4 { get; set; }
            [JsonProperty(en ? "Event's Submarine Damage Multiplier" : "Множитель урона от подводных лодок ивента")] public float submarineDamageScale { get; set; }
            [JsonProperty(en ? "Disable the radiation of radioactive barrels? [true/false]" : "Отключить радиацию от радиоактивных бочек? [true/false]")] public bool disableRadiationFromRadioactivrBarrels { get; set; }
            [JsonProperty(en ? "Enable logging of the start and end of the event? [true/false]" : "Включить логирование начала и окончания ивента? [true/false]")] public bool enableStartStopLogs { get; set; }
        }

        public class AnimationConfig
        {
            [JsonProperty(en ? "Time from message to departure F15 [sec.]" : "Время от сообщения до вылета самолета [sec.]")] public int timeBeforeF15TakeOff { get; set; }
            [JsonProperty(en ? "Add explosion effects to the ship while the ship is sinking? [true/false]" : "Добавить эффекты взрывов во время того, как корабль тонет? [true/false]")] public bool enableExplosionsEffects { get; set; }
            [JsonProperty(en ? "Locations of decorative objects on the ship" : "Расположения декоративных объектов на корабле")] public HashSet<DataSkinnedEntityConfig> decorEntities { get; set; }
        }

        public class EventConfig
        {
            [JsonProperty(en ? "Preset Name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty(en ? "Display name" : "Отображаемое название")] public string displayName { get; set; }
            [JsonProperty(en ? "Data file name" : "Название дата файла")] public string dataFileName { get; set; }
            [JsonProperty(en ? "Automatic startup" : "Автоматический запуск")] public bool automatickStart { get; set; }
            [JsonProperty(en ? "Probability of a preset [0.0-100.0]" : "Вероятность пресета [0.0-100.0]")] public float chance { get; set; }
            [JsonProperty(en ? "Duration of the event [sec.]" : "Длительность ивента [sec.]")] public int eventTime { get; set; }
            [JsonProperty(en ? "Event radius" : "Радиус ивента")] public float radius { get; set; }
            [JsonProperty(en ? "Setting up patrol routes for submarines/sharks/npcs" : "Настройка маршрутов патрулирования для подводных лодок/акул/npc")] public List<PatrolCustomConfig> customPatrolRoutes { get; set; }
            [JsonProperty(en ? "Type of doors on location (0 - spawn card readers, 1 - doors need to be raided, 2 - doors can be opened)" : "Тип дверей на локации (0 - спавнить считыватели карт, 1 - двери нужно рейдить, 2 - двери можно открыть)")] public int doorType { get; set; }
            [JsonProperty(en ? "Enable the effect of sinking the cargo ship [true/false]" : "Включить эффект потопления корабля? [true/false]")] public bool enableCargoSinkEffect { get; set; }
        }

        public class SharkConfig
        {
            [JsonProperty(en ? "Preset Name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty(en ? "Health" : "Здоровье")] public float health { get; set; }
            [JsonProperty(en ? "Damage Multiplier" : "Множитель урона")] public float damageScale { get; set; }
            [JsonProperty(en ? "Patrol speed" : "Скорость патрулирования")] public float minSpeed { get; set; }
            [JsonProperty(en ? "Сhase speed" : "Скорость погони")] public float maxSpeed { get; set; }
            [JsonProperty(en ? "Patrol turning speed" : "Скорость поворота при патрулировании")] public float minTurnSpeed { get; set; }
            [JsonProperty(en ? "Сhase turning speed" : "Скорость поворота при погоне")] public float maxTurnSpeed { get; set; }
            [JsonProperty(en ? "Time between attacks" : "Время между атаками")] public float attackCooldown { get; set; }
            [JsonProperty(en ? "Target search distance" : "Радиус поиска цели")] public float aggroRange { get; set; }
            [JsonProperty(en ? "Time to recover health (0 - disable recovery) [sec]" : "Время до восстановления здоровья (0 - отключить восстановление) [sec]")] public int healtRecoverTime { get; set; }
        }

        public class SubmarineConfig
        {
            [JsonProperty(en ? "Preset Name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty(en ? "Type of submarine (0 - solo, 1 - duo)" : "Тип подводной лодки (0 - solo, 1 - duo)")] public int submarineType { get; set; }
            [JsonProperty(en ? "Health" : "Здоровье")] public float health { get; set; }
            [JsonProperty(en ? "Speed" : "Скорость")] public float patrolSpeed { get; set; }
            [JsonProperty(en ? "Turning speed" : "Скорость поворота")] public float turnSpeed { get; set; }
            [JsonProperty(en ? "Target detection range" : "Дальность обнаружения цели")] public float targetDetectionRange { get; set; }
            [JsonProperty(en ? "Angle of view" : "Угол обзора")] public float visionCone { get; set; }
            [JsonProperty(en ? "Target loss range" : "Дальность потери цели")] public float targetLossRange { get; set; }
            [JsonProperty(en ? "Attack range" : "Дальность атаки")] public float attackRange { get; set; }
            [JsonProperty(en ? "Memory duration [sec.]" : "Длительность памяти [sec.]")] public float memoryDuration { get; set; }
            [JsonProperty(en ? "Number of torpedoes (0 - infinite)" : "Количество торпед (0 - бесконечно)")] public int torpedoCount { get; set; }
            [JsonProperty(en ? "Time between shots" : "Время между выстрелами")] public int timeBetweenShots { get; set; }
        }

        public class PatrolCustomConfig
        {
            [JsonProperty(en ? "Name of the submarine/shark/npc preset" : "Название пресета подводной лодки/акулы/npc", Order = 0)] public string presetName { get; set; }
            [JsonProperty(en ? "Enable [true/false]" : "Включить [true/false]", Order = 1)] public bool enable { get; set; }
            [JsonProperty(en ? "Ring route [true/false]" : "Кольцевой маршрут [true/false]", Order = 2)] public bool isRingRoute { get; set; }
            [JsonProperty(en ? "List of points" : "Позиции патрулирования", Order = 3)] public List<string> patrolPathList { get; set; }
        }

        public class CrateConfig
        {
            [JsonProperty("Prefab")] public string prefab { get; set; }
            [JsonProperty(en ? "Time to unlock the crates (LockedCrate) [sec.]" : "Время до открытия заблокированного ящика (LockedCrate) [sec.]")] public float crateUnlockTime { get; set; }
            [JsonProperty(en ? "Which loot table should the plugin use? (0 - default; 1 - own; 2 - AlphaLoot; 3 - CustomLoot; 4 - Add Items)" : "Какую таблицу лута необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - Добавить предметы)")] public int typeLootTable { get; set; }
            [JsonProperty(en ? "Loot table" : "Собственная лутовая таблицв")] public OwnLootTableConfig lootTable { get; set; }
        }

        public class OwnLootTableConfig
        {
            [JsonProperty(en ? "Minimum numbers of items" : "Минимальное кол-во элементов")] public int minItemsAmount { get; set; }
            [JsonProperty(en ? "Maximum numbers of items" : "Максимальное кол-во элементов")] public int maxItemsAmount { get; set; }
            [JsonProperty(en ? "List of items" : "Список предметов")] public List<LootItemConfig> items { get; set; }
        }

        public class LootItemConfig
        {
            [JsonProperty("ShortName")] public string shortname { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong skin { get; set; }
            [JsonProperty(en ? "Name (empty - default)" : "Название (empty - default)")] public string name { get; set; }
            [JsonProperty(en ? "Minimum" : "Минимальное кол-во")] public int minAmount { get; set; }
            [JsonProperty(en ? "Maximum" : "Максимальное кол-во")] public int maxAmount { get; set; }
            [JsonProperty(en ? "Chance [0.0-100.0]" : "Шанс выпадения предмета [0.0-100.0]")] public float chance { get; set; }
            [JsonProperty(en ? "Is this a blueprint? [true/false]" : "Это чертеж? [true/false]")] public bool isBlueprint { get; set; }
        }

        public class NpcConfig
        {
            [JsonProperty(en ? "Preset name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty(en ? "Name [Must be unique]" : "Название [Должно быть уникальным]")] public string name { get; set; }
            [JsonProperty(en ? "Health" : "Кол-во ХП")] public float health { get; set; }
            [JsonProperty(en ? "Should remove the corpse?" : "Удалять труп?")] public bool deleteCorpse { get; set; }
            [JsonProperty(en ? "Roam Range" : "Дальность патрулирования местности")] public float roamRange { get; set; }
            [JsonProperty(en ? "Attack Range Multiplier" : "Множитель радиуса атаки")] public float attackRangeMultiplier { get; set; }
            [JsonProperty(en ? "Sense Range" : "Радиус обнаружения цели")] public float senseRange { get; set; }
            [JsonProperty(en ? "Memory duration [sec.]" : "Длительность памяти цели [sec.]")] public float memoryDuration { get; set; }
            [JsonProperty(en ? "Scale damage" : "Множитель урона")] public float damageScale { get; set; }
            [JsonProperty(en ? "Detect the target only in the NPC's viewing vision cone?" : "Обнаруживать цель только в углу обзора NPC? [true/false]")] public bool checkVisionCone { get; set; }
            [JsonProperty(en ? "Vision Cone" : "Угол обзора")] public float visionCone { get; set; }
            [JsonProperty(en ? "Speed" : "Скорость")] public float speed { get; set; }
            [JsonProperty(en ? "Wear items" : "Одежда")] public List<NpcWear> wearItems { get; set; }
            [JsonProperty(en ? "Belt items" : "Быстрые слоты")] public List<NpcBelt> beltItems { get; set; }
            [JsonProperty(en ? "Kit" : "Kit")] public string kit { get; set; }
            [JsonProperty(en ? "Disable radio effects? [true/false]" : "Отключать эффекты рации? [true/false]")] public bool disableRadio { get; set; }
            [JsonProperty(en ? "Which loot table should the plugin use? (0 - default; 1 - own; 2 - AlphaLoot; 3 - CustomLoot; 4 - Add Items)" : "Какую таблицу лута необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - Добавить предметы)")] public int typeLootTable { get; set; }
            [JsonProperty(en ? "Loot table" : "Собственная лутовая таблицв")] public OwnLootTableConfig lootTable { get; set; }
        }

        public class NpcWear
        {
            [JsonProperty(en ? "ShortName" : "ShortName")] public string shortName { get; set; }
            [JsonProperty(en ? "skinID (0 - default)" : "SkinID (0 - default)")] public ulong skinID { get; set; }
        }

        public class NpcBelt
        {
            [JsonProperty(en ? "ShortName" : "ShortName")] public string shortName { get; set; }
            [JsonProperty(en ? "Amount" : "Кол-во")] public int amount { get; set; }
            [JsonProperty(en ? "skinID (0 - default)" : "SkinID (0 - default)")] public ulong skinID { get; set; }
            [JsonProperty(en ? "Mods" : "Модификации на оружие")] public List<string> Mods { get; set; }
            [JsonProperty(en ? "Ammo" : "Патроны")] public string ammo { get; set; }
        }

        public class LocationConfig
        {
            [JsonProperty(en ? "Position" : "Позиция")] public string position { get; set; }
            [JsonProperty(en ? "Rotation" : "Вращение")] public string rotation { get; set; }
        }

        public class MarkerConfig
        {
            [JsonProperty(en ? "Use a vending marker? [true/false]" : "Добавить маркер магазина? [true/false]")] public bool useShopMarker { get; set; }
            [JsonProperty(en ? "Use a circular marker? [true/false]" : "Добавить круговой маркер? [true/false]")] public bool useRingMarker { get; set; }
            [JsonProperty(en ? "Radius" : "Радиус")] public float radius { get; set; }
            [JsonProperty(en ? "Alpha" : "Прозрачность")] public float alpha { get; set; }
            [JsonProperty(en ? "Marker color" : "Цвет маркера")] public ColorConfig color1 { get; set; }
            [JsonProperty(en ? "Outline color" : "Цвет контура")] public ColorConfig color2 { get; set; }
        }

        public class ColorConfig
        {
            [JsonProperty("r")] public float r { get; set; }
            [JsonProperty("g")] public float g { get; set; }
            [JsonProperty("b")] public float b { get; set; }
        }

        public class ZoneConfig
        {
            [JsonProperty(en ? "Create a PVP zone? (only for those who use the TruePVE plugin)[true/false]" : "Создавать зону PVP в зоне проведения ивента? (только для тех, кто использует плагин TruePVE) [true/false]")] public bool isCreateZonePVP { get; set; }
            [JsonProperty(en ? "Use the dome? [true/false]" : "Использовать ли купол? [true/false]")] public bool isDome { get; set; }
            [JsonProperty(en ? "Darkening the dome" : "Затемнение купола")] public int darkening { get; set; }
            [JsonProperty(en ? "Prevent the actions of the RestoreUponDeath plugin in the event zone? [true/false]" : "Запрещать работу плагина RestoreUponDeath в зоне действия ивента? [true/false]")] public bool blockRestoreUponDeath { get; set; }

        }

        public class NotificationConfig
        {
            [JsonProperty(en ? "Use Chat Notifications? [true/false]" : "Использовать ли чат? [true/false]")] public bool chat { get; set; }
            [JsonProperty(en ? "GUI Setting" : "Настройки GUI")] public GUIConfig guiConfig { get; set; }
        }

        public class GUIConfig
        {
            [JsonProperty(en ? "Use the Countdown GUI? [true/false]" : "Использовать ли GUI обратного отсчета? [true/false]")] public bool isCountdownGUI { get; set; }
            [JsonProperty("AnchorMin")] public string anchorMin { get; set; }
            [JsonProperty("AnchorMax")] public string anchorMax { get; set; }
        }

        public class SupportedPluginsConfig
        {
            [JsonProperty(en ? "GUIAnnouncements Settings" : "Настройка GUI Announcements")] public GUIAnnouncementsConfig GUIAnnouncements { get; set; }
            [JsonProperty(en ? "Notify Settings" : "Настройка Notify")] public NotifyPluginConfig notify { get; set; }
            [JsonProperty(en ? "DiscordMessages Settings" : "Настройка DiscordMessages")] public DiscordConfig discord { get; set; }
            [JsonProperty(en ? "PVE Mode Setting" : "Настройка PVE Mode")] public PveModeConfig pveMode { get; set; }
            [JsonProperty(en ? "Economy Settings" : "Настройка экономики")] public EconomicsConfig economic { get; set; }

            [JsonProperty(en ? "SuperCard Settings" : "Настройка SuperCard")] public SuperCardConfig superCardConfig { get; set; }
        }

        public class GUIAnnouncementsConfig
        {
            [JsonProperty(en ? "Do you use the GUI Announcements? [true/false]" : "Использовать ли GUI Announcements? [true/false]")] public bool enable { get; set; }
            [JsonProperty(en ? "Banner color" : "Цвет баннера")] public string bannerColor { get; set; }
            [JsonProperty(en ? "Text color" : "Цвет текста")] public string textColor { get; set; }
            [JsonProperty(en ? "Adjust Vertical Position" : "Отступ от верхнего края")] public float apiAdjustVPosition { get; set; }
        }

        public class NotifyPluginConfig
        {
            [JsonProperty(en ? "Do you use the Notify? [true/false]" : "Использовать ли Notify? [true/false]")] public bool enable { get; set; }
            [JsonProperty(en ? "Type" : "Тип")] public string type { get; set; }
        }

        public class DiscordConfig
        {
            [JsonProperty(en ? "Do you use the Discord? [true/false]" : "Использовать ли Discord? [true/false]")] public bool enable { get; set; }
            [JsonProperty("Webhook URL")] public string webhookUrl { get; set; }
            [JsonProperty(en ? "Embed Color (DECIMAL)" : "Цвет полосы (DECIMAL)")] public int embedColor { get; set; }
            [JsonProperty(en ? "Keys of required messages" : "Ключи необходимых сообщений")] public HashSet<string> keys { get; set; }
        }

        public class PveModeConfig
        {
            [JsonProperty(en ? "Use the PVE mode of the plugin? [true/false]" : "Использовать PVE режим работы плагина? [true/false]")] public bool pve { get; set; }
            [JsonProperty(en ? "Display the name of the event owner on a marker on the map? [true/false]" : "Отображать имя владелца ивента на маркере на карте? [true/false]")] public bool showEventOwnerNameOnMap { get; set; }
            [JsonProperty(en ? "The amount of damage that the player has to do to become the Event Owner" : "Кол-во урона, которое должен нанести игрок, чтобы стать владельцем ивента")] public float damage { get; set; }
            [JsonProperty(en ? "Damage coefficients for calculate to become the Event Owner" : "Коэффициенты урона для подсчета, чтобы стать владельцем события")] public HashSet<ScaleDamageConfig> scaleDamage { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event loot the crates? [true/false]" : "Может ли не владелец ивента грабить ящики? [true/false]")] public bool lootCrate { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event hack locked crates? [true/false]" : "Может ли не владелец ивента взламывать заблокированные ящики? [true/false]")] public bool hackCrate { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event loot NPC corpses? [true/false]" : "Может ли не владелец ивента грабить трупы NPC? [true/false]")] public bool lootNpc { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event deal damage to the NPC? [true/false]" : "Может ли не владелец ивента наносить урон по NPC? [true/false]")] public bool damageNpc { get; set; }
            [JsonProperty(en ? "Can an Npc attack a non-owner of the event? [true/false]" : "Может ли Npc атаковать не владельца ивента? [true/false]")] public bool targetNpc { get; set; }
            [JsonProperty(en ? "Allow the non-owner of the event to enter the event zone? [true/false]" : "Разрешать входить внутрь зоны ивента не владельцу ивента? [true/false]")] public bool canEnter { get; set; }
            [JsonProperty(en ? "Allow a player who has an active cooldown of the Event Owner to enter the event zone? [true/false]" : "Разрешать входить внутрь зоны ивента игроку, у которого активен кулдаун на получение статуса владельца ивента? [true/false]")] public bool canEnterCooldownPlayer { get; set; }
            [JsonProperty(en ? "The time that the Event Owner may not be inside the event zone [sec.]" : "Время, которое владелец ивента может не находиться внутри зоны ивента [сек.]")] public int timeExitOwner { get; set; }
            [JsonProperty(en ? "The time until the end of Event Owner status when it is necessary to warn the player [sec.]" : "Время таймера до окончания действия статуса владельца ивента, когда необходимо предупредить игрока [сек.]")] public int alertTime { get; set; }
            [JsonProperty(en ? "The time that the player can`t become the Event Owner, after the end of the event and the player was its owner [sec.]" : "Время, которое игрок не сможет стать владельцем ивента, после того как ивент окончен и игрок был его владельцем [sec.]")] public double cooldownOwner { get; set; }
            [JsonProperty(en ? "Darkening the dome (0 - disables the dome)" : "Затемнение купола (0 - отключает купол)")] public int darkening { get; set; }
        }

        public class ScaleDamageConfig
        {
            [JsonProperty(en ? "Type of target" : "Тип цели")] public string Type { get; set; }
            [JsonProperty(en ? "Damage Multiplier" : "Множитель урона")] public float Scale { get; set; }
        }

        public class EconomicsConfig
        {
            [JsonProperty(en ? "Enable economy" : "Включить экономику?")] public bool enable { get; set; }
            [JsonProperty(en ? "Which economy plugins do you want to use? (Economics, Server Rewards, IQEconomic)" : "Какие плагины экономики вы хотите использовать? (Economics, Server Rewards, IQEconomic)")] public HashSet<string> plugins { get; set; }
            [JsonProperty(en ? "The minimum value that a player must collect to get points for the economy" : "Минимальное значение, которое игрок должен заработать, чтобы получить баллы за экономику")] public double minEconomyPiont { get; set; }
            [JsonProperty(en ? "The minimum value that a winner must collect to make the commands work" : "Минимальное значение, которое победитель должен заработать, чтобы сработали команды")] public double minCommandPoint { get; set; }
            [JsonProperty(en ? "Hacking a locked crate" : "Взлом заблокированного ящика")] public double lockedCratePoint { get; set; }
            [JsonProperty(en ? "Looting of crates" : "Ограбление ящиков")] public Dictionary<string, double> crates { get; set; }
            [JsonProperty(en ? "Killing an NPC" : "Убийство NPC")] public double npcPoint { get; set; }
            [JsonProperty(en ? "Killing an Door" : "Уничтожение Двери")] public double doorPoint { get; set; }
            [JsonProperty(en ? "Killing a shark" : "Убийство акулы")] public double sharkPoint { get; set; }
            [JsonProperty(en ? "Killing a submarine" : "Уничтожение подводной лодки")] public double submarinePoint { get; set; }
            [JsonProperty(en ? "Using the Red card" : "Использование красной карты")] public double redCardPoint { get; set; }
            [JsonProperty(en ? "Using the Blue card" : "Использование синей карты")] public double blueCardPoint { get; set; }
            [JsonProperty(en ? "Using the Green card" : "Использование зеленой карты")] public double greenCardPoint { get; set; }
            [JsonProperty(en ? "List of commands that are executed in the console at the end of the event ({steamid} - the player who collected the highest number of points)" : "Список команд, которые выполняются в консоли по окончанию ивента ({steamid} - игрок, который набрал наибольшее кол-во баллов)")] public HashSet<string> commands { get; set; }
        }

        public class SuperCardConfig
        {
            [JsonProperty(en ? "All event card readers will be opened using SuperCard? [true/false]" : "Все считыватели карт ивента будут открываться при помощи SuperCard ? [true/false]")] public bool enable { get; set; }
            [JsonProperty(en ? "Super card SkinID" : "Skin супер карты")] public ulong skin { get; set; }
        }

        private class PluginConfig
        {
            [JsonProperty(en ? "Version" : "Версия")] public VersionNumber version { get; set; }
            [JsonProperty(en ? "Chat Prefix" : "Префикс в чате")] public string prefix { get; set; }
            [JsonProperty(en ? "Main settings" : "Основные настройки")] public MainConfig mainConfig { get; set; }
            [JsonProperty(en ? "Event presets" : "Пресеты ивентов")] public HashSet<EventConfig> eventConfigs { get; set; }
            [JsonProperty(en ? "Shark presets" : "Пресеты акул")] public HashSet<SharkConfig> sharkConfigs { get; set; }
            [JsonProperty(en ? "Submarine presets" : "Пресеты подводных лодок")] public HashSet<SubmarineConfig> submarineConfigs { get; set; }
            [JsonProperty(en ? "Crates configurations" : "Настройка ящиков")] public HashSet<CrateConfig> crates { get; set; }
            [JsonProperty(en ? "NPC Settings" : "Настройка нпс")] public HashSet<NpcConfig> npcConfigs { get; set; }
            [JsonProperty(en ? "Setting up the animation of a sinking ship" : "Настройка анимации тонущего корабля")] public AnimationConfig animationConfig { get; set; }
            [JsonProperty(en ? "Marker Setting" : "Настройки маркера")] public MarkerConfig marker { get; set; }
            [JsonProperty(en ? "Event zone setting" : "Настройка зоны ивента")] public ZoneConfig eventZone { get; set; }
            [JsonProperty(en ? "Notification Settings" : "Настройкa уведомлений")] public NotificationConfig notificationConfig { get; set; }
            [JsonProperty(en ? "Supported Plugins" : "Поддерживаемые плагины")] public SupportedPluginsConfig supportedPluginsConfig { get; set; }

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    version = new VersionNumber(1, 1, 2),
                    prefix = "[Shipwreck]",
                    mainConfig = new MainConfig
                    {
                        isAutoEvent = false,
                        minTimeBetweenEvents = 3600,
                        maxTimeBetweenEvents = 7200,
                        preStartTime = 300,
                        timeNotifications = new HashSet<int>
                        {
                            300,
                            60,
                            30,
                            5
                        },
                        dontStopEventIfPlayerInZone = false,
                        disableFloatingCorpse = true,
                        allowStickC4 = true,
                        submarineDamageScale = 0.5f,
                        disableRadiationFromRadioactivrBarrels = true,
                        enableStartStopLogs = false,
                    },
                    eventConfigs = new HashSet<EventConfig>
                    {
                        new EventConfig
                        {
                            presetName = "cargo",
                            displayName = en ? "Shipwrecked cargo ship" : "Затонувший корабль",
                            dataFileName = "cargo",
                            automatickStart = true,
                            eventTime = 7200,
                            chance = 40,
                            radius = 90,

                            customPatrolRoutes = new List<PatrolCustomConfig>
                            {
                                new PatrolCustomConfig
                                {
                                    presetName = "submarine_solo_default",
                                    enable = true,
                                    isRingRoute = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(11.0, 24.0, -50.8)",
                                        "(10.7, 24.2, -45.7)",
                                        "(9.8, 24.2, -40.7)",
                                        "(6.5, 24.2, -36.7)",
                                        "(2.1, 24.2, -33.8)",
                                        "(-2.8, 24.2, -32.3)",
                                        "(-7.6, 24.1, -31.0)",
                                        "(-12.8, 24.1, -30.7)",
                                        "(-17.6, 23.8, -32.5)",
                                        "(-21.5, 23.8, -35.9)",
                                        "(-24.2, 23.8, -40.4)",
                                        "(-25.5, 23.8, -45.4)",
                                        "(-25.1, 23.8, -50.7)",
                                        "(-24.0, 23.8, -55.6)",
                                        "(-22.0, 23.8, -60.2)",
                                        "(-18.4, 23.8, -63.9)",
                                        "(-14.0, 23.8, -66.5)",
                                        "(-9.2, 23.8, -68.2)",
                                        "(-4.1, 23.8, -68.9)",
                                        "(1.0, 23.8, -67.9)",
                                        "(5.6, 23.8, -65.4)",
                                        "(9.6, 23.8, -62.5)",
                                        "(11.9, 23.8, -57.8)",
                                        "(12.3, 23.8, -55.6)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "submarine_solo_default",
                                    enable = true,
                                    isRingRoute = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(14.3, 13.8, -60.9)",
                                        "(14.3, 13.9, -55.7)",
                                        "(14.3, 13.6, -50.4)",
                                        "(14.3, 13.1, -45.3)",
                                        "(14.3, 13.4, -40.0)",
                                        "(14.3, 13.8, -34.8)",
                                        "(14.3, 14.2, -29.7)",
                                        "(14.4, 14.4, -24.4)",
                                        "(14.4, 14.9, -19.1)",
                                        "(14.4, 15.5, -14.0)",
                                        "(14.4, 15.6, -8.6)",
                                        "(14.4, 16.0, -3.6)",
                                        "(14.4, 16.7, 1.8)",
                                        "(14.4, 17.7, 7.0)",
                                        "(14.5, 17.7, 12.0)",
                                        "(14.9, 17.6, 17.3)",
                                        "(15.3, 17.8, 22.3)",
                                        "(15.8, 18.1, 27.6)",
                                        "(16.2, 18.6, 32.9)",
                                        "(16.5, 18.9, 38.0)",
                                        "(16.8, 19.7, 43.3)",
                                        "(17.0, 19.8, 48.3)",
                                        "(16.5, 20.3, 53.3)",
                                        "(15.6, 21.0, 58.6)",
                                        "(14.2, 21.0, 63.7)",
                                        "(12.3, 21.1, 68.8)",
                                        "(9.7, 21.3, 73.5)",
                                        "(6.6, 21.3, 77.7)",
                                        "(2.8, 21.1, 81.1)",
                                        "(-1.5, 20.9, 83.9)",
                                        "(-6.6, 20.9, 84.8)",
                                        "(-11.0, 20.9, 82.3)",
                                        "(-14.4, 20.9, 78.4)",
                                        "(-16.6, 20.6, 74.0)",
                                        "(-18.2, 20.5, 68.9)",
                                        "(-18.9, 20.5, 63.8)",
                                        "(-18.6, 20.5, 58.8)",
                                        "(-18.3, 20.4, 53.8)",
                                        "(-17.9, 20.2, 48.8)",
                                        "(-17.7, 19.6, 43.6)",
                                        "(-17.5, 19.3, 38.4)",
                                        "(-17.4, 18.7, 33.2)",
                                        "(-17.3, 17.7, 28.0)",
                                        "(-17.2, 17.0, 22.7)",
                                        "(-17.3, 16.5, 17.3)",
                                        "(-17.3, 15.6, 12.4)",
                                        "(-17.3, 14.4, 7.1)",
                                        "(-17.4, 13.3, 2.0)",
                                        "(-17.5, 12.5, -3.2)",
                                        "(-17.5, 12.3, -8.5)",
                                        "(-17.6, 11.8, -13.6)",
                                        "(-17.7, 10.7, -18.7)",
                                        "(-17.8, 9.9, -23.7)",
                                        "(-17.9, 9.4, -29.0)",
                                        "(-18.0, 9.4, -34.3)",
                                        "(-18.1, 8.9, -39.5)",
                                        "(-18.2, 8.1, -44.4)",
                                        "(-18.3, 8.1, -49.6)",
                                        "(-18.1, 8.1, -54.6)",
                                        "(-17.4, 8.1, -59.7)",
                                        "(-15.7, 8.1, -64.4)",
                                        "(-13.2, 8.4, -68.8)",
                                        "(-9.4, 8.6, -72.3)",
                                        "(-4.8, 9.0, -74.6)",
                                        "(0.3, 9.2, -74.1)",
                                        "(5.3, 9.5, -72.5)",
                                        "(9.7, 9.8, -70.2)",
                                        "(12.9, 10.6, -66.4)",
                                        "(14.1, 11.0, -64.9)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "submarine_solo_default",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(-2.9, 42.2, 48.0)",
                                        "(-3.7, 39.6, 38.3)",
                                        "(-4.2, 36.8, 28.0)",
                                        "(-4.4, 34.0, 17.7)",
                                        "(-4.5, 31.5, 8.0)",
                                        "(-4.5, 29.5, -1.8)",
                                        "(-4.5, 27.2, -12.2)",
                                        "(-4.7, 24.8, -22.6)",
                                        "(-4.9, 23.0, -32.4)"
                                    }
                                },

                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(8.3, 27.5, 55.3)",
                                        "(8.0, 27.8, 56.2)",
                                        "(7.6, 28.0, 57.3)",
                                        "(7.3, 28.0, 58.2)",
                                        "(7.0, 28.3, 59.2)",
                                        "(6.7, 28.4, 60.2)",
                                        "(6.3, 28.4, 61.3)",
                                        "(5.9, 28.4, 62.4)",
                                        "(5.5, 28.6, 63.4)",
                                        "(5.2, 28.7, 64.4)",
                                        "(4.8, 28.8, 65.5)",
                                        "(4.3, 28.9, 66.7)",
                                        "(4.0, 29.1, 67.8)",
                                        "(3.6, 29.1, 68.7)",
                                        "(3.3, 29.3, 69.8)",
                                        "(2.9, 29.4, 70.8)",
                                        "(2.6, 29.6, 71.7)",
                                        "(2.1, 29.6, 72.8)",
                                        "(1.7, 29.7, 73.7)",
                                        "(1.3, 29.6, 74.7)",
                                        "(0.9, 29.6, 75.6)",
                                        "(0.5, 29.4, 76.5)",
                                        "(0.3, 28.5, 76.9)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(-13.0, 5.6, -39.5)",
                                        "(-12.9, 5.1, -42.5)",
                                        "(-12.9, 4.6, -45.5)",
                                        "(-13.0, 4.1, -48.4)",
                                        "(-12.9, 3.6, -51.4)",
                                        "(-11.8, 3.3, -54.2)",
                                        "(-9.6, 3.4, -56.4)",
                                        "(-6.6, 3.9, -56.6)",
                                        "(-3.7, 4.6, -55.8)",
                                        "(-1.1, 5.3, -54.4)",
                                        "(1.7, 6.0, -53.5)",
                                        "(4.7, 6.6, -53.6)",
                                        "(7.7, 7.1, -53.8)",
                                        "(7.7, 7.6, -50.8)",
                                        "(7.8, 8.2, -47.7)",
                                        "(7.9, 8.7, -44.7)",
                                        "(7.8, 9.2, -41.6)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(7.1, 20.8, 45.1)",
                                        "(2.1, 19.9, 45.5)",
                                        "(-2.9, 19.0, 46.0)",
                                        "(-7.8, 18.1, 46.4)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(-11.3, 12.2, 15.7)",
                                        "(-11.7, 11.3, 10.7)",
                                        "(-11.8, 10.4, 5.6)",
                                        "(-12.0, 9.6, 0.6)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(8.8, 13.2, -1.2)",
                                        "(8.9, 14.1, 3.7)",
                                        "(9.1, 15.0, 8.7)",
                                        "(9.2, 15.8, 13.7)",
                                        "(9.2, 16.7, 18.6)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(-11.6, 14.3, 28.0)",
                                        "(-11.5, 15.1, 33.0)",
                                        "(-11.3, 16.0, 37.9)",
                                        "(-11.2, 16.8, 42.8)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(9.8, 20.9, 42.6)",
                                        "(9.8, 20.1, 37.7)",
                                        "(9.6, 19.1, 32.6)",
                                        "(9.4, 18.3, 27.6)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(8.4, 9.0, -26.8)",
                                        "(8.8, 9.7, -21.2)",
                                        "(8.7, 10.6, -16.2)",
                                        "(8.9, 11.5, -11.1)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(-11.7, 8.3, -6.9)",
                                        "(-11.7, 7.5, -12.0)",
                                        "(-11.8, 6.6, -17.0)",
                                        "(-12.3, 5.7, -22.0)",
                                        "(-11.9, 4.9, -26.9)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(-8.9, 5.2, -28.5)",
                                        "(-3.9, 6.0, -28.7)",
                                        "(1.2, 7.0, -29.0)",
                                        "(6.3, 7.9, -29.3)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(-8.4, 33.1, -46.9)",
                                        "(-8.4, 32.9, -47.9)",
                                        "(-8.5, 32.8, -48.9)",
                                        "(-8.3, 32.6, -49.9)",
                                        "(-7.3, 32.7, -50.3)",
                                        "(-6.5, 33.0, -49.8)",
                                        "(-6.6, 33.1, -48.8)",
                                        "(-6.6, 33.3, -47.8)",
                                        "(-6.6, 33.6, -46.8)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(-5.2, 27.7, -50.4)",
                                        "(-4.8, 27.9, -49.3)",
                                        "(-4.7, 28.1, -48.2)",
                                        "(-4.7, 28.3, -47.0)",
                                        "(-4.7, 28.5, -46.0)",
                                        "(-4.6, 28.7, -44.9)",
                                        "(-4.9, 28.9, -43.9)",
                                        "(-5.8, 28.8, -43.3)",
                                        "(-6.8, 28.7, -43.3)",
                                        "(-7.6, 28.4, -43.8)",
                                        "(-8.2, 28.1, -44.6)",
                                        "(-8.4, 27.9, -45.7)",
                                        "(-8.4, 27.7, -46.8)",
                                        "(-8.4, 27.5, -47.8)",
                                        "(-8.3, 27.4, -48.9)",
                                        "(-8.2, 27.2, -50.0)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(-12.9, 22.3, -50.8)",
                                        "(-8.0, 23.3, -50.7)",
                                        "(-2.8, 24.3, -50.5)",
                                        "(1.9, 26.0, -50.7)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(9.0, 21.1, 44.7)",
                                        "(8.9, 21.2, 45.7)",
                                        "(8.9, 21.4, 46.7)",
                                        "(8.9, 21.6, 47.7)",
                                        "(9.0, 21.7, 48.7)",
                                        "(9.0, 21.9, 49.7)",
                                        "(8.9, 22.1, 50.8)",
                                        "(8.7, 22.6, 51.7)",
                                        "(8.6, 23.4, 52.3)",
                                        "(8.5, 24.4, 52.5)",
                                        "(8.4, 25.0, 53.3)",
                                        "(8.3, 25.8, 53.9)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(-10.9, 17.3, 45.4)",
                                        "(-10.9, 17.5, 46.4)",
                                        "(-10.8, 17.7, 47.5)",
                                        "(-10.7, 17.9, 48.6)",
                                        "(-10.6, 18.1, 49.6)",
                                        "(-10.5, 18.3, 50.7)",
                                        "(-10.5, 18.5, 51.7)",
                                        "(-10.5, 19.1, 52.5)",
                                        "(-10.6, 19.9, 53.1)",
                                        "(-10.6, 20.9, 53.4)",
                                        "(-10.8, 21.4, 54.2)",
                                        "(-10.9, 22.3, 54.8)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(-2.7, 28.9, 75.9)",
                                        "(-4.6, 27.1, 71.1)",
                                        "(-6.9, 26.4, 66.2)",
                                        "(-8.8, 24.9, 61.8)",
                                        "(-10.2, 23.2, 57.1)"
                                    }
                                },

                                new PatrolCustomConfig
                                {
                                    presetName = "shark_default",
                                    enable = true,
                                    isRingRoute = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(50.9, 27.3, -11.6)",
                                        "(46.2, 25.2, -10.2)",
                                        "(41.1, 24.5, -10.4)",
                                        "(36.7, 23.6, -12.5)",
                                        "(33.0, 22.6, -16.2)",
                                        "(30.3, 21.7, -20.7)",
                                        "(29.0, 21.0, -25.8)",
                                        "(29.8, 20.5, -31.0)",
                                        "(32.8, 20.2, -35.4)",
                                        "(36.7, 20.1, -38.9)",
                                        "(41.8, 20.4, -39.7)",
                                        "(46.3, 21.5, -37.3)",
                                        "(50.8, 22.8, -34.7)",
                                        "(54.7, 24.2, -31.4)",
                                        "(57.8, 25.4, -27.2)",
                                        "(59.6, 26.6, -22.4)",
                                        "(59.3, 27.4, -17.2)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "shark_default",
                                    enable = true,
                                    isRingRoute = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(15.7, 15.4, 98.7)",
                                        "(12.7, 14.7, 94.3)",
                                        "(9.5, 14.2, 90.1)",
                                        "(5.9, 13.6, 86.2)",
                                        "(1.6, 13.1, 83.0)",
                                        "(-3.1, 12.6, 80.7)",
                                        "(-8.3, 12.2, 79.4)",
                                        "(-13.6, 12.0, 79.1)",
                                        "(-18.9, 12.0, 79.7)",
                                        "(-24.0, 12.3, 81.0)",
                                        "(-28.8, 12.5, 83.4)",
                                        "(-32.5, 12.9, 87.2)",
                                        "(-34.7, 13.3, 92.0)",
                                        "(-32.1, 13.2, 96.7)",
                                        "(-28.1, 13.0, 100.2)",
                                        "(-23.9, 12.9, 103.2)",
                                        "(-19.0, 12.7, 105.1)",
                                        "(-14.0, 12.4, 106.9)",
                                        "(-8.8, 12.5, 108.3)",
                                        "(-3.6, 12.6, 109.4)",
                                        "(1.7, 12.9, 109.9)",
                                        "(7.0, 13.3, 109.4)",
                                        "(11.7, 13.3, 107.1)",
                                        "(15.4, 12.9, 102.9)",
                                        "(15.8, 13.4, 97.9)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "shark_default",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(20.9, 3.1, -75.0)",
                                        "(14.1, 3.0, -74.5)",
                                        "(9.0, 3.0, -73.9)",
                                        "(2.6, 2.9, -72.8)",
                                        "(-2.3, 2.5, -71.4)",
                                        "(-8.0, 2.2, -69.6)",
                                        "(-13.2, 2.4, -68.2)",
                                        "(-19.0, 2.5, -67.2)",
                                        "(-24.5, 2.1, -67.5)",
                                        "(-29.7, 1.5, -68.8)",
                                        "(-34.8, 0.8, -70.3)"
                                    }
                                },

                                new PatrolCustomConfig
                                {
                                    presetName = "shark_default",
                                    enable = true,
                                    isRingRoute = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(-67.9, 11.5, -63.6)",
                                        "(-68.7, 10.4, -58.3)",
                                        "(-69.8, 9.8, -53.1)",
                                        "(-71.5, 9.5, -48.1)",
                                        "(-75.1, 9.4, -44.3)",
                                        "(-80.1, 9.5, -42.9)",
                                        "(-85.5, 9.6, -43.3)",
                                        "(-90.3, 9.9, -45.3)",
                                        "(-94.1, 10.2, -49.0)",
                                        "(-96.6, 10.4, -53.7)",
                                        "(-98.1, 10.6, -58.8)",
                                        "(-98.2, 10.6, -63.9)",
                                        "(-94.8, 10.0, -68.1)",
                                        "(-91.0, 9.7, -71.9)",
                                        "(-86.7, 10.0, -75.0)",
                                        "(-81.8, 10.6, -77.0)",
                                        "(-76.6, 11.2, -77.3)",
                                        "(-71.5, 11.5, -75.9)",
                                        "(-67.7, 11.7, -72.4)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "shark_default",
                                    enable = true,
                                    isRingRoute = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(-29.4, 11.3, -5.6)",
                                        "(-31.3, 10.8, -0.7)",
                                        "(-34.0, 10.3, 3.9)",
                                        "(-37.6, 10.1, 7.8)",
                                        "(-41.9, 10.0, 10.9)",
                                        "(-46.7, 10.0, 13.1)",
                                        "(-51.9, 10.0, 14.5)",
                                        "(-56.6, 9.5, 12.2)",
                                        "(-60.5, 9.2, 8.6)",
                                        "(-62.6, 9.0, 3.7)",
                                        "(-62.9, 8.9, -1.6)",
                                        "(-61.4, 8.9, -6.6)",
                                        "(-58.3, 9.0, -11.0)",
                                        "(-54.2, 9.2, -14.3)",
                                        "(-49.7, 9.8, -16.5)",
                                        "(-44.3, 9.7, -17.2)",
                                        "(-38.8, 9.9, -16.8)",
                                        "(-33.9, 10.2, -15.9)",
                                        "(-29.3, 10.5, -13.3)"
                                    }
                                }
                            },
                            doorType = 0,
                            enableCargoSinkEffect = false,
                        },
                        new EventConfig
                        {
                            presetName = "barge",
                            displayName = en ? "Sunken barge" : "Затонувший буксир",
                            dataFileName = "barge",
                            automatickStart = true,
                            eventTime = 7200,
                            chance = 30,
                            radius = 45,
                            customPatrolRoutes = new List<PatrolCustomConfig>
                            {
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(29.3, 2.9, 10.8)",
                                        "(24.4, 2.7, 13.3)",
                                        "(19.7, 2.4, 15.7)",
                                        "(14.9, 2.1, 18.0)",
                                        "(10.1, 1.9, 20.4)",
                                        "(5.3, 1.6, 22.7)",
                                        "(0.4, 1.4, 24.9)",
                                        "(-4.5, 1.2, 26.9)",
                                        "(-9.5, 1.0, 28.8)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(-18.4, 1.6, -13.3)",
                                        "(-13.3, 1.1, -14.6)",
                                        "(-8.1, 1.3, -16.1)",
                                        "(-3.1, 1.4, -17.7)",
                                        "(2.0, 1.3, -19.5)",
                                        "(6.9, 1.4, -21.5)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(6.1, 10.1, 6.1)",
                                        "(1.4, 9.1, 7.7)",
                                        "(-3.5, 8.4, 9.4)",
                                        "(-8.7, 7.8, 11.0)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "shark_default",
                                    enable = true,
                                    isRingRoute = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(13.9, 15.6, 7.3)",
                                        "(10.7, 14.3, 11.3)",
                                        "(7.4, 13.1, 15.3)",
                                        "(3.4, 12.5, 18.8)",
                                        "(-1.1, 12.6, 21.6)",
                                        "(-6.1, 13.2, 23.2)",
                                        "(-10.9, 13.2, 21.0)",
                                        "(-15.2, 13.2, 17.8)",
                                        "(-18.4, 13.3, 13.7)",
                                        "(-20.3, 13.4, 8.7)",
                                        "(-20.7, 13.5, 3.4)",
                                        "(-18.4, 13.5, -1.5)",
                                        "(-14.2, 13.1, -4.7)",
                                        "(-9.6, 13.4, -7.5)",
                                        "(-4.9, 14.1, -9.8)",
                                        "(0.2, 15.2, -11.1)",
                                        "(5.4, 16.0, -11.4)",
                                        "(10.6, 16.7, -10.4)",
                                        "(15.5, 17.5, -7.8)",
                                        "(18.7, 17.6, -3.5)",
                                        "(20.6, 17.4, 1.3)"
                                    }
                                }
                            }
                        },
                        new EventConfig
                        {
                            presetName = "farm",
                            displayName = en ? "Submerged ore farm" : "Затопленный рудник",
                            dataFileName = "farm",
                            automatickStart = true,
                            eventTime = 7200,
                            chance = 30,
                            radius = 80,
                            customPatrolRoutes = new List<PatrolCustomConfig>
                            {
                                new PatrolCustomConfig
                                {
                                    presetName = "submarine_solo_default",
                                    enable = true,
                                    isRingRoute = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(30.1, 26.5, -4.7)",
                                        "(31.3, 24.9, -9.8)",
                                        "(32.3, 23.5, -14.8)",
                                        "(32.9, 22.2, -20.0)",
                                        "(32.7, 21.1, -25.2)",
                                        "(31.0, 20.2, -30.1)",
                                        "(28.6, 19.2, -34.8)",
                                        "(25.0, 18.7, -38.6)",
                                        "(20.0, 18.8, -40.0)",
                                        "(15.1, 19.2, -38.1)",
                                        "(10.2, 20.0, -36.1)",
                                        "(5.4, 20.8, -34.0)",
                                        "(0.7, 21.4, -31.6)",
                                        "(-4.0, 21.3, -29.1)",
                                        "(-8.7, 20.4, -26.7)",
                                        "(-13.4, 19.0, -24.5)",
                                        "(-23.9, 17.2, -19.0)",
                                        "(-28.0, 16.9, -15.7)",
                                        "(-31.2, 17.2, -11.5)",
                                        "(-33.2, 18.3, -6.7)",
                                        "(-34.1, 19.7, -1.7)",
                                        "(-33.8, 21.2, 3.4)",
                                        "(-32.0, 22.7, 8.2)",
                                        "(-28.1, 23.7, 11.8)",
                                        "(-22.9, 23.6, 12.5)",
                                        "(-17.9, 23.5, 14.3)",
                                        "(-13.2, 22.8, 16.8)",
                                        "(-8.9, 21.7, 19.6)",
                                        "(-4.7, 20.1, 22.5)",
                                        "(-0.1, 18.7, 24.8)",
                                        "(4.9, 17.8, 25.7)",
                                        "(10.2, 17.9, 26.4)",
                                        "(15.2, 19.5, 27.0)",
                                        "(19.9, 21.9, 27.5)",
                                        "(24.7, 23.0, 25.7)",
                                        "(28.1, 22.3, 21.9)",
                                        "(28.1, 22.2, 16.6)",
                                        "(27.8, 23.1, 11.4)",
                                        "(28.3, 24.1, 6.2)",
                                        "(28.9, 25.1, 1.0)",
                                    },

                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(14.0, 7.8, 15.7)",
                                        "(9.8, 7.6, 12.6)",
                                        "(5.6, 7.6, 9.5)",
                                        "(1.2, 7.2, 6.5)",
                                        "(1.1, 6.0, 1.4)",
                                        "(1.7, 3.9, -3.4)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(17.3, 2.3, -24.3)",
                                        "(20.2, 1.7, -29.1)",
                                        "(22.9, 1.2, -33.6)",
                                        "(25.6, 1.1, -38.2)",
                                        "(28.3, 0.9, -42.8)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(-34.0, 10.6, -16.0)",
                                        "(-28.9, 11.6, -17.4)",
                                        "(-23.9, 12.6, -18.9)",
                                        "(-18.8, 13.6, -20.4)",
                                        "(-13.8, 14.6, -21.9)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(7.7, 8.7, 26.4)",
                                        "(11.5, 7.4, 22.5)",
                                        "(15.1, 6.4, 19.2)",
                                        "(18.1, 6.0, 14.8)"
                                    }
                                },
                                new PatrolCustomConfig
                                {
                                    presetName = "diver_1",
                                    enable = true,
                                    patrolPathList = new List<string>
                                    {
                                        "(7.9, 6.2, 58.5)",
                                        "(3.3, 5.7, 56.1)",
                                        "(-2.3, 5.1, 53.1)",
                                        "(-6.8, 4.5, 50.7)",
                                        "(-11.6, 4.2, 47.9)"
                                    }
                                }
                            }
                        }
                    },
                    sharkConfigs = new HashSet<SharkConfig>
                    {
                        new SharkConfig
                        {
                            presetName = "shark_default",
                            damageScale = 1,
                            health = 100,
                            minSpeed = 4,
                            maxSpeed = 10,
                            minTurnSpeed = 1.5f,
                            maxTurnSpeed = 3.5f,
                            aggroRange = 20,
                            attackCooldown = 4
                        }
                    },
                    submarineConfigs = new HashSet<SubmarineConfig>
                    {
                        new SubmarineConfig
                        {
                            presetName = "submarine_solo_default",
                            submarineType = 0,
                            attackRange = 40,
                            health = 100,
                            patrolSpeed = 1f,
                            turnSpeed = 1,
                            targetDetectionRange = 30,
                            targetLossRange = 60,
                            visionCone = 90,
                            memoryDuration = 35,
                            torpedoCount = 0,
                            timeBetweenShots = 7,
                        },
                    },
                    crates = new HashSet<CrateConfig>
                    {
                        new CrateConfig
                        {
                            prefab = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab",
                            typeLootTable = 0,
                            crateUnlockTime = 60,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/crate_elite.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/crate_normal.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/crate_normal_2_medical.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/crate_normal_2_food.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/crate_tools.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/foodbox.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },

                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_elite.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/crate_underwater_advanced.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_normal.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_normal_2.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_ammunition.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/crate_underwater_basic.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/loot_barrel_1.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/autospawn/resource/loot/loot-barrel-1.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/autospawn/resource/loot/loot-barrel-2.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/loot_barrel_2.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },
                        new CrateConfig
                        {
                            prefab = "assets/bundled/prefabs/radtown/oil_barrel.prefab",
                            typeLootTable = 0,
                            lootTable = new OwnLootTableConfig
                            {
                                minItemsAmount = 1,
                                maxItemsAmount = 1,
                                items = new List<LootItemConfig>
                                {
                                    new LootItemConfig
                                    {
                                        shortname = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBlueprint = false,
                                        skin = 0,
                                        name = ""
                                    }
                                }
                            },
                        },

                    },
                    npcConfigs = new HashSet<NpcConfig>
                    {
                        new NpcConfig
                        {
                            presetName = "diver_1",
                            name = en ? "Diver" : "Аквалангист",
                            health = 10,
                            wearItems = new List<NpcWear>
                            {
                                new NpcWear
                                {
                                    shortName = "hazmatsuit_scientist_nvgm",
                                    skinID = 0
                                }
                            },
                            beltItems = new List<NpcBelt>
                            {
                                new NpcBelt
                                {
                                    shortName = "speargun",
                                    amount = 1,
                                    skinID = 0,
                                    Mods = new List<string> { },
                                    ammo = ""
                                }
                            },
                            kit = "",
                            deleteCorpse = true,
                            disableRadio = true,
                            attackRangeMultiplier = 2.5f,
                            senseRange = 30,
                            memoryDuration = 60f,
                            damageScale = 1f,
                            checkVisionCone = true,
                            visionCone = 135f,
                            speed = 1
                        }
                    },
                    animationConfig = new AnimationConfig
                    {
                        timeBeforeF15TakeOff = 60,
                        enableExplosionsEffects = true,
                        decorEntities = new HashSet<DataSkinnedEntityConfig>
                        {
                            new DataSkinnedEntityConfig
                            {
                                prefab = "assets/prefabs/building/wall.frame.garagedoor/wall.frame.garagedoor.prefab",
                                skin = 1788350229,
                                locations = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-7.512, 6.5, 57.093)",
                                        rotation = "(0, 90, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(7.550, 6.5, 57.093)",
                                        rotation = "(0, 90, 0)"
                                    },

                                    new LocationConfig
                                    {
                                        position = "(7.488, 6.430, -33.062)",
                                        rotation = "(0, 270, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-7.488, 6.430, -33.062)",
                                        rotation = "(0, 270, 0)"
                                    }
                                }
                            }
                        }
                    },
                    marker = new MarkerConfig
                    {
                        useShopMarker = true,
                        useRingMarker = true,
                        radius = 0.2f,
                        alpha = 0.6f,
                        color1 = new ColorConfig { r = 0.81f, g = 0.25f, b = 0.15f },
                        color2 = new ColorConfig { r = 0f, g = 0f, b = 0f }
                    },
                    eventZone = new ZoneConfig
                    {
                        isCreateZonePVP = false,
                        isDome = false,
                        darkening = 5
                    },
                    notificationConfig = new NotificationConfig
                    {
                        chat = true,
                        guiConfig = new GUIConfig
                        {
                            isCountdownGUI = false,
                            anchorMin = "0 0.88",
                            anchorMax = "1 0.93"
                        }
                    },
                    supportedPluginsConfig = new SupportedPluginsConfig
                    {
                        GUIAnnouncements = new GUIAnnouncementsConfig
                        {
                            enable = false,
                            bannerColor = "Grey",
                            textColor = "White",
                            apiAdjustVPosition = 0.03f
                        },
                        notify = new NotifyPluginConfig
                        {
                            enable = false,
                            type = "0"
                        },
                        discord = new DiscordConfig
                        {
                            enable = false,
                            webhookUrl = "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks",
                            embedColor = 13516583,
                            keys = new HashSet<string>
                            {
                                "PreStartEvent",
                                "StartEvent",
                                "Crash"
                            }
                        },
                        pveMode = new PveModeConfig
                        {
                            pve = false,
                            showEventOwnerNameOnMap = true,
                            damage = 25,
                            scaleDamage = new HashSet<ScaleDamageConfig>
                            {
                                new ScaleDamageConfig { Type = "NPC", Scale = 1f },
                            },
                            lootCrate = false,
                            hackCrate = false,
                            lootNpc = false,
                            damageNpc = false,
                            targetNpc = false,
                            canEnter = false,
                            canEnterCooldownPlayer = true,
                            timeExitOwner = 300,
                            alertTime = 60,
                            cooldownOwner = 86400,
                            darkening = 5
                        },
                        economic = new EconomicsConfig
                        {
                            enable = false,
                            plugins = new HashSet<string> { "Economics", "Server Rewards", "IQEconomic" },
                            minCommandPoint = 0,
                            minEconomyPiont = 0,
                            lockedCratePoint = 5,
                            crates = new Dictionary<string, double>
                            {
                                ["assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab"] = 10,
                                ["assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate_oilrig.prefab"] = 10,
                                ["assets/bundled/prefabs/radtown/crate_elite.prefab"] = 10,
                                ["assets/bundled/prefabs/radtown/crate_normal.prefab"] = 10,
                                ["assets/bundled/prefabs/radtown/crate_normal_2.prefab"] = 10,
                                ["assets/bundled/prefabs/radtown/crate_normal_2_medical.prefab"] = 10,
                            },
                            npcPoint = 2,
                            sharkPoint = 5,
                            submarinePoint = 10,
                            doorPoint = 1,
                            redCardPoint = 5,
                            blueCardPoint = 4,
                            greenCardPoint = 3,
                            commands = new HashSet<string>()
                        },
                        superCardConfig = new SuperCardConfig
                        {
                            enable = false,
                            skin = 1988408422
                        }
                    }
                };
            }
        }
        #endregion Config
    }
}

namespace Oxide.Plugins.ShipwreckExtensionMethods
{
    public static class ExtensionMethods
    {
        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return true;
            return false;
        }

        public static HashSet<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            HashSet<TSource> result = new HashSet<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) result.Add(enumerator.Current);
            return result;
        }

        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return enumerator.Current;
            return default(TSource);
        }

        public static HashSet<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> predicate)
        {
            HashSet<TResult> result = new HashSet<TResult>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(predicate(enumerator.Current));
            return result;
        }

        public static List<TResult> Select<TSource, TResult>(this IList<TSource> source, Func<TSource, TResult> predicate)
        {
            List<TResult> result = new List<TResult>();
            for (int i = 0; i < source.Count; i++)
            {
                TSource element = source[i];
                result.Add(predicate(element));
            }
            return result;
        }

        public static HashSet<T> OfType<T>(this IEnumerable<BaseNetworkable> source)
        {
            HashSet<T> result = new HashSet<T>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (enumerator.Current is T) result.Add((T)(object)enumerator.Current);
            return result;
        }

        public static bool IsExists(this BaseNetworkable entity) => entity != null && !entity.IsDestroyed;

        public static void ClearItemsContainer(this ItemContainer container)
        {
            for (int i = container.itemList.Count - 1; i >= 0; i--)
            {
                Item item = container.itemList[i];
                item.RemoveFromContainer();
                item.Remove();
            }
        }

        public static bool IsRealPlayer(this BasePlayer player) => player != null && player.userID.IsSteamId();

        public static List<TSource> OrderBy<TSource>(this IEnumerable<TSource> source, Func<TSource, float> predicate)
        {
            List<TSource> result = source.ToList();
            for (int i = 0; i < result.Count; i++)
            {
                for (int j = 0; j < result.Count - 1; j++)
                {
                    if (predicate(result[j]) > predicate(result[j + 1]))
                    {
                        TSource z = result[j];
                        result[j] = result[j + 1];
                        result[j + 1] = z;
                    }
                }
            }
            return result;
        }

        public static List<TSource> ToList<TSource>(this IEnumerable<TSource> source)
        {
            List<TSource> result = new List<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(enumerator.Current);
            return result;
        }

        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
        {
            HashSet<TSource> result = new HashSet<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) result.Add(enumerator.Current);
            return result;
        }

        public static TSource Max<TSource>(this IEnumerable<TSource> source, Func<TSource, float> predicate)
        {
            TSource result = source.ElementAt(0);
            float resultValue = predicate(result);
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    TSource element = enumerator.Current;
                    float elementValue = predicate(element);
                    if (elementValue > resultValue)
                    {
                        result = element;
                        resultValue = elementValue;
                    }
                }
            }
            return result;
        }

        public static TSource Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float> predicate)
        {
            TSource result = source.ElementAt(0);
            float resultValue = predicate(result);
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    TSource element = enumerator.Current;
                    float elementValue = predicate(element);
                    if (elementValue < resultValue)
                    {
                        result = element;
                        resultValue = elementValue;
                    }
                }
            }
            return result;
        }

        public static TSource ElementAt<TSource>(this IEnumerable<TSource> source, int index)
        {
            int movements = 0;
            using (var enumerator = source.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (movements == index) return enumerator.Current;
                    movements++;
                }
            }
            return default(TSource);
        }

        public static TSource First<TSource>(this IList<TSource> source) => source[0];

        public static TSource Last<TSource>(this IList<TSource> source) => source[source.Count - 1];
    }
}