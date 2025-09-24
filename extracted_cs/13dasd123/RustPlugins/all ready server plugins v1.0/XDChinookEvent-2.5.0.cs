using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using VLB;

using static NPCPlayerApex;

namespace Oxide.Plugins
{
    [Info("XDChinookEvent", "TopPlugin.ru", "2.5.0")]
    [Description("Авто ивент особый груз")]
    public class XDChinookEvent : RustPlugin
    {
        private static XDChinookEvent _;
        [PluginReference] Plugin IQChat, IQEconomic, RustMap;

        #region Var
        Vector3 vector;
        private static string langserver = "ru";
        private const int LAND_LAYERS = 1 << 4 | 1 << 8 | 1 << 16 | 1 << 21 | 1 << 23;
        static double CurrentTime() => Facepunch.Math.Epoch.Current;
        public int InitStart;
        public List<CH47Helicopter> ListHeli = new List<CH47Helicopter>();

        #endregion

        #region Lang

        private new void LoadDefaultMessages()
        {
             lang.RegisterMessages(new Dictionary<string, string>
             {
                 ["ChinookBuild"] = "<color=#E66062>You cannot build in the event area</color>",
                 ["ChinookStart"] = "<color=#608FE6>Chinook Special Cargo</color> already poured on the dumping site.\nReset square <color=#6ACB52>{0}</color>",
                 ["ChinookIsDead"] = "<color=#608FE6>Chinook Special Cargo</color> crashed!!!",
                 ["ChinookFinish"] = "<color=#608FE6>Chinook Special Cargo</color> dumping device (square <color=#6ACB52>{0}</color>),left to wait <color=#3DA9FF>{1}</color>  \nMap mark G",
                 ["ChinookIsDrop"] = "<color=#608FE6>Chinook Special Cargo</color> dropped a very valuable load (square <color=#6ACB52>{0}</color>)\n Map mark G",
                 ["ChinookIsDropHack"] = "<color=#FFAB3D>Special someone started to crack the load!</color>\nYou have 15 minutes to open it\nSquare (<color=#6ACB52>{0}</color>) Map mark G",
                 ["ChinookIsDropHackEnd"] = "<color=#FFAB3D>Special cargo hacked but not tarnished!</color>",
                 ["ChinookIsDropHackEndLoot"] = "Special cargo tied up by player <color=#6ACB52>{0}</color>.",
                 ["Chinookcmd1"] = "You don't have enough rights!",
                 ["Chinookcmd2"] = "/chinook addspawnpoint - Adds custom chinook spawn position\n/chinook call - Summon the chinook prematurely",
                 ["Chinookcmd3"] = "Point added successfully",
                 ["Chinookcmd4"] = "Event is already active!",
                 ["Chinookcmd5"] = "You summoned a chinook",
                 ["ChinookEventTime"] = "The event will start in {0}",
             }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ChinookBuild"] = "<color=#E66062>Вы не можете строить в зоне ивента</color>",
                ["ChinookStart"] = "<color=#608FE6>Чинук с особым грузом</color> уже вылител на место сброса.\nКвадрат сброса <color=#6ACB52>{0}</color>\nБудте осторожны! Его охраняет патруль",
                ["ChinookIsDead"] = "<color=#608FE6>Чинук с особым грузом</color> потерпел крушения!!!",
                ["ChinookFinish"] = "<color=#608FE6>Чинук с особым грузом</color> прилител на место сброса (квадрат <color=#6ACB52>{0}</color>), осталось подождать <color=#3DA9FF>{1}</color>  \n Метка на карте G",
                ["ChinookIsDrop"] = "<color=#608FE6>Чинук с особым грузом</color> сбросил очень ценный груз (квадрат <color=#6ACB52>{0}</color>)\n Метка на карте G",
                ["ChinookIsDropHack"] = "<color=#FFAB3D>Особый груз кто то начал взламывать!</color>\nУ тебя есть 15 минут до его открытие\nКвадрат (<color=#6ACB52>{0}</color>) Метка на карте G",
                ["ChinookIsDropHackEnd"] = "<color=#FFAB3D>Особый груз взломан но не залутан!</color>",
                ["ChinookIsDropHackEndLoot"] = "Особый груз залутан игроком <color=#6ACB52>{0}</color>.",
                ["Chinookcmd1"] = "У вас недостаточно прав!",
                ["Chinookcmd2"] = "/chinook addspawnpoint - Добавляет кастомную позицию для спавна чинука\n/chinook call - Вызвать чинук преждевременно",
                ["Chinookcmd3"] = "Точка успешно добавлена",
                ["Chinookcmd4"] = "Ивент уже активен!",
                ["Chinookcmd5"] = "Вы вызвали чинук",
                ["ChinookEventTime"] = "Ивент будет запущен через {0}",
            }, this, "ru");
        }

        #endregion

        #region Configuration
        private Configuration config;
        private class Configuration
        {
            public class ItemsDrop
            {
                [JsonProperty("Short name")]
                public string Shortname;
                [JsonProperty("Skin ID ")]
                public ulong SkinID;
                [JsonProperty("item name")]
                public string DisplayName;
                [JsonProperty("BluePrint? | blueprint ?")]
                public bool BluePrint;
                [JsonProperty("минимальное количество | minimal amountл")]
                public int MinimalAmount;
                [JsonProperty("максимальное количество | maximum amount")]
                public int MaximumAmount;
                [JsonProperty("Шанс выпадения предмета | Item drop chance")]
                public int DropChance;
            }

            public class ChinookSetings
            {
                [JsonProperty("Время разблокировки ящика (Сек) | Box unlocking time (Sec)")]
                public int unBlockTime = 900;
                [JsonProperty("Настройка плавности/скорости спуска ящика | Adjusting the smoothness / speed of the drawer descent")]
                public float gravity = 0.7f;
                [JsonProperty("Радиус запрета построек во время ивента | Block radius of buildings during the event")]
                public int radius = 65;
                [JsonProperty("Время ожидания сброса | Reset timeout")]
                public int TimeStamp = 60;
                [JsonProperty("Время до самоуничтожения (Если никто не начнет взламывать) | Time to self-destruct (If no one starts hacking)")]
                public int CrateDestroy = 600;
                [JsonProperty("Минимальное колличевство игроков для запуска ивента | Minimum number of players to start an event")]
                public int PlayersMin = 20;
                [JsonProperty("Раз в сколько часов будет летать чинук | Once in how many hours a chinook will fly")]
                public int TimeChinook = 7200;
                [JsonProperty("Максимум предметов в 1 ящике | Maximum items in 1 box")]
                public int MaxItem = 7;
                [JsonProperty("Использовать стандартный лут в ящике (Если false, то будет выпадать ваш лут) | Use standard loot in the box (If false, your loot will drop out)")]
                public bool customLoot = true;
                [JsonProperty("Использовать кастомные позиции ? | Use custom positions?")]
                public bool useCustomPos = false;
                [JsonProperty("Кастомные позиции (/chinook addspawnpoint) | Custom positions (/ chinook addspawnpoint)")]
                public List<Vector3> customPos = new List<Vector3>();
            }

            public class NpcChinook
            {
                [JsonProperty("Спавнить нпс возле ящика ? | Spawn NPCs near a crate?")]
                public bool npcUse = true;
                [JsonProperty("Количество нпс | Number of npc")]
                public int npcCount = 6;
                [JsonProperty("Хп нпс | HP npc")]
                public int healtNpc = 150;
                [JsonProperty("Рандомные ники нпс | Random nicknames npc")]
                public List<string> nickName = new List<string>();
            }

            public class MapMarker
            {
                [JsonProperty("Создавать маркер на G карте ? | Create a marker on the G map?")]
                public bool markerUseG = true;
                [JsonProperty("Названия маркера | Marker names")]
                public string nameMarkerG = "Особый груз";
                [JsonProperty("Цвет маркера | Marker color")]
                public string colorMarkerG = "#54bbb4";
                [JsonProperty("Прзрачность маркера | Marker transparency")]
                public float alfaMarker = 0.4f;
                [JsonProperty("Метки на карте RustMap | RustMap labels")]
                public bool rustMapUse = false;
                [JsonProperty("Картинка для метки карты RustMap | Image for RustMap map label")]
                public string imageForRustMap = "https://i.imgur.com/x6qoCaK.png";
            }

            public class iqEconomic
            {
                [JsonProperty("Использовать выдачу баланса IQEconomic? | Use the IQEconomic balance sheet?")]
                public bool use = false;
                [JsonProperty("минимальное количество монет | minimum number of coins")]
                public int MinimalAmount = 3;
                [JsonProperty("максимальное количество монет | maximum number of coins")]
                public int MaximumAmount = 7;
                [JsonProperty("Шанс выпадения монеты | Coin drop chance")]
                public int chance = 90;
            }

            [JsonProperty("Настройки | Settings")]
            public ChinookSetings chinook = new ChinookSetings();
            [JsonProperty("Связка с IQEconomic | Link with IQEconomic")]
            public iqEconomic iqEconomics = new iqEconomic();
            [JsonProperty("Настройка отображения на картах | Configuring display on maps")]
            public MapMarker mapMarker = new MapMarker();
            [JsonProperty("Настройка нпс | Setting up npc")]
            public NpcChinook npcChinook = new NpcChinook();
            [JsonProperty("Выпадаемые предметы | Drop items")]
            public List<ItemsDrop> itemsDrops = new List<ItemsDrop>();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                    throw new Exception();
                SaveConfig();
            }
            catch
            {
                for (var i = 0; i < 3; i++)
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                LoadDefaultConfig();
            }
            ValidateConfig();
            SaveConfig();
        }

        private void ValidateConfig()
        {
            if (config.npcChinook.nickName.Count == 0)
            {
                config.npcChinook.nickName = new List<string>
                {
                    "Patrol"
                };
            }
            if (config.itemsDrops.Count == 0)
            {
                config.itemsDrops = new List<Configuration.ItemsDrop>
                {
                    new Configuration.ItemsDrop{Shortname = "halloween.surgeonsuit", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 1, DropChance = 70 },
                    new Configuration.ItemsDrop{Shortname = "metal.facemask", SkinID = 1886184322, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 1, DropChance = 20 },
                    new Configuration.ItemsDrop{Shortname = "door.double.hinged.metal", SkinID = 191100000, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 2, DropChance = 60 },
                    new Configuration.ItemsDrop{Shortname = "rifle.bolt", SkinID = 0, DisplayName = "", BluePrint = true, MinimalAmount = 1, MaximumAmount = 1, DropChance = 10 },
                    new Configuration.ItemsDrop{Shortname = "rifle.lr300", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 1, DropChance = 15 },
                    new Configuration.ItemsDrop{Shortname = "pistol.revolver", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 3, DropChance = 60 },
                    new Configuration.ItemsDrop{Shortname = "supply.signal", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 3, DropChance = 20 },
                    new Configuration.ItemsDrop{Shortname = "explosive.satchel", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 3, DropChance = 5 },
                    new Configuration.ItemsDrop{Shortname = "grenade.smoke", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 1, MaximumAmount = 20, DropChance = 45 },
                    new Configuration.ItemsDrop{Shortname = "ammo.rifle", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 50, MaximumAmount = 120, DropChance = 35 },
                    new Configuration.ItemsDrop{Shortname = "scrap", SkinID = 0, DisplayName = "", BluePrint = false, MinimalAmount = 100, MaximumAmount = 500, DropChance = 20 },
                    new Configuration.ItemsDrop{Shortname = "giantcandycanedecor", SkinID = 2477, DisplayName = "Новый год", BluePrint = false, MinimalAmount = 1, MaximumAmount = 5, DropChance = 70 },
                };
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }
        #endregion

        #region Classes 

        #region Method By OxideBro
        Vector3 RandomCircle(Vector3 center, float radius = 2)
        {
            float ang = UnityEngine.Random.value * 360;
            Vector3 pos;
            pos.x = center.x + radius * Mathf.Sin(ang * Mathf.Deg2Rad);
            pos.z = center.z + radius * Mathf.Cos(ang * Mathf.Deg2Rad); 
            pos.y = center.y; pos.y = GetGroundPosition(pos);
            
            return pos;
        }
        static float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos); RaycastHit hit; 

            if (Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask(new[] { "Terrain", "World", "Default", "Construction", "Deployed" })) && !hit.collider.name.Contains("rock_cliff")) return Mathf.Max(hit.point.y, y);
            return y;
        }

        const string scientist = "assets/prefabs/npc/scientist/scientist.prefab";
        private NPCPlayerApex InstantiateEntity(string type, Vector3 position)
        {

            position.y = GetGroundPosition(position);
            var gameObject = Instantiate.GameObject(GameManager.server.FindPrefab(type), position, new Quaternion());
            gameObject.name = type; SceneManager.MoveGameObjectToScene(gameObject, Rust.Server.EntityScene);
            UnityEngine.Object.Destroy(gameObject.GetComponent<Spawnable>());
            if (!gameObject.activeSelf) 
                gameObject.SetActive(true);

            NPCPlayerApex component = gameObject.GetComponent<NPCPlayerApex>();
            return component;
        }
        void SpawnBots( HackableLockedCrate hack)
        {
            for (int i = 0; i < config.npcChinook.npcCount; i++)
            {

                NPCPlayerApex entity = null; 
                entity = InstantiateEntity(scientist, RandomCircle(hack.transform.position, 10));
                entity.enableSaving = false;
                entity.Spawn();
                entity.IsInvinsible = false;
                entity.startHealth = config.npcChinook.healtNpc;
                entity.InitializeHealth(entity.startHealth, entity.startHealth);
                entity.Stats.AggressionRange = entity.Stats.DeaggroRange = 150;
                entity.CommunicationRadius = 0;
                entity.displayName = config.npcChinook.nickName.GetRandom();
                entity.GetComponent<Scientist>().LootPanelName = entity.displayName;
                entity.CancelInvoke(entity.EquipTest);
                Equip(entity);
                entity.Stats.MaxRoamRange = 75f;
                entity.NeverMove = true;

                NPCMonitor npcMonitor = entity.gameObject.AddComponent<NPCMonitor>();
                npcMonitor.Initialize(hack);
            }
        }
        public HeldEntity GetFirstWeapon(BasePlayer player)
        {
            foreach (Item item in player.inventory.containerBelt.itemList)
            {
                if (item.CanBeHeld() && (item.info.category == ItemCategory.Weapon))
                {
                    BaseProjectile projectile = item.GetHeldEntity() as BaseProjectile; if (projectile != null)
                    {
                        projectile.primaryMagazine.contents = projectile.primaryMagazine.capacity;
                        projectile.SendNetworkUpdateImmediate(); 
                         return item.GetHeldEntity() as HeldEntity;
                    }
                }
            }
            return null;
        }
        private void Equip(BasePlayer player)
        {
            HeldEntity weapon1 = GetFirstWeapon(player); if (weapon1 != null)
            {
                weapon1.SetHeld(true);
            }
        }
        public class NPCMonitor : FacepunchBehaviour
        {
            public NPCPlayerApex player { get; private set; }
            private List<Vector3> patrolPositions = new List<Vector3>();
            private HackableLockedCrate hacks;
            private Vector3 homePosition;
            private int lastPatrolIndex = 0;
            private void Awake()
            {
                player = GetComponent<NPCPlayerApex>();
                InvokeRepeating(UpdateDestination, 0f, 5.0f);
            }

            public void Initialize(HackableLockedCrate hack)
            {
                this.hacks = hack;
                homePosition = hacks.transform.position;
                GeneratePatrolPositions();
            }

            private void UpdateDestination()
            {
                if (player.AttackTarget == null)
                {
                    player.NeverMove = true;
                    float distance = (player.transform.position - homePosition).magnitude;
                    bool tooFar = distance > 20;

                    if (player.GetNavAgent == null || !player.GetNavAgent.isOnNavMesh)
                        player.finalDestination = patrolPositions[lastPatrolIndex];
                    else
                    {
                        if (Vector3.Distance(player.transform.position, patrolPositions[lastPatrolIndex]) < 5)
                            lastPatrolIndex++;
                        if (lastPatrolIndex >= patrolPositions.Count)
                            lastPatrolIndex = 0;
                        player.SetDestination(patrolPositions[lastPatrolIndex]);
                    }

                    player.SetDestination(patrolPositions[lastPatrolIndex]);
                    player.SetFact(NPCPlayerApex.Facts.Speed, tooFar ? (byte)NPCPlayerApex.SpeedEnum.Run : (byte)NPCPlayerApex.SpeedEnum.Walk, true, true);
                }
                else
                {
                    player.NeverMove = false;
                    player.IsStopped = false;

                    var attacker = player.AttackTarget as BasePlayer;
                    if (attacker == null)
                        return;

                    if (attacker.IsDead())
                        Forget();
                }
            }
            private void Forget()
            {
                player.lastDealtDamageTime = Time.time - 21f;
                player.SetFact(Facts.HasEnemy, 0, true, true);
                player.SetFact(Facts.EnemyRange, 3, true, true);
                player.SetFact(Facts.AfraidRange, 1, true, true);
                player.AiContext.EnemyNpc = null;
                player.AiContext.EnemyPlayer = null;
                player.AttackTarget = null;
                player.lastAttacker = null;
                player.lastAttackedTime = Time.time - 31f;
                player.LastAttackedDir = Vector3.zero;
                player.SetDestination(patrolPositions[lastPatrolIndex]);
            }
            private void OnDestroy()
            {
                if(player != null)
                    player.KillMessage();
                Destroy(gameObject);
            }
            private void GeneratePatrolPositions()
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector3 position = hacks.transform.position + (UnityEngine.Random.onUnitSphere * 20f);
                    position.y = TerrainMeta.HeightMap.GetHeight(position);
                    patrolPositions.Add(position);
                }
                enabled = true;
            }
        }
        #endregion


        private class ForceCrate : FacepunchBehaviour
        {
            public HackableLockedCrate crate;
            public bool isGrounded;
            public Vector3 groundPos;
            private BaseEntity chute;
            private float deathTime;
            private float speed = 1;
            private void Awake()
            {
                crate = GetComponent<HackableLockedCrate>();
                groundPos = new Vector3(crate.transform.position.x, TerrainMeta.HeightMap.GetHeight(crate.transform.position), crate.transform.position.z);
                deathTime = UnityEngine.Time.time + _.config.chinook.CrateDestroy;
                speed = _.config.chinook.gravity;
                isGrounded = false;
                InitChute();
            }


            private void Update()
            {
                
                if (!isGrounded)
                {
                    if (Physics.Raycast(new Ray(crate.transform.position, Vector3.down), 2f, LAND_LAYERS))
                    {
                        OnLanded();
                        return;
                    }
                    crate.transform.position = new Vector3(crate.transform.position.x, crate.transform.position.y - 0.015f * speed, crate.transform.position.z);
    
                }

                if (UnityEngine.Time.time >= deathTime)
                {
                    if (crate.HasFlag(BaseEntity.Flags.Reserved1))
                    {
                        deathTime += +320;
                        return;
                    }
                    _.DestroyAll<NPCMonitor>();
                    crate.Kill();
                    Destroy(this);
                    return;
                }
            }

            private void OnDestroy()
            {
                chute?.Kill();
                crate?.Kill();
                _.RemoveMarker();
            }

            private void OnLanded()
            {
                isGrounded = true;
                transform.position = groundPos;
                crate.GetComponent<Rigidbody>().useGravity = true;
                crate.GetComponent<Rigidbody>().drag  = 1.5f;
                crate.GetComponent<Rigidbody>().isKinematic  = false;
                chute?.Kill();
                if(_.config.npcChinook.npcUse)
                    _.SpawnBots(crate);
            }

            private void InitChute()
            {
                chute = GameManager.server.CreateEntity("assets/prefabs/misc/parachute/parachute.prefab", crate.transform.position);
                chute.enableSaving = false;
                chute.Spawn();

                chute.SetParent(crate, false);
                chute.transform.localPosition = Vector3.up;

                Effect.server.Run("assets/bundled/prefabs/fx/smoke_signal_full.prefab", chute, 0, Vector3.zero, Vector3.zero, null, true);
            }
        }
        #endregion

        #region Commands
        [ChatCommand("chinook")]
        void ChinookCommands(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                SendChatPlayer(GetLang("Chinookcmd1", player.UserIDString), player);
                return;
            }
            if (args.Length == 0)
            {
                SendChatPlayer(GetLang("Chinookcmd2", player.UserIDString), player);
                return;
            }
            switch (args[0])
            {
                case "addspawnpoint":
                    {
                        config.chinook.customPos.Add(player.transform.position);
                        SendChatPlayer(GetLang("Chinookcmd3", player.UserIDString), player);
                        SaveConfig();
                        break;
                    }
                case "call":
                    {                        
                        if (ListHeli.Count >= 1)
                        {
                            if (player != null)
                                SendChatPlayer(GetLang("Chinookcmd4", player.UserIDString), player);
                            return;
                        }
                        else
                        {
                            ChinookHelicopterCall();
                            SendChatPlayer(GetLang("Chinookcmd5", player.UserIDString), player);
                        }
                        break;
                    }
            }
        }
        #endregion

        #region MainMetods
        private void ChinookHelicopterCall()
        {
            #region RandomSpawnPosition
            float x = TerrainMeta.Size.x;
            float y = 70f;
            Vector3 val = Vector3Ex.Range(-1f, 1f);
            val.y = 0f;
            val.Normalize();
            val *= x * 1f;
            val.y = y;
            #endregion

            CH47Helicopter ChinookEnt = GameManager.server.CreateEntity("assets/prefabs/npc/ch47/ch47scientists.entity.prefab", val, new Quaternion(0f, 0f, 0f, 0f), true) as CH47Helicopter;//////////////////
            if (ChinookEnt == null) return;
            CH47HelicopterAIController ChinookAI = ChinookEnt.GetComponent<CH47HelicopterAIController>();
            ChinookAI.OwnerID = 112234;
            ChinookAI.Spawn();
            ChinookAI.SetMaxHealth(900000f);
            ChinookAI.health = 900000f;
            ChinookAI.Heal(900000f);
            ListHeli.Add(ChinookEnt);
            vector = config.chinook.useCustomPos ? config.chinook.customPos.GetRandom() : (Vector3)GetSpawnPoint();

            string Cvadrat = "";
            Cvadrat = GetGridString(vector);
            ChinookAI.SetMoveTarget(vector);
            #region Метки на карте
            if (config.mapMarker.markerUseG)
                CreateMarker(vector, 5, config.mapMarker.nameMarkerG, config.mapMarker.nameMarkerG);
            if (config.mapMarker.rustMapUse)
                RustMap?.Call("ApiAddPointUrl", config.mapMarker.imageForRustMap, Name, vector, config.mapMarker.nameMarkerG);
            #endregion
            SendChatAll(GetLang("ChinookStart", null, Cvadrat));
            Subscribe(nameof(CanBuild));

            ChinookAI.SetLandingTarget(new Vector3(vector.x, TerrainMeta.HeightMap.GetHeight(vector) + 90f, vector.z));
            Timer timers = null;
            Vector3 CheckPos = Vector3.zero;
            bool PositionOutCrate = false;
            int i = 0;

            timers = timer.Every(10 , () =>
            {
                if (ChinookAI.IsDead())
                {
                    SendChatAll(GetLang("ChinookIsDead", null));
                    PrintWarning("Cninook is dead");
                    RemoveMarkers();
                    Unsubscribe(nameof(CanBuild));
                    ListHeli.Remove(ChinookAI);
                    timers.Destroy();
                    return;
                } 

                if (Vector3.Distance(ChinookAI.transform.position.xz(), vector.xz()) < 10f)
                {
                    PositionOutCrate = true;
                    SendChatAll(GetLang("ChinookFinish", null, Cvadrat, FormatTime(TimeSpan.FromSeconds(config.chinook.TimeStamp), language: langserver))) ;
                    timers.Destroy();
                    timer.Once(config.chinook.TimeStamp, () => {
                        SendChatAll(GetLang("ChinookIsDrop", null, Cvadrat));

                        Vector3 pos = ChinookAI.transform.position + Vector3.down * 5f;
                        CreateCrate(pos);
                        ChinookAI.ClearLandingTarget();
                        ChinookAI.DestroyShared();
                    });      
                }

                #region Костыль века
                if(i > 5 && !PositionOutCrate && Vector3.Distance(CheckPos, ChinookAI.transform.position) < 7f)
                {
                    SendChatAll(GetLang("ChinookIsDead", null));
                    Unsubscribe(nameof(CanBuild));
                    RemoveMarkers();
                    ChinookAI.Kill();
                    ListHeli.Remove(ChinookAI);
                    timers.Destroy();
                    return;
                }

                if (i > 5)
                    CheckPos = ChinookAI.transform.position;
                i++;
                #endregion

            });
        }

        private void CreateCrate(Vector3 position)
        {
            Quaternion rot = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            HackableLockedCrate CrateEnt = GameManager.server.CreateEntity("assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate.prefab", position, rot, true) as HackableLockedCrate;
            CrateEnt.enableSaving = false;
            CrateEnt.GetComponent<Rigidbody>().useGravity = false;
            CrateEnt.Spawn();

            CrateEnt.OwnerID = 12436;

            ForceCrate crate = CrateEnt.gameObject.AddComponent<ForceCrate>();
            crate.crate = CrateEnt;
            if (!config.chinook.customLoot)
            {
                CrateEnt.inventory.itemList.Clear();
                for (int i = 0; i < config.itemsDrops.Count; i++)
                {
                    var cfg = config.itemsDrops[i];
                    bool goodChance = UnityEngine.Random.Range(0, 100) >= (100 - cfg.DropChance);
                    if (goodChance && CrateEnt.inventory.itemList.Count <= config.chinook.MaxItem)
                    {
                        if (cfg.BluePrint)
                        {
                            var bp = ItemManager.Create(ItemManager.blueprintBaseDef);
                            bp.blueprintTarget = ItemManager.FindItemDefinition(cfg.Shortname).itemid;
                            bp.MoveToContainer(CrateEnt.inventory);
                        }
                        else
                        {
                            Item GiveItem = ItemManager.CreateByName(cfg.Shortname, Oxide.Core.Random.Range(cfg.MinimalAmount, cfg.MaximumAmount), cfg.SkinID);
                            if (!string.IsNullOrEmpty(cfg.DisplayName)) { GiveItem.name = cfg.DisplayName; }
                            GiveItem.MoveToContainer(CrateEnt.inventory);
                        }
                    }
                }
                if (config.iqEconomics.use && IQEconomic)
                {
                    bool TypeMoney = (bool)IQEconomic?.Call("API_MONEY_TYPE");
                    if (TypeMoney)
                    {
                        bool goodChance = UnityEngine.Random.Range(0, 100) >= (100 - config.iqEconomics.chance);
                        if (goodChance)
                        {
                            Item money = (Item)IQEconomic?.Call("API_GET_ITEM", UnityEngine.Random.Range(config.iqEconomics.MinimalAmount, config.iqEconomics.MaximumAmount));
                            money.MoveToContainer(CrateEnt.inventory);
                        }
                    }
                }
                
            }
            CrateEnt.inventory.capacity = CrateEnt.inventory.itemList.Count;
            CrateEnt.inventory.MarkDirty();
            CrateEnt.SendNetworkUpdate();
            CrateEnt.hackSeconds = HackableLockedCrate.requiredHackSeconds - config.chinook.unBlockTime;
        }
        #endregion

        #region Hooks
        private object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            if (Vector3.Distance(planner.transform.position, vector) < config.chinook.radius)
            {
                BasePlayer player = planner?.GetOwnerPlayer();
                if (player != null)
                    SendChatPlayer(GetLang("ChinookBuild", player.UserIDString), player);
                return false;
            }
            return null;
        }
        void OnCrateHack(HackableLockedCrate crate)
        {
            if(crate.OwnerID == 12436)
            { 
                SendChatAll(GetLang("ChinookIsDropHack",null, GetGridString(crate.transform.position)));
            }
        }

        void OnCrateHackEnd(HackableLockedCrate crate)
        {
            if (crate.OwnerID == 12436)
            {
                SendChatAll(GetLang("ChinookIsDropHackEnd", null));
            }
        }

        void CanLootEntity(BasePlayer player, StorageContainer container)
        {
            if (container is HackableLockedCrate && container.OwnerID == 12436)
            {
                SendChatAll(GetLang("ChinookIsDropHackEndLoot", null, player.displayName));
                RemoveMarker();
                timer.Once(30, () => Unsubscribe(nameof(CanBuild)));
                container.OwnerID = 32456345;
                Interface.Oxide.CallHook("LootHack", player);
            }
        }
        void OnEntityDestroy(CH47HelicopterAIController info)
        {
            CH47Helicopter ch47 = info.GetComponent<CH47Helicopter>();
            if (ListHeli.Contains(ch47))
                 ListHeli.Remove(ch47);       
        }
        void Unload()
        {
            Unsubscribe(nameof(CanBuild));
            RemoveMarker();
            for (int i = 0; i < ListHeli.Count; i++)
                ListHeli[i].Kill();
            ListHeli.Clear();
            DestroyAll<ForceCrate>();
            DestroyAll<NPCMonitor>();
        }
        void Init()
        {
            Unsubscribe(nameof(CanBuild));
        }
        void OnServerInitialized()
        {
            _ = this;
            langserver = lang.GetServerLanguage();
            LoadDefaultMessages();
            if(!config.chinook.useCustomPos)
                GenerateSpawnpoints();
            InitStart = (int)CurrentTime();
            timer.Every(config.chinook.TimeChinook, () =>
            {
                if (BasePlayer.activePlayerList.Count >= config.chinook.PlayersMin)
                {
                    ChinookHelicopterCall();
                }
                InitStart = (int)CurrentTime();
            });
            PrintWarning(GetLang("ChinookEventTime", null, FormatTime(TimeSpan.FromSeconds(config.chinook.TimeChinook), language: langserver)));
        }

        #endregion
        
        #region SpawnMetods 
        private HashSet<Vector3> AnomaleisPoint = new HashSet<Vector3>();
        private int spawnCount;
        private object GetSpawnPoint()
        {
            Vector3 targetPos = AnomaleisPoint.ToList().GetRandom();
            if (targetPos == Vector3.zero)
                return null;

            List<BaseEntity> entities = Facepunch.Pool.GetList<BaseEntity>();

            Vis.Entities(targetPos, 50f, entities, LayerMask.GetMask("Construction", "Deployable", "Trigger"));
            int count = entities.Count;

            Facepunch.Pool.FreeList(ref entities);
            if (count > 0)
            {
                AnomaleisPoint.Remove(targetPos);
                --spawnCount;
                if (spawnCount < 10)
                {
                    PrintWarning("All points are completed!\n" +
                        "We start generating new...");
                    AnomaleisPoint.Clear();
                    GenerateSpawnpoints();
                    return null;
                }
                return GetSpawnPoint();
            }
            AnomaleisPoint.Remove(targetPos);
            return targetPos;
        }

        private void GenerateSpawnpoints()
        {
            PrintWarning("Generating points for chinook...");
            for (int i = 0; i < 1500; i++)
            {
                float max = TerrainMeta.Size.x / 2;
                var success = FindNewPosition(new Vector3(0, 0, 0), max);
                if (success is Vector3)
                {
                    Vector3 spawnPoint = (Vector3)success;
                    float height = TerrainMeta.HeightMap.GetHeight(spawnPoint);
                    if (spawnPoint.y >= height && !(spawnPoint.y - height > 1))
                    {
                        AnomaleisPoint.Add(spawnPoint);
                    }
                }
            }
            spawnCount += AnomaleisPoint.Count;

            PrintWarning($"{spawnCount} points generated!");
        }

        private object FindNewPosition(Vector3 position, float max, bool failed = false)
        {
            var targetPos = UnityEngine.Random.insideUnitCircle * max;
            var sourcePos = new Vector3(position.x + targetPos.x, 300, position.z + targetPos.y);
            var hitInfo = RayPosition(sourcePos);
            var success = ProcessRay(hitInfo);
            if (success == null)
            {
                if (failed) return null;
                else return FindNewPosition(position, max, true);
            }
            else if (success is Vector3)
            {
                if (failed) return null;
                else return FindNewPosition(new Vector3(sourcePos.x, ((Vector3)success).y, sourcePos.y), max, true);
            }
            else
            {
                sourcePos.y = Mathf.Max((float)success, TerrainMeta.HeightMap.GetHeight(sourcePos));
                return sourcePos;
            }
        }

        private object ProcessRay(RaycastHit hitInfo)
        {
            if (hitInfo.collider != null)
            {
                if (hitInfo.collider?.gameObject.layer == LayerMask.NameToLayer("Water"))
                    return null;
                if (hitInfo.collider?.gameObject.layer == LayerMask.NameToLayer("Prevent Building"))
                    return null;
                if (hitInfo.GetEntity() != null)
                {
                    return hitInfo.point.y;
                }
                if (hitInfo.collider?.name == "areaTrigger")
                    return null;
                if (hitInfo.collider?.GetComponentInParent<SphereCollider>() || hitInfo.collider?.GetComponentInParent<BoxCollider>())
                {
                    return hitInfo.collider.transform.position + new Vector3(0, -1, 0);
                }
            }
            return hitInfo.point.y;
        }

        private RaycastHit RayPosition(Vector3 sourcePos)
        {
            RaycastHit hitInfo;
            Physics.Raycast(sourcePos, Vector3.down, out hitInfo);
            return hitInfo;
        }

        #endregion

        #region HelpMetods
        private void RemoveMarker()
        {
            RemoveMarkers();
            RustMap?.Call("ApiRemovePointUrl", Name);
        }
        public static StringBuilder sb = new StringBuilder();
        public string GetLang(string LangKey, string userID = null, params object[] args)
        {
            sb.Clear();
            if (args != null)
            {
                sb.AppendFormat(lang.GetMessage(LangKey, this, userID), args);
                return sb.ToString();
            }
            return lang.GetMessage(LangKey, this, userID);
        }
        #region Метка на g карте 
        private void CreateMarker(Vector3 position, float refreshRate, string name, string displayName,
           float radius = 0.65f, string colorOutline = "00FFFFFF")
        {
            var marker = new GameObject().AddComponent<CustomMapMarker>();
            marker.name = name;
            marker.displayName = displayName;
            marker.radius = radius;
            marker.position = position;
            marker.refreshRate = refreshRate;
            ColorUtility.TryParseHtmlString($"{config.mapMarker.colorMarkerG}", out marker.color1);
            ColorUtility.TryParseHtmlString($"{colorOutline}", out marker.color2);
        }

        private void RemoveMarkers()
        {
            foreach (var marker in UnityEngine.Object.FindObjectsOfType<CustomMapMarker>())
            {
                UnityEngine.Object.Destroy(marker);
            }
        }

        private const string genericPrefab = "assets/prefabs/tools/map/genericradiusmarker.prefab";
        private const string vendingPrefab = "assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab";

        #region Scripts

        private class CustomMapMarker : MonoBehaviour
        {
            private VendingMachineMapMarker vending;
            private MapMarkerGenericRadius generic;
            public BaseEntity parent;
            private bool asChild;

            public float radius;
            public Color color1;
            public Color color2;
            public string displayName;
            public float refreshRate;
            public Vector3 position;
            public bool placedByPlayer;

            private void Start()
            {
                transform.position = position;
                asChild = parent != null;
                CreateMarkers();
            }

            private void CreateMarkers()
            {
                vending = GameManager.server.CreateEntity(vendingPrefab, position)
                    .GetComponent<VendingMachineMapMarker>();
                vending.markerShopName = displayName;
                vending.enableSaving = false;
                vending.Spawn();

                generic = GameManager.server.CreateEntity(genericPrefab).GetComponent<MapMarkerGenericRadius>();
                generic.color1 = color1;
                generic.color2 = color2;
                generic.radius = radius;
                generic.alpha = _.config.mapMarker.alfaMarker;
                generic.enableSaving = false;
                generic.SetParent(vending);
                generic.Spawn();

                UpdateMarkers();

                if (refreshRate > 0f)
                {
                    if (asChild)
                    {
                        InvokeRepeating(nameof(UpdatePosition), refreshRate, refreshRate);
                    }
                    else
                    {
                        InvokeRepeating(nameof(UpdateMarkers), refreshRate, refreshRate);
                    }
                }
            }

            private void UpdatePosition()
            {
                if (asChild == true)
                {
                    if (parent.IsValid() == false)
                    {
                        Destroy(this);
                        return;
                    }
                    else
                    {
                        var pos = parent.transform.position;
                        transform.position = pos;
                        vending.transform.position = pos;
                    }
                }

                UpdateMarkers();
            }

            private void UpdateMarkers()
            {
                vending.SendNetworkUpdate();
                generic.SendUpdate();
            }

            private void DestroyMakers()
            {
                if (vending.IsValid())
                {
                    vending.Kill();
                }

                if (generic.IsValid())
                {
                    generic.Kill();
                }


            }

            private void OnDestroy()
            {
                DestroyMakers();
            }
        }

        #endregion

        #endregion

        #region Узнаем квадрат

        string GetGridString(Vector3 pos)
        {
            char[] alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

            pos.z = -pos.z;
            pos += new Vector3(TerrainMeta.Size.x, 0, TerrainMeta.Size.z) * .5f;

            var cubeSize = 146.14f;

            int xCube = (int)(pos.x / cubeSize);
            int zCube = (int)(pos.z / cubeSize);

            int firstLetterIndex = (int)(xCube / alpha.Length) - 1;
            string firstLetter = "";
            if (firstLetterIndex >= 0)
                firstLetter = $"{alpha[firstLetterIndex]}";

            var xStr = $"{firstLetter}{alpha[xCube % 26]}";
            var zStr = $"{zCube}";

            return $"{xStr}{zStr}";
        }

        private string NumberToString(int number)
        {
            bool a = number > 25;
            Char c = (Char)(65 + (a ? number - 26 : number));
            return a ? "A" + c : c.ToString();
        }
        #endregion
        private void DestroyAll<T>()
        {
            UnityEngine.Object.FindObjectsOfType(typeof(T))
                .ToList()
                .ForEach(UnityEngine.Object.Destroy);
        }
        public static string FormatTime(TimeSpan time, int maxSubstr = 5, string language = "ru")
        {
            string result = string.Empty;
            switch (language)
            {
                case "ru":
                    int i = 0;
                    if (time.Days != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Days, "дней", "дня", "день")}";
                        i++;
                    }

                    if (time.Hours != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Hours, "часов", "часа", "час")}";
                        i++;
                    }

                    if (time.Minutes != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Minutes, "минут", "минуты", "минута")}";
                        i++;
                    }

                    if (time.Seconds != 0 && i < maxSubstr)
                    {
                        if (!string.IsNullOrEmpty(result))
                            result += " ";

                        result += $"{Format(time.Seconds, "секунд", "секунды", "секунд")}";
                        i++;
                    }

                    break;
                  default:
                    result = string.Format("{0}{1}{2}{3}",
                    time.Duration().Days > 0 ? $"{time.Days:0} day{(time.Days == 1 ? String.Empty : "s")}, " : string.Empty,
                    time.Duration().Hours > 0 ? $"{time.Hours:0} hour{(time.Hours == 1 ? String.Empty : "s")}, " : string.Empty,
                    time.Duration().Minutes > 0 ? $"{time.Minutes:0} minute{(time.Minutes == 1 ? String.Empty : "s")}, " : string.Empty,
                    time.Duration().Seconds > 0 ? $"{time.Seconds:0} second{(time.Seconds == 1 ? String.Empty : "s")}" : string.Empty);

                    if (result.EndsWith(", ")) result = result.Substring(0, result.Length - 2);

                    if (string.IsNullOrEmpty(result)) result = "0 seconds";
                    break;
            }
            return result;
        }
        int API_GetTimeStartIvent()
        {
            int RealTimeSecond = (int)CurrentTime();
            int LastTime = config.chinook.TimeChinook - (RealTimeSecond - InitStart);
            return LastTime;
        }
        public void SendChatAll(string Message, ConVar.Chat.ChatChannel channel = ConVar.Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT", Message);
            else BasePlayer.activePlayerList.ToList().ForEach(p => p.SendConsoleCommand("chat.add", channel, 0, Message));
        }

        public void SendChatPlayer(string Message, BasePlayer player, ConVar.Chat.ChatChannel channel = ConVar.Chat.ChatChannel.Global)
        {
            if (IQChat)
                IQChat?.Call("API_ALERT_PLAYER", player, Message);
            else player.SendConsoleCommand("chat.add", channel, 0, Message);
        }

        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }
        #endregion   
    }
}
