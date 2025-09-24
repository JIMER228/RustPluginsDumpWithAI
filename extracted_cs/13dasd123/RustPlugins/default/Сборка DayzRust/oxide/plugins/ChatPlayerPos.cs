namespace Oxide.RustPlugin {
    [Info("Chat Player Pos", "kurushimu", "0.0.1")]
    class ChatPlayerPos : RustPlugin {
        [ChatCommand ("pos")]
        void cmdChatPos (BasePlayer player) {
            player.ChatMessage("Ваша позиция: " + player.GetNetworkPosition())
        }
    }