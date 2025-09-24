// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Team Manager", "AI Assistant", "2.0.0")]
    [Description("Advanced team management system with waiting rooms and statistics")]
    public class TeamManager : RustPlugin
    {
        #region Configuration
        private Configuration config;

        private class Configuration
        {
            [JsonProperty("Default Room Position")]
            public Vector3 DefaultRoomPosition = new Vector3(100f, 20f, 100f);
            
            [JsonProperty("Max Team Size")]
            public int MaxTeamSize = 4;
            
            [JsonProperty("Max Activity Log Entries")]
            public int MaxLogEntries = 20;
        }

        protected override void LoadDefaultConfig() => Config.WriteObject(new Configuration());
        #endregion

        #region Data Storage
        private StoredData storedData;

        private class StoredData
        {
            public Dictionary<ulong, PlayerData> Players = new Dictionary<ulong, PlayerData>();
            public Dictionary<uint, TeamData> Teams = new Dictionary<uint, TeamData>();
            public List<WaitingRoom> WaitingRooms = new List<WaitingRoom>();
            public uint NextTeamId = 1;
            public uint NextRoomId = 2;
        }

        private class PlayerData
        {
            public string Name;
            public PlayerStats Stats = new PlayerStats();
            public uint? CurrentTeamId;
            public uint? CurrentRoomId;
            public bool IsOnline;
        }

        private class TeamData
        {
            public string Name;
            public List<ulong> Members = new List<ulong>();
            public List<ulong> PendingInvites = new List<ulong>();
            public Queue<string> ActivityLog = new Queue<string>();
            public TeamStats Stats = new TeamStats();
            public DateTime CreatedAt = DateTime.UtcNow;
        }

        private class WaitingRoom
        {
            public uint Id;
            public string Name;
            public Vector3 Position;
            public int MaxPlayers;
            public List<ulong> Players = new List<ulong>();
        }
        #endregion

        #region Initialization
        private void Init()
        {
            LoadConfig();
            config = Config.ReadObject<Configuration>();
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);
        }
        #endregion

        #region Player Management
        private void OnPlayerConnected(BasePlayer player)
        {
            var steamId = player.userID;
            
            // Создаем или обновляем запись игрока
            if (!storedData.Players.ContainsKey(steamId))
            {
                storedData.Players[steamId] = new PlayerData
                {
                    Name = player.displayName,
                    IsOnline = true
                };
            }
            else
            {
                storedData.Players[steamId].IsOnline = true;
            }

            // Телепортировать в комнату ожидания по умолчанию
            MoveToDefaultRoom(player);
            SendWelcomeMessage(player);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            var steamId = player.userID;
            if (storedData.Players.ContainsKey(steamId))
            {
                storedData.Players[steamId].IsOnline = false;
                SaveData();
            }
        }
        #endregion

        #region Team Commands
        [ChatCommand("createteam")]
        private void CreateTeamCommand(BasePlayer player, string command, string[] args)
        {
            // Проверка условий создания команды
            if (!IsInWaitingRoom(player))
            {
                SendReply(player, "Вы должны быть в комнате ожидания для создания команды!");
                return;
            }

            if (args.Length < 1)
            {
                SendReply(player, "Используйте: /createteam <название команды>");
                return;
            }

            var teamName = string.Join(" ", args);
            CreateTeam(player, teamName);
        }

        [ChatCommand("invite")]
        private void InvitePlayerCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                SendReply(player, "Используйте: /invite <имя игрока>");
                return;
            }

            var targetPlayer = FindPlayer(args[0]);
            if (targetPlayer == null)
            {
                SendReply(player, "Игрок не найден!");
                return;
            }

            InvitePlayer(player, targetPlayer);
        }
        #endregion

        #region Core Logic
        private void CreateTeam(BasePlayer leader, string teamName)
        {
            var teamId = storedData.NextTeamId++;
            var newTeam = new TeamData
            {
                Name = teamName,
                Members = { leader.userID }
            };

            storedData.Teams[teamId] = newTeam;
            storedData.Players[leader.userID].CurrentTeamId = teamId;
            
            // Обновление UI для игрока
            UpdatePlayerUI(leader);
            
            // Логирование действия
            LogTeamActivity(teamId, $"{leader.displayName} создал команду");
            
            SendReply(leader, $"Команда {teamName} успешно создана!");
            SaveData();
        }

        private void InvitePlayer(BasePlayer inviter, BasePlayer target)
        {
            // Проверка условий приглашения
            if (!storedData.Players.TryGetValue(inviter.userID, out var inviterData))
            {
                SendReply(inviter, "Ошибка данных игрока!");
                return;
            }

            if (!inviterData.CurrentTeamId.HasValue)
            {
                SendReply(inviter, "Вы не состоите в команде!");
                return;
            }

            var team = storedData.Teams[inviterData.CurrentTeamId.Value];
            
            if (team.Members.Count >= config.MaxTeamSize)
            {
                SendReply(inviter, "Команда заполнена!");
                return;
            }

            // Отправка приглашения
            team.PendingInvites.Add(target.userID);
            SendInviteNotification(target, team);
            SendReply(inviter, $"Приглашение отправлено {target.displayName}");
            SaveData();
        }
        #endregion

        #region UI Management
        private void UpdatePlayerUI(BasePlayer player)
        {
            var uiData = new Dictionary<string, object>
            {
                ["team"] = GetPlayerTeamData(player.userID),
                ["waitingRooms"] = storedData.WaitingRooms.Select(r => new 
                {
                    r.Id,
                    r.Name,
                    Players = r.Players.Count,
                    MaxPlayers = r.MaxPlayers
                }),
                ["notifications"] = GetPendingInvites(player.userID)
            };

            // Отправка данных через RPC
            player.SendConsoleCommand("teammanager.updateui", JsonConvert.SerializeObject(uiData));
        }

        private object GetPlayerTeamData(ulong steamId)
        {
            if (!storedData.Players.TryGetValue(steamId, out var playerData) || 
                !playerData.CurrentTeamId.HasValue)
                return null;

            return storedData.Teams.TryGetValue(playerData.CurrentTeamId.Value, out var team)
                ? new
                {
                    team.Name,
                    Members = team.Members.Select(GetPlayerName),
                    Leader = team.Members.FirstOrDefault(),
                    Stats = team.Stats
                }
                : null;
        }
        #endregion

        #region Helper Methods
        private bool IsInWaitingRoom(BasePlayer player)
        {
            return storedData.WaitingRooms.Any(r => 
                r.Players.Contains(player.userID));
        }

        private void MoveToDefaultRoom(BasePlayer player)
        {
            var defaultRoom = storedData.WaitingRooms.FirstOrDefault();
            if (defaultRoom != null)
            {
                player.Teleport(defaultRoom.Position);
                defaultRoom.Players.Add(player.userID);
            }
        }

        private void SendWelcomeMessage(BasePlayer player)
        {
            var message = new CuiElementContainer();
            // ... создание UI элементов ...
            CuiHelper.AddUi(player, message);
        }
        #endregion

        #region Data Persistence
        private void Loaded()
        {
            // Загрузка данных при старте плагина
            timer.Once(1f, () =>
            {
                if (storedData == null)
                    storedData = new StoredData();
            });
        }

        private void Unload()
        {
            // Сохранение данных при выгрузке
            SaveData();
        }
        #endregion
    }
}