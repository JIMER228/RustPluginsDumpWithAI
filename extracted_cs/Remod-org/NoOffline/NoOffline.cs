using Oxide.Core;
using Oxide.Core.Plugins;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("NoOffline", "RFC1920", "1.0.3")]
    [Description("Disables offline raid based on building privilege members, friends, clans, or teams")]
    class NoOffline : RustPlugin
    {
        [PluginReference]
        private readonly Plugin Friends, Clans, RustIO;
        private ConfigData configData;

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitinfo)
        {
            if (entity == null) return null;
            if (hitinfo.damageTypes.GetMajorityDamageType() == Rust.DamageType.Decay) return null;

            bool foundOnline = false;
            BuildingPrivlidge tc = entity.GetBuildingPrivilege();

            if (tc != null)
            {
                DoLog("Checking authorized players");
                foreach (ulong playerid in tc.authorizedPlayers.Select(x => x.userid).ToArray())
                {
                    DoLog($"Checking player {playerid}");
                    BasePlayer player = BasePlayer.Find(playerid.ToString());
                    if (player != null && BasePlayer.activePlayerList.Contains(player))
                    {
                        DoLog($"Damage allowed because an authorized player, {player.displayName}, is online.");
                        foundOnline = true;
                    }
                    if (configData.Options.HonorRelationships)
                    {
                        foreach (BasePlayer peep in BasePlayer.activePlayerList)
                        {
                            if (IsFriend(peep.userID, playerid))
                            {
                                DoLog($"Damage allowed because a friend of an authorized player, {peep.displayName}, is online.");
                                foundOnline = true;
                            }
                        }
                    }
                }
                if (foundOnline) return null; // Normal behavior
                return true;
            }

            return null;
        }

        private void DoLog(string message)
        {
            if (configData.Options.debug) Interface.Oxide.LogInfo(message);
        }

        private bool IsFriend(ulong playerid, ulong ownerid)
        {
            if (configData.Options.useFriends && Friends != null)
            {
                object fr = Friends?.CallHook("AreFriends", playerid, ownerid);
                if (fr != null && (bool)fr)
                {
                    return true;
                }
            }
            if (configData.Options.useClans && Clans != null)
            {
                string playerclan = (string)Clans?.CallHook("GetClanOf", playerid);
                string ownerclan = (string)Clans?.CallHook("GetClanOf", ownerid);
                if (playerclan != null && ownerclan != null && playerclan == ownerclan)
                {
                    return true;
                }
            }
            if (configData.Options.useTeams)
            {
                BasePlayer player = BasePlayer.FindByID(playerid);
                if (player != null)
                {
                    if (player.currentTeam != 0)
                    {
                        RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
                        if (playerTeam != null && playerTeam.members.Contains(ownerid))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        #region config
        private void LoadConfigVariables()
        {
            configData = Config.ReadObject<ConfigData>();

            configData.Version = Version;
            SaveConfig(configData);
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            ConfigData config = new ConfigData
            {
                Version = Version
            };

            SaveConfig(config);
        }
        private void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }

        public class ConfigData
        {
            public Options Options = new Options();
            public VersionNumber Version;
        }

        public class Options
        {
            public bool debug;
            public bool useFriends;
            public bool useClans;
            public bool useTeams;
            public bool HonorRelationships;
        }
        #endregion
    }
}
