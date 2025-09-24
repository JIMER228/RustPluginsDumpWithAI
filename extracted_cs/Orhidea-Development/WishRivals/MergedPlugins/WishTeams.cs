//Reference: WishInfrastructure
#define DEBUG
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
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
using System.Diagnostics;
using System.Linq;
using WishInfrastructure;
using WishInfrastructure.Models;


//WishTeams created with PluginMerge v(1.0.4.0) by MJSU @ https://github.com/dassjosh/Plugin.Merge
namespace Oxide.Plugins
{
    [Info("WishTeams", "Latvish", "0.0.1")]
    [Description("WishTeams")]
    public partial class WishTeams : RustPlugin
    {
        #region ChatCommands.cs
        [ChatCommand("team")]
        private void stats(BasePlayer player, string command, string[] args)
        {
            _teamsService.AddPlayer(ulong.Parse(args[0]), ulong.Parse(args[1]));
        }
        #endregion

        #region DiscordHook.cs
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
        #endregion

        #region WhitelistService.cs
        object CanUserLogin(string name, string id)
        {
            Interface.Oxide.LogDebug($"Checking if user {name} can login");
            if (!IsWhitelisted(id))
            {
                return "You are not whitelisted";
            }
            return null;
        }
        
        bool IsWhitelisted(string id)
        {
            Interface.Oxide.LogDebug($"Checking if user can login {permission.UserHasPermission(id, permAllow)}");
            return permission.UserHasPermission(id, permAllow);
        }
        void Whitelist(string id)
        {
            permission.GrantUserPermission(id, permAllow, this);
        }
        #endregion

        #region WishTeams.cs
        #region Class Fields
        
        const string permAllow = "whitelist.allow";
        public static DatabaseClient Database { get; set; }
        private ConfigSetup _config;
        private TeamsService _teamsService;
        #endregion
        void Init()
        {
            _config = new ConfigSetup(this);
            
            permission.RegisterPermission(permAllow, this);
            InitInfrastructure();
            _teamsService = new TeamsService(this, Database);
        }
        
        private void InitInfrastructure()
        {
            Database = new DatabaseClient("WishTeams", "WishTeamsClan", this, _config.ConfigFile.DatabaseConfig);
            Database.SetupDatabase();
        }
        
        private void OnServerInitialized()
        {
            InitDiscordClient();
            
        }
        void OnServerShutdown()
        {
            Interface.Oxide.LogDebug($"Shuting down WishTeams");
            _client.Disconnect();
            
        }
        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(ConfigSetup.GetDefaultConfig(), true);
        }
        void OnUserConnected(IPlayer player)
        {
            if (!Database.IsKnownPlayer(player.Id))
            {
                Database.LoadPlayer(player.Id);
            }
            
            Database.SetPlayerData(player.Id, "name", player.Name);
            var userId = ulong.Parse(player.Id);
            var teamId = _teamsService.GetPlayersTeam(userId);
            var basePlayer = BasePlayer.FindByID(ulong.Parse(player.Id));
            
            if (basePlayer.Team == null || basePlayer.Team.teamName != teamId.ToString())
            {
                //if (_teamsService.IsCaptain(userId, teamId))
                //{
                    //    if (_teamsService.TeamExists(teamId) < 2)
                    //    {
                        //        _teamsService.CreateTeam(userId, teamId);
                        //        return;
                    //    }
                    
                //}
                _teamsService.AddPlayer(userId, teamId);
                return;
            }
            Interface.Oxide.LogDebug("Player already in right team");
        }
        #endregion

        #region EventListeners\ServerEvents.cs
        void OnServerSave()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Interface.Oxide.LogDebug($"START Performing database save WishTeams");
            
            Database.SaveDatabase();
            
            stopwatch.Stop();
            Interface.Oxide.LogDebug($"END Performing database save WishTeams - {stopwatch.ElapsedMilliseconds}ms");
            
        }
        #endregion

        #region TeamsService.cs
        public class TeamsService
        {
            private readonly RustPlugin _rustPlugin;
            private readonly DatabaseClient _dbClient;
            
            public TeamsService(RustPlugin rustPlugin, DatabaseClient dbClient)
            {
                _rustPlugin = rustPlugin;
                _dbClient = dbClient;
                RelationshipManager.maxTeamSize = 30;
            }
            //0 nav creatota 1 ir creatota nav leaderis ienacis serveri 2 viss safe
            public int TeamExists(ulong teamId)
            {
                return _dbClient.GetClanDataRaw<int>(teamId.ToString(), "Exists");
            }
            public void InitTeamJoin(ulong playerId, ulong teamId)
            {
                _dbClient.SetPlayerData(playerId.ToString(), "TeamId", teamId.ToString());
                
            }
            //public bool IsCaptain(ulong playerId, ulong teamId)
            //{
                //    return _dbClient.GetClanDataRaw<string>(teamId.ToString(), "CaptainID") == playerId.ToString();
                
            //}
            //public void InitTeamCreation(ulong playerId, ulong teamId)
            //{
                //    _dbClient.SetPlayerData(playerId.ToString(), "TeamId", teamId.ToString());
                //    _dbClient.SetClanData(teamId.ToString(), "Exists", 1);
                //    _dbClient.SetClanData(teamId.ToString(), "CaptainID", playerId.ToString());
                
            //}
            public ulong GetPlayersTeam(ulong playerId)
            {
                return ulong.Parse(_dbClient.GetPlayerDataRaw<string>(playerId.ToString(), "TeamId"));
            }
            
            public void CreateTeam(ulong playerId, ulong teamId)
            {
                Interface.Oxide.LogDebug($"Creating team with id {teamId}, and leader {playerId}");
                var player = BasePlayer.FindByID(playerId);
                
                if (player == null)
                {
                    Interface.Oxide.LogWarning("Cant find player with id {0}", player.userID);
                    return;
                }
                
                PreparePlayerToJoinRequiredTeam(player, teamId);
                
                
                RelationshipManager.PlayerTeam aTeam = RelationshipManager.ServerInstance.CreateTeam();
                aTeam.teamLeader = player.userID;
                aTeam.RemovePlayer(playerId);
                aTeam.AddPlayer(player);
                
                //Domats ka dc role id izmantot ka teamId
                aTeam.teamID = teamId;
                aTeam.teamName = "Team " + player.displayName;
                
                player.TeamUpdate();
                
                _dbClient.SetPlayerData(player.userID.ToString(), "HasTeam", 1);
                
                _dbClient.SetClanData(aTeam.teamID.ToString(), "CaptainName", player.displayName);
                _dbClient.SetClanData(aTeam.teamID.ToString(), "Exists", 2);
                
                
            }
            
            
            public void AddPlayer(ulong playerId, ulong teamId)
            {
                
                var player = BasePlayer.FindByID(playerId);
                
                if (player == null)
                {
                    Interface.Oxide.LogWarning("Cant find player with id {0}", player.userID);
                    return;
                }
                var oldTeam = player.Team;
                if (oldTeam != null)
                {
                    oldTeam.RemovePlayer(playerId);
                    
                    oldTeam.members.ForEach(x =>
                    {
                        var teamate = BasePlayer.FindByID(x);
                        if (teamate != null)
                        {
                            teamate.TeamUpdate();
                        }
                    });
                }
                
                player.ClearTeam();
                
                
                RelationshipManager.ServerInstance.playerToTeam.Remove(teamId);
                Interface.Oxide.LogDebug("Clearing old team");
                //if (PreparePlayerToJoinRequiredTeam(player, teamId))
                //{
                    //    return;
                //}
                
                var teamKeyValue = RelationshipManager.ServerInstance.teams.FirstOrDefault(x => x.Value.teamName == teamId.ToString());
                RelationshipManager.PlayerTeam team = teamKeyValue.Value;
                if (teamKeyValue.Value == null)
                {
                    Interface.Oxide.LogWarning($"Creating team {player.displayName} , {teamId}");
                    team = RelationshipManager.ServerInstance.CreateTeam();
                    team.SetTeamLeader(player.userID);
                    team.teamName = teamId.ToString();
                    _dbClient.SetClanData(teamId.ToString(), "Exists", 1);
                    //return;
                }
                else
                {
                    Interface.Oxide.LogWarning("Removing from team ", player.displayName, teamId);
                    
                    team.RemovePlayer(playerId);
                }
                Interface.Oxide.LogDebug($"Adding player to clan {player.displayName} {teamId}");
                
                
                team.AddPlayer(player);
                Interface.Oxide.LogWarning($"Max team size {RelationshipManager.maxTeamSize}");
                
                //player.TeamUpdate();
            }
            public static bool CanJoinTeam(BasePlayer player, ulong teamId)
            {
                
                if (player.currentTeam == 0UL)
                {
                    return true;
                }
                
                Interface.Oxide.LogWarning("Player {0} already has a team", player.userID);
                return false;
            }
            private bool PreparePlayerToJoinRequiredTeam(BasePlayer player, ulong teamId)
            {
                if (!CanJoinTeam(player, teamId))
                {
                    player.ClearTeam();
                }
                return false;
            }
            private static bool IsAlreadyInRequiredTeam(BasePlayer player, ulong teamId)
            {
                return player.currentTeam == teamId;
            }
            
            
        }
        #endregion

        #region Models\ConfigFile.cs
        public class ConfigFile
        {
            public DatabaseConfig DatabaseConfig { get; set; }
            
        }
        #endregion

        #region Startup\ConfigSetup.cs
        public class ConfigSetup
        {
            
            
            private readonly RustPlugin _plugin;
            public ConfigFile ConfigFile { get; set; }
            
            
            internal ConfigSetup(RustPlugin plugin)
            {
                _plugin = plugin;
                Init();
            }
            
            private void Init()
            {
                ConfigFile = _plugin.Config.ReadObject<ConfigFile>();
            }
            
            internal static object GetDefaultConfig()
            {
                return new ConfigFile()
                {
                    DatabaseConfig = GetDefaultDatabaseConfig(),
                };
            }
            
            private static DatabaseConfig GetDefaultDatabaseConfig()
            {
                return new DatabaseConfig
                {
                    sql_host = "localhost",
                    sql_port = 1234,
                    sql_db = "rust",
                    sql_user = "admin",
                    sql_pass = "password"
                };
            }
        }
        #endregion

    }

}
