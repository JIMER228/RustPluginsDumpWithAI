// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
namespace Oxide.Plugins
{
    [Info("HelloWorldCs", "Bas", "0.1.0")]
    class CsHelloWorld : RustPlugin
    {
        void Init()
        {
            System.Console.WriteLine("Hello World from Cs");
        }
    }
}
