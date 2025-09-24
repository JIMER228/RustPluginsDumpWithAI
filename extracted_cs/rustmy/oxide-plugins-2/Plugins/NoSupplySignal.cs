namespace Oxide.Plugins
{
    [Info("NoSupplySignal", "Wulf/lukespragg", 0.1, ResourceId = 0)]
    [Description("Prevents supply drops triggering from supply signals")]

    class NoSupplySignal : RustPlugin
    {
        void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if (entity.name.Contains("signal")) entity.KillMessage();
        }
    }
}
