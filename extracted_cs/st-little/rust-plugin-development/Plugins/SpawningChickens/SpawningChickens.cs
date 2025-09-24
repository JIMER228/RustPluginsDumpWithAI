using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Spawning Chickens", "st-little", "0.1.0")]
    [Description("Throw a painted egg and a chicken will spawn.")]
    public class SpawningChickens : RustPlugin
    {
        #region Fields

        private const string EasterEggProjectilePrefab = "assets/prefabs/misc/easter/easter basket/eastereggprojectile.prefab";
        private const string ChickenPrefab = "assets/rust.ai/agents/chicken/chicken.prefab";

        #endregion

        #region Configuration

        private Configuration _configuration;

        private class Configuration
        {
            public int ProbabilityOfChickenSpawning;
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                ProbabilityOfChickenSpawning = 12,
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _configuration = Config.ReadObject<Configuration>();

                if (_configuration == null)
                    LoadDefaultConfig();
            }
            catch
            {
                PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig() => _configuration = GetDefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(_configuration);

        #endregion

        #region Oxide hooks

        object OnEntityKill(BaseNetworkable entity)
        {
            switch (entity.PrefabName)
            {
                case EasterEggProjectilePrefab:
                    EasterEggProjectileHandler(entity);
                    break;
            }

            return null;
        }

        #endregion

        #region Main

        private void EasterEggProjectileHandler(BaseNetworkable entity)
        {
            if (UnityEngine.Random.Range(0, 100) < _configuration.ProbabilityOfChickenSpawning)
            {
                SpawnChicken(entity.transform.position);
            }
        }

        private static Chicken? SpawnChicken(Vector3 position)
        {
            var chicken = GameManager.server.CreateEntity(ChickenPrefab, position) as Chicken;
            if (chicken == null) return null;

            chicken.Spawn();
            return chicken;
        }

        #endregion
    }
}


