using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Ext.CustomNpc.Gameplay.AI.States;
using Oxide.Ext.CustomNpc.Gameplay.Components;
using Oxide.Ext.CustomNpc.Gameplay.Configurations;
using Oxide.Ext.CustomNpc.Gameplay.Controllers;
using Oxide.Ext.CustomNpc.Gameplay.Entities;
using Oxide.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;


namespace Oxide.Ext.CustomNpc.Gameplay.Managers
{
    public static class CustomNpc_Manager
    {
        public const string NPC_FILE_BASE = "npc_";
        public const ulong CUSTOM_NPC_SKIN_ID = 11185464824609;

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

        public static CustomNpc_Entity CreateAndStartEntity(CustomNpc_Component component, CustomNpc_Controller npc, CustomNpcBrain_Controller brain, bool register = false)
        {
            CustomNpc_Entity entity = new CustomNpc_Entity(component.gameObject, npc);
            entity.Start(brain);

            if (register)
            {
                m_spawnedNpcs.Add(entity.Controller.Component.net.ID.Value, entity);
                m_spawnedNpcsByComponent.Add(component, entity);
            }

            return entity;
        }
        #endregion

        public static Dictionary<string, CustomNpc_Configuration> LoadNpcs(RustPlugin plugin)
        {
            Dictionary<string, CustomNpc_Configuration> npcs = null;

            if (Directory.Exists($"{Interface.Oxide.DataFileSystem.Directory}/{plugin.Name}") == false)
            {
                return null;
            }

            var files = Interface.Oxide.DataFileSystem.GetFiles($"{plugin.Name}");

            foreach (var filePath in files)
            {
                if (filePath.Contains(NPC_FILE_BASE) == false)
                    continue;

                string fileName = Path.GetFileNameWithoutExtension(filePath);
                var file = Interface.Oxide.DataFileSystem.GetFile($"{plugin.Name}/{fileName}");

                if (file == null)
                {
                    Interface.Oxide.LogWarning($"[CustomNpc] Imposible to load config {fileName} for plugin {plugin.Name}");
                }
                else
                {
                    Interface.Oxide.LogInfo($"[CustomNpc] Config {fileName} for plugin {plugin.Name} loaded");
                }

                var config = file.ReadObject<CustomNpc_Configuration>();

                if (npcs == null)
                    npcs = new Dictionary<string, CustomNpc_Configuration>();

                npcs.Add(fileName.Replace(NPC_FILE_BASE, ""), config);
            }

            return npcs;
        }

        public static void DestroyAllNpcs()
        {
            foreach(var npc in m_spawnedNpcs)
            {
                m_spawnedNpcsByComponent.Remove(npc.Value.Controller.Component);
                npc.Value.Controller.Component.Kill();
            }

            m_spawnedNpcs.Clear();
        }

        public static void OnPopulateCorpse(ScientistNPC component, ItemContainer container)
        {
            if (component == null)
                return;

            CustomNpc_Entity entity = null;

			Interface.Oxide.LogInfo($"[CustomNpc] OnPopulateCorpse 1");

			if (m_spawnedNpcs.TryGetValue(component.net.ID.Value, out entity) == false)
                return;

			Interface.Oxide.LogInfo($"[CustomNpc] OnPopulateCorpse 2");

			m_spawnedNpcs.Remove(entity.Controller.Component.net.ID.Value);
            m_spawnedNpcsByComponent.Remove(entity.Controller.Component);

            var lootTable = entity.Controller.Configuration.LootTable;
            if (lootTable != null)
            {
				Interface.Oxide.LogInfo($"[CustomNpc] OnPopulateCorpse 3");
				Loot(lootTable, container);
            }
        }

        private static void Loot(LootTableConfiguration configuration, ItemContainer container)
        {
			if (container.itemList != null && container.itemList.Count > 0)
            {
				for (int i = container.itemList.Count - 1; i >= 0; i--)
				{
					Item item = container.itemList[i];
					item.RemoveFromContainer();
					item.Remove();
				}
			}
			int addedItems = 0;
			foreach (var item in configuration.Items)
            {
                if (addedItems >= configuration.MaxItems)
                    break;

                int random = UnityEngine.Random.Range(0, 100);
                if (random > item.ChanceToLoot)
                    continue;

                int randomQuantity = (item.QuantityMin != item.QuantityMax) ? UnityEngine.Random.Range(item.QuantityMin, item.QuantityMax) : item.QuantityMax;
                var createdItem = ItemManager.FindItemDefinition(item.ItemShortname);

                if (createdItem == null)
                    continue;
                
                container.AddItem(createdItem, randomQuantity, item.SkinId);
                addedItems++;
            }

			Interface.Oxide.LogInfo($"Loot 3 " + addedItems);
		}

        public static bool IsVanillaNpc(ScientistNPC npc)
        {
            return npc.skinID != CUSTOM_NPC_SKIN_ID;
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
}
