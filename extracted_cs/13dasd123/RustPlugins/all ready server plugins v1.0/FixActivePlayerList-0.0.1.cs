using System.Linq;

namespace Oxide.Plugins
{
    [Info("FixActivePlayerList", "rostov114", "0.0.1")] //and fix error: global.playerlist (Facepunch.Raknet)
    class FixActivePlayerList : RustPlugin
    {
        void OnServerInitialized()
        {
            timer.Every(10f, () =>
            {
                BasePlayer errorPlayer = null;

                foreach (var activePlayer in BasePlayer.activePlayerList.ToList())
                {
                    if (!IsValid(activePlayer))
                    {
                        Puts("Kill: " + activePlayer);
                        activePlayer.Kill();
                    }

                    try
                    {
                        var ping = Network.Net.sv.GetAveragePing(activePlayer.net.connection);
                    }
                    catch
                    {
                        errorPlayer = activePlayer;
                    }
                }

                if (errorPlayer != null)
                {
                    Puts($"Removed: {errorPlayer.displayName} ({errorPlayer.userID})");
                    BasePlayer.activePlayerList.Remove(errorPlayer);
                }
            });
        }

        bool IsValid(BasePlayer player)
        {
            if (player is HTNPlayer || player is Scientist || player is NPCPlayer || player.userID < 76561197960265728L)
                return false;
            return true;
        }
    }
}