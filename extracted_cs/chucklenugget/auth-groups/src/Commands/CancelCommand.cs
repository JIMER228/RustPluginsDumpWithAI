namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupCancelCommand(BasePlayer player)
    {
      if (PendingInteractions.Remove(player.UserIDString))
        SendReply(player, Messages.InteractionCanceled);
      else
        OnAuthGroupHelpCommand(player);
    }
  }
}
