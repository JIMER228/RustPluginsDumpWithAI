namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using Newtonsoft.Json;
  using Newtonsoft.Json.Converters;

  public partial class AuthGroups : RustPlugin
  {
    class AuthGroup
    {
      public string Name;
      public string OwnerId;
      public HashSet<string> MemberIds;
      public HashSet<string> ManagerIds;
      public HashSet<ManagedEntity> Entities;
      public AuthGroupFlag Flags;

      public AuthGroup()
      {
        MemberIds = new HashSet<string>();
        ManagerIds = new HashSet<string>();
        Entities = new HashSet<ManagedEntity>();
      }

      public AuthGroup(BasePlayer owner, string name)
        : this()
      {
        OwnerId = owner.UserIDString;
        Name = name;
      }

      public bool AddMember(BasePlayer player)
      {
        return AddMember(player.UserIDString);
      }

      public bool AddMember(string playerId)
      {
        if (HasMember(playerId))
          return false;

        MemberIds.Add(playerId);

        BasePlayer player = BasePlayer.Find(playerId);
        if (player != null)
          EnsureAuthorized(player);

        return true;
      }

      public bool RemoveMember(BasePlayer player)
      {
        return RemoveMember(player.UserIDString);
      }

      public bool RemoveMember(string playerId)
      {
        if (HasOwner(playerId))
          throw new InvalidOperationException($"Cannot remove player {playerId} from auth group, since they are the owner");

        if (!HasMember(playerId))
          return false;

        MemberIds.Remove(playerId);
        ManagerIds.Remove(playerId);

        BasePlayer player = BasePlayer.Find(playerId);
        if (player != null)
          EnsureDeauthorized(player);

        return true;
      }

      public bool Promote(BasePlayer player)
      {
        return Promote(player.UserIDString);
      }

      public bool Promote(string playerId)
      {
        if (!MemberIds.Contains(playerId))
          throw new InvalidOperationException($"Cannot promote player {playerId} in group {Name} owned by {OwnerId}, since they are not a member");

        return ManagerIds.Add(playerId);
      }

      public bool Demote(BasePlayer player)
      {
        return Demote(player.UserIDString);
      }

      public bool Demote(string playerId)
      {
        if (!MemberIds.Contains(playerId))
          throw new InvalidOperationException($"Cannot demote player {playerId} in group {Name} owned by {OwnerId}, since they are not a member");

        return ManagerIds.Remove(playerId);
      }

      public bool AddEntity(ManagedEntity entity)
      {
        if (Entities.Any(e => e.Id == entity.Id))
          return false;

        Entities.Add(entity);

        entity.DeauthorizeAll();
        entity.Authorize(GetActiveAndSleepingMembers());

        return true;
      }

      public bool RemoveEntity(ManagedEntity entity)
      {
        if (!Entities.Remove(entity))
          return false;

        entity.DeauthorizeAll();
        return true;
      }

      public bool RemoveEntity(BaseEntity entity)
      {
        ManagedEntity managedEntity = GetManagedEntity(entity);

        if (managedEntity == null)
          return false;

        return RemoveEntity(managedEntity);
      }

      public void EnsureAuthorized(BasePlayer player)
      {
        foreach (ManagedEntity entity in Entities.Where(entity => !entity.IsAuthorized(player)))
          entity.Authorize(player);
      }

      public void EnsureDeauthorized(BasePlayer player)
      {
        foreach (ManagedEntity entity in Entities.Where(entity => entity.IsAuthorized(player)))
          entity.Deauthorize(player);
      }

      public void SynchronizeAll()
      {
        foreach (ManagedEntity entity in Entities)
          entity.SetAuthorizedPlayers(GetActiveAndSleepingMembers());
      }

      public void DeauthorizeAll()
      {
        foreach (ManagedEntity entity in Entities)
          entity.DeauthorizeAll();
      }

      public ManagedEntity GetManagedEntity(BaseEntity entity)
      {
        return Entities.FirstOrDefault(e => e.Id == entity.net.ID);
      }

      public BasePlayer[] GetActiveAndSleepingMembers()
      {
        return MemberIds.Select(id => BasePlayer.Find(id)).Where(p => p != null).ToArray();
      }

      public T[] GetAllManagedEntitiesOfType<T>() where T : ManagedEntity
      {
        return Entities.OfType<T>().ToArray();
      }

      public bool HasMember(BasePlayer basePlayer)
      {
        return HasMember(basePlayer.UserIDString);
      }

      public bool HasMember(string playerId)
      {
        return MemberIds.Contains(playerId);
      }

      public bool HasManager(BasePlayer basePlayer)
      {
        return HasManager(basePlayer.UserIDString);
      }

      public bool HasManager(string playerId)
      {
        return ManagerIds.Contains(playerId);
      }

      public bool HasOwner(BasePlayer basePlayer)
      {
        return HasOwner(basePlayer.UserIDString);
      }

      public bool HasOwner(string playerId)
      {
        return OwnerId == playerId;
      }

      public bool HasEntity(BaseEntity entity)
      {
        return GetManagedEntity(entity) != null;
      }
      
      public bool HasFlag(AuthGroupFlag flag)
      {
        return (Flags & flag) != 0;
      }

      public void AddFlag(AuthGroupFlag flag)
      {
        Flags |= flag;
      }

      public void RemoveFlag(AuthGroupFlag flag)
      {
        Flags &= ~flag;
      }

      public AuthGroupInfo Serialize()
      {
        return new AuthGroupInfo {
          Name = Name,
          OwnerId = OwnerId,
          Flags = Flags,
          ManagerIds = ManagerIds.ToArray(),
          MemberIds = MemberIds.ToArray(),
          EntityIds = Entities.Select(entity => entity.Id).ToArray()
        };
      }
    }

    public class AuthGroupInfo
    {
      [JsonProperty("name")]
      public string Name;

      [JsonProperty("ownerId")]
      public string OwnerId;

      [JsonProperty("flags"), JsonConverter(typeof(StringEnumConverter))]
      public AuthGroupFlag Flags;

      [JsonProperty("memberIds")]
      public string[] MemberIds;

      [JsonProperty("managerIds")]
      public string[] ManagerIds;

      [JsonProperty("entityIds")]
      public uint[] EntityIds;
    }

    [Flags]
    public enum AuthGroupFlag
    {
      TurretsLocked = 1
    }
  }
}
