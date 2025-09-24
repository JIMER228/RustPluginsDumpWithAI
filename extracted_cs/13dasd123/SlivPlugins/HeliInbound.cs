using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace Oxide.Plugins
{
    [Info("HeliInbound", "EcoSmile", "1.1.5")]
    class HeliInbound : RustPlugin
    {
        static HeliInbound ins;

        public PluginConfig config;
        public class PluginConfig
        {
            [JsonProperty("Запретить использовать шашку в Рейд Блоке?")]
            public bool UseRb = true;
            [JsonProperty("Настройки дропа")]
            public Dictionary<string, Options> Settings { get; set; }
            [JsonProperty("ХП основного ротора")]
            public float MainRotorHealth;
            [JsonProperty("ХП вторичного ротора")]
            public float TailRotorHealth;
            [JsonProperty("ХП корпуса вертолета")]
            public float BaseHealth;
            [JsonProperty("Количество ящиков падающих с вертолета")]
            public int MaxLootCrates;
            [JsonProperty("Скорость вертолета")]
            public float HeliSpeed;
            [JsonProperty("Дистанция с которой атакует турель вертолета")]
            public float TurretMaxRange;
            [JsonProperty("Cooldown на вызов вертолета (секунды)")]
            public float HeliCD;
            [JsonProperty("Спавнить ученых после уничтожения вертолета?")]
            public bool SpawnNPC;
            [JsonProperty("Количество ученых")]
            public int NPCCount;
            [JsonProperty("ХП Ученых (до 300)")]
            public int NPCHp;
            [JsonProperty("SkinID Шашки")]
            public ulong SkinID;
            [JsonProperty("Урон от пуль")]
            public float BulletDamage;
            [JsonProperty("Урон от ракет")]
            public float RocketDamage;
            [JsonProperty("Радиус взрыва ракеты")]
            public float RocketExplosionRadius;

            [JsonProperty("Минимум слотов в яшике")]
            public int MinSlots = 4;
            [JsonProperty("Максимум слотов в яшике")]
            public int MaxSlots = 12;
            [JsonProperty("Использовать кастомный лут?")]
            public bool UseCustom = false;
            [JsonProperty("Кастомный лут с вертолета")]
            public List<ItemSetting> LootSettings;
        }

        public class ItemSetting
        {
            [JsonProperty("Шортнейм предмета")]
            public string ShortName;
            [JsonProperty("Максимальное количество предмета")]
            public int MaxAmount;
            [JsonProperty("Минимальное количество предмета")]
            public int MinAmount;
            [JsonProperty("Кастомное название предмета")]
            public string CustomName;
            [JsonProperty("SkinID предмета")]
            public ulong SkinID = 0;
            [JsonProperty("Шанс дропа предмета")]
            public float Chance = 100;
        }

        public class Options
        {
            [JsonProperty("Включить ящик?")]
            public bool Include { get; set; }
            [JsonProperty("Шанс выпадения дропа в %")]
            public float chance { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig()
            {
                UseRb = false,
                MainRotorHealth = 750,
                TailRotorHealth = 350,
                BaseHealth = 5000,
                MaxLootCrates = 3,
                BulletDamage = 10,
                RocketDamage = 30,
                RocketExplosionRadius = 6f,
                HeliSpeed = 25,
                TurretMaxRange = 300,
                HeliCD = 300f,
                SpawnNPC = true,
                NPCCount = 5,
                NPCHp = 300,
                SkinID = 1589652003,
                LootSettings = new List<ItemSetting>()
                {
                        new ItemSetting()
                        {
                            ShortName = "wood",
                            MaxAmount = 25000,
                            MinAmount = 20000,
                            CustomName = "",
                            SkinID = 0
                        },
                        new ItemSetting()
                        {
                            ShortName = "sulfur",
                            MaxAmount = 25000,
                            MinAmount = 20000,
                            CustomName = "",
                            SkinID = 0
                        },
                        new ItemSetting()
                        {
                            ShortName = "gears",

                            MaxAmount = 15,
                            MinAmount = 10,
                            CustomName = "",
                            SkinID = 0
                        },

                },
                Settings = new Dictionary<string, Options>()
                {
                    ["codelockedhackablecrate"] = new Options
                    {
                        Include = false,
                        chance = 50f
                    },
                    ["supply_drop"] = new Options
                    {
                        Include = false,
                        chance = 50f
                    },
                    ["bradley_crate"] = new Options
                    {
                        Include = false,
                        chance = 50f
                    },
                    ["heli_crate"] = new Options
                    {
                        Include = false,
                        chance = 50f
                    },
                    ["crate_elite"] = new Options
                    {
                        Include = false,
                        chance = 50f
                    },
                    ["crate_normal"] = new Options
                    {
                        Include = false,
                        chance = 50f
                    },
                    ["crate_underwater_advanced"] = new Options
                    {
                        Include = false,
                        chance = 50f
                    },
                    ["crate_tools"] = new Options
                    {
                        Include = false,
                        chance = 50f
                    },

                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        void OnServerInitialized()
        {
            ins = this;

            if (config.LootSettings == null)
            {
                config.MinSlots = 4;
                config.MaxSlots = 12;
                config.UseCustom = false;
                config.LootSettings = new List<ItemSetting>()
                {
                        new ItemSetting()
                        {
                            ShortName = "wood",
                            MaxAmount = 25000,
                            MinAmount = 20000,
                            CustomName = "",
                            SkinID = 0
                        },
                        new ItemSetting()
                        {
                            ShortName = "sulfur",
                            MaxAmount = 25000,
                            MinAmount = 20000,
                            CustomName = "",
                            SkinID = 0
                        },
                        new ItemSetting()
                        {
                            ShortName = "gears",

                            MaxAmount = 15,
                            MinAmount = 10,
                            CustomName = "",
                            SkinID = 0
                        },

                };
                SaveConfig();
            }

            LoadMessages();
        }

        List<LootContainer> handledContainers = new List<LootContainer>();
        void OnLootEntity(BasePlayer player, BaseEntity entity, Item item)
        {
            if (!(entity is LootContainer)) return;
            var container = (LootContainer)entity;
            if (handledContainers.Contains(container) || container.ShortPrefabName == "stocking_large_deployed" || container.ShortPrefabName == "stocking_small_deployed") return;
            handledContainers.Add(container);
            List<int> ItemsList = new List<int>();
            if (config.Settings.ContainsKey(entity.ShortPrefabName) && config.Settings[entity.ShortPrefabName].Include)
            {
                var Chance = UnityEngine.Random.Range(0f, 100f);
                if (Chance < config.Settings[entity.ShortPrefabName].chance)
                {
                    var itemContainer = container.inventory;
                    foreach (var i1 in itemContainer.itemList)
                    {
                        ItemsList.Add(i1.info.itemid);
                    }
                    if (!ItemsList.Contains(1397052267))
                    {
                        if (container.inventory.itemList.Count == container.inventory.capacity)
                            container.inventory.capacity++;
                        item = ItemManager.CreateByName("supply.signal", 1, config.SkinID);
                        item.name = "Сигнальная шашка для вызова вертолета.";
                        item.MoveToContainer(itemContainer);
                    }
                }
            }
        }
        
        public class Holder
        {
            public DateTime date;
            public bool IsCustomSupply;
        }

        Dictionary<BasePlayer, Holder> holder = new Dictionary<BasePlayer, Holder>();

        void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (newItem != null && newItem.skin == config.SkinID)
            {
                if (!holder.ContainsKey(player))
                    holder[player] = new Holder { date = DateTime.Now, IsCustomSupply = true };
                else
                    holder[player].IsCustomSupply = true;
            }
            else if (holder.ContainsKey(player))
                holder[player].IsCustomSupply = false;
        }

        private void OnExplosiveDropped(BasePlayer player, BaseEntity entity) => OnExplosiveThrown(player, entity);

        [PluginReference]
        Plugin NoEscape;

        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if (holder.ContainsKey(player) && holder[player].IsCustomSupply)
            {
                holder[player].IsCustomSupply = false;
                if (entity == null)
                    return;
                if(config.UseRb && NoEscape != null && NoEscape.Call<bool>("IsRaidBlocked", player) == true)
                {
                    SendReply(player, GetMsg("rb", player.userID));
                    return;
                }
                if (holder[player].date > DateTime.Now)
                {
                    SendReply(player, GetMsg("cd", player.userID).Replace("{0}", Math.Ceiling((holder[player].date - DateTime.Now).TotalSeconds).ToString()));
                    entity.Kill();

                    Item item = ItemManager.CreateByName("supply.signal", 1, config.SkinID);
                    item.name = "Сигнальная шашка для вызова вертолета.";
                    player.GiveItem(item);

                    return;
                }
                entity.CancelInvoke((entity as SupplySignal).Explode);
                entity.Invoke(entity.KillMessage, 30f);

                holder[player].date = DateTime.Now.AddSeconds(ins.config.HeliCD);

                timer.Once(3, () =>
                {
                    Effect.server.Run("assets/bundled/prefabs/fx/smoke_signal_full.prefab", entity, 0, new Vector3(), new Vector3());
                    HeliCalling(player);
                    foreach (var pl in BasePlayer.activePlayerList)
                        SendReply(pl, GetMsg("incoming", pl.userID).Replace("{0}", player.displayName).Replace("{1}", $"{(int)entity.transform.position.x}").Replace("{2}", $"{(int)entity.transform.position.y}").Replace("{3}", $"{(int)entity.transform.position.z}"));
                });
            }
        }

        object CanStackItem(Item item, Item targetItem)
        {
            if (item.info.shortname == "supply.signal" && targetItem.info.shortname == "supply.signal")
            {
                if (item.skin == config.SkinID || targetItem.skin == config.SkinID)
                {
                    if (targetItem.skin != item.skin)
                    {
                        return false;
                    }
                }
            }
            return null;
        }
        [PluginReference]
        Plugin StacksExtended;

        object OnItemSplit(Item thisI, int split_Amount)
        {
            if (StacksExtended) return null;
            if (thisI.skin == 0uL) return null;
            if (thisI.skin == config.SkinID)
            {
                thisI.amount -= split_Amount;
                Item item = ItemManager.CreateByItemID(thisI.info.itemid, split_Amount, thisI.skin);
                if (item != null)
                {
                    item.amount = split_Amount;
                    item.name = thisI.name;
                    item.OnVirginSpawn();
                    if (thisI.IsBlueprint()) item.blueprintTarget = thisI.blueprintTarget;
                    if (thisI.hasCondition) item.condition = thisI.condition;
                    item.MarkDirty();
                    return item;
                }
            }
            return null;
        }

        void Unload()
        {

        }

        public class NPCComponent : FacepunchBehaviour
        {
            Dictionary<HumanNPC, Vector3> npclist;

            public void SpawnNPC(Vector3 position)
            {
                npclist = new Dictionary<HumanNPC, Vector3>();

                for (int i = 0; i < ins.config.NPCCount; i++)
                {
                    var pos = ins.RandomCircle(position, 15);
                    HumanNPC scientist = GameManager.server.CreateEntity("assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_junkpile_pistol.prefab", pos) as HumanNPC;
                    scientist.GetComponent<NavMeshAgent>().Warp(pos);
                    scientist.Spawn();
                    scientist.InitializeHealth(ins.config.NPCHp, ins.config.NPCHp);
                    npclist.Add(scientist, pos);
                }
                InvokeRepeating(MoveToHome, 30, 30);
            }

            void MoveToHome()
            {
                foreach (var npc in npclist)
                {
                    if (npc.Key == null || npc.Key.IsDead()) continue;

                    npc.Key.SetDestination(npc.Value);
                }
            }

            void OnDestroy()
            {
                CancelInvoke(MoveToHome);
            }
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity == null) return;
            if (entity.ShortPrefabName.Contains("rocket_heli"))
            {
                var resources = new List<PatrolHelicopter>();
                Vis.Entities(entity.transform.position, 10f, resources);
                if (resources.Count(x => x.GetComponent<HeliController>() != null) > 0)
                    return;

                var explosion = entity?.GetComponent<TimedExplosive>() ?? null;
                if (explosion == null) return;

                var dmgTypes = explosion?.damageTypes ?? null;
                explosion.explosionRadius = config.RocketExplosionRadius;
                if (dmgTypes != null && dmgTypes.Count > 0)
                {
                    for (int i = 0; i < dmgTypes.Count; i++)
                    {
                        var dmg = dmgTypes[i];
                        if (dmg.type == Rust.DamageType.Explosion) dmg.amount = config.RocketDamage;
                    }
                }

            }
        }

        public class HeliController : FacepunchBehaviour
        {
            MapMarkerGenericRadius mapMarker;
            PatrolHelicopter helicopter;
            //BasePlayer target;
            Vector3 callPosition;
            PatrolHelicopterAI heliAI;

            void Awake()
            {
                helicopter = GetComponent<PatrolHelicopter>();
                heliAI = GetComponent<PatrolHelicopterAI>();
            }

            public void SetInbouter(BasePlayer player)
            {
                heliAI.maxSpeed = ins.config.HeliSpeed;
                helicopter.InitializeHealth(ins.config.BaseHealth, ins.config.BaseHealth);
                var weakspots = helicopter.weakspots;
                weakspots[0].health = ins.config.MainRotorHealth;
                weakspots[1].health = ins.config.TailRotorHealth;
                helicopter.bulletDamage = ins.config.BulletDamage;
                helicopter.maxCratesToSpawn = ins.config.MaxLootCrates;
                heliAI.leftGun.maxTargetRange = heliAI.rightGun.maxTargetRange = ins.config.TurretMaxRange;
                SetTarget(player);
                CreatePrivateMap();

            }

            public void SetTarget(BasePlayer player)
            {
                //this.target = player;
                callPosition = player.transform.position;
                heliAI._targetList.Add(new global::PatrolHelicopterAI.targetinfo(player, player));
                heliAI.ValidStrafeTarget(player);
                heliAI.SetTargetDestination(player.transform.position, 10);
                heliAI.MoveToDestination();
                InvokeRepeating(GoToPatrol, 0, 30);
                InvokeRepeating(RandomMove, 10, 10);
            }

            void GoToPatrol()
            {
                if (helicopter == null || helicopter.IsDestroyed || helicopter.IsDead()) return;
                //if (target == null || target.IsDestroyed || target.IsDead()) return;

                heliAI.SetTargetDestination(callPosition, 10);
                heliAI.MoveToDestination();
            }

            void RandomMove()
            {
                heliAI.SetTargetDestination(ins.RandomCircle(callPosition, 40), 15);
                heliAI.MoveToDestination();
            }
            void CreatePrivateMap()
            {
                mapMarker = (MapMarkerGenericRadius)GameManager.server.CreateEntity("assets/prefabs/tools/map/genericradiusmarker.prefab", callPosition, new Quaternion());
                mapMarker.enableSaving = false;
                mapMarker.Spawn();
                mapMarker.radius = 0.2f;
                mapMarker.alpha = 1f;
                UnityEngine.Color color = ConvertToColor("#932e1d");
                UnityEngine.Color color2 = new UnityEngine.Color(0, 0, 0, 0);
                mapMarker.color1 = color;
                mapMarker.color2 = color2;
                mapMarker.SendUpdate();
            }

            private UnityEngine.Color ConvertToColor(string color)
            {
                if (color.StartsWith("#")) color = color.Substring(1);
                int red = int.Parse(color.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(color.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(color.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return new UnityEngine.Color((float)red / 255, (float)green / 255, (float)blue / 255);
            }

            void OnDestroy()
            {
                if (ins.config.SpawnNPC)
                {
                    var npcGO = new GameObject();
                    npcGO.AddComponent<NPCComponent>();
                    npcGO.GetComponent<NPCComponent>().SpawnNPC(helicopter.transform.position);
                }
                if (ins.config.UseCustom)
                {
                    var lootTable = ins.config.LootSettings;
                    var crateList = new List<LootContainer>();
                    Vis.Entities(heliAI.transform.position, 10, crateList);
                    foreach (var ent in crateList)
                        ent.OwnerID = 44312;
                    foreach (var ent in crateList)
                    {
                        ins.FillContaner(ent, lootTable);
                    } 

                }
                mapMarker?.Kill();
                Destroy(this);
            }
        }

        object CanUILootSpawn(LootContainer container)
        {
            if (container.OwnerID == 44312) return false;

            return null;
        }

        void HeliCalling(BasePlayer player)
        {
            var entity = GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", default(Vector3), default(Quaternion), true);
            if (!(bool)entity)
                return;
            entity.Spawn();
            entity.transform.position = RandomCircle(player.transform.position, 100);
            entity.gameObject.AddComponent<HeliController>();
            entity.GetComponent<HeliController>().SetInbouter(player);
        }

        Vector3 RandomCircle(Vector3 center, float radius = 2)
        {
            float ang = UnityEngine.Random.value * 360;
            Vector3 pos = new Vector3();
            pos.x = center.x + radius * Mathf.Sin(ang * Mathf.Deg2Rad);
            pos.z = center.z + radius * Mathf.Cos(ang * Mathf.Deg2Rad);
            pos.y = GetGroundPosition(pos) + 50;
            return pos;
        }
        static float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos);
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out hit, Mathf.Infinity,
                LayerMask.GetMask(new[] { "Terrain", "World", "Default", "Construction", "Deployed" })) && !hit.collider.name.Contains("rock_cliff")) return Mathf.Max(hit.point.y, y);
            return y;
        }  
          
        [ConsoleCommand("getsupply")]
        void cmdGetSupply(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2)
                return;
            if (arg.Args == null || arg.Args.Length != 2) 
            {
                Puts("Use: getsupply STEAMID count");
                return;
            } 
            ulong uid = arg.GetUInt64(0);
            int count = arg.GetInt(1);
            BasePlayer player = BasePlayer.FindByID(uid);
            if (arg.Args.Length == 2)
            {
                Item item = ItemManager.CreateByName("supply.signal", count, config.SkinID);
                item.name = "Сигнальная шашка для вызова вертолета.";
                player.GiveItem(item);
            }
        }

        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID?.ToString());

        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["incoming"] = "Player {0} called the helicopter with a modified grenade.\nThe location is marked on the map",
                ["cd"] = "Plase wate {0} sec to next heli calling",
                ["rb"] = "Can not use in the RaidBlock"
            }, this);

            lang.RegisterMessages(new Dictionary<string, string> 
            {
                ["incoming"] = "Игрок {0} вызвал модифицированной сигнальной гранатой на дуэль патрульный вертолет.\nМестоположение отмечено на карте",
                ["cd"] = "Пожалуйста подождите {0} сек до вызова следующего вертолета",
                ["rb"] = "Нельзя использовать шашку во время рейда"
            }, this, "ru");
        }

        void FillContaner(LootContainer container, List<ItemSetting> itemlist)
        {
            if (container == null)
            {
                return;
            }

            ItemManager.DoRemoves();
            List<Item> countItem = new List<Item>();
            container.inventory.itemList.Clear();
            int slots = UnityEngine.Random.Range(config.MinSlots, config.MaxSlots + 1);

            container.inventory.capacity = slots;
            container.inventorySlots = slots;

            int maxTry = 100;
            int itemCount = 0;
            while (itemCount < slots && maxTry > 0)
            {
                maxTry--;

                var rndItem = itemlist.GetRandom();
                var chance = UnityEngine.Random.Range(0, 100);
                if (chance <= rndItem.Chance)
                {
                    if (countItem.Any(x => x.info.shortname == rndItem.ShortName && x.skin == rndItem.SkinID))
                    {
                        continue;
                    }

                    var amount = UnityEngine.Random.Range(rndItem.MinAmount, rndItem.MaxAmount);
                    Item newitem = ItemManager.CreateByName(rndItem.ShortName, amount, rndItem.SkinID);
                    if (newitem == null) continue;
                    if (!string.IsNullOrEmpty(rndItem.CustomName))
                        newitem.name = rndItem.CustomName;


                    itemCount++;
                    countItem.Add(newitem);
                }
            }

            if (container.inventory == null)
            {
                return;
            }

            foreach (var item in countItem)
            {
                item.MoveToContainer(container.inventory);
            }

        }
    }
}