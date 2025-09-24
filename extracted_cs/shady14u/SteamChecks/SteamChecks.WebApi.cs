using System;
using System.Linq;
using Newtonsoft.Json;


namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
    
        /// <summary>
        /// Generic request to the Steam Web API
        /// </summary>
        /// <param name="steamRequestType"></param>
        /// <param name="endpoint">The specific endpoint, e.g. GetSteamLevel/v1</param>
        /// <param name="steamId64"></param>
        /// <param name="callback">Callback returning the HTTP status code <see cref="StatusCode"></see> and a JSON JObject</param>
        /// <param name="additionalArguments">Additional arguments, e.g. &foo=bar</param>
        private void SteamWebRequest(SteamRequestType steamRequestType, string endpoint, string steamId64,
            Action<int, string> callback, string additionalArguments = "")
        {
            var requestUrl = $"{apiURL}/{steamRequestType}/{endpoint}/?key={apiKey}&{(steamRequestType == SteamRequestType.IPlayerService ? "steamid" : "steamids")}={steamId64}{additionalArguments}";

            webrequest.Enqueue(requestUrl, "", (httpCode, response) =>
            {
                callback(httpCode, httpCode == (int)StatusCode.Success ? response : null);
            }, this, Core.Libraries.RequestMethod.GET, null, webTimeout);
        }

        /// <summary>
        /// Get the Steam level of a user
        /// </summary>
        /// <param name="steamId64">The users steamId64</param>
        /// <param name="callback">Callback with the statusCode <see cref="StatusCode"></see> and the steam level</param>
        private void GetSteamLevel(string steamId64, Action<int, int> callback)
        {
            SteamWebRequest(SteamRequestType.IPlayerService, "GetSteamLevel/v1", steamId64,
                (httpCode, response) =>
                {
                    if (httpCode == (int)StatusCode.Success)
                    {
                        callback(httpCode, JsonConvert.DeserializeObject<SteamLevelApiResponse>(response).Response.player_level);
                    }
                    else
                    {
                        callback(httpCode, -1);
                    }
                });
        }

        /// <summary>
        /// Get information about hours played in Steam
        /// </summary>
        /// <param name="steamId64">steamId64 of the user</param>
        /// <param name="callback">Callback with the statusCode <see cref="StatusCode"></see> and the <see cref="GameTimeInformation"></see></param>
        /// <remarks>
        /// Even when the user has his profile public, this can be hidden. This seems to be often the case.
        /// When hidden, the statusCode will be <see cref="StatusCode.GameInfoHidden"></see>
        /// </remarks>
        private void GetPlaytimeInformation(string steamId64, Action<int, GameTimeInformation> callback)
        {
            SteamWebRequest(SteamRequestType.IPlayerService, "GetOwnedGames/v1", steamId64,
                (httpCode, response) =>
                {
                    var steamResponse = JsonConvert.DeserializeObject<SteamApiResponse>(response);
                    if (httpCode == (int)StatusCode.Success)
                    {
                        // We need to check if it is null, because the steam-user can hide game information
                        var gamesCount = steamResponse.Response.game_count;
                        if (gamesCount == null)
                        {
                            callback((int)StatusCode.GameInfoHidden, null);
                            return;
                        }

                        var playtimeRust = steamResponse.Response.games
                            .FirstOrDefault(x => x.appid == 252490)?.playtime_forever;
                        
                        if (playtimeRust == null)
                        {
                            callback((int)StatusCode.GameInfoHidden, null);
                            return;
                        }

                        var playtimeAll = steamResponse.Response.games
                            .Sum(x => x.playtime_forever);
                        callback(httpCode, new GameTimeInformation((int)gamesCount, (int)playtimeRust, playtimeAll));
                    }
                    else
                    {
                        callback(httpCode, null);
                    }
                }, "&include_appinfo=false"); // We don't need additional appinfos, like images
        }


        /// <summary>
        /// Get Summary information about the player, like if his profile is visible
        /// </summary>
        /// <param name="steamId64">steamId64 of the user</param>
        /// <param name="callback">Callback with the statusCode <see cref="StatusCode"></see> and the <see cref="PlayerSummary"></see></param>
        private void GetSteamPlayerSummaries(string steamId64, Action<int, PlayerSummary> callback)
        {
            SteamWebRequest(SteamRequestType.ISteamUser, "GetPlayerSummaries/v2", steamId64,
                (httpCode, response) =>
                {
                    var steamResponse = JsonConvert.DeserializeObject<SteamApiResponse>(response);
                    if (httpCode == (int)StatusCode.Success)
                    {
                        if (steamResponse.Response.players.Count() != 1)
                        {
                            callback((int)StatusCode.PlayerNotFound, null);
                            return;
                        }

                        var playerInfo = steamResponse.Response.players[0];

                        var summary = new PlayerSummary
                        {
                            Visibility = (PlayerSummary.VisibilityType)playerInfo.communityvisibilitystate,
                            Profileurl = playerInfo.profileurl
                        };

                        // Account creation time can be only fetched, when the profile is public
                        if (summary.Visibility == PlayerSummary.VisibilityType.Public)
                            summary.Timecreated = playerInfo.timecreated;
                        else
                            summary.Timecreated = -1;

                        // We have to do a separate request to the steam community profile to get infos about limited and
                        // if they set up their profile
                        {
                            // Set defaults, which won't get the user kicked
                            summary.NoProfile = false;
                            summary.LimitedAccount = false;

                            webrequest.Enqueue($"https://steamcommunity.com/profiles/{steamId64}/?xml=1", "",
                                (httpCodeCommunity, responseCommunity) =>
                                {
                                    if (httpCodeCommunity == (int)StatusCode.Success)
                                    {
                                        // XML parser is disabled in uMod, so have to use contains

                                        // Has not set up their profile?
                                        if (responseCommunity.Contains(
                                                "This user has not yet set up their Steam Community profile."))
                                            summary.NoProfile = true;

                                        if (responseCommunity.Contains("<isLimitedAccount>1</isLimitedAccount>"))
                                            summary.LimitedAccount = true;

                                        callback(httpCode, summary);
                                    }
                                    else
                                    {
                                        ApiError(steamId64, "GetSteamPlayerSummaries - community xml",
                                            httpCodeCommunity);
                                        // We will send into the callback success, as the normal GetSteamPlayerSummaries worked in this case
                                        // So it's information can be respected
                                        callback((int)StatusCode.Success, summary);
                                    }
                                }, this, Core.Libraries.RequestMethod.GET, null, webTimeout);
                        }
                    }
                    else
                    {
                        callback(httpCode, null);
                    }
                });
        }

        /// <summary>
        /// Utility function for printing a log when a HTTP API Error was encountered
        /// </summary>
        /// <param name="steamId">steamId64 for which user the request was</param>
        /// <param name="function">function name in the plugin</param>
        /// <param name="statusCode">see <see cref="StatusCode"></see></param>
        private void ApiError(string steamId, string function, int statusCode)
        {
            var detailedError = $" SteamID: {steamId} - Function: {function} - ErrorCode: {(StatusCode)statusCode}";
            LogWarning(Lang("ErrorHttp"), detailedError);
        }

        /// <summary>
        /// Get all Steam Badges
        /// </summary>
        /// <param name="steamId64">steamId64 of the user</param>
        /// <param name="callback">Callback with the statusCode <see cref="StatusCode"></see> and the result as JSON</param>
        private void GetSteamBadges(string steamId64, Action<int,string> callback)
        {
            SteamWebRequest(SteamRequestType.IPlayerService, "GetBadges/v1", steamId64, callback);
        }

        /// <summary>
        /// Fetched the level of a given badgeid from a JSON Web API result
        /// </summary>
        /// <param name="response"></param>
        /// <param name="badgeId">ID of the badge, see <see cref="Badge"></see></param>
        /// <returns>level of the badge, or 0 if badge not existing</returns>
        private int ParseBadgeLevel(string response, Badge badgeId)
        {
            var steamResponse = JsonConvert.DeserializeObject<SteamBadgeApiResponse>(response);
            return steamResponse.response.badges.FirstOrDefault(x => x.badgeid == (int)badgeId)?.level ?? 0;
        }

        /// <summary>
        /// Get the information about the bans the player has
        /// </summary>
        /// <param name="steamId64">steamId64 of the user</param>
        /// <param name="callback">Callback with the statusCode <see cref="StatusCode"></see> and the result as <see cref="PlayerBans"></see></param>
        /// <remarks>
        /// Getting the user bans is even possible, if the profile is private
        /// </remarks>
        private void GetPlayerBans(string steamId64, Action<int, PlayerBans> callback)
        {
            SteamWebRequest(SteamRequestType.ISteamUser, "GetPlayerBans/v1", steamId64,
                (httpCode, response) =>
                {
                    if (httpCode == (int)StatusCode.Success)
                    {
                        var steamResponse = JsonConvert.DeserializeObject<SteamBanApiResponse>(response);
                        if (steamResponse.players.Count() != 1)
                        {
                            callback((int)StatusCode.PlayerNotFound, null);
                            return;
                        }

                        var playerInfo = steamResponse.players[0];

                        var bans = new PlayerBans
                        {
                            CommunityBan = playerInfo.CommunityBanned,
                            VacBan = playerInfo.VACBanned,
                            VacBanCount = playerInfo.NumberOfVACBans,
                            LastBan = playerInfo.DaysSinceLastBan,
                            GameBanCount = playerInfo.NumberOfGameBans,
                            // can be none, probation or banned
                            EconomyBan = playerInfo.EconomyBan != "none"
                        };

                        callback(httpCode, bans);
                    }
                    else
                    {
                        callback(httpCode, null);
                    }
                });
        }

    }
}