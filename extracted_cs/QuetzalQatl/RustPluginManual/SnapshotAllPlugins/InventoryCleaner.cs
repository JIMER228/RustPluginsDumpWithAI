// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using UnityEngine;
using Rust;

namespace Oxide.Plugins
{
    [Info("Admin Inventory Cleaner", "TheDoc - Uprising Servers", "1.0.0")]
    class InventoryCleaner : RustPlugin
    {
        [ChatCommand("cleaninv")]
        void cmdChatCleanInv(BasePlayer player, string command, string[] args)
        {
            player.inventory.Strip();
        }
    }
}
