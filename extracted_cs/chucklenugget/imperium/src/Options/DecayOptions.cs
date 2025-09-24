// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿namespace Oxide.Plugins
{
  using Newtonsoft.Json;

  public partial class Imperium : RustPlugin
  {
    class DecayOptions
    {
      [JsonProperty("enabled")]
      public bool Enabled;

      [JsonProperty("claimedLandDecayReduction")]
      public float ClaimedLandDecayReduction;

      public static DecayOptions Default = new DecayOptions {
        Enabled = false,
        ClaimedLandDecayReduction = 0.5f
      };
    }
  }
}
