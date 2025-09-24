using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Ext.Discord.Entities.Messages.Embeds;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities.Gatway.Events;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Entities.Gatway;

namespace Oxide.Plugins
{
    [Info("Discord Auth Logger", "YaMang -w-", "1.0.2")]
    [Description("Discord Auth, Core Linked Message Alarm")]
    public class DiscordAuthLogger : RustPlugin
    {
        #region Class Fields
        [DiscordClient] private DiscordClient _client;
        private DiscordGuild _guild;
        #endregion


        [HookMethod(DiscordHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            Puts($"Bot connected to:{ready.Guilds.FirstOrDefault().Value.Name}");

            DiscordGuild guild = null;
            if (ready.Guilds.Count == 1)
            {
                guild = ready.Guilds.Values.FirstOrDefault();
            }

            if (guild == null)
            {
                guild = ready.Guilds[_config.guildId];
            }

            if (guild == null)
            {
                PrintError("Failed to find a matching guild for the Discord Server Id. " +
                           "Please make sure your guild Id is correct and the bot is in the discord server.");
                return;
            }

            _guild = guild;
        }
        #region Hook
        private void OnServerInitialized()
        {
            if (string.IsNullOrEmpty(_config.token))
            {
                PrintWarning("Need a discord bot token\nNot loaded");
                return;
            }
            DiscordSettings discordSettings = new DiscordSettings();
            discordSettings.ApiToken = _config.token;
            discordSettings.Intents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.DirectMessages | GatewayIntents.GuildMessages;
            //discordSettings.LogLevel = DiscordLogLevel.Verbose; //remove the first comment for debugging
            _client.Connect(discordSettings);

        }
        void OnDiscordPlayerLinked(IPlayer player, DiscordUser discord)
        {
            webrequest.Enqueue(SteamApi(player.Id), null, (code, response) =>
            {
                var summaries = Deserialise<Summaries>(response);

                if (summaries == null)
                {
                    PrintWarning($"(Connect Devloper)Api Response Can't Deserialise:\n{response}");
                    return;
                }

                DiscordEmbed embed = new DiscordEmbed();

                embed.Author = new EmbedAuthor(player.Name, $"https://steamcommunity.com/profiles/{player.Id}/", GetAvatarFull(summaries));
                embed.Description = Format(_config.descriptionmsg, player, discord);
                List<EmbedField> fields = new List<EmbedField>();
                fields.Clear();
                foreach (var field in _config.EmbedsFields)
                {
                    var value = Format(field.Value, player, discord);

                    fields.Add(new EmbedField()
                    {
                        Name = field.Name,
                        Value = value,
                        Inline = field.Inline
                    });
                }

                embed.Fields = fields;
                if (!string.IsNullOrEmpty(_config.footermsg))
                    embed.Footer = new EmbedFooter()
                    {
                        Text = _config.footermsg,
                        IconUrl = _config.footericon
                    };

                _guild.Channels[_config.channelId].CreateMessage(_client, embed);


            }, this, RequestMethod.GET);

        }

        void OnDiscordPlayerUnlinked(IPlayer player, DiscordUser discord)
        {
            Puts($"{player.Name}({player.Id}) has unlinked with discord");
        }

        #endregion

        #region Helper

        private string SteamApi(string id)
        {
            return $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={_config.steamApiKey}&steamids={id}";
        }

        private string SteamGetAvatarUrl(IPlayer player)
        {
            string url = "";

            webrequest.Enqueue(string.Format(SteamApi(player.Id)), null, (code, response) =>
            {
                if (code != 200)
                {
                    PrintWarning($"Steam Api Can't {response}");
                    return;
                }

                var summaries = Deserialise<Summaries>(response);
                url = GetAvatarFull(summaries);
            }, this, RequestMethod.GET);

            return url;
        }
        private string Format(string msg, IPlayer player, DiscordUser discord)
        {
            msg = msg
                .Replace("{steamId}", player.Id)
                .Replace("{steamName}", player.Name)
                .Replace("{steamGetAvatarUrl}", SteamGetAvatarUrl(player))
                .Replace("{discordId}", discord.Id)
                .Replace("{discordName}", discord.GetFullUserName.Replace("#0", ""))
                .Replace("{discordGetAvatarUrl}", discord.GetAvatarUrl)
                .Replace("{datetime}", DateTime.Now.ToString(_config.TimeFormat));

            return msg;
        }
        #endregion
        #region Config
        private ConfigData _config;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "Discord Token (Auth, Core Same Token recommended)")] public string token { get; set; }
            [JsonProperty(PropertyName = "Steam API Key (https://steamcommunity.com/dev/apikey)")] public string steamApiKey { get; set; }
            [JsonProperty(PropertyName = "Description")] public string descriptionmsg { get; set; }
            [JsonProperty(PropertyName = "Footer Message")] public string footermsg { get; set; }
            [JsonProperty(PropertyName = "Footer Icon")] public string footericon { get; set; }
            [JsonProperty(PropertyName = "Embed Field")] public List<EmbedField> EmbedsFields { get; set; }
            [JsonProperty(PropertyName = "Time Formatting")] public string TimeFormat { get; set; }
            [JsonProperty(PropertyName = "Guild Id")] public Snowflake guildId { get; set; }
            [JsonProperty(PropertyName = "Channel Logger Id")] public Snowflake channelId { get; set; }

            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<ConfigData>();

            if (_config.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(_config, true);
        }

        protected override void LoadDefaultConfig() => _config = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                token = "",
                steamApiKey = "",
                descriptionmsg = "",
                footermsg = "",
                footericon = "",
                EmbedsFields = new List<EmbedField>()
                {
                    new EmbedField()
                    {
                        Name = "Steam",
                        Value = "[{steamName}](https://steamcommunity.com/profiles/{steamId}) ({steamId})"
                    },
                    new EmbedField()
                    {
                        Name = "Discord",
                        Value = "<@{discordId}> ({discordName} | {discordId})"
                    }
                },
                TimeFormat = "MM/dd/yyyy hh:mm:ss tt",
                guildId = new Snowflake(0),
                channelId = new Snowflake(0),
                Version = Version
            };
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");

            _config.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion

        #region SteamAPI

        private T Deserialise<T>(string json) => JsonConvert.DeserializeObject<T>(json);

        private string GetAvatarFull(Summaries s) => s.Response.Players[0].AvatarFull;
        private class Summaries
        {
            [JsonProperty("response")]
            public Content Response;

            public class Content
            {
                [JsonProperty("players")]
                public Player[] Players;

                public class Player
                {
                    [JsonProperty("steamid")]
                    public string SteamId;

                    [JsonProperty("communityvisibilitystate")]
                    public int CommunityVisibilityState;

                    [JsonProperty("profilestate")]
                    public int ProfileState;

                    [JsonProperty("personaname")]
                    public string PersonaName;

                    [JsonProperty("lastlogoff")]
                    public double LastLogOff;

                    [JsonProperty("commentpermission")]
                    public int CommentPermission;

                    [JsonProperty("profileurl")]
                    public string ProfileUrl;

                    [JsonProperty("avatar")]
                    public string Avatar;

                    [JsonProperty("avatarmedium")]
                    public string AvatarMedium;

                    [JsonProperty("avatarfull")]
                    public string AvatarFull;

                    [JsonProperty("personastate")]
                    public int PersonaState;

                    [JsonProperty("realname")]
                    public string RealName;

                    [JsonProperty("primaryclanid")]
                    public string PrimaryClanId;

                    [JsonProperty("timecreated")]
                    public double TimeCreated;

                    [JsonProperty("personastateflags")]
                    public int PersonaStateFlags;

                    [JsonProperty("loccountrycode")]
                    public string LocCountryCode;

                    [JsonProperty("locstatecode")]
                    public string LocStateCode;
                }
            }
        }
        #endregion
    }
}