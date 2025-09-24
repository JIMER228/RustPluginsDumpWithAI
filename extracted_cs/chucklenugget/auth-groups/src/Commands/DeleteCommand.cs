namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupDeleteCommand(BasePlayer player, string groupName)
    {
      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      Groups.Delete(group);
      SendReply(player, Messages.GroupDeleted, group.Name);
    }
  }
}
