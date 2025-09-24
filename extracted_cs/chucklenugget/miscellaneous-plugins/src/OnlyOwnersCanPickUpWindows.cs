namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using UnityEngine;

  [Info("OnlyOwnersCanPickUpWindows", "chucklenugget", "0.1.0")]
  public partial class OnlyOwnersCanPickUpWindows : RustPlugin
  {
    static HashSet<string> PREFABS = new HashSet<string> {
      "wall.window.bars.wood",
      "wall.window.bars.metal",
      "wall.window.bars.toptier",
      "wall.window.glass.reinforced"
    };

    const string FAILURE_EFFECT = "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab";

    object CanPickupEntity(BaseCombatEntity entity, BasePlayer player)
    {
      if (entity == null || player == null)
        return null;

      if (PREFABS.Contains(entity.ShortPrefabName) && player.userID != entity.OwnerID)
      {
        PlayEffectAtEntity(entity, FAILURE_EFFECT);
        return false;
      }

      return null;
    }

    void PlayEffectAtEntity(BaseEntity entity, string effectPrefab)
    {
      Effect.server.Run(effectPrefab, entity, 0, Vector3.zero, Vector3.forward);
    }
  }
}