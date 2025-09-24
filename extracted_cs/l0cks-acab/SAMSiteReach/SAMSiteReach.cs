using System.Collections.Generic;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SAMSiteReach", "herbs.acab", "1.0.1")]
    [Description("Prevents SAM sites from shooting at targets that are too far away")]
    public class SAMSiteReach : RustPlugin
    {
        private const float DefaultMaxDistance = 150f;
        private Configuration config;

        private class Configuration
        {
            public float MaxDistance { get; set; } = DefaultMaxDistance;
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        private object CanSamSiteShoot(SamSite samSite, BaseEntity target)
        {
            if (samSite == null || target == null) return null;

            float distance = Vector3Ex.Distance2D(samSite.ServerPosition, target.ServerPosition);
            if (distance > config.MaxDistance)
            {
                return false; 
            }

            return null;
        }
    }
}
