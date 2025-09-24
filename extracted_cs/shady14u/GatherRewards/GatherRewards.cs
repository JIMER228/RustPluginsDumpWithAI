using Oxide.Core.Plugins;


//plugin.merge -c -m -p ./merge.json

namespace Oxide.Plugins
{
    [Info("Gather Rewards", "Shady14u", "1.7.1")]
    [Description("Earn rewards through Economics/Server Rewards for killing and gathering")]
    //Define:FileOrder=1
    public partial class GatherRewards : RustPlugin
    {
        private const string _version = "1.7.0";

        private string _resource;

        [PluginReference] private Plugin Economics, ServerRewards, Friends, Clans, UINotify;
    }
}