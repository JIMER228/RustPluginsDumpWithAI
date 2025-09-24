using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.DiscordObjects;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using System;
using Oxide.Core.Plugins;
using Random = System.Random;

namespace Oxide.Plugins
{
    [Info("Discord Status", "Tricky", "2.0.1")]
    [Description("Shows server information as a discord bot status")]

    public class DiscordStatus : CovalencePlugin
    {
        #region Declared
        [DiscordClient]
        private DiscordClient Client;

        Random random = new Random();
        #endregion

        #region Plugin References
        [PluginReference]
        private Plugin DiscordAuth;
        #endregion

        #region Config
        Configuration config;

        class Configuration
        {
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string BotToken = string.Empty;

            [JsonProperty(PropertyName = "Update Interval (Seconds)")]
            public int UpdateInterval = 5;

            [JsonProperty(PropertyName = "Randomize Status")]
            public bool Randomize = false;

            [JsonProperty(PropertyName = "Status Type (Game/Stream/Listen/Watch)")]
            public string StatusType = "Game";

            [JsonProperty(PropertyName = "Status", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Status = new List<string>
            {
                "{players.online} / {server.maxplayers} Online!",
                "{server.entities} Entities",
                "{players.sleepers} Sleepers!",
                "{players.authenticated} Linked Account(s)"
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) throw new Exception();
            }
            catch
            {
                Config.WriteObject(config, false, $"{Interface.Oxide.ConfigDirectory}/{Name}.jsonError");
                PrintError("The configuration file contains an error and has been replaced with a default config.\n" +
                           "The error configuration file was saved in the .jsonError extension");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Oxide Hooks
        private void OnServerInitialized()
        {
            if (config.BotToken == string.Empty)
                return;

            Discord.CreateClient(this, config.BotToken);

            timer.Every(config.UpdateInterval, () => UpdateStatus());
            timer.Every(300, () => Reload());
        }

        private void Unload() => Discord.CloseClient(Client);
        #endregion

        #region Status Update
        private ActivityType GetStatusType()
        {
            if (config.StatusType != "Game" && config.StatusType != "Stream" && config.StatusType != "Listen" && config.StatusType != "Watch")
                PrintError($"Unknown Status Type '{config.StatusType}'");

            switch (config.StatusType)
            {
                case "Game":
                    return ActivityType.Game;
                case "Stream":
                    return ActivityType.Streaming;
                case "Listen":
                    return ActivityType.Listening;
                case "Watch":
                    return ActivityType.Watching;
                default:
                    return default(ActivityType);
            }
        }

        private void UpdateStatus()
        {
            Client.UpdateStatus(new Presence()
            {
                Game = new Ext.Discord.DiscordObjects.Game()
                {
                    Name = Format(config.Randomize ? config.Status[random.Next(config.Status.Count)] : config.Status[0]),
                    Type = GetStatusType()
                }
            });
        }
        #endregion

        #region Helpers
        private string Format(string message)
        {
            var placeholders = new Dictionary<string, string>
            {
                {"{guild.name}", Client.DiscordServer.name },
                {"{members.total}", Client.DiscordServer.member_count.ToString() },
                {"{channels.total}", Client.DiscordServer.channels.Count.ToString() },
                {"{server.hostname}", server.Name },
                {"{server.maxplayers}", server.MaxPlayers.ToString() },
                {"{players.online}", players.Connected.Count().ToString() },
                {"{players.authenticated}", DiscordAuth != null ? GetAuthCount().ToString() : "{unknown}" },
#if RUST
                {"{server.ip}", ConVar.Server.ip },
                {"{server.port}", ConVar.Server.port.ToString() },
                {"{server.entities}", BaseNetworkable.serverEntities.Count.ToString() },
                {"{server.worldsize}", ConVar.Server.worldsize.ToString() },
                {"{server.seed}", ConVar.Server.seed.ToString() },
                {"{players.queued}", ConVar.Admin.ServerInfo().Queued.ToString() },
                {"{players.joining}", ConVar.Admin.ServerInfo().Joining.ToString() },
                {"{players.sleepers}", BasePlayer.sleepingPlayerList.Count.ToString() },
                {"{players.total}", players.Connected.Count() + BasePlayer.sleepingPlayerList.Count.ToString() }
#endif
            };

            placeholders.ToList().ForEach(placeholder =>
            {
                if (message.Contains(placeholder.Key))
                    message = message.Replace(placeholder.Key, placeholder.Value);
            });

            return message;
        }

        private int GetAuthCount() => (int)DiscordAuth.Call("API_GetAuthCount");

        private void Reload() => server.Command($"oxide.reload {Name}");
        #endregion
    }
}
