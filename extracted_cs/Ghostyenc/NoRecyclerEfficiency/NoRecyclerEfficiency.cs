
/*           ________               __
           / ____/ /_  ____  _____/ /___  __
          / / __/ __ \/ __ \/ ___/ __/ / / /
         / /_/ / / / / /_/ (__  ) /_/ /_/ /
         \____/_/ /_/\____/____/\__/\__, /
                                   /____/

*/

using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("NoRecyclerEfficiency", "Ghosty", "1.0.0")]
    public class NoRecyclerEfficiency : RustPlugin
    {

        void OnServerInitialized(bool initial)
        {
            int modifiedRecyclers = 0;

            foreach (var networkable in BaseNetworkable.serverEntities)
            {
                if (networkable is Recycler recycler)
                    if (recycler != null)
                {
                    recycler.safezoneRecycleEfficiency = 1f;
                    recycler.radtownRecycleEfficiency = 1f;
                    modifiedRecyclers++;
                }
            }

            Puts("Recycler efficiencies have been restored. ");
        }
    }
}