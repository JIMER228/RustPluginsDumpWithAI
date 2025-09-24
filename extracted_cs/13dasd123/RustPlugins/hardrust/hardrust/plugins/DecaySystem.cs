using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.Collections;
using System.Diagnostics;
using Rust;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
namespace Oxide.Plugins
{
    [Info("DecaySystem", "TopPlugin.ru", "1.2.0")]
    public class DecaySystem : RustPlugin
    {
        #region Ref
        [PluginReference] Plugin ZoneManager;
        #endregion

        private float doorTimeout;
        private float doorRadius;
        private float timeout = 3600;
        private float TwigsTicks;
        private float MultiplierWood;
        private float MultiplierStone;
        private float MultiplierMetal;
        private float MultiplierTopTier;
        private Dictionary<string, int> decaySettingsInCupboard = new Dictionary<string, int>();
        private Dictionary<string, int> decaySettingsWithoutCupboard = new Dictionary<string, int>();
        private List<string> baseDecayPrefabs = new List<string>()
        {"foundation", "foundation.triangle", "gates.external.high.wood", "wall.external.high.wood", "gates.external.high.stone", "wall.external.high.stone", };
        private List<string> defaultDecayPrefabs = new List<string>()
        {"foundation", "foundation.triangle", "wall.external.high.stone", "cupboard.tool.deployed", "box.wooden.large", "sleepingbag_leather_deployed", "stocking_large_deployed", "stocking_small_deployed", "furnace", "woodbox_deployed", "barricade.sandbags", "barricade.stone", "jackolantern.happy", "barricade.concrete", "floor.grill", "barricade.metal", "autoturret_deployed", "campfire", "repairbench_deployed", "beartrap", "wall.external.high.wood", "bed_deployed", "gates.external.high.stone", "furnace.large", "refinery_small_deployed", "reactivetarget_deployed", "barricade.woodwire", "landmine", "lantern.deployed", "ceilinglight.deployed", "gates.external.high.wood", "spikes.floor", "barricade.wood", "jackolantern.angry", "water_catcher_large"};
        static Dictionary<string, Stopwatch> watches = new Dictionary<string, Stopwatch>();
        private static int TriggerMask = LayerMask.GetMask("Trigger");
        private DynamicConfigFile doortimers_file = Interface.Oxide.DataFileSystem.GetFile("DecaySystem_DoorTimers");
        private DynamicConfigFile blocksDoorFile = Interface.Oxide.DataFileSystem.GetFile("DecaySystem_Blocks");
        Dictionary<uint, float> doorTimers = new Dictionary<uint, float>();
        Dictionary<uint, uint> blocksDoor = new Dictionary<uint, uint>();
        List<BaseCombatEntity> decayEntities;
        Dictionary<uint, uint> blockCupboards = new Dictionary<uint, uint>();
        int WorldBuildingsLayer = LayerMask.GetMask("Construction", "World", "Terrain");
        int BuildingsLayer = LayerMask.GetMask("Construction");
        int DeployedLayer = LayerMask.GetMask("Deployed");
        private bool init = false;
        private bool isdecay = false;
        private UnityEngine.Vector3 offset = new UnityEngine.Vector3(0, 0.5f, 0);
        private Coroutine decayCoroutine;
        

        [ChatCommand("decay")]
        void cmdChatDecay(BasePlayer player)
        {
            if (!player.IsAdmin)
                return;
            RunDecay();
        }

        [ConsoleCommand("decay")]
        void cmdDecay(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
                return;
            RunDecay();
        }

        protected override void LoadDefaultConfig()
        {
            Config["Настройки: гниение соломяных обьектов (в часах)"] = TwigsTicks = GetConfig("Настройки: гниение соломяных обьектов (в часах)", 1f);
            Config["Приумножение гниения дерева"] = MultiplierWood = GetConfig("Приумножение гниения дерева", 1f);
            Config["Приумножение гниения камня"] = MultiplierStone = GetConfig("Приумножение гниения камня", 1f);
            Config["Приумножение гниения металла"] = MultiplierMetal = GetConfig("Приумножение гниения металла", 1f);
            Config["Приумножение гниения мвк"] = MultiplierTopTier = GetConfig("Приумножение гниения мвк", 1f);
            Config["Настройки: задержка перед гниением фундаментов, если в радиусе (настройка) метров открывали дверь (в часах)"] = doorTimeout = GetConfig("Настройки: задержка перед гниением фундаментов, если в радиусе (настройка) метров открывали дверь (в часах)", 5.0f);
            Config["Настройки: размер радиуса для поиска дверей"] = doorRadius = GetConfig("Настройки: размер радиуса для поиска дверей", 10f);
            Config["Гниение объектов которые находятся в зоне действия шкафа (в часах)"] = decaySettingsInCupboard = GetConfig("Гниение объектов которые находятся в зоне действия шкафа (в часах)", defaultDecayPrefabs.ToDictionary(p => p, p => (object)24)).ToDictionary(p => p.Key, p => int.Parse(p.Value.ToString()));
            Config["Гниение объектов которые находятся вне зоны действия шкафа (в часах)"] = decaySettingsWithoutCupboard = GetConfig("Гниение объектов которые находятся вне зоны действия шкафа (в часах)", defaultDecayPrefabs.ToDictionary(p => p, p => (object)36)).ToDictionary(p => p.Key, p => int.Parse(p.Value.ToString()));
            SaveConfig();
        }

        T GetConfig<T>(string name, T defaultValue)
        {
            return Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));
        }

        void DoorHandle(Door door)
        {
            doorTimers[door.net.ID] = doorTimeout;
        }

        float GetDoorTimeout(BaseCombatEntity block)
        {
            uint blockID = block.net.ID;
            uint doorNetID;
            if (blocksDoor.TryGetValue(blockID, out doorNetID))
            {
                float doorDecayCooldown;
                if (doorTimers.TryGetValue(doorNetID, out doorDecayCooldown))
                {
                    Door door = BaseNetworkable.serverEntities.Find(doorNetID) as Door;
                    if (door != null && doorDecayCooldown > 0)
                    {
                        return doorDecayCooldown;
                    }
                    else
                    {
                        doorTimers.Remove(doorNetID);
                    }
                }

                blocksDoor.Remove(blockID);
            }

            var position = block.GetNetworkPosition();
            List<BaseEntity> nearby = new List<BaseEntity>();
            Vis.Entities(position, doorRadius, nearby, BuildingsLayer, QueryTriggerInteraction.Ignore);
            float timeout = -1;
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
                CommunityEntity.ServerInstance.StopCoroutine(decayCoroutine);
            decayCoroutine = CommunityEntity.ServerInstance.StartCoroutine(Decay());
        }

        /// <summary>
        /// Start Stopwatch
        /// </summary>
        /// <param name = "name">KEY</param>
        public static void StopwatchStart(string name)
        {
            watches[name] = Stopwatch.StartNew();
        }

        /// <summary>
        /// Get Elapsed Milliseconds
        /// </summary>
        /// <param name = "name">KEY</param>
        /// <returns></returns>
        public static long StopwatchElapsedMilliseconds(string name)
        {
            return watches[name].ElapsedMilliseconds;
        }

        /// <summary>
        /// Remove StopWatch
        /// </summary>
        /// <param name = "name"></param>
        public static void StopwatchStop(string name)
        {
            watches.Remove(name);
        }

        

        IEnumerator Decay()
        {
            isdecay = true;
            PrintToChat("");
            List<uint> remove = doorTimers.Keys.ToList().Where(door => (doorTimers[door] -= timeout / 3600f) < 0).ToList();
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
                            Puts($"");
                        lastpercent = percent;
                        yield return new WaitForSeconds(0.2f);
                    }
                }

                if (Performance.report.frameRate < 150 || Performance.current.frameRate < 150 || i % 10 == 0)
                    yield return new WaitForEndOfFrame();
                if (block == null)
                    continue;
                if (block.IsDestroyed)
                    continue;
                if (block.transform == null)
                    continue;
                var t = GetDoorTimeout(block);
                var buildingBlock = block as BuildingBlock;
                if (t <= 0 || (buildingBlock != null && buildingBlock.grade == BuildingGrade.Enum.Twigs))
                {
                    var inCupboard = block.GetBuildingPrivilege(block.WorldSpaceBounds());
                    var DecaySettings = inCupboard ? decaySettingsInCupboard : decaySettingsWithoutCupboard;
                    var multiplier = 1f;
                    var reply = 16;
                    float hp = 0f;

                    if (buildingBlock != null)
                    {
                        switch (buildingBlock.grade)
                        {
                            case BuildingGrade.Enum.Twigs:
                                hp = block.MaxHealth() / TwigsTicks;
                                break;
                            case BuildingGrade.Enum.Wood:
                                multiplier = MultiplierWood;
                                break;
                            case BuildingGrade.Enum.Stone:
                                multiplier = MultiplierStone;
                                break;
                            case BuildingGrade.Enum.Metal:
                                multiplier = MultiplierMetal;
                                break;
                            case BuildingGrade.Enum.TopTier:
                                multiplier = MultiplierTopTier;
                                break;
                        }
                    }

                    if (Math.Abs(hp) < 0.001f)
                        hp = block.MaxHealth() / (DecaySettings[block.ShortPrefabName] * multiplier);
                    block.Hurt(hp, DamageType.Decay);
                    if (block.IsDead())
                    {
                        die++;
                        yield return new WaitForEndOfFrame();
                    }
                }
            }

            var time = DateTime.UtcNow.Subtract(start).TotalSeconds.ToString("F2");
            Puts($"");
            PrintToChat($"");
            List<BaseCombatEntity> list = new List<BaseCombatEntity>(decayEntities.Count);
            i = 0;
            foreach (var p in decayEntities.ToArray())
            {
                if (p != null)
                    list.Add(p);
                if (i++ % 100 == 0)
                    yield return new WaitForFixedUpdate();
            }

            decayEntities = list;
            isdecay = false;
        }

        void LoadDoorTimers()
        {
            doorTimers = doortimers_file.ReadObject<Dictionary<string, float>>().ToDictionary(v => uint.Parse(v.Key), t => t.Value);
            blocksDoor = blocksDoorFile.ReadObject<Dictionary<string, uint>>().ToDictionary(k => Convert.ToUInt32(k.Key), v => v.Value);
        }

        void Unload()
        {
            OnServerSave();
            if (decayCoroutine != null)
                ServerMgr.Instance.StopCoroutine(decayCoroutine);
        }

        void OnServerSave()
        {
            Dictionary<string, float> serializedTimers = new Dictionary<string, float>();
            foreach (var d in doorTimers)
            {
                if (!serializedTimers.ContainsKey(d.Key.ToString()))
                    serializedTimers.Add(d.Key.ToString(), d.Value);
            }

            doortimers_file.WriteObject(serializedTimers);
            blocksDoorFile.WriteObject(blocksDoor.ToDictionary(k => k.Key.ToString(), v => v.Value));
        }

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
            foreach (var ent in ents)
            {
                if (ent == null)
                    continue;
                DecayEntity decEnt = ent as DecayEntity;
                NextTick(() =>
                {
                    if (decEnt != null)
                    {
                        decEnt.CancelInvoke("RunDecay");
                    }

                    if (IsDecayEntity(ent))
                        decayEntities.Add(ent);
                }

                );
            }
            timer.Once(0.03f, () => { Puts($"Загрузка объектов прошла успешно! подсчитано: {decayEntities.Count}"); });
            init = true;
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!init)
                return;
            if (entity?.net?.ID == null)
                return;
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
            }

            );
        }

        bool IsDecayEntity(BaseNetworkable entity)
        {
            if (decaySettingsInCupboard.ContainsKey(entity.ShortPrefabName))
            {
                if (!baseDecayPrefabs.Contains(entity.ShortPrefabName))
                {
                    RaycastHit hit;
                    var ray = new Ray(entity.transform.position + offset, entity.transform.TransformDirection(UnityEngine.Vector3.down));
                    if (!Physics.Raycast(ray, out hit, 5, WorldBuildingsLayer, QueryTriggerInteraction.Ignore))
                        return true;
                    if (hit.transform.gameObject.layer == BuildingsLayer)
                        return false;
                }

                return true;
            }

            return false;
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (!init)
                return;
            if (entity?.net?.ID == null)
                return;
            if (entity is Door)
                doorTimers.Remove(entity.net.ID);
            var combatEnt = entity as BaseCombatEntity;
            if (combatEnt != null && decaySettingsInCupboard.ContainsKey(entity.ShortPrefabName))
                decayEntities.Remove(combatEnt);
        }

        void OnDoorOpened(Door door, BasePlayer player)
        {
            DoorHandle(door);
        }

        void OnDoorClosed(Door door, BasePlayer player)
        {
            DoorHandle(door);
        }

        bool inZone(UnityEngine.Vector3 vec)
        {
            if (ZoneManager == null)
                return false;
            return (bool)ZoneManager.Call("inZone", vec);
        }
    }
} 
               