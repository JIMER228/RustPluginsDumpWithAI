/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Glowing Eyes", "VisEntities", "1.0.0")]
    [Description("Gives players glowing eyes they can toggle whenever they want.")]
    public class GlowingEyes : RustPlugin
    {
        #region Fields

        private static GlowingEyes _plugin;
        private static Configuration _config;

        private const string EYES_SHORTNAME = "gloweyes";
        private readonly HashSet<ulong> _playersWithEyesEnabled = new HashSet<ulong>();

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Chat Command")]
            public string ChatCommand { get; set; }

            [JsonProperty("Auto Enable On Connect")]
            public bool AutoEnableOnConnect { get; set; }
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
                ChatCommand = "eye",
                AutoEnableOnConnect = false
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
            cmd.AddChatCommand(_config.ChatCommand, this, nameof(cmdToggleEyes));
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player == null)
                    continue;

                if (_playersWithEyesEnabled.Contains(player.userID))
                    RemoveEyes(player);
            }
            _playersWithEyesEnabled.Clear();

            _config = null;
            _plugin = null;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (_config.AutoEnableOnConnect && PermissionUtil.HasPermission(player, PermissionUtil.USE))
                EnableEyes(player, true);
        }

        private void OnPlayerDeath(BasePlayer player, HitInfo deathInfo)
        {
            if (player == null || deathInfo == null)
                return;

            if (_playersWithEyesEnabled.Contains(player.userID))
                RemoveEyes(player);
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player == null)
                return;

            if (_playersWithEyesEnabled.Contains(player.userID))
                EnableEyes(player, true);
        }

        #endregion Oxide Hooks

        #region Core

        private void EnableEyes(BasePlayer player, bool silent = false)
        {
            if (HasEyes(player))
                return;

            var item = ItemManager.CreateByName(EYES_SHORTNAME);
            item.MoveToContainer(player.inventory.containerWear, 100);
            _playersWithEyesEnabled.Add(player.userID);

            if (!silent)
                MessagePlayer(player, Lang.EnabledEyes);
        }

        private void DisableEyes(BasePlayer player, bool silent = false)
        {
            RemoveEyes(player);
            _playersWithEyesEnabled.Remove(player.userID);

            if (!silent)
                MessagePlayer(player, Lang.DisabledEyes);
        }

        private static void RemoveEyes(BasePlayer player)
        {
            if (player == null)
                return;

            var wearContainer = player.inventory.containerWear;
            if (wearContainer == null)
                return;

            List<Item> wear = wearContainer.itemList;
            if (wear == null)
                return;

            for (int i = wear.Count - 1; i >= 0; i--)
            {
                Item item = wear[i];
                if (item.info.shortname != EYES_SHORTNAME)
                    continue;

                item.RemoveFromContainer();

                var heldEntity = item.GetHeldEntity();
                if (heldEntity != null)
                    heldEntity.Kill();

                item.DoRemove();
            }
        }

        private static bool HasEyes(BasePlayer player)
        {
            if (player == null)
                return false;

            var wearContainer = player.inventory.containerWear;
            if (wearContainer == null)
                return false;

            List<Item> wear = wearContainer.itemList;
            if (wear == null)
                return false;

            foreach (Item item in wear)
            {
                if (item.info.shortname == EYES_SHORTNAME)
                    return true;
            }

            return false;
        }

        #endregion Core

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "glowingeyes.use";
            private static readonly List<string> _permissions = new List<string>
            {
                USE,
            };

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

        #region Commands

        private void cmdToggleEyes(BasePlayer player, string command, string[] args)
        {
            if (!PermissionUtil.HasPermission(player, PermissionUtil.USE))
            {
                MessagePlayer(player, Lang.NoPermission);
                return;
            }

            if (HasEyes(player))
                DisableEyes(player);
            else
                EnableEyes(player);
        }

        #endregion Commands

        #region Localization

        private class Lang
        {
            public const string NoPermission = "NoPermission";
            public const string EnabledEyes = "EnabledEyes";
            public const string DisabledEyes = "DisabledEyes";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.NoPermission] = "You do not have permission to use this command.",
                [Lang.EnabledEyes] = "Glowing eyes: enabled.",
                [Lang.DisabledEyes] = "Glowing eyes: disabled.",

            }, this, "en");
        }

        private static string GetMessage(BasePlayer player, string messageKey, params object[] args)
        {
            string message = _plugin.lang.GetMessage(messageKey, _plugin, player.UserIDString);

            if (args.Length > 0)
                message = string.Format(message, args);

            return message;
        }

        public static void MessagePlayer(BasePlayer player, string messageKey, params object[] args)
        {
            string message = GetMessage(player, messageKey, args);

            if (!string.IsNullOrWhiteSpace(message))
                _plugin.SendReply(player, message);
        }

        #endregion Localization
    }
}