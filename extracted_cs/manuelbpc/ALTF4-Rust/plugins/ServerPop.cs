namespace Oxide.Plugins
{
    [Info("ServerPop", "Mabel", "1.0.0"), Description("Show server pop in chat with /pop command.")]
	class ServerPop : CovalencePlugin
	{
		[Command("pop")]
        void OnPlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (command == "pop")
            {
                player.ChatMessage("There is currently\n\n " + BasePlayer.activePlayerList.Count + " player(s) online\n\n " + BasePlayer.sleepingPlayerList.Count + " player(s) sleeping\n\n " + ServerMgr.Instance.connectionQueue.Joining + " player(s) joining\n\n " + ServerMgr.Instance.connectionQueue.Queued + " player(s) queued ");
			}
			return;
        }
    }
}	
	