using ConVar;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    [Info("Chat to Telegram Relay", "Psystec", "1.0.0")]
    [Description("Relay chat to Telegram")]
    public class ChatToTelegram : CovalencePlugin
    {
        public const string AdminPermission = "chattotelegram.admin";

        #region Configuration

        private Configuration _configuration;
        private class Configuration
        {
            public string TelegramBotToken { get; set; } = "https://core.telegram.org/bots/features#creating-a-new-bot";
            public string ChatID { get; set; } = "https://www.alphr.com/find-chat-id-telegram/";
            public bool EnableGlobalChat { get; set; } = true;
            public bool EnableTeamChat { get; set; } = true;
            public bool EnableConnections { get; set; } = true;
            public string GlobalChatFormat { get; set; } = "<b>[{time}] [GLOBAL] {username}:</b> {message}";
            public string TeamChatFormat { get; set; } = "<b>[{time}] [TEAM] {username}:</b> {message}";
            public string ConnectionFormat { get; set; } = "<b>[{time}] {username}:</b> {connectionstatus}";
            public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["Connected"] = "Connected.",
                ["Disconnected"] = "Disconnected.",
                ["NoPermission"] = "You do not have permission to use this command.",
                ["FileLoaded"] = "File loaded.",
                ["cmdCommand"] = "COMMAND",
                ["cmdDescription"] = "DESCRIPTION",
                ["cmdReload"] = "Reads the config file."
            }, this);
        }

        protected override void SaveConfig() => Config.WriteObject(_configuration);
        private void LoadNewConfig() => _configuration = Config.ReadObject<Configuration>();
        protected override void LoadDefaultConfig() => _configuration = new Configuration();
        protected override void LoadConfig()
        {
            base.LoadConfig();
            _configuration = Config.ReadObject<Configuration>();
        }

        #endregion Configuration

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(AdminPermission, this);
        }

        private void Loaded()
        {
            CheckSubscribsions();
        }

        #endregion Hooks

        #region User Connection Hooks

        private void OnUserConnected(IPlayer player)
        {
            string message = _configuration.ConnectionFormat
                .Replace("{time}", DateTime.Now.ToString(_configuration.DateFormat))
                .Replace("{username}", player.Name)
                .Replace("{connectionstatus}", Lang("Connected"));

            SendToTelegram(_configuration.ChatID, message);
        }

        private void OnUserDisconnected(IPlayer player)
        {
            string message = _configuration.ConnectionFormat
                .Replace("{time}", DateTime.Now.ToString(_configuration.DateFormat))
                .Replace("{username}", player.Name)
                .Replace("{connectionstatus}", Lang("Disconnected"));

            SendToTelegram(_configuration.ChatID, message);
        }

        #endregion User Connection Hooks

        #region User Chat Hooks

        private void OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {

            message = RemoveSpecialCharacters(message);
            message = message.Replace("@here", "@.here")
                .Replace("@everyone", "@.everyone")
                .Replace("/start", "/.start");

            switch (channel)
            {
                case Chat.ChatChannel.Global:
                    if (_configuration.EnableGlobalChat)
                    {
                        message = _configuration.GlobalChatFormat
                            .Replace("{time}", DateTime.Now.ToString(_configuration.DateFormat))
                            .Replace("{username}", player.displayName)
                            .Replace("{message}", message);

                        SendToTelegram(_configuration.ChatID, message);
                    }
                    break;

                case Chat.ChatChannel.Team:
                    if (_configuration.EnableTeamChat)
                    {
                        message = _configuration.TeamChatFormat
                            .Replace("{time}", DateTime.Now.ToString(_configuration.DateFormat))
                            .Replace("{username}", player.displayName)
                            .Replace("{message}", message);

                        SendToTelegram(_configuration.ChatID, message);
                    }
                    break;
            }
        }

        #endregion User Chat Hooks

        #region Commands

        [Command("chattotelegram")]
        private void ChatToTelegramCommands(IPlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            if (!HasPermission(player, AdminPermission))
                return;

            if (args.IsNullOrEmpty())
            {
                player.Reply(Lang("cmdCommand").PadRight(30) + Lang("cmdDescription"));
                player.Reply(("chattotelegram loadconfig").PadRight(30) + Lang("cmdReload"));
                return;
            }

            if (args[0] == "loadconfig")
            {
                player.Reply(Lang("FileLoaded"));
                LoadNewConfig();
                Loaded();
            }
        }

        #endregion Commands

        #region Helpers

        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
        private bool HasPermission(IPlayer player, string permission)
        {
            if (!player.HasPermission(permission))
            {
                player.Reply(Lang("NoPermission"));
                PrintWarning("UserID: " + player.Id + " | UserName: " + player.Name + " | " + Lang("NoPermission"));
                return false;
            }
            return true;
        }
        public static string RemoveSpecialCharacters(string message)
        {
            string pattern = "[^a-zA-Z0-9._@\\[\\] ]";

            // Replace matched characters (those NOT allowed) with an empty string
            string cleanedMessage = Regex.Replace(message, pattern, "");

            return cleanedMessage;
        }
        private void SendToTelegram(string chatID, string message)
        {
            string sendMessageUrl = $"https://api.telegram.org/bot{_configuration.TelegramBotToken}/sendMessage";

            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };
            Dictionary<string, string> content = new Dictionary<string, string>
            {
                { "chat_id", chatID },
                { "text", message },
                { "parse_mode", "HTML" },
                { "disable_web_page_preview", "true" },
            };

            string payload = JsonConvert.SerializeObject(content);
            webrequest.Enqueue(sendMessageUrl, payload, (dcode, dresponse) =>
            {
                if (dcode != 200 && dcode != 204)
                {
                    if (dresponse == null)
                    {
                        PrintWarning($"Telegram didn't respond (down?). Code: {dcode}");
                    }

                    if (dresponse != message)
                    {
                        PrintWarning($"Telegram didn't send the message. Code: {dcode} Response: {dresponse}");
                    }
                }
            }, this, Core.Libraries.RequestMethod.POST, headers);
        }

        private void CheckSubscribsions()
        {
            if (!_configuration.EnableGlobalChat && !_configuration.EnableTeamChat)
                Unsubscribe(nameof(OnPlayerChat));
            else
                Subscribe(nameof(OnPlayerChat));

            if (!_configuration.EnableConnections)
            {
                Unsubscribe(nameof(OnUserConnected));
                Unsubscribe(nameof(OnUserDisconnected));
            }
            else
            {
                Subscribe(nameof(OnUserConnected));
                Subscribe(nameof(OnUserDisconnected));
            }
        }

        #endregion Helpers
    }
}
