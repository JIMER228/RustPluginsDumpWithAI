

namespace Oxide.Plugins
{
    public class HelloWorld
    {
        private void Init()
        {
            Puts("Hello, Rust World! Plugin successfully loaded.");
        }

        [ChatCommand("hello")]
        private void HelloCommand(BasePlayer player, string command, string[] args)
        {
            player.ChatMessage("Hello, " + player.displayName + "! Welcome to this Rust server.");
        }

    }
}
