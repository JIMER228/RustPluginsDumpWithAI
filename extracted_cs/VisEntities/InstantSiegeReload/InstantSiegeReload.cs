/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Reflection;

namespace Oxide.Plugins
{
    [Info("Instant Siege Reload", "VisEntities", "1.0.1")]
    [Description("Lets you modify reload time for catapults and ballistas.")]
    public class InstantSiegeReload : RustPlugin
    {
        #region Fields

        private static InstantSiegeReload _plugin;
        private static Configuration _config;

        private const float DEFAULT_CATAPULT_RELOAD_TIME = 6f;
        private const float DEFAULT_BALLISTA_RELOAD_TIME = 3f;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Catapult Reload Duration Seconds")]
            public float CatapultReloadDurationSeconds { get; set; }

            [JsonProperty("Ballista Reload Duration Seconds")]
            public float BallistaReloadDurationSeconds { get; set; }
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
                CatapultReloadDurationSeconds = DEFAULT_CATAPULT_RELOAD_TIME,
                BallistaReloadDurationSeconds = DEFAULT_BALLISTA_RELOAD_TIME
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
            foreach (BaseEntity entity in BaseNetworkable.serverEntities)
            {
                if (entity == null)
                    continue;

                if (entity is Catapult || entity is BallistaGun)
                {
                    float defaultReloadTime = DEFAULT_BALLISTA_RELOAD_TIME;
                    if (entity is Catapult)
                        defaultReloadTime = DEFAULT_CATAPULT_RELOAD_TIME;
  
                    SetReloadTime(entity, defaultReloadTime);
                }
            }

            _config = null;
            _plugin = null;
        }

        private void OnServerInitialized(bool isStartup)
        {
            foreach (BaseEntity entity in BaseNetworkable.serverEntities)
            {
                if (entity == null)
                    continue;

                float newReloadTime = _config.BallistaReloadDurationSeconds;
                if (entity is Catapult)
                    newReloadTime = _config.CatapultReloadDurationSeconds;

                SetReloadTime(entity, newReloadTime);
            }
        }

        private void OnEntitySpawned(BaseEntity entity)
        {
            if (entity is Catapult)
            {
                SetReloadTime(entity, _config.CatapultReloadDurationSeconds);
            }
            else if (entity is BallistaGun)
            {
                SetReloadTime(entity, _config.BallistaReloadDurationSeconds);
            }
        }

        #endregion Oxide Hooks

        #region Reload Time Tweaking

        private void SetReloadTime(BaseEntity entity, float reloadTime)
        {
            if (entity is Catapult catapult)
            {
                FieldInfo reloadField = typeof(Catapult).GetField("reloadTime", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (reloadField != null)
                {
                    reloadField.SetValue(catapult, reloadTime);
                }
            }
            else if (entity is BallistaGun ballista)
            {
                FieldInfo reloadField = typeof(BallistaGun).GetField("reloadTime", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (reloadField != null)
                {
                    reloadField.SetValue(ballista, reloadTime);
                }
            }
        }

        #endregion Reload Time Tweaking
    }
}