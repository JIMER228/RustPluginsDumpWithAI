/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Rust;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("No Cave Building", "VisEntities", "1.1.0")]
    [Description("Prevents players from building inside caves.")]
    public class NoCaveBuilding : RustPlugin
    {
        #region Fields

        private static NoCaveBuilding _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Detection Radius")]
            public float DetectionRadius { get; set; }

            [JsonProperty("Prevent Building In Caves")]
            public bool PreventBuildingInCaves { get; set; }

            [JsonProperty("Prevent Building Under Rock Formations")]
            public bool PreventBuildingUnderRockFormations { get; set; }
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
                DetectionRadius = 10f,
                PreventBuildingInCaves = true,
                PreventBuildingUnderRockFormations = false
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

        private object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            if (planner == null || prefab == null)
                return null;

            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null)
                return null;

            if (PermissionUtil.HasPermission(player, PermissionUtil.IGNORE))
                return null;

            if (_config.PreventBuildingInCaves && InsideRestrictedArea(target.position, _config.DetectionRadius, "cave"))
            {
                MessagePlayer(player, Lang.CannotBuildInCave);
                return true;
            }

            if (_config.PreventBuildingUnderRockFormations && InsideRestrictedArea(target.position, _config.DetectionRadius, "formation"))
            {
                MessagePlayer(player, Lang.CannotBuildUnderRockFormation);
                return true;
            }

            return null;
        }

        #endregion Oxide Hooks

        #region Permissions

        private static class PermissionUtil
        {
            public const string IGNORE = "nocavebuilding.ignore";
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
        
        #region Helper Functions

        public static bool InsideRestrictedArea(Vector3 position, float radius, string restrictionType)
        {
            List<Collider> colliders = Pool.Get<List<Collider>>();
            Vis.Colliders(position, radius, colliders, Layers.Mask.World, QueryTriggerInteraction.Ignore);

            bool result = false;

            foreach (Collider collider in colliders)
            {
                if ((restrictionType == "cave" && collider.name.Contains("cave", CompareOptions.OrdinalIgnoreCase)) ||
                    (restrictionType == "formation" && collider.name.Contains("formation", CompareOptions.OrdinalIgnoreCase)))
                {
                    result = true;
                    break;
                }
            }

            Pool.FreeUnmanaged(ref colliders);
            return result;
        }

        #endregion Helper Functions

        #region Localization

        private class Lang
        {
            public const string CannotBuildInCave = "CannotBuildInCave";
            public const string CannotBuildUnderRockFormation = "CannotBuildUnderRockFormation";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.CannotBuildInCave] = "You cannot build inside caves.",
                [Lang.CannotBuildUnderRockFormation] = "You cannot build under rock formations."
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