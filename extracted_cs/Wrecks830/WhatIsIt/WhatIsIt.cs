using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Oxide.Core.Libraries;
using UnityEngine;

//ADDED DISCORD REQUEST LOGGING USING /RWHAT ON UNLOGGED ITEMS

namespace Oxide.Plugins
{
    [Info("What Is It", "Wrecks", "1.0.1")]
    [Description("Pair your Custom Item Skins with a Description")]
    public class WhatIsIt : RustPlugin
    {
        private const string SelectEffect = "assets/bundled/prefabs/fx/notice/item.select.fx.prefab";

        public class ItemInfo
        {
            public ulong SkinID;
            public string Description;
        }

        #region Config

        static Configuration config;

        public class Configuration
        {
            [JsonProperty("Discord Webhook")] public string DiscordWebhook = "";

            [JsonProperty("Item Info List")] public List<ItemInfo> ItemInfoList = new();

            public static Configuration DefaultConfig()
            {
                return new Configuration {
                    ItemInfoList = {
                        new ItemInfo {
                            SkinID = 123456789,
                            Description = "This is an <color=green>Example</color> Description"
                        },
                        new ItemInfo {
                            SkinID = 987654321,
                            Description = "This is another <color=blue>Example</color> Description"
                        }
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                PrintWarning("Creating new configuration file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Commands

        [ChatCommand("what")]
        void cmdWhat(BasePlayer player, string command, string[] args)
        {
            if (player == null)
            {
                return;
            }
            var item = player.GetActiveItem();
            if (item == null)
            {
                return;
            }
            if (item.skin == 0)
            {
                return;
            }
            ItemInfo itemInfo = config.ItemInfoList.Find(x => x.SkinID == item.skin);
            if (itemInfo == null)
            {
                if (config.DiscordWebhook == null)
                {
                    SendReply(player, "No Description Found, Tell an Admin to add one.");
                    return;
                }
                SendReply(player, "No Description Found, Tell an Admin to add one or use /rwhat to request one.");
                return;

            }
            SendReply(player, itemInfo.Description);
            EffectNetwork.Send(new Effect(SelectEffect, player.transform.position, player.transform.position), player.net.connection);
        }

        [ChatCommand("rwhat")]
        void cmdRWhat(BasePlayer player, string command, string[] args)
        {
            if (player == null)
            {
                return;
            }
            var item = player.GetActiveItem();
            if (item == null)
            {
                return;
            }
            if (item.skin == 0)
            {
                return;
            }
            ItemInfo itemInfo = config.ItemInfoList.Find(x => x.SkinID == item.skin);
            if (itemInfo == null)
            {
                SendReply(player, "Sent a Request for a Description.");
                return;
            }
            var playerName = player.displayName;
            var skin = item.skin;
            var shortname = item.info.shortname;
            var customname = item.name ?? "empty";
            var message = $"{playerName} is requesting a description for item with Custom Name: {customname}, SkinID: {skin} and Shortname: {shortname}.";
            SendDiscordRequest(message);
            EffectNetwork.Send(new Effect(SelectEffect, player.transform.position, player.transform.position), player.net.connection);
        }

        #endregion

        #region Discord

        private void SendDiscordRequest(string message)
        {
            var url = config.DiscordWebhook;
            if (string.IsNullOrEmpty(url))
            {
                return;
            }
            var embed = new {
                type = "rich",
                title = "Item Description Request",
                description = message,
                color = 1730982,
            };
            var payload = new {
                embeds = new[] { embed }
            };
            var serializedPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            if (string.IsNullOrEmpty(url) || !url.Contains("/api/webhooks"))
                return;
            webrequest.Enqueue(url, serializedPayload, (code, response) =>
            {
                if (code != 204) Puts($"Discord responded with code {code}. Response: {response}");
            }, this, RequestMethod.POST, new Dictionary<string, string> {
                ["Content-Type"] = "application/json"
            });
        }

        #endregion
    }
}
