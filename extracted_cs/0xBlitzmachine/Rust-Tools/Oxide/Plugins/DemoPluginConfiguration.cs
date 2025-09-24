using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Text;
using UnityEngine;


namespace Oxide.Plugins;

[Info("Configuration", "Blitzmachine", "0.0.2")]
[Description("Blueprint/Demo of an Oxide Configuration")]
public class DemoPluginConfiguration : RustPlugin
{
    #region Setup
    private PluginConfiguration pluginConfig;
    #endregion

    #region Configuration

    #region Plugin Configuration Template
    private class PluginConfiguration
    {
        [JsonProperty(propertyName: "X Related Settings")]
        public SomeXRelatedSettings XSettings { get; set; }

        [JsonProperty(propertyName: "Y Related Settings")]
        public SomeYRelatedSettings YSettings { get; set; }

        public static PluginConfiguration GetDefaultConfiguration() => new PluginConfiguration
        {
            XSettings = new SomeXRelatedSettings
            {
                Steam64Id = 0U
            },
            YSettings = new SomeYRelatedSettings
            {
                NeedsAdminPermission = false,
                PlayerInventory = new SomeZRelatedSettings
                {
                    InventoryContainer = { },
                    InventoryBeltContainer = { },
                    InventoryWearContainer = { }
                }
            }
        };
    }


    private class SomeXRelatedSettings
    {
        [JsonProperty(propertyName: "Steam64 ID")]
        public ulong Steam64Id { get; set; }
    }

    private class SomeYRelatedSettings
    {
        [JsonProperty(propertyName: "Needs Admin Permission")]
        public bool NeedsAdminPermission { get; set; }

        [JsonProperty(propertyName: "Player Inventory Settings")]
        public SomeZRelatedSettings PlayerInventory { get; set; }
    }

    private class SomeZRelatedSettings
    {
        [JsonProperty(propertyName: "Inventory Container")]
        public Dictionary<string, int> InventoryContainer { get; set; }

        [JsonProperty(propertyName: "Inventory Wear Container")]
        public List<string> InventoryWearContainer { get; set; }

        [JsonProperty(propertyName: "Inventory Belt Container")]
        public List<string> InventoryBeltContainer { get; set; }
    }
    #endregion

    protected override void SaveConfig() => Config.WriteObject(pluginConfig, true);
    protected override void LoadDefaultConfig() => pluginConfig = PluginConfiguration.GetDefaultConfiguration();
    protected override void LoadConfig()
    {
        base.LoadConfig();
        try
        {
            pluginConfig = Config.ReadObject<PluginConfiguration>();
        }
        catch (Exception e)
        {
            StringBuilder builder = new StringBuilder("Configuration file is corrupt - (Invalid JSON Format?)");
            builder.AppendLine(e.Message);

            PrintError(builder.ToString());
            PrintWarning("Configuration Object is using default values.");
            LoadDefaultConfig();
        }
    }

    #endregion
    #region Chat Commands
    [ChatCommand("test")]
    private void CmdPlayground(BasePlayer player, string command, string[] args)
    {
        var builder = new StringBuilder("Your command executed! \n\n");

        foreach (KeyValuePair<string, int> pair in pluginConfig.YSettings.PlayerInventory.InventoryContainer)
        {
            builder.AppendLine(pair.Key + " - Amount: " + pair.Value + "\n");
        }

        player.ChatMessage(builder.ToString());
    }

    #endregion
}