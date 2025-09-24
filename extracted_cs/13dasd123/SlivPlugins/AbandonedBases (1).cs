// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿/*
*  <----- End-User License Agreement ----->
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
*  Copyright © 2022-2023 nivex
*/

using Facepunch;
using Facepunch.Math;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Plugins.AbandonedBasesExtensionMethods;
using Rust;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Abandoned Bases", "nivex", "2.0.8")]
    [Description("Allows bases to become raidable when the owner becomes inactive")]
    public class AbandonedBases : RustPlugin
    {
        #region Variables
        [PluginReference] Plugin Backpacks, Economics, ServerRewards, IQEconomic, RaidableBases, Clans, Friends, Notify, AdvancedAlerts, ZoneManager, SkillTree;

        private new const string Name = "Abandoned Bases";
        private List<string> ID_FLOORS = new List<string> { "floor", "floor.frame", "floor.grill", "floor.ladder.hatch", "floor.triangle", "floor.triangle.frame", "floor.triangle.grill", "floor.triangle.ladder.hatch" };
        private List<string> ID_DQD = new List<string> { "foundation", "foundation.triangle", "roof.triangle", "roof" };
        private List<string> TrueDamage = new List<string> { "Barricade", "SimpleBuildingBlock", "IceFence", "TeslaCoil", "BaseTrap", "GunTrap", "FlameTurret", "FogMachine", "SamSite", "AutoTurret" };
        private Coroutine reportCoroutine;
        private bool DebugMode { get; set; }
        private bool newSave { get; set; }
        private bool purgeDay { get; set; }
        private StoredData data { get; set; } = new StoredData();
        private StringBuilder _sb { get; set; } = new StringBuilder();
        private static AbandonedBases Instance { get; set; }
        private Coroutine abandonedCoroutine { get; set; }
        private List<ulong> AbandonedSleepers { get; set; } = new List<ulong>();
        private List<string> _waitingList { get; set; } = new List<string>();
        private List<UserConversion> _conversions { get; set; } = new List<UserConversion>();
        private List<AbandonedBuilding> AbandonedBuildings { get; set; } = new List<AbandonedBuilding>();
        private Dictionary<ulong, List<Notification>> _notifications { get; set; } = new Dictionary<ulong, List<Notification>>();
        private Dictionary<ulong, AbandonedBuilding> AbandonedReferences { get; set; } = new Dictionary<ulong, AbandonedBuilding>();
        private Dictionary<BuildingPrivlidge, List<BaseEntity>> _buildings { get; set; } = new Dictionary<BuildingPrivlidge, List<BaseEntity>>();

        public class UserConversion
        {
            public string userid;
            public Coroutine co;
            public List<ulong> owners;
            public UserConversion(string userid, List<ulong> owners, Coroutine co)
            {
                this.owners = owners;
                this.userid = userid;
                this.co = co;
            }
            public bool Exists(IPlayer user, List<ulong> owners)
            {
                if (co == null)
                {
                    return false;
                }
                return userid == user.Id || this.owners.Exists(owners.Contains);
            }
        }

        public class Notification
        {
            public BasePlayer player;
            public string messageEx;
        }

        private class StoredData
        {
            [JsonProperty("Last Seen", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<ulong, int> LastSeen { get; set; } = new Dictionary<ulong, int>();

            [JsonProperty("Cooldown Between Conversion", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<ulong, DateTime> CooldownBetweenConversion { get; set; } = new Dictionary<ulong, DateTime>();

            [JsonProperty("Cooldown Between Cancel", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<ulong, DateTime> CooldownBetweenCancel { get; set; } = new Dictionary<ulong, DateTime>();

            [JsonProperty("Cooldown Between Events", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<ulong, DateTime> CooldownBetweenEvents { get; set; } = new Dictionary<ulong, DateTime>();

            [JsonProperty("Activities", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ActivityInfo> Activity { get; set; } = new List<ActivityInfo>();

            public string LastRunTime { get; set; } = DateTime.MinValue.ToString();

            public StoredData() { }
        }

        public class Payment
        {
            public Payment(BasePlayer player, double cost = 0, List<CustomCostOptions> options = null)
            {
                userId = player.userID;
                this.options = options;
                this.player = player;
                this.cost = cost;
            }

            public double cost { get; set; }
            public ulong userId { get; set; }
            public List<CustomCostOptions> options { get; set; }
            public BasePlayer player { get; set; }
        }

        private class ActivityInfo
        {
            [JsonProperty(PropertyName = "owners", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> owners { get; set; } = new List<ulong>();

            [JsonProperty(PropertyName = "permission")]
            public string perm { get; set; } = string.Empty;

            [JsonProperty(PropertyName = "total")]
            public int total { get; set; }

            public ActivityInfo() { }

            public ActivityInfo(List<ulong> owners)
            {
                this.owners = owners;
                SetPermission();
            }

            public int GetLimit()
            {
                if (string.IsNullOrEmpty(perm) || !perm.PermissionExists())
                {
                    SetPermission();
                }
                foreach (var purge in config.Purges)
                {
                    if (purge.Permission.Equals(perm, StringComparison.OrdinalIgnoreCase))
                    {
                        if (purge.NoPurge)
                        {
                            return 0;
                        }
                        return purge.Limit;
                    }
                }
                return 0;
            }

            private void SetPermission()
            {
                var limit = int.MinValue;
                foreach (var owner in this.owners)
                {
                    var purge = PurgeSettings.Find(owner);
                    if (purge?.Permission == null)
                    {
                        continue;
                    }
                    if (purge.Limit > limit)
                    {
                        perm = purge.Permission;
                        limit = purge.Limit;
                    }
                    if (purge.Limit <= 0)
                    {
                        perm = purge.Permission;
                        break;
                    }
                }
            }

            public bool SameOwners(List<ulong> owners)
            {
                foreach (var owner in this.owners)
                {
                    if (!owners.Contains(owner))
                    {
                        return false;
                    }
                }

                return true;
            }

            public static ActivityInfo Find(List<ulong> owners, List<ActivityInfo> activity)
            {
                ActivityInfo result = null;
                int j = int.MinValue;

                foreach (var other in activity)
                {
                    if (other.SameOwners(owners))
                    {
                        int k = other.GetLimit();

                        if (k > j)
                        {
                            result = other;
                            j = k;
                        }
                    }
                }

                if (result == null)
                {
                    result = new ActivityInfo(owners);
                }

                return result;
            }
        }

        private class DelaySettings
        {
            public static Dictionary<ulong, DelaySettings> PvpDelay { get; set; } = new Dictionary<ulong, DelaySettings>();
            public AbandonedBuilding Building { get; set; }
            public Timer Timer { get; set; }

            public static void Remove(BasePlayer player)
            {
                DelaySettings ds;
                if (PvpDelay.TryGetValue(player.userID, out ds))
                {
                    if (ds.Timer != null && !ds.Timer.Destroyed)
                    {
                        ds.Timer.Callback.Invoke();
                        ds.Timer.Destroy();
                    }

                    PvpDelay.Remove(player.userID);
                }
            }

            public static void Add(AbandonedBuilding abandonedBuilding, BasePlayer player)
            {
                if (config.Abandoned.PVPDelay <= 0f)
                {
                    Message(player, abandonedBuilding.AllowPVP ? "OnPlayerExit" : "OnPlayerExitPVE");

                    return;
                }

                DelaySettings ds;
                if (!PvpDelay.TryGetValue(player.userID, out ds))
                {
                    ulong userid = player.userID;

                    Interface.CallHook("OnPlayerPvpDelayStart", player, userid, abandonedBuilding.center, abandonedBuilding.intruders, abandonedBuilding.entities);

                    PvpDelay[userid] = ds = new DelaySettings
                    {
                        Timer = abandonedBuilding.timer.Once(config.Abandoned.PVPDelay, () =>
                        {
                            Interface.CallHook("OnPlayerPvpDelayExpiredII", player, userid, abandonedBuilding.center, abandonedBuilding.intruders, abandonedBuilding.entities);
                            PvpDelay.Remove(userid);
                        }),
                        Building = abandonedBuilding
                    };

                    Message(player, "DoomAndGloom", _("PVPFlag", player.UserIDString), config.Abandoned.PVPDelay);
                }
                else ds.Timer.Reset();
            }
        }

        private class AbandonedBuilding : FacepunchBehaviour
        {
            internal class EntityOwner
            {
                public BaseEntity Entity;
                public ulong OwnerID;
                public EntityOwner(BaseEntity entity)
                {
                    Entity = entity;
                    OwnerID = entity.OwnerID;
                }
            }

            internal List<ulong> IsAllowed { get; set; } = new List<ulong>();
            internal List<ulong> Messages { get; set; } = new List<ulong>();
            internal HashSet<uint> buildingIDs { get; set; } = new HashSet<uint>();
            internal List<BuildingPrivlidge> privs { get; set; } = new List<BuildingPrivlidge>();
            internal List<ulong> owners { get; set; } = new List<ulong>();
            internal List<BaseEntity> entities { get; set; } = new List<BaseEntity>();
            internal List<SphereEntity> spheres { get; set; } = new List<SphereEntity>();
            internal List<Vector3> compound { get; set; } = new List<Vector3>();
            internal List<Vector3> foundations { get; set; } = new List<Vector3>();
            internal List<BasePlayer> intruders { get; set; } = new List<BasePlayer>();
            internal List<StorageContainer> containers { get; set; } = new List<StorageContainer>();
            internal Dictionary<ulong, EntityOwner> EntityOwners { get; set; } = new Dictionary<ulong, EntityOwner>();
            internal MapMarkerGenericRadius genericMarker { get; set; } = null;
            internal VendingMachineMapMarker vendingMarker { get; set; } = null;
            internal bool IsOwnerLocked { get; set; }
            internal bool DisableEventCooldown { get; set; }
            internal PluginTimers timer { get; set; }
            internal BaseEntity anchor { get; set; }
            internal DateTime DespawnDateTime { get; set; }
            internal float radius { get; set; }
            internal bool markerCreated { get; set; }
            internal bool isDestroyed { get; set; }
            internal bool IsClaimed { get; set; }
            internal bool AllowPVP { get; set; }
            internal bool IsExpired { get; set; }
            internal SphereCollider _collider { get; set; }
            internal Payment payment { get; set; }
            internal List<string> groups { get; set; } = new List<string>();
            internal GameObject go { get; set; }
            internal float cancelCooldownTime { get; set; }
            internal bool isCanceled { get; set; }
            internal string currentName { get; set; }
            internal ulong currentId { get; set; }
            internal string previousName { get; set; }
            internal ulong previousId { get; set; }
            internal bool canReassign { get; set; } = true;
            internal ActivityInfo activity { get; set; }
            internal int lootAmount { get; set; }
            internal Coroutine _coroutine { get; set; }
            internal AbandonedBases MyInstance { get; set; }
            internal bool LockBaseToFirstAttacker => AllowPVP ? config.Abandoned.LockBaseToFirstAttackerPVP : config.Abandoned.LockBaseToFirstAttackerPVE;
            internal bool EjectFromLockedBase => AllowPVP ? config.Abandoned.EjectLockedPVP : config.Abandoned.EjectLockedPVE;
            internal string GetGrid() => PhoneController.PositionToGridCoord(center);
            internal Vector3 _center { get; set; }

            internal Vector3 center
            {
                get
                {
                    if (!anchor.IsKilled())
                    {
                        _center = anchor.transform.position;
                    }
                    return _center;
                }
                set
                {
                    _center = value;
                }
            }

            public bool InRange(Vector3 from) => AbandonedBases.InRange(from, center, radius);

            public bool NearCompound(Vector3 from) => compound.Exists(to => AbandonedBases.InRange(from, to, 3f));

            public bool NearFoundation(Vector3 from) => foundations.Exists(to => AbandonedBases.InRange(from, to, 3f));

            public static bool AddRange(List<BaseEntity> entities, List<StorageContainer> containers, List<Vector3> foundations, List<Vector3> compound, List<Vector3> walls)
            {
                var floors = new List<Vector3>();

                foreach (var e in entities.ToList())
                {
                    if (e.IsKilled())
                    {
                        entities.Remove(e);
                        continue;
                    }

                    if (e.ShortPrefabName == "foundation" || e.ShortPrefabName == "foundation.triangle" || e.skinID == 1337424001 && e is CollectibleEntity)
                    {
                        if (config.Abandoned.Twig || (e as BuildingBlock).grade != BuildingGrade.Enum.Twigs)
                        {
                            foundations.Add(e.transform.position);
                            compound.Add(e.transform.position);
                        }
                    }
                    else if (e.ShortPrefabName.Contains("external.high"))
                    {
                        compound.Add(e.transform.position);
                    }
                    else if (e.ShortPrefabName == "wall" || e.ShortPrefabName == "wall.half" || e.ShortPrefabName == "wall.window")
                    {
                        if (config.Abandoned.Twig || (e as BuildingBlock)?.grade != BuildingGrade.Enum.Twigs)
                        {
                            walls.Add(e.transform.position);
                        }
                    }
                    else if (Instance.ID_FLOORS.Contains(e.ShortPrefabName))
                    {
                        floors.Add(e.transform.position);
                    }
                    else if (IsBox(e) || e is BuildingPrivlidge)
                    {
                        containers.Add(e as StorageContainer);
                    }
                }

                if (foundations.Count == 0 || foundations.Count < config.Abandoned.FoundationLimit || walls.Count < config.Abandoned.WallLimit)
                {
                    return false;
                }

                return true;
            }

            public static bool AddRange(List<BaseEntity> entities, List<StorageContainer> containers, List<Vector3> compound, ref int loot)
            {
                List<BaseEntity.Slot> _checkSlots = new List<BaseEntity.Slot> { BaseEntity.Slot.Lock, BaseEntity.Slot.UpperModifier, BaseEntity.Slot.MiddleModifier, BaseEntity.Slot.LowerModifier };

                foreach (var e in entities.ToList())
                {
                    if (e.IsKilled())
                    {
                        entities.Remove(e);
                        continue;
                    }

                    if (IsBox(e) || e is BuildingPrivlidge)
                    {
                        containers.Add(e as StorageContainer);
                    }

                    foreach (var checkSlot in _checkSlots)
                    {
                        var slot = e.GetSlot(checkSlot);
                        if (slot == null) continue;
                        if (entities.Contains(slot)) continue;
                        entities.Add(slot);
                    }

                    var container = e as IItemContainerEntity;

                    if (container?.inventory?.itemList != null)
                    {
                        loot += container.inventory.itemList.Count;
                    }

                    compound.Add(e.transform.position);
                }

                return config.Abandoned.Tugboats.Loot <= 0 || loot >= config.Abandoned.Tugboats.Loot;
            }

            public void TrySetOwner(IPlayer user)
            {
                if (user == null || user.IsServer)
                {
                    return;
                }
                var player = user.Object as BasePlayer;
                if (player == null || player.IsFlying || player.limitNetworking)
                {
                    return;
                }
                SetOwner(player.userID, player.displayName, player.userID, player.displayName);
            }

            public void TrySetOwnerLock(BasePlayer attacker)
            {
                if (IsOwnerLocked && !CanBypass(attacker) && !IsAlly(attacker))
                {
                    TryEjectFromLockedBase(attacker);
                    return;
                }
                if (!canReassign || !LockBaseToFirstAttacker || !attacker.IsHuman() || owners.Contains(attacker.userID) || MyInstance.HasEventCooldown(attacker, this) || IsAlly(attacker))
                {
                    return;
                }
                IsOwnerLocked = true;
                canReassign = false;
                SetOwner(attacker.userID, attacker.displayName, currentId, currentName);
                Invoke(UpdateMarkers, 0f);
            }

            public bool CanBypass(BasePlayer player)
            {
                return !player.IsHuman() || player.IsFlying || player.limitNetworking || player.HasPermission("abandonedbases.canbypass");
            }

            public bool CanDoActions(BasePlayer player)
            {
                return owners.Contains(player.userID) || CanBypass(player) || IsAlly(player);
            }

            public void SetOwner(ulong newid, string newname, ulong currid, string currname)
            {
                previousName = currname;
                previousId = currid;
                currentName = newname;
                currentId = newid;
            }

            public void TryEjectFromLockedBase(BasePlayer player)
            {
                if (!EjectFromLockedBase || owners.Contains(player.userID))
                {
                    return;
                }
                if (anchor == null && !NearFoundation(player.transform.position))
                {
                    return;
                }
                if (anchor is Tugboat && !(player.GetParentEntity() is Tugboat))
                {
                    return;
                }
                RemovePlayer(player);
            }

            public void RemovePlayer(BasePlayer player)
            {
                var m = player.GetMounted();
                if (m != null)
                {
                    m.DismountPlayer(player, true);
                }
                if (player.HasParent())
                {
                    player.SetParent(null);
                }
                var position = GetEjectLocation(player.transform.position, 10f, center, radius);
                player.Teleport(player.IsFlying ? position.WithY(player.transform.position.y) : position);
                player.SendNetworkUpdateImmediate();
                intruders.Remove(player);
            }

            public static Vector3 GetEjectLocation(Vector3 a, float distance, Vector3 target, float radius)
            {
                var position = ((a.XZ3D() - target.XZ3D()).normalized * (radius + distance)) + target; // credits ZoneManager
                float y = TerrainMeta.HighestPoint.y + 250f;

                RaycastHit hit;
                if (Physics.Raycast(position + new Vector3(0f, y, 0f), Vector3.down, out hit, Mathf.Infinity, 10551313, QueryTriggerInteraction.Ignore))
                {
                    position.y = hit.point.y + 0.75f;
                }
                else position.y = Mathf.Max(TerrainMeta.HeightMap.GetHeight(position), TerrainMeta.WaterMap.GetHeight(position)) + 0.75f;

                return position;
            }

            public void TryMessage(BasePlayer player, string key, params object[] args)
            {
                if (Messages.Contains(player.userID)) return;
                ulong userid = player.userID;
                Messages.Add(userid);
                timer.Once(10f, () => Messages.Remove(userid));
                Message(player, key, args);
            }

            public void Setup(AbandonedBases instance, IPlayer user, PluginTimers timer, List<Vector3> compound, List<Vector3> foundations, List<BaseEntity> entities, List<StorageContainer> containers, List<ulong> owners, Vector3 center, Payment payment, float radius, bool allowPVP)
            {
                MyInstance = instance;

                if (EjectFromLockedBase)
                {
                    timer.Every(1f, Protector);
                }

                TrySetOwner(user);

                markerCreated = user == null && !config.Abandoned.AutoMarkers || user != null && !config.Abandoned.ManualMarkers;
                canReassign = currentId == 0 || string.IsNullOrEmpty(currentName);

                MyInstance.AbandonedBuildings.Add(this);

                this.AllowPVP = allowPVP;
                this.compound = compound;
                this.foundations = foundations;
                this.containers = containers;
                this.entities = entities;
                this.payment = payment;
                this.owners = owners;
                this.radius = radius;
                this.timer = timer;
                this.center = center;

                Interface.Oxide.NextTick(() =>
                {
                    TryInvokeMethod(SetupEntities);
                    TryInvokeMethod(SetupCollider);
                    TryInvokeMethod(CompleteConvertPayment);
                    TryInvokeMethod(Announce);
                    TryInvokeMethod(Refresh);
                    TryInvokeMethod(SpawnNpcs);
                    TryInvokeMethod(SetupSleepers);
                    UpdateActivity(owners);

                    cancelCooldownTime = Time.time + config.Abandoned.CancelCooldown;
                });

                Interface.CallHook("OnAbandonedBaseStarted", center, AllowPVP, intruders, entities);
            }

            private void Awake()
            {
                go = gameObject;
            }

            private void OnDestroy()
            {
                if (groups.Count > 0)
                {
                    Plugin BotReSpawn = Interface.Oxide.RootPluginManager.GetPlugin("BotReSpawn");
                    groups.ForEach(group => BotReSpawn?.Call("RemoveGroupSpawn", group));
                }
            }

            private bool IsAlly(BasePlayer player) => MyInstance.IsAlly(player.userID, currentId);

            private void Protector()
            {
                if (isDestroyed || !IsOwnerLocked || intruders.Count == 0)
                {
                    return;
                }
                foreach (var intruder in intruders.ToList())
                {
                    if (intruder.IsKilled())
                    {
                        intruders.Remove(intruder);
                        continue;
                    }
                    if (IsAllowed.Contains(intruder.userID))
                    {
                        continue;
                    }
                    if (CanDoActions(intruder))
                    {
                        IsAllowed.Add(intruder.userID);
                        continue;
                    }
                    TryEjectFromLockedBase(intruder);
                }
            }

            private void OnTriggerEnter(Collider collider)
            {
                if (collider == null)
                {
                    return;
                }

                var entity = collider.ToBaseEntity();

                if (entity is BasePlayer)
                {
                    var player = entity as BasePlayer;

                    if (!intruders.Contains(player))
                    {
                        intruders.Add(player);

                        Message(player, AllowPVP ? "OnPlayerEntered" : "OnPlayerEnteredPVE");

                        Interface.CallHook("OnPlayerEnteredAbandonedBase", player, transform.position, AllowPVP, center, intruders, entities);
                    }
                }
                else if (entity is BaseMountable)
                {
                    var m = entity as BaseMountable;

                    GetMountedPlayers(m).ForEach(player =>
                    {
                        if (!intruders.Contains(player))
                        {
                            intruders.Add(player);

                            Message(player, AllowPVP ? "OnPlayerEntered" : "OnPlayerEnteredPVE");

                            Interface.CallHook("OnPlayerEnteredAbandonedBase", player, center, AllowPVP, intruders, entities);
                        }
                    });
                }
            }

            private void OnTriggerExit(Collider collider)
            {
                if (collider == null)
                {
                    return;
                }

                var entity = collider.ToBaseEntity();

                if (entity is BasePlayer)
                {
                    var player = entity as BasePlayer;

                    OnPlayerExit(player, player.IsDead());
                }
                else if (entity is BaseMountable)
                {
                    var m = entity as BaseMountable;

                    GetMountedPlayers(m).ForEach(player => OnPlayerExit(player, player.IsDead()));
                }
            }

            public void DestroyMe()
            {
                isDestroyed = true;
                TryInvokeMethod(CancelEntitySetup);
                TryInvokeMethod(KillMarkers);
                TryInvokeMethod(KillSpheres);
                TryInvokeMethod(RewardPlayers);
                TryInvokeMethod(PowerDownAutoTurrets);
                TryInvokeMethod(RestoreEntityOwners);
                TryInvokeMethod(RemoveReferences);
                TryInvokeMethod(CancelInvokes);
                if (anchor.IsKilled()) Destroy(go);
                Destroy(this);
            }

            private void SetupSleepers()
            {
                if (!config.KillInactiveSleepers || !config.MoveInventory)
                {
                    return;
                }
                var boxes = containers.Where(x => !x.IsKilled() && IsBox(x) && !x.inventory.IsFull());
                if (boxes.Count == 0)
                {
                    return;
                }
                foreach (var entity in entities)
                {
                    if (entity.IsKilled() || !(entity is BasePlayer)) continue;
                    var target = entity as BasePlayer;
                    if (!target.IsSleeping() || target.IsConnected || target.inventory == null) continue;
                    var items = target.inventory.AllItems().ToList();
                    while (items.Count > 0 && boxes.Count > 0)
                    {
                        var box = boxes.GetRandom();
                        if (box.inventory.IsFull())
                        {
                            boxes.Remove(box);
                            continue;
                        }
                        Item item = items[0];
                        items.Remove(item);
                        if (item == null)
                        {
                            continue;
                        }
                        if (config.MoveInventoryBlacklist.Contains(item.info.shortname) || !item.MoveToContainer(box.inventory))
                        {
                            item.RemoveFromContainer();
                            item.Remove(0f);
                        }
                    }
                }
            }

            private void RewardPlayers()
            {
                var raiders = intruders.Where(player => !IsPlayerIneligible(player));
                foreach (var player in raiders)
                {
                    if (!isCanceled)
                    {
                        MyInstance.GiveRewards(player, raiders.Count);
                    }
                    if (!DisableEventCooldown)
                    {
                        SetEventCooldown(player.userID);
                    }
                }
                if (isCanceled)
                {
                    return;
                }
                var players = string.Join(", ", raiders.Select(x => x.displayName));
                var text = $"{currentName} ({currentId}) has raided the base with {players} at {center} ({GetGrid()}) owned by {previousName} ({previousId})";
                if (raiders.Count == 0) text = $"{currentName} ({currentId}) base has been abandoned at {center} ({GetGrid()}) by {previousName} ({previousId})";
                if (config.UseLogFile && previousId != 0uL && previousId != currentId)
                {
                    MyInstance.LogToFile("sar", text, MyInstance, false, true);
                }
                Puts(text);
            }

            private bool IsPlayerIneligible(BasePlayer player)
            {
                return player == null || player.IsFlying || player.limitNetworking || config.Abandoned.RemoveAdminRaiders && player.IsAdmin;
            }

            private void RestoreEntityOwners()
            {
                if (IsExpired || IsClaimed || EntityOwners.Count == 0)
                {
                    return;
                }

                foreach (var pair in EntityOwners.ToList())
                {
                    if (pair.Value.Entity.IsKilled()) continue;
                    pair.Value.Entity.OwnerID = pair.Value.OwnerID;
                }
            }

            private void RemoveReferences() => MyInstance.RemoveReferences(this, entities);

            private void CancelInvokes() { try { CancelInvoke(DestroyAll); } catch { } }

            public void KillMarkers()
            {
                vendingMarker.SafelyKill();
                genericMarker.SafelyKill();
            }

            public void KillSpheres()
            {
                spheres.ToList().ForEach(sphere => sphere.SafelyKill());
            }

            public void KillCollider()
            {
                if (_collider != null)
                {
                    DestroyImmediate(_collider);
                }
            }

            private void CompleteConvertPayment() => CompletePayment(payment, false);

            public void CompleteCancelPayment(Payment payment) => CompletePayment(payment, true);

            public void SetConversionCooldown(ulong userid)
            {
                if (config?.Abandoned.CooldownBetweenConversion > 0 && !userid.HasPermission("abandonedbases.convert.nocooldown"))
                {
                    MyInstance.data.CooldownBetweenConversion[userid] = DateTime.Now.AddSeconds(config.Abandoned.CooldownBetweenConversion);
                }
            }

            public void SetCancelCooldown(ulong userid)
            {
                if (config?.Abandoned.CooldownBetweenCancel > 0 && !userid.HasPermission("abandonedbases.convert.cancel.nocooldown"))
                {
                    MyInstance.data.CooldownBetweenCancel[userid] = DateTime.Now.AddSeconds(config.Abandoned.CooldownBetweenCancel);
                }
            }

            public void SetEventCooldown(ulong userid)
            {
                if (config?.Abandoned.CooldownBetweenEvents > 0 && !userid.HasPermission("abandonedbases.noeventcooldown"))
                {
                    MyInstance.data.CooldownBetweenEvents[userid] = DateTime.Now.AddSeconds(config.Abandoned.CooldownBetweenEvents);
                }
            }

            private void CompletePayment(Payment payment, bool cancel)
            {
                if (cancel)
                {
                    isCanceled = config.Abandoned.Rewards.Cancel;
                }

                if (payment == null || payment.cost == 0 && !payment.options.IsValid())
                {
                    return;
                }

                var eco = cancel ? config.Abandoned.EconomicsCancel : config.Abandoned.Economics;

                if (eco > 0 && MyInstance.Economics.CanCall())
                {
                    if (Convert.ToBoolean(MyInstance.Economics?.Call("Withdraw", payment.userId.ToString(), payment.cost)))
                    {
                        if (payment.player.IsValid())
                        {
                            Message(payment.player, cancel ? "EconomicsWithdrawCancel" : "EconomicsWithdraw", payment.cost);
                        }
                    }
                }

                var rp = cancel ? config.Abandoned.ServerRewardsCancel : config.Abandoned.ServerRewards;

                if (rp > 0 && MyInstance.ServerRewards.CanCall())
                {
                    if (Convert.ToBoolean(MyInstance.ServerRewards?.Call("TakePoints", payment.userId, (int)payment.cost)))
                    {
                        if (payment.player.IsValid())
                        {
                            Message(payment.player, cancel ? "ServerRewardPointsTakenCancel" : "ServerRewardPointsTaken", (int)payment.cost);
                        }
                    }
                }

                if (payment.options.IsValid())
                {
                    TakeCustomCost(payment.player, payment.options, cancel);
                }
            }

            private void TakeCustomCost(BasePlayer player, List<CustomCostOptions> options, bool cancel)
            {
                var sb = new StringBuilder();

                foreach (var option in options)
                {
                    if (option.Amount <= 0) continue;
                    var slots = player.inventory.FindItemsByItemID(option.Definition.itemid);
                    var amountLeft = option.Amount;

                    foreach (var slot in slots)
                    {
                        if (slot == null || option.Skin != 0 && slot.skin != option.Skin)
                        {
                            continue;
                        }

                        var taken = slot.amount > amountLeft ? slot.SplitItem(amountLeft) : slot;

                        if (taken == null)
                        {
                            continue;
                        }

                        taken.RemoveFromContainer();
                        taken.Remove(0f);

                        amountLeft -= taken.amount;

                        if (amountLeft <= 0)
                        {
                            string name = string.IsNullOrEmpty(option.Name) ? slot.info.displayName.english : option.Name;
                            sb.Append(string.Format("{0} {1}", option.Amount, name)).Append(", ");
                            break;
                        }
                    }
                }

                if (sb.Length > 2)
                {
                    sb.Length -= 2;

                    Message(player, cancel ? "CustomCostTakenCancel" : "CustomCostTaken", sb.ToString());
                }
            }

            public void Announce()
            {
                if (MyInstance.purgeDay)
                {
                    return;
                }

                var grid = GetGrid();

                foreach (var target in BasePlayer.activePlayerList)
                {
                    if (target.HasPermission("abandonedbases.notices"))
                    {
                        Message(target, "Abandoned", grid);
                    }
                }
            }

            public bool IsAttached(BaseEntity entity)
            {
                if (entity is DecayEntity)
                {
                    var decayEntity = entity as DecayEntity;
                    if (buildingIDs.Contains(decayEntity.buildingID))
                    {
                        return true;
                    }
                }
                var priv = entity.GetBuildingPrivilege();
                if (priv == null)
                {
                    return NearCompound(entity.transform.position);
                }
                return buildingIDs.Contains(priv.buildingID);
            }

            public void RememberOwner(BaseEntity entity)
            {
                if (entity.OwnerID == 0 || owners.Contains(entity.OwnerID))
                {
                    MyInstance.AbandonedReferences[entity.net.ID.Value] = this;
                    EntityOwners[entity.net.ID.Value] = new EntityOwner(entity);
                    var ice = entity as IItemContainerEntity;
                    if (ice == null || ice.inventory == null) return;
                    lootAmount += ice.inventory.itemList.Count;
                }
            }

            private void CancelEntitySetup()
            {
                Interface.CallHook("OnAbandonedBaseEnded", center, AllowPVP, intruders, entities);

                if (_coroutine != null)
                {
                    ServerMgr.Instance.StopCoroutine(_coroutine);
                    _coroutine = null;
                }
            }

            private IEnumerator EntitySetup()
            {
                float invokeTime = 0f;
                int checks = 0;

                foreach (var e in entities.ToList())
                {
                    if (++checks % 100 == 0)
                    {
                        yield return CoroutineEx.waitForSeconds(0.025f);
                    }
                    if (e.IsKilled() || e.net == null || e.OwnerID == 0 && e is StorageContainer || e.OwnerID != 0 && !owners.Contains(e.OwnerID))
                    {
                        entities.Remove(e);
                        continue;
                    }
                    RememberOwner(e);
                    //if (e is BaseCombatEntity) TryInvokeMethod(() => SetupHealth(e as BaseCombatEntity));
                    if (e is BuildingPrivlidge) SetCurrentNameFromBuilding(e);
                    if (e is Tugboat) SetCurrentNameFromVehicle(e);
                    if (e is DecayEntity) SetupDecayEntity(e as DecayEntity);
                    if (e is AutoTurret) MyInstance.timer.Once(invokeTime += 0.1f, () => SetupTurret(e as AutoTurret));
                    if (config.Abandoned.DespawnTime <= 0f && !config.RemoveOwnershipZero) continue;
                    if (config.RemoveOwnership || config.RemoveOwnershipFromContainers && e is StorageContainer) e.OwnerID = 0;
                }

                var type = anchor.IsKilled() ? "BUILDING" : $"{anchor.GetType().Name.ToUpper()}";

                if (!string.IsNullOrEmpty(previousName))
                {
                    Puts("{0} - {1} ({2}) at {3} with {4} entities and {5} items", type, previousName, previousId, center, entities.Count, lootAmount);
                }
                else Puts("{0} - {1} with {2} entities and {3} items", type, center, entities.Count, lootAmount);

                _coroutine = null;
            }

            private void SetCurrentNameFromBuilding(BaseEntity entity)
            {
                if (!string.IsNullOrEmpty(currentName)) return;
                if (entity.OwnerID.IsSteamId() && owners.Contains(entity.OwnerID))
                {
                    var username = GetUserName(entity.OwnerID);

                    SetOwner(entity.OwnerID, username, entity.OwnerID, username);
                }
                privs.Add(entity as BuildingPrivlidge);
            }

            private void SetCurrentNameFromVehicle(BaseEntity entity)
            {
                if (!string.IsNullOrEmpty(currentName)) return;
                var priv = MyInstance.GetVehiclePrivilege(entity.children);
                if (priv == null || !priv.AnyAuthed()) return;
                foreach (var auth in priv.authorizedPlayers)
                {
                    var username = GetUserName(auth.userid);
                    if (string.IsNullOrEmpty(username)) continue;
                    SetOwner(auth.userid, username, auth.userid, username);
                    break;
                }
            }

            private void SetupEntities()
            {
                _coroutine = ServerMgr.Instance.StartCoroutine(EntitySetup());
                MyInstance.Subscribe();
            }

            private void SetupDecayEntity(DecayEntity e)
            {
                buildingIDs.Add(e.buildingID);
            }

            /*private void SetupHealth(BaseCombatEntity e)
            {
                if (e is BuildingBlock)
                {
                    BuildingBlockOptions bbo;
                    if (config.Abandoned.Blocks.TryGetValue(e.ShortPrefabName, out bbo))
                    {
                        var block = e as BuildingBlock;
                        if (block.IsKilled()) return;
                        block.currentGrade.construction.healthMultiplier = 1f;
                        block.SetHealthToMax();
                        System.Reflection.MethodInfo MemberwiseCloneMethod = typeof(object).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        Construction construction = (Construction)MemberwiseCloneMethod.Invoke(block.blockDefinition, null);
                        ConstructionGrade[] grades = (ConstructionGrade[])MemberwiseCloneMethod.Invoke(block.blockDefinition.grades, null);
                        block.blockDefinition = construction;
                        block.blockDefinition.grades = grades;
                        switch (block.grade)
                        {
                            case BuildingGrade.Enum.Twigs: block.currentGrade.gradeBase.baseHealth = bbo.Twigs; break;
                            case BuildingGrade.Enum.Wood: block.currentGrade.gradeBase.baseHealth = bbo.Wooden; break;
                            case BuildingGrade.Enum.Stone: block.currentGrade.gradeBase.baseHealth = bbo.Stone; break;
                            case BuildingGrade.Enum.Metal: block.currentGrade.gradeBase.baseHealth = bbo.Metal; break;
                            case BuildingGrade.Enum.TopTier: block.currentGrade.gradeBase.baseHealth = bbo.HQM; break;
                        }
                        float multiplier;
                        if (config.Abandoned.BlocksMultiplier.TryGetValue(block.grade, out multiplier))
                        {
                            block.currentGrade.construction = (Construction)MemberwiseCloneMethod.Invoke(block.currentGrade.construction, null);
                            block.currentGrade.construction.healthMultiplier = multiplier;
                        }
                        block.SetHealthToMax();
                    }
                    else
                    {
                        e._health = e.startHealth;
                    }
                    return;
                }
                float value;
                if (config.Abandoned.Other.TryGetValue(e.ShortPrefabName, out value))

                {
                    e.startHealth = value;
                    e.InitializeHealth(value, value);
                }
            }*/

            public class TurretInfo
            {
                public Item weapon;
                public AutoTurret turret;
                public List<Item> items = new List<Item>();
            }

            private List<TurretInfo> turrets = new List<TurretInfo>();

            private void SetupTurret(AutoTurret turret)
            {
                if (!config.Abandoned.AutoTurret.Enabled)
                {
                    return;
                }

                SetupIO(turret);
                if (payment == null && config.RemoveOwnership) turret.authorizedPlayers.Clear();
                turret.InitializeHealth(config.Abandoned.AutoTurret.Health, config.Abandoned.AutoTurret.Health);
                turret.sightRange = config.Abandoned.AutoTurret.SightRange;
                turret.aimCone = config.Abandoned.AutoTurret.AimCone;

                if (config.Abandoned.AutoTurret.RemoveWeapon)
                {
                    turret.AttachedWeapon = null;
                    Item slot = turret.inventory.GetSlot(0);

                    if (slot != null && (slot.info.category == ItemCategory.Weapon || slot.info.category == ItemCategory.Fun))
                    {
                        slot.RemoveFromContainer();
                        slot.Remove();
                    }
                }

                TurretInfo ti = new TurretInfo();

                ti.turret = turret;

                if (config.Abandoned.AutoTurret.Shortnames.Count > 0)
                {
                    ActionIn(0.1f, () =>
                    {
                        if (!turret.IsKilled() && turret.AttachedWeapon == null)
                        {
                            var shortname = config.Abandoned.AutoTurret.Shortnames.GetRandom();
                            var itemToCreate = ItemManager.FindItemDefinition(shortname);

                            if (itemToCreate != null)
                            {
                                Item item = ItemManager.Create(itemToCreate, 1, (ulong)itemToCreate.skins.GetRandom().id);

                                if (!item.MoveToContainer(turret.inventory, 0, false))
                                {
                                    item.Remove();
                                }
                                else
                                {
                                    item.SwitchOnOff(true);
                                    ti.weapon = item;
                                }
                            }
                        }
                    });
                }

                ActionIn(1f, turret.UpdateAttachedWeapon);

                ActionIn(2.5f, () => FillAmmoTurret(ti));

                if (config.Abandoned.AutoTurret.Hostile)
                {
                    turret.SetPeacekeepermode(false);

                }

                if (!config.Abandoned.AutoTurret.RequiresPower && !turret.IsOnline())
                {
                    ActionIn(3f, turret.InitiateStartup);
                }

                if (config.Abandoned.AutoTurret.InfiniteAmmo)
                {
                    turret.inventory.onPreItemRemove += new Action<Item>(OnWeaponItemPreRemove);
                }

                turrets.Add(ti);
            }

            private void PowerDownAutoTurrets()
            {
                foreach (var ti in turrets)
                {
                    if (ti == null || ti.turret.IsKilled())
                    {
                        continue;
                    }
                    if (GetConnectedInput(ti.turret) == null)
                    {
                        ti.turret.InitiateShutdown();
                    }
                    foreach (Item item in ti.items.ToArray())
                    {
                        if (item != null && item.parent != null && item.parent == ti.turret.inventory)
                        {
                            item.RemoveFromContainer();
                            item.Remove(0f);
                        }
                    }
                    if (ti.weapon != null && ti.weapon.parent != null && ti.weapon.parent == ti.turret.inventory)
                    {
                        ti.weapon.GetHeldEntity().SafelyKill();
                        ti.weapon.RemoveFromContainer();
                        ti.weapon.Remove();
                    }
                }
            }

            private IOEntity GetConnectedInput(IOEntity io)
            {
                if (io == null || io.inputs == null)
                {
                    return null;
                }

                foreach (var input in io.inputs)
                {
                    var e = input?.connectedTo?.Get(true);

                    if (e.IsValid())
                    {
                        return e;
                    }
                }

                return null;
            }

            private void ActionIn(float time, Action action)
            {
                timer.Once(time, () => { try { action.Invoke(); } catch { } });
            }

            private void OnWeaponItemPreRemove(Item item)
            {
                var weapon = item.parent?.entityOwner;

                if (weapon is AutoTurret)
                {
                    var turret = weapon as AutoTurret;
                    var ti = turrets.FirstOrDefault(x => x.turret == turret);

                    ActionIn(0.1f, () => FillAmmoTurret(ti));
                }
            }

            private void FillAmmoTurret(TurretInfo ti)
            {
                if (ti == null || ti.turret.IsKilled())
                {
                    return;
                }

                var attachedWeapon = ti.turret.GetAttachedWeapon();

                if (attachedWeapon == null)
                {
                    ti.turret.Invoke(() => FillAmmoTurret(ti), 0.2f);
                    return;
                }

                int p = Math.Max(config.Abandoned.AutoTurret.Ammo, attachedWeapon.primaryMagazine.capacity);
                Item ammo = ItemManager.Create(attachedWeapon.primaryMagazine.ammoType, p, 0uL);
                if (!ammo.MoveToContainer(ti.turret.inventory, -1, true, true, null, true)) ammo.Remove();
                attachedWeapon.primaryMagazine.contents = attachedWeapon.primaryMagazine.capacity;
                attachedWeapon.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
                ti.turret.Invoke(() =>
                {
                    if (ti != null && !ti.turret.IsKilled())
                    {
                        ti.items = new List<Item>();
                        ti.turret.UpdateTotalAmmo();
                        ti.items.AddRange(ti.turret.inventory.itemList.Where(x => x != null && x.position != 0));
                    }
                }, 0.25f);
            }

            private void SetupIO(ContainerIOEntity io)
            {
                io.dropsLoot = false;
                io.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);

                if (config.Abandoned.AutoTurret.HasPower)
                {
                    io.SetFlag(BaseEntity.Flags.Reserved8, true, false, true);
                }
            }

            private void SetupCollider()
            {
                try
                {
                    _collider = gameObject.GetComponent<SphereCollider>() ?? gameObject.AddComponent<SphereCollider>();
                    _collider.radius = radius;
                    _collider.isTrigger = true;
                    _collider.center = Vector3.zero;
                    gameObject.layer = (int)Layer.Trigger;
                    go.transform.position = center;
                }
                catch
                {
                    Invoke(SetupCollider, 0.1f);
                }
            }

            public void Refresh()
            {
                if (config.Abandoned.DespawnTime > 0f)
                {
                    CancelInvoke(DestroyAll);
                    Invoke(DestroyAll, config.Abandoned.DespawnTime);
                    DespawnDateTime = DateTime.Now.AddSeconds(config.Abandoned.DespawnTime);
                }
                CreateMarkers();
            }

            public void RemoveExpiration()
            {
                if (activity != null && --activity.total <= 0)
                {
                    MyInstance.data.Activity.Remove(activity);
                }
                IsExpired = false;
            }

            public void UpdateActivity(List<ulong> owners)
            {
                if (owners == null || config.Abandoned.DoNotDestroyManual && payment != null)
                {
                    IsExpired = false;
                    return;
                }

                if (!config.Abandoned.DoNotDestroy)
                {
                    activity = ActivityInfo.Find(owners, MyInstance.data.Activity);

                    activity.total++;

                    if (!MyInstance.data.Activity.Contains(activity))
                    {
                        MyInstance.data.Activity.Add(activity);
                    }

                    var limit = activity.GetLimit();

                    if (IsExpired = limit > 0 && activity.total >= limit)
                    {
                        MyInstance.data.Activity.Remove(activity);
                    }
                }
            }

            private void SpawnNpcs()
            {
                var profiles = config.Abandoned.BotSpawnProfileNames.ToList();

                profiles.Remove("profile_name_1");
                profiles.Remove("profile_name_2");

                if (profiles.Count == 0)
                {
                    return;
                }

                string group = Guid.NewGuid().ToString();

                Plugin BotReSpawn = Interface.Oxide.RootPluginManager.GetPlugin("BotReSpawn");

                if (BotReSpawn == null)
                {
                    return;
                }

                groups.Add(group);
                BotReSpawn?.Call("AddGroupSpawn", center, profiles.GetRandom(), group, 0);
            }

            private void CreateGenericMarker(bool shouldParent)
            {
                genericMarker = GameManager.server.CreateEntity(StringPool.Get(2849728229), center) as MapMarkerGenericRadius;
                genericMarker.alpha = 0.75f;
                genericMarker.color1 = Color.magenta;
                genericMarker.color2 = Color.magenta;
                genericMarker.radius = Mathf.Min(2.5f, World.Size <= 3600 ? config.Abandoned.MarkerSubRadius : config.Abandoned.MarkerRadius);
                genericMarker.Spawn();
                if (shouldParent) genericMarker.SetParent(anchor, true, true);
                //MapMarker.serverMapMarkers.Remove(genericMarker);
                genericMarker.SendUpdate();
            }

            private void CreateVendingMarker(bool shouldParent)
            {
                vendingMarker = GameManager.server.CreateEntity(StringPool.Get(3459945130), center) as VendingMachineMapMarker;
                vendingMarker.enabled = false;
                vendingMarker.Spawn();
                if (shouldParent) vendingMarker.SetParent(anchor, true, true);
            }

            private void CreateMarkers()
            {
                if (!markerCreated)
                {
                    bool shouldParent = !anchor.IsKilled();

                    CreateGenericMarker(shouldParent);
                    CreateVendingMarker(shouldParent);
                    CreateSpheres(shouldParent);
                    UpdateMarkers();
                    markerCreated = true;
                }
            }

            private void CreateSpheres(bool shouldParent)
            {
                if (config.Abandoned.SphereAmount > 0)
                {
                    for (int i = 0; i < config.Abandoned.SphereAmount; i++)
                    {
                        var sphere = GameManager.server.CreateEntity(StringPool.Get(3211242734), center) as SphereEntity;
                        sphere.currentRadius = 1f;
                        sphere.Spawn();
                        if (shouldParent) sphere.SetParent(anchor, true, true);
                        sphere.LerpRadiusTo(radius * 2f, radius * 0.75f);
                        spheres.Add(sphere);
                    }
                }
            }

            public void UpdateMarkers()
            {
                if (isDestroyed)
                {
                    return;
                }

                if (genericMarker != null)
                {
                    genericMarker.SendUpdate();
                }

                if (vendingMarker != null)
                {
                    if (DespawnDateTime == DateTime.MinValue)
                    {
                        if (config.Abandoned.ShowOwnersName && !string.IsNullOrEmpty(currentName))
                        {
                            vendingMarker.markerShopName = GetMarkerName(config.Abandoned.MarkerNameSeconds.Replace(" [{time}m]", string.Empty));
                        }
                        else vendingMarker.markerShopName = config.Abandoned.MarkerNameSeconds.Replace(" [{time}m]", string.Empty);
                        vendingMarker.SendNetworkUpdate();
                        Invoke(UpdateMarkers, 10f);
                        return;
                    }

                    var ts = DespawnDateTime.Subtract(DateTime.Now);

                    if (ts.TotalMinutes >= 1)
                    {
                        string despawnText = Math.Ceiling(ts.TotalMinutes).ToString();
                        if (config.Abandoned.ShowOwnersName && !string.IsNullOrEmpty(currentName))
                        {
                            vendingMarker.markerShopName = GetMarkerName(config.Abandoned.MarkerName.Replace("{time}", despawnText));
                        }
                        else vendingMarker.markerShopName = config.Abandoned.MarkerName.Replace("{time}", despawnText);
                        Invoke(UpdateMarkers, 10f);
                    }
                    else
                    {
                        string despawnText = Math.Ceiling(ts.TotalSeconds).ToString();
                        if (config.Abandoned.ShowOwnersName && !string.IsNullOrEmpty(currentName))
                        {
                            vendingMarker.markerShopName = GetMarkerName(config.Abandoned.MarkerNameSeconds.Replace("{time}", despawnText));
                        }
                        else vendingMarker.markerShopName = config.Abandoned.MarkerNameSeconds.Replace("{time}", despawnText);
                        Invoke(UpdateMarkers, 1f);
                    }

                    vendingMarker.transform.position = center;
                    vendingMarker.SendNetworkUpdate();
                }
            }

            private string GetMarkerName(string text)
            {
                try
                {
                    if (currentId != previousId)
                    {
                        if (!config.Abandoned.MarkerNameRaiderFormat.Contains("{1}"))
                        {
                            return currentName;
                        }
                        return string.Format(config.Abandoned.MarkerNameRaiderFormat, currentName, text);
                    }
                    if (!config.Abandoned.MarkerNameOwnerFormat.Contains("{1}"))
                    {
                        return currentName;
                    }
                    return string.Format(config.Abandoned.MarkerNameOwnerFormat, currentName, text);
                }
                catch
                {
                    return string.Format("{0} {1}", currentName, text);
                }
            }

            private void DestroyAll()
            {
                KillMarkers();
                KillSpheres();

                if (IsExpired && !IsClaimed)
                {
                    UndoLoop(entities, hookObjects);
                }

                DestroyMe();
            }

            public static void UndoLoop(ListHashSet<DecayEntity> decayEntities, object[] hookObjects = null, int count = 0)
            {
                var entities = new List<BaseEntity>();

                foreach (DecayEntity decayEntity in decayEntities)
                {
                    entities.Add(decayEntity);
                }

                UndoLoop(entities, hookObjects, count);
            }

            public static void UndoLoop(List<BaseEntity> entities, object[] hookObjects, int count = 0)
            {
                entities.RemoveAll(e => e.IsKilled() || e.HasParent());

                entities.Sort((x, y) => (x is BuildingBlock).CompareTo(y is BuildingBlock));

                entities.Take(10).ToList().ForEach(entity =>
                {
                    entities.Remove(entity);

                    if (entity is IOEntity)
                    {
                        var io = entity as IOEntity;

                        io.ClearConnections();

                        if (entity is SamSite)
                        {
                            (io as SamSite).staticRespawn = false;
                        }
                    }

                    if (entity is IItemContainerEntity)
                    {
                        (entity as IItemContainerEntity)?.inventory?.Clear();
                    }

                    entity.SafelyKill();
                });

                if (count != 0 && entities.Count != 0 && entities.Count == count)
                {
                    goto done;
                }

                if (entities.Count > 0)
                {
                    Interface.Oxide.NextTick(() => UndoLoop(entities, hookObjects, entities.Count));
                    return;
                }

            done:
                if (hookObjects != null)
                {
                    Interface.CallHook("OnAbandonedBaseDespawned", hookObjects);
                }
            }

            internal object[] hookObjects => new object[] { center, AllowPVP, intruders, entities };

            private bool InAnchorRange(BasePlayer player)
            {
                return !anchor.IsKilled() && AbandonedBases.InRange(player.transform.position, anchor.transform.position, anchor.bounds.extents.Max() * 1.15f);
            }

            public void OnPlayerExit(BasePlayer player, bool skipDelay)
            {
                if (!player.IsHuman())
                {
                    return;
                }

                if (!skipDelay && InAnchorRange(player))
                {
                    if (AllowPVP && config.Abandoned.Tugboats.Delay)
                    {
                        DelaySettings.Add(this, player);
                    }
                    return;
                }

                if (intruders.Remove(player))
                {
                    Interface.CallHook("OnPlayerExitAbandonedBase", player, center, AllowPVP, intruders, entities);
                }

                if (skipDelay || !AllowPVP)
                {
                    return;
                }

                DelaySettings.Add(this, player);
            }

            public static List<BasePlayer> GetMountedPlayers(BaseMountable m)
            {
                BaseVehicle vehicle = m.HasParent() ? m.VehicleParent() : m as BaseVehicle;

                if (!vehicle.IsRealNull())
                {
                    return GetMountedPlayers(vehicle);
                }

                var players = new List<BasePlayer>();
                var player = m.GetMounted();

                if (player.IsHuman())
                {
                    players.Add(player);
                }

                return players;
            }

            private static List<BasePlayer> GetMountedPlayers(BaseVehicle vehicle)
            {
                var players = new HashSet<BasePlayer>();

                if (!vehicle.HasMountPoints())
                {
                    var player = vehicle.GetMounted();

                    if (player.IsHuman())
                    {
                        players.Add(player);
                    }

                    return players.ToList();
                }

                foreach (var mountPoint in vehicle.allMountPoints)
                {
                    var player = mountPoint.mountable?.GetMounted();

                    if (player.IsHuman())
                    {
                        players.Add(player);
                    }
                }

                return players.ToList();
            }

            public static bool IsBox(BaseEntity entity)
            {
                return entity.ShortPrefabName == "box.wooden.large" || entity.ShortPrefabName == "woodbox_deployed" || entity.ShortPrefabName == "coffinstorage";
            }

            public bool HasCancelCooldown(IPlayer user)
            {
                if (cancelCooldownTime > Time.time)
                {
                    user.Reply(_("CancelCooldown", user.Id, Math.Ceiling(cancelCooldownTime - Time.time)));
                    return true;
                }

                return false;
            }

            public bool IsEventCompleted;

            public void CheckEventCompletion()
            {
                if (IsEventCompleted || !IsCompleted())
                {
                    return;
                }
                cancelCooldownTime = Time.time;
                IsEventCompleted = true;
                if (config.Messages.LocalCompletion > 0)
                {
                    var center = this.center;
                    foreach (var target in BasePlayer.activePlayerList)
                    {
                        if (target.IsKilled() || config.Messages.LocalCompletion < World.Size && !AbandonedBases.InRange(target.transform.position, center, config.Messages.LocalCompletion))
                        {
                            continue;
                        }
                        if (config.Messages.RaidCompletion && intruders.Contains(target) && target.HasPermission("abandonedbases.convert"))
                        {
                            continue;
                        }
                        if (currentName == previousName)
                        {
                            Message(target, "OnEventCompletedLocal", currentName, GetGrid());
                        }
                        else
                        {
                            Message(target, "OnEventCompletedLocalOwned", currentName, previousName, GetGrid());
                        }
                    }
                }
                if (config.Messages.RaidCompletion)
                {
                    foreach (var target in intruders.Where(x => !x.IsKilled() && x.HasPermission("abandonedbases.convert")))
                    {
                        if (target.HasPermission("abandonedbases.convert.cancel") && target.HasPermission("abandonedbases.convert.claim"))
                        {
                            Message(target, "OnEventCompletedClaimCancel");
                        }
                        else if (target.HasPermission("abandonedbases.convert.cancel"))
                        {
                            Message(target, "OnEventCompletedCancel");
                        }
                        else if (target.HasPermission("abandonedbases.convert.claim"))
                        {
                            Message(target, "OnEventCompletedClaim");
                        }
                        else Message(target, "OnEventCompleted");
                    }
                }
            }

            public bool IsCupboardTaken => privs.Count > 0 && privs.All(priv => priv.IsKilled() || priv.inventory.IsEmpty());

            public bool IsCompleted()
            {
                if (config.Abandoned.OnlyCupboardsAreRequired && IsCupboardTaken)
                {
                    return true;
                }

                foreach (var container in containers)
                {
                    if (!container.IsKilled() && !container.inventory.IsEmpty())
                    {
                        return false;
                    }
                }

                return true;
            }

            public static AbandonedBuilding Get(DroppedItemContainer backpack)
            {
                if (backpack.IsKilled())
                {
                    return null;
                }

                DelaySettings ds;
                if (DelaySettings.PvpDelay.TryGetValue(backpack.playerSteamID, out ds))
                {
                    return ds.Building;
                }

                return Get(backpack.transform.position);
            }

            public static AbandonedBuilding Get(BasePlayer victim, HitInfo hitInfo)
            {
                DelaySettings ds;
                if (DelaySettings.PvpDelay.TryGetValue(victim.userID, out ds))
                {
                    return CompareTo(ds.Building, Get(hitInfo));
                }

                return CompareTo(Get(victim.transform.position), Get(hitInfo));
            }

            public static AbandonedBuilding CompareTo(AbandonedBuilding v, AbandonedBuilding a)
            {
                if (v == null) return a;
                if (a == null) return v;
                if (v == a) return v;
                return null;
            }

            private static AbandonedBuilding Get(HitInfo hitInfo)
            {
                if (hitInfo?.Initiator?.IsDestroyed == false)
                {
                    var abandonedBuilding = Get(hitInfo.Initiator.ServerPosition);
                    if (abandonedBuilding != null) return abandonedBuilding;
                }

                if (hitInfo?.WeaponPrefab?.IsDestroyed == false)
                {
                    var abandonedBuilding = Get(hitInfo.WeaponPrefab.ServerPosition);
                    if (abandonedBuilding != null) return abandonedBuilding;
                }

                if (hitInfo?.Weapon?.IsDestroyed == false)
                {
                    return Get(hitInfo.Weapon.ServerPosition);
                }

                return null;
            }

            public static AbandonedBuilding Get(Vector3 target)
            {
                foreach (var abandonedBuilding in Instance.AbandonedBuildings)
                {
                    if (abandonedBuilding.InRange(target))
                    {
                        return abandonedBuilding;
                    }
                }

                return null;
            }

            public object OnLootEntityInternal(BasePlayer player, BaseEntity entity)
            {
                if (config.Abandoned.BlacklistedPickupItems.Exists(value => !string.IsNullOrEmpty(value) && entity.ShortPrefabName.Contains(value, CompareOptions.OrdinalIgnoreCase)))
                {
                    return false;
                }
                if (!canReassign && LockBaseToFirstAttacker && !CanDoActions(player))
                {
                    TryEjectFromLockedBase(player);
                    return false;
                }
                if (config.Abandoned.PreventHogging)
                {
                    bool reply = !Messages.Contains(player.userID);
                    if (MyInstance.IsHogging(player, this, reply, "HoggingFinishYourRaid"))
                    {
                        if (reply)
                        {
                            ulong userid = player.userID;
                            Messages.Add(userid);
                            timer.Once(10f, () => Messages.Remove(userid));
                        }
                        return true;
                    }
                }
                return null;
            }
        }

        #endregion

        #region Hooks

        private void OnNewSave(string filename)
        {
            newSave = true;
        }

        private void Init()
        {
            Instance = this;
            TryInvokeMethod(Unsubscribe);
            TryInvokeMethod(LoadData);
        }

        private void OnServerInitialized(bool isStartup)
        {
            TryInvokeMethod(CheckData);
            TryInvokeMethod(RegisterPermissions);
            TryInvokeMethod(RegisterCommands);
            TryInvokeMethod(() => config.Validate(this));
            TryInvokeMethod(StartNotificationTimer);
            TryInvokeMethod(ExcludeZones);
            SetupAbandonedRoutine();
            UpdateLastSeen();
            timer.Every(60f, UpdateLastSeen);
        }

        private object OnEngineStart(Tugboat tugboat, BasePlayer player)
        {
            if (tugboat.IsValid() && AbandonedReferences.ContainsKey(tugboat.net.ID.Value))
            {
                Message(player, "Engine failure");
                return true;
            }
            return null;
        }

        private void OnLootEntityEnd(BasePlayer player, StorageContainer container)
        {
            if (container.IsKilled() || container.OwnerID.IsSteamId())
            {
                return;
            }

            var abandonedBuilding = AbandonedBuilding.Get(container.transform.position);

            if (abandonedBuilding == null)
            {
                return;
            }

            abandonedBuilding.CheckEventCompletion();
        }

        private object CanLootEntity(BasePlayer player, BaseEntity entity) // BaseRidableAnimal, ContainerIOEntity, DroppedItemContainer, IndustrialCrafter, LootableCorpse, ResourceContainer, StorageContainer
        {
            if (entity.IsKilled()) return null;
            var abandonedBuilding = AbandonedBuilding.Get(entity.transform.position);
            if (abandonedBuilding == null || entity.OwnerID.IsSteamId() && !abandonedBuilding.owners.Contains(entity.OwnerID)) return null;
            return abandonedBuilding.OnLootEntityInternal(player, entity) == null ? (object)null : true;
        }

        private object CanPickupEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity.IsKilled()) return null;
            var abandonedBuilding = AbandonedBuilding.Get(entity.transform.position);
            if (abandonedBuilding == null || entity.OwnerID.IsSteamId() && !abandonedBuilding.owners.Contains(entity.OwnerID)) return null;
            //if (!abandonedBuilding.NearFoundation(player.transform.position)) return null;
            return abandonedBuilding.OnLootEntityInternal(player, entity);
        }

        private bool IsServerShuttingDown;

        private void OnServerShutdown()
        {
            IsServerShuttingDown = true;
        }

        private void TryDisableEventCooldowns() // allow plugin to be reloaded without penalizing everyone, but prevent exploiting this by enforcing a cooldown on a server restart
        {
            if (!IsServerShuttingDown)
            {
                foreach (var abandonedBuilding in AbandonedBuildings.ToList())
                {
                    abandonedBuilding.DisableEventCooldown = true;
                }
            }
        }

        private void Unload()
        {
            TryInvokeMethod(TryDisableEventCooldowns);
            TryInvokeMethod(StopAbandonedCoroutine);
            TryInvokeMethod(DestroyAll);
            TryInvokeMethod(SaveData);
            data = null;
            config = null;
            Instance = null;
            AbandonedBasesExtensionMethods.ExtensionMethods.permission = null;
            DelaySettings.PvpDelay.Clear();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.IsConnected)
            {
                UpdateLastSeen(player, Epoch.Current);
            }
        }

        private void OnPlayerSleep(BasePlayer player)
        {
            if (player.IsConnected)
            {
                UpdateLastSeen(player, Epoch.Current);
            }
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            DelaySettings.Remove(player);
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo hitInfo)
        {
            if (!player.IsHuman())
            {
                return;
            }

            if (player.IsConnected)
            {
                UpdateLastSeen(player, Epoch.Current);
            }

            var abandonedBuilding = AbandonedBuilding.Get(player, hitInfo);

            if (abandonedBuilding == null)
            {
                return;
            }

            abandonedBuilding.OnPlayerExit(player, true);

            if (CanDropPlayerBackpack(player, abandonedBuilding))
            {
                Backpacks?.Call("API_DropBackpack", player);
            }
        }

        private bool CanDropPlayerBackpack(BasePlayer player, AbandonedBuilding abandonedBuilding)
        {
            DelaySettings ds;
            if (DelaySettings.PvpDelay.TryGetValue(player.userID, out ds) && (ds.Building.AllowPVP && config.Abandoned.BackpacksPVP || !ds.Building.AllowPVP && config.Abandoned.BackpacksPVE))
            {
                return true;
            }

            return abandonedBuilding != null && (abandonedBuilding.AllowPVP && config.Abandoned.BackpacksPVP || !abandonedBuilding.AllowPVP && config.Abandoned.BackpacksPVE);
        }

        private void OnEntitySpawned(DroppedItemContainer backpack)
        {
            if (backpack == null || backpack.ShortPrefabName != "item_drop_backpack")
            {
                return;
            }

            bool isFromCorpse = backpack.playerSteamID == 0uL;

            NextTick(() =>
            {
                var abandonedBuilding = AbandonedBuilding.Get(backpack);

                if (abandonedBuilding == null)
                {
                    return;
                }

                if (!abandonedBuilding.AllowPVP && !abandonedBuilding.owners.Contains(backpack.playerSteamID))
                {
                    return;
                }

                if (config.Abandoned.CorpsesLooted && isFromCorpse || !isFromCorpse && config.Abandoned.BackpacksPVE && !abandonedBuilding.AllowPVP || !isFromCorpse && config.Abandoned.BackpacksPVP && abandonedBuilding.AllowPVP)
                {
                    backpack.playerSteamID = 0;
                }
            });
        }

        private void OnEntitySpawned(PlayerCorpse corpse)
        {
            if (!config.Abandoned.CorpsesLooted)
            {
                return;
            }

            NextTick(() =>
            {
                if (corpse.IsKilled())
                {
                    return;
                }

                if (AbandonedSleepers.Contains(corpse.playerSteamID) || DelaySettings.PvpDelay.ContainsKey(corpse.playerSteamID) || EventTerritory(corpse.transform.position))
                {
                    AbandonedSleepers.Remove(corpse.playerSteamID);

                    corpse.playerSteamID = 0;
                }
            });
        }

        private void OnEntitySpawned(MLRSRocket rocket)
        {
            if (rocket == null) return;
            var systems = FindEntitiesOfType<MLRS>(rocket.transform.position, 15f);
            if (systems.Count == 0 || !EventTerritory(systems[0].TrueHitPos)) return;
            var owner = systems[0].rocketOwnerRef.Get(true) as BasePlayer;
            if (owner == null) return;
            rocket.creatorEntity = config.Abandoned.MLRS ? owner : null;
            rocket.OwnerID = config.Abandoned.MLRS ? owner.userID : 0uL;
        }

        private void OnEntitySpawned(BaseEntity entity)
        {
            if (!entity.IsValid() || entity.IsDestroyed || !entity.OwnerID.IsSteamId())
            {
                return;
            }

            var abandonedBuilding = AbandonedBuilding.Get(entity.transform.position);

            if (abandonedBuilding == null || !abandonedBuilding.owners.Contains(entity.OwnerID))
            {
                return;
            }

            if (abandonedBuilding.AllowPVP || abandonedBuilding.IsAttached(entity))
            {
                abandonedBuilding.RememberOwner(entity);
            }
        }

        private object OnLifeSupportSavingLife(BasePlayer player)
        {
            if (EventTerritory(player.transform.position) || HasPVPDelay(player.userID))
            {
                return true;
            }

            return null;
        }

        private object OnPreventLooting(BasePlayer player, BaseEntity entity)
        {
            if (AbandonedReferences.ContainsKey(entity.net.ID.Value))
            {
                return true;
            }

            return null;
        }

        private object OnRestoreUponDeath(BasePlayer player)
        {
            if (config.Abandoned.BlockRestoreSleepers && AbandonedSleepers.Contains(player.userID))
            {
                return true;
            }

            var abandonedBuilding = AbandonedBuilding.Get(player.transform.position);

            if (abandonedBuilding == null)
            {
                return null;
            }

            return config.Abandoned.BlockRestorePVE && !abandonedBuilding.AllowPVP || config.Abandoned.BlockRestorePVP && abandonedBuilding.AllowPVP ? true : (object)null;
        }

        private object CanTeleport(BasePlayer player, Vector3 to)
        {
            if (player.HasPermission("abandonedbases.teleport"))
            {
                return null;
            }

            return DelaySettings.PvpDelay.ContainsKey(player.userID) || EventTerritory(player.transform.position) || EventTerritory(to) ? _("CannotTeleport", player.UserIDString) : null;
        }

        private object CanOpenBackpack(BasePlayer looter, ulong backpackOwnerID)
        {
            if (DelaySettings.PvpDelay.ContainsKey(looter.userID) || EventTerritory(looter.transform.position))
            {
                return _("NotAllowed", looter.UserIDString);
            }

            return null;
        }

        private object CanEntityBeTargeted(BaseEntity target, BaseEntity entity)
        {
            if (!entity.IsValid() || !AbandonedReferences.ContainsKey(entity.net.ID.Value)) return null;
            return entity is AutoTurret ? config.Abandoned.AutoTurret.Enabled : true;
        }

        private object CanEntityTrapTrigger(BaseTrap trap, BasePlayer player)
        {
            return trap.IsValid() && AbandonedReferences.ContainsKey(trap.net.ID.Value) ? true : (object)null;
        }

        private List<DamageType> _damageTypes = new List<DamageType> { DamageType.ElectricShock, DamageType.Decay };

        private object CanEntityTakeDamage(BaseEntity entity, HitInfo hitInfo)
        {
            if (hitInfo == null || _damageTypes.Contains(hitInfo.damageTypes.GetMajorityDamageType()) || entity.IsKilled() || entity.net == null || entity.OwnerID == 1337422 || IsInZone(entity.transform.position) || IsIgnoredPrefab(entity.ShortPrefabName))
            {
                return null;
            }

            if (entity is BasePlayer)
            {
                return ProcessVictim(entity as BasePlayer, hitInfo);
            }

            if (hitInfo.Initiator.IsValid() && AbandonedReferences.ContainsKey(hitInfo.Initiator.net.ID.Value))
            {
                return IsBuildingDamageAllowed(entity, hitInfo);
            }

            AbandonedBuilding abandonedBuilding;
            if (AbandonedReferences.TryGetValue(entity.net.ID.Value, out abandonedBuilding))
            {
                return ProcessAbandonedBuilding(entity, hitInfo, abandonedBuilding);
            }

            if (entity.OwnerID.IsSteamId() || entity is Tugboat)
            {
                return ProcessPlayerEntity(entity, hitInfo);
            }

            return null;
        }

        private object CanLootPlayer(BasePlayer target, BasePlayer looter)
        {
            if (target == null || looter == null || !target.IsWounded() || !EventTerritory(target.transform.position))
            {
                return null;
            }

            return IsAlly(target.userID, looter.userID) ? (object)null : false;
        }

        private void OnEntityDeath(BaseEntity entity, HitInfo hitInfo)
        {
            if (!entity.IsValid())
            {
                return;
            }
            AbandonedBuilding abandonedBuilding;
            if (!AbandonedReferences.Remove(entity.net.ID.Value, out abandonedBuilding))
            {
                return;
            }
            if (entity.OwnerID.IsSteamId())
            {
                Interface.CallHook("RemovePlayerEntity", entity.OwnerID, entity.ShortPrefabName);
                return;
            }
            if (abandonedBuilding.EntityOwners.ContainsKey(entity.net.ID.Value))
            {
                Interface.CallHook("RemovePlayerEntity", abandonedBuilding.EntityOwners[entity.net.ID.Value], entity.ShortPrefabName);
            }
            if (entity is BuildingPrivlidge)
            {
                abandonedBuilding.CheckEventCompletion();
            }
            if (hitInfo != null && hitInfo.Initiator is BasePlayer)
            {
                abandonedBuilding.TrySetOwnerLock(hitInfo.Initiator as BasePlayer);
            }
        }

        private void OnEntityKill(BaseEntity entity) => OnEntityDeath(entity, null);

        private void OnEntityBuilt(Planner planner, GameObject go)
        {
            var e = go.ToBaseEntity();

            if (e == null)
            {
                return;
            }

            var abandonedBuilding = AbandonedBuilding.Get(e.transform.position);

            if (abandonedBuilding == null || !abandonedBuilding.owners.Contains(e.OwnerID))
            {
                return;
            }

            if (e is BaseLadder)
            {
                if (!config.Abandoned.AllowLadders)
                {
                    Message(planner.GetOwnerPlayer(), "CannotBuildLadders");
                    e.Invoke(e.SafelyKill, 0.1f);
                }
                else
                {
                    AbandonedReferences[e.net.ID.Value] = abandonedBuilding;
                    abandonedBuilding.entities.Add(e);
                }
                return;
            }

            if (!config.Abandoned.AllowBuilding)
            {
                Message(planner.GetOwnerPlayer(), "CannotBuild");
                e.Invoke(e.SafelyKill, 0.1f);
                return;
            }

            AbandonedReferences[e.net.ID.Value] = abandonedBuilding;
            abandonedBuilding.entities.Add(e);

            if (e is BuildingPrivlidge)
            {
                var player = planner.GetOwnerPlayer();

                if (!player.HasPermission("abandonedbases.convert") || !player.HasPermission("abandonedbases.convert.claim"))
                {
                    Message(player, "OnBuiltPrivilegeNone");
                    return;
                }

                if (config.Abandoned.RequireEventFinished)
                {
                    Message(player, "OnBuiltPrivilegeEx");
                }
                else Message(player, "OnBuiltPrivilege");
            }
        }

        private object OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (player == null || !EventTerritory(player.transform.position))
            {
                return null;
            }

            foreach (var value in config.Abandoned.BlacklistedCommands)
            {
                if (command.Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    Message(player, "CommandNotAllowed");
                    return true;
                }
            }

            return null;
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            if (player == null || !EventTerritory(player.transform.position))
            {
                return null;
            }

            foreach (var value in config.Abandoned.BlacklistedCommands)
            {
                if (arg.cmd.FullName.EndsWith(value, StringComparison.OrdinalIgnoreCase))
                {
                    Message(player, "CommandNotAllowed");
                    return true;
                }
            }

            return null;
        }

        #endregion Hooks

        #region Helpers

        public void GiveRewards(BasePlayer player, int total)
        {
            if (config.Abandoned.Rewards.Money > 0)
            {
                double money = config.Abandoned.Rewards.DivideRewards ? config.Abandoned.Rewards.Money / total : config.Abandoned.Rewards.Money;
                if (Convert.ToBoolean(Economics?.Call("Deposit", player.UserIDString, money)))
                {
                    Message(player, "EconomicsDeposit", money);
                }
            }

            if (config.Abandoned.Rewards.Money > 0 && IQEconomic.CanCall())
            {
                int money = Convert.ToInt32(config.Abandoned.Rewards.DivideRewards ? config.Abandoned.Rewards.Money / total : config.Abandoned.Rewards.Money);
                IQEconomic?.Call("API_SET_BALANCE", player.UserIDString, money);
                Message(player, "EconomicsDeposit", money);
            }

            if (config.Abandoned.Rewards.Points > 0)
            {
                int points = config.Abandoned.Rewards.DivideRewards ? config.Abandoned.Rewards.Points / total : config.Abandoned.Rewards.Points;
                if (Convert.ToBoolean(ServerRewards?.Call("AddPoints", player.UserIDString, points)))
                {
                    Message(player, "ServerRewardPoints", points);
                }
            }

            if (config.Abandoned.Rewards.XP > 0 && SkillTree.CanCall())
            {
                double xp = config.Abandoned.Rewards.DivideRewards ? config.Abandoned.Rewards.XP / total : config.Abandoned.Rewards.XP;
                SkillTree?.Call("AwardXP", player, xp);
                Message(player, "SkillTreeXP", xp);
            }
        }

        private static void TryInvokeMethod(Action action)
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

        protected new static void Puts(string format, params object[] args)
        {
            Interface.Oxide.LogInfo("[{0}] {1}", Name, (args.Length != 0) ? string.Format(format, args) : format);
        }

        private static string GetUserName(ulong userid)
        {
            return ServerMgr.Instance.persistance.GetPlayerName(userid);
        }

        private IEnumerator ShowDataReportRoutine()
        {
            UpdateLastSeen();

            var total = data.LastSeen.ToList().Sum(x => GetRealtime(x.Value));
            var average = total / data.LastSeen.Count;

            yield return CoroutineEx.waitForSeconds(0.015f);

            Interface.Oxide.LogInfo("\nGenerating report...");
            Interface.Oxide.LogInfo("\nAverage offline time of {0} users: {1}", data.LastSeen.Count, _(TimeSpan.FromSeconds(average)));
            Interface.Oxide.LogInfo("\n\nCompiling rest of report...");

            var lastSeen = data.LastSeen.ToList();

            lastSeen.Sort((x, y) => x.Value.CompareTo(y.Value));

            foreach (var element in lastSeen)
            {
                _sb.AppendLine().AppendFormat("{0} was last seen {1} ago", GetUserName(element.Key) ?? element.Key.ToString(), _(TimeSpan.FromSeconds(GetRealtime(element.Value))));

                yield return CoroutineEx.waitForSeconds(0.015f);
            }

            Interface.Oxide.LogInfo(_sb.ToString());
            Interface.Oxide.LogInfo("\n\nReport finished.");

            reportCoroutine = null;
            _sb.Clear();
        }

        private static string _(string key, string userid, params object[] args)
        {
            string message = Instance.lang.GetMessage(key, Instance, userid);

            return args.Length > 0 ? string.Format(message, args) : message;
        }

        private string _(TimeSpan ts)
        {
            return $"{ts.Days:00}d {ts.Hours:00}h {ts.Minutes:00}m {ts.Seconds:00}s";
        }

        private int GetRealtime(int timestamp)
        {
            return Epoch.Current - timestamp;
        }

        private void RemoveReferences(AbandonedBuilding abandonedBuilding, List<BaseEntity> entities)
        {
            AbandonedReferences.RemoveAll((uid, x) => x?.center == abandonedBuilding.center);
            entities.Where(e => e.IsValid()).ToList().ForEach(e => AbandonedReferences.Remove(e.net.ID.Value));
            if (AbandonedBuildings.Remove(abandonedBuilding) && AbandonedReferences.Count == 0) Unsubscribe();
        }

        private object ProcessAbandonedBuilding(BaseEntity entity, HitInfo hitInfo, AbandonedBuilding abandonedBuilding)
        {
            if (!abandonedBuilding.InRange(entity.transform.position) || !IsBuildingDamageAllowed(entity, hitInfo))
            {
                CancelDamage(hitInfo);
                return false;
            }

            var attacker = hitInfo.Initiator as BasePlayer;

            if (abandonedBuilding.IsOwnerLocked && attacker.IsHuman() && !abandonedBuilding.CanDoActions(attacker))
            {
                abandonedBuilding.TryEjectFromLockedBase(attacker);
                abandonedBuilding.TryMessage(attacker, "Not An Ally");
                return false;
            }

            if (entity is BaseMountable)
            {
                return true;
            }

            if (hitInfo.WeaponPrefab is MLRSRocket)
            {
                return config.Abandoned.MLRS;
            }

            if (config.Abandoned.AutoTurret.Enabled && config.Abandoned.AutoTurret.AutoAdjust && entity is AutoTurret)
            {
                var turret = entity as AutoTurret;

                if (turret.sightRange < config.Abandoned.AutoTurret.SightRange * 2f)
                {
                    turret.sightRange = config.Abandoned.AutoTurret.SightRange * 2f;
                }

                return true;
            }

            if (!attacker.IsHuman())
            {
                return null;
            }

            if (config.Abandoned.BlockOutsideDamage && !abandonedBuilding.InRange(attacker.transform.position))
            {
                CancelDamage(hitInfo);
                return false;
            }

            if (config.Abandoned.CooldownBetweenEventsBlocksDamage && !abandonedBuilding.owners.Contains(attacker.userID) && HasEventCooldown(attacker, abandonedBuilding))
            {
                CancelDamage(hitInfo);
                return false;
            }

            if (IsLootingWeapon(hitInfo))
            {
                if (config.Abandoned.Reset)
                {
                    abandonedBuilding.Refresh();
                }
                if (abandonedBuilding.InRange(attacker.transform.position))
                {
                    abandonedBuilding.TrySetOwnerLock(attacker);
                }
            }

            return true;
        }

        private bool IsBuildingDamageAllowed(BaseEntity entity, HitInfo hitInfo)
        {
            if (config.Abandoned.BlocksImmune && entity is BuildingBlock)
            {
                return false;
            }

            if (config.Abandoned.TwigImmune && entity is BuildingBlock && (entity as BuildingBlock).grade == BuildingGrade.Enum.Twigs)
            {
                return false;
            }

            return true;
        }

        private object ProcessPlayerEntity(BaseEntity entity, HitInfo hitInfo)
        {
            if (entity.OwnerID == 76561199381312678 || entity is BradleyAPC || entity is PatrolHelicopter || entity.PrefabName.Contains("modular"))
            {
                return null;
            }

            if (Interface.CallHook("OnProcessPlayerEntity", entity, hitInfo) != null)
            {
                return null;
            }

            var weapon = hitInfo.Initiator ?? hitInfo.Weapon ?? hitInfo.WeaponPrefab;

            if (weapon is BaseProjectile && hitInfo.Initiator == null)
            {
                var projectile = weapon as BaseProjectile;

                hitInfo.Initiator = projectile.GetOwnerPlayer();
            }

            if (weapon is FireBall && hitInfo.Initiator == null)
            {
                hitInfo.Initiator = weapon.creatorEntity;
            }

            var attacker = hitInfo.Initiator as BasePlayer;

            if (attacker == null || attacker.IPlayer == null)
            {
                PrintDebugMessage(entity.transform.position, $"Attacker is null, victim is {entity.ShortPrefabName}");

                return null;
            }

            if (entity is BuildingBlock && (entity as BuildingBlock).grade == BuildingGrade.Enum.Twigs)
            {
                return null;
            }

            if (attacker.HasPermission("abandonedbases.attack") && !IsAlly(attacker.userID, entity.OwnerID) && !IsOnWaitingList(attacker.UserIDString))
            {
                ShowTimeLeft(attacker, entity, attacker.IsAdmin && attacker.HasPermission("abandonedbases.admindrawentity"));
            }

            return null;
        }

        public bool IsOnWaitingList(string userid)
        {
            if (_waitingList.Contains(userid))
            {
                return true;
            }
            _waitingList.Add(userid);
            timer.Once(10f, () => _waitingList.Remove(userid));
            return false;
        }

        private object ProcessVictim(BasePlayer victim, HitInfo hitInfo)
        {
            if (!victim.userID.IsSteamId())
            {
                return null;
            }

            var abandonedBuilding = AbandonedBuilding.Get(victim, hitInfo);

            if (abandonedBuilding == null)
            {
                return null;
            }

            if (victim.EqualNetID(hitInfo.Initiator))
            {
                return true;
            }

            if (DelaySettings.PvpDelay.ContainsKey(victim.userID))
            {
                return !hitInfo.Initiator.IsKilled() && abandonedBuilding.InRange(hitInfo.Initiator.transform.position);
            }

            if (hitInfo.Initiator is AutoTurret && EventTerritory(hitInfo.Initiator.transform.position))
            {
                hitInfo.damageTypes.Scale(DamageType.Bullet, UnityEngine.Random.Range(config.Abandoned.AutoTurret.Min, config.Abandoned.AutoTurret.Max));
                return config.Abandoned.AutoTurret.Enabled; // as-is this applies to current and newly deployed turrets which keeps it a level playing field
            }

            if (hitInfo.Initiator is FireBall && EventTerritory(hitInfo.Initiator.transform.position))
            {
                return true;
            }

            if (hitInfo.WeaponPrefab is MLRSRocket && EventTerritory(victim.transform.position))
            {
                return config.Abandoned.MLRS;
            }

            var weapon = hitInfo.Initiator ?? hitInfo.Weapon ?? hitInfo.WeaponPrefab;

            if (weapon.IsKilled() || weapon.skinID == 14922524)
            {
                return null;
            }

            if (weapon is BaseProjectile && hitInfo.Initiator == null)
            {
                var projectile = weapon as BaseProjectile;

                hitInfo.Initiator = projectile.GetOwnerPlayer();
            }

            if (weapon is FireBall && hitInfo.Initiator == null)
            {
                hitInfo.Initiator = weapon.creatorEntity;
            }

            var attacker = hitInfo.Initiator as BasePlayer;

            if (!attacker.IsHuman())
            {
                return !EventTerritory(victim.transform.position) || !IsTrueDamage(weapon) ? (object)null : true;
            }

            if (config.PlayersCanKillSleepers && AbandonedSleepers.Contains(victim.userID) && victim.IsSleeping() && !victim.IsConnected)
            {
                return true;
            }

            if (!abandonedBuilding.AllowPVP || !abandonedBuilding.InRange(victim.transform.position) || !abandonedBuilding.InRange(attacker.transform.position))
            {
                CancelDamage(hitInfo);

                return null;
            }

            if (abandonedBuilding.anchor.IsKilled())
            {
                return true;
            }

            return attacker.GetParentEntity() is Tugboat && victim.GetParentEntity() is Tugboat ? true : (object)null;
        }

        private bool IsTrueDamage(BaseEntity e)
        {
            if (!e.IsValid())
            {
                return false;
            }
            return AbandonedReferences.ContainsKey(e.net.ID.Value) || e.skinID == 1587601905 || e.ShortPrefabName == "spikes.floor" || TrueDamage.Contains(e.GetType().Name);
        }

        private void PrintDebugMessage(Vector3 position, string message)
        {
            if (DebugMode)
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (player.IsAdmin && player.Distance(position) < 100f)
                    {
                        Player.Message(player, message);
                    }
                }
            }
        }

        private bool IsAlly(ulong playerId, ulong targetId)
        {
            if (playerId == targetId)
            {
                return true;
            }

            if (RelationshipManager.ServerInstance.playerToTeam.TryGetValue(playerId, out var team) && team.members.Contains(targetId))
            {
                return true;
            }

            if (Clans.CanCall() && Convert.ToBoolean(Clans?.Call("IsMemberOrAlly", playerId, targetId)))
            {
                return true;
            }

            if (Friends.CanCall() && Convert.ToBoolean(Friends?.Call("AreFriends", playerId.ToString(), targetId.ToString())))
            {
                return true;
            }

            return false;
        }

        private void ShowTimeLeft(BasePlayer player, BaseEntity entity, bool canDrawEntity)
        {
            if (IsIgnoredPrefab(entity.ShortPrefabName) || IsInZone(entity.transform.position) || IsAbandoned(entity))
            {
                return;
            }

            if (config.Abandoned.TooClose && EventTerritory(entity.transform.position))
            {
                return;
            }

            if (config.Abandoned.Disabled.Exists(x => x.CanBlockAutomaticConversion()))
            {
                return;
            }

            var timestamp = Epoch.Current;
            var owners = new List<ulong>();
            var isAlly = false;

            if (entity is Tugboat)
            {
                var tugboat = entity as Tugboat;
                var vehiclePrivilege = GetVehiclePrivilege(tugboat.children);

                if (CanConvert(player.IPlayer, tugboat, vehiclePrivilege, owners, timestamp, player.userID, out isAlly))
                {
                    TryConvertTugboat(player.IPlayer, tugboat, vehiclePrivilege, owners, config.Abandoned.AllowPVPAttack == true);

                    return;
                }
            }
            else
            {
                var priv = entity.GetBuildingPrivilege(entity.WorldSpaceBounds());

                if (priv == null)
                {
                    if (!EventTerritory(entity.transform.position))
                    {
                        Message(player.IPlayer, "No privilege found");
                    }

                    return;
                }

                var building = priv.GetBuilding();

                if (CanConvert(player.IPlayer, building, priv, owners, timestamp, canDrawEntity, player))
                {
                    TryConvertCompound(player.IPlayer, building, priv, owners, config.Abandoned.AllowPVPAttack == true);

                    return;
                }
            }

            if (isAlly || !player.HasPermission("abandonedbases.attack.time") || EventTerritory(entity.transform.position))
            {
                return;
            }

            var timeLeft = double.MinValue;

            foreach (var owner in owners)
            {
                var purge = PurgeSettings.Find(owner);

                if (purge == null || purge.NoPurge)
                {
                    continue;
                }

                int lastSeen;
                if (!data.LastSeen.TryGetValue(owner, out lastSeen))
                {
                    continue;
                }

                timeLeft = Math.Max(timeLeft, lastSeen + purge.Lifetime - timestamp);
            }

            if (timeLeft != double.MinValue)
            {
                Message(player, "TimeLeft", FormatTime(player.UserIDString, timeLeft < 0 ? 0 : timeLeft));
            }
        }

        private void RegisterPermissions()
        {
            permission.RegisterPermission("abandonedbases.admin", this);
            permission.RegisterPermission("abandonedbases.canbypass", this);
            permission.RegisterPermission("abandonedbases.admindrawentity", this);
            permission.RegisterPermission("abandonedbases.convert.free", this);
            permission.RegisterPermission("abandonedbases.attack", this);
            permission.RegisterPermission("abandonedbases.attack.time", this);
            permission.RegisterPermission("abandonedbases.convert.cancel", this);
            permission.RegisterPermission("abandonedbases.convert.claim", this);
            permission.RegisterPermission("abandonedbases.convert.nocooldown", this);
            permission.RegisterPermission("abandonedbases.convert.cancel.nocooldown", this);
            permission.RegisterPermission("abandonedbases.noeventcooldown", this);
            permission.RegisterPermission("abandonedbases.convert", this);
            permission.RegisterPermission("abandonedbases.exclude", this);
            permission.RegisterPermission("abandonedbases.notices", this);
            permission.RegisterPermission("abandonedbases.purgeday", this);
            permission.RegisterPermission("abandonedbases.report", this);
            permission.RegisterPermission("abandonedbases.teleport", this);
        }

        private void RegisterCommands()
        {
            AddCovalenceCommand("ab.report", "CommandReport");
            AddCovalenceCommand("ab.debug", "CommandDebug");
            AddCovalenceCommand("sab", "CommandStart");
            AddCovalenceCommand("sar", "CommandConvert");
        }

        private void StartNotificationTimer()
        {
            timer.Repeat(Mathf.Clamp(config.Messages.Interval, 1f, 60f), 0, CheckNotifications);
        }

        private void Unsubscribe()
        {
            Unsubscribe(nameof(OnEngineStart));
            Unsubscribe(nameof(OnEntitySpawned));
            Unsubscribe(nameof(OnEntityDeath));
            Unsubscribe(nameof(OnEntityKill));
            Unsubscribe(nameof(OnEntityBuilt));
            Unsubscribe(nameof(OnPlayerSleepEnded));
            Unsubscribe(nameof(OnServerCommand));
            Unsubscribe(nameof(OnPlayerCommand));
            Unsubscribe(nameof(CanLootPlayer));
            Unsubscribe(nameof(CanTeleport));
            Unsubscribe(nameof(CanEntityBeTargeted));
            Unsubscribe(nameof(CanEntityTrapTrigger));
            Unsubscribe(nameof(CanOpenBackpack));
            Unsubscribe(nameof(OnRestoreUponDeath));
            Unsubscribe(nameof(OnLootEntityEnd));
            Unsubscribe(nameof(CanLootEntity));
            Unsubscribe(nameof(CanPickupEntity));
        }

        private void Subscribe()
        {
            if (config.Abandoned.BlacklistedCommands.Count > 0 && !config.Abandoned.BlacklistedCommands.All(DefaultBlacklistCommands().Contains))
            {
                Subscribe(nameof(OnServerCommand));
                Subscribe(nameof(OnPlayerCommand));
            }

            if (config.Messages.LocalCompletion > 0 || config.Messages.RaidCompletion)
            {
                Subscribe(nameof(OnLootEntityEnd));
            }

            if (config.Abandoned.Tugboats.Engine)
            {
                Subscribe(nameof(OnEngineStart));
            }

            if (!config.Abandoned.Backpacks)
            {
                Subscribe(nameof(CanOpenBackpack));
            }

            if (config.Abandoned.CannotLootWoundedPlayers)
            {
                Subscribe(nameof(CanLootPlayer));
            }

            if (config.Abandoned.BlacklistedPickupItems.Count > 0)
            {
                Subscribe(nameof(CanPickupEntity));
            }

            if (!config.Abandoned.AllowTeleport)
            {
                Subscribe(nameof(CanTeleport));
            }

            Subscribe(nameof(CanLootEntity));
            Subscribe(nameof(OnEntitySpawned));
            Subscribe(nameof(OnEntityDeath));
            Subscribe(nameof(OnEntityKill));
            Subscribe(nameof(OnEntityBuilt));
            Subscribe(nameof(OnPlayerSleepEnded));
            Subscribe(nameof(CanEntityBeTargeted));
            Subscribe(nameof(CanEntityTrapTrigger));
            Subscribe(nameof(OnRestoreUponDeath));
        }

        private void SetupAbandonedRoutine()
        {
            if (config.Startup)
            {
                StartAbandonedRoutine(null);
            }

            if (config.Delay > 0f)
            {
                timer.Every(Math.Max(900f, config.Delay), StartAbandonedRoutine);
            }
        }

        private void StopAbandonedCoroutine()
        {
            if (reportCoroutine != null)
            {
                ServerMgr.Instance.StopCoroutine(reportCoroutine);
                reportCoroutine = null;
            }

            if (abandonedCoroutine != null)
            {
                ServerMgr.Instance.StopCoroutine(abandonedCoroutine);
                abandonedCoroutine = null;
            }
        }

        private void StartAbandonedRoutine()
        {
            StartAbandonedRoutine(null);
        }

        private void StartAbandonedRoutine(IPlayer user)
        {
            if (user == null && config.Abandoned.MinimumOnlinePlayers > 0 && BasePlayer.activePlayerList.Count < config.Abandoned.MinimumOnlinePlayers)
            {
                timer.Once(1f, () => StartAbandonedRoutine(null));
                return;
            }

            if (user == null && config.Abandoned.Disabled.Exists(x => x != null && x.CanBlockAutomaticConversion()))
            {
                return;
            }

            if (permission.GroupHasPermission("default", "abandonedbases.exclude"))
            {
                Puts("Invalid permission set for default group: {0}", "abandonedbases.exclude");
                return;
            }

            if (abandonedCoroutine == null)
            {
                Message(user, "StartScan");
                abandonedCoroutine = ServerMgr.Instance.StartCoroutine(AbandonedCoroutine(user));
            }
            else if (user != null)
            {
                ServerMgr.Instance.StopCoroutine(abandonedCoroutine);
                abandonedCoroutine = null;
                Message(user, "You have aborted the scan.");
            }
        }

        private int _converted = 0;
        private int _deleted = 0;
        private int _skipped = 0;

        private IEnumerator AbandonedCoroutine(IPlayer user)
        {
            float waitTime = config.Abandoned.WaitTime < 2f ? 2f : config.Abandoned.WaitTime;
            int timestamp = Epoch.Current;

            _converted = _deleted = _skipped = 0;

            UpdateLastSeen();

            yield return AbandonedBuildingCo(user, timestamp, waitTime);

            KillInactiveSleepers(timestamp);

            if (!config.Abandoned.Tugboats.Manual)
            {
                yield return AbandonedTugboatCo(timestamp, waitTime);
            }

            if (user != null)
            {
                Message(user, "ScanEnded", _converted, _deleted, _skipped);
            }

            abandonedCoroutine = null;
        }

        private IEnumerator AbandonedBuildingCo(IPlayer user, int timestamp, float waitTime)
        {
            foreach (var building in BuildingManager.server.buildingDictionary.Values)
            {
                if (building.decayEntities.IsNullOrEmpty())
                {
                    continue;
                }

                var owners = new List<ulong>();
                var priv = building.GetDominatingBuildingPrivilege();

                if (CanConvert(null, building, priv, owners, timestamp))
                {
                    yield return TryConvertCompoundCo(null, building, priv, owners, true, config.Abandoned.AllowPVP);

                    yield return CoroutineEx.waitForSeconds(waitTime);
                }

                yield return CoroutineEx.waitForSeconds(0.1f);
            }
        }

        private IEnumerator AbandonedTugboatCo(int timestamp, float waitTime)
        {
            bool isAlly;

            foreach (var entity in BaseNetworkable.serverEntities)
            {
                if (entity.prefabID != 268742921)
                {
                    continue;
                }

                var tugboat = entity as Tugboat;

                if (IsAbandoned(tugboat))
                {
                    continue;
                }

                var priv = GetVehiclePrivilege(tugboat.children);

                if (priv == null)
                {
                    continue;
                }

                var owners = new List<ulong>();

                if (CanConvert(null, tugboat, priv, owners, timestamp, 0uL, out isAlly))
                {
                    yield return TryConvertTugboatCo(null, tugboat, priv, owners, !config.Abandoned.Tugboats.Unlock, config.Abandoned.AllowPVP);

                    yield return CoroutineEx.waitForSeconds(waitTime);
                }

                yield return CoroutineEx.waitForSeconds(0.1f);
            }
        }

        public VehiclePrivilege GetVehiclePrivilege(List<BaseEntity> children)
        {
            foreach (BaseEntity child in children)
            {
                VehiclePrivilege vehiclePrivilege = child as VehiclePrivilege;
                if (vehiclePrivilege != null && vehiclePrivilege.AnyAuthed())
                {
                    return vehiclePrivilege;
                }
            }

            return null;
        }

        private bool CanConvert(IPlayer user, BuildingManager.Building building, BuildingPrivlidge priv, List<ulong> owners, int timestamp, bool canDrawEntity = false, BasePlayer player = null)
        {
            bool canPurge = true;
            bool hasPrivilege = false;
            bool canDrawPrivilege = canDrawEntity;

            foreach (var entity in building.decayEntities)
            {
                if (entity.IsKilled() || AbandonedReferences.ContainsKey(entity.net.ID.Value))
                {
                    continue;
                }

                if (IsInZone(entity.transform.position))
                {
                    canPurge = false;
                }

                AdminDrawEntity(entity);

                if (canDrawEntity && entity.OwnerID == player.OwnerID)
                {
                    player.SendConsoleCommand("ddraw.text", 10f, Color.cyan, entity.CenterPoint(), _("Drawn Entity", player.UserIDString));
                }

                if (!owners.Contains(entity.OwnerID))
                {
                    owners.Add(entity.OwnerID);
                }

                if (purgeDay)
                {
                    hasPrivilege = true;

                    continue;
                }

                if (priv.IsValid())
                {
                    if (!hasPrivilege)
                    {
                        AdminDrawEntity(priv);

                        hasPrivilege = true;
                    }

                    if (canDrawPrivilege && priv.AnyAuthed() && priv.IsAuthed(player))
                    {
                        player.SendConsoleCommand("ddraw.text", 10f, Color.cyan, priv.CenterPoint(), _("Drawn Authed", player.UserIDString));

                        canDrawPrivilege = false;
                    }

                    if (canPurge && !IsUserExcluded(entity.OwnerID) && !CanPurge(entity.OwnerID, timestamp, priv.authorizedPlayers, owners))
                    {
                        canPurge = false;
                    }
                }
            }

            if (!canPurge || owners.All(IsUserExcluded))
            {
                Message(user, owners);

                return false;
            }

            if (!hasPrivilege)
            {
                Message(user, "No privilege found");

                return false;
            }

            return true;
        }

        private bool CanConvert(IPlayer user, Tugboat tugboat, VehiclePrivilege priv, List<ulong> owners, int timestamp, ulong userid, out bool isAlly)
        {
            isAlly = false;

            if (priv == null || tugboat.children == null || !priv.AnyAuthed() && !tugboat.children.Exists(child => child.OwnerID.IsSteamId()))
            {
                return false;
            }

            foreach (var auth in priv.authorizedPlayers)
            {
                if (userid != 0 && !isAlly && IsAlly(auth.userid, userid))
                {
                    isAlly = true;
                    return false;
                }

                if (!owners.Contains(auth.userid))
                {
                    owners.Add(auth.userid);
                }
            }

            bool canPurge = true;

            foreach (var entity in tugboat.children)
            {
                if (entity.IsKilled() || entity.OwnerID == 0 || AbandonedReferences.ContainsKey(entity.net.ID.Value))
                {
                    continue;
                }

                if (userid != 0 && !isAlly && IsAlly(entity.OwnerID, userid))
                {
                    isAlly = true;
                    return false;
                }

                if (IsInZone(entity.transform.position))
                {
                    canPurge = false;
                }

                if (!owners.Contains(entity.OwnerID))
                {
                    owners.Add(entity.OwnerID);
                }

                if (!purgeDay && canPurge && !IsUserExcluded(entity.OwnerID) && !CanPurge(entity.OwnerID, timestamp, priv.authorizedPlayers, owners))
                {
                    canPurge = false;
                }

                AdminDrawEntity(entity);
            }

            if (!owners.Exists(owner => owner.IsSteamId()))
            {
                Message(user, "Tugboat Player Requirement");

                return false;
            }

            if (!canPurge || owners.All(IsUserExcluded))
            {
                Message(user, owners);

                return false;
            }

            return true;
        }

        private void Message(IPlayer user, List<ulong> owners)
        {
            if (user == null)
            {
                return;
            }

            Message(user, "Base is active");

            if (!user.IsAdmin && !user.HasPermission("abandonedbases.attack.time"))
            {
                return;
            }

            int num = 0;

            owners.ForEach(userid =>
            {
                if (userid.HasPermission("abandonedbases.exclude") && !user.IsAdmin)
                {
                    return;
                }
                var username = GetUserName(userid) ?? userid.ToString();
                if (BasePlayer.activePlayerList.Exists(x => x.userID == userid))
                {
                    user.Message(_("FormatOnline", user.Id).Replace("{index}", $"{++num}").Replace("{username}", username).Replace("{userid}", $"{userid}"));
                    return;
                }
                int lastSeen;
                if (data.LastSeen.TryGetValue(userid, out lastSeen))
                {
                    user.Message(_("FormatLastSeen", user.Id).Replace("{index}", $"{++num}").Replace("{username}", username).Replace("{userid}", $"{userid}").Replace("{time}", FormatTime(user.Id, GetRealtime(lastSeen))));
                }
                else
                {
                    user.Message(_("FormatLastSeenUnknown", user.Id).Replace("{index}", $"{++num}").Replace("{username}", username).Replace("{userid}", $"{userid}"));
                }
            });
        }

        private void AdminDrawEntity(BaseEntity entity)
        {
            if (!DebugMode)
            {
                return;
            }
            var authorizedPlayers = entity is BuildingPrivlidge ? (entity as BuildingPrivlidge).authorizedPlayers : entity is VehiclePrivilege ? (entity as VehiclePrivilege).authorizedPlayers : null;
            int index;
            BasePlayer.activePlayerList.Where(x => x.IsAdmin).ForEach(player =>
            {
                if (authorizedPlayers != null)
                {
                    foreach (var auth in authorizedPlayers)
                    {
                        if ((index = config.Purges.FindIndex(purge => auth.userid.HasPermission(purge.Permission))) != -1)
                        {
                            player.SendConsoleCommand("ddraw.text", 30f, Color.magenta, entity.transform.position, "A");
                            return;
                        }
                    }
                }
                if ((index = config.Purges.FindIndex(purge => entity.OwnerID.HasPermission(purge.Permission))) != -1)
                {
                    player.SendConsoleCommand("ddraw.text", 30f, Color.magenta, entity.transform.position, "X");
                }
            });
        }

        private void KillInactiveSleepers(int timestamp)
        {
            Subscribe(nameof(OnEntitySpawned));

            foreach (var target in BasePlayer.sleepingPlayerList)
            {
                if (IsUserExcluded(target.userID))
                {
                    continue;
                }

                if (CanPurge(target.userID, timestamp))
                {
                    AbandonedSleepers.Add(target.userID);

                    if (config.MoveInventory && target.GetBuildingPrivilege() != null || config.PlayersCanKillSleepers && target.IsBuildingAuthed())
                    {
                        continue;
                    }

                    if (config.KillInactiveSleepers)
                    {
                        target.Die(new HitInfo(target, target, DamageType.Suicide, 1000f));
                    }
                }
            }
        }

        private bool CanPurge(ulong userid, int timestamp, List<ProtoBuf.PlayerNameID> authorizedPlayers, List<ulong> owners) // Credits: misticos (used with permission)
        {
            bool canPurge = true;

            foreach (var auth in authorizedPlayers)
            {
                if (!owners.Contains(auth.userid))
                {
                    owners.Add(auth.userid);
                }

                if (canPurge && !IsUserExcluded(auth.userid) && !CanPurge(auth.userid, timestamp))
                {
                    canPurge = false;
                }
            }

            if (canPurge && !CanPurge(userid, timestamp))
            {
                return userid == 0 && owners.Exists(x => x != 0);
            }

            return canPurge;
        }

        private bool CanPurge(ulong userid, int timestamp)
        {
            var purge = PurgeSettings.Find(userid);

            if (purge == null || purge.NoPurge)
            {
                return false;
            }

            int lastSeen;
            if (!data.LastSeen.TryGetValue(userid, out lastSeen))
            {
                data.LastSeen[userid] = timestamp;
                return false;
            }

            return purge.Lifetime > 0 && timestamp > lastSeen + purge.Lifetime;
        }

        private void UpdateLastSeen()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player.IsReallyConnected())
                {
                    UpdateLastSeen(player, Epoch.Current);
                }
            }

            SaveData();
        }

        private void UpdateLastSeen(BasePlayer player, int timestamp)
        {
            if (!player.IsHuman())
            {
                return;
            }

            if (DebugMode)
            {
                Puts("Updated last seen time for {0} ({1})", player.displayName, player.userID);
            }

            data.LastSeen[player.userID] = timestamp;
        }

        private void CancelDamage(HitInfo hitInfo)
        {
            hitInfo.damageTypes = new DamageTypeList();
            hitInfo.DoHitEffects = false;
            hitInfo.DidHit = false;
        }

        private void DestroyAll()
        {
            foreach (var abandonedBuilding in AbandonedBuildings.ToList())
            {
                if (abandonedBuilding == null) continue;
                abandonedBuilding.RemoveExpiration();
                abandonedBuilding.DestroyMe();
            }
        }

        private static bool InRange(Vector3 a, Vector3 b, float distance)
        {
            return (new Vector3(a.x, 0f, a.z) - new Vector3(b.x, 0f, b.z)).magnitude <= distance;
        }

        private bool IsTooClose(Vector3 position)
        {
            return AbandonedBuildings.Exists(x => !x.isDestroyed && x.InRange(position));
        }

        private bool IsTooClose(IPlayer user, List<BaseEntity> entities, bool check)
        {
            if (entities.Exists(entity => RaidableBasesEventTerritory(entity.transform.position) || check && config.Abandoned.TooClose && IsTooClose(entity.transform.position)))
            {
                Message(user, "Near Event Base");

                return true;
            }

            return false;
        }

        private bool EventTerritory(Vector3 position)
        {
            return AbandonedBuildings.Exists(x => x.InRange(position));
        }

        [HookMethod("isAbandoned")]
        public bool IsAbandoned(BaseEntity entity) => entity.IsValid() && AbandonedReferences.ContainsKey(entity.net.ID.Value);

        private void StopManualConversion(IPlayer user)
        {
            if (user != null)
            {
                _conversions.RemoveAll(uc => uc.userid == user.Id);
            }
        }

        private void TryConvertCompound(IPlayer user, BuildingManager.Building building, BuildingPrivlidge priv, List<ulong> owners, bool pvp, Payment payment = null, float radius = 0f)
        {
            if (!_conversions.Exists(uc => uc.Exists(user, owners)))
            {
                IEnumerator co = TryConvertCompoundCo(user, building, priv, owners, false, pvp, payment, radius);

                _conversions.Add(new UserConversion(user.Id, owners, ServerMgr.Instance.StartCoroutine(co)));
            }
        }

        private IEnumerator TryConvertCompoundCo(IPlayer user, BuildingManager.Building building, BuildingPrivlidge priv, List<ulong> owners, bool delete, bool allowPVP, Payment payment = null, float radius = 0f)
        {
            if (priv.IsKilled() || building == null || building.decayEntities.IsNullOrEmpty()) { _skipped++; yield break; }

            var entities = building.decayEntities.ToList<BaseEntity>();

            yield return FindCompoundEntities(entities, owners, priv.transform.position, Mathf.Max(50f, config.Abandoned.MaxDynamicRadius));

            if (IsTooClose(user, entities, !purgeDay))
            {
                StopManualConversion(user);
                _skipped++;
                yield break;
            }

            var containers = new List<StorageContainer>();
            var foundations = new List<Vector3>();
            var compound = new List<Vector3>();
            var walls = new List<Vector3>();

            if (!AbandonedBuilding.AddRange(entities, containers, foundations, compound, walls) && !CanBypass(user, priv, Epoch.Current) || compound.Count < 2)
            {
                Message(user, "Base Requirements", foundations.Count, config.Abandoned.FoundationLimit, walls.Count, config.Abandoned.WallLimit);

                StopManualConversion(user);

                if (delete)
                {
                    AbandonedBuilding.UndoLoop(building.decayEntities);
                    _deleted++;
                }

                yield break;
            }

            yield return CoroutineEx.waitForSeconds(0.25f);

            var bounds = new Bounds(compound[0], Vector3.zero);

            for (int i = 1; i < compound.Count; i++)
            {
                bounds.Encapsulate(compound[i]);
            }

            if (config.Abandoned.Dynamic && radius == 0f)
            {
                radius = Mathf.Min(config.Abandoned.MaxDynamicRadius, bounds.extents.Max() + Mathf.Max(9f, config.Abandoned.Padding));
            }

            if (radius == 0f || radius < config.Abandoned.MinCustomSphereRadius)
            {
                radius = Mathf.Max(config.Abandoned.SphereRadius, config.Abandoned.MinCustomSphereRadius);
            }

            var center = bounds.center.WithY(TerrainMeta.HeightMap.GetHeight(bounds.center));
            var abandonedBuilding = new GameObject().AddComponent<AbandonedBuilding>();

            abandonedBuilding.gameObject.name = "AbandonedBase";
            abandonedBuilding.Setup(this, user, timer, compound, foundations, entities, containers, owners, center, payment, radius, allowPVP);

            LogUserAction(user, abandonedBuilding);
        }

        private void TryConvertTugboat(IPlayer user, Tugboat tugboat, VehiclePrivilege priv, List<ulong> owners, bool pvp, Payment payment = null, float radius = 0f)
        {
            if (!_conversions.Exists(uc => uc.Exists(user, owners)))
            {
                IEnumerator co = TryConvertTugboatCo(user, tugboat, priv, owners, false, pvp, payment, radius);

                _conversions.Add(new UserConversion(user.Id, owners, ServerMgr.Instance.StartCoroutine(co)));
            }
        }

        private IEnumerator TryConvertTugboatCo(IPlayer user, Tugboat tugboat, VehiclePrivilege priv, List<ulong> owners, bool delete, bool allowPVP, Payment payment = null, float radius = 0f)
        {
            if (priv == null || tugboat.IsKilled() || tugboat.children.IsNullOrEmpty()) { _skipped++; yield break; }

            var entities = tugboat.children.Where(entity =>
            {
                if (entity.IsKilled())
                {
                    return false;
                }
                if (entity is BasePlayer)
                {
                    return false;
                }
                return !entity.ShortPrefabName.Contains("tugboat");
            });

            if (RaidableBasesEventTerritory(tugboat.transform.position))
            {
                Message(user, "Near Event Base");
                _skipped++;
                yield break;
            }

            var containers = new List<StorageContainer>();
            var compound = new List<Vector3>();
            var loot = 0;

            if (!AbandonedBuilding.AddRange(entities, containers, compound, ref loot))
            {
                StopManualConversion(user);

                if (delete)
                {
                    SinkTugboat(tugboat);
                }
                else
                {
                    UnlockTugboat(user, tugboat, priv, entities, loot);
                }

                yield break;
            }

            yield return CoroutineEx.waitForSeconds(0.25f);

            if (radius == 0f || radius < config.Abandoned.MinCustomSphereRadius)
            {
                radius = Mathf.Max(config.Abandoned.SphereRadius, config.Abandoned.MinCustomSphereRadius, 25f);
            }

            var abandonedBuilding = tugboat.gameObject.AddComponent<AbandonedBuilding>();
            var foundations = new List<Vector3> { tugboat.transform.position };

            if (!entities.Contains(tugboat))
            {
                entities.Add(tugboat);
            }

            abandonedBuilding.anchor = tugboat;
            abandonedBuilding.Setup(this, user, timer, compound, foundations, entities, containers, owners, tugboat.transform.position, payment, radius, allowPVP);

            if (config.Abandoned.Tugboats.Engine)
            {
                tugboat.SetFlag(BaseEntity.Flags.Reserved1, false, true);
            }

            LogUserAction(user, abandonedBuilding);
        }

        private void SinkTugboat(Tugboat tugboat)
        {
            if (!tugboat.IsDying)
            {
                tugboat.health = 0;
                tugboat.OnKilled(null);
                _deleted++;
            }
        }

        private void UnlockTugboat(IPlayer user, Tugboat tugboat, VehiclePrivilege priv, List<BaseEntity> entities, int loot)
        {
            Message(user, "Tugboat Unlocked", loot, config.Abandoned.Tugboats.Loot);

            entities.ToList().ForEach(ResetLock);
            priv.authorizedPlayers.Clear();
            priv.UpdateMaxAuthCapacity();
            priv.SendNetworkUpdate();
            _skipped++;
        }

        private void LogUserAction(IPlayer user, AbandonedBuilding abandonedBuilding)
        {
            _converted++;

            StopManualConversion(user);

            if (user == null || user.IsServer)
            {
                return;
            }

            abandonedBuilding.canReassign = config.Abandoned.AllowManualClaims;

            var player = user.ToPlayer();
            string text = string.Format("{0} ({1}) executed command /{2} at {3} in {4} at time {5}", player.displayName, player.userID, "sar", player.transform.position, abandonedBuilding.GetGrid(), DateTime.Now);

            Puts(text);

            if (config.UseLogFile)
            {
                LogToFile("sar", text, this, false);
            }

            Message(user, "Start");

            abandonedBuilding.SetConversionCooldown(player.userID);
        }

        private void ResetLock(BaseEntity entity)
        {
            if (entity.IsKilled())
            {
                return;
            }
            if (entity is CodeLock)
            {
                ResetCodeLock(entity as CodeLock);
            }
            else if (entity is KeyLock)
            {
                ResetKeyLock(entity as KeyLock);
            }
        }

        private void ResetCodeLock(CodeLock codeLock)
        {
            codeLock.SetFlag(BaseEntity.Flags.Locked, false);
            codeLock.ClearCodeEntryBlocked();
            codeLock.whitelistPlayers.Clear();
            codeLock.guestPlayers.Clear();
            codeLock.hasGuestCode = false;
            codeLock.hasCode = false;
            codeLock.guestCode = "";
            codeLock.code = "";
            codeLock.OwnerID = 0;
            codeLock.SendNetworkUpdate();
        }

        private void ResetKeyLock(KeyLock keyLock)
        {
            keyLock.SetFlag(BaseEntity.Flags.Locked, false);
            keyLock.firstKeyCreated = false;
            keyLock.keyCode = 0;
            keyLock.OwnerID = 0;
            keyLock.SendNetworkUpdate();
        }

        private bool CanBypass(IPlayer user, BuildingPrivlidge priv, int timestamp)
        {
            if (purgeDay && config.Abandoned.DespawnTime <= 0f && user == null && !CanPurge(priv.OwnerID, timestamp))
            {
                return true;
            }
            return false;
        }

        private bool RaidableBasesEventTerritory(Vector3 position) => Convert.ToBoolean(RaidableBases?.Call("EventTerritory", position));

        private IEnumerator FindCompoundEntities(List<BaseEntity> source, List<ulong> owners, Vector3 position, float radius)
        {
            var entities = Pool.GetList<BaseCombatEntity>();
            int checks = 0;

            foreach (BaseNetworkable serverEntity in BaseNetworkable.serverEntities)
            {
                if (++checks > 1000)
                {
                    checks = 0;
                    yield return null;
                }

                if (serverEntity.IsKilled() || !InRange(serverEntity.transform.position, position, radius))
                {
                    continue;
                }

                if (serverEntity is DecayEntity)
                {
                    var decayEntity = serverEntity as DecayEntity;

                    if (source.Contains(decayEntity) || decayEntity.OwnerID != 0 && !owners.Contains(decayEntity.OwnerID))
                    {
                        continue;
                    }

                    var building = decayEntity.GetBuilding();

                    if (building == null || !building.HasDecayEntities() || SameOwners(building, owners))
                    {
                        source.Add(decayEntity);
                    }
                }
                else if (serverEntity is BasePlayer)
                {
                    var player = serverEntity as BasePlayer;

                    if (owners.Contains(player.userID) && player.IsSleeping() && !player.IsConnected)
                    {
                        if (config.PlayersCanKillSleepers)
                        {
                            AbandonedSleepers.Add(player.userID);
                        }
                        if (config.KillInactiveSleepers)
                        {
                            source.Add(player);
                        }
                    }
                }
                else if (serverEntity is BaseCombatEntity)
                {
                    entities.Add(serverEntity as BaseCombatEntity);
                }
            }

            foreach (var entity in entities)
            {
                if (entity.IsKilled() || source.Contains(entity) || !entity.OwnerID.IsSteamId() && !IsPlayerEntity(entity) || entity.OwnerID.IsSteamId() && !owners.Contains(entity.OwnerID))
                {
                    continue;
                }

                var building = entity.GetBuildingPrivilege()?.GetBuilding();

                if (building == null || SameOwners(building, owners))
                {
                    source.Add(entity);
                }

                yield return null;
            }

            Pool.FreeList(ref entities);
        }

        private List<string> _playerEntityPrefabNames = new List<string> { "building", "deploy", "vehicle", "xmas" };

        private bool IsPlayerEntity(BaseCombatEntity entity)
        {
            if (_playerEntityPrefabNames.Exists(entity.PrefabName.Contains))
            {
                return true;
            }
            return entity is BaseMountable || entity is SamSite || entity is ElevatorLift;
        }

        private bool SameOwners(BuildingManager.Building building, List<ulong> owners)
        {
            if (building.HasBuildingPrivileges() && !building.buildingPrivileges.All(priv => priv.authorizedPlayers.Exists(auth => owners.Contains(auth.userid))))
            {
                return false;
            }

            if (building.HasDecayEntities() && !building.decayEntities.All(x => x.OwnerID == 0uL || owners.Contains(x.OwnerID)))
            {
                return false;
            }

            return true;
        }

        private static List<T> FindEntitiesOfType<T>(Vector3 a, float n, int m = -1) where T : BaseEntity
        {
            int hits = Physics.OverlapSphereNonAlloc(a, n, Vis.colBuffer, m, QueryTriggerInteraction.Collide);
            List<T> entities = new List<T>();
            for (int i = 0; i < hits; i++)
            {
                var collider = Vis.colBuffer[i];
                if (collider == null) continue;
                var entity = collider.ToBaseEntity() as T;
                if (entity != null && !entities.Contains(entity)) entities.Add(entity);
                Vis.colBuffer[i] = null;
            }
            return entities;
        }

        private bool IsLootingWeapon(HitInfo hitInfo)
        {
            if (hitInfo == null || hitInfo.damageTypes == null)
            {
                return false;
            }

            return hitInfo.damageTypes.Has(DamageType.Explosion) || hitInfo.damageTypes.Has(DamageType.Heat) || hitInfo.damageTypes.IsMeleeType();
        }

        private string FormatTime(string userid, double seconds) // Credits: MoNaH
        {
            var dd = string.Join("\\", $"<color=#FFA500>*</color>".ToCharArray()).Replace("\\*", "dd");
            var hh = string.Join("\\", $"<color=#FFA500>*</color>".ToCharArray()).Replace("\\*", "hh");
            var mm = string.Join("\\", $"<color=#FFA500>*</color>".ToCharArray()).Replace("\\*", "mm");
            var ss = string.Join("\\", $"<color=#FFA500>*</color>".ToCharArray()).Replace("\\*", "ss");
            var ddt = string.Join("\\", _("Days", userid).ToCharArray());
            var hht = string.Join("\\", _("Hours", userid).ToCharArray());
            var mmt = string.Join("\\", _("Minutes", userid).ToCharArray());
            var sst = string.Join("\\", _("Seconds", userid).ToCharArray());
            var tFormat = string.Empty;

            if (seconds >= 86400)
            {
                tFormat = "\\" + dd + "\\ \\" + ddt + "\\ \\" + hh + "\\ \\" + hht + "\\ \\" + mm + "\\ \\" + mmt + "\\ \\" + ss + "\\ \\" + sst;
            }
            else if (seconds >= 3600)
            {
                tFormat = "\\" + hh + "\\ \\" + hht + "\\ \\" + mm + "\\ \\" + mmt + "\\ \\" + ss + "\\ \\" + sst;
            }
            else if (seconds >= 60)
            {
                tFormat = "\\" + mm + "\\ \\" + mmt + "\\ \\" + ss + "\\ \\" + sst;
            }
            else if (seconds >= 0)
            {
                tFormat = "\\" + ss + "\\ \\" + sst;
            }

            return TimeSpan.FromSeconds(seconds).ToString(@"" + tFormat);
        }

        [HookMethod("HasPVPDelay")]
        public bool HasPVPDelay(ulong playerId)
        {
            return DelaySettings.PvpDelay.ContainsKey(playerId);
        }

        private void SaveData()
        {
            var now = DateTime.Now;

            data.LastRunTime = now.ToString();

            try { data.CooldownBetweenEvents.RemoveAll((id, date) => date < now); } catch { }
            try { data.CooldownBetweenCancel.RemoveAll((id, date) => date < now); } catch { }
            try { data.CooldownBetweenConversion.RemoveAll((id, date) => date < now); } catch { }

            Interface.Oxide.DataFileSystem.WriteObject(Name, data);
        }

        private void LoadData()
        {
            try
            {
                data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch
            {

            }

            if (data == null)
            {
                Puts("Data file is NULL (corrupted?) and has been reset.");
                data = new StoredData();
            }
            else if (data.LastSeen == null)
            {
                Puts("Last Seen data was CORRUPTED and has been reset.");
                data.LastSeen = new Dictionary<ulong, int>();
            }

            if (data.LastRunTime != DateTime.MinValue.ToString())
            {
                DateTime lastDate;
                if (DateTime.TryParse(data.LastRunTime, out lastDate) && DateTime.Now.Subtract(lastDate).TotalHours >= 24)
                {
                    data = new StoredData();
                    Puts("Data wiped due to plugin not being loaded for {0} day(s).", DateTime.Now.Subtract(lastDate).Days);
                }
            }

            SaveData();
        }

        private void CheckData()
        {
            if (newSave)
            {
                Puts("New save detected; wiped data.");
                data = new StoredData();
                SaveData();
            }
            else if (IsEmptyMap())
            {
                Puts("New save or map detected; wiped data.");
                data = new StoredData();
                SaveData();
            }
        }

        private bool IsEmptyMap()
        {
            foreach (var b in BuildingManager.server.buildingDictionary)
            {
                if (b.Value.HasDecayEntities() && b.Value.decayEntities.ToList().Exists(de => de != null && de.OwnerID.IsSteamId()))
                {
                    return false;
                }
            }
            return true;
        }

        private Payment TrySetPayment(BasePlayer player, bool cancel)
        {
            if (player.HasPermission("abandonedbases.convert.free"))
            {
                return new Payment(player);
            }

            if (cancel ? config.Abandoned.EconomicsCancel > 0 : config.Abandoned.Economics > 0)
            {
                return TrySetEconomicsPayment(player, cancel);
            }

            if (cancel ? config.Abandoned.ServerRewardsCancel > 0 : config.Abandoned.ServerRewards > 0)
            {
                return TrySetServerRewardsPayment(player, cancel);
            }

            if (cancel ? config.Abandoned.CustomCancel.IsValid() : config.Abandoned.Custom.IsValid())
            {
                return TrySetCustomCostPayment(player, cancel);
            }

            return new Payment(player);
        }

        private Payment TrySetCustomCostPayment(BasePlayer player, bool cancel)
        {
            var options = cancel ? config.Abandoned.CustomCancel : config.Abandoned.Custom;

            foreach (var option in options)
            {
                var slots = player.inventory.FindItemsByItemID(option.Definition.itemid);
                int amount = 0;

                foreach (var slot in slots)
                {
                    if (option.Skin != 0 && slot.skin != option.Skin)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(option.Name) && slot.name != option.Name)
                    {
                        continue;
                    }

                    amount += slot.amount;

                    if (amount >= option.Amount)
                    {
                        break;
                    }
                }

                if (amount < option.Amount)
                {
                    string name = string.IsNullOrEmpty(option.Name) ? option.Shortname : option.Name;
                    Message(player, cancel ? "CustomCostFailedCancel" : "CustomCostFailed", option.Amount, name);
                    return null;
                }
            }

            return new Payment(player, 0, options);
        }

        private Payment TrySetServerRewardsPayment(BasePlayer player, bool cancel)
        {
            var cost = cancel ? config.Abandoned.ServerRewardsCancel : config.Abandoned.ServerRewards;

            if (cost > 0 && ServerRewards.CanCall())
            {
                var points = Convert.ToInt32(ServerRewards?.Call("CheckPoints", player.userID));

                if (points > 0 && points - cost >= 0)
                {
                    return new Payment(player, cost);
                }
                else
                {
                    Message(player, cancel ? "ServerRewardPointsFailedCancel" : "ServerRewardPointsFailed", cost);
                }
            }

            return null;
        }

        private Payment TrySetEconomicsPayment(BasePlayer player, bool cancel)
        {
            var cost = cancel ? config.Abandoned.EconomicsCancel : config.Abandoned.Economics;

            if (cost > 0 && Economics.CanCall())
            {
                var points = Convert.ToDouble(Economics?.Call("Balance", player.UserIDString));

                if (points > 0 && points - cost >= 0)
                {
                    return new Payment(player, cost);
                }
                else
                {
                    Message(player, cancel ? "EconomicsWithdrawFailedCancel" : "EconomicsWithdrawFailed", cost);
                }
            }

            return null;
        }

        private bool IsHogging(BasePlayer player, AbandonedBuilding abandonedBuilding, bool reply, string key)
        {
            foreach (var otherBuilding in abandonedBuilding.MyInstance.AbandonedBuildings)
            {
                if (otherBuilding.center == abandonedBuilding.center)
                {
                    continue;
                }
                if (!IsHogging(otherBuilding, player))
                {
                    continue;
                }
                if (reply)
                {
                    abandonedBuilding.TryMessage(player, key, otherBuilding.GetGrid());
                }
                return true;
            }
            return false;
        }

        private bool IsHogging(AbandonedBuilding otherBuilding, BasePlayer player)
        {
            if (otherBuilding.intruders.Contains(player)) return true;
            if (!config.Abandoned.PreventAllyHogging) return false;
            if (IsAlly(otherBuilding.currentId, player.userID)) return true;
            return otherBuilding.intruders.Exists(x => x != null && IsAlly(x.userID, player.userID));
        }

        private bool HasEventCooldown(BasePlayer player, AbandonedBuilding abandonedBuilding)
        {
            ulong userid = player.userID;
            bool reply = !abandonedBuilding.Messages.Contains(userid);
            if (config.Abandoned.PreventHogging && IsHogging(player, abandonedBuilding, reply, "HoggingFinishYourRaid"))
            {
                if (reply)
                {
                    abandonedBuilding.Messages.Add(userid);
                    abandonedBuilding.timer.Once(10f, () => abandonedBuilding.Messages.Remove(userid));
                }
                return true;
            }
            if (HasCooldown(player, "abandonedbases.noeventcooldown", data.CooldownBetweenEvents, reply, "Event Cooldown"))
            {
                if (reply)
                {
                    abandonedBuilding.Messages.Add(userid);
                    abandonedBuilding.timer.Once(10f, () => abandonedBuilding.Messages.Remove(userid));
                }
                return true;
            }
            return false;
        }

        private bool HasCooldown(BasePlayer player, string bypass, Dictionary<ulong, DateTime> cooldowns, bool reply = true, string key = "Cooldown")
        {
            if (player.HasPermission(bypass))
            {
                return false;
            }
            double cooldown = 0;
            DateTime date;
            if (cooldowns.TryGetValue(player.userID, out date))
            {
                cooldown = date.Subtract(DateTime.Now).TotalSeconds;
            }
            if (cooldown > 0)
            {
                if (reply)
                {
                    Message(player, key, Math.Ceiling(cooldown));
                }
                return true;
            }
            cooldowns.Remove(player.userID);
            return false;
        }

        private void ExcludeZones()
        {
            if (ZoneManager == null || !ZoneManager.IsLoaded)
            {
                return;
            }

            var zoneIds = ZoneManager?.Call("GetZoneIDs") as string[];

            if (zoneIds == null)
            {
                return;
            }

            excludedZones.Clear();

            foreach (string zoneId in zoneIds)
            {
                var zoneLoc = ZoneManager.Call("GetZoneLocation", zoneId);

                if (!(zoneLoc is Vector3))
                {
                    continue;
                }

                var zoneName = Convert.ToString(ZoneManager.Call("GetZoneName", zoneId));

                if (config.Inclusions.Exists(zone => zone == "*" || zone == zoneId || !string.IsNullOrEmpty(zoneName) && zoneName.Contains(zone, CompareOptions.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var radius = ZoneManager.Call("GetZoneRadius", zoneId);
                var size = ZoneManager.Call("GetZoneSize", zoneId);

                excludedZones.Add(new ZoneInfo(zoneLoc, radius, size));
            }

            if (excludedZones.Count > 0)
            {
                Puts(_("BlockedZones", null, excludedZones.Count));
            }
        }

        private bool IsInZone(Vector3 position)
        {
            foreach (var zone in excludedZones)
            {
                if (zone.IsPositionInZone(position))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsIgnoredPrefab(string shortname)
        {
            return config.Abandoned.IgnoredPrefabs.Exists(x => x.Equals(shortname, StringComparison.OrdinalIgnoreCase));
        }

        private List<ZoneInfo> excludedZones = new List<ZoneInfo>();

        public class ZoneInfo
        {
            internal Vector3 origin;
            internal Vector3 extents;
            internal Matrix4x4 m;
            internal float radius;

            public ZoneInfo(object origin, object radius, object size)
            {
                this.origin = (Vector3)origin;

                if (radius is float)
                {
                    this.radius = (float)radius;
                }

                if (size is Vector3 && !size.Equals(Vector3.zero))
                {
                    extents = (Vector3)size * 0.5f;
                    m = Matrix4x4.TRS(this.origin, new Quaternion(), Vector3.one);
                }
            }

            public bool IsPositionInZone(Vector3 point)
            {
                if (extents != Vector3.zero)
                {
                    var v = m.inverse.MultiplyPoint3x4(point);

                    return v.x <= extents.x && v.x > -extents.x && v.y <= extents.y && v.y > -extents.y && v.z <= extents.z && v.z > -extents.z;
                }
                return InRange2D(origin, point, radius);
            }

            private bool InRange2D(Vector3 a, Vector3 b, float distance)
            {
                return (new Vector3(a.x, 0f, a.z) - new Vector3(b.x, 0f, b.z)).sqrMagnitude <= distance * distance;
            }
        }

        #endregion Helpers

        #region Commands

        private void CommandCancel(IPlayer user, string command, string[] args)
        {
            if (!user.HasPermission("abandonedbases.convert.cancel") || user.IsServer)
            {
                Message(user, "No Permission");
                return;
            }

            var player = user.ToPlayer();

            if (HasCooldown(player, "abandonedbases.convert.cancel.nocooldown", data.CooldownBetweenCancel))
            {
                return;
            }

            var abandonedBuilding = AbandonedBuilding.Get(player.transform.position);

            if (abandonedBuilding == null)
            {
                Message(user, "Nothing");
                return;
            }

            if (abandonedBuilding.IsOwnerLocked && !abandonedBuilding.CanDoActions(player))
            {
                abandonedBuilding.TryEjectFromLockedBase(player);
                Message(user, "Not An Ally");
                return;
            }

            if (config.Abandoned.RequireEventFinished && !abandonedBuilding.CanBypass(player) && !abandonedBuilding.IsCompleted())
            {
                Message(user, config.Abandoned.OnlyCupboardsAreRequired ? "MustFinishCupboards" : "MustFinish");
                return;
            }

            if (config.Abandoned.CancelCooldown > 0 && !abandonedBuilding.CanBypass(player) && abandonedBuilding.HasCancelCooldown(user))
            {
                return;
            }

            Payment payment = TrySetPayment(player, true);

            if (payment == null)
            {
                return;
            }

            abandonedBuilding.RemoveExpiration();
            abandonedBuilding.SetCancelCooldown(player.userID);
            abandonedBuilding.CompleteCancelPayment(payment);
            abandonedBuilding.KillCollider();
            abandonedBuilding.DestroyMe();
            Message(user, "Cancelled");
        }

        private void CommandUnlock(IPlayer user, string command, string[] args)
        {
            if (!user.HasPermission("abandonedbases.admin") || user.IsServer)
            {
                Message(user, "No Permission");
                return;
            }

            var player = user.Object as BasePlayer;
            var abandonedBuilding = AbandonedBuilding.Get(player.transform.position);

            if (abandonedBuilding == null)
            {
                Message(user, "Nothing");
                return;
            }

            abandonedBuilding.IsAllowed.Clear();
            abandonedBuilding.IsOwnerLocked = false;
            abandonedBuilding.canReassign = true;
            abandonedBuilding.currentId = abandonedBuilding.previousId;
            abandonedBuilding.currentName = abandonedBuilding.previousName;
            abandonedBuilding.Invoke(abandonedBuilding.UpdateMarkers, 0f);

            var position = abandonedBuilding.center;
            var grid = abandonedBuilding.GetGrid();
            var text = $"{player.displayName} ({player.userID}) has unlocked the base at {position} ({grid}) from {abandonedBuilding.previousName} ({abandonedBuilding.previousId})";

            if (config.UseLogFile)
            {
                LogToFile("sar", text, this, false);
            }

            Puts(text);
            Message(user, "Unlocked");
        }

        private void CommandClaim(IPlayer user, string command, string[] args)
        {
            if (!user.HasPermission("abandonedbases.convert.claim") || user.IsServer)
            {
                Message(user, "No Permission");
                return;
            }

            var player = user.ToPlayer();

            if (HasCooldown(player, "abandonedbases.noeventcooldown", data.CooldownBetweenEvents))
            {
                return;
            }

            data.CooldownBetweenConversion[player.userID] = DateTime.Now.AddSeconds(5);

            var abandonedBuilding = AbandonedBuilding.Get(player.transform.position);

            if (abandonedBuilding == null)
            {
                Message(user, "Nothing");
                return;
            }

            if (abandonedBuilding.IsOwnerLocked && !abandonedBuilding.CanDoActions(player))
            {
                abandonedBuilding.TryEjectFromLockedBase(player);
                Message(user, "Not An Ally");
                return;
            }

            if (config.Abandoned.RequireEventFinished && !abandonedBuilding.CanBypass(player) && !abandonedBuilding.IsCompleted())
            {
                Message(user, config.Abandoned.OnlyCupboardsAreRequired ? "MustFinishCupboards" : "MustFinish");
                return;
            }

            if (config.Abandoned.RequireCupboardAccess)
            {
                var tugboat = player.GetParentEntity() as Tugboat;

                if (tugboat != null)
                {
                    var priv = GetVehiclePrivilege(tugboat.children);

                    if (priv == null || !priv.AnyAuthed() || !priv.IsAuthed(player))
                    {
                        Message(player, "Authorize");
                        return;
                    }
                }
                else
                {
                    var priv = player.GetBuildingPrivilege();

                    if (priv == null || !priv.AnyAuthed() || !priv.IsAuthed(player))
                    {
                        Message(player, "Authorize");
                        return;
                    }
                }
            }

            foreach (var entity in abandonedBuilding.entities)
            {
                if (!entity.IsKilled())
                {
                    entity.OwnerID = player.userID;
                    SetAuthOwner(player, entity);
                }
            }

            var position = abandonedBuilding.center;
            var grid = abandonedBuilding.GetGrid();
            var text = $"{player.displayName} ({player.userID}) has claimed the base at {position} ({grid}) from {abandonedBuilding.previousName} ({abandonedBuilding.previousId})";

            abandonedBuilding.SetEventCooldown(player.userID);
            abandonedBuilding.currentId = player.userID;
            abandonedBuilding.currentName = player.displayName;
            abandonedBuilding.IsClaimed = true;
            abandonedBuilding.KillCollider();
            abandonedBuilding.DestroyMe();

            Puts(text);
            Message(user, "Claimed");

            if (config.UseLogFile)
            {
                LogToFile("sar", text, this, false);
            }

            if (config.Messages.Global)
            {
                foreach (var target in BasePlayer.activePlayerList)
                {
                    Message(target, "GlobalClaim", player.displayName, grid);
                }
            }
        }

        private void SetAuthOwner(BasePlayer player, BaseEntity entity)
        {
            if (entity is PoweredRemoteControlEntity)
            {
                var rce = entity as PoweredRemoteControlEntity;
                rce.rcIdentifier = "";
                rce.SendNetworkUpdate();
                return;
            }
            if (entity is AutoTurret)
            {
                var turret = entity as AutoTurret;
                turret.authorizedPlayers.RemoveAll(auth => !IsAlly(player.userID, auth.userid));
                if (turret.IsAuthed(player)) return;
                turret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID { ShouldPool = false, userid = player.userID, username = player.displayName });
            }
            else if (entity is BuildingPrivlidge)
            {
                var priv = entity as BuildingPrivlidge;
                priv.authorizedPlayers.RemoveAll(auth => !IsAlly(player.userID, auth.userid));
                if (priv.IsAuthed(player)) return;
                priv.authorizedPlayers.Add(new ProtoBuf.PlayerNameID { ShouldPool = false, userid = player.userID, username = player.displayName });
            }
            var baseLock = entity.GetSlot(BaseEntity.Slot.Lock);
            if (baseLock == null) return;
            var codeLock = baseLock.GetComponent<CodeLock>();
            if (codeLock != null)
            {
                codeLock.OwnerID = player.userID;
                codeLock.guestPlayers.Clear();
                codeLock.whitelistPlayers.Clear();
                codeLock.whitelistPlayers.Add(player.userID);
                codeLock.SetFlag(BaseEntity.Flags.Locked, b: true);
                codeLock.SendNetworkUpdateImmediate();
            }
            var keyLock = baseLock.GetComponent<KeyLock>();
            if (keyLock != null)
            {
                keyLock.OwnerID = player.userID;
                keyLock.firstKeyCreated = false;
                keyLock.keyCode = UnityEngine.Random.Range(1, 100000);
                keyLock.SetFlag(BaseEntity.Flags.Locked, b: true);
                keyLock.SendNetworkUpdate();
            }
        }

        private void CommandConvert(IPlayer user, string command, string[] args)
        {
            if (!user.HasPermission("abandonedbases.convert"))
            {
                Message(user, "No Permission");
                return;
            }

            if (args.Length == 1)
            {
                switch (args[0].ToLower())
                {
                    case "purge":
                        {
                            if (user.HasPermission("abandonedbases.admin") && user.HasPermission("abandonedbases.purgeday"))
                            {
                                Message(user, "Purge");
                                StopAbandonedCoroutine();
                                purgeDay = true;
                            }
                            else Message(user, "No Permission");

                            return;
                        }
                    case "cancel":
                        {
                            CommandCancel(user, "cancel", args);
                            return;
                        }
                    case "claim":
                        {
                            CommandClaim(user, "claim", args);
                            return;
                        }
                    case "unlock":
                        {
                            CommandUnlock(user, "unlock", args);
                            return;
                        }
                }
            }

            if (user.IsServer)
            {
                return;
            }

            var player = user.ToPlayer();

            if (player != null && HasCooldown(player, "abandonedbases.convert.nocooldown", data.CooldownBetweenConversion))
            {
                return;
            }

            data.CooldownBetweenConversion[player.userID] = DateTime.Now.AddSeconds(5);

            var tugboat = player.GetParentEntity() as Tugboat;

            if (tugboat != null)
            {
                CommandTugboat(user, player, tugboat, command, args);
                return;
            }

            var priv = player.GetBuildingPrivilege();

            if (priv == null || !priv.AnyAuthed() || !priv.IsAuthed(player))
            {
                Message(player, "Authorize");
                return;
            }

            var building = priv?.GetBuilding();

            if (building == null || !building.HasDecayEntities())
            {
                Message(player, "NotCloseEnough");
                return;
            }

            if (AbandonedBuildings.Exists(x => x.entities.Contains(priv)))
            {
                Message(user, "Abandoned Base Already");
                return;
            }

            Payment payment = TrySetPayment(player, false);

            if (payment == null)
            {
                return;
            }

            float radius = 0f;
            if (args.Length == 1 && float.TryParse(args[0], out radius))
            {
                radius = Mathf.Clamp(radius, config.Abandoned.MinCustomSphereRadius, config.Abandoned.MaxCustomSphereRadius);
            }

            var owners = building.decayEntities.Select(x => x.OwnerID).DistinctList();

            if (!owners.Contains(player.userID))
            {
                owners.Add(player.userID);
            }

            TryConvertCompound(user, building, priv, owners, config.Abandoned.AllowPVPSAR == true, payment, radius);
        }

        private void CommandTugboat(IPlayer user, BasePlayer player, Tugboat tugboat, string command, string[] args)
        {
            var priv = GetVehiclePrivilege(tugboat.children);

            if (priv == null || !priv.AnyAuthed() || !priv.IsAuthed(player))
            {
                Message(player, "Authorize");
                return;
            }

            if (AbandonedBuildings.Exists(x => x.entities.Contains(tugboat)))
            {
                Message(user, "Abandoned Base Already");
                return;
            }

            Payment payment = TrySetPayment(player, false);

            if (payment == null)
            {
                return;
            }

            float radius = 0f;
            if (args.Length == 1 && float.TryParse(args[0], out radius))
            {
                radius = Mathf.Clamp(radius, config.Abandoned.MinCustomSphereRadius, config.Abandoned.MaxCustomSphereRadius);
            }

            var owners = tugboat.children.Select(x => x.OwnerID).DistinctList();

            if (!owners.Contains(player.userID))
            {
                owners.Add(player.userID);
            }

            TryConvertTugboat(user, tugboat, priv, owners, config.Abandoned.AllowPVPSAR == true, payment, radius);
        }

        private void CommandReport(IPlayer user, string command, string[] args)
        {
            if (!user.IsAdmin && !user.HasPermission("abandonedbases.report"))
            {
                return;
            }

            if (reportCoroutine != null)
            {
                ServerMgr.Instance.StopCoroutine(reportCoroutine);
            }

            reportCoroutine = ServerMgr.Instance.StartCoroutine(ShowDataReportRoutine());
        }

        private void CommandDebug(IPlayer user, string command, string[] args)
        {
            if (!user.HasPermission("abandonedbases.admin"))
            {
                Message(user, "No Permission");
                return;
            }

            DebugMode = !DebugMode;
            user.Reply($"Debug mode: {DebugMode}");
        }

        private static bool IsUserExcluded(ulong userid) => userid.IsSteamId() && userid.HasPermission("abandonedbases.exclude");

        private void TryForceAddUser(ulong userid, ref int count)
        {
            if (!userid.IsSteamId() || data.LastSeen.ContainsKey(userid) || IsUserExcluded(userid)) return;
            data.LastSeen[userid] = Epoch.Current;
            count++;
        }

        private void CommandStart(IPlayer user, string command, string[] args)
        {
            if (!user.HasPermission("abandonedbases.admin"))
            {
                Message(user, "No Permission");
                return;
            }

            if (args.Length > 0 && args[0] == "force_add_offline")
            {
                int count = 0;

                foreach (var entity in BaseNetworkable.serverEntities.OfType<BaseEntity>())
                {
                    if (entity is CodeLock)
                    {
                        var codeLock = entity as CodeLock;
                        codeLock.guestPlayers.ForEach(id => TryForceAddUser(id, ref count));
                        codeLock.whitelistPlayers.ForEach(id => TryForceAddUser(id, ref count));
                    }
                    else if (entity is BuildingPrivlidge)
                    {
                        var priv = entity as BuildingPrivlidge;
                        priv.authorizedPlayers.ForEach(x => TryForceAddUser(x.userid, ref count));
                    }
                    else if (entity is AutoTurret)
                    {
                        var turret = entity as AutoTurret;
                        turret.authorizedPlayers.ForEach(x => TryForceAddUser(x.userid, ref count));
                    }
                    TryForceAddUser(entity.OwnerID, ref count);
                }

                if (count > 0) SaveData();
                Message(user, "ForceAddOffline", count);
                return;
            }

            StartAbandonedRoutine(user);
        }

        #endregion Commands

        #region Configuration

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "You don't have permission to use this command!"},
                {"CommandNotAllowed", "You are not allowed to use this command right now."},
                {"Not An Ally", "You must be an ally of the raid!"},
                {"Abandoned", "An abandoned player's base is now raidable at {0}"},
                {"PVPFlag", "[<color=#FF0000>PVP</color>] "},
                {"DoomAndGloom", "{0} <color=#FF0000>You have left a PVP zone and can be attacked for another {1} seconds!</color>"},
                {"CannotTeleport", "You are not allowed to teleport from this event"},
                {"NotCloseEnough", "You are not close enough to a building"},
                {"Authorize", "You must be authorized on the cupboard to use this command"},
                {"Cooldown", "You must wait {0} seconds to use this command!"},
                {"HoggingFinishYourRaid", "<color=#FF0000>You must finish your last raid at {0} before joining another.</color>"},
                {"Event Cooldown", "You must wait {0} seconds before you can become owner of this event."},
                {"Purge", "Purge enabled. Type /sab to start converting all bases."},
                {"Tugboat Unlocked", "Tugboat has insufficient loot ({0}/{1}) and has been unlocked!"},
                {"Tugboat Player Requirement", "Tugboat does not belong to a player!"},
                {"Base Requirements", "Base does not meet requirements: {0}/{1} foundations, {2}/{3} walls. Twig does NOT qualify."},
                {"Abandoned Base Already", "This is an abandoned base already!"},
                {"Near Event Base", "This building is too close to another base event!"},
                {"Start", "Starting abandoned base event..."},
                {"CancelCooldown", "You must wait <color=#FF0000>{0}</color> seconds to cancel this event." },
                {"EconomicsWithdraw", "You have paid <color=#FFFF00>${0}</color> to convert your base!"},
                {"EconomicsWithdrawFailed", "You do not have <color=#FFFF00>${0}</color> to convert your base!"},
                {"EconomicsWithdrawCancel", "You have paid <color=#FFFF00>${0}</color> to cancel your converted base!"},
                {"EconomicsWithdrawFailedCancel", "You do not have <color=#FFFF00>${0}</color> to cancel your converted base!"},
                {"ServerRewardPointsTaken", "You have paid <color=#FFFF00>{0} RP</color> to convert your base!"},
                {"ServerRewardPointsFailed", "You do not have <color=#FFFF00>{0} RP</color> to convert your base!"},
                {"ServerRewardPointsTakenCancel", "You have paid <color=#FFFF00>{0} RP</color> to cancel your converted base!"},
                {"ServerRewardPointsFailedCancel", "You do not have <color=#FFFF00>{0} RP</color> to cancel your converted base!"},
                {"CustomCostTaken", "You have paid <color=#FFFF00>{0}</color> to convert your base!"},
                {"CustomCostFailed", "You do not have <color=#FFFF00>{0} {1}</color> to convert your base!"},
                {"CustomCostTakenCancel", "You have paid <color=#FFFF00>{0}</color> to cancel your converted base!"},
                {"CustomCostFailedCancel", "You do not have <color=#FFFF00>{0} {1}</color> to cancel your converted base!"},
                {"StartScan", "Starting manual scan for abandoned bases... this could take a while..."},
                {"ScanEnded", "Abandoned base scan finished: {0} converted, {1} deleted, and {2} skipped."},
                {"TimeLeft", "This building will become abandoned in {0}"},
                {"No privilege found", "This building is not eligible as it does not have a tool cupboard in range."},
                {"Base is active", "This building is not eligible as it has at least one active user."},
                {"Nothing", "No abandoned event found. You must use this command inside of an event."},
                {"MustFinishCupboards", "This event cannot be canceled until you have looted every TC!"},
                {"MustFinish", "This event cannot be canceled until all boxes and TC are looted!"},
                {"Cancelled", "You have cancelled this event."},
                {"Claimed", "You have claimed this base as your own."},
                {"Unlocked", "You have reset the event status of this base."},
                {"GlobalClaim", "{0} has claimed the base in {1}."},
                {"OnEventCompletedLocalOwned", "<color=#FFFF00>{0}</color> has completed the abandoned event owned by <color=#FFFF00>{1}</color> in <color=#FFFF00>{2}</color>!"},
                {"OnEventCompletedLocal", "<color=#FFFF00>{0}</color> has completed the abandoned event in <color=#FFFF00>{1}</color>!"},
                {"OnEventCompleted", "You have completed the event."},
                {"OnEventCompletedClaim", "You have completed the event. You may type <color=#FF0000>/sar claim</color> to take over this base."},
                {"OnEventCompletedCancel", "You have completed the event. You may type <color=#FF0000>/sar cancel</color> to end the event."},
                {"OnEventCompletedClaimCancel", "You have completed the event. You may type <color=#FF0000>/sar claim</color> to take over this base, or <color=#FF0000>/sar cancel</color> to end the event."},
                {"OnBuiltPrivilege", "You cannot claim a base by placing a tool cupboard. You must type /sar claim."},
                {"OnBuiltPrivilegeEx", "You cannot claim a base by placing a tool cupboard. You must type /sar claim, when the raid is fully looted."},
                {"OnBuiltPrivilegeNone", "You cannot claim a base by placing a tool cupboard because you do not have the required permissions to do so."},
                {"OnPlayerExit", "<color=#FF0000>You have left a raidable PVP base!</color>"},
                {"OnPlayerExitPVE", "<color=#FF0000>You have left a raidable PVE base!</color>"},
                {"OnPlayerEntered", "<color=#FF0000>You have entered a raidable PVP base!</color>"},
                {"OnPlayerEnteredPVE", "<color=#FF0000>You have entered a raidable PVE base!</color>"},
                {"ForceAddOffline", "{0} players have been added to the database."},
                {"BlockedZones", "Blocked spawn points in {0} zones."},
                {"SkillTreeXP", "You have received <color=#FFFF00>{0} XP</color> from this event!"},
                {"ServerRewardPoints", "You have received <color=#FFFF00>{0} RP</color> from this event!"},
                {"EconomicsDeposit", "You have received <color=#FFFF00>${0}</color> from this event!"},
                {"CannotBuild", "<color=#FF0000>You are not allowed to build here!</color>"},
                {"CannotBuildLadders", "<color=#FF0000>You are not allowed to build ladders here!</color>"},
                {"FormatOnline", "<color=#c70000>{index}</color>. <color=#A8A7AE>{username}</color> (<color=#A8A7AE>{userid}</color>) is <color=#00FF00>online</color>" },
                {"FormatLastSeen", "<color=#c70000>{index}</color>. <color=#A8A7AE>{username}</color> (<color=#A8A7AE>{userid}</color>) was last seen {time} ago" },
                {"FormatLastSeenUnknown", "<color=#c70000>{index}</color>. <color=#A8A7AE>{username}</color> (<color=#A8A7AE>{userid}</color>)" },
                {"Engine failure", "<color=#c70000>You are not allowed to start the engine during this event!</color>" },
                {"Drawn Authed","<size=22>AUTHED</size>"},
                {"Drawn Entity","<size=22>OWNER</size>"},
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "У вас нет привилегии для использовании этой команды!"},
                {"CommandNotAllowed", "You are not allowed to use this command right now."},
                {"Not An Ally", "Вы должны быть союзником рейда!"},
                {"Abandoned", "Заброшенная база игрока теперь доступна для рейда. Квадрат: <color=#ff8833>{0}</color>"},
                {"PVPFlag", "[<color=#FF0000>ПВП</color>] "},
                {"DoomAndGloom", "{0} <color=#FF0000>Внимание!</color> Вы покинули зону ПВП и сможете получать урон в течении {1} сек!"},
                {"CannotTeleport", "Вы не можете телепортироваться в зоне заброшенной базы"},
                {"NotCloseEnough", "Вы находитесь недостаточно близко к зданию"},
                {"Authorize", "Вы должны быть авторизованны в шкафу для использования этой команды"},
                {"Cooldown", "Вы должны подождать {0} секунд, чтобы использовать эту команду!"},
                {"Event Cooldown", "Вы должны подождать {0} секунд, прежде чем станете владельцем этого события."},
                {"Purge", "Очистка включена. Введите /sab, чтобы начать преобразование всех баз."},
                {"Tugboat Unlocked", "Tugboat has insufficient loot and has been unlocked!"},
                {"Tugboat Player Requirement", "Tugboat does not belong to a player!"},
                {"Base Requirements", "Основание не соответствует требованиям: {0}/{1} фундаменты, {2}/{3} стены. Солома НЕ подходит."},
                {"Near Event Base", "Это здание слишком близко к другой базе!"},
                {"Start", "Запуск рейда заброшенных баз..."},
                {"EconomicsWithdraw", "Вы заплатили <color=#FFFF00>${0}</color> за конвертацию вашей базы!"},
                {"EconomicsWithdrawFailed", "У вас нет <color=#FFFF00>${0}</color> для преобразования вашей базы!"},
                {"ServerRewardPointsTaken", "Вы заплатили <color=#FFFF00>{0} RP</color> за конвертацию вашей базы!"},
                {"ServerRewardPointsFailed", "У вас нет <color=#FFFF00>{0} RP</color> для конвертации вашей базы!"},
                {"StartScan", "Запускаю ручное сканирование на предмет заброшенных баз... это может занять некоторое время..."},
                {"ScanEnded", "Сканирование заброшенной базы завершено: {0} преобразовано, {1} удалено и {2} пропущено."},
                {"TimeLeft", "Это здание станет заброшенным в {0}"},
                {"No privilege found", "Это здание не подходит, так как в нем нет шкафа с инструментами."},
                {"Base is active", "Это здание не подходит, так как в нем есть по крайней мере один активный пользователь."},
                {"Nothing", "Заброшенное событие не найдено. Вы должны использовать эту команду внутри события."},
                {"MustFinishCupboards", "This event cannot be canceled until you have looted every TC first!"},
                {"MustFinish", "Это событие не может быть отменено до тех пор, пока все ящики и шкаф не будут разграблены!"},
                {"Cancelled", "Вы отменили это мероприятие."},
                {"Claimed", "Вы заявили, что эта база принадлежит вам."},
                {"Unlocked", "Вы сбросили статус события этой базы."},
                {"GlobalClaim", "{0} забрал базу в {1}."},
                {"OnEventCompletedLocalOwned", "<color=#FFFF00>{0}</color> завершил заброшенное мероприятие, принадлежащее <color=#FFFF00>{1}</color> в <color=#FFFF00>{2}</color>!"},
                {"OnEventCompletedLocal", "<color=#FFFF00>{0}</color> завершил заброшенное мероприятие в <color=#FFFF00>{1}</color>!"},
                {"OnEventCompleted", "Вы завершили событие."},
                {"OnEventCompletedClaim", "Вы завершили событие. Вы можете набрать <color=#FF0000>/sar claim</color>, чтобы завладеть этой базой."},
                {"OnEventCompletedCancel", "Вы завершили событие. Вы можете ввести <color=#FF0000>/sar cancel</color>, чтобы завершить событие."},
                {"OnEventCompletedClaimCancel", "Вы завершили событие. Вы можете ввести <color=#FF0000>/sar claim</color>, чтобы захватить эту базу, или <color=#FF0000>/sar cancel</color>, чтобы завершить событие."},
                {"OnBuiltPrivilege", "Вы не можете претендовать на базу, разместив шкаф для инструментов. Вы должны ввести /sar для отправки заявки."},
                {"OnBuiltPrivilegeEx", "Вы не можете претендовать на базу, разместив шкаф для инструментов. Вы должны ввести /sar для отправки заявки, когда рейд будет полностью разграблен."},
                {"OnBuiltPrivilegeNone", "Вы не можете претендовать на базу, разместив шкаф для инструментов, поскольку у вас нет необходимых разрешений для этого."},
                {"OnPlayerExit", "<color=#FF0000>Внимание!</color> Вы покинули базу PVP с возможностью рейда!"},
                {"OnPlayerExitPVE", "<color=#FF0000>Внимание!</color> Вы покинули базу PVE, доступную для рейдов!"},
                {"OnPlayerEntered", "<color=#FF0000>Внимание!</color> Вы вошли на базу PVP, доступную для рейдов!"},
                {"OnPlayerEnteredPVE", "<color=#FF0000>Внимание!</color> Вы вошли на базу PVE, доступную для рейдов!"},
                {"BlockedZones", "Заблокированные точки появления {0} зон."},
                {"CannotBuild", "<color=#FF0000>Вам не разрешается строить здесь</color>"},
                {"CannotBuildLadders", "<color=#FF0000>You are not allowed to build ladders here!</color>"},
                {"FormatOnline", "<color=#c70000>{index}</color>. <color=#A8A7AE>{username}</color> (<color=#A8A7AE>{userid}</color>) is <color=#00FF00>online</color>" },
                {"FormatLastSeen", "<color=#c70000>{index}</color>. <color=#A8A7AE>{username}</color> (<color=#A8A7AE>{userid}</color>) was last seen {time} ago" },
                {"FormatLastSeenUnknown", "<color=#c70000>{index}</color>. <color=#A8A7AE>{username}</color> (<color=#A8A7AE>{userid}</color>)" },
                {"Engine failure", "<color=#c70000>You are not allowed to start the engine during this event!</color>" },
                {"Drawn Authed","<size=22>AUTHED</size>"},
                {"Drawn Entity","<size=22>OWNER</size>"},
            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"No Permission", "No tienes permiso para usar este comando!"},
                {"CommandNotAllowed", "You are not allowed to use this command right now."},
                {"Not An Ally", "Debes ser un aliado de la redada!"},
                {"Abandoned", "Se ha convertido una base en {0}"},
                {"PVPFlag", "[<color=#FF0000>PVP</color>] "},
                {"DoomAndGloom", "{0} <color=#FF0000>Has dejado una zona PVP y podrás ser atacado durante {1} segundos!</color>"},
                {"CannotTeleport", "No tienes permiso para teleport desde este evento"},
                {"NotCloseEnough", "No estas lo suficientemente cerca para construir"},
                {"Authorize", "Debes de estar autorizado en el armario para usar este comando"},
                {"Cooldown", "Debes esperar {0} segundos para usar este comando"},
                {"Event Cooldown", "Debes esperar {0} segundos antes de poder ser el propietario de este evento."},
                {"Purge", "Purga Activada. Escribe /sab para empezar la conversión de todas las bases."},
                {"Tugboat Unlocked", "Tugboat has insufficient loot and has been unlocked!"},
                {"Tugboat Player Requirement", "Tugboat does not belong to a player!"},
                {"Base Requirements", "La base no cumple los requisitos: {0}/{1} cimientos, {2}/{3} muros. La paja no cuenta...."},
                {"Near Event Base", "Este edificio está demasiado cerca de otra base!"},
                {"Start", "Iniciando el evento de bases abandonadas"},
                {"EconomicsWithdraw", "Has pagado <color=#FFFF00>${0}</color> para convertir tu base!"},
                {"EconomicsWithdrawFailed", "Tu no tienes <color=#FFFF00>${0}</color> para convertir tu base!"},
                {"ServerRewardPointsTaken", "Has pagado <color=#FFFF00>{0} RP</color> para convertir tu base!"},
                {"ServerRewardPointsFailed", "Tu no tienes <color=#FFFF00>{0} RP</color> para convertir tu base!"},
                {"StartScan", "Iniciando el escaneo para las bases abandonadas... esto puede tardar un poco, paciencia..."},
                {"ScanEnded", "Escaneo de bases abandonadas finalizado: {0} convertida, {1} borrada, and {2} ignorada."},
                {"TimeLeft", "Este edificio se convertirá en base abandonada en {0}"},
                {"No privilege found", "Este edificio no cumple los requisitos, no tiene un armario en rango."},
                {"Base is active", "Este edificio no cumple los requisitos, tiene al menos un usuario activo"},
                {"Nothing", "No se ha encontrado ningun evento de abandono. Debes usar este comando dentro de un evento"},
                {"MustFinishCupboards", "This event cannot be canceled until you have looted every TC first!"},
                {"MustFinish", "Este evento no puede ser cancelado hasta que todas las cajas y armario se hayan looteado!"},
                {"Cancelled", "Has cancelado este evento."},
                {"Claimed", "Has reclamado esta base como tuya."},
                {"Unlocked", "Has restablecido el estado del evento de esta base."},
                {"GlobalClaim", "{0} ha reclamado la base en {1}."},
                {"OnEventCompletedLocalOwned", "<color=#FFFF00>{0}</color> ha completado el evento abandonado propiedad de <color=#FFFF00>{1}</color> en <color=#FFFF00>{2}</color>!"},
                {"OnEventCompletedLocal", "<color=#FFFF00>{0}</color> ha completado el evento abandonado en <color=#FFFF00>{1}</color>!"},
                {"OnEventCompleted", "Has completado el evento."},
                {"OnEventCompletedClaim", "Has completado el evento. Puede escribir <color=#FF0000>/sar claim</color> para hacerse cargo de esta base."},
                {"OnEventCompletedCancel", "Has completado el evento. Puede escribir <color=#FF0000>/sar cancel</color> para terminar el evento."},
                {"OnEventCompletedClaimCancel", "Has completado el evento. Puede escribir <color=#FF0000>/sar claim</color> para hacerse cargo de esta base, o <color=#FF0000>/sar cancel</color> para terminar el evento."},
                {"OnBuiltPrivilege", "Tu no puedes reclamar una base. Debes teclear /sar para reclamar."},
                {"OnBuiltPrivilegeEx", "Tu no puedes reclamar una base. Debes teclear /sar para reclamar, cuando la raid haya sido saqueada completamente."},
                {"OnBuiltPrivilegeNone", "Tu no puedes reclamar una base porque no tienes los permisos requeridos para hacerlo."},
                {"OnPlayerExit", "<color=#FF0000>Has salido de una base raidable PVP !</color>"},
                {"OnPlayerExitPVE", "<color=#FF0000>Has salido de una base raidable PVE !</color>"},
                {"OnPlayerEntered", "<color=#FF0000>Has entrado en una base PVP!</color>"},
                {"OnPlayerEnteredPVE", "<color=#FF0000>Has entrado en una base PVE </color>"},
                {"BlockedZones", "Puntos de generación bloqueados en {0} zonas."},
                {"CannotBuild", "<color=#FF0000>No se le permite construir aquí</color>"},
                {"FormatOnline", "<color=#c70000>{index}</color>. <color=#A8A7AE>{username}</color> (<color=#A8A7AE>{userid}</color>) is <color=#00FF00>online</color>" },
                {"FormatLastSeen", "<color=#c70000>{index}</color>. <color=#A8A7AE>{username}</color> (<color=#A8A7AE>{userid}</color>) was last seen {time} ago" },
                {"FormatLastSeenUnknown", "<color=#c70000>{index}</color>. <color=#A8A7AE>{username}</color> (<color=#A8A7AE>{userid}</color>)" },
                {"Engine failure", "<color=#c70000>You are not allowed to start the engine during this event!</color>" },
                {"Drawn Authed","<size=22>AUTHED</size>"},
                {"Drawn Entity","<size=22>OWNER</size>"},
            }, this, "es");
        }

        private static void Message(IPlayer user, string key, params object[] args)
        {
            if (user == null)
            {
                return;
            }

            if (user.Object is BasePlayer)
            {
                Message(user.ToPlayer(), key, args);
            }
            else user.Message(_(key, user.Id, args));
        }

        private static void Message(BasePlayer player, string key, params object[] args)
        {
            if (player == null || Instance == null)
            {
                return;
            }

            string message = _(key, player.UserIDString, args);

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            if (config.Messages.Message)
            {
                Instance.Player.Message(player, message, config.ChatID);
            }

            if (config.Messages.AA.Enabled || config.Messages.NotifyType != -1)
            {
                List<Notification> notifications;
                if (!Instance._notifications.TryGetValue(player.userID, out notifications))
                {
                    Instance._notifications[player.userID] = notifications = new List<Notification>();
                }

                notifications.Add(new Notification
                {
                    player = player,
                    messageEx = message
                });
            }
        }

        private void CheckNotifications()
        {
            if (_notifications.Count > 0)
            {
                foreach (var entry in _notifications.ToList())
                {
                    var notification = entry.Value.ElementAt(0);

                    SendNotification(notification);

                    entry.Value.Remove(notification);

                    if (entry.Value.Count == 0)
                    {
                        _notifications.Remove(entry.Key);
                    }
                }
            }
        }

        private void SendNotification(Notification notification)
        {
            if (!notification.player.IsReallyConnected())
            {
                return;
            }

            if (config.Messages.AA.Enabled && AdvancedAlerts.CanCall())
            {
                AdvancedAlerts?.Call("SpawnAlert", notification.player, "hook", notification.messageEx, config.Messages.AA.AnchorMin, config.Messages.AA.AnchorMax, config.Messages.AA.Time);
            }

            if (config.Messages.NotifyType != -1 && Notify.CanCall())
            {
                Notify?.Call("SendNotify", notification.player, config.Messages.NotifyType, notification.messageEx);
            }
        }

        private static Configuration config;

        private static List<PurgeSettings> DefaultPurgeSettings()
        {
            return new List<PurgeSettings>
            {
                new PurgeSettings
                {
                    LifetimeRaw = "7",
                    Permission = "abandonedbases.vip"
                },
                new PurgeSettings
                {
                    LifetimeRaw = "5",
                    Permission = "abandonedbases.veteran"
                },
                new PurgeSettings
                {
                    LifetimeRaw = "3",
                    Permission = "abandonedbases.basic"
                },
            };
        }

        private static List<string> DefaultBlacklistCommands()
        {
            return new List<string> { "command1", "command2", "command3" };
        }

        public class BuildingOptionsAutoTurrets
        {
            [JsonProperty(PropertyName = "Enabled")]
            public bool Enabled { get; set; }

            [JsonProperty(PropertyName = "Aim Cone")]
            public float AimCone { get; set; } = 5f;

            [JsonProperty(PropertyName = "Ammo")]
            public int Ammo { get; set; } = 256;

            [JsonProperty(PropertyName = "Infinite Ammo")]
            public bool InfiniteAmmo { get; set; }

            [JsonProperty(PropertyName = "Minimum Damage Modifier")]
            public float Min { get; set; } = 1f;

            [JsonProperty(PropertyName = "Maximum Damage Modifier")]
            public float Max { get; set; } = 1f;

            [JsonProperty(PropertyName = "Start Health")]
            public float Health { get; set; } = 1000f;

            [JsonProperty(PropertyName = "Sight Range")]
            public float SightRange { get; set; } = 30f;

            [JsonProperty(PropertyName = "Double Sight Range When Shot")]
            public bool AutoAdjust { get; set; }

            [JsonProperty(PropertyName = "Set Hostile (False = Do Not Set Any Mode)")]
            public bool Hostile { get; set; } = true;

            [JsonProperty(PropertyName = "Has Power")]
            public bool HasPower { get; set; }

            [JsonProperty(PropertyName = "Requires Power Source")]
            public bool RequiresPower { get; set; }

            [JsonProperty(PropertyName = "Remove Equipped Weapon")]
            public bool RemoveWeapon { get; set; }

            [JsonProperty(PropertyName = "Random Weapons To Equip When Unequipped", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Shortnames { get; set; } = new List<string> { "rifle.ak" };
        }

        public class AbandonedDisabledSettings
        {
            [JsonProperty(PropertyName = "Enabled")]
            public bool Enabled { get; set; }

            [JsonProperty(PropertyName = "Start Time")]
            public string Start { get; set; }

            [JsonProperty(PropertyName = "End Time")]
            public string End { get; set; }

            public AbandonedDisabledSettings() { }

            public AbandonedDisabledSettings(string start, string end)
            {
                Start = start;
                End = end;
            }

            public bool CanBlockAutomaticConversion()
            {
                if (!Enabled)
                {
                    return false;
                }
                DateTime start;
                if (!DateTime.TryParse(Start, out start))
                {
                    Puts("Invalid datetime format in config: {0}", Start);
                    Enabled = false;
                    return false;
                }
                DateTime end;
                if (!DateTime.TryParse(End, out end))
                {
                    Puts("Invalid datetime format in config: {0}", End);
                    Enabled = false;
                    return false;
                }
                return DateTime.Now >= start && DateTime.Now <= end;
            }
        }

        public static List<AbandonedDisabledSettings> DefaultDisabledSettings
        {
            get
            {
                return new List<AbandonedDisabledSettings>
                {
                    new AbandonedDisabledSettings("00:00", "12:00"),
                    new AbandonedDisabledSettings("12:00", "00:00"),
                };
            }
        }

        public class BuildingBlockOptions
        {
            [JsonProperty(PropertyName = "Twigs")]
            public float Twigs { get; set; } = 10f;

            [JsonProperty(PropertyName = "Wooden")]
            public float Wooden { get; set; } = 250f;

            [JsonProperty(PropertyName = "Stone")]
            public float Stone { get; set; } = 500f;

            [JsonProperty(PropertyName = "Metal")]
            public float Metal { get; set; } = 1000f;

            [JsonProperty(PropertyName = "HQM")]
            public float HQM { get; set; } = 2000f;

            //public float GetMaxHealth(BuildingBlock __instance)
            //{
            //    float multiplier;
            //    if (!config.Abandoned.BlocksMultiplier.TryGetValue(__instance.grade, out multiplier))
            //    {
            //        multiplier = __instance.currentGrade.construction.healthMultiplier;
            //    }
            //    switch (__instance.grade)
            //    {
            //        case BuildingGrade.Enum.Twigs: return Twigs * multiplier;
            //        case BuildingGrade.Enum.Wood: return Wooden * multiplier;
            //        case BuildingGrade.Enum.Stone: return Stone * multiplier;
            //        case BuildingGrade.Enum.Metal: return Metal * multiplier;
            //        case BuildingGrade.Enum.TopTier: return HQM * multiplier;
            //    }
            //    return __instance.health;
            //}
        }

        public static Dictionary<string, float> DefaultEntityOptions
        {
            get
            {
                return new Dictionary<string, float>
                {
                    ["door.hinged.wood"] = 200f,
                    ["door.hinged.metal"] = 250f,
                    ["door.hinged.toptier"] = 800f,
                    ["door.double.hinged.wood"] = 200f,
                    ["door.double.hinged.metal"] = 250f,
                    ["door.double.hinged.toptier"] = 800f,
                    ["door.hinged.industrial.a"] = 250f,
                    ["door.hinged.industrial.d"] = 250f,
                    ["floor.grill"] = 250f,
                    ["floor.triangle.grill"] = 250f,
                    ["floor.triangle.ladder"] = 250f,
                    ["gates.external.high.wood"] = 500f,
                    ["gates.external.high.stone"] = 500f,
                    ["shutter.metal.embrasure.a"] = 500f,
                    ["shutter.metal.embrasure.b"] = 500f,
                    ["shutter.wood.a"] = 200f,
                    ["wall.external.high.ice"] = 500f,
                    ["wall.external.high.stone"] = 500f,
                    ["wall.external.high.wood"] = 500f,
                    ["wall.frame.cell"] = 300f,
                    ["wall.frame.fence"] = 100f,
                    ["wall.frame.garagedoor"] = 600f,
                    ["wall.frame.netting"] = 100f,
                    ["wall.frame.shopfront"] = 500f,
                    ["wall.frame.shopfront.metal"] = 750f,
                    ["wall.window.bars.wood"] = 250f,
                    ["wall.window.bars.metal"] = 500f,
                    ["wall.window.bars.toptier"] = 500f,
                };
            }
        }

        public static Dictionary<string, BuildingBlockOptions> DefaultBuildingBlockOptions
        {
            get
            {
                return new Dictionary<string, BuildingBlockOptions>
                {
                    ["block.stair.spiral"] = new BuildingBlockOptions(),
                    ["block.stair.spiral.triangle"] = new BuildingBlockOptions(),
                    ["block.stair.ushape"] = new BuildingBlockOptions(),
                    ["block.stair.lshape"] = new BuildingBlockOptions(),
                    ["floor"] = new BuildingBlockOptions(),
                    ["floor.triangle.frame"] = new BuildingBlockOptions(),
                    ["floor.triangle"] = new BuildingBlockOptions(),
                    ["floor.frame"] = new BuildingBlockOptions(),
                    ["foundation.steps"] = new BuildingBlockOptions(),
                    ["foundation.triangle"] = new BuildingBlockOptions(),
                    ["foundation"] = new BuildingBlockOptions(),
                    ["ramp"] = new BuildingBlockOptions(),
                    ["roof"] = new BuildingBlockOptions(),
                    ["roof.triangle"] = new BuildingBlockOptions(),
                    ["wall.low"] = new BuildingBlockOptions(),
                    ["wall.half"] = new BuildingBlockOptions(),
                    ["wall.frame"] = new BuildingBlockOptions(),
                    ["wall.window"] = new BuildingBlockOptions(),
                    ["wall.doorway"] = new BuildingBlockOptions(),
                    ["wall"] = new BuildingBlockOptions(),
                };
            }
        }

        public static Dictionary<BuildingGrade.Enum, float> DefaultBuildingBlockMultiplierOptions
        {
            get
            {
                return new Dictionary<BuildingGrade.Enum, float>
                {
                    [BuildingGrade.Enum.Twigs] = 1f,
                    [BuildingGrade.Enum.Wood] = 1f,
                    [BuildingGrade.Enum.Stone] = 1f,
                    [BuildingGrade.Enum.Metal] = 1f,
                    [BuildingGrade.Enum.TopTier] = 1f,
                };
            }
        }

        public static List<CustomCostOptions> DefaultCustomCost
        {
            get
            {
                return new List<CustomCostOptions>
                {
                    new CustomCostOptions(0)
                };
            }
        }

        public class CustomCostOptions
        {
            [JsonProperty(PropertyName = "Item Shortname")]
            public string Shortname { get; set; } = "scrap";

            [JsonProperty(PropertyName = "Item Name")]
            public string Name { get; set; } = null;

            [JsonProperty(PropertyName = "Amount")]
            public int Amount { get; set; }

            [JsonProperty(PropertyName = "Skin")]
            public ulong Skin { get; set; }

            [JsonIgnore]
            public ItemDefinition Definition { get; set; }

            public bool IsReallyValid()
            {
                if (!string.IsNullOrEmpty(Shortname) && Amount > 0)
                {
                    if (Definition == null)
                    {
                        Definition = ItemManager.FindItemDefinition(Shortname);
                    }

                    return Definition != null;
                }

                return false;
            }

            public CustomCostOptions(int amount)
            {
                Amount = amount;
            }
        }

        public class AbandonedSettings
        {
            [JsonProperty(PropertyName = "Automatic Conversions Disabled Between These Times", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<AbandonedDisabledSettings> Disabled { get; set; } = DefaultDisabledSettings;

            [JsonProperty(PropertyName = "Auto Turrets")]
            public BuildingOptionsAutoTurrets AutoTurret { get; set; } = new BuildingOptionsAutoTurrets();

            [JsonProperty(PropertyName = "Rewards")]
            public Rewards Rewards { get; set; } = new Rewards();

            [JsonProperty(PropertyName = "Blacklisted Commands", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> BlacklistedCommands { get; set; } = DefaultBlacklistCommands();

            [JsonProperty(PropertyName = "Entities Not Allowed To Be Picked Up", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> BlacklistedPickupItems { get; set; } = new List<string>();

            [JsonProperty(PropertyName = "Ignored Prefabs", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> IgnoredPrefabs { get; set; } = new List<string> { "sleepingbag_leather_deployed", "bed_deployed" };

            [JsonProperty(PropertyName = "BotSpawn Profile Names", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> BotSpawnProfileNames { get; set; } = new List<string> { "profile_name_1", "profile_name_2" };

            [JsonProperty(PropertyName = "Tugboats")]
            public TugboatSettings Tugboats { get; set; } = new TugboatSettings();

            [JsonProperty(PropertyName = "Custom Cost To Manually Convert (0 = disabled)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<CustomCostOptions> Custom { get; set; } = DefaultCustomCost;

            [JsonProperty(PropertyName = "Custom Cost To Cancel Conversion (0 = disabled)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<CustomCostOptions> CustomCancel { get; set; } = DefaultCustomCost;

            [JsonProperty(PropertyName = "Economics Cost To Manually Convert (0 = disabled)")]
            public double Economics { get; set; }

            [JsonProperty(PropertyName = "Economics Cost To Cancel Conversion (0 = disabled)")]
            public double EconomicsCancel { get; set; }

            [JsonProperty(PropertyName = "ServerRewards Cost To Manually Convert (0 = disabled)")]
            public int ServerRewards { get; set; }

            [JsonProperty(PropertyName = "ServerRewards Cost To Cancel Conversion (0 = disabled)")]
            public int ServerRewardsCancel { get; set; }

            /*[JsonProperty(PropertyName = "Building Block Health", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, BuildingBlockOptions> Blocks { get; set; } = DefaultBuildingBlockOptions;

            [JsonProperty(PropertyName = "Building Block Health Multiplier", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<BuildingGrade.Enum, float> BlocksMultiplier { get; set; } = DefaultBuildingBlockMultiplierOptions;

            [JsonProperty(PropertyName = "Other Entity Health", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<string, float> Other { get; set; } = DefaultEntityOptions;*/

            [JsonProperty(PropertyName = "Allow Teleport")]
            public bool AllowTeleport { get; set; }

            [JsonProperty(PropertyName = "Allow PVP")]
            public bool AllowPVP { get; set; } = true;

            [JsonProperty(PropertyName = "Allow PVP (when manually converted with SAR command)")]
            public bool? AllowPVPSAR { get; set; } = null;

            [JsonProperty(PropertyName = "Allow PVP (when manually converted with attack permission)")]
            public bool? AllowPVPAttack { get; set; } = null;

            [JsonProperty(PropertyName = "Allow Players To Build")]
            public bool AllowBuilding { get; set; } = true;

            [JsonProperty(PropertyName = "Allow Players To Build Ladders")]
            public bool AllowLadders { get; set; } = true;

            [JsonProperty(PropertyName = "Allow Players To Use MLRS")]
            public bool MLRS { get; set; } = true;

            [JsonProperty(PropertyName = "Minimum Required Players Online")]
            public int MinimumOnlinePlayers { get; set; } = 1;

            [JsonProperty(PropertyName = "Backpacks Plugin Drops At PVE Bases")]
            public bool BackpacksPVE { get; set; }

            [JsonProperty(PropertyName = "Backpacks Plugin Drops At PVP Bases")]
            public bool BackpacksPVP { get; set; }

            [JsonProperty(PropertyName = "Block Damage From Outside To Base")]
            public bool BlockOutsideDamage { get; set; } = true;

            [JsonProperty(PropertyName = "Block RestoreUponDeath Plugin For PVP Bases")]
            public bool BlockRestorePVP { get; set; }

            [JsonProperty(PropertyName = "Block RestoreUponDeath Plugin For PVE Bases")]
            public bool BlockRestorePVE { get; set; }

            [JsonProperty(PropertyName = "Block RestoreUponDeath Plugin For Sleepers")]
            public bool BlockRestoreSleepers { get; set; } = true;

            [JsonProperty(PropertyName = "Building Blocks Are Immune To Damage")]
            public bool BlocksImmune { get; set; }

            [JsonProperty(PropertyName = "Building Blocks Are Immune To Damage (Twig Only)")]
            public bool TwigImmune { get; set; }

            [JsonProperty(PropertyName = "Prevent Players From Hogging Raids")]
            public bool PreventHogging { get; set; } = true;

            [JsonProperty(PropertyName = "Prevent Ally From Hogging Raids")]
            public bool PreventAllyHogging { get; set; } = true;

            [JsonProperty(PropertyName = "Cooldown Between Conversions")]
            public float CooldownBetweenConversion { get; set; } = 3600f;

            [JsonProperty(PropertyName = "Cooldown Between Cancel")]
            public float CooldownBetweenCancel { get; set; } = 3600f;

            [JsonProperty(PropertyName = "Cooldown Between Events")]
            public float CooldownBetweenEvents { get; set; } = 3600f;

            [JsonProperty(PropertyName = "Cooldown Between Events Blocks Damage")]
            public bool CooldownBetweenEventsBlocksDamage { get; set; } = true;

            [JsonProperty(PropertyName = "Marker Name (Minutes)")]
            public string MarkerName { get; set; } = "Abandoned Player Base [{time}m]";

            [JsonProperty(PropertyName = "Marker Name (Seconds)")]
            public string MarkerNameSeconds { get; set; } = "Abandoned Player Base [{time}s]";

            [JsonProperty(PropertyName = "Marker Format With Owner Name")]
            public string MarkerNameOwnerFormat { get; set; } = "[Owner] {0} {1}";

            [JsonProperty(PropertyName = "Marker Format With Raider Name")]
            public string MarkerNameRaiderFormat { get; set; } = "[Raider] {0} {1}";

            [JsonProperty(PropertyName = "Show Owners Name On Map Marker")]
            public bool ShowOwnersName { get; set; }

            [JsonProperty(PropertyName = "Foundations Required")]
            public int FoundationLimit { get; set; } = 4;

            [JsonProperty(PropertyName = "Walls Required")]
            public int WallLimit { get; set; } = 3;

            [JsonProperty(PropertyName = "Include Twig Structures")]
            public bool Twig { get; set; }

            [JsonProperty(PropertyName = "Sphere Amount")]
            public int SphereAmount { get; set; } = 10;

            [JsonProperty(PropertyName = "Sphere Radius")]
            public float SphereRadius { get; set; } = 50f;

            [JsonProperty(PropertyName = "Use Dynamic Sphere Radius")]
            public bool Dynamic { get; set; }

            [JsonProperty(PropertyName = "Max Dynamic Radius")]
            public float MaxDynamicRadius { get; set; } = 75f;

            [JsonProperty(PropertyName = "Padding Added Onto Dynamic Radius")]
            public float Padding { get; set; } = 9f;

            [JsonProperty(PropertyName = "Min Custom Sphere Radius")]
            public float MinCustomSphereRadius { get; set; } = 25f;

            [JsonProperty(PropertyName = "Max Custom Sphere Radius")]
            public float MaxCustomSphereRadius { get; set; } = 75f;

            [JsonProperty(PropertyName = "Players Cannot Loot Wounded Players")]
            public bool CannotLootWoundedPlayers { get; set; } = true;

            [JsonProperty(PropertyName = "Seconds Until Event Can Be Canceled")]
            public float CancelCooldown { get; set; }

            [JsonProperty(PropertyName = "PVP Delay")]
            public float PVPDelay { get; set; } = 15f;

            [JsonProperty(PropertyName = "Despawn Timer")]
            public float DespawnTime { get; set; } = 1800f;

            [JsonProperty(PropertyName = "Reset Despawn Timer When Base Is Attacked")]
            public bool Reset { get; set; } = true;

            [JsonProperty(PropertyName = "Do Not Destroy Base When Despawn Timer Expires")]
            public bool DoNotDestroy { get; set; }

            [JsonProperty(PropertyName = "Do Not Destroy Manually Converted Base When Despawn Timer Expires")]
            public bool DoNotDestroyManual { get; set; }

            [JsonProperty(PropertyName = "Backpacks Can Be Opened")]
            public bool Backpacks { get; set; } = true;

            [JsonProperty(PropertyName = "Corpses Can Be Looted By Anyone")]
            public bool CorpsesLooted { get; set; } = true;

            [JsonProperty(PropertyName = "Time To Wait Between Spawns")]
            public float WaitTime { get; set; } = 15f;

            [JsonProperty(PropertyName = "Use Map Marker For Automatic")]
            public bool AutoMarkers { get; set; } = true;

            [JsonProperty(PropertyName = "Use Map Marker For Manual")]
            public bool ManualMarkers { get; set; } = true;

            [JsonProperty(PropertyName = "Map Marker Radius")]
            public float MarkerRadius { get; set; } = 0.25f;

            [JsonProperty(PropertyName = "Map Marker Radius (Map Size 3600 Or Less)")]
            public float MarkerSubRadius { get; set; } = 0.4f;

            [JsonProperty(PropertyName = "Allow Manually Converted Bases To Be Claimed")]
            public bool AllowManualClaims { get; set; }

            [JsonProperty(PropertyName = "Require Event Be Finished Before It Can Be Canceled")]
            public bool RequireEventFinished { get; set; } = true;

            [JsonProperty(PropertyName = "Require Cupboard Access To Claim")]
            public bool RequireCupboardAccess { get; set; }

            [JsonProperty(PropertyName = "Only Cupboards Are Required To Cancel An Event")]
            public bool OnlyCupboardsAreRequired { get; set; }

            [JsonProperty(PropertyName = "Check If Abandoned Bases Are Too Close Together")]
            public bool TooClose { get; set; } = true;

            [JsonProperty(PropertyName = "Remove Admins From Raiders List")]
            public bool RemoveAdminRaiders { get; set; }

            [JsonProperty(PropertyName = "Lock Base To First Attacker (PVE)")]
            public bool LockBaseToFirstAttackerPVE { get; set; }

            [JsonProperty(PropertyName = "Lock Base To First Attacker (PVP)")]
            public bool LockBaseToFirstAttackerPVP { get; set; }

            [JsonProperty(PropertyName = "Eject Enemies From Locked Raids (PVE)")]
            public bool EjectLockedPVE { get; set; } = true;

            [JsonProperty(PropertyName = "Eject Enemies From Locked Raids (PVP)")]
            public bool EjectLockedPVP { get; set; }
        }

        public class TugboatSettings
        {
            [JsonProperty(PropertyName = "Leaving Tugboat Triggers PVP Delay")]
            public bool Delay { get; set; } = true;

            [JsonProperty(PropertyName = "Unlock Instead Of Destroying During Scans")]
            public bool Unlock { get; set; }

            [JsonProperty(PropertyName = "Prevent Engine Starting During An Event")]
            public bool Engine { get; set; }

            [JsonProperty(PropertyName = "Loot Required")]
            public int Loot { get; set; } = 6;

            [JsonProperty(PropertyName = "Manual Conversions Only")]
            public bool Manual { get; set; }
        }

        public class Rewards
        {
            [JsonProperty(PropertyName = "Economics Money")]
            public double Money { get; set; }

            [JsonProperty(PropertyName = "ServerRewards Points")]
            public int Points { get; set; }

            [JsonProperty(PropertyName = "SkillTree XP")]
            public double XP { get; set; }

            [JsonProperty(PropertyName = "Do Not Reward Canceled Events")]
            public bool Cancel { get; set; }

            [JsonProperty(PropertyName = "Divide Rewards Among All Raiders")]
            public bool DivideRewards { get; set; } = true;
        }

        public class PurgeSettings
        {
            [JsonProperty(PropertyName = "Permission")]
            public string Permission { get; set; } = "";

            [JsonProperty(PropertyName = "Lifetime (Days)")]
            public string LifetimeRaw { get; set; } = "none";

            [JsonProperty(PropertyName = "Conversions Before Destroying Base")]
            public int Limit { get; set; } = 1;

            [JsonIgnore]
            public double Lifetime = 0;

            [JsonIgnore]
            public bool NoPurge { get; set; } = false;

            public static PurgeSettings Find(ulong userid)
            {
                if (!userid.IsSteamId())
                {
                    return null;
                }

                PurgeSettings best = null;

                foreach (var purge in config.Purges)
                {
                    if (!userid.HasPermission(purge.Permission))
                    {
                        continue;
                    }

                    if (purge.NoPurge)
                    {
                        return purge;
                    }

                    if (best == null || best.Lifetime < purge.Lifetime)
                    {
                        best = purge;
                    }
                }

                return best;
            }
        }

        public class UIAdvancedAlertSettings
        {
            [JsonProperty(PropertyName = "Enabled")]
            public bool Enabled { get; set; } = true;

            [JsonProperty(PropertyName = "Anchor Min")]
            public string AnchorMin { get; set; } = "0.35 0.85";

            [JsonProperty(PropertyName = "Anchor Max")]
            public string AnchorMax { get; set; } = "0.65 0.95";

            [JsonProperty(PropertyName = "Time Shown")]
            public float Time { get; set; } = 5f;
        }

        private class ConfigurationNotifications
        {
            [JsonProperty(PropertyName = "Advanced Alerts UI")]
            public UIAdvancedAlertSettings AA { get; set; } = new UIAdvancedAlertSettings();

            [JsonProperty(PropertyName = "Notify Plugin - Type (-1 = disabled)")]
            public int NotifyType { get; set; }

            [JsonProperty(PropertyName = "UI Popup Interval")]
            public float Interval { get; set; } = 1f;

            [JsonProperty(PropertyName = "Send Messages To Player")]
            public bool Message { get; set; } = true;

            [JsonProperty(PropertyName = "Send Global Message When Players Claim A Base")]
            public bool Global { get; set; }

            [JsonProperty(PropertyName = "Message Raiders When An Event Is Completed")]
            public bool RaidCompletion { get; set; }

            [JsonProperty(PropertyName = "Message Players Within X Meters When An Event Is Completed")]
            public float LocalCompletion { get; set; } = 8000f;
        }

        private class Configuration
        {
            [JsonProperty(PropertyName = "Purge Settings", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<PurgeSettings> Purges { get; set; } = DefaultPurgeSettings();

            [JsonProperty(PropertyName = "Abandoned Settings")]
            public AbandonedSettings Abandoned { get; set; } = new AbandonedSettings();

            [JsonProperty(PropertyName = "Messages")]
            public ConfigurationNotifications Messages { get; set; } = new ConfigurationNotifications();

            [JsonProperty(PropertyName = "Run Once On Server Startup")]
            public bool Startup { get; set; }

            [JsonProperty(PropertyName = "Run Every X Seconds")]
            public float Delay { get; set; } = 3600;

            [JsonProperty(PropertyName = "Kill Inactive Sleepers")]
            public bool KillInactiveSleepers { get; set; }

            [JsonProperty(PropertyName = "Move Inventory To Boxes Before Kill Inactive Sleepers")]
            public bool MoveInventory { get; set; }

            [JsonProperty(PropertyName = "Move Inventory Blacklist Shortnames", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> MoveInventoryBlacklist { get; set; } = new List<string> { "rock", "torch" };

            [JsonProperty(PropertyName = "Let Players Kill Abandoned Sleepers")]
            public bool PlayersCanKillSleepers { get; set; }

            [JsonProperty(PropertyName = "Remove Ownership From Bases")]
            public bool RemoveOwnership { get; set; } = true;

            [JsonProperty(PropertyName = "Remove Ownership From Containers")]
            public bool RemoveOwnershipFromContainers { get; set; } = true;

            [JsonProperty(PropertyName = "Remove Ownership When Despawn Timer Is Zero")]
            public bool RemoveOwnershipZero { get; set; }

            [JsonProperty(PropertyName = "Steam Chat ID")]
            public ulong ChatID { get; set; }

            [JsonProperty(PropertyName = "Use Log File")]
            public bool UseLogFile { get; set; }

            [JsonProperty(PropertyName = "Extended Distance To Spawn Away From Zone Manager Zones")]
            public float ZoneDistance { get; set; } = 25f;

            [JsonProperty(PropertyName = "Allowed Zone Manager Zones", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Inclusions { get; set; } = new List<string> { "pvp", "99999999" };

            public void Validate(AbandonedBases instance)
            {
                bool noPurge = false;
                foreach (var purge in Purges)
                {
                    if (!instance.permission.PermissionExists(purge.Permission))
                    {
                        instance.permission.RegisterPermission(purge.Permission, instance);
                    }
                    if (double.TryParse(purge.LifetimeRaw, out purge.Lifetime) && purge.Lifetime > 0)
                    {
                        purge.Lifetime *= 86400;
                    }
                    else
                    {
                        purge.NoPurge = true;
                        noPurge = true;
                    }
                }
                if (!noPurge)
                {
                    if (!instance.permission.PermissionExists("abandonedbases.immune"))
                    {
                        instance.permission.RegisterPermission("abandonedbases.immune", instance);
                    }
                    Purges.Add(new()
                    {
                        LifetimeRaw = "none",
                        NoPurge = true,
                        Permission = "abandonedbases.immune"
                    });
                }
                foreach (var value in Abandoned.BlacklistedCommands.ToList())
                {
                    if (value.StartsWith("/"))
                    {
                        Abandoned.BlacklistedCommands.Remove(value);
                        Abandoned.BlacklistedCommands.Add(value.Substring(1));
                    }
                }
                instance.SaveConfig();
            }
        }

        private bool allowSaveConfig;

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                config ??= new();
                CheckConfig();
                allowSaveConfig = true;
            }
            catch (JsonException ex)
            {
                Debug.LogException(ex);
                LoadDefaultConfig();
            }
        }

        private void CheckConfig()
        {
            foreach (var x in config.Abandoned.Disabled.ToList())
            {
                if (x.Start == "23:59" && x.End == "11:59")
                {
                    x.Start = "00:00";
                    x.End = "12:00";
                }
                else if (x.Start == "11:59" && x.End == "23:59")
                {
                    x.Start = "12:00";
                    x.End = "00:00";
                }
            }
            if (!config.Abandoned.AllowPVPSAR.HasValue)
            {
                config.Abandoned.AllowPVPSAR = config.Abandoned.AllowPVP;
            }
            if (!config.Abandoned.AllowPVPAttack.HasValue)
            {
                config.Abandoned.AllowPVPAttack = config.Abandoned.AllowPVP;
            }
        }

        private List<string> _buildingBlocks = new List<string>
        {
            "block.stair.lshape", "block.stair.lshape", "block.stair.spiral", "block.stair.spiral.triangle", "block.stair.ushape", "door.hinged.wood", "door.hinged.metal", "door.hinged.toptier", "door.double.hinged.wood", "door.double.hinged.stone", "door.double.hinged.toptier", "door.hinged.industrial.a", "door.hinged.industrial.d", "floor", "floor.frame", "floor.grill", "floor.triangle.grill", "floor.triangle.ladder", "floor.triangle", "floor.triangle.frame", "foundation", "foundation.steps", "foundation.triangle", "gates.external.high.wood", "gates.external.high.stone", "ramp", "roof", "roof.triangle", "shutter.metal.embrasure.a", "shutter.metal.embrasure.b", "shutter.wood.a", "wall.external.high.ice", "wall.external.high.stone", "wall.external.high.wood", "wall.frame.cell", "wall.frame.fence", "wall.frame.garagedoor", "wall.frame.netting", "wall.frame.shopfront", "wall.window.bars", "wall", "wall.doorway", "wall.frame", "wall.half", "wall.low", "wall.window"
        };

        protected override void SaveConfig()
        {
            if (allowSaveConfig)
            {
                Config.WriteObject(config);
            }
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        #endregion Configuration
    }
}

namespace Oxide.Plugins.AbandonedBasesExtensionMethods
{
    public static class ExtensionMethods
    {
        internal static Core.Libraries.Permission _permission;
        internal static Core.Libraries.Permission permission { get { if (_permission == null) { _permission = Interface.Oxide.GetLibrary<Core.Libraries.Permission>(null); } return _permission; } set { _permission = value; } }
        public static bool All<T>(this IList<T> a, Func<T, bool> b) { for (int i = 0; i < a.Count; i++) { if (!b(a[i])) { return false; } } return true; }
        public static List<T> DistinctList<T>(this IEnumerable<T> a) { var b = new List<T>(); using (var c = a.GetEnumerator()) { while (c.MoveNext()) { if (!b.Contains(c.Current)) { b.Add((T)(object)c.Current); } } } return b; }
        public static bool Exists<T>(this IEnumerable<T> a, Func<T, bool> b = null) { using (var c = a.GetEnumerator()) { while (c.MoveNext()) { if (b == null || b(c.Current)) { return true; } } } return false; }
        public static T ElementAt<T>(this IEnumerable<T> a, int b) { using (var c = a.GetEnumerator()) { while (c.MoveNext()) { if (b == 0) { return c.Current; } b--; } } return default(T); }
        public static T FirstOrDefault<T>(this IEnumerable<T> a, Func<T, bool> b = null) { using (var c = a.GetEnumerator()) { while (c.MoveNext()) { if (b == null || b(c.Current)) { return c.Current; } } } return default(T); }
        public static IEnumerable<V> Select<T, V>(this IList<T> a, Func<T, V> b) { var c = new List<V>(); for (int i = 0; i < a.Count; i++) { c.Add(b(a[i])); } return c; }
        public static List<T> Take<T>(this IList<T> a, int b) { var c = new List<T>(); for (int i = 0; i < a.Count; i++) { if (c.Count == b) { break; } c.Add(a[i]); } return c; }
        public static List<T> ToList<T>(this IEnumerable<T> a) { var b = new List<T>(); using (var c = a.GetEnumerator()) { while (c.MoveNext()) { b.Add(c.Current); } } return b; }
        public static bool IsHuman(this BasePlayer a) { if (a.IsKilled() || a.IsNpc || !a.userID.IsSteamId()) { return false; } return true; }
        public static int RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> c, Func<TKey, TValue, bool> d) { int a = 0; foreach (var b in c.ToList()) { if (d(b.Key, b.Value)) { c.Remove(b.Key); a++; } } return a; }
        public static List<T> OfType<T>(this IEnumerable<BaseNetworkable> a) where T : BaseEntity { var b = new List<T>(); using (var c = a.GetEnumerator()) { while (c.MoveNext()) { if (c.Current is T) { b.Add(c.Current as T); } } } return b; }
        public static int Sum<T>(this IList<T> a, Func<T, int> b) { int c = 0; for (int i = 0; i < a.Count; i++) { var d = b(a[i]); if (float.IsNaN(d)) { continue; } c += d; } return c; }
        public static List<T> Where<T>(this IList<T> a, Func<T, bool> b) { var c = new List<T>(); for (int i = 0; i < a.Count; i++) { if (b(a[i])) { c.Add(a[i]); } } return c; }
        public static bool HasPermission(this string a, string b) { return !string.IsNullOrEmpty(a) && permission.UserHasPermission(a, b); }
        public static bool HasPermission(this BasePlayer a, string b) { return a != null && a.UserIDString.HasPermission(b); }
        public static bool HasPermission(this ulong a, string b) { return a.ToString().HasPermission(b); }
        public static bool PermissionExists(this string a) { return permission.PermissionExists(a); }
        public static bool IsReallyValid(this BaseNetworkable a) { return !((object)a == null || a.IsDestroyed || (object)a.net == null); }
        public static bool IsReallyConnected(this BasePlayer a) { return a.IsReallyValid() && a.net.connection != null; }
        public static bool IsKilled(this BaseNetworkable a) { return a == null || a.IsDestroyed || a.transform == null; }
        public static void SafelyKill(this BaseNetworkable a) { if (a == null || a.IsDestroyed) { return; } a.Kill(BaseNetworkable.DestroyMode.None); }
        public static bool CanCall(this Plugin a) { return a != null && a.IsLoaded; }
        public static BasePlayer ToPlayer(this IPlayer user) { return user?.Object as BasePlayer; }
        public static bool IsInBounds(this OBB o, Vector3 a) { return o.ClosestPoint(a) == a; }
        public static bool IsValid(this List<AbandonedBases.CustomCostOptions> options) => options != null && options.Exists(o => o.IsReallyValid());
        public static string ObjectName(this GameObject go) { try { return go?.name ?? string.Empty; } catch { return string.Empty; } }
    }
}