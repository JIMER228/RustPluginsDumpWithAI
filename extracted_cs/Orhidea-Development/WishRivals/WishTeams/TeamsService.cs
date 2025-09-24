using Oxide.Core;
using System.Linq;
using WishInfrastructure;

namespace Oxide.Plugins
{
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
}
