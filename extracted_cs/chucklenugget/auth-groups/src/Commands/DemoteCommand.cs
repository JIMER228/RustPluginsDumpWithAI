namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupDemoteCommand(BasePlayer player, string groupName, string[] args)
    {
      if (args.Length == 0)
      {
        SendReply(player, "<color=#ffd479>Usage:</color> /ag NAME demote PLAYER");
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

      if (group.HasOwner(member))
      {
        SendReply(player, Messages.CannotDemoteIsOwnerOfGroup, member.displayName, group.Name);
        return;
      }

      if (!group.HasManager(member))
      {
        SendReply(player, Messages.CannotDemoteNotManagerOfGroup, member.displayName, group.Name);
        return;
      }

      group.Demote(member);
      SendReply(player, Messages.ManagerRemoved, member.displayName, group.Name);
    }
  }
}
