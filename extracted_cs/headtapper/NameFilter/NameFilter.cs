using System;
using Oxide.Core.Plugins;
using System.Linq;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("NameFilter", "headtapper", "1.0.1")]
    [Description("Allow connections from players with usernames containing only English alphanumeric characters and whitelisted symbols.")]

    public class NameFilter : RustPlugin
    {
        #region Initialization

        private const string NameFilterBypassPermission = "namefilter.bypass";

        private void Init()
        {
            permission.RegisterPermission(NameFilterBypassPermission, this);
        }

        #endregion

        #region Configuration

        private PluginConfig config;

        private class PluginConfig
        {
            public string AdditionalCharacterWhitelist { get; set; }
        }

        private PluginConfig GetDefaultConfigValues()
        {
            return new PluginConfig
            {
                AdditionalCharacterWhitelist = " "
            };
        }

        private void SaveConfig() => Config.WriteObject(config, true);

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfigValues();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            try
            {
                base.LoadConfig();
                config = Config.ReadObject<PluginConfig>();
                SaveConfig();
            }
            catch (Exception ex)
            {
                LoadDefaultConfig();
            }
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["KickMessage"] = "Your name must only contain English alphanumeric characters.",
            }, this);
        }

        #endregion

        #region Hooks

        private object CanUserLogin(string username, string userid, string ip)
        {
            if (!username.All(c => CheckCharacter(c)) && !permission.UserHasPermission(userid, NameFilterBypassPermission))
                return lang.GetMessage("KickMessage", this, userid);
            return null;
        }

        #endregion

        #region Functions

        private bool CheckCharacter(char character) => char.IsLetterOrDigit(character) || config.AdditionalCharacterWhitelist.Contains(character);

        #endregion
    }
}

