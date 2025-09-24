namespace Oxide.Plugins
{
    public partial class WishLeaderboards
    {
        [ChatCommand("leaderboards")]
        private void lb1(BasePlayer player, string command, string[] args)
        {
            _guiService.ActivateGui(player);
        }
        [ChatCommand("lb")]
        private void lb2(BasePlayer player, string command, string[] args)
        {
            _guiService.ActivateGui(player);
        }

        [ConsoleCommand("lb_close")]
        private void lb_close(ConsoleSystem.Arg arg)
        {
            _guiService.DestroyGui(BasePlayer.FindByID(ulong.Parse(arg.Args[0])));
        }

        
    }
}
