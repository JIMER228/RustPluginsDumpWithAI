using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Oxide.Plugins
{
    [Info("InfoMenu", "OxideBro", "1.0.1")]
    public class InfoMenu : RustPlugin
    {

        #region Configuration
        private PluginConfig config;
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Благодарим за покупку плагина у разработчика OxideBro. Если вы передадите этот плагин сторонним лицам знайте - это лишает вас гарантированных обновлений!");
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();

            if (config.PluginVersion < Version)
                UpdateConfigValues();

            Config.WriteObject(config, true);
        }

        private void UpdateConfigValues()
        {
            PluginConfig baseConfig = PluginConfig.DefaultConfig();
            if (config.PluginVersion < new VersionNumber(1, 1, 0))
            {
                PrintWarning("Config update detected! Updating config values...");
                PrintWarning("Config update completed!");
            }
            var reply = 0;
            if (reply == 0) { }
            config.PluginVersion = Version;
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        void OnPlayerConnected(BasePlayer player)
        {
            if (player == null || !player.IsConnected) return;
            if (player.IsReceivingSnapshot)
            {
                timer.In(1f, () => OnPlayerConnected(player));
                return;
            }
            if (config.OpenToConnectionFirst)
            {
                if (!PlayersList.ContainsKey(player.userID))
                {
                    PlayersList.Add(player.userID, new PlayerClasses() { Firts = true });
                    CreateMenu(player, 0, 0);
                    return;
                }
            }
            if (config.OpenToConnection)
            {
                CreateMenu(player, 0, 0);
                return;
            }
        }

        class PluginConfig
        {
            [JsonProperty("Версия конфигурации")]
            public VersionNumber PluginVersion = new VersionNumber();
            [JsonProperty("Команды для открытия меню")]
            public List<string> Commands = new List<string>();
            [JsonProperty("Показывать при подключении")]
            public bool OpenToConnection = false;
            [JsonProperty("Показывать только при первом подключении")]
            public bool OpenToConnectionFirst = true;
            [JsonProperty("Меню: высота (0.0 - 1.0)")]
            public double MenuAnchorHeight = 1;

            [JsonProperty("Меню: Текст снизу ({0} - онлайн | {1} - спящих | {2} - время")]
            public string MenuText = "Онлайн: {0}\nСпящих: {1}\nВремя: {2}";
            [JsonProperty("Меню: ширина (0.0 - 1.0)")]
            public double MenuAnchorWidth = 1;
            [JsonProperty("Меню: цвет фона (RGBA)")]
            public string MenuColor;
            [JsonProperty("Меню: фоновое изображение (ссылка)")]
            public string MenuBackgroundImage;
            [JsonProperty("Боковое меню: ширина (0.0 - 1.0)")]
            public double SidebarWidth;
            [JsonProperty("Боковое меню: цвет фона (RGBA)")]
            public string SidebarColor;
            [JsonProperty("Вкладки: ширина (0.0 - 1.0)")]
            public double TabWidth;
            [JsonProperty("Вкладки: высота (0.0 - 1.0)")]
            public double TabHeight;
            [JsonProperty("Вкладки: промежуточный отступ (0.0 - 1.0)")]
            public double TabIndent;
            [JsonProperty("Вкладки: верхний отступ (0.0 - 1.0)")]
            public double TabTopIndent;
            [JsonProperty("Вкладки: цвет фона (RGBA)")]
            public string TabColor;
            [JsonProperty("Вкладки: активный цвет фона (RGBA)")]
            public string TabActiveColor;
            [JsonProperty("Вкладки: размер шрифта")]
            public int TabFontSize;
            [JsonProperty("Вкладки: цвет текста (RGBA)")]
            public string TabTextColor;
            [JsonProperty("Вкладки: цвет тени текста (RGBA)")]
            public string TabTextOutlineColor;
            [JsonProperty("Страницы: верхний и нижний отступ (0.0 - 1.0)")]
            public double PageUpperAndLowerIndent;
            [JsonProperty("Страницы: левый и правый отступ (0.0 - 1.0)")]
            public double PageIndentLeftRight;
            [JsonProperty("Вкладки")]
            public List<Tabs> tabs = new List<Tabs>();
            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    PluginVersion = new VersionNumber(),
                    Commands = new List<string>()
                    {
                        "info",
                        "help"
                    },
                    MenuAnchorHeight = 0.9,
                    MenuAnchorWidth = 0.9,
                    MenuBackgroundImage = "",
                    MenuColor = "0.35 0.35 0.35 0.95",
                    OpenToConnection = true,
                    OpenToConnectionFirst = false,
                    PageIndentLeftRight = 0.015,
                    PageUpperAndLowerIndent = 0.03,
                    SidebarColor = "0 0 0 0.7",
                    SidebarWidth = 0.2,
                    TabActiveColor = "0.00 0.68 1.00 1.00",
                    TabColor = "0.00 0.68 1.00 0.4",
                    TabFontSize = 15,
                    TabHeight = 0.08,
                    TabIndent = 0.02,
                    TabTextColor = "1 1 1 1",
                    TabTextOutlineColor = "0 0 0 0.1",
                    TabTopIndent = 0.02,
                    TabWidth = 0.96,
                    tabs = new List<Tabs>()
                    {
                        new Tabs()
                        {
                            Title = "Первая вкладка",
                            pages = new List<Pages>()
                            {
                                new Pages()
                                {

                                    Images = new List<Images>()
                                    {
                                        new Images()
                                        {
                                            URL = "https://rustplugin.ru/styles/uix_dark/images/xlogoRP.png.pagespeed.ic.RO_NOxzsFE.png",
                                            Height = 0.11,
                                            Width = 0.5,
                                            PositionHorizontal =  0.25,
                                            PositionVertical = 0.98,
                                        }
                                    },
                                     Buttons = new List<Button>()
                                    {
                                         new Button()
                                         {
                                             ButtonText = "А это кнопка",
                                             ButtonTextColor = "1 1 1 1",
                                             Color = "0.00 0.68 1.00 1.00",
                                             Command = "chat.say /info",
                                             Height = 0.05,
                                             PositionHorizontal = 0.4,
                                             PositionVertical = 0.2,
                                             TextSize = 15,
                                             Width = 0.2,
                                         },
                                    },
                                    blocks = new List<TextBlocks>()
                                    {

                                        new TextBlocks()
                                        {
                                            Height = 0.85,
                                            colums = new List<TextColumns>()
                                            {
                                                new TextColumns()
                                                {
                                                    Anchor = TextAnchor.UpperCenter,
                                                    ColumnWidth = 1,
                                                    OutlineColor = "0 0 0 0",
                                                    TextList = new List<string>()
                                                    {
                                                        "Приветствуем тебя <color=#00ADFE>{name}</color> Сейчас онлайн: <color=#00ADFE>{online}</color> чел. заходят: <color=#00ADFE>{que}</color>",
                                                    },
                                                    TextSize = 20
                                                },
                                            },
                                        },
                                        new TextBlocks()
                                        {
                                            Height = 0.85,
                                            colums = new List<TextColumns>()
                                            {
                                                new TextColumns()
                                                {
                                                    Anchor = TextAnchor.MiddleCenter,
                                                    ColumnWidth = 1,
                                                    OutlineColor = "0 0 0 0",
                                                    TextList = new List<string>()
                                                    {
                                                        "Это плагин информационного меню для Вашего сервера!\n<b>Настраивайте</b> с осторожностью чтобы ничего не поломать",
                                                        "",
                                                        "Так же не забывайте делать backup конфига, это не убьет ваши нервы"
                                                    },
                                                    TextSize = 20
                                                },
                                            },
                                        },
                                         new TextBlocks()
                                        {
                                            Height = 1,
                                            colums = new List<TextColumns>()
                                            {
                                                new TextColumns()
                                                {
                                                    Anchor = TextAnchor.LowerRight,
                                                    ColumnWidth = 0.99,
                                                    OutlineColor = "0 0 0 0",
                                                    TextList = new List<string>()
                                                    {
                                                        "",
                                                        "С <color=red><3</color> OxideBro - <color=#00ADFE>RustPlugin.ru</color>",
                                                    },
                                                    TextSize = 20
                                                },
                                            },
                                        }
                                    }

                                },
                            },
                        },
                         new Tabs()
                        {
                            Title = "ВКЛАДКА ВАЙП",
                            pages = new List<Pages>()
                            {
                                new Pages()
                                {

                                    Images = new List<Images>(),

                                    blocks = new List<TextBlocks>()
                                    {

                                        new TextBlocks()
                                        {
                                            Height = 0.85,
                                            colums = new List<TextColumns>()
                                            {
                                                new TextColumns()
                                                {
                                                    Anchor = TextAnchor.UpperLeft,
                                                    ColumnWidth = 1,
                                                    OutlineColor = "0 0 0 0",
                                                    TextList = new List<string>()
                                                    {
                                                        " Вайп карты был <color=#00ADFE>{datewipe}</color>",
                                                        " Следующий вайп глобальный <color=#00ADFE>ЗАВТРА</color>"
                                                    },
                                                    TextSize = 20
                                                },
                                            },
                                        }
                                    }

                                },
                            },
                        },
                        new Tabs()
                        {
                            Title = "Вторая вкладка",
                            pages = new List<Pages>()
                            {
                                new Pages()
                                {
                                    Images = new List<Images>(),
                                    blocks = new List<TextBlocks>()
                                    {
                                        new TextBlocks()
                                        {
                                            Height = 1,
                                            colums = new List<TextColumns>()
                                            {
                                                 new TextColumns()
                                                {
                                                    Anchor = TextAnchor.UpperCenter,
                                                    ColumnWidth = 1,
                                                    OutlineColor = "0 0 0 1",
                                                    TextList = new List<string>()
                                                    {
                                                        "Вот так можно сделать ЗАГОЛОВОК",
                                                    },
                                                    TextSize = 20,
                                                },


                                                new TextColumns()
                                                {
                                                    Anchor = TextAnchor.MiddleCenter,
                                                    ColumnWidth = 1,
                                                    OutlineColor = "0 0 0 0",
                                                    TextList = new List<string>()
                                                    {
                                                        "Это пример второй страницы, чтобы вы понимали что плагин ОЧЕНЬ эластичен",
                                                        "Каждое сообщение данной страницы как отдельный столбец.",
                                                        "Это сообщение уже ниже потому что плагин так может ^_^."
                                                    },
                                                    TextSize = 20,
                                                },



                                               new TextColumns()
                                                {
                                                    Anchor = TextAnchor.LowerCenter,
                                                    ColumnWidth = 1,
                                                    OutlineColor = "0 0 0 0",
                                                    TextList = new List<string>()
                                                    {
                                                        "А вот так можно написать текст с низу",
                                                    },
                                                    TextSize = 20,
                                                },
                                            }
                                        }
                                    }

                                },
                                new Pages()
                                {
                                    blocks = new List<TextBlocks>()
                                    {
                                        new TextBlocks()
                                        {
                                            Height = 1,
                                            colums = new List<TextColumns>()
                                            {
                                                 new TextColumns()
                                                {
                                                    Anchor = TextAnchor.UpperCenter,
                                                    ColumnWidth = 1,
                                                    OutlineColor = "0 0 0 1",
                                                    TextList = new List<string>()
                                                    {
                                                        "Вот так можно сделать ЗАГОЛОВОК уже для второй страницы",
                                                    },
                                                    TextSize = 20,
                                                },


                                                new TextColumns()
                                                {
                                                    Anchor = TextAnchor.MiddleCenter,
                                                    ColumnWidth = 1,
                                                    OutlineColor = "0 0 0 0",
                                                    TextList = new List<string>()
                                                    {
                                                        "Это первое сообщение второй страницы",
                                                        "Вот это второе сообщение второй страницы"
                                                    },
                                                    TextSize = 20
                                                },
                                            }
                                        }
                                    }
                                }

                            },
                        },

                    }

                };
            }
        }

        public class Tabs
        {
            [JsonProperty("Заголовок вкладки")]
            public string Title;
            [JsonProperty("Страницы")]
            public List<Pages> pages = new List<Pages>();
        }

        public class Pages
        {
            [JsonProperty("Изображения")]
            public List<Images> Images = new List<Images>();

            [JsonProperty("Кнопки")]
            public List<Button> Buttons = new List<Button>();


            [JsonProperty("Блоки текста")]
            public List<TextBlocks> blocks = new List<TextBlocks>();
        }

        public class Images
        {
            [JsonProperty("Ссылка")]
            public string URL;
            [JsonProperty("Позиция по вертикали (0.0 - 1.0)")]
            public double PositionVertical;
            [JsonProperty("Позиция по горизонтали (0.0 - 1.0)")]
            public double PositionHorizontal;
            [JsonProperty("Высота (0.0 - 1.0)")]
            public double Height;
            [JsonProperty("Ширина (0.0 - 1.0)")]
            public double Width;
        }

        public class Button
        {
            [JsonProperty("Консаольная команда (Для чатовой используйте в начале chat.say")]
            public string Command;
            [JsonProperty("Позиция по вертикали (0.0 - 1.0)")]
            public double PositionVertical;
            [JsonProperty("Позиция по горизонтали (0.0 - 1.0)")]
            public double PositionHorizontal;
            [JsonProperty("Высота (0.0 - 1.0)")]
            public double Height;
            [JsonProperty("Ширина (0.0 - 1.0)")]
            public double Width;
            [JsonProperty("Цвет кнопки")]
            public string Color;
            [JsonProperty("Текст кнопки")]
            public string ButtonText;
            [JsonProperty("Цвет текста кнопки")]
            public string ButtonTextColor;
            [JsonProperty("Размер текста кнопки")]
            public int TextSize;


        }

        public class TextBlocks
        {
            [JsonProperty("Высота блока (0.0 - 1.0)")]
            public double Height;
            [JsonProperty("Колонки текста")]
            public List<TextColumns> colums = new List<TextColumns>();
        }

        public class TextColumns
        {
            [JsonProperty("Ширина колонки (0.0 - 1.0)")]
            public double ColumnWidth;
            [JsonProperty("Выравнивание (0 - UpperLeft, 1 - UpperCenter, 2 - UpperRight, 3 - MiddleLeft, 4 - MiddleCenter, 5 - MiddleRight, 6 - LowerLeft, 7 - LowerCenter, 8 - LowerRight))")]
            public TextAnchor Anchor = TextAnchor.LowerRight;

            [JsonProperty("Размер шрифта")]
            public int TextSize;
            [JsonProperty("Цвет тени текста (RGBA)")]
            public string OutlineColor;
            [JsonProperty("Строки текста ({name} - Имя игрока {online} - онлайн {que} - Заходят {datewipe} - дата вайпа")]
            public List<string> TextList = new List<string>();
        }

        #endregion

        #region Umod

        [PluginReference] Plugin ImageLibrary;
        void OnServerInitialized()
        {
            if (!ImageLibrary)
            {
                PrintError("Imagelibrary not found!");
                return;
            }
            LoadData();

            IEnumerable<Images> images = from message in config.tabs from attachment in message.pages from image in attachment.Images select image;

            foreach (var image in images)
            {
                ImageLibrary?.Call("AddImage", image.URL, image.URL);
            }

            if (!string.IsNullOrEmpty(config.MenuBackgroundImage))
            {
                ImageLibrary?.Call("AddImage", config.MenuBackgroundImage, config.MenuBackgroundImage);

            }

            config.Commands.ForEach(c => cmd.AddChatCommand(c, this, cmdOpenInfoMenu));
            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerConnected);
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, MainLayer);
            SaveData();
        }


        #endregion

        #region Commands
        void cmdOpenInfoMenu(BasePlayer player, string command, string[] args)
        {
            CreateMenu(player, 0, 0);
        }

        [ConsoleCommand("INFOMENU_UI")]
        void cmdSelectPage(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (args.Args.Length == 0) return;
            var type = args.GetInt(0);
            var page = args.GetInt(1);
            CreateMenu(player, type, page);
        }

        [ConsoleCommand("infomenu_close")]
        void cmdCloseInfoMenu(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null) return;
            CuiHelper.DestroyUi(player, MainLayer);

        }

        #endregion

        #region Data

        public Dictionary<ulong, PlayerClasses> PlayersList = new Dictionary<ulong, PlayerClasses>();

        public class PlayerClasses
        {
            public bool Firts = false;
        }

        void LoadData()
        {
            try
            {
                PlayersList = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PlayerClasses>>(Name);
            }
            catch
            {
                PlayersList = new Dictionary<ulong, PlayerClasses>();
            }
        }

        void SaveData()
        {
            if (PlayersList != null)
                Interface.Oxide.DataFileSystem.WriteObject(Name, PlayersList);
        }
        #endregion

        #region UI
        private string MainLayer = "InfoMenu";
        void CreateMenu(BasePlayer player, int type, int page, ulong playerid = 0)
        {
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = config.MenuColor,
                     Material =  "assets/content/ui/ui.background.tile.psd",

                },
                RectTransform =
                {
                    AnchorMin = $"{1 - config.MenuAnchorWidth} {1- config.MenuAnchorHeight}",
                    AnchorMax = $"{config.MenuAnchorWidth} {config.MenuAnchorHeight}"
                }
            }, "Overlay", MainLayer);

            if (!string.IsNullOrEmpty(config.MenuBackgroundImage))
            {
                container.Add(new CuiElement()
                {
                    Parent = MainLayer,
                    Components = {
                            new CuiRawImageComponent {
                                Png = (string)ImageLibrary?.Call("GetImage", config.MenuBackgroundImage), FadeIn = 0.2f, Color = "1 1 1 1"
                            }
                            , new CuiRectTransformComponent {
                                AnchorMin=$"0 0", AnchorMax= $"1 1"
                            }
                           }
                }
                         );
            }

            container.Add(new CuiButton
            {
                Button =
                {
                    Close = MainLayer,
                    Color = "0 0 0 0"
                },
                Text =
                {
                    Text = ""
                },
                RectTransform =
                {
                    AnchorMin = "-100 -100",
                    AnchorMax = "100 100"
                }
            }, MainLayer);

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = config.SidebarColor,
                   Material=  "Assets/Content/UI/UI.Background.Tile.psd",
                },
                RectTransform =
                {
                    AnchorMin = $"0 0",
                    AnchorMax = $"{0 + config.SidebarWidth} 0.998"
                }
            }, MainLayer, $"{MainLayer}SideBar");
            double amin = 1 - config.TabTopIndent;
            double amax = 1 - config.TabTopIndent - config.TabHeight;

            int pages = 0;
            foreach (var button in config.tabs)
            {
                container.Add(new CuiButton
                {
                    Button =
                {
                    Command = config.tabs[type].Title == button.Title ? "‌‌﻿‌‍‌​": $"INFOMENU_UI {pages}",
                    Color = config.tabs[type].Title == button.Title ? config.TabActiveColor : config.TabColor, Material = "assets/content/ui/ui.background.tile.psd",
                },
                    Text =
                {
                    Text = ""
                },
                    RectTransform =
                {
                    AnchorMin = $"{1- config.TabWidth} {amax}",
                    AnchorMax = $"{config.TabWidth} {amin}"
                }
                }, $"{MainLayer}SideBar", $"{MainLayer}Button{button.Title}");

                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = $"{MainLayer}Button{button.Title}",
                    Components =
                            {
                                new CuiTextComponent {Text = $"{button.Title}", FontSize = config.TabFontSize, Align = TextAnchor.MiddleCenter, Color = config.TabTextColor,Font="robotocondensed-bold.ttf",/* FadeIn = 0.1f */},
                                new CuiRectTransformComponent{ AnchorMin = "0 0", AnchorMax = "0.98 1" }
                            }
                });

                amin = amax - config.TabIndent;
                amax = amax - config.TabIndent - config.TabHeight;
                pages++;
            }

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0",
                },
                RectTransform =
                {
                    AnchorMin = $"{0 + config.SidebarWidth} 0",
                    AnchorMax = $"1 0.998"
                }
            }, MainLayer, $"{MainLayer}Info");


            var PanelInfo = config.tabs[type];

            var PageSelect = PanelInfo.pages[page];

            if (PageSelect.Images.Count > 0)
            {
                foreach (var image in PageSelect.Images)
                {
                    container.Add(new CuiElement()
                    {
                        Parent = $"{MainLayer}Info",
                        Components = {
                            new CuiRawImageComponent {
                                Png = (string)ImageLibrary?.Call("GetImage", image.URL), FadeIn = 0.3f, Color = "1 1 1 0.9"
                            }
                            , new CuiRectTransformComponent {
                                AnchorMin=$"{image.PositionHorizontal} {image.PositionVertical - image.Height}", AnchorMax= $"{image.PositionHorizontal + image.Width} {image.PositionVertical}"
                            },
                           }
                    }
                    );
                }
            }
            foreach (var block in PageSelect.blocks)
            {
                foreach (var select in block.colums)
                {
                    var blockMin = block.Height < 0.5 ? 1 - block.Height : 0;
                    var blockMax = block.Height > 0.5 ? block.Height : 1 - 0.03;
                    container.Add(new CuiElement
                    {
                        Name = CuiHelper.GetGuid(),
                        Parent = $"{MainLayer}Info",
                        Components =
                            {
                                new CuiTextComponent { Text = $"{string.Join("\n", select.TextList).Replace("{datewipe}",  $"{SaveRestore.SaveCreatedTime.ToLocalTime().Day} {(Months)SaveRestore.SaveCreatedTime.ToLocalTime().Month}").Replace("{name}", player.displayName).Replace("{online}",  BasePlayer.activePlayerList.Count.ToString()).Replace("{que}", $"{ServerMgr.Instance.connectionQueue.Joining + ServerMgr.Instance.connectionQueue.Queued}")}", FontSize = select.TextSize, Align = select.Anchor, Color = "1 1 1 1",Font="robotocondensed-bold.ttf" , FadeIn = 0.3f},
                                new CuiRectTransformComponent{ AnchorMin = $"0.01 {blockMin}", AnchorMax = $"{select.ColumnWidth} {blockMax}" },
                                new CuiOutlineComponent {Color = select.OutlineColor, Distance = "0.5 -0.5" }
                            }
                    });
                }
            }

            if (PanelInfo.pages.Count > 1)
            {

                if (PanelInfo.pages.Count > page + 1)
                {
                    container.Add(new CuiButton
                    {
                        Button =
                {
                    Command = $"INFOMENU_UI {type} {page + 1}",
                    Color = "0 0 0 0" , Material = "assets/content/ui/ui.background.tile.psd",
                },
                        Text =
                {
                    Text = $"▶", FontSize = 35, Align = TextAnchor.MiddleCenter
                },
                        RectTransform =
                {
                    AnchorMin = $"0.94 0",
                    AnchorMax = $"0.99 0.1"
                }
                    }, $"{MainLayer}Info", $"buttonNext");
                }
                if (page > 0)
                {
                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = $"INFOMENU_UI {type} {page - 1}",
                            Color = "0 0 0 0" , Material = "assets/content/ui/ui.background.tile.psd",
                        },
                        Text =
                        {
                            Text = $"◀", FontSize = 35, Align = TextAnchor.MiddleCenter
                        },
                        RectTransform =
                        {
                            AnchorMin = $"0.01 0",
                            AnchorMax = $"0.06 0.1"
                        }
                    }, $"{MainLayer}Info", $"buttonPrev");
                }

            }

            if (PageSelect.Buttons != null && PageSelect.Buttons.Count > 0)
            {

                foreach (var button in PageSelect.Buttons)
                {
                    container.Add(new CuiButton
                    {
                        Button =
                        {
                            Command = button.Command,
                            Color = button.Color , Material = "assets/content/ui/ui.background.tile.psd",
                        },
                        Text =
                        {
                            Text = button.ButtonText, FontSize = button.TextSize, Align = TextAnchor.MiddleCenter
                        },
                        RectTransform =
                        {
                             AnchorMin=$"{button.PositionHorizontal} {button.PositionVertical - button.Height}", AnchorMax= $"{button.PositionHorizontal + button.Width} {button.PositionVertical}"

                        }
                    }, $"{MainLayer}Info");
                }
            }

            if (!string.IsNullOrEmpty(config.MenuText))
                container.Add(new CuiElement()
                {
                    Parent = $"{MainLayer}SideBar",
                    Components = {
                            new CuiTextComponent {
                                Color = "1 1 1 1", Text = string.Format(config.MenuText, BasePlayer.activePlayerList.Count + "/" + ConVar.Server.maxplayers,BasePlayer.sleepingPlayerList.Count,covalence.Server.Time.ToShortTimeString()  ), Align = TextAnchor.MiddleCenter, FontSize = 20,Font="robotocondensed-bold.ttf"
                            }
                            , new CuiRectTransformComponent {
                                AnchorMin=$"0.05 0", AnchorMax= "1 0.15"
                            }
                            ,
                           }
                }
                        );
            CuiHelper.DestroyUi(player, MainLayer);
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Other
        public enum Months
        {
            Января = 1,
            Февраля = 2,
            Марта = 3,
            Апреля = 4,
            Мая = 5,
            Июня = 6,
            Июля = 7,
            Августа = 8,
            Сентября = 9,
            Октября = 10,
            Ноября = 11,
            Декабря = 12
        }
        #endregion
    }
}