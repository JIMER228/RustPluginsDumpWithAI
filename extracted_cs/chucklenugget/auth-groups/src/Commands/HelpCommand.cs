namespace Oxide.Plugins
{
  using System.Text;

  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupHelpCommand(BasePlayer player)
    {
      var sb = new StringBuilder();

      sb.AppendLine($"The following commands are available:");
      sb.AppendLine("  <color=#ffd479>/ag</color> List auth groups you are managing");
      sb.AppendLine("  <color=#ffd479>/ag create NAME</color> Create a new auth group");
      sb.AppendLine("  <color=#ffd479>/ag delete NAME</color> Delete an auth group you own");
      sb.AppendLine("  <color=#ffd479>/ag NAME</color> Show info about an auth group");
      sb.AppendLine("  <color=#ffd479>/ag NAME add|remove</color> Add/remove items to/from an auth group");
      sb.AppendLine("  <color=#ffd479>/ag NAME add|remove PLAYER</color> Add/remove a player to/from an auth group");
      sb.AppendLine("  <color=#ffd479>/ag NAME promote|demote PLAYER</color> Add/remove a player as manager of an auth group");
      sb.AppendLine("  <color=#ffd479>/ag NAME sync</color> Ensure that only group members are auth'd on the items in an auth group");
      sb.AppendLine("  <color=#ffd479>/ag NAME turrets</color> Send a command to all turrets in an auth group");
      sb.AppendLine("  <color=#ffd479>/ag NAME codelocks</color> Send a command to all locks in an auth group");

      SendReply(player, sb.ToString());
    }
  }
}
