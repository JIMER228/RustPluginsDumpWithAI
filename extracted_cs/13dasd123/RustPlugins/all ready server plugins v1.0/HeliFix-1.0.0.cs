namespace Oxide.Plugins
{
    [Info("HeliFix", "Tryhard", "1.0.0")]
    [Description("Fixes heli gibs and fire")]
    public class HeliFix : RustPlugin
    {
        private void OnServerInitialized()
        {
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                var scrapheli = entity as ScrapTransportHelicopter;
                var mini = entity as MiniCopter;
            }
        }

        private void OnEntitySpawned(ScrapTransportHelicopter entity)
        {
            entity.explosionEffect.guid = null;
            entity.serverGibs.guid = null;
            entity.fireBall.guid = null;
        }

        private void OnEntitySpawned(MiniCopter entity)
        {
            entity.explosionEffect.guid = null;
            entity.serverGibs.guid = null;
            entity.fireBall.guid = null;
        }
    }
}