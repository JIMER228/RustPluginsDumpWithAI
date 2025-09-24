using Oxide.Ext.Discord.Interfaces;
using Oxide.Ext.Discord.Libraries;
using Oxide.Plugins;

namespace DiscordCorePlugin.Plugins
{
    [Info("Discord Core", "MJSU", "3.0.2")]
    [Description("Creates a link between a player and discord")]
    public partial class DiscordCore : CovalencePlugin, IDiscordPlugin, IDiscordLink
    {

    }
}