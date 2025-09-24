using Oxide.Core;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("TeamManager", "YourName", "1.0.0")]
    [Description("Система управления командами с лимитом игроков и GUI-меню")]
    public class TeamManager : RustPlugin
    {
        #region Конфигурация

        private PluginConfig config;

        private class PluginConfig
        {
            [JsonProperty("Максимум игроков в команде")]
            public int MaxPlayersPerTeam { get; set; } = 10;

            [JsonProperty("Команды")]
            public List<Team> Teams { get; set; } = new List<Team>();

            [JsonProperty("Настройки интерфейса")]
            public UISettings UISettings { get; set; } = new UISettings();
        }

        private class Team
        {
            [JsonProperty("Название")]
            public string Name { get; set; }

            [JsonProperty("Цвет")]
            public string Color { get; set; }

            [JsonProperty("Точка спавна")]
            public Vector3 SpawnPoint { get; set; }
        }

        private class UISettings
        {
            [JsonProperty("Цвет фона")]
            public string BackgroundColor { get; set; } = "0.2 0.2 0.2 0.9";

            [JsonProperty("Цвет текста")]
            public string TextColor { get; set; } = "#FFFFFF";

            [JsonProperty("Размер текста")]
            public int TextSize { get; set; } = 14;
        }

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig
            {
                Teams = new List<Team>
                {
                    new Team { Name = "Красные", Color = "#FF0000", SpawnPoint = new Vector3(100f, 50f, 200f) },
                    new Team { Name = "Синие", Color = "#0000FF", SpawnPoint = new Vector3(300f, 50f, 400f) },
                    new Team { Name = "Зеленые", Color = "#00FF00", SpawnPoint = new Vector3(200f, 50f, 100f) },
                    new Team { Name = "Желтые", Color = "#FFFF00", SpawnPoint = new Vector3(400f, 50f, 300f) }
                }
            };
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
            if (config == null) LoadDefaultConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Данные

        private Dictionary<string, string> playerTeams = new Dictionary<string, string>();
        private const string TeamMenuName = "TeamSelectionMenu";

        #endregion

        #region Инициализация

        void Init()
        {
            permission.RegisterPermission("teammanager.use", this);
            cmd.AddChatCommand("team", this, "TeamCommand");
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, TeamMenuName);
            }
        }

        #endregion

        #region Логика команд

        void TeamCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "teammanager.use"))
            {
                SendReply(player, "У вас нет прав на использование этой команды");
                return;
            }

            ShowTeamMenu(player);
        }

        private void JoinTeam(BasePlayer player, string teamName)
        {
            // Проверка лимита команды
            int teamCount = playerTeams.Count(p => p.Value == teamName);
            if (teamCount >= config.MaxPlayersPerTeam)
            {
                SendReply(player, $"Команда {teamName} уже заполнена (максимум {config.MaxPlayersPerTeam} игроков)");
                return;
            }

            // Телепортация на спавн команды
            var team = config.Teams.FirstOrDefault(t => t.Name == teamName);
            if (team == null) return;

            player.Teleport(team.SpawnPoint);
            playerTeams[player.UserIDString] = teamName;
            SendReply(player, $"Вы присоединились к команде {teamName}!");

            // Обновляем интерфейс у всех игроков
            foreach (var p in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(p, TeamMenuName);
                ShowTeamMenu(p);
            }
        }

        #endregion

        #region Пользовательский интерфейс

        private void ShowTeamMenu(BasePlayer player)
        {
            var container = new CuiElementContainer();

            // Основная панель
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.7" },
                Image = { Color = config.UISettings.BackgroundColor }
            }, "Hud", TeamMenuName);

            // Заголовок
            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.1 0.8", AnchorMax = "0.9 0.9" },
                Text = { Text = "Выберите команду", FontSize = config.UISettings.TextSize, Align = TextAnchor.MiddleCenter, Color = config.UISettings.TextColor }
            }, TeamMenuName);

            // Кнопки команд
            float buttonY = 0.6f;
            foreach (var team in config.Teams)
            {
                int teamCount = playerTeams.Count(p => p.Value == team.Name);
                string buttonText = $"{team.Name} ({teamCount}/{config.MaxPlayersPerTeam})";

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0.1 {buttonY}", AnchorMax = $"0.9 {buttonY + 0.1}" },
                    Button = { Color = team.Color, Command = $"team.join {team.Name}" },
                    Text = { Text = buttonText, FontSize = config.UISettings.TextSize, Align = TextAnchor.MiddleCenter, Color = config.UISettings.TextColor }
                }, TeamMenuName);

                buttonY -= 0.12f;
            }

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("team.join")]
        private void TeamJoinCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return;

            if (arg.Args == null || arg.Args.Length == 0) return;
            
            JoinTeam(player, arg.Args[0]);
        }

        #endregion
    }
}