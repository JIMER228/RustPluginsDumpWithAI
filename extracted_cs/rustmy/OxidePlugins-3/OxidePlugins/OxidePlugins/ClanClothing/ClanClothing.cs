using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;


// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    [Info("Clan Clothing", "MJSU", "0.0.1")]
    [Description("Allows clans to set clan clothing")]
    // ReSharper disable once UnusedMember.Global
    class ClanClothing : RustPlugin
    {
        [PluginReference]
        Plugin Clans; //Clans plugin reference
        [PluginReference]
        Plugin ServerRewards; //Server Rewards Plugin Reference
        [PluginReference]
        Plugin Economics; //Economics Plugin Reference

        #region Class Fields
        private StoredData _storedData; //Plugin Data
        private PluginConfig _pluginConfig; //Plugin Config

        private const string UsePermission = "clanclothing.use"; //Plugin use permission
        private const string IgnoreExclusionPermission = "clanclothing.ignoreexclude";
        #endregion

        #region Loading & Setup
        // ReSharper disable once UnusedMember.Local
        private void Loaded()
        {
            LoadLang();

            _pluginConfig = ConfigOrDefault(Config.ReadObject<PluginConfig>());
            if (_pluginConfig.Prefix == null) PrintError("Loading config file failed. Using default config");
            else Config.WriteObject(_pluginConfig, true);

            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("ClanClothing");

            //Add the chat commands from the config
            cmd.AddChatCommand(_pluginConfig.ChatCommand, this, ClanClothingChatCommand);

            permission.RegisterPermission(UsePermission, this);
            permission.RegisterPermission(IgnoreExclusionPermission, this);
        }

        /// <summary>
        /// Register the lang messages for the plugin
        /// </summary>
        private void LoadLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this command",
                ["ExcludedItem"] = "is on the excluded item list and will not be added",
                ["Claimed"] = "You have claimed your clans clothing",
                ["Add"] = "You have added your clans clothing",
                ["Remove"] = "You have removed your clans clothing",
                ["NotInClan"] = "You do not appear to be in a clan",
                ["ClansNotLoaded"] = "The Clans plugin is not loaded",
                ["NotAvaliable"] = "Clothing not avaliable - Your clan owner has not setup your clan clothing yet!",
                ["NotOwner"] = "Only the clan owner can use this command",
                ["SRProfileNotFound"] = " Could not find a ServerRewards Profile for you",
                ["SRCantAfford"] = "You do not have enough ServerRewards to claim your clans clothing.\n Have: {0} Requires: {1}",
                ["ECantAfford"] = "You do not have enough Economics money to claim your clans clothing.\n Have: {0} Requires: {1}",
                ["ItemCantAfford"] = "You do not have enough {0} Have: {1} Requires: {2}",
                ["PluginFailed"] = "Something went wrong while taking your {0}",
                ["PluginCost"] = "{0} cost - {1}",
                ["ItemListCost"] = "Item list cost:",
                ["CanAfford"] = "You <color=green>CAN</color> afford this",
                ["CanNotAfford"] = "You <color=red>CAN NOT</color> afford this",
                ["NoCost"] = "There is no cost to claim Clan Clothing",
                ["NoClothingAdded"] = "No clothing items were added. Clan clothing will not be saved"
            }, this);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// load the default config for this plugin
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
            Config.WriteObject(ConfigOrDefault(null), true);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Uses the values passed in from config. If any values are null it updates them with default values
        /// </summary>
        /// <param name="config">Config that has been loaded or null</param>
        /// <returns>Config using values passed in from config default values</returns>
        /// ////////////////////////////////////////////////////////////////////////
        private PluginConfig ConfigOrDefault(PluginConfig config)
        {
            return new PluginConfig
            {
                ExcludedItems = config?.ExcludedItems ?? new List<string> { "metal.facemask", "metal.plate.torso", "roadsign.jacket", "roadsign.kilt" },
                Prefix = config?.Prefix ?? "[<color=yellow>Clan Clothing</color>]",
                WipeClanClothingOnMapWipe = config?.WipeClanClothingOnMapWipe ?? true,
                UseCost = config?.UseCost ?? false,
                ServerRewardsCost = config?.ServerRewardsCost ?? 0,
                EconomicsCost = config?.EconomicsCost ?? 0,
                UseItems = config?.UseItems ?? false,
                ItemCostList = config?.ItemCostList ?? new Hash<string, int> { ["wood"] = 100, ["stones"] = 50, ["metal.fragments"] = 25 },
                ChatCommand = config?.ChatCommand ?? "clanclothing"
            };
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Wipes the Clan Clothing data on map wipe if WipeDataOnMapWipe is set to true on the config
        /// </summary>
        /// <param name="name"></param>
        /// ///////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnNewSave(string name)
        {
            if (_pluginConfig.WipeClanClothingOnMapWipe)
            {
                PrintWarning("Map wipe detected - Wiping Clan Clothing Data");
                _storedData = new StoredData();
                Interface.Oxide.DataFileSystem.WriteObject("ClanClothing", _storedData);
            }
        }
        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Determines if the Clans plugin loaded. If not displays an error
        /// </summary>
        /// ///////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            if (Clans == null)
            {
                PrintWarning($"clans plugin failed to load");
            }

            if (_pluginConfig.UseCost) //Config is set to use cost
            {
                if (_pluginConfig.ServerRewardsCost > 0 && ServerRewards == null) //Server Rewards set to use but plugin is not present
                {
                    PrintWarning($"ServerRewards cost is greater then 0 but plugin was not found");
                }

                if (_pluginConfig.EconomicsCost > 0 && Economics == null) //Economics set to use but plugin is not present
                {
                    PrintWarning($"Economics cost is greater then 0 but plugin was not found");
                }
            }
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Plugin Unloading Save DataFile
        /// </summary>
        /// ///////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            Interface.Oxide.DataFileSystem.WriteObject("ClanClothing", _storedData);
        }
        #endregion

        #region Chat Commands
        /// <summary>
        /// Chat Command for Clan Clothing
        /// </summary>
        /// <param name="player">player who called it</param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        private void ClanClothingChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, UsePermission)) //Make sure player has permission
            {
                PrintToChat(player, Lang("NoPermission", player.UserIDString));
                return; 
            }

            if(args.Length != 1) // No arguments were passed send help text
            {
                SendHelpText(player);
                return;
            }

            string playerClanTag = GetPlayerClanTag(player);
            if (playerClanTag == null) //Failed to get Clan Tag for player
            {
                PrintToChat(player, Lang("NotInClan", player.UserIDString));
                return; 
            }

            switch (args[0].ToLower())
            {
                case "add":
                    AddClothing(player, playerClanTag);
                    break;

                case "remove":
                    RemoveClothing(player, playerClanTag);
                    break;

                case "claim":
                    ClaimClothing(player, playerClanTag);
                    break;

                case "view":
                    ViewClothing(player, playerClanTag);
                    break;

                case "check":
                    CheckClothingCost(player);
                    break;

                default:
                    SendHelpText(player);
                    break;
            }
        }
        #endregion

        #region Clan Clothing Chat Respone
        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Allows the owner of a clan to add their clan clothing
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playerClanTag">Clan tag for the player</param>
        /// ///////////////////////////////////////////////////////////////
        private void AddClothing(BasePlayer player, string playerClanTag)
        {
            if (!IsPlayerClanOwner(player, playerClanTag)) //Player is not the clan owner
            {
                PrintToChat(player, Lang("NotOwner", player.UserIDString));
                return; 
            }

            bool playerHasExcludePermission = HasPermission(player, IgnoreExclusionPermission); //So we don't have to call check permission multiple times
            List<ClothingItem> clanClothing = new List<ClothingItem>();

            foreach (Item item in player.inventory.containerWear.itemList) //Add the items from the owners wear container to the clans clothing list
            {
                if (!playerHasExcludePermission && _pluginConfig.ExcludedItems.Contains(item.info.shortname)) //If and item is on the excluded item list
                {
                    PrintToChat($"{_pluginConfig.Prefix} {ItemManager.FindItemDefinition(item.info.shortname).displayName.translated} {Lang("ExcludedItem", player.UserIDString)}");
                }
                else //item gets added to the clan clothing list
                {
                    clanClothing.Add(new ClothingItem(item.info.itemid, item.skin));
                }
            }

            if(clanClothing.Count == 0)
            {
                PrintToChat($"{_pluginConfig.Prefix} {Lang("NoClothingAdded", player.UserIDString)}");
                return;
            }

            _storedData.ClanClothing[playerClanTag] = clanClothing;

            PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("Add", player.UserIDString)}");
            Interface.Oxide.DataFileSystem.WriteObject("ClanClothing", _storedData);
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Allows the owner of a clan to remove their clan clothing
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playerClanTag">Clan tag for the player</param>
        /// ///////////////////////////////////////////////////////////////
        private void RemoveClothing(BasePlayer player, string playerClanTag)
        {
            if (!IsPlayerClanOwner(player, playerClanTag)) //Player is not the clan owner
            {
                PrintToChat(player, Lang("NotOwner", player.UserIDString));
                return;
            }

            _storedData.ClanClothing[playerClanTag] = null; //Remove the clan from the plugin data
            PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("Remove", player.UserIDString)}");
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Allows players in a clan to claim their clan clothing
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playerClanTag">Clan tag for the player</param>
        /// ///////////////////////////////////////////////////////////////
        private void ClaimClothing(BasePlayer player, string playerClanTag)
        {
            List<ClothingItem> itemsToGive = _storedData.ClanClothing[playerClanTag]; //Get's the Clothing for the Players Clan

            if (itemsToGive == null || itemsToGive.Count == 0) //No clan clothing is configured or contains no items
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("NotAvaliable", player.UserIDString)}");
                return;
            }

            if (!CanPlayerAfford(player)) return; //Player can't afford the clan clothing    
            TakeCostFromPlayer(player);

            foreach (ClothingItem item in itemsToGive) //Give all the items to the player
            {
                player.inventory.GiveItem(ItemManager.CreateByItemID(item.ItemId, 1, item.SkinId), player.inventory.containerWear);
            }

            PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("Claimed", player.UserIDString)}");
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Player command to see their clans clothing
        /// If a skin is used will show the skin name
        /// </summary>
        /// <param name="player"></param>
        /// <param name="playerClanTag">Clan tag for the player</param>
        /// ///////////////////////////////////////////////////////////////
        private void ViewClothing(BasePlayer player, string playerClanTag)
        {
            if(_storedData.ClanClothing[playerClanTag] == null || _storedData.ClanClothing[playerClanTag].Count == 0) //Players clan has not setup clan clothing
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("NotAvaliable", player.UserIDString)}");
                return;
            }

            //Prepare the message to the player to display their clans clothing
            string message = $"{_pluginConfig.Prefix} Clan clothing for [<color=orange>{playerClanTag}</color>]: \n";
            foreach(ClothingItem clothingItem in _storedData.ClanClothing[playerClanTag])
            {
                message += " - " + (clothingItem.SkinId == 0 ? 
                                        ItemManager.FindItemDefinition(clothingItem.ItemId).displayName.translated : 
                                        ItemSkinDirectory.Instance.skins.Where(skinItem => skinItem.id == clothingItem.SkinId).FirstOrDefault().invItem.displayName.translated) + "\n";
            }

            PrintToChat(player, message);
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Chat Command for play to check how much Clan Clothing costs
        /// </summary>
        /// <param name="player"></param>
        /// ///////////////////////////////////////////////////////////////
        private void CheckClothingCost(BasePlayer player)
        {
            string message = _pluginConfig.Prefix + "\n"; //Put the plugin prefix at the begining
            if (_pluginConfig.UseCost) //Use cost is true
            {
                if (_pluginConfig.ServerRewardsCost > 0) //Use server rewards is true
                {
                    message += $"{Lang("PluginCost", player.UserIDString, "ServerRewards", _pluginConfig.ServerRewardsCost)}\n";
                }

                if (_pluginConfig.EconomicsCost > 0) //Use economics is true
                {
                    message += $"{Lang("PluginCost", player.UserIDString, "Economics", _pluginConfig.EconomicsCost)}\n";
                }

                if (_pluginConfig.UseItems) //Use items is true
                {
                    message += $"{Lang("ItemListCost", player.UserIDString)}\n ";

                    foreach (KeyValuePair<string, int> item in _pluginConfig.ItemCostList)
                    {
                        message += $" - {ItemManager.FindItemDefinition(item.Key).displayName.translated} - {item.Value}\n";
                    }
                }

                //Puts whether the player can afford the clan clothing
                message += CanPlayerAfford(player) ? Lang("CanAfford", player.UserIDString) : Lang("CanNotAfford", player.UserIDString);
            }
            else //Use cost is false clan clothing cost nothing
            {
                message += Lang("NoCost", player.UserIDString);
            }

            PrintToChat(player, message);
        }
        #endregion

        #region Player cost handling
        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Determines if the player can afford the clan clothing
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        /// ///////////////////////////////////////////////////////////////
        private bool CanPlayerAfford(BasePlayer player)
        {
            if (!_pluginConfig.UseCost) return true; //Use cost is false

            bool canPlayerAfford = true;
            string message = _pluginConfig.Prefix + ":\n";

            if (_pluginConfig.ServerRewardsCost > 0) //Use server rewards
            {
                if (ServerRewards != null) //Server rewards is loaded
                {
                    object playerPoints = ServerRewards?.Call("CheckPoints", player.userID); //get the player server rewards points

                    if (playerPoints == null) //failed to retrieve the player server reward points
                    {
                        message += $"{Lang("SRProfileNotFound", player.UserIDString)}\n";
                        canPlayerAfford = false;
                    }
                    else if ((int)(playerPoints) < _pluginConfig.ServerRewardsCost) //The player cannot afford the cost in server rewards
                    {
                        message += $"{Lang("SRCantAfford", player.UserIDString, (int)(playerPoints), _pluginConfig.ServerRewardsCost)}\n";
                        canPlayerAfford = false;
                    }
                }
                else //Server rewards is set to true but failed to load
                {
                    PrintWarning("UseServerRewards set to true but ServerRewards was not loaded");
                }
            }

            if (_pluginConfig.EconomicsCost > 0) //Use Economics
            {
                if (Economics != null) //Economics is loaded
                {
                    // ReSharper disable once ConstantNullCoalescingCondition
                    double playerMoney = Economics?.Call<double>("GetPlayerMoney", player.userID) ?? 0; //Get the player economics points

                    if (playerMoney < _pluginConfig.EconomicsCost) //player cannot afford the economics money
                    {
                        message += $"{Lang("ECantAfford", player.UserIDString, playerMoney, _pluginConfig.EconomicsCost)}\n";
                        canPlayerAfford = false;
                    }
                }
                else //Economics is set to true but failed to load
                {
                    PrintWarning("Economics set to true but ServerRewards was not loaded");
                }
            }

            if (_pluginConfig.UseItems) //Use items is set to true
            {
                bool canItemAfford = true;
                foreach (KeyValuePair<string, int> item in _pluginConfig.ItemCostList) //Loop over the ItemCostList set in the config
                {
                    Item playerInventoryItem = player?.inventory?.FindItemID(item.Key); //Find the item in the players inventory

                    if (playerInventoryItem == null || playerInventoryItem.amount < item.Value) //Player does not have the item or they do not have enough
                    {
                        if (canItemAfford) message += "Item's you can't afford:\n";
                        message += $"   {Lang("ItemCantAfford", player?.UserIDString, ItemManager.FindItemDefinition(item.Key).displayName.translated, playerInventoryItem?.amount ?? 0, item.Value)}\n";
                        canItemAfford = false;
                    }
                }

                if (!canItemAfford) canPlayerAfford = false;
            }

            if (!canPlayerAfford) PrintToChat(player, message);
            return canPlayerAfford; //Player can afford
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Takes the cost from the player
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        /// ///////////////////////////////////////////////////////////////
        private void TakeCostFromPlayer(BasePlayer player)
        {
            if (!_pluginConfig.UseCost) return; //Use cost is false

            if (_pluginConfig.ServerRewardsCost > 0 && ServerRewards != null) //Use ServerRewards and Server Rewards is loaded
            {
                object success = ServerRewards?.Call("TakePoints", player, _pluginConfig.ServerRewardsCost); //Take the server reward points from the player

                if (success == null) //Player does not have any server rewards or failed to call plugin
                {
                    PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("PluginFailed", player.UserIDString, "ServerRewards")}");
                    PrintWarning(Lang("PluginFailed", player.UserIDString, "ServerRewards"));
                }
            }

            if (_pluginConfig.EconomicsCost > 0 && Economics != null) //Use Economics and Economics is loaded
            {
                object success = Economics?.Call("Withdraw", player, _pluginConfig.EconomicsCost); //Take the economics money from the player

                if (success == null) //Failed to call Withdraw on the economics plugin
                {
                    PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("PluginFailed", player.UserIDString, "Economics money")}");
                    PrintWarning(Lang("PluginFailed", player.UserIDString, "Economics money"));
                }
            }

            if (_pluginConfig.UseItems) //Use items is true
            {
                List<Item> collection = new List<Item>();
                foreach (KeyValuePair<string, int> item in _pluginConfig.ItemCostList)
                {
                    ItemDefinition itemToTake = ItemManager.FindItemDefinition(item.Key);
                    player.Command("note.inv ", itemToTake.itemid, item.Value * -1f);
                    player.inventory.Take(collection, itemToTake.itemid, item.Value);
                }
            }
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Gets the players can tag.
        /// </summary>
        /// <param name="player"></param>
        /// <returns>Players clan tag or null if player is not in a clan</returns>
        /// ///////////////////////////////////////////////////////////////
        private string GetPlayerClanTag(BasePlayer player)
        {
            if (Clans == null) //No Clans plugin loaded
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("ClansNotLoaded", player.UserIDString)}");
                return null;
            }

            var playerClanTag = Clans?.Call<string>("GetClanOf", player.userID);
            if (playerClanTag == null) //Player is not in a clan
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("NotInClan", player.UserIDString)}");
                return null;
            }

            return playerClanTag;
        }

        /// <summary>
        /// Determines if the player is the owner of the clan
        /// </summary>
        /// <param name="player">Player calling the plugin</param>
        /// <param name="playerClanTag">Clan tag of the player</param>
        /// <returns>true if player is owner of the clan, false otherwise</returns>
        private bool IsPlayerClanOwner(BasePlayer player, string playerClanTag)
        {
            JObject playerClanData = Clans?.Call<JObject>("GetClan", playerClanTag); //Gets the players Clan Information
            if (playerClanData == null) return false;

            ulong playerClanOwnerId;
            if (!ulong.TryParse(playerClanData["owner"].ToString(), out playerClanOwnerId)) return false; //If it failed to parse the owner to a ulong

            if (player.userID != playerClanOwnerId) //Player does not own the clan
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("NotOwner", player.UserIDString)}");
                return false;
            }

            return true;
        }
        #endregion

        #region Helper Methods
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// @credit to Exel80 - code from EasyVote
        /// Gets the registered lang messages
        /// </summary>
        /// <param name="key"></param>
        /// <param name="id"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        ///  //////////////////////////////////////////////////////////////////////////////////////
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Checks if the user has the given permissions
        /// </summary>
        /// <param name="player">Player to be checked</param>
        /// <param name="perm">Permission to check for</param>
        /// <returns></returns>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);
        #endregion

        #region Classes
        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Clan Clothing Plugin Config
        /// </summary>
        /// ///////////////////////////////////////////////////////////////
        private class PluginConfig
        {
            public List<string> ExcludedItems { get; set; }
            public string Prefix { get; set; }
            public bool WipeClanClothingOnMapWipe { get; set; }
            public bool UseCost { get; set; }
            public int ServerRewardsCost { get; set; }
            public double EconomicsCost { get; set; }
            public bool UseItems { get; set; }
            public Hash<string, int> ItemCostList { get; set; }
            public string ChatCommand { get; set; }
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Clan Clothing Plugin Data
        /// </summary>
        /// ///////////////////////////////////////////////////////////////
        private class StoredData
        {
            //Hash<ClanTag, List<ClothingItems>>
            public Hash<string, List<ClothingItem>> ClanClothing = new Hash<string, List<ClothingItem>>();
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Represents a clothing item for a clan
        /// </summary>
        /// ///////////////////////////////////////////////////////////////
        private class ClothingItem
        {
            public int ItemId { get; }
            public int SkinId { get; }

            public ClothingItem(int itemId, int skinId)
            {
                ItemId = itemId;
                SkinId = skinId;
            }
        }
        #endregion

        #region Help Text
        private void SendHelpText(BasePlayer player)
        {
            PrintToChat(player, $"{_pluginConfig.Prefix} Help Text:\n"+
                                "Allows clan owners to set their clans clothing. Clan members can then claim their clans clothing.\n"+
                                 "This will also save the skin of the clothing item added.\n"+
                                $" - <color=yellow>/{_pluginConfig.ChatCommand}</color> - to view this help text\n" +
                                $" - <color=yellow>/{_pluginConfig.ChatCommand} check</color> - Check if you can afford clan clothing\n"+
                                $" - <color=yellow>/{_pluginConfig.ChatCommand} claim</color> - Claim your clans clothing\n"+
                                $" - <color=yellow>/{_pluginConfig.ChatCommand} view</color> - Shows the player the clothing for their clan\n" +
                                $" - <color=yellow>/{_pluginConfig.ChatCommand} add</color> - Allows the clan owner to set their current clothing as the clan clothing\n"+
                                $" - <color=yellow>/{_pluginConfig.ChatCommand} remove</color> - Allows the clan owner to remove their current clan clothing\n");
        }
        #endregion
    }
}
