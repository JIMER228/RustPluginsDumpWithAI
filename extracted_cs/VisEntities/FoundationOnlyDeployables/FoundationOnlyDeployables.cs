/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Foundation Only Deployables", "VisEntities", "1.0.0")]
    [Description("Restricts deployables to be placed only on building blocks.")]
    public class FoundationOnlyDeployables : RustPlugin
    {
        #region Fields

        private static FoundationOnlyDeployables _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Prefab Names That Can Be Placed On Ground")]
            public List<string> PrefabNamesThatCanBePlacedOnGround { get; set; }
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
                PrefabNamesThatCanBePlacedOnGround = new List<string>
                {
                    "assets/prefabs/deployable/furnace.large/furnace.large.prefab",
                    "assets/prefabs/deployable/oil refinery/refinery_small_deployed.prefab",
                    "assets/prefabs/building/wall.external.high.stone/wall.external.high.stone.prefab",
                    "assets/prefabs/building/wall.external.high.wood/wall.external.high.wood.prefab",
                    "assets/prefabs/building/gates.external.high/gates.external.high.wood/gates.external.high.wood.prefab",
                    "assets/prefabs/building/gates.external.high/gates.external.high.stone/gates.external.high.stone.prefab",
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

        private object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            if (planner == null || prefab == null)
                return null;

            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null)
                return null;

            if (PermissionUtil.HasPermission(player, PermissionUtil.IGNORE))
                return null;

            if (_config.PrefabNamesThatCanBePlacedOnGround.Contains(prefab.fullName))
                return null;

            if (target.entity is BuildingBlock)
                return null;

            SendMessage(player, Lang.CannotPlaceOnGround);
            return true;
        }

        #endregion Oxide Hooks

        #region Permissions

        private static class PermissionUtil
        {
            public const string IGNORE = "foundationonlydeployables.ignore";
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
            public const string CannotPlaceOnGround = "CannotPlaceOnGround";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.CannotPlaceOnGround] = "This item can only be placed on building blocks, ground placement is not allowed.",
            }, this, "en");
        }

        private void SendMessage(BasePlayer player, string messageKey, params object[] args)
        {
            string message = lang.GetMessage(messageKey, this, player.UserIDString);
            if (args.Length > 0)
                message = string.Format(message, args);

            SendReply(player, message);
        }

        #endregion Localization
    }
}