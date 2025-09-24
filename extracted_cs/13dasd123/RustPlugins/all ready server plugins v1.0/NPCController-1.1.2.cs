using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using UnityEngine.AI;

namespace Oxide.Plugins
{
    [Info("NPCController", "Lime", "1.1.2")]
    public class NPCController : RustPlugin
    {
        #region CreateConfig
        private static JsonConfig _config;
        private struct JsonCargoShip
        {
            [JsonProperty("Any")] public DataNPCJson scientist_astar_full_any;
            [JsonProperty("lr300 статичный")] public DataNPCJson scientist_turret_lr300;
            [JsonProperty("Any статичный")] public DataNPCJson scientist_turret_any;
        }
        private struct JsonMilitaryTunnels
        {
            [JsonProperty("Ученый")] public DataNPCJson scientist;
            [JsonProperty("Any")] public DataNPCJson scientist_full_any;
            [JsonProperty("MP5A4")] public DataNPCJson scientist_full_mp5;
            [JsonProperty("ShotGun")] public DataNPCJson scientist_full_shotgun;
            [JsonProperty("Pistol")] public DataNPCJson scientist_full_pistol;
            [JsonProperty("LR300")] public DataNPCJson scientist_full_lr300;
        }
        private struct DataNPCJson
        {
            [JsonProperty("Увеличение урона")] public float MultiplerDamage;
            [JsonProperty("Максимальное количество HP")] public float MaxHealth;
            [JsonProperty("CoolDown на Шприц")] public float HealByInjector_CoolDown;
            [JsonProperty("Health на Шприц")] public float HealByInjector_Health;
            [JsonProperty("UseTime на Шприц")] public float HealByInjector_UseTime;
            public DataNPCJson(BasePlayer player)
            {
                MultiplerDamage = 1;
                MaxHealth = player.startHealth;
                HealByInjector_CoolDown = -1;
                HealByInjector_Health = 0;
                HealByInjector_UseTime = 2;
            }
        }
        private struct NPCJson
        {
            [JsonProperty("NPC у мусорок")] public DataNPCJson scientistjunkpile;
            [JsonProperty("NPC в CH47")] public DataNPCJson scientist_gunner;
            [JsonProperty("NPC в CargoShip")] public JsonCargoShip cargoship;
            [JsonProperty("NPC в городе бандитов")] public DataNPCJson bandit_guard;
            [JsonProperty("NPC в городе")] public DataNPCJson scientistpeacekeeper;
            [JsonProperty("NPC в туннеле")] public JsonMilitaryTunnels militarytunnels;
            [JsonProperty("Тяжелый NPC")] public DataNPCJson heavy;
        }
        private struct DataAnimalJson
        {
            [JsonProperty("Увеличение урона")] public float MultiplerDamage;
            [JsonProperty("Максимальное количество HP")] public float MaxHealth;
            public DataAnimalJson(BaseAnimalNPC animal)
            {
                MultiplerDamage = 1;
                MaxHealth = animal.startHealth;
            }
        }
        private struct AnimalJson
        {
            [JsonProperty("Медведь")] public DataAnimalJson bear;
            [JsonProperty("Кабан")] public DataAnimalJson boar;
            [JsonProperty("Курица")] public DataAnimalJson сhicken;
            [JsonProperty("Лошадь")] public DataAnimalJson horse;
            [JsonProperty("Олень")] public DataAnimalJson stag;
            [JsonProperty("Волк")] public DataAnimalJson wolf;
        }
        
        private class JsonConfig
        {
            [JsonProperty("Животные")] public AnimalJson animal;
            [JsonProperty("NPC")] public NPCJson NPC;
            public DataAnimalJson? GetJson(BaseAnimalNPC animalNPC)
            {
                switch (animalNPC?.ShortPrefabName.ToLower())
                {
                    case "bear": return animal.bear;
                    case "boar": return animal.boar;
                    case "сhicken": return animal.сhicken;
                    case "horse": return animal.horse;
                    case "stag": return animal.stag;
                    case "wolf": return animal.wolf;
                }
                return null;
            }
            public DataNPCJson? GetJson(BasePlayer playerNPC)
            {
                switch (playerNPC?.ShortPrefabName.ToLower())
                {
                    case "scientistjunkpile": return NPC.scientistjunkpile;
                    case "scientist_gunner": return NPC.scientist_gunner;
                    case "bandit_guard": return NPC.bandit_guard;
                    case "scientistpeacekeeper": return NPC.scientistpeacekeeper;

                    case "scientist_astar_full_any": return NPC.cargoship.scientist_astar_full_any;
                    case "scientist_turret_lr300": return NPC.cargoship.scientist_turret_lr300;
                    case "scientist_turret_any": return NPC.cargoship.scientist_turret_any;

                    case "scientist": return NPC.militarytunnels.scientist;
                    case "scientist_full_any": return NPC.militarytunnels.scientist_full_any;
                    case "scientist_full_lr300": return NPC.militarytunnels.scientist_full_lr300;
                    case "scientist_full_mp5": return NPC.militarytunnels.scientist_full_mp5;
                    case "scientist_full_pistol": return NPC.militarytunnels.scientist_full_pistol;
                    case "scientist_full_shotgun": return NPC.militarytunnels.scientist_full_shotgun;

                }
                return null;
            }
            public void SetJson(BasePlayer playerNPC)
            {
                switch (playerNPC?.ShortPrefabName.ToLower())
                {
                    case "scientistjunkpile": NPC.scientistjunkpile = new DataNPCJson(playerNPC); return;
                    case "scientist_gunner": NPC.scientist_gunner = new DataNPCJson(playerNPC); return;
                    case "bandit_guard": NPC.bandit_guard = new DataNPCJson(playerNPC); return;
                    case "scientistpeacekeeper": NPC.scientistpeacekeeper = new DataNPCJson(playerNPC); return;
                    case "heavyscientist": NPC.heavy = new DataNPCJson(playerNPC); return;

                    case "scientist_astar_full_any": NPC.cargoship.scientist_astar_full_any = new DataNPCJson(playerNPC); return;
                    case "scientist_turret_lr300": NPC.cargoship.scientist_turret_lr300 = new DataNPCJson(playerNPC); return;
                    case "scientist_turret_any": NPC.cargoship.scientist_turret_any = new DataNPCJson(playerNPC); return;

                    case "scientist": NPC.militarytunnels.scientist = new DataNPCJson(playerNPC); return;
                    case "scientist_full_any": NPC.militarytunnels.scientist_full_any = new DataNPCJson(playerNPC); return;
                    case "scientist_full_lr300": NPC.militarytunnels.scientist_full_lr300 = new DataNPCJson(playerNPC); return;
                    case "scientist_full_mp5": NPC.militarytunnels.scientist_full_mp5 = new DataNPCJson(playerNPC); return;
                    case "scientist_full_pistol": NPC.militarytunnels.scientist_full_pistol = new DataNPCJson(playerNPC); return;
                    case "scientist_full_shotgun": NPC.militarytunnels.scientist_full_shotgun = new DataNPCJson(playerNPC); return;
                }
            }
            public void SetJson(BaseAnimalNPC animalNPC)
            {
                switch (animalNPC?.ShortPrefabName.ToLower())
                {
                    case "bear": animal.bear = new DataAnimalJson(animalNPC); return;
                    case "boar": animal.boar = new DataAnimalJson(animalNPC); return;
                    case "сhicken": animal.сhicken = new DataAnimalJson(animalNPC); return;
                    case "horse": animal.horse = new DataAnimalJson(animalNPC); return;
                    case "stag": animal.stag = new DataAnimalJson(animalNPC); return;
                    case "wolf": animal.wolf = new DataAnimalJson(animalNPC); return;
                }
            }
        }
        
        protected override void LoadDefaultConfig()
        {
            _config = null;
            SaveConfig();
            PrintWarning("Creating default config");
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<JsonConfig>();
        }
        protected override void SaveConfig() => Config.WriteObject(_config);
        #endregion
        #region Data
        public static NPCController _plugin = null;
        void Loaded() => _plugin = this;
        void Unload()
        {
            foreach (var edits in BaseNetworkable.serverEntities.Where(ent => ent.GetComponents<Component>().Where(comp => comp.GetType().Name == "NPCEditor").Count() > 0).Select(ent => ent.GetComponents<Component>().Where(comp => comp.GetType().Name == "NPCEditor")))
                foreach (var edit in edits)
                    UnityEngine.Object.Destroy(edit);
        }
        #endregion
        #region Plugin
        void OnEntityTakeDamage(BasePlayer entity, HitInfo hitInfo)
        {
            //foreach (var npc in _data.NPCs) Debug.LogWarning($"[{_plugin.Title}] Loaded NPC[{npc.Key}] {DarkPluginsID} with:\n..{string.Join("\n..", npc.Value.GetParams().Select(kv => $"{kv.Key} - {kv.Value}"))}");
            try
            {
                NPCEditor npcEditor = hitInfo?.Initiator?.GetComponent<NPCEditor>();
                AnimalEditor animEditor = hitInfo?.Initiator?.GetComponent<AnimalEditor>();
                if (npcEditor != null) hitInfo.damageTypes.types = hitInfo.damageTypes.types.Select(type => type * npcEditor._thisData.MultiplerDamage).ToArray();
                if (animEditor != null) hitInfo.damageTypes.types = hitInfo.damageTypes.types.Select(type => type * animEditor._thisData.MultiplerDamage).ToArray();
            }
            catch
            {
            }
        }
        new bool IsLoaded = false;

        void OnServerInitialized()
        {
            Debug.Log(0);
            foreach (var edits in BaseNetworkable.serverEntities.Where(ent => ent.GetComponents<Component>().Where(comp => comp.GetType().Name == "NPCEditor").Count() > 0).Select(ent => ent.GetComponents<Component>().Where(comp => comp.GetType().Name == "NPCEditor")))
                foreach (var edit in edits)
                    UnityEngine.Object.Destroy(edit);
            Debug.Log(1);
            if (_config == null)
            {
                Debug.Log(2);
                _config = new JsonConfig
                {
                    animal = new AnimalJson(),
                    NPC = new NPCJson
                    {
                        cargoship = new JsonCargoShip(),
                        militarytunnels = new JsonMilitaryTunnels(),
                    }
                };
                Debug.Log(3);
                Dictionary<string, Action<BaseEntity>> ents = new Dictionary<string, Action<BaseEntity>>()
                {
                    ["assets/prefabs/npc/scientist/scientistjunkpile.prefab"] = null,
                    ["assets/prefabs/npc/scientist/scientist.prefab"] = null,
                    ["assets/prefabs/npc/scientist/scientistpeacekeeper.prefab"] = null,
                    ["assets/prefabs/npc/bandit/guard/bandit_guard.prefab"] = null,
                    ["assets/prefabs/npc/scientist/htn/scientist_full_pistol.prefab"] = null,
                    ["assets/prefabs/npc/scientist/htn/scientist_full_lr300.prefab"] = null,
                    ["assets/prefabs/npc/scientist/htn/scientist_full_mp5.prefab"] = null,
                    ["assets/prefabs/npc/scientist/htn/scientist_full_shotgun.prefab"] = null,
                    ["assets/prefabs/npc/scientist/htn/scientist_full_any.prefab"] = null,
                    ["assets/prefabs/npc/scientist/scientist_gunner.prefab"] = null,
                    ["assets/prefabs/npc/scientist/htn/scientist_astar_full_any.prefab"] = null,
                    ["assets/prefabs/npc/scientist/htn/scientist_turret_lr300.prefab"] = null,
                    ["assets/prefabs/npc/scientist/htn/scientist_turret_any.prefab"] = null,
                    ["assets/rust.ai/agents/npcplayer/humannpc/heavyscientist/heavyscientist.prefab"] = null,

                    ["assets/rust.ai/agents/bear/bear.prefab"] = null,
                    ["assets/rust.ai/agents/boar/boar.prefab"] = null,
                    ["assets/rust.ai/agents/chicken/chicken.prefab"] = null,
                    ["assets/rust.ai/agents/horse/horse.prefab"] = null,
                    ["assets/rust.ai/agents/stag/stag.prefab"] = null,
                    ["assets/rust.ai/agents/wolf/wolf.prefab"] = null,

                    /*
                  + case "scientistjunkpile": return NPC.scientistjunkpile;
                  + case "scientist_gunner": return NPC.scientist_gunner;
                  + case "bandit_guard": return NPC.bandit_guard;
                  + case "scientistpeacekeeper": return NPC.scientistpeacekeeper;

                  + case "scientist_astar_full_any": return NPC.cargoship.scientist_astar_full_any;
                  + case "scientist_turret_lr300": return NPC.cargoship.scientist_turret_lr300;
                  + case "scientist_turret_any": return NPC.cargoship.scientist_turret_any;

                  + case "scientist": return NPC.militarytunnels.scientist;
                  + case "scientist_full_any": return NPC.militarytunnels.scientist_full_any;
                  + case "scientist_full_lr300": return NPC.militarytunnels.scientist_full_lr300;
                  + case "scientist_full_mp5": return NPC.militarytunnels.scientist_full_mp5;
                  + case "scientist_full_pistol": return NPC.militarytunnels.scientist_full_pistol;
                  + case "scientist_full_shotgun": return NPC.militarytunnels.scientist_full_shotgun;
                    */
                };
                Debug.Log(4);
                foreach (var val in ents.ToArray())
                {
                    switch (val.Key)
                    {
                        case "assets/rust.ai/agents/bear/bear.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BaseAnimalNPC); }; break;
                        case "assets/rust.ai/agents/boar/boar.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BaseAnimalNPC); }; break;
                        case "assets/rust.ai/agents/chicken/chicken.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BaseAnimalNPC); }; break;
                        case "assets/rust.ai/agents/horse/horse.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BaseAnimalNPC); }; break;
                        case "assets/rust.ai/agents/stag/stag.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BaseAnimalNPC); }; break;
                        case "assets/rust.ai/agents/wolf/wolf.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BaseAnimalNPC); }; break;

                        case "assets/prefabs/npc/bandit/guard/bandit_guard.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BasePlayer); }; break;
                        case "assets/prefabs/npc/scientist/scientist.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BasePlayer); }; break;
                        case "assets/prefabs/npc/scientist/scientist_gunner.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BasePlayer); }; break;
                        case "assets/prefabs/npc/scientist/scientistjunkpile.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BasePlayer); }; break;
                        case "assets/prefabs/npc/scientist/scientistpeacekeeper.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BasePlayer); }; break;

                        case "assets/prefabs/npc/scientist/htn/scientist_full_any.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BasePlayer); }; break;
                        case "assets/prefabs/npc/scientist/htn/scientist_full_lr300.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BasePlayer); }; break;
                        case "assets/prefabs/npc/scientist/htn/scientist_full_mp5.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BasePlayer); }; break;
                        case "assets/prefabs/npc/scientist/htn/scientist_full_pistol.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BasePlayer); }; break;
                        case "assets/prefabs/npc/scientist/htn/scientist_full_shotgun.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BasePlayer); }; break;

                        case "assets/prefabs/npc/scientist/htn/scientist_astar_full_any.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BasePlayer); }; break;
                        case "assets/prefabs/npc/scientist/htn/scientist_turret_any.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BasePlayer); }; break;
                        case "assets/prefabs/npc/scientist/htn/scientist_turret_lr300.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BasePlayer); }; break;
                        case "assets/rust.ai/agents/npcplayer/humannpc/heavyscientist/heavyscientist.prefab": ents[val.Key] = (ent) => { _config.SetJson(ent as BasePlayer); }; break;
                    }
                }
                Debug.Log(5);
                InvokeAndKill(ents);
                Debug.Log(6);
                SaveConfig();
                Debug.Log(7);
            }
            Debug.Log(8 + ":" + BaseNetworkable.serverEntities);
            foreach (var ent in BaseNetworkable.serverEntities.ToArray())
            {
                try
                {
                    if (_config.GetJson(ent as BasePlayer) != null) (ent.gameObject.GetComponent<NPCEditor>() ?? ent.gameObject.AddComponent<NPCEditor>()).Init();
                    if (_config.GetJson(ent as BaseAnimalNPC) != null) (ent.gameObject.GetComponent<AnimalEditor>() ?? ent.gameObject.AddComponent<AnimalEditor>()).Init();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Prefab:{ent?.ShortPrefabName}|Player:{ent as BasePlayer}|Animal:{ent as BaseAnimalNPC}]\n{e}");
                }
            }
            IsLoaded = true;
        }
        void OnEntitySpawned(BaseNetworkable net)
        {
            if (!IsLoaded) return; 
            if (_config.GetJson(net as BasePlayer) != null) (net.gameObject.GetComponent<NPCEditor>() ?? net.gameObject.AddComponent<NPCEditor>()).Init();
            if (_config.GetJson(net as BaseAnimalNPC) != null) (net.gameObject.GetComponent<AnimalEditor>() ?? net.gameObject.AddComponent<AnimalEditor>()).Init();
        }
        public static void InvokeAndKill(IEnumerable<KeyValuePair<string, Action<BaseEntity>>> entities)
        {
            foreach (var ent in entities)
            {
                BaseEntity entity = GameManager.server.CreateEntity(ent.Key.ToLower());
                try
                {
                    entity.Spawn();
                    ent.Value?.Invoke(entity);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Prefab:{entity?.ShortPrefabName}|Player:{entity as BasePlayer}|Animal:{entity as BaseAnimalNPC}]\n{e}");
                }
                entity.Kill();
            }
        }
        private class NPCEditor: MonoBehaviour
        {
            public BasePlayer scientist => GetComponent<BasePlayer>();
            public bool medToolsUse = false;
            public DataNPCJson _thisData => _config.GetJson(scientist) ?? default(DataNPCJson);
            public void Init()
            {
                scientist.startHealth = _thisData.MaxHealth;
                SetValue(scientist, "_maxHealth", _thisData.MaxHealth);
                SetValue(scientist, "_health", _thisData.MaxHealth);
            }
            float CoolDown = 0;
            void FixedUpdate()
            {
                if (_thisData.HealByInjector_CoolDown == -1) return;
                if (_thisData.HealByInjector_Health == 0) return;
                if (scientist.Health() + 10 < scientist.MaxHealth())
                {
                    if (CoolDown > _thisData.HealByInjector_CoolDown)
                    {
                        UseInjector();
                        CoolDown = 0;
                    }
                    else
                    {
                        CoolDown += Time.fixedDeltaTime;
                    }
                }
                else
                {
                    CoolDown = 0;
                }
            }
            public void UseInjector()
            {
                if (medToolsUse) return;
                (scientist as NPCPlayerApex)?.Pause();
                Item defItem = scientist.GetActiveItem();
                defItem?.MoveToContainer(scientist.inventory.containerMain);
                Item injItem = ItemManager.CreateByItemID(1079279582, 2, 0);
                injItem.MoveToContainer(scientist.inventory.containerBelt);
                Invoke(scientist as BasePlayer, "UpdateActiveItem", injItem.uid);
                medToolsUse = true;
                _plugin.timer.Once(_thisData.HealByInjector_UseTime, () =>
                {
                    try
                    {
                        medToolsUse = false;
                        injItem.Remove();
                        defItem?.MoveToContainer(scientist.inventory.containerBelt);
                        (scientist as NPCPlayerApex)?.Resume();
                        scientist.health += _thisData.HealByInjector_Health;
                        if (defItem != null) Invoke(scientist as BasePlayer, "UpdateActiveItem", defItem.uid);
                    }
                    catch
                    {

                    }
                });
            }
            static void Invoke<T>(T obj, string method, params object[] param) => typeof(T).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(obj, param);
        }
        private class AnimalEditor: MonoBehaviour
        {
            public BaseAnimalNPC animal => GetComponent<BaseAnimalNPC>();
            public DataAnimalJson _thisData => _config.GetJson(animal) ?? default(DataAnimalJson);
            public void Init()
            {
                animal.startHealth = _thisData.MaxHealth;
                SetValue(animal, "_maxHealth", _thisData.MaxHealth);
                SetValue(animal, "_health", _thisData.MaxHealth);
            }
        }
        static void Invoke<T>(T obj, string method, params object[] param) => typeof(T).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(obj, param);
        static V InvokeValue<T, V>(T obj, string method, params object[] param) => (V)typeof(T).GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Invoke(obj, param);
        static void SetValue<T>(T obj, string field, object value) => typeof(T).GetField(field, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).SetValue(obj, value);
        #endregion
    }
}