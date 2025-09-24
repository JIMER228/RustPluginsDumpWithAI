// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Oxide.Core;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Facepunch;
using Rust;
using Rust.Ai;

#region Changelogs and ToDo

/**********************************************************************
* 
*   1.0.0   -   Cleanup and release ready
*   1.0.1   -   Nulled cactus damage to npc
*   1.0.2   -   Simplified Kit handout code
*           -   Fix for console NRE on ScarecrowNPC spawning since lates rust update
*           -   Added scarecrow deathsound
*   1.0.3   -   patched for nexttick nre
*           -   Added turret targeting to ignore junkpile NPC
*           -   Nulled BradleyAPC damage to npc
*           -   Using Native Junkpile class instead of prefab check
*           -   Code cleanup
*           -   Added support for BetterNpcNames
*           -   Temp removed chainsaw npc naming
*   1.0.4   -   Fix for compile issues
* 
**********************************************************************/

#endregion

namespace Oxide.Plugins
{
    [Info("JunkpileNPC", "Krungh Crow", "1.0.4")]
    [Description("Brings back junkpile scientists and even junkpile scarecrows")]
    class JunkpileNPC : RustPlugin
    {
        [PluginReference]
        private readonly Plugin BetterNpcNames, Kits;

        #region Variables

        #region Plugin
        bool BlockSpawn;
        System.Random rnd = new System.Random();
        private ConfigData configData;
        public static JunkpileNPC instance;

        #endregion

        #region NPC
        const string zombie = "assets/prefabs/npc/scarecrow/scarecrow.prefab";
        const string _scientist = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_junkpile_pistol.prefab";
        const string _CrowDeathsound = "assets/prefabs/npc/murderer/sound/death.prefab";
        float _DamageScale;
        int _Health;
        List<string> _NPCKit;
        float _LifeTime;
        string _NPCName;

        bool _UseKit;
        #endregion

        #endregion

        #region Behaviours

        #region Scarecrow
        private class Zombies : FacepunchBehaviour
        {
            private ScarecrowNPC npc;
            public bool ReturningToHome = false;
            public bool isRoaming = true;
            public Vector3 SpawnPoint;

            private void Awake()
            {
                npc = GetComponent<ScarecrowNPC>();
                Invoke(nameof(_UseBrain), 0.1f);
                InvokeRepeating(Attack, 0.1f, 1.5f);
                InvokeRepeating(GoHome, 8.0f, 10.0f);
            }

            private void Attack()
            {
                BaseEntity entity = npc.Brain.Senses.GetNearestThreat(40);
                Chainsaw heldEntity = npc.GetHeldEntity() as Chainsaw;
                if (entity == null || Vector3.Distance(entity.transform.position, npc.transform.position) > 40.0f)
                {
                    npc.Brain.Navigator.ClearFacingDirectionOverride();
                    GoHome();
                }
                if (entity != null && Vector3.Distance(entity.transform.position, npc.transform.position) < 2.0f)
                {
                    npc.StartAttacking(entity);
                    npc.Brain.Navigator.SetFacingDirectionEntity(entity);
                    if (heldEntity is Chainsaw)
                    {
                        if (!(heldEntity as Chainsaw).EngineOn())
                        {
                            (heldEntity as Chainsaw).ServerNPCStart();
                        }
                        heldEntity.SetFlag(BaseEntity.Flags.Busy, true, false, true);
                        heldEntity.SetFlag(BaseEntity.Flags.Reserved8, true, false, true);
                    }
                }

                if (entity != null && Vector3.Distance(entity.transform.position, npc.transform.position) > 1.5f)
                {
                    if (heldEntity is Chainsaw)
                    {

                        if (!(heldEntity as Chainsaw).EngineOn())
                        {
                            (heldEntity as Chainsaw).ServerNPCStart();
                        }
                        heldEntity.SetFlag(BaseEntity.Flags.Busy, false, false, true);
                        heldEntity.SetFlag(BaseEntity.Flags.Reserved8, false, false, true);
                    }
                }

                if (entity != null && Vector3.Distance(entity.transform.position, npc.transform.position) > 2.0f)
                {
                    npc.Brain.Navigator.SetFacingDirectionEntity(entity);
                }
            }

            public void _UseBrain()
            {
                #region navigation
                npc.Brain.Navigator.Agent.agentTypeID = -1372625422;
                npc.Brain.Navigator.DefaultArea = "Walkable";
                npc.Brain.Navigator.Agent.autoRepath = true;
                npc.Brain.Navigator.enabled = true;
                npc.Brain.Navigator.CanUseNavMesh = true;
                npc.Brain.Navigator.BestRoamPointMaxDistance = instance.configData.NPCData.NPCRoamMax;
                npc.Brain.Navigator.MaxRoamDistanceFromHome = instance.configData.NPCData.NPCRoamMax;
                npc.Brain.Navigator.Init(npc, npc.Brain.Navigator.Agent);
                npc.Brain.Navigator.SetDestination(SpawnPoint, BaseNavigator.NavigationSpeed.Slow, 0f, 0f);
                #endregion

                #region senses & Targeting
                npc.Brain.ForceSetAge(0);
                npc.Brain.AllowedToSleep = false;
                npc.Brain.sleeping = false;
                npc.Brain.SenseRange = 30f;
                npc.Brain.ListenRange = 40f;
                npc.Brain.Senses.Init(npc,npc.Brain, 5f, 30, 40f, 135f, true, true, true, 60f, false, false, true, EntityType.Player, true);
                npc.Brain.TargetLostRange = 20f;
                npc.Brain.HostileTargetsOnly = false;
                npc.Brain.IgnoreSafeZonePlayers = true;
                #endregion
            }

            void GoHome()
            {
                if (npc == null || npc.IsDestroyed || npc.isMounted)
                    return;

                if (!npc.HasBrain)
                    return;
                if (npc.Brain.Senses.Memory.Targets.Count > 0)
                {
                    for (var i = 0; i < npc.Brain.Senses.Memory.Targets.Count; i++)
                    {
                        BaseEntity target = npc.Brain.Senses.Memory.Targets[i];
                        BasePlayer player = target as BasePlayer;

                        if (target == null || !player.IsAlive() || player.IsSleeping() || player.IsFlying)
                        {
                            WipeMemory();
                            ReturningToHome = true;
                            isRoaming = false;
                            return;
                        }

                        if (player.InSafeZone() || player.HasPlayerFlag(BasePlayer.PlayerFlags.SafeZone))
                        {
                            WipeMemory();
                            ReturningToHome = true;
                            isRoaming = false;
                            return;
                        }

                        if (npc.Distance(player.transform.position) > 25f)
                        {
                            WipeMemory();
                            ReturningToHome = true;
                            isRoaming = false;
                            return;
                        }
                        if (player.IsSleeping() || player.IsFlying)
                        {
                            WipeMemory();
                            ReturningToHome = true;
                            isRoaming = false;
                            return;
                        }

                    }
                }

                var distanceHome = Vector3.Distance(npc.transform.position, SpawnPoint);
                if (ReturningToHome == false)
                {
                    if (isRoaming == true && distanceHome > instance.configData.NPCData.NPCRoamMax)
                    {
                        ReturningToHome = true;
                        isRoaming = false;
                        return;
                    }
                    if (isRoaming == true && distanceHome < instance.configData.NPCData.NPCRoamMax)
                    {
                        Vector3 random = UnityEngine.Random.onUnitSphere.normalized * instance.configData.NPCData.NPCRoamMax;
                        Vector3 newPos = instance.GetNavPoint(SpawnPoint + new Vector3(random.x, 0f, random.y));
                        SettargetDestination(newPos);
                        return;
                    }
                }
                if (ReturningToHome && distanceHome > 2)
                {
                    if (npc.Brain.Navigator.Destination == SpawnPoint)
                    {
                        return;
                    }

                    WipeMemory();
                    SettargetDestination(SpawnPoint);
                    return;
                }
                ReturningToHome = false;
                isRoaming = true;
            }

            private void SettargetDestination(Vector3 position)
            {
                npc.Brain.Navigator.Destination = position;
                npc.Brain.Navigator.SetDestination(position, BaseNavigator.NavigationSpeed.Slowest, 0f, 0f);
            }

            void WipeMemory()
            {
                if (!npc.HasBrain)
                {
                    return;
                }

                npc.Brain.Senses.Players.Clear();
                npc.Brain.Senses.Memory.Players.Clear();
                npc.Brain.Senses.Memory.Targets.Clear();
                npc.Brain.Senses.Memory.Threats.Clear();
                npc.Brain.Senses.Memory.LOS.Clear();
                npc.Brain.Senses.Memory.All.Clear();
            }

            void OnDestroy()
            {
                if (npc != null && !npc.IsDestroyed)
                {
                    npc.Kill();
                }
                CancelInvoke(GoHome);
                CancelInvoke(Attack);
                CancelInvoke(nameof(_UseBrain));
            }
        }
        #endregion

        #region Scientist
        public class Scientists : FacepunchBehaviour
        {
            public global::HumanNPC npc;
            public bool ReturningToHome = false;
            public bool isRoaming = true;
            public Vector3 SpawnPoint;

            void Start()
            {
                npc = GetComponent<global::HumanNPC>();

                InvokeRepeating("GoHome", 4.0f, 5.0f);
                Invoke(nameof(_UseBrain), 0.1f);
            }
            public void _UseBrain()
            {
                #region navigation
                npc.Brain.Navigator.Agent.agentTypeID = -1372625422;
                npc.Brain.Navigator.DefaultArea = "Walkable";
                npc.Brain.Navigator.Agent.autoRepath = true;
                npc.Brain.Navigator.enabled = true;
                npc.Brain.Navigator.CanUseNavMesh = true;
                npc.Brain.Navigator.BestRoamPointMaxDistance = instance.configData.SCIData.NPCRoamMax;
                npc.Brain.Navigator.MaxRoamDistanceFromHome = instance.configData.SCIData.NPCRoamMax;
                npc.Brain.Navigator.Init(npc, npc.Brain.Navigator.Agent);
                npc.Brain.Navigator.SetDestination(SpawnPoint, BaseNavigator.NavigationSpeed.Slow, 0f, 0f);
                #endregion

                #region senses & Targeting
                npc.Brain.ForceSetAge(0);
                npc.Brain.AllowedToSleep = false;
                npc.Brain.sleeping = false;
                npc.Brain.SenseRange = 30f;
                npc.Brain.ListenRange = 40f;
                npc.Brain.Senses.Init(npc,npc.Brain, 5f, 50f, 50f, -1f, true, false, true, 60f, false, false, false, EntityType.Player, false);
                npc.Brain.TargetLostRange = 25f;
                npc.Brain.HostileTargetsOnly = false;
                npc.Brain.IgnoreSafeZonePlayers = true;
                #endregion
            }
            void GoHome()
            {
                if (npc == null || npc.IsDestroyed || npc.isMounted)
                    return;

                if (!npc.HasBrain)
                    return;
                if (npc.Brain.Senses.Memory.Targets.Count > 0)
                {
                    for (var i = 0; i < npc.Brain.Senses.Memory.Targets.Count; i++)
                    {
                        BaseEntity target = npc.Brain.Senses.Memory.Targets[i];
                        BasePlayer player = target as BasePlayer;

                        if (target == null || !player.IsAlive() )
                        {
                            WipeMemory();
                            ReturningToHome = true;
                            isRoaming = false;
                            return;
                        }
                        if (player.InSafeZone() || player.HasPlayerFlag(BasePlayer.PlayerFlags.SafeZone))
                        {
                            WipeMemory();
                            ReturningToHome = true;
                            isRoaming = false;
                            return;
                        }

                        if (npc.Distance(player.transform.position) > 40f)
                        {
                            WipeMemory();
                            ReturningToHome = true;
                            isRoaming = false;
                            return;
                        }
                    }
                }

                var distanceHome = Vector3.Distance(npc.transform.position, SpawnPoint);
                if (ReturningToHome == false)
                {
                    if (isRoaming == true && distanceHome > instance.configData.SCIData.NPCRoamMax)
                    {
                        ReturningToHome = true;
                        isRoaming = false;
                        return;
                    }
                    if (isRoaming == true && distanceHome < instance.configData.SCIData.NPCRoamMax)
                    {
                        Vector3 random = UnityEngine.Random.onUnitSphere.normalized * instance.configData.SCIData.NPCRoamMax;
                        Vector3 newPos = instance.GetNavPoint(SpawnPoint + new Vector3(random.x, 0f, random.y));
                        SettargetDestination(newPos);
                        return;
                    }
                }
                if (ReturningToHome && distanceHome > 2)
                {
                    if (npc.Brain.Navigator.Destination == SpawnPoint)
                    {
                        return;
                    }

                    WipeMemory();
                    SettargetDestination(SpawnPoint);
                    return;
                }
                ReturningToHome = false;
                isRoaming = true;
            }
            private void SettargetDestination(Vector3 position)
            {
                npc.Brain.Navigator.Destination = position;
                npc.Brain.Navigator.SetDestination(position, BaseNavigator.NavigationSpeed.Slow, 0f, 0f);
            }
            void WipeMemory()
            {
                if (!npc.HasBrain)
                {
                    return;
                }

                npc.Brain.Senses.Players.Clear();
                npc.Brain.Senses.Memory.Players.Clear();
                npc.Brain.Senses.Memory.Targets.Clear();
                npc.Brain.Senses.Memory.Threats.Clear();
                npc.Brain.Senses.Memory.LOS.Clear();
                npc.Brain.Senses.Memory.All.Clear();
            }

            void OnDestroy()
            {
                if (npc != null && !npc.IsDestroyed)
                {
                    npc.Kill();
                }
                CancelInvoke("GoHome");
                CancelInvoke(nameof(_UseBrain));

            }
        }
        #endregion

        #endregion

        #region Configuration

        class ConfigData
        {
            [JsonProperty(PropertyName = "Scarecrow Settings")]
            public NPCSettings NPCData = new NPCSettings();
            [JsonProperty(PropertyName = "Scientist Settings")]
            public SCISettings SCIData = new SCISettings();
        }

        #region Scarecrow
        class NPCSettings
        {
            [JsonProperty(PropertyName = "Spawn chance (0-100)")]
            public int SpawnRate  = 10;
            [JsonProperty(PropertyName = "Spawn Amount")]
            public int NPCAmount = 1;
            [JsonProperty(PropertyName = "Max Roam Distance")]
            public int NPCRoamMax= 15;
            [JsonProperty(PropertyName = "Prefix (Title)")]
            public string NPCName = "Scarecrow";
            [JsonProperty(PropertyName = "Health (HP)")]
            public int NPCHealth = 250;
            [JsonProperty(PropertyName = "Life Duration (minutes)")]
            public float NPCLife = 30f;
            [JsonProperty(PropertyName = "Damage multiplier")]
            public float NPCDamageScale = 0.6f;
            [JsonProperty(PropertyName = "Use kit (clothing)")]
            public bool UseKit = false;
            [JsonProperty(PropertyName = "Kit ID")]
            public List<string> KitName = new List<string>();
        }
        #endregion

        #region Scientist
        class SCISettings
        {
            [JsonProperty(PropertyName = "Spawn chance (0-100)")]
            public int SpawnRate = 10;
            [JsonProperty(PropertyName = "Spawn Amount")]
            public int NPCAmount = 1;
            [JsonProperty(PropertyName = "Max Roam Distance")]
            public int NPCRoamMax = 15;
            [JsonProperty(PropertyName = "Prefix (Title)")]
            public string NPCName = "Scientist";
            [JsonProperty(PropertyName = "Health (HP)")]
            public int NPCHealth = 250;
            [JsonProperty(PropertyName = "Life Duration (minutes)")]
            public float NPCLife = 30f;
            [JsonProperty(PropertyName = "Damage multiplier")]
            public float NPCDamageScale = 0.6f;
            [JsonProperty(PropertyName = "Use kit (clothing)")]
            public bool UseKit = false;
            [JsonProperty(PropertyName = "Kit ID")]
            public List<string> KitName = new List<string>();
        }
        #endregion

        #region cfg save/load
        private bool LoadConfigVariables()
        {
            try
            {
                configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
                return false;
            }
            SaveConf();
            return true;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Fresh install detected Creating a new config file.");
            configData = new ConfigData();
            SaveConf();
        }

        void SaveConf() => Config.WriteObject(configData, true);
        #endregion

        #endregion

        #region Hooks

        void OnServerInitialized()
        {
            instance = this;
            if (!Kits)
            {
                PrintWarning("Kits plugin not found skipping Kits for Npc's");
            }
        }

        void Unload()
        {
            Zombies[] zombies = UnityEngine.Object.FindObjectsOfType<Zombies>();
            if (zombies != null)
            {
                foreach (Zombies zombie in zombies)
                    UnityEngine.Object.Destroy(zombie);
            }
            Scientists[] scientists = UnityEngine.Object.FindObjectsOfType<Scientists>();
            if (scientists != null)
            {
                foreach (Scientists scientist in scientists)
                    UnityEngine.Object.Destroy(scientist);
            }
        }

        void Init()
        {
            if (!LoadConfigVariables())
            {
                Puts("Config file issue detected. Please delete file, or check syntax and fix.");
                return;
            }
        }

        void OnEntitySpawned(JunkPile _Junkpile)
        {
            if (_Junkpile == null || _Junkpile is JunkPileWater) return;

            #region scientists
            if (SpawnRate(configData.SCIData.SpawnRate) == true)
            {
                timer.Once(1f, () =>
                {
                    for (int i = 0; i < configData.SCIData.NPCAmount; i++)
                    {
                        _Health = configData.SCIData.NPCHealth;
                        _DamageScale = configData.SCIData.NPCDamageScale;
                        _LifeTime = configData.SCIData.NPCLife;
                        _NPCName = configData.SCIData.NPCName;
                        _UseKit = configData.SCIData.UseKit;
                        _NPCKit = configData.SCIData.KitName;
                        SpawnScientist(_Junkpile.transform.position);
                    }
                });
            }
            #endregion

            #region Scarecrow
            if (SpawnRate(configData.NPCData.SpawnRate) == true)
            {
                timer.Once(1f, () =>
                {
                    for (int i = 0; i < configData.NPCData.NPCAmount; i++)
                    {
                        _Health = configData.NPCData.NPCHealth;
                        _DamageScale = configData.NPCData.NPCDamageScale;
                        _LifeTime = configData.NPCData.NPCLife;
                        _NPCName = configData.NPCData.NPCName;
                        _UseKit = configData.NPCData.UseKit;
                        _NPCKit = configData.NPCData.KitName;
                        Spawnnpc(_Junkpile.transform.position);

                    }
                });
            }
            #endregion
        }

        object OnNpcKits(BasePlayer player)
        {
            if (player?.gameObject?.GetComponent<Zombies>() != null)
                return true;
            if (player?.gameObject?.GetComponent<Scientists>() != null)
                return true;
            return null;
        }

        private object OnNpcTarget(BaseEntity attacker, BaseEntity target)
        {
            if (attacker?.gameObject?.GetComponent<Zombies>() || attacker?.gameObject?.GetComponent<Scientists>())
            {
                if (target is ScarecrowNPC || target is BaseNpc) return true;
                if (target is TunnelDweller || target is UnderwaterDweller) return true;
                return null;
            }
            if (target?.gameObject?.GetComponent<Zombies>() || target?.gameObject?.GetComponent<Scientists>()) return true;
            return null;
        }

        private object OnNpcTarget(BaseEntity attacker, BasePlayer target)
        {
            if (attacker?.gameObject?.GetComponent<Zombies>() || attacker?.gameObject?.GetComponent<Scientists>())
            {
                if (target.InSafeZone() || target.HasPlayerFlag(BasePlayer.PlayerFlags.SafeZone)) return true;
                if (target.IsSleeping() || target.IsFlying || !(target.userID.IsSteamId())) return true;
            }
            return null;
        }

        void OnEntityDeath(BasePlayer scarecrow, HitInfo info)
        {
            if (scarecrow?.gameObject?.GetComponent<Zombies>() && scarecrow != null) Effect.server.Run(_CrowDeathsound, scarecrow, 0, Vector3.zero, scarecrow.eyes.transform.forward.normalized);
        }

        object OnEntityTakeDamage(BasePlayer entity, HitInfo info)
        {
            if(entity?.gameObject?.GetComponent<Zombies>() || entity?.gameObject?.GetComponent<Scientists>())
            {
                if (info.Initiator?.ToString() == null || info.Initiator.ToString().Contains("cactus") || info.Initiator is BradleyAPC) return true;
            }
            return null;
        }

        private object OnTurretTarget(AutoTurret turret, BaseEntity target)
        {
            if (target != null && (target?.gameObject?.GetComponent<Zombies>() || target?.gameObject?.GetComponent<Scientists>())) return true;
            return null;
        }

        #endregion

        #region Event Helpers

        #region Nav & Checks
        public Vector3 GetNavPoint(Vector3 position)
        {
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(position, out hit, 5, -1))
            {
                return position;
            }
            else if (Physics.RaycastAll(hit.position + new Vector3(0, 100, 0), Vector3.down, 99f, 1235288065).Any())
            {
                return position;
            }
            else if (hit.position.y < TerrainMeta.WaterMap.GetHeight(hit.position))
            {
                return position;
            }
            position = hit.position;
            return position;
        }

        private bool SpawnRate(int npcRate)
        {
        if (rnd.Next(1, 101) <= npcRate)
            {
                return true;
            }
        return false;
        }

        private bool CheckPlayer(HitInfo info)
        {
            bool Checker = false;
            BasePlayer player = info.InitiatorPlayer;
            if (player != null || player.IsValid() || info?.Initiator != null)
            {
                Checker = true;
            }
            return Checker;
        }
        #endregion

        #region Scarecrow
        private void Spawnnpc(Vector3 position)
        {
            Vector3 pos = position + UnityEngine.Random.onUnitSphere * 1.5f;
            pos.y = TerrainMeta.HeightMap.GetHeight(pos);

            ScarecrowNPC npc = (ScarecrowNPC)GameManager.server.CreateEntity(zombie, pos, new Quaternion(), true);
            npc.Spawn();
            if (npc.IsHeadUnderwater())
            {
                npc.Kill();
                return;
            }

            if (npc == null) return;

            var mono = npc.gameObject.AddComponent<Zombies>();
            mono.SpawnPoint = pos;

            npc.startHealth = _Health;
            npc.InitializeHealth(_Health, _Health);
            npc.CanAttack();
            npc.displayName = _NPCName + " " + RandomUsernames.Get((int)npc.userID);
            if (BetterNpcNames != null) npc.displayName = BetterNpcNames.Call<string>("GiveName", npc, configData.NPCData.NPCName, true, true);

            npc.damageScale = _DamageScale;
            var inv_wear = npc.inventory.containerWear;
            var inv_belt = npc.inventory.containerBelt;

            if (Kits && _UseKit && _NPCKit.Count > 0)
            {
                object checkKit = Kits?.CallHook("GetKitInfo", _NPCKit[new System.Random().Next(_NPCKit.Count())]);
                if (checkKit == null) PrintWarning($"Kit for {npc} does not exist - Using a default outfit.");
                else
                {
                    npc.inventory.containerWear.Clear();
                    npc.inventory.containerBelt.Clear();
                    Kits?.Call($"GiveKit", npc, _NPCKit[new System.Random().Next(_NPCKit.Count())]);
                }
            }
            else
            {
                Item eyes = ItemManager.CreateByName("gloweyes", 1, 0);
                if (eyes != null) eyes.MoveToContainer(inv_wear);
            }

            timer.Once(_LifeTime * 60, () =>
            {
                if (npc != null) npc.Kill();
            });
        }
        #endregion

        #region Scientist
        private void SpawnScientist(Vector3 position)
        {
            Vector3 pos = position + UnityEngine.Random.onUnitSphere * 1.5f;
            pos.y = TerrainMeta.HeightMap.GetHeight(pos);

            NPCPlayer npc = (NPCPlayer)GameManager.server.CreateEntity(_scientist, pos, new Quaternion(), true);
            npc.Spawn();
            if (npc.IsHeadUnderwater())
            {
                npc.Kill();
                return;
            }

            if (npc == null) return;

            var mono = npc.gameObject.AddComponent<Scientists>();
            mono.SpawnPoint = pos;

            npc.startHealth = _Health;
            npc.InitializeHealth(_Health, _Health);
            npc.CanAttack();
            npc.displayName = _NPCName + " " + RandomUsernames.Get((int)npc.userID);
            if (BetterNpcNames != null) npc.displayName = BetterNpcNames.Call<string>("GiveName", npc, configData.SCIData.NPCName, true, true);

            npc.damageScale = _DamageScale;

            if (Kits && _UseKit && _NPCKit.Count > 0)
            {
                object checkKit = Kits?.CallHook("GetKitInfo", _NPCKit[new System.Random().Next(_NPCKit.Count())]);
                if (checkKit == null) PrintWarning($"Kit for {npc} does not exist - Using a default outfit.");
                else
                {
                    npc.inventory.Strip();
                    Kits?.Call($"GiveKit", npc, _NPCKit[new System.Random().Next(_NPCKit.Count())]);
                }
            }
            timer.Once(_LifeTime * 60, () =>
            {
                if (npc != null) npc.Kill();
            });
        }

        #endregion

        #endregion
    }
}