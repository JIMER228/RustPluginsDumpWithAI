namespace Oxide.Plugins
{
    [Info("NoDecayCandlesUltra", "Ultra", "1.1.2")]
    [Description("Candles don't get damage by decay")]

    class NoDecayCandlesUltra : RustPlugin
    {
        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity.ShortPrefabName.Contains("candles")) return 0;            
            return null;
        }
    }
}
