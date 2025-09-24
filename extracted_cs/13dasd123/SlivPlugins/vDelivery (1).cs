using System;
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
    [Info("vDelivery", "IIIaKa", "0.1.0")]
    [Description("Drones deliver right into your vending machine.")]
    class vDelivery : RustPlugin
    {
        [PluginReference]
        private Plugin ImageLibrary;
        
        #region Variables
        private bool _serverLoaded = false;
        private int _imgLibCheck = 0;
        private Dictionary<ulong, string> _playerUI = new Dictionary<ulong, string>();
        private Dictionary<ulong, Timer> _playerTimers = new Dictionary<ulong, Timer>();
        private Dictionary<string, string> _imgList;
        private const string PERMISSION_ONE = "vDelivery.one";
        private const string PERMISSION_TWO = "vDelivery.two";
        private const string PERMISSION_THREE = "vDelivery.three";
        private const string PERMISSION_ADMIN = "vDelivery.admin";
        private const string _command = "vdelivery";
        private const string marketplacePrefab = "assets/prefabs/misc/marketplace/marketplace.prefab";
        private const string marketplaceTerminalPrefab = "assets/prefabs/misc/marketplace/marketterminal.prefab";
        private const string itemDropPrefab = "assets/prefabs/misc/item drop/item_drop.prefab";

        private readonly Dictionary<string, int> LimitPerGroup = new Dictionary<string, int>
        {
            { PERMISSION_ONE, 1 },
            { PERMISSION_TWO, 3 },
            { PERMISSION_THREE, 5 }
        };
        private readonly List<FeeGroupConfiguration> FeePerGroup = new List<FeeGroupConfiguration> { new FeeGroupConfiguration(PERMISSION_ONE), new FeeGroupConfiguration(PERMISSION_TWO), new FeeGroupConfiguration(PERMISSION_THREE) };
        
        private const string UI_HUD_Name = "vDelivery_HUD";
        private const string UI_HUD_Icon_Name = "vDelivery_HUD_Icon";
        #endregion

        #region Configuration
        private static Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "ImageLibrary Counter Check")]
            public int ImgLibCounter = 5;
            
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
            public string UI_HUD_Icon_Color = "0.8 0.9 0.6";
            
            [JsonProperty(PropertyName = "UI_HUD. Icon - Transparency")]
            public float UI_HUD_Icon_Transparency = 0.8f;
            
            [JsonProperty(PropertyName = "UI. Text - FadeIn")]
            public float UI_Text_FadeIn = 1f;
            
            [JsonProperty("UI. Text - Font")]
            public string UI_HUD_Text_Font = "RobotoCondensed-Bold.ttf";
            
            [JsonProperty(PropertyName = "UI. Text - Font Size")]
            public int UI_HUD_Text_Font_Size = 14;
            
            [JsonProperty(PropertyName = "UI. Text - Font Color")]
            public string UI_HUD_Text_Font_Color = "1 1 1 1";
            
            [JsonProperty("UI. Text description - Font")]
            public string UI_HUD_Text_Desc_Font = "RobotoCondensed-Regular.ttf";
            
            [JsonProperty(PropertyName = "UI. Text description - Font Size")]
            public int UI_HUD_Text_Desc_Font_Size = 12;

            [JsonProperty(PropertyName = "UI. Text description - Font Color")]
            public string UI_HUD_Text_Desc_Font_Color = "1 1 1 1";
            
            [JsonProperty("Show HUD Sound - Prefab Name")]
            public string Sound_ShowHUD_Prefab = "assets/bundled/prefabs/fx/invite_notice.prefab";
        }

        private class FeeGroupConfiguration
        {
            [JsonProperty(PropertyName = "Permission name")]
            public string Permission_Name;

            [JsonProperty(PropertyName = "Delivery fee item")]
            public string Delivery_Fee_Item_Definition;

            [JsonProperty(PropertyName = "Delivery fee amount")]
            public int Delivery_Fee_Item_Amount;

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
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration { LimitPerGroup = LimitPerGroup, FeePerGroup = FeePerGroup };
        #endregion
        
        #region DataFile
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
        
        #region Language
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NotAllowed"] = "You do not have permission to use this command!",
                ["NotOwner"] = "You are not the owner of this vending machine!",
                ["NotAccessible"] = "The vending machine is not accessible to drones!",
                ["NotVending"] = "You need to look at the vending machine or provide correct net ID!",
                ["NotVendingDelivery"] = "The vending machine does not have a terminal!",
                ["LimitReached"] = "You cannot add a terminal as you have reached your limit of {0}!",
                ["AddBtn"] = "Add a terminal to the vending machine?",
                ["AddBtnDesc"] = "Click on the notification to confirm",
                ["MyAdded"] = "The terminal has been successfully added!",
                ["MyRemoved"] = "The terminal has been successfully removed!",
                ["MyAllRemoved"] = "All your terminals have been successfully removed!",
                ["PlayerAllRemoved"] = "All {0}'s terminals have been successfully removed!",
                ["AllRemoved"] = "All terminals have been successfully removed!",
                ["TerminalsNotFound"] = "No terminals found!",
                ["PlayerTerminalsNotFound"] = "{0}'s terminals not found!",
                ["NoHaveCustomFee"] = "To pay the personal fee, you need to have :{0}:(x{1}). Using default fee settings!"
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NotAllowed"] = "У вас недостаточно прав для использования этой команды!",
                ["NotOwner"] = "Вы не являетесь владельцем данного торгового автомата!",
                ["NotAccessible"] = "Торговый автомат не доступен для дронов!",
                ["NotVending"] = "Вам необходимо смотреть на торговый автомат или указать корректный net ID!",
                ["NotVendingDelivery"] = "Торговый автомат не имеет терминала!",
                ["LimitReached"] = "Вы не можете добавить терминал, так как вы превысили свой лимит в {0}!",
                ["AddBtn"] = "Добавить терминал к торговому автомату?",
                ["AddBtnDesc"] = "Нажмите на уведомление для подтверждения",
                ["MyAdded"] = "Терминал успешно добавлен!",
                ["MyRemoved"] = "Терминал успешно удален!",
                ["MyAllRemoved"] = "Все ваши терминалы успешно удалены!",
                ["PlayerAllRemoved"] = "Все терминалы игрока {0} успешно удалены!",
                ["AllRemoved"] = "Все терминалы успешно удалены!",
                ["TerminalsNotFound"] = "Терминалы не найдены!",
                ["PlayerTerminalsNotFound"] = "Терминалы игрока {0} не найдены!",
                ["NoHaveCustomFee"] = "Для оплаты персональной комиссии вам необходимо иметь :{0}:(x{1}). Использование настроек комиссии по умолчанию!"
            }, this, "ru");
        }
        #endregion

        #region Methods
        private void ImgLibCheck()
        {
            if (ImageLibrary == null || !ImageLibrary.IsLoaded)
            {
                _imgLibCheck++;

                if (_imgLibCheck >= _config.ImgLibCounter)
                {
                    PrintError("ImageLibrary appears to be missing or occupied by other plugins load orders.\n{1} will be unloaded.\nYou can reload {1} when ImageLibrary will be load and free. Or increase the config counter check limit to higher than {0}.", _config.ImgLibCounter, Name);
                    Interface.Oxide.UnloadPlugin(Name);
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
            LoadImgs();
        }
        
        private void LoadImgs()
        {
            _imgList = new Dictionary<string, string>
            {
                { UI_HUD_Icon_Name, _config.UI_HUD_Icon_Url }
            };
            ImageLibrary?.Call("ImportImageList", Name, _imgList, 0UL, true);
        }
        
        private bool CanAddToVending(VendingMachine vending, string initID = "", bool sendReply = false)
        {
            bool result = false;
            string replyKey = string.Empty;
            string[] replyArgs = new string[10];
            if (vending != null)
            {
                List<ulong> ownerData;
                _storedData.vDeliveryList.TryGetValue(vending.OwnerID, out ownerData);
                if (permission.UserHasPermission(initID, PERMISSION_ADMIN))
                    result = true;
                else
                {
                    var ownerPlayer = covalence.Players.FindPlayerById(vending.OwnerID.ToString());
                    if (ownerPlayer != null)
                    {
                        string userPerm = GetHighestPermission(ownerPlayer.Id);
                        if (string.IsNullOrWhiteSpace(userPerm))
                            replyKey = "NotAllowed";
                        else if (!string.IsNullOrWhiteSpace(initID) && ownerPlayer.Id != initID)
                            replyKey = "NotOwner";
                        else if (!_config.Ignore_Accessibility && !IsVendingAccessible(vending))
                            replyKey = "NotAccessible";
                        else if (ownerData != null && !ownerData.Contains(vending.net.ID.Value) && ownerData.Count >= _config.LimitPerGroup[userPerm])
                        {
                            replyKey = "LimitReached";
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

            var ownerPlayer = covalence.Players.FindPlayerById(vending.OwnerID.ToString());
            
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
                    SendTemporaryTip(ownerPlayer, lang.GetMessage("MyAdded", this, ownerPlayer.Id));
                else
                    ownerPlayer.Reply(lang.GetMessage("MyAdded", this, ownerPlayer.Id));
            }
        }
        
        private void RemoveFromVending(VendingMachine vending, bool isUnload = false)
        {
            if (vending != null && !vending.IsDestroyed && vending.children != null && vending.children.Count > 0)
            {
                List<BaseEntity> entitiesToRemove = Pool.GetList<BaseEntity>();
                foreach (var vendingChild in vending.children)
                {
                    if (vendingChild != null && !vendingChild.IsDestroyed && (vendingChild is MarketTerminal || vendingChild is Marketplace))
                        entitiesToRemove.Add(vendingChild);
                }
                foreach (var entToRemove in entitiesToRemove)
                {
                    if (entToRemove is MarketTerminal)
                    {
                        ItemContainer inventory = ((MarketTerminal)entToRemove).inventory;
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
                List<ulong> vendingList = kvp.Value;
                List<VendingMachine> vendingsToCreate = Pool.GetList<VendingMachine>();
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
            foreach (var vending in UnityEngine.Object.FindObjectsOfType<VendingMachine>())
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
            if (vending != null && vending.children != null && vending.children.Any(e => e is MarketTerminal))
                return true;
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
        
        private VendingMachine GetLookVending(BasePlayer basePlayer, float maxDistance = 10f)
        {
            RaycastHit hit;
            if (Physics.Raycast(basePlayer.eyes.HeadRay(), out hit, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                var entity = hit.GetEntity();
                if (entity != null && entity is VendingMachine)
                    return entity as VendingMachine;
            }
            return null;
        }
        
        private VendingMachine GetVendingFromArgs(BasePlayer bPlayer, string[] args)
        {
            ulong entId;
            if (args.Length > 1 && ulong.TryParse(args[1], out entId))
            {
                NetworkableId netID = new NetworkableId(entId);
                var vendingByID = BaseNetworkable.serverEntities.Find(netID) as VendingMachine;
                if (vendingByID != null)
                    return vendingByID;
            }
            return GetLookVending(bPlayer);
        }
        
        private List<VendingMachine> GetAllDeliveryVendings()
        {
            List<VendingMachine> vendingList = new List<VendingMachine>();
            foreach (var vending in UnityEngine.Object.FindObjectsOfType<VendingMachine>())
            {
                if (vending != null && IsVendingDelivery(vending))
                    vendingList.Add(vending);
            }
            return vendingList;
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
            List<VendingMachine> removeList = Pool.GetList<VendingMachine>();
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
            string uiName;
            if (_playerUI.TryGetValue(player.userID, out uiName))
            {
                CuiHelper.DestroyUi(player, uiName);
                _playerUI.Remove(player.userID);
            }
            Timer timer;
            if (_playerTimers.TryGetValue(player.userID, out timer))
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

        #region Hooks
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (_serverLoaded && entity != null && entity is VendingMachine)
            {
                var vending = (VendingMachine)entity;
                if (_storedData.vDeliveryList.ContainsKey(vending.OwnerID) && _storedData.vDeliveryList[vending.OwnerID].Contains(vending.net.ID.Value)) return;
                var ownerPlayer = BasePlayer.FindByID(vending.OwnerID);
                if (ownerPlayer != null && CanAddToVending(vending))
                    ShowHUD(ownerPlayer, vending.net.ID);
            }
        }
        
        object OnEntityKill(BaseNetworkable entity)
        {
            if (entity != null && entity is VendingMachine && IsVendingDelivery(entity as VendingMachine))
            {
                var vending = (VendingMachine)entity;
                ItemContainer inventory = GetTerminalContainer(vending);
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
                    SendTemporaryTip(player.IPlayer, lang.GetMessage("NotAccessible", this, player.UserIDString));
                else
                    player.ChatMessage(lang.GetMessage("NotAccessible", this, player.UserIDString));
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
                        SendTemporaryTip(buyer.IPlayer, lang.GetMessage("NotAccessible", this, buyer.UserIDString));
                    else
                        buyer.ChatMessage(lang.GetMessage("NotAccessible", this, buyer.UserIDString));
                    return false;
                }
            }
            return null;
        }
        
        object CanPurchaseItem(BasePlayer buyer, Item item, Action<BasePlayer, Item> onItemPurchased, VendingMachine shopVending, ItemContainer targetContainer)
        {
            if (targetContainer != null)
            {
                var teminal = targetContainer.GetEntityOwner() as MarketTerminal;
                if (teminal != null)
                {
                    var parentVending = teminal.GetParentEntity() as VendingMachine;
                    if (parentVending != null)
                    {
                        string replyKey = string.Empty;
                        string[] replyArgs = new string[10];
                        string userPerm = GetHighestPermission(buyer.UserIDString);
                        var feeValue = GetFeeByPermission(userPerm);
                        if (feeValue != null)
                        {
                            int deliveryFeeAmount = feeValue.Delivery_Fee_Item_Amount;
                            ItemDefinition deliveryFeeCurrency = ItemManager.FindItemDefinition(feeValue.Delivery_Fee_Item_Definition);
                            if (deliveryFeeCurrency != null &&
                                (feeValue.Delivery_Fee_Item_Definition != "scrap" || (feeValue.Delivery_Fee_Item_Definition == "scrap" && feeValue.Delivery_Fee_Item_Amount != 20)))
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
                                    replyKey = "NoHaveCustomFee";
                                    replyArgs[0] = feeValue.Delivery_Fee_Item_Definition;
                                    replyArgs[1] = deliveryFeeAmount.ToString();
                                }
                                else
                                {
                                    Item defaultFeeItem = ItemManager.CreateByItemID(teminal.deliveryFeeCurrency.itemid, teminal.deliveryFeeAmount, 0uL);
                                    if (!buyer.inventory.GiveItem(defaultFeeItem))
                                    {
                                        defaultFeeItem.Drop(buyer.inventory.containerMain.dropPosition, buyer.inventory.containerMain.dropVelocity);
                                    }
                                }
                            }
                            else
                                PrintWarning($"Could not find an item with the name {feeValue.Delivery_Fee_Item_Definition} for user {buyer.displayName} with permission {userPerm}. Using default fee settings.");
                        }

                        Facepunch.Rust.Analytics.Server.VendingMachineTransaction(null, item.info, item.amount);
                        if (!item.MoveToContainer(targetContainer))
                        {
                            item.Drop(targetContainer.dropPosition, targetContainer.dropVelocity);
                        }

                        onItemPurchased?.Invoke(buyer, item);
                        if (!string.IsNullOrWhiteSpace(replyKey))
                        {
                            if (_config.Use_GameTips)
                                SendTemporaryTip(buyer.IPlayer, string.Format(lang.GetMessage(replyKey, this, buyer.UserIDString), replyArgs));
                            else
                                buyer.ChatMessage(string.Format(lang.GetMessage(replyKey, this, buyer.UserIDString), replyArgs));
                        }
                        return true;
                    }
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
        
        void OnServerInitialized()
        {
            _serverLoaded = true;
            permission.RegisterPermission(PERMISSION_ONE, this);
            permission.RegisterPermission(PERMISSION_TWO, this);
            permission.RegisterPermission(PERMISSION_THREE, this);
            permission.RegisterPermission(PERMISSION_ADMIN, this);
            AddCovalenceCommand(_command, nameof(vDelivery_Command));
            ImgLibCheck();
            UpdateEntities();
        }
        
        void OnServerSave()
        {
            SaveData();
        }
        #endregion

        #region Commands
        private void vDelivery_Command(IPlayer player, string command, string[] args)
        {
            bool isAdmin = permission.UserHasPermission(player.Id, PERMISSION_ADMIN);
            string userPerm = GetHighestPermission(player.Id);
            if (!isAdmin && string.IsNullOrWhiteSpace(userPerm))
            {
                if (player != null)
                {
                    if (_config.Use_GameTips)
                        SendTemporaryTip(player, lang.GetMessage("NotAllowed", this, player.Id), true);
                    else
                        player.Reply(lang.GetMessage("NotAllowed", this, player.Id));
                }
                return;
            }
            string replyKey = string.Empty;
            string[] replyArgs = new string[10];

            BasePlayer bPlayer = player.Object as BasePlayer;
            if (bPlayer != null && args != null && args.Length > 0)
            {
                var vending = GetVendingFromArgs(bPlayer, args);
                
                if (args[0] == "add")
                {
                    if (vending == null)
                        replyKey = "NotVending";
                    else if (CanAddToVending(vending, player.Id, true))
                        AddToVending(vending, true);
                }
                else if (args[0] == "remove")
                {
                    if (vending == null)
                        replyKey = "NotVending";
                    else if (!IsVendingDelivery(vending))
                        replyKey = "NotVendingDelivery";
                    else if (!isAdmin && vending.OwnerID != bPlayer.userID)
                        replyKey = "NotOwner";
                    else
                    {
                        RemoveFromVending(vending);
                        replyKey = "MyRemoved";
                    }
                }
                else if (args[0] == "clear")
                {
                    List<VendingMachine> removeList = Pool.GetList<VendingMachine>();
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
                                replyKey = "PlayerAllRemoved";
                            }
                            if (removeList.Count == 0)
                                replyKey = "PlayerTerminalsNotFound";
                            replyArgs[0] = userID.ToString();
                        }
                        else if (args[1] == "all")
                        {
                            removeList = GetAllDeliveryVendings();
                            replyKey = "AllRemoved";
                        }
                    }
                    else if (_storedData.vDeliveryList.ContainsKey(bPlayer.userID))
                    {
                        foreach (var netID in _storedData.vDeliveryList[bPlayer.userID])
                        {
                            var netVending = BaseNetworkable.serverEntities.Find(new NetworkableId(netID)) as VendingMachine;
                            if (netVending != null)
                                removeList.Add(netVending);
                        }
                        replyKey = "MyAllRemoved";
                    }
                    
                    if (string.IsNullOrWhiteSpace(replyKey) && removeList.Count < 1)
                        replyKey = "TerminalsNotFound";
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

        #region UI
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
            container.Add(new CuiElement
            {
                Parent = UI_HUD_Name,
                Components =
                    {
                        new CuiImageComponent { Color = $"{_config.UI_HUD_Icon_Color} {_config.UI_HUD_Icon_Transparency}", Png = (string)ImageLibrary?.Call("GetImage", UI_HUD_Icon_Name) },
                        new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "0.15 1", OffsetMin = "5 5", OffsetMax = "-5 -5" }
                    }
            });
            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = lang.GetMessage("AddBtn", this, player.UserIDString),
                    Font = _config.UI_HUD_Text_Font,
                    FontSize = _config.UI_HUD_Text_Font_Size,
                    Color = _config.UI_HUD_Text_Font_Color,
                    Align = TextAnchor.UpperLeft,
                    FadeIn = _config.UI_Text_FadeIn
                },
                RectTransform = { AnchorMin = "0.15 0", AnchorMax = "1 1", OffsetMin = "0 0", OffsetMax = "5 -5" }
            }, UI_HUD_Name);
            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = lang.GetMessage("AddBtnDesc", this, player.UserIDString),
                    Font = _config.UI_HUD_Text_Desc_Font,
                    FontSize = _config.UI_HUD_Text_Desc_Font_Size,
                    Color = _config.UI_HUD_Text_Desc_Font_Color,
                    Align = TextAnchor.LowerLeft,
                    FadeIn = _config.UI_Text_FadeIn
                },
                RectTransform = { AnchorMin = "0.15 0", AnchorMax = "1 1", OffsetMin = "0 5", OffsetMax = "5 0" }
            }, UI_HUD_Name);
            container.Add(new CuiButton
            {
                Button =
                {
                    Close = UI_HUD_Name,
                    Command = $"{_command} add {entID}",
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

        #region Unload
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
