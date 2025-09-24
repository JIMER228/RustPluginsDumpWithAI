using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Ext.Chaos;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
using Oxide.Core.Libraries;
using System.Collections;
using System.Globalization;
using Oxide.Ext.Chaos.Data;
using Oxide.Ext.Chaos.UIFramework;
using Steamworks;
using UnityEngine.UI;

using Chaos = Oxide.Ext.Chaos;
using Color = Oxide.Ext.Chaos.UIFramework.Color;
using Font = Oxide.Ext.Chaos.UIFramework.Font;
using GridLayoutGroup = Oxide.Ext.Chaos.UIFramework.GridLayoutGroup;
using VerticalLayoutGroup = Oxide.Ext.Chaos.UIFramework.VerticalLayoutGroup;

///Скачано с дискорд сервера Rust Edit [PRO+]
///discord.gg/9vyTXsJyKR

namespace Oxide.Plugins
{
    [Info("PlayerSkins", "discord.gg/9vyTXsJyKR", "3.0.1")]
    class PlayerSkins : ChaosPlugin
    {
        #region Fields

        private Datafile<Hash<ulong, UserData>> m_UserData;
        private Datafile<Hash<string, Hash<ulong, SkinData>>> m_SkinData;
        private Datafile<List<ulong>> m_ExcludedSkins;
        
        private readonly List<ulong> m_SkinsToLoad = new List<ulong>();
        private readonly HashSet<ItemDefinition> m_SkinnableItems = new HashSet<ItemDefinition>();
        private static readonly Hash<string, int> m_ShortnameToItemId = new Hash<string, int>();
        
        private readonly string[] m_IgnoreItems = new string[] { "ammo.snowballgun", "blueprintbase", "rhib", "spraycandecal", "vehicle.chassis", "vehicle.module", "water", "water.salt" };

        private static DisplayMode m_ForcedDisplayMode;
        private CurrencyType m_CurrencyType;
        private int m_ScrapItemId;

        [Chaos.Permission] private const string SHOP_PERMISSION = "playerskins.shop";
        [Chaos.Permission] private const string RESKIN_PERMISSION = "playerskins.reskin";
        [Chaos.Permission] private const string NOCHARGE_PERMISSION = "playerskins.nocharge";
        [Chaos.Permission] private const string ADMIN_PERMISSION = "playerskins.admin";
        
        public enum CurrencyType { None, ServerRewards, Economics, Scrap }
        #endregion
        
        #region Oxide Hooks

        private void Loaded()
        {
	        m_UserData = new Datafile<Hash<ulong, UserData>>("PlayerSkins/userdata");
	        m_SkinData = new Datafile<Hash<string, Hash<ulong, SkinData>>>("PlayerSkins/skinlist");
	        m_ExcludedSkins = new Datafile<List<ulong>>("PlayerSkins/excludedskins");
	        
            SetupUIComponents();
        }
        
        private void OnServerInitialized()
        {
            m_CurrencyType = ParseType<CurrencyType>(Configuration.Purchase.Type);
            if (Configuration.Purchase.Enabled && m_CurrencyType == CurrencyType.None)
            {
                PrintError("Invalid purchase plugin specified in config. Must be either 'ServerRewards' or 'Economics'!");
                return;
            }

            m_ForcedDisplayMode = ParseType<DisplayMode>(Configuration.Shop.ForcedMode);

            bool updateConfig = false;

            for (int i = 0; i < Configuration.Shop.Permissions.Count; i++)
            {
                string perm = Configuration.Shop.Permissions[i];
                if (!perm.StartsWith("playerskins."))
                {
                    Configuration.Shop.Permissions[i] = perm = $"playerskins.{perm}";
                    updateConfig = true;
                }
                
                permission.RegisterPermission(perm, this);
            }

            if (updateConfig)
                SaveConfiguration();

            RegisterChatCommands();

            foreach (ItemDefinition itemDefinition in ItemManager.itemList)
            {
                if (itemDefinition.shortname == "scrap")
                    m_ScrapItemId = itemDefinition.itemid;
                
                m_ShortnameToItemId[itemDefinition.shortname] = itemDefinition.itemid;

                string workshopName = itemDefinition.displayName.english.ToLower().Replace("skin", "").Replace(" ", "").Replace("-", "");

                if (!m_WorkshopNameToShortname.ContainsKey(workshopName))
                    m_WorkshopNameToShortname[workshopName] = itemDefinition.shortname;

                if (!m_WorkshopNameToShortname.ContainsKey(itemDefinition.shortname))
                    m_WorkshopNameToShortname[itemDefinition.shortname] = itemDefinition.shortname;

                if (!m_WorkshopNameToShortname.ContainsKey(itemDefinition.shortname.Replace(".", "")))
                    m_WorkshopNameToShortname[itemDefinition.shortname.Replace(".", "")] = itemDefinition.shortname;
            }
            
            if (ImageLibrary.IsLoaded)
            {
                ImageLibrary.AddImage("https://chaoscode.io/oxide/Images/magnifyingglass.png", "playerskins.search", 0UL, () =>
                {
                    m_MagnifyImage = ImageLibrary.GetImage("playerskins.search", 0UL);
                });
            }

            if (string.IsNullOrEmpty(Configuration.Workshop.SteamAPIKey))
            {
                PrintError("You must enter a Steam API key in your config in order to retrieve approved skin icons and/or access workshop items. Unable to continue...");
                return;
            }

            if ((Steamworks.SteamInventory.Definitions?.Length ?? 0) == 0)
            {
                PrintWarning("Waiting for Steamworks to update item definitions....");
                Steamworks.SteamInventory.OnDefinitionsUpdated += StartApprovedRequest;
            }
            else StartApprovedRequest();

            timer.In(Configuration.Announcements.Interval * 60, BroadcastAnnouncement);
        }

        private void OnServerSave() => m_UserData.Save();

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                ChaosUI.Destroy(player, PS_UI_MOUSE);
                ChaosUI.Destroy(player, PS_UI);
                ChaosUI.Destroy(player, PS_UI_POPUP);
            }
        }

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (player == null || !player.IsValid() || !player.userID.IsSteamId())
                return;

            CuiHelper.DestroyUi(player, PS_UI);
        }

        private void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            if (player == null || npc == null) return;

            if (!m_UserData.Data.ContainsKey(player.userID))
                m_UserData.Data.Add(player.userID, new UserData());

            if (Configuration.Shop.NPCs.Contains(npc.UserIDString))
                OpenSkinShop(player);
            else if (Configuration.Reskin.NPCs.Contains(npc.UserIDString) && !Configuration.Shop.GiveItemOnPurchase)
                OpenReskinMenu(player);
        }

        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            BasePlayer player = task?.owner;
            if (player == null || item == null)
                return;

            if (item.skin != 0)
                return;

            UserData data;
            if (!m_UserData.Data.TryGetValue(player.userID, out data))
                return;

            if (data.defaultSkins.ContainsKey(item.info.shortname))
                ChangeItemSkin(item, data.defaultSkins[item.info.shortname]);
        }
        #endregion
        
        #region Functions
        private void BroadcastAnnouncement()
        {
            if (!Configuration.Announcements.Enabled && Configuration.Announcements.Interval > 0)
                return;

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (Configuration.Shop.DisableCommand)
                    player.LocalizedMessage(this, "Help.Shop.NPC");
                else player.LocalizedMessage(this, "Help.Shop.Command");
                if (Configuration.Reskin.DisableCommand)
                    player.LocalizedMessage(this, "Help.Reskin.NPC");
                else player.LocalizedMessage(this, "Help.Reskin.Command");
            }

            timer.In(Configuration.Announcements.Interval * 60, BroadcastAnnouncement);
        }

        private void ChangeItemSkin(BasePlayer player, ulong targetSkin)
        {
            Item item = player.GetActiveItem();
            if (item == null)
            {
                item = player.inventory.containerBelt.GetSlot(0);
                if (item == null)
                    return;
            }

            ChangeItemSkin(item, targetSkin);

            int slot = item.position;
            item.SetParent(null);
            item.MarkDirty();

            timer.Once(0.15f, () =>
            {
                if (item == null)
                    return;

                item.SetParent(player.inventory.containerBelt);
                item.position = slot;
                item.MarkDirty();
            });
        }

        private void ChangeItemSkin(Item item, ulong targetSkin)
        {
            item.skin = targetSkin;
            item.MarkDirty();

            BaseEntity heldEntity = item.GetHeldEntity();
            if (heldEntity != null)
            {
                heldEntity.skinID = targetSkin;
                heldEntity.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
            }
        }

        private int GetUserBalance(BasePlayer player)
        {
            switch (m_CurrencyType)
            {
                case CurrencyType.ServerRewards:
                    if (ServerRewards.IsLoaded)
                    {
                        object value = ServerRewards.CheckPoints(player.userID);
                        if (value is int)
                            return (int) value;
                    }

                    return 0;
                
                case CurrencyType.Economics:
                    if (Economics.IsLoaded)
                        return Convert.ToInt32(Economics.Balance(player.userID));
                    
                    return 0;
                
                case CurrencyType.Scrap:
                    return player.inventory.GetAmount(m_ScrapItemId);
                
                case CurrencyType.None:
                default:
                    return 0;
            }
        }
        
        private void RefundPurchase(BasePlayer player, int price)
        {
            switch (m_CurrencyType)
            {
                case CurrencyType.ServerRewards:
                    if (ServerRewards.IsLoaded)
                        ServerRewards.AddPoints(player.userID, price);
                    break;
                case CurrencyType.Economics:
                    if (Economics.IsLoaded)
                        Economics.Deposit(player.userID, (double) price);
                    break;
                case CurrencyType.Scrap:
                    player.GiveItem(ItemManager.CreateByItemID(m_ScrapItemId, price));
                    break;
                case CurrencyType.None:
                default:
                    return;
            }
        }
        
        private bool ChargeForPurchase(BasePlayer player, int price)
        {
            if (price <= 0)
                return true;
            
            switch (m_CurrencyType)
            {
                case CurrencyType.ServerRewards:
                    if (ServerRewards.IsLoaded)
                    {
                        object value = ServerRewards.TakePoints(player.userID, price);
                        if (value is bool)
                            return (bool) value;
                    }
                    return false;
                
                case CurrencyType.Economics:
                    if (Economics.IsLoaded)
                        return Economics.Withdraw(player.userID, (double) price);
                        
                    return false;
                
                case CurrencyType.Scrap:
                    if (player.inventory.GetAmount(m_ScrapItemId) >= price)
                    {
                        player.inventory.Take(null, m_ScrapItemId, price);
                        return true;
                    }
                    
                    return false;
                
                case CurrencyType.None:
                default:
                    return false;
            }
        }
        #endregion
        
        #region Workshop Name Conversions
        private Dictionary<string, string> m_WorkshopNameToShortname = new Dictionary<string, string>
        {
            {"longtshirt", "tshirt.long" },
            {"cap", "hat.cap" },
            {"beenie", "hat.beenie" },
            {"boonie", "hat.boonie" },
            {"balaclava", "mask.balaclava" },
            {"pipeshotgun", "shotgun.waterpipe" },
            {"woodstorage", "box.wooden" },
            {"ak47", "rifle.ak" },
            {"bearrug", "rug.bear" },
            {"boltrifle", "rifle.bolt" },
            {"bandana", "mask.bandana" },
            {"hideshirt", "attire.hide.vest" },
            {"snowjacket", "jacket.snow" },
            {"buckethat", "bucket.helmet" },
            {"semiautopistol", "pistol.semiauto" },
            {"roadsignvest", "roadsign.jacket" },
            {"roadsignpants", "roadsign.kilt" },
            {"burlappants", "burlap.trousers" },
            {"collaredshirt", "shirt.collared" },
            {"mp5", "smg.mp5" },
            {"sword", "salvaged.sword" },
            {"workboots", "shoes.boots" },
            {"vagabondjacket", "jacket" },
            {"hideshoes", "attire.hide.boots" },
            {"deerskullmask", "deer.skull.mask" },
            {"minerhat", "hat.miner" },
            {"lr300", "rifle.lr300" },
            {"lr300.item", "rifle.lr300" },
            {"burlapgloves", "burlap.gloves" },
            {"burlap.gloves", "burlap.gloves"},
            {"leather.gloves", "burlap.gloves"},
            {"python", "pistol.python" },
            {"m39", "rifle.m39" },
            {"l96", "rifle.l96" },
            {"woodendoubledoor", "door.double.hinged.wood" }
        };

        private void UpdateWorkshopNameConversionList()
        {
            foreach (ItemDefinition item in ItemManager.itemList)
            {
                string workshopName = item.displayName.english.ToLower().Replace("skin", "").Replace(" ", "").Replace("-", "");

                if (!m_WorkshopNameToShortname.ContainsKey(workshopName))
                    m_WorkshopNameToShortname[workshopName] = item.shortname;

                if (!m_WorkshopNameToShortname.ContainsKey(item.shortname))
                    m_WorkshopNameToShortname[item.shortname] = item.shortname;

                if (!m_WorkshopNameToShortname.ContainsKey(item.shortname.Replace(".", "")))
                    m_WorkshopNameToShortname[item.shortname.Replace(".", "")] = item.shortname;
            }

            foreach (Skinnable skin in Skinnable.All.ToList())
            {
                if (string.IsNullOrEmpty(skin.Name) || string.IsNullOrEmpty(skin.ItemName) || m_WorkshopNameToShortname.ContainsKey(skin.Name.ToLower()))
                    continue;

                m_WorkshopNameToShortname[skin.Name.ToLower()] = skin.ItemName.ToLower();
            }
        }

        private void FindValidSkinnableItems()
        {
            foreach (int itemId in ItemSkinDirectory.Instance.skins.Select(x => x.id))
            {
                ItemDefinition itemDefinition = ItemManager.FindItemDefinition(itemId);
                if (itemDefinition != null)
                    m_SkinnableItems.Add(itemDefinition);
            }

            foreach(Skinnable skin in Skinnable.All)
            {
                ItemDefinition itemDefinition = ItemManager.FindItemDefinition(skin.Name);
                if (itemDefinition != null)
                {
                    m_SkinnableItems.Add(itemDefinition);
                    continue;
                }

                itemDefinition = ItemManager.FindItemDefinition(skin.ItemName);
                if (itemDefinition != null)
                    m_SkinnableItems.Add(itemDefinition);
            }
            
            foreach (ItemDefinition itemDefinition in ItemManager.itemList)
            {
                if (itemDefinition.HasSkins || itemDefinition.skins?.Length > 0 || m_WorkshopNameToShortname.ContainsKey(itemDefinition.shortname))
                    m_SkinnableItems.Add(itemDefinition);                
            }
        }
        #endregion

        #region Approved Skins
        private const string PUBLISHED_FILE_DETAILS = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";
        private const string COLLECTION_DETAILS = "https://api.steampowered.com/ISteamRemoteStorage/GetCollectionDetails/v1/";
        private const string ITEMS_BODY = "?key={0}&itemcount={1}";
        private const string ITEM_ENTRY = "&publishedfileids[{0}]={1}";
        private const string COLLECTION_BODY = "?key={0}&collectioncount=1&publishedfileids[0]={1}";

        private bool m_HasChanges = false;
        private bool m_IsReady = false;
        
        private void StartApprovedRequest()
        {
            UpdateWorkshopNameConversionList();

            FindValidSkinnableItems();

            UpdateDefaultCosts();

            UpdateLocalization();

            Steamworks.SteamInventory.OnDefinitionsUpdated -= StartApprovedRequest;

            if (Configuration.Workshop.ApprovedDisabled && !Configuration.Workshop.Enabled)
            {
                PrintError("You have approved skins and workshop skins disabled. This leaves no skins to be shown in the skin shop!");
                return;
            }

            if (Configuration.Workshop.ApprovedDisabled && Configuration.Workshop.Enabled)
            {
                CollectWorkshopSkins();
                return;
            }

            PrintWarning("Retrieving approved skin lists...");

            //FastLoad();
            CollectApprovedSkins();
        }

        private void FastLoad()
        {
            m_IsReady = true;

            Hash<string, HashSet<ulong>> skinList = new Hash<string, HashSet<ulong>>();
            foreach(KeyValuePair<string, Hash<ulong, SkinData>> kvp in m_SkinData.Data)
            {
                HashSet<ulong> skins = new HashSet<ulong>();

                foreach (ulong skin in kvp.Value.Keys)
                    skins.Add(skin);

                skinList.Add(kvp.Key, skins);
            }

            Interface.Oxide.CallHook("OnPlayerSkinsSkinsLoaded", skinList);

            Debug.Log("[PlayerSkins] - Skins processed and ready to use!");
        }

        private void CollectApprovedSkins()
        {
            foreach(KeyValuePair<ulong, Rust.Workshop.ApprovedSkinInfo> item in Rust.Workshop.Approved.All)
            {
                m_SkinsToLoad.Add(item.Value.WorkshopdId);
            }
            
            if (Configuration.Workshop.Enabled)
                CollectWorkshopSkins();
            else SendWorkshopQuery();
        }

        private void CollectWorkshopSkins()
        {
            foreach (KeyValuePair<string, Hash<ulong, SkinData>> skinEntry in m_SkinData.Data)
            {
                foreach (ulong skinId in skinEntry.Value.Keys)
                {
                    if (!m_SkinsToLoad.Contains(skinId))
                        m_SkinsToLoad.Add(skinId);
                }
            }

            SendWorkshopQuery();
        }
        
        private void SendWorkshopQuery(int page = 0, string perm = "")
        {
            int totalPages = Mathf.CeilToInt((float)m_SkinsToLoad.Count / 100f);
            int index = page * 100;
            int limit = Mathf.Min((page + 1) * 100, m_SkinsToLoad.Count);
            string details = string.Format(ITEMS_BODY, Configuration.Workshop.SteamAPIKey, (limit - index));

            for (int i = index; i < limit; i++)
            {
                details += string.Format(ITEM_ENTRY, i - index, m_SkinsToLoad[i]);
            }

            try
            {
                webrequest.Enqueue(PUBLISHED_FILE_DETAILS, details, (code, response) => 
                    ServerMgr.Instance.StartCoroutine(ValidateRequiredSkins(code, response, page + 1, totalPages, false, perm)), this, RequestMethod.POST);
            }
            catch { }
        }

        private void SendWorkshopCollectionQuery(ulong collectionId, bool add, string perm = "")
        {            
            string details = string.Format(COLLECTION_BODY, Configuration.Workshop.SteamAPIKey, collectionId);

            try
            {
                webrequest.Enqueue(COLLECTION_DETAILS, details, (code, response) => 
                    ServerMgr.Instance.StartCoroutine(ProcessCollectionRequest(code, response, add, perm)), this, RequestMethod.POST);
            }
            catch { }
        }
       
        private IEnumerator ValidateRequiredSkins(int code, string response, int page, int totalPages, bool isCollection, string perm)
        {
            if (response != null && code == 200)
            {
                QueryResponse queryRespone = JsonConvert.DeserializeObject<QueryResponse>(response);
                if (queryRespone != null && queryRespone.response != null && queryRespone.response.publishedfiledetails?.Length > 0)
                {
                    Debug.Log($"[PlayerSkins] Processing workshop response. Page: {page} / {totalPages}");

                    foreach (PublishedFileDetails publishedFileDetails in queryRespone.response.publishedfiledetails)
                    {
                        if (publishedFileDetails.tags != null)
                        {
                            foreach (PublishedFileDetails.Tag tag in publishedFileDetails.tags)
                            {
                                if (string.IsNullOrEmpty(tag.tag))
                                    continue;

                                ulong workshopid = Convert.ToUInt64(publishedFileDetails.publishedfileid);

                                string adjTag = tag.tag.ToLower().Replace("skin", "").Replace(" ", "").Replace("-", "").Replace(".item", "");
                                if (m_WorkshopNameToShortname.ContainsKey(adjTag))
                                {                                    
                                    string shortname = m_WorkshopNameToShortname[adjTag];

                                    if (m_IgnoreItems.Contains(shortname))
                                        continue;

                                    bool isValid = IsValid(publishedFileDetails)/* || HasImage(shortname, workshopid)*/;

                                    if (isValid)
                                    {
                                        Hash<ulong, SkinData> skins;
                                        if (!m_SkinData.Data.TryGetValue(shortname, out skins))
                                            m_SkinData.Data.Add(shortname, skins = new Hash<ulong, SkinData>());

                                        SkinData skin;
                                        if (!skins.TryGetValue(workshopid, out skin))
                                        {
                                            skin = skins[workshopid] = new SkinData()
                                            {
                                                cost = Configuration.Purchase.DefaultCosts[shortname],
                                                isDisabled = false,
                                                permission = perm
                                            };

                                            m_HasChanges = true;
                                        }

                                        skin.Title = publishedFileDetails.title;
                                        skin.URL = publishedFileDetails.preview_url;
                                        skin.IsValid = isValid;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            yield return CoroutineEx.waitForEndOfFrame;
            yield return CoroutineEx.waitForEndOfFrame;

            if (page < totalPages)
                SendWorkshopQuery(page, perm);
            else
            {
                if (m_HasChanges)
                {
                    Debug.Log("[PlayerSkins] - The available skin list has been modified");
                    m_SkinData.Save();
                }

                m_IsReady = true;

                Hash<string, HashSet<ulong>> skinList = new Hash<string, HashSet<ulong>>();
                foreach(KeyValuePair<string, Hash<ulong, SkinData>> kvp in m_SkinData.Data)
                {
                    HashSet<ulong> skins = new HashSet<ulong>();

                    foreach (ulong skin in kvp.Value.Keys)
                        skins.Add(skin);

                    skinList.Add(kvp.Key, skins);
                }

                Interface.Oxide.CallHook("OnPlayerSkinsSkinsLoaded", skinList);

                Debug.Log("[PlayerSkins] - Skins processed and ready to use!");
            }
        }

        private IEnumerator ProcessCollectionRequest(int code, string response, bool add, string perm)
        {
            if (response != null && code == 200)
            {
                Debug.Log($"[PlayerSkins] Processing collection response");

                CollectionQueryResponse collectionQuery = JsonConvert.DeserializeObject<CollectionQueryResponse>(response);
                if (collectionQuery == null || !(collectionQuery is CollectionQueryResponse))
                {
                    Puts("Failed to receive a valid workshop collection response");
                    yield break;
                }

                if (collectionQuery.response.resultcount == 0 || collectionQuery.response.collectiondetails == null ||
                    collectionQuery.response.collectiondetails.Length == 0 || collectionQuery.response.collectiondetails[0].result != 1)
                {
                    Puts("Failed to receive a valid workshop collection response");
                    yield break;
                }

                m_SkinsToLoad.Clear();
                foreach (CollectionChild child in collectionQuery.response.collectiondetails[0].children)
                {
                    try
                    {
                        m_SkinsToLoad.Add(Convert.ToUInt64(child.publishedfileid));
                    }
                    catch
                    {
                    }
                }

                if (m_SkinsToLoad.Count == 0)
                {
                    Puts("No valid skin ID's in the specified collection");
                    yield break;
                }

                if (add)
                    SendWorkshopQuery(0, perm);
                else RemoveSkins();
            }
            else Debug.Log($"[PlayerSkins] Collection response failed. Error code {code}");
        }

        private void RemoveSkins()
        {
            int removedCount = 0;
            for (int y = m_SkinData.Data.Count - 1; y >= 0; y--)
            {
                KeyValuePair<string, Hash<ulong, SkinData>> skin = m_SkinData.Data.ElementAt(y);

                for (int i = 0; i < m_SkinsToLoad.Count; i++)
                {
                    if (skin.Value.ContainsKey(m_SkinsToLoad[i]))
                    {
                        skin.Value.Remove(m_SkinsToLoad[i]);
                        removedCount++;
                    }
                }

            }

            m_SkinData.Save();
            Puts($"Removed {removedCount} skins");
        }
        #endregion

        #region API Helpers
        private bool ContainsKeyword(string title)
        {
            foreach (string keyword in Configuration.Workshop.Filter)
            {
                if (title.ToLower().Contains(keyword.ToLower()))
                    return true;
            }
            return false;
        }

        private bool IsValid(PublishedFileDetails item)
        {
            if (ContainsKeyword(item.title))
                return false;

            if (string.IsNullOrEmpty(item.preview_url))
                return false;

            if (item.tags == null)
                return false;

            return true;
        }

        private void GetSkinnableShortnames(List<string> list)
        {
            foreach (KeyValuePair<string, Hash<ulong, SkinData>> skin in m_SkinData.Data)
            {
                if (skin.Value.Count == 0 || !skin.Value.Any(x => x.Value.IsValid))
                    continue;

                if (Configuration.Shop.BlockedItems.Contains(skin.Key) || m_IgnoreItems.Contains(skin.Key))
                    continue;

                list.Add(skin.Key);
            }

            list.Sort(delegate (string a, string b)
            {
                string displayNameA = GetString(a, string.Empty);
                string displayNameB = GetString(b, string.Empty);

                return displayNameA.CompareTo(displayNameB);
            });
        }

        private void GetValidSkins(List<KeyValuePair<string, ulong>> list, UIUser uiUser, UserData userData)
        {
            if (!string.IsNullOrEmpty(uiUser.SearchFilter))
            {
                if (uiUser.ShowOwned)
                {
                    foreach (KeyValuePair<string, List<ulong>> kvp in userData.purchasedSkins)
                    {
                        Hash<ulong, SkinData> skinList;
                        if (!m_SkinData.Data.TryGetValue(kvp.Key, out skinList))
                            continue;
                        
                        foreach (KeyValuePair<ulong, SkinData> idData in skinList)
                        {
                            if (kvp.Value.Contains(idData.Key) && idData.Value.Title.Contains(uiUser.SearchFilter, CompareOptions.OrdinalIgnoreCase))
                                list.Add(new KeyValuePair<string, ulong>(kvp.Key, idData.Key));
                        }
                    }
                }
                else
                {
                    foreach (KeyValuePair<string, Hash<ulong, SkinData>> kvp in m_SkinData.Data)
                    {
                        foreach (KeyValuePair<ulong, SkinData> idData in kvp.Value)
                        {
                            if (idData.Value.Title.Contains(uiUser.SearchFilter, CompareOptions.OrdinalIgnoreCase))
                            {
                                list.Add(new KeyValuePair<string, ulong>(kvp.Key, idData.Key));
                            }
                        }
                    }
                }
            }
            else
            {
                Hash<ulong, SkinData> skinList;
                if (!m_SkinData.Data.TryGetValue(uiUser.ItemShortname, out skinList))
                    return;

                if (uiUser.ShowOwned)
                {
                    List<ulong> purchasedSkinIds;
                    if (userData.purchasedSkins.TryGetValue(uiUser.ItemShortname, out purchasedSkinIds))
                    {
                        for (int i = 0; i < purchasedSkinIds.Count; i++)
                        {
                            ulong skinId = purchasedSkinIds[i];

                            SkinData skin;
                            if (skinList.TryGetValue(skinId, out skin))
                            {
                                if (!skin.isDisabled && skin.IsValid && !m_ExcludedSkins.Data.Contains(skinId))
                                {
                                    if (!string.IsNullOrEmpty(uiUser.SearchFilter) && !skin.Title.Contains(uiUser.SearchFilter, CompareOptions.OrdinalIgnoreCase))
                                        continue;

                                    list.Add(new KeyValuePair<string, ulong>(uiUser.ItemShortname, skinId));
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (KeyValuePair<ulong, SkinData> skin in skinList)
                    {
                        if (!skin.Value.isDisabled && skin.Value.IsValid && !m_ExcludedSkins.Data.Contains(skin.Key))
                        {
                            if (!string.IsNullOrEmpty(uiUser.SearchFilter) && !skin.Value.Title.Contains(uiUser.SearchFilter, CompareOptions.OrdinalIgnoreCase))
                                continue;

                            list.Add(new KeyValuePair<string, ulong>(uiUser.ItemShortname, skin.Key));
                        }
                    }
                }
            }
        }
        #endregion
        
         #region Chat Commands
        private void RegisterChatCommands()
        {
            cmd.AddChatCommand(Configuration.Commands.DefaultCommand, this, cmdSkin);
            cmd.AddChatCommand(Configuration.Commands.ReskinCommand, this, cmdReSkin);
            cmd.AddChatCommand(Configuration.Commands.ShopCommand, this, cmdSkinShop);

        }
        private void cmdSkin(BasePlayer player, string command, string[] args)
        {            
            if (!m_IsReady)
            {
                SendReply(player, "Waiting for item icons to finish downloading...");
                return;
            }

            if (!m_UserData.Data.ContainsKey(player.userID))
                m_UserData.Data.Add(player.userID, new UserData());

            if (args.Length == 0)
            {
                if (!player.HasPermission(RESKIN_PERMISSION))
                {
                    SendReply(player, "You do not have permission to use this command");
                    return;
                }

                if (Configuration.Shop.GiveItemOnPurchase)
                    return;

                if (Configuration.Reskin.DisableCommand)
                {
                    SendReply(player, "You can only access the re-skin menu via a re-skin NPC");
                    return;
                }
                OpenReskinMenu(player);
            }
            else
            {
                if (args[0].ToLower() != "shop")
                {
                    SendReply(player, "/skin - Open the re-skin menu");
                    SendReply(player, "/skin shop - Open the skin shop");
                    return;
                }

                if (!player.HasPermission(SHOP_PERMISSION))
                {
                    SendReply(player, "You do not have permission to use this command");
                    return;
                }

                if (Configuration.Shop.DisableCommand)
                {
                    SendReply(player, "You can only access the skin shop menu via a skin shop NPC");
                    return;
                }

                BaseContainer root = BaseContainer.Create(PS_UI_MOUSE, Layer.Hud, Anchor.Center, Offset.Default)
                    .NeedsCursor()
                    .NeedsKeyboard();
			    
                ChaosUI.Show(player, root);
                
                OpenSkinShop(player);
            }
        }

        private void cmdReSkin(BasePlayer player, string command, string[] args)
        {
            if (!m_IsReady)
            {
                SendReply(player, "Waiting for item icons to finish downloading...");
                return;
            }

            if (!m_UserData.Data.ContainsKey(player.userID))
                m_UserData.Data.Add(player.userID, new UserData());

            if (!player.HasPermission(RESKIN_PERMISSION))
            {
                SendReply(player, "You do not have permission to use this command");
                return;
            }

            if (Configuration.Shop.GiveItemOnPurchase)
                return;

            if (Configuration.Reskin.DisableCommand)
            {
                SendReply(player, "You can only access the re-skin menu via a re-skin NPC");
                return;
            }
            OpenReskinMenu(player);
        }

        private void cmdSkinShop(BasePlayer player, string command, string[] args)
        {
            if (!m_IsReady)
            {
                SendReply(player, "Waiting for item icons to finish downloading...");
                return;
            }

            if (!m_UserData.Data.ContainsKey(player.userID))
                m_UserData.Data.Add(player.userID, new UserData());
            
            if (!player.HasPermission(SHOP_PERMISSION))
            {
                SendReply(player, "You do not have permission to use this command");
                return;
            }

            if (Configuration.Shop.DisableCommand)
            {
                SendReply(player, "You can only access the skin shop menu via a skin shop NPC");
                return;
            }
            
            BaseContainer root = BaseContainer.Create(PS_UI_MOUSE, Layer.Hud, Anchor.Center, Offset.Default)
                .NeedsCursor()
                .NeedsKeyboard();
			    
            ChaosUI.Show(player, root);

            OpenSkinShop(player);
        }
        #endregion

        #region Console Commands
        [ConsoleCommand("playerskins.skins")]
        private void ccmdSkinManager(ConsoleSystem.Arg arg)
        {            
            if (arg.Connection != null)
            {
                if (arg.Connection.authLevel < 2)
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            if (string.IsNullOrEmpty(Configuration.Workshop.SteamAPIKey))
            {
                SendReply(arg, "No steam API key has been set");
                return;
            }

            if (arg.Args == null || arg.Args.Length < 3)
            {
                SendReply(arg, "playerskins.skins import skin <skin ID> - Import the specified workshop skin using its workshop ID. Type multiple ID's here to process them all at once");
                SendReply(arg, "playerskins.skins import collection <collection ID> <opt:permission> - Import the specified workshop skin collection. Optional add a permission to add to any new skins collected");
                SendReply(arg, "playerskins.skins remove skin <skin ID> - Remove the specified skin from the skin shop. Type multiple ID's here to process them all at once");
                SendReply(arg, "playerskins.skins remove collection <collection ID> - Remove the specified skin collection from the skin shop");
                return;
            }

            if (!Configuration.Workshop.Enabled)
            {
                SendReply(arg, "You have workshop disabled in your config. The playerskins.skins commands are unavailable when workshop is disabled");
                return;
            }

            switch (arg.GetString(0).ToLower())
            {
                case "import":
                    {
                        if (arg.Args.Length < 2)
                        {
                            SendReply(arg, "Invalid syntax. Type 'playerskins.skins' for more information");
                            return;
                        }

                        if (arg.Args.Length < 3)
                        {
                            SendReply(arg, "You must enter a workshop skin ID or collection ID");
                            return;
                        }

                        if (arg.Args[1].ToLower() == "skin")
                        {
                            m_SkinsToLoad.Clear();

                            for (int i = 2; i < arg.Args.Length; i++)
                            {
                                ulong skinId;
                                if (ulong.TryParse(arg.Args[i], out skinId))
                                    m_SkinsToLoad.Add(skinId);
                                else SendReply(arg, $"Invalid skin ID : {arg.Args[i]}");
                            }

                            if (m_SkinsToLoad.Count > 0)
                                SendWorkshopQuery();
                            else SendReply(arg, "No valid ID's entered");
                        }
                        else if (arg.Args[1].ToLower() == "collection")
                        {
                            m_SkinsToLoad.Clear();
                            ulong collectionId;
                            string perm = arg.Args.Length == 4 ? arg.Args[3] : string.Empty;
                            if (ulong.TryParse(arg.Args[2], out collectionId))
                            {
                                if (!string.IsNullOrEmpty(perm))
                                {
                                    if (!perm.StartsWith("playerskins."))
                                        perm = "playerskins." + perm;

                                    if (!Configuration.Shop.Permissions.Contains(perm))
                                    {
                                        Configuration.Shop.Permissions.Add(perm);
                                        SaveConfiguration();
                                        
                                        permission.RegisterPermission(perm, this);
                                    }
                                }
                                
                                SendWorkshopCollectionQuery(collectionId, true, perm);
                            }
                            else SendReply(arg, "Invalid collection ID entered");
                        }
                        else
                        {
                            SendReply(arg, "Invalid syntax. Type 'playerskins.skins' for more information");
                            return;
                        }
                    }
                    return;
                case "remove":
                    {
                        if (arg.Args.Length < 2)
                        {
                            SendReply(arg, "Invalid syntax. Type 'playerskins.skins' for more information");
                            return;
                        }

                        if (arg.Args.Length < 3)
                        {
                            SendReply(arg, "You must enter a workshop skin ID or collection ID");
                            return;
                        }

                        if (arg.Args[1].ToLower() == "skin")
                        {
                            m_SkinsToLoad.Clear();
                            for (int i = 2; i < arg.Args.Length; i++)
                            {
                                ulong skinId;
                                if (ulong.TryParse(arg.Args[i], out skinId))
                                    m_SkinsToLoad.Add(skinId);
                                else SendReply(arg, $"Invalid skin ID : {arg.Args[i]}");
                            }

                            RemoveSkins();
                        }
                        else if (arg.Args[1].ToLower() == "collection")
                        {
                            ulong collectionId;
                            if (ulong.TryParse(arg.Args[2], out collectionId))
                            {
                                SendWorkshopCollectionQuery(collectionId, false);
                            }
                            else SendReply(arg, "Invalid collection ID entered");
                        }
                        else
                        {
                            SendReply(arg, "Invalid syntax. Type 'playerskins.skins' for more information");
                            return;
                        }
                    }
                    return;
                default:
                    SendReply(arg, "Invalid syntax!");
                    break;
            }
        }

        [ConsoleCommand("playerskins.setprice")]
        private void ccmdSetSkinPrice(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                if (arg.Connection.authLevel < 2)
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            if (arg.Args == null || arg.Args.Length != 2)
            {
                SendReply(arg, "playerskins.setprice <item shortname> <amount> - Set the price for all skins for the specified item");
                SendReply(arg, "playerskins.setprice all <amount> - Set the price for all skins for all items");
                return;
            }

            int amount = 0;
            if (!int.TryParse(arg.GetString(1), out amount))
            {
                SendReply(arg, "You must enter a number to set the price");
                return;
            }

            string shortname = arg.GetString(0);

            if (shortname.ToLower() == "all")
            {
                foreach (Hash<ulong, SkinData> item in m_SkinData.Data.Values)
                {
                    foreach (SkinData skin in item.Values)
                        skin.cost = amount;
                }

                SendReply(arg, $"You have set all skin costs to {amount}");
            }
            else
            {
                Hash<ulong, SkinData> data;
                if (!m_SkinData.Data.TryGetValue(shortname, out data))
                {
                    SendReply(arg, $"Either an invalid shortname was entered, or there are no skins for the specified item : {shortname}");
                    return;
                }

                foreach (SkinData skin in data.Values)
                    skin.cost = amount;

                SendReply(arg, $"You have set all {shortname} skin costs to {amount}");
            }

            m_SkinData.Save();
        }

        [ConsoleCommand("playerskins.giveskin")]
        private void ccmdGiveSkin(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null)
            {
                if (arg.Connection.authLevel < 2)
                {
                    SendReply(arg, "You do not have permission to use this command");
                    return;
                }
            }

            if (arg.Args == null || arg.Args.Length != 3)
            {
                SendReply(arg, "playerskins.giveskin <userID> <item shortname> <skinID> - Give a user the specified skin");
                return;
            }

            ulong userID = arg.GetULong(0);
            string shortname = arg.GetString(1);
            ulong skinID = arg.GetULong(2);

            if (userID == 0UL)
            {
                SendReply(arg, "The user ID you entered is invalid");
                return;
            }

            if (!m_UserData.Data.ContainsKey(userID))
            {
                SendReply(arg, "The specified user does not have any stored data");
                return;
            }

            if (!ItemManager.itemDictionaryByName.ContainsKey(shortname))
            {
                SendReply(arg, "The item shortname you entered is invalid");
                return;
            }

            if (skinID == 0UL)
            {
                SendReply(arg, "The skin ID you entered is invalid");
                return;
            }

            Hash<ulong, SkinData> itemData;
            if (!m_SkinData.Data.TryGetValue(shortname, out itemData) || !itemData.ContainsKey(skinID))
            {
                SendReply(arg, "The skin ID you entered is not available in the skin shop");
                return;
            }

            List<ulong> skins;
            if (!m_UserData.Data[userID].purchasedSkins.TryGetValue(shortname, out skins))            
                skins = m_UserData.Data[userID].purchasedSkins[shortname] = new List<ulong>();

            if (skins.Contains(skinID))
            {
                SendReply(arg, "The user has already purchased that skin");
                return;
            }

            skins.Add(skinID);

            m_UserData.Save();
        }
        #endregion
        
        #region UI
        public enum DisplayMode { None, Full, Minimalist }
        
        private CommandCallbackHandler m_CallbackHandler;

        private const string PS_UI = "playerskins.ui";
        private const string PS_UI_MOUSE = "playerskins.ui.mouse";
        private const string PS_UI_POPUP = "playerskins.ui.popup";
        
        private string m_MagnifyImage;

        private readonly Hash<ulong, UIUser> m_UIUsers = new Hash<ulong, UIUser>();

        private void SetupUIComponents()
        {
            m_CallbackHandler = new CommandCallbackHandler(this);

            m_BackgroundStyle = new Style
            {
                ImageColor = new Color(Configuration.Colors.Background.Hex, Configuration.Colors.Background.Alpha),
                Material = Materials.BackgroundBlur,
                Sprite = Sprites.Background_Rounded,
                ImageType = Image.Type.Tiled
            };

            m_PanelStyle = new Style
            {
                ImageColor = new Color(Configuration.Colors.Panel.Hex, Configuration.Colors.Panel.Alpha),
                Sprite = Sprites.Background_Rounded,
                ImageType = Image.Type.Tiled
            };
            
            m_OwnedPanelStyle = new Style
            {
                ImageColor = new Color(Configuration.Colors.Highlight.Hex, 0.35f),
                Sprite = Sprites.Background_Rounded,
                ImageType = Image.Type.Tiled
            };

            m_ButtonStyle = new Style
            {
                ImageColor = new Color(Configuration.Colors.Button.Hex, Configuration.Colors.Button.Alpha),
                Sprite = Sprites.Background_Rounded,
                ImageType = Image.Type.Tiled,
                Alignment = TextAnchor.MiddleCenter,
                FontSize = 14
            };
            
            m_ButtonDisabledStyle = new Style
            {
	            ImageColor = new Color(Configuration.Colors.Button.Hex, 0.8f),
	            Sprite = Sprites.Background_Rounded,
	            ImageType = Image.Type.Tiled,
	            Alignment = TextAnchor.MiddleCenter,
	            FontColor = new Color(1f, 1f, 1f, 0.2f),
	            FontSize = 14
            };
            
            m_TitleStyle = new Style
            {
                FontSize = 18,
                Font = Font.PermanentMarker,
                Alignment = TextAnchor.MiddleLeft,
                WrapMode = VerticalWrapMode.Overflow
            };
            
            m_ToggleLabelStyle = new Style
            {
                FontSize = 40,
                Alignment = TextAnchor.MiddleCenter,
                WrapMode = VerticalWrapMode.Overflow,
                FontColor = new Color(Configuration.Colors.Highlight.Hex, Configuration.Colors.Highlight.Alpha)
            };
            
            m_OutlineGreen = new OutlineComponent(new Color(Configuration.Colors.Highlight.Hex, Configuration.Colors.Highlight.Alpha));
            m_OutlineRed = new OutlineComponent(new Color(Configuration.Colors.Close.Hex, Configuration.Colors.Close.Alpha));
        }
        
        #region Styles
        private Style m_BackgroundStyle;
        private Style m_PanelStyle;
        private Style m_OwnedPanelStyle;
        private Style m_ButtonStyle;
        private Style m_ButtonDisabledStyle;
        private Style m_TitleStyle;
        private Style m_ToggleLabelStyle;
        
        private OutlineComponent m_OutlineGreen;
        private OutlineComponent m_OutlineRed;
        #endregion
        
        #region Layout Groups
        private VerticalLayoutGroup m_ItemListLayout = new VerticalLayoutGroup(18)
        {
	        Area = new Area(-70f, -253.5f, 70f, 253.5f),
	        Spacing = new Spacing(5f, 5f),
	        Padding = new Padding(5f, 0f, 5f, 0f),
	        Corner = Corner.TopLeft,
        };
        
        private readonly GridLayoutGroup m_ItemGridFull = new GridLayoutGroup(10, 6, Axis.Horizontal)
        {
            Area = new Area(-462.5f, -270f, 462.5f, 270f),
            Spacing = new Spacing(5f, 5f),
            Padding = new Padding(5f, 5f, 5f, 5f),
            Corner = Corner.Centered,
        };

        private readonly GridLayoutGroup m_ItemGridMinimal = new GridLayoutGroup(3, 6, Axis.Horizontal)
        {
            Area = new Area(-142.5f, -270f, 142.5f, 270f),
            Spacing = new Spacing(5f, 5f),
            Padding = new Padding(5f, 5f, 5f, 5f),
            Corner = Corner.Centered,
        };
        
        private GridLayoutGroup m_ReskinItemGrid = new GridLayoutGroup(5, 2, Axis.Horizontal)
        {
            Area = new Area(-185.5f, -77.5f, 185.5f, 77.5f),
            Spacing = new Spacing(5f, 5f),
            Padding = new Padding(5f, 5f, 5f, 5f),
            Corner = Corner.Centered,
        };
        
        private VerticalLayoutGroup m_PermissionLayout = new VerticalLayoutGroup()
        {
            Area = new Area(-120f, -272.5f, 120f, 272.5f),
            Spacing = new Spacing(0f, 5f),
            Padding = new Padding(0f, 0f, 0f, 0f),
            Corner = Corner.TopLeft,
            FixedSize = new Vector2(240, 20),
            FixedCount = new Vector2Int(1, 22)
        };
        #endregion
        
        #region UI User
        private class UIUser
        {
	        public readonly BasePlayer Player;

	        public DisplayMode DisplayMode;
	        public string ItemShortname = string.Empty;
	        public int CategoryPage = 0;
	        public int GridPage = 0;
	        public string SearchFilter = string.Empty;
            public bool ShowOwned = false;
            public bool AdminMode = false;
            
	        public UIUser(BasePlayer player, DisplayMode userDisplayMode)
	        {
		        this.Player = player;
                DisplayMode = m_ForcedDisplayMode != DisplayMode.None ? m_ForcedDisplayMode : userDisplayMode;
            }

	        public void Reset()
	        {
                CategoryPage = 0;
                GridPage = 0;
		        ItemShortname = string.Empty;
		        SearchFilter = string.Empty;
                ShowOwned = false;
                AdminMode = false;
            }
        }
        #endregion
        
        #region UI

        private readonly Offset m_FullOffset = new Offset(-540f, -310f, 540f, 310f);
        private readonly Offset m_MinimalOffset = new Offset(-540f, -310f, -100f, 310f);
        
        #region Skin Shop
        private void OpenSkinShop(BasePlayer player)
        {
            UserData userData = m_UserData.Data[player.userID];
            
	        UIUser uiUser;
	        if (!m_UIUsers.TryGetValue(player.userID, out uiUser))
		        uiUser = m_UIUsers[player.userID] = new UIUser(player, userData?.displayMode ?? DisplayMode.Full);
	        
	        BaseContainer root = ImageContainer.Create(PS_UI, Layer.Overall, Anchor.Center, uiUser.DisplayMode == DisplayMode.Full ? m_FullOffset : m_MinimalOffset)
		        .WithStyle(m_BackgroundStyle)
		        .WithChildren(parent =>
		        {
			        CreateTitleBar(uiUser, parent);
			        
			        CreateItemSelector(uiUser, parent);

			        CreateItemGrid(uiUser, parent, userData);
                });
                
			ChaosUI.Destroy(player, PS_UI);
			ChaosUI.Show(player, root);
        }

        private void CreateTitleBar(UIUser uiUser, BaseContainer parent)
        {
	        ImageContainer.Create(parent, Anchor.TopStretch, new Offset(5f, -35f, -5f, -5f))
		        .WithStyle(m_PanelStyle)
		        .WithChildren(titleBar =>
		        {
			        TextContainer.Create(titleBar, Anchor.CenterLeft, new Offset(5f, -15f, 205f, 15f))
				        .WithText(Title)
				        .WithStyle(m_TitleStyle);

			        ImageContainer.Create(titleBar, Anchor.CenterRight, new Offset(-55f, -10f, -5f, 10f))
				        .WithStyle(m_ButtonStyle)
				        .WithOutline(m_OutlineRed)
				        .WithChildren(exit =>
				        {
					        TextContainer.Create(exit, Anchor.FullStretch, Offset.zero)
						        .WithText(GetString("UI.Exit", uiUser.Player))
						        .WithAlignment(TextAnchor.MiddleCenter);

					        ButtonContainer.Create(exit, Anchor.FullStretch, Offset.zero)
						        .WithColor(Color.Clear)
						        .WithCallback(m_CallbackHandler, arg =>
						        {
							        ChaosUI.Destroy(uiUser.Player, PS_UI);
							        ChaosUI.Destroy(uiUser.Player, PS_UI_POPUP);
                                    ChaosUI.Destroy(uiUser.Player, PS_UI_MOUSE);
							        m_UIUsers.Remove(uiUser.Player.userID);
						        }, $"{uiUser.Player.userID}.exit");

				        });

                    if (m_ForcedDisplayMode == DisplayMode.None)
                    {
                        // Toggle small big UI
                        ImageContainer.Create(titleBar, Anchor.CenterRight, new Offset(-90f, -10f, -60f, 10f))
                            .WithStyle(m_ButtonStyle)
                            .WithChildren(backButton =>
                            {
                                TextContainer.Create(backButton, Anchor.FullStretch, Offset.zero)
                                    .WithText(uiUser.DisplayMode == DisplayMode.Full ? "<<<" : ">>>")
                                    .WithAlignment(TextAnchor.MiddleCenter);

                                ButtonContainer.Create(backButton, Anchor.FullStretch, Offset.zero)
                                    .WithColor(Color.Clear)
                                    .WithCallback(m_CallbackHandler, arg =>
                                    {
                                        uiUser.GridPage = 0;
                                        uiUser.DisplayMode = uiUser.DisplayMode == DisplayMode.Full ? DisplayMode.Minimalist : DisplayMode.Full;
                                        OpenSkinShop(uiUser.Player);
                                    }, $"{uiUser.Player.userID}.displaymode");
                            });
                    }
                });
        }

        private void CreateItemSelector(UIUser uiUser, BaseContainer parent)
        {
	        ImageContainer.Create(parent, Anchor.LeftStretch, new Offset(5f, 5f, 145f, -40f))
		        .WithStyle(m_PanelStyle)
		        .WithChildren(itemMenu =>
                {
                    List<string> list = Facepunch.Pool.GetList<string>();
                    GetSkinnableShortnames(list);
                    
			        ImageContainer.Create(itemMenu, Anchor.TopCenter, new Offset(-65f, -28.44444f, 65f, -5.000017f))
				        .WithStyle(uiUser.CategoryPage > 0 ? m_ButtonStyle : m_ButtonDisabledStyle)
				        .WithChildren(back =>
                        {
                            TextContainer.Create(back, Anchor.FullStretch, Offset.zero)
                                .WithText(GetString("UI.Button.Up", uiUser.Player))
                                .WithStyle(uiUser.CategoryPage > 0 ? m_ButtonStyle : m_ButtonDisabledStyle);

                            if (uiUser.CategoryPage > 0)
                            {
                                ButtonContainer.Create(back, Anchor.FullStretch, Offset.zero)
                                    .WithColor(Color.Clear)
                                    .WithCallback(m_CallbackHandler, arg =>
                                    {
                                        uiUser.CategoryPage--;
                                        OpenSkinShop(uiUser.Player);
                                    }, $"{uiUser.Player.UserIDString}.category.back");
                            }
                        });

			        BaseContainer.Create(itemMenu, Anchor.FullStretch, new Offset(0f, 34f, 0f, -34f))
				        .WithLayoutGroup(m_ItemListLayout, list, uiUser.CategoryPage, (int i, string t, BaseContainer itemList, Anchor anchor, Offset offset) =>
				        {
					        BaseContainer button = ImageContainer.Create(itemList, anchor, offset)
						        .WithStyle(m_ButtonStyle)
						        .WithChildren(commands =>
						        {
							        TextContainer.Create(commands, Anchor.FullStretch, Offset.zero)
								        .WithSize(13)
								        .WithText(GetString(t, uiUser.Player))
								        .WithAlignment(TextAnchor.MiddleCenter);

							        ButtonContainer.Create(commands, Anchor.FullStretch, Offset.zero)
								        .WithColor(Color.Clear)
                                        .WithCallback(m_CallbackHandler, arg =>
                                        {
                                            uiUser.GridPage = 0;
                                            uiUser.SearchFilter = string.Empty;
                                            uiUser.ItemShortname = t;
                                            OpenSkinShop(uiUser.Player);
                                        }, $"{uiUser.Player.UserIDString}.category.{t}");

						        });

                            if (t == uiUser.ItemShortname)
                                button.WithOutline(m_OutlineGreen);
                        });

                    bool hasNextPage = m_ItemListLayout.HasNextPage(uiUser.CategoryPage, list.Count);
                    
			        ImageContainer.Create(itemMenu, Anchor.BottomCenter, new Offset(-65f, 5.000002f, 65f, 28.44446f))
				        .WithStyle(hasNextPage ? m_ButtonStyle : m_ButtonDisabledStyle)
				        .WithChildren(next =>
				        {
					        TextContainer.Create(next, Anchor.FullStretch, Offset.zero)
						        .WithText(GetString("UI.Button.Down", uiUser.Player))
						        .WithStyle(hasNextPage ? m_ButtonStyle : m_ButtonDisabledStyle);

                            if (hasNextPage)
                            {
                                ButtonContainer.Create(next, Anchor.FullStretch, Offset.zero)
                                    .WithColor(Color.Clear)
                                    .WithCallback(m_CallbackHandler, arg =>
                                    {
                                        uiUser.CategoryPage++;
                                        OpenSkinShop(uiUser.Player);
                                    }, $"{uiUser.Player.UserIDString}.category.next");
                            }
                        });
                    
                    Facepunch.Pool.FreeList(ref list);
                });
        }
       
        private void CreateItemGrid(UIUser uiUser, BaseContainer parent, UserData userData)
        {
            int count = 0;
            if (string.IsNullOrEmpty(uiUser.ItemShortname) && string.IsNullOrEmpty(uiUser.SearchFilter))
            {
                TextContainer.Create(parent, Anchor.FullStretch, new Offset(150f, 40f, -5f, -40f))
                    .WithText(GetString("UI.NoShortname", uiUser.Player))
                    .WithAlignment(TextAnchor.MiddleCenter);
            }
            else
            {
                List<KeyValuePair<string, ulong>> list = Facepunch.Pool.GetList<KeyValuePair<string, ulong>>();

                if (!string.IsNullOrEmpty(uiUser.ItemShortname) || !string.IsNullOrEmpty(uiUser.SearchFilter))
                    GetValidSkins(list, uiUser, userData);

                if (list.Count == 0)
                {
                    TextContainer.Create(parent, Anchor.FullStretch, new Offset(150f, 40f, -5f, -40f))
                        .WithText(GetString("UI.NoSkinsFound", uiUser.Player))
                        .WithAlignment(TextAnchor.MiddleCenter);
                }
                else
                {
                    count = list.Count;
                    
                    ImageContainer.Create(parent, Anchor.FullStretch, new Offset(150f, 40f, -5f, -40f))
                        .WithStyle(m_PanelStyle)
                        .WithLayoutGroup(uiUser.DisplayMode == DisplayMode.Full ? m_ItemGridFull : m_ItemGridMinimal, list, uiUser.GridPage, (int i, KeyValuePair<string, ulong> t, BaseContainer itemGrid, Anchor anchor, Offset offset) =>
                        {
                            ImageContainer.Create(itemGrid, anchor, offset)
                                .WithStyle(userData.IsOwned(t.Key, t.Value) && !uiUser.ShowOwned ? m_OwnedPanelStyle : m_PanelStyle)
                                .WithChildren(template =>
                                {
                                    ImageContainer.Create(template, Anchor.Center, new Offset(-42f, -42f, 42f, 42f))
                                        .WithIcon(m_ShortnameToItemId[t.Key], t.Value);

                                    ButtonContainer.Create(template, Anchor.FullStretch, Offset.zero)
                                        .WithColor(Color.Clear)
                                        .WithCallback(m_CallbackHandler, arg => { CreateItemView(uiUser, userData, t); }, $"{uiUser.Player.userID}.selectskin.{i}");

                                });
                        });
                }
                
                Facepunch.Pool.FreeList(ref list);
            }

            CreateFooterBar(uiUser, parent, count);
        }

        private void CreateFooterBar(UIUser uiUser, BaseContainer parent, int listCount)
        {
            ImageContainer.Create(parent, Anchor.BottomStretch, new Offset(150f, 5f, -5f, 35f))
                .WithStyle(m_PanelStyle)
                .WithChildren(footer =>
                {
                    ImageContainer.Create(footer, Anchor.CenterLeft, new Offset(5f, -10f, 35f, 10f))
                        .WithStyle(uiUser.GridPage > 0 ? m_ButtonStyle : m_ButtonDisabledStyle)
                        .WithChildren(backButton =>
                        {
                            TextContainer.Create(backButton, Anchor.FullStretch, Offset.zero)
                                .WithText("<<<")
                                .WithStyle(uiUser.GridPage > 0 ? m_ButtonStyle : m_ButtonDisabledStyle);

                            if (uiUser.GridPage > 0)
                            {
                                ButtonContainer.Create(backButton, Anchor.FullStretch, Offset.zero)
                                    .WithColor(Color.Clear)
                                    .WithCallback(m_CallbackHandler, arg =>
                                    {
                                        uiUser.GridPage--;
                                        OpenSkinShop(uiUser.Player);
                                    }, $"{uiUser.Player.UserIDString}.grid.back");

                            }
                        });

                    bool hasNextPage = (uiUser.DisplayMode == DisplayMode.Full ? m_ItemGridFull : m_ItemGridMinimal).HasNextPage(uiUser.GridPage, listCount);

                    ImageContainer.Create(footer, Anchor.CenterRight, new Offset(-35f, -10f, -5f, 10f))
                        .WithStyle(m_ButtonStyle)
                        .WithChildren(nextButton =>
                        {
                            TextContainer.Create(nextButton, Anchor.FullStretch, Offset.zero)
                                .WithText(">>>")
                                .WithStyle(hasNextPage ? m_ButtonStyle : m_ButtonDisabledStyle);

                            if (hasNextPage)
                            {
                                ButtonContainer.Create(nextButton, Anchor.FullStretch, Offset.zero)
                                    .WithColor(Color.Clear)
                                    .WithCallback(m_CallbackHandler, arg =>
                                    {
                                        uiUser.GridPage++;
                                        OpenSkinShop(uiUser.Player);
                                    }, $"{uiUser.Player.UserIDString}.grid.next");
                            }
                        });
                    
                    TextContainer.Create(footer, Anchor.CenterLeft, new Offset(45f, -10f, 205f, 10f))
                        .WithText(FormatString("UI.Balance", uiUser.Player, GetUserBalance(uiUser.Player), GetString(m_CurrencyType.ToString(), uiUser.Player)))
                        .WithAlignment(TextAnchor.MiddleLeft);

                    BaseContainer minimalFooter = null;
                    bool isMinimalMode = uiUser.DisplayMode == DisplayMode.Minimalist;
                    
                    if (isMinimalMode)
                    {
                        minimalFooter = ImageContainer.Create(parent, Anchor.BottomStretch, new Offset(0f, -35f, 0f, 5f))
                            .WithColor(m_BackgroundStyle.ImageColor)
                            .WithMaterial(Materials.BackgroundBlur)
                            .WithImageType(Image.Type.Tiled)
                            .WithChildren(minimal =>
                            {
                                ImageContainer.Create(minimal, Anchor.FullStretch, new Offset(5f, 5f, -5f, -5f))
                                    .WithStyle(m_PanelStyle);
                            });
                    }

                    CreateFooterSearchBar(uiUser, uiUser.DisplayMode == DisplayMode.Full ? footer : minimalFooter, isMinimalMode);
                    CreateFooterOwnedToggle(uiUser, uiUser.DisplayMode == DisplayMode.Full ? footer : minimalFooter, isMinimalMode);
                });
        }

        private void CreateFooterSearchBar(UIUser uiUser, BaseContainer parent, bool minimal)
        {
            if (!string.IsNullOrEmpty(m_MagnifyImage))
            {
                RawImageContainer.Create(parent, Anchor.CenterRight, minimal ? new Offset(-235f, -10f, -215f, 10f) : new Offset(-265f, -10f, -245f, 10f))
                    .WithPNG(m_MagnifyImage);
            }

            ImageContainer.Create(parent, Anchor.CenterRight, minimal ? new Offset(-210f, -10f, -10f, 10f) : new Offset(-240f, -10f, -40f, 10f))
                .WithStyle(m_ButtonStyle)
                .WithChildren(searchInput =>
                {
                    InputFieldContainer.Create(searchInput, Anchor.FullStretch, new Offset(5f, 0f, -5f, 0f))
                        .WithText(uiUser.SearchFilter)
                        .WithAlignment(TextAnchor.MiddleLeft)
                        .WithCallback(m_CallbackHandler, arg =>
                        {
                            uiUser.SearchFilter = arg.Args.Length > 1 ? string.Join(" ", arg.Args.Skip(1)) : string.Empty;
                            uiUser.GridPage = 0;
                            OpenSkinShop(uiUser.Player);
                        });
                });
        }

        private void CreateFooterOwnedToggle(UIUser uiUser, BaseContainer parent, bool minimal)
        {
            ImageContainer.Create(parent, Anchor.CenterLeft, minimal ? new Offset(10f, -10f, 30f, 10f) : new Offset(215f, -10f, 235f, 10f))
                .WithStyle(m_ButtonStyle)
                .WithChildren(ownedToggle =>
                {
                    if (uiUser.ShowOwned)
                    {
                        TextContainer.Create(ownedToggle, Anchor.FullStretch, Offset.zero)
                            .WithText("•")
                            .WithStyle(m_ToggleLabelStyle);
                    }

                    ButtonContainer.Create(ownedToggle, Anchor.FullStretch, Offset.zero)
                        .WithColor(Color.Clear)
                        .WithCallback(m_CallbackHandler, arg =>
                        {
                            uiUser.ShowOwned = !uiUser.ShowOwned;
                            OpenSkinShop(uiUser.Player);
                        }, $"{uiUser.Player.UserIDString}.toggleowner");

                    TextContainer.Create(ownedToggle, Anchor.CenterRight, new Offset(5f, -10f, 55f, 10f))
                        .WithText(GetString("UI.Popup.Owned", uiUser.Player))
                        .WithAlignment(TextAnchor.MiddleLeft);

                });
        }
       
        private void CreateItemView(UIUser uiUser, UserData userData, KeyValuePair<string, ulong> skin, int permissionPage = 0)
        {
            SkinData skinData = m_SkinData.Data[skin.Key][skin.Value];
            
            BaseContainer root = ImageContainer.Create(PS_UI, Layer.Overall, Anchor.FullStretch, Offset.zero)
                .WithStyle(m_BackgroundStyle)
                .WithChildren(parent =>
                {
                    ButtonContainer.Create(parent, Anchor.FullStretch, Offset.zero)
                        .WithColor(Color.Clear)
                        .WithCallback(m_CallbackHandler, arg => OpenSkinShop(uiUser.Player), $"{uiUser.Player.UserIDString}.itemview.exit");
                    
                    ImageContainer.Create(parent, Anchor.Center, new Offset(-100f, 72.5f, 100f, 102.5f))
                        .WithStyle(m_PanelStyle)
                        .WithChildren(titleBar =>
                        {
                            TextContainer.Create(titleBar, Anchor.FullStretch, Offset.zero)
                                .WithText(skinData.Title)
                                .WithAlignment(TextAnchor.MiddleCenter);
                        });

                    bool isOwned = userData.IsOwned(skin.Key, skin.Value);
                    bool isDefaultSkin = userData.IsDefaultSkin(skin.Key, skin.Value);
                    
                    ImageContainer.Create(parent, Anchor.Center, new Offset(-100f, -97.5f, 100f, 67.5f))
                        .WithStyle(m_PanelStyle)
                        .WithChildren(icon =>
                        {
                            ImageContainer.Create(icon, Anchor.TopCenter, new Offset(-64f, -133f, 64f, -5f))
                                .WithIcon(m_ShortnameToItemId[skin.Key], skin.Value);
                           
                            if (!string.IsNullOrEmpty(skinData.permission))
                            {
                                ImageContainer.Create(parent, Anchor.Center, new Offset(-100f, 107.5f, 100f, 137.5f))
                                    .WithStyle(m_OwnedPanelStyle)
                                    .WithChildren(vipskin =>
                                    {
                                        TextContainer.Create(vipskin, Anchor.FullStretch, Offset.zero)
                                            .WithText(GetString($"UI.VIP.{skinData.permission}", uiUser.Player))
                                            .WithAlignment(TextAnchor.MiddleCenter);
                                    });
                            }

                            // Purchase
                            ImageContainer.Create(icon, Anchor.BottomStretch, new Offset(5f, 5f, -5f, 25f))
                                .WithStyle(m_ButtonStyle)
                                .WithChildren(button =>
                                {
                                    bool noPermission = !string.IsNullOrEmpty(skinData.permission) && !uiUser.Player.HasPermission(skinData.permission);
                                    bool isFree = !Configuration.Purchase.Enabled || uiUser.Player.HasPermission(NOCHARGE_PERMISSION);
                                    bool canAfford = isFree || GetUserBalance(uiUser.Player) >= skinData.cost;
                                    
                                    string buttonStr = 
                                        isOwned ? GetString("UI.Popup.Owned", uiUser.Player) :
                                        noPermission ? GetString("UI.Popup.NoPermission", uiUser.Player) :
                                        Configuration.Purchase.Enabled ? FormatString(canAfford ? "UI.Popup.PurchasePrice" : "UI.Popup.InsufficientFunds", uiUser.Player, skinData.cost, GetString(m_CurrencyType.ToString(), uiUser.Player)) :
                                        GetString("UI.Popup.Claim", uiUser.Player);
                                    
                                    TextContainer.Create(button, Anchor.FullStretch, Offset.zero)
                                        .WithText(buttonStr)
                                        .WithAlignment(TextAnchor.MiddleCenter);

                                    if (isOwned || noPermission || !canAfford)
                                        return;
                                    
                                    ButtonContainer.Create(button, Anchor.FullStretch, Offset.zero)
                                        .WithColor(Color.Clear)
                                        .WithCallback(m_CallbackHandler, arg =>
                                        {
                                            int cost = isFree ? 0 : skinData.cost;
                                            
                                            if (Configuration.Purchase.Enabled && cost > GetUserBalance(uiUser.Player))
                                                return;

                                            if (Configuration.Shop.GiveItemOnPurchase)
                                            {
                                                if (!Configuration.Purchase.Enabled || ChargeForPurchase(uiUser.Player, cost))
                                                {
                                                    Item item = ItemManager.CreateByName(skin.Key, 1, skin.Value);
                                                    uiUser.Player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);

                                                    m_UIUsers.Remove(uiUser.Player.userID);
                                                    ChaosUI.Destroy(uiUser.Player, PS_UI);
                                                    ChaosUI.Destroy(uiUser.Player, PS_UI_POPUP);
                                                }
                                            }
                                            else
                                            {
                                                if (!Configuration.Purchase.Enabled || ChargeForPurchase(uiUser.Player, cost))
                                                {
                                                    if (!userData.purchasedSkins.ContainsKey(skin.Key))
                                                        userData.purchasedSkins.Add(skin.Key, new List<ulong>());

                                                    if (!userData.purchasedSkins[skin.Key].Contains(skin.Value))
                                                        userData.purchasedSkins[skin.Key].Add(skin.Value);
                                                }
                                            }
                                            
                                            OpenSkinShop(uiUser.Player);
                                        }, $"{uiUser.Player.UserIDString}.purchase");
                                });


                        });

                    if (isOwned)
                    {
                        ImageContainer.Create(parent, Anchor.Center, new Offset(-100f, -132.5f, 100f, -102.5f))
                            .WithStyle(m_PanelStyle)
                            .WithChildren(setDefault =>
                            {
                                ImageContainer.Create(setDefault, Anchor.FullStretch, new Offset(5f, 5f, -5f, -5f))
                                    .WithStyle(m_ButtonStyle)
                                    .WithChildren(button =>
                                    {
                                        TextContainer.Create(button, Anchor.FullStretch, Offset.zero)
                                            .WithText(GetString(isDefaultSkin ? "UI.Popup.RemoveDefault" : "UI.Popup.SetDefault", uiUser.Player))
                                            .WithAlignment(TextAnchor.MiddleCenter);

                                        ButtonContainer.Create(button, Anchor.FullStretch, Offset.zero)
                                            .WithColor(Color.Clear)
                                            .WithCallback(m_CallbackHandler, arg =>
                                            {
                                                if (isDefaultSkin)
                                                    userData.defaultSkins.Remove(skin.Key);
                                                else userData.defaultSkins[skin.Key] = skin.Value;

                                                CreateItemView(uiUser, userData, skin, permissionPage);
                                            }, $"{uiUser.Player.UserIDString}.setdefault");

                                    });
                            });

                        if (Configuration.Shop.SellSkins && Configuration.Purchase.Enabled)
                        {
                            ImageContainer.Create(parent, Anchor.Center, new Offset(-100f, -167.5f, 100f, -137.5f))
                                .WithStyle(m_PanelStyle)
                                .WithChildren(sellSkin =>
                                {
                                    ImageContainer.Create(sellSkin, Anchor.FullStretch, new Offset(5f, 5f, -5f, -5f))
                                        .WithStyle(m_ButtonStyle)
                                        .WithOutline(m_OutlineRed)
                                        .WithChildren(button =>
                                        {
                                            TextContainer.Create(button, Anchor.FullStretch, Offset.zero)
                                                .WithText(FormatString("UI.Popup.Sell", uiUser.Player, skinData.cost, GetString(m_CurrencyType.ToString(), uiUser.Player)))
                                                .WithAlignment(TextAnchor.MiddleCenter);

                                            ButtonContainer.Create(button, Anchor.FullStretch, Offset.zero)
                                                .WithColor(Color.Clear)
                                                .WithCallback(m_CallbackHandler, arg =>
                                                {
                                                    userData.purchasedSkins[skin.Key].Remove(skin.Value);

                                                    if (isDefaultSkin)
                                                        userData.defaultSkins.Remove(skin.Key);

                                                    if (!uiUser.Player.HasPermission(NOCHARGE_PERMISSION))
                                                        RefundPurchase(uiUser.Player, skinData.cost);

                                                    CreateItemView(uiUser, userData, skin, permissionPage);
                                                }, $"{uiUser.Player.UserIDString}.sell");
                                        });
                                });
                        }
                    }

                    if (uiUser.Player.HasPermission(ADMIN_PERMISSION))
                        ShowAdminMode(uiUser, parent, userData, skin, skinData, permissionPage);
                });
            
            ChaosUI.Destroy(uiUser.Player, PS_UI);
            ChaosUI.Show(uiUser.Player, root);
        }

        private void ShowAdminMode(UIUser uiUser, BaseContainer parent, UserData userData, KeyValuePair<string, ulong> skin, SkinData skinData, int permissionPage = 0)
        {
            ImageContainer.Create(parent, Anchor.TopRight, new Offset(-255f, -35f, -5f, -5f))
                .WithStyle(m_PanelStyle)
                .WithChildren(adminBar =>
                {
                    TextContainer.Create(adminBar, Anchor.FullStretch, Offset.zero)
                        .WithText(GetString("UI.Admin.Options", uiUser.Player))
                        .WithAlignment(TextAnchor.MiddleCenter);
                    
                    ImageContainer.Create(adminBar, Anchor.CenterLeft, new Offset(5f, -10f, 25f, 10f))
                        .WithStyle(m_ButtonStyle)
                        .WithChildren(toggle =>
                        {
                            if (uiUser.AdminMode)
                            {
                                TextContainer.Create(toggle, Anchor.FullStretch, Offset.zero)
                                    .WithText("•")
                                    .WithStyle(m_ToggleLabelStyle);
                            }

                            ButtonContainer.Create(toggle, Anchor.FullStretch, Offset.zero)
                                .WithColor(Color.Clear)
                                .WithCallback(m_CallbackHandler, arg =>
                                {
                                    uiUser.AdminMode = !uiUser.AdminMode;
                                    CreateItemView(uiUser, userData, skin, permissionPage);

                                }, $"{uiUser.Player.UserIDString}.toggleadmin");
                        });
                });

            if (!uiUser.AdminMode)
                return;
            
            BaseContainer.Create(parent, Anchor.RightStretch, new Offset(-255f, 5f, -5f, -40f))
                .WithChildren(adminOptions =>
                {
                    ImageContainer.Create(adminOptions, Anchor.TopStretch, new Offset(0f, -30f, 0f, 0f))
                        .WithStyle(m_PanelStyle)
                        .WithChildren(price =>
                        {
                            TextContainer.Create(price, Anchor.TopLeft, new Offset(5f, -25f, 105f, -5f))
                                .WithText(GetString("UI.Admin.Price", uiUser.Player))
                                .WithAlignment(TextAnchor.MiddleLeft);

                            ImageContainer.Create(price, Anchor.TopStretch, new Offset(105f, -25f, -5f, -5f))
                                .WithStyle(m_ButtonStyle)
                                .WithChildren(priceInput =>
                                {
                                    InputFieldContainer.Create(priceInput, Anchor.FullStretch, new Offset(5f, 0f, -5f, 0f))
                                        .WithText(skinData.cost.ToString())
                                        .WithAlignment(TextAnchor.MiddleLeft)
                                        .WithCallback(m_CallbackHandler, arg =>
                                        {
                                            skinData.cost = arg.GetInt(1);
                                            m_SkinData.Save();

                                            CreateItemView(uiUser, userData, skin, permissionPage);
                                        }, $"{uiUser.Player.UserIDString}.setprice");
                                });

                        });

                    ImageContainer.Create(adminOptions, Anchor.FullStretch, new Offset(0f, 35f, 0f, -35f))
                        .WithStyle(m_PanelStyle)
                        .WithChildren(permissions =>
                        {
                            TextContainer.Create(permissions, Anchor.TopStretch, new Offset(5f, -25f, 0f, -5f))
                                .WithText(GetString("UI.Admin.Permissions", uiUser.Player))
                                .WithAlignment(TextAnchor.MiddleLeft);

                            BaseContainer.Create(permissions, Anchor.FullStretch, new Offset(5f, 30f, -5f, -30f))
                                .WithLayoutGroup(m_PermissionLayout, Configuration.Shop.Permissions, permissionPage, (int i, string t, BaseContainer layout, Anchor anchor, Offset offset) =>
                                {
                                    BaseContainer button = ImageContainer.Create(layout, anchor, offset)
                                        .WithStyle(m_ButtonStyle)
                                        .WithChildren(permissionButton =>
                                        {
                                            TextContainer.Create(permissionButton, Anchor.FullStretch, Offset.zero)
                                                .WithText(t)
                                                .WithAlignment(TextAnchor.MiddleCenter);

                                            ButtonContainer.Create(permissionButton, Anchor.FullStretch, Offset.zero)
                                                .WithColor(Color.Clear)
                                                .WithCallback(m_CallbackHandler, arg =>
                                                {
                                                    if (skinData.permission == t)
                                                        skinData.permission = string.Empty;
                                                    else skinData.permission = t;
                                                    
                                                    m_SkinData.Save();

                                                    CreateItemView(uiUser, userData, skin, permissionPage);
                                                }, $"{uiUser.Player.UserIDString}.permission.{i}");
                                        });

                                    if (skinData.permission == t)
                                        button.WithOutline(m_OutlineGreen);
                                });
                            
                            ImageContainer.Create(permissions, Anchor.BottomCenter, new Offset(-121.25f, 5f, -3.75f, 25f))
                                .WithStyle(permissionPage > 0 ? m_ButtonStyle : m_ButtonDisabledStyle)
                                .WithChildren(back =>
                                {
                                    TextContainer.Create(back, Anchor.FullStretch, Offset.zero)
                                        .WithText("<<<")
                                        .WithStyle(permissionPage > 0 ? m_ButtonStyle : m_ButtonDisabledStyle);

                                    if (permissionPage > 0)
                                    {
                                        ButtonContainer.Create(back, Anchor.FullStretch, Offset.zero)
                                            .WithColor(Color.Clear)
                                            .WithCallback(m_CallbackHandler, arg =>
                                            {
                                                CreateItemView(uiUser, userData, skin, permissionPage - 1);
                                            }, $"{uiUser.Player.UserIDString}.back");
                                    }
                                });

                            bool hasNextPage = m_PermissionLayout.HasNextPage(permissionPage, Configuration.Shop.Permissions.Count);
                            
                            ImageContainer.Create(permissions, Anchor.BottomCenter, new Offset(2.5f, 5f, 120f, 25f))
                                .WithStyle(hasNextPage ? m_ButtonStyle : m_ButtonDisabledStyle)
                                .WithChildren(next =>
                                {
                                    TextContainer.Create(next, Anchor.FullStretch, Offset.zero)
                                        .WithText(">>>")
                                        .WithStyle(hasNextPage ? m_ButtonStyle : m_ButtonDisabledStyle);

                                    if (hasNextPage)
                                    {
                                        ButtonContainer.Create(next, Anchor.FullStretch, Offset.zero)
                                        .WithColor(Color.Clear)
                                        .WithCallback(m_CallbackHandler, arg =>
                                        {
                                            CreateItemView(uiUser, userData, skin, permissionPage + 1);
                                        }, $"{uiUser.Player.UserIDString}.next");
                                    }
                                });
                        });

                    ImageContainer.Create(adminOptions, Anchor.BottomStretch, new Offset(0f, 0f, 0f, 30f))
                        .WithStyle(m_PanelStyle)
                        .WithChildren(remove =>
                        {
                            ImageContainer.Create(remove, Anchor.BottomStretch, new Offset(5f, 5f, -5f, 25f))
                                .WithStyle(m_ButtonStyle)
                                .WithOutline(m_OutlineRed)
                                .WithChildren(removeButton =>
                                {
                                    TextContainer.Create(removeButton, Anchor.FullStretch, Offset.zero)
                                        .WithText(GetString(skinData.isDisabled ? "UI.Admin.Enable" : "UI.Admin.Disable", uiUser.Player))
                                        .WithAlignment(TextAnchor.MiddleCenter);

                                    if (!skinData.isDisabled)
                                    {
                                        ButtonContainer.Create(removeButton, Anchor.FullStretch, Offset.zero)
                                            .WithColor(Color.Clear)
                                            .WithCallback(m_CallbackHandler, arg =>
                                            {
                                                skinData.isDisabled = !skinData.isDisabled;
                                                m_SkinData.Save();
                                                
                                                CreateItemView(uiUser, userData, skin, permissionPage);
                                            }, $"{uiUser.Player.UserIDString}.disableditem");
                                    }
                                });
                        });
                });
        }

        #endregion
        
        #region Reskin Menu
        
        private void OpenReskinMenu(BasePlayer player)
        {
            Item item = player.GetActiveItem();
            if (item == null)
            {
                item = player.inventory.containerBelt.GetSlot(0);
                if (item == null)
                {
                    player.LocalizedMessage(this, "Chat.Reskin.NoItem2");
                    return;
                }
            }

            Hash<ulong, SkinData> skinData;
            if (!m_SkinData.Data.TryGetValue(item.info.shortname, out skinData) || skinData.Count == 0)
            {
                player.LocalizedMessage(this, "Chat.Reskin.NoSkins");
                return;
            }

            UserData userData;
            if (!m_UserData.Data.TryGetValue(player.userID, out userData))
            {
                player.LocalizedMessage(this, "Chat.Reskin.NoPurchases");
                return;
            }
            
            OpenReskinMenuUI(player, userData, item);
        }

        private void OpenReskinMenuUI(BasePlayer player, UserData userData, Item item)
        {
            UIUser uiUser;
            if (!m_UIUsers.TryGetValue(player.userID, out uiUser))
                uiUser = m_UIUsers[player.userID] = new UIUser(player, DisplayMode.None);

            BaseContainer root = BaseContainer.Create(PS_UI, Layer.Overall, Anchor.FullStretch, new Offset(16f, 16f, -16f, -16f))
                .WithChildren(inset =>
                    {
                        BaseContainer.Create(inset, Anchor.BottomStretch, new Offset(0f, 0f, 0f, 64f))
                            .WithChildren(bottom =>
                            {
                                ImageContainer.Create(bottom, Anchor.BottomCenter, new Offset(-198.5f, 69f, 182.5f, 304f))
                                    .WithStyle(m_BackgroundStyle)
                                    .WithChildren(parent =>
                                    {
                                        CreateReskinTitleBar(uiUser, parent, item);

                                        CreateReskinItemGrid(uiUser, parent, userData, item);
                                    });
                            });
                    })
                .NeedsCursor()
                .NeedsKeyboard();

            ChaosUI.Destroy(player, PS_UI);
            ChaosUI.Show(player, root);
        }

        private void CreateReskinTitleBar(UIUser uiUser, BaseContainer parent, Item item)
        {
            ImageContainer.Create(parent, Anchor.TopStretch, new Offset(5f, -35f, -5f, -5f))
                .WithStyle(m_PanelStyle)
                .WithChildren(titleBar =>
                {
                    TextContainer.Create(titleBar, Anchor.CenterLeft, new Offset(4.999992f, -15f, 243.8218f, 15f))
                        .WithText(FormatString("UI.Reskin.SkinList", uiUser.Player, GetString(item.info.shortname, uiUser.Player)))
                        .WithAlignment(TextAnchor.MiddleLeft);

                    ImageContainer.Create(titleBar, Anchor.CenterRight, new Offset(-55f, -10f, -5f, 10f))
                        .WithStyle(m_ButtonStyle)
                        .WithOutline(m_OutlineRed)
                        .WithChildren(exit =>
                        {
                            TextContainer.Create(exit, Anchor.FullStretch, Offset.zero)
                                .WithText(GetString("UI.Exit", uiUser.Player))
                                .WithAlignment(TextAnchor.MiddleCenter);

                            ButtonContainer.Create(exit, Anchor.FullStretch, Offset.zero)
                                .WithColor(Color.Clear)
                                .WithCallback(m_CallbackHandler, arg =>
                                {
                                    m_UIUsers.Remove(uiUser.Player.userID);
                                    ChaosUI.Destroy(uiUser.Player, PS_UI);
                                }, $"{uiUser.Player.UserIDString}.exit");
                        });
                });
        }
        
        private void CreateReskinItemGrid(UIUser uiUser, BaseContainer parent, UserData userData, Item item)
        {
            List<ulong> skinList = Facepunch.Pool.GetList<ulong>();

            Hash<ulong, SkinData> skinLookup;
            if (m_SkinData.Data.TryGetValue(item.info.shortname, out skinLookup))
            {
                List<ulong> purchasedSkins;
                userData.purchasedSkins.TryGetValue(item.info.shortname, out purchasedSkins);

                if (purchasedSkins?.Count > 0)
                {
                    foreach (ulong skin in purchasedSkins)
                    {
                        SkinData skinData;
                        if (skinLookup.TryGetValue(skin, out skinData) && !skinData.isDisabled)
                            skinList.Add(skin);
                    }
                }
            }
            if (skinList.Count == 0)
            {
                TextContainer.Create(parent, Anchor.FullStretch, new Offset(5f, 40f, -5f, -40f))
                    .WithText(GetString("Chat.Reskin.NoPurchases", uiUser.Player))
                    .WithAlignment(TextAnchor.MiddleCenter);
            }
            else
            {
                ImageContainer.Create(parent, Anchor.FullStretch, new Offset(5f, 40f, -5f, -40f))
                    .WithStyle(m_PanelStyle)
                    .WithLayoutGroup(m_ReskinItemGrid, skinList, uiUser.GridPage, (int i, ulong t, BaseContainer itemGrid, Anchor anchor, Offset offset) =>
                    {
                        ImageContainer.Create(itemGrid, anchor, offset)
                            .WithStyle(m_PanelStyle)
                            .WithChildren(template =>
                            {
                                ImageContainer.Create(template, Anchor.Center, new Offset(-32f, -32f, 32f, 32f))
                                    .WithIcon(item.info.itemid, t);

                                ButtonContainer.Create(template, Anchor.FullStretch, Offset.zero)
                                    .WithColor(Color.Clear)
                                    .WithCallback(m_CallbackHandler, arg =>
                                    {
                                        ChangeItemSkin(uiUser.Player, t);
                                        m_UIUsers.Remove(uiUser.Player.userID);
                                        ChaosUI.Destroy(uiUser.Player, PS_UI);
                                    }, $"{uiUser.Player.UserIDString}.reskin.{i}");
                            });
                    });
            }

            CreateReskinFooter(uiUser, parent, skinList.Count, userData, item);
            
            Facepunch.Pool.FreeList(ref skinList);
        }

        private void CreateReskinFooter(UIUser uiUser, BaseContainer parent, int listCount, UserData userData, Item item)
        {
            ImageContainer.Create(parent, Anchor.BottomStretch, new Offset(5f, 5f, -5f, 35f))
                .WithStyle(m_PanelStyle)
                .WithChildren(footer =>
                {
                    ImageContainer.Create(footer, Anchor.CenterLeft, new Offset(5f, -10f, 35f, 10f))
                        .WithStyle(uiUser.GridPage > 0 ? m_ButtonStyle : m_ButtonDisabledStyle)
                        .WithChildren(backButton =>
                        {
                            TextContainer.Create(backButton, Anchor.FullStretch, Offset.zero)
                                .WithText("<<<")
                                .WithStyle(uiUser.GridPage > 0 ? m_ButtonStyle : m_ButtonDisabledStyle);

                            if (uiUser.GridPage > 0)
                            {
                                ButtonContainer.Create(backButton, Anchor.FullStretch, Offset.zero)
                                    .WithColor(Color.Clear)
                                    .WithCallback(m_CallbackHandler, arg =>
                                    {
                                        uiUser.GridPage--;
                                        OpenReskinMenuUI(uiUser.Player, userData, item);
                                    }, $"{uiUser.Player.UserIDString}.grid.back");
                            }

                        });

                    bool hasNextPage = m_ReskinItemGrid.HasNextPage(uiUser.GridPage, listCount);
                    
                    ImageContainer.Create(footer, Anchor.CenterRight, new Offset(-35f, -10f, -5f, 10f))
                        .WithStyle(hasNextPage ? m_ButtonStyle : m_ButtonDisabledStyle)
                        .WithChildren(nextButton =>
                        {
                            TextContainer.Create(nextButton, Anchor.FullStretch, Offset.zero)
                                .WithText(">>>")
                                .WithStyle(hasNextPage ? m_ButtonStyle : m_ButtonDisabledStyle);

                            if (hasNextPage)
                            {
                                ButtonContainer.Create(nextButton, Anchor.FullStretch, Offset.zero)
                                    .WithColor(Color.Clear)
                                    .WithCallback(m_CallbackHandler, arg =>
                                    {
                                        uiUser.GridPage++;
                                        OpenReskinMenuUI(uiUser.Player, userData, item);
                                    }, $"{uiUser.Player.UserIDString}.grid.next");
                            }
                        });
                });
        }
        #endregion
        
        #region Popup Message

        private Hash<ulong, Timer> m_PopupTimers = new Hash<ulong, Timer>();

        private void CreatePopupMessage(UIUser uiUser, string message)
        {
            BaseContainer baseContainer = ImageContainer.Create(PS_UI_POPUP, Layer.Overall, Anchor.Center, new Offset(-540f, -345f, 540f, -315f))
                .WithStyle(m_BackgroundStyle)
                .WithChildren(popup =>
                {
                    ImageContainer.Create(popup, Anchor.FullStretch, new Offset(5f, 5f, -5f, -5f))
                        .WithStyle(m_PanelStyle)
                        .WithChildren(titleBar =>
                        {
                            TextContainer.Create(titleBar, Anchor.FullStretch, Offset.zero)
                                .WithText(message)
                                .WithAlignment(TextAnchor.MiddleCenter);

                        });
                });
			
            ChaosUI.Destroy(uiUser.Player, PS_UI_POPUP);
            ChaosUI.Show(uiUser.Player, baseContainer);

            Timer t;
            if (m_PopupTimers.TryGetValue(uiUser.Player.userID, out t))
                t?.Destroy();

            m_PopupTimers[uiUser.Player.userID] = timer.Once(5f, () => ChaosUI.Destroy(uiUser.Player, PS_UI_POPUP));
        }
        #endregion
        #endregion
        #endregion
        
        #region Configuration
        private ConfigData Configuration => ConfigurationData as ConfigData;
        
        protected override void OnConfigurationUpdated(VersionNumber oldVersion)
        {
            ConfigData baseConfigData = GenerateDefaultConfiguration<ConfigData>();

            if (oldVersion < new VersionNumber(3, 0, 0))
            {
                (ConfigurationData as ConfigData).Colors = baseConfigData.Colors;
                (ConfigurationData as ConfigData).Purchase.Type = "Scrap";
            }
        }

        protected override ConfigurationFile OnLoadConfig(ref ConfigurationFile configurationFile) => configurationFile = new ConfigurationFile<ConfigData>(Config);

        protected override T GenerateDefaultConfiguration<T>()
        {
            return new ConfigData
            {
                Announcements = new ConfigData.AnnouncementOptions
                {
                    Enabled = true,
                    Interval = 10
                },
                Commands = new ConfigData.CommandOptions
                {
                    DefaultCommand = "skin",
                    ReskinCommand = "reskin",
                    ShopCommand = "skinshop"
                },
                Reskin = new ConfigData.ReskinOptions
                {
                    DisableCommand = false,
                    NPCs = new string[0]
                },
                Shop = new ConfigData.ShopOptions
                {
                    BlockedItems = new string[0],
                    DisableCommand = false,
                    ForcedMode = "None",
                    GiveItemOnPurchase = false,
                    NPCs = new string[0],
                    Permissions = new List<string> { "playerskins.vip1", "playerskins.vip2", "playerskins.vip3" },
                    SellSkins = true,
                    HelpOnExit = true
                },
                Purchase = new ConfigData.PurchaseOptions
                {
                    Type = "Scrap",
                    Enabled = true,
                    DefaultCosts = new Hash<string, int>(),
                },
                Workshop = new ConfigData.WorkshopOptions
                {
                    ApprovedDisabled = false,
                    Enabled = true,
                    Filter = new string[0],
                    SteamAPIKey = string.Empty
                },
                Colors = new ConfigData.UIColors
                {
                    Background = new ConfigData.UIColors.Color
                    {
                        Hex = "151515",
                        Alpha = 0.94f
                    },
                    Panel = new ConfigData.UIColors.Color
                    {
                        Hex = "FFFFFF",
                        Alpha = 0.165f
                    },
                    Button = new ConfigData.UIColors.Color
                    {
                        Hex = "2A2E32",
                        Alpha = 1f
                    },
                    Highlight = new ConfigData.UIColors.Color
                    {
                        Hex = "C4FF00",
                        Alpha = 1f
                    },
                    Close = new ConfigData.UIColors.Color
                    {
                        Hex = "CE422B",
                        Alpha = 1f
                    }
                },
            } as T;
        }
        
        private class ConfigData : BaseConfigData
        {
            [JsonProperty(PropertyName = "Announcement Options")]
            public AnnouncementOptions Announcements { get; set; }

            [JsonProperty(PropertyName = "Command Options")]
            public CommandOptions Commands { get; set; }

            [JsonProperty(PropertyName = "Purchase Options")]
            public PurchaseOptions Purchase { get; set; }

            [JsonProperty(PropertyName = "Skin Shop Options")]
            public ShopOptions Shop { get; set; }

            [JsonProperty(PropertyName = "Re-skin Options")]
            public ReskinOptions Reskin { get; set; }

            [JsonProperty(PropertyName = "Workshop Options")]
            public WorkshopOptions Workshop { get; set; }

            [JsonProperty(PropertyName = "UI Colors")]
            public UIColors Colors { get; set; }

            public class AnnouncementOptions
            {
                [JsonProperty(PropertyName = "Display help information to players")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Information display interval (minutes)")]
                public int Interval { get; set; }
            }

            public class PurchaseOptions
            {
                [JsonProperty(PropertyName = "Enable purchase system")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Currency used to purchase skins (ServerRewards, Economics, Scrap)")]
                public string Type { get; set; }

                [JsonProperty(PropertyName = "Default Skin Costs")]
                public Hash<string, int> DefaultCosts { get; set; }
            }

            public class ShopOptions
            {
                [JsonProperty(PropertyName = "Custom permissions which can be assigned to skins")]
                public List<string> Permissions { get; set; }

                [JsonProperty(PropertyName = "NPC user IDs that players can interact with to open the skin shop")]
                public string[] NPCs { get; set; }

                [JsonProperty(PropertyName = "Disable the '/skin shop' command and force players to access it via a NPC")]
                public bool DisableCommand { get; set; }

                [JsonProperty(PropertyName = "Allow players to sell unwanted skins back to the skin store")]
                public bool SellSkins { get; set; }

                [JsonProperty(PropertyName = "Give player the item when they purchase a skin (this disables the reskin menu)")]
                public bool GiveItemOnPurchase { get; set; }

                [JsonProperty(PropertyName = "Forced display mode for skin shop (Full, Minimalist, None)")]
                public string ForcedMode { get; set; }

                [JsonProperty(PropertyName = "Send a help message to players when exiting the skin shop")]
                public bool HelpOnExit { get; set; }

                [JsonProperty(PropertyName = "List of shortnames for items to be blocked from appearing in the skin shop")]
                public string[] BlockedItems { get; set; }
            }

            public class ReskinOptions
            {
                [JsonProperty(PropertyName = "NPC user IDs that players can interact with to open the re-skin menu")]
                public string[] NPCs { get; set; }

                [JsonProperty(PropertyName = "Disable the '/skin' command and force players to access it via a NPC")]
                public bool DisableCommand { get; set; }
            }

            public class WorkshopOptions
            {
                [JsonProperty(PropertyName = "Disable approved skins from the skin shop")]
                public bool ApprovedDisabled { get; set; }

                [JsonProperty(PropertyName = "Enable workshop skins in the skin shop")]
                public bool Enabled { get; set; }

                [JsonProperty(PropertyName = "Word filter for workshop skins. If the skin title partially contains any of these words it will not be available as a potential skin")]
                public string[] Filter { get; set; }

                [JsonProperty(PropertyName = "Steam API key (get one here https://steamcommunity.com/dev/apikey)")]
                public string SteamAPIKey { get; set; }
            }

            public class CommandOptions
            {
                [JsonProperty(PropertyName = "Default chat command")]
                public string DefaultCommand { get; set; }

                [JsonProperty(PropertyName = "Re-skin direct command")]
                public string ReskinCommand { get; set; }

                [JsonProperty(PropertyName = "Skin shop direct command")]
                public string ShopCommand { get; set; }
            }

            public class UIColors
            {                
                public Color Background { get; set; }

                public Color Panel { get; set; }
                
                public Color Button { get; set; }

                public Color Highlight { get; set; }

                public Color Close { get; set; }
                
                public class Color
                {
                    public string Hex { get; set; }

                    public float Alpha { get; set; }
                }
            }
        }
        
        private void UpdateDefaultCosts()
        {
            bool hasChanged = false;

            foreach (ItemDefinition itemDefinition in m_SkinnableItems)
            {
                if (!Configuration.Purchase.DefaultCosts.ContainsKey(itemDefinition.shortname))
                {
                    Configuration.Purchase.DefaultCosts[itemDefinition.shortname] = Mathf.Max((int)itemDefinition.rarity, 1) * 10;
                    hasChanged = true;
                    continue;
                }
            }

            foreach(string shortname in Configuration.Purchase.DefaultCosts.Keys.ToList())
            {
                if (!m_SkinnableItems.Any((ItemDefinition itemDefintion) => itemDefintion.shortname.Equals(shortname)))
                {
                    Configuration.Purchase.DefaultCosts.Remove(shortname);
                    hasChanged = true;
                }
            }

            if (hasChanged)
                SaveConfiguration();
        }

        private void UpdateLocalization()
        {
            foreach (ItemDefinition itemDefinition in m_SkinnableItems)
            {
                m_Messages[itemDefinition.shortname] = itemDefinition.displayName.english;
            }

            foreach (string perm in Configuration.Shop.Permissions)
            {
                m_Messages[$"UI.VIP.{perm}"] = "VIP skin only";
            }
            lang.RegisterMessages(m_Messages, this);
        }
        #endregion

        #region Localization
        protected override void PopulatePhrases()
        {
            m_Messages = new Dictionary<string, string>
            {
                ["ServerRewards"] = "RP",
                ["Economics"] = "Coins",
                ["Scrap"] = "Scrap",
                ["UI.Balance"] = "Balance : {0} {1}",
                ["UI.Exit"] = "EXIT",
                ["UI.Popup.Owned"] = "Owned",
                ["UI.Popup.Sell"] = "Sell skin ({0} {1})",
                ["UI.Popup.RemoveDefault"] = "Remove as default",
                ["UI.Popup.SetDefault"] = "Set as default",
                ["UI.Popup.PurchasePrice"] = "Purchase ({0} {1})",
                ["UI.Popup.NoPermission"] = "You dont have permission",
                ["UI.Popup.InsufficientFunds"] = "Not Enough ({0} {1})",
                ["UI.Popup.Claim"] = "Claim",
                ["UI.Admin.Permissions"] = "Skin Permission",
                ["Chat.Reskin.NoItem2"] = "You need to hold a item in your hands, or have it equipped in the first slot of your hotbar to open the re-skin menu",
                ["Chat.Reskin.NoSkins"] = "There are no skins available for this item",
                ["Chat.Reskin.NoPurchases"] = "You have not purchased any skins from the skin shop",
                ["UI.Reskin.SkinList"] = "Skins purchased for {0}",
                ["UI.Button.Up"] = "▲ ▲ ▲",
                ["UI.Button.Down"] = "▼ ▼ ▼",
                ["UI.Admin.Enable"] = "Enable skin in store",
                ["UI.Admin.Disable"] = "Disable skin in store",
                ["UI.Admin.Options"] = "Admin Options",
                ["UI.Admin.Price"] = "Price",
                ["Help.Shop.NPC"] = "You can access the skin shop by visiting a skin shop NPC!",
                ["Help.Shop.Command"] = "You can access the skin shop by typing '/skin shop'",
                ["Help.Reskin.NPC"] = "You can apply purchased skins by visiting a reskin NPC!",
                ["Help.Reskin.Command"] = "You can apply purchased skins by typing '/skin' while holding the item in your hands!",
                ["UI.NoSkinsFound"] = "No skins found matching your criteria",
                ["UI.NoShortname"] = "Select a item on the left to continue"
            };
        }
        #endregion
        
        #region Data
        private class SkinData
        {
            public string permission = string.Empty;
            public int cost = 1;
            public bool isDisabled = false;

            [JsonIgnore]
            public string Title { get; set; } = string.Empty;

            [JsonIgnore]
            public string URL { get; set; } = string.Empty;

            [JsonIgnore]
            public bool IsValid { get; set; } = false;
        }

        private class UserData
        {
            public Dictionary<string, ulong> defaultSkins = new Dictionary<string, ulong>();
            public Dictionary<string, List<ulong>> purchasedSkins = new Dictionary<string, List<ulong>>();
            public DisplayMode displayMode = DisplayMode.Full;

            public UserData() { }

            public bool IsDefaultSkin(string shortname, ulong skinId)
            {
                if (!defaultSkins.ContainsKey(shortname))
                    return false;

                if (defaultSkins[shortname] == skinId)
                    return true;

                return false;
            }

            public bool IsOwned(string shortname, ulong skinId)
            {
                if (!purchasedSkins.ContainsKey(shortname))
                    return false;

                if (purchasedSkins[shortname].Contains(skinId))
                    return true;

                return false;
            }
        }
        
        private class WorkshopItem
        {
            public string title;
            public string description;
            public string imageUrl;

            public WorkshopItem() { }

            public WorkshopItem(PublishedFileDetails item)
            {
                if (item == null)
                    return;

                title = item.title;
                description = item.file_description;
                imageUrl = item.preview_url.Replace("https", "http");
            }
                     

            public WorkshopItem(InventoryDef item, string url)
            {
                if (item == null)
                    return;

                title = item.Name;
                description = item.Description;
                imageUrl = url == null ? string.Empty : url.Replace("https", "http");
            }
        }

        public class QueryResponse
        {
            public Response response;
        }

        public class Response
        {
            public int total;
            public PublishedFileDetails[] publishedfiledetails;
        }

        public class PublishedFileDetails
        {
            public int result;
            public string publishedfileid;
            public string creator;
            public int creator_appid;
            public int consumer_appid;
            public int consumer_shortcutid;
            public string filename;
            public string file_size;
            public string preview_file_size;
            public string file_url;
            public string preview_url;
            public string url;
            public string hcontent_file;
            public string hcontent_preview;
            public string title;
            public string file_description;
            public int time_created;
            public int time_updated;
            public int visibility;
            public int flags;
            public bool workshop_file;
            public bool workshop_accepted;
            public bool show_subscribe_all;
            public int num_comments_public;
            public bool banned;
            public string ban_reason;
            public string banner;
            public bool can_be_deleted;
            public string app_name;
            public int file_type;
            public bool can_subscribe;
            public int subscriptions;
            public int favorited;
            public int followers;
            public int lifetime_subscriptions;
            public int lifetime_favorited;
            public int lifetime_followers;
            public string lifetime_playtime;
            public string lifetime_playtime_sessions;
            public int views;
            public int num_children;
            public int num_reports;
            public Preview[] previews;
            public Tag[] tags;
            public int language;
            public bool maybe_inappropriate_sex;
            public bool maybe_inappropriate_violence;

            public class Tag
            {
                public string tag;
                public bool adminonly;
            }

        }

        public class Preview
        {
            public string previewid;
            public int sortorder;
            public string url;
            public int size;
            public string filename;
            public int preview_type;
            public string youtubevideoid;
            public string external_reference;
        }

        public class CollectionQueryResponse
        {
            public CollectionResponse response { get; set; }
        }

        public class CollectionResponse
        {
            public int result { get; set; }
            public int resultcount { get; set; }
            public CollectionDetails[] collectiondetails { get; set; }
        }

        public class CollectionDetails
        {
            public string publishedfileid { get; set; }
            public int result { get; set; }
            public CollectionChild[] children { get; set; }
        }

        public class CollectionChild
        {
            public string publishedfileid { get; set; }
            public int sortorder { get; set; }
            public int filetype { get; set; }
        }
        #endregion
    }
}
