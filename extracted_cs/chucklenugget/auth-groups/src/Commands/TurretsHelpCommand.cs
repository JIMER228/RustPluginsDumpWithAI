namespace Oxide.Plugins
{
  using System.Text;

  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupTurretsHelpCommand(BasePlayer player)
    {
      var sb = new StringBuilder();

      sb.AppendLine("The following commands can be sent to turrets:");
      sb.AppendLine("  <color=#ffd479>/ag NAME turrets on|off</color> Turn all turrets on or off");
      sb.AppendLine("  <color=#ffd479>/ag NAME turrets peacekeeper|hostile</color> Turn on or off peacekeeper mode");
      sb.AppendLine("  <color=#ffd479>/ag NAME turrets lock|unlock</color> Restrict opening or authing on turrets to group managers only");

      SendReply(player, sb.ToString());
    }
  }
}
