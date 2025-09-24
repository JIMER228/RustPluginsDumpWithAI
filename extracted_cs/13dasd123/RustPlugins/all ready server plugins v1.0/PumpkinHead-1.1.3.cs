using System;
using System.Linq;
using System.Reflection;
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

namespace Oxide.Plugins
{
    [Info("PumpkinHead", "Krungh Crow", "1.1.3", ResourceId = 1442)]
    [Description("When picking up wild pumpkins or Corn (Pumpkin/Corn)Head can appear")]

    #region Changelogs and ToDo
    /**********************************************************************
     * 1.0.0    :   -   Initial release By request [The Friendly Chap]
     * 1.0.1    :   -   Optimisations and minor Cleanup (thx to the Codefling Team)
     * 1.0.2    :   -   Wrapped corpse injection in a slight delay
     * 1.0.3    :   -   Removed Wounded state to avoid npc looting before it is killed.
     *          :   -   Npc now spawns in a small radius around the pumpkin.
     * 1.0.4    :   -   Added various FX through cfg
     * 1.1.0    :   -   Added CornHead npc and appropriate cfg settings and checks
     * 1.1.1    :   -   Fix for kits not aplying
     * 1.1.2    :   -   Patched for December 2nd rust update
     *              -   Removed murderer which is removed by Facepunch
     *              -   NPC are now scientists (working on mellee part)
     * 1.1.3    :   -   Fixed oxide mixing up HumanNPC plugin(same name) and npc type
     * 
     **********************************************************************/
    #endregion

    class PumpkinHead : RustPlugin
    {
        [PluginReference] Plugin Kits;

        public static Dictionary<BasePlayer, Zombies> SpawnedZombies { get; set; } = new Dictionary<BasePlayer, Zombies>();

        #region Mono
        public static PumpkinHead instance;

        void OnServerInitialized()
        {
            instance = this;
        }

        void Unload()
        {
            if (SpawnedZombies != null)
            {
                foreach (var zombie in SpawnedZombies)
                    if (zombie.Key != null && !zombie.Key.IsDead())
                        UnityEngine.Object.Destroy(zombie.Value);
            }
        }

        public class Zombies : MonoBehaviour
        {
            public global::HumanNPC npc;
            public bool ReturningToHome = false;
            public Vector3 SpawnPoint;
            public bool ignoreSafeZonePlayers = true;

            void Start()
            {
                npc = GetComponent<global::HumanNPC>();

                InvokeRepeating("GoHome", 1f, 1f);
            }
            public void Init()
            {
                npc.Brain.Navigator.Agent.agentTypeID = -1372625422;
                npc.Brain.Navigator.DefaultArea = "Walkable";
                npc.Brain.Navigator.Init(npc, npc.Brain.Navigator.Agent);
                npc.Brain.ForceSetAge(0);
                npc.Brain.TargetLostRange = 30f;
                npc.Brain.HostileTargetsOnly = false;
                npc.Brain.Navigator.BestCoverPointMaxDistance = 20f;//0
                npc.Brain.Navigator.BestRoamPointMaxDistance = 20f;//0
                npc.Brain.Navigator.MaxRoamDistanceFromHome = instance.configData.NPCData.NPCRoam;
                npc.SetDestination(SpawnPoint);
                npc.Brain.Senses.Init(npc, 5f, 60f, 140f, -1f, true, false, true, 60f, false, false, false, EntityType.Player, false);
            }

            void GoHome()
            {
                if (npc == null || npc.IsDestroyed || npc.isMounted)
                    return;

                if (!npc.HasBrain)
                    return;

                if (npc.Distance(SpawnPoint) >= instance.configData.NPCData.NPCRoam)
                {
                    npc.Brain.Senses.Memory.Targets.Clear();
                    npc.Brain.Senses.Memory.Threats.Clear();
                }

                if (npc.Brain.Senses.Memory.Targets.Count != 0)
                    return;

                if (npc.Brain.Navigator.Agent == null || !npc.Brain.Navigator.Agent.isOnNavMesh)
                {
                    npc.Brain.Navigator.Destination = SpawnPoint;
                    npc.SetDestination(SpawnPoint);
                }
                else
                    npc.Brain.Navigator.SetDestination(SpawnPoint);
            }

            void OnDestroy()
            {
                if (npc != null && !npc.IsDestroyed)
                {
                    npc.Kill();
                }
                CancelInvoke();
            }
        }
        #endregion

        #region Variables

        const string Use_Perm = "pumpkinhead.use";
        private readonly List<ulong> _Pin = new List<ulong>();

        private bool Debug;
        bool IsSpawned;
        bool IsSeed = false;
        const string zombie = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_junkpile_pistol.prefab";
        const ulong chaticon = 0;
        private Dictionary<string, List<ulong>> Skins { get; set; } = new Dictionary<string, List<ulong>>();
        string _Node;
        bool CanGive = false;
        bool KitIsValid;
        bool IsPumpkin;
        bool IsCorn;

        #endregion

        #region Configuration
        void Init()
        {
            if (!LoadConfigVariables())
            {
                Puts("Config file issue detected. Please delete file, or check syntax and fix.");
                return;
            }
            Debug = configData.UseDebug;
            permission.RegisterPermission(Use_Perm, this);
            if (Debug) Puts($"[Debug] Debug is active");
        }

        private ConfigData configData;

        class ConfigData
        {
            [JsonProperty(PropertyName = "Use Debug")]
            public bool UseDebug = false;
            [JsonProperty(PropertyName = "PumpkinHead triggers")]
            public Pickup PickupData = new Pickup();
            [JsonProperty(PropertyName = "NPC Settings")]
            public SettingsNPC NPCData = new SettingsNPC();
        }

        class Pickup
        {
            [JsonProperty(PropertyName = "Wild Pumpkins")]
            public Pumpkins DataPumpkins = new Pumpkins();
            [JsonProperty(PropertyName = "Wild Corn")]
            public Corn DataCorn = new Corn();
        }

        class Pumpkins
        {
            [JsonProperty(PropertyName = "Can spawn from wild Pumpkins")]
            public bool TriggerPumpkin = true;
            [JsonProperty(PropertyName = "Spawn chance (1-100%)")]
            public float NPCSpawnratePumpkin = 10f;
            [JsonProperty(PropertyName = "Sound and visual FX")]
            public Effects FXData = new Effects();
        }

        class Corn
        {
            [JsonProperty(PropertyName = "Can spawn from wild Corn")]
            public bool TriggerCorn = true;
            [JsonProperty(PropertyName = "Spawn chance (1-100%)")]
            public float NPCSpawnrateCorn = 10f;
            [JsonProperty(PropertyName = "Sound and visual FX")]
            public Effects FXData = new Effects();
        }

        class Effects
        {
            [JsonProperty(PropertyName = "FX used when npc spawns (at npc position)")]
            public string SpawnFX = "assets/bundled/prefabs/fx/explosions/water_bomb.prefab";
            [JsonProperty(PropertyName = "Spawn soundeffect (at player position)")]
            public string SpawnFXspawnsound = "assets/bundled/prefabs/fx/player/howl.prefab";
            [JsonProperty(PropertyName = "FX used when npc dies (at npc position)")]
            public string SpawnFXdie = "assets/bundled/prefabs/fx/explosions/water_bomb.prefab";
            [JsonProperty(PropertyName = "Death soundeffect (npc deathsound)")]
            public string SpawnFXdeathsound = "assets/prefabs/npc/murderer/sound/death.prefab";
        }

        class SettingsNPC
        {
            [JsonProperty(PropertyName = "Spawn Amount")]
            public int NPCAmount = 1;
            [JsonProperty(PropertyName = "Health")]
            public int NPCHealth = 250;
            [JsonProperty(PropertyName = "Max Roam Distance")]
            public int NPCRoam = 20;
            [JsonProperty(PropertyName = "Damage multiplier")]
            public float NPCDamageScale = 0.6f;
            [JsonProperty(PropertyName = "Lifetime (minutes)")]
            public float NPCLife = 10f;
            [JsonProperty(PropertyName = "Use kit (clothing)")]
            public bool UseKit = false;
            [JsonProperty(PropertyName = "Kit ID PumpkinHead")]
            public List<string> KitName = new List<string>();
            [JsonProperty(PropertyName = "Kit ID CornHead")]
            public List<string> KitName2 = new List<string>();
            [JsonProperty(PropertyName = "Show messages")]
            public bool ShowMsg = true;
            [JsonProperty(PropertyName = "NPC drop a Backpack with loot")]
            public bool UseLoot = false;
            [JsonProperty(PropertyName = "Use Random Skins")]
            public bool RandomSkins { get; set; } = true;
            [JsonProperty(PropertyName = "Spawn Min Amount Items")]
            public int MinAmount { get; set; } = 2;
            [JsonProperty(PropertyName = "Spawn Max Amount Items")]
            public int MaxAmount { get; set; } = 6;
            [JsonProperty(PropertyName = "Loot Table", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<LootItem> Loot { get; set; } = DefaultLoot;
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
            Puts("Fresh install detected Creating a new config file.");
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
                ["CornHead_Spawn"] = "My Coooorn",
                ["CornHead_Spawn_Backpack"] = "A Backpack dropped!",
                ["PumpkinHead_Spawn"] = "My Pumpkiiiin",
                ["PumpkinHead_Spawn_Backpack"] = "A Backpack dropped!",
                ["Prefix"] = "[<color=green>PumpkinHead</color>] : ",
                ["info"] = "\nGathering the wild pumpkins or corn outside you could be jumped by PumpkinHead or his twin CornHead.",
            }, this);
        }

        #endregion

        #region Commands

        [ChatCommand("phinfo")]
        void cmdphinfo(BasePlayer player, string cmd, string[] args)
        {
            string prefix = lang.GetMessage("Prefix", this);
            {
                Player.Message(player, prefix + string.Format(msg("Current Version v", player.UserIDString)) + this.Version.ToString() + " By : " + this.Author.ToString() + msg("info"), chaticon);
            }
        }

        #endregion

        #region Hooks

        object OnNpcKits(BasePlayer player)
        {
            if (_Pin.Contains(player.userID))
                return true;
            return null;
        }

        object OnCollectiblePickup(Item item, BasePlayer player, CollectibleEntity entity)
        {
            IsPumpkin = false;
            IsCorn = false;
            if (item == null || player == null || entity == null) return null;
            if (!permission.UserHasPermission(player.UserIDString, Use_Perm)) return null;
            if (item.info.shortname == "pumpkin" && !CheckSeed(item) && UnityEngine.Random.Range(0f, 1f) <= configData.PickupData.DataPumpkins.NPCSpawnratePumpkin / 100)
            {
                if (Debug) Puts($"{player} picked up Pumpkins");
                CanGive = true;
                IsPumpkin = true;
            }
            if (item.info.shortname == "corn" && !CheckSeed(item) && UnityEngine.Random.Range(0f, 1f) <= configData.PickupData.DataCorn.NPCSpawnrateCorn / 100)
            {
                if (Debug) Puts($"{player} picked up Corn");
                CanGive = true;
                IsCorn = true;
            }
            if (CanGive == true && !CheckSeed(item))
            {
                if (item != null)
                {
                    for (int i = 0; i < configData.NPCData.NPCAmount; i++)
                    {
                        Spawnnpc(entity.transform.position);
                    }
                    CanGive = false;
                    if (configData.NPCData.ShowMsg && IsSpawned == true)
                    {
                        string prefix = lang.GetMessage("Prefix", this);
                        if (IsPumpkin)
                        {
                            Player.Message(player, prefix + string.Format(msg("PumpkinHead_Spawn", player.UserIDString)), chaticon);
                        }
                        else if (IsCorn)
                        {
                            Player.Message(player, prefix + string.Format(msg("CornHead_Spawn", player.UserIDString)), chaticon);
                        }
                    }
                    if (IsPumpkin && configData.PickupData.DataPumpkins.FXData.SpawnFXspawnsound != null)
                    {
                        Effect.server.Run(configData.PickupData.DataPumpkins.FXData.SpawnFXspawnsound, player.transform.position);
                    }
                    if (IsCorn && configData.PickupData.DataCorn.FXData.SpawnFXspawnsound != null)
                    {
                        Effect.server.Run(configData.PickupData.DataCorn.FXData.SpawnFXspawnsound, player.transform.position);
                    }
                }
            }
            return null;
        }

        void OnEntityDeath(global::HumanNPC zombie, HitInfo info)
        {
            if (zombie == null || info?.Initiator == null) return;

            if (_Pin.Contains(zombie.userID))
            {
                BasePlayer player = info.InitiatorPlayer;
                if (player == null || !player.IsValid()) return;

                if (!info.InitiatorPlayer.userID.IsSteamId())
                {
                    return;
                }

                if (configData.NPCData.UseLoot == true)
                {
                    if (configData.NPCData.ShowMsg)
                    {
                        string prefix = lang.GetMessage("Prefix", this);
                        Player.Message(player, prefix + string.Format(msg("PumpkinHead_Spawn_Backpack", player.UserIDString)), chaticon);
                    }
                    SpawnLoot(zombie.transform.position + new Vector3(0f, 0.5f, 0f), zombie.transform.rotation);
                }
                _Pin.Remove(zombie.userID);
            }
        }

        private object OnNpcTarget(BasePlayer attacker, BasePlayer target)
        {
            if (attacker != null && (attacker.displayName == "PumpkinHead" || attacker.displayName == "CornHead"))
            {
                if ( target.IsSleeping () || !target.userID.IsSteamId () )
                    return true;
            }
            return null;
        }

        void OnEntitySpawned ( NPCPlayerCorpse corpse )
        {
            if ( corpse == null || corpse.IsDestroyed ) return;

            ulong id = corpse.playerSteamID;

            if (corpse._playerName.Contains("PumpkinHead") || corpse._playerName.Contains("CornHead"))
            {
                try
                {
                    if (corpse._playerName.Contains("PumpkinHead") && configData.PickupData.DataPumpkins.FXData.SpawnFXdie != null)
                    {
                        Vector3 pos = corpse.transform.position;
                        pos.y = TerrainMeta.HeightMap.GetHeight(pos);
                        CheckPos(pos);
                        if (pos != Vector3.zero)
                        {
                            Effect.server.Run(configData.PickupData.DataPumpkins.FXData.SpawnFXdie, pos);
                        }
                        Effect.server.Run(configData.PickupData.DataPumpkins.FXData.SpawnFXdeathsound, pos);
                    }

                    if (corpse._playerName.Contains("CornHead") && configData.PickupData.DataCorn.FXData.SpawnFXdie != null)
                    {
                        Vector3 pos = corpse.transform.position;
                        pos.y = TerrainMeta.HeightMap.GetHeight(pos);
                        CheckPos(pos);
                        if (pos != Vector3.zero)
                        {
                            Effect.server.Run(configData.PickupData.DataPumpkins.FXData.SpawnFXdie, pos);
                        }
                        Effect.server.Run(configData.PickupData.DataPumpkins.FXData.SpawnFXdeathsound, pos);
                    }

                    if (Debug && corpse._playerName.Contains("PumpkinHead")) Puts($"[Debug] NPC is PumpkinHead[{id}] so corpse injection is possible");
                    if (Debug && corpse._playerName.Contains("CornHead")) Puts($"[Debug] NPC is CornHead[{id}] so corpse injection is possible");
                    NextTick(() =>
                    {
                        Item PHCorn = ItemManager.CreateByName("corn", 1, 0);

                        if (corpse._playerName.Contains("PumpkinHead"))
                        {
                            PHCorn = ItemManager.CreateByName("corn", 1, 0);
                        }
                        if (corpse._playerName.Contains("CornHead"))
                        {
                            PHCorn = ItemManager.CreateByName("pumpkin", 1, 0);
                        }

                        ItemAmount NPCCorn = new ItemAmount() { itemDef = PHCorn.info, amount = 1, startAmount = 1 };
                        Item PHFiber = ItemManager.CreateByName("plantfiber", 1, 0);
                        ItemAmount NPCFiber = new ItemAmount() { itemDef = PHFiber.info, amount = 6, startAmount = 1 };

                        var dispenser = corpse.GetComponent<ResourceDispenser>();
                        if (dispenser != null)
                        {
                            dispenser.containedItems.Add(NPCCorn);
                            dispenser.containedItems.Add(NPCFiber);
                            dispenser.Initialize();
                            if (Debug && corpse._playerName.Contains("PumpkinHead")) Puts($"[Debug] PumpkinHead[{id}] Corpse was injected with [Corn & Plant fibers]");
                            if (Debug && corpse._playerName.Contains("CornHead")) Puts($"[Debug] CornHead[{id}] Corpse was injected with [Pumpkin & Plant fibers]");
                        }
                    });
                }
                catch
                {
                }
            }
            return;
        }

        #endregion

        #region Event Helpers
        private bool CheckSeed ( Item item )
        {
            if ( item.ToString ().ToLower ().Contains ( "seed" ) )
            {
                IsSeed = true;
            }
            else return false;
            return IsSeed;
        }

        public static Vector3 CheckPos(Vector3 pos)
        {
            NavMeshHit navMeshHit;

            if (!NavMesh.SamplePosition(pos, out navMeshHit, 1, 1))
                pos = Vector3.zero;
            else if (WaterLevel.GetWaterDepth(pos, true) > 0.6f)
                pos = Vector3.zero;
            else if (Physics.RaycastAll(navMeshHit.position + new Vector3(0, 100, 0), Vector3.down, 99f, 1235288065).Any())
                pos = Vector3.zero;
            else
                pos = new Vector3(navMeshHit.position.x, TerrainMeta.HeightMap.GetHeight(navMeshHit.position), navMeshHit.position.z);
            return pos;
        }

        private void Spawnnpc ( Vector3 position )
        {
            int radius = 3;
            Vector3 pos = position + UnityEngine.Random.onUnitSphere * radius;
            pos.y = TerrainMeta.HeightMap.GetHeight(pos);
            CheckPos(pos);
            if (pos == Vector3.zero)
            {
                return;
            }
            global::HumanNPC npc = (global::HumanNPC)GameManager.server.CreateEntity(zombie, pos, new Quaternion(), true);

            npc.Spawn ();
            if (IsPumpkin)
            {
                npc.displayName = "PumpkinHead";
            }
            if (IsCorn)
            {
                npc.displayName = "CornHead";
            }
            NextTick( () =>
              {
                  if ( npc == null )
                      return;

                  var mono = npc.gameObject.AddComponent<Zombies> ();
                  mono.SpawnPoint = pos;
                  SpawnedZombies.Add ( npc, mono );

                  npc.startHealth = configData.NPCData.NPCHealth;
                  npc.InitializeHealth ( configData.NPCData.NPCHealth, configData.NPCData.NPCHealth );
                  KitIsValid = false;
                  npc.damageScale = configData.NPCData.NPCDamageScale;

                  if ((npc.displayName == "PumpkinHead") && configData.NPCData.UseKit && configData.NPCData.KitName.Count > 0 )
                  {
                      object checkKit = Kits?.CallHook ( "GetKitInfo", configData.NPCData.KitName [ new System.Random ().Next ( configData.NPCData.KitName.Count () ) ] );
                      if ( checkKit != null )
                      {
                          npc.inventory.Strip ();
                          Kits?.Call ( $"GiveKit", npc, configData.NPCData.KitName [ new System.Random ().Next ( configData.NPCData.KitName.Count () ) ] );
                          KitIsValid = true;
                      }
                  }

                  if ((npc.displayName == "CornHead") && configData.NPCData.UseKit && configData.NPCData.KitName2.Count > 0)
                  {
                      object checkKit = Kits?.CallHook("GetKitInfo", configData.NPCData.KitName2[new System.Random().Next(configData.NPCData.KitName2.Count())]);
                      if (checkKit != null)
                      {
                          npc.inventory.Strip();
                          Kits?.Call($"GiveKit", npc, configData.NPCData.KitName2[new System.Random().Next(configData.NPCData.KitName2.Count())]);
                          KitIsValid = true;
                      }
                  }
                  (npc as ScientistNPC).radioChatterType = ScientistNPC.RadioChatterType.NONE;

                  if ( !KitIsValid || !configData.NPCData.UseKit )
                  {
                      var inv_belt = npc.inventory.containerBelt;
                      var inv_wear = npc.inventory.containerWear;

                      npc.inventory.Strip ();
                      if(npc.displayName.Contains("PumpkinHead"))
                      {
                          ItemManager.CreateByName("pumpkin", 1, 0).MoveToContainer(inv_wear, 0);
                      }
                      if (npc.displayName.Contains("CornHead"))
                      {
                          ItemManager.CreateByName("metal.facemask", 1, 1670059636UL).MoveToContainer(inv_wear, 0);
                      }

                      Item eyes = ItemManager.CreateByName ( "gloweyes", 1, 0 );
                      Item boots = ItemManager.CreateByName ( "shoes.boots", 1, 0 );
                      Item gloves = ItemManager.CreateByName ( "burlap.gloves.new", 1, 0 );
                      Item chest = ItemManager.CreateByName ( "burlap.shirt", 1, 0 );
                      Item pants = ItemManager.CreateByName ( "burlap.trousers", 1, 0 );
                      Item woodpants = ItemManager.CreateByName ( "wood.armor.pants", 1, 0 );

                      switch ( UnityEngine.Random.Range ( 0, 4 ) )
                      {
                          case 0:
                              ItemManager.CreateByName ( "shotgun.waterpipe", 1, 0 ).MoveToContainer ( inv_belt, 0 );
                              break;
                          case 1:
                              ItemManager.CreateByName ("shotgun.double", 1, 0 ).MoveToContainer ( inv_belt, 0 );
                              break;
                          case 2:
                              ItemManager.CreateByName ("shotgun.pump", 1, 0 ).MoveToContainer ( inv_belt, 0 );
                              break;
                          default:
                              ItemManager.CreateByName ("shotgun.spas12", 1, 0 ).MoveToContainer ( inv_belt, 0 );
                              break;
                      }
                      if ( eyes != null ) eyes.MoveToContainer ( inv_wear );
                      if ( boots != null ) boots.MoveToContainer ( inv_wear );
                      if ( gloves != null ) gloves.MoveToContainer ( inv_wear );
                      if ( chest != null ) chest.MoveToContainer ( inv_wear );
                      if ( pants != null ) pants.MoveToContainer ( inv_wear );
                      if ( woodpants != null ) woodpants.MoveToContainer ( inv_wear );
                      if ( Debug ) PrintWarning ( $"{npc} has a default outfit assigned." );

                  }
              } );

            if (npc.IsHeadUnderwater())
            {
                npc.Kill();
                if (IsPumpkin) Puts("PumpkinHead[" + npc.userID + "] skipped its spawn (spawned under water)");
                if (IsCorn) Puts("CornHead[" + npc.userID + "] skipped its spawn (spawned under water)");
                IsSpawned = false;
                return;
            }
            if (!npc.IsOutside())
            {
                npc.Kill();
                if (IsPumpkin) Puts("PumpkinHead[" + npc.userID + "] skipped its spawn (No valid spawnpoint)");
                if (IsCorn) Puts("CornHead[" + npc.userID + "] skipped its spawn (No valid spawnpoint)");
                IsSpawned = false;
                return;
            }
            IsSpawned = true;

            var id = npc.userID;
            if (IsPumpkin) Puts($"PumpkinHead[" + id + "] spawned");
            if (IsCorn) Puts($"CornHead[" + id + "] spawned");
            _Pin.Add(npc.userID);
            if (IsPumpkin && configData.PickupData.DataPumpkins.FXData.SpawnFX != null)
            {
                Effect.server.Run(configData.PickupData.DataPumpkins.FXData.SpawnFX, npc.transform.position);
            }
            if (IsCorn && configData.PickupData.DataCorn.FXData.SpawnFX != null)
            {
                Effect.server.Run(configData.PickupData.DataCorn.FXData.SpawnFX, npc.transform.position);
            }
            timer.Once(configData.NPCData.NPCLife * 60, () =>
            {
                if (npc != null)
                {
                    _Pin.Remove(npc.userID);
                    npc.Kill();
                    if (IsPumpkin) Puts("PumpkinHead[" + id + "] Died of natural causes!!!");
                    if (IsCorn) Puts("CornHead[" + id + "] Died of natural causes!!!");
                    return;
                }
                if (Debug && IsPumpkin) Puts("[Debug] PumpkinHead[" + id + "] was killed before ending of timer");
                if (Debug && IsCorn) Puts("[Debug] CornHead[" + id + "] was killed before ending of timer");
                return;
            });
        }

        #endregion

        #region Loot System

        private static List<LootItem> DefaultLoot
        {
            get
            {
                return new List<LootItem>
                {
                    new LootItem { shortname = "ammo.pistol", amount = 5, skin = 0, amountMin = 5 },
                    new LootItem { shortname = "ammo.pistol.fire", amount = 5, skin = 0, amountMin = 5 },
                    new LootItem { shortname = "ammo.pistol.hv", amount = 5, skin = 0, amountMin = 5 },
                    new LootItem { shortname = "ammo.rifle", amount = 5, skin = 0, amountMin = 5 },
                    new LootItem { shortname = "ammo.rifle.explosive", amount = 5, skin = 0, amountMin = 5 },
                    new LootItem { shortname = "ammo.rifle.hv", amount = 5, skin = 0, amountMin = 5 },
                    new LootItem { shortname = "ammo.rifle.incendiary", amount = 5, skin = 0, amountMin = 5 },
                    new LootItem { shortname = "ammo.shotgun", amount = 12, skin = 0, amountMin = 8 },
                    new LootItem { shortname = "explosive.timed", amount = 1, skin = 0, amountMin = 1 },
                    new LootItem { shortname = "explosives", amount = 1, skin = 0, amountMin = 1 },
                    new LootItem { shortname = "pistol.m92", amount = 1, skin = 0, amountMin = 1 },
                    new LootItem { shortname = "shotgun.spas12", amount = 1, skin = 0, amountMin = 1 },
                    new LootItem { shortname = "pickaxe", amount = 1, skin = 0, amountMin = 1 },
                    new LootItem { shortname = "hatchet", amount = 1, skin = 0, amountMin = 1 },
                    new LootItem { shortname = "can.beans", amount = 3, skin = 0, amountMin = 1 },
                    new LootItem { shortname = "can.tuna", amount = 3, skin = 0, amountMin = 1 },
                    new LootItem { shortname = "black.raspberries", amount = 5, skin = 0, amountMin = 3 },
                };
            }
        }

        public class LootItem
        {
            public string shortname { get; set; }
            public int amount { get; set; }
            public ulong skin { get; set; }
            public int amountMin { get; set; }
        }

        private void SpawnLoot ( Vector3 pos, Quaternion rot )
        {
            var backpack = GameManager.server.CreateEntity ( StringPool.Get ( 1519640547 ), pos, rot, true ) as DroppedItemContainer;

            if ( backpack == null ) return;

            backpack.inventory = new ItemContainer ();
            backpack.inventory.ServerInitialize ( null, 36 );
            backpack.inventory.GiveUID ();
            backpack.inventory.entityOwner = backpack;
            backpack.inventory.SetFlag ( ItemContainer.Flag.NoItemInput, true );
            backpack.Spawn ();
            backpack.playerName = "PumpkinHead Backpack";
            SpawnLoot ( backpack.inventory, configData.NPCData.Loot.ToList () );
        }

        private void SpawnLoot ( ItemContainer container, List<LootItem> loot )
        {
            int total = UnityEngine.Random.Range ( Math.Min ( loot.Count, configData.NPCData.MinAmount ), Math.Min ( loot.Count, configData.NPCData.MaxAmount ) );

            if ( total == 0 || loot.Count == 0 )
            {
                return;
            }

            container.capacity = total;
            ItemDefinition def;
            List<ulong> skins;
            LootItem lootItem;

            for ( int j = 0; j < total; j++ )
            {
                if ( loot.Count == 0 )
                {
                    break;
                }

                lootItem = loot.GetRandom ();

                loot.Remove ( lootItem );

                if ( lootItem.amount <= 0 )
                {
                    continue;
                }

                string shortname = lootItem.shortname;
                bool isBlueprint = shortname.EndsWith ( ".bp" );

                if ( isBlueprint )
                {
                    shortname = shortname.Replace ( ".bp", string.Empty );
                }

                def = ItemManager.FindItemDefinition ( shortname );

                if ( def == null )
                {
                    Puts ( "Invalid shortname: {0}", lootItem.shortname );
                    continue;
                }

                ulong skin = lootItem.skin;

                if ( configData.NPCData.RandomSkins && skin == 0 )
                {
                    skins = GetItemSkins ( def );

                    if ( skins.Count > 0 )
                    {
                        skin = skins.GetRandom ();
                    }
                }

                int amount = lootItem.amount;

                if ( amount <= 0 )
                {
                    continue;
                }

                if ( lootItem.amountMin > 0 && lootItem.amountMin < lootItem.amount )
                {
                    amount = UnityEngine.Random.Range ( lootItem.amountMin, lootItem.amount );
                }

                Item item;

                if ( isBlueprint )
                {
                    item = ItemManager.CreateByItemID ( -996920608, 1, 0 );

                    if ( item == null ) continue;

                    item.blueprintTarget = def.itemid;
                    item.amount = amount;
                }
                else item = ItemManager.Create ( def, amount, skin );

                if ( !item.MoveToContainer ( container, -1, false ) )
                {
                    item.Remove ();
                }
            }
        }

        private List<ulong> GetItemSkins ( ItemDefinition def )
        {
            List<ulong> skins;
            if ( !Skins.TryGetValue ( def.shortname, out skins ) )
            {
                Skins [ def.shortname ] = skins = ExtractItemSkins ( def, skins );
            }

            return skins;
        }

        private List<ulong> ExtractItemSkins ( ItemDefinition def, List<ulong> skins )
        {
            skins = new List<ulong> ();

            foreach ( var skin in def.skins )
            {
                skins.Add ( Convert.ToUInt64 ( skin.id ) );
            }
            foreach ( var asi in Rust.Workshop.Approved.All.Values )
            {
                if ( !string.IsNullOrEmpty ( asi.Skinnable.ItemName ) && asi.Skinnable.ItemName == def.shortname )
                {
                    skins.Add ( Convert.ToUInt64 ( asi.WorkshopdId ) );
                }
            }

            return skins;
        }

        #endregion

        #region Message helper

        private string msg ( string key, string id = null ) => lang.GetMessage ( key, this, id );

        #endregion
    }
}
