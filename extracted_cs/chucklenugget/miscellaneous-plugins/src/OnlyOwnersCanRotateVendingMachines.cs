namespace Oxide.Plugins
{
  [Info("OnlyOwnersCanRotateVendingMachines", "chucklenugget", "0.1.0")]
  public partial class OnlyOwnersCanRotateVendingMachines : RustPlugin
  {
    object OnRotateVendingMachine(VendingMachine machine, BasePlayer player)
    {
      if (machine == null || player == null)
        return null;

      if (player.userID != machine.OwnerID)
        return false;

      return null;
    }
  }
}