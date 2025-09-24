/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Instant Team Join", "VisEntities", "1.0.0")]
    [Description("Forces players to join teams as soon as they're invited.")]
    public class InstantTeamJoin : RustPlugin
    {
        #region Fields

        private static InstantTeamJoin _plugin;

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

        private void OnTeamInvite(BasePlayer inviter, BasePlayer invitee)
        {
            if (inviter == null || invitee == null)
                return;

            if (!PermissionUtil.HasPermission(inviter, PermissionUtil.USE))
                return;

            var inviterTeam = RelationshipManager.ServerInstance.FindPlayersTeam(inviter.userID);
            if (inviterTeam != null)
                inviterTeam.AcceptInvite(invitee);
        }

        #endregion Oxide Hooks

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "instantteamjoin.use";
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