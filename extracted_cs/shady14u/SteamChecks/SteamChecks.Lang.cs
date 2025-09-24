using System.Collections.Generic;

namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        #region Methods (Private)

        /// <summary>
        /// Abbreviation for printing Language-Strings
        /// </summary>
        /// <param name="key">Language Key</param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private string Lang(string key, string userId = null) => lang.GetMessage(key, this, userId);

        #endregion

        /// <summary>
        /// Load default language messages, for every plugin start
        /// </summary>
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Console"] = "Kicking {0}... ({1})",

                ["ErrorAPIConfig"] = "The API key you supplied in the config is empty.. register one here https://steamcommunity.com/dev/apikey",
                ["WarningPrivateProfileHours"] = "**** WARNING: Private profile-kick is off. However a option to kick for minimum amount of hours is on.",
                ["WarningPrivateProfileGames"] = "**** WARNING: Private profile-kick is off. However the option to kick for minimum amount of games is on (MinGameCount).",
                ["WarningPrivateProfileCreationTime"] = "**** WARNING: Private profile-kick is off. However the option to kick for account age is on (MinAccountCreationTime).",
                ["WarningPrivateProfileSteamLevel"] = "**** WARNING: Private profile-kick is off. However the option to kick for steam level is on (MinSteamLevel).",

                ["ErrorHttp"] = "Error while contacting the SteamAPI. Error: {0}.",
                ["ErrorPrivateProfile"] = "This player has a private profile, therefore SteamChecks cannot check their hours.",

                ["KickCommunityBan"] = "You have a Steam Community ban on record.",
                ["KickVacBan"] = "You have too many VAC bans on record.",
                ["KickGameBan"] = "You have too many Game bans on record.",
                ["KickTradeBan"] = "You have a Steam Trade ban on record.",
                ["KickPrivateProfile"] = "Your Steam profile state is set to private.",
                ["KickLimitedAccount"] = "Your Steam account is limited.",
                ["KickNoProfile"] = "Set up your Steam community profile first.",
                ["KickMinSteamLevel"] = "Your Steam level is not high enough.",
                ["KickMinRustHoursPlayed"] = "You haven't played enough hours.",
                ["KickMaxRustHoursPlayed"] = "You have played too much Rust.",
                ["KickMinSteamHoursPlayed"] = "You didn't play enough Steam games (hours).",
                ["KickMinNonRustPlayed"] = "You didn't play enough Steam games besides Rust (hours).",
                ["KickHoursPrivate"] = "Your Steam profile is public, but the hours you played is hidden'.",
                ["KickGameCount"] = "You don't have enough Steam games.",
                ["KickMaxAccountCreationTime"] = "Your Steam account is too new.",

                ["KickGeneric"] = "Your Steam account fails our test.",
                ["DiscordMessage"] = "User failed Steam Checks"
            }, this);
        }
    }
}