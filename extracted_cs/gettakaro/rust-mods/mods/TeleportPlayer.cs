using UnityEngine;
using System;

namespace Oxide.Plugins;

[Info("TeleportPlayer", "Takaro", "0.0.1")]
internal class TeleportPlayer : RustPlugin
{
    private readonly float _teleportHeight = 1000f;
        
    [ConsoleCommand("teleportplayer.pos")]
    private void CmdTeleportPlayerPos(ConsoleSystem.Arg arg)
    {
        if (!arg.IsRcon) return;
            
        if (arg.Args == null || arg.Args.Length < 4)
        {
            arg.ReplyWith("[TeleportPlayer] Usage: teleportplayer.pos <player> <x> <y> <z>");
            return;
        }

        string playerId = arg.Args[0];
        if (!ulong.TryParse(playerId, out ulong steamID))
        {
            arg.ReplyWith("[TeleportPlayer] Please provide a valid SteamID64");
            return;
        }

        BasePlayer player = BasePlayer.FindByID(steamID);
        if (player == null)
        {
            arg.ReplyWith("[TeleportPlayer] Could not find player with SteamID64: " + playerId);
            return;
        }

        if (!float.TryParse(arg.Args[1], out float x) || !float.TryParse(arg.Args[2], out float y) || !float.TryParse(arg.Args[3], out float z))
        {
            arg.ReplyWith("[TeleportPlayer] Please provide valid coordinates");
            return;
        }

        if (Math.Abs(y - (-1)) < 0.0001f)
        {
            y = _teleportHeight;
        }

        Vector3 position = new(x, y, z);

        player.Teleport(position);
        arg.ReplyWith("[TeleportPlayer] Teleported " + player.displayName + " to " + position);
    }
}