// TO DO

// - make a Spawn BradleyCrashSite Command
// - spawns randomly on road
// - kills itself
// - announces crash in chat
// - spawns scientists a certain value away from the current position of Bradley

using ConVar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("NPC Events", "DepressingDressing & LostStormX", "1.0.0")]
    [Description("Creates and controls different NPC Events such as Bradley Crashes, ect.")]

    class NPC_Events : RustPlugin
    {
        private float respawnDelay = 3600;

        private DateTime wipeTime;
        private readonly TimeSpan onehour = TimeSpan.FromHours(1);
        private void Init()
        {
            wipeTime = DateTime.UtcNow;
        }

        private void SpawningBradleyAtLocation(Vector3 position)
        {
            BaseEntity bradley = GameManager.server.CreateEntity("assets/prefabs/npc/military/barricade/barricade.prefab", position, Quaternion.identity);

            DateTime currentTime = DateTime.UtcNow;
            TimeSpan timeElapsed = currentTime - wipeTime;

            if (timeElapsed < onehour && bradley != null)
            {
                bradley.Spawn();
                return;
            }
        }
    }
}