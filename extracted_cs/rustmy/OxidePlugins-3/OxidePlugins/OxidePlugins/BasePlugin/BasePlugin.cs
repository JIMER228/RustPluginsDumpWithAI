using Oxide.Core;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("BasePlugin", "MJSU", "0.0.1")]
    [Description("Is a plugin")]
    class BasePlugin : RustPlugin
    {
        #region Class Fields
        private StoredData _storedData; //Plugin Data
        private PluginConfig _pluginConfig; //Plugin Config

        private const string UsePermission = "baseplugin.use";
        #endregion
        
        #region Setup & Loading
        private void Loaded()
        {
            LoadLang();

            _pluginConfig = ConfigOrDefault(Config.ReadObject<PluginConfig>());
            if (_pluginConfig.Prefix == null) PrintError("Loading config file failed. Using default config");
            else Config.WriteObject(_pluginConfig, true);

            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("Plugin");

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
                ["NoPermission"] = "You do not have permission to use this command"
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
                Prefix = config?.Prefix ?? "[<color=yellow>Plugin</color>]",
            };
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

        }
        #endregion
    }
}
