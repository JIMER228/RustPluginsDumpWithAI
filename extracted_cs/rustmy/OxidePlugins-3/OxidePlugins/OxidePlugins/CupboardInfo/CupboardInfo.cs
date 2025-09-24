using System.Collections.Generic;
using ProtoBuf;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    [Info("CupboardInfo", "MJSU", "0.0.1")]
    [Description("Displays the changes to cupboards to the user")]
    // ReSharper disable once UnusedMember.Global
    class CupboardInfo : RustPlugin
    {
        private PluginConfig _pluginConfig;

        private const string UsePermission = "cupboardinfo.use";

        #region Plugin Loading & Initalizing
        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Plugin is being loaded
        /// </summary>
        /// ///////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        private void Loaded()
        {
            _pluginConfig = ConfigOrDefault(Config.ReadObject<PluginConfig>());
            if (_pluginConfig.Prefix == null) PrintError("Loading config file failed. Using default config");
            else Config.WriteObject(_pluginConfig, true);

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["NoPermission"] = "You do not have permission to use this command",
                ["Cleared"] = "You have successfully cleared the cupboard list",
                ["Authorized"] = "List of users also authorized on this cupboard:",
                ["StillAuthoried"] = "List of users still authorized on this cupboard:"
            }, this);

            permission.RegisterPermission(UsePermission, this);
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
                Prefix = config?.Prefix ?? "[<color=yellow>Cupboard Info</color>]"
            };
        }
        #endregion

        #region Oxide Hooks
        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Called when the player clears a cupboard
        /// </summary>
        /// <param name="privilege"></param>
        /// <param name="player"></param>
        /// ///////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        void OnCupboardClearList(BuildingPrivlidge privilege, BasePlayer player)
        {
            if(HasPermission(player, UsePermission))
                PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("Cleared", player.UserIDString)}");
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Called when a player authorizes on a cupbaord
        /// </summary>
        /// <param name="privilege"></param>
        /// <param name="player"></param>
        /// ///////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        void OnCupboardAuthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            if (privilege.authorizedPlayers.Count == 0) return;
            if (!HasPermission(player, UsePermission)) return;
            DisplayCupboardData(privilege, player, "Authorized");
        }

        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Called when a player defaulthorizes on a cupboard
        /// </summary>
        /// <param name="privilege"></param>
        /// <param name="player"></param>
        /// ///////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        void OnCupboardDeauthorize(BuildingPrivlidge privilege, BasePlayer player)
        {
            if (privilege.authorizedPlayers.Count == 0) return;
            if (!HasPermission(player, UsePermission)) return;
            DisplayCupboardData(privilege, player, "StillAuthoried");
        }
        #endregion

        #region Display Data
        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Displays the cupboard information to the player
        /// </summary>
        /// <param name="privilege"></param>
        /// <param name="player"></param>
        /// <param name="langString">lang key to be used</param>
        /// ///////////////////////////////////////////////////////////////
        private void DisplayCupboardData(BuildingPrivlidge privilege, BasePlayer player, string langString)
        {
            string message = $"{_pluginConfig.Prefix} {Lang(langString, player.UserIDString)}\n";
            foreach (PlayerNameID user in privilege.authorizedPlayers.Where(playerName => playerName.userid != player.userID))
            {
                message += $" - {user.username}\n";
            }
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
        ///////////////////////////////////////////////////////////////
        /// <summary>
        /// Plugins Config
        /// </summary>
        /// ///////////////////////////////////////////////////////////////
        class PluginConfig
        {
            public string Prefix { get; set; }
        }
        #endregion
    }
}
