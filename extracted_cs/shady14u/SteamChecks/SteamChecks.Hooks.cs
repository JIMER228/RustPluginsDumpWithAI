#region Using Statements

using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;

#endregion

namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        #region Methods (Private)

        /// <summary>
        /// Called by Oxide when plugin starts
        /// </summary>
        private void Init()
        {
            InitializeConfig();

            if (string.IsNullOrEmpty(apiKey))
            {
                LogError(Lang("ErrorAPIConfig"));

                // Unload on next tick
                timer.Once(1f, () => { server.Command("oxide.unload SteamChecks"); });
                return;
            }

            appId = covalence.ClientAppId;

            passedList = new HashSet<string>();
            failedList = new HashSet<string>();

            permission.RegisterPermission(skipPermission, this);
        }

        /// <summary>
        /// Called when a user connects (but before he is spawning)
        /// </summary>
        /// <param name="player"></param>
        private void OnUserConnected(IPlayer player)
        {
            if (string.IsNullOrEmpty(apiKey))
                return;

            if (player.HasPermission(skipPermission))
            {
                Log("{0} / {1} in whitelist (via permission {2})", player.Name, player.Id, skipPermission);
                return;
            }

            // Check temporary White/Blacklist if kicking is enabled
            if (!logInsteadofKick)
            {
                // Player already passed the checks, since the plugin is active
                if (cachePassedPlayers && passedList.Contains(player.Id))
                {
                    Log("{0} / {1} passed all checks already previously", player.Name, player.Id);
                    return;
                }

                // Player already passed the checks, since the plugin is active
                if (cacheDeniedPlayers && failedList.Contains(player.Id))
                {
                    Log("{0} / {1} failed a check already previously", player.Name, player.Id);
                    webrequest.Enqueue($"https://steamcommunity.com/profiles/{player.Id}?xml=1", string.Empty,
                    (code, result) =>
                    {
                        WebHookThumbnail thumbnail = null;
                        if (code >= 200 && code <= 204)
                            thumbnail = new WebHookThumbnail
                            {
                                Url = _steamAvatarRegex.Match(result).Value
                            };
                        SendDiscordMessage(player.Id, "The player would not pass the checks. Reason: " + additionalKickMessage, thumbnail);
                    }, this);
                    player.Kick(Lang("KickGeneric", player.Id) + " " + additionalKickMessage);
                    return;
                }
            }

            CheckPlayer(player.Id, (playerAllowed, reason) =>
            {
                if (playerAllowed)
                {
                    Log("{0} / {1} passed all checks", player.Name, player.Id);
                    passedList.Add(player.Id);
                }
                else
                {
                    if (logInsteadofKick)
                    {
                        Log("{0} / {1} would have been kicked. Reason: {2}", player.Name, player.Id, reason);
                    }
                    else
                    {
                        Log("{0} / {1} kicked. Reason: {2}", player.Name, player.Id, reason);
                        failedList.Add(player.Id);
                        webrequest.Enqueue($"https://steamcommunity.com/profiles/{player.Id}?xml=1", string.Empty,
                    (code, result) =>
                    {
                        WebHookThumbnail thumbnail = null;
                        if (code >= 200 && code <= 204)
                            thumbnail = new WebHookThumbnail
                            {
                                Url = _steamAvatarRegex.Match(result).Value
                            };
                        SendDiscordMessage(player.Id, $"The player would not pass the checks. Reason: {reason} {additionalKickMessage}" , thumbnail);
                    }, this);
                        player.Kick(reason + " " + additionalKickMessage);

                        if (broadcastKick)
                        {
                            foreach (var target in players.Connected)
                            {
                                target.Message(Lang("Console", player.Id), "", player.Name, reason);
                            }
                        }
                    }
                }
            });
        }

        #endregion
    }
}