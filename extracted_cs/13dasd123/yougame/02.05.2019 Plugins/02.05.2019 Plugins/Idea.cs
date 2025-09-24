using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Idea", "Hougan", "1.0.1")]
      //  Слив плагинов server-rust by Apolo YouGame
    public class Idea : RustPlugin
    {
        [PluginReference] private Plugin VKBot;
        [PluginReference] private Plugin BannerSystem;
        
        private List<ulong> cooldownPlayers = new List<ulong>();
		
		void OnServerInitialized()
        {
			lang.RegisterMessages(Messages, this, "en");
            Messages = lang.GetMessages("en", this);
        }

        [ChatCommand("idea")]
        private void cmdChatIdea(BasePlayer player, string command, string[] args)
        {
            if (!VKBot)
            {
				SendReply(player, Messages["IdeaOFF"]);
                return;
            }
            
            if (args.Length == 0)
            {
				SendReply(player, Messages["IdeaError"]);
                return;
            }

            if (cooldownPlayers.Contains(player.userID))
            {
				SendReply(player, Messages["IdeaCooldown"]);
                return;
            }

            string idea = "";
            for (int i = 0; i < args.Length; i++)
                idea += args[i] + " ";

            VKBot.Call("VKAPIChatMsg", $"Предложение от игрока {player.displayName} [{player.userID}]\n" + idea, true);
			SendReply(player, Messages["IdeaAccept"]);
            BannerSystem.Call("GiveBanner", player.userID, "ban.technocircles");
            
            cooldownPlayers.Add(player.userID);
            timer.Once(60, () => cooldownPlayers.Remove(player.userID));
        }
		
        Dictionary<string, string> Messages = new Dictionary<string, string>()
        {
            {"IdeaOFF", "Приносим свои извинения, но мы сейчас не принимаем предложения!"},
			{"IdeaError", "Вы не написали свою идею, попробуйте ещё раз!"},
			{"IdeaCooldown", "Вы не можете предлагать идеи так часто, подождите немного!"},
            {"IdeaAccept", "Предложение успешно отправлено! Помните, если вы будете использовать плагин не по предназначению, вы можете быть заблокированы."},
        };
    }
}
