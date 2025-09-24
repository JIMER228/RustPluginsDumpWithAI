using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("EntCleanup", "herbs.acab", "1.8.0")]
    [Description("Cleans up old entities that aren't upkept to decrease server load and increase FPS.")]

    public class EntCleanup : RustPlugin
    {
        private const float DefaultCheckInterval = 3600f; // Check every hour by default
        private const float DefaultMaxEntityAge = 86400f; // 24 hours in seconds by default

        private float checkInterval;
        private float maxEntityAge;

        private List<string> targetEntities;
        private List<string> whitelistEntities;
        private Dictionary<BaseEntity, float> entityCreationTimes = new Dictionary<BaseEntity, float>();

        private void OnServerInitialized()
        {
            LoadConfigVariables();
            timer.Every(checkInterval, () => CleanupEntities());
        }

        private void LoadConfigVariables()
        {
            checkInterval = Convert.ToSingle(Config["CheckInterval"]);
            maxEntityAge = Convert.ToSingle(Config["MaxEntityAge"]);
            targetEntities = Config["TargetEntities"] as List<string> ?? GetDefaultTargetEntities();
            whitelistEntities = Config["WhitelistEntities"] as List<string> ?? new List<string>();
        }

        protected override void LoadDefaultConfig()
        {
            Config["CheckInterval"] = DefaultCheckInterval;
            Config["MaxEntityAge"] = DefaultMaxEntityAge;
            Config["TargetEntities"] = GetDefaultTargetEntities();
            Config["WhitelistEntities"] = new List<string>();
            SaveConfig();
        }

        private List<string> GetDefaultTargetEntities()
        {
            return new List<string>
            {
                "assets/prefabs/building/wall.external.high.stone/wall.external.high.stone.prefab",
                "assets/prefabs/building/wall.external.high.wood/wall.external.high.wood.prefab",
                "assets/prefabs/building/door.hinged.wood/door.hinged.wood.prefab",
                "assets/prefabs/deployable/barricade/barricade.wood.prefab",
                "assets/prefabs/deployable/barricade/barricade.wood.cover.prefab",
                "assets/prefabs/deployable/planter/planter.large.prefab",
                "assets/prefabs/deployable/planter/planter.small.prefab",
                "assets/prefabs/deployable/signs/sign.small.wood.prefab",
                "assets/prefabs/deployable/signs/sign.medium.wood.prefab",
                "assets/prefabs/deployable/signs/sign.large.wood.prefab",
                "assets/prefabs/deployable/signs/sign.huge.wood.prefab",
                "assets/prefabs/deployable/chair/chair.comfy.prefab",
                "assets/prefabs/deployable/table/table.small.prefab",
                "assets/prefabs/deployable/table/table.large.prefab",
                "assets/prefabs/deployable/barricade/barricade.stone.prefab",
                "assets/prefabs/deployable/water catcher/water_catcher_small.prefab",
                "assets/prefabs/deployable/water catcher/water_catcher_large.prefab",
                "assets/prefabs/deployable/water barrel/water_barrel.prefab"
            };
        }

        private void OnEntitySpawned(BaseEntity entity)
        {
            if (targetEntities.Contains(entity.PrefabName))
            {
                entityCreationTimes[entity] = UnityEngine.Time.realtimeSinceStartup;
            }
        }

        [ChatCommand("cleanup")]
        private void CleanupCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "entcleanup.admin"))
            {
                SendReply(player, "You don't have permission to use this command.");
                return;
            }

            CleanupEntities();
            SendReply(player, "Forced entity cleanup executed.");
        }

        private void CleanupEntities()
        {
            BroadcastChat("Cleaning up entities, please expect slight lag...");
            int removedEntities = 0;
            float currentTime = UnityEngine.Time.realtimeSinceStartup;

            foreach (var entity in entityCreationTimes.Keys)
            {
                if (entity != null && !entity.IsDestroyed && !whitelistEntities.Contains(entity.PrefabName))
                {
                    if (currentTime - entityCreationTimes[entity] > maxEntityAge && !IsInBuildingPrivilege(entity))
                    {
                        entity.Kill();
                        removedEntities++;
                    }
                }
            }

            Puts($"EntCleanup: Removed {removedEntities} old entities.");
            BroadcastChat("Cleanup finished!");
        }

        private bool IsInBuildingPrivilege(BaseEntity entity)
        {
            BuildingPrivlidge privilege = entity.GetBuildingPrivilege();
            return privilege != null;
        }

        private void BroadcastChat(string message)
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                SendReply(player, message);
            }
        }

        private void Init()
        {
            permission.RegisterPermission("entcleanup.admin", this);
        }
    }
}
