using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Kit Groups", "WhiteThunder", "2.1.0")]
    [Description("Adds players to Oxide groups when they redeem Kits.")]
    internal class KitGroups : CovalencePlugin
    {
        #region Fields

        [PluginReference]
        private readonly Plugin Kits, TimedPermissions;

        private Configuration _config;

        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
            _config.OnServerInitialized(this, TimedPermissions);
        }

        private void OnNewSave()
        {
            // Reset group membership
            foreach (var kitConfig in _config.KitConfigs.Values)
            {
                var userIdList = permission.GetUsersInGroup(kitConfig.Group);
                foreach (var userId in userIdList)
                {
                    permission.RemoveUserGroup(userId, kitConfig.Group);
                }
            }
        }

        // This hook is exposed by plugin: Kits.
        private void OnKitRedeemed(BasePlayer player, string kitName)
        {
            if (player.IsNpc || !player.userID.IsSteamId())
            {
                return;
            }

            if (_config.DebugLevel >= 2)
            {
                LogWarning($"Player {player.UserIDString} redeemed kit {kitName}");
            }

            var kitConfig = _config.GetKitConfig(kitName);
            if (kitConfig == null)
            {
                if (_config.DebugLevel >= 2)
                {
                    LogWarning($"Kit {kitName} has no KitGroups configuration.");
                }

                return;
            }

            if (kitConfig.Exclusive)
            {
                RevokeAllPermissions(player, kitConfig.Group);
            }

            GrantPermission(player, kitConfig);
        }

        #endregion

        #region Dependencies

        private bool KitExists(string kitName)
        {
            return Kits?.Call("IsKit", kitName) is true;
        }

        private void AddToGroupTimed(string userId, string groupName, string duration)
        {
            if (TimedPermissions == null)
            {
                LogError($"Unable to add user {userId} to group {groupName} because TimedPermissions is not loaded.");
                return;
            }

            server.Command($"addgroup {userId} {groupName} {duration}");
        }

        private void RemoveFromGroupTimed(string userId, string groupName)
        {
            if (TimedPermissions == null)
            {
                LogError($"Unable to add user {userId} to group {groupName} because TimedPermissions is not loaded.");
                return;
            }

            server.Command($"removegroup {userId} {groupName}");
        }

        #endregion

        #region Helpers

        private void GrantPermission(BasePlayer player, KitConfig kitConfig)
        {
            if (string.IsNullOrWhiteSpace(kitConfig.Duration))
            {
                if (_config.DebugLevel >= 1)
                {
                    LogWarning($"Adding user {player.UserIDString} to group {kitConfig.Group} until next wipe.");
                }

                permission.AddUserGroup(player.UserIDString, kitConfig.Group);
            }
            else
            {
                if (_config.DebugLevel >= 1)
                {
                    LogWarning($"Adding user {player.UserIDString} to group {kitConfig.Group} for {kitConfig.Duration}.");
                }

                AddToGroupTimed(player.UserIDString, kitConfig.Group, kitConfig.Duration);
            }
        }

        private void RevokeAllPermissions(BasePlayer player, string forGroup)
        {
            foreach (var kitConfig in _config.KitConfigs.Values)
            {
                if (!permission.UserHasGroup(player.UserIDString, kitConfig.Group))
                    continue;

                if (string.IsNullOrWhiteSpace(kitConfig.Duration))
                {
                    if (_config.DebugLevel >= 1)
                    {
                        LogWarning($"Removing user {player.UserIDString} from group {kitConfig.Group} due to exclusive group {forGroup}");
                    }

                    permission.RemoveUserGroup(player.UserIDString, kitConfig.Group);
                }
                else
                {
                    if (_config.DebugLevel >= 1)
                    {
                        LogWarning($"Removing user {player.UserIDString} from timed group {kitConfig.Group} due to exclusive group {forGroup}");
                    }

                    RemoveFromGroupTimed(player.UserIDString, kitConfig.Group);
                }
            }
        }

        #endregion

        #region Configuration

        private class KitConfig
        {
            [JsonProperty("Group")]
            public string Group;

            [JsonProperty("Duration")]
            public string Duration;

            [JsonProperty("Exclusive")]
            public bool Exclusive;
        }

        private class Configuration : BaseConfiguration
        {
            [JsonProperty("DebugLevel")]
            public int DebugLevel;

            [JsonProperty("Kits")]
            public Dictionary<string, KitConfig> KitConfigs = new Dictionary<string, KitConfig>();

            public void OnServerInitialized(KitGroups pluginInstance, Plugin timedPermissions)
            {
                foreach (var entry in KitConfigs)
                {
                    var kitName = entry.Key;

                    if (!pluginInstance.KitExists(entry.Key))
                    {
                        pluginInstance.LogError($"Kit '{kitName}' does not exist.");
                        continue;
                    }

                    var kitConfig = entry.Value;
                    if (!pluginInstance.permission.GroupExists(kitConfig.Group))
                    {
                        pluginInstance.LogError($"Kit '{kitName}' specifies group '{kitConfig.Group}' which does not exist.");
                        continue;
                    }

                    if (kitConfig.Duration != null && timedPermissions == null)
                    {
                        pluginInstance.LogError($"Kit '{kitName}' has duration enabled, but TimedPermissions is not loaded.");
                        continue;
                    }
                }
            }

            public KitConfig GetKitConfig(string kitName)
            {
                KitConfig kitConfig;
                return KitConfigs.TryGetValue(kitName, out kitConfig)
                    ? kitConfig
                    : null;
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
            var changed = false;

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

        protected override void LoadDefaultConfig() => _config = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(_config))
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
            Config.WriteObject(_config, true);
        }

        #endregion

        #endregion
    }
}
