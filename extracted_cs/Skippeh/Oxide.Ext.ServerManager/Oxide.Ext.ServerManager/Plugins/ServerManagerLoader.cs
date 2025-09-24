// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    public class ServerManagerLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(ServerManager) };
    }
}