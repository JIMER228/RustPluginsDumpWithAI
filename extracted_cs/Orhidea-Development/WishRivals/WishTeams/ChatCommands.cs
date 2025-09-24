namespace Oxide.Plugins
{
    public partial class WishTeams
    {
        [ChatCommand("team")]
        private void stats(BasePlayer player, string command, string[] args)
        {
            _teamsService.AddPlayer(ulong.Parse(args[0]), ulong.Parse(args[1]));
        }
        
        
    }
}
