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
    [Info("Instant Siege Deploy", "VisEntities", "1.0.1")]
    [Description("Siege weapons are placed fully built with no repairs required.")]
    public class InstantSiegeDeploy : RustPlugin
    {
        #region Fields

        private static InstantSiegeDeploy _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Siege Weapon Prefab Names")]
            public List<string> SiegeWeaponPrefabNames { get; set; }
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
                SiegeWeaponPrefabNames = new List<string>
                {
                    "batteringram.constructable.entity",
                    "catapult.constructable.entity",
                    "ballista.constructable.entity",
                    "siegetower.constructable.entity"
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

            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null)
                return;

            if (!PermissionUtil.HasPermission(player, PermissionUtil.USE))
                return;

            BaseEntity entity = gameObject.ToBaseEntity();
            if (entity == null)
                return;

            ConstructableEntity constructable = entity as ConstructableEntity;
            if (constructable == null)
                return;

            string shortPrefabName = entity.ShortPrefabName;
            if (string.IsNullOrEmpty(shortPrefabName))
                return;

            if (_config.SiegeWeaponPrefabNames != null && _config.SiegeWeaponPrefabNames.Contains(shortPrefabName))
            {
                NextTick(() =>
                {
                    if (constructable != null)
                        constructable.OnRepairFinished(player);
                });
            }
        }

        #endregion Oxide Hooks

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "instantsiegedeploy.use";
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