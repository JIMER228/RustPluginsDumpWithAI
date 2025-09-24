using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Welcome UI", "Mevent#4546", "1.0.9")]
    [Description("Information Panel for Server")]
    public class WelcomeUI : RustPlugin
    {
        #region Fields

        [PluginReference] private Plugin ImageLibrary;

        private const string Layer = "UI.Welcome";

        private static WelcomeUI _instance;

        #endregion

        #region Data

        private PluginData _data;

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, _data);
        }

        private void LoadData()
        {
            try
            {
                _data = Interface.Oxide.DataFileSystem.ReadObject<PluginData>(Name);
            }
            catch (Exception e)
            {
                PrintError(e.ToString());
            }

            if (_data == null) _data = new PluginData();
        }

        private class PluginData
        {
            [JsonProperty(PropertyName = "Users", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> Users = new List<ulong>();
        }

        #endregion

        #region Config

        private ConfigData _config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Только первый заход?")]
            public bool Introduce;

            [JsonProperty(PropertyName = "Цвет обводки")]
            public IColor BorderColor = new IColor("#3399DC", 100);

            [JsonProperty(PropertyName = "Резмер кнопки в меню")]
            public float MenuBtnSize = 18f;

            [JsonProperty(PropertyName = "Отступ между кнопками в меню")]
            public float MenuBtnMargin = 2.5f;

            [JsonProperty(PropertyName = "Ширина меню")]
            public float MenuWidth = 700;

            [JsonProperty(PropertyName = "Высота меню")]
            public float MenuHeight = 400;

            [JsonProperty(PropertyName = "Логотип")]
            public UiElement Logo = new UiElement
            {
                AnchorMin = "0.5 1", AnchorMax = "0.5 1",
                OffsetMin = "-80 5", OffsetMax = "80 35",
                Enabled = true,
                Type = CuiElementType.Image,
                Color = new IColor("#FFFFFF", 100),
                Text = new List<string>(),
                FontSize = 0,
                Font = string.Empty,
                Align = TextAnchor.UpperLeft,
                TColor = new IColor("#FFFFFF", 100),
                Command = string.Empty,
                Image = "https://i.imgur.com/eLKYjGR.png"
            };

            [JsonProperty(PropertyName = "Кнопка закрытия")]
            public CloseSettings Close = new CloseSettings
            {
                AnchorMin = "0.5 0", AnchorMax = "0.5 0",
                OffsetMin = "-180 20", OffsetMax = "180 50",
                Enabled = true,
                Type = CuiElementType.Button,
                Color = new IColor("#000000", 60),
                Text = new List<string>
                {
                    "Я прочитал все, что указано здесь"
                },
                FontSize = 16,
                Font = "robotocondensed-bold.ttf",
                Align = TextAnchor.MiddleCenter,
                TColor = new IColor("#FFFFFF", 100),
                Command = string.Empty,
                Image = "https://i.imgur.com/Ku5Z16z.png",
                CloseLast = true
            };

            [JsonProperty(PropertyName = "Меню", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<MenuSettings> Menu = new List<MenuSettings>
            {
                new MenuSettings
                {
                    BtnColor = new IColor("#3399DC", 100),
                    Icon = "https://i.imgur.com/RcORxrs.png",
                    Permission = string.Empty,
                    Elements = new List<UiElement>
                    {
                        new UiElement
                        {
                            AnchorMin = "0 0", AnchorMax = "1 1",
                            OffsetMin = "20 0", OffsetMax = "-20 -20",
                            Enabled = true,
                            Type = CuiElementType.Label,
                            Color = new IColor("#FFFFFF", 100),
                            Text = new List<string>
                            {
                                "<b><color=#b0fa66>Добро пожаловать на SERVERNAME, пожалуйста, прочитайте это перед тем как играть.</color></b>",
                                "<b><color=#5b86b4>SERVER.LINK/DISCORD  SERVER.LINK/STEAM  DONATE.SERVER.LINK</color></b>\n",
                                "<b><color=#5b86b4>ЛИМИТ ИГРОКОВ В КОМАНДЕ</color></b>",
                                "<color=#b0fa66>■</color> Использование сторонних приложений для получения преимущества приведет к бану. Это включает в себя читы, скрипты и макросы.",
                                "<color=#b0fa66>■</color> Спам или расизм приведут к отключению чата или бану в зависимости от продолжительности и контекста.",
                                "<color=#b0fa66>■</color> Любой тип рекламы приведёт к отключению чата или банум.",
                                "<color=#b0fa66>■</color> Если вас поймали на багоюзе, в зависимости от серьезности вы получите бан. Это включает попадание в места за пределами карты или в скалы и т.д.",
                                "<color=#b0fa66>■</color> Выдача себя за сотрудников сервера приведет к тому, что вас забанят, продолжительность этого зависит от намерений человека.",
                                "<color=#b0fa66>■</color> Пожалуйста, уважайте всех сотрудников, они здесь, чтобы помочь."
                            },
                            FontSize = 12,
                            Font = "robotocondensed-bold.ttf",
                            Align = TextAnchor.UpperLeft,
                            TColor = new IColor("#FFFFFF", 100),
                            Command = string.Empty,
                            Image = string.Empty
                        },
                        new UiElement
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
                            OffsetMin = "-20 -20", OffsetMax = "20 20",
                            Enabled = false,
                            Type = CuiElementType.Image,
                            Color = new IColor("#FFFFFF", 100),
                            Text = new List<string>(),
                            FontSize = 0,
                            Font = string.Empty,
                            Align = TextAnchor.UpperLeft,
                            TColor = new IColor("#FFFFFF", 100),
                            Command = string.Empty,
                            Image = "https://i.imgur.com/FShxQ8e.jpeg"
                        }
                    }
                },
                new MenuSettings
                {
                    BtnColor = new IColor("#3399DC", 100),
                    Icon = "https://i.imgur.com/gcTGb2M.png",
                    Permission = string.Empty,
                    Elements = new List<UiElement>
                    {
                        new UiElement
                        {
                            AnchorMin = "0 0", AnchorMax = "1 1",
                            OffsetMin = "20 0", OffsetMax = "-20 -20",
                            Enabled = true,
                            Type = CuiElementType.Label,
                            Color = new IColor("#FFFFFF", 100),
                            Text = new List<string>
                            {
                                "<b><color=#b0fa66>Добро пожаловать на SERVERNAME, пожалуйста, прочитайте это перед тем как играть.</color></b>",
                                "<b><color=#5b86b4>SERVER.LINK/DISCORD  SERVER.LINK/STEAM  DONATE.SERVER.LINK</color></b>\n",
                                "<b><color=#5b86b4>EasyAntiCheat (Facepunch/Rust) Баны:</color></b>",
                                "<color=#b0fa66>■</color> Любой игрок найденный на нашем сервере, уклоняющийся от бана игры, будет навсегда забанен, включая любые будущие аккаунты, приобретенные для обхода первоначального бана игры.",
                                "<color=#b0fa66>■</color> Любой, кого поймают на игре с читером, будет забанен на 2 недели для проверки. Уклонение от этого бана, играя на альтернативном аккаунте, приведет к тому, что вас навсегда забанят.",
                                "<color=#b0fa66>■</color> Любой, кого поймают за игру с человеком с несколькими учетными записями, заблокированными за уклонение от бана, будет навсегда заблокирован (включая запрет по любым причинам в наших Правилах).",
                                "<color=#b0fa66>■</color> Мы верим в один второй шанс. Если вы получили только один запрет EAC для Rust, если вы не уклонялись от этого запрета в течение 90 дней на нашем сервере, вы можете попросить администратора проверить ваше право на игру. Только после проверки и одобрения вы можете начать играть на наших серверах."
                            },
                            FontSize = 12,
                            Font = "robotocondensed-bold.ttf",
                            Align = TextAnchor.UpperLeft,
                            TColor = new IColor("#FFFFFF", 100),
                            Command = string.Empty,
                            Image = string.Empty
                        },
                        new UiElement
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
                            OffsetMin = "-20 -20", OffsetMax = "20 20",
                            Enabled = false,
                            Type = CuiElementType.Image,
                            Color = new IColor("#FFFFFF", 100),
                            Text = new List<string>(),
                            FontSize = 0,
                            Font = string.Empty,
                            Align = TextAnchor.UpperLeft,
                            TColor = new IColor("#FFFFFF", 100),
                            Command = string.Empty,
                            Image = "https://i.imgur.com/FShxQ8e.jpeg"
                        }
                    }
                },
                new MenuSettings
                {
                    BtnColor = new IColor("#3399DC", 100),
                    Icon = "https://i.imgur.com/JL4LFHV.png",
                    Permission = string.Empty,
                    Elements = new List<UiElement>
                    {
                        new UiElement
                        {
                            AnchorMin = "0 0", AnchorMax = "1 1",
                            OffsetMin = "20 0", OffsetMax = "-20 -20",
                            Enabled = true,
                            Type = CuiElementType.Label,
                            Color = new IColor("#FFFFFF", 100),
                            Text = new List<string>
                            {
                                "<b><color=#b0fa66>Добро пожаловать на SERVERNAME, пожалуйста, прочитайте это перед тем как играть.</color></b>",
                                "<b><color=#5b86b4>SERVER.LINK/DISCORD  SERVER.LINK/STEAM  DONATE.SERVER.LINK</color></b>\n",
                                "<b><color=#5b86b4>Стрим-Снайпинг:</color></b>",
                                "<color=#b0fa66>■</color> Стрим-снайпинг ПОДТВЕРЖДЁННЫХ стримеров запрещён",
                                "<color=#b0fa66>■</color> Любой, кого поймали за стрим-снайпинг потверждённого стримера, будет наказан в зависимости от тяжести преступления, вплоть до бана на сервере\n",
                                "<b><color=#5b86b4>Прокси & VPN:</color></b>",
                                "<color=#b0fa66>■</color> Мы не разрешаем использование прокси или VPN любого типа на наших серверах, если у вас нет разрешения от администратора. Присоединение к серверу с прокси/VPN приведет к бану, если это не будет одобрено.",
                                "<color=#b0fa66>■</color> Подача заявки на VPN-доступ не означает, что вы будете одобрены, а использование его для обхода фильтра нашей страны приведет к отклонению заявки."
                            },
                            FontSize = 12,
                            Font = "robotocondensed-bold.ttf",
                            Align = TextAnchor.UpperLeft,
                            TColor = new IColor("#FFFFFF", 100),
                            Command = string.Empty,
                            Image = string.Empty
                        },
                        new UiElement
                        {
                            AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
                            OffsetMin = "-20 -20", OffsetMax = "20 20",
                            Enabled = false,
                            Type = CuiElementType.Image,
                            Color = new IColor("#FFFFFF", 100),
                            Text = new List<string>(),
                            FontSize = 0,
                            Font = string.Empty,
                            Align = TextAnchor.UpperLeft,
                            TColor = new IColor("#FFFFFF", 100),
                            Command = string.Empty,
                            Image = "https://i.imgur.com/FShxQ8e.jpeg"
                        }
                    }
                }
            };
        }

        private class MenuSettings
        {
            [JsonProperty(PropertyName = "Цвет кнопки")]
            public IColor BtnColor;

            [JsonProperty(PropertyName = "Иконка")]
            public string Icon;

            [JsonProperty(PropertyName = "Разрешение (например: welcomeui.vip)")]
            public string Permission;

            [JsonProperty(PropertyName = "Элементы интерфейса",
                ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<UiElement> Elements;
        }

        private abstract class InterfacePosition
        {
            public string AnchorMin;

            public string AnchorMax;

            public string OffsetMin;

            public string OffsetMax;
        }

        private enum CuiElementType
        {
            Label,
            Panel,
            Button,
            Image
        }

        private class UiElement : InterfacePosition
        {
            [JsonProperty(PropertyName = "Включено?")]
            public bool Enabled;

            [JsonProperty(PropertyName = "Тип (Label/Panel/Button/Image)")] [JsonConverter(typeof(StringEnumConverter))]
            public CuiElementType Type;

            [JsonProperty(PropertyName = "Цвет")] public IColor Color;

            [JsonProperty(PropertyName = "Текст", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<string> Text;

            [JsonProperty(PropertyName = "Размер текста")]
            public int FontSize;

            [JsonProperty(PropertyName = "Шрифт")] public string Font;

            [JsonProperty(PropertyName = "Расположение")] [JsonConverter(typeof(StringEnumConverter))]
            public TextAnchor Align;

            [JsonProperty(PropertyName = "Цвет теста")]
            public IColor TColor;

            [JsonProperty(PropertyName = "Команда ({user} - user steamid)")]
            public string Command;

            [JsonProperty(PropertyName = "Изображение")]
            public string Image;

            public void Get(ref CuiElementContainer container, BasePlayer player, string parent, string name = null,
                string close = "")
            {
                if (!Enabled) return;

                if (string.IsNullOrEmpty(name))
                    name = CuiHelper.GetGuid();

                switch (Type)
                {
                    case CuiElementType.Label:
                    {
                        container.Add(new CuiElement
                        {
                            Name = name,
                            Parent = parent,
                            Components =
                            {
                                new CuiTextComponent
                                {
                                    Text = $"{string.Join("\n", Text)}",
                                    Align = Align,
                                    Font = Font,
                                    FontSize = FontSize,
                                    Color = TColor.Get()
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = AnchorMin, AnchorMax = AnchorMax,
                                    OffsetMin = OffsetMin, OffsetMax = OffsetMax
                                }
                            }
                        });
                        break;
                    }
                    case CuiElementType.Panel:
                    {
                        container.Add(new CuiElement
                        {
                            Name = name,
                            Parent = parent,
                            Components =
                            {
                                new CuiImageComponent
                                {
                                    Color = Color.Get()
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = AnchorMin, AnchorMax = AnchorMax,
                                    OffsetMin = OffsetMin, OffsetMax = OffsetMax
                                }
                            }
                        });
                        break;
                    }
                    case CuiElementType.Button:
                    {
                        container.Add(new CuiButton
                        {
                            RectTransform =
                            {
                                AnchorMin = AnchorMin, AnchorMax = AnchorMax,
                                OffsetMin = OffsetMin, OffsetMax = OffsetMax
                            },
                            Text =
                            {
                                Text = $"{string.Join("\n", Text)}",
                                Align = Align,
                                Font = Font,
                                FontSize = FontSize,
                                Color = TColor.Get()
                            },
                            Button =
                            {
                                Command = $"{Command}".Replace("{user}", player.UserIDString),
                                Color = Color.Get(),
                                Close = close
                            }
                        }, parent, name);
                        break;
                    }
                    case CuiElementType.Image:
                    {
                        container.Add(new CuiElement
                        {
                            Name = name,
                            Parent = parent,
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Png = _instance.ImageLibrary.Call<string>("GetImage", Image),
                                    Color = Color.Get()
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = AnchorMin, AnchorMax = AnchorMax,
                                    OffsetMin = OffsetMin, OffsetMax = OffsetMax
                                }
                            }
                        });
                        break;
                    }
                }
            }
        }

        private class CloseSettings : UiElement
        {
            [JsonProperty(PropertyName = "Показывать кнопку закрытия только на посленей странице?")]
            public bool CloseLast;
        }

        private class IColor
        {
            [JsonProperty(PropertyName = "HEX")] public string HEX;

            [JsonProperty(PropertyName = "Непрозрачность (0 - 100)")]
            public float Alpha;

            public string Get()
            {
                if (string.IsNullOrEmpty(HEX)) HEX = "#FFFFFF";

                var str = HEX.Trim('#');
                if (str.Length != 6) throw new Exception(HEX);
                var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
                var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
                var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);

                return $"{(double)r / 255} {(double)g / 255} {(double)b / 255} {Alpha / 100}";
            }

            public IColor(string hex, float alpha)
            {
                HEX = hex;
                Alpha = alpha;
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<ConfigData>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        protected override void LoadDefaultConfig()
        {
            _config = new ConfigData();
        }

        #endregion

        #region Hooks

        private void OnServerInitialized()
        {
            _instance = this;

            LoadImages();

            LoadData();

            _config.Menu.ForEach(menu =>
            {
                if (!string.IsNullOrEmpty(menu.Permission) && !permission.PermissionExists(menu.Permission))
                    permission.RegisterPermission(menu.Permission, this);
            });

            AddCovalenceCommand(new[] { "info" }, nameof(CmdChatHelp));
        }

        private void Unload()
        {
            SaveData();

            foreach (var player in BasePlayer.activePlayerList) CuiHelper.DestroyUi(player, Layer);

            _instance = null;
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null || player.IsNpc) return;

            if (_config.Introduce && !_data.Users.Contains(player.userID))
            {
                MainUi(player, isFirst: true);
                _data.Users.Add(player.userID);
            }
            else if (!_config.Introduce)
            {
                MainUi(player, isFirst: true);
            }
        }

        #endregion

        #region Commands

        private void CmdChatHelp(IPlayer cov, string command, string[] args)
        {
            var player = cov?.Object as BasePlayer;
            if (player == null) return;

            MainUi(player, isFirst: true);
        }

        [ConsoleCommand("welcomemenu")]
        private void CmdConsole(ConsoleSystem.Arg args)
        {
            var player = args.Player();
            if (player == null || !args.HasArgs()) return;

            switch (args.Args[0].ToLower())
            {
                case "page":
                {
                    int page;
                    if (!args.HasArgs(2) || !int.TryParse(args.Args[1], out page)) return;

                    MainUi(player, page);
                    break;
                }
            }
        }

        #endregion

        #region Interface

        private void MainUi(BasePlayer player, int page = 0, bool isFirst = false)
        {
            var container = new CuiElementContainer();
            var list = _config.Menu[page];

            #region Background

            if (isFirst)
            {
                CuiHelper.DestroyUi(player, Layer);

                container.Add(new CuiPanel
                {
                    CursorEnabled = true,
                    RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                    Image = { Color = "0 0 0 0.5", Material = "assets/content/ui/uibackgroundblur.mat" }
                }, "Overlay", Layer);
            }

            #endregion

            #region Main

            container.Add(new CuiPanel
            {
                RectTransform =
                {
                    AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5",
                    OffsetMin = $"-{_config.MenuWidth / 2f} -{_config.MenuHeight / 2f}",
                    OffsetMax = $"{_config.MenuWidth / 2f} {_config.MenuHeight / 2f}"
                },
                Image = { Color = "0 0 0 0.8" }
            }, Layer, Layer + ".Main");

            var yCoord = 0f;

            CreateOutLine(ref container, Layer + ".Main");

            if (_config.Logo.Enabled)
                _config.Logo.Get(ref container, player, Layer + ".Main");

            var menuList = _config.Menu.FindAll(x =>
                string.IsNullOrEmpty(x.Permission) || permission.UserHasPermission(player.UserIDString, x.Permission));

            for (var i = 0; i < menuList.Count; i++)
            {
                container.Add(new CuiButton
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1", AnchorMax = "0 1",
                        OffsetMin = $"-{_config.MenuBtnSize} {yCoord - _config.MenuBtnSize}",
                        OffsetMax = $"0 {yCoord}"
                    },
                    Button =
                    {
                        Color = menuList[i].BtnColor.Get(),
                        Command = $"welcomemenu page {_config.Menu.IndexOf(menuList[i])}"
                    },
                    Text = { Text = "" }
                }, Layer + ".Main", Layer + $".Btn.{i}");

                container.Add(new CuiElement
                {
                    Parent = Layer + $".Btn.{i}",
                    Components =
                    {
                        new CuiRawImageComponent
                            { Png = ImageLibrary.Call<string>("GetImage", menuList[i].Icon) },
                        new CuiRectTransformComponent
                            { AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin = "5 5", OffsetMax = "-5 -5" }
                    }
                });

                yCoord = yCoord - _config.MenuBtnSize - _config.MenuBtnMargin;
            }

            list.Elements.ForEach(el => el.Get(ref container, player, Layer + ".Main"));

            if (!_config.Close.CloseLast || _config.Close.CloseLast && page == _config.Menu.Count - 1)
            {
                _config.Close.Get(ref container, player, Layer + ".Main", Layer + ".Btn.Close", Layer);

                CreateOutLine(ref container, Layer + ".Btn.Close");
            }

            #endregion

            CuiHelper.DestroyUi(player, Layer + ".Main");
            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Utils

        private void CreateOutLine(ref CuiElementContainer container, string parent, int size = 2)
        {
            var color = _config.BorderColor.Get();

            container.Add(new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0",
                        AnchorMax = "1 0",
                        OffsetMin = $"{size} 0",
                        OffsetMax = $"-{size} {size}"
                    },
                    Image = { Color = color }
                },
                parent);
            container.Add(new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 1",
                        AnchorMax = "1 1",
                        OffsetMin = $"{size} -{size}",
                        OffsetMax = $"-{size} 0"
                    },
                    Image = { Color = color }
                },
                parent);
            container.Add(new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "0 0", AnchorMax = "0 1",
                        OffsetMin = "0 0",
                        OffsetMax = $"{size} 0"
                    },
                    Image = { Color = color }
                },
                parent);
            container.Add(new CuiPanel
                {
                    RectTransform =
                    {
                        AnchorMin = "1 0",
                        AnchorMax = "1 1",
                        OffsetMin = $"-{size} 0",
                        OffsetMax = "0 0"
                    },
                    Image = { Color = color }
                },
                parent);
        }

        private void LoadImages()
        {
            if (!ImageLibrary)
            {
                PrintWarning("IMAGE LIBRARY IS NOT INSTALLED");
            }
            else
            {
                var imagesList = new Dictionary<string, string>();

                if (_config.Logo.Enabled && !string.IsNullOrEmpty(_config.Logo.Image))
                    imagesList.Add(_config.Logo.Image, _config.Logo.Image);

                if (_config.Close.Enabled && !string.IsNullOrEmpty(_config.Close.Image))
                    imagesList.Add(_config.Close.Image, _config.Close.Image);

                _config.Menu.ForEach(menu =>
                {
                    if (!string.IsNullOrEmpty(menu.Icon) && !imagesList.ContainsKey(menu.Icon))
                        imagesList.Add(menu.Icon, menu.Icon);

                    menu.Elements.ForEach(el =>
                    {
                        if (!string.IsNullOrEmpty(el.Image) && !imagesList.ContainsKey(el.Image))
                            imagesList.Add(el.Image, el.Image);
                    });
                });

                ImageLibrary?.Call("ImportImageList", Title, imagesList, 0UL, true);
            }
        }

        #endregion
    }
}