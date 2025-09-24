namespace Oxide.Plugins
{
    [Info("ZealRatesController", "Kira", "1.0.0")]
    public class ZealRatesController : RustPlugin
    {
        private void OnExcavatorGather(ExcavatorArm arm, Item item)
        {
            item.amount *= 10; 
        }
    }
}