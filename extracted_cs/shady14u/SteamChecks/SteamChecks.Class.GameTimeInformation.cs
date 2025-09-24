namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        /// <summary>
        /// Struct for the GetOwnedGames API request
        /// </summary>
        private class GameTimeInformation
        {
            #region Constructors

            public GameTimeInformation(int gamesCount, int playtimeRust, int playtimeAll)
            {
                GamesCount = gamesCount;
                PlaytimeRust = playtimeRust;
                PlaytimeAll = playtimeAll;
            }

            #endregion

            #region Properties and Indexers

            /// <summary>
            /// Amount of games the user has
            /// </summary>
            public int GamesCount { get; set; }

            /// <summary>
            /// Play time across all Steam games
            /// </summary>
            public int PlaytimeAll { get; set; }

            /// <summary>
            /// Play time in rust
            /// </summary>
            public int PlaytimeRust { get; set; }

            #endregion

            #region Methods (Public)

            public override string ToString()
            {
                return
                    $"Games Count: {GamesCount} - Playtime in Rust: {PlaytimeRust} - Playtime all Steam games: {PlaytimeAll}";
            }

            #endregion
        }
    }
}