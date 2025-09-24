/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using HarmonyLib;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Undying Wall Torches", "VisEntities", "1.1.0")]
    [Description("Removes durability loss from wall torches.")]
    public class UndyingWallTorches : RustPlugin
    {
        #region Fields

        private static UndyingWallTorches _plugin;

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

        #endregion Oxide Hooks

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "undyingwalltorches.use";

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

        #region Helper Functions

        public static BasePlayer FindPlayerById(ulong playerId)
        {
            return RelationshipManager.FindByID(playerId);
        }

        #endregion Helper Functions

        #region Harmony Patches

        [AutoPatch]
        [HarmonyPatch(typeof(TorchDeployableLightSource), "TickTorchDurability")]
        public static class TorchDeployableLightSource_TickTorchDurability_Patch
        {
            public static bool Prefix(TorchDeployableLightSource __instance)
            {
                BasePlayer ownerPlayer = FindPlayerById(__instance.OwnerID);
                if (ownerPlayer != null && PermissionUtil.HasPermission(ownerPlayer, PermissionUtil.USE))
                {
                    return false;
                }

                return true;
            }
        }

        #endregion Harmony Patches
    }
}