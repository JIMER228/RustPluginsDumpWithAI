using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("NikoWeather", "The Friendly Chap", "1.0.0")]
    class NikoWeather : CovalencePlugin
    {
        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Storm Chance")]
            public float weatherstorm_chance = 0.0f;

            [JsonProperty(PropertyName = "Rain Chance")]
            public float weatherrain_chance = 0.0f;

            [JsonProperty(PropertyName = "Overcast Chance")]
            public float weatherovercast_chance = 0.0f;

            [JsonProperty(PropertyName = "Fog Chance")]
            public float weatherfog_chance = 0.0f;

            [JsonProperty(PropertyName = "Dust Chance")]
            public float weatherdust_chance = 0.0f;

            [JsonProperty(PropertyName = "Clear Chance")]
            public float weatherclear_chance = 1.0f;

            [JsonProperty(PropertyName = "Start Time")]
            public float envtime = 12.0f;

            [JsonProperty(PropertyName = "Rain")]
            public float weatherrain = 0.0f;

            [JsonProperty(PropertyName = "Wind")]
            public float weatherwind = 0.0f;

            [JsonProperty(PropertyName = "Fog")]
            public float weatherfog = 0.0f;

            [JsonProperty(PropertyName = "Rainbow")]
            public float weatherrainbow = 0.0f;

            [JsonProperty(PropertyName = "Thunder")]
            public float weatherthunder = 0.0f;

            [JsonProperty(PropertyName = "Atmosphere Brightness")]
            public float weatheratmosphere_brightness = 0.0f;

            [JsonProperty(PropertyName = "Atmosphere Contrast")]
            public float weatheratmosphere_contrast = -1.0f;

            [JsonProperty(PropertyName = "Atmosphere Directionality")]
            public float weatheratmosphere_directionality = 0.0f;

            [JsonProperty(PropertyName = "Atmosphere Mie")]
            public float weatheratmosphere_mie = 0.0f;

            [JsonProperty(PropertyName = "Atmosphere Rayleigh")]
            public float weatheratmosphere_rayleigh = 0.0f;

            [JsonProperty(PropertyName = "Cloud Attenuation")]
            public float weathercloud_attenuation = 0.0f;

            [JsonProperty(PropertyName = "Cloud Brightness")]
            public float weathercloud_brightness = 0.0f;

            [JsonProperty(PropertyName = "Cloud Colouring")]
            public float weathercloud_coloring = 0.0f;

            [JsonProperty(PropertyName = "Cloud Cover")]
            public float weathercloud_coverage = 0.0f;

            [JsonProperty(PropertyName = "Cloud Opacity")]
            public float weathercloud_opacity = 0.0f;

            [JsonProperty(PropertyName = "Cloud Saturation")]
            public float weathercloud_saturation = 0.0f;

            [JsonProperty(PropertyName = "Cloud Scattering")]
            public float weathercloud_scattering = 0.0f;

            [JsonProperty(PropertyName = "Cloud Sharpness")]
            public float weathercloud_sharpness = 0.0f;

            [JsonProperty(PropertyName = "Cloud Size")]
            public float weathercloud_size = 0.0f;
        }
        private bool LoadConfigVariables()
        {
            try
            {
                configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
                return false;
            }
            SaveConfig(configData);
            return true;
        }

        void Init()
        {
            if (!LoadConfigVariables())
            {
                Puts("Config Kaput, Fix it or Delete it.");
                return;
            }
        }

        void OnServerInitialized(bool initial)
        {
            ApplyWeatherCommands();
        }

        void Unload()
        {
            ResetWeatherToDefaults();
        }


        protected override void LoadDefaultConfig()
        {
            Puts("Making a new Config.");
            configData = new ConfigData();
            SaveConfig(configData);
        }

        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }
        private void ApplyWeatherCommands()
        {
            covalence.Server.Command("weather.storm_chance", configData.weatherstorm_chance);
            covalence.Server.Command("weather.rain_chance", configData.weatherrain_chance);
            covalence.Server.Command("weather.overcast_chance", configData.weatherovercast_chance);
            covalence.Server.Command("weather.fog_chance", configData.weatherfog_chance);
            covalence.Server.Command("weather.dust_chance", configData.weatherdust_chance);
            covalence.Server.Command("weather.clear_chance", configData.weatherclear_chance);
            covalence.Server.Command("env.time", configData.envtime);
            covalence.Server.Command("weather.rain", configData.weatherrain);
            covalence.Server.Command("weather.wind", configData.weatherwind);
            covalence.Server.Command("weather.fog", configData.weatherfog);
            covalence.Server.Command("weather.rainbow", configData.weatherrainbow);
            covalence.Server.Command("weather.thunder", configData.weatherthunder);
            covalence.Server.Command("weather.atmosphere_brightness", configData.weatheratmosphere_brightness);
            covalence.Server.Command("weather.atmosphere_contrast", configData.weatheratmosphere_contrast);
            covalence.Server.Command("weather.atmosphere_directionality", configData.weatheratmosphere_directionality);
            covalence.Server.Command("weather.atmosphere_mie", configData.weatheratmosphere_mie);
            covalence.Server.Command("weather.atmosphere_rayleigh", configData.weatheratmosphere_rayleigh);
            covalence.Server.Command("weather.cloud_attenuation", configData.weathercloud_attenuation);
            covalence.Server.Command("weather.cloud_brightness", configData.weathercloud_brightness);
            covalence.Server.Command("weather.cloud_coloring", configData.weathercloud_coloring);
            covalence.Server.Command("weather.cloud_coverage", configData.weathercloud_coverage);
            covalence.Server.Command("weather.cloud_opacity", configData.weathercloud_saturation);
            covalence.Server.Command("weather.cloud_saturation", configData.weathercloud_saturation);
            covalence.Server.Command("weather.cloud_scattering", configData.weathercloud_scattering);
            covalence.Server.Command("weather.cloud_sharpness", configData.weathercloud_sharpness);
            covalence.Server.Command("weather.cloud_size", configData.weathercloud_size);
        }
        private void ResetWeatherToDefaults()
        {
            covalence.Server.Command("weather.storm_chance", 0.02f);
            covalence.Server.Command("weather.rain_chance", 0.08f);
            covalence.Server.Command("weather.overcast_chance", 0.0f);
            covalence.Server.Command("weather.fog_chance", 0.03f);
            covalence.Server.Command("weather.dust_chance", 0.03f);
            covalence.Server.Command("weather.clear_chance", 0.9f);
            covalence.Server.Command("env.time", 16.333f);
            covalence.Server.Command("weather.rain", -1.0f);
            covalence.Server.Command("weather.wind", -1.0f);
            covalence.Server.Command("weather.fog", -1.0f);
            covalence.Server.Command("weather.rainbow", -1.0f);
            covalence.Server.Command("weather.thunder", -1.0f);
            covalence.Server.Command("weather.atmosphere_brightness", -1.0f);
            covalence.Server.Command("weather.atmosphere_contrast", -1.0f);
            covalence.Server.Command("weather.atmosphere_directionality", -1.0f);
            covalence.Server.Command("weather.atmosphere_mie", -1.0f);
            covalence.Server.Command("weather.atmosphere_rayleigh", -1.0f);
            covalence.Server.Command("weather.cloud_attenuation", -1.0f);
            covalence.Server.Command("weather.cloud_brightness", -1.0f);
            covalence.Server.Command("weather.cloud_coloring", -1.0f);
            covalence.Server.Command("weather.cloud_coverage", -1.0f);
            covalence.Server.Command("weather.cloud_opacity", -1.0f);
            covalence.Server.Command("weather.cloud_saturation", -1.0f);
            covalence.Server.Command("weather.cloud_scattering", -1.0f);
            covalence.Server.Command("weather.cloud_sharpness", -1.0f);
            covalence.Server.Command("weather.cloud_size", -1.0f);
        }
    }
}
