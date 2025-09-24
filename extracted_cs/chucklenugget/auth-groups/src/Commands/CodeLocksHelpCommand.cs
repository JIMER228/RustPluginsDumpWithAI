namespace Oxide.Plugins
{
  using System.Text;

  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupCodeLocksHelpCommand(BasePlayer player)
    {
      var sb = new StringBuilder();

      sb.AppendLine("The following commands can be sent to locks:");
      sb.AppendLine("  <color=#ffd479>/ag NAME codelocks code NNNN</color> Change the master code on all codelocks");
      sb.AppendLine("  <color=#ffd479>/ag NAME codelocks guestcode NNNN</color> Change the guest code on all codelocks");
      sb.AppendLine("  <color=#ffd479>/ag NAME codelocks lock|unlock</color> Lock or unlock all codelocks");

      SendReply(player, sb.ToString());
    }
  }
}
