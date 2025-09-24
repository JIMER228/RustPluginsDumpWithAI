using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord.Clients;
using Oxide.Ext.Discord.Connections;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Interfaces;
using Oxide.Ext.Discord.Libraries;
using Oxide.Ext.Discord.Logging;
#if RUST 
    using Network;
    using Net = Network.Net;
using UnityEngine;
#endif

namespace Oxide.Plugins
{
    [Info("Discord PM", "MJSU", "2.0.4")]
    [Description("Allows private messaging through discord")]
    internal class DiscordPM : CovalencePlugin, IDiscordPlugin
    {
        #region Class Fields
        public DiscordClient Client { get; set; }
        
        private PluginConfig _pluginConfig; //Plugin Config

        private const string AccentColor = "de8732";

        private readonly Hash<IPlayer, IPlayer> _replies = new Hash<IPlayer, IPlayer>();

        private readonly DiscordCommand _dcCommands = Interface.Oxide.GetLibrary<DiscordCommand>();
        private readonly BotConnection  _discordSettings = new BotConnection
        {
            Intents = GatewayIntents.Guilds | GatewayIntents.GuildMembers
        };
        
        private char _cmdPrefix;

#if RUST
        private readonly Effect _effect = new Effect();
#endif
        #endregion

        #region Setup & Loading
        private void Init()
        {
            RegisterChatLangCommand(nameof(DiscordPmChatCommand), LangKeys.ChatPmCommand);
            RegisterChatLangCommand(nameof(DiscordPmChatReplyCommand), LangKeys.ChatReplyCommand);
            
            _cmdPrefix = _dcCommands.CommandPrefixes[0];

            _discordSettings.ApiToken = _pluginConfig.DiscordApiKey;
            _discordSettings.LogLevel = _pluginConfig.ExtensionDebugging;

            if (_pluginConfig.AllowInDm)
            {
                _discordSettings.Intents |= GatewayIntents.DirectMessages;
            }
            
            if (_pluginConfig.AllowInGuild)
            {
                _discordSettings.Intents |= GatewayIntents.GuildMessages;
            }
        }
        
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.ChatFormat] = $"[#BEBEBE][[#{AccentColor}]{Title}[/#]] {{0}}[/#]",
                [LangKeys.MessageFormat] = $"[#BEBEBE][#{AccentColor}]PM {{0}} {{1}}:[/#] {{2}}[/#]",
                [LangKeys.InvalidPmSyntax] = $"Invalid Syntax. Type [#{AccentColor}]{{0}}{{1}} MJSU Hi![/#]",
                [LangKeys.InvalidReplySyntax] = $"Invalid Syntax. Ex: [#{AccentColor}]{{0}}{{1}} Hi![/#]",
                [LangKeys.NoPreviousPm] = "You do not have any previous discord PM's. Please use /pm to be able to use this command.",
                [LangKeys.NoPlayersFound] = "No players found with the name '{0}'",
                [LangKeys.MultiplePlayersFound] = "Multiple players found with the name '{0}'.",
                [LangKeys.From] = "from",
                [LangKeys.To] = "to",
                
                [LangKeys.ChatPmCommand] = "pm",
                [LangKeys.ChatReplyCommand] = "r",
                [LangKeys.DiscordPmCommand] = "pm",
                [LangKeys.DiscordReplyCommand] = "r",
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
            config.AllowedChannels = config.AllowedChannels ?? new List<Snowflake>();
            return config;
        }

        private void OnServerInitialized()
        {
            if (string.IsNullOrEmpty(_pluginConfig.DiscordApiKey))
            {
                PrintWarning("Please set the Discord Bot Token and reload the plugin");
                return;
            }
            
            Client.Connect(_discordSettings);

            RegisterDiscordLangCommand(nameof(DiscordPmMessageCommand), LangKeys.DiscordPmCommand, _pluginConfig.AllowInDm, _pluginConfig.AllowInGuild,  _pluginConfig.AllowedChannels);
            RegisterDiscordLangCommand(nameof(DiscordPmReplyMessageCommand), LangKeys.DiscordReplyCommand, _pluginConfig.AllowInDm, _pluginConfig.AllowInGuild, _pluginConfig.AllowedChannels);
            
#if RUST
            if (_pluginConfig.EnableEffectNotification)
            {
                _effect.Init(Effect.Type.Generic, Vector3.zero, Vector3.zero);
                _effect.pooledString = _pluginConfig.EffectNotification;
                _effect.pooledstringid = StringPool.Get(_pluginConfig.EffectNotification);
            
                if (_effect.pooledstringid == 0U)
                {
                    PrintWarning("Effect is not pooled: " + _effect.pooledString);
                }
            }
#endif
        }

        [HookMethod(DiscordExtHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            DiscordApplication app = Client.Bot.Application;
            if (!app.HasApplicationFlag(ApplicationFlags.GatewayGuildMembersLimited) && !app.HasApplicationFlag(ApplicationFlags.GatewayGuildMembers))
            {
                PrintError($"You need to enable \"Server Members Intent\" for {Client.Bot.BotUser.Username} @ https://discord.com/developers/applications\n" +
                           $"{Name} will not function correctly until that is fixed. Once updated please reload {Name}.");
                return;
            }

            if (!app.HasApplicationFlag(ApplicationFlags.GatewayMessageContentLimited))
            {
                PrintWarning($"You will need to enable \"Message Content Intent\" for {Client.Bot.BotUser.Username} @ https://discord.com/developers/applications\n by April 2022" +
                             $"{Name} will stop function correctly after that date until that is fixed. Once updated please reload {Name}.");
            }
            
            Puts($"{Title} Ready");
        }
        #endregion

        #region Chat Commands
        private void DiscordPmChatCommand(IPlayer player, string cmd, string[] args)
        {
            if (args.Length < 2)
            {
                Chat(player, Lang(LangKeys.InvalidPmSyntax, player, "/", Lang(LangKeys.ChatPmCommand, player)));
                return;
            }

            object search = FindPlayer(args[0]);
            if (search is string)
            {
                Chat(player, Lang((string) search, player, args[0]));
                return;
            }

            IPlayer searchedPlayer = search as IPlayer;
            if (searchedPlayer == null)
            {
                Chat(player, Lang(LangKeys.NoPlayersFound, player, args[0]));
                return;
            }

            _replies[player] = searchedPlayer;
            _replies[searchedPlayer] = player;

            string message = string.Join(" ", args.Skip(1).ToArray());
            
            SendPrivateMessage(player, searchedPlayer, message);
        }

        private void DiscordPmChatReplyCommand(IPlayer player, string cmd, string[] args)
        {
            if (args.Length < 1)
            {
                Chat(player, Lang(LangKeys.InvalidReplySyntax, player, "/", Lang(LangKeys.ChatReplyCommand, player)));
                return;
            }

            IPlayer target = _replies[player];
            if (target == null)
            {
                Chat(player, Lang(LangKeys.NoPreviousPm, player));
                return;
            }

            string message = string.Join(" ", args);
            SendPrivateMessage(player, target, message);
        }
        #endregion

        #region Discord Chat Commands
        private void DiscordPmMessageCommand(DiscordMessage message, string cmd, string[] args)
        {
            IPlayer player = message.Author.Player;
            if (player == null)
            {
                message.Reply(Client, "You cannot use this command until you're have linked your game and discord accounts.");
                return;
            }
            
            if (args.Length < 2)
            {
                message.Reply(Client, GetDiscordFormattedMessage(LangKeys.InvalidPmSyntax, player, _cmdPrefix, Lang(LangKeys.DiscordPmCommand, player)));
                return;
            }

            object searchedPlayer = FindPlayer(args[0]);
            if (searchedPlayer is string)
            {
                message.Reply(Client, GetDiscordFormattedMessage((string) searchedPlayer, player, args[0]));
                return;
            }

            IPlayer target = searchedPlayer as IPlayer;
            if (target == null)
            {
                message.Reply(Client, GetDiscordFormattedMessage(LangKeys.NoPlayersFound, player, args[0]));
                return;
            }

            _replies[player] = target;
            _replies[target] = player;

            string content = string.Join(" ", args.Skip(1).ToArray());
            SendPrivateMessage(player, target, content);
        }

        private void DiscordPmReplyMessageCommand(DiscordMessage message, string cmd, string[] args)
        {
            IPlayer player = message.Author.Player;
            if (player == null)
            {
                message.Reply(Client, "You cannot use this command until you're have linked your game and discord accounts.");
                return;
            }
            
            if (args.Length < 1)
            {
                message.Reply(Client, GetDiscordFormattedMessage(LangKeys.InvalidReplySyntax, player, _cmdPrefix, Lang(LangKeys.DiscordReplyCommand, player)));
                return;
            }

            IPlayer target = _replies[player];
            if (target == null)
            {
                message.Reply(Client, GetDiscordFormattedMessage(LangKeys.NoPreviousPm, player));
                return;
            }

            string content = string.Join(" ", args.ToArray());
            SendPrivateMessage(player, target, content);
        }
        #endregion
        
        #region Helpers
        private object FindPlayer(string name)
        {
            List<IPlayer> foundPlayers = new List<IPlayer>();
            foreach (IPlayer player in players.Connected)
            {
                if (player.Name.Contains(name, CompareOptions.OrdinalIgnoreCase))
                {
                    foundPlayers.Add(player);
                }
            }

            if (foundPlayers.Count == 0)
            {
                foreach (IPlayer player in players.All)
                {
                    if (player.Name.Contains(name, CompareOptions.OrdinalIgnoreCase))
                    {
                        foundPlayers.Add(player);
                    }
                }
            }

            if (foundPlayers.Count == 1)
            {
                return foundPlayers[0];
            }

            List<IPlayer> linkedPlayers = foundPlayers.Where(p => p.IsLinked()).ToList();
            if (linkedPlayers.Count == 1)
            {
                return linkedPlayers[0];
            }

            if (foundPlayers.Count > 1)
            {
                return LangKeys.MultiplePlayersFound;
            }

            return LangKeys.NoPlayersFound;
        }
        
        public string GetDiscordFormattedMessage(string key, IPlayer player = null, params object[] args)
        {
            return Formatter.ToPlaintext(Lang(LangKeys.ChatFormat, player, Lang(key, player, args)));
        }

        public void Chat(IPlayer player, string key, params object[] args)
        {
            if (player.IsConnected)
            {
                player.Reply(Lang(LangKeys.ChatFormat, player, Lang(key, player, args)));
            }
        }

        public void SendPrivateMessage(IPlayer player, IPlayer target, string message)
        {
            ServerPrivateMessage(player, target, message, Lang(LangKeys.To, player));
            DiscordPrivateMessage(player, target, message, Lang(LangKeys.To, player));
            ServerPrivateMessage(target, player, message, Lang(LangKeys.From, target));
            DiscordPrivateMessage(target, player, message, Lang(LangKeys.From, target));
            Puts($"{player.Name} -> {target.Name}: {message}");
            SendEffectToPlayer(player);
            SendEffectToPlayer(target);
        }

#if RUST
        private void SendEffectToPlayer(IPlayer player)
        {
            if (!_pluginConfig.EnableEffectNotification)
            {
                return;
            }
            
            if (!player.IsConnected)
            {
                return;
            }
            
            BasePlayer basePlayer = player.Object as BasePlayer;
            if (basePlayer == null)
            {
                return;
            }

            NetWrite write = Net.sv.StartWrite();
            
            write.PacketID(Message.Type.Effect);

            _effect.entity = basePlayer.net.ID;
            _effect.worldPos = basePlayer.transform.position;
            _effect.WriteToStream(write);
            write.Send(new SendInfo(basePlayer.Connection));
        }
#endif

        private void ServerPrivateMessage(IPlayer player, IPlayer target, string format, string prefix)
        {
            if (player.IsConnected)
            {
                player.Reply(Lang(LangKeys.MessageFormat, player, prefix, target.Name, format));
            }
        }

        private void SendMessage(IPlayer player, string message)
        {
            player.SendDiscordMessage(Client, message);
        }
        
        private void DiscordPrivateMessage(IPlayer player, IPlayer target, string format, string prefix)
        {
            if (Client?.Bot.Initialized ?? false)
            {
                player.SendDiscordMessage(Client,  Formatter.ToPlaintext(Lang(LangKeys.MessageFormat, player, prefix, target.Name, format)));
            }
        }
        
        public void RegisterChatLangCommand(string command, string langKey)
        {
            foreach (string langType in lang.GetLanguages(this))
            {
                Dictionary<string, string> langKeys = lang.GetMessages(langType, this);
                string commandValue;
                if (langKeys.TryGetValue(langKey, out commandValue) && !string.IsNullOrEmpty(commandValue))
                {
                    AddCovalenceCommand(commandValue, command);
                }
            }
        }
        
        public void RegisterDiscordLangCommand(string command, string langKey, bool direct, bool guild, List<Snowflake> allowedChannels)
        {
            if (direct)
            {
                _dcCommands.AddDirectMessageLocalizedCommand(langKey, this, command);
            }

            if (guild)
            {
                _dcCommands.AddGuildLocalizedCommand(langKey, this, allowedChannels, command);
            }
        }

        public string Lang(string key, IPlayer player = null, params object[] args)
        {
            try
            {
                return string.Format(lang.GetMessage(key, this, player?.Id), args);
            }
            catch(Exception ex)
            {
                PrintError($"Lang Key '{key}' threw exception\n:{ex}");
                throw;
            }
        }
        #endregion

        #region Classes
        private class PluginConfig
        {
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string DiscordApiKey { get; set; }
            
            [DefaultValue(true)]
            [JsonProperty(PropertyName = "Allow Discord Commands In Direct Messages")]
            public bool AllowInDm { get; set; }

            [DefaultValue(false)]
            [JsonProperty(PropertyName = "Allow Discord Commands In Guild")]
            public bool AllowInGuild { get; set; }
            
            [JsonProperty(PropertyName = "Allow Guild Commands Only In The Following Guild Channel Or Category (Channel ID Or Category ID)")]
            public List<Snowflake> AllowedChannels { get; set; }

#if RUST
            [DefaultValue(true)]
            [JsonProperty(PropertyName = "Enable Effect Notification")]
            public bool EnableEffectNotification { get; set; }
            
            [DefaultValue("assets/prefabs/locks/keypad/effects/lock.code.lock.prefab")]
            [JsonProperty(PropertyName = "Notification Effect")]
            public string EffectNotification { get; set; }
#endif
            
            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(DiscordLogLevel.Info)]
            [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public DiscordLogLevel ExtensionDebugging { get; set; }
        }

        private static class LangKeys
        {
            public const string ChatFormat = "Chat";
            public const string MessageFormat = "DiscordChatPrefixV1";
            public const string InvalidPmSyntax = "InvalidPmSyntaxV1";
            public const string InvalidReplySyntax = "InvalidReplySyntaxV1";
            public const string NoPreviousPm = "NoPreviousPm";
            public const string MultiplePlayersFound = "MultiplePlayersFound";
            public const string NoPlayersFound = "NoPlayersFound";
            public const string From = "From";
            public const string To = "To";


            public const string ChatPmCommand = "Commands.Chat.PM";
            public const string ChatReplyCommand = "Commands.Chat.Reply";
            public const string DiscordPmCommand = "Commands.Discord.PM";
            public const string DiscordReplyCommand = "Commands.Discord.Reply";
        }
        #endregion
    }
}