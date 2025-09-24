namespace Oxide.Plugins
{
  using System.Text;

  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupListCommand(BasePlayer player)
    {
      var sb = new StringBuilder();
      AuthGroup[] groups = Groups.GetAllByOwnerOrManager(player);

      sb.AppendLine($"<size=18>AuthGroups</size> v{Version} by chucklenugget");

      if (groups.Length == 0)
      {
        sb.AppendLine("You are not a manager of any auth groups.");
      }
      else
      {
        sb.AppendLine($"You are a manager of {groups.Length} auth group(s):");
        foreach (AuthGroup group in groups)
          sb.AppendLine($"  <color=#ffd479>{group.Name}</color>");
      }

      sb.AppendLine("To learn more about auth groups, type <color=#ffd479>/ag help</color>.");

      SendReply(player, sb.ToString());
    }
  }
}
