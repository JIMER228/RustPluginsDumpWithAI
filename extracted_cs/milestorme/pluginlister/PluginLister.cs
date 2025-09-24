using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("PluginLister", "Milestorme", "1.3.13")]
    [Description("Lists installed plugins and sends the data to a Discord webhook.")]
    public class PluginLister : CovalencePlugin
    {
        private PluginConfig config;
        private const string CurrentConfigVersion = "1.1";

        private class PluginConfig
        {
            public bool EnablePlugin { get; set; } = true;
            public bool EnableDiscordWebhook { get; set; } = true;
            public string WebhookUrl { get; set; } = "YOUR_DISCORD_WEBHOOK_URL_HERE";
            public int CommandCooldownSeconds { get; set; } = 30;
            public string ConfigVersion { get; set; } = CurrentConfigVersion;
        }

        private Dictionary<string, DateTime> cooldowns = new Dictionary<string, DateTime>();

        private static readonly Dictionary<string, string> Localization = new Dictionary<string, string>
        {
            { "NoPermission", "You do not have permission to use this command." },
            { "PluginDisabled", "This plugin is currently disabled." },
            { "NoPlugins", "No plugins are currently installed." },
            { "CooldownMessage", "You must wait {0} seconds before using this command again." },
            { "PluginsListed", "Installed Plugins ({0}):\n{1}" }
        };

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<PluginConfig>();
                if (config == null) throw new Exception("Configuration file is null");

                // Logging config version to verify it is loaded correctly
                Puts("Config loaded successfully.");
                Puts("Config Version: " + config.ConfigVersion);

                if (config.ConfigVersion != CurrentConfigVersion)
                {
                    Puts("Updating configuration to match the latest version.");
                    UpdateConfig();
                }
            }
            catch
            {
                Puts("Configuration file is corrupted or missing, creating a new one.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig() => Config.WriteObject(config, true);

        private void UpdateConfig()
        {
            if (string.IsNullOrEmpty(config.WebhookUrl))
                config.WebhookUrl = "YOUR_DISCORD_WEBHOOK_URL_HERE";
            if (config.CommandCooldownSeconds <= 0)
                config.CommandCooldownSeconds = 30;

            config.ConfigVersion = CurrentConfigVersion;
            SaveConfig();
        }

        private void ConditionalPrint(string message)
        {
            // Always print the message to the server console
            Puts(message);
        }

        private void CreatePermissionGroup()
        {
            if (!permission.GroupExists("pluginlister.admin"))
            {
                permission.CreateGroup("pluginlister.admin", "admin", 0);
                ConditionalPrint("Created 'pluginlister.admin' permission group.");
            }

            if (!permission.PermissionExists("pluginlister.listplugins"))
            {
                permission.RegisterPermission("pluginlister.listplugins", this);
                ConditionalPrint("Registered 'pluginlister.listplugins' permission.");
            }
        }

        [Command("listplugins", "pluginlister.listplugins")]
        private void ListPluginsCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission("pluginlister.admin") && !player.IsAdmin)
            {
                player.Reply(Localization["NoPermission"]);
                return;
            }

            if (!config.EnablePlugin)
            {
                player.Reply(Localization["PluginDisabled"]);
                return;
            }

            if (cooldowns.ContainsKey(player.Id) && cooldowns[player.Id] > DateTime.Now)
            {
                var remaining = (cooldowns[player.Id] - DateTime.Now).TotalSeconds;
                player.Reply(string.Format(Localization["CooldownMessage"], Math.Ceiling(remaining)));
                return;
            }

            cooldowns[player.Id] = DateTime.Now.AddSeconds(config.CommandCooldownSeconds);

            var plugins = Interface.Oxide.RootPluginManager.GetPlugins().ToList();
            if (!plugins.Any())
            {
                player.Reply(Localization["NoPlugins"]);
                return;
            }

            var pluginNames = plugins
                .Select((p, i) => $"{i + 1}. {p.Title}") // Numbering the plugins
                .ToList();
            var pluginList = string.Join("\n", pluginNames);
            player.Reply(string.Format(Localization["PluginsListed"], pluginNames.Count, pluginList));

            if (config.EnableDiscordWebhook)
            {
                SendToDiscord(plugins);
            }
        }

        private void SendToDiscord(List<Plugin> plugins)
        {
            if (string.IsNullOrEmpty(config.WebhookUrl) || config.WebhookUrl == "YOUR_DISCORD_WEBHOOK_URL_HERE")
            {
                ConditionalPrint("Webhook URL is not configured or is using the default placeholder.");
                return;
            }

            var pluginDetails = plugins.Select((p, i) => new { Number = i + 1, Name = p.Title, Version = p.Version }).ToList();
            var embed = new
            {
                embeds = new[] 
                {
                    new 
                    {
                        title = "Installed Plugins",
                        description = string.Join("\n", pluginDetails.Select(p => $"{p.Number}. **{p.Name}** v{p.Version}")),
                        color = 3447003
                    }
                }
            };

            SendDiscordPayload(JsonConvert.SerializeObject(embed));
        }

        private void SendDiscordPayload(string payload, int retryCount = 0)
        {
            var headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };

            webrequest.Enqueue(
                config.WebhookUrl,
                payload,
                (code, response) =>
                {
                    if (code != 200 && code != 204)
                    {
                        ConditionalPrint($"Failed to send data to Discord. Response code: {code}. Response: {response}");

                        if (retryCount < 3) // Retry up to 3 times
                        {
                            timer.Once(5f, () => SendDiscordPayload(payload, retryCount + 1));
                        }
                    }
                    else
                    {
                        ConditionalPrint("Successfully sent data to Discord!");
                    }
                },
                this,
                Oxide.Core.Libraries.RequestMethod.POST,
                headers
            );
        }

        private void OnServerInitialized()
        {
            CreatePermissionGroup();
        }
    }
}
