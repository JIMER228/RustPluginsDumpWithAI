using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rust;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("SharkBait", "Colon Blow", "2.0.0")]
    class SharkBait : RustPlugin
    {

        // changed to Rust Shark Entity
        // config changed to reflect less options
        // added Spawns at Underwater Labs
        // added prefab string to config

        #region Load

        const string permAdmin = "sharkbait.admin";

        private void Loaded()
        {
            LoadMessages();
            permission.RegisterPermission(permAdmin, this);
        }

        private void OnServerInitialized()
        {
            timer.In(10, RespawnAllSharks);
        }

        bool UserHasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);

        #endregion

        #region Configuration

        private static PluginConfig config;

        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Autospawn - Enable the Autospawn feature ? ")] public bool EnableAutoSpawn { get; set; }
            [JsonProperty(PropertyName = "Autospawn - Enable autospawn at Rock Formations ? ")] public bool EnableSpawnAtRockFormations { get; set; }
            [JsonProperty(PropertyName = "Autospawn - Enable autospawn at Dive Sites ? ")] public bool EnableSpawnAtDiveSites { get; set; }
            [JsonProperty(PropertyName = "Autospawn - Enable autospawn at Floating Loot Sites ? ")] public bool EnableSpawnAtFloatingLoot { get; set; }
            [JsonProperty(PropertyName = "Autospawn - Enable autospawn at Oil Rigs ? ")] public bool EnableSpawnAtOilRig { get; set; }
            [JsonProperty(PropertyName = "Autospawn - Enable autospawn at Lab Sites ? ")] public bool EnableSpawnAtLab { get; set; }

            [JsonProperty(PropertyName = "Loot Prefab to spawn when shark is killed : ")] public string LootPrefabString { get; set; }

            [JsonProperty(PropertyName = "Sharks will spawn when player is this distance from site : ")] public float SpawnActivationRadius { get; set; }
            [JsonProperty(PropertyName = "Sharks will despawn after this many seconds : ")] public float SharkDespawnTime { get; set; }
            [JsonProperty(PropertyName = "Chances a site will spawn a Shark when player gets close enough : ")] public int SharkSpawnChance { get; set; }

            public static PluginConfig DefaultConfig() => new PluginConfig()
            {
                EnableAutoSpawn = true,
                EnableSpawnAtRockFormations = true,
                EnableSpawnAtDiveSites = true,
                EnableSpawnAtFloatingLoot = true,
                EnableSpawnAtOilRig = true,
                EnableSpawnAtLab = true,

                LootPrefabString = "assets/bundled/prefabs/radtown/crate_underwater_basic.prefab",

                SpawnActivationRadius = 50f,
                SharkDespawnTime = 500f,
                SharkSpawnChance = 50
            };
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("New configuration file created!!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            SaveConfig();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Localization

        private void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["noperms"] = "You don't have permission to use this command.",
                ["sharkhasspawned"] = "There is a shark in the water !!!!",
                ["sharkhasdied"] = "You have killed a shark.. good job !!",
            }, this);
        }

        #endregion

        #region Commands

        [ChatCommand("sharkbait")]
        void chatSharkBait(BasePlayer player, string command, string[] args)
        {
            if (UserHasPermission(player, permAdmin))
                AddSharkEntity(player.transform.position);
            else
                PrintToChat(player, lang.GetMessage("noperms", this, player.UserIDString));
        }

        [ChatCommand("sharkbait.respawn")]
        void chatSharkBaitRespawn(BasePlayer player, string command, string[] args)
        {
            if (UserHasPermission(player, permAdmin))
                RespawnAllSharks();
            else
                PrintToChat(player, lang.GetMessage("noperms", this, player.UserIDString));
        }

        [ConsoleCommand("sharkbait.respawn")]
        void cmdConsoleSharkBaitRespawn(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() ?? null;
            if (player == null) { RespawnAllSharks(); return; }
            if (player != null)
            {
                if (UserHasPermission(player, permAdmin))
                    RespawnAllSharks();
                else
                    PrintToChat(player, lang.GetMessage("noperms", this, player.UserIDString));
            }
        }

        [ChatCommand("sharkbait.killall")]
        void chatSharkBaitKillAll(BasePlayer player, string command, string[] args)
        {
            if (UserHasPermission(player, permAdmin))
                RemoveAllSharks();
            else
                PrintToChat(player, lang.GetMessage("noperms", this, player.UserIDString));
        }

        #endregion

        #region Hooks

        private void RemoveAllSharks()
        {
            DestroyAll<SharkController>();
            DestroyAll<SharkSpawnController>();
        }

        void OnEntitySpawned(BaseEntity entity, UnityEngine.GameObject gameObject)
        {
            if (!config.EnableAutoSpawn) return;
            if (gameObject == null) return;
            if (gameObject.name.Contains("offshore/oilrig") && config.EnableSpawnAtOilRig)
            {
                RespawnHandler(gameObject);
            }
            if (gameObject.name.Contains("rockformation_underwater") && config.EnableSpawnAtRockFormations)
            {
                RespawnHandler(gameObject);
            }
            if (gameObject.name.Contains("junkpile_water") && config.EnableSpawnAtFloatingLoot)
            {
                RespawnHandler(gameObject);
            }
            if (gameObject.name.Contains("divesite/divesite") && config.EnableSpawnAtDiveSites)
            {
                RespawnHandler(gameObject);
            }
            if (gameObject.name.Contains("underwaterlabsdwelling") && config.EnableSpawnAtLab)
            {
                RespawnHandler(gameObject);
            }
        }

        void RespawnAllSharks()
        {
            if (!config.EnableAutoSpawn) return;
            RemoveAllSharks();

            var allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var gobject in allobjects)
            {
                if (gobject.name.Contains("offshore/oilrig") && config.EnableSpawnAtRockFormations)
                {
                    RespawnHandler(gobject);
                }
                if (gobject.name.Contains("rockformation_underwater") && config.EnableSpawnAtRockFormations)
                {
                    RespawnHandler(gobject);
                }
                if (gobject.name.Contains("junkpile_water") && config.EnableSpawnAtFloatingLoot)
                {
                    RespawnHandler(gobject);
                }
                if (gobject.name.Contains("divesite/divesite") && config.EnableSpawnAtDiveSites)
                {
                    RespawnHandler(gobject);
                }
                if (gobject.name.Contains("underwaterlabsdwelling") && config.EnableSpawnAtLab)
                {
                    RespawnHandler(gobject);
                }
            }
            PrintWarning("Respawn of Shark Population has been completed.");
        }

        public void RespawnHandler(GameObject foundGameObject)
        {
            var pos = foundGameObject.transform.position;

            int roll = UnityEngine.Random.Range(0, 100);
            if (roll < config.SharkSpawnChance)
            {
                var hascontroller = foundGameObject.GetComponent<SharkSpawnController>() ?? null;
                if (hascontroller != null) GameObject.Destroy(hascontroller);

                if (foundGameObject.name.Contains("underwaterlabsdwelling"))
                {
                    List<BaseEntity> nearshark = new List<BaseEntity>();
                    Vis.Entities<BaseEntity>(pos, 60f, nearshark);
                    foreach (BaseEntity shark in nearshark)
                    {
                        if (shark.GetComponentInParent<SharkController>())
                        {
                            return;
                        }
                    }
                }
                GameObject newObject = new GameObject();
                newObject.transform.position = pos;
                newObject.AddComponent<SharkSpawnController>();
            }
        }

        public void AddSharkEntity(Vector3 pos)
        {
            List<BaseEntity> nearshark = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(pos, 30f, nearshark);
            foreach (BaseEntity shark in nearshark)
            {
                if (shark.GetComponentInParent<SharkController>())
                {
                    return;
                }
            }
            SpawnShark(pos);
        }

        void SpawnShark(Vector3 pos)
        {
            string prefabsharkfin = "assets/rust.ai/agents/fish/simpleshark.prefab";

            var groundy = TerrainMeta.HeightMap.GetHeight(pos);
            if (pos.y < groundy) pos.y = groundy + 2f;
            if (pos.y > -0.5f) pos.y = -0.5f;
            BaseEntity sharkent = GameManager.server.CreateEntity(prefabsharkfin, new Vector3(pos.x, pos.y, pos.z), Quaternion.identity, true);
            sharkent.Spawn();
            var addentity = sharkent.gameObject.AddComponent<SharkController>();
        }

        void Unload()
        {
            RemoveAllSharks();
        }

        void DestroyAll<T>()
        {
            var objects = GameObject.FindObjectsOfType(typeof(T));
            if (objects != null)
                foreach (var gameObj in objects)
                {
                    GameObject.Destroy(gameObj);
                }
        }

        #endregion

        #region Shark Spawn Controller

        class SharkSpawnController : MonoBehaviour
        {
            SharkBait _instance;
            SphereCollider detectionradius;
            Vector3 spawnlocation;
            bool doactivation;
            Timer mytimer;

            void Awake()
            {
                _instance = new SharkBait();
                detectionradius = gameObject.AddComponent<SphereCollider>();
                detectionradius.gameObject.layer = (int)Layer.Reserved1;
                detectionradius.isTrigger = true;
                detectionradius.radius = config.SpawnActivationRadius;
                spawnlocation = detectionradius.transform.position;
            }

            private void OnTriggerEnter(Collider col)
            {
                if (col == null || doactivation) return;
                if (col.name.Contains("/player/player.prefab"))
                {
                    var player = col.GetComponentInParent<BasePlayer>() ?? null;
                    if (player != null)
                    {
                        doactivation = true;
                        _instance.AddSharkEntity(spawnlocation);
                        ToggleTrigger();
                    }
                }
            }

            void ToggleTrigger()
            {
                mytimer = _instance.timer.Once(System.Convert.ToSingle(config.SharkDespawnTime), () =>
                {
                    if (this == null) { mytimer.Destroy(); return; }
                    doactivation = false;
                });
            }

            void OnDestroy()
            {
                GameObject.Destroy(detectionradius);
                GameObject.Destroy(this);
            }
        }

        #endregion

        #region Shark Controller

        private class SharkController : MonoBehaviour
        {
            private BaseEntity sharkEntity;
            private float despawncounter;
            private bool spawnLoot;

            private void Awake()
            {
                sharkEntity = GetComponentInParent<BaseEntity>();
                despawncounter = 0f;
                spawnLoot = true;
            }

            private void FixedUpdate()
            {
                if (despawncounter >= (config.SharkDespawnTime * 15) && sharkEntity != null) { spawnLoot = false;  sharkEntity.Invoke("KillMessage", 0.1f); return; }
                despawncounter = despawncounter + 1f;
            }

            private void SpawnLoot(BaseEntity sharkEntity)
            {
                var lootbox = GameManager.server.CreateEntity(config.LootPrefabString, sharkEntity.transform.position, sharkEntity.transform.rotation, true);
                FreeableLootContainer lootcont = lootbox.GetComponent<FreeableLootContainer>();
                lootbox.Spawn();
                lootcont.GetRB().isKinematic = false;
                lootcont.buoyancy.enabled = true;
                lootcont.buoyancy.buoyancyScale = 1f;
                lootcont.SetFlag(BaseEntity.Flags.Reserved8, false, false);

                var getBox = lootbox.GetComponent<FreeableLootContainer>();
                if (getBox) getBox.SetFlag(BaseEntity.Flags.Reserved8, false, false);

                ItemContainer containerinv = lootbox.GetComponent<StorageContainer>().inventory;
                if (containerinv != null)
                {
                    containerinv.Clear();
                    Item addFishMeat = ItemManager.CreateByItemID(989925924, 5);
                    containerinv.itemList.Add(addFishMeat);
                    addFishMeat.parent = containerinv;
                    addFishMeat.MarkDirty();
                }
            }

            private void OnDestroy()
            {
                if (spawnLoot) SpawnLoot(sharkEntity);
                if (sharkEntity != null) sharkEntity.Invoke("KillMessage", 0.1f);
            }
        }

        #endregion

    }
}