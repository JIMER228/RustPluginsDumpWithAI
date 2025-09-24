namespace Oxide.Plugins
{
  using System.Linq;

  public partial class AuthGroups : RustPlugin
  {
    [ChatCommand("ag")]
    void OnAuthGroupCommand(BasePlayer player, string chatCommand, string[] args)
    {
      if (args.Length == 0)
      {
        if (PendingInteractions.ContainsKey(player.UserIDString))
          OnAuthGroupCancelCommand(player);
        else
          OnAuthGroupListCommand(player);
        return;
      }

      switch (args[0].ToLowerInvariant())
      {
        case "help":
          OnAuthGroupHelpCommand(player);
          return;
        case "cancel":
          OnAuthGroupCancelCommand(player);
          return;
        case "create":
          OnAuthGroupCreateCommand(player, args[1].Trim());
          return;
        case "delete":
          OnAuthGroupDeleteCommand(player, args[1].Trim());
          return;
        default:
          break;
      }

      string groupName = args[0].Trim();

      if (args.Length == 1)
      {
        OnAuthGroupShowCommand(player, groupName);
        return;
      }

      string command = args[1].ToLowerInvariant();
      string[] commandArgs = args.Skip(2).ToArray();

      switch (command)
      {
        case "show":
          OnAuthGroupShowCommand(player, groupName);
          break;
        case "add":
          OnAuthGroupAddCommand(player, groupName, commandArgs);
          break;
        case "remove":
          OnAuthGroupRemoveCommand(player, groupName, commandArgs);
          break;
        case "promote":
          OnAuthGroupPromoteCommand(player, groupName, commandArgs);
          break;
        case "demote":
          OnAuthGroupDemoteCommand(player, groupName, commandArgs);
          break;
        case "sync":
          OnAuthGroupSyncCommand(player, groupName);
          return;
        case "turrets":
          OnAuthGroupTurretsCommand(player, groupName, commandArgs);
          break;
        case "codelocks":
          OnAuthGroupCodeLocksCommand(player, groupName, commandArgs);
          break;
        default:
          OnAuthGroupHelpCommand(player);
          break;
      }
    }
  }
}
