
namespace Oxide.Plugins
{
    [Info("PreventPickupOnWorldIOEntities", "Death", "1.0.0")]
    class PreventPickupOnWorldIOEntities : RustPlugin
    {
        object CanPickupEntity(BasePlayer player, IOEntity entity)
        {
            if (entity != null && entity.OwnerID == 0)
            {
                return false;
            }

            return null;
        }
    }
}