using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("rPop", "Ftuoil Xelrash", "1.0.1")]
    [Description("Displays server population statistics and sends performance updates to Discord")]

    public class rPop : RustPlugin
    {
        #region Configuration

        private ConfigData config;
        private PluginData pluginData;
        private DateTime lastPopCommandTime = DateTime.MinValue;
        private Timer performanceTimer;
        private Timer inGameMessageTimer;
        private readonly Dictionary<string, DateTime> lastDiscordMessage = new Dictionary<string, DateTime>();
        private SaveInfo _saveInfo;
        private bool _isOnline = false;  // Server status tracking

        public class ConfigData
        {
            [JsonProperty("Settings")] public PluginSettings Settings = new PluginSettings();
        }

        public class PluginSettings
        {
            [JsonProperty("Enable !pop Command")] public bool EnablePopCommand = true;
            [JsonProperty("Command Cooldown (minutes)")] public float CommandCooldown = 5f;
            [JsonProperty("Show Last Wipe Date")] public bool ShowLastWipeDate = true;
            [JsonProperty("Show Last Blueprint Wipe Date")] public bool ShowLastBlueprintWipeDate = true;
            [JsonProperty("Show Next Wipe Date")] public bool ShowNextWipeDate = true;
            [JsonProperty("Show Network IO")] public bool ShowNetworkIO = true;
            [JsonProperty("Show Protocol")] public bool ShowProtocol = true;
            [JsonProperty("Show Server Status")] public bool ShowServerStatus = true;
            
            [JsonProperty("Show Players Joining")] public bool ShowPlayersJoining = true;
            [JsonProperty("Show Players Sleeping")] public bool ShowPlayersSleeping = true;
            [JsonProperty("Show Admins Online")] public bool ShowAdminsOnline = true;
            [JsonProperty("Hide Zero Values")] public bool HideZeroValues = true;
            
            [JsonProperty("Discord Webhook URL")] public string WebhookURL = "";
            [JsonProperty("Discord Rate Limit (seconds)")] public float DiscordRateLimit = 1f;
            [JsonProperty("Enable Discord Performance Messages")] public bool EnablePerformanceMessages = true;
            [JsonProperty("Performance Message Interval (minutes)")] public float PerformanceMessageInterval = 3f;
            [JsonProperty("Enable In-Game Performance Messages")] public bool EnableInGamePerformanceMessages = true;
            [JsonProperty("In-Game Message Interval (minutes)")] public float InGameMessageInterval = 60f;
            
            [JsonProperty("Use Server Header Image")] public bool UseServerHeaderImage = true;
            [JsonProperty("Fallback Discord Image URL")] public string FallbackImageURL = "https://files.facepunch.com/lewis/1b2911b1/rust-logo.png";
            [JsonProperty("Use Thumbnail Instead of Image")] public bool UseThumbnail = true;
            [JsonProperty("Show Discord Image")] public bool ShowDiscordImage = true;
            [JsonProperty("Discord Bot Name")] public string DiscordBotName = "Live Server Statistics";
            
            [JsonProperty("Show Population Records")] public bool ShowPopulationRecords = true;
            [JsonProperty("Show Total Players Ever")] public bool ShowTotalPlayersEver = true;
            [JsonProperty("Show Average Connection Time")] public bool ShowAverageConnectionTime = true;
            
            [JsonProperty("Enable Instant Population Updates")] public bool EnableInstantPopulationUpdates = true;
            [JsonProperty("Population Update Delay (seconds)")] public float PopulationUpdateDelay = 2f;
        }

        public class PluginData
        {
            [JsonProperty("Today High Population")] public PopulationRecord TodayHigh = new PopulationRecord();
            [JsonProperty("Monthly High Population")] public PopulationRecord MonthlyHigh = new PopulationRecord();
            [JsonProperty("All Time High Population")] public PopulationRecord AllTimeHigh = new PopulationRecord();
            [JsonProperty("Last Reset Date")] public DateTime LastResetDate = DateTime.Today;
            [JsonProperty("Last Monthly Reset")] public DateTime LastMonthlyReset = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            [JsonProperty("Discord Status Message ID")] public string StatusMessageId = null;
        }

        public class PopulationRecord
        {
            [JsonProperty("Count")] public int Count = 0;
            [JsonProperty("Date")] public DateTime Date = DateTime.MinValue;
        }

        protected override void LoadDefaultConfig()
        {
            config = new ConfigData();
            SaveConfig();
            Puts("Default configuration created.");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<ConfigData>();
                if (config == null) 
                {
                    LoadDefaultConfig();
                    return;
                }
                ValidateConfiguration();
            }
            catch (Exception ex)
            {
                PrintError($"Error loading configuration: {ex.Message}");
                LoadDefaultConfig();
            }
        }

        private void ValidateConfiguration()
        {
            bool needsSave = false;

            if (config.Settings.PerformanceMessageInterval < 1f)
            {
                Puts($"Performance message interval ({config.Settings.PerformanceMessageInterval}) is too low, setting to minimum of 1 minute.");
                config.Settings.PerformanceMessageInterval = 1f;
                needsSave = true;
            }

            if (config.Settings.DiscordRateLimit < 0.1f)
            {
                Puts($"Discord rate limit ({config.Settings.DiscordRateLimit}) is too low, setting to minimum of 0.1 seconds.");
                config.Settings.DiscordRateLimit = 0.1f;
                needsSave = true;
            }

            if (config.Settings.PopulationUpdateDelay < 1f)
            {
                Puts($"Population update delay ({config.Settings.PopulationUpdateDelay}) is too low, setting to minimum of 1 second.");
                config.Settings.PopulationUpdateDelay = 1f;
                needsSave = true;
            }

            if (!ConfigHasAllProperties())
            {
                Puts("Adding missing configuration properties...");
                needsSave = true;
            }

            if (needsSave)
            {
                SaveConfig();
                Puts("Configuration updated with missing properties (existing settings preserved).");
            }
        }

        private bool ConfigHasAllProperties()
        {
            try
            {
                var configText = Config.ReadObject<Dictionary<string, object>>();
                var settings = configText.GetValueOrDefault("Settings") as Dictionary<string, object>;
                
                if (settings == null) return false;

                bool hasAll = settings.ContainsKey("Discord Webhook URL") &&
                       settings.ContainsKey("Discord Rate Limit (seconds)") &&
                       settings.ContainsKey("Enable Discord Performance Messages") &&
                       settings.ContainsKey("Performance Message Interval (minutes)") &&
                       settings.ContainsKey("Enable In-Game Performance Messages") &&
                       settings.ContainsKey("In-Game Message Interval (minutes)") &&
                       settings.ContainsKey("Use Server Header Image") &&
                       settings.ContainsKey("Fallback Discord Image URL") &&
                       settings.ContainsKey("Use Thumbnail Instead of Image") &&
                       settings.ContainsKey("Show Discord Image") &&
                       settings.ContainsKey("Discord Bot Name") &&
                       settings.ContainsKey("Show Players Joining") &&
                       settings.ContainsKey("Show Players Sleeping") &&
                       settings.ContainsKey("Show Admins Online") &&
                       settings.ContainsKey("Hide Zero Values") &&
                       settings.ContainsKey("Show Population Records") &&
                       settings.ContainsKey("Show Total Players Ever") &&
                       settings.ContainsKey("Show Average Connection Time") &&
                       settings.ContainsKey("Show Last Blueprint Wipe Date") &&
                       settings.ContainsKey("Show Next Wipe Date") &&
                       settings.ContainsKey("Show Network IO") &&
                       settings.ContainsKey("Show Protocol") &&
                       settings.ContainsKey("Show Server Status") &&
                       settings.ContainsKey("Enable Instant Population Updates") &&
                       settings.ContainsKey("Population Update Delay (seconds)");

                if (!hasAll)
                {
                    if (!settings.ContainsKey("Discord Webhook URL"))
                        config.Settings.WebhookURL = "";
                    if (!settings.ContainsKey("Discord Rate Limit (seconds)"))
                        config.Settings.DiscordRateLimit = 1f;
                    if (!settings.ContainsKey("Enable Discord Performance Messages"))
                        config.Settings.EnablePerformanceMessages = true;
                    if (!settings.ContainsKey("Performance Message Interval (minutes)"))
                        config.Settings.PerformanceMessageInterval = 3f;
                    if (!settings.ContainsKey("Enable In-Game Performance Messages"))
                        config.Settings.EnableInGamePerformanceMessages = true;
                    if (!settings.ContainsKey("In-Game Message Interval (minutes)"))
                        config.Settings.InGameMessageInterval = 60f;
                    if (!settings.ContainsKey("Use Server Header Image"))
                        config.Settings.UseServerHeaderImage = true;
                    if (!settings.ContainsKey("Fallback Discord Image URL"))
                        config.Settings.FallbackImageURL = "https://files.facepunch.com/lewis/1b2911b1/rust-logo.png";
                    if (!settings.ContainsKey("Use Thumbnail Instead of Image"))
                        config.Settings.UseThumbnail = true;
                    if (!settings.ContainsKey("Show Discord Image"))
                        config.Settings.ShowDiscordImage = true;
                    if (!settings.ContainsKey("Discord Bot Name"))
                        config.Settings.DiscordBotName = "Live Server Statistics";
                    if (!settings.ContainsKey("Show Players Joining"))
                        config.Settings.ShowPlayersJoining = true;
                    if (!settings.ContainsKey("Show Players Sleeping"))
                        config.Settings.ShowPlayersSleeping = true;
                    if (!settings.ContainsKey("Show Admins Online"))
                        config.Settings.ShowAdminsOnline = true;
                    if (!settings.ContainsKey("Hide Zero Values"))
                        config.Settings.HideZeroValues = true;
                    if (!settings.ContainsKey("Show Population Records"))
                        config.Settings.ShowPopulationRecords = true;
                    if (!settings.ContainsKey("Show Total Players Ever"))
                        config.Settings.ShowTotalPlayersEver = true;
                    if (!settings.ContainsKey("Show Average Connection Time"))
                        config.Settings.ShowAverageConnectionTime = true;
                    if (!settings.ContainsKey("Show Last Blueprint Wipe Date"))
                        config.Settings.ShowLastBlueprintWipeDate = true;
                    if (!settings.ContainsKey("Show Next Wipe Date"))
                        config.Settings.ShowNextWipeDate = true;
                    if (!settings.ContainsKey("Show Network IO"))
                        config.Settings.ShowNetworkIO = true;
                    if (!settings.ContainsKey("Show Protocol"))
                        config.Settings.ShowProtocol = true;
                    if (!settings.ContainsKey("Show Server Status"))
                        config.Settings.ShowServerStatus = true;
                    if (!settings.ContainsKey("Enable Instant Population Updates"))
                        config.Settings.EnableInstantPopulationUpdates = true;
                    if (!settings.ContainsKey("Population Update Delay (seconds)"))
                        config.Settings.PopulationUpdateDelay = 2f;
                }

                return hasAll;
            }
            catch
            {
                return false;
            }
        }

        protected override void SaveConfig()
        {
            try
            {
                Config.WriteObject(config, true);
            }
            catch (Exception ex)
            {
                PrintError($"Error saving configuration: {ex.Message}");
            }
        }

        #endregion

        #region Data Management

        private void LoadData()
        {
            try
            {
                pluginData = Interface.Oxide.DataFileSystem.ReadObject<PluginData>("rPop");
                if (pluginData == null)
                {
                    pluginData = new PluginData();
                    SaveData();
                }
                
                // Check for daily reset
                if (pluginData.LastResetDate.Date < DateTime.Today)
                {
                    pluginData.TodayHigh = new PopulationRecord();
                    pluginData.LastResetDate = DateTime.Today;
                    SaveData();
                    Puts("Daily population stats reset for new day.");
                }

                // Check for monthly reset
                DateTime currentMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                if (pluginData.LastMonthlyReset < currentMonthStart)
                {
                    pluginData.MonthlyHigh = new PopulationRecord();
                    pluginData.LastMonthlyReset = currentMonthStart;
                    SaveData();
                    Puts("Monthly population stats reset for new month.");
                }

                Puts("Plugin data loaded successfully.");
            }
            catch (Exception ex)
            {
                PrintError($"Error loading data: {ex.Message}");
                pluginData = new PluginData();
                SaveData();
            }
        }

        private void SaveData()
        {
            try
            {
                Interface.Oxide.DataFileSystem.WriteObject("rPop", pluginData);
            }
            catch (Exception ex)
            {
                PrintError($"Error saving data: {ex.Message}");
            }
        }

        private void UpdatePopulationRecords()
        {
            try
            {
                int currentPopulation = BasePlayer.activePlayerList.Count;
                DateTime now = DateTime.Now;
                
                if (currentPopulation > pluginData.TodayHigh.Count)
                {
                    pluginData.TodayHigh.Count = currentPopulation;
                    pluginData.TodayHigh.Date = now;
                }
                
                if (currentPopulation > pluginData.MonthlyHigh.Count)
                {
                    pluginData.MonthlyHigh.Count = currentPopulation;
                    pluginData.MonthlyHigh.Date = now;
                }
                
                if (currentPopulation > pluginData.AllTimeHigh.Count)
                {
                    pluginData.AllTimeHigh.Count = currentPopulation;
                    pluginData.AllTimeHigh.Date = now;
                }
                
                SaveData();
            }
            catch (Exception ex)
            {
                PrintError($"Error updating population records: {ex.Message}");
            }
        }

        private double GetSessionPlayTime(BasePlayer player)
        {
            if (player?.net?.connection == null) return 0;
            return player.net.connection.GetSecondsConnected();
        }

        private string GetAverageConnectionTime()
        {
            try
            {
                var activePlayers = BasePlayer.activePlayerList;
                if (activePlayers.Count == 0) return "No players online";

                double totalSeconds = 0;
                int validPlayerCount = 0;

                foreach (var player in activePlayers)
                {
                    if (player?.net?.connection == null) continue;

                    double sessionTime = player.net.connection.GetSecondsConnected();
                    if (sessionTime > 0)
                    {
                        totalSeconds += sessionTime;
                        validPlayerCount++;
                    }
                }

                if (validPlayerCount == 0) return "No valid session data";

                double averageSeconds = totalSeconds / validPlayerCount;
                TimeSpan timeSpan = TimeSpan.FromSeconds(averageSeconds);
                
                if (timeSpan.TotalHours >= 1)
                    return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                else if (timeSpan.TotalMinutes >= 1)
                    return $"{timeSpan.Minutes}:{timeSpan.Seconds:D2} min";
                else
                    return $"{timeSpan.Seconds} sec";
            }
            catch (Exception ex)
            {
                PrintError($"Error calculating average connection time: {ex.Message}");
                return "Error calculating";
            }
        }

        private int GetTotalPlayersEver()
        {
            try
            {
                string userdataPath = "userdata";
                
                if (!Directory.Exists(userdataPath))
                {
                    return 0;
                }

                var playerFolders = Directory.GetDirectories(userdataPath);
                int validPlayerCount = 0;

                foreach (string playerFolder in playerFolders)
                {
                    string steamId = Path.GetFileName(playerFolder);
                    if (IsValidSteamID(steamId))
                    {
                        validPlayerCount++;
                    }
                }

                return validPlayerCount;
            }
            catch (Exception ex)
            {
                PrintError($"Error counting total players: {ex.Message}");
                return 0;
            }
        }

        private bool IsValidSteamID(string steamId)
        {
            return !string.IsNullOrEmpty(steamId) && 
                   steamId.Length >= 7 && 
                   steamId.All(char.IsDigit);
        }

        #endregion

        #region Server Status Functions

        private string GetServerStatus()
        {
            return _isOnline ? "✅ Online" : "❌ Offline";
        }

        private string GetServerStatusEmoji()
        {
            return _isOnline ? "✅" : "❌";
        }

        private string GetServerStatusText()
        {
            return _isOnline ? "Online" : "Offline";
        }

        #endregion

        #region Network, Blueprint, and Protocol Functions

        private string GetNetworkIO()
        {
            try
            {
                float networkIn = Network.Net.sv.GetStat(null, Network.BaseNetwork.StatTypeLong.BytesReceived_LastSecond) / 1024f;
                float networkOut = Network.Net.sv.GetStat(null, Network.BaseNetwork.StatTypeLong.BytesSent_LastSecond) / 1024f;
                
                return $"In: {networkIn:0.00} KB/s Out: {networkOut:0.00} KB/s";
            }
            catch (Exception ex)
            {
                PrintError($"Error getting network IO: {ex.Message}");
                return "Network IO: Error";
            }
        }

        private string GetServerProtocol()
        {
            try
            {
                return $"{Rust.Protocol.network}.{Rust.Protocol.save}.{Rust.Protocol.report}";
            }
            catch (Exception ex)
            {
                PrintError($"Error getting server protocol: {ex.Message}");
                return "Unknown";
            }
        }

        private DateTime GetLastBlueprintWipe()
        {
            try
            {
                if (_saveInfo == null)
                {
                    string saveInfoPath = Path.Combine(World.SaveFolderName, $"player.blueprints.{Rust.Protocol.persistance}.db");
                    _saveInfo = SaveInfo.Create(saveInfoPath);
                }
                
                return _saveInfo?.CreationTime ?? DateTime.MinValue;
            }
            catch (Exception ex)
            {
                PrintError($"Error getting last blueprint wipe: {ex.Message}");
                return DateTime.MinValue;
            }
        }

        private string GetFormattedBlueprintWipeDate()
        {
            try
            {
                DateTime blueprintWipe = GetLastBlueprintWipe();
                
                if (blueprintWipe == DateTime.MinValue)
                {
                    return "Unknown";
                }
                
                TimeSpan timeSinceWipe = DateTime.Now - blueprintWipe;
                
                if (timeSinceWipe.TotalDays < 1)
                {
                    return $"{blueprintWipe:MMM dd, yyyy} ({(int)timeSinceWipe.TotalHours}h ago)";
                }
                else if (timeSinceWipe.TotalDays < 7)
                {
                    return $"{blueprintWipe:MMM dd, yyyy} ({(int)timeSinceWipe.TotalDays}d ago)";
                }
                else
                {
                    return $"{blueprintWipe:MMM dd, yyyy} ({(int)timeSinceWipe.TotalDays}d ago)";
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error formatting blueprint wipe date: {ex.Message}");
                return "Unknown";
            }
        }

        #endregion

        #region Next Wipe Date Functions

        private string GetServerTimeZoneAbbreviation()
        {
            try
            {
                TimeZoneInfo tz = TimeZoneInfo.Local;
                
                var timezoneMap = new Dictionary<string, string>
                {
                    {"Central Standard Time", "CST"},
                    {"Central Daylight Time", "CDT"},
                    {"Eastern Standard Time", "EST"},
                    {"Eastern Daylight Time", "EDT"},
                    {"Mountain Standard Time", "MST"},
                    {"Mountain Daylight Time", "MDT"},
                    {"Pacific Standard Time", "PST"},
                    {"Pacific Daylight Time", "PDT"},
                    {"UTC", "UTC"},
                    {"GMT Standard Time", "GMT"}
                };
                
                string currentName = tz.IsDaylightSavingTime(DateTime.Now) ? tz.DaylightName : tz.StandardName;
                
                return timezoneMap.ContainsKey(currentName) ? timezoneMap[currentName] : "Server Time";
            }
            catch
            {
                return "Server Time";
            }
        }

        private DateTime GetFirstThursdayOfMonth(int year, int month)
        {
            DateTime firstOfMonth = new DateTime(year, month, 1);
            
            int daysUntilThursday = ((int)DayOfWeek.Thursday - (int)firstOfMonth.DayOfWeek + 7) % 7;
            
            return firstOfMonth.AddDays(daysUntilThursday);
        }

        private DateTime GetNextWipeDateTime()
        {
            DateTime now = DateTime.Now;
            DateTime firstThursday = GetFirstThursdayOfMonth(now.Year, now.Month);
            DateTime wipeDateTime = new DateTime(firstThursday.Year, firstThursday.Month, firstThursday.Day, 13, 0, 0);
            
            if (now > wipeDateTime)
            {
                DateTime nextMonth = now.AddMonths(1);
                DateTime nextFirstThursday = GetFirstThursdayOfMonth(nextMonth.Year, nextMonth.Month);
                wipeDateTime = new DateTime(nextFirstThursday.Year, nextFirstThursday.Month, nextFirstThursday.Day, 13, 0, 0);
            }
            
            return wipeDateTime;
        }

        private string FormatTimeRemaining(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays}d {timeSpan.Hours}h {timeSpan.Minutes}m";
            else if (timeSpan.TotalHours >= 1)
                return $"{timeSpan.Hours}h {timeSpan.Minutes}m";
            else
                return $"{timeSpan.Minutes}m";
        }

        private string GetFormattedNextWipeDate()
        {
            try
            {
                DateTime nextWipe = GetNextWipeDateTime();
                TimeSpan timeUntil = nextWipe - DateTime.Now;
                string timezone = GetServerTimeZoneAbbreviation();
                
                if (nextWipe.Date == DateTime.Today)
                {
                    if (timeUntil.TotalMinutes <= 0)
                        return $"Today 1:00 PM {timezone} (Wipe in progress!)";
                    else
                        return $"Today 1:00 PM {timezone} (in {FormatTimeRemaining(timeUntil)})";
                }
                else if (nextWipe.Date == DateTime.Today.AddDays(1))
                {
                    return $"Tomorrow 1:00 PM {timezone} (in {FormatTimeRemaining(timeUntil)})";
                }
                else
                {
                    return $"{nextWipe:MMM dd, yyyy} 1:00 PM {timezone} (in {FormatTimeRemaining(timeUntil)})";
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error formatting next wipe date: {ex.Message}");
                return "Unknown";
            }
        }

        #endregion

        #region Population Update Timer Management

        private void ResetPerformanceTimer()
        {
            try
            {
                performanceTimer?.Destroy();
                
                if (config.Settings.EnablePerformanceMessages)
                {
                    performanceTimer = timer.Every(config.Settings.PerformanceMessageInterval * 60f, SendPerformanceMessage);
                    Puts($"Performance timer reset - next update in {config.Settings.PerformanceMessageInterval} minutes");
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error resetting performance timer: {ex.Message}");
            }
        }

        private void TriggerInstantPopulationUpdate()
        {
            try
            {
                if (!config.Settings.EnableInstantPopulationUpdates || 
                    !config.Settings.EnablePerformanceMessages || 
                    string.IsNullOrEmpty(config.Settings.WebhookURL))
                {
                    return;
                }

                timer.Once(config.Settings.PopulationUpdateDelay, () =>
                {
                    SendPerformanceMessage();
                    ResetPerformanceTimer();
                });
                
                Puts($"Instant population update triggered - Discord will update in {config.Settings.PopulationUpdateDelay} seconds");
            }
            catch (Exception ex)
            {
                PrintError($"Error triggering instant population update: {ex.Message}");
            }
        }

        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
            try
            {
                _isOnline = true;
                Puts("Server marked as ONLINE");
                
                LoadData();
                
                try
                {
                    string saveInfoPath = Path.Combine(World.SaveFolderName, $"player.blueprints.{Rust.Protocol.persistance}.db");
                    _saveInfo = SaveInfo.Create(saveInfoPath);
                }
                catch (Exception ex)
                {
                    PrintError($"Error initializing blueprint save info: {ex.Message}");
                }
                
                if (config.Settings.EnablePerformanceMessages)
                {
                    performanceTimer = timer.Every(config.Settings.PerformanceMessageInterval * 60f, SendPerformanceMessage);
                    timer.Once(10f, SendPerformanceMessage);
                }

                if (config.Settings.EnableInGamePerformanceMessages)
                {
                    inGameMessageTimer = timer.Every(config.Settings.InGameMessageInterval * 60f, SendInGamePerformanceMessageOnly);
                }
                
                UpdatePopulationRecords();
            }
            catch (Exception ex)
            {
                PrintError($"Error during initialization: {ex.Message}");
            }
        }

        private void OnServerShutdown()
        {
            try
            {
                _isOnline = false;
                Puts("Server marked as OFFLINE - sending final Discord update");
                
                if (config.Settings.EnablePerformanceMessages && !string.IsNullOrEmpty(config.Settings.WebhookURL))
                {
                    SendPerformanceMessage();
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error during shutdown: {ex.Message}");
            }
        }

        private void Unload()
        {
            performanceTimer?.Destroy();
            inGameMessageTimer?.Destroy();
            SaveData();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            timer.Once(1f, () => 
            {
                UpdatePopulationRecords();
                TriggerInstantPopulationUpdate();
            });
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            timer.Once(2f, () => 
            {
                UpdatePopulationRecords();
                TriggerInstantPopulationUpdate();
            });
        }

        private void OnPlayerChat(BasePlayer player, string message, ConVar.Chat.ChatChannel channel)
        {
            if (player == null || string.IsNullOrEmpty(message)) return;

            if (message.ToLower() == "!pop")
                HandlePopCommand(player);
        }

        #endregion

        #region Pop Command

        private void HandlePopCommand(BasePlayer player)
        {
            if (!config.Settings.EnablePopCommand)
            {
                player.ChatMessage("The !pop command is currently disabled.");
                return;
            }

            var now = DateTime.Now;
            var timeSinceLastUse = now - lastPopCommandTime;

            if (timeSinceLastUse.TotalMinutes < config.Settings.CommandCooldown)
            {
                var remainingTime = TimeSpan.FromMinutes(config.Settings.CommandCooldown) - timeSinceLastUse;
                player.ChatMessage($"Server statistics are on cooldown. Try again in {GetCooldownTime(remainingTime)}.");
                return;
            }

            lastPopCommandTime = now;

            int playerCount = BasePlayer.activePlayerList.Count;
            int sleepingPlayers = BasePlayer.sleepingPlayerList.Count;
            int joiningPlayers = ServerMgr.Instance.connectionQueue.Queued;
            int maxPlayers = ConVar.Server.maxplayers;
            int adminCount = BasePlayer.activePlayerList.Count(p => p.IsAdmin);

            string statsMessage = $"<color=#FFD700><size=14>Population Stats:</size></color>\n" +
                                $"<color=#00FF00>Players Online:</color> <color=#FFFFFF>{playerCount}/{maxPlayers}</color>";

            if (config.Settings.ShowPlayersJoining && (!config.Settings.HideZeroValues || joiningPlayers > 0))
                statsMessage += $"\n<color=#FFFF00>Players Joining:</color> <color=#FFFFFF>{joiningPlayers}</color>";

            if (config.Settings.ShowPlayersSleeping && (!config.Settings.HideZeroValues || sleepingPlayers > 0))
                statsMessage += $"\n<color=#FF0000>Players Sleeping:</color> <color=#FFFFFF>{sleepingPlayers}</color>";

            if (config.Settings.ShowAdminsOnline && (!config.Settings.HideZeroValues || adminCount > 0))
                statsMessage += $"\n<color=#FFB6C1>Admins Online:</color> <color=#FFFFFF>{adminCount}</color>";

            foreach (var onlinePlayer in BasePlayer.activePlayerList)
                onlinePlayer?.ChatMessage(statsMessage);
        }

        #endregion

        #region Discord Performance

        private void SendPerformanceMessage()
        {
            try
            {
                int playerCount = BasePlayer.activePlayerList.Count;
                int sleepingPlayers = BasePlayer.sleepingPlayerList.Count;
                int joiningPlayers = ServerMgr.Instance.connectionQueue.Queued;
                int maxPlayers = ConVar.Server.maxplayers;
                int adminCount = BasePlayer.activePlayerList.Count(p => p.IsAdmin);
                float fps = Performance.current.frameRate;
                long memoryUsed = GC.GetTotalMemory(false) / 1024 / 1024;
                long totalMemory = SystemInfo.systemMemorySize;
                int mapEntities = BaseNetworkable.serverEntities.Count;
                string uptime = GetHumanReadableUptime();
                string lastWipeDate = GetLastWipeDate();
                int totalPlayersEver = GetTotalPlayersEver();
                string averageConnectionTime = GetAverageConnectionTime();
                string networkIO = GetNetworkIO();
                string lastBpWipeDate = GetFormattedBlueprintWipeDate();
                string nextWipeDate = GetFormattedNextWipeDate();

                string message = "";

                if (config.Settings.ShowServerStatus)
                {
                    message += $"{GetServerStatusEmoji()} **Server Status:** `{GetServerStatusText()}`\n\n";
                }

                message += $"`📊 Population Data`\n";
                message += $"🟢 **Players Online:** `{playerCount}/{maxPlayers}`";

                if (config.Settings.ShowPlayersJoining && (!config.Settings.HideZeroValues || joiningPlayers > 0))
                    message += $"\n🟡 **Players In Queue:** `{joiningPlayers}`";

                if (config.Settings.ShowPlayersSleeping && (!config.Settings.HideZeroValues || sleepingPlayers > 0))
                    message += $"\n🔴 **Players Sleeping:** `{sleepingPlayers}`";

                if (config.Settings.ShowAdminsOnline && (!config.Settings.HideZeroValues || adminCount > 0))
                    message += $"\n👑 **Admins Online:** `{adminCount}`";

                if (config.Settings.ShowPopulationRecords)
                {
                    if (pluginData.TodayHigh.Count > 0)
                        message += $"\n📈 **Today's Peak Players:** `{pluginData.TodayHigh.Count}`";
                    
                    if (pluginData.MonthlyHigh.Count > 0)
                        message += $"\n📊 **Monthly Peak Players:** `{pluginData.MonthlyHigh.Count}`";
                    
                    if (pluginData.AllTimeHigh.Count > 0)
                        message += $"\n🏆 **All-Time Peak Players:** `{pluginData.AllTimeHigh.Count}`";
                }

                if (config.Settings.ShowTotalPlayersEver)
                    message += $"\n🏢 **Total Server Players:** `{totalPlayersEver:N0}`";

                if (config.Settings.ShowAverageConnectionTime && playerCount > 0)
                    message += $"\n⏱️ **Average Active Session Time:** `{averageConnectionTime}`";

                message += $"\n\n`🌍 World Data`\n" +
                          $"🕒 **In-Game Time:** `{GetInGameTime()}`\n" +
                          $"🌍 **World Size:** `{ConVar.Server.worldsize}`\n" +
                          $"🌱 **Seed:** `{ConVar.Server.seed}`\n" +
                          $"🏗️ **Map Entities:** `{mapEntities:N0}`";

                if (config.Settings.ShowProtocol)
                    message += $"\n🔗 **Protocol:** `{GetServerProtocol()}`";

                message += $"\n\n`🖥️ Server Data`\n" +
                          $"💾 **Memory Usage:** `{memoryUsed:N0} MB / {totalMemory:N0} MB`\n" +
                          $"⚡ **Server FPS:** `{fps:F1}`";

                if (config.Settings.ShowNetworkIO)
                    message += $"\n🌐 **Network IO:** `{networkIO}`";

                message += $"\n🕐 **Server Online For:** `{uptime}`\n" +
                          $"🗺️ **Last Wipe:** `{lastWipeDate}`";

                if (config.Settings.ShowNextWipeDate)
                    message += $"\n📅 **Next Wipe:** `{nextWipeDate}`";

                if (config.Settings.ShowLastBlueprintWipeDate)
                    message += $"\n📘 **Last BP Wipe:** `{lastBpWipeDate}`";

                int embedColor = _isOnline ? 65535 : 16711680;
                string title = "Live Server Statistics";

                SendOrEditDiscordMessage(title, message, embedColor);
            }
            catch (Exception ex)
            {
                PrintError($"Error sending performance message: {ex.Message}");
            }
        }

        private void SendInGamePerformanceMessageOnly()
        {
            try
            {
                int playerCount = BasePlayer.activePlayerList.Count;
                int sleepingPlayers = BasePlayer.sleepingPlayerList.Count;
                int joiningPlayers = ServerMgr.Instance.connectionQueue.Queued;
                int maxPlayers = ConVar.Server.maxplayers;
                int adminCount = BasePlayer.activePlayerList.Count(p => p.IsAdmin);
                int totalPlayersEver = GetTotalPlayersEver();

                SendInGamePerformanceMessage(playerCount, sleepingPlayers, joiningPlayers, maxPlayers, adminCount, totalPlayersEver);
            }
            catch (Exception ex)
            {
                PrintError($"Error sending in-game performance message: {ex.Message}");
            }
        }

        private void SendInGamePerformanceMessage(int playerCount, int sleepingPlayers, int joiningPlayers, int maxPlayers, int adminCount, int totalPlayersEver)
        {
            try
            {
                string performanceMessage = $"<color=#FFD700><size=14>Population Stats:</size></color>\n" +
                                          $"<color=#00FF00>Players Online:</color> <color=#FFFFFF>{playerCount}/{maxPlayers}</color>";

                if (config.Settings.ShowPlayersJoining && (!config.Settings.HideZeroValues || joiningPlayers > 0))
                    performanceMessage += $"\n<color=#FFFF00>Players In Queue:</color> <color=#FFFFFF>{joiningPlayers}</color>";

                if (config.Settings.ShowPlayersSleeping && (!config.Settings.HideZeroValues || sleepingPlayers > 0))
                    performanceMessage += $"\n<color=#FF0000>Players Sleeping:</color> <color=#FFFFFF>{sleepingPlayers}</color>";

                if (config.Settings.ShowAdminsOnline && (!config.Settings.HideZeroValues || adminCount > 0))
                    performanceMessage += $"\n<color=#FFB6C1>Admins Online:</color> <color=#FFFFFF>{adminCount}</color>";

                if (config.Settings.ShowTotalPlayersEver)
                    performanceMessage += $"\n<color=#87CEEB>Total Server Players:</color> <color=#FFFFFF>{totalPlayersEver:N0}</color>";

                foreach (var player in BasePlayer.activePlayerList)
                    player?.ChatMessage(performanceMessage);
            }
            catch (Exception ex)
            {
                PrintError($"Error sending in-game performance message: {ex.Message}");
            }
        }

        #endregion

        #region Discord Integration

        private bool IsDiscordRateLimited(string messageType)
        {
            if (lastDiscordMessage.TryGetValue(messageType, out DateTime lastTime))
            {
                if ((DateTime.Now - lastTime).TotalSeconds < config.Settings.DiscordRateLimit)
                    return true;
            }
            lastDiscordMessage[messageType] = DateTime.Now;
            return false;
        }

        private string GetServerImageUrl()
        {
            try
            {
                if (config.Settings.UseServerHeaderImage)
                {
                    string headerImage = ConVar.Server.headerimage;
                    if (!string.IsNullOrEmpty(headerImage) && IsValidImageUrl(headerImage))
                        return headerImage;
                }
                return config.Settings.FallbackImageURL;
            }
            catch
            {
                return config.Settings.FallbackImageURL;
            }
        }

        private bool IsValidImageUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            
            try
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri result)) return false;
                if (result.Scheme != "http" && result.Scheme != "https") return false;
                
                string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
                string path = result.AbsolutePath.ToLower();
                
                return imageExtensions.Any(ext => path.EndsWith(ext)) || 
                       path.Contains("image") || 
                       result.Host.Contains("imgur") || 
                       result.Host.Contains("steam") ||
                       result.Host.Contains("facepunch");
            }
            catch
            {
                return false;
            }
        }

        private void SendOrEditDiscordMessage(string title, string description, int color)
        {
            try
            {
                string webhookUrl = config.Settings.WebhookURL;
                if (string.IsNullOrEmpty(webhookUrl)) return;

                if (IsDiscordRateLimited("performance")) return;

                string serverName = ConVar.Server.hostname ?? "Unknown Server";
                string serverImageUrl = GetServerImageUrl();

                string displayServerName = serverName.Length > 45 
                    ? serverName.Substring(0, 42) + "..." 
                    : serverName;

                string embedTitle = $"[{displayServerName}]\n\n🤖 Live Server Statistics";

                var embed = new
                {
                    title = embedTitle,
                    description = description,
                    color = color,
                    thumbnail = config.Settings.ShowDiscordImage && config.Settings.UseThumbnail ? new { url = serverImageUrl } : null,
                    image = config.Settings.ShowDiscordImage && !config.Settings.UseThumbnail ? new { url = serverImageUrl } : null,
                    footer = new { text = $"rPop Live Server Statistics V{Version} by Ftuoil Xelrash" },
                    timestamp = DateTime.UtcNow.ToString("o")
                };

                var payload = JsonConvert.SerializeObject(new
                {
                    username = config.Settings.DiscordBotName,
                    avatar_url = "https://cdn-icons-png.flaticon.com/512/1161/1161388.png",
                    embeds = new[] { embed }
                });

                var headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json",
                    ["User-Agent"] = "rPop/1.0"
                };

                if (!string.IsNullOrEmpty(pluginData.StatusMessageId))
                {
                    string editUrl = $"{webhookUrl}/messages/{pluginData.StatusMessageId}";
                    
                    webrequest.Enqueue(editUrl, payload, (code, response) =>
                    {
                        if (code == 200 || code == 204)
                        {
                            // Message successfully edited
                        }
                        else if (code == 404)
                        {
                            Puts("Discord status message not found, creating new one...");
                            pluginData.StatusMessageId = null;
                            SaveData();
                            CreateNewDiscordMessage(webhookUrl, payload, headers);
                        }
                        else
                        {
                            PrintError($"Failed to edit Discord message: HTTP {code} - {response}");
                        }
                    }, this, Core.Libraries.RequestMethod.PATCH, headers);
                }
                else
                {
                    CreateNewDiscordMessage(webhookUrl, payload, headers);
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error sending/editing Discord message: {ex.Message}");
            }
        }

        private void CreateNewDiscordMessage(string webhookUrl, string payload, Dictionary<string, string> headers)
        {
            webrequest.Enqueue(webhookUrl + "?wait=true", payload, (code, response) =>
            {
                if (code == 200)
                {
                    try
                    {
                        var messageData = JsonConvert.DeserializeObject<Dictionary<string, object>>(response);
                        if (messageData.ContainsKey("id"))
                        {
                            pluginData.StatusMessageId = messageData["id"].ToString();
                            SaveData();
                            Puts($"Created new Discord status message with ID: {pluginData.StatusMessageId}");
                        }
                    }
                    catch (Exception ex)
                    {
                        PrintError($"Error parsing Discord message response: {ex.Message}");
                    }
                }
                else
                {
                    PrintError($"Failed to create Discord message: HTTP {code} - {response}");
                }
            }, this, Core.Libraries.RequestMethod.POST, headers);
        }

        #endregion

        #region Utilities

        private string GetHumanReadableUptime()
        {
            TimeSpan uptime = TimeSpan.FromSeconds(UnityEngine.Time.realtimeSinceStartup);
            
            if (uptime.TotalDays >= 1)
                return $"{(int)uptime.TotalDays} days, {uptime.Hours} hours, {uptime.Minutes} minutes";
            else if (uptime.TotalHours >= 1)
                return $"{uptime.Hours} hours, {uptime.Minutes} minutes";
            else if (uptime.TotalMinutes >= 1)
                return $"{uptime.Minutes} minutes, {uptime.Seconds} seconds";
            else
                return $"{uptime.Seconds} seconds";
        }

        private string GetLastWipeDate()
        {
            try
            {
                DateTime wipeTime = GetWipeTimeFromSaveFile();
                
                if (wipeTime == DateTime.MinValue)
                {
                    TimeSpan uptime = TimeSpan.FromSeconds(UnityEngine.Time.realtimeSinceStartup);
                    DateTime serverStart = DateTime.Now - uptime;
                    return $"{serverStart:MMM dd, yyyy} (Server Start)";
                }
                
                TimeSpan timeSinceWipe = DateTime.Now - wipeTime;
                
                if (timeSinceWipe.TotalDays < 1)
                {
                    return $"{wipeTime:MMM dd, yyyy} ({(int)timeSinceWipe.TotalHours}h ago)";
                }
                else if (timeSinceWipe.TotalDays < 7)
                {
                    return $"{wipeTime:MMM dd, yyyy} ({(int)timeSinceWipe.TotalDays}d ago)";
                }
                else
                {
                    return $"{wipeTime:MMM dd, yyyy} ({(int)timeSinceWipe.TotalDays}d ago)";
                }
            }
            catch (Exception ex)
            {
                PrintError($"Error getting last wipe date: {ex.Message}");
                return "Unknown";
            }
        }

        private DateTime GetWipeTimeFromSaveFile()
        {
            try
            {
                string serverIdentity = ConVar.Server.identity ?? "server";
                string serverPath = Path.Combine("server", serverIdentity);
                
                if (!Directory.Exists(serverPath)) 
                    return DateTime.MinValue;

                var saveFiles = Directory.GetFiles(serverPath, "ProceduralMap.*.sav", SearchOption.TopDirectoryOnly);
                
                if (saveFiles.Length == 0)
                    return DateTime.MinValue;

                var latestSaveFile = saveFiles.OrderByDescending(f => File.GetCreationTime(f)).First();
                DateTime creationTime = File.GetCreationTime(latestSaveFile);
                
                return creationTime;
            }
            catch (Exception ex)
            {
                PrintError($"Error getting wipe time from save file: {ex.Message}");
                return DateTime.MinValue;
            }
        }

        private string GetCooldownTime(TimeSpan remainingTime)
        {
            if (remainingTime.TotalMinutes >= 1)
            {
                int minutes = (int)remainingTime.TotalMinutes;
                int seconds = remainingTime.Seconds;
                return seconds > 0 ? $"{minutes} minute(s) and {seconds} second(s)" : $"{minutes} minute(s)";
            }
            else
            {
                int seconds = Math.Max(1, (int)Math.Ceiling(remainingTime.TotalSeconds));
                return $"{seconds} second(s)";
            }
        }

        private string GetInGameTime()
        {
            try
            {
                float timeOfDay = TOD_Sky.Instance.Cycle.Hour;
                int hours = Mathf.FloorToInt(timeOfDay);
                int minutes = Mathf.FloorToInt((timeOfDay - hours) * 60);
                
                string period = hours >= 12 ? "PM" : "AM";
                int displayHours = hours == 0 ? 12 : (hours > 12 ? hours - 12 : hours);
                
                return $"{displayHours:D2}:{minutes:D2} {period}";
            }
            catch (Exception ex)
            {
                PrintError($"Error getting in-game time: {ex.Message}");
                return "Unknown";
            }
        }

        #endregion

        #region Console Commands

        [ConsoleCommand("rpop.test")]
        private void RPopTestCommand(ConsoleSystem.Arg arg)
        {
            int playerCount = BasePlayer.activePlayerList.Count;
            int sleepingPlayers = BasePlayer.sleepingPlayerList.Count;
            int joiningPlayers = ServerMgr.Instance.connectionQueue.Queued;
            int maxPlayers = ConVar.Server.maxplayers;
            int adminCount = BasePlayer.activePlayerList.Count(p => p.IsAdmin);
            string uptime = GetHumanReadableUptime();
            string lastWipeDate = GetLastWipeDate();
            string lastBpWipeDate = GetFormattedBlueprintWipeDate();
            string nextWipeDate = GetFormattedNextWipeDate();
            string networkIO = GetNetworkIO();
            int totalPlayersEver = GetTotalPlayersEver();
            string averageConnectionTime = GetAverageConnectionTime();
            string timezone = GetServerTimeZoneAbbreviation();

            Puts($"=== rPop Test Statistics ===");
            Puts($"Server Status: {GetServerStatus()}");
            Puts($"Server Timezone: {timezone}");
            Puts($"Players Online: {playerCount}/{maxPlayers}");
            Puts($"Players Joining: {joiningPlayers}");
            Puts($"Players Sleeping: {sleepingPlayers}");
            Puts($"Admins Online: {adminCount}");
            Puts($"Total Server Players: {totalPlayersEver:N0}");
            Puts($"Average Session Time: {averageConnectionTime}");
            Puts($"Network IO: {networkIO}");
            Puts($"In-Game Time: {GetInGameTime()}");
            Puts($"Server Uptime: {uptime}");
            Puts($"Last Wipe Date: {lastWipeDate}");
            Puts($"Next Wipe Date: {nextWipeDate}");
            Puts($"Last Blueprint Wipe Date: {lastBpWipeDate}");
            Puts($"Today's Peak: {pluginData.TodayHigh.Count} players");
            Puts($"Monthly Peak: {pluginData.MonthlyHigh.Count} players");
            Puts($"All-Time Peak: {pluginData.AllTimeHigh.Count} players");
            Puts($"Instant Population Updates: {(config.Settings.EnableInstantPopulationUpdates ? "Enabled" : "Disabled")}");
            Puts($"Population Update Delay: {config.Settings.PopulationUpdateDelay} seconds");
            
            SendOrEditDiscordMessage("🧪 Test Message", 
                $"Test message sent via console command.\n" +
                $"**Server Status:** `{GetServerStatus()}`\n" +
                $"**Server Time:** `{DateTime.Now:yyyy-MM-dd HH:mm:ss}`\n" +
                $"**Plugin Version:** `{Version}`\n" +
                $"**Server Timezone:** `{timezone}`\n" +
                $"**Total Server Players:** `{totalPlayersEver:N0}`\n" +
                $"**Average Active Session Time:** `{averageConnectionTime}`\n" +
                $"**Network IO:** `{networkIO}`\n" +
                $"**Next Wipe Date:** `{nextWipeDate}`\n" +
                $"**Last Blueprint Wipe:** `{lastBpWipeDate}`\n" +
                $"**Instant Updates:** `{(config.Settings.EnableInstantPopulationUpdates ? "Enabled" : "Disabled")}`", 
                16776960);
        }

        [ConsoleCommand("rpop.performance")]
        private void RPopPerformanceCommand(ConsoleSystem.Arg arg)
        {
            SendPerformanceMessage();
            Puts("Performance message sent to Discord!");
        }

        [ConsoleCommand("rpop.resetdata")]
        private void RPopResetDataCommand(ConsoleSystem.Arg arg)
        {
            pluginData = new PluginData();
            SaveData();
            Puts("rPop data has been reset!");
            SendOrEditDiscordMessage("🗑️ Data Reset", "Population records have been reset via console command", 16711680);
        }

        [ConsoleCommand("rpop.resetmessage")]
        private void RPopResetMessageCommand(ConsoleSystem.Arg arg)
        {
            pluginData.StatusMessageId = null;
            SaveData();
            Puts("Discord status message ID has been reset. A new message will be created on next update.");
        }

        [ConsoleCommand("rpop.status")]
        private void RPopStatusCommand(ConsoleSystem.Arg arg)
        {
            Puts($"Server Status: {GetServerStatus()}");
            Puts($"_isOnline variable: {_isOnline}");
            Puts($"Server Timezone: {GetServerTimeZoneAbbreviation()}");
            Puts($"Next Wipe Date: {GetFormattedNextWipeDate()}");
            Puts($"Instant Population Updates: {(config.Settings.EnableInstantPopulationUpdates ? "Enabled" : "Disabled")}");
            Puts($"Population Update Delay: {config.Settings.PopulationUpdateDelay} seconds");
            Puts($"Performance Timer Interval: {config.Settings.PerformanceMessageInterval} minutes");
            Puts($"Status will update immediately when server shuts down.");
        }

        [ConsoleCommand("rpop.help")]
        private void RPopHelpCommand(ConsoleSystem.Arg arg)
        {
            Puts("rPop Console Commands:");
            Puts("rpop.test - Show current server statistics and send test Discord message");
            Puts("rpop.performance - Force send performance stats to Discord");
            Puts("rpop.resetdata - Reset all population records data");
            Puts("rpop.resetmessage - Reset Discord message ID (creates new status message)");
            Puts("rpop.status - Show current server status and timer information");
            Puts("rpop.help - Show this help message");
            Puts("");
            Puts("Player Commands:");
            Puts("!pop - Display server statistics to all online players");
            Puts("");
            Puts("Configuration:");
            Puts("Data is stored in: oxide/data/rPop.json");
            Puts("Config is stored in: oxide/config/rPop.json");
            Puts("");
            Puts("Discord Setup:");
            Puts("1. Create a Discord webhook in your channel");
            Puts("2. Add the webhook URL to the 'Discord Webhook URL' config setting");
            Puts("3. The plugin will automatically create and update a single status message");
            Puts("");
            Puts("Session Time Tracking:");
            int totalPlayersEver = GetTotalPlayersEver();
            Puts($"Currently tracking {totalPlayersEver:N0} unique server players");
            Puts($"Average session time: {GetAverageConnectionTime()}");
            Puts("Session times are tracked from when players connect to the server");
            
            if (!string.IsNullOrEmpty(pluginData.StatusMessageId))
            {
                Puts($"Current Discord status message ID: {pluginData.StatusMessageId}");
            }
            else
            {
                Puts("No Discord status message ID stored (will create new message on next update)");
            }
            
            Puts("");
            Puts("Wipe Schedule:");
            Puts($"Server Timezone: {GetServerTimeZoneAbbreviation()}");
            Puts($"Next Wipe Date: {GetFormattedNextWipeDate()}");
            Puts("Wipes occur on the first Thursday of each month at 1:00 PM server time");
        }

        [ConsoleCommand("rpop.forceconfig")]
        private void RPopForceConfigCommand(ConsoleSystem.Arg arg)
        {
            LoadDefaultConfig();
            Puts("Configuration file regenerated with all default settings!");
        }

        #endregion
    }
}