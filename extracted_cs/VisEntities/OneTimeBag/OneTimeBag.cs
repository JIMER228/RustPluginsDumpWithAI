// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Facepunch;
using Rust;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("One Time Bag", "VisEntities", "1.0.0")]
    [Description("Turns sleeping bags into single-use respawn points.")]
    public class OneTimeBag : RustPlugin
    {
        #region Fields

        private static OneTimeBag _plugin;
        private const int LAYER_BEDS = Layers.Mask.Deployed;

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

            SleepingBag bag = GetClosestBag(player);
            if (bag != null)
                bag.Kill();
        }

        #endregion Oxide Hooks

        #region Bag Retrieval

        private SleepingBag GetClosestBag(BasePlayer player)
        {
            List<SleepingBag> nearbyBeds = Pool.Get<List<SleepingBag>>();
            SleepingBag firstBag = null;

            Vis.Entities(player.transform.position, 2f, nearbyBeds, LAYER_BEDS, QueryTriggerInteraction.Ignore);

            foreach (SleepingBag bed in nearbyBeds)
            {
                if (bed != null && bed.OwnerID == player.userID)
                {
                    firstBag = bed;
                    break;
                }
            }

            Pool.FreeUnmanaged(ref nearbyBeds);
            return firstBag;
        }

        #endregion Bag Retrieval

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "onetimebag.use";
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