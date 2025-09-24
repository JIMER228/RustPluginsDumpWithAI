// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using Newtonsoft.Json;

  public partial class ZombieAttack : RustPlugin
  {
    class ZombieAttackSettings
    {
      [JsonProperty("intervalMinutes")]
      public float IntervalMinutes = 30f;

      [JsonProperty("minAttackers")]
      public int MinAttackers = 5;

      [JsonProperty("maxAttackers")]
      public int MaxAttackers = 10;

      [JsonProperty("locations")]
      public Dictionary<string, ZombieAttackLocation> Locations = new Dictionary<string, ZombieAttackLocation>();
    }
  }
}