#region Using Statements

using System.Collections.Generic;

#endregion

namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        public class SteamResponse
        {
            public int? game_count;

            #region Properties and Indexers

            public List<SteamGame> games { get; set; }
            public List<SteamPlayer> players { get; set; }

            #endregion
        }

        public class SteamLevelResponse
        {
            public int player_level { get; set; }
        }

        public class SteamBadgeResponse
        {
            #region Properties and Indexers

            public List<SteamBadge> badges { get; set; }
            public int player_level { get; set; }
            public int player_xp { get; set; }
            public int player_xp_needed_current_level { get; set; }
            public int player_xp_needed_to_level_up { get; set; }

            #endregion
        }
        
    }
}