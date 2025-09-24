/*
TODO:
- Figure out how to slower player's descent
- Figure out how to change player's camera angle?
- Figure out how to change player's animation to... OnLadder?
- Add player to dictionary when chute is deployed, check to prevent duplication
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Parachute", "Wulf/lukespragg", "0.1")]
    [Description("Deploys a paracute and slows a player's descent")]

    class Parachute : RustPlugin
    {
        #region Initialization

        const string permChute = "parachute.use";

        void Init()
        {
            #if !RUST
            throw new NotSupportedException("This plugin does not support this game");
            #endif

            permission.RegisterPermission(permChute, this);
        }

        #endregion

        #region Parachute Deployment

        readonly MethodInfo entitySnapshot = typeof(BasePlayer).GetMethod("SendEntitySnapshot", BindingFlags.Instance | BindingFlags.NonPublic);
        readonly Dictionary<string, Timer> chuteTimer = new Dictionary<string, Timer>();

        void DeployChute(BasePlayer player)
        {
            if (!HasPermission(player.UserIDString, permChute)) return;

            var playerPos = player.transform.position;
            var playerRot = player.transform.rotation;

            // Create parachute
            var chute = GameManager.server.CreateEntity("assets/prefabs/misc/parachute/parachute.prefab", playerPos, playerRot);
            chute.gameObject.Identity();
            chute.SetParent(player, "parachute_attach");
            chute.Spawn();

            var oldBody = player.transform.gameObject.GetComponent<Rigidbody>();
            UnityEngine.Object.Destroy(oldBody);

            var newBody = player.transform.gameObject.AddComponent<Rigidbody>();
            newBody.useGravity = true;
            newBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            newBody.mass = 0f;
            newBody.interpolation = RigidbodyInterpolation.Interpolate;
            //newBody.velocity = lastMoveDir + (vector32 * Random.Range(1f, 3f));
            newBody.angularVelocity = Vector3Ex.Range(-1.75f, 1.75f);
            newBody.drag = 0.5f * (newBody.mass / 5f);
            newBody.angularDrag = 0.2f * (newBody.mass / 5f);
            Physics.gravity =  new Vector3(0, -50, 0);

            // Set player view
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, true);
            player.SendConsoleCommand("graphics.fov 100");
        }

        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.WasJustPressed(BUTTON.JUMP) && !player.IsOnGround() && !player.IsFlying())
            {
                var playerPos = player.transform.position;
                var groundPos = GetGroundPosition(playerPos);
                var distance = Vector3.Distance(playerPos, groundPos);

                if (distance > 10f) DeployChute(player);
            }
        }

        #endregion

        #region Parachute Removal

        void KillChute(BasePlayer player)
        {
            // Remove parachute
            foreach (var child in player.children.Where(child => child.name.EndsWith("parachute.prefab")))
            {
                player.RemoveChild(child);
                child.Kill();
            }

            // Restore player view
            player.SendConsoleCommand("graphics.fov 75");
            player.SetPlayerFlag(BasePlayer.PlayerFlags.ThirdPersonViewmode, false);
        }

        void OnPlayerLanded(BasePlayer player)
        {
            KillChute(player);
            if (chuteTimer.ContainsKey(player.UserIDString)) chuteTimer[player.UserIDString].Destroy();
        }

        void OnEntityDeath(BaseEntity entity)
        {
            var player = entity as BasePlayer;
            if (player)
            {
                KillChute(player);
                if (chuteTimer.ContainsKey(player.UserIDString)) chuteTimer[player.UserIDString].Destroy();
            }
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                KillChute(player);
                if (chuteTimer.ContainsKey(player.UserIDString)) chuteTimer[player.UserIDString].Destroy();
            }

        }

        #endregion

        #region Helpers

        T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null) return defaultValue;
            return (T) Convert.ChangeType(Config[name], typeof(T));
        }

        bool HasPermission(string id, string perm) => permission.UserHasPermission(id, perm);

        static Vector3 GetGroundPosition(Vector3 sourcePos)
        {
            RaycastHit hitInfo;
            var groundLayer = LayerMask.GetMask("Terrain", "World", "Construction");
            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo, groundLayer)) sourcePos.y = hitInfo.point.y;
            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos));
            return sourcePos;
        }

        #endregion
    }
}
