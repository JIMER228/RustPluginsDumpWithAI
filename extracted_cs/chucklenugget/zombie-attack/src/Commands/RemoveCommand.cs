namespace Oxide.Plugins
{
  public partial class ZombieAttack : RustPlugin
  {
    void OnRemoveCommand(BasePlayer player, string[] args)
    {
      if (args.Length != 1)
      {
        SendReply(player, "Usage: <color=#ffd479>/za remove NAME</color>");
        return;
      }

      string name = args[0].ToLowerInvariant();

      ZombieAttackLocation location;
      if (!Settings.Locations.TryGetValue(name, out location))
      {
        SendReply(player, $"No location named <color=#ffd479>{name}</color> has been defined.");
        return;
      }

      Settings.Locations.Remove(name);
      SendReply(player, $"Removed {location} as an attack location.");
    }
  }
}