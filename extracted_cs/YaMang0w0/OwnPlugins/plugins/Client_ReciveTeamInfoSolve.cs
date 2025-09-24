namespace Oxide.Plugins
{
    [Info("Client_ReciveTeamInfoSolve", "YaMang -w-", "1.0.0")]
    [Description("'CLIENT_ReciveTeamInfo: An Item with the same key has already been added. Key: error solve plugins'")]
    public class Client_ReciveTeamInfoSolve : RustPlugin
    {
        // Work for me 2024-09-15
        private void OnPlayerConnected(BasePlayer player)
        {
            if (player.UserIDString != "steamid") return;
            if(player.currentTeam == 0)
            {
                PrintWarning($"{player.displayName} No Team idk");
            }
            else
            {
                PrintWarning($"{player.displayName} Have Team | Team Disband!");

                player.Team.Disband();
                
                player.SendNetworkUpdate();
                player.SendNetworkUpdateImmediate();
            }
        }
    }
}
