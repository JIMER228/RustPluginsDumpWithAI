using System;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Economics","https://discord.gg/9vyTXsJyKR","1.0.1")]
    public class Economics : RustPlugin
    {
        #region var

        [PluginReference] private Plugin ImageLibrary,RustStore,GameStore,BroadcastSystem;

        private static Economics _;
        private string path = "Economics/";
        string _defaultCurrency;
        

        #endregion
 
        #region config

        class Money
        {
            [JsonProperty("Название валюты")]
            public string Name;
            [JsonProperty("Ключ валюты")]
            public string Key;
            [JsonProperty("Виртуальная/физическая(true/false)")]
            public bool Vitrual;
            [JsonProperty("Максимальное кол-во")]
            public float Max;
            [JsonProperty("Может быть отрицательным")]
            public bool Negative;
            [JsonProperty("Картинка")]
            public string Img;
            [JsonProperty("skinId(Если физическая)")]
            public ulong SkinId;
            [JsonProperty("Начальный бонус")]
            public int StartAmount = 1000;
            [JsonProperty("Сбрасывать после вайпа?(true/false)")]
            public bool WipeOnNewSave = false;
        }

        class FarmPoints
        {
            [JsonProperty("Настройка валюты за руду")]
            public OreGather Ore;
            [JsonProperty("Настройки валюты за дерево")]
            public WoodGather Wood;
            [JsonProperty("Настройки валюты за убийство")]
            public KillPoints KP;
            [JsonProperty("Настройки валюты за время")]
            public TimePoints TimePoints;
        }

        class OreGather
        {
            [JsonProperty("Валюта за добычу серы")]
            public double SulfurOreGather;
            [JsonProperty("Валюта за добычу железа")]
            public double MetalOreGather;
            [JsonProperty("Валюта за добычу камня")]
            public double StoneOreGather;
            
            [JsonProperty("Валюта за бонус серы")]
            public double SulfurOreBonus;
            [JsonProperty("Валюта за бонус железа")]
            public double MetalOreBonus;
            [JsonProperty("Валюта за бонус камня")]
            public double StoneOreBonus;
        }

        class WoodGather
        {
            [JsonProperty("Валюта за добычу дерева")]
            public double Gather;
            [JsonProperty("Валюта за бонус дерева")]
            public double Bonus;

        }

        class KillPoints
        {
            [JsonProperty("Валюта за убийство NPC")]
            public double Npc;
            [JsonProperty("Валюта за убийство Животного")]
            public double Animal;
            [JsonProperty("Валюта за убийство Игрока")]
            public double Player;
            [JsonProperty("Валюта за убийство Танка")]
            public double Tank = 5;
            [JsonProperty("Валюта за убийство Вертолета")]
            public double Heli = 5;
            [JsonProperty("Валюта за убийство Чинука")]
            public double Ch47 = 5;
        }

        class TimePoints
        {
            [JsonProperty("Как часто давать валюту за время(секунды)")]
            public short Time = 60;
            [JsonProperty("Количество валюты за время")]
            public double Amount = 1;
        }

        class LootPoints
        {
            [JsonProperty("Валюта за разрушение бочки")]
            public double Barrel = 0;
            [JsonProperty("Валюта за открытие залоченого контейнера")]
            public double LockedContainer = 0;
            [JsonProperty("Валюта за собирательство")]
            public double PickUp = 0;
        }
        
        internal class GameStores
        {
            [JsonProperty("API Магазина(GameStores)")]
            public string GameStoreAPIStore;
            [JsonProperty("ID Магазина(GameStores)")]
            public string GameStoreIDStore;
            [JsonProperty("Сообщение в магазин при выдаче баланса(GameStores)")]
            public string GameStoresMessage;
        }

        class StoreApi
        {
            [JsonProperty("Использовать магазин?(0-отключено/1-MoscowOVH/2-GameStores)")]
            public int UseStore = 0;

            [JsonProperty("Настройка  GameStore(Если вы используете GameStore)")]
            public GameStores GameStore;
        }
        
        static Configuration config = new Configuration();

        class Configuration
        {
            [JsonProperty("Валюты")] 
            public List<Money> Currencies;
            [JsonProperty("Настройка  валют")]
            public Dictionary<string, FarmPoints> Settings;

            [JsonProperty("Настройка  rates")]
            public Dictionary<string, double> Rates;

            
            [JsonProperty("Использовать GUI(true/false)")]
            public bool UseUI = true;
            
            [JsonProperty("Настройка  MoscowOVH/GameStore")]
            public Dictionary<string, FarmPoints> GameStore;
            


            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    Currencies = new List<Money>
                    {
                        new Money
                        {
                            Name = "Basic",
                            Negative = false,
                            Max = 10000,
                            StartAmount = 1000,
                            Vitrual = true,
                            Img = "https://gspics.org/images/2022/06/13/0nevXR.png",
                            SkinId = 0,
                            WipeOnNewSave = true
                        },
                        new Money
                        {
                            Name = "Additional",
                            Negative = true,
                            Max = 10000,
                            StartAmount = 1000,
                            Vitrual = true,
                            Img = "https://i.ibb.co/qJkjy4t/testLogo.png",
                            SkinId = 0,
                            WipeOnNewSave = true
                        }
                        
                    },
                    Rates = new Dictionary<string, double>
                    {
                        ["default"] = 1.0,
                        ["vip"] = 2.0
                    },
                    Settings = new Dictionary<string, FarmPoints>
                    {
                        ["Basic"] = new FarmPoints
                        {
                            KP = new KillPoints
                            {
                                Animal = 1,
                                Npc = 2,
                                Player = 3
                            },
                            Ore = new OreGather
                            {
                                MetalOreBonus = 1,
                                StoneOreBonus =  1,
                                SulfurOreBonus = 3,
                                MetalOreGather = 1,
                                StoneOreGather =  1,
                                SulfurOreGather = 3,
                            },
                            Wood = new WoodGather
                            {
                                Bonus = 1,
                                Gather = 2
                            },
                            TimePoints = new TimePoints()
                        },
                        ["Additional"] = new FarmPoints
                        {
                            KP = new KillPoints
                            {
                                Animal = 1,
                                Npc = 2,
                                Player = 3
                            },
                            Ore = new OreGather
                            {
                                MetalOreBonus = 1,
                                StoneOreBonus =  1,
                                SulfurOreBonus = 3,
                                MetalOreGather = 1,
                                StoneOreGather =  1,
                                SulfurOreGather = 3,
                            },
                            Wood = new WoodGather
                            {
                                Bonus = 1,
                                Gather = 2
                            },
                            TimePoints = new TimePoints()
                        }
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
                if (config == null) PrintWarning("NULL");
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintError($"Не удалось найти конфигурацию 'oxide/config/{Name}', Создание конфига!!");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region data
        Dictionary<ulong,double>_playerRates = new Dictionary<ulong, double>();

        Dictionary<string,Dictionary<ulong,double>>_database = new Dictionary<string, Dictionary<ulong, double>>();
        
        List<string>_currencies = new List<string>();

        void CalcPlayerRates(BasePlayer player)
        {
            if (_playerRates.ContainsKey(player.userID)) return;

            double rate = 1.0;
            foreach (var perm in config.Rates)
                if (permission.UserHasPermission(player.UserIDString, "economics." + perm.Key) && perm.Value > rate)
                    rate = perm.Value;
            _playerRates.Add(player.userID,rate);
            
            PrintWarning($"Setting player {player.displayName} rate to {rate}");
        }
        void GetCurrencies()
        {
            foreach (var var in config.Currencies)
            {
                _currencies.Add(var.Key);
                PrintWarning($"Валюта {var.Name} добавлена в список валют");
            }
        }

        string GetNameByKey(string key)
        {
            foreach (var currency in config.Currencies)
            {
                if (currency.Key == key) return currency.Name;
            }

            return GetNameByKey(_defaultCurrency);
        }

        void GetNotExistCurrencies(List<string>currency)
        {
            foreach (var cur in currency)
            {
                if (!Interface.Oxide.DataFileSystem.ExistsDatafile($"Economics/{cur}"))
                {
                    PrintWarning($"Не найдены данные валюты {cur}");
                    CreateCurrencyData(cur);
                }
            }
        }

        void CreateLocalDataBase()
        {
            foreach (var var in config.Currencies)
            {
                _database.Add(var.Key,new Dictionary<ulong, double>());
                _database[var.Key] =
                    Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, double>>($"Economics/{var.Key}");
                PrintWarning($"Данные валюты {var.Name} загружены");
            }
        }

        void CreatePlayerData(BasePlayer player, string currency)
        {
            ulong playerid = player.userID;
            Money cur = GetCurrnecyFromName(currency);
            if (!_database[currency].ContainsKey(playerid))
            {
                _database[currency].Add(playerid,cur.StartAmount);
                PrintWarning($"Добавлен игрок в валюту {currency} со стартовым значением {cur.StartAmount}");
            }
        }

        Money GetCurrnecyFromName(string name)
        {
            Money currency = config.Currencies.Find(p => p.Key == name);
            return currency;
        }

        void CreateCurrencyData(string name)
        {
            Interface.Oxide.DataFileSystem.WriteObject($"Economics/{name}",new Dictionary<ulong,double>());
            PrintWarning($"Создан файл для {name}");
        }

        

        string PathToName(string path)
        {
            string filename = path.Split('\\')[path.Split('\\').Length - 1];
            string[] cur = filename.Split('.');
            return cur[0];
        }

        void SaveDataBase()
        {
            foreach (var database in _database)
            {
                Interface.Oxide.DataFileSystem.WriteObject($"Economics/{database.Key}",database.Value);
                PrintWarning($"Данные {database.Key} сохранены!");
            }
        }
        
        #endregion

        #region hooks

        #region PlayerTime

        class TimeBonus : FacepunchBehaviour
        {
            ulong playerID;
            
            Dictionary<string,TimeTrack>MoneyTime = new Dictionary<string, TimeTrack>();

            class TimeTrack
            {
                public int Current = 0;
                
                public int Max = Int32.MaxValue;

                public double Amount = 0;
            }

            void Awake()
            {
                try
                {
                    playerID = GetComponent<BasePlayer>().userID;
                }
                catch (Exception e)
                {
                    Console.WriteLine("No Player Finded"+e);
                    throw;
                }
                
                if (playerID == null)
                {
                    Destroy(this);
                }
                
                InitMoney();
            }

            void InitMoney()
            {
                foreach (var money in config.Settings) InvokeRepeating((() => {AddMoney(money.Key,money.Value.TimePoints.Amount);}),10,money.Value.TimePoints.Time);
            }

            void CheckTime(double a = 0)
            {
                foreach (var currency in MoneyTime)
                {
                    currency.Value.Current += 1;
                    if (currency.Value.Current >= currency.Value.Max) AddMoney(currency.Key,currency.Value.Amount);
                }
            }
            private void AddMoney(string name,double amount)
            {
                _.Deposit(playerID.ToString(), amount*_._playerRates[playerID],name);
                
            }
        }
        
        
        
        #endregion

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player.GetComponent<TimeBonus>() != null)
            {
                PrintWarning("Destroying timer component");
                UnityEngine.Object.Destroy(player.GetComponent<TimeBonus>());
            }
            if (player.GetComponent<TimeBonus>() != null) UnityEngine.Object.DestroyImmediate(player.GetComponent<TimeBonus>());
        }

        string ModifyName(string name, double amount)
        {
            if (amount == 1)
            {
                return name;
            }

            if (amount.ToString().ToCharArray()[amount.ToString().ToCharArray().Length-1] == '4')
            {
                name += "а";
                return name;
            }
            else
            {
                return name + "";
            }
        }
        void NoticePlayer(BasePlayer player,double amount,string currency = null)
        {
            string text;
            string title;			
            if (amount<1) return;
                if (currency == null)
            {
               text = $"Вы получили {amount} {ModifyName(GetNameByKey(_defaultCurrency),amount)}";
               title = GetNameByKey(_defaultCurrency)+"ы";			   
            }
            else
            {
                text = $"Вы получили {amount} {ModifyName(GetNameByKey(currency),amount)}";
                title = GetNameByKey(currency)+"";				
            }
            BroadcastSystem.Call("SendCustonNotification",player,title,text,"assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab",3f);			
            /*player.SendConsoleCommand("gametip.hidegametip");
            player.SendConsoleCommand("gametip.showgametip", text);
            timer.Once(3f, () => player?.SendConsoleCommand("gametip.hidegametip"));*/
        }

        void OnServerInitialized()
        {
            _ = this;
            GetCurrencies();
            GetNotExistCurrencies(_currencies);
            CreateLocalDataBase();
            
            foreach (var player in BasePlayer.activePlayerList.ToList())
            {
                if (player == null) continue;
                OnPlayerConnected(player);
            }

            foreach (var perm in config.Rates.Keys) permission.RegisterPermission("economics."+perm,this);
            _defaultCurrency = config.Currencies.First().Key;
            PrintWarning($"Валюта по умолчанию: {_defaultCurrency}");
            
            timer.Once(1f, InitImages);

        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player.GetComponent<TimeBonus>() == null)
            {
                player.gameObject.AddComponent<TimeBonus>();
            }
            foreach (var var in config.Currencies)
            {
                CreatePlayerData(player,var.Key);
            }
            CalcPlayerRates(player);
        }

        void DestroyTimeBonusObjects()
        {
            foreach (var obj in UnityEngine.Object.FindObjectsOfType<TimeBonus>()) UnityEngine.Object.DestroyImmediate(obj);
        }
        void Unload()
        {
            DestroyTimeBonusObjects();
            SaveDataBase();
        }

        void InitImages()
        {
            foreach (var currency in config.Currencies)
            {
                ImageLibrary.Call("AddImage", currency.Img, currency.Key);
                PrintWarning($"Image - {currency.Name} loaded");
            }
        }
        
        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info== null) return;
                if (info.InitiatorPlayer == null || info.InitiatorPlayer.userID == 0) return;
                
            
                BasePlayer player = info.InitiatorPlayer;
            if ( entity is ScientistNPC)
                foreach (var money in config.Currencies)
                {
                    Deposit(player.UserIDString, config.Settings[money.Key].KP.Npc*_playerRates[player.userID], money.Key);
                    return;
                }
            if ( entity is BaseAnimalNPC)
                foreach (var money in config.Currencies)
                {
                    Deposit(player.UserIDString, config.Settings[money.Key].KP.Animal*_playerRates[player.userID], money.Key);
                    return;
                }

            if ( entity is BradleyAPC)
                foreach (var money in config.Currencies)
                {
                    Deposit(player.UserIDString, config.Settings[money.Key].KP.Tank*_playerRates[player.userID], money.Key);
                    return;
                }
            if ( entity is BaseHelicopter)
                foreach (var money in config.Currencies)
                {
                    Deposit(player.UserIDString, config.Settings[money.Key].KP.Tank*_playerRates[player.userID], money.Key);
                    return;
                }
            if ( entity is BasePlayer && !(entity is ScientistNPC)&& !(entity is NPCDwelling)&& !(entity is BanditGuard)&& !(entity is ScarecrowNPC) && entity.ToPlayer().userID != player.userID)
                foreach (var money in config.Currencies)
                {
                    Deposit(player.UserIDString, config.Settings[money.Key].KP.Player*_playerRates[player.userID], money.Key);
                    return;
                }


        }

        #endregion

        #region Point Farm Hooks
        
        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (!(entity is BasePlayer)) return;
            BasePlayer player = entity as BasePlayer;
            switch (item.info.shortname)
            {
                case "stones":
                    foreach (var var in config.Settings)
                    {
                        Deposit(player.UserIDString, var.Value.Ore.StoneOreGather*_playerRates[player.userID],var.Key);
                    }
                    break;
                case "sulfur.ore":
                    foreach (var var in config.Settings)
                    {
                        Deposit(player.UserIDString, var.Value.Ore.SulfurOreGather*_playerRates[player.userID],var.Key);
                    }
                    break;
                case "metal.ore":
                    foreach (var var in config.Settings)
                    {
                        Deposit(player.UserIDString, var.Value.Ore.MetalOreGather*_playerRates[player.userID],var.Key);
                    }
                    break;
                case "wood":
                    foreach (var var in config.Settings)
                    {
                        Deposit(player.UserIDString, var.Value.Wood.Gather*_playerRates[player.userID],var.Key);
                    }
                    break;
            }
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            switch (item.info.shortname)
            {
                case "stones":
                    foreach (var var in config.Settings)
                    {
                        Deposit(player.UserIDString, var.Value.Ore.StoneOreBonus*_playerRates[player.userID],var.Key);
                    }
                    break;
                case "sulfur.ore":
                    foreach (var var in config.Settings)
                    {
                        Deposit(player.UserIDString, var.Value.Ore.SulfurOreBonus*_playerRates[player.userID],var.Key);
                    }
                    break;
                case "hq.metal.ore":
                    foreach (var var in config.Settings)
                    {
                        Deposit(player.UserIDString, var.Value.Ore.MetalOreBonus*_playerRates[player.userID],var.Key);
                    }
                    break;
                case "wood":
                    foreach (var var in config.Settings)
                    {
                        Deposit(player.UserIDString, var.Value.Wood.Bonus*_playerRates[player.userID],var.Key);
                    }
                    break;
            }
        }

        #endregion

        #region API

        private double Balance(ulong playerId) => Balance(playerId.ToString());
        private double Balance(string playerId)
        {
            string currency = _defaultCurrency;
            if (!_database[currency].ContainsKey(Convert.ToUInt64(playerId)))
            {
                PrintWarning("Игрок не найден");
                return 0;
            }
            double balance = _database[currency][Convert.ToUInt64(playerId)];
            return balance;
        }

        private double Balance(string playerId,string currency)
        {
            if (!_database[currency].ContainsKey(Convert.ToUInt64(playerId)))
            {
                PrintWarning("Игрок не найден");
                return 0;
            }

            double balance = _database[currency][Convert.ToUInt64(playerId)];
            return balance;
        }

        private bool SetBalance(ulong playerId, double amount) => SetBalance(playerId.ToString(), amount);
        private bool SetBalance(string playerId, double amount)
        {
            string currency = _defaultCurrency;
            if (_database[currency].ContainsKey(Convert.ToUInt64(playerId)))
            {
                _database[currency][Convert.ToUInt64(playerId)] = amount;
                PrintWarning($"Баланс {currency} игрока {BasePlayer.FindByID(Convert.ToUInt64(playerId))} изменен на {amount}");
                return true;
            }
            else
            {
                PrintError($"Игрок {playerId} не найден!");
                return false;
            }
        }

        bool SetBalance(ulong playerId, double amount, string currency) =>
            SetBalance(playerId.ToString(), amount, currency);
        private bool SetBalance(string playerId, double amount,string currency)
        {
            if (!_database.ContainsKey(currency))
            {
                PrintError("Валюта не найдена");
                return false;
            }
            if (_database[currency].ContainsKey(Convert.ToUInt64(playerId)))
            {
                _database[currency][Convert.ToUInt64(playerId)] = amount;
                return true;
            }
            else
            {
                PrintError($"Игрок {playerId} не найден!");
                return false;
            }
            
        }

        private bool Deposit(string playerId, double amount, string currency)
        {
            BasePlayer player = BasePlayer.Find(playerId);
            if (player == null) return false;
            NoticePlayer(player,amount,currency);
                LogDeposit(playerId,amount,currency);
            return amount > 0 && SetBalance(playerId, amount + Balance(playerId),currency);
            
        }
        private bool Deposit(string playerId, double amount)
        {
            BasePlayer player = BasePlayer.Find(playerId);
            if (player == null) return false;
            NoticePlayer(player,amount);
            string currency = _defaultCurrency;
            LogDeposit(playerId,amount);
            return amount > 0 && SetBalance(playerId, amount + Balance(playerId),currency);
        }
        private bool Withdraw(ulong playerId, double amount) => Withdraw(playerId.ToString(), amount);
        private bool Withdraw(string playerId, double amount)
        {
            Money cur = config.Currencies.Find(p => p.Key == _defaultCurrency);
            if (cur == null)
            {
                PrintError($"Erorr to find currency - {_defaultCurrency}");
                return false;
            }
            if (amount >= 0 || cur.Negative)
            {
                double balance = Balance(playerId);
                return (balance >= amount || cur.Negative) && SetBalance(playerId, balance - amount);
            }

            return true;
        }
        private bool Withdraw(string playerId, double amount, string currency)
        {
            Money cur = config.Currencies.Find(p => p.Key == currency);
            if (cur == null)
            {
                PrintError($"Erorr to find currency - {currency}");
                return false;
            }
            if (amount >= 0 || cur.Negative)
            {
                double balance = Balance(playerId);
                return (balance >= amount || cur.Negative) && SetBalance(playerId, balance - amount);
            }

            return true;
        }

        
        string GetCurrencyURL(string name)
        {
            return config.Currencies.Find(p => p.Key == name).Img;
        }

        #region MoscowOvhApi

        bool OvhInitialized()
        {
            if (RustStore == null)
            {
                PrintError("MoscowOvh не подключен");
            }

            bool isInit = (bool) RustStore.CallHook("APIIsInitialized");
            if (isInit == null || isInit == false)
            {
                PrintError("MoscowOvh не подключен или настроен неверно");
                return false;
            }

            return true;
        }

        #endregion

        #endregion

        #region UI

        private string _layer = "BalanceUI";
        void BalanceUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, _layer);
            if (!config.UseUI)
            {
                return;
            }
            var container = new CuiElementContainer();
            int index = 1;
            int y = 40;
            
            container.Add(new CuiPanel
            {
                Image = {Color = "0 0 0 0.3"},
                RectTransform =
                {
                    AnchorMin = "1 1", AnchorMax = "1 1", OffsetMin = $"-120 {-50*config.Currencies.Count-5}", OffsetMax = $"-5 -5"
                }
            }, "Overlay", _layer);
            foreach (var money in config.Currencies)
            {
                container.Add(new CuiPanel
                {
                    Image = {Color = "0 0 0 0"},
                    RectTransform = {AnchorMin = "1 1",AnchorMax = "1 1",OffsetMin = $"-120 {-50*index}",OffsetMax = $"-5 {-50*index+50}"}
                }, _layer,_layer + $".{index}");
                container.Add(new CuiElement
                {
                    Parent = _layer + $".{index}",
                    Name = _layer + $".{index}"+".Img",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = (string) ImageLibrary.Call("GetImage",money.Img)
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "0 0",OffsetMax = "50 50"
                        }
                    }
                });
                container.Add(new CuiLabel
                {
                    Text = {Align = TextAnchor.MiddleLeft,Text = $": {_database[money.Key][player.userID]}",FontSize = 18},
                    RectTransform = {AnchorMin = "0 0",AnchorMax = "0 0",OffsetMin = "51 0",OffsetMax = "99 50"}
                }, _layer + $".{index}", _layer + $".{index}" + ".Money");
                index++;
            }

            CuiHelper.AddUi(player, container);
        }
        
        void OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            CuiHelper.DestroyUi(player, _layer);
        }
        
        void OnPlayerRespawned(BasePlayer player)
        {
            BalanceUI(player);
        }

        #endregion

        #region commands

        [ChatCommand("balance")]
        void ShowPlayerBalance(BasePlayer player, string command, string[] args)
        {
            SendReply(player,Balance(player.UserIDString).ToString());
        }

        #region admin

        [ChatCommand("eco")]
        void AdminEcoCommand(BasePlayer player, string command, string[] args)
        {
            PrintWarning("Admin Command Run");
            if (!player.IsAdmin)
            {
                SendReply(player,$"Not Exist Command!");
                return;
            }
            string text = $"Admin {player} ";
            if (args.Length < 2)
            {
                SendReply(player,$"Wrong Command\n" +
                                 $"Command list:" +
                                 $"/eco set (playerid/name) (amount) - to change default balance\n" +
                                 $"/eco set (playerid/name) (amount) (currency) - to change currency balance\n" +
                                 $"/eco setme (amount) (currency) - to change your currency balance\n" +
                                 $"/eco give (playerid/name) (amount) - give balance(default) for player\n" +
                                 $"/eco give (playerid/name) (amount) (currency) - give balance for player\n" +
                                 $"/eco giveme (amount) (currency) - give balance for player\n" +
                                 $"/eco withdraw (playerid/name) (amount) - withdraw player balance(default)\n" +
                                 $"/eco withdraw (playerid/name) (amount) (currency) - withdraw player balance\n" +
                                 $"/eco withdrawme (amount) (currency) - withdraw your balance \n");
            }

            BasePlayer target = BasePlayer.Find(args[1]);
            switch (args[0])
            {
                case "set":
                    if (args.Length<3)
                    {
                        SendReply(player,$"Wrong command");
                        return;
                    }
                    
                    if (args.Length<4)
                    {
                        double amount = Convert.ToDouble(args[2]);
                        text += "set ";
                        text += args[0];
                        text += " ";
                        text += args[1];
                        text += " ";
                        text += args[2];
                        LogAdminCommand(player,text);
                        SetBalance(target.UserIDString, amount);
                        return;
                    }

                    if (args.Length >= 4)
                    {
                        double amount = Convert.ToDouble(args[2]);
                        text += "set ";
                        text += args[0];
                        text += " ";
                        text += args[1];
                        text += " ";
                        text += args[2];
                        text += " ";
                        text += args[3];
                        LogAdminCommand(player,text);
                        SetBalance(target.UserIDString, amount,args[3]);
                        return;
                    }
                break;
                case "setme":
                    if (args.Length<3)
                    {
                        double amount = Convert.ToDouble(args[1]);
                        text += "setme ";
                        text += args[0];
                        text += " ";
                        text += args[1];
                        
                        LogAdminCommand(player,text);
                        SetBalance(player.UserIDString, amount);
                        return;
                    }

                    if (args.Length >= 3)
                    {
                        double amount = Convert.ToDouble(args[1]);
                        text += "setme ";
                        text += args[0];
                        text += " ";
                        text += args[1];
                        text += " ";
                        text += args[2];
                        LogAdminCommand(player,text);
                        SetBalance(player.UserIDString, amount,args[3]);
                        return;
                    }
                    break;
                case "withdraw":
                    
                    if (args.Length<4)
                    {
                        double amount = Convert.ToDouble(args[2]);
                        text += "withdraw ";
                        text += args[0];
                        text += " ";
                        text += args[1];
                        text += " ";
                        text += args[2];
                        LogAdminCommand(player,text);
                        Withdraw(target.UserIDString, amount);
                        return;
                    }

                    if (args.Length >= 4)
                    {
                        double amount = Convert.ToDouble(args[2]);
                        text += "withdraw ";
                        text += args[0];
                        text += " ";
                        text += args[1];
                        text += " ";
                        text += args[2];
                        text += " ";
                        text += args[3];
                        LogAdminCommand(player,text);
                        Withdraw(target.UserIDString, amount,args[3]);
                        return;
                    }
                    break;
                case "withdrawme":
                    
                    if (args.Length<3)
                    {
                        double amount = Convert.ToDouble(args[1]);
                        text += "withdrawme ";
                        text += args[0];
                        text += " ";
                        text += args[1];
                        
                        LogAdminCommand(player,text);
                        Withdraw(player.UserIDString, amount);
                        return;
                    }

                    if (args.Length >= 3)
                    {
                        double amount = Convert.ToDouble(args[1]);
                        text += "withdrawme ";
                        text += args[0];
                        text += " ";
                        text += args[1];
                        text += " ";
                        text += args[2];
                        LogAdminCommand(player,text);
                        Withdraw(player.UserIDString, amount,args[2]);
                        return;
                    }
                    break;
                case "give":
                    if (args.Length<3)
                    {
                        SendReply(player,$"Wrong command");
                        return;
                    }
                    
                    if (args.Length<4)
                    {
                        double amount = Convert.ToDouble(args[2]);
                        text += "give ";
                        text += args[0];
                        text += " ";
                        text += args[1];
                        text += " ";
                        text += args[2];
                        LogAdminCommand(player,text);
                        Deposit(target.UserIDString, amount);
                        return;
                    }

                    if (args.Length >= 4)
                    {
                        double amount = Convert.ToDouble(args[2]);
                        text += "give ";
                        text += args[0];
                        text += " ";
                        text += args[1];
                        text += " ";
                        text += args[2];
                        text += " ";
                        text += args[3];
                        LogAdminCommand(player,text);
                        Deposit(target.UserIDString, amount,args[3]);
                        return;
                    }
                    break;
                case "giveme":
                    
                    if (args.Length<3)
                    {
                        double amount = Convert.ToDouble(args[1]);
                        text += "giveme ";
                        text += args[0];
                        text += " ";
                        text += args[1];
                        
                        LogAdminCommand(player,text);
                        Deposit(player.UserIDString, amount);
                        return;
                    }

                    if (args.Length >= 3)
                    {
                        double amount = Convert.ToDouble(args[1]);
                        text += "giveme ";
                        text += args[0];
                        text += " ";
                        text += args[1];
                        text += " ";
                        text += args[2];
                        LogAdminCommand(player,text);
                        Deposit(player.UserIDString, amount,args[2]);
                        return;
                    }
                    break;
                case "giveall":
                    if (args.Length == 2)
                    {
                        double amount = Convert.ToDouble(args[1]);
                        text += "giveall ";
                        //text += args[0];
                        text += " ";
                        text += args[1];
                        
                        LogAdminCommand(player,text);
                        foreach (var targets in BasePlayer.activePlayerList)
                        {
                            Deposit(targets.UserIDString, amount);
                        }
                        //Deposit(player.UserIDString, amount);
                    }
                    break;

            }
        }

        [ConsoleCommand("ebalanceservadd")]
        void ServerAddBalance(ConsoleSystem.Arg arg)
        {
            if (!arg.IsServerside) return;
            if (arg.HasArgs(3))
            {
                string useid = arg.Args[0];
                int amount = arg.Args[1].ToInt();
                string currency = arg.Args[2];
                Deposit(useid, amount,currency);
                PrintWarning($"SERVER ADD BALANCE TO {useid}");
                return;
                
            }
            if (arg.HasArgs(2))
            {
                int amount = arg.Args[1].ToInt();
                string useid = arg.Args[0];
                Deposit(useid, amount);
                LogServerCommand(useid,amount,_defaultCurrency);
                PrintWarning($"SERVER ADD BALANCE TO {useid}");
                return;
            }
        }

        #endregion
        /*[ChatCommand("setbalance")]
        void SetPlayerBalance(BasePlayer player, string command, string[] args)
        {
            SetBalance(player.UserIDString, Convert.ToDouble(args[0]));
        }*/

        #endregion
        
        #region log

        private string _logFile = "Economics";
        void LogDeposit(string userid, double amount, string currency)
        {
            if (amount != 0)
            {
                LogToFile(_logFile,$"[{DateTime.UtcNow}]Added {amount} to {currency} for {userid}",this);
            }

        }
        
        void LogDeposit(string userid, double amount)
        {
            if (amount != 0)
            {
                LogToFile(_logFile,$"[{DateTime.UtcNow}]Added {amount} to {_defaultCurrency}(default) for {userid}",this);
            }
        }

        void LogSetBalance(string userid, double amount, string currency)
        {
            LogToFile(_logFile,$"[{DateTime.UtcNow}]Set Balnce for {userid} : {amount} to {currency}",this);
        }
        
        void LogSetBalance(string userid, double amount)
        {
            LogToFile(_logFile,$"[{DateTime.UtcNow}]Set Balnce for {userid} : {amount} to {_defaultCurrency}(default)",this);
        }

        void LogWithdraw(string userid, double amount, string currency)
        {
            LogToFile(_logFile,$"[{DateTime.UtcNow}]Withdraw {amount} to {currency} for {userid}",this);
        }
        
        void LogWithdraw(string userid, double amount)
        {
            LogToFile(_logFile,$"[{DateTime.UtcNow}]Withdraw {amount} to {_defaultCurrency}(default) for {userid}",this);
        }

        void LogAdminCommand(BasePlayer player, string text)
        {
            LogToFile(_logFile,$"[{DateTime.UtcNow}]Admin[{player}]: {text}",this);
        }

        void LogServerCommand(string target, int amount,string currency)
        {
            LogToFile(_logFile,$"[{DateTime.UtcNow}]SERVER:Add {amount}x{currency} to {target}",this);
        }

        #endregion
        
    }
}