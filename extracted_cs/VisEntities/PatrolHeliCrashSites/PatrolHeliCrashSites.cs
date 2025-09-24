// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Patrol Heli Crash Sites", "VisEntities", "1.0.0")]
    [Description("Prevents patrol helicopters from crashing at certain monuments.")]
    public class PatrolHeliCrashSites : RustPlugin
    {
        #region Fields

        private static PatrolHeliCrashSites _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Disallowed Monuments")]
            public List<string> DisallowedMonuments { get; set; }
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
                DisallowedMonuments = new List<string>
                {
                    "airfield_1",
                    "satellite_dish",
                    "sphere_tank"
                }
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

        private void OnServerInitialized(bool isStartup)
        {
            foreach (var monumentInfo in TerrainMeta.Path.Monuments)
            {
                foreach (string disallowedMonument in _config.DisallowedMonuments)
                {
                    if (monumentInfo.name.Contains(disallowedMonument))
                    {
                        monumentInfo.AllowPatrolHeliCrash = false;
                        break;
                    }
                }
            }
        }

        #endregion Oxide Hooks
    }
}