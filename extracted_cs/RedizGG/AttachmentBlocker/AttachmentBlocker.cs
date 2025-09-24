using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Rust;

namespace Oxide.Plugins
{
    [Info("AttachmentBlocker", "Eric", "1.0.7")]
    [Description("Blocks the usage and crafting of weapon attachments.")]

    class AttachmentBlocker : RustPlugin
    {
        private List<string> BlockedItems = new List<string>
        {
            "weapon.mod.silencer"
        };

        private void OnServerInitialized()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CheckWeaponAttachments(player);
            }
        }

        object OnItemCraft(ItemCraftTask task, BasePlayer player, Item item)
        {
            foreach (var ingredient in task.blueprint.ingredients)
            {
                if (BlockedItems.Contains(ingredient.itemDef.shortname))
                {
                    if (ingredient.amount > 0)
                    {
                        ingredient.amount = 0;
                        SendReply(player, "Blocked attachments can't be crafted!");
                    }
                }
            }
            return null;
        }
        
        void OnItemAddedToContainer(ItemContainer container, Item item, ItemCraftTask task)
        {
            if (BlockedItems.Contains(item.info.shortname))
            {
                var player = container.GetOwnerPlayer();
                if (player != null)
                {
                    SendReply(player, "Blocked attachments are not allowed!");
                }
                item.Remove();
            }
        }

        void OnPlayerActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (newItem != null && BlockedItems.Contains(newItem.info.shortname))
            {
                SendReply(player, "It's not allowed to equip locked attachments into weapons!");
                newItem.Remove();
            }

            if (player != null && newItem != null)
            {
                CheckWeaponAttachments(player);
            }
        }

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            CheckWeaponAttachments(player);
        }

        private void CheckWeaponAttachments(BasePlayer player)
        {
            Item activeItem = player.GetActiveItem();
            if (activeItem != null)
            {
                foreach (var attachment in activeItem.contents.itemList)
                {
                    if (attachment.info != null)
                    {
                        if (BlockedItems.Contains(attachment.info.shortname)){
                            SendReply(player, $"The {attachment.info.name} attachment got removed from your current weapon!");
                            attachment.RemoveFromContainer();
                        }
                    }
                }
            }
        }
    }
}
