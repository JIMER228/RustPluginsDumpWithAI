using System.Collections.Generic;


namespace Oxide.Plugins
{
    [Info("Admin Inventory Cleaner", "MJSU", "1.2.0", ResourceId = 973)]
    class InventoryCleaner : RustPlugin
    {
        #region Setup & Loading
        PluginConfig _pluginConfig;
        private void Loaded()
        {
            LoadLang();

            _pluginConfig = ConfigOrDefault(Config.ReadObject<PluginConfig>());
            if (_pluginConfig.Prefix == null) PrintError("Loading config file failed. Using default config");
            else Config.WriteObject(_pluginConfig, true);

            permission.RegisterPermission("inventorycleaner.use", this);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Register the lang messages
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void LoadLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this command",
                ["Clean"] = "Your Inventory is now clean!"
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
                Prefix = config?.Prefix ?? "[<color=lime>InvCleaner</color>]",
                CleanBelt = config?.CleanBelt ?? true,
                CleanClothes = config?.CleanClothes ?? true,
                CleanMain = config?.CleanMain ?? true
            };
        }
        #endregion

        #region Chat Command
        /// <summary>
        /// If player is admin or has permission. Clean their inventory
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        [ChatCommand("cleaninv")]
        // ReSharper disable once UnusedMember.Local
        void CleanInventoryChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            if (HasPermission(player, "inventorycleaner.use") || player.IsAdmin())
            {
                List<Item> collection = new List<Item>();
                if (_pluginConfig.CleanMain)
                {
                    while(player.inventory.containerMain.itemList.Count != 0) 
                    {
                        Item itemToTake = player.inventory.containerMain.itemList[0];
                        player.inventory.containerMain.Take(collection, itemToTake.info.itemid, itemToTake.amount);
                    }
                }

                if (_pluginConfig.CleanBelt)
                {
                    while (player.inventory.containerBelt.itemList.Count != 0)
                    {
                        Item itemToTake = player.inventory.containerBelt.itemList[0];
                        player.inventory.containerBelt.Take(collection, itemToTake.info.itemid, itemToTake.amount);
                    }
                }

                if (_pluginConfig.CleanClothes)
                {
                    while (player.inventory.containerWear.itemList.Count != 0)
                    {
                        Item itemToTake = player.inventory.containerWear.itemList[0];
                        player.inventory.containerWear.Take(collection, itemToTake.info.itemid, itemToTake.amount);
                    }
                }

                Chat(player, Lang("Clean", player.UserIDString));
            }
            else
            {
                Chat(player, Lang("NoPermission", player.UserIDString));
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Prints formated chat message to player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        private void Chat(BasePlayer player, string message, params object[] args) => PrintToChat(player, $"{_pluginConfig.Prefix} {message}", args);

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// @credit to Exel80 - code from EasyVote
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
        class PluginConfig
        {
            public string Prefix { get; set; }
            public bool CleanBelt { get; set; }
            public bool CleanMain { get; set; }
            public bool CleanClothes { get; set; }
        }
        #endregion
    }
}
