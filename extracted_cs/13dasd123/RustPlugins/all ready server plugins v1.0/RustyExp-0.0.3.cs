using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;
using Oxide.Core.Libraries.Covalence;
using System.Globalization;

namespace Oxide.Plugins
{
    [Info("RustyExp", "__red", "0.0.3", ResourceId = 2406)]
    [Description("Plugin for received exp and level system")]
    class RustyExp : RustPlugin
    {
        #region Constants
        /// <summary>
        /// Возвращает строковое представление привилегии администратора.
        /// </summary>
        private const string ADMIN_PERMISSION_NAME    = "rustyexp.admin";

        /// <summary>
        /// Возвращает строковое представление привилегии мультипликатора х2
        /// </summary>
        private const string MULTIPLIER_PERMISSION_X2 = "rustyexp.multiplier_x2";

        /// <summary>
        /// Возвращает строковое представление привилегии мультипликатора х3
        /// </summary>
        private const string MULTIPLIER_PERMISSION_X3 = "rustyexp.multiplier_x3";

        /// <summary>
        /// Возвращает строковое представление привилегии мультипликатора х4
        /// </summary>
        private const string MULTIPLIER_PERMISSION_X4 = "rustyexp.multiplier_x4";
        #endregion

        #region Structs
        /// <summary>
        /// Класс. Определяющий структуру привязки данных к игроку.
        /// Структура данных - EXP
        /// </summary>
        private class ExpStruct
        {
            /// <summary>
            /// Возвращает имя игрока.
            /// </summary>
            public string PlayerName;

            /// <summary>
            /// Возвращает уникальный идентификатор игрока.
            /// </summary>
            public string PlayerSTID;

            /// <summary>
            /// Возвращает текущий уровень игрока.
            /// </summary>
            public int Level;

            /// <summary>
            /// Возвращает текущее состояние опыта для игрока.
            /// </summary>
            public int CurrentExp;

            /// <summary>
            /// Возвращает требуемое количество опыта для повышения уровня игрока.
            /// </summary>
            public int NeededExp;

            /// <summary>
            /// Конструктор по умолчанию.
            /// </summary>
            public ExpStruct()
            {
                PlayerName = "";
                PlayerSTID = "";
                Level      = -1;
                CurrentExp = -1;
                NeededExp  = -1;
            }
        }

        /// <summary>
        /// Класс. Определеяющий структуру привязки данных к плагину.
        /// Структура данных - CONFIG
        /// </summary>
        private class PluginConfig
        {
            /// <summary>
            /// Префикс плагина.
            /// Используется для отображения в чате.
            /// </summary>
            [JsonProperty("CFG_PLUGIN_PREFIX")]
            public string PluginPrefix;

            /// <summary>
            /// Цвет префика плагина.
            /// </summary>
            [JsonProperty("CFG_PLUGIN_PREFIX_COLOR")]
            public string PluginPrefixColor;

            /// <summary>
            /// Максимальный уровень игроков.
            /// </summary>
            [JsonProperty("CFG_PLUGIN_GAME_MAXLEVEL")]
            public int PluginGameMaxLevel;

            /// <summary>
            /// Количество опыта, получаемое за убийство.
            /// </summary>
            [JsonProperty("CFG_PLUGIN_GAME_EXP_FOR_KILL")]
            public int PluginGameExpForKill;

            /// <summary>
            /// Количество опыта, получаемое за добычу ресурсов.
            /// </summary>
            [JsonProperty("CFG_PLUGIN_GAME_EXP_FOR_DISPENSER")]
            public int PluginGameExpForDispenser;

            /// <summary>
            /// Количество опыта, получаемое по каждому тику таймера.
            /// </summary>
            [JsonProperty("CFG_PLUGIN_GAME_EXP_FOR_TIMER")]
            public int PluginGameExpForTimer;

            /// <summary>
            /// Количество времени в миллисекундах, для срабатывания тика таймера.
            /// </summary>
            [JsonProperty("CFG_PLUGIN_GAME_TIME_TO_EXP")]
            public int PluginGameTimeToExp;

            /// <summary>
            /// Модификатор сложности прокачки уровня.
            /// </summary>
            [JsonProperty("CFG_PLUGIN_GAME_EXP_HARD_MODIFIER")]
            public int PluginGameExpModifier;

            /// <summary>
            /// Возвращает экземпляр класса <see cref="PluginConfig"/>.
            /// Используется для создания файла конфигурации.
            /// </summary>
            /// <returns></returns>
            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    PluginPrefix = "[REXP]: ",
                    PluginPrefixColor = "#f44253",
                    PluginGameMaxLevel = 40,
                    PluginGameExpForKill = 1,
                    PluginGameExpForDispenser = 1,
                    PluginGameExpModifier = 0,
                    PluginGameTimeToExp = 36000
                };
            }
        }

        /// <summary>
        /// Класс. Инструментарий для работы с базой данных и таблицами текущего плагина.
        /// </summary>
        private class Database
        {
            /// <summary>
            /// Строка подключения по умолчанию.
            /// </summary>
            private const string CONNECTION_STRING = "server=localhost;user=root;database=rusty_division;password=pass;";

            /// <summary>
            /// Экземпляр подключения
            /// </summary>
            private Ext.MySql.Connection Connection;

            /// <summary>
            /// Экземпляр "строителя" запросов.
            /// </summary>
            private Ext.MySql.Sql Builder;

            /// <summary>
            /// Конструктор по умолчанию.
            /// </summary>
            public Database()
            {
                Connection = new Ext.MySql.Connection(CONNECTION_STRING, true);
                Builder    = new Ext.MySql.Sql();
            }

            /// <summary>
            /// Извлекает указанный запрос из БД. 
            /// </summary>
            /// <param name="sql">"сырой" запрос к БД</param>
            /// <param name="parameters">Параметры</param>
            /// <returns></returns>
            public Ext.MySql.Sql Execute(string sql, params object[] parameters)
            {
                var result = Builder.Append(sql, parameters);

                return result;
            }
        }
        #endregion

        #region GUI
        public class UI
        {
            public static CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, bool useCursor = false, string parent = "Overlay")
            {
                var NewElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = useCursor
                        },
                        new CuiElement().Parent = parent,
                        panelName
                    }
                };
                return NewElement;
            }

            public static void LoadImage(ref CuiElementContainer container, string panel, string url, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    FadeOut = 0.15f,
                    Components =
                    {
                        new CuiRawImageComponent { Url = url, FadeIn = 0.3f },
                        new CuiRectTransformComponent { AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }

            public static void CreateInput(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, bool password, int charLimit, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiInputFieldComponent { Text = text, FontSize = size, Align = align, Color = color, Command = command, IsPassword = password, CharsLimit = charLimit},
                        new CuiRectTransformComponent {AnchorMin = aMin, AnchorMax = aMax }
                    }
                });
            }

            public static void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false)
            {
                container.Add(new CuiPanel
                {
                    Image = { Color = color },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    CursorEnabled = cursor
                },
                panel);
            }

            public static void CreateText(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel);
            }

            public static void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command, FadeIn = 1.0f },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel);
            }

            public static string Color(string hexColor, float alpha)
            {
                if (hexColor.StartsWith("#"))
                {
                    hexColor = hexColor.TrimStart('#');
                }

                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
            }
        }

        private string ExpOverlayName = "RustyExpOverlay";
        private string ExpLineName    = "RustyExpLine";

        private string TextColor = GetRustColor("#ffffff", 0.8f);
        private string ClearBGColor = GetRustColor("#ffffff", 0.0f);
        private string BackgroundColor = GetRustColor("#b3b3b3", 0.05f);
        public string TextColourLearned = "#27ae60";
        public string TextColourNotLearned = "#e74c3c";
        public string HudColourLevel = "#CD7C41";

        public string Green = "#95BB42";
        public string Magenta = "800000ff";
        public string Teal = "#008080ff";

        public static string GetRustColor(string hexColor, float alpha)
        {
            if (hexColor.StartsWith("#"))
            {
                hexColor = hexColor.TrimStart('#');
            }

            int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
            return $"{(double)red / 255} {(double)green / 255} {(double)blue / 255} {alpha}";
        }

        private void UpdatePlayerUI(BasePlayer player)
        {
            string colourProgressLevel;

            if(PlayerList[player].Level > 5)
            {
                colourProgressLevel = UI.Color(Green, 1.0f);
            }
            else if(PlayerList[player].Level > 15)
            {
                colourProgressLevel = UI.Color(Magenta, 1.0f);
            }
            else
            {
                colourProgressLevel = UI.Color(Teal, 1.0f);
            }

            CuiHelper.DestroyUi(player, ExpOverlayName);

            var elementLevel = UI.CreateElementContainer(ExpOverlayName, BackgroundColor,
                0.01f + " " + 0.075f,
                0.180f + " " + 0.1);

            float maxValue = float.Parse("0." + PlayerList[player].NeededExp);
            float maxRange = 0.80f - maxValue;

            UI.CreatePanel(ref elementLevel, ExpOverlayName, colourProgressLevel, "0.01 0.13",
                 GetPartFromMaxInt(player, PlayerList[player].NeededExp, PlayerList[player].CurrentExp) + " 0.80"); //+ (PlayerList[player].NeededExp - PlayerList[player].CurrentExp) +
            UI.CreateText(ref elementLevel, ExpOverlayName, TextColor, $"Уровень: {PlayerList[player].Level} ({PlayerList[player].CurrentExp} из {PlayerList[player].NeededExp} EXP)", 12, "0.200 0.0", "1.0 1.0", TextAnchor.MiddleLeft);

            CuiHelper.AddUi(player, elementLevel);
        }

        private string GetPartFromMaxInt(BasePlayer player, int max, int current)
        {
            /*
            decimal result = (current * 100M) / max;
            string res = $"0.{(int)result}";
            */

            return $"0.{(int)((current * 100M) / max)}";
        }

        private string GetExpInfo(BasePlayer player)
        {
            var pl = PlayerList[player];

            return string.Format("Уровень: {0} ({1} из {2} EXP)", pl.Level, pl.CurrentExp, pl.NeededExp);
        }
        #endregion

        #region Variables
        /// <summary>
        /// Словарь. Храняший в себе информацию об опыте всех игроков.
        /// </summary>
        private Dictionary<BasePlayer, ExpStruct> PlayerList;

        /// <summary>
        /// Словарь. Хранит информацию о запущенных таймерах игроков.
        /// </summary>
        private Dictionary<BasePlayer, Timer> PlayersTimer;

        /// <summary>
        /// Экземпляр класса <see cref="PluginConfig"/>
        /// для взаимодействия с конфигурацией плагина.
        /// </summary>
        private PluginConfig Cfg;

        /// <summary>
        /// Экземпляр класса <see cref="Database"/>
        /// для взаимодействия с базой данных плагина.
        /// </summary>
        private Database DbContext;
        #endregion

        #region Initialization
        /// <summary>
        /// Конструктор по умолчанию.
        /// Инициализирует объекты плагина.
        /// </summary>
        public RustyExp()
        {
            PlayerList         = new Dictionary<BasePlayer, ExpStruct>();
            PlayersTimer       = new Dictionary<BasePlayer, Timer>();
            DbContext          = new Database();

            RegisterPermissions();
        }

        /// <summary>
        /// Метод инициализации.
        /// Регистрирует привилегии для плагина.
        /// </summary>
        private void RegisterPermissions()
        {
            permission.RegisterPermission(ADMIN_PERMISSION_NAME, this);
            permission.RegisterPermission(MULTIPLIER_PERMISSION_X2, this);
            permission.RegisterPermission(MULTIPLIER_PERMISSION_X3, this);
            permission.RegisterPermission(MULTIPLIER_PERMISSION_X4, this);
        }
        #endregion

        #region Hooks
        void Loaded()
        {
            LoadAllPlayers();
        }

        void Unload()
        {
            SaveAllPlayers();
        }

        /// <summary>
        /// Обработчик события.
        /// Происходит при иницализации игрока.
        /// </summary>
        /// <param name="player">Экземпляр игрока</param>
        void OnPlayerInit(BasePlayer player)
        {
            if (PlayerList.ContainsKey(player))
            {
                SavePlayer(player);
                ClearPlayerData(player);
            }

            LoadPlayer(player);
            InitiateTimer(player);
        }

        /// <summary>
        /// Обработчик события.
        /// Происходит, когда игрок покидает игровой сервер.
        /// </summary>
        /// <param name="player">Экземпляр игрока</param>
        /// <param name="reason">Причина выхода</param>
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if(PlayerList.ContainsKey(player))
            {
                SavePlayer(player);
                ClearPlayerData(player);
            }
        }

        /// <summary>
        /// Обработчик события.
        /// Происходит, когда игрок умирает.
        /// </summary>
        /// <param name="player">Экземпляр игрока</param>
        /// <param name="info">Информация об убийстве</param>
        /// <returns></returns>
        object OnPlayerDie(BasePlayer player, HitInfo info)
        {
            if(!(info.InitiatorPlayer is BasePlayer))
            {
                return null;
            }

            BasePlayer killer = info.InitiatorPlayer;
            if(killer is BasePlayer)
            {
                try
                {
                    AddExpToPlayer(killer, Cfg.PluginGameExpForKill);
                }
                catch (Exception ex)
                {
                    Interface.Oxide.LogException("[REXP->Exception]: ", ex);
                }
            }

            return null;
        }

        /// <summary>
        /// Обработчик события.
        /// Происходит, когда игрок добывает ресурс.
        /// </summary>
        /// <param name="dispenser">Ресурс</param>
        /// <param name="player">Экземпляр игрока</param>
        /// <param name="item">Экземпляр получаемой вещи.</param>
        /// <returns></returns>
        object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            AddExpToPlayer(player, Cfg.PluginGameExpForDispenser);

            return null;
        }

        object OnPlayerChat(ConsoleSystem.Arg arg)
        {
            rust.BroadcastChat($"<color=#a52a2aff>[Lvl. {PlayerList[arg.Player()].Level}]</color> " + arg.Player().displayName, arg.Args[0]);

            return false;
        }
        #endregion

        #region Instruments
        private void ClearAllPlayersData()
        {
            PlayersTimer = null;
            PlayersTimer = new Dictionary<BasePlayer, Timer>();

            PlayerList = null;
            PlayerList = new Dictionary<BasePlayer, ExpStruct>();
        }

        private void SaveAllPlayers()
        {
            if (PlayerList.Count > 0)
            {
                foreach (var player in PlayerList)
                {
                    SavePlayer(player.Key);
                }
            }

            ClearAllPlayersData();
        }

        private void LoadAllPlayers()
        {
            foreach(var player in BasePlayer.activePlayerList)
            {
                LoadPlayer(player);
                InitiateTimer(player);
            }
        }

        /// <summary>
        /// Очищает данные игрока в хранилище текущего плагина.
        /// </summary>
        /// <param name="player"></param>
        private void ClearPlayerData(BasePlayer player)
        {
            if (PlayersTimer.ContainsKey(player))
            {
                PlayersTimer[player].Destroy();
                PlayersTimer[player] = null; // таймеры раста это пиздец

                PlayersTimer.Remove(player);
            }

            if (PlayerList.ContainsKey(player))
            {
                PlayerList.Remove(player);
            }
        }

        /// <summary>
        /// Метод инициализации таймера для указанного игрока.
        /// </summary>
        /// <param name="player">Экземпляр игрока</param>
        private void InitiateTimer(BasePlayer player)
        {
            if(PlayersTimer.ContainsKey(player))
            {
                return;
            }
            else
            {
                PlayersTimer.Add(player, timer.Every(Cfg.PluginGameTimeToExp, () =>
                {
                    TimerTick(player);
                }));
            }
        }

        /// <summary>
        /// Происходит по истечении заданного интервала времени в главном таймере.
        /// </summary>
        /// <param name="player">Экземпляр игрока</param>
        private void TimerTick(BasePlayer player)
        {
            if(PlayerList.ContainsKey(player))
            {
                AddExpToPlayer(player, Cfg.PluginGameExpForTimer);
            }
        }
        #endregion

        #region Localization
        /// <summary>
        /// Переопределенный метод.
        /// Метод локализации.
        /// Регистрирует строковые представления ответов плагина.
        /// </summary>
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["gui-exp-line-description"]        = "Уровень: {0} (EXP: {1}|{2})",
                ["system-create-default-cfg-start"] = "Создание нового файла конфигурации...",
                ["system-create-default-cfg-stop"]  = "Файл конфигурации успешно создан",
                ["welcome-message"]                 = "Запущен плагин RustyExp. Author: __red",
                ["new-level-info"]                  = "Поздравляем, вы получили новый уровень: {0}",
                ["new-level-broadcast"]             = "Игрок {0} получил уровень {1}",
                ["max-level-broadcast"]             = "Игрок {0} получил максимальный уровень",
                ["exp-earned"]                      = "Вы получили {0} EXP. До {} уровня осталось {2} EXP",
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["gui-exp-line-description"]        = "Level: {0} (EXP: {1}|{2})",
                ["system-create-default-cfg-start"] = "Creating new configuration file...",
                ["system-create-default-cfg-stop"]  = "Configurating file sucessfully created",
                ["welcome-message"]                 = "RustyExp plugin has been started. Author: __red",
                ["new-level-info"]                  = "You upped a new level {0}, congratulations",
                ["new-level-broadcast"]             = "Player {0} upped a new level {1}",
                ["max-level-broadcast"]             = "Player {0} upped give a maximus !",
                ["exp-earned"]                      = "You earned {0} EXP. Needed {1} EXP for {2} level",
            }, this, "en");
        }
        #endregion

        #region Config
        /// <summary>
        /// Происходит при первом запуске плагина или при удалении файла конфигурации плагина.
        /// Создает экземпляр класса-конфигуратора по умолчанию.
        /// </summary>
        protected override void LoadDefaultConfig()
        {
            PrintWarning(lang.GetMessage("system-create-default-cfg-start", this));

            Cfg = PluginConfig.DefaultConfig();
        }

        /// <summary>
        /// Переопределенный метод.
        /// Загружает состояние конфигурации для текущего плагина.
        /// </summary>
        protected override void LoadConfig()
        {
            base.LoadConfig();

            Cfg = Config.ReadObject<PluginConfig>();
        }

        /// <summary>
        /// Переопределенный метод.
        /// Записывает текущее состояние конфигурации плагина.
        /// </summary>
        protected override void SaveConfig()
        {
            Config.WriteObject(Cfg);
        }
        #endregion

        #region Player Instruments
        /// <summary>
        /// Сохраняет текущее состояние структуры <see cref="ExpStruct"/> 
        /// указанного игрока.
        /// </summary>
        /// <param name="player">Экземпляр игрока</param>
        private void SavePlayer(BasePlayer player)
        {
            //Database actions (future)

            if(PlayerList.ContainsKey(player))
            {
                Interface.Oxide.DataFileSystem.WriteObject("RustyExp\\" + player.displayName, PlayerList[player]);
            }
        }

        /// <summary>
        /// Загружает состояние <see cref="ExpStruct"/>
        /// для указанного игрока.
        /// </summary>
        /// <param name="player">Экземпляр игрока</param>
        private void LoadPlayer(BasePlayer player)
        {
            //Database actions (future)

            ExpStruct data = new ExpStruct();
            data = Interface.Oxide.DataFileSystem.ReadObject<ExpStruct>("RustyExp\\" + player.displayName);

            if(data.Level == -1)
            {
                data = new ExpStruct()
                {
                    Level = 1,
                    PlayerName = player.displayName,
                    PlayerSTID = player.UserIDString,
                    CurrentExp = 0,
                    NeededExp = GetNeededExp(player, 1),
                };
            }

            if (!PlayerList.ContainsKey(player))
            {
                PlayerList.Add(player, data);
            }

            UpdatePlayerUI(player);
        }

        /// <summary>
        /// Добавляет опыт указанному игроку.
        /// Просчитывает возможное владение привилегиями для мультипликации
        /// получаемого опыта.
        /// Вызывает калькуляцию.
        /// </summary>
        /// <param name="player">Экземпляр игрока</param>
        /// <param name="gived">Получаемое количество опыта</param>
        private void AddExpToPlayer(BasePlayer player, int gived)
        {
            int newGived = gived;

            if(permission.UserHasPermission(player.UserIDString, MULTIPLIER_PERMISSION_X2))
            {
                newGived += gived * 2;
            }

            if (permission.UserHasPermission(player.UserIDString, MULTIPLIER_PERMISSION_X3))
            {
                newGived += gived * 3;
            }

            if (permission.UserHasPermission(player.UserIDString, MULTIPLIER_PERMISSION_X4))
            {
                newGived += gived * 4;
            }

            if(!PlayerList.ContainsKey(player))
            {
                LoadPlayer(player);
            }

            PlayerList[player].CurrentExp += newGived;

            //string msg = string.Format("Вы получили <color=#ff0000ff>{0}</color> EXP. До <color=#008080ff>{1}</color> уровня осталось <color=#ff0000ff>{2}</color> EXP", newGived, PlayerList[player].Level + 1, PlayerList[player].NeededExp - PlayerList[player].CurrentExp);
            //ToChat(player, msg);

            CalculateExp(player);
        }

        /// <summary>
        /// Получает цифрофое представление о требуемом количестве
        /// опыта для получения следующего уровеня.
        /// Просчет происходит по формуле:
        /// ((player.level + 1) + player.level) + cfg.exp_mod
        /// </summary>
        /// <param name="player">Экземпляр игрока</param>
        /// <returns></returns>
        private int GetNeededExp(BasePlayer player, int currentlevel)
        {
            if(PlayerList.ContainsKey(player))
            {
                return ((PlayerList[player].Level + 1) + PlayerList[player].Level) + Cfg.PluginGameExpModifier;
            }
            else
            {
                return ((currentlevel + 1) + currentlevel) + Cfg.PluginGameExpModifier;
            }
        }

        /// <summary>
        /// Калькулирует состояние опыта игрока.
        /// Если текущего опыта больше, чем нужно опыта для повышения уровеня, то
        /// итерирует уровень указанного игрока.
        /// </summary>
        /// <param name="player">Экземпляр игрока</param>
        private void CalculateExp(BasePlayer player)
        {
            if(PlayerList[player].CurrentExp >= PlayerList[player].NeededExp)
            {
                IterLevel(player);
            }

            UpdatePlayerUI(player);
        }
        #endregion

        #region API
        /// <summary>
        /// Устанавливает указанному игроку значение уровня
        /// </summary>
        /// <param name="player">Объект игрока</param>
        /// <param name="level">Уровень для установки</param>
        public void SetLevel(BasePlayer player, int level)
        {
            if(PlayerList.ContainsKey(player))
            {
                if (PlayerList[player].Level == Cfg.PluginGameMaxLevel)
                {
                    return;
                }

                PlayerList[player].Level = level;
                PlayerList[player].CurrentExp = 0;
                PlayerList[player].NeededExp = GetNeededExp(player, level);

                string msg = string.Format(lang.GetMessage("new-level-info", this), PlayerList[player].Level);

                ToChat(player, msg);
            }
        }

        /// <summary>
        /// Добавляет игроку указанное количество опыта.
        /// После выдачи результат калькулируется.
        /// </summary>
        /// <param name="player">Экземпляр игрока</param>
        /// <param name="exp">Количество опыта</param>
        public void SetExp(BasePlayer player, int exp)
        {
            if(PlayerList.ContainsKey(player))
            {
                if(PlayerList[player].Level == Cfg.PluginGameMaxLevel)
                {
                    return;
                }

                AddExpToPlayer(player, exp);
            }
        }

        /// <summary>
        /// Итерирует указанному игроку уровень (+1)
        /// </summary>
        /// <param name="player">Экземпляр игрока для итерирования</param>
        public void IterLevel(BasePlayer player)
        {
            if(PlayerList.ContainsKey(player))
            {
                if(PlayerList[player].Level == Cfg.PluginGameMaxLevel)
                {
                    return;
                }

                string msg = "";

                PlayerList[player].Level++;
                PlayerList[player].CurrentExp = 0;
                PlayerList[player].NeededExp = GetNeededExp(player, PlayerList[player].Level);

                if (PlayerList[player].Level == Cfg.PluginGameMaxLevel)
                {
                    string broadcast = string.Format("Игрок: <color=#ff00ffff>{0}</color> получил максимальный уровень !", PlayerList[player].PlayerName);

                    ToBroadcast(broadcast);
                }

                msg = string.Format("Вы получили новый уровень: <color=#008080ff>{0}</color>", PlayerList[player].Level);
                ToChat(player, msg);
            }
        }

        /// <summary>
        /// Возвращает числовое представление о состоянии текущего уровня для указанного персонажа
        /// </summary>
        /// <param name="player">Экземпляр игрока</param>
        /// <returns></returns>
        public int GetPlayerLevel(BasePlayer player)
        {
            if(PlayerList.ContainsKey(player))
            {
                return PlayerList[player].Level;
            }
            else
            {
                return -1;
            }
        }
        #endregion

        #region Misc
        /// <summary>
        /// Отправляет сообщение в чат игроку.
        /// </summary>
        /// <param name="player">Экземпляр игрока</param>
        /// <param name="message">Сообщение</param>
        public void ToChat(BasePlayer player, string message)
        {
            rust.SendChatMessage(player, "", $"<color=#ffa500ff>{Cfg.PluginPrefix}</color>{message}");
        }

        /// <summary>
        /// Отправляет серверное оповещение.
        /// </summary>
        /// <param name="message">Строка для оповещения</param>
        public void ToBroadcast(string message)
        {
            rust.BroadcastChat("", $"<color=#ffa500ff>{Cfg.PluginPrefix}</color>{message}");
        }
        #endregion

        #region Commands
        [ChatCommand("exp")]
        void ExpCommand(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;

            UpdatePlayerUI(player);
        }
        #endregion
    }
}