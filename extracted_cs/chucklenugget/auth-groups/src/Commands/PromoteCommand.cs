namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupPromoteCommand(BasePlayer player, string groupName, string[] args)
    {
      if (args.Length == 0)
      {
        SendReply(player, "<color=#ffd479>Usage:</color> /ag NAME promote PLAYER");
        return;
      }

      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      var playerName = args[0].Trim();
      BasePlayer member = BasePlayerEx.FindByNameOrId(playerName);

      if (member == null)
      {
        SendReply(player, Messages.NoSuchPlayer, playerName);
        return;
      }

      if (!group.HasMember(member))
      {
        SendReply(player, Messages.CannotPromoteNotMemberOfGroup, member.displayName, group.Name);
        return;
      }

      if (group.HasOwner(member))
      {
        SendReply(player, Messages.CannotPromoteIsOwnerOfGroup, member.displayName, group.Name);
        return;
      }

      if (group.HasManager(member))
      {
        SendReply(player, Messages.CannotPromoteAlreadyManagerOfGroup, member.displayName, group.Name);
        return;
      }

      group.Promote(member);
      SendReply(player, Messages.ManagerAdded, member.displayName, group.Name);
    }
  }
}
