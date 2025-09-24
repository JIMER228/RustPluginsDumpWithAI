namespace Oxide.Plugins
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public partial class AuthGroups : RustPlugin
  {
    class ManagedAutoTurret : ManagedEntity
    {
      AutoTurret Turret;

      public ManagedAutoTurret(AutoTurret turret)
        : base(turret)
      {
        Turret = turret;
      }

      public override void Authorize(BasePlayer player)
      {
        Turret.authorizedPlayers.Add(CreateAuthListEntry(player));
        Turret.SendNetworkUpdate();
      }

      public override void Authorize(IEnumerable<BasePlayer> players)
      {
        Turret.authorizedPlayers.AddRange(players.Select(player => CreateAuthListEntry(player)));
        Turret.SendNetworkUpdate();
      }

      public override void Deauthorize(BasePlayer player)
      {
        Turret.authorizedPlayers.RemoveAll(entry => entry.userid == player.userID);
        Turret.SendNetworkUpdate();
        Turret.SetTarget(null);
      }

      public override void Deauthorize(IEnumerable<BasePlayer> players)
      {
        Turret.authorizedPlayers.RemoveAll(entry => players.Any(player => player.userID == entry.userid));
        Turret.SendNetworkUpdate();
        Turret.SetTarget(null);
      }

      public override void DeauthorizeAll()
      {
        Turret.authorizedPlayers.Clear();
        Turret.SetIsOnline(false);
        Turret.SendNetworkUpdate();
        Turret.SetTarget(null);
      }

      public override BasePlayer[] GetAuthorizedPlayers()
      {
        return Turret.authorizedPlayers.Select(entry => BasePlayer.FindByID(entry.userid)).ToArray();
      }

      public override void SetAuthorizedPlayers(IEnumerable<BasePlayer> players)
      {
        Turret.authorizedPlayers.Clear();
        Turret.authorizedPlayers.AddRange(players.Select(player => CreateAuthListEntry(player)));
        Turret.SendNetworkUpdate();
        Turret.SetTarget(null);
      }

      public override bool IsAuthorized(BasePlayer player)
      {
        return Turret.IsAuthed(player);
      }

      public void SetIsOnline(bool online)
      {
        Turret.SetIsOnline(online);
        Turret.SendNetworkUpdate();
      }

      public void SetIsPeacekeeper(bool peacekeeper)
      {
        Turret.SetPeacekeepermode(peacekeeper);
        Turret.SendNetworkUpdate();
        Turret.SetTarget(null);
      }
    }
  }
}
