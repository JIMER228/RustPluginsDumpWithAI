// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
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