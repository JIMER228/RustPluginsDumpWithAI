// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
namespace Oxide.Plugins
{
    [Info("GiveServerMsg", "MR.BUFF", "0.1", ResourceId = 2336)]
    public class GiveServerMsg : RustPlugin
    {
        private object OnServerMessage(string m, string n) => m.Contains("gave") && n == "SERVER" ? (object)true : null;
    }
}
