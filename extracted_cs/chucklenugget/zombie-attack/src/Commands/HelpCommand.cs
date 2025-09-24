namespace Oxide.Plugins
{
  using System.Text;

  public partial class ZombieAttack : RustPlugin
  {
    void OnHelpCommand(BasePlayer player)
    {
      var sb = new StringBuilder();

      sb.AppendLine("Available commands:");
      sb.AppendLine("<color=#ffd479>/za interval MINUTES</color> Set the interval for periodic attacks");
      sb.AppendLine("<color=#ffd479>/za list</color> List the current attack locations");
      sb.AppendLine("<color=#ffd479>/za add NAME</color> Add your current position as an attack location");
      sb.AppendLine("<color=#ffd479>/za remove NAME</color> Remove an attack location from the list");
      sb.AppendLine("<color=#ffd479>/za clear</color> Remove all attack locations");
      sb.AppendLine("<color=#ffd479>/za call [NAME]</color> Call an attack immediately");
      sb.AppendLine("<color=#ffd479>/za help</color> Show this message");

      SendReply(player, sb.ToString());
    }
  }
}