using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Network;


namespace Oxide.Plugins
{
    [Info("CassetteTapeDump", "jerkypaisen", "1.0.0")]
    [Description("Output all cassette tapes on your server to ogg files.")]
    class CassetteTapeDump : RustPlugin
    {
        [ConsoleCommand("tapedump")]
        private void CmdCassetteTapeDump(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || (arg.Connection != null && arg.Connection.authLevel == 2))
            {
                foreach (BaseNetworkable serverEntity in BaseNetworkable.serverEntities)
                {
                    if (serverEntity is Cassette cassette)
                    {
                        byte[] array = FileStorage.server.Get(cassette.AudioId, FileStorage.Type.ogg, serverEntity.net.ID);
                        if (array != null)
                            File.WriteAllBytes(cassette.AudioId.ToString() + ".ogg", array);
                    }
                }
            }
        }
    }
}
