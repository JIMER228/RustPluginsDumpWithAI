namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    class RemoveEntityFromGroup : Interaction
    {
      public RemoveEntityFromGroup(BasePlayer player, AuthGroup group)
        : base(player, group)
      {
      }

      public override void Handle(HitInfo hit)
      {
        ManagedEntity entity = Group.GetManagedEntity(hit.HitEntity);

        if (entity == null)
        {
          Instance.SendReply(Player, Messages.CannotRemoveEntityNotEnrolledInGroup, Group.Name);
          return;
        }

        if (!entity.IsAuthorized(Player))
        {
          Instance.SendReply(Player, Messages.CannotRemoveEntityNotAuthorized);
          return;
        }

        Group.RemoveEntity(entity);
        Instance.SendReply(Player, Messages.EntityRemoved, Group.Name);
      }
    }
  }
}
