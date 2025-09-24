namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        public class SteamBadge
        {
            #region Properties and Indexers

            public int badgeid { get; set; }
            public int completion_time { get; set; }
            public int level { get; set; }
            public int scarcity { get; set; }
            public int xp { get; set; }

            #endregion
        }
    }
}