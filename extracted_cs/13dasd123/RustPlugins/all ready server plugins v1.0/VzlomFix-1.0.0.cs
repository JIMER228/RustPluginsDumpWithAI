using System.Linq;
using UnityEngine;
using Network;
using System.Collections.Generic;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("fix", "Nimant", "1.0.0")]
    public class VzlomFix : RustPlugin
    {
        object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.authLevel >= 2) return null;
            var command = arg?.cmd?.FullName;
            if (command == null || !command.StartsWith("o.") && !command.StartsWith("oxide.") && !command.StartsWith("debug.") && !command.StartsWith("noclip") && !command.StartsWith("plugin.") && !command.StartsWith("o.show") && !command.StartsWith("god") && !command.StartsWith("ownerid") && !command.StartsWith("removeowner") && !command.StartsWith("moderatorid") && !command.StartsWith("removemoderator") && !command.StartsWith("culling.") && !command.StartsWith("perm.")); return null;
            return false;
        }
    }
}