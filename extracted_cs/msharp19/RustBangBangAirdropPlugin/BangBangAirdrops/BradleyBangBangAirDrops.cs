using Facepunch;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VLB;

namespace Oxide.Plugins
{
    // var drops = UnityEngine.Object.FindObjectsOfType<SupplyDrop>().ToList();
    [Info("BradleyBangBangAirDrops", "Matta", "1.0")]
    [Description("Creates custom vehilce supply signals")]
    public class BradleyBangBangAirDrops : RustPlugin
    {
        private const string _minicopterPrefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
        private const string _parachutePrefab = "assets/prefabs/misc/parachute/parachute.prefab";
        private const string _boatPrefab = "assets/content/vehicles/boats/rowboat/rowboat.prefab";
        private const string _rhibPrefab = "assets/content/vehicles/boats/rhib/rhib.prefab";
        private const string _scrapHelicopterPrefab = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab";
        private const string _cargoPlanePrefab = "assets/prefabs/npc/cargo plane/cargo_plane.prefab";
        private const string _bradleyAPCPrefab = "assets/prefabs/npc/m2bradley/bradleyapc.prefab";

        private const string _heliDropName = "MINI HELI DROP";
        private const string _scrapHeliDropName = "SCRAP HELI DROP";
        private const string _rowingBoatDropName = "ROWING BOAT DROP";
        private const string _rhibDropName = "RHIB DROP";

        #region Hooks

        /// <summary>
        /// Adds custom vehicle supply signals to certain containers
        /// </summary>
        /// <param name="container"></param>
        private void OnLootSpawn(StorageContainer container)
        {
            // Add custom airdrops to loot containers on spawn
            NextTick(() => { SpawnBangBangSupplySignals(container); });
        }

        /// <summary>
        /// Hook to capture supply signal being thrown. We rename the custom supply signals which can
        /// be listened for here. The correct entity is then determined and spawned
        /// </summary>
        /// <param name="player">The player that threw the 'explosive' device</param>
        /// <param name="entity"></param>
        void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            Interface.Oxide.LogDebug("Explosive thrown");

            // Check if the entity thrown is a supply signal
            if (entity is SupplySignal)
            {
                Interface.Oxide.LogDebug($"Is supply signal: {entity.name}");

                // If it is a supply signal, check if its one of our custom ones (defined by name)
                switch (entity.name.ToUpper())
                {
                    // If the name of the supply drop matches the mini heli drop, spawn mini heli cargo
                    case _heliDropName:
                        CallSpecializedCargoPlane(entity as SupplySignal, CargoType.MiniHeli);
                        break;
                    // If the name of the supply drop matches the scrap heli drop, spawn scrap heli cargo
                    case _scrapHeliDropName:
                        CallSpecializedCargoPlane(entity as SupplySignal, CargoType.ScrapHeli);
                        break;
                    // If the name of the supply drop matches the rowing boat drop, spawn rowing boat cargo
                    case _rowingBoatDropName:
                        CallSpecializedCargoPlane(entity as SupplySignal, CargoType.Boat);
                        break;
                    // If the name of the supply drop matches the rowing RHIB drop, spawn RHIB cargo
                    case _rhibDropName:
                        CallSpecializedCargoPlane(entity as SupplySignal, CargoType.RHIB);
                        break;
                }
            }
        }

        #endregion

        #region Core

        /// <summary>
        /// Adds custom supply drops to loot boxes
        /// </summary>
        /// <param name="container"></param>
        private void SpawnBangBangSupplySignals(StorageContainer container)
        {
            /*if (container.ShortPrefabName.ToString() != "supply_drop.prefab") {
                if (_chance >= UnityEngine.Random.Range(0f, 100f)) // 50 -> 50%
                {
                    var amount = Core.Random.Range(_amountMin, _amountMax + 1);
                    var item = ItemManager.CreateByName(_supplySignalShortName, amount, 0);

                    if (item != null)
                    {
                        item.name = _heliName;
                        item.blueprintTarget = 0;
                        container.inventory.capacity++;
                        item.MoveToContainer(container.inventory);
                    }
                }
            }*/

            /*
            var item1 = ItemManager.CreateByName("supply.signal", 1, 0);
            if (item1 != null)
            {
                item1.name = _heliDropName;
                item1.text = _heliDropName;
                item1.blueprintTarget = 0;
                container.inventory.capacity++;
                item1.MoveToContainer(container.inventory);
            }

            var item2 = ItemManager.CreateByName("supply.signal", 1, 0);
            if (item2 != null)
            {
                item2.name = _scrapHeliDropName;
                item2.text = _heliDropName;
                item2.blueprintTarget = 0;
                container.inventory.capacity++;
                item2.MoveToContainer(container.inventory);
            }
            */
        }

        /// <summary>
        /// Method to create cargo plane that drops our custom airdrops
        /// </summary>
        /// <param name="supplySignal">Supply signal that has initiated the custom airdrop (used for position & to cancel default airdrop)</param>
        /// <param name="cargoType">The type of cargo to drop (custom airdrop)</param>
        private void CallSpecializedCargoPlane(SupplySignal supplySignal, CargoType cargoType)
        {
            // Stop the normal cargo plane
            var cargoPlane = supplySignal.GetComponent<CargoPlane>();
            if (cargoPlane == null)
                Interface.Oxide.LogDebug("Cargo plane not found");
            NextTick(() => { cargoPlane?.Kill(); });

            // REMOVE WHEN FINISHED<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
            var planes = UnityEngine.Object.FindObjectsOfType<CargoPlane>().ToList();
            Interface.Oxide.LogDebug($"Found {planes?.Count ?? 0} active cargo planes");
            foreach (var plane in planes)
            {
                Interface.Oxide.LogDebug($"Killing plane: {plane.name}");
                NextTick(() => { plane?.Kill(); });
            }

            // Service to create custom airdrops (type defined in constructor)
            var vehicleDroppingService = new VehicleDroppingService(cargoType);
            // Put the special cargo plane into action
            vehicleDroppingService.CallPlane(supplySignal.transform.position);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Calls a mini heli vehicle airdrop
        /// </summary>
        /// <param name="player">The calling player (who ran command)</param>
        /// <param name="command">The command run</param>
        /// <param name="args">Arguments supplied after initial command</param>
        [ChatCommand("bradleybbairdrop")]
        private void CallBradleyAirdrop(BasePlayer player, string command, string[] args)
        {
            // Setup vehicle dropping service and call plane
            var vehicleDroppingService = new VehicleDroppingService(CargoType.BradleyAPC);
            vehicleDroppingService.CallPlane(player.transform.position);
        }

        /// <summary>
        /// Calls a mini heli vehicle airdrop
        /// </summary>
        /// <param name="player">The calling player (who ran command)</param>
        /// <param name="command">The command run</param>
        /// <param name="args">Arguments supplied after initial command</param>
        [ChatCommand("minihelibbairdrop")]
        private void CallMiniHeliAirdrop(BasePlayer player, string command, string[] args)
        {
            // Setup vehicle dropping service and call plane
            var vehicleDroppingService = new VehicleDroppingService(CargoType.MiniHeli);
            vehicleDroppingService.CallPlane(player.transform.position);
        }

        /// <summary>
        /// Calls a scrap heli vehicle airdrop
        /// </summary>
        /// <param name="player">The calling player (who ran command)</param>
        /// <param name="command">The command run</param>
        /// <param name="args">Arguments supplied after initial command</param>
        [ChatCommand("scrapbbairdrop")]
        private void CallScrapHeliAirdrop(BasePlayer player, string command, string[] args)
        {
            // Setup vehicle dropping service and call plane
            var vehicleDroppingService = new VehicleDroppingService(CargoType.ScrapHeli);
            vehicleDroppingService.CallPlane(player.transform.position);
        }

        /// <summary>
        /// Calls a scrap heli vehicle airdrop
        /// </summary>
        /// <param name="player">The calling player (who ran command)</param>
        /// <param name="command">The command run</param>
        /// <param name="args">Arguments supplied after initial command</param>
        [ChatCommand("boatbbairdrop")]
        private void CallBoatAirdrop(BasePlayer player, string command, string[] args)
        {
            // Setup vehicle dropping service and call plane
            var vehicleDroppingService = new VehicleDroppingService(CargoType.Boat);
            vehicleDroppingService.CallPlane(player.transform.position);
        }

        /// <summary>
        /// Calls a scrap heli vehicle airdrop
        /// </summary>
        /// <param name="player">The calling player (who ran command)</param>
        /// <param name="command">The command run</param>
        /// <param name="args">Arguments supplied after initial command</param>
        [ChatCommand("rhibbbairdrop")]
        private void CallRHIBAirdrop(BasePlayer player, string command, string[] args)
        {
            // Setup vehicle dropping service and call plane
            var vehicleDroppingService = new VehicleDroppingService(CargoType.RHIB);
            vehicleDroppingService.CallPlane(player.transform.position);
        }

        #endregion

        #region Helper Classes

        private class APCController : MonoBehaviour
        {
            protected internal BradleyAPC entity { get; private set; }
            private bool isDying = false;
            private void Awake()
            {
                /*
                var paths = UnityEngine.Object.FindObjectsOfType<TerrainPath>();
                var allRoads = paths.Select(x => x.Roads);
                foreach (var road in allRoads)
                {
                    foreach (var path in road)
                    {
                        var eachPathPoints = path.Path.Points;
                    }
                }
                */

                entity = GetComponent<BradleyAPC>();
                entity.enabled = true;
                entity.ClearPath();
                entity.IsAtFinalDestination();
                entity.searchRange = 100f;
                entity.maxCratesToSpawn = 5;
                entity._maxHealth = 300;
                entity.health = 300;
            }

            private void OnDestroy()
            {
                //if (entity != null && !entity.IsDestroyed)
                //    entity.Kill();
            }

            public void ManageDamage(HitInfo info)
            {
                if (isDying)
                    return;

                if (info.damageTypes.Total() >= entity.health)
                {
                    info.damageTypes = new Rust.DamageTypeList();
                    info.HitEntity = null;
                    info.HitMaterial = 0;
                    info.PointStart = Vector3.zero;

                    OnDeath();
                }
            }

            private void RemoveCrate(LockedByEntCrate crate)
            {
                if (crate == null || (crate?.IsDestroyed ?? true))
                {
                    return;
                }
                crate.Kill();
            }

            private void OnDeath()
            {
                isDying = true;
                Effect.server.Run(entity.explosionEffect.resourcePath, entity.transform.position, Vector3.up, null, true);

                List<ServerGib> serverGibs = ServerGib.CreateGibs(entity.servergibs.resourcePath, entity.gameObject, entity.servergibs.Get().GetComponent<ServerGib>()._gibSource, Vector3.zero, 3f);

                for (int i = 0; i < 12 - entity.maxCratesToSpawn; i++)
                {
                    BaseEntity fireBall = GameManager.server.CreateEntity(entity.fireBall.resourcePath, entity.transform.position, entity.transform.rotation, true);
                    if (fireBall)
                    {
                        Vector3 onSphere = UnityEngine.Random.onUnitSphere;
                        fireBall.transform.position = (entity.transform.position + new Vector3(0f, 1.5f, 0f)) + (onSphere * UnityEngine.Random.Range(-4f, 4f));
                        Collider collider = fireBall.GetComponent<Collider>();
                        fireBall.Spawn();
                        fireBall.SetVelocity(Vector3.zero + (onSphere * UnityEngine.Random.Range(3, 10)));
                        foreach (ServerGib serverGib in serverGibs)
                            Physics.IgnoreCollision(collider, serverGib.GetCollider(), true);
                    }
                }

                if (entity != null && !entity.IsDestroyed)
                    entity.Kill(BaseNetworkable.DestroyMode.Gib);
            }
        }

        /// <summary>
        /// Service to create custom cargo that drops custom airdrop type
        /// </summary>
        private class VehicleDroppingService
        {
            /// <summary>
            /// The cargo type to drop
            /// </summary>
            private CargoType _cargoType;

            /// <summary>
            /// The specialized plane
            /// </summary>
            private SpecialAirdropPlane _airbornePlane;

            /// <summary>
            /// Create cargo plane
            /// </summary>
            /// <returns></returns>
            private CargoPlane CreatePlane() => (CargoPlane)GameManager.server.CreateEntity(_cargoPlanePrefab, new Vector3(), new Quaternion(), true);

            /// <summary>
            /// Service constructor which defines which cargo type the cargo plane will drop
            /// </summary>
            /// <param name="cargoType"></param>
            public VehicleDroppingService(CargoType cargoType)
            {
                _cargoType = cargoType;
            }

            /// <summary>
            /// Calls custom cargo plane into action
            /// </summary>
            /// <param name="targetPosition">Position to drop item to</param>
            public CargoPlane CallPlane(Vector3 targetPosition)
            {
                // Create cargo 
                CargoPlane cargoPlane = CreatePlane();
                cargoPlane.Spawn();
                cargoPlane.name = "Bang Bang Cargo Plane";
                cargoPlane._name = "Bang Bang Cargo Plane";

                // If we don't specify the target position, then a random position is used
                if (targetPosition == null)
                    targetPosition = cargoPlane.RandomDropPosition();

                // Create custom cargo plane, init and set custom airdrop on position reached
                _airbornePlane = cargoPlane.gameObject.AddComponent<SpecialAirdropPlane>();
                _airbornePlane.InitializeFlightPath(targetPosition);
                _airbornePlane.PositionReached += PositionReached;

                return cargoPlane;
            }

            /// <summary>
            /// Drop the custom airdrop determined during the initialization of this service (from constructor arg)
            /// </summary>
            /// <param name="sender">The sender (event)</param>
            /// <param name="e">Arguments supplied along with event</param>
            private void PositionReached(object sender, EventArgs e)
            {
                // Determine which airdrop to create (predefined on init)
                switch (_cargoType)
                {
                    // Create mini heli
                    case CargoType.MiniHeli:
                        EntityFactory.CreateMiniCopter(_airbornePlane.transform.position);
                        break;
                    // Create scrap heli
                    case CargoType.ScrapHeli:
                        EntityFactory.CreateScrapHelicopter(_airbornePlane.transform.position);
                        break;
                    // Create rowing boat
                    case CargoType.Boat:
                        EntityFactory.CreateRowingBoat(_airbornePlane.transform.position);
                        break;
                    // Create RHIB
                    case CargoType.RHIB:
                        EntityFactory.CreateRHIB(_airbornePlane.transform.position);
                        break;
                    // Create BradleyAPC
                    case CargoType.BradleyAPC:
                        EntityFactory.CreateBradelyAPC(_airbornePlane.transform.position);
                        break;
                }
            }
        }

        /// <summary>
        /// Creates entities
        /// </summary>
        private class EntityFactory
        {
            /// <summary>
            /// Creates a mini copter entity
            /// </summary>
            /// <param name="position">Position to spawn heli drop</param>
            public static void CreateMiniCopter(Vector3 position) => CreateEntity<MiniCopter>(_minicopterPrefab, position);

            /// <summary>
            /// Creates a scrap helicopter entity
            /// </summary>
            /// <param name="position">Position to spawn scrap heli drop</param>
            public static void CreateScrapHelicopter(Vector3 position) => CreateEntity<ScrapTransportHelicopter>(_scrapHelicopterPrefab, position);

            /// <summary>
            /// Creates a scrap helicopter entity
            /// </summary>
            /// <param name="position">Position to spawn scrap heli drop</param>
            public static void CreateRowingBoat(Vector3 position) => CreateEntity<MotorRowboat>(_boatPrefab, position);

            /// <summary>
            /// Creates a scrap helicopter entity
            /// </summary>
            /// <param name="position">Position to spawn scrap heli drop</param>
            public static void CreateRHIB(Vector3 position) => CreateEntity<RHIB>(_rhibPrefab, position);

            /// <summary>
            /// Creates a mini copter entity and adds to supply drop
            /// </summary>
            /// <param name="prefabLocation">Location of prefab to create</param>
            /// <param name="position">Position to spawn heli drop</param>
            public static void CreateEntity<T>(string prefabLocation, Vector3 position) where T : BaseEntity
            {
                // Create copter
                T entity = (T)GameManager.server.CreateEntity(prefabLocation, position, default(Quaternion), true);
                InitializeEntity(entity);
            }

            /// <summary>
            /// Create, spawn and set dropping vehicle component in entity
            /// </summary>
            /// <param name="entity">Entity to setup</param>
            private static void InitializeEntity(BaseEntity entity)
            {
                // If there was an error - return
                if (entity == null)
                    return;

                entity.Spawn();

                // Add parachute and propagate 
                entity.GetOrAddComponent<DroppingVehicle>();
                entity.SetFlag(BaseEntity.Flags.On, true);
                entity.SendNetworkUpdateImmediate();
            }

            public static void CreateBradelyAPC(Vector3 position)
            {
                var randsphere = UnityEngine.Random.onUnitSphere;
                var entpos = (position + new Vector3(0f, 1.5f, 0f)) + (randsphere * UnityEngine.Random.Range(-2f, 3f));

                var entity = GameManager.server.CreateEntity(_bradleyAPCPrefab, entpos, Quaternion.LookRotation(randsphere), true) as BradleyAPC;
                entity.Spawn();

                entity.GetOrAddComponent<DroppingVehicle>();
                entity.GetOrAddComponent<APCController>();
                entity.SendNetworkUpdateImmediate();
            }
        }

        /// <summary>
        /// An override of the standard cargo plane so we can drop our custom vehicles
        /// </summary>
        private class SpecialAirdropPlane : MonoBehaviour
        {
            public event EventHandler PositionReached;
            private CargoPlane _entity;
            private Vector3 _targetPos;
            private Vector3 _startPos;
            private Vector3 _endPos;
            private float _secondsToTake;
            private float _planeSpeed = 160;
            private float _dropDistance = 75f;
            private bool _hasDropped = false;

            protected virtual void OnPositionReached(EventArgs e)
            {
                EventHandler handler = PositionReached;
                if (handler != null)
                    handler(this, e);
            }

            private void Awake()
            {
                _entity = GetComponent<CargoPlane>();

                _entity.dropped = true;
                enabled = false;
            }

            private void Update()
            {
                float xDistance = transform.position.x - _targetPos.x;
                float zDistance = transform.position.z - _targetPos.z;

                if (!_hasDropped && Math.Abs(xDistance) <= _dropDistance && Math.Abs(zDistance) <= _dropDistance)
                {
                    _hasDropped = true;
                    this.OnPositionReached(EventArgs.Empty);
                }
            }

            private void OnDestroy()
            {
                enabled = false;
                CancelInvoke();
                if (_entity != null && !_entity.IsDestroyed)
                    _entity.Kill();
            }

            /// <summary>
            /// Sets start and end points for the cargo plane
            /// </summary>
            /// <param name="targetPos">The target position</param>
            public void InitializeFlightPath(Vector3 targetPos)
            {
                // this._targetPos = (targetPos + new Vector3(UnityEngine.Random.Range(-10, 10), 0, UnityEngine.Random.Range(-10, 10)));
                _targetPos = targetPos;

                float size = TerrainMeta.Size.x;
                float highestPoint = 300;

                _startPos = Vector3Ex.Range(-1f, 1f);
                _startPos.y = 0f;
                _startPos.Normalize();
                _startPos = _startPos * (size * 2f);
                _startPos.y = highestPoint;

                _endPos = _startPos * -1f;
                _endPos.y = _startPos.y;
                _startPos = _startPos + targetPos;
                _endPos = _endPos + targetPos;

                _secondsToTake = (Vector3.Distance(_startPos, _endPos) / _planeSpeed) * UnityEngine.Random.Range(0.95f, 1.05f);

                _entity.transform.position = _startPos;
                _entity.transform.rotation = Quaternion.LookRotation(_endPos - _startPos);

                _entity.startPos = _startPos;
                _entity.endPos = _endPos;
                _entity.dropPosition = targetPos;
                _entity.secondsToTake = _secondsToTake;

                enabled = true;
            }
        }

        /// <summary>
        /// Defined functionality for the vehicle being dropped
        /// </summary>
        private class DroppingVehicle : MonoBehaviour
        {
            /// <summary>
            /// The vehicle being dropped
            /// </summary>
            private BaseEntity _vehicle;

            /// <summary>
            /// Parachute for vehicle
            /// </summary>
            private BaseEntity _parachute;

            /// <summary>
            /// The body of the vehicle to attach parachute to
            /// </summary>
            private Rigidbody _rigidBody;

            /// <summary>
            /// Called on init
            /// </summary>
            private void Awake()
            {
                _vehicle = GetComponent<BaseEntity>();
                _rigidBody = _vehicle.gameObject.GetComponent<Rigidbody>();
            }

            /// <summary>
            /// Called when spawned
            /// </summary>
            private void Start()
            {
                AddParachute();
            }

            /// <summary>
            /// Called when destroyed
            /// </summary>
            private void OnDestroy()
            {
                RemoveParachute();
            }

            /// <summary>
            /// Adds parachute to entity
            /// </summary>
            private void AddParachute()
            {
                // Turns off gravity so that we can customize drop speed
                if (_rigidBody != null)
                    _rigidBody.useGravity = false;

                // Create the parachute
                _parachute = GameManager.server.CreateEntity(_parachutePrefab);

                // If created successfully then the parachute is attached to the vehicle and spawned
                if (_parachute != null)
                {
                    _parachute.SetParent(_vehicle, "parachute_attach");
                    _parachute.Spawn();
                }
            }

            /// <summary>
            /// Removes the parachute from vehicle
            /// </summary>
            private void RemoveParachute()
            {
                // Sets back normal gravity on the vehicles body
                if (_vehicle.IsValid() == true && _rigidBody != null)
                    _rigidBody.useGravity = true;

                // Destroys parachute and ensures garbage collection of entity
                if (_parachute.IsValid() == true)
                {
                    _parachute.Kill();
                    _parachute = null;
                }
            }

            /// <summary>
            /// Called on collision with entity
            /// </summary>
            /// <param name="collision">Collision</param>
            private void OnCollisionEnter(Collision collision)
            {
                // Destroys this dropping vehicle + parachute
                Destroy(this);
            }

            /// <summary>
            /// Called on update - movement of vehicle drop
            /// </summary>
            private void FixedUpdate()
            {
                // Moves vehicle to new position
                if (_parachute.IsValid() == true)
                    _vehicle.transform.position -= new Vector3(0, 10f * Time.deltaTime, 0);
            }
        }

        #endregion

        #region Enums

        /// <summary>
        /// Defines cargo type
        /// </summary>
        public enum CargoType
        {
            /// <summary>
            /// To make the cargo drop a mini heli
            /// </summary>
            MiniHeli = 0,

            /// <summary>
            /// To make the cargo drop a scrap heli
            /// </summary>
            ScrapHeli = 1,

            /// <summary>
            /// To make the cargo drop a boat
            /// </summary>
            Boat = 2,

            /// <summary>
            /// To make the cargo drop a RHIB
            /// </summary>
            RHIB = 4,

            /// <summary>
            /// Spawns Bradely APC
            /// </summary>
            BradleyAPC = 8
        }

        #endregion
    }
}