namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        /// <summary>
        /// The badges we reference.
        /// </summary>
        /// <remarks>
        /// Every badge comes with a level, and EXP gained
        /// </remarks>
        private enum Badge
        {
            /// <summary>
            /// Badge for the amount of games owned
            /// </summary>
            /// <remarks>
            /// The level in this badge is exactly to the amount of games owned
            /// E.g. 42 games == level 42 for badge 13
            /// (so not the same as shown on the steam profiles)
            /// </remarks>
            GamesOwned = 13
        }
    }
}