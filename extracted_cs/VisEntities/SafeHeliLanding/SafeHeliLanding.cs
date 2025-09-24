/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using HarmonyLib;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Safe Heli Landing", "VisEntities", "1.1.0")]
    [Description("Lets players land helicopters safely on their own bases.")]
    public class SafeHeliLanding : RustPlugin
    {
        #region Fields

        private static SafeHeliLanding _plugin;

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
            public const string USE = "safehelilanding.use";
            private static readonly List<string> _permissions = new List<string>
            {
                USE,
            };

            public static string ConstructPermission(string suffix, bool addToList = true)
            {
                string perm = string.Join(".", nameof(SafeHeliLanding), suffix).ToLower();

                if (addToList && !_permissions.Contains(perm))
                    _permissions.Add(perm);

                return perm;
            }

            public static void AddPermission(string permission)
            {
                if (!_permissions.Contains(permission))
                    _permissions.Add(permission);
            }

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

        #region Harmony Patches

        [AutoPatch]
        [HarmonyPatch(typeof(BaseHelicopter), "ProcessCollision")]
        public static class BaseHelicopter_ProcessCollision_Patch
        {
            public static bool Prefix(BaseHelicopter __instance, Collision collision)
            {             
                BasePlayer driver = __instance.GetDriver();
                if (driver == null)
                    return true;

                if (PermissionUtil.HasPermission(driver, PermissionUtil.USE) && driver.IsBuildingAuthed())
                    return false;

                return true;
            }
        }

        #endregion Harmony Patches
    }
}