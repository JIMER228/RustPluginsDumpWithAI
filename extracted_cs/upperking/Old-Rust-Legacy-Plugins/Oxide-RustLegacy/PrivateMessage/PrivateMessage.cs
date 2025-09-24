using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("PrivateMessage", "ice cold", "1.0")]
    [Description("Send prive messages to other people")]
    public class PrivateMessage : RustLegacyPlugin
    {

        public Dictionary<NetUser, NetUser> lastsend = new Dictionary<NetUser, NetUser>();

        [ChatCommand("pm")]
        void pmcmd(NetUser sender, string command, string[] args)
        {
            if (args.Length != 2)
            {
                SendReply(sender, "Usage /pm player \"message\"");
                return;
            }
            NetUser target = rust.FindPlayer(args[0]);
            if (target == null) { SendReply(sender, "Couldn't find the target user"); return; }
            rust.SendChatMessage(target, "PrivateMessage", "[color#228b22](" + sender.displayName + " >=  You): [color aqua]" + args[1]);
            rust.SendChatMessage(sender, "PrivateMessage", "[color#228b22](You >= " + target.displayName + "): [color aqua]" + args[1]);
            if (lastsend.ContainsKey(target)) { lastsend.Remove(target); lastsend.Add(target, sender); return; }
            lastsend.Add(target, sender);
        }
        [ChatCommand("r")]
        void replycmd(NetUser netuser, string command, string[] args)
        {
            if (args.Length != 1)
            {
                SendReply(netuser, "Usage /r \"Message\"");
                return;
            }
            if(!lastsend.ContainsKey(netuser)) { SendReply(netuser, "You dont have anyone to reply"); return; }
            NetUser target = lastsend[netuser];
            if(target == null) { SendReply(netuser, "Player isnt online"); lastsend.Remove(netuser); return; }
            rust.SendChatMessage(target, "PrivateMessage", "[color#228b22](" + netuser.displayName + " >=  You): [color aqua]" + args[0]);
            rust.SendChatMessage(netuser, "PrivateMessage", "[color#228b22](You >= " + target.displayName + "): [color aqua]" + args[0]);
            
        }
    }
}
