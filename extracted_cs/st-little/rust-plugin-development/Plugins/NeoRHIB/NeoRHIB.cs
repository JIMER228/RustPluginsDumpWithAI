// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Neo RHIB", "st-little", "0.3.0")]
    [Description("This plugin spawns RHIB equipped with a searchlight, locker, and barbeque.")]
    public class NeoRHIB : RustPlugin
    {
        private const string PermissionAdmin = "neorhib.admin";
        private const string PermissionSpawn = "neorhib.spawn";
        private const string PermissionUpgrade = "neorhib.upgrade";
        private const string PermissionDestroy = "neorhib.destroy";

        // TODO https://github.com/st-little/rust-plugin-development/issues/49
        // private const string SpherePrefab = "assets/prefabs/visualization/sphere.prefab";
        private const string SmallWoodSignPrefab = "assets/prefabs/deployable/signs/sign.small.wood.prefab";
        public const string LockerPrefab = "assets/prefabs/deployable/locker/locker.deployed.prefab";
        public const string BbqPrefab = "assets/bundled/prefabs/static/bbq.static.prefab";
        private const string RHIBPrefab = "assets/content/vehicles/boats/rhib/rhib.prefab";
        private const string SearchLightPrefab = "assets/prefabs/deployable/search light/searchlight.deployed.prefab";

        [PluginReference]
        Plugin EntityScaleManager;

        private static NeoRHIB _pluginInstance;

        #region Oxide Hooks

        private void Init()
        {
            permission.RegisterPermission(PermissionAdmin, this);
            permission.RegisterPermission(PermissionSpawn, this);
            permission.RegisterPermission(PermissionUpgrade, this);
            permission.RegisterPermission(PermissionDestroy, this);
            _pluginInstance = this;
        }

        private void Unload()
        {
            _pluginInstance = null;
        }

        // TODO https://github.com/st-little/rust-plugin-development/issues/49
        // void OnEngineStarted(BaseVehicle vehicle, BasePlayer driver)
        // {
        //     if (vehicle.PrefabName != RHIBPrefab) return;

        //     foreach (var vehicleChild in vehicle.children)
        //     {
        //         if (vehicleChild.PrefabName != SpherePrefab) continue;

        //         foreach (var sphereChild in vehicleChild.children)
        //         {
        //             if (sphereChild.PrefabName != SearchLightPrefab) continue;

        //             sphereChild.SetFlag(BaseEntity.Flags.Reserved8, true);
        //         }
        //     }
        // }

        // TODO https://github.com/st-little/rust-plugin-development/issues/49
        // void OnEngineStopped(BaseVehicle vehicle)
        // {
        //     if (vehicle.PrefabName != RHIBPrefab) return;

        //     foreach (var vehicleChild in vehicle.children)
        //     {
        //         if (vehicleChild.PrefabName != SpherePrefab) continue;

        //         foreach (var sphereChild in vehicleChild.children)
        //         {
        //             if (sphereChild.PrefabName != SearchLightPrefab) continue;

        //             sphereChild.SetFlag(BaseEntity.Flags.Reserved8, false);
        //         }
        //     }
        // }

        #endregion

        #region Commands

        [ChatCommand("neorhib")]
        private void NeoRHIBCommand(BasePlayer player, string command, string[] args)
        {
            var hit = GetPlayerEyesHeadRay(player);
            if (hit == null) return;

            if (args.Length == 0)
            {
                PrintToChat(player, "Please specify the mode. /neorhib spawn or /neorhib upgrade or /neorhib destroy");
                return;
            }

            switch (args[0].ToLower())
            {
                case "spawn":
                    if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin) && !permission.UserHasPermission(player.UserIDString, PermissionSpawn))
                    {
                        PrintToChat(player, "You don't have permission to use this command!");
                        return;
                    }

                    if (args.Length == 1)
                    {
                        SpawnNeoRHIB(hit.Value.point);
                    }
                    else
                    {
                        SpawnNeoRHIB(hit.Value.point, null, args.Contains("searchlight"), args.Contains("bbq"), args.Contains("locker"), args.Contains("smallwoodsign"));
                    }
                    break;
                case "upgrade":
                    if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin) && !permission.UserHasPermission(player.UserIDString, PermissionUpgrade))
                    {
                        PrintToChat(player, "You don't have permission to use this command!");
                        return;
                    }

                    if (hit?.GetEntity()?.name == RHIBPrefab)
                    {
                        if (args.Length == 1)
                        {
                            UpgradeToNeoRHIB(hit?.GetEntity() as RHIB);
                        }
                        else
                        {
                            UpgradeToNeoRHIB(hit?.GetEntity() as RHIB, args.Contains("searchlight"), args.Contains("bbq"), args.Contains("locker"), args.Contains("smallwoodsign"));
                        }
                    }
                    else
                    {
                        PrintToChat(player, "Please look at the RHIB you want to upgrade.");
                    }
                    break;
                case "destroy":
                    if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin) && !permission.UserHasPermission(player.UserIDString, PermissionDestroy))
                    {
                        PrintToChat(player, "You don't have permission to use this command!");
                        return;
                    }

                    if (hit?.GetEntity()?.name == RHIBPrefab)
                    {
                        var error = DestroyNeoRHIB(hit?.GetEntity() as RHIB);
                        if (error != null)
                        {
                            PrintToChat(player, error);
                        }
                    }
                    else
                    {
                        PrintToChat(player, "Please look at the RHIB you want to destroy.");
                    }
                    break;
                default:
                    PrintToChat(player, "Please specify the mode. /neorhib spawn or /neorhib upgrade or /neorhib destroy");
                    break;
            }
        }

        #endregion

        private static RHIB? SpawnNeoRHIB(Vector3 spawnPosition, ulong? ownerID = null, bool isAddSearchlightAboveTheCockpit = true, bool isAddAddBbqBetweenTheRearSeats = true, bool isAddLockerOnTheRearHatch = true, bool isAddSmallWoodSignToTheFrontOfTheCockpit = true)
        {
            var rhibEntity = GameManager.server.CreateEntity(RHIBPrefab, spawnPosition) as RHIB;
            if (rhibEntity == null) return null;

            if (ownerID != null)
            {
                rhibEntity.OwnerID = (ulong)ownerID;
            }
            rhibEntity.Spawn();

            return UpgradeToNeoRHIB(rhibEntity, isAddSearchlightAboveTheCockpit, isAddAddBbqBetweenTheRearSeats, isAddLockerOnTheRearHatch, isAddSmallWoodSignToTheFrontOfTheCockpit);
        }

        private static RHIB UpgradeToNeoRHIB(RHIB rhibEntity, bool isAddSearchlightAboveTheCockpit = true, bool isAddAddBbqBetweenTheRearSeats = true, bool isAddLockerOnTheRearHatch = true, bool isAddSmallWoodSignToTheFrontOfTheCockpit = true)
        {
            if (isAddSearchlightAboveTheCockpit) AddSearchlightAboveTheCockpit(rhibEntity);
            if (isAddAddBbqBetweenTheRearSeats) AddBbqBetweenTheRearSeats(rhibEntity);
            if (isAddLockerOnTheRearHatch) AddLockerOnTheRearHatch(rhibEntity);
            if (isAddSmallWoodSignToTheFrontOfTheCockpit) AddSmallWoodSignToTheFrontOfTheCockpit(rhibEntity);
            rhibEntity.SendNetworkUpdateImmediate();

            return rhibEntity;
        }

        private static string? DestroyNeoRHIB(RHIB rhibEntity, ulong? ownerID = null)
        {
            if (ownerID != null && rhibEntity.OwnerID != ownerID)
            {
                return "You don't have permission to destroy this RHIB!";
            }

            rhibEntity.Kill();
            return null;
        }

        private class EntityTransform
        {
            public Vector3 LocalPosition;
            public float LocalScale;
            public Vector3 LocalEulerAngles;

        }

        private static void AddSearchlight(BaseVehicle vehicle, EntityTransform entityTransform)
        {
            var searchLight = GameManager.server.CreateEntity(SearchLightPrefab, entityTransform.LocalPosition) as SearchLight;
            if (searchLight == null) return;

            searchLight.SetParent(vehicle);
            // TODO https://github.com/st-little/rust-plugin-development/issues/49
            searchLight.SetFlag(BaseEntity.Flags.Reserved8, true);  // The search light is always on.
            searchLight.SetFlag(BaseEntity.Flags.Busy, true);       // Lock the searchlight so that it does not move.
            searchLight.pickup.enabled = false;
            searchLight.transform.localEulerAngles = entityTransform.LocalEulerAngles;

            RemoveColliderProtection(searchLight);

            searchLight.Spawn();

            _pluginInstance.EntityScaleManager.CallHook("API_ScaleEntity", searchLight, entityTransform.LocalScale);
        }

        private static void AddBbq(BaseVehicle vehicle, EntityTransform entityTransform)
        {
            var bbqEntity = GameManager.server.CreateEntity(BbqPrefab, entityTransform.LocalPosition) as BaseOven;
            if (bbqEntity == null) return;

            bbqEntity.dropsLoot = true;
            bbqEntity.SetParent(vehicle);
            bbqEntity.pickup.enabled = false;
            bbqEntity.transform.localEulerAngles = entityTransform.LocalEulerAngles;

            RemoveColliderProtection(bbqEntity);

            bbqEntity.Spawn();

            _pluginInstance.EntityScaleManager.CallHook("API_ScaleEntity", bbqEntity, entityTransform.LocalScale);
        }

        private static void AddLocker(BaseVehicle vehicle, EntityTransform entityTransform)
        {
            var locker = GameManager.server?.CreateEntity(LockerPrefab, entityTransform.LocalPosition) as Locker;
            if (locker == null) return;

            locker.SetParent(vehicle);
            locker.pickup.enabled = false;
            locker.transform.localEulerAngles = entityTransform.LocalEulerAngles;

            RemoveColliderProtection(locker);

            locker.Spawn();

            _pluginInstance.EntityScaleManager.CallHook("API_ScaleEntity", locker, entityTransform.LocalScale);
        }

        private static void AddSmallWoodSign(BaseVehicle vehicle, EntityTransform entityTransform)
        {
            var smallWoodSign = GameManager.server.CreateEntity(SmallWoodSignPrefab, entityTransform.LocalPosition) as Signage;
            if (smallWoodSign == null) return;

            smallWoodSign.SetFlag(BaseEntity.Flags.Reserved8, true);
            smallWoodSign.SetParent(vehicle);
            smallWoodSign.pickup.enabled = false;
            smallWoodSign.transform.localEulerAngles = entityTransform.LocalEulerAngles;

            RemoveColliderProtection(smallWoodSign);

            smallWoodSign.Spawn();

            _pluginInstance.EntityScaleManager.CallHook("API_ScaleEntity", smallWoodSign, entityTransform.LocalScale);
        }

        private static void AddSearchlightAboveTheCockpit(BaseVehicle vehicle)
        {
            AddSearchlight(vehicle, new EntityTransform
            {
                LocalScale = 0.4f,
                LocalPosition = new Vector3(0.27f, 3.2f, 0.85f),
                LocalEulerAngles = new Vector3(0f, 180.0f, 0f)
            });
        }

        private static void AddBbqBetweenTheRearSeats(BaseVehicle vehicle)
        {
            AddBbq(vehicle, new EntityTransform
            {
                LocalScale = 0.4f,
                LocalPosition = new Vector3(0f, 1.5f, -1.3f),
                LocalEulerAngles = new Vector3(0f, -90.0f, 0f)
            });
        }

        private static void AddLockerOnTheRearHatch(BaseVehicle vehicle)
        {
            AddLocker(vehicle, new EntityTransform
            {
                LocalScale = 0.6f,
                LocalPosition = new Vector3(0f, 1.15f, -1.65f),
                LocalEulerAngles = new Vector3(-90.0f, 0f, 0f)
            });
        }

        private static void AddSmallWoodSignToTheFrontOfTheCockpit(BaseVehicle vehicle)
        {
            AddSmallWoodSign(vehicle, new EntityTransform
            {
                LocalScale = 1f,
                LocalPosition = new Vector3(0f, 1.5f, 1.3f),
                LocalEulerAngles = new Vector3(-30f, 0f, 0f)
            });
        }

        private static RaycastHit? GetPlayerEyesHeadRay(BasePlayer basePlayer, float maxDistance = Mathf.Infinity)
        {
            return Physics.Raycast(basePlayer.eyes.HeadRay(), out var hit, maxDistance, Physics.DefaultRaycastLayers)
                ? hit
                : null;
        }

        private static void RemoveColliderProtection(BaseEntity colliderEntity)
        {
            foreach (var meshCollider in colliderEntity.GetComponentsInChildren<MeshCollider>())
            {
                UnityEngine.Object.DestroyImmediate(meshCollider);
            }

            UnityEngine.Object.DestroyImmediate(colliderEntity.GetComponent<GroundWatch>());
        }
    }
}
