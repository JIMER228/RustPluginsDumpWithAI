using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Rust;
using System.Linq;
using System.Globalization;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("CarCommander", "k1lly0u", "0.1.87", ResourceId = 0)]
      //  Слив плагинов server-rust by Apolo YouGame
    class CarCommander : RustPlugin
    {
        #region Fields
        [PluginReference]
        Plugin Clans, Friends, Spawns, RandomSpawns;

        RestoreData storedData;
        private DynamicConfigFile data;

        static CarCommander ins;

        private Dictionary<string, string> itemNames = new Dictionary<string, string>();
        private Dictionary<CommandType, BUTTON> controlButtons;

        private List<CarController> saveableCars = new List<CarController>();

        private bool initialized;
        private bool wipeData = false;

        private int fuelType;        
        private int repairType;
        private string fuelTypeName;
        private string repairTypeName;

        const string carPrefab = "assets/content/vehicles/sedan_a/sedantest.entity.prefab";
        const string heliExplosion = "assets/prefabs/npc/patrol helicopter/effects/heli_explosion.prefab";

        const string uiHealth = "CCUI_Health";
        const string uiFuel = "CCUI_Fuelh";
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            permission.RegisterPermission("carcommander.admin", this);
            permission.RegisterPermission("carcommander.use", this);
            permission.RegisterPermission("carcommander.canspawn", this);
            permission.RegisterPermission("carcommander.carspawn", this);

            lang.RegisterMessages(Messages, this);
            data = Interface.Oxide.DataFileSystem.GetFile("carcommander_data");
        }

        private void OnServerInitialized()
        {
            ins = this;
            LoadVariables();
            LoadData();

            ConvertControlButtons();

            fuelType = ItemManager.itemList.Find(x => x.shortname == configData.Fuel.FuelType)?.itemid ?? 0;
            repairType = ItemManager.itemList.Find(x => x.shortname == configData.Repair.Shortname)?.itemid ?? 0;
            fuelTypeName = ItemManager.itemList.Find(x => x.shortname == configData.Fuel.FuelType)?.displayName.english ?? "Invalid fuel shortname set in config!";
            repairTypeName = ItemManager.itemList.Find(x => x.shortname == configData.Repair.Shortname)?.displayName.english ?? "Invalid repair item shortname set in config!";
                       
            initialized = true;

            if (wipeData)
            {
                PrintWarning("Map wipe detected! Wiping previous car data");
                storedData.restoreData.Clear();
                SaveData();
            }

            timer.In(3, RestoreVehicleInventories);
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
      //  Слив плагинов server-rust by Apolo YouGame
        {
            if (entity == null || info == null) return;
            if (entity.GetComponent<CarController>())
            {
                entity.GetComponent<CarController>().ManageDamage(info);
            }
            else if (entity.GetComponent<CarController.InvisibleMount>())
            {
                info.damageTypes = new DamageTypeList();
                info.HitEntity = null;
                info.HitMaterial = 0;
                info.PointStart = Vector3.zero;
            }
        }

        private object OnEntityGroundMissing(BaseEntity entity)
        {
            if (entity.GetComponent<CarController>() || entity.GetComponent<CarController.InvisibleMount>())
                return false;
            return null;
        }

        private void OnHammerHit(BasePlayer player, HitInfo info)
      //  Слив плагинов server-rust by Apolo YouGame
        {
            if (player == null || info == null || info.HitEntity == null || !configData.Repair.Enabled)
                return;

            CarController controller = info.HitEntity.GetComponent<CarController>();
            if (controller != null && controller.entity != null)
            {
                if (controller.entity.health < controller.entity.MaxHealth())
                {
                    if (player.inventory.GetAmount(repairType) >= configData.Repair.Amount)
                    {
                        player.inventory.Take(null, repairType, configData.Repair.Amount);
                        controller.entity.Heal(configData.Repair.Damage);
                        player.Command("note.inv", new object[] { repairType, configData.Repair.Amount * -1 });
                    }
                    else SendReply(player, string.Format(msg("noresources", player.UserIDString), configData.Repair.Amount, repairTypeName));
                }
                else SendReply(player, msg("fullhealth", player.UserIDString));
            }
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (!initialized || player == null) return;

            if (input.WasJustPressed(BUTTON.USE))
            {
                RaycastHit hit;
                if (Physics.SphereCast(player.eyes.position, 0.5f, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward, out hit, 3f))
                {
                    CarController controller = hit.GetEntity()?.GetComponent<CarController>();
                    if (controller != null && controller.HasCommander() && !controller.occupants.Contains(player))
                        CanMountEntity(controller.entity, player);
                }
                return;
            }
            if (configData.Inventory.Enabled && input.WasJustPressed(controlButtons[CommandType.Inventory]))
            {
                RaycastHit hit;
                if (Physics.SphereCast(player.eyes.position, 0.5f, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward, out hit, 3f))
                {
                    CarController controller = hit.GetEntity()?.GetComponent<CarController>();
                    if (controller != null && !controller.HasCommander() && !controller.occupants.Contains(player))
                        OpenInventory(player, controller, controller.inventory);
                }
                return;
            }
            if (configData.Fuel.Enabled && input.WasJustPressed(controlButtons[CommandType.FuelTank]))
            {
                RaycastHit hit;
                if (Physics.SphereCast(player.eyes.position, 0.5f, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward, out hit, 3f))
                {
                    CarController controller = hit.GetEntity()?.GetComponent<CarController>();
                    if (controller != null && !controller.HasCommander() && !controller.occupants.Contains(player))
                        OpenInventory(player, controller, controller.fuelTank);
                }
                return;
            }
        }

        private void OnEntityKill(BaseNetworkable networkable)
        {
            BaseCar baseCar = networkable.GetComponent<BaseCar>();
            if (baseCar != null)
            {
                if (storedData.HasRestoreData(baseCar.net.ID))
                    storedData.RemoveData(baseCar.net.ID);
            }
        }

        private void OnNewSave(string filename) => wipeData = true;

        private void OnServerSave()
        {
            for (int i = saveableCars.Count - 1; i >= 0; i--)
            {
                CarController controller = saveableCars[i];
                if (controller == null || controller.entity == null || !controller.entity.IsValid() || controller.entity.IsDestroyed)
                {
                    saveableCars.RemoveAt(i);
                    continue;
                }
                storedData.AddData(controller);
            }
            SaveData();
        }

        private void Unload()
        {
            var objects = UnityEngine.Object.FindObjectsOfType<CarController>();
            if (objects != null)
            {
                foreach (var obj in objects)
                    UnityEngine.Object.Destroy(obj);
            }
        }

        private object CanPickupEntity(BaseCombatEntity entity, BasePlayer player)
        {
            if (entity.GetComponent<CarController.InvisibleMount>())
                return false;
            return null;
        }

        private void OnEntityDismounted(BaseMountable mountable, BasePlayer player)
        {
            CarController controller = mountable.GetComponent<CarController>();
            if (controller != null && controller.Commander?.userID == player.userID)
            {
                controller.Commander = null;
                controller.mountPoints[0].OnEntityDismounted();
                return;
            }

            CarController.InvisibleMount invisibleMount = mountable.GetComponent<CarController.InvisibleMount>();
            if (invisibleMount != null)
                invisibleMount.MountPosition.OnEntityDismounted();
        }

        private object CanMountEntity(BaseMountable mountable, BasePlayer player)
        {
            CarController controller = mountable.GetComponent<CarController>();
            if (controller != null)
            {
                if (player.isMounted)
                    return false;

                if (controller.externallyManaged)
                    return false;

                if (controller.isDieing)
                    return false;

                if (!permission.UserHasPermission(player.UserIDString, "carcommander.use"))
                {
                    SendReply(player, msg("nopermission", player.UserIDString));
                    return false;
                }

                CarController.MountPoint mountPoint = controller.GetClosestMountPoint(player.transform.position);
                if (mountPoint != null && !mountPoint.Entity.IsMounted())
                {
                    if (mountPoint.IsDriver)
                    {
                        MountPlayer(mountable, mountPoint, player);
                        return false;
                    }
                    else
                    {
                        if (configData.Passengers.Enabled)
                        {
                            BasePlayer commander = controller.Commander;

                            if (commander == null)
                            {
                                MountPlayer(mountable, mountPoint, player);
                                return false;
                            }
                            else
                            {
                                if (!configData.Passengers.UseFriends && !configData.Passengers.UseClans)
                                {
                                    MountPlayer(mountable, mountPoint, player);
                                    return false;
                                }

                                if (configData.Passengers.UseFriends && AreFriends(commander.userID, player.userID))
                                {
                                    MountPlayer(mountable, mountPoint, player);
                                    return false;
                                }

                                if (configData.Passengers.UseClans && IsClanmate(commander.userID, player.userID))
                                {
                                    MountPlayer(mountable, mountPoint, player);
                                    return false;
                                }

                                SendReply(player, msg("not_friend", player.UserIDString));
                                return false;
                            }
                        }
                        else
                        {
                            SendReply(player, msg("not_enabled", player.UserIDString));
                            return false;
                        }
                    }
                }
            }

            CarController.InvisibleMount invisibleMount = mountable.GetComponent<CarController.InvisibleMount>();
            if (invisibleMount != null)
            {
                if (configData.Passengers.Enabled)
                {
                    BasePlayer commander = invisibleMount.MountPosition.Controller.Commander;

                    if (commander != null)
                    {
                        if (!configData.Passengers.UseFriends && !configData.Passengers.UseClans)
                            return true;

                        if (configData.Passengers.UseFriends && AreFriends(commander.userID, player.userID))
                            return true;

                        if (configData.Passengers.UseClans && IsClanmate(commander.userID, player.userID))
                            return true;

                        SendReply(player, msg("not_friend", player.UserIDString));
                        return false;
                    }
                }
                else
                {
                    SendReply(player, msg("not_enabled", player.UserIDString));
                    return false;
                }
            }
            return null;
        }

        private object CanDismountEntity(BaseMountable mountable, BasePlayer player)
        {
            CarController controller = mountable.GetComponent<CarController>();
            if (controller != null)
            {
                if (controller.externallyManaged)
                    return false;
            }

            CarController.InvisibleMount invisibleMount = mountable.GetComponent<CarController.InvisibleMount>();
            if (invisibleMount != null)
            {
                invisibleMount.MountPosition.DismountPlayer();
                return false;
            }
            return null;
        }
        #endregion

        #region Functions
        private T ParseType<T>(string type)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), type, true);
            }
            catch
            {
                PrintError($"INVALID CONFIG OPTION DETECTED! The value \"{type}\" is an incorrect selection.\nAvailable options are: {Enum.GetNames(typeof(T)).ToSentence()}");
                return default(T);
            }
        }
        private bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm) || permission.UserHasPermission(player.UserIDString, "Carcommander.admin");

        private void ConvertControlButtons()
        {
            controlButtons = new Dictionary<CommandType, BUTTON>
            {
                [CommandType.Accelerate] = ParseType<BUTTON>(configData.Buttons.Accelerate),
                [CommandType.Brake] = ParseType<BUTTON>(configData.Buttons.Brake),
                [CommandType.Inventory] = ParseType<BUTTON>(configData.Buttons.Inventory),
                [CommandType.Left] = ParseType<BUTTON>(configData.Buttons.Left),
                [CommandType.Right] = ParseType<BUTTON>(configData.Buttons.Right),
                [CommandType.Handbrake] = ParseType<BUTTON>(configData.Buttons.HBrake),
                [CommandType.Lights] = ParseType<BUTTON>(configData.Buttons.Lights),
                [CommandType.FuelTank] = ParseType<BUTTON>(configData.Buttons.FuelTank)
            };
        }

        private void OpenInventory(BasePlayer player, CarController controller, ItemContainer container)
        {
            player.inventory.loot.Clear();
            player.inventory.loot.entitySource = controller.entity;
            player.inventory.loot.itemSource = null;
            player.inventory.loot.AddContainer(container);
            player.inventory.loot.SendImmediate();
            player.ClientRPCPlayer(null, player, "RPC_OpenLootPanel", "generic");
            player.SendNetworkUpdate();
        }

        private void MountPlayer(BaseMountable mountable, CarController.MountPoint mountPoint, BasePlayer player)
        {
            if (mountPoint.IsDriver)
                mountPoint.Controller.Commander = player;

            mountPoint.MountPlayer(player);
        }

        private void RestoreVehicleInventories()
        {
            if (storedData.restoreData.Count > 0)
            {
                BaseEntity[] objects = BaseEntity.saveList.Where(x => x is BaseCar).ToArray();
                if (objects != null)
                {
                    foreach (var obj in objects)
                    {
                        if (!obj.IsValid() || obj.IsDestroyed)
                            continue;

                        if (storedData.HasRestoreData(obj.net.ID))
                        {
                            obj.gameObject.AddComponent<CarController>();
                        }
                    }
                }
            }
            CheckForSpawns();
        }

        private void CheckForSpawns()
        {
            if (configData.Spawnable.Enabled)
            {                
                if (saveableCars.Count < configData.Spawnable.Max)
                {
                    object position = null;
                    if (configData.Spawnable.RandomSpawns)
                    {
                        if (!RandomSpawns)
                        {                            
                            PrintError("RandomSpawns can not be found! Unable to autospawn cars");
                            return;
                        }

                        object success = RandomSpawns.Call("GetSpawnPoint");
                        if (success != null)
                            position = (Vector3)success;
                        else PrintError("Unable to find a valid spawnpoint from RandomSpawns");
                    }
                    else
                    {
                        if (!Spawns)
                        {
                            PrintError("Spawns Database can not be found! Unable to autospawn cars");
                            return;
                        }

                        object success = Spawns.Call("GetSpawnsCount", configData.Spawnable.Spawnfile);
                        if (success is string)
                        {
                            PrintError("An invalid spawnfile has been set in the config. Unable to autospawn cars : " + (string)success);
                            return;
                        }

                        success = Spawns.Call("GetRandomSpawn", configData.Spawnable.Spawnfile);
                        if (success is string)
                        {
                            PrintError((string)success);
                            return;
                        }
                        else position = (Vector3)success;
                    }

                    if (position != null)
                    {
                        List<BaseCar> entities = Facepunch.Pool.GetList<BaseCar>();
                        Vis.Entities((Vector3)position, 5f, entities);
                        if (entities.Count > 0)
                            timer.In(60, CheckForSpawns);
                        else
                        {
                            SpawnAtLocation((Vector3)position, new Quaternion(), true);
                            timer.In(configData.Spawnable.Time, CheckForSpawns);
                        }
                        Facepunch.Pool.FreeList(ref entities);
                    }
                }
            }
        }
        #endregion

        #region API
        private BaseEntity SpawnAtLocation(Vector3 position, Quaternion rotation = default(Quaternion), bool enableSaving = false, bool isExternallyManaged = false)
        {
            BaseEntity entity = GameManager.server.CreateEntity(carPrefab, position + Vector3.up, rotation);
            entity.Spawn();

            entity.enableSaving = enableSaving;

            CarController controller = entity.gameObject.AddComponent<CarController>();

            if (enableSaving)
            {
                saveableCars.Add(controller);
                storedData.AddData(controller);
                SaveData();
            }

            if (isExternallyManaged)
                controller.SetExternallyManaged();

            return entity;
        }

        private void ToggleController(BaseCar baseCar, bool enabled)
        {
            if (baseCar == null)
                return;

            CarController controller = baseCar.GetComponent<CarController>();
            if (controller != null)
            {
                controller.enabled = enabled;

                if (!enabled)
                {
                    foreach (var wheel in controller.entity.wheels)
                        wheel.wheelCollider.brakeTorque = 1f;
                }
            }
        }

        private void MountPlayerTo(BasePlayer player, BaseCar baseCar)
        {
            if (player == null)
                return;

            CarController controller = baseCar.GetComponent<CarController>();
            if (controller != null)
            {
                controller.Commander = player;
                controller.mountPoints[0].MountPlayer(player);
            }
        }

        private void EjectAllPlayers(BaseCar baseCar)
        {
            CarController controller = baseCar.GetComponent<CarController>();
            if (controller != null)
                controller.EjectAllPlayers();
        }
        #endregion

        #region Component
        enum CommandType { Accelerate, Brake, Left, Right, Handbrake, Inventory, FuelTank, Lights }

        public class CarController : MonoBehaviour
        {
            #region Variables
            private BasePlayer player;
            public BaseCar entity;
            public ItemContainer inventory;
            public ItemContainer fuelTank;
            private Rigidbody rb;

            private Dictionary<CommandType, BUTTON> cb;

            public List<MountPoint> mountPoints = new List<MountPoint>();
            public List<BasePlayer> occupants = new List<BasePlayer>();

            WheelCollider[] allWheels = new WheelCollider[4];
            WheelFrictionCurve sidewaysFriction = new WheelFrictionCurve();
            WheelFrictionCurve sidewaysFrictionHB = new WheelFrictionCurve();

            private float engineTorque;
            private float brakeTorque;
            private float reverseTorque;

            private float speed;
            private float maxSpeed;

            private float maxSteeringAngleSpeed;
            private float maxSteeringAngle;
            private bool applyCounterSteer;
            private float driftAngle;
            private bool isDrifting;

            public float antiRollFrontHorizontal = 5000f; 
            public float antiRollRearHorizontal = 5000f;
            public float antiRollVertical = 500f;

            private float accelInput = 0f;
            private float brakeInput = 0f;
            private float steerInput = 0f;

            private bool repairEnabled;
            private bool fuelEnabled;
            private int consumptionRate;
            private int fuelId;
            private float nextFuelTick;
            private bool outOfFuel;

            private bool eBrake;
            private bool driftFriction;

            public bool externallyManaged;
            public bool isDieing;
            public bool lightsOn;

            public BasePlayer Commander
            {
                get
                {
                    return player;
                }
                set
                {
                    player = value;
                }
            }
            #endregion
           
            #region Initialization
            private void Awake()
            {
                entity = GetComponent<BaseCar>();
                entity.enabled = false;

                if (entity.IsMounted())
                    entity.DismountAllPlayers();

                SetWheelColliders(); 
                CreatePassengers();
                InitializeSettings();
                InitializeFuel();
                InitializeInventory(); 
            }

            private void SetWheelColliders()
            {
                allWheels = new WheelCollider[] { entity.wheels[0].wheelCollider, entity.wheels[1].wheelCollider, entity.wheels[2].wheelCollider, entity.wheels[3].wheelCollider };

                WheelFrictionCurve forwardFriction = new WheelFrictionCurve();
                forwardFriction.extremumSlip = 0.2f;
                forwardFriction.extremumValue = 1;
                forwardFriction.asymptoteSlip = 0.8f;
                forwardFriction.asymptoteValue = 0.75f;
                forwardFriction.stiffness = 1.5f;

                sidewaysFriction.extremumSlip = 0.2f;
                sidewaysFriction.extremumValue = 0.8f;
                sidewaysFriction.asymptoteSlip = 0.4f;
                sidewaysFriction.asymptoteValue = 0.6f;
                sidewaysFriction.stiffness = 1.0f;

                sidewaysFrictionHB.extremumSlip = 0.15f;
                sidewaysFrictionHB.extremumValue = 0.7f; 
                sidewaysFrictionHB.asymptoteSlip = 0.2f;
                sidewaysFrictionHB.asymptoteValue = 0.5f;
                sidewaysFrictionHB.stiffness = 0.75f;

                var movement = ins.configData.Movement;

                foreach (WheelCollider wc in allWheels)
                {
                    JointSpring spring = wc.suspensionSpring;

                    spring.spring = movement.Spring;
                    spring.damper = movement.Damper;
                    spring.targetPosition = movement.Target;

                    wc.suspensionSpring = spring;
                    wc.suspensionDistance = movement.Distance;
                    wc.forceAppPointDistance = 0.1f;
                    wc.mass = 40f;
                    wc.wheelDampingRate = 1f;
                    
                    wc.sidewaysFriction = sidewaysFriction;
                    wc.forwardFriction = forwardFriction;
                }                
            }

            private void CreatePassengers()
            {
                mountPoints = new List<MountPoint> { new MountPoint(this, new Vector3(-0.5f, 0.16f, 0.57f), new Vector3(-1.2f, 0.1f, 0.5f), true) };
                if (ins.configData.Passengers.Enabled)
                {
                    mountPoints.Add(new MountPoint(this, new Vector3(0.54f, 0.16f, 0.57f), new Vector3(1.2f, 0.1f, 0.5f)));
                    mountPoints.Add(new MountPoint(this, new Vector3(-0.48f, 0.16f, -0.55f), new Vector3(-1.2f, 0.1f, -0.5f)));
                    mountPoints.Add(new MountPoint(this, new Vector3(0.48f, 0.16f, -0.55f), new Vector3(1.2f, 0.1f, -0.5f)));
                }
            }

            private void InitializeSettings()
            {
                rb = entity.GetComponent<Rigidbody>();
                cb = ins.controlButtons;

                var movement = ins.configData.Movement;
                engineTorque = movement.Acceleration;
                brakeTorque = movement.Brakes;
                reverseTorque = movement.Reverse;

                maxSteeringAngle = movement.Steer;
                maxSteeringAngleSpeed = movement.SteerSpeed;
                applyCounterSteer = movement.CounterSteer;

                antiRollFrontHorizontal = movement.AntiRollFH;
                antiRollRearHorizontal = movement.AntiRollRH;
                antiRollVertical = movement.AntiRollV;

                maxSpeed = movement.Speed;

                repairEnabled = ins.configData.Repair.Enabled;
            }

            private void InitializeFuel()
            {
                var fuel = ins.configData.Fuel;
                fuelId = ins.fuelType;
                fuelEnabled = fuel.Enabled;
                consumptionRate = fuel.Consumption;

                if (fuelEnabled)
                {
                    fuelTank = new ItemContainer();
                    fuelTank.ServerInitialize(null, 1);
                    if ((int)fuelTank.uid == 0)
                        fuelTank.GiveUID();
                    fuelTank.onlyAllowedItem = ItemManager.itemList.Find(x => x.itemid == fuelId);

                    if (fuel.GiveFuel && !ins.storedData.HasRestoreData(entity.net.ID))
                    {
                        Item fuelItem = ItemManager.CreateByItemID(fuelId, UnityEngine.Random.Range(fuel.FuelAmountMin, fuel.FuelAmountMax));
                        fuelItem.MoveToContainer(fuelTank);
                    }
                }

                outOfFuel = fuelEnabled ? fuelTank.GetAmount(fuelId, false) < 1 : false;
            }

            private void InitializeInventory()
            {
                if (ins.configData.Inventory.Enabled)
                {
                    inventory = new ItemContainer();
                    inventory.ServerInitialize(null, ins.configData.Inventory.Size);
                    if ((int)inventory.uid == 0)
                        inventory.GiveUID();
                }

                if (ins.storedData.HasRestoreData(entity.net.ID))
                {
                    ins.storedData.RestoreInventory(this);
                    ins.saveableCars.Add(this);
                }
            }
            #endregion

            #region Input
            private void FixedUpdate()
            {
                foreach (var point in mountPoints)
                {
                    point.UpdatePosition();                    
                }

                if (WaterLevel.Factor(entity.WorldSpaceBounds().ToBounds()) > 0.7f)
                {
                    enabled = false;
                    if (externallyManaged)
                        Interface.CallHook("OnVehicleUnderwater", entity);
                    else StopToDie();
                    return;
                }

                if (player == null)
                {
                    accelInput = 0f;
                    brakeInput = 0.5f;
                }
                else
                {         
                    rb.drag = rb.velocity.magnitude / 250;

                    if (player.serverInput.WasJustPressed(cb[CommandType.Lights]))
                        ToggleLights();

                    eBrake = player.serverInput.IsDown(cb[CommandType.Handbrake]);

                    if (player.serverInput.IsDown(cb[CommandType.Accelerate]))
                    {
                        accelInput = 1f;
                        brakeInput = 0f;
                    }
                    else if (player.serverInput.IsDown(cb[CommandType.Brake]))
                    {
                        brakeInput = 1f;
                        accelInput = 0f;
                    }
                    else
                    {
                        brakeInput = 0f;
                        accelInput = 0f;
                    }

                    steerInput = player.serverInput.IsDown(cb[CommandType.Left]) ? -1f : player.serverInput.IsDown(cb[CommandType.Right]) ? 1f : 0f;

                    if (fuelEnabled)
                        CheckFuel(accelInput > 0);
                }

                speed = rb.velocity.magnitude * 3.6f;

                ApplyAcceleration();
                AntiRollBars();
                CheckForDrift();
                AdjustSteering();                

                entity.SetFlag(BaseEntity.Flags.Reserved1, player != null && !outOfFuel, false);
            }
            #endregion

            #region Movement
            private void ApplyAcceleration()
            {
                if (accelInput > 0 && speed > maxSpeed)
                    accelInput = 0;

                float velocity = rb.velocity.magnitude * Vector3.Dot(rb.velocity.normalized, entity.transform.forward);

                float _motorTorque = outOfFuel ? 0 : engineTorque * accelInput;
                float _reverseTorque = outOfFuel ? 0 : -reverseTorque;
                float _brakeTorque = brakeTorque * brakeInput;

                if (velocity < 0.01f && brakeInput > 0.5f)
                {
                    for (int i = 0; i < allWheels.Length; i++)
                    {
                        WheelCollider wc = allWheels[i];
                        wc.brakeTorque = 0;
                        if (i < 2)
                            wc.motorTorque = _reverseTorque * 0.8f;
                        else wc.motorTorque = _reverseTorque;
                    } 
                }
                else
                {
                    if (eBrake)
                    {
                        for (int i = 2; i < allWheels.Length; i++)
                        {
                            WheelCollider wc = allWheels[i];
                            wc.motorTorque = 0;
                            wc.brakeTorque = brakeTorque;

                            if (steerInput != 0 && allWheels[i].isGrounded)
                            {
                                rb.angularVelocity = new Vector3(rb.angularVelocity.x, rb.angularVelocity.y + (steerInput / 60f), rb.angularVelocity.z);
                                wc.sidewaysFriction = sidewaysFrictionHB;
                                driftFriction = true;
                            }
                        }                       
                    }
                    else
                    {
                        for (int i = 0; i < allWheels.Length; i++)
                        {
                            WheelCollider wc = allWheels[i];
                            if (i > 1 && driftFriction)
                            {
                                wc.sidewaysFriction = sidewaysFriction;
                                driftFriction = false;
                            }
                            wc.motorTorque = _motorTorque;
                            wc.brakeTorque = _brakeTorque;                            
                        }
                    }

                    entity.SetFlag(BaseEntity.Flags.Reserved3, (velocity > 0f && accelInput < 0f) || (velocity < 0f && brakeInput > 0f), false);
                }
            }

            private void AdjustSteering()
            {               
                var steerAngle = Mathf.Lerp(maxSteeringAngle, maxSteeringAngleSpeed, (speed / maxSpeed)) * steerInput;

                if (applyCounterSteer)
                    steerAngle = Mathf.Clamp((steerAngle * (steerInput + driftAngle)), -steerAngle, steerAngle);

                allWheels[0].steerAngle = steerAngle;
                allWheels[1].steerAngle = steerAngle;

                entity.SetFlag(BaseEntity.Flags.Reserved4, steerInput == -1f, false);
                entity.SetFlag(BaseEntity.Flags.Reserved5, steerInput == 1f, false);
            }

            private void AntiRollBars()
            {                
                WheelHit FrontWheelHit;

                float travelFL = 1.0f;
                float travelFR = 1.0f;

                bool groundedFL = allWheels[0].GetGroundHit(out FrontWheelHit);

                if (groundedFL)
                    travelFL = (-allWheels[0].transform.InverseTransformPoint(FrontWheelHit.point).y - allWheels[0].radius) / allWheels[0].suspensionDistance;

                bool groundedFR = allWheels[1].GetGroundHit(out FrontWheelHit);

                if (groundedFR)
                    travelFR = (-allWheels[1].transform.InverseTransformPoint(FrontWheelHit.point).y - allWheels[1].radius) / allWheels[1].suspensionDistance;

                float antiRollForceFrontHorizontal = (travelFL - travelFR) * antiRollFrontHorizontal;

                if (groundedFL)
                    rb.AddForceAtPosition(allWheels[0].transform.up * -antiRollForceFrontHorizontal, allWheels[0].transform.position);
                if (groundedFR)
                    rb.AddForceAtPosition(allWheels[1].transform.up * antiRollForceFrontHorizontal, allWheels[1].transform.position);

                WheelHit RearWheelHit;

                float travelRL = 1.0f;
                float travelRR = 1.0f;

                bool groundedRL = allWheels[2].GetGroundHit(out RearWheelHit);

                if (groundedRL)
                    travelRL = (-allWheels[2].transform.InverseTransformPoint(RearWheelHit.point).y - allWheels[2].radius) / allWheels[2].suspensionDistance;

                bool groundedRR = allWheels[3].GetGroundHit(out RearWheelHit);

                if (groundedRR)
                    travelRR = (-allWheels[3].transform.InverseTransformPoint(RearWheelHit.point).y - allWheels[3].radius) / allWheels[3].suspensionDistance;

                float antiRollForceRearHorizontal = (travelRL - travelRR) * antiRollRearHorizontal;

                if (groundedRL)
                    rb.AddForceAtPosition(allWheels[2].transform.up * -antiRollForceRearHorizontal, allWheels[2].transform.position);
                if (groundedRR)
                    rb.AddForceAtPosition(allWheels[3].transform.up * antiRollForceRearHorizontal, allWheels[3].transform.position);
                                
                float antiRollForceFrontVertical = (travelFL - travelRL) * antiRollVertical;

                if (groundedFL)
                    rb.AddForceAtPosition(allWheels[0].transform.up * -antiRollForceFrontVertical, allWheels[0].transform.position);
                if (groundedRL)
                    rb.AddForceAtPosition(allWheels[2].transform.up * antiRollForceFrontVertical, allWheels[2].transform.position);

                float antiRollForceRearVertical = (travelFR - travelRR) * antiRollVertical;

                if (groundedFR)
                    rb.AddForceAtPosition(allWheels[1].transform.up * -antiRollForceRearVertical, allWheels[1].transform.position);
                if (groundedRR)
                    rb.AddForceAtPosition(allWheels[3].transform.up * antiRollForceRearVertical, allWheels[3].transform.position);                                
            }

            private void CheckForDrift()
            {

                WheelHit hit;
                allWheels[3].GetGroundHit(out hit);

                if (speed > 1f && isDrifting && !eBrake)
                    driftAngle = hit.sidewaysSlip * 1f;
                else
                    driftAngle = 0f;

                if (Mathf.Abs(hit.sidewaysSlip) > .25f)
                    isDrifting = true;
                else
                    isDrifting = false;

            }
            #endregion

            #region Functions
            private void ToggleLights()
            {
                lightsOn = !lightsOn;
                entity.SetFlag(BaseEntity.Flags.Reserved2, lightsOn, false);
            }

            private void CheckFuel(bool isAccelerating)
            {
                if (outOfFuel)
                {
                    if (fuelTank.GetAmount(fuelId, true) > 0)
                    {
                        outOfFuel = false;

                        if (player != null)
                            ins.CreateFuelUI(player, this);
                    }
                }
                else
                {
                    nextFuelTick += isAccelerating ? Time.deltaTime : Time.deltaTime / 2;

                    if (nextFuelTick >= consumptionRate)
                    {
                        nextFuelTick = 0f;

                        fuelTank.Take(null, fuelId, 1);

                        if (fuelTank.GetAmount(fuelId, true) < 1)
                            outOfFuel = true;

                        if (player != null)
                            ins.CreateFuelUI(player, this);
                    }
                }
            }
           
            public bool HasCommander() => player != null;

            public MountPoint GetClosestMountPoint(Vector3 position)
            {
                MountPoint mountPoint = null;
                float closestDistance = 10f;

                foreach (var point in mountPoints)
                {
                    float distance = Vector3.Distance(point.Position, position);
                    if (distance < closestDistance)
                    {
                        if (point.Entity.IsMounted())
                            continue;

                        mountPoint = point;
                        closestDistance = distance;
                    }
                }
                return mountPoint;
            }
            #endregion

            #region Damage and Destruction
            private void OnDestroy()
            {
                EjectAllPlayers();
                DestroyAllMounts();                

                if (entity != null && !entity.IsDestroyed && !entity.enableSaving)
                    entity.Kill();
            }

            public void EjectAllPlayers()
            {
                foreach (MountPoint point in mountPoints)
                    point.DismountPlayer();
            }

            private void DestroyAllMounts()
            {
                foreach (MountPoint point in mountPoints)
                {
                    InvokeHandler.CancelInvoke(entity, point.UpdateHeldItems);
                    point.DestroyMountPoint();
                }
            }           

            public void ManageDamage(HitInfo info)
      //  Слив плагинов server-rust by Apolo YouGame
            {
                if (info.damageTypes.GetMajorityDamageType() == DamageType.Bullet)
                    info.damageTypes.ScaleAll(200);

                if (info.damageTypes.Total() >= entity.health)
                {
                    info.damageTypes = new DamageTypeList();
                    info.HitEntity = null;
                    info.HitMaterial = 0;
                    info.PointStart = Vector3.zero;
                    OnDeath();
                    return;
                }
                foreach (var occupant in occupants)
                    ins.CreateHealthUI(occupant, this);
            }

            public void StopToDie()
            {
                enabled = false;
                isDieing = true;

                entity.SetFlag(BaseEntity.Flags.Reserved1, false, false);

                foreach (var wheel in entity.wheels)
                {
                    wheel.wheelCollider.motorTorque = 0;
                    wheel.wheelCollider.brakeTorque = float.MaxValue;
                }

                rb.velocity = Vector3.zero;

                EjectAllPlayers();
                entity.Invoke(OnDeath, 5f);
            }
           
            private void OnDeath()
            {
                EjectAllPlayers();

                Effect.server.Run(heliExplosion, transform.position);
                if (ins.configData.Inventory.DropInv)
                {
                    inventory.Drop("assets/prefabs/misc/item drop/item_drop.prefab", transform.position + Vector3.up + (UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(2f, 3f)), new Quaternion());
                    fuelTank.Drop("assets/prefabs/misc/item drop/item_drop.prefab", transform.position + Vector3.up + (UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(2f, 3f)), new Quaternion());
                }

                ins.NextTick(() =>
                {
                    if (entity != null && !entity.IsDestroyed)
                        entity.DieInstantly();
                });
            }
            #endregion

            #region External Toggles
            public void SetExternallyManaged()
            {
                externallyManaged = true;
                repairEnabled = false;
                DisableFuelConsumption();
            }

            public void DisableFuelConsumption()
            {
                if (fuelEnabled)
                {
                    fuelEnabled = false;
                    outOfFuel = false;

                    foreach (var occupant in occupants)
                        ins.DestroyUI(occupant, uiFuel);
                }
            }
            #endregion

            public class MountPoint
            {
                #region Variables
                private BaseMountable entity;
                private CarController controller;
                private BasePlayer player;
                private Vector3 dismountLocal;
                private Vector3 offset;
                private bool isDriver;
                private bool allowHeldItems;
                private string[] disallowedItems;

                public CarController Controller
                {
                    get
                    {
                        return controller;
                    }
                }

                public BaseMountable Entity
                {
                    get
                    {
                        return entity;
                    }
                }

                public Vector3 Position
                {
                    get
                    {
                        if (entity == null && !isDriver)
                            GeneratePrefab();

                        return isDriver ? entity.transform.position + (entity.transform.forward * 0.5f) + (entity.transform.up * 0.1f) + (entity.transform.right * -0.5f) : entity.transform.position;
                    }
                }

                public bool IsDriver
                {
                    get
                    {
                        return isDriver;
                    }
                }
                #endregion

                #region Constructor
                public MountPoint() { }

                public MountPoint(CarController controller, Vector3 offset, Vector3 dismountLocal, bool isDriver = false)
                {
                    this.isDriver = isDriver;
                    this.dismountLocal = dismountLocal;
                    this.controller = controller;
                    this.offset = offset;

                    if (isDriver)
                        entity = controller.entity;
                    else GeneratePrefab();

                    allowHeldItems = isDriver ? !ins.configData.ActiveItems.DisableDriver : !ins.configData.ActiveItems.DisablePassengers;
                    disallowedItems = isDriver ? ins.configData.ActiveItems.DriverBlackList : ins.configData.ActiveItems.PassengerBlackList;

                    InvokeHandler.InvokeRepeating(entity, UpdateHeldItems, 0f, 0.5f);
                }

                public void GeneratePrefab()
                {
                    entity = GameManager.server.CreateEntity(controller.entity.chairRef.resourcePath, controller.transform.position) as BaseMountable;
                    entity.Spawn();
                    entity.enableSaving = false;
                    entity.maxMountDistance = 2f;

                    entity.GetComponent<DestroyOnGroundMissing>().enabled = false;
                    entity.GetComponent<GroundWatch>().enabled = false;
                    entity.GetComponent<MeshCollider>().convex = true;

                    InvisibleMount invisibleMount = entity.gameObject.AddComponent<InvisibleMount>();
                    invisibleMount.MountPosition = this;

                    entity.SetParent(controller.entity);
                    entity.transform.localPosition = offset;
                }
                #endregion

                #region Mounting
                public void OnEntityMounted()
                {
                    controller.occupants.Add(player);
                    ins.CreateHealthUI(player, controller);

                    if (isDriver)
                    {
                        string message = string.Format(ins.msg("controls1", player.UserIDString), controller.cb[CommandType.Accelerate], controller.cb[CommandType.Brake], controller.cb[CommandType.Left], controller.cb[CommandType.Right], controller.cb[CommandType.Handbrake], controller.cb[CommandType.Lights]);

                        if (controller.inventory != null)
                            message += $"\n{string.Format(ins.msg("access_inventory", player.UserIDString), controller.cb[CommandType.Inventory])}";

                        if (controller.fuelEnabled)
                        {
                            message += $"\n{string.Format(ins.msg("access_fuel", player.UserIDString), controller.cb[CommandType.FuelTank])}";
                            message += $"\n{string.Format(ins.msg("fuel_type", player.UserIDString), ins.fuelTypeName)}";
                            ins.CreateFuelUI(player, controller);
                        }                        
                        if (controller.repairEnabled)
                            message += $"\n{string.Format(ins.msg("repairhelp", player.UserIDString), ins.configData.Repair.Amount, ins.repairTypeName)}";

                        player.ChatMessage(message);
                    }
                    else
                    {
                        string message = string.Empty;

                        if (controller.inventory != null)
                            message += string.Format(ins.msg("access_inventory", player.UserIDString), controller.cb[CommandType.Inventory]);

                        if (controller.fuelEnabled)
                        {
                            message += $"\n{string.Format(ins.msg("access_fuel", player.UserIDString), controller.cb[CommandType.FuelTank])}";
                            message += $"\n{string.Format(ins.msg("fuel_type", player.UserIDString), ins.fuelTypeName)}";
                        }
                        if (controller.repairEnabled)
                            message += $"\n{string.Format(ins.msg("repairhelp", player.UserIDString), ins.configData.Repair.Amount, ins.repairTypeName)}";

                        if (!string.IsNullOrEmpty(message))
                            player.ChatMessage(message);
                    }
                }

                public void OnEntityDismounted()
                {
                    ins.DestroyAllUI(player);
                    controller.occupants.Remove(player);
                    player = null;
                }

                public void MountPlayer(BasePlayer player)
                {
                    this.player = player;
                    player.EnsureDismounted();
                    typeof(BaseMountable).GetField("_mounted", (BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic)).SetValue(entity, player);
                    player.MountObject(entity, 0);
                    player.MovePosition(entity.mountAnchor.transform.position);
                    player.transform.rotation = entity.mountAnchor.transform.rotation;
                    player.ServerRotation = entity.mountAnchor.transform.rotation;
                    player.OverrideViewAngles(entity.mountAnchor.transform.rotation.eulerAngles);
                    player.eyes.NetworkUpdate(entity.mountAnchor.transform.rotation);
                    player.ClientRPCPlayer(null, player, "ForcePositionTo", player.transform.position);
                    entity.SetFlag(BaseEntity.Flags.Busy, true, false);
                    OnEntityMounted();
                }

                public void DismountPlayer()
                {
                    if (player == null)
                        return;

                    Vector3 dismountPosition = controller.entity.transform.position + (controller.entity.transform.right * dismountLocal.x) + (controller.entity.transform.up * 0.1f) + (controller.entity.transform.forward * dismountLocal.z);

                    if (TerrainMeta.HeightMap.GetHeight(dismountPosition) > dismountPosition.y)
                        dismountPosition.y = TerrainMeta.HeightMap.GetHeight(dismountPosition) + 0.5f;

                    Quaternion dismountRotation = entity.dismountAnchor.rotation;
                    player.DismountObject();

                    player.transform.rotation = dismountRotation;
                    player.MovePosition(dismountPosition);
                    player.eyes.NetworkUpdate(dismountRotation);
                    player.SendNetworkUpdateImmediate(false);
                    player.ClientRPCPlayer(null, player, "ForcePositionTo", dismountPosition);
                    typeof(BaseMountable).GetField("_mounted", (BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic)).SetValue(entity, null);

                    OnEntityDismounted();
                }
                #endregion

                public void UpdatePosition()
                {
                    if (player == null)
                        return;

                    if (entity.IsMounted())
                        player.MovePosition(entity.mountAnchor.transform.position);
                }

                public void UpdateHeldItems()
                {
                    if (player == null)
                        return;

                    var item = player.GetActiveItem();
                    if (item == null || item.GetHeldEntity() == null)
                        return;

                    if (disallowedItems.Contains(item.info.shortname) || !allowHeldItems)
                    {
                        player.ChatMessage(ins.msg("itemnotallowed", player.UserIDString));

                        var slot = item.position;
                        item.SetParent(null);
                        item.MarkDirty();

                        ins.timer.Once(0.15f, () =>
                        {
                            if (item == null) return;
                            item.SetParent(player.inventory.containerBelt);
                            item.position = slot;
                            item.MarkDirty();
                        });
                    }
                }

                public void DestroyMountPoint()
                {
                    if (isDriver)
                        return;

                    if (entity.IsMounted())
                        DismountPlayer();                    

                    if (entity != null && !entity.IsDestroyed)
                    {
                        Destroy(entity.GetComponent<InvisibleMount>());
                        entity.Kill(BaseNetworkable.DestroyMode.None);
                    }
                }
            }

            public class InvisibleMount : MonoBehaviour
            {
                private BaseMountable entity;
                private MountPoint mountPoint;

                private void Awake()
                {
                    entity = GetComponent<BaseMountable>();
                    enabled = false;
                }

                public MountPoint MountPosition
                {
                    get
                    {
                        return mountPoint;
                    }
                    set
                    {
                        mountPoint = value;
                    }
                }
            }
        }
        #endregion

        #region UI
        #region UI Elements
        public static class UI
        {
            static public CuiElementContainer ElementContainer(string panelName, string color, UI4 dimensions, bool useCursor = false, string parent = "Overlay")
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax()},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panelName
                    }
                };
                return NewElement;
            }
            static public void Panel(ref CuiElementContainer container, string panel, string color, UI4 dimensions, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    CursorEnabled = cursor
                },
                panel);
            }
            static public void Label(ref CuiElementContainer container, string panel, string text, int size, UI4 dimensions, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { FontSize = size, Align = align, Text = text, Font = "droidsansmono.ttf" },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                },
                panel);

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

        #region UI Creation        
        private void CreateHealthUI(BasePlayer player, CarController controller)
        {
            var opt = configData.UI.Health;
            if (!opt.Enabled)
                return;

            var container = UI.ElementContainer(uiHealth, UI.Color(opt.Color1, opt.Color1A), new UI4(opt.Xmin, opt.YMin, opt.XMax, opt.YMax));
            UI.Label(ref container, uiHealth, msg("health", player.UserIDString), 12, new UI4(0.03f, 0, 1, 1), TextAnchor.MiddleLeft);
            var percentHealth = System.Convert.ToDouble((float)controller.entity.health / (float)controller.entity.MaxHealth());
            float yMaxHealth = 0.25f + (0.73f * (float)percentHealth);
            UI.Panel(ref container, uiHealth, UI.Color(opt.Color2, opt.Color2A), new UI4(0.25f, 0.1f, yMaxHealth, 0.9f));
            DestroyUI(player, uiHealth);
            CuiHelper.AddUi(player, container);
        }

        private void CreateFuelUI(BasePlayer player, CarController controller)
        {
            if (configData.Fuel.Enabled)
            {
                var opt = configData.UI.Fuel;
                if (!opt.Enabled)
                    return;

                var container = UI.ElementContainer(uiFuel, UI.Color(opt.Color1, opt.Color1A), new UI4(opt.Xmin, opt.YMin, opt.XMax, opt.YMax));
                UI.Label(ref container, uiFuel, string.Format(msg("fuel", player.UserIDString), $"<color={opt.Color2}>{controller.fuelTank.GetAmount(fuelType, false)}</color>"), 12, new UI4(0.03f, 0, 1, 1), TextAnchor.MiddleLeft);
                DestroyUI(player, uiFuel);
                CuiHelper.AddUi(player, container);
            }
        }

        private void DestroyUI(BasePlayer player, string panel) => CuiHelper.DestroyUi(player, panel);

        private void DestroyAllUI(BasePlayer player)
        {
            DestroyUI(player, uiHealth);
            DestroyUI(player, uiFuel);
        }
        #endregion
        #endregion

        #region Commands
        private Dictionary<BasePlayer, DateTime> Cooldowns = new Dictionary<BasePlayer, DateTime>();
        [ChatCommand("car")]
        void cmdSpawnCar1(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "carcommander.carspawn"))
            {
                SendReply(player, "У тебя нету прав использовать эту команду!"); return;

            }
            var Cooldown = configData.Passengers.Cooldown1;
            if (Cooldowns.ContainsKey(player))
            {
                double seconds = Cooldowns[player].Subtract(DateTime.Now).TotalMinutes;
                if (seconds >= 0)
                {
                    SendReply(player, msg("cooldowns", player.UserIDString), seconds);
                    return;
                }
            }
            Cooldowns[player] = DateTime.Now.AddMinutes(Cooldown);
            Vector3 position = player.transform.position + (player.transform.forward * 3);

            RaycastHit hit;
            if (Physics.SphereCast(player.eyes.position, 0.1f, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward, out hit, 20f))
                position = hit.point;

            SpawnAtLocation(position, new Quaternion(), (args.Length == 1 && args[0].ToLower() == "save"));
        }
        [ChatCommand("spawncar")]
        void cmdSpawnCar(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "carcommander.admin") && !permission.UserHasPermission(player.UserIDString, "carcommander.canspawn")) return;

            Vector3 position = player.transform.position + (player.transform.forward * 3);

            RaycastHit hit;
            if (Physics.SphereCast(player.eyes.position, 0.1f, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward, out hit, 20f))
                position = hit.point;

            SpawnAtLocation(position, new Quaternion(), (args.Length == 1 && args[0].ToLower() == "save"));
        }

        [ChatCommand("clearcars")]
        void cmdClearCars(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "carcommander.admin")) return;

            for (int i = saveableCars.Count - 1; i >= 0; i--)
            {
                var car = saveableCars[i];
                if (car != null && car.entity != null && !car.entity.IsDestroyed)
                    car.StopToDie();
            }
        }

        [ConsoleCommand("clearcars")]
        void ccmdClearCars(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null || arg.Args == null)
                return;

            for (int i = saveableCars.Count - 1; i >= 0; i--)
            {
                var car = saveableCars[i];
                if (car != null && car.entity != null && !car.entity.IsDestroyed)
                    car.StopToDie();
            }
        }

        [ConsoleCommand("spawncar")]
        void ccmdSpawnCar(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null || arg.Args == null)
                return;

            if (arg.Args.Length == 1 || arg.Args.Length == 2)
            {
                BasePlayer player = covalence.Players.Connected.FirstOrDefault(x => x.Id == arg.GetString(0))?.Object as BasePlayer;
                if (player != null)
                {
                    Vector3 position = player.transform.position + (player.transform.forward * 3) + Vector3.up;

                    RaycastHit hit;
                    if (Physics.SphereCast(player.eyes.position, 0.5f, Quaternion.Euler(player.serverInput.current.aimAngles) * Vector3.forward, out hit, 20f))
                        position = hit.point;

                    SpawnAtLocation(position, new Quaternion(), (arg.Args.Length == 2 && arg.Args[1].ToLower() == "save"));
                }
                return;
            }
            if (arg.Args.Length > 2)
            {
                float x;
                float y;
                float z;

                if (float.TryParse(arg.GetString(0), out x))
                {
                    if (float.TryParse(arg.GetString(1), out y))
                    {
                        if (float.TryParse(arg.GetString(2), out z))
                        {
                            SpawnAtLocation(new Vector3(x, y, z), new Quaternion(), (arg.Args.Length == 4 && arg.Args[3].ToLower() == "save"));
                            return;
                        }
                    }
                }
                PrintError($"Invalid arguments supplied to spawn a car at position : (x = {arg.GetString(0)}, y = {arg.GetString(1)}, z = {arg.GetString(2)})");
            }
        }
        #endregion

        #region Friends
        private bool AreFriends(ulong playerId, ulong friendId)
        {
            if (Friends && configData.Passengers.UseFriends)
                return (bool)Friends?.Call("AreFriendsS", playerId.ToString(), friendId.ToString());
            return true;
        }
        private bool IsClanmate(ulong playerId, ulong friendId)
        {
            if (Clans && configData.Passengers.UseClans)
            {
                object playerTag = Clans?.Call("GetClanOf", playerId);
                object friendTag = Clans?.Call("GetClanOf", friendId);
                if (playerTag is string && friendTag is string)
                {
                    if (!string.IsNullOrEmpty((string)playerTag) && !string.IsNullOrEmpty((string)friendTag) && (playerTag == friendTag))
                        return true;
                }
                return false;
            }
            return true;
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Movement Settings")]
            public MovementSettings Movement { get; set; }
            [JsonProperty(PropertyName = "Button Configuration")]
            public ButtonConfiguration Buttons { get; set; }
            [JsonProperty(PropertyName = "Passenger Options")]
            public PassengerOptions Passengers { get; set; }
            [JsonProperty(PropertyName = "Inventory Options")]
            public InventoryOptions Inventory { get; set; }
            [JsonProperty(PropertyName = "Spawnable Options")]
            public SpawnableOptions Spawnable { get; set; }
            [JsonProperty(PropertyName = "Fuel Options")]
            public FuelOptions Fuel { get; set; }
            [JsonProperty(PropertyName = "Repair Options")]
            public RepairSettings Repair { get; set; }
            [JsonProperty(PropertyName = "Active Item Options")]
            public ActiveItemOptions ActiveItems { get; set; }
            [JsonProperty(PropertyName = "UI Options")]
            public UIOptions UI { get; set; }

            public class ButtonConfiguration
            {
                [JsonProperty(PropertyName = "Open inventory")]
                public string Inventory { get; set; }
                public string Accelerate { get; set; }
                [JsonProperty(PropertyName = "Brake / Reverse")]
                public string Brake { get; set; }
                [JsonProperty(PropertyName = "Turn Left")]
                public string Left { get; set; }
                [JsonProperty(PropertyName = "Turn Right")]
                public string Right { get; set; }
                [JsonProperty(PropertyName = "Hand Brake")]
                public string HBrake { get; set; }
                [JsonProperty(PropertyName = "Open fuel tank")]
                public string FuelTank { get; set; }
                [JsonProperty(PropertyName = "Toggle lights")]
                public string Lights { get; set; }
            }
            public class RepairSettings
            {
                [JsonProperty(PropertyName = "Repair system enabled")]
                public bool Enabled { get; set; }
                [JsonProperty(PropertyName = "Shortname of item required to repair")]
                public string Shortname { get; set; }
                [JsonProperty(PropertyName = "Amount of item required to repair")]
                public int Amount { get; set; }
                [JsonProperty(PropertyName = "Amount of damage repaired per hit")]
                public int Damage { get; set; }
            }
            public class MovementSettings
            {
                [JsonProperty(PropertyName = "Engine - Acceleration torque")]
                public float Acceleration { get; set; }
                [JsonProperty(PropertyName = "Engine - Brake  torque")]
                public float Brakes { get; set; }
                [JsonProperty(PropertyName = "Engine - Reverse  torque")]
                public float Reverse { get; set; }
                [JsonProperty(PropertyName = "Engine - Maximum speed")]
                public float Speed { get; set; }

                [JsonProperty(PropertyName = "Steering - Max angle")]
                public float Steer { get; set; }
                [JsonProperty(PropertyName = "Steering - Max angle at speed")]
                public float SteerSpeed { get; set; }
                [JsonProperty(PropertyName = "Steering - Automatically counter steer")]
                public bool CounterSteer { get; set; }                

                [JsonProperty(PropertyName = "Suspension - Force")]
                public float Spring { get; set; }
                [JsonProperty(PropertyName = "Suspension - Damper")]
                public float Damper { get; set; }
                [JsonProperty(PropertyName = "Suspension - Target position (min 0, max 1)")]                
                public float Target { get; set; }
                [JsonProperty(PropertyName = "Suspension - Distance")]
                public float Distance { get; set; }


                [JsonProperty(PropertyName = "Anti Roll - Front horizontal force")]
                public float AntiRollFH { get; set; }
                [JsonProperty(PropertyName = "Anti Roll - Rear horizontal force")]
                public float AntiRollRH { get; set; }
                [JsonProperty(PropertyName = "Anti Roll - Vertical force")]
                public float AntiRollV { get; set; }                             
            }
            public class PassengerOptions
            {
                [JsonProperty(PropertyName = "Allow passengers")]
                public bool Enabled { get; set; }
                [JsonProperty(PropertyName = "Require passenger to be a friend (FriendsAPI)")]
                public bool UseFriends { get; set; }
                [JsonProperty(PropertyName = "Require passenger to be a clan mate (Clans)")]
                public bool UseClans { get; set; }
                [JsonProperty(PropertyName = "Время блокировки чат команды (в минутах)")]
                public double Cooldown1 { get; set; }
            }
            public class InventoryOptions
            {
                [JsonProperty(PropertyName = "Enable inventory system")]
                public bool Enabled { get; set; }
                [JsonProperty(PropertyName = "Drop inventory on death")]
                public bool DropInv { get; set; }
                [JsonProperty(PropertyName = "Inventory size (max 36)")]
                public int Size { get; set; }
            }
            public class SpawnableOptions
            {
                [JsonProperty(PropertyName = "Enable automatic vehicle spawning")]
                public bool Enabled { get; set; }
                [JsonProperty(PropertyName = "Use RandomSpawns for spawn locations")]
                public bool RandomSpawns { get; set; }
                [JsonProperty(PropertyName = "Spawnfile name")]
                public string Spawnfile { get; set; }
                [JsonProperty(PropertyName = "Maximum spawned vehicles at any time")]
                public int Max { get; set; }
                [JsonProperty(PropertyName = "Time between autospawns (seconds)")]
                public int Time { get; set; }
            }
            public class FuelOptions
            {
                [JsonProperty(PropertyName = "Requires fuel")]
                public bool Enabled { get; set; }
                [JsonProperty(PropertyName = "Fuel type (item shortname)")]
                public string FuelType { get; set; }
                [JsonProperty(PropertyName = "Fuel consumption rate (seconds per litre)")]
                public int Consumption { get; set; }
                [JsonProperty(PropertyName = "Spawn vehicles with fuel")]
                public bool GiveFuel { get; set; }
                [JsonProperty(PropertyName = "Amount of fuel to give spawned vehicles (minimum)")]
                public int FuelAmountMin { get; set; }
                [JsonProperty(PropertyName = "Amount of fuel to give spawned vehicles (maximum)")]
                public int FuelAmountMax { get; set; }
            }
            public class ActiveItemOptions
            {
                [JsonProperty(PropertyName = "Driver - Disable all held items")]
                public bool DisableDriver { get; set; }                     
                [JsonProperty(PropertyName = "Driver - List of disallowed held items (item shortnames)")]
                public string[] DriverBlackList { get; set; }
                [JsonProperty(PropertyName = "Passenger - Disable all held items")]
                public bool DisablePassengers { get; set; }                
                [JsonProperty(PropertyName = "Passenger - List of disallowed held items (item shortnames)")]
                public string[] PassengerBlackList { get; set; }
            }
            public class UIOptions
            {
                [JsonProperty(PropertyName = "Health settings")]
                public UICounter Health { get; set; }
                [JsonProperty(PropertyName = "Fuel settings")]
                public UICounter Fuel { get; set; }

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
                    [JsonProperty(PropertyName = "Status alpha")]
                    public float Color2A { get; set; }
                }
            }
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
                ActiveItems = new ConfigData.ActiveItemOptions
                {
                    DisableDriver = true,
                    DisablePassengers = false,
                    DriverBlackList = new string[0],
                    PassengerBlackList = new string[]
                    {
                        "explosive.timed", "rocket.launcher", "surveycharge", "explosive.satchel"
                    }
                },
                Buttons = new ConfigData.ButtonConfiguration
                {
                    Inventory = "RELOAD",
                    Accelerate = "FORWARD",
                    Brake = "BACKWARD",
                    Left = "LEFT",
                    Right = "RIGHT",
                    HBrake = "SPRINT",
                    FuelTank = "FIRE_THIRD",
                    Lights = "RELOAD"
                },
                Inventory = new ConfigData.InventoryOptions
                {
                    DropInv = true,
                    Enabled = true,
                    Size = 36
                },
                Movement = new ConfigData.MovementSettings
                {
                    Acceleration = 600f,
                    Brakes = 800f,
                    Reverse = 500f,
                    Steer = 60f,
                    SteerSpeed = 20f,
                    CounterSteer = false,
                    Speed = 90f,
                    Damper = 2000f,
                    Distance = 0.2f,
                    Spring = 40000f,
                    Target = 0.4f,
                    AntiRollFH = 3500f,
                    AntiRollRH = 3500f,
                    AntiRollV = 500f                    
                },
                Passengers = new ConfigData.PassengerOptions
                {
                    Enabled = true,
                    UseClans = true,
                    UseFriends = true,
                    Cooldown1 = 60f
                },
                Spawnable = new ConfigData.SpawnableOptions
                {
                    Enabled = true,
                    Max = 5,
                    Time = 1800,
                    Spawnfile = "",
                    RandomSpawns = false
                },
                Fuel = new ConfigData.FuelOptions
                {
                    Enabled = true,
                    Consumption = 10,
                    FuelType = "lowgradefuel",
                    FuelAmountMin = 10,
                    FuelAmountMax = 50,
                    GiveFuel = true
                },
                Repair = new ConfigData.RepairSettings
                {
                    Amount = 10,
                    Damage = 30,
                    Enabled = true,
                    Shortname = "scrap"
                },
                UI = new ConfigData.UIOptions
                {
                    Fuel = new ConfigData.UIOptions.UICounter
                    {
                        Color1 = "#F2F2F2",
                        Color1A = 0.05f,
                        Color2 = "#ce422b",
                        Color2A = 1,
                        Enabled = true,
                        Xmin = 0.69f,
                        XMax = 0.83f,
                        YMin = 0.06f,
                        YMax = 0.096f
                    },
                    Health = new ConfigData.UIOptions.UICounter
                    {
                        Color1 = "#F2F2F2",
                        Color1A = 0.05f,
                        Color2 = "#ce422b",
                        Color2A = 0.6f,
                        Enabled = true,
                        Xmin = 0.69f,
                        XMax = 0.83f,
                        YMin = 0.1f,
                        YMax = 0.135f
                    }
                }
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion

        #region Data Management
        void SaveData() => data.WriteObject(storedData);
        void LoadData()
        {
            try
            {
                storedData = data.ReadObject<RestoreData>();
            }
            catch
            {
                storedData = new RestoreData();
            }
        }

        public class RestoreData
        {
            public Hash<uint, InventoryData> restoreData = new Hash<uint, InventoryData>();

            public void AddData(CarController controller)
            {
                restoreData[controller.entity.net.ID] = new InventoryData(controller);
            }

            public void RemoveData(uint netId)
            {
                if (HasRestoreData(netId))
                    restoreData.Remove(netId);
            }

            public bool HasRestoreData(uint netId) => restoreData.ContainsKey(netId);

            public void RestoreInventory(CarController controller)
            {
                InventoryData inventoryData;
                if (restoreData.TryGetValue(controller.entity.net.ID, out inventoryData))
                {
                    if (controller == null || controller.inventory == null)
                        return;

                    RestoreAllItems(controller, inventoryData);
                }
            }

            private void RestoreAllItems(CarController controller, InventoryData inventoryData)
            {
                if (controller == null)
                    return;

                RestoreItems(controller, inventoryData.vehicleContainer, true);
                RestoreItems(controller, inventoryData.fuelContainer, false);
            }

            private bool RestoreItems(CarController controller, ItemData[] itemData, bool isInventory)
            {
                if ((!isInventory && !ins.configData.Fuel.Enabled) || (isInventory && !ins.configData.Inventory.Enabled) || itemData == null || itemData.Length == 0)
                    return true;

                for (int i = 0; i < itemData.Length; i++)
                {
                    Item item = CreateItem(itemData[i]);
                    item.MoveToContainer(isInventory ? controller.inventory : controller.fuelTank, itemData[i].position, true);
                }
                return true;
            }

            private Item CreateItem(ItemData itemData)
            {
                var item = ItemManager.CreateByItemID(itemData.itemid, itemData.amount, itemData.skin);
                item.condition = itemData.condition;
                if (itemData.instanceData != null)
                    item.instanceData = itemData.instanceData;

                item.blueprintTarget = itemData.blueprintTarget;

                var weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    if (!string.IsNullOrEmpty(itemData.ammotype))
                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(itemData.ammotype);
                    weapon.primaryMagazine.contents = itemData.ammo;
                }
                if (itemData.contents != null)
                {
                    foreach (var contentData in itemData.contents)
                    {
                        var newContent = ItemManager.CreateByItemID(contentData.itemid, contentData.amount);
                        if (newContent != null)
                        {
                            newContent.condition = contentData.condition;
                            newContent.MoveToContainer(item.contents);
                        }
                    }
                }
                return item;
            }

            public class InventoryData
            {
                public ItemData[] vehicleContainer = new ItemData[0];
                public ItemData[] fuelContainer = new ItemData[0];

                public InventoryData() { }

                public InventoryData(CarController controller)
                {
                    if (ins.configData.Inventory.Enabled)
                        vehicleContainer = GetItems(controller.inventory).ToArray();
                    if (ins.configData.Fuel.Enabled)
                        fuelContainer = GetItems(controller.fuelTank).ToArray();
                }

                private IEnumerable<ItemData> GetItems(ItemContainer container)
                {
                    return container.itemList.Select(item => new ItemData
                    {
                        itemid = item.info.itemid,
                        amount = item.amount,
                        ammo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.contents ?? 0,
                        ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                        position = item.position,
                        skin = item.skin,
                        condition = item.condition,
                        instanceData = item.instanceData ?? null,
                        blueprintTarget = item.blueprintTarget,
                        contents = item.contents?.itemList.Select(item1 => new ItemData
                        {
                            itemid = item1.info.itemid,
                            amount = item1.amount,
                            condition = item1.condition
                        }).ToArray()
                    });
                }
            }

            public class ItemData
            {
                public int itemid;
                public ulong skin;
                public int amount;
                public float condition;
                public int ammo;
                public string ammotype;
                public int position;
                public int blueprintTarget;
                public ProtoBuf.Item.InstanceData instanceData;
                public ItemData[] contents;
            }
        }
        #endregion

        #region Localization
        string msg(string key, string playerId = null) => lang.GetMessage(key, this, playerId);

        Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["not_friend"] = "<color=#D3D3D3>You must be a friend or clanmate with the operator</color>",
            ["controls1"] = "<color=#ce422b>Car Controls:</color>\n<color=#D3D3D3>Accelerate:</color> <color=#ce422b>{0}</color>\n<color=#D3D3D3>Brake/Reverse:</color> <color=#ce422b>{1}</color>\n<color=#D3D3D3>Turn Left:</color> <color=#ce422b>{2}</color>\n<color=#D3D3D3>Turn Right:</color> <color=#ce422b>{3}</color>\n<color=#D3D3D3>Hand Brake:</color> <color=#ce422b>{4}</color>\n<color=#D3D3D3>Toggle Lights:</color> <color=#ce422b>{5}</color>",
            ["access_inventory"] = "<color=#D3D3D3>Access Inventory (from outside of the vehicle) </color><color=#ce422b>{0}</color>",
            ["cooldowns"] = "<color=#D3D3D3>Погоди не много. Нельзя так часто спавнить авто! Осталось: <color=#ce422b>{0:00} мин..</color></color>",
            ["access_fuel"] = "<color=#D3D3D3>Access Fuel Tank (from outside of the vehicle) </color><color=#ce422b>{0}</color>",
            ["fuel_type"] = "<color=#D3D3D3>This vehicle requires </color><color=#ce422b>{0}</color> <color=#D3D3D3>to run!</color>",
            ["not_enabled"] = "<color=#D3D3D3>Passengers is not enabled</color>",
            ["nopermission"] = "<color=#D3D3D3>You do not have permission to drive this car</color>",
            ["health"] = "HLTH: ",
            ["fuel"] = "FUEL: {0} L",
            ["fullhealth"] = "<color=#D3D3D3>This vehicle is already at full health</color>",
            ["noresources"] = "<color=#D3D3D3>You need atleast </color><color=#ce422b>{0}x {1}</color> <color=#D3D3D3>to make repairs</color>",
            ["repairhelp"] = "<color=#D3D3D3>You can make repairs to this vehicle using a hammer which costs </color><color=#ce422b>{0}x {1}</color> <color=#D3D3D3>per hit</color>",
            ["itemnotallowed"] = "<color=#D3D3D3>You can not use that item whilst you are in a car</color>"
        };
        #endregion
    }
}
