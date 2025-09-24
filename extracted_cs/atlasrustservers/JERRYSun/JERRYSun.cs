using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("JERRYSun", "JERRY", "1.0.3")]
    [Description("Removes the harsh sun glare effect or the sun itself using server commands for weather. May be overriden by other weather plugins.")]
    public class JERRYSun : RustPlugin
    {
        private void Init()
        {
            Puts("JERRYSun Started!");
        }
        private void Unload()
        {
            ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.reset");
            Puts("JERRYSun Stopped! weather.reset command sent.");
        }

        private ConfigData configData = new ConfigData(); private class ConfigData
        {
            [JsonProperty(PropertyName = "Remove Sun")]
            public bool noSun = false;
            
            [JsonProperty(PropertyName = "Enable Fix")]
            public bool fix = true;

        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                configData = Config.ReadObject<ConfigData>();
                if (configData == null) LoadDefaultConfig();
            }
            catch
            {
                PrintError("Configuration file can't be read.");
                LoadDefaultConfig();
                return;
            }
            SaveConfig();
        }
        protected override void LoadDefaultConfig() => configData = new ConfigData(); 
        protected override void SaveConfig() => Config.WriteObject(configData);

        private void OnServerInitialized()
        {
            if (configData.noSun)
            {
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.cloud_opacity 1");
            }
            else if (configData.fix)
            {
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.cloud_opacity 0.965");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.cloud_brightness 1.5");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.cloud_coloring 0");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.cloud_attenuation -1");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.cloud_saturation 1");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.cloud_scattering 0");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.cloud_sharpness 0");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.cloud_brightness 0.75");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.cloud_size 0");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.cloud_coverage 1");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.rain 0");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.clear_chance 1");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.fog 0");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.rain 0");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.thunder 0"); 
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.atmosphere_contrast 1.2");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.atmosphere_directionality 0");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.atmosphere_mie 0");
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), "weather.atmosphere_rayleigh 1.3");
            }
            else
            {
                return;
            }
        }

    }
}

