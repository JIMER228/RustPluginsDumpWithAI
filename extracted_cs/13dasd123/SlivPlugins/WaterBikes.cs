// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Facepunch;
using Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("WaterBikes", "senyaa", "1.4.0")]
    [Description("Turns snowmobiles into waterbikes")]
    class WaterBikes : RustPlugin
    {
        #region Optional Dependencies
        [PluginReference]
        Plugin ServerRewards, Economics;

        static bool? TakePoints(ulong playerID, int amount) => Instance.ServerRewards?.Call("TakePoints", playerID, amount) as bool?;
        static int? CheckPoints(ulong ID) => Instance.ServerRewards?.Call("CheckPoints", ID) as int?;

        static bool? Economics_Withdraw(string playerId, double amount) => Instance.Economics?.Call("Withdraw", playerId, amount) as bool?;
        #endregion

        #region Dependency Boilerplate
        public static bool CheckDependency(string plugin_name)
        {
            var fields = Instance.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (field.FieldType != typeof(Plugin))
                {
                    continue;
                }

                if (field.Name.ToLower() != plugin_name.ToLower())
                {
                    continue;
                }

                if (field.GetValue(Instance) == null)
                {
                    continue;
                }

                return true;
            }
            return false;
        }

        public static bool CheckDependencyAndPrintWarning(string plugin_name, BasePlayer player = null)
        {
            if (!CheckDependency(plugin_name))
            {
                Instance.PrintError($"{plugin_name} is not installed, but it is used in the config. Execution aborted!");

                if (player != null)
                {
                    Instance.PrintToChat(player, $"<color=red>{plugin_name} is not installed, but it is used in the config. Execution aborted!</color>");
                }

                return false;
            }
            return true;
        }
        #endregion

        #region Constants
        const int HAMMER_ITEMID = 200773292;

        const int BEACHSIDE_TOPOLOGY = 16;
        const int BEACH_TOPOLOGY = 8;
        const int OCEANSIDE_TOPOLOGY = 256;

        const string SPAWN_PERMISSION = "waterbikes.spawn";
        const string FREE_PERMISSION = "waterbikes.free";
        const string DESPAWN_PERMISSION = "waterbikes.despawn";
        const string ADMIN_PERMISSION = "waterbikes.admin";
        const string BUY_PERMISSION = "waterbikes.buy";
        #endregion

        #region Fields
        public static WaterBikes Instance;
        private static Dictionary<BaseNetworkable, WaterBikeComponent> waterbikes;
        private Dictionary<BasePlayer, Snowmobile> playerWaterbikes;
        private List<BasePlayer> cooldownList;
        private ItemDefinition priceDef;
        private Vector3 lastKilledSnowmobilePos; // workaround to get water bikes work properly with spray can reskinning
        #endregion

        #region Configuration
        private class Configuration : SerializableConfiguration
        {
            [JsonProperty("(1.1) Waterbike price (set value to 0 to make it free, use ServerRewards or Economics as a shortname to use RP points or Economics balance respectively)")]
            public Price Price = new Price
            {
                ShortName = "scrap",
                Amount = 75,
                SkinID = 0
            };

            [JsonProperty("(1.2) Spawn cooldown (in seconds)")]
            public int Cooldown = 120;

            [JsonProperty("(1.3) Allow only 1 water bike per player")]
            public bool AllowOnlyOneWaterBikePerPlayer = false;

            [JsonProperty("(1.4) Allow spawning water bikes only on beaches")]
            public bool spawnOnlyOnBeaches = false;

            [JsonProperty("(1.5) Amount of water bikes /buywaterbike command gives")]
            public int AmountPerBuy = 1;

            [JsonProperty("(1.6) Starting fuel")]
            public int startingFuel = 0;

            [JsonProperty("(2.1) Allow picking up the water bike only in building privilege")]
            public bool Allow_Pickup_Only_In_Building_Privilege = false;

            [JsonProperty("(2.2) How much HP is reduced when the water bike is picked up (0-100)")]
            public float HP_Reduction = 25f;

            [JsonProperty("(2.3) Water bike item name")]
            public string ItemName = "Water Bike";

            [JsonProperty("(2.4) Water bike item skin ID")]
            public ulong ItemSkinID = 2935987835;

            [JsonProperty("(2.5) Water bike item ID")]
            public int ItemID = 794443127;

            [JsonProperty("(3.1) Make all snowmobiles waterbikes")]
            public bool Make_All_Snowmobiles_Waterbikes = true;

            [JsonProperty("(3.2) Allow waterbikes to drive on land")]
            public bool Allow_To_Move_On_Land = true;

            [JsonProperty("(4.1) Enable 'boost' button (Left Shift)")]
            public bool enableBoostButton = false;

            [JsonProperty("(4.2) 'Boost' button thrust")]
            public float boostButtonThrust = 10000f;

            [JsonProperty("(4.3) 'Boost' duration (seconds)")]
            public float boostDuration = 5f;

            [JsonProperty("(4.4) 'Boost' cooldown (seconds)")]
            public float boostCooldown = 30f;

            [JsonProperty("(5.1) Engine thrust")]
            public int engineThrust = 5000;

            [JsonProperty("(5.2) Engine thrust on land")]
            public int engineThrustOnLand = 49;

            [JsonProperty("(5.3) Move slowly on grass or roads")]
            public bool slowlyOnGrass = true;

            [JsonProperty("(5.4) Steering scale")]
            public float steeringScale = 0.05f;

            [JsonProperty("(5.5) Automatically flip water bikes")]
            public bool autoFlip = false;

            [JsonProperty("(5.6) Off axis drag")]
            public float offAxisDrag = 0.35f;

            [JsonProperty("(5.7) Buoyancy force")]
            public float buoyancyForce = 730f;

            [JsonProperty("(6.1) Waterbike prefab")]
            public string WaterbikePrefab = "assets/content/vehicles/snowmobiles/tomahasnowmobile.prefab";

            [JsonProperty("(6.2) Thrust point position")]
            public Vector3 ThrustPoint = new Vector3(-0.001150894f, 0.055f, -1.125f);

            [JsonProperty("(6.3) Buoyancy points")]
            public SerializedBuoyancyPoint[] BuoyancyPoints = new SerializedBuoyancyPoint[]
            {
                new SerializedBuoyancyPoint(new Vector3(-0.62f, 0.09f, -1.284f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(0.5f, 0.09f, -1.284f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(-0.68f, 0.09f, -0.028f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(0.54f, 0.09f, -0.028f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(-0.64f, 0.09f, 1.283f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(0.53f, 0.09f, 1.283f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(-0.05f, 0.148f, 3.015f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(-0.05f, 0.129f, 1.81f), 1.3f),
                new SerializedBuoyancyPoint(new Vector3(-0.05f, 0.529f, -0.828f), 1.3f)
            };
        }
        #endregion

        #region Configuration Boilerplate
        static Configuration config;

        private class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        private static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool ValidateConfig(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            var changed = false;
            var oldKeys = new List<string>();

            foreach (var key in currentRaw.Keys)
            {
                if (currentWithDefaults.Keys.Contains(key))
                {
                    continue;
                }

                changed = true;
                oldKeys.Add(key);
            }

            foreach (var key in oldKeys)
            {
                currentRaw.Remove(key);
            }

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                            continue;
                        }

                        if (ValidateConfig(defaultDictValue, currentDictValue))
                        {
                            changed = true;
                        }
                    }
                    continue;
                }

                currentRaw[key] = currentWithDefaults[key];
                changed = true;
            }


            return changed;
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }

                var currentWithDefaults = config.ToDictionary();
                var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);

                if (ValidateConfig(currentWithDefaults, currentRaw))
                {
                    PrintWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                PrintWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Puts($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }
        #endregion

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You don't have permission to run this command",
                ["Spawned"] = "Spawned waterbike",
                ["NoWaterbike"] = "You aren't on a waterbike",
                ["Showing"] = "Showing buoyancy points...",
                ["NotEnough"] = "You don't have enough to buy a waterbike",
                ["Converted"] = "Snowmobile converted into waterbike",
                ["Cooldown"] = "You are on cooldown!",
                ["onlyBeach"] = "You can spawn water bikes only on beaches",
                ["onlyOne"] = "You can have only 1 water bike",
                ["removed"] = "Waterbike is removed",
                ["waterbikeDoesntExist"] = "You don't have a water bike",
                ["boostToast"] = "You can press LSHIFT to boost",
                ["lookat"] = "Look at a water bike",
                ["waterbikeGiven"] = "Water bike has been given to you"
            }, this, "en");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "У вас нет прав для выполнения этой команды",
                ["Spawned"] = "Заспаунен гидроцикл",
                ["NoWaterbike"] = "Не на гидроцикле",
                ["Showing"] = "Показываются точки плавучести...",
                ["NotEnough"] = "У вас не хватает ресурсов для покупки гидроцикла",
                ["Converted"] = "Снегоход переделан в гидроцикл",
                ["Cooldown"] = "Вы не можете спаунить гидроциклы так быстро",
                ["onlyBeach"] = "Вы можете заспаунить гидроцикл только на пляже",
                ["onlyOne"] = "Только 1 гидроцикл на игрока",
                ["removed"] = "Гидроцикл удалён",
                ["waterbikeDoesntExist"] = "У вас нет гидроцикла",
                ["boostToast"] = "Вы можете нажать LSHIFT для ускорения",
                ["lookat"] = "Смотрите на гидроцикл",
                ["waterbikeGiven"] = "Вам был выдан гидроцикл"
            }, this, "ru");
        }
        #endregion

        #region Types
        public struct Price
        {
            public string ShortName;
            public int Amount;
            public ulong SkinID;

            public Price(string ShortName, int Amount, ulong SkinID)
            {
                this.ShortName = ShortName;
                this.Amount = Amount;
                this.SkinID = SkinID;
            }

            public bool TryPaying(BasePlayer player, string bypass_permission, string noPointsText, string noResourcesText)
            {
                var normalizedName = ShortName.ToLower();

                if ((normalizedName == "serverrewards" || normalizedName == "economics") && !CheckDependencyAndPrintWarning(normalizedName, player))
                {
                    return false;
                }


                if (!Instance.permission.UserHasPermission(player.UserIDString, bypass_permission) && Amount > 0)
                {
                    switch (normalizedName)
                    {
                        case "serverrewards":
                            var points = CheckPoints(player.userID);

                            if (points == null || points < Amount)
                            {
                                Instance.PrintToChat(player, noPointsText);
                                return false;
                            }

                            TakePoints(player.userID, Amount);
                            break;
                        case "economics":
                            if (Economics_Withdraw(player.UserIDString, Amount) == false)
                            {
                                Instance.PrintToChat(player, noPointsText);
                                return false;
                            }
                            break;
                        default:
                            var itemDef = ItemManager.FindItemDefinition(ShortName);

                            if (SkinID != 0)
                            {
                                var playerItems = Pool.GetList<Item>();
                                var itemsToCollect = Pool.GetList<Item>();

                                player.inventory.AllItemsNoAlloc(ref playerItems);

                                foreach (var item in playerItems)
                                {
                                    if (item.skin == SkinID && item.info.shortname == ShortName && item.amount > 0)
                                    {
                                        itemsToCollect.Add(item);
                                    }
                                }

                                if (itemsToCollect.Count < Amount)
                                {
                                    Instance.PrintToChat(player, string.Format(noResourcesText, itemDef.displayName.english.ToLower()));
                                    return false;
                                }

                                foreach (var item in itemsToCollect)
                                {
                                    item.RemoveFromContainer();
                                }

                                Pool.FreeList(ref itemsToCollect);
                                Pool.FreeList(ref playerItems);

                                break;
                            }

                            if (player.inventory.GetAmount(itemDef.itemid) < Amount)
                            {
                                Instance.PrintToChat(player, string.Format(noResourcesText, itemDef.displayName.english.ToLower()));
                                return false;
                            }

                            player.inventory.Take(null, itemDef.itemid, Amount);

                            break;
                    }
                }

                return true;
            }
        }

        private struct SerializedBuoyancyPoint
        {
            public Vector3 Position;
            public float Size;
            public SerializedBuoyancyPoint(Vector3 Position, float Size)
            {
                this.Position = Position;
                this.Size = Size;
            }
        }

        private class WaterBikeComponent : FacepunchBehaviour
        {
            private Snowmobile _snowmobile;
            private Buoyancy _buoyancy;
            private Rigidbody _rigidbody;

            public GameObject thrustPoint;
            private GameObject parentPoint;

            private float gasPedal;
            private float steering;

            private int framesSinceLastFlip;
            private int framesSinceLastInWater;

            private bool boosting;
            private bool boostCooldown;

            private Vector3 _waterLoggedPointLocalPosition;

            private int framesSinceLastControl;

            void Awake()
            {
                _snowmobile = GetComponent<Snowmobile>();
                _rigidbody = _snowmobile.gameObject.GetComponent<Rigidbody>();

                _snowmobile.engineKW = config.Allow_To_Move_On_Land ? config.engineThrustOnLand : -1;

                if (_snowmobile.waterloggedPoint != null && _snowmobile.waterloggedPoint.parent != null)
                {
                    _waterLoggedPointLocalPosition = _snowmobile.waterloggedPoint.localPosition;
                    _snowmobile.waterloggedPoint.SetParent(null);
                    _snowmobile.waterloggedPoint.position = new Vector3(0, 2000, 0);
                }

                thrustPoint = new GameObject("ThrustPoint");
                thrustPoint.transform.SetParent(_snowmobile.transform, false);
                thrustPoint.transform.localPosition = config.ThrustPoint;
                InitBuoyancy();
                _snowmobile.SetFlag(BaseEntity.Flags.Reserved10, true, true, true);
                waterbikes.Add(_snowmobile, this);

                _snowmobile.SendNetworkUpdateImmediate();
            }

            void OnDestroy()
            {
                Destroy(_buoyancy);
                Destroy(parentPoint);
                Destroy(thrustPoint);
                if (_snowmobile.IsDestroyed)
                {
                    return;
                }

                _snowmobile.waterloggedPoint.SetParent(_snowmobile.transform);
                _snowmobile.waterloggedPoint.transform.localPosition = _waterLoggedPointLocalPosition;
                _snowmobile.engineKW = 49;
                _snowmobile.SendNetworkUpdateImmediate();
                waterbikes.Remove(_snowmobile);
            }

            private void InitBuoyancy()
            {
                _buoyancy = _snowmobile.gameObject.AddComponent<Buoyancy>();

                _buoyancy.forEntity = _snowmobile;
                _buoyancy.rigidBody = _rigidbody;

                _buoyancy.doEffects = true;

                _buoyancy.requiredSubmergedFraction = 0f;

                _buoyancy.useUnderwaterDrag = true;
                _buoyancy.underwaterDrag = 20f;

                _buoyancy.flatWaterLerp = 1f; 

                var points = new BuoyancyPoint[config.BuoyancyPoints.Length];

                parentPoint = new GameObject("buoyancy");

                for (var i = 0; i < points.Length; i++)
                {
                    var go = new GameObject($"buoyancyPoint_{i}");

                    go.transform.SetParent(parentPoint.transform, false);
                    go.transform.localPosition = config.BuoyancyPoints[i].Position;

                    var point = go.AddComponent<BuoyancyPoint>();

                    point.buoyancyForce = config.buoyancyForce;
                    point.size = config.BuoyancyPoints[i].Size;
                    points[i] = point;
                }

                parentPoint.transform.SetParent(_snowmobile.transform, false);
                parentPoint.transform.localPosition = new Vector3(0.046f, -0.15f, -0.853f);

                _buoyancy.points = points;
            }

            void FixedUpdate()
            {
                if (Time.frameCount - framesSinceLastControl >= 7)
                {
                    ProcessPlayerInput();
                    framesSinceLastControl = Time.frameCount;
                }

                var isInWater = WaterLevel.Test(thrustPoint.transform.position, true, _snowmobile);
                if (isInWater)
                {
                    framesSinceLastInWater = 0;
                }
                else
                {
                    framesSinceLastInWater += 1;
                }

                framesSinceLastFlip += 1;
                if (config.autoFlip && IsFlipped() && framesSinceLastFlip > 30f && isInWater)
                {
                    Flip();
                    framesSinceLastFlip = 0;
                    return;
                }

                if (IsFlipped() && _snowmobile.engineController.IsOn)
                {
                    _snowmobile.engineController.StopEngine();
                    Flip();
                    return;
                }
                _snowmobile.SetFlag(BaseEntity.Flags.Reserved7, _rigidbody.IsSleeping() && !_snowmobile.AnyMounted(), false, true);
                if (!_snowmobile.engineController.IsOn)
                {
                    gasPedal = 0f;
                    steering = 0f;
                }
                _snowmobile.SetFlag(BaseEntity.Flags.Reserved7, _rigidbody.IsSleeping() && !_snowmobile.AnyMounted(), false, true);

                if (gasPedal != 0f && isInWater && _buoyancy.submergedFraction > 0.3f)
                {
                    var force = (transform.forward + (transform.right * steering * config.steeringScale)).normalized * gasPedal * (boosting ? config.boostButtonThrust : config.engineThrust);
                    _rigidbody.AddForceAtPosition(force, thrustPoint.transform.position, ForceMode.Force);
                    _snowmobile.engineKW = 65;
                }
                else
                {
                    _snowmobile.engineKW = config.Allow_To_Move_On_Land ? config.engineThrustOnLand : 1;
                }

                if (!config.slowlyOnGrass || framesSinceLastInWater < 100 || TerrainMeta.TopologyMap.GetTopology(_snowmobile.transform.position, OCEANSIDE_TOPOLOGY))
                {
                    _rigidbody.drag = 0.2f + (0.6f * Mathf.InverseLerp(0f, 1f, _buoyancy.submergedFraction));
                    _rigidbody.angularDrag = 0.5f + (0.005f * Mathf.InverseLerp(0f, 2f, _rigidbody.velocity.SqrMagnitude2D()));
                }

                parentPoint.transform.rotation = _snowmobile.transform.rotation;
                if (config.offAxisDrag > 0f)
                {
                    var value2 = Vector3.Dot(transform.forward, _rigidbody.velocity.normalized);
                    var num2 = Mathf.InverseLerp(0.98f, 0.92f, value2);
                    _rigidbody.drag += num2 * config.offAxisDrag * _buoyancy.submergedFraction;
                }

                var x = Mathf.InverseLerp(1f, 10f, _rigidbody.velocity.Magnitude2D()) * 0.5f * _snowmobile.healthFraction;

                if (!_snowmobile.engineController.IsOn)
                {
                    x = 0f;
                }

                var y = 1f - (0.3f * (1f - _snowmobile.healthFraction));

                _buoyancy.buoyancyScale = (1f + x) * y;
            }


            public void ProcessPlayerInput()
            {
                if (_snowmobile == null || _snowmobile.IsDestroyed)
                {
                    return;
                }

                if (!_snowmobile.AnyMounted())
                {
                    return;
                }

                var driver = _snowmobile.GetDriver();
                var inputState = driver?.serverInput;

                if (driver == null || inputState == null)
                {
                    return;
                }

                if (inputState.IsDown(BUTTON.SPRINT) && !boosting && !boostCooldown && config.enableBoostButton)
                {
                    boosting = true;
                    boostCooldown = true;
                    Instance.timer.Once(config.boostDuration, () => { boosting = false; });
                    Instance.timer.Once(config.boostCooldown, () => { boostCooldown = false; });
                    SendEffect("assets/prefabs/tools/pager/effects/beep.prefab", driver, transform.position);
                }


                if (inputState.IsDown(BUTTON.FORWARD))
                {
                    gasPedal = 1f;
                }
                else if (inputState.IsDown(BUTTON.BACKWARD))
                {
                    gasPedal = -0.5f;
                }
                else
                {
                    gasPedal = 0f;
                }

                if (inputState.IsDown(BUTTON.LEFT))
                {
                    steering = 1f;
                    return;
                }

                if (inputState.IsDown(BUTTON.RIGHT))
                {
                    steering = -1f;
                    return;
                }
                steering = 0f;
            }

            public bool IsFlipped()
            {
                return Vector3.Dot(Vector3.up, transform.up) <= 0f;
            }

            public void Flip()
            {
                _rigidbody.AddRelativeTorque(Vector3.right * 4f, ForceMode.VelocityChange);
                _rigidbody.AddForce(Vector3.up * 4f, ForceMode.VelocityChange);
            }
        }
        #endregion

        #region Hooks
        void Init()
        {
            Instance = this;
            waterbikes = new Dictionary<BaseNetworkable, WaterBikeComponent>();
            playerWaterbikes = new Dictionary<BasePlayer, Snowmobile>();
            cooldownList = Facepunch.Pool.GetList<BasePlayer>();

            permission.RegisterPermission(ADMIN_PERMISSION, this);
            permission.RegisterPermission(SPAWN_PERMISSION, this);
            permission.RegisterPermission(FREE_PERMISSION, this);
            permission.RegisterPermission(DESPAWN_PERMISSION, this);
            permission.RegisterPermission(BUY_PERMISSION, this);
        }

        void Unload()
        {
            foreach (var obj in UnityEngine.Object.FindObjectsOfType<WaterBikeComponent>())
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }

            waterbikes = null;
            playerWaterbikes = null;
            Pool.FreeList(ref cooldownList);
            config = null;
            Instance = null;
        }

        object CanBuild(Planner plan, Construction prefab, Construction.Target target)
        {
            if (plan == null)
            {
                return null;
            }

            var item = plan.GetItem();
            if (item == null)
            {
                return null;
            }

            var player = plan.GetOwnerPlayer();
            if (player == null)
            {
                return null;
            }

            if (!(item.info.itemid == config.ItemID && item.skin == config.ItemSkinID))
            {
                return null;
            }

            if (config.AllowOnlyOneWaterBikePerPlayer && playerWaterbikes.ContainsKey(player) && playerWaterbikes[player] != null)
            {
                PrintToChat(player, lang.GetMessage("onlyOne", this, player.UserIDString));
                return false;
            }

            if (cooldownList.Contains(player))
            {
                PrintToChat(player, lang.GetMessage("Cooldown", this, player.UserIDString));
                return false;
            }

            if (config.spawnOnlyOnBeaches && !(TerrainMeta.TopologyMap.GetTopology(player.transform.position, BEACH_TOPOLOGY) || TerrainMeta.TopologyMap.GetTopology(player.transform.position, BEACHSIDE_TOPOLOGY)))
            {
                PrintToChat(player, lang.GetMessage("onlyBeach", this, player.UserIDString));
                return false;
            }

            return null;
        }


        void OnEntityBuilt(Planner plan, GameObject go)
        {
            if (plan == null)
            {
                return;
            }

            var item = plan.GetItem();
            if (item == null)
            {
                return;
            }

            var player = plan.GetOwnerPlayer();
            if (player == null)
            {
                return;
            }

            if (item.info.itemid == config.ItemID && item.skin == config.ItemSkinID)
            {
                StartCooldown(player);
                SpawnWaterbike(go.transform.position, go.transform.rotation, player, item.conditionNormalized);

                NextFrame(() =>
                {
                    go.GetComponent<BaseEntity>().AdminKill();
                });
            }
        }

        void OnServerInitialized(bool initial)
        {
            var count = 0;

            foreach (var ent in BaseNetworkable.serverEntities)
            {
                if (!(ent is Snowmobile))
                {
                    continue;
                }

                if ((config.Make_All_Snowmobiles_Waterbikes || (ent as BaseEntity).HasFlag(BaseEntity.Flags.Reserved10)) && !waterbikes.ContainsKey(ent))
                {
                    ent.gameObject.AddComponent<WaterBikeComponent>();
                    var snowmobile = ent as Snowmobile;
                    if (snowmobile.OwnerID != 0)
                    {
                        var player = BasePlayer.FindByID(snowmobile.OwnerID);
                        if (player == null)
                        {
                            continue;
                        }

                        if (playerWaterbikes.ContainsKey(player))
                        {
                            playerWaterbikes[player] = snowmobile;
                        }
                        else
                        {
                            playerWaterbikes.Add(player, snowmobile);
                        }
                    }
                    count++;
                }
            }

            Puts($"Loaded {count} waterbikes");
        }

        void OnEngineStarted(BaseVehicle vehicle, BasePlayer driver)
        {
            if (driver == null || vehicle == null)
            {
                return;
            }

            if (!(vehicle is Snowmobile))
            {
                return;
            }

            if (!waterbikes.ContainsKey(vehicle))
            {
                return;
            }

            if (config.enableBoostButton)
            {
                driver.ShowToast(GameTip.Styles.Blue_Normal, lang.GetMessage("boostToast", this, driver.UserIDString));
            }

            if (config.Allow_To_Move_On_Land)
            {
                return;
            }

            if (!WaterLevel.Test(waterbikes[vehicle].thrustPoint.transform.position, true, vehicle))
            {
                timer.Once(0.1f, () =>
                {
                    if (vehicle != null)
                    {
                        (vehicle as Snowmobile).engineController.StopEngine();
                    }
                });
            }
        }

        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!(entity is Snowmobile))
            {
                return;
            }

            if (lastKilledSnowmobilePos == entity.transform.position)
            {
                entity.gameObject.AddComponent<WaterBikeComponent>();
                lastKilledSnowmobilePos = new Vector3();
                return;
            }

            if (!config.Make_All_Snowmobiles_Waterbikes)
            {
                return;
            }

            if (waterbikes.ContainsKey(entity))
            {
                return;
            }

            entity.gameObject.AddComponent<WaterBikeComponent>();
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity == null)
            {
                return;
            }

            if (!waterbikes.ContainsKey(entity))
            {
                return;
            }

            lastKilledSnowmobilePos = entity.transform.position;
            var snowmobile = entity as Snowmobile;

            if (!playerWaterbikes.ContainsValue(snowmobile))
            {
                return;
            }

            var player = BasePlayer.FindByID(snowmobile.OwnerID);
            if (player != null)
            {
                playerWaterbikes.Remove(player);
            }
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (!cooldownList.Contains(player))
            {
                return;
            }

            cooldownList.Remove(player);
        }

        object CanMountEntity(BasePlayer player, BaseMountable seat)
        {
            if (seat?.parentEntity == null)
            {
                return null;
            }

            var entity = seat.parentEntity.Get(true);

            if (entity == null || entity.IsDestroyed || !waterbikes.ContainsKey(entity))
            {
                return null;
            }

            var activeItem = player.GetActiveItem();

            if (((!config.Allow_Pickup_Only_In_Building_Privilege && !player.IsBuildingBlocked()) ||
                (config.Allow_Pickup_Only_In_Building_Privilege && player.IsBuildingAuthed())) &&
                activeItem != null && activeItem.info.itemid == HAMMER_ITEMID)
            {
                var condition = entity.Health() / entity.MaxHealth() * 100f;

                player.GiveItem(CreateItem(1, condition - config.HP_Reduction), BaseEntity.GiveItemReason.PickedUp);
                entity.AdminKill();
                return false;
            }

            return null;
        }
        #endregion

        #region Methods
        public Vector3 GetSpawnPosition(BasePlayer player)
        {
            RaycastHit hit;
            Vector3 position;
            if (Physics.Raycast(player.eyes.position, player.eyes.HeadForward(), out hit, 15f))
            {
                position = hit.point + new Vector3(0, 1.2f, 0);
            }
            else
            {
                position = player.transform.position + new Vector3(0, 0.2f, 0);
            }

            return position;
        }

        public void StartCooldown(BasePlayer player)
        {
            if (player == null)
            {
                return;
            }

            if (config.Cooldown <= 0)
            {
                return;
            }

            if (cooldownList.Contains(player))
            {
                return;
            }

            cooldownList.Add(player);

            timer.Once(config.Cooldown, () =>
            {
                if (cooldownList.Contains(player))
                {
                    cooldownList.Remove(player);
                }
            });
        }

        public static void WriteLine(BasePlayer player, string message)
        {
            if (player == null)
            {
                Instance.Puts(message);
                return;
            }
            Instance.PrintToConsole(player, $"[{Instance.Name}] " + message);
        }

        public static void SendEffect(string effectPath, BasePlayer player, Vector3 position)
        {
            if (player == null)
            {
                return;
            }

            if (!Net.sv.IsConnected())
            {
                return;
            }

            var effect = new Effect(effectPath, position, position);
            effect.pooledstringid = StringPool.Get(effect.pooledString);

            var netWrite = Net.sv.StartWrite();
            netWrite.PacketID(Message.Type.Effect);
            effect.WriteToStream(netWrite);
            netWrite.Send(new SendInfo(player.net.connection));
        }

        public Item CreateItem(int amount = 1, float condition = 100f)
        {
            var item = ItemManager.CreateByItemID(config.ItemID, amount, config.ItemSkinID);
            item.name = config.ItemName;
            item.condition = condition;
            return item;
        }
        #endregion

        #region Console Commands
        [ConsoleCommand("givewaterbike")]
        void GiveWaterbikeCCmd(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();

            if (player != null && !permission.UserHasPermission(player.UserIDString, ADMIN_PERMISSION))
            {
                WriteLine(player, "You don't have permission to run this console command!");
                return;
            }

            var args = arg.Args;
            if (args == null || args?.Length == 0)
            {
                WriteLine(player, "Usage: givewaterbike <amount> <player name>");
                return;
            }

            var amount = 1;
            var skips_first_arg = false;

            if (arg.Args[0].IsNumeric() && arg.Args.Length > 1)
            {
                amount = Convert.ToInt32(arg.Args[0]);
                skips_first_arg = true;
            }

            var target = BasePlayer.Find(string.Join(" ", skips_first_arg ? arg.Args.Skip(1) : arg.Args));
            if (target == null)
            {
                WriteLine(player, "Player not found!");
                return;
            }

            target.GiveItem(CreateItem(amount));
            PrintToChat(target, lang.GetMessage("waterbikeGiven", this, target.UserIDString));

            WriteLine(player, $"{amount} water bike(s) were given to " + target.displayName);
        }
        #endregion

        #region Chat Commands
        [ChatCommand("waterbike_debug")]
        private void WaterbikeDebugCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, ADMIN_PERMISSION))
            {
                PrintToChat(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                return;
            }
            var vehicle = player.GetMountedVehicle();

            if (vehicle == null || !waterbikes.ContainsKey(vehicle))
            {
                PrintToChat(player, lang.GetMessage("NoWaterbike", this, player.UserIDString));
                return;
            }

            var buoy = vehicle.gameObject.GetComponent<Buoyancy>();
            var waterbike = waterbikes[vehicle];

            PrintToChat(player, lang.GetMessage("Showing", this, player.UserIDString));

            foreach (var point in buoy.points)
            {
                player.SendConsoleCommand("ddraw.text", 30f, Color.green, point.transform.position, $"<size=13>Force - {point.buoyancyForce}\nSize = {point.size}</size>");
                player.SendConsoleCommand("ddraw.box", 30f, Color.green, point.transform.position, point.size);
            }

            player.SendConsoleCommand("ddraw.box", 30f, Color.blue, waterbike.thrustPoint.transform.position, 0.1f);
            player.SendConsoleCommand("ddraw.text", 30f, Color.blue, waterbike.thrustPoint.transform.position, "<size=13>Thrust point</size>");
        }


        [ChatCommand("buywaterbike")]
        void BuyWaterbikeCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, BUY_PERMISSION))
            {
                PrintToChat(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                return;
            }

            var notEnoughMsg = lang.GetMessage("NotEnough", this, player.UserIDString);

            if (config.Price.TryPaying(player, FREE_PERMISSION, notEnoughMsg, notEnoughMsg))
            {
                player.GiveItem(CreateItem(config.AmountPerBuy));
                PrintToChat(player, lang.GetMessage("waterbikeGiven", this, player.UserIDString));
            }
        }

        [ChatCommand("waterbike")]
        private void WaterbikeCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, SPAWN_PERMISSION))
            {
                PrintToChat(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                return;
            }

            if (args.Length == 1 && args[0].ToLower() == "remove")
            {
                if (!permission.UserHasPermission(player.UserIDString, DESPAWN_PERMISSION))
                {
                    PrintToChat(player, lang.GetMessage("NoPermission", this, player.UserIDString));
                    return;
                }

                if (config.AllowOnlyOneWaterBikePerPlayer)
                {
                    if (!playerWaterbikes.ContainsKey(player) || playerWaterbikes[player] == null)
                    {
                        PrintToChat(player, lang.GetMessage("waterbikeDoesntExist", this, player.UserIDString));
                        return;
                    }

                    playerWaterbikes[player].DismountAllPlayers();
                    playerWaterbikes[player].Kill(BaseNetworkable.DestroyMode.Gib);
                    PrintToChat(player, lang.GetMessage("removed", this, player.UserIDString));
                    return;
                }

                RaycastHit despawn_hit;
                if (Physics.Raycast(player.eyes.position, player.eyes.HeadForward(), out despawn_hit, 15f))
                {
                    var hit_ent = despawn_hit.GetEntity();
                    if (hit_ent != null && hit_ent is Snowmobile && waterbikes.ContainsKey(hit_ent))
                    {
                        hit_ent.Kill(BaseNetworkable.DestroyMode.Gib);
                        PrintToChat(player, lang.GetMessage("removed", this, player.UserIDString));
                        return;
                    }
                }
                PrintToChat(player, lang.GetMessage("lookat", this, player.UserIDString));
                return;
            }

            if (config.AllowOnlyOneWaterBikePerPlayer && playerWaterbikes.ContainsKey(player) && playerWaterbikes[player] != null)
            {
                PrintToChat(player, lang.GetMessage("onlyOne", this, player.UserIDString));
                return;
            }

            if (cooldownList.Contains(player))
            {
                PrintToChat(player, lang.GetMessage("Cooldown", this, player.UserIDString));
                return;
            }

            if (config.spawnOnlyOnBeaches && !(TerrainMeta.TopologyMap.GetTopology(player.transform.position, BEACH_TOPOLOGY) || TerrainMeta.TopologyMap.GetTopology(player.transform.position, BEACHSIDE_TOPOLOGY)))
            {
                PrintToChat(player, lang.GetMessage("onlyBeach", this, player.UserIDString));
                return;
            }

            RaycastHit hit;
            BaseEntity ent = null;

            if (Physics.Raycast(player.eyes.position, player.eyes.HeadForward(), out hit, 15f))
            {
                var hit_ent = hit.GetEntity();
                if (hit_ent != null && hit_ent is Snowmobile && !waterbikes.ContainsKey(hit_ent))
                {
                    ent = hit_ent;
                }
            }

            var notEnoughMsg = lang.GetMessage("NotEnough", this, player.UserIDString);

            if (config.Price.TryPaying(player, FREE_PERMISSION, notEnoughMsg, notEnoughMsg))
            {
                if (ent != null)
                {
                    ent.gameObject.AddComponent<WaterBikeComponent>();
                    PrintToChat(player, lang.GetMessage("Converted", this, player.UserIDString));
                }
                else
                {
                    SpawnWaterbike(GetSpawnPosition(player), player.eyes.rotation, player);
                    StartCooldown(player);
                    PrintToChat(player, lang.GetMessage("Spawned", this, player.UserIDString));
                }

                return;
            }
        }
        #endregion

        #region API
        [HookMethod("SpawnWaterbike")]
        public BaseEntity SpawnWaterbike(Vector3 position, Quaternion rotation, BasePlayer ownerPlayer = null, float hp_fraction = 1f)
        {
            var waterBike = GameManager.server.CreateEntity(config.WaterbikePrefab, position, rotation) as Snowmobile;
            waterBike.gameObject.AddComponent<WaterBikeComponent>();
            waterBike.Spawn();

            if (config.AllowOnlyOneWaterBikePerPlayer && ownerPlayer != null)
            {
                waterBike.OwnerID = ownerPlayer.userID;
                playerWaterbikes.Add(ownerPlayer, waterBike);
            }

            if (config.startingFuel > 0)
            {
                waterBike.GetFuelSystem().AddStartingFuel(config.startingFuel);
            }

            waterBike.health *= hp_fraction;

            waterBike.SendNetworkUpdateImmediate();

            return waterBike;
        }

        [HookMethod("CreateWaterbikeItem")]
        public Item CreateWaterbikeItem()
        {
            return CreateItem();
        }
        #endregion
    }
}
