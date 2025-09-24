using System;
using System.Collections.Generic;

// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    [Info("HeliTargetInfo", "MJSU", "0.0.1")]
    [Description("Displays who the heli locks onto")]
    // ReSharper disable once UnusedMember.Global
    class HeliTargetInfo : RustPlugin
    {
        private PluginConfig _pluginConfig;
        private readonly Hash<ulong, DateTime> _heliTargetCooldown = new Hash<ulong, DateTime>();

        #region Plugin Loading & Setup
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Called when the plugin is loading
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        private void Loaded()
        {
            _pluginConfig = ConfigOrDefault(Config.ReadObject<PluginConfig>());
            if (_pluginConfig.Prefix == null) PrintError("Loading config file failed. Using default config");
            else Config.WriteObject(_pluginConfig, true);

            lang.RegisterMessages(new Dictionary<string, string>()
            {
                ["Locked"] = "<color=yellow>{0}</color> the heli has locked onto you."
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
                Prefix = config?.Prefix ?? "[<color=yellow>Heli Target Info</color>]",
                Cooldown = config?.Cooldown ?? new TimeSpan(0, 0, 5, 0)
            };
        }
        #endregion

        #region Oxide Hooks      
        //////////////////////////////////////////////////////////////////////////////////////  
        /// <summary>
        /// Called when the heli locks onto a player
        /// </summary>
        /// <param name="turret"></param>
        /// <param name="entity"></param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        void OnHelicopterTarget(HelicopterTurret turret, BaseCombatEntity entity)
        {
            BasePlayer player = entity.ToPlayer();
            if (player == null) return;
            if (!_heliTargetCooldown.ContainsKey(player.userID) || (_heliTargetCooldown.ContainsKey(player.userID) && _heliTargetCooldown[player.userID] < DateTime.Now))
            {
                PrintToChat($"{_pluginConfig.Prefix}  {Lang("Locked", player.UserIDString, player.displayName)}");
                _heliTargetCooldown[player.userID] = DateTime.Now + _pluginConfig.Cooldown;
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
        #endregion

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Config for the plugin
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        class PluginConfig
        {
            public string Prefix { get; set; }
            public TimeSpan Cooldown { get; set; }
        }
    }
}
