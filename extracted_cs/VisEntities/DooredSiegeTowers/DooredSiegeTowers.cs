/*
 * Copyright (C) 2024 Game4Freak.io
 * This mod is provided under the Game4Freak EULA.
 * Full legal terms can be found at https://game4freak.io/eula/
 */

using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Doored Siege Towers", "VisEntities", "1.0.0")]
    [Description("Adds a door to siege towers automatically.")]
    public class DooredSiegeTowers : RustPlugin
    {
        #region Fields

        private static DooredSiegeTowers _plugin;
        private static Configuration _config;

        private static readonly Vector3 _doorLocalPosition = new Vector3(0f, 1.92f, -1.65f);
        private static readonly Quaternion _doorLocalRotation = Quaternion.Euler(0f, 270f, 0f);

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Door Prefab")]
            public string DoorPrefab { get; set; }

            [JsonProperty("Door Skins")]
            public List<ulong> DoorSkins { get; set; }
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
                DoorPrefab = "assets/prefabs/building/door.hinged/door.hinged.metal.prefab",
                DoorSkins = new List<ulong>
                {
                    3216933839,
                    3208884292,
                    3173120319,
                    2655845884
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

        private void OnEntitySpawned(SiegeTower siegeTower)
        {
            if (siegeTower == null)
                return;

            Door door = GameManager.server.CreateEntity(_config.DoorPrefab, _doorLocalPosition, _doorLocalRotation) as Door;
            if (door != null)
            {
                door.SetParent(siegeTower);

                if (_config.DoorSkins != null && _config.DoorSkins.Count > 0)
                {
                    int randomIndex = Random.Range(0, _config.DoorSkins.Count);
                    door.skinID = _config.DoorSkins[randomIndex];
                }

                door.Spawn();
            }
        }

        #endregion Oxide Hooks
    }
}