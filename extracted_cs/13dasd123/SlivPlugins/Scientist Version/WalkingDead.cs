// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Facepunch;
using Rust;
using Rust.Ai;


#region Changelogs and ToDo
/*************************************************************
* 
* Thx Steenamaroo for the coding assistance :)
*
* 1.0.9     :   Added additional checks
*           :   Possible fix for msg nre in console
* 1.0.10    :   Added check for chat message timer (hopefully fixes the nre)
* 1.0.11    :   Removed Gametip messages
* 1.0.12    :   Walkers will not spawn on foundations
* 1.1.0     :   Walkers dont attack animals/npc true/false to config
* 1.1.1     :   Fix for NRE
* 1.1.2     :   Added ResourceId for dev API
*           :   Added simple debug through cfg settings
*           :   Possible fix for NextTick issue
* 1.1.3     :   Fix for Kits NRE
* 1.1.4     :   Added option to block spawn if player suicided
*           :   Added test for single zoneblock
* 1.1.5     :   Added watercheck and suicide when walking under water
*           :   Improved NPC targetinging eachother
* 1.1.6     :   Patched for December 2nd rust update
*           :   Removed murderer which is removed by Facepunch
*           :   NPC are now scientists (working on mellee part)
*           :   Fixed walkers not spawning with suit if no clothing or kits
*           :   Walkers now wield a eoka waterpipe or double barrel by default
*           :   Fixed oxide mixing up HumanNPC plugin(same name) and npc type
* 1.2.0     :   Improved the scientist behaviour and roaming
            :   switched from junkpile to roam scientist
* 1.2.1     :   Added safezonecheck (including custom safezone triggers)
*           :   Zones (old test) is now a list
* 1.2.2     :   Fix for compile issues
* 
**************************************************************/
#endregion

namespace Oxide.Plugins
{
    [Info("Walking Dead", "Krungh Crow", "1.2.2", ResourceId = 280)]
    [Description("Spawns a real zombie after you die")]

    class WalkingDead : RustPlugin
    {
        [PluginReference] Plugin Kits, ZoneManager;

        #region Variables
        Dictionary<string, Timer> CoolDowns = new Dictionary<string, Timer>();
        public static WalkingDead instance;

        const string zombie = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roam.prefab";
        const string fireball1 = "assets/bundled/prefabs/fireball.prefab";
        const ulong chaticon = 76561199087926121;
        bool Debug = false;

        #endregion

        #region Behaviour
        void Unload()
        {
            Walkers[] walkers = UnityEngine.Object.FindObjectsOfType<Walkers>();
            if (walkers != null)
            {
                foreach (Walkers walker in walkers)
                    UnityEngine.Object.Destroy(walker);
                if (Debug) Puts($"[Debug] All Walkers where destroyed on Plugin Un/Reload");
            }
        }
        public class Walkers : FacepunchBehaviour
        {
            public global::HumanNPC npc;
            public bool ReturningToHome = false;
            public bool isRoaming = true;
            public Vector3 SpawnPoint;

            void Start()
            {
                npc = GetComponent<global::HumanNPC>();

                InvokeRepeating("GoHome", 2.0f, 4.5f);
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
                npc.Brain.Navigator.BestRoamPointMaxDistance = instance.configData.ZombieData.ZombieRoam;
                npc.Brain.Navigator.MaxRoamDistanceFromHome = instance.configData.ZombieData.ZombieRoam;
                npc.Brain.Navigator.Init(npc, npc.Brain.Navigator.Agent);
                npc.Brain.Navigator.SetDestination(SpawnPoint, BaseNavigator.NavigationSpeed.Slow, 0f, 0f);
                #endregion

                #region senses & Targeting
                npc.Brain.ForceSetAge(0);
                npc.Brain.AllowedToSleep = false;
                npc.Brain.sleeping = false;
                npc.Brain.SenseRange = 30f;
                npc.Brain.ListenRange = 40f;
                npc.Brain.Senses.Init(npc, npc.Brain, 5f, 140f, 140f, -1f, true, false, true, 60f, false, false, false, EntityType.Player, false);
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

                        if (target == null || !player.IsAlive())
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
                    if (isRoaming == true && distanceHome > instance.configData.ZombieData.ZombieRoam)
                    {
                        ReturningToHome = true;
                        isRoaming = false;
                        return;
                    }
                    if (isRoaming == true && distanceHome < instance.configData.ZombieData.ZombieRoam)
                    {
                        Vector3 random = UnityEngine.Random.insideUnitCircle.normalized * instance.configData.ZombieData.ZombieRoam;
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

        #region Configuration
        void Init()
        {
            if (!LoadConfigVariables())
            {
                Puts("Config file issue detected. Please delete file, or check syntax and fix.");
                return;
            }
            Debug = configData.PlugCFG.Debug;
            if (Debug) Puts($"[Debug] is activated");
        }

        private ConfigData configData;

        class ConfigData
        {
            [JsonProperty(PropertyName = "Settings Plugin")]
            public SettingsPlugin PlugCFG = new SettingsPlugin();
            [JsonProperty(PropertyName = "Settings Player")]
            public SettingsPlayer PlayerCFG = new SettingsPlayer();
            [JsonProperty(PropertyName = "Zombie Settings")]
            public SettingsZombie ZombieData = new SettingsZombie();
            [JsonProperty(PropertyName = "Zombie Targeting")]
            public SettingsTarget ZombieTarget = new SettingsTarget();
        }

        class SettingsPlugin
        {
            [JsonProperty(PropertyName = "Debug")]
            public bool Debug = false;
        }

        class SettingsPlayer
        {
            [JsonProperty(PropertyName = "Suicide block")]
            public bool SuicideBlock = false;
            [JsonProperty(PropertyName = "Zone block")]
            public bool ZoneBlock = false;
            [JsonProperty(PropertyName = "Zone ID's")]
            public List<string> ZoneID = new List<string>();
        }

        class SettingsZombie
        {
            [JsonProperty(PropertyName = "Zombie spawn delay (seconds)")]
            public int SpawnTime = 5;
            [JsonProperty(PropertyName = "Zombie spawn cooldown (seconds)")]
            public int Cooldown = 300;
            [JsonProperty(PropertyName = "Zombie Show cooldown chat messages")]
            public bool ShowCooldownMsg = false;
            [JsonProperty(PropertyName = "Zombie Prefix Title")]
            public string ZombiePrefix = "Walker";
            [JsonProperty(PropertyName = "Zombie spawn amount")]
            public int SpawnAmount = 1;
            [JsonProperty(PropertyName = "Zombie Health")]
            public int ZombieHealth = 250;
            [JsonProperty(PropertyName = "Zombie spawn radius")]
            public int Radius = 5;
            [JsonProperty(PropertyName = "Zombie Max Roam Distance")]
            public int ZombieRoam = 20;
            [JsonProperty(PropertyName = "Zombie Damage multiplier")]
            public float ZombieDamageScale = 0.6f;
            [JsonProperty(PropertyName = "Zombie Lifetime (minutes)")]
            public float ZombieLife = 30f;
            [JsonProperty(PropertyName = "Zombie Spawns on fire")]
            public bool FromHell = false;
            [JsonProperty(PropertyName = "Zombie Kit ID")]
            public List<string> KitName = new List<string>();
            [JsonProperty(PropertyName = "Zombie Show chat messages")]
            public bool ShowMsg = false;
        }

        class SettingsTarget
        {
            [JsonProperty(PropertyName = "Zombie Can target other npc")]
            public bool TargetNPC = false;
            [JsonProperty(PropertyName = "Zombie Can target animals")]
            public bool TargetAnimal = false;
        }


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
            Puts("Creating new config file.");
            configData = new ConfigData();
            SaveConf();
        }

        void SaveConf() => Config.WriteObject(configData, true);
        #endregion

        #region LanguageAPI
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Cooldown"] = "You have a cooldown and didnt ressurect a walker!!",
                ["Zombie_Spawn"] = "You died and resurrected a Walker!",
                ["Prefix"] = "<color=green>Walking Dead : </color>",
                ["info"] = "\nThe [Walking Dead] will resurrect players as Walkers\nThese are aggressive and attack anything on sight !!\nUsing their melee weapons they can and will go after you !!",
            }, this);
        }

        #endregion

        #region Commands

        [ChatCommand("wdinfo")]
        void cmdwdversion(BasePlayer player, string cmd, string[] args)
        {
            string prefix = lang.GetMessage("Prefix", this);
            {
                Player.Message(player, prefix + string.Format(msg("Current Version v", player.UserIDString)) + this.Version.ToString() + " By : " + this.Author.ToString() + msg("info"), chaticon);
            }
        }

        #endregion

        #region Hooks
        void OnServerInitialized()
        {
            instance = this;

            if (ZoneManager)
            {
                Puts("Check for [ZoneManager] dependency : Installed");
            }
            else
            {
                Puts("Check for [ZoneManager] dependency : Not Installed");
            }

        }

        private object OnNpcTarget(BaseEntity attacker, BaseEntity target)
        {
            if (attacker?.gameObject?.GetComponent<Walkers>())
            {
                if (target is NPCPlayer && configData.ZombieTarget.TargetNPC == false)
                {
                    if (Debug) Puts($"[Debug] NPC targeting is false ignore NPC {target}");
                    return true;
                }

                if (target is TunnelDweller || target is UnderwaterDweller && configData.ZombieTarget.TargetNPC == false)
                {
                    if (Debug) Puts($"[Debug] NPC targeting is false ignoring Dweller {target}");
                    return true;
                }

                if (target is BaseNpc && (configData.ZombieTarget.TargetAnimal == false))
                {
                    if (Debug) Puts($"[Debug] Animal targeting is false ignoring {target}");
                    return true;
                }
                return null;

                if (target?.gameObject?.GetComponent<Walkers>() && (attacker.IsNpc || attacker is NPCPlayer))
                {
                    return true;
                }
                return null;
            }
            return null;
        }

        private object OnNpcTarget(BaseEntity attacker, BasePlayer target)
        {
            if (attacker?.gameObject?.GetComponent<Walkers>())
            {
                if (target.InSafeZone() || target.HasPlayerFlag(BasePlayer.PlayerFlags.SafeZone)) return true;
                if (target.IsSleeping() || target.IsFlying || !(target.userID.IsSteamId())) return true;
            }
            return null;
        }

        object OnNpcKits(BasePlayer player)
        {
            if (player?.gameObject?.GetComponent<Walkers>() != null)
                return true;
            return null;
        }

        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player.userID.IsSteamId())
            {
                if (!ZoneManager)
                {
                    if (Debug) Puts($"[Debug] Zonemanager not installed skipping ZoneCheck");
                }
                else
                {
                    var zoneIds = ZoneManager?.Call("GetZoneIDs") as string[];

                    if (zoneIds == null)
                    {
                        return;
                    }

                    bool IsInZone = false;

                    foreach (string zoneId in zoneIds)

                    {
                        if ((bool)ZoneManager?.Call("isPlayerInZone", zoneId, player) && zoneId.Contains(configData.PlayerCFG.ZoneID.ToString()) && configData.PlayerCFG.ZoneBlock)
                        {
                            IsInZone = true;
                            if (Debug) Puts($"[Debug] {player} is in excluded Zone:[{zoneId}] skipping Walker spawn");
                            return;
                        }
                    }
                }
                if (player.InSafeZone() || player.HasPlayerFlag(BasePlayer.PlayerFlags.SafeZone))
                {
                    if (Debug) Puts($"[Debug] {player} was in a safezone skipping walker spawn");
                    return;
                }
                if (configData.PlayerCFG.SuicideBlock && info != null && info.damageTypes.GetMajorityDamageType() == Rust.DamageType.Suicide)
                {
                    if (Debug) Puts($"[Debug] {player} was suicideblocked skipping walker spawn");
                    return;
                }

                if (CoolDowns.ContainsKey(player.UserIDString))
                {
                    if (configData.ZombieData.ShowCooldownMsg == true)
                    {
                        if (Debug) Puts($"[Debug] Player had cooldown message triggered");
                        string prefix = lang.GetMessage("Prefix", this);
                        Player.Message(player, prefix + string.Format(msg("Cooldown", player.UserIDString)), chaticon);
                    }
                    return;
                }
                else
                {
                    string id = player.UserIDString;
                    Timer Cooldown = timer.Once((float)configData.ZombieData.Cooldown, () =>
                    {
                        if (Debug) Puts($"[Debug] {player} cooldown ended");
                        CoolDowns.Remove(id);
                    });

                    CoolDowns.Add(id, Cooldown);
                    if (Debug) Puts($"[Debug] {player} cooldown started");
                }

                var loc = player.transform.position;
                string name = player.displayName;

                List<ClothingItems> clothes = new List<ClothingItems>();

                foreach (var item in player.inventory.containerWear.itemList)
                {
                    clothes.Add(new ClothingItems() { ItemId = item.info.itemid, SkinId = item.skin });
                }

                timer.Once(configData.ZombieData.SpawnTime, () =>
                {
                    for (int i = 0; i < configData.ZombieData.SpawnAmount; i++)
                        Spawnnpc(loc, name, clothes);
                    if (Debug) Puts($"[Debug] Walker ({name}) spawned at {loc}");
                });

                if (configData.ZombieData.ShowMsg)
                {
                    if (!player.userID.IsSteamId())
                    {
                        return;
                    }

                    if (player == null)
                    {
                        return;
                    }

                    if (player != null) ;
                    {
                        string prefix = lang.GetMessage("Prefix", this);
                        Player.Message(player, prefix + string.Format(msg("Zombie_Spawn", player.UserIDString)), chaticon);
                        if (Debug) Puts($"[Debug] {player} recieved a spawn message");
                    }

                }
            }
        }

        #endregion

        #region Event Helpers

        public class ClothingItems
        {
            public int ItemId;
            public ulong SkinId;
        }
         
        private void Spawnnpc(Vector3 pos, string name, List<ClothingItems> clothes)
        {
            int radius = configData.ZombieData.Radius;
            int x = UnityEngine.Random.Range(-radius, radius);
            int z = UnityEngine.Random.Range(-radius, radius);
            pos += new Vector3(x, 0.1f, z);

            NPCPlayer npc = (NPCPlayer)GameManager.server.CreateEntity(zombie, pos, new Quaternion(), true);
            npc.Spawn();
            if (npc == null)
                return;
            (npc as ScientistNPC).radioChatterType = ScientistNPC.RadioChatterType.NONE;

            var inv_wear = npc.inventory.containerWear;
            var inv_belt = npc.inventory.containerBelt;

            NextTick(() =>
            {
                if (npc == null)
                    return;

                var mono = npc.gameObject.AddComponent<Walkers>();
                mono.SpawnPoint = pos;

                string Zombieprefix = configData.ZombieData.ZombiePrefix;

                int HealtH = configData.ZombieData.ZombieHealth;

                npc.startHealth = HealtH;
                npc.InitializeHealth(HealtH, HealtH);
                npc.displayName = Zombieprefix + (" ") + name;

                if (configData.ZombieData.FromHell)
                {
                    var Fire = GameManager.server.CreateEntity(fireball1, new Vector3(0, 1, 0), Quaternion.Euler(0, 0, 0));
                    Fire.gameObject.Identity();
                    Fire.SetParent(npc);
                    Fire.Spawn();
                    if (Debug) Puts($"[Debug] Walker ({name}) spawned From Hell (on Fire)");
                    timer.Once(1f, () =>
                    {
                        if (Fire != null) Fire.Kill();
                    });
                }
                npc.damageScale = configData.ZombieData.ZombieDamageScale;
                //(npc as ScientistNPC).radioChatterType = ScientistNPC.RadioChatterType.NONE;

                if (clothes.Count != 0)
                {
                    foreach (var item in clothes)
                    {
                    var item1 = ItemManager.CreateByItemID(item.ItemId, 1, item.SkinId);
                    if (item1 == null)
                    Puts("Failed to create item.");
                    if (!item1.MoveToContainer(npc.inventory.containerWear))
                        {
                            Puts("Failed to move item to zombie inventory.");
                            item1.Remove();
                        }
                    }
                    inv_belt.Clear();
                    switch (UnityEngine.Random.Range(0, 3))
                    {
                        case 0:
                            ItemManager.CreateByName("shotgun.waterpipe", 1, 0).MoveToContainer(inv_belt, 0);
                            break;
                        case 1:
                            ItemManager.CreateByName("shotgun.double", 1, 0).MoveToContainer(inv_belt, 0);
                            break;
                        default:
                            ItemManager.CreateByName("pistol.eoka", 1, 0).MoveToContainer(inv_belt, 0);
                            break;
                    }

                }
                else if (clothes.Count == 0 && configData.ZombieData.KitName.Count > 0)
                {
                    object checkKit = Kits?.CallHook("GetKitInfo", configData.ZombieData.KitName[new System.Random().Next(configData.ZombieData.KitName.Count())]);
                    if (checkKit == null)
                    {
                        Item outfit = ItemManager.CreateByName("halloween.mummysuit", 1, 0);
                        Item eyes = ItemManager.CreateByName("gloweyes", 1, 0);

                        inv_wear.Clear();
                        inv_belt.Clear();

                        if (outfit != null) outfit.MoveToContainer(inv_wear);
                        if (eyes != null) eyes.MoveToContainer(inv_wear);
                        switch (UnityEngine.Random.Range(0, 3))
                        {
                            case 0:
                                ItemManager.CreateByName("shotgun.waterpipe", 1, 0).MoveToContainer(inv_belt, 0);
                                break;
                            case 1:
                                ItemManager.CreateByName("shotgun.double", 1, 0).MoveToContainer(inv_belt, 0);
                                break;
                            default:
                                ItemManager.CreateByName("pistol.eoka", 1, 0).MoveToContainer(inv_belt, 0);
                                break;
                        }
                    }
                    else
                    {
                        npc.inventory.Strip();
                        Kits?.Call($"GiveKit", npc, configData.ZombieData.KitName[new System.Random().Next(configData.ZombieData.KitName.Count())]);
                    }
                }
                else
                {
                    Item outfit = ItemManager.CreateByName("halloween.mummysuit", 1, 0);
                    Item eyes = ItemManager.CreateByName("gloweyes", 1, 0);

                    inv_wear.Clear();
                    inv_belt.Clear();
                    if (outfit != null) outfit.MoveToContainer(inv_wear);
                    if (eyes != null) eyes.MoveToContainer(inv_wear);
                    switch (UnityEngine.Random.Range(0, 3))
                    {
                        case 0:
                            ItemManager.CreateByName("shotgun.waterpipe", 1, 0).MoveToContainer(inv_belt, 0);
                            break;
                        case 1:
                            ItemManager.CreateByName("shotgun.double", 1, 0).MoveToContainer(inv_belt, 0);
                            break;
                        default:
                            ItemManager.CreateByName("pistol.eoka", 1, 0).MoveToContainer(inv_belt, 0);
                            break;
                    }
                }
            });

            if (npc.IsHeadUnderwater())
            {
                npc.Kill();
                if (Debug) Puts($"[Debug] Walker ({name}) spawned under water killing walker ({name})");
                return;
            }

            if (!npc.IsOutside())
            {
                npc.Kill();
                Puts("walker[" + npc.userID + "] was not spawned beceause it wanted to spawn inside");
                return;
            }

            var id = npc.userID;

            timer.Once(configData.ZombieData.ZombieLife * 60, () =>
            {
                if (npc != null)
                    npc.Hurt(configData.ZombieData.ZombieHealth + 100);

                Puts("Walker [" + id + "] Died of unnatural causes!!!");
            });

        }

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


        #endregion

        #region Message Helper

        private string msg(string key, string id = null) => lang.GetMessage(key, this, id);

        #endregion
    }
}