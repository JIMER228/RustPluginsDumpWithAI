using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Rust;
using Rust.Modular;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;

namespace Oxide.Plugins
{
    [Info("MyCaravan", "jerkypaisen", "1.0.0")]
    [Description("This is My Caravan.")]
    class MyCaravan : RustPlugin
    {
        #region [Fields]
        private const string PrefabSockets4 = "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab";
        private const string PermissionGiveCar = "mycaravan.givecar";
        #endregion

        #region [Oxide Hooks]
        private void Init()
        {
            permission.RegisterPermission(PermissionGiveCar, this);
            AddCovalenceCommand("mycaravan", "SpawnCarServerCommand");
        }
        #endregion

        #region Commands
        [Command("mycaravan")]
        private void SpawnCarServerCommand(IPlayer iplayer, string cmd, string[] args)
        {
            var player = iplayer.Object as BasePlayer;
            if (!iplayer.IsServer && !VerifyPermissionAny(iplayer, PermissionGiveCar))
                return;

            if (player == null)
            {
                ReplyToPlayer(iplayer, "Command.Give.Error.PlayerNotFound");
                return;
            }
            Vector3 spawnPosition = GetFixedCarPosition(player);
            Quaternion rotation = GetRelativeCarRotation(player);

            SpawnCar(player, spawnPosition, rotation);
        }
        #endregion

        #region [method]
        private void SpawnCar(BasePlayer player, Vector3 spawnPosition, Quaternion rotation)
        {
            var car = GameManager.server.CreateEntity(PrefabSockets4, spawnPosition, rotation) as ModularCar;
            car.enableSaving = false;
            car.spawnSettings.useSpawnSettings = false;

            if (player != null)
                car.OwnerID = player.userID;

            car.Spawn();
            var moduleItem = ItemManager.CreateByItemID(-1040518150);
            var moduleItem2 = ItemManager.CreateByItemID(-1040518150);
            if (moduleItem != null)
            { 
                car.TryAddModule(moduleItem, 0);
                car.TryAddModule(moduleItem2, 2);
            }
        }
        private static Vector3 GetPlayerForwardPosition(BasePlayer player)
        {
            Vector3 forward = player.GetNetworkRotation() * Vector3.forward;
            forward.y = 0;
            return forward.normalized;
        }

        private static Vector3 GetFixedCarPosition(BasePlayer player)
        {
            Vector3 forward = GetPlayerForwardPosition(player);
            Vector3 position = player.transform.position + forward * 3f;
            position.y = player.transform.position.y + 1f;
            return position;
        }
        private static Quaternion GetRelativeCarRotation(BasePlayer player) =>
            Quaternion.Euler(0, player.GetNetworkRotation().eulerAngles.y - 90, 0);

        private bool VerifyPermissionAny(IPlayer player, params string[] permissionNames)
        {
            foreach (var perm in permissionNames)
            {
                if (!permission.UserHasPermission(player.Id, perm))
                {
                    ReplyToPlayer(player, "Generic.Error.NoPermission");
                    return false;
                }
            }
            return true;
        }

        private void ReplyToPlayer(IPlayer player, string messageName, params object[] args) =>
            player.Reply(string.Format(GetMessage(player, messageName), args));
        
        private string GetMessage(IPlayer player, string messageName, params object[] args)
        {
            var message = lang.GetMessage(messageName, this, player.Id);
            return args.Length > 0 ? string.Format(message, args) : message;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Generic.Error.NoPermission"] = "You don't have permission to use this command.",
            }, this);
        }
        #endregion
    }
}
