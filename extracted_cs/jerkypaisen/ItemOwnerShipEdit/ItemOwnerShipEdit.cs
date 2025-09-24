using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("ItemOwnerShipEdit", "Jerky/Copilot", "1.0.0")]
    [Description("This plugin allows you to change the owner text or description of the item you're holding to any desired string.")]
    public class ItemOwnerShipEdit : RustPlugin
    {
        [ChatCommand("aos")]
        private void ItemAddOwnerShipCommand(BasePlayer player, string command, string[] args)
        {
            if (player.Connection == null || (player.Connection != null && player.Connection.authLevel == 2))
            {
                if (args.Length != 3)
                {
                    SendReply(player, "Usage: /aos <name> <description> <quantity>");
                    return;
                }

                string itemOwnerName = args[0];
                string itemDescription = args[1];
                int itemQuantity;

                if (!int.TryParse(args[2], out itemQuantity))
                {
                    SendReply(player, "Quantity must be a number.");
                    return;
                }

                player.Command("inventory.addownership", itemOwnerName, itemDescription, itemQuantity);

                SendReply(player, $"Added {itemQuantity} of {itemOwnerName} with description '{itemDescription}' to your inventory.");
            }
            else
            {
                SendReply(player, "You do not have permission to use this command.");
            }
        }

        [ChatCommand("cos")]
        private void ItemChangeOwnerShipCommand(BasePlayer player, string command, string[] args)
        {
            if (player.Connection == null || (player.Connection != null && player.Connection.authLevel == 2))
            {
                if (args.Length != 2)
                {
                    SendReply(player, "Usage: /cos <name> <description>");
                    return;
                }

                string itemOwnerName = args[0];
                string itemDescription = args[1];
                player.Command("inventory.convertownership", itemOwnerName, itemDescription, 1);

                SendReply(player, $"Added {itemOwnerName} with description '{itemDescription}' to your inventory.");
            }
            else
            {
                SendReply(player, "You do not have permission to use this command.");
            }
        }
    }
}