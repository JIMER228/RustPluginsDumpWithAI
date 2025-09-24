namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupSyncCommand(BasePlayer player, string groupName)
    {
      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      group.SynchronizeAll();
      SendReply(player, Messages.GroupSynchronized, group.Name, group.MemberIds.Count);
    }
  }
}
