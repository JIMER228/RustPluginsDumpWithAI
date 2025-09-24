/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Default Item Skin Changer", "VisEntities", "1.0.1")]
    [Description("Changes the default skin of items players receive at spawn.")]
    public class DefaultItemSkinChanger : RustPlugin
    {
        #region Fields

        private static DefaultItemSkinChanger _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Item Skins")]
            public Dictionary<string, ulong> ItemSkins { get; set; }
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

            if (string.Compare(_config.Version, "1.0.1") < 0)
            {
                _config = defaultConfig;
            }

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                ItemSkins = new Dictionary<string, ulong>
                {
                    {
                        "rock", 0
                    },
                    {
                        "torch", 0
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

        private object OnDefaultItemsReceive(PlayerInventory inventory)
        {
            BasePlayer player = inventory.baseEntity;
            if (player == null || !PermissionUtil.HasPermission(player, PermissionUtil.USE))
                return null;

            inventory.Strip();

            foreach (var kvp in _config.ItemSkins)
            {
                string itemShortName = kvp.Key;
                ulong skinId = kvp.Value;
                Item item;

                if (skinId > 0)
                    item = ItemManager.CreateByName(itemShortName, 1, skinId);
                else
                    item = ItemManager.CreateByName(itemShortName, 1);

                if (item != null)
                {
                    item.SetItemOwnership(player, ItemOwnershipPhrases.BornPhrase);
                    inventory.GiveItem(item, inventory.containerBelt);
                }
            }

            return true;
        }

        #endregion Oxide Hooks

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "defaultitemskinchanger.use";
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