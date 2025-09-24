// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("Module Car License Plate", "st-little", "0.3.0")]
    [Description("This plugin spawns a module car with a license plate.")]
    public class ModuleCarLicensePlate : RustPlugin
    {
        private const string SmallWoodSignPrefab = "assets/prefabs/deployable/signs/sign.small.wood.prefab";
        private const string TwoModuleCarSpawnedPrefab = "assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab";
        private const string ThreeModuleCarSpawnedPrefab = "assets/content/vehicles/modularcar/3module_car_spawned.entity.prefab";
        private const string FourModuleCarSpawnedPrefab = "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab";
        private const string TwoModuleCarChassis = "assets/content/vehicles/modularcar/car_chassis_2module.entity.prefab";
        private const string ThreeModuleCarChassis = "assets/content/vehicles/modularcar/car_chassis_3module.entity.prefab";
        private const string FourModuleCarChassis = "assets/content/vehicles/modularcar/car_chassis_4module.entity.prefab";

        #region Configuration

        private Configuration _configuration;

        private class Configuration
        {
            public bool EnableTwoModuleCar;
            public bool EnableThreeModuleCar;
            public bool EnableFourModuleCar;

            public float TwoModuleCarPositionX;
            public float TwoModuleCarPositionY;
            public float TwoModuleCarPositionZ;

            public float ThreeModuleCarPositionX;
            public float ThreeModuleCarPositionY;
            public float ThreeModuleCarPositionZ;

            public float FourModuleCarPositionX;
            public float FourModuleCarPositionY;
            public float FourModuleCarPositionZ;
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                EnableTwoModuleCar = true,
                EnableThreeModuleCar = true,
                EnableFourModuleCar = true,

                TwoModuleCarPositionX = 0f,
                TwoModuleCarPositionY = 0.25f,
                TwoModuleCarPositionZ = -1.80f,

                ThreeModuleCarPositionX = 0f,
                ThreeModuleCarPositionY = 0.25f,
                ThreeModuleCarPositionZ = -2.55f,

                FourModuleCarPositionX = 0f,
                FourModuleCarPositionY = 0.25f,
                FourModuleCarPositionZ = -3.30f,
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

        #region Data Files

        private class StoredData
        {
            // Cars with added license plate.
            public List<ulong> addedLicensePlateCars = new List<ulong>();

            public StoredData()
            {
            }
        }

        private const string DataFileName = "ModuleCarLicensePlate";
        private StoredData storedData;

        #endregion

        #region Oxide Hooks

        private void Init()
        {
            storedData = Core.Interface.Oxide.DataFileSystem.ReadObject<StoredData>(DataFileName);
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (!IsModuleCar(entity.PrefabName) && !IsModuleCarChassis(entity.PrefabName)) return;
            if (IsTwoModuleCar(entity.PrefabName) || TwoModuleCarChassis == entity.PrefabName)
            {
                if (_configuration.EnableTwoModuleCar)
                {
                    AddLicensePlate(entity as BaseVehicle, new Vector3(_configuration.TwoModuleCarPositionX, _configuration.TwoModuleCarPositionY, _configuration.TwoModuleCarPositionZ));
                }
                return;
            }
            if (IsThreeModuleCar(entity.PrefabName) || ThreeModuleCarChassis == entity.PrefabName)
            {
                if (_configuration.EnableThreeModuleCar)
                {
                    AddLicensePlate(entity as BaseVehicle, new Vector3(_configuration.ThreeModuleCarPositionX, _configuration.ThreeModuleCarPositionY, _configuration.ThreeModuleCarPositionZ));
                }
                return;
            }
            if (IsFourModuleCar(entity.PrefabName) || FourModuleCarChassis == entity.PrefabName)
            {
                if (_configuration.EnableFourModuleCar)
                {
                    AddLicensePlate(entity as BaseVehicle, new Vector3(_configuration.FourModuleCarPositionX, _configuration.FourModuleCarPositionY, _configuration.FourModuleCarPositionZ));
                }
                return;
            }
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            if (!IsModuleCar(entity.PrefabName) && !IsModuleCarChassis(entity.PrefabName)) return;
            if (!storedData.addedLicensePlateCars.Contains(entity.net.ID.Value)) return;

            storedData.addedLicensePlateCars.Remove(entity.net.ID.Value);
            Core.Interface.Oxide.DataFileSystem.WriteObject(DataFileName, storedData);
        }

        #endregion

        private void AddLicensePlate(BaseVehicle vehicle, Vector3 localPosition)
        {
            if (storedData.addedLicensePlateCars.Contains(vehicle.net.ID.Value)) return;

            var licensePlate = GameManager.server.CreateEntity(SmallWoodSignPrefab, vehicle.transform.position) as Signage;
            if (licensePlate == null) return;

            licensePlate.SetFlag(BaseEntity.Flags.Reserved8, true);
            licensePlate.SetParent(vehicle);
            licensePlate.pickup.enabled = false;
            licensePlate.transform.localPosition = localPosition;
            Vector3 licensePlateAngles = licensePlate.transform.localEulerAngles;
            licensePlateAngles.x = 180.0f;
            licensePlateAngles.z = 180.0f;
            licensePlate.transform.localEulerAngles = licensePlateAngles;

            RemoveColliderProtection(licensePlate);

            licensePlate.Spawn();
            licensePlate.SendNetworkUpdateImmediate(true);

            storedData.addedLicensePlateCars.Add(vehicle.net.ID.Value);
            Core.Interface.Oxide.DataFileSystem.WriteObject(DataFileName, storedData);
        }

        /// <summary>
        /// Determines whether the given entity is a modular car prefab (spawned or non-spawned variant).
        /// </summary>
        private static bool IsModuleCar(string? prefab)
        {
            if (string.IsNullOrEmpty(prefab)) return false;
            return SpawnedModuleCarRegex.IsMatch(prefab) || NonSpawnedModuleCarRegex.IsMatch(prefab);
        }

        private static bool IsTwoModuleCar(string? prefab)
        {
            if (string.IsNullOrEmpty(prefab)) return false;
            return prefab == TwoModuleCarSpawnedPrefab || NonSpawnedTwoModuleCarRegex.IsMatch(prefab);
        }

        private static bool IsThreeModuleCar(string? prefab)
        {
            if (string.IsNullOrEmpty(prefab)) return false;
            return prefab == ThreeModuleCarSpawnedPrefab || NonSpawnedThreeModuleCarRegex.IsMatch(prefab);
        }

        private static bool IsFourModuleCar(string? prefab)
        {
            if (string.IsNullOrEmpty(prefab)) return false;
            return prefab == FourModuleCarSpawnedPrefab || NonSpawnedFourModuleCarRegex.IsMatch(prefab);
        }

        private static bool IsModuleCarChassis(string? prefab)
        {
            if (string.IsNullOrEmpty(prefab)) return false;
            return ModuleCarChassisRegex.IsMatch(prefab);
        }

        // Matches spawned (temporary) modular car variants with "_spawned" in the name
        private static readonly Regex SpawnedModuleCarRegex = new Regex(
                @"^assets/content/vehicles/modularcar/[234]module_car_spawned\.entity\.prefab$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Matches base (non "_spawned") modular car admin prefabs:
        // 2 module: 01-08
        // 3 module: 01-12
        // 4 module: 01-11
        // Corrected pattern so that 10+ variants do not get an extra leading zero (previous pattern produced 010,011,012 etc.)
        private static readonly Regex NonSpawnedModuleCarRegex = new Regex(
                @"^assets/content/vehicles/modularcar/admin_prefabs/(2_modules/car_2mod_0[1-8]|3_modules/car_3mod_(0[1-9]|1[0-2])|4_modules/car_4mod_(0[1-9]|1[0-1]))\.prefab$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Specific regex only for non-spawned 2-module car variants (01-08)
        private static readonly Regex NonSpawnedTwoModuleCarRegex = new Regex(
            @"^assets/content/vehicles/modularcar/admin_prefabs/2_modules/car_2mod_0[1-8]\.prefab$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex NonSpawnedThreeModuleCarRegex = new Regex(
            @"^assets/content/vehicles/modularcar/admin_prefabs/3_modules/car_3mod_(0[1-9]|1[0-2])\.prefab$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex NonSpawnedFourModuleCarRegex = new Regex(
            @"^assets/content/vehicles/modularcar/admin_prefabs/4_modules/car_4mod_(0[1-9]|1[0-1])\.prefab$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex ModuleCarChassisRegex = new Regex(
            @"^assets/content/vehicles/modularcar/car_chassis_[234]module\.entity\.prefab$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static void RemoveColliderProtection(BaseEntity colliderEntity)
        {
            foreach (var meshCollider in colliderEntity.GetComponentsInChildren<MeshCollider>())
            {
                UnityEngine.Object.DestroyImmediate(meshCollider);
            }

            UnityEngine.Object.DestroyImmediate(colliderEntity.GetComponent<GroundWatch>());
        }

        #region Test Methods

        internal protected static bool IsModuleCarWrapper(string? prefab) => IsModuleCar(prefab);
        internal protected static bool IsTwoModuleCarWrapper(string? prefab) => IsTwoModuleCar(prefab);
        internal protected static bool IsThreeModuleCarWrapper(string? prefab) => IsThreeModuleCar(prefab);
        internal protected static bool IsFourModuleCarWrapper(string? prefab) => IsFourModuleCar(prefab);
        internal protected static bool IsModuleCarChassisWrapper(string? prefab) => IsModuleCarChassis(prefab);

        #endregion
    }
}
