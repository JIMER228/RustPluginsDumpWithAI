namespace Oxide.Plugins
{
  public partial class AuthGroups : RustPlugin
  {
    abstract class Interaction
    {
      public BasePlayer Player { get; private set; }
      public AuthGroup Group { get; private set; }

      public Interaction(BasePlayer player, AuthGroup group)
      {
        Player = player;
        Group = group;
      }

      public abstract void Handle(HitInfo hit);
    }
  }
}
