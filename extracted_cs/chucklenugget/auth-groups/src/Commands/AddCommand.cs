namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupAddCommand(BasePlayer player, string groupName, string[] args)
    {
      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      if (args.Length == 0)
      {
        PendingInteractions.Add(player.UserIDString, new AddEntityToGroup(player, group));
        SendReply(player, Messages.SelectEntityToAddToGroup, group.Name);
        return;
      }

      var playerName = args[0].Trim();
      BasePlayer member = BasePlayerEx.FindByNameOrId(playerName);

      if (member == null)
      {
        SendReply(player, Messages.NoSuchPlayer, playerName);
        return;
      }

      if (group.HasMember(member))
      {
        SendReply(player, Messages.CannotAddMemberAlreadyMemberOfGroup, member.displayName, group.Name);
        return;
      }

      group.AddMember(member);
      SendReply(player, Messages.MemberAdded, member.displayName, group.Name);
    }
  }
}
