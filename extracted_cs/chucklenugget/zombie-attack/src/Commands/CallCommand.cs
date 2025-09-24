namespace Oxide.Plugins
{
  public partial class ZombieAttack : RustPlugin
  {
    void OnCallCommand(BasePlayer player, string[] args)
    {
      if (args.Length > 1)
      {
        SendReply(player, "Usage: <color=#ffd479>/za call [NAME]</color>");
        return;
      }

      if (Settings.Locations.Count == 0)
      {
        SendReply(player, "Can't send an attack, because no locations have been defined!");
        return;
      }

      ZombieAttackLocation location;

      if (args.Length == 0)
      {
        location = GetRandomAttackLocation();
      }
      else
      {
        string name = args[0].ToLowerInvariant();
        if (!Settings.Locations.TryGetValue(name, out location))
        {
          SendReply(player, $"No attack location named <color=#ffd479>{name}</color> has been defined.");
          return;
        }
      }

      SendReply(player, $"Sending attack to <color=#ffd479>{location}</color>!");
      BeginAttack(location);
    }
  }
}