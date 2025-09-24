/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Weaponless Vehicles", "VisEntities", "1.0.0")]
    [Description("Prevents players from dealing damage while mounted on vehicles.")]
    public class WeaponlessVehicles : RustPlugin
    {
        #region Fields

        private static WeaponlessVehicles _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Restricted Vehicle Prefabs")]
            public List<string> RestrictedVehiclePrefabs { get; set; }
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
                RestrictedVehiclePrefabs = new List<string>
                {
                    "minicopter",
                    "rhib",
                    "rowboat",
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

        private void OnEntityTakeDamage(BaseEntity entity, HitInfo hitInfo)
        {
            if (entity == null || hitInfo == null || hitInfo.InitiatorPlayer == null)
                return;

            BasePlayer attacker = hitInfo.InitiatorPlayer;

            if (PermissionUtil.HasPermission(attacker, PermissionUtil.IGNORE))
                return;

            BaseMountable mountedVehicle = attacker.GetMountedVehicle();
            if (mountedVehicle != null && _config.RestrictedVehiclePrefabs.Contains(mountedVehicle.ShortPrefabName))
            {
                hitInfo.damageTypes.Clear();
                MessagePlayer(attacker, Lang.DamageBlocked);           
            }
        }

        #endregion Oxide Hooks

        #region Permissions

        private static class PermissionUtil
        {
            public const string IGNORE = "weaponlessvehicles.ignore";
            private static readonly List<string> _permissions = new List<string>
            {
                IGNORE,
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

        #region Localization

        private class Lang
        {
            public const string DamageBlocked = "DamageBlocked";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.DamageBlocked] = "You cannot deal damage while mounted on a restricted vehicle.",

            }, this, "en");
        }

        private static string GetMessage(BasePlayer player, string messageKey, params object[] args)
        {
            string message = _plugin.lang.GetMessage(messageKey, _plugin, player.UserIDString);

            if (args.Length > 0)
                message = string.Format(message, args);

            return message;
        }

        public static void MessagePlayer(BasePlayer player, string messageKey, params object[] args)
        {
            string message = GetMessage(player, messageKey, args);
            _plugin.SendReply(player, message);
        }

        #endregion Localization
    }
}