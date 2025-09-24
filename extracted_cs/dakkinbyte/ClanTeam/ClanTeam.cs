// Requires: Clans

using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Clan Team", "Dakkinbyte", "2.0.1")]
    [Description("Adds clan members to the same team")]
    class ClanTeam : CovalencePlugin
    {
        #region Definitions

        [PluginReference]
        private Plugin Clans;

        private readonly Dictionary<string, List<ulong>> clans = new Dictionary<string, List<ulong>>();

        #endregion Definitions

        #region Functions

        private bool CompareTeams(List<ulong> currentIds, List<ulong> clanIds)
        {
            foreach (ulong clanId in clanIds)
            {
                if (!currentIds.Contains(clanId))
                {
                    return false;
                }
            }

            return true;
        }

        private void GenerateClanTeam(List<ulong> memberIds)
        {
            string clanTag = ClanTag(memberIds[0].ToString());
            if (string.IsNullOrEmpty(clanTag))
            {
                Puts("Failed to retrieve clan tag.");
                return;
            }

            Puts($"Generating team for clan: {clanTag} with members: {string.Join(", ", memberIds)}");

            if (clans.ContainsKey(clanTag))
            {
                clans.Remove(clanTag);
            }

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
                        current.RemovePlayer(player.userID);
                    }
                    team.AddPlayer(player);

                    clans[clanTag].Add(player.userID);

                    if (IsAnOwner(player))
                    {
                        team.SetTeamLeader(player.userID);
                    }
                }
            }
        }

        private bool IsAnOwner(BasePlayer player)
        {
            if (Clans == null)
            {
                Puts("Clans plugin is not loaded or available.");
                return false;
            }

            // string clanId = Clans.Call<string>("GetClanOf", player.userID);
            // if (string.IsNullOrEmpty(clanId))
            // {
            //     Puts($"Player {player.displayName} is not part of any clan. (UserID: {player.userID})");
            //     return false;
            // }

            // Retrieve the clan tag instead of a clan ID
            //string clanTag = Clans.Call<string>("GetClanOf", player.userID);
            string clanTag = Clans.Call<string>("GetClanOf", player.UserIDString);
            Puts($"Retrieved clan tag for player {player.displayName} (UserID: {player.UserIDString}): {clanTag}");

            if (string.IsNullOrEmpty(clanTag))
            {
                Puts($"Player {player.displayName} is not part of any clan. (UserID: {player.UserIDString})");
                return false;
            }

            // Use the clan tag to retrieve clan information
            JObject clanInfo = Clans.Call<JObject>("GetClan", clanTag);
            if (clanInfo == null)
            {
                Puts($"No clan information found for clan tag: {clanTag}");
                return false;
            }

            Puts($"Clan info for clan tag {clanTag}: {clanInfo.ToString()}");

            JToken ownerToken;
            if (!clanInfo.TryGetValue("owner", out ownerToken))
            {
                Puts($"No owner information found for clan tag: {clanTag}");
                return false;
            }

            Puts($"Owner for clan tag {clanTag} is {ownerToken}, checking against player {player.UserIDString}");

            return (string)ownerToken == player.UserIDString;
        }
        
        private string ClanTag(string memberId)
        {
            if (Clans == null)
            {
                Puts("Clans plugin is not loaded or available.");
                return string.Empty;
            }

            //return Clans.Call<string>("GetClanOf", memberId);
            return Clans.Call<string>("GetClanOf", memberId);
        }

        private List<ulong> ClanPlayers(BasePlayer player)
        {
            if (Clans == null)
            {
                Puts("Clans plugin is not loaded or available.");
                return new List<ulong>();
            }

            //JObject clanInfo = Clans.Call<JObject>("GetClan", Clans.Call<string>("GetClanOf", player.userID));
            JObject clanInfo = Clans.Call<JObject>("GetClan", Clans.Call<string>("GetClanOf", player.UserIDString));
            return clanInfo["members"].ToObject<List<ulong>>();
        }

        private List<ulong> ClanPlayersTag(string tag)
        {
            if (Clans == null)
            {
                Puts("Clans plugin is not loaded or available.");
                return new List<ulong>();
            }

            JObject clanInfo = Clans.Call<JObject>("GetClan", tag);
            return clanInfo["members"].ToObject<List<ulong>>();
        }

        #endregion Functions

        #region Hooks

        private void OnClanCreate(string tag)
        {
            if (Clans == null)
            {
                Puts("Clans plugin is not loaded or available.");
                return;
            }

            timer.Once(1f, () =>
            {
                List<ulong> clanPlayers = new List<ulong>();
                JObject clanInfo = Clans.Call<JObject>("GetClan", tag);
                JArray players = clanInfo["members"] as JArray;
                foreach (string memberId in players)
                {
                    ulong clanId;
                    ulong.TryParse(memberId, out clanId);
                    if (clanId != 0UL)
                    {
                        clanPlayers.Add(clanId);
                    }
                }
                GenerateClanTeam(clanPlayers);
            });
        }

        private void OnClanUpdate(string tag)
        {
            if (Clans == null)
            {
                Puts("Clans plugin is not loaded or available.");
                return;
            }

            List<ulong> clanPlayers = ClanPlayersTag(tag);
            GenerateClanTeam(clanPlayers);
        }

        private void OnClanDestroy(string tag)
        {
            if (Clans == null)
            {
                Puts("Clans plugin is not loaded or available.");
                return;
            }

            if (!clans.ContainsKey(tag))
            {
                Puts($"No clan found with tag: {tag}");
                return;
            }

            BasePlayer player = BasePlayer.FindByID(clans[tag][0]);
            if (player != null)
            {
                RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);

                foreach (ulong memberId in clans[tag])
                {
                    team.RemovePlayer(memberId);
                }

                RelationshipManager.ServerInstance.DisbandTeam(team);
                clans.Remove(tag);
            }
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (Clans == null)
            {
                Puts("Clans plugin is not loaded or available.");
                return;
            }

            //if (!string.IsNullOrEmpty(ClanTag(player.userID)))
            if (!string.IsNullOrEmpty(ClanTag(player.UserIDString)))
            {
                List<ulong> clanPlayers = ClanPlayers(player);
                if (player.currentTeam != 0UL)
                {
                    RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                    if (team != null && CompareTeams(team.members, clanPlayers))
                    {
                        return;
                    }
                }

                GenerateClanTeam(clanPlayers);
            }
        }
    }
    #endregion Hooks
}