/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Facepunch;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Auto Team Up", "VisEntities", "1.0.0")]
    [Description("Automatically places players into predefined teams on join.")]
    public class AutoTeamUp : RustPlugin
    {
        #region Fields

        private static AutoTeamUp _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Teams")]
            public List<TeamConfig> Teams { get; set; }
        }

        private class TeamConfig
        {
            [JsonProperty("Team Name")]
            public string TeamName { get; set; }

            [JsonProperty("Player Ids")]
            public List<ulong> PlayerIds { get; set; } = new List<ulong>();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                Teams = new List<TeamConfig>
                {
                    new TeamConfig
                    {
                        TeamName = "Admin",
                        PlayerIds = new List<ulong>
                        {
                            76561198000000001UL,
                            76561198000000002UL
                        }
                    },
                    new TeamConfig
                    {
                        TeamName = "Team A",
                        PlayerIds = new List<ulong>
                        {
                            76561198000000003UL,
                            76561198000000004UL
                        }
                    },
                    new TeamConfig
                    {
                        TeamName = "Team B",
                        PlayerIds = new List<ulong>
                        {
                            76561198000000005UL,
                            76561198000000006UL
                        }
                    }
                }
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private void OnServerInitialized(bool isStartup)
        {
            RefreshTeams();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            RefreshTeams();
        }

        #endregion Oxide Hooks

        #region Team Formation

        private void RefreshTeams()
        {
            RelationshipManager rm = RelationshipManager.ServerInstance;

            for (int t = 0; t < _config.Teams.Count; t++)
            {
                TeamConfig teamConfig = _config.Teams[t];

                List<BasePlayer> players = Pool.Get<List<BasePlayer>>();
                players.Clear();

                for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
                {
                    BasePlayer player = BasePlayer.activePlayerList[i];
                    if (teamConfig.PlayerIds.Contains(player.userID))
                    {
                        players.Add(player);
                    }
                }

                if (players.Count == 0)
                {
                    Pool.FreeUnmanaged(ref players);
                    continue;
                }

                RelationshipManager.PlayerTeam oldTeam = null;
                foreach (var kvp in rm.teams)
                {
                    if (kvp.Value.teamName == teamConfig.TeamName)
                    {
                        oldTeam = kvp.Value;
                        break;
                    }
                }

                if (oldTeam != null)
                {
                    oldTeam.Disband();
                }

                RelationshipManager.PlayerTeam newTeam = rm.CreateTeam();
                newTeam.teamName = teamConfig.TeamName;

                for (int j = 0; j < players.Count; j++)
                {
                    BasePlayer member = players[j];
                    member.ClearTeam();
                    rm.playerToTeam.Remove(member.userID);
                    newTeam.AddPlayer(member);
                }

                newTeam.SetTeamLeader(players[0].userID);
                Pool.FreeUnmanaged(ref players);
            }
        }

        #endregion Team Formation
    }
}