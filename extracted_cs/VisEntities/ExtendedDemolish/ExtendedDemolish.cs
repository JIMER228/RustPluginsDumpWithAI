/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Extended Demolish", "VisEntities", "1.0.0")]
    [Description("Lets you change how long players can demolish their own structures.")]
    public class ExtendedDemolish : RustPlugin
    {
        #region Fields

        private static ExtendedDemolish _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Demolish Times")]
            public Dictionary<string, int> DemolishTimes { get; set; }
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
                DemolishTimes = new Dictionary<string, int>
                {
                    ["default"] = 600,
                    ["vip"] = 1200
                }
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            BuildPermissionsFromConfig();
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            if (planner == null || gameObject == null)
                return;

            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null)
                return;

            BaseEntity entity = gameObject.ToBaseEntity();
            if (entity == null)
                return;

            StabilityEntity stabilityEntity = entity as StabilityEntity;
            if (stabilityEntity == null)
                return;

            float demolishTime = GetDemolishTimeForPlayer(player);

            stabilityEntity.CancelInvoke(stabilityEntity.StopBeingDemolishable);
            stabilityEntity.Invoke(stabilityEntity.StopBeingDemolishable, demolishTime);
        }

        #endregion Oxide Hooks

        #region Helper Functions

        private float GetDemolishTimeForPlayer(BasePlayer player)
        {
            float demolishTime = StabilityEntity.demolish_seconds;

            foreach (var kvp in _config.DemolishTimes)
            {
                string suffix = kvp.Key;
                string fullPerm = PermissionUtil.ConstructPermission(suffix);
                if (permission.UserHasPermission(player.UserIDString, fullPerm))
                {
                    demolishTime = kvp.Value;
                    break;
                }
            }

            return demolishTime;
        }

        private void BuildPermissionsFromConfig()
        {
            foreach (var kvp in _config.DemolishTimes)
            {
                string permission = PermissionUtil.ConstructPermission(kvp.Key);
                PermissionUtil.AddPermission(permission);
            }
        }

        #endregion Helper Functions

        #region Permissions

        private static class PermissionUtil
        {
            private static readonly List<string> _permissions = new List<string>
            {
                
            };

            public static void RegisterPermissions()
            {
                foreach (var permission in _permissions)
                {
                    _plugin.permission.RegisterPermission(permission, _plugin);
                }
            }

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permissionName);
            }

            public static string ConstructPermission(string suffix)
            {
                return string.Join(".", nameof(ExtendedDemolish), suffix).ToLower();
            }

            public static void AddPermission(string permission)
            {
                if (!_permissions.Contains(permission))
                    _permissions.Add(permission);
            }
        }

        #endregion Permissions
    }
}