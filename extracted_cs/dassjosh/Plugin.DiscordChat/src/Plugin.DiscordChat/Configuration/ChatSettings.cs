using Newtonsoft.Json;
using Oxide.Ext.Discord.Entities;
using Oxide.Plugins;

namespace DiscordChatPlugin.Configuration;

public class ChatSettings
{
    [JsonProperty("Chat Channel ID")]
    public Snowflake ChatChannel { get; set; }

#if RUST
    [JsonProperty("Team Channel ID")]
    public Snowflake TeamChannel { get; set; }

    [JsonProperty("Cards Channel ID")]
    public Snowflake CardsChannel { get; set; }
        
    [JsonProperty("Clans Channel ID")]
    public Snowflake ClansChannel { get; set; }
#endif

    [JsonProperty("Replace Discord User Message With Bot Message")]
    public bool UseBotToDisplayChat { get; set; }

    [JsonProperty("Send Messages From Server Chat To Discord Channel")]
    public bool ServerToDiscord { get; set; }

    [JsonProperty("Send Messages From Discord Channel To Server Chat")]
    public bool DiscordToServer { get; set; }

    [JsonProperty("Add Discord Tag To In Game Messages When Sent From Discord")]
    public string DiscordTag { get; set; }
        
    [JsonProperty("Allow plugins to process Discord to Server Chat Messages")]
    public bool AllowPluginProcessing { get; set; }

    [JsonProperty("Text Replacements")]
    public Hash<string, string> TextReplacements { get; set; }

    [JsonProperty("Unlinked Settings")]
    public UnlinkedSettings UnlinkedSettings { get; set; }

    [JsonProperty("Message Filter Settings")]
    public MessageFilterSettings Filter { get; set; }

    public ChatSettings(ChatSettings settings)
    {
        ChatChannel = settings?.ChatChannel ?? default(Snowflake);
#if RUST
        TeamChannel = settings?.TeamChannel ?? default(Snowflake);
        CardsChannel = settings?.CardsChannel ?? default(Snowflake);
#endif
        UseBotToDisplayChat = settings?.UseBotToDisplayChat ?? true;
        ServerToDiscord = settings?.ServerToDiscord ?? true;
        DiscordToServer = settings?.DiscordToServer ?? true;
        DiscordTag = settings?.DiscordTag ?? "[#5f79d6][Discord][/#]";
        AllowPluginProcessing = settings?.AllowPluginProcessing ?? true;
        TextReplacements = settings?.TextReplacements ?? new Hash<string, string> { ["TextToBeReplaced"] = "ReplacedText" };
        UnlinkedSettings = new UnlinkedSettings(settings?.UnlinkedSettings);
        Filter = new MessageFilterSettings(settings?.Filter);
    }
}