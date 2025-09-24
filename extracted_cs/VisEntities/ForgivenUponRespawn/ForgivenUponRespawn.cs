/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Network;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Forgiven Upon Respawn", "VisEntities", "1.0.0")]
    [Description("Players are no longer hostile after they respawn.")]
    public class ForgivenUponRespawn : RustPlugin
    {
        #region Fields

        private static ForgivenUponRespawn _plugin;

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

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player == null)
                return;

            if (!PermissionUtil.HasPermission(player, PermissionUtil.USE))
                return;

            if (player.IsHostile())
            {
                double now = TimeEx.currentTimestamp;
                player.State.unHostileTimestamp = now;
                player.DirtyPlayerState();
                player.ClientRPC(RpcTarget.Player("SetHostileLength", player), 0f);
            }
        }

        #endregion Oxide Hooks

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "forgivenuponrespawn.use";
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