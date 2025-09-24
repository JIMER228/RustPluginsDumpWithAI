// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    void OnClaimRemoveCommand(User user)
    {
      Faction faction = Factions.GetByMember(user);

      if (!EnsureUserCanChangeFactionClaims(user, faction))
        return;

      user.SendChatMessage(Messages.SelectClaimCupboardToRemove);
      user.BeginInteraction(new RemovingClaimInteraction(faction));
    }
  }
}