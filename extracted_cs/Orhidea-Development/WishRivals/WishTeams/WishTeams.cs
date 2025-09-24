using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using WishInfrastructure;

namespace Oxide.Plugins
{
    [Info("WishTeams", "Latvish", "0.0.1")]
    [Description("WishTeams")]
    public partial class WishTeams : RustPlugin
    {
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
    }
}

