using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("IQRates", "Mercury", "0.1.1")]
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
        /// Обновление 0.0.9
        /// - Исправлена скорость плавки в печах
        /// - Добавлена возможность настроить скорость плавки по пермишенсам
        /// - Исправлен NRE в OnLootEntity
        /// - Исправлен OnConsumeFuel 
        /// - Добавил в настройку "Рейты по привилегиям" расширенную настройку - возможность настраивать рейты для привилегий на день и ночь
        /// - Изменил полностью систему "Кастомной настройки определенных предметов" и "Кастомной настройки определенных предметов по пермишенам"
        /// - Добавил "Кастомную настройку определенных предметов" на день и ночь с поддержкой пермишенсов, все настраивается детально до мелочей
        /// - Исправлен подъем ягод с плантаций
        /// - Полностью изменен метод спавна кастомных ивентов
        /// - Добавлена возможность полностью отключить любой ивент на сервере
        /// - Добавлена возможность спавнить ивенты с установкой случайного времени (каждый спавн будет случайный от N до N времени, устанавливается в конфигурации)
        /// Обновление 0.1.0
        /// - FIX OnDispenserGather
        /// Обновление 0.1.1
        /// - Убрал спам (забыл дебаг стереть)

        #region Vars
        public List<UInt64> LootersListCrateID = new List<UInt64>();
        public static IQRates _;
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
                    public Dictionary<String, DayAnNightRate> PrivilegyRates = new Dictionary<String, DayAnNightRate>();

                    [JsonProperty("Настройка кастомных рейтов(предметов) по пермишенсу - настройка (По убыванию)")]
                    public PermissionsRate CustomRatesPermissions = new PermissionsRate();

                    [JsonProperty("Черный лист предметов,на которые катигорично не будут действовать рейтинг")]
                    public List<String> BlackList = new List<String>();
                    [JsonProperty("Включить скорость плавки в печах(true - да/false - нет)")]
                    public Boolean UseSpeedBurnable;
                    [JsonProperty("Скорость плавки печей(Если включен список - это значение будет стандартное для всех у кого нет прав)")]
                    public Single SpeedBurnable;
                    [JsonProperty("Включить список скорости плавки в печах(true - да/false - нет)")]
                    public Boolean UseSpeedBurnableList;
                    [JsonProperty("Настройка скорости плавки в печах по привилегиям")]
                    public List<SpeedBurnablePreset> SpeedBurableList = new List<SpeedBurnablePreset>();
                    internal class DayAnNightRate
                    {
                        [JsonProperty("Настройка рейтинга днем")]
                        public AllRates DayRates = new AllRates();
                        [JsonProperty("Настройка рейтинга ночью")]
                        public AllRates NightRates = new AllRates();
                    }
                    internal class SpeedBurnablePreset
                    {
                        [JsonProperty("Права")]
                        public String Permissions;
                        [JsonProperty("Скорость плавки печей")]
                        public Single SpeedBurnable;
                    }
                    internal class PermissionsRate
                    {
                        [JsonProperty("Настройка рейтинга днем")]
                        public Dictionary<String, List<PermissionsRateDetalis>> DayRates = new Dictionary<String, List<PermissionsRateDetalis>>();
                        [JsonProperty("Настройка рейтинга ночью")]
                        public Dictionary<String, List<PermissionsRateDetalis>> NightRates = new Dictionary<String, List<PermissionsRateDetalis>>();
                        public class PermissionsRateDetalis
                        {
                            [JsonProperty("Shortname")]
                            public String Shortname;
                            [JsonProperty("Рейтинг")]
                            public Single Rate;
                        }
                    }
                    internal class AllRates
                    {
                        [JsonProperty("Рейтинг добываемых ресурсов")]
                        public Single GatherRate;
                        [JsonProperty("Рейтинг найденных предметов")]
                        public Single LootRate;
                        [JsonProperty("Рейтинг поднимаемых предметов")]
                        public Single PickUpRate;
                        [JsonProperty("Рейтинг карьеров")]
                        public Single QuarryRate;
                        [JsonProperty("Рейтинг экскаватора")]
                        public Single ExcavatorRate;
                        [JsonProperty("Шанс выпадения угля")]
                        public Single CoalRare;
                    }
                }
                internal class OtherSettings
                {
                    [JsonProperty("Настройки ивентов на сервере")]
                    public EventSettings EventSetting = new EventSettings();

                    [JsonProperty("Использовать ускорение времени")]
                    public Boolean UseTime;
                    [JsonProperty("Использовать заморозку времени(время будет такое, какое вы установите в пунке <Замороженное время на сервере>)")]
                    public Boolean UseFreezeTime;
                    [JsonProperty("Замороженное время на сервере (Установите время, которое не будет изменяться и будет вечно на сервере, должен быть true на <Использовать заморозку времени>")]
                    public Int32 FreezeTime;
                    [JsonProperty("Укажите во сколько будет начинаться день")]
                    public Int32 DayStart;
                    [JsonProperty("Укажите во сколько будет начинаться ночь")]
                    public Int32 NightStart;
                    [JsonProperty("Укажите сколько будет длится день в минутах")]
                    public Int32 DayTime;
                    [JsonProperty("Укажите сколько будет длится ночь в минутах")]
                    public Int32 NightTime;

                    internal class EventSettings
                    {
                        [JsonProperty("Кастомные настройки спавна вертолета")]
                        public Setting HelicopterSetting = new Setting();
                        [JsonProperty("Кастомные настройки спавна танка")]
                        public Setting BreadlaySetting = new Setting();
                        [JsonProperty("Кастомные настройки спавна корабля")]
                        public Setting CargoShipSetting = new Setting();
                        [JsonProperty("Кастомные настройки спавна аирдропа")]
                        public Setting CargoPlaneSetting = new Setting();
                        [JsonProperty("Кастомные настройки спавна чинука")]
                        public Setting ChinoockSetting = new Setting();
                        internal class Setting
                        {
                            [JsonProperty("Полностью отключить спавн ивента на сервере(true - да/false - нет)")]
                            public Boolean FullOff;
                            [JsonProperty("Включить кастомный спавн ивент(true - да/false - нет)")]
                            public Boolean UseEventCustom;
                            [JsonProperty("Статическое время спавна ивента")]
                            public Int32 EventSpawnTime;
                            [JsonProperty("Настройки случайного времени спавна")]
                            public RandomingTime RandomTimeSpawn = new RandomingTime();
                            internal class RandomingTime
                            {
                                [JsonProperty("Использовать случайное время спавно ивента(статическое время не будет учитываться)(true - да/false - нет)")]
                                public Boolean UseRandomTime;
                                [JsonProperty("Минимальное значение спавна ивента")]
                                public Int32 MinEventSpawnTime;
                                [JsonProperty("Максимальное значении спавна ивента")]
                                public Int32 MaxEventSpawnTime;
                            }
                        }
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
                            UseSpeedBurnableList = true,
                            SpeedBurableList = new List<PluginSettings.Rates.SpeedBurnablePreset>
                            {
                                new PluginSettings.Rates.SpeedBurnablePreset
                                {
                                    Permissions = "iqrates.vip",
                                    SpeedBurnable = 5.0f
                                },
                                new PluginSettings.Rates.SpeedBurnablePreset
                                {
                                    Permissions = "iqrates.speedrun",
                                    SpeedBurnable = 55.0f
                                },
                                new PluginSettings.Rates.SpeedBurnablePreset
                                {
                                    Permissions = "iqrates.fuck",
                                    SpeedBurnable = 200f
                                },
                            },
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
                            CustomRatesPermissions = new PluginSettings.Rates.PermissionsRate
                            {
                                DayRates = new Dictionary<String, List<PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis>>
                                {
                                    ["iqrates.gg"] = new List<PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis>
                                    {
                                        new PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis
                                        {
                                            Rate = 200.0f,
                                            Shortname = "wood",
                                        },
                                        new PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis
                                        {
                                              Rate = 200.0f,
                                              Shortname = "stones",
                                        }
                                    }
                                },
                                NightRates = new Dictionary<string, List<PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis>>
                                {
                                    ["iqrates.gg"] = new List<PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis>
                                    {
                                        new PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis
                                        {
                                            Rate = 400.0f,
                                            Shortname = "wood",
                                        },
                                        new PluginSettings.Rates.PermissionsRate.PermissionsRateDetalis
                                        {
                                              Rate = 400.0f,
                                              Shortname = "stones",
                                        }
                                    }
                                },
                            },
                            PrivilegyRates = new Dictionary<string, PluginSettings.Rates.DayAnNightRate>
                            {
                                ["iqrates.vip"] = new PluginSettings.Rates.DayAnNightRate
                                {
                                    DayRates =
                                    {
                                        GatherRate = 3.0f,
                                        LootRate = 3.0f,
                                        PickUpRate = 3.0f,
                                        QuarryRate = 3.0f,
                                        ExcavatorRate = 3.0f,
                                        CoalRare = 15,
                                    },
                                    NightRates = new PluginSettings.Rates.AllRates
                                    {
                                        GatherRate = 13.0f,
                                        LootRate = 13.0f,
                                        PickUpRate = 13.0f,
                                        QuarryRate = 13.0f,
                                        ExcavatorRate = 13.0f,
                                        CoalRare = 25,
                                    }
                                },
                                ["iqrates.premium"] = new PluginSettings.Rates.DayAnNightRate
                                {
                                    DayRates =
                                    {
                                        GatherRate = 3.5f,
                                        LootRate = 3.5f,
                                        PickUpRate = 3.5f,
                                        QuarryRate = 3.5f,
                                        ExcavatorRate = 3.5f,
                                        CoalRare = 20,
                                    },
                                    NightRates = new PluginSettings.Rates.AllRates
                                    {
                                        GatherRate = 13.5f,
                                        LootRate = 13.5f,
                                        PickUpRate = 13.5f,
                                        QuarryRate = 13.5f,
                                        ExcavatorRate = 13.5f,
                                        CoalRare = 20,
                                    }
                                },
                            },
                            BlackList = new List<String>
                            {
                                "sulfur.ore",
                            },
                        },
                        OtherSetting = new PluginSettings.OtherSettings
                        {
                            UseTime = false,
                            FreezeTime = 12,
                            UseFreezeTime = true,
                            DayStart = 10,
                            NightStart = 22,
                            DayTime = 5,
                            NightTime = 1,
                            EventSetting = new PluginSettings.OtherSettings.EventSettings
                            {
                                BreadlaySetting = new PluginSettings.OtherSettings.EventSettings.Setting
                                {
                                    FullOff = false,
                                    UseEventCustom = true,
                                    EventSpawnTime = 3000,
                                    RandomTimeSpawn = new PluginSettings.OtherSettings.EventSettings.Setting.RandomingTime
                                    {
                                        UseRandomTime = false,
                                        MaxEventSpawnTime = 3000,
                                        MinEventSpawnTime = 1000,
                                    },
                                },
                                CargoPlaneSetting = new PluginSettings.OtherSettings.EventSettings.Setting
                                {
                                    FullOff = false,
                                    UseEventCustom = true,
                                    EventSpawnTime = 5000,
                                    RandomTimeSpawn = new PluginSettings.OtherSettings.EventSettings.Setting.RandomingTime
                                    {
                                        UseRandomTime = false,
                                        MaxEventSpawnTime = 3000,
                                        MinEventSpawnTime = 1000,
                                    },
                                },
                                CargoShipSetting = new PluginSettings.OtherSettings.EventSettings.Setting
                                {
                                    FullOff = false,
                                    UseEventCustom = true,
                                    EventSpawnTime = 0,
                                    RandomTimeSpawn = new PluginSettings.OtherSettings.EventSettings.Setting.RandomingTime
                                    {
                                        UseRandomTime = true,
                                        MaxEventSpawnTime = 3000,
                                        MinEventSpawnTime = 8000,
                                    },
                                },
                                ChinoockSetting = new PluginSettings.OtherSettings.EventSettings.Setting
                                {
                                    FullOff = true,
                                    UseEventCustom = false,
                                    EventSpawnTime = 3000,
                                    RandomTimeSpawn = new PluginSettings.OtherSettings.EventSettings.Setting.RandomingTime
                                    {
                                        UseRandomTime = false,
                                        MaxEventSpawnTime = 3000,
                                        MinEventSpawnTime = 1000,
                                    },
                                },
                                HelicopterSetting = new PluginSettings.OtherSettings.EventSettings.Setting
                                {
                                    FullOff = true,
                                    UseEventCustom = false,
                                    EventSpawnTime = 3000,
                                    RandomTimeSpawn = new PluginSettings.OtherSettings.EventSettings.Setting.RandomingTime
                                    {
                                        UseRandomTime = false,
                                        MaxEventSpawnTime = 3000,
                                        MinEventSpawnTime = 1000,
                                    },
                                },
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
                PrintWarning("Ошибка #2657" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
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

        private Int32 GetRandomTime(Int32 Min, Int32 Max) => UnityEngine.Random.Range(Min, Max);
        void StartEvent()
        {
            var EventSettings = config.pluginSettings.OtherSetting.EventSetting;
            StartCargoShip(EventSettings);
            StartCargoPlane(EventSettings);
            StartBreadley(EventSettings);
            StartChinoock(EventSettings);
            StartHelicopter(EventSettings);
        }
        private void StartCargoShip(Configuration.PluginSettings.OtherSettings.EventSettings EventSettings)
        {
            if (!EventSettings.CargoShipSetting.FullOff && EventSettings.CargoShipSetting.UseEventCustom)
            {
                Int32 TimeSpawn = EventSettings.CargoShipSetting.RandomTimeSpawn.UseRandomTime ? GetRandomTime(EventSettings.CargoShipSetting.RandomTimeSpawn.MinEventSpawnTime, EventSettings.CargoShipSetting.RandomTimeSpawn.MaxEventSpawnTime) : EventSettings.CargoShipSetting.EventSpawnTime;
                timer.Once(TimeSpawn, () =>
                {
                    StartCargoShip(EventSettings);
                    SpawnCargo();
                });
            }
        }
        private void StartCargoPlane(Configuration.PluginSettings.OtherSettings.EventSettings EventSettings)
        {
            if (!EventSettings.CargoPlaneSetting.FullOff && EventSettings.CargoPlaneSetting.UseEventCustom)
            {
                Int32 TimeSpawn = EventSettings.CargoPlaneSetting.RandomTimeSpawn.UseRandomTime ? GetRandomTime(EventSettings.CargoPlaneSetting.RandomTimeSpawn.MinEventSpawnTime, EventSettings.CargoPlaneSetting.RandomTimeSpawn.MaxEventSpawnTime) : EventSettings.CargoPlaneSetting.EventSpawnTime;
                timer.Once(TimeSpawn, () =>
                {
                    StartCargoPlane(EventSettings);
                    SpawnPlane();
                });
            }
        }
        private void StartBreadley(Configuration.PluginSettings.OtherSettings.EventSettings EventSettings)
        {
            if (!EventSettings.BreadlaySetting.FullOff && EventSettings.BreadlaySetting.UseEventCustom)
            {
                Int32 TimeSpawn = EventSettings.BreadlaySetting.RandomTimeSpawn.UseRandomTime ? GetRandomTime(EventSettings.BreadlaySetting.RandomTimeSpawn.MinEventSpawnTime, EventSettings.BreadlaySetting.RandomTimeSpawn.MaxEventSpawnTime) : EventSettings.BreadlaySetting.EventSpawnTime;
                timer.Once(TimeSpawn, () =>
                {
                    StartBreadley(EventSettings);
                    SpawnTank();
                });
            }
        }
        private void StartChinoock(Configuration.PluginSettings.OtherSettings.EventSettings EventSettings)
        {
            if (!EventSettings.ChinoockSetting.FullOff && EventSettings.ChinoockSetting.UseEventCustom)
            {
                Int32 TimeSpawn = EventSettings.ChinoockSetting.RandomTimeSpawn.UseRandomTime ? GetRandomTime(EventSettings.ChinoockSetting.RandomTimeSpawn.MinEventSpawnTime, EventSettings.ChinoockSetting.RandomTimeSpawn.MaxEventSpawnTime) : EventSettings.ChinoockSetting.EventSpawnTime;
                timer.Once(TimeSpawn, () =>
                {
                    StartChinoock(EventSettings);
                    SpawnCH47();
                });
            }
        }
        private void StartHelicopter(Configuration.PluginSettings.OtherSettings.EventSettings EventSettings)
        {
            if (!EventSettings.HelicopterSetting.FullOff && EventSettings.HelicopterSetting.UseEventCustom)
            {
                Int32 TimeSpawn = EventSettings.HelicopterSetting.RandomTimeSpawn.UseRandomTime ? GetRandomTime(EventSettings.HelicopterSetting.RandomTimeSpawn.MinEventSpawnTime, EventSettings.HelicopterSetting.RandomTimeSpawn.MaxEventSpawnTime) : EventSettings.HelicopterSetting.EventSpawnTime;
                timer.Once(TimeSpawn, () => 
                {
                    StartHelicopter(EventSettings);
                    SpawnHeli();
                });
            }
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
            Boolean IsTimes = IsTime();
            var Rates = IsTimes ? config.pluginSettings.RateSetting.DayRates : config.pluginSettings.RateSetting.NightRates;
            if (player != null)
            {
                var CustomRate = IsTimes ? config.pluginSettings.RateSetting.CustomRatesPermissions.DayRates : config.pluginSettings.RateSetting.CustomRatesPermissions.NightRates;

                var Rate = CustomRate.FirstOrDefault(x => IsPermission(player.UserIDString, x.Key)); //dbg
                if (Rate.Value != null)
                    foreach (var RateValue in Rate.Value.Where(x => x.Shortname == Shortname))
                    {
                        ConvertedAmount = Amount * RateValue.Rate;
                        return (int)ConvertedAmount;
                    }

                foreach (var RatesSetting in PrivilegyRates)
                    if (IsPermission(player.UserIDString, RatesSetting.Key))
                        Rates = IsTimes ? RatesSetting.Value.DayRates : RatesSetting.Value.NightRates;
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
            Boolean IsTimes = IsTime();

            var Rates = IsTimes ? config.pluginSettings.RateSetting.DayRates : config.pluginSettings.RateSetting.NightRates;
            var PrivilegyRates = config.pluginSettings.RateSetting.PrivilegyRates;

            if (player != null)
            {
                foreach (var RatesSetting in PrivilegyRates)
                    if (IsPermission(player.UserIDString, RatesSetting.Key))
                        Rates = IsTimes ? RatesSetting.Value.DayRates : RatesSetting.Value.NightRates;
            }

            float Rare = Rates.CoalRare;
            float RareResult = (100 - Rare) / 100;
            return RareResult;
        }

        #endregion

        #region BoolMetods

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
            if (item == null || player == null) return null;

            int Rate = Converted(Types.Gather, item.info.shortname, item.amount, player);
            item.amount = Rate;
            return null;
        }

        void OnDispenserBonus(ResourceDispenser disp, BasePlayer player, Item item)
        {
            if (item == null || player == null) return;

            int Rate = Converted(Types.Gather, item.info.shortname, item.amount, player);
            item.amount = Rate;
        }
        #endregion

        #region Player PickUP Hooks

        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            if (player == null || item == null) return;
            item.amount = Converted(Types.PickUP, item.info.shortname, item.amount, player);
        }

        void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player)
        {
            if (item == null || player == null) return;
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
            LootContainer container = entity as LootContainer;
            if (container == null || entity.net == null) return;
            UInt64 NetID = entity.net.ID;
            if (LootersListCrateID.Contains(NetID)) return;

            foreach (Item item in container.inventory.itemList)
                item.amount = Converted(Types.Loot, item.info.shortname, item.amount, player);
            LootersListCrateID.Add(NetID);
        }
        void OnEntityKill(BaseNetworkable entity)
        {
            UInt64 NetID = entity.net.ID;
            if (LootersListCrateID.Contains(NetID))
                LootersListCrateID.Remove(NetID);

        }
        #endregion

        #region Quarry Gather Hooks
        void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            if (item == null || quarry == null) return;
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

        void OnFuelConsume(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            if (oven == null) return;
            burnable.byproductChance = GetRareCoal(BasePlayer.FindByID(oven.OwnerID));
            if (burnable.byproductChance == 0)
                burnable.byproductChance = -1;
        }
        #endregion

        #region Server Hooks
        TOD_Time timeComponent = null;
        Boolean activatedDay;

        private void GetTimeComponent()
        {
            timeComponent = TOD_Sky.Instance.Components.Time;
            if (timeComponent == null) return;
            SetTimeComponent();
            StartupFreeze();
        }
        void SetTimeComponent()
        {
            if (!config.pluginSettings.OtherSetting.UseTime) return;

            timeComponent.ProgressTime = true;
            timeComponent.UseTimeCurve = false;
            timeComponent.OnSunrise += OnSunrise;
            timeComponent.OnSunset += OnSunset;
            timeComponent.OnHour += OnHour;

            if (TOD_Sky.Instance.Cycle.Hour > TOD_Sky.Instance.SunriseTime && TOD_Sky.Instance.Cycle.Hour < TOD_Sky.Instance.SunsetTime)
                OnSunrise();
            else
                OnSunset();
        }

        void OnHour()
        {
            if (TOD_Sky.Instance.Cycle.Hour > TOD_Sky.Instance.SunriseTime && TOD_Sky.Instance.Cycle.Hour < TOD_Sky.Instance.SunsetTime && !activatedDay)
            {
                OnSunrise();
                return;
            }
            if ((TOD_Sky.Instance.Cycle.Hour > TOD_Sky.Instance.SunsetTime || TOD_Sky.Instance.Cycle.Hour < TOD_Sky.Instance.SunriseTime) && activatedDay)
            {
                OnSunset();
                return;
            }
        }

        void OnSunrise()
        {
            timeComponent.DayLengthInMinutes = config.pluginSettings.OtherSetting.DayTime * (24.0f / (TOD_Sky.Instance.SunsetTime - TOD_Sky.Instance.SunriseTime));
            activatedDay = true;
        }

        void OnSunset()
        {
            timeComponent.DayLengthInMinutes = config.pluginSettings.OtherSetting.NightTime * (24.0f / (24.0f - (TOD_Sky.Instance.SunsetTime - TOD_Sky.Instance.SunriseTime)));
            activatedDay = false;
        }
        void StartupFreeze()
        {
            if (!config.pluginSettings.OtherSetting.UseFreezeTime) return;
            timeComponent.ProgressTime = false;
            ConVar.Env.time = config.pluginSettings.OtherSetting.FreezeTime;
        }

        private void OnServerInitialized()
        {
            _ = this;
            StartEvent();
            foreach (var RateCustom in config.pluginSettings.RateSetting.PrivilegyRates)
                Register(RateCustom.Key);

            if (config.pluginSettings.RateSetting.UseSpeedBurnableList)
                foreach (var BurnableList in config.pluginSettings.RateSetting.SpeedBurableList)
                    Register(BurnableList.Permissions);

            List<String> PrivilegyCustomRatePermissions = config.pluginSettings.RateSetting.CustomRatesPermissions.NightRates.Keys.Union(config.pluginSettings.RateSetting.CustomRatesPermissions.DayRates.Keys).ToList();
            foreach (var RateItemCustom in PrivilegyCustomRatePermissions)
                Register(RateItemCustom);

                timer.Once(5, GetTimeComponent);
            
            if(config.pluginSettings.RateSetting.UseSpeedBurnable)
            foreach (var oven in BaseNetworkable.serverEntities.OfType<BaseOven>())
                OvenController.GetOrAdd(oven).TryRestart();

            if (!config.pluginSettings.RateSetting.UseSpeedBurnable)
                Unsubscribe("OnOvenToggle");

        }

        #endregion

        #region Burnable
        public Single GetMultiplaceBurnableSpeed(String ownerid)
        {
            Single Multiplace = config.pluginSettings.RateSetting.SpeedBurnable;
            if (config.pluginSettings.RateSetting.UseSpeedBurnableList)
            {
                var SpeedInList = config.pluginSettings.RateSetting.SpeedBurableList.OrderByDescending(z => z.SpeedBurnable).FirstOrDefault(x => permission.UserHasPermission(ownerid, x.Permissions));
                if (SpeedInList != null)
                    Multiplace = SpeedInList.SpeedBurnable;
            }
            return Multiplace;
        }
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
                Single Multiplace = _.GetMultiplaceBurnableSpeed(_ownerId);
                _speed = 0.5f * Multiplace;

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
            if (timeComponent == null) return;
            timeComponent.OnSunrise -= OnSunrise;
            timeComponent.OnSunset -= OnSunset;
            timeComponent.OnHour -= OnHour;
        }
        private void OnEntitySpawned(SupplySignal entity) => UnSubProSub(10);
        private void OnEntitySpawned(CargoPlane entity)
        {
            var EvenTimer = config.pluginSettings.OtherSetting.EventSetting.CargoPlaneSetting;
            if (EvenTimer.FullOff)
                entity.Kill();
            else
            {
                if (EvenTimer.UseEventCustom)
                    if (entity.OwnerID == 0)
                        entity.Kill();
            }
        }
        private void OnEntitySpawned(CargoShip entity)
        {
            var EvenTimer = config.pluginSettings.OtherSetting.EventSetting.CargoShipSetting;
            if (EvenTimer.FullOff)
                entity.Kill();
            else
            {
                if (EvenTimer.UseEventCustom)
                    if (entity.OwnerID == 0)
                        entity.Kill();
            }
        }
        private void OnEntitySpawned(BradleyAPC entity)
        {
            var EvenTimer = config.pluginSettings.OtherSetting.EventSetting.BreadlaySetting;
            if (EvenTimer.FullOff)
                entity.Kill();
            else
            {
                if (EvenTimer.UseEventCustom)
                    if (entity.OwnerID == 0)
                        entity.Kill();
            }
        }

        private void OnEntitySpawned(BaseHelicopter entity)
        {
            var EvenTimer = config.pluginSettings.OtherSetting.EventSetting.HelicopterSetting;
            if (EvenTimer.FullOff)
                entity.Kill();
            else
            {
                if (EvenTimer.UseEventCustom)
                    if (entity.OwnerID == 0)
                        entity.Kill();
            }
        }
        private void OnEntitySpawned(CH47Helicopter entity)
        {
            timer.Once(5f, () =>
             {
                 var EvenTimer = config.pluginSettings.OtherSetting.EventSetting.HelicopterSetting;
                 if (EvenTimer.FullOff && entity.mountPoints.Where(x => x.mountable.GetMounted() != null && x.mountable.GetMounted().ShortPrefabName.Contains("heavyscientist")).Count() <= 0)
                     timer.Once(1f, () => { entity.Kill(); });
                 else
                 {
                     if (EvenTimer.UseEventCustom)
                         if (entity.OwnerID == 0 && entity.mountPoints.Where(x => x.mountable.GetMounted() != null && x.mountable.GetMounted().ShortPrefabName.Contains("heavyscientist")).Count() <= 0)
                             timer.Once(1f, () => { entity.Kill(); });

                 }
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