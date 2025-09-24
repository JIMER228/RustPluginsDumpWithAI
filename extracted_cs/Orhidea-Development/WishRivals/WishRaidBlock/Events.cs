using System;
using System.Linq;

namespace Oxide.Plugins
{
    public partial class WishRaidBlock
    {
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity == null) return null;
                if (info == null) return null;
                if (info.InitiatorPlayer == null) return null;
                if (!_raidBlockService.IsOn()) return null;                  // check if on or off
                if (entity is BasePlayer) return null;                          // damage to players ok!!
                if (entity.OwnerID == 0) return null;
                if (entity.OwnerID == info.InitiatorPlayer.userID) return null; // owner can damage own stuff
                //Allow attacking teams stuff
                if (info.InitiatorPlayer.Team != null)
                {
                    if (info.InitiatorPlayer.Team.teamID != 0UL)
                    {
                        if (info.InitiatorPlayer.Team?.members?.Any(teamUserId => teamUserId == entity.OwnerID) == true)
                        {
                            return null;
                        }
                    }
                }


                if (info.InitiatorPlayer != null)
                {

                    if ((bool)Config["RaidBlockInformPlayer"] == true)
                    {
                        if (!_raidBlockService.IsForceActivated())
                        {
                            PrintToChat(info.InitiatorPlayer, lang.GetMessage("nodamage", this, info.InitiatorPlayer.UserIDString)
                                .Replace("{starttime}", ShowTime(_raidBlockService.GetStartTime()))
                                .Replace("{endtime}", ShowTime(_raidBlockService.GetEndTime())));

                        }
                        else
                        {
                            PrintToChat(info.InitiatorPlayer, lang.GetMessage("activated", this, info.InitiatorPlayer.UserIDString));
                        }
                    }
                }

                info.damageTypes.ScaleAll(0.0f);                                // no damage
                return false;
            }
            catch (Exception ex)
            {
                PrintError("Error OnEntityTakeDamage: " + ex.Message);

            }
            return null;
        }
    }
}
