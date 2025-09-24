using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    [Info("ZoneAutoDoors", "MJSU", "0.0.1")]
    [Description("Force auto doors in set zones")]
    class ZoneAutoDoors : RustPlugin
    {
        [PluginReference]
        Plugin ZoneManager;

        #region Class Fields
        private StoredData _storedData; //Plugin Data
        private PluginConfig _pluginConfig; //Plugin Config

        private const string UsePermission = "ZoneAutoDoors.use";
        #endregion

        #region Setup & Loading
        // ReSharper disable once UnusedMember.Local
        private void Loaded()
        {
            LoadLang();

            _pluginConfig = ConfigOrDefault(Config.ReadObject<PluginConfig>());
            if (_pluginConfig.Prefix == null) PrintError("Loading config file failed. Using default config");
            else Config.WriteObject(_pluginConfig, true);

            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("ZoneAutoDoors");

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
                ["AddSyntax"] = "Invalid add syntax - /zad add ZoneId seconds",
                ["RemoveSyntax"] = "Invalid remove syntax - /zad remove ZoneId",
                ["InvalidSeconds"] = "The time you set of {0} is not valid",
                ["Added"] = "You have added zone {0} with a delay of {1} seconds",
                ["Removed"] = "You have removed zone {0}",
                ["NotAZone"] = "The zone id {0} is an an active zone"
            }, this);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// load the default config for this plugin
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        protected override void LoadDefaultConfig()
        {
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
                Prefix = config?.Prefix ?? "[<color=yellow>Zone Auto Doors</color>]",
            };
        }

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            if (ZoneManager == null)
            {
                PrintWarning("ZoneAutoDoors could not find ZoneManager. ZoneAutoDoors will not work");
            }
        }
        #endregion

        #region Chat Command
        [ChatCommand("zonead")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void ZoneAutoDoorsChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin() && !HasPermission(player, UsePermission))
            {
                PrintToChat(player, Lang("NoPermission", player.UserIDString));
                return;
            }

            if (args.Length < 1)
            {
                SendHelpText(player);
                return;
            }

            switch (args[0].ToLower())
            {
                case "add":
                    HandleAddCommand(player, args);
                    break;

                case "remove":
                    HandleRemoveCommand(player, args);
                    break;

                case "list":
                    HandleListCommand(player);
                    break;

                default:
                    SendHelpText(player);
                    break;
            }

            Interface.Oxide.DataFileSystem.WriteObject("ZoneAutoDoors", _storedData);
        }

        /// <summary>
        /// Handles the chat command for adding auto doors to a zone
        /// </summary>
        /// <param name="player">player calling the command</param>
        /// <param name="args">args for setting up the auto doors</param>
        private void HandleAddCommand(BasePlayer player, string[] args)
        {
            if (args.Length != 3) //make sure we have the correct number of arguments
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("AddSyntax", player.UserIDString)}");
                return;
            }

            float time;
            if (!float.TryParse(args[2], out time) || time <= 0) //failed to parse time or it is <= 0
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("InvalidSeconds", player.UserIDString, args[2])}");
                return;
            }

            if(ZoneManager?.Call("CheckZoneID", args[1]) == null)
            {
                PrintToChat(player, Lang("NotAZone", args[1]));
                return;
            }

            _storedData.ZoneTimes[args[1]] = time;
            PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("Added", player.UserIDString, args[1], args[2])}");
        }

        /// <summary>
        /// Handle the chat command for removing auto doors from a zone
        /// </summary>
        /// <param name="player">player calling the command</param>
        /// <param name="args">args for removing auto doors</param>
        private void HandleRemoveCommand(BasePlayer player, string[] args)
        {
            if (args.Length != 2) //make sure we have the correct number of arguments
            {
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("RemoveSyntax", player.UserIDString)}");
                return;
            }

            if (ZoneManager?.Call("CheckZoneID", args[1]) == null)
            {
                PrintToChat(player, Lang("NotAZone", args[1]));
                return;
            }

            _storedData.ZoneTimes.Remove(args[1]);
            PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("Removed", player.UserIDString, args[1])}");
        }

        /// <summary>
        /// Lists all the zones with auto doors to the player
        /// </summary>
        /// <param name="player"></param>
        private void HandleListCommand(BasePlayer player)
        {
            string message = $"{_pluginConfig.Prefix} Zones with auto doors set:\n";
            foreach(KeyValuePair<string, float> zone in _storedData.ZoneTimes)
            {
                message += $"{zone.Key} with {zone.Value} seconds\n";
            }

            PrintToChat(player, message);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnDoorOpened(Door door, BasePlayer player)
        {
            if (door == null || !door.IsOpen() || door.LookupPrefab().name.Contains("shutter")) return;

            float time = -1;
            foreach (KeyValuePair<string, float> zone in _storedData.ZoneTimes)
            {
                if (ZoneManager?.Call<bool>("isPlayerInZone", zone.Key, player) ?? false)
                {
                    time = zone.Value;
                    break;
                }
            }

            if ((int)time == -1) return;

            timer.Once(time, () =>
            {
                if (!door || !door.IsOpen()) return;

                door.SetFlag(BaseEntity.Flags.Open, false);
                door.SendNetworkUpdateImmediate();
            });
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
        class PluginConfig
        {
            public string Prefix { get; set; }
        }

        class StoredData
        {
            public Hash<string, float> ZoneTimes = new Hash<string, float>();
        }
        #endregion

        #region Help Text
        private void SendHelpText(BasePlayer player)
        {
            PrintToChat(player, $"{_pluginConfig.Prefix} Help Text:\n" +
                                "Allows admins or players with permission to set a zone from Zone Manager to have auto doors" +
                                "Any door in this zone will close after the set number of seconds" +
                                " - <color=yellow>/zonead</color> - to view this help text\n" +
                                " - <color=yellow>/zonead add zoneId seconds</color> - Will set the auto doors for the given zone id to the number of seconds" +
                                " - <color=yellow>/zonead remove zoneId - Will remove auto doors for the given zone" +
                                " - <color=yellow>/zonead list - will list the zone ids and seconds for each zone with auto doors" +
                                "Note: If you wish to update a zone the run the /zonead add command again for the given zone");
        }
        #endregion
    }
}
