#define DEBUG
using BaseAIBrain;
using BaseEntity;
using Facepunch;
using JetBrains.Annotations;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Ext.CustomNpc.Gameplay.AI;
using Oxide.Ext.CustomNpc.Gameplay.AI.States;
using Oxide.Ext.CustomNpc.Gameplay.Components;
using Oxide.Ext.CustomNpc.Gameplay.Configurations;
using Oxide.Ext.CustomNpc.Gameplay.Controllers;
using Oxide.Ext.CustomNpc.Gameplay.Controllers.NpcAttire;
using Oxide.Ext.CustomNpc.Gameplay.Data;
using Oxide.Ext.CustomNpc.Gameplay.Entities;
using Oxide.Ext.CustomNpc.Gameplay.Managers;
using Oxide.Ext.CustomNpc.Gameplay.NpcCreator;
using Oxide.Ext.CustomNpc.PluginExntesions;
using Rust;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;


//CustomNpcPlugin created with PluginMerge v(1.0.4.0) by MJSU @ https://github.com/dassjosh/Plugin.Merge
namespace Oxide.Plugins
{
    [Info("CustomNpcPlugin", "Flutes", "0.1.0")]
    [Description("test")]
    public partial class CustomNpcPlugin : RustPlugin
    {
        #region CustomNpcPlugin.cs
        private void Init()
        {
            new PluginsExtensionsManager();
            CustomNpc_Manager.Setup();
            
            Unsubscribe("OnInventoryNetworkUpdate");
        }
        
        private void Unload()
        {
            CustomNpc_Manager.DestroyAllNpcs();
        }
        
        private void SpawnTest(BasePlayer player)
        {
            CustomNpc_Configuration npcConfig = new CustomNpc_Configuration()
            {
                DamageScale = 1,
                DisableRadio = true,
                AgentTypeID = -1372625422,
                MemoryDuration = 10.0f,
                AreaMask = 1,
                AttackRangeMultiplier = 1.0f,
                WearItems = new List<CustomNpc_WearItem> { new CustomNpc_WearItem { ShortName = "attire.egg.suit", SkinId = 0 } },
                BeltItems = new List<CustomNpc_BeltItem>
                {
                    new CustomNpc_BeltItem { ShortName = "rifle.lr300", Amount = 1, SkinId = 0, Mods = new List<string> { "weapon.mod.holosight", "weapon.mod.flashlight" } },
                    new CustomNpc_BeltItem { ShortName = "syringe.medical", Amount = 10, SkinId = 0, Mods = new List<string>() },
                    new CustomNpc_BeltItem { ShortName = "grenade.f1", Amount = 10, SkinId = 0, Mods = new List<string>() },
                    new CustomNpc_BeltItem { ShortName = "grenade.smoke", Amount = 10, SkinId = 0, Mods = new List<string>() },
                    new CustomNpc_BeltItem { ShortName = "explosive.timed", Amount = 10, SkinId = 0, Mods = new List<string>() },
                    new CustomNpc_BeltItem { ShortName = "rocket.launcher", Amount = 1, SkinId = 0, Mods = new List<string>() }
                },
                CanTargetOtherNpc = false,
                ChaseRange = 100.0f,
                CheckVisionCone = false,
                ListenRange = 10.0f,
                MaxHealth = 200.0f,
                StartHealth = 200.0f,
                RoamRange = 10f,
                Name = "Test",
                SenseRange = 50f,
                Speed = 7.5f,
                VisionCone = 15.0f,
                States = new List<string>() { "DefaultIdleState", "DefaultRoamState", "DefaultChaseState", "DefaultCombatState", "DefaultHealState" }
            };
            
            NpcInstantiationFactory.InstanceNpcDefault(player.ServerPosition, npcConfig);
        }
        
        #region Oxide Hooks
        private void OnEntityKill(CustomNpc_Component customNpc)
        {
            CustomNpc_Manager.OnNpcDestroyed(customNpc);
            
            if (NpcCreator_Manager.IsStarted)
            {
                NpcCreator_Manager.OnEntityKill(customNpc);
            }
            //NpcCreator_ManagerFactory.OnEntityKill(customNpc);
        }
        
        object OnInventoryNetworkUpdate(PlayerInventory inventory, ItemContainer container, ProtoBuf.UpdateItemContainer updateItemContainer, PlayerInventory.Type type, bool broadcast)
        {
            if (inventory.baseEntity == null)
            return null;
            
            if (NpcCreator_Manager.Controllers.ContainsKey(inventory.baseEntity.userID) == false)
            return null;
            
            var controller = NpcCreator_Manager.Controllers[inventory.baseEntity.userID];
            controller.CopyWearToSelectedNpc();
            
            return null;
        }
        #endregion
        
        [ChatCommand("customnpc_spawn")]
        private void CustomNpcSpawnChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
            return;
            
            SpawnTest(player);
        }
        
        [ChatCommand("customnpc_creator_start")]
        private void CustomNpcCreatorStartChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
            return;
            
            if (args.Length == 0)
            return;
            
            string pluginName = args[0];
            var plugin = Interface.Oxide.RootPluginManager.GetPlugin(pluginName);
            
            if (plugin == null)
            {
                player.ChatMessage($"Plugin {pluginName} not found");
                return;
            }
            
            if (NpcCreator_Manager.IsStarted == false)
            Subscribe("OnInventoryNetworkUpdate");
            
            NpcCreator_Manager.StartCreator(player, plugin);
            player.ChatMessage($"Success");
        }
        
        
        [ChatCommand("customnpc_creator_stop")]
        private void CustomNpcCreatorStopChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
            return;
            
            NpcCreator_Manager.StopCreator(player);
            
            if (NpcCreator_Manager.IsStarted == false)
            Unsubscribe("OnInventoryNetworkUpdate");
        }
        
        [ChatCommand("customnpc_creator_stop_all")]
        private void CustomNpcCreatorStopAllChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
            return;
            
            NpcCreator_Manager.Stop();
            
            Unsubscribe("OnInventoryNetworkUpdate");
            
            player.ChatMessage($"Success");
        }
        
        [ChatCommand("customnpc_creator_edit")]
        private void CustomNpcCreatorEditChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
            return;
            
            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
            return;
            
            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.InstanceNpcCommand(args);
            
            player.ChatMessage($"Success");
        }
        
        [ChatCommand("customnpc_creator_select")]
        private void CustomNpcCreatorSelectChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
            return;
            
            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
            return;
            
            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.SelectNpcCommand();
            
            player.ChatMessage($"Success");
        }
        
        [ChatCommand("customnpc_creator_unselect")]
        private void CustomNpcCreatorUnselectChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
            return;
            
            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
            return;
            
            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.UnselectNpcCommand();
            
            player.ChatMessage($"Success");
        }
        
        [ChatCommand("customnpc_creator_save_all")]
        private void CustomNpcCreatorSaveAllChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
            return;
            
            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
            return;
            
            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.SaveAllNpcConfig();
            
            player.ChatMessage($"Success");
        }
        
        [ChatCommand("customnpc_creator_save_selected")]
        private void CustomNpcCreatorSaveSelectedChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
            return;
            
            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
            return;
            
            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.SaveSelectNpcConfig();
            
            player.ChatMessage($"Success");
        }
        
        [ChatCommand("customnpc_creator_reload_all")]
        private void CustomNpcCreatoReloadAllChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
            return;
            
            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
            return;
            
            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.ReloadAllNpcConfig();
            
            player.ChatMessage($"Success");
        }
        
        [ChatCommand("customnpc_creator_reload_selected")]
        private void CustomNpcCreatoReloadSelectedChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
            return;
            
            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
            return;
            
            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.ReloadSelectedNpcConfig();
            
            player.ChatMessage($"Success");
        }
        
        [ChatCommand("customnpc_creator_test_selected")]
        private void CustomNpcCreatorTestSelectedChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
            return;
            
            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
            return;
            
            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.TestSelect();
            
            player.ChatMessage($"Success");
        }
        
        [ChatCommand("customnpc_creator_test_all")]
        private void CustomNpcCreatorTestAllChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
            return;
            
            
            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
            return;
            
            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.TestAll();
            
            player.ChatMessage($"Success");
        }
        
        [ChatCommand("customnpc_creator_test_stop")]
        private void CustomNpcCreatorTestEndChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsAdmin == false)
            return;
            
            if (NpcCreator_Manager.Controllers.ContainsKey(player.userID) == false)
            return;
            
            var controller = NpcCreator_Manager.Controllers[player.userID];
            controller.StopTest();
            
            player.ChatMessage($"Success");
        }
        #endregion

        #region Configuration.cs
        public class Configuration
        {
            private static Configuration m_current;
            public static Configuration Current => m_current != null ? m_current : Load();
            
            public static Configuration Load()
            {
                if (ConfigurationFileExist() == false)
                {
                    return Create();
                }
                
                // else Load
                
                return null;
            }
            
            private static bool ConfigurationFileExist()
            {
                return false;
            }
            
            private static Configuration Create()
            {
                return null;
            }
        }
        #endregion

        #region PluginExntesions\PluginsExtensionsManager.cs
        public class PluginsExtensionsManager : IDisposable
        {
            public static PluginsExtensionsManager Instance { get; private set; }
            
            public IReadOnlyDictionary<string, Plugin> LoadedPlugins => m_loadedPlugins;
            
            private Dictionary<string, Plugin> m_loadedPlugins = new Dictionary<string, Plugin>();
            private readonly List<string> m_neededPlugins = new List<string>()
            {
                "Kits"
            };
            
            
            public PluginsExtensionsManager()
            {
                if (Instance != null)
                return;
                
                Instance = this;
                
                RegisterPlugins();
                RegisterEvents();
            }
            
            private void RegisterEvents()
            {
                Interface.Oxide.RootPluginManager.OnPluginAdded += OnPluginAdded;
                Interface.Oxide.RootPluginManager.OnPluginAdded -= OnPluginRemoved;
            }
            
            private void UnregisterEvents()
            {
                Interface.Oxide.RootPluginManager.OnPluginAdded -= OnPluginAdded;
                Interface.Oxide.RootPluginManager.OnPluginAdded -= OnPluginRemoved;
            }
            
            private void RegisterPlugins()
            {
                foreach (var pluginName in m_neededPlugins)
                {
                    var plugin = Interface.Oxide.RootPluginManager.GetPlugin(pluginName);
                    if (plugin == null)
                    continue;
                    
                    m_loadedPlugins.Add(pluginName, plugin);
                }
            }
            
            private void OnPluginAdded(Plugin plugin)
            {
                if (m_neededPlugins.Contains(plugin.Name) == false)
                return;
                
                if (m_loadedPlugins.ContainsKey(plugin.Name))
                return;
                
                m_loadedPlugins.Add(plugin.Name, plugin);
            }
            
            private void OnPluginRemoved(Plugin plugin)
            {
                if (m_neededPlugins.Contains(plugin.Name) == false)
                return;
                
                if (m_loadedPlugins.ContainsKey(plugin.Name) == false)
                return;
                
                m_loadedPlugins.Remove(plugin.Name);
            }
            
            private bool m_isDisposed;
            public void Dispose()
            {
                if (m_isDisposed)
                {
                    Interface.Oxide.LogWarning($"attempt to dispose already disposed {nameof(PluginsExtensionsManager)} ");
                    return;
                }
                
                UnregisterEvents();
                
                m_isDisposed = true;
            }
        }
        #endregion

        #region Gameplay\AI\CustomNpc_Pathfinding.cs
        public class CustomNpc_Pathfinding
        {
            private CustomNpc_Controller m_controller;
            private CustomNpc_Component m_component => m_controller.Component;
            public CustomNpc_Pathfinding(CustomNpc_Controller controller)
            {
                m_controller = controller;
            }
            
            public bool IsEqualsVector3(Vector3 a, Vector3 b) => Vector3.Distance(a, b) < 0.1f;
            
            public void SetDestination(Vector3 pos, float radius, BaseNavigator.NavigationSpeed speed)
            {
                Vector3 sample = GetSamplePosition(pos, radius);
                sample.y += 2f;
                if (!IsEqualsVector3(sample, m_controller.Brain.Component.Navigator.Destination)) m_controller.Brain.Component.Navigator.SetDestination(sample, speed);
            }
            
            public Vector3 GetSamplePosition(Vector3 source, float radius)
            {
                NavMeshHit navMeshHit;
                if (NavMesh.SamplePosition(source, out navMeshHit, radius, m_component.NavAgent.areaMask))
                {
                    NavMeshPath path = new NavMeshPath();
                    if (NavMesh.CalculatePath(m_controller.GameObject.transform.position, navMeshHit.position, m_component.NavAgent.areaMask, path))
                    {
                        if (path.status == NavMeshPathStatus.PathComplete) return navMeshHit.position;
                        else return path.corners.Last();
                    }
                }
                return source;
            }
            
            public Vector3 GetRandomPositionAround(Vector3 source, float radius)
            {
                Vector2 vector2 = UnityEngine.Random.insideUnitCircle * radius;
                return source + new Vector3(vector2.x, 0f, vector2.y);
            }
        }
        #endregion

        #region Gameplay\Components\CustomNpcBrain_Component.cs
        public class CustomNpcBrain_Component : ScientistBrain
        {
            public Action onAddStates;
            public Action onInitializeAI;
            public Action<float> onThink;
            
            protected CustomNpcBrain_Controller m_controller;
            
            public virtual void Setup(CustomNpcBrain_Controller controller)
            {
                m_controller = controller;
            }
            
            public override void AddStates()
            {
                onAddStates?.Invoke();
            }
            
            public override void InitializeAI()
            {
                onInitializeAI?.Invoke();
            }
            
            public override void Think(float delta)
            {
                onThink?.Invoke(delta);
            }
            
            public void SetThinkRate(float rate)
            {
                thinkRate = rate;
            }
            
            public void SetLastThinkTime(float time)
            {
                lastThinkTime = time;
            }
        }
        #endregion

        #region Gameplay\Components\CustomNpc_Component.cs
        public class CustomNpc_Component : ScientistNPC
        {
            public Action onServerInstantiated;
            public Action onDestroy;
            
            public bool IsEquipingWeapon { get; private set; }
            public bool IsReloadGrenadeLauncher { get; private set; }
            public bool IsReloadFlameThrower { get; private set; }
            public bool IsHealing { get; private set; }
            
            public BaseEntity CurrentTarget { get; private set; }
            public AttackEntity CurrentWeapon { get; private set; }
            
            public Vector3 HomePosition { get; private set; }
            
            public void Setup(CustomNpcBrain_Component brainComponent, Vector3 homePosition)
            {
                HomePosition = homePosition;
                skinID = 11185464824609;
                Brain = brainComponent;
                enableSaving = false;
                gameObject.AwakeFromInstantiate();
            }
            
            private void OnDestroy()
            {
                CancelInvoke();
                onDestroy?.Invoke();
            }
            
            protected override string OverrideCorpseName() => displayName;
            
            public override void ServerInit()
            {
                base.ServerInit();
                
                onServerInstantiated?.Invoke();
            }
            
            public void OnEquipingWeapon(AttackEntity weapon)
            {
                CurrentWeapon = weapon;
                IsEquipingWeapon = true;
            }
            
            public void OnDesequipingWeapon()
            {
                CurrentWeapon = null;
            }
            
            public void OnFinishEquipingWeapon()
            {
                IsEquipingWeapon = false;
            }
            
            
            public void SetTarget(BaseEntity target)
            {
                CurrentTarget = target;
            }
            
            public void OnHeal()
            {
                IsHealing = true;
            }
            
            public void OnFinishHeal()
            {
                IsHealing = false;
            }
        }
        #endregion

        #region Gameplay\Configurations\CustomNpc_BeltItem.cs
        public class CustomNpc_BeltItem
        {
            public string ShortName;
            public ulong SkinId;
            public int Amount;
            public List<string> Mods = new List<string>();
            public string Ammo;
        }
        #endregion

        #region Gameplay\Configurations\CustomNpc_Configuration.cs
        public class CustomNpc_Configuration
        {
            public string Name;
            
            public int AreaMask;
            public int AgentTypeID;
            
            public float StartHealth;
            public float MaxHealth;
            
            public float DamageScale;
            public float AttackRangeMultiplier;
            
            public float Speed;
            public float MemoryDuration;
            public float SenseRange;
            public bool CheckVisionCone;
            public float ListenRange;
            public float VisionCone;
            
            public bool CanRunAwayWater;
            
            public float ChaseRange;
            public float RoamRange;
            
            public List<string> States = new List<string>();
            
            public bool DisableRadio;
            
            public string Kit;
            public List<CustomNpc_BeltItem> BeltItems = new List<CustomNpc_BeltItem>();
            public List<CustomNpc_WearItem> WearItems = new List<CustomNpc_WearItem>();
            
            public bool CanTargetOtherNpc;
            
            public static CustomNpc_Configuration Default()
            {
                CustomNpc_Configuration npcConfig = new CustomNpc_Configuration()
                {
                    DamageScale = 1,
                    DisableRadio = true,
                    AgentTypeID = -1372625422,
                    MemoryDuration = 10.0f,
                    AreaMask = 1,
                    AttackRangeMultiplier = 1.0f,
                    WearItems = new List<CustomNpc_WearItem> { new CustomNpc_WearItem { ShortName = "attire.egg.suit", SkinId = 0 } },
                    BeltItems = new List<CustomNpc_BeltItem>
                    {
                        new CustomNpc_BeltItem { ShortName = "rifle.lr300", Amount = 1, SkinId = 0, Mods = new List<string> { "weapon.mod.holosight", "weapon.mod.flashlight" } },
                        new CustomNpc_BeltItem { ShortName = "syringe.medical", Amount = 10, SkinId = 0, Mods = new List<string>() },
                        new CustomNpc_BeltItem { ShortName = "grenade.f1", Amount = 10, SkinId = 0, Mods = new List<string>() },
                        new CustomNpc_BeltItem { ShortName = "grenade.smoke", Amount = 10, SkinId = 0, Mods = new List<string>() },
                        new CustomNpc_BeltItem { ShortName = "explosive.timed", Amount = 10, SkinId = 0, Mods = new List<string>() },
                        new CustomNpc_BeltItem { ShortName = "rocket.launcher", Amount = 1, SkinId = 0, Mods = new List<string>() }
                    },
                    CanTargetOtherNpc = false,
                    ChaseRange = 100.0f,
                    CheckVisionCone = false,
                    ListenRange = 10.0f,
                    MaxHealth = 200.0f,
                    StartHealth = 200.0f,
                    RoamRange = 10f,
                    Name = "Test",
                    SenseRange = 50f,
                    Speed = 7.5f,
                    VisionCone = 15.0f,
                    States = new List<string>() { "DefaultIdleState", "DefaultRoamState", "DefaultChaseState", "DefaultCombatState", "ChangeWeaponState" }
                };
                
                return npcConfig;
            }
        }
        #endregion

        #region Gameplay\Configurations\CustomNpc_WearItem.cs
        public class CustomNpc_WearItem
        {
            public string ShortName;
            public ulong SkinId;
        }
        #endregion

        #region Gameplay\Controllers\CustomNpcBrain_Controller.cs
        public class CustomNpcBrain_Controller
        {
            public CustomNpcBrain_Component Component { get; private set; }
            private CustomNpc_Controller m_npc;
            public CustomNpc_Controller Npc => m_npc;
            
            private bool m_isInit;
            public virtual void Initialize(CustomNpc_Controller npc, CustomNpcBrain_Component component)
            {
                Component = component;
                m_npc = npc;
                
                component.Setup(this);
                
                Component.onAddStates += AddStates;
                Component.onInitializeAI += InitializeAI;
                Component.onThink += Think;
                
                m_isInit = true;
            }
            
            protected virtual void AddStates()
            {
                if (m_isInit == false)
                return;
                
                Component.states = new Dictionary<AIState, BasicAIState>();
                
                foreach (var stateName in m_npc.Configuration.States)
                {
                    CustomNpc_Manager.IStatesFactory states = null;
                    if (CustomNpc_Manager.States.TryGetValue(stateName, out states) == false)
                    continue;
                    
                    var state = states.Get();
                    state.Setup(m_npc);
                    Component.AddState(state);
                }
                
                if (Component.states.Count == 0)
                {
                    Interface.Oxide.LogWarning("Not state added for this npc");
                }
            }
            
            protected virtual void InitializeAI()
            {
                if (m_isInit == false)
                {
                    Interface.Oxide.LogError("[CustomNpc] InitializeAI brain without initialization !!");
                    return;
                }
                
                m_npc.Component.HasBrain = true;
                Component.Navigator = m_npc.GameObject.GetComponent<BaseNavigator>();
                Component.Navigator.Speed = m_npc.Configuration.Speed;
                Component.InvokeRandomized(Component.DoMovementTick, 1f, 0.1f, 0.01f);
                
                Component.AttackRangeMultiplier = m_npc.Configuration.AttackRangeMultiplier;
                Component.MemoryDuration = m_npc.Configuration.MemoryDuration;
                Component.SenseRange = Math.Abs(m_npc.Configuration.SenseRange);
                Component.TargetLostRange = Component.SenseRange * 2f;
                Component.VisionCone = Vector3.Dot(Vector3.forward, Quaternion.Euler(0f, m_npc.Configuration.VisionCone, 0f) * Vector3.forward);
                Component.CheckVisionCone = m_npc.Configuration.CheckVisionCone;
                Component.CheckLOS = true;
                Component.IgnoreNonVisionSneakers = true;
                Component.MaxGroupSize = 0;
                Component.ListenRange = m_npc.Configuration.ListenRange;
                Component.HostileTargetsOnly = m_npc.Configuration.SenseRange < 0f;
                Component.IgnoreSafeZonePlayers = !Component.HostileTargetsOnly;
                Component.SenseTypes = EntityType.Player;
                Component.RefreshKnownLOS = false;
                Component.IgnoreNonVisionMaxDistance = Component.ListenRange / 3f;
                Component.IgnoreSneakersMaxDistance = Component.IgnoreNonVisionMaxDistance / 3f;
                Component.Senses.Init(
                m_npc.Component, Component, Component.MemoryDuration,
                Component.SenseRange, Component.TargetLostRange,
                Component.VisionCone, Component.CheckVisionCone,
                Component.CheckLOS, Component.IgnoreNonVisionSneakers,
                Component.ListenRange, Component.HostileTargetsOnly, false,
                Component.IgnoreSafeZonePlayers, Component.SenseTypes, Component.RefreshKnownLOS);
                
                Component.ThinkMode = AIThinkMode.Interval;
                Component.SetThinkRate(0.5f);
                Component.PathFinder = new HumanPathFinder();
                ((HumanPathFinder)Component.PathFinder).Init(m_npc.Component);
            }
            
            protected virtual void Think(float delta)
            {
                if (m_isInit == false)
                return;
                
                if (m_npc == null)
                return;
                
                if (Component == null)
                return;
                
                if (Component.Senses == null)
                return;
                
                if (Component.states == null)
                return;
                
                
                Component.SetLastThinkTime(Time.time);
                if (Component.sleeping) return;
                
                Component.Senses.Update();
                m_npc.SetBestTarget();
                
                
                if (Component.CurrentState != null)
                {
                    var thinkResult = Component.CurrentState.StateThink(delta, Component, m_npc.Component);
                    switch (thinkResult)
                    {
                        case StateStatus.Finished:
                        case StateStatus.Error:
                        Component.CurrentState.StateLeave(Component, m_npc.Component);
                        Component.CurrentState = null;
                        break;
                        case StateStatus.Running:
                        if (Component.CurrentState.CanLeave() == false)
                        {
                            return;
                        }
                        break;
                    }
                    
                }
                
                float num = 0f;
                BasicAIState newState = null;
                foreach (BasicAIState value in Component.states.Values)
                {
                    if (value == null || value.CanEnter() == false) continue;
                    float weight = value.GetWeight();
                    if (weight < num) continue;
                    num = weight;
                    newState = value;
                }
                if (newState != Component.CurrentState)
                {
                    Component.CurrentState?.StateLeave(Component, m_npc.Component);
                    Component.CurrentState = newState;
                    Component.CurrentState?.StateEnter(Component, m_npc.Component);
                }
            }
            
        }
        #endregion

        #region Gameplay\Controllers\CustomNpc_Controller.cs
        public class CustomNpc_Controller
        {
            public GameObject GameObject => Component.gameObject;
            public  CustomNpc_Component Component { get; private set; }
            public CustomNpcBrain_Controller Brain { get; private set; }
            public CustomNpc_Pathfinding Pathfinding { get; private set; }
            public CustomNpc_Configuration Configuration { get; private set; }
            
            public float DistanceToTarget => Vector3.Distance(GameObject.transform.position, Component.CurrentTarget.transform.position);
            public float DistanceFromHome => Vector3.Distance(GameObject.transform.position, Component.HomePosition);
            public bool IsBehindBarricade() => Component.CanSeeTarget(Component.CurrentTarget) && HasNearbyBarricade();
            
            private Coroutine m_healCoroutine;
            
            private bool m_isInit;
            
            #region Setup
            public virtual void Initialize(CustomNpc_Component component, CustomNpc_Configuration configuration)
            {
                Component = component;
                Configuration = configuration;
                Pathfinding = new CustomNpc_Pathfinding(this);
                
                m_isInit = true;
            }
            
            public virtual void Start(CustomNpcBrain_Controller brain)
            {
                if (m_isInit == false)
                {
                    Interface.Oxide.LogError("[CustomNpc] Start npc without initialization !!");
                    return;
                }
                
                Brain = brain;
                Component.Setup(brain.Component, GameObject.transform.position);
                
                RegisterEvents();
                
                Component.Spawn();
            }
            
            private void RegisterEvents()
            {
                Component.onServerInstantiated += OnSpawn;
                Component.onDestroy += OnDestroy;
            }
            
            protected virtual void OnSpawn()
            {
                if (m_isInit == false)
                return;
                
                Setup();
            }
            
            protected virtual void OnDestroy()
            {
                if (m_isInit == false)
                return;
                
                if (m_healCoroutine != null) ServerMgr.Instance.StopCoroutine(m_healCoroutine);
                
                if (Brain.Component.CurrentState != null)
                {
                    Brain.Component.CurrentState.StateLeave(Brain.Component, Component);
                }
                
                Component.onServerInstantiated -= OnSpawn;
                Component.onDestroy -= OnDestroy;
            }
            
            protected virtual void Setup()
            {
                if (m_isInit == false)
                return;
                
                Component.displayName = Configuration.Name;
                
                SetupNavAgent();
                SetupHealth();
                SetupDamages();
                SetupSound();
                SetupInventory();
                
                Component.InvokeRepeating(Component.LightCheck, 1f, 30f); // Todo : Config
                Component.InvokeRepeating(Update, 1f, 2f); // Todo : Config
            }
            
            protected virtual void SetupNavAgent()
            {
                if (Component.NavAgent == null)
                Component.NavAgent = GameObject.GetComponent<NavMeshAgent>();
                
                if (Component.NavAgent != null)
                {
                    Component.NavAgent.areaMask = Configuration.AreaMask;
                    Component.NavAgent.agentTypeID = Configuration.AgentTypeID;
                }
            }
            
            protected virtual void SetupHealth()
            {
                Component.startHealth = Configuration.StartHealth;
                Component._maxHealth = Configuration.MaxHealth;
                Component._health = Configuration.StartHealth;
            }
            
            protected virtual void SetupDamages()
            {
                Component.damageScale = Configuration.DamageScale;
            }
            
            protected virtual void SetupSound()
            {
                if (Configuration.DisableRadio)
                {
                    Component.CancelInvoke(Component.PlayRadioChatter);
                    Component.RadioChatterEffects = Array.Empty<GameObjectRef>();
                    Component.DeathEffects = Array.Empty<GameObjectRef>();
                }
            }
            
            protected virtual void SetupInventory()
            {
                ClearItems(Component.inventory.containerWear);
                ClearItems(Component.inventory.containerBelt);
                
                NpcAttire_Controller_Base attire = null;
                
                if (string.IsNullOrEmpty(Configuration.Kit) == false
                && PluginsExtensionsManager.Instance.LoadedPlugins.ContainsKey(NpcAttire_Kits_Controller.PLUGIN_NAME))
                {
                    attire = new NpcAttire_Kits_Controller(this, Configuration.Kit);
                }
                else
                {
                    attire = new NpcAttire_Default_Controller(Component.inventory, Configuration.BeltItems, Configuration.WearItems);
                }
                
                attire.Equip();
            }
            #endregion
            
            private void ClearItems(ItemContainer container)
            {
                for (int i = container.itemList.Count - 1; i >= 0; i--)
                {
                    Item item = container.itemList[i];
                    item.RemoveFromContainer();
                    item.Remove();
                }
            }
            
            protected virtual void Update()
            {
                if (m_isInit == false)
                return;
            }
            
            
            protected virtual void SetAimTowardsTarget()
            {
                Vector3 aimDirection = (Component.CurrentTarget.transform.position - GameObject.transform.position).normalized;
                Component.SetAimDirection(aimDirection);
            }
            
            private IEnumerable<Barricade> GetBarricadeHits(RaycastHit[] hits)
            {
                foreach (var hit in hits)
                {
                    var barricade = hit.GetEntity() as Barricade;
                    if (barricade != null && CoversData.Barricades.Contains(barricade.ShortPrefabName))
                    {
                        yield return barricade;
                    }
                }
            }
            
            private bool HasNearbyBarricade()
            {
                SetAimTowardsTarget();
                
                RaycastHit[] hits = UnityEngine.Physics.RaycastAll(Component.eyes.HeadRay());
                GamePhysics.Sort(hits);
                
                return GetBarricadeHits(hits).Any(barricade => Vector3.Distance(GameObject.transform.position, barricade.transform.position) < DistanceToTarget);
            }
            
            #region Weapons
            
            #region Grenade Launcher
            public bool IsReloadGrenadeLauncher { get; private set; } = false;
            protected int m_availableAmmoInGrenadeLauncher = 6;
            
            public virtual void FireGrenadeLauncher()
            {
                RaycastHit raycastHit;
                Component.SignalBroadcast(Signal.Attack, string.Empty);
                Vector3 vector3 = Component.IsMounted() ? Component.eyes.position + new Vector3(0f, 0.5f, 0f) : Component.eyes.position;
                Vector3 modifiedAimConeDirection = AimConeUtil.GetModifiedAimConeDirection(0.675f, Component.eyes.BodyForward());
                float distance = 1f;
                if (UnityEngine.Physics.Raycast(vector3, modifiedAimConeDirection, out raycastHit, distance, 1236478737))
                distance = raycastHit.distance - 0.1f;
                
                TimedExplosive grenade = GameManager.server.CreateEntity("assets/prefabs/ammo/40mmgrenade/40mm_grenade_he.prefab", vector3 + modifiedAimConeDirection * distance) as TimedExplosive;
                grenade.creatorEntity = Component;
                ServerProjectile serverProjectile = grenade.GetComponent<ServerProjectile>();
                serverProjectile.InitializeVelocity(Component.GetInheritedProjectileVelocity(modifiedAimConeDirection) + modifiedAimConeDirection * serverProjectile.speed * 2f);
                grenade.Spawn();
                m_availableAmmoInGrenadeLauncher--;
                if (m_availableAmmoInGrenadeLauncher == 0)
                {
                    IsReloadGrenadeLauncher = true;
                    Component.Invoke(FinishReloadGrenadeLauncher, 8f);
                }
            }
            
            protected void FinishReloadGrenadeLauncher()
            {
                m_availableAmmoInGrenadeLauncher = 6;
                IsReloadGrenadeLauncher = false;
            }
            #endregion Multiple Grenade Launcher
            
            #region Flame Thrower
            public bool IsReloadFlameThrower { get; private set; }
            
            public virtual void FireFlameThrower()
            {
                FlameThrower flameThrower = Component.CurrentWeapon as FlameThrower;
                if (flameThrower == null || flameThrower.IsFlameOn()) return;
                if (flameThrower.ammo <= 0)
                {
                    IsReloadFlameThrower = true;
                    Component.Invoke(FinishReloadFlameThrower, 4f);
                    return;
                }
                flameThrower.SetFlameState(true);
                Component.Invoke(flameThrower.StopFlameState, 0.25f);
            }
            
            protected void FinishReloadFlameThrower()
            {
                FlameThrower flameThrower = Component.CurrentWeapon as FlameThrower;
                if (flameThrower == null) return;
                flameThrower.TopUpAmmo();
                IsReloadFlameThrower = false;
            }
            #endregion Flame Thrower
            
            #region Melee Weapon
            public void UseMeleeWeapon()
            {
                BaseMelee weapon = Component.CurrentWeapon as BaseMelee;
                if (weapon.HasAttackCooldown())
                return;
                
                weapon.StartAttackCooldown(weapon.repeatDelay * 2f);
                Component.SignalBroadcast(Signal.Attack, string.Empty, null);
                
                if (weapon.swingEffect.isValid)
                Effect.server.Run(weapon.swingEffect.resourcePath, weapon.transform.position, Vector3.forward, Component.net.connection, false);
                
                Vector3 vector31 = Component.eyes.BodyForward();
                
                for (int i = 0; i < 2; i++)
                {
                    List<RaycastHit> list = Pool.GetList<RaycastHit>();
                    GamePhysics.TraceAll(new Ray(Component.eyes.position - (vector31 * (i == 0 ? 0f : 0.2f)), vector31), (i == 0 ? 0f : weapon.attackRadius), list, weapon.effectiveRange + 0.2f, 1219701521, QueryTriggerInteraction.UseGlobal, null);
                    bool flag = false;
                    for (int j = 0; j < list.Count; j++)
                    {
                        RaycastHit item = list[j];
                        BaseEntity entity = item.GetEntity();
                        if (entity != null && (entity == null || entity != Component && !entity.EqualNetID(Component)) && (entity == null || !entity.isClient))
                        {
                            float single = weapon.damageTypes.Sum(x => x.amount);
                            entity.OnAttacked(new HitInfo(Component, entity, DamageType.Slash, single * weapon.npcDamageScale * Configuration.DamageScale));
                            HitInfo hitInfo = Pool.Get<HitInfo>();
                            hitInfo.HitEntity = entity;
                            hitInfo.HitPositionWorld = item.point;
                            hitInfo.HitNormalWorld = -vector31;
                            if (entity is BaseNpc || entity is BasePlayer) hitInfo.HitMaterial = StringPool.Get("Flesh");
                            else hitInfo.HitMaterial = StringPool.Get(item.GetCollider().sharedMaterial != null ? item.GetCollider().sharedMaterial.GetName() : "generic");
                            weapon.ServerUse_OnHit(hitInfo);
                            Effect.server.ImpactEffect(hitInfo);
                            Pool.Free(ref hitInfo);
                            flag = true;
                            if (entity == null || entity.ShouldBlockProjectiles()) break;
                        }
                    }
                    Pool.FreeList(ref list);
                    if (flag) break;
                }
            }
            #endregion Melee Weapon
            #endregion
            
            
            #region Targeting
            public virtual void SetBestTarget()
            {
                BaseEntity bestTarget = null;
                float highestScore = -1f;
                
                foreach (BaseEntity potentialTarget in Brain.Component.Senses.Players)
                {
                    if (!CanTargetEntity(potentialTarget)) continue;
                    
                    float targetScore = CalculateTargetScore(potentialTarget);
                    if (targetScore > highestScore)
                    {
                        bestTarget = potentialTarget;
                        highestScore = targetScore;
                    }
                }
                
                Component.SetTarget(bestTarget);
            }
            
            protected virtual float CalculateTargetScore(BaseEntity entity)
            {
                float distanceScore = 1f - Mathf.InverseLerp(1f, Brain.Component.SenseRange, Vector3.Distance(entity.transform.position, GameObject.transform.position));
                float visionScore = Mathf.InverseLerp(Brain.Component.VisionCone, 1f, Vector3.Dot((entity.transform.position - Component.eyes.position).normalized, Component.eyes.BodyForward())) / 2f;
                float losBonus = Brain.Component.Senses.Memory.IsLOS(entity) ? 2f : 0f;
                
                return distanceScore + visionScore + losBonus;
            }
            
            protected virtual bool CanTargetEntity(BaseEntity target)
            {
                if (target == null || target.IsDestroyed || target.Health() <= 0f) return false;
                
                BasePlayer player = target as BasePlayer;
                if (player != null)
                {
                    return CanTargetPlayer(player);
                }
                else
                {
                    Drone drone = target as Drone;
                    if (drone != null)
                    {
                        return CanTargetDrone(drone);
                    }
                }
                
                return false;
            }
            
            private bool CanTargetPlayer(BasePlayer player)
            {
                if (player.IsDead()) return false;
                if (player.skinID != 0 && NpcSkinsData.PlayerSkinIDs.Contains(player.skinID)) return true;
                if (player.userID.IsSteamId()) return !player.IsSleeping() && !player.IsWounded() && !player._limitedNetworking;
                NPCPlayer npcPlayer = player as NPCPlayer;
                if (npcPlayer != null) return CanTargetNpcPlayer(npcPlayer);
                return false;
            }
            
            protected virtual bool CanTargetNpcPlayer(NPCPlayer npcPlayer)
            {
                return npcPlayer is FrankensteinPet || Configuration.CanTargetOtherNpc;
            }
            
            protected virtual bool CanTargetDrone(Drone drone)
            {
                return !(Component.CurrentWeapon is BaseMelee);
            }
            
            #endregion
            
            
            public void Heal()
            {
                m_healCoroutine = ServerMgr.Instance.StartCoroutine(HealCoroutine());
            }
            
            private IEnumerator HealCoroutine()
            {
                Component.OnHeal();
                Item syringe = Component.inventory.containerBelt.itemList.FirstOrDefault(x => x.info.shortname == "syringe.medical");
                Component.OnDesequipingWeapon();
                Component.UpdateActiveItem(syringe.uid);
                MedicalTool medicalTool = syringe.GetHeldEntity() as MedicalTool;
                yield return CoroutineEx.waitForSeconds(1.5f);
                
                if (medicalTool != null) medicalTool.ServerUse();
                Component.InitializeHealth(
                Component.health + 15f > Configuration.MaxHealth
                ? Configuration.MaxHealth
                : Component.health + 15f, Configuration.MaxHealth);
                
                yield return CoroutineEx.waitForSeconds(3f);
                
                Component.OnFinishHeal();
            }
        }
        #endregion

        #region Gameplay\Data\CoversData.cs
        public static class CoversData
        {
            public static readonly HashSet<string> Barricades = new HashSet<string>
            {
                "barricade.cover.wood",
                "barricade.sandbags",
                "barricade.concrete",
                "barricade.stone"
            };
        }
        #endregion

        #region Gameplay\Data\NpcSkinsData.cs
        public static class NpcSkinsData
        {
            public static HashSet<ulong> PlayerSkinIDs = new HashSet<ulong>
            {
                14922524,
                19395142091920
            };
        }
        #endregion

        #region Gameplay\Data\WeaponsData.cs
        public static class WeaponsData
        {
            public class Range { public float EffectiveRange; public float AttackLengthMin; public float AttackLengthMax; }
            
            public static readonly Dictionary<string, Range> WeaponsRange = new Dictionary<string, Range>
            {
                ["snowballgun"] = new Range { EffectiveRange = 5f, AttackLengthMin = 2f, AttackLengthMax = 2f },
                ["shotgun.double"] = new Range { EffectiveRange = 10f, AttackLengthMin = 0.3f, AttackLengthMax = 1f },
                ["pistol.eoka"] = new Range { EffectiveRange = 10f, AttackLengthMin = -1f, AttackLengthMax = -1f },
                ["shotgun.waterpipe"] = new Range { EffectiveRange = 10f, AttackLengthMin = -1f, AttackLengthMax = -1f },
                ["speargun"] = new Range { EffectiveRange = 15f, AttackLengthMin = -1f, AttackLengthMax = -1f },
                ["bow.compound"] = new Range { EffectiveRange = 15f, AttackLengthMin = -1f, AttackLengthMax = -1f },
                ["crossbow"] = new Range { EffectiveRange = 15f, AttackLengthMin = -1f, AttackLengthMax = -1f },
                ["bow.hunting"] = new Range { EffectiveRange = 15f, AttackLengthMin = -1f, AttackLengthMax = -1f },
                ["pistol.nailgun"] = new Range { EffectiveRange = 15f, AttackLengthMin = 0f, AttackLengthMax = 0.46f },
                ["pistol.python"] = new Range { EffectiveRange = 15f, AttackLengthMin = 0.175f, AttackLengthMax = 0.525f },
                ["pistol.semiauto"] = new Range { EffectiveRange = 15f, AttackLengthMin = 0f, AttackLengthMax = 0.46f },
                ["pistol.prototype17"] = new Range { EffectiveRange = 15f, AttackLengthMin = 0f, AttackLengthMax = 0.46f },
                ["multiplegrenadelauncher"] = new Range { EffectiveRange = 20f, AttackLengthMin = -1f, AttackLengthMax = -1f },
                ["smg.2"] = new Range { EffectiveRange = 20f, AttackLengthMin = 0.4f, AttackLengthMax = 0.4f },
                ["smg.thompson"] = new Range { EffectiveRange = 20f, AttackLengthMin = 0.4f, AttackLengthMax = 0.4f },
                ["rifle.bolt"] = new Range { EffectiveRange = 150f, AttackLengthMin = -1f, AttackLengthMax = -1f },
                ["rifle.l96"] = new Range { EffectiveRange = 150f, AttackLengthMin = -1f, AttackLengthMax = -1f }
            };
            
            public static readonly HashSet<string> MeleeWeapons = new HashSet<string>
            {
                "bone.club",
                "knife.bone",
                "knife.butcher",
                "candycaneclub",
                "knife.combat",
                "longsword",
                "mace",
                "machete",
                "paddle",
                "pitchfork",
                "salvaged.cleaver",
                "salvaged.sword",
                "spear.stone",
                "spear.wooden",
                "chainsaw",
                "hatchet",
                "jackhammer",
                "pickaxe",
                "axe.salvaged",
                "hammer.salvaged",
                "icepick.salvaged",
                "stonehatchet",
                "stone.pickaxe",
                "torch",
                "sickle",
                "rock",
                "snowball",
                "mace.baseballbat"
            };
            
            public static readonly HashSet<string> FirstDistanceWeapons = new HashSet<string>
            {
                "speargun",
                "bow.compound",
                "crossbow",
                "bow.hunting",
                "shotgun.double",
                "pistol.eoka",
                "flamethrower",
                "pistol.m92",
                "pistol.nailgun",
                "multiplegrenadelauncher",
                "shotgun.pump",
                "pistol.python",
                "pistol.revolver",
                "pistol.semiauto",
                "pistol.prototype17",
                "snowballgun",
                "shotgun.spas12",
                "shotgun.waterpipe"
            };
            
            public static readonly HashSet<string> SecondDistanceWeapons = new HashSet<string>
            {
                "smg.2",
                "smg.mp5",
                "rifle.semiauto",
                "smg.thompson"
            };
            
            public static readonly HashSet<string> ThirdDistanceWeapons = new HashSet<string>
            {
                "rifle.ak",
                "rifle.lr300",
                "lmg.m249",
                "rifle.m39",
                "hmlmg",
                "rifle.ak.ice"
            };
            
            public static readonly HashSet<string> FourthDistanceWeapons = new HashSet<string>
            {
                "rifle.bolt",
                "rifle.l96"
            };
        }
        #endregion

        #region Gameplay\Entities\CustomNpc_Entity.cs
        public class CustomNpc_Entity
        {
            public readonly GameObject GameObject;
            public readonly CustomNpc_Controller Controller;
            
            public CustomNpc_Entity(GameObject gameObject, CustomNpc_Controller controller)
            {
                GameObject = gameObject;
                Controller = controller;
            }
            
            public void Start(CustomNpcBrain_Controller brain)
            {
                Controller.Start(brain);
            }
        }
        #endregion

        #region Gameplay\Managers\CustomNpc_Manager.cs
        public static class CustomNpc_Manager
        {
            private static Dictionary<ulong, CustomNpc_Entity> m_spawnedNpcs = new Dictionary<ulong, CustomNpc_Entity>();
            public static IReadOnlyDictionary<ulong, CustomNpc_Entity> SpawnedNpcs => m_spawnedNpcs;
            
            private static Dictionary<CustomNpc_Component, CustomNpc_Entity> m_spawnedNpcsByComponent = new Dictionary<CustomNpc_Component, CustomNpc_Entity>();
            public static IReadOnlyDictionary<CustomNpc_Component, CustomNpc_Entity> SpawnedNpcsByComponent => m_spawnedNpcsByComponent;
            
            private static Dictionary<string, IStatesFactory> m_states = new Dictionary<string, IStatesFactory>();
            public static IReadOnlyDictionary<string, IStatesFactory> States => m_states;
            
            public static void Setup()
            {
                RegisterDefaultStates();
            }
            
            #region Instantiation
            
            public static CustomNpc_Entity CreateAndStartEntity(CustomNpc_Component component, CustomNpc_Controller npc, CustomNpcBrain_Controller brain)
            {
                CustomNpc_Entity entity = new CustomNpc_Entity(component.gameObject, npc);
                entity.Start(brain);
                m_spawnedNpcs.Add(entity.Controller.Component.net.ID.Value, entity);
                m_spawnedNpcsByComponent.Add(component, entity);
                return entity;
            }
            #endregion
            
            public static void DestroyAllNpcs()
            {
                foreach(var npc in m_spawnedNpcs)
                {
                    m_spawnedNpcsByComponent.Remove(npc.Value.Controller.Component);
                    npc.Value.Controller.Component.Kill();
                }
                
                m_spawnedNpcs.Clear();
            }
            
            public static void OnNpcDestroyed(CustomNpc_Component component)
            {
                if (component == null)
                return;
                
                CustomNpc_Entity entity = null;
                if (m_spawnedNpcsByComponent.TryGetValue(component, out entity) == false)
                return;
                
                m_spawnedNpcs.Remove(entity.Controller.Component.net.ID.Value);
                m_spawnedNpcsByComponent.Remove(component);
            }
            
            #region States
            public interface IStatesFactory
            {
                CustomAIState Get();
            }
            
            public class StatesFactory<T> : IStatesFactory where T : CustomAIState, new()
            {
                public CustomAIState Get()
                {
                    return new T();
                }
                
            }
            
            public static void RegisterState<T>() where T : CustomAIState, new()
            {
                string name = typeof(T).Name;
                
                if (m_states.ContainsKey(name))
                return;
                
                m_states.Add(name, new StatesFactory<T>());
            }
            
            public static void UnregisterState(Type type)
            {
                string name = type.Name;
                
                if (m_states.ContainsKey(name) == false)
                return;
                
                m_states.Remove(name);
            }
            
            private static void RegisterDefaultStates()
            {
                RegisterState<DefaultRoamState>();
                RegisterState<DefaultChaseState>();
                RegisterState<DefaultCombatState>();
                RegisterState<DefaultIdleState>();
                RegisterState<DefaultCombatStationaryState>();
                RegisterState<DefaultHealState>();
                RegisterState<DefaultHealState>();
                RegisterState<ChangeWeaponState>();
            }
            #endregion
            
            
        }
        #endregion

        #region Gameplay\Managers\NpcInstantiationFactory.cs
        internal class NpcInstantiationFactory
        {
            // Tous les types par défaut
            public static CustomNpc_Entity InstanceNpcDefault(Vector3 position, CustomNpc_Configuration configuration)
            {
                return InstanceNpc<CustomNpc_Controller, CustomNpcBrain_Controller, CustomNpc_Component, CustomNpcBrain_Component>(position, configuration);
            }
            
            // NpcController custom
            public static CustomNpc_Entity InstanceNpcWithCustomController<TnpcController>(Vector3 position, CustomNpc_Configuration configuration)
            where TnpcController : CustomNpc_Controller, new()
            {
                return InstanceNpc<TnpcController, CustomNpcBrain_Controller, CustomNpc_Component, CustomNpcBrain_Component>(position, configuration);
            }
            
            // BrainController custom
            public static CustomNpc_Entity InstanceNpcWithCustomBrainController<TBrainController>(Vector3 position, CustomNpc_Configuration configuration)
            where TBrainController : CustomNpcBrain_Controller, new()
            {
                return InstanceNpc<CustomNpc_Controller, TBrainController, CustomNpc_Component, CustomNpcBrain_Component>(position, configuration);
            }
            
            // NpcComponent custom
            public static CustomNpc_Entity InstanceNpcWithCustomComponent<TNpcComponent>(Vector3 position, CustomNpc_Configuration configuration)
            where TNpcComponent : CustomNpc_Component
            {
                return InstanceNpc<CustomNpc_Controller, CustomNpcBrain_Controller, TNpcComponent, CustomNpcBrain_Component>(position, configuration);
            }
            
            // BrainComponent custom
            public static CustomNpc_Entity InstanceNpcWithCustomBrainComponent<TBrainComponent>(Vector3 position, CustomNpc_Configuration configuration)
            where TBrainComponent : CustomNpcBrain_Component
            {
                return InstanceNpc<CustomNpc_Controller, CustomNpcBrain_Controller, CustomNpc_Component, TBrainComponent>(position, configuration);
            }
            
            // NpcController et BrainController custom
            public static CustomNpc_Entity InstanceNpcWithCustomControllers<TnpcController, TBrainController>(Vector3 position, CustomNpc_Configuration configuration)
            where TnpcController : CustomNpc_Controller, new()
            where TBrainController : CustomNpcBrain_Controller, new()
            {
                return InstanceNpc<TnpcController, TBrainController, CustomNpc_Component, CustomNpcBrain_Component>(position, configuration);
            }
            
            // NpcController et NpcComponent custom
            public static CustomNpc_Entity InstanceNpcWithCustomControllerAndComponent<TnpcController, TNpcComponent>(Vector3 position, CustomNpc_Configuration configuration)
            where TnpcController : CustomNpc_Controller, new()
            where TNpcComponent : CustomNpc_Component
            {
                return InstanceNpc<TnpcController, CustomNpcBrain_Controller, TNpcComponent, CustomNpcBrain_Component>(position, configuration);
            }
            
            // NpcController et BrainComponent custom
            public static CustomNpc_Entity InstanceNpcWithCustomControllerAndBrainComponent<TnpcController, TBrainComponent>(Vector3 position, CustomNpc_Configuration configuration)
            where TnpcController : CustomNpc_Controller, new()
            where TBrainComponent : CustomNpcBrain_Component
            {
                return InstanceNpc<TnpcController, CustomNpcBrain_Controller, CustomNpc_Component, TBrainComponent>(position, configuration);
            }
            
            // BrainController et NpcComponent custom
            public static CustomNpc_Entity InstanceNpcWithCustomBrainControllerAndComponent<TBrainController, TNpcComponent>(Vector3 position, CustomNpc_Configuration configuration)
            where TBrainController : CustomNpcBrain_Controller, new()
            where TNpcComponent : CustomNpc_Component
            {
                return InstanceNpc<CustomNpc_Controller, TBrainController, TNpcComponent, CustomNpcBrain_Component>(position, configuration);
            }
            
            // BrainController et BrainComponent custom
            public static CustomNpc_Entity InstanceNpcWithCustomBrainControllerAndBrainComponent<TBrainController, TBrainComponent>(Vector3 position, CustomNpc_Configuration configuration)
            where TBrainController : CustomNpcBrain_Controller, new()
            where TBrainComponent : CustomNpcBrain_Component
            {
                return InstanceNpc<CustomNpc_Controller, TBrainController, CustomNpc_Component, TBrainComponent>(position, configuration);
            }
            
            // NpcController, BrainController et NpcComponent custom
            public static CustomNpc_Entity InstanceNpcWithCustomControllerBrainControllerAndComponent<TnpcController, TBrainController, TNpcComponent>(Vector3 position, CustomNpc_Configuration configuration)
            where TnpcController : CustomNpc_Controller, new()
            where TBrainController : CustomNpcBrain_Controller, new()
            where TNpcComponent : CustomNpc_Component
            {
                return InstanceNpc<TnpcController, TBrainController, TNpcComponent, CustomNpcBrain_Component>(position, configuration);
            }
            
            // NpcController, BrainController et BrainComponent custom
            public static CustomNpc_Entity InstanceNpcWithCustomControllerBrainControllerAndBrainComponent<TnpcController, TBrainController, TBrainComponent>(Vector3 position, CustomNpc_Configuration configuration)
            where TnpcController : CustomNpc_Controller, new()
            where TBrainController : CustomNpcBrain_Controller, new()
            where TBrainComponent : CustomNpcBrain_Component
            {
                return InstanceNpc<TnpcController, TBrainController, CustomNpc_Component, TBrainComponent>(position, configuration);
            }
            
            // NpcController, NpcComponent et BrainComponent custom
            public static CustomNpc_Entity InstanceNpcWithCustomControllerComponentAndBrainComponent<TnpcController, TNpcComponent, TBrainComponent>(Vector3 position, CustomNpc_Configuration configuration)
            where TnpcController : CustomNpc_Controller, new()
            where TNpcComponent : CustomNpc_Component
            where TBrainComponent : CustomNpcBrain_Component
            {
                return InstanceNpc<TnpcController, CustomNpcBrain_Controller, TNpcComponent, TBrainComponent>(position, configuration);
            }
            
            // BrainController, NpcComponent et BrainComponent custom
            public static CustomNpc_Entity InstanceNpcWithCustomBrainControllerComponentAndBrainComponent<TBrainController, TNpcComponent, TBrainComponent>(Vector3 position, CustomNpc_Configuration configuration)
            where TBrainController : CustomNpcBrain_Controller, new()
            where TNpcComponent : CustomNpc_Component
            where TBrainComponent : CustomNpcBrain_Component
            {
                return InstanceNpc<CustomNpc_Controller, TBrainController, TNpcComponent, TBrainComponent>(position, configuration);
            }
            
            private static CustomNpc_Entity InstanceNpc<TnpcController, TBrainController, TNpcComponent, TBrainCoponent>(Vector3 position, CustomNpc_Configuration configuration)
            where TnpcController : CustomNpc_Controller, new()
            where TBrainController : CustomNpcBrain_Controller, new()
            where TNpcComponent : CustomNpc_Component
            where TBrainCoponent : CustomNpcBrain_Component
            {
                LogInstanceNpc(position);
                
                ScientistNPC scientistNpc = CreateScientistNpc(position);
                if (scientistNpc == null) return null;
                
                ScientistBrain scientistBrain = GetScientistBrain(scientistNpc);
                if (scientistBrain == null) return null;
                
                TNpcComponent customNpcComponent = AddComponentToGameObject<TNpcComponent>(scientistNpc.gameObject);
                TBrainCoponent customBrainComponent = AddComponentToGameObject<TBrainCoponent>(scientistNpc.gameObject);
                
                CopySerializableFields(scientistNpc, customNpcComponent);
                CopySerializableFields(scientistBrain, customBrainComponent);
                
                DestroyObject(scientistNpc);
                DestroyObject(scientistBrain);
                
                TnpcController customNpc = InitializeNpc<TnpcController>(customNpcComponent, configuration);
                TBrainController brain = InitializeBrain<TBrainController>(customNpc, customBrainComponent);
                
                return CustomNpc_Manager.CreateAndStartEntity(customNpcComponent, customNpc, brain);
            }
            
            private static void LogInstanceNpc(Vector3 position)
            {
                Interface.Oxide.LogInfo($"[CustomNpc] InstanceNpc at {position.ToString()} position ...");
            }
            
            private static void CopySerializableFields<T>(T src, T dst)
            {
                FieldInfo[] srcFields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo field in srcFields)
                {
                    object value = field.GetValue(src);
                    field.SetValue(dst, value);
                }
            }
            
            private static ScientistNPC CreateScientistNpc(Vector3 position)
            {
                ScientistNPC scientistNpc = GameManager.server.CreateEntity("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_heavy.prefab", position, Quaternion.identity, false) as ScientistNPC;
                if (scientistNpc == null)
                {
                    Interface.Oxide.LogInfo("[CustomNpc] Scientist npc is null");
                }
                return scientistNpc;
            }
            
            private static ScientistBrain GetScientistBrain(ScientistNPC scientistNpc)
            {
                ScientistBrain scientistBrain = scientistNpc.GetComponent<ScientistBrain>();
                if (scientistBrain == null)
                {
                    Interface.Oxide.LogInfo("[CustomNpc] Scientist npc brain is null");
                }
                return scientistBrain;
            }
            
            private static T AddComponentToGameObject<T>(GameObject gameObject) where T : Component
            {
                return gameObject.AddComponent<T>();
            }
            
            private static void DestroyObject(UnityEngine.Object obj)
            {
                UnityEngine.Object.DestroyImmediate(obj, true);
            }
            
            private static TController InitializeNpc<TController>(CustomNpc_Component component, CustomNpc_Configuration configuration) where TController : CustomNpc_Controller, new()
            {
                TController controller = new TController();
                controller.Initialize(component, configuration);
                return controller;
            }
            
            private static TController InitializeBrain<TController>(CustomNpc_Controller npc, CustomNpcBrain_Component component) where TController : CustomNpcBrain_Controller, new()
            {
                TController controller = new TController();
                controller.Initialize(npc, component);
                return controller;
            }
        }
        #endregion

        #region Gameplay\NpcCreator\NpcCreator_BrainComponent.cs
        public class NpcCreator_BrainComponent : CustomNpcBrain_Component
        {
            private Vector3 m_beforeTestPosition;
            
            public override void AddStates()
            {
                states = new Dictionary<AIState, BasicAIState>();
                DefaultIdleState idleState = new DefaultIdleState();
                idleState.Setup(m_controller.Npc);
            }
            
            public void StartTest()
            {
                m_beforeTestPosition = baseEntity.ServerPosition;
                base.AddStates();
            }
            
            public void StopTest()
            {
                CurrentState?.StateLeave(this, baseEntity);
                AddStates();
                
                m_controller.Npc.Pathfinding.SetDestination(m_beforeTestPosition, 2f, BaseNavigator.NavigationSpeed.Fast);
            }
        }
        #endregion

        #region Gameplay\NpcCreator\NpcCreator_Controller.cs
        public class NpcCreator_Controller
        {
            public const float DISTANCE_SELECT_MAX = 15.0f;
            
            private Dictionary<ulong, NpcCreator_NpcController> m_npcs = new Dictionary<ulong, NpcCreator_NpcController>();
            
            public NpcCreator_NpcController SelectedNpc { get; private set; }
            public readonly BasePlayer BasePlayer;
            public Action<NpcCreator_NpcController> onAddNpc;
            public Action<string> onRemoveNpc;
            
            private bool m_isTest;
            private bool m_isSelectedTest;
            
            private bool m_onStopping;
            
            public NpcCreator_Controller(BasePlayer player)
            {
                BasePlayer = player;
            }
            
            public void Stop()
            {
                m_onStopping = true;
                
                for (int i = 0; i <  m_npcs.Count; i++)
                {
                    KillNpc(m_npcs.ElementAt(i).Value);
                    i--;
                }
                
                m_onStopping = false;
            }
            
            public void KillNpc(NpcCreator_NpcController npc)
            {
                npc.Npc.Controller.Component.Kill();
            }
            
            public bool InstanceNpc(string name, Vector3 position, out NpcCreator_NpcController npc, bool select = false)
            {
                npc = null;
                
                if (NpcCreator_Manager.EditesNpcs.ContainsKey(name))
                {
                    return false;
                }
                
                CustomNpc_Entity entity = null;
                CustomNpc_Configuration config = null;
                
                if (m_npcs.Values != null)
                {
                    var alreadyExist = m_npcs.Values.FirstOrDefault(x => x.Name == name);
                    if (alreadyExist != null)
                    {
                        return false;
                    }
                }
                
                if (NpcCreator_Manager.NpcConfigurations.ContainsKey(name))
                {
                    config = NpcCreator_Manager.NpcConfigurations[name];
                    Interface.Oxide.LogInfo($"[CustomNpc] Instance npc {name} with configuration");
                }
                else
                {
                    config = CustomNpc_Configuration.Default();
                    config.Name = name;
                }
                
                entity = NpcInstantiationFactory.InstanceNpcWithCustomBrainComponent<NpcCreator_BrainComponent>(position, config);
                NpcCreator_NpcController npcCreator = new NpcCreator_NpcController(BasePlayer, name, entity);
                m_npcs.Add(entity.Controller.Component.net.ID.Value, npcCreator);
                
                if (select)
                {
                    SelectNpc(npcCreator);
                }
                
                npcCreator.onKill += OnNpcKilled;
                
                npc = npcCreator;
                onAddNpc?.Invoke(npcCreator);
                return true;
            }
            
            private void OnNpcKilled(NpcCreator_NpcController npcController)
            {
                if (npcController == null)
                return;
                
                bool selected = false;
                
                if (SelectedNpc == npcController)
                {
                    selected = true;
                }
                
                m_npcs.Remove(npcController.Npc.Controller.Component.net.ID.Value);
                OnRemoveNpc(npcController);
                
                if (m_onStopping == false)
                {
                    NpcCreator_NpcController newNpc = null;
                    
                    if (InstanceNpc(npcController.Name, npcController.StartPosition, out newNpc, selected))
                    {
                        if ((selected && m_isSelectedTest) || m_isTest)
                        {
                            newNpc.Npc.Controller.Component.Invoke(newNpc.CreatorBrain.StartTest, 2.0f);
                        }
                    }
                    else
                    {
                        Interface.Oxide.LogInfo("[CustomNpc] Fail to spawn npc");
                    }
                }
            }
            
            private void OnRemoveNpc(NpcCreator_NpcController npcController)
            {
                npcController.onKill -= OnNpcKilled;
                
                onRemoveNpc?.Invoke(npcController.Name);
            }
            
            public void InstanceNpcCommand(string[] args)
            {
                if (m_isTest)
                return;
                
                if (args.Length != 1)
                {
                    BasePlayer.ChatMessage("Need 1 arg (name)");
                    return;
                }
                
                string name = args[0];
                NpcCreator_NpcController newNpc = null;
                if (InstanceNpc(name, BasePlayer.ServerPosition, out newNpc, true) == false)
                {
                    BasePlayer.ChatMessage($"npc {name} already instantiated for edition");
                    return;
                }
            }
            
            //public bool RemoveNpcCommand()
            //{
                //    if (m_isTest)
                //        return false;
                
                //    if (SelectedNpc == null)
                //    {
                    //        BasePlayer.ChatMessage("Not npc selected");
                    //        return false;
                //    }
                
                //    SelectedNpc.Npc.Controller.Component.Kill();
                //    onRemoveNpc?.Invoke(SelectedNpc.Name);
                
                //    return true;
            //}
            
            public void SelectNpcCommand()
            {
                if (m_isTest)
                return;
                
                NpcCreator_NpcController npc = null;
                if (TrySelectNpc(out npc))
                {
                    SelectNpc(npc);
                    BasePlayer.ChatMessage($"Npc {npc.Npc.Controller.Component.displayName} selected");
                }
                else
                {
                    BasePlayer.ChatMessage("Not npc founded");
                }
            }
            
            public void UnselectNpcCommand()
            {
                if (m_isTest)
                return;
                
                UnSelectNpc();
            }
            
            public void CopyWearToSelectedNpc()
            {
                if (m_isTest)
                return;
                
                if (SelectedNpc == null) return;
                
                SelectedNpc.MovePlayerWearItemsToNpc(BasePlayer.inventory);
                SelectedNpc.MovePlayerBeltItemsToNpc(BasePlayer.inventory);
            }
            
            public bool TrySelectNpc(out NpcCreator_NpcController selectedNpc)
            {
                selectedNpc = null;
                var ray = BasePlayer.eyes.HeadRay();
                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit, DISTANCE_SELECT_MAX, (1 << (int)Layer.AI) | (1 << (int)Layer.Player_Server)))
                {
                    if (hit.collider == null)
                    return false;
                    
                    if (hit.collider.gameObject == null)
                    return false;
                    
                    var component = hit.collider.gameObject.GetComponent<CustomNpc_Component>();
                    if (component == null)
                    return false;
                    
                    if (m_npcs.ContainsKey(component.net.ID.Value) == false)
                    return false;
                    
                    selectedNpc = m_npcs[component.net.ID.Value];
                    return true;
                }
                
                return false;
            }
            
            private void SelectNpc(NpcCreator_NpcController npc)
            {
                if (SelectedNpc != null)
                SelectedNpc.OnUnselect();
                
                SelectedNpc = npc;
                npc.MoveNpcWearItemsToPlayer(BasePlayer.inventory);
                npc.MoveNpcBeltItemsToPlayer(BasePlayer.inventory);
                
                SelectedNpc.OnSelect();
            }
            
            private void UnSelectNpc()
            {
                SelectedNpc = null;
            }
            
            public void SaveAllNpcConfig()
            {
                foreach (var npc in m_npcs.Values)
                {
                    SaveNpcConfig(npc);
                }
            }
            
            public void SaveSelectNpcConfig()
            {
                SaveNpcConfig(SelectedNpc);
            }
            
            public void SaveNpcConfig(NpcCreator_NpcController npc)
            {
                var config = npc.Npc.Controller.Configuration;
                var fileName = NpcCreator_Manager.NPC_FILE_BASE + config.Name;
                
                SaveWearConfiguration(npc);
                SaveBeltConfiguration(npc);
                
                Interface.Oxide.DataFileSystem.WriteObject($"{NpcCreator_Manager.Plugin.Name}/{fileName}", config);
            }
            
            private void SaveWearConfiguration(NpcCreator_NpcController npc)
            {
                var config = npc.Npc.Controller.Configuration;
                config.WearItems = new List<CustomNpc_WearItem>();
                
                foreach (var item in npc.Npc.Controller.Component.inventory.containerWear.itemList)
                {
                    config.WearItems.Add(new CustomNpc_WearItem() { ShortName = item.info.shortname, SkinId = item.skin });
                }
            }
            
            private void SaveBeltConfiguration(NpcCreator_NpcController npc)
            {
                var config = npc.Npc.Controller.Configuration;
                config.BeltItems = new List<CustomNpc_BeltItem>();
                
                foreach (var item in npc.Npc.Controller.Component.inventory.containerBelt.itemList)
                {
                    var beltItem = new CustomNpc_BeltItem() { ShortName = item.info.shortname, SkinId = item.skin, Amount = item.amount };
                    config.BeltItems.Add(beltItem);
                    
                    var heldentity = item.GetHeldEntity();
                    if (heldentity == null)
                    continue;
                    
                    BaseProjectile baseProjectile = heldentity as BaseProjectile;
                    if (baseProjectile == null)
                    continue;
                    
                    var mods = item.contents;
                    if (mods != null && mods.itemList != null)
                    {
                        foreach(var mod in mods.itemList)
                        {
                            beltItem.Mods.Add(mod.info.shortname);
                        }
                    }
                    
                    if (baseProjectile.primaryMagazine != null)
                    {
                        var ammo = baseProjectile.primaryMagazine.ammoType.shortname;
                        beltItem.Ammo = ammo;
                    }
                }
            }
            
            public void ReloaddNpcConfig(NpcCreator_NpcController npc, bool select = false)
            {
                if (SelectedNpc == null)
                return;
                
                var config = npc.Npc.Controller.Configuration;
                
                NpcCreator_Manager.ReloadConfig(config.Name);
                
                ulong id = npc.Npc.Controller.Component.net.ID.Value;
                Vector3 position = npc.Npc.Controller.Component.ServerPosition;
                m_npcs.Remove(id);
                KillNpc(npc);
                NpcCreator_NpcController newNpc = null;
                InstanceNpc(config.Name, position, out newNpc, select);
            }
            
            public void ReloadSelectedNpcConfig()
            {
                ReloaddNpcConfig(SelectedNpc, true);
            }
            
            public void ReloadAllNpcConfig()
            {
                for (int i = 0; i < m_npcs.Count; i++)
                {
                    ReloaddNpcConfig(m_npcs.ElementAt(i).Value);
                    i--;
                }
            }
            
            public void TestSelect()
            {
                if (m_isTest)
                return;
                
                m_isSelectedTest = true;
                m_isTest = true;
                SelectedNpc.CreatorBrain.StartTest();
            }
            
            public void TestAll()
            {
                if (m_isTest)
                return;
                
                m_isSelectedTest = false;
                m_isTest = true;
                
                foreach (var npc in m_npcs)
                {
                    npc.Value.CreatorBrain.StartTest();
                }
            }
            
            public void StopTest()
            {
                if (m_isTest == false)
                return;
                
                if (m_isSelectedTest == false)
                {
                    foreach(var npc in m_npcs)
                    {
                        npc.Value.CreatorBrain.StopTest();
                    }
                }
                else
                {
                    SelectedNpc.CreatorBrain.StopTest();
                }
                
                m_isTest = false;
            }
        }
        #endregion

        #region Gameplay\NpcCreator\NpcCreator_Manager.cs
        public static class NpcCreator_Manager
        {
            public const string NPC_FILE_BASE = "npc_";
            
            private static Dictionary<ulong, NpcCreator_Controller> m_controllers = new Dictionary<ulong, NpcCreator_Controller>();
            public static IReadOnlyDictionary<ulong, NpcCreator_Controller> Controllers => m_controllers;
            
            private static Dictionary<string, CustomNpc_Configuration> m_npcConfigurations = new Dictionary<string, CustomNpc_Configuration>();
            public static IReadOnlyDictionary<string, CustomNpc_Configuration> NpcConfigurations => m_npcConfigurations;
            
            private static Dictionary<string, NpcCreator_NpcController> m_editedNpcs = new Dictionary<string, NpcCreator_NpcController>();
            public static IReadOnlyDictionary<string, NpcCreator_NpcController> EditesNpcs => m_editedNpcs;
            public static Plugin Plugin { get; private set; }
            
            public static bool IsStarted = false;
            
            public static bool StartCreator(BasePlayer player, Plugin plugin)
            {
                if (Plugin != plugin || plugin == null)
                {
                    if (Plugin != null)
                    Stop();
                    
                    Plugin = plugin;
                    StartNew();
                }
                
                if (m_controllers.ContainsKey(player.userID))
                return false;
                
                var creator = new NpcCreator_Controller(player);
                m_controllers.Add(player.userID, creator);
                
                creator.onAddNpc += OnNpcAdded;
                creator.onRemoveNpc += OnNpcRemoved;
                
                IsStarted = true;
                return true;
            }
            
            private static void StartNew()
            {
                LoadConfigs();
            }
            
            public static bool StopCreator(BasePlayer player)
            {
                if (m_controllers.ContainsKey(player.userID) == false)
                return false;
                
                var controller = m_controllers[player.userID];
                
                controller.Stop();
                m_controllers.Remove(player.userID);
                
                controller.onAddNpc -= OnNpcAdded;
                controller.onRemoveNpc -= OnNpcRemoved;
                
                if (m_controllers.Count == 0)
                IsStarted = false;
                
                return true;
            }
            
            public static void Stop()
            {
                foreach(var controller in m_controllers.Values)
                {
                    controller.Stop();
                    
                    controller.onAddNpc -= OnNpcAdded;
                    controller.onRemoveNpc -= OnNpcRemoved;
                }
                
                IsStarted = false;
                m_controllers = null;
            }
            
            public static void OnEntityKill(CustomNpc_Component npcComponent)
            {
                if (m_editedNpcs.ContainsKey(npcComponent.displayName) == false)
                return;
                
                var editedNpc = m_editedNpcs[npcComponent.displayName];
                editedNpc.OnKill();
            }
            
            private static void OnNpcAdded(NpcCreator_NpcController npc)
            {
                m_editedNpcs.Add(npc.Name, npc);
            }
            
            private static void OnNpcRemoved(string npcName)
            {
                m_editedNpcs.Remove(npcName);
            }
            
            public static void AddNpcConfiguration(string name, CustomNpc_Configuration config)
            {
                if (m_npcConfigurations.ContainsKey(name))
                return;
                
                m_npcConfigurations.Add(name, config);
            }
            
            public static void RemoveNpcConfiguration(string name)
            {
                if (m_npcConfigurations.ContainsKey(name) == false)
                return;
                
                m_npcConfigurations.Remove(name);
            }
            
            public static void Save()
            {
                foreach(var controller in m_controllers)
                {
                    controller.Value.SaveAllNpcConfig();
                }
            }
            
            public static void LoadConfigs()
            {
                if (Directory.Exists($"{Interface.Oxide.DataFileSystem.Directory}/{Plugin.Name}") == false)
                {
                    return;
                }
                
                var files = Interface.Oxide.DataFileSystem.GetFiles($"{Plugin.Name}");
                
                foreach (var filePath in files)
                {
                    if (filePath.Contains(NPC_FILE_BASE) == false)
                    continue;
                    
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    var file = Interface.Oxide.DataFileSystem.GetFile($"{Plugin.Name}/{fileName}");
                    
                    if (file == null)
                    {
                        Interface.Oxide.LogWarning($"[CustomNpc] Imposible to load config {fileName} for plugin {Plugin.Name}");
                    }
                    else
                    {
                        Interface.Oxide.LogInfo($"[CustomNpc] Config {fileName} for plugin {Plugin.Name} loaded");
                    }
                    
                    var config = file.ReadObject<CustomNpc_Configuration>();
                    
                    AddNpcConfiguration(fileName.Replace(NPC_FILE_BASE, ""), config);
                }
            }
            
            public static void ReloadConfig(string name)
            {
                if (m_npcConfigurations.ContainsKey(name) == false)
                return;
                
                var fileName = NpcCreator_Manager.NPC_FILE_BASE + name;
                
                var file = Interface.Oxide.DataFileSystem.GetFile($"{Plugin.Name}/{fileName}");
                var config = file.ReadObject<CustomNpc_Configuration>();
                
                m_npcConfigurations[name] = config;
            }
        }
        #endregion

        #region Gameplay\NpcCreator\NpcCreator_NpcController.cs
        public class NpcCreator_NpcController
        {
            public const float SELECTED_OVERLAY_TIME = 10.0f;
            
            public readonly CustomNpc_Entity Npc;
            public readonly NpcCreator_BrainComponent CreatorBrain;
            public readonly string Name;
            public readonly Vector3 StartPosition;
            
            //private List<Item> m_lastPlayerWearItems = new List<Item>();
            //private List<Item> m_lastPlayerBeltItems = new List<Item>();
            
            private bool m_onSetWearItemsToPlayer = true;
            private bool m_onSetBeltItemsToPlayer = true;
            
            public Action<NpcCreator_NpcController> onKill;
            private readonly BasePlayer m_basePlayer;
            
            private Coroutine m_selectedOverlayCoroutine;
            private bool m_isSelected;
            
            public NpcCreator_NpcController(BasePlayer player, string name, CustomNpc_Entity npc)
            {
                m_basePlayer = player;
                Name = name;
                Npc = npc;
                
                StartPosition = npc.Controller.Component.ServerPosition;
                
                CreatorBrain = npc.Controller.Brain.Component as NpcCreator_BrainComponent;
            }
            
            public void MovePlayerWearItemsToNpc(PlayerInventory playerInventory)
            {
                if (m_onSetWearItemsToPlayer)
                return;
                
                //m_lastPlayerWearItems = new List<Item>(playerInventory.containerWear.itemList);
                CopyWearItems(playerInventory, Npc.Controller.Component.inventory);
            }
            
            public void MoveNpcWearItemsToPlayer(PlayerInventory playerInventory)
            {
                m_onSetWearItemsToPlayer = true;
                CopyWearItems(Npc.Controller.Component.inventory, playerInventory);
                m_onSetWearItemsToPlayer = false;
            }
            
            private void CopyWearItems(PlayerInventory a, PlayerInventory b)
            {
                // clear
                for (int i = b.containerWear.itemList.Count - 1; i >= 0; i--)
                {
                    Item item = b.containerWear.itemList[i];
                    item.RemoveFromContainer();
                    item.Remove();
                }
                
                if (a.containerWear == null || a.containerWear.itemList.Count == 0)
                return;
                
                // copy
                foreach(var item in a.containerWear.itemList)
                {
                    var copiedItem = ItemManager.CreateByName(item.info.shortname, 1, item.skin);
                    if (copiedItem == null) continue;
                    if (!copiedItem.MoveToContainer(b.containerWear)) copiedItem.Remove();
                }
            }
            
            
            public void MovePlayerBeltItemsToNpc(PlayerInventory playerInventory)
            {
                if (m_onSetBeltItemsToPlayer)
                return;
                
                //m_lastPlayerBeltItems = new List<Item>(playerInventory.containerBelt.itemList);
                CopyBeltItems(playerInventory, Npc.Controller.Component.inventory);
            }
            
            public void MoveNpcBeltItemsToPlayer(PlayerInventory playerInventory)
            {
                m_onSetBeltItemsToPlayer = true;
                CopyBeltItems(Npc.Controller.Component.inventory, playerInventory);
                m_onSetBeltItemsToPlayer = false;
            }
            
            private void CopyBeltItems(PlayerInventory a, PlayerInventory b)
            {
                // clear
                for (int i = b.containerBelt.itemList.Count - 1; i >= 0; i--)
                {
                    Item item = b.containerBelt.itemList[i];
                    item.RemoveFromContainer();
                    item.Remove();
                }
                
                if (a.containerBelt == null || a.containerBelt.itemList.Count == 0)
                return;
                
                // copy
                foreach (var item in a.containerBelt.itemList)
                {
                    var copiedItem = ItemManager.CreateByName(item.info.shortname, 1, item.skin);
                    if (copiedItem == null) continue;
                    if (!copiedItem.MoveToContainer(b.containerBelt)) copiedItem.Remove();
                }
                
                Npc.Controller.Component.OnDesequipingWeapon();
            }
            
            //private bool WearItemsChange()
            //{
                //    if (m_lastPlayerWearItems.Count != Npc.Controller.Component.inventory.containerWear.itemList.Count)
                //        return true;
                
                //    for (int i = 0; i < m_lastPlayerWearItems.Count; i++)
                //    {
                    //        var playerItem = m_lastPlayerWearItems[i];
                    //        var npcItem = Npc.Controller.Component.inventory.containerWear.itemList[i];
                    
                    //        if (playerItem.info.shortname != npcItem.info.shortname && playerItem.skin != npcItem.skin)
                    //            return true;
                //    }
                
                //    return false;
            //}
            
            public void OnKill()
            {
                onKill?.Invoke(this);
                
                if (m_isSelected)
                {
                    OnUnselect();
                }
            }
            
            public void OnSelect()
            {
                //StartSelectedNpcOverlayTimer();
                m_isSelected = true;
            }
            
            public void OnUnselect()
            {
                //StopSelectedNpcOverlayTimer();
                m_isSelected = false;
                m_onSetWearItemsToPlayer = true;
                m_onSetBeltItemsToPlayer = true;
            }
            
            private void StopSelectedNpcOverlayTimer()
            {
                if (m_selectedOverlayCoroutine != null)
                ServerMgr.Instance.StopCoroutine(m_selectedOverlayCoroutine);
                
                m_selectedOverlayCoroutine = null;
            }
            
            private void StartSelectedNpcOverlayTimer()
            {
                if (m_selectedOverlayCoroutine != null)
                StopSelectedNpcOverlayTimer();
                
                ServerMgr.Instance.StartCoroutine(SelectedNpcOverlay());
            }
            
            private IEnumerator SelectedNpcOverlay()
            {
                while (m_isSelected)
                {
                    m_basePlayer.SendConsoleCommand("ddraw.sphere", SELECTED_OVERLAY_TIME, Color.blue, Npc.Controller.Component.ServerPosition + Vector3.up, 1f);
                    yield return CoroutineEx.waitForSeconds(SELECTED_OVERLAY_TIME);
                }
            }
        }
        #endregion

        #region Gameplay\NpcCreator\NpcCreator_UI.cs
        internal class NpcCreator_UI
        {
        }
        #endregion

        #region Gameplay\AI\States\ChangeWeaponState.cs
        public class ChangeWeaponState : CustomAIState
        {
            private AttackEntity m_lastBestWeapon;
            private Item m_lastBestWeaponItem;
            public ChangeWeaponState() : base(AIState.Cooldown)
            {
                
            }
            
            public override bool CanEnter()
            {
                base.CanEnter();
                
                if (CanEquipWeapon() == false)
                return false;
                
                var bestWeapon = GetBestWeaponToEquip();
                if (bestWeapon == null)
                return false;
                
                if (bestWeapon == m_npc.Component.CurrentWeapon)
                return false;
                
                return true;
            }
            
            public override void StateEnter(BaseAIBrain brain, BaseEntity entity)
            {
                base.StateEnter(brain, entity);
                EquipWeapon();
            }
            
            public override bool CanLeave()
            {
                return true;
            }
            
            public override float GetWeight()
            {
                return 75f;
            }
            
            private bool CanEquipWeapon()
            {
                if (m_npc.Component.inventory == null || m_npc.Component.inventory.containerBelt == null) return false;
                if (m_npc.Component.IsEquipingWeapon) return false;
                return true;
            }
            
            protected virtual AttackEntity GetBestWeaponToEquip()
            {
                float distanceToTarget = m_npc.Component.CurrentTarget != null ? m_npc.DistanceToTarget : 25; // Default distance
                
                List<Item> availableWeapons = m_npc.Component.inventory.containerBelt.itemList
                .Where(item => GetWeaponRangeType(item) != EWeaponRangeType.NONE)
                .ToList();
                
                if (!availableWeapons.Any())
                return null;
                
                EWeaponRangeType desiredWeaponType = GetDesiredWeaponTypeForDistance(distanceToTarget);
                Item bestWeapon = availableWeapons.FirstOrDefault(item => GetWeaponRangeType(item) == desiredWeaponType);
                
                if (bestWeapon == null)
                bestWeapon = availableWeapons.First();
                
                var heldEntity = bestWeapon.GetHeldEntity();
                if (heldEntity == null)
                return null;
                
                var attackEntity = heldEntity as AttackEntity;
                if (attackEntity == null)
                return null;
                
                m_lastBestWeapon = attackEntity;
                m_lastBestWeaponItem = bestWeapon;
                
                return attackEntity;
            }
            
            // Todo : use config ?
            private EWeaponRangeType GetDesiredWeaponTypeForDistance(float distance)
            {
                if (distance <= 5) return EWeaponRangeType.Melee;
                if (distance <= 8) return EWeaponRangeType.ShortDistance;
                if (distance <= 16) return EWeaponRangeType.MidleDistance;
                if (distance <= 32) return EWeaponRangeType.HighDistance;
                return EWeaponRangeType.LongDistance;
            }
            
            public enum EWeaponRangeType
            {
                Melee = 0,
                ShortDistance = 1,
                MidleDistance = 2,
                HighDistance = 3,
                LongDistance = 4,
                NONE = -1
            }
            
            private void EquipWeapon()
            {
                m_npc.Component.OnEquipingWeapon(m_lastBestWeapon);
                m_npc.Component.UpdateActiveItem(m_lastBestWeaponItem.uid);
                m_lastBestWeapon.TopUpAmmo();
                
                BaseProjectile baseProjectile = m_lastBestWeapon as BaseProjectile;
                if (baseProjectile != null)
                {
                    WeaponsData.Range range = null;
                    
                    if (WeaponsData.WeaponsRange.TryGetValue(m_lastBestWeaponItem.info.shortname, out range))
                    {
                        m_lastBestWeapon.effectiveRange = range.EffectiveRange;
                        m_lastBestWeapon.attackLengthMin = range.AttackLengthMin;
                        m_lastBestWeapon.attackLengthMax = range.AttackLengthMax;
                    }
                    
                    m_lastBestWeapon.aiOnlyInRange = true;
                    
                    if (baseProjectile.MuzzlePoint == null)
                    baseProjectile.MuzzlePoint = baseProjectile.transform;
                    
                    var belt = m_npc.Configuration.BeltItems.FirstOrDefault(x => x.ShortName == m_lastBestWeaponItem.info.shortname);
                    
                    if (belt != null)
                    {
                        string ammo = belt.Ammo;
                        if (!string.IsNullOrEmpty(ammo))
                        {
                            if (baseProjectile.primaryMagazine != null)
                            {
                                var ammoType = ItemManager.FindItemDefinition(ammo);
                                if (ammoType != null)
                                {
                                    baseProjectile.primaryMagazine.ammoType = ammoType;
                                    baseProjectile.SendNetworkUpdateImmediate();
                                }
                            }
                        }
                    }
                    
                }
                else
                {
                    Chainsaw chainSaw = m_lastBestWeapon as Chainsaw;
                    if (chainSaw != null)
                    {
                        chainSaw.ServerNPCStart();
                    }
                }
                
                m_npc.Component.Invoke(m_npc.Component.OnFinishEquipingWeapon, 1.5f);
            }
            
            private EWeaponRangeType GetWeaponRangeType(Item item)
            {
                if (WeaponsData.MeleeWeapons.Contains(item.info.shortname)) return EWeaponRangeType.Melee;
                if (WeaponsData.FirstDistanceWeapons.Contains(item.info.shortname)) return EWeaponRangeType.ShortDistance;
                if (WeaponsData.SecondDistanceWeapons.Contains(item.info.shortname)) return EWeaponRangeType.MidleDistance;
                if (WeaponsData.ThirdDistanceWeapons.Contains(item.info.shortname)) return EWeaponRangeType.HighDistance;
                if (WeaponsData.FourthDistanceWeapons.Contains(item.info.shortname)) return EWeaponRangeType.LongDistance;
                return EWeaponRangeType.NONE;
            }
        }
        #endregion

        #region Gameplay\AI\States\CustomAIState.cs
        public abstract class CustomAIState : BasicAIState
        {
            protected CustomNpc_Controller m_npc;
            
            public CustomAIState(AIState state) : base(state)
            {
                
            }
            
            public virtual void Setup(CustomNpc_Controller npc)
            {
                m_npc = npc;
            }
            
            public override void StateEnter(BaseAIBrain brain, BaseEntity entity)
            {
                //Interface.Oxide.LogInfo("State Enter : " + this.GetType().ToString());
            }
            
            public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
            {
                //Interface.Oxide.LogInfo("State Leave : " + this.GetType().ToString());
            }
            
            public override bool CanEnter()
            {
                return true;
            }
            
            public override bool CanInterrupt()
            {
                return true;
            }
            
            public override bool CanLeave()
            {
                return true;
            }
            
            public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
            {
                return StateStatus.Running;
            }
            
            public virtual void Clear()
            {
                m_npc = null;
            }
        }
        #endregion

        #region Gameplay\AI\States\DefaultChaseState.cs
        public class DefaultChaseState : CustomAIState
        {
            public DefaultChaseState() : base(AIState.Chase)
            {
                
            }
            
            public override float GetWeight() => 50.0f;
            
            public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
            {
                base.StateThink(delta, brain, entity);
                
                if (m_npc.Component.CurrentTarget == null)
                return StateStatus.Error;
                
                SetNpcDestination();
                
                return StateStatus.Running;
            }
            
            private void SetNpcDestination()
            {
                var navigationSpeed = BaseNavigator.NavigationSpeed.Fast;
                
                if (m_npc.Component.CurrentWeapon is BaseProjectile)
                {
                    navigationSpeed = m_npc.DistanceToTarget > 10f ? BaseNavigator.NavigationSpeed.Fast : BaseNavigator.NavigationSpeed.Normal;
                }
                
                m_npc.Pathfinding.SetDestination(m_npc.Component.CurrentTarget.transform.position, 2f, navigationSpeed);
            }
            
            public override bool CanEnter()
            {
                base.CanEnter();
                
                if (m_npc.DistanceFromHome > m_npc.Configuration.ChaseRange)
                return false;
                
                if (m_npc.Component.CurrentTarget == null)
                return false;
                
                return true;
            }
            
            public override bool CanLeave()
            {
                return true;
            }
        }
        #endregion

        #region Gameplay\AI\States\DefaultCombatState.cs
        internal class DefaultCombatState : CustomAIState
        {
            private float m_nextStrafeTime;
            
            public DefaultCombatState() : base(AIState.Combat)
            {
            }
            
            public override float GetWeight() => 75f;
            
            public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
            {
                base.StateLeave(brain, entity);
                
                m_npc.Component.SetDucked(false);
                brain.Navigator.ClearFacingDirectionOverride();
            }
            
            public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
            {
                base.StateThink(delta, brain, entity);
                
                if (m_npc.Component.CurrentTarget == null)
                return StateStatus.Error;
                
                brain.Navigator.SetFacingDirectionEntity(m_npc.Component.CurrentTarget);
                
                var currentWeapon = m_npc.Component.CurrentWeapon;
                var isTimeToStrafe = UnityEngine.Time.time > m_nextStrafeTime;
                
                if (currentWeapon is BaseProjectile && isTimeToStrafe)
                {
                    HandleBaseProjectileActions(currentWeapon);
                }
                else if (currentWeapon is FlameThrower)
                {
                    HandleFlameThrowerActions();
                }
                else if (currentWeapon is BaseMelee)
                {
                    HandleBaseMeleeActions();
                }
                
                return StateStatus.Running;
            }
            
            private void HandleBaseProjectileActions(AttackEntity currentWeapon)
            {
                float deltaTime;
                if (UnityEngine.Random.Range(0, 3) == 1)
                {
                    deltaTime = currentWeapon is BaseLauncher ? UnityEngine.Random.Range(0.5f, 1f) : UnityEngine.Random.Range(1f, 2f);
                    m_nextStrafeTime = UnityEngine.Time.time + deltaTime;
                    m_npc.Component.SetDucked(true);
                    brain.Navigator.Stop();
                }
                else
                {
                    deltaTime = currentWeapon is BaseLauncher ? UnityEngine.Random.Range(1f, 1.5f) : UnityEngine.Random.Range(2f, 3f);
                    m_nextStrafeTime = UnityEngine.Time.time + deltaTime;
                    m_npc.Component.SetDucked(false);
                    m_npc.Pathfinding.SetDestination(m_npc.Pathfinding.GetRandomPositionAround(m_npc.GameObject.transform.position, 2f), 2f, BaseNavigator.NavigationSpeed.Normal);
                }
                
                if (currentWeapon is BaseLauncher)
                {
                    m_npc.FireGrenadeLauncher();
                }
                else
                {
                    m_npc.Component.ShotTest(m_npc.DistanceToTarget);
                }
            }
            
            private void HandleFlameThrowerActions()
            {
                if (m_npc.DistanceToTarget < m_npc.Component.CurrentWeapon.effectiveRange)
                {
                    m_npc.FireFlameThrower();
                }
                m_npc.Pathfinding.SetDestination(m_npc.Component.CurrentTarget.transform.position, 2f, BaseNavigator.NavigationSpeed.Fast);
            }
            
            private void HandleBaseMeleeActions()
            {
                if (m_npc.DistanceToTarget < m_npc.Component.CurrentWeapon.effectiveRange * 2f)
                {
                    m_npc.UseMeleeWeapon();
                }
                m_npc.Pathfinding.SetDestination(m_npc.Component.CurrentTarget.transform.position, 2f, BaseNavigator.NavigationSpeed.Fast);
            }
            
            public override bool CanEnter()
            {
                base.CanEnter();
                
                if (m_npc.Component.IsEquipingWeapon) return false;
                if (m_npc.Component.CurrentWeapon == null) return false;
                if (m_npc.Component.CurrentWeapon.ShortPrefabName == "mgl.entity" && m_npc.Component.IsReloadGrenadeLauncher) return false;
                if (m_npc.Component.CurrentWeapon is FlameThrower && m_npc.Component.IsReloadFlameThrower) return false;
                if (m_npc.DistanceFromHome > m_npc.Configuration.ChaseRange) return false;
                if (m_npc.Component.CurrentTarget == null) return false;
                if (m_npc.DistanceToTarget > m_npc.Component.EngagementRange()) return false;
                if (!m_npc.Component.CanSeeTarget(m_npc.Component.CurrentTarget)) return false;
                if (m_npc.IsBehindBarricade()) return false;
                return true;
            }
        }
        #endregion

        #region Gameplay\AI\States\DefaultCombatStationaryState.cs
        public class DefaultCombatStationaryState : CustomAIState
        {
            private float m_nextStrafeTime;
            
            public DefaultCombatStationaryState() : base(AIState.CombatStationary)
            {
            }
            
            public override float GetWeight() => 80f;
            
            public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
            {
                base.StateLeave(brain, entity);
                ResetState();
            }
            
            private void ResetState()
            {
                m_nextStrafeTime = 0.0f;
                if (!m_npc.Component.IsMounted())
                m_npc.Component.SetDucked(false);
                brain.Navigator.ClearFacingDirectionOverride();
            }
            
            public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
            {
                base.StateThink(delta, brain, entity);
                
                if (m_npc.Component.CurrentTarget == null)
                return StateStatus.Error;
                
                brain.Navigator.SetFacingDirectionEntity(m_npc.Component.CurrentTarget);
                HandleWeaponActions();
                
                return StateStatus.Running;
            }
            
            private void HandleWeaponActions()
            {
                if (m_npc.Component.CurrentWeapon is BaseProjectile && UnityEngine.Time.time > m_nextStrafeTime)
                {
                    ProcessProjectileWeaponActions();
                }
                else if (m_npc.Component.CurrentWeapon is FlameThrower && m_npc.DistanceToTarget < m_npc.Component.CurrentWeapon.effectiveRange)
                {
                    m_npc.FireFlameThrower();
                }
                else if (m_npc.Component.CurrentWeapon is BaseMelee && m_npc.DistanceToTarget < m_npc.Component.CurrentWeapon.effectiveRange * 2f)
                {
                    m_npc.UseMeleeWeapon();
                }
            }
            
            private void ProcessProjectileWeaponActions()
            {
                float deltaTime = DetermineDeltaTime();
                m_nextStrafeTime = UnityEngine.Time.time + deltaTime;
                
                if (!m_npc.Component.IsMounted())
                {
                    bool shouldDuck = UnityEngine.Random.Range(0, 3) == 1;
                    m_npc.Component.SetDucked(shouldDuck);
                }
                
                if (m_npc.Component.CurrentWeapon is BaseLauncher)
                {
                    m_npc.FireGrenadeLauncher();
                }
                else
                {
                    m_npc.Component.ShotTest(m_npc.DistanceToTarget);
                }
            }
            
            private float DetermineDeltaTime()
            {
                if (m_npc.Component.CurrentWeapon is BaseLauncher)
                {
                    return UnityEngine.Random.Range(0.5f, 1.5f);
                }
                return UnityEngine.Random.Range(1f, 3f);
            }
            
            public override bool CanEnter()
            {
                if (m_npc.Component.CurrentWeapon == null || m_npc.Component.CurrentTarget == null)
                return false;
                
                bool hasSpecificWeaponConditions =
                (m_npc.Component.CurrentWeapon.ShortPrefabName == "mgl.entity" && m_npc.Component.IsReloadGrenadeLauncher) ||
                (m_npc.Component.CurrentWeapon is FlameThrower && m_npc.Component.IsReloadFlameThrower);
                
                if (hasSpecificWeaponConditions)
                return false;
                
                if (m_npc.DistanceToTarget > m_npc.Component.EngagementRange() || !m_npc.Component.CanSeeTarget(m_npc.Component.CurrentTarget) || m_npc.IsBehindBarricade())
                return false;
                
                return true;
            }
        }
        #endregion

        #region Gameplay\AI\States\DefaultHealState.cs
        internal class DefaultHealState : CustomAIState
        {
            public DefaultHealState() : base(AIState.Cooldown)
            {
            }
            
            public override float GetWeight() => 100;
            
            public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
            {
                base.StateThink(delta, brain, entity);
                
                if (m_npc.Component.IsHealing)
                {
                    return StateStatus.Running;
                }
                else
                {
                    if (CanEnter() == false)
                    {
                        m_npc.Heal();
                        return StateStatus.Running;
                    }
                }
                
                return StateStatus.Finished;
            }
            
            public override bool CanLeave()
            {
                base.CanLeave();
                
                return CanEnter() == false;
            }
            
            public override bool CanEnter()
            {
                base.CanEnter();
                
                if (m_npc.Component.IsHealing || m_npc.Component.health >= m_npc.Configuration.MaxHealth|| m_npc.Component.IsEquipingWeapon) return false;
                return m_npc.Component.inventory.containerBelt.itemList.Any(x => x.info.shortname == "syringe.medical");
            }
            
        }
        #endregion

        #region Gameplay\AI\States\DefaultIdleState.cs
        public class DefaultIdleState : CustomAIState
        {
            public DefaultIdleState() : base(AIState.Idle)
            {
            }
            public override float GetWeight() => 50f;
        }
        #endregion

        #region Gameplay\AI\States\DefaultMoveTowardsState.cs
        internal class DefaultMoveTowardsState : CustomAIState
        {
            public DefaultMoveTowardsState()
            : base(AIState.MoveTowards)
            {
            }
            
            public override float GetWeight() => 50.0f;
            
            public override void StateLeave(BaseAIBrain brain, BaseEntity entity)
            {
                base.StateLeave(brain, entity);
                Stop();
            }
            
            private void Stop()
            {
                brain.Navigator.Stop();
            }
            
            public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
            {
                base.StateThink(delta, brain, entity);
                
                BaseEntity targetEntity = GetMemoryTargetEntity();
                if (targetEntity == null)
                {
                    Stop();
                    return StateStatus.Error;
                }
                
                FaceTarget();
                
                if (!SetNavigatorDestination(targetEntity.transform.position))
                {
                    return StateStatus.Error;
                }
                
                return brain.Navigator.Moving ? StateStatus.Running : StateStatus.Finished;
            }
            
            private BaseEntity GetMemoryTargetEntity()
            {
                return brain.Events.Memory.Entity.Get(brain.Events.CurrentInputMemorySlot);
            }
            
            private bool SetNavigatorDestination(Vector3 destination)
            {
                return brain.Navigator.SetDestination(destination, brain.Navigator.MoveTowardsSpeed, 0.25f);
            }
            
            private void FaceTarget()
            {
                if (!brain.Navigator.FaceMoveTowardsTarget) return;
                
                BaseEntity targetEntity = GetMemoryTargetEntity();
                if (targetEntity == null)
                {
                    brain.Navigator.ClearFacingDirectionOverride();
                    return;
                }
                
                if (IsCloseToTarget(targetEntity.transform.position))
                {
                    brain.Navigator.SetFacingDirectionEntity(targetEntity);
                }
            }
            
            private bool IsCloseToTarget(Vector3 targetPosition)
            {
                return Vector3.Distance(targetPosition, brain.transform.position) <= 1.5f;
            }
            
            
            
        }
        #endregion

        #region Gameplay\AI\States\DefaultRoamState.cs
        public class DefaultRoamState : CustomAIState
        {
            public DefaultRoamState() : base(AIState.Roam)
            {
            }
            
            public override float GetWeight() => 25f;
            
            public override StateStatus StateThink(float delta, BaseAIBrain brain, BaseEntity entity)
            {
                base.StateThink(delta, brain, entity);
                
                if (m_npc.DistanceFromHome > m_npc.Configuration.RoamRange) m_npc.Pathfinding.SetDestination(m_npc.Component.HomePosition, 2f, BaseNavigator.NavigationSpeed.Fast);
                else if (!brain.Navigator.Moving && m_npc.Configuration.RoamRange > 2f) m_npc.Pathfinding.SetDestination(m_npc.Pathfinding.GetRandomPositionAround(m_npc.Component.HomePosition, m_npc.Configuration.RoamRange - 2f), 2f, BaseNavigator.NavigationSpeed.Slowest);
                return StateStatus.Running;
            }
        }
        #endregion

        #region Gameplay\AI\States\SafeState.cs
        public class SafeState : CustomAIState
        {
            public SafeState(AIState state) : base(state)
            {
            }
        }
        #endregion

        #region Gameplay\Controllers\NpcAttire\NpcAttire_Controller_Base.cs
        public abstract class NpcAttire_Controller_Base
        {
            public abstract void Equip();
        }
        #endregion

        #region Gameplay\Controllers\NpcAttire\NpcAttire_Default_Controller.cs
        public class NpcAttire_Default_Controller : NpcAttire_Controller_Base
        {
            private List<CustomNpc_BeltItem> m_belt;
            private List<CustomNpc_WearItem> m_wear;
            private PlayerInventory m_inventory;
            
            public NpcAttire_Default_Controller(PlayerInventory inventory, List<CustomNpc_BeltItem> belt, List<CustomNpc_WearItem> wear)
            {
                m_belt = belt;
                m_wear = wear;
                m_inventory = inventory;
            }
            
            public override void Equip()
            {
                if (m_wear.Count > 0)
                {
                    foreach (Item item in m_wear.Select(x => ItemManager.CreateByName(x.ShortName, 1, x.SkinId)))
                    {
                        if (item == null) continue;
                        if (!item.MoveToContainer(m_inventory.containerWear)) item.Remove();
                    }
                }
                if (m_belt.Count > 0)
                {
                    foreach (var npcItem in m_belt)
                    {
                        Item item = ItemManager.CreateByName(npcItem.ShortName, npcItem.Amount, npcItem.SkinId);
                        if (item == null) continue;
                        foreach (ItemDefinition itemDefinition in npcItem.Mods.Select(ItemManager.FindItemDefinition)) if (itemDefinition != null) item.contents.AddItem(itemDefinition, 1);
                        if (!item.MoveToContainer(m_inventory.containerBelt)) item.Remove();
                    }
                }
            }
            
        }
        #endregion

        #region Gameplay\Controllers\NpcAttire\NpcAttire_Kits_Controller.cs
        public class NpcAttire_Kits_Controller : NpcAttire_Controller_Base
        {
            public const string PLUGIN_NAME = "Kits";
            
            private string m_kitName;
            private CustomNpc_Controller m_npc;
            
            public NpcAttire_Kits_Controller(CustomNpc_Controller npc, string kitName)
            {
                m_kitName = kitName;
                m_npc = npc;
            }
            
            public override void Equip()
            {
                PluginsExtensionsManager.Instance.LoadedPlugins[PLUGIN_NAME]
                .Call("GiveKit", m_npc.Component, m_kitName);
            }
        }
        #endregion

    }

}
