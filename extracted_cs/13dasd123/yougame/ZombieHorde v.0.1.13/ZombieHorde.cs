// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.AI;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("ZombieHorde", "k1lly0u", "0.1.13", ResourceId = 0)]
    [Description("Ужастная орда зомби! Приобретено на Oxide-Russia.RU у Ernieleo")]
    class ZombieHorde : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin Kits, Vanish, RandomSpawns, Spawns;

        private static ZombieHorde ins; 
        private List<HordeManager> managers = new List<HordeManager>();

        private SpawnSystem spawnSystem = SpawnSystem.None;

        private LootType lootType = LootType.Default;

        private List<Vector3> spawnPoints = new List<Vector3>();

        private Queue<HordeOrder> hordeQueue = new Queue<HordeOrder>();

        private Dictionary<ulong, InventoryData> deadMemberIds = new Dictionary<ulong, InventoryData>();

        private Dictionary<string, int> itemNameToId = new Dictionary<string, int>();

        private bool IsQueueRunning { get; set; } = false;

        private int groundLayer = LayerMask.GetMask("Terrain", "World");

        const string zombiePrefab = "assets/prefabs/npc/murderer/murderer.prefab";
        #endregion

        #region Oxide Hooks        
        private void OnServerInitialized()
        {
            ins = this;
            permission.RegisterPermission("zombiehorde.admin", this);
            permission.RegisterPermission("zombiehorde.ignore", this);

            itemNameToId = ItemManager.itemList.ToDictionary(x => x.shortname, y => y.itemid);

            if (!configData.Member.TargetedByTurrets)
                Unsubscribe(nameof(CanBeTargeted));

            lootType = ParseType<LootType>(configData.Member.Loot);
            if (lootType == LootType.Default)                            
                Unsubscribe(nameof(OnEntitySpawned));            

            if (!VerifySpawnSystem())
                return;

            FindMonuments();
        }

        private void Unload()
        {
            for (int i = managers.Count - 1; i >= 0; i--)            
                UnityEngine.Object.Destroy(managers.ElementAt(i));

            ins = null;
        }

        private void OnEntitySpawned(BaseNetworkable networkable)
        {
            NPCPlayerCorpse corpse = networkable.GetComponent<NPCPlayerCorpse>();
            if (corpse == null)
                return;

            if (lootType == LootType.Default)
                return;

            InventoryData inventoryData;

            if (!deadMemberIds.TryGetValue(corpse.playerSteamID, out inventoryData))
                return;

            deadMemberIds.Remove(corpse.playerSteamID);

            timer.In(2, () =>
            {
                if (inventoryData == null || lootType == LootType.Random)
                    FillLootContainer(corpse);
                else inventoryData.RestoreItemsTo(corpse);
            });            
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null)
                return;

            HordeMember hordeMember = entity.GetComponent<HordeMember>();
            if (hordeMember != null)
            {
                BasePlayer attacker = info.InitiatorPlayer;
                if (attacker != null)
                    hordeMember.Manager.OnTargetAquired(attacker);
                return;
            }

            hordeMember = info?.InitiatorPlayer?.GetComponent<HordeMember>();
            if (hordeMember != null && configData.Member.DamageMultiplier != 1)            
                info.damageTypes.ScaleAll(configData.Member.DamageMultiplier);            
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null)
                return;

            HordeMember hordeMember = entity.GetComponent<HordeMember>();
            if (hordeMember != null)
            {
                if (lootType != LootType.Default)
                {
                    if (!deadMemberIds.ContainsKey(hordeMember.Player.userID))
                        deadMemberIds.Add(hordeMember.Player.userID, lootType == LootType.Kit ? new InventoryData(hordeMember.Player) : null);
                }

                hordeMember.Manager.OnMemberDeath(hordeMember);
                if (hordeMember.Manager.Members.Count == 0)
                {
                    HordeManager manager = hordeMember.Manager;
                    NextTick(() =>
                    {
                        OnHordeDestroyed(manager);
                    });
                }
                return;
            }

            foreach (HordeManager manager in managers)            
                manager.OnEntityDeath(entity, info);
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null)
                return;

            HordeMember hordeMember = entity.GetComponent<HordeMember>();
            if (hordeMember != null)
            {
                if (lootType != LootType.Default)
                {
                    if (!deadMemberIds.ContainsKey(hordeMember.Player.userID))
                        deadMemberIds.Add(hordeMember.Player.userID, lootType == LootType.Kit ? new InventoryData(hordeMember.Player) : null);
                }                    

                hordeMember.Manager.OnMemberDeath(hordeMember);
                if (hordeMember.Manager.Members.Count == 0)
                {
                    HordeManager manager = hordeMember.Manager;
                    NextTick(() =>
                    {
                        OnHordeDestroyed(manager);
                    });
                }
                return;
            }
        }

        private object CanBeTargeted(BaseCombatEntity player, MonoBehaviour behaviour)
        {
            if (player == null)
                return null;

            HordeMember hordeMember = player.GetComponent<HordeMember>();
            if (hordeMember != null)
                return false;

            return null;
        }

        private object OnNpcDestinationSet(NPCPlayerApex npcPlayer, Vector3 destination)
        {
            HordeMember hordeMember = npcPlayer.GetComponent<HordeMember>();
            if (hordeMember != null)
            {
                if (hordeMember.Player.AttackTarget != null)
                    return null;
                return false;
            }
            return null;
        }

        private object OnNpcStopMoving(NPCPlayerApex npcPlayer)
        {
            HordeMember hordeMember = npcPlayer.GetComponent<HordeMember>();
            if (hordeMember != null)
                return false;
            return null;
        }

        private object OnNpcPlayerTarget(NPCPlayerApex npcPlayer, BaseEntity target)
        {
            HordeMember hordeMember = npcPlayer.GetComponent<HordeMember>();
            if (hordeMember != null && target != null)
            {
                if (!target.HasAnyTrait(configData.Member.TargetAnimals ? BaseEntity.TraitFlag.Animal | BaseEntity.TraitFlag.Human : BaseEntity.TraitFlag.Human))                
                    return true;
                
                if (target.GetComponent<HordeMember>())
                    return true;
                
                if (target.Health() <= 0f)
                    return true;

                if (target is BasePlayer)
                {
                    BasePlayer player = target as BasePlayer;
                    
                    if (Vanish != null && (bool)Vanish.Call("IsInvisible", player))
                        return null;
                    
                    if (configData.Member.TargetScientists && player is Scientist)
                        return true;

                    if (player.IsFlying)
                        return true;

                    if (player.HasPlayerFlag(BasePlayer.PlayerFlags.SafeZone))
                        return true;

                    if (permission.UserHasPermission(player.UserIDString, "zombiehorde.ignore"))
                        return true;
                }

                if (Vector3.Distance(npcPlayer.transform.position, target.transform.position) > configData.Horde.ChaseDistance)
                    return true;

                if (hordeMember.IsHordeLeader)
                    hordeMember.Manager.OnTargetAquired(target);
            }
            return null;
        }
        #endregion

        #region Functions
        private T ParseType<T>(string type)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), type, true);
            }
            catch
            {
                return default(T);
            }
        }

        private bool VerifySpawnSystem()
        {
            spawnSystem = ParseType<SpawnSystem>(configData.Horde.SpawnType);
            switch (spawnSystem)
            {
                case SpawnSystem.None:
                    PrintError("You have set an invalid value in the config entry \"Spawn Type\". Unable to spawn hordes!");
                    return false;
                case SpawnSystem.Default:
                    return true;
                case SpawnSystem.SpawnsDatabase:
                    if (Spawns != null)
                    {
                        if (string.IsNullOrEmpty(configData.Horde.SpawnFile))
                        {
                            PrintError("You have selected SpawnsDatabase as your method of spawning hordes, however you have not specified a spawn file. Unable to spawn hordes!");
                            return false;
                        }

                        object success = Spawns?.Call("LoadSpawnFile", configData.Horde.SpawnFile);
                        if (success is List<Vector3>)
                        {
                            spawnPoints = success as List<Vector3>;
                            if (spawnPoints.Count == 0)
                            {
                                PrintError("You have selected SpawnsDatabase as your method of spawning hordes, however the spawn file you have chosen has no spawn points. Unable to spawn hordes!");
                                return false;
                            }
                            return true;
                        }
                    }
                    else PrintError("You have selected SpawnsDatabase as your method of spawning hordes, however SpawnsDatabase is not loaded on your server. Unable to spawn hordes!");
                    return false;
                case SpawnSystem.RandomSpawns:
                    if (RandomSpawns != null)
                        return true;
                    else PrintError("You have selected RandomSpawns as your method of spawning hordes, however RandomSpawns is not loaded on your server. Unable to spawn hordes!");
                    return false;                
            }
            return false;
        }

        private object GetSpawnPoint()
        {
            switch (spawnSystem)
            {
                case SpawnSystem.None:
                    return null;
                case SpawnSystem.Default:
                    return ServerMgr.FindSpawnPoint().pos;
                case SpawnSystem.SpawnsDatabase:
                    {
                        if (Spawns == null)
                        {
                            PrintError("Tried getting a spawn point but SpawnsDatabase is null. Make sure SpawnsDatabase is still loaded to continue using custom spawn points");
                            goto case SpawnSystem.Default;
                        }

                        Vector3 spawnPoint = spawnPoints.GetRandom();
                        spawnPoints.Remove(spawnPoint);
                        if (spawnPoints.Count == 0)
                            spawnPoints = (List<Vector3>)Spawns.Call("LoadSpawnFile", configData.Horde.SpawnFile);
                        return spawnPoint;
                    }
                case SpawnSystem.RandomSpawns:
                    if (RandomSpawns == null)
                    {
                        PrintError("Tried getting a spawn point but RandomSpawns is null. Make sure RandomSpawns is still loaded to continue using randomised spawn points");
                        goto case SpawnSystem.Default;
                    }
                    return RandomSpawns.Call("GetSpawnPoint");
            }
            return null;
        }

        private static object FindPointOnNavmesh(Vector3 targetPosition)
        {
            for (int i = 0; i < 10; i++)
            {
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(targetPosition, out navHit, 100, 1))
                {
                    if (navHit.position.y < TerrainMeta.HeightMap.GetHeight(navHit.position) + 1)
                    {
                        if (IsNearOrInRock(targetPosition + Vector3.up))
                            continue;
                        return navHit.position;
                    }
                }
            }           
            return null;
        }

        private static bool IsNearOrInRock(Vector3 position)
        {
            int entities = Physics.OverlapSphereNonAlloc(position, 2f, Vis.colBuffer, LayerMask.GetMask("World"));
            if (entities == 0)
                return false;

            bool flag = false;
            for (int i = 0; i < entities; i++)
            {
                flag = Vis.colBuffer[i].gameObject?.name.Contains("rock_") ?? false;
                Vis.colBuffer[i] = null;
            }

            return flag;
        }

        private void CreateRandomHordes()
        {
            for (int i = 0; i < configData.Horde.HordeAmount - managers.Count; i++)            
                hordeQueue.Enqueue(new HordeOrder(Vector3.zero, configData.Horde.RoamLocal, configData.Horde.RoamDistance, false));

            ProcessHordeOrders();
        }

        private void ProcessHordeOrders(bool force = false)
        {
            if (IsQueueRunning)
                return;

            if (force || (hordeQueue.Count > 0 && managers.Count < configData.Horde.HordeAmount))
            {
                IsQueueRunning = true;

                HordeOrder hordeOrder = hordeQueue.Dequeue();
                ServerMgr.Instance.StartCoroutine(CreateZombieHorde(hordeOrder));
            }
        }

        private IEnumerator CreateZombieHorde(HordeOrder hordeOrder)
        {
            Vector3 spawnPosition = hordeOrder.position;

            if (spawnPosition == Vector3.zero)
            {
                object success = GetSpawnPoint();
                if (success is Vector3)
                    spawnPosition = (Vector3)success;
                else yield break;                
            }

            HordeManager manager = new GameObject().AddComponent<HordeManager>();
            if (hordeOrder.roamLocal)
                manager.Initialize(configData, spawnPosition, hordeOrder.distanceOverride, hordeOrder.isMonumentSpawned);
            else manager.Initialize(configData);

            managers.Add(manager);

            for (int y = 0; y < configData.Horde.InitialSize; y++)
            {
                CreateHordeMember(spawnPosition, manager);
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
                yield return new WaitForEndOfFrame();
            }

            manager.SetHordeDestination(manager.GetAverageVector());
            manager.SetHordeLeader();

            yield return new WaitForSecondsRealtime(1f);

            IsQueueRunning = false;

            if (hordeQueue.Count > 0)
                ProcessHordeOrders();
        }

        private NPCPlayerApex InstantiateEntity(Vector3 position)
        {
            GameObject gameObject = Facepunch.Instantiate.GameObject(GameManager.server.FindPrefab(zombiePrefab), position, new Quaternion());
            gameObject.name = zombiePrefab;

            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);

            UnityEngine.Object.Destroy(gameObject.GetComponent<Spawnable>());

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            NPCPlayerApex component = gameObject.GetComponent<NPCPlayerApex>();
            return component;
        }

        private void CreateHordeMember(Vector3 position, HordeManager manager)
        {
            Vector2 random = UnityEngine.Random.insideUnitCircle * 3;
            object success = FindPointOnNavmesh(position + new Vector3(random.x, 0, random.y));
            if (success == null)           
                return;            

            NPCPlayerApex player = InstantiateEntity((Vector3)success);
            if (player == null)
                return;

            player.enableSaving = false;
            player.Spawn();

            player.displayName = Facepunch.RandomUsernames.All.GetRandom();
            player.InitializeHealth(configData.Member.Health, configData.Member.Health);
            player.Stats.AggressionRange = player.Stats.DeaggroRange = configData.Horde.ChaseDistance;

            if (lootType != LootType.Default)
                (player as NPCMurderer).LootSpawnSlots = new LootContainer.LootSpawnSlot[0];

            if (Kits != null && configData.Member.Kits.Length > 0)
            {
                player.CancelInvoke(player.EquipTest);
                StripInventory(player);
                NextTick(() =>
                {
                    if (player == null)
                        return;

                    string kitName = configData.Member.Kits.GetRandom();
                    Kits?.Call("GiveKit", player, kitName);
                    ins.timer.In(3, () =>
                    {
                        if (player == null)
                            return;

                        if (player.inventory.containerBelt.GetSlot(0) == null)                        
                            PrintError($"The kit '{kitName}' does not have a active weapon in the first slot of the tool belt. You must re-adjust the kit for NPC players to utilize a weapon!");  
                        else player.EquipTest();
                    });
                });                
            }

            HordeMember member = player.gameObject.AddComponent<HordeMember>();
            member.Manager = manager;
            manager.Members.Add(member);
        }

        private void OnHordeDestroyed(HordeManager manager)
        {
            HordeOrder hordeOrder = manager.IsMonumentSpawned ? manager.CreateHordeOrder() : new HordeOrder(Vector3.zero, configData.Horde.RoamLocal, configData.Horde.RoamDistance, false);

            managers.Remove(manager);
            UnityEngine.Object.Destroy(manager);
            

            timer.In(configData.Horde.RespawnTime, () =>
            {
                if (hordeOrder != null)
                {
                    hordeQueue.Enqueue(hordeOrder);
                    ProcessHordeOrders();
                }                
            });
        }

        private void StripInventory(BasePlayer player)
        {
            Item[] allItems = player.inventory.AllItems();

            for (int i = allItems.Length - 1; i >= 0; i--)
            {
                Item item = allItems[i];
                item.RemoveFromContainer();
                item.Remove();
            }
        }

        private void FillLootContainer(NPCPlayerCorpse corpse)
        {
            if (corpse == null) return;
            ItemContainer itemContainer = corpse.containers[0];
            if (itemContainer == null)
                return;

            int count = UnityEngine.Random.Range(configData.Loot.Minimum, configData.Loot.Maximum);

            if (itemContainer.capacity < count)
                itemContainer.capacity = count;

            List<ConfigData.LootTable.LootItem> Items = new List<ConfigData.LootTable.LootItem>(configData.Loot.Items);
            for (int i = 0; i < count; i++)
            {
                ConfigData.LootTable.LootItem lootItem = Items.GetRandom();
                if (lootItem == null) continue;

                bool isBlueprint = lootItem.Name.EndsWith(".bp");
                string shortname = isBlueprint ? lootItem.Name.Substring(0, lootItem.Name.Length - 3) : lootItem.Name;

                if (!itemNameToId.ContainsKey(shortname))
                {
                    PrintError($"Invalid item shortname set in loot list : {shortname}");
                    continue;
                }

                Item item = null;
                if (isBlueprint)
                {
                    item = ItemManager.CreateByItemID(-996920608, 1, 0);
                    item.blueprintTarget = itemNameToId[shortname];
                    item.amount = UnityEngine.Random.Range(lootItem.Minimum, lootItem.Maximum);
                }
                else item = ItemManager.CreateByName(shortname);

                if (item != null)
                {
                    item.amount = UnityEngine.Random.Range(lootItem.Minimum, lootItem.Maximum);
                    item.MoveToContainer(itemContainer, -1, false);
                }
                Items.Remove(lootItem);
            }            
        }

        private void FindMonuments()
        {
            int count = 0;
            GameObject[] allobjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var gobject in allobjects)
            {
                if (count >= configData.Horde.HordeAmount)
                    break;

                if (gobject.name.Contains("autospawn/monument"))
                {
                    Vector3 position = gobject.transform.position;
                    if (position == Vector3.zero)
                        continue;
                  
                    if (gobject.name.Contains("powerplant_1"))
                    {
                        if (configData.Monument.Powerplant.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.Powerplant.RoamLocal, configData.Monument.Powerplant.RoamDistance, true));                            
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("military_tunnel_1"))
                    {
                        if (configData.Monument.Tunnels.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.Tunnels.RoamLocal, configData.Monument.Tunnels.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("harbor_1"))
                    {
                        if (configData.Monument.LargeHarbor.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.LargeHarbor.RoamLocal, configData.Monument.LargeHarbor.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("harbor_2"))
                    {
                        if (configData.Monument.SmallHarbor.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.SmallHarbor.RoamLocal, configData.Monument.SmallHarbor.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("airfield_1"))
                    {
                        if (configData.Monument.Airfield.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.Airfield.RoamLocal, configData.Monument.Airfield.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("trainyard_1"))
                    {
                        if (configData.Monument.Trainyard.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.Trainyard.RoamLocal, configData.Monument.Trainyard.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("water_treatment_plant_1"))
                    {
                        if (configData.Monument.WaterTreatment.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.WaterTreatment.RoamLocal, configData.Monument.WaterTreatment.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("warehouse"))
                    {
                        if (configData.Monument.Warehouse.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.Warehouse.RoamLocal, configData.Monument.Warehouse.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("satellite_dish"))
                    {
                        if (configData.Monument.Satellite.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.Satellite.RoamLocal, configData.Monument.Satellite.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("sphere_tank"))
                    {
                        if (configData.Monument.Dome.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.Dome.RoamLocal, configData.Monument.Dome.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("radtown_small_3"))
                    {
                        if (configData.Monument.Radtown.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.Radtown.RoamLocal, configData.Monument.Radtown.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("launch_site_1"))
                    {
                        if (configData.Monument.RocketFactory.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.RocketFactory.RoamLocal, configData.Monument.RocketFactory.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("gas_station_1"))
                    {
                        if (configData.Monument.GasStation.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.GasStation.RoamLocal, configData.Monument.GasStation.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("supermarket_1"))
                    {
                        if (configData.Monument.Supermarket.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.Supermarket.RoamLocal, configData.Monument.Supermarket.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("mining_quarry_c"))
                    {
                        if (configData.Monument.Quarry_HQM.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.Quarry_HQM.RoamLocal, configData.Monument.Quarry_HQM.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("mining_quarry_a"))
                    {
                        if (configData.Monument.Quarry_Sulfur.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.Quarry_Sulfur.RoamLocal, configData.Monument.Quarry_Sulfur.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("mining_quarry_b"))
                    {
                        if (configData.Monument.Quarry_Stone.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.Quarry_Stone.RoamLocal, configData.Monument.Quarry_Stone.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }

                    if (gobject.name.Contains("junkyard_1"))
                    {
                        if (configData.Monument.Junkyard.Enabled)
                        {
                            hordeQueue.Enqueue(new HordeOrder(position, configData.Monument.Junkyard.RoamLocal, configData.Monument.Junkyard.RoamDistance, true));
                            count++;
                        }
                        continue;
                    }
                }
            }

            if (count < configData.Horde.HordeAmount)
                CreateRandomHordes();
            else ProcessHordeOrders();
        }
        #endregion

        #region Behaviours
        private class HordeManager : MonoBehaviour
        {
            public List<HordeMember> Members { get; private set; }
            public ConfigData Config { get; set; }

            public HordeMember GetHordeLeader
            {
                get
                {
                    return Members.FirstOrDefault(x => x.IsHordeLeader);
                }
            }

            public bool JustMergedHorde
            {
                get
                {
                    return justMerged;
                }
                set
                {
                    justMerged = value;
                    InvokeHandler.Invoke(this, () => justMerged = false, 180);
                }
            }

            public bool IsMonumentSpawned { get; private set; }   

            private BaseEntity targetEntity = null;
            private Vector3 targetPosition;

            private Vector3 initialSpawnPoint;
            private float roamDistance;
            private bool localRoam;

            private float nextGrowthTime;
            private bool justMerged;

            private void Awake()
            {
                enabled = false;               
            }

            private void OnDestroy()
            {
                InvokeHandler.CancelInvoke(this, HordeTick);

                for (int i = Members.Count - 1; i >= 0; i--)                
                    Destroy(Members.ElementAt(i));                
            }

            public void Initialize(ConfigData config, Vector3 initialSpawnPoint, object distanceOverride, bool isMonumentSpawned)
            {
                this.initialSpawnPoint = initialSpawnPoint;
                IsMonumentSpawned = isMonumentSpawned;

                if (distanceOverride is float)
                    roamDistance = (float)distanceOverride;
                else roamDistance = config.Horde.RoamDistance;
                localRoam = true;
                Initialize(config);
            }

            public void Initialize(ConfigData config)
            {
                Members = new List<HordeMember>();
                Config = config;

                nextGrowthTime = Time.realtimeSinceStartup + Config.Horde.GrowthRate;

                InvokeHandler.InvokeRandomized(this, HordeTick, 1f, 5f, 1f);
            }

            public void SetHordeDestination(Vector3 destination)
            {
                targetPosition = destination;

                foreach (HordeMember member in Members)               
                    member.UpdateTargetPosition(destination);               
            }

            public void OnTargetAquired(BaseEntity targetEntity)
            {
                if (targetEntity == null || this.targetEntity == targetEntity)
                    return;

                Vector3 averageVector = GetAverageVector();

                if (Vector3.Distance(averageVector, targetEntity.transform.position) > Config.Horde.ChaseDistance || (localRoam && Vector3.Distance(averageVector, initialSpawnPoint) > roamDistance))
                {
                    targetEntity = null;
                    return;
                }

                if (this.targetEntity == null || Vector3.Distance(targetEntity.transform.position, averageVector) < Vector3.Distance(this.targetEntity.transform.position, averageVector))
                {
                    this.targetEntity = targetEntity;
                    foreach (HordeMember hordeMember in Members)
                        hordeMember.UpdateTargetEntity(targetEntity);
                }
            }

            private void HordeTick()
            {
                if (Members.Count == 0)
                {
                    ins.OnHordeDestroyed(this);
                    Destroy(this);
                    return;
                }

                HordeMember hordeLeader = GetHordeLeader;
                if (hordeLeader == null)
                {
                    SetHordeLeader();
                    return;
                }

                if (hordeLeader.Player.IsDormant)
                    return;

                TryMergeHordes();
                TryGrowHorde();  

                if (IsValidTarget(targetEntity))
                {
                    foreach (HordeMember hordeMember in Members)                    
                       hordeMember.UpdateTargetEntity(targetEntity);                    
                }
                else
                {
                    targetEntity = null;

                    Vector3 averageVector = GetAverageVector();

                    if (Vector3.Distance(averageVector, targetPosition) < 30)
                    {
                        SetHordeDestination(GetRandomLocation());
                        return;
                    }

                    foreach (HordeMember hordeMember in Members)
                    {
                        if (hordeMember == null)
                            continue;

                        if (Vector3.Distance(hordeMember.transform.position, averageVector) > 10)                      
                            hordeMember.UpdateTargetPosition(averageVector, true);                        
                        else hordeMember.UpdateTargetPosition(targetPosition);                        
                    }                   
                }               
            }            

            public void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
            {                
                if (targetEntity == entity)                
                    targetEntity = null;
                
                HordeMember hordeMember = info?.InitiatorPlayer?.GetComponent<HordeMember>();
                if (hordeMember == null || !Members.Contains(hordeMember))
                    return;

                if (entity is BasePlayer && Config.Horde.CreateOnDeath && Members.Count < Config.Horde.MaximumSize)
                    AddHordeMember();
            }

            public void OnMemberDeath(HordeMember hordeMember)
            {                
                bool isHordeLeader = hordeMember.IsHordeLeader;
                Members.Remove(hordeMember);

                if (isHordeLeader && Members.Count > 0)
                    SetHordeLeader();
            }

            public Vector3 GetAverageVector()
            {
                float x = 0;
                float y = 0;
                float z = 0;

                foreach (HordeMember hordeMember in Members)
                {
                    if (hordeMember == null || hordeMember.Player == null)
                        continue;

                    x += hordeMember.transform.position.x;
                    y += hordeMember.transform.position.y;
                    z += hordeMember.transform.position.z;
                }

                return new Vector3(x / Members.Count, y / Members.Count, z / Members.Count);
            }

            public void SetHordeLeader()
            {
                if (Members.Count > 0)
                    Members.GetRandom().IsHordeLeader = true;
            }

            private Vector3 GetRandomLocation()
            {
                Vector3 position = Vector3.zero;

                if (localRoam)
                {
                    Vector2 randomInCircle = UnityEngine.Random.insideUnitCircle * roamDistance;

                    position = initialSpawnPoint + new Vector3(randomInCircle.x, 0, randomInCircle.y);
                    position.y = TerrainMeta.HeightMap.GetHeight(position);
                }
                else
                {
                    if (ins.RandomSpawns)
                    {
                        object success = ins.RandomSpawns.Call("GetSpawnPoint");
                        if (success is Vector3)
                            position = (Vector3)success;
                    }

                    if (position == Vector3.zero)
                        position = ServerMgr.FindSpawnPoint().pos;
                }

                NavMeshHit navHit;
                if (NavMesh.SamplePosition(position, out navHit, 250, 1))              
                    return navHit.position;                
                return position;
            }

            private bool IsValidTarget(BaseEntity target)
            {
                if (target == null || target.IsDestroyed)
                    return false; 

                if (!target.HasAnyTrait(Config.Member.TargetAnimals ? BaseEntity.TraitFlag.Animal | BaseEntity.TraitFlag.Human : BaseEntity.TraitFlag.Human))
                    return false;

                if (target.GetComponent<HordeMember>())
                    return false;

                if (target.Health() <= 0f)
                    return false;

                if (target is BasePlayer)
                {
                    BasePlayer player = target as BasePlayer;

                    if (ins.Vanish != null && (bool)ins.Vanish.Call("IsInvisible", player))
                        return false;

                    if (player.IsFlying)
                        return false;

                    if (player.HasPlayerFlag(BasePlayer.PlayerFlags.SafeZone))
                        return false;
                }

                if (Vector3.Distance(GetAverageVector(), target.transform.position) > Config.Horde.ChaseDistance)
                    return false;

                return true;
            }

            private void TryGrowHorde()
            {     
                float time = Time.realtimeSinceStartup;
                if (time > nextGrowthTime)
                {
                    if (Members.Count < Config.Horde.MaximumSize)
                        AddHordeMember();

                    nextGrowthTime = time + Config.Horde.GrowthRate;
                }
            }

            private void AddHordeMember()
            {
                Vector3 spawnPosition = GetAverageVector();                
                ins.CreateHordeMember(spawnPosition, this);
            }

            private void TryMergeHordes()
            {
                if (!Config.Horde.MergeHordes || JustMergedHorde)
                    return;

                foreach(HordeManager manager in ins.managers)
                {
                    if (manager == this)
                        continue;

                    if (Members.Count >= Config.Horde.MaximumSize)
                        return;

                    HordeMember otherLeader = manager.GetHordeLeader;
                    if (otherLeader == null)
                        continue;

                    if (Vector3.Distance(GetHordeLeader.transform.position, otherLeader.transform.position) < 30)
                    {
                        int amountToMerge = Config.Horde.MaximumSize - Members.Count;
                        if (amountToMerge >= manager.Members.Count)
                        {
                            otherLeader.IsHordeLeader = false;
                            Members.AddRange(manager.Members);
                            manager.Members.Clear();
                        }
                        else
                        {     
                            for (int i = 0; i < amountToMerge; i++)
                            {
                                if (manager.Members.Count > 0)
                                {
                                    HordeMember member = manager.Members[0];
                                    manager.Members.Remove(member);
                                    Members.Add(member);
                                    manager.SetHordeLeader();
                                    manager.JustMergedHorde = true;
                                }
                            }
                        }
                    }
                }                
            }

            public HordeOrder CreateHordeOrder() => new HordeOrder(initialSpawnPoint, localRoam, roamDistance, IsMonumentSpawned);
        }

        private class HordeMember : MonoBehaviour
        {
            public NPCPlayerApex Player { get; private set; }
            public HordeManager Manager { get; set; }
            public bool IsHordeLeader { get; set; }            
            
            public Vector3 targetPosition = Vector3.zero;           

            private void Awake()
            {
                Player = GetComponent<NPCPlayerApex>();
                enabled = false;
                Player.clothingMoveSpeedReduction = -ins.configData.Member.SpeedModifier;
            }

            private void OnDestroy()
            {
                if (Player != null && !Player.IsDestroyed)
                    Player.Kill();
            }

            public void UpdateTargetPosition(Vector3 targetPosition, bool isRegrouping = false)
            {
                if (Player == null || Player.GetNavAgent == null)
                    return;

                Player.AttackTarget = null;
                this.targetPosition = targetPosition;

                Player.finalDestination = targetPosition;
                Player.Destination = targetPosition;
                Player.IsStopped = false;
                Player.SetFact(NPCPlayerApex.Facts.Speed, isRegrouping ? (byte)NPCPlayerApex.SpeedEnum.Sprint : (byte)NPCPlayerApex.SpeedEnum.Walk, true, true);
            }

            public void UpdateTargetEntity(BaseEntity targetEntity)
            {
                if (Player == null || Player.GetNavAgent == null)
                    return;

                if (targetEntity == null)
                {
                    Player.AttackTarget = null;
                    return;
                }

                if (targetEntity == Player.AttackTarget)
                    return;
                
                Player.AttackTarget = targetEntity;
                Player.SetFact(NPCPlayerApex.Facts.Speed, (byte)NPCPlayerApex.SpeedEnum.Sprint, true, true);
            }            
        }

        public class HordeOrder
        {
            public Vector3 position;
            public bool roamLocal;
            public object distanceOverride;
            public bool isMonumentSpawned;

            public HordeOrder(Vector3 position, bool roamLocal, object distanceOverride, bool isMonumentSpawned)
            {
                this.position = position;
                this.roamLocal = roamLocal;
                this.distanceOverride = distanceOverride;
                this.isMonumentSpawned = isMonumentSpawned;
            }
        }

        public class InventoryData
        {
            public List<InventoryItem> items = new List<InventoryItem>();

            public InventoryData(NPCPlayerApex player)
            {
                items = player.inventory.AllItems().Select(item => new InventoryItem
                {
                    itemid = item.info.itemid,
                    amount = item.amount > 1 ? UnityEngine.Random.Range(1, item.amount) : item.amount,
                    ammo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.contents ?? 0,
                    ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                    skin = item.skin,
                    condition = UnityEngine.Random.Range(1, item.condition),
                    instanceData = new InventoryItem.InstanceData(item),
                    contents = item.contents?.itemList.Select(item1 => new InventoryItem
                    {
                        itemid = item1.info.itemid,
                        amount = item1.amount,
                        condition = UnityEngine.Random.Range(1, item1.condition)
                    }).ToArray()
                }).ToList();
            }

            public void RestoreItemsTo(NPCPlayerCorpse corpse)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    Item item = CreateItem(items[i]);
                    item.MoveToContainer(corpse.containers[0]);
                }
            }

            private Item CreateItem(InventoryItem itemData)
            {
                Item item = ItemManager.CreateByItemID(itemData.itemid, itemData.amount, itemData.skin);
                item.condition = itemData.condition;

                if (itemData.instanceData != null)
                    itemData.instanceData.Restore(item);

                BaseProjectile weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    if (!string.IsNullOrEmpty(itemData.ammotype))
                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(itemData.ammotype);
                    weapon.primaryMagazine.contents = itemData.ammo;
                }
                if (itemData.contents != null)
                {
                    foreach (var contentData in itemData.contents)
                    {
                        var newContent = ItemManager.CreateByItemID(contentData.itemid, contentData.amount);
                        if (newContent != null)
                        {
                            newContent.condition = contentData.condition;
                            newContent.MoveToContainer(item.contents);
                        }
                    }
                }
                return item;
            }

            public class InventoryItem
            {
                public int itemid;
                public ulong skin;
                public int amount;
                public float condition;
                public int ammo;
                public string ammotype;
                public InstanceData instanceData;
                public InventoryItem[] contents;

                public class InstanceData
                {
                    public int dataInt;
                    public int blueprintTarget;
                    public int blueprintAmount;

                    public InstanceData() { }
                    public InstanceData(Item item)
                    {
                        if (item.instanceData == null)
                            return;

                        dataInt = item.instanceData.dataInt;
                        blueprintAmount = item.instanceData.blueprintAmount;
                        blueprintTarget = item.instanceData.blueprintTarget;
                    }

                    public void Restore(Item item)
                    {
                        item.instanceData = new ProtoBuf.Item.InstanceData();
                        item.instanceData.blueprintAmount = blueprintAmount;
                        item.instanceData.blueprintTarget = blueprintTarget;
                        item.instanceData.dataInt = dataInt;
                    }
                }
            }           
        }
        #endregion

        #region Commands
        [ChatCommand("horde")]
        private void cmdHorde(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "zombiehorde.admin"))
            {
                SendReply(player, "You do not have permission to use this command");
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, "/horde info - Show position and information about active zombie hordes");
                SendReply(player, "/horde destroy <number> - Destroy the specified zombie horde");
                SendReply(player, "/horde create <opt:distance> - Create a new zombie horde on your position, optionally specifying distance they can roam");
                return;
            }

            switch (args[0].ToLower())
            {
                case "info":
                    int memberCount = 0;
                    int hordeNumber = 0;
                    foreach (HordeManager hordeManager in managers)
                    {
                        player.SendConsoleCommand("ddraw.text", 30, Color.green, hordeManager.GetAverageVector() + new Vector3(0, 1.5f, 0), $"<size=20>Zombie Horde {hordeNumber}</size>");
                        memberCount += hordeManager.Members.Count;
                        hordeNumber++;
                    }

                    SendReply(player, $"There are {managers.Count} active zombie hordes with a total of {memberCount} zombies");
                    return;
                case "destroy":
                    int number;
                    if (args.Length != 2 || !int.TryParse(args[1], out number))
                    {
                        SendReply(player, "You must specify a horde number");
                        return;
                    }

                    if (number < 1 || number > managers.Count)
                    {
                        SendReply(player, "An invalid horde number has been specified");
                        return;
                    }

                    HordeManager manager = managers.ElementAt(number - 1);
                    OnHordeDestroyed(manager);
                    SendReply(player, $"You have destroyed zombie horde {number}");
                    return;
                case "create":
                    if (args.Length >= 2)
                    {
                        float distance;
                        if (float.TryParse(args[1], out distance))
                        {
                            hordeQueue.Enqueue(new HordeOrder(player.transform.position, configData.Horde.RoamLocal, configData.Horde.RoamDistance, false));
                            ProcessHordeOrders(true);

                            SendReply(player, $"You have created a zombie horde order with a roam distance of {distance}");
                        }
                        else SendReply(player, "Invalid Syntax!");
                    }
                    else
                    {
                        hordeQueue.Enqueue(new HordeOrder(player.transform.position, false, null, false));
                        ProcessHordeOrders(true);
                        SendReply(player, "You have created a zombie horde order");
                    }
                    return;
                default:
                    SendReply(player, "Invalid Syntax!");
                    break;
            }            
        }

        [ConsoleCommand("horde")]
        private void ccmdHorde(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                if (!permission.UserHasPermission(arg.Connection.userid.ToString(), "zombiehorde.admin"))
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            if (arg.Args == null || arg.Args.Length == 0)
            {
                SendReply(arg, "horde info - Show position and information about active zombie hordes");
                SendReply(arg, "horde destroy <number> - Destroy the specified zombie horde");
                SendReply(arg, "horde create <opt:distance> - Create a new zombie horde at a random position, optionally specifying distance they can roam from the initial spawn point");
                return;
            }

            switch (arg.Args[0].ToLower())
            {
                case "info":
                    int memberCount = 0;
                    int hordeNumber = 0;
                    foreach (HordeManager hordeManager in managers)
                    {
                        memberCount += hordeManager.Members.Count;
                        hordeNumber++;
                    }

                    SendReply(arg, $"There are {managers.Count} active zombie hordes with a total of {memberCount} zombies");
                    return;
                case "destroy":
                    int number;
                    if (arg.Args.Length != 2 || !int.TryParse(arg.Args[1], out number))
                    {
                        SendReply(arg, "You must specify a horde number");
                        return;
                    }

                    if (number < 1 || number > managers.Count)
                    {
                        SendReply(arg, "An invalid horde number has been specified");
                        return;
                    }

                    HordeManager manager = managers.ElementAt(number - 1);
                    OnHordeDestroyed(manager);
                    SendReply(arg, $"You have destroyed zombie horde {number}");
                    return;
                case "create":
                    if (arg.Args.Length >= 2)
                    {
                        float distance = -1;
                        if (float.TryParse(arg.Args[1], out distance))
                        {
                            hordeQueue.Enqueue(new HordeOrder(Vector3.zero, configData.Horde.RoamLocal, configData.Horde.RoamDistance, false));
                            ProcessHordeOrders(true);

                            SendReply(arg, $"You have created a zombie horde order with a roam distance of {distance}");
                        }
                        else SendReply(arg, "Invalid Syntax!");
                    }
                    else
                    {
                        hordeQueue.Enqueue(new HordeOrder(Vector3.zero, false, null, false));
                        ProcessHordeOrders(true);
                        SendReply(arg, "You have created a zombie horde order");
                    }
                    return;
                default:
                    SendReply(arg, "Invalid Syntax!");
                    break;
            }
        }
        #endregion

        #region Config 
        public enum SpawnSystem { None, Default, SpawnsDatabase, RandomSpawns }
        public enum LootType { Default, Random, Kit }
        private ConfigData configData;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Horde Options")]
            public HordeOptions Horde { get; set; }
            [JsonProperty(PropertyName = "Horde Member Options")]
            public MemberOptions Member { get; set; }
            [JsonProperty(PropertyName = "Loot Table")]
            public LootTable Loot { get; set; }
            [JsonProperty(PropertyName = "Monument Spawn Options")]
            public MonumentSpawn Monument { get; set; }

            public class HordeOptions
            {
                [JsonProperty(PropertyName = "Maximum amount of spawned zombies per horde")]
                public int MaximumSize { get; set; }
                [JsonProperty(PropertyName = "Amount of zombies to spawn when a new horde is created")]
                public int InitialSize { get; set; }
                [JsonProperty(PropertyName = "Amount hordes to create")]
                public int HordeAmount { get; set; }
                [JsonProperty(PropertyName = "Amount of time from when a horde is destroyed until a new horde is created (seconds)")]
                public int RespawnTime { get; set; }
                [JsonProperty(PropertyName = "Amount of time before a horde grows in size")]
                public int GrowthRate { get; set; }
                [JsonProperty(PropertyName = "Add a zombie to the horde when a horde member kills a player")]
                public bool CreateOnDeath { get; set; }
                [JsonProperty(PropertyName = "Maximum distance a horde can be away from a target before losing interest")]
                public float ChaseDistance { get; set; }
                [JsonProperty(PropertyName = "Merge hordes together if they collide")]
                public bool MergeHordes { get; set; }
                [JsonProperty(PropertyName = "Spawn system (Default, SpawnsDatabase, RandomSpawns)")]
                public string SpawnType { get; set; }
                [JsonProperty(PropertyName = "Spawn file (only required when using SpawnsDatabase)")]
                public string SpawnFile { get; set; }
                [JsonProperty(PropertyName = "All hordes will only roam locally in a set distance from their initial spawn point")]
                public bool RoamLocal { get; set; }
                [JsonProperty(PropertyName = "Distance that hordes can roam from their initial spawn point (Local roam hordes only)")]
                public float RoamDistance { get; set; }
            }

            public class MemberOptions
            {
                [JsonProperty(PropertyName = "Zombie damage multiplier")]
                public float DamageMultiplier { get; set; }
                [JsonProperty(PropertyName = "Zombie speed modifier")]
                public float SpeedModifier { get; set; }
                [JsonProperty(PropertyName = "Zombie kits (selected at random per zombie)")]
                public string[] Kits { get; set; }
                [JsonProperty(PropertyName = "Initial health")]
                public float Health { get; set; }
                [JsonProperty(PropertyName = "Members can target animals")]
                public bool TargetAnimals { get; set; }
                [JsonProperty(PropertyName = "Members can be targeted by turrets")]
                public bool TargetedByTurrets { get; set; }
                [JsonProperty(PropertyName = "Members can target scientists")]
                public bool TargetScientists { get; set; }
                [JsonProperty(PropertyName = "Type of loot dropped when killed (Default, Random, Kit)")]
                public string Loot { get; set; }
            }

            public class LootTable
            {               
                [JsonProperty(PropertyName = "Minimum amount of items to spawn")]
                public int Minimum { get; set; }
                [JsonProperty(PropertyName = "Maximum amount of items to spawn")]
                public int Maximum { get; set; }   
                [JsonProperty(PropertyName = "Loot list")]
                public List<LootItem> Items { get; set; }

                public class LootItem
                {
                    [JsonProperty(PropertyName = "Item shortname")]
                    public string Name { get; set; }
                    [JsonProperty(PropertyName = "Item skin ID")]
                    public ulong Skin { get; set; }
                    [JsonProperty(PropertyName = "Minimum amount of item")]
                    public int Minimum { get; set; }
                    [JsonProperty(PropertyName = "Maximum amount of item")]
                    public int Maximum { get; set; }
                }
            }

            public class MonumentSpawn
            {
                public MonumentSettings Airfield { get; set; }
                public MonumentSettings Dome { get; set; }
                public MonumentSettings Junkyard { get; set; }
                public MonumentSettings LargeHarbor { get; set; }
                public MonumentSettings GasStation { get; set; }
                public MonumentSettings Powerplant { get; set; }
                [JsonProperty(PropertyName = "Stone Quarry")]
                public MonumentSettings Quarry_Stone { get; set; }
                [JsonProperty(PropertyName = "Sulfur Quarry")]
                public MonumentSettings Quarry_Sulfur { get; set; }
                [JsonProperty(PropertyName = "HQM Quarry")]
                public MonumentSettings Quarry_HQM { get; set; }
                public MonumentSettings Radtown { get; set; }
                public MonumentSettings RocketFactory { get; set; }
                public MonumentSettings Satellite { get; set; }
                public MonumentSettings SmallHarbor { get; set; }
                public MonumentSettings Supermarket { get; set; }
                public MonumentSettings Trainyard { get; set; }
                public MonumentSettings Tunnels { get; set; }
                public MonumentSettings Warehouse { get; set; }
                public MonumentSettings WaterTreatment { get; set; }
                public class MonumentSettings
                {
                    [JsonProperty(PropertyName = "Enable spawns at this monument")]
                    public bool Enabled { get; set; }
                    [JsonProperty(PropertyName = "This horde can only roam locally")]
                    public bool RoamLocal { get; set; }
                    [JsonProperty(PropertyName = "Distance that this horde can roam from their initial spawn point (Local roam only)")]
                    public float RoamDistance { get; set; }
                }
            }

            public Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Horde = new ConfigData.HordeOptions
                {
                    InitialSize = 5,
                    HordeAmount = 3,
                    MaximumSize = 15,
                    GrowthRate = 300,
                    CreateOnDeath = true,
                    ChaseDistance = 75,
                    MergeHordes = true,
                    RespawnTime = 900,
                    RoamDistance = 50,
                    RoamLocal = false,
                    SpawnType = "Default",
                    SpawnFile = ""
                },
                Member = new ConfigData.MemberOptions
                {
                    Health = 100,
                    Kits = new string[0],
                    DamageMultiplier = 1.0f,
                    TargetAnimals = true,
                    TargetedByTurrets = false,
                    Loot = "Random",
                    TargetScientists = true,
                    SpeedModifier = 0f
                }, 
                Loot = new ConfigData.LootTable
                {
                    Maximum = 4,
                    Minimum = 1,
                    Items = new List<ConfigData.LootTable.LootItem>
                        {
                            new ConfigData.LootTable.LootItem {Name = "apple", Skin = 0, Maximum = 6, Minimum = 2 },
                            new ConfigData.LootTable.LootItem {Name = "bearmeat.cooked", Skin = 0, Maximum = 4, Minimum = 2 },
                            new ConfigData.LootTable.LootItem {Name = "blueberries", Skin = 0, Maximum = 8, Minimum = 4 },
                            new ConfigData.LootTable.LootItem {Name = "corn", Skin = 0, Maximum = 8, Minimum = 4 },
                            new ConfigData.LootTable.LootItem {Name = "fish.raw", Skin = 0, Maximum = 4, Minimum = 2 },
                            new ConfigData.LootTable.LootItem {Name = "granolabar", Skin = 0, Maximum = 4, Minimum = 1 },
                            new ConfigData.LootTable.LootItem {Name = "meat.pork.cooked", Skin = 0, Maximum = 8, Minimum = 4 },
                            new ConfigData.LootTable.LootItem {Name = "candycane", Skin = 0, Maximum = 2, Minimum = 1 }
                        }
                },
                Monument = new ConfigData.MonumentSpawn
                {
                    Airfield = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 85,
                        RoamLocal = true
                    },
                    Dome = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 50,
                        RoamLocal = true
                    },
                    Junkyard = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 50,
                        RoamLocal = true
                    },
                    GasStation = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 15,
                        RoamLocal = true
                    },
                    LargeHarbor = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 120,
                        RoamLocal = true
                    },                   
                    Powerplant = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 120,
                        RoamLocal = true
                    },
                    Quarry_HQM = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 15,
                        RoamLocal = true
                    },
                    Quarry_Stone = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 15,
                        RoamLocal = true
                    },
                    Quarry_Sulfur = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 15,
                        RoamLocal = true
                    },
                    Radtown = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 85,
                        RoamLocal = true
                    },
                    RocketFactory = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 140,
                        RoamLocal = true
                    },
                    Satellite = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 60,
                        RoamLocal = true
                    },
                    SmallHarbor = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 85,
                        RoamLocal = true
                    },
                    Supermarket = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 20,
                        RoamLocal = true
                    },
                    Trainyard = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 100,
                        RoamLocal = true
                    },
                    Tunnels = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 90,
                        RoamLocal = true
                    },
                    Warehouse = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 15,
                        RoamLocal = true
                    },
                    WaterTreatment = new ConfigData.MonumentSpawn.MonumentSettings
                    {
                        Enabled = false,
                        RoamDistance = 120,
                        RoamLocal = true
                    },
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");
            ConfigData baseConfig = GetBaseConfig();

            if (configData.Version < new Core.VersionNumber(0, 1, 1))
            {
                configData.Member.TargetAnimals = baseConfig.Member.TargetAnimals;
                configData.Member.TargetedByTurrets = baseConfig.Member.TargetedByTurrets;
                configData.Horde.SpawnType = baseConfig.Horde.SpawnType;
                configData.Horde.SpawnFile = baseConfig.Horde.SpawnFile;
            }

            if (configData.Version < new Core.VersionNumber(0, 1, 5))
            {
                configData.Horde.RoamLocal = baseConfig.Horde.RoamLocal;
                configData.Horde.RoamDistance = baseConfig.Horde.RoamDistance;
                configData.Monument = baseConfig.Monument;
            }

            if (configData.Version < new Core.VersionNumber(0, 1, 6))
            {
                configData.Member.Loot = baseConfig.Member.Loot;
                configData.Loot = baseConfig.Loot;
            }

            if (configData.Version < Version)
                configData.Member.SpeedModifier = baseConfig.Member.SpeedModifier;

            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

       
    }
}
