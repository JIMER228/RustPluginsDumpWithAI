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
using Time = UnityEngine.Time;
using System.Reflection;
using System.IO;
using static ProtoBuf.PatternFirework;

namespace Oxide.Plugins
{
    [Info("ArmoredTrain", "Adem", "1.5.1")]
    class ArmoredTrain : RustPlugin
    {
        [PluginReference] Plugin NpcSpawn, PveMode, GUIAnnouncements, DiscordMessages, Economics, ServerRewards, IQEconomic, Friends, Clans;

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
            "OnSamSiteModeToggle",
            "OnPlayerViolation",

            "CanBradleySpawnNpc",
            "OnCustomNpcTarget",
            "CanPopulateLoot",
            "CanEntityTakeDamage",
            "CanEntityBeTargeted",
            "OnCustomLootContainer",
            "SetOwnerPveMode",
            "ClearOwnerPveMode"
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

        bool IsTrainBradley(ulong netID)
        {
            if (!isEventActive) return false;
            return train.IsTrainBradley(netID);
        }

        bool IsTrainHeli(ulong netID)
        {
            if (!isEventActive) return false;
            return train.IsTrainHeli(netID);
        }

        bool IsTrainCrate(ulong netID)
        {
            if (!isEventActive) return false;
            return train.IsTrainCrate(netID);
        }

        bool IsTrainSamSite(ulong netID)
        {
            if (!isEventActive) return false;
            return train.IsTrainSamSite(netID);
        }

        bool IsTrainWagon(ulong netID)
        {
            if (!isEventActive) return false;
            return train.IsTrainWagon(netID);
        }

        bool IsTrainTurret(ulong netID)
        {
            if (!isEventActive) return false;
            return train.IsTrainTurret(netID);
        }
        #endregion ExternalAPI

        #region Hooks
        void Init() => Unsubscribes();

        void OnServerInitialized()
        {
            ins = this;
            LoadDefaultMessages();
            UpdateConfig();
            WagonCustomizator.LoadCurrentCustomizationProfile();
            PostLoadCheck();

            if (_config.mainConfig.isAutoEvent)
                autoEventCoroutine = ServerMgr.Instance.StartCoroutine(AutoEventCorountine());
        }

        void OnButtonPress(PressButton button, BasePlayer player)
        {
            if (!isEventActive || !button.IsExists() || player == null || button.net == null) return;
            if (train.IsTrainButton(button.net.ID.Value)) OnPlayerTryStopTrain(player);
        }

        object CanPickupEntity(BasePlayer player, PressButton entity)
        {
            if (!isEventActive || !entity.IsExists() || player == null || entity.net == null) return null;
            if (train.IsTrainButton(entity.net.ID.Value)) return false;
            return null;
        }

        object OnTrainCarUncouple(TrainCar trainCar, BasePlayer player)
        {
            if (!isEventActive || trainCar == null || player == null || trainCar.net == null) return null;
            if (train.IsTrainWagon(trainCar.net.ID.Value)) return true;
            return null;
        }

        object CanTrainCarCouple(TrainCar trainCar1, TrainCar trainCar2)
        {
            if (!isEventActive || _config.mainConfig.allowConnectWagons || !trainCar1.IsExists() || !trainCar2.IsExists() || trainCar1.net == null || trainCar2.net == null) return null;
            if (!train.IsTrainWagon(trainCar1.net.ID.Value) && !train.IsTrainWagon(trainCar2.net.ID.Value)) return null;
            return false;
        }

        object CanMountEntity(BasePlayer player, BaseVehicleSeat entity)
        {
            if (!isEventActive || !player.userID.IsSteamId() || entity == null || entity.VehicleParent() == null || entity.VehicleParent().net == null || !train.IsTrainWagon(entity.VehicleParent().net.ID.Value)) return null;
            return true;
        }

        object OnTurretTarget(AutoTurret turret, BaseCombatEntity entity)
        {
            if (!isEventActive || !turret.IsExists() || !entity.IsExists() || turret.net == null) return null;
            if (!train.IsTrainTurret(turret.net.ID.Value)) return null;
            BasePlayer player = entity as BasePlayer;
            if (!player.IsRealPlayer()) return true;
            if (!train.IsTrainCanAttack()) return true;
            return null;
        }

        object CanBradleyApcTarget(BradleyAPC apc, BasePlayer player)
        {
            if (!isEventActive || !apc.IsExists() || !player.IsRealPlayer() || apc.net == null) return null;
            if (!train.IsTrainBradley(apc.net.ID.Value)) return null;
            if (!train.IsTrainCanAttack()) return false;
            return null;
        }

        object CanSamSiteShoot(SamSite samSite)
        {
            if (!isEventActive || samSite == null || samSite.net == null) return null;
            if (!train.IsTrainSamSite(samSite.net.ID.Value)) return null;
            if (!train.IsTrainCanAttack()) return true;
            return null;
        }

        object OnSamSiteModeToggle(SamSite samSite, BasePlayer player, bool flag)
        {
            if (samSite == null || player == null || samSite.net == null) return null;
            if (train.IsTrainSamSite(samSite.net.ID.Value)) return true;
            return null;
        }

        object CanHelicopterTarget(PatrolHelicopterAI heli, BasePlayer player)
        {
            if (!isEventActive || train == null || !heli.helicopterBase.IsExists() || heli.helicopterBase.net == null)
                return null;
            if (!train.IsTrainHeli(heli.helicopterBase.net.ID.Value))
                return null;
            if (!train.IsTrainCanAttack())
                return false;
            return null;
        }

        void OnEntitySpawned(HelicopterDebris entity)
        {
            if (!isEventActive || !entity.IsExists() || entity.transform == null || train == null || train.locomotive == null) return;
            if (Vector3.Distance(entity.transform.position, train.locomotive.baseWagonEntity.transform.position) < 20 || train.wagons.Any(x => x != null && x.baseWagonEntity.IsExists() && Vector3.Distance(entity.transform.position, x.baseWagonEntity.transform.position) < 20)) entity.Kill();
        }

        object OnEntityTakeDamage(ScientistNPC scientistNPC, HitInfo info)
        {
            if (!isEventActive || scientistNPC == null || scientistNPC.net == null)
                return null;

            if (!_config.NPCConfigs.Any(x => x.name == scientistNPC.displayName) && !_config.driverConfigs.Any(x => x.name == scientistNPC.displayName))
                return null;

            OnTrainAttacked(info.InitiatorPlayer);

            if (!_config.mainConfig.allowDriverDamage && train.IsTrainDriver(scientistNPC.net.ID.Value))
                return true;

            return null;
        }

        object OnEntityTakeDamage(BradleyAPC bradley, HitInfo info)
        {
            if (!isEventActive || bradley == null || bradley.net == null) return null;
            if (!train.IsTrainBradley(bradley.net.ID.Value)) return null;
            return train.OnTrainAttacked(info);
        }

        object OnEntityTakeDamage(AutoTurret autoTurret, HitInfo info)
        {
            if (!isEventActive || autoTurret == null || autoTurret.net == null) return null;
            if (!train.IsTrainTurret(autoTurret.net.ID.Value)) return null;
            return train.OnTrainAttacked(info);
        }

        object OnEntityTakeDamage(SamSite samSite, HitInfo info)
        {
            if (!isEventActive || samSite == null || samSite.net == null) return null;
            if (!train.IsTrainSamSite(samSite.net.ID.Value)) return null;
            return train.OnTrainAttacked(info);
        }

        object OnEntityTakeDamage(TrainCar trainEngine, HitInfo info)
        {
            if (train == null || trainEngine == null || trainEngine.net == null) return null;
            if (!train.IsTrainWagon(trainEngine.net.ID.Value)) return null;
            return true;
        }

        object OnEntityTakeDamage(PressButton pressButton, HitInfo info)
        {
            if (!isEventActive || pressButton == null || pressButton.net == null) return null;
            if (train.IsTrainButton(pressButton.net.ID.Value)) return true;
            return null;
        }

        void OnEntityDeath(BradleyAPC entity, HitInfo info)
        {
            if (!isEventActive || entity == null || info == null || !info.InitiatorPlayer.IsRealPlayer()) return;
            BaseWagon baseWagon = train.locomotive.bradleys.Any(y => y != null && y.net != null && y.net.ID == entity.net.ID) ? train.locomotive : train.wagons.FirstOrDefault(x => x != null && x.bradleys.Any(y => y != null && y.net != null && y.net.ID == entity.net.ID));
            if (baseWagon == null) return;
            baseWagon.bradleys.Remove(entity);
            EconomyManager.ActionEconomy(info.InitiatorPlayer.userID, "Bradley");
        }

        void OnEntityDeath(ScientistNPC scientistNPC, HitInfo info)
        {
            if (!isEventActive || scientistNPC == null || info == null)
                return;

            if (info.InitiatorPlayer.IsRealPlayer() && _config.NPCConfigs.Any(x => x != null && x.name == scientistNPC.displayName))
                EconomyManager.ActionEconomy(info.InitiatorPlayer.userID, "Npc");

            else if (_config.driverConfigs.Any(x => x != null && x.name == scientistNPC.displayName) && train.IsTrainDriver(scientistNPC.net.ID.Value))
                OnPlayerTryStopTrain(info.InitiatorPlayer);
        }

        void OnEntityDeath(AutoTurret entity, HitInfo info)
        {
            if (!isEventActive || entity == null || info == null || !info.InitiatorPlayer.IsRealPlayer()) return;
            BaseWagon baseWagon = train.locomotive.turrets.Any(y => y != null && y.net != null && y.net.ID == entity.net.ID) ? train.locomotive : train.wagons.FirstOrDefault(x => x != null && x.turrets.Any(y => y != null && y.net != null && y.net.ID == entity.net.ID));
            if (baseWagon == null) return;
            baseWagon.turrets.Remove(entity);
            EconomyManager.ActionEconomy(info.InitiatorPlayer.userID, "Turret");
        }

        void OnEntityDeath(PatrolHelicopter entity, HitInfo info)
        {
            if (!isEventActive || entity == null || info == null || !info.InitiatorPlayer.IsRealPlayer() || entity.net == null) return;
            if (!train.IsTrainHeli(entity.net.ID.Value)) return;
            if (_config.supportedPluginsConfig.economy.enable) EconomyManager.ActionEconomy(info.InitiatorPlayer.userID, "Heli");
        }

        void OnEntityKill(BasePlayer player)
        {
            if (!isEventActive || !player.IsRealPlayer()) return;

            if (ZoneController.IsPlayerInZone(player.userID))
                ZoneController.OnPlayerLeaveZone(player);
        }

        void OnEntityKill(TrainCar trainCar)
        {
            if (!isEventActive || trainCar == null || trainCar.net == null) return;
            if (!train.IsTrainWagon(trainCar.net.ID.Value)) return;
            train.DiconnectTrain();
        }

        object CanLootEntity(BasePlayer player, LootContainer container)
        {
            if (!isEventActive || !_config.mainConfig.needStopTrain || player == null || container == null || container.net == null) return null;
            if (!train.IsTrainCrate(container.net.ID.Value)) return null;
            if (container.transform.position.y - player.transform.position.y > 0.5) return true;
            if (train.CanLoot()) return null;
            NotifyManager.SendMessageToPlayer(player, "NeedStopTrain", _config.prefix);
            return true;
        }

        object CanLootEntity(BasePlayer player, SamSite samSite)
        {
            if (!isEventActive || samSite == null || samSite.net == null) return null;
            if (train.IsTrainSamSite(samSite.net.ID.Value)) return true;
            return null;
        }

        object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (!isEventActive || !_config.mainConfig.needStopTrain || player == null || crate == null || crate.net == null) return null;
            BaseWagon crateWagon = train.GetWagonByLockedCrateNetId(crate.net.ID.Value);

            if (crateWagon != null)
            {
                if (!train.CanLoot())
                {
                    NotifyManager.SendMessageToPlayer(player, "NeedStopTrain", _config.prefix);
                    return true;
                }
                else
                {
                    float hackTime = HackableLockedCrate.requiredHackSeconds - crate.hackSeconds;
                    train.OnLockedCrateStartHack(hackTime);
                    crateWagon.UpdateLockedCrateTimeWithDelay(crate);
                    EconomyManager.ActionEconomy(player.userID, "LockedCrate");
                }
            }
            return null;
        }

        void OnLootEntity(BasePlayer player, LootContainer container)
        {
            if (!isEventActive || player == null || container == null || container.net == null) return;
            if (train.IsTrainCrate(container.net.ID.Value))
                train.OnTrainCrateOpened(container, player);
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

        void Unload()
        {
            EndEvent(true);

            if (ins._config.supportedPluginsConfig.pveMode.enable && ins.plugins.Exists("PveMode"))
                ins.PveMode.Call("EventRemovePveMode", ins.Name, true);
        }

        object OnPlayerViolation(BasePlayer player, AntiHackType type, float amount)
        {
            if (isEventActive && type == AntiHackType.FlyHack && train.IsPositionNearToTrain(player.transform.position))
                return true;

            return null;
        }

        #region OtherPlugins
        object CanBradleySpawnNpc(BradleyAPC bradley)
        {
            if (!isEventActive || bradley == null || bradley.net == null) return null;
            if (train.IsTrainBradley(bradley.net.ID.Value)) return true;
            return null;
        }

        object OnCustomNpcTarget(ScientistNPC npc, BasePlayer player)
        {
            if (!isEventActive || npc == null) return null;
            if (!_config.NPCConfigs.Any(x => x.name == npc.displayName)) return null;
            if (!train.IsTrainCanAttack()) return false;
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

            HackableLockedCrate hackableLockedCrate = container as HackableLockedCrate;
            if (hackableLockedCrate != null)
            {
                if (train.locomotive.lockedCrates.Any(y => y.Key != null && y.Key.net != null && y.Key.net.ID == container.net.ID))
                {
                    cratePresetName = train.locomotive.lockedCrates[hackableLockedCrate];
                }
                else
                {
                    BaseWagon baseWagon = train.wagons.FirstOrDefault(x => x.lockedCrates.Any(y => y.Key != null && y.Key.net != null && y.Key.net.ID == container.net.ID));
                    if (baseWagon != null) cratePresetName = baseWagon.lockedCrates[hackableLockedCrate];
                }
            }

            if (train.locomotive.crates.Any(y => y.Key != null && y.Key.net != null && y.Key.net.ID == container.net.ID))
            {
                cratePresetName = train.locomotive.crates[container];
            }
            else
            {
                BaseWagon baseWagon = train.wagons.FirstOrDefault(x => x.crates.Any(y => y.Key != null && y.Key.net != null && y.Key.net.ID == container.net.ID));
                if (baseWagon != null) cratePresetName = baseWagon.crates[container];
            }

            CrateConfig crateConfig = _config.crateConfigs.FirstOrDefault(x => x.presetName == cratePresetName);
            if (crateConfig != null && crateConfig.typeLootTable != 2) return true;

            return null;
        }

        object CanEntityTakeDamage(BasePlayer victim, HitInfo hitinfo)
        {
            if (!isEventActive || hitinfo == null)
                return null;

            if (victim.IsRealPlayer())
            {
                BasePlayer attacker = hitinfo.InitiatorPlayer;
                if (_config.zoneConfig.isCreateZonePVP && !_config.supportedPluginsConfig.pveMode.enable && attacker.IsRealPlayer())
                {
                    if (ZoneController.IsPlayerInZone(victim.userID) && ZoneController.IsPlayerInZone(attacker.userID))
                        return true;
                }

                if (hitinfo.Initiator == null)
                    return null;

                AutoTurret autoTurret = hitinfo.Initiator as AutoTurret;
                if (autoTurret != null && autoTurret.net != null && train.IsTrainTurret(autoTurret.net.ID.Value))
                {
                    if (!PveModeController.IsPveModeBlockAction(victim))
                        return true;
                    else
                        return null;
                }

                SamSite samSite = hitinfo.Initiator as SamSite;
                if (samSite != null && samSite.net != null && train.IsTrainSamSite(samSite.net.ID.Value))
                {
                    if (!PveModeController.IsPveModeBlockAction(victim))
                        return true;
                    else
                        return null;
                }
                return null;
            }

            return null;
        }

        object CanEntityTakeDamage(PlayerHelicopter victim, HitInfo hitinfo)
        {
            if (!isEventActive || !victim.IsExists() || hitinfo.Initiator == null)
                return null;

            SamSite samSite = hitinfo.Initiator as SamSite;

            if (samSite != null && samSite.net != null && train.IsTrainSamSite(samSite.net.ID.Value))
            {
                BasePlayer heliDriver = victim.GetDriver();

                if (heliDriver != null && !PveModeController.IsPveModeBlockAction(heliDriver))
                    return true;
                else
                    return null;
            }

            return null;
        }

        object CanEntityTakeDamage(AutoTurret autoTurret, HitInfo hitinfo)
        {
            if (!isEventActive || autoTurret == null || autoTurret.net == null)
                return null;

            if (train.IsTrainTurret(autoTurret.net.ID.Value))
            {
                if (!hitinfo.InitiatorPlayer.IsRealPlayer())
                    return false;

                train.OnTrainAttacked(hitinfo);

                if (hitinfo.InitiatorPlayer != null && !PveModeController.IsPveModeBlockAction(hitinfo.InitiatorPlayer))
                    return true;
                else
                    return null;
            }

            return null;
        }

        object CanEntityTakeDamage(SamSite samSite, HitInfo hitinfo)
        {
            if (!isEventActive || samSite == null || samSite.net == null)
                return null;

            if (train.IsTrainSamSite(samSite.net.ID.Value))
            {
                if (!hitinfo.InitiatorPlayer.IsRealPlayer())
                    return false;

                train.OnTrainAttacked(hitinfo);

                if (hitinfo.InitiatorPlayer != null && !PveModeController.IsPveModeBlockAction(hitinfo.InitiatorPlayer))
                    return true;
                else
                    return null;
            }

            return null;
        }

        object CanEntityBeTargeted(BasePlayer player, AutoTurret turret)
        {
            if (!isEventActive || train == null || !turret.IsExists() || !player.IsExists() || turret.net == null || turret.net.ID == null)
                return null;

            if (train.IsTrainTurret(turret.net.ID.Value))
            {
                if (!player.IsRealPlayer())
                    return false;
                if (!train.IsTrainCanAttack())
                    return false;

                if (player != null && !PveModeController.IsPveModeBlockAction(player))
                    return true;
                else
                    return null;
            }

            return null;
        }

        object CanEntityBeTargeted(PlayerHelicopter entity, SamSite samSite)
        {
            if (!isEventActive || samSite == null || samSite.net == null)
                return null;

            if (train.IsTrainSamSite(samSite.net.ID.Value))
            {
                BasePlayer heliDriver = entity.GetDriver();

                if (heliDriver != null && !PveModeController.IsPveModeBlockAction(heliDriver))
                    return true;
                else
                    return null;
            }

            return null;
        }

        object OnCustomLootContainer(NetworkableId netID)
        {
            if (!isEventActive || netID == null) return null;
            LootContainer lootContainer;
            lootContainer = train.locomotive.lockedCrates.Keys.FirstOrDefault(y => y != null && y.net.ID.Value == netID.Value);
            if (lootContainer != null)
            {
                BaseWagon baseWagon = train.wagons.FirstOrDefault(x => x.lockedCrates.Any(y => y.Key != null && y.Key.net.ID.Value == netID.Value));
                if (baseWagon != null) lootContainer = lootContainer = baseWagon.lockedCrates.Keys.FirstOrDefault(x => x != null && x.net.ID.Value == netID.Value);

                if (lootContainer == null)
                {
                    if (lootContainer == null) lootContainer = train.locomotive.crates.Keys.FirstOrDefault(y => y != null && y.net.ID.Value == netID.Value);
                    if (lootContainer == null)
                    {
                        BaseWagon baseWagon1 = train.wagons.FirstOrDefault(x => x.crates.Any(y => y.Key != null && y.Key.net.ID.Value == netID.Value));
                        if (baseWagon1 != null) lootContainer = lootContainer = baseWagon1.crates.Keys.FirstOrDefault(x => x != null && x.net.ID.Value == netID.Value);
                    }
                }
            }
            return CanPopulateLoot(lootContainer);
        }

        void SetOwnerPveMode(string eventName, BasePlayer owner)
        {
            if (eventName == Name)
                PveModeController.SetPveModeOwner(owner);
        }

        void ClearOwnerPveMode(string eventName)
        {
            if (eventName == Name)
                PveModeController.ClearPveModeOwner();
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
                NotifyManager.SendMessageToPlayer(player, "EventActive", _config.prefix);
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
                NotifyManager.SendMessageToPlayer(player, "EventActive", _config.prefix);
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
                NotifyManager.SendMessageToPlayer(player, "EventActive", _config.prefix);
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
            StartEvent(underGroundChance: 0);
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

        [ConsoleCommand("savecustomwagon")]
        void ConsoleSaveCustomWagonCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null)
                return;

            string customizationPresetName = arg.Args[0];
            string wagonShortPrefabName = arg.Args[1];

            WagonCustomizator.MapSaver.CreateOrAddNewWagonToData(customizationPresetName, wagonShortPrefabName);
        }
        #endregion Commands

        #region Actions
        void OnPlayerTryStopTrain(BasePlayer player)
        {
            train.StopTrain(player);
            train.BecomeAgressive(player);
        }

        void OnTrainAttacked(BasePlayer attacker)
        {
            train.BecomeAgressive(attacker);
            if (_config.mainConfig.stopTrainAfterReceivingDamage) OnPlayerTryStopTrain(attacker);
        }
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
                        else if (baseWagonConfig.prefabName == "assets/content/vehicles/train/trainwagonb.entity.prefab") baseWagonConfig.prefabName = "assets/content/vehicles/trains/wagons/trainwagonb.entity.prefab";
                        else if (baseWagonConfig.prefabName == "assets/content/vehicles/train/trainwagonc.entity.prefab") baseWagonConfig.prefabName = "assets/content/vehicles/trains/wagons/trainwagonc.entity.prefab";

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
                _config.versionConfig = new VersionNumber(1, 2, 0);
            }
            if (_config.versionConfig.Minor == 2)
            {
                if (_config.versionConfig.Patch <= 4)
                {
                    _config.mainConfig.selfDestructionConfig = new SelfDestructionConfig
                    {
                        enable = true,
                        beepingTime = 10
                    };
                }
                _config.versionConfig = new VersionNumber(1, 3, 0);
            }
            if (_config.versionConfig.Minor == 3)
            {
                if (_config.versionConfig.Patch <= 5)
                {
                    _config.markerConfig.useShopMarker = true;
                    _config.markerConfig.useRingMarker = true;
                }
                if (_config.versionConfig.Patch <= 6)
                {
                    foreach (HeliConfig heliConfig in _config.heliConfigs)
                        heliConfig.outsideTime = 30;
                }
                if (_config.versionConfig.Patch <= 7)
                {
                    foreach (DriverConfig driverConfig in _config.driverConfigs)
                        driverConfig.turretDamageScale = 0.5f;
                }
                _config.versionConfig = new VersionNumber(1, 4, 0);
            }
            if (_config.versionConfig.Minor == 4)
            {
                if (_config.versionConfig.Patch <= 1)
                {
                    CrateConfig crateNormalConfig = new CrateConfig
                    {
                        presetName = "crate_normal_default",
                        prefab = "assets/bundled/prefabs/radtown/crate_normal.prefab",
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
                    };
                    CrateConfig crateNormal2Config = new CrateConfig
                    {
                        presetName = "crate_normal2_default",
                        prefab = "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
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
                    };
                    _config.crateConfigs.Add(crateNormalConfig);
                    _config.crateConfigs.Add(crateNormal2Config);

                    TurretConfig turretConfig = _config.turretConfigs.FirstOrDefault(x => true);
                    BaseWagonConfig halloweenWagoConfig = new BaseWagonConfig
                    {
                        presetName = "halloween_wagon",
                        prefabName = "assets/content/vehicles/trains/wagons/trainwagonunloadableloot.entity.prefab",
                        brradleys = new Dictionary<string, HashSet<LocationConfig>>(),
                        crates = new Dictionary<string, HashSet<LocationConfig>>
                        {
                            ["crate_normal_default"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(0.407, 2.403, -3.401)",
                                        rotation = "(303.510, 0, 328.794)"
                                    },
                                    new LocationConfig
                                    {
                                        position = "(-0.374, 2.416, 3.104)",
                                        rotation = "(21.106, 261.772, 352.540)"
                                    }
                                },
                            ["crate_normal2_default"] = new HashSet<LocationConfig>
                                {
                                    new LocationConfig
                                    {
                                        position = "(-0.095, 1.817, -0.217)",
                                        rotation = "(19.048, 336.704, 359.624)"
                                    }
                                }
                        },
                        samsites = new Dictionary<string, HashSet<LocationConfig>>(),
                        decors = new Dictionary<string, HashSet<LocationConfig>>(),
                        NPCs = new Dictionary<string, HashSet<LocationConfig>>(),
                        turrets = new Dictionary<string, HashSet<LocationConfig>>
                        {
                            [turretConfig.presetName] = new HashSet<LocationConfig>
                                    {
                                        new LocationConfig
                                        {
                                            position = "(-0.940, 1.559, -6.811)",
                                            rotation = "(0, 180, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(0.940, 1.559, -6.811)",
                                            rotation = "(0, 180, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(-0.940, 1.559, 6.811)",
                                            rotation = "(0, 0, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(0.940, 1.559, 6.811)",
                                            rotation = "(0, 0, 0)"
                                        }
                                    }
                        },
                    };
                    _config.wagonConfigs.Add(halloweenWagoConfig);

                    DriverConfig driverConfig = _config.driverConfigs.FirstOrDefault(x => true);
                    NpcConfig npcConfig = _config.NPCConfigs.FirstOrDefault(x => true);
                    LocomotiveConfig halloweenLocomotive = new LocomotiveConfig
                    {
                        presetName = "locomotive_halloween",
                        prefabName = "assets/content/vehicles/trains/locomotive/locomotive.entity.prefab",
                        engineForce = 500000f,
                        maxSpeed = 14,
                        driverName = driverConfig.name,
                        brradleys = new Dictionary<string, HashSet<LocationConfig>>
                        {
                        },
                        turrets = new Dictionary<string, HashSet<LocationConfig>>
                        {
                        },
                        NPCs = new Dictionary<string, HashSet<LocationConfig>>
                        {
                            [npcConfig.name] = new HashSet<LocationConfig>
                                    {
                                        new LocationConfig
                                        {
                                            position = "(-1.341, 1.546, 2)",
                                            rotation = "(0, 0, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(-1.341, 1.546, -2)",
                                            rotation = "(0, 0, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(-1.341, 1.546, -6)",
                                            rotation = "(0, 0, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(1.341, 1.546, 2)",
                                            rotation = "(0, 0, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(1.341, 1.546, -2)",
                                            rotation = "(0, 0, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(1.341, 1.546, -6)",
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
                    };
                    _config.locomotiveConfigs.Add(halloweenLocomotive);

                    BaseWagonConfig samsiteWagomConfig = _config.wagonConfigs.FirstOrDefault(x => x.presetName.Contains("sam"));
                    BaseWagonConfig crateWagomConfig = _config.wagonConfigs.FirstOrDefault(x => x.presetName.Contains("crate"));

                    TrainConfig halloweenTrainConfig = new TrainConfig
                    {
                        presetName = "train_halloween",
                        trainName = en ? "Halloween Train" : "Хэллоуинский Поезд",
                        isUndergroundTrain = false,
                        eventTime = 3600,
                        stopTime = 300,
                        automaticStart = true,
                        chance = 0,
                        locomotivePreset = "locomotive_halloween",
                        wagonsPreset = new List<string>(),
                        heliPreset = ""
                    };

                    if (crateWagomConfig != null)
                        halloweenTrainConfig.wagonsPreset.Add(crateWagomConfig.presetName);

                    halloweenTrainConfig.wagonsPreset.Add(halloweenWagoConfig.presetName);

                    if (samsiteWagomConfig != null)
                        halloweenTrainConfig.wagonsPreset.Add(samsiteWagomConfig.presetName);

                    _config.trainConfigs.Add(halloweenTrainConfig);
                }
                if (_config.versionConfig.Patch <= 3)
                {
                    _config.customizationConfig = new CustomizationConfig
                    {
                        isElectricFurnacesEnable = true,
                        isBoilersEnable = true,
                        isFireEnable = true
                    };
                }
                if (_config.versionConfig.Patch <= 4)
                {
                    if (_config.customizationConfig == null)
                    {
                        _config.customizationConfig = new CustomizationConfig
                        {
                            isElectricFurnacesEnable = true,
                            isBoilersEnable = true,
                            isFireEnable = true
                        };
                    }
                }
                if (_config.versionConfig.Patch <= 6)
                {
                    _config.customizationConfig.isLightOnlyAtNight = true;
                    _config.mainConfig.isRestoreStopTimeAfterDamageOrLoor = true;
                }
                if (_config.versionConfig.Patch <= 7)
                {
                    _config.customizationConfig.profileName = "";
                    PluginConfig defaultConfig = PluginConfig.DefaultConfig();

                    _config.trainConfigs.Add
                    (
                        new TrainConfig
                        {
                            presetName = "train_xmas_easy",
                            trainName = en ? "Small Christmas train" : "Маленький Новогодний Поезд",
                            isUndergroundTrain = true,
                            eventTime = 3600,
                            stopTime = 300,
                            automaticStart = true,
                            chance = 0,
                            locomotivePreset = "locomotive_default",
                            wagonsPreset = new List<string>
                            {
                                    "xmas_wagon_1"
                            },
                            heliPreset = ""
                        }
                    );
                    _config.trainConfigs.Add
                    (
                        new TrainConfig
                        {
                            presetName = "train_xmas_medium",
                            trainName = en ? "Medium Christmas train" : "Средний Новогодний Поезд",
                            isUndergroundTrain = true,
                            eventTime = 3600,
                            stopTime = 300,
                            automaticStart = true,
                            chance = 0,
                            locomotivePreset = "locomotive_turret",
                            wagonsPreset = new List<string>
                            {
                                    "xmas_wagon_1",
                                    "xmas_wagon_2"
                            },
                            heliPreset = ""
                        }
                    );
                    _config.trainConfigs.Add
                    (
                       new TrainConfig
                       {
                           presetName = "train_xmas_hard",
                           trainName = en ? "Big Christmas train" : "Большой Новогодний Поезд",
                           isUndergroundTrain = false,
                           eventTime = 3600,
                           stopTime = 300,
                           automaticStart = true,
                           chance = 0,
                           locomotivePreset = "locomotive_new",
                           wagonsPreset = new List<string>
                            {
                                    "wagon_crate_2",
                                    "xmas_wagon_2",
                                    "wagon_bradley"
                            },
                           heliPreset = ""
                       }
                    );

                    _config.wagonConfigs.Add
                    (
                        new BaseWagonConfig
                        {
                            presetName = "xmas_wagon_1",
                            prefabName = "assets/content/vehicles/trains/wagons/trainwagonunloadableloot.entity.prefab",
                            brradleys = new Dictionary<string, HashSet<LocationConfig>>(),
                            crates = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["xmas_crate"] = new HashSet<LocationConfig>
                                {
                                        new LocationConfig
                                        {
                                            position = "(-0.148, 2.707, -1.613)",
                                            rotation = "(72.214, 0, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(-0.221, 2.478, -2.314)",
                                            rotation = "(52.555, 180.000, 0)"
                                        }
                                }
                            },
                            samsites = new Dictionary<string, HashSet<LocationConfig>>(),
                            decors = new Dictionary<string, HashSet<LocationConfig>>(),
                            NPCs = new Dictionary<string, HashSet<LocationConfig>>(),
                            turrets = new Dictionary<string, HashSet<LocationConfig>>
                            {
                                ["turret_ak"] = new HashSet<LocationConfig>
                                {
                                        new LocationConfig
                                        {
                                            position = "(-0.940, 1.559, -6.811)",
                                            rotation = "(0, 180, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(0.940, 1.559, -6.811)",
                                            rotation = "(0, 180, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(-0.940, 1.559, 6.811)",
                                            rotation = "(0, 0, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(0.940, 1.559, 6.811)",
                                            rotation = "(0, 0, 0)"
                                        }
                                }
                            },
                        }
                    );
                    _config.wagonConfigs.Add
                    (
                       new BaseWagonConfig
                       {
                           presetName = "xmas_wagon_2",
                           prefabName = "assets/content/vehicles/trains/wagons/trainwagonunloadable.entity.prefab",
                           brradleys = new Dictionary<string, HashSet<LocationConfig>>(),
                           crates = new Dictionary<string, HashSet<LocationConfig>>
                           {
                               ["xmas_crate"] = new HashSet<LocationConfig>
                                {
                                        new LocationConfig
                                        {
                                            position = "(0.027, 3.276, -3.562)",
                                            rotation = "(0.361, 355.541, 16.972)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(0.027, 3.276, 3.897)",
                                            rotation = "(334.884, 355.419, 352.239)"
                                        }
                                }
                           },
                           samsites = new Dictionary<string, HashSet<LocationConfig>>(),
                           decors = new Dictionary<string, HashSet<LocationConfig>>(),
                           NPCs = new Dictionary<string, HashSet<LocationConfig>>(),
                           turrets = new Dictionary<string, HashSet<LocationConfig>>
                           {
                           },
                       }
                    );

                    _config.crateConfigs.Add
                    (
                        new CrateConfig
                        {
                            presetName = "xmas_crate",
                            prefab = "assets/prefabs/missions/portal/proceduraldungeon/xmastunnels/loot/xmastunnellootbox.prefab",
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
                    );

                    _config.customizationConfig.isNeonSignsEnable = true;
                    _config.customizationConfig.giftCannonSetting = defaultConfig.customizationConfig.giftCannonSetting;
                    _config.customizationConfig.fireworksSettings = defaultConfig.customizationConfig.fireworksSettings;
                }
                if (_config.versionConfig.Patch <= 9)
                {

                }
                _config.versionConfig = new VersionNumber(1, 5, 0);
            }
            if (_config.versionConfig.Minor == 5)
            {
                _config.supportedPluginsConfig.pveMode.scaleDamage = new Dictionary<string, float>
                {
                    ["Npc"] = 1f,
                    ["Bradley"] = 2f,
                    ["Helicopter"] = 2f,
                    ["Turret"] = 2f,
                };
            }
            _config.versionConfig = Version;
            SaveConfig();
        }

        void Unsubscribes()
        {
            foreach (string hook in subscribeMethods) Unsubscribe(hook);
        }

        void Subscribes()
        {
            foreach (string hook in subscribeMethods) Subscribe(hook);
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
            if (undergroundChance <= chance && ins._config.trainConfigs.Any(x => x.isUndergroundTrain))
            {
                foreach (TrainCar trainCar in trains)
                    if (trainCar != null && trainCar.transform != null && trainCar.transform.position.y < 0)
                        railTransformList.Add(trainCar.transform);
                underGround = true;
            }
            else
            {
                foreach (TrainCar trainCar in trains) if (trainCar != null && trainCar.transform != null && trainCar.transform.position.y > 0)
                        railTransformList.Add(trainCar.transform);

                if (railTransformList.Count < 3 && _config.mainConfig.isUnderGround)
                {
                    railTransformList.Clear();

                    foreach (TrainCar trainCar in trains)
                        if (trainCar != null && trainCar.transform != null && trainCar.transform.position.y < 0)
                            railTransformList.Add(trainCar.transform);
                    underGround = true;
                }
            }

            if (railTransformList.Count < 2 && (!_config.mainConfig.useCustomCoords || _config.mainConfig.customSpawnPoints.Count == 0))
            {
                PrintError("No rail detected");
                NextTick(() => Server.Command($"o.reload {Name}"));
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
            if (_config.notifyConfig.preStartTime > 0)
            {
                NotifyManager.SendMessageToAll("PreStartTrain", ins._config.prefix, trainConfig.trainName, _config.notifyConfig.preStartTime);
                yield return CoroutineEx.waitForSeconds(_config.notifyConfig.preStartTime);
            }

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
            int countLootInContainer = 0;
            int countLoot = UnityEngine.Random.Range(lootTableConfig.minAmount, lootTableConfig.maxAmount);
            container.capacity = countLoot;
            while (countLootInContainer < countLoot)
            {
                HashSet<ItemConfig> suitableItems = lootTableConfig.itemsConfig.Where(y => !container.itemList.Any(x => x != null && x.info.shortname == y.shortName && x.skin == y.skinID));
                if (suitableItems == null || suitableItems.Count == 0) return;

                foreach (ItemConfig suitableItem in suitableItems)
                {
                    if (UnityEngine.Random.Range(0.0f, 100.0f) <= suitableItem.chance)
                    {
                        Item item = CreateItem(suitableItem);
                        if (!item.MoveToContainer(container))
                            item.Remove();
                        if (countLootInContainer == countLoot)
                            return;
                        countLootInContainer++;
                    }
                }
            }
        }

        Item CreateItem(ItemConfig itemConfig)
        {
            int amount = UnityEngine.Random.Range(itemConfig.minAmount, itemConfig.maxAmount + 1);
            Item item = itemConfig.isBluePrint ? ItemManager.CreateByName("blueprintbase") : ItemManager.CreateByName(itemConfig.shortName, amount, itemConfig.skinID);
            if (itemConfig.isBluePrint) item.blueprintTarget = ItemManager.FindItemDefinition(itemConfig.shortName).itemid;
            if (item.name != "") item.name = itemConfig.name;
            return item;
        }

        void SendErrorMessage(string message, BasePlayer player = null)
        {
            if (player != null) PrintToChat(player, message);
            else PrintError(message);
        }

        void EndEvent(bool isUnloading = false)
        {
            if (autoEventCoroutine != null)
                ServerMgr.Instance.StopCoroutine(autoEventCoroutine);
            if (!isUnloading && _config.mainConfig.isAutoEvent)
                autoEventCoroutine = ServerMgr.Instance.StartCoroutine(AutoEventCorountine());
            Unsubscribes();
            isEventActive = false;
            if (train != null)
            {
                NotifyManager.SendMessageToAll("EndEvent", _config.prefix);
                if (ins._config.mainConfig.enableStartStopLogs)
                    NotifyManager.PrintLogMessage("EventStop_Log");
                train.DestroyTrain();
                train = null;
                Interface.CallHook("OnArmoredTrainEventStop");
                EconomyManager.SendBalance();

                if (ins._config.supportedPluginsConfig.pveMode.enable && ins.plugins.Exists("PveMode"))
                    ins.PveMode.Call("EventRemovePveMode", Name, true);
            }

            ZoneController.DeleteZone(isEventEnd: true);
            PveModeController.ClearPveModeOwner();
        }
        #endregion Methods

        #region Classes
        class Train : FacepunchBehaviour
        {
            Coroutine controlCoroutine;
            Coroutine selfDestructionCorountine;
            Coroutine createTrainCorountine;

            internal TrainConfig trainConfig;
            internal ATrainHeliHeli trainHeli;
            HeliConfig heliConfig;

            internal Locomotive locomotive;
            TrainEngine locomotiveEntity;
            internal List<BaseWagon> wagons = new List<BaseWagon>();
            HashSet<ulong> openedCrates = new HashSet<ulong>();

            int eventTime = 0;
            int stopTime = 0;
            int agressiveTime = 0;
            internal int countCrates = 0;
            ulong stopPlayerUserId = 0;

            bool reverse = false;
            bool isStop = false;
            bool isAgressive = false;

            Vector3 lastLocomotivePoition = Vector3.zero;
            WagonCustomizator wagonCustomizator;

            internal bool CanLoot()
            {
                return !ins._config.mainConfig.needStopTrain || (isStop && ZoneController.IsZoneCreated());
            }

            internal bool IsTrainStop()
            {
                return isStop;
            }

            internal bool IsTrainHeli(ulong netID)
            {
                return trainHeli != null && trainHeli.patrolHelicopter != null && trainHeli.patrolHelicopter.net.ID.Value == netID;
            }

            internal bool IsTrainButton(ulong netID)
            {
                return locomotive != null && locomotive.stopButton.net.ID.Value == netID;
            }

            internal bool IsTrainWagon(ulong netID)
            {
                return (locomotive != null && locomotive.baseWagonEntity.IsExists() && locomotive.baseWagonEntity.net.ID.Value == netID) || (wagons.Any(x => x != null && x.baseWagonEntity.IsExists() && x.baseWagonEntity.net.ID.Value == netID));
            }

            internal bool IsTrainTurret(ulong netID)
            {
                return locomotive != null && locomotive.turrets.Any(x => x.IsExists() && x.net.ID.Value == netID) || wagons.Any(y => y != null && y.turrets.Any(z => z.IsExists() && z.net.ID.Value == netID));
            }

            internal bool IsTrainSamSite(ulong netID)
            {
                return locomotive != null && locomotive.samsites.Any(x => x.IsExists() && x.net.ID.Value == netID) || wagons.Any(y => y != null && y.samsites.Any(z => z.IsExists() && z.net.ID.Value == netID));
            }

            internal bool IsTrainBradley(ulong netID)
            {
                return locomotive != null && locomotive.bradleys.Any(x => x.IsExists() && x.net.ID.Value == netID) || wagons.Any(y => y != null && y.bradleys.Any(z => z.IsExists() && z.net.ID.Value == netID));
            }

            internal bool IsTrainLockedCrate(ulong netID)
            {
                return locomotive != null && locomotive.lockedCrates.Any(x => x.Key.IsExists() && x.Key.net.ID.Value == netID) || wagons.Any(y => y != null && y.lockedCrates.Any(z => z.Key.IsExists() && z.Key.net.ID.Value == netID));
            }

            internal BaseWagon GetWagonByLockedCrateNetId(ulong crateNetID)
            {
                if (locomotive != null && locomotive.lockedCrates.Any(x => x.Key.IsExists() && x.Key.net.ID.Value == crateNetID))
                    return locomotive;

                return wagons.FirstOrDefault(y => y != null && y.lockedCrates.Any(z => z.Key.IsExists() && z.Key.net.ID.Value == crateNetID));
            }

            internal bool IsTrainCrate(ulong netID)
            {
                return locomotive != null && locomotive.lockedCrates.Any(x => x.Key.IsExists() && x.Key.net.ID.Value == netID) || locomotive.crates.Any(x => x.Key.IsExists() && x.Key.net.ID.Value == netID) || wagons.Any(y => y != null && (y.lockedCrates.Any(z => z.Key.IsExists() && z.Key.net.ID.Value == netID) || y.crates.Any(z => z.Key.IsExists() && z.Key.net.ID.Value == netID)));
            }

            internal bool IsTrainCanAttack()
            {
                return isAgressive;
            }

            internal bool IsTrainDriver(ulong netID)
            {
                return locomotive != null && locomotive.driver.IsExists() && locomotive.driver.net.ID.Value == netID;
            }

            internal int GetEventTime()
            {
                return eventTime;
            }

            internal object OnTrainAttacked(HitInfo info)
            {
                if (info == null || !info.InitiatorPlayer.IsRealPlayer())
                    return true;
                BecomeAgressive(info.InitiatorPlayer);

                if (ins._config.mainConfig.stopTrainAfterReceivingDamage)
                    StopTrain(info.InitiatorPlayer);
                if (isStop && ins._config.mainConfig.isRestoreStopTimeAfterDamageOrLoor)
                    stopTime = trainConfig.stopTime;
                return null;
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

            internal void OnLockedCrateStartHack(float hackTime)
            {
                if (isStop && ins._config.mainConfig.isRestoreStopTimeAfterDamageOrLoor)
                    stopTime = trainConfig.stopTime;

                eventTime += (int)hackTime + 10;
            }

            internal void OnTrainCrateOpened(LootContainer container, BasePlayer player)
            {
                if (!openedCrates.Contains(container.net.ID.Value))
                {
                    openedCrates.Add(container.net.ID.Value);
                    EconomyManager.ActionEconomy(player.userID, "Crates", container.PrefabName);
                    BecomeAgressive(player);

                    if (openedCrates.Count >= countCrates)
                    {
                        eventTime = ins._config.mainConfig.killTimeTrainAfterLoot;
                        NotifyManager.SendMessageToAll("RemainTime", ins._config.prefix, eventTime);
                    }
                }
            }

            internal bool IsPositionNearToTrain(Vector3 position)
            {
                return wagons.Any(x => x != null && x.baseWagonEntity.IsExists() && Vector3.Distance(position, x.baseWagonEntity.transform.position) < 15);
            }

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
                wagons.Clear();
                bool failed = false;
                iteration++;
                if (iteration < 10)
                {
                    DefineSpawnPosition();

                    if (spawnPosition == Vector3.zero || !DeleteBarriersInPosition(spawnPosition) || !CreateLocomotive(spawnPosition, spawnRotation))
                        goto StartSpawn;

                    yield return CoroutineEx.waitForSeconds(0.5f);

                    if (!lastWagon.IsExists() || locomotive == null)
                        goto StartSpawn;

                    locomotive.CreateDriver();
                    locomotive.trainEngine.SetThrottle(TrainEngine.EngineSpeeds.Fwd_Hi);
                    yield return CoroutineEx.waitForSeconds(1f);

                    foreach (string wagonPresetName in trainConfig.wagonsPreset)
                    {
                        while (lastWagon.IsExists() && Vector3.Distance(lastWagon.transform.position, spawnPosition) < 20)
                        {
                            if (locomotive == null || locomotive.driver == null || wagons.Any(x => x == null))
                            {
                                OnDestroy();
                                CreateTrain(trainConfig);
                                failed = true;
                                break;
                            }
                            yield return CoroutineEx.waitForSeconds(1f);
                        }
                        if (!failed)
                            DeleteBarriersInPosition(spawnPosition);

                        if (!failed && lastWagon.IsExists())
                            CreateWagon(wagonPresetName);
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
                        if (PostSpawnCheck())
                            PostSpawnUpdate();
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
                else if (ins.railTransformList.Count > 0)
                {
                    Transform randomTransform = ins.railTransformList.GetRandom();
                    if (randomTransform != null)
                    {
                        spawnPosition = randomTransform.position;
                        spawnRotation = randomTransform.rotation;
                    }
                }
                else
                {
                    spawnPosition = Vector3.zero;
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
                locomotiveEntity.CancelInvoke(locomotiveEntity.DecayTick);

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
                wagonEntity.CancelInvoke(wagonEntity.DecayTick);

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
                    if (trainCar == null || IsTrainWagon(trainCar.net.ID.Value)) continue;
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
                PatrolHelicopter heli = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", position, locomotive.baseWagonEntity.transform.rotation) as PatrolHelicopter;
                heli.enableSaving = false;
                heli.Spawn();
                heli.transform.position = position;
                trainHeli = heli.gameObject.AddComponent<ATrainHeliHeli>();
                trainHeli.InitHelicopter(heli, heliConfig, locomotive.trainEngine);
            }

            bool PostSpawnCheck()
            {
                if (locomotive == null || !locomotive.baseWagonEntity.IsExists())
                    return false;
                bool newLocomotive = locomotive.baseWagonEntity.PrefabName.Contains("locomotive.entity");
                if (wagons.Count == 0) return true;
                if (wagons.Any(x => x == null)) return false;

                if (!wagons.Any(x => x != null && Vector3.Distance(x.baseWagonEntity.transform.position, locomotive.baseWagonEntity.transform.position) < 11f) && ((!newLocomotive && Vector3.Distance(wagons[0].baseWagonEntity.transform.position, locomotive.baseWagonEntity.transform.position) < 15)
                    || newLocomotive && Vector3.Distance(wagons[0].baseWagonEntity.transform.position, locomotive.baseWagonEntity.transform.position) < 19))
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
                if (ins._config.mainConfig.destrroyWagons && traincar.IsExists() && !IsTrainWagon(traincar.net.ID.Value))
                {
                    barrierEintity.Kill(BaseNetworkable.DestroyMode.Gib);
                }
            }

            void PostSpawnUpdate()
            {
                SpawnMarkers();

                locomotive.DelayBuild();

                foreach (BaseWagon wagon in wagons)
                    wagon.DelayBuild();

                ins.isEventActive = true;

                if (!ins.underGround) CreateHelicopter();
                Interface.CallHook("OnArmoredTrainEventStart");
                if (ins._config.mainConfig.enableStartStopLogs)
                    NotifyManager.PrintLogMessage("EventStart_Log", trainConfig.presetName);
                controlCoroutine = ServerMgr.Instance.StartCoroutine(ControlCorountine());
                DecorateTrain();
                StartTrain();
            }

            void DecorateTrain()
            {
                if (WagonCustomizator.IsCustomizationCanApplied())
                {
                    wagonCustomizator = this.gameObject.AddComponent<WagonCustomizator>();
                    wagonCustomizator.Init(this);
                }
            }

            void SpawnMarkers()
            {
                if (!ins._config.markerConfig.enable) return;
                EventMapMarker.CreateMarker();
            }
            #endregion Build

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
                ZoneController.DeleteZone();
                PveModeController.ClearPveModeOwner();
                if (trainHeli != null)
                    trainHeli.OnTrainStopOrStart();
                if (!changeDirection) NotifyManager.SendMessageToAll("StartTrain", ins._config.prefix, trainConfig.trainName);
                locomotive.CreateEmergencyCord(isStop);
            }

            internal void StopTrain(BasePlayer player)
            {
                if (isStop)
                    return;

                if (player != null)
                    stopPlayerUserId = player.userID;
                else
                    stopPlayerUserId = 0;

                isStop = true;
                stopTime = trainConfig.stopTime;
                locomotive.trainEngine.SetThrottle(TrainEngine.EngineSpeeds.Zero);

                if (trainHeli != null)
                    trainHeli.OnTrainStopOrStart();
                if (player != null)
                    NotifyManager.SendMessageToAll("PlayerStopTrain", ins._config.prefix, player.displayName);

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

            IEnumerator ControlCorountine()
            {
                while (eventTime > 0 && CheckTrain())
                {
                    eventTime--;

                    SendRemainTimeMessage();
                    TrainStartStopControl();
                    TrainAgressiveControl();
                    KillFrontWagons();
                    UpdateEntitiesContainers();
                    GuiManager.UpdateCountdownGui();
                    if (locomotive != null) lastLocomotivePoition = locomotive.transform.position;

                    if (ins._config.mainConfig.selfDestructionConfig.enable && ins._config.mainConfig.selfDestructionConfig.beepingTime > eventTime)
                        BeepEffect();

                    yield return CoroutineEx.waitForSeconds(1);
                }

                if (ins._config.mainConfig.selfDestructionConfig.enable && CheckTrain())
                    StartSelfDestruction();
                else
                    ins.EndEvent();
            }

            bool CheckTrain()
            {
                return locomotive != null && !wagons.Any(x => x == null);
            }

            void SendRemainTimeMessage()
            {
                if (ins._config.notifyConfig.chat && ins._config.notifyConfig.timeNotifications.Contains(eventTime))
                {
                    NotifyManager.SendMessageToAll("RemainTime", ins._config.prefix, eventTime);
                }
            }

            void TrainStartStopControl()
            {
                if (isStop)
                {
                    stopTime--;

                    if (stopTime <= 0)
                        StartTrain();
                    else if (!ZoneController.IsZoneCreated() && Vector3.Distance(lastLocomotivePoition, locomotive.baseWagonEntity.transform.position) < 1f)
                        OnTrainStopMoving();
                }
                else if (Vector3.Distance(lastLocomotivePoition, locomotive.baseWagonEntity.transform.position) < 0.25f) StartTrain(true);
            }

            void OnTrainStopMoving()
            {
                if (locomotive != null)
                    Interface.CallHook("OnArmoredTrainStopMoving", locomotive.transform.position);

                CreateZone();
            }

            void TrainAgressiveControl()
            {
                if (!ins._config.mainConfig.isAggressive && isAgressive)
                {
                    agressiveTime--;
                    if (agressiveTime <= 0) BecomeNoAgressive();
                }
            }

            void KillFrontWagons()
            {
                Vector3 firstPosition = reverse ? lastWagon.rearCoupling.position - lastWagon.transform.forward : locomotive.baseWagonEntity.frontCoupling.position + locomotive.baseWagonEntity.transform.forward;
                Vector3 secondPosition = reverse ? lastWagon.rearCoupling.position - lastWagon.transform.forward * 3 : locomotive.baseWagonEntity.frontCoupling.position + locomotive.baseWagonEntity.transform.forward * 3f;

                foreach (Collider collider in UnityEngine.Physics.OverlapCapsule(firstPosition, secondPosition, 0.5f))
                {
                    if (collider == null)
                        continue;

                    BaseEntity entity = collider.ToBaseEntity();

                    if (!entity.IsExists())
                        continue;

                    TrainCar barrierTrainCar = entity as TrainCar;

                    if (ins._config.mainConfig.destrroyWagons && barrierTrainCar.IsExists() && barrierTrainCar.net != null && !IsTrainWagon(barrierTrainCar.net.ID.Value))
                        barrierTrainCar.Kill(BaseNetworkable.DestroyMode.Gib);
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
                    foreach (LootContainer lootContainer in baseWagon.lockedCrates.Keys)
                        if (lootContainer.IsExists())
                            lootContainer.SendNetworkUpdate();
                }
            }

            void CreateZone()
            {
                Vector3 position = (locomotive.transform.position + lastWagon.transform.position) / 2;
                ZoneController.CreateZone(position, stopPlayerUserId);
            }

            void BeepEffect()
            {
                for (int index = wagons.Count - 1; index >= 0; index--)
                {
                    BaseWagon wagon = wagons[index];
                    if (wagon != null && wagon.baseWagonEntity.IsExists())
                    {
                        Effect.server.Run("assets/prefabs/gamemodes/objects/capturepoint/effects/capturepoint_progress_complete.prefab", wagon.baseWagonEntity.transform.position + new Vector3(0, 1, 0));
                        Effect.server.Run("assets/prefabs/gamemodes/objects/capturepoint/effects/capturepoint_progress_complete.prefab", wagon.baseWagonEntity.transform.position + new Vector3(0, 1, 0));
                        Effect.server.Run("assets/prefabs/gamemodes/objects/capturepoint/effects/capturepoint_progress_complete.prefab", wagon.baseWagonEntity.transform.position + new Vector3(0, 1, 0));
                    }
                }
            }

            void StartSelfDestruction()
            {
                selfDestructionCorountine = ServerMgr.Instance.StartCoroutine(SelfDestructionCorountine());
            }

            IEnumerator SelfDestructionCorountine()
            {
                for (int index = wagons.Count - 1; index >= 0; index--)
                {
                    BaseWagon wagon = wagons[index];
                    if (wagon != null && wagon.baseWagonEntity.IsExists())
                    {
                        Effect.server.Run("assets/bundled/prefabs/fx/explosions/explosion_03.prefab", wagon.baseWagonEntity.transform.position + new Vector3(0, 1, 0) + wagon.baseWagonEntity.transform.forward * 5);
                        Effect.server.Run("assets/bundled/prefabs/fx/explosions/explosion_02.prefab", wagon.baseWagonEntity.transform.position + new Vector3(0, 1, 0) + wagon.baseWagonEntity.transform.forward * 1.5f);
                        Effect.server.Run("assets/bundled/prefabs/fx/explosions/explosion_03.prefab", wagon.baseWagonEntity.transform.position + new Vector3(0, 1, 0) + wagon.baseWagonEntity.transform.forward * -5);
                        Effect.server.Run("assets/bundled/prefabs/fx/explosions/explosion_02.prefab", wagon.baseWagonEntity.transform.position + new Vector3(0, 1, 0) + wagon.baseWagonEntity.transform.forward * -1.5f);
                        yield return CoroutineEx.waitForSeconds(0.1f);
                        if (wagon != null && wagon.baseWagonEntity.IsExists()) wagon.DestroyWagon();
                    }
                    wagons.Remove(wagon);
                    yield return CoroutineEx.waitForSeconds(0.25f);
                }
                ins.EndEvent();
            }

            internal void DestroyTrain()
            {
                DiconnectTrain();

                for (int index = wagons.Count - 1; index >= 0; index--)
                {
                    BaseWagon wagon = wagons[index];
                    if (wagon != null && wagon.baseWagonEntity.IsExists())
                        wagon.DestroyWagon();
                }
                if (locomotive != null && locomotive.baseWagonEntity.IsExists())
                    locomotive.DestroyWagon();

                EventMapMarker.DeleteMapMarker();

                ZoneController.DeleteZone(isEventEnd: true);

                if (trainHeli != null && trainHeli.patrolHelicopter.IsExists())
                    trainHeli.patrolHelicopter.Kill();

                Destroy(this);
            }

            void OnDestroy()
            {
                if (controlCoroutine != null)
                    ServerMgr.Instance.StopCoroutine(controlCoroutine);
                if (createTrainCorountine != null)
                    ServerMgr.Instance.StopCoroutine(createTrainCorountine);
                if (selfDestructionCorountine != null)
                    ServerMgr.Instance.StopCoroutine(selfDestructionCorountine);
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

                JArray wearItems = WagonCustomizator.GetCustomizeNpcWearSet();

                if (wearItems == null)
                {
                    wearItems = new JArray
                        {
                            npcConfig.wearItems.Select(x => new JObject
                            {
                                ["ShortName"] = x.shortName,
                                ["SkinID"] = x.skinID
                            })
                        };
                }

                HashSet<string> states = new HashSet<string> { "IdleState", "CombatStationaryState" };
                JObject config = new JObject
                {
                    ["Name"] = npcConfig.name,
                    ["WearItems"] = wearItems,
                    ["BeltItems"] = new JArray(),
                    ["Kit"] = npcConfig.kit,
                    ["Health"] = npcConfig.health / 15,
                    ["RoamRange"] = 100,
                    ["ChaseRange"] = 100,
                    ["DamageScale"] = 1,
                    ["TurretDamageScale"] = npcConfig.turretDamageScale,
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
                trainEngine.mountPoints[0].mountable.AttemptMount(driver, true);
                trainEngine.engineController.TryStartEngine(driver);
                if (!ins.underGround) return;
                TrainDriver trainDriver = driver.gameObject.AddComponent<TrainDriver>();
                trainDriver.InitDriver(driver, trainEngine);

                driver.limitNetworking = true;
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
            internal Dictionary<HackableLockedCrate, string> lockedCrates = new Dictionary<HackableLockedCrate, string>();

            internal void Init(TrainCar baseWagonEntity, BaseWagonConfig baseWagonConfig)
            {
                this.baseWagonEntity = baseWagonEntity;
                this.baseWagonConfig = baseWagonConfig;
            }

            public virtual void DelayBuild()
            {
                if (baseWagonEntity is TrainCarUnloadable)
                    foreach (LootContainer lootContainer in GetComponentsInChildren<LootContainer>())
                        if (lootContainer.IsExists())
                            lootContainer.Kill();

                foreach (var bradleyPair in baseWagonConfig.brradleys)
                {
                    BradleyConfig bradleyConfig = ins._config.bradleysConfigs.FirstOrDefault(x => x.presetName == bradleyPair.Key);

                    if (bradleyConfig == null)
                    {
                        ins.PrintError("Bradley configuration not found!");
                        ins.EndEvent();
                        return;
                    }

                    foreach (LocationConfig locationConfig in bradleyPair.Value)
                        CreateBradley(bradleyConfig, locationConfig);
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

                    foreach (LocationConfig locationConfig in turretPair.Value)
                        CreateTurret(turretConfig, locationConfig);
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

                    foreach (LocationConfig locationConfig in npcPair.Value)
                        CreateNpc(npcConfig, locationConfig);
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

                    foreach (LocationConfig locationConfig in cratePair.Value)
                        CreateCrate(crateConfig, locationConfig);
                }

                foreach (var decorPair in baseWagonConfig.decors)
                {
                    foreach (LocationConfig locationConfig in decorPair.Value)
                        CreateDecor(decorPair.Key, locationConfig);
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

                    foreach (LocationConfig locationConfig in samsitePair.Value)
                        CreateSamsite(samSiteConfig, locationConfig);
                }

                foreach (TriggerTrainCollisions Trigger in baseWagonEntity.GetComponentsInChildren<TriggerTrainCollisions>())
                    Trigger.triggerCollider.gameObject.layer = 18;

                if (baseWagonEntity is TrainCarUnloadable)
                    UpdateUnloadableTrainCar();
            }

            void UpdateUnloadableTrainCar()
            {
                foreach (LootContainer lootContainer in GetComponentsInChildren<LootContainer>())
                    if (lootContainer != null)
                        lootContainer.inventory.SetLocked(false);

            }

            internal void UpdateLockedCrateTimeWithDelay(HackableLockedCrate hackableLockedCrate)
            {
                string cratePresetName;
                lockedCrates.TryGetValue(hackableLockedCrate, out cratePresetName);

                if (cratePresetName != null)
                {
                    CrateConfig crateConfig = ins._config.crateConfigs.FirstOrDefault(x => x.presetName == cratePresetName);
                    if (crateConfig == null)
                        return;

                    Invoke(() =>
                    {
                        if (hackableLockedCrate != null)
                            hackableLockedCrate.hackSeconds = HackableLockedCrate.requiredHackSeconds - crateConfig.crateUnlockTime;
                    }, 0.5f);
                }
            }

            void CreateTurret(TurretConfig turretConfig, LocationConfig locationConfig)
            {
                AutoTurret autoTurret = CreateEntity("assets/prefabs/npc/autoturret/autoturret_deployed.prefab", locationConfig) as AutoTurret;

                ContainerIOEntity containerIO = autoTurret.GetComponent<ContainerIOEntity>();
                if (turretConfig.shortNameWeapon != "")
                    containerIO.inventory.Insert(ItemManager.CreateByName(turretConfig.shortNameWeapon));
                if (turretConfig.shortNameAmmo != "")
                    containerIO.inventory.Insert(ItemManager.CreateByName(turretConfig.shortNameAmmo, turretConfig.countAmmo));

                containerIO.SendNetworkUpdate();
                autoTurret.InitializeHealth(turretConfig.hp, turretConfig.hp);
                autoTurret.UpdateFromInput(10, 0);
                autoTurret.isLootable = false;
                autoTurret.dropFloats = false;
                autoTurret.dropsLoot = ins._config.mainConfig.turretDropWeapon;

                turrets.Add(autoTurret);

                if (turretConfig.targetLossRange != 0)
                {
                    autoTurret.sightRange = turretConfig.targetLossRange;

                    if (autoTurret.targetTrigger == null)
                        return;
                }

                if (turretConfig.targetDetectionRange != 0 && autoTurret.targetTrigger != null)
                {
                    SphereCollider sphereCollider = autoTurret.targetTrigger.GetComponent<SphereCollider>();

                    if (sphereCollider != null)
                        sphereCollider.radius = turretConfig.targetDetectionRange;
                }
            }

            void CreateBradley(BradleyConfig bradleyConfig, LocationConfig locationConfig)
            {
                BradleyAPC bradley = CreateEntity("assets/prefabs/npc/m2bradley/bradleyapc.prefab", locationConfig) as BradleyAPC;
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
                        bradley.maxCratesToSpawn = 0;
                        foreach (var a in bradley.GetComponentsInChildren<WheelCollider>()) DestroyImmediate(a);
                        bradley.rightWheels = new WheelCollider[0];
                        bradley.leftWheels = new WheelCollider[0];
                        Rigidbody rigidbody = bradley.myRigidBody;
                        if (rigidbody != null) DestroyImmediate(rigidbody);
                        bradley.myRigidBody = baseWagonEntity.rigidBody;

                        foreach (var a in bradley.GetComponentsInChildren<TriggerBase>())
                        {
                            a.enabled = false;
                        }
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
                    Invoke(() =>
                    {
                        hackableLockedCrate.DestroyShared();
                        hackableLockedCrate.shouldDecay = false;
                        hackableLockedCrate.decayTimer = float.MaxValue;
                        hackableLockedCrate.SendNetworkUpdate();
                        hackableLockedCrate.hackSeconds = HackableLockedCrate.requiredHackSeconds - crateConfig.crateUnlockTime;
                        lockedCrates.Add(hackableLockedCrate, crateConfig.presetName);
                    }, 0.5f);
                }
                else crates.Add(lootContainer, crateConfig.presetName);

                if (crateConfig.typeLootTable == 1 || crateConfig.typeLootTable == 4)
                    Invoke(() => ins.AddToContainerItem(lootContainer.inventory, crateConfig.ownLootTable, crateConfig.typeLootTable), 2f);
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
                if (rigidbody != null)
                {
                    rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    rigidbody.isKinematic = true;
                }
                DestroyOnGroundMissing destroyOnGroundMissing = entity.GetComponentInChildren<DestroyOnGroundMissing>();
                if (destroyOnGroundMissing != null) Destroy(destroyOnGroundMissing);

                entity.SetParent(baseWagonEntity);
                entity.transform.localPosition = locationConfig.position.ToVector3();
                entity.transform.localEulerAngles = locationConfig.rotation.ToVector3();
                entity.SendNetworkUpdate();
                return entity;
            }

            void CreateNpc(NpcConfig config, LocationConfig locationConfig)
            {
                HashSet<string> states = new HashSet<string>
                    {
                        "IdleState",
                        "CombatStationaryState"
                    };

                JArray wearItems = WagonCustomizator.GetCustomizeNpcWearSet();

                if (wearItems == null)
                {
                    wearItems = new JArray
                        {
                            config.wearItems.Select(x => new JObject
                            {
                                ["ShortName"] = x.shortName,
                                ["SkinID"] = x.skinID
                            })
                        };
                }

                JObject jconfig = new JObject
                {
                    ["Name"] = config.name,
                    ["WearItems"] = wearItems,
                    ["BeltItems"] = new JArray { config.beltItems.Select(x => new JObject { ["ShortName"] = x.shortName, ["Amount"] = x.amount, ["SkinID"] = x.skinID, ["Mods"] = new JArray { x.Mods }, ["Ammo"] = string.Empty }) },
                    ["Kit"] = config.kit,
                    ["Health"] = config.health,
                    ["RoamRange"] = 0,
                    ["ChaseRange"] = 100,
                    ["SenseRange"] = config.senseRange,
                    ["ListenRange"] = config.senseRange / 2f,
                    ["AttackRangeMultiplier"] = config.attackRangeMultiplier,
                    ["VisionCone"] = config.visionCone,
                    ["DamageScale"] = config.damageScale,
                    ["TurretDamageScale"] = config.turretDamageScale,
                    ["AimConeScale"] = config.aimConeScale,
                    ["DisableRadio"] = config.disableRadio,
                    ["CanRunAwayWater"] = true,
                    ["CanSleep"] = false,
                    ["Speed"] = 0,
                    ["AreaMask"] = 1,
                    ["AgentTypeID"] = -1372625422,
                    ["HomePosition"] = string.Empty,
                    ["MemoryDuration"] = config.memoryDuration,
                    ["States"] = new JArray { states }
                };

                ScientistNPC scientist = (ScientistNPC)ins.NpcSpawn.Call("SpawnNpc", baseWagonEntity.transform.position, jconfig);
                scientist.SetParent(baseWagonEntity);
                scientist.transform.localPosition = locationConfig.position.ToVector3();
                scientist.transform.localEulerAngles = locationConfig.rotation.ToVector3();
                npcs.Add(scientist);
            }

            internal void DestroyDecorEntities()
            {
                foreach (BaseEntity decorEntity in decorEntities) if (decorEntity.IsExists()) decorEntity.Kill();
            }

            internal virtual void DestroyWagon()
            {
                foreach (ScientistNPC scientistNPC in npcs) if (scientistNPC.IsExists()) scientistNPC.Kill();
                foreach (AutoTurret autoTurret in turrets)
                {
                    if (autoTurret.IsExists())
                    {
                        autoTurret.dropsLoot = false;
                        autoTurret.Kill();
                    }
                }
                foreach (BradleyAPC bradleyAPC in bradleys) if (bradleyAPC.IsExists()) bradleyAPC.Kill();
                foreach (SamSite samSite in samsites) if (samSite.IsExists()) samSite.Kill();
                DestroyDecorEntities();
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
            internal PatrolHelicopter patrolHelicopter;
            HeliConfig heliConfig;
            int ounsideTime = 0;

            internal void InitHelicopter(PatrolHelicopter patrolHelicopter, HeliConfig heliConfig, TrainCar targetEntity)
            {
                this.heliConfig = heliConfig;
                this.patrolHelicopter = patrolHelicopter;
                this.targetEntity = targetEntity;

                patrolHelicopterAI = patrolHelicopter.myAI;
                patrolHelicopter.startHealth = heliConfig.hp;
                patrolHelicopter.InitializeHealth(heliConfig.hp, heliConfig.hp);
                patrolHelicopter.maxCratesToSpawn = heliConfig.cratesAmount;
                patrolHelicopter.bulletDamage = heliConfig.bulletDamage;
                patrolHelicopter.bulletSpeed = heliConfig.bulletSpeed;
                patrolHelicopter.myAI.isRetiring = true;
                var weakspots = patrolHelicopter.weakspots;
                if (weakspots != null && weakspots.Length > 1)
                {
                    weakspots[0].maxHealth = heliConfig.mainRotorHealth;
                    weakspots[0].health = heliConfig.mainRotorHealth;
                    weakspots[1].maxHealth = heliConfig.rearRotorHealth;
                    weakspots[1].health = heliConfig.rearRotorHealth;
                }

                InvokeRepeating(ControllerWhetTrainStopped, 1, 1);
            }

            internal void SetTarget(BasePlayer player)
            {
                if (!player.IsRealPlayer())
                    return;

                patrolHelicopterAI.SetTargetDestination(player.transform.position);
                patrolHelicopterAI._targetList.Add(new PatrolHelicopterAI.targetinfo(player, player));
            }

            internal void OnTrainStopOrStart()
            {
                patrolHelicopterAI.ExitCurrentState();
            }

            internal ulong GetHeliNetId()
            {
                return patrolHelicopter.net.ID.Value;
            }

            void ControllerWhetTrainStopped()
            {
                if (!targetEntity.IsExists() || patrolHelicopter.myAI._currentState == PatrolHelicopterAI.aiState.DEATH || patrolHelicopter.myAI._currentState == PatrolHelicopterAI.aiState.STRAFE)
                    return;

                Vector3 targetPosition = targetEntity.transform.position;

                if (ins.train.IsTrainStop())
                {
                    if (patrolHelicopterAI.leftGun.HasTarget() || patrolHelicopterAI.rightGun.HasTarget())
                    {
                        if (Vector3.Distance(targetPosition, patrolHelicopter.transform.position) > heliConfig.distance)
                        {
                            ounsideTime++;
                            if (ounsideTime > heliConfig.outsideTime)
                                patrolHelicopter.myAI.State_Move_Enter(targetPosition);
                        }
                        else ounsideTime = 0;
                    }
                    else if (Vector3.Distance(targetPosition, patrolHelicopter.transform.position) > heliConfig.distance)
                    {
                        patrolHelicopterAI.State_Move_Enter(targetPosition);
                        ounsideTime = 0;
                    }
                    else ounsideTime = 0;
                }
                else
                    patrolHelicopter.myAI.State_Move_Enter(targetPosition);
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

        sealed class ZoneController : FacepunchBehaviour
        {
            static ZoneController zoneController;
            static float lastZoneCreationTime;

            SphereCollider sphereCollider;
            HashSet<BaseEntity> spheres = new HashSet<BaseEntity>();
            HashSet<BasePlayer> playersInZone = new HashSet<BasePlayer>();

            static HashSet<ulong> pveModOwners = new HashSet<ulong>();
            static ulong pveModOwner = 0;
            static float timeScienceLastZoneCreated;

            internal static void CreateZone(Vector3 position, ulong stopperUserId)
            {
                if (zoneController != null)
                    UnityEngine.GameObject.Destroy(zoneController.gameObject);

                GameObject gameObject = new GameObject();
                gameObject.transform.position = position;
                gameObject.layer = (int)Rust.Layer.Reserved1;

                zoneController = gameObject.AddComponent<ZoneController>();
                zoneController.Init(stopperUserId);
            }

            internal static bool IsZoneCreated()
            {
                return zoneController != null;
            }

            internal static HashSet<BasePlayer> GetPlayersInZone()
            {
                if (zoneController == null)
                    return new HashSet<BasePlayer>();

                return zoneController.playersInZone;
            }

            internal static bool IsPlayerInZone(ulong userID)
            {
                return zoneController != null && zoneController.playersInZone.Any(x => x != null && x.userID == userID);
            }

            internal static string GetEventOwnerPlayerName()
            {
                if (zoneController == null)
                {
                    if (pveModOwner != 0)
                    {
                        if (Time.realtimeSinceStartup - timeScienceLastZoneCreated < ins._config.supportedPluginsConfig.pveMode.timeExitOwner)
                        {
                            BasePlayer player = BasePlayer.FindByID(pveModOwner);
                            if (player != null)
                                return player.displayName;
                        }
                        else
                            pveModOwner = 0;
                    }
                    return "";
                }

                timeScienceLastZoneCreated = Time.realtimeSinceStartup;

                BasePlayer pveModeOwnerPlayer = PveModeController.GetEventOwner();

                if (pveModeOwnerPlayer.IsRealPlayer())
                {
                    pveModOwner = pveModeOwnerPlayer.userID;
                    return pveModeOwnerPlayer.displayName;
                }
                else
                {
                    pveModOwner = 0;
                    return "";
                }
            }

            internal static void OnPlayerLeaveZone(BasePlayer player)
            {
                if (zoneController == null) return;
                zoneController.playersInZone.Remove(player);
                if (ins._config.guiConfig.IsGUI) GuiManager.DestroyGuiForPlayer(player);
                if (ins._config.zoneConfig.isCreateZonePVP) NotifyManager.SendMessageToPlayer(player, "ExitPVP", ins._config.prefix);
            }

            void Init(ulong stopperUserId)
            {
                CreateTriggerSphere();

                if (ins._config.supportedPluginsConfig.pveMode.enable && ins.plugins.Exists("PveMode"))
                {
                    if (stopperUserId != 0 && pveModOwner == 0 && (bool)ins.PveMode.Call("CanTimeOwner", ins.Name, stopperUserId, ins._config.supportedPluginsConfig.pveMode.cooldownOwner))
                        pveModOwner = stopperUserId;
                    CreatePveModeZone();
                }
                else if (ins._config.zoneConfig.isDome)
                    CreateSphere();
            }

            void CreateTriggerSphere()
            {
                sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = ins._config.zoneConfig.radius;
            }

            void CreatePveModeZone()
            {
                Dictionary<string, object> config = new Dictionary<string, object>
                {
                    ["Damage"] = ins._config.supportedPluginsConfig.pveMode.damage,
                    ["ScaleDamage"] = ins._config.supportedPluginsConfig.pveMode.scaleDamage,
                    ["LootCrate"] = ins._config.supportedPluginsConfig.pveMode.lootCrate,
                    ["HackCrate"] = ins._config.supportedPluginsConfig.pveMode.hackCrate,
                    ["LootNpc"] = ins._config.supportedPluginsConfig.pveMode.lootNpc,
                    ["DamageNpc"] = ins._config.supportedPluginsConfig.pveMode.damageNpc,
                    ["DamageTank"] = ins._config.supportedPluginsConfig.pveMode.damageTank,
                    ["DamageHelicopter"] = ins._config.supportedPluginsConfig.pveMode.damageHeli,
                    ["DamageTurret"] = ins._config.supportedPluginsConfig.pveMode.damageTurret,
                    ["TargetNpc"] = ins._config.supportedPluginsConfig.pveMode.targetNpc,
                    ["TargetTank"] = ins._config.supportedPluginsConfig.pveMode.targetTank,
                    ["TargetHelicopter"] = ins._config.supportedPluginsConfig.pveMode.targetHeli,
                    ["TargetTurret"] = ins._config.supportedPluginsConfig.pveMode.targetTurret,
                    ["CanEnter"] = ins._config.supportedPluginsConfig.pveMode.canEnter,
                    ["CanEnterCooldownPlayer"] = ins._config.supportedPluginsConfig.pveMode.canEnterCooldownPlayer,
                    ["TimeExitOwner"] = ins._config.supportedPluginsConfig.pveMode.timeExitOwner,
                    ["AlertTime"] = ins._config.supportedPluginsConfig.pveMode.alertTime,
                    ["RestoreUponDeath"] = ins._config.supportedPluginsConfig.pveMode.restoreUponDeath,
                    ["CooldownOwner"] = ins._config.supportedPluginsConfig.pveMode.cooldownOwner,
                    ["Darkening"] = ins._config.supportedPluginsConfig.pveMode.darkening
                };

                HashSet<ulong> npcs = new HashSet<ulong>();
                HashSet<ulong> bradleys = new HashSet<ulong>();
                HashSet<ulong> helicopters = new HashSet<ulong>();
                HashSet<ulong> crates = new HashSet<ulong>();
                HashSet<ulong> turrets = new HashSet<ulong>();

                if (ins.train.trainHeli != null)
                {
                    helicopters.Add(ins.train.trainHeli.GetHeliNetId());
                }

                foreach (ScientistNPC scientistNPC in ins.train.locomotive.npcs)
                    if (scientistNPC.IsExists())
                        npcs.Add(scientistNPC.net.ID.Value);
                foreach (LootContainer lootContainer in ins.train.locomotive.crates.Keys)
                    if (lootContainer.IsExists())
                        crates.Add(lootContainer.net.ID.Value);
                foreach (LootContainer lootContainer in ins.train.locomotive.lockedCrates.Keys)
                    if (lootContainer.IsExists())
                        crates.Add(lootContainer.net.ID.Value);
                foreach (BradleyAPC bradleyAPC in ins.train.locomotive.bradleys)
                    if (bradleyAPC.IsExists())
                        bradleys.Add(bradleyAPC.net.ID.Value);
                foreach (AutoTurret autoTurret in ins.train.locomotive.turrets)
                    if (autoTurret.IsExists())
                        turrets.Add(autoTurret.net.ID.Value);
                foreach (SamSite samSite in ins.train.locomotive.samsites)
                    if (samSite.IsExists())
                        turrets.Add(samSite.net.ID.Value);

                foreach (BaseWagon baseWagon in ins.train.wagons)
                {
                    if (baseWagon == null) continue;

                    foreach (ScientistNPC scientistNPC in baseWagon.npcs)
                        if (scientistNPC.IsExists())
                            npcs.Add(scientistNPC.net.ID.Value);
                    foreach (LootContainer lootContainer in baseWagon.crates.Keys)
                        if (lootContainer.IsExists())
                            crates.Add(lootContainer.net.ID.Value);
                    foreach (LootContainer lootContainer in baseWagon.lockedCrates.Keys)
                        if (lootContainer.IsExists())
                            crates.Add(lootContainer.net.ID.Value);
                    foreach (BradleyAPC bradleyAPC in baseWagon.bradleys)
                        if (bradleyAPC.IsExists())
                            bradleys.Add(bradleyAPC.net.ID.Value);
                    foreach (AutoTurret autoTurret in baseWagon.turrets)
                        if (autoTurret.IsExists())
                            turrets.Add(autoTurret.net.ID.Value);
                    foreach (SamSite samSite in baseWagon.samsites)
                        if (samSite.IsExists())
                            turrets.Add(samSite.net.ID.Value);
                }

                BasePlayer playerOwner = null;
                if (pveModOwner != 0 && Time.realtimeSinceStartup - lastZoneCreationTime < ins._config.supportedPluginsConfig.pveMode.timeExitOwner)
                    playerOwner = BasePlayer.FindByID(pveModOwner);

                ins.PveMode.Call("EventAddPveMode", ins.Name, config, gameObject.transform.position, ins._config.zoneConfig.radius, crates, npcs, bradleys, helicopters, turrets, pveModOwners, playerOwner);
            }

            void CreateSphere()
            {
                for (int i = 0; i < ins._config.zoneConfig.darkening; i++)
                {
                    BaseEntity sphere = GameManager.server.CreateEntity("assets/prefabs/visualization/sphere.prefab", gameObject.transform.position);
                    SphereEntity entity = sphere.GetComponent<SphereEntity>();
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
                if (player.IsRealPlayer())
                {
                    playersInZone.Add(player);
                    if (ins._config.guiConfig.IsGUI) GuiManager.CountdownGUIForNewPlayer(player);
                    if (ins._config.zoneConfig.isCreateZonePVP) NotifyManager.SendMessageToPlayer(player, "EnterPVP", ins._config.prefix);
                }
            }

            void OnTriggerExit(Collider other)
            {
                if (other.ToBaseEntity() == null) return;
                BasePlayer player = other.ToBaseEntity() as BasePlayer;
                if (player.IsRealPlayer())
                    OnPlayerLeaveZone(player);
            }

            internal static void DeleteZone(bool isEventEnd = false)
            {
                if (zoneController != null)
                    Destroy(zoneController.gameObject);
                if (isEventEnd)
                {
                    if (ins._config.supportedPluginsConfig.pveMode.enable && ins.plugins.Exists("PveMode"))
                        ins.PveMode.Call("EventAddCooldown", ins.Name, pveModOwners, ins._config.supportedPluginsConfig.pveMode.cooldownOwner);

                    ClearZoneData();
                }
                zoneController = null;
            }

            internal static void ClearZoneData()
            {
                pveModOwners = new HashSet<ulong>();
                pveModOwner = 0;
            }

            void OnDestroy()
            {
                foreach (BaseEntity sphere in spheres)
                    if (sphere.IsExists())
                        sphere.Kill();

                GuiManager.DestroyGuiForAllPLayers();
                if (ins._config.supportedPluginsConfig.pveMode.enable && ins.plugins.Exists("PveMode"))
                {
                    if (ins.isEventActive)
                    {
                        pveModOwners = (HashSet<ulong>)ins.PveMode.Call("GetEventOwners", ins.Name);

                        if (pveModOwners == null)
                            pveModOwners = new HashSet<ulong>();

                        pveModOwner = (ulong)ins.PveMode.Call("GetEventOwner", ins.Name);
                        ins.PveMode.Call("EventRemovePveMode", ins.Name, false);
                    }
                }

                lastZoneCreationTime = Time.realtimeSinceStartup;
            }
        }

        static class GuiManager
        {
            internal static void UpdateCountdownGui()
            {
                if (!ins._config.guiConfig.IsGUI) return;

                int time = ins.train.GetEventTime();
                if (time < 0) time = 0;

                HashSet<BasePlayer> playersInZone = ZoneController.GetPlayersInZone();

                foreach (BasePlayer player in playersInZone)
                {
                    if (player != null)
                        CountdownGUI(player, time);
                }
            }

            internal static void CountdownGUIForNewPlayer(BasePlayer player)
            {
                int eventTime = ins.train.GetEventTime();
                CountdownGUI(player, eventTime);
            }

            internal static void CountdownGUI(BasePlayer player, int eventTime)
            {
                MessageGUI(player, GetMessage("GUI", player.UserIDString, NotifyManager.GetTimeMessage(player.UserIDString, eventTime)));
            }

            internal static void MessageGUI(BasePlayer player, string text)
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

            internal static void DestroyGuiForAllPLayers()
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    if (player != null)
                        DestroyGuiForPlayer(player);
                }
            }

            internal static void DestroyGuiForPlayer(BasePlayer player)
            {
                CuiHelper.DestroyUi(player, "TrainGui");
            }
        }

        class EventMapMarker : FacepunchBehaviour
        {
            static EventMapMarker eventMapMarker;

            MapMarkerGenericRadius radiusMarker;
            InvisibleVendingMachine invisibleVendingMachine;
            VendingMachineMapMarker nameMarker;
            VendingMachineMapMarker timeMarker;
            Coroutine updateCounter;

            internal static EventMapMarker CreateMarker()
            {
                GameObject gameObject = new GameObject();
                gameObject.layer = (int)Rust.Layer.Reserved1;
                eventMapMarker = gameObject.AddComponent<EventMapMarker>();
                eventMapMarker.Init();
                return eventMapMarker;
            }

            void Init()
            {
                Vector3 eventPosition = ins.train.locomotive.transform.position;
                CreateRadiusMarker(eventPosition);

                if (ins._config.markerConfig.useShopMarker)
                {
                    CreateInvisibleVendingMachine(eventPosition);
                    GetTimeMapMarker();
                    nameMarker = SpawnVendingMarker(eventPosition);
                    updateCounter = ServerMgr.Instance.StartCoroutine(MarkerUpdateCounter());
                }
            }

            void CreateInvisibleVendingMachine(Vector3 position)
            {
                invisibleVendingMachine = BuildManager.SpawnEntity("assets/prefabs/deployable/vendingmachine/npcvendingmachines/shopkeeper_vm_invis.prefab", position, Quaternion.identity,0, false) as InvisibleVendingMachine;
                
                invisibleVendingMachine.SetFlag(BaseEntity.Flags.Busy, true);
                timeMarker = invisibleVendingMachine.GetComponent<VendingMachineMapMarker>();
                invisibleVendingMachine.CancelInvoke(invisibleVendingMachine.InstallFromVendingOrders);

                for (int i = 0; i < 5; i++)
                    invisibleVendingMachine.AddItemForSale(696029452, 0, 696029452, 0, 0);
            }

            void GetTimeMapMarker()
            {
                for (int i = 0; i < MapMarker.serverMapMarkers.Count; i++)
                {
                    MapMarker mapMarker = MapMarker.serverMapMarkers[i];
                    VendingMachineMapMarker vendingMachineMapMarker = mapMarker as VendingMachineMapMarker;

                    if (vendingMachineMapMarker != null && vendingMachineMapMarker.server_vendingMachine == invisibleVendingMachine)
                    {
                        timeMarker = vendingMachineMapMarker;
                        return;
                    }
                }
            }

            void CreateRadiusMarker(Vector3 position)
            {
                if (!ins._config.markerConfig.useRingMarker)
                    return;

                radiusMarker = GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", position) as MapMarkerGenericRadius;
                radiusMarker.enableSaving = false;
                radiusMarker.Spawn();
                radiusMarker.radius = ins._config.markerConfig.radius;
                radiusMarker.alpha = ins._config.markerConfig.alpha;
                radiusMarker.color1 = new Color(ins._config.markerConfig.color1.r, ins._config.markerConfig.color1.g, ins._config.markerConfig.color1.b);
                radiusMarker.color2 = new Color(ins._config.markerConfig.color2.r, ins._config.markerConfig.color2.g, ins._config.markerConfig.color2.b);
            }

            VendingMachineMapMarker SpawnVendingMarker(Vector3 position)
            {
                VendingMachineMapMarker vendingMarker = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab", position) as VendingMachineMapMarker;
                vendingMarker.enableSaving = false;
                vendingMarker.SetVendingMachine(invisibleVendingMachine, "");
                vendingMarker.Spawn();
                return vendingMarker;
            }

            IEnumerator MarkerUpdateCounter()
            {
                while (ins.train != null)
                {
                    Vector3 position = Vector3.zero;
                    if (ins.train.locomotive != null)
                        position = ins.train.locomotive.transform.position;
                    else
                    {
                        DeleteMapMarker();
                        break;
                    }

                    if (position != Vector3.zero)
                    {
                        UpdateVendingMarkers(position);
                        yield return CoroutineEx.waitForSeconds(0.15f);
                        UpdateRadiusMarker(position);
                    }

                    yield return CoroutineEx.waitForSeconds(5f);
                }
            }

            void UpdateRadiusMarker(Vector3 position)
            {
                if (!radiusMarker.IsExists())
                    return;

                radiusMarker.transform.position = position;
                radiusMarker.SendUpdate();
                radiusMarker.SendNetworkUpdate();
            }

            void UpdateVendingMarkers(Vector3 position)
            {
                UpdateTimeMarker(position);
                UpdateNameMarker(position);
            }

            void UpdateTimeMarker(Vector3 position)
            {
                if (invisibleVendingMachine == null)
                    return;

                int eventTime = ins.train.GetEventTime();
                invisibleVendingMachine.transform.position = position;
                invisibleVendingMachine.shopName = $"{NotifyManager.GetTimeMessage(null, eventTime)}";

                if (timeMarker != null)
                {
                    timeMarker.transform.position = position;
                }

                invisibleVendingMachine.UpdateMapMarker();
            }

            void UpdateNameMarker(Vector3 position)
            {
                if (nameMarker == null)
                    return;

                nameMarker.transform.position = position;
                nameMarker.markerShopName = $"{ins.train.trainConfig.trainName}";
                nameMarker.limitNetworking = true;
                nameMarker.limitNetworking = false;
                nameMarker.SendNetworkUpdate();
            }

            internal static void DeleteMapMarker()
            {
                if (eventMapMarker != null)
                    eventMapMarker.Delete();
            }

            void Delete()
            {
                if (invisibleVendingMachine.IsExists())
                    invisibleVendingMachine.Kill();

                if (nameMarker.IsExists())
                    nameMarker.Kill();

                if (radiusMarker.IsExists())
                    radiusMarker.Kill();

                if (updateCounter != null)
                    ServerMgr.Instance.StopCoroutine(updateCounter);

                Destroy(eventMapMarker.gameObject);
            }
        }

        static class NotifyManager
        {
            internal static void PrintError(BasePlayer player, string langKey, params object[] args)
            {
                if (player == null) ins.PrintError(ClearColorAndSize(GetMessage(langKey, null, args)));
                else ins.PrintToChat(player, GetMessage(langKey, player.UserIDString, args));
            }

            internal static void PrintLogMessage(string langKey, params object[] args)
            {
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

                if (ins._config.notifyConfig.chat) ins.PrintToChat(player, GetMessage(langKey, player.UserIDString, args));
                if (ins._config.supportedPluginsConfig.GUIAnnouncements.isGUIAnnouncements) ins.GUIAnnouncements?.Call("CreateAnnouncement", ClearColorAndSize(GetMessage(langKey, player.UserIDString, args)), ins._config.supportedPluginsConfig.GUIAnnouncements.bannerColor, ins._config.supportedPluginsConfig.GUIAnnouncements.textColor, player, ins._config.supportedPluginsConfig.GUIAnnouncements.apiAdjustVPosition);
                if (ins._config.supportedPluginsConfig.notify.isNotify) player.SendConsoleCommand($"notify.show {ins._config.supportedPluginsConfig.notify.type} {ClearColorAndSize(GetMessage(langKey, player.UserIDString, args))}");
            }

            internal static void SendDiscordMessage(string langKey, params object[] args)
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

            static bool CanSendDiscordMessage() => ins._config.supportedPluginsConfig.discord.isDiscord && !string.IsNullOrEmpty(ins._config.supportedPluginsConfig.discord.webhookUrl) && ins._config.supportedPluginsConfig.discord.webhookUrl != "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks";

            internal static string GetTimeMessage(string userIDString, int seconds)
            {
                string message = "";

                TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
                if (timeSpan.Hours > 0) message += $" {timeSpan.Hours} {GetMessage("Hour", userIDString)}";
                if (timeSpan.Minutes > 0) message += $" {timeSpan.Minutes} {GetMessage("Min", userIDString)}";
                if (message == "") message += $" {timeSpan.Seconds} {GetMessage("Sec", userIDString)}";

                return message;
            }
        }

        static class EconomyManager
        {
            static readonly Dictionary<ulong, double> _playersBalance = new Dictionary<ulong, double>();

            internal static void ActionEconomy(ulong playerId, string type, string arg = "")
            {
                switch (type)
                {
                    case "Crates":
                        double economyCrateData;
                        if (ins._config.supportedPluginsConfig.economy.crates.TryGetValue(arg, out economyCrateData)) AddBalance(playerId, economyCrateData);
                        break;
                    case "Npc":
                        AddBalance(playerId, ins._config.supportedPluginsConfig.economy.npcPoint);
                        break;
                    case "LockedCrate":
                        AddBalance(playerId, ins._config.supportedPluginsConfig.economy.lockedCratePoint);
                        break;
                    case "Turret":
                        AddBalance(playerId, ins._config.supportedPluginsConfig.economy.turretPoint);
                        break;
                    case "Bradley":
                        AddBalance(playerId, ins._config.supportedPluginsConfig.economy.bradleyPoint);
                        break;
                    case "Heli":
                        AddBalance(playerId, ins._config.supportedPluginsConfig.economy.heliPoint);
                        break;
                }
            }

            static void AddBalance(ulong playerId, double balance)
            {
                if (balance == 0) return;
                if (_playersBalance.ContainsKey(playerId)) _playersBalance[playerId] += balance;
                else _playersBalance.Add(playerId, balance);
            }

            internal static void SendBalance()
            {
                if (!ins._config.supportedPluginsConfig.economy.enable || _playersBalance.Count == 0)
                {
                    _playersBalance.Clear();
                    return;
                }
                foreach (KeyValuePair<ulong, double> dic in _playersBalance)
                {
                    if (dic.Value < ins._config.supportedPluginsConfig.economy.minEconomyPiont) continue;
                    int intCount = Convert.ToInt32(dic.Value);
                    if (ins._config.supportedPluginsConfig.economy.plugins.Contains("Economics") && ins.plugins.Exists("Economics") && dic.Value > 0) ins.Economics.Call("Deposit", dic.Key.ToString(), dic.Value);
                    if (ins._config.supportedPluginsConfig.economy.plugins.Contains("Server Rewards") && ins.plugins.Exists("ServerRewards") && intCount > 0) ins.ServerRewards.Call("AddPoints", dic.Key, intCount);
                    if (ins._config.supportedPluginsConfig.economy.plugins.Contains("IQEconomic") && ins.plugins.Exists("IQEconomic") && intCount > 0) ins.IQEconomic.Call("API_SET_BALANCE", dic.Key, intCount);
                    BasePlayer player = BasePlayer.FindByID(dic.Key);
                    if (player != null) NotifyManager.SendMessageToPlayer(player, "SendEconomy", ins._config.prefix, dic.Value);
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

                if (max >= ins._config.supportedPluginsConfig.economy.minCommandPoint) foreach (string command in ins._config.supportedPluginsConfig.economy.commands) ins.Server.Command(command.Replace("{steamid}", $"{winnerId}"));
                _playersBalance.Clear();
                DefineEventWinner();
            }

            static void DefineEventWinner()
            {
                var winnerPair = _playersBalance.Max(x => (float)x.Value);
                if (winnerPair.Value > 0) Interface.CallHook("OnArmoredTrainEventWin", winnerPair.Key);
            }
        }

        class WagonCustomizator : FacepunchBehaviour
        {
            static CustomizeProfile customizeProfile;
            static HashSet<string> fireEntities = new HashSet<string>
                {
                    "hobobarrel.deployed",
                    "largecandleset",
                    "jackolantern.happy",
                    "skullspikes.candles.deployed",
                    "skull_fire_pit"
                };

            Train train;
            Coroutine updateCoroutine;
            HashSet<BaseEntity> lightEntities = new HashSet<BaseEntity>();
            HashSet<NeonSign> neonSigns = new HashSet<NeonSign>();
            float lastNeonUpdateTime;
            ItemThrower itemThrower;
            CustomFirework customFirework;
            bool isLightEnable;

            internal static void LoadCurrentCustomizationProfile()
            {
                if (ins._config.customizationConfig.profileName == "")
                    return;

                customizeProfile = LoadProfile(ins._config.customizationConfig.profileName);

                if (!IsCustomizationCanApplied())
                {
                    NotifyManager.PrintError(null, "DataFileNotFound_Exeption", ins._config.customizationConfig.profileName);
                    return;
                }
            }

            internal static bool IsCustomizationCanApplied()
            {
                return customizeProfile != null && customizeProfile.wagonPresets != null && customizeProfile.wagonPresets.Count > 0;
            }

            static CustomizeProfile LoadProfile(string profileName)
            {
                string filePath = $"{ins.Name}/{profileName}";
                return Interface.Oxide.DataFileSystem.ReadObject<CustomizeProfile>(filePath);
            }

            internal static JArray GetCustomizeNpcWearSet()
            {
                if (customizeProfile == null || customizeProfile.npcPresets == null)
                    return null;

                CustomizeNpcConfig randomCustomizeNpcConfig = customizeProfile.npcPresets.GetRandom();

                if (randomCustomizeNpcConfig == null)
                    return null;

                return new JArray
                    {
                        randomCustomizeNpcConfig.customWearItems.Select(x => new JObject
                        {
                            ["ShortName"] = x.shortName,
                            ["SkinID"] = x.skinID
                        })
                    };
            }

            internal void Init(Train train)
            {
                this.train = train;

                DecorateTrain();
                updateCoroutine = ServerMgr.Instance.StartCoroutine(UpdateCoroutine());
            }

            void DecorateTrain()
            {
                if (train == null)
                    return;

                DecorateWagon(train.locomotive, train.locomotive.baseWagonConfig.presetName);

                foreach (BaseWagon baseWagon in train.wagons)
                    DecorateWagon(baseWagon, baseWagon.baseWagonConfig.presetName);

                SpawnCustomCanonShell();
            }

            void DecorateWagon(BaseWagon baseWagon, string wagonPresetName)
            {
                TrainCar trainCar = baseWagon.baseWagonEntity;

                if (customizeProfile == null || customizeProfile.wagonPresets == null)
                    return;

                WagonCustomizationData wagonCustomizationData = customizeProfile.wagonPresets.FirstOrDefault(x => x.isEnabled && x.prefabName == trainCar.ShortPrefabName && !x.wagonExceptions.Contains(wagonPresetName) && (x.wagonOnly == null || x.wagonOnly.Count == 0 || x.wagonOnly.Contains(wagonPresetName)));

                if (wagonCustomizationData == null)
                    return;

                if (wagonCustomizationData.decorEntityConfigs != null)
                {
                    List<DecorEntityConfig> decorEntityConfigList = wagonCustomizationData.decorEntityConfigs.ToList();

                    for (int i = 0; i < decorEntityConfigList.Count; i++)
                    {
                        DecorEntityConfig decorEntityConfig = decorEntityConfigList[i];
                        if (i > 0)
                        {
                            DecorEntityConfig previousDecorConfig = decorEntityConfigList[i - 1];
                            if (previousDecorConfig.position == decorEntityConfig.position && previousDecorConfig.rotation == decorEntityConfig.rotation && previousDecorConfig.prefabName == decorEntityConfig.prefabName)
                                continue;
                        }
                        SpawnDecorEntity(decorEntityConfig, trainCar);
                    }
                }
                if (wagonCustomizationData.signConfigs != null)
                {
                    foreach (DecorEntityConfig decorEntityConfig in wagonCustomizationData.signConfigs)
                        SpawnDecorEntity(decorEntityConfig, trainCar);
                }

                if (wagonCustomizationData.isBaseDecorDisable)
                    baseWagon.DestroyDecorEntities();
            }

            void SpawnDecorEntity(DecorEntityConfig decorEntityConfig, TrainCar trainCar)
            {
                Vector3 localPosition = decorEntityConfig.position.ToVector3();
                Vector3 localRotation = decorEntityConfig.rotation.ToVector3();
                bool isNoDecorEntity = decorEntityConfig.prefabName.Contains("neon");

                BaseEntity entity = BuildManager.SpawnChildEntity(trainCar, decorEntityConfig.prefabName, localPosition, localRotation, decorEntityConfig.skin, !isNoDecorEntity);
                if (entity != null)
                    UpdateDecorEntity(entity, decorEntityConfig);
            }

            void UpdateDecorEntity(BaseEntity entity, DecorEntityConfig decorEntityConfig)
            {
                entity.SetFlag(BaseEntity.Flags.Busy, true);
                entity.SetFlag(BaseEntity.Flags.Locked, true);

                NeonSign neonSign = entity as NeonSign;
                if (neonSign != null)
                    UpdateNeonSign(neonSign, decorEntityConfig);

                else
                    UpdateCommonEntities(entity);
            }

            void UpdateNeonSign(NeonSign neonSign, DecorEntityConfig decorEntityConfig)
            {
                PaintedSignConfig paintedSignConfig = decorEntityConfig as PaintedSignConfig;

                if (paintedSignConfig != null)

                    if (ins._config.customizationConfig.isNeonSignsEnable)
                        SignPainter.UpdateNeonSign(neonSign, paintedSignConfig.imageName);

                neonSigns.Add(neonSign);
            }

            void UpdateCommonEntities(BaseEntity entity)
            {
                if (entity.ShortPrefabName == "skulltrophy.deployed")
                    entity.SetFlag(BaseEntity.Flags.Reserved1, true);

                if (ins._config.customizationConfig.isBoilersEnable && entity.ShortPrefabName == "cursedcauldron.deployed")
                    UpdateLightEntity(entity);

                else if (ins._config.customizationConfig.isElectricFurnacesEnable && entity.ShortPrefabName == "electricfurnace.deployed")
                    UpdateLightEntity(entity);

                else if (ins._config.customizationConfig.isFireEnable && IsEntityFire(entity.ShortPrefabName))
                    UpdateLightEntity(entity);

                else if (entity.ShortPrefabName == "industrial.wall.lamp.red.deployed")
                    UpdateLightEntity(entity);

                else if (entity.ShortPrefabName == "xmas_tree.deployed")
                    DecorateChristmasTree(entity);

                if (entity.ShortPrefabName == "wooden_crate_gingerbread" || entity.ShortPrefabName == "gingerbread_barricades_snowman" || entity.ShortPrefabName == "gingerbread_barricades_house" || entity.ShortPrefabName == "gingerbread_barricades_tree")
                    entity.gameObject.layer = 12;
            }

            bool IsEntityFire(string shortPrefabName)
            {
                return fireEntities.Contains(shortPrefabName);
            }

            void UpdateLightEntity(BaseEntity entity)
            {
                if (ins._config.customizationConfig.isLightOnlyAtNight)
                    lightEntities.Add(entity);
                else
                    entity.SetFlag(BaseEntity.Flags.On, true);
            }

            void DecorateChristmasTree(BaseEntity christmasTree)
            {
                christmasTree.SetFlag(BaseEntity.Flags.Reserved1, true);
                christmasTree.SetFlag(BaseEntity.Flags.Reserved2, true);
                christmasTree.SetFlag(BaseEntity.Flags.Reserved3, true);
                christmasTree.SetFlag(BaseEntity.Flags.Reserved4, true);
                christmasTree.SetFlag(BaseEntity.Flags.Reserved5, true);
                christmasTree.SetFlag(BaseEntity.Flags.Reserved6, true);
                christmasTree.SetFlag(BaseEntity.Flags.Reserved7, true);
            }

            void SpawnCustomCanonShell()
            {
                if (!ins._config.customizationConfig.giftCannonSetting.isGiftCannonEnable && !ins._config.customizationConfig.fireworksSettings.isFireworksOn)
                    return;

                Vector3 canonShellPosition = train.locomotive.trainEngine.ShortPrefabName == "locomotive.entity" ? new Vector3(0, 4.649f, 4.478f) : new Vector3(0.719f, 3.814f, 3.513f);
                BuildManager.SpawnChildEntity(train.locomotive.trainEngine, "assets/prefabs/deployable/fireworks/mortarpattern.prefab", canonShellPosition, Vector3.zero, 0, true);

                if (ins._config.customizationConfig.giftCannonSetting.isGiftCannonEnable)
                    itemThrower = new ItemThrower(train.locomotive.trainEngine, canonShellPosition);

                if (ins._config.customizationConfig.fireworksSettings.isFireworksOn && customizeProfile.fireworkConfigs != null && customizeProfile.fireworkConfigs.Count > 0)
                    customFirework = new CustomFirework(train.locomotive.trainEngine, canonShellPosition);
            }

            IEnumerator UpdateCoroutine()
            {
                while (train != null)
                {
                    PeriodicUpdateOfCustomizationEntities();
                    yield return CoroutineEx.waitForSeconds(1);
                }
            }

            void PeriodicUpdateOfCustomizationEntities()
            {
                UptateTimeLight();
                TryUpdateSignEntities();

                if (itemThrower != null)
                    itemThrower.UpdateItemThrower();

                if (customFirework != null)
                    customFirework.UpdateCustomFirework();
            }

            void UptateTimeLight()
            {
                if (ins._config.customizationConfig.isLightOnlyAtNight)
                {

                    if (isLightEnable)
                    {
                        if (ConVar.Env.time > 9 && ConVar.Env.time < 18)
                        {
                            TurnLight(false);
                        }
                    }
                    else
                    {
                        if (ConVar.Env.time < 9 || ConVar.Env.time > 18)
                        {
                            TurnLight(true);
                        }
                    }
                }
            }

            void TurnLight(bool enable)
            {
                isLightEnable = enable;

                foreach (BaseEntity entity in lightEntities)
                    if (entity.IsExists())
                        entity.SetFlag(BaseEntity.Flags.On, enable);
            }

            void TryUpdateSignEntities()
            {
                if (ins._config.customizationConfig.isNeonSignsEnable && Time.realtimeSinceStartup - lastNeonUpdateTime > 30)
                {
                    lastNeonUpdateTime = Time.realtimeSinceStartup;
                    foreach (NeonSign neonSign in neonSigns)
                    {
                        if (neonSign.IsExists())
                        {
                            neonSign.limitNetworking = true;
                            neonSign.limitNetworking = false;
                        }
                    }
                }
            }

            void OnDestroy()
            {
                if (updateCoroutine != null)
                    ServerMgr.Instance.StopCoroutine(updateCoroutine);
            }

            class ItemThrower
            {
                TrainCar trainCar;
                Vector3 localThrowingPosition;
                float lastThrowItemTime = Time.realtimeSinceStartup;
                float nextItemThrowDelay;

                internal ItemThrower(TrainCar trainCar, Vector3 localThrowingPosition)
                {
                    this.trainCar = trainCar;
                    this.localThrowingPosition = localThrowingPosition;

                    nextItemThrowDelay = UnityEngine.Random.Range(ins._config.customizationConfig.giftCannonSetting.minTimeBetweenItems, ins._config.customizationConfig.giftCannonSetting.maxTimeBetweenItems);
                }

                internal void UpdateItemThrower()
                {
                    if (Time.realtimeSinceStartup - lastThrowItemTime > nextItemThrowDelay)
                    {
                        lastThrowItemTime = Time.realtimeSinceStartup;
                        nextItemThrowDelay = UnityEngine.Random.Range(ins._config.customizationConfig.giftCannonSetting.minTimeBetweenItems, ins._config.customizationConfig.giftCannonSetting.maxTimeBetweenItems);
                        ThrowItem();
                    }
                }

                void ThrowItem()
                {
                    ItemConfig itemForThrow = GetItemForThrowing();

                    if (itemForThrow == null)
                        return;

                    Item dropppedItem = CreateItemByItemConfig(itemForThrow);

                    if (dropppedItem != null)
                    {
                        Vector3 startPosition = BuildManager.GetGlobalPosition(trainCar.transform, localThrowingPosition) + Vector3.up;
                        Vector3 velocity = trainCar.GetWorldVelocity() + new Vector3(UnityEngine.Random.Range(-5, 5), UnityEngine.Random.Range(7, 20), UnityEngine.Random.Range(-5, 5));
                        Quaternion randomItemRotation = Quaternion.Euler(new Vector3(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 360)));

                        dropppedItem.Drop(startPosition, velocity, randomItemRotation);
                    }
                }

                ItemConfig GetItemForThrowing()
                {
                    int counter = 0;

                    while (counter < 100)
                    {
                        ItemConfig itemConfig = ins._config.customizationConfig.giftCannonSetting.itemsConfig.GetRandom();

                        if (UnityEngine.Random.Range(0.0f, 100.0f) <= itemConfig.chance)
                            return itemConfig;
                        counter++;
                    }

                    return ins._config.customizationConfig.giftCannonSetting.itemsConfig.Max(x => x.chance);
                }

                Item CreateItemByItemConfig(ItemConfig itemConfig)
                {
                    int amount = UnityEngine.Random.Range(itemConfig.minAmount, itemConfig.maxAmount + 1);

                    Item newItem;
                    if (itemConfig.isBluePrint)
                    {
                        newItem = ItemManager.CreateByName("blueprintbase");

                        ItemDefinition itemDefinition = ItemManager.FindItemDefinition(itemConfig.shortName);
                        if (itemDefinition != null)
                            newItem.blueprintTarget = itemDefinition.itemid;
                    }
                    else
                    {
                        newItem = ItemManager.CreateByName(itemConfig.shortName, amount, itemConfig.skinID);
                    }

                    return newItem;
                }
            }

            class CustomFirework
            {
                TrainCar trainCar;
                PatternFirework patternFirework;
                Vector3 localFireworkPosition;
                float lastFireTime = Time.realtimeSinceStartup;

                internal CustomFirework(TrainCar trainCar, Vector3 localFireworkPosition)
                {
                    this.trainCar = trainCar;
                    this.localFireworkPosition = localFireworkPosition;
                }

                internal void UpdateCustomFirework()
                {
                    if ((!ins._config.customizationConfig.fireworksSettings.isNighFireworks || IsNightNow()) && Time.realtimeSinceStartup - lastFireTime >= ins._config.customizationConfig.fireworksSettings.timeBetweenFireworks)
                    {
                        lastFireTime = Time.realtimeSinceStartup;
                        ActivateFireWork();
                    }
                }

                static bool IsNightNow()
                {
                    return ConVar.Env.time < 7 || ConVar.Env.time > 20;
                }

                void ActivateFireWork()
                {
                    if (patternFirework.IsExists())
                        patternFirework.Kill();

                    HashSet<FireworkConfig> suitableFireworkConfigs = customizeProfile.fireworkConfigs.Where(x => x.isEnabled);
                    if (suitableFireworkConfigs == null)
                        return;

                    FireworkConfig fireworkConfig = suitableFireworkConfigs.ToList().GetRandom();
                    if (fireworkConfig == null)
                        return;

                    patternFirework = BuildManager.SpawnChildEntity(trainCar, "assets/prefabs/deployable/fireworks/mortarpattern.prefab", localFireworkPosition, Vector3.zero, 0, false) as PatternFirework;
                    UpdateFireworkPaint(fireworkConfig);
                    patternFirework.TryLightFuse();
                }

                void UpdateFireworkPaint(FireworkConfig fireworkConfig)
                {
                    patternFirework.maxRepeats = ins._config.customizationConfig.fireworksSettings.numberShotsInSalvo;

                    patternFirework.Design?.Dispose();
                    patternFirework.MaxStars = 1000;
                    patternFirework.Design = new ProtoBuf.PatternFirework.Design();
                    patternFirework.Design.stars = new List<Star>();
                    Vector3 color = fireworkConfig.color.ToVector3();
                    foreach (string coord in fireworkConfig.paintCoordinates)
                    {
                        Vector3 position = coord.ToVector3();

                        ProtoBuf.PatternFirework.Star star = new ProtoBuf.PatternFirework.Star
                        {
                            color = new Color(color.x, color.y, color.z),
                            position = new Vector2(position.x, position.y)
                        };

                        patternFirework.Design.stars.Add(star);
                    }
                }
            }

            internal static class MapSaver
            {
                static Dictionary<string, string> colliderPrefabNames = new Dictionary<string, string>
                {
                    ["fence_a"] = "assets/prefabs/misc/xmas/icewalls/icewall.prefab",
                    ["christmas_present_LOD0"] = "assets/prefabs/misc/xmas/sleigh/presentdrop.prefab",
                    ["snowman_LOD1"] = "assets/prefabs/misc/xmas/snowman/snowman.deployed.prefab",
                    ["giftbox_LOD0"] = "assets/prefabs/misc/xmas/giftbox/giftbox_loot.prefab"
                };

                internal static void CreateOrAddNewWagonToData(string customizationPresetName, string wagonShortPrefabName)
                {
                    CustomizeProfile newCustomizeProfile = LoadProfile(customizationPresetName);
                    if (newCustomizeProfile == null || newCustomizeProfile.wagonPresets == null)
                    {
                        newCustomizeProfile = new CustomizeProfile
                        {
                            wagonPresets = new List<WagonCustomizationData>(),
                            npcPresets = GetNewNpcCustomizeConfig(),
                            fireworkConfigs = GetFireWorksConfig()
                        };
                    }

                    WagonCustomizationData wagonCustomizationData = SaveWagonFromMap(wagonShortPrefabName);
                    newCustomizeProfile.wagonPresets.Add(wagonCustomizationData);
                    SaveProfile(newCustomizeProfile, customizationPresetName);
                }

                static WagonCustomizationData SaveWagonFromMap(string wagonShortPrefabName)
                {
                    WagonCustomizationData wagonCustomizationData = new WagonCustomizationData
                    {
                        prefabName = wagonShortPrefabName,
                        isEnabled = true,
                        wagonOnly = new HashSet<string>(),
                        wagonExceptions = new HashSet<string>(),
                        trainExceptions = new HashSet<string>(),
                        decorEntityConfigs = new HashSet<DecorEntityConfig>(),
                        signConfigs = new HashSet<PaintedSignConfig>()
                    };

                    CheckAndSaveColliders(ref wagonCustomizationData);
                    return wagonCustomizationData;
                }

                static void CheckAndSaveColliders(ref WagonCustomizationData wagonCustomizationData)
                {
                    List<Collider> colliders = Physics.OverlapSphere(Vector3.zero, 50).OrderBy(x => x.transform.position.z);

                    foreach (Collider collider in colliders)
                        TrySaveCollder(collider, ref wagonCustomizationData);
                }

                static void TrySaveCollder(Collider collider, ref WagonCustomizationData wagonCustomizationData)
                {
                    BaseEntity entity = collider.ToBaseEntity();

                    if (entity == null)
                        SaveCollider(collider, ref wagonCustomizationData);
                    else if (IsCustomizingEntity(entity))
                    {
                        NeonSign neonSign = entity as NeonSign;

                        if (neonSign != null)
                            SaveNeonSign(neonSign, ref wagonCustomizationData);
                        else
                            SaveRegularEntity(entity, ref wagonCustomizationData);
                    }
                }

                static bool IsCustomizingEntity(BaseEntity entity)
                {
                    if (entity == null)
                        return false;
                    else if (entity is ResourceEntity || entity is BasePlayer)
                        return false;

                    if (entity is LootContainer)
                    {

                        return false;
                    }

                    return true;
                }

                static void SaveNeonSign(NeonSign neonSign, ref WagonCustomizationData wagonCustomizationData)
                {
                    PaintedSignConfig paintedSignConfig = GetPaintedSignConfig(neonSign);

                    if (paintedSignConfig != null && !wagonCustomizationData.signConfigs.Any(x => x.prefabName == paintedSignConfig.prefabName && x.position == paintedSignConfig.position && x.rotation == paintedSignConfig.rotation))
                        wagonCustomizationData.signConfigs.Add(paintedSignConfig);
                }

                static PaintedSignConfig GetPaintedSignConfig(NeonSign neonSign)
                {
                    return new PaintedSignConfig
                    {
                        prefabName = neonSign.PrefabName,
                        skin = 0,
                        position = $"({neonSign.transform.position.x}, {neonSign.transform.position.y}, {neonSign.transform.position.z})",
                        rotation = neonSign.transform.eulerAngles.ToString(),
                        imageName = ""
                    };
                }

                static void SaveRegularEntity(BaseEntity entity, ref WagonCustomizationData wagonCustomizationData)
                {
                    DecorEntityConfig decorLocationConfig = GetDecorEntityConfig(entity);

                    if (decorLocationConfig != null && !wagonCustomizationData.decorEntityConfigs.Any(x => x.prefabName == decorLocationConfig.prefabName && x.position == decorLocationConfig.position && x.rotation == decorLocationConfig.rotation))
                        wagonCustomizationData.decorEntityConfigs.Add(decorLocationConfig);
                }

                static DecorEntityConfig GetDecorEntityConfig(BaseEntity entity)
                {
                    ulong skin = entity.skinID;
                    if (entity.ShortPrefabName == "rug.deployed")
                    {
                        skin = 2349822120;//ice
                                          //skin = 2902669530;//snow
                                          //skin = 1587884106;//red
                    }
                    else if (entity.ShortPrefabName == "rug.bear.deployed")
                    {
                        skin = 91053011;
                    }
                    else if (entity.ShortPrefabName == "barricade.sandbags")
                    {
                        skin = 809144507;
                    }
                    else if (entity.ShortPrefabName == "barricade.concrete")
                    {
                        skin = 3103508242;
                    }

                    return new DecorEntityConfig
                    {
                        prefabName = entity.PrefabName,
                        skin = skin,
                        position = $"({entity.transform.position.x}, {entity.transform.position.y}, {entity.transform.position.z})",
                        rotation = entity.transform.eulerAngles.ToString()
                    };
                }

                static void SaveCollider(Collider collider, ref WagonCustomizationData wagonCustomizationData)
                {
                    DecorEntityConfig colliderEntityConfig = GetColliderConfigAsBaseEntity(collider);

                    if (colliderEntityConfig != null && !wagonCustomizationData.decorEntityConfigs.Any(x => x.prefabName == colliderEntityConfig.prefabName && x.position == colliderEntityConfig.position && x.rotation == colliderEntityConfig.rotation))
                        wagonCustomizationData.decorEntityConfigs.Add(colliderEntityConfig);
                }

                static DecorEntityConfig GetColliderConfigAsBaseEntity(Collider collider)
                {
                    string prefabName = "";

                    if (!colliderPrefabNames.TryGetValue(collider.name, out prefabName))
                        return null;

                    return new DecorEntityConfig
                    {
                        prefabName = prefabName,
                        skin = 0,
                        position = $"({collider.transform.position.x}, {collider.transform.position.y}, {collider.transform.position.z})",
                        rotation = collider.transform.eulerAngles.ToString()
                    };
                }

                static void SaveProfile(CustomizeProfile customizeData, string name)
                {
                    Interface.Oxide.DataFileSystem.WriteObject($"{ins.Name}/{name}", customizeData);
                }

                static List<CustomizeNpcConfig> GetNewNpcCustomizeConfig()
                {
                    return GetNYNpcConfigs();
                }

                static List<CustomizeNpcConfig> GetHalloweenNpcConfigs()
                {
                    return new List<CustomizeNpcConfig>
                        {
                            new CustomizeNpcConfig
                            {
                                enable = true,
                                customWearItems = new List<CustomWearItem>
                                {
                                    new CustomWearItem
                                    {
                                        shortName = "pumpkin",
                                        skinID = 0
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "gloweyes",
                                        skinID = 0
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "metal.plate.torso",
                                        skinID = 2624420786
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "roadsign.kilt",
                                        skinID = 1539570583
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "hoodie",
                                        skinID = 2963939240
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "pants",
                                        skinID = 2963934001
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "shoes.boots",
                                        skinID = 3047756539
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "roadsign.gloves",
                                        skinID = 3044771291
                                    }
                                }
                            },
                            new CustomizeNpcConfig
                            {
                                enable = true,
                                customWearItems = new List<CustomWearItem>
                                {
                                    new CustomWearItem
                                    {
                                        shortName = "metal.facemask",
                                        skinID = 882453233
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "gloweyes",
                                        skinID = 0
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "hoodie",
                                        skinID = 2256109331
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "pants",
                                        skinID = 2256110716
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "shoes.boots",
                                        skinID = 811633396
                                    },
                                }
                            },
                            new CustomizeNpcConfig
                            {
                                enable = true,
                                customWearItems = new List<CustomWearItem>
                                {
                                    new CustomWearItem
                                    {
                                        shortName = "mask.balaclava",
                                        skinID = 2873514778
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "gloweyes",
                                        skinID = 0
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "burlap.trousers",
                                        skinID = 2873788586
                                    },

                                    new CustomWearItem
                                    {
                                        shortName = "burlap.shirt",
                                        skinID = 2873786685
                                    },

                                    new CustomWearItem
                                    {
                                        shortName = "shoes.boots",
                                        skinID = 1644270941
                                    },
                                }
                            },
                            new CustomizeNpcConfig
                            {
                                enable = true,
                                customWearItems = new List<CustomWearItem>
                                {
                                    new CustomWearItem
                                    {
                                        shortName = "frankensteins.monster.01.head",
                                        skinID = 0
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "gloweyes",
                                        skinID = 0
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "frankensteins.monster.01.torso",
                                        skinID = 0
                                    },

                                    new CustomWearItem
                                    {
                                        shortName = "frankensteins.monster.01.legs",
                                        skinID = 0
                                    }
                                }
                            },
                            new CustomizeNpcConfig
                            {
                                enable = true,
                                customWearItems = new List<CustomWearItem>
                                {
                                    new CustomWearItem
                                    {
                                        shortName = "frankensteins.monster.02.head",
                                        skinID = 0
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "gloweyes",
                                        skinID = 0
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "frankensteins.monster.02.torso",
                                        skinID = 0
                                    },

                                    new CustomWearItem
                                    {
                                        shortName = "frankensteins.monster.02.legs",
                                        skinID = 0
                                    }
                                }
                            },
                            new CustomizeNpcConfig
                            {
                                enable = true,
                                customWearItems = new List<CustomWearItem>
                                {
                                    new CustomWearItem
                                    {
                                        shortName = "frankensteins.monster.03.head",
                                        skinID = 0
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "gloweyes",
                                        skinID = 0
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "frankensteins.monster.03.torso",
                                        skinID = 0
                                    },

                                    new CustomWearItem
                                    {
                                        shortName = "frankensteins.monster.03.legs",
                                        skinID = 0
                                    }
                                }
                            },
                        };
                }

                static List<CustomizeNpcConfig> GetNYNpcConfigs()
                {
                    return new List<CustomizeNpcConfig>
                        {
                            new CustomizeNpcConfig
                            {
                                enable = true,
                                customWearItems = new List<CustomWearItem>
                                {
                                    new CustomWearItem
                                    {
                                        shortName = "burlap.shirt",
                                        skinID = 1587743344
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "burlap.trousers",
                                        skinID = 1587746365
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "burlap.gloves",
                                        skinID = 784676585
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "roadsign.jacket",
                                        skinID = 1935355816
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "roadsign.kilt",
                                        skinID = 1935355440
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "shoes.boots",
                                        skinID = 2675531117
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "metal.facemask",
                                        skinID = 1170471712
                                    },
                                }
                            },
                            new CustomizeNpcConfig
                            {
                                enable = true,
                                customWearItems = new List<CustomWearItem>
                                {
                                    new CustomWearItem
                                    {
                                        shortName = "attire.snowman.helmet",
                                        skinID = 0
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "metal.plate.torso",
                                        skinID = 1934946028
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "pants",
                                        skinID = 2728153861
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "shoes.boots",
                                        skinID = 1158967113
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "hoodie",
                                        skinID = 2728150332
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "roadsign.gloves",
                                        skinID = 2950127861
                                    }
                                }
                            },
                            new CustomizeNpcConfig
                            {
                                enable = true,
                                customWearItems = new List<CustomWearItem>
                                {
                                    new CustomWearItem
                                    {
                                        shortName = "burlap.shirt",
                                        skinID = 1229561297
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "burlap.trousers",
                                        skinID = 1229552157
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "burlap.gloves",
                                        skinID = 784676585
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "shoes.boots",
                                        skinID = 2675531117
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "santahat",
                                        skinID = 2675531117
                                    }
                                }
                            },
                            new CustomizeNpcConfig
                            {
                                enable = true,
                                customWearItems = new List<CustomWearItem>
                                {
                                    new CustomWearItem
                                    {
                                        shortName = "shoes.boots",
                                        skinID = 2675531117
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "burlap.gloves",
                                        skinID = 784676585
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "roadsign.kilt",
                                        skinID = 2320295405
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "pants",
                                        skinID = 1587846022
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "metal.facemask",
                                        skinID = 2684259722
                                    },
                                    new CustomWearItem
                                    {
                                        shortName = "jacket.snow",
                                        skinID = 1229047763
                                    },
                                }
                            },
                            new CustomizeNpcConfig
                            {
                                enable = true,
                                customWearItems = new List<CustomWearItem>
                                {
                                    new CustomWearItem
                                    {
                                        shortName = "gingerbreadsuit",
                                        skinID = 0
                                    }
                                }
                            },
                        };
                }

                static List<FireworkConfig> GetFireWorksConfig()
                {
                    return GetNYFireWorksConfig();
                }

                static List<FireworkConfig> GetNYFireWorksConfig()
                {
                    return new List<FireworkConfig>
                        {
                            new FireworkConfig
                            {
                                presetName = "2024",
                                isEnabled = true,
                                color = "(0, 1, 0)",
                                paintCoordinates = new HashSet<string>
                                {
                                    "(0.01, 0.85, 0.00)",
                                    "(0.00, 1.00, 0.00)",
                                    "(0.13, 1.00, 0.00)",
                                    "(0.28, 1.00, 0.00)",
                                    "(0.43, 1.00, 0.00)",
                                    "(0.43, 0.85, 0.00)",
                                    "(0.43, 0.71, 0.00)",
                                    "(0.33, 0.59, 0.00)",
                                    "(0.22, 0.50, 0.00)",
                                    "(0.11, 0.41, 0.00)",
                                    "(0.04, 0.29, 0.00)",
                                    "(0.04, 0.15, 0.00)",
                                    "(0.04, 0.02, 0.00)",
                                    "(0.19, 0.02, 0.00)",
                                    "(0.33, 0.03, 0.00)",
                                    "(0.48, 0.04, 0.00)",
                                    "(0.74, 0.85, 0.00)",
                                    "(0.73, 1.00, 0.00)",
                                    "(0.86, 1.00, 0.00)",
                                    "(1.01, 1.00, 0.00)",
                                    "(1.16, 1.00, 0.00)",
                                    "(1.16, 0.85, 0.00)",
                                    "(1.17, 0.71, 0.00)",
                                    "(0.74, 0.71, 0.00)",
                                    "(0.75, 0.56, 0.00)",
                                    "(0.76, 0.42, 0.00)",
                                    "(0.77, 0.29, 0.00)",
                                    "(0.77, 0.15, 0.00)",
                                    "(0.77, 0.02, 0.00)",
                                    "(0.92, 0.02, 0.00)",
                                    "(1.06, 0.03, 0.00)",
                                    "(1.19, 0.03, 0.00)",
                                    "(1.17, 0.56, 0.00)",
                                    "(1.18, 0.41, 0.00)",
                                    "(1.19, 0.26, 0.00)",
                                    "(1.19, 0.16, 0.00)",
                                    "(1.45, 0.85, 0.00)",
                                    "(1.44, 1.00, 0.00)",
                                    "(1.57, 1.00, 0.00)",
                                    "(1.72, 1.00, 0.00)",
                                    "(1.87, 1.00, 0.00)",
                                    "(1.87, 0.85, 0.00)",
                                    "(1.87, 0.71, 0.00)",
                                    "(1.77, 0.59, 0.00)",
                                    "(1.66, 0.50, 0.00)",
                                    "(1.55, 0.41, 0.00)",
                                    "(1.48, 0.29, 0.00)",
                                    "(1.48, 0.15, 0.00)",
                                    "(1.48, 0.02, 0.00)",
                                    "(1.63, 0.02, 0.00)",
                                    "(1.77, 0.03, 0.00)",
                                    "(1.92, 0.04, 0.00)",
                                    "(2.18, 0.85, 0.00)",
                                    "(2.17, 1.00, 0.00)",
                                    "(2.49, 0.56, 0.00)",
                                    "(2.34, 0.56, 0.00)",
                                    "(2.60, 1.00, 0.00)",
                                    "(2.60, 0.85, 0.00)",
                                    "(2.61, 0.71, 0.00)",
                                    "(2.18, 0.71, 0.00)",
                                    "(2.19, 0.56, 0.00)",
                                    "(2.63, 0.03, 0.00)",
                                    "(2.61, 0.56, 0.00)",
                                    "(2.62, 0.41, 0.00)",
                                    "(2.63, 0.26, 0.00)",
                                    "(2.63, 0.16, 0.00)"
                                }
                            }
                        };
                }
            }

            static class SignPainter
            {
                static string imagePath = $"{ins.Name}/Images/";

                internal static void UpdateNeonSign(NeonSign neonSign, string imageName)
                {
                    if (imageName != "")
                        ServerMgr.Instance.StartCoroutine(LoadImage(neonSign, imageName));

                    neonSign.UpdateFromInput(100, 0);
                }

                static IEnumerator LoadImage(NeonSign neonSign, string imageName)
                {
                    string url = "file://" + Interface.Oxide.DataDirectory + Path.DirectorySeparatorChar + imagePath + imageName + ".png";
                    using (WWW www = new WWW(url))
                    {
                        yield return www;

                        if (www.error == null)
                        {
                            neonSign.EnsureInitialized();
                            Texture2D tex = www.texture;
                            byte[] bt = tex.EncodeToPNG();
                            Array.Resize(ref neonSign.textureIDs, 1);
                            uint textureIndex = 0;
                            uint textureId = FileStorage.server.Store(bt, FileStorage.Type.png, neonSign.net.ID, textureIndex);

                            neonSign.textureIDs[textureIndex] = textureId;
                            neonSign.SendNetworkUpdate();
                        }
                        else
                        {
                            ins.PrintError($"{imageName} file was not found in the data/ArmoredTrain/Images folder");
                        }
                    }
                }
            }

            internal static class PatternFireworkSignSaver
            {
                static HashSet<string> symbol2 = new HashSet<string>()
                    {
                        "(0.01, 0.85, 0.00)",
                        "(0.00, 1.00, 0.00)",
                        "(0.13, 1.00, 0.00)",
                        "(0.28, 1.00, 0.00)",
                        "(0.43, 1.00, 0.00)",
                        "(0.43, 0.85, 0.00)",
                        "(0.43, 0.71, 0.00)",
                        "(0.33, 0.59, 0.00)",
                        "(0.22, 0.50, 0.00)",
                        "(0.11, 0.41, 0.00)",
                        "(0.04, 0.29, 0.00)",
                        "(0.04, 0.15, 0.00)",
                        "(0.04, 0.02, 0.00)",
                        "(0.19, 0.02, 0.00)",
                        "(0.33, 0.03, 0.00)",
                        "(0.48, 0.04, 0.00)",
                    };

                static HashSet<string> symbol0 = new HashSet<string>()
                    {
                        "(0.01, 0.85, 0.00)",
                        "(0.00, 1.00, 0.00)",
                        "(0.13, 1.00, 0.00)",
                        "(0.28, 1.00, 0.00)",
                        "(0.43, 1.00, 0.00)",
                        "(0.43, 0.85, 0.00)",
                        "(0.44, 0.71, 0.00)",
                        "(0.01, 0.71, 0.00)",
                        "(0.02, 0.56, 0.00)",
                        "(0.03, 0.42, 0.00)",
                        "(0.04, 0.29, 0.00)",
                        "(0.04, 0.15, 0.00)",
                        "(0.04, 0.02, 0.00)",
                        "(0.19, 0.02, 0.00)",
                        "(0.33, 0.03, 0.00)",
                        "(0.46, 0.03, 0.00)",
                        "(0.44, 0.56, 0.00)",
                        "(0.45, 0.41, 0.00)",
                        "(0.46, 0.26, 0.00)",
                        "(0.46, 0.16, 0.00)",
                    };

                static HashSet<string> symbol4 = new HashSet<string>()
                    {
                        "(0.01, 0.85, 0.00)",
                        "(0.00, 1.00, 0.00)",
                        "(0.32, 0.56, 0.00)",
                        "(0.17, 0.56, 0.00)",
                        "(0.43, 1.00, 0.00)",
                        "(0.43, 0.85, 0.00)",
                        "(0.44, 0.71, 0.00)",
                        "(0.01, 0.71, 0.00)",
                        "(0.02, 0.56, 0.00)",
                        "(0.46, 0.03, 0.00)",
                        "(0.44, 0.56, 0.00)",
                        "(0.45, 0.41, 0.00)",
                        "(0.46, 0.26, 0.00)",
                        "(0.46, 0.16, 0.00)",
                    };

                internal static void UpdatePatternFirework(PatternFirework patternFirework)
                {
                    patternFirework.Design?.Dispose();
                    patternFirework.MaxStars = 1000;
                    patternFirework.Design = new ProtoBuf.PatternFirework.Design();
                    patternFirework.Design.stars = new List<Star>();

                    Print2024(patternFirework);
                    patternFirework.SendNetworkUpdateImmediate();
                }

                static void Print2024(PatternFirework patternFirework)
                {
                    float x0 = -2;

                    PrintSymbol(symbol2, patternFirework, ref x0);
                    PrintSymbol(symbol0, patternFirework, ref x0);
                    PrintSymbol(symbol2, patternFirework, ref x0);
                    PrintSymbol(symbol4, patternFirework, ref x0);
                }

                static void PrintSymbol(HashSet<string> symbol, PatternFirework patternFirework, ref float x0)
                {
                    float newx0 = float.MinValue;

                    foreach (string coord in symbol)
                    {
                        Vector3 position = coord.ToVector3();

                        if (position.x + x0 > newx0)
                            newx0 = position.x + x0;

                        patternFirework.Design.stars.Add
                        (
                            new ProtoBuf.PatternFirework.Star
                            {
                                color = new Color(1, 0, 0),
                                position = new Vector2(position.x + x0, position.y)
                            }
                        );
                    }

                    x0 = newx0 + 0.25f;
                }

                internal static void ShowStarsCoordinatesOfRegularPaint(PatternFirework patternFirework)
                {
                    foreach (Star start in patternFirework.Design.stars)
                    {
                        Vector3 starPosition = new Vector3(start.position.x, start.position.y, 0) + new Vector3(1, 0, 0);
                        ins.Puts(starPosition.ToString());
                    }
                }

                internal static void ShowStarsCoordinatesOfCustomPaint(PatternFirework patternFirework)
                {
                    foreach (Star start in patternFirework.Design.stars)
                    {
                        Vector3 starPosition = new Vector3(start.position.x, start.position.y, 0) + new Vector3(2, 0, 0);
                        ins.Puts(starPosition.ToString());
                    }
                }
            }
        }

        static class BuildManager
        {
            internal static BaseEntity SpawnEntity(string prefabName, Vector3 position, Quaternion rotation, ulong skinId, bool decor = true)
            {
                BaseEntity entity = decor ? CreateDecorEntity(prefabName, position, rotation, skinId) : CreateEntity(prefabName, position, rotation, skinId);
                DestroyUnnessesaryComponents(entity);
                entity.Spawn();
                return entity;
            }

            internal static BaseEntity SpawnChildEntity(BaseEntity parrentEntity, string prefabName, Vector3 localPosition, Vector3 localRotation, ulong skinId, bool decor = true)
            {
                BaseEntity entity = decor ? CreateDecorEntity(prefabName, parrentEntity.transform.position, Quaternion.identity, skinId) : CreateEntity(prefabName, parrentEntity.transform.position, Quaternion.identity, skinId);
                SetParent(parrentEntity, entity, localPosition, localRotation);
                DestroyUnnessesaryComponents(entity);
                entity.Spawn();
                return entity;
            }

            internal static BaseEntity CreateEntity(string prefabName, Vector3 position, Quaternion rotation, ulong skinId, bool enableSaving = false)
            {
                BaseEntity entity = GameManager.server.CreateEntity(prefabName, position, rotation);
                entity.enableSaving = enableSaving;
                entity.skinID = skinId;
                return entity;
            }

            internal static void SetParent(BaseEntity parrentEntity, BaseEntity childEntity, Vector3 localPosition, Vector3 localRotation)
            {
                childEntity.SetParent(parrentEntity, true, false);
                childEntity.transform.localPosition = localPosition;
                childEntity.transform.localEulerAngles = localRotation;
            }

            internal static BaseEntity CreateDecorEntity(string prefabName, Vector3 position, Quaternion rotation, ulong skinId)
            {
                BaseEntity entity = CreateEntity(prefabName, position, rotation, skinId);

                BaseEntity trueBaseEntity = entity.gameObject.AddComponent<BaseEntity>();
                CopySerializableFields(entity, trueBaseEntity);
                UnityEngine.Object.DestroyImmediate(entity, true);

                return trueBaseEntity;
            }

            static void DestroyUnnessesaryComponents(BaseEntity entity)
            {
                DestroyEntityConponent<Rigidbody>(entity);
                DestroyEntityConponent<TriggerHurtEx>(entity);
                DestroyEntityConponent<GroundWatch>(entity);
                DestroyEntityConponent<DestroyOnGroundMissing>(entity);
            }

            internal static void DestroyEntityConponent<TypeForDestroy>(BaseEntity entity)
            {
                TypeForDestroy component = entity.GetComponent<TypeForDestroy>();

                if (component != null)
                    UnityEngine.GameObject.DestroyImmediate(component as UnityEngine.Object);
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

            internal static Vector3 GetGlobalPosition(Transform parentTransform, Vector3 position)
            {
                return parentTransform.transform.TransformPoint(position);
            }
        }

        static class PveModeController
        {
            static BasePlayer eventOwner;

            internal static void SetPveModeOwner(BasePlayer newEventOwner)
            {
                eventOwner = newEventOwner;
                NotifyManager.SendMessageToAll("PveMode_NewOwner", ins._config.prefix, newEventOwner.displayName);
            }

            internal static void ClearPveModeOwner()
            {
                eventOwner = null;
            }

            internal static bool IsPveModeBlockAction(BasePlayer player)
            {
                if (!ins._config.supportedPluginsConfig.pveMode.enable)
                    return false;

                if (eventOwner == null)
                    return false;

                if (player.userID == eventOwner.userID)
                    return false;

                if (IsTeam(player, eventOwner.userID))
                    return false;

                return true;
            }

            internal static BasePlayer GetEventOwner()
            {
                return eventOwner;
            }

            static bool IsTeam(BasePlayer player, ulong targetId)
            {
                if (player == null || targetId == 0)
                    return false;

                if (player.currentTeam != 0)
                {
                    RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                    if (playerTeam != null && playerTeam.members.Contains(targetId))
                        return true;
                }

                if (ins.plugins.Exists("Friends") && (bool)ins.Friends.Call("AreFriends", player.userID, targetId))
                    return true;

                if (ins.plugins.Exists("Clans") && ins.Clans.Author == "k1lly0u" && (bool)ins.Clans.Call("IsMemberOrAlly", player.UserIDString, targetId.ToString()))
                    return true;

                return false;
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

                ["SendEconomy"] = "{0} Вы <color=#738d43>получили</color> <color=#55aaff>{1}</color> баллов в экономику за прохождение ивента",

                ["Hour"] = "ч.",
                ["Min"] = "м.",
                ["Sec"] = "с.",

                ["PveMode_NewOwner"] = "{0} <color=#55aaff>{1}</color> стал владельцем ивента!",
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

                ["SendEconomy"] = "{0} You <color=#738d43>have earned</color> <color=#55aaff>{1}</color> points in economics for participating in the event",

                ["Hour"] = "h.",
                ["Min"] = "m.",
                ["Sec"] = "s.",

                ["Marker_EventOwner"] = "Event Owner: {0}",

                ["EventStart_Log"] = "The event has begun! (Preset name - {0})",
                ["EventStop_Log"] = "The event is over!",

                ["DataFileNotFound_Exeption"] = "Could not find a data file for customization ({0}.json). Empty the [Customization preset] in the config or upload the data file",

                ["PveMode_NewOwner"] = "{0} <color=#55aaff>{1}</color> became the owner of the event!",
            }, this);
        }

        internal static string GetMessage(string langKey, string userID) => ins.lang.GetMessage(langKey, ins, userID);

        internal static string GetMessage(string langKey, string userID, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userID) : string.Format(GetMessage(langKey, userID), args);
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

        #region CustomizationConfig
        public class CustomizeProfile
        {
            [JsonProperty("Wagons presets")] public List<WagonCustomizationData> wagonPresets { get; set; }
            [JsonProperty("Npc presets")] public List<CustomizeNpcConfig> npcPresets { get; set; }
            [JsonProperty("Fireworks Presets")] public List<FireworkConfig> fireworkConfigs { get; set; }
        }

        public class WagonCustomizationData
        {
            [JsonProperty("Preset Name")] public string presetName { get; set; }
            [JsonProperty("Enable [true/false]")] public bool isEnabled { get; set; }
            [JsonProperty("Short prefab name of the wagon to which customization will be applied")] public string prefabName { get; set; }
            [JsonProperty("Presets of wagons to which this preset will be applied (leave empty for all presets)")] public HashSet<string> wagonOnly { get; set; }
            [JsonProperty("Presets of wagons that will NOT be customized")] public HashSet<string> wagonExceptions { get; set; }
            [JsonProperty("Presets of trains that will NOT be customized")] public HashSet<string> trainExceptions { get; set; }
            [JsonProperty("Disable the basic decoration on the carriage [true/false]")] public bool isBaseDecorDisable { get; set; }
            [JsonProperty("List of decorations")] public HashSet<DecorEntityConfig> decorEntityConfigs { get; set; }
            [JsonProperty("List of signs")] public HashSet<PaintedSignConfig> signConfigs { get; set; }
        }

        public class DecorEntityConfig
        {
            [JsonProperty("Prefab")] public string prefabName { get; set; }
            [JsonProperty("Skin")] public ulong skin { get; set; }
            [JsonProperty("Position")] public string position { get; set; }
            [JsonProperty("Rotation")] public string rotation { get; set; }
        }

        public class PaintedSignConfig : DecorEntityConfig
        {
            [JsonProperty("Image Name")] public string imageName { get; set; }
        }

        public class CustomizeNpcConfig
        {
            [JsonProperty("Enable [true/false]")] public bool enable { get; set; }
            [JsonProperty("Wear Items")] public List<CustomWearItem> customWearItems { get; set; }
        }

        public class CustomWearItem
        {
            [JsonProperty("ShortName")] public string shortName { get; set; }
            [JsonProperty("SkinID (0 - default)")] public ulong skinID { get; set; }
        }

        public class FireworkConfig
        {
            [JsonProperty("Preset Name")] public string presetName { get; set; }
            [JsonProperty("Enable [true/false]")] public bool isEnabled { get; set; }
            [JsonProperty("Color (r, g, b)")] public string color { get; set; }
            [JsonProperty("Coordinates for the drawing")] public HashSet<string> paintCoordinates { get; set; }
        }
        #endregion CustomizationConfig

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
            [JsonProperty(en ? "Stop the train after taking damage [true/false]" : "Останавливать поезд после получения урона [true/false]")] public bool stopTrainAfterReceivingDamage { get; set; }
            [JsonProperty(en ? "Restore the stop time when the train receives damage/loot crates [true/false]" : "Восстанавливать время остановки при получении поездом урона/лутании ящиков [true/false]")] public bool isRestoreStopTimeAfterDamageOrLoor { get; set; }
            [JsonProperty(en ? "Destroy the train after opening all the crates [true/false]" : "Уничтожать поезд после открытия всех ящиков [true/false]")] public bool killTrainAfterLoot { get; set; }
            [JsonProperty(en ? "Time to destroy the train after opening all the crates [sec]" : "Время до уничтожения поезда после открытия всех ящиков [sec]")] public int killTimeTrainAfterLoot { get; set; }
            [JsonProperty(en ? "Destroy wagons in front of the train [true/false]" : "Уничтожать вагоны перед поездом [true/false]")] public bool destrroyWagons { get; set; }
            [JsonProperty(en ? "Use custom spawn coordinates [true/false]" : "Использовать кастомные координаты спавна [true/false]")] public bool useCustomCoords { get; set; }
            [JsonProperty(en ? "Custom coordinates for the spawn of the train (/atrainpoint)" : "Кастомные координаты для спавна поезда (/atrainpoint)")] public List<string> customSpawnPoints { get; set; }
            [JsonProperty(en ? "Allow wagons to be connected to the train [true/false]" : "Разрешить присоединение вагонов к поезду [true/false]")] public bool allowConnectWagons { get; set; }
            [JsonProperty(en ? "Allow damage to the train driver [true/false]" : "Разрешить урон по водителю поезда [true/false]")] public bool allowDriverDamage { get; set; }
            [JsonProperty(en ? "To revive the train driver if he was killed? [true/false]" : "Возрождать водителя поезда, если он был убит [true/false]")] public bool reviveTrainDriver { get; set; }
            [JsonProperty(en ? "Self-destruct effects settings" : "Настройка эффектов самоуничтожения")] public SelfDestructionConfig selfDestructionConfig { get; set; }
            [JsonProperty(en ? "Enable logging of the start and end of the event? [true/false]" : "Включить логирование начала и окончания ивента? [true/false]")] public bool enableStartStopLogs { get; set; }
            [JsonProperty(en ? "The turrets of the train will drop loot after destruction? [true/false]" : "Турели поезда будут оставлять лут после уничтожения? [true/false]")] public bool turretDropWeapon { get; set; }
        }

        public class CustomizationConfig
        {
            [JsonProperty(en ? "Customization preset (Empty - use standard wagons)" : "Пресет кастомизации (оставить пустым - использовать стандартые вагоны)")] public string profileName { get; set; }
            [JsonProperty(en ? "Turn on the electric furnaces (high impact on performance) [true/false]" : "Включить свечение электрических печей (высокое влияение на производительность) [true/false]")] public bool isElectricFurnacesEnable { get; set; }
            [JsonProperty(en ? "Turn on the boilers (medium impact on performance) [true/false]" : "Включить свечение котлов (среднее влияение на производительность) [true/false]")] public bool isBoilersEnable { get; set; }
            [JsonProperty(en ? "Turn on the fire (medium impact on performance) [true/false]" : "Включить огонь (среднее влияение на производительность) [true/false]")] public bool isFireEnable { get; set; }
            [JsonProperty(en ? "Turn on the lighting entities only at night [true/false]" : "Включать предметы освещения только ночью [true/false]")] public bool isLightOnlyAtNight { get; set; }
            [JsonProperty(en ? "Turn on the Neon Signs [true/false]" : "Включить неоновые таблички [true/false]")] public bool isNeonSignsEnable { get; set; }
            [JsonProperty(en ? "Setting up the Gift cannon" : "Настройка пушки подарков")] public GiftCannonSetting giftCannonSetting { get; set; }
            [JsonProperty(en ? "Setting up fireworks" : "Настройка фейрверков")] public FireworksSetting fireworksSettings { get; set; }
        }

        public class GiftCannonSetting
        {
            [JsonProperty(en ? "Enable throwing gifts out of the cannon [true/false]" : "Включить выбрасывание подарков из пушки [true/false]")] public bool isGiftCannonEnable { get; set; }
            [JsonProperty(en ? "Minimum time between throwing gifts [sec]" : "Минимальное время между выбрасываниями подарков [sec]")] public int minTimeBetweenItems { get; set; }
            [JsonProperty(en ? "Maximum time between throwing gifts [sec]" : "Максимальное время между выбрасываниями подарков [sec]")] public int maxTimeBetweenItems { get; set; }
            [JsonProperty(en ? "List of gifts" : "Список подарков")] public List<ItemConfig> itemsConfig { get; set; }
        }

        public class FireworksSetting
        {
            [JsonProperty(en ? "Turn on the fireworks [true/false]" : "Включить фейерверки [true/false]")] public bool isFireworksOn { get; set; }
            [JsonProperty(en ? "The time between fireworks salvos [s]" : "Время между залпами фейерверков [s]")] public int timeBetweenFireworks { get; set; }
            [JsonProperty(en ? "The number of shots in a salvo" : "Количество выстрелов в залпе")] public int numberShotsInSalvo { get; set; }
            [JsonProperty(en ? "Activate fireworks only at night [true/false]" : "Активировать фейерверки только ночью [true/false]")] public bool isNighFireworks { get; set; }
        }

        public class SelfDestructionConfig
        {
            [JsonProperty(en ? "Enable self-destruct effects? [true/false]" : "Включить эффекты самоуничтожения? [true/false]")] public bool enable { get; set; }
            [JsonProperty(en ? "The time until the end of the event when the C4 beeping effect begins [sec]" : "Время до окончания ивента, когда начинается эффект пищания C4 [sec]")] public int beepingTime { get; set; }
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
            [JsonProperty(en ? "Preset name" : "Название пресета", Order = 0)] public string presetName { get; set; }
            [JsonProperty(en ? "Prefab name" : "Префаба", Order = 1)] public string prefabName { get; set; }
            [JsonProperty(en ? "Bradley preset - locations" : "Пресет бредли - расположения", Order = 2)] public Dictionary<string, HashSet<LocationConfig>> brradleys { get; set; }
            [JsonProperty(en ? "Turret preset - locations" : "Пресет турели - расположения", Order = 3)] public Dictionary<string, HashSet<LocationConfig>> turrets { get; set; }
            [JsonProperty(en ? "SamSite preset - locations" : "Пресет SamSite - расположения", Order = 4)] public Dictionary<string, HashSet<LocationConfig>> samsites { get; set; }
            [JsonProperty(en ? "NPC preset - locations" : "Пресет NPC - расположения", Order = 5)] public Dictionary<string, HashSet<LocationConfig>> NPCs { get; set; }
            [JsonProperty(en ? "Crate preset - locations" : "Пресет крейта - расположения", Order = 6)] public Dictionary<string, HashSet<LocationConfig>> crates { get; set; }
            [JsonProperty(en ? "Decorative prefab - locations" : "Префаб декоративного блока - расположения", Order = 7)] public Dictionary<string, HashSet<LocationConfig>> decors { get; set; }
        }

        public class LocomotiveConfig : BaseWagonConfig
        {
            [JsonProperty(en ? "Engine force" : "Мощность двигателя", Order = 8)] public float engineForce { get; set; }
            [JsonProperty(en ? "Max speed" : "Максимальная скорость", Order = 9)] public float maxSpeed { get; set; }
            [JsonProperty(en ? "Driver name" : "Имя водителя", Order = 10)] public string driverName { get; set; }
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
            [JsonProperty(en ? "Use a vending marker? [true/false]" : "Добавить маркер магазина? [true/false]")] public bool useShopMarker { get; set; }
            [JsonProperty(en ? "Use a circular marker? [true/false]" : "Добавить круговой маркер? [true/false]")] public bool useRingMarker { get; set; }
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
            [JsonProperty(en ? "Target detection range (0 - do not change)" : "Дальность обнаружения цели (0 - не изменять)")] public float targetDetectionRange { get; set; }
            [JsonProperty(en ? "Target loss range (0 - do not change)" : "Дальность потери цели (0 - не изменять)")] public float targetLossRange { get; set; }
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
            [JsonProperty(en ? "The time for which the helicopter can leave the train to attack the target [sec.]" : "Время, на которое верталет может покидать поезд для атаки цели [sec.]")] public float outsideTime { get; set; }
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
            [JsonProperty(en ? "Turret Damage Multiplier" : "Множитель урона от турелей")] public float turretDamageScale { get; set; }
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
            [JsonProperty(en ? "Use the PVE mode of the plugin? [true/false]" : "Использовать PVE режим работы плагина? [true/false]")] public bool enable { get; set; }
            [JsonProperty(en ? "The amount of damage that the player has to do to become the Event Owner" : "Кол-во урона, которое должен нанести игрок, чтобы стать владельцем ивента")] public float damage { get; set; }
            [JsonProperty(en ? "Damage coefficients for calculate to become the Event Owner." : "Коэффициенты урона для подсчета, чтобы стать владельцем события.")] public Dictionary<string, float> scaleDamage { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event loot the crates? [true/false]" : "Может ли не владелец ивента грабить ящики? [true/false]")] public bool lootCrate { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event hack locked crates? [true/false]" : "Может ли не владелец ивента взламывать заблокированные ящики? [true/false]")] public bool hackCrate { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event loot NPC corpses? [true/false]" : "Может ли не владелец ивента грабить трупы NPC? [true/false]")] public bool lootNpc { get; set; }

            [JsonProperty(en ? "Can an Npc attack a non-owner of the event? [true/false]" : "Может ли Npc атаковать не владельца ивента? [true/false]")] public bool targetNpc { get; set; }
            [JsonProperty(en ? "Can Bradley attack a non-owner of the event? [true/false]" : "Может ли Bradley атаковать не владельца ивента? [true/false]")] public bool targetTank { get; set; }
            [JsonProperty(en ? "Can Helicopter attack a non-owner of the event? [true/false]" : "Может ли Вертолет атаковать не владельца ивента? [true/false]")] public bool targetHeli { get; set; }
            [JsonProperty(en ? "Can Turret attack a non-owner of the event? [true/false]" : "Может ли Турель атаковать не владельца ивента? [true/false]")] public bool targetTurret { get; set; }

            [JsonProperty(en ? "Can the non-owner of the event deal damage to the NPC? [true/false]" : "Может ли не владелец ивента наносить урон по NPC? [true/false]")] public bool damageNpc { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event do damage to Helicopter? [true/false]" : "Может ли не владелец ивента наносить урон по Вертолету? [true/false]")] public bool damageHeli { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event do damage to Bradley? [true/false]" : "Может ли не владелец ивента наносить урон по Bradley? [true/false]")] public bool damageTank { get; set; }
            [JsonProperty(en ? "Can the non-owner of the event do damage to Turret? [true/false]" : "Может ли не владелец ивента наносить урон по Турелям? [true/false]")] public bool damageTurret { get; set; }

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
            [JsonProperty(en ? "Version" : "Версия", Order = 0)] public VersionNumber versionConfig { get; set; }
            [JsonProperty(en ? "Prefix of chat messages" : "Префикс в чате", Order = 1)] public string prefix { get; set; }
            [JsonProperty(en ? "Main Setting" : "Основные настройки", Order = 2)] public MainConfig mainConfig { get; set; }
            [JsonProperty(en ? "Customization Settings" : "Настройки кастомизации", Order = 3)] public CustomizationConfig customizationConfig { get; set; }
            [JsonProperty(en ? "Train presets" : "Пресеты поездов", Order = 4)] public HashSet<TrainConfig> trainConfigs { get; set; }
            [JsonProperty(en ? "Locomotive presets" : "Пресеты локомотивов", Order = 5)] public HashSet<LocomotiveConfig> locomotiveConfigs { get; set; }
            [JsonProperty(en ? "Wagon presets" : "Пресеты вагонов", Order = 6)] public HashSet<BaseWagonConfig> wagonConfigs { get; set; }
            [JsonProperty(en ? "Bradley presets" : "Пресеты бредли", Order = 7)] public HashSet<BradleyConfig> bradleysConfigs { get; set; }
            [JsonProperty(en ? "Turrets presets" : "Пресеты турелей", Order = 8)] public HashSet<TurretConfig> turretConfigs { get; set; }
            [JsonProperty(en ? "Samsite presets" : "Пресеты Samsite", Order = 9)] public HashSet<SamSiteConfig> samsiteConfigs { get; set; }
            [JsonProperty(en ? "Crate presets" : "Пресеты ящиков", Order = 10)] public HashSet<CrateConfig> crateConfigs { get; set; }
            [JsonProperty(en ? "Heli presets" : "Пресеты вертолетов", Order = 11)] public HashSet<HeliConfig> heliConfigs { get; set; }
            [JsonProperty(en ? "NPC presets" : "Пресеты NPC", Order = 12)] public HashSet<NpcConfig> NPCConfigs { get; set; }
            [JsonProperty(en ? "Driver presets" : "Пресеты водителей", Order = 13)] public HashSet<DriverConfig> driverConfigs { get; set; }
            [JsonProperty(en ? "Marker Setting" : "Настройки маркера", Order = 14)] public MarkerConfig markerConfig { get; set; }
            [JsonProperty(en ? "Zone Setting" : "Настройки зоны ивента", Order = 15)] public ZoneConfig zoneConfig { get; set; }
            [JsonProperty(en ? "GUI Setting" : "Настройки GUI", Order = 16)] public GUIConfig guiConfig { get; set; }
            [JsonProperty(en ? "Notification Settings" : "Настройки уведомлений", Order = 17)] public NotifyConfig notifyConfig { get; set; }
            [JsonProperty(en ? "Supported Plugins" : "Поддерживаемые плагины", Order = 18)] public SupportedPluginsConfig supportedPluginsConfig { get; set; }

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    versionConfig = new VersionNumber(1, 5, 1),
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
                        undergroundChance = 0,
                        isRestoreStopTimeAfterDamageOrLoor = true,
                        destrroyWagons = true,
                        useCustomCoords = false,
                        customSpawnPoints = new List<string>(),
                        selfDestructionConfig = new SelfDestructionConfig
                        {
                            enable = true,
                            beepingTime = 10,
                        },
                        enableStartStopLogs = false
                    },
                    customizationConfig = new CustomizationConfig
                    {
                        profileName = "",
                        isElectricFurnacesEnable = false,
                        isBoilersEnable = true,
                        isFireEnable = true,
                        isNeonSignsEnable = true,
                        isLightOnlyAtNight = true,

                        giftCannonSetting = new GiftCannonSetting
                        {
                            isGiftCannonEnable = true,
                            minTimeBetweenItems = 1,
                            maxTimeBetweenItems = 60,
                            itemsConfig = new List<ItemConfig>
                                {
                                    new ItemConfig
                                    {
                                        shortName = "xmas.present.small",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 80,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "xmas.present.medium",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 15,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    },
                                    new ItemConfig
                                    {
                                        shortName = "xmas.present.large",
                                        minAmount = 1,
                                        maxAmount = 1,
                                        chance = 5,
                                        isBluePrint = false,
                                        skinID = 0,
                                        name = ""
                                    }
                                }
                        },
                        fireworksSettings = new FireworksSetting
                        {
                            isFireworksOn = true,
                            timeBetweenFireworks = 600,
                            numberShotsInSalvo = 5,
                            isNighFireworks = true,
                        }
                    },
                    trainConfigs = new HashSet<TrainConfig>
                        {
                            new TrainConfig
                            {
                                presetName = "train_easy",
                                trainName = en ? "Small Train" : "Небольшой поезд",
                                isUndergroundTrain = true,
                                eventTime = 3600,
                                stopTime = 300,
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
                                trainName = en ? "Train" : "Поезд",
                                isUndergroundTrain = false,
                                eventTime = 3600,
                                stopTime = 300,
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
                                trainName = en ? "Giant Train" : "Огромный поезд",
                                isUndergroundTrain = false,
                                eventTime = 3600,
                                stopTime = 300,
                                automaticStart = true,
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
                                chance = 0,
                                locomotivePreset = "locomotive_default",
                                wagonsPreset = new List<string>
                                {
                                    "caboose_wagon"
                                },
                                heliPreset = ""
                            },
                            new TrainConfig
                            {
                                presetName = "train_halloween",
                                trainName = en ? "Halloween Train" : "Хэллоуинский Поезд",
                                isUndergroundTrain = false,
                                eventTime = 3600,
                                stopTime = 300,
                                automaticStart = true,
                                chance = 0,
                                locomotivePreset = "locomotive_new",
                                wagonsPreset = new List<string>
                                {
                                    "wagon_crate_1",
                                    "halloween_wagon",
                                    "wagon_samsite"

                                },
                                heliPreset = ""
                            },
                            new TrainConfig
                            {
                                presetName = "train_xmas_easy",
                                trainName = en ? "Small Christmas train" : "Маленький Новогодний Поезд",
                                isUndergroundTrain = false,
                                eventTime = 3600,
                                stopTime = 300,
                                automaticStart = true,
                                chance = 0,
                                locomotivePreset = "locomotive_default",
                                wagonsPreset = new List<string>
                                {
                                    "xmas_wagon_1"
                                },
                                heliPreset = ""
                            },
                            new TrainConfig
                            {
                                presetName = "train_xmas_medium",
                                trainName = en ? "Medium Christmas train" : "Средний Новогодний Поезд",
                                isUndergroundTrain = false,
                                eventTime = 3600,
                                stopTime = 300,
                                automaticStart = true,
                                chance = 0,
                                locomotivePreset = "locomotive_turret",
                                wagonsPreset = new List<string>
                                {
                                    "xmas_wagon_1",
                                    "xmas_wagon_2"
                                },
                                heliPreset = ""
                            },
                            new TrainConfig
                            {
                                presetName = "train_xmas_hard",
                                trainName = en ? "Big Christmas train" : "Большой Новогодний Поезд",
                                isUndergroundTrain = false,
                                eventTime = 3600,
                                stopTime = 300,
                                automaticStart = true,
                                chance = 0,
                                locomotivePreset = "locomotive_new",
                                wagonsPreset = new List<string>
                                {
                                    "wagon_crate_2",
                                    "xmas_wagon_2",
                                    "wagon_bradley"
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
                            },
                            new LocomotiveConfig
                            {
                                presetName = "locomotive_halloween",
                                prefabName = "assets/content/vehicles/trains/locomotive/locomotive.entity.prefab",
                                engineForce = 500000f,
                                maxSpeed = 14,
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
                                                position = "(-1.341, 1.546, 2)",
                                                rotation = "(0, 0, 0)"
                                            },
                                            new LocationConfig
                                            {
                                                position = "(-1.341, 1.546, -2)",
                                                rotation = "(0, 0, 0)"
                                            },
                                            new LocationConfig
                                            {
                                                position = "(-1.341, 1.546, -6)",
                                                rotation = "(0, 0, 0)"
                                            },
                                            new LocationConfig
                                            {
                                                position = "(1.341, 1.546, 2)",
                                                rotation = "(0, 0, 0)"
                                            },
                                            new LocationConfig
                                            {
                                                position = "(1.341, 1.546, -2)",
                                                rotation = "(0, 0, 0)"
                                            },
                                            new LocationConfig
                                            {
                                                position = "(1.341, 1.546, -6)",
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
                            },
                            new BaseWagonConfig
                            {
                                presetName = "halloween_wagon",
                                prefabName = "assets/content/vehicles/trains/wagons/trainwagonunloadableloot.entity.prefab",
                                brradleys = new Dictionary<string, HashSet<LocationConfig>>(),
                                crates = new Dictionary<string, HashSet<LocationConfig>>
                                {
                                    ["crate_normal_default"] = new HashSet<LocationConfig>
                                    {
                                        new LocationConfig
                                        {
                                            position = "(0.407, 2.403, -3.401)",
                                            rotation = "(303.510, 0, 328.794)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(-0.374, 2.416, 3.104)",
                                            rotation = "(21.106, 261.772, 352.540)"
                                        }
                                    },
                                    ["crate_normal2_default"] = new HashSet<LocationConfig>
                                    {
                                        new LocationConfig
                                        {
                                            position = "(-0.095, 1.817, -0.217)",
                                            rotation = "(19.048, 336.704, 359.624)"
                                        }
                                    }
                                },
                                samsites = new Dictionary<string, HashSet<LocationConfig>>(),
                                decors = new Dictionary<string, HashSet<LocationConfig>>(),
                                NPCs = new Dictionary<string, HashSet<LocationConfig>>(),
                                turrets = new Dictionary<string, HashSet<LocationConfig>>
                                {
                                    ["turret_ak"] = new HashSet<LocationConfig>
                                    {
                                        new LocationConfig
                                        {
                                            position = "(-0.940, 1.559, -6.811)",
                                            rotation = "(0, 180, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(0.940, 1.559, -6.811)",
                                            rotation = "(0, 180, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(-0.940, 1.559, 6.811)",
                                            rotation = "(0, 0, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(0.940, 1.559, 6.811)",
                                            rotation = "(0, 0, 0)"
                                        }
                                    }
                                },
                            },

                            new BaseWagonConfig
                            {
                                presetName = "xmas_wagon_1",
                                prefabName = "assets/content/vehicles/trains/wagons/trainwagonunloadableloot.entity.prefab",
                                brradleys = new Dictionary<string, HashSet<LocationConfig>>(),
                                crates = new Dictionary<string, HashSet<LocationConfig>>
                                {
                                    ["xmas_crate"] = new HashSet<LocationConfig>
                                    {
                                        new LocationConfig
                                        {
                                            position = "(-0.148, 2.707, -1.613)",
                                            rotation = "(72.214, 0, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(-0.221, 2.478, -2.314)",
                                            rotation = "(52.555, 180.000, 0)"
                                        }
                                    }
                                },
                                samsites = new Dictionary<string, HashSet<LocationConfig>>(),
                                decors = new Dictionary<string, HashSet<LocationConfig>>(),
                                NPCs = new Dictionary<string, HashSet<LocationConfig>>(),
                                turrets = new Dictionary<string, HashSet<LocationConfig>>
                                {
                                    ["turret_ak"] = new HashSet<LocationConfig>
                                    {
                                        new LocationConfig
                                        {
                                            position = "(-0.940, 1.559, -6.811)",
                                            rotation = "(0, 180, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(0.940, 1.559, -6.811)",
                                            rotation = "(0, 180, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(-0.940, 1.559, 6.811)",
                                            rotation = "(0, 0, 0)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(0.940, 1.559, 6.811)",
                                            rotation = "(0, 0, 0)"
                                        }
                                    }
                                },
                            },
                            new BaseWagonConfig
                            {
                                presetName = "xmas_wagon_2",
                                prefabName = "assets/content/vehicles/trains/wagons/trainwagonunloadable.entity.prefab",
                                brradleys = new Dictionary<string, HashSet<LocationConfig>>(),
                                 crates = new Dictionary<string, HashSet<LocationConfig>>
                                {
                                    ["xmas_crate"] = new HashSet<LocationConfig>
                                    {
                                        new LocationConfig
                                        {
                                            position = "(0.027, 3.276, -3.562)",
                                            rotation = "(0.361, 355.541, 16.972)"
                                        },
                                        new LocationConfig
                                        {
                                            position = "(0.027, 3.276, 3.897)",
                                            rotation = "(334.884, 355.419, 352.239)"
                                        }
                                    }
                                },
                                samsites = new Dictionary<string, HashSet<LocationConfig>>(),
                                decors = new Dictionary<string, HashSet<LocationConfig>>(),
                                NPCs = new Dictionary<string, HashSet<LocationConfig>>(),
                                turrets = new Dictionary<string, HashSet<LocationConfig>>
                                {
                                },
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
                            },
                            new CrateConfig
                            {
                                presetName = "crate_normal_default",
                                prefab = "assets/bundled/prefabs/radtown/crate_normal.prefab",
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
                                presetName = "crate_normal2_default",
                                prefab = "assets/bundled/prefabs/radtown/crate_normal_2.prefab",
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
                                presetName = "xmas_crate",
                                prefab = "assets/prefabs/missions/portal/proceduraldungeon/xmastunnels/loot/xmastunnellootbox.prefab",
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
                                distance = 250f,
                                speed = 25f,
                                outsideTime = 30
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
                                        Mods = new HashSet<string>{ "weapon.mod.holosight" }
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
                    markerConfig = new MarkerConfig
                    {
                        enable = true,
                        useRingMarker = true,
                        useShopMarker = true,
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
                        preStartTime = 0,
                        chat = true,
                        timeNotifications = new HashSet<int>
                            {
                                300,
                                60,
                                30,
                                5
                            }
                    },
                    supportedPluginsConfig = new SupportedPluginsConfig
                    {
                        pveMode = new PveModeConfig
                        {
                            enable = false,
                            damage = 500f,
                            scaleDamage = new Dictionary<string, float>
                            {
                                ["Npc"] = 1f,
                                ["Bradley"] = 2f,
                                ["Helicopter"] = 2f,
                                ["Turret"] = 2f,
                            },
                            lootCrate = false,
                            hackCrate = false,
                            lootNpc = false,
                            damageNpc = false,
                            targetNpc = false,
                            damageTank = false,
                            targetTank = false,
                            damageTurret = false,
                            targetTurret = false,
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
