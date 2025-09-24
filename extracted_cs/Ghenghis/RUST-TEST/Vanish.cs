using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using Carbon.Core;
using Carbon.Extensions;
using Carbon.Plugins;

namespace Oxide.Plugins
{
    [Info("Vanish", "Wulf/lukespragg", "1.0.0")]
    [Description("Allows players with permission to become invisible")]
    public class Vanish : RustPlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {
            public bool EnableSound { get; set; } = true;
            public bool ShowNotifications { get; set; } = true;
            public float CooldownTime { get; set; } = 60f;
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
                ["NoPermission"] = "You don't have permission to use this command!",
                ["VanishEnabled"] = "Vanish mode enabled!",
                ["VanishDisabled"] = "Vanish mode disabled!",
                ["PlayerNotFound"] = "Player not found!",
                ["AlreadyVanished"] = "You are already vanished!",
                ["NotVanished"] = "You are not vanished!"
            }, this);
        }

        private string GetMessage(string key, string playerId = null) => lang.GetMessage(key, this, playerId);

        #endregion

        #region Fields

        private readonly Dictionary<ulong, DateTime> cooldowns = new Dictionary<ulong, DateTime>();
        private readonly HashSet<ulong> vanishedPlayers = new HashSet<ulong>();

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission("vanish.use", this);
            permission.RegisterPermission("vanish.admin", this);
            
            AddCovalenceCommand("vanish", "VanishCommand");
        }

        private object OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            if (vanishedPlayers.Contains(attacker.userID))
            {
                return false;
            }
            return null;
        }

        private object CanNetworkTo(BaseNetworkable entity, BasePlayer target)
        {
            if (entity is BasePlayer player && vanishedPlayers.Contains(player.userID))
            {
                if (!permission.UserHasPermission(target.UserIDString, "vanish.admin"))
                {
                    return false;
                }
            }
            return null;
        }

        #endregion

        #region Commands

        [Command("vanish")]
        private void VanishCommand(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, "vanish.use"))
            {
                player.Reply(GetMessage("NoPermission", player.Id));
                return;
            }

            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            if (args.Length > 0 && permission.UserHasPermission(player.Id, "vanish.admin"))
            {
                var targetPlayer = covalence.Players.FindPlayer(args[0]);
                if (targetPlayer == null)
                {
                    player.Reply(GetMessage("PlayerNotFound", player.Id));
                    return;
                }
                ToggleVanish(targetPlayer.Object as BasePlayer, player);
            }
            else
            {
                ToggleVanish(basePlayer, player);
            }
        }

        #endregion

        #region Core Methods

        private void ToggleVanish(BasePlayer player, IPlayer commander = null)
        {
            if (player == null) return;

            var playerId = player.userID;
            var commanderId = commander?.Id ?? player.UserIDString;

            if (vanishedPlayers.Contains(playerId))
            {
                DisableVanish(player);
                var message = GetMessage("VanishDisabled", commanderId);
                if (commander != null)
                    commander.Reply(message);
                else
                    player.ChatMessage(message);
            }
            else
            {
                EnableVanish(player);
                var message = GetMessage("VanishEnabled", commanderId);
                if (commander != null)
                    commander.Reply(message);
                else
                    player.ChatMessage(message);
            }
        }

        private void EnableVanish(BasePlayer player)
        {
            if (player == null) return;

            vanishedPlayers.Add(player.userID);
            player.limitNetworking = true;
            player.SendNetworkUpdateImmediate();

            // Hide from other players
            foreach (var target in BasePlayer.activePlayerList)
            {
                if (target != player && !permission.UserHasPermission(target.UserIDString, "vanish.admin"))
                {
                    target.OnNetworkSubscribersLeave(new List<Network.Connection> { player.net.connection });
                }
            }
        }

        private void DisableVanish(BasePlayer player)
        {
            if (player == null) return;

            vanishedPlayers.Remove(player.userID);
            player.limitNetworking = false;
            player.SendNetworkUpdateImmediate();

            // Show to other players
            foreach (var target in BasePlayer.activePlayerList)
            {
                if (target != player)
                {
                    target.OnNetworkSubscribersEnter(new List<Network.Connection> { player.net.connection });
                }
            }
        }

        private bool IsVanished(BasePlayer player)
        {
            return player != null && vanishedPlayers.Contains(player.userID);
        }

        #endregion

        #region API

        private bool IsInvisible(BasePlayer player)
        {
            return IsVanished(player);
        }

        private void Disappear(BasePlayer player)
        {
            if (player != null && !IsVanished(player))
            {
                EnableVanish(player);
            }
        }

        private void Reappear(BasePlayer player)
        {
            if (player != null && IsVanished(player))
            {
                DisableVanish(player);
            }
        }

        #endregion
    }
}

