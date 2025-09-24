// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    [ConsoleCommand("imperium.map.toggle")]
    void OnMapToggleConsoleCommand(ConsoleSystem.Arg arg)
    {
      var player = arg.Connection.player as BasePlayer;
      if (player == null) return;

      User user = Users.Get(player);
      if (user == null) return;

      if (!user.Map.IsVisible && !EnforceCommandCooldown(user, "map", Options.Map.CommandCooldownSeconds))
        return;

      user.Map.Toggle();
    }
  }
}
