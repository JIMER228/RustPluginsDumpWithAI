using System;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    public class ServerManagerLoader : PluginLoader
    {
        public override Type[] CorePlugins => new[] { typeof(ServerManager) };
    }
}