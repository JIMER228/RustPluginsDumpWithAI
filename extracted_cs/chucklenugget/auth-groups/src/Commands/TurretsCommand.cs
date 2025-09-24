namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    void OnAuthGroupTurretsCommand(BasePlayer player, string groupName, string[] args)
    {
      string command = (args.Length == 0) ? null : args[0].ToLowerInvariant();

      if (command == null)
      {
        OnAuthGroupTurretsHelpCommand(player);
        return;
      }

      AuthGroup group = Groups.GetByOwnerOrManager(player, groupName);

      if (group == null)
      {
        SendReply(player, Messages.YouAreNotOwnerOrManagerOfGroup, groupName);
        return;
      }

      var turrets = group.GetAllManagedEntitiesOfType<ManagedAutoTurret>();

      if (turrets.Length == 0)
      {
        SendReply(player, Messages.CannotSendTurretsCommandNoTurretsInGroup, group.Name);
        return;
      }

      switch (command)
      {
        case "on":
          foreach (ManagedAutoTurret turret in turrets)
            turret.SetIsOnline(true);
          SendReply(player, Messages.AllTurretsSetOn, group.Name);
          break;

        case "off":
          foreach (ManagedAutoTurret turret in turrets)
            turret.SetIsOnline(false);
          SendReply(player, Messages.AllTurretsSetOff, group.Name);
          break;

        case "hostile":
          foreach (ManagedAutoTurret turret in turrets)
            turret.SetIsPeacekeeper(false);
          SendReply(player, Messages.AllTurretsSetHostile, group.Name);
          break;

        case "peacekeeper":
          foreach (ManagedAutoTurret turret in turrets)
            turret.SetIsPeacekeeper(true);
          SendReply(player, Messages.AllTurretsSetPeacekeeper, group.Name);
          break;

        case "lock":
          group.AddFlag(AuthGroupFlag.TurretsLocked);
          SendReply(player, Messages.AllTurretsLocked, group.Name);
          break;

        case "unlock":
          group.RemoveFlag(AuthGroupFlag.TurretsLocked);
          SendReply(player, Messages.AllTurretsUnlocked, group.Name);
          break;

        default:
          OnAuthGroupTurretsHelpCommand(player);
          break;
      }
    }
  }
}
