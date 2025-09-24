using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("itemGiver", "ice cold", "1.0")]
    [Description("Give items to yourself or to others")]
    public class ItemGiver : RustLegacyPlugin
    {
        ItemDataBlock data = null;
        void Loaded()
        {
            if(!permission.PermissionExists("itemgiver.use"))
            {
                permission.RegisterPermission("itemgiver.use", this);
            }
        }
        bool hasAcces(NetUser netuser)
        {
            if (netuser.CanAdmin())
            {
                return true;
            }
            else if (permission.UserHasPermission(netuser.playerClient.userID.ToString(), "itemgiver.use"))
            {
                return true;
            }
            else
            {
            	rust.SendChatMessage(netuser, "You dont have permission for this command");
                return false;
            }
        }
        [ChatCommand("give")]
        void givecmd(NetUser netuser, string command, string[] args)
        {
            NetUser target = null;
            string item = String.Empty;
            int amount = 1;

            if(hasAcces(netuser))
            {
                if (args == null || args.Length != 3)
                {
                    rust.SendChatMessage(netuser, "Usage /give player item amount");
                    return;
                }
                target = rust.FindPlayer(args[0]);
                item = args[1];
                data = DatablockDictionary.GetByName(item);
                amount = Convert.ToInt32(args[2]);
                if(netuser == target) { rust.SendChatMessage(netuser, "Use /i item amount to give to yourself"); return; }
                if(data == null) { rust.SendChatMessage(netuser, "Item doesnt exists"); return; }
                if(target == null) { rust.SendChatMessage(netuser, "Couldn't find the target user " + args[0]); return; }
                GiveItem(netuser, target, data, amount);            
            }
        }
        [ChatCommand("i")]
        void icmd(NetUser netuser, string command, string[] args)
        {
            string item = String.Empty;
            int amount = 1;
            if(hasAcces(netuser))
            {
                if(args == null || args.Length != 2)
                {
                    rust.SendChatMessage(netuser, "Usage /i item amount");
                    return;
                }
                item = args[0];
                amount = Convert.ToInt32(args[1]);
                data = DatablockDictionary.GetByName(item);
                if (data == null) { rust.SendChatMessage(netuser, "Item doesnt exists"); return; }
                GiveSelf(netuser, data, amount);

            }
        }

        private void GiveSelf(NetUser netuser, ItemDataBlock data, int amount)
        {
            PlayerInventory inv = netuser.playerClient.rootControllable.idMain.GetComponent<PlayerInventory>();
            inv.AddItemAmount(data, amount);
            rust.SendChatMessage(netuser, "You gave yourself (" + data.ToString() + ") (" + amount + ")");
        }

        private void GiveItem(NetUser netuser, NetUser target, ItemDataBlock data, int amount)
        {
            PlayerInventory inv = target.playerClient.rootControllable.idMain.GetComponent<PlayerInventory>();
            inv.AddItemAmount(data, amount);
            rust.SendChatMessage(netuser, "You gave (" + data.ToString() + ") (" + amount + ") to " + target.displayName);
        }
    }
}