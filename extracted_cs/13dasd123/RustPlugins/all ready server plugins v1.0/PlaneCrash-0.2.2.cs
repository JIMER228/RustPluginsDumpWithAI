using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Rust;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AI;

namespace Oxide.Plugins
{
    [Info("PlaneCrash", "k1lly0u", "0.2.2")]
    [Description("Call cargo planes that can be shot down by players to score loot")]
    class PlaneCrash : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin Clans, Friends, FancyDrop, LustyMap, Kits;

        private StoredData storedData;
        private DynamicConfigFile data;

        private static PlaneCrash Instance { get; set; }

        private float mapSize;

        private Timer callTimer;
        private bool initialized;

        private Dictionary<ulong, InventoryData> deadMemberIds = new Dictionary<ulong, InventoryData>();

        // Effects
        private const string C4EXPLOSION_EFFECT = "assets/prefabs/tools/c4/effects/c4_explosion.prefab";
        private const string HELIEXPLOSION_EFFECT = "assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab";
        private const string DEBRIS_EFFECT = "assets/prefabs/npc/patrol helicopter/damage_effect_debris.prefab";
        private const string FIREBALL_EFFECT = "assets/bundled/prefabs/oilfireballsmall.prefab";
        private const string SMOKE_EFFECT = "assets/bundled/prefabs/fx/smoke_signal_full.prefab";

        // Prefabs
        private const string CARGOPLANE_PREFAB = "assets/prefabs/npc/cargo plane/cargo_plane.prefab";
        private const string GIBS_PREFAB = "assets/prefabs/npc/patrol helicopter/servergibs_patrolhelicopter.prefab";
        private const string CRATE_PREFAB = "assets/prefabs/npc/patrol helicopter/heli_crate.prefab";
        private const string SUPPLYDROP_PREFAB = "assets/prefabs/misc/supply drop/supply_drop.prefab";
        private const string ZOMBIE_PREFAB = "assets/prefabs/npc/murderer/murderer.prefab";
        private const string SCIENTIST_PREFAB = "assets/prefabs/npc/scientist/scientist.prefab";
        private const string HEAVYSCIENTIST_PREFAB = "assets/rust.ai/agents/npcplayer/humannpc/heavyscientist/heavyscientist.prefab";
        private const string CORPSE_PREFAB = "assets/prefabs/player/player_corpse.prefab";
        private const string DEBRISMARKER_PREFAB = "assets/prefabs/tools/map/explosionmarker.prefab";
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {           
            mapSize = TerrainMeta.Size.x;
           
            data = Interface.Oxide.DataFileSystem.GetFile("planecrash_cooldowns");

            foreach (KeyValuePair<string, int> kvp in configData.Cooldowns)
            {
                if (!kvp.Key.StartsWith("planecrash."))
                    permission.RegisterPermission("planecrash." + kvp.Key, this);
                else permission.RegisterPermission(kvp.Key, this);
            }
            permission.RegisterPermission("planecrash.cancall", this);

            lang.RegisterMessages(Messages, this);
        }

        private void OnServerInitialized()
        {
            Instance = this;
            initialized = true;

            LoadData();

            if (configData.EventTimers.Random)
                StartCrashTimer();
        }

        private void OnServerSave() => SaveData();

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!initialized || entity == null)
                return;

            if (entity is CargoPlane)
            {
                if (!configData.Plane.ApplyToAll)
                    return;

                NextTick(() =>
                {
                    if (entity == null)
                        return;

                    object success = Interface.Call("isStrikePlane", entity as CargoPlane);
                    if (success is bool && (bool)success)
                        return;

                    object location = (entity as CargoPlane).dropPosition;
                    if (!(location is Vector3))
                        location = Vector3Ex.Range(-(mapSize / 6f), mapSize / 6f);

                    timer.In(2, () => AddCrashComponent(entity as CargoPlane, (Vector3)location, true));
                });
            }

            if (entity is LootableCorpse)
            {
                if (!configData.NPCOptions.ReplaceCorpseLoot)
                    return;

                LootableCorpse corpse = entity as LootableCorpse;              

                InventoryData inventoryData;

                if (!deadMemberIds.TryGetValue(corpse.playerSteamID, out inventoryData))
                    return;

                deadMemberIds.Remove(corpse.playerSteamID);

                timer.In(2, () =>
                {
                    if (inventoryData != null)
                        inventoryData.RestoreItemsTo(corpse);

                    corpse.ResetRemovalTime(configData.NPCOptions.DespawnTime);
                });
            }
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || !configData.NPCOptions.ReplaceCorpseLoot)
                return;

            NPCController npcController = entity.GetComponent<NPCController>();
            if (npcController != null)
            {
                if (!deadMemberIds.ContainsKey(npcController.Entity.userID))
                    deadMemberIds.Add(npcController.Entity.userID, new InventoryData(npcController.Entity));

                StripInventory(npcController.Entity, true);
                return;
            }
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null)
                return;

            CrashPlane crashComponent = entity.GetComponent<CrashPlane>();
            if (crashComponent != null)
                UnityEngine.Object.Destroy(crashComponent);
        }

        private object CanLootEntity(BasePlayer player, LootContainer container)
        {
            LootLock lootLock = container.GetComponent<LootLock>();
            if (lootLock != null && !lootLock.IsUnlocked())
            {
                if (player.userID == lootLock.ownerID)                
                    return null;
                
                if (configData.Loot.LockSettings.LockToPlayerTeam)
                {
                    RelationshipManager.PlayerTeam ownerTeam = RelationshipManager.Instance.FindPlayersTeam(lootLock.ownerID);
                    RelationshipManager.PlayerTeam playerTeam = RelationshipManager.Instance.FindPlayersTeam(player.userID);
                    if (ownerTeam != null && playerTeam != null && ownerTeam == playerTeam)                    
                        return null;                    
                }

                if (configData.Loot.LockSettings.LockToPlayerClans && IsClanmate(lootLock.ownerID, player.userID))                                   
                    return null;
                
                if (configData.Loot.LockSettings.LockToPlayerFriends && AreFriends(lootLock.ownerID, player.userID))                
                    return null;
                
                player.ChatMessage(msg("LootLocked", player.UserIDString));
                return false;
            }
            return null;
        }

        private void Unload()
        {
            CrashPlane[] planes = UnityEngine.Object.FindObjectsOfType<CrashPlane>();

            for (int i = 0; i < planes?.Length; i++)            
                UnityEngine.Object.Destroy(planes[i]);
            
            configData = null;
            Instance = null;
        }
        #endregion

        #region Components
        private enum FlightStatus { Flying, Crashing }

        private class CrashPlane : MonoBehaviour
        {
            private CargoPlane entity;
            private Transform tr;

            public FlightStatus status;

            private BasePlayer lastAttacker;

            private FireBall engineFireLeft;
            private FireBall engineFireRight;

            private Vector3 engineFireLeftOffset = new Vector3(-10, 6, 10);
            private Vector3 engineFireRightOffset = new Vector3(10, 6, 10);

            private Vector3 startPos;
            private Vector3 endPos;
            
            private int rocketHits;

            private float speed;
            private float currentSpeed;

            private float crashTimeTaken;
            private float crashTimeToTake;           

            private bool isDying;
            private bool isSmoking;
            private bool hasDropped;
            private bool isDropPlane;

            private bool isFancyDrop;

            private bool willAutoCrash;
            private float autoCrashAt;
            private float randomCrashDirection = -1;
            private float crashDelay = 0;
            private Vector3 baseEulerAngles;

            private Type[] interactableTypes = new Type[] { typeof(TreeEntity), typeof(ResourceEntity), typeof(BuildingBlock), typeof(SimpleBuildingBlock), typeof(BaseHelicopter) };

            private void Awake()
            {
                entity = GetComponent<CargoPlane>();
                entity.enabled = false;

                tr = entity.transform;

                gameObject.layer = (int)Layer.Reserved2;
                gameObject.name = "CrashPlane";

                AttachCollider();

                speed = currentSpeed = configData.Plane.Speed;

                crashTimeTaken = 0;
                crashTimeToTake = 20;
                status = FlightStatus.Flying;

                willAutoCrash = UnityEngine.Random.Range(0, 100) < configData.Plane.AutoCrashChance;
                autoCrashAt = UnityEngine.Random.Range(configData.Plane.CrashMinimumRange, configData.Plane.CrashMaximumRange);

                if (configData.Plane.RandomizeCrashDirection)
                    randomCrashDirection = UnityEngine.Random.value;
            }

            private void Update()
            {
                if (tr.position.y <= 0)
                {
                    if (!isDying)
                        Die();
                    return;
                }

                if (status == FlightStatus.Crashing)
                {
                    crashTimeTaken = crashTimeTaken + Time.deltaTime;

                    float delta = Mathf.InverseLerp(0, crashTimeToTake, crashTimeTaken);
                    if (delta < 1)
                    {
                        currentSpeed = speed + Mathf.Lerp(0, 10, delta);

                        Vector3 direction = baseEulerAngles;

                        direction.x = Mathf.Lerp(0, 25f, delta);

                        if (randomCrashDirection != -1)
                        {
                            float yaw = Mathf.Lerp(0f, 25f, delta * 1.75f);
                            float roll = Mathf.Lerp(0f, 35f, delta * 2.25f);

                            if (randomCrashDirection <= 0.5f)
                            {
                                direction.y -= yaw;
                                direction.z += roll;
                            }
                            else
                            {
                                direction.y += yaw;
                                direction.z -= roll;
                            }
                        }
                        tr.eulerAngles = direction;
                    }
                   
                    tr.position = Vector3.MoveTowards(tr.position, tr.position + (tr.forward * 10), currentSpeed * UnityEngine.Time.deltaTime);                    
                }
                else
                {
                    tr.position = Vector3.MoveTowards(tr.position, endPos, currentSpeed * UnityEngine.Time.deltaTime);

                    float delta = InverseLerp(startPos, endPos, tr.position);

                    if (isDropPlane)
                    {                        
                        entity.secondsTaken = entity.secondsToTake * delta;
                        
                        if (!hasDropped && delta >= 0.5f)
                        {
                            hasDropped = true;
                            if (!Instance.FancyDrop || !isFancyDrop)
                            {
                                BaseEntity drop = GameManager.server.CreateEntity(SUPPLYDROP_PREFAB, tr.position);
                                drop.globalBroadcast = true;
                                drop.Spawn();
                            }
                        }
                    }

                    if (willAutoCrash)
                    {
                        if (delta >= autoCrashAt && crashDelay < Time.time)
                        {
                            if (TerrainMeta.HeightMap.GetHeight(tr.position) < 0)
                            {
                                crashDelay = Time.time + 3f;
                                return;
                            }

                            SendChatMessage("AutoCrashMessage");
                            BeginCrash();
                        }
                    }
                }

                if (engineFireLeft != null)                
                    engineFireLeft.transform.position = tr.TransformPoint(engineFireLeftOffset);

                if (engineFireRight != null)
                    engineFireRight.transform.position = tr.TransformPoint(engineFireRightOffset);                

                tr.hasChanged = true;
            }

            private float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
            {
                Vector3 ab = b - a;
                Vector3 av = value - a;
                return Vector3.Dot(av, ab) / Vector3.Dot(ab, ab);
            }

            private void OnDestroy()
            {
                enabled = false;
                CancelInvoke();

                if (engineFireLeft != null && !engineFireLeft.IsDestroyed)
                    engineFireLeft.Extinguish();

                if (engineFireRight != null && !engineFireRight.IsDestroyed)
                    engineFireRight.Extinguish();

                if (entity != null && !entity.IsDestroyed)
                    entity.Kill();
            }

            private void OnTriggerEnter(Collider col)
            {
                if (col is TerrainCollider)
                {
                    Die();
                    return;
                }

                BasePlayer player = col.gameObject.GetComponentInParent<BasePlayer>();
                if (player != null && col.gameObject.layer == 17)
                {
                    if (player.IsNpc)
                        return;

                    player.Die();
                    return;
                }

                ServerProjectile serverProjectile = col.GetComponentInParent<ServerProjectile>();
                if (serverProjectile != null)
                {
                    SmallExplosion();
                    col.GetComponentInParent<TimedExplosive>()?.Explode();
                    rocketHits++;

                    BasePlayer attacker = col.GetComponent<BaseEntity>()?.creatorEntity?.ToPlayer();
                   
                    if (configData.Loot.CrateHit > 0)
                        ServerMgr.Instance.StartCoroutine(SpawnLoot(configData.Loot.CrateHit, false));

                    if (configData.Loot.SupplyHit > 0)
                        ServerMgr.Instance.StartCoroutine(SpawnLoot(configData.Loot.SupplyHit, true));

                    if (rocketHits == 1)                    
                        AddFire(ref engineFireLeft);                    

                    if (rocketHits >= configData.Plane.DestroyHits)
                    {
                        if (attacker != null)
                            lastAttacker = attacker;

                        if (configData.Messages.DisplayAttacker && lastAttacker != null)
                            SendChatMessage("AttackerMessage2", lastAttacker.displayName);

                        Die();
                        return;
                    }

                    if (rocketHits >= configData.Plane.DownHits && status == FlightStatus.Flying)
                    {
                        if (attacker != null)
                            lastAttacker = attacker;

                        if (lastAttacker != null && configData.Messages.DisplayAttacker)
                            SendChatMessage("AttackerMessage1", lastAttacker.displayName);

                        BeginCrash();
                        return;
                    }
                }

                if (configData.Plane.Destruction)
                {
                    for (int i = 0; i < interactableTypes.Length; i++)
                    {
                        if (col.GetComponentInParent(interactableTypes[i]) != null)
                        {
                            BaseEntity baseEntity = col.GetComponentInParent<BaseEntity>();
                            BaseCombatEntity baseCombatEntity = baseEntity as BaseCombatEntity;
                            if (baseCombatEntity != null)
                            {
                                baseCombatEntity.Die();
                                return;
                            }
                            
                            if (baseEntity != null)
                            {
                                baseEntity.Kill(BaseNetworkable.DestroyMode.Gib);
                                return;
                            }
                        }
                    }            
                } 
            }

            #region Initialization
            public void SetFlightPath(Vector3 newDropPosition, bool isStandard = false)
            {
                if (entity == null)
                {
                    print("[ERROR] Error in SetFlightPath, the plane is null. Something (likely another plugin) has destroyed it...");
                    return;
                }

                if (!isStandard)
                {
                    if (configData.Plane.RandomDistance != 0)                    
                        newDropPosition += new Vector3(UnityEngine.Random.Range(-configData.Plane.RandomDistance, configData.Plane.RandomDistance), 0, UnityEngine.Random.Range(-configData.Plane.RandomDistance, configData.Plane.RandomDistance));
                    
                    float size = TerrainMeta.Size.x;
                    startPos = Vector3Ex.Range(-1f, 1f);
                    startPos.y = 0f;
                    startPos.Normalize();
                    startPos = startPos * (size * 2f);
                    endPos = startPos * -1f;
                    startPos = startPos + newDropPosition;
                    endPos = endPos + newDropPosition;
                    startPos.y = 150 + configData.Plane.Height;
                    endPos.y = startPos.y;

                    if (configData.Plane.SmokeTrail)
                        RunEffect(SMOKE_EFFECT, entity, new Vector3(), Vector3.up * 3);
                }
                else
                {
                    startPos = entity.startPos;
                    endPos = entity.endPos;
                    isDropPlane = true;
                }

                tr.position = startPos;
                tr.rotation = Quaternion.LookRotation(endPos - startPos);

                if (configData.Plane.RandomizeCrashDirection)
                {
                    Vector3 perp = Vector3.Cross(tr.forward, Vector3.zero - tr.position);
                    float dir = Vector3.Dot(perp, Vector3.up);

                    randomCrashDirection = dir >= 0 ? 1 : 0f;
                }

                entity.secondsToTake = Vector3.Distance(startPos, endPos) / speed;

                isFancyDrop = Instance.FancyDrop ? (bool)Instance.FancyDrop?.CallHook("IsFancyDrop", entity) : false;
                if (isDropPlane && isFancyDrop)
                    Instance.FancyDrop?.CallHook("OverrideDropTime", entity, entity.secondsToTake);

                Destroy(this, entity.secondsToTake);
            }

            private void AttachCollider()
            {
                Rigidbody rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = true;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(50, 10, 50);
                collider.transform.localPosition = Vector3.up * 6;
            }

            public void BeginCrash()
            {
                endPos = new Vector3(endPos.x, 0, endPos.z);
                status = FlightStatus.Crashing;

                baseEulerAngles = tr.eulerAngles;

                SmallExplosion();
                AddFire(ref engineFireLeft);
                AddFire(ref engineFireRight);
            }
            #endregion

            #region Effects
            private void BigExplosion()
            {
                RunEffect(HELIEXPLOSION_EFFECT, null, tr.position);
                RunEffect(DEBRIS_EFFECT, null, tr.position);
            }

            private void SmallExplosion()
            {
                RunEffect(C4EXPLOSION_EFFECT, null, tr.position);
                RunEffect(DEBRIS_EFFECT, null, tr.position);
            }

            private void AddFire(ref FireBall fireBall, float lifetime = 600)
            {
                if (fireBall != null)
                    return;

                fireBall = GameManager.server.CreateEntity(FIREBALL_EFFECT, tr.position) as FireBall;
                fireBall.enableSaving = false;
                fireBall.Spawn();

                Rigidbody rb = fireBall.GetComponent<Rigidbody>();                
                rb.isKinematic = true;

                fireBall.CancelInvoke(fireBall.Extinguish);
                fireBall.CancelInvoke(fireBall.TryToSpread);

                fireBall.Invoke(fireBall.Extinguish, lifetime);
            }

            #endregion

            private void Die()
            {
                if (isDying)
                    return;

                isDying = true;
                BigExplosion();
                CreateLootSpawns();

                if (configData.NPCOptions.Enabled)
                    ServerMgr.Instance.StartCoroutine(CreateNPCs(tr.position));

                if (configData.LustyOptions.CrashIcon)
                    InvokeHandler.Invoke(this, UpdateCrashMarker, 1.5f);

                if (configData.MapOptions.CrashIcon)
                    InvokeHandler.Invoke(this, UpdateMapMarker, 1.5f);

                InvokeHandler.Invoke(this, SmallExplosion, 0.25f);
                InvokeHandler.Invoke(this, SmallExplosion, 0.5f);
                InvokeHandler.Invoke(this, BigExplosion, 1.25f);
                InvokeHandler.Invoke(this, SmallExplosion, 1.75f);
                InvokeHandler.Invoke(this, BigExplosion, 2.25f);

                Destroy(this, 2.5f);
            }

            #region Loot
            private void CreateLootSpawns()
            {
                List<ServerGib> serverGibs = ServerGib.CreateGibs(GIBS_PREFAB, gameObject, entity.gameObject, tr.forward * 2, 5f);
                for (int i = 0; i < 12; i++)
                {
                    BaseEntity fireBall = GameManager.server.CreateEntity(FIREBALL_EFFECT, tr.position, tr.rotation, true);
                    if (fireBall)
                    {
                        Vector3 randsphere = UnityEngine.Random.onUnitSphere;
                        fireBall.transform.position = (tr.position + new Vector3(0f, 1.5f, 0f)) + (randsphere * UnityEngine.Random.Range(-4f, 4f));

                        Collider collider = fireBall.GetComponent<Collider>();
                        fireBall.Spawn();
                        fireBall.SetVelocity(tr.forward + randsphere);

                        foreach (ServerGib serverGib in serverGibs)
                            Physics.IgnoreCollision(collider, serverGib.GetCollider(), true);
                    }
                }

                if (configData.Loot.CrateCrash > 0)
                    ServerMgr.Instance.StartCoroutine(SpawnLoot(configData.Loot.CrateCrash, false, true));

                if (configData.Loot.SupplyCrash > 0)
                    ServerMgr.Instance.StartCoroutine(SpawnLoot(configData.Loot.SupplyCrash, true, true));

                if (configData.Messages.DisplayDestroy)
                {
                    if (configData.Messages.UseGrid)
                        SendChatMessage("DestroyMessage.Grid", GetGridString(tr.position));
                    else SendChatMessage("DestroyMessage", tr.position.x, tr.position.z);
                }
            }

            private IEnumerator SpawnLoot(int amount, bool isDrop, bool isCrashing = false)
            {
                for (int j = 0; j < amount; j++)
                {
                    Vector3 position = (tr.position + new Vector3(0f, 1.5f, 0f)) + (UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(-2f, 3f));

                    position.y = Mathf.Max(TerrainMeta.HeightMap.GetHeight(position), position.y);
                    
                    string ent = isDrop ? SUPPLYDROP_PREFAB : CRATE_PREFAB;

                    LootContainer container = GameManager.server.CreateEntity(ent, position, Quaternion.LookRotation(UnityEngine.Random.onUnitSphere), true) as LootContainer;
                    container.enableSaving = false;
                    container.Spawn();

                    if (configData.Loot.LockSettings.LockToPlayer && lastAttacker != null)                    
                        LootLock.Initialize(container, lastAttacker.userID);
                    
                    if (j == 0 && configData.Plane.Smoke && isCrashing && !isSmoking)
                    {
                        RunEffect(SMOKE_EFFECT, container);
                        isSmoking = true;
                    }

                    Rigidbody rigidbody;
                    if (!isDrop)
                        rigidbody = container.gameObject.AddComponent<Rigidbody>();
                    else
                    {
                        container.GetComponent<SupplyDrop>().RemoveParachute();
                        rigidbody = container.GetComponent<Rigidbody>();
                    }

                    if (rigidbody != null)
                    {
                        rigidbody.isKinematic = false;
                        rigidbody.useGravity = true;
                        rigidbody.mass = 1.25f;
                        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                        rigidbody.drag = 0.25f;
                        rigidbody.angularDrag = 0.1f;
                        rigidbody.AddForce((tr.forward + (tr.right * UnityEngine.Random.Range(-10f, 10f))) * 100);
                    }

                    FireBall fireBall = null;
                    AddFire(ref fireBall, configData.Loot.FireLife);                    
                    fireBall.SetParent(container, 0, false, true);
                    
                    InvokeHandler.Invoke(container, () => FillLootContainer(container, isDrop), 2f);

                    if (configData.Loot.LootDespawnTime > 0)
                    {
                        container.Invoke(() =>
                        {
                            if (container.HasFlag(BaseEntity.Flags.Open))
                                return;
                            container?.Kill();
                        }, 
                        configData.NPCOptions.DespawnTime);
                    }

                    yield return CoroutineEx.waitForEndOfFrame;
                }
            }
            #endregion

            #region NPCs
            private IEnumerator CreateNPCs(Vector3 position)
            {
                string type = configData.NPCOptions.Type.ToLower() == "murderer" ? ZOMBIE_PREFAB : configData.NPCOptions.Type.ToLower() == "heavyscientist" ? HEAVYSCIENTIST_PREFAB : SCIENTIST_PREFAB;

                int amount = configData.NPCOptions.Amount + (configData.NPCOptions.CorpseEnabled ? configData.NPCOptions.CorpseAmount : 0);

                for (int i = 0; i < amount; i++)
                {
                    Vector3 newPosition = position + (UnityEngine.Random.onUnitSphere * 20);

                    object point = Instance.FindPointOnNavmesh(newPosition);
                    if (point is Vector3)
                        newPosition = (Vector3)point;
                    else newPosition.y = TerrainMeta.HeightMap.GetHeight(newPosition);

                    if (newPosition.y < -0.25f)
                        continue;

                    NPCPlayer npcPlayer = InstantiateEntity(type, newPosition);
                    npcPlayer.enableSaving = false;
                    npcPlayer.Spawn();

                    bool isCorpse = i >= configData.NPCOptions.Amount;

                    NPCController npcController = npcPlayer.gameObject.AddComponent<NPCController>();
                    npcController.Setup(lastAttacker, isCorpse);

                    yield return CoroutineEx.waitForEndOfFrame;
                }
            }
            
            private NPCPlayer InstantiateEntity(string type, Vector3 position)
            {
                GameObject gameObject = Facepunch.Instantiate.GameObject(GameManager.server.FindPrefab(type), position, new Quaternion());
                gameObject.name = type;

                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);

                Destroy(gameObject.GetComponent<Spawnable>());

                if (!gameObject.activeSelf)
                    gameObject.SetActive(true);

                NPCPlayer component = gameObject.GetComponent<NPCPlayer>();
                return component;
            }
            #endregion

            #region Map Markers
            private void UpdateCrashMarker()
            {
                if (!Instance.LustyMap || entity == null)
                    return;

                Instance.LustyMap.Call("AddTemporaryMarker", tr.position.x, tr.position.z, "Crashed Plane", configData.LustyOptions.IconURL, 0);
                Instance.timer.In(configData.LustyOptions.CrashIconTime, () => Instance.LustyMap.Call("RemoveTemporaryMarker", "Crashed Plane"));
            }

            private void UpdateMapMarker()
            {
                if (entity == null)
                    return;

                BaseEntity baseEntity = GameManager.server.CreateEntity(DEBRISMARKER_PREFAB, tr.position, Quaternion.identity, true);
                baseEntity.Spawn();
                baseEntity.SendMessage("SetDuration", configData.MapOptions.CrashIconTime, SendMessageOptions.DontRequireReceiver);
            }
            #endregion
        }

        public class LootLock : MonoBehaviour
        {
            internal LootContainer Container { get; private set; }

            internal ulong ownerID;

            private float lockExpiretime;

            private void Awake()
            {
                Container = GetComponent<LootContainer>();
                lockExpiretime = Time.time + configData.Loot.LockSettings.LockTime;
                Destroy(this, configData.Loot.LockSettings.LockTime);
            }

            internal bool IsUnlocked() => Time.time >= lockExpiretime;

            internal static void Initialize(LootContainer container, ulong ownerId)
            {
                LootLock lootLock = container.gameObject.AddComponent<LootLock>();
                lootLock.ownerID = ownerId;                
            }
        }

        public class NPCController : MonoBehaviour
        {
            public NPCPlayer Entity { get; private set; }

            private Vector3 basePosition;

            private BasePlayer lastAttacker;

            private float woundedDuration;

            private float woundedStartTime;

            private float secondsSinceWoundedStarted
            {
                get
                {
                    return Time.realtimeSinceStartup - this.woundedStartTime;
                }
            }

            private void Awake()
            {
                Entity = GetComponent<NPCPlayer>();
                enabled = false;
                basePosition = Entity.transform.position;

                Entity.NavAgent.areaMask = 1;
                Entity.NavAgent.agentTypeID = -1372625422;

                if (Entity is NPCPlayerApex)
                    (Entity as NPCPlayerApex).CommunicationRadius = -1f;

                Entity.displayName = configData.NPCOptions.Names?.Length > 0 ? configData.NPCOptions.Names.GetRandom() : Entity.ShortPrefabName;
                Entity.InitializeHealth(configData.NPCOptions.Health, configData.NPCOptions.Health);                
            }

            public void Setup(BasePlayer lastAttacker, bool isCorpse)
            {
                this.lastAttacker = lastAttacker;

                if (!isCorpse)
                {
                    if (UnityEngine.Random.Range(0, 100) <= configData.NPCOptions.WoundedChance)
                    {
                        woundedDuration = UnityEngine.Random.Range(300f, 600f);
                        woundedStartTime = Time.realtimeSinceStartup;
                        Entity.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, true);
                        Entity.EnableServerFall(true);
                        Entity.SendNetworkUpdateImmediate(false);
                        Entity.Invoke(WoundingTick, 1f);
                    }

                    InvokeHandler.Invoke(Entity, Despawn, configData.NPCOptions.DespawnTime);
                }

                string kit = isCorpse ? configData.NPCOptions.CorpseKit : configData.NPCOptions.Kit;

                if (Entity is NPCMurderer && configData.NPCOptions.ReplaceCorpseLoot)
                    (Entity as NPCMurderer).LootSpawnSlots = new LootContainer.LootSpawnSlot[0];

                if (Entity is global::HumanNPC && configData.NPCOptions.ReplaceCorpseLoot)
                    (Entity as global::HumanNPC).LootSpawnSlots = new LootContainer.LootSpawnSlot[0];

                if (!string.IsNullOrEmpty(kit))
                {
                    if (Entity.IsInvoking(Entity.EquipTest))
                        Entity.CancelInvoke(Entity.EquipTest);

                    StripInventory(Entity);

                    Instance.NextTick(() =>
                    {
                        Instance.Kits?.Call("GiveKit", Entity, kit);

                        if (isCorpse)
                            InvokeHandler.Invoke(Entity, () => Entity.Die(new HitInfo(lastAttacker ?? Entity, Entity, DamageType.Explosion, 1000f)), UnityEngine.Random.Range(1f, 3f));
                        else Instance.NextTick(() => Entity.EquipTest());
                    });
                }
                else
                {
                    if (isCorpse)
                        InvokeHandler.Invoke(Entity, () => Entity.Die(new HitInfo(lastAttacker ?? Entity, Entity, DamageType.Explosion, 1000f)), UnityEngine.Random.Range(1f, 3f));
                }

                if (!isCorpse)
                    InvokeHandler.InvokeRandomized(this, CheckNPCLocation, 1f, 5f, 1f);
            }

            private void Despawn()
            {
                if (Entity == null || Entity.IsDestroyed || Entity.IsDead())
                    return;

                Entity.inventory.Strip();
                Entity.Die(new HitInfo(lastAttacker ?? Entity, Entity, DamageType.Explosion, 1000f));
            }

            private void WoundingTick()
            {
                if (!Entity.IsDead())
                {
                    if (secondsSinceWoundedStarted < woundedDuration)
                    {
                        Entity.Invoke(WoundingTick, 1f);
                    }
                    else if (UnityEngine.Random.Range(0, 100) >= configData.NPCOptions.WoundedRecoveryChance)
                    {
                        Entity.Die(new HitInfo(lastAttacker ?? Entity, Entity, DamageType.Explosion, 1000f));
                    }
                    else
                    {
                        Entity.SetPlayerFlag(BasePlayer.PlayerFlags.Wounded, false);
                    }
                }
            }

            public void CheckNPCLocation()
            {
                if (Entity == null || Entity.IsDestroyed)
                {
                    Destroy(this);
                    return;
                }

                if (Vector3.Distance(Entity.transform.position, basePosition) > 30)                
                    ResetPosition(); 
            }
            
            private void ResetPosition() => Entity.SetDestination(basePosition);            
        }
        #endregion

        #region Functions
        private void StartCrashTimer()
        {
            callTimer = timer.In(UnityEngine.Random.Range(configData.EventTimers.Min, configData.EventTimers.Max) * 60, () =>
            {
                CargoPlane plane = (CargoPlane)GameManager.server.CreateEntity(CARGOPLANE_PREFAB, new Vector3(), new Quaternion(), true);
                plane.enableSaving = false;
                plane.Spawn();

                CrashPlane crash = plane.gameObject.AddComponent<CrashPlane>();
                crash.SetFlightPath(Vector3Ex.Range(-(mapSize / 6f), mapSize / 6f));

                if (configData.Messages.DisplayIncoming)
                    SendChatMessage("IncomingMessage");

                StartCrashTimer();
            });
        }

        private static void RunEffect(string name, BaseEntity entity = null, Vector3 position = new Vector3(), Vector3 offset = new Vector3())
        {
            if (entity != null)
                Effect.server.Run(name, entity, 0, offset, position, null, true);
            else Effect.server.Run(name, position, Vector3.up, null, true);
        }

        private void AddCrashComponent(CargoPlane plane, Vector3 location, bool isStandard = false)
        {
            if (plane.GetComponent<CrashPlane>())
                return;

            CrashPlane crash = plane.gameObject.AddComponent<CrashPlane>();
            crash.SetFlightPath(location, isStandard);

            if (!isStandard && configData.Messages.DisplayIncoming)
                SendChatMessage("IncomingMessage");
        }

        private static void FillLootContainer(BaseEntity entity, bool isDrop)
        {
            if (entity == null)
                return;

            ItemContainer container = isDrop ? entity.GetComponent<SupplyDrop>()?.inventory : entity.GetComponentInParent<LootContainer>()?.inventory;
            ConfigData.LootSettings.LootTables lootTable = isDrop ? configData.Loot.SupplyLoot : configData.Loot.CrateLoot;
           
            if (container == null || lootTable == null)
                return;

            if (lootTable.Enabled)
            {
                while (container.itemList.Count > 0)
                {
                    Item item = container.itemList[0];
                    item.RemoveFromContainer();
                    item.Remove(0f);
                }

                List<ConfigData.LootSettings.LootTables.LootItem> items = new List<ConfigData.LootSettings.LootTables.LootItem>(lootTable.Items);

                int count = UnityEngine.Random.Range(lootTable.Minimum, lootTable.Maximum);
                for (int i = 0; i < count; i++)
                {
                    ConfigData.LootSettings.LootTables.LootItem lootItem = items.GetRandom();
                    if (lootItem == null)
                        continue;

                    items.Remove(lootItem);
                    if (items.Count == 0)
                        items.AddRange(lootTable.Items);

                    Item item = ItemManager.CreateByName(lootItem.Shortname, UnityEngine.Random.Range(lootItem.Min, lootItem.Max));
                    if (item != null)
                        item.MoveToContainer(container);
                }
            }
            if (configData.Loot.LockSettings.LockCrates)
            {
                entity.SetFlag(BaseEntity.Flags.Locked, true, false);
                InvokeHandler.Invoke(entity, () => { entity.SetFlag(BaseEntity.Flags.Locked, false, false); }, configData.Loot.LockSettings.LockTimer);                
            }
        }

        private static void StripInventory(BasePlayer player, bool skipWear = false)
        {
            Item[] allItems = player.inventory.AllItems();

            for (int i = allItems.Length - 1; i >= 0; i--)
            {
                Item item = allItems[i];
                if (skipWear && item?.parent == player.inventory.containerWear)
                    continue;

                item.RemoveFromContainer();
                item.Remove();
            }
        }

        private object FindPointOnNavmesh(Vector3 targetPosition)
        {
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(targetPosition, out navHit, 100, 1))
                return navHit.position;
            return null;
        }

        private static string GetGridString(Vector3 position)
        {
            Vector2 adjPosition = new Vector2((World.Size / 2) + position.x, (World.Size / 2) - position.z);
            return $"{NumberToString((int)(adjPosition.x / 145))}{((int)(adjPosition.y / 145)) - 1}";
        }

        private static string NumberToString(int number)
        {
            bool a = number > 26;
            Char c = (Char)(65 + (a ? number - 26 : number));
            return a ? "A" + c : c.ToString();
        }

        private bool IsCrashPlane(CargoPlane plane) => plane.GetComponent<CrashPlane>();        

        private string FormatTime(double time)
        {
            TimeSpan dateDifference = TimeSpan.FromSeconds((float)time);
            var days = dateDifference.Days;
            var hours = dateDifference.Hours;
            hours += (days * 24);
            var mins = dateDifference.Minutes;
            var secs = dateDifference.Seconds;
            return string.Format("{0:00}:{1:00}:{2:00}", hours, mins, secs);
        }

        private static void SendChatMessage(string key, params object[] args)
        {
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                BasePlayer player = BasePlayer.activePlayerList[i];           
                player.ChatMessage(args != null ? string.Format(msg(key, player.UserIDString), args) : msg(key, player.UserIDString));
            }
        }
        #endregion

        #region Friends
        private bool AreFriends(ulong playerId, ulong friendId)
        {
            if (Friends)
                return (bool)Friends?.Call("AreFriends", playerId.ToString(), friendId.ToString());
            return false;
        }
        private bool IsClanmate(ulong playerId, ulong friendId)
        {
            if (Clans)
            {
                object playerTag = Clans?.Call("GetClanOf", playerId);
                object friendTag = Clans?.Call("GetClanOf", friendId);
                if (playerTag is string && friendTag is string)
                {
                    if (!string.IsNullOrEmpty((string)playerTag) && !string.IsNullOrEmpty((string)friendTag) && (playerTag == friendTag))
                        return true;
                }
            }
            return false;
        }
        #endregion

        #region Commands
        [ConsoleCommand("callcrash")]
        void ccmdSendCrash(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
                return;

            Vector3 location = Vector3.zero;
            if (arg.Args != null)
            {
                if (arg.Args.Length == 1 && arg.Args[0].ToLower() == "help")
                {
                    SendReply(arg, $"{Title}  v{Version} - k1lly0u @ chaoscode.io");
                    SendReply(arg, "callcrash - Send a random crash plane");
                    SendReply(arg, "callcrash \"X\" \"Z\" - Send a crash plane towards the specified X and Z co-ordinates");
                    SendReply(arg, "callcrash \"playername\" - Send a crash plane towards the specified player's position");
                    SendReply(arg, "callcrash crashall - Force crash any active planes");
                    return;
                }

                if (arg.Args.Length > 0)
                {
                    if (arg.Args.Length == 2)
                    {
                        float x;
                        float z;
                        if (float.TryParse(arg.Args[0], out x) && float.TryParse(arg.Args[1], out z))
                        {
                            location = new Vector3(x, 0, z);
                            SendReply(arg, $"Crash plane sent to X: {x}, Z: {z}");
                        }
                    }
                    if (arg.Args.Length == 1)
                    {
                        if (arg.Args[0].ToLower() == "crashall")
                        {
                            CrashPlane[] crashPlanes = UnityEngine.Object.FindObjectsOfType<CrashPlane>();
                            if (crashPlanes == null || crashPlanes.Length == 0)
                            {
                                SendReply(arg, "There are no planes currently active");
                                return;
                            }

                            for (int i = 0; i < crashPlanes.Length; i++)
                            {
                                CrashPlane crashPlane = crashPlanes[i];
                                if (crashPlane.status == FlightStatus.Flying)
                                    crashPlane.BeginCrash();
                            }
                            
                            SendReply(arg, $"Force crashing {crashPlanes.Length} planes!");
                            return;
                        }
                        else
                        {
                            IPlayer targetPlayer = covalence.Players.FindPlayer(arg.Args[0]);
                            if (targetPlayer != null && targetPlayer.IsConnected)
                            {
                                BasePlayer target = targetPlayer?.Object as BasePlayer;
                                if (target != null)
                                {
                                    location = target.transform.position;
                                    SendReply(arg, $"Crash plane sent towards {target.displayName}'s current position");
                                }
                            }
                            else
                            {
                                SendReply(arg, "Could not locate the specified player");
                                return;
                            }
                        }
                    }
                    else
                    {
                        location = Vector3Ex.Range(-(mapSize / 6f), mapSize / 6f);
                        SendReply(arg, "Crash plane sent to random location");
                    }
                }
            }
            else
            {
                location = Vector3Ex.Range(-(mapSize / 6f), mapSize / 6f);
                SendReply(arg, "Crash plane sent to random location");
            }

            CargoPlane plane = (CargoPlane)GameManager.server.CreateEntity(CARGOPLANE_PREFAB, new Vector3(), new Quaternion(), true);
            plane.Spawn();
            AddCrashComponent(plane, location);
        }

        [ChatCommand("callcrash")]
        void cmdSendCrash(BasePlayer player, string command, string[] args)
        {
            int cooldown = -1;

            if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, "planecrash.cancall"))
                cooldown = 0;
            else
            {
                foreach (KeyValuePair<string, int> kvp in configData.Cooldowns.OrderBy(x => x.Value))
                {
                    if (permission.UserHasPermission(player.UserIDString, kvp.Key))
                    {
                        cooldown = kvp.Value;
                        break;
                    }
                }
            }

            if (cooldown < 0)
                return;

            if (args.Length == 1 && args[0].ToLower() == "help")
            {
                SendReply(player, $"<color=#ce422b>{Title}</color>  <color=#939393>v</color><color=#ce422b>{Version}</color> <color=#939393>-</color> <color=#ce422b>k1lly0u</color><color=#939393> @</color> <color=#ce422b>chaoscode.io</color>");
                SendReply(player, "<color=#ce422b>/callcrash</color><color=#939393> - Send a random crash plane</color>");
                SendReply(player, "<color=#ce422b>/callcrash \"X\" \"Z\" </color><color=#939393>- Send a crash plane towards the specified X and Z co-ordinates</color>");
                SendReply(player, "<color=#ce422b>/callcrash \"playername\" </color><color=#939393>- Send a crash plane towards the specified player's position</color>");

                if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, "planecrash.cancall"))
                    SendReply(player, "<color=#ce422b>/callcrash crashall</color><color=#939393> - Force all active planes to crash</color>");
                return;
            }

            if (cooldown != 0 && storedData.IsOnCooldown(player))
            {
                SendReply(player, string.Format(msg("OnCooldown", player.UserIDString), FormatTime(storedData.GetTimeRemaining(player))));
                return;
            }

            Vector3 location = Vector3.zero;
            if (args.Length > 0)
            {
                if (args.Length == 2)
                {
                    float x;
                    float z;
                    if (float.TryParse(args[0], out x) && float.TryParse(args[1], out z))
                    {
                        location = new Vector3(x, 0, z);
                        SendReply(player, $"<color=#939393>Crash plane sent to</color> <color=#ce422b>X: {x}, Z: {z}</color>");
                    }
                }
                if (args.Length == 1)
                {
                    if (args[0].ToLower() == "crashall")
                    {
                        if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, "planecrash.cancall"))
                        {
                            CrashPlane[] crashPlanes = UnityEngine.Object.FindObjectsOfType<CrashPlane>();
                            if (crashPlanes == null || crashPlanes.Length == 0)
                            {
                                SendReply(player, "There are no planes currently active");
                                return;
                            }

                            for (int i = 0; i < crashPlanes.Length; i++)
                            {
                                CrashPlane crashPlane = crashPlanes[i];
                                if (crashPlane.status == FlightStatus.Flying)
                                    crashPlane.BeginCrash();
                            }

                            SendReply(player, $"Force crashing {crashPlanes.Length} planes!");
                        }
                        return;
                    }
                    else
                    {
                        IPlayer targetPlayer = covalence.Players.FindPlayer(args[0]);
                        if (targetPlayer != null && targetPlayer.IsConnected)
                        {
                            BasePlayer target = targetPlayer?.Object as BasePlayer;
                            if (target != null)
                            {
                                location = target.transform.position;
                                SendReply(player, $"<color=#939393>Crash plane sent towards </color><color=#ce422b>{target.displayName}'s</color><color=#939393> current position</color>");
                            }
                        }
                        else
                        {
                            SendReply(player, "<color=#ce422b>Could not locate the specified player</color>");
                            return;
                        }
                    }
                }
            }
            else
            {
                location = Vector3Ex.Range(-(mapSize / 6f), mapSize / 6f);
                SendReply(player, "<color=#ce422b>Crash plane sent to random location</color>");
            }

            CargoPlane plane = (CargoPlane)GameManager.server.CreateEntity(CARGOPLANE_PREFAB, new Vector3(), new Quaternion(), true);
            plane.Spawn();
            AddCrashComponent(plane, location);

            if (cooldown > 0)
                storedData.AddCooldown(player, cooldown);
        }
        #endregion

        #region Config        
        private static ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Plane Settings")]
            public PlaneSettings Plane { get; set; }

            [JsonProperty(PropertyName = "Loot Settings")]
            public LootSettings Loot { get; set; }

            [JsonProperty(PropertyName = "Message Options")]
            public Messaging Messages { get; set; }

            [JsonProperty(PropertyName = "Timer Settings")]
            public Timers EventTimers { get; set; }

            [JsonProperty(PropertyName = "LustyMap Integration")]
            public Lusty LustyOptions { get; set; }

            [JsonProperty(PropertyName = "Ingame Map Integration")]
            public Map MapOptions { get; set; }

            [JsonProperty(PropertyName = "NPC Options")]
            public Bots NPCOptions { get; set; }

            [JsonProperty(PropertyName = "Command Cooldowns (permission / time in minutes)")]
            public Dictionary<string, int> Cooldowns { get; set; }
           
            public class PlaneSettings
            {
                [JsonProperty(PropertyName = "Apply crash mechanics to all spawned planes")]
                public bool ApplyToAll { get; set; }

                [JsonProperty(PropertyName = "Flight speed")]
                public float Speed { get; set; }

                [JsonProperty(PropertyName = "Show smoke on crash site")]
                public bool Smoke { get; set; }

                [JsonProperty(PropertyName = "Height modifier to default flight height")]
                public float Height { get; set; }

                [JsonProperty(PropertyName = "Amount of rocket hits to destroy mid-flight")]
                public int DestroyHits { get; set; }

                [JsonProperty(PropertyName = "Amount of rocket hits to make the plane crash")]
                public int DownHits { get; set; }

                [JsonProperty(PropertyName = "Show smoke trail behind plane")]
                public bool SmokeTrail { get; set; }

                [JsonProperty(PropertyName = "Destroy objects that get in the way of a crashing plane")]
                public bool Destruction { get; set; }   
                
                [JsonProperty(PropertyName = "Destination randomization distance")]
                public float RandomDistance { get; set; }

                [JsonProperty(PropertyName = "Chance of crashing without player interaction (x out of 100)")]
                public int AutoCrashChance { get; set; }

                [JsonProperty(PropertyName = "Randomize crash direction (may lean left or right when crashing)")]
                public bool RandomizeCrashDirection { get; set; }

                [JsonProperty(PropertyName = "Automatic crash travel range minimum")]
                public float CrashMinimumRange { get; set; }

                [JsonProperty(PropertyName = "Automatic crash travel range maximum")]
                public float CrashMaximumRange { get; set; }
            }

            public class LootSettings
            {                
                [JsonProperty(PropertyName = "Fireball lifetime (seconds)")]
                public int FireLife { get; set; }

                [JsonProperty(PropertyName = "Crate amount (Crash)")]
                public int CrateCrash { get; set; }

                [JsonProperty(PropertyName = "Supply drop amount (Crash)")]
                public int SupplyCrash { get; set; }

                [JsonProperty(PropertyName = "Crate amount (Rocket hit)")]
                public int CrateHit { get; set; }

                [JsonProperty(PropertyName = "Supply drop amount (Rocket hit)")]
                public int SupplyHit { get; set; }

                [JsonProperty(PropertyName = "Supply drop loot table")]
                public LootTables SupplyLoot { get; set; }

                [JsonProperty(PropertyName = "Loot despawn time (seconds)")]
                public int LootDespawnTime { get; set; }

                [JsonProperty(PropertyName = "Crate loot table")]
                public LootTables CrateLoot { get; set; }

                [JsonProperty(PropertyName = "Loot Locking Settings")]
                public LootLock LockSettings { get; set; }
                
                public class LootTables
                {
                    [JsonProperty(PropertyName = "Use this loot table")]
                    public bool Enabled { get; set; }

                    [JsonProperty(PropertyName = "Minimum amount of items to drop")]
                    public int Minimum { get; set; }

                    [JsonProperty(PropertyName = "Maximum amount of items to drop")]
                    public int Maximum { get; set; }

                    [JsonProperty(PropertyName = "Item list")]
                    public List<LootItem> Items { get; set; }

                    public class LootItem
                    {
                        [JsonProperty(PropertyName = "Item shortname")]
                        public string Shortname { get; set; }

                        [JsonProperty(PropertyName = "Minimum amount")]
                        public int Min { get; set; }

                        [JsonProperty(PropertyName = "Maximum amount")]
                        public int Max { get; set; }
                    }
                }

                public class LootLock
                {
                    [JsonProperty(PropertyName = "Lock dropped crates and supply drops")]
                    public bool LockCrates { get; set; }

                    [JsonProperty(PropertyName = "Locked crates and supply drop timer (seconds)")]
                    public int LockTimer { get; set; }

                    [JsonProperty(PropertyName = "Lock loot to player who shot down plane")]
                    public bool LockToPlayer { get; set; }

                    [JsonProperty(PropertyName = "Allow friends to loot")]
                    public bool LockToPlayerFriends { get; set; }

                    [JsonProperty(PropertyName = "Allow clan mates to loot")]
                    public bool LockToPlayerClans { get; set; }

                    [JsonProperty(PropertyName = "Allow team mates to loot")]
                    public bool LockToPlayerTeam { get; set; }

                    [JsonProperty(PropertyName = "Amount of time containers will be locked to the player who shot down the plane (seconds)")]
                    public int LockTime { get; set; }
                }
            }   

            public class Lusty
            {
                [JsonProperty(PropertyName = "Show icon on crash site")]
                public bool CrashIcon { get; set; }

                [JsonProperty(PropertyName = "Amount of time the crash icon will be displayed on LustyMap (seconds)")]
                public int CrashIconTime { get; set; }

                [JsonProperty(PropertyName = "Crash icon URL")]
                public string IconURL { get; set; }               
            }

            public class Map
            {
                [JsonProperty(PropertyName = "Show ingame map marker on crash site")]
                public bool CrashIcon { get; set; }

                [JsonProperty(PropertyName = "Amount of time the crash icon will be displayed on the ingame map (minutes)")]
                public int CrashIconTime { get; set; }               
            }

            public class Messaging
            {
                [JsonProperty(PropertyName = "Display incoming crash plane message")]
                public bool DisplayIncoming { get; set; }

                [JsonProperty(PropertyName = "Display destroyed crash plane message")]
                public bool DisplayDestroy { get; set; }

                [JsonProperty(PropertyName = "Display message stating who shot down the plane")]
                public bool DisplayAttacker { get; set; }

                [JsonProperty(PropertyName = "Use grid coordinates instead of world coordinates")]
                public bool UseGrid { get; set; }
            }

            public class Timers
            {
                [JsonProperty(PropertyName = "Autospawn crash planes with a random spawn timer")]
                public bool Random { get; set; }

                [JsonProperty(PropertyName = "Minimum time between autospawned planes (minutes)")]
                public int Min { get; set; }

                [JsonProperty(PropertyName = "Maximum time between autospawned planes (minutes)")]
                public int Max { get; set; }
            }

            public class Bots
            {
                [JsonProperty(PropertyName = "Spawn NPCs at the crash site")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Amount of NPCs to spawn")]
                public int Amount { get; set; }

                [JsonProperty(PropertyName = "Spawn corpses at the crash site")]
                public bool CorpseEnabled { get; set; }

                [JsonProperty(PropertyName = "Amount of corpses to spawn")]
                public int CorpseAmount { get; set; }

                [JsonProperty(PropertyName = "Custom kit for the corpses (Requires 'Replace corpse loot' set to true)")]
                public string CorpseKit { get; set; }

                [JsonProperty(PropertyName = "Type of NPCs to spawn (Murderer / Scientist / HeavyScientist)")]
                public string Type { get; set; }

                [JsonProperty(PropertyName = "Custom kit for the NPC")]
                public string Kit { get; set; }

                [JsonProperty(PropertyName = "Replace corpse loot with current items")]
                public bool ReplaceCorpseLoot { get; set; }

                [JsonProperty(PropertyName = "Initial health for the NPC")]
                public float Health { get; set; }

                [JsonProperty(PropertyName = "Despawn time for NPCs (seconds)")]
                public int DespawnTime { get; set; }

                [JsonProperty(PropertyName = "NPC Names (Chosen at random)")]
                public string[] Names { get; set; }

                [JsonProperty(PropertyName = "Chance that NPCs will spawn in the wounded state (x out of 100)")]
                public int WoundedChance { get; set; }

                [JsonProperty(PropertyName = "Chance that NPCs will recover from the wounded state (x out of 100)")]
                public int WoundedRecoveryChance { get; set; }
            }

            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Cooldowns = new Dictionary<string, int>
                {
                    ["planecrash.cancall.vip1"] = 120,
                    ["planecrash.cancall.vip2"] = 60,
                    ["planecrash.cancall.vip3"] = 30,
                },
                EventTimers = new ConfigData.Timers
                {
                    Max = 60,
                    Min = 45,
                    Random = true
                },
                Plane = new ConfigData.PlaneSettings
                {
                    ApplyToAll = false,
                    AutoCrashChance = 20,
                    Smoke = true,
                    Speed = 35f,
                    Height = 0f,
                    DestroyHits = 3,
                    Destruction = true,
                    DownHits = 1,
                    SmokeTrail = true,
                    RandomDistance = 50f,
                    RandomizeCrashDirection = true,
                    CrashMinimumRange = 0.3f,
                    CrashMaximumRange = 0.5f,
                },
                Loot = new ConfigData.LootSettings
                {
                    CrateCrash = 3,
                    SupplyCrash = 3,
                    FireLife = 300,
                    CrateHit = 1,
                    SupplyHit = 1,
                    LootDespawnTime = 0,
                    CrateLoot = new ConfigData.LootSettings.LootTables
                    {
                        Maximum = 4,
                        Minimum = 1,
                        Items = new List<ConfigData.LootSettings.LootTables.LootItem>
                        {
                            new ConfigData.LootSettings.LootTables.LootItem {Shortname = "metal.refined", Max = 100, Min = 10 },
                            new ConfigData.LootSettings.LootTables.LootItem {Shortname = "explosive.timed", Max = 2, Min = 1 },
                            new ConfigData.LootSettings.LootTables.LootItem {Shortname = "grenade.f1", Max = 3, Min = 1 },
                            new ConfigData.LootSettings.LootTables.LootItem {Shortname = "supply.signal", Max = 1, Min = 1 },
                            new ConfigData.LootSettings.LootTables.LootItem {Shortname = "cctv.camera", Max = 1, Min = 1 },
                            new ConfigData.LootSettings.LootTables.LootItem {Shortname = "targeting.computer", Max = 1, Min = 1 },
                            new ConfigData.LootSettings.LootTables.LootItem {Shortname = "ammo.rifle", Max = 60, Min = 20 },
                            new ConfigData.LootSettings.LootTables.LootItem {Shortname = "ammo.pistol", Max = 60, Min = 20 }
                        },
                        Enabled = false
                    },
                    SupplyLoot = new ConfigData.LootSettings.LootTables
                    {
                        Maximum = 6,
                        Minimum = 2,
                        Items = new List<ConfigData.LootSettings.LootTables.LootItem>
                        {
                            new ConfigData.LootSettings.LootTables.LootItem {Shortname = "rifle.ak", Max = 1, Min = 1 },
                            new ConfigData.LootSettings.LootTables.LootItem {Shortname = "pistol.m92", Max = 1, Min = 1 },
                            new ConfigData.LootSettings.LootTables.LootItem {Shortname = "pistol.semiauto", Max = 1, Min = 1 },
                            new ConfigData.LootSettings.LootTables.LootItem {Shortname = "shotgun.double", Max = 1, Min = 1 },
                            new ConfigData.LootSettings.LootTables.LootItem {Shortname = "smg.thompson", Max = 1, Min = 1 },
                            new ConfigData.LootSettings.LootTables.LootItem {Shortname = "rifle.bolt", Max = 1, Min = 1 }
                        },
                        Enabled = false
                    },
                    LockSettings = new ConfigData.LootSettings.LootLock
                    {
                        LockCrates = true,
                        LockTimer = 120,
                        LockToPlayer = false,
                        LockToPlayerClans = false,
                        LockToPlayerFriends = false,
                        LockToPlayerTeam = false,
                        LockTime = 600
                    }
                },
                LustyOptions = new ConfigData.Lusty
                {
                    CrashIcon = true,
                    CrashIconTime = 300,
                    IconURL = "http://www.rustedit.io/images/crashicon.png"
                },
                MapOptions = new ConfigData.Map
                {
                    CrashIcon = true,
                    CrashIconTime = 5
                },
                Messages = new ConfigData.Messaging
                {
                    DisplayDestroy = true,
                    DisplayIncoming = true,
                    DisplayAttacker = true,
                    UseGrid = false
                },
                NPCOptions = new ConfigData.Bots
                {
                    Amount = 5,
                    Enabled = true,
                    Type = "Murderer",
                    Kit = "",
                    Health = 100,
                    DespawnTime = 300,
                    Names = new string[0],
                    CorpseAmount = 5,
                    CorpseEnabled = false,
                    CorpseKit = "",
                    ReplaceCorpseLoot = true,
                    WoundedChance = 0,
                    WoundedRecoveryChance = 20
                },
                Version = Version
            };            
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();
            if (configData.Version < new VersionNumber(0, 1, 9))
            {
                configData.Messages.DisplayAttacker = baseConfig.Messages.DisplayAttacker;                
                configData.NPCOptions = baseConfig.NPCOptions;
            }

            if (configData.Version < new VersionNumber(0, 1, 94))            
                configData.Plane.Destruction = baseConfig.Plane.Destruction;

            if (configData.Version < new VersionNumber(0, 1, 97))
                configData.MapOptions = baseConfig.MapOptions;

            if (configData.Version < new VersionNumber(0, 1, 99))
                configData.NPCOptions.DespawnTime = baseConfig.NPCOptions.DespawnTime;

            if (configData.Version < new VersionNumber(0, 1, 100))
                configData.NPCOptions.Names = new string[0];

            if (configData.Version < new VersionNumber(0, 1, 101))
            {
                configData.NPCOptions.Type = "murderer";
                configData.NPCOptions.ReplaceCorpseLoot = false;
                configData.NPCOptions.CorpseAmount = 5;
                configData.NPCOptions.CorpseEnabled = false;
                configData.NPCOptions.CorpseKit = string.Empty;

            }

            if (configData.Version < new VersionNumber(0, 1, 103))
            {
                configData.Plane.RandomDistance = 50f;
                configData.Cooldowns = baseConfig.Cooldowns;
            }

            if (configData.Version < new VersionNumber(0, 1, 104))
            {
                configData.Loot.LockSettings = baseConfig.Loot.LockSettings;
            }

            if (configData.Version < new VersionNumber(0, 1, 105))
            {
                configData.NPCOptions.WoundedChance = 0;
                configData.NPCOptions.WoundedRecoveryChance = 20;                
            }

            if (configData.Version < new VersionNumber(1, 0, 107))
            {
                configData.Plane.AutoCrashChance = 20;
                configData.Plane.RandomizeCrashDirection = true;
            }

            if (configData.Version < new VersionNumber(1, 0, 108))
            {
                configData.Plane.CrashMinimumRange = 0.3f;
                configData.Plane.CrashMaximumRange = 0.5f;
            }

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }
        #endregion

        #region Corpse Inventory Population
        public class InventoryData
        {
            public List<InventoryItem> items = new List<InventoryItem>();

            public InventoryData(NPCPlayer player)
            {
                items = player.inventory.AllItems().Select(item => new InventoryItem
                {
                    itemid = item.info.itemid,
                    amount = item.amount > 1 ? UnityEngine.Random.Range(1, item.amount) : item.amount,
                    ammo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.contents ?? 0,
                    ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                    skin = item.skin,
                    condition = UnityEngine.Random.Range(1, item.condition),
                    instanceData = new InventoryItem.InstanceData(item),
                    contents = item.contents?.itemList.Select(item1 => new InventoryItem
                    {
                        itemid = item1.info.itemid,
                        amount = item1.amount,
                        condition = UnityEngine.Random.Range(1, item1.condition)
                    }).ToArray()
                }).ToList();
            }

            public void RestoreItemsTo(LootableCorpse corpse)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    Item item = CreateItem(items[i]);
                    item.MoveToContainer(corpse.containers[0]);
                }
            }

            private Item CreateItem(InventoryItem itemData)
            {
                Item item = ItemManager.CreateByItemID(itemData.itemid, itemData.amount, itemData.skin);
                item.condition = itemData.condition;

                if (itemData.instanceData != null)
                    itemData.instanceData.Restore(item);

                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    if (!string.IsNullOrEmpty(itemData.ammotype))
                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(itemData.ammotype);
                    weapon.primaryMagazine.contents = itemData.ammo;
                }
                if (itemData.contents != null)
                {
                    foreach (InventoryItem contentData in itemData.contents)
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

            public class InventoryItem
            {
                public int itemid;
                public ulong skin;
                public int amount;
                public float condition;
                public int ammo;
                public string ammotype;
                public InstanceData instanceData;
                public InventoryItem[] contents;

                public class InstanceData
                {
                    public int dataInt;
                    public int blueprintTarget;
                    public int blueprintAmount;

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
                        item.instanceData = new ProtoBuf.Item.InstanceData();
                        item.instanceData.blueprintAmount = blueprintAmount;
                        item.instanceData.blueprintTarget = blueprintTarget;
                        item.instanceData.dataInt = dataInt;
                    }
                }
            }
        }
        #endregion

        #region Data Management
        private void SaveData() => data.WriteObject(storedData);

        private void LoadData()
        {
            try
            {
                storedData = data.ReadObject<StoredData>();
            }
            catch
            {
                storedData = new StoredData();
            }
        }

        private class StoredData
        {
            public Dictionary<ulong, double> cooldowns = new Dictionary<ulong, double>();

            private double CurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

            public void AddCooldown(BasePlayer player, int time)
            {
                if (cooldowns.ContainsKey(player.userID))
                    cooldowns[player.userID] = CurrentTime() + (time * 60);
                else cooldowns.Add(player.userID, CurrentTime() + (time * 60));
            }

            public bool IsOnCooldown(BasePlayer player)
            {
                if (!cooldowns.ContainsKey(player.userID))
                    return false;

                if (cooldowns[player.userID] < CurrentTime())
                    return false;

                return true;
            }

            public double GetTimeRemaining(BasePlayer player)
            {
                if (!cooldowns.ContainsKey(player.userID))
                    return 0;

                return cooldowns[player.userID] - CurrentTime();
            }
        }
        #endregion

        #region Localization
        private static string msg(string key, string playerId = null) => Instance.lang.GetMessage(key, Instance, playerId);

        private Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["DestroyMessage"] = "<color=#939393>A plane carrying cargo has just crashed at co-ordinates </color><color=#ce422b>X: {0}, Z: {1}</color>",
            ["DestroyMessage.Grid"] = "<color=#939393>A plane carrying cargo has just crashed around </color><color=#ce422b>{0}</color>",
            ["IncomingMessage"] = "<color=#ce422b>A low flying plane carrying cargo is about to fly over!</color><color=#939393>\nIf you are skilled enough you can shoot it down with a rocket launcher!</color>",
            ["AttackerMessage1"] = "<color=#ce422b>{0}</color><color=#939393> has shot down the plane!</color>",
            ["AttackerMessage2"] = "<color=#ce422b>{0}</color><color=#939393> has shot the plane out of the sky!</color>",
            ["OnCooldown"] = "<color=#939393>You must wait another </color><color=#ce422b>{0}</color><color=#939393> before you can call another crash plane</color>",
            ["LootLocked"] = "<color=#939393>This container is locked to the player who shot down the plane</color>",
            ["AutoCrashMessage"] = "<color=#939393>A Cargo Planes <color=#ce422b>engines have malfunctioned</color> and it is making a crash landing!</color>"
        };
        #endregion
    }
}
