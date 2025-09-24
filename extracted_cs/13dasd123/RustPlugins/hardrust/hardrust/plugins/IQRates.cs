using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("IQRates", "https://topplugin.ru/ / https://discord.com/invite/5DPTsRmd3G", "0.0.8")]
    [Description("Настройка рейтинга на сервере")]
    class IQRates : RustPlugin
    {
        /// <summary>
        /// Обновление 0.0.6
        /// - Исправил добычу в стандартных карьерах(которые просто спавнятся на сервере)
        /// - Добавил скорость плавки печей
        /// - Добавил возможность включения или отключения скорости плавки
        /// - Добавил возможность включения или отключения кастомного спавно каждого ивента
        /// - Исправил проблему с удалением чинука во время вылета на OilRig
        /// </summary>
        ///  /// Обновление 0.0.7
        /// - Убрал лишний тип
        /// - Убрал лишнюю настройку в конфигурации
        /// - Исправил NRE в Unload

        #region Vars
        public List<uint> LootersListCrateID = new List<uint>();
        #endregion

        #region Configuration

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Настройка плагина")]
            public PluginSettings pluginSettings = new PluginSettings();

            internal class PluginSettings
            {
                [JsonProperty("Настройка рейтингов")]
                public Rates RateSetting = new Rates();
                [JsonProperty("Дополнительная настройка плагина")]
                public OtherSettings OtherSetting = new OtherSettings();
                internal class Rates
                {
                    [JsonProperty("Настройка рейтинга днем")]
                    public AllRates DayRates = new AllRates();
                    [JsonProperty("Настройка рейтинга ночью")]
                    public AllRates NightRates = new AllRates();
                    [JsonProperty("Настройка привилегий и рейтингов конкретно для них [iqrates.vip] = { Настройка } (По убыванию)")]
                    public Dictionary<string, AllRates> PrivilegyRates = new Dictionary<string, AllRates>();
                    [JsonProperty("Настройка кастомных предметов и рейтинг к ним")]
                    public Dictionary<string, float> CustomRatesAll = new Dictionary<string, float>();
                    [JsonProperty("Настройка кастомных рейтов(предметов) по пермишенсу [permissions] - настройка (По убыванию)")]
                    public List<PermissionsRate> CustomRatesPermissions = new List<PermissionsRate>();
                    [JsonProperty("Черный лист предметов,на которые катигорично не будут действовать рейтинг")]
                    public List<string> BlackList = new List<string>();
                    [JsonProperty("Включить скорость плавки в печах(true - да/false - нет)")]
                    public bool UseSpeedBurnable;
                    [JsonProperty("Скорость плавки печей")]
                    public float SpeedBurnable;
                    public class PermissionsRate
                    {
                        public string Permissions;
                        public string Shortname;
                        public float Rate;
                    }
                    internal class AllRates
                    {
                        [JsonProperty("Рейтинг добываемых ресурсов")]
                        public float GatherRate;
                        [JsonProperty("Рейтинг найденных предметов")]
                        public float LootRate;
                        [JsonProperty("Рейтинг поднимаемых предметов")]
                        public float PickUpRate;
                        [JsonProperty("Рейтинг карьеров")]
                        public float QuarryRate;
                        [JsonProperty("Рейтинг экскаватора")]
                        public float ExcavatorRate;
                        [JsonProperty("Шанс выпадения угля")]
                        public float CoalRare;
                    }
                }
                internal class OtherSettings
                {
                    [JsonProperty("Настройки времени появления ивентов на сервере")]
                    public EventSettings EventSetting = new EventSettings();

                    [JsonProperty("Использовать ускорение времени")]
                    public bool UseTime;
                    [JsonProperty("Укажите во сколько будет начинаться день")]
                    public int DayStart;
                    [JsonProperty("Укажите во сколько будет начинаться ночь")]
                    public int NightStart;
                    [JsonProperty("Укажите сколько будет длится день в минутах")]
                    public int DayTime;
                    [JsonProperty("Укажите сколько будет длится ночь в минутах")]
                    public int NightTime;

                    internal class EventSettings
                    {
                        [JsonProperty("Включить скорость кастомный вылет вертолета(true - да/false - нет)")]
                        public bool UseEventHelicopter;
                        [JsonProperty("Раз сколько времени будет вылетать вертолет(в секундах)")]
                        public int EventHelicopter;
                        [JsonProperty("Включить скорость кастомный выплыв корабля(true - да/false - нет)")]
                        public bool UseEventCargoShip;
                        [JsonProperty("Раз сколько времени будет выплывать корабль(в секундах)")]
                        public int EventCargoShip;
                        [JsonProperty("Включить скорость кастомный вылет аирдропа(true - да/false - нет)")]
                        public bool UseEventAirdrops;
                        [JsonProperty("Раз сколько времени будет вылетать аирдроп(в секундах)")]
                        public int EventAirdrop;
                        [JsonProperty("Включить скорость кастомный вылет чинука(true - да/false - нет)")]
                        public bool UseEventChinoock;
                        [JsonProperty("Раз сколько времени будет вылетать чинук(в секундах)")]
                        public int EventChinoock;
                        [JsonProperty("Включить скорость кастомный спавн танка(true - да/false - нет)")]
                        public bool UseEventBradley;
                        [JsonProperty("Раз сколько времени будет спавнится танк(в секундах)")] 
                        public int EventBreadley;
                    }
                }
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    pluginSettings = new PluginSettings
                    {
                        RateSetting = new PluginSettings.Rates
                        {
                            UseSpeedBurnable = true,
                            SpeedBurnable = 3.5f,
                            DayRates = new PluginSettings.Rates.AllRates
                            {
                                GatherRate = 1.0f,
                                LootRate = 1.0f,
                                PickUpRate = 1.0f,
                                QuarryRate = 1.0f,
                                ExcavatorRate = 1.0f,
                                CoalRare = 10,
                            },
                            NightRates = new PluginSettings.Rates.AllRates
                            {
                                GatherRate = 2.0f,
                                LootRate = 2.0f,
                                PickUpRate = 2.0f,
                                QuarryRate = 2.0f,
                                ExcavatorRate = 2.0f,
                                CoalRare = 15,
                            }, 
                            CustomRatesPermissions = new List<PluginSettings.Rates.PermissionsRate>
                            {
                               new PluginSettings.Rates.PermissionsRate
                               {
                                   Permissions = "iqrates.gg",
                                   Rate = 400.0f,
                                   Shortname = "wood",
                               },
                               new PluginSettings.Rates.PermissionsRate
                               {
                                   Permissions = "iqrates.gg",
                                   Rate = 400.0f,
                                   Shortname = "stones",
                               },
                            },
                            PrivilegyRates = new Dictionary<string, PluginSettings.Rates.AllRates>
                            {
                                ["iqrates.vip"] = new PluginSettings.Rates.AllRates
                                {
                                    GatherRate = 3.0f,
                                    LootRate = 3.0f,
                                    PickUpRate = 3.0f,
                                    QuarryRate = 3.0f,
                                    ExcavatorRate = 3.0f,
                                    CoalRare = 15,
                                },
                                ["iqrates.premium"] = new PluginSettings.Rates.AllRates
                                {
                                    GatherRate = 3.5f,
                                    LootRate = 3.5f,
                                    PickUpRate = 3.5f,
                                    QuarryRate = 3.5f,
                                    ExcavatorRate = 3.5f,
                                    CoalRare = 20,
                                },
                            },
                            CustomRatesAll = new Dictionary<string, float>
                            {
                                ["wood"] = 5.0f,
                                ["stones"] = 3.5f,
                                ["scrap"] = 10.0f
                            },
                            BlackList = new List<string>
                            {
                                "sulfur.ore",
                            },
                        },
                        OtherSetting = new PluginSettings.OtherSettings
                        {
                            UseTime = false,
                            DayStart = 10,
                            NightStart = 22,
                            DayTime = 5,
                            NightTime = 1,
                            EventSetting = new PluginSettings.OtherSettings.EventSettings
                            {
                                UseEventAirdrops = false,
                                UseEventBradley = false,
                                UseEventCargoShip = false,
                                UseEventChinoock = false,
                                UseEventHelicopter = false,
                                EventAirdrop = 3000,
                                EventCargoShip = 2500,
                                EventChinoock = 5000,
                                EventHelicopter = 4000,
                                EventBreadley = 3000,
                            }
                        },
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка #1344" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Metods
        public void Register(string Permissions)
        {
            if (!String.IsNullOrWhiteSpace(Permissions))
                if (!permission.PermissionExists(Permissions, this))
                    permission.RegisterPermission(Permissions, this);
        }

        #region Events
        private const string prefabCH47 = "assets/prefabs/npc/ch47/ch47scientists.entity.prefab";
        private const string prefabPlane = "assets/prefabs/npc/cargo plane/cargo_plane.prefab";
        private const string prefabShip = "assets/content/vehicles/boats/cargoship/cargoshiptest.prefab";
        private const string prefabPatrol = "assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab";
        void StartEvent()
        {
            var EvenTimer = config.pluginSettings.OtherSetting.EventSetting;
            if (EvenTimer.UseEventChinoock)
                timer.Every(EvenTimer.EventChinoock, () => { SpawnCH47(); });
            if (EvenTimer.UseEventCargoShip)
                timer.Every(EvenTimer.EventCargoShip, () => { SpawnCargo(); });
            if (EvenTimer.UseEventHelicopter)
                timer.Every(EvenTimer.EventHelicopter, () => { SpawnHeli(); });
            if (EvenTimer.UseEventAirdrops)
                timer.Every(EvenTimer.EventAirdrop, () => { SpawnPlane(); });
            if (EvenTimer.UseEventBradley)
                timer.Every(EvenTimer.EventBreadley, () => { SpawnTank(); });
        }
        private void UnSubProSub(int time = 1)
        {
            Unsubscribe("OnEntitySpawned");
            timer.Once(time, () =>
            {
                Subscribe("OnEntitySpawned");
            });
        }
        void SpawnCH47()
        {
            UnSubProSub();

            var position = new Vector3(ConVar.Server.worldsize, 100, ConVar.Server.worldsize) - new Vector3(50f, 0f, 50f);
            var entity = GameManager.server.CreateEntity(prefabCH47, position) as CH47HelicopterAIController;
            entity?.TriggeredEventSpawn();
            entity?.Spawn();
        }
        void SpawnCargo()
        {
            UnSubProSub();

            var x = TerrainMeta.Size.x;
            var vector3 = Vector3Ex.Range(-1f, 1f);
            vector3.y = 0.0f;
            vector3.Normalize();
            var worldPos = vector3 * (x * 1f);
            worldPos.y = TerrainMeta.WaterMap.GetHeight(worldPos);
            var entity = GameManager.server.CreateEntity(prefabShip, worldPos);
            entity?.Spawn();
        }
        void SpawnHeli()
        {
            UnSubProSub();

            var position = new Vector3(ConVar.Server.worldsize, 100, ConVar.Server.worldsize) - new Vector3(50f, 0f, 50f);
            var entity = GameManager.server.CreateEntity(prefabPatrol, position);
            entity?.Spawn();
        }
        void SpawnPlane()
        {
            UnSubProSub();

            var position = new Vector3(ConVar.Server.worldsize, 100, ConVar.Server.worldsize) - new Vector3(50f, 0f, 50f);
            var entity = GameManager.server.CreateEntity(prefabPlane, position);
            entity?.Spawn();
        }

        private void SpawnTank()
        {
            UnSubProSub();
            if (!BradleySpawner.singleton.spawned.isSpawned)
                BradleySpawner.singleton?.SpawnBradley();
        }
        #endregion

        #region ConvertedMetods
        enum Types
        {
            Gather,
            Loot,
            PickUP,
            Quarry,
            Excavator,
        }
        int Converted(Types RateType, string Shortname, float Amount, BasePlayer player = null)
        {
            float ConvertedAmount = Amount;
            if (IsBlackList(Shortname)) return Convert.ToInt32(ConvertedAmount);
            var PrivilegyRates = config.pluginSettings.RateSetting.PrivilegyRates;
            var Rates = IsTime() ? config.pluginSettings.RateSetting.DayRates : config.pluginSettings.RateSetting.NightRates;
            if (player != null)
            {
                var CustomRate = config.pluginSettings.RateSetting.CustomRatesPermissions;

                foreach (var Rate in CustomRate)
                    if (Rate.Shortname == Shortname)
                        if (IsPermission(player.UserIDString, Rate.Permissions))
                        {
                            ConvertedAmount = Amount * Rate.Rate;
                            return (int)ConvertedAmount;
                        }

                foreach (var RatesPerm in PrivilegyRates)
                    if (IsPermission(player.UserIDString, RatesPerm.Key))
                        Rates = RatesPerm.Value;
            }

            if (IsCustom(Shortname))
            {
                ConvertedAmount = GetCustomConverted(Shortname, Amount);
                return Convert.ToInt32(ConvertedAmount);
            }

            switch (RateType)
            {
                case Types.Gather:
                    {
                        ConvertedAmount = Amount * Rates.GatherRate;
                        break;
                    }
                case Types.Loot:
                    {
                        ConvertedAmount = Amount * Rates.LootRate;
                        break;
                    }
                case Types.PickUP:
                    {
                        ConvertedAmount = Amount * Rates.PickUpRate;
                        break;
                    }
                case Types.Quarry:
                    {
                        ConvertedAmount = Amount * Rates.QuarryRate;
                        break;
                    }
                case Types.Excavator:
                    {
                        ConvertedAmount = Amount * Rates.ExcavatorRate;
                        break;
                    }
            }
            return Convert.ToInt32(ConvertedAmount);
        }
        float GetRareCoal(BasePlayer player = null)
        {
            var Rates = IsTime() ? config.pluginSettings.RateSetting.DayRates : config.pluginSettings.RateSetting.NightRates;
            var PrivilegyRates = config.pluginSettings.RateSetting.PrivilegyRates;

            if (player != null)
                foreach (var RatesPerm in PrivilegyRates)
                    if (IsPermission(player.UserIDString, RatesPerm.Key))
                        Rates = RatesPerm.Value;

            float Rare = Rates.CoalRare;
            float RareResult = (100 - Rare) / 100;
            return RareResult;
        }
        float GetCustomConverted(string Shortname, float Amount)
        { 
            float CustomRates = config.pluginSettings.RateSetting.CustomRatesAll[Shortname];
            float ConvertedCustom = Amount * CustomRates;
            return ConvertedCustom; 
        }
        #endregion

        #region BoolMetods
        bool IsCustom(string Shortname)
        {
            var CustomRates = config.pluginSettings.RateSetting.CustomRatesAll;
            if (CustomRates.ContainsKey(Shortname))
                return true;
            else return false;
        }
        bool IsBlackList(string Shortname)
        {
            var BlackList = config.pluginSettings.RateSetting.BlackList;
            if (BlackList.Contains(Shortname))
                return true;
            else return false;
        }
        bool IsTime()
        {
            var Settings = config.pluginSettings.OtherSetting;
            float TimeServer = TOD_Sky.Instance.Cycle.Hour;
            return TimeServer < Settings.NightStart && Settings.DayStart <= TimeServer;
        }
        bool IsPermission(string userID,string Permission)
        {
            if (permission.UserHasPermission(userID, Permission))
                return true;
            else return false;
        }
        #endregion

        #endregion

        #region Hooks

        #region Player Gather Hooks
        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity as BasePlayer;
            if (player == null) return null;
            if (item == null) return null;
            if (entity == null) return null;

            int Rate = Converted(Types.Gather, item.info.shortname, item.amount, player);
            item.amount = Rate;
            return null;
        }
        void OnDispenserBonus(ResourceDispenser disp, BasePlayer player, Item item)
        {
            if (player == null) return;
            if (item == null) return;
            int Rate = Converted(Types.Gather, item.info.shortname, item.amount, player);
            item.amount = Rate;
        }
        #endregion

        #region Player PickUP Hooks
        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (player == null) return;
            if (item == null) return;
            item.amount = Converted(Types.PickUP, item.info.shortname, item.amount, player);
        }
        void OnContainerDropItems(ItemContainer container)
        {
            if (container == null) return;
            var Container = container.entityOwner as LootContainer;
            if (Container == null) return;
            uint NetID = Container.net.ID;
            if (LootersListCrateID.Contains(NetID)) return;

            BasePlayer player = Container.lastAttacker as BasePlayer;
            if (player == null) return;

            foreach (var item in container.itemList)
                item.amount = Converted(Types.Loot, item.info.shortname, item.amount, player);
        }
        #endregion
                
        #region Player Loot Hooks
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity == null) return;
            var container = entity as LootContainer;
            if (container == null) return;
            uint NetID = entity.net.ID;
            if (LootersListCrateID.Contains(NetID)) return;

            foreach (var item in container.inventory.itemList)
                item.amount = Converted(Types.Loot, item.info.shortname, item.amount, player);
            LootersListCrateID.Add(NetID);
        }
        void OnEntityKill(BaseNetworkable entity)
        {
            uint NetID = entity.net.ID;
            if (LootersListCrateID.Contains(NetID))
                LootersListCrateID.Remove(NetID);

        }
        #endregion

        #region Quarry Gather Hooks
        void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            if (item == null) return;
            if (quarry == null) return;
            BasePlayer player = quarry.OwnerID != 0 ? BasePlayer.FindByID(quarry.OwnerID) : null;
            item.amount = Converted(Types.Quarry, item.info.shortname, item.amount, player);
        }
        #endregion

        #region Exacavator Gather Hooks
        private object OnExcavatorGather(ExcavatorArm arm, Item item)
        {
            if (arm == null) return null;
            if (item == null) return null;
            item.amount = Converted(Types.Excavator, item.info.shortname, item.amount);
            return null;
        }
        #endregion

        #region Coal Hooks
        void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            if (oven == null) return;
            burnable.byproductChance = GetRareCoal(BasePlayer.FindByID(oven.OwnerID));
            if (burnable.byproductChance == 0)
                burnable.byproductChance = -1;
        }
        #endregion

        #region Server Hooks
        TOD_Time timeComponent = null;
        private void GetTimeComponent()
        {
            timeComponent = TOD_Sky.Instance.Components.Time;
            if (timeComponent == null) return;
            timeComponent.OnHour += OnHour;
        }
        private void OnHour()
        {       
            float Time = IsTime() ? config.pluginSettings.OtherSetting.DayTime : config.pluginSettings.OtherSetting.NightTime;
            timeComponent.DayLengthInMinutes = Time;
        }

        private void OnServerInitialized()
        {
            StartEvent();
            foreach (var RateCustom in config.pluginSettings.RateSetting.PrivilegyRates)
                Register(RateCustom.Key);

            foreach(var RateItemCustom in config.pluginSettings.RateSetting.CustomRatesPermissions)
                Register(RateItemCustom.Permissions);

            if(config.pluginSettings.OtherSetting.UseTime)
                timer.Once(5, GetTimeComponent);
            
            if(config.pluginSettings.RateSetting.UseSpeedBurnable)
            foreach (var oven in BaseNetworkable.serverEntities.OfType<BaseOven>())
                OvenController.GetOrAdd(oven).TryRestart();

            if (!config.pluginSettings.RateSetting.UseSpeedBurnable)
                Unsubscribe("OnOvenToggle");

        }

        #endregion

        #region Burnable
        private object OnOvenToggle(BaseOven oven, BasePlayer player)
        {
            return OvenController.GetOrAdd(oven).Switch(player);
        }
        private class OvenController : FacepunchBehaviour
        {
            private static readonly Dictionary<BaseOven, OvenController> Controllers = new Dictionary<BaseOven, OvenController>();
            private BaseOven _oven;
            private float _speed;
            private string _ownerId;

            private bool IsFurnace => (int)_oven.temperature >= 2;

            private void Awake()
            {
                _oven = (BaseOven)gameObject.ToBaseEntity();
                _ownerId = _oven.OwnerID.ToString();
            }

            public object Switch(BasePlayer player)
            {
                if (!IsFurnace || _oven.needsBuildingPrivilegeToUse && !player.CanBuild())
                    return null;

                if (_oven.IsOn())
                    StopCooking();
                else
                {
                    _ownerId = _oven.OwnerID != 0 ? _oven.OwnerID.ToString() : player.UserIDString;
                    StartCooking();
                }
                return false;
            }

            public void TryRestart()
            {
                if (!_oven.IsOn())
                    return;
                _oven.CancelInvoke(_oven.Cook);
                StopCooking();
                StartCooking();
            }
            private void Kill()
            {
                if (_oven.IsOn())
                {
                    StopCooking();
                    _oven.StartCooking();
                }
                Destroy(this);
            }

            #region Static methods⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠⁠

            public static OvenController GetOrAdd(BaseOven oven)
            {
                OvenController controller;
                if (Controllers.TryGetValue(oven, out controller))
                    return controller;
                controller = oven.gameObject.AddComponent<OvenController>();
                Controllers[oven] = controller;
                return controller;
            }

            public static void TryRestartAll()
            {
                foreach (var pair in Controllers)
                {
                    pair.Value.TryRestart();
                }
            }
            public static void KillAll()
            {
                foreach (var pair in Controllers)
                {
                    pair.Value.Kill();
                }
                Controllers.Clear();
            }

            #endregion

            private void StartCooking()
            {
                if (_oven.FindBurnable() == null)
                    return;
                _speed = 0.5f * config.pluginSettings.RateSetting.SpeedBurnable;

                _oven.inventory.temperature = _oven.cookingTemperature;
                _oven.UpdateAttachmentTemperature();
                InvokeRepeating(Cook, 0.5f, 0.5f);
                _oven.SetFlag(BaseEntity.Flags.On, true);
            }

            private void StopCooking()
            {
                _oven.UpdateAttachmentTemperature();
                if (_oven.inventory != null)
                {
                    _oven.inventory.temperature = 15f;
                    foreach (Item item in _oven.inventory.itemList)
                    {
                        if (!item.HasFlag(global::Item.Flag.OnFire))
                            continue;
                        item.SetFlag(global::Item.Flag.OnFire, false);
                        item.MarkDirty();
                    }
                }
                CancelInvoke(Cook);
                _oven.SetFlag(BaseEntity.Flags.On, false);
            }

            private void Cook()
            {
                if (!_oven.IsOn())
                {
                    //BaseOven.OvenFull workaround;
                    CancelInvoke(Cook);
                    return;
                }

                Item item = _oven.FindBurnable();
                if (item == null)
                {
                    StopCooking();
                    return;
                }

                _oven.inventory.OnCycle(_speed);
                BaseEntity slot = _oven.GetSlot(BaseEntity.Slot.FireMod);
                if (slot)
                    slot.SendMessage("Cook", _speed, SendMessageOptions.DontRequireReceiver);

                if (!item.HasFlag(global::Item.Flag.OnFire))
                {
                    item.SetFlag(global::Item.Flag.OnFire, true);
                    item.MarkDirty();
                }

                var burnable = item.info.GetComponent<ItemModBurnable>();
                var requiredFuel = _speed * (_oven.cookingTemperature / 200f);
                if (item.fuel >= requiredFuel)
                {
                    item.fuel -= requiredFuel;
                    if (item.fuel <= 0f)
                        _oven.ConsumeFuel(item, burnable);
                    return;
                }
                var itemsRequired = Mathf.CeilToInt(requiredFuel / burnable.fuelAmount);
                for (var i = 0; i < itemsRequired; i++)
                {
                    requiredFuel -= item.fuel;
                    _oven.ConsumeFuel(item, burnable);
                    if (!item.IsValid())
                        return;
                }

                item.fuel -= requiredFuel;
            }
        }

        #endregion

        #region Event Hooks
        private void Unload()
        {
            OvenController.KillAll();
        }
        private void OnEntitySpawned(SupplySignal entity) => UnSubProSub(10);
        private void OnEntitySpawned(CargoPlane entity)
        {
            var EvenTimer = config.pluginSettings.OtherSetting.EventSetting;
            if (EvenTimer.UseEventAirdrops)
                if (entity.OwnerID == 0)
                    entity.Kill();
        }
        private void OnEntitySpawned(CargoShip entity)
        {
            var EvenTimer = config.pluginSettings.OtherSetting.EventSetting;
            if (EvenTimer.UseEventCargoShip)
                if (entity.OwnerID == 0)
                entity.Kill();
        }
        private void OnEntitySpawned(BradleyAPC entity)
        {
            var EvenTimer = config.pluginSettings.OtherSetting.EventSetting;
            if (EvenTimer.UseEventBradley)
                if (entity.OwnerID == 0)
                entity.Kill();
        }

        private void OnEntitySpawned(BaseHelicopter entity)
        {
            var EvenTimer = config.pluginSettings.OtherSetting.EventSetting;
            if (EvenTimer.UseEventHelicopter)
                if (entity.OwnerID == 0)
                entity.Kill();
        }
        private void OnEntitySpawned(CH47Helicopter entity)
        {
            timer.Once(5f, () =>
             {
                 var EvenTimer = config.pluginSettings.OtherSetting.EventSetting;
                 if (EvenTimer.UseEventChinoock)
                     if (entity.OwnerID == 0 && entity.mountPoints.Where(x => x.mountable.GetMounted() != null && x.mountable.GetMounted().ShortPrefabName.Contains("heavyscientist")).Count() <= 0)
                     timer.Once(1f, () => { entity.Kill(); });
             });
        }
        #endregion

        #endregion

        #region API
        int API_CONVERT(Types RateType, string Shortname, float Amount, BasePlayer player = null) => Converted(RateType, Shortname, Amount, player);
        int API_CONVERT_GATHER(string Shortname, float Amount, BasePlayer player = null) => Converted(Types.Gather, Shortname, Amount, player);
        #endregion
    }
}