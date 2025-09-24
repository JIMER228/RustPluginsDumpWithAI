// Requires: Clans

using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;


namespace Oxide.Plugins
{
    [Info("Clan Team Sync", "AnotherPanda", "1.0.0")]
    [Description("Syncs clan members with in-game teams automatically.")]
    class ClanTeamSync : CovalencePlugin
    {
        #region Definitions

        [PluginReference]
        private Plugin Clans;

        private readonly Dictionary<string, List<ulong>> clans = new Dictionary<string, List<ulong>>();

        #endregion Definitions

        #region Functions

        private void GenerateClanTeam(string clanTag)
        {
            if (string.IsNullOrEmpty(clanTag))
                return;

            List<ulong> memberIds = ClanPlayersTag(clanTag);
            if (memberIds.Count == 0)
                return;

            if (clans.ContainsKey(clanTag))
                clans.Remove(clanTag);

            clans[clanTag] = new List<ulong>();
            RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance.CreateTeam();

            foreach (ulong memberId in memberIds)
            {
                BasePlayer player = BasePlayer.FindByID(memberId);
                if (player != null)
                {
                    if (player.currentTeam != 0UL)
                    {
                        RelationshipManager.PlayerTeam current = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                        current?.RemovePlayer(player.userID);
                    }
                    team.AddPlayer(player);
                    clans[clanTag].Add(player.userID);

                    if (IsAnOwner(player))
                        team.SetTeamLeader(player.userID);
                }
            }
        }

        private bool IsAnOwner(BasePlayer player)
        {
            string clanTag = Clans?.Call<string>("GetClanOf", player.UserIDString);
            if (string.IsNullOrEmpty(clanTag))
                return false;

            JObject clanInfo = Clans?.Call<JObject>("GetClan", clanTag);
            if (clanInfo == null)
                return false;

            return (string)clanInfo["owner"] == player.UserIDString;
        }

        private List<ulong> ClanPlayersTag(string tag)
        {
            JObject clanInfo = Clans?.Call<JObject>("GetClan", tag);
            if (clanInfo == null || clanInfo["members"] == null)
                return new List<ulong>();

            return clanInfo["members"].ToObject<List<ulong>>();
        }

        private void RemovePlayerFromTeam(ulong playerId)
        {
            BasePlayer player = BasePlayer.FindByID(playerId);
            if (player != null && player.currentTeam != 0UL)
            {
                RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                team?.RemovePlayer(player.userID);
            }
        }

        #endregion Functions

        #region Hooks

        private void OnClanCreate(string tag) => timer.Once(1f, () => GenerateClanTeam(tag));

        private void OnClanUpdate(string tag) => GenerateClanTeam(tag);

        private void OnClanDisbanded(string tag)
        {
            if (string.IsNullOrEmpty(tag) || !clans.TryGetValue(tag, out List<ulong> members) || members.Count == 0)
                return;

            RelationshipManager.PlayerTeam team = null;

            foreach (ulong memberId in members)
            {
                BasePlayer player = BasePlayer.FindByID(memberId);
                if (player != null && player.currentTeam != 0UL)
                {
                    if (team == null)
                        team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);

                    team?.RemovePlayer(player.userID);
                }
            }

            team?.Disband();
            clans.Remove(tag);
        }

        private void OnClanMemberJoined(ulong playerId, string tag) => GenerateClanTeam(tag);

        private void OnClanMemberGone(ulong playerId, string tag)
        {
            RemovePlayerFromTeam(playerId);
            GenerateClanTeam(tag);
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (player == null)
                return;

            string clanTag = Clans?.Call<string>("GetClanOf", player.UserIDString);
            if (string.IsNullOrEmpty(clanTag))
            {
                RemovePlayerFromTeam(player.userID);
                return;
            }

            if (player.currentTeam != 0UL)
            {
                RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                if (team != null)
                {
                    List<ulong> clanPlayers = ClanPlayersTag(clanTag);
                    if (clanPlayers.TrueForAll(id => team.members.Contains(id)))
                        return;
                }
            }

            GenerateClanTeam(clanTag);
        }

        private void OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            if (player == null)
                return;

            timer.Once(0.1f, () =>
            {
                string clanTag = Clans?.Call<string>("GetClanOf", player.UserIDString);
                if (string.IsNullOrEmpty(clanTag))
                    return;

                Clans?.Call("LeaveClan", player.IPlayer);
            });
        }

        private void OnTeamAcceptInvite(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            if (player == null || team == null)
                return;

            BasePlayer leader = BasePlayer.FindByID(team.teamLeader);
            if (leader == null)
                return;

            string clanTag = Clans?.Call<string>("GetClanOf", leader.UserIDString);
            if (string.IsNullOrEmpty(clanTag))
                return;

            string targetClan = Clans?.Call<string>("GetClanOf", player.UserIDString);
            if (!string.IsNullOrEmpty(targetClan))
                return;

            IPlayer leaderIPlayer = leader.IPlayer;
            IPlayer targetIPlayer = player.IPlayer;

            bool invited = Clans?.Call<bool>("InvitePlayer", leaderIPlayer, targetIPlayer) ?? false;
            if (!invited)
                return;

            Clans?.Call("JoinClan", targetIPlayer, clanTag);
        }

        #endregion Hooks
    }
}