using ConVar;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Offline Raid Defenders", "NoSoyLito", "0.0.1")]
    [Description("Protection against offline raids spawning NPCs to fight raiders")]
    public class OfflineRaidDefenders : RustPlugin
    {
        void Init()
        {


        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {

            if (IsPlayerRaider(entity, info) && IsAuthedOffline(entity, info) && IsEntityStructure(entity) && IsDamageExplosion(info))
            {
                Server.Broadcast("El jugador es un " + ColorString("RAIDER", "#fc4e03") + ", y el dueño de la base está " + ColorString("OFFLINE", "#fc4e03"));
                SpawnORD(entity, info);
            }

            return null;
        }

        #region "OnEntityTakeDamage Checks"
        private bool IsDamageExplosion(HitInfo info)
        {
            if (info.damageTypes.GetMajorityDamageType().ToString().Equals("Explosion"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsEntityStructure(BaseCombatEntity ent)
        {
            if (ent.GetType().ToString().Equals("BuildingBlock") || ent.GetType().ToString().Equals("Door"))
            {
                return true;
            }
            return false;
        }

        private bool IsPlayerRaider(BaseCombatEntity entity, HitInfo info)
        {
            // Check if damage initiator is a player
            if (info.InitiatorPlayer != null && !info.InitiatorPlayer.IsNpc)
            {
                // Check if structure has Building Privilege (therefore a TC)
                if (entity.GetBuildingPrivilege() != null)
                {
                    // Check if the Initiator has Building Privileges (therefore TC auth)
                    if (entity.GetBuildingPrivilege().IsAuthed(info.InitiatorPlayer))
                    {
                        Puts("- Player " + info.InitiatorPlayer.displayName + " is not a raider.");
                        return false;
                    }
                    else
                    {
                        Puts("- Player " + info.InitiatorPlayer.displayName + " is raiding this base.");
                        return true;
                    }
                }
                else
                {
                    Puts("- Player " + info.InitiatorPlayer.displayName + " is attacking a base with no TC.");
                    return false;
                }
            }
            return false;
        }

        private bool IsAuthedOffline(BaseCombatEntity entity, HitInfo info)
        {
            if (entity.GetBuildingPrivilege() != null)
            {
                foreach (ProtoBuf.PlayerNameID authed in entity.GetBuildingPrivilege().authorizedPlayers)
                {
                    if (BasePlayer.Find(authed.userid.ToString()))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }
        #endregion

        private void SpawnORD(BaseCombatEntity entity, HitInfo info)
        {
            UnityEngine.Vector3 spawnPoint = GenORDSpawnPoint(entity, info);
            string npcPrefab = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roam.prefab";
            var npcEntity = GameManager.server.CreateEntity(npcPrefab, spawnPoint) as ScientistNPC;
            npcEntity.Spawn();
            Server.Broadcast("Spawned ScientistNPC entity in coords " + npcEntity.GetPosition().ToString());
        }

        // falta checkear:
        // - que no spawnee encima de otra entity
        // - que no spawnee encima del jugador
        private UnityEngine.Vector3 GenORDSpawnPoint(BaseCombatEntity entity, HitInfo info)
        {
            int maxDistance = 10;
            UnityEngine.Vector3 raiderPoint = info.InitiatorPlayer.transform.position;
            UnityEngine.Vector3 spawnPoint = new UnityEngine.Vector3();
            UnityEngine.Vector3 GROUND_LEVEL = new UnityEngine.Vector3(0, 0, 0);

            int n = 0;
            while ((spawnPoint == null || entity.bounds.Contains(spawnPoint) || spawnPoint == raiderPoint || IsPositionInWater(spawnPoint)) && n < 1000)
            {
                spawnPoint = new UnityEngine.Vector3(UnityEngine.Random.Range(raiderPoint.x - maxDistance, raiderPoint.x + maxDistance), 0, UnityEngine.Random.Range(raiderPoint.z - maxDistance, raiderPoint.z + maxDistance));
                spawnPoint.y = GetGroundPosition(spawnPoint);
                n++;
            }
            return spawnPoint;
        }

        private float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos);

            RaycastHit hit;
            if (UnityEngine.Physics.Raycast(new Vector3(pos.x, pos.y + 200f, pos.z), Vector3.down, out hit, Mathf.Infinity, UnityEngine.LayerMask.GetMask("World", "Construction", "Default")))
            {
                return Mathf.Max(hit.point.y, y);
            }

            return y;
        }

        private bool IsPositionInWater(Vector3 position)
        {
            float terrainHeight = TerrainMeta.HeightMap.GetHeight(position);
            float waterHeight = TerrainMeta.WaterMap.GetHeight(position);

            return waterHeight > terrainHeight;
        }

        // Method to color a string
        string ColorString(string text, string color)
        {
            return "<color=" + color + ">" + text + "</color>";
        }
    }
}
