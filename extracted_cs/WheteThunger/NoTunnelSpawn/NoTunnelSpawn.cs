using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("No Tunnel Spawn", "WhiteThunder", "1.0.0")]
    [Description("Prevents spawning or fetching vehicles in train tunnels.")]
    internal class NoTunnelSpawn : CovalencePlugin
    {
        #region Hooks

        // From SpawnMini.cs
        private bool? OnMyMiniSpawn(BasePlayer player) =>
            HandleSpawnOrFetchAttempt(player);
        private bool? OnMyMiniFetch(BasePlayer player) =>
            HandleSpawnOrFetchAttempt(player);

        // From SpawnModularCar.cs
        private bool? CanSpawnModularCar(BasePlayer player) =>
            HandleSpawnOrFetchAttempt(player);
        private bool? CanFetchMyCar(BasePlayer player) =>
            HandleSpawnOrFetchAttempt(player);

        // From SpawnScrapHeli.cs
        private bool? OnMyHeliSpawn(BasePlayer player) =>
            HandleSpawnOrFetchAttempt(player);
        private bool? OnMyHeliFetch(BasePlayer player) =>
            HandleSpawnOrFetchAttempt(player);

        #endregion

        #region Helper Methods

        private bool? HandleSpawnOrFetchAttempt(BasePlayer player)
        {
            if (IsInTrainTunnel(player.transform.position))
            {
                ReplyToPlayer(player.IPlayer, "ActionBlocked");
                return false;
            }
            return null;
        }

        private static bool IsInTrainTunnel(Vector3 position)
        {
            TrainTrackSpline spline;
            float distanceResult = 0;
            if (TrainTrackSpline.TryFindTrackNearby(position, 10, out spline, out distanceResult))
                return true;

            return false;
        }

        #endregion

        #region Localization

        private void ReplyToPlayer(IPlayer player, string messageName, params object[] args) =>
            player.Reply(string.Format(GetMessage(player, messageName), args));

        private string GetMessage(IPlayer player, string messageName, params object[] args) =>
            GetMessage(player.Id, messageName, args);

        private string GetMessage(string playerId, string messageName, params object[] args)
        {
            var message = lang.GetMessage(messageName, this, playerId);
            return args.Length > 0 ? string.Format(message, args) : message;
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["ActionBlocked"] = "You cannot do that here.",
            }, this, "en");
        }

        #endregion
    }
}
