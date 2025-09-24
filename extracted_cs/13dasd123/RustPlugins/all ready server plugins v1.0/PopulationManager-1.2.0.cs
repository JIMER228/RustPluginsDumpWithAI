using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("PopulationManager", "VooDoo", "1.2.0")]
    public class PopulationManager : RustPlugin
    {
        #region Fields
        private static SpawnHandler _spawnHandler;
        private float _density = 1045;

        private Dictionary<string, int> _serverPopulation = new Dictionary<string, int>();
        private List<SpawnPopulationCache> _spawnHandlerCache = new List<SpawnPopulationCache>();
        private Dictionary<string, TopologyCache> _spawnFilters = new Dictionary<string, TopologyCache>();

        private Dictionary<string, int> _currentServerPopulation = new Dictionary<string, int>();
        private List<SpawnPopulationCache> _currentSpawnHandlerCache = new List<SpawnPopulationCache>();
        private Dictionary<string, TopologyCache> _currentSpawnFilters = new Dictionary<string, TopologyCache>();

        private Queue<BaseEntity> entityCache = new Queue<BaseEntity>();
        #endregion

        #region Spawn Cache
        public class SpawnPopulationCache
        {
            public string Name { get; set; }
            public string PopulationPath { get; set; }
            public string ResourcePath { get; set; }
            public List<string> ResourceList { get; set; }

            public static SpawnPopulationCache CreateSpawnPopulation(string name, string populationPath, string resourcePath, List<string> resourceList)
            {
                return new SpawnPopulationCache()
                {
                    Name = name,
                    PopulationPath = populationPath,
                    ResourcePath = resourcePath,
                    ResourceList = resourceList
                };
            }
        }

        public class TopologyCache
        {
            public string Name { get; set; }
            public List<string> TopologyAny { get; set; }
            public List<string> TopologyAll { get; set; }
            public List<string> TopologyNot { get; set; }

            public static TopologyCache CreateTopology(string name, TerrainTopology.Enum topologyAny, TerrainTopology.Enum topologyAll, TerrainTopology.Enum topologyNot)
            {
                TopologyCache tCache = new TopologyCache();

                tCache.Name = name;
                tCache.TopologyAny = new List<string>();
                tCache.TopologyAll = new List<string>();
                tCache.TopologyNot = new List<string>();

                foreach (TerrainTopology.Enum topologyEnum in Enum.GetValues(typeof(TerrainTopology.Enum)))
                {
                    if (topologyAny.HasFlag(topologyEnum))
                    {
                        tCache.TopologyAny.Add(topologyEnum.ToString());
                    }

                    if (topologyAll.HasFlag(topologyEnum))
                    {
                        tCache.TopologyAll.Add(topologyEnum.ToString());
                    }

                    if (topologyNot.HasFlag(topologyEnum))
                    {
                        tCache.TopologyNot.Add(topologyEnum.ToString());
                    }
                }

                return tCache;
            }
        }
        #endregion

        private void OnServerInitialized()
        {
            if (isLoaded == false)
            {
                _spawnHandler = SingletonComponent<SpawnHandler>.Instance;
                _density = TerrainMeta.Size.x * TerrainMeta.Size.z * 1E-06f;

                _serverPopulation = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, int>>("SpawnableCounts");
                _spawnHandlerCache = Interface.Oxide.DataFileSystem.ReadObject<List<SpawnPopulationCache>>("SpawnPopulations");
                _spawnFilters = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, TopologyCache>>("SpawnFilters");

                GetServerPopulation();

                bool isDefault = false;
                if (_serverPopulation.Count == 0)
                {
                    _serverPopulation = _currentServerPopulation;
                    Core.Interface.Oxide.DataFileSystem.WriteObject("SpawnableCounts", _serverPopulation);
                    isDefault = true;
                }

                if (_spawnHandlerCache.Count == 0)
                {
                    _spawnHandlerCache = _currentSpawnHandlerCache;
                    Core.Interface.Oxide.DataFileSystem.WriteObject("SpawnPopulations", _spawnHandlerCache);
                    isDefault = true;
                }

                if (_spawnFilters.Count == 0)
                {
                    _spawnFilters = _currentSpawnFilters;
                    Core.Interface.Oxide.DataFileSystem.WriteObject("SpawnFilters", _spawnFilters);
                    isDefault = true;
                }

                if (isDefault == false)
                {
                    PrepareSpawnable();
                    ReduceSpawnable(true);
                }
            }
        }

        private bool isLoaded = false;
        object OnSaveLoad(Dictionary<BaseEntity, ProtoBuf.Entity> entities)
        {
            _spawnHandler = SingletonComponent<SpawnHandler>.Instance;
            _density = TerrainMeta.Size.x * TerrainMeta.Size.z * 1E-06f;

            _serverPopulation = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, int>>("SpawnableCounts");
            _spawnHandlerCache = Interface.Oxide.DataFileSystem.ReadObject<List<SpawnPopulationCache>>("SpawnPopulations");
            _spawnFilters = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, TopologyCache>>("SpawnFilters");

            UpdateSpawnHandler();

            isLoaded = true;
            return null;
        }

        private void GetServerPopulation()
        {
            for (int i = 0; i < _spawnHandler.AllSpawnPopulations.Length; i++)
            {
                if (_spawnHandler.AllSpawnPopulations[i] == null)
                {
                    PrintError("Spawn Population NULL " + i);
                    continue;
                }

                if (_spawnHandler.SpawnDistributions[i] == null)
                {
                    PrintError("Spawn SpawnDistributions NULL " + i);
                    continue;
                } 

                var spawnPopulation = _spawnHandler.AllSpawnPopulations[i];
                var spawnDistribution = _spawnHandler.SpawnDistributions[i];

                _currentServerPopulation.Add(spawnPopulation.name, _spawnHandler.GetTargetCount(spawnPopulation, spawnDistribution));

                var resourceList = new List<string>();
                if (spawnPopulation.ResourceList != null)
                {
                    for (int j = 0; j < spawnPopulation.ResourceList.Length; j++)
                    {
                        resourceList.Add(spawnPopulation.ResourceList[j].resourcePath);
                    }
                }

                if(spawnPopulation.Filter != null)
                {
                    _currentSpawnFilters.Add(spawnPopulation.name, TopologyCache.CreateTopology(spawnPopulation.name, spawnPopulation.Filter.TopologyAny, spawnPopulation.Filter.TopologyAll, spawnPopulation.Filter.TopologyNot));
                }

                _currentSpawnHandlerCache.Add(SpawnPopulationCache.CreateSpawnPopulation(spawnPopulation.name, spawnPopulation.LookupFileName(), spawnPopulation.ResourceFolder, spawnPopulation.ResourceList.Select(x => x.resourcePath).ToList()));
            }
        }

        private void UpdateSpawnHandler()
        {
            List<SpawnPopulation> spawnPopulations = new List<SpawnPopulation>();

            Type type = typeof(TerrainTopology.Enum);

            for (int i = 0; i < _spawnHandlerCache.Count; i++)
            {
                Prefab<Spawnable>[] prefab;

                if (_spawnHandlerCache[i].ResourceList.Count == 0)
                {
                    prefab = Prefab.Load<Spawnable>("assets/bundled/prefabs/autospawn/" + _spawnHandlerCache[i].ResourcePath, GameManager.server, PrefabAttribute.server, false);
                }
                else
                {
                    prefab = Prefab.Load<Spawnable>(_spawnHandlerCache[i].ResourceList.ToArray(), GameManager.server, PrefabAttribute.server);
                }

                SpawnPopulation spawnPopulation = FileSystem.Load<SpawnPopulation>(_spawnHandlerCache[i].PopulationPath, true);

                TopologyCache tCache;
                if(_spawnFilters.TryGetValue(spawnPopulation.name, out tCache))
                {
                    bool anyM = false, allM = false, notM = false;

                    foreach (var topology in tCache.TopologyAny)
                    {
                        if (anyM == false)
                        {
                            anyM = true;
                            spawnPopulation.Filter.TopologyAny = (TerrainTopology.Enum)Enum.Parse(type, topology);
                        }
                        else
                        {
                            spawnPopulation.Filter.TopologyAny = spawnPopulation.Filter.TopologyAny | (TerrainTopology.Enum)Enum.Parse(type, topology);
                        }
                    }

                    foreach (var topology in tCache.TopologyAll) 
                    {
                        if (allM == false)
                        {
                            allM = true;
                            spawnPopulation.Filter.TopologyAll = (TerrainTopology.Enum)Enum.Parse(type, topology);
                        }
                        else
                        {
                            spawnPopulation.Filter.TopologyAll = spawnPopulation.Filter.TopologyAll | (TerrainTopology.Enum)Enum.Parse(type, topology);
                        }
                    }

                    foreach (var topology in tCache.TopologyNot)
                    {
                        if (notM == false)
                        {
                            notM = true;
                            spawnPopulation.Filter.TopologyNot = (TerrainTopology.Enum)Enum.Parse(type, topology);
                        }
                        else
                        {
                            spawnPopulation.Filter.TopologyNot = spawnPopulation.Filter.TopologyNot | (TerrainTopology.Enum)Enum.Parse(type, topology);
                        }
                    }
                }

                spawnPopulations.Add(spawnPopulation);
                for (int j = 0; j < prefab.Length; j++)
                {
                    prefab[j].Component.Population = spawnPopulation;
                }
            }
            _spawnHandler.AllSpawnPopulations = spawnPopulations.ToArray();
            _spawnHandler.UpdateDistributions();
            UpdatePopulation();
        }

        private void UpdatePopulation()
        {
            var spawnables = UnityEngine.Object.FindObjectsOfType<Spawnable>()?.Where(x => x.gameObject.activeInHierarchy && x.Population != null)?.GroupBy(x => x.Population)?.ToDictionary(x => x.Key, y => y.ToArray());
            
            entityCache.Clear();
            for (int i = 0; i < _spawnHandler.AllSpawnPopulations.Length; i++)
            {
                if (_spawnHandler.AllSpawnPopulations[i] == null)
                {
                    PrintError("Spawn Population NULL " + i);
                    continue;
                }

                if (_spawnHandler.SpawnDistributions[i] == null)
                {
                    PrintError("Spawn SpawnDistributions NULL " + i);
                    continue; 
                }

                var spawnPopulation = _spawnHandler.AllSpawnPopulations[i];
                var spawnDistribution = _spawnHandler.SpawnDistributions[i];

                spawnPopulation.ScaleWithLargeMaps = false;
                spawnPopulation.ScaleWithServerPopulation = false;
                spawnPopulation.ScaleWithSpawnFilter = false;
                spawnPopulation.EnforcePopulationLimits = false;

                if (_serverPopulation.ContainsKey(spawnPopulation.name))
                {
                    if (spawnPopulation is ConvarControlledSpawnPopulation)
                    {
                        var populationConvar = (spawnPopulation as ConvarControlledSpawnPopulation).PopulationConvar;
                        ConsoleSystem.Command command = ConsoleSystem.Index.Server.Find(populationConvar);
                        command?.Set(_serverPopulation[spawnPopulation.name] / _density);
                    }
                    else
                    {
                        spawnPopulation._targetDensity = _serverPopulation[spawnPopulation.name] / _density;
                    }
                }

                if (spawnables.ContainsKey(spawnPopulation) && isLoaded == false)
                {
                    PrepareSpawnable(spawnPopulation, spawnDistribution, spawnables[spawnPopulation]);
                }
            }

            if(isLoaded == false)
                ReduceSpawnable(false);
        }

        #region Prepare for destroy
        /// <summary>
        /// Prepare unnecessary Spawnable for destroy
        /// </summary>
        /// <param name="population"></param>
        /// <param name="distribution"></param>
        /// <param name="array"></param>
        private void PrepareSpawnable(SpawnPopulation population, SpawnDistribution distribution, Spawnable[] array)
        {
            int targetCount = _spawnHandler.GetTargetCount(population, distribution);
            if (array.Length > targetCount)
            {
                int num = array.Length - targetCount;
                foreach (Spawnable item in array.Take(num))
                {
                    BaseEntity baseEntity = GameObjectEx.ToBaseEntity(item.gameObject);
                    if (BaseEntityEx.IsValid(baseEntity))
                    {
                        if (baseEntity is BaseVehicle)
                            continue;

                        if (baseEntity is JunkPile)
                        {
                            (baseEntity as JunkPile).SinkAndDestroy();
                            InvokeHandler.CancelInvoke((baseEntity as JunkPile), (baseEntity as JunkPile).KillMe);
                        }

                        entityCache.Enqueue(baseEntity);
                        _spawnHandler.RemoveInstance(item);
                    }
                }
            }
        }

        /// <summary>
        /// Prepare all Spawnable for destroy
        /// </summary>
        private void PrepareSpawnable(int spawnValue = 1045)
        {
            var spawnables = UnityEngine.Object.FindObjectsOfType<Spawnable>()?.Where(x => x.gameObject.activeInHierarchy && x.Population != null)?.GroupBy(x => x.Population)?.ToDictionary(x => x.Key, y => y.ToArray());
            foreach (var a in spawnables)
            {
                foreach (var item in a.Value)
                {
                    BaseEntity baseEntity = GameObjectEx.ToBaseEntity(item.gameObject);
                    if (BaseEntityEx.IsValid(baseEntity))
                    {
                        if (baseEntity is BaseVehicle)
                           continue;

                        if (baseEntity is JunkPile)
                        {
                            (baseEntity as JunkPile).SinkAndDestroy();
                            InvokeHandler.CancelInvoke((baseEntity as JunkPile), (baseEntity as JunkPile).KillMe);
                        }

                        entityCache.Enqueue(baseEntity);
                        _spawnHandler.RemoveInstance(item);
                    }
                }
            }
        }
        #endregion

        #region Reduce Spawnable
        private void ReduceSpawnable(bool updateHandler = false)
        {
            int reduceCount = 10;
            if(entityCache.Count < reduceCount)
                reduceCount = entityCache.Count;

            if (entityCache.Count >= reduceCount && reduceCount != 0)
            {
                for (int i = 0; i < reduceCount; i++)
                {
                    var entity = entityCache.Dequeue();
                    if(BaseEntityEx.IsValid(entity))
                    {
                        entity.Kill();
                    }
                }

                NextTick(() => ReduceSpawnable(updateHandler));
                return;
            }

            if (updateHandler)
            {
                UpdateSpawnHandler();
            }
            else
            {
                _spawnHandler.FillPopulations();
            }
        }
        #endregion
    }
}
