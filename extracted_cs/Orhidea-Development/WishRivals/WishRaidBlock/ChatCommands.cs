using System.Linq;

namespace Oxide.Plugins
{
    public partial class WishRaidBlock
    {
        [ChatCommand("raidblock")]
        private void raidblock(BasePlayer player, string command, string[] args)
        {
            PrintToChat(player, lang.GetMessage("nodamage", this, player.UserIDString)
                .Replace("{starttime}", ShowTime(_raidBlockService.GetStartTime()))
                .Replace("{endtime}", ShowTime(_raidBlockService.GetEndTime())));

            if (!permission.UserHasPermission(player.UserIDString, adminPriv))
                return;

            if (args.Count() > 0)
            {


                if (args[0].ToLower() == "activate")
                {
                    _raidBlockService.Enable();
                    PrintToChat(lang.GetMessage("activate", this));
                    return;
                }

                if (args[0].ToLower() == "deactivate")
                {
                    _raidBlockService.Disable();
                    PrintToChat(lang.GetMessage("deactivate", this));
                    return;
                }
            }
        }
    }
}
