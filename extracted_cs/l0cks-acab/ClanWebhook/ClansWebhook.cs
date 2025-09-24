using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("ClansWebhook", "locks", "1.0.5")]
    [Description("Sends clan events to a Discord webhook and provides an admin command to post all clans")]

    class ClansWebhook : CovalencePlugin
    {
        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Webhook URL")]
            public string WebhookUrl { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new Configuration
            {
                WebhookUrl = "YOUR_DISCORD_WEBHOOK_URL_HERE"
            }, true);
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
        }

        #endregion

        #region Hooks

        private void OnClanCreate(string clanTag)
        {
            var clanInfo = GetClanInfo(clanTag);
            if (clanInfo != null)
            {
                SendWebhook(clanInfo);
            }
        }

        private void OnClanUpdate(string clanTag)
        {
            var clanInfo = GetClanInfo(clanTag);
            if (clanInfo != null)
            {
                SendWebhook(clanInfo);
            }
        }

        private void OnClanDestroy(string clanTag)
        {
            var message = new
            {
                embeds = new[]
                {
                    new
                    {
                        author = new { name = $"Clan Disbanded: {clanTag}" },
                        description = $"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                        footer = new { text = "developed by herbs.acab" }
                    }
                }
            };

            SendWebhook(JsonConvert.SerializeObject(message));
        }

        private void OnClanChat(IPlayer player, string message)
        {
            var webhookMessage = new
            {
                embeds = new[]
                {
                    new
                    {
                        author = new { name = $"Clan Chat from {player.Name}" },
                        description = $"{message}\n\nTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                        footer = new { text = "developed by herbs.acab" }
                    }
                }
            };

            SendWebhook(JsonConvert.SerializeObject(webhookMessage));
        }

        private void OnClanMemberJoined(string userID, List<string> memberUserIDs)
        {
            var player = covalence.Players.FindPlayerById(userID);
            if (player == null)
            {
                Puts($"Player with ID {userID} not found in OnClanMemberJoined hook");
                return;
            }

            var message = new
            {
                embeds = new[]
                {
                    new
                    {
                        author = new { name = "Player Joined Clan" },
                        description = $"{player.Name} ({player.Id})\nTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                        footer = new { text = "developed by herbs.acab" }
                    }
                }
            };

            SendWebhook(JsonConvert.SerializeObject(message));
        }

        private void OnClanMemberJoined(string userID, string clanTag)
        {
            var player = covalence.Players.FindPlayerById(userID);
            if (player == null)
            {
                Puts($"Player with ID {userID} not found in OnClanMemberJoined hook");
                return;
            }

            var message = new
            {
                embeds = new[]
                {
                    new
                    {
                        author = new { name = $"Player Joined Clan {clanTag}" },
                        description = $"{player.Name} ({player.Id})\nTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                        footer = new { text = "developed by herbs.acab" }
                    }
                }
            };

            SendWebhook(JsonConvert.SerializeObject(message));
        }

        private void OnClanMemberGone(string userID, List<string> memberUserIDs)
        {
            var player = covalence.Players.FindPlayerById(userID);
            if (player == null)
            {
                Puts($"Player with ID {userID} not found in OnClanMemberGone hook");
                return;
            }

            var message = new
            {
                embeds = new[]
                {
                    new
                    {
                        author = new { name = "Player Left Clan" },
                        description = $"{player.Name} ({player.Id})\nTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                        footer = new { text = "developed by herbs.acab" }
                    }
                }
            };

            SendWebhook(JsonConvert.SerializeObject(message));
        }

        private void OnClanMemberGone(string userID, string clanTag)
        {
            var player = covalence.Players.FindPlayerById(userID);
            if (player == null)
            {
                Puts($"Player with ID {userID} not found in OnClanMemberGone hook");
                return;
            }

            var message = new
            {
                embeds = new[]
                {
                    new
                    {
                        author = new { name = $"Player Left Clan {clanTag}" },
                        description = $"{player.Name} ({player.Id})\nTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                        footer = new { text = "developed by herbs.acab" }
                    }
                }
            };

            SendWebhook(JsonConvert.SerializeObject(message));
        }

        private void OnClanDisbanded(List<string> memberUserIDs)
        {
            var memberList = memberUserIDs != null ? string.Join(", ", memberUserIDs) : "No members found";
            var message = new
            {
                embeds = new[]
                {
                    new
                    {
                        author = new { name = "Clan Disbanded" },
                        description = $"Members: {memberList}\nTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                        footer = new { text = "developed by herbs.acab" }
                    }
                }
            };

            SendWebhook(JsonConvert.SerializeObject(message));
        }

        private void OnClanDisbanded(string clanTag, List<string> memberUserIDs)
        {
            var memberList = memberUserIDs != null ? string.Join(", ", memberUserIDs) : "No members found";
            var message = new
            {
                embeds = new[]
                {
                    new
                    {
                        author = new { name = $"Clan {clanTag} Disbanded" },
                        description = $"Members: {memberList}\nTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                        footer = new { text = "developed by herbs.acab" }
                    }
                }
            };

            SendWebhook(JsonConvert.SerializeObject(message));
        }

        #endregion

        #region Webhook

        private void SendWebhook(string message)
        {
            if (string.IsNullOrEmpty(config.WebhookUrl))
            {
                Puts("Discord webhook URL is not set in the configuration.");
                return;
            }

            webrequest.Enqueue(config.WebhookUrl, message, (code, response) =>
            {
                if (code != 204)
                {
                    Puts($"Failed to send webhook: {code} - {response}");
                }
            }, this, Core.Libraries.RequestMethod.POST, new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            });
        }

        #endregion

        #region Commands

        [Command("postclans")]
        private void PostClansCommand(IPlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                player.Reply("You are not authorized to use this command.");
                return;
            }

            var clansPlugin = plugins.Find("Clans") as Plugin;
            if (clansPlugin == null)
            {
                player.Reply("Clans plugin not found.");
                return;
            }

            var clans = GetAllClans(clansPlugin);
            foreach (var clan in clans)
            {
                var clanInfo = GetClanInfo(clansPlugin, clan);
                SendWebhook(clanInfo);
            }
            player.Reply("Posted all clans to the webhook.");
        }

        #endregion

        #region Helpers

        private List<string> GetAllClans(Plugin clansPlugin)
        {
            var result = clansPlugin.Call("GetAllClans") as JArray;
            return result?.ToObject<List<string>>() ?? new List<string>();
        }

        private string GetClanInfo(Plugin clansPlugin, string clanTag)
        {
            var clan = clansPlugin.Call("GetClan", clanTag) as JObject;
            if (clan == null) return $"Clan {clanTag} not found.";

            var clanInfo = new
            {
                embeds = new[]
                {
                    new
                    {
                        author = new { name = $"Clan: {clan["tag"]}" },
                        description = $"Members:\n{GetClanMembers(clan)}\nTime: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                        footer = new { text = "developed by herbs.acab" }
                    }
                }
            };

            return JsonConvert.SerializeObject(clanInfo);
        }

        private string GetClanMembers(JObject clan)
        {
            var members = clan["members"] as JArray;
            var owner = clan["owner"]?.ToString();
            var memberList = new List<string>();

            if (owner != null)
            {
                var ownerPlayer = covalence.Players.FindPlayerById(owner);
                if (ownerPlayer != null)
                {
                    memberList.Add($"{ownerPlayer.Name} (Owner) ({ownerPlayer.Id})");
                }
            }

            if (members != null)
            {
                foreach (var memberId in members)
                {
                    var player = covalence.Players.FindPlayerById(memberId.ToString());
                    if (player != null && player.Id != owner)
                    {
                        memberList.Add($"{player.Name} ({player.Id})");
                    }
                }
            }

            return string.Join("\n", memberList);
        }

        private string GetClanInfo(string clanTag)
        {
            var clansPlugin = plugins.Find("Clans") as Plugin;
            if (clansPlugin == null)
            {
                Puts("Clans plugin not found.");
                return null;
            }

            return GetClanInfo(clansPlugin, clanTag);
        }

        #endregion
    }
}
