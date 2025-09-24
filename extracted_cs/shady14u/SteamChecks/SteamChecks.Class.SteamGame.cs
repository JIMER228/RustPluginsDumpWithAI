namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        public class SteamGame
        {
            #region Properties and Indexers

            public int appid { get; set; }
            public int? playtime_2weeks { get; set; }
            public int playtime_forever { get; set; }
            public int playtime_linux_forever { get; set; }
            public int playtime_mac_forever { get; set; }
            public int playtime_windows_forever { get; set; }

            #endregion
        }
    }
}