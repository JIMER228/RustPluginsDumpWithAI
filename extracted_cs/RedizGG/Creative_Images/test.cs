// testing

using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    public class DynamicFunctions : CovalencePlugin
    {
        [ConsoleCommand("test")]
        private void TestCommand(ConsoleSystem.Arg arg)
        {
            if (arg == null || arg.Connection != null)
            {
                Puts("This command can only be run from the server console.");
                return;
            }

            Puts("Dynamic test command executed!");
        }
    }
}
