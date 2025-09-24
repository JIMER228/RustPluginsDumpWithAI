using Oxide.Core;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Diagnostics;

namespace Oxide.Plugins
{
    [Info("RatesController", "ADMIN | Night_Tiger", "2.1.62")]
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("Детальная настройка рейтов")]
    /*
     * v2.1.61
     *   Добавление отдельных рейтов для бензопилы, отбойного молотка, нефтекачки by ADMIN | Night_Tiger
	 * v2.1.62
	 * Исправлены рейты нефтекачки. Теперь при нахождении нефтяной скважины, не будут выпадать ресурсы, приводящие к добыче руды нефтекачкой by ADMIN
     */

    class RatesController : RustPlugin
    {
        private bool isDay;

        private string GatherDayString = "Рейт добываемых ресурсов днём";
        private string GatherNightString = "Рейт добываемых ресурсов ночью";
		private string ChainsawDayString = "Рейт бензопилы днём";
        private string ChainsawNightString = "Рейт бензопилы ночью";
		private string JackhammerDayString = "Рейт отбойного молотка днём";
        private string JackhammerNightString = "Рейт отбойного молотка ночью";
        private string PickUpDayString = "Рейт поднимаемых ресурсов днём";
        private string PickUpNightString = "Рейт поднимаемых ресурсов ночью";
        private string QuerryDayString = "Рейт добываемых ресурсов в карьере днём";
        private string QuerryNightString = "Рейт добываемых ресурсов в карьере ночью";
        private string PumpjackDayString = "Рейт добываемых ресурсов в нефтекачке днём";
        private string PumpjackNightString = "Рейт добываемых ресурсов в нефтекачке ночью";
        private string LootDayString = "Рейт лута днём(если включен)";
        private string LootNightString = "Рейт лута ночью(если включен)";
        private string SmeltDayString = "Скорость работы печей днём";
        private string SmeltNightString = "Скорость работы печей ночью";
        private ItemDefinition hqmo;

        private string DefaultRatesCfg = "Общие рейты ресурсов";
        private string CustomRatesCfg = "Изменение рейтов для игроков с привилегиями";
        private string DefaultPickupRatesCfg = "Стандартные рейты поднимаемых ресурсов";
        private string DefaultGatherRatesCfg = "Стандартные рейты добываемых ресурсов";
        private string DefaultSmeltRatesCfg = "Стандартное время переработки ресурсов (в секундах)";
        private string DefaultQuerryRatesCfg = "Стандартные рейты добываемых ресурсов в карьере";
		private string DefaultPumpjackRatesCfg = "Стандартный рейт добычи нефти в нефтекачке";
        private string PrefixCfg = "Префикс в чате";
        private string PrefixColorCfg = "Цвет префикса в чате";
        private string UseLootMultyplierCfg = "Использовать умножение лута(выключите для совместимости с контроллерами лута)";
        private string DayStartCfg = "Час начала дня(игровое время)";
        private string DayLenghtCfg = "Длина дня(в минутах)";
        private string NightStartCfg = "Час начала ночи(игровое время)";
        private string NightLenghtCfg = "Длина ночи(в минутах)";
        private string WarnChatCfg = "Выводить сообщения в чат о начале дня или ночи";
        private string CoalRateDayCfg = "Рейт угля при сжигании дерева днём";
        private string CoalRateNightCfg = "Рейт угля при сжигании дерева ночью";
        private string CoalChanceDayCfg = "Шанс производства угля днём";
        private string CoalChanceNightCfg = "Шанс производства угля ночью";
        private string BlacklistedLootCfg = "Список лута, на который не действуют множители";
        private string MoreHQMCfg = "Добавить металл высокого качества во все рудные жилы";

        private List<string> AvaliableMods;
        private Dictionary<string, float> SmeltBackup = new Dictionary<string, float>();
        //Откат обновления ящиков
        Dictionary<int, DateTime> CratesCD = new Dictionary<int, DateTime>();
        //Список рудных жил и их бонусов
        //Dictionary<BaseEntity, Dictionary<string, float>> DefaultFinishBonuses = new Dictionary<BaseEntity, Dictionary<string, float>>();

        private double CoalRate = 1f;
        private int CoalChance = 25;
        private double SmeltRate = 1f;
        private double GatherRate = 1f;
		private double ChainsawRate = 1f;
		private double JackhammerRate = 1f;
        private double PickupRate = 1f;
        private double QuerryRate = 1f;
		private double PumpjackRate = 1f;
        private double LootRate = 1f;

        #region config setup

        private string Prefix = "РЕЙТЫ РЕСУРСОВ НА ServerName";//Префикс плагина в чате
        private bool UseLootMultyplier = true; //Использовать множители лута
        private string PrefixColor = "#ff0000";//Цвет префикса
        private float DayStart = 6f;//Час, когда начинается день
        private float NightStart = 18f;//Час, когда начинается ночь
        private uint DayLenght = 30u;//Длинна дня
        private uint NightLenght = 30u;//Длинна ночи
        private bool WarnChat = true;//Выводить ли в чат оповещения о смене дня\ночи и рейтов.
        private bool MoreHQM = false; //Добавить ли во все рудные жилы мвк?

        //Стандартные рейты, для игроков без привелегий
        private Dictionary<string, double> DefaultRates = new Dictionary<string, double>();
        //Рейты для игроков с привелегиями
        private Dictionary<string, Dictionary<string, double>> CustomRates = new Dictionary<string, Dictionary<string, double>>();

        private double CoalRateDay = 1f;
        private double CoalRateNight = 1f;
        private int CoalChanceDay = 25;
        private int CoalChanceNight = 25;
        //private Dictionary<string, double> DefaultSmeltRates = new Dictionary<string, double>();
        private Dictionary<string, double> DefaultGatherRates = new Dictionary<string, double>();
        private Dictionary<string, double> DefaultPickupRates = new Dictionary<string, double>();
        private Dictionary<string, double> DefaultQuerryRates = new Dictionary<string, double>();
		private Dictionary<string, double> DefaultPumpjackRates = new Dictionary<string, double>();
        private List<string> BlacklistedLoot = new List<string>();

        #endregion

        #region Loading config

        //Загрузка стандартного конфиг-файла. Вызывается ТОЛЬКО в случае отсутствия файла PluginName.json в папке config
        protected override void LoadDefaultConfig()
        {
        }

        void LoadConfigValues()
        {
            AvaliableMods = new List<string>()
            {
                GatherDayString,
                GatherNightString,
				ChainsawDayString,
                ChainsawNightString,
				JackhammerDayString,
                JackhammerNightString,
                PickUpDayString,
                PickUpNightString,
                QuerryDayString,
                QuerryNightString,
                PumpjackDayString,
                PumpjackNightString,
                LootDayString,
                LootNightString,
                SmeltDayString,
                SmeltNightString
            };
            Dictionary<string, object> defaultRates = CreatePerms(AvaliableMods, 1f);
            Dictionary<string, object> customRates = new Dictionary<string, object>()
            {
                ["ratescontroller.elite"] = CreatePerms(AvaliableMods, 5f),
				["ratescontroller.premium"] = CreatePerms(AvaliableMods, 4f),
				["ratescontroller.gold"] = CreatePerms(AvaliableMods, 3f),
                ["ratescontroller.vip"] = CreatePerms(AvaliableMods, 2f)
            };
            Dictionary<string, object> defaultGatherRates = new Dictionary<string, object>()
            {
                {"Animal Fat", 1.0},
                {"Bear Meat", 1.0},
                {"Bone Fragments", 1.0},
                {"Cloth", 1.0},
                {"High Quality Metal Ore", 1.0},
                {"Human Skull", 1.0},
                {"Leather", 1.0},
                {"Metal Ore", 1.0},
                {"Pork", 1.0},
                {"Raw Chicken Breast", 1.0},
                {"Raw Human Meat", 1.0},
                {"Raw Wolf Meat", 1.0},
                {"Stones", 1.0},
                {"Sulfur Ore", 1.0},
                {"Wolf Skull", 1.0},
                {"Wood", 1.0},
                {"Raw Deer Meat", 1.0 },
                {"Cactus Flesh", 1.0 }
            };
            Dictionary<string, object> defaultPickupRates = new Dictionary<string, object>()
            {
                {"Metal Ore", 1.0},
                {"Stones", 1.0},
                {"Sulfur Ore", 1.0},
                {"Wood", 1.0},
                {"Hemp Seed", 1.0},
                {"Corn Seed", 1.0},
                {"Pumpkin Seed", 1.0},
                {"Cloth", 1.0},
                {"Pumpkin", 1.0},
                {"Corn", 1.0},
                {"Wolf Skull", 1.0}
            };
            Dictionary<string, object> defaultQuerryRates = new Dictionary<string, object>()
            {
                {"High Quality Metal Ore", 1.0},
                {"Metal Fragments", 1.0},
                {"Metal Ore", 1.0},
                {"Stones", 1.0},
                {"Sulfur Ore", 1.0}
            };
			Dictionary<string, object> defaultPumpjackRates = new Dictionary<string, object>()
            {
				{"Crude Oil", 1.0}
            };
            List<object> blacklistedLoot = new List<object>()
            {
                "Rotten Apple",
                "Spoiled Wolf Meat",
                "Spoiled Chicken",
                "Spoiled Human Meat"
            };
            Dictionary<string, object> defaultSmeltRates = new Dictionary<string, object>();
            //var itemDefinitions = ItemManager.itemList;
            var itemDefinitions = ItemManager.GetItemDefinitions();
            foreach (var item in itemDefinitions)
            {
                // Записываем стандартные рейты готовки
                var cookable = item.GetComponent<ItemModCookable>();
                if (cookable == null) continue;
                defaultSmeltRates.Add(item.displayName.english, cookable.cookTime);
                SmeltBackup.Add(item.displayName.english, cookable.cookTime);
            }
            GetConfig(BlacklistedLootCfg, ref blacklistedLoot);
            GetConfig(DefaultRatesCfg, ref defaultRates);
            GetConfig(CustomRatesCfg, ref customRates);
            GetConfig(DefaultPickupRatesCfg, ref defaultPickupRates);
            GetConfig(DefaultGatherRatesCfg, ref defaultGatherRates);
            GetConfig(DefaultSmeltRatesCfg, ref defaultSmeltRates);
            GetConfig(DefaultQuerryRatesCfg, ref defaultQuerryRates);
			GetConfig(DefaultPumpjackRatesCfg, ref defaultPumpjackRates);
            GetConfig(PrefixCfg, ref Prefix);
            GetConfig(PrefixColorCfg, ref PrefixColor);
            GetConfig(UseLootMultyplierCfg, ref UseLootMultyplier);
            GetConfig(DayStartCfg, ref DayStart);
            GetConfig(DayLenghtCfg, ref DayLenght);
            GetConfig(NightStartCfg, ref NightStart);
            GetConfig(NightLenghtCfg, ref NightLenght);
            GetConfig(WarnChatCfg, ref WarnChat);
            GetConfig(CoalRateDayCfg, ref CoalRateDay);
            GetConfig(CoalRateNightCfg, ref CoalRateNight);
            GetConfig(CoalChanceDayCfg, ref CoalChanceDay);
            GetConfig(CoalChanceNightCfg, ref CoalChanceNight);
            GetConfig(MoreHQMCfg, ref MoreHQM);
            SaveConfig();

            BlacklistedLoot = blacklistedLoot.Select(x => x.ToString()).ToList();

            foreach (var item in defaultRates)
            {
                double mod;
                if (!double.TryParse(item.Value.ToString(), out mod))
                {
                    PrintWarning($"Default rates for {item.Key} is incorrect and will not work untill it will be resolved.");
                    continue;
                }
                DefaultRates.Add(item.Key, mod);
            }

            foreach (var item in customRates)
            {
                Dictionary<string, object> perms = (Dictionary<string, object>)item.Value;
                Dictionary<string, double> Perms = new Dictionary<string, double>();
                foreach (var p in perms)
                {
                    double mod;
                    if (!double.TryParse(p.Value.ToString(), out mod))
                    {
                        PrintWarning($"Custom rates for {item.Key} - {p.Key} is incorrect and will not work untill it will be resolved.");
                        continue;
                    }
                    Perms.Add(p.Key, mod);
                }
                CustomRates.Add(item.Key, Perms);
            }

            foreach (var item in defaultGatherRates)
            {
                double mod;
                if (!double.TryParse(item.Value.ToString(), out mod))
                {
                    PrintWarning($"Default gather rates for {item.Key} is incorrect and will not work untill it will be resolved.");
                    continue;
                }
                DefaultGatherRates.Add(item.Key, mod);
            }

            foreach (var item in defaultPickupRates)
            {
                double mod;
                if (!double.TryParse(item.Value.ToString(), out mod))
                {
                    PrintWarning($"Default pickup rates for {item.Key} is incorrect and will not work untill it will be resolved.");
                    continue;
                }
                DefaultPickupRates.Add(item.Key, mod);
            }

            foreach (var item in defaultQuerryRates)
            {
                double mod;
                if (!double.TryParse(item.Value.ToString(), out mod))
                {
                    PrintWarning($"Default querry rates for {item.Key} is incorrect and will not work untill it will be resolved.");
                    continue;
                }
                DefaultQuerryRates.Add(item.Key, mod);
            }
			
			foreach (var item in defaultPumpjackRates)
            {
                double mod;
                if (!double.TryParse(item.Value.ToString(), out mod))
                {
                    PrintWarning($"Default pumpjack rates for {item.Key} is incorrect and will not work untill it will be resolved.");
                    continue;
                }
                DefaultPumpjackRates.Add(item.Key, mod);
            }

        }
        #endregion

        #region localization
        void LoadMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LootRate"] = "Рейты <color=#1CA9C9>компонентов</color> <color=#66FF00>x{rates}</color>\n",
                ["CoalRate"] = "Рейты <color=#1CA9C9>производства угля</color> <color=#66FF00>x{rates}</color>\n",
                ["No Permission"] = "Недостаточно прав на выполнение данной команды.",
                ["Day Starts"] = "\n<color=#FFCF40>ДОБРОЕ УТРО</color>\n",
                ["Night Starts"] = "\n<color=#1E90FF>ДОБРОЙ НОЧИ</color>\n",
                ["PickupRates"] = "Рейты <color=#1CA9C9>подбираемых</color> ресурсов <color=#66FF00>x{rates}</color>\n",
                ["GatherRates"] = "Рейты <color=#1CA9C9>добываемых</color> ресурсов <color=#66FF00>x{rates}</color>\n",
                ["QuerryRates"] = "Рейты <color=#1CA9C9>карьеров</color> <color=#66FF00>x{rates}</color>\n",
				["ChainsawRates"] = "Рейты <color=#1CA9C9>бензопилой</color> <color=#66FF00>x{rates}</color>\n",
				["JackhammerRates"] = "Рейты <color=#1CA9C9>отбойным молотком</color> <color=#66FF00>x{rates}</color>\n",
				["PumpjackRates"] = "Рейты <color=#1CA9C9>нефтекачки</color> <color=#66FF00>x{rates}</color>\n",
                ["Smelt Rate"] = "Скорость <color=#1CA9C9>работы печей</color> <color=#66FF00>x{rates}</color>\n",
                ["PersonalRates"] = "\n<color=#FFFF00>ЛИЧНЫЕ РЕЙТЫ:</color>\n",
                ["PickUpRatesPers"] = "Рейты <color=#1CA9C9>подбираемых</color> ресурсов <color=#66FF00>x{rates}</color>\n",
                ["GatherRatesPers"] = "Рейты <color=#1CA9C9>добываемых</color> ресурсов <color=#66FF00>x{rates}</color>\n",
                ["QueryPers"] = "Рейты <color=#1CA9C9>карьера</color> <color=#66FF00>x{rates}</color>\n",
				["ChainsawPers"] = "Рейты <color=#1CA9C9>бензопилой</color> <color=#66FF00>x{rates}</color>\n",
				["JackhammerPers"] = "Рейты <color=#1CA9C9>отбойным молотком</color> <color=#66FF00>x{rates}</color>\n",
				["PumpjackPers"] = "Рейты <color=#1CA9C9>нефтекачки</color> <color=#66FF00>x{rates}</color>\n",
                ["SmeltPers"] = "Рейты <color=#1CA9C9>переплавки</color> <color=#66FF00>x{rates}</color>\n",
                ["LootPers"] = "Рейты <color=#1CA9C9>компонентов</color> <color=#66FF00>x{rates}</color>"
            }, this, "en");
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["LootRate"] = "Рейты <color=#1CA9C9>компонентов</color> <color=#66FF00>x{rates}</color>\n",
                ["CoalRate"] = "Рейты <color=#1CA9C9>производства угля</color> <color=#66FF00>x{rates}</color>\n",
                ["No Permission"] = "Недостаточно прав на выполнение данной команды.",
                ["Day Starts"] = "\n<color=#FFCF40>ДОБРОЕ УТРО</color>\n",
                ["Night Starts"] = "\n<color=#1E90FF>ДОБРОЙ НОЧИ</color>\n",
                ["PickupRates"] = "Рейты <color=#1CA9C9>подбираемых</color> ресурсов <color=#66FF00>x{rates}</color>\n",
                ["GatherRates"] = "Рейты <color=#1CA9C9>добываемых</color> ресурсов <color=#66FF00>x{rates}</color>\n",
                ["QuerryRates"] = "Рейты <color=#1CA9C9>карьеров</color> <color=#66FF00>x{rates}</color>\n",
				["ChainsawRates"] = "Рейты <color=#1CA9C9>бензопилой</color> <color=#66FF00>x{rates}</color>\n",
				["JackhammerRates"] = "Рейты <color=#1CA9C9>отбойным молотком</color> <color=#66FF00>x{rates}</color>\n",
				["PumpjackRates"] = "Рейты <color=#1CA9C9>нефтекачки</color> <color=#66FF00>x{rates}</color>\n",
                ["Smelt Rate"] = "Скорость <color=#1CA9C9>работы печей</color> <color=#66FF00>x{rates}</color>\n",
                ["PersonalRates"] = "\n<color=#FFFF00>ЛИЧНЫЕ РЕЙТЫ:</color>\n",
                ["PickUpRatesPers"] = "Рейты <color=#1CA9C9>подбираемых</color> ресурсов <color=#66FF00>x{rates}</color>\n",
                ["GatherRatesPers"] = "Рейты <color=#1CA9C9>добываемых</color> ресурсов <color=#66FF00>x{rates}</color>\n",
                ["QueryPers"] = "Рейты <color=#1CA9C9>карьера</color> <color=#66FF00>x{rates}</color>\n",
				["ChainsawPers"] = "Рейты <color=#1CA9C9>бензопилой</color> <color=#66FF00>x{rates}</color>\n",
				["JackhammerPers"] = "Рейты <color=#1CA9C9>отбойным молотком</color> <color=#66FF00>x{rates}</color>\n",
				["PumpjackPers"] = "Рейты <color=#1CA9C9>нефтекачки</color> <color=#66FF00>x{rates}</color>\n",
                ["SmeltPers"] = "Рейты <color=#1CA9C9>переплавки</color> <color=#66FF00>x{rates}</color>\n",
                ["LootPers"] = "Рейты <color=#1CA9C9>компонентов</color> <color=#66FF00>x{rates}</color>"
            }, this, "ru");
        }
        #endregion

        #region initializing
        void OnServerInitialized()
        {
            //Загружаем конфиг из файла
            LoadConfigValues();

            //Подгружаем данные локализации
            LoadMessages();
            hqmo = ItemManager.FindItemDefinition("hq.metal.ore");
            if (!UseLootMultyplier)
            {
                Unsubscribe("OnContainerDropItems");
                Unsubscribe("OnLootEntity");
            }
            //Инициализируем управление временем - получаем компоненту времени
            timer.Once(3, GetTimeComponent);

            foreach (var perm in CustomRates.Keys)
            {
                permission.RegisterPermission($"{perm}".ToLower(), this);
                //PrintWarning($"{Title}.{perm}".ToLower());
            }
            var curtime = covalence.Server.Time;
            //Если при старте на сервере день - ставим в false, дабы вызывалось событие OnDayStart()
            isDay = (!(DayStart <= curtime.Hour) || !(curtime.Hour < NightStart));

            UpdateFurnaces();
        }

        //Вызывается при выгрузке плагина
        void Unload()
        {
            //Очищаем привящку к ивнту
            if (timeComponent != null)
                timeComponent.OnHour -= OnHour;
            //Восстанавливаем стандартное время переплавки
            foreach (var item in ItemManager.GetItemDefinitions())
            {
                if (!SmeltBackup.ContainsKey(item.displayName.english)) continue;
                var cookable = item.GetComponent<ItemModCookable>();
                if (cookable != null)
                {
                    cookable.cookTime = SmeltBackup[item.displayName.english];
                }
            }
        }

        #endregion

        #region Time managment

        //Переменная, для хранения компоненты времени
        private TOD_Time timeComponent = null;

        //Заморожено ли время.
        private bool Frozen = false;

        #region main
        //Колличество попыток определения компоненты
        private uint componentSearchAttempts = 0;

        //Инициализация управления временем
        private void GetTimeComponent()
        {
            //Если Instance == 0,
            if (TOD_Sky.Instance == null)
            {
                //Увеличиваем номер попытки
                ++componentSearchAttempts;
                if (this.componentSearchAttempts < 50)
                {
                    PrintWarning("Restarting timer for GetTimeComponent(). Attempt " + componentSearchAttempts.ToString() + "/10.");
                    timer.Once(3, GetTimeComponent);
                }
                else
                {
                    RaiseError("По всем вопросам обращаться сюда: https://vk.com/id494713683");
                }

                return;
            }

            if (TOD_Sky.Instance != null && componentSearchAttempts >= 0)
            {
                Puts("Found TOD_Time component after attempt " + componentSearchAttempts.ToString() + ".");
            }

            //Записываем компаненту времени
            timeComponent = TOD_Sky.Instance.Components.Time;

            if (timeComponent == null)
            {
                RaiseError("Could not fetch time component. Plugin will not work without it.");
                return;
            }

            //Добавляем ивент к событию
            timeComponent.OnHour += OnHour;

            //Вызываем функцию, дабы узнать текущее время суток.
            OnHour();

        }

        //Идёт ли прогресс времени, данные хватаем из игры
        //Ибо костылями - мир полнится....
        private bool ProgressTime
        {
            get
            {
                return timeComponent.ProgressTime; 
            }
            set
            {
                timeComponent.ProgressTime = value;
            }
        }

        //Обработчик события OnHour
        private void OnHour()
        {
            if (DayStart <= CurrentHour && CurrentHour < NightStart)
            {
                if (isDay) return;
                //Устанавливаем время суток на День
                isDay = true;
                //Вызываем процедуру обновления длинны суток
                UpdateDayLenght(DayLenght, false);
                Interface.Oxide.CallHook("OnDayStart");
            }
            else
            {
                if (!isDay) return;
                //Устанавливаем время суток на ночь
                isDay = false;
                //Вызываем процедуру обновления длинны суток
                UpdateDayLenght(NightLenght, true);
                Interface.Oxide.CallHook("OnNightStart");
            }
        }

        //Функция, обновляющяя длительность суток в зависимости от времени суток в игре.
        void UpdateDayLenght(uint Lenght, bool night)
        {
            float dif = NightStart - DayStart;
            if (night)
            {
                dif = (24 - dif);
            }
            float part = 24.0f / dif;
            float newLenght = part * Lenght;
            if (newLenght <= 0) newLenght = 0.1f;
            timeComponent.DayLengthInMinutes = newLenght;
        }

        #endregion

        #region Helpers

        //Возвращает текущий час
        public float CurrentHour => TOD_Sky.Instance.Cycle.Hour;

        #endregion

        #endregion

        #region Rate controller
        void OnContainerDropItems(ItemContainer container)
        {
            if (container == null)
            {
                return;
            }
            var lootcont = container?.entityOwner as LootContainer;
            if (lootcont == null)
                return;
            var basePlayer = lootcont.lastAttacker as BasePlayer;
            var player = basePlayer != null ? basePlayer : null;
            if(player == null) return;
            double rate = GetUserRates(player.UserIDString, isDay ? LootDayString : LootNightString);

            foreach (var lootitem in lootcont.inventory.itemList)
            {
                if (lootitem.info.stackable <= 1) continue;
                if (BlacklistedLoot.Contains(lootitem.info.displayName.english) || BlacklistedLoot.Contains(lootitem.info.shortname)) continue;
                var newAmount = (int)(lootitem.amount * rate);
                lootitem.amount = newAmount > 1 ? newAmount : 1;
            }
        }
        void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity is SupplyDrop) return;
            if (entity is LockedByEntCrate) return;
            if (entity is Stocking) return;

            var lootcont = entity as LootContainer;
            if (!lootcont) return;
            if (lootcont.OwnerID == player.userID) return;
            var instanceID = lootcont.GetInstanceID();
            DateTime cd;
            if (!CratesCD.TryGetValue(instanceID, out cd))
            {
                cd = DateTime.Now;
                CratesCD.Add(instanceID, cd);
            }
            if (cd.Subtract(DateTime.Now).TotalSeconds > 0)
            {
                return;
            }
            CratesCD[instanceID] = DateTime.Now.AddMinutes(2);
            lootcont.OwnerID = player.userID;
            lootcont.SpawnLoot();

            double rate = GetUserRates(player.UserIDString, isDay ? LootDayString : LootNightString);

            foreach (var lootitem in lootcont.inventory.itemList)
            {
                if (lootitem.info.stackable <= 1) continue;
                if (BlacklistedLoot.Contains(lootitem.info.displayName.english) || BlacklistedLoot.Contains(lootitem.info.shortname)) continue;
                var new_amount = (int)(lootitem.amount * rate);
                lootitem.amount = new_amount > 1 ? new_amount : 1;
            }
        }
        private void UpdateFurnaces()
        {
            var baseOvens = Resources.FindObjectsOfTypeAll<BaseOven>().Where(c => c.isActiveAndEnabled).Cast<BaseEntity>().ToList();
            foreach (var oven in baseOvens)
            {
                if (!oven.HasFlag(BaseEntity.Flags.On)) continue;
                double ovenMultiplier = GetUserRates(oven.OwnerID.ToString(), isDay ? SmeltDayString : SmeltNightString);
                if (ovenMultiplier > 10f) ovenMultiplier = 10f;
                if (ovenMultiplier < 0.1f) ovenMultiplier = 0.1f;
                InvokeHandler.CancelInvoke(oven.GetComponent<MonoBehaviour>(), new Action((oven as BaseOven).Cook));
                (oven as BaseOven).inventory.temperature = CookingTemperature((oven as BaseOven).temperature);
                (oven as BaseOven).UpdateAttachmentTemperature();
                InvokeHandler.InvokeRepeating(oven.GetComponent<MonoBehaviour>(), new Action((oven as BaseOven).Cook), (float)(0.5f / ovenMultiplier), (float)(0.5f / ovenMultiplier));
            }
        }
        float CookingTemperature(BaseOven.TemperatureType temperature)
        {
            switch (temperature)
            {
                case BaseOven.TemperatureType.Warming:
                    return 50f;
                case BaseOven.TemperatureType.Cooking:
                    return 200f;
                case BaseOven.TemperatureType.Smelting:
                    return 1000f;
                case BaseOven.TemperatureType.Fractioning:
                    return 1500f;
                default:
                    return 15f;
            }
        }
        object OnOvenToggle(BaseOven oven, BasePlayer player)
        {
            if (oven.HasFlag(BaseEntity.Flags.On)) return null;
            double ovenMultiplier = GetUserRates(oven.OwnerID.ToString(), isDay ? SmeltDayString : SmeltNightString);
            if (ovenMultiplier > 10f) ovenMultiplier = 10f;
            if (ovenMultiplier < 0.1f) ovenMultiplier = 0.1f;
            StartCooking(oven, oven.GetComponent<BaseEntity>(), ovenMultiplier);
            return false;
        }
        void StartCooking(BaseOven oven, BaseEntity entity, double ovenMultiplier)
        {
            if (FindBurnable(oven) == null)
                return;
            oven.inventory.temperature = CookingTemperature(oven.temperature);
            oven.UpdateAttachmentTemperature();
            InvokeHandler.CancelInvoke(entity.GetComponent<MonoBehaviour>(), new Action(oven.Cook));
            InvokeHandler.InvokeRepeating(entity.GetComponent<MonoBehaviour>(), new Action(oven.Cook), (float)(0.5f / ovenMultiplier), (float)(0.5f / ovenMultiplier));
            entity.SetFlag(BaseEntity.Flags.On, true, false);
        }
        Item FindBurnable(BaseOven oven)
        {
            if (oven.inventory == null)
                return null;
            foreach (Item current in oven.inventory.itemList)
            {
                ItemModBurnable component = current.info.GetComponent<ItemModBurnable>();
                if (component && (oven.fuelType == null || current.info == oven.fuelType))
                    return current;
            }
            return null;
        }

        private void OnDayStart()
        {
            CoalRate = CoalRateDay;
            CoalChance = CoalChanceDay;
            SmeltRate = DefaultRates[SmeltDayString];
            GatherRate = DefaultRates[GatherDayString];
			ChainsawRate = DefaultRates[ChainsawDayString];
			JackhammerRate = DefaultRates[JackhammerDayString];
            PickupRate = DefaultRates[PickUpDayString];
            QuerryRate = DefaultRates[QuerryDayString];
			PumpjackRate = DefaultRates[PumpjackDayString];
            LootRate = DefaultRates[LootDayString];
            //Обновляем скорость переплавки
            //UpdateSmeltTime();
            UpdateFurnaces();

            //Оповещаем игоков о смене времени суток
            if (WarnChat)
            {
                RatesToChat();
            }
        }
        private void OnNightStart()
        {
            CoalRate = CoalRateNight;
            CoalChance = CoalChanceNight;
            SmeltRate = DefaultRates[SmeltNightString];
            GatherRate = DefaultRates[GatherNightString];
			ChainsawRate = DefaultRates[ChainsawNightString];
			JackhammerRate = DefaultRates[JackhammerNightString];
            PickupRate = DefaultRates[PickUpNightString];
            QuerryRate = DefaultRates[QuerryNightString];
			PumpjackRate = DefaultRates[PumpjackNightString];
            LootRate = DefaultRates[LootNightString];
            //Обновляем скорость переплавки
            //UpdateSmeltTime();
            UpdateFurnaces();

            //Оповещаем игоков о смене времени суток
            if (WarnChat)
            {
                RatesToChat();
            }
        }
        private void RatesToChat()
        {
            string Message = string.Empty;
            Message = isDay ? GetMsg("Day Starts") : GetMsg("Night Starts");
            Message += GetMsg("GatherRates").Replace("{rates}", GatherRate.ToString());
            Message += GetMsg("PickupRates").Replace("{rates}", PickupRate.ToString());
            Message += GetMsg("QuerryRates").Replace("{rates}", QuerryRate.ToString());
			Message += GetMsg("ChainsawRates").Replace("{rates}", ChainsawRate.ToString());
			Message += GetMsg("JackhammerRates").Replace("{rates}", JackhammerRate.ToString());
			Message += GetMsg("PumpjackRates").Replace("{rates}", PumpjackRate.ToString());
            Message += GetMsg("Smelt Rate").Replace("{rates}", SmeltRate.ToString());
            Message += GetMsg("CoalRate").Replace("{rates}", CoalRate.ToString());
            if (UseLootMultyplier)
                Message += GetMsg("LootRate").Replace("{rates}", LootRate.ToString());
            SendToChat(Message);
        }


        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            double mod = 1f;
            if (DefaultPickupRates.ContainsKey(item.info.displayName.english))
            {
                mod = DefaultPickupRates[item.info.displayName.english];
            }
            int new_amount;
            if (isDay)
            {
                new_amount = (int)(item.amount * GetUserRates(player.UserIDString, PickUpDayString) * mod);
                item.amount = new_amount > 1 ? new_amount : 1;
                return;
            }
            new_amount = (int)(item.amount * GetUserRates(player.UserIDString, PickUpNightString) * mod);
            item.amount = new_amount > 1 ? new_amount : 1;
        }
        void OnCropGather(PlantEntity plant, Item item, BasePlayer player)
        {
            double mod = 1f;
            if (DefaultPickupRates.ContainsKey(item.info.displayName.english))
            {
                mod = DefaultPickupRates[item.info.displayName.english];
            }
            int new_amount;
            if (isDay)
            {
                new_amount = (int)(item.amount * GetUserRates(player.UserIDString, PickUpDayString) * mod);
                item.amount = new_amount > 1 ? new_amount : 1;
                return;
            }
            new_amount = (int)(item.amount * GetUserRates(player.UserIDString, PickUpNightString) * mod);
            item.amount = new_amount > 1 ? new_amount : 1;
        }
        void OnEntityKill(BaseNetworkable entity)
        {
            var lootcont = entity as LootContainer;
            if (lootcont)
            {
                var instanceid = lootcont.GetInstanceID();
                if (CratesCD.ContainsKey(instanceid))
                {
                    CratesCD.Remove(instanceid);
                }
            }
        }
        void OnDispenserBonus(ResourceDispenser disp, BasePlayer player, Item item)
        {
            if (player == null) return;
            double mod = 1f;
            if (DefaultGatherRates.ContainsKey(item.info.displayName.english))
            {
                mod = DefaultGatherRates[item.info.displayName.english];
            }
			if (!player) return;
			var activeItem = player.GetActiveItem();
			if (activeItem == null) return;
			var toolName = activeItem.info.shortname;
            double rate = GetUserRates(player.UserIDString, isDay ? GatherDayString : GatherNightString);
			if (toolName == "chainsaw") { rate = GetUserRates(player.UserIDString, isDay ? ChainsawDayString : ChainsawNightString); }
			if (toolName == "jackhammer") { rate = GetUserRates(player.UserIDString, isDay ? JackhammerDayString : JackhammerNightString); }
            var newAmount = (int)(item.amount * rate * mod);
            item.amount = newAmount > 1 ? newAmount : 1;
        }
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!entity.ToPlayer()) return;
            double mod = 1f;
            if (DefaultGatherRates.ContainsKey(item.info.displayName.english))
            {
                mod = DefaultGatherRates[item.info.displayName.english];
            }
			var player = entity as BasePlayer;
			if (!player) return;
			var activeItem = player.GetActiveItem();
			if (activeItem == null) return;
			var toolName = activeItem.info.shortname;
            double rate = GetUserRates(entity.ToPlayer().UserIDString, isDay ? GatherDayString : GatherNightString);
			if (toolName == "chainsaw") { rate = GetUserRates(entity.ToPlayer().UserIDString, isDay ? ChainsawDayString : ChainsawNightString); }
			if (toolName == "jackhammer") { rate = GetUserRates(entity.ToPlayer().UserIDString, isDay ? JackhammerDayString : JackhammerNightString); }
            var newAmount = (int)(item.amount * rate * mod);
            item.amount = newAmount > 1 ? newAmount : 1;
            if (!MoreHQM || dispenser.gatherType != ResourceDispenser.GatherType.Ore) return;
            bool HaveHQM = dispenser.finishBonus.Any(x => x.itemDef.shortname == "hq.metal.ore");
            var reply = 309;
            if (!HaveHQM)
            {
                dispenser.finishBonus.Add(new ItemAmount(hqmo, 2f));
            }
        }
        void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            double mod = 1f;
            if (DefaultQuerryRates.ContainsKey(item.info.displayName.english))
            {
                mod = DefaultQuerryRates[item.info.displayName.english];
            }
            int newAmount;
            if (isDay)
            {
                newAmount = (int)(item.amount * GetUserRates(quarry.OwnerID, QuerryDayString) * mod);
				if (quarry.ShortPrefabName.Contains("mining.pumpjack")) newAmount = (int)(item.amount * GetUserRates(quarry.OwnerID, PumpjackDayString) * mod);
                item.amount = newAmount > 1 ? newAmount : 1;
                return;
            }
            newAmount = (int)(item.amount * GetUserRates(quarry.OwnerID, QuerryNightString) * mod);
			if (quarry.ShortPrefabName.Contains("mining.pumpjack")) newAmount = (int)(item.amount * GetUserRates(quarry.OwnerID, PumpjackNightString) * mod);
            item.amount = newAmount > 1 ? newAmount : 1;
        }
        void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            if (oven == null) return;
            burnable.byproductAmount = (int)Math.Ceiling(CoalRate);
            burnable.byproductChance = (100 - CoalChance) / 100f;
            if (burnable.byproductChance == 0)
            {
                burnable.byproductChance = -1;
            }
        }
        //void UpdateSmeltTime()
        //{
        //    var itemDefinitions = ItemManager.GetItemDefinitions();
        //    foreach (var item in itemDefinitions)
        //    {
        //        var cookable = item.GetComponent<ItemModCookable>();
        //        if (cookable != null)
        //        {
        //            if (DefaultSmeltRates.ContainsKey(item.displayName.english))
        //            {
        //                cookable.cookTime = (float)(DefaultSmeltRates[item.displayName.english] / SmeltRate);
        //            }else
        //            {
        //                DefaultSmeltRates.Add(item.displayName.english, cookable.cookTime);
        //                cookable.cookTime = (float)(DefaultSmeltRates[item.displayName.english] / SmeltRate);
        //            }
        //        }
        //    }
        //}
        #endregion

        #region Сonsole commands
        [ConsoleCommand("env.freeze")]
        void TimeFreeze(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (Frozen)
            {
                //Puts("The time is already frozen!");
                arg.ReplyWith("The time is already frozen!");
                return;
            }
            Frozen = true;
            ProgressTime = false;
            //Puts("The time was frozen.");
            arg.ReplyWith("The time was frozen.");
        }

        [ConsoleCommand("env.unfreeze")]
        void TimeUnFreeze(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (!Frozen)
            {
                arg.ReplyWith("The time is not frozen!");
                return;
            }
            Frozen = false;
            ProgressTime = true;
            arg.ReplyWith("The time was unfrozen.");
        }

        [ConsoleCommand("rates.show")]
        void ShowRates(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 2) return;
            if (arg.Args == null)
            {
                arg.ReplyWith("Usage rates.show steamid [type]");
                return;
            }
            var target = covalence.Players.FindPlayer(arg.Args[0]);
            if (target == null)
            {
                arg.ReplyWith("User not found or multiply user mathces");
                return;
            }
            if (arg.Args.Length >= 2)
            {
                arg.ReplyWith(GetUserRates(target.Id, arg.Args[1]).ToString());
                return;
            }
            string reply = $"User '{target.Name}' current rates:\n";
            foreach (var p in AvaliableMods)
            {
                reply += p + ": " + GetUserRates(target.Id, p).ToString() + " \n";
            }
            arg.ReplyWith(reply);
        }
        #endregion

        #region Chat commands
        [ChatCommand("rates")]
        private void ShowRatesChat(BasePlayer player, string command, string[] args)
        {
            string reply = GetMsg("PersonalRates", player.UserIDString);
            if (isDay)
            {
                reply += GetMsg("GatherRatesPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, GatherDayString).ToString());
                reply += GetMsg("PickUpRatesPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, PickUpDayString).ToString());
                reply += GetMsg("QueryPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, QuerryDayString).ToString());
				reply += GetMsg("ChainsawPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, ChainsawDayString).ToString());
				reply += GetMsg("JackhammerPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, JackhammerDayString).ToString());
				reply += GetMsg("PumpjackPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, PumpjackDayString).ToString());
                reply += GetMsg("SmeltPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, SmeltDayString).ToString());
                reply += GetMsg("LootPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, LootDayString).ToString());
                SendToChat(player, reply);
                return;
            }
            reply += GetMsg("GatherRatesPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, GatherNightString).ToString());
            reply += GetMsg("PickUpRatesPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, PickUpNightString).ToString());
            reply += GetMsg("QueryPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, QuerryNightString).ToString());
			reply += GetMsg("ChainsawPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, ChainsawNightString).ToString());
			reply += GetMsg("JackhammerPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, JackhammerNightString).ToString());
			reply += GetMsg("PumpjackPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, PumpjackNightString).ToString());
            reply += GetMsg("SmeltPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, SmeltNightString).ToString());
            reply += GetMsg("LootPers", player.UserIDString).Replace("{rates}", GetUserRates(player.UserIDString, LootNightString).ToString());
            SendToChat(player, reply);
        }
        #endregion

        #region Helpers
        private void GetConfig<T>(string Key, ref T var)
        {
            if (Config[Key] != null)
            {
                var = (T)Convert.ChangeType(Config[Key], typeof(T));
            }
            Config[Key] = var;
        }

        //Функция, отправляющая сообщение в чат конкретному пользователю, добавляет префикс
        private void SendToChat(BasePlayer Player, string Message)
        {
            PrintToChat(Player, "<color=" + PrefixColor + ">" + Prefix + "</color> " + Message);
        }

        //Перезгрузка функции отправки собщения в чат - отправляет сообщение всем пользователям
        private void SendToChat(string Message)
        {
            PrintToChat("<color=" + PrefixColor + ">" + Prefix + "</color> " + Message);
        }

        //Функция получения строки из языкового файла
        string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID?.ToString());
        double GetUserRates(string steamId, string RateType)
        {
            /*
             * Из списка кастомных привелегий выбираем только те, на которые у игрока есть привелегия.
             * выбираем только сами привелегии, без названий. Только содерживое
             * Из них выбираем те, где есть нужный нам тип рейтов. И выбираем только нужные нам типы рейтов.
             */
            var playergroups = CustomRates.Where(i => permission.UserHasPermission(steamId, i.Key)).Select(i => i.Value).
                Where(i => i.ContainsKey(RateType)).Select(i => i[RateType]);
            return playergroups.Any() ? playergroups.Aggregate((i1, i2) => i1 > i2 ? i1 : i2) : DefaultRates[RateType];
        }
        //Перегрузка функции. Ибо мне так будет проще)
        double GetUserRates(ulong steamId, string RateType) => GetUserRates(steamId.ToString(), RateType);

        Dictionary<string, object> CreatePerms(List<string> mods, double rate)
        {
            return mods.ToDictionary(x => x, x => (object)rate);
        }


        #endregion
    }
}
////////////////////////////////////////////////////////////////////////////////////////////////
