// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Oxide.Core;
using UnityEngine;
using Facepunch;
using Rust;
using System.Collections;
using Oxide.Plugins.TeamBattle.Data;

namespace Oxide.Plugins.TeamBattle
{
    public static class TeleportSystem
    {
        private static TeamBattle Plugin;
        
        public static void Init(TeamBattle plugin)
        {
            Plugin = plugin;
        }
        
        public static Vector3 GetDefaultSpawnPoint()
        {
            var spawnPoints = ConVar.Server.spawnpoints;
            if (spawnPoints != null && spawnPoints.Count > 0)
            {
                return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
            }
            
            return new Vector3(
                ConVar.Server.worldsize * 0.5f,
                100f,
                ConVar.Server.worldsize * 0.5f
            );
        }
        
        public static bool SafeTeleport(BasePlayer player, Vector3 position)
        {
            if (player == null || !player.IsConnected || position == Vector3.zero) return false;

            if (position.y < TerrainMeta.HeightMap.GetHeight(position))
            {
                position.y = TerrainMeta.HeightMap.GetHeight(position) + 1f;
            }

            Plugin.ServerMgr.Instance.StartCoroutine(DelayedTeleport(player, position));
            return true;
        }
        
        private static IEnumerator DelayedTeleport(BasePlayer player, Vector3 position)
        {
            if (player == null || !player.IsConnected || position == Vector3.zero) yield break;

            player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, true);
            player.SendNetworkUpdateImmediate();
            player.ClientRPCPlayer(null, player, "StartLoading");

            yield return CoroutineEx.waitForSeconds(0.5f);

            if (player == null || !player.IsConnected) yield break;

            player.Teleport(position);
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Spectating, false);
            player.SendNetworkUpdateImmediate();
            PlayTeleportEffect(player);
        }
        
        public static void PlayTeleportEffect(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;

            Effect.server.Run("assets/prefabs/misc/particle_teleport.prefab", player.transform.position);
            Effect.server.Run("assets/prefabs/misc/sound_teleport.prefab", player.transform.position);
        }
        
        public static Vector3 FindSafeSpawnPoint(int roomId, string team)
        {
            if (Configuration.ManualSpawnPoints.TryGetValue(roomId, out var teamSpawns) && 
                teamSpawns.TryGetValue(team, out var points))
            {
                foreach (var point in points)
                {
                    if (IsSpawnPointSafe(point))
                        return point;
                }
            }

            float teamOffset = team == "Team1" ? 10f : -10f;
            float randomOffset = UnityEngine.Random.Range(-5f, 5f);
            
            Vector3 basePos = new Vector3(
                300f * roomId + teamOffset,
                100f,
                -100f + randomOffset
            );

            basePos.y = TerrainMeta.HeightMap.GetHeight(basePos) + 1f;
            return basePos;
        }
        
        private static bool IsSpawnPointSafe(Vector3 point)
        {
            if (point.y < TerrainMeta.WaterMap.GetHeight(point))
                return false;

            var entities = new List<BaseEntity>();
            Vis.Entities(point, 3f, entities);
            return !entities.Any(e => e is BasePlayer);
        }
    }
}