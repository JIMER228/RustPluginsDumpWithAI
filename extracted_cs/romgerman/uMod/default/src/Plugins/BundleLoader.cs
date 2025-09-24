using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace Oxide.Core.Plugins
{
    public class BundleLoader : PluginLoader
    {
        public override string FileExtension => ".dll";

        public override Plugin Load(string directory, string name)
        {
            var fullPath = Path.Combine(directory, name + FileExtension);
            var assembly = Assembly.LoadFile(fullPath);

            var types = assembly.GetTypes();

            foreach(var type in types)
            {
                if (type.BaseType == typeof(CSPlugin))
                {
                    var plugin = (CSPlugin)Activator.CreateInstance(type);
                    plugin.Load();
                    Interface.Oxide.PluginLoaded(plugin);
                }
            }

            return null;
        }
    }
}
