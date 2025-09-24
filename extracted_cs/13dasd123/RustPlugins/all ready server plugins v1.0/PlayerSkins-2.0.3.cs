using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Globalization;

namespace Oxide.Plugins
{
    [Info("PlayerSkins", "k1lly0u", "2.0.3", ResourceId = 0)]
      //  Слив плагинов server-rust by Apolo YouGame
    class PlayerSkins : RustPlugin
    {
        #region Fields
        [PluginReference] Plugin Economics, ImageLibrary, ServerRewards;

        private Dictionary<string, Dictionary<ulong, SkinData>> skinData;
        private Dictionary<ulong, UserData> userData;

        private DynamicConfigFile userdata, skindata;

        private Dictionary<string, Dictionary<ulong, WorkshopItem>> workshopItems = new Dictionary<string, Dictionary<ulong, WorkshopItem>>();

        private Dictionary<string, string> shortnameToDisplayname = new Dictionary<string, string>();

        private List<ulong> adminToggle = new List<ulong>();
        private List<ulong> ownedToggle = new List<ulong>();

        private bool initialized = false;

        private Facepunch.Steamworks.Workshop.Query query = null;
        private bool workshopInitialized = false;

        private static Dictionary<Colors, string> uiColor;

        private TokenType purchaseType;
        private DisplayMode forcedMode;

        public enum TokenType { None, ServerRewards, Economics }
        #endregion

        #region Oxide Hooks
        private void Loaded()
        {
            skindata = Interface.Oxide.DataFileSystem.GetFile("PlayerSkins/skinlist");
            userdata = Interface.Oxide.DataFileSystem.GetFile("PlayerSkins/userdata");

            permission.RegisterPermission("playerskins.shop", this);
            permission.RegisterPermission("playerskins.reskin", this);
            permission.RegisterPermission("playerskins.admin", this);

            lang.RegisterMessages(Messages, this);
        }

        private void OnServerInitialized()
        {
            LoadData();

            purchaseType = ParseType<TokenType>(configData.Purchase.Type);
            if (configData.Purchase.Enabled && purchaseType == TokenType.None)
            {
                PrintError("Invalid purchase plugin specified in config. Must be either 'ServerRewards' or 'Economics'!");
                return;
            }

            forcedMode = ParseType<DisplayMode>(configData.Shop.ForcedMode);
            
            foreach (string perm in configData.Shop.Permissions)
            {
                if (!perm.StartsWith("playerskins."))
                    permission.RegisterPermission($"playerskins.{perm}", this);
                else permission.RegisterPermission(perm, this);
            }

            uiColor = new Dictionary<Colors, string>();
            foreach (var color in configData.UI.Colors)
                uiColor.Add(color.Key, UI.Color(color.Value.Color, color.Value.Alpha));

            foreach (ItemDefinition item in ItemManager.itemList)
            {
                shortnameToDisplayname.Add(item.shortname, item.displayName.english);

                string workshopName = item.displayName.english.ToLower().Replace("skin", "").Replace(" ", "").Replace("-", "");
                if (!workshopNameToShortname.ContainsKey(workshopName))
                    workshopNameToShortname.Add(workshopName, item.shortname);
            }

            GetItemSkins();
        }

        private void OnServerSave() => SaveUserData();

        private void Unload()
        {
            if (query != null)
            {
                ServerMgr.Instance.StopCoroutine(GetWorkshopSkins());
                query.Dispose();
            }

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                DestroyUI(player);
        }

        private void OnUseNPC(BasePlayer npc, BasePlayer player)
        {
            if (player == null || npc == null) return;

            if (!userData.ContainsKey(player.userID))
                userData.Add(player.userID, new UserData());

            if (configData.Shop.NPCs.Contains(npc.UserIDString))
                OpenSkinMenu(player, "", 0, 0);
            else if (configData.Reskin.NPCs.Contains(npc.UserIDString) && !configData.Shop.GiveItemOnPurchase)
                CreateReskinMenu(player);
        }

        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            BasePlayer player = task?.owner;
            if (player == null || item == null)
                return;

            if (item.skin != 0)
                return;

            UserData data;
            if (!userData.TryGetValue(player.userID, out data))
                return;

            if (data.defaultSkins.ContainsKey(item.info.shortname))            
                ChangeItemSkin(item, data.defaultSkins[item.info.shortname]);            
        }
        #endregion

        #region Functions
        private T ParseType<T>(string type)
        {
            try
            {
                return (T)Enum.Parse(typeof(T), type, true);
            }
            catch
            {
                return default(T);
            }
        }

        private bool HasPermission(BasePlayer player, string perm)
        {
            if (!perm.StartsWith("playerskins."))
                perm = $"playerskins.{perm}";

            return permission.UserHasPermission(player.UserIDString, perm);
        }

        private void BroadcastAnnouncement()
        {
            if (!configData.Announcements.Enabled && configData.Announcements.Interval > 0)
                return;

            foreach(BasePlayer player in BasePlayer.activePlayerList)
            {
                if (configData.Shop.DisableCommand)
                    SendReply(player, msg("Help.Shop.NPC", player.userID));
                else SendReply(player, msg("Help.Shop.Command", player.userID));
                if (configData.Reskin.DisableCommand)
                    SendReply(player, msg("Help.Reskin.NPC", player.userID));
                else SendReply(player, msg("Help.Reskin.Command", player.userID));
            }

            timer.In(configData.Announcements.Interval * 60, BroadcastAnnouncement);
        }

        private void ChangeItemSkin(BasePlayer player, ulong targetSkin)
        {
            Item item = player.GetActiveItem();
            if (item == null)
                return;

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
        #endregion

        #region Skin List Initialization
        private Dictionary<string, string> workshopNameToShortname = new Dictionary<string, string>
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
            {"burlapgloves", "burlap.gloves" },
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
            {"lr300.item", "rifle.lr300" }
        };
       
        private void GetItemSkins()
        {
            PrintWarning("Retrieving approved skin lists...");
            webrequest.Enqueue("http://s3.amazonaws.com/s3.playrust.com/icons/inventory/rust/schema.json", null, (code, response) =>
            {
                if (response != null && code == 200)
                {
                    Rust.Workshop.ItemSchema.Item[] items = JsonConvert.DeserializeObject<Rust.Workshop.ItemSchema>(response).items;

                    Dictionary<string, Dictionary<ulong, string>> loadOrder = new Dictionary<string, Dictionary<ulong, string>>();
                    int loadOrderCount = 0;
                    int skinCount = 0;

                    foreach (var item in items)
                    {
                        if (item == null || string.IsNullOrEmpty(item.itemshortname) || string.IsNullOrEmpty(item.icon_url))
                            continue;

                        string shortname = item.itemshortname;
                        if (workshopNameToShortname.ContainsKey(shortname))
                            shortname = workshopNameToShortname[shortname];
                       
                        if (!ItemManager.itemList.Find(x => x.shortname == shortname)?.HasSkins ?? false)                        
                            continue;                        

                        ulong skinId = 0;

                        if (item.itemdefid < 20000)
                            skinId = item.itemdefid;
                        else
                        {
                            if (!string.IsNullOrEmpty(item.workshopid))
                                skinId = ulong.Parse(item.workshopid);
                            else skinId = item.itemdefid;
                        }

                        if (!skinData.ContainsKey(shortname))
                            skinData.Add(shortname, new Dictionary<ulong, SkinData>());

                        if (!skinData[shortname].ContainsKey(skinId))
                            skinData[shortname].Add(skinId, new SkinData());

                        if (!workshopItems.ContainsKey(shortname))
                            workshopItems.Add(shortname, new Dictionary<ulong, WorkshopItem>());

                        if (!workshopItems[shortname].ContainsKey(skinId))
                            workshopItems[shortname].Add(skinId, new WorkshopItem(item));

                        if (!HasImage(shortname, skinId))
                        {
                            if (!loadOrder.ContainsKey(shortname))
                                loadOrder.Add(shortname, new Dictionary<ulong, string>());

                            if (!loadOrder[shortname].ContainsKey(skinId))
                            {
                                loadOrder[shortname].Add(skinId, item.icon_url);
                                skinCount++;
                            }

                            if (skinCount >= 500)
                            {
                                loadOrderCount++;
                                Puts($"Creating a skin load order (Order #{loadOrderCount})");
                                ImageLibrary.Call("ImportItemList", $"{Title} approved skin icons (Order #{loadOrderCount})", loadOrder);

                                loadOrder = new Dictionary<string, Dictionary<ulong, string>>();
                                skinCount = 0;
                            }
                        }
                    }

                    if (skinCount > 0)
                    {
                        loadOrderCount++;
                        Puts($"Creating a skin load order (Order #{loadOrderCount})");
                        ImageLibrary.Call("ImportItemList", $"{Title} approved skin icons (Order #{loadOrderCount})", loadOrder);
                    }
                    PrintWarning("Successfully retrieved the approved skin list");
                }
                else PrintWarning($"There was an error retrieving the approved skin list. Code : {code}");

                initialized = true;

                if (configData.Workshop.Enabled)
                    ServerMgr.Instance.StartCoroutine(GetWorkshopSkins());
                else ClearEmptyLists();
            }, this);
        }

        private IEnumerator GetWorkshopSkins()
        {
            PrintWarning("Querying Steam for workshop items. This process can take up to 15 minutes!\nWorkshop skins will be unavailable in the skin shop and reskin menu until this process has completed. Once the workshop data has been loaded any registered workshop skins will appear.");

            query = Rust.Global.SteamServer.Workshop.CreateQuery();
            query.Page = 1;
            query.PerPage = 50000;
            query.RequireTags.Add("version3");
            query.RequireTags.Add("skin");
            query.RequireAllTags = true;
            query.Run();

            yield return new WaitWhile(new Func<bool>(() => query.IsRunning));

            if (ImageLibrary == null)
                yield break;

            Dictionary<string, Dictionary<ulong, string>> loadOrder = new Dictionary<string, Dictionary<ulong, string>>();
            int loadOrderCount = 0;
            int skinCount = 0;

            int count = 0;
            foreach (var item in query.Items)
            {
                if (ContainsKeyword(item.Title))
                    continue;

                if (!string.IsNullOrEmpty(item.PreviewImageUrl))
                {
                    foreach (string tag in item.Tags)
                    {
                        string adjTag = tag.ToLower().Replace("skin", "").Replace(" ", "").Replace("-", "").Replace(".item", "");
                        if (workshopNameToShortname.ContainsKey(adjTag))
                        {
                            string identifier = workshopNameToShortname[adjTag];

                            if (!workshopItems.ContainsKey(identifier))
                                workshopItems.Add(identifier, new Dictionary<ulong, WorkshopItem>());

                            if (!workshopItems[identifier].ContainsKey(item.Id))
                            {
                                workshopItems[identifier].Add(item.Id, new WorkshopItem(item));
                                count++;
                            }

                            if (skinData.ContainsKey(identifier))
                            {
                                if (skinData[identifier].ContainsKey(item.Id))
                                {
                                    if (!HasImage(identifier, item.Id))
                                    {
                                        if (!loadOrder.ContainsKey(identifier))
                                            loadOrder.Add(identifier, new Dictionary<ulong, string>());

                                        if (!loadOrder[identifier].ContainsKey(item.Id))
                                        {
                                            loadOrder[identifier].Add(item.Id, item.PreviewImageUrl);
                                            skinCount++;
                                        }

                                        if (skinCount >= 500)
                                        {
                                            loadOrderCount++;
                                            Puts($"Creating a skin load order (Order #{loadOrderCount})");
                                            ImageLibrary.Call("ImportItemList", $"{Title} workshop skin icons (Order #{loadOrderCount})", loadOrder);

                                            loadOrder = new Dictionary<string, Dictionary<ulong, string>>();
                                            skinCount = 0;
                                        }
                                    }
                                }
                            }
                        }
                    }                    
                }
            }

            if (skinCount > 0)
            {
                loadOrderCount++;
                Puts($"Creating a skin load order (Order #{loadOrderCount})");
                ImageLibrary.Call("ImportItemList", $"{Title} workshop skin icons (Order #{loadOrderCount})", loadOrder);
            }

            query.Dispose();
            query = null;

            PrintWarning($"Workshop skin list retrieval completed. {count} workshop skins are now available!");
            ClearEmptyLists();
            workshopInitialized = true;
        }

        private void ClearEmptyLists()
        {
            for (int i = skinData.Keys.Count - 1; i >= 0; i--)
            {
                var skinInfo = skinData.ElementAt(i);
      //  Слив плагинов server-rust by Apolo YouGame
                if (skinInfo.Value.Count < 2)
      //  Слив плагинов server-rust by Apolo YouGame
                    skinData.Remove(skinInfo.Key);
      //  Слив плагинов server-rust by Apolo YouGame
            }

            RemoveInvalidSkins();
        }

        private void RemoveInvalidSkins()
        {
            for (int i = skinData.Count - 1; i >= 0; i--)
            {
                var skinInfo = skinData.ElementAt(i);
      //  Слив плагинов server-rust by Apolo YouGame
                for (int y = skinInfo.Value.Count - 1; y >= 0; y--)
      //  Слив плагинов server-rust by Apolo YouGame
                {
                    var skin = skinInfo.Value.ElementAt(y);
      //  Слив плагинов server-rust by Apolo YouGame
                    if (!workshopItems.ContainsKey(skinInfo.Key) || !workshopItems[skinInfo.Key].ContainsKey(skin.Key))
      //  Слив плагинов server-rust by Apolo YouGame
                        skinData[skinInfo.Key].Remove(skin.Key);
      //  Слив плагинов server-rust by Apolo YouGame
                }
            }

            SaveSkinData();
        }

        private bool ContainsKeyword(string title)
        {
            foreach (string keyword in configData.Workshop.Filter)
            {
                if (title.ToLower().Contains(keyword.ToLower()))
                    return true;
            }
            return false;
        }
        #endregion

        #region Image Library       
        private string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary.Call("GetImage", shortname, skin);

        private bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary.Call("AddImage", url, shortname, skin);

        private bool HasImage(string shortname, ulong skin = 0) => (bool)ImageLibrary.Call("HasImage", shortname, skin);

        private bool IsReady() => (bool)ImageLibrary.Call("IsReady");
        #endregion

        #region Chat Commands
        [ChatCommand("skin")]
        private void cmdSkin(BasePlayer player, string command, string[] args)
        {
            if (!userData.ContainsKey(player.userID))
                userData.Add(player.userID, new UserData());

            if (args.Length == 0)
            {
                if (!permission.UserHasPermission(player.UserIDString, "playerskins.reskin"))
                {
                    SendReply(player, "You do not have permission to use this command");
                    return;
                }

                if (configData.Shop.GiveItemOnPurchase)
                    return;

                if (configData.Reskin.DisableCommand)
                {
                    SendReply(player, "You can only access the re-skin menu via a re-skin NPC");
                    return;
                }
                CreateReskinMenu(player);
            }
            else
            {
                if (args[0].ToLower() != "shop")
                {
                    SendReply(player, "/skin - Open the re-skin menu");
                    SendReply(player, "/skin shop - Open the skin shop");
                    return;
                }

                if (!permission.UserHasPermission(player.UserIDString, "playerskins.shop"))
                {
                    SendReply(player, "You do not have permission to use this command");
                    return;
                }

                if (configData.Shop.DisableCommand)
                {
                    SendReply(player, "You can only access the skin shop menu via a skin shop NPC");
                    return;
                }                

                OpenSkinMenu(player, string.Empty, 0, 0);
            }
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

            if (arg.Args == null || arg.Args.Length < 3)
            {
                SendReply(arg, "playerskins.skins import <item shortname> <skin ID> - Import the specified workshop skin");
                SendReply(arg, "playerskins.skins import random <amount> - Import the specified amount of random skins for random items");
                SendReply(arg, "playerskins.skins import random <item shortname> <amount> - Import the specified amount of random skins for the specified item");
                SendReply(arg, "playerskins.skins remove <item shortname> <skin ID> - Remove the specified skin from the skin shop");
                return;
            }

            if (!configData.Workshop.Enabled)
            {
                SendReply(arg, "You have workshop disabled in your config. The playerskins.skins commands are unavailable when workshop is disabled");
                return;
            }

            if (!workshopInitialized)
            {
                SendReply(arg, "Workshop initialization has not yet finished. The playerskins.skins commands are unavailable until that process is complete");
                return;
            }

            switch (arg.GetString(0).ToLower())
            {
                case "import":
                    if (arg.Args[1].ToLower() == "random")
                    {
                        if (arg.Args.Length == 4)
                        {
                            string shortname = arg.GetString(2).ToLower();
                            if (!ItemManager.itemList.Select(x => x.shortname).Contains(shortname) || !workshopItems.ContainsKey(shortname))
                            {
                                SendReply(arg, "The item shortname you have entered either does not have any skins, or is invalid");
                                return;
                            }

                            int amount;
                            if (!int.TryParse(arg.GetString(3), out amount))
                            {
                                SendReply(arg, "You must enter an amount of random skins to import");
                                return;
                            }

                            Dictionary<string, Dictionary<ulong, string>> loadOrder = new Dictionary<string, Dictionary<ulong, string>>();
                            int skinCount = 0;
                            int loadOrderCount = 0;

                            List<KeyValuePair<ulong, WorkshopItem>> newList = new List<KeyValuePair<ulong, WorkshopItem>>(workshopItems[shortname].ToList());

                            for (int i = 0; i < amount; i++)
                            {
                                var skinInfo = newList.GetRandom();
      //  Слив плагинов server-rust by Apolo YouGame
                                newList.Remove(skinInfo);
      //  Слив плагинов server-rust by Apolo YouGame

                                if ((skinData.ContainsKey(shortname) && skinData[shortname].ContainsKey(skinInfo.Key)) || string.IsNullOrEmpty(skinInfo.Value.imageUrl))
      //  Слив плагинов server-rust by Apolo YouGame
                                {
                                    i--;
                                    continue;
                                }

                                if (!skinData.ContainsKey(shortname))
                                    skinData.Add(shortname, new Dictionary<ulong, SkinData>());

                                skinData[shortname].Add(skinInfo.Key, new SkinData());
      //  Слив плагинов server-rust by Apolo YouGame

                                if (!HasImage(shortname, skinInfo.Key))
      //  Слив плагинов server-rust by Apolo YouGame
                                {
                                    if (!loadOrder.ContainsKey(shortname))
                                        loadOrder.Add(shortname, new Dictionary<ulong, string>());

                                    if (!loadOrder[shortname].ContainsKey(skinInfo.Key))
      //  Слив плагинов server-rust by Apolo YouGame
                                    {
                                        loadOrder[shortname].Add(skinInfo.Key, skinInfo.Value.imageUrl);
      //  Слив плагинов server-rust by Apolo YouGame
                                        skinCount++;
                                    }
                                }

                                if (skinCount >= 500)
                                {
                                    loadOrderCount++;
                                    Puts($"Creating a skin load order (Order #{loadOrderCount})");
                                    ImageLibrary.Call("ImportItemList", $"{Title} skin icons (Order #{loadOrderCount})", loadOrder);

                                    loadOrder = new Dictionary<string, Dictionary<ulong, string>>();
                                    skinCount = 0;
                                }
                            }

                            if (skinCount > 0)
                            {
                                loadOrderCount++;
                                Puts($"Creating a skin load order (Order #{loadOrderCount})");
                                ImageLibrary.Call("ImportItemList", $"{Title} skin icons (Order #{loadOrderCount})", loadOrder);
                            }

                            SendReply(arg, $"Importing {amount} random {shortname} workshop skins to ImageLibrary");
                            SaveSkinData();
                        }
                        else
                        {
                            int amount;
                            if (!int.TryParse(arg.GetString(2), out amount))
                            {
                                SendReply(arg, "You must enter an amount of random skins to import");
                                return;
                            }

                            Dictionary<string, Dictionary<ulong, string>> loadOrder = new Dictionary<string, Dictionary<ulong, string>>();
                            int skinCount = 0;
                            int loadOrderCount = 0;

                            Dictionary<string, Dictionary<ulong, WorkshopItem>> newList = new Dictionary<string, Dictionary<ulong, WorkshopItem>>(workshopItems);

                            for (int i = 0; i < amount; i++)
                            {
                                string shortname = newList.Keys.ToArray().GetRandom();
                                var skinInfo = newList[shortname].ToArray().GetRandom();
      //  Слив плагинов server-rust by Apolo YouGame

                                if ((skinData.ContainsKey(shortname) && skinData[shortname].ContainsKey(skinInfo.Key)) || string.IsNullOrEmpty(skinInfo.Value.imageUrl))
      //  Слив плагинов server-rust by Apolo YouGame
                                {
                                    i--;
                                    continue;
                                }

                                if (!skinData.ContainsKey(shortname))
                                    skinData.Add(shortname, new Dictionary<ulong, SkinData>());

                                skinData[shortname].Add(skinInfo.Key, new SkinData());
      //  Слив плагинов server-rust by Apolo YouGame
                                
                                if (!HasImage(shortname, skinInfo.Key))
      //  Слив плагинов server-rust by Apolo YouGame
                                {
                                    if (!loadOrder.ContainsKey(shortname))
                                        loadOrder.Add(shortname, new Dictionary<ulong, string>());

                                    if (!loadOrder[shortname].ContainsKey(skinInfo.Key))
      //  Слив плагинов server-rust by Apolo YouGame
                                    {
                                        loadOrder[shortname].Add(skinInfo.Key, skinInfo.Value.imageUrl);
      //  Слив плагинов server-rust by Apolo YouGame
                                        skinCount++;
                                    }
                                }

                                if (skinCount >= 500)
                                {
                                    loadOrderCount++;
                                    Puts($"Creating a skin load order (Order #{loadOrderCount})");
                                    ImageLibrary.Call("ImportItemList", $"{Title} skin icons (Order #{loadOrderCount})", loadOrder);

                                    loadOrder = new Dictionary<string, Dictionary<ulong, string>>();
                                    skinCount = 0;
                                }
                            }

                            if (skinCount > 0)
                            {
                                loadOrderCount++;
                                Puts($"Creating a skin load order (Order #{loadOrderCount})");
                                ImageLibrary.Call("ImportItemList", $"{Title} skin icons (Order #{loadOrderCount})", loadOrder);
                            }

                            SendReply(arg, $"Importing {amount} random workshop skins to ImageLibrary");
                            SaveSkinData();
                        }
                    }
                    else
                    {
                        string shortname = arg.GetString(1).ToLower();
                        if (!ItemManager.itemList.Select(x => x.shortname).Contains(shortname) || !workshopItems.ContainsKey(shortname))
                        {
                            SendReply(arg, "The item shortname you have entered either does not have any skins, or is invalid");
                            return;
                        }

                        ulong skinId = 0U;
                        if (!ulong.TryParse(arg.GetString(2), out skinId) || !workshopItems[shortname].ContainsKey(skinId))
                        {
                            SendReply(arg, "The skin ID you have entered is either invalid or there is no skin with that ID");
                            return;
                        }

                        WorkshopItem workshopItem = workshopItems[shortname][skinId];
                        if (string.IsNullOrEmpty(workshopItem.imageUrl))
                        {
                            SendReply(arg, "The specified workshop skin can not be imported");
                            return;
                        }

                        if (!skinData.ContainsKey(shortname))
                            skinData.Add(shortname, new Dictionary<ulong, SkinData>());

                        if (!skinData[shortname].ContainsKey(skinId))
                            skinData[shortname].Add(skinId, new SkinData());

                        if (!HasImage(shortname, skinId))
                        {
                            Dictionary<string, Dictionary<ulong, string>> loadOrder = new Dictionary<string, Dictionary<ulong, string>>
                            {
                                [shortname] = new Dictionary<ulong, string>
                                {
                                    [skinId] = workshopItem.imageUrl
                                }
                            };
                            ImageLibrary.Call("ImportItemList", $"{Title} - {shortname} {skinId}", loadOrder);
                        }

                        SendReply(arg, $"Importing skin ID {skinId} for {shortname} to ImageLibrary!");
                        SaveSkinData();
                    }
                    return;
                case "remove":
                    {
                        string shortname = arg.GetString(1).ToLower();
                        if (!ItemManager.itemList.Select(x => x.shortname).Contains(shortname) || !skinData.ContainsKey(shortname))
                        {
                            SendReply(arg, "The item shortname you have entered either does not have any available skins, or is invalid");
                            return;
                        }

                        ulong skinId = 0U;
                        if (!ulong.TryParse(arg.GetString(2), out skinId) || !skinData[shortname].ContainsKey(skinId))
                        {
                            SendReply(arg, "The skin ID you have entered is either invalid or has not been added to the list of available skins");
                            return;
                        }

                        skinData[shortname][skinId].isDisabled = true;
                        ImageLibrary.Call("RemoveImage", shortname, skinId);

                        SendReply(arg, $"You have removed skin ID {skinId} for {shortname}!");
                        SaveSkinData();
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
                foreach (var item in skinData.Values)
                {
                    foreach (var skin in item.Values)
                        skin.cost = amount;
                }

                SendReply(arg, $"You have set all skin costs to {amount}");
            }
            else
            {
                Dictionary<ulong, SkinData> data;
                if (!skinData.TryGetValue(shortname, out data))
                {
                    SendReply(arg, $"Either an invalid shortname was entered, or there are no skins for the specified item : {shortname}");
                    return;
                }

                foreach (var skin in data.Values)
                    skin.cost = amount;

                SendReply(arg, $"You have set all {shortname} skin costs to {amount}");
            }

            SaveSkinData();
        }        
        #endregion

        #region UI Helper
        const string MainPanel = "PSMainPanel";
        const string SelectPanel = "PSSelectionPanel";
        const string PopupPanel = "PSPopupPanel";
        const string ReskinPanel = "PSReskinPanel";

        private void DestroyUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, MainPanel);
            CuiHelper.DestroyUi(player, SelectPanel);
            CuiHelper.DestroyUi(player, PopupPanel);
            CuiHelper.DestroyUi(player, ReskinPanel);
        }

        public static class UI
        {
            static public CuiElementContainer Container(string panel, string color, UI4 dimensions, bool useCursor = false, string parent = "Overlay")
            {
                CuiElementContainer container = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax()},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panel
                    }
                };
                return container;
            }
            static public void Panel(ref CuiElementContainer container, string panel, string color, UI4 dimensions, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    CursorEnabled = cursor
                },
                panel);
            }
            static public void Label(ref CuiElementContainer container, string panel, string text, int size, UI4 dimensions, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                },
                panel);

            }
            static public void Button(ref CuiElementContainer container, string panel, string color, string text, int size, UI4 dimensions, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 0f },
                    RectTransform = { AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }
            static public void Image(ref CuiElementContainer container, string panel, string png, UI4 dimensions)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent {Png = png },
                        new CuiRectTransformComponent {AnchorMin = dimensions.GetMin(), AnchorMax = dimensions.GetMax() }
                    }
                });
            }
            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                    hexColor = hexColor.Substring(1);
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }
        public class UI4
        {
            public float xMin, yMin, xMax, yMax;
            public UI4(float xMin, float yMin, float xMax, float yMax)
            {
                this.xMin = xMin;
                this.yMin = yMin;
                this.xMax = xMax;
                this.yMax = yMax;
            }
            public string GetMin() => $"{xMin} {yMin}";
            public string GetMax() => $"{xMax} {yMax}";
        }
        #endregion

        #region UI Creation
        private void OpenSkinMenu(BasePlayer player, string shortname = "", int column = 0, int page = 0)
        {
            DisplayMode displayMode;
            if (forcedMode == DisplayMode.None)
            {
                UserData data;
                if (userData.TryGetValue(player.userID, out data))
                    displayMode = data.displayMode;
                else displayMode = DisplayMode.Full;
            }
            else displayMode = forcedMode;

            if (displayMode == DisplayMode.Full)
                CreateSelectionMenu(player, shortname, column, page);
            else CreateSmallSelectionMenu(player, shortname, column, page);
        }
        private void CreateSelectionMenu(BasePlayer player, string shortname = "", int column = 0, int page = 0)
        {
            CuiElementContainer container = UI.Container(MainPanel, uiColor[Colors.Background], new UI4(0, 0, 1, 1), true);

            bool adminMode = adminToggle.Contains(player.userID);
            bool ownedMode = ownedToggle.Contains(player.userID);
            UserData data = userData[player.userID];

            int indexCount = 0;
            int columnIndex = column * 20;

            if (columnIndex > 0)
                UI.Button(ref container, MainPanel, uiColor[Colors.Button], msg("UI.Button.Up", player.userID), 12, new UI4(0.02f, 0.92f, 0.14f, 0.95f),
                    $"psui.changepage {(string.IsNullOrEmpty(shortname) ? "empty" : shortname)} {column - 1} {page}");
            if (columnIndex + 20 < skinData.Keys.Count - 1)
                UI.Button(ref container, MainPanel, uiColor[Colors.Button], msg("UI.Button.Down", player.userID), 12, new UI4(0.02f, 0.08f, 0.14f, 0.11f),
                    $"psui.changepage {(string.IsNullOrEmpty(shortname) ? "empty" : shortname)} {column + 1} {page}");

            string[] skinKeys = skinData.Keys.OrderBy(x => shortnameToDisplayname[x]).ToArray();

            for (int i = columnIndex; i < columnIndex + 20; i++)
            {
                if (i > skinKeys.Length - 1)
                    break;

                string itemShortname = skinKeys.ElementAt(i);
                string displayName = shortnameToDisplayname[itemShortname];
                float[] position = GetItemPosition(indexCount);

                UI.Button(ref container, MainPanel, shortname == itemShortname ? uiColor[Colors.ButtonSelected] : uiColor[Colors.Button], displayName, 12, new UI4(position[0], position[1], position[2], position[3]), shortname == itemShortname ? "" : $"psui.changepage {itemShortname} {column} 0");
                indexCount++;
            }

            if (!string.IsNullOrEmpty(shortname))
            {
                int itemCount = 0;
                int itemIndex = 48 * page;

                ulong[] skins = ownedMode ? (data.purchasedSkins.ContainsKey(shortname) ? data.purchasedSkins[shortname].Where(x =>
                    workshopItems.ContainsKey(shortname) &&
                    workshopItems[shortname].ContainsKey(x) &&
                    skinData.ContainsKey(shortname) &&
                    skinData[shortname].ContainsKey(x) &&
                    !skinData[shortname][x].isDisabled &&
                    HasImage(shortname, x)).ToArray() : new ulong[0]) :
                    skinData[shortname].Where(x =>
                    workshopItems.ContainsKey(shortname) &&
                    workshopItems[shortname].ContainsKey(x.Key) &&
                    !x.Value.isDisabled &&
                    HasImage(shortname, x.Key)).Select(x => x.Key).ToArray();

                for (int i = itemIndex; i < itemIndex + 48; i++)
                {
                    if (i > skins.Length - 1)
                        break;

                    ulong skinId = skins[i];
                    float[] position = GetButtonPosition(itemCount, 8);

                    UI.Image(ref container, MainPanel, GetImage(shortname, skinId), new UI4(position[0] + 0.01f, position[1], position[2] - 0.01f, position[3] - 0.01f));
                    UI.Button(ref container, MainPanel, "0 0 0 0", "", 0, new UI4(position[0], position[1], position[2] + 0.1f, position[3] - 0.01f), $"psui.selectitem {shortname} {skinId}");

                    itemCount++;
                }

                if (itemIndex > 0)
                    UI.Button(ref container, MainPanel, uiColor[Colors.Button], msg("UI.Button.Previous", player.userID), 12, new UI4(0.17f, 0.04f, 0.27f, 0.07f), $"psui.changepage {(string.IsNullOrEmpty(shortname) ? "empty" : shortname)} {column} {page - 1}");
                if (itemIndex + 48 < skinData[shortname].Where(x => !x.Value.isDisabled).Count())
                    UI.Button(ref container, MainPanel, uiColor[Colors.Button], msg("UI.Button.Next", player.userID), 12, new UI4(0.87f, 0.04f, 0.97f, 0.07f), $"psui.changepage {(string.IsNullOrEmpty(shortname) ? "empty" : shortname)} {column} {page + 1}");
            }
            else UI.Label(ref container, MainPanel, FormatHelpText(DisplayMode.Full, player.userID), 12, new UI4(0.17f, 0.1f, 0.97f, 0.92f), TextAnchor.UpperLeft);

            UI.Button(ref container, MainPanel, uiColor[Colors.ButtonSelected], msg("UI.Exit", player.userID), 12, new UI4(0.02f, 0.04f, 0.14f, 0.07f), "psui.exit");

            UI.Button(ref container, MainPanel, ownedMode ? uiColor[Colors.ButtonSelected] : uiColor[Colors.Button], msg("UI.ShowOwned", player.userID), 12, new UI4(0.76f, 0.04f, 0.86f, 0.07f),
                $"psui.toggle owned {(string.IsNullOrEmpty(shortname) ? "empty" : shortname)} {column} 0");

            if (forcedMode == DisplayMode.None)            
                UI.Button(ref container, MainPanel, uiColor[Colors.Button], msg("UI.Minimised", player.userID), 12, new UI4(0.65f, 0.04f, 0.75f, 0.07f),
                   $"psui.toggle size {(string.IsNullOrEmpty(shortname) ? "empty" : shortname)} {column} {page}");

            if (permission.UserHasPermission(player.UserIDString, "playerskins.admin"))            
                UI.Button(ref container, MainPanel, adminMode ? uiColor[Colors.ButtonSelected] : uiColor[Colors.Button], msg("UI.AdminMode", player.userID), 12, new UI4(0.28f, 0.04f, 0.38f, 0.07f),
                   $"psui.toggle admin {(string.IsNullOrEmpty(shortname) ? "empty" : shortname)} {column} 0");

            if (configData.Purchase.Enabled)
            {
                UI.Panel(ref container, MainPanel, uiColor[Colors.Panel], new UI4(0.39f, 0.04f, 0.64f, 0.07f));
                UI.Label(ref container, MainPanel, string.Format((purchaseType == TokenType.ServerRewards ? msg("Money.RP", player.userID) : msg("Money.Eco", player.userID)), GetBalance(player.userID)), 12, new UI4(0.38f, 0.04f, 0.65f, 0.07f));
            }

            CuiHelper.DestroyUi(player, PopupPanel);
            CuiHelper.DestroyUi(player, MainPanel);
            CuiHelper.AddUi(player, container);
        }

        private void CreateSmallSelectionMenu(BasePlayer player, string shortname = "", int column = 0, int page = 0)
        {
            CuiElementContainer container = UI.Container(MainPanel, uiColor[Colors.Background], new UI4(0.2f, 0.12f, 0.45f, 0.92f), true);
            
            bool adminMode = adminToggle.Contains(player.userID);
            bool ownedMode = ownedToggle.Contains(player.userID);
            UserData data = userData[player.userID];

            int indexCount = 0;
            int columnIndex = column * 20;

            if (columnIndex > 0)
                UI.Button(ref container, MainPanel, uiColor[Colors.Button], msg("UI.Button.Up", player.userID), 10, new UI4(0.04f, 0.92f, 0.5f, 0.95f),
                    $"psui.changepage {(string.IsNullOrEmpty(shortname) ? "empty" : shortname)} {column - 1} {page}");
            if (columnIndex + 20 < skinData.Keys.Count - 1)
                UI.Button(ref container, MainPanel, uiColor[Colors.Button], msg("UI.Button.Down", player.userID), 10, new UI4(0.04f, 0.08f, 0.5f, 0.11f),
                    $"psui.changepage {(string.IsNullOrEmpty(shortname) ? "empty" : shortname)} {column + 1} {page}");

            string[] skinKeys = skinData.Keys.OrderBy(x => shortnameToDisplayname[x]).ToArray();

            for (int i = columnIndex; i < columnIndex + 20; i++)
            {
                if (i > skinKeys.Length - 1)
                    break;

                string itemShortname = skinKeys.ElementAt(i);
                string displayName = shortnameToDisplayname[itemShortname];
                float[] position = GetItemPosition(indexCount, 0.04f, 0.88f, 0.46f, 0.04f);

                UI.Button(ref container, MainPanel, shortname == itemShortname ? uiColor[Colors.ButtonSelected] : uiColor[Colors.Button], displayName, 10, new UI4(position[0], position[1], position[2], position[3]), shortname == itemShortname ? "" : $"psui.changepage {itemShortname} {column} 0");
                indexCount++;
            }

            if (!string.IsNullOrEmpty(shortname))
            {
                int itemCount = 0;
                int itemIndex = 5 * page;

                ulong[] skins = ownedMode ? (data.purchasedSkins.ContainsKey(shortname) ? data.purchasedSkins[shortname].Where(x =>
                    workshopItems.ContainsKey(shortname) &&
                    workshopItems[shortname].ContainsKey(x) &&
                    skinData.ContainsKey(shortname) &&
                    skinData[shortname].ContainsKey(x) &&
                    !skinData[shortname][x].isDisabled &&
                    HasImage(shortname, x)).ToArray() : new ulong[0]) :
                    skinData[shortname].Where(x =>
                    workshopItems.ContainsKey(shortname) &&
                    workshopItems[shortname].ContainsKey(x.Key) &&
                    !x.Value.isDisabled &&
                    HasImage(shortname, x.Key)).Select(x => x.Key).ToArray();

                for (int i = itemIndex; i < itemIndex + 5; i++)
                {
                    if (i > skins.Length - 1)
                        break;

                    ulong skinId = skins[i];
                    float[] position = GetButtonPosition(itemCount, 1, 0.5f, 0.5f, 0.8f, 0.18f);

                    UI.Image(ref container, MainPanel, GetImage(shortname, skinId), new UI4(position[0] + 0.05f, position[1], position[2] - 0.05f, position[3] - 0.01f));
                    UI.Button(ref container, MainPanel, "0 0 0 0", "", 0, new UI4(position[0], position[1], position[2] + 0.1f, position[3] - 0.01f), $"psui.selectitem {shortname} {skinId}");

                    itemCount++;
                }

                if (itemIndex > 0)
                    UI.Button(ref container, MainPanel, uiColor[Colors.Button], msg("UI.Button.Previous", player.userID), 10, new UI4(0.53f, 0.04f, 0.73f, 0.07f), $"psui.changepage {(string.IsNullOrEmpty(shortname) ? "empty" : shortname)} {column} {page - 1}");
                if (itemIndex + 5 < skinData[shortname].Where(x => !x.Value.isDisabled).Count())
                    UI.Button(ref container, MainPanel, uiColor[Colors.Button], msg("UI.Button.Next", player.userID), 10, new UI4(0.77f, 0.04f, 0.97f, 0.07f), $"psui.changepage {(string.IsNullOrEmpty(shortname) ? "empty" : shortname)} {column} {page + 1}");
            }
            else UI.Label(ref container, MainPanel, FormatHelpText(DisplayMode.Minimalist, player.userID), 10, new UI4(0.53f, 0.1f, 0.97f, 0.92f), TextAnchor.UpperLeft);

            UI.Button(ref container, MainPanel, uiColor[Colors.ButtonSelected], msg("UI.Exit", player.userID), 10, new UI4(0.04f, 0.04f, 0.5f, 0.07f), "psui.exit");

            if (forcedMode == DisplayMode.None)            
                UI.Button(ref container, MainPanel, uiColor[Colors.Button], msg("UI.FullScreen", player.userID), 10, new UI4(0.53f, 0.005f, 0.73f, 0.035f),
                $"psui.toggle size {(string.IsNullOrEmpty(shortname) ? "empty" : shortname)} {column} 0");

            UI.Button(ref container, MainPanel, ownedMode ? uiColor[Colors.ButtonSelected] : uiColor[Colors.Button], msg("UI.ShowOwned", player.userID), 10, new UI4(0.77f, 0.005f, 0.97f, 0.035f),
                $"psui.toggle owned {(string.IsNullOrEmpty(shortname) ? "empty" : shortname)} {column} 0");

            if (permission.UserHasPermission(player.UserIDString, "playerskins.admin"))            
                UI.Button(ref container, MainPanel, adminMode ? uiColor[Colors.ButtonSelected] : uiColor[Colors.Button], msg("UI.AdminMode", player.userID), 10, new UI4(0.04f, 0.005f, 0.24f, 0.035f),
                    $"psui.toggle admin {(string.IsNullOrEmpty(shortname) ? "empty" : shortname)} {column} {page}");

            if (configData.Purchase.Enabled)
            {
                UI.Panel(ref container, MainPanel, uiColor[Colors.Panel], new UI4(0, 0.97f, 1, 1));
                UI.Label(ref container, MainPanel, string.Format((purchaseType == TokenType.ServerRewards ? msg("Money.RP", player.userID) : msg("Money.Eco", player.userID)), GetBalance(player.userID)), 10, new UI4(0, 0.97f, 1, 1));
            }
            
            CuiHelper.DestroyUi(player, PopupPanel);
            CuiHelper.DestroyUi(player, MainPanel);
            CuiHelper.AddUi(player, container);
        }

        private void CreateItemPopup(BasePlayer player, string shortname, ulong skinId)
        {
            CuiElementContainer container = UI.Container(PopupPanel, uiColor[Colors.Panel], new UI4(0.45f, 0.24f, 0.65f, 0.66f), true);
            UI.Panel(ref container, PopupPanel, uiColor[Colors.Background], new UI4(0.01f, 0.01f, 0.99f, 0.99f));

            UserData data = userData[player.userID];

            bool isOwned = data.IsOwned(shortname, skinId);
            int skinCost = skinData[shortname][skinId].cost;

            WorkshopItem workshopItem = workshopItems[shortname][skinId];

            UI.Image(ref container, PopupPanel, GetImage(shortname, skinId), new UI4(0.1f, 0.4f, 0.9f, 0.95f));
            UI.Label(ref container, PopupPanel, workshopItem.title, 14, new UI4(0.1f, 0.35f, 0.9f, 0.4f));

            if (isOwned)
            {
                if (configData.Shop.SellSkins)
                    UI.Button(ref container, PopupPanel, uiColor[Colors.Button], string.Format(msg("UI.Popup.SellSkin", player.userID), skinCost), 12, new UI4(0.1f, 0.21f, 0.9f, 0.28f), $"psui.sellskin {shortname} {skinId}");

                UI.Button(ref container, PopupPanel, data.IsDefaultSkin(shortname, skinId) ? uiColor[Colors.ButtonSelected] : uiColor[Colors.Button], data.IsDefaultSkin(shortname, skinId) ? msg("UI.Popup.RemoveDefault", player.userID) : msg("UI.Popup.SetDefault", player.userID), 12, new UI4(0.1f, 0.13f, 0.9f, 0.2f), $"psui.setdefault {shortname} {skinId}");
            }

            UI.Button(ref container, PopupPanel, uiColor[Colors.ButtonSelected], isOwned ? msg("UI.Popup.Owned", player.userID) :
                !string.IsNullOrEmpty(skinData[shortname][skinId].permission) && !HasPermission(player, skinData[shortname][skinId].permission) ? msg("UI.Popup.VIP", player.userID) :
                (configData.Purchase.Enabled ? GetBalance(player.userID) >= skinCost ? string.Format(msg("UI.Popup.Purchase", player.userID), skinCost) :
                string.Format(msg("UI.Popup.Insufficient", player.userID), skinCost) :
                msg("UI.Popup.Claim", player.userID)), 12, new UI4(0.1f, 0.05f, 0.9f, 0.12f), isOwned || (!string.IsNullOrEmpty(skinData[shortname][skinId].permission) && !HasPermission(player, skinData[shortname][skinId].permission)) ? "" : $"psui.purchase {shortname} {skinId}");

            UI.Button(ref container, PopupPanel, uiColor[Colors.ButtonSelected], msg("UI.Popup.Exit", player.userID), 12, new UI4(0.91f, 0.92f, 0.98f, 0.98f), "psui.exitpopup");

            if (adminToggle.Contains(player.userID))
                CreateAdminPopup(ref container, shortname, skinId, player.userID);

            CuiHelper.DestroyUi(player, PopupPanel);
            CuiHelper.AddUi(player, container);
        }

        private void CreateAdminPopup(ref CuiElementContainer container, string shortname, ulong skinId, ulong playerId)
        {
            UI.Panel(ref container, PopupPanel, uiColor[Colors.Panel], new UI4(1f, 0f, 1.5f, 1f));
            UI.Panel(ref container, PopupPanel, uiColor[Colors.Background], new UI4(1f, 0.01f, 1.49f, 0.99f));

            UI.Label(ref container, PopupPanel, string.Format(msg("UI.Admin.Cost", playerId), skinData[shortname][skinId].cost), 12, new UI4(1.01f, 0.9f, 1.27f, 0.96f), TextAnchor.MiddleLeft);
            UI.Button(ref container, PopupPanel, uiColor[Colors.ButtonSelected], msg("UI.Admin.Down", playerId), 12, new UI4(1.27f, 0.905f, 1.37f, 0.955f), $"psui.setprice {shortname} {skinId} {skinData[shortname][skinId].cost - 1}");
            UI.Button(ref container, PopupPanel, uiColor[Colors.ButtonSelected], msg("UI.Admin.Up", playerId), 12, new UI4(1.38f, 0.905f, 1.48f, 0.955f), $"psui.setprice {shortname} {skinId} {skinData[shortname][skinId].cost + 1}");

            UI.Label(ref container, PopupPanel, msg("UI.Admin.Permission", playerId), 12, new UI4(1.01f, 0.81f, 1.48f, 0.9f), TextAnchor.MiddleLeft);

            for (int i = 0; i < configData.Shop.Permissions.Length; i++)
            {
                string permission = configData.Shop.Permissions[i];
                bool hasPermission = skinData[shortname][skinId].permission == permission;

                UI.Button(ref container, PopupPanel, hasPermission ? uiColor[Colors.ButtonSelected] : uiColor[Colors.Button], permission, 12, new UI4(1.01f, 0.81f - (0.07f * i) - 0.06f, 1.48f, 0.81f - (0.07f * i)), $"psui.setpermission {shortname} {skinId} {permission}");
            }

            UI.Button(ref container, PopupPanel, uiColor[Colors.ButtonSelected], msg("UI.Admin.Delete", playerId), 12, new UI4(1.01f, 0.05f, 1.48f, 0.12f), $"psui.remove {shortname} {skinId}");
        }

        private void CreateReskinMenu(BasePlayer player, int page = 0)
        {
            Item item = player.GetActiveItem();
            if (item == null)
            {
                SendReply(player, msg("Chat.Reskin.NoItem", player.userID));
                return;
            }

            string shortname = item.info.shortname;

            if (!skinData.ContainsKey(item.info.shortname) || skinData[item.info.shortname].Count == 0)
            {
                SendReply(player, msg("Chat.Reskin.NoSkins", player.userID));
                return;
            }

            UserData data;
            if (!userData.TryGetValue(player.userID, out data))
            {
                SendReply(player, msg("Chat.Reskin.NoPurchases", player.userID));
                return;
            }

            CuiElementContainer container = UI.Container(ReskinPanel, uiColor[Colors.Background], new UI4(0.35f, 0.15f, 0.65f, 0.4f), true);
            UI.Panel(ref container, ReskinPanel, uiColor[Colors.Panel], new UI4(0, 0.86f, 1, 1));
            UI.Label(ref container, ReskinPanel, string.Format(msg("UI.Reskin.SkinList", player.userID), shortnameToDisplayname[item.info.shortname]), 12, new UI4(0.1f, 0.86f, 0.9f, 1f));

            UI.Button(ref container, ReskinPanel, uiColor[Colors.ButtonSelected], "✘", 12, new UI4(0.93f, 0.88f, 0.99f, 0.98f), "psui.exit");

            List<ulong> skinList;
            if (data.purchasedSkins.TryGetValue(item.info.shortname, out skinList))
            {
                skinList = skinList.Where(x =>
                    workshopItems.ContainsKey(shortname) &&
                    workshopItems[shortname].ContainsKey(x) &&
                    skinData.ContainsKey(shortname) &&
                    skinData[shortname].ContainsKey(x) &&
                    !skinData[shortname][x].isDisabled &&
                    HasImage(shortname, x)).ToList();

                int itemIndex = page * 8;
                int itemCount = 0;
                
                for (int i = itemIndex; i < itemIndex + 8; i++)
                {
                    if (i > skinList.Count - 1)
                        break;

                    ulong skinId = skinList[i];

                    if (HasImage(item.info.shortname, skinId))
                    {
                        float[] position = GetButtonPosition(itemCount, 4, 0.05f, 0.225f, 0.425f, 0.4f);

                        UI.Image(ref container, ReskinPanel, GetImage(item.info.shortname, skinId), new UI4(position[0] + 0.005f, position[1], position[2] - 0.005f, position[3]));
                        UI.Button(ref container, ReskinPanel, "0 0 0 0", "", 0, new UI4(position[0], position[1], position[2], position[3]), $"psui.reskinitem {skinId}");
                    }
                    itemCount++;
                }
                UI.Panel(ref container, ReskinPanel, uiColor[Colors.Panel], new UI4(0, -0.1f, 1, 0));
                if (page > 0)
                    UI.Button(ref container, ReskinPanel, uiColor[Colors.ButtonSelected], msg("UI.Button.Previous", player.userID), 12, new UI4(0, -0.1f, 0.25f, 0), $"psui.changeskinpage {page - 1}");
                if (itemIndex + 8 < skinList.Count)
                    UI.Button(ref container, ReskinPanel, uiColor[Colors.ButtonSelected], msg("UI.Button.Next", player.userID), 12, new UI4(0.75f, -0.1f, 1, 0), $"psui.changeskinpage {page + 1}");
            }
            else UI.Label(ref container, ReskinPanel, msg("Chat.Reskin.NoPurchases", player.userID), 15, new UI4(0, 0.2f, 1, 0.8f));

            CuiHelper.DestroyUi(player, ReskinPanel);
            CuiHelper.AddUi(player, container);
        }

        #region UI Functions
        private float[] GetItemPosition(int number, float offsetx = 0.02f, float offsety = 0.88f, float width = 0.12f, float height = 0.04f)
        {
            float offsetX = offsetx;
            float offsetY = offsety - (number * height);

            return new float[] { offsetX, offsetY, offsetX + width, offsetY + (height - 0.01f) };
        }

        private float[] GetButtonPosition(int number, int rows = 8, float xOffset = 0.15f, float width = 0.1f, float yOffset = 0.8f, float height = 0.14f)
        {
            int rowNumber = number == 0 ? 0 : RowNumber(rows, number);
            int columnNumber = number - (rowNumber * rows);

            float offsetX = xOffset + (width * columnNumber);
            float offsetY = (yOffset - (rowNumber * height));

            return new float[] { offsetX, offsetY, offsetX + width, offsetY + height };
        }

        private int RowNumber(int max, int count) => Mathf.FloorToInt(count / max);

        private int GetBalance(ulong playerId)
        {
            object success = null;
            if (purchaseType == TokenType.ServerRewards)
            {
                success = ServerRewards?.Call("CheckPoints", playerId);               
                if (success is int)
                    return (int)success;
            }
            else
            {
                success = Economics?.Call("Balance", playerId);
                if (success is double)
                    return Convert.ToInt32(success);
            }

            return 0;
        }

        private bool TakeMoney(ulong playerId, int amount)
        {
            object success = null;
            if (purchaseType == TokenType.ServerRewards)
            {
                success = ServerRewards?.Call("TakePoints", playerId, amount);
                if (success is bool)
                    return (bool)success;
            }
            else
            {
                success = Economics?.Call("Withdraw", playerId, (double)amount);
                if (success is bool)
                    return (bool)success;
            }

            return false;
        }

        private void GiveMoney(ulong playerId, int amount)
        {
            if (purchaseType == TokenType.ServerRewards)
                ServerRewards?.Call("AddPoints", playerId, amount);
            else Economics?.Call("Deposit", playerId, (double)amount);
        }

        private string FormatHelpText(DisplayMode displayMode, ulong playerId)
        {
            if (displayMode == DisplayMode.Full)
            {
                return string.Format(msg("Help.Text.Full", playerId), purchaseType == TokenType.ServerRewards ? msg("Help.Text.RP", playerId) : msg("Help.Text.Eco", playerId), configData.Reskin.DisableCommand ? msg("Help.Text.Reskin.NPC", playerId) : msg("Help.Text.Reskin.Command", playerId));
            }
            else
            {
                return string.Format(msg("Help.Text.Small", playerId), purchaseType == TokenType.ServerRewards ? msg("Help.Text.RP", playerId) : msg("Help.Text.Eco", playerId), configData.Reskin.DisableCommand ? msg("Help.Text.Reskin.NPC", playerId) : msg("Help.Text.Reskin.Command", playerId));
            }
        }
        #endregion

        #region UI Commands
        [ConsoleCommand("psui.changepage")]
        private void ccmdChangePage(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            OpenSkinMenu(player, arg.GetString(0) == "empty" ? string.Empty : arg.GetString(0), arg.GetInt(1), arg.GetInt(2));
        }

        [ConsoleCommand("psui.changeskinpage")]
        private void ccmdChangeSkinPage(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            CreateReskinMenu(player, arg.GetInt(0));
        }

        [ConsoleCommand("psui.reskinitem")]
        private void ccmdChangeItemSkin(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            ChangeItemSkin(player, arg.GetUInt64(0));
            CuiHelper.DestroyUi(player, ReskinPanel);
        }

        [ConsoleCommand("psui.selectitem")]
        private void ccmdSelectItem(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            CreateItemPopup(player, arg.GetString(0), arg.GetUInt64(1));
        }

        [ConsoleCommand("psui.exit")]
        private void ccmdExit(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            DestroyUI(player);

            if (configData.Shop.HelpOnExit)
            {
                if (configData.Reskin.DisableCommand)
                    SendReply(player, msg("Help.Reskin.NPC", player.userID));
                else SendReply(player, msg("Help.Reskin.Command", player.userID));
            }
        }

        [ConsoleCommand("psui.exitpopup")]
        private void ccmdExitPopup(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            CuiHelper.DestroyUi(player, PopupPanel);
        }

        [ConsoleCommand("psui.remove")]
        private void ccmdRemoveSkin(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            string shortname = arg.GetString(0);
            ulong skinId = arg.GetUInt64(1);
            skinData[shortname][skinId].isDisabled = true;

            ImageLibrary.Call("RemoveImage", shortname, skinId);

            OpenSkinMenu(player, shortname, 0, 0);
        }

        [ConsoleCommand("psui.sellskin")]
        private void ccmdSellSkin(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            string shortname = arg.GetString(0);
            ulong skinId = arg.GetUInt64(1);

            UserData data = userData[player.userID];
            data.purchasedSkins[shortname].Remove(skinId);

            if (data.IsDefaultSkin(shortname, skinId))
                data.defaultSkins.Remove(shortname);

            GiveMoney(player.userID, skinData[shortname][skinId].cost);

            CreateItemPopup(player, shortname, skinId);
        }

        [ConsoleCommand("psui.setprice")]
        private void ccmdSetPrice(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            string shortname = arg.GetString(0);
            ulong skinId = arg.GetUInt64(1);
            int amount = Mathf.Clamp(arg.GetInt(2), 0, int.MaxValue);

            skinData[shortname][skinId].cost = amount;

            CreateItemPopup(player, shortname, skinId);
        }

        [ConsoleCommand("psui.setpermission")]
        private void ccmdSetPermission(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            string shortname = arg.GetString(0);
            ulong skinId = arg.GetUInt64(1);
            string permission = arg.GetString(2);

            if (skinData[shortname][skinId].permission == permission)
                skinData[shortname][skinId].permission = string.Empty;
            else skinData[shortname][skinId].permission = permission;

            CreateItemPopup(player, shortname, skinId);
        }

        [ConsoleCommand("psui.setdefault")]
        private void ccmdSetDefault(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            string shortname = arg.GetString(0);
            ulong skinId = arg.GetUInt64(1);

            UserData data = userData[player.userID];

            if (!data.defaultSkins.ContainsKey(shortname))
                data.defaultSkins.Add(shortname, skinId);
            else
            {
                if (data.defaultSkins[shortname] == skinId)
                    data.defaultSkins.Remove(shortname);
                else data.defaultSkins[shortname] = skinId;
            }

            CreateItemPopup(player, shortname, skinId);
        }

        [ConsoleCommand("psui.purchase")]
        private void ccmdPurchase(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            string shortname = arg.GetString(0);
            ulong skinId = arg.GetUInt64(1);
            int cost = skinData[shortname][skinId].cost;

            UserData data = userData[player.userID];
            if (configData.Purchase.Enabled && cost > GetBalance(player.userID))
                return;
            else
            {
                if (configData.Shop.GiveItemOnPurchase)
                {
                    Item item = ItemManager.CreateByName(shortname, 1, skinId);
                    player.GiveItem(item, BaseEntity.GiveItemReason.PickedUp);
                    DestroyUI(player);
                    return;
                }
                else
                {
                    if (!configData.Purchase.Enabled || TakeMoney(player.userID, cost))
                    {
                        if (!data.purchasedSkins.ContainsKey(shortname))
                            data.purchasedSkins.Add(shortname, new List<ulong>());

                        if (!data.purchasedSkins[shortname].Contains(skinId))
                            data.purchasedSkins[shortname].Add(skinId);
                    }
                }
            }

            CreateItemPopup(player, shortname, skinId);
        }

        [ConsoleCommand("psui.toggle")]
        private void ccmdToggleAdmin(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
                return;

            switch (arg.GetString(0))
            {
                case "admin":
                    if (adminToggle.Contains(player.userID))
                        adminToggle.Remove(player.userID);
                    else adminToggle.Add(player.userID);
                    break;
                case "owned":
                    if (ownedToggle.Contains(player.userID))
                        ownedToggle.Remove(player.userID);
                    else ownedToggle.Add(player.userID);
                    break;
                case "size":
                    UserData data;
                    if (userData.TryGetValue(player.userID, out data))                    
                        data.displayMode = (data.displayMode == DisplayMode.Full ? data.displayMode = DisplayMode.Minimalist : data.displayMode = DisplayMode.Full);
                    break;
                default:
                    break;
            }

            OpenSkinMenu(player, arg.GetString(1) == "empty" ? "" : arg.GetString(1), arg.GetInt(2), arg.GetInt(3));
        }
        #endregion
        #endregion

        #region Config           
        public enum Colors { Background, Panel, Button, ButtonSelected }
        public enum DisplayMode { None, Full, Minimalist }

        private ConfigData configData;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Announcement Options")]
            public AnnouncementOptions Announcements { get; set; }
            [JsonProperty(PropertyName = "Purchase Options")]
            public PurchaseOptions Purchase { get; set; }
            [JsonProperty(PropertyName = "Skin Shop Options")]
            public ShopOptions Shop { get; set; }
            [JsonProperty(PropertyName = "Re-skin Options")]
            public ReskinOptions Reskin { get; set; }
            [JsonProperty(PropertyName = "Workshop Options")]
            public WorkshopOptions Workshop { get; set; }
            [JsonProperty(PropertyName = "UI Options")]
            public UIOptions UI { get; set; }

            public class AnnouncementOptions
            {
                [JsonProperty(PropertyName = "Display help information to players")]
                public bool Enabled { get; set; }
                [JsonProperty(PropertyName = "Information display interval (minutes)")]
      //  Слив плагинов server-rust by Apolo YouGame
                public int Interval { get; set; }
            }
            public class PurchaseOptions
            {
                [JsonProperty(PropertyName = "Enable purchase system")]
                public bool Enabled { get; set; }
                [JsonProperty(PropertyName = "Plugin used to purchase skins (ServerRewards, Economics)")]
                public string Type { get; set; }
            }
            public class ShopOptions
            {
                [JsonProperty(PropertyName = "Custom permissions which can be assigned to skins")]
                public string[] Permissions { get; set; }
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
                [JsonProperty(PropertyName = "Retrieve workshop skin information when the plugin loads")]
                public bool Enabled { get; set; }
                [JsonProperty(PropertyName = "Word filter for workshop skins. If the skin title partially contains any of these words it will not be available as a potential skin")]
                public string[] Filter { get; set; }
            }
            public class UIOptions
            {
                [JsonProperty(PropertyName = "UI Colors")]
                public Dictionary<Colors, UIColor> Colors { get; set; }
                public class UIColor
                {
                    [JsonProperty(PropertyName = "Color (hex)")]
                    public string Color { get; set; }
                    [JsonProperty(PropertyName = "Alpha (0.0 - 1.0)")]
                    public float Alpha { get; set; }
                }
            }

            public VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            configData = Config.ReadObject<ConfigData>();

            if (configData.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(configData, true);
        }

        protected override void LoadDefaultConfig() => configData = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                Announcements = new ConfigData.AnnouncementOptions
                {
                    Enabled = true,
                    Interval = 10
                },
                Reskin = new ConfigData.ReskinOptions
                {
                    DisableCommand = false,
                    NPCs = new string[0]
                },
                Shop = new ConfigData.ShopOptions
                {
                    DisableCommand = false,
                    ForcedMode = "None",
                    GiveItemOnPurchase = false,
                    NPCs = new string[0],
                    Permissions = new string[] { "vip1", "vip2", "vip3" },
                    SellSkins = true,
                    HelpOnExit = true
                },
                Purchase = new ConfigData.PurchaseOptions
                {
                    Type = "ServerRewards",
                    Enabled = false
                },
                Workshop = new ConfigData.WorkshopOptions
                {
                    Enabled = true,
                    Filter = new string[0]
                },
                UI = new ConfigData.UIOptions
                {
                    Colors = new Dictionary<Colors, ConfigData.UIOptions.UIColor>
                    {
                        [Colors.Background] = new ConfigData.UIOptions.UIColor { Alpha = 0.7f, Color = "#2b2b2b" },
                        [Colors.Panel] = new ConfigData.UIOptions.UIColor { Alpha = 1f, Color = "#545554" },
                        [Colors.Button] = new ConfigData.UIOptions.UIColor { Alpha = 1f, Color = "#393939" },
                        [Colors.ButtonSelected] = new ConfigData.UIOptions.UIColor { Alpha = 1f, Color = "#d85540" }
                    }
                },
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(configData, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            ConfigData baseConfig = GetBaseConfig();
            if (configData.Version < new VersionNumber(2, 0, 1))
            {
                configData.Shop.HelpOnExit = baseConfig.Shop.HelpOnExit;
            }
            configData.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region Data Management
        private void SaveUserData() => userdata.WriteObject(userData);

        private void SaveSkinData() => skindata.WriteObject(skinData);

        private void LoadData()
        {
            try
            {
                userData = userdata.ReadObject<Dictionary<ulong, UserData>>();
            }
            catch
            {
                userData = new Dictionary<ulong, UserData>();
            }
            try
            {
                skinData = skindata.ReadObject<Dictionary<string, Dictionary<ulong, SkinData>>>();
            }
            catch
            {
                skinData = new Dictionary<string, Dictionary<ulong, SkinData>>();
            }
        }

        private class SkinData
        {
            public string permission = string.Empty;
            public int cost = 1;
            public bool isDisabled = false;
        }
        
        private class UserData
        {
            public Dictionary<string, ulong> defaultSkins = new Dictionary<string, ulong>();
            public Dictionary<string, List<ulong>> purchasedSkins = new Dictionary<string, List<ulong>>();
            public DisplayMode displayMode = DisplayMode.Minimalist;

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

            public WorkshopItem(Facepunch.Steamworks.Workshop.Item item)
            {
                title = item.Title;
                description = item.Description;
                imageUrl = item.PreviewImageUrl;
            }

            public WorkshopItem(Rust.Workshop.ItemSchema.Item item)
            {
                title = item.name;
                description = item.description;
                imageUrl = item.icon_url;
            }
        }
        #endregion

        private string msg(string key, ulong playerId = 0U) => lang.GetMessage(key, this, playerId == 0U ? null : playerId.ToString());

        private Dictionary<string, string> Messages = new Dictionary<string, string>
        {
            ["Money.RP"] = "Available RP : {0}",
            ["Money.Eco"] = "Available Coins : {0}",
            ["UI.AdminMode"] = "Admin Mode",
            ["UI.ShowOwned"] = "Show Owned",
            ["UI.FullScreen"] = "Full Screen",
            ["UI.Minimised"] = "Small UI",
            ["UI.Exit"] = "EXIT",
            ["UI.Popup.Owned"] = "Owned",
            ["UI.Popup.VIP"] = "VIP skin only",
            ["UI.Popup.SellSkin"] = "Sell skin ({0})",
            ["UI.Popup.RemoveDefault"] = "Remove as default",
            ["UI.Popup.SetDefault"] = "Set as default",
            ["UI.Popup.Purchase"] = "Purchase (Cost {0})",
            ["UI.Popup.Insufficient"] = "Not Enough (Cost {0})",
            ["UI.Popup.Claim"] = "Claim",
            ["UI.Admin.Cost"] = "Cost : {0}",
            ["UI.Admin.Permission"] = "Permission :",
            ["UI.Admin.Delete"] = "Delete From Store",
            ["Chat.Reskin.NoItem"] = "You need to hold a item in your hands to open the re-skin menu",
            ["Chat.Reskin.NoSkins"] = "There are no skins available for this item",
            ["Chat.Reskin.NoPurchases"] = "You have not purchased any skins from the skin shop",
            ["UI.Reskin.SkinList"] = "Skins purchased for {0}",
            ["UI.Button.Up"] = "▲ ▲ ▲",
            ["UI.Button.Down"] = "▼ ▼ ▼",
            ["UI.Button.Previous"] = "◄ ◄ ◄",
            ["UI.Button.Next"] = "► ► ►",
            ["UI.Admin.Up"] = "▼",
            ["UI.Admin.Down"] = "▲",
            ["UI.Popup.Exit"] = "✘",
            ["Help.Text.Full"] = "You can use the skin shop to purchase skins for your items using {0}\n\nOnce you have purchased a skin you can use the reskin menu to apply it to your item by {1}.\nOnce the reskin menu is open you can select from the list of skins you have purchased for that item by clicking the skin icon.",
            ["Help.Text.Small"] = "You can use the skin shop to purchase skins for your items using {0}\n\nOnce you have purchased a skin you can use the reskin menu to apply it to your item by {1}.\nOnce the reskin menu is open you can select from the list of skins you have purchased for that item by clicking the skin icon.",
            ["Help.Text.RP"] = "RP (ServerRewards)",
            ["Help.Text.Eco"] = "coins (Economics)",
            ["Help.Text.Reskin.NPC"] = "visiting a re-skin NPC",
            ["Help.Text.Reskin.Command"] = "typing '/skin'",
            ["Help.Shop.NPC"] = "You can access the skin shop by visiting a skin shop NPC!",
            ["Help.Shop.Command"] = "You can access the skin shop by typing '/skin shop'",
            ["Help.Reskin.NPC"] = "You can apply purchased skins by visiting a reskin NPC!",
            ["Help.Reskin.Command"] = "You can apply purchased skins by typing '/skin' while holding the item in your hands!"
        };
    }
}
