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
    [Info("DropTag", "jerkypaisen", "1.0.0")]
    [Description("Drop Tag")]
    class DropTag : RustPlugin
    {
        #region [Oxide Hooks]
        private void OnEntityDeath(BasePlayer victim, HitInfo hitInfo)
        {
            if (victim == null)
                return;
            DoTagSpawns(victim);
        }
        #endregion
        #region [method]
        void DoTagSpawns(BasePlayer player)
        {
            var item = ItemManager.CreateByName("dogtagneutral", 1);
            ApplyVelocity(DropNearPosition(item, player.eyes.position));
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
