#region Using Statements

using System;

#endregion

namespace Oxide.Plugins
{
    //Define:FileOrder=1
    [Info("Steam Checks", "Shady14u", "5.0.7")]
    [Description("Kick players depending on information on their Steam profile")]
    public partial class SteamChecks : CovalencePlugin
    {
        #region Methods (Private)

        /// <summary>
        /// Checks a steamId, if it would be allowed into the server
        /// </summary>
        /// <param name="steamId">steamId64 of the user</param>
        /// <param name="callback">
        /// First parameter is true, when the user is allowed, otherwise false
        /// Second parameter is the reason why he is not allowed, filled out when first is false
        /// </param>
        /// <remarks>
        /// Asynchronously
        /// Runs through all checks one-by-one
        /// 1. Bans
        /// 2. Player Summaries (Private profile, Creation time)
        /// 3. Player Level
        /// Via <see cref="CheckPlayerGameTime"></see>
        /// 4. Game Hours and Count
        /// 5. Game badges, to get amount of games if user has hidden Game Hours
        /// </remarks>
        private void CheckPlayer(string steamId, Action<bool, string> callback)
        {
            // Check Bans first, as they are also visible on private profiles
            GetPlayerBans(steamId, (banStatusCode, banResponse) =>
            {
                if (banStatusCode != (int)StatusCode.Success)
                {
                    ApiError(steamId, "GetPlayerBans", banStatusCode);
                    return;
                }

                if (banResponse.CommunityBan && kickCommunityBan)
                {
                    callback(false, Lang("KickCommunityBan", steamId));
                    return;
                }

                if (banResponse.EconomyBan && kickTradeBan)
                {
                    callback(false, Lang("KickTradeBan", steamId));
                    return;
                }

                if (banResponse.GameBanCount > maxGameBans && maxGameBans > -1)
                {
                    callback(false, Lang("KickGameBan", steamId));
                    return;
                }

                if (banResponse.VacBanCount > maxVACBans && maxVACBans > -1)
                {
                    callback(false, Lang("KickVacBan", steamId));
                    return;
                }

                if (banResponse.LastBan > 0 && banResponse.LastBan < minDaysSinceLastBan && minDaysSinceLastBan > 0)
                {
                    callback(false, Lang("KickVacBan", steamId));
                    return;
                }

                //get Player summaries - we have to check if the profile is public
                GetSteamPlayerSummaries(steamId, (statusCode, sumResult) =>
                {
                    if (statusCode != (int)StatusCode.Success)
                    {
                        ApiError(steamId, "GetSteamPlayerSummaries", statusCode);
                        return;
                    }

                    if (sumResult.NoProfile && kickNoProfile)
                    {
                        callback(false, Lang("KickNoProfile", steamId));
                        return;
                    }

                    // Is profile not public?
                    if (sumResult.Visibility != PlayerSummary.VisibilityType.Public)
                    {
                        if (kickPrivateProfile)
                        {
                            callback(false, Lang("KickPrivateProfile", steamId));
                            return;
                        }
                        else
                        {
                            // If it is not public, we can cancel checks here and allow the player in
                            callback(true, null);
                            return;
                        }
                    }

                    // Check how old the account is
                    if (maxAccountCreationTime > 0 && sumResult.Timecreated > maxAccountCreationTime)
                    {
                        callback(false, Lang("KickMaxAccountCreationTime", steamId));
                        return;
                    }

                    // Check Steam Level
                    if (minSteamLevel > 0)
                    {
                        GetSteamLevel(steamId, (steamLevelStatusCode, steamLevelResult) =>
                        {
                            if (steamLevelStatusCode != (int)StatusCode.Success)
                            {
                                ApiError(steamId, "GetSteamLevel", statusCode);
                                return;
                            }

                            if (minSteamLevel > steamLevelResult)
                            {
                                callback(false, Lang("KickMinSteamLevel", steamId));
                            }
                            else
                            {
                                // Check game time, and amount of games
                                if (minGameCount > 1 || minRustHoursPlayed > 0 || maxRustHoursPlayed > 0 ||
                                    minOtherGamesPlayed > 0 || minAllGamesHoursPlayed > 0)
                                    CheckPlayerGameTime(steamId, callback);
                                else // Player now already passed all checks
                                    callback(true, null);
                            }
                        });
                    }
                    // Else, if level check not done, Check game time, and amount of games
                    else if (minGameCount > 1 || minRustHoursPlayed > 0 || maxRustHoursPlayed > 0 ||
                             minOtherGamesPlayed > 0 || minAllGamesHoursPlayed > 0)
                    {
                        CheckPlayerGameTime(steamId, callback);
                    }
                    else // Player now already passed all checks
                    {
                        callback(true, null);
                    }
                });
            });
        }

        /// <summary>
        /// Checks a steamId, and if it would be allowed into the server
        /// Called by <see cref="CheckPlayer"></see>
        /// </summary>
        /// <param name="steamId">steamId64 of the user</param>
        /// <param name="callback">
        /// First parameter is true, when the user is allowed, otherwise false
        /// Second parameter is the reason why he is not allowed, filled out when first is false
        /// </param>
        /// <remarks>
        /// Regards those specific parts:
        /// - Game Hours and Count
        /// - Game badges, to get amount of games if user has hidden Game Hours
        /// </remarks>
        void CheckPlayerGameTime(string steamId, Action<bool, string> callback)
        {
            GetPlaytimeInformation(steamId, (gameTimeStatusCode, gameTimeResult) =>
            {
                // Players can additionally hide their play time, check
                var gameTimeHidden = false;
                if (gameTimeStatusCode == (int)StatusCode.GameInfoHidden)
                {
                    gameTimeHidden = true;
                }
                // Check if the request failed in general
                else if (gameTimeStatusCode != (int)StatusCode.Success)
                {
                    ApiError(steamId, "GetPlaytimeInformation", gameTimeStatusCode);
                    return;
                }

                // In rare cases, the SteamAPI returns all games, however with the game time set to 0. (when the user has this info hidden)
                if (gameTimeResult != null && (gameTimeResult.PlaytimeRust == 0 || gameTimeResult.PlaytimeAll == 0))
                    gameTimeHidden = true;

                // If the server owner really wants a hour check, we will kick
                if (gameTimeHidden && forceHoursPlayedKick)
                {
                    if (minRustHoursPlayed > 0 || maxRustHoursPlayed > 0 ||
                        minOtherGamesPlayed > 0 || minAllGamesHoursPlayed > 0)
                    {
                        callback(false, Lang("KickHoursPrivate", steamId));
                        return;
                    }
                }
                // Check the times and game count now, when not hidden
                else if (!gameTimeHidden && gameTimeResult != null)
                {
                    if (minRustHoursPlayed > 0 && gameTimeResult.PlaytimeRust < minRustHoursPlayed)
                    {
                        callback(false, Lang("KickMinRustHoursPlayed", steamId));
                        return;
                    }

                    if (maxRustHoursPlayed > 0 && gameTimeResult.PlaytimeRust > maxRustHoursPlayed)
                    {
                        callback(false, Lang("KickMaxRustHoursPlayed", steamId));
                        return;
                    }

                    if (minAllGamesHoursPlayed > 0 && gameTimeResult.PlaytimeAll < minAllGamesHoursPlayed)
                    {
                        callback(false, Lang("KickMinSteamHoursPlayed", steamId));
                        return;
                    }

                    if (minOtherGamesPlayed > 0 &&
                        (gameTimeResult.PlaytimeAll - gameTimeResult.PlaytimeRust) < minOtherGamesPlayed &&
                        gameTimeResult.GamesCount >
                        1) // it makes only sense to check, if there are other games in the result set
                    {
                        callback(false, Lang("KickMinNonRustPlayed", steamId));
                        return;
                    }

                    if (minGameCount > 1 && gameTimeResult.GamesCount < minGameCount)
                    {
                        callback(false, Lang("KickGameCount", steamId));
                        return;
                    }
                }

                // If the server owner wants to check minimum amount of games, but the user has hidden game time
                // We will get the count over an additional API request via badges
                if (gameTimeHidden && minGameCount > 1)
                {
                    GetSteamBadges(steamId, (badgeStatusCode, badgeResult) =>
                    {
                        // Check if the request failed in general
                        if (badgeStatusCode != (int)StatusCode.Success)
                        {
                            ApiError(steamId, "GetPlaytimeInformation", gameTimeStatusCode);
                            return;
                        }

                        var gamesOwned = ParseBadgeLevel(badgeResult, Badge.GamesOwned);
                        if (gamesOwned < minGameCount)
                        {
                            callback(false, Lang("KickGameCount", steamId));
                        }
                        else
                        {
                            // Checks passed
                            callback(true, null);
                        }
                    });
                }
                else
                {
                    // Checks passed
                    callback(true, null);
                }
            });
        }

        #endregion
    }
}