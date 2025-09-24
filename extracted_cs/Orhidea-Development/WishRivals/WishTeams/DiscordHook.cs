using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Ext.Discord;
using Oxide.Ext.Discord.Attributes;
using Oxide.Ext.Discord.Constants;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Entities.Gatway;
using Oxide.Ext.Discord.Entities.Gatway.Events;
using Oxide.Ext.Discord.Entities.Messages;
using Oxide.Ext.Discord.Entities.Permissions;
using Oxide.Ext.Discord.Libraries.Command;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Oxide.Plugins
{
    public partial class WishTeams
    {
        [DiscordClient] private DiscordClient _client;

        private const string _apiToken = "MTA0NDY1NzUyODA1Njg0MDI5Mg.GRYxs_.zewj6IbADCy_EMxjWxlTFZfRc_PhvBNruGrJw0";
        private readonly DiscordCommand _dcCommands = Interface.Oxide.GetLibrary<DiscordCommand>();

        private void InitDiscordClient()
        {
            var settings = new DiscordSettings()
            {
                ApiToken = _apiToken,
                LogLevel = Ext.Discord.Logging.DiscordLogLevel.Debug,
            };
            settings.Intents |= GatewayIntents.GuildMessages;
            //settings.Intents |= GatewayIntents.DirectMessages;
            settings.Intents |= GatewayIntents.Guilds;
            //settings.Intents |= GatewayIntents.GuildIntegrations;
            //settings.Intents |= GatewayIntents.GuildPresences;
            settings.Intents |= GatewayIntents.GuildMembers;
            settings.Intents |= GatewayIntents.GuildMessageTyping;
            settings.Intents |= GatewayIntents.DirectMessageTyping;


            RegisterDiscordLangCommand(nameof(RegisterCommand), LangKeys.RegisterCommand, true, true, null
                //new List<Snowflake>() { (Snowflake)897528550083665985 }
                );
            _client.Connect(settings);

        }

        [HookMethod(DiscordExtHooks.OnDiscordGatewayReady)]
        public void OnDiscordGatewayReady(GatewayReadyEvent ready)
        {
            Puts($"Bot connected to:{ready.Guilds.FirstOrDefault().Value.Name}");
        }

        private void RegisterCommand(DiscordMessage message, string cmd, string[] args)
        {
            Interface.Oxide.LogDebug($"Recieved register call from  {message.Author.Username}");

            if (args == null || args.Length != 1)
            {
                InvalidArgs(message);
                return;
            }
            if (!ValidSteamId64(args[0]))
            {
                NotValidId(message, args);
                return;
            }

            var userRoles = _client.Bot.GetGuild(message.GuildId).Roles.Where(x => message.Member.Roles.Contains(x.Key));
            var teamId = GetTeamId(userRoles);
            if (teamId == 0)
            {
                NoTeam(message);
                return;
            }
            if (Database.IsKnownClan(message.Author.Id.ToString()))
            {
                if (Database.GetClanDataRaw<string>(message.Author.Id.ToString(), "WhitelistId") != args[0] && !string.IsNullOrEmpty(Database.GetClanDataRaw<string>(message.Author.Id.ToString(), "WhitelistId")))
                {
                    message.Author.SendDirectMessage(_client, $"You have already been whitelisted using id  {Database.GetClanDataRaw<string>(message.Author.Id.ToString(), "WhitelistId")}");

                    message.DeleteMessage(_client);
                    return;
                }
            }
            message.Author.SendDirectMessage(_client, $"Team {teamId} joined");

            _teamsService.InitTeamJoin(ulong.Parse(args[0]), teamId);

            Whitelist(args[0]);
            message.Author.SendDirectMessage(_client, $"You have been whitelisted {args[0]} servera ip: 49.12.133.163:30109");
            message.DeleteMessage(_client);
            SaveDiscordUser(message, args[0]);

        }

        private static void SaveDiscordUser(DiscordMessage message, string steam64)
        {
            Interface.Oxide.LogDebug($"Saving discord user {message.Author.Id}");
            Database.SetClanData(message.Author.Id.ToString(), "Discord", message.Author.Username);
            Database.SetClanData(message.Author.Id.ToString(), "WhitelistId", steam64);
        }

        private void AlreadyExists(DiscordMessage message, ulong teamId)
        {
            Interface.Oxide.LogDebug($"Team {teamId} already exists");
            message.Author.SendDirectMessage(_client, $"Team {teamId} already exists");
            message.DeleteMessage(_client);
        }

        //private void CreateTeam(DiscordMessage message, string[] args, ulong teamId)
        //{
        //    Interface.Oxide.LogDebug($"Team {teamId} does not exist");
        //    _teamsService.InitTeamCreation(ulong.Parse(args[0]), teamId);
        //    message.Author.SendDirectMessage(_client, $"Team {teamId} created");
        //}

        private void NoTeam(DiscordMessage message)
        {
            Interface.Oxide.LogDebug($"User has not joined a team {message.Author.Username}");
            message.Author.SendDirectMessage(_client, $"Please join a team or contact admin");
            message.DeleteMessage(_client);
        }

        private ulong GetTeamId(IEnumerable<KeyValuePair<Snowflake, DiscordRole>> userRoles)
        {
            return userRoles.FirstOrDefault(x => x.Value.Name.Contains("team")).Key;
        }

        private bool IsLeader(IEnumerable<KeyValuePair<Snowflake, DiscordRole>> userRoles)
        {
            return userRoles.Any(x => x.Value.Name.Contains("Leader"));
        }

        private void InvalidArgs(DiscordMessage message)
        {
            Interface.Oxide.LogDebug($"Invalid args for register user {message.Author.Username}");

            message.Author.SendDirectMessage(_client, $"Please use format /register <steamid64>");
            message.DeleteMessage(_client);
        }

        private void NotValidId(DiscordMessage message, string[] args)
        {
            Interface.Oxide.LogDebug($"Invalid steamid64 for  user {message.Author.Username} steamid64 {args[0]}");

            message.Author.SendDirectMessage(_client, $"Please enter valid steam64id");
            message.DeleteMessage(_client);
        }

        private bool ValidSteamId64(string steamId64)
        {
            return steamId64.Length == 17 && steamId64.All(char.IsDigit);
        }
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.RegisterCommand] = "register",

            }, this);
        }
        public void RegisterDiscordLangCommand(string command, string langKey, bool direct, bool guild, List<Snowflake> allowedChannels)
        {
            //if (direct)
            //{
            //    _dcCommands.AddDirectMessageLocalizedCommand(langKey, this, command);
            //}

            if (guild)
            {
                _dcCommands.AddGuildLocalizedCommand(langKey, this, allowedChannels, command);
            }
        }
        private static class LangKeys
        {
            public const string RegisterCommand = nameof(RegisterCommand);
        }
    }

}