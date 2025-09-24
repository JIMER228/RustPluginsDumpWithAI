// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿namespace Oxide.Plugins
{
    partial class RustPluginTemplate : RustPlugin
    {
        partial void initData();

        partial void initCommands();

        partial void initLang();

        partial void initPermissions();

        partial void initGUI();

        private void Loaded()
        {
            initData();
            initCommands();
            initLang();
            initPermissions();
            initGUI();
        }
    }
}