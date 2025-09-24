/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Oxide.Core;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Balloon Stay On", "VisEntities", "1.0.0")]
    [Description("Stops hot air balloons from turning off automatically.")]
    public class BalloonStayOn : RustPlugin
    {
        #region Fields

        private static BalloonStayOn _plugin;

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

        private object OnHotAirBalloonToggle(HotAirBalloon balloon, BasePlayer player)
        {
            if (balloon == null || player == null)
                return null;

            if (balloon.OnlyOwnerAccessible() && player != balloon.creatorEntity)
                return null;

            bool turnOn = !balloon.IsOn();
            balloon.SetFlag(BaseEntity.Flags.On, turnOn, false, true);

            if (turnOn)
            {
                if (!PermissionUtil.HasPermission(player, PermissionUtil.USE))
                {
                    balloon.Invoke(new Action(balloon.ScheduleOff), 60f);
                }
            }
            else
            {
                balloon.CancelInvoke(new Action(balloon.ScheduleOff));
            }

            Interface.CallHook("OnHotAirBalloonToggled", balloon, player);
            return true;
        }

        #endregion Oxide Hooks

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "balloonstayon.use";
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