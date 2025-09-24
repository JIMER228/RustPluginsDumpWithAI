// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using Oxide.Core.Plugins;
using UnityEngine.AI;
using System.Collections;
using Facepunch;
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries.Covalence;
using Rust;
using Random = System.Random;

namespace Oxide.Plugins
{
    [Info("NpcHorses", "Razor", "2.2.0")]
    [Description("Make Npc ride horses")]
    public class NpcHorses : RustPlugin
    {
        [PluginReference]
        Plugin Kits;

        public bool debug = false;
        private static NpcHorses ins;
        private static Coroutine SpawnRoutine2 { get; set; }
        public Dictionary<ulong, ulong> HorseAndRider = new Dictionary<ulong, ulong>();
        public Dictionary<ulong, bool> _allNpc = new Dictionary<ulong, bool>();
      //  private List<Vector3> SpawnLocations = new List<Vector3>();
        public List<ulong> UsedPile = new List<ulong>();
        public bool rebooting = false;
        public int failed;
        public int totalHorses;
        public bool booted;
        public Dictionary<string, int> TropToInt = new Dictionary<string, int>()
        {
            { "field", 1 }, { "cliff", 2 }, { "summit", 4 }, { "beachside", 8 }, { "beach", 16 }, { "forest", 32 }, { "forestside", 64 }, { "ocean", 128 },
            { "oceanside", 256 }, { "decor", 512 }, { "monument", 1024 }, { "road", 2048 }, { "roadside", 4096 }, { "swamp", 8192 }, { "river", 16384 },
            { "riverside", 32768 }, { "lake", 65536 }, { "lakeside", 131072 }, { "offshore", 262144 }, { "powerline", 524288 }, { "plain", 1048576 },
            { "building", 2097152 }, { "cliffside", 4194304 }, { "mountain", 8388608 }, { "clutter", 16777216 }, { "alt", 33554432 }, { "tier0", 67108864 },
            { "tier1", 134217728 }, { "tier2", 268435456 }, { "mainland", 536870912 }, { "hilltop", 1073741824 }
        };

        public Dictionary<string, List<Vector3>> TropSpawns = new Dictionary<string, List<Vector3>>();
        public Dictionary<string, List<ulong>> totalTypes = new Dictionary<string, List<ulong>>();
        public static Dictionary<Vector3, bool> deathLocation = new Dictionary<Vector3, bool>();


        #region Config

        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Spawn Settings")]
            public SpawnSettings spawnSettings { get; set; }

            [JsonProperty(PropertyName = "Horse Inventory Configs")]
            public Settings settings { get; set; }

            [JsonProperty(PropertyName = "Horse And Npc Spawn Settings")]
            public NpcHorseSpawn npcHorseSpawn { get; set; }

            public class Settings
            {
                public Dictionary<string, List<int>> HorseItems { get; set; }
            }

            public class SpawnSettings
            {
                public float NavScanRange { get; set; }
                public bool CanKillSleepers { get; set; }
            }

            public class NpcHorseSpawn
            {
                public Dictionary<string, horseSpawns> HorseAndNpc { get; set; }
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
                settings = new ConfigData.Settings
                {
                    HorseItems = new Dictionary<string, List<int>> { { "item", new List<int> { 60528587, 1400460850 } }, { "item2", new List<int> { 60528587, 1400460850 } } }
                },

                spawnSettings = new ConfigData.SpawnSettings
                {
                    NavScanRange = 50f,
                    CanKillSleepers = false
                },

                npcHorseSpawn = new ConfigData.NpcHorseSpawn
                {
                    HorseAndNpc = new Dictionary<string, horseSpawns>()
                },

                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new VersionNumber(2, 0, 0))
                configData = baseConfig;

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        public class horseSpawns
        {
            public int TotalToSpawn;
            public float HorseHealth;
            public float NpcHealth;
            public float VisionRange;
            public bool KillHorseOnRiderDeath;
            public bool ClearHorseInventoryOnDeath;
            public List<string> KitsList;
            public List<string> NpcNames;
            public List<string> HorseItemsConfigs;
            public float speed;
            public bool CanWander;
            public float RomeRange;
            public bool UseDormant;
            public bool RadioChatter;
            public string WayPoint;
            public float WayPointRespawn;
            public List<string> TopologiesAllowed;

            public horseSpawns(int TotalToSpawn, float HorseHealth, float NpcHealth, float VisionRange, bool KillHorseOnRiderDeath, bool ClearHorseInventoryOnDeath, List<string> KitsList, List<string> NpcNames, List<string> HorseItemsConfigs, float speed, bool CanWander, float RomeRange, bool UseDormant, string WayPoint, float WayPointRespawn, List<string> TopologiesAllowed)
            {
                this.TotalToSpawn = TotalToSpawn;
                this.HorseHealth = HorseHealth;
                this.NpcHealth = NpcHealth;
                this.VisionRange = VisionRange;
                this.KillHorseOnRiderDeath = KillHorseOnRiderDeath;
                this.ClearHorseInventoryOnDeath = ClearHorseInventoryOnDeath;
                this.KitsList = KitsList;
                this.NpcNames = NpcNames;
                this.HorseItemsConfigs = HorseItemsConfigs;
                this.speed = speed;
                this.CanWander = CanWander;
                this.RomeRange = RomeRange;
                this.UseDormant = UseDormant;
                this.RadioChatter = true;
                this.WayPoint = WayPoint;
                this.WayPointRespawn = WayPointRespawn;
                this.TopologiesAllowed = TopologiesAllowed;
            }
        }

        #endregion Config

        private void OnServerInitialized()
        {
            ins = this;
            if (configData.npcHorseSpawn.HorseAndNpc.Count <= 0)
            {
                configData.npcHorseSpawn.HorseAndNpc.Add("easy", new horseSpawns(10, 50, 20f, 30f, true, true, new List<string>(), new List<string>() { "Cowboy Bill" }, new List<string>() { "item", "item2" }, 5.0f, true, 20f, true, "", 0, new List<string>() { "Field", "Summit", "Forest", "Forestside", "Monument", "Road", "Roadside", "Swamp", "Lakeside", "Plain", "Clutter", "Mainland" }));
                configData.npcHorseSpawn.HorseAndNpc.Add("hard", new horseSpawns(10, 60, 30f, 30f, true, true, new List<string>(), new List<string>() { "Cowboy Bob" }, new List<string>() { "item", "item2" }, 5.0f, true, 20f, true, "", 0, new List<string>() { "Field", "Summit", "Forest", "Forestside", "Monument", "Road", "Roadside", "Swamp", "Lakeside", "Plain", "Clutter", "Mainland" }));
                SaveConfig();
            }
            foreach (var key in configData.npcHorseSpawn.HorseAndNpc)
            {
                TropSpawns.Add(key.Key, GenerateVecotrList(key.Value.TotalToSpawn * 10, key.Key));
            }

            BeginSpawning();

            timer.Every(60f, () =>
            {
                if (totalTypes.Count > 0)
                {
                    foreach (var key in totalTypes.ToList())
                    {
                        if (key.Value.Count < configData.npcHorseSpawn.HorseAndNpc[key.Key].TotalToSpawn)
                        {
                            if (SpawnRoutine2 == null)
                                SpawnRoutine2 = ServerMgr.Instance.StartCoroutine(ins.startSpawn(key.Value.Count, key.Key));
                            break;
                        }
                    }
                }
            });
        }

        private void Unload()
        {
            StopSpawning();
            rebooting = true;
            var Controller = UnityEngine.Object.FindObjectsOfType<horseBoxBehavior>();
            foreach (var gameObj in Controller)
            {
                UnityEngine.Object.Destroy(gameObj);
            }

            foreach (var HandR in HorseAndRider)
            {
                var rider = FindEntity(HandR.Value);
                if (rider != null) rider?.Kill();
                var horse = FindEntity(HandR.Key);
                if (horse != null) horse?.Kill();
            }
        }

        public BaseNetworkable FindEntity(ulong netID)
        {
            return BaseNetworkable.serverEntities.Find(new NetworkableId(netID));
        }

        private List<Vector3> GenerateVecotrList(int TotalSpawn, string configs)
        {
            List<Vector3> newTrop = new List<Vector3>();
            var success = 0;
            var size = ConVar.Server.worldsize / 2;
            for (var i = 0; i < TotalSpawn * 3; i++)
            {
                if (success > TotalSpawn)
                {
                    break;
                }

                var x = Core.Random.Range(-size, size);
                var z = Core.Random.Range(-size, size);
                Vector3 original = GetGround(new Vector3(x, 0, z));
                if (original == Vector3.zero)
                    continue;
                var position = FindPointOnNavmesh(original);

                if (position != null && position is Vector3 && (Vector3)position != Vector3.zero)
                {
                    if (TerrainMeta.HeightMap.GetHeight((Vector3)position) > TerrainMeta.WaterMap.GetHeight((Vector3)position))
                    {
                        if (IsValidTopology((Vector3)position, configs))
                        {
                            newTrop.Add((Vector3)position);
                            success++;
                        }
                    }
                }
            }
            return newTrop;
        }

        //Thanks to PowerSpawn Plugin
        public bool IsValidTopology(Vector3 position, string configs)
        {
            if (configData.npcHorseSpawn.HorseAndNpc[configs].TopologiesAllowed != null && configData.npcHorseSpawn.HorseAndNpc[configs].TopologiesAllowed.Count > 0)
            {
                foreach (var topology in configData.npcHorseSpawn.HorseAndNpc[configs].TopologiesAllowed)
                    if (TropToInt.ContainsKey(topology.ToLower()) && TerrainMeta.TopologyMap.GetTopology(position, (int)TropToInt[topology.ToLower()]))
                    {
                        return true;
                    }

                return false;
            }

            return true;
        }

        private Vector3 GetGround(Vector3 position)
        {
            if (position == Vector3.zero) return Vector3.zero;

            position.y = TerrainMeta.HeightMap.GetHeight(position);

            if (TerrainMeta.HeightMap.GetHeight(position) < TerrainMeta.WaterMap.GetHeight(position))
                return Vector3.zero;

            if (position == Vector3.zero) return Vector3.zero;

            return position;
        }

        internal static Coroutine BeginSpawning() => SpawnRoutine2 = ServerMgr.Instance.StartCoroutine(ins.startSpawn(0));

        internal static void StopSpawning()
        {
            if (SpawnRoutine2 != null)
                ServerMgr.Instance.StopCoroutine(SpawnRoutine2);
            SpawnRoutine2 = null;
        }

        private IEnumerator startSpawn(int total, string theConfig = "")
        {
            if (theConfig != "")
            {
                while (total < configData.npcHorseSpawn.HorseAndNpc[theConfig].TotalToSpawn)
                {
                    Vector3 spawnPoint = TropSpawns[theConfig].GetRandom();
                    spawnHorseAndRider(configData.npcHorseSpawn.HorseAndNpc[theConfig], spawnPoint, theConfig);
                    yield return CoroutineEx.waitForSeconds(0.5f);
                    total++;
                }
            }
            else
            {
                foreach (var configs in configData.npcHorseSpawn.HorseAndNpc)
                {
                    total = 0;
                    yield return CoroutineEx.waitForSeconds(1.0f);
                    while (total < configs.Value.TotalToSpawn)
                    {
                        Vector3 spawnPoint = TropSpawns[configs.Key].GetRandom();
                        spawnHorseAndRider(configData.npcHorseSpawn.HorseAndNpc[configs.Key], spawnPoint, configs.Key);
                        yield return CoroutineEx.waitForSeconds(0.5f);
                        total++;
                    }
                }
            }

            yield return CoroutineEx.waitForSeconds(1.0f);
            if (!ins.booted) { PrintWarning($"Spawned a total of {totalHorses} and failed to spawn {failed}"); ins.booted = true; ins.totalHorses = 0; ins.failed = 0; }
            else { PrintWarning($"Spawned a total of {totalHorses} and failed to spawn {failed} Current Horses {HorseAndRider.Count}"); ins.totalHorses = 0; ins.failed = 0; }
            StopSpawning();
        }

        private object OnNpcTarget(global::HumanNPC npc, BaseEntity player)
        {
            if (npc?.GetMountedVehicle() != null)
            {
                if (player.IsNpc) return true;

                if (player != null && player is BasePlayer && (player as BasePlayer).InSafeZone())
                    return true;

                if (!configData.spawnSettings.CanKillSleepers && player != null && player is BasePlayer && (player as BasePlayer).IsSleeping())
                    return true;
            }
            return null;
        }

        [ChatCommand("horsetest")]
        private void cmdSpawn(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            if (args.Length < 1)
            {
                SendReply(player, $"Usage: /horsetest <configName>");
                return;
            }
            if (!configData.npcHorseSpawn.HorseAndNpc.ContainsKey(args[0]))
            {
                SendReply(player, $"The config does not contain {args[0]}");
                return;
            }
            object point = FindPointOnNavmesh(player.transform.position, 10f);
            if (point is Vector3)
                spawnHorseAndRider(configData.npcHorseSpawn.HorseAndNpc[args[0]], (Vector3)point, args[0]);
        }

        private object OnNpcRadioChatter(ScientistNPC npc)
        {
            if (_allNpc.ContainsKey(npc.net.ID.Value) && _allNpc[npc.net.ID.Value])
                return false;
            return null;
        }

        public void spawnHorseAndRider(horseSpawns theConfig, Vector3 success, string theConfigKey)
        {
            if (success != Vector3.zero)
            {
                BaseEntity horseBot = InstantiateEntity("assets/rust.ai/nextai/testridablehorse.prefab", (Vector3)success, Quaternion.Euler(Vector3.zero));
                horseBoxBehavior controller = horseBot.gameObject.AddComponent<horseBoxBehavior>();
                horseBot.enableSaving = false;

                (horseBot as BaseRidableAnimal).startHealth = theConfig.HorseHealth;
                (horseBot as BaseRidableAnimal).SetMaxHealth(theConfig.HorseHealth);
                (horseBot as BaseRidableAnimal).InitializeHealth(theConfig.HorseHealth, theConfig.HorseHealth);
                (horseBot as BaseRidableAnimal)._health = theConfig.HorseHealth;

                horseBot.Spawn();

                global::HumanNPC horseNpc = InstantiateEntity("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_any.prefab", (Vector3)success, Quaternion.Euler(Vector3.zero)) as global::HumanNPC;
                if (horseNpc == null) return;
                controller.Npc = horseNpc;
                horseNpc.enableSaving = false;

                horseNpc.startHealth = theConfig.NpcHealth;
                horseNpc.InitializeHealth(theConfig.NpcHealth, theConfig.NpcHealth);
                horseNpc._health = theConfig.NpcHealth;
                horseNpc.health = theConfig.NpcHealth;
                // horseNpc.sightRange = theConfig.VisionRange;

                horseNpc.Spawn();
                _allNpc.Add(horseNpc.net.ID.Value, theConfig.RadioChatter);
                HorseAndRider.Add(horseBot.net.ID.Value, horseNpc.net.ID.Value);
                (horseBot as BaseRidableAnimal).AttemptMount(horseNpc, false);
                if (totalTypes.ContainsKey(theConfigKey))
                    totalTypes[theConfigKey].Add(horseBot.net.ID.Value);
                else totalTypes.Add(theConfigKey, new List<ulong>() { horseBot.net.ID.Value });

                controller.netID = horseBot.net.ID.Value;
                controller.SpawnLocation = (Vector3)success;

                if (theConfig.KitsList.Count > 0)
                {
                    horseNpc.inventory.Strip();
                    string kitUse = theConfig.KitsList.GetRandom();
                    if (CheckExists(kitUse))
                    {
                        Kits.Call($"GiveKit", horseNpc, kitUse, false);
                    }
                }
                else
                {
                    horseNpc.inventory.Strip();
                    switch (UnityEngine.Random.Range(0, 7))
                    {
                        case 0:
                            ItemManager.CreateByName("hazmatsuit").MoveToContainer(horseNpc.inventory.containerWear);
                            break;
                        case 1:
                            ItemManager.CreateByName("hazmatsuit_scientist_peacekeeper").MoveToContainer(horseNpc.inventory.containerWear);
                            break;
                        case 2:
                            ItemManager.CreateByName("hazmatsuit_scientist").MoveToContainer(horseNpc.inventory.containerWear);
                            break;
                        case 3:
                            ItemManager.CreateByName("hazmatsuit").MoveToContainer(horseNpc.inventory.containerWear);
                            break;
                        case 4:
                            ItemManager.CreateByName("hazmatsuit_scientist").MoveToContainer(horseNpc.inventory.containerWear);
                            break;
                        case 5:
                            ItemManager.CreateByName("hazmatsuit_scientist_peacekeeper").MoveToContainer(horseNpc.inventory.containerWear);
                            break;
                        case 6:
                            ItemManager.CreateByName("hazmatsuit_scientist").MoveToContainer(horseNpc.inventory.containerWear);
                            break;
                        case 7:
                            ItemManager.CreateByName("hazmatsuit").MoveToContainer(horseNpc.inventory.containerWear);
                            break;
                        default:
                            ItemManager.CreateByName("hazmatsuit_scientist").MoveToContainer(horseNpc.inventory.containerWear);
                            break;
                    }

                    switch (UnityEngine.Random.Range(0, 3))
                    {
                        case 0:
                            ItemManager.CreateByName("rifle.ak").MoveToContainer(horseNpc.inventory.containerBelt, 0);
                            break;
                        case 1:
                            ItemManager.CreateByName("smg.mp5").MoveToContainer(horseNpc.inventory.containerBelt, 0);
                            break;
                        case 2:
                            ItemManager.CreateByName("rifle.ak").MoveToContainer(horseNpc.inventory.containerBelt, 0);
                            break;
                        default:
                            ItemManager.CreateByName("rifle.ak").MoveToContainer(horseNpc.inventory.containerBelt, 0);
                            break;
                    }
                }

                controller.SetupConfig(theConfig, theConfigKey);

                if (theConfig.NpcNames != null && theConfig.NpcNames.Count > 0)
                    horseNpc.displayName = theConfig.NpcNames.GetRandom();

                if (theConfig != null && theConfig.HorseItemsConfigs != null && theConfig.HorseItemsConfigs.Count > 0)
                {
                    string rand = theConfig.HorseItemsConfigs.GetRandom();
                    if (configData.settings != null && configData.settings.HorseItems != null && configData.settings.HorseItems.ContainsKey(rand))
                        addHorseItems((horseBot as RidableHorse), configData.settings.HorseItems[rand]);
                    else PrintWarning($"HorseItems does not contain {rand}");
                }

                foreach (Item gun in horseNpc.inventory.containerBelt.itemList)
                {
                    AttackEntity attackEntity = gun.GetHeldEntity() as AttackEntity;
                    if (attackEntity != null && attackEntity is BaseProjectile && attackEntity.effectiveRange <= 1)
                    {
                        attackEntity.effectiveRange = 15f;
                    }
                }

                totalHorses++;
            }
            else failed++;
        }

        private void addHorseItems(RidableHorse horse, List<int> itemList)
        {
            if (configData.settings.HorseItems.Count > 0)
            {
                foreach (int itemID in itemList.ToList())
                {
                    var itemToCreate = ItemManager.FindItemDefinition(itemID);
                    if (itemToCreate != null)
                    {
                        Item item = ItemManager.Create(itemToCreate, 1, 0);
                        if (!item.MoveToContainer(horse.inventory, 0, false))
                        {
                            item.Remove();
                            continue;
                        }
                    }
                }
                horse.EquipmentUpdate();
                horse.inventory.SetLocked(true);
            }
        }

        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            if (item != null && item.parent != null && item.parent.IsLocked())
                return false;

            return null;
        }

        private bool CheckExists(string fileName)
        {
            if (Kits == null) return false;
            return ins.Kits.Call<bool>("isKit", fileName);
        }

        private BaseEntity InstantiateEntity(string type, Vector3 position, Quaternion rotation)
        {
            var gameObject = Facepunch.Instantiate.GameObject(GameManager.server.FindPrefab(type), position, rotation);
            gameObject.name = type;

            SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);

            UnityEngine.Object.Destroy(gameObject.GetComponent<Spawnable>());

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            BaseEntity component = gameObject.GetComponent<BaseEntity>();
            return component;
        }

        private static NavMeshHit navmeshHit;

        private static RaycastHit raycastHit;

        private static Collider[] _buffer = new Collider[256];

        private const int WORLD_LAYER = 65536;

        internal static object FindPointOnNavmesh(Vector3 targetPosition, float maxDistance = 50f)
        {
            if (ins.configData.spawnSettings.NavScanRange > 0)
                maxDistance = ins.configData.spawnSettings.NavScanRange;

            for (int i = 0; i < 10; i++)
            {
                Vector3 position = i == 0 ? targetPosition : targetPosition + (UnityEngine.Random.onUnitSphere * maxDistance);
                if (NavMesh.SamplePosition(position, out navmeshHit, maxDistance, 1))
                {

                    if (navmeshHit.mask != 1)
                        continue;

                    if (IsInRockPrefab(navmeshHit.position))
                        continue;

                    if (IsNearWorldCollider(navmeshHit.position))
                        continue;

                    return navmeshHit.position;
                }
            }
            return null;
        }

        private static bool IsInRockPrefab(Vector3 position)
        {
            Physics.queriesHitBackfaces = true;

            bool isInRock = Physics.Raycast(position, Vector3.up, out raycastHit, 20f, WORLD_LAYER, QueryTriggerInteraction.Ignore) &&
                            blockedColliders.Any(s => raycastHit.collider?.gameObject?.name.ToLower().Contains(s) ?? false);

            Physics.queriesHitBackfaces = false;

            return isInRock;
        }

        private static bool IsNearWorldCollider(Vector3 position)
        {
            Physics.queriesHitBackfaces = true;

            int count = Physics.OverlapSphereNonAlloc(position, 2f, _buffer, WORLD_LAYER, QueryTriggerInteraction.Ignore);
            Physics.queriesHitBackfaces = false;

            int removed = 0;
            for (int i = 0; i < count; i++)
            {
                if (acceptedColliders.Any(s => _buffer[i].gameObject.name.Contains(s)))
                    removed++;
            }


            return count - removed > 0;
        }

        private static readonly string[] acceptedColliders = new string[] { "road", "carpark", "range", "train_track", "runway", "_grounds", "concrete_slabs", "cave", "walkways" };

        private static readonly string[] blockedColliders = new string[] { "rock", "junk", "range", "invisible", "cliff", "prevent_movement", "formation_", "radiation" };

        public class horseBoxBehavior : FacepunchBehaviour
        {
            private BaseRidableAnimal horse { get; set; }
            public global::HumanNPC Npc { get; set; }
            public JunkPile junkPile { get; set; }
            public Vector3 SpawnLocation { get; set; }
            public float RomeRange { get; set; }
            public BaseEntity player { get; set; }
            public bool canWander { get; set; }
            public float moveSpeed { get; set; }
            public float nextWanderTime { get; set; }
            private float lastTargetTime { get; set; }
            public Vector3 MoveVector { get; set; }
            public float allowedMoveTime { get; set; }
            public float findingCover { get; set; }
            private bool findCover { get; set; }
            public bool Active { get; set; }
            public ulong netID { get; set; }
            public bool killAi { get; set; }
            private Transform tr { get; set; }
            private InputState aiInputState;
            private NavMeshAgent agent { get; set; }
            private int mountTrys { get; set; }
            private bool isDestroying { get; set; }
            private List<Vector3> roamPosition { get; set; }
            private bool useDormant { get; set; }
            public List<float> waypointSpeed = new List<float>();
            public List<Vector3> waypoint = new List<Vector3>();
            public int currentPoint { get; set; }
            private bool usingWaypoints { get; set; }
            private bool hastarget { get; set; }
            private BaseAIBrain defaultBrain { get; set; }
            public horseSpawns theConfig { get; set; }
            public string theConfigKey { get; set; }
            public BaseEntity currentTarget { get; private set; }

            private void Awake()
            {
                horse = GetComponent<BaseRidableAnimal>();
                MoveVector = Vector3.zero;
                allowedMoveTime = Time.time;
                tr = horse.transform;
                agent = horse.gameObject.AddComponent<NavMeshAgent>();
                nextWanderTime = Time.time + 30;
                if (ins.debug) ins.NextTick(() => ins.PrintWarning("Spawning NpcHorse at: " + SpawnLocation.ToString()));

                ins.NextTick(() =>
                {
                    if (Npc != null && Npc.Brain != null && Npc.Brain.Senses != null)
                    {
                        Npc.Brain.Senses.Init(Npc, Npc.Brain, 5f, 50f, 60f, Npc.Brain.VisionCone, Npc.Brain.CheckVisionCone, Npc.Brain.CheckLOS, Npc.Brain.IgnoreNonVisionSneakers, Npc.Brain.ListenRange, Npc.Brain.HostileTargetsOnly, false, true, Npc.Brain.SenseTypes, Npc.Brain.RefreshKnownLOS);
                        Npc.Brain.Senses.ignoreSafeZonePlayers = true;
                    }
                    InvokeRepeating("GetBestTarget", 1, 2);
                });
            }

            public void GetBestTarget()
            {
                if (Npc == null || horse == null || Npc.IsDestroyed || horse.IsDestroyed || Npc.Brain == null || Npc.Brain.Senses == null || Npc.Brain.Senses.Memory == null || Npc.Brain.Senses.Memory.Targets == null) return;
                    
                currentTarget = Npc.GetBestTarget();          
            }

            public void SetupConfig(horseSpawns theConfi, string theConfigKe)
            {
                theConfigKey = theConfigKe;
                theConfig = theConfi;
                useDormant = theConfi.UseDormant;
                moveSpeed = theConfi.speed;
                canWander = theConfi.CanWander;
                RomeRange = theConfi.RomeRange;
                ins.NextTick(() =>
                {
                    if (Npc != null)
                        defaultBrain = Npc.GetComponent<BaseAIBrain>();
                    if (horse != null)
                        netID = horse.net.ID.Value;
                    if (Npc.Brain.Navigator != null)
                    {
                        Npc.Brain.Navigator.CanNavigateMounted = false;
                        Npc.Brain.Navigator.CanUseNavMesh = false;
                    }
                    if (theConfi != null && !string.IsNullOrEmpty(theConfi.WayPoint))
                    {
                        var cwaypoints = Interface.Oxide.CallHook("GetWaypointsList", theConfi.WayPoint);
                        if (cwaypoints != null)
                        {
                            foreach (var cwaypoint in (List<object>)cwaypoints)
                            {
                                foreach (var pair in (Dictionary<Vector3, float>)cwaypoint)
                                {
                                    if (waypoint.Count <= 0)
                                    {
                                        object newPoint = FindPointOnNavmesh(pair.Key, 5f);
                                        if (newPoint is Vector3)
                                        {
                                            SpawnLocation = (Vector3)newPoint;
                                            agent.Warp((Vector3)newPoint);
                                            horse.transform.position = (Vector3)newPoint;

                                            agent.enabled = true;
                                            agent.isStopped = false;
                                            destination(horse.transform.position);
                                        }
                                        else
                                        {
                                            SpawnLocation = pair.Key;
                                            horse.transform.position = pair.Key;

                                        }
                                    }
                                    horse.SendNetworkUpdate();
                                    waypoint.Add(pair.Key);
                                    waypointSpeed.Add(pair.Value);

                                }
                            }
                            canWander = false;
                            usingWaypoints = true;
                        }
                        else
                        {
                            ins.PrintWarning($"Waypoint name {theConfi.WayPoint} not found.");
                            Destroy(this);
                            return;
                        }
                    }
                    else if (canWander)
                        GenerateRoam();
                    Active = true;
                });
            }

            private void GenerateRoam()
            {
                if (horse == null || horse.IsDestroyed || horse.transform == null)
                    return;
                roamPosition = FindCoverPoint(horse.transform.position, RomeRange, 60.0f, horse.transform.position.y);
            }

            public void DoDebugMovementFoward(bool run = false)
            {
                tr.rotation = Quaternion.Lerp(tr.rotation, currentTarget.transform.rotation, 4f * Time.deltaTime);
                aiInputState = new InputState();
                if (run)
                    aiInputState.current.buttons |= 128;
                aiInputState.current.buttons |= 2;
                horse.RiderInput(aiInputState, (BasePlayer)null);
            }

            public void Update()
            {
                if (!Active || isDestroying) return;
                if (horse == null || horse.IsDestroyed || horse.IsDead() || horse.IsDead() || Npc == null || Npc.IsDestroyed || Npc.IsDead())
                {
                    Destroy(this);
                    return;
                }

                else if (!Npc.isMounted)
                {
                    mountTrys++;
                    if (mountTrys > 5)
                    { 
                        if (horse != null || !horse.IsDestroyed || !horse.IsDead()) 
                        horse?.Kill(); 
                        if (ins.debug) ins.PrintWarning("Npc failed to mount horse killing NpcHorse and rider");
                        return;
                    }
                    if (Vector3.Distance(horse.transform.position, Npc.transform.position) > 2f)
                        Npc.transform.position = horse.transform.position;
                    horse.DismountAllPlayers();
                    (horse.children.FirstOrDefault(m => m.PrefabName.Contains("saddle")) as BaseMountable).MountPlayer(Npc);
                    //  horse.AttemptMount(Npc, false);
                    //   Npc.Mount(horse); // Need fix
                    return;
                }

                else if (mountTrys > 0) mountTrys = 0;

                if (currentTarget != null)
                {
                    hastarget = true;
                    lastTargetTime = Time.time + 180;
                    float distance = Vector3.Distance(horse.transform.position, currentTarget.transform.position);

                    if (!findCover && distance < 9)
                    {
                        findCover = true;
                        ins.timer.Once(8f, () => { findCover = false; });
                    }

                    if (findCover)
                    {
                        if (findingCover < Time.time)
                        {
                            // object point = FindPointOnNavmesh(Npc.AttackTarget.transform.position, 20f);
                            Vector3 point = FindCoverPoint(currentTarget.transform.position, 20, 60.0f, currentTarget.transform.position.y).GetRandom();
                            if (point != Vector3.zero)
                            {
                                destination(point, moveSpeed);
                            }
                            findingCover = Time.time + 3;
                        }

                    }
                    else if (agent.isOnNavMesh && distance < 13 && distance > 9)
                    {
                        if (tr.rotation != Npc.transform.rotation)
                            tr.rotation = Quaternion.Lerp(tr.rotation, Npc.transform.rotation, 4f * Time.deltaTime);
                        if (!agent.isStopped)
                            agent.isStopped = true;
                    }
                    else if (allowedMoveTime < Time.time)
                    {
                        allowedMoveTime = Time.time + 5;
                        if (agent.isOnNavMesh && Vector3.Distance(horse.transform.position, currentTarget.transform.position) > 13)
                            destination(currentTarget.transform.position, moveSpeed);
                    }
                }
                else if (canWander && canWanderNow()) //Wander
                {
                    nextWanderTime = Time.time + UnityEngine.Random.Range(30, 60);
                    if (useDormant && Npc != null && Npc.IsDormant) return;
                    if (!agent.isOnNavMesh) return;
                    if (roamPosition != null && roamPosition.Count > 0)
                    {
                        Vector3 point = roamPosition.GetRandom();
                        if (point != Vector3.zero)
                        {
                            if (Npc != null)
                                Npc.SetAimDirection(point - Npc.transform.position);
                            destination((Vector3)point, 1f);
                        }
                    }
                }
                else if (usingWaypoints)
                {
                    if (agent.isOnNavMesh && Vector3.Distance(horse.transform.position, waypoint[currentPoint]) < 0.5f)
                    {
                        if (currentPoint >= waypoint.Count - 1)
                        {
                            waypoint.Reverse();
                            waypointSpeed.Reverse();
                            currentPoint = 0;
                        }
                        currentPoint++;
                        destination((Vector3)waypoint[currentPoint], waypointSpeed[currentPoint] - 0.5f);
                        Npc.SetAimDirection(waypoint[currentPoint] - Npc.transform.position);
                    }
                    else if (hastarget)
                    {
                        hastarget = false;
                        destination((Vector3)waypoint[currentPoint], waypointSpeed[currentPoint] - 0.5f);
                        Npc.SetAimDirection(waypoint[currentPoint] - Npc.transform.position);
                    }
                }
            }

            private bool canWanderNow()
            {
                if (canWander && Npc != null && nextWanderTime < Time.time && lastTargetTime < Time.time)
                    return true;
                return false;
            }

            private void destination(Vector3 movePoint, float speed = 8f)
            {
                if (agent.isOnNavMesh)
                {
                    if (agent.isStopped) agent.isStopped = false;

                    if (agent.speed != speed)
                        agent.speed = speed;
                    if (!isInWater(movePoint))
                        agent.SetDestination(movePoint);
                }
            }

            private bool isInWater(Vector3 point)
            {
                if (TerrainMeta.WaterMap.GetHeight(point) - TerrainMeta.HeightMap.GetHeight(point) > 0.6f)
                    return true;
                return false;
            }

            public List<Vector3> FindCoverPoint(Vector3 center, float radius, float next, float y) // Thanks to ArenaWallGenerator
            {
                List<Vector3> positions = new List<Vector3>();
                float degree = 0f;

                while (degree < 360)
                {
                    float angle = (float)(2 * Math.PI / 360) * degree;
                    float x = center.x + radius * (float)Math.Cos(angle);
                    float z = center.z + radius * (float)Math.Sin(angle);
                    Vector3 position = new Vector3(3.2009f, 2.639f, 0.2845f);
                    position = new Vector3(x, center.y, z);
                    if (position != Vector3.zero)
                        positions.Add(position);

                    degree += next;
                }

                return positions;
            }

            internal void ClearContainer(ItemContainer container)
            {
                container.SetLocked(true);
                while (container.itemList.Count > 0)
                {
                    var item = container.itemList[0];
                    item.RemoveFromContainer();
                    item.Remove(0f);
                }
            }

            private void OnDestroy()
            {
                CancelInvoke("GetBestTarget");

                if (theConfig.ClearHorseInventoryOnDeath && horse != null && horse.inventory != null)
                {
                    ClearContainer(horse.inventory);
                    if (theConfig.ClearHorseInventoryOnDeath && !deathLocation.ContainsKey(horse.transform.position))
                        deathLocation.Add(horse.transform.position, true);
                }
                else if (horse != null && horse.inventory != null) horse.inventory.SetLocked(false);

                if (ins.totalTypes.ContainsKey(theConfigKey))
                    if (ins.totalTypes[theConfigKey].Contains(netID))
                        ins.totalTypes[theConfigKey].Remove(netID);

                if (ins.rebooting)
                {
                    if (Npc != null && !Npc.IsDestroyed)
                        Npc.Kill();
                }
                else if (Npc != null && !Npc.IsDestroyed)
                    Npc.Die();

                if (theConfig.KillHorseOnRiderDeath && !horse.IsDestroyed)
                {
                    if (theConfig.ClearHorseInventoryOnDeath && !deathLocation.ContainsKey(horse.transform.position))
                        deathLocation.Add(horse.transform.position, true);
                    horse.Die();
                }
                else if (horse != null && !horse.IsDestroyed)
                {
                    if (agent != null)
                        UnityEngine.Object.Destroy(agent);

                    if (horse != null && !horse.IsDestroyed)
                    {
                        horse.DismountAllPlayers();
                        horse.transform.position = horse.transform.position + new Vector3(0, -0.01f, 0);
                        if (theConfig.ClearHorseInventoryOnDeath && !deathLocation.ContainsKey(horse.transform.position))
                            deathLocation.Add(horse.transform.position, true);
                        horse.inventory.SetLocked(false);
                    }

                }
                if (ins.HorseAndRider.ContainsKey(netID)) ins.HorseAndRider.Remove(netID);
                if (!ins.rebooting && !string.IsNullOrEmpty(theConfig.WayPoint) && theConfig.WayPointRespawn > 0)
                    respawnHorse(SpawnLocation, theConfig, theConfig.WayPointRespawn, theConfigKey);
            }

            public void OnKilled()
            {
                if (theConfig.ClearHorseInventoryOnDeath && horse != null)
                {
                    if (theConfig.ClearHorseInventoryOnDeath && !deathLocation.ContainsKey(horse.transform.position))
                        deathLocation.Add(horse.transform.position, true);
                }
            }
        }

        internal void ClearContainer(ItemContainer container)
        {
            container.SetLocked(true);
            while (container.itemList.Count > 0)
            {
                var item = container.itemList[0];
                item.RemoveFromContainer();
                item.Remove(0f);
            }
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity.name.Contains("horse.co"))
            {
                BaseCorpse corpse = entity as BaseCorpse;
                NextTick(() =>
                {
                    if (corpse != null)
                    {
                        foreach (var key in deathLocation.ToList())
                        {
                            if (Vector3.Distance(corpse.transform.position, key.Key) < 1.5f)
                            {
                                LootableCorpse component = corpse.GetComponent<LootableCorpse>();
                                if (component != null)
                                    foreach (ItemContainer containers in component.containers)
                                        ClearContainer(containers);
                                deathLocation.Remove(key.Key);
                                break;
                            }
                        }
                    }
                });
            }
        }

        public static void respawnHorse(Vector3 success, horseSpawns waypoints, float rTime, string theKey)
        {
            if (rTime > 0)
                ins.timer.Once(rTime, () => ins.spawnHorseAndRider(waypoints, success, theKey));
        }

        object OnEntityTakeDamage(global::HumanNPC entity, HitInfo hitinfo)
        {
            if (entity == null || hitinfo == null || hitinfo.Initiator == null) return null;
            if (hitinfo.Initiator is global::HumanNPC && (hitinfo.Initiator as global::HumanNPC).GetMountedVehicle() != null)
            {
                return true;
            }
            if (entity.GetMountedVehicle() != null && entity.GetMountedVehicle() is BaseRidableAnimal && hitinfo.damageTypes != null)
            {
                hitinfo.damageTypes.ScaleAll(10f);
                return null;
            }
            return null;
        }

        object OnEntityTakeDamage(RidableHorse entity, HitInfo hitinfo)
        {
            if (hitinfo == null || hitinfo.Initiator == null) return null;
            if (hitinfo.Initiator is global::HumanNPC)
            {
                global::HumanNPC scientist = hitinfo.Initiator as global::HumanNPC;
                if (scientist?.GetMountedVehicle() != null)
                    return true;
            }
            return null;
        }

        object CanDismountEntity(global::HumanNPC player, BaseMountable entity)
        {
            if (entity.name.Contains("saddletest") && !player.IsDestroyed && !player.IsDead() && !entity.IsDead() && !entity.IsDestroyed)
            {
                return true;
            }
            return null;
        }
    }
}