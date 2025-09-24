using Oxide.Core.Plugins;
using Oxide.Core;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel;
using System;

namespace Oxide.Plugins
{
    [Info("StreamerVoiceOnly", "jerky", "1.0.0")]
    [Description("This is StreamerVoiceOnly.")]
    class StreamerVoiceOnly : RustPlugin
    {
        private const string PermissionUse = "streamervoiceonly.use";

        private void Init()
        {
            permission.RegisterPermission(PermissionUse, this);
        }

        object OnPlayerVoice(BasePlayer player, Byte[] data)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionUse)) return true;
            return null;
        }
 
    }
}
