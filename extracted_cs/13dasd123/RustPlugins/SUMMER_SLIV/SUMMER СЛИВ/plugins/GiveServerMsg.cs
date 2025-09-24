// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
namespace Oxide.Plugins
{
    [Info("GiveServerMsg", "", "0.1", ResourceId = 2336)]

    class GiveServerMsg : RustPlugin
    {
        object OnServerMessage(string m, string n) => m.Contains("gave") && n == "SERVER" ? (object)true : null;
    }
}
