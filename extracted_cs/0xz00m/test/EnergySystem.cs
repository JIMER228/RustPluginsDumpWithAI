using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Rust;

namespace Oxide.Plugins
{
    [Info("EnergySystem", "YourName", "1.0.13")]
    [Description("Система энергии с UI-шкалой, зонами и ограничениями для игроков с низкой энергией")]
    public class EnergySystem : RustPlugin
    {
        private Dictionary<ulong, PlayerEnergyData> playerData = new Dictionary<ulong, PlayerEnergyData>();
        private Dictionary<ulong, string> uiPool = new Dictionary<ulong, string>();
        private List<MonumentData> monuments = new List<MonumentData>();
        private const float MAX_ENERGY = 200000f;
        private const float INITIAL_ENERGY = 30000f;
        private const float SAFE_ZONE_REGEN = 500f;    // Регенерация в безопасной зоне
        private const float NORMAL_ZONE_DRAIN = 50f;   // Расход в обычной зоне
        private const float RT_ZONE_DRAIN = 200f;      // Расход в RT зоне
        private const float DRAIN_INTERVAL = 10f;      // Интервал расхода энергии
        private const float REGEN_INTERVAL = 60f;      // Интервал регенерации
        private const float MOVEMENT_THRESHOLD = 20f;  // Проверка зоны при перемещении
        private const float DATA_SAVE_INTERVAL = 600f; // Сохранение данных раз в 10 минут
        private const float SIGNIFICANT_ENERGY_CHANGE = 100f; // Порог для обновления UI
        private const float ZONE_CHECK_INTERVAL = 1f;  // Интервал проверки зоны (1 секунда)

        // API Dictionary
        private static EnergySystem _instance;

        #region Data Structures
        private class PlayerEnergyData
        {
            public float Energy { get; set; }
            public Vector3 LastPosition { get; set; }
            public ZoneType CurrentZone { get; set; }
            public string UiId { get; set; }
            public long LastLogoutTime { get; set; }
            public bool IsDead { get; set; }
        }

        private class MonumentData
        {
            public string Name { get; set; }
            public Vector3 Position { get; set; }
            public float Radius { get; set; }
            public ZoneType Type { get; set; }
        }

        private enum ZoneType
        {
            Safe,    // Безопасная зона (шкаф, Outpost, Bandit Camp)
            Normal,  // Обычная зона
            RT       // RT зона (радтаун)
        }
        #endregion

        #region Data Management
        private void SaveData()
        {
            var dataToSave = playerData.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    Energy = kvp.Value.Energy,
                    LastLogoutTime = kvp.Value.LastLogoutTime,
                    CurrentZone = kvp.Value.CurrentZone,
                    IsDead = kvp.Value.IsDead
                }
            );
            Interface.Oxide.DataFileSystem.WriteObject("EnergySystem", dataToSave);
        }

        private void LoadData()
        {
            var loadedData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, Dictionary<string, object>>>("EnergySystem");
            foreach (var entry in loadedData)
            {
                playerData[entry.Key] = new PlayerEnergyData
                {
                    Energy = Convert.ToSingle(entry.Value["Energy"]),
                    LastPosition = Vector3.zero,
                    CurrentZone = entry.Value.ContainsKey("CurrentZone") ? (ZoneType)Enum.Parse(typeof(ZoneType), entry.Value["CurrentZone"].ToString()) : ZoneType.Normal,
                    UiId = null,
                    LastLogoutTime = entry.Value.ContainsKey("LastLogoutTime") ? Convert.ToInt64(entry.Value["LastLogoutTime"]) : 0,
                    IsDead = entry.Value.ContainsKey("IsDead") && Convert.ToBoolean(entry.Value["IsDead"])
                };
            }
        }
        #endregion

        #region Plugin Hooks
        private void Init()
        {
            LoadData();
            timer.Every(DRAIN_INTERVAL, DrainEnergyForAllPlayers);
            timer.Every(REGEN_INTERVAL, RegenEnergyForAllPlayers);
            timer.Every(DATA_SAVE_INTERVAL, SaveData);
            
            // Проверка зоны каждую секунду для всех игроков
            timer.Every(ZONE_CHECK_INTERVAL, CheckAllPlayerZones);
            
            // Регистрируем разрешение для команды energy_set
            permission.RegisterPermission("energysystem.admin", this);
            
            // Сохраняем экземпляр плагина для API доступа
            _instance = this;
        }

        private void OnServerInitialized()
        {
            CacheMonuments();
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
            }
            uiPool.Clear();
            SaveData();
        }

        private void OnServerSave()
        {
            SaveData();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (!playerData.ContainsKey(player.userID))
            {
                playerData[player.userID] = new PlayerEnergyData
                {
                    Energy = INITIAL_ENERGY,
                    LastPosition = player.transform.position,
                    CurrentZone = GetPlayerZone(player, player.transform.position),
                    UiId = null,
                    LastLogoutTime = 0,
                    IsDead = false
                };
            }
            else
            {
                var data = playerData[player.userID];
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (data.LastLogoutTime > 0)
                {
                    float offlineSeconds = currentTime - data.LastLogoutTime;
                    float energyChange = CalculateOfflineEnergyChange(data.CurrentZone, data.IsDead, offlineSeconds);
                    data.Energy = Mathf.Clamp(data.Energy + energyChange, 0, MAX_ENERGY);
                }
                if (!data.IsDead)
                {
                    // Сразу определяем зону игрока при подключении
                    data.CurrentZone = GetPlayerZone(player, player.transform.position);
                }
            }
            playerData[player.userID].LastPosition = player.transform.position;
            
            // Создаем UI сразу после подключения
            timer.Once(0.1f, () => UpdateUI(player));
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (playerData.ContainsKey(player.userID))
            {
                playerData[player.userID].LastLogoutTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            DestroyUI(player);
        }

        private void OnPlayerDie(BasePlayer player, HitInfo info)
        {
            if (playerData.ContainsKey(player.userID))
            {
                playerData[player.userID].IsDead = true;
                UpdateUI(player);
            }
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (playerData.ContainsKey(player.userID))
            {
                var data = playerData[player.userID];
                data.IsDead = false;
                data.CurrentZone = GetPlayerZone(player, player.transform.position);
                data.LastPosition = player.transform.position;
                
                // Восстанавливаем UI после респавна
                timer.Once(0.1f, () => UpdateUI(player));
            }
        }

        // Проверяет зоны для всех игроков
        private void CheckAllPlayerZones()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player == null || !player.IsConnected || !playerData.ContainsKey(player.userID) || playerData[player.userID].IsDead)
                    continue;
                    
                var data = playerData[player.userID];
                Vector3 currentPosition = player.transform.position;
                
                // Снижен порог для более частой проверки
                if (Vector3.Distance(currentPosition, data.LastPosition) > 3f)
                {
                    CheckAndUpdatePlayerZone(player, currentPosition);
                }
            }
        }
        
        // Проверяет зону игрока и обновляет её при изменении
        private void CheckAndUpdatePlayerZone(BasePlayer player, Vector3 position)
        {
            if (!playerData.ContainsKey(player.userID)) return;
            
            var data = playerData[player.userID];
            ZoneType newZone = GetPlayerZone(player, position);
            
            if (newZone != data.CurrentZone)
            {
                string oldZoneName = data.CurrentZone.ToString();
                data.CurrentZone = newZone;
                data.LastPosition = position;
                
                string newZoneName = newZone.ToString();
                Puts($"Игрок {player.displayName} сменил зону с {oldZoneName} на {newZoneName}");
                
                // Отправляем сообщение игроку о смене зоны
                switch (newZone)
                {
                    case ZoneType.Safe:
                        SendReply(player, "Вы вошли в безопасную зону. Энергия восстанавливается.");
                        break;
                    case ZoneType.RT:
                        SendReply(player, "Вы вошли в радиоактивную зону. Энергия расходуется быстрее!");
                        break;
                    case ZoneType.Normal:
                        SendReply(player, "Вы вошли в обычную зону.");
                        break;
                }
                
                // Мгновенно обновляем UI для отражения смены зоны
                UpdateUI(player);
            }
            
            // В любом случае обновляем последнюю позицию
            data.LastPosition = position;
        }
        
        // Основной хук для обработки предметов в руках
        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.IsDown(BUTTON.FORWARD) || input.IsDown(BUTTON.BACKWARD) || 
                input.IsDown(BUTTON.LEFT) || input.IsDown(BUTTON.RIGHT) ||
                input.IsDown(BUTTON.JUMP) || input.IsDown(BUTTON.DUCK))
            {
                var position = player.transform.position;
                CheckAndUpdatePlayerZone(player, position);
            }
        }
        
        // Отслеживание изменения энергии
        private void CheckEnergyForHoldingItems()
        {
            // Можно добавить логику проверки энергии для удержания предметов
        }

        private void ProcessPlayerDrain(BasePlayer player)
        {
            if (player == null || !player.IsConnected || !playerData.ContainsKey(player.userID) || playerData[player.userID].IsDead) return;
            
            var data = playerData[player.userID];
            
            // Проверяем зону игрока перед тратой энергии
            CheckAndUpdatePlayerZone(player, player.transform.position);
            
            if (data.CurrentZone != ZoneType.Safe) // Не тратим энергию в сейф-зонах
            {
                float energyChange = data.CurrentZone == ZoneType.RT ? -RT_ZONE_DRAIN : -NORMAL_ZONE_DRAIN;
                data.Energy = Mathf.Clamp(data.Energy + energyChange, 0, MAX_ENERGY);
                UpdateUI(player);
            }
        }

        // Наблюдение за изменением энергии при регенерации
        private void ProcessPlayerRegen(BasePlayer player)
        {
            if (player == null || !player.IsConnected || !playerData.ContainsKey(player.userID) || playerData[player.userID].IsDead) return;

            var data = playerData[player.userID];
            
            // Проверяем зону игрока перед регенерацией энергии
            CheckAndUpdatePlayerZone(player, player.transform.position);
            
            if (data.CurrentZone == ZoneType.Safe)
            {
                data.Energy = Mathf.Clamp(data.Energy + SAFE_ZONE_REGEN, 0, MAX_ENERGY);
                UpdateUI(player);
                Puts($"Регенерация энергии для {player.displayName}: +{SAFE_ZONE_REGEN}, текущая энергия: {data.Energy}");
            }
        }

        private float CalculateOfflineEnergyChange(ZoneType zone, bool isDead, float offlineSeconds)
        {
            if (isDead) return 0;
            return zone switch
            {
                ZoneType.Safe => SAFE_ZONE_REGEN * (offlineSeconds / REGEN_INTERVAL),
                ZoneType.RT => -RT_ZONE_DRAIN * (offlineSeconds / DRAIN_INTERVAL),
                _ => -NORMAL_ZONE_DRAIN * (offlineSeconds / DRAIN_INTERVAL)
            };
        }

        // Метод проверки и обновления для массовой обработки игроков
        private void DrainEnergyForAllPlayers()
        {
            var players = BasePlayer.activePlayerList.ToList();
            for (int i = 0; i < players.Count; i += 50)
            {
                int startIndex = i;
                timer.In(0.01f * (i / 50), () =>
                {
                    for (int j = startIndex; j < Math.Min(startIndex + 50, players.Count); j++)
                    {
                        ProcessPlayerDrain(players[j]);
                    }
                });
            }
        }

        private void RegenEnergyForAllPlayers()
        {
            var players = BasePlayer.activePlayerList.ToList();
            for (int i = 0; i < players.Count; i += 50)
            {
                int startIndex = i;
                timer.In(0.01f * (i / 50), () =>
                {
                    for (int j = startIndex; j < Math.Min(startIndex + 50, players.Count); j++)
                    {
                        ProcessPlayerRegen(players[j]);
                    }
                });
            }
        }
        #endregion

        #region UI Management
        private void UpdateUI(BasePlayer player)
        {
            if (!playerData.ContainsKey(player.userID)) return;
            var data = playerData[player.userID];

            // Удаляем старую панель, если есть
            if (!string.IsNullOrEmpty(data.UiId))
        {
                CuiHelper.DestroyUi(player, data.UiId);
                data.UiId = null;
                uiPool.Remove(player.userID);
            }

            // Создаем новую панель
            var elements = new CuiElementContainer();
            string panelId = elements.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0.5" },
                RectTransform = { AnchorMin = "0.02 0.85", AnchorMax = "0.25 0.90" },
                CursorEnabled = false
            }, "Hud", "EnergySystemPanel");
            data.UiId = panelId;
            uiPool[player.userID] = panelId;

            string barColor = data.CurrentZone switch
            {
                ZoneType.Safe => "0.2 0.8 0.2 1",  // Зеленый
                ZoneType.Normal => "0.8 0.8 0.2 1", // Желтый
                ZoneType.RT => "0.8 0.2 0.2 1",     // Красный
                _ => "0.8 0.8 0.2 1"
            };

            float energyFraction = data.Energy / MAX_ENERGY;

            // Добавляем полоску энергии
            elements.Add(new CuiElement
            {
                Name = CuiHelper.GetGuid(),
                Parent = panelId,
                Components =
                {
                    new CuiImageComponent { Color = barColor },
                    new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = $"{energyFraction} 0.5" }
                }
            });

            // Добавляем текст
            elements.Add(new CuiLabel
            {
                Text = { Text = $"Energy: {data.Energy:F0}/{MAX_ENERGY}", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0.5", AnchorMax = "1 1" }
            }, panelId);

            CuiHelper.AddUi(player, elements);
        }
        
        private void DestroyUI(BasePlayer player)
        {
            if (!playerData.ContainsKey(player.userID)) return;
            var data = playerData[player.userID];
            if (!string.IsNullOrEmpty(data.UiId))
        {
                CuiHelper.DestroyUi(player, data.UiId);
                data.UiId = null;
                uiPool.Remove(player.userID);
            }
        }
        #endregion

        #region Commands
        [ChatCommand("energy")]
        private void CheckEnergy(BasePlayer player, string command, string[] args)
        {
            if (!playerData.ContainsKey(player.userID)) return;
            var data = playerData[player.userID];
            SendReply(player, $"Ваша энергия: {data.Energy:F0}/{MAX_ENERGY}");
            UpdateUI(player);
        }
        
        [ChatCommand("energy_set")]
        private void SetPlayerEnergy(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin && !permission.UserHasPermission(player.UserIDString, "energysystem.admin"))
            {
                SendReply(player, "У вас нет прав для выполнения этой команды!");
                return;
            }
            
            if (args.Length != 2)
            {
                SendReply(player, "Использование: /energy_set <steamID или ник> <количество_энергии>");
                return;
            }
            
            string targetIdentifier = args[0];
            BasePlayer targetPlayer = null;
            ulong targetSteamID = 0;
            
            if (ulong.TryParse(targetIdentifier, out targetSteamID))
            {
                targetPlayer = BasePlayer.FindByID(targetSteamID);
            }
            else
            {
                targetPlayer = BasePlayer.Find(targetIdentifier);
                if (targetPlayer != null)
                {
                    targetSteamID = targetPlayer.userID;
                }
                else
                {
                    SendReply(player, "Игрок не найден! Проверьте SteamID или никнейм.");
                    return;
                }
            }
            
            if (!float.TryParse(args[1], out float energyAmount))
            {
                SendReply(player, "Некорректное количество энергии!");
                return;
            }
            
            if (!playerData.ContainsKey(targetSteamID))
            {
                playerData[targetSteamID] = new PlayerEnergyData
                {
                    Energy = INITIAL_ENERGY,
                    LastPosition = Vector3.zero,
                    CurrentZone = ZoneType.Normal,
                    UiId = null,
                    LastLogoutTime = 0,
                    IsDead = false
                };
            }
            
            playerData[targetSteamID].Energy = Mathf.Clamp(energyAmount, 0, MAX_ENERGY);
            
            if (targetPlayer != null && targetPlayer.IsConnected)
            {
                UpdateUI(targetPlayer);
                SendReply(targetPlayer, $"Администратор {player.displayName} установил вам энергию: {energyAmount:F0}");
            }
            
            SendReply(player, $"Вы установили игроку {(targetPlayer != null ? targetPlayer.displayName : targetSteamID.ToString())} энергию: {energyAmount:F0}");
            SaveData(); 
        }
        #endregion

        #region Helper Methods
        private void CacheMonuments()
        {
            monuments.Clear();
            if (TerrainMeta.Path == null || TerrainMeta.Path.Monuments == null)
            {
                timer.Once(5f, CacheMonuments);
                return;
            }

            Puts("Начинаю кэширование монументов...");
            
            string[] rtKeywords = new string[] {
                "radtown", "launch_site", "powerplant", "trainyard", "military_tunnel", "airfield", 
                "water_treatment", "harbor", "arctic", "research", "dome", "satellite", "junkyard",
                "excavator", "sewer", "abandoned", "giant", "supermarket", "gas", "station",
                "lighthouse", "cave", "warehouse", "mining", "quarry", "mining_quarry", "arctic_research_base"
            };
            
            string[] safeKeywords = new string[] {
                "outpost", "bandit", "ranch", "barn", "compound", "fishing_village", "safe"
            };

            Dictionary<string, float> monumentRadiusMap = new Dictionary<string, float>
            {
                {"lighthouse/lighthouse.prefab", 40f},
                {"launch_site.prefab", 200f},
                {"military_tunnel.prefab", 180f},
                {"powerplant.prefab", 180f},
                {"airfield.prefab", 170f},
                {"water_treatment.prefab", 170f},
                {"harbor.prefab", 100f},
                {"harbor_2.prefab", 100f},
                {"trainyard.prefab", 100f},
                {"dome.prefab", 80f},
                {"junkyard.prefab", 80f},
                {"satellite_dish.prefab", 80f},
                {"arctic_research_base.prefab", 80f},
                {"gas_station.prefab", 60f},
                {"supermarket.prefab", 60f},
                
                {"bandit_town.prefab", 160f},
                {"compound.prefab", 160f},
                
                {"cave_small_medium.prefab", 50f},
                {"cave_medium_medium.prefab", 50f},
                {"cave_small_easy.prefab", 50f},
                {"cave_small_hard.prefab", 50f},
                {"cave_large_medium.prefab", 50f},
                
                {"fishing_village.prefab", 80f},
                {"fishing_village_small.prefab", 80f},
                {"underwater_lab_a.prefab", 100f},
                {"underwater_lab_b.prefab", 100f},
                {"underwater_lab_c.prefab", 100f},
                {"underwater_lab_d.prefab", 100f},
                {"oilrig_1.prefab", 100f},
                {"oilrig_2.prefab", 100f}
            };

            foreach (var monument in TerrainMeta.Path.Monuments)
            {
                if (monument == null) continue;
                
                string fullName = monument.name;
                string lowerName = fullName.ToLower();
                ZoneType type = ZoneType.Normal;
                
                string fileName = lowerName.Contains("/") 
                    ? lowerName.Substring(lowerName.LastIndexOf("/") + 1) 
                    : lowerName;
                    
                Puts($"Обрабатываю монумент: {fullName}, файл: {fileName}");
                
                foreach(var keyword in rtKeywords)
                {
                    if (lowerName.Contains(keyword))
                    {
                        type = ZoneType.RT;
                        break;
                    }
                }
                
                if (type != ZoneType.RT)
                {
                    foreach(var keyword in safeKeywords)
                    {
                        if (lowerName.Contains(keyword))
                        {
                            if (lowerName.Contains("fishing_village") && !lowerName.Contains("bandit") && !lowerName.Contains("outpost"))
                            {
                                type = ZoneType.Normal;
                            }
                            else
                            {
                                type = ZoneType.Safe;
                            }
                            break;
                        }
                    }
                }
                
                float radius = 100f; 
                
                foreach (var entry in monumentRadiusMap)
                {
                    if (fileName.EndsWith(entry.Key))
                    {
                        radius = entry.Value;
                        Puts($"Установлен специальный радиус для {fileName}: {radius}");
                        break;
                    }
                }
                
                if (radius == 100f)
                {
                    if (type == ZoneType.RT)
                        radius = 80f;
                    else if (type == ZoneType.Safe)
                        radius = 150f;
                    else
                        radius = 100f;
                }
                
                monuments.Add(new MonumentData
                {
                    Name = monument.name,
                    Position = monument.transform.position,
                    Radius = radius,
                    Type = type
                });
                
                Puts($"Добавлен монумент: {monument.name}, Тип: {type}, Радиус: {radius}");
            }
            
            Puts($"Кэшировано {monuments.Count} монументов");
        }

        private ZoneType GetPlayerZone(BasePlayer player, Vector3 position)
        {
            var privilege = player.GetBuildingPrivilege();
            if (privilege != null && privilege.IsAuthed(player))
            {
                return ZoneType.Safe;
            }

            foreach (var monument in monuments)
            {
                float distance = Vector3.Distance(position, monument.Position);
                if (distance < monument.Radius)
                {
                    return monument.Type;
                }
            }

            if (player.InSafeZone())
            {
                return ZoneType.Safe;
            }

            return ZoneType.Normal;
        }
        #endregion

        #region API Methods
        private float GetPlayerEnergy(ulong playerId)
        {
            if (playerData.ContainsKey(playerId))
                return playerData[playerId].Energy;
            return 0f;
        }
        
        private void SetPlayerEnergy(ulong playerId, float amount)
        {
            if (!playerData.ContainsKey(playerId))
            {
                playerData[playerId] = new PlayerEnergyData
                {
                    Energy = Mathf.Clamp(amount, 0, MAX_ENERGY),
                    LastPosition = Vector3.zero,
                    CurrentZone = ZoneType.Normal,
                    UiId = null,
                    LastLogoutTime = 0,
                    IsDead = false
                };
            }
            else
            {
                playerData[playerId].Energy = Mathf.Clamp(amount, 0, MAX_ENERGY);
            }
            
            var player = BasePlayer.FindByID(playerId);
            if (player != null && player.IsConnected)
            {
                UpdateUI(player);
            }
        }
        
        private void ModifyPlayerEnergy(ulong playerId, float amount)
        {
            if (!playerData.ContainsKey(playerId))
            {
                playerData[playerId] = new PlayerEnergyData
                {
                    Energy = Mathf.Clamp(INITIAL_ENERGY + amount, 0, MAX_ENERGY),
                    LastPosition = Vector3.zero,
                    CurrentZone = ZoneType.Normal,
                    UiId = null,
                    LastLogoutTime = 0,
                    IsDead = false
                };
            }
            else
            {
                playerData[playerId].Energy = Mathf.Clamp(playerData[playerId].Energy + amount, 0, MAX_ENERGY);
            }
            
            var player = BasePlayer.FindByID(playerId);
            if (player != null && player.IsConnected)
            {
                UpdateUI(player);
            }
        }
        
        private float GetMaxEnergy()
        {
            return MAX_ENERGY;
        }
        
        private ZoneType GetCurrentPlayerZone(ulong playerId)
        {
            if (playerData.ContainsKey(playerId))
                return playerData[playerId].CurrentZone;
            return ZoneType.Normal;
        }
        
        private void CallHook_OnEnergyChanged(ulong playerId, float oldEnergy, float newEnergy)
        {
            Interface.CallHook("OnPlayerEnergyChanged", playerId, oldEnergy, newEnergy);
        }
        
        private void CallHook_OnZoneChanged(ulong playerId, ZoneType oldZone, ZoneType newZone)
        {
            Interface.CallHook("OnPlayerZoneChanged", playerId, oldZone.ToString(), newZone.ToString());
        }
        
        public static class API
        {
            public static float GetPlayerEnergy(ulong playerId)
            {
                return _instance?.GetPlayerEnergy(playerId) ?? 0f;
            }
            
            public static void SetPlayerEnergy(ulong playerId, float amount)
            {
                _instance?.SetPlayerEnergy(playerId, amount);
            }
            
            public static void ModifyPlayerEnergy(ulong playerId, float amount)
            {
                _instance?.ModifyPlayerEnergy(playerId, amount);
            }
            
            public static float GetMaxEnergy()
            {
                return _instance?.GetMaxEnergy() ?? 0f;
            }
            
            public static string GetPlayerZone(ulong playerId)
            {
                return _instance?.GetCurrentPlayerZone(playerId).ToString() ?? "Unknown";
            }
        }
        #endregion
    }
}