using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("MyPlugin", "<author>", "1.0.0")]
    [Description("<optional description>")]
    internal sealed class MyPlugin : RustPlugin
    {
        #region Fields

        private Configuration _config;

        private Data _data;
        private DynamicConfigFile _dataFile;

        #endregion

        #region Permission

        private static class Permission
        {
            public const string Admin = "myplugin.admin";
        }

        #endregion

        #region Configuration

        private sealed class Configuration
        {
            [JsonProperty(PropertyName = "Default Chat Avatar (steamId)")]
            public ulong ChatAvatarId { get; set; }
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                ChatAvatarId = 0,
            };
        }

        #region Override

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();
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

        #endregion
        #endregion

        #region Data

        private sealed class Data
        {
            [JsonProperty(PropertyName = "Players Data")]
            public Dictionary<ulong, PlayerData> Players { get; set; } = new Dictionary<ulong, PlayerData>();
        }

        private sealed class PlayerData
        {
            [JsonProperty("Last connection time")]
            public DateTime LastConnectionTime { get; set; }

            [JsonProperty("Number of death")]
            public int NumberOfDeath { get; set; }
        }

        private void CreatePlayerData(ulong playerId, DateTime lastConnectionTime, int numberOfDeath = 0)
        {
            _data.Players[playerId] = new PlayerData
            {
                LastConnectionTime = lastConnectionTime,
                NumberOfDeath = numberOfDeath,
            };
            SaveData();
        }

        private PlayerData? GetPlayerData(ulong playerId)
        {
            _data.Players.TryGetValue(playerId, out var p);
            return p;
        }

        private void LoadData()
        {
            _dataFile = Interface.Oxide.DataFileSystem.GetFile(Name);
            _data = _dataFile.ReadObject<Data>() ?? new Data();
        }

        private void SaveData()
        {
            _dataFile.WriteObject(_data);
        }

        private void ClearData()
        {
            _data = new Data();
            SaveData();
        }

        #endregion

        #region Oxide Hooks

        void Init()
        {
            permission.RegisterPermission(Permission.Admin, this);
            LoadData();
        }

        void OnServerInitialized()
        {
            AddCovalenceCommand(Command.Prefix, nameof(SendWipeCommand));
        }

        void Loaded()
        {
            if (SomePluginNameToReference == null)
            {
                PrintWarning(GetMessage(MessageKey.PluginMissing));
            }
        }

        void UnLoad()
        {
            SaveData();
        }

        void OnServerSave()
        {
            SaveData();
        }

        void OnNewSave(string fileName)
        {
            ClearData();
        }

        void OnPlayerConnected(BasePlayer player)
        {
            var playerData = GetPlayerData(player.userID);
            if (playerData != null) playerData.LastConnectionTime = DateTime.Now;
            else CreatePlayerData(player.userID, DateTime.Now);
        }

        object? OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            var playerData = GetPlayerData(player.userID);
            if (playerData != null) playerData.NumberOfDeath += 1;
            else CreatePlayerData(player.userID, DateTime.Now, 1);
            return null;
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var playerData = GetPlayerData(player.userID);
            if (playerData != null) playerData.LastConnectionTime = DateTime.Now;
            else CreatePlayerData(player.userID, DateTime.Now, 0);
        }

        #endregion

        #region Helper

        private void SendChatMessage(string message, BasePlayer? player = null)
        {
            if (player != null) Player.Message(player, message, _config.ChatAvatarId);
            else Puts(message);
        }

        private void SendGlobalChatMessage(string message)
        {
            Puts(message);
            Server.Broadcast(message, _config.ChatAvatarId);
        }

        private bool HasPermission(IPlayer player, string perm)
            => permission.UserHasPermission(player.Id, perm);

        private string StripRichText(string? message)
        {
            if (message == null) return string.Empty;

            var stringReplacements = new string[]
            {
                "<b>", "</b>",
                "<i>", "</i>",
                "</size>",
                "</color>"
            };

            var regexReplacements = new[]
            {
                new Regex(@"<color=.+?>"),
                new Regex(@"<size=.+?>"),
            };

            foreach (var replacement in stringReplacements)
            {
                message = message.Replace(replacement, string.Empty);
            }

            foreach (var replacement in regexReplacements)
            {
                message = replacement.Replace(message, string.Empty);
            }

            return Formatter.ToPlaintext(message);
        }

        #endregion

        #region Commands

        private static class Command
        {
            public const string Prefix = "myplugin";
            public const string Help = "help";
        }

        private bool SendWipeCommand(IPlayer player, string cmd, string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));
            if (!HasPermission(player, Permission.Admin))
            {
                player.Message(GetMessage(MessageKey.NoPermission));
                return true;
            }
            if (args.Length == 0)
            {
                player.Message(GetMessage(MessageKey.HelpMessage));
                return true;
            }
            switch (args[0])
            {
                case Command.Help:
                    player.Message(GetMessage(MessageKey.HelpMessage));
                    break;
                default:
                    player.Message(GetMessage(MessageKey.UnknownCommand));
                    break;
            }
            return true;
        }

        #endregion

        #region Localization

        private static class MessageKey
        {
            public const string HelpMessage = "HelpMessage";
            public const string NoPermission = "NoPermission";
            public const string PluginMissing = "PluginMissing";
            public const string UnknownCommand = "UnknownCommand";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [MessageKey.PluginMissing] = "The plugin \"SomePlugin\" was not found. Check on UMod: https://umod.org/plugins/someplugin",
                [MessageKey.NoPermission] = "You are not allowed to run this command!",
                [MessageKey.HelpMessage] = "Some useful help!",
                [MessageKey.UnknownCommand] = "Unknown command!",
            }, this);
        }

        private string GetMessage(string messageKey, string? playerId = null)
        {
            return lang.GetMessage(messageKey, this, playerId);
        }

        private string GetCustomMessage(string messageKey, string? playerId = null, params object[] data)
        {
            try
            {
                var template = lang.GetMessage(messageKey, this, playerId);
                return string.Format(template, data);
            }
            catch (Exception exception)
            {
                PrintError(exception.ToString());
                throw;
            }
        }

        #endregion

        #region API

        #region Internal API

        [HookMethod("GetLastConnectionTime")]
        public DateTime? GetLastConnectionTime(ulong userId)
            => GetPlayerData(userId)?.LastConnectionTime;

        [HookMethod("GetNumberOfDeath")]
        public int? GetNumberOfDeath(ulong userId)
            => GetPlayerData(userId)?.NumberOfDeath;

        #endregion

        #region External API

        [PluginReference]
        Plugin? SomePluginNameToReference;

        #endregion
        #endregion
    }
}