namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupRemoveCommand(BasePlayer player, string groupName, string[] args)
    {
      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      if (args.Length == 0)
      {
        PendingInteractions.Add(player.UserIDString, new RemoveEntityFromGroup(player, group));
        SendReply(player, Messages.SelectEntityToRemoveFromGroup, group.Name);
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
        SendReply(player, Messages.CannotRemoveMemberNotMemberOfGroup, member.displayName, group.Name);
        return;
      }

      group.RemoveMember(member);
      SendReply(player, Messages.MemberRemoved, member.displayName, group.Name);
    }
  }
}
