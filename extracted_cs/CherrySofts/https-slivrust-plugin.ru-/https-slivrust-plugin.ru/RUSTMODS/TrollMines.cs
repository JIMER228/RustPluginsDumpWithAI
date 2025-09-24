using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("TrollMines", "Amphetaminov", "1.0.0")]
    [Description("Плагин для троллинга нарушителей при помощи минного поле вокруг игрока")]
    public class TrollMines : RustPlugin
    {
        private Configuration config;
        
        private class Configuration
        {
            public float MineSpawnRadius = 5f;
            public float MineDespawnRadius = 7f;
            public float MineExplosionRadius = 0.5f;
            public float SpawnInterval = 0.2f;
            public int MaxMinesPerPlayer = 30;
            public int MinesPerSpawn = 5;
            public float MinDistanceBetweenMines = 1.5f;
        }

        private class CheatData
        {
            public Timer timer;
            public List<BaseEntity> mines = new List<BaseEntity>();
            public BasePlayer target;
            public BasePlayer activator;
        }

        private Dictionary<ulong, CheatData> activeCheaters = new Dictionary<ulong, CheatData>();

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        void Init()
        {
            permission.RegisterPermission("trollmines.use", this);
            permission.RegisterPermission("trollmines.admin", this);
            cmd.AddConsoleCommand("trollmines", this, nameof(ConsoleTrollMines));
        }

        private void ConsoleTrollMines(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            if (!permission.UserHasPermission(player.UserIDString, "trollmines.use"))
            {
                arg.ReplyWith($"[TrollMines] У вас нет прав на использование команды!");
                return;
            }

            if (arg.Args == null || arg.Args.Length == 0)
            {
                arg.ReplyWith($"[TrollMines] Использование: trollmines <ник/steamid>");
                return;
            }

            string searchValue = arg.Args[0];
            var targetPlayer = FindPlayer(searchValue);
            
            if (targetPlayer == null)
            {
                arg.ReplyWith($"[TrollMines] Игрок '{searchValue}' не найден!");
                return;
            }

            ulong uid = targetPlayer.userID;
            if (activeCheaters.ContainsKey(uid))
            {
                DeactivateMines(targetPlayer);
                arg.ReplyWith($"[TrollMines] Мины выключены для {targetPlayer.displayName} ({targetPlayer.UserIDString})");
            }
            else
            {
                ActivateMines(targetPlayer, player);
                arg.ReplyWith($"[TrollMines] Мины включены для {targetPlayer.displayName} ({targetPlayer.UserIDString})");
            }
        }

        private BasePlayer FindPlayer(string nameOrId)
        {
            // Сначала пробуем найти по Steam ID
            if (ulong.TryParse(nameOrId, out ulong steamId))
            {
                var player = BasePlayer.FindByID(steamId);
                if (player != null && player.IsConnected)
                    return player;
                    
                player = BasePlayer.FindSleeping(steamId);
                if (player != null)
                    return player;
            }

            // Затем ищем по имени (полное или частичное совпадение)
            var players = BasePlayer.activePlayerList.Where(p => 
                p.displayName.Contains(nameOrId, StringComparison.OrdinalIgnoreCase) ||
                p.UserIDString.Contains(nameOrId)).ToList();

            // Если найден только один игрок - возвращаем его
            if (players.Count == 1)
                return players[0];

            // Если найдено несколько - ищем точное совпадение
            var exactMatch = players.FirstOrDefault(p => 
                p.displayName.Equals(nameOrId, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
                return exactMatch;

            // Если несколько совпадений и нет точного - возвращаем null
            return null;
        }

        private void ActivateMines(BasePlayer target, BasePlayer activator)
        {
            CheatData data = new CheatData
            {
                target = target,
                activator = activator
            };
            
            activeCheaters[target.userID] = data;
            data.timer = timer.Every(config.SpawnInterval, () => ProcessCheatMines(target));
        }

        private void DeactivateMines(BasePlayer player)
        {
            if (!activeCheaters.ContainsKey(player.userID)) return;

            CheatData data = activeCheaters[player.userID];
            data.timer?.Destroy();
            
            foreach (BaseEntity mine in data.mines)
            {
                if (mine != null && !mine.IsDestroyed)
                {
                    mine.Kill();
                }
            }
            
            activeCheaters.Remove(player.userID);
        }

        private void ProcessCheatMines(BasePlayer player)
        {
            CheatData data = activeCheaters[player.userID];
            if (data == null) return;

            Vector3 playerPos = player.transform.position;
            List<BaseEntity> remaining = new List<BaseEntity>();

            foreach (BaseEntity mine in data.mines)
            {
                if (mine == null || mine.IsDestroyed) continue;

                float dist = Vector3.Distance(mine.transform.position, playerPos);
                float targetDist = Vector3.Distance(mine.transform.position, data.target.transform.position);

                if (dist > config.MineDespawnRadius)
                {
                    mine.Kill();
                }
                else if (targetDist < config.MineExplosionRadius)
                {
                    var landmine = mine as Landmine;
                    if (landmine != null)
                    {
                        landmine.ObjectEntered(data.target.gameObject);
                    }
                }
                else
                {
                    remaining.Add(mine);
                }
            }

            data.mines = remaining;

            if (data.mines.Count < config.MaxMinesPerPlayer)
            {
                for (int i = 0; i < config.MinesPerSpawn && data.mines.Count < config.MaxMinesPerPlayer; i++)
                {
                    SpawnNewMine(player, data);
                }
            }
        }

        private void SpawnNewMine(BasePlayer player, CheatData data)
        {
            float angle = UnityEngine.Random.Range(0f, 360f);
            float distance = UnityEngine.Random.Range(2f, config.MineSpawnRadius);
            float rad = angle * Mathf.Deg2Rad;
            
            Vector3 spawnPos = player.transform.position;
            spawnPos.x += distance * Mathf.Cos(rad);
            spawnPos.z += distance * Mathf.Sin(rad);
            
            RaycastHit hit;
            if (Physics.Raycast(spawnPos + Vector3.up * 50f, Vector3.down, out hit, 100f, LayerMask.GetMask("Terrain", "World")))
            {
                spawnPos.y = hit.point.y + 0.05f;
            }

            foreach (var mine in data.mines)
            {
                if (mine != null && !mine.IsDestroyed && Vector3.Distance(mine.transform.position, spawnPos) < config.MinDistanceBetweenMines)
                {
                    return;
                }
            }

            var landmine = GameManager.server.CreateEntity("assets/prefabs/deployable/landmine/landmine.prefab", spawnPos, Quaternion.identity, true) as Landmine;
            if (landmine != null)
            {
                landmine.Spawn();
                landmine.creatorEntity = data.target;
                landmine.OwnerID = data.target.userID;
                landmine.SetFlag(BaseEntity.Flags.On, true);
                landmine.SetFlag(BaseEntity.Flags.Protected, true);
                landmine.SendNetworkUpdate();
                data.mines.Add(landmine);
            }
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            // Защищаем мины от взрывов
            var landmine = entity as Landmine;
            if (landmine != null)
            {
                foreach (var pair in activeCheaters)
                {
                    if (pair.Value.mines.Contains(landmine))
                    {
                        if (info?.damageTypes?.Has(Rust.DamageType.Explosion) ?? false)
                        {
                            return true; // Отменяем урон от взрыва
                        }
                    }
                }
            }

            // Защищаем других игроков от любого урона от наших мин
            var player = entity as BasePlayer;
            if (player != null)
            {
                // Проверяем, является ли источник урона миной или взрывом
                if (info?.Initiator is Landmine || (info?.damageTypes?.Has(Rust.DamageType.Explosion) ?? false))
                {
                    // Проверяем все активные мины
                    foreach (var pair in activeCheaters)
                    {
                        // Если игрок не является целью, отменяем весь урон рядом с минами
                        if (player != pair.Value.target)
                        {
                            foreach (var mine in pair.Value.mines)
                            {
                                if (mine != null && !mine.IsDestroyed)
                                {
                                    float dist = Vector3.Distance(mine.transform.position, player.transform.position);
                                    if (dist < 10f) // Защищаем в радиусе 10 метров от любой мины
                                    {
                                        return true; // Отменяем урон
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            DeactivateMines(player);
        }

        void Unload()
        {
            foreach (var pair in activeCheaters)
            {
                DeactivateMines(pair.Value.target);
            }
        }
    }
} 