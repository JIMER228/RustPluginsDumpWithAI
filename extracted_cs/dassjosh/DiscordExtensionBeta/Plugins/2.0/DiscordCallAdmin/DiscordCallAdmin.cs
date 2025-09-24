using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using ConVar;
using Newtonsoft.Json.Converters;
using Oxide.Ext.Discord.Builders;
using Oxide.Ext.Discord.Clients;
using Oxide.Ext.Discord.Connections;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Interfaces;
using Oxide.Ext.Discord.Libraries;
using Oxide.Ext.Discord.Logging;

namespace Oxide.Plugins
{
	[Info("Discord Call Admin", "evlad", "1.0.0")]
	[Description("Creates a live chat between a specific player and Admins through Discord")]

	internal class DiscordCallAdmin : CovalencePlugin, IDiscordPlugin
	{
		#region Variables
		public DiscordClient Client { get; set; }
		
		private DiscordGuild _discordGuild;
		private DiscordUser _discordBot;
		
		private readonly BotConnection _discordSettings = new BotConnection
		{
			Intents = GatewayIntents.Guilds | GatewayIntents.GuildMembers | GatewayIntents.GuildMessages
		};

		private readonly ChannelMessagesRequest _messageRequest = new ChannelMessagesRequest { Limit = 50 };

		private readonly DiscordSubscriptions _subscriptions = GetLibrary<DiscordSubscriptions>();
		
		#endregion

		#region Config

		PluginConfig _config;

		private class PluginConfig
		{
			[JsonProperty(PropertyName = "Discord Bot Token")]
			public string ApiKey { get; set; } = string.Empty;
            
			[JsonProperty(PropertyName = "Discord Server ID (Optional if bot only in 1 guild)")]
			public Snowflake GuildId { get; set; }
			
			[JsonProperty(PropertyName = "CategoryID")]
			public Snowflake CategoryID;

			[JsonProperty(PropertyName = "ReplyCommand")]
			public string ReplyCommand = "r";

			[JsonProperty(PropertyName = "SteamProfileIcon")]
			public string SteamProfileIcon = "";

			[JsonProperty(PropertyName = "ShowAdminUsername")]
			public Boolean ShowAdminUsername = false;

			[JsonConverter(typeof(StringEnumConverter))]
			[JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
			public DiscordLogLevel ExtensionDebugging { get; set; } = DiscordLogLevel.Info;
		}

		protected override void LoadConfig()
		{
			base.LoadConfig();
			try
			{
				_config = Config.ReadObject<PluginConfig>();
				if (_config == null) throw new Exception();
				SaveConfig();
			}
			catch
			{
				PrintError("Your configuration file contains an error. Using default configuration values.");
				LoadDefaultConfig();
			}
		}

		protected override void LoadDefaultConfig() => _config = new PluginConfig();
		protected override void SaveConfig() => Config.WriteObject(_config);

		#endregion

		#region Localization
		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(new Dictionary<string, string>
			{
				["CallAdminNotAvailable"] = "/calladmin is not available yet.",
				["CallAdminSuccess"] = "[#00C851]Admins have been notified, they'll get in touch with you as fast as possible.[/#]",
				["CallAdminAlreadyCalled"] = "[#ff4444]You've already notified the admins, please wait until an admin responds.[/#]",
				["CallAdminMessageLayout"] = "[#9c0000]Admin Live Chat[/#] - <size=11>[#dadada]Reply by typing:[/#] [#bd8f8f]/{1} [message][/#]</size>\n{0}",
				["ReplyNotAvailable"] = "/{0} is not available yet.",
				["ReplyCommandUsage"] = "Usage: /{0} [message]",
				["ReplyNoLiveChatInProgress"] = "You have no live chat in progress.",
				["ReplyWaitForAdminResponse"] = "[#ff4444]Wait until an admin responds.[/#]",
				["ReplyMessageSent"] = "[#00C851]Your message has been sent to the admins[/#]\n{0}: {1}",
				["ChatClosed"] = "[#55aaff]An admin closed the live chat.[/#]",
				["NoPermission"] = "You do not have permission to use /calladmin."
			}, this);
		}

		private string GetTranslation(string key, string id = null, params object[] args) => covalence.FormatText(string.Format(lang.GetMessage(key, this, id), args));

		#endregion

		#region Initialization & Setup
		private void Init()
		{
			permission.RegisterPermission("discordcalladmin.use", this);

			if (_config.ReplyCommand.Length > 0) {
				AddCovalenceCommand(_config.ReplyCommand, "ReplyCommand");
			}
		}

		[HookMethod(DiscordExtHooks.OnDiscordClientCreated)]
		private void OnDiscordClientCreated()
		{
			if (string.IsNullOrEmpty(_config.ApiKey))
			{
				PrintWarning("Please set the Discord Bot Token and reload the plugin");
				return;
			}
			
			_discordSettings.ApiToken = _config.ApiKey;
			_discordSettings.LogLevel = _config.ExtensionDebugging;
			Client.Connect(_discordSettings);
		}

		[HookMethod(DiscordExtHooks.OnDiscordGatewayReady)]
		private void OnDiscordGatewayReady(GatewayReadyEvent ready)
		{
			if (ready.Guilds.Count == 0)
			{
				PrintError("Your bot was not found in any discord servers. Please invite it to a server and reload the plugin.");
				return;
			}

			DiscordGuild guild = null;
			if (ready.Guilds.Count == 1 && !_config.GuildId.IsValid())
			{
				guild = ready.Guilds.Values.FirstOrDefault();
			}

			if (guild == null)
			{
				guild = ready.Guilds[_config.GuildId];
			}

			if (guild == null)
			{
				PrintError("Failed to find a matching guild for the Discord Server Id. " +
				           "Please make sure your guild Id is correct and the bot is in the discord server.");
				return;
			}
                
			if (Client.Bot.Application.Flags.HasValue && !Client.Bot.Application.Flags.Value.HasFlag(ApplicationFlags.GatewayGuildMembersLimited))
			{
				PrintError($"You need to enable \"Server Members Intent\" for {Client.Bot.BotUser.Username} @ https://discord.com/developers/applications\n" +
				           $"{Name} will not function correctly until that is fixed. Once updated please reload {Name}.");
				return;
			}
            
			_discordGuild = guild;
			_discordBot = Client.Bot.BotUser;

			DiscordChannel category = _discordGuild.Channels[_config.CategoryID];
			if (category == null || category.Type != ChannelType.GuildCategory)
			{
				throw new Exception("Category with ID: \"" + _config.CategoryID + "\" doesn't exist!");
			}

			foreach (DiscordChannel channel in _discordGuild.Channels.Values)
			{
				if (channel.ParentId == _config.CategoryID)
				{
					SubscribeToChannel(channel);
				}
			}
			
			Puts($"{Title} ready!");
		}

		#endregion

		#region Helpers

		[HookMethod("StartLiveChat")]
		public bool StartLiveChat(string playerID)
		{
			BasePlayer player = GetPlayerByID(playerID);
			if (!player) {
				PrintError("Player with ID \"" + playerID + "\" wasn't found!");
				return false;
			}

			string channelName = playerID + "_" + _discordBot.Id;
			
			DiscordChannel existingChannel = _discordGuild.GetChannel(channelName);
			if (existingChannel != null) {
				PrintError("Player \"" + playerID + "\" already has an opened chat!");
				return false;
			}
			
			ChannelCreate newChannel = new ChannelCreate{
				Name = channelName,
				Type = ChannelType.GuildText,
				ParentId = _config.CategoryID
			};
			
			_discordGuild.CreateChannel(Client, newChannel).Then(channel => {
				MessageComponentBuilder builder = new MessageComponentBuilder()
					.AddLinkButton("View steam profile", $"https://steamcommunity.com/profiles/{playerID}");

				MessageCreate createMessage = new MessageCreate
				{
					Content = $"@here New chat opened!\nYou are now talking to `{player.displayName}`",
					Components = builder.Build()
				};
				
				channel.CreateMessage(Client, createMessage);
			});

			return true;
		}


		[HookMethod("StopLiveChat")]
		public void StopLiveChat(string playerID, string reason = null)
		{
			DiscordChannel channel = _discordGuild.GetChannel(playerID + "_" + _discordBot.Id);
			if (channel == null)
				return;

			DeleteChannel(channel, reason);
		}

		private void DeleteChannel(DiscordChannel channel, string reason = null)
		{
			channel.CreateMessage(Client, "Closing the chat in 5 seconds..." + (reason != null ? "\nReason: " + reason : ""));
			
			timer.Once(5f, () =>
			{
				channel.Delete(Client);
			});
		}

		private void SubscribeToChannel(DiscordChannel channel)
		{
			string[] channelName = channel.Name.Split('_');
			if (channelName.Length != 2 || channelName[1] != _discordBot.Id)
				return;

			_subscriptions.AddChannelSubscription(Client, channel.Id, message =>
			{
				if (message.Content == "!close") {
					DeleteChannel(channel);
					return;
				}

				string messageContent = "";
				if (_config.ShowAdminUsername) {
					messageContent += "[#c9c9c9]" + message.Author.Username + ": [/#]";
				}
				messageContent += message.Content;
				if (!SendMessageToPlayerID(channelName[0], GetTranslation("CallAdminMessageLayout", channelName[0], messageContent, _config.ReplyCommand))) {
					DeleteChannel(channel, "User is not connected");
				}

				message.CreateReaction(Client, "✅");
			});
		}

		private BasePlayer GetPlayerByID(string ID)
		{
			try {
				return BasePlayer.FindByID(Convert.ToUInt64(ID));
			} catch {
				return null;
			}
		}

		private bool SendMessageToPlayerID(string playerID, string message)
		{
			BasePlayer player = GetPlayerByID(playerID);
			if (player == null)
				return false;
			
			player.Command("chat.add", Chat.ChatChannel.Server, _config.SteamProfileIcon, message);
			return true;
		}

		#endregion

		#region Events

		[HookMethod(DiscordExtHooks.OnDiscordGuildChannelCreated)]
		private void OnDiscordGuildChannelCreated(DiscordChannel channel)
		{
			if (channel.ParentId == _config.CategoryID)
				SubscribeToChannel(channel);
		}

		[HookMethod(DiscordExtHooks.OnDiscordGuildChannelDeleted)]
		private void OnDiscordGuildChannelDeleted(DiscordChannel channel)
		{
			if (channel.ParentId == _config.CategoryID) {
				string[] channelName = channel.Name.Split('_');
				if (channelName.Length != 1 && channelName[1] != _discordBot.Id)
					return;

				SendMessageToPlayerID(channelName[0], GetTranslation("ChatClosed", channelName[0]));
			}
		}

		private void OnPlayerDisconnected(BasePlayer player, string reason)
		{
			if (player == null) return;
			StopLiveChat(player.UserIDString, reason);
		}

		#endregion

		#region Commands

		[Command("calladmin")]
		private void CallAdminCommand(IPlayer player, string command, string[] args)
		{
			if (!player.HasPermission("discordcalladmin.use"))
			{
				player.Reply(GetTranslation("NoPermission", player.Id));
				return;
			}
			if (_discordGuild == null) {
				player.Reply(GetTranslation("CallAdminNotAvailable", player.Id));
				return;
			}
			SendMessageToPlayerID(
				player.Id,
				GetTranslation(
					StartLiveChat(player.Id) ?
						"CallAdminSuccess" :
						"CallAdminAlreadyCalled", player.Id
				)
			);
		}

		private void ReplyCommand(IPlayer player, string command, string[] args)
		{
			if (_discordGuild == null) {
				player.Reply(GetTranslation("ReplyNotAvailable", player.Id, _config.ReplyCommand));
				return;
			}
			if (args.Length < 1) {
				player.Reply(GetTranslation("ReplyCommandUsage", player.Id, _config.ReplyCommand));
				return;
			}
			DiscordChannel replyChannel = _discordGuild.GetChannel(player.Id + "_" + _discordBot.Id);
			string sentMessage = string.Join(" ", args);

			if (replyChannel == null) {
				SendMessageToPlayerID(player.Id, GetTranslation("ReplyNoLiveChatInProgress", player.Id));
				return;
			}

			replyChannel.GetMessages(Client, _messageRequest).Then(messages =>
			{
				if (messages.Count < 2) {
					SendMessageToPlayerID(player.Id, GetTranslation("ReplyWaitForAdminResponse", player.Id));
					return;
				}

				DateTime now = DateTime.Now;
				replyChannel.CreateMessage(Client, $"({now.Hour.ToString() + ":" + now.Minute.ToString()}) {player.Name}: {sentMessage}");
				SendMessageToPlayerID(player.Id, GetTranslation("ReplyMessageSent", player.Id, player.Name, sentMessage));
			});
		}

		#endregion
	}
}