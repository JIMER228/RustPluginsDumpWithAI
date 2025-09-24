// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Diagnostics;

namespace Oxide.Plugins
{

    public partial class WishStatistics
    {
        void OnServerSave()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Interface.Oxide.LogDebug($"START Performing database save WishStatistics");

            Database.SaveDatabase();

            stopwatch.Stop();
            Interface.Oxide.LogDebug($"END Performing database save WishStatistics - {stopwatch.ElapsedMilliseconds}ms");
        }

        void OnUserConnected(IPlayer player)
        {
            if (!Database.IsKnownPlayer(player.Id))
            {
                Database.LoadPlayer(player.Id);
            }

            Database.SetPlayerData(player.Id, "name", player.Name);

        }
    }
}
