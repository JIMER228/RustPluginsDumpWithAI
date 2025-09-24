using System;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("ZealWipeAnnouncer", "Kira", "1.0.0")]
    public class ZealWipeAnnouncer : RustPlugin
    {
        private DateTime _lastWipe = new DateTime(1970, 1, 1, 0, 0, 0);
        private DateTime _unblock;
 
        private void OnServerInitialized()
        {
            var save = SaveRestore.SaveCreatedTime;
            _lastWipe = new DateTime(save.Year, save.Month, save.Day, 11, 0, 0);
            _unblock = _lastWipe.AddHours(5);
            PrintWarning($"LastWipe : {_lastWipe:f}");
            PrintWarning($"Unblock : {_unblock:f}");
        }

        [HookMethod("IsBlock")]
        private bool IsBlock()
        {
            var parseTime = _unblock - DateTime.Now;
            return parseTime.TotalSeconds > 1;
        }
    }
}