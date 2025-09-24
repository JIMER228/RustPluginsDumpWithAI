namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public partial class AuthGroups : RustPlugin
  {
    class AuthGroupManager
    {
      List<AuthGroup> Groups = new List<AuthGroup>();

      public void Init(IEnumerable<AuthGroupInfo> infos)
      {
        foreach (AuthGroupInfo info in infos)
        {
          Groups.Add(new AuthGroup {
            Name = info.Name,
            OwnerId = info.OwnerId,
            Flags = info.Flags,
            MemberIds = new HashSet<string>(info.MemberIds),
            ManagerIds = new HashSet<string>(info.ManagerIds),
            Entities = new HashSet<ManagedEntity>(FindEntities(info.EntityIds).Select(ManagedEntity.Create))
          });
        }
      }

      public AuthGroup GetByOwnerOrManager(BasePlayer player, string name)
      {
        return GetAllByOwnerOrManager(player).FirstOrDefault(group => String.Compare(group.Name, name, true) == 0);
      }

      public AuthGroup[] GetAllByOwnerOrManager(BasePlayer player)
      {
        return Groups.Where(group => group.HasOwner(player) || group.HasManager(player)).ToArray();
      }

      public AuthGroup[] GetAllByMember(BasePlayer player)
      {
        return Groups.Where(group => group.HasMember(player)).ToArray();
      }

      public AuthGroup[] GetAllByEntity(BaseEntity entity)
      {
        return Groups.Where(group => group.HasEntity(entity)).ToArray();
      }

      public AuthGroup Create(BasePlayer owner, string name)
      {
        if (GetByOwnerOrManager(owner, name) != null)
          throw new InvalidOperationException($"Player ${owner.userID} is already the manager of an auth group named {name}!");

        var group = new AuthGroup(owner, name);
        Groups.Add(group);

        return group;
      }

      public void Delete(AuthGroup group)
      {
        group.DeauthorizeAll();
        Groups.Remove(group);
      }

      public AuthGroupInfo[] Serialize()
      {
        return Groups.Select(group => group.Serialize()).ToArray();
      }

      IEnumerable<BaseEntity> FindEntities(IEnumerable<uint> ids)
      {
        return ids.Select(FindEntity).Where(entity => entity != null);
      }

      BaseEntity FindEntity(uint id)
      {
        var entity = BaseNetworkable.serverEntities.Find(id) as BaseEntity;

        if (entity == null)
          Instance.PrintWarning($"Couldn't find entity {id} for an auth group!");

        return entity;
      }
    }
  }
}
