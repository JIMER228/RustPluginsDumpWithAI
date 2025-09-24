// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Threading;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Timers;
using Newtonsoft.Json;
using UnityEngine;


namespace Oxide.Plugins
{
    [Info("Anticheat+", "Nord", "1.1.2")]
    [Description("An extra layer of protection for your rust server!")]

    public class AnticheatPlus : RustPlugin

    {
        private BasePlayer lastFlaggedPlayer = null;
        private Vector3 excavatorPosition;
        private Timer _checkPlayersTimer;
        private DateTime lastMessageSent = DateTime.MinValue;
        private TimeSpan messageCooldown => TimeSpan.FromSeconds(_config.WebhookCooldown);
        private const string DataFileName = "AnticheatPlusAdminData";
        private Dictionary<string, bool> AdminPreferences = new Dictionary<string, bool>();
        private void LoadAdminPreferences() { AdminPreferences = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, bool>>(DataFileName) ?? new Dictionary<string, bool>(); }
        private void SaveAdminPreferences() { Interface.Oxide.DataFileSystem.WriteObject(DataFileName, AdminPreferences); }
        private class ReportData { public List<DateTime> ReportTimes = new List<DateTime>(); }
        private Dictionary<ulong, ReportData> playerReports = new Dictionary<ulong, ReportData>();  
        private Dictionary<ulong, DateTime> elevatedPlayers = new Dictionary<ulong, DateTime>();

        #region Configuration
        protected override void LoadDefaultConfig() => _config = new Configuration(); protected override void LoadConfig() { base.LoadConfig(); _config = Config.ReadObject<Configuration>(); SaveConfig(); }
        protected override void SaveConfig() => Config.WriteObject(_config);
        private class Configuration

        {
            [JsonProperty("Debug Mode (will cause console spam.)")]
            public bool debugMode { get; private set; } = false;

            [JsonProperty("Anticheat+ Update Time (default: 5 seconds")]
            public float AnticheatCheckSpeed { get; private set; } = 5;

            [JsonProperty("Kick When Flagged")]
            public bool KickUponFlag { get; private set; } = false;

            [JsonProperty("Flag Admins")]
            public bool FlagAdmins { get; private set; } = false;

            [JsonProperty("Alert In-Game Admins (with anticheatplus.admin permission)")]
            public bool AlertIngameAdmins { get; private set; } = true;

            [JsonProperty("Enable Flyhack+ (beta)")]
            public bool BetterFlyhack { get; private set; } = true;

            [JsonProperty("Flyhack+ Threshold (in meters, default is 7)")]
            public float BetterFlyhackThreshold { get; private set; } = 7;

            [JsonProperty("Flyhack+ Threshold Time (time the user is exceeding the height threshold defined above. default is 10 seconds)")]
            public float BetterFlyhackHoldTime { get; private set; } = 10;

            [JsonProperty("Enable Max Shooting Distance Check")]
            public bool EnableMaxShootingDistanceCheck { get; private set; } = true;
            [JsonProperty("Ignore Scientist (Max Distance Shots)")]
            public bool IgnoreScientist { get; private set; } = true;

            [JsonProperty("Max Shooting Distance Per Weapon")]
            public Dictionary<string, (float, string)> MaxShootingDistancePerWeapon { get; set; } = new Dictionary<string, (float, string)>
            {
                { "rifle.ak", (300f, "https://rustlabs.com/img/items180/rifle.ak.png") },
                { "rifle.ak.diver", (300f, "https://rustlabs.com/img/items180/rifle.ak.png") },
                { "rifle.ak.ice", (300f, "https://rustlabs.com/img/items180/rifle.ak.png") },
                { "lmg.m249", (300f, "https://rustlabs.com/img/items180/lmg.m249.png") },
                { "hmlmg", (300f, "https://rustlabs.com/img/items180/hmlmg.png") },
                { "rifle.lr300", (300f, "https://rustlabs.com/img/items180/rifle.lr300.png") },
                { "rifle.bolt", (350f, "https://rustlabs.com/img/items180/rifle.bolt.png") },
                { "rifle.l96", (400f, "https://rustlabs.com/img/items180/rifle.l96.png") },
                { "rifle.semiauto", (200f, "https://rustlabs.com/img/items180/rifle.semiauto.png") },
                { "pistol.python", (100f, "https://rustlabs.com/img/items180/pistol.python.png") },
                { "pistol.semi", (100f, "https://rustlabs.com/img/items180/pistol.semiauto.png") },
                { "smg.mp5", (150f, "https://rustlabs.com/img/items180/smg.mp5.png") },
                { "smg.2", (120f, "https://rustlabs.com/img/items180/smg.2.png") },
                { "shotgun.pump", (40f, "https://rustlabs.com/img/items180/shotgun.pump.png") },
                { "shotgun.spas12", (30f, "https://rustlabs.com/img/items180/shotgun.spas12.png") },
                { "crossbow", (75f, "https://rustlabs.com/img/items180/crossbow.png") },
                { "bow.compound", (100f, "https://rustlabs.com/img/items180/bow.compound.png") },
                { "bow.hunting", (60f, "https://rustlabs.com/img/items180/bow.hunting.png") },
                { "shotgun.double", (40f, "https://rustlabs.com/img/items180/shotgun.double.png") },
                { "pistol.eoka", (10f, "https://rustlabs.com/img/items180/pistol.eoka.png") },
                { "flamethrower", (10f, "https://rustlabs.com/img/items180/flamethrower.png") },
                { "rifle.m249", (300f, "https://rustlabs.com/img/items180/lmg.m249.png") },
                { "rifle.m39", (250f, "https://rustlabs.com/img/items180/rifle.m39.png") },
                { "pistol.m92", (100f, "https://rustlabs.com/img/items180/pistol.m92.png") },
                { "smg.thompson", (150f, "https://rustlabs.com/img/items180/pistol.m92.png") },
                { "shotgun.waterpipe", (20f, "https://rustlabs.com/img/items180/shotgun.waterpipe.png") },
            };
            [JsonProperty("Max Shooting Distance (default fallback if no weapon is found upon attacking a player)")]
            public float MaxShootingDistance { get; private set; } = 285;

            [JsonProperty("Enable Shooting Height Check")]
            public bool EnableShootingHeightCheck { get; private set; } = true;

            [JsonProperty("Max Shooting Height")]
            public float MaxShootingHeight { get; private set; } = 3;

            [JsonProperty("Enable MySQL")]
            public bool EnableMySQL { get; private set; } = false;

            [JsonProperty("MySQL Host")]
            public string MySQL_Host { get; private set; } = "";

            [JsonProperty("MySQL Database")]
            public string MySQL_Database { get; private set; } = "";

            [JsonProperty("MySQL User")]
            public string MySQL_User { get; private set; } = "";

            [JsonProperty("MySQL Password")]
            public string MySQL_Password { get; private set; } = "";

            [JsonProperty("Enable Discord Integration")]
            public bool EnableDiscordIntegration { get; private set; } = false;

            [JsonProperty("Discord Webhook URL")]
            public string DiscordWebhookUrl { get; private set; } = "Example - https://discord.com/api/webhooks/";

            [JsonProperty("Discord Webhook Cooldown (default: 30 seconds) - this is to simply prevent spam, set to '1' to disable.")]
            public float WebhookCooldown { get; private set; } = 30;

            [JsonProperty("Discord Username")]
            public string DiscordUsername { get; private set; } = "Anticheat+";

            [JsonProperty("Embed Color")]
            public int EmbedColor { get; private set; } = 16711680;

            [JsonProperty("Embed Title")]
            public string EmbedTitle { get; private set; } = "Anticheat Alert";

            [JsonProperty("Embed Footer")]
            public string EmbedFooter { get; private set; } = "Anticheat System";

            [JsonProperty("Flyhack Kick Reason")]
            public string FlyhackKickReason { get; private set; } = "Anticheat+ Violation [Flyhack]";

            [JsonProperty("Shoot in Air Kick Reason")]
            public string ShootInAirKickReason { get; private set; } = "Anticheat+ Violation [Shooting-in-air]";

            [JsonProperty("Shoot Max Distance Kick Reason")]
            public string ShootMaxDistanceKickReason { get; private set; } = "Anticheat+ Violation [Max-Shot-Distance]";

        }
        #endregion
        private Configuration _config;

        void OnServerInitialized()
        {
            LoadConfigValues();
            LoadAdminPreferences();
            permission.RegisterPermission("anticheatplus.bypass", this);
            permission.RegisterPermission("anticheatplus.admin", this);
            _checkPlayersTimer = timer.Repeat(_config.AnticheatCheckSpeed, 0, CheckPlayers);
            excavatorPosition = FindExcavatorCoordinates();

            Puts("\n========================================\n" +
              $"- Anticheat+ Update Time: {_config.AnticheatCheckSpeed}s -\n" +
              "Initialized! Scanning for cheaters.\n" +
              $"Version: 1.1.2\nCodefling: codefling.com/nord\n" +
              "========================================\n");

        }

        private void LoadConfigValues()
        {
            LoadConfig();
            _checkPlayersTimer?.Destroy();
            _checkPlayersTimer = timer.Repeat(_config.AnticheatCheckSpeed, 0, CheckPlayers);
            excavatorPosition = FindExcavatorCoordinates();
        }

        void Unload()
        {
            _checkPlayersTimer?.Destroy();
        }

        #region Commands

        [ConsoleCommand("anticheatplus.reloadconfig")]
        private void CmdReloadConfig(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null) { return; }
            LoadConfigValues();
            Puts("Anticheat+ configuration reloaded.");
        }

        [ChatCommand("recentflag")]
        private void RecentFlagCommand(BasePlayer player, string command, string[] args)
        {
            if (!IsValidAdmin(player))
            {
                // SendReply(player, "You do not have permission to use this command.");
                return;
            }

            if (lastFlaggedPlayer == null || !lastFlaggedPlayer.IsConnected)
            {
                SendReply(player, "No recent flags available or player is no longer online.");
                return;
            }

            player.Teleport(lastFlaggedPlayer.transform.position);
            SendReply(player, $"<color=white><size=20>Anticheat+</size></color>\n\n<color=white><size=16>Teleported to the most recent flagged player: {lastFlaggedPlayer.displayName}</size></color>");
        }


        [ChatCommand("togglenotifications")]
        private void ToggleAlertsCommand(BasePlayer player, string command, string[] args)
        {
            string steamId = player.UserIDString;
            if (!IsValidAdmin(player)) { return; }
            bool currentValue = AdminPreferences.ContainsKey(steamId) && AdminPreferences[steamId];
            AdminPreferences[steamId] = !currentValue;
            SaveAdminPreferences();
            SendReply(player, $"<color=white><size=20>Anticheat+</size></color>\n\n<color=white><size=16>In-game alerts are now {(AdminPreferences[steamId] ? "enabled" : "disabled")}.</size></color>");
        }
        #endregion

        #region Anticheat Core

        private void CheckPlayers()
        {
            if (_config.BetterFlyhack)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    if (!player.IsConnected || HasPermission(player) || IsNearExcavator(player.transform.position)) continue;
                    float heightAboveGround = player.transform.position.y - TerrainMeta.HeightMap.GetHeight(player.transform.position);
                    bool isInTheAir = !player.modelState.onground && !player.isMounted && !player.IsSwimming();
                    if (_config.debugMode)
                    {
                        Puts($"Player - {player.displayName} (isInTheAir: {isInTheAir}, isSwimming: {player.IsSwimming()}) - current height: {heightAboveGround}");
                    }
                    if (isInTheAir && heightAboveGround > _config.BetterFlyhackThreshold)
                    {
                        if (!elevatedPlayers.ContainsKey(player.userID))
                        {
                            elevatedPlayers[player.userID] = DateTime.UtcNow;
                        }
                        else if (DateTime.UtcNow - elevatedPlayers[player.userID] > TimeSpan.FromSeconds(_config.BetterFlyhackHoldTime))
                        {
                            HandleViolation(player, heightAboveGround, _config.FlyhackKickReason, null);
                            elevatedPlayers.Remove(player.userID);
                        }
                    }
                    else if (elevatedPlayers.ContainsKey(player.userID))
                    {
                        elevatedPlayers.Remove(player.userID);
                    }
                }
            }
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (!(entity is BasePlayer) || info == null || info.Initiator == null || !(info.Initiator is BasePlayer))
                return;

            if (_config.IgnoreScientist && (entity is NPCPlayer || info.Initiator is NPCPlayer))
                return;

            BasePlayer attacker = info.Initiator.ToPlayer();
            BasePlayer victim = entity.ToPlayer();

            float distanceSquared = (attacker.transform.position - victim.transform.position).sqrMagnitude;
            bool isInTheAir = !attacker.modelState.onground;
            float heightAboveGround = attacker.transform.position.y - TerrainMeta.HeightMap.GetHeight(attacker.transform.position);
            bool isInVehicle = attacker.isMounted;

            Item weapon = attacker.GetActiveItem();
            if (weapon == null) return;
            string weaponShortname = weapon.info.shortname;
            float maxDistanceForWeapon = _config.MaxShootingDistancePerWeapon.ContainsKey(weaponShortname)
                                             ? _config.MaxShootingDistancePerWeapon[weaponShortname].Item1
                                             : _config.MaxShootingDistance;



            if (_config.EnableShootingHeightCheck && isInTheAir && !isInVehicle && !IsNearExcavator(attacker.transform.position))
            {
                if (heightAboveGround > _config.MaxShootingHeight)
                {
                    HandleViolation(attacker, distanceSquared, _config.ShootInAirKickReason, weapon);
                    return;
                }
            }

            if (_config.EnableMaxShootingDistanceCheck && distanceSquared > maxDistanceForWeapon * maxDistanceForWeapon)
            {
                HandleViolation(attacker, distanceSquared, _config.ShootMaxDistanceKickReason, weapon);
            }
        }


        void HandleViolation(BasePlayer attacker, float distanceSquared, string reason, Item weapon)
        {
            lastFlaggedPlayer = attacker;

            string discordMessage = $"- **Player:** `{attacker.displayName}`\n" +
                                    $"- **Steam ID:** [{attacker.UserIDString}](https://steamcommunity.com/profiles/{attacker.UserIDString})\n" +
                                    $"- **Reason:** `{reason}`\n" +
                                    $"- **Position:** `{attacker.transform.position}`\n" +
                                    $"- **Time:** <t:{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}> (<t:{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}:R>)\n" +
                                    $"- **Server:** `{ConVar.Server.hostname}`";

            string imageLink = null;

            if (reason == _config.ShootMaxDistanceKickReason && weapon != null)
            {
                if (_config.MaxShootingDistancePerWeapon.TryGetValue(weapon.info.shortname, out var weaponData))
                {
                    imageLink = weaponData.Item2;
                }

                string weaponInfo = $"- **Weapon Type:** `{weapon.info.shortname}`\n- **Distance:** `{Mathf.Sqrt(distanceSquared)}m`\n";
                discordMessage = weaponInfo + discordMessage;
            }

            if (!_config.FlagAdmins && HasPermission(attacker)) { return; }

            if (_config.KickUponFlag)
            {
                attacker.Kick(reason);
                discordMessage = "- **Player Kicked:** " + discordMessage.Substring(3);
            }

            if (_config.EnableDiscordIntegration)
            {
                SendDiscordMessage(discordMessage, imageLink);
            }

            if (_config.EnableMySQL)
            {
                LogPlayerAction(ulong.Parse(attacker.UserIDString), attacker.displayName, Mathf.Sqrt(distanceSquared), attacker.transform.position);
            }

            if (_config.AlertIngameAdmins)
            {
                string message = $"{attacker.displayName} has been flagged for: {reason} /recentflag to teleport to the player";

                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    if (IsValidAdmin(player) && ShouldReceiveAlerts(player))
                    {
                        player.SendConsoleCommand($"gametip.showgametip \"{message}\"");
                        timer.Once(2.0f, () => { player.SendConsoleCommand("gametip.hidegametip"); });
                    }
                }
            }
        }

        #endregion

        #region Dependencies 
        private bool ShouldReceiveAlerts(BasePlayer player)
        {
            return AdminPreferences.TryGetValue(player.UserIDString, out bool receiveAlerts) && receiveAlerts;
        }

        private void LogPlayerAction(ulong steamId, string displayName, float distance, Vector3 location)
        {
            using (var conn = CreateConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO AnticheatPlus (SteamID, DisplayName, HighestDistanceShot, MostRecentLocation, TotalKicks, TotalFlags) VALUES (@SteamID, @DisplayName, @HighestDistanceShot, @MostRecentLocation, 0, 0) ON DUPLICATE KEY UPDATE HighestDistanceShot = GREATEST(HighestDistanceShot, @HighestDistanceShot), MostRecentLocation = @MostRecentLocation, TotalKicks = TotalKicks + 1;";
                cmd.Parameters.AddWithValue("@SteamID", steamId);
                cmd.Parameters.AddWithValue("@DisplayName", displayName);
                cmd.Parameters.AddWithValue("@HighestDistanceShot", distance);
                cmd.Parameters.AddWithValue("@MostRecentLocation", location.ToString());
                cmd.ExecuteNonQuery();
            }
        }
        private void SendDiscordMessage(string content, string imageLink = null)
        {
            if (DateTime.UtcNow - lastMessageSent < messageCooldown)
            {
                // Puts("Cooldown in effect. Message not sent.");
                return;
            }

            var embed = new
            {
                title = _config.EmbedTitle,
                description = content,
                color = _config.EmbedColor,
                footer = new { text = _config.EmbedFooter },
                thumbnail = imageLink != null ? new { url = imageLink } : null
            };

            var payload = new
            {
                username = _config.DiscordUsername,
                embeds = new[] { embed }
            };

            string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

            webrequest.EnqueuePost(_config.DiscordWebhookUrl, jsonPayload, (code, response) =>
            {
                if (code != 200 && code != 204)
                {
                    Puts($"Error: Failed to send Discord webhook. Status code: {code}");
                }
                else
                {
                    lastMessageSent = DateTime.UtcNow;
                }
            }, this, new Dictionary<string, string> { { "Content-Type", "application/json" } });
        }


        private Vector3 FindExcavatorCoordinates()
        {
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                if (entity?.ShortPrefabName != null && entity.ShortPrefabName.Contains("excavator"))
                {
                    return entity.transform.position;
                }
            }
            return Vector3.zero;

        }
        private bool IsNearExcavator(Vector3 playerPosition)
        {
            float radius = 100f;
            return Vector3.Distance(playerPosition, excavatorPosition) < radius;
        }

        private bool IsValidAdmin(BasePlayer player)
        {
            return permission.UserHasPermission(player.UserIDString, "anticheatplus.admin");
        }

        private bool HasPermission(BasePlayer player)
        {
            return permission.UserHasPermission(player.UserIDString, "anticheatplus.bypass");

        }
        private MySqlConnection CreateConnection()
        {
            if (!_config.EnableMySQL)
            {
                return null;
            }

            string connectionString = $"server={_config.MySQL_Host};database={_config.MySQL_Database};user={_config.MySQL_User};password={_config.MySQL_Password};";
            return new MySqlConnection(connectionString);
        }

        private void InitializeDatabase()
        {
            using (var conn = CreateConnection())
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS AnticheatPlus (SteamID BIGINT PRIMARY KEY, DisplayName VARCHAR(255), HighestDistanceShot FLOAT, MostRecentLocation VARCHAR(255), TotalKicks INT, TotalFlags INT);";
                cmd.ExecuteNonQuery();
            }
        }

        #endregion
    }
}