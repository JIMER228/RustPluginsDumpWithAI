// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Ping", "A0001", "0.0.1", ResourceId = 2126)]

    class Ping : CovalencePlugin
    {
        [Command("ping")]
        void PingCommand(IPlayer player, string command, string[] args)
        {
			player.Reply(Lang("<color=#FBA026>Ваш пинг:</color> <color=#00FF00>{0}</color>", player.Id, player.Ping));
        }

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);
    }
}
