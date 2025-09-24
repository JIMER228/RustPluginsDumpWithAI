using System;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Fix Horses", "Substrata", "1.0.0")]
    [Description("Fix immovable horses")]

    class FixHorses : RustPlugin
    {
        void OnServerInitialized()
        {
            foreach (var ent in BaseNetworkable.serverEntities)
            {
                if (ent is RidableHorse horse)
                {
                    horse.obstacleDetectionRadius = 0.25f;
                }
            }
        }

        void OnEntitySpawned(RidableHorse horse)
        {
            horse.obstacleDetectionRadius = 0.25f;
        }
    }
}