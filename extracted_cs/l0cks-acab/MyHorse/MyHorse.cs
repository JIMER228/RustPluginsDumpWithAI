using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("MyHorse", "locks", "1.1.0")]
    [Description("Allows players with permission to spawn, retrieve, and remove a custom horse")]
    public class MyHorse : RustPlugin
    {
        private const string PermissionName = "myhorse.use";
        private const string PermissionOverride = "myhorse.override";
        private Dictionary<ulong, float> lastHorseSpawnTime;

        private class PluginConfig
        {
            public float CooldownTime { get; set; } = 300f; // Default 5 minutes
            public string NoPermissionMessage { get; set; } = "You do not have permission to use this command.";
            public string BuildingBlockedMessage { get; set; } = "You cannot use this command while building blocked.";
            public string CooldownMessage { get; set; } = "You must wait {0} seconds before using this command again.";
            public string HorseExistsMessage { get; set; } = "You already have a horse. Use /gethorse to retrieve it or /removehorse to remove it.";
            public string HorseCreatedMessage { get; set; } = "Your horse has arrived!";
            public string NoHorseToRetrieveMessage { get; set; } = "You do not have a horse to retrieve.";
            public string MountedMessage { get; set; } = "You cannot use this command while mounted.";
            public string OtherPlayerMountedMessage { get; set; } = "Someone is mounted on your horse. You cannot retrieve it right now.";
            public string HorseMovedMessage { get; set; } = "Your horse has been moved in front of you.";
            public string HorseRemovedMessage { get; set; } = "Your horse has been removed.";
        }

        private PluginConfig config;

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        void Init()
        {
            permission.RegisterPermission(PermissionName, this);
            permission.RegisterPermission(PermissionOverride, this);
            lastHorseSpawnTime = new Dictionary<ulong, float>();
        }

        [ChatCommand("myhorse")]
        private void MyHorseCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionName))
            {
                player.ChatMessage(config.NoPermissionMessage);
                return;
            }

            if (player.IsBuildingBlocked())
            {
                player.ChatMessage(config.BuildingBlockedMessage);
                return;
            }

            if (!permission.UserHasPermission(player.UserIDString, PermissionOverride))
            {
                if (lastHorseSpawnTime.TryGetValue(player.userID, out float lastUsage) && Time.realtimeSinceStartup - lastUsage < config.CooldownTime)
                {
                    float remainingTime = config.CooldownTime - (Time.realtimeSinceStartup - lastUsage);
                    player.ChatMessage(string.Format(config.CooldownMessage, Mathf.CeilToInt(remainingTime)));
                    return;
                }
            }

            var playerHorse = player.GetComponent<PlayerHorse>() ?? player.gameObject.AddComponent<PlayerHorse>();

            if (playerHorse.OwnedHorse != null)
            {
                player.ChatMessage(config.HorseExistsMessage);
                return;
            }

            SpawnCustomHorse(player);
            lastHorseSpawnTime[player.userID] = Time.realtimeSinceStartup;
        }

        [ChatCommand("gethorse")]
        private void GetHorseCommand(BasePlayer player, string command, string[] args)
        {
            if (player.IsBuildingBlocked())
            {
                player.ChatMessage(config.BuildingBlockedMessage);
                return;
            }

            var playerHorse = player.GetComponent<PlayerHorse>();
            if (playerHorse == null || playerHorse.OwnedHorse == null)
            {
                player.ChatMessage(config.NoHorseToRetrieveMessage);
                return;
            }

            if (player.isMounted)
            {
                player.ChatMessage(config.MountedMessage);
                return;
            }

            var mountable = playerHorse.OwnedHorse.GetComponent<BaseMountable>();
            if (mountable != null && mountable.AnyMounted())
            {
                player.ChatMessage(config.OtherPlayerMountedMessage);
                return;
            }

            MoveHorseToPlayer(player, playerHorse.OwnedHorse);
        }

        [ChatCommand("removehorse")]
        private void RemoveHorseCommand(BasePlayer player, string command, string[] args)
        {
            var playerHorse = player.GetComponent<PlayerHorse>();
            if (playerHorse == null || playerHorse.OwnedHorse == null)
            {
                player.ChatMessage(config.NoHorseToRetrieveMessage);
                return;
            }

            KillHorse(playerHorse.OwnedHorse);
            playerHorse.OwnedHorse = null;
            player.ChatMessage(config.HorseRemovedMessage);
        }

        private void SpawnCustomHorse(BasePlayer player)
        {
            // Calculate spawn position directly in front of the player
            Vector3 spawnPosition = player.transform.position + (player.transform.forward * 3);
            Quaternion spawnRotation = Quaternion.Euler(0, player.transform.rotation.eulerAngles.y, 0);

            // Use the correct horse prefab path
            string horsePrefab = "assets/rust.ai/nextai/testridablehorse.prefab";

            // Spawn the horse
            BaseEntity horseEntity = GameManager.server.CreateEntity(horsePrefab, spawnPosition, spawnRotation);

            if (horseEntity == null)
            {
                player.ChatMessage("Failed to create horse entity.");
                return;
            }

            horseEntity.Spawn();

            // Set the horse to max health
            var combatEntity = horseEntity as BaseCombatEntity;
            if (combatEntity != null)
            {
                combatEntity.health = combatEntity.MaxHealth();
                combatEntity.SendNetworkUpdate();
            }

            // Assign the horse to the player
            var playerHorse = player.GetComponent<PlayerHorse>() ?? player.gameObject.AddComponent<PlayerHorse>();
            playerHorse.OwnedHorse = horseEntity;

            player.ChatMessage(config.HorseCreatedMessage);
        }

        private void MoveHorseToPlayer(BasePlayer player, BaseEntity horse)
        {
            if (horse == null) return;

            // Calculate spawn position directly in front of the player
            Vector3 newPosition = player.transform.position + (player.transform.forward * 3);
            Quaternion newRotation = Quaternion.Euler(0, player.transform.rotation.eulerAngles.y, 0);

            horse.transform.position = newPosition;
            horse.transform.rotation = newRotation;
            horse.SendNetworkUpdate();

            player.ChatMessage(config.HorseMovedMessage);
        }

        private void KillHorse(BaseEntity horse)
        {
            if (horse == null) return;

            horse.Kill();
        }

        public class PlayerHorse : MonoBehaviour
        {
            public BaseEntity OwnedHorse { get; set; }
        }
    }
}

