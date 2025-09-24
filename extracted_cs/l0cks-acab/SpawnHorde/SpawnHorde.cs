using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SpawnHorde", "locks", "1.1.2")]
    [Description("Allows admins to spawn a horde of specified animals that chase a specific or random player on the map with custom size and health")]

    public class SpawnHorde : RustPlugin
    {
        private List<BaseEntity> spawnedEntities = new List<BaseEntity>();

        [ChatCommand("spawnhorde")]
        void SpawnHordeCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                SendReply(player, "You don't have permission to use this command.");
                return;
            }

            if (args.Length < 4)
            {
                SendReply(player, "Usage: /spawnhorde <player> <horde_size> <horde_health> <animal:bear,chicken,wolf,boar,stag>");
                return;
            }

            BasePlayer target = FindPlayer(args[0]);
            if (target == null)
            {
                SendReply(player, $"Player '{args[0]}' not found.");
                return;
            }

            int hordeSize;
            if (!int.TryParse(args[1], out hordeSize) || hordeSize <= 0)
            {
                SendReply(player, "Invalid horde size. It must be a positive integer.");
                return;
            }

            float hordeHealth;
            if (!float.TryParse(args[2], out hordeHealth) || hordeHealth <= 0)
            {
                SendReply(player, "Invalid horde health. It must be a positive number.");
                return;
            }

            string animalType = args[3].ToLower();
            List<string> validAnimals = new List<string> { "bear", "chicken", "wolf", "boar", "stag" };
            if (!validAnimals.Contains(animalType))
            {
                SendReply(player, "Invalid animal type. Valid types are: bear, chicken, wolf, boar, stag.");
                return;
            }

            for (int i = 0; i < hordeSize; i++)
            {
                Vector3 spawnPosition = target.transform.position + new Vector3(UnityEngine.Random.Range(-10, 10), 0, UnityEngine.Random.Range(-10, 10));
                BaseEntity entity = GameManager.server.CreateEntity($"assets/rust.ai/agents/{animalType}/{animalType}.prefab", spawnPosition);

                if (entity != null)
                {
                    entity.Spawn();
                    spawnedEntities.Add(entity);

                    BaseNpc npc = entity as BaseNpc;
                    if (npc != null)
                    {
                        npc.InitializeHealth(hordeHealth, hordeHealth);
                        npc.health = hordeHealth;
                        npc.Attack(target);
                    }
                }
            }

            SendReply(player, $"A <color=red>HORDE</color> of {animalType}s has spawned on {target.displayName}!");
        }

        [ChatCommand("killhorde")]
        void KillHordeCommand(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin)
            {
                SendReply(player, "You don't have permission to use this command.");
                return;
            }

            int count = 0;
            foreach (var entity in spawnedEntities)
            {
                if (entity != null && !entity.IsDestroyed)
                {
                    entity.Kill();
                    count++;
                }
            }
            spawnedEntities.Clear();
            SendReply(player, $"Killed {count} spawned entities.");
        }

        BasePlayer GetRandomPlayer()
        {
            List<BasePlayer> players = new List<BasePlayer>(BasePlayer.activePlayerList);
            if (players.Count == 0) return null;
            return players[UnityEngine.Random.Range(0, players.Count)];
        }

        BasePlayer FindPlayer(string identifier)
        {
            ulong steamId;
            if (ulong.TryParse(identifier, out steamId))
            {
                return BasePlayer.FindAwakeOrSleeping(steamId.ToString());
            }
            else
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    if (player.displayName.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                    {
                        return player;
                    }
                }
            }
            return null;
        }

        void OnServerSave()
        {
            spawnedEntities.RemoveAll(entity => entity == null || entity.IsDestroyed);
        }

        void Unload()
        {
            foreach (var entity in spawnedEntities)
            {
                entity?.Kill();
            }
            spawnedEntities.Clear();
        }
    }
}
