namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        /// <summary>
        /// Struct for the GetPlayerBans/v1 Web API
        /// </summary>
        public class PlayerBans
        {
            #region Properties and Indexers

            /// <summary>
            /// if the user has a community ban
            /// </summary>
            public bool CommunityBan { get; set; }

            /// <summary>
            /// If the user is economy banned
            /// </summary>
            public bool EconomyBan { get; set; }

            /// <summary>
            /// Amount of game bans
            /// </summary>
            public int GameBanCount { get; set; }

            /// <summary>
            /// When the last ban was, in Unix time
            /// </summary>
            /// <remarks>
            /// The steam profile only shows bans in the last 7 years
            /// </remarks>
            public int LastBan { get; set; }

            /// <summary>
            /// Seems to be true, when the steam user has at least one ban
            /// </summary>
            public bool VacBan { get; set; }

            /// <summary>
            /// Amount of VAC Bans
            /// </summary>
            public int VacBanCount { get; set; }

            #endregion

            #region Methods (Public)

            public override string ToString()
            {
                return $"Community Ban: {CommunityBan} - VAC Ban: {VacBan} " +
                       $"- VAC Ban Count: {VacBanCount} - Last Ban: {LastBan} " +
                       $"- Game Ban Count: {GameBanCount} - Economy Ban: {EconomyBan}";
            }

            #endregion
        }
    }
}