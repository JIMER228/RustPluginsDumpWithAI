using System;
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Remove NPC Suits", "Substrata", "1.0.0")]
    [Description("Remove all NPC suits from players & containers")]

    class RemoveNPCSuits : RustPlugin
    {
        HashSet<string> npcSuits = new HashSet<string>() { "hazmatsuit_scientist", "scientistsuit_heavy", "hazmatsuit_scientist_arctic", "hazmatsuit_scientist_peacekeeper", "jumpsuit.suit", "jumpsuit.suit.blue", "hat.gas.mask", "attire.banditguard" };

        [ConsoleCommand("npcsuits.remove")]
        void CmdNpcSuitsRemove(ConsoleSystem.Arg arg)
        {
            if (!arg.IsRcon && !arg.IsAdmin) return;

            foreach (var player in BasePlayer.allPlayerList)
            {
                RemoveSuits(player.inventory.containerMain);
                RemoveSuits(player.inventory.containerBelt);
                RemoveSuits(player.inventory.containerWear);
            }

            foreach (var ent in BaseNetworkable.serverEntities)
            {
                if (ent is StorageContainer)
                    RemoveSuits((ent as StorageContainer).inventory);
                else if (ent is PlayerCorpse)
                {
                    var corpse = ent as PlayerCorpse;
                    foreach (var container in corpse.containers)
                        RemoveSuits(container);
                    corpse.SendNetworkUpdateImmediate();
                }
                else if (ent is DroppedItemContainer)
                    RemoveSuits((ent as DroppedItemContainer).inventory);
                else if (ent is RidableHorse)
                    RemoveSuits((ent as RidableHorse).inventory);
            }
        }

        void RemoveSuits(ItemContainer container)
        {
            if (container == null) return;

            for (var slot = container.itemList.Count - 1; slot >= 0; slot--)
            {
                var item = container.itemList[slot];
                if (item != null && npcSuits.Contains(item.info.shortname))
                {
                    item.RemoveFromContainer();
                    item.Remove();
                }
            }
        }
    }
}