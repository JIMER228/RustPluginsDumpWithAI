// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Glow Garage Door", "st-little", "0.1.1")]
    [Description("Glow Garage Door is a server-side plugin for Rust that adds a glowing effect to garage doors by embedding a light inside the door thickness.")]
    public class GlowGarageDoor : RustPlugin
    {
        // Permissions
        private const string PermissionAdmin = "glowgaragedoor.admin";
        private const string PermissionUser = "glowgaragedoor.user";

        // Data file name
        private const string DataFileName = "GlowGarageDoor";

        // Prefabs
        private const string GarageDoorPrefab = "assets/prefabs/building/wall.frame.garagedoor/wall.frame.garagedoor.prefab";
        private const string EmbeddedLightPrefab = "assets/prefabs/misc/permstore/industriallight/industrial.wall.lamp.deployed.prefab";

        // Transform (local to door)
        // Front: slightly forward, shines downward
        private static readonly Vector3 LightFrontLocalPosition = new Vector3(-0.04f, 2.99f, 0.00f);
        private static readonly Vector3 LightFrontLocalEuler = new Vector3(90f, 90f, 0f);
        // Back: slightly backward, shines downward (invert Y)
        private static readonly Vector3 LightBackLocalPosition = new Vector3(-0.14f, 2.99f, 0.00f);
        private static readonly Vector3 LightBackLocalEuler = new Vector3(90f, 90f, 0f);

        // Track attached lamps per door (doorNetId -> {front, back})
        private sealed class AttachedLamps { public ulong Front; public ulong Back; }
        private readonly Dictionary<ulong, AttachedLamps> _attachedLampByDoor = new Dictionary<ulong, AttachedLamps>();
        // Guard set to avoid recursive Kill() loops across hooks
        private readonly HashSet<ulong> _selfKilling = new HashSet<ulong>();

        // Persisted data (position keys of doors that should have lamps)
        private class StoredData
        {
            public HashSet<string> Doors = new HashSet<string>();
        }
        private StoredData _data = new StoredData();

        #region Configuration

        private Configuration _configuration = new Configuration { CommandName = "glow" };

        private class Configuration
        {
            [JsonProperty(PropertyName = "Command Name")]
            public string CommandName = "glow";
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                CommandName = "glow"
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

        #region Localization

        protected override void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You don't have permission to use this command.",
                ["NoBuildPrivilege"] = "You must have building privilege to use this.",
                ["OnlyGarageDoor"] = "This command can only be used on a garage door.",
                ["GlowOn"] = "Glow: ON",
                ["GlowOff"] = "Glow: OFF",
                ["LightCreateFailed"] = "Failed to create the light."
            }, this);

            // Japanese
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "このコマンドを使用する権限がありません。",
                ["NoBuildPrivilege"] = "建築が許可されていません。",
                ["OnlyGarageDoor"] = "このコマンドはガレージドアにのみ使用できます。",
                ["GlowOn"] = "Glow: ON",
                ["GlowOff"] = "Glow: OFF",
                ["LightCreateFailed"] = "ライトの生成に失敗しました。"
            }, this, "ja");
        }

        #endregion

        #region Commands

        private void GlowGarageCommand(BasePlayer player, string command, string[] args)
        {
            // Check permissions
            if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin) &&
                !permission.UserHasPermission(player.UserIDString, PermissionUser))
            {
                Reply(player, "NoPermission");
                return;
            }

            // Check if the player has permission to build.
            var buildingPrivilege = player.GetBuildingPrivilege();
            if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin) && buildingPrivilege == null)
            {
                Reply(player, "NoBuildPrivilege");
                return;
            }

            var hit = GetPlayerEyesHeadRay(player);
            if (hit == null)
            {
                Reply(player, "OnlyGarageDoor");
                return;
            }

            var entity = hit.Value.GetEntity() as BaseEntity;
            if (entity == null)
            {
                Reply(player, "OnlyGarageDoor");
                return;
            }

            var door = entity as Door;
            if (door == null || !string.Equals(door.PrefabName, GarageDoorPrefab, StringComparison.Ordinal))
            {
                Reply(player, "OnlyGarageDoor");
                return;
            }

            ToggleEmbeddedLamp(player, door);
        }

        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(PermissionAdmin, this);
            permission.RegisterPermission(PermissionUser, this);

            cmd.AddChatCommand(_configuration.CommandName, this, nameof(GlowGarageCommand));

            LoadData();
        }

        private void OnServerInitialized()
        {
            // After restart, re-attach to existing doors
            NextTick(RestoreAllDoorsFromData);
        }

        private void Unload()
        {
            // Clean all attached lamps
            foreach (var kv in _attachedLampByDoor)
            {
                var front = BaseNetworkable.serverEntities?.Find(new NetworkableId(kv.Value.Front)) as BaseEntity;
                var back = BaseNetworkable.serverEntities?.Find(new NetworkableId(kv.Value.Back)) as BaseEntity;
                if (front != null && !front.IsDestroyed)
                {
                    _selfKilling.Add(kv.Value.Front);
                    front.Kill();
                }
                if (back != null && !back.IsDestroyed)
                {
                    _selfKilling.Add(kv.Value.Back);
                    back.Kill();
                }
            }
            _attachedLampByDoor.Clear();

            // Save persisted data on unload
            SaveData();
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            // If door destroyed, remove lamps (avoid recursion)
            if (entity is Door door)
            {
                var doorId = GetNetworkId(door);
                if (doorId != 0UL && _attachedLampByDoor.TryGetValue(doorId, out var lamps))
                {
                    // Remove mapping first to prevent lamp hook from using it
                    _attachedLampByDoor.Remove(doorId);

                    // Kill lamps with guard marks
                    var front = BaseNetworkable.serverEntities?.Find(new NetworkableId(lamps.Front)) as BaseEntity;
                    var back = BaseNetworkable.serverEntities?.Find(new NetworkableId(lamps.Back)) as BaseEntity;
                    if (front != null && !front.IsDestroyed)
                    {
                        _selfKilling.Add(lamps.Front);
                        front.Kill();
                    }
                    if (back != null && !back.IsDestroyed)
                    {
                        _selfKilling.Add(lamps.Back);
                        back.Kill();
                    }
                }

                // Also remove from persisted data
                var key = MakeKey(door.transform.position);
                if (_data.Doors.Remove(key)) SaveData();
                return;
            }

            // If lamp destroyed, drop mapping
            if (entity is SimpleLight simple)
            {
                var lampId = simple.net?.ID.Value ?? 0UL;
                if (lampId != 0UL)
                {
                    // If this Kill was initiated by us, swallow and exit
                    if (_selfKilling.Remove(lampId))
                    {
                        return;
                    }
                    ulong? targetDoor = null;
                    foreach (var kv in _attachedLampByDoor)
                    {
                        if (kv.Value.Front == lampId || kv.Value.Back == lampId)
                        {
                            targetDoor = kv.Key;
                            break;
                        }
                    }
                    if (targetDoor.HasValue)
                    {
                        var lamps = _attachedLampByDoor[targetDoor.Value];
                        var otherId = (lamps.Front == lampId) ? lamps.Back : lamps.Front;
                        // Remove mapping before killing the other to avoid re-entrancy using mapping
                        _attachedLampByDoor.Remove(targetDoor.Value);
                        var other = BaseNetworkable.serverEntities?.Find(new NetworkableId(otherId)) as BaseEntity;
                        if (other != null && !other.IsDestroyed)
                        {
                            _selfKilling.Add(otherId);
                            other.Kill();
                        }
                    }
                }
            }
        }
        #endregion

        #region Impl
        private void OnEntitySpawned(BaseNetworkable entity)
        {
            var door = entity as Door;
            if (door == null) return;
            if (!string.Equals(door.PrefabName, GarageDoorPrefab, StringComparison.Ordinal)) return;

            var key = MakeKey(door.transform.position);
            if (_data.Doors.Contains(key))
            {
                // If saved as ON, auto re-attach
                EnsureLampsForDoor(door);
            }
        }

        private void ToggleEmbeddedLamp(BasePlayer player, Door door)
        {
            var doorId = GetNetworkId(door);
            if (doorId == 0UL) return;

            if (_attachedLampByDoor.TryGetValue(doorId, out var lamps))
            {
                var front = BaseNetworkable.serverEntities?.Find(new NetworkableId(lamps.Front)) as BaseEntity;
                var back = BaseNetworkable.serverEntities?.Find(new NetworkableId(lamps.Back)) as BaseEntity;
                if (front != null && !front.IsDestroyed)
                {
                    _selfKilling.Add(lamps.Front);
                    front.Kill();
                }
                if (back != null && !back.IsDestroyed)
                {
                    _selfKilling.Add(lamps.Back);
                    back.Kill();
                }
                _attachedLampByDoor.Remove(doorId);
                // Remove from persisted data
                var offKey = MakeKey(door.transform.position);
                if (_data.Doors.Remove(offKey)) SaveData();
                Reply(player, "GlowOff");
                return;
            }

            if (!EnsureLampsForDoor(door))
            {
                Reply(player, "LightCreateFailed");
                return;
            }
            // Add to persisted data
            var onKey = MakeKey(door.transform.position);
            if (_data.Doors.Add(onKey)) SaveData();
            Reply(player, "GlowOn");
        }

        // Creation logic (two lights: front/back)
        private bool EnsureLampsForDoor(Door door)
        {
            try
            {
                RemoveExistingEmbeddedLampUnder(door);
                var frontCreated = CreateEmbeddedLamp(door, isBack: false);
                var backCreated = CreateEmbeddedLamp(door, isBack: true);
                if (frontCreated == null || backCreated == null) return false;
                var doorId = GetNetworkId(door);
                if (doorId == 0UL) return true; // Lamps created; treat as success
                _attachedLampByDoor[doorId] = new AttachedLamps
                {
                    Front = frontCreated.net.ID.Value,
                    Back = backCreated.net.ID.Value
                };
                return true;
            }
            catch (Exception e)
            {
                PrintError($"EnsureLampsForDoor failed: {e}");
                return false;
            }
        }

        private SimpleLight? CreateEmbeddedLamp(Door door, bool isBack)
        {
            var lamp = GameManager.server.CreateEntity(EmbeddedLightPrefab, door.transform.position) as SimpleLight;
            if (lamp == null) return null;

            lamp.SetFlag(BaseEntity.Flags.Reserved8, true); // turn ON
            lamp.SetFlag(BaseEntity.Flags.On, true);        // explicit ON
            lamp.pickup.enabled = false; // no pickup
            lamp.SetParent(door);
            lamp.transform.localPosition = isBack ? LightBackLocalPosition : LightFrontLocalPosition;
            lamp.transform.localEulerAngles = isBack ? LightBackLocalEuler : LightFrontLocalEuler;
            RemoveColliderProtection(lamp);
            lamp.Spawn();
            lamp.SendNetworkUpdateImmediate(true);
            return lamp;
        }

        // Data helpers
        private void LoadData()
        {
            try
            {
                _data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(DataFileName) ?? new StoredData();
            }
            catch (Exception)
            {
                _data = new StoredData();
            }
        }

        private void SaveData()
        {
            try
            {
                Interface.Oxide.DataFileSystem.WriteObject(DataFileName, _data);
            }
            catch (Exception e)
            {
                PrintError($"Failed to save data: {e}");
            }
        }

        private static string MakeKey(Vector3 worldPos)
        {
            // Quantize to generate a key (0.1 m units)
            float qx = Mathf.Round(worldPos.x * 10f) / 10f;
            float qy = Mathf.Round(worldPos.y * 10f) / 10f;
            float qz = Mathf.Round(worldPos.z * 10f) / 10f;
            return $"{qx:F1}|{qy:F1}|{qz:F1}";
        }

        private void RestoreAllDoorsFromData()
        {
            try
            {
                var doors = UnityEngine.Object.FindObjectsOfType<Door>();
                foreach (var door in doors)
                {
                    if (!string.Equals(door.PrefabName, GarageDoorPrefab, StringComparison.Ordinal)) continue;
                    var key = MakeKey(door.transform.position);
                    if (_data.Doors.Contains(key))
                    {
                        EnsureLampsForDoor(door);
                    }
                }
            }
            catch (Exception e)
            {
                PrintWarning($"RestoreAllDoorsFromData failed: {e.Message}");
            }
        }

        private static void RemoveColliderProtection(BaseEntity colliderEntity)
        {
            foreach (var meshCollider in colliderEntity.GetComponentsInChildren<MeshCollider>())
                UnityEngine.Object.DestroyImmediate(meshCollider);
            var gw = colliderEntity.GetComponent<GroundWatch>();
            if (gw != null) UnityEngine.Object.DestroyImmediate(gw);
        }

        private static void RemoveExistingEmbeddedLampUnder(Door door)
        {
            var nearby = door.GetComponentsInChildren<SimpleLight>(true);
            foreach (var sl in nearby)
            {
                if (sl == null || sl.IsDestroyed) continue;
                var parent = sl.GetParentEntity();
                if (parent != null && parent.net?.ID == door.net?.ID)
                {
                    var local = door.transform.InverseTransformPoint(sl.transform.position);
                    if (Vector3.Distance(local, LightFrontLocalPosition) <= 0.10f || Vector3.Distance(local, LightBackLocalPosition) <= 0.10f)
                    {
                        sl.Kill();
                    }
                }
            }
        }

        private static RaycastHit? GetPlayerEyesHeadRay(BasePlayer basePlayer, float maxDistance = Mathf.Infinity)
        {
            return Physics.Raycast(basePlayer.eyes.HeadRay(), out var hit, maxDistance, Physics.DefaultRaycastLayers)
                ? hit
                : null;
        }

        private static ulong GetNetworkId(BaseNetworkable ent)
        {
            try { return ent?.net?.ID.Value ?? 0UL; }
            catch { return 0UL; }
        }

        // Localization helper
        private void Reply(BasePlayer player, string key)
        {
            var msg = lang.GetMessage(key, this, player?.UserIDString);
            if (string.IsNullOrEmpty(msg)) msg = key;
            SendReply(player, msg);
        }
        #endregion
    }
}
