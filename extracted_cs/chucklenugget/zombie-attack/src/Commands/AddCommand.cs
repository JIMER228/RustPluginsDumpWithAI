namespace Oxide.Plugins
{
  using System;
  using UnityEngine;

  public partial class ZombieAttack : RustPlugin
  {
    void OnAddCommand(BasePlayer player, string[] args)
    {
      if (args.Length != 2)
      {
        SendReply(player, "Usage: <color=#ffd479>/za add NAME RADIUS</color>");
        return;
      }

      Vector3 position;
      if (!TryFindPointOnNavMesh(player.transform.position, out position))
      {
        SendReply(player, $"Couldn't find a position on the navmesh near your location. Please try again.");
        return;
      }

      string name = args[0].ToLowerInvariant();
      float radius;

      try
      {
        radius = Convert.ToSingle(args[1]);
      }
      catch
      {
        SendReply(player, $"Radius must be a valid number between 0 and {MAX_SPAWN_RADIUS}");
        return;
      }

      var location = new ZombieAttackLocation(name, position, radius);
      Settings.Locations[name] = location;

      SendReply(player, $"Added <color=#ffd479>{location}</color> as an attack location with a spawn radius of <color=#ffd479>{location.Radius}</color>.");
    }
  }
}