// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("vDelivery", "IIIaKa", "0.1.1")]
    [Description("Drones deliver right into your vending machine.")]
    class vDelivery : RustPlugin
    {
        [PluginReference]
        private Plugin ImageLibrary, CustomVendingSetup;
        
        #region ~Variables~
        private bool _serverLoaded = false;
        private int _imgLibCheck = 0;
        private bool _imgLibIsLoaded = false;
        private Dictionary<ulong, string> _playerUI = new Dictionary<ulong, string>();
        private Dictionary<ulong, Timer> _playerTimers = new Dictionary<ulong, Timer>();
        private const string PERMISSION_ONE = "vDelivery.one";
        private const string PERMISSION_TWO = "vDelivery.two";
        private const string PERMISSION_THREE = "vDelivery.three";
        private const string PERMISSION_ADMIN = "vDelivery.admin";
        private const string marketplacePrefab = "assets/prefabs/misc/marketplace/marketplace.prefab";
        private const string marketplaceTerminalPrefab = "assets/prefabs/misc/marketplace/marketterminal.prefab";
        private const string itemDropPrefab = "assets/prefabs/misc/item drop/item_drop.prefab";

        private readonly Dictionary<string, int> DefaultLimitsPerGroup = new Dictionary<string, int>
        {
            { PERMISSION_ONE, 1 },
            { PERMISSION_TWO, 3 },
            { PERMISSION_THREE, 5 }
        };
        private readonly List<FeeGroupConfiguration> DefaultFeesPerGroup = new List<FeeGroupConfiguration> { new FeeGroupConfiguration(PERMISSION_ONE), new FeeGroupConfiguration(PERMISSION_TWO), new FeeGroupConfiguration(PERMISSION_THREE) };
        
        private const string UI_HUD_Name = "vDelivery_HUD";
        #endregion

        #region ~Configuration~
        private static Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "ImageLibrary Counter Check")]
            public int ImgLibCounter = 5;
            
            [JsonProperty(PropertyName = "vDelivery command")]
            public string Command = "vdelivery";
            
            [JsonProperty(PropertyName = "Use GameTip for messages?")]
            public bool Use_GameTips = true;
            
            [JsonProperty(PropertyName = "Is it worth ignoring the inaccessibility of drones?")]
            public bool Ignore_Accessibility = false;
            
            [JsonProperty(PropertyName = "Display position - Forward")]
            public float Display_Pos_Forward = -0.35f;

            [JsonProperty(PropertyName = "Display position - Up")]
            public float Display_Pos_Up = 1.8f;

            [JsonProperty(PropertyName = "Display position - Right")]
            public float Display_Pos_Right = 0f;

            [JsonProperty(PropertyName = "Display rotation - x")]
            public float Display_Rot_X = 0f;

            [JsonProperty(PropertyName = "Display rotation - y")]
            public float Display_Rot_Y = 180f;

            [JsonProperty(PropertyName = "Display rotation - z")]
            public float Display_Rot_Z = 0f;
            
            [JsonProperty(PropertyName = "Max ammount of Vending machines with the Terminal")]
            public Dictionary<string, int> LimitPerGroup = new Dictionary<string, int>();
            
            [JsonProperty(PropertyName = "Delivery fee for each permissions")]
            public List<FeeGroupConfiguration> FeePerGroup = new List<FeeGroupConfiguration>();
            
            [JsonProperty(PropertyName = "UI - Duration")]
            public float UI_Duration = 6f;
            
            [JsonProperty(PropertyName = "UI_HUD. Position - AnchorMin")]
            public string UI_HUD_AnchorMin = "0 0.9";

            [JsonProperty(PropertyName = "UI_HUD. Position - AnchorMax")]
            public string UI_HUD_AnchorMax = "0.25 1";

            [JsonProperty(PropertyName = "UI_HUD. Position - OffsetMin")]
            public string UI_HUD_OffsetMin = "20 0";

            [JsonProperty(PropertyName = "UI_HUD. Position - OffsetMax")]
            public string UI_HUD_OffsetMax = "0 -30";
            
            [JsonProperty("UI_HUD. Icon - Url")]
            public string UI_HUD_Icon_Url = "https://i.imgur.com/4Adzkb8.png";
            
            [JsonProperty(PropertyName = "UI_HUD. Icon - Color")]
            public string UI_HUD_Icon_Color = "#CCE699";
            
            [JsonProperty(PropertyName = "UI_HUD. Icon - Transparency")]
            public float UI_HUD_Icon_Transparency = 0.8f;
            
            [JsonProperty("UI. Text - Font")]
            public string UI_HUD_Text_Font = "RobotoCondensed-Bold.ttf";
            
            [JsonProperty(PropertyName = "UI. Text - Font Size")]
            public int UI_HUD_Text_Font_Size = 14;
            
            [JsonProperty(PropertyName = "UI. Text - Font Color")]
            public string UI_HUD_Text_Font_Color = "#FFFFFF";
            
            [JsonProperty("UI. Text description - Font")]
            public string UI_HUD_Text_Desc_Font = "RobotoCondensed-Regular.ttf";
            
            [JsonProperty(PropertyName = "UI. Text description - Font Size")]
            public int UI_HUD_Text_Desc_Font_Size = 12;

            [JsonProperty(PropertyName = "UI. Text description - Font Color")]
            public string UI_HUD_Text_Desc_Font_Color = "#FFFFFF";
            
            [JsonProperty("Show HUD Sound - Prefab Name")]
            public string Sound_ShowHUD_Prefab = "assets/bundled/prefabs/fx/invite_notice.prefab";
            
            public Oxide.Core.VersionNumber Version;
        }

        private class FeeGroupConfiguration
        {
            [JsonProperty(PropertyName = "Permission name")]
            public string Permission_Name;

            [JsonProperty(PropertyName = "Delivery fee item")]
            public string Delivery_Fee_Item_Definition;

            [JsonProperty(PropertyName = "Delivery fee amount")]
            public int Delivery_Fee_Item_Amount;
            
            public FeeGroupConfiguration() { }
            
            public FeeGroupConfiguration(string permission_Name, string delivery_Fee_Item_Definition = "scrap", int delivery_Fee_Item_Amount = 20)
            {
                Permission_Name = permission_Name;
                Delivery_Fee_Item_Definition = delivery_Fee_Item_Definition;
                Delivery_Fee_Item_Amount = delivery_Fee_Item_Amount;
            }
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                {
                    PrintWarning("Configuration file not found. Creating a new one...");
                    LoadDefaultConfig();
                }
                else if (_config.Version < Version)
                {
                    PrintWarning($"Your configuration file version({_config.Version}) is outdated. Updating it to {Version}.");
                    LoadDefaultConfig();
                    _config.Version = Version;
                    PrintWarning($"The configuration file has been successfully updated to version {_config.Version}!");
                }
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }
        
        protected override void SaveConfig() => Config.WriteObject(_config);
        protected override void LoadDefaultConfig() => _config = new Configuration { LimitPerGroup = DefaultLimitsPerGroup, FeePerGroup = DefaultFeesPerGroup };
        #endregion
        
        #region ~DataFile~
        private static StoredData _storedData;

        private class StoredData
        {
            [JsonProperty(PropertyName = "vDelivery list")]
            public Dictionary<ulong, List<ulong>> vDeliveryList = new Dictionary<ulong, List<ulong>>();
        }
        
        private void Init()
        {
            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
        }
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _storedData);
        #endregion
        
        #region ~Language~
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["MsgNotAllowed"] = "You do not have permission to use this command!",
                ["MsgNotOwner"] = "You are not the owner of this vending machine!",
                ["MsgNotAccessible"] = "The vending machine is not accessible to drones!",
                ["MsgNotVending"] = "You need to look at the vending machine or provide correct net ID!",
                ["MsgNotVendingDelivery"] = "The vending machine does not have a terminal!",
                ["MsgLimitReached"] = "You cannot add a terminal as you have reached your limit of {0}!",
                ["MsgAddBtn"] = "Add a terminal to the vending machine?",
                ["MsgAddBtnDesc"] = "Click on the notification to confirm",
                ["MsgMyAdded"] = "The terminal has been successfully added!",
                ["MsgMyRemoved"] = "The terminal has been successfully removed!",
                ["MsgMyAllRemoved"] = "All your terminals have been successfully removed!",
                ["MsgPlayerAllRemoved"] = "All {0}'s terminals have been successfully removed!",
                ["MsgAllRemoved"] = "All terminals have been successfully removed!",
                ["MsgTerminalsNotFound"] = "No terminals found!",
                ["MsgPlayerTerminalsNotFound"] = "{0}'s terminals not found!",
                ["MsgNoHaveCustomFee"] = "To pay the personal fee, you need to have :{0}:(x{1}). Using default fee settings!"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["MsgNotAllowed"] = "У вас недостаточно прав для использования этой команды!",
                ["MsgNotOwner"] = "Вы не являетесь владельцем данного торгового автомата!",
                ["MsgNotAccessible"] = "Торговый автомат не доступен для дронов!",
                ["MsgNotVending"] = "Вам необходимо смотреть на торговый автомат или указать корректный net ID!",
                ["MsgNotVendingDelivery"] = "Торговый автомат не имеет терминала!",
                ["MsgLimitReached"] = "Вы не можете добавить терминал, так как вы превысили свой лимит в {0}!",
                ["MsgAddBtn"] = "Добавить терминал к торговому автомату?",
                ["MsgAddBtnDesc"] = "Нажмите на уведомление для подтверждения",
                ["MsgMyAdded"] = "Терминал успешно добавлен!",
                ["MsgMyRemoved"] = "Терминал успешно удален!",
                ["MsgMyAllRemoved"] = "Все ваши терминалы успешно удалены!",
                ["MsgPlayerAllRemoved"] = "Все терминалы игрока {0} успешно удалены!",
                ["MsgAllRemoved"] = "Все терминалы успешно удалены!",
                ["MsgTerminalsNotFound"] = "Терминалы не найдены!",
                ["MsgPlayerTerminalsNotFound"] = "Терминалы игрока {0} не найдены!",
                ["MsgNoHaveCustomFee"] = "Для оплаты персональной комиссии вам необходимо иметь :{0}:(x{1}). Использование настроек комиссии по умолчанию!"
            }, this, "ru");
        }
        #endregion

        #region ~Methods~
        private void ImgLibCheck()
        {
            if (ImageLibrary == null || !ImageLibrary.IsLoaded)
            {
                _imgLibCheck++;

                if (_imgLibCheck >= _config.ImgLibCounter)
                {
                    PrintError("ImageLibrary appears to be missing or occupied by other plugins load orders. For full plugin functionality, it is recommended to load ImageLibrary plugin.", _config.ImgLibCounter, Name);
                    return;
                }

                timer.In(60, ImgLibCheck);
                if (ImageLibrary == null)
                    PrintWarning("ImageLibrary is NOT loaded! Waiting ImageLibrary...");
                else
                    PrintWarning("ImageLibrary appears to be occupied will check again in 1 minute.");
                return;
            }

            _imgLibCheck = 0;
            _imgLibIsLoaded = true;
            LoadImgs();
        }
        
        private void LoadImgs()
        {
            if (_config.UI_HUD_Icon_Url.StartsWithAny(new string[2] { "http://", "https://" }))
            {
                Dictionary<string, string> imgList = new Dictionary<string, string>() { { $"{Name}_Add", _config.UI_HUD_Icon_Url } };
                ImageLibrary?.Call("ImportImageList", Name, imgList, 0UL, true);
            }
        }
        
        private bool CanAddToVending(VendingMachine vending, string initID = "", bool sendReply = false)
        {
            bool result = false;
            string replyKey = string.Empty;
            string[] replyArgs = new string[10];
            if (vending != null)
            {
                _storedData.vDeliveryList.TryGetValue(vending.OwnerID, out var ownerData);
                if (permission.UserHasPermission(initID, PERMISSION_ADMIN))
                    result = true;
                else
                {
                    var ownerPlayer = covalence.Players.FindPlayerById($"{vending.OwnerID}");
                    if (ownerPlayer != null)
                    {
                        string userPerm = GetHighestPermission(ownerPlayer.Id);
                        if (string.IsNullOrWhiteSpace(userPerm))
                            replyKey = "MsgNotAllowed";
                        else if (!string.IsNullOrWhiteSpace(initID) && ownerPlayer.Id != initID)
                            replyKey = "MsgNotOwner";
                        else if (!_config.Ignore_Accessibility && !IsVendingAccessible(vending))
                            replyKey = "MsgNotAccessible";
                        else if (ownerData != null && !ownerData.Contains(vending.net.ID.Value) && ownerData.Count >= _config.LimitPerGroup[userPerm])
                        {
                            replyKey = "MsgLimitReached";
                            replyArgs[0] = $"{_config.LimitPerGroup[userPerm]}";
                        }    
                        else
                            result = true;

                        if (sendReply && !string.IsNullOrWhiteSpace(replyKey))
                        {
                            if (_config.Use_GameTips)
                                SendTemporaryTip(ownerPlayer, string.Format(lang.GetMessage(replyKey, this, ownerPlayer.Id), replyArgs), true);
                            else
                                ownerPlayer.Reply(lang.GetMessage(replyKey, this, ownerPlayer.Id));
                        }
                    }
                }
                if (!result && ownerData != null && ownerData.Contains(vending.net.ID.Value))
                    _storedData.vDeliveryList[vending.OwnerID].Remove(vending.net.ID.Value);
            }
            return result;
        }
        
        private void AddToVending(VendingMachine vending, bool sendReply = false)
        {
            if (vending == null) return;
            if (vending.children != null) RemoveFromVending(vending);//TODO replace to checking

            var ownerPlayer = covalence.Players.FindPlayerById($"{vending.OwnerID}");
            
            var marketPos = vending.transform.position + vending.transform.up * -2000f;
            var marketEnt = GameManager.server.CreateEntity(marketplacePrefab, marketPos, vending.transform.rotation) as Marketplace;
            if (marketEnt != null)
            {
                marketEnt.Spawn();
                marketEnt.SetParent(vending, true);
                marketEnt.droneLaunchPoint.position = vending.transform.position + vending.transform.forward * 1f + vending.transform.up * 1.5f;
                foreach (var terminal in marketEnt.terminalEntities)
                {
                    terminal.Get(true).Kill();
                }
            }

            var terPos = vending.transform.position + vending.transform.forward * _config.Display_Pos_Forward + vending.transform.up * _config.Display_Pos_Up + vending.transform.right * _config.Display_Pos_Right;
            var terRot = vending.transform.rotation * Quaternion.Euler(_config.Display_Rot_X, _config.Display_Rot_Y, _config.Display_Rot_Z);
            var terEnt = GameManager.server.CreateEntity(marketplaceTerminalPrefab, terPos, terRot) as MarketTerminal;
            if (terEnt != null)
            {
                terEnt.Spawn();
                terEnt.SetParent(vending, true);
                terEnt.Setup(marketEnt);
            }
            if (!_storedData.vDeliveryList.ContainsKey(vending.OwnerID))
                _storedData.vDeliveryList[vending.OwnerID] = new List<ulong>();
            _storedData.vDeliveryList[vending.OwnerID].Add(vending.net.ID.Value);
            if (sendReply)
            {
                if (_config.Use_GameTips)
                    SendTemporaryTip(ownerPlayer, lang.GetMessage("MsgMyAdded", this, ownerPlayer.Id));
                else
                    ownerPlayer.Reply(lang.GetMessage("MsgMyAdded", this, ownerPlayer.Id));
            }
        }
        
        private void RemoveFromVending(VendingMachine vending, bool isUnload = false)
        {
            if (vending != null && !vending.IsDestroyed && vending.children != null && vending.children.Count > 0)
            {
                var entitiesToRemove = Pool.GetList<BaseEntity>();
                foreach (var vendingChild in vending.children)
                {
                    if (vendingChild != null && !vendingChild.IsDestroyed && (vendingChild is MarketTerminal || vendingChild is Marketplace))
                        entitiesToRemove.Add(vendingChild);
                }
                foreach (var entToRemove in entitiesToRemove)
                {
                    if (entToRemove is MarketTerminal)
                    {
                        var inventory = ((MarketTerminal)entToRemove).inventory;
                        if (inventory != null && inventory.itemList.Count > 0)
                        {
                            var invPos = vending.transform.position + vending.transform.forward * -0.6f + vending.transform.up * 1f;
                            inventory.Drop(itemDropPrefab, invPos, vending.transform.rotation, 0f);
                        }
                    }
                    entToRemove.Kill();
                }
                Pool.FreeList(ref entitiesToRemove);
            }
            if (!isUnload && vending != null && _storedData.vDeliveryList.ContainsKey(vending.OwnerID))
                _storedData.vDeliveryList[vending.OwnerID].Remove(vending.net.ID.Value);
        }
        
        private void UpdateEntities()
        {
            var vList = _storedData.vDeliveryList.ToDictionary(entry => entry.Key, entry => new List<ulong>(entry.Value));
            foreach (var kvp in vList)
            {
                ulong key = kvp.Key;
                var vendingList = kvp.Value;
                var vendingsToCreate = Pool.GetList<VendingMachine>();
                foreach (var vendingID in vendingList)
                {
                    var vending = BaseNetworkable.serverEntities.Find(new NetworkableId(vendingID)) as VendingMachine;
                    if (vending != null && !vending.IsDestroyed && CanAddToVending(vending))
                        vendingsToCreate.Add(vending);
                    else if (_storedData.vDeliveryList[key].Contains(vendingID))
                        _storedData.vDeliveryList[key].Remove(vendingID);
                }
                if (vendingsToCreate.Count > 0)
                {
                    string userPerm = GetHighestPermission(key.ToString());
                    int limit = _config.LimitPerGroup[userPerm];
                    int counter = 0;
                    foreach (var vending in vendingsToCreate)
                    {
                        counter++;
                        if (counter <= limit)
                            AddToVending(vending);
                        else
                            _storedData.vDeliveryList[key].Remove(vending.net.ID.Value);
                    }
                }
                Pool.FreeList(ref vendingList);
            }
        }
        
        private void ClearEntities()
        {
            foreach (var vending in BaseNetworkable.serverEntities.OfType<VendingMachine>())
            {
                RemoveFromVending(vending, true);
            }
        }
        
        private FeeGroupConfiguration GetFeeByPermission(string permission)
        {
            if (!string.IsNullOrWhiteSpace(permission))
                return _config.FeePerGroup.FirstOrDefault(config => config.Permission_Name.Equals(permission, StringComparison.OrdinalIgnoreCase));
            return null;
        }

        private ItemContainer GetTerminalContainer(VendingMachine vending)
        {
            if (vending != null && vending.children != null)
            {
                foreach (var child in vending.children.OfType<MarketTerminal>())
                {
                    return child.inventory;
                }
            }
            return null;
        }
        
        private bool IsVendingDelivery(VendingMachine vending)
        {
            if (vending != null && vending.children != null)
                return vending.children.Any(e => e is MarketTerminal);
            return false;
        }
        
        public bool IsVendingAccessible(VendingMachine vending, Vector3 offset = default, Vector3 halfExtents = default, float testHeight = 200f)
        {
            if (offset.Equals(default))
                offset = new Vector3(0f, 1f, 1f);
            if (halfExtents.Equals(default))
                halfExtents = new Vector3(0.5f, 0.5f, 0.5f);
            Vector3 vector = vending.transform.TransformPoint(offset);
            if (Physics.BoxCast(vector + Vector3.up * testHeight, halfExtents, Vector3.down, vending.transform.rotation, testHeight, 161546496))
                return false;

            return vending.IsVisibleAndCanSee(vector);
        }
        
        private VendingMachine GetLookVending(BasePlayer player, float maxDistance = 10f)
        {
            if (Physics.Raycast(player.eyes.HeadRay(), out var hit, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.GetEntity() is VendingMachine vending)
                    return vending;
            }
            return null;
        }
        
        private VendingMachine GetVendingFromArgs(BasePlayer player, string[] args)
        {
            if (args.Length > 1 && ulong.TryParse(args[1], out var entID))
            {
                var vendingByID = BaseNetworkable.serverEntities.Find(new NetworkableId(entID)) as VendingMachine;
                if (vendingByID != null)
                    return vendingByID;
            }
            return GetLookVending(player);
        }
        
        private List<VendingMachine> GetAllDeliveryVendings()
        {
            var vendingList = new List<VendingMachine>();
            foreach (var vending in BaseNetworkable.serverEntities.OfType<VendingMachine>())
            {
                if (vending != null && IsVendingDelivery(vending))
                    vendingList.Add(vending);
            }
            return vendingList;
        }
        
        private bool IsNPCVendingCustom(VendingMachine vendingMachine) => CustomVendingSetup?.Call("API_IsCustomized", vendingMachine as NPCVendingMachine) is bool result ? result : false;
        
        private static string StringColorFromHex(string hexColor, double transperent = 1)
        {
            if (hexColor[0] != '#' || hexColor.Length < 7)
                return $"1 1 1 {transperent:F2}";

            int red = Convert.ToInt32(hexColor.Substring(1, 2), 16);
            int green = Convert.ToInt32(hexColor.Substring(3, 2), 16);
            int blue = Convert.ToInt32(hexColor.Substring(5, 2), 16);

            double redPercentage = (double)red / 255;
            double greenPercentage = (double)green / 255;
            double bluePercentage = (double)blue / 255;

            return $"{redPercentage:F2} {greenPercentage:F2} {bluePercentage:F2} {transperent:F2}";
        }
        
        private static void SendEffect(Vector3 position, Network.Connection connection, string prefabName = "")
        {
            if (prefabName.Length < 10)
                prefabName = "assets/bundled/prefabs/fx/invite_notice.prefab";
            var effect = new Effect();
            effect.Init(Effect.Type.Generic, position, Vector3.zero);
            effect.pooledString = prefabName;
            EffectNetwork.Send(effect, connection);
        }
        
        private static void SendTemporaryTip(IPlayer player, string message, bool isWarning = false)
        {
            if (player != null)
                player.Command("gametip.showtoast", isWarning ? 1 : 0, message, null, null);
        }
        
        private string GetHighestPermission(string playerID)
        {
            if (!string.IsNullOrWhiteSpace(playerID))
            {
                if (permission.UserHasPermission(playerID, PERMISSION_THREE))
                    return PERMISSION_THREE;
                else if (permission.UserHasPermission(playerID, PERMISSION_TWO))
                    return PERMISSION_TWO;
                else if (permission.UserHasPermission(playerID, PERMISSION_ONE))
                    return PERMISSION_ONE;
            }
            return string.Empty;
        }
        
        private Dictionary<ulong, string> GetUsersInGroup(string groupName)
        {
            var userList = new Dictionary<ulong, string>();
            var groupData = permission.GetGroupData(groupName);
            if (groupData != null)
            {
                ulong userID;
                foreach (var user in permission.GetUsersInGroup(groupName))
                {
                    var parts = user.Split(new char[] { '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        if (ulong.TryParse(parts[0], out userID))
                            userList[userID] = parts[1];
                    }
                }
            }
            return userList;
        }

        private void RemoveVendingOnRevoke(ulong userID)
        {
            string perm = GetHighestPermission(userID.ToString());
            int counter = 0;
            var removeList = Pool.GetList<VendingMachine>();
            foreach (var netID in _storedData.vDeliveryList[userID])
            {
                var vending = BaseNetworkable.serverEntities.Find(new NetworkableId(netID)) as VendingMachine;
                if (vending != null)
                {
                    counter++;
                    if (string.IsNullOrWhiteSpace(perm) || counter > _config.LimitPerGroup[perm])
                        removeList.Add(vending);
                }
            }
            foreach (var vend in removeList)
            {
                RemoveFromVending(vend);
            }
            Pool.FreeList(ref removeList);
        }
        
        private void DestroyPlayerUI(BasePlayer player)
        {
            if (_playerUI.TryGetValue(player.userID, out var uiName))
            {
                CuiHelper.DestroyUi(player, uiName);
                _playerUI.Remove(player.userID);
            }
            if (_playerTimers.TryGetValue(player.userID, out var timer))
            {
                timer.Destroy();
                _playerTimers.Remove(player.userID);
            }
        }
        
        void DestroyAllUI()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                DestroyPlayerUI(player);
            }
        }
        #endregion

        #region ~Hooks~
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (_serverLoaded && entity is VendingMachine vending)
            {
                if (_storedData.vDeliveryList.ContainsKey(vending.OwnerID) && _storedData.vDeliveryList[vending.OwnerID].Contains(vending.net.ID.Value)) return;
                var ownerPlayer = BasePlayer.FindByID(vending.OwnerID);
                if (ownerPlayer != null && CanAddToVending(vending))
                    ShowHUD(ownerPlayer, vending.net.ID);
            }
        }
        
        object OnEntityKill(BaseNetworkable entity)
        {
            if (entity is VendingMachine vending && IsVendingDelivery(vending))
            {
                var inventory = GetTerminalContainer(vending);
                if (inventory != null && inventory.itemList.Count > 0)
                {
                    var invPos = vending.transform.position + vending.transform.forward * -0.6f + vending.transform.up * 1f;
                    inventory.Drop(itemDropPrefab, invPos, vending.transform.rotation, 0f);
                }
                if (_storedData.vDeliveryList.ContainsKey(vending.OwnerID))
                    _storedData.vDeliveryList[vending.OwnerID].Remove(vending.net.ID.Value);
            }
            return null;
        }
        
        object OnRotateVendingMachine(VendingMachine machine, BasePlayer player)
        {
            if (!_config.Ignore_Accessibility && IsVendingDelivery(machine) && !IsVendingAccessible(machine, new Vector3(0f, 1f, -1.2f)))
            {
                if (_config.Use_GameTips)
                    SendTemporaryTip(player.IPlayer, lang.GetMessage("MsgNotAccessible", this, player.UserIDString));
                else
                    player.ChatMessage(lang.GetMessage("MsgNotAccessible", this, player.UserIDString));
                return false;
            }
            return null;
        }
        
        object OnVendingTransaction(VendingMachine shopVending, BasePlayer buyer, int sellOrderId, int numberOfTransactions, ItemContainer targetContainer)
        {
            if (targetContainer != null)
            {
                var parentVending = targetContainer.GetEntityOwner().GetParentEntity() as VendingMachine;
                if (parentVending != null && !IsVendingAccessible(parentVending))
                {
                    if (_config.Use_GameTips)
                        SendTemporaryTip(buyer.IPlayer, lang.GetMessage("MsgNotAccessible", this, buyer.UserIDString));
                    else
                        buyer.ChatMessage(lang.GetMessage("MsgNotAccessible", this, buyer.UserIDString));
                    return false;
                }
            }
            return null;
        }
        
        object CanPurchaseItem(BasePlayer buyer, Item item, Action<BasePlayer, Item> onItemPurchased, VendingMachine shopVending, ItemContainer targetContainer)
        {
            if (targetContainer != null && shopVending != null && targetContainer.GetEntityOwner() is MarketTerminal terminal && terminal.GetParentEntity() is VendingMachine parentVending)
            {
                string userPerm = GetHighestPermission(buyer.UserIDString);
                var feeValue = GetFeeByPermission(userPerm);
                if (feeValue != null)
                {
                    int deliveryFeeAmount = feeValue.Delivery_Fee_Item_Amount;
                    var deliveryFeeCurrency = ItemManager.FindItemDefinition(feeValue.Delivery_Fee_Item_Definition);

                    if (deliveryFeeCurrency != null)
                    {
                        if ((feeValue.Delivery_Fee_Item_Definition != "scrap" || (feeValue.Delivery_Fee_Item_Definition == "scrap" && feeValue.Delivery_Fee_Item_Amount != 20)) && !IsNPCVendingCustom(shopVending))
                        {
                            int takenAmount = buyer.inventory.Take(null, deliveryFeeCurrency.itemid, deliveryFeeAmount);
                            if (takenAmount != deliveryFeeAmount)
                            {
                                if (takenAmount > 0)
                                {
                                    Item defaultFeeItem = ItemManager.CreateByItemID(deliveryFeeCurrency.itemid, takenAmount, 0uL);
                                    if (!buyer.inventory.GiveItem(defaultFeeItem))
                                    {
                                        defaultFeeItem.Drop(buyer.inventory.containerMain.dropPosition, buyer.inventory.containerMain.dropVelocity);
                                    }
                                }

                                string replyKey = "MsgNoHaveCustomFee";
                                string[] replyArgs = new string[2] { feeValue.Delivery_Fee_Item_Definition, $"{deliveryFeeAmount}" };
                                if (_config.Use_GameTips)
                                    SendTemporaryTip(buyer.IPlayer, string.Format(lang.GetMessage(replyKey, this, buyer.UserIDString), replyArgs));
                                else
                                    buyer.ChatMessage(string.Format(lang.GetMessage(replyKey, this, buyer.UserIDString), replyArgs));
                            }
                            else
                            {
                                Item defaultFeeItem = ItemManager.CreateByItemID(terminal.deliveryFeeCurrency.itemid, terminal.deliveryFeeAmount, 0uL);
                                if (!buyer.inventory.GiveItem(defaultFeeItem))
                                {
                                    defaultFeeItem.Drop(buyer.inventory.containerMain.dropPosition, buyer.inventory.containerMain.dropVelocity);
                                }
                            }

                            Facepunch.Rust.Analytics.Server.VendingMachineTransaction(null, item.info, item.amount);
                            if (!item.MoveToContainer(targetContainer))
                            {
                                item.Drop(targetContainer.dropPosition, targetContainer.dropVelocity);
                            }
                            onItemPurchased?.Invoke(buyer, item);

                            return true;
                        }
                    }
                    else
                        PrintWarning($"Could not find an item with the name {feeValue.Delivery_Fee_Item_Definition} for user {buyer.displayName} with permission {userPerm}. Using default fee settings.");
                }
            }
            return null;
        }
        
        void OnUserPermissionRevoked(string id, string permName)
        {
            if (permName != PERMISSION_ONE && permName != PERMISSION_TWO && permName != PERMISSION_THREE) return;
            
            ulong userID;
            if (ulong.TryParse(id, out userID) && _storedData.vDeliveryList.ContainsKey(userID))
                RemoveVendingOnRevoke(userID);
        }
        
        void OnGroupPermissionRevoked(string name, string permName)
        {
            if (permName != PERMISSION_ONE && permName != PERMISSION_TWO && permName != PERMISSION_THREE) return;
            
            foreach (var userID in GetUsersInGroup(permName).Keys)
            {
                if (_storedData.vDeliveryList.ContainsKey(userID))
                    RemoveVendingOnRevoke(userID);
            }
        }
        
        void OnPluginLoaded(Plugin plugin)
        {
            if (plugin == ImageLibrary)
                ImgLibCheck();
        }
        
        void OnPluginUnloaded(Plugin plugin)
        {
            if (plugin.Name == "ImageLibrary")
                _imgLibIsLoaded = false;
        }
        
        void OnServerInitialized()
        {
            _serverLoaded = true;
            permission.RegisterPermission(PERMISSION_ONE, this);
            permission.RegisterPermission(PERMISSION_TWO, this);
            permission.RegisterPermission(PERMISSION_THREE, this);
            permission.RegisterPermission(PERMISSION_ADMIN, this);
            AddCovalenceCommand(_config.Command, nameof(vDelivery_Command));
            ImgLibCheck();
            UpdateEntities();
        }
        
        void OnServerSave()
        {
            SaveData();
        }
        #endregion

        #region ~Commands~
        private void vDelivery_Command(IPlayer player, string command, string[] args)
        {
            bool isAdmin = permission.UserHasPermission(player.Id, PERMISSION_ADMIN);
            string userPerm = GetHighestPermission(player.Id);
            if (!isAdmin && string.IsNullOrWhiteSpace(userPerm))
            {
                if (player != null)
                {
                    if (_config.Use_GameTips)
                        SendTemporaryTip(player, lang.GetMessage("MsgNotAllowed", this, player.Id), true);
                    else
                        player.Reply(lang.GetMessage("MsgNotAllowed", this, player.Id));
                }
                return;
            }
            string replyKey = string.Empty;
            string[] replyArgs = new string[10];
            
            if (player.Object is BasePlayer bPlayer && args != null && args.Length > 0)
            {
                var vending = GetVendingFromArgs(bPlayer, args);
                
                if (args[0] == "add")
                {
                    if (vending == null)
                        replyKey = "MsgNotVending";
                    else if (CanAddToVending(vending, player.Id, true))
                        AddToVending(vending, true);
                }
                else if (args[0] == "remove")
                {
                    if (vending == null)
                        replyKey = "MsgNotVending";
                    else if (!IsVendingDelivery(vending))
                        replyKey = "MsgNotVendingDelivery";
                    else if (!isAdmin && vending.OwnerID != bPlayer.userID)
                        replyKey = "MsgNotOwner";
                    else
                    {
                        RemoveFromVending(vending);
                        replyKey = "MsgMyRemoved";
                    }
                }
                else if (args[0] == "clear")
                {
                    var removeList = Pool.GetList<VendingMachine>();
                    if (isAdmin && args.Length > 1)
                    {
                        ulong userID;
                        if (ulong.TryParse(args[1], out userID))
                        {
                            if (_storedData.vDeliveryList.ContainsKey(userID))
                            {
                                foreach (var netID in _storedData.vDeliveryList[userID])
                                {
                                    var netVending = BaseNetworkable.serverEntities.Find(new NetworkableId(netID)) as VendingMachine;
                                    if (netVending != null)
                                        removeList.Add(netVending);
                                }
                                replyKey = "MsgPlayerAllRemoved";
                            }
                            if (removeList.Count == 0)
                                replyKey = "MsgPlayerTerminalsNotFound";
                            replyArgs[0] = userID.ToString();
                        }
                        else if (args[1] == "all")
                        {
                            removeList = GetAllDeliveryVendings();
                            replyKey = "MsgAllRemoved";
                        }
                    }
                    if (string.IsNullOrWhiteSpace(replyKey) && _storedData.vDeliveryList.ContainsKey(bPlayer.userID))
                    {
                        foreach (var netID in _storedData.vDeliveryList[bPlayer.userID])
                        {
                            var netVending = BaseNetworkable.serverEntities.Find(new NetworkableId(netID)) as VendingMachine;
                            if (netVending != null)
                                removeList.Add(netVending);
                        }
                        replyKey = "MsgMyAllRemoved";
                    }
                    
                    if (string.IsNullOrWhiteSpace(replyKey) && removeList.Count < 1)
                        replyKey = "MsgTerminalsNotFound";
                    foreach (var vend in removeList)
                    {
                        RemoveFromVending(vend);
                    }
                    Pool.FreeList(ref removeList);
                }
            }
            
            if (!string.IsNullOrWhiteSpace(replyKey))
            {
                if (_config.Use_GameTips)
                    SendTemporaryTip(player, string.Format(lang.GetMessage(replyKey, this, player.Id), replyArgs));
                else
                    player.Reply(string.Format(lang.GetMessage(replyKey, this, player.Id), replyArgs));
            }
        }
        #endregion

        #region ~UI~
        private void ShowHUD(BasePlayer player, NetworkableId entID)
        {
            if (_playerUI.ContainsKey(player.userID))
                DestroyPlayerUI(player);
            
            SendEffect(player.transform.position, player.Connection, _config.Sound_ShowHUD_Prefab);
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = _config.UI_HUD_AnchorMin, AnchorMax = _config.UI_HUD_AnchorMax, OffsetMin = _config.UI_HUD_OffsetMin, OffsetMax = _config.UI_HUD_OffsetMax },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", UI_HUD_Name);
            ICuiComponent Icon;
            if (!_imgLibIsLoaded)
                Icon = new CuiRawImageComponent { Url = _config.UI_HUD_Icon_Url };
            else
                Icon = new CuiImageComponent { Color = StringColorFromHex(_config.UI_HUD_Icon_Color, _config.UI_HUD_Icon_Transparency), Png = (string)ImageLibrary?.Call("GetImage", $"{Name}_Add") };
            container.Add(new CuiElement
            {
                Parent = UI_HUD_Name,
                Components =
                {
                    Icon,
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0.15 1", OffsetMin = "5 5", OffsetMax = "-5 -5" }
                }
            });
            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = lang.GetMessage("MsgAddBtn", this, player.UserIDString),
                    Font = _config.UI_HUD_Text_Font,
                    FontSize = _config.UI_HUD_Text_Font_Size,
                    Color = StringColorFromHex(_config.UI_HUD_Text_Font_Color),
                    Align = TextAnchor.UpperLeft
                },
                RectTransform = { AnchorMin = "0.15 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "5 -5" }
            }, UI_HUD_Name);
            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = lang.GetMessage("MsgAddBtnDesc", this, player.UserIDString),
                    Font = _config.UI_HUD_Text_Desc_Font,
                    FontSize = _config.UI_HUD_Text_Desc_Font_Size,
                    Color = StringColorFromHex(_config.UI_HUD_Text_Desc_Font_Color),
                    Align = TextAnchor.LowerLeft
                },
                RectTransform = { AnchorMin = "0.15 0", AnchorMax = "1 1", OffsetMin = "0 5", OffsetMax = "5 0" }
            }, UI_HUD_Name);
            container.Add(new CuiButton
            {
                Button =
                {
                    Close = UI_HUD_Name,
                    Command = $"{_config.Command} add {entID}",
                    Color = "0 0 0 0"
                },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "0 0" }
            }, UI_HUD_Name);

            CuiHelper.AddUi(player, container);
            _playerUI[player.userID] = UI_HUD_Name;
            Timer newTimer = timer.Once(_config.UI_Duration, () =>
            {
                DestroyPlayerUI(player);
            });
            _playerTimers[player.userID] = newTimer;
        }
        #endregion

        #region ~Unload~
        void Unload()
        {
            SaveData();
            ClearEntities();
            DestroyAllUI();
            _storedData = null;
            _config = null;
        }
        #endregion
    }
}
