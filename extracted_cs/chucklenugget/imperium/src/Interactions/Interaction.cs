// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿namespace Oxide.Plugins
{
  public partial class Imperium
  {
    abstract class Interaction
    {
      public User User { get; set; }
      public abstract bool TryComplete(HitInfo hit);
    }
  }
}
