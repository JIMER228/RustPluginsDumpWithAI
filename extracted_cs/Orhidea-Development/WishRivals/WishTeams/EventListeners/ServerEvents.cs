using Oxide.Core;
using System.Diagnostics;

namespace Oxide.Plugins
{
    public partial class WishTeams
    {
        void OnServerSave()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Interface.Oxide.LogDebug($"START Performing database save WishTeams");

            Database.SaveDatabase();

            stopwatch.Stop();
            Interface.Oxide.LogDebug($"END Performing database save WishTeams - {stopwatch.ElapsedMilliseconds}ms");

        }
    }
}
