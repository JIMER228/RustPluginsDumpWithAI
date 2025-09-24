using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("ServerCommands", "Captain Blake", "1.0.3")]
    [Description("Provides server rules and population information via chat commands.")]
    public class ServerCommands : RustPlugin
    {
        private PluginConfig _config;

        private const string UsePermission = "servercommands.use";
        private const string AdminPermission = "servercommands.admin";

        private class PluginConfig
        {
            public Dictionary<string, ChatCommand> ChatCommands { get; set; }
        }

        private class ChatCommand
        {
            public string Command { get; set; }
            public string[] Responses { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            _config = new PluginConfig
            {
                ChatCommands = new Dictionary<string, ChatCommand>
                {
                    {
                        "!rules", new ChatCommand
                        {
                            Command = "!rules",
                            Responses = new string[]
                            {
                                "Server Rules:",
                                "1. No racism, abusive language, or hate speech.",
                                "2. No cheating or exploiting game mechanics.",
                                "3. Do not intentionally disrupt the gameplay experience of others.",
                                "4. Avoid spamming chat.",
                                "5. Do not advertise other servers.",
                                "6. PvP Zones are around major monuments and in RaidMe events.",
                                "7. Don't build Turrets facing into PvP zones.",
                                "8. General Conduct: Play fair, be kind, and enjoy the game together."
                            }
                        }
                    },
                    {
                        "!pop", new ChatCommand
                        {
                            Command = "!pop",
                            Responses = new string[]
                            {
                                "There are {onlinePlayers} players online."
                            }
                        }
                    }
                }
            };
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        private void Init()
        {
            Puts("ServerCommands plugin initialized.");

            // Register permissions
            permission.RegisterPermission(UsePermission, this);
            permission.RegisterPermission(AdminPermission, this);
        }

        private void OnPlayerChat(BasePlayer player, string message)
        {
            var lowerMessage = message.ToLower();
            if (!_config.ChatCommands.TryGetValue(lowerMessage, out var value)) return;
            // Check permissions
            if (!permission.UserHasPermission(player.UserIDString, UsePermission))
            {
                SendReply(player, "You do not have permission to use this command.");
                return;
            }

            // Handle special placeholders
            foreach (var response in value.Responses)
            {
                var formattedResponse = response;

                if (lowerMessage == "!pop")
                {
                    var onlinePlayers = BasePlayer.activePlayerList.Count;


                    formattedResponse = formattedResponse.Replace("{onlinePlayers}", onlinePlayers.ToString());
                }
                
                SendReply(player, formattedResponse);
            }
        }

        // Admin command to reload configuration
        [ChatCommand("reloadservercommands")]
        private void ReloadServerCommands(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, AdminPermission))
            {
                SendReply(player, "You do not have permission to use this command.");
                return;
            }

            LoadConfig();
            SendReply(player, "ChatCommands configuration reloaded.");
        }
    }
}