using Oxide.Ext.Discord.Entities;
using Oxide.Plugins;

namespace DiscordCorePlugin.Data
{
    public class PluginData
    {
        public Hash<string, DiscordInfo> PlayerDiscordInfo = new();
        public Hash<Snowflake, DiscordInfo> LeftPlayerInfo = new();
        public Hash<string, DiscordInfo> InactivePlayerInfo = new();
        public LinkMessageData MessageData;
    }
}