namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using UnityEngine;

  [Info("OnlyOwnersCanLootLights", "chucklenugget", "0.1.0")]
  public partial class OnlyOwnersCanLootLights : RustPlugin
  {
    static HashSet<string> PREFABS = new HashSet<string> {
      "ceilinglight.deployed",
      "lantern.deployed",
      "tunalight.deployed",
      "searchlight.deployed"
    };

    const string FAILURE_EFFECT = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab";

    void OnLootEntity(BasePlayer player, BaseEntity entity)
    {
      if (player == null || entity == null)
        return;

      if (PREFABS.Contains(entity.ShortPrefabName) && player.userID != entity.OwnerID)
      {
        PlayEffectAtEntity(entity, FAILURE_EFFECT);
        NextTick(player.EndLooting);
      }
    }

    void PlayEffectAtEntity(BaseEntity entity, string effectPrefab)
    {
      Effect.server.Run(effectPrefab, entity, 0, Vector3.zero, Vector3.forward);
    }
  }
}