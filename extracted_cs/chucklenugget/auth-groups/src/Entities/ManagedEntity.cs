namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using UnityEngine;

  public partial class AuthGroups : RustPlugin
  {
    abstract class ManagedEntity
    {
      public BaseEntity Entity { get; }
      public uint Id { get; }

      public abstract void Authorize(BasePlayer player);
      public abstract void Authorize(IEnumerable<BasePlayer> players);
      public abstract void Deauthorize(BasePlayer player);
      public abstract void Deauthorize(IEnumerable<BasePlayer> players);
      public abstract void DeauthorizeAll();
      public abstract void SetAuthorizedPlayers(IEnumerable<BasePlayer> players);
      public abstract BasePlayer[] GetAuthorizedPlayers();
      public abstract bool IsAuthorized(BasePlayer player);

      public static bool IsSupportedEntity(BaseEntity entity)
      {
        return (entity is BuildingPrivlidge) || (entity is AutoTurret) || (entity is CodeLock);
      }

      public static ManagedEntity Create(BaseEntity entity)
      {
        var cupboard = entity as BuildingPrivlidge;
        if (cupboard != null)
          return new ManagedToolCupboard(cupboard);

        var turret = entity as AutoTurret;
        if (turret != null)
          return new ManagedAutoTurret(turret);

        var codeLock = entity as CodeLock;
        if (codeLock != null)
          return new ManagedCodeLock(codeLock);

        codeLock = entity.GetSlot(BaseEntity.Slot.Lock) as CodeLock;
        if (codeLock != null)
          return new ManagedCodeLock(codeLock);

        return null;
      }

      protected ManagedEntity(BaseEntity entity)
      {
        Entity = entity;
        Id = entity.net.ID;
      }

      protected ProtoBuf.PlayerNameID CreateAuthListEntry(BasePlayer player, bool shouldPool = true)
      {
        return new ProtoBuf.PlayerNameID {
          userid = player.userID,
          username = player.displayName,
          ShouldPool = shouldPool
        };
      }

      public void PlayEffectAtEntity(GameObjectRef effect)
      {
        Effect.server.Run(effect.resourcePath, Entity, 0, Vector3.zero, Vector3.forward);
      }
    }
  }
}
