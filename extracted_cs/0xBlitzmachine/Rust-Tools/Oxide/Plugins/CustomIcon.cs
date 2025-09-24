using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;
using Network;
using System;

// Same version as collect_vood :: https://umod.org/plugins/custom-icon
// Just recreated for education reasons.

namespace Oxide.Plugins;

[Info("Custom Icon", "Blitzmachine", "1.0.0")]
[Description("")]
public class CustomIcon : RustPlugin
{
    #region Definition
    private PluginConfiguration configuration;
    #endregion

    #region Configuration
    private class PluginConfiguration
    {
        [JsonProperty(propertyName: "SteamID")]
        public ulong SteamId { get; set; }

        public static PluginConfiguration GetDefaultConfig() => new()
        {
            SteamId = 76561198883622649,
        };
    }
    #endregion

    #region Oxide Commands
    [ChatCommand("chat-player")]
    private void TestCommandPlayer(BasePlayer player)
    {
        if (!player.IsAdmin)
            return;

        player.ChatMessage("Personal message!");
    }

    [ChatCommand("chat-server")]
    private void TestCommandServer(BasePlayer player)
    {
        if (!player.IsAdmin)
            return;

        Server.Broadcast("Message to everyone!");
    }
    #endregion

    #region Oxide Hooks

    private void OnBroadcastCommand(string command, object[] args) => ApplySteamIcon(ref command, ref args);
    private void OnSendCommand(Connection cn, string command, object[] args) => ApplySteamIcon(ref command, ref args);
    private void OnSendCommand(List<Connection> cn, string command, object[] args) => ApplySteamIcon(ref command, ref args);

    protected override void LoadDefaultConfig() => configuration = PluginConfiguration.GetDefaultConfig();
    protected override void SaveConfig() => Config.WriteObject(configuration);
    protected override void LoadConfig()
    {
        base.LoadConfig();
        try
        {
            configuration = Config.ReadObject<PluginConfiguration>();
        }
        catch (Exception ex)
        {
            StringBuilder builder = new StringBuilder("Failed to load configuration file - Wrong JSON Format?").AppendLine();
            builder.AppendLine(ex.Message);

            PrintError(builder.ToString());

            PrintWarning("Initializing default configuration object!");
            LoadDefaultConfig();
        }
    }

    #endregion

    #region Internal Helpers
    private void ApplySteamIcon(ref string command, ref object[] args)
    {
        if (string.IsNullOrEmpty(command) || args is null)
            return;

        if (args.Length < 2 || (command != "chat.add" && command != "chat.add2"))
            return;

        ulong providedSteamId;

        if (ulong.TryParse(args[1].ToString(), out providedSteamId) && providedSteamId == 0)
            args[1] = configuration.SteamId;
    }
    #endregion
}
