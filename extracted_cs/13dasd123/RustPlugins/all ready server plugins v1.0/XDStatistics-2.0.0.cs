using UnityEngine;
using System;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("XDStatistics", "https://topplugin.ru/", "2.0.0")]
    [Description("Статистика для вашего сервера:3")]
    class XDStatistics : RustPlugin
    {
        #region Vars

        [PluginReference] Plugin ImageLibrary, OreBonus, Friends, Clans, Duel, Battles, IQEconomic, IQFakeActive;
        readonly string perm = "XDStatistics.inkognito";
        public enum BG
        {
            blur,
            IMG
        }

        public string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary?.Call("GetImage", shortname, skin);
        public bool AddImage(string url, string shortname, ulong skin = 0) => (bool)ImageLibrary?.Call("AddImage", url, shortname, skin);
        public bool HasImage(string imageName) => (bool)ImageLibrary?.Call("HasImage", imageName);
        public void SendImage(BasePlayer player, string imageName, ulong imageId = 0) => ImageLibrary?.Call("SendImage", player, imageName, imageId);

        #endregion

        #region Data

        class PlayerData
        {
            public Pvp pvp;
            public Other other;
            public Gather gather;
            public int PlayedTimeMin = 0;
            public string nick = "";
            public bool inkognito = true;
            public PlayerData() { }
        }

        class Other
        {
            public int exsplosive = 0;
            public int economic = 0;

            public Other() { }
        }

        class Gather
        {
            public int stone = 0;
            public int wood = 0;
            public int metall = 0;
            public int sulfur = 0;
            public int hqm = 0;
            public int radore = 0;
            public int allresource = 0;
            public Gather() { }
        }
        class Pvp
        {
            public int kill = 0;
            public int death = 0;
            public int headshot = 0;
            public int npc = 0;
            public Pvp() { }
        }

        class StoredData
        {
            public Dictionary<ulong, PlayerData> players = new Dictionary<ulong, PlayerData>();
            public List<ulong> playerignore = new List<ulong>();
        }

        void SaveData()
        {
            StatData.WriteObject(storedData);
        }

        void LoadData()
        {
            string resultName = this.Name + $"/Stats";

            StatData = Interface.Oxide.DataFileSystem.GetFile(resultName);
            try
            {
                storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(resultName);
            }
            catch
            {
                storedData = new StoredData();
            }
        }

        StoredData storedData;
        private DynamicConfigFile StatData;

        #endregion

        #region Config

        private static Configuration config = new Configuration();
        private class Configuration
        {
            [JsonProperty("Настройка интерфейса плагина")]
            public Intarface intarface;

            [JsonProperty("Основные настройки плагина")]
            public Setings setings;

            internal class Setings
            {
                [JsonProperty("Скрытие статистики по премешену(true - без пермешена не скроешь. false - Скрытие доступно всем игрокам)")]
                public bool statInkognito;
                [JsonProperty("Отчищать статистику игроков при вайпе ?")]
                public bool dataClear;
                [JsonProperty("Команда для открытия статистики")]
                public string commandOpenStat;
                [JsonProperty("Использовать поддержку IQFakeActive")]
                public bool UseIQFakeActive;
                [JsonProperty("Сохранять дату при каждом сохранении сервера? (Рекомендуется ставить в крайних случаях, если не сохраняется дата)")]
                public bool useServerSave;
            }

            internal class Intarface
            {
                [JsonProperty("Тип фона 0 - Блюр, 1 - Картинка ")]
                public BG bG;
                [JsonProperty("Цвет блюра hex (Если тип фона 0)")]
                public string colorBlur;

                [JsonProperty("Цвет задних панелей 'Топ 10 лучших игроков' hex")]
                public string colorTopTen;

                [JsonProperty("Иконки (Названия не менять! Только ссылки)")]
                public Dictionary<string, string> IconStats;
            }

            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    intarface = new Intarface
                    {
                        bG = BG.IMG,
                        colorBlur = "#5B77E4A2",
                        colorTopTen = "#767676A0",
                        IconStats = new Dictionary<string, string>
                        {
                            { "Head", "https://i.imgur.com/hB5PeGM.png" },
                            { "Rip", "https://i.imgur.com/uhfywvv.png" },
                            { "Gather", "https://i.imgur.com/FNIYgEF.png" },
                            { "Kill", "https://i.imgur.com/ZNVS80J.png" },
                            { "FonUI", "https://i.imgur.com/BEpvMN7.png" },
                            { "Time", "https://i.imgur.com/eaPoPdf.png" },
                            { "TopTime", "https://i.imgur.com/mi2iuhy.png" },
                            { "Economic", "https://i.imgur.com/AlYGP18.png" },
                            { "Killer", "https://i.imgur.com/KcMC9qj.png" },
                            { "Exsplosive", "https://i.imgur.com/RMbLnp3.png" },
                            { "Farmila", "https://i.imgur.com/Q7zpl4A.png" },
                            { "NpcKill", "https://i.imgur.com/49bl3hv.png" },
                        }
                    },
                    setings = new Setings
                    {
                        statInkognito = true,
                        dataClear = true,
                        commandOpenStat = "stat",
                        UseIQFakeActive = false,
                        useServerSave = false,
                    }
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintWarning("Ошибка #153" + $"чтения конфигурации 'oxide/config/{Name}', создаём новую конфигурацию!!");
                LoadDefaultConfig();
            }
            if (config.intarface.colorTopTen == null) config.intarface.colorTopTen = "#767676A0";
            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig() => config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(config);

        #endregion

        #region Lang
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["XDStatistics_TITLE"] = "<b>STATISTICS</b>",
                ["XDStatistics_TITLETWO"] = "<b>You can find out your statistics and the statistics of other players.</b>",
                ["XDStatistics_EXITSTAT"] = "<b>Leave page</b>",
                ["XDStatistics_PLAYERLISTBUTTON"] = "<b>View player list</b>",
                ["XDStatistics_GATHER_STONE"] = "Quarried stone:",
                ["XDStatistics_GATHER_WOOD"] = "Prey tree:",
                ["XDStatistics_GATHER_SULFUR"] = "Sulfur produced:",
                ["XDStatistics_GATHER_METALL"] = "Mined metal:",
                ["XDStatistics_GATHER_HQM"] = "Mvc produced:",
                ["XDStatistics_GATHER_RADORE"] = "Radioactive ore:",
                ["XDStatistics_INKOGNITOOFF"] = "Hide your stats",
                ["XDStatistics_INKOGNITOON"] = "Show statistics to all players",
                ["XDStatistics_MYSTATBUTTON"] = "<b>My stats</b>",
                ["XDStatistics_TITLEtop"] = "<b>Top 5 best players</b>",
                ["XDStatistics_TITLEtopTen"] = "<b>Top 10 best players in each category</b>",
                ["XDStatistics_TITLEEconomic"] = "<b>Top 10 rich</b>",
                ["XDStatistics_TITLENpcKill"] = "<b>Top 10 killer scientists</b>",
                ["XDStatistics_TITLEPlayerKill"] = "<b>Top 10 player killers</b>",
                ["XDStatistics_TITLETime"] = "<b>Top 10 centenarians</b>",
                ["XDStatistics_TITLEExsplosive"] = "<b>Top 10 Demolitionists</b>",
                ["XDStatistics_TITLEFarmila"] = "<b>Top 10 farm</b>",
                ["XDStatistics_TITLEWarning"] = "*If a player has a lock open, then you will be redirected to his personal statistics on use for his nickname",


            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["XDStatistics_TITLE"] = "<b>СТАТИСТИКА</b>",
                ["XDStatistics_TITLETWO"] = "<b>Вы можете узнать свою статистику и статистику других игроков.</b>",
                ["XDStatistics_EXITSTAT"] = "<b>Покинуть страницу</b>",
                ["XDStatistics_PLAYERLISTBUTTON"] = "<b>Посмотреть список игроков</b>",
                ["XDStatistics_GATHER_STONE"] = "Добыто камня:",
                ["XDStatistics_GATHER_WOOD"] = "Добыто дерева:",
                ["XDStatistics_GATHER_SULFUR"] = "Добыто серы:",
                ["XDStatistics_GATHER_METALL"] = "Добыто метала:",
                ["XDStatistics_GATHER_HQM"] = "Добыто мвк:",
                ["XDStatistics_GATHER_RADORE"] = "Добыто радиоактивной руды:",
                ["XDStatistics_INKOGNITOOFF"] = "Срыть свою статистику",
                ["XDStatistics_INKOGNITOON"] = "Показывать статистику всем игрокам",
                ["XDStatistics_MYSTATBUTTON"] = "<b>Моя статистика</b>",
                ["XDStatistics_TITLEtop"] = "<b>Топ 5 лучших игроков</b>",
                ["XDStatistics_TITLEtopTen"] = "<b>Топ 10 лучших игроков в каждой категории</b>",
                ["XDStatistics_TITLEEconomic"] = "<b>Топ 10 богачей</b>",
                ["XDStatistics_TITLENpcKill"] = "<b>Топ 10 убийц ученых</b>",
                ["XDStatistics_TITLEPlayerKill"] = "<b>Топ 10 убийц игроков</b>",
                ["XDStatistics_TITLETime"] = "<b>Топ 10 долгожителей</b>",
                ["XDStatistics_TITLEExsplosive"] = "<b>Топ 10 подрывников</b>",
                ["XDStatistics_TITLEFarmila"] = "<b>Топ 10 фармил</b>",
                ["XDStatistics_TITLEWarning"] = "*Если у игрока замочек открыт, то вы можете перейти на его личную статистику по нажатию на его ник",

            }, this, "ru");
        }

        #endregion

        #region UI    
        public static string UI_INTERFACE = "INTERFACE_STATS";
        public static string UI_INTERFACE_PLAYERSTAT = "UI_INTERFACE_PLAYERSTAT";
        public static string UI_INTERFACE_PLAYERLIST = "UI_INTERFACE_PLAYERLIST";
        public static string UI_INTERFACE_TOPUSERSERVERS = "UI_INTERFACE_TOPUSERSERVERS";
        public static string UI_INTERFACE_TopTenUserServer = "UI_INTERFACE_TopTenUserServer";

        void openui(BasePlayer p)
        {
            Ui_Stat(p);
        }

        public void Ui_Stat(BasePlayer player, ulong target = 0)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_INTERFACE);

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                Image = { Color = "0 0 0 0" }
            }, "Overlay", UI_INTERFACE);

            if (config.intarface.bG == BG.IMG)
            {
                container.Add(new CuiElement
                {
                    Parent = UI_INTERFACE,
                    Components =
                    {
                    new CuiRawImageComponent { Png = GetImage("FonUI"), Color = HexToRustFormat("#FFFFFFFF") },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"},
                    }
                });
            }
            else
            {
                container.Add(new CuiElement
                {
                    Parent = UI_INTERFACE,
                    Components =
                    {
                    new CuiImageComponent {Material = "assets/content/ui/uibackgroundblur.mat", Color = HexToRustFormat(config.intarface.colorBlur) },
                    new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "1 1"},
                    }
                });
            }

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE,
                Components =
                    {
                        new CuiTextComponent { Text = lang.GetMessage("XDStatistics_TITLE", this, player.userID.ToString()), Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFFF"), FontSize = 38 },
                        new CuiRectTransformComponent{ AnchorMin = "0.3750013 0.8694", AnchorMax = "0.6088542 0.9425805"},
                        new CuiOutlineComponent{ Color = HexToRustFormat("#000000FF"), Distance = "0.3 -0.3", UseGraphicAlpha = false }
                    }
            });

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE,
                Components =
                    {
                        new CuiTextComponent { Text = lang.GetMessage("XDStatistics_TITLETWO", this, player.userID.ToString()), Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFFF"), FontSize = 17 },
                        new CuiRectTransformComponent{ AnchorMin = "0.2671881 0.8314802", AnchorMax = "0.7177087 0.8777773"},
                        new CuiOutlineComponent{ Color = HexToRustFormat("#000000FF"), Distance = "0.3 -0.3", UseGraphicAlpha = false }
                    }
            });

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.8828125 0", AnchorMax = "1 0.05185185" },
                Button = { Close = UI_INTERFACE, Color = "0 0 0 0" },
                Text = { Text = lang.GetMessage("XDStatistics_EXITSTAT", this, player.userID.ToString()), FontSize = 15, Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5557265 0", AnchorMax = "0.7354206 0.05277777" },
                Button = { Command = "UI_HandlerStat Golistplayer", Color = "0 0 0 0" },
                Text = { Text = lang.GetMessage("XDStatistics_PLAYERLISTBUTTON", this, player.userID.ToString()), FontSize = 15, Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE);

            CuiHelper.AddUi(player, container);
            PlayerStat(player);
        }

        public void PlayerStat(BasePlayer player, ulong target = 0)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_INTERFACE_PLAYERSTAT);
            CuiHelper.DestroyUi(player, UI_INTERFACE_PLAYERLIST);
            CuiHelper.DestroyUi(player, UI_INTERFACE_TOPUSERSERVERS);
            CuiHelper.DestroyUi(player, UI_INTERFACE_TopTenUserServer);
            CuiHelper.DestroyUi(player, "MyStatButton");
            CuiHelper.DestroyUi(player, "topuserservers");
            ulong Stat = target == 0 ? player.userID : target;

            string ImageAvatar = GetImage(Stat.ToString());
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.2932296 0.4046296", AnchorMax = "0.8317714 0.7351853" },
                Image = { Color = "0 0 0 0" }
            }, UI_INTERFACE, UI_INTERFACE_PLAYERSTAT);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.1723958 0.05277777" },
                Button = { Command = "UI_HandlerStat topuser", Color = "0 0 0 0" },
                Text = { Text = lang.GetMessage("XDStatistics_TITLEtop", this, player.userID.ToString()), FontSize = 15, Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE, "topuserserver");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.2447931 0", AnchorMax = "0.4390625 0.05277777" },
                Button = { Command = "UI_HandlerStat GoTopTenPlayer", Color = "0 0 0 0" },
                Text = { Text = lang.GetMessage("XDStatistics_TITLEtopTen", this, player.userID.ToString()), FontSize = 15, Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE, "topuserservers");

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_PLAYERSTAT,
                Components = {
                    new CuiRawImageComponent {
                        Png = ImageAvatar,
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.005767672 0.4171677",
                        AnchorMax = "0.1991913 0.9773858"
                    },
                }
            });
            var users = storedData.players[Stat];
            string nick = player.userID == Stat ? player.displayName : users.nick;
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.005767672 0.2747308", AnchorMax = "0.1991913 0.3941599" },
                Button = { Color = HexToRustFormat("#2dd199") },
                Text = { Text = nick, Align = TextAnchor.MiddleCenter, FontSize = 17 }
            }, UI_INTERFACE_PLAYERSTAT);

            #region SkillUser

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_PLAYERSTAT,
                Components = {
                    new CuiRawImageComponent {
                        Png = GetImage("Head"),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.2125244 0.8063986",
                        AnchorMax = "0.2608804 0.9464546"
                    },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.2659575 0.8179268", AnchorMax = "0.4690514 0.915966" },
                Text = { Text = $" - {users.pvp.headshot}", FontSize = 17, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_INTERFACE_PLAYERSTAT);

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_PLAYERSTAT,
                Components = {
                    new CuiRawImageComponent {
                        Png = GetImage("Kill"),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.2125244 0.6467347",
                        AnchorMax = "0.2608804 0.7867907"
                    },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.2659575 0.66386", AnchorMax = "0.4690514 0.7619044" },
                Text = { Text = $" - {users.pvp.kill}", FontSize = 17, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_INTERFACE_PLAYERSTAT);

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_PLAYERSTAT,
                Components = {
                    new CuiRawImageComponent {
                        Png = GetImage("Gather"),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.2125244 0.4730653",
                        AnchorMax = "0.2608804 0.6131213"
                    },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.2659575 0.4873946", AnchorMax = "0.4690514 0.5854338" },
                Text = { Text = $" - {users.gather.allresource}", FontSize = 17, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_INTERFACE_PLAYERSTAT);

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_PLAYERSTAT,
                Components = {
                    new CuiRawImageComponent {
                        Png = GetImage("Rip"),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.2125244 0.2993958",
                        AnchorMax = "0.2608804 0.4394518"
                    },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.2659575 0.31932", AnchorMax = "0.4690514 0.4173666" },/////////////
                Text = { Text = $" - {users.pvp.death}", FontSize = 17, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_INTERFACE_PLAYERSTAT);

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_PLAYERSTAT,
                Components = {
                    new CuiRawImageComponent {
                        Png = GetImage("Time"),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.2125244 0.1313286",
                        AnchorMax = "0.2608804 0.2713846"
                    },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.2659575 0.1512602", AnchorMax = "0.4690514 0.2492994" },
                Text = { Text = $" -  {FormatTime(TimeSpan.FromMinutes(users.PlayedTimeMin))}", FontSize = 15, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_INTERFACE_PLAYERSTAT);

            #endregion

            #region Stat

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.4845255 0.8291312", AnchorMax = "0.7891711 0.9271703" },
                Text = { Text = lang.GetMessage("XDStatistics_GATHER_STONE", this, player.userID.ToString()), FontSize = 18, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_INTERFACE_PLAYERSTAT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.7959413 0.8291312", AnchorMax = "0.9206995 0.9271703" },
                Text = { Text = users.gather.stone.ToString(), FontSize = 18, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_INTERFACE_PLAYERSTAT);


            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.4845255 0.708683", AnchorMax = "0.7891711 0.8067221" },
                Text = { Text = lang.GetMessage("XDStatistics_GATHER_SULFUR", this, player.userID.ToString()), FontSize = 18, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_INTERFACE_PLAYERSTAT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.7959413 0.708683", AnchorMax = "0.9206995 0.8067221" },
                Text = { Text = users.gather.sulfur.ToString(), FontSize = 18, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_INTERFACE_PLAYERSTAT);


            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.4845255 0.5854337", AnchorMax = "0.7891711 0.6834728" },
                Text = { Text = lang.GetMessage("XDStatistics_GATHER_METALL", this, player.userID.ToString()), FontSize = 18, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_INTERFACE_PLAYERSTAT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.7959413 0.5854337", AnchorMax = "0.9206995 0.6834728" },
                Text = { Text = users.gather.metall.ToString(), FontSize = 18, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_INTERFACE_PLAYERSTAT);



            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.4845255 0.4621844", AnchorMax = "0.7891711 0.5602235" },
                Text = { Text = lang.GetMessage("XDStatistics_GATHER_HQM", this, player.userID.ToString()), FontSize = 18, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_INTERFACE_PLAYERSTAT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.7959413 0.4621844", AnchorMax = "0.9206995 0.5602235" },
                Text = { Text = users.gather.hqm.ToString(), FontSize = 18, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_INTERFACE_PLAYERSTAT);


            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.4845255 0.3389351", AnchorMax = "0.7891711 0.4369742" },
                Text = { Text = lang.GetMessage("XDStatistics_GATHER_WOOD", this, player.userID.ToString()), FontSize = 18, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_INTERFACE_PLAYERSTAT);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.7959413 0.3389351", AnchorMax = "0.9206995 0.4369742" },
                Text = { Text = users.gather.wood.ToString(), FontSize = 18, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
            }, UI_INTERFACE_PLAYERSTAT);

            if (radore)
            {
                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.4845255 0.2156858", AnchorMax = "0.7891711 0.3137249" },
                    Text = { Text = lang.GetMessage("XDStatistics_GATHER_RADORE", this, player.userID.ToString()), FontSize = 18, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, UI_INTERFACE_PLAYERSTAT);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.7959413 0.2156858", AnchorMax = "0.9206995 0.3137249" },
                    Text = { Text = users.gather.radore.ToString(), FontSize = 18, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleLeft, Color = "1 1 1 1" }
                }, UI_INTERFACE_PLAYERSTAT);
            }

            #endregion

            CuiHelper.AddUi(player, container);
            bool stats = Stat == player.userID ? true : false;
            ButtonInkognito(player, stats);
        }

        public void ButtonInkognito(BasePlayer player, bool MyStat)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, "ButtonInkognito");
            string color = "#d12d5f";
            string color2 = "#FFFFFFFF";
            string comand = "UI_HandlerStat inkognito";
            if (!MyStat)
            {
                color = "#D12D5F82";
                color2 = "#FFFFFF82";
                comand = "";
            }
            string ink = storedData.players[player.userID].inkognito == true ? "XDStatistics_INKOGNITOOFF" : "XDStatistics_INKOGNITOON";
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.005767672 0.141623", AnchorMax = "0.1991913 0.2610531" },
                Button = { Color = HexToRustFormat(color), Command = comand },
                Text = { Text = lang.GetMessage(ink, this, player.userID.ToString()), FontSize = 10, Align = TextAnchor.MiddleCenter, Color = HexToRustFormat(color2) }
            }, UI_INTERFACE_PLAYERSTAT, "ButtonInkognito");
            CuiHelper.AddUi(player, container);
        }


        public void PlayerListStat(BasePlayer player, string target = "")
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_INTERFACE_PLAYERSTAT);
            CuiHelper.DestroyUi(player, UI_INTERFACE_PLAYERLIST);
            CuiHelper.DestroyUi(player, UI_INTERFACE_TOPUSERSERVERS);
            CuiHelper.DestroyUi(player, UI_INTERFACE_TopTenUserServer);
            CuiHelper.DestroyUi(player, "MyStatButton");
            CuiHelper.DestroyUi(player, "topuserserver");
            CuiHelper.DestroyUi(player, "topuserservers");


            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.2848959 0.370334", AnchorMax = "0.6979167 0.7509261" },
                Image = { Color = "0 0 0 0" }
            }, UI_INTERFACE, UI_INTERFACE_PLAYERLIST);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.10625 0.05277777" },
                Button = { Command = $"UI_HandlerStat GoStatPlayers {player.userID}", Color = "0 0 0 0" },
                Text = { Text = lang.GetMessage("XDStatistics_MYSTATBUTTON", this, player.UserIDString), FontSize = 15, Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE, "MyStatButton");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.2447931 0", AnchorMax = "0.4390625 0.05277777" },
                Button = { Command = "UI_HandlerStat GoTopTenPlayer", Color = "0 0 0 0" },
                Text = { Text = lang.GetMessage("XDStatistics_TITLEtopTen", this, player.userID.ToString()), FontSize = 15, Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE, "topuserservers");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.01513237 0.8710458", AnchorMax = "0.9873895 0.9635031" },
                Image = { Color = HexToRustFormat("#00000080") }
            }, UI_INTERFACE_PLAYERLIST, "Input");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.002594034 0.05263158", AnchorMax = "0.04669262 0.9473684" },
                Image = { Color = HexToRustFormat("#00000080") }
            }, "Input", "IcoSearch");

            container.Add(new CuiElement
            {
                Parent = "IcoSearch",
                Components = {
                    new CuiImageComponent {
                        Color = HexToRustFormat("#FFFFFFFF"),
                        Sprite = "assets/icons/examine.png",
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.1842105 0.1578947",
                        AnchorMax = "0.8018576 0.7755418"
                    },
                }
            });
            string SearchName = "";
            container.Add(new CuiElement
            {
                Parent = "Input",
                Name = ".Input.Current",
                Components =
                {
                    new CuiInputFieldComponent { Text = SearchName, FontSize = 14,Command = $"UI_HandlerStat listplayer {SearchName}", Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#ffffffFF"), CharsLimit = 15},
                    new CuiRectTransformComponent { AnchorMin = "0.05058365 0.1052618", AnchorMax = "0.994812 0.8947383" }
                }
            });
            Dictionary<ulong, PlayerData> plist = storedData.players;
            if (config.setings.UseIQFakeActive && IQFakeActive)
                plist = storedData.players.Union(playerdataFake).ToDictionary(q => q.Key, q => q.Value);


            int x = 0; int y = 0;
            foreach (var pList in plist.Where(i => i.Value.nick.ToLower().Contains(target.ToLower())))
            {

                if (storedData.playerignore.Contains(pList.Key)) continue;
                string LockStatus = pList.Value.inkognito == false ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = pList.Value.inkognito == false ? "" : $"UI_HandlerStat GoStatPlayers {pList.Key}";

                container.Add(new CuiButton
                {
                    FadeOut = 0.2f,
                    RectTransform = { AnchorMin = $"{0.0239597 + (x * 0.3)} {0.715328 - (y * 0.1)}", AnchorMax = $"{0.3026481 + (x * 0.3)} {0.8102188 - (y * 0.1)}" },
                    Button = { Command = Command, Color = HexToRustFormat("#00000080") },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                }, UI_INTERFACE_PLAYERLIST, $"BUTTON{player.userID}");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.1611373 0", AnchorMax = "1 1" },
                    Text = { Text = pList.Value.nick.Replace(" ", ""), FontSize = 12, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
                }, $"BUTTON{player.userID}");

                container.Add(new CuiElement
                {
                    Parent = $"BUTTON{player.userID}",
                    Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat("#FFFFFFFF"), Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.02369668 0.2051285", AnchorMax = "0.1374408 0.820514" }
                    }
                });

                x++;
                if (x == 3)
                {
                    x = 0;
                    y++;

                    if (y == 5)
                    {
                        break;
                    }
                }

            };

            CuiHelper.AddUi(player, container);
        }
        Dictionary<ulong, PlayerData> playerdataFake = new Dictionary<ulong, PlayerData>();

        #region IQFakePlayer
        int synhIqFake = 0;
        System.Random random = new System.Random();
        void SyncReservedFinish(string JSON)
        {
              if (!config.setings.UseIQFakeActive) return;
            List<FakePlayer> ContentDeserialize = JsonConvert.DeserializeObject<List<FakePlayer>>(JSON);
            PlayerBases = ContentDeserialize;
            int DayWipe = (int)IQFakeActive.Call("DayWipe");
            foreach(FakePlayer fakePlayer in ContentDeserialize)
            {
                if (BasePlayer.activePlayerList.Where(x => x.userID == fakePlayer.UserID).Any()) continue;
                PlayerData i = new PlayerData();
                i.gather = new Gather()
                {
                    allresource = (int)((random.Next(0, synhIqFake+10) * DayWipe) * UnityEngine.Random.Range(0.23f, 10.97f))
                };
                i.pvp = new Pvp()
                {
                    kill = (int)((random.Next(0, synhIqFake) * DayWipe) * UnityEngine.Random.Range(0.23f, 1.07f)),
                    npc = (int)((random.Next(0, synhIqFake +1) * DayWipe) * UnityEngine.Random.Range(0.23f, 2.47f)),
                };
                i.other = new Other()
                {
                    economic = (int)((random.Next(0, synhIqFake + 1) * DayWipe) * UnityEngine.Random.Range(0.23f, 3.47f)),
                    exsplosive = (int)((random.Next(0, synhIqFake) * DayWipe) * UnityEngine.Random.Range(0.23f, 1.17f)),
                };
                i.inkognito = false;
                i.PlayedTimeMin = (int)((random.Next(0, synhIqFake + 1) * DayWipe) * UnityEngine.Random.Range(1.23f, 2.47f));
                i.nick = fakePlayer.DisplayName;
                playerdataFake.Add(fakePlayer.UserID, i);
            }
            PrintWarning("XDStatistics - успешно синхронизирована с IQFakeActive");
            PrintWarning("=============SYNC==================");
            synhIqFake++;
        }
        public List<FakePlayer> PlayerBases = new List<FakePlayer>();
        public class FakePlayer
        {
            public ulong UserID;
            public string DisplayName;
            public string IQChatPreifx;
            public string IQChatColorChat;
            public string IQChatColorNick;
        }

        #endregion
        public void TopUserServer(BasePlayer player)
        {
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_INTERFACE_PLAYERSTAT);
            CuiHelper.DestroyUi(player, UI_INTERFACE_PLAYERLIST);
            CuiHelper.DestroyUi(player, UI_INTERFACE_TOPUSERSERVERS);
            CuiHelper.DestroyUi(player, UI_INTERFACE_TopTenUserServer);

            CuiHelper.DestroyUi(player, "MyStatButton");
            CuiHelper.DestroyUi(player, "topuserserver");
            CuiHelper.DestroyUi(player, "topuserservers");

            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.02812498 0.0472222", AnchorMax = "0.9791667 0.8268518" },
                Image = { Color = "0 0 0 0" }
            }, UI_INTERFACE, UI_INTERFACE_TOPUSERSERVERS);

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_TOPUSERSERVERS,
                Components =
                    {
                        new CuiTextComponent { Text = lang.GetMessage("XDStatistics_TITLEtop", this, player.userID.ToString()), Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFFF"), FontSize = 19 },
                        new CuiRectTransformComponent{ AnchorMin = "0.384995 0.9429928", AnchorMax = "0.5887185 0.9940618"},
                        new CuiOutlineComponent{ Color = HexToRustFormat("#000000FF"), Distance = "0.3 -0.3", UseGraphicAlpha = false }
                    }
            });
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.2447931 0", AnchorMax = "0.4390625 0.05277777" },
                Button = { Command = "UI_HandlerStat GoTopTenPlayer", Color = "0 0 0 0" },
                Text = { Text = lang.GetMessage("XDStatistics_TITLEtopTen", this, player.userID.ToString()), FontSize = 15, Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE, "topuserservers");
            #region Top Users

            ulong NpcKiller = storedData.players.OrderByDescending(x => x.Value.pvp.npc).Select(x => x.Key).First();
            ulong Economiks = storedData.players.OrderByDescending(x => x.Value.other.economic).Select(x => x.Key).First();
            ulong Killer = storedData.players.OrderByDescending(x => x.Value.pvp.kill).Select(x => x.Key).First();
            ulong TimePlayed = storedData.players.OrderByDescending(x => x.Value.PlayedTimeMin).Select(x => x.Key).First();
            ulong Exsplosive = storedData.players.OrderByDescending(x => x.Value.other.exsplosive).Select(x => x.Key).First();
            ulong Farmila = storedData.players.OrderByDescending(x => x.Value.gather.allresource).Select(x => x.Key).First();
            #region Avatar

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_TOPUSERSERVERS,
                Components = {
                    new CuiRawImageComponent {
                        Png = GetImage(NpcKiller.ToString()),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.1182914 0.395487",
                        AnchorMax = "0.2048193 0.591449"
                    },
                }
            });

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_TOPUSERSERVERS,
                Components = {
                    new CuiRawImageComponent {
                        Png = GetImage(Killer.ToString()),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.2968244 0.5653197",
                        AnchorMax = "0.3833523 0.7612842"
                    },
                }
            });

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_TOPUSERSERVERS,
                Components = {
                    new CuiRawImageComponent {
                        Png = GetImage(TimePlayed.ToString()),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.4770004 0.7113973",
                        AnchorMax = "0.5635283 0.9073618"
                    },
                }
            });

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_TOPUSERSERVERS,
                Components = {
                    new CuiRawImageComponent {
                        Png = GetImage(Exsplosive.ToString()),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.6522476 0.5617568",
                        AnchorMax = "0.7387755 0.7577214"
                    },
                }
            });

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_TOPUSERSERVERS,
                Components = {
                    new CuiRawImageComponent {
                        Png = GetImage(Farmila.ToString()),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.8263995 0.3966747",
                        AnchorMax = "0.9129274 0.5926367"
                    },
                }
            });

            #endregion

            #region Top

            if (Economic)
            {
                container.Add(new CuiElement
                {
                    Parent = UI_INTERFACE_TOPUSERSERVERS,
                    Components = {
                    new CuiRawImageComponent {
                        Png = GetImage("Economic"),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.03888117 0.03562898",
                        AnchorMax = "0.2272717 0.643705"
                    },
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.09145674 0.2125891", AnchorMax = "0.203724 0.2672209" },
                    Text = { Text = storedData.players[Economiks].other.economic.ToString(), FontSize = 20, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
                }, UI_INTERFACE_TOPUSERSERVERS);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.08817088 0.1201425", AnchorMax = "0.203724 0.1769595" },
                    Text = { Text = storedData.players[Economiks].nick, FontSize = 13, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperRight }
                }, UI_INTERFACE_TOPUSERSERVERS);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.06516981 0.09144896", AnchorMax = "0.1998905 0.131829" },
                    Button = { Command = $"UI_HandlerStat GoStatPlayers {Economiks}", Color = "0 0 0 0" },
                    Text = { Text = "", FontSize = 15, Align = TextAnchor.MiddleCenter }
                }, UI_INTERFACE_TOPUSERSERVERS);
            }
            else
            {
                container.Add(new CuiElement
                {
                    Parent = UI_INTERFACE_TOPUSERSERVERS,
                    Components = {
                    new CuiRawImageComponent {
                        Png = GetImage("NpcKill"),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.03888117 0.03562898",
                        AnchorMax = "0.2272717 0.643705"
                    },
                }
                });

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.09145674 0.2125891", AnchorMax = "0.203724 0.2672209" },
                    Text = { Text = storedData.players[NpcKiller].pvp.npc.ToString(), FontSize = 20, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
                }, UI_INTERFACE_TOPUSERSERVERS);

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.08817088 0.1201425", AnchorMax = "0.203724 0.1769595" },
                    Text = { Text = storedData.players[NpcKiller].nick, FontSize = 13, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperRight }
                }, UI_INTERFACE_TOPUSERSERVERS);

                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = "0.06516981 0.09144896", AnchorMax = "0.1998905 0.131829" },
                    Button = { Command = $"UI_HandlerStat GoStatPlayers {NpcKiller}", Color = "0 0 0 0" },
                    Text = { Text = "", FontSize = 15, Align = TextAnchor.MiddleCenter }
                }, UI_INTERFACE_TOPUSERSERVERS);
            }

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_TOPUSERSERVERS,
                Components = {
                    new CuiRawImageComponent {
                        Png = GetImage("Killer"),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.2174141 0.2042751",
                        AnchorMax = "0.4058046 0.8123511"
                    },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.2683469 0.3788604", AnchorMax = "0.3806141 0.4334922" },
                Text = { Text = storedData.players[Killer].pvp.kill.ToString(), FontSize = 20, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
            }, UI_INTERFACE_TOPUSERSERVERS);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.2598474 0.2876015", AnchorMax = "0.3806141 0.3444184" },
                Text = { Text = storedData.players[Killer].nick, FontSize = 13, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperRight }
            }, UI_INTERFACE_TOPUSERSERVERS);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.2437029 0.2600954", AnchorMax = "0.3784236 0.3004757" },
                Button = { Command = $"UI_HandlerStat GoStatPlayers {Killer}", Color = "0 0 0 0" },
                Text = { Text = "", FontSize = 15, Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE_TOPUSERSERVERS);

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_TOPUSERSERVERS,
                Components = {
                    new CuiRawImageComponent {
                        Png = GetImage("TopTime"),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.3959472 0.3491687",
                        AnchorMax = "0.5843377 0.9572447"
                    },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.4222344 0.5225658", AnchorMax = "0.5591472 0.5771989" },
                Text = { Text = $"{FormatTime(TimeSpan.FromMinutes(storedData.players[TimePlayed].PlayedTimeMin))}", FontSize = 20, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
            }, UI_INTERFACE_TOPUSERSERVERS);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.4383805 0.4324951", AnchorMax = "0.5591472 0.489312" },
                Text = { Text = storedData.players[TimePlayed].nick, FontSize = 13, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperRight }
            }, UI_INTERFACE_TOPUSERSERVERS);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.422236 0.4038013", AnchorMax = "0.5569566 0.4441816" },
                Button = { Command = $"UI_HandlerStat GoStatPlayers {TimePlayed}", Color = "0 0 0 0" },
                Text = { Text = "", FontSize = 15, Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE_TOPUSERSERVERS);

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_TOPUSERSERVERS,
                Components = {
                    new CuiRawImageComponent {
                        Png = GetImage("Exsplosive"),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.5728373 0.2042751",
                        AnchorMax = "0.7612278 0.8123511"
                    },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.6128149 0.3788604", AnchorMax = "0.7360374 0.4334922" },
                Text = { Text = storedData.players[Exsplosive].other.exsplosive.ToString(), FontSize = 20, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
            }, UI_INTERFACE_TOPUSERSERVERS);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.6152706 0.2899768", AnchorMax = "0.7360373 0.3467937" },
                Text = { Text = storedData.players[Exsplosive].nick, FontSize = 13, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperRight }
            }, UI_INTERFACE_TOPUSERSERVERS);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.5991261 0.2589077", AnchorMax = "0.7338468 0.299288" },
                Button = { Command = $"UI_HandlerStat GoStatPlayers {Exsplosive}", Color = "0 0 0 0" },
                Text = { Text = "", FontSize = 15, Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE_TOPUSERSERVERS);

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_TOPUSERSERVERS,
                Components = {
                    new CuiRawImageComponent {
                        Png = GetImage("Farmila"),
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent {
                        AnchorMin = "0.7469892 0.03562898",
                        AnchorMax = "0.9353797 0.643705"
                    },
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.7864192 0.2078385", AnchorMax = "0.9096416 0.2624702" },
                Text = { Text = storedData.players[Farmila].gather.allresource.ToString(), FontSize = 20, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleRight }
            }, UI_INTERFACE_TOPUSERSERVERS);

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0.7888748 0.1201425", AnchorMax = "0.9096415 0.1769594" },
                Text = { Text = storedData.players[Farmila].nick, FontSize = 13, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperRight }
            }, UI_INTERFACE_TOPUSERSERVERS);

            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0.7738256 0.0902613", AnchorMax = "0.9085463 0.1306414" },
                Button = { Command = $"UI_HandlerStat GoStatPlayers {Farmila}", Color = "0 0 0 0" },
                Text = { Text = "", FontSize = 15, Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE_TOPUSERSERVERS);

            #endregion


            #endregion

            CuiHelper.AddUi(player, container);

        }

        public void TopTenUserServer(BasePlayer player)
        {

            Dictionary<ulong, PlayerData> plist = storedData.players;
            if (config.setings.UseIQFakeActive && IQFakeActive)
                plist = storedData.players.Union(playerdataFake).ToDictionary(h => h.Key, j => j.Value);

            string fives = lang.GetMessage("XDStatistics_TITLENpcKill", this, player.UserIDString);
            var five = plist.OrderByDescending(x => x.Value.pvp.npc).Select(x => x.Key).Take(10);
            if (Economic)
            {
                five = plist.OrderByDescending(x => x.Value.other.economic).Select(x => x.Key).Take(10);
                fives = lang.GetMessage("XDStatistics_TITLEEconomic", this, player.UserIDString);
            }
            var Killer = plist.OrderByDescending(x => x.Value.pvp.kill).Select(x => x.Key).Take(10);
            var TimePlayed = plist.OrderByDescending(x => x.Value.PlayedTimeMin).Select(x => x.Key).Take(10);
            var Exsplosive = plist.OrderByDescending(x => x.Value.other.exsplosive).Select(x => x.Key).Take(10);
            var Farmila = plist.OrderByDescending(x => x.Value.gather.allresource).Select(x => x.Key).Take(10);
            CuiElementContainer container = new CuiElementContainer();
            CuiHelper.DestroyUi(player, UI_INTERFACE_PLAYERSTAT);
            CuiHelper.DestroyUi(player, UI_INTERFACE_PLAYERLIST);
            CuiHelper.DestroyUi(player, UI_INTERFACE_TOPUSERSERVERS);
            CuiHelper.DestroyUi(player, UI_INTERFACE_TopTenUserServer);
            CuiHelper.DestroyUi(player, "MyStatButton");
            CuiHelper.DestroyUi(player, "topuserserver");
            CuiHelper.DestroyUi(player, "topuserservers");
            container.Add(new CuiButton
            {
                RectTransform = { AnchorMin = "0 0", AnchorMax = "0.10625 0.05277777" },
                Button = { Command = $"UI_HandlerStat GoStatPlayers {player.userID}", Color = "0 0 0 0" },
                Text = { Text = lang.GetMessage("XDStatistics_MYSTATBUTTON", this, player.UserIDString), FontSize = 15, Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE, "MyStatButton");
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.02812498 0.0472222", AnchorMax = "0.9791667 0.8268518" },
                Image = { Color = "0 0 0 0" }
            }, UI_INTERFACE, UI_INTERFACE_TopTenUserServer);

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_TopTenUserServer,
                Components =
                    {
                        new CuiTextComponent { Text = lang.GetMessage("XDStatistics_TITLEtopTen", this, player.userID.ToString()), Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFFF"), FontSize = 21 },
                        new CuiRectTransformComponent{ AnchorMin = "0.3285873 0.8931116", AnchorMax = "0.6456733 0.9572447"},
                        new CuiOutlineComponent{ Color = HexToRustFormat("#000000FF"), Distance = "0.3 -0.3", UseGraphicAlpha = false }
                    }
            });

            container.Add(new CuiElement
            {
                Parent = UI_INTERFACE_TopTenUserServer,
                Components =
                    {
                        new CuiTextComponent { Text = lang.GetMessage("XDStatistics_TITLEWarning", this, player.userID.ToString()), Align = TextAnchor.MiddleCenter, Color = HexToRustFormat("#FFFFFFFF"), FontSize = 15 },
                        new CuiRectTransformComponent{ AnchorMin = "0.2179633 0.008321077", AnchorMax = "0.7557505 0.07245211"},
                        new CuiOutlineComponent{ Color = HexToRustFormat("#000000FF"), Distance = "0.3 -0.3", UseGraphicAlpha = false }
                    }
            });
            double pos = 0.088;
            #region five
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.03395402 0.08788604", AnchorMax = "0.1911281 0.8491687" },
                Image = { Color = HexToRustFormat(config.intarface.colorTopTen) }
            }, UI_INTERFACE_TopTenUserServer, UI_INTERFACE_TopTenUserServer + ".five");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9126365", AnchorMax = "1 1" },
                Text = { Text = fives, FontSize = 18, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE_TopTenUserServer + ".five");
            int i = 0;
            foreach (var item in five)
            {
                string LockStatus = plist[item].inkognito == false ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = plist[item].inkognito == false ? "" : $"UI_HandlerStat GoStatPlayers {item}";
                string text = plist[item].nick;
                if (plist[item].nick.Length >= 8) text = plist[item].nick.Substring(0, 8) + "..";
                else text = plist[item].nick;
                string TXTFive = $"{text} - {plist[item].pvp.npc}";
                if(IQEconomic)
                    TXTFive = $"{text} - {plist[item].other.economic}";

                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.01 {0.7971919 - (i * pos)}", AnchorMax = $"0.99 {0.8767551 - (i * pos)}" },
                    Image = { Color = HexToRustFormat("#92929272") }
                }, UI_INTERFACE_TopTenUserServer + ".five", "Main");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.139124 0", AnchorMax = "1 1" },
                    Text = { Text = TXTFive, FontSize = 16, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
                }, "Main");

                container.Add(new CuiElement
                {
                    Parent = "Main",
                    Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat("#FFFFFFFF"), Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.007110861 0.1568627", AnchorMax = "0.1315509 0.8431371" }
                    }
                });
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                }, "Main");

                if (i >= 10)
                    break;
                i++;
            }

            #endregion

            #region Killer
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.223987 0.08788605", AnchorMax = "0.3811616 0.8491687" },
                Image = { Color = HexToRustFormat(config.intarface.colorTopTen) }
            }, UI_INTERFACE_TopTenUserServer, UI_INTERFACE_TopTenUserServer + ".Killer");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9126365", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("XDStatistics_TITLEPlayerKill", this, player.UserIDString), FontSize = 18, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE_TopTenUserServer + ".Killer");
            int q = 0;
            foreach (var item in Killer)
            {
                string LockStatus = plist[item].inkognito == false ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = plist[item].inkognito == false ? "" : $"UI_HandlerStat GoStatPlayers {item}";
                string text = plist[item].nick;
                if (plist[item].nick.Length >= 8) text = plist[item].nick.Substring(0, 8) + "..";
                else text = plist[item].nick;
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.01 {0.7971919 - (q * pos)}", AnchorMax = $"0.99 {0.8767551 - (q * pos)}" },
                    Image = { Color = HexToRustFormat("#92929272") }
                }, UI_INTERFACE_TopTenUserServer + ".Killer", "Main");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.139124 0", AnchorMax = "1 1" },
                    Text = { Text = $"{text} - {plist[item].pvp.kill}", FontSize = 16, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
                }, "Main");

                container.Add(new CuiElement
                {
                    Parent = "Main",
                    Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat("#FFFFFFFF"), Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.007110861 0.1568627", AnchorMax = "0.1315509 0.8431371" }
                    }
                });
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                }, "Main");

                if (q >= 10)
                    break;
                q++;
            }

            #endregion

            #region TopTime
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.4107352 0.08788604", AnchorMax = "0.5679094 0.8491687" },
                Image = { Color = HexToRustFormat(config.intarface.colorTopTen) }
            }, UI_INTERFACE_TopTenUserServer, UI_INTERFACE_TopTenUserServer + ".TopTime");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9126365", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("XDStatistics_TITLETime", this, player.UserIDString), FontSize = 18, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE_TopTenUserServer + ".TopTime");
            int r = 0;

            foreach (var item in TimePlayed)
            {
                string LockStatus = plist[item].inkognito == false ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = plist[item].inkognito == false ? "" : $"UI_HandlerStat GoStatPlayers {item}";
                string text = plist[item].nick;
                if (plist[item].nick.Length >= 8) text = plist[item].nick.Substring(0, 8) + "..";
                else text = plist[item].nick;
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.01 {0.7971919 - (r * pos)}", AnchorMax = $"0.99 {0.8767551 - (r * pos)}" },
                    Image = { Color = HexToRustFormat("#92929272") }
                }, UI_INTERFACE_TopTenUserServer + ".TopTime", "Main");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.139124 0", AnchorMax = "1 1" },
                    Text = { Text = $"{text} - {FormatShortTime(TimeSpan.FromMinutes(plist[item].PlayedTimeMin))}", FontSize = 16, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
                }, "Main");

                container.Add(new CuiElement
                {
                    Parent = "Main",
                    Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat("#FFFFFFFF"), Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.007110861 0.1568627", AnchorMax = "0.1315509 0.8431371" }
                    }
                });
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                }, "Main");

                if (r >= 10)
                    break;
                r++;
            }
            #endregion

            #region Exsplosive
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.5980307 0.08788604", AnchorMax = "0.7552048 0.8491687" },
                Image = { Color = HexToRustFormat(config.intarface.colorTopTen) }
            }, UI_INTERFACE_TopTenUserServer, UI_INTERFACE_TopTenUserServer + ".Exsplosive");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9126365", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("XDStatistics_TITLEExsplosive", this, player.UserIDString), FontSize = 18, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE_TopTenUserServer + ".Exsplosive");
            int e = 0;
            foreach (var item in Exsplosive)
            {
                string LockStatus = plist[item].inkognito == false ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = plist[item].inkognito == false ? "" : $"UI_HandlerStat GoStatPlayers {item}";
                string text = plist[item].nick;
                if (plist[item].nick.Length >= 8) text = plist[item].nick.Substring(0, 8) + "..";
                else text = plist[item].nick;
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.01 {0.7971919 - (e * pos)}", AnchorMax = $"0.99 {0.8767551 - (e * pos)}" },
                    Image = { Color = HexToRustFormat("#92929272") }
                }, UI_INTERFACE_TopTenUserServer + ".Exsplosive", "Main");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.139124 0", AnchorMax = "1 1" },
                    Text = { Text = $"{text} - {plist[item].other.exsplosive}", FontSize = 16, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
                }, "Main");

                container.Add(new CuiElement
                {
                    Parent = "Main",
                    Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat("#FFFFFFFF"), Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.007110861 0.1568627", AnchorMax = "0.1315509 0.8431371" }
                    }
                });
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                }, "Main");

                if (e >= 10)
                    break;
                e++;
            }
            #endregion

            #region Farmila
            container.Add(new CuiPanel
            {
                RectTransform = { AnchorMin = "0.7897073 0.08788604", AnchorMax = "0.9468814 0.8491687" },
                Image = { Color = HexToRustFormat(config.intarface.colorTopTen) }
            }, UI_INTERFACE_TopTenUserServer, UI_INTERFACE_TopTenUserServer + ".Farmila");

            container.Add(new CuiLabel
            {
                RectTransform = { AnchorMin = "0 0.9126365", AnchorMax = "1 1" },
                Text = { Text = lang.GetMessage("XDStatistics_TITLEFarmila", this, player.UserIDString), FontSize = 18, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
            }, UI_INTERFACE_TopTenUserServer + ".Farmila");
            int s = 0;
            foreach (var item in Farmila)
            {
                string LockStatus = plist[item].inkognito == false ? "assets/icons/lock.png" : "assets/icons/unlock.png";
                string Command = plist[item].inkognito == false ? "" : $"UI_HandlerStat GoStatPlayers {item}";
                string text = plist[item].nick;
                if (plist[item].nick.Length >= 8) text = plist[item].nick.Substring(0, 8) + "..";
                else text = plist[item].nick;
                container.Add(new CuiPanel
                {
                    RectTransform = { AnchorMin = $"0.01 {0.7971919 - (s * pos)}", AnchorMax = $"0.99 {0.8767551 - (s * pos)}" },
                    Image = { Color = HexToRustFormat("#92929272") }
                }, UI_INTERFACE_TopTenUserServer + ".Farmila", "Main");

                container.Add(new CuiLabel
                {
                    RectTransform = { AnchorMin = "0.139124 0", AnchorMax = "1 1" },
                    Text = { Text = $"{text} - {plist[item].gather.allresource}", FontSize = 16, Color = HexToRustFormat("#FFFFFFFF"), Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleLeft }
                }, "Main");

                container.Add(new CuiElement
                {
                    Parent = "Main",
                    Components =
                    {
                        new CuiImageComponent {  Color = HexToRustFormat("#FFFFFFFF"), Sprite = LockStatus },
                        new CuiRectTransformComponent { AnchorMin = "0.007110861 0.1568627", AnchorMax = "0.1315509 0.8431371" }
                    }
                });
                container.Add(new CuiButton
                {
                    RectTransform = { AnchorMin = $"0 0", AnchorMax = $"1 1" },
                    Button = { Command = Command, Color = "0 0 0 0" },
                    Text = { Text = "", Align = TextAnchor.MiddleCenter }
                }, "Main");

                if (s >= 10)
                    break;
                s++;
            }
            #endregion

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Command

        [ConsoleCommand("stats")]
        private void CmdCommand(ConsoleSystem.Arg args)
        {
            switch (args.Args[0])
            {
                case "ignoreplayer":
                    {
                        BasePlayer player = BasePlayer.FindByID(ulong.Parse(args.Args[1]));

                        if (player == null) return;
                        storedData.playerignore.Add(player.userID);
                        PrintWarning($"Вы скрыли статистику для игрока {player.userID}");
                        break;
                    }
            }
        }


        [ConsoleCommand("UI_HandlerStat")]
        private void CmdConsoleHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();

            if (player != null && args.HasArgs(1))
            {
                switch (args.Args[0])
                {
                    case "listplayer":
                        {
                            if (args.Args.Length > 1)
                            {
                                string Seaecher = args.Args[1].ToLower();
                                PlayerListStat(player, Seaecher);
                            }
                            else PlayerListStat(player);
                            break;
                        }
                    case "GoStatPlayers":
                        {
                            ulong id = ulong.Parse(args.Args[1]);
                            PlayerStat(player, id);
                            break;
                        }
                    case "Golistplayer":
                        {
                            PlayerListStat(player);
                            break;
                        }
                    case "GoTopTenPlayer":
                        {
                            TopTenUserServer(player);
                            break;
                        }
                    case "topuser":
                        {
                            TopUserServer(player);
                            break;
                        }
                    case "inkognito":
                        {
                            if (config.setings.statInkognito && !permission.UserHasPermission(player.UserIDString, perm)) return;
                            if (storedData.players[player.userID].inkognito)
                            {

                                storedData.players[player.userID].inkognito = false;
                            }
                            else storedData.players[player.userID].inkognito = true;
                            ButtonInkognito(player, true);
                            break;
                        }
                }
            }
        }

        #endregion

        #region Hooks
        bool radore = false;
        bool Economic = false;
        void Init()
        {
            Unsubscribe(nameof(OnServerSave));
            LoadData();
        }
        void OnServerSave()
        {
            SaveData();
        }
        private void OnServerInitialized()
        {
            if (!ImageLibrary)
            {
                PrintError($"ERROR! Plugin ImageLibrary not found!");
            }
            if (OreBonus)
                radore = true;
            if (IQEconomic)
                Economic = true;
            foreach (var img in config.intarface.IconStats)
            {
                AddImage(img.Value, img.Key);
            }

            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                OnPlayerConnected(BasePlayer.activePlayerList[i]);
            }

            permission.RegisterPermission(perm, this);
            cmd.AddChatCommand(config.setings.commandOpenStat, this, nameof(openui));
            if (config.setings.useServerSave)
                Subscribe(nameof(OnServerSave));
            timer.Every(60f, GrabTimePlayed);
        }

        void OnNewSave(string filename)
        {
            if (config.setings.dataClear)
            {
                storedData = new StoredData();
                storedData.players.Clear();
            }
        }

        private void Unload()
        {
            SaveData();

            BasePlayer.activePlayerList.ToList().ForEach(x =>
            {
                CuiHelper.DestroyUi(x, UI_INTERFACE_PLAYERSTAT);
                CuiHelper.DestroyUi(x, UI_INTERFACE_PLAYERLIST);
                CuiHelper.DestroyUi(x, UI_INTERFACE_TOPUSERSERVERS);
                CuiHelper.DestroyUi(x, UI_INTERFACE);
            });
        }

        void OnPlayerConnected(BasePlayer player, bool first = true)
        {
            if (player.IsReceivingSnapshot)
            {
                NextTick(() => OnPlayerConnected(player, first));
                return;
            }

            #region DataUserLoad

            if (!storedData.players.ContainsKey(player.userID))
            {
                CrateDataUsers(player);
            }
            else
            {
                storedData.players[player.userID].nick = player.displayName;
            }

            SteamAvatarAdd(player.UserIDString);

            #endregion

            #region CashImage

            foreach (var img in config.intarface.IconStats)
            {
                SendImage(player, img.Key);
            }

            #endregion
        }

        void OnDispenserBonus(ResourceDispenser dispenser, BaseEntity entity, Item item) => OnDispenserGather(dispenser, entity, item);

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            BasePlayer player = entity.ToPlayer();
            if (player == null) return;
            ProgressAdd(player, item);

        }
        void RadOreGive(BasePlayer player, Item RadOre)
        {
            storedData.players[player.userID].gather.radore += RadOre.amount;
        }
        void OnCollectiblePickup(Item item, BasePlayer player, CollectibleEntity entity)
        {
            ProgressAdd(player, item);
        }
        void OnQuarryGather(MiningQuarry quarry, Item item)
        {
            if (quarry == null) return;
            BasePlayer player = BasePlayer.FindByID(quarry.OwnerID);

            if (player == null) return;
            ProgressAdd(player, item);
        }

        void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if (player == null || entity == null || entity is SupplySignal || entity is SurveyCharge || entity is SmokeGrenade || entity.ShortPrefabName.Contains("flare") || IsNPC(player)) return;
            if (!storedData.players.ContainsKey(player.userID))
            {
                CrateDataUsers(player);
            }
            storedData.players[player.userID].other.exsplosive++;
        }

        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            if (player == null || entity == null || IsNPC(player)) return;
            if (!storedData.players.ContainsKey(player.userID))
            {
                CrateDataUsers(player);
            }
            storedData.players[player.userID].other.exsplosive++;
        }

        [HookMethod("SET_BALANCE_USER")]
        void SET_BALANCE_USER(ulong player, int balance)
        {
            if (player == 0 && balance > 1) return;
            BasePlayer player1 = BasePlayer.FindByID(player);
            if (player1 == null || storedData.playerignore.Contains(player1.userID)) return;
            if (!storedData.players.ContainsKey(player1.userID))
            {
                CrateDataUsers(player1);
            }
            storedData.players[player1.userID].other.economic += balance;
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            try
            {
                if (entity.name.Contains("corpse")) return;
                if (info == null) return;
                if (entity is NPCPlayer || entity is NPCMurderer)
                {
                    BasePlayer player = info.InitiatorPlayer;
                    if (player == null) return;
                    storedData.players[player.userID].pvp.npc++;
                    return;
                }
                if (entity is BasePlayer)
                {
                    var victim = entity?.ToPlayer();
                    if (victim == null) return;

                    var slayer = info?.Initiator?.ToPlayer();
                    if (slayer == null) return;
                    if (IsFriends(victim.userID, slayer.userID)) return;
                    if (IsClans(victim.UserIDString, slayer.UserIDString)) return;
                    if (IsDuel(victim.userID)) return;
                    if (storedData.players.ContainsKey(victim.userID))
                        storedData.players[victim.userID].pvp.death++;
                    if (slayer == victim) return;
                    if (!storedData.players.ContainsKey(slayer.userID)) return;

                    storedData.players[slayer.userID].pvp.kill++;
                }
            }
            catch (Exception xe)
            {

                Logs($"Error: {xe}");
            }
        }

        private void OnPlayerAttack(BasePlayer attacker, HitInfo hitinfo)
        {
            if (hitinfo == null || attacker == null || !attacker.IsConnected) return;
            if (hitinfo.HitEntity is BaseNpc) return;
            var victim = hitinfo.HitEntity as BasePlayer;
            if (victim == null) return;
            if (victim == attacker) return;
            if (hitinfo.isHeadshot)
            {
                storedData.players[attacker.userID].pvp.headshot++;
            }
        }

        #endregion

        #region Metods  

        public void SteamAvatarAdd(string userId)
        {
            if (ImageLibrary == null) return;
            if (HasImage(userId)) return;

            string url = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=3F2959BD838BF8FB544B9A767F873457&" + "steamids=" + userId;
            webrequest.Enqueue(url, null, (code, response) =>
            {
                string Avatar = (string)JObject.Parse(response)["response"]["players"][0]["avatarfull"];
                AddImage(Avatar, userId);
            }, this);
        }
        public void CrateDataUsers(BasePlayer player)
        {
            PlayerData i = new PlayerData();
            i.gather = new Gather();
            i.pvp = new Pvp();
            i.other = new Other();
            i.nick = player.displayName;
            storedData.players.Add(player.userID, i);
        }

        public void ProgressAdd(BasePlayer player, Item item)
        {
            if (storedData.playerignore.Contains(player.userID)) return;
            if (!storedData.players.ContainsKey(player.userID))
            {
                CrateDataUsers(player);
            }
            switch (item.info.shortname)
            {
                case "wood":
                    {
                        storedData.players[player.userID].gather.wood += item.amount;
                        storedData.players[player.userID].gather.allresource += item.amount;
                        break;
                    }
                case "stones":
                    {
                        storedData.players[player.userID].gather.stone += item.amount;
                        storedData.players[player.userID].gather.allresource += item.amount;
                        break;
                    }
                case "metal.ore":
                    {
                        storedData.players[player.userID].gather.metall += item.amount;
                        storedData.players[player.userID].gather.allresource += item.amount;
                        break;
                    }
                case "sulfur.ore":
                    {
                        storedData.players[player.userID].gather.sulfur += item.amount;
                        storedData.players[player.userID].gather.allresource += item.amount;
                        break;
                    }
                case "hq.metal.ore":
                    {
                        storedData.players[player.userID].gather.hqm += item.amount;
                        storedData.players[player.userID].gather.allresource += item.amount;
                        break;
                    }
            }
        }

        public void GrabTimePlayed()
        {
            for (int i = 0; i < BasePlayer.activePlayerList.Count; i++)
            {
                BasePlayer player = BasePlayer.activePlayerList[i];
                if (storedData.playerignore.Contains(player.userID)) return;
                storedData.players[player.userID].PlayedTimeMin += 1;
            }
        }

        void Logs(string msg, string name = "log")
        {
            LogToFile(name, $"({DateTime.Now}) {msg}", this);
        }

        #endregion

        #region Help

        #region ref

        public bool IsFriends(ulong userID, ulong targetID)
        {
            if (Friends)
                return (bool)Friends?.Call("AreFriends", userID, targetID);
            else return false;
        }
        public bool IsClans(string userID, string targetID)
        {
            if (Clans)
                return (bool)Clans?.Call("IsClanMember", userID, targetID);
            else
                return false;
        }
        public bool IsDuel(ulong userID)
        {
            if (Battles)
                return (bool)Battles?.Call("IsPlayerOnBattle", userID);
            else if (Duel) return (bool)Duel?.Call("IsPlayerOnActiveDuel", BasePlayer.FindByID(userID));
            else return false;
        }

        #endregion

        private static string HexToRustFormat(string hex)
        {
            Color color;
            ColorUtility.TryParseHtmlString(hex, out color);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        private bool IsNPC(BasePlayer player)
        {
            if (player is NPCPlayer)
                return true;
            if (!(player.userID >= 76560000000000000L || player.userID <= 0L))
                return true;
            return false;
        }
        public static string FormatShortTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result += $"{time.Days} д. ";

            if (time.Hours != 0)
                result += $"{time.Hours} ч. ";

            if (time.Minutes != 0)
                result += $"{time.Minutes} м. ";

            return result;
        }
        public static string FormatTime(TimeSpan time)
        {
            string result = string.Empty;
            if (time.Days != 0)
                result += $"{Format(time.Days, "дней", "дня", "день")} ";

            if (time.Hours != 0)
                result += $"{Format(time.Hours, "часов", "часа", "час")} ";

            if (time.Minutes != 0)
                result += $"{Format(time.Minutes, "минут", "минуты", "минуту")} ";

            return result;
        }

        private static string Format(int units, string form1, string form2, string form3)
        {
            var tmp = units % 10;

            if (units >= 5 && units <= 20 || tmp >= 5 && tmp <= 9)
                return $"{units} {form1}";

            if (tmp >= 2 && tmp <= 4)
                return $"{units} {form2}";

            return $"{units} {form3}";
        }

        #endregion
    }
}
