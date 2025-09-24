using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("SwimmingDrops", "RustFlash", "1.0.0")]
    [Description("Keeps supply drops swimming smoothly on water after they touch the water surface")]
    public class SwimmingDrops : RustPlugin
    {
        private const float FloatHeight = 0.1f;
        private const float SmoothTime = 0.5f;
        private const float LandingThreshold = 0.1f;
        private HashSet<SupplyDrop> activeDrops = new HashSet<SupplyDrop>();

        void OnEntitySpawned(SupplyDrop drop)
        {
            if (drop == null) return;
            drop.gameObject.AddComponent<DropMonitor>().Init(this);
        }

        void OnEntityKill(SupplyDrop drop)
        {
            if (drop == null) return;
            activeDrops.Remove(drop);
        }

        void Unload()
        {
            foreach (var drop in activeDrops)
            {
                if (drop != null && drop.gameObject != null)
                {
                    UnityEngine.Object.Destroy(drop.gameObject.GetComponent<DropMonitor>());
                }
            }
            activeDrops.Clear();
        }

        public void AddActiveDrops(SupplyDrop drop)
        {
            activeDrops.Add(drop);
        }

        private class DropMonitor : MonoBehaviour
        {
            private SupplyDrop drop;
            private Rigidbody rb;
            private bool isInWater = false;
            private bool hasLanded = false;
            private SwimmingDrops pluginInstance;

            public void Init(SwimmingDrops plugin)
            {
                pluginInstance = plugin;
            }

            void Awake()
            {
                drop = GetComponent<SupplyDrop>();
                rb = GetComponent<Rigidbody>();
            }

            void FixedUpdate()
            {
                if (drop == null || drop.IsDestroyed) 
                {
                    Destroy(this);
                    return;
                }

                var waterInfo = WaterLevel.GetWaterInfo(transform.position, true, true);

                if (!isInWater && waterInfo.isValid && transform.position.y <= waterInfo.surfaceLevel)
                {
                    isInWater = true;
                    pluginInstance.AddActiveDrops(drop);
                    DetachParachute();
                }

                if (isInWater)
                {
                    if (!hasLanded && HasLanded())
                    {
                        hasLanded = true;
                    }

                    if (hasLanded)
                    {
                        SmoothFloatSupplyDrop(waterInfo.surfaceLevel);
                    }
                }
            }

            private void SmoothFloatSupplyDrop(float waterLevel)
            {
                var targetY = waterLevel + FloatHeight;
                var currentY = transform.position.y;
                
                float displacementY = targetY - currentY;
                float buoyancyForce = displacementY * 9.81f * rb.mass;

                Vector3 smoothedForce = Vector3.up * buoyancyForce * Time.fixedDeltaTime / SmoothTime;
                rb.AddForce(smoothedForce, ForceMode.Impulse);

                rb.velocity *= 0.98f;

                rb.velocity = new Vector3(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -1f, 1f), rb.velocity.z);
            }

            private bool HasLanded()
            {
                return Mathf.Abs(rb.velocity.y) < LandingThreshold;
            }

            private void DetachParachute()
            {
                var parachute = GetComponentInChildren<Parachute>();
                if (parachute != null)
                {
                    parachute.Kill(BaseNetworkable.DestroyMode.None);
                }
            }
        }
    }
}