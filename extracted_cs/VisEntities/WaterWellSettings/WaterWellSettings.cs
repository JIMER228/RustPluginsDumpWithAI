/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Water Well Settings", "VisEntities", "1.0.0")]
    [Description("Allows customization of water wells.")]
    public class WaterWellSettings : RustPlugin
    {
        #region Fields

        private static WaterWellSettings _plugin;
        private static Configuration _config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Water Produced Per Pump")]
            public int WaterProducedPerPump { get; set; }

            [JsonProperty("Calories Consumed Per Pump")]
            public float CaloriesConsumedPerPump { get; set; }
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
                WaterProducedPerPump = 50,
                CaloriesConsumedPerPump = 5f
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
            UpdateWaterWellProperties();
        }

        #endregion Oxide Hooks

        #region Water Well Setup

        private void UpdateWaterWellProperties()
        {
            foreach (WaterWell waterWell in BaseNetworkable.serverEntities.OfType<WaterWell>())
            {
                if (waterWell != null)
                {
                    waterWell.waterPerPump = _config.WaterProducedPerPump;
                    waterWell.caloriesPerPump = _config.CaloriesConsumedPerPump;
                }
            }
        }

        #endregion Water Well Setup
    }
}