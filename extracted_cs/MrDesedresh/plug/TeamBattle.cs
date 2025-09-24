using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("TeamBattle", "YourName", "1.2.3")]
    [Description("Система комнат для командных сражений с таймером, снаряжением и респавном")]
    public class TeamBattle : RustPlugin
    {
        #region Конфигурация и данные

        private const int MAX_ROOMS = 4;
        private const int MAX_TEAM_PLAYERS = 5;
        private const int DEFAULT_COUNTDOWN = 60;
        private const int FAST_COUNTDOWN = 10;
        private const float RESPAWN_DELAY = 5f;

        private Dictionary<int, Dictionary<string, List<BasePlayer>>> Rooms = new Dictionary<int, Dictionary<string, List<BasePlayer>>>();
        private Dictionary<int, Timer> RoomTimers = new Dictionary<int, Timer>();
        private Dictionary<int, int> RoomCountdowns = new Dictionary<int, int>();
        private Dictionary<int, bool> RoomLocked = new Dictionary<int, bool>();

        private Dictionary<int, Dictionary<string, List<Vector3>>> ManualSpawnPoints = new Dictionary<int, Dictionary<string, List<Vector3>>>()
        {
            [1] = new Dictionary<string, List<Vector3>>
            {
                ["Team1"] = new List<Vector3>
                {
                    new Vector3(68.46f, 5.01f, 47.67f),
                    new Vector3(69f, 5.01f, 36.81f),
                    new Vector3(68.5f, 5.01f, 54.51f),
                    new Vector3(65.28f, 5.01f, 68.76f),
                    new Vector3(67.28f, 5.01f, 25.14f)
                },
                ["Team2"] = new List<Vector3>
                {
                    new Vector3(124.03f, 5f, 25.46f),
                    new Vector3(121.39f, 5f, 39.64f),
                    new Vector3(122.07f, 5f, 45.6f),
                    new Vector3(122.72f, 5f, 57.33f),
                    new Vector3(121.82f, 5f, 68.51f)
                }
            },
            [2] = new Dictionary<string, List<Vector3>>
            {
                ["Team1"] = new List<Vector3>
                {
                    new Vector3(628f, 10f, -106f),
                    new Vector3(630f, 10f, -104f),
                    new Vector3(632f, 10f, -106f),
                    new Vector3(630f, 10f, -108f),
                    new Vector3(634f, 10f, -106f)
                },
                ["Team2"] = new List<Vector3>
                {
                    new Vector3(608f, 10f, -106f),
                    new Vector3(610f, 10f, -104f),
                    new Vector3(612f, 10f, -106f),
                    new Vector3(610f, 10f, -108f),
                    new Vector3(614f, 10f, -106f)
                }
            },
            [3] = new Dictionary<string, List<Vector3>>
            {
                ["Team1"] = new List<Vector3>
                {
                    new Vector3(942f, 10f, -106f),
                    new Vector3(944f, 10f, -104f),
                    new Vector3(946f, 10f, -106f),
                    new Vector3(944f, 10f, -108f),
                    new Vector3(948f, 10f, -106f)
                },
                ["Team2"] = new List<Vector3>
                {
                    new Vector3(922f, 10f, -106f),
                    new Vector3(924f, 10f, -104f),
                    new Vector3(926f, 10f, -106f),
                    new Vector3(924f, 10f, -108f),
                    new Vector3(928f, 10f, -106f)
                }
            },
            [4] = new Dictionary<string, List<Vector3>>
            {
                ["Team1"] = new List<Vector3>
                {
                    new Vector3(1256f, 10f, -106f),
                    new Vector3(1258f, 10f, -104f),
                    new Vector3(1260f, 10f, -106f),
                    new Vector3(1258f, 10f, -108f),
                    new Vector3(1262f, 10f, -106f)
                },
                ["Team2"] = new List<Vector3>
                {
                    new Vector3(1236f, 10f, -106f),
                    new Vector3(1238f, 10f, -104f),
                    new Vector3(1240f, 10f, -106f),
                    new Vector3(1238f, 10f, -108f),
                    new Vector3(1242f, 10f, -106f)
                }
            }
        };

        private Dictionary<BasePlayer, Timer> RespawnTimers = new Dictionary<BasePlayer, Timer>();
        private Dictionary<BasePlayer, PlayerStatsData> PlayerStats = new Dictionary<BasePlayer, PlayerStatsData>();

        class PlayerStatsData
        {
            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int Wins { get; set; }
        }

        #endregion

        #region Инициализация

        void Init()
        {
            for (int i = 1; i <= MAX_ROOMS; i++)
            {
                Rooms[i] = new Dictionary<string, List<BasePlayer>>
                {
                    ["Team1"] = new List<BasePlayer>(),
                    ["Team2"] = new List<BasePlayer>()
                };
                RoomCountdowns[i] = DEFAULT_COUNTDOWN;
                RoomLocked[i] = false;

                if (!ManualSpawnPoints.ContainsKey(i) || ManualSpawnPoints[i].Count == 0)
                {
                    Puts($"Внимание: для комнаты {i} не заданы координаты спавна!");
                }
            }

            permission.RegisterPermission("teambattle.admin", this);
            permission.RegisterPermission("teambattle.leave", this);
        }

        #endregion

        #region Основные команды

        [ChatCommand("play")]
        void PlayCommand(BasePlayer player, string command, string[] args)
        {
            ShowRoomSelection(player);
        }

        [ChatCommand("leave")]
        void LeaveCommand(BasePlayer player, string command, string[] args)
        {
            ProcessPlayerLeave(player, false);
        }

        [ChatCommand("tbleave")]
        void AdminLeaveCommand(BasePlayer admin, string command, string[] args)
        {
            if (!permission.UserHasPermission(admin.UserIDString, "teambattle.admin"))
            {
                admin.ChatMessage("<color=red>У вас нет прав на эту команду!</color>");
                return;
            }

            if (args.Length == 0)
            {
                admin.ChatMessage("Использование: /tbleave <ник игрока>");
                return;
            }

            var target = BasePlayer.Find(args[0]);
            if (target == null)
            {
                admin.ChatMessage("<color=red>Игрок не найден!</color>");
                return;
            }

            if (ProcessPlayerLeave(target, true))
            {
                admin.ChatMessage($"<color=green>Игрок {target.displayName} исключен из битвы!</color>");
            }
        }

        bool ProcessPlayerLeave(BasePlayer player, bool adminForced)
        {
            int roomId = FindPlayerRoom(player);
            if (roomId == -1)
            {
                if (adminForced)
                    player.ChatMessage("<color=yellow>Игрок не в битве!</color>");
                return false;
            }

            // Отменяем респавн
            if (RespawnTimers.TryGetValue(player, out Timer respawnTimer))
            {
                respawnTimer.Destroy();
                RespawnTimers.Remove(player);
            }

            // Удаляем из комнаты
            RemovePlayerFromAllRooms(player);
            
            // Телепорт и очистка
            player.Teleport(ConVar.Server.spawnpoint);
            player.inventory.Strip();
            player.health = player.MaxHealth();
            player.metabolism.bleeding.value = 0;

            // Эффекты и уведомления
            Effect.server.Run("assets/prefabs/misc/supply drop/effects/supply_drop_parachute.prefab", player.transform.position);
            CuiHelper.DestroyUi(player, "TeamSelectPanel");
            
            if (adminForced)
            {
                player.ChatMessage("<color=red>Администратор исключил вас из битвы!</color>");
            }
            else
            {
                player.ChatMessage("<color=green>Вы вышли из командной битвы!</color>");
            }

            // Обновляем UI и проверяем комнату
            UpdateRoomUI(roomId);
            if (Rooms[roomId]["Team1"].Count + Rooms[roomId]["Team2"].Count == 0)
            {
                ResetRoom(roomId);
            }

            // Статистика
            if (RoomLocked[roomId])
            {
                PlayerStats[player].Deaths++;
            }

            return true;
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            ProcessPlayerLeave(player, false);
        }

        #endregion

        #region Команды и UI

        [ChatCommand("play")]
        void PlayCommand(BasePlayer player, string command, string[] args)
        {
            ShowRoomSelection(player);
        }

        void ShowRoomSelection(BasePlayer player)
        {
            var elements = new CuiElementContainer();
            string panel = elements.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.1 0.1 0.9" },
                RectTransform = { AnchorMin = "0.3 0.2", AnchorMax = "0.7 0.8" },
                CursorEnabled = true
            }, "Overlay", "RoomSelectPanel");

            elements.Add(new CuiLabel
            {
                Text = { Text = "Выберите комнату", FontSize = 18, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0.1 0.85", AnchorMax = "0.9 0.95" }
            }, panel, "Title");

            for (int i = 1; i <= MAX_ROOMS; i++)
            {
                int roomId = i;
                int playersCount = Rooms[roomId]["Team1"].Count + Rooms[roomId]["Team2"].Count;
                string status = RoomLocked[roomId] ? "[ЗАКРЫТО]" : $"[{playersCount}/{MAX_TEAM_PLAYERS * 2}]";

                elements.Add(new CuiButton
                {
                    Button = {
                        Command = $"teambattle.selectroom {roomId}",
                        Color = RoomLocked[roomId] ? "0.3 0.3 0.3 0.5" : "0.3 0.3 0.3 1",
                        Close = panel
                    },
                    RectTransform = { AnchorMin = $"0.1 {0.75 - (i * 0.15)}", AnchorMax = $"0.9 {0.85 - (i * 0.15)}" },
                    Text = { Text = $"Комната {roomId} {status}", FontSize = 14, Align = TextAnchor.MiddleCenter }
                }, panel, $"RoomBtn_{i}");
            }

            CuiHelper.AddUi(player, elements);
        }

        [ConsoleCommand("teambattle.selectroom")]
        void SelectRoomCMD(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() as BasePlayer;
            if (player == null) return;

            if (!int.TryParse(arg.Args?[0], out int roomId) || roomId < 1 || roomId > MAX_ROOMS)
            {
                player.ChatMessage("Некорректный номер комнаты!");
                return;
            }

            if (RoomLocked[roomId])
            {
                player.ChatMessage("Комната уже в игре!");
                return;
            }

            CuiHelper.DestroyUi(player, "RoomSelectPanel");
            ShowTeamSelection(player, roomId);
        }

        void ShowTeamSelection(BasePlayer player, int roomId)
        {
            var elements = new CuiElementContainer();
            string panel = elements.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.1 0.1 0.9" },
                RectTransform = { AnchorMin = "0.3 0.2", AnchorMax = "0.7 0.8" },
                CursorEnabled = true
            }, "Overlay", "TeamSelectPanel");

            elements.Add(new CuiLabel
            {
                Text = { Text = $"Комната {roomId} - Старт через: {RoomCountdowns[roomId]} сек", FontSize = 18, Align = TextAnchor.MiddleCenter },
                RectTransform = { AnchorMin = "0.1 0.85", AnchorMax = "0.9 0.95" }
            }, panel, "Title");

            CreateTeamButton(elements, panel, roomId, "Team1", "0.2 0.4 0.8 1", "0.1 0.6", "0.9 0.75");
            CreateTeamButton(elements, panel, roomId, "Team2", "0.8 0.2 0.2 1", "0.1 0.35", "0.9 0.5");

            elements.Add(new CuiButton
            {
                Button = { Command = "teambattle.leaveroom", Color = "0.8 0.2 0.2 1", Close = panel },
                RectTransform = { AnchorMin = "0.1 0.1", AnchorMax = "0.9 0.25" },
                Text = { Text = "Покинуть комнату", FontSize = 14, Align = TextAnchor.MiddleCenter }
            }, panel, "LeaveBtn");

            CuiHelper.AddUi(player, elements);
        }

        void CreateTeamButton(CuiElementContainer elements, string panel, int roomId, string team, string color, string min, string max)
        {
            bool isFull = Rooms[roomId][team].Count >= MAX_TEAM_PLAYERS;

            elements.Add(new CuiButton
            {
                Button = {
                    Command = isFull ? "" : $"teambattle.selectteam {roomId} {team}",
                    Color = isFull ? "0.3 0.3 0.3 0.5" : color,
                    Close = panel
                },
                RectTransform = { AnchorMin = min, AnchorMax = max },
                Text = { Text = $"{team} ({Rooms[roomId][team].Count}/{MAX_TEAM_PLAYERS})", FontSize = 14, Align = TextAnchor.MiddleCenter }
            }, panel, $"{team}Btn");
        }

        #endregion

        #region Логика игры

        [ConsoleCommand("teambattle.selectteam")]
        void SelectTeamCMD(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() as BasePlayer;
            if (player == null) return;

            if (!int.TryParse(arg.Args?[0], out int roomId) || roomId < 1 || roomId > MAX_ROOMS)
            {
                player.ChatMessage("Ошибка выбора комнаты!");
                return;
            }

            if (RoomLocked[roomId])
            {
                player.ChatMessage("Игра уже началась!");
                return;
            }

            int currentRoom = FindPlayerRoom(player);
            if (currentRoom == roomId)
            {
                player.ChatMessage("Вы уже в этой комнате!");
                return;
            }

            string team = arg.Args?[1];
            if (string.IsNullOrEmpty(team) || !Rooms[roomId].ContainsKey(team))
            {
                player.ChatMessage("Ошибка выбора команды!");
                return;
            }

            if (Rooms[roomId][team].Count >= MAX_TEAM_PLAYERS)
            {
                player.ChatMessage("Команда заполнена!");
                return;
            }

            RemovePlayerFromAllRooms(player);
            Rooms[roomId][team].Add(player);

            if (!PlayerStats.ContainsKey(player))
            {
                PlayerStats[player] = new PlayerStatsData();
            }

            player.ChatMessage($"Вы в команде {team} (Комната {roomId})");

            UpdateRoomUI(roomId);
            ManageRoomTimer(roomId);

            Effect.server.Run("assets/prefabs/npc/patrol helicopter/effects/rocket_fire.prefab", player.transform.position);
        }

        [ConsoleCommand("teambattle.leaveroom")]
        void LeaveRoomCMD(ConsoleSystem.Arg arg)
        {
            var player = arg.Player() as BasePlayer;
            if (player == null) return;

            int roomId = FindPlayerRoom(player);
            if (roomId == -1) return;

            if (RespawnTimers.ContainsKey(player))
            {
                RespawnTimers[player].Destroy();
                RespawnTimers.Remove(player);
            }

            if (RoomLocked[roomId])
            {
                player.ChatMessage("Нельзя выйти во время игры!");
                return;
            }

            RemovePlayerFromAllRooms(player);
            CuiHelper.DestroyUi(player, "TeamSelectPanel");
            player.ChatMessage("Вы покинули комнату");
            ManageRoomTimer(roomId);
        }

        void RemovePlayerFromAllRooms(BasePlayer player)
        {
            foreach (var room in Rooms.Values)
            {
                foreach (var team in room.Values)
                {
                    team.Remove(player);
                }
            }
        }

        int FindPlayerRoom(BasePlayer player)
        {
            foreach (var kvp in Rooms)
            {
                foreach (var team in kvp.Value)
                {
                    if (team.Value.Contains(player))
                        return kvp.Key;
                }
            }
            return -1;
        }

        void UpdateRoomUI(int roomId)
        {
            foreach (var team in Rooms[roomId])
            {
                foreach (var player in team.Value)
                {
                    CuiHelper.DestroyUi(player, "TeamSelectPanel");
                    ShowTeamSelection(player, roomId);
                }
            }
        }

        void ManageRoomTimer(int roomId)
        {
            int playersCount = Rooms[roomId]["Team1"].Count + Rooms[roomId]["Team2"].Count;

            if (playersCount == 0)
            {
                ResetTimer(roomId);
                return;
            }

            RoomCountdowns[roomId] = playersCount >= MAX_TEAM_PLAYERS * 2 ? FAST_COUNTDOWN : DEFAULT_COUNTDOWN;

            if (!RoomTimers.ContainsKey(roomId))
            {
                StartRoomCountdown(roomId);
            }
        }

        void StartRoomCountdown(int roomId)
        {
            RoomTimers[roomId] = timer.Every(1f, () =>
            {
                RoomCountdowns[roomId]--;
                UpdateRoomUI(roomId);

                if (RoomCountdowns[roomId] <= 0)
                {
                    StartGame(roomId);
                    return;
                }

                if (Rooms[roomId]["Team1"].Count + Rooms[roomId]["Team2"].Count == 0)
                {
                    ResetTimer(roomId);
                }
            });
        }

        void ResetTimer(int roomId)
        {
            if (RoomTimers.TryGetValue(roomId, out Timer roomTimer))
            {
                roomTimer.Destroy();
                RoomTimers.Remove(roomId);
            }
            RoomCountdowns[roomId] = DEFAULT_COUNTDOWN;
        }

        void StartGame(int roomId)
        {
            RoomLocked[roomId] = true;
            Puts($"Игра началась в комнате {roomId}");

            TeleportTeams(roomId);
            GiveStarterKits(roomId);

            foreach (var team in Rooms[roomId])
            {
                foreach (var player in team.Value)
                {
                    player.ChatMessage($"<color=green>Игра началась! Вы в {team.Key}</color>");
                    CuiHelper.DestroyUi(player, "TeamSelectPanel");
                    Effect.server.Run("assets/prefabs/misc/xmas/presents/effects/gift_open.prefab", player.transform.position);
                }
            }

            timer.Once(300f, () => EndGame(roomId));
        }

        void TeleportTeams(int roomId)
        {
            if (!ManualSpawnPoints.ContainsKey(roomId))
            {
                Puts($"Ошибка: для комнаты {roomId} не заданы координаты спавна!");
                return;
            }

            for (int i = 0; i < Rooms[roomId]["Team1"].Count; i++)
            {
                if (i < ManualSpawnPoints[roomId]["Team1"].Count)
                {
                    BasePlayer player = Rooms[roomId]["Team1"][i];
                    player.Teleport(ManualSpawnPoints[roomId]["Team1"][i]);
                    PlayTeleportEffect(player);
                }
                else
                {
                    Puts($"Не хватает координат спавна для Team1 в комнате {roomId}!");
                }
            }

            for (int i = 0; i < Rooms[roomId]["Team2"].Count; i++)
            {
                if (i < ManualSpawnPoints[roomId]["Team2"].Count)
                {
                    BasePlayer player = Rooms[roomId]["Team2"][i];
                    player.Teleport(ManualSpawnPoints[roomId]["Team2"][i]);
                    PlayTeleportEffect(player);
                }
                else
                {
                    Puts($"Не хватает координат спавна для Team2 в комнате {roomId}!");
                }
            }
        }

        void PlayTeleportEffect(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;

            Effect.server.Run("assets/prefabs/misc/particle_teleport.prefab", player.transform.position);
            Effect.server.Run("assets/prefabs/misc/sound_teleport.prefab", player.transform.position);
        }

        void GiveStarterKits(int roomId)
        {
            foreach (var team in Rooms[roomId])
            {
                foreach (var player in team.Value)
                {
                    player.inventory.Strip();
                    GiveWeapon(player, "rifle.ak");
                    GiveItem(player, "ammo.rifle", 100);
                    GiveItem(player, "medkit", 3);
                    GiveItem(player, "bandage", 5);
                }
            }
        }

        void GiveWeapon(BasePlayer player, string itemName)
        {
            var item = ItemManager.CreateByName(itemName, 1);
            if (item != null)
            {
                if (!item.MoveToContainer(player.inventory.containerBelt))
                {
                    item.Remove();
                }
            }
        }

        void GiveItem(BasePlayer player, string itemName, int amount)
        {
            var item = ItemManager.CreateByName(itemName, amount);
            if (item != null)
            {
                if (!item.MoveToContainer(player.inventory.containerBelt))
                {
                    item.Remove();
                }
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            var player = entity as BasePlayer;
            if (player == null) return;

            int roomId = FindPlayerRoom(player);
            if (roomId == -1 || !RoomLocked[roomId]) return;

            player.inventory.Strip();
            player.ChatMessage($"<color=red>Вы погибли! Возрождение через {RESPAWN_DELAY} сек.</color>");

            if (RespawnTimers.ContainsKey(player))
            {
                RespawnTimers[player].Destroy();
                RespawnTimers.Remove(player);
            }

            RespawnTimers[player] = timer.Once(RESPAWN_DELAY, () => RespawnPlayer(player, roomId));
        }

        void RespawnPlayer(BasePlayer player, int roomId)
        {
            if (player == null || !player.IsConnected) return;

            string playerTeam = Rooms[roomId]["Team1"].Contains(player) ? "Team1" : "Team2";
            Vector3 spawnPos = FindAvailableSpawnPoint(roomId, playerTeam);

            player.Respawn();
            timer.Once(0.1f, () =>
            {
                if (player.IsConnected)
                {
                    player.Teleport(spawnPos);
                    GiveStarterKitsToPlayer(player);
                    PlayTeleportEffect(player);
                }
            });

            if (RespawnTimers.ContainsKey(player))
            {
                RespawnTimers.Remove(player);
            }
        }

        Vector3 FindAvailableSpawnPoint(int roomId, string team)
        {
            if (ManualSpawnPoints.ContainsKey(roomId) && ManualSpawnPoints[roomId].ContainsKey(team))
            {
                foreach (var point in ManualSpawnPoints[roomId][team])
                {
                    if (IsSpawnPointFree(point))
                        return point;
                }
            }

            float offsetX = team == "Team1" ? 10f : -10f;
            float randomZ = UnityEngine.Random.Range(-5f, 5f);

            Vector3 basePos = ManualSpawnPoints.ContainsKey(roomId) &&
                              ManualSpawnPoints[roomId].ContainsKey(team) &&
                              ManualSpawnPoints[roomId][team].Count > 0
                ? ManualSpawnPoints[roomId][team][0]
                : new Vector3(314 * roomId, 10, -106);

            return basePos + new Vector3(offsetX, 0, randomZ);
        }

        bool IsSpawnPointFree(Vector3 point)
        {
            var entities = new List<BaseEntity>();
            Vis.Entities(point, 1f, entities);
            return !entities.Any(e => e is BasePlayer);
        }

        void GiveStarterKitsToPlayer(BasePlayer player)
        {
            player.inventory.Strip();
            GiveWeapon(player, "rifle.ak");
            GiveItem(player, "ammo.rifle", 100);
            GiveItem(player, "medkit", 3);
            GiveItem(player, "bandage", 5);
        }

        void EndGame(int roomId)
        {
            foreach (var kvp in RespawnTimers.ToList())
            {
                if (kvp.Value != null && !kvp.Value.Destroyed)
                {
                    kvp.Value.Destroy();
                }
            }
            RespawnTimers.Clear();

            // Здесь можно добавить логику определения победителя
            // Пока просто отправим всем сообщение о завершении игры

            foreach (var team in Rooms[roomId])
            {
                foreach (var player in team.Value)
                {
                    player.ChatMessage("<color=blue>Игра окончена!</color>");
                }
            }

            ResetRoom(roomId);
        }

        void ResetRoom(int roomId)
        {
            Rooms[roomId]["Team1"].Clear();
            Rooms[roomId]["Team2"].Clear();
            RoomLocked[roomId] = false;
            RoomCountdowns[roomId] = DEFAULT_COUNTDOWN;
            Puts($"Комната {roomId} сброшена");
        }

        #endregion

        #region Админ-команды

        [ChatCommand("tbreset")]
        void AdminResetCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "teambattle.admin"))
            {
                player.ChatMessage("У вас нет прав на эту команду!");
                return;
            }

            if (args.Length == 0)
            {
                player.ChatMessage("Использование: /tbreset [номер комнаты или all]");
                return;
            }

            if (args[0] == "all")
            {
                for (int i = 1; i <= MAX_ROOMS; i++)
                {
                    ResetRoom(i);
                }
                player.ChatMessage("Все комнаты сброшены!");
            }
            else if (int.TryParse(args[0], out int roomId) && roomId >= 1 && roomId <= MAX_ROOMS)
            {
                ResetRoom(roomId);
                player.ChatMessage($"Комната {roomId} сброшена!");
            }
            else
            {
                player.ChatMessage("Некорректный номер комнаты!");
            }
        }

        #endregion
    }
}
