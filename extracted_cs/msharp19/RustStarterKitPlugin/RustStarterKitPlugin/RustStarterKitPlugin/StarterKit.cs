using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    /// <summary>
    /// Plugin that allows Rust starter kits
    /// </summary>
    [Info("StarterKit", "matta", "1.0")]
    [Description("Starter kits for both new and existing players (spawn-respawn). This also includes the ability to run commands on spawn/respawn so this can be used in conjunction with other plugin commands ie. recycler.give USER")]
    public class StarterKit : RustPlugin
    {
        /// <summary>
        /// Class config
        /// </summary>
        private StarterKitConfig _config;

        /// <summary>
        /// Sotrage data
        /// </summary>
        private StoredData _storedData;

        /// <summary>
        /// Data storage file name
        /// </summary>
        private const string _startKitDataStorageKey = "StarterKitData";

        #region Hooks 

        /// <summary>
        /// Called on init of plugin
        /// </summary>
        void Init()
        {
            // Load persisted data
            LoadStoredData();
            // Load successful load
            Puts("RustStarterKit loaded.");
        }

        /// <summary>
        /// Called on new .sav file creation - reset stored data
        /// </summary>
        /// <param name="filename"></param>
        void OnNewSave(string filename)
        {
            // Create new storage data object
            _storedData = new StoredData().GenerateDefaults();

            // Overwrite current one
            Interface.Oxide.DataFileSystem.WriteObject(_startKitDataStorageKey, _storedData);
        }

        /// <summary>
        /// Called on player respawn
        /// </summary>
        /// <param name="player"></param>
        void OnUserRespawned(IPlayer player)
        {
            // Check the start kit is enabled
            if (_config.Enabled)
            {
                // Convert to base player
                var respawnPlayer = player.Object as BasePlayer;

                // Add starter kit
                AddStarterKit(respawnPlayer, _config.CommandsToRunOnRespawn, _config.ItemsToAddToInventoryOnRespawn);
            }
        }

        #endregion Hooks

        #region Helpers

        /// <summary>
        /// Loads stored data
        /// </summary>
        private void LoadStoredData()
        {
            // Load stored data from file
            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(_startKitDataStorageKey);

            // If nothing already exists, generate default properties (init list/s and so forth)
            if (_storedData == null || _storedData.PlayerItemCooldownPeriods == null)
                _storedData = new StoredData().GenerateDefaults();
        }

        /// <summary>
        /// Creates a starter kit using config from commands and predetermined inventory items
        /// </summary>
        /// <param name="player">The player to equip</param>
        /// <param name="commands">Any commands to run (to map other plugins)</param>
        /// <param name="inventoryItems">And inventory items to provide players with</param>
        private void AddStarterKit(BasePlayer player, List<Command> commands, List<InventoryItem> inventoryItems)
        {
            // Used to check if storage has been updated (indicator to persist)
            var commandStorageUpdated = false;
            var inventoryStorageUpdated = false;

            // If we want to remove stone & torch
            if (_config.RemoveExistingInventory)
                player.inventory.Strip();

            // Check if commands are to be run
            if (_config.CommandsToRunOnRespawnEnabled)
                RunCommands(player, commands, out commandStorageUpdated);

            // Add inventory items defined in config
            if (_config.ItemsToAddToInventoryOnRespawnEnabled)
                AddInventoryItems(player, inventoryItems, out inventoryStorageUpdated);

            // If we want to notify the user about the starter kit
            if (_config.ShowRespawnMessage)
                player.ChatMessage(_config.RespawnMessage);

            // Write object to storage file for backup if storage updated
            if (commandStorageUpdated || inventoryStorageUpdated)
                Interface.Oxide.DataFileSystem.WriteObject(_startKitDataStorageKey, _storedData);
        }

        /// <summary>
        /// Runs commands (defined in config)
        /// </summary>
        /// <param name="player">The player to run predetermined commands for</param>
        /// <param name="commands">The commands to run</param>
        /// <param name="storageHasBeenUpdated">If storage has been updated during runtime of this code</param>
        private void RunCommands(BasePlayer player, List<Command> commands, out bool storageHasBeenUpdated)
        {
            // Log that commands are being run
            Puts("Running commands");

            // Default update state
            storageHasBeenUpdated = false;

            // Run the commands one by one
            foreach (var command in commands)
            {
                // Check that player isnt in the cool down period for current item
                if (!IsPlayerInCoolDownPeriodForItem(player.UserIDString, command.Text))
                {
                    // If the player is not in cooldown, run command and add player id if required {0}
                    Server.Command(string.Format(command.Text, player.UserIDString));
                    // Log command run
                    Puts($"Command: {command} executed at: {DateTime.UtcNow}UTC");

                    // Try to add cool down period for player-item, if successful - set state
                    if (AddPlayerCoolDownPeriodForItem(player.UserIDString, command.Text, command.CoolDownPeriodInSeconds))
                        storageHasBeenUpdated = true;
                }
            }
        }

        /// <summary>
        /// Adds inventory items to a player
        /// </summary>
        /// <param name="player">The player to add inventory items to</param>
        /// <param name="inventoryItems">The inventory items to add</param>
        private void AddInventoryItems(BasePlayer player, List<InventoryItem> inventoryItems, out bool storageHasBeenUpdated)
        {
            // Log action
            Puts($"Setting inventory for player:{player.UserIDString}");

            // Default update state
            storageHasBeenUpdated = false;

            // Add items to inventory
            foreach (var inventoryItem in inventoryItems)
            {
                try
                {
                    // Check that player isnt in the cool down period for current item
                    if (!IsPlayerInCoolDownPeriodForItem(player.UserIDString, inventoryItem.ShortCode))
                    {
                        // If the player is not in cooldown, safely try to add inventory item
                        player.inventory.GiveItem(ItemManager.CreateByName(inventoryItem.ShortCode, inventoryItem.Amount));

                        // Try to add cool down period for player-item, if successful - set state
                        if (AddPlayerCoolDownPeriodForItem(player.UserIDString, inventoryItem.ShortCode, inventoryItem.CoolDownPeriodInSeconds) == true)
                            storageHasBeenUpdated = true;
                    }
                }
                catch(Exception ex)
                {
                    // If error - log it but then we can continue to add the rest of the inventory items
                    Puts($"Error adding inventory item:{inventoryItem.ShortCode} with amount:{inventoryItem.Amount}.");
                    Puts($"Stacktrace: {ex.StackTrace}");
                }
            }
        }

        /// <summary>
        /// Checks if a user has a cool down end date cached and if so, that the cool down period is still in effect (or not)
        /// </summary>
        /// <param name="userId">The user id</param>
        /// <param name="item">The item to check cooldown period for</param>
        /// <returns>If the player is in the cool down period for item</returns>
        private bool IsPlayerInCoolDownPeriodForItem(string userId, string item)
        {
            // Create key and default cooldown
            var key = $"{userId}_{item}";
            DateTime coolDownEndDate = DateTime.MinValue;

            // Look for cooldown period matching key
            if (_storedData.PlayerItemCooldownPeriods.TryGetValue(key, out coolDownEndDate))
            {
                // Check that the cooldown period hasnt ended - ends here if it hasnt
                if (coolDownEndDate > DateTime.UtcNow)
                    return true;

                // If we have past the cooldown period, we can then remove that old item
                _storedData.PlayerItemCooldownPeriods.Remove(key);
            }

            // Player has no restriction (cooldown period) for item sort code
            return false;
        }

        /// <summary>
        /// Adds a cool down period for a player-item
        /// </summary>
        /// <param name="userId">The user id</param>
        /// <param name="item">The item to check cooldown period for</param>
        private bool AddPlayerCoolDownPeriodForItem(string userId, string item, int coolDownPeriodInSeconds)
        {
            // If set to 0 or less, no point persisting
            if (coolDownPeriodInSeconds <= 0)
                return false;

            // Create key
            var key = $"{userId}_{item}";

            // Add cool down period (date + cool down in seconds) locally (in-memory)
            _storedData.PlayerItemCooldownPeriods?.Add(key, DateTime.UtcNow.AddSeconds(coolDownPeriodInSeconds));

            return true;
        }

        /// <summary>
        /// Loads start kit config
        /// </summary>
        protected override void LoadConfig()
        {
            // Load base config
            base.LoadConfig();

            // Try to load starter kit config (may have errors)
            try
            {
                // Try to read config
                _config = Config.ReadObject<StarterKitConfig>();

                // If empty then load the default
                if (_config == null) 
                    LoadDefaultConfig();
            }
            catch
            {
                // Log if read error
                PrintError($"An error occurred reading the configuration file!");
                PrintError($"Check it with any JSON Validator!");
                return;
            }

            // Save the config
            Config.WriteObject(_config);
        }

        /// <summary>
        /// The default config is loaded in constructor (everything turned off)
        /// </summary>
        protected override void LoadDefaultConfig() 
            => _config = new StarterKitConfig();

        #endregion Helpers
    }

    #region Helper Classes

    /// <summary>
    /// Config class
    /// </summary>
    public class StarterKitConfig
    {
        /// <summary>
        /// If the start kit is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Allows for the commands to be turned of (if not needed) for on respawn command actions
        /// </summary>
        public bool CommandsToRunOnRespawnEnabled { get; set; }

        /// <summary>
        /// List of commands to run as part of starter kit (ie. can give recycler) on respawn
        /// </summary>
        public List<Command> CommandsToRunOnRespawn { get; set; }

        /// <summary>
        /// Allows items to be added to player inventory on respawn to be turned of (if not needed)
        /// </summary>
        public bool ItemsToAddToInventoryOnRespawnEnabled { get; set; }

        /// <summary>
        /// Items to add to the inventory on respawn
        /// </summary>
        public List<InventoryItem> ItemsToAddToInventoryOnRespawn { get; set; }

        /// <summary>
        /// Switch to enable the respawn message to be shown
        /// </summary>
        public bool ShowRespawnMessage { get; set; }

        /// <summary>
        /// The message to be shown on respawn (if enabled) to alert user to starter kit
        /// </summary>
        public string RespawnMessage { get; set; }

        /// <summary>
        /// Switch to remove all existing inventory items (stone & torch)
        /// </summary>
        public bool RemoveExistingInventory { get; set; }

        /// <summary>
        /// Constructor - sets defaults
        /// </summary>
        public StarterKitConfig()
        {
            // Defaults (off and empty)
            Enabled = false;
            ItemsToAddToInventoryOnRespawnEnabled = false;
            ItemsToAddToInventoryOnRespawn = new List<InventoryItem>();
            CommandsToRunOnRespawnEnabled = false;
            CommandsToRunOnRespawn = new List<Command>();
            ShowRespawnMessage = false;
            RespawnMessage = string.Empty;
            RemoveExistingInventory = false;
        }
    }

    /// <summary>
    /// An inventory item to give
    /// </summary>
    public class InventoryItem
    {
        /// <summary>
        /// The item shortcode eg. ammo.shotgun.fire
        /// </summary>
        public string ShortCode { get; set; }

        /// <summary>
        /// The number of shortcode item to give
        /// </summary>
        public int Amount { get; set; }

        /// <summary>
        /// How long until item can once be given on respawn
        /// </summary>
        public int CoolDownPeriodInSeconds { get; set; }
    }

    /// <summary>
    /// Model to hold data for storage
    /// </summary>
    public class StoredData
    {
        /// <summary>
        /// Player-item cool down periods
        /// </summary>
        public Dictionary<string, DateTime> PlayerItemCooldownPeriods { get; set; }

        /// <summary>
        /// Generate default property values
        /// </summary>
        public StoredData GenerateDefaults()
        {
            PlayerItemCooldownPeriods = new Dictionary<string, DateTime>();
            return this;
        }
    }

    /// <summary>
    /// A command to execute
    /// </summary>
    public class Command
    {
        /// <summary>
        /// The command text to execute
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// How long until item can once be given on respawn
        /// </summary>
        public int CoolDownPeriodInSeconds { get; set; }
    }

    #endregion Helper Classes
}
