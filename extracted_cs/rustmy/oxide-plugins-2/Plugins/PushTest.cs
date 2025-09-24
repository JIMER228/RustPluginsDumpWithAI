// Requires: PushAPI

using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("PushTest", "Wulf/lukespragg", 0.1)]
    [Description("Push API test plugin")]

    class PushTest : CovalencePlugin
    {
        [PluginReference] Plugin PushAPI;

        [Command("ptest", "global.ptest")]
        void ChatCommand(IPlayer player, string command, string[] args)
        {
            if (PushAPI == null)
            {
                Puts("Push API is not loaded! http://oxidemod.org/plugins/705/");
                return;
            }

            PushAPI.Call("PushMessage", "This is a test push", "This is a test of the Push API!");
        }
    }
}
