using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Private Message", "Danulsan", "1.0.0")]
    [Description("Allows admins to send private messages to players via console using SteamID.")]

    public class PrivateMessage : RustPlugin
    {
        [ConsoleCommand("pm")]
        private void CmdPrivateMessage(ConsoleSystem.Arg arg)
        {
            if (arg.Connection != null && arg.Connection.authLevel < 1)
            {
                arg.ReplyWith("You do not have permission to use this command.");
                return;
            }

            if (arg.Args == null || arg.Args.Length < 2)
            {
                Puts("Usage: pm <steamid> \"<message>\"");
                return;
            }

            string steamId = arg.Args[0];
            string message = string.Join(" ", arg.Args, 1, arg.Args.Length - 1).Trim('"');

            BasePlayer targetPlayer = BasePlayer.FindByID(ulong.Parse(steamId));
            if (targetPlayer == null)
            {
                Puts($"Player with SteamID {steamId} not found.");
                return;
            }

            targetPlayer.ChatMessage($"[SERVER]: {message}");
            Puts($"Sent message to {targetPlayer.displayName} ({steamId}): {message}");
        }
    }
}
