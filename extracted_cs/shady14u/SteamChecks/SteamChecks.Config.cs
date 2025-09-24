#region Using Statements

using System.Collections.Generic;
using System.Text.RegularExpressions;

#endregion

namespace Oxide.Plugins
{
    //Define:FileOrder=2
    public partial class SteamChecks
    {
        /// <summary>
        /// Url to the Steam Web API
        /// </summary>
        private const string apiURL = "https://api.steampowered.com";
        private readonly Regex _steamAvatarRegex =
            new Regex(@"(?<=<avatarMedium>[\w\W]+)https://.+\.jpg(?=[\w\W]+<\/avatarMedium>)", RegexOptions.Compiled);
        /// <summary>
        /// Oxide permission for a whitelist
        /// </summary>
        private const string skipPermission = "steamchecks.skip";

        /// <summary>
        /// Timeout for a web request
        /// </summary>
        private const int webTimeout = 2000;

        /// <summary>
        /// This message will be appended to all Kick-messages
        /// </summary>
        private string additionalKickMessage;

        /// <summary>
        /// API Key to use for the Web API
        /// </summary>
        /// <remarks>
        /// https://steamcommunity.com/dev/apikey
        /// </remarks>
        private string apiKey;

        /// <summary>
        /// AppID of the game, where the plugin is loaded
        /// </summary>
        private uint appId;

        /// <summary>
        /// Broadcast kick via chat?
        /// </summary>
        private bool broadcastKick;

        /// <summary>
        /// Cache players, which joined and failed the checks
        /// </summary>
        private bool cacheDeniedPlayers;

        /// <summary>
        /// Cache players, which joined and successfully completed the checks
        /// </summary>
        private bool cachePassedPlayers;

        private List<string> discordRolesToMention = new List<string>();

        private string discordWebHookUrl;

        /// <summary>
        /// Set of steamIds, which failed the steam check test on joining
        /// </summary>
        /// <remarks>
        /// Resets after a plugin reload
        /// </remarks>
        private HashSet<string> failedList;

        /// <summary>
        /// Kick user, when his hours are hidden
        /// </summary>
        /// <remarks>
        /// A lot of steam users have their hours hidden
        /// </remarks>
        private bool forceHoursPlayedKick;

        /// <summary>
        /// Kick when the user has a Steam Community ban
        /// </summary>
        private bool kickCommunityBan;

        /// <summary>
        /// Kick when the user has not set up his steam profile yet
        /// </summary>
        private bool kickNoProfile;

        /// <summary>
        /// Kick when the user has a private profile
        /// </summary>
        /// <remarks>
        /// Most checks depend on a public profile
        /// </remarks>
        private bool kickPrivateProfile;

        /// <summary>
        /// Kick when the user has a Steam Trade ban
        /// </summary>
        private bool kickTradeBan;

        /// <summary>
        /// Just log instead of actually kicking users?
        /// </summary>
        private bool logInsteadofKick;

        /// <summary>
        /// Unix-Time, if the account created by the user is newer/higher than it
        /// he won't be allowed
        /// </summary>
        private long maxAccountCreationTime;

        /// <summary>
        /// Maximum amount of game bans, the user is allowed to have
        /// </summary>
        private int maxGameBans;

        /// <summary>
        /// Maximum amount of rust played
        /// </summary>
        private int maxRustHoursPlayed;

        /// <summary>
        /// Maximum amount of VAC bans, the user is allowed to have
        /// </summary>
        private int maxVACBans;

        /// <summary>
        /// Minimum amount of Steam games played - including Rust
        /// </summary>
        private int minAllGamesHoursPlayed;

        /// <summary>
        /// How old the last VAC ban should minimally
        /// </summary>
        private int minDaysSinceLastBan;

        /// <summary>
        /// Minimum amount of Steam games
        /// </summary>
        private int minGameCount;

        /// <summary>
        /// Minimum amount of Steam games played - except Rust
        /// </summary>
        private int minOtherGamesPlayed;

        /// <summary>
        /// Minimum amount of rust played
        /// </summary>
        private int minRustHoursPlayed;

        /// <summary>
        /// The minimum steam level, the user must have
        /// </summary>
        private int minSteamLevel;

        /// <summary>
        /// Set of steamIds, which already passed the steam check test on joining
        /// </summary>
        /// <remarks>
        /// Resets after a plugin reload
        /// </remarks>
        private HashSet<string> passedList;

        #region Methods (Protected)

        /// <summary>
        /// Loads default configuration options
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            Config["ApiKey"] = "";
            Config["BroadcastKick"] = false;
            Config["LogInsteadofKick"] = false;
            Config["AdditionalKickMessage"] = "";
            Config["CachePassedPlayers"] = true;
            Config["CacheDeniedPlayers"] = false;
            Config["Kicking"] = new Dictionary<string, bool>
            {
                ["CommunityBan"] = true,
                ["TradeBan"] = true,
                ["PrivateProfile"] = true,
                ["LimitedAccount"] = true,
                ["NoProfile"] = true,
                ["FamilyShare"] = false,
                ["ForceHoursPlayedKick"] = false,
            };
            Config["Thresholds"] = new Dictionary<string, long>
            {
                ["MaxVACBans"] = 1,
                ["MinDaysSinceLastBan"] = -1,
                ["MaxGameBans"] = 1,
                ["MinSteamLevel"] = 2,
                ["MaxAccountCreationTime"] = -1,
                ["MinGameCount"] = 3,
                ["MinRustHoursPlayed"] = -1,
                ["MaxRustHoursPlayed"] = -1,
                ["MinOtherGamesPlayed"] = 2,
                ["MinAllGamesHoursPlayed"] = -1
            };
            Config["DiscordRolesToMention"] = new List<string>();
            Config["discordWebHookUrl"] = "";
        }

        #endregion

        #region Methods (Private)

        /// <summary>
        /// Initializes config options, for every plugin start
        /// </summary>
        private void InitializeConfig()
        {
            apiKey = Config.Get<string>("ApiKey");
            broadcastKick = Config.Get<bool>("BroadcastKick");
            logInsteadofKick = Config.Get<bool>("LogInsteadofKick");
            additionalKickMessage = Config.Get<string>("AdditionalKickMessage");
            cachePassedPlayers = Config.Get<bool>("CachePassedPlayers");
            cacheDeniedPlayers = Config.Get<bool>("CacheDeniedPlayers");

            kickCommunityBan = Config.Get<bool>("Kicking", "CommunityBan");
            kickTradeBan = Config.Get<bool>("Kicking", "TradeBan");
            kickPrivateProfile = Config.Get<bool>("Kicking", "PrivateProfile");
            kickNoProfile = Config.Get<bool>("Kicking", "NoProfile");
            forceHoursPlayedKick = Config.Get<bool>("Kicking", "ForceHoursPlayedKick");

            maxVACBans = Config.Get<int>("Thresholds", "MaxVACBans");
            minDaysSinceLastBan = Config.Get<int>("Thresholds", "MinDaysSinceLastBan");
            maxGameBans = Config.Get<int>("Thresholds", "MaxGameBans");


            minSteamLevel = Config.Get<int>("Thresholds", "MinSteamLevel");

            minRustHoursPlayed = Config.Get<int>("Thresholds", "MinRustHoursPlayed") * 60;
            maxRustHoursPlayed = Config.Get<int>("Thresholds", "MaxRustHoursPlayed") * 60;
            minOtherGamesPlayed = Config.Get<int>("Thresholds", "MinOtherGamesPlayed") * 60;
            minAllGamesHoursPlayed = Config.Get<int>("Thresholds", "MinAllGamesHoursPlayed") * 60;

            minGameCount = Config.Get<int>("Thresholds", "MinGameCount");
            maxAccountCreationTime = Config.Get<long>("Thresholds", "MaxAccountCreationTime");

            if (!kickPrivateProfile)
            {
                if (minRustHoursPlayed > 0 || maxRustHoursPlayed > 0 || minOtherGamesPlayed > 0 ||
                    minAllGamesHoursPlayed > 0)
                    LogWarning(Lang("WarningPrivateProfileHours"));

                if (minGameCount > 1)
                    LogWarning(Lang("WarningPrivateProfileGames"));

                if (maxAccountCreationTime > 0)
                    LogWarning(Lang("WarningPrivateProfileCreationTime"));

                if (minSteamLevel > 0)
                    LogWarning(Lang("WarningPrivateProfileSteamLevel"));
            }

            discordRolesToMention = Config.Get<List<string>>("DiscordRolesToMention");
            discordWebHookUrl = Config.Get<string>("discordWebHookUrl");
        }

        #endregion
    }
}