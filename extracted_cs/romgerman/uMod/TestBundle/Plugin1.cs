using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    public class Plugin1 : CSPlugin
    {
        public override void Load()
        {
            Console.WriteLine("Loaded");
            var e = Directory.Exists("hello");
        }
    }
}
