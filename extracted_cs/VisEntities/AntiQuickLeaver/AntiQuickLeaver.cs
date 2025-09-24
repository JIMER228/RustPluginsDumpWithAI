/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Anti Quick Leaver", "VisEntities", "1.0.0")]
    [Description("Flags players who repeatedly log in for only 1-5 minutes.")]
    public class AntiQuickLeaver : RustPlugin
    {
        #region 3rd Party Dependencies

        [PluginReference]
        private readonly Plugin PlaytimeTracker;

        #endregion 3rd Party Dependencies

        #region Fields

        private static AntiQuickLeaver _plugin;
        private static Configuration _config;
        private readonly Dictionary<ulong, List<SessionRecord>> _recentShortSessionsByPlayer = new Dictionary<ulong, List<SessionRecord>>();
        private readonly HashSet<ulong> _playersAlreadyFlagged = new HashSet<ulong>();

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Minimum Session Duration Seconds")]
            public int MinimumSessionDurationSeconds { get; set; }

            [JsonProperty("Maximum Session Duration Seconds")]
            public int MaximumSessionDurationSeconds { get; set; }

            [JsonProperty("Required Short Sessions To Be Flagged")]
            public int RequiredShortSessionsToBeFlagged { get; set; }

            [JsonProperty("Hours To Look Back For Short Sessions")]
            public int HoursToLookBackForShortSessions { get; set; }

            [JsonProperty("Discord Webhook Url")]
            public string DiscordWebhookUrl { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                MinimumSessionDurationSeconds = 60,
                MaximumSessionDurationSeconds = 300,
                RequiredShortSessionsToBeFlagged = 3,
                HoursToLookBackForShortSessions = 24,
                DiscordWebhookUrl = ""
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private void OnServerInitialized(bool isStartup)
        {
            if (!CheckDependencies())
            {
                timer.Once(1f, () => Interface.Oxide.UnloadPlugin(Name));
                return;
            }
        }

        // This hook is exposed by Playtime Tracker plugin (https://game4freak.io/plugins/playtime-tracker.261)
        private void OnPlayerSessionEnded(BasePlayer player, double sessionActiveSeconds, double logoutEpoch)
        {
            if (player == null)
                return;

            if (PermissionUtil.HasPermission(player, PermissionUtil.IGNORE))
                return;
 
            List<SessionRecord> sessionList;
            if (!_recentShortSessionsByPlayer.TryGetValue(player.userID, out sessionList))
            {
                sessionList = new List<SessionRecord>();
                _recentShortSessionsByPlayer[player.userID] = sessionList;
            }

            SessionRecord newRecord = new SessionRecord();
            newRecord.Duration = sessionActiveSeconds;
            newRecord.LogoutEpoch = logoutEpoch;
            sessionList.Add(newRecord);

            double cutoffEpoch = logoutEpoch - (_config.HoursToLookBackForShortSessions * 3600);
            for (int i = sessionList.Count - 1; i >= 0; i--)
            {
                if (sessionList[i].LogoutEpoch < cutoffEpoch)
                {
                    sessionList.RemoveAt(i);
                }
            }

            int shortSessionCount = 0;
            foreach (SessionRecord record in sessionList)
            {
                if (record.Duration >= _config.MinimumSessionDurationSeconds && record.Duration <= _config.MaximumSessionDurationSeconds)
                {
                    shortSessionCount++;
                }
            }

            if (shortSessionCount >= _config.RequiredShortSessionsToBeFlagged && !_playersAlreadyFlagged.Contains(player.userID))
            {
                _playersAlreadyFlagged.Add(player.userID);
                AnnounceFlag(player, shortSessionCount);
            }
        }

        #endregion Oxide Hooks

        #region Player Session Storage

        private class SessionRecord
        {
            public double Duration;
            public double LogoutEpoch;
        }

        #endregion Player Session Storage

        #region Alerts

        private void AnnounceFlag(BasePlayer player, int shortSessionCount)
        {
            string message = player.displayName + " (" + player.UserIDString + ") flagged for "
                + shortSessionCount + " short sessions (each "
                + (_config.MaximumSessionDurationSeconds / 60) + " minutes or less) within the last "
                + _config.HoursToLookBackForShortSessions + " hours.";

            Puts(message);

            foreach (BasePlayer admin in BasePlayer.activePlayerList)
            {
                if (admin != null && admin.IsAdmin)
                {
                    SendReply(admin, message);
                }
            }

            if (string.IsNullOrWhiteSpace(_config.DiscordWebhookUrl))
                return;

            string payload = JsonConvert.SerializeObject(new { content = message });

            webrequest.Enqueue(
                _config.DiscordWebhookUrl,
                payload,
                (code, response) =>
                {
                    if (code == 204 || (code >= 200 && code < 300))
                    {
                        Puts("Alert sent to Discord.");
                    }
                    else
                    {
                        Puts($"Discord error {code}: {response}");
                    }
                },
                this,
                RequestMethod.POST,
                new Dictionary<string, string> { { "Content-Type", "application/json" } }
            );
        }

        #endregion Alerts

        #region Helper Functions

        private bool CheckDependencies()
        {
            if (!PluginLoaded(PlaytimeTracker))
            {
                Puts("Playtime Tracker is not loaded. Download it from https://game4freak.io.");
                return false;
            }
            return true;
        }

        private static bool PluginLoaded(Plugin plugin)
        {
            if (plugin != null && plugin.IsLoaded)
                return true;
            else
                return false;
        }

        #endregion Helper Functions

        #region Permissions

        private static class PermissionUtil
        {
            public const string IGNORE = "antiquickleaver.ignore";
            private static readonly List<string> _permissions = new List<string>
            {
                IGNORE,
            };

            public static string ConstructPermission(string suffix, bool addToList = true)
            {
                string perm = string.Join(".", nameof(AntiQuickLeaver), suffix).ToLower();

                if (addToList && !_permissions.Contains(perm))
                    _permissions.Add(perm);

                return perm;
            }

            public static void AddPermission(string permission)
            {
                if (!_permissions.Contains(permission))
                    _permissions.Add(permission);
            }

            public static void RegisterPermissions()
            {
                foreach (string perm in _permissions)
                    _plugin.permission.RegisterPermission(perm, _plugin);
            }

            public static bool HasPermission(BasePlayer player, string permission)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permission);
            }
        }

        #endregion Permissions
    }
}