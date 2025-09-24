using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ATM System", "David", "1.8.3")]
    [Description("A dynamic and comprehensive ATM System")]
    class ATMSystem : CovalencePlugin
    {
        #region References

        [PluginReference]
        private Plugin Economics;

        [PluginReference]
        private Plugin ZoneManager;

        [PluginReference]
        private Plugin MarkerManager;

        #endregion

        #region Initialize

        private List<IPlayer> _hasError = new List<IPlayer>();

        PluginConfig _config;

        private void Init()
        {
            _config = Config.ReadObject<PluginConfig>();
            RegisterPermissions();
            AddCovalenceCommand("spawncardreader", "CreateReader");
            AddCovalenceCommand("setplace", "CreateReader");
            AddCovalenceCommand("setatmzone", "CreateReader");
            AddCovalenceCommand("vmspawn", "CreateReader");
        }

        private void OnServerInitialized()
        {
            SetUpAuth();
            LoadData();
            if (_config._vendingSettings._vendingOwner) preSetMachines();
            timer.Once(240f, () => {
                if (_config._vendingSettings._vendingOwner) preSetMachines();
            });
            
        }

        private static System.Random _random = new System.Random();

        private Dictionary<string, string> _depositWithdrawStore = new Dictionary<string, string>();

        private List<string> _atms = new List<string>(); 

        List<BaseEntity> _entities = new List<BaseEntity>();

        private void RegisterPermissions()
        {
            permission.RegisterPermission($"{Name}.admin", this);
        }

        #endregion

        #region Config

        private DynamicConfigFile data;

        protected override void LoadDefaultConfig() => _config = LoadBaseConfig();

        protected override void SaveConfig() => Config.WriteObject(_config, true);

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<PluginConfig>();

                if (_config == null)
                    throw new JsonException();

                if (_config.Version < Version || _config.Version > Version)
                {
                    LogWarning(GetLang("_configUpdated"));
                    LoadDefaultConfig();
                }
                SaveConfig();
            }

            catch
            {
                LoadDefaultConfig();
                LogWarning(GetLang("_confCorrupt"));
                SaveConfig();
            }
        }

        private void SaveData()
        {
            if (_atms != null)
                Interface.Oxide.DataFileSystem.WriteObject($"{Name}/ATMSystemData", _atms);
        }

        private void LoadData()
        {
            data = Interface.Oxide.DataFileSystem.GetFile($"{Name}/ATMSystemData");
            try
            {
                _atms = data.ReadObject<List<string>>();
            }
            catch
            {
                LogError(GetLang("_ccantLoadData"));
            }
        }

        private PluginConfig LoadBaseConfig()
        {
            return new PluginConfig
            {
                _generalSettings = new ATMSystem.PluginConfig.GeneralSettings
                {
                    _isEnabled = true,
                    _canAnnounce = true,
                    _requireCard = false,
                    _useButton = false,
                    _titleText = "<color=#ce422b>RUST</color> ATM",
                    _defaultInfo = "This panel serves to inform player about functions of ATM machines and ingame currencies. It can be customized inside config file, including font styling",
                    _logWarnings = true,
                },
                _currSettings = new ATMSystem.PluginConfig.CurrencySettings
                {
                    _atmCurrencyId = "-1779183908",
                    _atmCurrencySkin = "2420097877",
                    _atmCurrencyName = "Dollar",
                    _maxDepoLimit = 150000,
                },
                _vendingSettings = new ATMSystem.PluginConfig.VendingSettings
                {
                    _vendSkinid = "2441271366",
                    _vendingOwner = false,
                    _createMarkers = false,
                    _markerColor = "7DD800",
                    _markerSize = 0.2f,
                },
                _cardSettings = new ATMSystem.PluginConfig.CardSettings
                {
                    _atmCardSkin = "2410672337",
                    _cardOnPlayerSpawn = false,
                },
                _chatSettings = new ATMSystem.PluginConfig.ChatSettings
                {
                    _enableChatTag = true,
                    _chatTagColor = "4A95CC",
                    _chatMessageColor = "FFFFFFFF",
                    _enableChatAnnounce = true,
                },
                Version = Version
            };
        }

        class PluginConfig
        {
            [JsonProperty(PropertyName = "General Settings")]
            public GeneralSettings _generalSettings { get; set; }

            [JsonProperty(PropertyName = "Chat Settings")]
            public ChatSettings _chatSettings { get; set; }

            [JsonProperty(PropertyName = "Vending Machine Settings")]
            public VendingSettings _vendingSettings { get; set; }

            [JsonProperty(PropertyName = "Currency Settings")]
            public CurrencySettings _currSettings { get; set; }

            [JsonProperty(PropertyName = "Credit Card Settings")]
            public CardSettings _cardSettings { get; set; }

            public class GeneralSettings
            {
                [JsonProperty("Is Enabled: ")]
                public bool _isEnabled { get; set; }

                [JsonProperty("Can Announce: ")]
                public bool _canAnnounce { get; set; }

                [JsonProperty(PropertyName = "Auth Key")]
                public int _commAuthKey = 0;

                [JsonProperty("Require Card: ")]
                public bool _requireCard { get; set; }

                [JsonProperty("Use Button: ")]
                public bool _useButton { get; set; }

                [JsonProperty("Title Text: ")]
                public string _titleText { get; set; }

                [JsonProperty("Default Bank Information: ")]
                public string _defaultInfo { get; set; }

                [JsonProperty("Log Warnings: ")]
                public bool _logWarnings { get; set; }

            }

            public class ChatSettings
            {
                [JsonProperty("Messages Enabled: ")]
                public bool _enableChatAnnounce { get; set; }

                [JsonProperty("Chat Tag Enabled ")]
                public bool _enableChatTag { get; set; }

                [JsonProperty(PropertyName = "Chat Tag Color: ")]
                public string _chatTagColor { get; set; }

                [JsonProperty(PropertyName = "Chat Message Color: ")]
                public string _chatMessageColor { get; set; }
            }

            public class VendingSettings
            {
                [JsonProperty(PropertyName = "Vending Machine Skin")]
                public string _vendSkinid { get; set; }

                [JsonProperty(PropertyName = "Spawned Vending Machines as ATM")]
                public bool _vendingOwner { get; set; }

                [JsonProperty(PropertyName = "MarkerManager - Create markers at spawned vending machines")]
                public bool _createMarkers { get; set; }

                [JsonProperty(PropertyName = "Marker Outline Color")]
                public string _markerColor { get; set; }

                [JsonProperty(PropertyName = "Marker Outline Size")]
                public float _markerSize { get; set; }



            }

            public class CurrencySettings
            {
                [JsonProperty(PropertyName = "Currency Item ID")]
                public string _atmCurrencyId { get; set; }

                [JsonProperty(PropertyName = "Currency Skin ID")]
                public string _atmCurrencySkin { get; set; }

                [JsonProperty(PropertyName = "Currency Name")]
                public string _atmCurrencyName { get; set; }

                [JsonProperty(PropertyName = "Maximum balance for accounts")]
                public int _maxDepoLimit { get; set; }

            }

            public class CardSettings
            {
                [JsonProperty(PropertyName = "Card Skin ID")]
                public string _atmCardSkin { get; set; }

                [JsonProperty(PropertyName = "Card On Player Spawn")]
                public bool _cardOnPlayerSpawn { get; set; } 

            }

            [JsonProperty(PropertyName = "Version: ")]
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        #endregion

        #region Commands

        private void CreateReader(IPlayer _iplayer, string command, string[] args)
        {   
            var player = BasePlayer.FindByID(Convert.ToUInt64(_iplayer.Id));
            if (!_iplayer.IsAdmin)
            {
                PrivateMessage(_iplayer, GetLang("_noPerm"));
                return;
            }

            switch (command)
            {
                case "spawncardreader":
                    if (args.Length > 0)
                    {
                        PrivateMessage(_iplayer, GetLang("_invalidUsage"));
                        return;
                    }
                    Vector3 _position = player.transform.position + player.eyes.transform.forward * 1.5f;

                    var _reader = GameManager.server.CreateEntity("assets/prefabs/io/electric/switches/cardreader.prefab", _position, Quaternion.Euler(0, 0, 0));
                    var _readerReady = _reader.gameObject.AddComponent<MoveController>();

                    _reader.gameObject.SetActive(true);

                    CardReader _cardReader = null;

                    if (_reader is CardReader)
                        _cardReader = _reader as CardReader;

                    if (_cardReader != null)
                        _cardReader.accessLevel = 2;
                   
                    _reader.name = "ATM";
                    _reader.Spawn();
                    _reader.SetFlag(BaseEntity.Flags.Reserved8, true, false, true);

                    _entities.Add(_reader);
                    _atms.Add(_reader.net.ID.ToString());

                    _reader.UpdateNetworkGroup();
                    _reader.SendNetworkUpdateImmediate();

                    var _getComp = _reader.GetComponent<MoveController>();
                    if (_getComp != null)
                    {
                        _getComp._player = player;
                        _getComp._distance = 1.4f;
                    }

                    SaveData();
                    return;

                case "setplace":
                    if (args.Length > 0)
                    {
                        PrivateMessage(_iplayer, GetLang("_invalidUsage"));
                        return;
                    }

                    int _counter = 0;
                    List<BaseEntity> _clones = new List<BaseEntity>();
                    foreach (BaseEntity _entity in _entities)
                    {
                        var _component = _entity.GetComponent<MoveController>();
                        if (_component != null)
                        {
                            if (_component._player == player)
                            {
                                _component.Deactivate();
                                PrivateMessage(_iplayer, GetLang("_readerPlaced"));
                                _clones.Add(_entity);
                                _counter++;
                                if (_entity.GetComponent<VendingMachine>() && _config._vendingSettings._vendSkinid != "")
                                {
                                    string _SkinIDString = _config._vendingSettings._vendSkinid;
                                    ulong _SkinID = Convert.ToUInt64(_SkinIDString);
                                    _entity.skinID = _SkinID;
                                    _entity.UpdateNetworkGroup();
                                    _entity.SendNetworkUpdateImmediate();
                                }
                            }
                        }
                    }

                    if (_counter > 0)
                    {
                        foreach (BaseEntity _ent  in _clones)
                        {
                            if (_ent != null)
                                if (_entities.Contains(_ent))
                                _entities.Remove(_ent);
                        }
                        return;
                    }

                    PrivateMessage(_iplayer, GetLang("_noReaderToPlace"));
                    return;

                case "setatmzone":

                    if (args.Length < 1)
                    {
                    
                        PrivateMessage(_iplayer, GetLang("_invalidZoneName"));
                        return;
                    }

                    string _spawnItem = args[0];

                    RaycastHit _eyehit;
                    if (Physics.Raycast(player.eyes.HeadRay(), out _eyehit, 10.0f))
                    {
                        string[] _arenaSettings =
                        {
                            "name",
                            _spawnItem,
                            "eject",
                            "false",
                            "radius",
                            "7"
                        };

                        if (_atms.Contains(_spawnItem))
                        {
                            PrivateMessage(_iplayer, GetLang("_zoneAlreadyExists"));
                            return;
                        }

                        if ((bool)ZoneManager.CallHook("CreateOrUpdateZone", _spawnItem, _arenaSettings, _eyehit.point))
                        {
                            _atms.Add(_spawnItem);
                            SaveData();

                            PrivateMessage(_iplayer, GetLang("_zoneCreated"));

                            return;
                        }
                    }
                    else
                    {
                        PrivateMessage(_iplayer, GetLang("_aimCloser"));
                    }

                    break;

                case "vmspawn":
                    if (args.Length > 0)
                    {
                        PrivateMessage(_iplayer, GetLang("_invalidUsage"));
                        return;
                    }

                    Vector3 _boxposition = player.transform.position + player.eyes.transform.forward * 1.5f;

                    var _machine = GameManager.server.CreateEntity("assets/prefabs/deployable/vendingmachine/vendingmachine.deployed.prefab", _boxposition, Quaternion.Euler(0, 0, 0));
                    var _machineReady = _machine.gameObject.AddComponent<MoveController>();

                    VendingMachine _vending = null;

                    if (_machine is VendingMachine)
                        _vending = _machine as VendingMachine;

                    _vending.SendNetworkUpdate();

                    _machine.Spawn();

                    var _moveComp = _machine.GetComponent<MoveController>();
                    if (_moveComp != null)
                    {
                        _moveComp._player = player;
                    }

                    _entities.Add(_machine);
                    _atms.Add(_vending.net.ID.ToString());

                    SaveData();
                    return;


                default:
                    PrivateMessage(_iplayer, GetLang("_invalidUsage"));
                    return;
            }

        }

        [Command("openbank")]
        private void OpenBank(IPlayer _iplayer, string command, string[] args)
        {
            if (!_iplayer.IsAdmin)
            {
                PrivateMessage(_iplayer, GetLang("_noPerm"));
                return;
            }
            var _player = BasePlayer.FindByID(Convert.ToUInt64(_iplayer.Id));
            ATMCUI(_player);
        }

        [Command("setvendingskins")]
        private void setskins(IPlayer _iplayer)
        {
            if (!_iplayer.IsAdmin)
            {
                PrivateMessage(_iplayer, GetLang("_noPerm"));
                return;
            }

            preSetMachines();
        }

        private void preSetMachines()
        {
            if (MarkerManager != null)
            {
                MarkerManager.CallHook("API_RemoveMarker", "ATM");
            }
            foreach (VendingMachine machine in GameObject.FindObjectsOfType(typeof(VendingMachine)))
            {   
                if (machine.OwnerID == 0)
                {   
                    if (machine is NPCVendingMachine)
                    {
                        return;
                    }
                    string _SkinIDString = _config._vendingSettings._vendSkinid;
                    ulong _SkinID = Convert.ToUInt64(_SkinIDString);
                    machine.skinID = _SkinID;
                    machine.shopName = "ATM";                        
				    machine.SendNetworkUpdate();
                    
                    if (MarkerManager != null)
                    {   
                        if (_config._vendingSettings._createMarkers)
                        {   
                            machine.SetFlag(BaseEntity.Flags.Reserved4, false, false);
				            machine.SendNetworkUpdate();
                            var transform = machine.transform;
                            var pos = transform.position;
                            MarkerManager.CallHook("API_CreateMarker", pos, "ATM", 0, 3f, _config._vendingSettings._markerSize, "ATM", _config._vendingSettings._markerColor, "000000");
                        }   
                    }  
                }
            }
        }
        //MarkerManager.CallHook("API_RemoveMarker", "ATM");

        


        
        [Command("atm.withdraw")]
        private void ATMWithdraw(IPlayer player, string command, string[] args)
        {
            if (player == null)
                return;
           
            var _player = BasePlayer.FindByID(Convert.ToUInt64(player.Id));
            if (args.Length < 1)
            {
                return;
            }

            string _getAuth = _config._generalSettings._commAuthKey.ToString();

            if (args[0] != _getAuth)
                return;
            
            if (!_depositWithdrawStore.ContainsKey(_player.UserIDString))
            {
                return;
            }

            int _amount = 0;

            if (!int.TryParse(_depositWithdrawStore[_player.UserIDString], out _amount)) 
                return;

            double _amountDouble = Convert.ToDouble(_amount);

            double _balance = Economics.Call<double>("Balance", _player.UserIDString);

            if (_amountDouble > _balance)
            {
                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.denied.prefab", (BaseEntity)_player, 0U, Vector3.zero, Vector3.zero);

                if (!_hasError.Contains(player))
                    ATMPOPUP(player, GetLang("_insuffBalance"));

                return;
            }

            int _currId = int.Parse(_config._currSettings._atmCurrencyId);
            string _SkinIDString = _config._currSettings._atmCurrencySkin;
            ulong _SkinID = Convert.ToUInt64(_SkinIDString);
            var _currency = ItemManager.CreateByItemID(_currId, _amount, _SkinID);
            _currency.name = _config._currSettings._atmCurrencyName;
            _currency.MarkDirty();
            if (!_player.inventory.GiveItem(_currency))
            {
                _currency.Remove(0);
                if (!_hasError.Contains(player))
                    ATMPOPUP(player, GetLang("_noSpace"));

                return;
            }

            if (Economics.CallHook("Withdraw", _player.UserIDString, _amountDouble) != null)
            {
                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.updated.prefab", (BaseEntity)_player, 0U, Vector3.zero, Vector3.zero);

                double _finalplayerBalance = Economics.Call<double>("Balance", _player.UserIDString);
                int _finalAmount = _player.inventory.GetAmount(_currId);

                var _element = new CuiElementContainer();
                CuiHelper.DestroyUi(_player, "_balance");
                CuiHelper.DestroyUi(_player, "_cash");

                CUIClass.CreateText(ref _element, "_balance", "_balanceBox", _uiColors["fadedWhite"], $"${_finalplayerBalance.ToString()}", 16, "0.04 0", "1 1", TextAnchor.MiddleCenter);
                CUIClass.CreateText(ref _element, "_cash", "_cashBox", _uiColors["fadedWhite"], $"${_finalAmount.ToString()}", 16, "0.04 0", "1 1", TextAnchor.MiddleCenter);
                CuiHelper.AddUi(_player, _element);

                return;
            }
            return;
        }

        [Command("atm.deposit")]
        private void ATMDeposit(IPlayer iPlayer, string command, string[] args)
        {
            if (args.Length < 1)
            {
                return;
            }

            string _getAuth = _config._generalSettings._commAuthKey.ToString();

            var player = BasePlayer.FindByID(Convert.ToUInt64(iPlayer.Id));

            if (args[0] != _getAuth)
            {
                return;
            }

            if (!_depositWithdrawStore.ContainsKey(player.UserIDString))
            {
                return;
            }

            int _cost;

            if (!int.TryParse(_depositWithdrawStore[player.UserIDString], out _cost)) 
                return;
            
            if (_cost > _config._currSettings._maxDepoLimit) 
            {
                ATMPOPUP(iPlayer, GetLang("_maxDepoValue"));
                return;
            }

            double _playerAccount = Economics.Call<double>("Balance", player.UserIDString);
            int _playerBalanceInt = Convert.ToInt32(_playerAccount);
            int _accountLimit = _config._currSettings._maxDepoLimit - _playerBalanceInt;
            
            if (_cost > _accountLimit)
            {
                ATMPOPUP(iPlayer, GetLang("_maxDepoValue"));
                return;
            }
            
            Puts($"{_accountLimit}");
            double _costDouble = Convert.ToDouble(_cost);

            int _itemId = int.Parse(_config._currSettings._atmCurrencyId);
            int _onHand = player.inventory.GetAmount(_itemId);
            int _final = _onHand - _cost;
            if (_final < 0)
            {
                if (!_hasError.Contains(iPlayer))
                    ATMPOPUP(iPlayer, GetLang("_insuffCash"));

                return;
            }

            if (CanTake(player, _itemId, _cost))
            {
                Economics.CallHook("Deposit", player.UserIDString, _costDouble);
                Effect.server.Run("assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab", (BaseEntity)player, 0U, Vector3.zero, Vector3.zero);
               
                double _playerBalance = Economics.Call<double>("Balance", player.UserIDString);
                int amount = player.inventory.GetAmount(_itemId);

                var _element = new CuiElementContainer();
                CuiHelper.DestroyUi(player, "_balance");
                CuiHelper.DestroyUi(player, "_cash");

                CUIClass.CreateText(ref _element, "_balance", "_balanceBox", _uiColors["fadedWhite"], $"${_playerBalance.ToString()}", 16, "0.04 0", "1 1", TextAnchor.MiddleCenter);
                CUIClass.CreateText(ref _element, "_cash", "_cashBox", _uiColors["fadedWhite"], $"${amount.ToString()}", 16, "0.04 0", "1 1", TextAnchor.MiddleCenter);
                CuiHelper.AddUi(player, _element);

                return;
            }

            return;

        }

        [Command("atmui.close")]
        private void ATMUIClose(IPlayer player, string command, string[] args)
        {
            if (args.Length == 0 || args.Length > 1)
                return;

            string _getAuth = _config._generalSettings._commAuthKey.ToString();

            if (args[0] != _getAuth)
            {
                PrivateMessage(player, GetLang("_invalidAuth"));
                return;
            }
            var _bPlayer = BasePlayer.FindByID(Convert.ToUInt64(player.Id));
            ATMKILLCUI(_bPlayer);
        }

        [Command("processtransaction.input")]
        private void ProcessInput(IPlayer iPlayer, string command, string[] args)
        {
            if (args.Length == 0)
                return;

            var _player = BasePlayer.FindByID(Convert.ToUInt64(iPlayer.Id));

            if (args[0] != _config._generalSettings._commAuthKey.ToString())
                return;

            if (args.Length == 1 || args.Length > 2)
            {
                if (_depositWithdrawStore.ContainsKey(_player.UserIDString))
                    _depositWithdrawStore.Remove(_player.UserIDString);

                return;
            }

            int _amount = 0;
            if (!int.TryParse(args[1], out _amount))
            {
                return;
            }

            if (_amount == 0)
            {
                return;
            }

            if (_depositWithdrawStore.ContainsKey(_player.UserIDString))
            {
                _depositWithdrawStore[_player.UserIDString] = _amount.ToString();
            } 
            else
            {
                _depositWithdrawStore.Add(_player.UserIDString, _amount.ToString());
            }
        }

        #endregion

        #region Helpers

        private void SendAnnouncement(string _message)
        {
            if (_message == null)
            {
                LogWarning(GetLang("_pmNull"));
                return;
            }

            foreach (IPlayer _player in players.Connected)
            {
                PrivateMessage(_player, _message);
            }
        }

        private void PrivateMessage(IPlayer _player, string _message)
        {
            if (_message == null)
            {
                LogWarning(GetLang("_pmNull"));
                return;
            }

            foreach (IPlayer _conplayer in players.Connected)
            {
                if (_player.Id == _conplayer.Id)
                {
                    string _tagColor = _config._chatSettings._chatTagColor;
                    string _msgColor = _config._chatSettings._chatMessageColor;

                    string _finalMessage = $"<color=#{_msgColor}>{_message}</color>";

                    if (_config._chatSettings._enableChatTag)
                    {
                        _finalMessage = $"<color=#{_tagColor}>[ATMSystem]</color> <color=#{_msgColor}>{_message}</color>";
                    }

                    _player.Reply(_finalMessage);
                    return;
                }
            }
            return;
        }

        private string GetLang(string _message) => lang.GetMessage(_message, this);

        private bool CanTake(BasePlayer _player, int _itemId, int _amount)
        {
            if (_player.inventory.Take(null, _itemId, _amount) != 0)
            {
                return true;
            }
            return false;
        }

        private void SetUpAuth()
        {
            if (_config._generalSettings._commAuthKey == 0)
            {
                _config._generalSettings._commAuthKey = _random.Next();
                SaveConfig();
            }
        }

        private void GiveCard(BasePlayer _player)
        {
            string _id = "-484206264";
            Item item = ItemManager.Create(FindItem(_id));
            if (item == null || _player == null || !_player.IsConnected)
            {
                LogError(GetLang("_itemNullBase"));
                return;
            }

            ItemContainer itemContainer = null;
            itemContainer = _player.inventory.containerMain;
                    
            item.amount = 1;
            if (!item.MoveToContainer(itemContainer) && !_player.inventory.GiveItem(item))
            {
                item.Remove();
                return;
            }

            ulong skin = Convert.ToUInt64(_config._cardSettings._atmCardSkin);
            item.skin = skin;
            item.name = $"{_player.displayName}'s Bank Card";
            item.MarkDirty();
        }

        private ItemDefinition FindItem(string _idOrName)
        {
            ItemDefinition _itemDef = ItemManager.FindItemDefinition(_idOrName.ToLower());
            if (_itemDef == null)
            {
                int _itemId;
                if (int.TryParse(_idOrName, out _itemId))
                {
                    _itemDef = ItemManager.FindItemDefinition(_itemId);
                }
            }
            return _itemDef;
        }



        #endregion

        #region Hooks
        private void OnEntityTakeDamage(VendingMachine entity, HitInfo info)
		{
			if (_atms.Contains(entity.net.ID.ToString()))
            {
                info.damageTypes.ScaleAll(0);
            }
            if (_config._vendingSettings._vendingOwner)
            {
                if (entity.OwnerID == 0)
                {  
                    info.damageTypes.ScaleAll(0);
                }
            }      
		}

        private object OnRotateVendingMachine(VendingMachine machine, BasePlayer player)
        {   
            if (_atms.Contains(machine.net.ID.ToString()))
            {
                 return false;
            }
            if (_config._vendingSettings._vendingOwner)
            {
                if (machine.OwnerID == 0)
                {   
                    if (machine is NPCVendingMachine)
                    {
                        return null;
                    }
                    return false;
                }
                return null;
            }
            return null;
        }

        private object CanAdministerVending(IPlayer player, VendingMachine machine)
        {
            if (!player.IsAdmin) return false;
            return true;
        }

        /*
        void OnPlayerInput(BasePlayer player, InputState input)
        {   
            if (input.WasJustPressed(BUTTON.DUCK))
            {
                ATMKILLCUI(player);
                //player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
            }
            //var _bPlayer = BasePlayer.FindByID(Convert.ToUInt64(player.Id));
            //_bPlayer.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
        }
        */
        private object CanUseVending(BasePlayer player, VendingMachine vending)
        {   

            var _comp = vending.gameObject.GetComponent<MoveController>();
            if (_comp)
            {
                if (_comp._holding)
                    return false;
            }
            if (_atms.Contains(vending.net.ID.ToString()))
            {
                if (_config._generalSettings._requireCard || _config._generalSettings._useButton)
                    return false;

                ATMKILLCUI(player);
                ATMCUI(player);
                return false;
            }
            if (_config._vendingSettings._vendingOwner)
            {
                if (vending.OwnerID == 0)
                {   
                    if (vending is NPCVendingMachine)
                    {
                        return null;
                    }
                    ATMKILLCUI(player);
                    ATMCUI(player);
                    return false;
                }
                return null;
            }
            return null;
        }

        object OnCardSwipe(CardReader cardReader, Keycard _card, BasePlayer player)
        {
            var _comp = cardReader.gameObject.GetComponent<MoveController>();
            if (_comp)
            {
                if (_comp._holding)
                    return false;
            }

            Item _carditem = _card.GetItem();
            if (_config._generalSettings._requireCard)
            {
                if (_atms.Contains(cardReader.net.ID.ToString()))
                {
                    if (_carditem != null)
                    {
                        if (_carditem.skin != 0)
                        {
                            string _skinString = _config._cardSettings._atmCardSkin;
                            ulong _skin = Convert.ToUInt64(_skinString);
                            if (_carditem.skin == _skin)
                            {
                                ATMCUI(player);
                                return false;
                            }
                        }
                    }
                }
                if (_carditem != null)
                {
                    if (_carditem.skin != 0)
                    {
                        foreach (var _items in _atms)
                        {
                            if (ZoneManager.Call<bool>("IsPlayerInZone", _items.ToString(), player)) 
                            {   
                                ATMCUI(player);
                                return false;
                            }
                        }
                    }
                }
            }

            if (_carditem != null)
            {
                if (_carditem.skin != 0)
                {
                    string _skinString = _config._cardSettings._atmCardSkin;
                    ulong _skin = Convert.ToUInt64(_skinString);
                    if (_carditem.skin == _skin)
                    {
                        return false;
                    }
                }
            }     
            return null;
        }

        void OnUserRespawned(IPlayer player)
        { 
            var _bPlayer = BasePlayer.FindByID(Convert.ToUInt64(player.Id));
                
            if (_config._cardSettings._cardOnPlayerSpawn)
            {
                if (!_config._generalSettings._requireCard && _config._generalSettings._logWarnings)
                    LogError(GetLang("_spawningOnNoCardRequired"));
                
                GiveCard(_bPlayer);
                return;
            }
            return;
        }

        object OnButtonPress(PressButton button, BasePlayer player)
        {
            if (_config._generalSettings._useButton)
            {
                foreach (var _items in _atms)
                {
                    if (ZoneManager.Call<bool>("IsPlayerInZone", _items.ToString(), player)) 
                    {
                        ATMCUI(player);
                        return false;
                    }
                }
                return null;
            }
            return null;
        }

        void Loaded()
        {
            if (Economics == null)
            {
                LogError(GetLang("_noEconomics"));
            }

            if (ZoneManager == null)
            {
                LogError(GetLang("_noZoneManager"));
            }
        }

        void OnServerSave()
        {
            timer.Once(3f, () => {
                SaveData();

            });
        }

        void Unload()
        {
            foreach (BaseEntity _entity in _entities)
            {
                var _component = _entity?.GetComponent<MoveController>();
                if (_component)
                {
                    if (_component._holding)
                    {
                        _component.Deactivate();
                        UnityEngine.Object.Destroy(_entity);
                    }
                    _component.Deactivate();
                }
                _entities.Clear();
            }
            SaveData();
            if (MarkerManager != null)
            { 
                MarkerManager.CallHook("API_RemoveMarker", "ATM");
            }
        }

        #endregion

        #region CUI

        private Dictionary<string, string> _uiColors = new Dictionary<string, string>
        {
            { "green", "0.337 0.424 0.196 1" },
            { "blueButt", "0.2 0.322 0.4 1" },
            { "blueButtText", "0.271 0.647 0.914 1" },
            { "red", "0.631 0.282 0.22 1" },
            { "black", "0.14 0.14 0.14 0.8" },
            { "fadedWhite", "0.843 0.816 0.78 1" },
            { "white", "1 1 1 1" },
            { "greyBlack", "0.14 0.14 0.14 0.95" }, 
            { "grey", "0.25 0.25 0.25 1" }, 
        };

        private void ATMCUI(BasePlayer player)
        {

           
            
            //player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);

            string _displayName = player.displayName;

            if (_displayName.Length > 16)
                _displayName = player.displayName.Substring(0, 15);

            double _finalplayerBalance = Economics.Call<double>("Balance", player.UserIDString);

            int _itemId = int.Parse(_config._currSettings._atmCurrencyId);
            int _finalAmount = player.inventory.GetAmount(_itemId);

            var _element = CUIClass.CreateOverlay("ATM", "0, 0, 0, 0.7", "0 0", "1 1", true);

            //Main
            CUIClass.CreatePanel(ref _element, "ATMBack", "ATM", "0.14 0.14 0.14 0.95", "0.3485 0.677", "0.641 0.7", false);
            CUIClass.CreateText(ref _element, "_information", "ATMBack", _uiColors["fadedWhite"], "INFORMATION:", 11, "0.01 0", "1 1", TextAnchor.MiddleLeft);
            CUIClass.CreatePanel(ref _element, "ATMBackInformationBox", "ATM", _uiColors["grey"], "0.35 0.6", "0.64 0.675", false);
            CUIClass.CreateText(ref _element, "_information", "ATMBackInformationBox", _uiColors["fadedWhite"], _config._generalSettings._defaultInfo, 11, "0.02 0", "0.81 1", TextAnchor.MiddleLeft);
            CUIClass.CreateImage(ref _element, "ATMBackInformationBox", "https://i.ibb.co/X7c0vFq/iconcash.png", "0.83 0.03", "0.99 0.97");
            CUIClass.CreateText(ref _element, "Title", "ATM", _uiColors["white"], $"{_config._generalSettings._titleText}", 70, "0.3485 0.690", "0.7 0.9", TextAnchor.LowerLeft);    

            //Account
            CUIClass.CreatePanel(ref _element, "ATMAccountTop", "ATM", _uiColors["greyBlack"], "0.3485 0.57", "0.494 0.593", false);
            CUIClass.CreateText(ref _element, "_accountText", "ATMAccountTop", "0.843 0.816 0.78 1", "ACCOUNT", 11, "0.02 0", "1 1", TextAnchor.MiddleLeft);
            CUIClass.CreatePanel(ref _element, "ATMAccountBox", "ATM", _uiColors["grey"], "0.35 0.35", "0.493 0.57", false);

            //Holder
            CUIClass.CreateText(ref _element, "_holderText", "ATMAccountBox", _uiColors["fadedWhite"], "HOLDER:", 11, "0.08 0.85", "1 0.95", TextAnchor.MiddleLeft);
            CUIClass.CreatePanel(ref _element, "_holderBox", "ATMAccountBox", "0.14 0.14 0.14 0.5", "0.075 0.68", "0.92 0.87", false);
            CUIClass.CreateText(ref _element, "_holderName", "_holderBox", _uiColors["white"], $"{_displayName}", 16, "0 0", "1 1", TextAnchor.MiddleCenter);

            //Balance
            CUIClass.CreateText(ref _element, "_balanceText", "ATMAccountBox", _uiColors["fadedWhite"], "BALANCE:", 11, "0.08 0.57", "1 0.67", TextAnchor.MiddleLeft);
            CUIClass.CreatePanel(ref _element, "_balanceBox", "ATMAccountBox", "0.14 0.14 0.14 0.5", "0.075 0.4", "0.92 0.59", false);
            CUIClass.CreateText(ref _element, "_balance", "_balanceBox", _uiColors["white"], $"${_finalplayerBalance.ToString()}", 16, "0 0", "1 1", TextAnchor.MiddleCenter);

            //Cash
            CUIClass.CreateText(ref _element, "_cashText", "ATMAccountBox", _uiColors["fadedWhite"], "CASH:", 11, "0.08 0.29", "1 0.39", TextAnchor.MiddleLeft);
            CUIClass.CreatePanel(ref _element, "_cashBox", "ATMAccountBox", "0.14 0.14 0.14 0.5", "0.075 0.12", "0.92 0.31", false);
            CUIClass.CreateText(ref _element, "_cash", "_cashBox", _uiColors["white"], $"${_finalAmount.ToString()}", 16, "0 0", "1 1", TextAnchor.MiddleCenter);

            //Withdraw And Deposit
            CUIClass.CreatePanel(ref _element, "WithAndDepoTop", "ATM", _uiColors["greyBlack"], "0.4965 0.57", "0.641 0.593", false);
            CUIClass.CreateText(ref _element, "_withanddepoText", "WithAndDepoTop", "0.843 0.816 0.78 1", "WITHDRAW AND DEPOSIT", 11, "0.03 0", "1 1", TextAnchor.MiddleLeft);
            CUIClass.CreatePanel(ref _element, "WithAndDepoBox", "ATM", _uiColors["grey"], "0.498 0.35", "0.64 0.57", false);

            //Input
            CUIClass.CreateText(ref _element, "_enterAmountText", "WithAndDepoBox", _uiColors["fadedWhite"], "ENTER AMOUNT:", 11, "0.08 0.85", "1 0.95", TextAnchor.MiddleLeft);
            CUIClass.CreatePanel(ref _element, "_amountBox", "WithAndDepoBox", "0.14 0.14 0.14 0.5", "0.08 0.68", "0.92 0.87", false);
            CUIClass.CreateText(ref _element, "_enterAmountText", "_amountBox", _uiColors["fadedWhite"], "$", 14, "0.03 0.03", "1 0.95", TextAnchor.MiddleLeft);
            CUIClass.CreateInput(ref _element, "_ammountInput", "_amountBox", _uiColors["fadedWhite"], 12, "0.1 0", "1 1", "robotocondensed-bold.ttf", $"processtransaction.input {_config._generalSettings._commAuthKey}", TextAnchor.MiddleLeft);

            //Function
            CUIClass.CreateButton(ref _element, "_withdrawButton", "WithAndDepoBox", _uiColors["green"], "WITHDRAW", 16, "0.08 0.37", "0.92 0.59", $"atm.withdraw {_config._generalSettings._commAuthKey}", "", _uiColors["fadedWhite"]);
            CUIClass.CreateButton(ref _element, "_depositButton", "WithAndDepoBox", _uiColors["blueButt"], "DEPOSIT", 16, "0.08 0.12", "0.92 0.33", $"atm.deposit {_config._generalSettings._commAuthKey}", "", _uiColors["blueButtText"]);
            CUIClass.CreateButton(ref _element, "_closeButton", "ATMBack", _uiColors["red"], "X", 9, "0.955 0.1", "0.994 0.86", $"atmui.close {_config._generalSettings._commAuthKey}", "", _uiColors["white"]);

            CuiHelper.AddUi(player, _element);
        }   

        private void ATMKILLCUI(BasePlayer player)
        {
            
            //player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
            CuiHelper.DestroyUi(player, "ATM");
        }

        private void ATMPOPUP(IPlayer _player, string _msg)
        {
            _hasError.Add(_player);
            var _bPlayer = BasePlayer.FindByID(Convert.ToUInt64(_player.Id));
            var _element = new CuiElementContainer();
            CuiHelper.DestroyUi(_bPlayer, "_notifButton");
            CUIClass.CreateButton(ref _element, "_notifButton", "Overlay", _uiColors["red"], _msg, 20, "0.35 0.29", "0.64 0.34", "", "_notifButton", _uiColors["white"]);
            CUIClass.CreateImage(ref _element, "_notifButton", "http://333017_web.fakaheda.eu/warning.png", "0 0", "0.8 1");

            CuiHelper.AddUi(_bPlayer, _element);

            timer.Once(3f, () => {
                CuiHelper.DestroyUi(_bPlayer, "_notifButton");
                _hasError.Remove(_player);
            });
        }

        public class CUIClass
        {
            public static CuiElementContainer CreateOverlay(string _name, string _color, string _anchorMin, string _anchorMax, bool _cursorOn = false, float _fade = 1f)
            {
                var _element = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = { Color = _color, Material = "assets/content/ui/uibackgroundblur.mat", FadeIn = _fade},
                            RectTransform = { AnchorMin = _anchorMin, AnchorMax = _anchorMax },
                            CursorEnabled = _cursorOn
                        },
                        new CuiElement().Parent = "Overlay",
                        _name
                    }
                };
                return _element;
            }

            public static void CreatePanel(ref CuiElementContainer _container, string _name, string _parent, string _color, string _anchorMin, string _anchorMax, bool _cursorOn = false, float _fade = 1f)
            {
                _container.Add(new CuiPanel
                {
                    Image = { Color = _color, Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat", FadeIn = _fade },
                    RectTransform = { AnchorMin = _anchorMin, AnchorMax = _anchorMax },
                    CursorEnabled = _cursorOn
                },
                _parent,
                _name);
            }

            public static void CreateImage(ref CuiElementContainer _container, string _parent, string _image, string _anchorMin, string _anchorMax, float _fade = 1f)
            {
                if (_image.StartsWith("http") || _image.StartsWith("www"))
                {
                    _container.Add(new CuiElement
                    {
                        Parent = _parent,
                        Components =
                        {
                            new CuiRawImageComponent { Url = _image, Sprite = "assets/content/textures/generic/fulltransparent.tga", FadeIn = _fade},
                            new CuiRectTransformComponent { AnchorMin = _anchorMin, AnchorMax = _anchorMax }
                        }
                    });
                }
                else
                {
                    _container.Add(new CuiElement
                    {
                        Parent = _parent,
                        Components =
                        {
                            new CuiRawImageComponent { Png = _image, Sprite = "assets/content/textures/generic/fulltransparent.tga", FadeIn = _fade},
                            new CuiRectTransformComponent { AnchorMin = _anchorMin, AnchorMax = _anchorMax }
                        }
                    });
                }
            }

            public static void CreateInput(ref CuiElementContainer _container, string _name, string _parent, string _color, int _size, string _anchorMin, string _anchorMax, string _font = "robotocondensed-bold.ttf", string _command = "command.processinput", TextAnchor _align = TextAnchor.MiddleCenter)
            {
                _container.Add(new CuiElement
                {
                    Parent = _parent,
                    Name = _name,

                    Components =
                    {
                        new CuiInputFieldComponent
                        {

                            Text = "0",
                            CharsLimit = 11,
                            Color = _color,
                            IsPassword = false,
                            Command = _command,
                            Font = _font,
                            FontSize = _size,
                            Align = _align
                        },

                        new CuiRectTransformComponent
                        {
                            AnchorMin = _anchorMin,
                            AnchorMax = _anchorMax

                        }

                    },
                });
            }

            public static void CreateText(ref CuiElementContainer _container, string _name, string _parent, string _color, string _text, int _size, string _anchorMin, string _anchorMax, TextAnchor _align = TextAnchor.MiddleCenter, string _font = "robotocondensed-bold.ttf")
            {
                _container.Add(new CuiElement
                {
                    Parent = _parent,
                    Name = _name,
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = _text,
                            FontSize = _size,
                            Font = _font,
                            Align = _align,
                            Color = _color,
                            FadeIn = 1f,
                        },

                        new CuiRectTransformComponent
                        {
                             AnchorMin = _anchorMin,
                             AnchorMax = _anchorMax
                        }
                    },
                });
            }

            public static void CreateButton(ref CuiElementContainer _container, string _name, string _parent, string _color, string _text, int _size, string _anchorMin, string _anchorMax, string _command = "", string _close = "", string _textColor = "0.843 0.816 0.78 1", TextAnchor _align = TextAnchor.MiddleCenter)
            {
                _container.Add(new CuiButton
                {
                    Button = { Close = _close, Command = _command, Color = _color, FadeIn = 1f},
                    RectTransform = { AnchorMin = _anchorMin, AnchorMax = _anchorMax },
                    Text = { Text = _text, FontSize = _size, Align = _align, Color = _textColor, FadeIn = 1f}
                },
                _parent,
                _name);
            }

        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["_noPerm"] = "You lack the Permission to do this ",
                ["_confCorrupt"] = "Config Is Corrupt ",
                ["_configUpdated"] = "Different Version Detected, Updating Config ",
                ["_pmNull"] = "Private Message null ",
                ["_atmPlaced"] = "Placed ",
                ["_insuffCash"] = "Insufficient Cash",
                ["_maxDepoValue"] = "Deposit Value Limit",
                ["_insuffBalance"] = "Insufficient Balance ",
                ["_noSpace"] = "No space To Give Item ",
                ["_aimCloser"] = "Aim at the floor, closer to yourself ",
                ["_zoneCreated"] = "New Zone Created ",
                ["_zoneAlreadyExists"] = "Zone Already Exists ",
                ["_invalidZoneName"] = "Invalid Zone Name ",
                ["_noReaderToPlace"] = "No Reader To Place ",
                ["_readerPlaced"] = "Reader Has Been Placed",
                ["_invalidUsage"] = "Invalid Usage ",
                ["_entervalidNumb"] = "Enter a Valid Number ",
                ["_giveItemNull"] = "Item Null, Contact Developer if error is from unmodified version",
                ["_noEconomics"] = "Wulfs Economics Not Detected, It is required for this plugin",
                ["_noZoneManager"] = "ZoneManager Not Detected, /setatmzone command won't work. ",
                ["_invalidAuth"] = "Invalid Auth Key",
                ["_spawningOnNoCardRequired"] = "Give card on Respawn Enabled, but not required for ATM usage. If this is intentional, you can disable this message in General Settings, changing Log Warnings to false",
                ["_cantLoadData"] = "Couldn't Load Data... Ignore if fresh install",

            }, this);
        }

        #endregion Localization

        #region Behaviour

        public class MoveController : MonoBehaviour
        {
            public float _distance = 2.2f;
            public BasePlayer _player;
            BaseEntity _entity;
            public bool _holding = false;
         
            void Start()
            {
                _entity = gameObject.GetComponent<BaseEntity>();
                Activate();
            }

            void Update()
            {
                if (_holding)
                {
                    if (_entity == null || _player == null)
                        return;

                    _entity.transform.position = _player.transform.position + _player.eyes.BodyRay().direction * _distance;
                    _entity.transform.LookAt(_player.transform.position);
                    _entity.UpdateNetworkGroup();
                    _entity.SendNetworkUpdateImmediate();
                }
            }

            public void Activate()
            {
                if (_entity == null || _player == null)
                    return;

                _entity.transform.position = _player.transform.position + _player.eyes.BodyRay().direction * _distance;
                _entity.transform.LookAt(_player.transform.position);
                _entity.UpdateNetworkGroup();
                _entity.SendNetworkUpdateImmediate();

                _holding = true;
            }

            public void Deactivate()
            {
                _holding = false;
                _entity = null;
                _player = null;
                UnityEngine.Object.Destroy(this);
            }
        }
        #endregion

    }
}