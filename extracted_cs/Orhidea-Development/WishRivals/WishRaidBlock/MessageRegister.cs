using Oxide.Core;
using Oxide.Core.Libraries;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    public class MessageRegister
    {
        private readonly RustPlugin _plugin;
        private readonly Lang _lang;

        public MessageRegister(RustPlugin plugin)
        {
            _plugin = plugin;
            _lang = Interface.Oxide.GetLibrary<Lang>();
        }
        public void Register()
        {
            RegisterEnglishMessages();
        }

        private void RegisterEnglishMessages()
        {
            _lang.RegisterMessages(new Dictionary<string, string>
                {
                    { "localtime", "Local time is {localtime}." },
                    { "nodamage", "You cannot cause damage to buildings from {starttime} until {endtime}" },
                    { "activated", "You cannot cause damage while Raid block is activated." },
                    { "starts", "Raid block starts at {starts}." },
                    { "onstatus", "Raid block is ON. It is active  from @ {starttime} until {endtime}" },
                    { "offstatus", "Raid block is {status}. It will NOT become active." },
                    { "activate", "- Raid Block is enabled"},
                    { "deactivate", "- Raid Block is disabled"},
            }, _plugin, "en");
        }
    }
}
