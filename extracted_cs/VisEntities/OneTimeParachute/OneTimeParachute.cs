/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("One Time Parachute", "VisEntities", "1.0.0")]
    [Description("Parachutes are removed as soon as players land.")]
    public class OneTimeParachute : RustPlugin
    {
        #region Fields

        private static OneTimeParachute _plugin;

        #endregion Fields

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            _plugin = null;
        }

        private void OnItemRemove(Item item)
        {
            if (item == null)
                return;

            BasePlayer player = item.GetOwnerPlayer();
            if (player == null)
                return;

            if (PermissionUtil.HasPermission(player, PermissionUtil.IGNORE))
                return;

            Parachute parachute = player.GetMountedVehicle() as Parachute;
            if (parachute != null)
                parachute.ConditionLossPerUse = 1.0f;

            return;
        }

        #endregion Oxide Hooks

        #region Permissions

        private static class PermissionUtil
        {
            public const string IGNORE = "onetimeparachute.ignore";
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
    }
}