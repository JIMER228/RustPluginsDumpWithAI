namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class AuthGroups : RustPlugin
  {
    class ManagedToolCupboard : ManagedEntity
    {
      BuildingPrivlidge Cupboard;

      public ManagedToolCupboard(BuildingPrivlidge cupboard)
        : base(cupboard)
      {
        Cupboard = cupboard;
      }

      public override void Authorize(BasePlayer player)
      {
        Cupboard.authorizedPlayers.Add(CreateAuthListEntry(player));
        Cupboard.SendNetworkUpdate();
      }

      public override void Authorize(IEnumerable<BasePlayer> players)
      {
        Cupboard.authorizedPlayers.AddRange(players.Select(player => CreateAuthListEntry(player)));
        Cupboard.SendNetworkUpdate();
      }

      public override void Deauthorize(BasePlayer player)
      {
        Cupboard.authorizedPlayers.RemoveAll(entry => entry.userid == player.userID);
        Cupboard.SendNetworkUpdate();
      }

      public override void Deauthorize(IEnumerable<BasePlayer> players)
      {
        Cupboard.authorizedPlayers.RemoveAll(entry => players.Any(player => player.userID == entry.userid));
        Cupboard.SendNetworkUpdate();
      }

      public override void DeauthorizeAll()
      {
        Cupboard.authorizedPlayers.Clear();
        Cupboard.SendNetworkUpdate();
      }

      public override BasePlayer[] GetAuthorizedPlayers()
      {
        return Cupboard.authorizedPlayers.Select(entry => BasePlayer.FindByID(entry.userid)).ToArray();
      }

      public override void SetAuthorizedPlayers(IEnumerable<BasePlayer> players)
      {
        Cupboard.authorizedPlayers.Clear();
        Cupboard.authorizedPlayers.AddRange(players.Select(player => CreateAuthListEntry(player)));
        Cupboard.SendNetworkUpdate();
      }

      public override bool IsAuthorized(BasePlayer player)
      {
        return Cupboard.IsAuthed(player);
      }
    }
  }
}
