using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Library;

namespace Oxide.Plugins
{
    public class ServerManager : CSPlugin
    {
        public int UpdateInterval = 30;
        
        private Timer.TimerInstance timerInstance;
        private ServerManagerLibrary serverManagerLibrary;

        public ServerManager()
        {
            Name = "ServerManager";
            Title = "Server Manager";
            Author = "Skipcast";
            Version = new VersionNumber(1, 0, 0);
        }

        [HookMethod("OnServerInitialized")]
        private void OnServerInitialized()
        {
            var timer = Interface.Oxide.GetLibrary<Timer>();
            serverManagerLibrary = Interface.Oxide.GetLibrary<ServerManagerLibrary>("ServerManager");

            timerInstance = timer.Repeat(UpdateInterval * 60, -1, () => serverManagerLibrary.CheckForUpdates());
        }
    }
}