using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ModifiedClothing", "Kira", "1.1.3")]
    [Description("Modify clothing for Rust")]
    public class ModifiedClothing : RustPlugin
    {
        #region [Vars] / [Переменные]

        private static ModifiedClothing _;
#pragma warning disable CS0649
        [PluginReference] private Plugin ImageLibrary, BrightNight;
#pragma warning restore CS0649
        private bool ClearNight;

        #endregion

        #region [Configuraton] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "ModifiedClothing - Config")]
            public ModifyCFG ModifyCloth = new ModifyCFG();

            public class ModifyCFG
            {
                [JsonProperty(PropertyName = "[Метеоритная пыль] Количество пыли с одного камня")]
                public int MeteorRate = 10;

                [JsonProperty(PropertyName = "[Щдем] Регенерация здоровья (в секунду)")]
                public float HelmetRegen = 1f;

                [JsonProperty(PropertyName = "[Броня] Увеличение здоровья (0.0-1.0)")]
                public float ArmorMaxHealth = 0.3f;

                [JsonProperty(PropertyName = "[Щит] Количество здоровья")]
                public float Shield = 50f;

                [JsonProperty(PropertyName = "[Щит] Количество точек здоровья (Здоровье делённое на 10)")]
                public int ShieldPoint = 5;

                [JsonProperty(PropertyName = "[Щит] Регенерегация  здоровья (в секунду)")]
                public float ShieldRegen = 1f;

                [JsonProperty(PropertyName = "[Худи] Время нокнутого состояния (в секундах)")]
                public int WoundTime = 5;

                [JsonProperty(PropertyName = "[Штаны] Увеличение рейтов")]
                public float PantsRate = 1f;

                [JsonProperty(PropertyName = "[Перчатки] Увеличение рейтов на метеоритную пыль")]
                public int GlovesRate = 2;

                [JsonProperty(PropertyName = "[Сет] Уменьшение общего урона, при собранном сете (0.0-1.0)")]
                public float DamageScale = 0.7f;

                [JsonProperty(PropertyName = "[Коптер] Кд на спавн коптера (в секундах)")]
                public int CopterCD = 60;
            }
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                ModifyCloth = new ConfigData.ModifyCFG
                {
                    HelmetRegen = 1,
                    ArmorMaxHealth = 0.3f,
                    Shield = 50f,
                    WoundTime = 5,
                    DamageScale = 0.7f,
                    CopterCD = 60,
                    ShieldPoint = 5,
                    ShieldRegen = 1
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region [Dictionary] / [Словари]

        private Dictionary<ulong, ClothingCore> ClothingCores = new Dictionary<ulong, ClothingCore>();

        #endregion

        #region [MonoBehaviours]

        public class ClothingCore : MonoBehaviour
        {
            private static BasePlayer player;
            public ulong OwnerID;

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public float Shield;
            public Dictionary<string, MonoBehaviour> Clothing;
            public BaseEntity Copter;
            public Vector3 copterpos;
            public int CopterCooldown = _._config.ModifyCloth.CopterCD;
            public int TimerCooldown;
            public bool CopterSpawned;
            public bool CopterGiving;

            #region [Initialization]

            private void Awake()
            {
                player = GetComponent<BasePlayer>();
                OwnerID = player.userID;
                name = OwnerID.ToString();
                Shield = _._config.ModifyCloth.Shield;
                Clothing = new Dictionary<string, MonoBehaviour>();
                InitializeList();
                player.inventory.containerWear.SetLocked(false);
            }

            #endregion

            public void Timer()
            {
                if (TimerCooldown <= 0) CancelInvoke(nameof(Timer));
                TimerCooldown--;
            }

            #region [Attire]

            public class Helmet : MonoBehaviour
            {
                public BasePlayer player;
                public ClothingCore cl;
                public PlayerMetabolism metabolism;

                private void Start()
                {
                    player = GetComponent<BasePlayer>();
                    metabolism = player.metabolism;
                    cl = player.gameObject.GetComponent<ClothingCore>();
                    InvokeRepeating(nameof(Heeling), 1f, 1f);
                    UpdateOxygen();
                    cl.Clothing.Add("metal.facemask", this);
                }

                public void SetMetabolism()
                {
                    metabolism.calories.value = 500;
                    metabolism.hydration.value = 250;
                }

                public void UpdateOxygen()
                {
                    metabolism.oxygen.min = 4;
                    metabolism.wetness.max = 0;
                }

                public void Heeling()
                {
                    SetMetabolism();
                    player.Heal(_._config.ModifyCloth.HelmetRegen);
                }

                private void OnDestroy()
                {
                    metabolism.oxygen.min = 0;
                    metabolism.calories.min = 0;
                    metabolism.hydration.min = 0;
                    metabolism.wetness.max = 100;
                    cl.RemoveAttire("metal.facemask");
                }
            }

            public class Armor : MonoBehaviour
            {
                public BasePlayer player;
                public ClothingCore cl;
                public float MaxHealth;
                public float Health = _._config.ModifyCloth.ArmorMaxHealth;

                private void Awake()
                {
                    player = GetComponent<BasePlayer>();
                    cl = player.gameObject.GetComponent<ClothingCore>();
                    player.modifiers.RemoveAll();
                    MaxHealth += Health;
                    UpdateMaxHealth();
                    cl.Clothing.Add("metal.plate.torso", this);
                }

                public void UpdateMaxHealth()
                {
                    if (MaxHealth <= 0)
                    {
                        player.modifiers.RemoveAll();
                        return;
                    }

                    var mod = new List<ModifierDefintion>
                    {
                        new ModifierDefintion
                        {
                            source = Modifier.ModifierSource.Tea,
                            duration = 3600,
                            type = Modifier.ModifierType.Max_Health,
                            value = MaxHealth
                        }
                    };
                    player.modifiers.Add(mod);
                }

                private void OnDestroy()
                {
                    player.modifiers.RemoveAll();
                    cl.RemoveAttire("metal.plate.torso");
                }
            }

            public class Boots : MonoBehaviour
            {
                public BasePlayer player;
                public ClothingCore cl;

                // ReSharper disable once Unity.RedundantEventFunction
                private void Awake()
                {
                    player = GetComponent<BasePlayer>();
                    cl = player.gameObject.GetComponent<ClothingCore>();
                    cl.Clothing.Add("shoes.boots", this);
                }

                // ReSharper disable once Unity.RedundantEventFunction
                private void OnDestroy()
                {
                    cl.RemoveAttire("shoes.boots");
                }
            }

            public class Hoodie : MonoBehaviour
            {
                public BasePlayer player;
                public PlayerMetabolism metabolism;
                public ClothingCore cl;

                private void Awake()
                {
                    player = GetComponent<BasePlayer>();
                    cl = player.gameObject.GetComponent<ClothingCore>();
                    cl.Clothing.Add("hoodie", this);
                    metabolism = player.metabolism;
                    OffCold();
                }

                public void StartWake()
                {
                    Invoke(nameof(WakeUp), _._config.ModifyCloth.WoundTime);
                }

                public void OffCold()
                {
                    metabolism.temperature.min = 20;
                    metabolism.temperature.max = 30;
                }

                public void WakeUp()
                {
                    player.StopWounded();
                }

                private void OnDestroy()
                {
                    metabolism.temperature.min = -40;
                    metabolism.temperature.max = 40;
                    cl.RemoveAttire("hoodie");
                }
            }

            public class Pants : MonoBehaviour
            {
                public BasePlayer player;
                public PlayerMetabolism metabolism;
                public ClothingCore cl;

                private void Awake()
                {
                    player = GetComponent<BasePlayer>();
                    cl = player.gameObject.GetComponent<ClothingCore>();
                    metabolism = player.metabolism;
                    RadiationResist();
                    cl.Clothing.Add("pants", this);
                }

                public void RadiationResist()
                {
                    metabolism.radiation_level.max = 0;
                    metabolism.radiation_poison.max = 0;
                }

                private void OnDestroy()
                {
                    metabolism.radiation_level.max = 1000;
                    metabolism.radiation_poison.max = 1000;
                    cl.RemoveAttire("pants");
                }
            }

            public class Kilt : MonoBehaviour
            {
                public BasePlayer player;
                private ClothingCore cl;

                private void Awake()
                {
                    player = GetComponent<BasePlayer>();
                    cl = player.gameObject.GetComponent<ClothingCore>();
                    cl.Clothing.Add("roadsign.kilt", this);
                    DrawUI_Shield();
                    InvokeRepeating(nameof(ShieldRegeneration), 1f, 1f);
                }

                public void ShieldRegeneration()
                {
                    if (cl.Shield >= _._config.ModifyCloth.Shield) return;
                    cl.Shield += _._config.ModifyCloth.ShieldRegen;
                    DrawUI_Shield();
                }

                public void DrawUI_Shield()
                {
                    var ui = new CuiElementContainer
                    {
                        new CuiElement
                        {
                            Name = "UI.Shield",
                            Parent = "Hud",
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = (string) _.ImageLibrary.Call("GetImage", "UI.Shield.BG")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "1 1",
                                    AnchorMax = "1 1",
                                    OffsetMin = "-200 -30",
                                    OffsetMax = "0 0"
                                }
                            }
                        }
                    };

                    var count = cl.Shield / _._config.ModifyCloth.ShieldPoint;
                    for (var i = 0; i <= count; i++)
                    {
                        ui.Add(new CuiLabel
                        {
                            Text =
                            {
                                Text = "<color=#4550BD>•</color>",
                                Align = TextAnchor.MiddleCenter,
                                FontSize = 25,
                                FadeIn = 0.1f * i
                            },
                            RectTransform =
                            {
                                AnchorMin = $"{1 - (0.15 * i)} 0",
                                AnchorMax = "1 0.95"
                            }
                        }, "UI.Shield");
                    }

                    CuiHelper.DestroyUi(player, "UI.Shield");
                    CuiHelper.AddUi(player, ui);
                }

                private void OnDestroy()
                {
                    CuiHelper.DestroyUi(player, "UI.Shield");
                    cl.RemoveAttire("roadsign.kilt");
                    CancelInvoke(nameof(ShieldRegeneration));
                }
            }

            public class Gloves : MonoBehaviour
            {
                public BasePlayer player;
                private ClothingCore cl;

                // ReSharper disable once Unity.RedundantEventFunction
                private void Awake()
                {
                    player = GetComponent<BasePlayer>();
                    cl = player.gameObject.GetComponent<ClothingCore>();
                    cl.Clothing.Add("tactical.gloves", this);
                }

                private void OnDestroy()
                {
                    cl.RemoveAttire("tactical.gloves");
                }
            }

            #endregion

            #region [Helpers]

            public void CopterRespawn()
            {
                if (TimerCooldown > 0)
                {
                    var convert = TimeSpan.FromSeconds(160);
                    player.ChatMessage($"Copter in colldown {convert.Minutes} m {convert.Seconds} s");
                    return;
                }

                if (Copter != null) Copter.Kill();
                Copter = GameManager.server.CreateEntity("assets/content/vehicles/minicopter/minicopter.entity.prefab");
                Copter.transform.position = new Vector3(copterpos.x, copterpos.y + 1, copterpos.z);
                Copter.OwnerID = player.userID;
                Copter.skinID = player.userID;
                Copter.Spawn();
                Copter.SendNetworkUpdate();
                CopterSpawned = true;
                player.inventory.containerWear.SetLocked(true);
            }

            public void CopterDestroy()
            {
                if (Copter == null) return;
                copterpos = Copter.transform.position;
                Copter.Kill();
                player.inventory.containerWear.SetLocked(false);
                TimerCooldown = CopterCooldown;
                CopterSpawned = false;
                CopterGiving = false;
                InvokeRepeating(nameof(Timer), 1f, 1f);
            }

            public void InitializeList()
            {
                _.ClothingCores.Add(player.userID, this);
            }

            public void RemoveAttire(string remattire)
            {
                if (!Clothing.ContainsKey(remattire)) return;
                Destroy(Clothing[remattire]);
                Clothing.Remove(remattire);
                if (Clothing.Count < 7 & Copter != null) CopterDestroy();
            }

            public void DestroyClothers()
            {
                foreach (var obj in Clothing) Destroy(obj.Value);
            }

            #endregion

            #region [Hooks]

            private void OnDestroy()
            {
                player.modifiers.RemoveAll();
                CopterDestroy();
                player.inventory.containerWear.SetLocked(false);
                DestroyClothers();
            }

            #endregion
        }

        #endregion

        #region [Hooks] / [Хуки]

        private List<string> BlockItems = new List<string>
        {
            "40mm_grenade_he",
            "grenade.f1.deployed",
            "explosive.satchel.deployed",
            "grenade.beancan.deployed"
        };

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (!ClothingCores.ContainsKey(player.userID)) return;
            var component = ClothingCores[player.userID];
            component.DestroyClothers();
            component.CopterDestroy();
            UnityEngine.Object.Destroy(component);
        }

        // ReSharper disable once UnusedMember.Local
        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (info == null || entity == null) return null;
            if (info.damageTypes.Has(DamageType.Decay)) return null;
            if (entity.ToPlayer() == null) return null;
            if (!(entity is BasePlayer)) return null;
            var player = entity.ToPlayer();
            if (!ClothingCores.ContainsKey(player.userID)) return null;
            var component = ClothingCores[player.userID];
            if (component.Clothing.Count == 7) info.damageTypes.ScaleAll(_._config.ModifyCloth.DamageScale);
            switch (info.damageTypes.GetMajorityDamageType())
            {
                case DamageType.Heat:
                    if (component.Clothing.ContainsKey("shoes.boots")) return false;
                    break;
                case DamageType.Bullet:
                    Shield(info, component);
                    break;
                case DamageType.Bite:
                case DamageType.Slash:
                case DamageType.Blunt:
                case DamageType.Stab:

                    var obj = info.WeaponPrefab;
                    if (obj != null)
                        if (BlockItems.Contains(obj.ShortPrefabName))
                        {
                            Shield(info, component);
                            return null;
                        }

                    if (component.Clothing.ContainsKey("pants")) return false;
                    break;
                case DamageType.Fall:
                    if (component.Clothing.ContainsKey("shoes.boots")) return false;
                    break;
                case DamageType.Explosion:
                    Shield(info, component);
                    break;
            }

            return null;
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local 
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (!(entity is MiniCopter)) return;
            if (entity.skinID == 0) return;
            var player = BasePlayer.FindByID(entity.gameObject.GetComponent<BaseEntity>().OwnerID);
            var component = player.gameObject.GetComponent<ClothingCore>();
            component.DestroyClothers();
            component.CopterDestroy();
        }

        // ReSharper disable once UnusedMember.Local
        private object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (dispenser == null || entity == null) return null;
            if (!(entity is BasePlayer)) return null;
            var player = entity.ToPlayer();
            if (!ClothingCores.ContainsKey(player.userID)) return null;
            var component = ClothingCores[player.userID];
            if (component.Clothing.ContainsKey("pants"))
                item.amount = Convert.ToInt32(item.amount * _config.ModifyCloth.PantsRate);
            if (!component.Clothing.ContainsKey("tactical.gloves")) return null;
            var cookable = item.info.GetComponent<ItemModCookable>();
            if (cookable == null)
                return null;

            var cookedItem = ItemManager.Create(cookable.becomeOnCooked, item.amount);
            player.GiveItem(cookedItem, BaseEntity.GiveItemReason.ResourceHarvested);

            return false;
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            if (dispenser == null || player == null) return null;
            CheckCore(player);

            #region [Meteor]

            if (ClearNight)
            { 
                CheckCore(player);
                if (item.info.shortname == "sulfur.ore" || item.info.shortname == "metal.ore")
                {
                    var count = _config.ModifyCloth.MeteorRate;
                    if (ClothingCores[player.userID].Clothing.ContainsKey("tactical.gloves"))
                        count *= _config.ModifyCloth.GlovesRate;
                    var meteor = ItemManager.CreateByName("ducttape", count, 2814895972);
                    meteor.name = "Метеоритная пыль";
                    player.GiveItem(meteor);
                }
            }

            #endregion

            #region [Smelt]

            if (ClothingCores[player.userID].Clothing.ContainsKey("tactical.gloves"))
            {
                CheckCore(player);
                var cookable = item.info.GetComponent<ItemModCookable>();
                if (cookable == null)
                    return null;

                var cookedItem = ItemManager.Create(cookable.becomeOnCooked, item.amount);
                player.GiveItem(cookedItem, BaseEntity.GiveItemReason.ResourceHarvested);
                NextTick(item.DoRemove);
            }

            return true;

            #endregion
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private object OnItemCraft(ItemCraftTask task, BasePlayer player, Item item)
        {
            if (!ClothingCores.ContainsKey(player.userID)) return null;
            var component = ClothingCores[player.userID];
            if (player.userID != component.OwnerID) return null;
            if (component.Clothing.ContainsKey("pants")) task.endTime = 1f;
            return null;
        }

        // ReSharper disable once UnusedMember.Local
        private object OnPlayerAddModifiers(BasePlayer player, Item item, ItemModConsumable consumable)
        {
            if (player == null || item == null || consumable == null) return null;
            if (!ClothingCores.ContainsKey(player.userID)) return null;
            var component = ClothingCores[player.userID];
            if (!component.Clothing.ContainsKey("metal.plate.torso")) return null;
            if (player.userID != component.OwnerID) return null;
            switch (item.info.shortname)
            {
                case "maxhealthtea":
                case "maxhealthtea.advanced":
                case "maxhealthtea.pure":
                    return false;
                default:
                    return true;
            }
        }

        // ReSharper disable once UnusedMember.Local
        private object CanMountEntity(BasePlayer player, BaseMountable entity)
        {
            if (player == null || entity == null) return null;
            if (entity.OwnerID == 0) return null;
            if (entity.mountPose != PlayerModel.MountPoses.SitMinicopter_Pilot) return null;
            var owner = entity.GetComponentInParent<MiniCopter>().OwnerID;
            if (owner != player.userID) return false;
            return null;
        }

        // ReSharper disable once UnusedMember.Local 
        private object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player == null || info == null) return null;
            if (!ClothingCores.ContainsKey(player.userID)) return null;
            var obj = ClothingCores[player.userID];
            if (player.userID != obj.OwnerID) return null;
            switch (info.damageTypes.GetMajorityDamageType())
            {
                case DamageType.Explosion:
                    info.damageTypes.ScaleAll(0.8f);
                    break;
            }

            if (obj.Clothing.Count == 7)
            {
                switch (info.damageTypes.GetMajorityDamageType())
                {
                    case DamageType.Generic:
                    case DamageType.Hunger:
                    case DamageType.Thirst:
                    case DamageType.Cold:
                    case DamageType.Drowned:
                    case DamageType.Heat:
                    case DamageType.Bleeding:
                    case DamageType.Poison:
                    case DamageType.Slash:
                    case DamageType.Blunt:
                    case DamageType.Fall:
                    case DamageType.Radiation:
                    case DamageType.Bite:
                    case DamageType.Stab:
                    case DamageType.Explosion:
                    case DamageType.RadiationExposure:
                    case DamageType.ColdExposure:
                    case DamageType.Decay:
                    case DamageType.ElectricShock:
                    case DamageType.Arrow:
                    case DamageType.AntiVehicle:
                    case DamageType.Collision:
                    case DamageType.Fun_Water:
                    case DamageType.LAST:
                        obj.Shield = 0;
                        return false;
                }
            }

            obj.DestroyClothers();
            obj.CopterDestroy();
            UnityEngine.Object.Destroy(obj);
            return null;
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private object OnPlayerWound(BasePlayer player, HitInfo info)
        {
            if (!ClothingCores.ContainsKey(player.userID)) return null;
            var component = ClothingCores[player.userID];
            if (player.userID != component.OwnerID) return null;
            if (component.Clothing.ContainsKey("hoodie")) component.GetComponent<ClothingCore.Hoodie>().StartWake();
            return null;
        }

        // ReSharper disable once UnusedMember.Local
        private bool CanDropActiveItem(BasePlayer player)
        {
            if (!ClothingCores.ContainsKey(player.userID)) return true;
            var component = ClothingCores[player.userID];
            if (player.userID != component.OwnerID) return true;
            return !component.Clothing.ContainsKey("metal.plate.torso");
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local 
        private void OnItemDropped(Item item, BaseEntity entity)
        {
            if (item == null) return;
            if (item.GetOwnerPlayer() == null) return;
            var player = item.GetOwnerPlayer();
            if (!ClothingCores.ContainsKey(player.userID)) return;
            if (item.GetRootContainer().capacity != 7) return;
            var component = ClothingCores[player.userID];
            switch (item.skin)
            {
                case 2786314369:
                    component.RemoveAttire(item.info.shortname);
                    break;
                case 2786313151:
                    component.RemoveAttire(item.info.shortname);
                    break;
                case 1581890527:
                    component.RemoveAttire(item.info.shortname);
                    break;
                case 1581896222:
                    component.RemoveAttire(item.info.shortname);
                    break;
                case 2514895377:
                    component.RemoveAttire(item.info.shortname);
                    break;
                case 1088899968:
                    component.RemoveAttire(item.info.shortname);
                    break;
                case 7:
                    component.RemoveAttire(item.info.shortname);
                    break;
            }
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private static object Shield(HitInfo info, ClothingCore component)
        {
            if (!component.Clothing.ContainsKey("roadsign.kilt")) return null;
            if (component.Shield <= 0) return null;
            var damage = component.Shield - info.damageTypes.Total();
            if (damage < 0) damage = -5;
            info.damageTypes.Clear();
            component.Shield = damage;
            if (info.HitEntity != null)
            {
                Effect.server.Run("assets/bundled/prefabs/fx/decals/bullet/glass/decal_bullet_glass2.prefab",
                    info.HitEntity.transform.position);
            }
            return null;
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private object CanWearItem(PlayerInventory inventory, Item item, int targetSlot)
        {
            if (inventory == null || item == null) return null;
            if (inventory._baseEntity == null) return null;
            var player = item.GetOwnerPlayer();
            switch (item.skin)
            {
                case 2786314369:
                    CheckCore(player);
                    if (player.inventory.containerWear.FindItemsByItemName("metal.facemask") == null)
                    {
                        if (player.gameObject.GetComponent<ClothingCore.Helmet>() == null)
                            player.gameObject.AddComponent<ClothingCore.Helmet>();
                        else return false;
                    }
                    else return false;

                    break;
                case 2786313151:
                    CheckCore(player);
                    if (player.inventory.containerWear.FindItemsByItemName("metal.plate.torso") == null)
                    {
                        if (player.gameObject.GetComponent<ClothingCore.Armor>() == null)
                            player.gameObject.AddComponent<ClothingCore.Armor>();
                        else return false;
                    }
                    else return false;

                    break;
                case 1581890527:
                    CheckCore(player);
                    if (player.inventory.containerWear.FindItemsByItemName("hoodie") == null)
                    {
                        if (player.gameObject.GetComponent<ClothingCore.Hoodie>() == null)
                            player.gameObject.AddComponent<ClothingCore.Hoodie>();
                        else return false;
                    }
                    else return false;

                    break;
                case 1581896222:
                    CheckCore(player);
                    if (player.inventory.containerWear.FindItemsByItemName("pants") == null)
                    {
                        if (player.gameObject.GetComponent<ClothingCore.Pants>() == null)
                            player.gameObject.AddComponent<ClothingCore.Pants>();
                        else return false;
                    }
                    else return false;

                    break;
                case 2514895377:
                    CheckCore(player);
                    if (player.inventory.containerWear.FindItemsByItemName("roadsign.kilt") == null)
                    {
                        if (player.gameObject.GetComponent<ClothingCore.Kilt>() == null)
                            player.gameObject.AddComponent<ClothingCore.Kilt>();
                        else return false;
                    }
                    else return false;

                    break;
                case 1088899968:
                    CheckCore(player);
                    if (player.inventory.containerWear.FindItemsByItemName("shoes.boots") == null)
                    {
                        if (player.gameObject.GetComponent<ClothingCore.Boots>() == null)
                            player.gameObject.AddComponent<ClothingCore.Boots>();
                        else return false;
                    }
                    else return false;

                    break;
                case 7:
                    if (player.inventory.containerWear.FindItemsByItemName("tactical.gloves") == null)
                    {
                        CheckCore(player);
                        if (player.gameObject.GetComponent<ClothingCore.Gloves>() == null)
                            player.gameObject.AddComponent<ClothingCore.Gloves>();
                        else return false;
                    }
                    else return false;

                    break;
            }

            return null;
        }

        // ReSharper disable once UnusedMember.Local
        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container.GetOwnerPlayer() == null || item == null) return;
            if (container.HasFlag(ItemContainer.Flag.Clothing)) return;
            var player = container.GetOwnerPlayer();
            if (item.IsChildContainer(player.inventory.containerWear)) return;
            switch (item.skin)
            {
                case 2786314369:
                    CheckCore(player);
                    UnityEngine.Object.Destroy(player.gameObject.GetComponent<ClothingCore.Helmet>());
                    break;
                case 2786313151:
                    CheckCore(player);
                    UnityEngine.Object.Destroy(player.gameObject.GetComponent<ClothingCore.Armor>());
                    break;
                case 1581890527:
                    CheckCore(player);
                    UnityEngine.Object.Destroy(player.gameObject.GetComponent<ClothingCore.Hoodie>());
                    break;
                case 1581896222:
                    CheckCore(player);
                    UnityEngine.Object.Destroy(player.gameObject.GetComponent<ClothingCore.Pants>());
                    break;
                case 2514895377:
                    CheckCore(player);
                    UnityEngine.Object.Destroy(player.gameObject.GetComponent<ClothingCore.Kilt>());
                    break;
                case 1088899968:
                    CheckCore(player);
                    UnityEngine.Object.Destroy(player.gameObject.GetComponent<ClothingCore.Boots>());
                    break;
                case 7:
                    CheckCore(player);
                    UnityEngine.Object.Destroy(player.gameObject.GetComponent<ClothingCore.Gloves>());
                    break;
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (plan == null || go == null) return;
            if (plan.GetOwnerPlayer() == null) return;
            if (go.ToBaseEntity().skinID != 8) return;
            var player = plan.GetOwnerPlayer();
            if (player == null) return;
            var component = player.gameObject.GetComponent<ClothingCore>();
            if (component.Clothing.Count != 7)
            {
                player.ChatMessage("Not permission");
                NextTick(() => go.ToBaseEntity().Kill());
                return;
            }

            if (component.CopterSpawned)
            {
                player.ChatMessage("Copter already spawned");
                return;
            }

            component.copterpos = go.transform.position;
            component.CopterRespawn();
            NextTick(() => go.ToBaseEntity().Kill());
        }

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            _ = this;
            ImageLibrary.Call("AddImage", "https://i.imgur.com/LAjAfoi.png", "UI.Shield.BG");
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            foreach (var cores in ClothingCores) UnityEngine.Object.Destroy(cores.Value);
        }

        #endregion

        #region [ConsoleCommands]

        [ChatCommand("copter")]
        // ReSharper disable once UnusedMember.Local
        private void GiveCopter(BasePlayer player)
        {
            if (!ClothingCores.ContainsKey(player.userID)) return;
            var component = ClothingCores[player.userID];
            if (component.Clothing.Count != 7) return;
            if (component.TimerCooldown > 0)
            {
                var convert = TimeSpan.FromSeconds(160);
                player.ChatMessage($"Copter in colldown {convert.Minutes} m {convert.Seconds} s");
                return;
            }

            if (component.CopterSpawned)
            {
                player.ChatMessage("Copter already spawned");
                return;
            }

            if (component.CopterGiving)
            {
                player.ChatMessage("Copter already giving");
                return;
            }

            var item = ItemManager.CreateByName("box.wooden.large", 1, 8);
            player.GiveItem(item);
            component.CopterGiving = true;
        }

        [ChatCommand("copter.destroy")]
        // ReSharper disable once UnusedMember.Local
        private void DestroyCopter(BasePlayer player)
        {
            if (!ClothingCores.ContainsKey(player.userID)) return;
            var component = ClothingCores[player.userID];
            if (component.Clothing.Count != 7)
            {
                player.ChatMessage("Not wearing all armor");
                return;
            }

            component.CopterGiving = false;
            component.CopterDestroy();
        }

        [ConsoleCommand("modifiedclothing.give")]
        // ReSharper disable once UnusedMember.Local
        private void GiveClothing(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!player.IsAdmin) return;
            var cloth = args.Args[0];
            Item item;
            switch (cloth)
            {
                case "metal.facemask":
                    item = ItemManager.CreateByName("metal.facemask", 1, 2786314369);
                    player.GiveItem(item);
                    break;
                case "metal.plate.torso":
                    item = ItemManager.CreateByName("metal.plate.torso", 1, 2786313151);
                    player.GiveItem(item);
                    break;
                case "shoes.boots":
                    item = ItemManager.CreateByName("shoes.boots", 1, 1088899968);
                    player.GiveItem(item);
                    break;
                case "hoodie":
                    item = ItemManager.CreateByName("hoodie", 1, 1581890527);
                    player.GiveItem(item);
                    break;
                case "pants":
                    item = ItemManager.CreateByName("pants", 1, 1581896222);
                    player.GiveItem(item);
                    break;
                case "roadsign.kilt":
                    item = ItemManager.CreateByName("roadsign.kilt", 1, 2514895377);
                    player.GiveItem(item);
                    break;
                case "tactical.gloves":
                    item = ItemManager.CreateByName("tactical.gloves", 1, 7);
                    player.GiveItem(item);
                    break;
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private void CheckCore(BasePlayer player)
        {
            if (player.gameObject.GetComponent<ClothingCore>() != null) return;
            player.gameObject.AddComponent<ClothingCore>();
        }

        #endregion

        #region [API]

        // ReSharper disable once UnusedMember.Local
        private void IsClearNight(bool IsOn)
        {
            ClearNight = IsOn;
        }

        #endregion
    }
}