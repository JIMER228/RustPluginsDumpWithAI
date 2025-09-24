// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿namespace Oxide.Plugins
{
    [Info("Hello World", "open_mailbox", "0.0.1")]
    class HelloWorld : CovalencePlugin
    {
        private void Init()
        {
            Puts("Hello World!");
        }

        private void OnPlayerConnected(Network.Message packet)
        {
            Puts("OnPlayerConnected works!");
        }
    }
}
