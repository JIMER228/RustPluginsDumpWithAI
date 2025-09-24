using Oxide.Core.Plugins;
using Oxide.Core;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("WelcomePlugin", "herbs.acab", "1.0.0")]
    [Description("Sends a welcome message to new players and notifies all players when a new player joins for the first time.")]
    public class WelcomePlugin : RustPlugin
    {
        private Dictionary<ulong, bool> playerData = new Dictionary<ulong, bool>();

        private void Init()
        {
            LoadData();
        }

        private void LoadData()
        {
            playerData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>(Name);
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, playerData);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            SendWelcomeMessage(player);
            
            if (!playerData.ContainsKey(player.userID))
            {
                playerData[player.userID] = true;
                SaveData();
                BroadcastNewPlayerMessage(player);
            }
        }

        private void SendWelcomeMessage(BasePlayer player)
        {
            string message = $"Welcome to [color=blue]Rusty Reef[/color] {player.displayName}! Use /help to see a list of commands and join our discord with the [color=green]View Website[/color] button in the server browser!";
            player.ChatMessage(message);
        }

        private void BroadcastNewPlayerMessage(BasePlayer player)
        {
            string message = $"{player.displayName} has joined the Reef for the first time!";
            PrintToChat(message);
        }
    }
}
