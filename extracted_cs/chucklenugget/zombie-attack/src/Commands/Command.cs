namespace Oxide.Plugins
{
  using System.Linq;

  public partial class ZombieAttack : RustPlugin
  {
    [ChatCommand("za")]
    void OnCommand(BasePlayer player, string command, string[] args)
    {
      if (!permission.UserHasPermission(player.UserIDString, PERM_ADMIN))
      {
        SendReply(player, "You don't have permission to do that.");
        return;
      }

      if (args.Length == 0)
      {
        OnHelpCommand(player);
        return;
      }

      var restArgs = args.Skip(1).ToArray();

      switch (args[0].ToLowerInvariant())
      {
        case "add":
          OnAddCommand(player, restArgs);
          return;
        case "remove":
          OnRemoveCommand(player, restArgs);
          return;
        case "list":
          OnListCommand(player);
          return;
        case "clear":
          OnClearCommand(player);
          return;
        case "interval":
          OnIntervalCommand(player, restArgs);
          return;
        case "number":
          OnNumberCommand(player, restArgs);
          return;
        case "call":
          OnCallCommand(player, restArgs);
          return;
        default:
          OnHelpCommand(player);
          return;
      }
    }
  }
}