using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ZealAHammer", "Kira", "1.0.0")]
    public class ZealAHammer : RustPlugin
    {
        [ConsoleCommand("checkent")]
        private void CheckEntity(ConsoleSystem.Arg args)
        {
            if (args.Player() == null) return;
            var player = args.Player();
            if (!player.IsAdmin) return;
            RaycastHit h;
            Physics.Raycast(player.eyes.HeadRay(), out h, 100);
            if (h.GetEntity() == null) return;
            var ent = h.GetEntity();
            if (ent == null) return;
            if (ent is CodeLock)
            {
                var codelockMsg = ((CodeLock) ent).whitelistPlayers
                    .Select(plid => covalence.Players.FindPlayerById($"{plid}")).Aggregate(
                        "\n\n<size=15><color=#3D90DA>Codelocks authorized :\n</color></size>",
                        (current, lockplayer) =>
                            current + $"\n<size=13><color=#CECECE>{lockplayer.Name} - {lockplayer.Id}</color></size>");

                player.ChatMessage(codelockMsg);

                return;
            }

            if (!(ent is BuildingBlock)) return;
            var cupboard = ((BuildingBlock) ent).GetBuildingPrivilege();
            var cupboardAuth = cupboard.authorizedPlayers;
            var codelockAuth = new List<ulong>();
            var entities = cupboard.GetBuilding().decayEntities;
            foreach (var codelock in entities.OfType<Door>().Select(door => door.GetComponentInChildren<CodeLock>()))
            {
                foreach (var authplayer in codelock.whitelistPlayers)
                    if (!codelockAuth.Contains(authplayer))
                        codelockAuth.Add(authplayer);
                foreach (var authplayer in codelock.guestPlayers)
                    if (!codelockAuth.Contains(authplayer))
                        codelockAuth.Add(authplayer);
            }

            var message = $"<size=17><color=#CD5050>Cupboard owner</color> : {cupboard.OwnerID}</size>\n";

            message += "\n<size=15><color=#3D90DA>Cupboard authorized :\n</color></size>";

            message = cupboardAuth.Aggregate(message,
                (current, cauth) =>
                    current + $"\n<size=13><color=#CECECE>{cauth.username} - {cauth.userid}</color></size>");

            message += "\n\n<size=15><color=#3D90DA>Codelocks authorized :\n</color></size>";

            message = codelockAuth.Select(lockauth => covalence.Players.FindPlayerById($"{lockauth}")).Aggregate(
                message,
                (current, lockplayer) =>
                    current + $"\n<size=13><color=#CECECE>{lockplayer.Name} - {lockplayer.Id}</color></size>");

            player.ChatMessage(message);
        }
    }
}