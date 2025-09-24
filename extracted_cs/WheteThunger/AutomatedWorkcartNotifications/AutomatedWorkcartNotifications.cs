using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using Network;

namespace Oxide.Plugins
{
    [Info("Automated Workcart Notifications", "WhiteThunder", "0.2.1")]
    [Description("Notifies players via chat when Automated Workcarts stop nearby.")]
    internal class AutomatedWorkcartNotifications : CovalencePlugin
    {
        #region Fields

        [PluginReference]
        private readonly Plugin AutomatedWorkcarts;

        private Configuration _pluginConfig;
        private object[] _objectArrayForChatCommand;

        #endregion

        #region Hooks

        private void Init()
        {
            _objectArrayForChatCommand = new object[3]
            {
                2.ToString(),
                _pluginConfig.ChatSteamIdIcon.ToString(),
                null,
            };
        }

        private void OnServerInitialized()
        {
            if (AutomatedWorkcarts == null)
            {
                LogError("AutomatedWorkcarts is not loaded, get it at http://umod.org.");
            }
        }

        #endregion

        #region Commands

        [Command("workcart.notify")]
        private void CommandWorkcartNotify(IPlayer player, string cmd, string[] args)
        {
            if (!player.IsServer)
                return;

            ulong trainEngineId;
            if (args.Length < 3 || !ulong.TryParse(args[0], out trainEngineId))
            {
                LogError($"Invalid syntax. Expected: {cmd} <train_engine_id> <notification_name> <delay_seconds>");
                return;
            }

            var trainEngine = BaseNetworkable.serverEntities.Find(new NetworkableId(trainEngineId)) as TrainEngine;
            if (trainEngine == null || trainEngine.IsDestroyed)
            {
                LogError($"No train engine found with ID: {trainEngineId}. Make sure you are using '$id' parameter.");
                return;
            }

            var notificationConfigKey = args[1];

            NotificationConfig notificationConfig;
            if (!_pluginConfig.Notifications.TryGetValue(notificationConfigKey, out notificationConfig))
            {
                LogError($"Invalid notification config: '{notificationConfigKey}'");
                return;
            }

            float notificationDelay;
            if (!float.TryParse(args[2], out notificationDelay))
            {
                LogError($"Invalid delay: '{args[2]}'");
                return;
            }

            ScheduleNotification(args, trainEngine, notificationConfigKey, notificationDelay, notificationConfig);
        }

        #endregion

        #region Helper Methods

        private string[] SkipArgs(string[] args, int count)
        {
            var newArgs = new string[args.Length - count];
            for (var i = count; i < args.Length; i++)
            {
                newArgs[i - count] = args[i];
            }
            return newArgs;
        }

        private void ChatMessage(BasePlayer player, string message)
        {
            _objectArrayForChatCommand[2] = message;
            player.SendConsoleCommand("chat.add", _objectArrayForChatCommand);
        }

        private void ScheduleNotification(string[] args, TrainEngine trainEngine, string notificationConfigKey, float notificationDelay, NotificationConfig notificationConfig)
        {
            timer.Once(notificationDelay, () =>
            {
                if (trainEngine == null || trainEngine.IsDestroyed)
                    return;

                if (notificationConfig.MaxSpeed != 0 && trainEngine.GetTrackSpeed() > notificationConfig.MaxSpeed)
                    return;

                var trainEnginePosition = trainEngine.transform.position;

                var networkGroup = Net.sv.visibility.GetGroup(trainEnginePosition);
                if (networkGroup == null)
                    return;

                var maxDistanceSquared = Math.Pow(notificationConfig.MaxDistance, 2);

                foreach (var connection in networkGroup.subscribers)
                {
                    if (!connection.active)
                        continue;

                    var basePlayer = connection.player as BasePlayer;
                    if (basePlayer == null)
                        continue;

                    if (basePlayer.GetParentEntity() is TrainCar)
                        continue;

                    if (basePlayer.GetMounted() is TrainCar)
                        continue;

                    if (basePlayer.SqrDistance(trainEnginePosition) > maxDistanceSquared)
                        continue;

                    if (notificationConfig.EnableChatMessage)
                    {
                        if (args.Length > 3)
                        {
                            ChatMessage(basePlayer, string.Join(",", SkipArgs(args, 3)));
                        }

                        var message = args.Length > 3
                            ? GetMessage(basePlayer.UserIDString, notificationConfigKey, SkipArgs(args, 3))
                            : GetMessage(basePlayer.UserIDString, notificationConfigKey);

                        ChatMessage(basePlayer, message);
                    }

                    if (notificationConfig.HornDuration > 0 && !trainEngine.HasFlag(TrainEngine.Flag_Horn))
                    {
                        trainEngine.SetFlag(TrainEngine.Flag_Horn, true);
                        trainEngine.Invoke(() => trainEngine.SetFlag(TrainEngine.Flag_Horn, false), notificationConfig.HornDuration);
                    }
                }
            });
        }

        #endregion

        #region Configuration

        private class CaseInsensitiveDictionary<TValue> : Dictionary<string, TValue>
        {
            public CaseInsensitiveDictionary() : base(StringComparer.OrdinalIgnoreCase) {}
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class DeprecatedChatConfig
        {
            [JsonProperty("Max distance")]
            public float MaxDistance = 30;

            [JsonProperty("Max speed", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public float MaxSpeed;
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class NotificationConfig
        {
            [JsonProperty("Broadcast chat message")]
            public bool EnableChatMessage;

            [JsonProperty("Horn duration (seconds)")]
            public float HornDuration;

            [JsonProperty("Max distance")]
            public float MaxDistance = 30;

            [JsonProperty("Max speed", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public float MaxSpeed;
        }

        [JsonObject(MemberSerialization.OptIn)]
        private class Configuration : BaseConfiguration
        {
            [JsonProperty("Chat SteamID icon")]
            public ulong ChatSteamIdIcon;

            [JsonProperty("Notifications", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public CaseInsensitiveDictionary<NotificationConfig> Notifications = new CaseInsensitiveDictionary<NotificationConfig>
            {
                ["Arrived"] = new NotificationConfig
                {
                    EnableChatMessage = true,
                    HornDuration = 0,
                    MaxDistance = 40,
                },
                ["DepartingSoon"] = new NotificationConfig
                {
                    EnableChatMessage = true,
                    HornDuration = 0,
                    MaxDistance = 20,
                    MaxSpeed = 1,
                },
            };

            [JsonProperty("Chat notifications")]
            private Dictionary<string, DeprecatedChatConfig> DeprecatedChatNotifications
            {
                set
                {
                    foreach (var entry in value)
                    {
                        Notifications[entry.Key] = new NotificationConfig
                        {
                            EnableChatMessage = true,
                            MaxDistance = entry.Value.MaxDistance,
                            MaxSpeed = entry.Value.MaxSpeed,
                        };
                    }
                }
            }
        }

        private Configuration GetDefaultConfig() => new Configuration();

        #region Configuration Helpers

        private class BaseConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        private static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(BaseConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        protected override void LoadDefaultConfig() => _pluginConfig = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _pluginConfig = Config.ReadObject<Configuration>();
                if (_pluginConfig == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_pluginConfig))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch (Exception e)
            {
                LogError(e.Message);
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(_pluginConfig, true);
        }

        #endregion

        #endregion

        #region Localization

        private class LangEntry
        {
            public static List<LangEntry> AllLangEntries = new List<LangEntry>();

            public static readonly LangEntry Arrived = new LangEntry("Arrived", "All Aboard! A train has has arrived at the station.");
            public static readonly LangEntry DepartingSoon = new LangEntry("DepartingSoon", "All Aboard! The train will be departing soon.");

            public string Name;
            public string English;

            public LangEntry(string name, string english)
            {
                Name = name;
                English = english;

                AllLangEntries.Add(this);
            }
        }

        private string GetMessage(string playerId, string messageName) =>
            lang.GetMessage(messageName, this, playerId);

        private string GetMessage(string playerId, string messageName, params object[] args) =>
            string.Format(GetMessage(playerId, messageName), args);

        protected override void LoadDefaultMessages()
        {
            var englishLangKeys = new Dictionary<string, string>();

            foreach (var langEntry in LangEntry.AllLangEntries)
            {
                // Only add the default lang entries for valid config entries.
                if (_pluginConfig.Notifications.ContainsKey(langEntry.Name))
                {
                    englishLangKeys[langEntry.Name] = langEntry.English;
                }
            }

            foreach (var notificationEntry in _pluginConfig.Notifications)
            {
                if (notificationEntry.Value.EnableChatMessage)
                {
                    // Only add placeholder lang entries for config entries that have no plugin defaults.
                    englishLangKeys.TryAdd(notificationEntry.Key, notificationEntry.Key);
                }
            }

            lang.RegisterMessages(englishLangKeys, this, "en");
        }

        #endregion
    }
}
