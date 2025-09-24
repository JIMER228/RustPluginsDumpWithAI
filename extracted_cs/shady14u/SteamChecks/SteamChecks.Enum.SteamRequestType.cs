namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        /// <summary>
        /// Type of Steam request
        /// </summary>
        private enum SteamRequestType
        {
            /// <summary>
            /// Allows to request only one SteamID
            /// </summary>
            IPlayerService,

            /// <summary>
            /// Allows to request multiple SteamID
            /// But only one used
            /// </summary>
            ISteamUser
        }
    }
}