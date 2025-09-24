using Oxide.Ext.Discord.Interfaces;
using Oxide.Plugins;

namespace DiscordChatPlugin.Plugins;

[Info("Discord Chat", "MJSU", "3.0.6")]
[Description("Allows chatting between discord and game server")]
public partial class DiscordChat : CovalencePlugin, IDiscordPlugin, IDiscordPool
{
    
}