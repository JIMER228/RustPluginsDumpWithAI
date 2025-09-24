using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.AI;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("ZombieHorde", "Автор k1lly0u", "0.1.4", ResourceId = 0)]
      //  Слив плагинов server-rust by Apolo YouGame
    class ZombieHorde : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin Kits, Vanish, RandomSpawns, Spawns;

        private static ZombieHorde ins; 
        private List<HordeManager> managers = new List<HordeManager>();

        private SpawnSystem spawnSystem = SpawnSystem.None;
        private List<Vector3> spawnPoints = new List<Vector3>();

        const string zombiePrefab = "assets/prefabs/npc/murderer/murderer.prefab";
        #endregion

        #region Oxide Hooks        
        private void OnServerInitialized()
        {
            ins = this;
            permission.RegisterPermission("zombiehorde.admin", this);

            if (!configData.Member.TargetedByTurrets)
                Unsubscribe(nameof(CanBeTargeted));

            if (!VerifySpawnSystem())
                return;

            CreateHordes();
        }

        private void Unload()
        {
            for (int i = managers.Count - 1; i >= 0; i--)            
                UnityEngine.Object.Destroy(managers.ElementAt(i));

            ins = null;
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
      //  Слив плагинов server-rust by Apolo YouGame
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
      //  Слив плагинов server-rust by Apolo YouGame
        {
            if (entity == null)
                return;

            HordeMember hordeMember = entity.GetComponent<HordeMember>();
            if (hordeMember != null)
            {
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
            if (hordeMember != null)
            {
                if (!target.HasAnyTrait(configData.Member.TargetAnimals ? BaseEntity.TraitFlag.Animal | BaseEntity.TraitFlag.Human : BaseEntity.TraitFlag.Human))                
                    return false;
                
                if (target.GetComponent<HordeMember>())
                    return false;
                
                if (target.Health() <= 0f)
                    return false;

                if (target is BasePlayer)
                {
                    BasePlayer player = target as BasePlayer;
                    
                    if (Vanish != null && (bool)Vanish.Call("IsInvisible", player))
                        return null;

                    if (configData.Member.TargetScientists && player is Scientist)
                        return false;

                    if (player.IsFlying)
                        return false;

                    if (player.HasPlayerFlag(BasePlayer.PlayerFlags.SafeZone))
                        return false;
                }

                if (Vector3.Distance(npcPlayer.transform.position, target.transform.position) > configData.Horde.ChaseDistance)
                    return false;

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
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(targetPosition, out navHit, 100, 1))            
                return navHit.position;
            return null;
        }

        private void CreateHordes() => ServerMgr.Instance.StartCoroutine(CreateZombieHorde(configData.Horde.HordeAmount, default(Vector3)));        

        private IEnumerator CreateZombieHorde(int amount = 1, Vector3 position = default(Vector3))
        {
            for (int i = 0; i < amount; i++)
            {
                Vector3 spawnPosition = default(Vector3);

                if (position == default(Vector3))
                {
                    object success = GetSpawnPoint();
                    if (success is Vector3)
                        spawnPosition = (Vector3)success;
                    else yield break;
                }

                HordeManager manager = new GameObject().AddComponent<HordeManager>();
                manager.Initialize(configData);
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
            }
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
            player.enableSaving = false;
            player.Spawn();

            player.InitializeHealth(configData.Member.Health, configData.Member.Health);
            player.Stats.AggressionRange = player.Stats.DeaggroRange = configData.Horde.ChaseDistance;

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
            managers.Remove(manager);
            UnityEngine.Object.Destroy(manager);

            timer.In(configData.Horde.RespawnTime, () =>
            {
                if (managers.Count < configData.Horde.HordeAmount)
                    ServerMgr.Instance.StartCoroutine(CreateZombieHorde());
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
           
            private BaseEntity targetEntity = null;
            private Vector3 targetPosition;

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

                if (Vector3.Distance(averageVector, targetEntity.transform.position) > Config.Horde.ChaseDistance)
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
      //  Слив плагинов server-rust by Apolo YouGame
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

                if (ins.RandomSpawns)
                {
                    object success = ins.RandomSpawns.Call("GetSpawnPoint");
                    if (success is Vector3)
                        position = (Vector3)success;
                }

                if (position == Vector3.zero)
                    position = ServerMgr.FindSpawnPoint().pos; 

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
            }

            private void OnDestroy()
            {
                if (Player != null && !Player.IsDestroyed)
                    Player.Kill();
            }

            public void UpdateTargetPosition(Vector3 targetPosition, bool isRegrouping = false)
            {
                Player.AttackTarget = null;
                this.targetPosition = targetPosition;

                Player.finalDestination = targetPosition;
                Player.Destination = targetPosition;
                Player.IsStopped = false;
                Player.SetFact(NPCPlayerApex.Facts.Speed, isRegrouping ? (byte)NPCPlayerApex.SpeedEnum.Sprint : (byte)NPCPlayerApex.SpeedEnum.Walk, true, true);
            }

            public void UpdateTargetEntity(BaseEntity targetEntity)
            {
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
        #endregion

        #region Commands
        [ChatCommand("zh")]
        private void cmdHorde(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "zombiehorde.admin"))
            {
                SendReply(player, "У вас нет права на использование данной команды");
                return;
            }

            if (args.Length == 0)
            {
                SendReply(player, "/zh info - Показать информацию о местоположении зомби");
                SendReply(player, "/zh destroy <номер> - Убить определенную орду зомби");
                SendReply(player, "/zh create - Создать новую позицию для появления зомби");
                return;
            }

            switch (args[0].ToLower())
            {
                case "info":
                    int memberCount = 0;
                    int hordeNumber = 1;
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
                    ServerMgr.Instance.StartCoroutine(CreateZombieHorde(1, player.transform.position));
                    SendReply(player, "You have successfully created a new zombie horde");
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
                SendReply(arg, "horde info - Показать позицию и количество активных зомби");
                SendReply(arg, "horde destroy <номер> - Уничтожить определенную орду");
                SendReply(arg, "horde create - Создать зомби в рандомном месте");
                return;
            }

            switch (arg.Args[0].ToLower())
            {
                case "info":
                    int memberCount = 0;
                    int hordeNumber = 1;
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
                    ServerMgr.Instance.StartCoroutine(CreateZombieHorde());
                    SendReply(arg, "You have successfully created a new zombie horde");
                    return;
                default:
                    SendReply(arg, "Invalid Syntax!");
                    break;
            }
        }
        #endregion

        #region Config 
        public enum SpawnSystem { None, Default, SpawnsDatabase, RandomSpawns }
        private ConfigData configData;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Horde Options")]
            public HordeOptions Horde { get; set; }
            [JsonProperty(PropertyName = "Horde Member Options")]
            public MemberOptions Member { get; set; }

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
            }

            public class MemberOptions
            {
                [JsonProperty(PropertyName = "Zombie damage multiplier")]
                public float DamageMultiplier { get; set; }               
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
                    SpawnType = "Default",
                    SpawnFile = ""
                },
                Member = new ConfigData.MemberOptions
                {
                    Health = 100,
                    Kits = new string[0],                    
                    DamageMultiplier = 1.0f,
                    TargetAnimals = true,
                    TargetedByTurrets = false
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
            configData.Version = Version;
            PrintWarning("Config update completed.");
        }

        #endregion

       
    }
}
