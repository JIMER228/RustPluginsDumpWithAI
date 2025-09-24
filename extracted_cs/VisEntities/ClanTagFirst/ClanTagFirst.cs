/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Clan Tag First", "VisEntities", "1.0.0")]
    [Description("Forces clan tags to appear before all other Better Chat group titles.")]
    public class ClanTagFirst : RustPlugin
    {
        #region 3rd Party Dependencies

        [PluginReference]
        private readonly Plugin Clans;

        #endregion 3rd Party Dependencies

        #region Fields

        private static ClanTagFirst _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Clan Tag Format")]
            public string ClanTagFormat { get; set; }

            [JsonProperty("Clan Tag Color")]
            public string ClanTagColor { get; set; }

            [JsonProperty("Clan Tag Size")]
            public int ClanTagSize { get; set; }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                ClanTagFormat = "[{clanTag}]",
                ClanTagColor = "#aaff55",
                ClanTagSize = 15
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        // This hook is exposed by Better Chat plugin (https://umod.org/plugins/better-chat)
        private Dictionary<string, object> OnBetterChat(Dictionary<string, object> data)
        {
            IPlayer iPlayer = data["Player"] as IPlayer;
            if (iPlayer == null)
                return data;

            BasePlayer basePlayer = iPlayer.Object as BasePlayer;
            if (basePlayer == null)
                return data;

            string clanTag = ClansUtil.GetClanOf(basePlayer.userID);
            if (string.IsNullOrEmpty(clanTag))
                return data;

            if (!data.TryGetValue("Titles", out var titlesObj) || !(titlesObj is List<string> titles))
            {
                titles = new List<string>();
                data["Titles"] = titles;
            }

            for (int i = titles.Count - 1; i >= 0; i--)
            {
                if (ContainsClanTag(titles[i], clanTag))
                {
                    titles.RemoveAt(i);
                }
            }

            string bracketedClan = _config.ClanTagFormat.Replace("{clanTag}", clanTag);
            string finalClanString = bracketedClan;

            bool hasColor = !string.IsNullOrEmpty(_config.ClanTagColor);
            bool hasSize = _config.ClanTagSize > 0;

            if (hasColor || hasSize)
            {
                if (hasSize)
                    finalClanString = $"[+{_config.ClanTagSize}]{finalClanString}[/+]";

                if (hasColor)
                    finalClanString = $"[{_config.ClanTagColor}]{finalClanString}[/#]";
            }

            titles.Insert(0, finalClanString);
            return data;
        }

        #endregion Oxide Hooks

        #region 3rd Party Integration

        public static class ClansUtil
        {
            public static string GetClanOf(ulong playerId)
            {
                if (!PluginLoaded(_plugin.Clans))
                    return null;

                return _plugin.Clans.Call<string>("GetClanOf", playerId);
            }

            public static JObject GetClan(string tag)
            {
                if (!PluginLoaded(_plugin.Clans))
                    return null;

                return _plugin.Clans.Call<JObject>("GetClan", tag);
            }
        }

        #endregion 3rd Party Integration

        #region Helper Functions

        private bool ContainsClanTag(string title, string clanTag)
        {
            title = title.ToLowerInvariant();
            clanTag = clanTag.ToLowerInvariant();

            return title.Contains(clanTag);
        }

        public static bool PluginLoaded(Plugin plugin)
        {
            if (plugin != null && plugin.IsLoaded)
                return true;
            else
                return false;
        }

        #endregion Helper Functions
    }
}