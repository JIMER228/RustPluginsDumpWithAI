// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Oxide.Core;
using System.Diagnostics;

namespace Oxide.Plugins
{
    public partial class WishLeaderboards
    {
        void OnServerSave()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Interface.Oxide.LogDebug($"START Updating leaderboards");

            LbService.UpdateLeaderboards();
            stopwatch.Stop();
            
            Interface.Oxide.LogDebug($"STOP Updating leaderboards {stopwatch.ElapsedMilliseconds}ms");

        }
    }
}
