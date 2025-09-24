// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Facepunch.Extend;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Builders;
using Oxide.Ext.Discord.Builders.MessageComponents;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Channels;
using Oxide.Ext.Discord.Entities.Emojis;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;
using Oxide.Ext.Discord.Entities.Guilds;
using Oxide.Ext.Discord.Entities.Interactions;
using Oxide.Ext.Discord.Entities.Interactions.MessageComponents;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Entities.Messages.Embeds;
using Oxide.Ext.Discord.Entities.Permissions;
using Oxide.Ext.Discord.Entities.Users;
using Oxide.Ext.Discord.Libraries.Command;
using Oxide.Ext.Discord.Libraries.Linking;
using Oxide.Ext.Discord.Logging;
using System;
using Random = System.Random;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Discord Server Panel", "Aimon", "1.1.0")]
    [Description("Adds server control commands to your discord bot.")]
    public class DiscordServerPanel : CovalencePlugin
    {
        #region Classes

        public string consolelog = "";

        public class PlayerConsole : IPlayer
        {
            public object Object { set; get; }

            public CommandType LastCommand { set; get; } = CommandType.Console; public string Name { set; get; } = "Server Console"; public string Id { set; get; } = "server_console"; public string Address { set; get; } = "server_console"; public int Ping { set; get; } = -1; public CultureInfo Language { set; get; } = null; public bool IsConnected { set; get; } = false; public bool IsSleeping { set; get; } = true; public bool IsServer { set; get; } = true; public bool IsAdmin { set; get; } = true; public bool IsBanned { set; get; } = false; public TimeSpan BanTimeRemaining { set; get; } = new TimeSpan(0, 0, 0); public float Health { set; get; } = 0; public float MaxHealth { set; get; } = 100; public void AddToGroup(string group)
            { }

            public void Ban(string reason, TimeSpan duration = default(TimeSpan))
            { }

            public bool BelongsToGroup(string group)
            { return false; }

            public void Command(string command, params object[] args)
            { }

            public void GrantPermission(string perm)
            { }

            public bool HasPermission(string perm)
            { return true; }

            public void Heal(float amount)
            { }

            public void Hurt(float amount)
            { }

            public void Kick(string reason)
            { }

            public void Kill()
            { }

            public void Message(string message, string prefix, params object[] args)
            { }

            public void Message(string message)
            { }

            public void Position(out float x, out float y, out float z)
            { x = 0; y = 0; z = 0; }

            public GenericPosition Position()
            { return new GenericPosition(0, 0, 0); }

            public void RemoveFromGroup(string group)
            { }

            public void Rename(string name)
            { }

            public void Reply(string message, string prefix, params object[] args)
            {; }

            public void Reply(string message)
            { }

            public void RevokePermission(string perm)
            { }

            public void Teleport(float x, float y, float z)
            { }

            public void Teleport(GenericPosition pos)
            { }

            public void Unban()
            { }
        }

        public class LinkMessageData
        {
            public Snowflake ChannelId { get; set; }
            public Snowflake MessageId { get; set; }
        }

        #endregion Classes

        #region Global Variables

        [PluginReference] private readonly Plugin DiscordAuth, DiscordCore, WipeInfoApi, Economics, TimedPermissions, EnhancedBanSystem, ServerArmour, BetterChatMute, Clans, DSPWipe, WipeServer;
        [DiscordClient] private readonly DiscordClient _client;
        private readonly Random _random = new Random();
        private bool connected = true;
        private DiscordGuild _guild;
        private DiscordRole _role;
        private DiscordRole _rolec;
        private DiscordRole _roleperm;
        private DiscordRole _rolep;
        private DiscordRole _roler;
        private DiscordRole _rolew;
        private readonly DiscordLink _link = GetLibrary<DiscordLink>();
        private readonly DiscordCommand _dcCommands = Interface.Oxide.GetLibrary<DiscordCommand>();
        public List<PluginInfo> DisabledPlugins = new List<PluginInfo>();
        private const string PluginsButtonIcon = "🔵";
        private const string PermissionsButtonIcon = "👀";
        private const string RestartButtonIcon = "🔁";
        private const string MapVoterButtonIcon = "🌍";
        private const string SkipNightButtonIcon = "💤";
        private const string CustomButtonIcon = "🔨";
        private const string WipeButtonIcon = "❌";

        public class PluginInfo
        {
            public PluginInfo(string name, string title)
            {
                Name = name;
                Title = title;
            }

            public string Name { get; set; }
            public string Title { get; set; }
        }

        #endregion Global Variables

        #region Configuration

        private ConfigData _configData;

        private class ConfigData
        {
            public string LogFileName = "DiscordServerPanel";

            [JsonProperty(PropertyName = "Bot token")]
            public string Token = "";

            [JsonProperty(PropertyName = "Discord Guild ID (optional if the bot is in one guild)")]
            public Snowflake GuildId { get; set; }

            [JsonProperty(PropertyName = "Discord Role ID")]
            public Snowflake RoleId { get; set; }

            [JsonProperty(PropertyName = "Discord Channel ID where the command can be used")]
            public Snowflake ChannelID { get; set; }

            [JsonProperty(PropertyName = "Embed's color")]
            public string Color = "#ff0000";

            [JsonProperty(PropertyName = "Show Server FPS (Server command)")]
            public bool showfps = true;

            [JsonProperty(PropertyName = "Show Gamemode (Server command)")]
            public bool showgamemode = true;

            [JsonProperty(PropertyName = "Show Plugins Loaded (Server command)")]
            public bool showplugins = true;

            [JsonProperty(PropertyName = "Custom")]
            public Custom CUSTOM = new Custom();

            [JsonProperty(PropertyName = "Restart")]
            public Restart RESTART = new Restart();

            [JsonProperty(PropertyName = "Plugins")]
            public Plugins PLUGINS = new Plugins();

            [JsonProperty(PropertyName = "Permissions")]
            public Perm PERM = new Perm();

            [JsonProperty(PropertyName = "Wipe")]
            public Wipe WIPE = new Wipe();
            public class Restart
            {
                [JsonProperty(PropertyName = "Use Restart (true/false)")]
                public bool restartuse = true;

                [JsonProperty(PropertyName = "Discord Role ID (Can be left empty to use the first role id)")]
                public Snowflake RoleId { get; set; }

                [JsonProperty(PropertyName = "Restart Reasons (One Word)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public string[] RestartReasons = new string[] { "Maintenance", "Update", "Wipe", "None" };

                [JsonProperty(PropertyName = "Time for command (Use 'h' for hours, 'm' for minutes)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public string[] timerestart = new string[] { "5m", "10m", "30m", "1h" };
            }

            public class Perm
            {
                [JsonProperty(PropertyName = "Use Permissions (true/false)")]
                public bool permuse = true;

                [JsonProperty(PropertyName = "Discord Role ID (Can be left empty to use the first role id)")]
                public Snowflake RoleId { get; set; }
            }

            public class Plugins
            {
                [JsonProperty(PropertyName = "Use Plugins (true/false)")]
                public bool pluginsuse = true;

                [JsonProperty(PropertyName = "Discord Role ID (Can be left empty to use the first role id)")]
                public Snowflake RoleId { get; set; }

                [JsonProperty(PropertyName = "Use Confirmation (true/false)")]
                public bool confirmuse = true;
            }
            public class Wipe
            {
                [JsonProperty(PropertyName = "Use Wipe (true/false)")]
                public bool wipeuse = false;

                [JsonProperty(PropertyName = "Discord Role ID (Can be left empty to use the first role id)")]
                public Snowflake RoleId { get; set; }

                [JsonProperty(PropertyName = "Generate Random Seeds (How many options to choose from)")]
                public int generatemaps = 4;

                [JsonProperty(PropertyName = "Map Size")]
                public int mapsize = 3500;

                [JsonProperty(PropertyName = "Backup Map Files (Backup in identity folder)")]
                public bool backup = true;

                [JsonProperty(PropertyName = "Time for command (Use 'h' for hours, 'm' for minutes)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public string[] timedelay = new string[] { "5m", "10m", "30m", "1h" };
            }
            public class Custom
            {
                [JsonProperty(PropertyName = "Use Custom (true/false)")]
                public bool customuse = false;

                [JsonProperty(PropertyName = "Use Confirmation (true/false)")]
                public bool confirmuse = false;

                [JsonProperty(PropertyName = "Discord Role ID (Can be left empty to use the first role id)")]
                public Snowflake RoleId { get; set; }

                [JsonProperty(PropertyName = "Custom Commands ('command name' 'command to send on console')", ObjectCreationHandling = ObjectCreationHandling.Replace)]
                public Dictionary<string, string> Commands = new Dictionary<string, string>
                {
                    {
                        "Restart", "restart 300 Maintenance"
                    },
                    {
                        "SkipNight", "env.time 12"
                    }
                };
            }

            public Logs logs = new Logs();

            public class Logs
            {
                [JsonProperty(PropertyName = "Log to console (true/false)")]
                public bool LogToConsole = true;

                [JsonProperty(PropertyName = "Log to discord (true/false)")]
                public bool LogToDiscord = false;

                [JsonProperty(PropertyName = "Log Discord Channel ID")]
                public Snowflake logChannelID { get; set; }

                [JsonConverter(typeof(StringEnumConverter))]
                [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose/Debug/Info/Warning/Error/Exception/Off)")]
                public DiscordLogLevel ExtensionDebugging = DiscordLogLevel.Info;

                [JsonProperty(PropertyName = "Delete message after command")]
                public bool messagedelete = true;

                [JsonProperty(PropertyName = "Delete message after interaction")]
                public bool interactiondelete = true;
            }
        }

        protected override void LoadConfig()
        {
            try
            {
                base.LoadConfig();
                _configData = Config.ReadObject<ConfigData>();
                SaveConfig(_configData);
            }
            catch (Exception)
            {
                PrintError(Lang("ConfigIssue"));
                return;
            }
        }

        private void OnServerInitialized()
        {
            if (string.IsNullOrEmpty(_configData.Token))
            {
                PrintError(Lang("NoToken"));
                return;
            }
            RegisterCommands();
            RegisterPerms();
        }

        protected override void LoadDefaultConfig()
        {
            Puts(Lang("NewConfig"));
            _configData = new ConfigData();
            SaveConfig(_configData);
        }

        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        private void Loaded()
        {
            UnityEngine.Application.logMessageReceived += ConsoleLog;    
        }

        private void Unload()
        {
            UnityEngine.Application.logMessageReceived -= ConsoleLog;
        }

        #endregion Configuration

        #region Discord Bot Connection

        [HookMethod(DiscordExtHooks.OnDiscordClientCreated)]
        private void OnDiscordClientCreated()
        {
            if (string.IsNullOrEmpty(_configData.Token) || _configData.Token == null || _configData.Token == "BotToken")
            {
                PrintError("API key is empty or invalid!");
                server.Command("o.unload DiscordServerPanel");
                return;
            }

            try
            {
                DiscordSettings settings = new DiscordSettings
                {
                    ApiToken = _configData.Token,
                    Intents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages | GatewayIntents.DirectMessages,
                    LogLevel = _configData.logs.ExtensionDebugging
                };
                _client.Connect(settings);
            }
            catch (Exception e)
            {
                connected = false;
                PrintError($"DiscordAdminPanel failed to create client! Exception message: {e}");
            }
        }

        private void GetGuild(GatewayReadyEvent ready)
        {
            DiscordGuild guild = null;

            if (!ready.Guilds.Values.Contains(ready.Guilds[_configData.GuildId]))
            {
                guild = ready.Guilds.Values.FirstOrDefault();
            }
            if (guild == null)
            {
                guild = ready.Guilds[_configData.GuildId];
            }
            if (guild == null)
            {
                PrintError(Lang("InviteBot"));
                _client.Disconnect();
                server.Command("o.unload DiscordServerPanel");
                return;
            }
            _guild = guild;
        }

        private void RegisterDiscordCommands()
        {
            if (string.IsNullOrEmpty(_configData.ChannelID.ToString()))
            {
                _dcCommands.AddGuildCommand("plugins", this, null, nameof(PluginList));
                _dcCommands.AddGuildCommand("server", this, null, nameof(ServerCommand));
            }
            else
            {
                _dcCommands.AddGuildCommand("plugins", this, new List<Snowflake> { _configData.ChannelID }, nameof(PluginList));
                _dcCommands.AddGuildCommand("server", this, new List<Snowflake> { _configData.ChannelID }, nameof(ServerCommand));
            }
        }

        [HookMethod(DiscordExtHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            if (ready.Guilds.Count == 0)
            {
                PrintError(Lang("InviteBot"));
                _client.Disconnect();
                return;
            }
            GetGuild(ready);
            RegisterDiscordCommands();
            Puts(Lang("Connected"));
            Puts($"Connected to bot: {_client.Bot.BotUser.Username}");
        }

        private void OnDiscordGuildMembersLoaded(DiscordGuild guild)
        {
            if (string.IsNullOrEmpty(_configData.RoleId))
            {
                PrintWarning("Main Role ID Cannot be empty or Null");
                server.Command("unload DiscordServerPanel");
            }
            foreach (DiscordRole role in guild.Roles.Values)
            {
                if (role.Id == _configData.RoleId)
                {
                    _role = role;
                }
                if (!string.IsNullOrEmpty(_configData.CUSTOM.RoleId))
                {
                    if (role.Id == _configData.CUSTOM.RoleId)
                    {
                        _rolec = role;
                    }
                }
                if (!string.IsNullOrEmpty(_configData.PLUGINS.RoleId))
                {
                    if (role.Id == _configData.PLUGINS.RoleId)
                    {
                        _rolep = role;
                    }
                }
                if (!string.IsNullOrEmpty(_configData.RESTART.RoleId))
                {
                    if (role.Id == _configData.RESTART.RoleId)
                    {
                        _roler = role;
                    }
                }
                if (!string.IsNullOrEmpty(_configData.PERM.RoleId))
                {
                    if (role.Id == _configData.PERM.RoleId)
                    {
                        _roleperm = role;
                    }
                }
                if (!string.IsNullOrEmpty(_configData.WIPE.RoleId))
                {
                    if (role.Id == _configData.WIPE.RoleId)
                    {
                        _rolew = role;
                    }
                }
            }
            if (_rolec == null)
            {
                _rolec = _role;
            }
            if (_rolep == null)
            {
                _rolep = _role;
            }
            if (_roler == null)
            {
                _roler = _role;
            }
            if (_roleperm == null)
            {
                _roleperm = _role;
            }
            if (_rolew == null)
            {
                _rolew = _role;
            }
            if (_role == null)
            {
                PrintError(Lang("RoleNotFound"));
                _client.Disconnect();
                server.Command("unload DiscordAdminPanel");
            }
        }

        #endregion Discord Bot Connection

        #region Commands

        public void RegisterCommands()
        {
        }

        public void RegisterPerms()
        {
        }

        #endregion Commands

        #region DiscordCommand

        private void ServerCommand(DiscordMessage message, string cmd, string[] args)
        {
            if (!message.Member.HasRole(_role) || message.Author.Bot == true)
            {
                message.Reply(_client, CreateEmbed(Lang("NotAllowedTitle"), Lang("NotAllowedContent"), _configData.Color).Build());
                return;
            }

            DiscordEmbedBuilder Embed = CreateEmbed("Server Command", "Choose a command:", _configData.Color);
            Embed.AddField("Hostname:", $" {ConVar.Server.hostname}", true);
            if (_configData.showfps)
                Embed.AddField("FPS:", $" {Performance.report.frameRate}", true);

            Embed.AddField("Player Count:", $" {players.Connected.Count()} / {ConVar.Server.maxplayers}", true);

            if (!(ConVar.Server.gamemode == "hardcore"))
            {
                Embed.AddField("Map Seed:", $" {ConVar.Server.seed}", true);
            }
            string gamemode = string.IsNullOrEmpty(ConVar.Server.gamemode) ? "Vanilla" : ConVar.Server.gamemode;
            if (_configData.showgamemode)
                Embed.AddField("Gamemode:", $" {gamemode}", true);
            if (_configData.showplugins)
                Embed.AddField("Plugins Loaded:", $" {GetPlugins()}", true);

            List<DiscordEmbed> embeds = new List<DiscordEmbed>
            {
                Embed.Build()
            };
            MessageComponentBuilder builder = AddButtonsServer();
            MessageCreate create = new MessageCreate
            {
                Content = "Server Command",
                Embeds = embeds,
                Components = builder.Build()
            };
            DiscordChannel.GetChannel(_client, message.ChannelId, chan =>
            {
                chan.CreateMessage(_client, create);
            });
            //Log(_configData.LogFileName, "Server Command", message.Author.Username, (string)message.Author.Id);
            message.DeleteMessage(_client);
        }

        private void PluginList(DiscordMessage message, string cmd, string[] args)
        {
            if (!message.Member.HasRole(_role) || message.Author.Bot == true)
            {
                message.Reply(_client, CreateEmbed(Lang("NotAllowedTitle"), Lang("NotAllowedContent"), _configData.Color).Build());
                return;
            }

            var embed = CreateEmbed("Plugin List: ", $"Select Plugin Interaction to access plugins actions.\n You will find disabled plugins in the end of the list.", _configData.Color).Build();

            List<DiscordEmbed> embeds = new List<DiscordEmbed>
            {
                embed
            };
            MessageComponentBuilder builder = AddButtonsPluginlist();
            MessageCreate create = new MessageCreate
            {
                Content = "Plugin List Command",
                Embeds = embeds,
                Components = builder.Build()
            };
            DiscordChannel.GetChannel(_client, message.ChannelId, chan =>
            {
                chan.CreateMessage(_client, create);
            });
            Log(_configData.LogFileName, "Plugins Command", message.Author.Username, (string)message.Author.Id);
            message.DeleteMessage(_client);
        }

        #endregion DiscordCommand

        #region ProcessInteraction

        private void ProcessRestartCommand(DiscordUser author, string[] args, DiscordInteraction interaction)
        {
            GuildMember member = _guild.Members[author.Id];
            if (member == null)
            {
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.ChannelMessageWithSource,
                    Data = new InteractionCallbackData
                    {
                        Content = "Restart Command!",
                        Embeds = new List<DiscordEmbed> { CreateEmbed("Who sent the command?", "Not a valid Guild Member", _configData.Color).Build() },
                        Flags = MessageFlags.Ephemeral
                    }
                });
                return;
            }
            if (!member.HasRole(_roler) || author.Bot == true)
            {
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.ChannelMessageWithSource,
                    Data = new InteractionCallbackData
                    {
                        Content = "Restart Command!",
                        Embeds = new List<DiscordEmbed> { CreateEmbed(Lang("NotAllowedTitle"), Lang("NotAllowedContent"), _configData.Color).Build() },
                        Flags = MessageFlags.Ephemeral
                    }
                });
                return;
            }
            if (args.Length == 4)
            {
                if ((args[1] == "1") || (args[1] == "-1"))
                    server.Command($"restart {args[1]} {args[2]}");
                else
                    server.Command($"restart {ToSeconds(args[1])} {args[2]}");

                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.ChannelMessageWithSource,
                    Data = new InteractionCallbackData
                    {
                        Content = $"**Restart Command Sent Successfully!**\nReturned {consolelog}",
                        Flags = MessageFlags.Ephemeral
                    }
                });
                Log(_configData.LogFileName, "Restart Command", interaction.Member.User.Username, (string)interaction.Member.User.Id, args[1], args[2]);
                return;
            }
            interaction.CreateInteractionResponse(_client, new InteractionResponse
            {
                Type = InteractionResponseType.UpdateMessage,
            });
            MessageComponentBuilder builder = new MessageComponentBuilder();
            var embed = CreateEmbed("Restart Notice: ", null, _configData.Color);
            List<DiscordEmbed> embeds = new List<DiscordEmbed>();

            if (args.Length == 1)
            {
                embed.AddDescription("Choose a delay");
                foreach (var time in _configData.RESTART.timerestart)
                {
                    var enabletime = !BetterChatMute;
                    string timeprocessed = "";
                    if (time.ToLower().Contains("d"))
                    {
                        timeprocessed = time.Substring(0, time.IndexOf("d")) + " days";
                    }
                    if (time.ToLower().Contains("m"))
                    {
                        timeprocessed = time.Substring(0, time.IndexOf("m")) + " minutes";
                    }
                    if (time.ToLower().Contains("h"))
                    {
                        timeprocessed = time.Substring(0, time.IndexOf("h")) + " hours";
                    }

                    builder.AddActionButton(ButtonStyle.Danger, $"{timeprocessed}", $"restartdsp {time}", false, null);
                }
                builder.AddActionButton(ButtonStyle.Danger, $"Cancel Restart", $"restartdsp -1 Cancel", false, null);
                builder.AddActionButton(ButtonStyle.Danger, $"Instant Restart", $"restartdsp 1", false, null);
            }
            if (args.Length == 2)
            {
                embed.AddField("Delay of restart", $"{args[1]}", true);
                embed.AddDescription("Choose a reason");
                foreach (var reason in _configData.RESTART.RestartReasons)
                    builder.AddActionButton(ButtonStyle.Danger, $"{reason}", $"restartdsp {args[1]} {reason}", false, null);
            }
            if (args.Length == 3)
            {
                embed.AddField("Delay of restart", $"{args[1]}", true);
                embed.AddField("Reason of restart", $"{args[2]}", true);
                embed.AddDescription("Confirm your action");
                builder.AddActionButton(ButtonStyle.Danger, $"Confirm Action", $"restartdsp {args[1]} {args[2]} y", false, null);
            }
            embeds.Add(embed.Build());
            builder.AddActionButton(ButtonStyle.Danger, "Exit", "deletedsp", false, null);
            MessageCreate create = new MessageCreate
            {
                Content = "Restart Command",
                Embeds = embeds,
                Components = builder.Build()
            };
            DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
            {
                chan.CreateMessage(_client, create);
            });
        }
        
        private void ProcessWipeCommand(DiscordUser author, string[] args, DiscordInteraction interaction)
        {
            GuildMember member = _guild.Members[author.Id];
            if (member == null)
            {
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.ChannelMessageWithSource,
                    Data = new InteractionCallbackData
                    {
                        Content = "Wipe Command!",
                        Embeds = new List<DiscordEmbed> { CreateEmbed("Who sent the command?", "Not a valid Guild Member", _configData.Color).Build() },
                        Flags = MessageFlags.Ephemeral
                    }
                });
                return;
            }
            if (!member.HasRole(_rolew) || author.Bot == true)
            {
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.ChannelMessageWithSource,
                    Data = new InteractionCallbackData
                    {
                        Content = "Wipe Command!",
                        Embeds = new List<DiscordEmbed> { CreateEmbed(Lang("NotAllowedTitle"), Lang("NotAllowedContent"), _configData.Color).Build() },
                        Flags = MessageFlags.Ephemeral
                    }
                });
                return;
            }
            if (args.Length == 4)
            {

                if (args[3] == "map" || args[3] == "cancel")
                {
                    if (API_MapWipe(args[2], args[1], args[3]))
                    {
                        interaction.CreateInteractionResponse(_client, new InteractionResponse
                        {
                            Type = InteractionResponseType.ChannelMessageWithSource,
                            Data = new InteractionCallbackData
                            {
                                Content = $"**Wipe Command Sent Successfully!**",
                                Flags = MessageFlags.Ephemeral
                            }
                        });
                        Log(_configData.LogFileName, "Wipe Command", interaction.Member.User.Username, (string)interaction.Member.User.Id, args[1], args[2]);

                        return;
                    }
                }
                
                    interaction.CreateInteractionResponse(_client, new InteractionResponse
                    {
                        Type = InteractionResponseType.ChannelMessageWithSource,
                        Data = new InteractionCallbackData
                        {
                            Content = $"**Wipe Command was not sent!**\nWIPE Extension was not loaded",
                            Flags = MessageFlags.Ephemeral
                        }
                    });
                    return;
                
            }
            interaction.CreateInteractionResponse(_client, new InteractionResponse
            {
                Type = InteractionResponseType.UpdateMessage,
            });
            MessageComponentBuilder builder = new MessageComponentBuilder();
            var embed = CreateEmbed("Wipe Notice: ", null, _configData.Color);
            List<DiscordEmbed> embeds = new List<DiscordEmbed>();

            if (args.Length == 1)
            {
                embed.AddDescription("Choose a delay");
                foreach (var time in _configData.WIPE.timedelay)
                {
                    string timeprocessed = "";
                    if (time.ToLower().Contains("d"))
                    {
                        timeprocessed = time.Substring(0, time.IndexOf("d")) + " days";
                    }
                    if (time.ToLower().Contains("m"))
                    {
                        timeprocessed = time.Substring(0, time.IndexOf("m")) + " minutes";
                    }
                    if (time.ToLower().Contains("h"))
                    {
                        timeprocessed = time.Substring(0, time.IndexOf("h")) + " hours";
                    }

                    builder.AddActionButton(ButtonStyle.Danger, $"{timeprocessed}", $"wipedsp {time}", false, null);
                }
                builder.AddActionButton(ButtonStyle.Danger, $"Cancel Wipe", $"wipedsp -1 Cancel", false, null);
                builder.AddActionButton(ButtonStyle.Danger, $"Instant Wipe", $"wipedsp 1", false, null);
            }
            if (args.Length == 2)
            {
                embed.AddField("Delay of wipe", $"{args[1]}", true);
                embed.AddDescription("Choose a map seed");
                if (args[1] == "-1")
                {
                    builder.AddActionButton(ButtonStyle.Danger, $"Continue", $"wipedsp {args[1]} 0", false, null);
                }
                else
                for (int i = 0; i < _configData.WIPE.generatemaps; i++)
                {
                    uint value = (uint)_random.Next(1, 2147483647);
                    embed.AddField("Map Link : ", $"https://rustmaps.com/map/{_configData.WIPE.mapsize}_{value}", true);
                    builder.AddActionButton(ButtonStyle.Danger, $"{value}", $"wipedsp {args[1]} {value}", false, null);
                }
                    
            }
            if (args.Length == 3)
            {
                embed.AddField("Delay of wipe", $"{args[1]}", false);
                embed.AddField("Map chosen", $"{args[2]}", true);
                embed.AddDescription("Confirm your action");
                if (args[1] == "-1")
                {
                    builder.AddActionButton(ButtonStyle.Danger, $"Cancel Wipe", $"wipedsp {args[1]} {args[2]} cancel", false, null);
                }
                else
                {
                    builder.AddActionButton(ButtonStyle.Danger, $"Map Wipe", $"wipedsp {args[1]} {args[2]} map", false, null);
                    //builder.AddActionButton(ButtonStyle.Danger, $"Full Wipe", $"wipedsp {args[1]} {args[2]} full", false, null);
                }
            }
            embeds.Add(embed.Build());
            builder.AddActionButton(ButtonStyle.Danger, "Exit", "deletedsp", false, null);
            MessageCreate create = new MessageCreate
            {
                Content = "Wipe Command",
                Embeds = embeds,
                Components = builder.Build()
            };
            DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
            {
                chan.CreateMessage(_client, create);
            });
        }
        
        private void ProcessPluginCommand(DiscordUser author, string[] args, DiscordInteraction interaction)
        {
            GuildMember member = _guild.Members[author.Id];
            int arglength = _configData.PLUGINS.confirmuse ? 4 : 3;
            if (member == null)
            {
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.ChannelMessageWithSource,
                    Data = new InteractionCallbackData
                    {
                        Content = "Plugin Command!",
                        Embeds = new List<DiscordEmbed> { CreateEmbed("Who sent the command?", "Not a valid Guild Member", _configData.Color).Build() },
                        Flags = MessageFlags.Ephemeral
                    }
                });
                return;
            }
            if (!member.HasRole(_rolep) || author.Bot == true)
            {
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.ChannelMessageWithSource,
                    Data = new InteractionCallbackData
                    {
                        Content = "Plugin Command!",
                        Embeds = new List<DiscordEmbed> { CreateEmbed(Lang("NotAllowedTitle"), Lang("NotAllowedContent"), _configData.Color).Build() },
                        Flags = MessageFlags.Ephemeral
                    }
                });
                return;
            }
            if (args.Length == arglength)
            {
                if (args[2] == "o.unload")
                {
                    DisabledPlugins.Add(new PluginInfo(args[1], SearchPlugin(args[1]).Title));
                }
                if (args[2] == "o.load")
                {
                    EnablePlugin(args[1]);
                }
                server.Command($"{args[2]} {args[1]}");

                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.ChannelMessageWithSource,
                    Data = new InteractionCallbackData
                    {
                        Content = $"**{args[2]} sent on {args[1]} Successfully!**\nReturned {consolelog}",
                        Flags = MessageFlags.Ephemeral
                    }
                });
                Log(_configData.LogFileName, "Plugin Command", interaction.Member.User.Username, (string)interaction.Member.User.Id, args[1], args[2]);
                return;
            }

            MessageComponentBuilder builder = new MessageComponentBuilder();
            var embed = CreateEmbed("Plugin Command: ", null, _configData.Color);

            List<DiscordEmbed> embeds = new List<DiscordEmbed>();
            if (args.Length == 2)
            {
                embed.AddField("Plugin Name", $"{args[1]}", true);
                embed.AddDescription("Choose a command");
                if (IsPluginDisabled(args[1]))
                    builder.AddActionButton(ButtonStyle.Primary, "Load", $"plugindsp {args[1]} o.load", false, null);
                else
                {
                    builder.AddActionButton(ButtonStyle.Primary, "Reload", $"plugindsp {args[1]} o.reload", false, null);
                    builder.AddActionButton(ButtonStyle.Primary, "Unload", $"plugindsp {args[1]} o.unload", false, null);
                }
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.UpdateMessage,
                });
            }

            if ((args.Length == 3) && (_configData.PLUGINS.confirmuse))
            {
                embed.AddField("Plugin Name", $"{args[1]}", true);
                embed.AddField("Command Chosen", $"{args[2]}", true);
                embed.AddDescription("Confirm Your Choice");
                builder.AddActionButton(ButtonStyle.Success, "Confirm", $"plugindsp {args[1]} {args[2]} y", false, null);
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.UpdateMessage,
                });
            }
            embeds.Add(embed.Build());
            builder.AddActionButton(ButtonStyle.Danger, "Exit", "deletedsp", false, null);
            MessageCreate create = new MessageCreate
            {
                Content = "Plugin Command",
                Embeds = embeds,
                Components = builder.Build()
            };
            DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
            {
                chan.CreateMessage(_client, create);
            });
        }

        private void ProcessPermissionsCommand(DiscordUser author, string[] args, DiscordInteraction interaction)
        {
            GuildMember member = _guild.Members[author.Id];
            if (member == null)
            {
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.ChannelMessageWithSource,
                    Data = new InteractionCallbackData
                    {
                        Content = "Permissions Command!",
                        Embeds = new List<DiscordEmbed> { CreateEmbed("Who sent the command?", "Not a valid Guild Member", _configData.Color).Build() },
                        Flags = MessageFlags.Ephemeral
                    }
                });
                return;
            }
            if (!member.HasRole(_roleperm) || author.Bot == true)
            {
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.ChannelMessageWithSource,
                    Data = new InteractionCallbackData
                    {
                        Content = "Permissions Command!",
                        Embeds = new List<DiscordEmbed> { CreateEmbed(Lang("NotAllowedTitle"), Lang("NotAllowedContent"), _configData.Color).Build() },
                        Flags = MessageFlags.Ephemeral
                    }
                });
                return;
            }
            if (args.Length == 6)
            {
                string type = args[1];
                string action = args[3];

                switch (type)
                {
                    case "player":
                        switch (action)
                        {
                            case "grant":
                                server.Command($"o.grant user {args[2]} {args[4]}");
                                break;

                            case "revoke":
                                server.Command($"o.revoke user {args[2]} {args[4]}");
                                break;

                            case "add":
                                server.Command($"o.usergroup add {args[2]} {args[4]}");
                                break;

                            case "remove":
                                server.Command($"o.usergroup remove {args[2]} {args[4]}");
                                break;
                        }
                        break;

                    case "group":
                        switch (args[3])
                        {
                            case "grant":
                                server.Command($"o.grant group {args[2]} {args[4]}");
                                break;

                            case "revoke":
                                server.Command($"o.revoke group {args[2]} {args[4]}");
                                break;

                            case "add":
                                server.Command($"o.usergroup add {args[4]} {args[2]}");
                                break;

                            case "remove":
                                server.Command($"o.usergroup remove {args[4]} {args[2]}");
                                break;
                        }
                        break;
                }

                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.ChannelMessageWithSource,
                    Data = new InteractionCallbackData
                    {
                        Content = $"**Permissions Command Sent Successfully!**\nReturned {consolelog}",
                        Flags = MessageFlags.Ephemeral
                    }
                });
                Log(_configData.LogFileName, "Permission Command", interaction.Member.User.Username, (string)interaction.Member.User.Id, type, action, args[4], args[2]);
                return;
            }

            MessageComponentBuilder builder = new MessageComponentBuilder();
            var embed = CreateEmbed("Permissions Command: ", null, _configData.Color);

            List<DiscordEmbed> embeds = new List<DiscordEmbed>();
            //Args 1 = Player or Group
            if (args.Length == 1)
            {
                embed.AddDescription("Choose an entity type");
                builder.AddActionButton(ButtonStyle.Primary, "Players", $"permissionsdsp player", false, null);
                builder.AddActionButton(ButtonStyle.Primary, "Oxide Group", $"permissionsdsp group", false, null);
                builder.AddActionButton(ButtonStyle.Danger, "Exit", "deletedsp", false, null);
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.UpdateMessage,
                });
            }
            //Args 2 = playerid or groupname
            if (args.Length == 2)
            {
                embed.AddField("Entity Chosen", $"{args[1]}", true);
                embed.AddDescription("Choose an entity");
                builder = args[1] == "player" ? AddButtonsPlayerlist(0, args) : args[1] == "group" ? AddButtonsGrouplist(0, args) : null;
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.UpdateMessage,
                });
            }
            //Args 3 = grant or revoke or "add player to group or group to player"
            if (args.Length == 3)
            {
                embed.AddField("Entity Chosen", $"{args[1]} {args[2]}", true);
                embed.AddDescription("Choose a command");
                string buttonadd = args[1] == "player" ? "to group" : "";
                string buttonremove = args[1] == "player" ? "from group" : "";
                builder.AddActionButton(ButtonStyle.Primary, "Grant", $"permissionsdsp {args[1]} {args[2]} grant", false, null);
                builder.AddActionButton(ButtonStyle.Primary, "Revoke", $"permissionsdsp {args[1]} {args[2]} revoke", false, null);
                if (args[1] == "player")
                {
                    builder.AddActionButton(ButtonStyle.Primary, $"Add {buttonadd}", $"permissionsdsp {args[1]} {args[2]} add", false, null);
                    builder.AddActionButton(ButtonStyle.Primary, $"Remove {buttonremove}", $"permissionsdsp {args[1]} {args[2]} remove", false, null);
                }
                builder.AddActionButton(ButtonStyle.Danger, "Exit", "deletedsp", false, null);
            }
            //Args 4 = permission or affectation to affect on
            if (args.Length == 4)
            {
                embed.AddField("Entity Chosen", $"{args[1]} {args[2]}", true);
                embed.AddField("Command Chosen", $"{args[3]}", true);
                embed.AddDescription("Choose a permission/group");
                builder = args[3] == "grant" ? AddButtonsGrantlist(0, args) : args[3] == "revoke" ? AddButtonsRevokelist(0, args) : (args[3] == "add" || args[3] == "remove") ? args[1] == "player" ? AddButtonsGrouplist(0, args, args[2]) : null : null;
            }
            //Args 5 = confirmation
            if (args.Length == 5)
            {
                embed.AddField("Entity Chosen", $"{args[1]} {args[2]}", true);
                embed.AddField("Command Chosen", $"{args[3]} {args[4]}", true);
                embed.AddDescription("Confirm your action");
                builder.AddActionButton(ButtonStyle.Success, "Confirm Action", $"permissionsdsp {args[1]} {args[2]} {args[3]} {args[4]} y", false, null);
                builder.AddActionButton(ButtonStyle.Danger, "Exit", "deletedsp", false, null);
            }
            embeds.Add(embed.Build());

            MessageCreate create = new MessageCreate
            {
                Content = "Permissions Command",
                Embeds = embeds,
                Components = builder.Build()
            };
            DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
            {
                chan.CreateMessage(_client, create);
            });
        }

        private void ProcessCustomCommand(DiscordUser author, string[] args, DiscordInteraction interaction)
        {
            GuildMember member = _guild.Members[author.Id];
            int arglength = _configData.CUSTOM.confirmuse ? 3 : 2;
            if (member == null)
            {
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.ChannelMessageWithSource,
                    Data = new InteractionCallbackData
                    {
                        Content = "Custom Command!",
                        Embeds = new List<DiscordEmbed> { CreateEmbed("Who sent the command?", "Not a valid Guild Member", _configData.Color).Build() },
                        Flags = MessageFlags.Ephemeral
                    }
                });
                return;
            }
            if (!member.HasRole(_rolec) || author.Bot == true)
            {
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.ChannelMessageWithSource,
                    Data = new InteractionCallbackData
                    {
                        Content = "Custom Command!",
                        Embeds = new List<DiscordEmbed> { CreateEmbed(Lang("NotAllowedTitle"), Lang("NotAllowedContent"), _configData.Color).Build() },
                        Flags = MessageFlags.Ephemeral
                    }
                });
                return;
            }
            if (args.Length == arglength)
            {
                string cmd = _configData.CUSTOM.Commands[args[1]];
                server.Command(cmd);
                interaction.CreateInteractionResponse(_client, new InteractionResponse
                {
                    Type = InteractionResponseType.ChannelMessageWithSource,
                    Data = new InteractionCallbackData
                    {
                        Content = $"**{args[1]} Command Successfully!**\nReturned {consolelog}",
                        Flags = MessageFlags.Ephemeral
                    }
                });

                Log(_configData.LogFileName, "Custom Command", interaction.Member.User.Username, (string)interaction.Member.User.Id, args[1]);
                return;
            }
            interaction.CreateInteractionResponse(_client, new InteractionResponse
            {
                Type = InteractionResponseType.UpdateMessage,
            });
            MessageComponentBuilder builder = new MessageComponentBuilder();
            var embed = CreateEmbed("Custom Command Notice: ", $"Sending Custom Command.", _configData.Color);
            List<DiscordEmbed> embeds = new List<DiscordEmbed>
            {
                embed.Build()
            };
            if (args.Length == 1)
            {
                embed.AddDescription("Choose a command");
                foreach (var cmd in _configData.CUSTOM.Commands)
                {
                    builder.AddActionButton(ButtonStyle.Primary, $"{cmd.Key}", $"customdsp {cmd.Key}", false, null);
                }
            }
            if (args.Length == 2)
            {
                embed.AddField("Custom Command Chosen", $"{args[1]}", true);
                embed.AddDescription("Confirm your action");
                builder.AddActionButton(ButtonStyle.Success, $"Confirm {args[1]}", $"customdsp {args[1]} Yes", false, null);
            }
            builder.AddActionButton(ButtonStyle.Danger, "Exit", "deletedsp", false, null);

            MessageCreate create = new MessageCreate
            {
                Content = "Custom Command",
                Embeds = embeds,
                Components = builder.Build()
            };
            DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
            {
                chan.CreateMessage(_client, create);
            });
        }

        private void PagePlugin(DiscordUser author, string[] args, DiscordInteraction interaction, int page)
        {
            GuildMember member = _guild.Members[author.Id];
            if (member == null)
            {
                DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
                {
                    chan.CreateMessage(_client, CreateEmbed("Who sent the command?", "Not a valid Guild Member", _configData.Color).Build());
                });
                return;
            }
            if (!member.HasRole(_role) || author.Bot == true)
            {
                DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
                {
                    chan.CreateMessage(_client, CreateEmbed(Lang("NotAllowedTitle"), Lang("NotAllowedContent"), _configData.Color).Build());
                });
                return;
            }

            var embed = CreateEmbed($"Plugin List: Page {page}", $"Select Plugins Interaction to interact with it.", _configData.Color);

            List<DiscordEmbed> embeds = new List<DiscordEmbed>
            {
                embed.Build()
            };
            MessageComponentBuilder builder = AddButtonsPluginlist(page);

            MessageCreate create = new MessageCreate
            {
                Content = "Plugins Page Command",
                Embeds = embeds,
                Components = builder.Build()
            };
            DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
            {
                chan.CreateMessage(_client, create);
            });
        }

        private void PagePlayer(DiscordUser author, string[] args, DiscordInteraction interaction, int page)
        {
            GuildMember member = _guild.Members[author.Id];
            if (member == null)
            {
                DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
                {
                    chan.CreateMessage(_client, CreateEmbed("Who sent the command?", "Not a valid Guild Member", _configData.Color).Build());
                });
                return;
            }
            if (!member.HasRole(_role) || author.Bot == true)
            {
                DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
                {
                    chan.CreateMessage(_client, CreateEmbed(Lang("NotAllowedTitle"), Lang("NotAllowedContent"), _configData.Color).Build());
                });
                return;
            }

            var embed = CreateEmbed($"Player List: Page {page}", $"Select Player Interaction to interact with it.", _configData.Color);

            List<DiscordEmbed> embeds = new List<DiscordEmbed>
            {
                embed.Build()
            };
            MessageComponentBuilder builder = new MessageComponentBuilder();

            builder = AddButtonsPlayerlist(page, args.Skip(2).ToArray());

            MessageCreate create = new MessageCreate
            {
                Content = "Player Page Command",
                Embeds = embeds,
                Components = builder.Build()
            };
            DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
            {
                chan.CreateMessage(_client, create);
            });
        }

        private void PageGroup(DiscordUser author, string[] args, DiscordInteraction interaction, int page)
        {
            GuildMember member = _guild.Members[author.Id];
            if (member == null)
            {
                DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
                {
                    chan.CreateMessage(_client, CreateEmbed("Who sent the command?", "Not a valid Guild Member", _configData.Color).Build());
                });
                return;
            }
            if (!member.HasRole(_role) || author.Bot == true)
            {
                DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
                {
                    chan.CreateMessage(_client, CreateEmbed(Lang("NotAllowedTitle"), Lang("NotAllowedContent"), _configData.Color).Build());
                });
                return;
            }

            var embed = CreateEmbed($"Group List: Page {page}", $"Select Group Interaction to interact with it.", _configData.Color);

            List<DiscordEmbed> embeds = new List<DiscordEmbed>
            {
                embed.Build()
            };
            MessageComponentBuilder builder = new MessageComponentBuilder();
            string[] arg = args.Skip(2).ToArray();
            if (arg.Length == 4)
                builder = AddButtonsGrouplist(page, arg, arg[2]);
            else
                builder = AddButtonsGrouplist(page, arg);

            MessageCreate create = new MessageCreate
            {
                Content = "Group Page Command",
                Embeds = embeds,
                Components = builder.Build()
            };
            arg = null;
            DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
            {
                chan.CreateMessage(_client, create);
            });

            arg = null;
        }

        private void PagePluginPerms(DiscordUser author, string[] args, DiscordInteraction interaction, int page)
        {
            GuildMember member = _guild.Members[author.Id];
            if (member == null)
            {
                DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
                {
                    chan.CreateMessage(_client, CreateEmbed("Who sent the command?", "Not a valid Guild Member", _configData.Color).Build());
                });
                return;
            }
            if (!member.HasRole(_role) || author.Bot == true)
            {
                DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
                {
                    chan.CreateMessage(_client, CreateEmbed(Lang("NotAllowedTitle"), Lang("NotAllowedContent"), _configData.Color).Build());
                });
                return;
            }

            var embed = CreateEmbed($"Permissions List: Page {page}", $"Select Permission Interaction to interact with it.", _configData.Color);

            List<DiscordEmbed> embeds = new List<DiscordEmbed>
            {
                embed.Build()
            };
            MessageComponentBuilder builder = new MessageComponentBuilder();
            string[] arg = args.Skip(3).ToArray();
            builder = arg[3] == "grant" ? AddButtonsGrantlist(page, arg, args[1]) : AddButtonsRevokelist(page, arg, args[1]);
            MessageCreate create = new MessageCreate
            {
                Content = "Permissions Page Command",
                Embeds = embeds,
                Components = builder.Build()
            };
            arg = null;
            DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
            {
                chan.CreateMessage(_client, create);
            });
        }

        private void PagePerms(DiscordUser author, string[] args, DiscordInteraction interaction, int page)
        {
            GuildMember member = _guild.Members[author.Id];
            if (member == null)
            {
                DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
                {
                    chan.CreateMessage(_client, CreateEmbed("Who sent the command?", "Not a valid Guild Member", _configData.Color).Build());
                });
                return;
            }
            if (!member.HasRole(_role) || author.Bot == true)
            {
                DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
                {
                    chan.CreateMessage(_client, CreateEmbed(Lang("NotAllowedTitle"), Lang("NotAllowedContent"), _configData.Color).Build());
                });
                return;
            }

            var embed = CreateEmbed($"Permissions List: Page {page}", $"Select Group Interaction to interact with group.", _configData.Color);

            List<DiscordEmbed> embeds = new List<DiscordEmbed>
            {
                embed.Build()
            };
            MessageComponentBuilder builder = new MessageComponentBuilder();
            string[] arg = args.Skip(2).ToArray();
            builder = arg[3] == "grant" ? AddButtonsGrantlist(page, arg) : AddButtonsRevokelist(page, arg);
            MessageCreate create = new MessageCreate
            {
                Content = "Permissions Page Command",
                Embeds = embeds,
                Components = builder.Build()
            };
            arg = null;
            DiscordChannel.GetChannel(_client, (Snowflake)interaction.ChannelId, chan =>
            {
                chan.CreateMessage(_client, create);
            });
        }

        #endregion ProcessInteraction

        #region DiscordInteractions

        private void OnDiscordInteractionCreated(DiscordInteraction interaction)
        {
            if (interaction.Type != InteractionType.MessageComponent)
            {
                return;
            }

            if (!interaction.Data.ComponentType.HasValue || ((interaction.Data.ComponentType.Value != MessageComponentType.Button) && (interaction.Data.ComponentType.Value != MessageComponentType.SelectMenu)))
            {
                return;
            }

            if ((interaction.ChannelId != _configData.ChannelID) && !(string.IsNullOrEmpty(_configData.ChannelID.ToString())))
            {
                return;
            }
            string[] args = interaction.Data.CustomId.Split(' ');

            switch (args[0])
            {
                case "deletedsp":
                    HandleDeleteCommand(interaction);
                    break;

                case "pluginsdsp":
                    HandlePluginsCommand(interaction);
                    break;

                case "plugindsp":
                    HandlePluginCommand(interaction);
                    break;

                case "permissionsdsp":
                    HandlePermissionsCommand(interaction);
                    break;

                case "restartdsp":
                    HandleRestartCommand(interaction);
                    break;

                case "pluginpagedsp":
                    HandlePluginPageCommand(interaction);
                    break;

                case "playerpagedsp":
                    HandlePluginPageCommand(interaction);
                    break;

                case "grouppagedsp":
                    HandlePluginPageCommand(interaction);
                    break;

                case "permpagedsp":
                    HandlePermissionPageCommand(interaction);
                    break;

                case "pluginpermdsp":
                    HandlePluginPermissionPageCommand(interaction);
                    break;

                case "customdsp":
                    HandleCustomCommand(interaction);
                    break;

                case "wipedsp":
                    HandleWipeCommand(interaction);
                    break;

                default:
                    if (interaction.Data.ComponentType.Value == MessageComponentType.SelectMenu)
                    {
                        HandleMenuCommand(interaction);
                    }
                    break;
            }
        }

        #endregion DiscordInteractions

        #region HandleInteractions

        private void HandleDeleteCommand(DiscordInteraction interaction)
        {
            GuildMember member = _guild.Members[interaction.Member.User.Id];
            interaction.CreateInteractionResponse(_client, new InteractionResponse
            {
                Type = InteractionResponseType.ChannelMessageWithSource,
                Data = new InteractionCallbackData
                {
                    Content = "Exited Successfully!",
                    Flags = MessageFlags.Ephemeral
                }
            });
            if (_configData.logs.interactiondelete)
                timer.Once(1, () => interaction.Message.DeleteMessage(_client));
            else
            {
                timer.Once(2, () => DisableButtons(interaction));
            }
        }

        private void HandlePluginCommand(DiscordInteraction interaction, string Command = null)
        {
            string[] args = (Command != null) ? Command.Split(' ') : interaction.Data.CustomId.Split(' ');
            GuildMember member = _guild.Members[interaction.Member.User.Id];
            ProcessPluginCommand(interaction.Member.User, args, interaction);
            if (_configData.logs.interactiondelete)
                timer.Once(1, () => interaction.Message.DeleteMessage(_client));
            else
            {
                timer.Once(2, () => DisableButtons(interaction));
            }
        }

        private void HandlePluginsCommand(DiscordInteraction interaction, string Command = null)
        {
            string[] args = (Command != null) ? Command.Split(' ') : interaction.Data.CustomId.Split(' ');
            GuildMember member = _guild.Members[interaction.Member.User.Id];

            PagePlugin(interaction.Member.User, args, interaction, 0);
            if (_configData.logs.interactiondelete)
                timer.Once(1, () => interaction.Message.DeleteMessage(_client));
            else
            {
                timer.Once(2, () => DisableButtons(interaction));
            }
        }

        private void HandleMenuCommand(DiscordInteraction interaction)
        {
            string command = interaction.Data.Values.FirstOrDefault();

            switch (command.Split(' ')[0])
            {
                case "deletedsp":
                    HandleDeleteCommand(interaction);
                    break;

                case "pluginsdsp":
                    HandlePluginsCommand(interaction, command);
                    break;

                case "plugindsp":
                    HandlePluginCommand(interaction, command);
                    break;

                case "permissionsdsp":
                    HandlePermissionsCommand(interaction, command);
                    break;

                case "restartdsp":
                    HandleRestartCommand(interaction, command);
                    break;

                case "pluginpagedsp":
                    HandlePluginPageCommand(interaction, command);
                    break;

                case "playerpagedsp":
                    HandlePluginPageCommand(interaction, command);
                    break;

                case "grouppagedsp":
                    HandlePluginPageCommand(interaction, command);
                    break;

                case "permpagedsp":
                    HandlePermissionPageCommand(interaction, command);
                    break;

                case "pluginpermdsp":
                    HandlePluginPermissionPageCommand(interaction, command);
                    break;

                case "customdsp":
                    HandleCustomCommand(interaction, command);
                    break;
            }
        }

        private void HandlePermissionsCommand(DiscordInteraction interaction, string Command = null)
        {
            string[] args = (Command != null) ? Command.Split(' ') : interaction.Data.CustomId.Split(' ');
            GuildMember member = _guild.Members[interaction.Member.User.Id];
            ProcessPermissionsCommand(interaction.Member.User, args, interaction);
            if (_configData.logs.interactiondelete)
                timer.Once(1, () => interaction.Message.DeleteMessage(_client));
            else
            {
                timer.Once(2, () => DisableButtons(interaction));
            }
        }

        private void HandleRestartCommand(DiscordInteraction interaction, string Command = null)
        {
            string[] args = (Command != null) ? Command.Split(' ') : interaction.Data.CustomId.Split(' ');
            GuildMember member = _guild.Members[interaction.Member.User.Id];
            ProcessRestartCommand(interaction.Member.User, args, interaction);
            if (_configData.logs.interactiondelete)
                timer.Once(1, () => interaction.Message.DeleteMessage(_client));
            else
            {
                timer.Once(2, () => DisableButtons(interaction));
            }
        }
        
        private void HandleWipeCommand(DiscordInteraction interaction, string Command = null)
        {
            string[] args = (Command != null) ? Command.Split(' ') : interaction.Data.CustomId.Split(' ');
            GuildMember member = _guild.Members[interaction.Member.User.Id];
            ProcessWipeCommand(interaction.Member.User, args, interaction);
            if (_configData.logs.interactiondelete)
                timer.Once(1, () => interaction.Message.DeleteMessage(_client));
            else
            {
                timer.Once(2, () => DisableButtons(interaction));
            }
        }
        
        private void HandlePluginPageCommand(DiscordInteraction interaction, string Command = null)
        {
            string[] args = (Command != null) ? Command.Split(' ') : interaction.Data.CustomId.Split(' ');
            interaction.CreateInteractionResponse(_client, new InteractionResponse
            {
                Type = InteractionResponseType.ChannelMessageWithSource,
                Data = new InteractionCallbackData
                {
                    Content = $"Plugin Page Changed {args[1]}!",
                    Flags = MessageFlags.Ephemeral
                }
            });
            int page;
            int.TryParse(args[1], out page);
            PagePlugin(interaction.Member.User, args, interaction, page);
            if (_configData.logs.interactiondelete)
                timer.Once(1, () => interaction.Message.DeleteMessage(_client));
            else
            {
                timer.Once(2, () => DisableButtons(interaction));
            }
        }

        private void HandlePlayerPageCommand(DiscordInteraction interaction, string Command = null)
        {
            string[] args = (Command != null) ? Command.Split(' ') : interaction.Data.CustomId.Split(' ');
            interaction.CreateInteractionResponse(_client, new InteractionResponse
            {
                Type = InteractionResponseType.ChannelMessageWithSource,
                Data = new InteractionCallbackData
                {
                    Content = $"Player Page Changed {args[1]}!",
                    Flags = MessageFlags.Ephemeral
                }
            });
            int page;
            int.TryParse(args[1], out page);
            PagePlayer(interaction.Member.User, args, interaction, page);
            if (_configData.logs.interactiondelete)
                timer.Once(1, () => interaction.Message.DeleteMessage(_client));
            else
            {
                timer.Once(2, () => DisableButtons(interaction));
            }
        }

        private void HandlePermissionPageCommand(DiscordInteraction interaction, string Command = null)
        {
            string[] args = (Command != null) ? Command.Split(' ') : interaction.Data.CustomId.Split(' ');
            interaction.CreateInteractionResponse(_client, new InteractionResponse
            {
                Type = InteractionResponseType.ChannelMessageWithSource,
                Data = new InteractionCallbackData
                {
                    Content = $"Permission Page Changed {args[1]}!",
                    Flags = MessageFlags.Ephemeral
                }
            });
            int page;
            int.TryParse(args[1], out page);
            PagePerms(interaction.Member.User, args, interaction, page);
            if (_configData.logs.interactiondelete)
                timer.Once(1, () => interaction.Message.DeleteMessage(_client));
            else
            {
                timer.Once(2, () => DisableButtons(interaction));
            }
        }

        private void HandlePluginPermissionPageCommand(DiscordInteraction interaction, string Command = null)
        {
            string[] args = (Command != null) ? Command.Split(' ') : interaction.Data.CustomId.Split(' ');
            interaction.CreateInteractionResponse(_client, new InteractionResponse
            {
                Type = InteractionResponseType.ChannelMessageWithSource,
                Data = new InteractionCallbackData
                {
                    Content = $"Permission Page Changed {args[1]}!",
                    Flags = MessageFlags.Ephemeral
                }
            });
            int page;
            int.TryParse(args[2], out page);
            PagePluginPerms(interaction.Member.User, args, interaction, page);
            if (_configData.logs.interactiondelete)
                timer.Once(1, () => interaction.Message.DeleteMessage(_client));
            else
            {
                timer.Once(2, () => DisableButtons(interaction));
            }
        }

        private void HandleGroupPageCommand(DiscordInteraction interaction, string Command = null)
        {
            string[] args = (Command != null) ? Command.Split(' ') : interaction.Data.CustomId.Split(' ');
            interaction.CreateInteractionResponse(_client, new InteractionResponse
            {
                Type = InteractionResponseType.ChannelMessageWithSource,
                Data = new InteractionCallbackData
                {
                    Content = $"Player Page Changed {args[1]}!",
                    Flags = MessageFlags.Ephemeral
                }
            });
            int page;
            int.TryParse(args[1], out page);
            PageGroup(interaction.Member.User, args, interaction, page);
            if (_configData.logs.interactiondelete)
                timer.Once(1, () => interaction.Message.DeleteMessage(_client));
            else
            {
                timer.Once(2, () => DisableButtons(interaction));
            }
        }

        private void HandleCustomCommand(DiscordInteraction interaction, string Command = null)
        {
            string[] args = (Command != null) ? Command.Split(' ') : interaction.Data.CustomId.Split(' ');
            int arglength = _configData.CUSTOM.confirmuse ? 3 : 2;
            if (args.Length == 1)
            {
                ProcessCustomCommand(interaction.Member.User, args, interaction);
            }

            if (args.Length == 2)
            {
                ProcessCustomCommand(interaction.Member.User, args, interaction);
            }
            if (args.Length == 3)
            {
                ProcessCustomCommand(interaction.Member.User, args, interaction);
            }
            if (_configData.logs.interactiondelete)
                timer.Once(1, () => interaction.Message.DeleteMessage(_client));
            else
            {
                timer.Once(2, () => DisableButtons(interaction));
            }
        }

        #endregion HandleInteractions

        #region Helper Methods

        private string ToMinutes(string time)
        {
            string timeprocessed = time;
            string num = string.Empty;
            if (time.ToLower().Contains("permanent"))
            {
                return "0";
            }
            foreach (var c in time)
            {
                if (c >= '0' && c <= '9')
                {
                    num = string.Concat(num, c.ToString());
                }
                else
                {
                    break;
                }
            }

            if (time.ToLower().Contains("d"))
            {
                int process = num.ToInt() * 1440;
                return process.ToString();
            }
            if (time.ToLower().Contains("h"))
            {
                int process = num.ToInt() * 60;
                return process.ToString();
            }
            if (time.ToLower().Contains("m"))
            {
                int process = num.ToInt();
                return process.ToString();
            }
            return timeprocessed;
        }

        private string ToSeconds(string time)
        {
            return (ToMinutes(time).ToInt() * 60).ToString();
        }

        private void DisableButtons(DiscordInteraction interaction)
        {
            string button = interaction.Data.CustomId.Split(' ').Length == 2 ? interaction.Data.CustomId.Split(' ').Last() : interaction.Data.CustomId.Split(' ').First();
            string log = interaction.Message.Content + $" used by <@{interaction.Member.Id}>\n**Chosen Button** ||{button}||";
            interaction.Message.Content = log;
            MessageComponentBuilder builder = new MessageComponentBuilder();
            builder.AddActionButton(ButtonStyle.Primary, "Disabled", "None", true, null);
            interaction.Message.Components = builder.Build();

            interaction.Message.EditMessage(_client);
        }

        private string Lang(string key, string id = null) => lang.GetMessage(key, this, id);

        private MessageComponentBuilder AddButtonsServer()
        {
            MessageComponentBuilder builder = new MessageComponentBuilder();

            builder.AddActionButton(ButtonStyle.Danger, "Permissions", $"permissionsdsp", false, DiscordEmoji.FromCharacter(PermissionsButtonIcon));
            builder.AddActionButton(ButtonStyle.Danger, "Plugins", $"pluginsdsp", !_configData.PLUGINS.pluginsuse, DiscordEmoji.FromCharacter(PluginsButtonIcon));
            builder.AddActionButton(ButtonStyle.Success, "Restart", $"restartdsp", !_configData.RESTART.restartuse, DiscordEmoji.FromCharacter(RestartButtonIcon));
            builder.AddActionButton(ButtonStyle.Success, "Wipe", $"wipedsp", !(_configData.WIPE.wipeuse && (DSPWipe || WipeServer)), DiscordEmoji.FromCharacter(WipeButtonIcon));
            if (_configData.CUSTOM.customuse)
            {
                builder.AddActionButton(ButtonStyle.Success, "Custom Command", $"customdsp", !_configData.CUSTOM.customuse, DiscordEmoji.FromCharacter(CustomButtonIcon));
            }
            builder.AddActionButton(ButtonStyle.Danger, "Exit", "deletedsp", false, null);

            return builder;
        }

        public string[] GetGrantList(string perms, bool playerorgroup, string plugin = null)
        {
            string[] permissionslist = null;
            List<string> permlist = new List<string>();
            List<string> grantlist = new List<string>();
            if (playerorgroup)
                permissionslist = permission.GetUserPermissions(perms);
            else
                permissionslist = permission.GetGroupPermissions(perms);
            foreach (string pm in permission.GetPermissions())
            {
                if (!permissionslist.Contains(pm))
                    permlist.Add(pm);
            }
            if (plugin == null)
                foreach (string pm in permlist)
                {
                    if (!grantlist.Contains(pm.Split('.')[0]))
                        grantlist.Add(pm.Split('.')[0]);
                }
            else
                foreach (string pm in permlist)
                {
                    if (pm.Contains(plugin))
                        grantlist.Add(pm);
                }
            permissionslist = null;
            permlist.Clear();
            permlist = null;
            var result = grantlist.ToArray();
            grantlist.Clear();
            grantlist = null;
            return result;
        }

        public string[] GetRevokeList(string perms, bool playerorgroup, string plugin = null)
        {
            string[] permissionslist = null;
            List<string> revokelist = new List<string>();
            if (playerorgroup)
                permissionslist = permission.GetUserPermissions(perms);
            else
                permissionslist = permission.GetGroupPermissions(perms);
            if (plugin == null)
                foreach (string pm in permissionslist)
                {
                    if (!revokelist.Contains(pm.Split('.')[0]))
                        revokelist.Add(pm.Split('.')[0]);
                }
            else
                foreach (string pm in permissionslist)
                {
                    if (pm.Contains(plugin))
                        revokelist.Add(pm);
                }
            permissionslist = null;
            var result = revokelist.ToArray();
            revokelist.Clear();
            revokelist = null;
            return result;
        }

        public string[] GetAddGroup(string player)
        {
            string[] groupuserlist = null;
            List<string> finalgrouplist = new List<string>();

            groupuserlist = permission.GetUserGroups(player);
            string[] grouplist = permission.GetGroups();
            foreach (string group in grouplist)
            {
                if (!groupuserlist.Contains(group))
                {
                    finalgrouplist.Add(group);
                }
            }

            groupuserlist = null;
            grouplist = null;
            var result = finalgrouplist.ToArray();
            finalgrouplist.Clear();
            finalgrouplist = null;
            return result;
        }

        public string[] GetRemoveGroup(string player)
        {
            return permission.GetUserGroups(player);
        }

        public string[] GetRemovePlayer(string group)
        {
            return permission.GetUsersInGroup(group);
        }

        private MessageComponentBuilder AddButtonsGrantlist(int page = 0, string[] arg = null, string plugin = null)
        {
            MessageComponentBuilder builder = new MessageComponentBuilder();
            int limit = 25;

            int nextpage = page + 1;
            int previouspage = page - 1;
            int j = 0;
            string[] permissionslist = null;
            if (arg[1] == "player")
                permissionslist = GetGrantList(arg[2], true, plugin);
            else
                permissionslist = GetGrantList(arg[2], false, plugin);
            int permissionspnumber = permissionslist.Count() - (limit * page);
            int pages = permissionslist.Count() / limit;
            var selectmenu = builder.AddSelectMenu("menupermsdsp", "Select a permission from the list", 1, 1);
            var selectmenupage = builder.AddSelectMenu("menupermdsp", "Change Page or Exit", 1, 1);
            string args = "";
            if (args != null)
                args = String.Join(" ", arg);
            if (permissionspnumber < limit)
            {
                j = 1;
            }
            string pagecommand = String.IsNullOrEmpty(plugin) ? "permpagedsp" : $"pluginpermdsp {plugin}";
            if ((page == 0) && (j == 0))
            {
                for (int i = 0; i < permissionslist.Count(); i++)
                {
                    var perm = permissionslist[i];

                    if (i == limit)
                    {
                        break;
                    }
                    if (String.IsNullOrEmpty(plugin))
                        selectmenu.AddOption($"{perm}", $"pluginpermdsp {perm} 0 {args}", "", false, null);
                    else
                        selectmenu.AddOption($"{perm}", $"{args} {perm}", "", false, null);
                }
                for (int i = 0; i < pages; i++)
                    if (i != page && i != nextpage && i != previouspage)
                        selectmenupage.AddOption($"Page {i}", $"{pagecommand} {i} {args}", "", false, null);
                selectmenupage.AddOption("Next Page", $"{pagecommand} {nextpage} {args}", "-->", false, null);
            }
            else if ((page == 0) && (j == 1))
            {
                for (int i = 0; i < permissionslist.Count(); i++)
                {
                    var perm = permissionslist[i];
                    if (String.IsNullOrEmpty(plugin))
                        selectmenu.AddOption($"{perm}", $"pluginpermdsp {perm} 0 {args}", "", false, null);
                    else
                        selectmenu.AddOption($"{perm}", $"{args} {perm}", "", false, null);
                }
            }
            else if ((page > 0) && (j == 0))
            {
                for (int i = limit * page; i < permissionslist.Count(); i++)
                {
                    var perm = permissionslist[i];
                    if (i == (limit * page) + limit)
                        break;
                    if (String.IsNullOrEmpty(plugin))
                        selectmenu.AddOption($"{perm}", $"pluginpermdsp {perm} 0 {args}", "", false, null);
                    else
                        selectmenu.AddOption($"{perm}", $"{args} {perm}", "", false, null);
                }
                for (int i = 0; i < pages; i++)
                    if (i != page && i != nextpage && i != previouspage)
                        selectmenupage.AddOption($"Page {i}", $"{pagecommand} {i} {args}", "", false, null);
                selectmenupage.AddOption("Previous Page", $"{pagecommand} {previouspage} {args}", "<--", false, null);
                selectmenupage.AddOption("Next Page", $"{pagecommand} {nextpage} {args}", "-->", false, null);
            }
            else if ((page > 0) && (j == 1))
            {
                for (int i = limit * page; i < permissionslist.Count(); i++)
                {
                    var perm = permissionslist[i];
                    if (String.IsNullOrEmpty(plugin))
                        selectmenu.AddOption($"{perm}", $"pluginpermdsp {perm} 0 {args}", "", false, null);
                    else
                        selectmenu.AddOption($"{perm}", $"{args} {perm}", "", false, null);
                }
                for (int i = 0; i < pages; i++)
                    if (i != page && i != nextpage && i != previouspage)
                        selectmenupage.AddOption($"Page {i}", $"{pagecommand} {i} {args}", "", false, null);
                selectmenupage.AddOption("Previous Page", $"{pagecommand} {previouspage} {args}", "<--", false, null);
            }
            selectmenupage.AddOption("Exit", $"deletedsp", "Exit the command", false, null);
            return builder;
        }

        private MessageComponentBuilder AddButtonsRevokelist(int page = 0, string[] arg = null, string plugin = null)
        {
            MessageComponentBuilder builder = new MessageComponentBuilder();
            int limit = 25;

            int nextpage = page + 1;
            int previouspage = page - 1;
            int j = 0;
            string[] permissionslist = null;
            if (arg[1] == "player")
                permissionslist = GetRevokeList(arg[2], true, plugin);
            else
                permissionslist = GetRevokeList(arg[2], false, plugin);
            int permissionspnumber = permissionslist.Count() - (limit * page);
            int pages = permissionslist.Count() / limit;
            var selectmenu = builder.AddSelectMenu("menupermsdsp", "Select a permission from the list", 1, 1);
            var selectmenupage = builder.AddSelectMenu("menupermdsp", "Change Page or Exit", 1, 1);
            string args = "";
            if (args != null)
                args = String.Join(" ", arg);
            string pagecommand = String.IsNullOrEmpty(plugin) ? "permpagedsp" : $"pluginpermdsp {plugin}";
            if (permissionspnumber < limit)
            {
                j = 1;
            }

            if ((page == 0) && (j == 0))
            {
                for (int i = 0; i < permissionslist.Count(); i++)
                {
                    var perm = permissionslist[i];

                    if (i == limit)
                    {
                        break;
                    }

                    if (plugin == null)
                        selectmenu.AddOption($"{perm}", $"pluginpermdsp {perm} 0 {args}", "", false, null);
                    else
                        selectmenu.AddOption($"{perm}", $"{args} {perm}", "", false, null);
                }
                for (int i = 0; i < pages; i++)
                    if (i != page && i != nextpage && i != previouspage)
                        selectmenupage.AddOption($"Page {i}", $"{pagecommand} {i} {args}", "", false, null);
                selectmenupage.AddOption("Next Page", $"{pagecommand} {nextpage} {args}", "-->", false, null);
            }
            else if ((page == 0) && (j == 1))
            {
                for (int i = 0; i < permissionslist.Count(); i++)
                {
                    var perm = permissionslist[i];

                    if (plugin == null)
                        selectmenu.AddOption($"{perm}", $"pluginpermdsp {perm} 0 {args}", "", false, null);
                    else
                        selectmenu.AddOption($"{perm}", $"{args} {perm}", "", false, null);
                }
            }
            else if ((page > 0) && (j == 0))
            {
                for (int i = limit * page; i < permissionslist.Count(); i++)
                {
                    var perm = permissionslist[i];
                    if (i == (limit * page) + limit)
                        break;
                    if (plugin == null)
                        selectmenu.AddOption($"{perm}", $"pluginpermdsp {perm} 0 {args}", "", false, null);
                    else
                        selectmenu.AddOption($"{perm}", $"{args} {perm}", "", false, null);
                }
                for (int i = 0; i < pages; i++)
                    if (i != page && i != nextpage && i != previouspage)
                        selectmenupage.AddOption($"Page {i}", $"{pagecommand} {i} {args}", "", false, null);
                selectmenupage.AddOption("Previous Page", $"{pagecommand} {previouspage} {args}", "<--", false, null);
                selectmenupage.AddOption("Next Page", $"{pagecommand} {nextpage} {args}", "-->", false, null);
            }
            else if ((page > 0) && (j == 1))
            {
                for (int i = limit * page; i < permissionslist.Count(); i++)
                {
                    var perm = permissionslist[i];
                    if (plugin == null)
                        selectmenu.AddOption($"{perm}", $"pluginpermdsp {perm} 0 {args}", "", false, null);
                    else
                        selectmenu.AddOption($"{perm}", $"{args} {perm}", "", false, null);
                }
                for (int i = 0; i < pages; i++)
                    if (i != page && i != nextpage && i != previouspage)
                        selectmenupage.AddOption($"Page {i}", $"{pagecommand} {i} {args}", "", false, null);
                selectmenupage.AddOption("Previous Page", $"{pagecommand} {previouspage} {args}", "<--", false, null);
            }
            selectmenupage.AddOption("Exit", $"deletedsp", "Exit the command", false, null);
            return builder;
        }

        private MessageComponentBuilder AddButtonsGrouplist(int page = 0, string[] arg = null, string player = null)
        {
            MessageComponentBuilder builder = new MessageComponentBuilder();
            int limit = 25;

            int nextpage = page + 1;
            int previouspage = page - 1;
            int j = 0;
            string[] groupslist = null;
            if (player == null)
                groupslist = permission.GetGroups();
            else
                switch (arg[3])
                {
                    case "add":
                        groupslist = GetAddGroup(player);
                        break;

                    case "remove":
                        groupslist = GetRemoveGroup(player);
                        break;
                }

            int groupnumber = groupslist.Count() - (limit * page);
            int pages = groupslist.Count() / limit;
            var selectmenu = builder.AddSelectMenu("menugroupsdsp", "Select a group from the list", 1, 1);
            var selectmenupage = builder.AddSelectMenu("menugroupdsp", "Change Page or Exit", 1, 1);
            string args = "";
            if (args != null)
                args = String.Join(" ", arg);
            if (groupnumber < limit)
            {
                j = 1;
            }

            if ((page == 0) && (j == 0))
            {
                for (int i = 0; i < groupslist.Count(); i++)
                {
                    var group = groupslist[i];

                    if (i == limit)
                    {
                        break;
                    }

                    if (player == null)
                        selectmenu.AddOption($"{group}", $"{args} {group}", "", false, null);
                    else
                        if (group != "default")
                        selectmenu.AddOption($"{group}", $"{args} {group}", "", false, null);
                }
                for (int i = 0; i < pages; i++)
                    if (i != page && i != nextpage && i != previouspage)
                        selectmenupage.AddOption($"Page {i}", $"grouppagedsp {i} {args}", "", false, null);
                selectmenupage.AddOption("Next Page", $"grouppagedsp {nextpage} {args}", "-->", false, null);
            }
            else if ((page == 0) && (j == 1))
            {
                for (int i = 0; i < groupslist.Count(); i++)
                {
                    var group = groupslist[i];
                    if (player == null)
                        selectmenu.AddOption($"{group}", $"{args} {group}", "", false, null);
                    else
                        if (group != "default")
                        selectmenu.AddOption($"{group}", $"{args} {group}", "", false, null);
                }
            }
            else if ((page > 0) && (j == 0))
            {
                for (int i = limit * page; i < groupslist.Count(); i++)
                {
                    var group = groupslist[i];
                    if (i == (limit * page) + limit)
                        break;

                    if (player == null)
                        selectmenu.AddOption($"{group}", $"{args} {group}", "", false, null);
                    else
                        if (group != "default")
                        selectmenu.AddOption($"{group}", $"{args} {group}", "", false, null);
                }
                for (int i = 0; i < pages; i++)
                    if (i != page && i != nextpage && i != previouspage)
                        selectmenupage.AddOption($"Page {i}", $"grouppagedsp {i} {args}", "", false, null);
                selectmenupage.AddOption("Previous Page", $"grouppagedsp {previouspage} {args}", "<--", false, null);
                selectmenupage.AddOption("Next Page", $"grouppagedsp {nextpage} {args}", "-->", false, null);
            }
            else if ((page > 0) && (j == 1))
            {
                for (int i = limit * page; i < groupslist.Count(); i++)
                {
                    var group = groupslist[i];
                    if (player == null)
                        selectmenu.AddOption($"{group}", $"{args} {group}", "", false, null);
                    else
                        if (group != "default")
                        selectmenu.AddOption($"{group}", $"{args} {group}", "", false, null);
                }
                for (int i = 0; i < pages; i++)
                    if (i != page && i != nextpage && i != previouspage)
                        selectmenupage.AddOption($"Page {i}", $"grouppagedsp {i} {args}", "", false, null);
                selectmenupage.AddOption("Previous Page", $"grouppagedsp {previouspage} {args}", "<--", false, null);
            }
            selectmenupage.AddOption("Exit", $"deletedsp", "Exit the command", false, null);
            return builder;
        }

        private MessageComponentBuilder AddButtonsPlayerlist(int page = 0, string[] arg = null)
        {
            MessageComponentBuilder builder = new MessageComponentBuilder();
            int limit = 24;
            int nextpage = page + 1;
            int previouspage = page - 1;
            int j = 0;
            int i = 0;
            int playernumber = players.Connected.Count() - (limit * page);
            int pages = players.Connected.Count() / limit;

            string args = "";
            if (args != null)
                args = String.Join(" ", arg);
            if (playernumber < limit)
            {
                j = 1;
            }
            if (players.Connected.Count() == 0)
            {
                builder.AddActionButton(ButtonStyle.Secondary, "No Players Connected", "none", true, null);
                builder.AddActionButton(ButtonStyle.Danger, "Exit", "deletedsp", false, null);
            }
            else
            {
                var selectmenu = builder.AddSelectMenu("menuplayerdsp", "Select a player from the list", 1, 1);
                var selectmenupage = builder.AddSelectMenu("menuplayersdsp", "Change Page or Exit", 1, 1);
                if ((page == 0) && (j == 0))
                {
                    foreach (var target in players.Connected)
                    {
                        i++;
                        if (i == limit)
                        {
                            break;
                        }

                        selectmenu.AddOption($"{target.Name}", $"{args} {target.Id}", "", false, null);
                    }
                    for (int k = 0; k < pages; k++)
                        if (k != page && k != nextpage && k != previouspage)
                            selectmenupage.AddOption($"Page {k}", $"playerpagedsp {k} {args}", "", false, null);
                    selectmenupage.AddOption("Next Page", $"playerpagedsp {nextpage} {args}", "-->", false, null);
                }
                else if ((page == 0) && (j == 1))
                {
                    foreach (var target in players.Connected)
                    {
                        selectmenu.AddOption($"{target.Name}", $"{args} {target.Id}", "", false, null);
                    }
                }
                else if ((page > 0) && (j == 0))
                {
                    foreach (var target in players.Connected)
                    {
                        i++;
                        if (i == (limit * page) + limit)
                            break;
                        if (i > (limit * page))
                        {
                            selectmenu.AddOption($"{target.Name}", $"{args} {target.Id}", "", false, null);
                        }
                    }
                    for (int k = 0; k < pages; k++)
                        if (k != page && k != nextpage && k != previouspage)
                            selectmenupage.AddOption($"Page {k}", $"playerpagedsp {k} {args}", "", false, null);
                    selectmenupage.AddOption("Previous Page", $"playerpagedsp {previouspage} {args}", "<--", false, null);
                    selectmenupage.AddOption("Next Page", $"playerpagedsp {nextpage} {args}", "-->", false, null);
                }
                else if ((page > 0) && (j == 1))
                {
                    foreach (var target in players.Connected)
                    {
                        i++;
                        if (i > (limit * page))
                        {
                            selectmenu.AddOption($"{target.Name}", $"{args} {target.Id}", "", false, null);
                        }
                    }
                    for (int k = 0; k < pages; k++)
                        if (i != page && i != nextpage && i != previouspage)
                            selectmenupage.AddOption($"Page {i}", $"playerpagedsp {i} {args}", "", false, null);
                    selectmenupage.AddOption("Previous Page", $"playerpagedsp {previouspage} {args}", "<--", false, null);
                }

                selectmenupage.AddOption("Exit", $"deletedsp", "Exit the command", false, null);
            }

            return builder;
        }

        private MessageComponentBuilder AddButtonsPluginlist(int page = 0)
        {
            MessageComponentBuilder builder = new MessageComponentBuilder();
            int limit = 25;

            int nextpage = page + 1;
            int previouspage = page - 1;
            int j = 0;

            var pluginslist = GetPluginNames();
            int pluginnumber = pluginslist.Count - (limit * page);
            int pages = pluginslist.Count / limit;
            var selectmenu = builder.AddSelectMenu("menupluginsdsp", "Select a plugin from the list", 1, 1);
            var selectmenupage = builder.AddSelectMenu("menuplugindsp", "Change Page or Exit", 1, 1);

            if (pluginnumber < limit)
            {
                j = 1;
            }

            if ((page == 0) && (j == 0))
            {
                for (int i = 0; i < pluginslist.Count; i++)
                {
                    var plugin = pluginslist[i];

                    if (i == limit)
                    {
                        break;
                    }
                    if (DisabledPlugins.Contains(plugin))
                        selectmenu.AddOption($"{plugin.Title}", $"plugindsp {plugin.Name}", "", false, null);
                    else
                        selectmenu.AddOption($"{plugin.Title}", $"plugindsp {plugin.Name}", "", false, null);
                }
                for (int i = 0; i < pages; i++)
                    if (i != page && i != nextpage && i != previouspage)
                        selectmenupage.AddOption($"Page {i}", $"pluginpagedsp {i}", "", false, null);
                selectmenupage.AddOption("Next Page", $"pluginpagedsp {nextpage}", "-->", false, null);
            }
            else if ((page == 0) && (j == 1))
            {
                for (int i = 0; i < pluginslist.Count; i++)
                {
                    var plugin = pluginslist[i];
                    if (DisabledPlugins.Contains(plugin))
                        selectmenu.AddOption($"{plugin.Title}", $"plugindsp {plugin.Name}", "", false, null);
                    else
                        selectmenu.AddOption($"{plugin.Title}", $"plugindsp {plugin.Name}", "", false, null);
                }
            }
            else if ((page > 0) && (j == 0))
            {
                for (int i = limit * page; i < pluginslist.Count; i++)
                {
                    var plugin = pluginslist[i];
                    if (i == (limit * page) + limit)
                        break;
                    if (DisabledPlugins.Contains(plugin))
                        selectmenu.AddOption($"{plugin.Title}", $"plugindsp {plugin.Name}", "", false, null);
                    else
                        selectmenu.AddOption($"{plugin.Title}", $"plugindsp {plugin.Name}", "", false, null);
                }
                for (int i = 0; i < pages; i++)
                    if (i != page && i != nextpage && i != previouspage)
                        selectmenupage.AddOption($"Page {i}", $"pluginpagedsp {i}", "", false, null);
                selectmenupage.AddOption("Previous Page", $"pluginpagedsp {previouspage}", "<--", false, null);
                selectmenupage.AddOption("Next Page", $"pluginpagedsp {nextpage}", "-->", false, null);
            }
            else if ((page > 0) && (j == 1))
            {
                for (int i = limit * page; i < pluginslist.Count; i++)
                {
                    var plugin = pluginslist[i];
                    if (DisabledPlugins.Contains(plugin))
                        selectmenu.AddOption($"{plugin.Title}", $"plugindsp {plugin.Name}", "", false, null);
                    else
                        selectmenu.AddOption($"{plugin.Title}", $"plugindsp {plugin.Name}", "", false, null);
                }
                for (int i = 0; i < pages; i++)
                    if (i != page && i != nextpage && i != previouspage)
                        selectmenupage.AddOption($"Page {i}", $"pluginpagedsp {i}", "", false, null);
                selectmenupage.AddOption("Previous Page", $"pluginpagedsp {previouspage}", "<--", false, null);
            }
            selectmenupage.AddOption("Exit", $"deletedsp", "Exit the command", false, null);
            return builder;
        }

        private DiscordEmbedBuilder CreateEmbed(string title, string message, string color)
        {
            DiscordEmbedBuilder Embed = new DiscordEmbedBuilder();

            Embed.AddTitle(title);

            if (!string.IsNullOrEmpty(message))
            {
                Embed.AddDescription(message);
            }
            Embed.AddColor(color);
            return Embed;
        }

        private string GetFormattedSteamID(string id)
        {
            return $"[{id}](https://steamcommunity.com/profiles/{id})";
        }

        private string GetFormattedServerArmour(string id)
        {
            return $"[{id}](https://io.serverarmour.com/profile/{id})";
        }

        private IPlayer GetPlayer(string id)
        {
            return covalence.Players.FindPlayerById(id);
        }

        private BasePlayer FindPlayerByID(string Id)
        {
            return BasePlayer.Find(Id);
        }

        private DiscordUser FindUserByID(Snowflake id)
        {
            foreach (DiscordGuild guild in _client.Bot.Servers.Values)
            {
                var member = guild.Members[id];
                if (member != null)
                {
                    return member.User;
                }
            }

            return null;
        }

        private IPlayer FindPlayer(string nameorId)
        {
            foreach (var player in covalence.Players.Connected)
            {
                if (player.Id == nameorId)
                    return player;

                if (player.Name == nameorId)
                    return player;
            }

            return null;
        }

        private int GetPlugins()
        {
            var count = 0;
            foreach (var plugin in plugins.PluginManager.GetPlugins())
            {
                if (plugin.IsCorePlugin) continue;
                count++;
            }

            return count;
        }

        private List<PluginInfo> GetPluginNames()
        {
            List<PluginInfo> pluginnameslist = new List<PluginInfo>();
            foreach (Plugin plugin in plugins.PluginManager.GetPlugins())
            {
                if (plugin.IsCorePlugin) continue;
                pluginnameslist.Add(new PluginInfo(plugin.Name, plugin.Title));
            }
            foreach (var plugininfo in DisabledPlugins)
            {
                pluginnameslist.Add(plugininfo);
            }
            return pluginnameslist;
        }

        private Plugin SearchPlugin(string name)
        {
            foreach (var plugin in plugins.GetAll().ToList())
            {
                if (name == plugin.Name)
                {
                    return plugin;
                }
            }
            return null;
        }

        private void EnablePlugin(string name)
        {
            PluginInfo plugin = null;
            foreach (var plugininfo in DisabledPlugins)
            {
                if (name == plugininfo.Name)
                {
                    plugin = plugininfo;
                    return;
                }
            }
            if (plugin != null)
            {
                DisabledPlugins.Remove(plugin);
            }
        }

        private bool IsPluginDisabled(string name)
        {
            foreach (var plugininfo in DisabledPlugins)
            {
                if (name == plugininfo.Name)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion Helper Methods

        #region API
        private bool API_MapWipe(string mapseed, string restart, string action)
        {
            bool cancel = restart == "-1";
            if (DSPWipe)
            {

                DSPWipe.Call("MapWipe",_configData.WIPE.mapsize, mapseed.ToInt(), _configData.WIPE.backup, cancel);
                if (restart == "1" || restart == "-1")
                {
                    server.Command($"restart {restart} DSP WIPE");
                }
                else
                {
                    server.Command($"restart {ToSeconds(restart)} DSP WIPE");
                }
                return true;
            }
            if (WipeServer)
            {

                WipeServer.Call("API_MapWipe", _configData.WIPE.mapsize, mapseed.ToInt(), _configData.WIPE.backup, cancel);
                if (restart == "1" || restart == "-1")
                {
                    server.Command($"restart {restart} DSP WIPE");
                }
                else
                {
                    server.Command($"restart {ToSeconds(restart)} DSP WIPE");
                }
                return true;
            }
            return false;
        }
        private IPlayer API_GetPlayer(string nameOrId, IPlayer requestor)
        {
            if (nameOrId.IsSteamId())
            {
                IPlayer player = players.All.ToList().Find(p => p.Id == nameOrId);
                return player;
            }

            List<IPlayer> foundPlayers = new List<IPlayer>();

            foreach (var player in players.Connected)
            {
                if (string.Equals(player.Name, nameOrId, StringComparison.CurrentCultureIgnoreCase))
                    return player;

                if (player.Name.ToLower().Contains(nameOrId.ToLower()))
                    foundPlayers.Add(player);
            }

            switch (foundPlayers.Count)
            {
                case 0:
                    requestor.Reply(lang.GetMessage("Player Name Not Found", this, requestor.Id).Replace("{name}", nameOrId));
                    break;

                case 1:
                    return foundPlayers[0];

                default:

                    break;
            }

            return null;
        }

        #endregion API

        #region Localization

        private void Log(string filename, string key, params object[] args)
        {
            if (_configData.logs.LogToConsole)
            {
                Puts($"[{DateTime.Now}] {Lang(key, args)}");
            }

            if (_configData.logs.LogToDiscord && !string.IsNullOrEmpty(_configData.logs.logChannelID))
            {
                MessageCreate create = new MessageCreate
                {
                    Content = key,
                    Embeds = new List<DiscordEmbed> { CreateEmbed(key, Lang(key, args), _configData.Color).AddFooter(DateTime.Now.ToString(), "https://cdn.discordapp.com/attachments/847579658635051051/1033452407134359663/Refresh_icon.png").Build() },
                    Components = null
                };
                DiscordChannel.GetChannel(_client, _configData.logs.logChannelID, chan =>
                {
                    chan.CreateMessage(_client, create);
                });
            }
            LogToFile(filename, $"[{DateTime.Now}] {Lang(key, args)}", this);
        }

        private void ConsoleLog(string condition, string stackTrace, LogType type)
        {
            if (!string.IsNullOrEmpty(condition))
            {
                consolelog = condition;
            }
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoLinkerPluginTitle"] = "Something went wrong..",
                ["NoLinkerPluginContent"] = "There is no linking system loaded at the moment.\nPlease install/load DiscordAuth or DiscordCore.",
                ["NotAllowedTitle"] = "Not allowed.",
                ["NotAllowedContent"] = "You are not allowed to use this command.",
                ["InvalidIDTitle"] = "The given ID is invalid.",
                ["InvalidIDContent"] = "Please use <steamID/discordID> in arguments",
                ["ResultTitle"] = "Search Result :",
                ["NoResult"] = "No users found.",
                ["Connected"] = "Discord bot connected.",
                ["NoToken"] = "Please set the discord bot token and reload the plugin to continue.",
                ["ConfigIssue"] = "Config file issue detected. Please delete file, or check syntax and fix.",
                ["NewConfig"] = "Creating new config file.",
                ["RoleNotFound"] = "The role with the given id can not be found. Please change the role in the config file.",
                ["Plugins Command"] = "Username : {0}, ID : {1} used plugins command",
                ["Plugin Command"] = "Username : {0}, ID : {1} used {3} command on {2}",
                ["Permission Command"] = "Username : {0}, ID : {1} used permissions on {2} {5} with {3} action : {4}",
                ["Custom Command"] = "Username : {0}, DiscordID : {1} used custom command {2}",
                ["Server Command"] = "Username : {0}, DiscordID : {1} used server command",
                ["Restart Command"] = "Username : {0}, DiscordID : {1} used restart command in {2} for {3}",
                ["Wipe Command"] = "Username : {0}, DiscordID : {1} used wipe command in {2} map {3}"

            }, this, "en");
        }

        private string Lang(string key, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this), args);
        }

        #endregion Localization
    }
}