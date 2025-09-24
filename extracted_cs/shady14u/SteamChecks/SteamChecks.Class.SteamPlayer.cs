namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        public class SteamPlayer
        {
            #region Properties and Indexers

            public string avatar { get; set; }
            public string avatarfull { get; set; }
            public string avatarhash { get; set; }
            public string avatarmedium { get; set; }
            public int commentpermission { get; set; }
            public int communityvisibilitystate { get; set; }
            public string gameextrainfo { get; set; }
            public string gameid { get; set; }
            public string gameserverip { get; set; }
            public string gameserversteamid { get; set; }
            public int lastlogoff { get; set; }
            public string loccountrycode { get; set; }
            public string locstatecode { get; set; }
            public string personaname { get; set; }
            public int personastate { get; set; }
            public int personastateflags { get; set; }
            public string primaryclanid { get; set; }
            public int profilestate { get; set; }
            public string profileurl { get; set; }
            public int timecreated { get; set; }
            public string steamId { get; set; }

            #endregion
        }

        public class SteamBanPlayer
        {
            #region Properties and Indexers

            public string SteamId { get; set; }
            public bool CommunityBanned { get; set; }
            public bool VACBanned { get; set; }
            public int NumberOfVACBans { get; set; }
            public int DaysSinceLastBan { get; set; }
            public int NumberOfGameBans { get; set; }
            public string EconomyBan { get; set; }

            #endregion
        }
    }
}