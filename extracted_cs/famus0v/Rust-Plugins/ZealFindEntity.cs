using System;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealFindEntity", "Kira", "1.0.0")]
    public class ZealFindEntity : RustPlugin
    {
        private int reader = 0;

        [ConsoleCommand("findentity")]
        private void FindEntity(ConsoleSystem.Arg args) 
        {
            if (args.Player() == null) return;
            var player = args.Player();
            RaycastHit h; 
            Physics.Raycast(player.eyes.HeadRay(), out h, 1000);
            if (h.GetEntity() == null) return;
            var ent = h.GetEntity();
            if (ent == null) return;
            var message = $"<size=11>NAME : {ent.ShortPrefabName}\n" +
                          $"PREFAB : {ent.PrefabName}</size>\n" +
                          $"POSITION :{ent.transform.position} " +
                          $"SKIN : {ent.skinID} " +
                          $"ROTATION :{ent.GetNetworkRotation()} " +
                          $"PREFABID : {ent.net.ID}";
            player.ChatMessage(message);
            PrintWarning(message);
            reader = Convert.ToInt32(ent.skinID);
        }

        [ConsoleCommand("findcollider")]
        private void FindCollider(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            RaycastHit h;
            Physics.Raycast(player.eyes.HeadRay(), out h, 1000);
            if (h.GetCollider() == null) return;
            var ent = h.GetCollider();
            if (ent == null) return;
            var transform = ent.transform;
            var message = $"{ent.name} | {transform.position} | {transform.rotation}";
            player.ChatMessage(message);
            PrintWarning(message);
        }
    }
}