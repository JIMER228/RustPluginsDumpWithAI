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
    [Info("Full Water Catchers", "VisEntities", "1.0.0")]
    [Description("Fills water catchers with water instantly when placed.")]
    public class FullWaterCatchers : RustPlugin
    {
        #region Fields

        private static FullWaterCatchers _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Water Catcher Amounts")]
            public Dictionary<string, int> WaterCatcherAmounts { get; set; }
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
                WaterCatcherAmounts = new Dictionary<string, int>
                {
                    {
                        "assets/prefabs/deployable/water catcher/water_catcher_large.prefab",
                        1000
                    },
                    {
                        "assets/prefabs/deployable/water catcher/water_catcher_small.prefab",
                        500
                    }
                }
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

        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            if (planner == null || gameObject == null)
                return;

            BaseEntity entity = gameObject.ToBaseEntity();
            if (entity == null || !(entity is WaterCatcher))
                return;

            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null)
                return;

            if (!PermissionUtil.HasPermission(player, PermissionUtil.USE))
                return;

            WaterCatcher waterCatcher = entity as WaterCatcher;
            if (waterCatcher == null)
                return;

            string prefabName = waterCatcher.PrefabName;
            if (_config.WaterCatcherAmounts.TryGetValue(prefabName, out int waterAmount))
            {
                waterCatcher.AddResource(waterAmount);
            }
        }

        #endregion Oxide Hooks

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "fullwatercatchers.use";
            private static readonly List<string> _permissions = new List<string>
            {
                USE,
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
        }

        #endregion Permissions
    }
}