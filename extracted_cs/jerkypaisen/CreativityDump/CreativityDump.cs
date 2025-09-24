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
    [Info("CreativityDump", "jerky", "1.0.0")]
    [Description("Output all creativity on your server to files.")]
    class CreativityDump : RustPlugin
    {
        [ConsoleCommand("alldump")]
        private void CmdAllDump(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || (arg.Connection != null && arg.Connection.authLevel == 2))
            {
                foreach (BaseNetworkable serverEntity in BaseNetworkable.serverEntities)
                {
                    if (serverEntity is PhotoFrame photoFrame)
                    {
                        for (int j = 0; j < photoFrame.GetContentCRCs.Length; j++)
                        {
                            uint num = photoFrame.GetContentCRCs[j];
                            if (num != 0)
                            {
                                byte[] array = FileStorage.server.Get(num, FileStorage.Type.png, photoFrame.net.ID);
                                if (array != null)
                                    File.WriteAllBytes("PhotoFrame_" + num.ToString() + ".png", array);
                            }
                        }
                    }
                    if (serverEntity is PhotoEntity photoEntity)
                    {
                        byte[] array = FileStorage.server.Get(photoEntity.CrcToLoad, FileStorage.Type.jpg, photoEntity.net.ID);
                        if (array != null)
                            File.WriteAllBytes("Photo_" + photoEntity.CrcToLoad.ToString() + ".jpg", array);
                    }
                    if (serverEntity is Signage signage)
                    {
                        for (int j = 0; j < signage.TextureCount; j++)
                        {
                            uint num = signage.textureIDs[j];
                            if (num != 0)
                            {
                                byte[] array = FileStorage.server.Get(num, FileStorage.Type.png, signage.net.ID);
                                if (array != null)
                                    File.WriteAllBytes("Sign_" + num.ToString() + ".png", array);
                            }
                        }
                    }
                    if (serverEntity is Cassette cassette)
                    {
                        byte[] array = FileStorage.server.Get(cassette.AudioId, FileStorage.Type.ogg, cassette.net.ID);
                        if (array != null)
                            File.WriteAllBytes("Cassette_" + cassette.AudioId.ToString() + ".ogg", array);
                    }
                }
            }
        }
    }
}
