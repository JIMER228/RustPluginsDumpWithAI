namespace Oxide.Plugins
{
  using System.Collections.Generic;
  using System.Linq;

  public partial class AuthGroups : RustPlugin
  {
    class ManagedCodeLock : ManagedEntity
    {
      CodeLock CodeLock;

      public ManagedCodeLock(CodeLock codeLock)
        : base(codeLock)
      {
        CodeLock = codeLock;
      }

      public override void Authorize(BasePlayer player)
      {
        if (!CodeLock.guestPlayers.Contains(player.userID))
          CodeLock.guestPlayers.Add(player.userID);
        CodeLock.SendNetworkUpdate();
      }

      public override void Authorize(IEnumerable<BasePlayer> players)
      {
        CodeLock.guestPlayers.AddRange(players.Select(player => player.userID));
        CodeLock.SendNetworkUpdate();
      }

      public override void Deauthorize(BasePlayer player)
      {
        CodeLock.guestPlayers.RemoveAll(entry => entry == player.userID);
        CodeLock.SendNetworkUpdate();
      }

      public override void Deauthorize(IEnumerable<BasePlayer> players)
      {
        CodeLock.guestPlayers.RemoveAll(entry => players.Any(player => player.userID == entry));
        CodeLock.SendNetworkUpdate();
      }

      public override void DeauthorizeAll()
      {
        CodeLock.guestPlayers.Clear();
        CodeLock.SendNetworkUpdate();
      }

      public override BasePlayer[] GetAuthorizedPlayers()
      {
        return CodeLock.guestPlayers.Select(entry => BasePlayer.FindByID(entry)).ToArray();
      }

      public override void SetAuthorizedPlayers(IEnumerable<BasePlayer> players)
      {
        CodeLock.guestPlayers.Clear();
        CodeLock.guestPlayers.AddRange(players.Select(player => player.userID));
        CodeLock.SendNetworkUpdate();
      }

      public override bool IsAuthorized(BasePlayer player)
      {
        return CodeLock.whitelistPlayers.Contains(player.userID) || CodeLock.guestPlayers.Contains(player.userID);
      }

      public void SetIsLocked(bool locked)
      {
        PlayEffectAtEntity(locked ? CodeLock.effectLocked : CodeLock.effectUnlocked);
        CodeLock.SetFlag(BaseEntity.Flags.Locked, locked, false);
        CodeLock.SendNetworkUpdate();
      }

      public void SetCode(string code)
      {
        PlayEffectAtEntity(CodeLock.effectCodeChanged);
        CodeLock.code = code;
        CodeLock.SendNetworkUpdate();
      }

      public void SetGuestCode(string code)
      {
        PlayEffectAtEntity(CodeLock.effectCodeChanged);
        CodeLock.guestCode = code;
        CodeLock.SendNetworkUpdate();
      }
    }
  }
}
