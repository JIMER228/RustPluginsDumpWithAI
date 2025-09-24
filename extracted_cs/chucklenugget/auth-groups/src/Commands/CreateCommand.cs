namespace Oxide.Plugins
{
  using System.Text;

  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupCreateCommand(BasePlayer player, string groupName)
    {
      AuthGroup existingGroup = Groups.GetByOwnerOrManager(player, groupName);

      if (existingGroup != null)
      {
        SendReply(player, Messages.CannotCreateGroupOneAlreadyExists, groupName);
        return;
      }

      AuthGroup group = Groups.Create(player, groupName);
      SendReply(player, Messages.GroupCreated, groupName);
    }
  }
}
