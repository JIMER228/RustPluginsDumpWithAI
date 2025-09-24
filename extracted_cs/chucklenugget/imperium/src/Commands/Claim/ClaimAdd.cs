// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimAddCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureUserCanChangeFactionClaims(user, faction))
        return;

      user.SendChatMessage(Messages.SelectClaimCupboardToAdd);
      user.BeginInteraction(new AddingClaimInteraction(faction));
    }
  }
}