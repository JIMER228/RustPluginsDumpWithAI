// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Carbon.Core;
using Carbon.Extensions;
using Carbon.Plugins;
using Oxide.Core.Libraries;
using Rust;

namespace Oxide.Plugins
{
    [Info("AdminPanel", "austinv900", "1.3.0")]
    [Description("Enhanced admin panel with improved functionality and error handling")]
    public class AdminPanel : RustPlugin
    {
        #region Plugin References
        
        [PluginReference] 
        private Plugin Vanish;

        [PluginReference] 
        private Plugin Godmode;

        [PluginReference] 
        private Plugin AdminRadar;

        [PluginReference] 
        private Plugin NTeleportation;

        [PluginReference] 
        private Plugin EnhancedBanSystem;

        #endregion

        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Admin Permission")]
            public string AdminPermission { get; set; } = "adminpanel.allowed";

            [JsonProperty("Panel Settings")]
            public PanelSettings Panel { get; set; } = new PanelSettings();

            [JsonProperty("Auto-close Timer")]
            public float AutoCloseTimer { get; set; } = 300f;

            [JsonProperty("Enable Logging")]
            public bool EnableLogging { get; set; } = true;
        }

        public class PanelSettings
        {
            [JsonProperty("Background Color")]
            public string BackgroundColor { get; set; } = "0.1 0.1 0.1 0.95";

            [JsonProperty("Button Color")]
            public string ButtonColor { get; set; } = "0.2 0.6 0.2 0.8";

            [JsonProperty("Text Color")]
            public string TextColor { get; set; } = "1 1 1 1";
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }
            }
            catch
            {
                LogWarning("Configuration file is corrupt, using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region Localization

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You don't have permission to use the admin panel!",
                ["PanelOpened"] = "Admin panel opened successfully",
                ["PanelClosed"] = "Admin panel closed",
                ["PlayerNotFound"] = "Player not found: {0}",
                ["ActionCompleted"] = "Action completed successfully",
                ["InvalidCommand"] = "Invalid command or parameters",
                ["TeleportSuccess"] = "Teleported to {0}",
                ["KickSuccess"] = "Player {0} has been kicked",
                ["BanSuccess"] = "Player {0} has been banned",
                ["GodmodeEnabled"] = "Godmode enabled for {0}",
                ["GodmodeDisabled"] = "Godmode disabled for {0}",
                ["VanishEnabled"] = "Vanish enabled for {0}",
                ["VanishDisabled"] = "Vanish disabled for {0}"
            }, this);
        }

        private string GetMessage(string key, string playerId = null, params object[] args)
        {
            return string.Format(lang.GetMessage(key, this, playerId), args);
        }

        #endregion

        #region Fields

        private readonly Dictionary<ulong, Timer> autoCloseTimers = new Dictionary<ulong, Timer>();
        private readonly HashSet<ulong> activePanels = new HashSet<ulong>();

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(config.AdminPermission, this);
            AddCovalenceCommand("adminpanel", "AdminPanelCommand");
            AddCovalenceCommand("ap", "AdminPanelCommand");

            if (config.EnableLogging)
            {
                Puts("AdminPanel Enhanced v2.0.0 initialized successfully");
            }
        }

        private void Unload()
        {
            foreach (var timer in autoCloseTimers.Values)
            {
                timer?.Destroy();
            }
            autoCloseTimers.Clear();

            foreach (var playerId in activePanels.ToList())
            {
                var player = BasePlayer.FindByID(playerId);
                if (player != null)
                {
                    CuiHelper.DestroyUi(player, "AdminPanel");
                }
            }
            activePanels.Clear();
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null) return;

            CleanupPlayerData(player.userID);
        }

        #endregion

        #region Commands

        [Command("adminpanel", "ap")]
        private void AdminPanelCommand(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, config.AdminPermission))
            {
                player.Reply(GetMessage("NoPermission", player.Id));
                return;
            }

            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            if (args.Length > 0)
            {
                HandleAdminAction(basePlayer, args);
            }
            else
            {
                ToggleAdminPanel(basePlayer);
            }
        }

        #endregion

        #region Core Methods

        private void ToggleAdminPanel(BasePlayer player)
        {
            if (activePanels.Contains(player.userID))
            {
                CloseAdminPanel(player);
            }
            else
            {
                OpenAdminPanel(player);
            }
        }

        private void OpenAdminPanel(BasePlayer player)
        {
            if (player == null) return;

            CloseAdminPanel(player); // Ensure clean state

            var container = CreateAdminPanelUI(player);
            CuiHelper.AddUi(player, container);

            activePanels.Add(player.userID);
            SetupAutoClose(player);

            if (config.EnableLogging)
            {
                Puts($"Admin panel opened by {player.displayName} ({player.UserIDString})");
            }

            player.ChatMessage(GetMessage("PanelOpened", player.UserIDString));
        }

        private void CloseAdminPanel(BasePlayer player)
        {
            if (player == null) return;

            CuiHelper.DestroyUi(player, "AdminPanel");
            CleanupPlayerData(player.userID);

            player.ChatMessage(GetMessage("PanelClosed", player.UserIDString));
        }

        private void CleanupPlayerData(ulong playerId)
        {
            activePanels.Remove(playerId);
            
            if (autoCloseTimers.TryGetValue(playerId, out var timer))
            {
                timer?.Destroy();
                autoCloseTimers.Remove(playerId);
            }
        }

        private void SetupAutoClose(BasePlayer player)
        {
            if (config.AutoCloseTimer <= 0) return;

            if (autoCloseTimers.TryGetValue(player.userID, out var existingTimer))
            {
                existingTimer?.Destroy();
            }

            autoCloseTimers[player.userID] = timer.Once(config.AutoCloseTimer, () =>
            {
                if (player != null && player.IsConnected)
                {
                    CloseAdminPanel(player);
                }
            });
        }

        private CuiElementContainer CreateAdminPanelUI(BasePlayer player)
        {
            var container = new CuiElementContainer();

            // Main panel
            container.Add(new CuiPanel
            {
                Image = { Color = config.Panel.BackgroundColor },
                RectTransform = { AnchorMin = "0.2 0.2", AnchorMax = "0.8 0.8" },
                CursorEnabled = true
            }, "Overlay", "AdminPanel");

            // Title
            container.Add(new CuiLabel
            {
                Text = { Text = "Admin Panel - Enhanced", FontSize = 20, Align = TextAnchor.MiddleCenter, Color = config.Panel.TextColor },
                RectTransform = { AnchorMin = "0 0.9", AnchorMax = "1 1" }
            }, "AdminPanel");

            // Close button
            container.Add(new CuiButton
            {
                Button = { Command = "adminpanel.close", Color = "0.8 0.2 0.2 0.8" },
                RectTransform = { AnchorMin = "0.9 0.9", AnchorMax = "1 1" },
                Text = { Text = "X", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = config.Panel.TextColor }
            }, "AdminPanel");

            // Action buttons
            var buttonData = new[]
            {
                new { Text = "Player List", Command = "adminpanel.players", Y = 0.75f },
                new { Text = "Teleport", Command = "adminpanel.teleport", Y = 0.65f },
                new { Text = "Godmode", Command = "adminpanel.godmode", Y = 0.55f },
                new { Text = "Vanish", Command = "adminpanel.vanish", Y = 0.45f },
                new { Text = "Admin Radar", Command = "adminpanel.radar", Y = 0.35f },
                new { Text = "Server Info", Command = "adminpanel.serverinfo", Y = 0.25f }
            };

            foreach (var button in buttonData)
            {
                container.Add(new CuiButton
                {
                    Button = { Command = button.Command, Color = config.Panel.ButtonColor },
                    RectTransform = { AnchorMin = $"0.1 {button.Y - 0.05f}", AnchorMax = $"0.9 {button.Y + 0.05f}" },
                    Text = { Text = button.Text, FontSize = 14, Align = TextAnchor.MiddleCenter, Color = config.Panel.TextColor }
                }, "AdminPanel");
            }

            return container;
        }

        private void HandleAdminAction(BasePlayer player, string[] args)
        {
            if (args.Length == 0) return;

            var action = args[0].ToLower();
            
            try
            {
                switch (action)
                {
                    case "close":
                        CloseAdminPanel(player);
                        break;
                    case "players":
                        ShowPlayerList(player);
                        break;
                    case "teleport":
                        HandleTeleport(player, args);
                        break;
                    case "godmode":
                        ToggleGodmode(player, args);
                        break;
                    case "vanish":
                        ToggleVanish(player, args);
                        break;
                    case "radar":
                        ToggleRadar(player);
                        break;
                    case "serverinfo":
                        ShowServerInfo(player);
                        break;
                    default:
                        player.ChatMessage(GetMessage("InvalidCommand", player.UserIDString));
                        break;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error handling admin action '{action}': {ex.Message}");
                player.ChatMessage(GetMessage("InvalidCommand", player.UserIDString));
            }
        }

        private void ShowPlayerList(BasePlayer admin)
        {
            var players = BasePlayer.activePlayerList;
            var message = $"Online Players ({players.Count}):\n";
            
            foreach (var player in players.Take(10)) // Limit to prevent spam
            {
                message += $"• {player.displayName} ({player.UserIDString})\n";
            }
            
            if (players.Count > 10)
            {
                message += $"... and {players.Count - 10} more players";
            }

            admin.ChatMessage(message);
        }

        private void HandleTeleport(BasePlayer admin, string[] args)
        {
            if (args.Length < 2)
            {
                admin.ChatMessage("Usage: /ap teleport <player>");
                return;
            }

            var targetPlayer = FindPlayer(args[1]);
            if (targetPlayer == null)
            {
                admin.ChatMessage(GetMessage("PlayerNotFound", admin.UserIDString, args[1]));
                return;
            }

            admin.Teleport(targetPlayer.transform.position);
            admin.ChatMessage(GetMessage("TeleportSuccess", admin.UserIDString, targetPlayer.displayName));
        }

        private void ToggleGodmode(BasePlayer admin, string[] args)
        {
            var target = args.Length > 1 ? FindPlayer(args[1]) : admin;
            if (target == null)
            {
                admin.ChatMessage(GetMessage("PlayerNotFound", admin.UserIDString, args[1]));
                return;
            }

            if (Godmode != null)
            {
                Godmode.Call("ToggleGodmode", target);
                admin.ChatMessage(GetMessage("ActionCompleted", admin.UserIDString));
            }
            else
            {
                // Fallback godmode implementation
                target.metabolism.bleeding.value = 0f;
                target.health = target.MaxHealth();
                admin.ChatMessage($"Basic godmode applied to {target.displayName}");
            }
        }

        private void ToggleVanish(BasePlayer admin, string[] args)
        {
            var target = args.Length > 1 ? FindPlayer(args[1]) : admin;
            if (target == null)
            {
                admin.ChatMessage(GetMessage("PlayerNotFound", admin.UserIDString, args[1]));
                return;
            }

            if (Vanish != null)
            {
                Vanish.Call("Disappear", target);
                admin.ChatMessage(GetMessage("VanishEnabled", admin.UserIDString, target.displayName));
            }
            else
            {
                admin.ChatMessage("Vanish plugin not available");
            }
        }

        private void ToggleRadar(BasePlayer admin)
        {
            if (AdminRadar != null)
            {
                AdminRadar.Call("ToggleRadar", admin);
                admin.ChatMessage(GetMessage("ActionCompleted", admin.UserIDString));
            }
            else
            {
                admin.ChatMessage("AdminRadar plugin not available");
            }
        }

        private void ShowServerInfo(BasePlayer admin)
        {
            var timeRemaining = UnityEngine.Time.realtimeSinceStartup;
            var info = $"Server Information:\n" +
                      $"• Players Online: {BasePlayer.activePlayerList.Count}/{ConVar.Server.maxplayers}\n" +
                      $"• Server FPS: {Performance.current.frameRate:F1}\n" +
                      $"• Memory Usage: {Performance.current.memoryUsageSystem:F1} MB\n" +
                      $"• Uptime: {timeRemaining / 3600:F1} hours\n" +
                      $"• Map: {ConVar.Server.level}\n" +
                      $"• Seed: {ConVar.Server.seed}";

            admin.ChatMessage(info);
        }

        private BasePlayer FindPlayer(string nameOrId)
        {
            if (string.IsNullOrEmpty(nameOrId)) return null;

            // Try by ID first
            if (ulong.TryParse(nameOrId, out var userId))
            {
                return BasePlayer.FindByID(userId);
            }

            // Try by name
            var players = BasePlayer.activePlayerList
                .Where(p => p.displayName.ToLower().Contains(nameOrId.ToLower()))
                .ToList();

            return players.Count == 1 ? players[0] : null;
        }

        #endregion

        #region Console Commands

        [ConsoleCommand("adminpanel.close")]
        private void CloseCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null)
            {
                CloseAdminPanel(player);
            }
        }

        [ConsoleCommand("adminpanel.players")]
        private void PlayersCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && permission.UserHasPermission(player.UserIDString, config.AdminPermission))
            {
                ShowPlayerList(player);
            }
        }

        [ConsoleCommand("adminpanel.teleport")]
        private void TeleportCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && permission.UserHasPermission(player.UserIDString, config.AdminPermission))
            {
                HandleTeleport(player, new[] { "teleport", arg.GetString(0, "") });
            }
        }

        [ConsoleCommand("adminpanel.godmode")]
        private void GodmodeCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && permission.UserHasPermission(player.UserIDString, config.AdminPermission))
            {
                ToggleGodmode(player, new[] { "godmode" });
            }
        }

        [ConsoleCommand("adminpanel.vanish")]
        private void VanishCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && permission.UserHasPermission(player.UserIDString, config.AdminPermission))
            {
                ToggleVanish(player, new[] { "vanish" });
            }
        }

        [ConsoleCommand("adminpanel.radar")]
        private void RadarCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && permission.UserHasPermission(player.UserIDString, config.AdminPermission))
            {
                ToggleRadar(player);
            }
        }

        [ConsoleCommand("adminpanel.serverinfo")]
        private void ServerInfoCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player != null && permission.UserHasPermission(player.UserIDString, config.AdminPermission))
            {
                ShowServerInfo(player);
            }
        }

        #endregion

        #region API

        private void OpenPanelForPlayer(BasePlayer player)
        {
            if (player != null && permission.UserHasPermission(player.UserIDString, config.AdminPermission))
            {
                OpenAdminPanel(player);
            }
        }

        private void ClosePanelForPlayer(BasePlayer player)
        {
            if (player != null)
            {
                CloseAdminPanel(player);
            }
        }

        private bool IsPlayerUsingPanel(BasePlayer player)
        {
            return player != null && activePanels.Contains(player.userID);
        }

        #endregion
    }
}
