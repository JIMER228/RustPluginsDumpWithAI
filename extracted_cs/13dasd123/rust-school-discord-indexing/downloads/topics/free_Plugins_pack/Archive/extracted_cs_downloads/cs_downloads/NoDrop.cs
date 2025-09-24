// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿namespace Oxide.Plugins
{
    [Info("No Drop", "Khan", "1.0.0")]
    [Description("Prevent all items from being dropped on Wounded & Death.")]
    public class NoDrop : RustPlugin
    {
        private const bool PreventItemDrop = false;
        private object CanDropActiveItem(BasePlayer player) => PreventItemDrop;
    }
} 