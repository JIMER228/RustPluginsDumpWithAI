using Oxide.Game.Rust.Cui;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using System.IO;
using Newtonsoft.Json;
using Oxide.Core.Configuration;
using System.Collections;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System.Threading.Tasks;
using System.Timers;

namespace Oxide.Plugins
{
    [Info("SkinController", "Amino", "1.3.13")]
    [Description("An advanced skin system for Rust")]
    public class SkinController : RustPlugin
    {
        [PluginReference] private Plugin Backpacks, Kits, KitController;

        #region Config
        public class SkinnerConfig
        {
            [JsonProperty(PropertyName = "Steam API Key (https://steamcommunity.com/dev/apikey)")]
            public string SteamApiKey { get; set; } = "";
            public SkinCommands Commands { get; set; } = new SkinCommands();
            [JsonProperty(PropertyName = "Permission: Max outfits")]
            public Dictionary<string, int> OutfitPermissions { get; set; } = new Dictionary<string, int>();
            [JsonProperty(PropertyName = "Skin base cooldown (seconds)")]
            public int BaseSkinCooldownSeconds { get; set; } = 60;
            [JsonProperty(PropertyName = "Allow skinning items on craft")]
            public bool SkinItemsOnCraft { get; set; } = true;
            [JsonProperty(PropertyName = "Allow skinning items on pickup")]
            public bool SkinItemsOnPickup { get; set; } = true;
            [JsonProperty(PropertyName = "Allow skinning backpack items")]
            public bool SkinItemsInBackpack { get; set; } = true;
            [JsonProperty(PropertyName = "Allow skinning kits on redeemed")]
            public bool SkinKitsOnRedeemed { get; set; } = true;
            public string UIImage { get; set; } = "https://media.discordapp.net/attachments/670451699063980083/1142120651797319861/AdvSkinner.png";
            [JsonProperty(PropertyName = "Settings icon")]
            public string SettingsIcon = "https://media.discordapp.net/attachments/670451699063980083/1162986265826820106/SettingsCog.png";
            [JsonProperty(PropertyName = "Background UI color")]
            public string UIBackgroundColor { get; set; } = $"0 0 0 .6";
            [JsonProperty(PropertyName = "Main UI color")]
            public string UIButtonColor { get; set; } = $"1 .4 0";
            [JsonProperty(PropertyName = "Secondary UI color")]
            public string UISecondaryButtonColor { get; set; } = ".29 .29 .29";
            [JsonProperty(PropertyName = "Active UI color")]
            public string UIActiveButtonColor { get; set; } = ".29 .29 .29";
            [JsonProperty(PropertyName = "Safe UI color")]
            public string UISafeButtonColor { get; set; } = ".32 .89 .26";
            [JsonProperty(PropertyName = "Danger UI color")]
            public string UIDangerButtonColor { get; set; } = "1 .34 .34";
            [JsonProperty(PropertyName = "Blur UI background")]
            public bool BlurUIBackground { get; set; } = true;
            [JsonProperty(PropertyName = "Default Outfit")]
            public List<OutfitDetail> DefaultOutfit { get; set; } = new List<OutfitDetail>();
            public static SkinnerConfig DefaultConfig()
            {
                return new SkinnerConfig
                {
                    Commands = new SkinCommands
                    {
                        SkinMenuCommand = new List<string> { "skin", "sb" },
                        SkinItemCommand = new List<string> { "skinitem", "skini" },
                        SkinBaseCommand = new List<string> { "skinbase" },
                        SkinItemsInContainerCommand = new List<string> { "skincontainer", "skinc" }
                    },
                    DefaultOutfit = new List<OutfitDetail>()
                    {
                        new OutfitDetail { ItemId = -194953424, Shortname = "metal.facemask", SkinId = "0" },
                        new OutfitDetail { ItemId = 1751045826, Shortname = "hoodie", SkinId = "0" },
                        new OutfitDetail { ItemId = 1110385766, Shortname = "metal.plate.torso", SkinId = "0" },
                        new OutfitDetail { ItemId = 1850456855, Shortname = "roadsign.kilt", SkinId = "0" },
                        new OutfitDetail { ItemId = 237239288, Shortname = "pants", SkinId = "0" },
                        new OutfitDetail { ItemId = -1549739227, Shortname = "shoes.boots", SkinId = "0" },
                        new OutfitDetail { ItemId = 1545779598, Shortname = "rifle.ak", SkinId = "0" },
                        new OutfitDetail { ItemId = 442886268, Shortname = "rocket.launcher", SkinId = "0" }
                    },
                    OutfitPermissions = new Dictionary<string, int>()
                    {
                        { "skincontroller.default", 4 },
                        { "skincontroller.vip", 6 },
                        { "skincontroller.vip+", 10 },
                        { "skincontroller.admin", 100 }
                    }
                };
            }
        }

        public class SkinCommands
        {
            [JsonProperty(PropertyName = "Skin menu command")]
            public List<string> SkinMenuCommand { get; set; } = new List<string>();
            [JsonProperty(PropertyName = "Skin item command")]
            public List<string> SkinItemCommand { get; set; } = new List<string>();
            [JsonProperty(PropertyName = "Skin base command")]
            public List<string> SkinBaseCommand { get; set; } = new List<string>();
            [JsonProperty(PropertyName = "Skin container items command")]
            public List<string> SkinItemsInContainerCommand { get; set; } = new List<string>();
        }

        private static SkinnerConfig _config;

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<SkinnerConfig>();
                if (_config == null) LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                PrintWarning("Creating new config file.");
                LoadDefaultConfig();
            }
        }
        protected override void LoadDefaultConfig() => _config = SkinnerConfig.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(_config);
        #endregion

        #region Data
        private SkinManager _skinList = new SkinManager();
        private Dictionary<ulong, PlayerSettings> _playerSettings = new Dictionary<ulong, PlayerSettings>();

        public class SkinManager
        {
            public List<SkinData> WorkshopSkins = new List<SkinData>();
            public List<SkinData> AddedSkins = new List<SkinData>();
            public List<string> BlacklistedSkins = new List<string>();
            public Dictionary<string, List<int>> CategoryList = new Dictionary<string, List<int>>();
        }

        public class SkinData
        {
            public string SkinName { get; set; }
            public string SkinID { get; set; }
            public string ItemShortname { get; set; }
            public int ItemID { get; set; }
        }

        public class PlayerSettings
        {
            public bool SkinOnPickUp { get; set; } = false;
            public bool SkinOnCraft { get; set; } = false;
            public bool SkinOnLoot { get; set; } = false;
            public bool SkinOnKitRedeem { get; set; } = false;
            public bool FavoriteFallback { get; set; } = true;
            public int CategoryItem { get; set; } = -194953424;
            public string SelectedShortName { get; set; } = "metal.facemask";
            public ulong SelectedSkinId { get; set; } = 0;
            public string Category { get; set; } = "Attire";
            public int ItemPage { get; set; } = 0;
            public string SkinsFilter { get; set; } = null;
            public int SkinPage { get; set; } = 0;
            public int AllSkinPage { get; set; } = 0;
            public int OutfitPage { get; set; } = 0;
            public string LoadedItems { get; set; } = null;
            public string ActiveSaveName { get; set; }
            public long BaseSkinCooldown { get; set; } = 0;
            public List<OutfitDetail> SavedItems { get; set; } = new List<OutfitDetail>();
            public List<Outfits> Outfits { get; set; } = new List<Outfits>();
        }

        public class Outfits
        {
            public string OutfitName { get; set; }
            public bool Favorite { get; set; } = false;
            public List<OutfitDetail> OutfitDetails { get; set; }
        }

        public class OutfitDetail
        {
            public int ItemId { get; set; }
            public string SkinId { get; set; }
            public string Shortname { get; set; }
        }

        private List<string> Permissions = new List<string>
        {
            "skincontroller.addskins",
            "skincontroller.use",
            "skincontroller.skinoncraft",
            "skincontroller.skinonpickup",
            "skincontroller.skinbase",
            "skincontroller.skinitem",
            "skincontroller.skincontainer",
            "skincontroller.skinonkitredeemed"
        };
        #endregion

        #region Constructors
        private List<string> _hazmatList = new List<string> { "1266491000", "-470439097", "-797592358", "861513346", "491263800", "-560304835" };

        private readonly Dictionary<string, string> _namesToConvert = new Dictionary<string, string> {
            { "woodbox_deployed", "box.wooden" },
            { "waterpurifier.deployed", "water.purifier" },
            { "vendingmachine.deployed", "vending.machine" },
            { "box.wooden.large", "box.wooden.large" },
            { "rug.bear.deployed", "rug.bear" },
            { "door.hinged.toptier", "door.hinged.toptier" },
            { "door.hinged.metal", "door.hinged.metal" },
            { "wall.frame.garagedoor", "wall.frame.garagedoor" },
            { "rug.deployed", "rug" },
            { "locker.deployed", "locker" },
            { "door.double.hinged.metal", "door.double.hinged.metal" },
            { "door.double.hinged.wood", "door.double.hinged.wood" },
            { "barricade.concrete", "barricade.concrete" },
            { "sleepingbag_leather_deployed", "sleepingbag" },
            { "table.deployed", "table" },
            { "door.double.hinged.toptier", "door.double.hinged.toptier" },
            { "fridge.deployed", "fridge" },
            { "furnace", "furnace" },
            { "barricade.sandbags", "barricade.sandbags" },
            { "chair.deployed", "chair" },
            { "door.hinged.wood", "door.hinged.wood" }
        };

        public class AddSkinRoot
        {
            public Response response;
        }

        public class Tag
        {
            public string tag { get; set; }
        }

        public class Response
        {
            public int result { get; set; }
            public int resultcount { get; set; }
            public List<PublishedFileDetail> publishedfiledetails { get; set; }
        }

        public class PublishedFileDetail
        {
            public string publishedfileid { get; set; }
            public int result { get; set; }
            public string creator { get; set; }
            public int creator_app_id { get; set; }
            public int consumer_app_id { get; set; }
            public string filename { get; set; }
            public int file_size { get; set; }
            public string preview_url { get; set; }
            public string hcontent_preview { get; set; }
            public string title { get; set; }
            public string description { get; set; }
            public int time_created { get; set; }
            public int time_updated { get; set; }
            public int visibility { get; set; }
            public int banned { get; set; }
            public string ban_reason { get; set; }
            public int subscriptions { get; set; }
            public int favorited { get; set; }
            public int lifetime_subscriptions { get; set; }
            public int lifetime_favorited { get; set; }
            public int views { get; set; }
            public List<Tag> tags { get; set; }
        }

        public class CollectionRoot
        {
            public CollectionResponse response { get; set; }
        }

        public class CollectionResponse
        {
            public int result { get; set; }
            public int resultcount { get; set; }
            public List<Collectiondetail> collectiondetails { get; set; }
        }

        public class Collectiondetail
        {
            public string publishedfileid { get; set; }
            public int result { get; set; }
            public List<Child> children { get; set; }
        }

        public class Child
        {
            public string publishedfileid { get; set; }
            public int sortorder { get; set; }
            public int filetype { get; set; }
        }

        public Queue<ItemTask> itemTasks = new Queue<ItemTask>();

        private void ProcessItemQueue()
        {
            while (itemTasks.Count > 0)
            {
                var task = itemTasks.Dequeue();
                ItemContainer tempContainer = new ItemContainer();
                tempContainer.ServerInitialize(null, 1);
                if (!tempContainer.IsFull())
                {
                    task.Item.MoveToContainer(tempContainer);
                }

                SkinItemContainer(task.Crafter.owner, task.PlayerSettings, tempContainer, true);
                task.Item.MoveToContainer(task.Item.GetRootContainer());
            }
        }

        public class ItemTask
        {
            public Item Item { get; set; }
            public ItemCrafter Crafter { get; set; }
            public PlayerSettings PlayerSettings { get; set; }
        }
        #endregion

        #region Handlers
        private void UnsubscribeHooks()
        {
            if (!_config.SkinItemsOnCraft) Unsubscribe(nameof(OnItemCraftFinished));
            if (!_config.SkinItemsOnPickup) Unsubscribe(nameof(OnItemPickup));
            if (!_config.SkinKitsOnRedeemed) Unsubscribe(nameof(OnKitRedeemed));
        }

        private void SavePlayerSettings(BasePlayer player)
        {
            if (!_playerSettings.ContainsKey(player.userID)) return;
            Interface.GetMod().DataFileSystem.WriteObject($"SkinController{Path.DirectorySeparatorChar}PlayerOptions{Path.DirectorySeparatorChar}{player.userID}", _playerSettings[player.userID]);
        }

        private void RemoveSkin(string skinId)
        {
            if (_skinList.WorkshopSkins.Any(x => x.SkinID == skinId))
            {
                _skinList.WorkshopSkins.RemoveAll(x => x.SkinID == skinId);
                _skinList.BlacklistedSkins.Add(skinId);
            }
            if (_skinList.AddedSkins.Any(x => x.SkinID == skinId)) _skinList.AddedSkins.RemoveAll(x => x.SkinID == skinId);
        }

        private void LoadSavedPlayerSettings(BasePlayer player)
        {
            var playerOptions = Interface.GetMod().DataFileSystem.ReadObject<PlayerSettings>($"SkinController{Path.DirectorySeparatorChar}PlayerOptions{Path.DirectorySeparatorChar}{player.userID}") ?? new PlayerSettings();
            if (playerOptions.SkinsFilter != null && playerOptions.SkinsFilter.Any(x => !char.IsLetterOrDigit(x))) playerOptions.SkinsFilter = null;
            _playerSettings[player.userID] = playerOptions;
        }

        private PlayerSettings GetPlayerSettings(BasePlayer player)
        {
            if (!_playerSettings.ContainsKey(player.userID))
            {
                LoadSavedPlayerSettings(player);
            }
            if (_playerSettings[player.userID].SavedItems.Count == 0 && _playerSettings[player.userID].LoadedItems == null) _playerSettings[player.userID].SavedItems = new List<OutfitDetail>(_config.DefaultOutfit.Select(item => new OutfitDetail
            {
                ItemId = item.ItemId,
                Shortname = item.Shortname,
                SkinId = item.SkinId
            }));
            return _playerSettings[player.userID];
        }

        private void HandleSetItem(BasePlayer player, int itemId, string skinId, string shortname)
        {
            var playerSettings = GetPlayerSettings(player);
            var itemIsHazzy = _hazmatList.Contains(itemId.ToString());
            if (playerSettings.SavedItems.Any(x => x.ItemId == itemId || (itemIsHazzy && _hazmatList.Contains(x.ItemId.ToString()))))
            {
                var itemPlacement = playerSettings.SavedItems.FindIndex(x => x.ItemId == itemId || (itemIsHazzy && _hazmatList.Contains(x.ItemId.ToString())));
                playerSettings.SavedItems[itemPlacement] = new OutfitDetail { ItemId = itemId, Shortname = shortname, SkinId = skinId };
            }
            else playerSettings.SavedItems.Add(new OutfitDetail { ItemId = itemId, SkinId = skinId, Shortname = shortname });
            UIMainLoadoutDisplay(player);
        }

        private void HandleSetItemFull(BasePlayer player, string skinId)
        {
            var playerSettings = GetPlayerSettings(player);
            var skinList = new List<SkinData>(_skinList.WorkshopSkins.Select(item => new SkinData { SkinName = item.SkinName, ItemID = item.ItemID, ItemShortname = item.ItemShortname, SkinID = item.SkinID }));
            skinList.AddRange(_skinList.AddedSkins);
            var skin = skinList.FirstOrDefault(x => x.SkinID == skinId);
            if (skin == null || skin.SkinName == null)
            {
                SaveErrorPopup(player, "Could not decipher skin name", true);
                return;
            }
            var nameSplit = skin.SkinName.Split(' ');
            var skinName = nameSplit[0];
            var foundSkins = skinList.Where(x => x.SkinName?.ToLower().StartsWith(skinName.ToLower()) == true).ToList();
            playerSettings.SavedItems.Clear();
            foreach (var item in foundSkins)
            {
                if (playerSettings.SavedItems.Any(x => x.ItemId == item.ItemID))
                {
                    var itemPlacement = playerSettings.SavedItems.FindIndex(x => x.ItemId == item.ItemID);
                    playerSettings.SavedItems[itemPlacement] = new OutfitDetail { ItemId = item.ItemID, Shortname = item.ItemShortname, SkinId = item.SkinID };
                }
                else playerSettings.SavedItems.Add(new OutfitDetail { ItemId = item.ItemID, SkinId = item.SkinID, Shortname = item.ItemShortname });
            }
            UIMainLoadoutDisplay(player);
        }

        private void HandleRemoveItem(BasePlayer player, int itemId, bool isMain)
        {
            var playerSettings = GetPlayerSettings(player);
            playerSettings.SavedItems.RemoveAll(x => x.ItemId == itemId);
            if (isMain) UIMainLoadoutDisplay(player);
            else UIOutfitDisplay(player, isMain);
        }

        private void SaveErrorPopup(BasePlayer player, string errMsg, bool isNotOutfitEditor = false)
        {
            var container = new CuiElementContainer();

            var SCErrorPanel = CreatePanel(ref container, isNotOutfitEditor ? ".0 .85" : ".676 .66", isNotOutfitEditor ? ".25 .99" : ".926 .8", isNotOutfitEditor ? "0 0 0 .9" : "0 0 0 .7", "SCMainPanel", "SCErrPanel");
            CreatePanel(ref container, "0 0", ".015 1", $"{_config.UIButtonColor} 1", SCErrorPanel);
            CreateLabel(ref container, ".03 0", "1 1", "0 0 0 0", "1 1 1 1", errMsg, 15, TextAnchor.MiddleCenter, SCErrorPanel);

            CuiHelper.DestroyUi(player, "SCErrPanel");
            CuiHelper.AddUi(player, container);
            timer.Once(2, () => CuiHelper.DestroyUi(player, SCErrorPanel));
        }

        private void LoadDefaultOutfit(BasePlayer player, PlayerSettings playerSettings)
        {
            playerSettings.SavedItems = new List<OutfitDetail>(_config.DefaultOutfit.Select(item => new OutfitDetail
            {
                ItemId = item.ItemId,
                Shortname = item.Shortname,
                SkinId = item.SkinId
            }));
        }

        private void GetCollection(BasePlayer player, bool isChat, string[] args, ConsoleSystem.Arg arg = null)
        {
            if (args == null || args.Length == 0)
            {
                DetermineReply(player, isChat, arg, "No collection id provided");
                return;
            }

            try
            {
                webrequest.Enqueue("https://api.steampowered.com/ISteamRemoteStorage/GetCollectionDetails/v1/", $"?key={_config.SteamApiKey}&collectioncount=1&publishedfileids[0]={args[0]}",
                    (code, response) =>
                    {
                        if (code != 200)
                        {
                            DetermineReply(player, isChat, arg, $"Collection {args[0]} didn't return code 200 on request");
                            return;
                        }

                        if (response == null)
                        {
                            DetermineReply(player, isChat, arg, $"Collection {args[0]} not found, null reply");
                            return;
                        }
                        else
                        {
                            var collectionResponse = JsonConvert.DeserializeObject<CollectionRoot>(response);
                            if (collectionResponse.response.collectiondetails[0].children == null)
                            {
                                DetermineReply(player, isChat, arg, $"Collection {args[0]} not found");
                                return;
                            }
                            else DetermineReply(player, isChat, arg, $"Importing {collectionResponse.response.collectiondetails[0].children.Count} skins from collection [ {args[0]} ]");
                            foreach (var item in collectionResponse.response.collectiondetails[0].children)
                            {
                                webrequest.Enqueue("https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/", $"?key={_config.SteamApiKey}&itemcount=1&publishedfileids[0]={item.publishedfileid}",
                                    (skincode, skinresponse) => ServerMgr.Instance.StartCoroutine(AddWorkShopSkin(skincode, skinresponse, item.publishedfileid)), this, RequestMethod.POST);
                            }
                        }
                    }, this, RequestMethod.POST);
            }
            catch { DetermineReply(player, isChat, arg, $"Error getting skin info for {args[0]}"); }
        }

        private void GetSkin(BasePlayer player, bool isChat, string[] args, ConsoleSystem.Arg arg = null)
        {
            if (args == null || args.Length == 0 || args[0] == string.Empty || args[0] == null)
            {
                DetermineReply(player, isChat, arg, "No skin id provided");
                return;
            }
            var skinId = args[0];

            if (_skinList.AddedSkins.Any(x => x.SkinID == skinId) || _skinList.WorkshopSkins.Any(x => x.SkinID == skinId))
            {
                DetermineReply(player, isChat, arg, "Skin already in skins list");
            }
            else
            {
                try
                {
                    webrequest.Enqueue("https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/", $"?itemcount=1&publishedfileids[0]={skinId}",
                        (code, response) =>
                        {
                            if (code != 200)
                            {
                                DetermineReply(player, isChat, arg, $"Skin {skinId} didn't return code 200 on request");
                                return;
                            }

                            if (response == null)
                            {
                                DetermineReply(player, isChat, arg, $"Skin {skinId} not found, null reply");
                                return;
                            }
                            else
                            {
                                var workshopskin = JsonConvert.DeserializeObject<AddSkinRoot>(response);
                                if (workshopskin.response.publishedfiledetails[0].tags == null)
                                {
                                    DetermineReply(player, isChat, arg, $"Skin {skinId} not found");
                                    return;
                                }
                                else DetermineReply(player, isChat, arg, $"Importing {skinId} - [ {workshopskin.response.publishedfiledetails[0].title} ]");
                            }

                            ServerMgr.Instance.StartCoroutine(AddWorkShopSkin(code, response, skinId));
                        }, this, RequestMethod.POST);
                }
                catch { DetermineReply(player, isChat, arg, $"Error getting skin info for {skinId}"); };

            }
        }

        private void DetermineReply(BasePlayer player, bool isChat, ConsoleSystem.Arg arg, string reply)
        {
            if (isChat)
            {
                CreateGameTip(player, 3, 2, reply);
            }
            else
            {
                if (arg.Connection == null)
                    Interface.Oxide.LogInfo(reply);
                else SendReply(arg, reply);
            }
        }

        private IEnumerator AddWorkShopSkin(int code, string response, string skinId)
        {
            if (response != null && code == 200)
            {
                if (_skinList.AddedSkins.Any(x => x.SkinID == skinId) || _skinList.WorkshopSkins.Any(x => x.SkinID == skinId)) yield return null;
                var workshopskin = JsonConvert.DeserializeObject<AddSkinRoot>(response);
                
                foreach (var tag in workshopskin.response.publishedfiledetails[0].tags)
                {
                    if (_workshopRename.ContainsKey(tag.tag.ToLower().Replace(" ", "")))
                    {
                        var itemName = _workshopRename.FirstOrDefault(x => x.Key == tag.tag.ToLower().Replace(" ", ""));
                        if (!_skinList.AddedSkins.Any(x => x.SkinID == skinId) || _skinList.WorkshopSkins.Any(x => x.SkinID == skinId))
                        {
                            _skinList.AddedSkins.Add(new SkinData { SkinName = workshopskin.response.publishedfiledetails[0].title, SkinID = skinId, ItemID = itemName.Value.itemid, ItemShortname = itemName.Value.shortname });
                        }
                    }
                }
            }
            else
                yield return null;
        }

        private bool CheckPlayerPermissionSkins(BasePlayer player, bool isChat, ConsoleSystem.Arg arg = null)
        {
            if (isChat)
            {
                if (!permission.UserHasPermission(player.UserIDString, "skincontroller.addskins"))
                {
                    CreateGameTip(player, 3, 1, "You do not have permission");
                    return false;
                }
                else return true;
            }

            if (arg.Connection != null)
            {
                if (!permission.UserHasPermission(arg.Player().UserIDString, "skincontroller.addskins"))
                {
                    SendReply(arg, "You do not have permission");
                    return false;
                }
                else return true;
            }

            return true;
        }

        [ChatCommand("addskin")]
        void CmdAddSkin(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (CheckPlayerPermissionSkins(player, true))
            {
                GetSkin(player, true, args);
            }
        }

        [ChatCommand("addcollection")]
        void CmdAddCollection(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (CheckPlayerPermissionSkins(player, true))
            {
                GetCollection(player, true, args);
            }
        }

        private void SkinItemContainer(BasePlayer player, PlayerSettings playerSettings, ItemContainer container, bool playerInv = true, bool isInd = false, string InputSkinID = "0", int InputItemID = 0)
        {
            if (container == null) return;
            var itemsToRemove = new List<Item>();
            var itemsToAdd = new List<Item>();
            var theSkinInfo = new OutfitDetail();

            if (isInd)
            {
                var tempItemID = InputItemID;
                var tempSkinID = InputSkinID;
                if (tempItemID == 1266491000)
                {
                    tempItemID = int.Parse(InputSkinID);
                    tempSkinID = "0";
                }
                theSkinInfo = new OutfitDetail { ItemId = tempItemID, Shortname = String.Empty, SkinId = tempSkinID };
            }

            foreach (Item item in container.itemList)
            {
                if (!isInd) theSkinInfo = FindSkin(player, playerSettings, item, true);
                if (theSkinInfo == null) continue;
                var isHazzy = _hazmatList.Contains(theSkinInfo.ItemId.ToString());
                if (isHazzy && _hazmatList.Contains(item.info.itemid.ToString()))
                {
                    if (theSkinInfo.ItemId == item.info.itemid) continue;
                    var theHazmat = ItemManager.CreateByItemID(theSkinInfo.ItemId, item.amount, 0);
                    theHazmat.name = item.name;
                    theHazmat.text = item.text;
                    theHazmat.condition = item.condition;
                    theHazmat.position = item.position;
                    itemsToAdd.Add(theHazmat);
                    itemsToRemove.Add(item);
                }
                else if (item.info.itemid == theSkinInfo.ItemId)
                {
                    if (ulong.Parse(theSkinInfo.SkinId) == item.skin) continue;
                    if (playerInv)
                    {
                        Backpacks?.Call("API_MutateBackpackItems", player.userID,
                        new Dictionary<string, object> { ["ItemId"] = item.info.itemid },
                        new Dictionary<string, object> { ["SkinId"] = theSkinInfo.SkinId });
                    }

                    item.skin = ulong.Parse(theSkinInfo.SkinId);
                    if (item.GetHeldEntity() != null)
                    {
                        item.GetHeldEntity().skinID = ulong.Parse(theSkinInfo.SkinId);
                        item.GetHeldEntity().SendNetworkUpdateImmediate();
                    }
                    item.MarkDirty();
                }
            }

            foreach (var tempitem in itemsToRemove)
            {
                tempitem.RemoveFromContainer();
            }

            foreach (var tempitem in itemsToAdd)
            {
                tempitem.MoveToContainer(container, tempitem.position);
            }
            container.MarkDirty();
        }

        private void SkinSignleItem(BasePlayer player, PlayerSettings playerSettings, Item item)
        {
            if (item == null || player == null) return;
            var theSkinInfo = FindSkin(player, playerSettings, item, true);
            if (theSkinInfo == null || _hazmatList.Contains(theSkinInfo.ItemId.ToString())) return;
            if (item.info.itemid == theSkinInfo.ItemId)
            {
                if (ulong.Parse(theSkinInfo.SkinId) == item.skin) return;

                item.skin = ulong.Parse(theSkinInfo.SkinId);
                if (item.GetHeldEntity() != null)
                {
                    item.GetHeldEntity().skinID = ulong.Parse(theSkinInfo.SkinId);
                    item.GetHeldEntity().SendNetworkUpdateImmediate();
                }
                item.MarkDirty();
            }
        }


        OutfitDetail FindSkin(BasePlayer player, PlayerSettings playerSettings, Item item, bool playerInv, int itemId = 0, string skinId = null)
        {
            if (_hazmatList.Any(x => x == item.info.itemid.ToString()))
            {
                var savedItem = playerSettings.SavedItems.FirstOrDefault(x => _hazmatList.Contains(x.ItemId.ToString()));
                if (savedItem != null) return savedItem;


                if (playerSettings.Outfits.Count > 0 && playerSettings.FavoriteFallback)
                {
                    var favoriteOutfit = playerSettings.Outfits.FirstOrDefault(x => x.Favorite);
                    if (favoriteOutfit != null)
                    {
                        var favOutfitItem = favoriteOutfit.OutfitDetails.FirstOrDefault(x => _hazmatList.Contains(x.ItemId.ToString()));
                        if (favOutfitItem != null)
                        {
                            return favOutfitItem;
                        }

                    }
                }
            }
            else
            {
                if (itemId == 0)
                {
                    var savedItem = playerSettings.SavedItems.FirstOrDefault(x => x.ItemId == item.info.itemid || _hazmatList.Contains(item.info.itemid.ToString()));
                    if (savedItem != null)
                    {
                        return savedItem;
                    }

                    if (playerSettings.Outfits.Count > 0 && playerSettings.FavoriteFallback)
                    {
                        var favoriteOutfit = playerSettings.Outfits.FirstOrDefault(x => x.Favorite);
                        if (favoriteOutfit != null)
                        {
                            var favOutfitItem = favoriteOutfit.OutfitDetails.FirstOrDefault(x => x.ItemId == item.info.itemid || _hazmatList.Contains(item.info.itemid.ToString()));
                            if (favOutfitItem != null)
                            {
                                return favOutfitItem;
                            }
                        }
                    }
                }
                else
                {
                    var theItem = ItemManager.FindItemDefinition(itemId);
                    return new OutfitDetail { ItemId = theItem.itemid, Shortname = theItem.shortname, SkinId = skinId };
                }
            }

            return null;
        }

        private void FindBaseSkin(BasePlayer player, PlayerSettings playerSettings, BaseEntity entity)
        {
            if (playerSettings.SavedItems.Any(x => x.Shortname == convertedShortnames(entity.ShortPrefabName)))
            {
                SkinEntity(player, entity, playerSettings.SavedItems);
                return;
            }
            else if (playerSettings.Outfits.Count > 0 && playerSettings.FavoriteFallback)
            {
                var favoriteOutfit = playerSettings.Outfits.FirstOrDefault(x => x.Favorite);
                if (favoriteOutfit != null)
                {
                    SkinEntity(player, entity, favoriteOutfit.OutfitDetails);
                    return;
                }
            }
            return;
        }

        private void SkinEntity(BasePlayer player, BaseEntity entity, List<OutfitDetail> outfit)
        {
            var foundskin = outfit.FirstOrDefault(x => x.Shortname == convertedShortnames(entity.ShortPrefabName));
            if (foundskin != null)
            {
                if (entity.skinID != ulong.Parse(foundskin.SkinId))
                {
                    entity.skinID = ulong.Parse(foundskin.SkinId);
                    entity.SendNetworkUpdateImmediate();
                }
                return;
            }

        }

        private void SkinContainer(BasePlayer player, string commands, string[] args)
        {
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, "skincontroller.skincontainer")) CreateGameTip(player, 3, 1, Lang("NoPermission"));
            else
            {
                if (player.IsBuildingAuthed())
                {
                    RaycastHit hit;
                    if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f, LayerMask.GetMask("Deployed")))
                    {
                        BaseEntity entity = hit.GetEntity();
                        if (entity != null)
                        {
                            List<string> containerList = new List<string>() { "box.wooden.large", "woodbox_deployed", "coffinstorage", "locker.deployed" };
                            if (containerList.Contains(entity.ShortPrefabName))
                            {
                                var playerSettings = GetPlayerSettings(player);
                                StorageContainer container = entity as StorageContainer;
                                SkinItemContainer(player, playerSettings, container.inventory, false);
                                CreateGameTip(player, 3, 2, Lang("SkinningContainer"));
                            }
                        }
                    }
                }
                else CreateGameTip(player, 3, 1, Lang("NotAuthed"));
            }
        }

        private void SkinItemRaycast(BasePlayer player, string commands, string[] args)
        {
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, "skincontroller.skinitem")) CreateGameTip(player, 3, 1, Lang("NoPermission"));
            else
            {
                if (player.IsBuildingAuthed())
                {
                    RaycastHit hit;
                    if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f, LayerMask.GetMask("Deployed", "Construction")))
                    {
                        BaseEntity entity = hit.GetEntity();
                        if (entity != null)
                        {
                            var playerSettings = GetPlayerSettings(player);
                            FindBaseSkin(player, playerSettings, entity);
                        }
                        else CreateGameTip(player, 3, 1, Lang("NotValidDeployable"));
                    }
                }
                else CreateGameTip(player, 3, 1, Lang("NotAuthed"));
            }
        }

        private IEnumerator SkinPlayerBase(BasePlayer player, PlayerSettings playerSettings, List<BaseEntity> entities, uint? currentBuilding)
        {
            foreach (var entity in entities.Where(x => x.GetBuildingPrivilege()?.GetBuilding()?.ID == currentBuilding))
            {
                FindBaseSkin(player, playerSettings, entity);
                yield return null;
            }
        }

        private void SkinBase(BasePlayer player, string commands, string[] args)
        {
            if (player == null) return;
            if (!permission.UserHasPermission(player.UserIDString, "skincontroller.skinbase")) CreateGameTip(player, 3, 1, Lang("NoPermission"));
            else
            {
                var playerSettings = GetPlayerSettings(player);
                if (playerSettings.BaseSkinCooldown - DateTimeOffset.Now.ToUnixTimeSeconds() > 0) CreateGameTip(player, 3, 1, "On cooldown");
                else
                {
                    if (player.IsBuildingAuthed())
                    {
                        playerSettings.BaseSkinCooldown = DateTimeOffset.Now.ToUnixTimeSeconds() + _config.BaseSkinCooldownSeconds;
                        var entities = Physics.OverlapSphere(player.transform.position, 30, LayerMask.GetMask("Deployed", "Construction")).Select(x => x.ToBaseEntity()).Distinct().ToList();
                        var currentBuilding = player.GetBuildingPrivilege()?.GetBuilding()?.ID;
                        CreateGameTip(player, 3, 2, Lang("SkinningBase"));
                        InvokeHandler.Instance.StartCoroutine(SkinPlayerBase(player, playerSettings, entities, currentBuilding));
                    }
                    else CreateGameTip(player, 3, 1, Lang("NotAuthed"));
                }
            }
        }

        private string convertedShortnames(string deployedShortname)
        {
            if (_namesToConvert.ContainsKey(deployedShortname)) return _namesToConvert[deployedShortname];
            return deployedShortname;
        }

        private void CreateGameTip(BasePlayer player, float length, int type, string text)
        {
            if (player == null) return;
            player.SendConsoleCommand("gametip.hidegametip");
            player.SendConsoleCommand("gametip.showtoast", type, text);
            timer.Once(length, () => player?.SendConsoleCommand("gametip.hidegametip"));
        }
        #endregion

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Close"] = "CLOSE",
                ["Settings"] = "SETTINGS",
                ["OutfitText"] = "LOADED OUTFIT: {0}",
                ["ApplyToInv"] = "APPLY TO INVENTORY",
                ["ManageOutfits"] = "MANAGE OUTFITS",
                ["Weapons"] = "WEAPONS",
                ["Clothing"] = "CLOTHING",
                ["Deployables"] = "DEPLOYABLES",
                ["Tools"] = "TOOLS",
                ["Construction"] = "CONSTRUCTION",
                ["Search"] = "SEARCH",
                ["SkinButton"] = "SKIN",
                ["SetButton"] = "SET",
                ["Favorite"] = "FAVORITE",
                ["Favorited"] = "FAVORITED",
                ["NewSet"] = "+ NEW SET",
                ["Delete"] = "DELETE",
                ["Clear"] = "CLEAR",
                ["Edit"] = "EDIT",
                ["Save"] = "SAVE",
                ["SaveNewOutfit"] = "SAVE NEW OUTFIT",
                ["OutfitName"] = "OUTFIT NAME",
                ["OutfitEditor"] = "OUTFIT EDITOR",
                ["NamePlaceholder"] = "SET NAME",
                ["SkinCount"] = "{0} SKINS",
                ["NoOutfit"] = "No outfit",
                ["NoSkinsAvail"] = "NO SKINS AVAILABLE",
                ["OverrideOutfit"] = "OVERRIDE OUTFIT\n[ {0} ]",
                ["AutoSkinPickup"] = "AUTO SKIN ON PICKUP",
                ["AutoSkinCraft"] = "AUTO SKIN ON CRAFT",
                ["AutoSkinKitRedeem"] = "AUTO SKIN ON KIT REDEEMED",
                ["FavoriteFallback"] = "FAVORITE FALLBACK",
                ["OutfitNameSaved"] = "You already have an outfit with the name {0}",
                ["OutfitNameCharLimit"] = "Outfit name cannot be longer than 20 characters",
                ["OutfitSaveCount"] = "You've exceeded your save limit of {0}",
                ["NoPermission"] = "No permission to use this command",
                ["SkinningContainer"] = "Skinning items in container",
                ["NotAuthed"] = "You can only do this in a building authed zone",
                ["NotValidDeployable"] = "Not a valid deployable",
                ["SkinningBase"] = "Skinning base",
            }, this, "en");
        }

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        #endregion

        #region Hooks
        void OnServerInitialized(bool initial)
        {
            HandleCommands();
            HandlePermissions();

            _skinList.CategoryList.Add("Weapon", new List<int> { 1545779598, -1812555177, 1318558775, -2069578888, 1588298435, -778367295, 442886268 });
            _skinList.CategoryList.Add("Attire", new List<int> { -194953424, 1751045826, 1110385766, 1850456855, 237239288, -1549739227, -699558439, -803263829, -2002277461, 1266491000 });
            _skinList.CategoryList.Add("Tool", new List<int> { 1488979457 });
            _skinList.CategoryList.Add("Construction", new List<int> { 1390353317 });
            _skinList.CategoryList.Add("Items", new List<int> { 1534542921 });

            UnsubscribeHooks();

            if ((Steamworks.SteamInventory.Definitions?.Length ?? 0) == 0) Steamworks.SteamInventory.OnDefinitionsUpdated += StartSkinRequest;
            else StartSkinRequest();
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            SavePlayerSettings(player);

            _playerSettings.Remove(player.userID);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            LoadSavedPlayerSettings(player);
        }

        object OnItemPickup(Item item, BasePlayer player)
        {
            if (player == null) return null;
            var playerSettings = GetPlayerSettings(player);
            if (!playerSettings.SkinOnPickUp || !permission.UserHasPermission(player.UserIDString, "skincontroller.skinonpickup")) return null;
            SkinSignleItem(player, playerSettings, item);
            return null;
        }

        void OnItemCraftFinished(ItemCraftTask task, Item item, ItemCrafter crafter)
        {
            if (crafter == null) return;
            var playerSettings = GetPlayerSettings(crafter.owner);
            if (!playerSettings.SkinOnPickUp || !permission.UserHasPermission(crafter.owner.UserIDString, "skincontroller.skinoncraft")) return;

            itemTasks.Enqueue(new ItemTask { Item = item, Crafter = crafter, PlayerSettings = playerSettings });
        }


        private void OnKitRedeemed(BasePlayer player, string kitName)
        {
            var playerSettings = GetPlayerSettings(player);
            if (!playerSettings.SkinOnKitRedeem) return;
            SkinItemContainer(player, playerSettings, player.inventory.containerBelt, false);
            SkinItemContainer(player, playerSettings, player.inventory.containerMain, false);
            SkinItemContainer(player, playerSettings, player.inventory.containerWear, false);
        }
        void OnServerSave()
        {
            SaveSkins();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                SavePlayerSettings(player);
            }
        }

        private void Unload()
        {
            if (!Interface.Oxide.IsShuttingDown)
                SaveSkins();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                SavePlayerSettings(player);
                CuiHelper.DestroyUi(player, "SCMainPanel");
            }

            _config = null;
        }
        #endregion

        #region Command and Permission Handlers
        private void HandleCommands()
        {
            foreach (var command in _config.Commands.SkinMenuCommand)
                cmd.AddChatCommand(command, this, CMDOpenSkinUI);

            foreach (var command in _config.Commands.SkinItemCommand)
                cmd.AddChatCommand(command, this, SkinItemRaycast);

            foreach (var command in _config.Commands.SkinBaseCommand)
                cmd.AddChatCommand(command, this, SkinBase);

            foreach (var command in _config.Commands.SkinItemsInContainerCommand)
                cmd.AddChatCommand(command, this, SkinContainer);
        }

        private void HandlePermissions()
        {
            foreach (var perm in Permissions)
            {
                if (!permission.PermissionExists(perm, this))
                {
                    permission.RegisterPermission(perm, this);
                }
            }

            foreach (var perm in _config.OutfitPermissions)
            {
                if (!permission.PermissionExists(perm.Key, this))
                {
                    permission.RegisterPermission(perm.Key, this);
                }
            }
        }

        private int GetSaveCount(BasePlayer player)
        {
            if (_config.OutfitPermissions.Count != 0)
            {
                var sortedPermissions = _config.OutfitPermissions.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
                var maxSaves = 0;
                foreach (var perm in sortedPermissions)
                {
                    if (!permission.UserHasPermission(player.UserIDString, perm.Key)) continue;
                    else
                    {
                        maxSaves = perm.Value;
                        break;
                    }
                }
                return maxSaves;
            }
            return 10;
        }
        #endregion

        #region Console Commands
        [ConsoleCommand("sc_close")]
        private void CMDClose(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            CuiHelper.DestroyUi(arg.Player(), "SCMainPanel");
        }

        [ConsoleCommand("sc_settings")]
        private void CMDOpenSettingsMenu(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            var playerSettings = GetPlayerSettings(arg.Player());
            switch (arg.Args[0])
            {
                case "open":
                    UISettingsMenu(arg.Player());
                    break;
                case "close":
                    CuiHelper.DestroyUi(arg.Player(), "SCSettingsMain");
                    break;
                case "pickup":
                    if (playerSettings.SkinOnPickUp) playerSettings.SkinOnPickUp = false;
                    else playerSettings.SkinOnPickUp = true;
                    UISettingsMenu(arg.Player());
                    break;
                case "craft":
                    if (playerSettings.SkinOnCraft) playerSettings.SkinOnCraft = false;
                    else playerSettings.SkinOnCraft = true;
                    UISettingsMenu(arg.Player());
                    break;
                case "kit":
                    if (playerSettings.SkinOnKitRedeem) playerSettings.SkinOnKitRedeem = false;
                    else playerSettings.SkinOnKitRedeem = true;
                    UISettingsMenu(arg.Player());
                    break;
                case "favorite":
                    if (playerSettings.FavoriteFallback) playerSettings.FavoriteFallback = false;
                    else playerSettings.FavoriteFallback = true;
                    UISettingsMenu(arg.Player());
                    break;
            }
        }

        [ConsoleCommand("sc_display")]
        private void CMDOpenDisplayMenu(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;

            switch (arg.Args[0])
            {
                case "open":
                    UIOutfitDisplay(arg.Player(), bool.Parse(arg.Args[1]));
                    break;
                case "close":
                    if (bool.Parse(arg.Args[1]))
                    {
                        UIMainLoadoutDisplay(arg.Player());
                    }
                    else UIOutfitManager(arg.Player());
                    CuiHelper.DestroyUi(arg.Player(), "SCOutfitDisplayMain");
                    break;
            }
        }

        [ConsoleCommand("sc_setskin")]
        private void CMDSetSkin(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            if (arg.Args[0] == "full")
            {
                HandleSetItemFull(arg.Player(), arg.Args[1]);
            }
            else HandleSetItem(arg.Player(), int.Parse(arg.Args[1]), arg.Args[0], String.Join(" ", arg.Args.Skip(2)));
        }

        [ConsoleCommand("sc_skinfilter")]
        private void CMDSkinFilter(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            var playerSettings = GetPlayerSettings(arg.Player());
            if (arg.Args.Length == 0 || arg.Args[0].ToLower() == "search" || String.Join(" ", arg.Args).Any(x => !char.IsLetterOrDigit(x)))
            {
                playerSettings.SkinsFilter = null;
            }
            else playerSettings.SkinsFilter = String.Join(" ", arg.Args);
            UISkinSelection(arg.Player());
        }

        [ConsoleCommand("sc_remove")]
        private void CMDRemoveSkin(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            HandleRemoveItem(arg.Player(), int.Parse(arg.Args[0]), bool.Parse(arg.Args[1]));
        }

        [ConsoleCommand("sc_skin")]
        private void CMDSkin(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            var playerSettings = GetPlayerSettings(arg.Player());
            switch (arg.Args[0])
            {
                case "remove":
                    RemoveSkin(arg.Args[1]);
                    break;
                case "inv":
                    SkinItemContainer(arg.Player(), playerSettings, arg.Player().inventory.containerBelt, true);
                    SkinItemContainer(arg.Player(), playerSettings, arg.Player().inventory.containerMain, true);
                    SkinItemContainer(arg.Player(), playerSettings, arg.Player().inventory.containerWear, true);
                    CuiHelper.DestroyUi(arg.Player(), "SCMainPanel");
                    break;
                case "ind":
                    SkinItemContainer(arg.Player(), playerSettings, arg.Player().inventory.containerBelt, true, isInd: true, InputItemID: int.Parse(arg.Args[1]), InputSkinID: arg.Args[2]);
                    SkinItemContainer(arg.Player(), playerSettings, arg.Player().inventory.containerMain, true, isInd: true, InputItemID: int.Parse(arg.Args[1]), InputSkinID: arg.Args[2]);
                    SkinItemContainer(arg.Player(), playerSettings, arg.Player().inventory.containerWear, true, isInd: true, InputItemID: int.Parse(arg.Args[1]), InputSkinID: arg.Args[2]);
                    break;
            }
            UISkinSelection(arg.Player());
        }

        [ConsoleCommand("sc_category")]
        private void CMDCategory(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            var playerOptions = GetPlayerSettings(arg.Player());
            playerOptions.Category = arg.Args[0];
            playerOptions.SkinPage = 0;
            switch (playerOptions.Category)
            {
                case "Attire":
                    playerOptions.SelectedShortName = "metal.facemask";
                    playerOptions.CategoryItem = -194953424;
                    break;
                case "Weapon":
                    playerOptions.SelectedShortName = "rifle.ak";
                    playerOptions.CategoryItem = 1545779598;
                    break;
                case "Construction":
                    playerOptions.SelectedShortName = "door.hinged.metal";
                    playerOptions.CategoryItem = -2067472972;
                    break;
                case "Tool":
                    playerOptions.SelectedShortName = "jackhammer";
                    playerOptions.CategoryItem = 1488979457;
                    break;
                case "Items":
                    playerOptions.SelectedShortName = "furnace";
                    playerOptions.CategoryItem = -1999722522;
                    break;
            }
            UISkins(arg.Player());
        }

        [ConsoleCommand("sc_item")]
        private void CMDItem(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            var playerOptions = GetPlayerSettings(arg.Player());
            playerOptions.CategoryItem = int.Parse(arg.Args[0]);
            playerOptions.SkinPage = 0;
            UIItemSelection(arg.Player());
            UISkinSelection(arg.Player());
        }

        [ConsoleCommand("sc_page")]
        private void CMDPage(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            var playerOptions = GetPlayerSettings(arg.Player());
            switch (arg.Args[0])
            {
                case "catitem":
                    playerOptions.ItemPage = int.Parse(arg.Args[1]);
                    UIItemSelection(arg.Player());
                    break;
                case "skinitem":
                    playerOptions.SkinPage = int.Parse(arg.Args[1]);
                    UISkinSelection(arg.Player());
                    break;
                case "outfit":
                    playerOptions.OutfitPage = int.Parse(arg.Args[1]);
                    UIOutfitSelection(arg.Player());
                    break;
            }
        }

        [ConsoleCommand("sc_outfit")]
        private void CMDOutfit(ConsoleSystem.Arg arg)
        {
            if (arg.Player() == null) return;
            var playerSettings = GetPlayerSettings(arg.Player());
            switch (arg.Args[0])
            {
                case "open":
                    UIOutfitManager(arg.Player());
                    break;
                case "close":
                    UIMainLoadoutDisplay(arg.Player());
                    CuiHelper.DestroyUi(arg.Player(), "SCOutfitManager");
                    break;
                case "save":
                    playerSettings.ActiveSaveName = null;
                    UIOutfitEdit(arg.Player());
                    break;
                case "clear":
                    if (playerSettings.LoadedItems != null)
                        foreach (var skin in playerSettings.Outfits.FirstOrDefault(x => x.OutfitName == playerSettings.LoadedItems).OutfitDetails)
                            skin.SkinId = "0";
                    foreach (var skin in playerSettings.SavedItems)
                        skin.SkinId = "0";
                    UIOutfitManager(arg.Player());
                    UIOutfitSelection(arg.Player());
                    break;
                case "delete":
                    if (playerSettings.LoadedItems != null)
                    {
                        var isFavorite = playerSettings.Outfits.FirstOrDefault(x => x.OutfitName == playerSettings.LoadedItems).Favorite;
                        playerSettings.Outfits.RemoveAll(x => x.OutfitName == playerSettings.LoadedItems);
                        playerSettings.SavedItems = new List<OutfitDetail>();
                        playerSettings.LoadedItems = null;
                        if (isFavorite && playerSettings.Outfits.Count > 0)
                            playerSettings.Outfits[0].Favorite = true;
                        else
                        {
                            LoadDefaultOutfit(arg.Player(), playerSettings);
                        }
                    }
                    else
                    {
                        LoadDefaultOutfit(arg.Player(), playerSettings);
                    }
                    UIOutfitManager(arg.Player());
                    UIOutfitSelection(arg.Player());
                    break;
                case "new":
                    playerSettings.LoadedItems = null;
                    playerSettings.SavedItems = new List<OutfitDetail>();
                    LoadDefaultOutfit(arg.Player(), playerSettings);
                    UIOutfitManager(arg.Player());
                    UIOutfitSelection(arg.Player());
                    break;
                case "editclose":
                    CuiHelper.DestroyUi(arg.Player(), "SCOutfitEditPanel");
                    break;
                case "changename":
                    if (arg.Args.Length < 2) playerSettings.ActiveSaveName = null;
                    else playerSettings.ActiveSaveName = String.Join(" ", arg.Args.Skip(1));
                    UIOutfitEdit(arg.Player());
                    break;
                case "override":
                    if (playerSettings.Outfits.Any(x => x.OutfitName.ToLower() == playerSettings.ActiveSaveName.ToLower() && playerSettings.LoadedItems.ToLower() != playerSettings.ActiveSaveName.ToLower()))
                    {
                        SaveErrorPopup(arg.Player(), $"You already have an outfit with the name {playerSettings.ActiveSaveName}");
                        return;
                    }
                    if (playerSettings.ActiveSaveName.Length > 20)
                    {
                        SaveErrorPopup(arg.Player(), Lang("OutfitNameCharLimit"));
                        return;
                    }
                    var outfit = playerSettings.Outfits.FirstOrDefault(x => x.OutfitName == playerSettings.LoadedItems);
                    outfit.OutfitName = playerSettings.ActiveSaveName;
                    outfit.OutfitDetails = new List<OutfitDetail>(playerSettings.SavedItems.Select(item => new OutfitDetail
                    {
                        ItemId = item.ItemId,
                        Shortname = item.Shortname,
                        SkinId = item.SkinId
                    }));
                    playerSettings.ActiveSaveName = null;
                    CuiHelper.DestroyUi(arg.Player(), "SCOutfitEditPanel");
                    UIOutfitManager(arg.Player());
                    break;
                case "select":
                    if (playerSettings.LoadedItems != null && playerSettings.LoadedItems.ToLower() == String.Join(" ", arg.Args.Skip(1)).ToLower())
                    {
                        playerSettings.LoadedItems = null;
                        LoadDefaultOutfit(arg.Player(), playerSettings);
                    }
                    else
                    {
                        playerSettings.LoadedItems = String.Join(" ", arg.Args.Skip(1));
                        playerSettings.SavedItems = playerSettings.Outfits.FirstOrDefault(x => x.OutfitName == playerSettings.LoadedItems).OutfitDetails;
                    }
                    UIOutfitManager(arg.Player());
                    break;
                case "savenew":
                    var isFav = false;
                    var saveCount = GetSaveCount(arg.Player());
                    if (playerSettings.Outfits.Any(x => x.OutfitName.ToLower() == playerSettings.ActiveSaveName.ToLower()))
                    {
                        SaveErrorPopup(arg.Player(), $"You already have an outfit with the name {playerSettings.ActiveSaveName}");
                        return;
                    }
                    if (playerSettings.ActiveSaveName.Length > 20)
                    {
                        SaveErrorPopup(arg.Player(), Lang("OutfitNameCharLimit"));
                        return;
                    }
                    if (saveCount <= playerSettings.Outfits.Count)
                    {
                        SaveErrorPopup(arg.Player(), Lang("OutfitSaveCount", null, saveCount));
                        return;
                    }
                    if (playerSettings.Outfits.Count == 0) isFav = true;

                    playerSettings.Outfits.Add(new Outfits
                    {
                        OutfitName = playerSettings.ActiveSaveName,
                        Favorite = isFav,
                        OutfitDetails = new List<OutfitDetail>(playerSettings.SavedItems.Select(item => new OutfitDetail
                        {
                            ItemId = item.ItemId,
                            Shortname = item.Shortname,
                            SkinId = item.SkinId
                        }))
                    });
                    playerSettings.LoadedItems = playerSettings.ActiveSaveName;
                    playerSettings.ActiveSaveName = null;
                    CuiHelper.DestroyUi(arg.Player(), "SCOutfitEditPanel");
                    UIOutfitManager(arg.Player());
                    break;
                case "favorite":
                    var currentFavorite = playerSettings.Outfits.FirstOrDefault(x => x.Favorite);
                    if (currentFavorite != null) currentFavorite.Favorite = false;
                    var newFavorite = playerSettings.Outfits.FirstOrDefault(x => x.OutfitName.ToLower() == String.Join(" ", arg.Args.Skip(1)).ToLower());
                    if (newFavorite != null) newFavorite.Favorite = true;
                    UIOutfitManager(arg.Player());
                    break;
            }
        }

        [ConsoleCommand("skincontroller.addskin")]
        private void CMDAddSkin(ConsoleSystem.Arg arg)
        {
            if (CheckPlayerPermissionSkins(arg.Player(), false, arg))
            {
                GetSkin(arg.Player(), false, arg.Args, arg);
            }
        }

        [ConsoleCommand("skincontroller.addcollection")]
        private void CMDAddCollection(ConsoleSystem.Arg arg)
        {
            if (CheckPlayerPermissionSkins(arg.Player(), false, arg))
            {
                GetCollection(arg.Player(), false, arg.Args, arg);
            }
        }
        #endregion

        #region UIMain
        void APIOpenSkinUI(BasePlayer player)
        {
            var container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "WCInfoPanel");
            CreatePanel(ref container, "0 0", "1 1", "0 0 0 0", "WCSourcePanel", "SCMainPanel");
            UIOpenSkinMenu(player, ref container);
        }

        private void CMDOpenSkinUI(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "skincontroller.use"))
            {
                CreateGameTip(player, 3, 1, Lang("NoPermission"));
                return;
            }
            var container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1",
                    OffsetMin = "0 0",
                    OffsetMax = "0 0"
                },
                Image = { Color = _config.UIBackgroundColor, Material = "assets/content/ui/uibackgroundblur.mat" },
                CursorEnabled = true
            }, "Overlay", "SCMainPanel");
            UIOpenSkinMenu(player, ref container);
        }

        private void UIOpenSkinMenu(BasePlayer player, ref CuiElementContainer container)
        {
            var playerSettings = GetPlayerSettings(player);
            var saveCount = GetSaveCount(player);

            if (playerSettings.Outfits.Count > saveCount) playerSettings.Outfits.RemoveRange(saveCount, playerSettings.Outfits.Count - saveCount);

            CreatePanel(ref container, ".005 .89", ".995 .99", "0 0 0 .7", "SCMainPanel", "SCBannerPanel");
            CreateImagePanel(ref container, ".3 0", ".7 1", _config.UIImage, "SCBannerPanel", "SCBannerImg");
            var settingsPanel = CreatePanel(ref container, ".0025 .48", ".096 .95", "0 0 0 .5", "SCBannerPanel");
            CreateImagePanel(ref container, ".02 .12", ".245 .85", _config.SettingsIcon, settingsPanel, "SCSettingImg");
            CreateLabel(ref container, ".3 0", "1 1", "0 0 0 0", "1 1 1 1", Lang("Settings"), 17, TextAnchor.MiddleLeft, settingsPanel);
            CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, "sc_settings open", settingsPanel);

            CuiHelper.DestroyUi(player, "SCMainPanel");
            CuiHelper.AddUi(player, container);

            UIMainLoadoutDisplay(player);
            UISkins(player);
        }
        #endregion

        #region UISections
        private void UIMainLoadoutDisplay(BasePlayer player)
        {
            var container = new CuiElementContainer();
            var playerSettings = GetPlayerSettings(player);

            CreatePanel(ref container, ".005 .66", ".995 .88", "0 0 0 .7", "SCMainPanel", "SCMainLoadoutDisplay");
            CreateButton(ref container, ".965 .03", ".995 .26", $"{_config.UIButtonColor} .8", $"1 1 1 1", "+", 18, "sc_display open true", "SCMainLoadoutDisplay");

            CreateLabel(ref container, "0 .79", ".999 .99", "0 0 0 .5", "1 1 1 1", $" {Lang("OutfitText", null, (playerSettings.LoadedItems != null ? playerSettings.LoadedItems : Lang("NoOutfit")))}", 20, TextAnchor.MiddleLeft, "SCMainLoadoutDisplay", "SCMainLoadoutName");
            CreateButton(ref container, ".71 .15", ".875 .81", $"{_config.UISecondaryButtonColor} 1", $"1 1 1 1", Lang("ApplyToInv"), 18, "sc_skin inv", "SCMainLoadoutName");
            CreateButton(ref container, ".88 .15", ".995 .81", $"{_config.UIButtonColor} 1", $"1 1 1 1", Lang("ManageOutfits"), 18, "sc_outfit open", "SCMainLoadoutName");

            var i = 0;
            foreach (var item in playerSettings.SavedItems)
            {
                var tempItemId = item.ItemId;
                var tempSkinId = item.SkinId;

                if (tempItemId == 1266491000)
                {
                    tempItemId = int.Parse(item.SkinId);
                    tempSkinId = $"{item.ItemId}";
                }

                var AnchorMin = $"{.01 + (i * .05)} .0";
                var AnchorMax = $"{.087 + (i * .05)} .6";
                if (i % 2 == 0)
                {
                    AnchorMin = $"{.01 + (i * .05)} .15";
                    AnchorMax = $"{.087 + (i * .05)} .75";
                }
                if (i < 17)
                {
                    var itemPanel = CreateItemPanel(ref container, AnchorMin, AnchorMax, 0f, "0 0 0 0", tempItemId, "SCMainLoadoutDisplay", skinId: ulong.Parse(tempSkinId));
                    CreateButton(ref container, ".2 .2", ".8 .8", ".20 .92 .81 0", "1 1 1 1", " ", 15, $"sc_remove {item.ItemId} true", itemPanel);
                }
                else
                {
                    CreateButton(ref container, AnchorMin, AnchorMax, "0 0 0 0", "1 1 1 1", $"+ {playerSettings.SavedItems.Count - 17}", 20, "sc_display open true", "SCMainLoadoutDisplay");
                    break;
                }

                i++;
            }

            CuiHelper.DestroyUi(player, "SCMainLoadoutDisplay");
            CuiHelper.AddUi(player, container);
        }

        private void UISkins(BasePlayer player)
        {
            var container = new CuiElementContainer();
            var playerSettings = GetPlayerSettings(player);

            CreatePanel(ref container, ".005 .008", ".995 .65", "0 0 0 .7", "SCMainPanel", "SCSkinsPanel");
            CreateButton(ref container, ".005 .90", ".1975 .985", $"{(playerSettings.Category == "Weapon" ? _config.UIActiveButtonColor : _config.UIButtonColor)} 1", "1 1 1 1", Lang("Weapons"), 20, "sc_category Weapon", "SCSkinsPanel");
            CreateButton(ref container, ".2025 .90", ".3975 .985", $"{(playerSettings.Category == "Attire" ? _config.UIActiveButtonColor : _config.UIButtonColor)} 1", "1 1 1 1", Lang("Clothing"), 20, "sc_category Attire", "SCSkinsPanel");
            CreateButton(ref container, ".4025 .90", ".5975 .985", $"{(playerSettings.Category == "Items" ? _config.UIActiveButtonColor : _config.UIButtonColor)} 1", "1 1 1 1", Lang("Deployables"), 20, "sc_category Items", "SCSkinsPanel");
            CreateButton(ref container, ".6025 .90", ".7975 .985", $"{(playerSettings.Category == "Tool" ? _config.UIActiveButtonColor : _config.UIButtonColor)} 1", "1 1 1 1", Lang("Tools"), 20, "sc_category Tool", "SCSkinsPanel");
            CreateButton(ref container, ".8025 .90", ".995 .985", $"{(playerSettings.Category == "Construction" ? _config.UIActiveButtonColor : _config.UIButtonColor)} 1", "1 1 1 1", Lang("Construction"), 20, "sc_category Construction", "SCSkinsPanel");

            CuiHelper.DestroyUi(player, "SCSkinsPanel");
            CuiHelper.AddUi(player, container);

            UIItemSelection(player);
            UISkinSelection(player);
        }

        private void UIItemSelection(BasePlayer player)
        {
            var container = new CuiElementContainer();
            var playerSettings = GetPlayerSettings(player);

            var maxPage = _skinList.CategoryList[playerSettings.Category].Count / 16;
            if (playerSettings.ItemPage > maxPage) playerSettings.ItemPage = 0;
            if (playerSettings.ItemPage < 0) playerSettings.ItemPage = maxPage;
            CreatePanel(ref container, "0 .75", "1 .89", "0 0 0 0", "SCSkinsPanel", "SCItemSelection");
            if (maxPage > 0)
            {
                CreateButton(ref container, ".005 0", ".055 .955", $"{_config.UIButtonColor} 1", "1 1 1 1", "<", 20, $"sc_page catitem {playerSettings.ItemPage - 1}", "SCItemSelection");
                CreateButton(ref container, ".944 0", ".994 .955", $"{_config.UIButtonColor} 1", "1 1 1 1", ">", 20, $"sc_page catitem {playerSettings.ItemPage + 1}", "SCItemSelection");
            }
            else
            {
                CreatePanel(ref container, ".005 0", ".055 .955", $"{_config.UISecondaryButtonColor} .7", "SCItemSelection");
                CreatePanel(ref container, ".944 0", ".994 .955", $"{_config.UISecondaryButtonColor} .7", "SCItemSelection");
            }

            var i = 0;
            foreach (var item in _skinList.CategoryList[playerSettings.Category].Skip(playerSettings.ItemPage * 16).Take(16))
            {
                var itemPanel = CreateItemPanel(ref container, $"{.06 + (i * .0553)} 0", $"{.11 + (i * .0553)} .955", 0, playerSettings.CategoryItem == item ? $"{_config.UIButtonColor} .4" : "1 1 1 .15", item, "SCItemSelection");
                CreateButton(ref container, "0 0", $"1 1", $"0 0 0 0", "1 1 1 1", " ", 15, $"sc_item {item}", itemPanel);
                i++;
            }

            CuiHelper.DestroyUi(player, "SCItemSelection");
            CuiHelper.AddUi(player, container);
        }

        private void UISkinSelection(BasePlayer player)
        {
            var container = new CuiElementContainer();
            var playerSettings = GetPlayerSettings(player);
            List<SkinData> skinList = new List<SkinData>(_skinList.WorkshopSkins.Select(item => new SkinData
            {
                ItemID = item.ItemID,
                SkinID = item.SkinID,
                SkinName = item.SkinName,
                ItemShortname = item.ItemShortname
            }));
            skinList.AddRange(new List<SkinData>(_skinList.AddedSkins.Select(item => new SkinData
            {
                ItemID = item.ItemID,
                SkinID = item.SkinID,
                SkinName = item.SkinName,
                ItemShortname = item.ItemShortname
            })));
            if (playerSettings.SkinsFilter != null)
            {
                skinList = skinList.Where(x => x.SkinName.ToLower().Contains(playerSettings.SkinsFilter.ToLower())).ToList();
            }
            else skinList = skinList.Where(x => x.ItemID == playerSettings.CategoryItem).ToList();

            var maxPage = skinList.Count / 33;
            if (playerSettings.SkinPage > maxPage) playerSettings.SkinPage = 0;
            if (playerSettings.SkinPage < 0) playerSettings.SkinPage = maxPage;
            CreatePanel(ref container, "0 .013", "1 .74", "0 0 0 0", "SCSkinsPanel", "SCSkinSelection");
            CreateLabel(ref container, ".03 0", ".15 .06", $"{_config.UIButtonColor} 1", "1 1 1 1", $"{playerSettings.SkinPage + 1} / {maxPage + 1}", 15, TextAnchor.MiddleCenter, "SCSkinSelection");
            CreateInput(ref container, ".155 0", ".28 .06", "sc_skinfilter", $"{_config.UIButtonColor} .7", "1 1 1 1", playerSettings.SkinsFilter == null ? Lang("Search") : playerSettings.SkinsFilter, 15, TextAnchor.MiddleCenter, "SCSkinSelection");
            CreateButton(ref container, ".43 0", ".57 .06", $"{_config.UIButtonColor} 1", $"1 1 1 1", Lang("Close"), 15, "sc_close", "SCSkinSelection");

            if (maxPage > 0)
            {
                CreateButton(ref container, ".005 0", ".025 .992", $"{_config.UIButtonColor} 1", "1 1 1 1", "<", 20, $"sc_page skinitem {playerSettings.SkinPage - 1}", "SCSkinSelection");
                CreateButton(ref container, ".974 0", ".994 .992", $"{_config.UIButtonColor} 1", "1 1 1 1", ">", 20, $"sc_page skinitem {playerSettings.SkinPage + 1}", "SCSkinSelection");
            }
            else
            {
                CreatePanel(ref container, ".005 0", ".025 .992", $"{_config.UISecondaryButtonColor} .9", "SCSkinSelection");
                CreatePanel(ref container, ".973 0", ".993 .992", $"{_config.UISecondaryButtonColor} .9", "SCSkinSelection");
            }

            var buttonHeight = 0;
            var i = 0;
            if (skinList.Count == 0)
                CreateLabel(ref container, "0 .1", "1 .9", "0 0 0 0", "1 1 1 .3", Lang("NoSkinsAvail"), 70, TextAnchor.MiddleCenter, "SCSkinSelection");
            else
                foreach (var skin in skinList.Skip(playerSettings.SkinPage * 33).Take(33))
                {
                    if (i == 11)
                    {
                        i = 0;
                        buttonHeight++;
                    }

                    int tempItemID = skin.ItemID;
                    string tempSkinID = skin.SkinID;
                    string theSkinId = skin.SkinID;

                    var itemName = skin.SkinName;
                    if (skin.SkinName.Length > 20)
                    {
                        itemName = itemName.Substring(0, 20) + "...";
                    }

                    if (tempItemID == 1266491000)
                    {
                        tempItemID = int.Parse(tempSkinID);
                        tempSkinID = "0";
                    }

                    var itemPanel = CreatePanel(ref container, $"{.035 + (i * .085)} {.7 - (buttonHeight * .31)}", $"{.115 + (i * .085)} {.992 - (buttonHeight * .31)}", "1 1 1 .15", "SCSkinSelection");
                    CreateItemPanel(ref container, ".15 .15", ".85 .85", 0f, "0 0 0 0", tempItemID, itemPanel, skinId: ulong.Parse(tempSkinID));
                    CreateLabel(ref container, "0 .85", ".99 1", "1 1 1 .15", "1 1 1 1", itemName, 10, TextAnchor.MiddleCenter, itemPanel);
                    if (permission.UserHasPermission(player.UserIDString, "skincontroller.addskins")) CreateButton(ref container, ".86 .70", ".98 .825", "1 1 1 .2", "1 1 1 1", "X", 10, $"sc_skin remove {tempSkinID}", itemPanel);
                    CreateButton(ref container, "0 .70", ".2 .825", "1 1 1 .2", "1 1 1 1", "++", 10, $"sc_setskin full {theSkinId}", itemPanel);
                    CreateButton(ref container, ".05 .04", ".475 .15", "1 1 1 .2", "1 1 1 1", Lang("SkinButton"), 10, $"sc_skin ind {skin.ItemID} {skin.SkinID}", itemPanel);
                    CreateButton(ref container, ".525 .04", ".93 .15", "1 1 1 .2", "1 1 1 1", Lang("SetButton"), 10, $"sc_setskin {tempSkinID} {tempItemID} {skin.ItemShortname}", itemPanel);

                    i++;
                }

            CuiHelper.DestroyUi(player, "SCSkinSelection");
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region UISettings
        private void UISettingsMenu(BasePlayer player)
        {
            var container = new CuiElementContainer();
            var playerSettings = GetPlayerSettings(player);

            CreatePanel(ref container, "0 0", "1 1", ".14 .22 .33 .7", "SCMainPanel", "SCSettingsMain", true);
            CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, "sc_settings close", "SCSettingsMain");
            CreatePanel(ref container, ".325 .10", ".675 .9", "0 0 0 .6", "SCSettingsMain", "SCSettingsPanel");

            CreateLabel(ref container, "0 .9", "1 1", "0 0 0 .6", "1 1 1 1", Lang("Settings"), 35, TextAnchor.MiddleCenter, "SCSettingsPanel", "SCSettingsBanner");
            CreateButton(ref container, ".9 0", "1 1", "0 0 0 0", "1 1 1 1", "X", 35, "sc_settings close", "SCSettingsBanner");

            CreateLabel(ref container, ".25 .81", ".75 .85", "0 0 0 .6", "1 1 1 1", Lang("AutoSkinPickup"), 13, TextAnchor.MiddleCenter, "SCSettingsPanel");
            var pickupPanel = CreatePanel(ref container, ".25 .73", ".75 .8", "0 0 0 .7", "SCSettingsPanel");
            CreateButton(ref container, playerSettings.SkinOnPickUp ? ".505 .065" : ".01 .065", playerSettings.SkinOnPickUp ? ".98 .895" : ".495 .9", playerSettings.SkinOnPickUp ? ".13 .75 .21 1" : ".75 .13 .13 1", "1 1 1 1", " ", 15, "sc_settings pickup", pickupPanel);
            if (!permission.UserHasPermission(player.UserIDString, "skincontroller.skinonpickup")) CreatePanel(ref container, ".23 .72", ".77 .86", $"{_config.UIDangerButtonColor} .1", "SCSettingsPanel", blur: true);

            CreateLabel(ref container, ".25 .62", ".75 .66", "0 0 0 .6", "1 1 1 1", Lang("AutoSkinCraft"), 13, TextAnchor.MiddleCenter, "SCSettingsPanel");
            var craftPanel = CreatePanel(ref container, ".25 .54", ".75 .61", "0 0 0 .7", "SCSettingsPanel");
            CreateButton(ref container, playerSettings.SkinOnCraft ? ".505 .065" : ".01 .065", playerSettings.SkinOnCraft ? ".98 .885" : ".495 .895", playerSettings.SkinOnCraft ? ".13 .75 .21 1" : ".75 .13 .13 1", "1 1 1 1", " ", 15, "sc_settings craft", craftPanel);
            if (!permission.UserHasPermission(player.UserIDString, "skincontroller.skinoncraft")) CreatePanel(ref container, ".23 .54", ".77 .67", $"{_config.UIDangerButtonColor} .1", "SCSettingsPanel", blur: true);

            CreateLabel(ref container, ".25 .43", ".75 .47", "0 0 0 .6", "1 1 1 1", Lang("AutoSkinKitRedeem"), 13, TextAnchor.MiddleCenter, "SCSettingsPanel");
            var kitPanel = CreatePanel(ref container, ".25 .35", ".75 .42", "0 0 0 .7", "SCSettingsPanel");
            CreateButton(ref container, playerSettings.SkinOnKitRedeem ? ".505 .06" : ".01 .06", playerSettings.SkinOnKitRedeem ? ".98 .895" : ".495 .91", playerSettings.SkinOnKitRedeem ? ".13 .75 .21 1" : ".75 .13 .13 1", "1 1 1 1", " ", 15, "sc_settings kit", kitPanel);
            if (!permission.UserHasPermission(player.UserIDString, "skincontroller.skinonkitredeemed")) CreatePanel(ref container, ".23 .34", ".77 .48", $"{_config.UIDangerButtonColor} .1", "SCSettingsPanel", blur: true);

            CreateLabel(ref container, ".25 .24", ".75 .28", "0 0 0 .6", "1 1 1 1", Lang("FavoriteFallback"), 13, TextAnchor.MiddleCenter, "SCSettingsPanel");
            var favoritePanel = CreatePanel(ref container, ".25 .16", ".75 .23", "0 0 0 .7", "SCSettingsPanel");
            CreateButton(ref container, playerSettings.FavoriteFallback ? ".505 .065" : ".01 .065", playerSettings.FavoriteFallback ? ".98 .895" : ".495 .9", playerSettings.FavoriteFallback ? ".13 .75 .21 1" : ".75 .13 .13 1", "1 1 1 1", " ", 15, "sc_settings favorite", favoritePanel);

            CuiHelper.DestroyUi(player, "SCSettingsMain");
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region UIOutfitDisplay
        private void UIOutfitDisplay(BasePlayer player, bool isMain)
        {
            var container = new CuiElementContainer();
            var playerSettings = GetPlayerSettings(player);

            CreatePanel(ref container, "0 0", "1 1", ".14 .22 .33 .7", "SCMainPanel", "SCOutfitDisplayMain", true);
            CreateLabel(ref container, ".115 .80", ".885 .95", "0 0 0 .7", "1 1 1 1", playerSettings.LoadedItems == null ? Lang("NoOutfit") : playerSettings.LoadedItems.ToUpper(), 55, TextAnchor.MiddleCenter, "SCOutfitDisplayMain");
            CreatePanel(ref container, ".02 .8", ".105 .95", "0 0 0 .7", "SCOutfitDisplayMain");
            CreatePanel(ref container, ".895 .8", ".98 .95", "0 0 0 .7", "SCOutfitDisplayMain");


            var skinsPanel = CreatePanel(ref container, ".02 .05", ".98 .78", "0 0 0 .7", "SCOutfitDisplayMain");
            var buttonHeight = 0;
            var i = 0;
            foreach (var skin in playerSettings.SavedItems)
            {
                if (i == 10)
                {
                    i = 0;
                    buttonHeight++;
                }

                var itemPanel = CreateItemPanel(ref container, $"{.005 + (i * .0995)} {.767 - (buttonHeight * .23)}", $"{.0995 + (i * .0995)} {.987 - (buttonHeight * .23)}", 0f, "1 1 1 .15", skin.ItemId, skinsPanel, skinId: ulong.Parse(skin.SkinId));
                CreateButton(ref container, "0 0", "1 1", "1 1 1 0", "1 1 1 1", " ", 10, $"sc_remove {skin.ItemId} false", itemPanel);
                i++;
            }

            CreateButton(ref container, ".43 .008", ".57 .04", $"{_config.UIButtonColor} 1", "1 1 1 1", Lang("Close"), 15, $"sc_display close {isMain}", "SCOutfitDisplayMain");

            CuiHelper.DestroyUi(player, "SCOutfitDisplayMain");
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region UIOutfitManager
        private void UIOutfitManager(BasePlayer player)
        {
            var container = new CuiElementContainer();
            var playerSettings = GetPlayerSettings(player);

            CreatePanel(ref container, "0 0", "1 1", ".14 .22 .33 .7", "SCMainPanel", "SCOutfitManager", true);
            CreateButton(ref container, ".43 .01", ".57 .05", $"{_config.UIButtonColor} 1", "1 1 1 1", Lang("Close"), 15, "sc_outfit close", "SCOutfitManager");

            var outfitPanel = CreatePanel(ref container, ".005 .75", ".993 .99", "0 0 0 .6", "SCOutfitManager");
            var outfitTitle = CreateLabel(ref container, "0 .8", "1 1", "0 0 0 .5", "1 1 1 1", $" {Lang("OutfitText", null, (playerSettings.LoadedItems != null ? playerSettings.LoadedItems : Lang("NoOutfit")))}", 20, TextAnchor.MiddleLeft, outfitPanel);
            CreateButton(ref container, ".5075 .12", ".5995 .8", $"{_config.UISafeButtonColor} 1", "1 1 1 1", Lang("NewSet"), 20, "sc_outfit new", outfitTitle);
            CreateButton(ref container, ".605 .12", ".697 .8", $"{_config.UIDangerButtonColor} 1", "1 1 1 1", Lang("Delete"), 20, "sc_outfit delete", outfitTitle);
            CreateButton(ref container, ".7025 .12", ".795 .8", $"{_config.UIDangerButtonColor} 1", "1 1 1 1", Lang("Clear"), 20, "sc_outfit clear", outfitTitle);
            CreateButton(ref container, ".8 .12", ".8925 .8", $"{_config.UISecondaryButtonColor} 1", "1 1 1 1", Lang("Edit"), 20, "sc_outfit close", outfitTitle);
            CreateButton(ref container, ".8975 .12", ".995 .8", $"{_config.UIButtonColor} 1", "1 1 1 1", Lang("Save"), 20, "sc_outfit save", outfitTitle);

            var i = 0;
            foreach (var item in playerSettings.SavedItems)
            {
                var AnchorMin = $"{.01 + (i * .053)} .0";
                var AnchorMax = $"{.097 + (i * .053)} .6";
                if (i % 2 == 0)
                {
                    AnchorMin = $"{.01 + (i * .053)} .15";
                    AnchorMax = $"{.09 + (i * .053)} .75";
                }
                if (i < 17)
                    CreateItemPanel(ref container, AnchorMin, AnchorMax, 0f, "1 1 1 0", item.ItemId, outfitPanel, skinId: ulong.Parse(item.SkinId));
                else
                {
                    CreateButton(ref container, AnchorMin, AnchorMax, "0 0 0 0", "1 1 1 1", $"+ {playerSettings.SavedItems.Count - 17}", 20, "sc_display open false", outfitPanel);
                    break;
                }

                i++;
            }

            CuiHelper.DestroyUi(player, "SCOutfitManager");
            CuiHelper.AddUi(player, container);

            UIOutfitSelection(player);
        }

        private void UIOutfitSelection(BasePlayer player)
        {
            var container = new CuiElementContainer();
            var playerSettings = GetPlayerSettings(player);
            var maxPage = playerSettings.Outfits.Count / 8;
            if (playerSettings.OutfitPage > maxPage) playerSettings.OutfitPage = 0;
            if (playerSettings.OutfitPage < 0) playerSettings.OutfitPage = maxPage;

            CreatePanel(ref container, ".005 .055", ".993 .74", "0 0 0 0", "SCOutfitManager", "SCOutfitSelectionPanel");
            if (playerSettings.Outfits.Count > 8)
            {
                CreateButton(ref container, ".4 .01", ".425 .05", $"{_config.UIButtonColor} 1", "1 1 1 1", "<", 15, $"sc_page outfit {playerSettings.OutfitPage - 1}", "SCOutfitManager");
                CreateButton(ref container, ".575 .01", ".6 .05", $"{_config.UIButtonColor} 1", "1 1 1 1", ">", 15, $"sc_page outfit {playerSettings.OutfitPage + 1}", "SCOutfitManager");
            }

            var i = 0;
            var btni = 0;
            foreach (var outfit in playerSettings.Outfits.Skip(playerSettings.OutfitPage * 8).Take(8))
            {
                var btnPositions = new List<string> { $"0 {.76 - (btni * .25)}", $".4975 {1 - (btni * .25)}" };
                if (i % 2 == 1)
                {
                    btnPositions = new List<string> { $".5025 {.76 - (btni * .25)}", $"1 {1 - (btni * .25)}" };
                    btni++;
                }
                var outfitPanel = CreatePanel(ref container, btnPositions[0], btnPositions[1], playerSettings.LoadedItems == outfit.OutfitName ? "1 .96 .5 .4" : "0 0 0 .6", "SCOutfitSelectionPanel");
                var outfitTitle = CreateLabel(ref container, "0 .75", ".998 .99", "0 0 0 .5", "1 1 1 1", $" {outfit.OutfitName.ToUpper()}", 18, TextAnchor.MiddleLeft, outfitPanel);
                if (outfit.Favorite)
                    CreateLabel(ref container, ".85 .15", ".99 .79", $"{_config.UIButtonColor} 1", "1 1 1 1", Lang("Favorited"), 17, TextAnchor.MiddleCenter, outfitTitle);
                else CreateButton(ref container, ".85 .15", ".99 .79", $"{_config.UIActiveButtonColor} 1", "1 1 1 1", Lang("Favorite"), 17, $"sc_outfit favorite {outfit.OutfitName}", outfitTitle);

                var itemsCount = 0;
                foreach (var item in outfit.OutfitDetails)
                {
                    var AnchorMin = $"{.01 + (itemsCount * .066)} .01";
                    var AnchorMax = $"{.11 + (itemsCount * .066)} .55";
                    if (itemsCount % 2 == 0)
                    {
                        AnchorMin = $"{.01 + (itemsCount * .066)} .2";
                        AnchorMax = $"{.11 + (itemsCount * .066)} .74";
                    }
                    if (itemsCount < 13)
                    {
                        var itemPanel = CreateItemPanel(ref container, AnchorMin, AnchorMax, 0f, "1 1 1 0", item.ItemId, outfitPanel, skinId: ulong.Parse(item.SkinId));
                    }
                    else
                    {
                        CreateLabel(ref container, AnchorMin, AnchorMax, "0 0 0 0", "1 1 1 1", $"+ {outfit.OutfitDetails.Count - 13}", 20, TextAnchor.MiddleCenter, outfitPanel);
                        break;
                    }

                    itemsCount++;
                }
                CreateButton(ref container, "0 0", ".998 .745", "0 0 0 0", "0 0 0 0", " ", 15, $"sc_outfit select {outfit.OutfitName}", outfitPanel);

                i++;
            }

            CuiHelper.DestroyUi(player, "SCOutfitSelectionPanel");
            CuiHelper.AddUi(player, container);
        }

        private void UIOutfitEdit(BasePlayer player)
        {
            var container = new CuiElementContainer();
            var playerSettings = GetPlayerSettings(player);
            if (playerSettings.ActiveSaveName == null) playerSettings.ActiveSaveName = playerSettings.LoadedItems;

            var panelBackground = CreatePanel(ref container, "0 0", "1 1", ".14 .22 .33 .7", "SCMainPanel", "SCOutfitEditPanel", true);
            CreateButton(ref container, "0 0", "1 1", "0 0 0 0", "1 1 1 1", " ", 15, "sc_outfit editclose", panelBackground);
            var panelMain = CreatePanel(ref container, ".325 .20", ".675 .8", "0 0 0 .6", panelBackground, "SCOutfitEPanel");

            CreateLabel(ref container, "0 .9", "1 1", "0 0 0 .6", "1 1 1 1", Lang("OutfitEditor"), 35, TextAnchor.MiddleCenter, panelMain, "SCOutfitEditBanner");
            CreateButton(ref container, ".9 0", "1 1", "0 0 0 0", "1 1 1 1", "X", 35, "sc_outfit editclose", "SCOutfitEditBanner");

            CreateLabel(ref container, ".1 .79", ".9 .85", "0 0 0 .6", "1 1 1 1", Lang("OutfitName"), 13, TextAnchor.MiddleCenter, panelMain);
            CreateInput(ref container, ".10 .62", ".9 .78", "sc_outfit changename", "0 0 0 .6", "1 1 1 1", playerSettings.ActiveSaveName != null ? playerSettings.ActiveSaveName : "SET NAME", 25, TextAnchor.MiddleCenter, panelMain);
            CreateLabel(ref container, ".1 .53", ".9 .61", "0 0 0 .6", "1 1 1 1", Lang("SkinCount", null, playerSettings.SavedItems.Count), 25, TextAnchor.MiddleCenter, panelMain);


            if (playerSettings.LoadedItems != null)
            {
                CreateButton(ref container, ".1 .3", ".9 .5", $"{_config.UISecondaryButtonColor} .8", "1 1 1 1", Lang("OverrideOutfit", null, playerSettings.LoadedItems), 25, "sc_outfit override", panelMain);
            }
            else CreateLabel(ref container, ".1 .3", ".9 .5", $"{_config.UISecondaryButtonColor} .8", "1 1 1 .3", $"- - -", 25, TextAnchor.MiddleCenter, panelMain);
            CreateButton(ref container, ".1 .08", ".9 .28", $"{_config.UIButtonColor} .8", "1 1 1 1", Lang("SaveNewOutfit"), 25, "sc_outfit savenew", panelMain);

            CuiHelper.DestroyUi(player, "SCOutfitEditPanel");
            CuiHelper.AddUi(player, container);
        }
        #endregion

        #region Load Skins
        public class ClassedItem
        {
            public int itemid { get; set; }
            public string shortname { get; set; }
        }

        private void StartSkinRequest()
        {
            ServerMgr.Instance.StartCoroutine(LoadNewSkins());
        }

        private readonly Dictionary<string, ClassedItem> _workshopRename = new Dictionary<string, ClassedItem>
        {
            {"ak47", new ClassedItem { itemid = 1545779598, shortname = "rifle.ak" }},
            {"balaclava", new ClassedItem { itemid = -2012470695, shortname = "mask.balaclava" }},
            {"bandana", new ClassedItem { itemid = -702051347, shortname = "mask.bandana" }},
            {"bearrug", new ClassedItem { itemid = -1104881824, shortname = "rug.bear" }},
            {"bearskinrug", new ClassedItem { itemid = -1104881824, shortname = "rug.bear" }},
            {"beenie", new ClassedItem { itemid = 1675639563, shortname = "hat.beenie" }},
            {"boltrifle", new ClassedItem { itemid = 1588298435, shortname = "rifle.bolt" }},
            {"boonie", new ClassedItem { itemid =   -23994173, shortname = "hat.boonie" }},
            {"buckethat", new ClassedItem { itemid = 850280505, shortname = "bucket.helmet" }},
            {"burlapgloves", new ClassedItem { itemid = 1366282552, shortname = "burlap.gloves" }},
            {"burlappants", new ClassedItem { itemid = 1992974553, shortname = "burlap.trousers" }},
            {"cap", new ClassedItem { itemid = -1022661119, shortname = "hat.cap" }},
            {"collaredshirt", new ClassedItem { itemid = -2025184684, shortname = "shirt.collared" }},
            {"deerskullmask", new ClassedItem { itemid = -1903165497, shortname = "deer.skull.mask" }},
            {"hideshirt", new ClassedItem { itemid = 196700171, shortname = "attire.hide.vest" }},
            {"hideshoes", new ClassedItem { itemid = 794356786, shortname = "attire.hide.boots" }},
            {"l96", new ClassedItem { itemid = -778367295, shortname = "rifle.l96" }},
            {"leather.gloves", new ClassedItem { itemid = 1366282552, shortname = "burlap.gloves" }},
            {"longtshirt", new ClassedItem { itemid = 935692442, shortname = "tshirt.long" }},
            {"lr300", new ClassedItem { itemid = -1812555177, shortname = "rifle.lr300" }},
            {"lr300.item", new ClassedItem { itemid = -1812555177, shortname = "rifle.lr300" }},
            {"m39", new ClassedItem { itemid = 28201841, shortname = "rifle.m39" }},
            {"minerhat", new ClassedItem { itemid = -1539025626, shortname = "hat.miner" }},
            {"mp5", new ClassedItem { itemid = 1318558775, shortname = "smg.mp5" }},
            {"pipeshotgun", new ClassedItem { itemid = -1367281941, shortname = "shotgun.waterpipe" }},
            {"python", new ClassedItem { itemid = 1373971859, shortname = "pistol.python" }},
            {"roadsignvest", new ClassedItem { itemid = -2002277461, shortname = "roadsign.jacket" }},
            {"roadsignpants", new ClassedItem { itemid = 1850456855, shortname = "roadsign.kilt" }},
            {"semiautopistol", new ClassedItem { itemid = 818877484, shortname = "pistol.semiauto" }},
            {"sword", new ClassedItem { itemid = 1326180354, shortname = "salvaged.sword" }},
            {"snowjacket", new ClassedItem { itemid = -48090175, shortname = "jacket.snow" }},
            {"tshirt", new ClassedItem { itemid = 223891266, shortname = "tshirt" }},
            {"vagabondjacket", new ClassedItem { itemid = -1163532624, shortname = "jacket" }},
            {"woodendoubledoor", new ClassedItem { itemid = -1336109173, shortname = "door.double.hinged.wood" }},
            {"woodstorage", new ClassedItem { itemid = -180129657, shortname = "box.wooden" }},
            {"workboots", new ClassedItem { itemid = -1549739227, shortname = "shoes.boots" }}
        };
        private IEnumerator LoadNewSkins()
        {
            LoadSkinList();
            var itemList = Facepunch.Pool.GetList<SkinData>();
            var skinsList = new List<SkinData>();
            var skinInfo = new HashSet<int>(ItemSkinDirectory.Instance.skins.Select(x => x.id));
            var steamDefFiltered = Steamworks.SteamInventory.Definitions;

            foreach (ItemDefinition itemDefinition in ItemManager.itemList)
            {
                itemList.Clear();
                var displayName = itemDefinition.displayName.english.Replace(" ", "").ToLower();

                var categoryKey = itemDefinition.category.ToString();
                if (categoryKey == "Fun" || categoryKey == "Electrical") categoryKey = "Tool";
                if (!_workshopRename.ContainsKey(displayName)) _workshopRename.Add(displayName, new ClassedItem { itemid = itemDefinition.itemid, shortname = itemDefinition.shortname });

                foreach (Steamworks.InventoryDef item in steamDefFiltered)
                {
                    if (_skinList.BlacklistedSkins.Contains(item.Id.ToString())) continue;
                    string shortName = item.GetProperty("itemshortname");
                    string skinId = string.Empty;

                    if (string.IsNullOrEmpty(shortName) || item.Id < 100) continue;

                    if (_workshopRename.ContainsKey(shortName)) shortName = _workshopRename[shortName].shortname;
                    if (!shortName.Equals(itemDefinition.shortname)) continue;

                    if (skinInfo.Contains(item.Id)) skinId = item.Id.ToString();
                    else if (string.IsNullOrEmpty(item.GetProperty("workshopid"))) continue;
                    else skinId = item.GetProperty("workshopid");

                    if (skinId.Length < 6 || itemList.Any(x => x.SkinID == skinId)) continue;

                    itemList.Add(new SkinData { SkinID = skinId, SkinName = item.Name, ItemID = itemDefinition.itemid, ItemShortname = itemDefinition.shortname });

                    yield return null;
                }

                if (itemList.Count <= 0) continue;
                if (!_skinList.CategoryList.ContainsKey(categoryKey))
                {
                    _skinList.CategoryList.Add(categoryKey, new List<int>());
                    _skinList.CategoryList[categoryKey].Add(itemDefinition.itemid);
                }
                else if (!_skinList.CategoryList[categoryKey].Contains(itemDefinition.itemid)) _skinList.CategoryList[categoryKey].Add(itemDefinition.itemid);

                skinsList.AddRange(itemList);
                yield return CoroutineEx.waitForEndOfFrame;
            }

            Facepunch.Pool.FreeList(ref itemList);
            _skinList.WorkshopSkins = skinsList;
            foreach (var hazmat in _hazmatList)
            {
                if (!_skinList.WorkshopSkins.Any(x => x.SkinID == hazmat))
                {
                    var theHazzy = ItemManager.FindItemDefinition(int.Parse(hazmat));
                    if (theHazzy != null) _skinList.WorkshopSkins.Add(new SkinData { SkinID = hazmat, ItemID = 1266491000, ItemShortname = theHazzy.shortname, SkinName = theHazzy.displayName.english });
                }
            }
            Puts("\nUpdated skins list ({0} skins)\nImported {1} imported skins\nImported {2} blacklisted skins", skinsList.Count, _skinList.AddedSkins.Count, _skinList.BlacklistedSkins.Count);
        }

        private void SaveSkins()
        {
            Interface.GetMod().DataFileSystem.WriteObject($"SkinController{Path.DirectorySeparatorChar}workshopskins", _skinList.WorkshopSkins);
            Interface.GetMod().DataFileSystem.WriteObject($"SkinController{Path.DirectorySeparatorChar}importedskins", _skinList.AddedSkins);
            Interface.GetMod().DataFileSystem.WriteObject($"SkinController{Path.DirectorySeparatorChar}blacklistedskins", _skinList.BlacklistedSkins);
        }

        private void LoadSkinList()
        {
            _skinList.WorkshopSkins = Interface.GetMod().DataFileSystem.ReadObject<List<SkinData>>($"SkinController{Path.DirectorySeparatorChar}workshopskins");
            _skinList.AddedSkins = Interface.GetMod().DataFileSystem.ReadObject<List<SkinData>>($"SkinController{Path.DirectorySeparatorChar}importedskins");
            _skinList.BlacklistedSkins = Interface.GetMod().DataFileSystem.ReadObject<List<string>>($"SkinController{Path.DirectorySeparatorChar}blacklistedskins");
        }
        #endregion

        #region UICreators
        private static string CreateItemPanel(ref CuiElementContainer container, string anchorMin, string anchorMax, float padding, string color, int itemId, string parent = "Overlay",
        string panelName = null, ulong skinId = 0L)
        {
            var panel = container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = anchorMin,
                    AnchorMax = anchorMax
                },
                Image = { Color = color }
            }, parent, panelName);

            container.Add(new CuiElement
            {
                Parent = panel,
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = $"{padding} {padding + .004f}",
                        AnchorMax = $"{1 - padding - .004f} {1 - padding - .02f}"
                    },
                    new CuiImageComponent {ItemId = itemId, SkinId = skinId}
                }
            });

            return panel;
        }

        private static string CreateLabel(ref CuiElementContainer container, string anchorMin, string anchorMax, string backgroundColor, string textColor,
            string labelText, int fontSize, TextAnchor alignment, string parent = "Overlay",
            string labelName = null)
        {
            var panel = container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = anchorMin, AnchorMax = anchorMax                },
                Image = { Color = backgroundColor }
            }, parent, labelName);
            container.Add(new CuiLabel
            {
                Text =
                {
                    Color = textColor,
                    Text = labelText,
                    Align = alignment,
                    FontSize = fontSize,
                    Font = "robotocondensed-bold.ttf"
                }
            }, panel);
            return panel;
        }

        private static string CreatePanel(ref CuiElementContainer container, string anchorMin, string anchorMax, string panelColor, string parent = "Overlay",
            string panelName = null, bool blur = false)
        {
            if (blur)
                return container.Add(new CuiPanel
                {
                    RectTransform =
            {
                AnchorMin = anchorMin,
                AnchorMax = anchorMax
            },
                    Image = { Color = panelColor, Material = "assets/content/ui/uibackgroundblur.mat" }
                }, parent, panelName);
            else
                return container.Add(new CuiPanel
                {
                    RectTransform =
            {
                AnchorMin = anchorMin,
                AnchorMax = anchorMax
            },
                    Image = { Color = panelColor }
                }, parent, panelName);
        }

        private static void CreateImagePanel(ref CuiElementContainer container, string anchorMin, string anchorMax, string panelImage, string parent = "Overlay",
        string panelName = null)
        {
            container.Add(new CuiElement
            {
                Parent = parent,
                Name = panelName,
                Components =
                {
                    new CuiRectTransformComponent
                    {
                        AnchorMin = anchorMin,
                        AnchorMax = anchorMax

                    },
                    new CuiRawImageComponent {Url = panelImage},
                }
            });
        }

        private static string CreateButton(ref CuiElementContainer container, string anchorMin, string anchorMax, string buttonColor, string textColor, string buttonText,
        int fontSize, string buttonCommand, string parent = "Overlay",
        TextAnchor labelAnchor = TextAnchor.MiddleCenter)
        {
            var panel = container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = anchorMin,
                    AnchorMax = anchorMax
                },
                Image = { Color = "0 0 0 0" }
            }, parent);

            container.Add(new CuiButton
            {
                Button = { Color = buttonColor, Command = $"{buttonCommand}" },
                Text = { Align = labelAnchor, Color = textColor, FontSize = fontSize, Text = buttonText }
            }, panel);
            return panel;
        }

        private static string CreateInput(ref CuiElementContainer container, string anchorMin, string anchorMax, string command, string backgroundColor, string textColor,
        string labelText, int fontSize, TextAnchor alignment, string parent = "Overlay",
        string labelName = null)
        {
            var panel = container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = anchorMin, AnchorMax = anchorMax
                },
                Image = { Color = backgroundColor }
            }, parent, labelName);

            container.Add(new CuiElement
            {
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Color = textColor,
                        Text = labelText,
                        Align = alignment,
                        FontSize = fontSize,
                        Font = "robotocondensed-bold.ttf",
                        NeedsKeyboard = true,
                        Command = command
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 1"
                    }
                },
                Parent = panel
            });

            return panel;
        }
        #endregion
    }
}