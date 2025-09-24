namespace Oxide.Plugins
{
    [Info("PlayerDeath", "sami37", "1.0.0")]
    public class PlayerDeath : RustPlugin
    {
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            BasePlayer player = entity?.ToPlayer();
            if (player != null)
                DestroyEntity(player);
        }

        private void DestroyEntity(BasePlayer player)
        {
            if(player.net.connection.authLevel == 0)
                ConVar.Entity.DeleteBy(player.userID);
        }
    }
}