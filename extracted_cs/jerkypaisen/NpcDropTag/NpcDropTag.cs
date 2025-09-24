using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Rust;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("NpcDropTag", "jerkypaisen", "1.0.0")]
    [Description("NpcDropTag")]
    class NpcDropTag : RustPlugin
    {
        #region [Oxide Hooks]
        private void OnEntityDeath(ScientistNPC npc, HitInfo hitInfo)
        {
            if (npc == null)return;
            DoTagSpawns(npc);
        }

        #endregion
        #region [method]
        void DoTagSpawns(ScientistNPC npc)
        {
            var item = ItemManager.CreateByName("dogtagneutral", 1);
            ApplyVelocity(DropNearPosition(item, npc.eyes.position));
        }

        BaseEntity DropNearPosition(Item item, Vector3 pos) => item.CreateWorldObject(pos);

        BaseEntity ApplyVelocity(BaseEntity entity)
        {
            entity.SetVelocity(new Vector3(Random.Range(-4f, 4f), Random.Range(-0.3f, 2f), Random.Range(-4f, 4f)));
            entity.SetAngularVelocity(
                new Vector3(Random.Range(-10f, 10f),
                Random.Range(-10f, 10f),
                Random.Range(-10f, 10f))
            );
            entity.SendNetworkUpdateImmediate();
            return entity;
        }
        #endregion
    }
}
