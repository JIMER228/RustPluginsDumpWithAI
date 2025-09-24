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
    [Info("Consistent Wind Power", "VisEntities", "1.0.0")]
    [Description("Keeps windmill output stable no matter the weather or height.")]
    public class ConsistentWindPower : RustPlugin
    {
        #region Fields

        private static ConsistentWindPower _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Windmill Power Output")]
            public int WindmillPowerOutput { get; set; }
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
                WindmillPowerOutput = 150
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private object OnWindmillUpdate(ElectricWindmill mill)
        {
            if (mill == null || mill.OwnerID == 0)
                return null;

            BasePlayer owner = FindPlayerById(mill.OwnerID);
            if (owner == null || !PermissionUtil.HasPermission(owner, PermissionUtil.USE))
                return null;

            int desired = Mathf.Clamp(_config.WindmillPowerOutput, 0, mill.maxPowerGeneration);
            if (mill.currentEnergy != desired)
            {
                mill.currentEnergy = desired;
                mill.MarkDirty();
                mill.SendNetworkUpdate();
            }

            return true;
        }

        #endregion Oxide Hooks

        #region Helper Functions

        public static BasePlayer FindPlayerById(ulong playerId)
        {
            return RelationshipManager.FindByID(playerId);
        }

        #endregion Helper Functions

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "consistentwindpower.use";
            private static readonly List<string> _permissions = new List<string>
            {
                USE,
            };

            public static void RegisterPermissions()
            {
                foreach (string perm in _permissions)
                    _plugin.permission.RegisterPermission(perm, _plugin);
            }

            public static bool HasPermission(BasePlayer player, string permission)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permission);
            }
        }

        #endregion Permissions
    }
}