

using System;
using ConVar;
using System.Collections.Generic;
using Network;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using System.IO;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("AutoEmojis", "YaMang -w-", "1.0.3")]
    [Description("Automatically replaces configurable words with Emojis")]
    class AutoEmojis : RustPlugin
    {
        [PluginReference]
        Plugin BetterChat, IQChat;

        #region Hook

        void OnServerInitialized(bool initial)
        {
            if (BetterChat != null || IQChat != null)
            {
                Unsubscribe(nameof(OnPlayerChat));
            }

            if (_config.generalSettings.autoreg)
            {
                string[] files = Directory.GetFiles(ConVar.Server.GetServerFolder("serveremoji"));
                foreach (string filePath in files)
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);


                    if (!_config.generalSettings.customEmoji.Contains(fileNameWithoutExtension))
                    {
                        if (!_config.generalSettings.withoutEmoji.Contains(fileNameWithoutExtension))
                        {
                            _config.generalSettings.customEmoji.Add(fileNameWithoutExtension);
                            
                        }
                    }
                }
                SaveConfig();
            }
            

        }

        [HookMethod("OnBetterChat")]
        private object OnBetterChat(Dictionary<string, object> data)
        {
            IPlayer player = (IPlayer)data["Player"];

            BasePlayer basePlayer = BasePlayer.FindByID(ulong.Parse(player.Id));
            string message = data["Message"] as string;

            string msg = HandleMessage(message);
            bool find = false;
            if (string.IsNullOrEmpty(msg)) find = false;
            else find = true;

            if (find)
            {
                data["Message"] = msg;
            }

            return data;
        }
        private string HandleMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return null;

            Dictionary<string, string> containMsg = new Dictionary<string, string>();
            
            void ProcessItems(Dictionary<string, string> items, bool custom = false)
            {
                foreach (var item in items)
                {
                    if (message.Contains(item.Key))
                    {
                        if (message.Contains(":")) continue;
                        if (!containMsg.ContainsKey(item.Key))
                            containMsg.Add(item.Key, custom ? item.Key : item.Value);
                    }
                }
            }

            ProcessItems(_config.generalSettings.itemsEmoji);

            var customEmojiDict = _config.generalSettings.customEmoji.ToDictionary(emoji => emoji, emoji => emoji);
            ProcessItems(customEmojiDict, true);

            foreach (var item in containMsg)
            {
                message = message.Replace(item.Key, $":{item.Value}:");
            }

            return containMsg.Count > 0 ? message : null;
        }

        Object OnMessageIQChat(String message)
        {
            String msg = HandleMessage(message);
            return string.IsNullOrEmpty(msg) ? message : msg;
        }
        
        private object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            if (BetterChat == null)
            {
                string msg = HandleMessage(message);

                bool find = false;

                if (string.IsNullOrEmpty(msg)) find = false;
                else find = true;

                if (find)
                {
                    if (channel == Chat.ChatChannel.Team)
                    {
                        List<Connection> sendUserList = new List<Connection>();
                        RelationshipManager.PlayerTeam team = player.Team;
                        if (null == team || team.members.Count < 1) return true;
                        foreach (ulong teamUserId in team.members)
                        {
                            Connection inUser = BasePlayer.FindByID(teamUserId).Connection;
                            if (null != inUser) sendUserList.Add(inUser);
                        }

                        if (sendUserList.Count > 0) 
                            player.SendConsoleCommand("chat.add2", new object[] { channel, player.UserIDString, msg, "[TEAM] " + player.displayName, "#5af" });
                    }
                    else
                    {
                        List<Connection> sendUserList = new List<Connection>();
                        foreach (BasePlayer basePlayer in BasePlayer.activePlayerList)
                        {
                            sendUserList.Add(basePlayer.Connection);
                        }

                        if (sendUserList.Count > 0) player.SendConsoleCommand("chat.add2", new object[] { channel, player.UserIDString, msg, player.displayName, "#5af" });
                    }

                    return false;
                }
            }

            return null;
        }
        #endregion


        #region Config        
        private ConfigData _config;
        private class ConfigData
        {
            [JsonProperty(PropertyName = "General Settings")] public GeneralSettings generalSettings { get; set; }
            public Oxide.Core.VersionNumber Version { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<ConfigData>();

            if (_config.Version < Version)
                UpdateConfigValues();

            Config.WriteObject(_config, true);
        }

        protected override void LoadDefaultConfig() => _config = GetBaseConfig();

        private ConfigData GetBaseConfig()
        {
            return new ConfigData
            {
                generalSettings = new GeneralSettings
                {
                    autoreg = true,
                    customEmoji = new List<string> { },
                    withoutEmoji = new List<string> { },
                    itemsEmoji = new Dictionary<string, string>
                    {
                        {
                            "wood", "wood"
                        }
                    }
                },
                Version = Version
            };
        }

        public class GeneralSettings
        {
            [JsonProperty(PropertyName = "Auto Emojis registration", Order = 1)] public bool autoreg { get; set; }
            [JsonProperty(PropertyName = "Auto Emojis [You must have a custom emoji]", Order = 2)] public List<string> customEmoji { get; set; }
            [JsonProperty(PropertyName = "Without Emojis [You must have a custom emoji]", Order = 3)] public List<string> withoutEmoji { get; set; }
            [JsonProperty(PropertyName = "Item Emojis (chat | shortname)", Order = 4)] public Dictionary<string, string> itemsEmoji { get; set; }
        }

        protected override void SaveConfig() => Config.WriteObject(_config, true);

        private void UpdateConfigValues()
        {
            PrintWarning("Config update detected! Updating config values...");
            _config.Version = Version;
            PrintWarning("Config update completed!");
        }

        #endregion
    }
}
