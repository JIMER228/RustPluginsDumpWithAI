using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using Oxide.Core.Plugins;
using Rust;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("PlaneCrash", "S1m0n", "0.1.72")]
    class PlaneCrash : RustPlugin
    {
        #region Fields
        [PluginReference]
        Plugin FancyDrop;

        static PlaneCrash ins;
        static MethodInfo removeChute;
        static FieldInfo cpSecondsTaken;
        static FieldInfo cpSecondsToTake;
        static FieldInfo cpStartPos;
        static FieldInfo cpEndPos;
        private FieldInfo getDropPosition;

        static float initialHeight;
        private float mapSize;

        private Timer callTimer;
        private bool initialized;
        // Effects
        const string c4Explosion = "assets/prefabs/tools/c4/effects/c4_explosion.prefab";
        const string heliExplosion = "assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab";
        const string debris = "assets/prefabs/npc/patrol helicopter/damage_effect_debris.prefab";
        const string fireball = "assets/bundled/prefabs/oilfireballsmall.prefab";
        const string smokeSignal = "assets/bundled/prefabs/fx/smoke_signal_full.prefab";

        // Prefabs
        const string cargoPlanePrefab = "assets/prefabs/npc/cargo plane/cargo_plane.prefab";
        const string gibs = "assets/prefabs/npc/patrol helicopter/servergibs_patrolhelicopter.prefab";
        const string crates = "assets/prefabs/npc/patrol helicopter/heli_crate.prefab";
        const string supply = "assets/prefabs/misc/supply drop/supply_drop.prefab";
        #endregion

        #region Components
        enum FlightStatus { Flying, Crashing }
        class CrashPlane : MonoBehaviour
        {
            private CargoPlane entity;
            private FlightStatus status;

            private FireBall[] engineFires;

            private Vector3 startPos;
            private Vector3 endPos;

            private RaycastHit rayHit;

            private int rocketHits;

            private float speed;
            private float currentSpeed;
            private float crashRotation;
            private float yRotation;

            private float crashTimeTaken;
            private float crashTimeToTake;
            private float timeTaken;
            private float timeToTake;

            private bool isDying;
            private bool isSmoking;
            private bool hasDropped;
            private bool isDropPlane;

            void Awake()
            {
                entity = GetComponent<CargoPlane>();
                entity.enabled = false;

                gameObject.layer = (int)Layer.Reserved1;
                gameObject.name = "CrashPlane";

                AttachCollider();

                speed = ins.configData.PlaneSettings.FlightSpeed;
                currentSpeed = speed;
                crashTimeTaken = 0;
                crashTimeToTake = 20;
                status = FlightStatus.Flying;
                crashRotation = UnityEngine.Random.Range(-4, 4);
            }

            void FixedUpdate()
            {
                if (entity.transform.position.y <= -5)
                {
                    if (!isDying)
                        Die();
                    return;
                }
                if (status == FlightStatus.Crashing)
                {
                    crashTimeTaken = crashTimeTaken + UnityEngine.Time.deltaTime;
                    var single = Mathf.InverseLerp(0, crashTimeToTake, crashTimeTaken);
                    if (single < 1)
                    {
                        currentSpeed = speed + Mathf.Lerp(0, 10, single);
                        entity.transform.transform.eulerAngles = new Vector3(Mathf.Lerp(0, 25f, single), Mathf.Lerp(yRotation, yRotation + crashRotation, single), entity.transform.transform.eulerAngles.z);
                    }

                    entity.transform.position = Vector3.MoveTowards(entity.transform.position, entity.transform.position + (entity.transform.forward * 10), currentSpeed * UnityEngine.Time.deltaTime);
                }
                else
                {
                    entity.transform.position = Vector3.MoveTowards(entity.transform.position, endPos, currentSpeed * UnityEngine.Time.deltaTime);
                    if (isDropPlane)
                    {
                        timeTaken = timeTaken + UnityEngine.Time.deltaTime;
                        cpSecondsTaken.SetValue(entity, timeTaken);
                        var single = Mathf.InverseLerp(0, timeToTake, timeTaken);
                        if (!hasDropped && single >= 0.5f)
                        {
                            hasDropped = true;
                            if (!ins.FancyDrop || (ins.FancyDrop && !(bool)ins.FancyDrop.CallHook("IsFancyDrop", entity)))
                            {
                                BaseEntity drop = GameManager.server.CreateEntity(supply, entity.transform.position);
                                drop.globalBroadcast = true;
                                drop.Spawn();
                            }
                        }
                    }
                }

                entity.transform.hasChanged = true;
            }
            void OnDestroy()
            {
                enabled = false;
                CancelInvoke();
                if (engineFires != null)
                {
                    foreach (var fire in engineFires)
                    {
                        if (fire != null && !fire.isDestroyed)
                            fire.Extinguish();
                    }
                }
                if (!entity.isDestroyed)
                    entity.Kill();
            }
            void OnTriggerEnter(Collider col)
            {
                bool isRocket = false;
                if (col?.gameObject?.layer == 17)
                {
                    var player = col.GetComponentInParent<BasePlayer>();
                    if (player != null)
                    {
                        player.DieInstantly();
                        return;
                    }
                }                

                if (col.GetComponentInParent<LootContainer>() || col.GetComponentInParent<SupplyDrop>()) return;

                if (col.GetComponentInParent<BaseEntity>() || col.GetComponentInParent<Terrain>())
                {
                    if (col.GetComponentInParent<ServerProjectile>())
                    {
                        SmallExplosion();
                        col.GetComponentInParent<TimedExplosive>()?.Explode();
                        isRocket = true;
                        rocketHits++;

                        SpawnLoot(ins.configData.LootSettings.RocketHit_CrateAmount, false);
                        SpawnLoot(ins.configData.LootSettings.RocketHit_SupplyDropAmount, true);

                        if (rocketHits >= ins.configData.PlaneSettings.RocketHitsToDestroy)
                        {
                            Die();
                            return;
                        }
                    }

                    if (status == FlightStatus.Flying)
                    {
                        endPos = new Vector3(endPos.x, 0, endPos.z);
                        status = FlightStatus.Crashing;
                        yRotation = entity.transform.transform.eulerAngles.y;
                        BeginCrash();
                    }
                    else
                    {
                        if (!isRocket)
                            Die();
                    }
                }
            }
            public void SetFlightPath(Vector3 newDropPosition, bool isStandard = false)
            {
                if (!isStandard)
                {
                    float size = TerrainMeta.Size.x;
                    startPos = Vector3Ex.Range(-1f, 1f);
                    startPos.y = 0f;
                    startPos.Normalize();
                    startPos = startPos * (size * 2f);
                    endPos = startPos * -1f;
                    startPos = startPos + newDropPosition;
                    endPos = endPos + newDropPosition;
                    startPos.y = initialHeight;
                    endPos.y = startPos.y;

                    if (ins.configData.PlaneSettings.ShowSmokeTrail)
                        RunEffect(smokeSignal, entity, new Vector3(), Vector3.up * 3);
                }
                else
                {
                    startPos = (Vector3)cpStartPos.GetValue(entity);
                    endPos = (Vector3)cpEndPos.GetValue(entity);
                    isDropPlane = true;
                }
                entity.transform.position = startPos;
                entity.transform.rotation = Quaternion.LookRotation(endPos - startPos);

                timeToTake = Vector3.Distance(startPos, endPos) / speed;
                cpSecondsToTake.SetValue(entity, timeToTake);
                if (isDropPlane && ins.FancyDrop && (bool)ins.FancyDrop.CallHook("IsFancyDrop", entity))
                    ins.FancyDrop.CallHook("OverrideDropTime", entity, timeToTake);


                Invoke("KillComponent", timeToTake);
            }
            internal void AttachCollider()
            {
                var rigidbody = gameObject.AddComponent<Rigidbody>();
                rigidbody.useGravity = false;
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = true;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

                var collider = gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(50, 10, 50);
                collider.transform.position = entity.transform.position + Vector3.up * 6;
            }           
            internal void BeginCrash()
            {
                SmallExplosion();
                AddFire();
            }
            internal void BigExplosion()
            {
                RunEffect(heliExplosion, null, entity.transform.position);
                RunEffect(debris, null, entity.transform.position);
            }
            internal void SmallExplosion()
            {
                RunEffect(c4Explosion, null, entity.transform.position);
                RunEffect(debris, null, entity.transform.position);
            }
            internal void AddFire()
            {
                engineFires = new FireBall[]
                {
                    SpawnFireball(entity, new Vector3(-10, 6, 10), 600),
                    SpawnFireball(entity, new Vector3(10, 6, 10), 600)
                };
            }
            internal void Die()
            {
                if (isDying)
                    return;
                isDying = true;
                BigExplosion();
                CreateLootSpawns();
                Invoke("SmallExplosion", 0.25f);
                Invoke("SmallExplosion", 0.5f);
                Invoke("BigExplosion", 1.25f);
                Invoke("SmallExplosion", 1.75f);
                Invoke("BigExplosion", 2.25f);
                Invoke("KillComponent", 2.5f);
            }
            internal void KillComponent()
            {
                enabled = false;
                Destroy(this);
            }
            internal void CreateLootSpawns()
            {
                List<ServerGib> serverGibs = ServerGib.CreateGibs(gibs, gameObject, entity.gameObject, entity.transform.forward * 2, 5f);
                for (int i = 0; i < 12; i++)
                {
                    BaseEntity fireb = GameManager.server.CreateEntity(fireball, entity.transform.position, entity.transform.rotation, true);
                    if (fireb)
                    {
                        Vector3 randsphere = UnityEngine.Random.onUnitSphere;

                        fireb.transform.position = (entity.transform.position + new Vector3(0f, 1.5f, 0f)) + (randsphere * UnityEngine.Random.Range(-6f, 6f));
                        Collider collider = fireb.GetComponent<Collider>();
                        fireb.Spawn();
                        fireb.SetVelocity(entity.transform.forward + (randsphere * UnityEngine.Random.Range(3, 10)));
                        foreach (ServerGib serverGib in serverGibs)
                        {
                            Physics.IgnoreCollision(collider, serverGib.GetCollider(), true);
                        }
                    }
                }

                foreach (ServerGib serverGib1 in serverGibs)
                    Physics.IgnoreCollision(GetComponent<Collider>(), serverGib1.GetCollider(), true);

                SpawnLoot(ins.configData.LootSettings.Crash_CrateAmount, false, true);
                SpawnLoot(ins.configData.LootSettings.Crash_SupplyDropAmount, true, true);

                if (ins.configData.Messages.DisplayDestroyMessage)
                    ins.PrintToChat(ins.configData.Messages.DestroyMessage.Replace("{x}", entity.transform.position.x.ToString()).Replace("{z}", entity.transform.position.z.ToString()));

            }
            void SpawnLoot(int amount, bool isDrop, bool isCrashing = false)
            {
                if (amount == 0) return;

                for (int j = 0; j < amount; j++)
                {
                    Vector3 randsphere = UnityEngine.Random.onUnitSphere;
                    Vector3 entpos = (entity.transform.position + new Vector3(0f, 1.5f, 0f)) + (randsphere * UnityEngine.Random.Range(-2f, 3f));

                    var ent = isDrop ? supply : crates;
                    BaseEntity crate = GameManager.server.CreateEntity(ent, entpos, Quaternion.LookRotation(randsphere), true);
                    crate.Spawn();

                    if (j == 0 && ins.configData.PlaneSettings.DeploySmokeOnCrashSite && isCrashing && !isSmoking)
                    {
                        RunEffect(smokeSignal, crate);
                        isSmoking = true;
                    }

                    if (!isDrop)
                    {
                        Rigidbody rigidbody = crate.gameObject.AddComponent<Rigidbody>();
                        rigidbody.useGravity = true;
                        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                        rigidbody.mass = 2f;
                        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                        rigidbody.velocity = entity.transform.forward + (randsphere * UnityEngine.Random.Range(1f, 3f));
                        rigidbody.angularVelocity = Vector3Ex.Range(-1.75f, 1.75f);
                        rigidbody.drag = 0.5f * (rigidbody.mass / 5f);
                        rigidbody.angularDrag = 0.2f * (rigidbody.mass / 5f);
                    }
                    else
                    {
                        removeChute.Invoke(crate.GetComponent<SupplyDrop>(), null);
                        var rigidbody = crate.GetComponent<Rigidbody>();
                        rigidbody.useGravity = true;
                        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                        rigidbody.mass = 2f;
                        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                        rigidbody.velocity = entity.transform.forward + (randsphere * UnityEngine.Random.Range(1f, 3f));
                        rigidbody.angularVelocity = Vector3Ex.Range(-1.75f, 1.75f);
                        rigidbody.drag = 0.5f * (rigidbody.mass / 5f);
                        rigidbody.angularDrag = 0.2f * (rigidbody.mass / 5f);

                    }
                    FireBall fireBall = SpawnFireball(crate, null);
                    crate.Invoke("RemoveMe", 1800f);
                    crate.SendMessage("SetLockingEnt", fireBall.gameObject, SendMessageOptions.DontRequireReceiver);
                    ins.timer.In(1, ()=> ins.FillLootContainer(crate, isDrop));
                }
            }
            FireBall SpawnFireball(BaseEntity parent, object offset = null, float lifetime = 0)
            {
                FireBall fireBall = GameManager.server.CreateEntity(fireball, parent.transform.position, new Quaternion(), true) as FireBall;
                if (fireBall)
                {                    
                    fireBall.GetComponent<Rigidbody>().isKinematic = true;
                    fireBall.GetComponent<Collider>().enabled = false;
                    fireBall.SetParent(parent);
                    if (offset is Vector3)
                        fireBall.transform.localPosition = (Vector3)offset;
                    fireBall.Spawn();
                    fireBall.Invoke("Extinguish", lifetime == 0 ? ins.configData.LootSettings.FireLifetime : lifetime);
                    return fireBall;
                }
                return null;
            }
        }
        #endregion

        #region Oxide Hooks
        void Loaded()
        {
            removeChute = typeof(SupplyDrop).GetMethod("RemoveParachute", (BindingFlags.Instance | BindingFlags.NonPublic));
            getDropPosition = typeof(CargoPlane).GetField("dropPosition", (BindingFlags.Instance | BindingFlags.NonPublic));
            cpStartPos = typeof(CargoPlane).GetField("startPos", (BindingFlags.Instance | BindingFlags.NonPublic));
            cpEndPos = typeof(CargoPlane).GetField("endPos", (BindingFlags.Instance | BindingFlags.NonPublic));
            cpSecondsTaken = typeof(CargoPlane).GetField("secondsTaken", (BindingFlags.Instance | BindingFlags.NonPublic));
            cpSecondsToTake = typeof(CargoPlane).GetField("secondsToTake", (BindingFlags.Instance | BindingFlags.NonPublic));
            mapSize = TerrainMeta.Size.x;
            permission.RegisterPermission("planecrash.cancall", this);
        }
        void OnServerInitialized()
        {
            ins = this;
            LoadVariables();
            initialized = true;
            initialHeight = 150 + configData.PlaneSettings.HeightModifier;
            if (configData.EventTimers.UseRandomCrashTimer)
                StartCrashTimer();
        }
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!initialized || !configData.PlaneSettings.ApplyToAllPlanes) return;
            if (entity is CargoPlane)
            {
                var success = Interface.Call("isStrikePlane", entity as CargoPlane);
                if (success is bool && (bool)success) { Puts("isstrike"); return; }

                object location = getDropPosition.GetValue(entity as CargoPlane);
                if (!(location is Vector3))
                    location = Vector3Ex.Range(-(mapSize / 6f), mapSize / 6f);
                timer.In(2, () => AddCrashComponent(entity as CargoPlane, (Vector3)location, true));
            }
        }
        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null) return;
            if (entity.GetComponent<CrashPlane>())
                UnityEngine.Object.Destroy(entity.GetComponent<CrashPlane>());
        }
        void Unload()
        {
            var planes = UnityEngine.Object.FindObjectsOfType<CrashPlane>();
            foreach (var obj in planes)
                UnityEngine.Object.Destroy(obj);
        }
        #endregion

        #region Functions
        void StartCrashTimer()
        {
            callTimer = timer.In(UnityEngine.Random.Range(configData.EventTimers.MinimumMinutesBetweenCalls, configData.EventTimers.MaximumMinutesBetweenCalls) * 60, () =>
            {
                CargoPlane plane = (CargoPlane)GameManager.server.CreateEntity(cargoPlanePrefab, new Vector3(), new Quaternion(), true);
                plane.Spawn();
                var crash = plane.gameObject.AddComponent<CrashPlane>();
                crash.SetFlightPath(Vector3Ex.Range(-(mapSize / 6f), mapSize / 6f));

                if (configData.Messages.DisplayIncomingMessage)
                    PrintToChat(configData.Messages.IncomingMessage);

                StartCrashTimer();
            });
        }
        static void RunEffect(string name, BaseEntity entity = null, Vector3 position = new Vector3(), Vector3 offset = new Vector3())
        {
            if (entity != null)
                Effect.server.Run(name, entity, 0, offset, position, null, true);
            else Effect.server.Run(name, position);
        }
        private void AddCrashComponent(CargoPlane plane, Vector3 location, bool isStandard = false)
        {
            if (plane.GetComponent<CrashPlane>()) return;
            var crash = plane.gameObject.AddComponent<CrashPlane>();
            crash.SetFlightPath(location, isStandard);

            if (!isStandard && configData.Messages.DisplayIncomingMessage)
                PrintToChat(configData.Messages.IncomingMessage);
        }
        private void FillLootContainer(BaseEntity entity, bool isDrop)
        {
            if (entity == null) return;
            ItemContainer container = null;
            LootTables lootTable;
            if (isDrop)
            {
                lootTable = configData.LootSettings.SupplyDropLootTables;
                container = entity.GetComponent<SupplyDrop>()?.inventory;
            }
            else
            {
                container = entity.GetComponentInParent<LootContainer>()?.inventory;                
                lootTable = configData.LootSettings.CrateLootTables;
            }
            if (container == null || lootTable == null) return;
            if (lootTable.UseLootTable)
            {
                while (container.itemList.Count > 0)
                {
                    var item = container.itemList[0];
                    item.RemoveFromContainer();
                    item.Remove(0f);
                }
                int count = UnityEngine.Random.Range(lootTable.MinimumItems, lootTable.MaximumItems);
                for (int i = 0; i < count; i++)
                {
                    var lootItem = lootTable.LootItems.GetRandom();
                    if (lootItem == null) continue;
                    Item item = ItemManager.CreateByName(lootItem.Shortname, UnityEngine.Random.Range(lootItem.MinimumAmount, lootItem.MaximumAmount));
                    if (item != null)
                        item.MoveToContainer(container);
                }
            }
        }
        #endregion

        #region Commands
        [ConsoleCommand("callcrash")]
        void ccmdSendCrash(ConsoleSystem.Arg arg)
        {
            if (arg.connection != null) return;
            Vector3 location = Vector3.zero;
            if (arg.Args != null)
            {
                if (arg.Args.Length == 1 && arg.Args[0].ToLower() == "help")
                {
                    SendReply(arg, $"<color=orange>{Title}</color>");
                    SendReply(arg, "<color=#ffd479>callcrash</color> - Вызвать самолет-катастрофу в случайное направление");
                    SendReply(arg, "<color=#ffd479>callcrash <X Z></color> - Вызвать самолет-катастрофу на координаты X и Z");
                    SendReply(arg, "<color=#ffd479>callcrash <игрок></color> - Вызвать самолет-катастрофу на местоположение указанного игрока");
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
                            SendReply(arg, $"<color=#ffd479>Самолет-катастрофа</color> вылетел на координаты <color=orange>X: {x}</color>, <color=orange>Z: {z}</color>");
                        }
                    }
                    if (arg.Args.Length == 1)
                    {
                        var targetPlayer = covalence.Players.FindPlayer(arg.Args[0]);
                        if (targetPlayer != null && targetPlayer.IsConnected)
                        {
                            var target = targetPlayer?.Object as BasePlayer;
                            if (target != null)
                            {
                                location = target.transform.position;
                                SendReply(arg, $"<color=#ffd479>Самолет-катастрофа </color>вылетел на местоположение игрока<color=orange>{target.displayName}</color>");
                            }
                        }
                        else
                        {
                            SendReply(arg, "Не удалось найти указанного игрока");
                            return;
                        }
                    }
                    else
                    {
                        location = Vector3Ex.Range(-(mapSize / 6f), mapSize / 6f);
                        SendReply(arg, "<color=#ffd479>Самолет-катастрофа</color> вылетел в случайном направлении");
                    }
                }
            }
            else
            {
                location = Vector3Ex.Range(-(mapSize / 6f), mapSize / 6f);
                SendReply(arg, "<color=#ffd479>Самолет-катастрофа</color> вылетел в случайном направлении");
            }

            CargoPlane plane = (CargoPlane)GameManager.server.CreateEntity(cargoPlanePrefab, new Vector3(), new Quaternion(), true);
            plane.Spawn();
            AddCrashComponent(plane, location);
        }
        [ChatCommand("callcrash")]
        void cmdSendCrash(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin() && !permission.UserHasPermission(player.UserIDString, "planecrash.cancall")) return;
            if (args.Length == 1 && args[0].ToLower() == "help")
            {
                SendReply(player, $"<color=orange>{Title}</color>");
                SendReply(player, "<color=#ffd479>/callcrash</color> - Вызвать самолет-катастрофу в случайное направление");
                SendReply(player, "<color=#ffd479>/callcrash <X Z></color> - Вызвать самолет-катастрофу на координаты X и Z");
                SendReply(player, "<color=#ffd479>/callcrash <игрок></color> - Вызвать самолет-катастрофу на местоположение указанного игрока");
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
                        SendReply(player, $"<color=#ffd479>Самолет-катастрофа</color> вылетел на координаты <color=orange>X: {x}</color>, <color=orange>Z: {z}</color>");
                    }
                }
                if (args.Length == 1)
                {
                    var targetPlayer = covalence.Players.FindPlayer(args[0]);
                    if (targetPlayer != null && targetPlayer.IsConnected)
                    {
                        var target = targetPlayer?.Object as BasePlayer;
                        if (target != null)
                        {
                            location = target.transform.position;
                            SendReply(player, $"<color=#ffd479>Самолет-катастрофа </color>вылетел на местоположение игрока<color=orange>{target.displayName}</color>");
                        }
                    }
                    else
                    {
                        SendReply(player, "Не удалось найти указанного игрока");
                        return;
                    }
                }
            }
            else
            {
                location = Vector3Ex.Range(-(mapSize / 6f), mapSize / 6f);
                SendReply(player, "<color=#ffd479>Самолет-катастрофа</color> вылетел в случайном направлении");
            }

            CargoPlane plane = (CargoPlane)GameManager.server.CreateEntity(cargoPlanePrefab, new Vector3(), new Quaternion(), true);
            plane.Spawn();
            AddCrashComponent(plane, location);
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class PlaneSettings
        {
            public bool ApplyToAllPlanes { get; set; }
            public float FlightSpeed { get; set; }
            public bool DeploySmokeOnCrashSite { get; set; }
            public float HeightModifier { get; set; }
            public int RocketHitsToDestroy { get; set; }
            public bool ShowSmokeTrail { get; set; }
        }
        class LootSettings
        {
            public int FireLifetime { get; set; }
            public int Crash_CrateAmount { get; set; }
            public int Crash_SupplyDropAmount { get; set; }
            public int RocketHit_CrateAmount { get; set; }
            public int RocketHit_SupplyDropAmount { get; set; }
            public LootTables SupplyDropLootTables { get; set; }
            public LootTables CrateLootTables { get; set; }
        }
        class LootTables
        {
            public bool UseLootTable { get; set; }
            public int MinimumItems { get; set; }
            public int MaximumItems { get; set; }
            public List<LootItem> LootItems { get; set; }
        }
        class LootItem
        {
            public string Shortname { get; set; }
            public int MinimumAmount { get; set; }
            public int MaximumAmount { get; set; }
        }
        class Messaging
        {
            public bool DisplayIncomingMessage { get; set; }
            public bool DisplayDestroyMessage { get; set; }
            public string IncomingMessage { get; set; }
            public string DestroyMessage { get; set; }
        }
        class Timers
        {
            public bool UseRandomCrashTimer { get; set; }
            public int MinimumMinutesBetweenCalls { get; set; }
            public int MaximumMinutesBetweenCalls { get; set; }
        }
        class ConfigData
        {
            public PlaneSettings PlaneSettings { get; set; }
            public LootSettings LootSettings { get; set; }
            public Messaging Messages { get; set; }
            public Timers EventTimers { get; set; }
        }
        private void LoadVariables()
        {
            LoadConfigVariables();
            SaveConfig();
        }
        protected override void LoadDefaultConfig()
        {
            var config = new ConfigData
            {
                EventTimers = new Timers
                {
                    MaximumMinutesBetweenCalls = 60,
                    MinimumMinutesBetweenCalls = 45,
                    UseRandomCrashTimer = true
                },
                PlaneSettings = new PlaneSettings
                {
                    ApplyToAllPlanes = false,
                    DeploySmokeOnCrashSite = true,
                    FlightSpeed = 35f,
                    HeightModifier = 0f,
                    RocketHitsToDestroy = 3,
                    ShowSmokeTrail = true
                },
                LootSettings = new LootSettings
                {
                    Crash_CrateAmount = 3,
                    Crash_SupplyDropAmount = 3,
                    FireLifetime = 300,
                    RocketHit_CrateAmount = 1,
                    RocketHit_SupplyDropAmount = 1,
                    CrateLootTables = new LootTables
                    {
                        MaximumItems = 4,
                        MinimumItems = 1,
                        LootItems = new List<LootItem>
                        {
                            new LootItem {Shortname = "metal.refined", MaximumAmount = 100, MinimumAmount = 10 },
                            new LootItem {Shortname = "explosive.timed", MaximumAmount = 2, MinimumAmount = 1 },
                            new LootItem {Shortname = "grenade.f1", MaximumAmount = 3, MinimumAmount = 1 },
                            new LootItem {Shortname = "supply.signal", MaximumAmount = 1, MinimumAmount = 1 },
                            new LootItem {Shortname = "cctv.camera", MaximumAmount = 1, MinimumAmount = 1 },
                            new LootItem {Shortname = "targeting.computer", MaximumAmount = 1, MinimumAmount = 1 },
                            new LootItem {Shortname = "ammo.rifle", MaximumAmount = 60, MinimumAmount = 20 },
                            new LootItem {Shortname = "ammo.pistol", MaximumAmount = 60, MinimumAmount = 20 }
                        },
                        UseLootTable = false
                    },
                    SupplyDropLootTables = new LootTables
                    {
                        MaximumItems = 6,
                        MinimumItems = 2,
                        LootItems = new List<LootItem>
                        {
                            new LootItem {Shortname = "rifle.ak", MaximumAmount = 1, MinimumAmount = 1 },
                            new LootItem {Shortname = "pistol.m92", MaximumAmount = 1, MinimumAmount = 1 },
                            new LootItem {Shortname = "pistol.semiauto", MaximumAmount = 1, MinimumAmount = 1 },
                            new LootItem {Shortname = "shotgun.double", MaximumAmount = 1, MinimumAmount = 1 },
                            new LootItem {Shortname = "smg.thompson", MaximumAmount = 1, MinimumAmount = 1 },
                            new LootItem {Shortname = "rifle.bolt", MaximumAmount = 1, MinimumAmount = 1 }
                        },
                        UseLootTable = false
                    }
                },
                Messages = new Messaging
                {
                    DestroyMessage = "Самолет с грузом только что упал. Координаты <color=orange>X: {x}, Z: {z}</color>",
                    DisplayDestroyMessage = true,
                    DisplayIncomingMessage = true,
                    IncomingMessage = "Самолет, несущий груз, вот-вот улетит!\nЕсли вы достаточно опытны, вы можете сбить его с помощью ракетницы!"
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion        
    }
}
