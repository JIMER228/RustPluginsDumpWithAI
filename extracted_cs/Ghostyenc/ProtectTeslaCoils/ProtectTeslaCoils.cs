/*           ________               __
           / ____/ /_  ____  _____/ /___  __
          / / __/ __ \/ __ \/ ___/ __/ / / /
         / /_/ / / / / /_/ (__  ) /_/ /_/ /
         \____/_/ /_/\____/____/\__/\__, /
                                   /____/

*/
using Oxide.Core.Plugins;
using Rust;

namespace Oxide.Plugins
{
    [Info("ProtectTeslaCoils", "Ghosty", "1.0.0")]
    class ProtectTeslaCoils : RustPlugin
    {
        private const string TeslaCoilPrefab =
            "assets/prefabs/npc/autoturret/tesla_coil/teslacoil.deployed.prefab";

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity.ShortPrefabName == TeslaCoilPrefab)
            {
                return false;
            }

            return null;
        }
    }
}
