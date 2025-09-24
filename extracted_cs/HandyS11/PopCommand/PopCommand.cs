using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ConVar;

namespace Oxide.Plugins
{
    [Info("PopCommand", "HandyS11", "1.0.2")]
    [Description("Displays the population information of your server")]
    public class PopCommand : RustPlugin
    {
        #region Configuration

        private Configuration _configuration;

        private sealed class Configuration
        {
            [JsonProperty(PropertyName = "Use permission for lambda players")]
            public bool UsePermissionForPlayers { get; set; }

            [JsonProperty(PropertyName = "Broadcast to every player on /pop")]
            public bool DoBroadcast { get; set; }

            [JsonProperty(PropertyName = "Chat default avatar")]
            public ulong ChatAvatar { get; set; }

            [JsonProperty(PropertyName = "Display Options")]
            public DisplayOptions DisplayOption { get; set; }
        }

        public class DisplayOptions
        {
            [JsonProperty(PropertyName = "Show player count")]
            public bool ShowPlayerCount { get; set; }

            [JsonProperty(PropertyName = "Show server slots")]
            public bool ShowServerSlots { get; set; }

            [JsonProperty(PropertyName = "Show sleepers")]
            public bool ShowSleepers { get; set; }

            [JsonProperty(PropertyName = "Show joining players")]
            public bool ShowJoiningPlayers { get; set; }

            [JsonProperty(PropertyName = "Show players in queue")]
            public bool ShowPlayersInQueue { get; set; }
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                UsePermissionForPlayers = false,
                DoBroadcast = false,
                ChatAvatar = 0,
                DisplayOption = new DisplayOptions
                {
                    ShowPlayerCount = true,
                    ShowServerSlots = true,
                    ShowSleepers = false,
                    ShowJoiningPlayers = false,
                    ShowPlayersInQueue = false,
                },
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _configuration = Config.ReadObject<Configuration>();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _configuration = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_configuration, true);
        }

        #endregion

        #region Permissions

        private static class Permission
        {
            public const string PopCommand = "popcommand.pop";
            public const string PopCommandAdmin = "popcommand.apop";
        }

        # endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(Permission.PopCommand, this);
            permission.RegisterPermission(Permission.PopCommandAdmin, this);
        }

        private void OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            switch (message)
            {
                case "!pop":
                    PopPlayer(player, "pop");
                    break;
                case "!apop":
                    PopAdmin(player, "apop");
                    break;
            }
        }

        #endregion

        #region Functions

        private void SendChatMessage(BasePlayer player, string message)
        {
            Player.Message(player, message, _configuration.ChatAvatar);
        }

        private void SendMessageToAll(string message)
        {
            Server.Broadcast(message, _configuration.ChatAvatar);
        }

        private bool HasPermission(BasePlayer player, string permissionName)
        {
            return permission.UserHasPermission(player.UserIDString, permissionName);
        }

        private IEnumerable<int> GetData()
        {
            var data = new List<int>();

            if (_configuration.DisplayOption.ShowPlayerCount)
                data.Add(BasePlayer.activePlayerList.Count);
            if (_configuration.DisplayOption.ShowServerSlots)
                data.Add(ConVar.Server.maxplayers);
            if (_configuration.DisplayOption.ShowSleepers)
                data.Add(BasePlayer.sleepingPlayerList.Count);
            if (_configuration.DisplayOption.ShowJoiningPlayers)
                data.Add(ServerMgr.Instance.connectionQueue.joining.Count);
            if (_configuration.DisplayOption.ShowPlayersInQueue)
                data.Add(ServerMgr.Instance.connectionQueue.queue.Count);

            return data;
        }

        private bool VerifyPlaceholders(string template, int expectedCount)
        {
            var actualCount = Regex.Matches(template, @"\{\d+\}").Count;
            return actualCount <= expectedCount;
        }

        #endregion

        #region Command

        private static class Command
        {
            public const string PopPlayer = "pop";
            public const string PopAdmin = "apop";
        }

        [ChatCommand(Command.PopPlayer)]
        private void PopPlayer(BasePlayer player, string command, string[] args = null)
        {
            if (_configuration.UsePermissionForPlayers && !HasPermission(player, Permission.PopCommand))
            {
                SendChatMessage(player, GetMessage(MessageKey.PopPermissionDeny, player.UserIDString));
                return;
            }

            if (_configuration.DoBroadcast)
            {
                SendMessageToAll(GetMessage(MessageKey.PopMessage, player.UserIDString, GetData().Cast<object>().ToArray()));
                return;
            }
            SendChatMessage(player, GetMessage(MessageKey.PopMessage, player.UserIDString, GetData().Cast<object>().ToArray()));
        }

        [ChatCommand(Command.PopAdmin)]
        private void PopAdmin(BasePlayer player, string command, string[] args = null)
        {
            if (!player.IsAdmin || !HasPermission(player, Permission.PopCommandAdmin))
            {
                SendChatMessage(player, GetMessage(MessageKey.PopAdminPermissionDeny, player.UserIDString));
                return;
            }

            SendChatMessage(player, GetMessage(MessageKey.PopMessageAdmin, player.UserIDString,
                BasePlayer.activePlayerList.Count,
                ConVar.Server.maxplayers,
                BasePlayer.sleepingPlayerList.Count,
                ServerMgr.Instance.connectionQueue.joining.Count,
                ServerMgr.Instance.connectionQueue.queue.Count
            ));
        }

        #endregion

        #region Localization

        private static class MessageKey
        {
            public const string PopMessage = "PopCommand.ChatMessage";
            public const string PopMessageAdmin = "PopCommand.AdminMessage";
            public const string PopPermissionDeny = "PopCommand.PermissionDeny";
            public const string PopAdminPermissionDeny = "PopCommand.AdminPermissionDeny";
            public const string PopError = "PopCommand.Error";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [MessageKey.PopMessage] = "Players online: <color=orange>{0}</color>/<color=orange>{1}</color>",
                [MessageKey.PopMessageAdmin] = "Players online: <color=orange>{0}</color>/<color=orange>{1}</color> | Sleeping: <color=orange>{2}</color> | Joining: <color=orange>{3}</color> | Queued: <color=orange>{4}</color>",
                [MessageKey.PopPermissionDeny] = "You are not allowed to run this command!",
                [MessageKey.PopAdminPermissionDeny] = "Only administrators can run this command!",
                [MessageKey.PopError] = "The config/lang file contains some errors!",
            }, this);
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [MessageKey.PopMessage] = "Joueurs en ligne : <color=green>{0}</color>/<color=red>{1}</color>",
                [MessageKey.PopMessageAdmin] = "Joueurs en ligne : <color=orange>{0}</color>/<color=orange>{1}</color> | Endormi : <color=orange>{2}</color> | En train de rejoindre : <color=orange>{3}</color> | Dans la queue : <color=orange>{4}</color>",
                [MessageKey.PopPermissionDeny] = "Vous n'êtes pas autorisé à utiliser cette commande !",
                [MessageKey.PopAdminPermissionDeny] = "Seul les administrateurs peuvent utiliser cette commande !",
                [MessageKey.PopError] = "Le fichier de configuration et/ou de traduction contient des erreurs !",
            }, this, "fr");
        }

        private string GetMessage(string messageKey, string playerId = null, params object[] data)
        {
            try
            {
                var template = lang.GetMessage(messageKey, this, playerId);
                if (VerifyPlaceholders(template, data.Length)) return string.Format(template, data);
                Puts("Wrong number of params compared to the string");
                return lang.GetMessage(MessageKey.PopError, this, playerId);
            }
            catch (Exception exception)
            {
                PrintError(exception.ToString());
                throw;
            }
        }

        #endregion
    }
}
