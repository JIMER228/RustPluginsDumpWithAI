// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Rust;
using UnityEngine;
using System.Diagnostics;

namespace Oxide.Plugins
{
    [Info("DecaySystem", "bazuka5801", "1.1.0")]
    class DecaySystem : RustPlugin
    {
        #region CLASSES

        class Vector3
        {
            public float x, y, z;

            public Vector3(string s)
            {
                var ss = s.Split(' ');
                this.x = float.Parse(ss[0]);
                this.y = float.Parse(ss[1]);
                this.z = float.Parse(ss[2]);
            }

            public string Save() => $"{x} {y} {z}";

            public Vector3(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public Vector3(UnityEngine.Vector3 vec)
            {
                this.x = vec.x;
                this.y = vec.y;
                this.z = vec.z;
            }

            public UnityEngine.Vector3 ToVector3() => new UnityEngine.Vector3(x, y, z);

            public static bool Equal(Vector3 a, Vector3 b)
                => UnityEngine.Vector3.Distance(a.ToVector3(), b.ToVector3()) < 0.001;

            public static bool Equal(UnityEngine.Vector3 a, UnityEngine.Vector3 b)
                => UnityEngine.Vector3.Distance(a, b) < 0.001;

        }

        #endregion

        #region PLUGINS API

        [PluginReference]
        Plugin ZoneManager;

        bool inZone(UnityEngine.Vector3 vec)
        {
            if (ZoneManager == null) return false;
            return (bool)ZoneManager.Call("inZone", vec);
        }

        #endregion

        #region CONST VARIABLES

        private List<string> baseDecayPrefabs = new List<string>()
        {
            "foundation",
            "foundation.triangle",
            "gates.external.high.wood",
            "wall.external.high.wood",
            "gates.external.high.stone",
            "wall.external.high.stone",
        };

        private List<string> defaultDecayPrefabs = new List<string>()
        {
            "foundation",
            "foundation.triangle",
            "wall.external.high.stone",
            "cupboard.tool.deployed",
            "box.wooden.large",
            "sleepingbag_leather_deployed",
            "stocking_large_deployed",
            "stocking_small_deployed",
            "furnace",
            "woodbox_deployed",
            "barricade.sandbags",
            "barricade.stone",
            "jackolantern.happy",
            "barricade.concrete",
            "floor.grill",
            "barricade.metal",
            "autoturret_deployed",
            "campfire",
            "repairbench_deployed",
            "beartrap",
            "wall.external.high.wood",
            "bed_deployed",
            "gates.external.high.stone",
            "furnace.large",
            "refinery_small_deployed",
            "reactivetarget_deployed",
            "barricade.woodwire",
            "landmine",
            "lantern.deployed",
            "ceilinglight.deployed",
            "gates.external.high.wood",
            "spikes.floor",
            "barricade.wood",
            "jackolantern.angry",
            "water_catcher_large"
        };

        #endregion

        List<BaseCombatEntity> decayEntities;
        Dictionary<uint, int> doorTimers = new Dictionary<uint, int>();
        Dictionary<uint, uint> blocksDoor = new Dictionary<uint, uint>();
        int WorldBuildingsLayer = LayerMask.GetMask("Construction", "World", "Terrain");
        int BuildingsLayer = LayerMask.GetMask("Construction");


        #region CONGIGURATION

        private int doorTimeout;
        private float doorRadius;
        private float timeout = 3600;
        private Dictionary<string, int> decaySettings = new Dictionary<string, int>();
        protected override void LoadDefaultConfig()
        {
            Config["Задержка после открытия/закрытия двери"] =
                doorTimeout = GetConfig("Задержка после открытия/закрытия двери", 5);
            Config["Радиус обнаружения дверей"] =
                doorRadius = GetConfig("Радиус обнаружения дверей", 10f);
            Config["Гниение объектов"] =
                decaySettings =
                    GetConfig("Гниение объектов", defaultDecayPrefabs.ToDictionary(p => p, p => (object)2))
                        .ToDictionary(p => p.Key, p => int.Parse(p.Value.ToString()));
            SaveConfig();
        }

        T GetConfig<T>(string name, T defaultValue)
            => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));

        #endregion

        #region DATA

        private DynamicConfigFile doortimers_file = Interface.Oxide.DataFileSystem.GetFile("DecaySystem_DoorTimers");
        private DynamicConfigFile blocksDoorFile = Interface.Oxide.DataFileSystem.GetFile("DecaySystem_Blocks");
        void LoadDoorTimers()
        {
            doorTimers =
                doortimers_file.ReadObject<Dictionary<string, int>>()
                    .ToDictionary(v => uint.Parse(v.Key), t => t.Value);
            blocksDoor = blocksDoorFile.ReadObject<Dictionary<string, uint>>()
                .ToDictionary(k => Convert.ToUInt32(k.Key), v => v.Value);
        }

        void Unload()
        {
            OnServerSave();
            if (decayCoroutine != null)
                ServerMgr.Instance.StopCoroutine(decayCoroutine);
        }


        void OnServerSave()
        {
            Dictionary<string, int> serializedTimers = new Dictionary<string, int>();
            foreach (var d in doorTimers)
            {
                if (!serializedTimers.ContainsKey(d.Key.ToString())) serializedTimers.Add(d.Key.ToString(), d.Value);
            }

            doortimers_file.WriteObject(serializedTimers);

            blocksDoorFile.WriteObject(blocksDoor.ToDictionary(k => k.Key.ToString(), v => v.Value));
        }

        #endregion

        #region COMMANDS
[ConsoleCommand("repairallentities")]
        void cmdRepairAllEntities(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;

            var objs = UnityEngine.Object.FindObjectsOfType<BaseCombatEntity>().Where(IsDecayEntity).ToList();
            foreach(var ent in objs)
            {
                if (ent != null)
                ent.Heal(ent.MaxHealth()-ent.health);
            }            
            Puts($"Repaired {objs.Count} entities!");
        }

        [ChatCommand("decay")]
        void cmdChatDecay(BasePlayer player)
        {
            if (!player.IsAdmin) return;
            RunDecay();
        }

        [ConsoleCommand("decay")]
        void cmdDecay(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            RunDecay();
        }

        /*[ConsoleCommand("setdoors")]
        void cmdSetDoors(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            foreach (var door in UnityEngine.Object.FindObjectsOfType<Door>())
            {
                if (door?.net?.ID != null)
                    doorTimers[door.net.ID] = doorTimeout;
            }
            Puts("success");
        }*/

        #endregion

        #region OXIDE HOOKS

        private bool init = false;
        private bool isdecay = false;
        private UnityEngine.Vector3 offset = new UnityEngine.Vector3(0, 0.5f, 0);
        private Coroutine decayCoroutine;

        void OnServerInitialized()
        {
            LoadDefaultConfig();
            LoadDoorTimers();
            timer.Every(timeout, RunDecay);
            InitDecayEntities();
        }

        void InitDecayEntities()
        {
            var ents = UnityEngine.Object.FindObjectsOfType<BaseCombatEntity>();
            decayEntities = new List<BaseCombatEntity>(ents.Length);

            Parallel.For(0, ents.Length, (i)=>
            {

                var ent = ents[i];
                if (ent == null) return;
                DecayEntity decEnt = ent as DecayEntity;
                if (decEnt != null)
                {
                    decEnt.CancelInvoke("RunDecay");
                }
                if (IsDecayEntity(ent))
                    decayEntities.Add(ent);
            });
                    Puts($"Загрузка объектов прошла успешно! Count: {ents.Length}");                    
                    init = true;
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!init) return;
            if (entity?.net?.ID == null) return;
            if (IsDecayEntity(entity) && !decayEntities.Contains((BaseCombatEntity)entity))
                decayEntities.Add((BaseCombatEntity)entity);
            if (entity is Door)
                doorTimers[entity.net.ID] = doorTimeout;
            NextTick(() =>
            {
                DecayEntity decEnt = entity as DecayEntity;
                if (decEnt != null)
                {
                    decEnt.CancelInvoke("RunDecay");
                }
            });
        }

        bool IsDecayEntity(BaseNetworkable entity)
        {
            if (decaySettings.ContainsKey(entity.ShortPrefabName))
            {
                if (!baseDecayPrefabs.Contains(entity.ShortPrefabName))
                {
                    RaycastHit hit;
                    var ray = new Ray(entity.transform.position + offset,
                        entity.transform.TransformDirection(UnityEngine.Vector3.down));
                    if (!Physics.Raycast(ray, out hit, 5, WorldBuildingsLayer, QueryTriggerInteraction.Ignore))
                        return true;
                    if (hit.transform.gameObject.layer == BuildingsLayer)
                        return false;
                }
                return true;
            }
            return false;
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!init) return;
            if (entity?.net?.ID == null) return;
            if (entity is Door)
                doorTimers.Remove(entity.net.ID);
            if (decaySettings.ContainsKey(entity.ShortPrefabName))
                decayEntities.Remove(entity);
        }


        void OnDoorOpened(Door door, BasePlayer player)
        {
            DoorHandle(door);
        }

        void OnDoorClosed(Door door, BasePlayer player)
        {
            DoorHandle(door);
        }

        #endregion

        #region CORE


        void DoorHandle(Door door)
        {
            doorTimers[door.net.ID] = doorTimeout;
        }

        int GetDoorTimeout(BaseCombatEntity block)
        {
            uint blockID = block.net.ID;
            uint doorNetID;
            if (blocksDoor.TryGetValue(blockID, out doorNetID))
            {
                int doorDecayCooldown;
                if (doorTimers.TryGetValue(doorNetID, out doorDecayCooldown) && doorDecayCooldown > 0)
                    return doorDecayCooldown;
                blocksDoor.Remove(blockID);
            }

            var position = block.GetNetworkPosition();
            List<BaseEntity> nearby = new List<BaseEntity>();
            Vis.Entities(position, doorRadius, nearby, BuildingsLayer, QueryTriggerInteraction.Ignore);

            int timeout = -1;
            foreach (var entity in nearby)
            {
                var door = entity as Door;
                if (door != null)
                {
                    if (!doorTimers.TryGetValue(door.net.ID, out timeout) || timeout <= 0)
                        continue;

                    blocksDoor[blockID] = door.net.ID;

                    break;
                }
            }

            return timeout;
        }

        void RunDecay()
        {
            if (decayCoroutine != null)
                ServerMgr.Instance.StopCoroutine(decayCoroutine);
            decayCoroutine = ServerMgr.Instance.StartCoroutine(Decay());
        }

        IEnumerator Decay()
        {
            isdecay = true;
            PrintToChat("<size=18><color=#fee3b4>Запущена оптимизация карты</color></size>\nПожалуйста, ожидайте...");
            List<uint> remove = doorTimers.Keys.ToList().Where(door => --doorTimers[door] < 0).ToList();
            foreach (var d in remove)
                doorTimers.Remove(d);

            int i = 0;
            int count = decayEntities.Count;
            int die = 0;
            int lastpercent = -1;
            var start = DateTime.UtcNow;
            decayEntities.RemoveAll(item => item == null || item.IsDestroyed);
            StopwatchStart("DecaySystem");
            foreach (var block in decayEntities.ToArray())
            {

                i++;
                var percent = (int)(i / (float)count * 100);
                if (StopwatchElapsedMilliseconds("DecaySystem") > 10 || percent != lastpercent)
                {
                    StopwatchStart("DecaySystem");
                    if (percent != lastpercent)
                    {
                        if (percent % 20 == 0)
                            Puts($"Идёт оптимизация карты: {percent}%");
                        lastpercent = percent;
                        yield return new WaitForSeconds(0.2f);
                    }
                }

                if (Performance.report.frameRate < 150 || Performance.current.frameRate < 150)
                    yield return new WaitForEndOfFrame();

                if (block == null) continue;
                if (block.IsDestroyed) continue;
                if (block.transform == null) continue;
                if (inZone(block.transform.position)) continue;
                var t = GetDoorTimeout(block);
                if (t <= 0)
                {
                    block.Hurt(block.MaxHealth() / decaySettings[block.ShortPrefabName], DamageType.Decay);
                    if (block.IsDead())
                    {
                        die++;
                        yield return new WaitForEndOfFrame();
                    }
                }
            }
            var time = DateTime.UtcNow.Subtract(start).TotalSeconds.ToString("F2");
            Puts($"count:{count} die:{die}");
            PrintToChat($"<size=18><color=#fee3b4>Оптимизация карты завершена</color></size>\nОбработанно объектов: <color=#fee3b4>{count}</color>\nРазрушенно объектов: <color=#fee3b4>{die}</color>\nЗатрачено времени: <color=#fee3b4>{time}c</color>");
            List<BaseCombatEntity> list = new List<BaseCombatEntity>(decayEntities.Count);
            i = 0;
            foreach (var p in decayEntities.ToArray())
            {
                if (p != null) list.Add(p);
                if (i++ % 100 == 0)
                    yield return new WaitForFixedUpdate();
            }
            decayEntities = list;
            isdecay = false;
        }

        static Dictionary<string, Stopwatch> watches = new Dictionary<string, Stopwatch>();

        /// <summary>
        /// Start Stopwatch
        /// </summary>
        /// <param name="name">KEY</param>
        public static void StopwatchStart(string name)
        {
            watches[name] = Stopwatch.StartNew();
        }

        /// <summary>
        /// Get Elapsed Milliseconds
        /// </summary>
        /// <param name="name">KEY</param>
        /// <returns></returns>
        public static long StopwatchElapsedMilliseconds(string name) => watches[name].ElapsedMilliseconds;

        /// <summary>
        /// Remove StopWatch
        /// </summary>
        /// <param name="name"></param>
        public static void StopwatchStop(string name)
        {
            watches.Remove(name);
        }
        #endregion
    }
}
