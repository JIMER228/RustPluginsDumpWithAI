namespace Oxide.Plugins
{
    //Define:FileOrder=60
    public partial class ExtraLoot
    {

        private void OnLootSpawn(LootContainer container)
        {
            NextTick(() => { SpawnLoot(container); });
        }

    }
}
