//            ________               __                      //
//           / ____/ /_  ____  _____/ /___  __              //
//          / / __/ __ \/ __ \/ ___/ __/ / / /             //
//         / /_/ / / / / /_/ (__  ) /_/ /_/ /             //
//         \____/_/ /_/\____/____/\__/\__, /             //
//                                   /____/             //
//                                                     //
////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust.Modular;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Vehicle Spawner", "Ghosty", "1.0.3")]
    class VehicleSpawner : RustPlugin
    {
        [PluginReference]
        Plugin ImageLibrary;

        private class StoredData
        {
            public Dictionary<string, PlayerData> Players = new Dictionary<string, PlayerData>();
        }

        private class PlayerData
        {
            public Dictionary<string, DateTime> LastSpawnTimes = new Dictionary<string, DateTime>();
            public List<ulong> VehicleIDs = new List<ulong>();
            public int VehicleLimit = 1;
        }

        private const string PermissionUse = "vehiclespawner.use";
        private const string PermissionDespawn = "vehiclespawner.despawn";
        private const string PermissionCooldown1M = "vehiclespawner.cooldown.1m";
        private const string PermissionCooldown1H = "vehiclespawner.cooldown.1h";
        private const string PermissionCooldown1D = "vehiclespawner.cooldown.1d";
        private const string PermissionMinicopter = "vehiclespawner.minicopter";
        private const string Permissioncar = "vehiclespawner.car";
        private const string PermissionNoCooldown = "vehiclespawner.nocooldown";
        private const string PermissionVehicleLimit3 = "vehiclespawner.limit.3";
        private const string PermissionVehicleLimit6 = "vehiclespawner.limit.6";
        private const string PermissionVehicleLimitUnlimited = "vehiclespawner.limit.unlimited";
        private const int SpawnPointLayerMask = Rust.Layers.Solid | Rust.Layers.Mask.Water;
        private Dictionary<ulong, Dictionary<string, DateTime>> lastSpawnTimes =
            new Dictionary<ulong, Dictionary<string, DateTime>>();
        private Dictionary<ulong, DateTime> operationCooldowns = new Dictionary<ulong, DateTime>();
        private const double FetchDestroyCooldownSeconds = 3.0;
        private StoredData storedData;
        private string[] carPrefabs = new string[]
        {
            "assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab",
            "assets/content/vehicles/modularcar/3module_car_spawned.entity.prefab",
            "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab",
        };

        void Init()
        {
            permission.RegisterPermission(PermissionUse, this);
            permission.RegisterPermission(PermissionDespawn, this);
            permission.RegisterPermission(PermissionCooldown1M, this);
            permission.RegisterPermission(PermissionCooldown1H, this);
            permission.RegisterPermission(PermissionCooldown1D, this);
            permission.RegisterPermission(PermissionMinicopter, this);
            permission.RegisterPermission(Permissioncar, this);
            permission.RegisterPermission(PermissionNoCooldown, this);
            permission.RegisterPermission(PermissionVehicleLimit3, this);
            permission.RegisterPermission(PermissionVehicleLimit6, this);
            permission.RegisterPermission(PermissionVehicleLimitUnlimited, this);
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("VehicleSpawner");
            ImageLibrary?.Call("AddImage", "https://i.imgur.com/ckEkBqZ.png", "CarImage");
            ImageLibrary?.Call("AddImage", "https://i.imgur.com/4jAEtww.png", "HelicopterImage");
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("VehicleSpawner", storedData);
        }

        void OnServerSave()
        {
            SaveData();
            Puts("Vehicle Spawner is now saving data...");
        }

        void OnNewSave(string filename)
        {
            storedData = new StoredData();
            Interface.Oxide.DataFileSystem.WriteObject("VehicleSpawner", storedData);
            Puts("Vehicle Spawner data has been wiped!");
        }

        void OnPlayerInit(BasePlayer player)
        {
            UpdatePlayerVehicleLimit(player);
        }

        private bool CanSpawnVehicle(BasePlayer player, string vehicleType)
        {
            var playerId = player.UserIDString;
            if (permission.UserHasPermission(playerId, PermissionNoCooldown))
            {
                return true;
            }
            if (storedData.Players.TryGetValue(playerId, out PlayerData playerData))
            {
                if (playerData.LastSpawnTimes.TryGetValue(vehicleType, out DateTime lastSpawnTime))
                {
                    var cooldownSeconds = GetCooldownSeconds(GetCooldownPermission(player));
                    var timeElapsed = (DateTime.UtcNow - lastSpawnTime).TotalSeconds;
                    if (timeElapsed < cooldownSeconds)
                    {
                        var remainingCooldown = cooldownSeconds - timeElapsed;
                        var timeSpan = TimeSpan.FromSeconds(remainingCooldown);
                        var formattedTime = string.Format(
                            "{0:D2}h:{1:D2}m:{2:D2}s",
                            timeSpan.Hours,
                            timeSpan.Minutes,
                            timeSpan.Seconds
                        );
                        player.ChatMessage(
                            $"You must wait {formattedTime} before spawning another {vehicleType}."
                        );
                        return false;
                    }
                }
            }
            int vehicleLimit = GetPlayerVehicleLimit(player);
            int currentVehicleCount = playerData?.VehicleIDs?.Count ?? 0;
            if (currentVehicleCount >= vehicleLimit)
            {
                player.ChatMessage(
                    $"You have reached your vehicle limit of {vehicleLimit} and cannot spawn more."
                );
                return false;
            }
            return true;
        }

        private string GetCooldownPermission(BasePlayer player)
        {
            if (permission.UserHasPermission(player.UserIDString, PermissionCooldown1D))
                return PermissionCooldown1D;
            if (permission.UserHasPermission(player.UserIDString, PermissionCooldown1H))
                return PermissionCooldown1H;
            if (permission.UserHasPermission(player.UserIDString, PermissionCooldown1M))
                return PermissionCooldown1M;

            return null;
        }

        private float GetCooldownSeconds(string cooldownPermission)
        {
            switch (cooldownPermission)
            {
                case PermissionCooldown1M:
                    return 60;
                case PermissionCooldown1H:
                    return 3600;
                case PermissionCooldown1D:
                    return 86400;
                default:
                    return 0;
            }
        }

        private int GetPlayerVehicleLimit(BasePlayer player)
        {
            string playerId = player.UserIDString;

            if (permission.UserHasPermission(playerId, PermissionVehicleLimitUnlimited))
            {
                return int.MaxValue;
            }
            if (permission.UserHasPermission(playerId, PermissionVehicleLimit6))
            {
                return 6;
            }
            if (permission.UserHasPermission(playerId, PermissionVehicleLimit3))
            {
                return 3;
            }
            return 1;
        }

        void SetPlayerVehicleLimit(string playerId, int newLimit)
        {
            if (!storedData.Players.ContainsKey(playerId))
            {
                storedData.Players[playerId] = new PlayerData();
            }

            storedData.Players[playerId].VehicleLimit = newLimit;
            Interface.Oxide.DataFileSystem.WriteObject("VehicleSpawner", storedData);
        }

        private bool CanSpawnAnotherVehicle(BasePlayer player)
        {
            var playerId = player.UserIDString;

            if (!storedData.Players.TryGetValue(playerId, out PlayerData playerData))
            {
                storedData.Players[playerId] = new PlayerData();
            }

            int vehicleLimit = GetPlayerVehicleLimit(player);
            if (playerData.VehicleIDs.Count >= vehicleLimit)
            {
                player.ChatMessage(
                    $"You have reached your vehicle limit.\nYou have {vehicleLimit} vehicle spawned, try fetching it.."
                );
                Effect.server.Run(
                    "assets/prefabs/locks/keypad/effects/lock.code.denied.prefab",
                    player.transform.position
                );
                return false;
            }

            return true;
        }

        private void UpdatePlayerVehicleLimit(BasePlayer player)
        {
            var playerId = player.UserIDString;
            var newLimit = GetPlayerVehicleLimit(player);

            if (!storedData.Players.ContainsKey(playerId))
            {
                storedData.Players[playerId] = new PlayerData();
            }

            storedData.Players[playerId].VehicleLimit = newLimit;
            SaveData();
        }

        private bool TryGetSpawnPoint(BasePlayer player, out Vector3 spawnPosition)
        {
            Vector3 forward = player.eyes.HeadForward();
            Vector3 startPosition = player.eyes.position + forward * 4f;
            RaycastHit hit;
            if (Physics.Raycast(startPosition, Vector3.down, out hit, 50f, SpawnPointLayerMask))
            {
                if (Vector3.Angle(Vector3.up, hit.normal) < 30)
                {
                    spawnPosition = hit.point + Vector3.up * 2f;
                    return true;
                }
            }
            if (
                Physics.SphereCast(
                    startPosition,
                    1.5f,
                    Vector3.down,
                    out hit,
                    10f,
                    SpawnPointLayerMask
                )
            )
            {
                if (Vector3.Angle(Vector3.up, hit.normal) < 30)
                {
                    spawnPosition = hit.point + Vector3.up * 2f;
                    return true;
                }
            }
            if (
                Physics.Raycast(
                    player.transform.position + Vector3.up,
                    forward,
                    out hit,
                    10f,
                    SpawnPointLayerMask
                )
            )
            {
                spawnPosition = player.transform.position + forward * 7f;
                spawnPosition.y = hit.point.y + 2f;
                return true;
            }

            spawnPosition = Vector3.zero;
            return false;
        }

        private bool ValidSurfaceBelow(Vector3 position)
        {
            RaycastHit hit;
            if (Physics.Raycast(position, Vector3.down, out hit, 10f, SpawnPointLayerMask))
            {
                return true;
            }
            return false;
        }

        private void SpawnVehicle(
            string prefabPath,
            Vector3 spawnPosition,
            Quaternion spawnRotation,
            BasePlayer player,
            string vehicleType
        )
        {
            if (!CanSpawnVehicle(player, vehicleType) || !CanSpawnAnotherVehicle(player))
                return;

            Quaternion adjustedSpawnRotation = Quaternion.Euler(
                spawnRotation.eulerAngles.x,
                spawnRotation.eulerAngles.y + -50,
                spawnRotation.eulerAngles.z
            );

            BaseEntity vehicleEntity = GameManager.server.CreateEntity(
                prefabPath,
                spawnPosition,
                adjustedSpawnRotation
            );
            if (vehicleEntity != null)
            {
                vehicleEntity.Spawn();
                var playerId = player.UserIDString;
                if (!storedData.Players.ContainsKey(playerId))
                {
                    storedData.Players[playerId] = new PlayerData();
                }
                storedData.Players[playerId].VehicleIDs.Add(vehicleEntity.net.ID.Value);
                storedData.Players[playerId].LastSpawnTimes[vehicleType] = DateTime.UtcNow;
                SaveData();
                player.ChatMessage($"{vehicleType} spawned!");
                Effect.server.Run(
                    "assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab",
                    player.transform.position
                );
            }
            else
            {
                player.ChatMessage("Failed to spawn the vehicle. Please try again.");
            }
        }

        private void UpdateLastSpawnTime(ulong playerId, string vehicleType)
        {
            if (!lastSpawnTimes.ContainsKey(playerId))
            {
                lastSpawnTimes[playerId] = new Dictionary<string, DateTime>();
            }
            lastSpawnTimes[playerId][vehicleType] = DateTime.UtcNow;
        }

        private void UpdatePlayerData(BasePlayer player, string vehicleType, BaseEntity vehicle)
        {
            var playerId = player.UserIDString;
            if (!storedData.Players.ContainsKey(playerId))
            {
                storedData.Players[playerId] = new PlayerData();
            }
            storedData.Players[playerId].LastSpawnTimes[vehicleType] = DateTime.UtcNow;
            storedData.Players[playerId].VehicleIDs.Add(vehicle.net.ID.Value);
            Interface.Oxide.DataFileSystem.WriteObject("VehicleSpawner", storedData);
        }

        private void PerformVehicleOperation(BasePlayer player, bool isFetch, string vehicleType)
        {
            var playerId = player.userID;
            var operationName = isFetch ? "fetch" : "destroy";
            if (operationCooldowns.TryGetValue(playerId, out DateTime lastOperationTime))
            {
                var timeSinceLastOperation = DateTime.UtcNow - lastOperationTime;
                if (timeSinceLastOperation.TotalSeconds < FetchDestroyCooldownSeconds)
                {
                    var timeLeft =
                        FetchDestroyCooldownSeconds - timeSinceLastOperation.TotalSeconds;
                    player.ChatMessage(
                        $"Slow down there buddy, please wait {timeLeft:F1} more seconds to {operationName} a {vehicleType}."
                    );
                    return;
                }
            }

            if (!storedData.Players.ContainsKey(playerId.ToString()))
            {
                player.ChatMessage("You do not own any vehicles.");
                return;
            }

            var playerVehicles = storedData.Players[playerId.ToString()].VehicleIDs;
            if (playerVehicles.Count == 0)
            {
                player.ChatMessage($"You do not have any {vehicleType}s to {operationName}.");
                return;
            }

            foreach (var vehicleId in Enumerable.Reverse(playerVehicles.ToList()))
            {
                BaseEntity vehicle =
                    BaseNetworkable.serverEntities.Find(new NetworkableId(vehicleId)) as BaseEntity;
                if (
                    vehicle != null
                    && (
                        (vehicleType == "car" && vehicle.ShortPrefabName.Contains("car"))
                        || (vehicleType == "heli" && vehicle.ShortPrefabName.Contains("minicopter"))
                    )
                )
                {
                    if (isFetch)
                    {
                        FetchVehicle(player, vehicle);
                    }
                    else
                    {
                        DestroyVehicle(player, vehicleId, vehicle);
                    }
                    operationCooldowns[playerId] = DateTime.UtcNow;
                    return;
                }
            }

            player.ChatMessage($"No {vehicleType} found to {operationName}.");
        }

        private void FetchVehicle(BasePlayer player, BaseEntity vehicle)
        {
            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f, SpawnPointLayerMask))
            {
                var entity = hit.GetEntity();
                if (entity != null && (entity is BuildingBlock || entity is Deployable))
                {
                    player.ChatMessage("You cannot fetch vehicle while looking at a base.");
                    return;
                }
            }
            Vector3 spawnPosition;
            if (TryGetSpawnPoint(player, out spawnPosition))
            {
                vehicle.transform.position = spawnPosition;
                Quaternion fetchRotation = Quaternion.Euler(
                    0,
                    player.transform.eulerAngles.y + -50,
                    0
                );
                vehicle.transform.rotation = fetchRotation;
                vehicle.SendNetworkUpdateImmediate();
                player.ChatMessage("Vehicle fetched to your location!");
            }
            else
            {
                player.ChatMessage(
                    "Could not find a suitable location to place your vehicle near you."
                );
            }
        }

        private void DestroyVehicle(BasePlayer player, ulong vehicleId, BaseEntity vehicle)
        {
            vehicle.Kill();
            storedData.Players[player.UserIDString].VehicleIDs.Remove(vehicleId);
            SaveData();
            player.ChatMessage("Vehicle has been successfully destroyed!");
        }

        [ChatCommand("vs")]
        private void CmdOpenVehicleSpawner(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionUse))
            {
                player.ChatMessage("You do not have permission to use this command.");
                return;
            }

            OpenVehicleSpawnerMenu(player);
        }

        private void OpenVehicleSpawnerMenu(BasePlayer player)
        {
            var container = new CuiElementContainer();
            container.Add(
                new CuiPanel
                {
                    CursorEnabled = true,
                    Image =
                    {
                        Color = "0.1843137 0.1803922 0.145098 1",
                        Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-152.295 -124.331",
                        OffsetMax = "152.295 124.331"
                    }
                },
                "Overlay",
                "Menu"
            );

            container.Add(
                new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1372549 0.1333333 0.1098039 1" },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-151.595 83.23",
                        OffsetMax = "152.995 124.33"
                    }
                },
                "Menu",
                "PanelBG"
            );

            container.Add(
                new CuiPanel
                {
                    CursorEnabled = false,
                    Image = { Color = "0.1372549 0.1333333 0.1098039 1" },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-152.295 -124.329",
                        OffsetMax = "152.295 -98.271"
                    }
                },
                "Menu",
                "BottomPanel"
            );

            container.Add(
                new CuiElement
                {
                    Name = "Title",
                    Parent = "Menu",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = "Vehicle Spawner",
                            Font = "permanentmarker.ttf",
                            FontSize = 16,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-110.023 79.266",
                            OffsetMax = "111.423 128.294"
                        }
                    }
                }
            );

            container.Add(
                new CuiElement
                {
                    Name = "HelicopterImage",
                    Parent = "Menu",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Sprite = "assets/icons/minicopter.png",
                            Png = ImageLibrary?.Call<string>("GetImage", "HelicopterImage")
                        },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "67.1 -1.896",
                            OffsetMax = "143.7 67.667"
                        }
                    }
                }
            );

            container.Add(
                new CuiElement
                {
                    Name = "CarImage",
                    Parent = "Menu",
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Color = "1 1 1 1",
                            Sprite = "assets/icons/modular-vehicle-3-removebg-preview.png",
                            Png = ImageLibrary?.Call<string>("GetImage", "CarImage")
                        },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-152.273 -14.113",
                            OffsetMax = "-56.296 79.885"
                        }
                    }
                }
            );

            container.Add(
                new CuiButton
                {
                    Button = { Color = "0.6981132 0.03622286 0.03622286 1", Command = "closemenu" },
                    Text =
                    {
                        Text = "✘",
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 15,
                        Align = TextAnchor.MiddleCenter,
                        Color = "1 1 1 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "111.423 102.959",
                        OffsetMax = "149.677 121.841"
                    }
                },
                "Menu",
                "CloseButton"
            );

            container.Add(
                new CuiButton
                {
                    Button = { Color = "0.1372549 0.1333333 0.1098039 1", Command = "spawncar" },
                    Text =
                    {
                        Text = "Spawn Car",
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Align = TextAnchor.MiddleCenter,
                        Color = "0.5377358 0.5349457 0.5098344 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-137.424 -27.92",
                        OffsetMax = "-71.135 -9.68"
                    }
                },
                "Menu",
                "Carspawer"
            );

            container.Add(
                new CuiButton
                {
                    Button = { Color = "0.1372549 0.1333333 0.1098039 1", Command = "fetchcar" },
                    Text =
                    {
                        Text = "Fetch Car",
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Align = TextAnchor.MiddleCenter,
                        Color = "0.5377358 0.5349457 0.5098344 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-33.145 -32.32",
                        OffsetMax = "33.144 -14.08"
                    }
                },
                "Carspawer",
                "CarFetch"
            );

            container.Add(
                new CuiButton
                {
                    Button = { Color = "0.1372549 0.1333333 0.1098039 1", Command = "destroycar" },
                    Text =
                    {
                        Text = "Destroy Car",
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Align = TextAnchor.MiddleCenter,
                        Color = "0.5377358 0.5349457 0.5098344 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-33.145 -54.82",
                        OffsetMax = "33.144 -36.58"
                    }
                },
                "Carspawer",
                "CarDestroy"
            );

            container.Add(
                new CuiButton
                {
                    Button = { Color = "0.1372549 0.1333333 0.1098039 1", Command = "spawnheli" },
                    Text =
                    {
                        Text = "Spawn Mini",
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Align = TextAnchor.MiddleCenter,
                        Color = "0.5377358 0.5349457 0.5098344 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "72.253 -27.919",
                        OffsetMax = "138.547 -9.68"
                    }
                },
                "Menu",
                "Helispawner"
            );

            container.Add(
                new CuiButton
                {
                    Button = { Color = "0.1372549 0.1333333 0.1098039 1", Command = "fetchheli" },
                    Text =
                    {
                        Text = "Fetch Mini",
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Align = TextAnchor.MiddleCenter,
                        Color = "0.5377358 0.5349457 0.5098344 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-33.147 -32.319",
                        OffsetMax = "33.147 -14.08"
                    }
                },
                "Helispawner",
                "HeliFetch"
            );

            container.Add(
                new CuiButton
                {
                    Button = { Color = "0.1372549 0.1333333 0.1098039 1", Command = "destroyheli" },
                    Text =
                    {
                        Text = "Destroy Mini",
                        Font = "robotocondensed-regular.ttf",
                        FontSize = 10,
                        Align = TextAnchor.MiddleCenter,
                        Color = "0.5377358 0.5349457 0.5098344 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.5 0.5",
                        AnchorMax = "0.5 0.5",
                        OffsetMin = "-33.147 -54.819",
                        OffsetMax = "33.147 -36.58"
                    }
                },
                "Helispawner",
                "HeliDestroy"
            );

            container.Add(
                new CuiElement
                {
                    Name = "Watermark",
                    Parent = "Menu",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Text = "By Ghosty",
                            Font = "permanentmarker.ttf",
                            FontSize = 9,
                            Align = TextAnchor.MiddleCenter,
                            Color = "1 1 1 1"
                        },
                        new CuiOutlineComponent { Color = "0 0 0 0.5", Distance = "1 -1" },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0.5 0.5",
                            AnchorMax = "0.5 0.5",
                            OffsetMin = "-49.3 -121.655",
                            OffsetMax = "50.7 -100.945"
                        }
                    }
                }
            );

            CuiHelper.DestroyUi(player, "Menu");
            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("spawnheli")]
        private void CmdSpawnHeli(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;
            UpdatePlayerVehicleLimit(player);
            if (!permission.UserHasPermission(player.UserIDString, PermissionMinicopter))
            {
                player.ChatMessage("You do not have permission to spawn a minicopter.");
                return;
            }
            if (!CanSpawnVehicle(player, "heli"))
            {
                return;
            }
            if (!CanSpawnAnotherVehicle(player))
                return;

            if (TryGetSpawnPoint(player, out Vector3 spawnPosition))
            {
                string minicopterPrefab =
                    "assets/content/vehicles/minicopter/minicopter.entity.prefab";
                Quaternion spawnRotation = Quaternion.Euler(0, player.transform.eulerAngles.y, 0);

                SpawnVehicle(minicopterPrefab, spawnPosition, spawnRotation, player, "Minicopter");
                CuiHelper.DestroyUi(player, "Menu");
            }
            else
            {
                player.ChatMessage(
                    "No suitable location found in front of you to spawn the minicopter."
                );
            }
        }

        [ConsoleCommand("spawncar")]
        private void CmdSpawnCar(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;
            UpdatePlayerVehicleLimit(player);
            if (!permission.UserHasPermission(player.UserIDString, Permissioncar))
            {
                player.ChatMessage("You do not have permission to spawn a car.");
                return;
            }
            if (!CanSpawnVehicle(player, "car"))
            {
                return;
            }
            if (!CanSpawnAnotherVehicle(player))
                return;

            if (TryGetSpawnPoint(player, out Vector3 spawnPosition))
            {
                string carPrefab = carPrefabs[UnityEngine.Random.Range(0, carPrefabs.Length)];
                Quaternion spawnRotation = Quaternion.Euler(0, player.transform.eulerAngles.y, 0);

                SpawnVehicle(carPrefab, spawnPosition, spawnRotation, player, "Car");
                CuiHelper.DestroyUi(player, "Menu");
            }
            else
            {
                player.ChatMessage("No suitable location found in front of you to spawn the car.");
            }
        }

        [ConsoleCommand("fetchcar")]
        private void ConsoleCmd_FetchCar(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;
            PerformVehicleOperation(player, true, "car");
            CuiHelper.DestroyUi(player, "Menu");
        }

        [ConsoleCommand("fetchheli")]
        private void ConsoleCmd_FetchHeli(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;
            PerformVehicleOperation(player, true, "heli");
            CuiHelper.DestroyUi(player, "Menu");
        }

        [ConsoleCommand("destroycar")]
        private void ConsoleCmd_DestroyCar(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;
            PerformVehicleOperation(player, false, "car");
        }

        [ConsoleCommand("destroyheli")]
        private void ConsoleCmd_DestroyHeli(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;
            PerformVehicleOperation(player, false, "heli");
        }

        [ConsoleCommand("closemenu")]
        private void CmdCloseMenu(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            CuiHelper.DestroyUi(player, "Menu");
        }
    }
}
