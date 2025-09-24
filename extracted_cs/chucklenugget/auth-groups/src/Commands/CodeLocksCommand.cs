namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupCodeLocksCommand(BasePlayer player, string groupName, string[] args)
    {
      string command = (args.Length == 0) ? null : args[0].ToLowerInvariant();

      if (command == null)
      {
        OnAuthGroupCodeLocksHelpCommand(player);
        return;
      }

      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      var codeLocks = group.GetAllManagedEntitiesOfType<ManagedCodeLock>();

      if (codeLocks.Length == 0)
      {
        SendReply(player, Messages.CannotSendLocksCommandNoLocksInGroup, group.Name);
        return;
      }

      string code = (args.Length < 2) ? null : args[1].Trim();

      switch (command)
      {
        case "code":
          if (code == null || code.Length != 4)
          {
            OnAuthGroupCodeLocksHelpCommand(player);
            break;
          }
          foreach (ManagedCodeLock codeLock in codeLocks)
            codeLock.SetCode(code);
          SendReply(player, Messages.AllLocksCodeChanged, group.Name);
          break;

        case "guestcode":
          if (code == null || code.Length != 4)
          {
            OnAuthGroupCodeLocksHelpCommand(player);
            break;
          }
          foreach (ManagedCodeLock codeLock in codeLocks)
            codeLock.SetGuestCode(code);
          SendReply(player, Messages.AllLocksGuestCodeChanged, group.Name);
          break;

        case "lock":
          foreach (ManagedCodeLock codeLock in codeLocks)
            codeLock.SetIsLocked(true);
          SendReply(player, Messages.AllLocksLocked, group.Name);
          break;

        case "unlock":
          foreach (ManagedCodeLock codeLock in codeLocks)
            codeLock.SetIsLocked(false);
          SendReply(player, Messages.AllLocksUnlocked, group.Name);
          break;

        default:
          OnAuthGroupTurretsHelpCommand(player);
          break;
      }
    }
  }
}
