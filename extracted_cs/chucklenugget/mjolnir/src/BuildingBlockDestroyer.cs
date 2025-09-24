namespace Oxide.Plugins
{
  using System.Collections;
  using System.Collections.Generic;
  using UnityEngine;

  public partial class Mjolnir : RustPlugin
  {
    class BuildingBlockDestroyer : MonoBehaviour
    {
      Mjolnir Core;
      Queue<BaseEntity[]> QueuedBuildings;

      public bool IsDestroying { get; private set; }

      public void Init(Mjolnir core)
      {
        Core = core;
        QueuedBuildings = new Queue<BaseEntity[]>();
      }

      public void Destroy(BaseEntity[] blocks)
      {
        QueuedBuildings.Enqueue(blocks);

        if (!IsDestroying)
          DestroyNext();
      }

      void DestroyNext()
      {
        if (QueuedBuildings.Count == 0)
        {
          IsDestroying = false;
          return;
        }

        BaseEntity[] blocks = QueuedBuildings.Dequeue();
        IsDestroying = true;

        StartCoroutine(DestroyBlocks(blocks));
      }

      IEnumerator DestroyBlocks(BaseEntity[] blocks)
      {
        Core.Puts("Destroying {0} entities", blocks.Length);

        foreach (BuildingBlock block in blocks)
        {
          if (block != null && !block.IsDestroyed)
            block.Kill(BaseNetworkable.DestroyMode.Gib);

          yield return null;
        }

        DestroyNext();
      }
    }
  }
}
