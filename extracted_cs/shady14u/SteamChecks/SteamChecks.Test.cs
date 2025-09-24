#region Using Statements

using Oxide.Core.Libraries.Covalence;

#endregion

namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        private const string PluginPrefix = "[SteamChecks] ";

        #region Methods (Private)

        /// <summary>
        /// Command, which checks a steamid64 - with the same method when a user joins
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args">steamid64 to test for</param>
        [Command("steamcheck"), Permission("steamchecks.use")]
        private void SteamCheckCommand(IPlayer player, string command, string[] args)
        { 
            if (args.Length != 1)
            {
                TestResult(player, "SteamCheckTests", "You have to provide a SteamID64 as first argument");
                return;
            }

            var steamId = args[0];

            CheckPlayer(steamId, (playerAllowed, reason) =>
            {
                if (playerAllowed)
                    TestResult(player, "CheckPlayer", "The player would pass the checks");
                else
                {
                    webrequest.Enqueue($"https://steamcommunity.com/profiles/{steamId}?xml=1", string.Empty,
                    (code, result) =>
                    {
                        WebHookThumbnail thumbnail = null;
                        if (code >= 200 && code <= 204)
                            thumbnail = new WebHookThumbnail
                            {
                                Url = _steamAvatarRegex.Match(result).Value
                            };
                        SendDiscordMessage(steamId, "The player would not pass the checks. Reason: " + reason, thumbnail);
                    }, this);

                    
                    TestResult(player, "CheckPlayer", "The player would not pass the checks. Reason: " + reason);
                }
            });
        }

        /// <summary>
        /// Unit tests for all Web API functions
        /// Returns detailed results of the queries.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args">steamid64 to test for</param>
        [Command("steamcheck.runtests"), Permission("steamchecks.use")]
        private void SteamCheckTests(IPlayer player, string command, string[] args)
        {
            if (args.Length != 1)
            {
                TestResult(player, "SteamCheckTests", "You have to provide a SteamID64 as first argument");
                return;
            }

            var steamId = args[0];

            GetSteamLevel(steamId,
                (statusCode, response) =>
                {
                    TestResult(player, "GetSteamLevel",
                        $"Status {(StatusCode)statusCode} - Response {response}");
                });

            GetPlaytimeInformation(steamId,
                (statusCode, response) =>
                {
                    TestResult(player, "GetPlaytimeInformation",
                        $"Status {(StatusCode)statusCode} - Response {response}");
                });

            GetSteamPlayerSummaries(steamId,
                (statusCode, response) =>
                {
                    TestResult(player, "GetSteamPlayerSummaries",
                        $"Status {(StatusCode)statusCode} - Response {response}");
                });

            GetSteamBadges(steamId, (statusCode, response) =>
            {
                if ((StatusCode)statusCode == StatusCode.Success)
                {
                    var badgeLevel = ParseBadgeLevel(response, Badge.GamesOwned);
                    TestResult(player, "GetSteamBadges - Badge 13, Games owned",
                        $"Status {(StatusCode)statusCode} - Response {badgeLevel}");
                }
                else
                {
                    TestResult(player, "GetSteamBadges",
                        $"Status {(StatusCode)statusCode}");
                }
            });

            GetPlayerBans(steamId,
                (statusCode, response) =>
                {
                    TestResult(player, "GetPlayerBans",
                        $"Status {(StatusCode)statusCode} - Response {response}");
                });
        }

        private void TestResult(IPlayer player, string function, string result)
        {
            player.Reply(PluginPrefix + $"{function} - {result}");
        }

        #endregion
    }
}