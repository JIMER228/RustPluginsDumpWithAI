using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{


    //!!!!!!!!!!!!!!!Have to copy this class to make all the saving happen

    public partial class DatabaseUsageTemplate
    {
        void OnServerSave()
        {
            Interface.Oxide.LogDebug($"Performing database save");
            Database.SaveDatabase();
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
