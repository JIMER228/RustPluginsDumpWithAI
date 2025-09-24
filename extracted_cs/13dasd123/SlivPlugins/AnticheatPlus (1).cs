using System;
using System.Threading;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System.Timers;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;


namespace Oxide.Plugins
{
    [Info("Anticheat+", "Nord", "1.1.4")]
    [Description("An extra layer of protection for your rust server!")]

    public class AnticheatPlus : RustPlugin

    {


        private Dictionary<BasePlayer, ViolationCount> cachedViolationsShootInAir = new Dictionary<BasePlayer, ViolationCount>();
        private Dictionary<BasePlayer, ViolationCount> cachedViolationsShootMaxDistance = new Dictionary<BasePlayer, ViolationCount>();
        private Dictionary<BasePlayer, ViolationCount> cachedViolationsFlyhackViolation = new Dictionary<BasePlayer, ViolationCount>();
        private Dictionary<string, List<int>> playerPingHistory = new Dictionary<string, List<int>>();
        private Dictionary<ulong, DateTime> elevatedPlayers = new Dictionary<ulong, DateTime>();

        private class ViolationCount
        {
            public int Count { get; set; }
            public long TimeOfLastViolation { get; set; }
        }
        public DateTime TimeOfViolation { get; set; }


        private BasePlayer lastFlaggedPlayer = null;
        private Vector3 excavatorPosition;


        private Timer _checkPlayersTimer;
        private Timer _checkPlayersPingTimer;
        private Timer _checkServerStatusTimer;

        private DateTime lastMessageSent = DateTime.MinValue;
        private TimeSpan messageCooldown => TimeSpan.FromSeconds(_config.WebhookCooldown);
        private const string DataFileName = "AnticheatPlusAdminData";
        private Dictionary<string, bool> AdminPreferences = new Dictionary<string, bool>();
        private void LoadAdminPreferences() { AdminPreferences = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, bool>>(DataFileName) ?? new Dictionary<string, bool>(); }
        private void SaveAdminPreferences() { Interface.Oxide.DataFileSystem.WriteObject(DataFileName, AdminPreferences); }







        #region Configuration
        protected override void LoadDefaultConfig() => _config = new Configuration(); protected override void LoadConfig() { base.LoadConfig(); _config = Config.ReadObject<Configuration>(); SaveConfig(); }
        protected override void SaveConfig() => Config.WriteObject(_config);
        private class Configuration

        {
            [JsonProperty("Debug Mode (will cause console spam.)")]
            public bool debugMode { get; private set; } = false;

            [JsonProperty("Anticheat Throttling (will disable some anticheat features if server fps has fell below 13 FPS.)")]
            public bool acThrottle { get; private set; } = false;

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
            [JsonProperty("Max Shooting Distance (default fallback if no weapon is found upon attacking a player)")]
            public float MaxShootingDistance { get; private set; } = 285;

            [JsonProperty("Enable Shooting Height Check")]
            public bool EnableShootingHeightCheck { get; private set; } = true;

            [JsonProperty("Max Shooting Height")]
            public float MaxShootingHeight { get; private set; } = 3;

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
                { "shotgun.m4", (275f, "https://rustlabs.com/img/items180/shotgun.m4.png")},
            };
        }
        #endregion
        private Configuration _config;


        #region Hooks
        private void OnServerInitialized()
        {
            Puts("> Clearing existing data...");
            _checkPlayersTimer?.Destroy();

            Puts("> Loading configuration...");

            LoadConfig();
            LoadAdminPreferences();

            Puts("> Registering permissions...");
            permission.RegisterPermission("anticheatplus.bypass", this);
            permission.RegisterPermission("anticheatplus.admin", this);

            Puts("> Finding Glitchty Monuments (applying blocks)...");

            excavatorPosition = FindExcavatorCoordinates();


            Puts("> Starting Anticheat+ Core Modules...");

            _checkPlayersTimer = timer.Every(_config.AnticheatCheckSpeed, CheckPlayers);

            _checkPlayersPingTimer = timer.Every(_config.AnticheatCheckSpeed, TrackPlayerPing);


            if (_config.acThrottle)
            {
                _checkServerStatusTimer = timer.Every(_config.AnticheatCheckSpeed, CheckServerStatus);
            }
            Puts($"> Anticheat+ Update Time: {_config.AnticheatCheckSpeed}s");
            Puts("> Anticheat+ is now running!");


        }

        private void OnPlayerInit(BasePlayer player)
        {
            playerPingHistory[player.userID.ToString()] = new List<int>();
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            string userIdString = player.userID.ToString();
            if (playerPingHistory.ContainsKey(userIdString))
            {
                playerPingHistory.Remove(userIdString);
            }
        }

        void Unload()
        {
            _checkPlayersTimer?.Destroy();
            _checkPlayersPingTimer?.Destroy();
            _checkServerStatusTimer?.Destroy();
        }



        #endregion

        #region Commands

        // [ConsoleCommand("anticheatplus.reloadconfig")]
        // private void CmdReloadConfig(ConsoleSystem.Arg arg)
        // {
        //     if (arg.Connection != null) { return; }
        //     Puts("Anticheat+ configuration reloaded.");
        // }

        [ChatCommand("recentflag")]
        private void RecentFlagCommand(BasePlayer player, string command, string[] args)
        {
            if (!IsValidAdmin(player))
            {
                SendReply(player, "You do not have permission to use this command.");
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

        bool timersEnabled = true;

        void CheckServerStatus()
        {
            int currentFPS = Performance.report.frameRate;

            if (_config.debugMode)
            {
                Puts($"> Current FPS: {currentFPS}");

            }
            if (currentFPS < 13)
            {
                if (timersEnabled)
                {
                    timer.Destroy(ref _checkPlayersTimer);
                    timer.Destroy(ref _checkPlayersPingTimer);
                    timersEnabled = false;

                    Puts("> Server FPS has fell below 13 FPS - Anticheat+ has disabled some features to prevent server lag.");
                }
            }
            else if (currentFPS > 15 && !timersEnabled)
            {
                _checkPlayersTimer = timer.Every(_config.AnticheatCheckSpeed, CheckPlayers);
                _checkPlayersPingTimer = timer.Every(_config.AnticheatCheckSpeed, TrackPlayerPing);
                timersEnabled = true;
                Puts("> Server FPS has recovered - Anticheat+ has re-enabled all features.");
            }

        }


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
                    AddViolation(cachedViolationsShootInAir, attacker); 
                    return;
                }
            }

            if (_config.EnableMaxShootingDistanceCheck && distanceSquared > maxDistanceForWeapon * maxDistanceForWeapon)
            {
                HandleViolation(attacker, distanceSquared, _config.ShootMaxDistanceKickReason, weapon);
                AddViolation(cachedViolationsShootMaxDistance, attacker);
            }
        }

        void HandleViolation(BasePlayer attacker, float distanceSquared, string reason, Item weapon)
        {
            lastFlaggedPlayer = attacker;
            int shootInAirCount = cachedViolationsShootInAir.TryGetValue(attacker, out ViolationCount inAirDetails) ? inAirDetails.Count : 0;
            int shootMaxDistanceCount = cachedViolationsShootMaxDistance.TryGetValue(attacker, out ViolationCount maxDistanceDetails) ? maxDistanceDetails.Count : 0;
            int flyhackCount = cachedViolationsFlyhackViolation.TryGetValue(attacker, out ViolationCount flyhackDetails) ? flyhackDetails.Count : 0;

            string discordMessage = $"- **Player:** `{attacker.displayName}`\n" +
                                    $"- **Steam ID:** [{attacker.UserIDString}](https://steamcommunity.com/profiles/{attacker.UserIDString})\n" +
                                    $"- **Reason:** `{reason}`\n" +
                                    $"- **Position:** `{attacker.transform.position}`\n" +
                                    $"- **Time:** <t:{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}> (<t:{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}:R>)\n" +
                                    $"- **Server:** `{ConVar.Server.hostname}`\n" +
                                    $"- **Violations Summary:**\n" +
                                    $"    - Shooting in Air: {shootInAirCount}\n" +
                                    $"    - Max Distance Shots: {shootMaxDistanceCount}\n" +
                                    $"    - Flyhack Attempts: {flyhackCount}";
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

        private void TrackPlayerPing()
        {
            foreach (BasePlayer basePlayer in BasePlayer.activePlayerList)
            {
                IPlayer player = covalence.Players.FindPlayerById(basePlayer.UserIDString);

                if (player != null)
                {
                    int currentPing = player.Ping;

                    if (_config.debugMode)
                    {
                        Puts($"{player.Name} - {currentPing}ms");
                    }

                    if (!playerPingHistory.TryGetValue(player.Id, out List<int> history))
                    {
                        history = new List<int>();
                        playerPingHistory.Add(player.Id, history);
                    }

                    history.Add(currentPing);
                    if (history.Count > 60) history.RemoveAt(0);
                    if (IsPingSuspicious(history))
                    // if (_config.debugMode)
                    {
                        string discordMessage = $"- **Player:** `{player.Name}`\n" +
                                                $"- **Steam ID:** [{player.Id}](https://steamcommunity.com/profiles/{player.Id})\n" +
                                                $"- **Reason:** `Lagswitching Detected! `\n" +
                                                $"- **Position:** `{player.Position()}`\n" +
                                                $"- **Time:** <t:{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}> (<t:{new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}:R>)\n" +
                                                $"- **Server:** `{ConVar.Server.hostname}`\n" +
                                                $"- **Ping Data:**\n" +
                                                $"    - Current Ping: {player.Ping}ms\n" +
                                                $"    - Last 5 Pings: {string.Join(", ", history.Skip(Math.Max(0, history.Count - 5)))}";

                        SendDiscordMessage(discordMessage, null);
                    }


                }
            }
        }


        #endregion

        #region Dependencies 

        private bool IsPingSuspicious(List<int> history)
        {
            if (history.Count < 10) return false;

            int latestPing = history[history.Count - 1];
            int averagePing = 0;
            history.ForEach(p => averagePing += p);
            averagePing /= history.Count;

            if (latestPing > averagePing * 3 && latestPing > 200)
            {
                return true;
            }
            return false;
        }
        private bool ShouldReceiveAlerts(BasePlayer player)
        {
            return AdminPreferences.TryGetValue(player.UserIDString, out bool receiveAlerts) && receiveAlerts;
        }

        private void SendDiscordMessage(string content, string imageLink = null)
        {
            if (DateTime.UtcNow - lastMessageSent < messageCooldown)
            {
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
        private void AddViolation(Dictionary<BasePlayer, ViolationCount> cache, BasePlayer player)
        {
            if (cache.TryGetValue(player, out ViolationCount existingViolation))
            {
                existingViolation.Count++;
                existingViolation.TimeOfLastViolation = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            else
            {
                cache[player] = new ViolationCount
                {
                    Count = 1,
                    TimeOfLastViolation = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
            }
        }



        #endregion
    }
}


