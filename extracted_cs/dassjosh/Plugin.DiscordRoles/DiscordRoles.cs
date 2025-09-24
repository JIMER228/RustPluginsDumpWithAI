using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Builders;
using Oxide.Ext.Discord.Cache;
using Oxide.Ext.Discord.Callbacks;
using Oxide.Ext.Discord.Clients;
using Oxide.Ext.Discord.Connections;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Extensions;
using Oxide.Ext.Discord.Interfaces;
using Oxide.Ext.Discord.Libraries;
using Oxide.Ext.Discord.Logging;
using Oxide.Ext.Discord.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Random = Oxide.Core.Random;

//DiscordRoles created with PluginMerge v(1.0.10.0) by MJSU @ https://github.com/dassjosh/Plugin.Merge
namespace Oxide.Plugins
{
    [Info("Discord Roles", "MJSU", "3.0.0")]
    [Description("Syncs players oxide group with discord roles")]
    public partial class DiscordRoles : CovalencePlugin, IDiscordPlugin, IDiscordPool
    {
        #region Plugins\DiscordRoles.Commands.cs
        [Command("dcr.forcecheck")]
        private void HandleCommand(IPlayer player, string cmd, string[] args)
        {
            Logger.Info("Begin checking all players");
            CheckLinkedPlayers();
        }
        #endregion

        #region Plugins\DiscordRoles.Config.cs
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }
        
        protected override void LoadConfig()
        {
            base.LoadConfig();
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _config = AdditionalConfig(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_config);
        }
        
        private PluginConfig AdditionalConfig(PluginConfig config)
        {
            config.EventSettings = new EventSettings(config.EventSettings);
            config.LogSettings = new LogSettings(config.LogSettings);
            config.ConflictSettings = new ConflictSettings(config.ConflictSettings);
            
            config.SyncSettings ??= new List<SyncSettings>
            {
                new("Default", default, SyncMode.Server),
                new("VIP", default, SyncMode.Discord)
            };
            
            config.PriorityGroupSettings ??= new List<PriorityGroupSettings>
            {
                new(null)
            };
            
            config.Nickname = new NicknameSettings(config.Nickname);
            
            for (int index = 0; index < config.SyncSettings.Count; index++)
            {
                config.SyncSettings[index] = new SyncSettings(config.SyncSettings[index]);
            }
            
            for (int index = 0; index < config.PriorityGroupSettings.Count; index++)
            {
                config.PriorityGroupSettings[index] = new PriorityGroupSettings(config.PriorityGroupSettings[index]);
            }
            
            return config;
        }
        #endregion

        #region Plugins\DiscordRoles.DiscordCommands.cs
        public void RegisterCommands()
        {
            ApplicationCommandBuilder builder = new ApplicationCommandBuilder("roles", "Discord Roles Command", ApplicationCommandType.ChatInput)
            .AddDefaultPermissions(PermissionFlags.Administrator);
            
            AddPlayerSyncCommand(builder);
            AddUserSyncCommand(builder);
            
            CommandCreate cmd = builder.Build();
            DiscordCommandLocalization loc = builder.BuildCommandLocalization();
            
            TemplateKey command = new("Roles");
            
            _localizations.RegisterCommandLocalizationAsync(this, command, loc, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0)).Then(_ => { _localizations.ApplyCommandLocalizationsAsync(this, cmd, command).Then(() => { Client.Bot.Application.CreateGlobalCommand(Client, builder.Build()); }); });
        }
        
        public void AddPlayerSyncCommand(ApplicationCommandBuilder builder)
        {
            builder.AddSubCommand("player", "Sync Oxide Player", sub =>
            {
                sub.AddOption(CommandOptionType.String, "player", "Player to Sync", options => options.Required().AutoComplete());
            });
        }
        
        public void AddUserSyncCommand(ApplicationCommandBuilder builder)
        {
            builder.AddSubCommand("user", "Sync Discord User", sub =>
            {
                sub.AddOption(CommandOptionType.User, "user", "User to Sync",
                options => options.Required());
            });
        }
        
        [DiscordApplicationCommand("roles", "player")]
        private void HandlePlayerCommand(DiscordInteraction interaction, InteractionDataParsed parsed)
        {
            string playerId = parsed.Args.GetString("player");
            IPlayer player = players.FindPlayerById(playerId);
            DiscordUser user = player?.GetDiscordUser();
            if (user == null)
            {
                interaction.CreateTemplateResponse(Client, InteractionResponseType.ChannelMessageWithSource, TemplateKeys.Commands.Player.Errors.NotLinked, null, GetDefault(player));
                return;
            }
            
            QueueSync(new PlayerSyncRequest(player, user.Id, SyncEvent.None, false));
            interaction.CreateTemplateResponse(Client, InteractionResponseType.ChannelMessageWithSource, TemplateKeys.Commands.Player.Queued, null, GetDefault(player, user));
        }
        
        [DiscordApplicationCommand("roles", "user")]
        private void HandleUserCommand(DiscordInteraction interaction, InteractionDataParsed parsed)
        {
            DiscordUser user = parsed.Args.GetUser("user");
            IPlayer player = user?.Player;
            if (player == null)
            {
                interaction.CreateTemplateResponse(Client, InteractionResponseType.ChannelMessageWithSource, TemplateKeys.Commands.User.Errors.NotLinked, null, GetDefault(user));
                return;
            }
            
            QueueSync(new PlayerSyncRequest(player, user.Id, SyncEvent.None, false));
            interaction.CreateTemplateResponse(Client, InteractionResponseType.ChannelMessageWithSource, TemplateKeys.Commands.User.Queued, null, GetDefault(player, user));
        }
        
        [DiscordAutoCompleteCommand("roles", "player", "player")]
        private void HandleNameAutoComplete(DiscordInteraction interaction, InteractionDataOption focused)
        {
            string search = focused.GetString();
            InteractionAutoCompleteBuilder response = interaction.GetAutoCompleteBuilder();
            response.AddAllOnlineFirstPlayers(search, PlayerNameFormatter.ClanName);
            interaction.CreateResponse(Client, response);
        }
        #endregion

        #region Plugins\DiscordRoles.DiscordHooks.cs
        [HookMethod(DiscordExtHooks.OnDiscordGatewayReady)]
        private void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            if (ready.Guilds.Count == 0)
            {
                PrintError("Your bot was not found in any discord servers. Please invite it to a server and reload the plugin.");
                return;
            }
            
            Guild = null;
            if (ready.Guilds.Count == 1 && !_config.GuildId.IsValid())
            {
                Guild = ready.Guilds.Values.FirstOrDefault();
            }
            
            if (Guild == null)
            {
                Guild = ready.Guilds[_config.GuildId];
                if (Guild == null)
                {
                    PrintError("Failed to find a matching guild for the Discord Server Id. " +
                    "Please make sure your guild Id is correct and the bot is in the discord server.");
                }
            }
            
            DiscordApplication app = Client.Bot.Application;
            if (!app.HasApplicationFlag(ApplicationFlags.GatewayGuildMembersLimited) && !app.HasApplicationFlag(ApplicationFlags.GatewayGuildMembers))
            {
                PrintError($"You need to enable \"Server Members Intent\" for {Client.Bot.BotUser.Username} @ https://discord.com/developers/applications\n" +
                $"{Name} will not function correctly until that is fixed. Once updated please reload {Name}.");
                return;
            }
            
            SubscribeAll();
            RegisterTemplates();
            RegisterCommands();
        }
        
        [HookMethod(DiscordExtHooks.OnDiscordGuildCreated)]
        private void OnDiscordGuildCreated(DiscordGuild guild)
        {
            if (guild.Id != Guild?.Id)
            {
                return;
            }
            
            ValidateSyncs();
            ValidateNickname();
            RegisterSyncs();
        }
        
        public bool ProcessSyncSetting(BaseSyncSettings data, DiscordRole botMaxRole)
        {
            if (!permission.GroupExists(data.GroupName))
            {
                Logger.Warning("Server group does not exist: '{0}'. Please create the group or correct the name in the config and reload the plugin.", data.GroupName);
                return false;
            }
            
            DiscordRole role = Guild.Roles[data.RoleId];
            if (role == null)
            {
                Logger.Warning("Discord Role ID does not exist: '{0}'. Please fix the role ID in the config and reload the plugin", data.RoleId);
                return false;
            }
            
            data.Role = role;
            if (botMaxRole != null && role.Position > botMaxRole.Position)
            {
                Logger.Warning("Discord Role '{0}' has a role position of {1} which is higher than the highest bot role {2} with position {3}. The bot will not be able to grant this role until this is fixed.", role.Name, role.Position, botMaxRole.Name, botMaxRole.Position);
            }
            
            if (!ProcessRoles.Contains(data.RoleId))
            {
                ProcessRoles.Add(data.RoleId);
            }
            
            return true;
        }
        
        [HookMethod(DiscordExtHooks.OnDiscordGuildMembersLoaded)]
        private void OnDiscordGuildMembersLoaded(DiscordGuild guild)
        {
            if (guild.Id != Guild?.Id)
            {
                return;
            }
            
            if (_config.EventSettings.IsAnyEnabled(SyncEvent.PluginLoaded))
            {
                timer.In(5f, CheckLinkedPlayers);
                Logger.Debug("{0} Members have been loaded. Starting processing in 5 seconds.", Guild.Name);
            }
            
            Puts($"{Title} Ready");
        }
        
        [HookMethod(DiscordExtHooks.OnDiscordPlayerLinked)]
        private void OnDiscordPlayerLinked(IPlayer player, DiscordUser user)
        {
            Logger.Debug($"{nameof(OnDiscordPlayerLinked)} Added Player: {{0}}({{1}}) Discord: {{2}}({{3}}) to be processed", player.Name, player.Id, user.FullUserName, user.Id);
            ProcessChange(player, SyncEvent.PlayerLinkedChanged);
        }
        
        [HookMethod(DiscordExtHooks.OnDiscordPlayerUnlinked)]
        private void OnDiscordPlayerUnlinked(IPlayer player, DiscordUser user)
        {
            Logger.Debug($"{nameof(OnDiscordPlayerUnlinked)} Added Player: {{0}}({{1}}) Discord: {{2}}({{3}}) to be processed", player.Name, player.Id, user.FullUserName, user.Id);
            ProcessLeaving(player.Id, user.Id, SyncEvent.PlayerLinkedChanged);
        }
        
        [HookMethod(DiscordExtHooks.OnDiscordGuildMemberAdded)]
        private void OnDiscordGuildMemberAdded(GuildMemberAddedEvent member)
        {
            if (member.GuildId != Guild.Id)
            {
                return;
            }
            
            Logger.Debug($"{nameof(OnDiscordGuildMemberAdded)} Added {{0}}({{1}}) to be processed", member.User.FullUserName, member.Id);
            HandleDiscordChange(member, SyncEvent.DiscordServerJoinLeave);
        }
        
        [HookMethod(DiscordExtHooks.OnDiscordGuildMemberRemoved)]
        private void OnDiscordGuildMemberRemoved(GuildMember member, DiscordGuild guild)
        {
            if (guild.Id != Guild.Id)
            {
                return;
            }
            
            Logger.Debug($"{nameof(OnDiscordGuildMemberRemoved)} Added {{0}}({{1}}) to be processed", member.User.FullUserName, member.Id);
            HandleDiscordChange(Guild.GetMember(member.User.Id, true), SyncEvent.DiscordServerJoinLeave);
        }
        
        [HookMethod(DiscordExtHooks.OnDiscordGuildMemberNicknameUpdated)]
        private void OnDiscordGuildMemberNicknameUpdated(GuildMember member, string oldNickname, string newNickname, DateTime? lastNicknameUpdate)
        {
            if(lastNicknameUpdate.HasValue && DateTime.UtcNow - lastNicknameUpdate.Value < TimeSpan.FromMinutes(_config.Nickname.TimeBetweenNicknameSync))
            {
                Logger.Debug($"{nameof(OnDiscordGuildMemberNicknameUpdated)} Skipped processing {{0}}({{1}}) to be processed because nickname was changed {{2:0.00}} minutes ago.", member.User.FullUserName, member.Id, (DateTime.UtcNow - lastNicknameUpdate.Value).TotalMinutes);
                return;
            }
            
            Logger.Debug($"{nameof(OnDiscordGuildMemberRemoved)} Added {{0}}({{1}}) to be processed because nickname changed: {{2}} -> {{3}}", member.User.FullUserName, member.Id, oldNickname, newNickname);
            HandleDiscordChange(member, SyncEvent.DiscordNicknameChanged);
        }
        
        [HookMethod(DiscordExtHooks.OnDiscordGuildMemberRoleAdded)]
        private void OnDiscordGuildMemberRoleAdded(GuildMember member, Snowflake roleId, DiscordGuild guild)
        {
            if (ProcessRoles.Contains(roleId))
            {
                Logger.Debug($"{nameof(OnDiscordGuildMemberRemoved)} Added {{0}}({{1}}) to be processed because {{2}} role added.", member.User.FullUserName, member.Id, guild.Roles[roleId]?.Name ?? "Unknown Role");
                HandleDiscordChange(member, SyncEvent.DiscordRoleChanged);
            }
        }
        
        [HookMethod(DiscordExtHooks.OnDiscordGuildMemberRoleRemoved)]
        private void OnDiscordGuildMemberRoleRemoved(GuildMember member, Snowflake roleId, DiscordGuild guild)
        {
            if (ProcessRoles.Contains(roleId))
            {
                Logger.Debug($"{nameof(OnDiscordGuildMemberRemoved)} Added {{0}}({{1}}) to be processed because {{2}} role removed.", member.User.FullUserName, member.Id, guild.Roles[roleId]?.Name ?? "Unknown Role");
                HandleDiscordChange(member, SyncEvent.DiscordRoleChanged);
            }
        }
        
        public void HandleDiscordChange(GuildMember member, SyncEvent syncEvent)
        {
            DiscordUser user = member.User;
            Snowflake userId = user.Id;
            PlayerId playerId = _link.GetPlayerId(member.Id);
            if (!playerId.IsValid)
            {
                Logger.Debug("Skipping {0} Sync for {1}({2}). Player is not linked", syncEvent, user.FullUserName, userId);
                return;
            }
            
            IPlayer player = playerId.Player;
            if (player == null)
            {
                Logger.Debug("Skipping {0} Sync for {1}({2}). No IPlayer found", syncEvent, user.FullUserName, userId);
                return;
            }
            
            ProcessQueue.RemoveAll(p => p.MemberId == userId && !p.IsLeaving);
            QueueSync(new PlayerSyncRequest(player, member, syncEvent, false));
        }
        #endregion

        #region Plugins\DiscordRoles.Fields.cs
        // ReSharper disable once UnassignedField.Global
        public DiscordClient Client { get; set; }
        public DiscordPluginPool Pool { get; set; }
        
        [PluginReference]
        #pragma warning disable CS0649
        // ReSharper disable InconsistentNaming
        private Plugin AntiSpam, Clans;
        // ReSharper restore InconsistentNaming
        #pragma warning restore CS0649
        
        public PluginConfig _config;
        public PluginData Data;
        
        public DiscordGuild Guild;
        
        private const string AccentColor = "#de8732";
        
        private readonly DiscordLink _link = GetLibrary<DiscordLink>();
        private readonly DiscordPlaceholders _placeholders = GetLibrary<DiscordPlaceholders>();
        public readonly DiscordMessageTemplates Templates = GetLibrary<DiscordMessageTemplates>();
        private readonly DiscordCommandLocalizations _localizations = GetLibrary<DiscordCommandLocalizations>();
        
        public ILogger Logger;
        
        public readonly List<BaseHandler> SyncHandlers = new();
        public readonly List<Snowflake> ProcessRoles = new();
        public readonly List<PlayerSyncRequest> ProcessQueue = new();
        public readonly Hash<string, RecentSyncData> RecentSync = new();
        
        public static DiscordRoles Instance;
        #endregion

        #region Plugins\DiscordRoles.Helpers.cs
        public string GetPlayerName(IPlayer player)
        {
            string playerName = player.Name;
            if (_config.Nickname.UseAntiSpam && AntiSpam is { IsLoaded: true })
            {
                playerName = AntiSpam.Call<string>("GetClearName", player);
                if (string.IsNullOrEmpty(playerName))
                {
                    Logger.Warning("AntiSpam returned an empty string for '{0}'", player.Name);
                    playerName = player.Name;
                }
                else if (!playerName.Equals(player.Name))
                {
                    Logger.Debug("Nickname '{0}' was filtered by AntiSpam to '{1}'", player.Name, playerName);
                }
            }
            
            if (_config.Nickname.SyncClanTag && Clans is { IsLoaded: true })
            {
                string tag = Clans.Call<string>("GetClanOf", player.Id);
                if (!string.IsNullOrEmpty(tag))
                {
                    playerName = Lang(LangKeys.ClanTag, player, tag, playerName);
                }
            }
            
            if (playerName.Length > 32)
            {
                playerName = playerName.Substring(0, 32);
            }
            
            return playerName;
        }
        
        public RecentSyncData GetRecentSync(string playerId)
        {
            RecentSyncData data = RecentSync[playerId];
            if (data == null)
            {
                data = new RecentSyncData(_config.ConflictSettings, playerId);
                RecentSync[playerId] = data;
            }
            
            return data;
        }
        
        public void SaveData(bool force = false)
        {
            if (Data == null)
            {
                return;
            }
            
            if(!Data.HasChanged || !force)
            {
                return;
            }
            
            Interface.Oxide.DataFileSystem.WriteObject(Name, Data);
            Data.OnSaved();
        }
        #endregion

        #region Plugins\DiscordRoles.Hooks.cs
        private void OnUserConnected(IPlayer player)
        {
            Logger.Debug($"{nameof(OnUserConnected)} Added Player: {{0}}({{1}}) to be processed", player.Name, player.Id);
            ProcessChange(player, SyncEvent.PlayerConnected);
        }
        
        private void OnUserGroupAdded(string id, string group)
        {
            IPlayer player = ServerPlayerCache.Instance.GetPlayerById(id);
            Logger.Debug($"{nameof(OnUserGroupAdded)} Added Player: {{0}}({{1}}) to be processed because added to group {{2}}", player.Name, player.Id, group);
            ProcessChange(player, SyncEvent.ServerGroupChanged);
        }
        
        private void OnUserGroupRemoved(string id, string group)
        {
            IPlayer player = ServerPlayerCache.Instance.GetPlayerById(id);
            Logger.Debug($"{nameof(OnUserGroupRemoved)} Added Player: {{0}}({{1}}) to be processed because removed from group {{2}}", player.Name, player.Id, group);
            ProcessChange(player, SyncEvent.ServerGroupChanged);
        }
        
        public void ProcessChange(IPlayer player, SyncEvent syncEvent)
        {
            if (player == null)
            {
                return;
            }
            
            Snowflake userId = _link.GetDiscordId(player);
            ProcessQueue.RemoveAll(p => p.Player.Id == player.Id && !p.IsLeaving);
            QueueSync(new PlayerSyncRequest(player, userId, syncEvent, false));
        }
        
        public void ProcessLeaving(string playerId, Snowflake discordId, SyncEvent syncEvent)
        {
            ProcessQueue.RemoveAll(p => p.Player.Id == playerId);
            
            IPlayer player = players.FindPlayerById(playerId);
            if (player != null && discordId.IsValid())
            {
                QueueSync(new PlayerSyncRequest(player, discordId, syncEvent, true));
            }
        }
        #endregion

        #region Plugins\DiscordRoles.HookSubscriptions.cs
        public void SubscribeAll()
        {
            SubscribeOxideAll();
            SubscribeDiscordAll();
        }
        
        public void UnsubscribeAll()
        {
            UnsubscribeOxideAll();
            UnsubscribeDiscordAll();
        }
        
        public void SubscribeOxideAll()
        {
            try
            {
                Subscribe(nameof(OnUserConnected));
                Subscribe(nameof(OnUserGroupAdded));
                Subscribe(nameof(OnUserGroupRemoved));
                Subscribe(nameof(OnDiscordPlayerLinked));
                Subscribe(nameof(OnDiscordPlayerUnlinked));
            }
            catch
            {
                
            }
        }
        
        public void SubscribeDiscordAll()
        {
            Subscribe(nameof(OnDiscordGuildMemberNicknameUpdated));
            Subscribe(nameof(OnDiscordGuildMemberRoleAdded));
            Subscribe(nameof(OnDiscordGuildMemberRoleRemoved));
            Subscribe(nameof(OnDiscordGuildMemberAdded));
            Subscribe(nameof(OnDiscordGuildMemberRemoved));
        }
        
        public void UnsubscribeOxideAll()
        {
            try
            {
                Unsubscribe(nameof(OnUserConnected));
                Unsubscribe(nameof(OnUserGroupAdded));
                Unsubscribe(nameof(OnUserGroupRemoved));
                Unsubscribe(nameof(OnDiscordPlayerLinked));
                Unsubscribe(nameof(OnDiscordPlayerUnlinked));
            }
            catch
            {
                // ignored
            }
        }
        
        public void UnsubscribeDiscordAll()
        {
            Unsubscribe(nameof(OnDiscordGuildMemberNicknameUpdated));
            Unsubscribe(nameof(OnDiscordGuildMemberRoleAdded));
            Unsubscribe(nameof(OnDiscordGuildMemberRoleRemoved));
            Unsubscribe(nameof(OnDiscordGuildMemberAdded));
            Unsubscribe(nameof(OnDiscordGuildMemberRemoved));
        }
        #endregion

        #region Plugins\DiscordRoles.Lang.cs
        public void RegisterLang()
        {
            HashSet<string> server = new();
            HashSet<string> player = new();
            Dictionary<string, string> loc = new()
            {
                [LangKeys.Chat] = $"[#BEBEBE][[{AccentColor}]{Title}[/#]] {{0}}[/#]",
                [LangKeys.ClanTag] = "[{0}] {1}"
            };
            
            for (int index = 0; index < _config.SyncSettings.Count; index++)
            {
                SyncSettings settings = _config.SyncSettings[index];
                HandleServerRegistration(settings.ServerNotifications, loc, server);
                HandlePlayerRegistration(settings.PlayerNotifications, loc, player);
            }
            
            for (int index = 0; index < _config.PriorityGroupSettings.Count; index++)
            {
                PriorityGroupSettings group = _config.PriorityGroupSettings[index];
                HandleServerRegistration(group.ServerNotifications, loc, server);
                HandlePlayerRegistration(group.PlayerNotifications, loc, player);
            }
            
            lang.RegisterMessages(loc, this);
        }
        
        public void HandleServerRegistration(ServerNotificationSettings server, Dictionary<string, string> loc, HashSet<string> registered)
        {
            server.Initialize();
            if (registered.Add(server.LocalizationKey))
            {
                server.AddLocalizations(loc);
            }
        }
        
        public void HandlePlayerRegistration(PlayerNotificationSettings player, Dictionary<string, string> loc, HashSet<string> registered)
        {
            player.Initialize();
            if (registered.Add(player.LocalizationKey))
            {
                player.AddLocalizations(loc);
            }
        }
        
        public void Chat(string message)
        {
            server.Broadcast(Lang(LangKeys.Chat, null, message));
        }
        
        public void Chat(IPlayer player, string message)
        {
            server.Broadcast(Lang(LangKeys.Chat, player, message));
        }
        
        public string Lang(string key, IPlayer player = null) => lang.GetMessage(key, this, player?.Id);
        
        public string Lang(string key, IPlayer player = null, params object[] args)
        {
            try
            {
                return string.Format(lang.GetMessage(key, this, player?.Id), args);
            }
            catch (Exception ex)
            {
                PrintError($"Lang Key '{key}' threw exception:\n{ex}");
                throw;
            }
        }
        
        public string Lang(string key, PlaceholderData data, IPlayer player = null)
        {
            string message = Lang(key, player);
            if (data != null)
            {
                message = _placeholders.ProcessPlaceholders(message, data);
            }
            
            return message;
        }
        #endregion

        #region Plugins\DiscordRoles.Notifications.cs
        public void SendSyncNotification(PlayerSyncRequest request, BaseSyncSettings sync, INotificationSettings settings, NotificationType type)
        {
            using PlaceholderData data = GetDefault(request.Player, request.Member?.User ?? EntityCache<DiscordUser>.Instance.Get(request.MemberId), sync);
            data.ManualPool();
            if (settings.ServerNotifications.CanSendNotification(type))
            {
                SendServerMessage(settings.ServerNotifications, data, type);
                SendDiscordMessage(settings.ServerNotifications, data, type);
            }
            
            if (settings.PlayerNotifications.CanSendNotification(type))
            {
                SendPlayerMessage(request.Player, settings.PlayerNotifications, data, type);
                if (!request.IsLeaving)
                {
                    SendDiscordPmMessage(request.Player, settings.PlayerNotifications, data, type);
                }
            }
        }
        
        public void SendServerMessage(ServerNotificationSettings settings, PlaceholderData data, NotificationType type)
        {
            if (settings.SendMessageToServer)
            {
                Chat(Lang(settings.GetLocalizationKey(type), data));
            }
        }
        
        public void SendDiscordMessage(ServerNotificationSettings settings, PlaceholderData data, NotificationType type)
        {
            DiscordChannel channel = Guild.Channels[settings.DiscordMessageChannelId];
            channel?.CreateGlobalTemplateMessage(Client, settings.GetLocalizationTemplate(type), null, data);
        }
        
        public void SendPlayerMessage(IPlayer player, PlayerNotificationSettings settings, PlaceholderData data, NotificationType type)
        {
            if (settings.SendMessageToPlayer && player.IsConnected)
            {
                Chat(player, Lang(settings.GetLocalizationKey(type), data, player));
            }
        }
        
        public void SendDiscordPmMessage(IPlayer player, PlayerNotificationSettings settings, PlaceholderData data, NotificationType type)
        {
            if (settings.SendDiscordPm)
            {
                player.SendDiscordTemplateMessage(Client, settings.GetLocalizationTemplate(type), null, data);
            }
        }
        #endregion

        #region Plugins\DiscordRoles.Placeholders.cs
        public void RegisterPlaceholders()
        {
            _placeholders.RegisterPlaceholder<BaseSyncSettings, string>(this, PlaceholderKeys.Group, PlaceholderDataKeys.Sync, settings => settings.GroupName);
        }
        
        public PlaceholderData GetDefault(IPlayer player, DiscordUser user, BaseSyncSettings settings)
        {
            return _placeholders.CreateData(this).AddPlayer(player).AddUser(user).Add(PlaceholderDataKeys.Sync, settings).AddRole(settings.Role);
        }
        
        public PlaceholderData GetDefault(IPlayer player)
        {
            return _placeholders.CreateData(this).AddPlayer(player);
        }
        
        public PlaceholderData GetDefault(DiscordUser user)
        {
            return _placeholders.CreateData(this).AddUser(user);
        }
        
        public PlaceholderData GetDefault(IPlayer player, DiscordUser user)
        {
            return _placeholders.CreateData(this).AddPlayer(player).AddUser(user);
        }
        #endregion

        #region Plugins\DiscordRoles.Processing.cs
        private bool _isReadyForProcessing;
        
        public void CheckLinkedPlayers()
        {
            if (!IsDiscordLinkEnabled())
            {
                return;
            }
            
            IReadOnlyDictionary<PlayerId, Snowflake> links = _link.PlayerToDiscordIds;
            foreach (KeyValuePair<PlayerId, Snowflake> link in links)
            {
                IPlayer player = link.Key.Player;
                if (player != null)
                {
                    QueueSync(new PlayerSyncRequest(player, link.Value, SyncEvent.PluginLoaded, false));
                }
            }
            
            Logger.Info("Starting sync for {0} linked players", ProcessQueue.Count);
            ProcessNextSync();
            _isReadyForProcessing = true;
        }
        
        private bool IsDiscordLinkEnabled()
        {
            if (!_link.IsEnabled)
            {
                PrintWarning("No Discord Link plugin registered! This plugin will not work until one is added. Please add a Discord Link plugin and reload this plugin.");
                return false;
            }
            
            return true;
        }
        
        public void QueueSync(PlayerSyncRequest sync)
        {
            if (!IsDiscordLinkEnabled())
            {
                return;
            }
            
            if (!sync.MemberId.IsValid())
            {
                Logger.Debug("Skipping Sync: MemberId is invalid. Player ID: {0}({1})", sync.Player.Name, sync.Player.Id);
                return;
            }
            
            if (ProcessQueue.Count <= 3)
            {
                ProcessQueue.Add(sync);
                if (_isReadyForProcessing && ProcessQueue.Count == 1)
                {
                    ProcessNextSync();
                }
            }
            else
            {
                ProcessQueue.Insert(3, sync);
            }
        }
        
        public void ProcessNextSync()
        {
            if (ProcessQueue.Count == 0)
            {
                return;
            }
            
            PlayerSyncRequest request = ProcessQueue[0];
            ProcessQueue.RemoveAt(0);
            Promise promise = Promise.Create();
            promise.Finally(ProcessNextSync);
            ProcessUser(request, promise);
        }
        
        public void ProcessUser(PlayerSyncRequest request, Promise promise)
        {
            ProcessUserCallback callback = ProcessUserCallback.Create(request, this, promise);
            request.GetGuildMember().Finally(callback.RunAction);
        }
        
        public void HandleUserNick(PlayerSyncRequest request)
        {
            if (!_config.Nickname.SyncNicknames)
            {
                Logger.Debug( "Skipping Nickname Sync: Nickname sync is disabled.");
                return;
            }
            
            if (!_config.EventSettings.IsNicknameEnabled(request.Event))
            {
                Logger.Debug( "Skipping Nickname Sync: Nickname sync is disabled for event: {0}.", request.Event);
                return;
            }
            
            if (request.IsLeaving)
            {
                Logger.Debug("Skipping Nickname Sync: Member is leaving.");
                return;
            }
            
            if (request.Member.User.Id == Guild.OwnerId)
            {
                Logger.Debug("Skipping Nickname Sync: Member is Discord Server Owner.");
                return;
            }
            
            string playerName = GetPlayerName(request.Player);
            if (playerName.Equals(request.Member.Nickname, StringComparison.Ordinal))
            {
                Logger.Debug("Skipping Nickname Sync: Member Nickname matches '{0}' expected value", playerName);
                return;
            }
            
            Logger.Debug("Updating {0}'s Discord Nickname {1} -> {2}", request.PlayerName, request.Member.DisplayName, playerName);
            
            string oldNickname = request.Member.Nickname;
            Guild.EditMemberNick(Client, request.Member.User.Id, playerName).Then(member =>
            {
                Logger.Info("Successfully updated {0}'s Discord Nickname {1} -> {2}. Discord nickname now has the value: {3}", request.PlayerName, oldNickname, playerName, member.Nickname);
            }).Catch<ResponseError>(error =>
            {
                Logger.Error("An error has occured updating {0}'s discord server nickname to {1}.\n{2}", request.Member.DisplayName, playerName);
            });
        }
        #endregion

        #region Plugins\DiscordRoles.Setup.cs
        private void Init()
        {
            Instance = this;
            Logger = DiscordLoggerFactory.Instance.CreateLogger(this, _config.LogSettings.PluginLogLevel, _config.LogSettings);
            Data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name);
            UnsubscribeAll();
            RegisterLang();
        }
        
        private void OnServerInitialized()
        {
            if (string.IsNullOrEmpty(_config.DiscordApiKey))
            {
                PrintWarning("Please enter your bot token in the config and reload the plugin.");
                return;
            }
            
            if (_config.Nickname.UseAntiSpam)
            {
                if (AntiSpam is not { IsLoaded: true })
                {
                    PrintWarning("AntiSpam is enabled in the config but is not loaded. " +
                    "Please disable the setting in the config or load AntiSpam: https://umod.org/plugins/anti-spam");
                }
                else if (AntiSpam.Version < new VersionNumber(2, 1, 3))
                {
                    PrintWarning("AntiSpam requires version >= 2.1.3. " +
                    "Please update the plugin @ https://umod.org/plugins/anti-spam");
                }
            }
            
            RegisterPlaceholders();
            
            Client.Connect(new BotConnection
            {
                ApiToken = _config.DiscordApiKey,
                LogLevel = _config.ExtensionDebugging,
                Intents = GatewayIntents.Guilds | GatewayIntents.GuildMembers
            });
        }
        
        private void OnServerSave() => SaveData();
        
        private void Unload()
        {
            SaveData(true);
            Instance = null;
        }
        #endregion

        #region Plugins\DiscordRoles.SyncSetup.cs
        public void ValidateSyncs()
        {
            GuildMember botMember = Guild.Members[Client.Bot.BotUser.Id];
            DiscordRole botMaxRole = botMember.Roles.Select(r => Guild.Roles[r]).OrderByDescending(r => r.Position).FirstOrDefault();
            
            for (int index = _config.SyncSettings.Count - 1; index >= 0; index--)
            {
                if (!ProcessSyncSetting(_config.SyncSettings[index], botMaxRole))
                {
                    _config.SyncSettings.RemoveAt(index);
                }
            }
            
            for (int index = _config.PriorityGroupSettings.Count - 1; index >= 0; index--)
            {
                PriorityGroupSettings group = _config.PriorityGroupSettings[index];
                for (int i = group.SyncSettings.Count - 1; i >= 0; i--)
                {
                    if (!ProcessSyncSetting(group.SyncSettings[i], botMaxRole))
                    {
                        group.SyncSettings.RemoveAt(index);
                    }
                }
                
                if (group.SyncSettings.Count == 0)
                {
                    _config.PriorityGroupSettings.RemoveAt(index);
                }
            }
        }
        
        public void ValidateNickname()
        {
            if (_config.Nickname.SyncNicknames)
            {
                PermissionFlags botPerms = Guild.GetUserPermissions(Client.Bot.BotUser.Id);
                if ((botPerms & PermissionFlags.ManageNicknames) == 0)
                {
                    Logger.Warning("Sync nicknames is enabled but Discord Bot {0} does not have permission to change {1} Discord Server nicknames. Please grant the {0} bot permission to Manage Nicknames or disable sync nicknames.", Client.Bot.BotUser.FullUserName, Guild.Name);
                }
            }
        }
        
        public void RegisterSyncs()
        {
            SyncHandlers.AddRange(_config.SyncSettings
            .Where(s => s.SyncMode == SyncMode.Bidirectional)
            .Select(s => new BidirectionalSyncHandler(s)));
            
            SyncHandlers.AddRange(_config.SyncSettings
            .Where(s => s.SyncMode == SyncMode.Server)
            .GroupBy(s => s.RoleId)
            .Select(s => new ServerSyncHandler(s.ToList())));
            
            SyncHandlers.AddRange(_config.SyncSettings
            .Where(s => s.SyncMode == SyncMode.Discord)
            .GroupBy(s => s.GroupName)
            .Select(s => new DiscordSyncHandler(s.Key, s.ToList())));
            
            SyncHandlers.AddRange(_config.PriorityGroupSettings
            .Select(p => new PrioritySyncHandler(p)));
        }
        #endregion

        #region Plugins\DiscordRoles.Templates.cs
        public void RegisterTemplates()
        {
            DiscordMessageTemplate queued = CreatePrefixedTemplateEmbed($"Player: {DefaultKeys.Player.Name}({DefaultKeys.Player.Id}) User: {DefaultKeys.User.Mention} has been queued to be synced.", DiscordColor.Success);
            Templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Commands.Player.Queued, queued, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            Templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Commands.User.Queued, queued, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            
            DiscordMessageTemplate playerNotLinked = CreatePrefixedTemplateEmbed($"Player: {DefaultKeys.Player.Name}({DefaultKeys.Player.Id}) is not linked and cannot be queued to be synced", DiscordColor.Danger);
            Templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Commands.Player.Errors.NotLinked, playerNotLinked, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            
            DiscordMessageTemplate userNotLinked = CreatePrefixedTemplateEmbed($"User: {DefaultKeys.User.Mention} is not linked and cannot be queued to be synced", DiscordColor.Danger);
            Templates.RegisterLocalizedTemplateAsync(this, TemplateKeys.Commands.User.Errors.NotLinked, userNotLinked, new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
        }
        
        public DiscordMessageTemplate CreatePrefixedTemplateEmbed(string description, DiscordColor color)
        {
            return new DiscordMessageTemplate
            {
                Embeds = new List<DiscordEmbedTemplate>
                {
                    new()
                    {
                        Description = $"[{DefaultKeys.Plugin.Title}] {description}",
                        Color = color.ToHex()
                    }
                }
            };
        }
        #endregion

        #region Configuration\ConflictSettings.cs
        public class ConflictSettings
        {
            public int GroupConflictLimit { get; set; }
            public int RoleConflictLimit { get; set; }
            
            [JsonConstructor]
            public ConflictSettings() { }
            
            public ConflictSettings(ConflictSettings settings)
            {
                GroupConflictLimit = settings?.GroupConflictLimit ?? 3;
                RoleConflictLimit = settings?.RoleConflictLimit ?? 3;
            }
        }
        #endregion

        #region Configuration\EnabledSyncEvents.cs
        public class EnabledSyncEvents
        {
            [JsonProperty("Sync On Plugin Load")]
            public bool SyncOnPluginLoad { get; set; }
            
            [JsonProperty("Sync On Player Connected")]
            public bool SyncOnPlayerConnected { get; set; }
            
            [JsonProperty("Sync On Server Group Changed")]
            public bool SyncOnServerGroupChanged { get; set; }
            
            [JsonProperty("Sync On Discord Role Changed")]
            public bool SyncOnDiscordRoleChanged { get; set; }
            
            [JsonProperty("Sync On Discord Nickname Changed")]
            public bool SyncOnDiscordNicknameChanged { get; set; }
            
            [JsonProperty("Sync On Player Linked / Unlinked")]
            public bool SyncOnLinkedChanged { get; set; }
            
            [JsonProperty("Sync On User Join / Leave Discord Server")]
            public bool SyncOnDiscordServerJoinLeave { get; set; }
            
            public bool IsEnabled(SyncEvent syncEvent)
            {
                switch (syncEvent)
                {
                    case SyncEvent.None:
                    return false;
                    case SyncEvent.PluginLoaded:
                    return SyncOnPluginLoad;
                    case SyncEvent.PlayerConnected:
                    return SyncOnPlayerConnected;
                    case SyncEvent.ServerGroupChanged:
                    return SyncOnServerGroupChanged;
                    case SyncEvent.DiscordRoleChanged:
                    return SyncOnDiscordRoleChanged;
                    case SyncEvent.DiscordNicknameChanged:
                    return SyncOnDiscordNicknameChanged;
                    case SyncEvent.PlayerLinkedChanged:
                    return SyncOnLinkedChanged;
                    case SyncEvent.DiscordServerJoinLeave:
                    return SyncOnDiscordServerJoinLeave;
                    default:
                    throw new ArgumentOutOfRangeException(nameof(syncEvent), syncEvent, null);
                }
            }
        }
        #endregion

        #region Configuration\EventSettings.cs
        public class EventSettings
        {
            [JsonProperty("Events To Sync Server Groups -> Discord Roles")]
            public EnabledSyncEvents ServerSync { get; set; }
            
            [JsonProperty("Events To Sync Discord Roles -> Server Groups")]
            public EnabledSyncEvents DiscordSync { get; set; }
            
            [JsonProperty("Events To Sync Discord Nickname")]
            public EnabledSyncEvents NicknameSync { get; set; }
            
            [JsonConstructor]
            public EventSettings() { }
            
            public EventSettings(EventSettings settings)
            {
                ServerSync = new EnabledSyncEvents
                {
                    SyncOnPluginLoad = settings?.ServerSync?.SyncOnPluginLoad ?? true,
                    SyncOnPlayerConnected = settings?.ServerSync?.SyncOnPlayerConnected ?? true,
                    SyncOnServerGroupChanged = settings?.ServerSync?.SyncOnServerGroupChanged ?? true,
                    SyncOnDiscordRoleChanged = settings?.ServerSync?.SyncOnDiscordRoleChanged ?? true,
                    SyncOnDiscordNicknameChanged = settings?.ServerSync?.SyncOnDiscordNicknameChanged ?? false,
                    SyncOnLinkedChanged = settings?.ServerSync?.SyncOnLinkedChanged ?? true,
                    SyncOnDiscordServerJoinLeave = settings?.ServerSync?.SyncOnDiscordServerJoinLeave ?? true
                };
                
                DiscordSync = new EnabledSyncEvents
                {
                    SyncOnPluginLoad = settings?.DiscordSync?.SyncOnPluginLoad ?? true,
                    SyncOnPlayerConnected = settings?.DiscordSync?.SyncOnPlayerConnected ?? true,
                    SyncOnServerGroupChanged = settings?.DiscordSync?.SyncOnServerGroupChanged ?? true,
                    SyncOnDiscordRoleChanged = settings?.DiscordSync?.SyncOnDiscordRoleChanged ?? true,
                    SyncOnDiscordNicknameChanged = settings?.DiscordSync?.SyncOnDiscordNicknameChanged ?? false,
                    SyncOnLinkedChanged = settings?.DiscordSync?.SyncOnLinkedChanged ?? true,
                    SyncOnDiscordServerJoinLeave = settings?.DiscordSync?.SyncOnDiscordServerJoinLeave ?? true
                };
                
                NicknameSync = new EnabledSyncEvents
                {
                    SyncOnPluginLoad = settings?.NicknameSync?.SyncOnPluginLoad ?? true,
                    SyncOnPlayerConnected = settings?.NicknameSync?.SyncOnPlayerConnected ?? true,
                    SyncOnServerGroupChanged = settings?.NicknameSync?.SyncOnServerGroupChanged ?? false,
                    SyncOnDiscordRoleChanged = settings?.NicknameSync?.SyncOnDiscordRoleChanged ?? false,
                    SyncOnDiscordNicknameChanged = settings?.NicknameSync?.SyncOnDiscordNicknameChanged ?? true,
                    SyncOnLinkedChanged = settings?.NicknameSync?.SyncOnLinkedChanged ?? true,
                    SyncOnDiscordServerJoinLeave = settings?.NicknameSync?.SyncOnDiscordServerJoinLeave ?? false
                };
            }
            
            public bool IsServerEnabled(SyncEvent syncEvent)
            {
                return ServerSync.IsEnabled(syncEvent);
            }
            
            public bool IsDiscordEnabled(SyncEvent syncEvent)
            {
                return DiscordSync.IsEnabled(syncEvent);
            }
            
            public bool IsNicknameEnabled(SyncEvent syncEvent)
            {
                return NicknameSync.IsEnabled(syncEvent);
            }
            
            public bool IsAnyEnabled(SyncEvent syncEvent)
            {
                return IsServerEnabled(syncEvent) || IsDiscordEnabled(syncEvent) || IsNicknameEnabled(syncEvent);
            }
        }
        #endregion

        #region Configuration\LogSettings.cs
        public class LogSettings : IDiscordLoggingConfig
        {
            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Plugin Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public DiscordLogLevel PluginLogLevel { get; set; }
            
            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Console Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public DiscordLogLevel ConsoleLogLevel { get; }
            
            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "File Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public DiscordLogLevel FileLogLevel { get; }
            
            [JsonProperty(PropertyName = "File DateTime Format")]
            public string FileDateTimeFormat { get; }
            
            [JsonProperty(PropertyName = "Console Log Format")]
            public string ConsoleLogFormat { get; }
            
            [JsonConstructor]
            public LogSettings() { }
            
            public LogSettings(LogSettings settings)
            {
                PluginLogLevel = settings?.PluginLogLevel ?? DiscordLogLevel.Info;
                ConsoleLogLevel = settings?.ConsoleLogLevel ?? DiscordLogLevel.Info;
                FileLogLevel = settings?.FileLogLevel ?? DiscordLogLevel.Off;
                ConsoleLogFormat = settings?.ConsoleLogFormat ?? "[DiscordRoles] [{0}]: {1}";
                FileDateTimeFormat = settings?.FileDateTimeFormat ?? "HH:mm:ss";
            }
        }
        #endregion

        #region Configuration\NicknameSettings.cs
        public class NicknameSettings
        {
            [DefaultValue(false)]
            [JsonProperty(PropertyName = "Sync Nicknames")]
            public bool SyncNicknames { get; set; }
            
            [DefaultValue(5f)]
            [JsonProperty(PropertyName = "Minimum time between syncing player nicknames (Minutes)")]
            public float TimeBetweenNicknameSync { get; set; }
            
            [DefaultValue(false)]
            [JsonProperty(PropertyName = "Sync Clan Tag")]
            public bool SyncClanTag { get; set; }
            
            [DefaultValue(false)]
            [JsonProperty(PropertyName = "Use AntiSpam On Discord Nickname")]
            public bool UseAntiSpam { get; set; }
            
            public NicknameSettings(NicknameSettings settings)
            {
                SyncNicknames = settings?.SyncNicknames ?? false;
                TimeBetweenNicknameSync = settings?.TimeBetweenNicknameSync ?? 5f;
                SyncClanTag = settings?.SyncClanTag ?? false;
                UseAntiSpam = settings?.UseAntiSpam ?? false;
            }
        }
        #endregion

        #region Configuration\PluginConfig.cs
        public class PluginConfig
        {
            [DefaultValue("")]
            [JsonProperty(PropertyName = "Discord Bot Token")]
            public string DiscordApiKey { get; set; }
            
            [JsonProperty(PropertyName = "Discord Server ID (Optional if bot only in 1 guild)")]
            public Snowflake GuildId { get; set; }
            
            [JsonProperty(PropertyName = "Action To Perform By Event")]
            public EventSettings EventSettings { get; set; }
            
            [JsonProperty(PropertyName = "Conflict Settings")]
            public ConflictSettings ConflictSettings { get; set; }
            
            [JsonProperty(PropertyName = "Sync Settings")]
            public List<SyncSettings> SyncSettings { get; set; }
            
            [JsonProperty(PropertyName = "Priority Group Settings")]
            public List<PriorityGroupSettings> PriorityGroupSettings { get; set; }
            
            [JsonProperty(PropertyName = "Nickname Sync Settings")]
            public NicknameSettings Nickname { get; set; }
            
            [JsonProperty(PropertyName = "Plugin Log Settings")]
            public LogSettings LogSettings { get; set; }
            
            [JsonConverter(typeof(StringEnumConverter))]
            [DefaultValue(DiscordLogLevel.Info)]
            [JsonProperty(PropertyName = "Discord Extension Log Level (Verbose, Debug, Info, Warning, Error, Exception, Off)")]
            public DiscordLogLevel ExtensionDebugging { get; set; }
        }
        #endregion

        #region Data\PlayerData.cs
        public class PlayerData
        {
            public string PlayerId { get; set; }
            public readonly List<string> IgnoreGroup = new();
            public readonly List<Snowflake> IgnoreRole = new();
            
            [JsonConstructor]
            public PlayerData() { }
            
            public PlayerData(string playerId)
            {
                if (string.IsNullOrEmpty(playerId))
                {
                    throw new ArgumentNullException(nameof(playerId));
                }
                PlayerId = playerId;
            }
            
            public bool CanRemoveGroup(string group) => !IgnoreGroup.Contains(group);
            public bool CanRemoveRole(Snowflake role) => !IgnoreRole.Contains(role);
            
            public void OnGroupAdded(string group)
            {
                IgnoreGroup.Remove(group);
                CheckCleanup();
            }
            
            public void OnGroupSyncConflict(string group)
            {
                IgnoreGroup.Add(group);
                DiscordRoles.Instance.Data.OnDataChanged();
            }
            
            public void OnRoleAdded(Snowflake role)
            {
                IgnoreRole.Remove(role);
                CheckCleanup();
            }
            
            public void OnRoleSyncConflict(Snowflake role)
            {
                IgnoreRole.Add(role);
                DiscordRoles.Instance.Data.OnDataChanged();
            }
            
            private void CheckCleanup()
            {
                if (IsEmpty())
                {
                    DiscordRoles.Instance.Data.Cleanup(PlayerId);
                }
            }
            
            private bool IsEmpty() => IgnoreGroup.Count == 0 && IgnoreRole.Count == 0;
        }
        #endregion

        #region Data\PluginData.cs
        public class PluginData
        {
            public Hash<string, PlayerData> PlayerData = new();
            
            [JsonIgnore]
            public bool HasChanged { get; private set; }
            
            public PlayerData GetPlayerData(string playerId) => PlayerData[playerId];
            
            public PlayerData GetOrCreatePlayerData(string playerId)
            {
                PlayerData data = GetPlayerData(playerId);
                if (data == null)
                {
                    data = new PlayerData(playerId);
                    PlayerData[playerId] = data;
                    OnDataChanged();
                }
                
                return data;
            }
            
            public void Cleanup(string playerId)
            {
                PlayerData.Remove(playerId);
                OnDataChanged();
            }
            
            public void OnDataChanged() => HasChanged = true;
            public void OnSaved() => HasChanged = false;
        }
        #endregion

        #region Data\RecentSyncData.cs
        public class RecentSyncData
        {
            private readonly string _playerId;
            private readonly Hash<string, int> _removedGroupCount = new();
            private readonly Hash<Snowflake, int> _removedRoleCount = new();
            private readonly ConflictSettings _settings;
            
            public RecentSyncData(ConflictSettings settings, string playerId)
            {
                _settings = settings;
                _playerId = playerId;
            }
            
            public void OnGroupRemoved(string group)
            {
                int count = _removedGroupCount[group] + 1;
                _removedGroupCount[group] = count;
                if (count > _settings.GroupConflictLimit)
                {
                    DiscordRoles.Instance.Data.GetOrCreatePlayerData(_playerId).OnGroupSyncConflict(group);
                }
            }
            
            public void OnRoleRemoved(Snowflake role)
            {
                int count = _removedRoleCount[role] + 1;
                _removedRoleCount[role] = count;
                if (count > _settings.RoleConflictLimit)
                {
                    DiscordRoles.Instance.Data.GetOrCreatePlayerData(_playerId).OnRoleSyncConflict(role);
                }
            }
        }
        #endregion

        #region Enums\NotificationType.cs
        public enum NotificationType : byte
        {
            GroupAdded,
            GroupRemoved,
            RoleAdded,
            RoleRemoved
        }
        #endregion

        #region Enums\PriorityMode.cs
        public enum PriorityMode : byte
        {
            Highest,
            HighestAndBelow
        }
        #endregion

        #region Enums\RemoveMode.cs
        public enum RemoveMode : byte
        {
            Remove,
            Keep
        }
        #endregion

        #region Enums\SyncEvent.cs
        public enum SyncEvent : byte
        {
            None,
            PluginLoaded,
            PlayerConnected,
            ServerGroupChanged,
            DiscordRoleChanged,
            DiscordNicknameChanged,
            PlayerLinkedChanged,
            DiscordServerJoinLeave
        }
        #endregion

        #region Enums\SyncMode.cs
        public enum SyncMode : byte
        {
            Server,
            Discord,
            Bidirectional
        }
        #endregion

        #region Handlers\BaseHandler.cs
        public abstract class BaseHandler
        {
            protected readonly DiscordRoles Plugin = DiscordRoles.Instance;
            
            protected abstract bool CanProcess(PlayerSyncRequest request);
            protected abstract void LogProcessStart(PlayerSyncRequest request);
            protected abstract BaseSyncSettings GetMatchingSync(PlayerSyncRequest request);
            protected abstract bool IsInGroup(PlayerSyncRequest request, BaseSyncSettings sync);
            protected abstract bool IsInRole(PlayerSyncRequest request, BaseSyncSettings sync);
            protected abstract bool RequiresSync(PlayerSyncRequest request, bool isInGroup, bool isInRole);
            protected abstract bool ShouldAdd(bool isInGroup, bool isInRole);
            protected abstract bool ShouldRemove(PlayerSyncRequest request);
            protected abstract void HandleAdd(PlayerSyncRequest request, BaseSyncSettings sync, bool isInGroup, bool isInRole);
            protected abstract void HandleRemove(PlayerSyncRequest request, BaseSyncSettings sync);
            protected abstract void SendNotification(PlayerSyncRequest request, BaseSyncSettings sync, NotificationType type);
            
            protected virtual void ProcessAddRemove(PlayerSyncRequest request, BaseSyncSettings sync, bool isInGroup, bool isInRole)
            {
                if (ShouldAdd(isInGroup, isInRole))
                {
                    HandleAdd(request, sync, isInGroup, isInRole);
                }
                else if (ShouldRemove(request))
                {
                    HandleRemove(request, sync);
                }
            }
            
            public void Process(PlayerSyncRequest request)
            {
                if (!CanProcess(request))
                {
                    return;
                }
                
                LogProcessStart(request);
                
                BaseSyncSettings sync = GetMatchingSync(request);
                bool isInGroup = IsInGroup(request, sync);
                bool isInRole = IsInRole(request, sync);
                if (RequiresSync(request, isInGroup, isInRole))
                {
                    ProcessAddRemove(request, sync, isInGroup, isInRole);
                }
            }
        }
        #endregion

        #region Handlers\BidirectionalSyncHandler.cs
        public class BidirectionalSyncHandler : BaseHandler
        {
            private readonly SyncSettings _settings;
            private readonly string _syncName;
            
            public BidirectionalSyncHandler(SyncSettings settings)
            {
                _settings = settings;
                _syncName = settings.GetInfoString(SyncMode.Bidirectional);
            }
            
            protected override bool CanProcess(PlayerSyncRequest request)
            {
                if (request.IsLeaving)
                {
                    Plugin.Logger.Debug($"${nameof(BidirectionalSyncHandler)} Skipping Sync: Member Is Leaving Discord Server", request.PlayerName, request.IsLeaving);
                    return false;
                }
                
                return true;
            }
            
            protected override void LogProcessStart(PlayerSyncRequest request) => Plugin.Logger.Debug($"{nameof(BidirectionalSyncHandler)} Processing Sync: [{{0}}] Sync for {{1}} Is Leaving: {{2}}", _syncName, request.PlayerName, request.IsLeaving);
            protected override BaseSyncSettings GetMatchingSync(PlayerSyncRequest request) => _settings;
            protected override bool IsInGroup(PlayerSyncRequest request, BaseSyncSettings sync) => request.HasGroup(sync.GroupName);
            protected override bool IsInRole(PlayerSyncRequest request, BaseSyncSettings sync) => request.HasRole(sync.RoleId);
            protected override bool RequiresSync(PlayerSyncRequest request, bool isInGroup, bool isInRole)
            {
                if (isInGroup != isInRole)
                {
                    return true;
                }
                
                if (Plugin.Logger.IsLogging(DiscordLogLevel.Debug))
                {
                    Plugin.Logger.Debug($"{nameof(BidirectionalSyncHandler)} {{0}} Skipping Sync: {{1}} -> {{2}} Reason: {{3}}", request.PlayerName, _settings.GroupName, _settings.Role.Name, isInGroup ? "Already Synced" : "Not in group");
                    if (!isInGroup)
                    {
                        Plugin.Logger.Debug($"{nameof(BidirectionalSyncHandler)} {{0}} is in the following Groups: {{1}} Roles: {{2}}", request.PlayerName, request.PlayerGroups, request.PlayerRoles);
                    }
                }
                
                return false;
            }
            
            protected override bool ShouldAdd(bool isInGroup, bool isInRole) => isInGroup || isInRole;
            protected override bool ShouldRemove(PlayerSyncRequest request) => false;
            protected override void HandleAdd(PlayerSyncRequest request, BaseSyncSettings sync, bool isInGroup, bool isInRole)
            {
                if (!isInGroup)
                {
                    request.AddServerGroup(_settings.GroupName);
                    SendNotification(request, _settings, NotificationType.GroupAdded);
                }
                
                if (!isInRole)
                {
                    request.AddGuildRole(_settings.Role);
                    SendNotification(request, _settings, NotificationType.RoleAdded);
                }
            }
            
            protected override void HandleRemove(PlayerSyncRequest request, BaseSyncSettings sync) {}
            protected override void SendNotification(PlayerSyncRequest request, BaseSyncSettings sync, NotificationType type) =>  Plugin.SendSyncNotification(request, sync, (INotificationSettings)sync, type);
        }
        #endregion

        #region Handlers\DiscordSyncHandler.cs
        public class DiscordSyncHandler : BaseHandler
        {
            private readonly string _group;
            private readonly List<SyncSettings> _syncs;
            private readonly string _roleNameList;
            
            public DiscordSyncHandler(string group, List<SyncSettings> syncs)
            {
                _group = group;
                _syncs = syncs;
                _roleNameList = string.Join(", ", syncs.Select(d => DiscordRoles.Instance.Guild.Roles[d.RoleId].Name));
            }
            
            protected override bool CanProcess(PlayerSyncRequest request)
            {
                if (request.IsLeaving)
                {
                    Plugin.Logger.Debug($"{nameof(DiscordSyncHandler)} Skipping Sync: Member Is Leaving Discord Server", request.PlayerName, request.IsLeaving);
                    return false;
                }
                
                if (!Plugin._config.EventSettings.IsDiscordEnabled(request.Event))
                {
                    Plugin.Logger.Debug($"{nameof(DiscordSyncHandler)} Skipping sync for event {{0}}", request.Event);
                    return false;
                }
                
                return true;
            }
            
            protected override void LogProcessStart(PlayerSyncRequest request) => Plugin.Logger.Debug($"{nameof(DiscordSyncHandler)} Processing Sync: [{{0}}] -> {{1}} Sync for {{2}} Is Leaving: {{3}}", _roleNameList, _group, request.PlayerName, request.IsLeaving);
            
            protected override BaseSyncSettings GetMatchingSync(PlayerSyncRequest request)
            {
                if (request.IsLeaving || request.Member == null)
                {
                    return null;
                }
                
                for (int index = 0; index < _syncs.Count; index++)
                {
                    SyncSettings sync = _syncs[index];
                    if (request.HasRole(sync.RoleId))
                    {
                        return sync;
                    }
                }
                
                return null;
            }
            
            protected override bool IsInGroup(PlayerSyncRequest request, BaseSyncSettings sync) => request.HasGroup(_group);
            protected override bool IsInRole(PlayerSyncRequest request, BaseSyncSettings sync) => sync != null;
            protected override bool RequiresSync(PlayerSyncRequest request, bool isInGroup, bool isInRole)
            {
                if (isInGroup != isInRole)
                {
                    return true;
                }
                
                if (Plugin.Logger.IsLogging(DiscordLogLevel.Debug))
                {
                    Plugin.Logger.Debug($"{nameof(DiscordSyncHandler)} {{0}} Skipping Sync: [{{1}}] -> {{2}} {{3}}", request.PlayerName, _roleNameList, _group, isInRole ? "Already Synced" : "Not in role");
                    if (!isInGroup)
                    {
                        Plugin.Logger.Debug($"{nameof(DiscordSyncHandler)} {{0}} has the following roles ({{1}})", request.PlayerName, request.PlayerRoles);
                    }
                }
                
                return false;
            }
            protected override bool ShouldAdd(bool isInGroup, bool isInRole) => isInRole;
            protected override bool ShouldRemove(PlayerSyncRequest request)
            {
                for (int index = 0; index < _syncs.Count; index++)
                {
                    SyncSettings sync = _syncs[index];
                    if (sync.RemoveMode == RemoveMode.Keep && request.HasRole(sync.RoleId))
                    {
                        Plugin.Logger.Debug($"{nameof(DiscordSyncHandler)} Skipped Removing {{0}} from Server Group '{{1}}' because {{2}} -> {{1}} Remove Mode is {nameof(RemoveMode.Keep)}", request.PlayerName, _group, sync.Role.Name, sync.RemoveMode);
                        return false;
                    }
                }
                
                return true;
            }
            protected override void HandleAdd(PlayerSyncRequest request, BaseSyncSettings sync, bool isInGroup, bool isInRole)
            {
                request.AddServerGroup(_group);
                SendNotification(request, sync ?? _syncs[0], NotificationType.GroupAdded);
            }
            
            protected override void HandleRemove(PlayerSyncRequest request, BaseSyncSettings sync)
            {
                request.RemoveServerGroup(_group);
                SendNotification(request, sync ?? _syncs[0], NotificationType.GroupRemoved);
            }
            
            protected override void SendNotification(PlayerSyncRequest request, BaseSyncSettings sync, NotificationType type) =>  Plugin.SendSyncNotification(request, sync, (INotificationSettings)sync, type);
        }
        #endregion

        #region Handlers\PrioritySyncHandler.cs
        public class PrioritySyncHandler : BaseHandler
        {
            private readonly PriorityGroupSettings _group;
            private readonly List<PrioritySyncSettings> _syncs;
            private readonly string _syncName;
            
            public PrioritySyncHandler(PriorityGroupSettings settings)
            {
                _group = settings;
                _syncs = settings.SyncSettings;
                _syncs.Sort((l, r) => l.Priority.CompareTo(r.Priority));
                _syncName = string.Join(", ", _syncs.Select(s => s.GetInfoString(_group.SyncMode)));
            }
            
            protected override bool CanProcess(PlayerSyncRequest request)
            {
                if (_group.SyncMode == SyncMode.Server && request.IsLeaving)
                {
                    Plugin.Logger.Debug($"{nameof(PrioritySyncHandler)} Skipping Sync for Sync Mode: {{0}}. Member Is Leaving Discord Server", _group.SyncMode);
                    return false;
                }
                
                return true;
            }
            protected override void LogProcessStart(PlayerSyncRequest request) => Plugin.Logger.Debug($"{nameof(PrioritySyncHandler)} Processing Sync: [{{0}}] Sync for {{1}} Is Leaving: {{2}}", _syncName, request.PlayerName, request.IsLeaving);
            
            protected override BaseSyncSettings GetMatchingSync(PlayerSyncRequest request)
            {
                SyncMode mode = _group.SyncMode;
                for (int index = 0; index < _syncs.Count; index++)
                {
                    PrioritySyncSettings settings = _syncs[index];
                    switch (mode)
                    {
                        case SyncMode.Bidirectional or SyncMode.Server when request.HasGroup(settings.GroupName):
                        return settings;
                        case SyncMode.Bidirectional or SyncMode.Discord when request.HasRole(settings.RoleId):
                        return settings;
                    }
                }
                
                return null;
            }
            
            protected override bool IsInGroup(PlayerSyncRequest request, BaseSyncSettings sync) => sync != null && request.HasGroup(sync.GroupName);
            protected override bool IsInRole(PlayerSyncRequest request, BaseSyncSettings sync) => sync != null && request.HasRole(sync.RoleId);
            protected override bool RequiresSync(PlayerSyncRequest request, bool isInGroup, bool isInRole) => true;
            protected override void ProcessAddRemove(PlayerSyncRequest request, BaseSyncSettings baseSync, bool isInGroup, bool isInRole)
            {
                SyncMode mode = _group.SyncMode;
                PriorityMode priorityMode = _group.PriorityMode;
                PrioritySyncSettings sync = baseSync as PrioritySyncSettings;
                for (int index = 0; index < _syncs.Count; index++)
                {
                    PrioritySyncSettings settings = _syncs[index];
                    
                    bool shouldAdd = false;
                    if (priorityMode == PriorityMode.Highest)
                    {
                        shouldAdd = sync != null && settings == sync;
                    }
                    else if(priorityMode == PriorityMode.HighestAndBelow)
                    {
                        shouldAdd = sync != null && settings.Priority <= sync.Priority;
                    }
                    
                    if (mode == SyncMode.Bidirectional || mode == SyncMode.Server)
                    {
                        if (shouldAdd)
                        {
                            if (!request.HasRole(settings.RoleId))
                            {
                                SendNotification(request, settings, NotificationType.RoleAdded);
                            }
                            request.AddGuildRole(settings.Role);
                        }
                        else if(request.HasRole(settings.RoleId))
                        {
                            request.RemoveGuildRole(settings.Role);
                            SendNotification(request, settings, NotificationType.RoleRemoved);
                        }
                    }
                    
                    if (mode == SyncMode.Bidirectional || mode == SyncMode.Discord)
                    {
                        if (shouldAdd)
                        {
                            if (!request.HasGroup(settings.GroupName))
                            {
                                SendNotification(request, settings, NotificationType.GroupAdded);
                            }
                            request.AddServerGroup(settings.GroupName);
                        }
                        else if(request.HasGroup(settings.GroupName))
                        {
                            request.RemoveServerGroup(settings.GroupName);
                            SendNotification(request, settings, NotificationType.GroupRemoved);
                        }
                    }
                }
            }
            
            protected override bool ShouldAdd(bool isInGroup, bool isInRole) => false;
            protected override bool ShouldRemove(PlayerSyncRequest request) => false;
            protected override void HandleAdd(PlayerSyncRequest request, BaseSyncSettings sync, bool isInGroup, bool isInRole){}
            protected override void HandleRemove(PlayerSyncRequest request, BaseSyncSettings sync) {}
            protected override void SendNotification(PlayerSyncRequest request, BaseSyncSettings sync, NotificationType type) =>  Plugin.SendSyncNotification(request, sync, _group, type);
        }
        #endregion

        #region Handlers\ServerSyncHandler.cs
        public class ServerSyncHandler : BaseHandler
        {
            private readonly DiscordRole _role;
            private readonly List<SyncSettings> _syncs;
            private readonly string _groupNames;
            
            public ServerSyncHandler(List<SyncSettings> syncs)
            {
                _role = syncs[0].Role;
                _syncs = syncs;
                _groupNames = string.Join(", ", syncs.Select(d => d.GroupName));
            }
            
            protected override bool CanProcess(PlayerSyncRequest request) => !request.IsLeaving && Plugin._config.EventSettings.IsServerEnabled(request.Event);
            protected override void LogProcessStart(PlayerSyncRequest request) => Plugin.Logger.Debug($"{nameof(ServerSyncHandler)} Processing Sync: [{{0}}] -> {{1}} Sync for {{2}} Is Leaving: {{3}}", _groupNames, _role.Name, request.PlayerName, request.IsLeaving);
            protected override BaseSyncSettings GetMatchingSync(PlayerSyncRequest request)
            {
                for (int index = 0; index < _syncs.Count; index++)
                {
                    SyncSettings sync = _syncs[index];
                    if (request.HasGroup(sync.GroupName))
                    {
                        return sync;
                    }
                }
                
                return null;
            }
            protected override bool IsInGroup(PlayerSyncRequest request, BaseSyncSettings sync) => sync != null;
            protected override bool IsInRole(PlayerSyncRequest request, BaseSyncSettings sync) => request.HasRole(_role.Id);
            protected override bool RequiresSync(PlayerSyncRequest request, bool isInGroup, bool isInRole)
            {
                if (isInGroup != isInRole)
                {
                    return true;
                }
                
                if (Plugin.Logger.IsLogging(DiscordLogLevel.Debug))
                {
                    Plugin.Logger.Debug($"{nameof(ServerSyncHandler)} {{0}} Skipping Server Sync: [{{1}}] -> {{2}} Reason: {{3}}", request.PlayerName, _groupNames, _role.Name, isInGroup ? "Already Synced" : "Not in group");
                    if (!isInGroup)
                    {
                        Plugin.Logger.Debug($"{nameof(ServerSyncHandler)} {{0}} is in the following Groups: {{1}}", request.PlayerName, request.PlayerGroups);
                    }
                }
                
                return false;
            }
            protected override bool ShouldAdd(bool isInGroup, bool isInRole) => isInGroup;
            protected override bool ShouldRemove(PlayerSyncRequest request)
            {
                for (int index = 0; index < _syncs.Count; index++)
                {
                    SyncSettings sync = _syncs[index];
                    if (sync.RemoveMode == RemoveMode.Keep && request.HasGroup(sync.GroupName))
                    {
                        Plugin.Logger.Debug($"{nameof(ServerSyncHandler)} Skipped Removing {{0}} from Discord Role '{{1}}' because Remove Move is {nameof(RemoveMode.Keep)}", request.PlayerName, sync.Role.Name);
                        return false;
                    }
                }
                
                return true;
            }
            protected override void HandleAdd(PlayerSyncRequest request, BaseSyncSettings sync, bool isInGroup, bool isInRole)
            {
                request.AddGuildRole(_role);
                SendNotification(request, sync ?? _syncs[0], NotificationType.RoleAdded);
            }
            
            protected override void HandleRemove(PlayerSyncRequest request, BaseSyncSettings sync)
            {
                request.RemoveGuildRole(_role);
                SendNotification(request, sync ?? _syncs[0], NotificationType.RoleRemoved);
            }
            
            protected override void SendNotification(PlayerSyncRequest request, BaseSyncSettings sync, NotificationType type) =>  Plugin.SendSyncNotification(request, sync, (INotificationSettings)sync, type);
        }
        #endregion

        #region Interfaces\INotificationSettings.cs
        public interface INotificationSettings
        {
            ServerNotificationSettings ServerNotifications { get; }
            
            PlayerNotificationSettings PlayerNotifications { get; }
        }
        #endregion

        #region Lang\LangKeys.cs
        public static class LangKeys
        {
            public const string Chat = nameof(Chat);
            public const string ClanTag = nameof(ClanTag);
            
            public static class Announcements
            {
                public const string GroupAdded = ".Announcement.Group.Added";
                public const string GroupRemoved = ".Announcement.Group.Removed";
                public const string RoleAdded = ".Announcement.Role.Added";
                public const string RoleRemoved = ".Announcement.Role.Removed";
            }
            
            public static class Player
            {
                public const string GroupAdded = ".Player.Group.Added";
                public const string GroupRemoved = ".Player.Group.Removed";
                public const string RoleAdded = ".Player.Role.Added";
                public const string RoleRemoved = ".Player.Role.Removed";
            }
        }
        #endregion

        #region Lang\TemplateKeys.cs
        public class TemplateKeys
        {
            public static class Commands
            {
                private const string Base = nameof(Commands) + ".";
                
                public static class User
                {
                    private const string Base = Commands.Base + nameof(User) + ".";
                    
                    public static readonly TemplateKey Queued = new(Base + nameof(Queued));
                    
                    public static class Errors
                    {
                        private const string Base = User.Base + nameof(Errors) + ".";
                        
                        public static readonly TemplateKey NotLinked = new(Base + nameof(NotLinked));
                    }
                }
                
                public static class Player
                {
                    private const string Base = Commands.Base + nameof(Player) + ".";
                    
                    public static readonly TemplateKey Queued = new(Base + nameof(Queued));
                    
                    public static class Errors
                    {
                        private const string Base = Player.Base + nameof(Errors) + ".";
                        
                        public static readonly TemplateKey NotLinked = new(Base + nameof(NotLinked));
                    }
                }
            }
        }
        #endregion

        #region Placeholders\PlaceholderDataKeys.cs
        public class PlaceholderDataKeys
        {
            public static readonly PlaceholderDataKey Sync = new("sync");
        }
        #endregion

        #region Placeholders\PlaceholderKeys.cs
        public class PlaceholderKeys
        {
            public static readonly PlaceholderKey Group = new(nameof(DiscordRoles), "group");
        }
        #endregion

        #region Sync\PlayerSyncRequest.cs
        public class PlayerSyncRequest
        {
            public readonly IPlayer Player;
            public readonly Snowflake MemberId;
            public readonly SyncEvent Event;
            
            public GuildMember Member { get; private set; }
            public bool IsLeaving { get; private set; }
            
            private string _playerName;
            private string _playerGroups;
            private string _playerRoles;
            
            private readonly List<Snowflake> _roles = new();
            private readonly List<string> _groups = new();
            private static readonly Permission Permission = Interface.Oxide.GetLibrary<Permission>();
            private readonly DiscordRoles _plugin = DiscordRoles.Instance;
            private readonly RecentSyncData _recentSync;
            
            public PlayerSyncRequest(IPlayer player, Snowflake memberId, SyncEvent sync, bool isLeaving)
            {
                Player = player ?? throw new ArgumentNullException(nameof(player));
                _groups.AddRange(Permission.GetUserGroups(player.Id));
                MemberId = memberId;
                Event = sync;
                IsLeaving = isLeaving;
                _recentSync = _plugin.GetRecentSync(player.Id);
            }
            
            public PlayerSyncRequest(IPlayer player, GuildMember member, SyncEvent sync, bool isLeaving)
            {
                Player = player ?? throw new ArgumentNullException(nameof(player));
                Member = member ?? throw new ArgumentException(nameof(member));
                _groups.AddRange(Permission.GetUserGroups(player.Id));
                MemberId = member.Id;
                Event = sync;
                IsLeaving = isLeaving;
                _recentSync = _plugin.GetRecentSync(player.Id);
            }
            
            public string PlayerName => _playerName ??= $"Player: {Player.Name}({Player.Id}) User: {Member?.User.FullUserName}({MemberId})";
            public string PlayerGroups => _playerGroups ??= string.Join(", ", _groups);
            public string PlayerRoles => _playerRoles ??= string.Join(", ", _roles.Select(r => DiscordRoles.Instance.Guild.Roles[r]?.Name ?? $"Unknown Role ({r})"));
            public bool HasGroup(string group) => _groups.Contains(group, StringComparer.InvariantCultureIgnoreCase);
            public bool HasRole(Snowflake roleId) => !IsLeaving && (_roles.Contains(roleId) || _plugin.Guild.Id == roleId);
            
            private void SetMember(GuildMember member)
            {
                if (member != null)
                {
                    Member = member;
                    _roles.Clear();
                    _roles.AddRange(member.Roles);
                    _playerRoles = null;
                }
            }
            
            public void AddServerGroup(string group)
            {
                PlayerData playerData = _plugin.Data.GetPlayerData(Player.Id);
                playerData?.OnGroupAdded(group);
                
                _plugin.Logger.Info("Adding Player {0} to Server Group '{1}'", PlayerName, group);
                Permission.AddUserGroup(Player.Id, group);
                _groups.Add(group);
                _playerGroups = null;
            }
            
            public void RemoveServerGroup(string group)
            {
                PlayerData playerData = _plugin.Data.GetPlayerData(Player.Id);
                if (playerData != null && !playerData.CanRemoveGroup(group))
                {
                    _plugin.Logger.Info("Skipped removing player {0} from Server Group {1}. Conflicting Sync Detected.", PlayerName, group);
                    return;
                }
                
                _plugin.Logger.Info("Removing player {0} from Server Group '{1}'", PlayerName, group);
                Permission.RemoveUserGroup(Player.Id, group);
                _groups.Remove(group);
                _playerGroups = null;
                _recentSync.OnGroupRemoved(group);
            }
            
            public void AddGuildRole(DiscordRole role)
            {
                PlayerData playerData = _plugin.Data.GetPlayerData(Player.Id);
                playerData?.OnRoleAdded(role.Id);
                
                string playerName = PlayerName;
                _plugin.Logger.Info("Adding {0} to Discord Role {1}", playerName, role.Name);
                _roles.Add(role.Id);
                _playerRoles = null;
                DiscordRoles.Instance.Guild.AddMemberRole(DiscordRoles.Instance.Client, Member.User.Id, role.Id).Then(() =>
                {
                    _plugin.Logger.Info("Successfully Added {0} to Discord Role {1}", playerName, role.Name);
                }).Catch<ResponseError>(error =>
                {
                    if (error.DiscordError is { Code: 50013 })
                    {
                        _plugin.Logger.Error("An error has occured adding {0} to Discord Role {1}. The Discord Bot {2} does not have permission to add Discord Role {3}.", playerName, role.Name, _plugin.Client.Bot.BotUser.FullUserName, role.Name);
                    }
                    else
                    {
                        _plugin.Logger.Error("An error has occured adding {0} to Discord Role {1}.\nCode:{2}\nMessage:{3}", playerName, role.Name, error.HttpStatusCode, error.Message);
                    }
                });
            }
            
            public void RemoveGuildRole(DiscordRole role)
            {
                PlayerData playerData = _plugin.Data.GetPlayerData(Player.Id);
                if (playerData != null && !playerData.CanRemoveRole(role.Id))
                {
                    _plugin.Logger.Info("Skipped removing player {0} from Discord Role {1}. Conflicting Sync Detected.", PlayerName, role.Name);
                    return;
                }
                
                string playerName = PlayerName;
                _roles.Remove(role.Id);
                _playerRoles = null;
                _plugin.Guild.RemoveMemberRole(_plugin.Client, Member.User.Id, role.Id).Then(() =>
                {
                    _plugin.Logger.Info("Successfully Removed {0} from Discord Role: {1}", playerName, role.Name);
                    _recentSync.OnRoleRemoved(role.Id);
                }).Catch<ResponseError>(error =>
                {
                    if (error.DiscordError is { Code: 50013 })
                    {
                        _plugin.Logger.Error("An error has occured removing {0} from Discord Role {1}. The Discord Bot {2} does not have permission to remove Discord role {3}.", playerName, role.Name, _plugin.Client.Bot.BotUser.FullUserName, role.Name);
                        error.SuppressErrorMessage();
                    }
                    else
                    {
                        _plugin.Logger.Error("An error has occured removing {0} from Discord Role {1}.\nCode:{2}\nMessage:{3}", playerName, role.Name, error.HttpStatusCode, error.Message);
                    }
                });
            }
            
            public IPromise GetGuildMember()
            {
                return _plugin.Guild.GetMember(_plugin.Client, MemberId)
                .Then(SetMember)
                .Catch<ResponseError>(error =>
                {
                    if (error.HttpStatusCode == DiscordHttpStatusCode.NotFound)
                    {
                        error.SuppressErrorMessage();
                        IsLeaving = true;
                        return;
                    }
                    
                    _plugin.Logger.Error("An error occured loading Guild Member For: {0}.\nCode:{1}\nMessage:{2}", PlayerName, error.HttpStatusCode, error.Message);
                });
            }
        }
        #endregion

        #region Sync\ProcessUserCallback.cs
        public class ProcessUserCallback : BaseCallback
        {
            private PlayerSyncRequest _request;
            private DiscordRoles _plugin;
            private Promise _promise;
            
            public readonly Action RunAction;
            
            public ProcessUserCallback()
            {
                RunAction = Run;
            }
            
            public static ProcessUserCallback Create(PlayerSyncRequest request, DiscordRoles plugin, Promise promise)
            {
                ProcessUserCallback callback = plugin.Pool.Get<ProcessUserCallback>();
                callback.Init(request, plugin, promise);
                return callback;
            }
            
            private void Init(PlayerSyncRequest request, DiscordRoles plugin, Promise promise)
            {
                _request = request;
                _plugin = plugin;
                _promise = promise;
            }
            
            protected override void HandleCallback()
            {
                try
                {
                    if (_request.Member == null && !_request.IsLeaving)
                    {
                        _plugin.Logger.Debug("Skipping processing: {0}({1}) Failed to load Member with ID: {2}", _request.PlayerName, _request.Player.Id, _request.MemberId);
                        return;
                    }
                    
                    _plugin.Logger.Debug("Start processing: {0}({1}) Is Leaving: {2} Server Groups: {3} Discord Roles: {4}", _request.PlayerName, _request.Player.Id, _request.IsLeaving, _request.PlayerGroups, _request.PlayerRoles);
                    
                    List<BaseHandler> syncHandlers = _plugin.SyncHandlers;
                    for (int index = 0; index < syncHandlers.Count; index++)
                    {
                        BaseHandler handler = syncHandlers[index];
                        handler.Process(_request);
                    }
                    
                    _plugin.HandleUserNick(_request);
                    _plugin.Logger.Debug("Finish processing: {0}({1}) Is Leaving: {2} Server Groups: {3} Discord Roles: {4}", _request.PlayerName, _request.Player.Id, _request.IsLeaving, _request.PlayerGroups, _request.PlayerRoles);
                }
                catch (Exception ex)
                {
                    _plugin.Logger.Exception("An error occured processing sync for {0}.\n{1}", _request.PlayerName, ex);
                }
                finally
                {
                    _promise.Resolve();
                }
            }
        }
        #endregion

        #region Configuration\Notifications\BaseNotifications.cs
        public abstract class BaseNotifications
        {
            [JsonProperty(PropertyName = "Send Message When Added")]
            public bool SendMessageOnAdd { get; set; }
            
            [JsonProperty(PropertyName = "Send Message When Removed")]
            public bool SendMessageOnRemove { get; set; }
            
            [JsonProperty(PropertyName = "Message Localization Key")]
            public string LocalizationKey { get; set; }
            
            [JsonIgnore]
            protected string GroupAddedKey { get; set; }
            
            [JsonIgnore]
            protected string GroupRemoveKey { get; set; }
            
            [JsonIgnore]
            protected string RoleAddedKey { get; set; }
            
            [JsonIgnore]
            protected string RoleRemoveKey { get; set; }
            
            [JsonIgnore]
            protected TemplateKey GroupAddedTemplate { get; set; }
            
            [JsonIgnore]
            protected TemplateKey GroupRemoveTemplate { get; set; }
            
            [JsonIgnore]
            protected TemplateKey RoleAddedTemplate { get; set; }
            
            [JsonIgnore]
            protected TemplateKey RoleRemoveTemplate { get; set; }
            
            protected BaseNotifications() { }
            
            protected BaseNotifications(BaseNotifications settings)
            {
                SendMessageOnAdd = settings?.SendMessageOnAdd ?? false;
                SendMessageOnRemove = settings?.SendMessageOnRemove ?? false;
                LocalizationKey = settings?.LocalizationKey ?? "Default";
            }
            
            public bool CanSendNotification(NotificationType type)
            {
                switch (type)
                {
                    case NotificationType.GroupAdded:
                    case NotificationType.RoleAdded:
                    return SendMessageOnAdd;
                    
                    case NotificationType.GroupRemoved:
                    case NotificationType.RoleRemoved:
                    return SendMessageOnRemove;
                    
                    default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            }
            
            public string GetLocalizationKey(NotificationType type)
            {
                return type switch
                {
                    NotificationType.GroupAdded => GroupAddedKey,
                    NotificationType.GroupRemoved => GroupRemoveKey,
                    NotificationType.RoleAdded => RoleAddedKey,
                    NotificationType.RoleRemoved => RoleRemoveKey,
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };
            }
            
            public TemplateKey GetLocalizationTemplate(NotificationType type)
            {
                return type switch
                {
                    NotificationType.GroupAdded => GroupAddedTemplate,
                    NotificationType.GroupRemoved => GroupRemoveTemplate,
                    NotificationType.RoleAdded => RoleAddedTemplate,
                    NotificationType.RoleRemoved => RoleRemoveTemplate,
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };
            }
            
            public abstract void Initialize();
            public abstract void AddLocalizations(Dictionary<string, string> loc);
        }
        #endregion

        #region Configuration\Notifications\PlayerNotificationSettings.cs
        public class PlayerNotificationSettings : BaseNotifications
        {
            [JsonProperty(PropertyName = "Send Server Message To Player")]
            public bool SendMessageToPlayer { get; set; }
            
            [JsonProperty(PropertyName = "Discord PM To Player")]
            public bool SendDiscordPm { get; set; }
            
            [JsonConstructor]
            public PlayerNotificationSettings() { }
            
            public PlayerNotificationSettings(PlayerNotificationSettings settings) : base(settings)
            {
                SendMessageToPlayer = settings?.SendMessageToPlayer ?? false;
                SendDiscordPm = settings?.SendDiscordPm ?? false;
            }
            
            public override void Initialize()
            {
                GroupAddedKey = LocalizationKey + LangKeys.Player.GroupAdded;
                GroupRemoveKey = LocalizationKey + LangKeys.Player.GroupRemoved;
                RoleAddedKey = LocalizationKey + LangKeys.Player.RoleAdded;
                RoleRemoveKey = LocalizationKey + LangKeys.Player.RoleRemoved;
                
                GroupAddedTemplate = new TemplateKey(GroupAddedKey);
                GroupRemoveTemplate = new TemplateKey(GroupRemoveKey);
                RoleAddedTemplate = new TemplateKey(RoleAddedKey);
                RoleRemoveTemplate = new TemplateKey(RoleRemoveKey);
            }
            
            public override void AddLocalizations(Dictionary<string, string> loc)
            {
                loc[GroupAddedKey] = $"You have been added to group {PlaceholderKeys.Group}";
                loc[GroupRemoveKey] =  $"You have been removed from group {PlaceholderKeys.Group}";
                loc[RoleAddedKey] = $"You have been added to Discord Role {DefaultKeys.Role.Name}";
                loc[RoleRemoveKey] = $"You have been removed from Discord Role {DefaultKeys.Role.Name}";
                
                DiscordMessageTemplates templates = DiscordRoles.Instance.Templates;
                templates.RegisterLocalizedTemplateAsync(DiscordRoles.Instance, GroupAddedTemplate, DiscordRoles.Instance.CreatePrefixedTemplateEmbed(loc[GroupAddedKey], DiscordColor.Success), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
                templates.RegisterLocalizedTemplateAsync(DiscordRoles.Instance, GroupRemoveTemplate, DiscordRoles.Instance.CreatePrefixedTemplateEmbed(loc[GroupRemoveKey], DiscordColor.Danger), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
                templates.RegisterLocalizedTemplateAsync(DiscordRoles.Instance, RoleAddedTemplate, DiscordRoles.Instance.CreatePrefixedTemplateEmbed(loc[RoleAddedKey], DiscordColor.Success), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
                templates.RegisterLocalizedTemplateAsync(DiscordRoles.Instance, RoleRemoveTemplate, DiscordRoles.Instance.CreatePrefixedTemplateEmbed(loc[RoleRemoveKey], DiscordColor.Danger), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            }
        }
        #endregion

        #region Configuration\Notifications\ServerNotificationSettings.cs
        public class ServerNotificationSettings : BaseNotifications
        {
            [JsonProperty(PropertyName = "Send Message To Game Server")]
            public bool SendMessageToServer { get; set; }
            
            [JsonProperty(PropertyName = "Discord Message Channel ID")]
            public Snowflake DiscordMessageChannelId { get; set; }
            
            [JsonConstructor]
            public ServerNotificationSettings() { }
            
            public ServerNotificationSettings(ServerNotificationSettings settings) : base(settings)
            {
                SendMessageToServer = settings?.SendMessageToServer ?? false;
                DiscordMessageChannelId = settings?.DiscordMessageChannelId ?? default(Snowflake);
            }
            
            public override void Initialize()
            {
                GroupAddedKey = LocalizationKey + LangKeys.Announcements.GroupAdded;
                GroupRemoveKey = LocalizationKey + LangKeys.Announcements.GroupRemoved;
                RoleAddedKey = LocalizationKey + LangKeys.Announcements.RoleAdded;
                RoleRemoveKey = LocalizationKey + LangKeys.Announcements.RoleRemoved;
                
                GroupAddedTemplate = new TemplateKey(GroupAddedKey);
                GroupRemoveTemplate = new TemplateKey(GroupRemoveKey);
                RoleAddedTemplate = new TemplateKey(RoleAddedKey);
                RoleRemoveTemplate = new TemplateKey(RoleRemoveKey);
            }
            
            public override void AddLocalizations(Dictionary<string, string> loc)
            {
                loc[GroupAddedKey] = $"{DefaultKeys.Player.Name} has been added to server group {PlaceholderKeys.Group}";
                loc[GroupRemoveKey] = $"{DefaultKeys.Player.Name} has been removed from server group {PlaceholderKeys.Group}";
                loc[RoleAddedKey] = $"{DefaultKeys.Player.Name} has been added to discord role {PlaceholderKeys.Group}";
                loc[RoleRemoveKey] = $"{DefaultKeys.Player.Name} has been removed from discord role {PlaceholderKeys.Group}";
                
                DiscordMessageTemplates templates = DiscordRoles.Instance.Templates;
                templates.RegisterGlobalTemplateAsync(DiscordRoles.Instance, GroupAddedTemplate, DiscordRoles.Instance.CreatePrefixedTemplateEmbed(loc[GroupAddedKey], DiscordColor.Success), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
                templates.RegisterGlobalTemplateAsync(DiscordRoles.Instance, GroupRemoveTemplate, DiscordRoles.Instance.CreatePrefixedTemplateEmbed(loc[GroupRemoveKey], DiscordColor.Danger), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
                templates.RegisterGlobalTemplateAsync(DiscordRoles.Instance, RoleAddedTemplate, DiscordRoles.Instance.CreatePrefixedTemplateEmbed(loc[RoleAddedKey], DiscordColor.Success), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
                templates.RegisterGlobalTemplateAsync(DiscordRoles.Instance, RoleRemoveTemplate, DiscordRoles.Instance.CreatePrefixedTemplateEmbed(loc[RoleRemoveKey], DiscordColor.Danger), new TemplateVersion(1, 0, 0), new TemplateVersion(1, 0, 0));
            }
        }
        #endregion

        #region Configuration\SyncConfig\BaseSyncSettings.cs
        public abstract class BaseSyncSettings
        {
            [JsonProperty(PropertyName = "Server Group Name", Order = 3)]
            public string GroupName { get; set; }
            
            [JsonProperty(PropertyName = "Discord Role ID", Order = 4)]
            public Snowflake RoleId { get; set; }
            
            [JsonIgnore]
            public DiscordRole Role { get; set; }
            
            protected BaseSyncSettings() { }
            
            protected BaseSyncSettings(string groupName, Snowflake roleId)
            {
                GroupName = groupName;
                RoleId = roleId;
            }
            
            protected BaseSyncSettings(BaseSyncSettings settings)
            {
                GroupName = settings?.GroupName ?? "Group";
                RoleId = settings?.RoleId ?? default(Snowflake);
            }
            
            public string GetInfoString(SyncMode mode)
            {
                switch (mode)
                {
                    case SyncMode.Server:
                    return $"{GroupName} -> {Role.Name}";
                    case SyncMode.Discord:
                    return $"{Role.Name} -> {GroupName}";
                    case SyncMode.Bidirectional:
                    return $"{GroupName} <-> {Role.Name}";
                }
                
                return null;
            }
        }
        #endregion

        #region Configuration\SyncConfig\PriorityGroupSettings.cs
        public class PriorityGroupSettings : INotificationSettings
        {
            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Sync Mode (Server, Discord, Bidirectional)")]
            public SyncMode SyncMode { get; set; }
            
            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty("Priority Mode")]
            public PriorityMode PriorityMode { get; set; }
            
            [JsonProperty(PropertyName = "Priority Sync Settings")]
            public List<PrioritySyncSettings> SyncSettings { get; set; }
            
            [JsonProperty(PropertyName = "Server Notification Settings")]
            public ServerNotificationSettings ServerNotifications { get; set; }
            
            [JsonProperty(PropertyName = "Player Notification Settings")]
            public PlayerNotificationSettings PlayerNotifications { get; set; }
            
            [JsonConstructor]
            public PriorityGroupSettings() { }
            
            public PriorityGroupSettings(PriorityGroupSettings settings)
            {
                SyncMode = settings?.SyncMode ?? SyncMode.Server;
                PriorityMode = settings?.PriorityMode ?? PriorityMode.Highest;
                SyncSettings = settings?.SyncSettings ?? new List<PrioritySyncSettings>();
                ServerNotifications = new ServerNotificationSettings(settings?.ServerNotifications);
                PlayerNotifications = new PlayerNotificationSettings(settings?.PlayerNotifications);
                for (int index = 0; index < SyncSettings.Count; index++)
                {
                    SyncSettings[index] = new PrioritySyncSettings(SyncSettings[index]);
                }
            }
        }
        #endregion

        #region Configuration\SyncConfig\PrioritySyncSettings.cs
        public class PrioritySyncSettings : BaseSyncSettings
        {
            [JsonProperty("Sync Priority", Order = 5)]
            public int Priority { get; set; }
            
            [JsonConstructor]
            public PrioritySyncSettings() { }
            
            public PrioritySyncSettings(PrioritySyncSettings settings) : base(settings)
            {
                Priority = settings?.Priority ?? 1;
            }
        }
        #endregion

        #region Configuration\SyncConfig\SyncSettings.cs
        public class SyncSettings : BaseSyncSettings, INotificationSettings
        {
            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Sync Mode (Server, Discord, Bidirectional)", Order = 1)]
            public SyncMode SyncMode { get; set; }
            
            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(PropertyName = "Sync Remove Mode (Remove, Keep)", Order = 2)]
            public RemoveMode RemoveMode { get; set; }
            
            [JsonProperty(PropertyName = "Server Notification Settings", Order = 5)]
            public ServerNotificationSettings ServerNotifications { get; set; }
            
            [JsonProperty(PropertyName = "Player Notification Settings", Order = 6)]
            public PlayerNotificationSettings PlayerNotifications { get; set; }
            
            [JsonConstructor]
            public SyncSettings() { }
            
            public SyncSettings(string groupName, Snowflake discordRole, SyncMode source) : base(groupName, discordRole)
            {
                SyncMode = source;
                RemoveMode = RemoveMode.Remove;
                ServerNotifications = new ServerNotificationSettings(null);
                PlayerNotifications = new PlayerNotificationSettings(null);
            }
            
            public SyncSettings(SyncSettings settings) : base(settings)
            {
                SyncMode = settings?.SyncMode ?? SyncMode.Server;
                RemoveMode = settings?.RemoveMode ?? RemoveMode.Remove;
                ServerNotifications = new ServerNotificationSettings(settings?.ServerNotifications);
                PlayerNotifications = new PlayerNotificationSettings(settings?.PlayerNotifications);
            }
            
            public bool ShouldSerializeRemoveMode() => SyncMode != SyncMode.Bidirectional;
        }
        #endregion

    }

}
