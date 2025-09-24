using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Raid Alert", "AnotherPanda", "1.1.0")]
    [Description("Alerts authorized players when their base is being raided")]

    class RaidAlert : CovalencePlugin
    {
        #region References

        [PluginReference]
        private Plugin Clans;

        #endregion

        #region Configuration

        private float alertCooldown;
        private float worldSize;
        private float tcDetectionRange;
        private const float gridSize = 150f;
        private bool debugMode;

        private Dictionary<ulong, float> lastAlertTime = new Dictionary<ulong, float>();
        private List<string> raidWeapons = new List<string>();

        #endregion

        #region Hooks

        private void Init()
        {
            LoadConfigValues();
        }

        private void LoadConfigValues()
        {
            alertCooldown = GetConfig("AlertCooldown", 300f);
            worldSize = GetConfig("WorldSize", 3000f);
            tcDetectionRange = GetConfig("TCRange", 30f);
            raidWeapons = GetConfig("RaidWeapons", GetDefaultRaidWeapons());
            debugMode = GetConfig("DebugMode", false);

            SaveConfig();

            if (worldSize <= 0)
            {
                Puts("WorldSize is not configured correctly. Please edit it in the configuration file.");
            }
        }

        protected override void LoadDefaultConfig()
        {
            Config["AlertCooldown"] = 300f;
            Config["WorldSize"] = 3000f;
            Config["TCRange"] = 30f;
            Config["DebugMode"] = false;
            Config["RaidWeapons"] = GetDefaultRaidWeapons();
            SaveConfig();
        }

        #endregion

        #region Utility Methods

        private T GetConfig<T>(string key, T defaultValue)
        {
            if (Config[key] == null)
            {
                Config[key] = defaultValue;
                return defaultValue;
            }

            try
            {
                return (T)Convert.ChangeType(Config[key], typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        private List<string> GetConfig(string key, List<string> defaultValue)
        {
            if (Config[key] is List<object> objList)
            {
                return objList.Select(o => o.ToString()).ToList();
            }

            Config[key] = defaultValue;
            return defaultValue;
        }

        private List<string> GetDefaultRaidWeapons()
        {
            return new List<string>
            {
                "rocket_basic",
                "rocket_hv",
                "explosive.satchel.deployed",
                "explosive.timed.deployed",
                "grenade.f1.deployed",
                "grenade.beancan.deployed",
                "ammo.rocket.mlrs"
            };
        }

        private bool IsRaidDamage(string weaponName)
        {
            return raidWeapons.Any(weaponName.Contains);
        }

        private BuildingPrivlidge GetNearbyTC(BaseCombatEntity entity)
        {
            List<BuildingPrivlidge> tcList = new List<BuildingPrivlidge>();
            Vis.Entities(entity.transform.position, tcDetectionRange, tcList);
            return tcList.FirstOrDefault();
        }

        private bool IsAuthorizedOrClanMember(BasePlayer attacker, BuildingPrivlidge tc)
        {
            foreach (var auth in tc.authorizedPlayers)
            {
                if (auth.userid == attacker.userID)
                    return true;
            }

            if (Clans != null)
            {
                string attackerClan = Clans?.Call<string>("GetClanOf", attacker.UserIDString);
                if (!string.IsNullOrEmpty(attackerClan))
                {
                    foreach (var auth in tc.authorizedPlayers)
                    {
                        string authorizedClan = Clans?.Call<string>("GetClanOf", auth.userid.ToString());
                        if (authorizedClan == attackerClan)
                            return true;
                    }
                }
            }

            return false;
        }

        private void NotifyAuthorizedPlayers(BuildingPrivlidge tc, string gridPosition)
        {
            foreach (var auth in tc.authorizedPlayers)
            {
                BasePlayer basePlayer = BasePlayer.FindByID(auth.userid);
                if (basePlayer != null && basePlayer.IsConnected && !HasRecentAlert(basePlayer.userID))
                {
                    SendMessage(basePlayer, $"Your base at grid {gridPosition} is being raided!");
                    lastAlertTime[basePlayer.userID] = Time.realtimeSinceStartup;
                }
            }
        }

        private bool HasRecentAlert(ulong userID)
        {
            return lastAlertTime.ContainsKey(userID) && (Time.realtimeSinceStartup - lastAlertTime[userID]) < alertCooldown;
        }

        private string GetGridPosition(Vector3 position)
        {
            float halfWorld = worldSize / 2f;
            int adjustedX = Mathf.FloorToInt((position.x + halfWorld) / gridSize);
            int adjustedZ = Mathf.FloorToInt((worldSize / gridSize) - ((position.z + halfWorld) / gridSize));
            char column = (char)('A' + adjustedX);
            return $"{column}{adjustedZ}";
        }

        private void SendMessage(BasePlayer player, string message)
        {
            player.ChatMessage(message);
        }

        #endregion

        #region Hooks

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null || info.Initiator == null || worldSize == 0)
                return;

            if (!(info.Initiator is BasePlayer attacker))
                return;

            string weaponName = info.WeaponPrefab?.ShortPrefabName ?? "Unknown";
            if (!IsRaidDamage(weaponName))
                return;

            BuildingPrivlidge tc = GetNearbyTC(entity);
            if (tc != null)
            {
                if (!debugMode && IsAuthorizedOrClanMember(attacker, tc))
                    return;

                string gridPosition = GetGridPosition(entity.transform.position);
                NotifyAuthorizedPlayers(tc, gridPosition);
            }
        }

        #endregion
    }
}
