/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Durable Attachments", "VisEntities", "1.0.0")]
    [Description("Prevents durability loss on all weapon attachments.")]
    public class DurableAttachments : RustPlugin
    {
        #region Fields

        private static DurableAttachments _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Attachment Shortnames That Never Lose Condition")]
            public List<string> AttachmentShortnamesThatNeverLoseCondition { get; set; }
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
                AttachmentShortnamesThatNeverLoseCondition = new List<string>
                {
                    "weapon.mod.flashlight",
                    "weapon.mod.simplesight",
                    "weapon.mod.muzzlebrake",
                    "weapon.mod.muzzleboost",
                    "weapon.mod.holosight",
                    "weapon.mod.lasersight",
                    "weapon.mod.extendedmags",
                    "weapon.mod.silencer",
                    "weapon.mod.small.scope",
                    "weapon.mod.8x.scope"
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

        private void OnLoseCondition(Item item, ref float amount)
        {
            if (amount <= 0f || item == null)
                return;

            if (!_config.AttachmentShortnamesThatNeverLoseCondition.Contains(item.info.shortname))
                return;

            Item weapon = item.parentItem;
            if (weapon == null ||
                weapon.contents == null ||
                weapon.info.category != ItemCategory.Weapon ||
                !weapon.contents.itemList.Contains(item))
                return;

            BasePlayer owner = item.GetOwnerPlayer();
            if (owner == null || !PermissionUtil.HasPermission(owner, PermissionUtil.USE))
                return;

            amount = 0f;
        }

        #endregion Oxide Hooks

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "durableattachments.use";
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