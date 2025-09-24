namespace Oxide.Plugins
{
  using System.Text;

  public partial class ZombieAttack : RustPlugin
  {
    void OnListCommand(BasePlayer player)
    {
      if (Settings.Locations.Count == 0)
      {
        SendReply(player, "No attack locations have been defined.");
        return;
      }

      var sb = new StringBuilder();

      sb.AppendLine("The following attack locations have been defined:");
      foreach (ZombieAttackLocation location in Settings.Locations.Values)
        sb.AppendLine($"<color=#ffd479>{location.Name}:</color> {location.Position}");

      SendReply(player, sb.ToString());
    }
  }
}