using System.Collections.Generic;

namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        public class SteamApiResponse
        {
            #region Properties and Indexers

            public SteamResponse Response { get; set; }

            #endregion
        }

        public class SteamLevelApiResponse
        {
            #region Properties and Indexers

            public SteamLevelResponse Response { get; set; }

            #endregion
        }

        public class SteamBadgeApiResponse
        {
            #region Properties and Indexers

            public SteamBadgeResponse response { get; set; }

            #endregion
        }

        public class SteamBanApiResponse
        {
            #region Properties and Indexers

            public List<SteamBanPlayer> players { get; set; }

            #endregion
        }
    }
}