namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    class AddEntityToGroup : Interaction
    {
      public AddEntityToGroup(BasePlayer player, AuthGroup group)
        : base(player, group)
      {
      }

      public override void Handle(HitInfo hit)
      {
        if (Group.HasEntity(hit.HitEntity))
        {
          Instance.SendReply(Player, Messages.CannotAddEntityAlreadyEnrolledInGroup, Group.Name);
          return;
        }

        var entity = ManagedEntity.Create(hit.HitEntity);

        if (entity == null)
        {
          Instance.SendReply(Player, Messages.InvalidEntitySelected);
          return;
        }

        if (!entity.IsAuthorized(Player))
        {
          Instance.SendReply(Player, Messages.CannotAddEntityNotAuthorized);
          return;
        }

        Group.AddEntity(entity);
        Instance.SendReply(Player, Messages.EntityAdded, Group.Name, Group.MemberIds.Count);
      }
    }
  }
}
