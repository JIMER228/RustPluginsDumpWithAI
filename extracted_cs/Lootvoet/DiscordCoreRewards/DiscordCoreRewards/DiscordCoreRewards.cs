using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Discord Core Rewards", "KL", "0.0.2")]
    [Description("Gives a reward to players who linked with discord")]
    internal class DiscordCoreRewards : CovalencePlugin
    {
        #region Fields & Properties
        [PluginReference] private Plugin DiscordCore;
        private ConfigFile _config;
        #endregion

        #region Setup & Loading
        protected override void LoadDefaultConfig()
        {
            _config = ConfigFile.DefaultConfig();
            PrintWarning("Default configuration has been loaded.");
        }

        private void OnServerInitialized()
        {
            if (DiscordCore == null)
            {
                PrintError("Missing plugin dependency DiscordCore: https://umod.org/plugins/discord-core. Unloading plugin");
                return;
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _config = Config.ReadObject<ConfigFile>();
            if (_config == null)
            {
                LoadDefaultConfig();
            }
            Config.WriteObject(_config);
        }
        #endregion

        #region DiscordCore Hooks
        private void OnDiscordCoreJoin(IPlayer player)
        {
            foreach (var command in _config.commands)
            {
                var toExecute = command.Replace("{id}", player.Id);
                server.Command(toExecute);
            }
            player.Message("You have successfully linked your discord account. [#8E44AD]Enjoy your reward[/#]!");
        }
        #endregion

        #region Classes
        private class ConfigFile
        {
            [JsonProperty("Commands to execute when a player is verified. Use {id} for the player's steam id.")]
            public List<string> commands;

            public static ConfigFile DefaultConfig()
            {
                return new ConfigFile
                {
                    commands = new List<string>
                    {
                        "inventory.giveto {id} stones 1000",
                        "inventory.giveto {id} wood 5000"
                    }
                };
            }
        }
        #endregion
    }
}