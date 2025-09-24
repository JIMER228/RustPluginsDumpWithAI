/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Leaderless Team Disband", "VisEntities", "1.0.0")]
    [Description("Breaks up teams when their leader dies.")]
    public class LeaderlessTeamDisband : RustPlugin
    {
        #region Fields

        private static LeaderlessTeamDisband _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Disband If Killed By Player")]
            public bool DisbandIfKilledByPlayer { get; set; }

            [JsonProperty("Disband If Killed By NPC")]
            public bool DisbandIfKilledByNPC { get; set; }

            [JsonProperty("Disband If Killed By Animal")]
            public bool DisbandIfKilledByAnimal { get; set; }
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
                DisbandIfKilledByPlayer = true,
                DisbandIfKilledByNPC = false,
                DisbandIfKilledByAnimal = false
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

        private void OnPlayerDeath(BasePlayer victim, HitInfo deathInfo)
        {
            if (victim == null || deathInfo == null)
                return;

            var team = GetTeam(victim.userID);
            if (team == null || team.teamLeader != victim.userID)
                return;

            BasePlayer attacker = deathInfo.InitiatorPlayer;
            BaseNpc animalAttacker = deathInfo.Initiator as BaseNpc;

            if (attacker != null && attacker == victim)
                return;

            if (attacker != null && _config.DisbandIfKilledByPlayer && AreEnemies(attacker.userID, victim.userID))
            {
                team.Disband();
                return;
            }

            if (animalAttacker != null && _config.DisbandIfKilledByAnimal)
            {
                team.Disband();
                return;
            }

            if (attacker != null && attacker.IsNpc && _config.DisbandIfKilledByNPC)
                team.Disband();
        }

        #endregion Oxide Hooks

        #region Player Relationships

        private RelationshipManager.PlayerTeam GetTeam(ulong playerId)
        {
            if (RelationshipManager.ServerInstance == null)
                return null;

            return RelationshipManager.ServerInstance.FindPlayersTeam(playerId);
        }

        private bool AreEnemies(ulong firstPlayerId, ulong secondPlayerId)
        {
            var attackerTeam = GetTeam(firstPlayerId);
            var victimTeam = GetTeam(secondPlayerId);
            return attackerTeam == null || victimTeam == null || attackerTeam != victimTeam;
        }

        #endregion Player Relationships
    }
}