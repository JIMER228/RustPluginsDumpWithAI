using System.Collections.Generic;
using System.Reflection;
using Oxide.Core;
using UnityEngine;

//Todo: Add Clans support
//Todo: Add RustIO friends support

// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    // ReSharper disable once UnusedMember.Global
    [Info("AutoCodeLock", "MJSU", "0.0.1")]
    [Description("Adds a codelock to a placed door or storage container and set the code")]
    class AutoCodeLock : RustPlugin
    {
        #region Class Fields
        private readonly FieldInfo _codelockField = typeof(CodeLock).GetField("code", BindingFlags.NonPublic | BindingFlags.Instance);
        readonly FieldInfo _whitelistField = typeof(CodeLock).GetField("whitelistPlayers", BindingFlags.Instance | BindingFlags.NonPublic);

        private StoredData _storedData;
        private PluginConfig _pluginConfig; //Config File

        private const string UsePermission = "autocodelock.use";
        private const string CodeLockPrefabLocation = "assets/prefabs/locks/keypad/lock.code.prefab";
        private bool _serverInitialized;
        #endregion

        #region Loading & Setup
        // ReSharper disable once UnusedMember.Local
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Plugin has been loaded
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private void Loaded()
        {
            LoadLang();

            _pluginConfig = ConfigOrDefault(Config.ReadObject<PluginConfig>());
            if (_pluginConfig.Prefix == null) PrintError("Loading config file failed. Using default config");
            else Config.WriteObject(_pluginConfig, true);

            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("AutoCodeLock");

            permission.RegisterPermission(UsePermission, this);
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
                ["Disabled"] = "You have disabled Auto Code Lock\n To set again type {0} code",
                ["InvalidCode"] = "Your code '{0}' is not valid",
                ["CodeSet"] = "You have set your {0} codelock code to {1}",
                ["HowToSet"] = "To set your codelock code type {0} 'code' Ex: {0} 1234",
                ["CanNotAfford"] = "You can not afford to use Auto Code lock",
                ["ParseFailed"] = "Your code of {0} failed to be parse correctly. Code will be not bet on the door"
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
                Prefix = config?.Prefix ?? "[<color=yellow>Auto Code Lock</color>]",
                UseCost = config?.UseCost ?? true,
                UseItemCost = config?.UseItemCost ?? true,
                ItemCostList = config?.ItemCostList ?? new List<Hash<string, int>> { new Hash<string, int> { ["lock.code"] = 1 }, new Hash<string, int> { ["wood"] = 400, ["metal.fragments"] = 100 } },
                AllowedDoors = new AllowedDoors
                {
                    All = config?.AllowedDoors?.All ?? false,
                    CellGate = config?.AllowedDoors?.CellGate ?? true,
                    HighExternalStoneGates = config?.AllowedDoors?.HighExternalStoneGates ?? true,
                    HighExternalWoodGate = config?.AllowedDoors?.HighExternalWoodGate ?? true,
                    LadderHatch = config?.AllowedDoors?.LadderHatch ?? true,
                    SheetMetalDoor = config?.AllowedDoors?.SheetMetalDoor ?? true,
                    SheetMetalDoubleDoor = config?.AllowedDoors?.SheetMetalDoubleDoor ?? true,
                    ShopFront = config?.AllowedDoors?.ShopFront ?? true,
                    Shutter = config?.AllowedDoors?.Shutter ?? false,
                    TopTierDoor = config?.AllowedDoors?.TopTierDoor ?? true,
                    TopTierDoubleDoor = config?.AllowedDoors?.TopTierDoubleDoor ?? true,
                    WoodenDoor = config?.AllowedDoors?.WoodenDoor ?? true,
                    WoodenDoubleDoor = config?.AllowedDoors?.WoodenDoubleDoor ?? true,
                },
                AllowedStorageContainers = new AllowedStorageContainers
                {
                    All = config?.AllowedStorageContainers?.All ?? false,
                    AutoTurret = config?.AllowedStorageContainers?.AutoTurret ?? false,
                    Campfire = config?.AllowedStorageContainers?.Campfire ?? false,
                    CeilingLight = config?.AllowedStorageContainers?.CeilingLight ?? false,
                    FishTrap = config?.AllowedStorageContainers?.FishTrap ?? false,
                    Furnace = config?.AllowedStorageContainers?.Furnace ?? false,
                    JackOLantern = config?.AllowedStorageContainers?.JackOLantern ?? false,
                    Lantern = config?.AllowedStorageContainers?.Lantern ?? false,
                    LargeFurnace = config?.AllowedStorageContainers?.LargeFurnace ?? false,
                    LargeStocking = config?.AllowedStorageContainers?.LargeStocking ?? false,
                    LargeWoodenBox = config?.AllowedStorageContainers?.LargeWoodenBox ?? true,
                    Refinery = config?.AllowedStorageContainers?.Refinery ?? false,
                    RepairBench = config?.AllowedStorageContainers?.RepairBench ?? false,
                    ResearchTable = config?.AllowedStorageContainers?.ResearchTable ?? false,
                    SmallStash = config?.AllowedStorageContainers?.SmallStash ?? false,
                    SmallStocking = config?.AllowedStorageContainers?.SmallStocking ?? false,
                    WaterBarrel = config?.AllowedStorageContainers?.WaterBarrel ?? false,
                    WoodenBox = config?.AllowedStorageContainers?.WoodenBox ?? false
                }
            };
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Used to mark when the server has finished loading
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            _serverInitialized = true;
        }
        #endregion

        #region Chat Commands
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Chat command to set / remove the players door codelock code
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        /// ////////////////////////////////////////////////////////////////////////
        [ChatCommand("adc")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void DoorCodeLockChatCommand(BasePlayer player, string command, string[] args)
        {
            HandleChatCommand(player, command, args, _storedData.DoorCode);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Chat command to set / remove the players storage codelock code
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        /// ////////////////////////////////////////////////////////////////////////
        [ChatCommand("asc")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void StorageContainerChatCommand(BasePlayer player, string command, string[] args)
        {
            HandleChatCommand(player, command, args, _storedData.StorageCodes);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Handles the proccessing of both chat commands
        /// </summary>
        /// <param name="player">player running the command</param>
        /// <param name="command">the command that was typed</param>
        /// <param name="args">args passed by the player</param>
        /// <param name="codeStorage">which Data hash the code belongs too</param>
        /// ////////////////////////////////////////////////////////////////////////
        private void HandleChatCommand(BasePlayer player, string command, string[] args, Hash<ulong, string> codeStorage)
        {
            if (!HasPermission(player, UsePermission)) //Make sure player has permission
            {
                PrintToChat(player, Lang("NoPermission", player.UserIDString));
                return;
            }

            switch (args.Length)
            {
                //Disable set autocode
                case 0:
                    codeStorage[player.userID] = null;
                    PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("Disabled", player.UserIDString, command)}");
                    break;

                //Set autocode
                case 1:
                    if (!ValidCode(args[0]))
                    {
                        PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("InvalidCode", player.UserIDString, args[0])}");
                        return;
                    }
                    codeStorage[player.userID] = ParseToSaveFormat(args[0]);
                    PrintToChat(player,
                        command.Equals("adc")
                            ? $"{_pluginConfig.Prefix} {Lang("CodeSet", player.UserIDString, "door", args[0])}"
                            : $"{_pluginConfig.Prefix} {Lang("CodeSet", player.UserIDString, "storage", args[0])}");
                    break;

                //How to use AutoCodeLock
                default:
                    PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("HowToSet", player.UserIDString, command)}");
                    break;
            }

            Interface.Oxide.DataFileSystem.WriteObject("AutoCodeLock", _storedData);
        }
        #endregion

        #region Oxide Hook
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Oxide hook to detect when a door or storage container is placed
        /// </summary>
        /// <param name="entity"></param>
        /// ////////////////////////////////////////////////////////////////////////
        /// // ReSharper disable once UnusedMember.Local
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!_serverInitialized) return; //Server has not finished starting yet. Used to prevent this code from running when the server is starting up

            Door door = entity as Door;
            StorageContainer container = entity as StorageContainer;
            if (door == null && container == null) return; //Not a door or container

            if (!AllowedEntity(door, container)) return; //Not allowed to put a code lock on that entity

            BasePlayer player = BasePlayer.FindByID(door != null ? door.OwnerID : container.OwnerID);
            if (player == null) return; //Could not find owner

            string code = SaveFormatToCode(door != null ? _storedData.DoorCode[player.userID] : _storedData.StorageCodes[player.userID]);
            if (code == null) return; //Player has not set their autocodelock code yet

            if (!HandleCost(player)) return; //Player cannot afford

            BaseEntity spawnedEntity = door != null ? (BaseEntity) door : (BaseEntity)container;
            
            AddLockToEntity(player, spawnedEntity, code); //Add the lock to the entity
        }
        #endregion

        #region Can Afford & Take Cost
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Determines if that player can afford and takes the cost from the player.
        /// </summary>
        /// <param name="player"></param>
        /// <returns>True if the player can afford, false otherwise</returns>
        /// ////////////////////////////////////////////////////////////////////////
        private bool HandleCost(BasePlayer player)
        {
            int index;
            if (!CanAfford(player, out index)) //Player cannot afford
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("CanNotAfford", player.UserIDString)}");
                return false;
            }

            if (index != -1) //The code lock costs items to place
            {
                TakeCost(player, index);
            }

            return true;
        }
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Checks if the player can afford to have the codelock added
        /// </summary>
        /// <param name="player">player to check cost for</param>
        /// <param name="costIndex">the index of the list of costs the player can afford</param>
        /// <returns>true if the player can afford, false otherwise</returns>
        /// ////////////////////////////////////////////////////////////////////////
        private bool CanAfford(BasePlayer player, out int costIndex)
        {
            costIndex = -1;
            if (!_pluginConfig.UseCost) return true; //Plugin doesn't use cost

            if (_pluginConfig.UseItemCost) //Plugin uses cost
            {
                bool canAfford = true; //Determines if the player can afford

                for (int index = 0; index < _pluginConfig.ItemCostList.Count; index++)
                {
                    canAfford = true; //Set to true every loop
                    foreach (KeyValuePair<string, int> item in _pluginConfig.ItemCostList[index])
                    {
                        Item playerItem = player.inventory.FindItemID(item.Key);

                        if (playerItem == null || playerItem.amount < item.Value) //If the player doesnt have the item or cannot afford
                        {
                            canAfford = false;
                            break; //break out of the inner loop
                        }
                    }

                    if (canAfford) //Made it through a group of costs and has the required items
                    {
                        costIndex = index; //index the player can afford
                        break; //break out of the outer loop
                    }
                }

                if (!canAfford) return false; //Player could not afford the items
            }

            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Take the cost of the CodeLock from the player
        /// </summary>
        /// <param name="player">player to take the coset from</param>
        /// <param name="costIndex">index in the item list the player can afford and to take the items from</param>
        /// ////////////////////////////////////////////////////////////////////////
        private void TakeCost(BasePlayer player, int costIndex)
        {
            if (!_pluginConfig.UseCost) return; //Plugin is not using cost

            if (_pluginConfig.UseItemCost) //Plugin is using items
            {
                Hash<string, int> items = _pluginConfig.ItemCostList[costIndex]; //Hash of the items the player can afford
                List<Item> collection = new List<Item>();

                foreach (KeyValuePair<string, int> item in items) //Loop over the items and collect them from the player
                {
                    ItemDefinition itemToTake = ItemManager.FindItemDefinition(item.Key);
                    player.Command("note.inv ", itemToTake.itemid, item.Value * -1f);
                    player.inventory.Take(collection, itemToTake.itemid, item.Value);
                }
            }
        }
        #endregion

        #region Code Lock Allowed on Entity
        private bool AllowedEntity(Door door, StorageContainer container)
        {
            if (door != null)
            {
                if (_pluginConfig.AllowedDoors.All) return true;

                switch (door.LookupPrefab().name)
                {
                    case "door.hinged.wood":
                        return _pluginConfig.AllowedDoors.WoodenDoor;

                    case "door.hinged.metal":
                        return _pluginConfig.AllowedDoors.SheetMetalDoor;

                    case "door.hinged.toptier":
                        return _pluginConfig.AllowedDoors.TopTierDoor;

                    case "door.double.hinged.wood":
                        return _pluginConfig.AllowedDoors.WoodenDoubleDoor;

                    case "door.double.hinged.metal":
                        return _pluginConfig.AllowedDoors.SheetMetalDoubleDoor;

                    case "door.double.hinged.toptier":
                        return _pluginConfig.AllowedDoors.TopTierDoubleDoor;

                    case "shutter.wood.a":
                        return _pluginConfig.AllowedDoors.Shutter;

                    case "floor.ladder.hatch":
                        return _pluginConfig.AllowedDoors.LadderHatch;

                    case "wall.frame.shopfront":
                        return _pluginConfig.AllowedDoors.ShopFront;

                    case "wall.frame.cell.gate":
                        return _pluginConfig.AllowedDoors.CellGate;

                    case "gates.external.high.wood":
                        return _pluginConfig.AllowedDoors.HighExternalWoodGate;

                    case "gates.external.high.stone":
                        return _pluginConfig.AllowedDoors.HighExternalWoodGate;
                }
            }

            if (container != null)
            {
                if (_pluginConfig.AllowedStorageContainers.All) return true;

                switch (container.LookupPrefab().name)
                {
                    case "woodbox_deployed":
                        return _pluginConfig.AllowedStorageContainers.WoodenBox;

                    case "box.wooden.large":
                        return _pluginConfig.AllowedStorageContainers.LargeWoodenBox;

                    case "campfire":
                        return _pluginConfig.AllowedStorageContainers.Campfire;

                    case "furnace":
                        return _pluginConfig.AllowedStorageContainers.Furnace;

                    case "furnace.large":
                        return _pluginConfig.AllowedStorageContainers.LargeFurnace;

                    case "refinery_small_deployed":
                        return _pluginConfig.AllowedStorageContainers.Refinery;

                    case "stocking_small_deployed":
                        return _pluginConfig.AllowedStorageContainers.SmallStocking;

                    case "stocking_large_deployed":
                        return _pluginConfig.AllowedStorageContainers.LargeStocking;

                    case "repairbench_deployed":
                        return _pluginConfig.AllowedStorageContainers.RepairBench;

                    case "researchtable_deployed":
                        return _pluginConfig.AllowedStorageContainers.ResearchTable;

                    case "lantern.deployed":
                        return _pluginConfig.AllowedStorageContainers.Lantern;

                    case "WaterBarrel":
                        return _pluginConfig.AllowedStorageContainers.WaterBarrel;

                    case "autoturret_deployed":
                        return _pluginConfig.AllowedStorageContainers.AutoTurret;

                    case "jackolantern.happy":
                    case "jackolantern.angry":
                        return _pluginConfig.AllowedStorageContainers.JackOLantern;

                    case "survivalfishtrap.deployed":
                        return _pluginConfig.AllowedStorageContainers.FishTrap;

                    case "small_stash_deployed":
                        return _pluginConfig.AllowedStorageContainers.SmallStash;
                }
            }

            return false;
        }
        #endregion

        #region Adding Lock
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Create a CodeLock add the players code to it and add them to the whitelist. Then place the codelock on the door
        /// </summary>
        /// <param name="player">player who placed the door</param>
        /// <param name="door">the door the codelock is going to be attached too</param>
        /// <param name="container">the container the codelock is going to be attached too</param>
        /// ////////////////////////////////////////////////////////////////////////
        private void AddLockToEntity(BasePlayer player, BaseEntity entity, string code)
        {
            //Create a CodeLock
            BaseEntity lockentity = GameManager.server.CreateEntity(CodeLockPrefabLocation, Vector3.zero, new Quaternion());

            lockentity.OnDeployed(entity);

            //Add the player to the codelock whitelist
            List<ulong> whitelist = (List<ulong>)_whitelistField.GetValue(lockentity);
            whitelist.Add(player.userID);
            _whitelistField.SetValue(lockentity, whitelist);

            if (ValidCode(code))
            {
                CodeLock @lock = lockentity.GetComponent<CodeLock>();
                _codelockField.SetValue(@lock, code);
                @lock.SetFlag(BaseEntity.Flags.Locked, true);
            }
            else
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("ParseFailed", player.UserIDString, code)}");
            }

            //Add the codelock to the door
            if (!lockentity) return;
            lockentity.gameObject.Identity();
            lockentity.SetParent(entity, "lock");
            lockentity.Spawn();
            entity.SetSlot(BaseEntity.Slot.Lock, lockentity);

        }
        #endregion

        #region Code Handling
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Makes sure the code entered by the player is a valid CodeLock code
        /// </summary>
        /// <param name="code">code passed in by the player</param>
        /// <returns>true if the code is valid, false otherwise</returns>
        /// ////////////////////////////////////////////////////////////////////////
        private bool ValidCode(string code)
        {
            int codeNum;
            if (!int.TryParse(code, out codeNum)) return false; //try to parse the code to an int
            if (codeNum < 0 || codeNum > 9999) return false; //make sure the code is not negative or greater than the the biggest code of 9999
            return true;
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Converts the code to the save format. Use to make it hard for server owners to see players codes
        /// </summary>
        /// <param name="code">code to be converted</param>
        /// <returns>code in the save format</returns>
        /// ////////////////////////////////////////////////////////////////////////
        private string ParseToSaveFormat(string code)
        {
            if (code.Length != 4) return null;
            code = SwapCodeCharacter(SwapCodeCharacter(SwapCodeCharacter(SwapCodeCharacter(code, 0, 2), 1, 3), 1, 0), 2, 1);
            int codeNum;
            if (!int.TryParse(code, out codeNum)) return null; //try to parse the code to an int
            // ReSharper disable once InterpolatedStringExpressionIsNotIFormattable
            return $"{codeNum:X4}";
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// converts the code from the save format to the code for the player
        /// </summary>
        /// <param name="code">encoded code</param>
        /// <returns>code in string format</returns>
        /// ////////////////////////////////////////////////////////////////////////
        private string SaveFormatToCode(string code)
        {
            string convertedCode = null;
            int val;
            if (int.TryParse(code, System.Globalization.NumberStyles.HexNumber, null, out val))
            {
                convertedCode = SwapCodeCharacter(SwapCodeCharacter(SwapCodeCharacter(SwapCodeCharacter(val.ToString("D4"), 2, 1), 1, 0), 1, 3), 0, 2);
            }

            return convertedCode;
        }

        private string SwapCodeCharacter(string value, int index1, int index2)
        {
            char[] array = value.ToCharArray();
            char temp = array[index1];
            array[index1] = array[index2];
            array[index2] = temp;
            return new string(array);
        }
        #endregion

        #region Helper Methods
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
        // ReSharper disable once ClassNeverInstantiated.Local
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Plugin Config
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private class PluginConfig
        {
            public string Prefix { get; set; }
            public bool UseCost { get; set; }
            public bool UseItemCost { get; set; }
            public List<Hash<string, int>> ItemCostList { get; set; }
            public AllowedDoors AllowedDoors { get; set; }
            public AllowedStorageContainers AllowedStorageContainers { get; set; }
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Config Class for doors
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private class AllowedDoors
        {
            public bool All { get; set; }
            public bool WoodenDoor { get; set; }
            public bool SheetMetalDoor { get; set; }
            public bool TopTierDoor { get; set; }
            public bool WoodenDoubleDoor { get; set; }
            public bool SheetMetalDoubleDoor { get; set; }
            public bool TopTierDoubleDoor { get; set; }
            public bool Shutter { get; set; }
            public bool LadderHatch { get; set; }
            public bool CellGate { get; set; }
            public bool ShopFront { get; set; }
            public bool HighExternalWoodGate { get; set; }
            public bool HighExternalStoneGates { get; set; }
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Config class for storage containers
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        private class AllowedStorageContainers
        {
            public bool All { get; set; }
            public bool WoodenBox { get; set; }
            public bool LargeWoodenBox { get; set; }
            public bool Campfire { get; set; }
            public bool Furnace { get; set; }
            public bool LargeFurnace { get; set; }
            public bool Refinery { get; set; }
            public bool SmallStocking { get; set; }
            public bool LargeStocking { get; set; }
            public bool RepairBench { get; set; }
            public bool ResearchTable { get; set; }
            public bool Lantern { get; set; }
            public bool CeilingLight { get; set; }
            public bool WaterBarrel { get; set; }
            public bool AutoTurret { get; set; }
            public bool JackOLantern { get; set; }
            public bool FishTrap { get; set; }
            public bool SmallStash { get; set; }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Data File
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        class StoredData
        {
            public Hash<ulong, string> DoorCode = new Hash<ulong, string>();
            public Hash<ulong, string> StorageCodes = new Hash<ulong, string>();
        }
        #endregion

        #region Help Text
        private void SendHelpText(BasePlayer player)
        {
            PrintToChat(player, @"[<color=yellow>Auto CodeLock</color>] Help Text:\n
                                Allows players to have a Code Lock added when a player places a door\n
                                The code on the Code Lock is set, the player is added to the door, and the lock is locked\n
                                <color=yellow>/ac</color> - remove your set CodeLock code\n
                                <color=yellow>/ac [code] - ex: /ac 1234 - sets your CodeLock Code to 1234</color>");
        }
        #endregion
    }
}
