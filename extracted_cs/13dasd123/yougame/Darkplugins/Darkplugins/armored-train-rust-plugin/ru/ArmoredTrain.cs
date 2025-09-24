// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System.Collections.Generic;
using Newtonsoft.Json;
using CompanionServer.Handlers;
using Oxide.Plugins.ArmoredTrainExtensionMethods;
using UnityEngine;
using System;
using Oxide.Core.Plugins;
using Network;
using Rust;
using System.Collections;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("ArmoredTrain", "Sempai#3239", "1.2.0")]
    class ArmoredTrain : RustPlugin
    {
        [PluginReference] Plugin NpcSpawn, PveMode, GUIAnnouncements, DiscordMessages;

        #region Variables
        const bool en = false;
        static ArmoredTrain ins;
        Coroutine autoEventCoroutine;
        Train train;
        bool isEventActive = false;
        bool underGround = false;
        internal List<Transform> railTransformList = new List<Transform>();
        HashSet<string> subscribeMethods = new HashSet<string>
        {
            "CanBradleyApcTarget",
            "OnEntityTakeDamage",
            "OnButtonPress",
            "OnTurretTarget",
            "CanLootEntity",
            "CanHackCrate",
            "OnLootEntity",
            "OnEntityDeath",
            "OnEntityKill",
            "OnLootEntity",
            "OnCorpsePopulate",
            "OnTrainCarUncouple",
            "OnEntitySpawned",
            "CanTrainCarCouple",
            "CanMountEntity",
            "OnSamSiteModeToggle"
        };
        Coroutine startEventCoroutine;
        #endregion Variables

        #region ExternalAPI
        bool IsArmoredTrainActive() => isEventActive;

        bool StopArmoredTrain()
        {
            if (!isEventActive) return false;
            train.StopTrain(null);
            return true;
        }

        bool StartArmoredTrainEvent()
        {
            if (isEventActive) return false;
            StartEvent();
            return true;
        }

        bool EndArmoredTrainEvent()
        {
            if (!isEventActive) return false;
            EndEvent();
            return true;
        }

        Vector3 ArmoredTrainLocomotivePosition()
        {
            if (!isEventActive) return Vector3.zero;
            return train.locomotive.trainEngine.transform.position;
        }
        #endregion ExternalAPI

        #region Hooks
        void Init() => Unsubscribes();

        void OnServerInitialized()
        {
			PrintWarning("\n-----------------------------\n" +
            " Author - Sempai#3239\n" +
            " VK - https://vk.com/rustnastroika/n" +
            " Forum - https://darkplugins.ru/n" +
            " Discord - https://discord.gg/5DPTsRmd3G/n" +
            "-----------------------------");
            ins = this;
            UpdateConfig();
            if (_config.mainConfig.isAutoEvent) autoEventCoroutine = ServerMgr.Instance.StartCoroutine(AutoEventCorountine());
            PostLoadCheck();
        }

        void OnButtonPress(PressButton button, BasePlayer player)
        {
            if (!isEventActive || !button.IsExists() || player == null) return;
            if (train.IsTrainButton(button.net.ID)) OnPlayerTryStopTrain(player);
        }

        object CanPickupEntity(BasePlayer player, PressButton entity)
        {
            if (!isEventActive || !entity.IsExists() || player == null) return null;
            if (train.IsTrainButton(entity.net.ID)) return false;
            return null;
        }

        object OnTrainCarUncouple(TrainCar trainCar, BasePlayer player)
        {
            if (!isEventActive || trainCar == null || player == null) return null;
            if (train.IsTrainWagon(trainCar.net.ID)) return true;
            return null;
        }

        object CanTrainCarCouple(TrainCar trainCar1, TrainCar trainCar2)
        {
            if (!isEventActive || _config.mainConfig.allowConnectWagons || !trainCar1.IsExists() || !trainCar2.IsExists()) return null;
            if (!train.IsTrainWagon(trainCar1.net.ID) && !train.IsTrainWagon(trainCar2.net.ID)) return null;
            return false;
        }

        object CanMountEntity(BasePlayer player, BaseVehicleSeat entity)
        {
            if (!isEventActive || !player.userID.IsSteamId() || entity == null || entity.VehicleParent() == null || !train.IsTrainWagon(entity.VehicleParent().net.ID)) return null;
            return true;
        }

        object OnTurretTarget(AutoTurret turret, BaseCombatEntity entity)
        {
            if (!isEventActive || !turret.IsExists() || !entity.IsExists()) return null;
            if (!train.IsTrainTurret(turret.net.ID)) return null;
            BasePlayer player = entity as BasePlayer;
            if (!player.IsRealPlayer()) return true;
            if (!train.IsTrainCanAttack()) return true;
            return null;
        }

        object CanBradleyApcTarget(BradleyAPC apc, BaseEntity entity)
        {
            if (!isEventActive || !apc.IsExists() || !entity.IsExists()) return null;
            if (!train.IsTrainBradley(apc.net.ID)) return null;
            BasePlayer player = entity as BasePlayer;
            if (!player.IsRealPlayer()) return false;
            if (!train.IsTrainCanAttack()) return false;
            return null;
        }

        object CanSamSiteShoot(SamSite samSite)
        {
            if (!isEventActive || samSite == null) return null;
            if (!train.IsTrainSamSite(samSite.net.ID)) return null;
            if (!train.IsTrainCanAttack()) return true;
            return null;
        }

        object OnSamSiteModeToggle(SamSite samSite, BasePlayer player, bool flag)
        {
            if (samSite == null || player == null) return null;
            if (train.IsTrainSamSite(samSite.net.ID)) return true;
            return null;
        }

        object CanHelicopterTarget(PatrolHelicopterAI heli, BasePlayer player)
        {
            if (!isEventActive || !heli.helicopterBase.IsExists()) return null;
            if (!train.IsTrainHeli(heli.helicopterBase.net.ID)) return null;
            if (!train.IsTrainCanAttack()) return false;
            return null;
        }

        void OnEntitySpawned(HelicopterDebris entity)
        {
            if (!isEventActive || !entity.IsExists() || entity.transform == null || train == null) return;
            if (Vector3.Distance(entity.transform.position, train.locomotive.baseWagonEntity.transform.position) < 20 || train.wagons.Any(x => Vector3.Distance(entity.transform.position, x.baseWagonEntity.transform.position) < 20)) entity.Kill();
        }

        object OnEntityTakeDamage(ScientistNPC scientistNPC, HitInfo info)
        {
            if (!isEventActive || scientistNPC == null) return null;
            if (!_config.NPCConfigs.Any(x => x.name == scientistNPC.displayName) && !_config.driverConfigs.Any(x => x.name == scientistNPC.displayName)) return null;
            OnTrainAttacked(info.InitiatorPlayer);
            if (!_config.mainConfig.allowDriverDamage && train.IsTrainDriver(scientistNPC.net.ID)) return true;
            return null;
        }

        object OnEntityTakeDamage(BradleyAPC bradley, HitInfo info)
        {
            if (!isEventActive || bradley == null) return null;
            if (!train.IsTrainBradley(bradley.net.ID)) return null;
            return train.OnTrainAttacked(info);
        }

        object OnEntityTakeDamage(AutoTurret autoTurret, HitInfo info)
        {
            if (!isEventActive || autoTurret == null) return null;
            if (!train.IsTrainTurret(autoTurret.net.ID)) return null;
            return train.OnTrainAttacked(info);
        }

        object OnEntityTakeDamage(SamSite samSite, HitInfo info)
        {
            if (!isEventActive || samSite == null) return null;
            if (!train.IsTrainSamSite(samSite.net.ID)) return null;
            return train.OnTrainAttacked(info);
        }

        object OnEntityTakeDamage(TrainCar trainEngine, HitInfo info)
        {
            if (train == null || trainEngine == null) return null;
            if (!train.IsTrainWagon(trainEngine.net.ID)) return null;
            return true;
        }

        object OnEntityTakeDamage(PressButton pressButton, HitInfo info)
        {
            if (!isEventActive || pressButton == null) return null;
            if (train.IsTrainButton(pressButton.net.ID)) return true;
            return null;
        }

        void OnEntityDeath(BradleyAPC entity, HitInfo info)
        {
            if (!isEventActive || entity == null || info == null || !info.InitiatorPlayer.IsRealPlayer()) return;
            BaseWagon baseWagon = train.locomotive.bradleys.Any(y => y != null && y.net.ID == entity.net.ID) ? train.locomotive : train.wagons.FirstOrDefault(x => x != null && x.bradleys.Any(y => y != null && y.net.ID == entity.net.ID));
            if (baseWagon == null) return;
            baseWagon.bradleys.Remove(entity);
            ActionEconomy(info.InitiatorPlayer.userID, "Bradley");
        }

        void OnEntityDeath(ScientistNPC scientistNPC, HitInfo info)
        {
            if (!isEventActive || scientistNPC == null || info == null) return;
            if (info.InitiatorPlayer.IsRealPlayer() && _config.NPCConfigs.Any(x => x != null && x.name == scientistNPC.displayName)) ActionEconomy(info.InitiatorPlayer.userID, "Npc");
            else if (_config.driverConfigs.Any(x => x != null && x.name == scientistNPC.displayName) && train.IsTrainDriver(scientistNPC.net.ID)) OnPlayerTryStopTrain(info.InitiatorPlayer);
        }

        void OnEntityDeath(AutoTurret entity, HitInfo info)
        {
            if (!isEventActive || entity == null || info == null || !info.InitiatorPlayer.IsRealPlayer()) return;
            BaseWagon baseWagon = train.locomotive.turrets.Any(y => y != null && y.net.ID == entity.net.ID) ? train.locomotive : train.wagons.FirstOrDefault(x => x != null && x.turrets.Any(y => y != null && y.net.ID == entity.net.ID));
            if (baseWagon == null) return;
            baseWagon.turrets.Remove(entity);
            ActionEconomy(info.InitiatorPlayer.userID, "Turret");
        }

        void OnEntityDeath(BaseHelicopter entity, HitInfo info)
        {
            if (!isEventActive || entity == null || info == null || !info.InitiatorPlayer.IsRealPlayer()) return;
            if (!train.IsTrainHeli(entity.net.ID)) return;
            if (_config.supportedPluginsConfig.economy.enable) ActionEconomy(info.InitiatorPlayer.userID, "Heli");
        }

        void OnEntityKill(BasePlayer player)
        {
            if (!isEventActive || !player.IsRealPlayer() || !train.playersInZone.Contains(player)) return;
            train.playersInZone.Remove(player);
            if (_config.guiConfig.IsGUI) CuiHelper.DestroyUi(player, "TextMain");
        }

        void OnEntityKill(TrainCar trainCar)
        {
            if (!isEventActive || trainCar == null) return;
            if (!train.IsTrainWagon(trainCar.net.ID)) return;
            train.DiconnectTrain();
        }

        object CanLootEntity(BasePlayer player, LootContainer container)
        {
            if (!isEventActive || !_config.mainConfig.needStopTrain || player == null || container == null) return null;
            if (!train.IsTrainCrate(container.net.ID) || train.CanLoot()) return null;
            InformPlayer(player, "NeedStopTrain", _config.prefix);
            return true;
        }

        object CanLootEntity(BasePlayer player, SamSite samSite)
        {
            if (!isEventActive || samSite == null) return null;
            if (train.IsTrainSamSite(samSite.net.ID)) return true;
            return null;
        }

        object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (!isEventActive || !_config.mainConfig.needStopTrain || player == null || crate == null) return null;

            if (train.IsTrainLockedCrate(crate.net.ID))
            {
                if (!train.CanLoot())
                {
                    InformPlayer(player, "NeedStopTrain", _config.prefix);
                    return true;
                }
                else
                {
                    ActionEconomy(player.userID, "LockedCrate");
                }
            }
            return null;
        }

        private void OnLootEntity(BasePlayer player, LootContainer container)
        {
            if (!isEventActive || player == null || container == null) return;
            if (train.IsTrainCrate(container.net.ID) && !train.openedCrates.Contains(container.net.ID))
            {
                train.openedCrates.Add(container.net.ID);
                ActionEconomy(player.userID, "Crates", container.PrefabName);
                train.CheckAllCrates();
            }
        }

        void OnCorpsePopulate(BasePlayer entity, NPCPlayerCorpse corpse)
        {
            if (!isEventActive || entity == null || corpse == null) return;
            if (entity is ScientistNPC)
            {
                NpcConfig npcConfig = _config.NPCConfigs.FirstOrDefault(x => x.name == entity.displayName);
                if (npcConfig == null) return;
                NextTick(() =>
                {
                    if (corpse == null) return;
                    ItemContainer container = corpse.containers[0];

                    if (npcConfig.typeLootTable == 0)
                    {
                        for (int i = container.itemList.Count - 1; i >= 0; i--)
                        {
                            Item item = container.itemList[i];
                            if (npcConfig.wearItems.Any(x => x.shortName == item.info.shortname))
                            {
                                item.RemoveFromContainer();
                                item.Remove();
                            }
                        }
                        if (npcConfig.deleteCorpse && corpse != null && !corpse.IsDestroyed) corpse.Kill();
                        return;
                    }

                    if (npcConfig.typeLootTable == 2 || npcConfig.typeLootTable == 3)
                    {
                        if (npcConfig.deleteCorpse && !corpse.IsDestroyed) corpse.Kill();
                        return;
                    }

                    container.ClearItemsContainer();

                    if (npcConfig.typeLootTable == 1 && npcConfig.lootTable.minAmount > 0) AddToContainerItem(container, npcConfig.lootTable, npcConfig.typeLootTable);

                    if (npcConfig.deleteCorpse && corpse != null && !corpse.IsDestroyed) corpse.Kill();
                });
            }
        }

        void Unload() => EndEvent(true);

        #region OtherPlugins
        object CanBradleySpawnNpc(BradleyAPC bradley)
        {
            if (!isEventActive || bradley == null) return null;
            if (train.IsTrainBradley(bradley.net.ID)) return true;
            return null;
        }

        object CanEntityTakeDamage(BasePlayer victim, HitInfo hitinfo)
        {
            if (!isEventActive || victim == null || hitinfo == null || !victim.userID.IsSteamId()) return null;
            BasePlayer attacker = hitinfo.InitiatorPlayer;
            if (_config.zoneConfig.isCreateZonePVP && attacker != null)
            {
                if (train.playersInZone.Contains(victim) && (attacker == null || (attacker != null && train.playersInZone.Contains(attacker)))) return true;
            }
            if (hitinfo.Initiator == null) return null;
            AutoTurret autoTurret = hitinfo.Initiator as AutoTurret;
            if (autoTurret != null && train.IsTrainTurret(autoTurret.net.ID)) return true;
            return null;
        }

        object OnCustomNpcTarget(ScientistNPC npc, BasePlayer player)
        {
            if (!isEventActive || npc == null) return null;
            if (!_config.NPCConfigs.Any(x => x.name == npc.displayName)) return null;
            if (!train.IsTrainCanAttack()) return true;
            return null;
        }

        object CanPopulateLoot(ScientistNPC entity, NPCPlayerCorpse corpse)
        {
            if (!isEventActive || entity == null || corpse == null) return null;
            NpcConfig npcConfig = _config.NPCConfigs.FirstOrDefault(x => x.name == entity.displayName);
            if (npcConfig != null && npcConfig.typeLootTable != 2) return true;
            return null;
        }

        object CanPopulateLoot(LootContainer container)
        {
            if (!isEventActive || container == null) return null;
            string cratePresetName = null;

            if (container is HackableLockedCrate)
            {
                if (train.locomotive.lockedCrates.Any(y => y.Key != null && y.Key.net.ID == container.net.ID)) cratePresetName = train.locomotive.lockedCrates[container];
                else
                {
                    BaseWagon baseWagon = train.wagons.FirstOrDefault(x => x.lockedCrates.Any(y => y.Key != null && y.Key.net.ID == container.net.ID));
                    if (baseWagon != null) cratePresetName = baseWagon.lockedCrates[container];
                }
                return null;
            }

            if (train.locomotive.crates.Any(y => y.Key != null && y.Key.net.ID == container.net.ID)) cratePresetName = train.locomotive.lockedCrates[container];
            else
            {
                BaseWagon baseWagon = train.wagons.FirstOrDefault(x => x.crates.Any(y => y.Key != null && y.Key.net.ID == container.net.ID));
                if (baseWagon != null) cratePresetName = baseWagon.lockedCrates[container];
            }
            CrateConfig crateConfig = _config.crateConfigs.FirstOrDefault(x => x.presetName == cratePresetName);
            if (crateConfig != null && crateConfig.typeLootTable != 2) return true;
            return null;
        }

        object CanEntityTakeDamage(AutoTurret autoTurret, HitInfo hitinfo)
        {
            if (!isEventActive || autoTurret == null) return null;
            if (!train.IsTrainTurret(autoTurret.net.ID)) return null;
            if (!hitinfo.InitiatorPlayer.IsRealPlayer()) return false;
            train.OnTrainAttacked(hitinfo);
            return true;
        }

        object CanEntityTakeDamage(SamSite samSite, HitInfo hitinfo)
        {
            if (!isEventActive || samSite == null) return null;
            if (!train.IsTrainSamSite(samSite.net.ID)) return null;
            if (!hitinfo.InitiatorPlayer.IsRealPlayer()) return false;
            train.OnTrainAttacked(hitinfo);
            return true;
        }

        object CanEntityBeTargeted(BasePlayer player, AutoTurret turret)
        {
            if (!isEventActive || !turret.IsExists() || !player.IsExists()) return null;
            if (!train.IsTrainTurret(turret.net.ID)) return null;
            if (!player.IsRealPlayer()) return false;
            if (!train.IsTrainCanAttack()) return false;
            return true;
        }

        object OnCustomLootContainer(uint netID)
        {
            if (!isEventActive) return null;
            LootContainer lootContainer;
            lootContainer = train.locomotive.lockedCrates.Keys.FirstOrDefault(y => y != null && y.net.ID == netID);
            if (lootContainer != null)
            {
                BaseWagon baseWagon = train.wagons.FirstOrDefault(x => x.lockedCrates.Any(y => y.Key != null && y.Key.net.ID == netID));
                if (baseWagon != null) lootContainer = lootContainer = baseWagon.lockedCrates.Keys.FirstOrDefault(x => x != null && x.net.ID == netID);

                if (lootContainer == null)
                {
                    if (lootContainer == null) lootContainer = train.locomotive.crates.Keys.FirstOrDefault(y => y != null && y.net.ID == netID);
                    if (lootContainer == null)
                    {
                        BaseWagon baseWagon1 = train.wagons.FirstOrDefault(x => x.crates.Any(y => y.Key != null && y.Key.net.ID == netID));
                        if (baseWagon1 != null) lootContainer = lootContainer = baseWagon1.crates.Keys.FirstOrDefault(x => x != null && x.net.ID == netID);
                    }
                }
            }
            return CanPopulateLoot(lootContainer);
        }
        #endregion OtherPlugins
        #endregion Hooks

        #region Commands

        [ChatCommand("atrainstart")]
        void ChatStartCommand(BasePlayer player, string command, string[] arg)
        {
            if (!player.IsAdmin) return;
            if (isEventActive)
            {
                InformPlayer(player, "EventActive", _config.prefix);
                return;
            }

            if (arg != null && arg.Length >= 1) StartEvent(arg[0], player);
            else StartEvent("", player);
        }

        [ChatCommand("atrainstartunderground")]
        void ChatStarttUndergroundCommand(BasePlayer player, string command, string[] arg)
        {
            if (!player.IsAdmin) return;
            if (isEventActive)
            {
                InformPlayer(player, "EventActive", _config.prefix);
                return;
            }

            if (arg != null && arg.Length >= 1) StartEvent(arg[0], player, 100);
            else StartEvent("", player, 100);
        }

        [ChatCommand("atrainstartaboveground")]
        void ChatStarttAbovegroundCommand(BasePlayer player, string command, string[] arg)
        {
            if (!player.IsAdmin) return;
            if (isEventActive)
            {
                InformPlayer(player, "EventActive", _config.prefix);
                return;
            }

            if (arg != null && arg.Length >= 1) StartEvent(arg[0], player, 0);
            else StartEvent("", player, 0);
        }

        [ChatCommand("atrainstop")]
        void ChatStopCommand(BasePlayer player, string command, string[] arg)
        {
            if (!player.IsAdmin) return;
            if (isEventActive) EndEvent();
        }

        [ConsoleCommand("atrainstart")]
        void ConsoleStartCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null) return;
            if (arg.Args != null && arg.Args.Length > 0) StartEvent(arg.Args[0]);
            StartEvent();
            Puts(_config.prefix + " Event activated");
        }

        [ConsoleCommand("atrainstartunderground")]
        void ConsoleStartUndergroundCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null) return;
            if (arg.Args != null && arg.Args.Length > 0) StartEvent(arg.Args[0]);
            StartEvent(underGroundChance: 100);
            Puts(_config.prefix + " Event activated");
        }

        [ConsoleCommand("atrainstartaboveground")]
        void ConsoleStartAbovegroundCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null) return;
            if (arg.Args != null && arg.Args.Length > 0) StartEvent(arg.Args[0]);
            StartEvent(underGroundChance : 0);
            Puts(_config.prefix + " Event activated");
        }

        [ConsoleCommand("atrainstop")]
        void ConsoleStopCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null && isEventActive) EndEvent();
        }

        [ChatCommand("atrainpoint")]
        void SpawnPointCommand(BasePlayer player, string command, string[] arg)
        {
            if (!player.IsAdmin) return;
            if (CheckRailsInPosition(player.transform.position))
            {
                _config.mainConfig.customSpawnPoints.Add(player.transform.position.ToString());
                PrintToChat(player, _config.prefix + " New spawn point <color=#738d43>successfully</color> added");
                SaveConfig();
                return;
            }
            PrintToChat(player, _config.prefix + " <color=#ce3f27>Couldn't</color> find the rails");
        }
        #endregion Commands

        #region Actions
        void OnPlayerTryStopTrain(BasePlayer player)
        {
            train.StopTrain(player);
            train.BecomeAgressive(player);
        }

        void OnTrainAttacked(BasePlayer attacker) => train.BecomeAgressive(attacker);
        #endregion Actions

        #region Methods
        void UpdateConfig()
        {
            if (_config.versionConfig == Version) return;

            if (_config.versionConfig.Minor == 0)
            {
                if (_config.versionConfig.Patch <= 1)
                {
                    _config.mainConfig.customSpawnPoints = new List<string>();
                    _config.mainConfig.isUnderGround = true;
                }

                if (_config.versionConfig.Patch <= 3)
                {
                    foreach (NpcConfig npcConfig in _config.NPCConfigs)
                    {
                        npcConfig.kit = "";
                        foreach (NpcBelt npcBelt in npcConfig.beltItems) npcBelt.Mods = new HashSet<string>();
                    }
                }
                if (_config.versionConfig.Patch <= 5)
                {
                    _config.driverConfigs = new HashSet<DriverConfig>
                    {
                        new DriverConfig
                        {
                            name = "Train_Driver_1",
                            health = 200f,
                            wearItems = new List<NpcWear>
                            {
                                new NpcWear
                                {
                                    shortName = "metal.plate.torso",
                                    skinID = 1988476232
                                },
                                new NpcWear
                                {
                                    shortName = "riot.helmet",
                                    skinID = 1988478091
                                },
                                new NpcWear
                                {
                                    shortName = "pants",
                                    skinID = 1582399729
                                },
                                new NpcWear
                                {
                                    shortName = "tshirt",
                                    skinID = 1582403431
                                },
                                new NpcWear
                                {
                                    shortName = "shoes.boots",
                                    skinID = 0
                                }
                            },
                            kit = ""
                        }
                    };
                    foreach (LocomotiveConfig locomotiveConfig in _config.locomotiveConfigs)
                        locomotiveConfig.driverName = "Train_Driver_1";
                }
                if (_config.versionConfig.Patch <= 6)
                {
                    _config.samsiteConfigs = new HashSet<SamSiteConfig>
                    {
                        new SamSiteConfig
                        {
                            presetName = "samsite_default",
                            hp = 1000f,
                            countAmmo = 100
                        }
                    };
                    foreach (BaseWagonConfig baseWagonConfig in _config.wagonConfigs)
                    {
                        if (baseWagonConfig.prefabName == "assets/content/vehicles/trains/wagons/trainwagonc.entity.prefab")
                        {
                            baseWagonConfig.samsites = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["samsite_default"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(0, 4.216, 0)",
                                        rotation = "(0, 180, 0)"
                                    }
                                }
                            };
                        }
                        else baseWagonConfig.samsites = new Dictionary<string, HashSet<LocationConfig>>();
                    }
                    foreach (BaseWagonConfig baseWagonConfig in _config.locomotiveConfigs)
                    {
                        baseWagonConfig.samsites = new Dictionary<string, HashSet<LocationConfig>>();
                    }
                }
                if (_config.versionConfig.Patch <= 7)
                {
                    foreach (NpcConfig npcConfig in _config.NPCConfigs)
                    {
                        npcConfig.turretDamageScale = 1;
                    }
                }
                if (_config.versionConfig.Patch <= 8)
                {
                    _config.mainConfig.destroyEntities = new HashSet<string>
                    {
                        "minicopter",
                        "car"
                    };
                }
                _config.versionConfig = new VersionNumber(1, 1, 0);
                foreach (BaseWagonConfig locomotiveConfig in _config.wagonConfigs)
                {
                    if (locomotiveConfig.prefabName == "assets/content/vehicles/train/trainwagond.entity.prefab") locomotiveConfig.prefabName = "assets/content/vehicles/train/trainwagonc.entity.prefab";
                    else if (locomotiveConfig.prefabName == "assets/content/vehicles/train/trainwagonb.entity.prefab") locomotiveConfig.prefabName = "assets/content/vehicles/train/trainwagonb.entity.prefab";
                    else if (locomotiveConfig.prefabName == "assets/content/vehicles/train/trainwagonc.entity.prefab") locomotiveConfig.prefabName = "assets/content/vehicles/train/trainwagonunloadablefuel.entity.prefab";
                }
            }
            if (_config.versionConfig.Minor == 1)
            {
                if (_config.versionConfig.Patch < 3)
                {
                    foreach (BaseWagonConfig locomotiveConfig in _config.wagonConfigs)
                    {
                        if (locomotiveConfig.prefabName == "assets/content/vehicles/train/trainwagond.entity.prefab") locomotiveConfig.prefabName = "assets/content/vehicles/train/trainwagonc.entity.prefab";
                        else if (locomotiveConfig.prefabName == "assets/content/vehicles/train/trainwagonb.entity.prefab") locomotiveConfig.prefabName = "assets/content/vehicles/train/trainwagonb.entity.prefab";
                        else if (locomotiveConfig.prefabName == "assets/content/vehicles/train/trainwagonc.entity.prefab") locomotiveConfig.prefabName = "assets/content/vehicles/train/trainwagonunloadablefuel.entity.prefab";
                    }
                }
                if (_config.versionConfig.Patch < 4)
                {
                    _config.locomotiveConfigs.Add(new LocomotiveConfig
                    {
                        presetName = "locomotive_new",
                        prefabName = "assets/content/vehicles/locomotive/locomotive.entity.prefab",
                        engineForce = 500000f,
                        maxSpeed = 14,
                        driverName = "Train_Driver_1",
                        brradleys = new Dictionary<string, HashSet<LocationConfig>>
                        {
                        },
                        turrets = new Dictionary<string, HashSet<LocationConfig>>
                        {
                            ["turret_m249"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-0.554, 1.546, -8.849)",
                                        rotation = "(0, 180, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(0.554, 1.546, -8.849)",
                                        rotation = "(0, 180, 0)"
                                    }
                                }
                        },
                        NPCs = new Dictionary<string, HashSet<LocationConfig>>
                        {
                            ["TrainNPC"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-1.341, 1.546, 2)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-1.341, 1.546, 0)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-1.341, 1.546, -2)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-1.341, 1.546, -4)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-1.341, 1.546, -6)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-1.341, 1.546, -8)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(1.341, 1.546, 2)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(1.341, 1.546, 0)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(1.341, 1.546, -2)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(1.341, 1.546, -4)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(1.341, 1.546, -6)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(1.341, 1.546, -8)",
                                        rotation = "(0, 0, 0)"
                                    }
                                }
                        },
                        crates = new Dictionary<string, HashSet<LocationConfig>>
                        {
                        },
                        samsites = new Dictionary<string, HashSet<LocationConfig>>
                        {

                        },
                        decors = new Dictionary<string, HashSet<LocationConfig>>
                        {

                        }
                    });
                    _config.trainConfigs.Add(new TrainConfig
                    {
                        presetName = "train_hard_new",
                        trainName = "Giant Train",
                        isUndergroundTrain = false,
                        eventTime = 3600,
                        stopTime = 120,
                        automaticStart = false,
                        chance = 20,
                        locomotivePreset = "locomotive_new",
                        wagonsPreset = new List<string>
                        {
                            "wagon_bradley",
                            "wagon_crate_2",
                            "wagon_bradley",
                            "wagon_samsite"
                        },
                        heliPreset = "heli_1"
                    });
                }

                if (_config.versionConfig.Patch < 5)
                {
                    foreach (BaseWagonConfig wagonConfig in _config.wagonConfigs)
                    {
                        if (wagonConfig.presetName == "wagon_crate_1")
                        {
                            wagonConfig.prefabName = "assets/content/vehicles/train/trainwagonc.entity.prefab";
                        }
                    }
                }

                if (_config.versionConfig.Patch < 9)
                {
                    foreach (LocomotiveConfig locomotiveConfig in _config.locomotiveConfigs)
                    {
                        if (locomotiveConfig.prefabName == "assets/content/vehicles/workcart/workcart_aboveground.entity.prefab") locomotiveConfig.prefabName = "assets/content/vehicles/trains/workcart/workcart_aboveground.entity.prefab";
                        else if (locomotiveConfig.prefabName == "assets/content/vehicles/workcart/workcart_aboveground2.entity.prefab") locomotiveConfig.prefabName = "assets/content/vehicles/trains/workcart/workcart_aboveground2.entity.prefab";
                        else if (locomotiveConfig.prefabName == "assets/content/vehicles/locomotive/locomotive.entity.prefab") locomotiveConfig.prefabName = "assets/content/vehicles/trains/locomotive/locomotive.entity.prefab";
                    }

                    foreach (BaseWagonConfig baseWagonConfig in _config.wagonConfigs)
                    {
                        if (baseWagonConfig.prefabName == "assets/content/vehicles/train/trainwagona.entity.prefab") baseWagonConfig.prefabName = "assets/content/vehicles/trains/wagons/trainwagona.entity.prefab";
                        else if(baseWagonConfig.prefabName == "assets/content/vehicles/train/trainwagonb.entity.prefab") baseWagonConfig.prefabName = "assets/content/vehicles/trains/wagons/trainwagonb.entity.prefab";
                        else if(baseWagonConfig.prefabName == "assets/content/vehicles/train/trainwagonc.entity.prefab") baseWagonConfig.prefabName = "assets/content/vehicles/trains/wagons/trainwagonc.entity.prefab";

                        else if (baseWagonConfig.prefabName == "assets/content/vehicles/train/trainwagonunloadable.entity.prefab") baseWagonConfig.prefabName = "assets/content/vehicles/trains/wagons/trainwagonunloadable.entity.prefab";
                        else if (baseWagonConfig.prefabName == "assets/content/vehicles/train/trainwagonunloadablefuel.entity.prefab") baseWagonConfig.prefabName = "assets/content/vehicles/trains/wagons/trainwagonunloadablefuel.entity.prefab";
                        else if (baseWagonConfig.prefabName == "assets/content/vehicles/train/trainwagonunloadableloot.entity.prefab") baseWagonConfig.prefabName = "assets/content/vehicles/trains/wagons/trainwagonunloadableloot.entity.prefab";
                    }
                }
                
                if (_config.versionConfig.Patch == 9)
                {
                    _config.wagonConfigs.Add(new BaseWagonConfig
                    {
                        presetName = "caboose_wagon",
                        prefabName = "assets/content/vehicles/trains/caboose/traincaboose.entity.prefab",
                        brradleys = new Dictionary<string, HashSet<LocationConfig>>(),
                        crates = new Dictionary<string, HashSet<LocationConfig>>(),
                        samsites = new Dictionary<string, HashSet<LocationConfig>>(),
                        decors = new Dictionary<string, HashSet<LocationConfig>>(),
                        NPCs = new Dictionary<string, HashSet<LocationConfig>>(),
                        turrets = new Dictionary<string, HashSet<LocationConfig>>()
                    });

                    _config.trainConfigs.Add(new TrainConfig
                    {
                        presetName = "train_caboose",
                        trainName = "Caboose",
                        isUndergroundTrain = false,
                        eventTime = 3600,
                        stopTime = 0,
                        automaticStart = false,
                        chance = 20,
                        locomotivePreset = _config.locomotiveConfigs.FirstOrDefault(x => true).presetName,
                        wagonsPreset = new List<string>
                        {
                            "caboose_wagon"
                        },
                        heliPreset = ""
                    });
                }
            }
            _config.versionConfig = Version;
            SaveConfig();
        }

        bool CheckRailsInPosition(Vector3 position)
        {
            foreach (Collider collider in UnityEngine.Physics.OverlapSphere(position, 2f))
            {
                if (collider.name.Contains("Rail Mesh") || collider.name.Contains("train_track") || collider.name.Contains("train_tunnel")) return true;
            }
            return false;
        }

        void PostLoadCheck()
        {
            if (!plugins.Exists("NpcSpawn"))
            {
                PrintError("NpcSpawn plugin doesn`t exist! Please read the file ReadMe.txt");
                NextTick(() => Server.Command($"o.unload {Name}"));
                return;
            }
        }

        IEnumerator AutoEventCorountine()
        {
            yield return CoroutineEx.waitForSeconds(UnityEngine.Random.Range(_config.mainConfig.minTimeBetweenEvent, _config.mainConfig.maxTimeBetweenEvent));
            StartEvent();
        }

        void StartEvent(string trainPresetName = "", BasePlayer player = null, float underGroundChance = -1)
        {
            if (train != null) return;
            if (autoEventCoroutine != null) ServerMgr.Instance.StopCoroutine(autoEventCoroutine);
            FindRails(underGroundChance);
            TrainConfig trainConfig = DefineTrainConfig(trainPresetName);
            if (CanStartEvent(trainConfig, player)) startEventCoroutine = ServerMgr.Instance.StartCoroutine(StartEventCorountine(trainConfig));
        }

        void FindRails(float externalChance)
        {
            railTransformList.Clear();
            HashSet<TrainCar> trains = BaseNetworkable.serverEntities.OfType<TrainCar>().Where(x => x.IsExists() && (x.GetFuelSystem() == null || !x.GetFuelSystem().HasFuel()) && !x.AnyPlayersNearby(25));
            underGround = false;
            float undergroundChance = UnityEngine.Random.Range(0, 100);
            float chance = externalChance < 0 ? _config.mainConfig.undergroundChance : externalChance;
            if (undergroundChance <= chance)
            {
                foreach (TrainCar trainCar in trains) if (trainCar != null && trainCar.transform != null && trainCar.transform.position.y < 0) railTransformList.Add(trainCar.transform);
                underGround = true;
            }
            else
            {
                foreach (TrainCar trainCar in trains) if (trainCar != null && trainCar.transform != null && trainCar.transform.position.y > 0) railTransformList.Add(trainCar.transform);
                if (trains.Count < 3 && _config.mainConfig.isUnderGround)
                {
                    foreach (TrainCar trainCar in trains) if (trainCar.transform.position.y < 0) railTransformList.Add(trainCar.transform);
                    underGround = true;
                }
            }

            if (railTransformList.Count < 2 && (!_config.mainConfig.useCustomCoords || _config.mainConfig.customSpawnPoints.Count == 0))
            {
                PrintError("No rail detected");
                NextTick(() => Server.Command($"o.unload {Name}"));
            }
        }

        TrainConfig DefineTrainConfig(string trainPresetName = "")
        {
            TrainConfig trainConfig = null;
            if (trainPresetName == "")
            {
                if (!_config.trainConfigs.Any(x => x.chance > 0 && x.automaticStart && (!underGround || x.isUndergroundTrain))) return null;

                while (trainConfig == null)
                {
                    foreach (TrainConfig train in _config.trainConfigs)
                    {
                        if ((!underGround || train.isUndergroundTrain) && train.automaticStart && UnityEngine.Random.Range(0.0f, 100.0f) <= train.chance)
                        {
                            trainConfig = train;
                            break;
                        }
                    }
                }
            }
            else trainConfig = ins._config.trainConfigs.FirstOrDefault(x => x.presetName == trainPresetName);
            return trainConfig;
        }

        bool CanStartEvent(TrainConfig trainConfig, BasePlayer activator = null)
        {
            if (trainConfig == null)
            {
                SendErrorMessage("Train configuration not found!", activator);
                return false;
            }

            if (isEventActive || startEventCoroutine != null)
            {
                SendErrorMessage("The event has already been launched!", activator);
                return false;
            }
            return true;
        }

        IEnumerator StartEventCorountine(TrainConfig trainConfig)
        {
            if (_config.notifyConfig.chat) foreach (BasePlayer player in BasePlayer.activePlayerList) if (player != null) PrintToChat(player, GetMessage("PreStartTrain", player.UserIDString, _config.prefix, trainConfig.trainName, GetTimeMessage(player.UserIDString, _config.notifyConfig.preStartTime)));
            SendDiscordMessage("PreStartTrain", _config.prefix, trainConfig.trainName, GetTimeMessage(null, _config.notifyConfig.preStartTime));

            yield return CoroutineEx.waitForSeconds(_config.notifyConfig.preStartTime);
            GameObject gameObject = new GameObject();
            gameObject.layer = (int)Layer.Reserved1;
            train = gameObject.AddComponent<Train>();
            Subscribes();
            train.CreateTrain(trainConfig);
            startEventCoroutine = null;
        }

        private void AddToContainerItem(ItemContainer container, LootTableConfig lootTableConfig, int typeOfLootTable)
        {
            if (typeOfLootTable == 1) container.ClearItemsContainer();
            int CountLootInContainer = 0;
            int countLoot = UnityEngine.Random.Range(lootTableConfig.minAmount, lootTableConfig.maxAmount);
            container.capacity = countLoot;
            for (; CountLootInContainer <= countLoot;)
            {
                foreach (ItemConfig item in lootTableConfig.itemsConfig)
                {
                    if (UnityEngine.Random.Range(0.0f, 100.0f) <= item.chance)
                    {
                        int amount = UnityEngine.Random.Range(item.minAmount, item.maxAmount + 1);
                        Item newItem = item.isBluePrint ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(item.shortName, amount, item.skinID);
                        if (item.isBluePrint) newItem.blueprintTarget = ItemManager.FindItemDefinition(item.shortName).itemid;
                        if (item.name != "") newItem.name = item.name;
                        if (!newItem.MoveToContainer(container)) newItem.Remove();
                        if (CountLootInContainer == countLoot) return;
                        CountLootInContainer++;
                    }
                }
            }
        }

        void SendErrorMessage(string message, BasePlayer player = null)
        {
            if (player != null) PrintToChat(player, message);
            else PrintError(message);
        }

        void EndEvent(bool isUnloading = false)
        {
            if (autoEventCoroutine != null) ServerMgr.Instance.StopCoroutine(autoEventCoroutine);
            if (!isUnloading && _config.mainConfig.isAutoEvent) autoEventCoroutine = ServerMgr.Instance.StartCoroutine(AutoEventCorountine());
            Unsubscribes();
            isEventActive = false;
            if (train != null)
            {
                InformAllPlayers("EndEvent", true, _config.prefix);
                UnityEngine.Object.Destroy(train.gameObject);
                train = null;
                DefineEventWinner();
                Interface.CallHook("OnArmoredTrainEventStop");
                SendBalance();
            }
        }

        void DefineEventWinner()
        {
            var winnerPair = _playersBalance.Max(x => (float)x.Value);
            if (winnerPair.Value > 0) Interface.CallHook("OnArmoredTrainEventWin", winnerPair.Key);
        }

        #region Notify
        void InformAllPlayers(string langKey, bool chatMessahe, params object[] args)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList) if (player != null) InformPlayer(player, langKey, args);
        }

        void SendDiscordMessage(string langKey, params object[] args)
        {
            if (CanSendDiscordMessage() && _config.supportedPluginsConfig.discord.keys.Contains(langKey))
            {
                object fields = new[] { new { name = Title, value = ClearColorAndSize(GetMessage(langKey, null, args)), inline = false } };
                DiscordMessages?.Call("API_SendFancyMessage", _config.supportedPluginsConfig.discord.webhookUrl, "", _config.supportedPluginsConfig.discord.embedColor, JsonConvert.SerializeObject(fields), null, this);
            }
        }

        void InformPlayer(BasePlayer player, string langKey, params object[] args)
        {
            if (_config.notifyConfig.chat) PrintToChat(player, GetMessage(langKey, player.UserIDString, args));
            if (_config.supportedPluginsConfig.GUIAnnouncements.isGUIAnnouncements) GUIAnnouncements?.Call("CreateAnnouncement", ClearColorAndSize(GetMessage(langKey, player.UserIDString, args)), _config.supportedPluginsConfig.GUIAnnouncements.bannerColor, _config.supportedPluginsConfig.GUIAnnouncements.textColor, player, _config.supportedPluginsConfig.GUIAnnouncements.apiAdjustVPosition);
            if (_config.supportedPluginsConfig.notify.isNotify) player.SendConsoleCommand($"notify.show {_config.supportedPluginsConfig.notify.type} {ClearColorAndSize(GetMessage(langKey, player.UserIDString, args))}");
        }

        string ClearColorAndSize(string message)
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

        private bool CanSendDiscordMessage() => _config.supportedPluginsConfig.discord.isDiscord && !string.IsNullOrEmpty(_config.supportedPluginsConfig.discord.webhookUrl) && _config.supportedPluginsConfig.discord.webhookUrl != "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks";

        string GetTimeMessage(string userIDString, int seconds)
        {
            string message = "";

            TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
            if (timeSpan.Hours > 0) message += $" {timeSpan.Hours} {GetMessage("Hour", userIDString)}";
            if (timeSpan.Minutes > 0) message += $" {timeSpan.Minutes} {GetMessage("Min", userIDString)}";
            if (message == "") message += $" {timeSpan.Seconds} {GetMessage("Sec", userIDString)}";

            return message;
        }

        void Unsubscribes() { foreach (string hook in subscribeMethods) Unsubscribe(hook); }

        void Subscribes() { foreach (string hook in subscribeMethods) Subscribe(hook); }
        #endregion Notify

        #region Economy
        [PluginReference] private readonly Plugin Economics, ServerRewards, IQEconomic;

        private readonly Dictionary<ulong, double> _playersBalance = new Dictionary<ulong, double>();

        private void ActionEconomy(ulong playerId, string type, string arg = "")
        {
            switch (type)
            {
                case "Bradley":
                    AddBalance(playerId, _config.supportedPluginsConfig.economy.bradleyPoint);
                    break;
                case "Crates":
                    if (_config.supportedPluginsConfig.economy.crates.ContainsKey(arg)) AddBalance(playerId, _config.supportedPluginsConfig.economy.crates[arg]);
                    break;
                case "Npc":
                    AddBalance(playerId, _config.supportedPluginsConfig.economy.npcPoint);
                    break;
                case "LockedCrate":
                    AddBalance(playerId, _config.supportedPluginsConfig.economy.lockedCratePoint);
                    break;
                case "Turret":
                    AddBalance(playerId, _config.supportedPluginsConfig.economy.turretPoint);
                    break;
                case "Heli":
                    AddBalance(playerId, _config.supportedPluginsConfig.economy.heliPoint);
                    break;
            }
        }

        private void AddBalance(ulong playerId, double balance)
        {
            if (balance == 0) return;
            if (_playersBalance.ContainsKey(playerId)) _playersBalance[playerId] += balance;
            else _playersBalance.Add(playerId, balance);
        }

        private void SendBalance()
        {
            if (!_config.supportedPluginsConfig.economy.enable || _playersBalance.Count == 0) return;
            foreach (KeyValuePair<ulong, double> dic in _playersBalance)
            {
                if (dic.Value < _config.supportedPluginsConfig.economy.minEconomyPiont) continue;
                int intCount = Convert.ToInt32(dic.Value);
                if (_config.supportedPluginsConfig.economy.plugins.Contains("Economics") && plugins.Exists("Economics") && dic.Value > 0) Economics.Call("Deposit", dic.Key.ToString(), dic.Value);
                if (_config.supportedPluginsConfig.economy.plugins.Contains("Server Rewards") && plugins.Exists("ServerRewards") && intCount > 0) ServerRewards.Call("AddPoints", dic.Key, intCount);
                if (_config.supportedPluginsConfig.economy.plugins.Contains("IQEconomic") && plugins.Exists("IQEconomic") && intCount > 0) IQEconomic.Call("API_SET_BALANCE", dic.Key, intCount);
                BasePlayer player = BasePlayer.FindByID(dic.Key);
                if (player != null) InformPlayer(player, "SendEconomy", _config.prefix, dic.Value);
            }

            double max = 0;
            ulong winnerId = 0;
            foreach (var a in _playersBalance)
            {
                if (a.Value > max)
                {
                    max = a.Value;
                    winnerId = a.Key;
                }
            }

            if (max >= _config.supportedPluginsConfig.economy.minCommandPoint) foreach (string command in _config.supportedPluginsConfig.economy.commands) Server.Command(command.Replace("{steamid}", $"{winnerId}"));
            _playersBalance.Clear();
        }
        #endregion Economy
        #endregion Methods

        #region Classes
        class Train : FacepunchBehaviour
        {
            Coroutine controlCoroutine;
            Coroutine createTrainCorountine;
            Coroutine eventTimeCorountine;

            TrainConfig trainConfig;
            ATrainHeliHeli trainHeli = null;
            internal HeliConfig heliConfig;
            internal Locomotive locomotive;

            SphereCollider sphereCollider;
            MapMarkerGenericRadius mapmarker;
            VendingMachineMapMarker vendingMarker;

            TrainEngine locomotiveEntity;
            internal List<BaseWagon> wagons = new List<BaseWagon>();
            internal HashSet<uint> openedCrates = new HashSet<uint>();
            internal HashSet<BasePlayer> playersInZone = new HashSet<BasePlayer>();
            HashSet<BaseEntity> spheres = new HashSet<BaseEntity>();
            HashSet<ulong> pveModOwners = new HashSet<ulong>();

            int eventTime = 0;
            int stopTime = 0;
            int agressiveTime = 0;
            internal int countCrates = 0;

            internal bool reverse = false;
            bool createdZone = false;
            bool isStop = false;
            bool isAgressive = false;

            internal Vector3 lastLocomotivePoition = Vector3.zero;

            #region API
            internal bool CanLoot() => !ins._config.mainConfig.needStopTrain || (isStop && createdZone);

            internal bool IsTrainStop() => isStop;

            internal bool IsTrainHeli(uint netID) => trainHeli != null && trainHeli.baseHelicopter != null && trainHeli.baseHelicopter.net.ID == netID;

            internal bool IsTrainButton(uint netID) => locomotive != null && locomotive.stopButton.net.ID == netID;

            internal bool IsTrainWagon(uint netID) => locomotive != null && locomotive.baseWagonEntity.net.ID == netID || wagons.Any(x => x != null && x.baseWagonEntity.net.ID == netID);

            internal bool IsTrainTurret(uint netID) => locomotive != null && locomotive.turrets.Any(x => x.IsExists() && x.net.ID == netID) || wagons.Any(y => y != null && y.turrets.Any(z => z.IsExists() && z.net.ID == netID));

            internal bool IsTrainSamSite(uint netID) => locomotive != null && locomotive.samsites.Any(x => x.IsExists() && x.net.ID == netID) || wagons.Any(y => y != null && y.samsites.Any(z => z.IsExists() && z.net.ID == netID));

            internal bool IsTrainBradley(uint netID) => locomotive != null && locomotive.bradleys.Any(x => x.IsExists() && x.net.ID == netID) || wagons.Any(y => y != null && y.bradleys.Any(z => z.IsExists() && z.net.ID == netID));

            internal bool IsTrainLockedCrate(uint netID) => locomotive != null && locomotive.lockedCrates.Any(x => x.Key.IsExists() && x.Key.net.ID == netID) || wagons.Any(y => y != null && y.lockedCrates.Any(z => z.Key.IsExists() && z.Key.net.ID == netID));

            internal bool IsTrainCrate(uint netID) => locomotive != null && locomotive.lockedCrates.Any(x => x.Key.IsExists() && x.Key.net.ID == netID) || locomotive.crates.Any(x => x.Key.IsExists() && x.Key.net.ID == netID) || wagons.Any(y => y != null && (y.lockedCrates.Any(z => z.Key.IsExists() && z.Key.net.ID == netID) || y.crates.Any(z => z.Key.IsExists() && z.Key.net.ID == netID)));

            internal bool IsTrainCanAttack() => isAgressive;

            internal bool IsTrainDriver(uint netID) => locomotive != null && locomotive.driver.IsExists() && locomotive.driver.net.ID == netID;

            internal object OnTrainAttacked(HitInfo info)
            {
                if (info == null || !info.InitiatorPlayer.IsRealPlayer()) return true;
                if (!IsTrainCanBeAttacked(info)) return true;
                BecomeAgressive(info.InitiatorPlayer);
                return null;
            }

            internal bool IsTrainCanBeAttacked(HitInfo info) => true;

            internal void CheckAllCrates()
            {
                if (openedCrates.Count >= countCrates)
                {
                    eventTime = ins._config.mainConfig.killTimeTrainAfterLoot;
                    if (ins._config.notifyConfig.chat) foreach (BasePlayer player in BasePlayer.activePlayerList) if (player != null) ins.PrintToChat(player, ins.GetMessage("RemainTime", player.UserIDString, ins._config.prefix, ins.GetTimeMessage(player.UserIDString, eventTime)));
                }
            }

            internal void DiconnectTrain()
            {
                for (int index = wagons.Count - 1; index >= 0; index--)
                {
                    BaseWagon wagon = wagons[index];
                    if (wagon == null) continue;
                    wagon.baseWagonEntity.coupling.Uncouple(true);
                    wagon.baseWagonEntity.coupling.Uncouple(false);
                }
                if (locomotive == null) return;
                locomotive.baseWagonEntity.coupling.Uncouple(true);
                locomotive.baseWagonEntity.coupling.Uncouple(false);
            }
            #endregion API

            #region Build
            internal void CreateTrain(TrainConfig trainConfig)
            {
                this.trainConfig = trainConfig;
                eventTime = trainConfig.eventTime;
                isAgressive = ins._config.mainConfig.isAggressive;
                createTrainCorountine = ServerMgr.Instance.StartCoroutine(CreateTrainCorountine());
            }

            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation;
            TrainCar lastWagon = null;
            int iteration = 0;
            IEnumerator CreateTrainCorountine()
            {
            StartSpawn:
                bool failed = false;
                iteration++;
                if (iteration < 10)
                {
                    DefineSpawnPosition();

                    if (spawnPosition == Vector3.zero || !DeleteBarriersInPosition(spawnPosition) || !CreateLocomotive(spawnPosition, spawnRotation)) goto StartSpawn;
                    yield return CoroutineEx.waitForSeconds(0.5f);
                    if (!lastWagon.IsExists() || locomotive == null) goto StartSpawn;
                    else
                    {
                        locomotive.CreateDriver();
                        locomotive.trainEngine.SetThrottle(TrainEngine.EngineSpeeds.Fwd_Hi);
                    }

                    foreach (string wagonPresetName in trainConfig.wagonsPreset)
                    {
                        while (lastWagon.IsExists() && Vector3.Distance(lastWagon.transform.position, spawnPosition) < 20)
                        {
                            yield return CoroutineEx.waitForSeconds(1f);
                            DeleteBarriersInPosition(spawnPosition);
                        }
                        if (lastWagon.IsExists()) CreateWagon(wagonPresetName);
                        else
                        {
                            OnDestroy();
                            CreateTrain(trainConfig);
                            failed = true;
                            break;
                        }
                    }

                    yield return CoroutineEx.waitForSeconds(1f);
                    if (!failed)
                    {
                        if (PostSpawnCheck()) PostSpawnUpdate();
                        else
                        {
                            yield return CoroutineEx.waitForSeconds(0.5f);
                            OnDestroy();
                            CreateTrain(trainConfig);
                        }
                    }
                }
                else
                {
                    ins.PrintError("Failed to spawn the train!");
                    ins.EndEvent();
                }
            }

            void DefineSpawnPosition()
            {
                if (ins._config.mainConfig.customSpawnPoints.Count > 0 && ins._config.mainConfig.useCustomCoords)
                {
                    string spawnPositionString = ins._config.mainConfig.customSpawnPoints.GetRandom();
                    spawnPosition = spawnPositionString.ToVector3();
                    spawnRotation = Quaternion.identity;
                    if (!ins.CheckRailsInPosition(spawnPosition))
                    {
                        spawnPosition = Vector3.zero;
                        ins._config.mainConfig.customSpawnPoints.Remove(spawnPositionString);
                        ins.SaveConfig();
                    }
                }
                if (ins.railTransformList.Count > 0 && spawnPosition == Vector3.zero)
                {
                    Transform randomTransform = ins.railTransformList.GetRandom();
                    if (randomTransform == null) return;
                    spawnPosition = randomTransform.position;
                    spawnRotation = randomTransform.rotation;
                }
            }

            bool CreateLocomotive(Vector3 position, Quaternion rotation)
            {
                LocomotiveConfig locomotiveConfig = ins._config.locomotiveConfigs.FirstOrDefault(x => x.presetName == trainConfig.locomotivePreset);
                if (locomotiveConfig == null)
                {
                    ins.PrintError("Locomotive configuration not found!");
                    ins.EndEvent();
                    return false;
                }
                locomotiveEntity = GameManager.server.CreateEntity(locomotiveConfig.prefabName, position, rotation) as TrainEngine;
                locomotiveEntity.skinID = 1114526;
                locomotiveEntity.Spawn();
                locomotiveEntity.SetFlag(BaseEntity.Flags.Reserved9, true);
                if (!locomotiveEntity.IsExists()) return false;
                locomotiveEntity.enableSaving = false;
                locomotiveEntity.GetFuelSystem().cachedHasFuel = true;
                locomotiveEntity.GetFuelSystem().nextFuelCheckTime = float.MaxValue;
                locomotiveEntity.engineForce = locomotiveConfig.engineForce;
                locomotiveEntity.maxSpeed = locomotiveConfig.maxSpeed;
                locomotive = locomotiveEntity.gameObject.AddComponent<Locomotive>();
                locomotive.OnCreateLocomotive(locomotiveEntity, locomotiveConfig);
                locomotiveEntity.engineForce = 500000;
                locomotiveEntity.maxSpeed = 2;
                lastWagon = locomotiveEntity;
                return true;
            }

            bool CreateWagon(string wagonPresetName)
            {
                CheackSpace(lastWagon, true);
                BaseWagonConfig wagonConfig = ins._config.wagonConfigs.FirstOrDefault(x => x.presetName == wagonPresetName);
                if (wagonConfig == null)
                {
                    ins.PrintError("Wagon configuration not found!");
                    ins.EndEvent();
                    return false;
                }
                TrainCar wagonEntity = GameManager.server.CreateEntity(wagonConfig.prefabName, spawnPosition, spawnRotation == Quaternion.identity ? lastWagon.transform.rotation : spawnRotation) as TrainCar;
                wagonEntity.skinID = 1114526;
                wagonEntity.Spawn();
                wagonEntity.enableSaving = false;
                if (!wagonEntity.IsExists()) return false;
                lastWagon.coupling.Uncouple(false);

                wagonEntity.coupling.frontCoupling.TryCouple(lastWagon.coupling.rearCoupling, false);
                lastWagon.coupling.rearCoupling.TryCouple(wagonEntity.coupling.frontCoupling, false);
                BaseWagon wagon = wagonEntity.gameObject.AddComponent<BaseWagon>();
                wagon.Init(wagonEntity, wagonConfig);
                wagons.Add(wagon);
                lastWagon = wagonEntity;
                return true;
            }

            bool DeleteBarriersInPosition(Vector3 position)
            {
                foreach (Collider collider in UnityEngine.Physics.OverlapSphere(position, 12f))
                {
                    BaseEntity entity = collider.ToBaseEntity();
                    if (!entity.IsExists() || entity.skinID != 0) continue;
                    TrainCar trainCar = entity as TrainCar;
                    if (trainCar == null || IsTrainWagon(trainCar.net.ID)) continue;
                    if (trainCar.GetFuelSystem() != null && trainCar.GetFuelSystem().cachedHasFuel) return false;
                    trainCar.Kill();
                }
                return true;
            }

            void CreateHelicopter()
            {
                if (trainConfig.heliPreset == "") return;
                heliConfig = ins._config.heliConfigs.FirstOrDefault(x => x.presetName == trainConfig.heliPreset);
                if (heliConfig == null)
                {
                    ins.PrintError("Heli configuration not found!");
                    return;
                }
                Vector3 position = locomotive.baseWagonEntity.transform.position + new Vector3(0, heliConfig.height, 0);
                BaseHelicopter heli = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", position, locomotive.baseWagonEntity.transform.rotation) as BaseHelicopter;
                heli.enableSaving = false;
                heli.Spawn();
                heli.transform.position = position;
                trainHeli = heli.gameObject.AddComponent<ATrainHeliHeli>();
                trainHeli.InitHelicopter(heli, heliConfig);
            }

            bool PostSpawnCheck()
            {
                bool newLocomotive = locomotive.baseWagonEntity.PrefabName.Contains("locomotive.entity");
                if (!wagons.Any(x => x != null && Vector3.Distance(x.baseWagonEntity.transform.position, locomotive.baseWagonEntity.transform.position) < 11f) && ((!newLocomotive && Vector3.Distance(wagons[0].baseWagonEntity.transform.position, locomotive.baseWagonEntity.transform.position) < 15) || newLocomotive && Vector3.Distance(wagons[0].baseWagonEntity.transform.position, locomotive.baseWagonEntity.transform.position) < 19))
                {
                    for (int i = 0; i <= wagons.Count - 2; i++)
                    {
                        BaseWagon baseWagon1 = wagons[i];
                        BaseWagon baseWagon2 = wagons[i + 1];
                        if (baseWagon1 == null || baseWagon2 == null) return false;
                        float distance = Vector3.Distance(baseWagon1.baseWagonEntity.transform.position, baseWagon2.baseWagonEntity.transform.position);
                        if (distance < 15 || distance > 17) return false;
                    }
                    return true;
                }
                return false;
            }

            void CheackSpace(TrainCar trainCar, bool rear)
            {
                if (trainCar == null) return;
                RaycastHit raycastHit;
                Vector3 position = rear ? trainCar.rearCoupling.position : trainCar.frontCoupling.position;
                Vector3 direction = rear ? -trainCar.transform.forward.normalized : trainCar.transform.forward.normalized;
                if (Physics.Raycast(position - trainCar.transform.up * 0.5f, direction, out raycastHit, 10f)) CheckRayCast(raycastHit);
                if (Physics.Raycast(position + trainCar.transform.right * 0.5f - trainCar.transform.up * 0.5f, direction, out raycastHit, 10f)) CheckRayCast(raycastHit);
                if (Physics.Raycast(position - trainCar.transform.right * 0.5f - trainCar.transform.up * 0.5f, direction, out raycastHit, 10f)) CheckRayCast(raycastHit);
            }

            void CheckRayCast(RaycastHit raycastHit)
            {
                BaseEntity barrierEintity = raycastHit.GetEntity();
                if (!barrierEintity.IsExists()) return;
                TrainCar traincar = barrierEintity as TrainCar;
                if (ins._config.mainConfig.destrroyWagons && traincar.IsExists() && !IsTrainWagon(traincar.net.ID))
                {
                    barrierEintity.Kill(BaseNetworkable.DestroyMode.Gib);
                }
                else if (ins._config.mainConfig.destroyEntities.Any(x => barrierEintity.ShortPrefabName.Contains(x)))
                {
                    barrierEintity.Kill(BaseNetworkable.DestroyMode.Gib);
                }
            }

            void PostSpawnUpdate()
            {
                SpawnMarkers();

                locomotive.DelayBuild();
                foreach (BaseWagon wagon in wagons) wagon.DelayBuild();
                ins.isEventActive = true;

                if (!ins.underGround) CreateHelicopter();
                Interface.CallHook("OnArmoredTrainEventStart");
                eventTimeCorountine = ServerMgr.Instance.StartCoroutine(EventTimeCorountine());
                controlCoroutine = ServerMgr.Instance.StartCoroutine(ControlCorountine());
                StartTrain();
            }

            void SpawnMarkers()
            {
                if (!ins._config.markerConfig.enable) return;
                SpawnMapMarker();
                SpawnVendingMarker();
            }

            void SpawnMapMarker()
            {
                mapmarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", locomotive.transform.position) as MapMarkerGenericRadius;
                mapmarker.enableSaving = false;
                mapmarker.Spawn();
                mapmarker.radius = ins._config.markerConfig.radius;
                mapmarker.alpha = ins._config.markerConfig.alpha;
                mapmarker.color1 = new Color(ins._config.markerConfig.color1.r, ins._config.markerConfig.color1.g, ins._config.markerConfig.color1.b);
                mapmarker.color2 = new Color(ins._config.markerConfig.color2.r, ins._config.markerConfig.color2.g, ins._config.markerConfig.color2.b);
            }

            void SpawnVendingMarker()
            {
                vendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", locomotive.transform.position) as VendingMachineMapMarker;
                vendingMarker.Spawn();
                vendingMarker.markerShopName = "ArmoredTrain";
                InvokeRepeating(UpdateMapMarker, 0, 1f);
            }

            void UpdateMapMarker()
            {
                Vector3 position = locomotive.transform.position;

                if (position == null)
                {
                    if (mapmarker.IsExists()) mapmarker.Kill();
                    if (vendingMarker.IsExists()) vendingMarker.Kill();
                    CancelInvoke(UpdateMapMarker);
                    return;
                }

                mapmarker.transform.position = position;
                mapmarker.SendUpdate();
                mapmarker.SendNetworkUpdate();

                vendingMarker.transform.position = position;
                vendingMarker.markerShopName = trainConfig.trainName + " " + ins.GetTimeMessage(null, eventTime);
                vendingMarker.SendNetworkUpdate();
            }
            #endregion Build

            #region Control
            void StartTrain(bool changeDirection = false)
            {
                if (!locomotive.driver.IsExists())
                {
                    if (ins._config.mainConfig.reviveTrainDriver) locomotive.CreateDriver();
                    else return;
                }
                if (changeDirection) reverse = !reverse;
                if (!reverse) locomotive.trainEngine.SetThrottle(TrainEngine.EngineSpeeds.Fwd_Hi);
                else locomotive.trainEngine.SetThrottle(TrainEngine.EngineSpeeds.Rev_Hi);
                stopTime = 0;
                isStop = false;
                DestroyZone();
                if (!changeDirection) ins.InformAllPlayers("StartTrain", ins._config.notifyConfig.chat, ins._config.prefix, trainConfig.trainName);
                locomotive.CreateEmergencyCord(isStop);
            }

            internal void StopTrain(BasePlayer player)
            {
                if (isStop) return;
                isStop = true;
                stopTime = trainConfig.stopTime;
                locomotive.trainEngine.SetThrottle(TrainEngine.EngineSpeeds.Zero);
                if (player != null) ins.InformAllPlayers("PlayerStopTrain", true, ins._config.prefix, player.displayName);
                locomotive.CreateEmergencyCord(isStop);
            }

            internal void BecomeAgressive(BasePlayer player)
            {
                agressiveTime = ins._config.mainConfig.agressiveTime;
                if (ins._config.mainConfig.isAggressive) return;
                if (!isAgressive)
                {
                    foreach (BradleyAPC bradleyAPC in locomotive.bradleys) if (bradleyAPC.IsExists()) bradleyAPC.DoAI = true;

                    foreach (BaseWagon baseWagon in wagons)
                    {
                        foreach (BradleyAPC bradleyAPC in baseWagon.bradleys) if (bradleyAPC.IsExists()) bradleyAPC.DoAI = true;
                    }

                    if (trainHeli != null) trainHeli.SetTarget(player);
                }
                isAgressive = true;
            }

            void BecomeNoAgressive()
            {
                isAgressive = false;
                agressiveTime = 0;
                if (ins._config.mainConfig.isAggressive) return;

                foreach (BradleyAPC bradleyAPC in locomotive.bradleys) if (bradleyAPC.IsExists()) bradleyAPC.DoAI = false;

                foreach (BaseWagon baseWagon in wagons)
                {
                    foreach (BradleyAPC bradleyAPC in baseWagon.bradleys) if (bradleyAPC.IsExists()) bradleyAPC.DoAI = false;
                }
            }
            #endregion Control

            #region Updates
            IEnumerator EventTimeCorountine()
            {
                while (eventTime > 0)
                {
                    eventTime--;
                    yield return CoroutineEx.waitForSeconds(1);
                }
                ins.EndEvent();
            }

            IEnumerator ControlCorountine()
            {
                while (eventTime > 0)
                {
                    SendRemainTimeMessage();
                    TrainStartStopControl();
                    TrainAgressiveControl();
                    DestroyFronEntities();
                    UpdateEntitiesContainers();
                    PlayerGui();
                    lastLocomotivePoition = locomotive.transform.position;
                    yield return CoroutineEx.waitForSeconds(1);
                }
            }

            void SendRemainTimeMessage()
            {
                if (ins._config.notifyConfig.chat && ins._config.notifyConfig.timeNotifications.Contains(eventTime)) foreach (BasePlayer player in BasePlayer.activePlayerList) if (player != null) ins.PrintToChat(player, ins.GetMessage("RemainTime", player.UserIDString, ins._config.prefix, ins.GetTimeMessage(player.UserIDString, eventTime)));
            }

            void TrainStartStopControl()
            {
                if (isStop)
                {
                    stopTime--;
                    if (stopTime <= 0) StartTrain();
                    else if (!createdZone && Vector3.Distance(lastLocomotivePoition, locomotive.baseWagonEntity.transform.position) < 1f) CreateZone();
                }
                else if (Vector3.Distance(lastLocomotivePoition, locomotive.baseWagonEntity.transform.position) < 0.25f) StartTrain(true);
            }

            void TrainAgressiveControl()
            {
                if (!ins._config.mainConfig.isAggressive && isAgressive)
                {
                    agressiveTime--;
                    if (agressiveTime <= 0) BecomeNoAgressive();
                }
            }

            void DestroyFronEntities()
            {
                if (ins._config.mainConfig.destrroyWagons || ins._config.mainConfig.destroyEntities.Count > 0)
                {
                    if (reverse) CheackSpace(lastWagon, true);
                    else CheackSpace(locomotive.baseWagonEntity, false);
                }
            }

            void UpdateEntitiesContainers()
            {
                locomotive.stopButton.SendNetworkUpdateImmediate();
                if (locomotive != null)
                {
                    foreach (LootContainer lootContainer in locomotive.lockedCrates.Keys) if (lootContainer.IsExists()) lootContainer.SendNetworkUpdate();
                }
                foreach (BaseWagon baseWagon in wagons)
                {
                    if (baseWagon == null) continue;
                    foreach (LootContainer lootContainer in baseWagon.lockedCrates.Keys) if (lootContainer.IsExists()) lootContainer.SendNetworkUpdate();
                }
            }

            void PlayerGui()
            {
                if (!isStop) return;
                foreach (BasePlayer player in playersInZone) if (player != null) MessageGUI(player, ins.GetMessage("GUI", player.UserIDString, ins.GetTimeMessage(player.UserIDString, eventTime)));
            }
            #endregion Updates

            #region Zone
            void CreateZone()
            {
                Vector3 position = (locomotive.transform.position + lastWagon.transform.position) / 2;

                createdZone = true;
                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = ins._config.zoneConfig.radius;
                sphereCollider.transform.position = position;
                if (ins._config.supportedPluginsConfig.pveMode.pve)
                {
                    JObject config = new JObject
                    {
                        ["Damage"] = ins._config.supportedPluginsConfig.pveMode.damage,
                        ["ScaleDamage"] = new JArray { ins._config.supportedPluginsConfig.pveMode.scaleDamage.Select(x => new JObject { ["Type"] = x.Type, ["Scale"] = x.Scale }) },
                        ["LootCrate"] = ins._config.supportedPluginsConfig.pveMode.lootCrate,
                        ["HackCrate"] = ins._config.supportedPluginsConfig.pveMode.hackCrate,
                        ["LootNpc"] = ins._config.supportedPluginsConfig.pveMode.lootNpc,
                        ["DamageNpc"] = ins._config.supportedPluginsConfig.pveMode.damageNpc,
                        ["DamageTank"] = ins._config.supportedPluginsConfig.pveMode.damageTank,
                        ["TargetNpc"] = ins._config.supportedPluginsConfig.pveMode.targetNpc,
                        ["TargetTank"] = ins._config.supportedPluginsConfig.pveMode.targetTank,
                        ["CanEnter"] = ins._config.supportedPluginsConfig.pveMode.canEnter,
                        ["CanEnterCooldownPlayer"] = ins._config.supportedPluginsConfig.pveMode.canEnterCooldownPlayer,
                        ["TimeExitOwner"] = ins._config.supportedPluginsConfig.pveMode.timeExitOwner,
                        ["AlertTime"] = ins._config.supportedPluginsConfig.pveMode.alertTime,
                        ["RestoreUponDeath"] = ins._config.supportedPluginsConfig.pveMode.restoreUponDeath,
                        ["CooldownOwner"] = ins._config.supportedPluginsConfig.pveMode.cooldownOwner,
                        ["Darkening"] = ins._config.supportedPluginsConfig.pveMode.darkening
                    };
                    HashSet<uint> npcs = new HashSet<uint>();
                    HashSet<uint> bradleys = new HashSet<uint>();
                    HashSet<uint> crates = new HashSet<uint>();

                    foreach (ScientistNPC scientistNPC in locomotive.npcs) if (scientistNPC.IsExists()) npcs.Add(scientistNPC.net.ID);
                    foreach (LootContainer lootContainer in locomotive.crates.Keys) if (lootContainer.IsExists()) crates.Add(lootContainer.net.ID);
                    foreach (LootContainer lootContainer in locomotive.lockedCrates.Keys) if (lootContainer.IsExists()) crates.Add(lootContainer.net.ID);
                    foreach (BradleyAPC bradleyAPC in locomotive.bradleys) if (bradleyAPC.IsExists()) bradleys.Add(bradleyAPC.net.ID);

                    foreach (BaseWagon baseWagon in wagons)
                    {
                        if (baseWagon == null) continue;
                        foreach (ScientistNPC scientistNPC in baseWagon.npcs) if (scientistNPC.IsExists()) npcs.Add(scientistNPC.net.ID);
                        foreach (LootContainer lootContainer in baseWagon.crates.Keys) if (lootContainer.IsExists()) crates.Add(lootContainer.net.ID);
                        foreach (LootContainer lootContainer in baseWagon.lockedCrates.Keys) if (lootContainer.IsExists()) crates.Add(lootContainer.net.ID);
                        foreach (BradleyAPC bradleyAPC in baseWagon.bradleys) if (bradleyAPC.IsExists()) bradleys.Add(bradleyAPC.net.ID);
                    }
                    ins.PveMode.Call("EventAddPveMode", ins.Name, config, position, ins._config.zoneConfig.radius, crates, npcs, bradleys, pveModOwners, null);
                }
                else if (ins._config.zoneConfig.isDome) CreateSphere();
            }

            void CreateSphere()
            {
                for (int i = 0; i < ins._config.zoneConfig.darkening; i++)
                {
                    BaseEntity sphere = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", sphereCollider.transform.position);
                    SphereEntity entity = sphere as SphereEntity;
                    entity.currentRadius = ins._config.zoneConfig.radius * 2;
                    entity.lerpSpeed = 0f;
                    sphere.enableSaving = false;
                    sphere.Spawn();
                    spheres.Add(sphere);
                }
            }

            void OnTriggerEnter(Collider other)
            {
                if (other.ToBaseEntity() == null) return;
                BasePlayer player = other.ToBaseEntity() as BasePlayer;
                if (player != null && player.userID.IsSteamId())
                {
                    playersInZone.Add(player);
                    if (ins._config.guiConfig.IsGUI) MessageGUI(player, ins.GetMessage("GUI", player.UserIDString, ins.GetTimeMessage(player.UserIDString, eventTime)));
                    if (ins._config.zoneConfig.isCreateZonePVP) ins.InformPlayer(player, "EnterPVP", ins._config.prefix);
                }
            }

            void OnTriggerExit(Collider other)
            {
                if (other.ToBaseEntity() == null) return;
                BasePlayer player = other.ToBaseEntity() as BasePlayer;
                if (player != null && player.userID.IsSteamId())
                {
                    playersInZone.Remove(player);
                    if (ins._config.guiConfig.IsGUI) CuiHelper.DestroyUi(player, "TrainGui");
                    if (ins._config.zoneConfig.isCreateZonePVP) ins.InformPlayer(player, "ExitPVP", ins._config.prefix);
                }
            }

            void DestroyZone()
            {
                if (ins.isEventActive && sphereCollider != null) UnityEngine.GameObject.DestroyImmediate(sphereCollider);
                foreach (BaseEntity sphere in spheres) if (sphere.IsExists()) sphere.Kill();
                foreach (BasePlayer player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, "TrainGui");
                createdZone = false;
                playersInZone.Clear();

                if (ins._config.supportedPluginsConfig.pveMode.pve && ins.plugins.Exists("PveMode"))
                {
                    if (ins.isEventActive)
                    {
                        pveModOwners = (HashSet<ulong>)ins.PveMode.Call("GetEventOwners", ins.Name);
                        if (pveModOwners == null) pveModOwners = new HashSet<ulong>();
                    }
                    ins.PveMode.Call("EventRemovePveMode", ins.Name, false);
                }
            }

            void MessageGUI(BasePlayer player, string text)
            {
                CuiHelper.DestroyUi(player, "TrainGui");

                CuiElementContainer container = new CuiElementContainer();

                container.Add(new CuiPanel
                {
                    Image = { Color = "0 0 0 0" },
                    RectTransform = { AnchorMin = ins._config.guiConfig.AnchorMin, AnchorMax = ins._config.guiConfig.AnchorMax },
                    CursorEnabled = false,
                }, "Hud", "TrainGui");
                container.Add(new CuiElement
                {
                    Parent = "TrainGui",
                    Components =
                {
                    new CuiTextComponent() { Color = "1 1 1 1", FadeIn = 0f, Text = text, FontSize = 24, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" },
                    new CuiOutlineComponent { Distance = "1 1", Color = "0 0 0 1" },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" }
                }
                });

                CuiHelper.AddUi(player, container);
            }
            #endregion Zone

            void OnDestroy()
            {
                CancelInvoke(UpdateMapMarker);
                if (controlCoroutine != null) ServerMgr.Instance.StopCoroutine(controlCoroutine);
                if (createTrainCorountine != null) ServerMgr.Instance.StopCoroutine(createTrainCorountine);
                if (eventTimeCorountine != null) ServerMgr.Instance.StopCoroutine(eventTimeCorountine);
                if (mapmarker.IsExists()) mapmarker.Kill();
                if (vendingMarker.IsExists()) vendingMarker.Kill();
                if (trainHeli != null && trainHeli.baseHelicopter.IsExists()) trainHeli.baseHelicopter.Kill();
                DiconnectTrain();
                for (int index = wagons.Count - 1; index >= 0; index--)
                {
                    BaseWagon wagon = wagons[index];
                    if (wagon != null && wagon.baseWagonEntity.IsExists()) wagon.DestroyWagon();
                }
                if (locomotive != null && locomotive.baseWagonEntity.IsExists()) locomotive.DestroyWagon();
                locomotive = null;
                lastWagon = null;
                wagons.Clear();
                DestroyZone();
            }
        }

        class Locomotive : BaseWagon
        {
            internal TrainEngine trainEngine;
            internal BasePlayer driver;
            internal LocomotiveConfig locomotiveConfig;
            internal PressButton stopButton;
            internal BaseEntity hammer;

            #region Build
            internal void OnCreateLocomotive(TrainEngine locomotiveEntity, LocomotiveConfig locomotiveConfig)
            {
                Init(locomotiveEntity, locomotiveConfig);
                trainEngine = baseWagonEntity as TrainEngine;
                if (trainEngine == null)
                {
                    ins.PrintError("Locomotive is not locomotive!");
                    ins.EndEvent();
                    return;
                }
                this.locomotiveConfig = locomotiveConfig;
            }

            public override void DelayBuild()
            {
                base.DelayBuild();
                CreateStopButton();
                trainEngine.engineForce = locomotiveConfig.engineForce;
                trainEngine.maxSpeed = locomotiveConfig.maxSpeed;
            }

            internal void CreateDriver()
            {
                trainEngine.DismountAllPlayers();
                DriverConfig npcConfig = ins._config.driverConfigs.FirstOrDefault(x => x.name == locomotiveConfig.driverName);
                if (npcConfig == null)
                {
                    ins.PrintError("The driver preset was not found!");
                    return;
                }

                HashSet<string> states = new HashSet<string> { "IdleState", "CombatStationaryState" };
                JObject config = new JObject
                {
                    ["Name"] = npcConfig.name,
                    ["WearItems"] = new JArray { npcConfig.wearItems.Select(x => new JObject { ["ShortName"] = x.shortName, ["SkinID"] = x.skinID }) },
                    ["BeltItems"] = new JArray(),
                    ["Kit"] = npcConfig.kit,
                    ["Health"] = npcConfig.health,
                    ["RoamRange"] = 100,
                    ["ChaseRange"] = 100,
                    ["DamageScale"] = 1,
                    ["TurretDamageScale"] = 1f,
                    ["AimConeScale"] = 10,
                    ["DisableRadio"] = true,
                    ["CanUseWeaponMounted"] = false,
                    ["CanRunAwayWater"] = false,
                    ["Speed"] = 0,
                    ["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = string.Empty,
                    ["States"] = new JArray { states },
                    ["Sensory"] = new JObject
                    {
                        ["AttackRangeMultiplier"] = 1,
                        ["SenseRange"] = 100,
                        ["MemoryDuration"] = 10,
                        ["CheckVisionCone"] = false,
                        ["VisionCone"] = 120
                    }
                };

                driver = (ScientistNPC)ins.NpcSpawn.Call("SpawnNpc", baseWagonEntity.transform.position, config);
                trainEngine.mountPoints[0].mountable.AttemptMount(driver, false);
                trainEngine.engineController.TryStartEngine(driver);
                if (!ins.underGround) return;
                TrainDriver trainDriver = driver.gameObject.AddComponent<TrainDriver>();
                trainDriver.InitDriver(driver, trainEngine);
            }

            void CreateStopButton()
            {
                stopButton = CreateEntity("assets/prefabs/deployable/playerioents/button/button.prefab", locomotiveConfig.prefabName == "assets/content/vehicles/trains/locomotive/locomotive.entity.prefab" ? new LocationConfig { position = "(0, 1.599, -7.985)", rotation = "(0, 180, 0)" } : new LocationConfig { position = "(0.076, 1.599, 1.84)", rotation = "(0, 180, 0)" }) as PressButton;
            }

            internal void CreateEmergencyCord(bool stop)
            {
                if (hammer.IsExists()) hammer.Kill();

                LocationConfig locationConfig = new LocationConfig();
                if (locomotiveConfig.prefabName == "assets/content/vehicles/trains/locomotive/locomotive.entity.prefab")
                {
                    if (stop) locationConfig = new LocationConfig { position = "(-0.344, 2.817, -8.245)", rotation = "(7.212, 258.156, 167.291)" };
                    else locationConfig = new LocationConfig { position = "(-0.007, 3.180, -8.245)", rotation = "(283.5, 209.277, 228.833)" };
                }
                else
                {
                    if (stop) locationConfig = new LocationConfig { position = "(-0.268, 2.817, 1.580)", rotation = "(7.212, 258.156, 167.291)" };
                    else locationConfig = new LocationConfig { position = "(0.069, 3.180, 1.580)", rotation = "(283.5, 209.277, 228.833)" };
                }

                hammer = CreateEntity("assets/prefabs/weapons/salvaged_hammer/hammer_salvaged.entity.prefab", locationConfig, true);
            }

            internal override void DestroyWagon()
            {
                trainEngine.DismountAllPlayers();
                if (driver.IsExists()) driver.Kill();
                if (stopButton.IsExists()) stopButton.Kill();
                if (hammer.IsExists()) hammer.Kill();
                base.DestroyWagon();
            }

            void OnDestroy()
            {
                if (driver.IsExists()) driver.Kill();
            }
            #endregion Build
        }

        class BaseWagon : FacepunchBehaviour
        {
            internal TrainCar baseWagonEntity;
            internal BaseWagonConfig baseWagonConfig;
            internal HashSet<BradleyAPC> bradleys = new HashSet<BradleyAPC>();
            internal HashSet<AutoTurret> turrets = new HashSet<AutoTurret>();
            internal HashSet<ScientistNPC> npcs = new HashSet<ScientistNPC>();
            internal HashSet<ScientistNPC> roamnpcs = new HashSet<ScientistNPC>();
            internal HashSet<BaseEntity> decorEntities = new HashSet<BaseEntity>();
            internal HashSet<SamSite> samsites = new HashSet<SamSite>();
            internal Dictionary<LootContainer, string> crates = new Dictionary<LootContainer, string>();
            internal Dictionary<LootContainer, string> lockedCrates = new Dictionary<LootContainer, string>();

            internal void Init(TrainCar baseWagonEntity, BaseWagonConfig baseWagonConfig)
            {
                this.baseWagonEntity = baseWagonEntity;
                this.baseWagonConfig = baseWagonConfig;
            }

            public virtual void DelayBuild()
            {
                foreach (var bradleyPair in baseWagonConfig.brradleys)
                {
                    BradleyConfig bradleyConfig = ins._config.bradleysConfigs.FirstOrDefault(x => x.presetName == bradleyPair.Key);
                    if (bradleyConfig == null)
                    {
                        ins.PrintError("Bradley configuration not found!");
                        ins.EndEvent();
                        return;
                    }
                    foreach (LocationConfig locationConfig in bradleyPair.Value) CreateBradley(bradleyConfig, locationConfig);
                }
                foreach (var turretPair in baseWagonConfig.turrets)
                {
                    TurretConfig turretConfig = ins._config.turretConfigs.FirstOrDefault(x => x.presetName == turretPair.Key);
                    if (turretConfig == null)
                    {
                        ins.PrintError("Turret configuration not found!");
                        ins.EndEvent();
                        return;
                    }
                    foreach (LocationConfig locationConfig in turretPair.Value) CreateTurret(turretConfig, locationConfig);
                }
                foreach (var npcPair in baseWagonConfig.NPCs)
                {
                    NpcConfig npcConfig = ins._config.NPCConfigs.FirstOrDefault(x => x.name == npcPair.Key);
                    if (npcConfig == null)
                    {
                        ins.PrintError("NPC configuration not found!");
                        ins.EndEvent();
                        return;
                    }
                    foreach (LocationConfig locationConfig in npcPair.Value) CreateNpc(npcConfig, locationConfig);
                }
                foreach (var cratePair in baseWagonConfig.crates)
                {
                    CrateConfig crateConfig = ins._config.crateConfigs.FirstOrDefault(x => x.presetName == cratePair.Key);
                    if (crateConfig == null)
                    {
                        ins.PrintError("Crate configuration not found!");
                        ins.EndEvent();
                        return;
                    }
                    foreach (LocationConfig locationConfig in cratePair.Value) CreateCrate(crateConfig, locationConfig);
                }
                foreach (var decorPair in baseWagonConfig.decors)
                {
                    foreach (LocationConfig locationConfig in decorPair.Value) CreateDecor(decorPair.Key, locationConfig);
                }
                foreach (var samsitePair in baseWagonConfig.samsites)
                {
                    SamSiteConfig samSiteConfig = ins._config.samsiteConfigs.FirstOrDefault(x => x.presetName == samsitePair.Key);
                    if (samSiteConfig == null)
                    {
                        ins.PrintError("SamSite configuration not found!");
                        ins.EndEvent();
                        return;
                    }
                    foreach (LocationConfig locationConfig in samsitePair.Value) CreateSamsite(samSiteConfig, locationConfig);
                }

                foreach (TriggerTrainCollisions Trigger in baseWagonEntity.GetComponentsInChildren<TriggerTrainCollisions>())
                {
                    Trigger.triggerCollider.gameObject.layer = 18;
                }
            }

            void CreateTurret(TurretConfig turretConfig, LocationConfig locationConfig)
            {
                AutoTurret autoTurret = CreateEntity("assets/prefabs/npc/autoturret/autoturret_deployed.prefab", locationConfig) as AutoTurret;
                ContainerIOEntity containerIO = autoTurret.GetComponent<ContainerIOEntity>();
                containerIO.inventory.Insert(ItemManager.CreateByName(turretConfig.shortNameWeapon));
                containerIO.inventory.Insert(ItemManager.CreateByName(turretConfig.shortNameAmmo, turretConfig.countAmmo));
                containerIO.SendNetworkUpdate();
                autoTurret.InitializeHealth(turretConfig.hp, turretConfig.hp);
                autoTurret.UpdateFromInput(10, 0);
                autoTurret.isLootable = false;
                autoTurret.dropFloats = false;
                autoTurret.dropsLoot = false;
                turrets.Add(autoTurret);
            }

            void CreateBradley(BradleyConfig bradleyConfig, LocationConfig locationConfig)
            {
                BradleyAPC bradley = CreateEntity("assets/prefabs/npc/m2bradley/bradleyapc.prefab", locationConfig) as BradleyAPC;
                bradley.InstallPatrolPath(new BasePath());
                bradley.skinID = 755446;
                bradley.DoAI = ins._config.mainConfig.isAggressive;
                bradley._maxHealth = bradleyConfig.hp;
                bradley.health = bradleyConfig.hp;
                bradley.maxCratesToSpawn = 0;
                bradley.viewDistance = bradleyConfig.viewDistance;
                bradley.searchRange = bradleyConfig.searchDistance;
                bradley.coaxAimCone *= bradleyConfig.coaxAimCone;
                bradley.coaxFireRate *= bradleyConfig.coaxFireRate;
                bradley.coaxBurstLength = bradleyConfig.coaxBurstLength;
                bradley.nextFireTime = bradleyConfig.nextFireTime;
                bradley.topTurretFireRate = bradleyConfig.topTurretFireRate;
                bradley.recoilScale = 0;
                bradleys.Add(bradley);

                ins.timer.In(2f, () =>
                {
                    if (bradley != null && baseWagonEntity != null)
                    {
                        foreach (var a in bradley.GetComponentsInChildren<WheelCollider>()) DestroyImmediate(a);
                        bradley.rightWheels = new WheelCollider[0];
                        bradley.leftWheels = new WheelCollider[0];
                        Rigidbody rigidbody = bradley.myRigidBody;
                        if (rigidbody != null) DestroyImmediate(rigidbody);
                        bradley.myRigidBody = baseWagonEntity.rigidBody;
                    }
                });
            }

            void CreateCrate(CrateConfig crateConfig, LocationConfig locationConfig)
            {
                LootContainer lootContainer = CreateEntity(crateConfig.prefab, locationConfig) as LootContainer;
                if (lootContainer == null) return;
                HackableLockedCrate hackableLockedCrate = lootContainer as HackableLockedCrate;
                if (hackableLockedCrate != null)
                {
                    hackableLockedCrate.DestroyShared();
                    hackableLockedCrate.shouldDecay = false;
                    hackableLockedCrate.decayTimer = float.MaxValue;
                    hackableLockedCrate.SendNetworkUpdate();
                    hackableLockedCrate.hackSeconds = HackableLockedCrate.requiredHackSeconds - crateConfig.crateUnlockTime;
                    lockedCrates.Add(hackableLockedCrate, crateConfig.presetName);
                }
                else crates.Add(lootContainer, crateConfig.presetName);
                if (crateConfig.typeLootTable == 1 || crateConfig.typeLootTable == 4) Invoke(() => ins.AddToContainerItem(lootContainer.inventory, crateConfig.ownLootTable, crateConfig.typeLootTable), 2f);
                ins.train.countCrates++;
            }

            void CreateDecor(string prefab, LocationConfig locationConfig)
            {
                BaseEntity entity = CreateEntity(prefab, locationConfig);
                if (entity == null) return;
                BaseCombatEntity baseCombatEntity = entity as BaseCombatEntity;
                if (baseCombatEntity == null) return;
                baseCombatEntity.ShowHealthInfo = false;
                baseCombatEntity.lifestate = BaseCombatEntity.LifeState.Dead;
                decorEntities.Add(entity);
            }

            void CreateSamsite(SamSiteConfig samSiteConfig, LocationConfig locationConfig)
            {
                SamSite samSite = CreateEntity("assets/prefabs/npc/sam_site_turret/sam_site_turret_deployed.prefab", locationConfig) as SamSite;
                samSite.InitializeHealth(samSiteConfig.hp, samSiteConfig.hp);
                samSite.inventory.Insert(ItemManager.CreateByName("ammo.rocket.sam", samSiteConfig.countAmmo));
                samSite.UpdateFromInput(100, 0);
                samSite.isLootable = false;
                samSite.dropFloats = false;
                samSite.dropsLoot = false;
                samsites.Add(samSite);
            }

            internal BaseEntity CreateEntity(string prefab, LocationConfig locationConfig, bool setFlag8 = false)
            {
                BaseEntity entity = GameManager.server.CreateEntity(prefab, baseWagonEntity.transform.position);
                if (entity == null) ins.PrintError($"Prefab not found! {prefab}");
                entity.enableSaving = false;
                if (setFlag8) entity.SetFlag(BaseEntity.Flags.Reserved8, true);
                entity.Spawn();
                Rigidbody rigidbody = entity.GetComponent<Rigidbody>();
                if (rigidbody != null) rigidbody.isKinematic = true;
                DestroyOnGroundMissing destroyOnGroundMissing = entity.GetComponentInChildren<DestroyOnGroundMissing>();
                if (destroyOnGroundMissing != null) Destroy(destroyOnGroundMissing);

                entity.SetParent(baseWagonEntity);
                entity.transform.localPosition = locationConfig.position.ToVector3();
                entity.transform.localEulerAngles = locationConfig.rotation.ToVector3();
                entity.SendNetworkUpdate();
                return entity;
            }

            void CreateNpc(NpcConfig npcConfig, LocationConfig locationConfig)
            {
                HashSet<string> states = new HashSet<string> { "IdleState", "CombatStationaryState" };
                if (npcConfig.beltItems.Any(x => x.shortName == "rocket.launcher" || x.shortName == "explosive.timed")) states.Add("RaidState");
                JObject config = new JObject
                {
                    ["Name"] = npcConfig.name,
                    ["WearItems"] = new JArray { npcConfig.wearItems.Select(x => new JObject { ["ShortName"] = x.shortName, ["SkinID"] = x.skinID }) },
                    ["BeltItems"] = new JArray { npcConfig.beltItems.Select(x => new JObject { ["ShortName"] = x.shortName, ["Amount"] = x.amount, ["SkinID"] = x.skinID, ["Mods"] = new JArray { x.Mods }, ["Ammo"] = string.Empty }) },
                    ["Kit"] = npcConfig.kit,
                    ["Health"] = npcConfig.health,
                    ["RoamRange"] = 0,
                    ["ChaseRange"] = 100,
                    ["DamageScale"] = npcConfig.damageScale,
                    ["TurretDamageScale"] = npcConfig.turretDamageScale,
                    ["AimConeScale"] = npcConfig.aimConeScale,
                    ["DisableRadio"] = npcConfig.disableRadio,
                    ["CanUseWeaponMounted"] = true,
                    ["CanRunAwayWater"] = true,
                    ["Speed"] = 0,
                    ["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = string.Empty,
                    ["States"] = new JArray { states },
                    ["Sensory"] = new JObject
                    {
                        ["AttackRangeMultiplier"] = npcConfig.attackRangeMultiplier,
                        ["SenseRange"] = npcConfig.senseRange,
                        ["MemoryDuration"] = npcConfig.memoryDuration,
                        ["CheckVisionCone"] = npcConfig.checkVisionCone,
                        ["VisionCone"] = npcConfig.visionCone
                    }
                };

                ScientistNPC scientist = (ScientistNPC)ins.NpcSpawn.Call("SpawnNpc", baseWagonEntity.transform.position, config);
                scientist.SetParent(baseWagonEntity);
                scientist.transform.localPosition = locationConfig.position.ToVector3();
                scientist.transform.localEulerAngles = locationConfig.rotation.ToVector3();
                npcs.Add(scientist);
            }

            internal virtual void DestroyWagon()
            {
                foreach (ScientistNPC scientistNPC in npcs) if (scientistNPC.IsExists()) scientistNPC.Kill();
                foreach (AutoTurret autoTurret in turrets) if (autoTurret.IsExists()) autoTurret.Kill();
                foreach (BradleyAPC bradleyAPC in bradleys) if (bradleyAPC.IsExists()) bradleyAPC.Kill();
                foreach (SamSite samSite in samsites) if (samSite.IsExists()) samSite.Kill();
                foreach (BaseEntity decorEntity in decorEntities) if (decorEntity.IsExists()) decorEntity.Kill();
                foreach (LootContainer lootContainer in crates.Keys) if (lootContainer.IsExists()) lootContainer.Kill();
                foreach (LootContainer lootContainer in lockedCrates.Keys) if (lootContainer.IsExists()) lootContainer.Kill();
                baseWagonEntity.SetFlag(BaseEntity.Flags.Reserved2, false);
                baseWagonEntity.SetFlag(BaseEntity.Flags.Reserved3, false);
                if (baseWagonEntity.IsExists()) baseWagonEntity.Kill();
            }
        }

        class ATrainHeliHeli : FacepunchBehaviour
        {
            TrainCar targetEntity;

            internal PatrolHelicopterAI patrolHelicopterAI;
            internal BaseHelicopter baseHelicopter;
            HeliConfig heliConfig;

            internal void InitHelicopter(BaseHelicopter baseHelicopter, HeliConfig heliConfig)
            {
                this.heliConfig = heliConfig;
                this.baseHelicopter = baseHelicopter;
                patrolHelicopterAI = baseHelicopter.myAI;
                baseHelicopter.startHealth = heliConfig.hp;
                baseHelicopter.InitializeHealth(heliConfig.hp, heliConfig.hp);
                baseHelicopter.maxCratesToSpawn = heliConfig.cratesAmount;
                baseHelicopter.bulletDamage = heliConfig.bulletDamage;
                baseHelicopter.bulletSpeed = heliConfig.bulletSpeed;
                var weakspots = baseHelicopter.weakspots;
                if (weakspots != null && weakspots.Length > 1)
                {
                    weakspots[0].maxHealth = heliConfig.mainRotorHealth;
                    weakspots[0].health = heliConfig.mainRotorHealth;
                    weakspots[1].maxHealth = heliConfig.rearRotorHealth;
                    weakspots[1].health = heliConfig.rearRotorHealth;
                }
                targetEntity = ins.train.locomotive.baseWagonEntity;
                patrolHelicopterAI.isRetiring = true;
            }

            internal void SetTarget(BasePlayer player)
            {
                patrolHelicopterAI.SetTargetDestination(player.transform.position);
                patrolHelicopterAI._targetList.Add(new PatrolHelicopterAI.targetinfo(player, player));
            }

            void FixedUpdate()
            {
                if (targetEntity == null || targetEntity.IsDestroyed || baseHelicopter.myAI.isDead) return;
                if (!ins.train.IsTrainStop())
                {
                    patrolHelicopterAI.SetTargetDestination(targetEntity.transform.position + new Vector3(0, heliConfig.height, 0));
                    if (Vector2.Distance(new Vector2(baseHelicopter.transform.position.x, baseHelicopter.transform.position.z), new Vector2(targetEntity.transform.position.x, targetEntity.transform.position.z)) < 50)
                    {
                        if (ins.train.reverse) patrolHelicopterAI.SetIdealRotation(Quaternion.AngleAxis(180, Vector3.up) * targetEntity.transform.rotation);
                        else patrolHelicopterAI.SetIdealRotation(targetEntity.transform.rotation);
                    }
                }
                else if (targetEntity.Distance(baseHelicopter.transform.position) > heliConfig.distance) patrolHelicopterAI.SetTargetDestination(targetEntity.transform.position + new Vector3(0, heliConfig.height, 0));
            }
        }

        class TrainDriver : FacepunchBehaviour
        {
            BasePlayer driver;
            Coroutine driverCorountine;
            InputState inputState;
            TrainEngine trainEngine;

            internal void InitDriver(BasePlayer driver, TrainEngine trainEngine)
            {
                this.driver = driver;
                this.trainEngine = trainEngine;
                driverCorountine = ServerMgr.Instance.StartCoroutine(DriverCorountine());
            }

            IEnumerator DriverCorountine()
            {
                while (true)
                {
                    CreateInput();
                    yield return CoroutineEx.waitForSeconds(30);
                }
            }

            InputState CreateInput()
            {
                inputState = new InputState();
                inputState.previous.mouseDelta = Vector3.zero;
                inputState.current.aimAngles = Vector3.zero;
                inputState.current.mouseDelta = Vector3.zero;
                int random = UnityEngine.Random.Range(0, 3);
                if (random == 0) inputState.current.buttons = 8;
                else if (random == 1) inputState.current.buttons = 16;
                else inputState.current.buttons = 0;
                return inputState;
            }

            void FixedUpdate() => trainEngine.PlayerServerInput(inputState, driver);

            void OnDestroy()
            {
                if (driverCorountine != null) ServerMgr.Instance.StopCoroutine(driverCorountine);
            }
        }

        #endregion Classes

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["EventActive"] = "{0} Ивент в данный момент активен, сначала завершите текущий ивент (<color=#ce3f27>/atrainstop</color>)!",
                ["PreStartTrain"] = "{0} <color=#738d43>{1}</color> появится через {2}!",
                ["StartTrain"] = "{0} <color=#738d43>{1}</color> начал движение!",
                ["PlayerStopTrain"] = "{0} <color=#ce3f27>{1}</color> остановил поезд!",
                ["RemainTime"] = "{0} Поезд будет уничтожен через <color=#ce3f27>{1}</color>!",
                ["EndEvent"] = "{0} Перевозка груза <color=#ce3f27>окончена</color>!",
                ["NeedStopTrain"] = "{0} Необходимо <color=#ce3f27>остановить</color> поезд!",
                ["EnterPVP"] = "{0} Вы <color=#ce3f27>вошли</color> в PVP зону, теперь другие игроки <color=#ce3f27>могут</color> наносить вам урон!",
                ["ExitPVP"] = "{0} Вы <color=#738d43>вышли</color> из PVP зоны, теперь другие игроки <color=#738d43>не могут</color> наносить вам урон!",
                ["GUI"] = "Поезд будет уничтожен через <color=#ce3f27>{0}</color>",

                ["Hour"] = "ч.",
                ["Min"] = "м.",
                ["Sec"] = "с.",
            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["EventActive"] = "{0} This event is active now. To finish this event (<color=#ce3f27/atrainstop</color>)!",
                ["PreStartTrain"] = "{0} The <color=#738d43>{1}</color> will spawn in {2}!",
                ["StartTrain"] = "{0} The <color=#738d43>{1}</color> started moving!",
                ["PlayerStopTrain"] = "{0} <color=#ce3f27>{1}</color>  stopped the train!",
                ["RemainTime"] = "{0} The train will be destroyed in <color=#ce3f27>{1}</color>!",
                ["EndEvent"] = "{0} The event is <color=#ce3f27>over</color>!",
                ["NeedStopTrain"] = "{0} It is necessary to <color=#ce3f27>stop</color> the train!",
                ["EnterPVP"] = "{0} You <color=#ce3f27>have entered</color> the PVP zone, now other players <color=#ce3f27>can damage</color> you!",
                ["ExitPVP"] = "{0} You <color=#738d43>have gone out</color> the PVP zone, now other players <color=#738d43>can’t damage</color> you!",
                ["GUI"] = "The train will be destroyed in <color=#ce3f27>{0}</color>",

                ["Hour"] = "h.",
                ["Min"] = "m.",
                ["Sec"] = "s.",
            }, this);
        }

        string GetMessage(string langKey, string userID) => lang.GetMessage(langKey, this, userID);

        string GetMessage(string langKey, string userID, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userID) : string.Format(GetMessage(langKey, userID), args);
        #endregion Lang

        #region Config  
        private PluginConfig _config;

        protected override void LoadDefaultConfig() => _config = PluginConfig.DefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
            Config.WriteObject(_config, true);
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        public class MainConfig
        {
            [JsonProperty(en ? "Enable automatic event holding [true/false]" : "Включить автоматическое проведение ивента [true/false]")] public bool isAutoEvent { get; set; }
            [JsonProperty(en ? "Minimum time between events [sec]" : "Минимальное вермя между ивентами [sec]")] public int minTimeBetweenEvent { get; set; }
            [JsonProperty(en ? "Maximum time between events [sec]" : "Максимальное вермя между ивентами [sec]")] public int maxTimeBetweenEvent { get; set; }
            [JsonProperty(en ? "If there is no railway on the map, then the event will be held underground [true/false]" : "Если на карте нет железной дороги, то ивент будет проводиться под землей [true/false]")] public bool isUnderGround { get; set; }
            [JsonProperty(en ? "The probability of holding an event underground [0 - 100]" : "Вероятность проведения ивента под землей [0 - 100]")] public float undergroundChance { get; set; }
            [JsonProperty(en ? "The train attacks first [true/false]" : "Поезд атакует первым [true/false]")] public bool isAggressive { get; set; }
            [JsonProperty(en ? "The time for which the train becomes aggressive after taking damage [sec]" : "Время, на которое поезд становится враждебным, после получения урона [sec]")] public int agressiveTime { get; set; }
            [JsonProperty(en ? "The crates can only be opened when the train is stopped [true/false]" : "Ящики можно открыть только на остановленном поезеде [true/false]")] public bool needStopTrain { get; set; }
            [JsonProperty(en ? "Destroy the train after opening all the crates [true/false]" : "Уничтожать поезд после открытия всех ящиков [true/false]")] public bool killTrainAfterLoot { get; set; }
            [JsonProperty(en ? "Time to destroy the train after opening all the crates [sec]" : "Время до уничтожения поезда после открытия всех ящиков [sec]")] public int killTimeTrainAfterLoot { get; set; }
            [JsonProperty(en ? "Destroy wagons in front of the train [true/false]" : "Уничтожать вагоны перед поездом [true/false]")] public bool destrroyWagons { get; set; }
            [JsonProperty(en ? "List of entities to delete ahead of the train" : "Список объектов для удаления впереди поезда")] public HashSet<string> destroyEntities { get; set; }
            [JsonProperty(en ? "Use custom spawn coordinates [true/false]" : "Использовать кастомные координаты спавна [true/false]")] public bool useCustomCoords { get; set; }
            [JsonProperty(en ? "Custom coordinates for the spawn of the train (/atrainpoint)" : "Кастомные координаты для спавна поезда (/atrainpoint)")] public List<string> customSpawnPoints { get; set; }
            [JsonProperty(en ? "Allow wagons to be connected to the train [true/false]" : "Разрешить присоединение вагонов к поезду [true/false]")] public bool allowConnectWagons { get; set; }
            [JsonProperty(en ? "Allow damage to the train driver [true/false]" : "Разрешить урон по водителю поезда [true/false]")] public bool allowDriverDamage { get; set; }
            [JsonProperty(en ? "To revive the train driver if he was killed? [true/false]" : "Возрождать водителя поезда, если он был убит [true/false]")] public bool reviveTrainDriver { get; set; }
        }

        public class TrainConfig
        {
            [JsonProperty(en ? "Preset Name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty(en ? "Train Name" : "Название поезда")] public string trainName { get; set; }
            [JsonProperty(en ? "Event time" : "Время ивента")] public int eventTime { get; set; }
            [JsonProperty(en ? "Train can be spawned underground [true/false]" : "Поезд может появляться под землей [true/false]")] public bool isUndergroundTrain { get; set; }
            [JsonProperty(en ? "Allow automatic startup? [true/false]" : "Разрешить автоматический запуск? [true/false]")] public bool automaticStart { get; set; }
            [JsonProperty(en ? "Probability of a preset [0.0-100.0]" : "Вероятность пресета [0.0-100.0]")] public float chance { get; set; }
            [JsonProperty(en ? "Locomotive Preset" : "Пресет локомотива")] public string locomotivePreset { get; set; }
            [JsonProperty(en ? "Order of wagons" : "Порядок вагонов")] public List<string> wagonsPreset { get; set; }
            [JsonProperty(en ? "Heli preset" : "Пресет вертолета")] public string heliPreset { get; set; }
            [JsonProperty(en ? "Train Stop time" : "Время на которое останавливается поезд")] public int stopTime { get; set; }
        }

        public class BaseWagonConfig
        {
            [JsonProperty(en ? "Preset name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty(en ? "Prefab name" : "Префаба")] public string prefabName { get; set; }
            [JsonProperty(en ? "Bradley preset - locations" : "Пресет бредли - расположения")] public Dictionary<string, HashSet<LocationConfig>> brradleys { get; set; }
            [JsonProperty(en ? "Turret preset - locations" : "Пресет турели - расположения")] public Dictionary<string, HashSet<LocationConfig>> turrets { get; set; }
            [JsonProperty(en ? "SamSite preset - locations" : "Пресет SamSite - расположения")] public Dictionary<string, HashSet<LocationConfig>> samsites { get; set; }
            [JsonProperty(en ? "NPC preset - locations" : "Пресет NPC - расположения")] public Dictionary<string, HashSet<LocationConfig>> NPCs { get; set; }
            [JsonProperty(en ? "Crate preset - locations" : "Пресет крейта - расположения")] public Dictionary<string, HashSet<LocationConfig>> crates { get; set; }
            [JsonProperty(en ? "Decorative prefab - locations" : "Префаб декоративного блока - расположения")] public Dictionary<string, HashSet<LocationConfig>> decors { get; set; }
        }

        public class LocomotiveConfig : BaseWagonConfig
        {
            [JsonProperty(en ? "Engine force" : "Мощность двигателя")] public float engineForce { get; set; }
            [JsonProperty(en ? "Max speed" : "Максимальная скорость")] public float maxSpeed { get; set; }
            [JsonProperty(en ? "Driver name" : "Имя водителя")] public string driverName { get; set; }
        }

        public class ColorConfig
        {
            [JsonProperty("r")] public float r { get; set; }
            [JsonProperty("g")] public float g { get; set; }
            [JsonProperty("b")] public float b { get; set; }
        }

        public class MarkerConfig
        {
            [JsonProperty(en ? "Do you use the Marker? [true/false]" : "Использовать ли маркер? [true/false]")] public bool enable { get; set; }
            [JsonProperty(en ? "Radius" : "Радиус")] public float radius { get; set; }
            [JsonProperty(en ? "Alpha" : "Прозрачность")] public float alpha { get; set; }
            [JsonProperty(en ? "Marker color" : "Цвет маркера")] public ColorConfig color1 { get; set; }
            [JsonProperty(en ? "Outline color" : "Цвет контура")] public ColorConfig color2 { get; set; }
        }

        public class LocationConfig
        {
            [JsonProperty(en ? "Position" : "Позиция")] public string position { get; set; }
            [JsonProperty(en ? "Rotation" : "Вращение")] public string rotation { get; set; }
        }

        public class BradleyConfig
        {
            [JsonProperty(en ? "Preset Name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty("HP")] public float hp { get; set; }
            [JsonProperty(en ? "Scale damage" : "Множитель урона")] public float scaleDamage { get; set; }
            [JsonProperty(en ? "The viewing distance" : "Дальность обзора")] public float viewDistance { get; set; }
            [JsonProperty(en ? "Radius of search" : "Радиус поиска")] public float searchDistance { get; set; }
            [JsonProperty(en ? "The multiplier of Machine-gun aim cone" : "Множитель разброса пулемёта")] public float coaxAimCone { get; set; }
            [JsonProperty(en ? "The multiplier of Machine-gun fire rate" : "Множитель скорострельности пулемёта")] public float coaxFireRate { get; set; }
            [JsonProperty(en ? "Amount of Machine-gun burst shots" : "Кол-во выстрелов очереди пулемёта")] public int coaxBurstLength { get; set; }
            [JsonProperty(en ? "The time between shots of the main gun [sec.]" : "Время между залпами основного орудия [sec.]")] public float nextFireTime { get; set; }
            [JsonProperty(en ? "The time between shots of the main gun in a fire rate [sec.]" : "Время между выстрелами основного орудия в залпе [sec.]")] public float topTurretFireRate { get; set; }
        }

        public class TurretConfig
        {
            [JsonProperty(en ? "Preset Name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty(en ? "Health" : "Кол-во ХП")] public float hp { get; set; }
            [JsonProperty(en ? "Weapon ShortName" : "ShortName оружия")] public string shortNameWeapon { get; set; }
            [JsonProperty(en ? "Ammo ShortName" : "ShortName патронов")] public string shortNameAmmo { get; set; }
            [JsonProperty(en ? "Number of ammo" : "Кол-во патронов")] public int countAmmo { get; set; }
        }

        public class SamSiteConfig
        {
            [JsonProperty(en ? "Preset Name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty(en ? "Health" : "Кол-во ХП")] public float hp { get; set; }
            [JsonProperty(en ? "Number of ammo" : "Кол-во патронов")] public int countAmmo { get; set; }
        }

        public class DecorConfig
        {
            [JsonProperty(en ? "Preset Name" : "Название пресета")] public Dictionary<string, LocationConfig> decorList { get; set; }
        }

        public class HeliConfig
        {
            [JsonProperty(en ? "Name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty("HP")] public float hp { get; set; }
            [JsonProperty(en ? "HP of the main rotor" : "HP главного винта")] public float mainRotorHealth { get; set; }
            [JsonProperty(en ? "HP of tail rotor" : "HP хвостового винта")] public float rearRotorHealth { get; set; }
            [JsonProperty(en ? "Numbers of crates" : "Количество ящиков")] public int cratesAmount { get; set; }
            [JsonProperty(en ? "Flying height" : "Высота полета")] public float height { get; set; }
            [JsonProperty(en ? "Bullet speed" : "Скорость пуль")] public float bulletSpeed { get; set; }
            [JsonProperty(en ? "Bullet Damage" : "Урон пуль")] public float bulletDamage { get; set; }
            [JsonProperty(en ? "The distance to which the helicopter can move away from the convoy" : "Дистанция, на которую вертолет может отдаляться от конвоя")] public float distance { get; set; }
            [JsonProperty(en ? "Speed" : "Скорость")] public float speed { get; set; }
        }

        public class NpcConfig
        {
            [JsonProperty("Name")] public string name { get; set; }
            [JsonProperty(en ? "Health" : "Кол-во ХП")] public float health { get; set; }
            [JsonProperty(en ? "Should remove the corpse?" : "Удалять труп?")] public bool deleteCorpse { get; set; }
            [JsonProperty(en ? "Attack Range Multiplier" : "Множитель радиуса атаки")] public float attackRangeMultiplier { get; set; }
            [JsonProperty(en ? "Sense Range" : "Радиус обнаружения цели")] public float senseRange { get; set; }
            [JsonProperty(en ? "Memory duration [sec.]" : "Длительность памяти цели [sec.]")] public float memoryDuration { get; set; }
            [JsonProperty(en ? "Scale damage" : "Множитель урона")] public float damageScale { get; set; }
            [JsonProperty(en ? "Aim Cone Scale" : "Множитель разброса")] public float aimConeScale { get; set; }
            [JsonProperty(en ? "Detect the target only in the NPC's viewing vision cone?" : "Обнаруживать цель только в углу обзора NPC? [true/false]")] public bool checkVisionCone { get; set; }
            [JsonProperty(en ? "Vision Cone" : "Угол обзора")] public float visionCone { get; set; }
            [JsonProperty(en ? "Disable radio effects? [true/false]" : "Отключать эффекты рации? [true/false]")] public bool disableRadio { get; set; }
            [JsonProperty(en ? "Wear items" : "Одежда")] public List<NpcWear> wearItems { get; set; }
            [JsonProperty(en ? "Belt items" : "Быстрые слоты")] public List<NpcBelt> beltItems { get; set; }
            [JsonProperty(en ? "Which loot table should the plugin use? (0 - default, BetterLoot, MagicLoot; 1 - own)" : "Какую таблицу лута необходимо использовать? (0 - стандартную, BetterLoot, MagicLoot; 1 - собственную)")] public int typeLootTable { get; set; }
            [JsonProperty("Kit")] public string kit { get; set; }
            [JsonProperty(en ? "Turret Damage Multiplier" : "Множитель урона от турелей")] public float turretDamageScale { get; set; }
            [JsonProperty(en ? "Own loot table" : "Собственная таблица лута")] public LootTableConfig lootTable { get; set; }
        }

        public class DriverConfig
        {
            [JsonProperty("Name")] public string name { get; set; }
            [JsonProperty(en ? "Health" : "Кол-во ХП")] public float health { get; set; }
            [JsonProperty(en ? "Wear items" : "Одежда")] public List<NpcWear> wearItems { get; set; }
            [JsonProperty("Kit")] public string kit { get; set; }
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
            [JsonProperty(en ? "Mods" : "Модификации на оружие")] public HashSet<string> Mods { get; set; }
        }

        public class CrateConfig
        {
            [JsonProperty(en ? "Preset Name" : "Название пресета")] public string presetName { get; set; }
            [JsonProperty("Prefab")] public string prefab { get; set; }
            [JsonProperty(en ? "Time to unlock the crates (LockedCrate) [sec.]" : "Время до открытия заблокированного ящика (LockedCrate) [sec.]")] public float crateUnlockTime { get; set; }
            [JsonProperty(en ? "Which loot table should the plugin use? (0 - default; 1 - own; 2 - AlphaLoot; 3 - CustomLoot; 4 - Add Items)" : "Какую таблицу лута необходимо использовать? (0 - стандартную; 1 - собственную; 2 - AlphaLoot; 3 - CustomLoot; 4 - Добавить предметы)")] public int typeLootTable { get; set; }
            [JsonProperty(en ? "Own loot table" : "Собственная таблица предметов")] public LootTableConfig ownLootTable { get; set; }
        }

        public class LootTableConfig
        {
            [JsonProperty(en ? "Minimum numbers of items" : "Минимальное кол-во элементов")] public int minAmount { get; set; }
            [JsonProperty(en ? "Maximum numbers of items" : "Максимальное кол-во элементов")] public int maxAmount { get; set; }
            [JsonProperty(en ? "List of items" : "Список предметов")] public List<ItemConfig> itemsConfig { get; set; }
        }

        public class ItemConfig
        {
            [JsonProperty("ShortName")] public string shortName { get; set; }
            [JsonProperty(en ? "Minimum" : "Минимальное кол-во")] public int minAmount { get; set; }
            [JsonProperty(en ? "Maximum" : "Максимальное кол-во")] public int maxAmount { get; set; }
            [JsonProperty(en ? "Chance [0.0-100.0]" : "Шанс выпадения предмета [0.0-100.0]")] public float chance { get; set; }
            [JsonProperty(en ? "Is this a blueprint? [true/false]" : "Это чертеж? [true/false]")] public bool isBluePrint { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong skinID { get; set; }
            [JsonProperty(en ? "Name (empty - default)" : "Название (empty - default)")] public string name { get; set; }
        }

        public class ZoneConfig
        {
            [JsonProperty(en ? "Create a PVP zone in the convoy isStop zone? (only for those who use the TruePVE plugin)[true/false]" : "Создавать зону PVP в зоне проведения ивента? (только для тех, кто использует плагин TruePVE) [true/false]")] public bool isCreateZonePVP { get; set; }
            [JsonProperty(en ? "Use the dome? [true/false]" : "Использовать ли купол? [true/false]")] public bool isDome { get; set; }
            [JsonProperty(en ? "Darkening the dome" : "Затемнение купола")] public int darkening { get; set; }
            [JsonProperty(en ? "Radius" : "Радиус")] public float radius { get; set; }
        }

        public class GUIConfig
        {
            [JsonProperty(en ? "Use the Countdown GUI? [true/false]" : "Использовать ли GUI обратного отсчета? [true/false]")] public bool IsGUI { get; set; }
            [JsonProperty("AnchorMin")] public string AnchorMin { get; set; }
            [JsonProperty("AnchorMax")] public string AnchorMax { get; set; }
        }

        public class NotifyConfig
        {
            [JsonProperty(en ? "The time from the notification to the start of the event [sec]" : "Время от оповещения до начала ивента [sec]")] public int preStartTime { get; set; }
            [JsonProperty(en ? "Use a chat? [true/false]" : "Использовать ли чат? [true/false]")] public bool chat { get; set; }
            [JsonProperty(en ? "The time until the end of the event, when a message is displayed about the time until the end of the event [sec]" : "Время до конца ивента, когда выводится сообщение о сокром окончании ивента [sec]")] public HashSet<int> timeNotifications { get; set; }
        }

        public class PveModeConfig
        {
            [JsonProperty(en ? "Use the PVE mode of the plugin? [true/false]" : "Использовать PVE режим работы плагина? [true/false]")] public bool pve { get; set; }
            [JsonProperty(en ? "The amount of damage that the player has to do to become the Event Owner" : "Кол-во урона, которое должен нанести игрок, чтобы стать владельцем ивента")] public float damage { get; set; }
            [JsonProperty(en ? "Damage coefficients for calculate to become the Event Owner" : "Коэффициенты урона для подсчета, чтобы стать владельцем события")] public HashSet<ScaleDamageConfig> scaleDamage { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event loot the crates? [true/false]" : "Может ли не владелец ивента грабить ящики? [true/false]")] public bool lootCrate { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event hack locked crates? [true/false]" : "Может ли не владелец ивента взламывать заблокированные ящики? [true/false]")] public bool hackCrate { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event loot NPC corpses? [true/false]" : "Может ли не владелец ивента грабить трупы NPC? [true/false]")] public bool lootNpc { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event deal damage to the NPC? [true/false]" : "Может ли не владелец ивента наносить урон по NPC? [true/false]")] public bool damageNpc { get; set; }
            [JsonProperty(en ? "Can an Npc attack a non-owner of the event? [true/false]" : "Может ли Npc атаковать не владельца ивента? [true/false]")] public bool targetNpc { get; set; }
            [JsonProperty(en ? "Can Bradley attack a non-owner of the event? [true/false]" : "Может ли Bradley атаковать не владельца ивента? [true/false]")] public bool targetTank { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event do damage to Bradley? [true/false]" : "Может ли не владелец ивента наносить урон по Bradley? [true/false]")] public bool damageTank { get; set; }
            [JsonProperty(en ? "Allow the non-owner of the event to enter the event zone? [true/false]" : "Разрешать входить внутрь зоны ивента не владельцу ивента? [true/false]")] public bool canEnter { get; set; }
            [JsonProperty(en ? "Allow a player who has an active cooldown of the Event Owner to enter the event zone? [true/false]" : "Разрешать входить внутрь зоны ивента игроку, у которого активен кулдаун на получение статуса владельца ивента? [true/false]")] public bool canEnterCooldownPlayer { get; set; }
            [JsonProperty(en ? "The time that the Event Owner may not be inside the event zone [sec.]" : "Время, которое владелец ивента может не находиться внутри зоны ивента [сек.]")] public int timeExitOwner { get; set; }
            [JsonProperty(en ? "The time until the end of Event Owner status when it is necessary to warn the player [sec.]" : "Время таймера до окончания действия статуса владельца ивента, когда необходимо предупредить игрока [сек.]")] public int alertTime { get; set; }
            [JsonProperty(en ? "Prevent the actions of the RestoreUponDeath plugin in the event zone? [true/false]" : "Запрещать работу плагина RestoreUponDeath в зоне действия ивента? [true/false]")] public bool restoreUponDeath { get; set; }
            [JsonProperty(en ? "The time that the player can`t become the Event Owner, after the end of the event and the player was its owner [sec.]" : "Время, которое игрок не сможет стать владельцем ивента, после того как ивент окончен и игрок был его владельцем [sec.]")] public double cooldownOwner { get; set; }
            [JsonProperty(en ? "Darkening the dome (0 - disables the dome)" : "Затемнение купола (0 - отключает купол)")] public int darkening { get; set; }
        }

        public class ScaleDamageConfig
        {
            [JsonProperty(en ? "Type of target" : "Тип цели")] public string Type { get; set; }
            [JsonProperty(en ? "Damage Multiplier" : "Множитель урона")] public float Scale { get; set; }
        }

        public class EconomyConfig
        {
            [JsonProperty(en ? "Enable economy" : "Включить экономику?")] public bool enable { get; set; }
            [JsonProperty(en ? "Which economy plugins do you want to use? (Economics, Server Rewards, IQEconomic)" : "Какие плагины экономики вы хотите использовать? (Economics, Server Rewards, IQEconomic)")] public HashSet<string> plugins { get; set; }
            [JsonProperty(en ? "The minimum value that a player must collect to get points for the economy" : "Минимальное значение, которое игрок должен заработать, чтобы получить баллы за экономику")] public double minEconomyPiont { get; set; }
            [JsonProperty(en ? "The minimum value that a winner must collect to make the commands work" : "Минимальное значение, которое победитель должен заработать, чтобы сработали команды")] public double minCommandPoint { get; set; }
            [JsonProperty(en ? "Looting of crates" : "Ограбление ящиков")] public Dictionary<string, double> crates { get; set; }
            [JsonProperty(en ? "Killing an NPC" : "Убийство NPC")] public double npcPoint { get; set; }
            [JsonProperty(en ? "Killing an Bradley" : "Уничтожение Bradley")] public double bradleyPoint { get; set; }
            [JsonProperty(en ? "Killing an Turret" : "Уничтожение Турели")] public double turretPoint { get; set; }
            [JsonProperty(en ? "Killing an Heli" : "Уничтожение Вертолета")] public double heliPoint { get; set; }
            [JsonProperty(en ? "Hacking a locked crate" : "Взлом заблокированного ящика")] public double lockedCratePoint { get; set; }
            [JsonProperty(en ? "List of commands that are executed in the console at the end of the event ({steamid} - the player who collected the highest number of points)" : "Список команд, которые выполняются в консоли по окончанию ивента ({steamid} - игрок, который набрал наибольшее кол-во баллов)")] public HashSet<string> commands { get; set; }
        }

        public class GUIAnnouncementsConfig
        {
            [JsonProperty(en ? "Do you use the GUI Announcements? [true/false]" : "Использовать ли GUI Announcements? [true/false]")] public bool isGUIAnnouncements { get; set; }
            [JsonProperty(en ? "Banner color" : "Цвет баннера")] public string bannerColor { get; set; }
            [JsonProperty(en ? "Text color" : "Цвет текста")] public string textColor { get; set; }
            [JsonProperty(en ? "Adjust Vertical Position" : "Отступ от верхнего края")] public float apiAdjustVPosition { get; set; }
        }

        public class NotifyPluginConfig
        {
            [JsonProperty(en ? "Do you use the Notify? [true/false]" : "Использовать ли Notify? [true/false]")] public bool isNotify { get; set; }
            [JsonProperty(en ? "Type" : "Тип")] public string type { get; set; }
        }

        public class DiscordConfig
        {
            [JsonProperty(en ? "Do you use the Discord? [true/false]" : "Использовать ли Discord? [true/false]")] public bool isDiscord { get; set; }
            [JsonProperty("Webhook URL")] public string webhookUrl { get; set; }
            [JsonProperty(en ? "Embed Color (DECIMAL)" : "Цвет полосы (DECIMAL)")] public int embedColor { get; set; }
            [JsonProperty(en ? "Keys of required messages" : "Ключи необходимых сообщений")] public HashSet<string> keys { get; set; }
        }

        public class SupportedPluginsConfig
        {
            [JsonProperty(en ? "PVE Mode Setting" : "Настройка PVE Mode")] public PveModeConfig pveMode { get; set; }
            [JsonProperty(en ? "Economy Setting" : "Настройка экономики")] public EconomyConfig economy { get; set; }
            [JsonProperty(en ? "GUI Announcements setting" : "Настройка GUI Announcements")] public GUIAnnouncementsConfig GUIAnnouncements { get; set; }
            [JsonProperty(en ? "Notify setting" : "Настройка Notify")] public NotifyPluginConfig notify { get; set; }
            [JsonProperty(en ? "DiscordMessages setting" : "Настройка DiscordMessages")] public DiscordConfig discord { get; set; }
        }

        private class PluginConfig
        {
            [JsonProperty(en ? "Version" : "Версия")] public VersionNumber versionConfig { get; set; }
            [JsonProperty(en ? "Prefix of chat messages" : "Префикс в чате")] public string prefix { get; set; }
            [JsonProperty(en ? "Main Setting" : "Основные настройки")] public MainConfig mainConfig { get; set; }
            [JsonProperty(en ? "Train presets" : "Пресеты поездов")] public HashSet<TrainConfig> trainConfigs { get; set; }
            [JsonProperty(en ? "Locomotive presets" : "Пресеты локомотивов")] public HashSet<LocomotiveConfig> locomotiveConfigs { get; set; }
            [JsonProperty(en ? "Wagon presets" : "Пресеты вагонов")] public HashSet<BaseWagonConfig> wagonConfigs { get; set; }
            [JsonProperty(en ? "Marker Setting" : "Настройки маркера")] public MarkerConfig markerConfig { get; set; }
            [JsonProperty(en ? "Zone Setting" : "Настройки зоны ивента")] public ZoneConfig zoneConfig { get; set; }
            [JsonProperty(en ? "GUI Setting" : "Настройки GUI")] public GUIConfig guiConfig { get; set; }
            [JsonProperty(en ? "Notification Settings" : "Настройки уведомлений")] public NotifyConfig notifyConfig { get; set; }
            [JsonProperty(en ? "Bradley presets" : "Пресеты бредли")] public HashSet<BradleyConfig> bradleysConfigs { get; set; }
            [JsonProperty(en ? "Turrets presets" : "Пресеты турелей")] public HashSet<TurretConfig> turretConfigs { get; set; }
            [JsonProperty(en ? "Samsite presets" : "Пресеты Samsite")] public HashSet<SamSiteConfig> samsiteConfigs { get; set; }
            [JsonProperty(en ? "Crate presets" : "Пресеты ящиков")] public HashSet<CrateConfig> crateConfigs { get; set; }
            [JsonProperty(en ? "Heli presets" : "Пресеты вертолетов")] public HashSet<HeliConfig> heliConfigs { get; set; }
            [JsonProperty(en ? "NPC presets" : "Пресеты NPC")] public HashSet<NpcConfig> NPCConfigs { get; set; }
            [JsonProperty(en ? "Driver presets" : "Пресеты водителей")] public HashSet<DriverConfig> driverConfigs { get; set; }
            [JsonProperty(en ? "Supported Plugins" : "Поддерживаемые плагины")] public SupportedPluginsConfig supportedPluginsConfig { get; set; }

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    versionConfig = new VersionNumber(1, 2, 0),
                    prefix = "[ArmoredTrain]",
                    mainConfig = new MainConfig
                    {
                        isAutoEvent = true,
                        minTimeBetweenEvent = 7200,
                        maxTimeBetweenEvent = 7200,
                        isAggressive = false,
                        agressiveTime = 300,
                        needStopTrain = true,
                        killTrainAfterLoot = true,
                        killTimeTrainAfterLoot = 300,
                        isUnderGround = true,
                        undergroundChance = 50,
                        destrroyWagons = true,
                        destroyEntities = new HashSet<string>
                        {
                            "minicopter",
                            "car"
                        },
                        useCustomCoords = false,
                        customSpawnPoints = new List<string>()
                    },
                    trainConfigs = new HashSet<TrainConfig>
                    {
                        new TrainConfig
                        {
                            presetName = "train_easy",
                            trainName = "Small Train",
                            isUndergroundTrain = true,
                            eventTime = 3600,
                            stopTime = 120,
                            automaticStart = true,
                            chance = 40,
                            locomotivePreset = "locomotive_default",
                            wagonsPreset = new List<string>
                            {
                                "wagon_crate_1"
                            },
                            heliPreset = ""
                        },
                        new TrainConfig
                        {
                            presetName = "train_normal",
                            trainName = "Train",
                            isUndergroundTrain = false,
                            eventTime = 3600,
                            stopTime = 120,
                            automaticStart = true,
                            chance = 40,
                            locomotivePreset = "locomotive_turret",
                            wagonsPreset = new List<string>
                            {
                                "wagon_bradley",
                                "wagon_crate_1",
                                "wagon_samsite"
                            },
                            heliPreset = ""
                        },
                        new TrainConfig
                        {
                            presetName = "train_hard",
                            trainName = "Giant Train",
                            isUndergroundTrain = false,
                            eventTime = 3600,
                            stopTime = 120,
                            automaticStart = true,
                            chance = 20,
                            locomotivePreset = "locomotive_turret",
                            wagonsPreset = new List<string>
                            {
                                "wagon_bradley",
                                "wagon_crate_2",
                                "wagon_bradley",
                                "wagon_samsite"
                            },
                            heliPreset = "heli_1"
                        },
                        new TrainConfig
                        {
                            presetName = "train_hard_new",
                            trainName = "Giant Train",
                            isUndergroundTrain = false,
                            eventTime = 3600,
                            stopTime = 120,
                            automaticStart = false,
                            chance = 20,
                            locomotivePreset = "locomotive_new",
                            wagonsPreset = new List<string>
                            {
                                "wagon_bradley",
                                "wagon_crate_2",
                                "wagon_bradley",
                                "wagon_samsite"
                            },
                            heliPreset = "heli_1"
                        },
                        new TrainConfig
                        {
                            presetName = "train_caboose",
                            trainName = "Caboose",
                            isUndergroundTrain = false,
                            eventTime = 3600,
                            stopTime = 0,
                            automaticStart = true,
                            chance = 20,
                            locomotivePreset = "locomotive_default",
                            wagonsPreset = new List<string>
                            {
                                "caboose_wagon"
                            },
                            heliPreset = ""
                        }
                    },
                    locomotiveConfigs = new HashSet<LocomotiveConfig>
                    {
                        new LocomotiveConfig
                        {
                            presetName = "locomotive_default",
                            prefabName = "assets/content/vehicles/trains/workcart/workcart_aboveground.entity.prefab",
                            engineForce = 250000f,
                            maxSpeed = 12,
                            driverName = "Train_Driver_1",
                            brradleys = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            turrets = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            NPCs = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["TrainNPC"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-0.742, 1.458, 4)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.742, 1.458, 2)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.742, 1.458, 0)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(0.742, 1.458, -3.5)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.742, 1.458, -3.5)",
                                        rotation = "(0, 0, 0)"
                                    }
                                }
                            },
                            crates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            samsites = new Dictionary<string, HashSet<LocationConfig>>
                            {

                            },
                            decors = new Dictionary<string, HashSet<LocationConfig>>
                            {

                            }
                        },
                        new LocomotiveConfig
                        {
                            presetName = "locomotive_turret",
                            prefabName = "assets/content/vehicles/trains/workcart/workcart_aboveground2.entity.prefab",
                            engineForce = 250000f,
                            maxSpeed = 12,
                            driverName = "Train_Driver_1",
                            brradleys = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            turrets = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["turret_ak"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(0.684, 3.845, 3.683)",
                                        rotation = "(0, 0, 0)"
                                    }
                                },
                                ["turret_m249"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(0.945, 2.627, 0.556)",
                                        rotation = "(0, 313, 0)"
                                    }
                                }
                            },
                            NPCs = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["TrainNPC"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-0.742, 1.458, 4)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.742, 1.458, 2)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.742, 1.458, 0)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(0.742, 1.458, -3.5)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.742, 1.458, -3.5)",
                                        rotation = "(0, 0, 0)"
                                    }
                                }
                            },
                            crates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            samsites = new Dictionary<string, HashSet<LocationConfig>>
                            {

                            },
                            decors = new Dictionary<string, HashSet<LocationConfig>>
                            {

                            }
                        },
                        new LocomotiveConfig
                        {
                            presetName = "locomotive_new",
                            prefabName = "assets/content/vehicles/trains/locomotive/locomotive.entity.prefab",
                            engineForce = 500000f,
                            maxSpeed = 14,
                            driverName = "Train_Driver_1",
                            brradleys = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            turrets = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["turret_m249"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-0.554, 1.546, -8.849)",
                                        rotation = "(0, 180, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(0.554, 1.546, -8.849)",
                                        rotation = "(0, 180, 0)"
                                    }
                                }
                            },
                            NPCs = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["TrainNPC"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-1.341, 1.546, 2)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-1.341, 1.546, 0)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-1.341, 1.546, -2)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-1.341, 1.546, -4)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-1.341, 1.546, -6)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-1.341, 1.546, -8)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(1.341, 1.546, 2)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(1.341, 1.546, 0)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(1.341, 1.546, -2)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(1.341, 1.546, -4)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(1.341, 1.546, -6)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(1.341, 1.546, -8)",
                                        rotation = "(0, 0, 0)"
                                    }
                                }
                            },
                            crates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            samsites = new Dictionary<string, HashSet<LocationConfig>>
                            {

                            },
                            decors = new Dictionary<string, HashSet<LocationConfig>>
                            {

                            }
                        }
                    },
                    wagonConfigs = new HashSet<BaseWagonConfig>
                    {
                        new BaseWagonConfig
                        {
                            presetName = "wagon_crate_1",
                            prefabName = "assets/content/vehicles/trains/wagons/trainwagonc.entity.prefab",

                            brradleys = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            turrets = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["turret_ak"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-0.940, 1.559, -6.811)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(0.940, 1.559, -6.811)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.940, 1.559, 6.811)",
                                        rotation = "(0, 180, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(0.940, 1.559, 6.811)",
                                        rotation = "(0, 180, 0)"
                                    }
                                }
                            },
                            NPCs = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            crates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["chinooklockedcrate_default"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(0, 1.550, 0)",
                                        rotation = "(0, 0, 0)"
                                    }
                                },
                                ["crateelite_default"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(0.7, 1.550, -2.359)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.7, 1.550, -2.359)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(0.7, 1.550, 2.359)",
                                        rotation = "(0, 180, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.7, 1.550, 2.359)",
                                        rotation = "(0, 180, 0)"
                                    }
                                },
                            },
                            samsites = new Dictionary<string, HashSet<LocationConfig>>
                            {

                            },
                            decors = new Dictionary<string, HashSet<LocationConfig>>
                            {

                            }
                        },
                        new BaseWagonConfig
                        {
                            presetName = "wagon_crate_2",
                            prefabName = "assets/content/vehicles/trains/wagons/trainwagonb.entity.prefab",

                            brradleys = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            turrets = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            NPCs = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["TrainNPC"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-1.177, 1.458, -2.267)",
                                        rotation = "(0, 270, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-1.177, 1.458, 0.475)",
                                        rotation = "(0, 270, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-1.177, 1.458, 3.202)",
                                        rotation = "(0, 270, 0)"
                                    }
                                }
                            },
                            crates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["chinooklockedcrate_default"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-0.772, 1.550, 5.693)",
                                        rotation = "(0, 180, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.772, 1.550, -5.693)",
                                        rotation = "(0, 0, 0)"
                                    }
                                },
                                ["crateelite_default"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(1.076, 1.550, 1.047)",
                                        rotation = "(0, 270, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(1.076, 1.550, -0.609)",
                                        rotation = "(0, 270, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(1.076, 1.550, -2.359)",
                                        rotation = "(0, 270, 0)"
                                    }
                                },
                            },
                            samsites = new Dictionary<string, HashSet<LocationConfig>>
                            {

                            },
                            decors = new Dictionary<string, HashSet<LocationConfig>>
                            {

                            }
                        },
                        new BaseWagonConfig
                        {
                            presetName = "wagon_bradley",
                            prefabName = "assets/content/vehicles/trains/wagons/trainwagonb.entity.prefab",
                            brradleys = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["bradley_default"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-0.185, 2.206, -3.36)",
                                        rotation = "(0, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(0.185, 2.206, 3.460)",
                                        rotation = "(0, 180, 0)"
                                    }
                                }
                            },
                            turrets = new Dictionary<string, HashSet<LocationConfig>>
                            {

                            },
                            NPCs = new Dictionary<string, HashSet<LocationConfig>>
                            {

                            },
                            crates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            samsites = new Dictionary<string, HashSet<LocationConfig>>
                            {

                            },
                            decors = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["assets/content/vehicles/modularcar/module_entities/2module_fuel_tank.prefab"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-0.772, 3.008, -4.295)",
                                        rotation = "(0, 0, 90)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.772, 3.008, -0.232)",
                                        rotation = "(0, 0, 90)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.812, 1.659, -3.221)",
                                        rotation = "(90, 270, 0)"
                                    },

                                    new LocationConfig
                                    {
                                        position = "(0.772, 3.008, 4.295)",
                                        rotation = "(0, 180, 90)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(0.772, 3.008, 0.232)",
                                        rotation = "(0, 180, 90)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(0.812, 1.659, 3.221)",
                                        rotation = "(90, 90, 0)"
                                    },

                                    new LocationConfig
                                    {
                                        position = "(-0.757, 1.659, 3.226)",
                                        rotation = "(90, 270, 0)"
                                    },

                                    new LocationConfig
                                    {
                                        position = "(0.516, 1.7, 5.521)",
                                        rotation = "(90, 0, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.516, 1.7, 5.521)",
                                        rotation = "(90, 0, 0)"
                                    },

                                    new LocationConfig
                                    {
                                        position = "(0.516, 1.7, -5.521)",
                                        rotation = "(90, 180, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.516, 1.7, -5.521)",
                                        rotation = "(90, 180, 0)"
                                    }
                                }
                            }
                        },
                        new BaseWagonConfig
                        {
                            presetName = "wagon_samsite",
                            prefabName = "assets/content/vehicles/trains/wagons/trainwagonunloadablefuel.entity.prefab",
                            brradleys = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            turrets = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["turret_ak"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(0, 4.296, -5.346)",
                                        rotation = "(0, 180, 0)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(0, 4.296, 5.346)",
                                        rotation = "(0, 0, 0)"
                                    }
                                }
                            },
                            NPCs = new Dictionary<string, HashSet<LocationConfig>>
                            {

                            },
                            crates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                            },
                            samsites = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["samsite_default"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(0, 4.216, 0)",
                                        rotation = "(0, 180, 0)"
                                    }
                                }
                            },
                            decors = new Dictionary<string, HashSet<LocationConfig>>
                            {

                            }
                        },
                        new BaseWagonConfig
                        {
                            presetName = "caboose_wagon",
                            prefabName = "assets/content/vehicles/trains/caboose/traincaboose.entity.prefab",
                            brradleys = new Dictionary<string, HashSet<LocationConfig>>(),
                            crates = new Dictionary<string, HashSet<LocationConfig>>(),
                            samsites = new Dictionary<string, HashSet<LocationConfig>>(),
                            decors = new Dictionary<string, HashSet<LocationConfig>>(),
                            NPCs = new Dictionary<string, HashSet<LocationConfig>>(),
                            turrets = new Dictionary<string, HashSet<LocationConfig>>()
                        }
                    },
                    markerConfig = new MarkerConfig
                    {
                        enable = true,
                        radius = 0.2f,
                        alpha = 0.6f,
                        color1 = new ColorConfig { r = 0.81f, g = 0.25f, b = 0.15f },
                        color2 = new ColorConfig { r = 0f, g = 0f, b = 0f }
                    },
                    zoneConfig = new ZoneConfig
                    {
                        isCreateZonePVP = false,
                        isDome = false,
                        darkening = 5,
                        radius = 100
                    },
                    guiConfig = new GUIConfig
                    {
                        IsGUI = true,
                        AnchorMin = "0 0.9",
                        AnchorMax = "1 0.95"
                    },
                    notifyConfig = new NotifyConfig
                    {
                        preStartTime = 10,
                        chat = true,
                        timeNotifications = new HashSet<int>
                        {
                            300,
                            60,
                            30,
                            5
                        }
                    },
                    bradleysConfigs = new HashSet<BradleyConfig>
                    {
                        new BradleyConfig
                        {
                            presetName = "bradley_default",
                            hp = 900f,
                            scaleDamage = 0.3f,
                            viewDistance = 100.0f,
                            searchDistance = 100.0f,
                            coaxAimCone = 1.1f,
                            coaxFireRate = 1.0f,
                            coaxBurstLength = 10,
                            nextFireTime = 10f,
                            topTurretFireRate = 0.25f
                        },
                    },
                    turretConfigs = new HashSet<TurretConfig>
                    {
                        new TurretConfig
                        {
                            presetName = "turret_ak",
                            hp = 250f,
                            shortNameWeapon = "rifle.ak",
                            shortNameAmmo = "ammo.rifle",
                            countAmmo = 200
                        },
                        new TurretConfig
                        {
                            presetName = "turret_m249",
                            hp = 300f,
                            shortNameWeapon = "lmg.m249",
                            shortNameAmmo = "ammo.rifle",
                            countAmmo = 400
                        }
                    },
                    samsiteConfigs = new HashSet<SamSiteConfig>
                    {
                        new SamSiteConfig
                        {
                            presetName = "samsite_default",
                            hp = 1000,
                            countAmmo = 100
                        }
                    },
                    crateConfigs = new HashSet<CrateConfig>
                    {
                        new CrateConfig
                        {
                            presetName = "chinooklockedcrate_default",
                            prefab = "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab",
                            typeLootTable = 0,
                            crateUnlockTime = 0,
                            ownLootTable = new LootTableConfig
                            {
                                minAmount = 1,
                                maxAmount = 2,
                                itemsConfig = new List<ItemConfig>
                                {
                                    new ItemConfig
                                    {
                                        shortName = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    }
                                }
                            }
                        },
                        new CrateConfig
                        {
                            presetName = "crateelite_default",
                            prefab = "assets/bundled/prefabs/radtown/underwater_labs/crate_elite.prefab",
                            typeLootTable = 0,
                            crateUnlockTime = 0,
                            ownLootTable = new LootTableConfig
                            {
                                minAmount = 1,
                                maxAmount = 2,
                                itemsConfig = new List<ItemConfig>
                                {
                                    new ItemConfig
                                    {
                                        shortName = "scrap",
                                        minAmount = 100,
                                        maxAmount = 200,
                                        chance = 100f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    }
                                }
                            }
                        }
                    },
                    heliConfigs = new HashSet<HeliConfig>
                    {
                        new HeliConfig
                        {
                            presetName = "heli_1",
                            hp = 10000f,
                            cratesAmount = 0,
                            mainRotorHealth = 750f,
                            rearRotorHealth = 375f,
                            height = 50f,
                            bulletDamage = 20f,
                            bulletSpeed = 250f,
                            distance = 350f,
                            speed = 25f
                        }
                    },
                    NPCConfigs = new HashSet<NpcConfig>
                    {
                        new NpcConfig
                        {
                            name = "TrainNPC",
                            health = 200f,
                            deleteCorpse = true,
                            wearItems = new List<NpcWear>
                            {
                                new NpcWear
                                {
                                    shortName = "metal.plate.torso",
                                    skinID = 1988476232
                                },
                                new NpcWear
                                {
                                    shortName = "riot.helmet",
                                    skinID = 1988478091
                                },
                                new NpcWear
                                {
                                    shortName = "pants",
                                    skinID = 1582399729
                                },
                                new NpcWear
                                {
                                    shortName = "tshirt",
                                    skinID = 1582403431
                                },
                                new NpcWear
                                {
                                    shortName = "shoes.boots",
                                    skinID = 0
                                }
                            },
                            beltItems = new List<NpcBelt>
                            {
                                new NpcBelt
                                {
                                    shortName = "rifle.lr300",
                                    amount = 1,
                                    skinID = 0,
                                    Mods = new HashSet<string>{ "weapon.mod.flashlight", "weapon.mod.holosight" }
                                },
                                new NpcBelt
                                {
                                    shortName = "syringe.medical",
                                    amount = 10,
                                    skinID = 0,
                                    Mods = new HashSet<string> ()
                                },
                                new NpcBelt
                                {
                                    shortName = "grenade.f1",
                                    amount = 10,
                                    skinID = 0,
                                    Mods = new HashSet<string> ()
                                }
                            },
                            kit = "",
                            turretDamageScale = 1,
                            attackRangeMultiplier = 1f,
                            senseRange = 60f,
                            memoryDuration = 60f,
                            damageScale = 1f,
                            aimConeScale = 1f,
                            checkVisionCone = false,
                            visionCone = 135f,
                            disableRadio = false,
                            typeLootTable = 0,
                            lootTable = new LootTableConfig
                            {
                                minAmount = 2,
                                maxAmount = 4,
                                itemsConfig = new List<ItemConfig>
                                {
                                    new ItemConfig
                                    {
                                        shortName = "rifle.semiauto",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 0.09f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "shotgun.pump",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 0.09f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "pistol.semiauto",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 0.09f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "largemedkit",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 0.1f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "smg.2",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 0.1f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "pistol.python",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 0.1f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "smg.thompson",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 0.1f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "shotgun.waterpipe",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 0.2f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "shotgun.double",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 0.2f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "ammo.rifle.explosive",
                                        minAmount = 8,
                                        maxAmount = 8,
                                        chance = 0.2f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "pistol.revolver",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 0.2f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "ammo.rifle.hv",
                                        minAmount = 10,
                                        maxAmount = 10,
                                        chance = 0.2f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "ammo.rifle.incendiary",
                                        minAmount = 8,
                                        maxAmount = 8,
                                        chance = 0.5f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "ammo.pistol.hv",
                                        minAmount = 10,
                                        maxAmount = 10,
                                        chance = 0.5f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "ammo.pistol.fire",
                                        minAmount = 10,
                                        maxAmount = 10,
                                        chance = 1f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "ammo.shotgun.slug",
                                        minAmount = 4,
                                        maxAmount = 8,
                                        chance = 5f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "ammo.shotgun.fire",
                                        minAmount = 4,
                                        maxAmount = 14,
                                        chance = 5f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "ammo.shotgun",
                                        minAmount = 6,
                                        maxAmount = 12,
                                        chance = 8f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "bandage",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 17f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "syringe.medical",
                                        minAmount = 1,
                                        maxAmount = 2,
                                        chance = 34f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "ammo.rifle",
                                        minAmount = 12,
                                        maxAmount = 36,
                                        chance = 51f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "ammo.pistol",
                                        minAmount = 15,
                                        maxAmount = 45,
                                        chance = 52f,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    }
                                }
                            }
                        }
                    },
                    driverConfigs = new HashSet<DriverConfig>
                    {
                        new DriverConfig
                        {
                            name = "Train_Driver_1",
                            health = 200f,
                            wearItems = new List<NpcWear>
                            {
                                new NpcWear
                                {
                                    shortName = "metal.plate.torso",
                                    skinID = 1988476232
                                },
                                new NpcWear
                                {
                                    shortName = "riot.helmet",
                                    skinID = 1988478091
                                },
                                new NpcWear
                                {
                                    shortName = "pants",
                                    skinID = 1582399729
                                },
                                new NpcWear
                                {
                                    shortName = "tshirt",
                                    skinID = 1582403431
                                },
                                new NpcWear
                                {
                                    shortName = "shoes.boots",
                                    skinID = 0
                                }
                            },
                            kit = "",
                        }
                    },
                    supportedPluginsConfig = new SupportedPluginsConfig
                    {
                        pveMode = new PveModeConfig
                        {
                            pve = false,
                            damage = 500f,
                            scaleDamage = new HashSet<ScaleDamageConfig>
                            {
                                new ScaleDamageConfig { Type = "NPC", Scale = 1f },
                                new ScaleDamageConfig { Type = "Bradley", Scale = 1f }
                            },
                            lootCrate = false,
                            hackCrate = false,
                            lootNpc = false,
                            damageNpc = false,
                            targetNpc = false,
                            damageTank = false,
                            targetTank = false,
                            canEnter = false,
                            canEnterCooldownPlayer = true,
                            timeExitOwner = 300,
                            alertTime = 60,
                            restoreUponDeath = true,
                            cooldownOwner = 86400,
                            darkening = 12
                        },
                        economy = new EconomyConfig
                        {
                            enable = false,
                            plugins = new HashSet<string> { "Economics", "Server Rewards", "IQEconomic" },
                            minCommandPoint = 0,
                            minEconomyPiont = 0,
                            crates = new Dictionary<string, double>
                            {
                                ["assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab"] = 0.4
                            },
                            npcPoint = 2,
                            bradleyPoint = 5,
                            lockedCratePoint = 5,
                            turretPoint = 2,
                            heliPoint = 5,
                            commands = new HashSet<string>()
                        },
                        GUIAnnouncements = new GUIAnnouncementsConfig
                        {
                            isGUIAnnouncements = false,
                            bannerColor = "Grey",
                            textColor = "White",
                            apiAdjustVPosition = 0.03f
                        },
                        notify = new NotifyPluginConfig
                        {
                            isNotify = false,
                            type = "0"
                        },
                        discord = new DiscordConfig
                        {
                            isDiscord = false,
                            webhookUrl = "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks",
                            embedColor = 13516583,
                            keys = new HashSet<string>
                            {
                                "PreStartTrain",
                                "PlayerStopTrain",
                                "EndEvent"
                            }
                        },
                    }
                };
            }
        }
        #endregion Config
    }
}

namespace Oxide.Plugins.ArmoredTrainExtensionMethods
{
    public static class ExtensionMethods
    {
        #region Any
        public static bool Any<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return true;
            return false;
        }
        #endregion Any

        #region Where
        public static HashSet<TSource> Where<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            HashSet<TSource> result = new HashSet<TSource>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) result.Add(enumerator.Current);
            return result;
        }
        #endregion Where

        #region FirstOrDefault
        public static TSource FirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (predicate(enumerator.Current)) return enumerator.Current;
            return default(TSource);
        }
        #endregion FirstOrDefault

        #region Select
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
        #endregion Select

        #region OfType
        public static HashSet<T> OfType<T>(this IEnumerable<BaseNetworkable> source)
        {
            HashSet<T> result = new HashSet<T>();
            using (var enumerator = source.GetEnumerator()) while (enumerator.MoveNext()) if (enumerator.Current is T) result.Add((T)(object)enumerator.Current);
            return result;
        }
        #endregion OfType

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
    }
}
