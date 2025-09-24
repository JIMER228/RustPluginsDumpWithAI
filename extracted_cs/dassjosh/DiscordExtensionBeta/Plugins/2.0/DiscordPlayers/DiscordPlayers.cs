using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord.Builders;
using Oxide.Ext.Discord.Clients;
using Oxide.Ext.Discord.Connections;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Interfaces;
using Oxide.Ext.Discord.Libraries;
using Oxide.Ext.Discord.Logging;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Discord Players", "MJSU", "2.1.3")]
    [Description("Displays online players in discord")]
    internal class DiscordPlayers : CovalencePlugin, IDiscordPlugin
    {
        #region Class Fields
        public DiscordClient Client { get; set; }
        
        [PluginReference] private Plugin Clans;

        private PluginConfig _pluginConfig; //Plugin Config
        private PluginData _pluginData;
        
        private readonly DiscordCommand _dcCommands = GetLibrary<DiscordCommand>();
        private readonly BotConnection _discordSettings = new BotConnection();

        private readonly Hash<string, DateTime> _onlineSince = new Hash<string, DateTime>();
        private readonly Hash<Snowflake, PermanentMessageHandler> _permanentState = new Hash<Snowflake, PermanentMessageHandler>();

        private readonly StringBuilderPool _pool = new StringBuilderPool();
        
        private readonly EmbedState _state = new EmbedState();

        private const string BaseCommand = "DiscordPlayers_";
        private const string BackCommand = BaseCommand + "Back";
        private const string RefreshCommand = BaseCommand + "Reload";
        private const string ForwardCommand = BaseCommand + "Forward";
        private const string ChangeSort = BaseCommand + "ChangeSort";
        private const string CommandVersion = "V1";
        
        public enum SortBy {Name, Time}
        private readonly SortBy[] _sortBy = (SortBy[])Enum.GetValues(typeof(SortBy));

        private static DiscordPlayers _ins;
        #endregion

        #region Setup & Loading
        private void Init()
        {
            _ins = this;
            _discordSettings.ApiToken = _pluginConfig.DiscordApiKey;
            _discordSettings.LogLevel = _pluginConfig.ExtensionDebugging;
            
            if(_pluginConfig.CommandEmbeds.Any(c => c.Value.Permissions.AllowInDm))
            {
                _discordSettings.Intents |= GatewayIntents.DirectMessages;
            }
            
            if(_pluginConfig.CommandEmbeds.Any(c => c.Value.Permissions.AllowInGuild))
            {
                _discordSettings.Intents |= GatewayIntents.Guilds | GatewayIntents.GuildMessages;
            }

            HashSet<string> perms = new HashSet<string>();
            foreach (EmbedSettings settings in _pluginConfig.CommandEmbeds.Values)
            {
                if (settings.Permissions.RequirePermissions)
                {
                    perms.Add(settings.Permissions.OxidePermission);
                }
            }

            foreach (string perm in perms)
            {
                permission.RegisterPermission(perm, this);
            }

            _pluginData = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name);
        }
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.NoPermission] = "You do not have permission to use this command",
                [LangKeys.BackButtonLabel] = "Back",
                [LangKeys.BackButtonEmoji] = "⬅",
                [LangKeys.PageButtonLabel] = "Page: {0}/{1}",
                [LangKeys.PageButtonEmoji] = "",
                [LangKeys.NextButtonLabel] = "Next",
                [LangKeys.NextButtonEmoji] = "➡",
                [LangKeys.RefreshButtonLabel] = "Refresh",
                [LangKeys.RefreshButtonEmoji] = "🔄",
                [LangKeys.SortedByButtonLabel] = "Sorted By: {0}",
                [LangKeys.SortedByButtonEmoji] = "",
                [LangKeys.SortByEnumName] = "Name",
                [LangKeys.SortByEnumTime] = "Time",
                [LangKeys.OnlineTimeFormat] = "{1}h {2}m {3}s",
            }, this);
        }
        
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig);
        }

        private PluginConfig AdditionalConfig(PluginConfig config)
        {
            if (config.PermanentMessages == null)
            {
                config.PermanentMessages = new List<PermanentMessageConfig>();
            }

            if (config.PermanentMessages.Count == 0)
            {
                config.PermanentMessages.Add(new PermanentMessageConfig(null)
                {
                    Command = "players"
                });
                config.PermanentMessages.Add(new PermanentMessageConfig(null)
                {
                    Command = "playersadmin"
                });
            }

            for (int i = 0; i < config.PermanentMessages.Count; i++)
            {
                config.PermanentMessages[i] = new PermanentMessageConfig(config.PermanentMessages[i]);
            }
            
            if (config.CommandEmbeds == null)
            {
                config.CommandEmbeds = new Hash<string, EmbedSettings>();
            }

            //Set default commands if empty
            if (config.CommandEmbeds.Count == 0)
            {
                EmbedSettings playerCommand = new EmbedSettings(null)
                {
                    Embed = new EmbedConfig(null)
                    {
                        Title = "{server.name}",
                        Description = "{server.players}/{server.players.max} Online Players | {server.players.loading} Loading | {server.players.queued} Queued",
                        Color = "#de8732",
                        Image = string.Empty,
                        Thumbnail = string.Empty,
                        Url = string.Empty,
                        Timestamp = true,
                        FieldFormat = new FieldFormat(null)
                        {
                            Title = "{player.name}",
                            Value = "**Online For:** {discordplayers.player.duration}",
                            Inline = true
                        },
                        Footer = new FooterConfig(null)
                        {
                            Enabled = true,
                            Text = string.Empty,
                            IconUrl = string.Empty
                        }
                    },
                    Permissions =
                    {
                        OxidePermission = "discordplayers.use"
                    }
                };

                EmbedSettings adminCommand = new EmbedSettings(null)
                {
                    Embed = new EmbedConfig(null)
                    {
                        Title = "{server.name}",
                        Description = "{server.players}/{server.players.max} Online Players | {server.players.loading} Loading | {server.players.queued} Queued",
                        Color = "#de8732",
                        Image = string.Empty,
                        Thumbnail = string.Empty,
                        Url = string.Empty,
                        Timestamp = true,
                        FieldFormat = new FieldFormat(null)
                        {
                            Title = "{player.name}",
                            Value = "**Steam ID:**{player.id}\n**Online For:** {discordplayers.player.duration}\n**Ping:** {player.ping}ms\n**Country:** {player.address.data!country}",
                            Inline = true
                        },
                        Footer = new FooterConfig(null)
                        {
                            Enabled = true,
                            Text = string.Empty,
                            IconUrl = string.Empty
                        }
                    },
                    Permissions =
                    {
                        RequirePermissions = true,
                        OxidePermission = "discordplayers.admin"
                    }
                };

                config.CommandEmbeds["players"] = playerCommand;
                config.CommandEmbeds["playersadmin"] = adminCommand;
            }
            
            foreach (string key in config.CommandEmbeds.Keys.ToList())
            {
                config.CommandEmbeds[key] = new EmbedSettings(config.CommandEmbeds[key]);
            }

            return config;
        }

        private void OnServerInitialized()
        {
            if (string.IsNullOrEmpty(_pluginConfig.DiscordApiKey))
            {
                PrintWarning("Please set the Discord Bot Token and reload the plugin");
                return;
            }
            
            if (!IsPlaceholderApiLoaded())
            {
                PrintError("Missing plugin dependency PlaceholderAPI: https://umod.org/plugins/placeholder-api");
                return;
            }
            
            if(PlaceholderAPI.Version < new VersionNumber(2, 2, 0))
            {
                PrintError("Placeholder API plugin must be version 2.2.0 or higher");
                return;
            }
            
            if (_pluginConfig.EmbedFieldLimit > 25)
            {
                PrintWarning("Player Amount Per Page cannot be greater than 25");
            }
            else if (_pluginConfig.EmbedFieldLimit < 0)
            {
                PrintWarning("Player Amount Per Page cannot be less than 0");
            }

            _pluginConfig.EmbedFieldLimit = Mathf.Clamp(_pluginConfig.EmbedFieldLimit, 0, 25);
            
            foreach (KeyValuePair<string, EmbedSettings> command in _pluginConfig.CommandEmbeds)
            {
                RegisterDiscordCommand(command.Key, nameof(DiscordPlayersCommand), command.Value.Permissions.AllowInDm, command.Value.Permissions.AllowInGuild, command.Value.Permissions.AllowedChannels);
            }

#if RUST
            foreach (Network.Connection connection in Network.Net.sv.connections)
            {
                _onlineSince[connection.ownerid.ToString()] = DateTime.UtcNow - TimeSpan.FromSeconds(connection.GetSecondsConnected());
            }
#else
            foreach (IPlayer player in players.Connected)
            {
                _onlineSince[player.Id] = DateTime.UtcNow;
            }
#endif

            Client.Connect(_discordSettings);
        }

        private void OnUserConnected(IPlayer player)
        {
            _onlineSince[player.Id] = DateTime.UtcNow;
        }

        private void OnUserDisconnected(IPlayer player)
        {
            _onlineSince.Remove(player.Id);
        }

        private void Unload()
        {
            SaveData();
            _ins = null;
        }
        #endregion

        #region Discord Chat Command
        private void DiscordPlayersCommand(DiscordMessage message, string cmd, string[] args)
        {
            EmbedSettings embed = _pluginConfig.CommandEmbeds[cmd];
            if (embed == null)
            {
                return;
            }
            
            if (embed.Permissions.RequirePermissions && !UserHasPermission(message, embed))
            {
                message.Reply(Client, Lang(LangKeys.NoPermission, message.Author.Player));
                return;
            }
            
            _state.Update(cmd, 0, SortBy.Name);

            message.Reply(Client, BuildCreateMessage(_state));
        }

        public bool UserHasPermission(DiscordMessage message, EmbedSettings settings)
        {
            IPlayer player = message.Author.Player;
            if (player != null && player.HasPermission(settings.Permissions.OxidePermission))
            {
                return true;
            }

            if (message.Member != null)
            {
                foreach (Snowflake role in settings.Permissions.AllowedRoles)
                {
                    if (message.Member.Roles.Contains(role))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        #endregion

        #region Discord Hooks
        [HookMethod(DiscordExtHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            DiscordApplication app = Client.Bot.Application;
            if (!app.HasApplicationFlag(ApplicationFlags.GatewayMessageContentLimited))
            {
                PrintWarning($"You will need to enable \"Message Content Intent\" for {Client.Bot.BotUser.Username} @ https://discord.com/developers/applications\n by April 2022" +
                             $"{Name} will stop function correctly after that date until that is fixed.");
            }

            Puts($"{Title} Ready");
        }

        [HookMethod(DiscordExtHooks.OnDiscordGuildCreated)]
        private void OnDiscordGuildCreated(DiscordGuild created)
        {
            foreach (PermanentMessageConfig permConfig in _pluginConfig.PermanentMessages)
            {
                if (!permConfig.Enabled || !permConfig.ChannelId.IsValid())
                {
                    continue;
                }

                if (!created.Channels.ContainsKey(permConfig.ChannelId))
                {
                    continue;
                }
                
                if (!_pluginConfig.CommandEmbeds.ContainsKey(permConfig.Command))
                {
                    PrintError($"Permanent Command '{permConfig.Command}' Not Found in Embed Command. Possible commands to use are: {string.Join(", ",  _pluginConfig.CommandEmbeds.Keys.ToArray())}");
                    continue;
                }
                
                DiscordChannel channel = created.Channels[permConfig.ChannelId];
                if (channel == null)
                {
                    continue;
                }

                PermanentMessageData existing = _pluginData.GetPermanentMessage(channel.Id, permConfig.Command);
                if (existing != null)
                {
                    channel.GetMessage(Client, existing.MessageId).Then(message =>
                    {
                        _permanentState[message.Id] = new PermanentMessageHandler(message, permConfig);
                    }).Catch<ResponseError>(error =>
                    {
                        if (error.HttpStatusCode == DiscordHttpStatusCode.NotFound)
                        {
                            CreatePermanentMessage(channel, permConfig);
                        }
                    });
                }
                else
                {
                    CreatePermanentMessage(channel, permConfig);
                }
            }
        }

        private void CreatePermanentMessage(DiscordChannel channel, PermanentMessageConfig permConfig)
        {
            EmbedState state = new EmbedState(permConfig.Command, 0, SortBy.Name);
            channel.CreateMessage(Client, BuildCreateMessage(state)).Then(message =>
            {
                _pluginData.SetPermanentMessage(channel.Id, permConfig.Command, new PermanentMessageData
                {
                    MessageId = message.Id
                });
                SaveData();
                _permanentState[message.Id] = new PermanentMessageHandler(message, permConfig);
            });
        }

        [HookMethod(DiscordExtHooks.OnDiscordInteractionCreated)]
        private void OnDiscordInteractionCreated(DiscordInteraction interaction)
        {
            if (interaction.Type != InteractionType.MessageComponent)
            {
                return;
            }

            string customId = interaction.Data.CustomId;
            if (!customId.StartsWith(BaseCommand))
            {
                return;
            }

            EmbedState state = _permanentState[interaction.Message.Id]?.State ?? _state;
            
            state.Update(customId);
            
            EmbedSettings settings = _pluginConfig.CommandEmbeds[state.EmbedCommand];
            if (settings == null)
            {
                return;
            }
            
            switch (state.InteractionCommand)
            {
                case BackCommand:
                    state.Page--;
                    break;
                
                case RefreshCommand:
                    break;
                
                case ForwardCommand:
                    state.Page++;
                    break;
                
                case ChangeSort:
                    state.Sort = NextEnum(state.Sort, _sortBy);
                    break;
            }

            interaction.CreateResponse(Client, new InteractionResponse
            {
                Type = InteractionResponseType.UpdateMessage,
                Data = BuildInteractionCallback(state)
            });
        }
        #endregion

        #region Message Building
        private MessageCreate BuildCreateMessage(EmbedState state)
        {
            MessageCreate message = new MessageCreate
            {
                Embeds = new List<DiscordEmbed> { BuildEmbed(state) },
                Components = BuildComponents(state)
            };

            return message;
        }

        private InteractionCallbackData BuildInteractionCallback(EmbedState state)
        {
            InteractionCallbackData message = new InteractionCallbackData
            {
                Embeds = new List<DiscordEmbed> { BuildEmbed(state) },
                Components = BuildComponents(state)
            };

            return message;
        }

        private DiscordEmbed BuildEmbed(EmbedState state)
        {
            EmbedSettings settings = _pluginConfig.CommandEmbeds[state.EmbedCommand];
            EmbedConfig embed = settings.Embed;
            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            if (!string.IsNullOrEmpty(embed.Title))
            {
                builder.AddTitle(ParseField(null, embed.Title));
            }

            if (!string.IsNullOrEmpty(embed.Description))
            {
                builder.AddDescription(ParseField(null, embed.Description));
            }
            
            if (!string.IsNullOrEmpty(embed.Url))
            {
                builder.AddUrl(ParseField(null, embed.Url));
            }
            
            if (!string.IsNullOrEmpty(embed.Image))
            {
                builder.AddImage(ParseField(null, embed.Image));
            }
            
            if (!string.IsNullOrEmpty(embed.Thumbnail))
            {
                builder.AddThumbnail(ParseField(null, embed.Thumbnail));
            }
            
            if (!string.IsNullOrEmpty(embed.Color))
            {
                builder.AddColor(embed.Color);
            }
            
            if (embed.Timestamp)
            {
                builder.AddNowTimestamp();
            }

            if (embed.Footer.Enabled)
            {
                if (string.IsNullOrEmpty(embed.Footer.Text) &&
                    string.IsNullOrEmpty(embed.Footer.IconUrl))
                {
                    AddPluginInfoFooter(builder);
                }
                else
                {
                    string text = ParseField(null, embed.Footer.Text);
                    string footerUrl = ParseField(null, embed.Footer.IconUrl);
                    builder.AddFooter(text, footerUrl);
                }
            }

            int onlineCount = players.Connected.Count();
            if ((state.Page + 1) * _pluginConfig.EmbedFieldLimit > onlineCount)
            {
                state.Page = onlineCount / _pluginConfig.EmbedFieldLimit;
                if (onlineCount % _pluginConfig.EmbedFieldLimit == 0)
                {
                    state.Page = Math.Max(state.Page - 1, 0);
                }
            }

            IEnumerable<IPlayer> sortedPlayers = players.Connected.Where(p => !p.IsAdmin || settings.ShowAdmins);
            if (state.Sort == SortBy.Name)
            {
                sortedPlayers = sortedPlayers.OrderBy(p => p.Name);
            }
            else
            {
                sortedPlayers = sortedPlayers.OrderBy(p => _onlineSince[p.Id]);
            }

            int index = state.Page * _pluginConfig.EmbedFieldLimit + 1;
            sortedPlayers = sortedPlayers.Skip(state.Page * _pluginConfig.EmbedFieldLimit).Take(_pluginConfig.EmbedFieldLimit);
            foreach (IPlayer player in sortedPlayers)
            {
                string title = $"#{index} {ParseField(player, embed.FieldFormat.Title)}";
                builder.AddField(title, ParseField(player, embed.FieldFormat.Value), embed.FieldFormat.Inline);
                index++;
            }

            return builder.Build();
        }

        private List<ActionRowComponent> BuildComponents(EmbedState state)
        {
            int totalPlayers = players.Connected.Count();
            bool disableBack = state.Page <= 0;
            bool disableForward = (state.Page + 1) * _pluginConfig.EmbedFieldLimit >= totalPlayers;

            string buttonCmd = BuildCommand(state);

            string backEmoji = Lang(LangKeys.BackButtonEmoji);
            string pageEmoji = Lang(LangKeys.PageButtonEmoji);
            string nextEmoji = Lang(LangKeys.NextButtonEmoji);
            string refreshEmoji = Lang(LangKeys.RefreshButtonEmoji);
            string sortedByEmoji = Lang(LangKeys.SortedByButtonEmoji);
            string sortByText = Lang(state.Sort == SortBy.Name ? LangKeys.SortByEnumName : LangKeys.SortByEnumTime);
            int maxPage = totalPlayers / _pluginConfig.EmbedFieldLimit;
            if (totalPlayers == 0 || totalPlayers % _pluginConfig.EmbedFieldLimit != 0)
            {
                maxPage += 1;
            }

            MessageComponentBuilder builder = new MessageComponentBuilder();
            builder.AddActionButton(ButtonStyle.Primary, Lang(LangKeys.BackButtonLabel), $"{BackCommand} {buttonCmd}", disableBack, false, string.IsNullOrEmpty(backEmoji) ? null : DiscordEmoji.FromCharacter(backEmoji));
            builder.AddActionButton(ButtonStyle.Primary,  Lang(LangKeys.PageButtonLabel, state.Page + 1, maxPage), "PAGE", true, false, string.IsNullOrEmpty(pageEmoji) ? null : DiscordEmoji.FromCharacter(pageEmoji));
            builder.AddActionButton(ButtonStyle.Primary, Lang(LangKeys.NextButtonLabel), $"{ForwardCommand} {buttonCmd}", disableForward, false, string.IsNullOrEmpty(nextEmoji) ? null : DiscordEmoji.FromCharacter(nextEmoji));
            builder.AddActionButton(ButtonStyle.Primary, Lang(LangKeys.RefreshButtonLabel), $"{RefreshCommand} {buttonCmd}", false, false, string.IsNullOrEmpty(refreshEmoji) ? null : DiscordEmoji.FromCharacter(refreshEmoji));
            builder.AddActionButton(ButtonStyle.Primary,  Lang(LangKeys.SortedByButtonLabel, sortByText), $"{ChangeSort} {buttonCmd}", false, false, string.IsNullOrEmpty(sortedByEmoji) ? null : DiscordEmoji.FromCharacter(sortedByEmoji));

            return builder.Build();
        }

        private string BuildCommand(EmbedState state)
        {
            StringBuilder sb = _pool.Get();
            sb.Append(CommandVersion);
            sb.Append(" ");
            sb.Append(state.EmbedCommand);
            sb.Append(" ");
            sb.Append(state.Page);
            sb.Append(" ");
            sb.Append(state.Sort.ToString());
            string buttonCmd = sb.ToString();
            _pool.Free(ref sb);
            return buttonCmd;
        }
        #endregion

        #region Helper Methods
        private const string PluginIcon = "https://assets.umod.org/images/icons/plugin/61354f8bd5faf.png";
        
        private void AddPluginInfoFooter(DiscordEmbedBuilder builder)
        {
            builder.AddFooter($"{Title} V{Version} by {Author}", PluginIcon);
        }

        private object GetDuration(IPlayer player, string _)
        {
            TimeSpan duration = DateTime.UtcNow - _onlineSince[player.Id];
            return Lang(LangKeys.OnlineTimeFormat, duration.Days, duration.Hours, duration.Minutes, duration.Seconds);
        }
        
        private string GetClanTag(IPlayer player)
        {
            string clanTag = Clans?.Call<string>("GetClanOf", player);
            if (!string.IsNullOrEmpty(clanTag))
            {
                return $"[{clanTag}]";
            }

            return string.Empty;
        }

        public T NextEnum<T>(T src, T[] array) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException($"Argument {typeof(T).FullName} is not an Enum");
            
            int index = Array.IndexOf(array, src) + 1;
            return array.Length == index ? array[0] : array[index];            
        }
        
        public string Lang(string key)
        {
            return lang.GetMessage(key, this);
        }
        
        public string Lang(string key, params object[] args)
        {
            try
            {
                return string.Format(Lang(key), args);
            }
            catch(Exception ex)
            {
                PrintError($"Lang Key '{key}' threw exception\n:{ex}");
                throw;
            }
        }
        
        public void RegisterDiscordCommand(string name, string method, bool direct, bool guild, List<Snowflake> allowedChannels)
        {
            if (direct)
            {
                _dcCommands.AddDirectMessageCommand(name, this, method);
            }

            if (guild)
            {
                _dcCommands.AddGuildCommand(name, this, allowedChannels, method);
            }
        }
        
        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _pluginData);
        #endregion

        #region Classes
        public class PluginConfig
        {
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string DiscordApiKey { get; set; }

            [DefaultValue(25)]
            [JsonProperty(PropertyName = "Player Amount Per Page")]
            public int EmbedFieldLimit { get; set; }
            
            [JsonProperty(PropertyName = "Permanent Embeds")]
            public List<PermanentMessageConfig> PermanentMessages { get; set; }
            
            [JsonProperty(PropertyName = "Command Embeds")]
            public Hash<string, EmbedSettings> CommandEmbeds { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(DiscordLogLevel.Info)]
            [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public DiscordLogLevel ExtensionDebugging { get; set; }
        }
        
        public class PermanentMessageConfig
        {
            [JsonProperty(PropertyName = "Enabled")]
            public bool Enabled { get; set; }
            
            [JsonProperty(PropertyName = "Permanent Message Channel ID")]
            public Snowflake ChannelId { get; set; }

            [JsonProperty(PropertyName = "Update Rate (Minutes)")]
            public float UpdateRate { get; set; }
            
            [JsonProperty(PropertyName = "Command To Use")]
            public string Command { get; set; }

            public PermanentMessageConfig(PermanentMessageConfig settings)
            {
                Enabled = settings?.Enabled ?? false;
                ChannelId = settings?.ChannelId ?? default(Snowflake);
                UpdateRate = settings?.UpdateRate ?? 1f;
                Command = settings?.Command ?? "";
            }
        }

        public class EmbedSettings
        {
            [JsonProperty(PropertyName = "Permissions Needed To Use Command")]
            public CommandPermissions Permissions { get; set; }
            
            [JsonProperty(PropertyName = "Display Setting For The Embed")]
            public EmbedConfig Embed { get; set; }
            
            [JsonProperty(PropertyName = "Display Admins In The Player List")]
            public bool ShowAdmins { get; set; }

            public EmbedSettings(EmbedSettings settings)
            {
                Permissions = new CommandPermissions(settings?.Permissions);
                Embed = new EmbedConfig(settings?.Embed);
                ShowAdmins = settings?.ShowAdmins ?? true;
            }
        }
        
        public class CommandPermissions
        {
            [JsonProperty(PropertyName = "Require Permissions To Use Command")]
            public bool RequirePermissions { get; set; }
            
            [JsonProperty(PropertyName = "Oxide Permission To Use Command")]
            public string OxidePermission { get; set; }
            
            [JsonProperty(PropertyName = "Allow Discord Commands In Direct Messages")]
            public bool AllowInDm { get; set; }
            
            [JsonProperty(PropertyName = "Allow Discord Commands In Guild")]
            public bool AllowInGuild { get; set; }
            
            [JsonProperty(PropertyName = "Allow Guild Commands Only In The Following Guild Channel Or Category (Channel ID Or Category ID)")]
            public List<Snowflake> AllowedChannels { get; set; }
            
            [JsonProperty(PropertyName = "Allow Commands for members having role (Role ID)")]
            public List<Snowflake> AllowedRoles { get; set; }

            public CommandPermissions(CommandPermissions settings)
            {
                RequirePermissions = settings?.RequirePermissions ?? false;
                OxidePermission = settings?.OxidePermission ?? string.Empty;
                AllowInDm = settings?.AllowInDm ?? true;
                AllowInGuild = settings?.AllowInGuild ?? false;
                AllowedChannels = settings?.AllowedChannels ?? new List<Snowflake>();
                AllowedRoles = settings?.AllowedRoles ?? new List<Snowflake>();
            }
        }

        public class PermanentMessageHandler
        {
            public DiscordMessage Message { get; }

            private MessageUpdate Update = new MessageUpdate
            {
                Embeds = new List<DiscordEmbed>()
            };
            public EmbedState State { get; }
            public Timer Timer { get; }

            public PermanentMessageHandler(DiscordMessage message, PermanentMessageConfig config)
            {
                Message = message;
                string customId = ((ButtonComponent)message.Components[0].Components[0]).CustomId;
                State = new EmbedState(customId);
                Timer = _ins.timer.Every(config.UpdateRate * 60f, SendUpdate);
                SendUpdate();
            }

            private void SendUpdate()
            {
                Update.Embeds.Clear();
                Update.Embeds.Add(_ins.BuildEmbed(State));
                Update.Components = _ins.BuildComponents(State);
                Message.Edit(_ins.Client, Update).Catch<ResponseError>(error =>
                {
                    if (error.HttpStatusCode == DiscordHttpStatusCode.NotFound)
                    {
                        Timer?.Destroy();
                    }
                });
            }
        }

        public static class LangKeys
        {
            public const string NoPermission = nameof(NoPermission);
            public const string BackButtonLabel = nameof(BackButtonLabel);
            public const string BackButtonEmoji = nameof(BackButtonEmoji);
            public const string PageButtonLabel = nameof(PageButtonLabel);
            public const string PageButtonEmoji = nameof(PageButtonEmoji);
            public const string NextButtonLabel = nameof(NextButtonLabel);
            public const string NextButtonEmoji = nameof(NextButtonEmoji);
            public const string RefreshButtonLabel = nameof(RefreshButtonLabel);
            public const string RefreshButtonEmoji = nameof(RefreshButtonEmoji);
            public const string SortedByButtonLabel = nameof(SortedByButtonLabel);
            public const string SortedByButtonEmoji = nameof(SortedByButtonEmoji);
            public const string SortByEnumName = nameof(SortByEnumName);
            public const string SortByEnumTime = nameof(SortByEnumTime);
            public const string OnlineTimeFormat = nameof(OnlineTimeFormat);
        }

        public class EmbedConfig
        {
            [JsonProperty("Title")]
            public string Title { get; set; }
            
            [JsonProperty("Description")]
            public string Description { get; set; }
            
            [JsonProperty("Url")]
            public string Url { get; set; }
            
            [JsonProperty("Embed Color (Hex Color Code)")]
            public string Color { get; set; }
            
            [JsonProperty("Image Url")]
            public string Image { get; set; }
            
            [JsonProperty("Thumbnail Url")]
            public string Thumbnail { get; set; }
            
            [JsonProperty("Add Timestamp")]
            public bool Timestamp { get; set; }
            
            [JsonProperty(PropertyName = "Format Applied To Fields")]
            public FieldFormat FieldFormat { get; set; }

            [JsonProperty("Footer")]
            public FooterConfig Footer { get; set; }

            public EmbedConfig(EmbedConfig settings)
            {
                Title = settings?.Title ?? string.Empty;
                Description = settings?.Description ?? string.Empty;
                Url = settings?.Url ?? string.Empty;
                Color = settings?.Color ?? string.Empty;
                Image = settings?.Image ?? string.Empty;
                Thumbnail = settings?.Thumbnail ?? string.Empty;
                Timestamp = settings?.Timestamp ?? true;
                FieldFormat = new FieldFormat(settings?.FieldFormat);
                Footer = new FooterConfig(settings?.Footer);
            }
        }
        
        public class FooterConfig
        {
            [JsonProperty("Icon Url")]
            public string IconUrl { get; set; }
            
            [JsonProperty("Text")]
            public string Text { get; set; }
            
            [JsonProperty("Enabled")]
            public bool Enabled { get; set; }

            public FooterConfig(FooterConfig settings)
            {
                IconUrl = settings?.IconUrl ?? string.Empty;
                Text = settings?.Text ?? string.Empty;
                Enabled = settings?.Enabled ?? true;
            }
        }
        
        public class FieldFormat
        {
            [JsonProperty("Title")]
            public string Title { get; set; }
            
            [JsonProperty("Value")]
            public string Value { get; set; }
            
            [JsonProperty("Inline")]
            public bool Inline { get; set; }

            public FieldFormat(FieldFormat settings)
            {
                Title = settings?.Title ?? string.Empty;
                Value = settings?.Value ?? string.Empty;
                Inline = settings?.Inline ?? true;
            }
        }

        public class PluginData
        {
            public Hash<string, PermanentMessageData> PermanentMessageIds = new Hash<string, PermanentMessageData>();

            public PermanentMessageData GetPermanentMessage(Snowflake channelId, string command)
            {
                return PermanentMessageIds[$"{channelId.ToString()}_{command}"];
            }

            public void SetPermanentMessage(Snowflake channelId, string command, PermanentMessageData data)
            {
                PermanentMessageIds[$"{channelId.ToString()}_{command}"] = data;
            }
        }

        public class PermanentMessageData
        {
            public Snowflake MessageId { get; set; }
        }

        public class EmbedState
        {
            public string InteractionCommand;
            public string Version;
            public string EmbedCommand;
            public int Page;
            public SortBy Sort;
            
            [JsonConstructor]
            public EmbedState()
            {
                
            }
            
            public EmbedState(string customId)
            {
                Update(customId);
            }
            
            public EmbedState(string commandKey, int page, SortBy sort)
            {
                InteractionCommand = string.Empty;
                Version = CommandVersion;
                Update(commandKey, page, sort);
            }

            public void Update(string commandKey, int page, SortBy sort)
            {
                EmbedCommand = commandKey;
                Page = page;
                Sort = sort;
            }
            
            public void Update(string customId)
            {
                try
                {
                    string[] args = customId.Split(' ');
                    InteractionCommand = args[0];
                    Version = args[1];
                    EmbedCommand = args[2];
                    int.TryParse(args[3], out Page);
                    Sort = (SortBy)Enum.Parse(typeof(SortBy), args[4]);
                }
                catch (Exception ex)
                {
                    _ins.PrintError($"Failed to Update Embed With ID: {customId}");
                    throw;
                }
            }
        }

        private class BasePool<T> where T : class
        {
            protected readonly List<T> Pool = new List<T>();
            protected readonly Func<T> Init;

            public BasePool(Func<T> init)
            {
                Init = init;
            }
            
            public virtual T Get()
            {
                if (Pool.Count == 0)
                {
                    return Init.Invoke();
                }

                int index = Pool.Count - 1; //Removing the last element prevents an array copy.
                T entity = Pool[index];
                Pool.RemoveAt(index);
                
                return entity;
            }

            public virtual void Free(ref T entity)
            {
                Pool.Add(entity);
                entity = null;
            }
        }
        
        private class StringBuilderPool : BasePool<StringBuilder>
        {
            public StringBuilderPool() : base(() => new StringBuilder())
            {
            }
            
            public override void Free(ref StringBuilder sb)
            {
                sb.Length = 0;
                base.Free(ref sb);
            }
        }
        #endregion
        
        #region PlaceholderAPI
        [PluginReference] private Plugin PlaceholderAPI;
        private Action<IPlayer, StringBuilder, bool> _replacer;
        
        private string ParseField(IPlayer player, string field)
        {
            StringBuilder sb = _pool.Get();
            sb.Append(field);
            GetReplacer()?.Invoke(player, sb, false);
            string parsed = sb.ToString();
            _pool.Free(ref sb);
            return parsed;
        }
        
        private void OnPluginUnloaded(Plugin plugin)
        {
            if (plugin?.Name == "PlaceholderAPI")
            {
                _replacer = null;
            }
        }
        
        private void OnPlaceholderAPIReady()
        {
            RegisterPlaceholder("discordplayers.player.duration", GetDuration, "Displays the online duration for a player");
            RegisterPlaceholder("discordplayers.player.clantag", (player, s) => GetClanTag(player), "Displays the players clan tag");
        }
        
        private void RegisterPlaceholder(string key, Func<IPlayer, string, object> action, string description = null)
        {
            if (IsPlaceholderApiLoaded())
            {
                PlaceholderAPI.Call("AddPlaceholder", this, key, action, description);
            }
        }

        private Action<IPlayer, StringBuilder, bool> GetReplacer()
        {
            if (!IsPlaceholderApiLoaded())
            {
                return _replacer;
            }
            
            return _replacer ?? (_replacer = PlaceholderAPI.Call<Action<IPlayer, StringBuilder, bool>>("GetProcessPlaceholders", 1));
        }

        private bool IsPlaceholderApiLoaded() => PlaceholderAPI != null && PlaceholderAPI.IsLoaded;
        #endregion
    }
}
