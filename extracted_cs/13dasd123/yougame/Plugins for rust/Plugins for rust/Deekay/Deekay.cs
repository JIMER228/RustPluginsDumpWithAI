// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;
using UnityEngine;
using Facepunch;

namespace Oxide.Plugins
{
    [Info("Deekay", "k1lly0u", "0.1.98", ResourceId = 0)]
    class Deekay : RustPlugin
    {
        #region Fields
        [PluginReference]
        Plugin ZoneManager;

        static Deekay ins;
        private Hash<uint, DecayManager> dkEntities = new Hash<uint, DecayManager>();
        private bool isInitialized;
        #endregion

        #region Oxide Hooks        
        void OnServerInitialized()
        {
            ins = this;
            LoadVariables();
            InitializeConfigData();
        }
        void OnEntitySpawned(BaseNetworkable entity)
        {
            try
            {
                if (!isInitialized) return;
                string prefabName = entity.PrefabName;
                if (prefabName.Contains("autospawn") || prefabName.Contains("npc") || prefabName.Contains("static")) return;                
                if (entity is BaseCombatEntity)
                {
                    EntityDecay decayData = null;
                    if (entity is BuildingBlock)
                    {
                        if (configData.BuildingDecay.ContainsKey(prefabName))
                            configData.BuildingDecay[prefabName].TryGetValue((entity as BuildingBlock).grade, out decayData);
                    }
                    else
                    {
                        if (configData.EntityDecay.ContainsKey(prefabName))
                            configData.EntityDecay.TryGetValue(prefabName, out decayData);
                    }
                    if (decayData != null && decayData.IsEnabled)
                    {
                        if (!dkEntities.ContainsKey(entity.net.ID))
                            dkEntities.Add(entity.net.ID, new DecayManager(entity as BaseCombatEntity, decayData));                       
                    }
                }
            }
            catch { }         
        }
        void OnDoorOpened(Door door, BasePlayer player)
        {
            if (!configData._MonitorActivity || door == null) return;           
                DecayTouch(door.transform.position);
        }
        void OnStructureRepair(BaseCombatEntity entity, BasePlayer player)
        {
            if (!configData._MonitorActivity || entity == null) return;
                DecayTouch(entity.transform.position);
        }
        void OnStructureUpgrade(BaseCombatEntity entity, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (entity == null || entity.IsDestroyed || !entity.IsValid()) return;
           
            EntityDecay decayData = null;
            string prefabName = entity.PrefabName;
            if (configData.BuildingDecay.ContainsKey(prefabName))
            {
                if (configData.BuildingDecay[prefabName].TryGetValue(grade, out decayData))
                {
                    if (!decayData.IsEnabled)
                    {
                        if (dkEntities.ContainsKey(entity.net.ID))
                        {
                            dkEntities[entity.net.ID].decayTimer?.Destroy();
                            dkEntities.Remove(entity.net.ID);
                        }
                        return;
                    }
                    if (dkEntities.ContainsKey(entity.net.ID))
                    {
                        dkEntities[entity.net.ID].decayTimer?.Destroy();
                        dkEntities[entity.net.ID] = new DecayManager(entity, decayData);
                    }
                    else dkEntities.Add(entity.net.ID, new DecayManager(entity, decayData));
                }
            }
        }
        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null || entity?.net?.ID == null) return;
            if (dkEntities.ContainsKey(entity.net.ID))
            {
                dkEntities[entity.net.ID].decayTimer?.Destroy();
                dkEntities.Remove(entity.net.ID);
            }           
        }       
        void Unload()
        {
            foreach(var entity in dkEntities)
            {
                entity.Value.decayTimer?.Destroy();
            }
            dkEntities.Clear();
        }
        #endregion

        #region Functions        
        void InitializeConfigData()
        {
            bool hasChanged = false;
            foreach (var construction in PrefabAttribute.server.GetAll<Construction>())
            {
                if (construction?.deployable == null && !string.IsNullOrEmpty(construction.info.name.english))
                {
                    if (!configData.BuildingDecay.ContainsKey(construction.fullName))
                    {
                        configData.BuildingDecay.Add(construction.fullName, new Dictionary<BuildingGrade.Enum, EntityDecay>
                            {
                                { BuildingGrade.Enum.Metal, configData._DefaultDecay },
                                { BuildingGrade.Enum.Stone, configData._DefaultDecay },
                                { BuildingGrade.Enum.TopTier, configData._DefaultDecay },
                                { BuildingGrade.Enum.Twigs, configData._DefaultDecay },
                                { BuildingGrade.Enum.Wood, configData._DefaultDecay }
                            });
                        hasChanged = true;
                    }
                }
            }
           
            foreach (var item in ItemManager.GetItemDefinitions())
            {
                var deployable = item?.GetComponent<ItemModDeployable>();
                if (deployable == null) continue;
                if (!configData.EntityDecay.ContainsKey(deployable.entityPrefab.resourcePath))
                {
                    configData.EntityDecay.Add(deployable.entityPrefab.resourcePath, configData._DefaultDecay);
                    hasChanged = true;
                }
            }

            if (hasChanged)
                SaveConfig(configData);
            isInitialized = true;
            FindAllEntities();
        }
       
        void FindAllEntities()
        {
            PrintWarning("Finding all deekay-able entities");

            var entities = UnityEngine.Object.FindObjectsOfType<BaseCombatEntity>().Distinct();
            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    if (entity == null || entity.IsDestroyed || !entity.IsValid()) continue;
                    string prefabName = entity.PrefabName;
                    if (string.IsNullOrEmpty(prefabName)) continue;
                    EntityDecay decayData;
                    if (!configData.EntityDecay.TryGetValue(prefabName, out decayData))
                    {
                        if (!configData.BuildingDecay.ContainsKey(prefabName)) continue;
                        if (!configData.BuildingDecay[prefabName].TryGetValue((entity as BuildingBlock).grade, out decayData)) continue;                        
                    }
                    if (decayData != null && decayData.IsEnabled && !dkEntities.ContainsKey(entity.net.ID))
                        timer.In(Random.Range(0.1f, 10f), () =>
                        {
                            if (entity != null && !entity.IsDestroyed && entity.IsValid() && !dkEntities.ContainsKey(entity.net.ID))
                                dkEntities.Add(entity.net.ID, new DecayManager(entity, decayData));
                        });
                }
            }
            PrintWarning("Waiting for decay managers to initialize");
            timer.In(10f, ()=> PrintWarning($"Added decay manager to {dkEntities.Count} entities"));
        }  
        void DecayTouch(Vector3 position)
        {
            List<BaseCombatEntity> list = Pool.GetList<BaseCombatEntity>();
            Vis.Entities<BaseCombatEntity>(position, configData._ActivityRadius, list, 2097408, QueryTriggerInteraction.Collide);
            for (int i = 0; i < list.Count; i++)
            {
                BaseCombatEntity entity = list[i];
                if (entity == null || entity.IsDestroyed || !entity.IsValid()) continue;
                DecayManager manager;
                if (dkEntities.TryGetValue(entity.net.ID, out manager))                
                    manager.ResetDecay();  
            }
        }     
        #endregion

        #region Component  
        class DecayManager
        {
            private BaseCombatEntity entity;
            private EntityDecay decayData;
            private float detectionRange;
            private bool isInPrivs = false;
            public Timer decayTimer;

            public DecayManager() { }
            public DecayManager(BaseCombatEntity entity, EntityDecay decayData)
            {
                this.entity = entity;
                this.decayData = decayData;
                detectionRange = ins.configData._DetectionRange;
                BeginDecay();
            }
            public void BeginDecay()
            {
                float decayRate = IsInPrivilege(false) ? decayData.InsidePrivilege.DecayRate : decayData.OutsidePrivilege.DecayRate;
                if (decayRate == 0)
                    decayTimer = ins.timer.In(300, BeginDecay);
                else decayTimer = ins.timer.Repeat(decayRate, 0, () => DealDamage());
            }
            public bool DealDamage()
            {
                if (entity == null || !entity.IsValid())
                    return false;

                if (entity.IsDestroyed)
                {
                    decayTimer?.Destroy();
                    ins.dkEntities.Remove(entity.net.ID);
                    return false;
                }

                if (ins.ZoneManager)
                {
                    var success = ins.ZoneManager.Call("EntityHasFlag", entity, "nodecay");
                    if (success is bool && (bool)success)
                        return true;
                }               
                
                float decayAmount = IsInPrivilege(true) ? decayData.InsidePrivilege.DamageRate : decayData.OutsidePrivilege.DamageRate;
                if (decayAmount == 0) return false;
                float amount = entity.MaxHealth() * decayAmount;
                if (entity.health <= amount)
                {
                    decayTimer?.Destroy();
                    ins.dkEntities.Remove(entity.net.ID);
                    entity.Die();
                    return true;                   
                }
                else entity.Hurt(amount, Rust.DamageType.Decay);
                return true;
            } 
            public bool IsInPrivilege(bool reset)
            {
                bool foundTC = false;
                var colliders = Pool.GetList<Collider>();
                Vis.Colliders(entity.transform.position + new Vector3(0, entity.bounds.max.y, 0), detectionRange, colliders, LayerMask.GetMask("Trigger"));
                foreach (var collider in colliders)
                {
                    if (collider.gameObject != null && collider.gameObject.GetComponentInParent<BuildingPrivlidge>() && collider.gameObject.name == "areaTrigger" && collider.gameObject.layer == 18) 
                        foundTC = true;  
                }
                Pool.FreeList(ref colliders);
                  
                if (foundTC && !isInPrivs)
                {
                    isInPrivs = true;
                    if (reset)
                        ResetDecay();
                }
                else if (!foundTC && isInPrivs)
                {
                    isInPrivs = false;
                    if (reset)
                        ResetDecay();
                }
                return foundTC;
            }
            public void ResetDecay()
            {
                decayTimer?.Destroy();
                BeginDecay();
            }
        }
        #endregion

        #region Commands
        [ConsoleCommand("dk.reset")]
        void ccmdDKReset(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            configData.BuildingDecay.Clear();
            configData.EntityDecay.Clear();
            InitializeConfigData();
            SendReply(arg, "The config has been reset");
        }
        [ConsoleCommand("dk.rundecay")]
        void ccmdDKRun(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) return;
            int decayCount = 0;

            ICollection<uint> keyCollection = dkEntities.Keys;
            uint[] keys = new uint[keyCollection.Count];
            keyCollection.CopyTo(keys, 0);

            for (int i = 0; i < keys.Length; i++)
            {
                var obj = dkEntities[keys[i]];
                if (obj.DealDamage())
                {
                    obj.ResetDecay();
                    ++decayCount;
                }
            }       
            SendReply(arg, $"{decayCount} entities have been dealt decay damage!");
        }
        #endregion

        #region Config        
        private ConfigData configData;
        class EntityDecay
        {
            public Rates InsidePrivilege { get; set; }
            public Rates OutsidePrivilege { get; set; }
            public bool IsEnabled { get; set; }
        } 
        class Rates
        {
            public float DamageRate { get; set; }
            public float DecayRate { get; set; }
        }
       
        class ConfigData
        {           
            public bool _MonitorActivity { get; set; }
            public float _ActivityRadius { get; set; }
            public float _DetectionRange { get; set; }
            public EntityDecay _DefaultDecay { get; set; }
            public Dictionary<string, EntityDecay> EntityDecay { get; set; }
            public Dictionary<string, Dictionary<BuildingGrade.Enum, EntityDecay>> BuildingDecay { get; set; }
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
                _ActivityRadius = 40f,
                _DetectionRange = 0.25f,
                _DefaultDecay = new EntityDecay
                {
                    InsidePrivilege = new Rates
                    {
                        DamageRate = 0.05f,
                        DecayRate = 3600
                    },
                    OutsidePrivilege = new Rates
                    {                        
                        DamageRate = 0.2f,
                        DecayRate = 3600                    
                    },
                    IsEnabled = true
                },
                _MonitorActivity = true,
                BuildingDecay = new Dictionary<string, Dictionary<BuildingGrade.Enum, EntityDecay>>(),
                EntityDecay = new Dictionary<string, EntityDecay>()                    
            };
            SaveConfig(config);
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();
        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion               
    }
}
