// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6

using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Timed Permissions In Statusbar", "shoprust.ru", "1.3.0")]
    public class TimedPermissionsInStatusbar : RustPlugin
    {
        #region Plugin References
        [PluginReference] private Plugin ImageLibrary, CustomStatusFramework, TimedPermissions, IQPermissions;
        #endregion

        #region Permissions
        const string hidePerm = "timedpermissionsinstatusbar.hide";
        #endregion

        #region Default Icon
        const string DefaultIconId = "TimedPermissionsInStatusbar_DefaultIcon";
        const string DefaultIconUrl = "https://i.imgur.com/nMeXKPp.png";
        #endregion

        #region Magic
        private abstract class StatusDependencies
        {
            public abstract bool Condition(BasePlayer player, string key, bool isGroup);
            public abstract string Value(BasePlayer player, string key, bool isGroup, string format = @"d\d\ hh\h\ mm\m");
        }

        private class __TimedPermissions : StatusDependencies
        {
            public __TimedPermissions(Plugin plugin)
            {
                References.GetReferences(plugin);
            }


            public static class References
            {
                public static Plugin Plugin { get; set; }
                public static Type TimedPermissionsType { get; set; }
                public static Type[] NestedTypes_TimedPermissions { get; set; }
                public static Type PlayerInformationType { get; set; }
                public static MethodInfo PlayerInformation_Get { get; set; }
                public static PropertyInfo PlayerInformation_Groups { get; set; }
                public static PropertyInfo PlayerInformation_Permissions { get; set; }
                public static Type ExpiringAccessValueType { get; set; }
                public static PropertyInfo ExpiringAccessValue_Value { get; set; }
                public static PropertyInfo ExpiringAccessValue_ExpireDate { get; set; }
                public static PropertyInfo ReadOnlyCollection_Count { get; set; }
                public static MethodInfo ElementAt { get; set; }
                public static bool GetReferences(Plugin plugin)
                {
                    Plugin = plugin;
                    if (Plugin == null)
                        return false;
                    TimedPermissionsType = Plugin?.GetType();
                    NestedTypes_TimedPermissions = TimedPermissionsType?.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance);
                    PlayerInformationType = NestedTypes_TimedPermissions?.First(t => t.Name == "PlayerInformation");
                    PlayerInformation_Get = PlayerInformationType?.GetMethod("Get");
                    PlayerInformation_Groups = PlayerInformationType?.GetProperty("Groups");
                    PlayerInformation_Permissions = PlayerInformationType?.GetProperty("Permissions");
                    ExpiringAccessValueType = NestedTypes_TimedPermissions?.First(t => t.Name == "ExpiringAccessValue");
                    ExpiringAccessValue_Value = ExpiringAccessValueType?.GetProperty("Value");
                    ExpiringAccessValue_ExpireDate = ExpiringAccessValueType?.GetProperty("ExpireDate");
                    ReadOnlyCollection_Count = typeof(System.Collections.ObjectModel.ReadOnlyCollection<>)
                                  .MakeGenericType(ExpiringAccessValueType)
                                  .GetProperty("Count");
                    ElementAt = typeof(Enumerable).GetMethod("ElementAt").MakeGenericMethod(ExpiringAccessValueType);
                    return true;
                }
            }

            public override bool Condition(BasePlayer player, string key, bool isGroup)
            {
                var pinfo = References.PlayerInformation_Get.Invoke(null, new object[] { player.UserIDString });
                if (pinfo == null) return false;
                var collection = (isGroup ? References.PlayerInformation_Groups.GetValue(pinfo) : References.PlayerInformation_Permissions.GetValue(pinfo));
                if (collection == null) return false;
                int countGroups = (int)References.ReadOnlyCollection_Count.GetValue(collection);
                object ExpiringAccessValue = null;
                for (int i = 0; i < countGroups; i++)
                {
                    var eav = References.ElementAt.Invoke(null, new object[] { collection, i });
                    var namePerm = (string)References.ExpiringAccessValue_Value.GetValue(eav);
                    if (namePerm.Equals(key))
                        ExpiringAccessValue = eav;
                }
                return ExpiringAccessValue != null;
            }
            public override string Value(BasePlayer player, string key, bool isGroup, string format = @"d\d\ hh\h\ mm\m")
            {
                var pinfo = __TimedPermissions.References.PlayerInformation_Get.Invoke(null, new object[] { player.UserIDString });
                if (pinfo == null) return string.Empty;
                var collection = (isGroup ? __TimedPermissions.References.PlayerInformation_Groups.GetValue(pinfo) : __TimedPermissions.References.PlayerInformation_Permissions.GetValue(pinfo));
                if (collection == null) return string.Empty;
                int countGroups = (int)__TimedPermissions.References.ReadOnlyCollection_Count.GetValue(collection);
                object ExpiringAccessValue = null;
                for (int i = 0; i < countGroups; i++)
                {
                    var eav = __TimedPermissions.References.ElementAt.Invoke(null, new object[] { collection, i });
                    var namePerm = (string)__TimedPermissions.References.ExpiringAccessValue_Value.GetValue(eav);
                    if (namePerm.Equals(key))
                        ExpiringAccessValue = eav;
                }
                if (ExpiringAccessValue == null) return string.Empty;
                var ExpireDate = (DateTime)__TimedPermissions.References.ExpiringAccessValue_ExpireDate.GetValue(ExpiringAccessValue);
                return (ExpireDate - DateTime.UtcNow).ToString(format);
            }
        }
        private class __IQPermissions : StatusDependencies
        {
            private Plugin Plugin { get; set; }

            public __IQPermissions(Plugin plugin)
            {
                Plugin = plugin;
                References.GetReferences(plugin);
            }

            public static class References
            {
                public static Plugin Plugin { get; set; }
                public static Type IQPermissionsType { get; set; }
                public static MethodInfo GetGroups { get; set; }
                public static MethodInfo GetPermissions { get; set; }

                public static bool GetReferences(Plugin plugin)
                {
                    Plugin = plugin;
                    if (Plugin == null)
                        return false;
                    IQPermissionsType = Plugin?.GetType();
                    GetGroups = IQPermissionsType.GetMethod("GetGroups", BindingFlags.Public | BindingFlags.Instance);
                    GetPermissions = IQPermissionsType.GetMethod("GetPermissions", BindingFlags.Public | BindingFlags.Instance);
                    return true;
                }
            }

            public override bool Condition(BasePlayer player, string key, bool isGroup)
            {
                Dictionary<String, DateTime> dictionary = (Dictionary<String, DateTime>)(isGroup ? References.GetGroups : References.GetPermissions).Invoke(Plugin, new object[] { player.userID });
                return dictionary.ContainsKey(key);
            }
            public override string Value(BasePlayer player, string key, bool isGroup, string format = @"d\d\ hh\h\ mm\m")
            {
                Dictionary<String, DateTime> dictionary = (Dictionary<String, DateTime>)(isGroup ? References.GetGroups : References.GetPermissions).Invoke(Plugin, new object[] { player.userID }); ;
                DateTime dateTime;
                if (!dictionary.TryGetValue(key, out dateTime)) return string.Empty;
                return (DateTime.Now - dateTime).ToString(format);
            }
        }

        #endregion

        #region Fields
        static StatusDependencies Dependencies;
        #endregion

        #region Methods
        private bool BasicConditionForPlayer(BasePlayer player, string key)
        {
            if (permission.UserHasPermission(player.UserIDString, hidePerm))
                return false;

            if (config[key].InCupboardArea && !player.IsBuildingAuthed())
                return false;

            return true;
        }

        private bool HasStatus(BasePlayer player, string id)
        {
            return CustomStatusFramework?.Call<bool>("HasStatus", new object[] {
                player, id
            }) == true;
        }

        private void SetStaticStatus(BasePlayer player, string key, ConfigData.StatusSettings settings)
        {
            if (!BasicConditionForPlayer(player, key)) return;

            bool hasTimedPerm = Dependencies.Condition(player, key, settings.IsGroup);
            bool hasUsualPerm = settings.IsGroup ? permission.UserHasGroup(player.UserIDString, key) : permission.UserHasPermission(player.UserIDString, key);

            if (!hasTimedPerm && !hasUsualPerm) return;
            if (hasTimedPerm && settings.SelectedMode != ConfigData.StatusSettings.Mode.Static) return;

            CustomStatusFramework?.Call("SetStatus", new object[] {
                player,
                GetStatusId(lang.GetLanguage(player.UserIDString), settings.IsGroup, key),
                settings.Color,
                GetMessage(key, player.userID),
                settings.TextColor,
                hasUsualPerm && !hasTimedPerm ? GetMessage(LangKeys.Unlimited, player.userID) : GetMessage($"{key}:Subtext", player.userID),
                settings.SubTextColor,
                settings.IconUrl == "default" ? DefaultIconId : settings.IconUrl,
                settings.IconColor
            });
        }

        private void DeleteStatus(string id)
        {
            CustomStatusFramework?.Call("DeleteStatus", new object[] { id });
        }
        private void ClearStaticStatus(BasePlayer player, string id)
        {
            CustomStatusFramework?.Call("ClearStatus", new object[] {
                player, id
            });
        }

        private void UpdateStaticStatus(BasePlayer player, string key, ConfigData.StatusSettings settings)
        {
            string id = GetStatusId(lang.GetLanguage(player.UserIDString), settings.IsGroup, key);
            if (HasStatus(player, id))
                ClearStaticStatus(player, id);
            SetStaticStatus(player, key, settings);
        }

        private string GetStatusId(string lang, bool isGroup, string key)
        {
            return $"[{lang}] {(isGroup ? "Group" : "Permission")}:{key}";
        }

        private void UpdateStaticStatusesForPlayer(BasePlayer player)
        {
            foreach (var pair in config)
            {
                foreach (var l in cachedLanguages)
                {
                    string id = GetStatusId(l, pair.Value.IsGroup, pair.Key);
                    if (HasStatus(player, id))
                        ClearStaticStatus(player, id);
                }
                SetStaticStatus(player, pair.Key, pair.Value);
            }
        }

        private void SendMessage(BasePlayer player, string format, params object[] args) => SendReply(player, $"<color=#6fcbd9>[{this.Title}]</color>\n> " + format, args);

        #endregion

        #region Commands
        [ChatCommand("tps.toggle")]
        private void tpstoggleCmd(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, hidePerm))
            {
                permission.RevokeUserPermission(player.UserIDString, hidePerm);
                UpdateStaticStatusesForPlayer(player);
                SendMessage(player, GetMessage(LangKeys.ShowMessage, player.userID));
            }
            else
            {
                permission.GrantUserPermission(player.UserIDString, hidePerm, this);
                UpdateStaticStatusesForPlayer(player);
                SendMessage(player, GetMessage(LangKeys.HideMessage, player.userID));
            }
        }
        #endregion

        #region Hooks

        void Init()
        {
            permission.RegisterPermission(hidePerm, this);
        }

        void OnServerInitialized()
        {
            if (ImageLibrary == null || CustomStatusFramework == null)
            {
                PrintError("\nYou do not have one or more required plugins installed!\n" +
                    "ImageLibrary: https://umod.org/plugins/image-library \n" +
                    "CustomStatusFramework: https://codefling.com/plugins/custom-status-framework");
                NextTick(() => Interface.Oxide.UnloadPlugin(this.Name));
                return;
            }

            LoadConfig();
            SaveConfig();


            bool timedpermLoaded = CSharpPluginLoader.Instance.LoadedPlugins.ContainsKey(nameof(TimedPermissions));
            bool iqpermLoaded = CSharpPluginLoader.Instance.LoadedPlugins.ContainsKey(nameof(IQPermissions));


            if (!timedpermLoaded && !iqpermLoaded)
            {
                PrintError("\nAt least one of the plugins for temporary permissions is needed!\n" +
                    "Timed Permissions: https://umod.org/plugins/timed-permissions \n" +
                    "IQPermissions");
                NextTick(() => Interface.Oxide.UnloadPlugin(this.Name));
                return;
            }
            else if (timedpermLoaded && iqpermLoaded)
            {
                PrintError("Leave only one plugin for temporary permissions!");
                return;
            }
            if (timedpermLoaded)
                Dependencies = new __TimedPermissions(TimedPermissions);
            else
                Dependencies = new __IQPermissions(IQPermissions);

            if (TimedPermissions != null)
            {
                if (!__TimedPermissions.References.GetReferences(TimedPermissions))
                {
                    PrintError("References.GetReferences call failed!");
                    return;
                }
            }


            if (!ImageLibrary.Call<bool>("HasImage", new object[] { DefaultIconId, (ulong)0 }))
                ImageLibrary.Call<bool>("AddImage", new object[] { DefaultIconUrl, DefaultIconId, (ulong)0 });

            foreach (var pair in config)
            {
                if (pair.Value.IconUrl != "default")
                    if (!ImageLibrary.Call<bool>("HasImage", new object[] { pair.Value.IconUrl, (ulong)0 }))
                        ImageLibrary.Call<bool>("AddImage", new object[] { pair.Value.IconUrl, pair.Value.IconUrl, (ulong)0 });
            }

            if (cachedLanguages == null)
                cachedLanguages = lang.GetLanguages();

            timer.Once(1, () =>
            {
                var languages = cachedLanguages;
                foreach (var pair in config)
                {
                    switch (pair.Value.SelectedMode)
                    {
                        case ConfigData.StatusSettings.Mode.Time:
                            foreach (var l in languages)
                            {
                                var text = GetMessage(pair.Key, l);
                                CustomStatusFramework?.Call("CreateDynamicStatus", new object[] { GetStatusId(l, pair.Value.IsGroup, pair.Key), pair.Value.Color, text, pair.Value.TextColor, pair.Value.SubTextColor, pair.Value.IconUrl == "default" ? DefaultIconId : pair.Value.IconUrl, pair.Value.IconColor,
                                    (Func<BasePlayer, bool>) ((BasePlayer player) =>
                                    {
                                        if (GetNearestSupportedLanguage(player) != l) return false;
                                        if (!BasicConditionForPlayer(player, pair.Key)) return false;
                                        return Dependencies.Condition(player, pair.Key, pair.Value.IsGroup);
                                    }),
                                    (Func<BasePlayer, string>) ((BasePlayer player) => Dependencies.Value(player, pair.Key, pair.Value.IsGroup, pair.Value.TimeFormat))
                                });
                            }
                            break;
                    }
                    foreach (var player in BasePlayer.activePlayerList)
                        UpdateStaticStatus(player, pair.Key, pair.Value);
                }

            });
        }


        void OnPlayerConnected(BasePlayer player)
        {
            foreach (var pair in config.Where(p => p.Value.SelectedMode == ConfigData.StatusSettings.Mode.Static))
                UpdateStaticStatus(player, pair.Key, pair.Value);
        }

        void OnUserPermissionGranted(string id, string permName)
        {
            BasePlayer player = BasePlayer.Find(id);
            if (player == null) return;

            if (!config.ContainsKey(permName)) return;

            ConfigData.StatusSettings settings = null;
            if (!config.TryGetValue(permName, out settings) || settings == null) return;

            if (settings.IsGroup) return;

            UpdateStaticStatus(player, permName, settings);
        }

        void OnUserPermissionRevoked(string id, string permName)
        {
            BasePlayer player = BasePlayer.Find(id);
            if (player == null) return;

            if (!config.ContainsKey(permName)) return;

            ConfigData.StatusSettings settings = null;
            if (!config.TryGetValue(permName, out settings) || settings == null) return;

            if (settings.IsGroup) return;

            UpdateStaticStatus(player, permName, settings);
        }

        void OnUserGroupAdded(string id, string groupName)
        {
            BasePlayer player = BasePlayer.Find(id);
            if (player == null) return;

            if (!config.ContainsKey(groupName)) return;

            ConfigData.StatusSettings settings = null;
            if (!config.TryGetValue(groupName, out settings) || settings == null) return;

            if (!settings.IsGroup) return;

            UpdateStaticStatus(player, groupName, settings);
        }

        void OnUserGroupRemoved(string id, string groupName)
        {
            BasePlayer player = BasePlayer.Find(id);
            if (player == null) return;

            if (!config.ContainsKey(groupName)) return;

            ConfigData.StatusSettings settings = null;
            if (!config.TryGetValue(groupName, out settings) || settings == null) return;

            if (!settings.IsGroup) return;

            UpdateStaticStatus(player, groupName, settings);
        }

        private void Unload()
        {
            foreach (var l in cachedLanguages)
                foreach (var pair in config)
                {
                    foreach (var player in BasePlayer.activePlayerList)
                    {
                        string id = GetStatusId(l, pair.Value.IsGroup, pair.Key);
                        if (HasStatus(player, id))
                            ClearStaticStatus(player, id);
                    }
                    DeleteStatus(GetStatusId(l, pair.Value.IsGroup, pair.Key));
                }

        }
        #endregion

        #region Config

        private static Dictionary<string, ConfigData.StatusSettings> config;

        private class ConfigData
        {
            public class StatusSettings
            {
                public enum Mode
                {
                    Time,
                    Static
                }


                [JsonProperty(PropertyName = "true - Group | false - Permission")]
                public bool IsGroup { get; set; } = true;
                [JsonProperty(PropertyName = "Display Mode (0 - Time, 1 - Static)")]
                public Mode SelectedMode { get; set; } = 0;
                [JsonProperty(PropertyName = "Only show in authorized cupboard area")]
                public bool InCupboardArea { get; set; } = false;
                [JsonProperty(PropertyName = "Icon Url")]
                public string IconUrl { get; set; } = "default";
                [JsonProperty(PropertyName = "Time Format")]
                public string TimeFormat { get; set; } = @"d\d\ hh\h\ mm\m";
                [JsonProperty(PropertyName = "Color")]
                public string Color { get; set; } = "0.16 0.44 0.63 1";
                [JsonProperty(PropertyName = "Icon Color")]
                public string IconColor { get; set; } = "0.22 0.63 0.90 1";
                [JsonProperty(PropertyName = "Text Color")]
                public string TextColor { get; set; } = "1 1 1 1";
                [JsonProperty(PropertyName = "Subtext Color")]
                public string SubTextColor { get; set; } = "1 1 1 1";
            }
        }

        private Dictionary<string, ConfigData.StatusSettings> GetDefaultConfig()
        {
            return new Dictionary<string, ConfigData.StatusSettings>()
            {
                ["ExampleGroupOrPerm"] = new ConfigData.StatusSettings(),
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Dictionary<string, ConfigData.StatusSettings>>();
                if (config == null)
                    LoadDefaultConfig();
            }
            catch (Exception ex)
            {
                if (ex is JsonSerializationException || ex is NullReferenceException || ex is JsonReaderException)
                {
                    Puts($"Exception: {ex}");
                    LoadDefaultConfig();
                    return;
                }
                throw;
            }
        }
        protected override void LoadDefaultConfig()
        {
            Puts("Configuration file missing or corrupt, creating default config file.");
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
        #endregion

        #region Lang

        public class LangKeys
        {
            public const string HideMessage = nameof(HideMessage);
            public const string ShowMessage = nameof(ShowMessage);
            public const string Unlimited = nameof(Unlimited);
        }

        protected override void LoadDefaultMessages()
        {
            cachedLanguages = lang.GetLanguages();

            var basic = new Dictionary<string, string>()
            {
                [LangKeys.HideMessage] = "You have hidden the statuses of timed permissions.",
                [LangKeys.ShowMessage] = "You have enabled the display of timed permissions statuses.",
                [LangKeys.Unlimited] = "Unlimited",
            };
            var keys = config.Keys.ToDictionary(x => x, x => x);
            var subtexts = config.Where(p => p.Value.SelectedMode == ConfigData.StatusSettings.Mode.Static).Select(p => p.Key).ToDictionary(x => $"{x}:Subtext", x => "");
            foreach (var l in cachedLanguages)
                lang.RegisterMessages(basic.Concat(keys).Concat(subtexts).ToDictionary(x => x.Key, x => x.Value), this, l);
        }

        public string[] cachedLanguages;

        public string GetNearestSupportedLanguage(BasePlayer player)
        {
            return GetNearestSupportedLanguage(player.UserIDString);
        }
        public string GetNearestSupportedLanguage(string userID)
        {
            string playerLang = lang.GetLanguage(userID);
            if (cachedLanguages.Contains(playerLang))
                return playerLang;
            return "en";
        }

        private MethodInfo GetMessageKeyMethod = typeof(Core.Libraries.Lang).GetMethod("GetMessageKey", (BindingFlags.NonPublic | BindingFlags.Instance));

        private string GetMessage(string langKey, ulong userID)
        {
            var _lang = GetNearestSupportedLanguage(userID.ToString());
            return GetMessage(langKey, _lang);
        }
        private string GetMessage(string langKey, string _lang)
        {
            string @return = (string)GetMessageKeyMethod.Invoke(lang, new object[] { langKey, this, _lang });
            if (_lang != "en" && (@return == langKey || @return == string.Empty))
                @return = (string)GetMessageKeyMethod.Invoke(lang, new object[] { langKey, this, "en" });
            return @return;
        }
        private string GetMessage(string langKey, ulong userID, params object[] args) => (args.Length == 0) ? GetMessage(langKey, userID) : string.Format(GetMessage(langKey, userID), args);
        #endregion Lang
    }
}