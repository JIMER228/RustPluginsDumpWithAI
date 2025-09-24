using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Fast Supply Drop", "Djimbou", "1.0.2")]
    [Description("Makes supply drops fall faster while preventing underground issues")]

    public class FastSupplyDrop : RustPlugin
    {
        private const float FallSpeed = -25f; // Safe falling speed
        private const float MinHeight = 5f; // Minimum height above terrain to prevent underground issues

        private void OnEntitySpawned(BaseEntity entity)
        {
            if (entity == null || entity.PrefabName == null) return;

            if (entity.PrefabName.Contains("supply_drop"))
            {
                NextTick(() => AdjustSupplyDrop(entity));
            }
        }

        private void AdjustSupplyDrop(BaseEntity entity)
        {
            if (entity == null || entity.IsDestroyed) return;

            Rigidbody rb = entity.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Ensure we're not too close to the ground
                float groundHeight = TerrainMeta.HeightMap.GetHeight(entity.transform.position);
                if (entity.transform.position.y - groundHeight < MinHeight)
                {
                    // Reposition if too close to ground
                    Vector3 newPosition = entity.transform.position;
                    newPosition.y = groundHeight + MinHeight;
                    entity.transform.position = newPosition;
                }

                // Configure physics
                rb.drag = 0f;
                rb.useGravity = true;
                rb.velocity = new Vector3(0f, FallSpeed, 0f);
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                
                // Add safety checks
                entity.gameObject.layer = (int)Layer.Reserved1; // Helps with collision
                
                Puts($"[FastSupplyDrop] Adjusted supply drop at {entity.transform.position}");
            }
        }
    }
}
