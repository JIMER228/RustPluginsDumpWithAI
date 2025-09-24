// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿namespace Oxide.Plugins
{
    [Info("__Template", "<author>", "1.0.0")]
    [Description("<optional_description>")]
    public class __Template : RustPlugin
    {
        private void OnServerInitialized()
        {
            Puts("Hello world!");
        }
    }
}
