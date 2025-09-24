// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
namespace Oxide.Plugins
{
    [Info("CorpseDespawn", "chesee - lox", "1.0.0")]
    class CorpseDespawn : RustPlugin
    {
        private void OnEntitySpawned(BaseCorpse corpse)
        {
            corpse.ResetRemovalTime(5.0f);
        }
    }
}