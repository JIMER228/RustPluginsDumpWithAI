using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("LuckyBoxNotifier", "herbs.acab", "1.2.7")]
    [Description("Notifies via Discord when a new LuckyBox is spawned and when it is found.")]
    public class LuckyBoxNotifier : RustPlugin
    {
        [PluginReference]
        private Plugin LuckyBox;

        private ConfigData configData;

        private class ConfigData
        {
            public string WebhookUrl { get; set; }
            public string AdminWebhookUrl { get; set; }
            public string AdminFindWebhookUrl { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file...");
            configData = new ConfigData
            {
                WebhookUrl = "YOUR_WEBHOOK_URL_HERE",
                AdminWebhookUrl = "YOUR_ADMIN_WEBHOOK_URL_HERE",
                AdminFindWebhookUrl = "YOUR_ADMIN_FIND_WEBHOOK_URL_HERE"
            };
            SaveConfig();
        }

        private void OnServerInitialized()
        {
            LoadConfigData();

            // Check if the LuckyBox plugin is loaded
            if (LuckyBox == null)
            {
                PrintError("LuckyBox plugin is not loaded. This plugin requires the LuckyBox plugin to function.");
                return;
            }
        }

        private void LoadConfigData()
        {
            configData = Config.ReadObject<ConfigData>();
            if (configData == null)
            {
                LoadDefaultConfig();
            }
        }

        private void OnLuckyBoxSpawned(Vector3 position, string secretKey)
        {
            PrintWarning($"LuckyBox spawned at {position} with secret key: {secretKey}");
            // Notify regular users with an embed
            SendDiscordEmbed(configData.WebhookUrl, "New LuckyBox Spawned", "A new LuckyBox has spawned on the map!");

            // Notify admins with an embed
            SendDiscordEmbed(configData.AdminWebhookUrl, "New LuckyBox Spawned", $"A new LuckyBox has spawned with the secret key: {secretKey}. Use /tpbox to teleport yourself to the luckybox!");
        }

        private void OnLuckyBoxFound(BasePlayer player, Vector3 position, string secretKey)
        {
            PrintWarning($"{player.displayName} found LuckyBox at {position} with secret key: {secretKey}");
            // Notify admins with an embed
            SendDiscordEmbed(configData.AdminFindWebhookUrl, "LuckyBox Found", $"{player.displayName} has found a LuckyBox with the secret key: {secretKey}. Use /tpbox to teleport yourself to the luckybox!");
        }

        private void SendDiscordEmbed(string webhookUrl, string title, string description)
        {
            PrintWarning($"Sending Discord embed to {webhookUrl}: {title} - {description}");
            
            var embed = new
            {
                embeds = new[]
                {
                    new
                    {
                        title = title,
                        description = description,
                        color = 3447003 // Blue color
                    }
                }
            };

            var payload = JsonConvert.SerializeObject(embed);

            webrequest.Enqueue(webhookUrl, payload, (code, response) =>
            {
                if (code != 204)
                {
                    PrintError($"Failed to send Discord embed. Code: {code}, Response: {response}");
                }
                else
                {
                    PrintWarning("Discord embed sent successfully.");
                }
            }, this, RequestMethod.POST, new Dictionary<string, string> { { "Content-Type", "application/json" } });
        }

        private void SaveConfig()
        {
            Config.WriteObject(configData, true);
        }
    }
}
