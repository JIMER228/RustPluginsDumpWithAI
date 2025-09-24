// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿namespace Oxide.Plugins
{
  public partial class ZombieAttack : RustPlugin
  {
    void OnClearCommand(BasePlayer player)
    {
      Settings.Locations.Clear();
      SendReply(player, "All attack locations have been removed.");
    }
  }
}