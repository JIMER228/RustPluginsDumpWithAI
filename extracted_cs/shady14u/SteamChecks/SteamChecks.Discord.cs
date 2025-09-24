using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        private void SendDiscordMessage(string steamId, string reasonMessage, WebHookThumbnail thumbnail)
        {
            if (string.IsNullOrEmpty(discordWebHookUrl)) return;

            var mentions = "";
            if (discordRolesToMention != null)
                foreach (var roleId in discordRolesToMention)
                {
                    mentions += $"<@&{roleId}> ";
                }

            var message = Lang("DiscordMessage");
            
            var contentBody = new WebHookContentBody
            {
                Content = $"{mentions}{message}"
            };

            var color = 3092790;
            var player = BasePlayer.FindAwakeOrSleeping(steamId);

            var firstBody = new WebHookEmbedBody
            {
                Embeds = new[]
                {
                    new WebHookEmbed
                    {
                        Color = color,
                        Thumbnail = thumbnail,
                        Description =
                            $"Player{Environment.NewLine}[{player?.displayName??steamId}](https://steamcommunity.com/profiles/{steamId})" +
                            $"{Environment.NewLine}{Environment.NewLine}Steam ID{Environment.NewLine}{steamId}" + 
                            $"{Environment.NewLine}{Environment.NewLine}{reasonMessage}"
                    }
                }
            };
           
            webrequest.Enqueue(discordWebHookUrl,
                JsonConvert.SerializeObject(contentBody,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                (headerCode, headerResult) =>
                {
                    if (headerCode >= 200 && headerCode <= 204)
                    {
                        webrequest.Enqueue(discordWebHookUrl,
                            JsonConvert.SerializeObject(firstBody,
                                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }),
                            (firstCode, firstResult) => { }, this, RequestMethod.POST,
                            new Dictionary<string, string> { { "Content-Type", "application/json" } });
                    }
                }, this, RequestMethod.POST,
                new Dictionary<string, string> { { "Content-Type", "application/json" } });
        }
    }
}