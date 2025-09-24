using System;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("BlockBedAssign", "Substrata", "1.0.0")]
    [Description("Blocks players from assigning beds/sleeping bags to other players")]

    class BlockBedAssign : RustPlugin
    {
        object CanAssignBed(BasePlayer player, SleepingBag bag, ulong targetPlayerId)
        {
            SendReply(player, "Cannot assign beds/sleeping bags to other players");
            return false;
        }
    }
}