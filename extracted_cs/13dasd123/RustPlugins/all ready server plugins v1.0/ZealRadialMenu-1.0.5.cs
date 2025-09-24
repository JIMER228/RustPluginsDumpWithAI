using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Globalization;
using System.Linq;
using Oxide.Core;
using Color = UnityEngine.Color;

namespace Oxide.Plugins
{
    [Info("ZealRadialMenu", "Kira", "1.0.5")]
    [Description("Меню, с кнопками расположенными по оси.")]
    class ZealRadialMenu : RustPlugin
    {
        #region [Reference] / [Запросы]

        [PluginReference] Plugin ImageLibrary;

        private string GetImg(string name)
        {
            return (string) ImageLibrary?.Call("GetImage", name) ?? "";
        }

        #endregion

        #region [Configuraton] / [Конфигурация]

        static public ConfigData config;

        public class ButtonElement
        {
            [JsonProperty(PropertyName = "Название кнопки")]
            public string button_name;

            [JsonProperty(PropertyName = "Иконка заднего фона")]
            public string background_url_img;

            [JsonProperty(PropertyName = "Главная иконка")]
            public string icon_url;

            [JsonProperty(PropertyName = "Цвет заднего фона")]
            public string background_color;

            [JsonProperty(PropertyName = "Цвет текста")]
            public string text_color;

            [JsonProperty(PropertyName = "Цвет главной иконки")]
            public string icon_color;

            [JsonProperty(PropertyName = "Цвет обводки иконки")]
            public string outline_color_icon;

            [JsonProperty(PropertyName = "Размер кнопки")]
            public int button_size;

            [JsonProperty(PropertyName = "Размер текста кнопки")]
            public int text_size;

            [JsonProperty(PropertyName = "Текст кнопки")]
            public string text;

            [JsonProperty(PropertyName = "Команда кнопки")]
            public string command;

            [JsonProperty(PropertyName = "Включить текст ?")]
            public bool text_bool;

            [JsonProperty(PropertyName = "Включить обводку иконки ?")]
            public bool icon_outline;

            [JsonProperty(PropertyName = "Включить иконку ?")]
            public bool icon_bool;
        }

        public class ConfigData
        {
            [JsonProperty(PropertyName = "ZealRadialMenu")]
            public GUICFG ZealRadialMenu = new GUICFG();

            public class GUICFG
            {
                [JsonProperty(PropertyName = "Радиус кнопок")] 
                public int radius;

                [JsonProperty(PropertyName = "Изображение кнопки выхода из меню (URL)")]
                public string imgclose;
 
                [JsonProperty(PropertyName = "Цвет кнопки выхода из меню (HEX)")]
                public string imgclosecolor;

                [JsonProperty(PropertyName = "Настройка кнопок")]
                public List<ButtonElement> Buttons = new List<ButtonElement>();
            }
        }

        public ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                ZealRadialMenu = new ConfigData.GUICFG
                {
                    radius = 100,
                    imgclose = "https://i.imgur.com/Fpkaxgp.png",
                    imgclosecolor = "#BA3737FF",
                    Buttons =
                    {
                        new ButtonElement
                        {
                            button_name = "test1",
                            background_url_img = "http://i.imgur.com/FVeJbNJ.png",
                            icon_url = "https://i.imgur.com/Xa4C18L.png",

                            background_color = "#3F8C48FF",
                            text_color = "#CACACA",
                            icon_color = "#CACACA",

                            outline_color_icon = "#00000099",

                            button_size = 50,
                            text_size = 25,

                            text = "LIKE",
                            command = "chat.say 0 [OK]",

                            icon_bool = true,
                            icon_outline = true,
                            text_bool = false
                        },
                        new ButtonElement
                        {
                            button_name = "test2",
                            background_url_img = "https://i.imgur.com/4WlOU5g.png",
                            icon_url = "https://i.imgur.com/Xa4C18L.png",

                            background_color = "#3F8C48FF",
                            text_color = "#CACACA",
                            icon_color = "#CACACA",

                            outline_color_icon = "#00000099",

                            button_size = 50,
                            text_size = 25,

                            text = "LIKE",
                            command = "chat.say 0 [OK]",

                            icon_bool = false,
                            icon_outline = false,
                            text_bool = true
                        },
                        new ButtonElement
                        {
                            button_name = "test3",
                            background_url_img = "http://i.imgur.com/FVeJbNJ.png",
                            icon_url = "https://i.imgur.com/Xa4C18L.png",

                            background_color = "#3F8C48FF",
                            text_color = "#CACACA",
                            icon_color = "#CACACA",

                            outline_color_icon = "#00000099",

                            button_size = 50,
                            text_size = 25,

                            text = "LIKE",
                            command = "chat.say 0 [OK]",

                            icon_bool = false,
                            icon_outline = true,
                            text_bool = true
                        }
                    }
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region [DrawUI] / [Показ UI]

        private string Sharp = "assets/content/ui/ui.background.tile.psd";
        private string Blur = "assets/content/ui/uibackgroundblur.mat";

        void DrawUI(BasePlayer player)
        {
            CuiElementContainer Gui = new CuiElementContainer();
            var config = ZealRadialMenu.config.ZealRadialMenu;

            Gui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0.5 0.5",
                    AnchorMax = "0.5 0.5"
                }
            }, "Hud", "CenterHUD");

            Gui.Add(new CuiElement
            {
                Name = "CloseIMG",
                Parent = "CenterHUD",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = HexToRustFormat(config.imgclosecolor),
                        Png = GetImg("Close")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "-24.00039 -25.00073",
                        AnchorMax = "26.00038 25.00075"
                    }
                }
            });

            Gui.Add(new CuiButton
            {
                Button =
                {
                    Command = "close.menu",
                    Color = "0 0 0 0",
                    Close = "CloseIMG"
                },
                Text =
                {
                    Text = ""
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "CloseIMG", "close.menu");

            int butnum = 0;
            foreach (var button in config.Buttons)
            {
                int r = config.radius + 10 * config.Buttons.Count;
                double c = (double) config.Buttons.Count / 2;
                double rad = (double) butnum / c * 3.14;
                double x = r * Math.Cos(rad);
                double y = r * Math.Sin(rad);


                if (button.background_url_img != "" & button.background_url_img != " ")
                {
                    Gui.Add(new CuiElement
                    {
                        Name = button.button_name,
                        Parent = "CenterHUD",
                        Components =
                        {
                            new CuiRawImageComponent
                            {
                                Color = HexToRustFormat(button.background_color),
                                Png = GetImg(button.button_name + button.button_size)
                            },
                            new CuiRectTransformComponent
                            {
                                AnchorMin = $"{x - button.button_size} {y - button.button_size}",
                                AnchorMax = $"{x + button.button_size} {y + button.button_size}"
                            }
                        }
                    });

                    if (button.icon_bool != false)
                    {
                        string outline_color_icon = "#3E7844FF";
                        if (button.icon_outline == true)
                        {
                            outline_color_icon = button.outline_color_icon;
                        }

                        Gui.Add(new CuiElement
                        {
                            Name = "Icon_" + button.button_name,
                            Parent = button.button_name,
                            Components =
                            {
                                new CuiRawImageComponent
                                {
                                    Color = HexToRustFormat(button.icon_color),
                                    Png = GetImg(button.button_name + button.button_size + "_ico")
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.2 0.2",
                                    AnchorMax = "0.8 0.85"
                                },
                                new CuiOutlineComponent
                                {
                                    Color = HexToRustFormat(outline_color_icon),
                                    Distance = "0.2 0.2"
                                }
                            }
                        });
                    }

                    if (button.text_bool != true)
                    {
                        Gui.Add(new CuiButton
                            {
                                Button =
                                {
                                    Command = $"sendcmd {button.command}",
                                    Color = "0 0 0 0",
                                    Close = "CenterHUD"
                                },
                                Text =
                                {
                                    Text = " "
                                },
                                RectTransform =
                                {
                                    AnchorMin = "0 0",
                                    AnchorMax = "1 1"
                                }
                            }, button.button_name, "Button_" + button.button_name);
                    }
                    else
                    {
                        Gui.Add(new CuiButton
                            {
                                Button =
                                {
                                    Command = $"sendcmd {button.command}",
                                    Color = "0 0 0 0",
                                    Close = "CenterHUD"
                                },
                                Text =
                                {
                                    Align = TextAnchor.MiddleCenter,
                                    Color = HexToRustFormat(button.text_color),
                                    FontSize = button.text_size,
                                    Text = button.text,
                                    Font = "robotocondensed-regular.ttf"
                                },
                                RectTransform =
                                {
                                    AnchorMin = "0 0.1",
                                    AnchorMax = "1 0.85"
                                }
                            }, button.button_name, "Button_" + button.button_name);
                    }

                    butnum++;
                }
            }

            CuiHelper.AddUi(player, Gui);
        }

        #endregion

        #region [ChatCommand] / [Чат команды]

        [ChatCommand("Menu")]
        private void MenuOpen(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "CenterHUD");
            DrawUI(player);
        }

        [ConsoleCommand("close.menu")]
        private void MenuClose(ConsoleSystem.Arg args)
        {
            if (args.Player() != null)
            {
                var player = args.Player();
                CuiHelper.DestroyUi(player, "CenterHUD");
            }
        }

        [ConsoleCommand("sendcmd")]
        private void SendCMD(ConsoleSystem.Arg args)
        {
            if (args.Player() != null)
            {
                var player = args.Player();
                string convertcmd =
                    $"{args.Args[0]}  \" {string.Join(" ", args.Args.ToList().GetRange(1, args.Args.Length - 1))}\" 0";
                player.SendConsoleCommand(convertcmd);
            }
        }

        #endregion

        #region [Hooks] / [Крюки]

        void OnServerInitialized()
        {
            LoadConfig();

            if (ImageLibrary != null)
            {
                ImageLibrary.Call("AddImage", config.ZealRadialMenu.imgclose, "Close");

                foreach (var button in config.ZealRadialMenu.Buttons)
                {
                    ImageLibrary.Call("AddImage", button.background_url_img, button.button_name + button.button_size);
                    if (button.icon_bool == true)
                    {
                        ImageLibrary.Call("AddImage", button.icon_url,
                            button.button_name + button.button_size + "_ico");
                    }
                }
                
            }
            else
            {
                PrintError($"На сервере не установлен плагин [ImageLibrary]");
                Interface.Oxide.UnloadPlugin(Title);
            }
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');
            if (str.Length == 6)
                str += "FF";
            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);

            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);
            Color color = new Color32(r, g, b, a);
            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        #endregion
    }
}