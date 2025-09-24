namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        /// <summary>
        /// HTTP Status Codes (positive) and
        /// custom status codes (negative)
        /// 
        /// 200 is successful in all cases
        /// </summary>
        private enum StatusCode
        {
            Success = 200,
            BadRequest = 400,
            Unauthorized = 401,
            Forbidden = 403,
            NotFound = 404,
            MethodNotAllowed = 405,
            TooManyRequests = 429,
            InternalError = 500,
            Unavailable = 503,

            /// <summary>
            /// User has is games and game hours hidden
            /// </summary>
            GameInfoHidden = -100,

            /// <summary>
            /// Invalid steamId
            /// </summary>
            PlayerNotFound = -101,

            /// <summary>
            /// Can also happen, when the SteamAPI returns something unexpected
            /// </summary>
            ParsingFailed = -102
        }
    }
}