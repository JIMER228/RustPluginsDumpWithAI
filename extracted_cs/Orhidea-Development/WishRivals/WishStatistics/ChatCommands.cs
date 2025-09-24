namespace Oxide.Plugins
{
    public partial class WishStatistics
    {
        [ChatCommand("statistics")]
        private void stats(BasePlayer player, string command, string[] args)
        {
            _guiService.ActivateGui(player);
        }
        [ChatCommand("stats")]
        private void stats1(BasePlayer player, string command, string[] args)
        {
            _guiService.ActivateGui(player);
        }

        [ConsoleCommand("stats_close")]
        private void stats_close(ConsoleSystem.Arg arg)
        {
            _guiService.DestroyGui(BasePlayer.FindByID(ulong.Parse(arg.Args[0])));
        }
        [ConsoleCommand("remove_points_clan")]
        private void remove_points_clan(ConsoleSystem.Arg arg)
        {
            Database.SetClanData(arg.Args[0], arg.Args[1], Database.GetClanDataRaw<int>(arg.Args[0], arg.Args[1]) - int.Parse(arg.Args[2]));
        }


    }
}
